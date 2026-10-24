using UnityEngine;

namespace CatOnTheSill
{
    /// <summary>Chooses the best thing to paw at, shows the contextual prompt and forwards key presses.</summary>
    public class InteractionSystem : MonoBehaviour
    {
        public static InteractionSystem I;
        public CatController cat;
        public Interactable Best { get; private set; }
        public MovableObject NearMovable { get; private set; }
        public Vector3? FocusPoint { get; private set; }
        public string CurrentPrompt { get; private set; } = "";

        void Awake() { I = this; }

        void Update()
        {
            if (cat == null) return;
            if (cat.Frozen || !Inp.Enabled)
            {
                Best = null; NearMovable = null; FocusPoint = null;
                SetPrompt("");
                return;
            }

            Vector3 c = cat.Center;
            Interactable best = null; float bestScore = float.MaxValue; Vector3 bestPt = Vector3.zero;
            var all = Interactable.All;
            for (int i = 0; i < all.Count; i++)
            {
                var it = all[i];
                if (it == null || !it.CanInteract) continue;
                Vector3 p = it.ClosestPoint(c);
                float d = Vector3.Distance(c, p);
                if (d > it.range) continue;
                Vector3 to = p - cat.transform.position; to.y = 0;
                float facing = to.sqrMagnitude > 0.0004f ? Vector3.Dot(to.normalized, cat.transform.forward) : 1f;
                if (it.needsFacing && facing < -0.1f && d > 0.35f) continue;
                float score = d - facing * 0.25f;
                if (score < bestScore) { bestScore = score; best = it; bestPt = p; }
            }
            Best = best;
            FocusPoint = best != null ? bestPt : (Vector3?)null;

            // closest draggable prop
            MovableObject nm = null; float nd = 0.7f;
            if (cat.Grabbed == null)
            {
                foreach (var m in MovableObject.All)
                {
                    if (m == null || !m.grabbable) continue;
                    float d = (m.ClosestPoint(c) - c).magnitude;
                    if (d < nd) { nd = d; nm = m; }
                }
            }
            NearMovable = nm;

            string prompt = "";
            if (cat.Grabbed != null) prompt = "Dragging the " + cat.Grabbed.displayName + " - move to pull or push (release F to let go)";
            else
            {
                if (best != null)
                {
                    prompt = "[E] " + best.GetPrompt();
                    var alt = best.GetAltPrompt();
                    if (!string.IsNullOrEmpty(alt)) prompt += "     [Q] " + alt;
                }
                if (nm != null) prompt += (prompt.Length > 0 ? "     " : "") + "[Hold F] Drag the " + nm.displayName;
                if (best == null && nm == null) prompt = "[E] Meow";
            }
            SetPrompt(prompt);

            if (best != null)
            {
                if (Inp.InteractDown) best.Interact(cat);
                else if (Inp.AltDown) best.Alt(cat);
            }
            else if (Inp.InteractDown) cat.Meow();
            if (Inp.MeowDown) cat.Meow();
        }

        void SetPrompt(string s)
        {
            CurrentPrompt = s;
            if (UIManager.I != null) UIManager.I.SetPrompt(s);
        }
    }
}
