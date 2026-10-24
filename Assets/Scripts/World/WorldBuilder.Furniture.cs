using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CatOnTheSill
{
    public partial class WorldBuilder
    {
        static readonly Color[] BookColors =
        {
            new Color(0.75f, 0.25f, 0.25f), new Color(0.25f, 0.45f, 0.70f), new Color(0.30f, 0.60f, 0.35f), new Color(0.85f, 0.70f, 0.30f),
            new Color(0.55f, 0.35f, 0.65f), new Color(0.90f, 0.55f, 0.25f), new Color(0.2f, 0.5f, 0.55f), new Color(0.8f, 0.8f, 0.75f)
        };

        Material LampShadeMat()
        {
            var m = new Material(Mats.LitShader) { name = "LampShade" };
            m.SetColor("_BaseColor", new Color(0.97f, 0.91f, 0.78f)); m.SetFloat("_Smoothness", 0.1f);
            m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.black);
            return m;
        }

        Transform Group(string name, Vector3 pos, float yaw = 0f, bool climbable = false)
        {
            var g = new GameObject(name).transform;
            g.SetParent(root, false);
            g.position = pos; g.rotation = Quaternion.Euler(0, yaw, 0);
            if (climbable) g.gameObject.AddComponent<Climbable>();
            return g;
        }

        GameObject Part(Transform parent, string name, Vector3 localPos, Vector3 size, Material m, bool collide = true)
            => B.Box(parent, name, localPos, size, m, collide, true);

        // ------------------------------------------------------------------------------------------ tables / desks
        public Transform Table(string name, Vector3 pos, float yaw, float w, float h, float d, Material top, Material leg, bool collideLegs = true)
        {
            var g = Group(name, pos, yaw, true);
            Part(g, "Top", new Vector3(0, h - 0.03f, 0), new Vector3(w, 0.06f, d), top);
            float lx = w * 0.5f - 0.06f, lz = d * 0.5f - 0.06f;
            for (int i = 0; i < 4; i++)
                Part(g, "Leg", new Vector3((i % 2 == 0 ? -1 : 1) * lx, (h - 0.06f) * 0.5f, (i < 2 ? -1 : 1) * lz), new Vector3(0.07f, h - 0.06f, 0.07f), leg, collideLegs);
            return g;
        }

        public Transform Desk(string name, Vector3 pos, float yaw, float w, float d, float h = 0.75f)
        {
            var g = Group(name, pos, yaw, true);
            Part(g, "Top", new Vector3(0, h - 0.03f, 0), new Vector3(w, 0.06f, d), mWoodLight);
            Part(g, "SideL", new Vector3(-w * 0.5f + 0.04f, (h - 0.06f) * 0.5f, 0), new Vector3(0.06f, h - 0.06f, d - 0.06f), mWood);
            Part(g, "SideR", new Vector3(w * 0.5f - 0.04f, (h - 0.06f) * 0.5f, 0), new Vector3(0.06f, h - 0.06f, d - 0.06f), mWood);
            Part(g, "Back", new Vector3(0, (h - 0.06f) * 0.5f + 0.1f, -d * 0.5f + 0.03f), new Vector3(w - 0.1f, h - 0.4f, 0.04f), mWood, false);
            // drawer block
            var dr = Part(g, "Drawers", new Vector3(w * 0.5f - 0.32f, 0.33f, 0), new Vector3(0.5f, 0.55f, d - 0.12f), mWood, false);
            for (int i = 0; i < 3; i++) Part(g, "Knob", new Vector3(w * 0.5f - 0.32f, 0.12f + i * 0.17f, d * 0.5f - 0.04f), new Vector3(0.05f, 0.025f, 0.025f), mBrass, false);
            return g;
        }

        public Transform Chair(string name, Vector3 pos, float yaw, Material m = null)
        {
            m = m ?? mWood;
            var g = Group(name, pos, yaw, true);
            Part(g, "Seat", new Vector3(0, 0.43f, 0), new Vector3(0.44f, 0.05f, 0.44f), m);
            Part(g, "Cushion", new Vector3(0, 0.47f, 0), new Vector3(0.4f, 0.04f, 0.4f), mFabric1, false);
            Part(g, "Back", new Vector3(0, 0.75f, -0.2f), new Vector3(0.44f, 0.5f, 0.05f), m);
            for (int i = 0; i < 4; i++) Part(g, "Leg", new Vector3((i % 2 == 0 ? -1 : 1) * 0.19f, 0.2f, (i < 2 ? -1 : 1) * 0.19f), new Vector3(0.05f, 0.4f, 0.05f), m);
            return g;
        }

        public Transform Stool(string name, Vector3 pos, float h = 0.42f, Material m = null)
        {
            m = m ?? mWoodLight;
            var g = Group(name, pos, 0, true);
            B.Cyl(g, "Seat", new Vector3(0, h, 0), 0.2f, 0.06f, m, Col.Box);
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f * Mathf.Deg2Rad + 0.78f;
                Part(g, "Leg", new Vector3(Mathf.Cos(a) * 0.13f, h * 0.5f, Mathf.Sin(a) * 0.13f), new Vector3(0.05f, h, 0.05f), m, false);
            }
            return g;
        }

        // ------------------------------------------------------------------------------------------ seating & beds
        public Transform Bed(string name, Vector3 pos, float yaw, float w = 1.7f, float len = 1.95f)
        {
            var g = Group(name, pos, yaw, true);
            Part(g, "Frame", new Vector3(0, 0.22f, 0), new Vector3(w, 0.3f, len), mWood);
            Part(g, "Mattress", new Vector3(0, 0.47f, 0.03f), new Vector3(w - 0.08f, 0.2f, len - 0.1f), mWhite);
            Part(g, "Blanket", new Vector3(0, 0.575f, 0.38f), new Vector3(w - 0.04f, 0.04f, len * 0.6f), mFabric2);
            Part(g, "Pillow1", new Vector3(-w * 0.25f, 0.62f, -len * 0.5f + 0.3f), new Vector3(0.55f, 0.12f, 0.35f), mWhite, false);
            Part(g, "Pillow2", new Vector3(w * 0.25f, 0.62f, -len * 0.5f + 0.3f), new Vector3(0.55f, 0.12f, 0.35f), mWhite, false);
            Part(g, "Headboard", new Vector3(0, 0.62f, -len * 0.5f - 0.02f), new Vector3(w + 0.1f, 0.95f, 0.08f), mWoodDark);
            Part(g, "Footboard", new Vector3(0, 0.38f, len * 0.5f + 0.02f), new Vector3(w + 0.1f, 0.55f, 0.07f), mWoodDark);
            return g;
        }

        public Transform Sofa(string name, Vector3 pos, float yaw, float len = 2.2f, Material m = null)
        {
            m = m ?? mFabric2;
            var g = Group(name, pos, yaw, true);
            Part(g, "Base", new Vector3(0, 0.22f, 0), new Vector3(len, 0.34f, 0.9f), m);
            Part(g, "Back", new Vector3(0, 0.62f, -0.36f), new Vector3(len, 0.55f, 0.2f), m);
            Part(g, "ArmL", new Vector3(-len * 0.5f + 0.1f, 0.45f, 0), new Vector3(0.2f, 0.3f, 0.9f), m);
            Part(g, "ArmR", new Vector3(len * 0.5f - 0.1f, 0.45f, 0), new Vector3(0.2f, 0.3f, 0.9f), m);
            var cm = Mats.Lit(Color.Lerp(m.GetColor("_BaseColor"), Color.white, 0.18f), 0.05f);
            int n = Mathf.Max(2, Mathf.RoundToInt(len / 0.7f));
            for (int i = 0; i < n; i++)
                Part(g, "Cushion", new Vector3(-len * 0.5f + 0.2f + (len - 0.4f) * (i + 0.5f) / n, 0.44f, 0.08f), new Vector3((len - 0.4f) / n - 0.03f, 0.12f, 0.68f), cm, false);
            Part(g, "Pillow", new Vector3(-len * 0.5f + 0.4f, 0.62f, -0.14f), new Vector3(0.36f, 0.34f, 0.12f), mFabric1, false).transform.localRotation = Quaternion.Euler(-10, 15, 0);
            return g;
        }

        public Transform Armchair(string name, Vector3 pos, float yaw, Material m = null)
        {
            m = m ?? mFabric3;
            var g = Group(name, pos, yaw, true);
            Part(g, "Base", new Vector3(0, 0.22f, 0), new Vector3(0.85f, 0.34f, 0.85f), m);
            Part(g, "Back", new Vector3(0, 0.62f, -0.33f), new Vector3(0.85f, 0.55f, 0.2f), m);
            Part(g, "ArmL", new Vector3(-0.35f, 0.45f, 0), new Vector3(0.18f, 0.3f, 0.85f), m);
            Part(g, "ArmR", new Vector3(0.35f, 0.45f, 0), new Vector3(0.18f, 0.3f, 0.85f), m);
            Part(g, "Cushion", new Vector3(0, 0.44f, 0.05f), new Vector3(0.5f, 0.12f, 0.6f), mFabric1, false);
            return g;
        }

        // ------------------------------------------------------------------------------------------ storage
        public Transform Bookshelf(string name, Vector3 pos, float yaw, float w, float h, float d, int shelves, int seed = 1, bool climbable = true)
        {
            var g = Group(name, pos, yaw, climbable);
            Part(g, "Body", new Vector3(0, h * 0.5f, 0.0f), new Vector3(w, h, d - 0.04f), mWoodDark);   // solid collider volume
            // open front illusion: shelf boards & books slightly proud of the body
            var rnd = new System.Random(seed);
            for (int s = 0; s <= shelves; s++)
                Part(g, "Board", new Vector3(0, 0.05f + (h - 0.1f) * s / shelves, d * 0.5f - 0.02f), new Vector3(w, 0.04f, 0.06f), mWood, false);
            Part(g, "SideL", new Vector3(-w * 0.5f + 0.02f, h * 0.5f, d * 0.5f - 0.02f), new Vector3(0.04f, h, 0.06f), mWood, false);
            Part(g, "SideR", new Vector3(w * 0.5f - 0.02f, h * 0.5f, d * 0.5f - 0.02f), new Vector3(0.04f, h, 0.06f), mWood, false);
            for (int s = 0; s < shelves; s++)
            {
                float y0 = 0.07f + (h - 0.1f) * s / shelves;
                float x = -w * 0.5f + 0.06f;
                float rowH = (h - 0.1f) / shelves - 0.1f;
                while (x < w * 0.5f - 0.14f)
                {
                    float bw = 0.035f + (float)rnd.NextDouble() * 0.045f;
                    float bh = Mathf.Clamp(rowH * (0.6f + (float)rnd.NextDouble() * 0.4f), 0.12f, 0.36f);
                    var bm = Mats.Lit(BookColors[rnd.Next(BookColors.Length)], 0.1f);
                    Part(g, "Book", new Vector3(x + bw * 0.5f, y0 + bh * 0.5f + 0.02f, d * 0.5f - 0.04f), new Vector3(bw, bh, d * 0.55f), bm, false);
                    x += bw + 0.004f;
                    if (rnd.NextDouble() < 0.08) x += 0.1f;
                }
            }
            return g;
        }

        public Transform Cabinet(string name, Vector3 pos, float yaw, float w, float h, float d, Material m = null, bool climbable = true)
        {
            m = m ?? mWood;
            var g = Group(name, pos, yaw, climbable);
            Part(g, "Body", new Vector3(0, h * 0.5f, 0), new Vector3(w, h, d), m);
            Part(g, "Door", new Vector3(0, h * 0.5f, d * 0.5f + 0.005f), new Vector3(w - 0.06f, h - 0.1f, 0.015f), mWoodLight, false);
            Part(g, "Handle", new Vector3(0.05f, h * 0.5f, d * 0.5f + 0.025f), new Vector3(0.02f, 0.14f, 0.02f), mBrass, false);
            return g;
        }

        public Transform Wardrobe(string name, Vector3 pos, float yaw)
        {
            var g = Group(name, pos, yaw, false);
            Part(g, "Body", new Vector3(0, 1.05f, 0), new Vector3(1.5f, 2.1f, 0.62f), mWoodLight);
            Part(g, "DoorL", new Vector3(-0.37f, 1.05f, 0.315f), new Vector3(0.72f, 1.95f, 0.02f), mWood, false);
            Part(g, "DoorR", new Vector3(0.37f, 1.05f, 0.315f), new Vector3(0.72f, 1.95f, 0.02f), mWood, false);
            Part(g, "HandleL", new Vector3(-0.06f, 1.05f, 0.34f), new Vector3(0.025f, 0.16f, 0.025f), mBrass, false);
            Part(g, "HandleR", new Vector3(0.06f, 1.05f, 0.34f), new Vector3(0.025f, 0.16f, 0.025f), mBrass, false);
            return g;
        }

        public Transform Dresser(string name, Vector3 pos, float yaw, float w = 1.2f, float h = 0.8f, float d = 0.5f)
        {
            var g = Cabinet(name, pos, yaw, w, h, d, mWood, true);
            for (int i = 0; i < 3; i++) Part(g, "Line", new Vector3(0, 0.14f + i * 0.24f, d * 0.5f + 0.01f), new Vector3(w - 0.08f, 0.012f, 0.012f), mWoodDark, false);
            return g;
        }

        // ------------------------------------------------------------------------------------------ soft furnishing & decor
        public void Rug(string name, Vector3 centre, float w, float d, Color a, Color b, float yaw = 0f)
        {
            var tex = Mats.Pattern("rug", a, b, 128);
            var m = Mats.Textured(tex, Vector2.one, Color.white, 0.05f);
            var q = B.Quad(root, name, new Vector3(centre.x, 0.012f, centre.z), Vector3.up, new Vector2(w, d), m);
            q.transform.rotation = Quaternion.Euler(90, yaw, 0);
            q.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        public Transform Plant(string name, Vector3 pos, float scale = 1f, bool decorOnly = false)
        {
            var g = Group(name, pos, 0, false);
            B.Cyl(g, "Pot", new Vector3(0, 0.15f * scale, 0), 0.16f * scale, 0.3f * scale, mPot, Col.None);
            B.Cyl(g, "Soil", new Vector3(0, 0.3f * scale, 0), 0.14f * scale, 0.02f * scale, Mats.Lit(new Color(0.25f, 0.17f, 0.12f), 0.05f), Col.None);
            var rnd = new System.Random(name.GetHashCode());
            for (int i = 0; i < 9; i++)
            {
                float a = i * 40f + rnd.Next(20);
                var l = B.Sphere(g, "Leaf", new Vector3(0, (0.55f + (float)rnd.NextDouble() * 0.5f) * scale, 0), new Vector3(0.06f, 0.55f, 0.16f) * scale * (0.8f + (float)rnd.NextDouble() * 0.4f), i % 2 == 0 ? mLeaf : mLeaf2, Col.None);
                l.transform.localRotation = Quaternion.Euler(0, a, 38f + (float)rnd.NextDouble() * 30f);
                l.transform.localPosition = Quaternion.Euler(0, a, 0) * new Vector3(0.1f * scale, (0.5f + (float)rnd.NextDouble() * 0.3f) * scale, 0);
            }
            if (!decorOnly) Part(g, "Collider", new Vector3(0, 0.35f * scale, 0), new Vector3(0.36f, 0.7f, 0.36f) * scale, mPot).GetComponent<MeshRenderer>().enabled = false;
            return g;
        }

        public Transform TableLamp(string name, Vector3 pos, float intensity = 1.4f)
        {
            var g = Group(name, pos, 0, false);
            B.Cyl(g, "Base", new Vector3(0, 0.04f, 0), 0.07f, 0.08f, mBrass, Col.None);
            B.Cyl(g, "Stem", new Vector3(0, 0.15f, 0), 0.012f, 0.22f, mBrass, Col.None);
            var shade = B.Cyl(g, "Shade", new Vector3(0, 0.3f, 0), 0.11f, 0.17f, LampShadeMat(), Col.None, false);
            var lg = new GameObject("Light"); lg.transform.SetParent(g, false); lg.transform.localPosition = new Vector3(0, 0.3f, 0);
            var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = 4.2f; l.color = new Color(1f, 0.8f, 0.55f); l.shadows = LightShadows.None; l.intensity = 0;
            var ll = g.gameObject.AddComponent<LampLight>(); ll.lightSource = l; ll.bulb = shade.GetComponent<Renderer>(); ll.maxIntensity = intensity;
            return g;
        }

        public Transform FloorLamp(string name, Vector3 pos, float h = 1.5f, bool movable = false, float intensity = 1.8f)
        {
            var g = Group(name, pos, 0, false);
            B.Cyl(g, "Base", new Vector3(0, 0.03f, 0), 0.16f, 0.06f, mBrass, Col.None);
            B.Cyl(g, "Pole", new Vector3(0, h * 0.5f, 0), 0.015f, h, mBrass, Col.None);
            var shade = B.Cyl(g, "Shade", new Vector3(0, h - 0.17f, 0), 0.21f, 0.34f, LampShadeMat(), Col.None);
            var lg = new GameObject("Light"); lg.transform.SetParent(g, false); lg.transform.localPosition = new Vector3(0, h - 0.17f, 0);
            var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = 5f; l.color = new Color(1f, 0.8f, 0.55f); l.shadows = LightShadows.None; l.intensity = 0;
            var ll = g.gameObject.AddComponent<LampLight>(); ll.lightSource = l; ll.bulb = shade.GetComponent<Renderer>(); ll.maxIntensity = intensity;
            return g;
        }

        public void Picture(string name, Vector3 pos, Vector3 normal, float w, float h, Color art, int style = 0)
        {
            var g = Group(name, pos, 0, false);
            g.rotation = Quaternion.LookRotation(-normal, Vector3.up);
            Part(g, "Frame", Vector3.zero, new Vector3(w, h, 0.03f), mWoodDark, false);
            Part(g, "Canvas", new Vector3(0, 0, -0.012f), new Vector3(w - 0.08f, h - 0.08f, 0.02f), Mats.Lit(art, 0.1f), false);
            Part(g, "Accent", new Vector3(w * 0.1f, -h * 0.1f, -0.02f), new Vector3((w - 0.08f) * 0.5f, (h - 0.08f) * 0.35f, 0.02f), Mats.Lit(Color.Lerp(art, Color.white, 0.5f), 0.1f), false);
            Part(g, "Sun", new Vector3(-w * 0.2f, h * 0.18f, -0.025f), new Vector3(0.1f, 0.1f, 0.02f), Mats.Lit(new Color(1f, 0.9f, 0.5f), 0.1f), false);
        }

        public Transform BookStack(string name, Vector3 basePos, int count, float w = 0.3f, float d = 0.22f, int seed = 3, bool movable = true, string id = null)
        {
            var g = Group(name, basePos, 0, false);
            var rnd = new System.Random(seed);
            float y = 0;
            for (int i = 0; i < count; i++)
            {
                float th = 0.045f + (float)rnd.NextDouble() * 0.03f;
                var m = Mats.Lit(BookColors[rnd.Next(BookColors.Length)], 0.15f);
                var b = Part(g, "Book", new Vector3((float)(rnd.NextDouble() - 0.5) * 0.03f, y + th * 0.5f, 0), new Vector3(w * (0.9f + (float)rnd.NextDouble() * 0.15f), th, d), m, false);
                b.transform.localRotation = Quaternion.Euler(0, (float)(rnd.NextDouble() - 0.5) * 12f, 0);
                y += th;
            }
            var c = g.gameObject.AddComponent<BoxCollider>();
            c.center = new Vector3(0, y * 0.5f, 0); c.size = new Vector3(w, y, d);
            if (movable)
            {
                var mv = g.gameObject.AddComponent<MovableObject>();
                mv.id = id ?? name; mv.displayName = "stack of books";
            }
            return g;
        }

        public Transform CrateStack(string name, Vector3 basePos, int levels, string id = null, bool movable = true)
        {
            var g = Group(name, basePos, 0, false);
            var cm = Mats.Lit(new Color(0.72f, 0.52f, 0.30f), 0.1f);
            var cm2 = Mats.Lit(new Color(0.6f, 0.42f, 0.24f), 0.1f);
            for (int i = 0; i < levels; i++)
            {
                float s = 0.34f - i * 0.02f;
                var b = Part(g, "Crate", new Vector3(0, 0.15f + i * 0.3f, 0), new Vector3(s, 0.3f, s), i % 2 == 0 ? cm : cm2, false);
                b.transform.localRotation = Quaternion.Euler(0, i * 17f, 0);
                Part(g, "Strap", new Vector3(0, 0.15f + i * 0.3f, 0), new Vector3(s + 0.01f, 0.05f, s + 0.01f), mWoodDark, false).transform.localRotation = Quaternion.Euler(0, i * 17f, 0);
            }
            var c = g.gameObject.AddComponent<BoxCollider>();
            c.center = new Vector3(0, levels * 0.15f, 0); c.size = new Vector3(0.34f, levels * 0.3f, 0.34f);
            if (movable) { var mv = g.gameObject.AddComponent<MovableObject>(); mv.id = id ?? name; mv.displayName = "crate tower"; }
            return g;
        }

        public Transform Clock(string name, Vector3 pos, float yaw)
        {
            var g = Group(name, pos, yaw, false);
            Part(g, "Body", new Vector3(0, 0.95f, 0), new Vector3(0.5f, 1.9f, 0.35f), mWoodDark);
            Part(g, "Face", new Vector3(0, 1.45f, 0.18f), new Vector3(0.34f, 0.34f, 0.02f), mPaper, false);
            Part(g, "Hand", new Vector3(0, 1.5f, 0.2f), new Vector3(0.02f, 0.12f, 0.01f), mBrass, false).transform.localRotation = Quaternion.Euler(0, 0, 25);
            B.Cyl(g, "Pendulum", new Vector3(0, 0.8f, 0.19f), 0.07f, 0.01f, mBrass, Col.None).transform.localRotation = Quaternion.Euler(90, 0, 0);
            return g;
        }

        // ------------------------------------------------------------------------------------------ window dressing helpers
        public WindowCover AddCover(string id, WindowSpec w, CoverKind kind, Color color, int startState, int[] cycle, float? gapL = null, float? gapR = null, float? widthOverride = null, float? centerU = null)
        {
            var g = new GameObject("Cover_" + id);
            g.transform.SetParent(root, false);
            var c = g.AddComponent<WindowCover>();
            c.id = id; c.kind = kind; c.color = color;
            c.origin = w.center + w.right * (centerU ?? 0f);
            c.right = w.right; c.up = Vector3.up; c.inward = w.inward;
            c.width = widthOverride ?? w.width; c.height = w.height;
            c.state = startState; c.cycle = cycle;
            if (gapL.HasValue) c.gapLeft = gapL.Value;
            if (gapR.HasValue) c.gapRight = gapR.Value;
            Covers.Add(c);
            return c;
        }

        /// <summary>A dangling cord that operates a cover. pos is the world position of the cord's top.</summary>
        public CordInteractable AddCord(WindowCover cover, Vector3 top, float length = 1.0f)
        {
            var g = new GameObject("Cord_" + cover.id).transform;
            g.SetParent(root, false); g.position = top;
            var cm = Mats.Lit(new Color(0.9f, 0.85f, 0.7f), 0.1f);
            B.Cyl(g, "String", new Vector3(0, -length * 0.5f, 0), 0.006f, length, cm, Col.None, false);
            var tass = B.Sphere(g, "Tassel", new Vector3(0, -length - 0.04f, 0), new Vector3(0.07f, 0.1f, 0.07f), mFabric1, Col.None, false);
            var trig = g.gameObject.AddComponent<BoxCollider>();
            trig.isTrigger = true; trig.center = new Vector3(0, -length + 0.1f, 0); trig.size = new Vector3(0.25f, 0.5f, 0.25f);
            var ci = g.gameObject.AddComponent<CordInteractable>();
            ci.cover = cover; ci.range = 0.85f; ci.needsFacing = false;
            return ci;
        }

        public SkyWindow AddSkyWindowTrigger(WindowSpec w, string label)
        {
            // glowing ornament on the frame + interaction volume on the sill
            var g = new GameObject("SkyWindow_" + w.id).transform;
            g.SetParent(root, false);
            Vector3 mid = w.center - w.inward * (WT * 0.5f);
            float sillY = w.center.y - w.height * 0.5f;
            g.position = new Vector3(mid.x, sillY, mid.z) + w.inward * 0.35f;
            var col = g.gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true; col.center = new Vector3(0, 0.1f, 0); col.size = Vector3.Max(new Vector3(Mathf.Abs(w.right.x) * w.width, 0.4f, Mathf.Abs(w.right.z) * w.width), new Vector3(0.6f, 0.4f, 0.6f));
            var sw = g.gameObject.AddComponent<SkyWindow>(); sw.windowName = label;
            // ornament: a small dial above the window, with soft glow
            Vector3 orn = new Vector3(mid.x, w.center.y + w.height * 0.5f + 0.2f, mid.z) + w.inward * 0.12f;
            var o = B.Quad(root, "SkyOrnament_" + w.id, orn, w.inward, new Vector2(0.3f, 0.3f), Mats.Blend(new Color(1f, 0.85f, 0.45f, 0.95f), Mats.Glyph(GlyphKind.Sun), Mats.BlendKind.Additive));
            var o2 = B.Quad(root, "SkyOrnamentMoon_" + w.id, orn + w.right * 0.32f, w.inward, new Vector2(0.24f, 0.24f), Mats.Blend(new Color(0.6f, 0.75f, 1f, 0.95f), Mats.Glyph(GlyphKind.Moon), Mats.BlendKind.Additive));
            o.AddComponent<PulseGlow>(); o2.AddComponent<PulseGlow>();
            return sw;
        }
    }
}
