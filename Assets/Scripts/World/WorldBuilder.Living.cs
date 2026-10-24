using System.Collections.Generic;
using UnityEngine;

namespace CatOnTheSill
{
    public partial class WorldBuilder
    {
        // Solution geometry is derived from the light direction and the carvings, so the puzzles are always exactly solvable.
        static Vector3 SunAt(TimeOfDay t) => Times.ToSun(t);

        GlyphTarget MakeGlyph(string name, Vector3 pos, Vector3 normal, GlyphKind kind, float size, GlyphMode mode, Color glow, Color? etch = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.position = pos;
            var g = go.AddComponent<GlyphTarget>();
            g.mode = mode; g.normal = normal; g.kind = kind; g.glowColor = glow;
            if (etch.HasValue) g.etchColor = etch.Value;
            g.Build(size);
            return g;
        }

        public Transform MovablePlant(string name, Vector3 pos, string id)
        {
            var g = Group(name, pos, 0, false);
            B.Cyl(g, "Pot", new Vector3(0, 0.17f, 0), 0.2f, 0.34f, mPot, Col.None);
            B.Cyl(g, "Soil", new Vector3(0, 0.34f, 0), 0.18f, 0.02f, Mats.Lit(new Color(0.25f, 0.17f, 0.12f), 0.05f), Col.None);
            var rnd = new System.Random(name.GetHashCode());
            for (int i = 0; i < 22; i++)
            {
                float a = i * 360f / 22f + rnd.Next(14);
                float lift = 0.5f + (float)rnd.NextDouble() * 0.85f;
                var l = B.Sphere(g, "Leaf", Quaternion.Euler(0, a, 0) * new Vector3(0.09f + (float)rnd.NextDouble() * 0.08f, lift, 0), new Vector3(0.07f, 0.62f, 0.2f) * (0.9f + (float)rnd.NextDouble() * 0.4f), i % 2 == 0 ? mLeaf : mLeaf2, Col.None);
                l.transform.localRotation = Quaternion.Euler(0, a, 28f + (float)rnd.NextDouble() * 34f);
            }
            B.Sphere(g, "Core", new Vector3(0, 0.9f, 0), new Vector3(0.34f, 1.0f, 0.34f), mLeaf, Col.None);
            var c = g.gameObject.AddComponent<BoxCollider>();
            c.center = new Vector3(0, 0.75f, 0); c.size = new Vector3(0.44f, 1.5f, 0.44f);
            var mv = g.gameObject.AddComponent<MovableObject>();
            mv.id = id; mv.displayName = "potted fern"; mv.pushSpeed = 0.85f;
            return g;
        }

        public Transform MovableLamp(string name, Vector3 pos, string id)
        {
            var g = Group(name, pos, 0, false);
            B.Cyl(g, "Base", new Vector3(0, 0.03f, 0), 0.16f, 0.06f, mBrass, Col.None);
            B.Cyl(g, "Pole", new Vector3(0, 0.75f, 0), 0.015f, 1.5f, mBrass, Col.None);
            var shade = B.Cyl(g, "Shade", new Vector3(0, 1.33f, 0), 0.21f, 0.34f, LampShadeMat(), Col.None);
            var lg = new GameObject("Light"); lg.transform.SetParent(g, false); lg.transform.localPosition = new Vector3(0, 1.3f, 0);
            var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = 5f; l.color = new Color(1f, 0.8f, 0.55f); l.shadows = LightShadows.None; l.intensity = 0;
            var ll = g.gameObject.AddComponent<LampLight>(); ll.lightSource = l; ll.bulb = shade.GetComponent<Renderer>(); ll.maxIntensity = 2f;
            // colliders match what actually blocks light: a thin pole plus the shade
            var c1 = g.gameObject.AddComponent<BoxCollider>(); c1.center = new Vector3(0, 0.05f, 0); c1.size = new Vector3(0.32f, 0.1f, 0.32f);
            var c2 = g.gameObject.AddComponent<BoxCollider>(); c2.center = new Vector3(0, 0.75f, 0); c2.size = new Vector3(0.04f, 1.5f, 0.04f);
            var c3 = g.gameObject.AddComponent<BoxCollider>(); c3.center = new Vector3(0, 1.33f, 0); c3.size = new Vector3(0.42f, 0.34f, 0.42f);
            var mv = g.gameObject.AddComponent<MovableObject>();
            mv.id = id; mv.displayName = "floor lamp"; mv.pushSpeed = 0.85f;
            return g;
        }

        // ====================================================================================== LIVING ROOM  (Fragment 2 - The Garden's Shadow, and the final puzzle wall)
        void BuildLiving()
        {
            var win = Windows["liv_s"];
            win.splits = new[] { 0.325f, 0.8114f }; win.panes = 3;

            // --- furniture (the south half stays clear: the sun and the puzzle shadows live there)
            Sofa("Sofa", new Vector3(7.3f, 0, 7.42f), 180f, 2.2f, mFabric2);
            Armchair("Armchair", new Vector3(11.2f, 0, 7.15f), 205f, mFabric3);
            Table("CoffeeTable", new Vector3(8.9f, 0, 5.6f), 0f, 1.1f, 0.42f, 0.55f, mWoodLight, mWood);
            Rug("LivingRug", new Vector3(8.9f, 0, 5.4f), 3.0f, 2.2f, new Color(0.72f, 0.45f, 0.40f), new Color(0.96f, 0.88f, 0.7f));
            Bookshelf("LivingShelf", new Vector3(11.68f, 0, 6.7f), 270f, 1.4f, 1.9f, 0.4f, 5, 21);
            Clock("GrandClock", new Vector3(10.35f, 0, 7.62f), 180f);
            Picture("PicSofa", new Vector3(7.3f, 1.85f, 7.89f), Vector3.back, 1.1f, 0.7f, new Color(0.45f, 0.65f, 0.8f));
            Picture("PicSunset", new Vector3(11.89f, 1.8f, 4.0f), Vector3.left, 0.8f, 0.55f, new Color(0.95f, 0.55f, 0.35f));
            Picture("PicFlowers", new Vector3(6.11f, 1.75f, 7.0f), Vector3.right, 0.6f, 0.8f, new Color(0.8f, 0.5f, 0.65f));
            Plant("CornerFern", new Vector3(6.45f, 0, 0.5f), 1f, true);
            Plant("SofaPlant", new Vector3(8.6f, 0, 7.55f), 0.8f, true);
            TableLamp("SofaLamp", new Vector3(6.55f, 0.55f, 6.0f), 1.4f);
            Cabinet("SideTable", new Vector3(6.5f, 0, 6.0f), 90f, 0.5f, 0.55f, 0.45f, mWood, true);
            // mantle clutter
            BookStack("DecorBooks", new Vector3(11.7f, 1.9f, 6.5f), 3, 0.24f, 0.2f, 5, false);

            // --- dangling toy (bat it!)
            var toyRoot = Group("DanglingToy", new Vector3(7.4f, WallH, 4.3f), 0);
            var swing = B.Empty(toyRoot, "Swing", Vector3.zero);
            B.Cyl(swing.transform, "String", new Vector3(0, -1.1f, 0), 0.006f, 2.2f, Mats.Lit(new Color(0.9f, 0.85f, 0.7f), 0.1f), Col.None, false);
            B.Sphere(swing.transform, "Ball", new Vector3(0, -2.28f, 0), 0.14f, mFabric1, Col.None);
            B.Sphere(swing.transform, "Feather", new Vector3(0.03f, -2.46f, 0), new Vector3(0.05f, 0.18f, 0.02f), Mats.Lit(new Color(0.4f, 0.8f, 0.9f), 0.1f), Col.None);
            var trig = toyRoot.gameObject.AddComponent<BoxCollider>(); trig.isTrigger = true; trig.center = new Vector3(0, -2.3f, 0); trig.size = new Vector3(0.5f, 0.5f, 0.5f);
            var toy = toyRoot.gameObject.AddComponent<DanglingToy>(); toy.swing = swing.transform;

            // --- blinds on the bay window: each pane has its own cord
            float[] centersU = { -1.485f, 0.3f, 1.785f };
            float[] widths = { 1.43f, 2.14f, 0.83f };
            int[] startStates = { 0, 2, 0 };
            float[] cordX = { 7.12f, 8.5f, 10.66f };
            var blinds = new List<WindowCover>();
            for (int i = 0; i < 3; i++)
            {
                var b = AddCover("liv_blind" + i, win, CoverKind.Blind, new Color(0.93f, 0.9f, 0.82f), startStates[i], new[] { 2, 0 }, null, null, widths[i] - 0.08f, centersU[i]);
                blinds.Add(b);
                AddCord(b, new Vector3(cordX[i], 2.95f, 0.38f), 2.3f);
            }
            AddSkyWindowTrigger(win, "living room bay window");
            // decorative side curtains outside the opening (they never block the light)
            var curtMat = Mats.Textured(Mats.Pattern("stripes", new Color(0.78f, 0.5f, 0.35f), new Color(0.68f, 0.42f, 0.3f), 128), new Vector2(3, 1), Color.white, 0.1f);
            Part(Group("CurtainL", new Vector3(6.55f, 1.6f, 0.3f), 0), "c", Vector3.zero, new Vector3(0.36f, 3.0f, 0.1f), curtMat, false);
            Part(Group("CurtainR", new Vector3(11.7f, 1.6f, 0.3f), 0), "c", Vector3.zero, new Vector3(0.36f, 3.0f, 0.1f), curtMat, false);

            // ---------------------------------------------------------------- the three movable props (used by two puzzles)
            var plant = MovablePlant("LivingFern", new Vector3(10.9f, 0, 6.2f), "liv_plant").GetComponent<MovableObject>();
            var tower = CrateStack("LivingCrates", new Vector3(10.1f, 0, 4.5f), 4, "liv_crates").GetComponent<MovableObject>();
            var lamp = MovableLamp("LivingLamp", new Vector3(7.0f, 0, 3.4f), "liv_lamp").GetComponent<MovableObject>();
            foreach (var m in new[] { plant, tower, lamp }) { m.xRange = new Vector2(6.45f, 11.55f); m.zRange = new Vector2(0.55f, 7.0f); }
            tower.xRange = new Vector2(6.4f, 11.6f);

            Solutions["liv_plant_p2"] = new Vector3(10.30f, 0, 1.68f); Solutions["liv_crates_p2"] = new Vector3(10.89f, 0, 2.02f);
            // ================================================================ PUZZLE 2 - at sunset, two shadows fill two carvings on the east wall
            Vector3 sunset = SunAt(TimeOfDay.Sunset);
            Vector3 wallE = new Vector3(11.9f, 0, 2.6f);
            var crown = MakeGlyph("TreeCrown", new Vector3(11.9f, 0.85f, 2.6f), Vector3.left, GlyphKind.Tree, 0.56f, GlyphMode.Shadow, new Color(1f, 0.62f, 0.25f), new Color(0.35f, 0.18f, 0.1f));
            var trunk = MakeGlyph("TreeTrunk", new Vector3(11.9f, 0.32f, 2.6f), Vector3.left, GlyphKind.Square, 0.5f, GlyphMode.Shadow, new Color(1f, 0.62f, 0.25f), new Color(0.35f, 0.18f, 0.1f));
            var plantMark = MakeGlyph("FernFloorMark", Solutions["liv_plant_p2"] + Vector3.up * 0.012f, Vector3.up, GlyphKind.Tree, 1.05f, GlyphMode.Light, new Color(1f, 0.62f, 0.25f), new Color(0.7f, 0.34f, 0.06f));
            var crateMark = MakeGlyph("CrateFloorMark", Solutions["liv_crates_p2"] + Vector3.up * 0.012f, Vector3.up, GlyphKind.Square, 1.0f, GlyphMode.Light, new Color(1f, 0.62f, 0.25f), new Color(0.7f, 0.34f, 0.06f));
            plantMark.enabled = false;
            crateMark.enabled = false;
            AddLightClue("LivingSunsetClue", new Vector3(11.88f, 1.45f, 2.6f), Vector3.left, "PUT THE FERN ON THE TREE\nPUT THE CRATES ON THE SQUARE", false, false, 0.105f);
            AddPawTrail("LivingLightPaws", new Vector3(7.25f, 0f, 1.05f), Solutions["liv_plant_p2"], 7, false, false);
            AddPawTrail("LivingShadowPaws", new Vector3(9.2f, 0f, 3.9f), Solutions["liv_crates_p2"], 7, true, false);
            crown.casters.Add(plant); trunk.casters.Add(tower);
            var p2go = new GameObject("Puzzle2"); p2go.transform.SetParent(root, false);
            var p2 = p2go.AddComponent<PlacementPuzzle>();
            p2.first = plant; p2.firstPosition = Solutions["liv_plant_p2"];
            p2.second = tower; p2.secondPosition = Solutions["liv_crates_p2"];
            p2.radius = 0.8f;
            p2.id = "p2"; p2.fragmentIndex = 1; p2.title = FragmentInfo.Titles[1]; p2.roomName = "Living Room"; p2.roomBounds = Rooms["Living Room"];
            p2.requiredHold = 0.35f; p2.revealPos = new Vector3(11.2f, 0.95f, 2.6f); p2.landPos = new Vector3(11.2f, 0.5f, 2.6f);
            p2.hints = new[]
            {
                "Follow the pawprints to the two large floor marks.",
                "Push the fern onto the tree mark and the crates onto the square mark.",
                "That is all: fern on tree, crates on square."
            };
            Puzzles.Add(p2);

            // (the finale - fragment 7 - lives in the Library next to the Study: see WorldBuilder.Finale.cs)
        }
    }
}










