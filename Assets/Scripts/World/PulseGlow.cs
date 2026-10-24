using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CatOnTheSill
{
    /// <summary>Gentle breathing glow for ornaments.</summary>
    public class PulseGlow : MonoBehaviour
    {
        Material m; Color baseCol; float seed;
        void Start() { var r = GetComponent<Renderer>(); m = r.material; baseCol = m.GetColor("_Color"); seed = Random.value * 6f; }
        void Update() { if (m == null) return; float k = 0.65f + 0.35f * Mathf.Sin(Time.time * 1.7f + seed); m.SetColor("_Color", new Color(baseCol.r, baseCol.g, baseCol.b, baseCol.a * k)); }
    }
}
