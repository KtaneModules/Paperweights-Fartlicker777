using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class Paperweights : MonoBehaviour {

   public KMBombInfo Bomb;
   public KMAudio Audio;

   public KMSelectable[] Balls;
   public GameObject[] BallsInScale;
   public KMSelectable[] Bowls;
   public GameObject[] LeftDigit;
   public GameObject[] RightDigit;

   public GameObject Tippy;
   public GameObject[] Scales;

   enum BowlSelection {
      Left,
      Right
   }

   enum Placements {
      None,
      Left,
      Right
   }

   enum Lean {
      Center,
      Left,
      Transitioning,
      Right
   }

   Lean Rot;
   Placements[] BallPlacement = new Placements[8];

   BowlSelection BigBowlsOfSelecting;
   bool[] Gone = new bool[8];

   int[][] Generation = {
      new int[] { 0, 0, 0, 0},
      new int[] { 0, 0, 0, 0}
   };
   int[] Solution = new int[8];
   int[][] SevenSeg = {
      new int[] { 0, 1, 2, 4, 5, 6},
      new int[] { 2, 5},
      new int[] { 0, 2, 3, 4, 6},
      new int[] { 0, 2, 3, 5, 6},
      new int[] { 1, 2, 3, 5},
      new int[] { 0, 1, 3, 5, 6},
      new int[] { 0, 1, 3, 4, 5, 6},
      new int[] { 0, 2, 5},
      new int[] { 0, 1, 2, 3, 4, 5, 6},
      new int[] { 0, 1, 2, 3, 5, 6},
   };

   int[] Weights = { 0, 0 };

   static int ModuleIdCounter = 1;
   int ModuleId;
   private bool ModuleSolved;

   void Awake () {
      ModuleId = ModuleIdCounter++;

      foreach (KMSelectable Ball in Balls) {
         Ball.OnInteract += delegate () { BallPress(Ball); return false; };
      }

      foreach (KMSelectable Bowl in Bowls) {
         Bowl.OnInteract += delegate () { BowlPress(Bowl); return false; };
      }

   }

   void BallPress (KMSelectable Ball) {
      Audio.PlaySoundAtTransform("Marble Hit", Ball.transform);
      for (int i = 0; i < 8; i++) {
         if (Ball == Balls[i]) {
            if (BallPlacement[i] == Placements.Left) {
               Weights[0] -= Solution[i];
               BallPlacement[i] = Placements.None;
            }
            else if (BallPlacement[i] == Placements.Right) {
               Weights[1] -= Solution[i];
               BallPlacement[i] = Placements.None;
            }
            else if (BigBowlsOfSelecting == BowlSelection.Left) {
               Weights[0] += Solution[i];
               BallPlacement[i] = Placements.Left;
            }
            else {
               Weights[1] += Solution[i];
               BallPlacement[i] = Placements.Right;
            }

            LookAtMyBalls(Ball, i);
            if (Weights[0] != 0 && Weights[1] != 0) {
               UpdateDifference(Math.Abs(Weights[0] - Weights[1]) % 100);
            }
            else {
               UpdateDifference(-1);
            }
         }
      }
      StartCoroutine(RotateScale());
      for (int i = 0; i < 8; i++) {
         if (BallPlacement[i] == Placements.None) {
            return;
         }
      }
      CheckSolution();
   }

   void CheckSolution () {
      if (Weights[0] == Weights[1]) {
         GetComponent<KMBombModule>().HandlePass();
      }
      ModuleSolved = true;
   }

   void LookAtMyBalls (KMSelectable Ball, int i) {
      Ball.gameObject.SetActive(Gone[i]);
      if (Gone[i]) {
         BallsInScale[i].SetActive(false);
         BallsInScale[8 + i].SetActive(false);
      }
      else {
         BallsInScale[8 * (int) BigBowlsOfSelecting + i].SetActive(true);
      }
      Gone[i] ^= true;
   }

   void BowlPress (KMSelectable Bowl) {
      Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Bowl.transform);
      if (Bowl == Bowls[0]) {
         BigBowlsOfSelecting = BowlSelection.Left;
      }
      else {
         BigBowlsOfSelecting = BowlSelection.Right;
      }
   }

   void Start () {
      CalculateWeights();
      string[] ColorNames = { "black", "cyan", "yellow", "red", "blue", "green", "white", "magenta"};
      Debug.LogFormat("[Paperweights #{0}] Both sides must equal {1}.", ModuleId, Solution.Sum() / 2);
      for (int i = 0; i < 8; i++) {
         Debug.LogFormat("[Paperweights #{0}] The {1} paperweight has a weight of {2}.", ModuleId, ColorNames[i], Solution[i]);
      }
   }

   void CalculateWeights () {
      for (int i = 0; i < 2; i++) {
         for (int j = 0; j < 4; j++) {
            Generation[i][j] = 0;
         }
      }
      int Sum = Rnd.Range(50, 101);
      for (int i = 0; i < 2; i++) {
         for (int j = 0; j < 3; j++) {
            Generation[i][j] = Rnd.Range(4 - j, Sum - Generation[i].Sum());
         }
         Generation[i][3] = Sum - Generation[i].Sum();
      }
      for (int i = 0; i < 7; i++) {
         for (int j = i + 1; j < 8; j++) {
            if (Generation[i / 4][i % 4] == Generation[j / 4][j % 4] || Generation[i / 4][i % 4] < 1 || Generation[i / 4][i % 4] > 99 || Generation[j / 4][j % 4] < 1 || Generation[j / 4][j % 4] > 99) {
               CalculateWeights();
               return;
            }
         }
      }
      for (int i = 0; i < 8; i++) {
         Solution[i] = Generation[i / 4][i % 4];
      }
   }

   void UpdateDifference (int Weight) {
      foreach (GameObject l in LeftDigit) {
         l.SetActive(false);
      }
      foreach (GameObject l in RightDigit) {
         l.SetActive(false);
      }
      if (Weight == -1) {
         return;
      }
      for (int i = 0; i < SevenSeg[Weight / 10].Length; i++) {
         LeftDigit[SevenSeg[Weight / 10][i]].SetActive(true);
      }
      for (int i = 0; i < SevenSeg[Weight % 10].Length; i++) {
         RightDigit[SevenSeg[Weight % 10][i]].SetActive(true);
      }
   }

   IEnumerator RotateScale () {        //I tried splitting these in separate functions, but I don't know how nested coroutines will affect each other.
      while (Rot == Lean.Transitioning) {
         yield return null;
      }
      if (Weights[0] < Weights[1] && Rot == Lean.Center) {
         Rot = Lean.Transitioning;
         for (int i = 0; i < 40; i++) {
            Tippy.transform.Rotate(new Vector3(0, 0, .5f));
            Scales[0].transform.localPosition = new Vector3(.1f / 40 * i, 0.44f / 40 * i, 0);
            Scales[1].transform.localPosition = new Vector3(-0.09f / 40 * i, -.5f / 40 * i, 0);
            Scales[0].transform.Rotate(new Vector3(0, 0, -.5f));
            Scales[1].transform.Rotate(new Vector3(0, 0, -.5f));
            yield return null;
         }
         Rot = Lean.Left;
      }
      else if (Weights[0] < Weights[1] && Rot == Lean.Right) {
         Rot = Lean.Transitioning;

         for (int i = 0; i < 40; i++) {
            Tippy.transform.Rotate(new Vector3(0, 0, .5f));
            Scales[0].transform.localPosition -= new Vector3(0.05f / 40, -0.47f / 40, 0);
            Scales[1].transform.localPosition -= new Vector3(-0.1f / 40, .46f / 40, 0);
            Scales[0].transform.Rotate(0, 0, -.5f);
            Scales[1].transform.Rotate(0, 0, -.5f);
            yield return null;
         }

         for (int i = 0; i < 40; i++) {
            Tippy.transform.Rotate(new Vector3(0, 0, .5f));
            Scales[0].transform.localPosition = new Vector3(.1f / 40 * i, 0.44f / 40 * i, 0);
            Scales[1].transform.localPosition = new Vector3(-0.09f / 40 * i, -.5f / 40 * i, 0);
            Scales[0].transform.Rotate(new Vector3(0, 0, -.5f));
            Scales[1].transform.Rotate(new Vector3(0, 0, -.5f));
            yield return null;
         }
         Rot = Lean.Left;
      }
      else if (Weights[1] < Weights[0] && Rot == Lean.Center) {
         Rot = Lean.Transitioning;
         for (int i = 0; i < 40; i++) {
            Tippy.transform.Rotate(new Vector3(0, 0, -.5f));
            Scales[0].transform.localPosition = new Vector3(0.05f / 40 * i, -0.47f / 40 * i, 0);
            Scales[1].transform.localPosition = new Vector3(-0.1f / 40 * i, .46f / 40 * i, 0);
            Scales[0].transform.Rotate(0, 0, .5f);
            Scales[1].transform.Rotate(0, 0, .5f);
            yield return null;
         }
         Rot = Lean.Right;
      }
      else if (Weights[1] < Weights[0] && Rot == Lean.Left) {
         Rot = Lean.Transitioning;
         for (int i = 0; i < 40; i++) {
            Tippy.transform.Rotate(new Vector3(0, 0, -.5f));
            Scales[0].transform.localPosition += new Vector3(-.1f / 40, -0.44f / 40, 0);
            Scales[1].transform.localPosition += new Vector3(0.09f / 40, .5f / 40, 0);
            Scales[0].transform.Rotate(new Vector3(0, 0, .5f));
            Scales[1].transform.Rotate(new Vector3(0, 0, .5f));
            yield return null;
         }

         for (int i = 0; i < 40; i++) {
            Tippy.transform.Rotate(new Vector3(0, 0, -.5f));
            Scales[0].transform.localPosition = new Vector3(0.05f / 40 * i, -0.47f / 40 * i, 0);
            Scales[1].transform.localPosition = new Vector3(-0.1f / 40 * i, .46f / 40 * i, 0);
            Scales[0].transform.Rotate(0, 0, .5f);
            Scales[1].transform.Rotate(0, 0, .5f);
            yield return null;
         }

         Rot = Lean.Right;
      }
      else if (Weights[0] == Weights[1] && Rot == Lean.Left) {
         Rot = Lean.Transitioning;
         for (int i = 0; i < 40; i++) {
            Tippy.transform.Rotate(new Vector3(0, 0, -.5f));
            Scales[0].transform.localPosition += new Vector3(-.1f / 40, -0.44f / 40, 0);
            Scales[1].transform.localPosition += new Vector3(0.09f / 40, .5f / 40, 0);
            Scales[0].transform.Rotate(new Vector3(0, 0, .5f));
            Scales[1].transform.Rotate(new Vector3(0, 0, .5f));
            yield return null;
         }
         Rot = Lean.Center;
      }
      else if (Weights[0] == Weights[1] && Rot == Lean.Right) {
         Rot = Lean.Transitioning;
         for (int i = 0; i < 40; i++) {
            Tippy.transform.Rotate(new Vector3(0, 0, .5f));
            Scales[0].transform.localPosition -= new Vector3(0.05f / 40, -0.47f / 40, 0);
            Scales[1].transform.localPosition -= new Vector3(-0.1f / 40, .46f / 40, 0);
            Scales[0].transform.Rotate(0, 0, -.5f);
            Scales[1].transform.Rotate(0, 0, -.5f);
            yield return null;
         }
         Rot = Lean.Center;
      }
   }

#pragma warning disable 414
   private readonly string TwitchHelpMessage = @"Use !{0} KBCGORWM to press that colored paperweight. Use !{0} left/right to press that side of the scale.";
#pragma warning restore 414

   IEnumerator ProcessTwitchCommand (string Command) {
      Command = Command.Trim().ToUpper();
      yield return null;
      if (!Command.Contains("KBCGORWM") && Command.Length != 1 && Command != "LEFT" && Command != "RIGHT") {
         yield return "sendtochaterror I don't understand!";
      }
      else if (Command.Length == 1) {
         string l = "KCYRBGWM";
         Balls[l.IndexOf(Command)].OnInteract();
      }
      else if (Command == "LEFT") {
         Bowls[0].OnInteract();
      }
      else {
         Bowls[1].OnInteract();
      }
   }

   IEnumerator TwitchHandleForcedSolve () {
      string l = "KCYRBGWM";
      for (int i = 0; i < 8; i++) {
         if (BallPlacement[i] != Placements.None) {
            yield return ProcessTwitchCommand(l[i].ToString());
            yield return new WaitForSeconds(.1f);
         }
      }
      Bowls[0].OnInteract();
      int Sum = Solution.Sum() / 2 - Solution[0];
      yield return ProcessTwitchCommand(l[0].ToString());
      yield return new WaitForSeconds(.1f);
      bool Stop = false;
      for (int i = 1; i < 6; i++) {
         for (int j = i + 1; j < 7; j++) {
            for (int k = j + 1; k < 8; k++) {
               if (Solution[i] + Solution[j] + Solution[k] == Sum) {
                  yield return ProcessTwitchCommand(l[i].ToString());
                  yield return new WaitForSeconds(.1f);
                  yield return ProcessTwitchCommand(l[j].ToString());
                  yield return new WaitForSeconds(.1f);
                  yield return ProcessTwitchCommand(l[k].ToString());
                  yield return new WaitForSeconds(.1f);
                  Stop = true;
               }
               if (Stop) {
                  break;
               }
            }
            if (Stop) {
               break;
            }
         }
         if (Stop) {
            break;
         }
      }

      Bowls[1].OnInteract();
      for (int i = 0; i < 8; i++) {
         if (BallPlacement[i] == Placements.None) {
            yield return ProcessTwitchCommand(l[i].ToString());
            yield return new WaitForSeconds(.1f);
         }
      }
   }
}
