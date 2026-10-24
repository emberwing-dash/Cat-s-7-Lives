using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CatOnTheSill
{
    /// <summary>
    /// Builds the whole house procedurally at runtime. Partial class: Shell (walls / windows), Furniture, and one file per room.
    /// Coordinates: X east, Y up, Z north. Grid lines x = 0 / 6 / 12 / 18 and z = 0 / 6 / 8 / 12.
    /// </summary>
    public partial class WorldBuilder
    {
        public const float WallH = 3.2f;
        public const float SunroomH = 6.6f;
        public const float WT = 0.2f;        // wall thickness

        /// <summary>How prefabs are placed in the scene. The editor baker swaps this for PrefabUtility.InstantiatePrefab so they stay linked.</summary>
        public static System.Func<GameObject, Transform, GameObject> Spawn = (p, parent) => Object.Instantiate(p, parent);

        public Transform root;
        public readonly Dictionary<string, WindowSpec> Windows = new Dictionary<string, WindowSpec>();
        public readonly Dictionary<string, Bounds> Rooms = new Dictionary<string, Bounds>();
        public readonly List<Puzzle> Puzzles = new List<Puzzle>();
        public readonly List<WindowCover> Covers = new List<WindowCover>();
        public Vector3 CatStart = new Vector3(1.2f, 0.66f, 5.0f);
        /// <summary>Known solution poses (for tests and debugging): id -> position (x, y, z)</summary>
        public readonly Dictionary<string, Vector3> Solutions = new Dictionary<string, Vector3>();

        // openings registered per wall: key "X6" = wall at x=6 etc.
        readonly Dictionary<string, List<Opening>> openings = new Dictionary<string, List<Opening>>();

        // materials
        Material mWall, mWallDark, mTrim, mCeil, mFloorWood, mFloorTile, mWallBed, mWallLiving, mWallKitchen, mWallStudy, mWallSun, mWallHall, mExterior, mGlass;
        Material mWood, mWoodDark, mWoodLight, mFabric1, mFabric2, mFabric3, mMetal, mWhite, mPot, mLeaf, mLeaf2, mBrass, mPaper;

        public void Build(Transform parent)
        {
            root = new GameObject("World").transform;
            root.SetParent(parent, false);
            MakeMaterials();
            DeclareOpenings();
            BuildFloorsAndCeilings();
            BuildWalls();
            BuildWindowFrames();
            BuildDoorTrim();
            BuildExterior();
            BuildBedroom();
            BuildLiving();
            BuildKitchen();
            BuildStudy();
            BuildHall();
            BuildSunroom();
            BuildNpcs();
            foreach (var c in Covers) c.Build();
            var refs = root.gameObject.AddComponent<WorldRefs>();
            refs.puzzles.AddRange(Puzzles); refs.catStart = CatStart;
            foreach (var kv in Solutions) refs.solutionList.Add(new NamedPoint { key = kv.Key, value = kv.Value });
            Physics.SyncTransforms();
        }

        void MakeMaterials()
        {
            mWall = Mats.Lit(new Color(0.95f, 0.90f, 0.82f), 0.05f);
            mWallBed = Mats.Lit(new Color(0.78f, 0.86f, 0.88f), 0.05f);
            mWallLiving = Mats.Lit(new Color(0.96f, 0.88f, 0.76f), 0.05f);
            mWallKitchen = Mats.Lit(new Color(0.92f, 0.95f, 0.84f), 0.05f);
            mWallStudy = Mats.Lit(new Color(0.60f, 0.70f, 0.66f), 0.05f);
            mWallSun = Mats.Lit(new Color(0.93f, 0.94f, 0.9f), 0.05f);
            mWallHall = Mats.Lit(new Color(0.76f, 0.68f, 0.62f), 0.05f);
            mWallDark = Mats.Lit(new Color(0.35f, 0.27f, 0.22f), 0.1f);
            mTrim = Mats.Lit(new Color(0.98f, 0.96f, 0.92f), 0.2f);
            mCeil = Mats.Lit(new Color(0.97f, 0.96f, 0.94f), 0.0f);
            mExterior = Mats.Lit(new Color(0.88f, 0.80f, 0.68f), 0.05f);
            mFloorWood = Mats.Textured(Mats.Pattern("wood", new Color(0.62f, 0.42f, 0.25f), new Color(0.78f, 0.55f, 0.34f)), Vector2.one, Color.white, 0.25f);
            mFloorTile = Mats.Textured(Mats.Pattern("tile", new Color(0.95f, 0.93f, 0.88f), new Color(0.55f, 0.65f, 0.62f)), Vector2.one, Color.white, 0.35f);
            mWood = Mats.Lit(new Color(0.60f, 0.40f, 0.24f), 0.3f);
            mWoodDark = Mats.Lit(new Color(0.36f, 0.22f, 0.13f), 0.3f);
            mWoodLight = Mats.Lit(new Color(0.82f, 0.64f, 0.42f), 0.3f);
            mFabric1 = Mats.Lit(new Color(0.78f, 0.38f, 0.34f), 0.05f);
            mFabric2 = Mats.Lit(new Color(0.36f, 0.52f, 0.62f), 0.05f);
            mFabric3 = Mats.Lit(new Color(0.84f, 0.72f, 0.46f), 0.05f);
            mMetal = Mats.Metal(new Color(0.8f, 0.82f, 0.85f), 0.9f);
            mBrass = Mats.Metal(new Color(0.85f, 0.65f, 0.3f), 0.7f);
            mWhite = Mats.Lit(new Color(0.96f, 0.95f, 0.92f), 0.3f);
            mPot = Mats.Lit(new Color(0.72f, 0.4f, 0.28f), 0.2f);
            mLeaf = Mats.Lit(new Color(0.28f, 0.55f, 0.25f), 0.2f);
            mLeaf2 = Mats.Lit(new Color(0.40f, 0.68f, 0.30f), 0.2f);
            mPaper = Mats.Lit(new Color(0.94f, 0.9f, 0.8f), 0.1f);
            mGlass = Mats.Blend(new Color(0.75f, 0.88f, 1f, 0.07f), Mats.Soft, Mats.BlendKind.Alpha);
        }

        // ================================================================================== openings
        string WallKey(char axis, float c) => axis + Mathf.RoundToInt(c).ToString();

        void AddOpening(char axis, float c, Opening o)
        {
            string k = WallKey(axis, c);
            if (!openings.TryGetValue(k, out var l)) openings[k] = l = new List<Opening>();
            l.Add(o);
        }

        void Door(string name, char axis, float c, float s0, float s1, float height = 2.3f)
            => AddOpening(axis, c, new Opening { name = name, s0 = s0, s1 = s1, y0 = 0f, y1 = height });

        /// <summary>Declares a window on the wall 'axis'+'c' (X = wall at constant x, Z = wall at constant z).</summary>
        WindowSpec Window(string id, char axis, float c, float s0, float s1, float y0, float y1, int panes = 1, bool slit = false, Color? tint = null)
        {
            AddOpening(axis, c, new Opening { name = id, s0 = s0, s1 = s1, y0 = y0, y1 = y1, isWindow = true });
            // inward direction from which side of the grid the room is on
            Vector3 inward;
            Vector3 center;
            float mid = (s0 + s1) * 0.5f, my = (y0 + y1) * 0.5f;
            if (axis == 'Z')
            {
                inward = c < 6f ? Vector3.forward : Vector3.back;       // south wall (z=0) faces +z; north wall (z=12) faces -z; others decided by caller
                center = new Vector3(mid, my, c + (inward.z > 0 ? WT * 0.5f : -WT * 0.5f));
            }
            else
            {
                inward = c < 9f ? Vector3.right : Vector3.left;
                center = new Vector3(c + (inward.x > 0 ? WT * 0.5f : -WT * 0.5f), my, mid);
            }
            var w = new WindowSpec { id = id, center = center, inward = inward, right = Vector3.Cross(Vector3.up, inward), width = s1 - s0, height = y1 - y0, panes = panes, slit = slit };
            if (tint.HasValue) w.tint = tint.Value;
            Windows[id] = w;
            return w;
        }

        void DeclareOpenings()
        {
            // doors
            Door("d_bed_liv", 'X', 6, 4.3f, 5.5f);
            Door("d_bed_study", 'Z', 6, 4.2f, 5.4f);
            Door("d_study_hall", 'X', 6, 9.4f, 10.6f);
            Door("d_liv_hall", 'Z', 8, 8.2f, 9.8f);
            Door("d_liv_kit", 'X', 12, 4.3f, 5.5f);
            Door("d_hall_sun", 'X', 12, 9.4f, 10.6f);
            Door("d_kit_sun", 'Z', 6, 14.6f, 15.8f);

            // windows (south wall z=0 -> inward +z, north wall z=12 -> inward -z, west wall x=0 -> +x, east wall x=18 -> -x)
            Window("bed_s", 'Z', 0, 1.8f, 4.2f, 0.85f, 2.55f, 2);
            Window("liv_s", 'Z', 0, 7.0f, 11.4f, 0.8f, 2.9f, 3).splits = new[] { 0.325f, 0.8114f };
            Window("kit_s", 'Z', 0, 13.6f, 15.6f, 1.15f, 2.45f, 1);
            Window("study_n", 'Z', 12, 1.9f, 4.5f, 1.0f, 2.6f, 1);
            Window("study_w", 'X', 0, 7.6f, 9.4f, 1.0f, 2.6f, 1);
            Window("hall_n", 'Z', 12, 7.6f, 10.4f, 0.9f, 2.9f, 2);
            Window("sun_n", 'Z', 12, 13.3f, 16.7f, 0.9f, 2.3f, 3);
            Window("attic_n", 'Z', 12, 14.3f, 15.7f, 3.6f, 5.2f, 1);
            // stained-glass slits in the sunroom's east wall (x=18)
            Window("sun_e_gold", 'X', 18, 6.75f, 7.25f, 0.9f, 2.4f, 1, true, new Color(1f, 0.82f, 0.35f, 0.16f)).beamTint = new Color(1f, 0.8f, 0.3f);
            Window("sun_e_red", 'X', 18, 7.75f, 8.25f, 0.9f, 2.4f, 1, true, new Color(1f, 0.3f, 0.3f, 0.16f)).beamTint = new Color(1f, 0.3f, 0.25f);
            Window("sun_e_blue", 'X', 18, 8.75f, 9.25f, 0.9f, 2.4f, 1, true, new Color(0.35f, 0.55f, 1f, 0.16f)).beamTint = new Color(0.35f, 0.55f, 1f);
        }

        // ================================================================================== floors & ceilings
        void BuildFloorsAndCeilings()
        {
            var slabMat = Mats.Lit(new Color(0.5f, 0.45f, 0.4f), 0.05f);
            B.BoxMinMax(root, "Foundation", new Vector3(-0.4f, -0.6f, -0.4f), new Vector3(18.4f, 0f, 12.4f), slabMat, true, true).layer = Layer.Structure;

            void Floor(string n, float x0, float x1, float z0, float z1, Material m, float tile = 1.2f)
            {
                var mat = new Material(m); mat.mainTextureScale = new Vector2((x1 - x0) / tile, (z1 - z0) / tile); mat.SetTextureScale("_BaseMap", mat.mainTextureScale);
                var q = B.Quad(root, n, new Vector3((x0 + x1) * 0.5f, 0.004f, (z0 + z1) * 0.5f), Vector3.up, new Vector2(x1 - x0, z1 - z0), mat);
                q.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            Floor("F_Bedroom", 0.1f, 5.9f, 0.1f, 5.9f, mFloorWood);
            Floor("F_Study", 0.1f, 5.9f, 6.1f, 11.9f, mFloorWood);
            Floor("F_Living", 6.1f, 11.9f, 0.1f, 7.9f, mFloorWood);
            Floor("F_Hall", 6.1f, 11.9f, 8.1f, 11.9f, mFloorWood);
            Floor("F_Kitchen", 12.1f, 17.9f, 0.1f, 5.9f, mFloorTile, 1.4f);
            Floor("F_Sunroom", 12.1f, 17.9f, 6.1f, 11.9f, mFloorTile, 1.4f);
            // doorway thresholds
            Floor("F_Doors", 5.9f, 6.1f, 0.1f, 11.9f, mFloorWood);

            // ceilings: closed boxes that only cast shadows (invisible to the camera but they block sunlight & the camera)
            void Ceiling(string n, float x0, float x1, float z0, float z1, float y)
            {
                var g = B.BoxMinMax(root, n, new Vector3(x0, y, z0), new Vector3(x1, y + 0.25f, z1), mCeil, true, true);
                g.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                g.AddComponent<CeilingMarker>();
                g.layer = Layer.Structure;
                // the visible ceiling: a single-sided quad seen only from below (so the title-screen dollhouse view stays open)
                var vis = B.Quad(root, n + "_Visible", new Vector3((x0 + x1) * 0.5f, y - 0.004f, (z0 + z1) * 0.5f), Vector3.down, new Vector2(x1 - x0 - 0.2f, z1 - z0 - 0.2f), n == "C_Sunroom" ? mWoodLight : mCeil);
                vis.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            Ceiling("C_Bedroom", -0.1f, 6.1f, -0.1f, 6.1f, WallH);
            Ceiling("C_Study", -0.1f, 6.1f, 5.9f, 12.1f, WallH);
            Ceiling("C_Living", 5.9f, 12.1f, -0.1f, 8.1f, WallH);
            Ceiling("C_Hall", 5.9f, 12.1f, 7.9f, 12.1f, WallH);
            Ceiling("C_Kitchen", 11.9f, 18.1f, -0.1f, 6.1f, WallH);
            Ceiling("C_Sunroom", 11.9f, 18.1f, 5.9f, 12.1f, SunroomH);

            Rooms["Bedroom"] = new Bounds(new Vector3(3, 1.5f, 3), new Vector3(6, 4, 6));
            Rooms["Study"] = new Bounds(new Vector3(3, 1.5f, 9), new Vector3(6, 4, 6));
            Rooms["Living Room"] = new Bounds(new Vector3(9, 1.5f, 4), new Vector3(6, 4, 8));
            Rooms["Library"] = new Bounds(new Vector3(9, 1.5f, 10), new Vector3(6, 4, 4));
            Rooms["Kitchen"] = new Bounds(new Vector3(15, 1.5f, 3), new Vector3(6, 4, 6));
            Rooms["Sunroom"] = new Bounds(new Vector3(15, 3.3f, 9), new Vector3(6, 8, 6));
        }

        // ================================================================================== walls
        void BuildWalls()
        {
            // exterior
            WallZ(0, 0f, 18f, WallH, mExterior, mWallBed);        // south
            WallZ(12, 0f, 18f, WallH, mExterior, mWallStudy);     // north (sunroom part is taller, see below)
            WallX(0, 0f, 12f, WallH, mExterior, mWallBed);        // west
            WallX(18, 0f, 6f, WallH, mExterior, mWallKitchen);    // east kitchen
            // sunroom shell is taller
            WallX(18, 6f, 12f, SunroomH, mExterior, mWallSun);
            WallZ(12, 12f, 18f, SunroomH - WallH, mExterior, mWallSun, WallH);   // extra height above north wall of the sunroom
            // interior
            WallX(6, 0f, 12f, WallH, mWall, mWall);
            WallX(12, 0f, 6f, WallH, mWall, mWall);
            WallX(12, 6f, 12f, SunroomH, mWall, mWallSun);
            WallZ(6, 0f, 6f, WallH, mWall, mWall);
            WallZ(6, 12f, 18f, SunroomH, mWall, mWallSun);
            WallZ(8, 6f, 12f, WallH, mWall, mWall);
        }

        /// <summary>Wall of constant x. s runs along z.</summary>
        void WallX(float x, float z0, float z1, float h, Material outer, Material inner, float yBase = 0f)
            => BuildWall('X', x, z0, z1, h, yBase);
        void WallZ(float z, float x0, float x1, float h, Material outer, Material inner, float yBase = 0f)
            => BuildWall('Z', z, x0, x1, h, yBase);

        readonly HashSet<string> builtSegments = new HashSet<string>();

        void BuildWall(char axis, float c, float s0, float s1, float h, float yBase)
        {
            openings.TryGetValue(WallKey(axis, c), out var all);
            var list = new List<Opening>();
            if (all != null) foreach (var o in all) if (o.s0 >= s0 - 1e-3f && o.s1 <= s1 + 1e-3f && o.y1 > yBase + 1e-3f && o.y0 < yBase + h - 1e-3f) list.Add(o);
            list.Sort((a, b) => a.s0.CompareTo(b.s0));
            // extend wall slightly past ends to close corners
            float a0 = s0 - WT * 0.5f, a1 = s1 + WT * 0.5f;
            float cur = a0;
            string n = $"Wall_{axis}{c}_{s0}-{s1}";
            Material m = mWall;
            void Seg(float from, float to, float y0, float y1)
            {
                if (to - from < 1e-3f || y1 - y0 < 1e-3f) return;
                Vector3 mn, mx;
                if (axis == 'X') { mn = new Vector3(c - WT * 0.5f, y0, from); mx = new Vector3(c + WT * 0.5f, y1, to); }
                else { mn = new Vector3(from, y0, c - WT * 0.5f); mx = new Vector3(to, y1, c + WT * 0.5f); }
                var g = B.BoxMinMax(root, n, mn, mx, PickWallMat(axis, c, (from + to) * 0.5f), true, true);
                g.layer = Layer.Structure;
            }
            foreach (var o in list)
            {
                Seg(cur, o.s0, yBase, yBase + h);
                if (o.y0 > 0.001f) Seg(o.s0, o.s1, yBase, o.y0);
                Seg(o.s0, o.s1, o.y1, yBase + h);
                cur = o.s1;
            }
            Seg(cur, a1, yBase, yBase + h);
        }

        Material PickWallMat(char axis, float c, float s)
        {
            // pick a wall colour by the room the wall segment mostly belongs to (inner side)
            float x = axis == 'X' ? c : s, z = axis == 'X' ? s : c;
            if (x < 6f && z < 6f) return mWallBed;
            if (x < 6f) return mWallStudy;
            if (x < 12f && z < 8f) return mWallLiving;
            if (x < 12f) return mWallHall;
            if (z < 6f) return mWallKitchen;
            return mWallSun;
        }

        // ================================================================================== window frames, sills, glass, beams
        void BuildWindowFrames()
        {
            foreach (var w in Windows.Values)
            {
                var g = new GameObject("Window_" + w.id).transform;
                g.SetParent(root, false);
                w.frameRoot = g;
                Vector3 c = w.center, r = w.right, inward = w.inward;
                // the wall's inner face is at c; the centre of the wall is WT/2 outward
                Vector3 mid = c - inward * (WT * 0.5f);
                float fw = w.slit ? 0.03f : 0.06f, depth = 0.12f;
                Material fm = mTrim;
                Vector3 FramePos(float u, float v, float d) => mid + r * u + Vector3.up * v + inward * d;
                void Bar(string n, float u, float v, float sw, float sh)
                {
                    var bar = B.Box(g, n, FramePos(u, v, 0.02f), Vector3.one, fm, true, true);
                    bar.transform.rotation = Quaternion.LookRotation(inward, Vector3.up);
                    bar.transform.localScale = new Vector3(sw, sh, depth);
                }
                float hw = w.width * 0.5f, hh = w.height * 0.5f, my = c.y;
                Bar("L", -hw + fw * 0.5f, 0, fw, w.height);
                Bar("R", hw - fw * 0.5f, 0, fw, w.height);
                Bar("T", 0, hh - fw * 0.5f, w.width, fw);
                Bar("B", 0, -hh + fw * 0.5f, w.width, fw);
                if (w.splits != null)
                    for (int i = 0; i < w.splits.Length; i++) Bar("M" + (i + 1), -hw + w.width * w.splits[i], 0, 0.05f, w.height);
                else
                    for (int i = 1; i < w.panes; i++) Bar("M" + i, -hw + w.width * i / w.panes, 0, 0.05f, w.height);
                // glass (no collider; light passes through)
                var gm = Mats.Blend(w.tint, Mats.BeamTex, Mats.BlendKind.Alpha);
                gm.SetTexture("_MainTex", Texture2D.whiteTexture);
                var glass = B.Quad(g, "Glass", mid, inward, new Vector2(w.width, w.height), gm);
                glass.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                // sill (cat can sit on it)
                if (!w.slit && w.center.y - w.height * 0.5f < 2.0f)
                {
                    float sillTopY = c.y - hh;
                    var sill = B.Box(g, "Sill", new Vector3(mid.x, sillTopY - 0.025f, mid.z) + inward * (WT * 0.5f + 0.1f), Vector3.one, mWood, true, true);
                    sill.transform.rotation = Quaternion.LookRotation(inward, Vector3.up);
                    sill.transform.localScale = new Vector3(w.width + 0.24f, 0.05f, 0.24f + 0.0f);
                    sill.AddComponent<Climbable>();
                    w.sillTop = sill.transform;
                }
                // beam shafts
                var bgo = new GameObject("Beam_" + w.id);
                bgo.transform.SetParent(root, false);
                var beam = bgo.AddComponent<WindowBeam>();
                // beams start at the glass: slightly inside the wall's inner face minus frame
                beam.center = c - inward * 0.005f; beam.right = r; beam.up = Vector3.up; beam.inward = inward;
                beam.width = w.width; beam.height = w.height; beam.cols = w.panes; beam.bar = w.slit ? 0.02f : 0.05f;
                beam.splits = w.splits; if (w.beamTint.HasValue) { beam.hasTint = true; beam.tintColor = w.beamTint.Value; }
                w.beam = beam;
                if (w.slit)
                {
                    var tcol = w.tint; tcol.a = 1f;
                    beam.strength = 1.2f;
                }
            }
        }

        void BuildDoorTrim()
        {
            foreach (var kv in openings)
                foreach (var o in kv.Value)
                {
                    if (o.isWindow) continue;
                    char axis = kv.Key[0]; float c = float.Parse(kv.Key.Substring(1));
                    float mid = (o.s0 + o.s1) * 0.5f, wdt = o.s1 - o.s0;
                    var g = new GameObject("Trim_" + o.name).transform; g.SetParent(root, false);
                    void Post(float s, float y, float sw, float sh)
                    {
                        Vector3 p = axis == 'X' ? new Vector3(c, y, s) : new Vector3(s, y, c);
                        Vector3 sz = axis == 'X' ? new Vector3(0.28f, sh, sw) : new Vector3(sw, sh, 0.28f);
                        B.Box(g, "t", p, sz, mTrim, false, true);
                    }
                    Post(o.s0 - 0.03f, o.y1 * 0.5f, 0.06f, o.y1);
                    Post(o.s1 + 0.03f, o.y1 * 0.5f, 0.06f, o.y1);
                    Post(mid, o.y1 + 0.03f, wdt + 0.12f, 0.06f);
                }
        }

        // ================================================================================== exterior scenery (seen through the windows only)
        void BuildExterior()
        {
            var grass = Mats.Textured(Mats.Pattern("grass", new Color(0.30f, 0.52f, 0.24f), new Color(0.42f, 0.66f, 0.30f)), new Vector2(60, 60), Color.white, 0.05f);
            var ground = B.Quad(root, "Ground", new Vector3(9, -0.02f, 6), Vector3.up, new Vector2(400, 400), grass);
            ground.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var gc = ground.AddComponent<BoxCollider>(); gc.size = new Vector3(1, 1, 0.01f);
            // path & flower beds & fence
            var path = Mats.Lit(new Color(0.78f, 0.7f, 0.58f), 0.1f);
            B.Quad(root, "Path", new Vector3(9, -0.01f, -4.5f), Vector3.up, new Vector2(2.4f, 9f), path).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            // hills
            var hillMat = Mats.Lit(new Color(0.25f, 0.45f, 0.30f), 0.05f);
            for (int i = 0; i < 9; i++)
            {
                float a = i * 40f * Mathf.Deg2Rad;
                var h = B.Sphere(root, "Hill" + i, new Vector3(9 + Mathf.Cos(a) * 130f, -18f, 6 + Mathf.Sin(a) * 130f), new Vector3(120f, 60f + (i % 3) * 18f, 100f), hillMat, Col.None, false);
                h.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            // trees and shrubs from the character-asset library (also used as scenery)
            string[] trees = { "Art/Props/Trees/FBX/Birch_1", "Art/Props/Trees/FBX/Tree_1", "Art/Props/Trees/FBX/Pine_1", "Art/Props/Trees/FBX/Tree_5", "Art/Props/Scenery/BroadleafTree_01", "Art/Props/Scenery/Fir_01", "Art/Props/Trees/FBX/Birch_4", "Art/Props/Scenery/BroadleafTree_03" };
            var rnd = new System.Random(5);
            Vector3[] spots =
            {
                new Vector3(-7, 0, 3), new Vector3(-9, 0, 10), new Vector3(-6, 0, 18), new Vector3(3, 0, 24), new Vector3(12, 0, 22), new Vector3(24, 0, 16),
                new Vector3(26, 0, 6), new Vector3(24, 0, -4), new Vector3(15, 0, -9), new Vector3(2, 0, -8), new Vector3(-8, 0, -6), new Vector3(20, 0, 24),
                new Vector3(-14, 0, 14), new Vector3(30, 0, 13), new Vector3(8, 0, -16), new Vector3(-16, 0, 0)
            };
            for (int i = 0; i < spots.Length; i++)
                Scenery(trees[i % trees.Length], spots[i], 0.9f + (float)rnd.NextDouble() * 0.5f, rnd.Next(360));
            string[] bushes = { "Art/Props/Scenery/Bush_01_01", "Art/Props/Scenery/Bush_02_01" };
            Vector3[] bs = { new Vector3(-1.8f, 0, 2f), new Vector3(-2f, 0, 8f), new Vector3(4.5f, 0, -1.8f), new Vector3(13f, 0, -1.9f), new Vector3(19.8f, 0, 3f), new Vector3(19.5f, 0, 9.4f), new Vector3(6f, 0, 13.5f), new Vector3(10.5f, 0, 13.7f), new Vector3(16f, 0, 13.6f), new Vector3(-1.9f, 0, 11f) };
            for (int i = 0; i < bs.Length; i++) Scenery(bushes[i % 2], bs[i], 0.8f + (float)rnd.NextDouble() * 0.4f, rnd.Next(360));
            // flowers right outside the living-room bay window
            string[] fl = { "Art/Props/Scenery/FlowerMeadow_Red", "Art/Props/Scenery/FlowerMeadow_White", "Art/Props/Scenery/FlowerMeadow_PurpleRedPink" };
            for (int i = 0; i < 26; i++)
                Scenery(fl[i % 3], new Vector3(6.3f + (float)rnd.NextDouble() * 5.5f, 0, -0.7f - (float)rnd.NextDouble() * 1.6f), 1.6f + (float)rnd.NextDouble(), rnd.Next(360), false);
            for (int i = 0; i < 14; i++)
                Scenery(fl[i % 3], new Vector3(13f + (float)rnd.NextDouble() * 3.5f, 0, -0.7f - (float)rnd.NextDouble() * 1.2f), 1.6f + (float)rnd.NextDouble(), rnd.Next(360), false);
            // a low garden wall / fence for foreground interest
            var fence = Mats.Lit(new Color(0.9f, 0.88f, 0.82f), 0.1f);
            for (int i = 0; i < 20; i++) B.Box(root, "Fence", new Vector3(-3 + i * 1.2f, 0.45f, -9f), new Vector3(0.1f, 0.9f, 0.1f), fence, false, true);
            B.Box(root, "FenceRail", new Vector3(9, 0.7f, -9f), new Vector3(24f, 0.07f, 0.06f), fence, false, true);
        }

        void Scenery(string path, Vector3 pos, float scale, float yaw, bool cast = true)
        {
            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null) return;
            var g = Spawn(prefab, root);
            g.transform.position = pos; g.transform.rotation = Quaternion.Euler(0, yaw, 0); g.transform.localScale = Vector3.one * scale;
            FixMaterials.Scenery(g, path);
            foreach (var r in g.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = cast ? ShadowCastingMode.On : ShadowCastingMode.Off;
            foreach (var c in g.GetComponentsInChildren<Collider>()) c.enabled = false;
        }
    }
}
