using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CatOnTheSill
{
    /// <summary>Base class of the seven fragment puzzles. A puzzle watches the world, latches when solved and reveals its fragment.</summary>
    public abstract class Puzzle : MonoBehaviour, IPersist
    {
        public static readonly List<Puzzle> All = new List<Puzzle>();

        public string id;
        public int fragmentIndex;
        public string title;
        public string roomName;
        public Bounds roomBounds;                  // used to know whether the cat is "in" this puzzle's room
        public string[] hints = new string[3];
        public Vector3 revealPos;                  // where the fragment appears
        public bool hasLand; public Vector3 landPosition;
        public OpenableCabinet cabinet;            // opened when this puzzle latches (kitchen)
        public Vector3? landPos                    // optional spot the fragment floats down to (so the cat can reach it)
        {
            get => hasLand ? landPosition : (Vector3?)null;
            set { hasLand = value.HasValue; if (hasLand) landPosition = value.Value; }
        }
        public float requiredHold = 0.7f;
        public bool autoReveal = true;

        public bool Solved { get; protected set; }
        public float Closeness { get; protected set; }
        public FragmentPickup Fragment { get; protected set; }
        public float TimeInRoom { get; set; }
        public int HintsShown { get; set; }
        public event Action<Puzzle> OnSolved;
        public Action<bool> OnLatchedAction;          // extra consequence hooked up by the room builder

        float hold, lastClose;

        public string PersistId => "puzzle:" + id;

        protected virtual void Awake() { All.Add(this); Game.Register(this); }
        protected virtual void OnDestroy() { All.Remove(this); Game.Unregister(this); }

        /// <summary>Is the winning condition currently true? closeness is 0..1 progress toward it.</summary>
        protected abstract bool Evaluate(out float closeness);
        /// <summary>Visual/mechanical consequence of solving (open the cabinet, grow a flower...).</summary>
        protected virtual void OnLatched(bool animated) { }

        protected virtual void Update()
        {
            if (Solved) return;
            if (TimeManager.I != null && !TimeManager.I.Settled) return;
            bool ok = Evaluate(out float c);
            Closeness = c;
            if (c > lastClose + 0.04f && IsCatNear()) AudioManager.I?.Proximity(c);
            lastClose = Mathf.Lerp(lastClose, c, 0.02f);
            if (c > lastClose) lastClose = c;
            if (ok) { hold += Time.deltaTime; if (hold >= requiredHold) Solve(); }
            else hold = Mathf.Max(0, hold - Time.deltaTime * 2f);
        }

        bool IsCatNear()
        {
            var cat = CatController.I;
            if (cat == null) return false;
            return roomBounds.size != Vector3.zero ? roomBounds.Contains(cat.transform.position) : true;
        }

        public void Solve()
        {
            if (Solved) return;
            Solved = true;
            Closeness = 1f;
            Debug.Log("[Puzzle] solved: " + id);
            OnLatched(true);
            OnLatchedAction?.Invoke(true);
            if (cabinet != null) cabinet.Open(true);
            Game.RaisePuzzleSolved(this);
            Game.ShowToast("Something stirs...  " + title);
            AudioManager.I?.Play2D("reveal", 0.7f);
            if (autoReveal) SpawnFragment();
            OnSolved?.Invoke(this);
        }

        public void SpawnFragment()
        {
            if (Fragment != null || Game.Has(fragmentIndex)) return;
            Fragment = FragmentPickup.Create(fragmentIndex, revealPos, landPos);
        }

        public string Capture() => Solved ? "1" : "0";

        public void Restore(string s)
        {
            if (s != "1") return;
            Solved = true; Closeness = 1f;
            OnLatched(false);
            OnLatchedAction?.Invoke(false);
            if (cabinet != null) cabinet.Open(false);
            if (autoReveal && !Game.Has(fragmentIndex)) SpawnFragment();
        }

        public bool InRoom(Vector3 p) => roomBounds.size != Vector3.zero && roomBounds.Contains(p);
    }
}
