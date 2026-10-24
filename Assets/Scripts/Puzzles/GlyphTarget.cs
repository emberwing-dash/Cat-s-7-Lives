using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CatOnTheSill
{
    public class GlyphTarget : MonoBehaviour
    {
        public static readonly List<GlyphTarget> All = new List<GlyphTarget>();

        public GlyphMode mode = GlyphMode.Light;
        public Vector3 normal = Vector3.up;
        public float radius = 0.12f;
        public GlyphKind kind = GlyphKind.Sun;
        public Color glowColor = new Color(1f, 0.8f, 0.35f);
        public Color etchColor = new Color(0.30f, 0.20f, 0.12f);
        public List<MovableObject> casters = new List<MovableObject>();       // accepted shadow casters (empty = any movable)
        public List<Collider> staticCasters = new List<Collider>();           // static things whose shadow counts
        public bool requireMoonOrSun = false;
        public bool onlyMoon, onlySun;
        public bool Satisfied { get; private set; }
        public float Fraction { get; private set; }
        public float Glow { get; private set; }
        public bool Visible = true;
        public int gateFragments;                                             // >0: inert until fragments 0..n-1 are all home

        [SerializeField] Renderer etchR, glowR;
        [SerializeField] Transform glowT, etchT;
        Material etchMat, glowMat;
        readonly Vector3[] samples = new Vector3[5];
        float nextEval;
        float pulse;

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public void Build(float size)
        {
            radius = size * 0.5f;
            var tex = Mats.Glyph(kind);
            etchMat = Mats.Blend(new Color(etchColor.r, etchColor.g, etchColor.b, 0.55f), tex, Mats.BlendKind.Alpha);
            etchMat.renderQueue = 2460;
            glowMat = Mats.Blend(new Color(glowColor.r, glowColor.g, glowColor.b, 0f), tex, Mats.BlendKind.Additive);
            var e = B.Quad(transform, "Etch", normal * 0.008f, normal, new Vector2(size, size), etchMat);
            var g = B.Quad(transform, "Glow", normal * 0.014f, normal, new Vector2(size * 1.25f, size * 1.25f), glowMat);
            etchT = e.transform; glowT = g.transform;
            etchR = e.GetComponent<Renderer>(); glowR = g.GetComponent<Renderer>();
            e.transform.localPosition = normal * 0.008f; g.transform.localPosition = normal * 0.014f;
            // orient the texture "up" along world-up when on a wall
            ComputeSamples();
        }

        void ComputeSamples()
        {
            Vector3 up = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > 0.9f ? Vector3.forward : Vector3.up;
            Vector3 r = Vector3.Cross(up, normal).normalized; Vector3 u = Vector3.Cross(normal, r).normalized;
            Vector3 c = transform.position;
            float k = radius * 0.62f;
            samples[0] = c;
            samples[1] = c + r * k; samples[2] = c - r * k; samples[3] = c + u * k; samples[4] = c - u * k;
        }

        public void Reposition(Vector3 pos) { transform.position = pos; ComputeSamples(); }

        bool Check(Vector3 p)
        {
            if (mode == GlyphMode.Light)
            {
                if (onlyMoon && !SunLight.IsMoon) return false;
                if (onlySun && SunLight.IsMoon) return false;
                return SunLight.IsLit(p, normal);
            }
            if (onlyMoon && !SunLight.IsMoon) return false;
            if (onlySun && SunLight.IsMoon) return false;
            return SunLight.ShadowedBy(p, normal, casters.Count > 0 ? casters : null, staticCasters);
        }

        bool GateOpen()
        {
            for (int i = 0; i < gateFragments; i++) if (!Game.Has(i)) return false;
            return true;
        }

        void Start()
        {
            // per-instance materials so the baked assets are never modified at run time
            if (etchR != null) etchMat = etchR.material;
            if (glowR != null) glowMat = glowR.material;
        }

        public bool Evaluate()
        {
            if (gateFragments > 0 && !GateOpen()) { Satisfied = false; Fraction = 0f; return false; }
            ComputeSamples();
            int ok = 0; bool center = false;
            for (int i = 0; i < samples.Length; i++) { if (Check(samples[i])) { ok++; if (i == 0) center = true; } }
            Fraction = ok / (float)samples.Length;
            Satisfied = center && ok >= 3;
            return Satisfied;
        }

        void Update()
        {
            if (Time.time >= nextEval) { nextEval = Time.time + 0.12f; if (TimeManager.I == null || TimeManager.I.Settled) Evaluate(); else { Satisfied = false; Fraction = 0; } }
            pulse += Time.deltaTime;
            float target = Satisfied ? 1f : Fraction * 0.35f;
            Glow = Mathf.MoveTowards(Glow, target, Time.deltaTime * 2.5f);
            if (glowMat != null)
            {
                float a = Glow * (0.85f + 0.15f * Mathf.Sin(pulse * 3f));
                var c = glowColor; c.a = a;
                glowMat.SetColor("_Color", c);
                glowT.localScale = Vector3.one * radius * 2.5f * (1f + 0.04f * Glow * Mathf.Sin(pulse * 2f));
            }
            if (etchMat != null)
            {
                var c = etchColor; c.a = Mathf.Lerp(0.55f, 0.18f, Glow);
                etchMat.SetColor("_Color", c);
            }
        }
    }
}
