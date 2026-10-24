using System.Collections.Generic;
using UnityEngine;

namespace CatOnTheSill
{
    public partial class WorldBuilder
    {
        Transform DeskObject(string name, string id, Vector3 deskPos, int kind)
        {
            // kind 0 = globe, 1 = inkwell, 2 = music box. Root sits on the desk top.
            var g = Group(name, deskPos, 0, false);
            var mv = g.gameObject.AddComponent<MovableObject>();
            mv.id = id;
            switch (kind)
            {
                case 0:
                {
                    B.Cyl(g, "Foot", new Vector3(0, 0.02f, 0), 0.07f, 0.04f, mBrass, Col.None);
                    B.Cyl(g, "Stem", new Vector3(0, 0.07f, 0), 0.012f, 0.08f, mBrass, Col.None);
                    var globe = B.Sphere(g, "Globe", new Vector3(0, 0.2f, 0), 0.22f, Mats.Lit(new Color(0.25f, 0.55f, 0.75f), 0.4f), Col.None);
                    B.Sphere(g, "Land", new Vector3(0.03f, 0.22f, 0.07f), 0.12f, Mats.Lit(new Color(0.35f, 0.65f, 0.35f), 0.3f), Col.None);
                    var sc = g.gameObject.AddComponent<SphereCollider>(); sc.center = new Vector3(0, 0.2f, 0); sc.radius = 0.11f;
                    var bc = g.gameObject.AddComponent<BoxCollider>(); bc.center = new Vector3(0, 0.04f, 0); bc.size = new Vector3(0.14f, 0.08f, 0.14f);
                    mv.displayName = "globe"; break;
                }
                case 1:
                {
                    // a tall-necked ink bottle (its slim neck throws a long, readable shadow)
                    B.Cyl(g, "Ink", new Vector3(0, 0.05f, 0), 0.08f, 0.1f, Mats.Lit(new Color(0.12f, 0.1f, 0.25f), 0.7f), Col.None);
                    B.Cyl(g, "Neck", new Vector3(0, 0.2f, 0), 0.05f, 0.2f, Mats.Lit(new Color(0.15f, 0.13f, 0.3f), 0.7f), Col.None);
                    B.Cyl(g, "Stopper", new Vector3(0, 0.32f, 0), 0.04f, 0.04f, mBrass, Col.None);
                    var q = B.Cyl(g, "Quill", new Vector3(0.06f, 0.36f, 0), 0.004f, 0.18f, mWhite, Col.None); q.transform.localRotation = Quaternion.Euler(0, 0, -22);
                    var b1 = g.gameObject.AddComponent<BoxCollider>(); b1.center = new Vector3(0, 0.05f, 0); b1.size = new Vector3(0.16f, 0.1f, 0.16f);
                    var b2 = g.gameObject.AddComponent<BoxCollider>(); b2.center = new Vector3(0, 0.2f, 0); b2.size = new Vector3(0.1f, 0.2f, 0.1f);
                    mv.displayName = "ink bottle"; break;
                }
                default:
                {
                    Part(g, "Box", new Vector3(0, 0.05f, 0), new Vector3(0.22f, 0.1f, 0.15f), mWoodDark, false);
                    Part(g, "Lid", new Vector3(0, 0.105f, 0), new Vector3(0.23f, 0.02f, 0.16f), mWood, false);
                    Part(g, "Crank", new Vector3(0.12f, 0.06f, 0), new Vector3(0.03f, 0.03f, 0.03f), mBrass, false);
                    Part(g, "Dancer", new Vector3(0, 0.23f, 0), new Vector3(0.1f, 0.25f, 0.1f), Mats.Lit(new Color(0.95f, 0.75f, 0.8f), 0.3f), false);
                    B.Sphere(g, "DancerHead", new Vector3(0, 0.37f, 0), 0.08f, Mats.Lit(new Color(0.96f, 0.82f, 0.72f), 0.3f), Col.None);
                    var b1 = g.gameObject.AddComponent<BoxCollider>(); b1.center = new Vector3(0, 0.055f, 0); b1.size = new Vector3(0.23f, 0.11f, 0.16f);
                    var b2 = g.gameObject.AddComponent<BoxCollider>(); b2.center = new Vector3(0, 0.235f, 0); b2.size = new Vector3(0.1f, 0.25f, 0.1f);
                    mv.displayName = "music box"; break;
                }
            }
            mv.pushSpeed = 0.7f;
            mv.xRange = new Vector2(1.45f, 4.2f); mv.zRange = new Vector2(11.12f, 11.6f);
            return g;
        }

        // ====================================================================================== STUDY & LIBRARY  (Fragment 4 - Three Shadows, at night)
        void BuildStudy()
        {
            var win = Windows["study_n"];
            // library walls
            Bookshelf("StudyShelfW1", new Vector3(0.32f, 0, 6.95f), 90f, 1.3f, 2.3f, 0.42f, 5, 31, false);
            Bookshelf("StudyShelfW2", new Vector3(0.32f, 0, 10.1f), 90f, 1.4f, 2.3f, 0.42f, 5, 32, false);
            Bookshelf("StudyShelfN", new Vector3(0.78f, 0, 11.68f), 180f, 1.2f, 2.3f, 0.4f, 5, 33, false);
            Bookshelf("StudyShelfE", new Vector3(5.68f, 0, 7.8f), 270f, 3.0f, 2.2f, 0.42f, 5, 34, false);
            Bookshelf("StudyLowShelf", new Vector3(5.1f, 0, 11.68f), 180f, 1.3f, 1.2f, 0.4f, 3, 35, true);
            Armchair("ReadingChair", new Vector3(1.4f, 0, 8.55f), 90f, mFabric1);
            Cabinet("ReadingSide", new Vector3(0.6f, 0, 8.55f), 90f, 0.5f, 0.55f, 0.4f, mWood, true);
            TableLamp("ReadingLamp", new Vector3(0.6f, 0.55f, 8.55f), 1.6f);
            Table("ReadingTable", new Vector3(3.5f, 0, 7.7f), 0f, 1.2f, 0.72f, 0.7f, mWoodLight, mWood);
            B.Cyl(root, "Candle", new Vector3(3.5f, 0.78f, 7.7f), 0.03f, 0.12f, mWhite, Col.None);
            Rug("StudyRug", new Vector3(3.2f, 0, 8.1f), 3.0f, 2.2f, new Color(0.45f, 0.3f, 0.35f), new Color(0.85f, 0.75f, 0.5f));
            Chair("DeskChair2", new Vector3(2.8f, 0, 10.25f), 180f, mWoodDark);
            Stool("LibraryStool", new Vector3(4.75f, 0, 10.55f), 0.45f);
            // Keep the east wall clear; the old circle/triangle chalkboard competed with the puzzle clues.
            Picture("StudyMap", new Vector3(3.2f, 2.3f, 6.11f), Vector3.forward, 1.3f, 0.5f, new Color(0.75f, 0.7f, 0.5f));
            // west window (decor, a sill to sit on)
            AddSkyWindowTrigger(win, "study window");
            var sw = Windows["study_w"];
            // Invisible safety glass keeps the cat inside while preserving the view and moonlight.
            var northSafety = B.Box(root, "StudyNorthSafetyGlass", new Vector3(3.2f, 1.8f, 11.98f), new Vector3(2.5f, 1.75f, 0.06f), mGlass, true, true);
            northSafety.GetComponent<MeshRenderer>().enabled = false; northSafety.layer = Layer.Structure;
            var westSafety = B.Box(root, "StudyWestSafetyGlass", new Vector3(0.02f, 1.8f, 8.5f), new Vector3(0.06f, 1.75f, 1.7f), mGlass, true, true);
            westSafety.GetComponent<MeshRenderer>().enabled = false; westSafety.layer = Layer.Structure;


            // --- the writing desk under the north window
            var desk = Desk("WritingDesk", new Vector3(2.85f, 0, 11.38f), 0f, 3.3f, 0.95f, 0.76f);
            // brass inlay plaque with a moon
            var plaque = B.Box(root, "MoonPlaque", new Vector3(4.15f, 0.77f, 11.55f), new Vector3(0.32f, 0.01f, 0.22f), mBrass, false, false);
            B.Quad(root, "MoonInlay", new Vector3(4.15f, 0.778f, 11.55f), Vector3.up, new Vector2(0.2f, 0.2f), Mats.Blend(new Color(0.15f, 0.12f, 0.1f, 0.9f), Mats.Glyph(GlyphKind.Moon), Mats.BlendKind.Alpha));

            // --- milk puzzle: slide the bottle along the writing desk to the saucer (same table, F to drag)
            var milk = Group("MilkBottle", new Vector3(1.85f, 0.76f, 11.25f), 0, false);
            B.Cyl(milk, "Bottle", new Vector3(0, 0.16f, 0), 0.11f, 0.32f, Mats.Lit(new Color(0.94f, 0.96f, 1f), 0.55f), Col.None);
            B.Cyl(milk, "Neck", new Vector3(0, 0.36f, 0), 0.065f, 0.14f, Mats.Lit(new Color(0.94f, 0.96f, 1f), 0.55f), Col.None);
            B.Cyl(milk, "Cap", new Vector3(0, 0.445f, 0), 0.07f, 0.03f, mBrass, Col.None);
            var milkCol = milk.gameObject.AddComponent<BoxCollider>(); milkCol.center = new Vector3(0, 0.2f, 0); milkCol.size = new Vector3(0.24f, 0.44f, 0.24f);
            var milkMv = milk.gameObject.AddComponent<MovableObject>(); milkMv.id = "study_milk"; milkMv.displayName = "milk bottle"; milkMv.pushSpeed = 0.8f;
            milkMv.xRange = new Vector2(1.45f, 4.2f); milkMv.zRange = new Vector2(11.12f, 11.6f);

            Vector3 saucerPos = new Vector3(4.15f, 0.79f, 11.28f);
            B.Cyl(root, "MilkSaucer", saucerPos, 0.25f, 0.035f, Mats.Lit(new Color(0.82f, 0.88f, 0.96f), 0.7f), Col.None);
            B.Cyl(root, "MilkSaucerRing", saucerPos + Vector3.up * 0.022f, 0.18f, 0.012f, mBrass, Col.None);
            AddLightClue("StudyMilkClue", saucerPos + new Vector3(0f, 0.025f, -0.38f), Vector3.up, "A MIDNIGHT TREAT", true, false, 0.1f);
            Solutions["study_milk"] = saucerPos;

            var p4go = new GameObject("Puzzle4"); p4go.transform.SetParent(root, false);
            var p4 = p4go.AddComponent<PlacementPuzzle>();
            p4.first = milkMv; p4.firstPosition = saucerPos; p4.radius = 0.42f;
            p4.id = "p4"; p4.fragmentIndex = 3; p4.title = FragmentInfo.Titles[3]; p4.roomName = "Study"; p4.roomBounds = Rooms["Study"];
            p4.requiredHold = 0.45f; p4.revealPos = saucerPos + Vector3.up * 0.55f; p4.landPos = saucerPos + Vector3.up * 0.35f;
            p4.hints = new[]
            {
                "A small saucer waits beside the moonlit desk.",
                "Something pale on the desk belongs there.",
                "Bring the milk close to the saucer and let it settle."
            };
            Puzzles.Add(p4);
        }
    }
}







