using System.Collections;
using UnityEngine;

namespace CatOnTheSill
{
    /// <summary>Gentle optional hints: after lingering in a room with an unsolved puzzle, a thought drifts by. H repeats the latest.</summary>
    public class HintSystem : MonoBehaviour
    {
        public float[] thresholds = { 40f, 100f, 180f };
        Puzzle lastRoomPuzzle;

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || gm.Mode != GameMode.Playing) return;
            var cat = CatController.I;
            if (cat == null) return;
            Puzzle candidate = null;
            foreach (var p in Puzzle.All)
            {
                if (p == null || p.Solved || Game.Has(p.fragmentIndex)) continue;
                if (!p.InRoom(cat.transform.position)) continue;
                var psz = p.roomBounds.size; var csz = candidate != null ? candidate.roomBounds.size : Vector3.zero;
                if (candidate == null || psz.x * psz.y * psz.z < csz.x * csz.y * csz.z) candidate = p;
            }

            lastRoomPuzzle = candidate;
            if (candidate != null)
            {
                PuzzleGuide.Next(candidate, cat.transform.position, out string objectiveStep, out Vector3 _);
                UIManager.I?.SetObjective("SOUL FRAGMENT — " + candidate.roomName.ToUpperInvariant(), objectiveStep + "\nH = show solution picture");
                candidate.TimeInRoom += Time.deltaTime;
                if (candidate.HintsShown < candidate.hints.Length && candidate.HintsShown < thresholds.Length && candidate.TimeInRoom >= thresholds[candidate.HintsShown])
                {
                    if (UIManager.I != null && UIManager.I.HintsEnabled)
                    {
                        PuzzleGuide.Next(candidate, cat.transform.position, out string st, out Vector3 _);
                        UIManager.I.ShowHint(st);
                    }
                    candidate.HintsShown++;
                }
            }
        }

        public void ShowHintNow()
        {
            var ui = UIManager.I; if (ui == null) return;
            var p = lastRoomPuzzle;
            if (p == null)
            {
                string left = PuzzleGuide.UnsolvedRooms();
                ui.ShowToast(left.Length > 0 ? "Walk into any room - H shows its puzzle.  Still waiting: " + left : "Nothing left to wonder about here.");
                return;
            }
            PuzzleGuide.Next(p, CatController.I.transform.position, out string step, out Vector3 target);
            ui.ShowSolutionPicture(p.fragmentIndex, step);
            GameManager.I?.Rig?.FocusHint(target);
        }

        public void Tutorial() { StopAllCoroutines(); StartCoroutine(TutorialRoutine()); }

        IEnumerator TutorialRoutine()
        {
            yield return new WaitForSeconds(6f);
            UIManager.I?.ShowToast("E interact  •  F drag  •  H hint  •  M map");
        }
    }
}







