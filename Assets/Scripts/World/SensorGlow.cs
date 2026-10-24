using System.Collections.Generic;
using UnityEngine;

namespace CatOnTheSill
{
    /// <summary>Brightens a decorative glow while a LightSensor is being hit.</summary>
    public class SensorGlow : MonoBehaviour
    {
        public LightSensor sensor;
        public MeshRenderer glowRenderer;
        public Color color = Color.white;
        float v;
        void Update()
        {
            v = Mathf.MoveTowards(v, sensor != null && sensor.Lit ? 1f : 0f, Time.deltaTime * 3f);
            if (glowRenderer != null) glowRenderer.material.SetColor("_Color", new Color(color.r, color.g, color.b, v * (0.8f + 0.2f * Mathf.Sin(Time.time * 6f))));
        }
    }
}
