using UnityEngine;

namespace CatOnTheSill
{
    /// <summary>
    /// Works out the single next thing to do for a puzzle and where it is in the world, so the golden arrow can lead
    /// the player step by step. Text is deliberately tiny ("Pull this cord").
    /// </summary>
    public static class PuzzleGuide
    {
        public static string LastText = "";
        public static StepKind LastKind { get; private set; }
        public static GlyphKind LastGlyph { get; private set; } = GlyphKind.Square;
        public static bool LastNight { get; private set; }
        public static string LastHour { get; private set; } = "";
        public static Vector3 LastTarget { get; private set; }
        public static float LastRequestTime { get; private set; } = -100f;

        static readonly TimeOfDay[] Hours =
        {
            TimeOfDay.Morning, TimeOfDay.Sunset, TimeOfDay.Morning, TimeOfDay.Night, TimeOfDay.Midnight, TimeOfDay.Sunrise, TimeOfDay.Sunrise
        };

        static WorldRefs refs;
        static string text;
        static Vector3 target;
        static Vector3 cat;
        static StepKind kindNow;
        static GlyphKind glyphNow = GlyphKind.Square;
        static string hourNow = "";
        static bool nightNow;

        /// <summary>
        /// The puzzle to guide the player through right now. There is no fixed order: it is the unsolved puzzle in the
        /// room the cat is standing in, otherwise the nearest unsolved one. (The finale waits for the other six.)
        /// </summary>
        public static TimeOfDay RequiredHour(Puzzle p) => Hours[Mathf.Clamp(p.fragmentIndex, 0, 6)];

        public static Puzzle Pick(Vector3 catPos)
        {
            Puzzle best = null; float bestVol = float.MaxValue;
            foreach (var q in Puzzle.All)
            {
                if (q == null || q.Solved || Game.Has(q.fragmentIndex) || !q.InRoom(catPos)) continue;
                var sz = q.roomBounds.size; float vol = sz.x * sz.y * sz.z;   // smallest room wins (the attic sits inside the sunroom's bounds)
                if (vol < bestVol) { bestVol = vol; best = q; }
            }
            return best;
        }

        public static string UnsolvedRooms()
        {
            var names = new System.Collections.Generic.List<string>();
            foreach (var q in Puzzle.All)
                if (q != null && !q.Solved && !Game.Has(q.fragmentIndex) && !names.Contains(q.roomName)) names.Add(q.roomName);
            return string.Join(", ", names);
        }

        public static void Next(Puzzle p, Vector3 catPos, out string outText, out Vector3 outTarget)
        {
            cat = catPos; text = null; target = p.revealPos; kindNow = StepKind.Wait;
            int i = Mathf.Clamp(p.fragmentIndex, 0, 6);
            if (i == 6 || !HourStep(Hours[i]))
            {
                switch (i)
                {
                    case 0:
                        Move("books", "bed_books", p.revealPos + new Vector3(0.6f, -0.9f, 0.6f));
                        if (text == null) Fallback("Wait for the sunbeam to touch the carving");
                        break;
                    case 1:
                        if (!Cover("liv_blind0", "Follow the arrow to the blind cord, then press E to pull it"))
                            if (!Move("fern", "liv_plant", Sol("liv_plant_p2")))
                                if (!Move("crates", "liv_crates", Sol("liv_crates_p2")))
                                    Fallback("Wait: the shadows are filling the carvings");
                        break;
                    case 2:
                        if (!Move("tray", "kit_tray", Sol("kit_tray")))
                        {
                            kindNow = StepKind.Turn;
                            text = "Turn the tray (E / Q) until its light hits the star";
                            target = FindRotatablePosition("kit_tray", p.revealPos);
                        }
                        break;
                    case 3:
                        if (!Move("milk bottle", "study_milk", Sol("study_milk")))
                            Fallback("Keep the milk on the saucer");
                        break;
                    case 4:
                        if (!Cover("attic_shutters", "Follow the arrow to the shutter cord, then press E to pull it"))
                            if (!Move("basin", "attic_bowl", Sol("attic_ring")))
                                Fallback("Wait: the moon's shadow will reach the ring");
                        break;
                    case 5:
                        if (!Turn("sun_m1", "sun_m1_step"))
                            if (!Turn("sun_m2", "sun_m2_step"))
                                Fallback("Follow the gold beam");
                        break;
                    default:
                        if (!Move("fern", "read_plant", Sol("read_plant")))
                            if (!Move("lamp", "read_lamp", Sol("read_lamp")))
                                Fallback("Wait: the house is remembering");
                        break;
                }
            }
            outText = text; outTarget = target; LastText = text;
            LastKind = kindNow; LastGlyph = glyphNow; LastHour = hourNow; LastNight = nightNow;
            LastTarget = target;
            LastRequestTime = Time.unscaledTime;
        }
        public static void PointAt(Vector3 worldPosition, string hint, out string outText, out Vector3 outTarget)
        {
            text = hint;
            target = worldPosition; kindNow = StepKind.Wait; LastKind = StepKind.Wait;
            outText = text;
            outTarget = target;
            LastText = text;
            LastTarget = target;
            LastRequestTime = Time.unscaledTime;
        }



        // ---------------------------------------------------------------- steps (each returns true when it produced the step)
        static bool HourStep(TimeOfDay want)
        {
            if (TimeManager.Current == want) return false;
            SkyWindow best = null; float bd = float.MaxValue;
            foreach (var it in Interactable.All)
            {
                var w = it as SkyWindow; if (w == null) continue;
                float d = (w.transform.position - cat).sqrMagnitude;
                if (d < bd) { bd = d; best = w; }
            }
            if (best == null) return false;
            text = "Look out a window (E) and choose " + want;
            target = best.transform.position;
            kindNow = StepKind.Hour; hourNow = want.ToString(); nightNow = want == TimeOfDay.Night || want == TimeOfDay.Midnight;
            return true;
        }

        static bool Cover(string id, string label)
        {
            foreach (var it in Interactable.All)
            {
                var c = it as CordInteractable;
                if (c == null || c.cover == null || c.cover.id != id || c.cover.state != 0) continue;
                text = label; target = c.transform.position; kindNow = StepKind.Cord;
                return true;
            }
            return false;
        }

        static bool Turn(string rotId, string stepKey)
        {
            var w = WorldRefsSolutions(stepKey); if (!w.HasValue) return false;
            foreach (var it in Interactable.All)
            {
                var r = it as RotatableObject;
                if (r == null || r.id != rotId) continue;
                if (r.stepIndex == Mathf.RoundToInt(w.Value.x)) return false;
                text = "Turn the mirror (E / Q) until the beam hits the target";
                target = r.transform.position + Vector3.up * 0.9f; kindNow = StepKind.Turn;
                return true;
            }
            return false;
        }

        static Vector3 FindRotatablePosition(string id, Vector3 fallback)
        {
            foreach (var it in Interactable.All)
            {
                var r = it as RotatableObject;
                if (r != null && r.id == id) return r.transform.position + Vector3.up * 0.9f;
            }
            return fallback;
        }


        static bool Move(string label, string id, Vector3? dest)
        {
            if (!dest.HasValue) return false;
            MovableObject m = null;
            foreach (var o in MovableObject.All) if (o != null && o.id == id) { m = o; break; }
            if (m == null) return false;
            Vector3 a = m.transform.position;
            Vector3 d = dest.Value;
            if (Flat(a, d) < 0.3f) return false;
            Vector3 dt = new Vector3(d.x, Mathf.Max(d.y, 0f) + 0.5f, d.z);
            text = "Push the " + label + " onto the glowing ring";
            target = Flat(cat, a) > 2.4f ? m.CenterOfMass : dt;
            kindNow = StepKind.Push; glyphNow = GlyphFor(id);
            return true;
        }

        static GlyphKind GlyphFor(string id)
        {
            if (id.Contains("plant")) return GlyphKind.Leaf;
            if (id.Contains("lamp")) return GlyphKind.Star;
            if (id.Contains("bowl")) return GlyphKind.Circle;
            if (id.Contains("milk")) return GlyphKind.Triangle;
            return GlyphKind.Square;
        }

        static void Fallback(string t) { text = t; }

        static float Flat(Vector3 a, Vector3 b) { a.y = 0; b.y = 0; return Vector3.Distance(a, b); }

        static Vector3? Sol(string key) => WorldRefsSolutions(key);

        static Vector3? WorldRefsSolutions(string key)
        {
            if (refs == null) refs = Object.FindFirstObjectByType<WorldRefs>();
            if (refs == null) return null;
            var d = refs.Solutions;
            return d != null && d.TryGetValue(key, out var v) ? v : (Vector3?)null;
        }
    }
}










