using System.Collections.Generic;
using UnityEngine;

namespace CatOnTheSill
{
    /// <summary>Paw at it to turn a mirror / tray in 15 degree steps.</summary>
    public class RotatableObject : Interactable, IPersist
    {
        public string id = "rot";
        public Transform pivot;
        public float stepDeg = 15f;
        public int stepIndex;
        public string label = "mirror";
        float curYaw, targetYaw;
        float baseYaw;

        public string PersistId => "rot:" + id;
        public float Yaw => targetYaw;

        protected override void OnEnable() { base.OnEnable(); Game.Register(this); }
        protected override void OnDisable() { base.OnDisable(); Game.Unregister(this); }

        void Start()
        {
            if (pivot == null) pivot = transform;
            baseYaw = 0f;
            curYaw = targetYaw = baseYaw + stepIndex * stepDeg;
            Apply();
            prompt = "Turn the " + label;
            altPrompt = "Turn back";
        }

        public void SetStep(int i, bool instant = true)
        {
            stepIndex = ((i % Steps) + Steps) % Steps;
            targetYaw = baseYaw + stepIndex * stepDeg;
            if (instant) { curYaw = targetYaw; Apply(); WorldState.Dirty(); }
        }

        public int Steps => Mathf.RoundToInt(360f / stepDeg);

        public override void Interact(CatController cat) { SetStep(stepIndex + 1, false); AudioManager.I?.Play("scrape", transform.position, 0.5f, 1.2f); }
        public override void Alt(CatController cat) { SetStep(stepIndex - 1, false); AudioManager.I?.Play("scrape", transform.position, 0.5f, 0.9f); }

        void Update()
        {
            if (Mathf.Abs(Mathf.DeltaAngle(curYaw, targetYaw)) > 0.05f)
            {
                curYaw = Mathf.MoveTowardsAngle(curYaw, targetYaw, 260f * Time.deltaTime);
                Apply();
                Physics.SyncTransforms();
                WorldState.Dirty();
            }
        }

        void Apply() { pivot.localRotation = Quaternion.Euler(0, curYaw, 0); }

        public string Capture() => stepIndex.ToString();
        public void Restore(string s) { if (int.TryParse(s, out int i)) SetStep(i, true); }
    }
}
