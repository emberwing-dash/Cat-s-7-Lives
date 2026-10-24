using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CatOnTheSill
{
    /// <summary>Slides a gate up when its plate is pressed.</summary>
    public class SlidingGate : MonoBehaviour
    {
        public PressurePlate plate;
        public float lift = 1.5f;
        Vector3 baseP; float v;
        void Start() { baseP = transform.position; if (plate != null) plate.Changed += OnPlate; }
        void OnPlate(bool p) { AudioManager.I?.Play("open", transform.position, 0.7f, 1.2f); Game.ShowToast("Somewhere, a latch clicks. The little gate rises."); }
        void Update()
        {
            v = Mathf.MoveTowards(v, plate != null && plate.Pressed ? 1f : 0f, Time.deltaTime * 0.8f);
            transform.position = baseP + Vector3.up * (lift * Mathf.SmoothStep(0, 1, v));
            if (v > 0.02f && v < 0.98f) Physics.SyncTransforms();
        }
    }
}
