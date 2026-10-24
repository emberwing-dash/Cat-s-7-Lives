using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CatOnTheSill
{
    public partial class WorldBuilder
    {
        // ====================================================================================== HALL (library hub)
        void BuildHall()
        {
            var win = Windows["hall_n"];
            Bookshelf("HallShelfL", new Vector3(6.9f, 0, 11.68f), 180f, 1.3f, 2.4f, 0.4f, 5, 41, false);
            Bookshelf("HallShelfR", new Vector3(11.1f, 0, 11.68f), 180f, 1.5f, 2.4f, 0.4f, 5, 42, false);
            Bookshelf("HallLowShelf", new Vector3(9.0f, 0, 11.7f), 180f, 1.5f, 0.55f, 0.35f, 2, 43, true);
            Armchair("HallArmchair", new Vector3(7.3f, 0, 8.75f), 140f, mFabric1);
            Table("HallRoundTable", new Vector3(9.0f, 0, 10.0f), 0f, 1.0f, 0.7f, 1.0f, mWoodLight, mWood);
            B.Cyl(root, "TableVase", new Vector3(9.0f, 0.82f, 10.0f), 0.07f, 0.24f, Mats.Lit(new Color(0.4f, 0.65f, 0.75f), 0.6f), Col.None);
            BookStack("HallBooks", new Vector3(8.7f, 0.7f, 10.15f), 4, 0.24f, 0.18f, 9, false);
            Rug("HallRug", new Vector3(9.0f, 0, 10.0f), 2.8f, 2.2f, new Color(0.4f, 0.5f, 0.65f), new Color(0.9f, 0.8f, 0.55f));
            Plant("HallFern", new Vector3(11.4f, 0, 8.6f), 1f, true);
            FloorLamp("HallLamp", new Vector3(6.55f, 0, 8.55f), 1.5f, false, 1.7f);
            // a soft bed for the cat
            var bed = Group("CatBed", new Vector3(11.1f, 0, 9.05f), 0, true);
            B.Cyl(bed, "Rim", new Vector3(0, 0.07f, 0), 0.34f, 0.14f, mFabric1, Col.Box);
            B.Cyl(bed, "Pad", new Vector3(0, 0.12f, 0), 0.26f, 0.05f, mFabric3, Col.None);
            Picture("HallPic1", new Vector3(6.11f, 1.9f, 9.0f), Vector3.right, 0.6f, 0.45f, new Color(0.5f, 0.6f, 0.7f));
            Picture("HallPic2", new Vector3(11.89f, 1.9f, 8.9f), Vector3.left, 0.6f, 0.45f, new Color(0.85f, 0.6f, 0.45f));
            var hc = AddCover("hall_curtain", win, CoverKind.Curtains, new Color(0.45f, 0.5f, 0.75f), 2, new[] { 2, 0 });
            AddCord(hc, new Vector3(10.6f, 2.9f, 11.62f), 2.4f);
            AddSkyWindowTrigger(win, "library window");
            BuildReadingFinale();
        }

        // ====================================================================================== SUNROOM + ATTIC
        Transform MirrorStand(string name, float x, float z, float centreY, float thetaDeg, float tilt, bool rotatable, string id, int step, out Reflector refl, out RotatableObject rot)
        {
            var g = Group(name, new Vector3(x, 0, z), 0, false);
            var gold = mBrass;
            B.Cyl(g, "Foot", new Vector3(0, 0.025f, 0), 0.2f, 0.05f, gold, Col.None);
            Part(g, "FootCol", new Vector3(0, 0.03f, 0), new Vector3(0.36f, 0.06f, 0.36f), gold, true).GetComponent<MeshRenderer>().enabled = false;
            B.Cyl(g, "Pole", new Vector3(0, (centreY - 0.3f) * 0.5f, 0), 0.022f, centreY - 0.3f, gold, Col.None);
            Part(g, "PoleCol", new Vector3(0, (centreY - 0.3f) * 0.5f, 0), new Vector3(0.05f, centreY - 0.3f, 0.05f), gold, true).GetComponent<MeshRenderer>().enabled = false;
            var pivot = B.Empty(g, "Pivot", new Vector3(0, centreY, 0)).transform;
            var plate = B.Box(pivot, "Plate", Vector3.zero, new Vector3(0.5f, 0.55f, 0.03f), Mats.Silver(new Color(0.8f, 0.9f, 1f)), true, true);
            plate.transform.localRotation = Quaternion.Euler(-tilt, 0, 0);
            var frame = Mats.Metal(new Color(0.85f, 0.68f, 0.35f), 0.7f);
            B.Box(plate.transform, "FT", new Vector3(0, 0.5f, -0.5f), Vector3.one, frame, false, true).transform.localScale = new Vector3(1.08f, 0.06f, 2.2f);
            B.Box(plate.transform, "FB", new Vector3(0, -0.5f, -0.5f), Vector3.one, frame, false, true).transform.localScale = new Vector3(1.08f, 0.06f, 2.2f);
            B.Box(plate.transform, "FL", new Vector3(-0.5f, 0, -0.5f), Vector3.one, frame, false, true).transform.localScale = new Vector3(0.07f, 1.0f, 2.2f);
            B.Box(plate.transform, "FR", new Vector3(0.5f, 0, -0.5f), Vector3.one, frame, false, true).transform.localScale = new Vector3(0.07f, 1.0f, 2.2f);
            refl = g.gameObject.AddComponent<Reflector>();
            refl.plate = plate.transform; refl.plateCollider = plate.GetComponent<Collider>();
            rot = null;
            if (rotatable)
            {
                rot = g.gameObject.AddComponent<RotatableObject>();
                rot.id = id; rot.pivot = pivot; rot.label = "mirror"; rot.stepIndex = step; rot.range = 0.95f; rot.needsFacing = false;
            }
            else pivot.localRotation = Quaternion.Euler(0, thetaDeg, 0);
            g.gameObject.AddComponent<Climbable>();
            return g;
        }

        GameObject DecoyLanternAt(string name, Vector3 pos, Vector3 normal, out LightSensor sensor)
        {
            var go = new GameObject(name); go.transform.SetParent(root, false);
            go.transform.position = pos;
            go.layer = Layer.Sensor;
            var bc = go.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(0.4f, 0.4f, 0.4f);
            sensor = go.AddComponent<LightSensor>(); sensor.acceptAny = true;
            var body = B.Cyl(go.transform, "Body", normal * 0.12f, 0.1f, 0.26f, mBrass, Col.None);
            var glass = B.Cyl(go.transform, "Glass", normal * 0.12f, 0.075f, 0.2f, Mats.Emissive(new Color(1f, 0.6f, 0.35f), 0.2f), Col.None, false);
            B.Quad(go.transform, "Plate", normal * 0.01f, normal, new Vector2(0.36f, 0.36f), Mats.Blend(new Color(0.3f, 0.2f, 0.12f, 0.6f), Mats.Glyph(GlyphKind.Ring), Mats.BlendKind.Alpha));
            var lg = new GameObject("L"); lg.transform.SetParent(go.transform, false); lg.transform.localPosition = normal * 0.25f;
            var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = 3f; l.color = new Color(1f, 0.6f, 0.35f); l.intensity = 0; l.shadows = LightShadows.None;
            var dl = go.AddComponent<DecoyLantern>(); dl.sensor = sensor; dl.glass = glass.GetComponent<Renderer>(); dl.lamp = l;
            return go;
        }

        void BuildSunroom()
        {
            // ------------------------------------------------------------------ the staircase up to the attic loft (west wall)
            const float stepRise = 0.245f, stepRun = 0.28f;
            var stairs = Group("AtticStairs", Vector3.zero, 0, false);
            for (int i = 0; i < 11; i++)
            {
                float top = stepRise * (i + 1);
                float z0 = 6.3f + i * stepRun;
                var st = B.BoxMinMax(stairs, "Step" + i, new Vector3(12.1f, 0, z0), new Vector3(13.1f, top, z0 + stepRun), mWoodLight, true, true);
            }
            // handrail posts (decor)
            for (int i = 0; i < 11; i += 2)
                B.Box(stairs, "Post", new Vector3(13.15f, stepRise * (i + 1) + 0.4f, 6.3f + i * stepRun + 0.14f), new Vector3(0.04f, 0.8f, 0.04f), mWoodDark, false, true);
            B.Box(stairs, "Rail", new Vector3(13.15f, 1.65f, 7.9f), new Vector3(0.05f, 0.05f, 3.3f), mWoodDark, false, true).transform.rotation = Quaternion.Euler(-Mathf.Atan2(stepRise * 10, stepRun * 10) * Mathf.Rad2Deg, 0, 0) * Quaternion.identity;

            // loft floor
            float loftTop = 2.7f;
            B.BoxMinMax(root, "LoftFloor", new Vector3(12.1f, loftTop - 0.2f, 9.4f), new Vector3(17.9f, loftTop, 11.9f), mWoodLight, true, true);
            // low rail at the loft's edge (leaves the stair landing open)
            B.BoxMinMax(root, "LoftRail", new Vector3(13.4f, loftTop, 9.4f), new Vector3(17.9f, loftTop + 0.42f, 9.46f), mWoodDark, true, true);
            // rafters
            for (int i = 0; i < 4; i++) B.Box(root, "Rafter", new Vector3(13.2f + i * 1.5f, 6.45f, 9.0f), new Vector3(0.14f, 0.16f, 5.6f), mWoodDark, false, true);
            B.Box(root, "CrossBeam", new Vector3(15.0f, 6.45f, 11.45f), new Vector3(5.8f, 0.16f, 0.14f), mWoodDark, false, true);

            // ------------------------------------------------------------------ decorative paw plate (the stairs to the attic are open: any puzzle can be done first)
            var plateGo = new GameObject("PawPlate"); plateGo.transform.SetParent(root, false);
            plateGo.transform.position = new Vector3(15.25f, 0, 6.95f);
            var pbox = plateGo.AddComponent<BoxCollider>(); pbox.isTrigger = true; pbox.center = new Vector3(0, 0.15f, 0); pbox.size = new Vector3(0.6f, 0.4f, 0.6f);
            var prb = plateGo.AddComponent<Rigidbody>(); prb.isKinematic = true; prb.useGravity = false;
            var pv = B.Cyl(plateGo.transform, "Disc", new Vector3(0, 0.02f, 0), 0.28f, 0.04f, mBrass, Col.None, false);
            B.Quad(plateGo.transform, "Paw", new Vector3(0, 0.043f, 0), Vector3.up, new Vector2(0.34f, 0.34f), Mats.Blend(new Color(0.25f, 0.15f, 0.08f, 0.9f), Mats.Glyph(GlyphKind.Paw), Mats.BlendKind.Alpha));
            var plate = plateGo.AddComponent<PressurePlate>(); plate.id = "gate_plate"; plate.latch = true; plate.visual = pv.transform;

            // ------------------------------------------------------------------ sunroom furniture & plants
            Table("TeaTable", new Vector3(16.7f, 0, 7.35f), 0f, 0.9f, 0.7f, 0.9f, mWhite, mWood);
            B.Cyl(root, "Teapot", new Vector3(16.7f, 0.8f, 7.35f), 0.08f, 0.14f, Mats.Lit(new Color(0.9f, 0.9f, 0.95f), 0.7f), Col.None);
            Chair("TeaChair1", new Vector3(16.7f, 0, 6.65f), 0f, mWoodLight);
            Chair("TeaChair2", new Vector3(17.35f, 0, 7.4f), 270f, mWoodLight);
            Cabinet("SunBench", new Vector3(13.7f, 0, 6.4f), 0f, 1.3f, 0.45f, 0.45f, mFabric3, true);
            Plant("SunFern1", new Vector3(17.45f, 0, 11.35f), 1.5f, true);
            Plant("SunFern2", new Vector3(17.35f, 0, 6.5f), 1.1f, true);
            Plant("SunFern3", new Vector3(12.55f, 0, 11.45f), 1.4f, true);
            Plant("SunFern4", new Vector3(16.5f, 0, 11.5f), 1.0f, true);
            Plant("SunFern5", new Vector3(14.1f, 0, 6.5f), 0.8f, true);
            Plant("SunFern6", new Vector3(12.55f, 0, 10.2f), 1.1f, true);
            Plant("SunFern7", new Vector3(13.4f, 0, 11.5f), 0.9f, true);
            Rug("SunRug", new Vector3(15.8f, 0, 8.3f), 2.4f, 2.0f, new Color(0.55f, 0.7f, 0.5f), new Color(0.95f, 0.9f, 0.7f));
            FloorLamp("SunLamp", new Vector3(17.5f, 0, 8.7f), 1.5f, false, 1.6f);
            // hanging lamp in the double-height room
            var hl = Group("HangLamp", new Vector3(15.0f, 4.7f, 8.0f), 0, false);
            B.Cyl(hl, "Cord", new Vector3(0, 0.85f, 0), 0.01f, 1.7f, mBrass, Col.None, false);
            var hshade = B.Sphere(hl, "Globe", Vector3.zero, 0.36f, LampShadeMat(), Col.None, false);
            var hlg = new GameObject("L"); hlg.transform.SetParent(hl, false);
            var hli = hlg.AddComponent<Light>(); hli.type = LightType.Point; hli.range = 8f; hli.color = new Color(1f, 0.85f, 0.6f); hli.shadows = LightShadows.None; hli.intensity = 0;
            var hll = hl.gameObject.AddComponent<LampLight>(); hll.lightSource = hli; hll.bulb = hshade.GetComponent<Renderer>(); hll.maxIntensity = 3.2f;
            // sun-room sky window (north glass wall)
            AddSkyWindowTrigger(Windows["sun_n"], "sunroom window");
            var sunCurt = Mats.Textured(Mats.Pattern("stripes", new Color(0.95f, 0.95f, 0.85f), new Color(0.88f, 0.9f, 0.8f), 128), new Vector2(3, 1), Color.white, 0.1f);
            Part(Group("SunCurtL", new Vector3(13.15f, 1.7f, 11.7f), 0), "c", Vector3.zero, new Vector3(0.3f, 2.2f, 0.08f), sunCurt, false);
            Part(Group("SunCurtR", new Vector3(16.85f, 1.7f, 11.7f), 0), "c", Vector3.zero, new Vector3(0.3f, 2.2f, 0.08f), sunCurt, false);

            // ================================================================== PUZZLE 6 - the false reflection (sunrise)
            BuildMirrorPuzzle();
            // ================================================================== PUZZLE 5 - the moving moon (attic loft)
            BuildAtticPuzzle();
        }

        void BuildMirrorPuzzle()
        {
            Vector3 L = -SunAt(TimeOfDay.Sunrise);
            // beam emitters on the three stained-glass slits
            foreach (var kv in new[] { ("sun_e_gold", BeamColor.Gold), ("sun_e_red", BeamColor.Red), ("sun_e_blue", BeamColor.Blue) })
            {
                var w = Windows[kv.Item1];
                var go = new GameObject("Emit_" + kv.Item1); go.transform.SetParent(root, false);
                var be = go.AddComponent<BeamEmitter>();
                be.center = w.center; be.right = w.right; be.up = Vector3.up; be.inward = w.inward;
                be.width = w.width; be.height = w.height - 0.1f; be.cols = 5; be.rows = 3; be.color = kv.Item2;
            }
            // --- the gold path: slit -> M1 -> M2 -> T
            Vector3 goldO = new Vector3(17.9f, 1.65f, 7.0f);
            float m1x = 13.6f;
            float t1 = (goldO.x - m1x) / -L.x;
            Vector3 m1p = goldO + L * t1;
            float tilt = 6f;
            int solStep1 = 6, solStep2 = 16;               // yaw = step * 15 degrees (90 and 240); target stays visible on the west wall
            Vector3 N(float th) => Quaternion.Euler(0, th, 0) * (Quaternion.Euler(-tilt, 0, 0) * Vector3.forward);
            Vector3 d1 = Vector3.Reflect(L, N(solStep1 * 15f)).normalized;
            float hd = new Vector2(d1.x, d1.z).magnitude;
            Vector3 m2p = m1p + d1 * (2.2f / hd);
            Vector3 d2 = Vector3.Reflect(d1, N(solStep2 * 15f)).normalized;
            float tT = (12.1f - m2p.x) / d2.x;
            Vector3 tp = m2p + d2 * tT;

            var m1 = MirrorStand("MirrorNear", m1p.x, m1p.z, m1p.y, 0, tilt, true, "sun_m1", 4, out var r1, out var rot1);
            var m2 = MirrorStand("MirrorFar", m2p.x, m2p.z, m2p.y, 0, tilt, true, "sun_m2", 20, out var r2, out var rot2);
            Solutions["sun_m1_step"] = new Vector3(solStep1, 0, 0); Solutions["sun_m2_step"] = new Vector3(solStep2, 0, 0);

            // --- the true target: a moon-lily carving on the west wall that blooms when the gold beam arrives
            var tgo = new GameObject("LilySensor"); tgo.transform.SetParent(root, false);
            tgo.transform.position = new Vector3(12.1f + 0.05f, tp.y, tp.z);
            tgo.layer = Layer.Sensor;
            var tb = tgo.AddComponent<BoxCollider>(); tb.isTrigger = true; tb.size = new Vector3(0.14f, 0.5f, 0.5f);
            var tsen = tgo.AddComponent<LightSensor>(); tsen.acceptAny = false; tsen.accepts = BeamColor.Gold;
            B.Quad(tgo.transform, "Etch", new Vector3(-0.045f, 0, 0), Vector3.right, new Vector2(0.72f, 0.72f), Mats.Blend(new Color(0.42f, 0.28f, 0.10f, 0.9f), Mats.Glyph(GlyphKind.Diamond), Mats.BlendKind.Alpha));
            var bloomRoot = B.Empty(tgo.transform, "Bloom", new Vector3(0.05f, 0, 0)).transform;
            var petal = Mats.Emissive(new Color(0.85f, 0.95f, 1f), 1.6f);
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f;
                var pt = B.Sphere(bloomRoot, "Petal", Quaternion.Euler(0, 0, a) * new Vector3(0, 0.09f, 0.0f), new Vector3(0.07f, 0.2f, 0.07f), petal, Col.None, false);
                pt.transform.localRotation = Quaternion.Euler(0, 0, a);
            }
            B.Sphere(bloomRoot, "Heart", Vector3.zero, 0.1f, Mats.Emissive(new Color(1f, 0.85f, 0.4f), 2f), Col.None, false);
            var bl = new GameObject("BloomLight"); bl.transform.SetParent(bloomRoot, false); bl.transform.localPosition = new Vector3(0.12f, 0, 0);
            var bli = bl.AddComponent<Light>(); bli.type = LightType.Point; bli.color = new Color(0.8f, 0.9f, 1f); bli.range = 4f; bli.intensity = 2.5f; bli.shadows = LightShadows.None;
            bloomRoot.localRotation = Quaternion.Euler(0, 90, 0);   // petals face into the room
            var hb = tgo.AddComponent<HiddenBloom>(); hb.sensor = tsen; hb.bloom = bloomRoot;
            bloomRoot.gameObject.SetActive(false);
            AddLightClue("SunroomGoldClue", new Vector3(12.16f, tp.y + 0.52f, tp.z), Vector3.right, "SUNRISE  •  FOLLOW GOLD", false, false, 0.13f);

            var p6go = new GameObject("Puzzle6"); p6go.transform.SetParent(root, false);
            var p6 = p6go.AddComponent<SensorPuzzle>();
            p6.sensors.Add(tsen);
            p6.id = "p6"; p6.fragmentIndex = 5; p6.title = FragmentInfo.Titles[5]; p6.roomName = "Sunroom"; p6.roomBounds = Rooms["Sunroom"];
            p6.requiredHold = 0.45f; p6.revealPos = new Vector3(12.7f, tp.y + 0.2f, tp.z); p6.landPos = new Vector3(12.9f, 0.75f, tp.z);
            hb.puzzle = p6;
            p6.hints = new[]
            {
                "Three stained-glass slits throw coloured beams across this room at sunrise. Not every glittering path leads anywhere.",
                "Only the gold beam matters. The mirrors on their stands can be turned (E / Q): the near one passes the beam to the far one, the far one to the west wall.",
                "Turn the near mirror until the gold line crosses to the far mirror, then turn the far mirror until the line reaches the faint diamond carving low on the west wall. The red lanterns are decoys."
            };
            Puzzles.Add(p6);

            // --- decoys: the first mirror pointing the wrong way, and a red beam bouncing off a fixed mirror
            Vector3 d1alt = Vector3.Reflect(L, N(4 * 15f)).normalized;
            Vector3 alt = WallHit(m1p, d1alt);
            DecoyLanternAt("DecoyGold", alt + new Vector3(0, 0, -0.02f), Vector3.back, out var dsen1);
            Vector3 redO = new Vector3(17.9f, 1.65f, 8.0f);
            float tr = (redO.x - 16.0f) / -L.x; Vector3 rp = redO + L * tr;
            var rm = MirrorStand("MirrorRed", rp.x, rp.z, rp.y, 150f, 0f, false, "sun_r1", 0, out var rr, out var rrot);
            Vector3 dRed = Vector3.Reflect(L, rr.Normal).normalized;
            Vector3 redHit = WallHit(rp, dRed);
            Vector3 redN = redHit.z < 6.2f ? Vector3.forward : (redHit.x < 12.2f ? Vector3.right : (redHit.x > 17.8f ? Vector3.left : Vector3.back));
            DecoyLanternAt("DecoyRed", redHit + redN * 0.02f, redN, out var dsen2);
            // a couple more shiny non-reflecting ornaments to tempt the player
            B.Sphere(root, "SilverBall", new Vector3(14.6f, 0.96f, 6.75f), 0.3f, Mats.Silver(new Color(0.9f, 0.92f, 0.95f)), Col.Sphere);
            B.Cyl(root, "BallStand", new Vector3(14.6f, 0.4f, 6.75f), 0.1f, 0.8f, mWood, Col.Box);
        }

        /// <summary>Where a ray from p along d first meets the sunroom's walls (inner faces).</summary>
        Vector3 WallHit(Vector3 p, Vector3 d)
        {
            float best = float.MaxValue;
            void Try(float val, int axis)
            {
                float comp = axis == 0 ? d.x : d.z; float pp = axis == 0 ? p.x : p.z;
                if (Mathf.Abs(comp) < 1e-5f) return;
                float t = (val - pp) / comp;
                if (t > 0.01f && t < best) best = t;
            }
            Try(12.1f, 0); Try(17.9f, 0); Try(6.1f, 1); Try(11.9f, 1);
            return p + d * best;
        }

        void BuildAtticPuzzle()
        {
            Vector3 mid = SunAt(TimeOfDay.Midnight);
            float loftTop = 2.7f;
            // --- shutters on the attic's round-ish window
            var aw = Windows["attic_n"];
            var shut = AddCover("attic_shutters", aw, CoverKind.Shutters, new Color(0.42f, 0.28f, 0.17f), 0, new[] { 2, 0 });
            AddCord(shut, new Vector3(14.1f, 5.15f, 11.7f), 2.15f);
            // frame ring decoration (moon + stars)
            B.Quad(root, "AtticRing", new Vector3(15.0f, 4.4f, 11.86f), Vector3.back, new Vector2(1.9f, 1.9f), Mats.Blend(new Color(0.55f, 0.45f, 0.3f, 0.6f), Mats.Glyph(GlyphKind.Ring), Mats.BlendKind.Alpha));
            AddSkyWindowTrigger(aw, "attic window");
            // the sky window trigger sits at sill height 3.7 - move it down to the loft floor so the cat can reach it
            var skyTrigger = GameObject.Find("SkyWindow_attic_n");
            if (skyTrigger != null) skyTrigger.transform.position = new Vector3(15.0f, loftTop, 11.45f);

            // --- the hanging mobile: a silver moon and two little stars on strings
            const float orbY = 3.4f;
            var mob = Group("Mobile", new Vector3(15.5f, 0, 11.5f), 0, false);
            B.Cyl(mob, "String", new Vector3(0, (6.45f + orbY) * 0.5f, 0), 0.006f, 6.45f - orbY, mBrass, Col.None, false);
            var moonOrnament = B.Sphere(mob, "MoonOrnament", new Vector3(0, orbY, 0), 0.36f, Mats.Silver(new Color(0.9f, 0.93f, 1f)), Col.Sphere);
            B.Sphere(mob, "Crescent", new Vector3(0.05f, orbY, 0.18f), new Vector3(0.1f, 0.3f, 0.06f), mBrass, Col.None);
            var moonCol = moonOrnament.GetComponent<Collider>();
            var starA = Group("StarA", new Vector3(14.5f, 0, 11.45f), 0, false);
            B.Cyl(starA, "S", new Vector3(0, 4.7f, 0), 0.005f, 3.5f, mBrass, Col.None, false);
            B.Sphere(starA, "Star", new Vector3(0, 3.0f, 0), 0.2f, Mats.Silver(new Color(1f, 0.9f, 0.6f)), Col.Sphere);
            var starB = Group("StarB", new Vector3(16.9f, 0, 11.5f), 0, false);
            B.Cyl(starB, "S", new Vector3(0, 4.7f, 0), 0.005f, 3.5f, mBrass, Col.None, false);
            B.Sphere(starB, "Star", new Vector3(0, 3.0f, 0), 0.2f, Mats.Silver(new Color(0.8f, 0.9f, 1f)), Col.Sphere);

            // --- where the moon-star's shadow lands at Midnight on the loft floor
            Vector3 ornamentCentre = new Vector3(15.5f, orbY, 11.5f);
            Vector3 L = -mid;
            float t = (ornamentCentre.y - loftTop) / -L.y;
            Vector3 shadow = ornamentCentre + L * t;
            Vector3 ringPos = new Vector3(shadow.x, loftTop, shadow.z);
            Solutions["attic_ring"] = ringPos;

            var ring = MakeGlyph("MoonRing", ringPos, Vector3.up, GlyphKind.Ring, 0.46f, GlyphMode.Shadow, new Color(0.7f, 0.85f, 1f), new Color(0.12f, 0.1f, 0.12f));
            ring.staticCasters.Add(moonCol);
            AddLightClue("AtticMoonClue", ringPos + new Vector3(0f, 0.018f, 0.65f), Vector3.up, "MIDNIGHT  •  BASIN ON THE RING", true, false, 0.11f);
            ring.onlyMoon = true;
            // inlaid clue glyphs beside the ring: a moon at 'Night' and a bigger moon at 'Midnight'
            B.Quad(root, "ClueNight", new Vector3(14.6f, loftTop + 0.006f, 10.3f), Vector3.up, new Vector2(0.34f, 0.34f), Mats.Blend(new Color(0.2f, 0.15f, 0.12f, 0.55f), Mats.Glyph(GlyphKind.Moon), Mats.BlendKind.Alpha));
            B.Quad(root, "ClueMid", new Vector3(ringPos.x - 0.55f, loftTop + 0.006f, ringPos.z - 0.2f), Vector3.up, new Vector2(0.28f, 0.28f), Mats.Blend(new Color(0.2f, 0.15f, 0.12f, 0.55f), Mats.Glyph(GlyphKind.Star), Mats.BlendKind.Alpha));

            // --- the little moon basin (movable)
            var bowl = Group("MoonBasin", new Vector3(13.7f, loftTop, 10.4f), 0, false);
            B.Cyl(bowl, "Dish", new Vector3(0, 0.05f, 0), 0.17f, 0.1f, Mats.Metal(new Color(0.75f, 0.8f, 0.9f), 0.85f), Col.None);
            B.Cyl(bowl, "Water", new Vector3(0, 0.1f, 0), 0.13f, 0.01f, Mats.Emissive(new Color(0.5f, 0.7f, 1f), 0.8f), Col.None, false);
            B.Cyl(bowl, "Rim", new Vector3(0, 0.1f, 0), 0.18f, 0.02f, mBrass, Col.None);
            var bc = bowl.gameObject.AddComponent<BoxCollider>(); bc.center = new Vector3(0, 0.03f, 0); bc.size = new Vector3(0.32f, 0.06f, 0.32f);
            var bowlMv = bowl.gameObject.AddComponent<MovableObject>();
            bowlMv.id = "attic_bowl"; bowlMv.displayName = "moon basin"; bowlMv.xRange = new Vector2(12.6f, 17.5f); bowlMv.zRange = new Vector2(9.9f, 11.6f); bowlMv.pushSpeed = 0.8f;
            ring.casters.Add(bowlMv);

            var p5go = new GameObject("Puzzle5"); p5go.transform.SetParent(root, false);
            var p5 = p5go.AddComponent<GlyphPuzzle>();
            p5.glyphs.Add(ring);
            p5.nearObject = bowlMv; p5.nearPos = ringPos; p5.nearRadius = 0.3f;
            p5.id = "p5"; p5.fragmentIndex = 4; p5.title = FragmentInfo.Titles[4]; p5.roomName = "Attic"; p5.roomBounds = new Bounds(new Vector3(15f, 3.9f, 10.7f), new Vector3(6f, 3.4f, 2.6f));
            p5.requiredHold = 0.45f; p5.revealPos = ringPos + Vector3.up * 0.5f; p5.landPos = ringPos + Vector3.up * 0.38f;
            p5.hints = new[]
            {
                "The moon does not stay put. Open the shutters on the round window (pull the cord) and watch where its light lands as the night wears on.",
                "A hanging silver moon casts a shadow that wanders across the floor. A pale ring is carved where the shadow stops - but only at one hour.",
                "Carry the little basin onto the carved ring, set the clock to Midnight, and let the moon-star's shadow fall across it."
            };
            Puzzles.Add(p5);
        }
    }
}






