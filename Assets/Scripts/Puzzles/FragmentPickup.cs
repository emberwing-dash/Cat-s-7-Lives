using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CatOnTheSill
{
    /// <summary>A floating soul fragment. Gentle to look at, magical to collect.</summary>
    public class FragmentPickup : Interactable
    {
        public int index;
        Vector3 basePos;
        Vector3 landPos;
        bool landing;
        float t, spawnT;
        bool collecting;
        Transform orb, ring1, ring2;
        Light lt;
        ParticleSystem sparks;
        Material orbMat;

        public static FragmentPickup Create(int index, Vector3 pos, Vector3? land)
        {
            var g = new GameObject("Fragment" + index);
            g.transform.position = pos;
            var f = g.AddComponent<FragmentPickup>();
            f.index = index;
            f.basePos = pos;
            if (land.HasValue) { f.landPos = land.Value; f.landing = true; }
            f.Build();
            return f;
        }

        void Build()
        {
            Color c = FragmentInfo.Colors[index];
            orbMat = Mats.Blend(new Color(c.r, c.g, c.b, 1f) * 1.2f, Mats.Soft, Mats.BlendKind.Additive);
            orb = B.Quad(transform, "Orb", Vector3.zero, Vector3.back, new Vector2(1, 1), orbMat).transform;
            orb.localScale = Vector3.one * 0.55f;
            // solid-looking core
            var core = B.Sphere(transform, "Core", Vector3.zero, 0.16f, Mats.Emissive(Color.Lerp(c, Color.white, 0.4f), 3f), Col.None, false);
            core.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var rm = Mats.Emissive(c, 1.4f);
            ring1 = B.Cyl(transform, "Ring1", Vector3.zero, 0.13f, 0.006f, rm, Col.None, false).transform;
            ring2 = B.Cyl(transform, "Ring2", Vector3.zero, 0.1f, 0.006f, rm, Col.None, false).transform;
            ring2.localRotation = Quaternion.Euler(80, 0, 0);
            var lg = new GameObject("Light"); lg.transform.SetParent(transform, false);
            lt = lg.AddComponent<Light>(); lt.type = LightType.Point; lt.color = c; lt.intensity = 2.5f; lt.range = 3.5f; lt.shadows = LightShadows.None;
            // sparkles
            var sg = new GameObject("Sparks"); sg.transform.SetParent(transform, false);
            sparks = sg.AddComponent<ParticleSystem>();
            sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = sparks.main; main.startLifetime = 1.6f; main.startSpeed = 0.15f; main.startSize = 0.03f; main.startColor = c; main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 50;
            var em = sparks.emission; em.rateOverTime = 14f;
            sparks.Play();
            var sh = sparks.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.2f;
            var col = sparks.colorOverLifetime; col.enabled = true;
            var gr = new Gradient(); gr.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.2f), new GradientAlphaKey(0, 1) });
            col.color = gr;
            var pr = sparks.GetComponent<ParticleSystemRenderer>(); pr.sharedMaterial = Mats.Blend(Color.white, Mats.Soft, Mats.BlendKind.Additive); pr.shadowCastingMode = ShadowCastingMode.Off;
            var trig = gameObject.AddComponent<SphereCollider>(); trig.isTrigger = true; trig.radius = 0.28f;
            prompt = "Gather the soul fragment";
            range = 0.9f; needsFacing = false;
            AudioManager.I?.Play("reveal", transform.position, 0.6f, 1.2f);
        }

        public override bool CanInteract => !collecting && isActiveAndEnabled;
        public override void Interact(CatController cat) { Collect(cat); }

        void Update()
        {
            if (collecting) return;
            t += Time.deltaTime; spawnT = Mathf.Min(1f, spawnT + Time.deltaTime * 1.2f);
            if (landing)
            {
                basePos = Vector3.MoveTowards(basePos, landPos, 0.35f * Time.deltaTime);
                if ((basePos - landPos).sqrMagnitude < 0.0001f) landing = false;
            }
            transform.position = basePos + Vector3.up * (Mathf.Sin(t * 1.8f) * 0.04f);
            float s = spawnT * (0.9f + 0.1f * Mathf.Sin(t * 3f));
            orb.localScale = Vector3.one * 0.6f * s;
            if (Camera.main != null) orb.rotation = Camera.main.transform.rotation;
            ring1.Rotate(0, 90 * Time.deltaTime, 40 * Time.deltaTime); ring2.Rotate(60 * Time.deltaTime, 0, 90 * Time.deltaTime);
            lt.intensity = 2.5f * s;

            var cat = CatController.I;
            if (cat != null && !landing && cat.DistanceTo(transform.position) < 0.42f) Collect(cat);
        }

        void Collect(CatController cat)
        {
            if (collecting) return;
            collecting = true;
            StartCoroutine(CollectRoutine(cat));
        }

        System.Collections.IEnumerator CollectRoutine(CatController cat)
        {
            Vector3 start = transform.position;
            AudioManager.I?.PlayFragment(index);
            for (float u = 0; u < 1f; u += Time.deltaTime / 0.8f)
            {
                float e = u * u * (3f - 2f * u);
                Vector3 target = cat != null ? cat.Center + Vector3.up * 0.25f : start;
                transform.position = Vector3.Lerp(start, target, e) + Vector3.up * Mathf.Sin(u * Mathf.PI) * 0.35f;
                orb.localScale = Vector3.one * Mathf.Lerp(0.6f, 0.25f, u);
                lt.intensity = Mathf.Lerp(2.5f, 6f, u);
                yield return null;
            }
            Game.Collect(index);
            Burst(transform.position);
            Destroy(gameObject);
        }

        public static void Burst(Vector3 p)
        {
            var g = new GameObject("Burst"); g.transform.position = p;
            var ps = g.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.duration = 1f; main.loop = false; main.startLifetime = 1.2f; main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.6f); main.startSize = 0.05f;
            main.startColor = new Color(1f, 0.95f, 0.7f); main.simulationSpace = ParticleSystemSimulationSpace.World; main.gravityModifier = -0.05f;
            var em = ps.emission; em.rateOverTime = 0; em.SetBursts(new[] { new ParticleSystem.Burst(0f, 60) });
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.1f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var gr = new Gradient(); gr.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
            col.color = gr;
            var pr = ps.GetComponent<ParticleSystemRenderer>(); pr.sharedMaterial = Mats.Blend(Color.white, Mats.Soft, Mats.BlendKind.Additive);
            ps.Play();
            Destroy(g, 2.5f);
        }
    }
}
