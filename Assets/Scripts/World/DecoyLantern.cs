using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CatOnTheSill
{
    /// <summary>A decoy lantern: glows when a beam strikes it, but leads nowhere.</summary>
    public class DecoyLantern : MonoBehaviour
    {
        public LightSensor sensor;
        public Renderer glass;
        public Light lamp;
        float v; bool was; float lastToast = -100f;
        void Update()
        {
            bool lit = sensor != null && sensor.Lit;
            v = Mathf.MoveTowards(v, lit ? 1f : 0f, Time.deltaTime * 3f);
            if (glass != null) glass.material.SetColor("_EmissionColor", new Color(1f, 0.5f, 0.3f) * (0.1f + 3.5f * v));
            if (lamp != null) lamp.intensity = 2f * v;
            if (lit && !was && Time.time - lastToast > 12f)
            {
                lastToast = Time.time;
                AudioManager.I?.Play("dud", transform.position, 0.7f);
                Game.ShowToast("A false light - it flickers, then leads nowhere.");
            }
            was = lit;
        }
    }
}
