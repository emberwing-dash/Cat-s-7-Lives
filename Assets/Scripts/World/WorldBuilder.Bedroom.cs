using System.Collections.Generic;
using UnityEngine;

namespace CatOnTheSill
{
    public partial class WorldBuilder
    {
        // ====================================================================================== BEDROOM  (Fragment 1 - The First Ray)
        void BuildBedroom()
        {
            // --- furniture
            Bed("Bed", new Vector3(1.15f, 0, 4.85f), 180f);
            Cabinet("Nightstand", new Vector3(2.35f, 0, 5.62f), 0f, 0.45f, 0.5f, 0.4f, mWood, true);
            TableLamp("BedLamp", new Vector3(2.35f, 0.5f, 5.62f));
            Desk("StudyDesk", new Vector3(0.485f, 0, 2.5f), 90f, 2.0f, 0.75f, 0.75f);
            Chair("DeskChair", new Vector3(1.3f, 0, 2.55f), 270f);
            Cabinet("WindowBench", new Vector3(3.0f, 0, 0.38f), 0f, 2.3f, 0.42f, 0.5f, mFabric3, true);
            Part(Group("BenchCushion", new Vector3(3.0f, 0.43f, 0.38f), 0f, false), "c", Vector3.zero, new Vector3(2.2f, 0.08f, 0.46f), mFabric1, false);
            Wardrobe("Wardrobe", new Vector3(5.58f, 0, 1.45f), 270f);
            Bookshelf("BedShelf", new Vector3(3.3f, 0, 5.7f), 180f, 1.3f, 1.3f, 0.36f, 3, 7);
            Rug("BedRug", new Vector3(3.2f, 0, 3.3f), 2.5f, 1.9f, new Color(0.55f, 0.72f, 0.78f), new Color(0.95f, 0.88f, 0.7f));
            Picture("PicFamily", new Vector3(5.88f, 1.6f, 3.2f), Vector3.left, 0.7f, 0.55f, new Color(0.55f, 0.7f, 0.5f));
            Picture("PicMorning", new Vector3(1.15f, 1.75f, 5.88f), Vector3.back, 0.9f, 0.6f, new Color(0.95f, 0.78f, 0.55f));
            Picture("PicCat", new Vector3(4.6f, 1.55f, 0.12f), Vector3.forward, 0.5f, 0.6f, new Color(0.9f, 0.55f, 0.35f));
            Plant("BedPlant", new Vector3(5.45f, 0, 5.4f), 0.9f, true);
            Stool("BedStool", new Vector3(1.15f, 0, 0.85f), 0.4f);
            // desk trinkets (decor only)
            Part(Group("DeskMug", new Vector3(0.4f, 0.75f, 3.3f), 0), "mug", new Vector3(0, 0.05f, 0), new Vector3(0.08f, 0.1f, 0.08f), mWhite, false);
            B.Cyl(Group("DeskLampBase", new Vector3(0.3f, 0.75f, 3.1f), 0), "base", new Vector3(0, 0.03f, 0), 0.08f, 0.06f, mBrass, Col.None);

            // --- window: curtains with a gap, cord
            var w = Windows["bed_s"];
            var curtain = AddCover("bed_curtain", w, CoverKind.Curtains, new Color(0.85f, 0.45f, 0.42f), 1, new[] { 2, 0, 1 }, -0.85f, -0.15f);
            AddCord(curtain, new Vector3(4.42f, 2.0f, 0.3f), 1.55f);
            AddSkyWindowTrigger(w, "bedroom window");

            // --- puzzle: the book stack hides the sun mark
            var books = BookStack("BedBooks", new Vector3(0.45f, 0.75f, 1.82f), 8, 0.3f, 0.24f, 11, true, "bed_books");
            var mv = books.GetComponent<MovableObject>();
            mv.xRange = new Vector2(0.3f, 0.62f); mv.zRange = new Vector2(1.7f, 3.3f); mv.displayName = "stack of books";

            var glyphGo = new GameObject("SunMark");
            glyphGo.transform.SetParent(root, false);
            glyphGo.transform.position = new Vector3(0.1f, 0.95f, 2.1f);
            var glyph = glyphGo.AddComponent<GlyphTarget>();
            glyph.mode = GlyphMode.Light; glyph.normal = Vector3.right; glyph.kind = GlyphKind.Sun;
            glyph.glowColor = new Color(1f, 0.82f, 0.4f); glyph.etchColor = new Color(0.45f, 0.3f, 0.18f);
            glyph.Build(0.36f);

            var pz = glyphGo.AddComponent<GlyphPuzzle>();
            pz.glyphs.Add(glyph);
            pz.id = "p1"; pz.fragmentIndex = 0; pz.title = FragmentInfo.Titles[0]; pz.roomName = "Bedroom";
            pz.roomBounds = Rooms["Bedroom"]; pz.autoReveal = false; pz.requiredHold = 0.6f;
            pz.revealPos = new Vector3(0.65f, 1.05f, 2.1f);
            pz.hints = new[]
            {
                "Some lights arrive only at certain hours. Watch where the sun slips through the curtains.",
                "A faint carving on the west wall is waiting for a ray - but something on the desk is in the way.",
                "Climb onto the desk and push the stack of books along it, out of the sunbeam."
            };
            var mark = glyphGo.AddComponent<RevealMark>(); mark.puzzle = pz;
            var col = glyphGo.AddComponent<BoxCollider>(); col.isTrigger = true; col.size = new Vector3(0.6f, 0.6f, 0.6f);
            Puzzles.Add(pz);
        }
    }
}
