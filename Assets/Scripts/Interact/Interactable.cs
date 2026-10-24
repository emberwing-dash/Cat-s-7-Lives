using System.Collections.Generic;
using UnityEngine;

namespace CatOnTheSill
{
    /// <summary>Anything the cat can paw at. The InteractionSystem picks the best candidate each frame.</summary>
    public abstract class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();

        public string prompt = "Interact";
        public string altPrompt = null;
        public float range = 1.0f;
        public bool needsFacing = true;
        Collider[] cols;

        protected virtual void OnEnable() { All.Add(this); }
        protected virtual void OnDisable() { All.Remove(this); }

        public virtual bool CanInteract => isActiveAndEnabled;
        public virtual string GetPrompt() => prompt;
        public virtual string GetAltPrompt() => altPrompt;
        public abstract void Interact(CatController cat);
        public virtual void Alt(CatController cat) { }

        /// <summary>Closest point on this object to p (uses its colliders, falling back to its position).</summary>
        public virtual Vector3 ClosestPoint(Vector3 p)
        {
            if (cols == null) cols = GetComponentsInChildren<Collider>();
            bool any = false; Vector3 best = transform.position; float bd = float.MaxValue;
            foreach (var c in cols)
            {
                if (c == null || !c.enabled) continue;
                Vector3 q = c.ClosestPoint(p);
                float d = (q - p).sqrMagnitude;
                if (d < bd) { bd = d; best = q; any = true; }
            }
            return any ? best : transform.position;
        }
    }
}
