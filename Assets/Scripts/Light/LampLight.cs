using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CatOnTheSill
{
    /// <summary>A warm indoor lamp that glows when the day is dim.</summary>
    public class LampLight : MonoBehaviour
    {
        public Light lightSource;
        public Renderer bulb;
        public float maxIntensity = 1.6f;
        public Color glow = new Color(1f, 0.78f, 0.5f);
        public float emissionBoost = 3f;
        MaterialPropertyBlock mpb;

        public static readonly List<LampLight> All = new List<LampLight>();
        void OnEnable() { All.Add(this); if (TimeManager.I != null) TimeManager.I.RegisterLamp(this); }
        void OnDisable() { All.Remove(this); if (TimeManager.I != null) TimeManager.I.UnregisterLamp(this); }

        public void Apply(float level)
        {
            if (lightSource != null) { lightSource.intensity = maxIntensity * level; lightSource.enabled = level > 0.01f; }
            if (bulb != null)
            {
                if (mpb == null) mpb = new MaterialPropertyBlock();
                bulb.GetPropertyBlock(mpb);
                mpb.SetColor("_EmissionColor", glow * (0.05f + emissionBoost * level));
                bulb.SetPropertyBlock(mpb);
            }
        }
    }
}
