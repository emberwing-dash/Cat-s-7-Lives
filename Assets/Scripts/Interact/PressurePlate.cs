using System.Collections.Generic;
using UnityEngine;

namespace CatOnTheSill
{
    /// <summary>A floor plate. Pressed while the cat (or a movable prop) stands on it. Latches once pressed if asked.</summary>
    public class PressurePlate : MonoBehaviour, IPersist
    {
        public string id = "plate";
        public bool latch = true;
        public bool Pressed { get; private set; }
        public event System.Action<bool> Changed;
        public Transform visual;
        float lift;
        int inside;
        Vector3 visBase;

        public string PersistId => "plate:" + id;
        void OnEnable() { Game.Register(this); }
        void OnDisable() { Game.Unregister(this); }

        void Start() { if (visual != null) visBase = visual.localPosition; }

        void OnTriggerStay(Collider other)
        {
            if (other.GetComponentInParent<CatController>() != null || other.GetComponentInParent<MovableObject>() != null) SetPressed(true, true);
        }

        void SetPressed(bool p, bool fromStay)
        {
            if (Pressed == p) { occupied = fromStay ? 2 : occupied; return; }
            Pressed = p; Changed?.Invoke(p);
            if (p) { AudioManager.I?.Play("click", transform.position, 0.8f, 1f); Game.Save(); }
        }

        int occupied;
        void FixedUpdate()
        {
            if (!latch && Pressed)
            {
                if (occupied > 0) occupied--; else SetPressed(false, false);
            }
            else if (occupied > 0) occupied--;
        }

        void Update()
        {
            if (visual != null)
            {
                lift = Mathf.MoveTowards(lift, Pressed ? 1f : 0f, Time.deltaTime * 6f);
                visual.localPosition = visBase - new Vector3(0, 0.02f * lift, 0);
            }
        }

        public string Capture() => Pressed ? "1" : "0";
        public void Restore(string s) { if (s == "1") { Pressed = true; Changed?.Invoke(true); if (visual != null) visual.localPosition = visBase - new Vector3(0, 0.02f, 0); } }
    }
}
