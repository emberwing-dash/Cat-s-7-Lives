using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CatOnTheSill
{
    /// <summary>
    /// Visual god-rays for a window: translucent prisms from each pane out to wherever the light lands, plus dust
    /// motes floating in them. The prisms are built by raycasting from the pane corners, so they always agree with
    /// the real shadows (and the puzzle logic, which uses the same rays).
    /// </summary>
    public class WindowBeam : MonoBehaviour
    {
        public Vector3 center;          // centre of the glass plane
        public Vector3 right, up, inward;
        public float width, height;
        public int cols = 1;
        public float bar = 0.05f;
        public float strength = 1f;
        public float[] splits;               // optional unequal pane boundaries (fractions across width)
        public bool hasTint; public Color tintColor;

        Mesh mesh;
        MeshRenderer mr;
        ParticleSystem dust;
        Material mat;
        float nextBuild;
        int lastVersion = -1, lastSunVersion = -1;
        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<Color> cols_ = new List<Color>();
        readonly List<int> tris = new List<int>();
        static readonly List<WindowBeam> all = new List<WindowBeam>();
        public static IReadOnlyList<WindowBeam> All => all;

        void Awake()
        {
            all.Add(this);
            mesh = new Mesh { name = "Beam", indexFormat = IndexFormat.UInt32 };
            mesh.MarkDynamic();
            var mf = gameObject.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            mr = gameObject.AddComponent<MeshRenderer>();
            mat = Mats.Blend(new Color(1f, 0.92f, 0.75f, 1f), Mats.Soft, Mats.BlendKind.Additive);
            mat.SetTexture("_MainTex", Texture2D.whiteTexture);
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.enabled = false;

            var go = new GameObject("Dust");
            go.transform.SetParent(transform, false);
            dust = go.AddComponent<ParticleSystem>();
            dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = dust.main;
            main.loop = true; main.playOnAwake = false; main.duration = 8f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.01f, 0.05f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.022f, 0.05f);
            main.startColor = new Color(1f, 0.95f, 0.8f, 0.85f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 160;
            main.gravityModifier = 0f;
            var em = dust.emission; em.rateOverTime = 8f;
            var sh = dust.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Box;
            var noise = dust.noise; noise.enabled = true; noise.strength = 0.08f; noise.frequency = 0.5f; noise.scrollSpeed = 0.15f;
            var col = dust.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.2f), new GradientAlphaKey(1, 0.8f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var rend = dust.GetComponent<ParticleSystemRenderer>();
            rend.sharedMaterial = Mats.Blend(Color.white, Mats.Soft, Mats.BlendKind.Additive);
            rend.shadowCastingMode = ShadowCastingMode.Off;
            rend.receiveShadows = false;
        }

        void OnDestroy() { all.Remove(this); }

        void Update()
        {
            if (Time.time < nextBuild) return;
            nextBuild = Time.time + 0.1f;
            if (lastVersion == WorldState.Version && lastSunVersion == SunLight.Version && !(TimeManager.I != null && TimeManager.I.Transitioning)) return;
            lastVersion = WorldState.Version; lastSunVersion = SunLight.Version;
            Rebuild();
        }

        public bool Active { get; private set; }
        public int DustCount => dust != null ? dust.particleCount : 0;

        public void Rebuild()
        {
            verts.Clear(); cols_.Clear(); tris.Clear();
            Vector3 L = SunLight.Travel;
            float facing = Vector3.Dot(L, inward);
            if (facing < 0.06f || SunLight.Strength < 0.05f)
            {
                SetActive(false);
                return;
            }
            float a0 = 0.24f * strength * Mathf.Lerp(0.5f, 1f, SunLight.Strength);
            float a1 = 0.07f * strength;
            int paneCount = splits != null ? splits.Length + 1 : cols;
            int built = 0;
            for (int c = 0; c < paneCount; c++)
            {
                float f0 = splits != null ? (c == 0 ? 0f : splits[c - 1]) : c / (float)cols;
                float f1 = splits != null ? (c == paneCount - 1 ? 1f : splits[c]) : (c + 1) / (float)cols;
                float u0 = -width * 0.5f + f0 * width + bar, u1 = -width * 0.5f + f1 * width - bar;
                float v0 = -height * 0.5f + bar, v1 = height * 0.5f - bar;
                Vector3[] g = new Vector3[4]
                {
                    center + right * u0 + up * v0 + inward * 0.03f,
                    center + right * u1 + up * v0 + inward * 0.03f,
                    center + right * u1 + up * v1 + inward * 0.03f,
                    center + right * u0 + up * v1 + inward * 0.03f
                };
                Vector3[] e = new Vector3[4];
                float far = 0f;
                for (int i = 0; i < 4; i++)
                {
                    if (Physics.Raycast(g[i], L, out var hit, 30f, Layer.OccluderMask, QueryTriggerInteraction.Ignore)) { e[i] = hit.point; far = Mathf.Max(far, hit.distance); }
                    else { e[i] = g[i] + L * 12f; far = 12f; }
                }
                if (far < 0.35f) continue;
                built++;
                int baseIdx = verts.Count;
                for (int i = 0; i < 4; i++) { verts.Add(g[i]); cols_.Add(new Color(1, 1, 1, a0)); }
                for (int i = 0; i < 4; i++) { verts.Add(e[i]); cols_.Add(new Color(1, 1, 1, a1)); }
                for (int i = 0; i < 4; i++)
                {
                    int j = (i + 1) % 4;
                    tris.Add(baseIdx + i); tris.Add(baseIdx + j); tris.Add(baseIdx + 4 + j);
                    tris.Add(baseIdx + i); tris.Add(baseIdx + 4 + j); tris.Add(baseIdx + 4 + i);
                }
            }
            if (built == 0) { SetActive(false); return; }
            mesh.Clear();
            mesh.SetVertices(verts); mesh.SetColors(cols_); mesh.SetTriangles(tris, 0);
            mesh.bounds = new Bounds(center, Vector3.one * 40f);
            var sc = SunLight.IsMoon ? new Color(0.55f, 0.7f, 1f, 1f) : new Color(1f, 0.93f, 0.78f, 1f);
            if (TimeManager.I != null) { var d = Times.Get(TimeManager.I.Target); sc = Color.Lerp(Color.white, d.sunColor, 0.65f); }
            if (hasTint) sc = Color.Lerp(sc, tintColor, 0.8f);
            mat.SetColor("_Color", sc);
            var sh = dust.shape; sh.shapeType = ParticleSystemShapeType.Mesh; sh.meshShapeType = ParticleSystemMeshShapeType.Triangle; sh.mesh = null; sh.mesh = mesh;
            var em = dust.emission; em.rateOverTime = 16f * built;
            SetActive(true);
        }

        void SetActive(bool on)
        {
            Active = on;
            mr.enabled = on;
            if (on) { if (!dust.isPlaying || dust.particleCount == 0 && dust.time > 1f) { dust.Clear(); dust.Play(); } }
            else { if (dust.isPlaying) dust.Stop(true, ParticleSystemStopBehavior.StopEmitting); }
        }
    }
}
