using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatOnTheSill
{
    /// <summary>
    /// A guided first-room tutorial (one step at a time, with a bobbing golden arrow in the world and a compass on the
    /// HUD), followed by a permanent "what to do next" objective that points at the next soul fragment.
    /// </summary>
    public class TutorialGuide : MonoBehaviour
    {
        class Step { public string title, body; public Func<bool> done; public Func<Vector3?> target; }

        readonly List<Step> steps = new List<Step>();
        int idx;
        Transform beacon;
        Vector3 startPos;
        float lookAccum, lastYaw, finishTimer;
        bool jumped, finished;
        MovableObject books;
        Puzzle p1;
        Transform desk, mark;

        static readonly string[] BestHour = { "Morning", "Sunset", "Morning", "Night", "Midnight", "Sunrise", "Sunrise" };

        public static TutorialGuide Begin(GameManager gm, bool skipSteps)
        {
            var t = gm.gameObject.AddComponent<TutorialGuide>();
            t.Setup(skipSteps);
            return t;
        }

        void Setup(bool skip)
        {
            var cat = CatController.I;
            startPos = cat.transform.position;
            lastYaw = GameManager.I.Rig.yaw;
            foreach (var m in MovableObject.All) if (m != null && m.id == "bed_books") books = m;
            foreach (var p in Puzzle.All) if (p != null && p.id == "p1") p1 = p;
            var dg = GameObject.Find("StudyDesk"); if (dg != null) desk = dg.transform;
            var mg = GameObject.Find("SunMark"); if (mg != null) mark = mg.transform;
            Vector3 deskPos = desk != null ? desk.position + Vector3.up * 0.95f : new Vector3(0.5f, 1.2f, 2.5f);
            Vector3 booksPos() => books != null ? books.transform.position + Vector3.up * 0.75f : deskPos;
            Vector3 markPos = mark != null ? mark.position + new Vector3(0.45f, 0.1f, 0f) : new Vector3(0.6f, 1.0f, 2.1f);

            steps.Add(new Step
            {
                title = "Move and explore",
                body = "WASD move  •  Mouse look  •  Space jump  •  E interact  •  F drag",
                done = () => Vector3.Distance(CatController.I.transform.position, startPos) > 2.5f
            });
            steps.Add(new Step
            {
                title = "Follow the morning light",
                body = "Move the books near the desk and watch the wall.",
                done = () => p1 != null && p1.Solved,
                target = () => booksPos()
            });
            steps.Add(new Step
            {
                title = "Collect the fragment",
                body = "Follow the glow.",
                done = () => Game.Has(0),
                target = () => p1 != null && p1.Fragment != null ? p1.Fragment.transform.position : markPos
            });

            if (skip || Game.Has(0) || Game.Data.tutorialDone) { finished = true; Game.Data.tutorialDone = true; }
            else Show();
        }

        void Show()
        {
            UIManager.I?.SetObjective(steps[idx].title, steps[idx].body);
            AudioManager.I?.Play2D("blip2", 0.4f);
        }

        void MakeBeacon()
        {
            var g = new GameObject("GuideArrow");
            var m = Mats.Emissive(new Color(1f, 0.82f, 0.3f), 2.5f);
            var cone = B.Pyramid(g.transform, "Cone", Vector3.zero, new Vector3(0.22f, 0.34f, 0.22f), m);
            cone.transform.localRotation = Quaternion.Euler(180f, 0, 0);
            cone.transform.localPosition = new Vector3(0, 0.34f, 0);
            var lg = new GameObject("L"); lg.transform.SetParent(g.transform, false);
            var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.color = new Color(1f, 0.85f, 0.4f); l.range = 2.5f; l.intensity = 2f;
            beacon = g.transform;
        }

        void Update()
        {
            var gm = GameManager.I;
            var cat = CatController.I;
            if (gm == null || cat == null || gm.Mode != GameMode.Playing) { if (beacon != null) beacon.gameObject.SetActive(false); return; }

            // the tutorial only covers the first room: once the cat walks away (or any fragment is found) the player is free
            if (!finished && (Game.Collected > 0 || (idx >= 3 && p1 != null && !p1.InRoom(cat.transform.position))))
            { finished = true; finishTimer = 0f; Game.Data.tutorialDone = true; }

            float yaw = gm.Rig.yaw;
            lookAccum += Mathf.Abs(Mathf.DeltaAngle(lastYaw, yaw)); lastYaw = yaw;
            if (!cat.Grounded) jumped = true;

            Vector3? target = null;
            if (!finished)
            {
                var s = steps[idx];
                    target = s.target?.Invoke();
                if (s.done())
                {
                    idx++;
                    if (idx >= steps.Count)
                    {
                        finished = true; Game.Data.tutorialDone = true;
                        UIManager.I?.SetObjective("Seven fragments remain", "Read the rooms. Press H only when you want a hint.");
                        finishTimer = 4f;
                        AudioManager.I?.Play2D("collect", 0.5f);
                    }
                    else Show();
                }
            }
            else
            {
                if (finishTimer > 0f) finishTimer -= Time.deltaTime;
                else UpdateObjective(out target);
            }

            if (target.HasValue)
            {
                if (beacon == null) MakeBeacon();
                beacon.gameObject.SetActive(true);
                beacon.position = target.Value + Vector3.up * (0.1f + Mathf.Sin(Time.time * 3f) * 0.08f);
                beacon.Rotate(0, 90f * Time.deltaTime, 0, Space.World);
            }
            else if (beacon != null) beacon.gameObject.SetActive(false);

            if (UIManager.I != null)
            {
                if (target.HasValue)
                {
                    Vector3 d = target.Value - cat.transform.position; float dist = d.magnitude; d.y = 0;
                    UIManager.I.SetCompass(true, -Vector3.SignedAngle(gm.Rig.PlanarForward, d, Vector3.up), dist);
                }
                else UIManager.I.SetCompass(false, 0, 0);
            }
        }

        /// <summary>After the tutorial: keep the objective mysterious; the world arrow appears only when the player asks for a hint.</summary>
        void UpdateObjective(out Vector3? target)
        {
            target = null;
            foreach (var p in Puzzle.All)
                if (p != null && p.Fragment != null && !Game.Has(p.fragmentIndex)
                    && (p.InRoom(CatController.I.transform.position) || Vector3.Distance(p.Fragment.transform.position, CatController.I.transform.position) < 7f))
                {
                    target = p.Fragment.transform.position;
                    UIManager.I?.SetObjective("COLLECT THE GLOWING ORB", "Walk into the fragment.");
                    return;
                }

            Puzzle next = PuzzleGuide.Pick(CatController.I.transform.position);
            if (next == null)
            {
                string left = PuzzleGuide.UnsolvedRooms();
                UIManager.I?.SetObjective(left.Length > 0 ? "SOUL FRAGMENTS" : "", left.Length > 0 ? "Any room, any order.\nStill waiting: " + left : "");
                return;
            }

            PuzzleGuide.Next(next, CatController.I.transform.position, out string instruction, out Vector3 guideTarget);
            UIManager.I?.SetObjective("SOUL FRAGMENT — " + next.roomName.ToUpperInvariant(), instruction + "\nH = show solution picture");
            target = guideTarget;
        }
    }
}










