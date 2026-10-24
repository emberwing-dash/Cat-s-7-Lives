using System.Collections.Generic;
using UnityEngine;

namespace CatOnTheSill
{
    public partial class WorldBuilder
    {
        Transform CounterBlock(string name, float x0, float x1, float z0, float z1, float h = 0.92f, Material body = null)
        {
            body = body ?? mWhite;
            var g = Group(name, new Vector3((x0 + x1) * 0.5f, 0, (z0 + z1) * 0.5f), 0, true);
            Part(g, "Body", new Vector3(0, (h - 0.04f) * 0.5f, 0), new Vector3(x1 - x0, h - 0.04f, z1 - z0), body);
            Part(g, "Top", new Vector3(0, h - 0.02f, 0), new Vector3(x1 - x0 + 0.03f, 0.04f, z1 - z0 + 0.03f), Mats.Lit(new Color(0.55f, 0.58f, 0.6f), 0.5f), true);
            return g;
        }

        /// <summary>The kitchen's silver tray: a movable stand with a shiny plate that can be turned 15 degrees at a time.</summary>
        Transform BuildTray(Vector3 tablePos, string id, int startStep, float tiltDeg, out Reflector refl, out RotatableObject rot)
        {
            var g = Group("SilverTray", tablePos, 0, false);
            var pivot = B.Empty(g, "Pivot", Vector3.zero).transform;
            var wood = mWoodDark;
            var baseBox = Part(pivot, "Stand", new Vector3(0, 0.03f, 0), new Vector3(0.26f, 0.06f, 0.2f), wood);
            var brace = Part(pivot, "Brace", new Vector3(0, 0.12f, -0.07f), new Vector3(0.05f, 0.2f, 0.04f), wood, false);
            var silver = Mats.Silver(new Color(0.88f, 0.9f, 0.94f));
            var plate = B.Box(pivot, "Plate", new Vector3(0, 0.2f, 0.02f), new Vector3(0.44f, 0.32f, 0.018f), silver, true, true);
            plate.transform.localRotation = Quaternion.Euler(-tiltDeg, 0, 0);
            var rimMat = Mats.Metal(new Color(0.7f, 0.72f, 0.76f), 0.8f);
            B.Box(plate.transform, "RimT", new Vector3(0, 0.5f, -0.5f), new Vector3(1.04f, 0.03f, 1f), rimMat, false, true).transform.localScale = new Vector3(1.04f, 0.03f, 1.4f);
            B.Box(plate.transform, "RimB", new Vector3(0, -0.5f, -0.5f), new Vector3(1.04f, 0.03f, 1f), rimMat, false, true).transform.localScale = new Vector3(1.04f, 0.03f, 1.4f);
            refl = g.gameObject.AddComponent<Reflector>();
            refl.plate = plate.transform; refl.plateCollider = plate.GetComponent<Collider>();
            rot = g.gameObject.AddComponent<RotatableObject>();
            rot.id = id; rot.pivot = pivot; rot.label = "silver tray"; rot.stepIndex = startStep; rot.range = 0.9f; rot.needsFacing = false;
            var mv = g.gameObject.AddComponent<MovableObject>();
            mv.id = id; mv.displayName = "silver tray";
            return g;
        }

        // ====================================================================================== KITCHEN  (Fragment 3 - The Silver Tray)
        void BuildKitchen()
        {
            var win = Windows["kit_s"];
            // --- cabinetry along the south wall (window above the sink)
            CounterBlock("Stove", 12.2f, 13.15f, 0.12f, 0.75f, 0.92f, mMetal);
            for (int i = 0; i < 4; i++)
                B.Cyl(root, "Burner", new Vector3(12.45f + (i % 2) * 0.4f, 0.945f, 0.3f + (i / 2) * 0.28f), 0.08f, 0.012f, Mats.Lit(new Color(0.1f, 0.1f, 0.1f), 0.4f), Col.None);
            CounterBlock("Sink", 13.15f, 14.7f, 0.12f, 0.72f, 0.92f, mWhite);
            Part(Group("Basin", new Vector3(13.9f, 0.93f, 0.4f), 0), "b", Vector3.zero, new Vector3(0.6f, 0.03f, 0.4f), mMetal, false);
            Part(Group("Tap", new Vector3(13.9f, 1.05f, 0.18f), 0), "t", Vector3.zero, new Vector3(0.04f, 0.2f, 0.04f), mMetal, false);
            CounterBlock("Counter", 14.7f, 16.7f, 0.12f, 0.72f, 0.92f, mWhite);
            var fridge = Group("Fridge", new Vector3(17.3f, 0, 0.55f), 0, false);
            Part(fridge, "Body", new Vector3(0, 0.95f, 0), new Vector3(1.0f, 1.9f, 0.8f), Mats.Lit(new Color(0.85f, 0.9f, 0.92f), 0.5f));
            Part(fridge, "Seam", new Vector3(0, 1.3f, 0.41f), new Vector3(0.98f, 0.015f, 0.01f), mMetal, false);
            Part(fridge, "Handle", new Vector3(0.38f, 1.55f, 0.43f), new Vector3(0.03f, 0.35f, 0.03f), mMetal, false);
            Part(fridge, "Handle2", new Vector3(0.38f, 0.8f, 0.43f), new Vector3(0.03f, 0.35f, 0.03f), mMetal, false);
            // jars, herbs, kettle on the counter (decor)
            for (int i = 0; i < 4; i++)
                B.Cyl(root, "Jar", new Vector3(15.1f + i * 0.38f, 1.04f, 0.3f), 0.07f, 0.2f, Mats.Lit(BookColors[(i + 3) % BookColors.Length], 0.5f), Col.None);
            Plant("HerbPlant", new Vector3(14.1f, 0.92f, 0.62f), 0.45f, true);
            B.Cyl(root, "Kettle", new Vector3(12.7f, 1.06f, 0.3f), 0.1f, 0.22f, mBrass, Col.None);
            // hanging pots on the north wall
            for (int i = 0; i < 4; i++)
            {
                B.Cyl(root, "Pot", new Vector3(12.55f + i * 0.45f, 1.9f, 5.72f), 0.1f, 0.12f, Mats.Metal(new Color(0.55f, 0.55f, 0.6f), 0.7f), Col.None);
            }
            B.Box(root, "PotRail", new Vector3(13.3f, 2.1f, 5.82f), new Vector3(1.9f, 0.03f, 0.03f), mBrass, false, true);
            Cabinet("NorthCabinet", new Vector3(13.4f, 0, 5.62f), 180f, 2.0f, 0.92f, 0.5f, mWhite, true);
            // dining table
            Table("DiningTable", new Vector3(15.45f, 0, 3.7f), 0f, 1.5f, 0.75f, 0.9f, mWoodLight, mWood);
            Chair("DiningChair1", new Vector3(14.9f, 0, 4.5f), 180f, mWood);
            Chair("DiningChair2", new Vector3(16.0f, 0, 4.55f), 175f, mWood);
            Chair("DiningChair3", new Vector3(14.9f, 0, 2.9f), 5f, mWood);
            Rug("KitchenRug", new Vector3(15.4f, 0, 3.7f), 2.4f, 1.8f, new Color(0.45f, 0.62f, 0.55f), new Color(0.95f, 0.9f, 0.75f));
            B.Cyl(root, "FruitBowl", new Vector3(15.45f, 0.78f, 3.7f), 0.14f, 0.06f, mFabric1, Col.None);
            B.Sphere(root, "Apple", new Vector3(15.42f, 0.84f, 3.7f), 0.08f, mFabric1, Col.None);
            Picture("KitchenPic", new Vector3(12.11f, 1.8f, 2.2f), Vector3.right, 0.55f, 0.7f, new Color(0.85f, 0.7f, 0.4f));
            TableLamp("KitchenLamp", new Vector3(16.3f, 0.92f, 0.45f), 1.2f);
            AddSkyWindowTrigger(win, "kitchen window");
            // a curtain pair that never blocks the opening
            var kc = Mats.Textured(Mats.Pattern("stripes", new Color(0.95f, 0.85f, 0.45f), new Color(0.88f, 0.77f, 0.38f), 128), new Vector2(3, 1), Color.white, 0.1f);
            Part(Group("KCurtL", new Vector3(13.35f, 1.85f, 0.26f), 0), "c", Vector3.zero, new Vector3(0.3f, 1.6f, 0.08f), kc, false);
            Part(Group("KCurtR", new Vector3(15.85f, 1.85f, 0.26f), 0), "c", Vector3.zero, new Vector3(0.3f, 1.6f, 0.08f), kc, false);

            // ---------------------------------------------------------------- PUZZLE 3: reflect the morning beam into the high cupboard
            Vector3 trayPos = new Vector3(13.3f, 0.8f, 1.65f);           // where the tray must stand
            float tilt = 18f; int solutionStep = 7;                       // 7 * 15 = 105 degrees
            var table = Table("TrayTable", new Vector3(13.35f, 0, 1.7f), 0f, 1.4f, 0.8f, 0.8f, mWoodLight, mWood);
            var tray = BuildTray(new Vector3(13.85f, 0.8f, 1.9f), "kit_tray", 10, tilt, out var refl, out var rot);
            var trayMv = tray.GetComponent<MovableObject>();
            trayMv.xRange = new Vector2(12.9f, 13.85f); trayMv.zRange = new Vector2(1.5f, 1.95f);
            Chair("TrayChair", new Vector3(13.35f, 0, 2.55f), 180f, mWood);
            Stool("KitchenStool", new Vector3(12.6f, 0, 2.7f), 0.46f);
            Stool("KitchenStool2", new Vector3(14.2f, 0, 2.7f), 0.46f);

            // where does the reflected beam land when the tray stands at trayPos with the solution orientation?
            Vector3 Ldir = -SunAt(TimeOfDay.Morning);
            Quaternion q = Quaternion.Euler(0, solutionStep * 15f, 0) * Quaternion.Euler(-tilt, 0, 0);
            Vector3 n = q * Vector3.forward;
            Vector3 centre = trayPos + new Vector3(0, 0.2f, 0);
            Vector3 d = Vector3.Reflect(Ldir, n).normalized;
            float tt = (17.9f - centre.x) / d.x;
            Vector3 hit = centre + d * tt;
            float cz = Mathf.Clamp(hit.z, 1.6f, 4.3f), cy = Mathf.Clamp(hit.y, 1.45f, 2.2f);

            Solutions["kit_tray"] = new Vector3(trayPos.x, 0.8f, trayPos.z);
            // east-wall counter + high wall cupboard with a glass sensor in the middle
            CounterBlock("EastCounter", 17.25f, 17.88f, Mathf.Max(1.0f, cz - 1.3f), cz + 1.3f, 0.92f, mWhite);
            var cab = Group("WallCupboard", new Vector3(17.9f - 0.19f, cy, cz), 270f, false);   // local +Z faces west (into the room)
            var cabRoot = cab.gameObject.AddComponent<OpenableCabinet>();
            float w = 1.3f, h = 1.0f, dep = 0.36f;
            Part(cab, "Back", new Vector3(0, 0, -dep * 0.5f + 0.02f), new Vector3(w, h, 0.04f), mWoodDark);
            Part(cab, "Top", new Vector3(0, h * 0.5f - 0.02f, 0), new Vector3(w, 0.04f, dep), mWoodDark);
            Part(cab, "Bottom", new Vector3(0, -h * 0.5f + 0.02f, 0), new Vector3(w, 0.04f, dep), mWoodDark);
            Part(cab, "SideL", new Vector3(-w * 0.5f + 0.02f, 0, 0), new Vector3(0.04f, h, dep), mWoodDark);
            Part(cab, "SideR", new Vector3(w * 0.5f - 0.02f, 0, 0), new Vector3(0.04f, h, dep), mWoodDark);
            Part(cab, "Inside", new Vector3(0, 0, -dep * 0.5f + 0.05f), new Vector3(w - 0.1f, h - 0.1f, 0.01f), Mats.Lit(new Color(0.95f, 0.9f, 0.7f), 0.1f), false);
            Part(cab, "Shelf", new Vector3(0, -0.1f, 0), new Vector3(w - 0.1f, 0.03f, dep - 0.08f), mWood, false);
            Part(cab, "CentreColumn", new Vector3(0, 0, dep * 0.5f - 0.02f), new Vector3(0.34f, h, 0.05f), mWood);
            // doors on pivots
            var dl = B.Empty(cab, "DoorLPivot", new Vector3(-w * 0.5f + 0.02f, 0, dep * 0.5f + 0.005f)).transform;
            Part(dl, "Door", new Vector3((w * 0.5f - 0.19f) * 0.5f, 0, 0.012f), new Vector3(w * 0.5f - 0.19f, h - 0.02f, 0.025f), mWoodLight, true);
            var dr = B.Empty(cab, "DoorRPivot", new Vector3(w * 0.5f - 0.02f, 0, dep * 0.5f + 0.005f)).transform;
            Part(dr, "Door", new Vector3(-(w * 0.5f - 0.19f) * 0.5f, 0, 0.012f), new Vector3(w * 0.5f - 0.19f, h - 0.02f, 0.025f), mWoodLight, true);
            Part(dl, "Knob", new Vector3(w * 0.5f - 0.24f, 0, 0.04f), new Vector3(0.025f, 0.12f, 0.025f), mBrass, false);
            Part(dr, "Knob", new Vector3(-(w * 0.5f - 0.24f), 0, 0.04f), new Vector3(0.025f, 0.12f, 0.025f), mBrass, false);
            cabRoot.doorL = dl; cabRoot.doorR = dr;
            // glass star sensor on the centre column
            var sensorGo = new GameObject("StarSensor"); sensorGo.transform.SetParent(root, false);
            sensorGo.transform.position = new Vector3(17.9f - 0.19f - 0.18f - 0.05f, cy, cz);   // just in front of the centre column
            sensorGo.layer = Layer.Sensor;
            var sbox = sensorGo.AddComponent<BoxCollider>(); sbox.isTrigger = true; sbox.size = new Vector3(0.1f, 0.7f, 0.34f);
            var sensor = sensorGo.AddComponent<LightSensor>();
            var starDisc = B.Quad(sensorGo.transform, "Star", Vector3.right * 0.03f, Vector3.left, new Vector2(0.3f, 0.3f), Mats.Blend(new Color(0.8f, 0.9f, 1f, 0.95f), Mats.Glyph(GlyphKind.Star), Mats.BlendKind.Alpha));
            var starGlow = B.Quad(sensorGo.transform, "StarGlow", Vector3.right * 0.026f, Vector3.left, new Vector2(0.48f, 0.48f), Mats.Blend(new Color(0.8f, 0.9f, 1f, 0f), Mats.Glyph(GlyphKind.Star), Mats.BlendKind.Additive));
            var glow = sensorGo.AddComponent<SensorGlow>(); glow.sensor = sensor; glow.glowRenderer = starGlow.GetComponent<MeshRenderer>(); glow.color = new Color(0.85f, 0.93f, 1f);
            AddLightClue("KitchenLightClue", new Vector3(17.84f, cy + 0.48f, cz), Vector3.left, "MORNING  •  SILVER TURNS LIGHT", false, false, 0.11f);

            var p3go = new GameObject("Puzzle3"); p3go.transform.SetParent(root, false);
            var p3 = p3go.AddComponent<SensorPuzzle>();
            p3.sensors.Add(sensor);
            p3.id = "p3"; p3.fragmentIndex = 2; p3.title = FragmentInfo.Titles[2]; p3.roomName = "Kitchen"; p3.roomBounds = Rooms["Kitchen"];
            p3.requiredHold = 0.9f;
            p3.revealPos = new Vector3(17.6f, cy - 0.1f, cz); p3.landPos = new Vector3(17.4f, 1.28f, cz);
            p3.hints = new[]
            {
                "Something shiny in this kitchen can throw light where light does not usually go.",
                "The silver tray catches the window's beam. Turn it with E / Q and slide it along the table until a bright line points at the glass star on the high cupboard.",
                "Light bounces off the tray at the same angle it arrives. Stand the tray in the sunlit patch (the beam only reaches the west end of the table) and turn it toward the east wall."
            };
            p3.cabinet = cabRoot;
            // sunbeam emitter for reflected light (bundle of sample rays across the window)
            var em = new GameObject("KitchenEmitter"); em.transform.SetParent(root, false);
            var be = em.AddComponent<BeamEmitter>();
            be.center = win.center - win.inward * 0.0f; be.right = win.right; be.up = Vector3.up; be.inward = win.inward;
            be.width = win.width - 0.2f; be.height = win.height - 0.2f; be.cols = 7; be.rows = 4; be.color = BeamColor.White;
            Puzzles.Add(p3);
        }
    }
}

