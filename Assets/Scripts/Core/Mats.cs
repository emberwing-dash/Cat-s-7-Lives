using System.Collections.Generic;
using UnityEngine;

namespace CatOnTheSill
{
    /// <summary>Procedural materials, textures and meshes - the game ships no hand-authored art except characters.</summary>
    public static class Mats
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static readonly Dictionary<string, Texture2D> texCache = new Dictionary<string, Texture2D>();
        static Shader litShader, particleShader;

        public static Shader LitShader => litShader != null ? litShader : (litShader = Shader.Find("Universal Render Pipeline/Lit"));
        public static Shader Particle => particleShader != null ? particleShader : (particleShader = Shader.Find("CatOnTheSill/Particle"));

        public static void Clear() { cache.Clear(); }

        // ------------------------------------------------------------------ lit
        public static Material Lit(Color c, float smooth = 0.2f, float metal = 0f)
        {
            string key = $"L{ColorUtility.ToHtmlStringRGBA(c)}_{smooth:F2}_{metal:F2}";
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(LitShader) { name = key };
            SetLit(m, c, smooth, metal);
            cache[key] = m;
            return m;
        }

        static void SetLit(Material m, Color c, float smooth, float metal)
        {
            m.SetColor("_BaseColor", c);
            m.SetColor("_Color", c);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", metal);
        }

        public static Material Textured(Texture2D tex, Vector2 tiling, Color tint, float smooth = 0.2f, float metal = 0f)
        {
            var m = new Material(LitShader) { name = "Tex_" + tex.name };
            SetLit(m, tint, smooth, metal);
            m.SetTexture("_BaseMap", tex);
            m.SetTextureScale("_BaseMap", tiling);
            m.mainTexture = tex;
            m.mainTextureScale = tiling;
            return m;
        }

        public static Material Emissive(Color c, float intensity = 2f)
        {
            string key = $"E{ColorUtility.ToHtmlStringRGBA(c)}_{intensity:F1}";
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(LitShader) { name = key };
            SetLit(m, c * 0.4f, 0.3f, 0f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * intensity);
            cache[key] = m;
            return m;
        }

        public static Material Metal(Color c, float smooth = 0.85f) => Lit(c, smooth, 1f);

        /// <summary>Bright polished silver that still reads as silver in a dim room (partly metallic + a faint self glow).</summary>
        public static Material Silver(Color c)
        {
            string key = "S" + ColorUtility.ToHtmlStringRGBA(c);
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(LitShader) { name = key };
            SetLit(m, c, 0.92f, 0.55f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * 0.22f);
            cache[key] = m;
            return m;
        }

        public static Material Glass(Color tint)
        {
            return Blend(tint, Soft, BlendKind.Alpha);
        }

        // ------------------------------------------------------------------ unlit blended
        public enum BlendKind { Additive, Alpha }

        public static Material Blend(Color c, Texture tex, BlendKind kind)
        {
            var m = new Material(Particle) { name = "Blend" };
            m.SetColor("_Color", c);
            m.SetTexture("_MainTex", tex != null ? tex : Texture2D.whiteTexture);
            if (kind == BlendKind.Additive) { m.SetFloat("_Src", 5); m.SetFloat("_Dst", 1); }
            else { m.SetFloat("_Src", 5); m.SetFloat("_Dst", 10); }
            m.renderQueue = 3000;
            return m;
        }

        static Material sharedAdd;
        public static Material SharedAdditive(Texture tex = null)
        {
            if (tex == null)
            {
                if (sharedAdd == null) sharedAdd = Blend(Color.white, Soft, BlendKind.Additive);
                return sharedAdd;
            }
            return Blend(Color.white, tex, BlendKind.Additive);
        }

        // ------------------------------------------------------------------ meshes
        static Mesh cube, sphere, cylinder, capsule, quad, pyramid;

        static Mesh Prim(PrimitiveType t)
        {
            var g = GameObject.CreatePrimitive(t);
            var m = g.GetComponent<MeshFilter>().sharedMesh;
            if (Application.isPlaying) Object.Destroy(g); else Object.DestroyImmediate(g);
            return m;
        }

        public static Mesh Cube => cube != null ? cube : (cube = Prim(PrimitiveType.Cube));
        public static Mesh Sphere => sphere != null ? sphere : (sphere = Prim(PrimitiveType.Sphere));
        public static Mesh Cylinder => cylinder != null ? cylinder : (cylinder = Prim(PrimitiveType.Cylinder));
        public static Mesh Capsule => capsule != null ? capsule : (capsule = Prim(PrimitiveType.Capsule));
        public static Mesh Quad => quad != null ? quad : (quad = Prim(PrimitiveType.Quad));

        /// <summary>A small 4-sided pyramid (apex up, base 1x1 at y=0, height 1) - used for ears, cones, paperweights.</summary>
        public static Mesh Pyramid
        {
            get
            {
                if (pyramid != null) return pyramid;
                var m = new Mesh { name = "Pyramid" };
                Vector3 a = new Vector3(-0.5f, 0, -0.5f), b = new Vector3(0.5f, 0, -0.5f), c = new Vector3(0.5f, 0, 0.5f), d = new Vector3(-0.5f, 0, 0.5f), t = new Vector3(0, 1, 0);
                var v = new List<Vector3>(); var tris = new List<int>(); var uv = new List<Vector2>();
                void Tri(Vector3 p, Vector3 q, Vector3 r)
                {
                    int i = v.Count; v.Add(p); v.Add(q); v.Add(r); uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(0.5f, 1));
                    tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
                }
                Tri(a, t, b); Tri(b, t, c); Tri(c, t, d); Tri(d, t, a);
                Tri(a, b, c); Tri(a, c, d);
                m.SetVertices(v); m.SetTriangles(tris, 0); m.SetUVs(0, uv);
                m.RecalculateNormals(); m.RecalculateBounds();
                pyramid = m;
                return m;
            }
        }

        // ------------------------------------------------------------------ textures
        static Texture2D soft;
        public static Texture2D Soft
        {
            get
            {
                if (soft != null) return soft;
                const int n = 64;
                soft = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "Soft", wrapMode = TextureWrapMode.Clamp };
                var px = new Color[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float dx = (x + 0.5f) / n * 2 - 1, dy = (y + 0.5f) / n * 2 - 1;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01(1 - d); a = a * a * (3 - 2 * a);
                        px[y * n + x] = new Color(1, 1, 1, a);
                    }
                soft.SetPixels(px); soft.Apply();
                return soft;
            }
        }

        static Texture2D beamTex;
        /// <summary>1xN gradient used across a beam prism (soft edges).</summary>
        public static Texture2D BeamTex
        {
            get
            {
                if (beamTex != null) return beamTex;
                beamTex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "Beam" };
                beamTex.SetPixel(0, 0, Color.white); beamTex.Apply();
                return beamTex;
            }
        }

        public static Texture2D Pattern(string kind, Color a, Color b, int size = 256)
        {
            string key = $"{kind}_{ColorUtility.ToHtmlStringRGB(a)}_{ColorUtility.ToHtmlStringRGB(b)}";
            if (texCache.TryGetValue(key, out var t) && t != null) return t;
            t = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = key, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, anisoLevel = 4 };
            var px = new Color[size * size];
            var rnd = new System.Random(key.GetHashCode());
            switch (kind)
            {
                case "wood":
                {
                    int planks = 4; float[] tone = new float[planks * 4];
                    for (int i = 0; i < tone.Length; i++) tone[i] = (float)rnd.NextDouble();
                    for (int y = 0; y < size; y++)
                        for (int x = 0; x < size; x++)
                        {
                            int row = x * planks / size;               // plank column
                            int seg = ((y + row * 53) * 4 / size) % 4;  // staggered boards
                            float tn = tone[row * 4 + seg];
                            float grain = Mathf.PerlinNoise(x * 0.04f + row * 9f, y * 0.003f + tn * 7f) * 0.5f + Mathf.PerlinNoise(x * 0.4f, y * 0.01f) * 0.15f;
                            Color c = Color.Lerp(a, b, Mathf.Clamp01(tn * 0.5f + grain * 0.7f));
                            bool seamX = (x * planks % size) < planks;                    // seam between planks
                            bool seamY = ((y + row * 53) * 4 % size) < 4;                  // board end
                            if (seamX || seamY) c *= 0.55f;
                            px[y * size + x] = c;
                        }
                    break;
                }
                case "tile":
                {
                    int n = 4;
                    for (int y = 0; y < size; y++)
                        for (int x = 0; x < size; x++)
                        {
                            int tx = x * n / size, ty = y * n / size;
                            bool grout = (x * n % size) < 3 || (y * n % size) < 3;
                            Color c = ((tx + ty) % 2 == 0) ? a : b;
                            c *= 0.94f + 0.06f * Mathf.PerlinNoise(x * 0.1f, y * 0.1f);
                            px[y * size + x] = grout ? new Color(0.62f, 0.60f, 0.56f) : c;
                        }
                    break;
                }
                case "plaster":
                {
                    for (int y = 0; y < size; y++)
                        for (int x = 0; x < size; x++)
                        {
                            float n = Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 0.5f + Mathf.PerlinNoise(x * 0.3f, y * 0.3f) * 0.5f;
                            px[y * size + x] = Color.Lerp(a, b, n);
                        }
                    break;
                }
                case "stripes":
                {
                    for (int y = 0; y < size; y++)
                        for (int x = 0; x < size; x++)
                        {
                            bool s = (x * 8 / size) % 2 == 0;
                            Color c = s ? a : b;
                            c *= 0.97f + 0.03f * Mathf.PerlinNoise(x * 0.2f, y * 0.2f);
                            px[y * size + x] = c;
                        }
                    break;
                }
                case "rug":
                {
                    for (int y = 0; y < size; y++)
                        for (int x = 0; x < size; x++)
                        {
                            float fx = x / (float)size, fy = y / (float)size;
                            float edge = Mathf.Min(Mathf.Min(fx, 1 - fx), Mathf.Min(fy, 1 - fy));
                            Color c = a;
                            if (edge < 0.05f) c = b;
                            else if (edge < 0.08f) c = Color.Lerp(a, b, 0.4f);
                            else if (edge > 0.2f && edge < 0.23f) c = b;
                            float cx = fx - 0.5f, cy = fy - 0.5f;
                            float dd = Mathf.Sqrt(cx * cx + cy * cy);
                            if (dd < 0.12f && dd > 0.09f) c = b;
                            c *= 0.93f + 0.07f * Mathf.PerlinNoise(x * 0.3f, y * 0.3f);
                            px[y * size + x] = c;
                        }
                    break;
                }
                case "grass":
                {
                    for (int y = 0; y < size; y++)
                        for (int x = 0; x < size; x++)
                        {
                            float n = Mathf.PerlinNoise(x * 0.08f, y * 0.08f) * 0.6f + Mathf.PerlinNoise(x * 0.5f, y * 0.5f) * 0.4f;
                            px[y * size + x] = Color.Lerp(a, b, n);
                        }
                    break;
                }
                default:
                    for (int i = 0; i < px.Length; i++) px[i] = a;
                    break;
            }
            t.SetPixels(px); t.Apply(true);
            texCache[key] = t;
            return t;
        }

        // ------------------------------------------------------------------ glyph textures (signed distance field drawing)
        static float Circle(Vector2 p, Vector2 c, float r) => (p - c).magnitude - r;
        static float Box(Vector2 p, Vector2 c, Vector2 h) { var d = new Vector2(Mathf.Abs(p.x - c.x), Mathf.Abs(p.y - c.y)) - h; return Vector2.Max(d, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0); }
        static float Seg(Vector2 p, Vector2 a, Vector2 b, float w)
        {
            var pa = p - a; var ba = b - a; float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude - w;
        }
        static float Tri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float Cross(Vector2 u, Vector2 v) => u.x * v.y - u.y * v.x;
            float s1 = Cross(b - a, p - a), s2 = Cross(c - b, p - b), s3 = Cross(a - c, p - c);
            bool inside = (s1 >= 0 && s2 >= 0 && s3 >= 0) || (s1 <= 0 && s2 <= 0 && s3 <= 0);
            float d = Mathf.Min(Seg(p, a, b, 0), Mathf.Min(Seg(p, b, c, 0), Seg(p, c, a, 0)));
            return inside ? -d : d;
        }
        static float Ring(float d, float w) => Mathf.Abs(d) - w;

        static float GlyphSdf(GlyphKind k, Vector2 p)
        {
            // p in 0..1, y up
            switch (k)
            {
                case GlyphKind.Sun:
                {
                    float d = Circle(p, new Vector2(0.5f, 0.5f), 0.2f);
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * Mathf.PI / 4f; var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                        d = Mathf.Min(d, Seg(p, new Vector2(0.5f, 0.5f) + dir * 0.28f, new Vector2(0.5f, 0.5f) + dir * 0.44f, 0.03f));
                    }
                    return d;
                }
                case GlyphKind.Tree:
                {
                    float d = Circle(p, new Vector2(0.5f, 0.62f), 0.26f);
                    d = Mathf.Min(d, Circle(p, new Vector2(0.34f, 0.5f), 0.17f));
                    d = Mathf.Min(d, Circle(p, new Vector2(0.66f, 0.5f), 0.17f));
                    d = Mathf.Min(d, Box(p, new Vector2(0.5f, 0.2f), new Vector2(0.06f, 0.2f)));
                    return d;
                }
                case GlyphKind.Star:
                {
                    float d = 1;
                    Vector2 c = new Vector2(0.5f, 0.5f);
                    for (int i = 0; i < 5; i++)
                    {
                        float a0 = (90 + i * 72) * Mathf.Deg2Rad, a1 = (90 + (i + 1) * 72) * Mathf.Deg2Rad, am = (90 + i * 72 + 36) * Mathf.Deg2Rad;
                        Vector2 tip = c + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * 0.46f;
                        Vector2 l = c + new Vector2(Mathf.Cos(am - 0.0f), Mathf.Sin(am)) * 0.18f;
                        Vector2 l0 = c + new Vector2(Mathf.Cos(a0 - 36 * Mathf.Deg2Rad), Mathf.Sin(a0 - 36 * Mathf.Deg2Rad)) * 0.18f;
                        d = Mathf.Min(d, Tri(p, tip, l, l0));
                    }
                    d = Mathf.Min(d, Circle(p, c, 0.19f));
                    return d;
                }
                case GlyphKind.Circle: return Mathf.Min(Ring(Circle(p, new Vector2(0.5f, 0.5f), 0.36f), 0.05f), Circle(p, new Vector2(0.5f, 0.5f), 0.1f));
                case GlyphKind.Square: return Mathf.Min(Ring(Box(p, new Vector2(0.5f, 0.5f), new Vector2(0.3f, 0.3f)), 0.05f), Box(p, new Vector2(0.5f, 0.5f), new Vector2(0.09f, 0.09f)));
                case GlyphKind.Triangle:
                {
                    float t = Tri(p, new Vector2(0.5f, 0.88f), new Vector2(0.1f, 0.14f), new Vector2(0.9f, 0.14f));
                    return Mathf.Min(Ring(t, 0.05f), Circle(p, new Vector2(0.5f, 0.38f), 0.08f));
                }
                case GlyphKind.Moon:
                {
                    float a = Circle(p, new Vector2(0.5f, 0.5f), 0.38f);
                    float b = Circle(p, new Vector2(0.64f, 0.56f), 0.32f);
                    return Mathf.Max(a, -b);
                }
                case GlyphKind.Diamond:
                {
                    Vector2 q = new Vector2(Mathf.Abs(p.x - 0.5f), Mathf.Abs(p.y - 0.5f));
                    float d = (q.x + q.y) - 0.4f;
                    return Mathf.Min(Ring(d, 0.045f), (q.x + q.y) - 0.12f);
                }
                case GlyphKind.Paw:
                {
                    float d = Circle(p, new Vector2(0.5f, 0.32f), 0.2f);
                    d = Mathf.Min(d, Circle(p, new Vector2(0.22f, 0.6f), 0.1f));
                    d = Mathf.Min(d, Circle(p, new Vector2(0.4f, 0.78f), 0.1f));
                    d = Mathf.Min(d, Circle(p, new Vector2(0.6f, 0.78f), 0.1f));
                    d = Mathf.Min(d, Circle(p, new Vector2(0.78f, 0.6f), 0.1f));
                    return d;
                }
                case GlyphKind.Leaf:
                {
                    float d = Mathf.Max(Circle(p, new Vector2(0.2f, 0.2f), 0.7f), Circle(p, new Vector2(0.8f, 0.8f), 0.7f));
                    d = Mathf.Min(d, Seg(p, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f), 0.015f));
                    return d;
                }
                case GlyphKind.Eye:
                {
                    float a = Circle(p, new Vector2(0.5f, -0.1f), 0.62f), b = Circle(p, new Vector2(0.5f, 1.1f), 0.62f);
                    float d = Mathf.Max(a, b);
                    return Mathf.Min(Ring(d, 0.035f), Circle(p, new Vector2(0.5f, 0.5f), 0.12f));
                }
                case GlyphKind.Cat:
                {
                    float d = Circle(p, new Vector2(0.5f, 0.45f), 0.3f);
                    d = Mathf.Min(d, Tri(p, new Vector2(0.22f, 0.55f), new Vector2(0.2f, 0.92f), new Vector2(0.45f, 0.72f)));
                    d = Mathf.Min(d, Tri(p, new Vector2(0.78f, 0.55f), new Vector2(0.8f, 0.92f), new Vector2(0.55f, 0.72f)));
                    d = Mathf.Max(d, -Circle(p, new Vector2(0.4f, 0.5f), 0.045f));
                    d = Mathf.Max(d, -Circle(p, new Vector2(0.6f, 0.5f), 0.045f));
                    return d;
                }
                case GlyphKind.Ear: return Tri(p, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.05f), new Vector2(0.5f, 0.95f));
                case GlyphKind.Arrow:
                {
                    float d = Tri(p, new Vector2(0.5f, 0.95f), new Vector2(0.08f, 0.5f), new Vector2(0.92f, 0.5f));
                    return Mathf.Min(d, Box(p, new Vector2(0.5f, 0.28f), new Vector2(0.14f, 0.25f)));
                }
                case GlyphKind.Whisker:
                {
                    float d = Seg(p, new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.5f), 0.03f);
                    d = Mathf.Min(d, Seg(p, new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.65f), 0.03f));
                    d = Mathf.Min(d, Seg(p, new Vector2(0.1f, 0.65f), new Vector2(0.9f, 0.35f), 0.03f));
                    return d;
                }
                default: return Ring(Circle(p, new Vector2(0.5f, 0.5f), 0.4f), 0.05f);
            }
        }

        public static Texture2D Glyph(GlyphKind kind)
        {
            string key = "glyph_" + kind;
            if (texCache.TryGetValue(key, out var t) && t != null) return t;
            const int n = 128;
            t = new Texture2D(n, n, TextureFormat.RGBA32, true) { name = key, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    var p = new Vector2((x + 0.5f) / n, (y + 0.5f) / n);
                    float d = GlyphSdf(kind, p);
                    float a = Mathf.Clamp01(0.5f - d * n * 0.5f);
                    // faint soft halo helps the glow read from afar
                    float halo = Mathf.Clamp01(1f - d * 6f) * 0.12f;
                    px[y * n + x] = new Color(1, 1, 1, Mathf.Max(a, halo * (d > 0 ? 1 : 0)));
                }
            t.SetPixels(px); t.Apply(true);
            texCache[key] = t;
            return t;
        }

        public static void ResetSprites() { roundSprite = null; softSprite = null; }
        static Sprite roundSprite;
        public static Sprite RoundedSprite()
        {
            if (roundSprite != null) return roundSprite;
            const int n = 64; const float r = 20f;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, r, n - r), cy = Mathf.Clamp(y + 0.5f, r, n - r);
                    float d = new Vector2(x + 0.5f - cx, y + 0.5f - cy).magnitude - r;
                    px[y * n + x] = new Color(1, 1, 1, Mathf.Clamp01(0.5f - d));
                }
            t.SetPixels(px); t.Apply();
            roundSprite = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
            return roundSprite;
        }

        static Sprite softSprite;
        public static Sprite SoftSprite()
        {
            if (softSprite != null) return softSprite;
            softSprite = Sprite.Create(Soft, new Rect(0, 0, Soft.width, Soft.height), new Vector2(0.5f, 0.5f), 100);
            return softSprite;
        }
    }
}
