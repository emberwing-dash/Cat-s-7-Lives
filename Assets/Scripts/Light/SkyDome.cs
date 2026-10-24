using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CatOnTheSill
{
    /// <summary>Stars, moon disc and drifting clouds visible through the windows.</summary>
    public class SkyDome : MonoBehaviour
    {
        Transform moon;
        Material moonMat, starMat;
        Renderer moonR, starsR;
        readonly List<Transform> clouds = new List<Transform>();
        Material cloudMat;

        public void Build()
        {
            // moon
            moonMat = Mats.Emissive(new Color(0.85f, 0.9f, 1f), 1.6f);
            moon = B.Sphere(transform, "Moon", Vector3.zero, 46f, moonMat, Col.None, false).transform;
            moonR = moon.GetComponent<Renderer>();
            moonR.shadowCastingMode = ShadowCastingMode.Off;
            var halo = B.Quad(moon, "Halo", Vector3.zero, Vector3.back, new Vector2(1, 1), Mats.Blend(new Color(0.6f, 0.75f, 1f, 0.35f), Mats.Soft, Mats.BlendKind.Additive));
            halo.transform.localScale = new Vector3(3.2f, 3.2f, 1f);
            halo.transform.SetParent(moon, false);
            moonHalo = halo.transform;

            // stars: one mesh of tiny camera-agnostic quads facing the origin
            var rnd = new System.Random(77);
            int n = 520;
            var verts = new Vector3[n * 4]; var cols = new Color[n * 4]; var uvs = new Vector2[n * 4]; var tris = new int[n * 6];
            for (int i = 0; i < n; i++)
            {
                double az = rnd.NextDouble() * Math.PI * 2, el = Math.Asin(0.05 + rnd.NextDouble() * 0.95);
                Vector3 dir = new Vector3((float)(Math.Cos(el) * Math.Cos(az)), (float)Math.Sin(el), (float)(Math.Cos(el) * Math.Sin(az)));
                Vector3 c = dir * 420f;
                Vector3 r = Vector3.Cross(dir, Vector3.up).normalized, u = Vector3.Cross(r, dir).normalized;
                float s = 0.7f + (float)rnd.NextDouble() * 1.6f;
                float b = 0.35f + (float)rnd.NextDouble() * 0.65f;
                Color col = Color.Lerp(new Color(0.8f, 0.9f, 1f), new Color(1f, 0.92f, 0.8f), (float)rnd.NextDouble()) * b; col.a = b;
                int vi = i * 4;
                verts[vi] = c - r * s - u * s; verts[vi + 1] = c + r * s - u * s; verts[vi + 2] = c + r * s + u * s; verts[vi + 3] = c - r * s + u * s;
                uvs[vi] = new Vector2(0, 0); uvs[vi + 1] = new Vector2(1, 0); uvs[vi + 2] = new Vector2(1, 1); uvs[vi + 3] = new Vector2(0, 1);
                for (int k = 0; k < 4; k++) cols[vi + k] = col;
                int ti = i * 6;
                tris[ti] = vi; tris[ti + 1] = vi + 2; tris[ti + 2] = vi + 1; tris[ti + 3] = vi; tris[ti + 4] = vi + 3; tris[ti + 5] = vi + 2;
            }
            var mesh = new Mesh { name = "Stars", indexFormat = IndexFormat.UInt32 };
            mesh.vertices = verts; mesh.colors = cols; mesh.uv = uvs; mesh.triangles = tris;
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000f);
            var go = new GameObject("Stars");
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            starMat = Mats.Blend(Color.white, Mats.Soft, Mats.BlendKind.Additive);
            starsR = go.AddComponent<MeshRenderer>();
            starsR.sharedMaterial = starMat; starsR.shadowCastingMode = ShadowCastingMode.Off; starsR.receiveShadows = false;

            // clouds
            cloudMat = Mats.Lit(new Color(1f, 1f, 1f), 0f);
            for (int i = 0; i < 9; i++)
            {
                var root = new GameObject("Cloud" + i).transform;
                root.SetParent(transform, false);
                float a = i * 0.7f + 0.3f;
                root.position = new Vector3(Mathf.Cos(a) * 160f, 55f + (i % 3) * 8f, Mathf.Sin(a) * 160f);
                int puffs = 5 + i % 3;
                for (int p = 0; p < puffs; p++)
                {
                    var s = B.Sphere(root, "p", new Vector3((p - puffs / 2f) * 9f, UnityEngine.Random.Range(-1.5f, 2.5f), UnityEngine.Random.Range(-4f, 4f)), new Vector3(15f + p % 2 * 5f, 7f, 11f), cloudMat, Col.None, false);
                    s.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                }
                clouds.Add(root);
            }
        }

        Transform moonHalo;

        public void Apply(Vector3 toSun, bool moonUp, float night, Color sunCol)
        {
            moon.position = toSun * 380f + Vector3.zero;
            moon.gameObject.SetActive(moonUp);
            if (moonHalo != null) moonHalo.LookAt(Vector3.zero);
            starMat.SetColor("_Color", new Color(1, 1, 1, night));
            starsR.enabled = night > 0.01f;
            cloudMat.SetColor("_BaseColor", Color.Lerp(Color.white, new Color(0.18f, 0.2f, 0.32f), night));
        }

        void Update()
        {
            foreach (var c in clouds)
            {
                c.RotateAround(Vector3.zero, Vector3.up, 0.25f * Time.deltaTime);
            }
        }
    }
}
