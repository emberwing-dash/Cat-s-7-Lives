using UnityEngine;

namespace CatOnTheSill
{
    public partial class WorldBuilder
    {
        // ====================================================================================== READING ROOM  (Fragment 7 - The House Remembers)
        // The little room between the Study and the Sunroom. Two props must be carried onto their floor marks so the
        // sleeping-cat carving on the west wall can wake. Works at any hour, like every other puzzle: no fixed order.
        void BuildReadingFinale()
        {
            var fern = MovablePlant("LibraryFern", new Vector3(6.9f, 0, 10.9f), "read_plant").GetComponent<MovableObject>();
            var lamp = MovableLamp("LibraryLamp", new Vector3(10.4f, 0, 8.6f), "read_lamp").GetComponent<MovableObject>();
            foreach (var m in new[] { fern, lamp }) { m.xRange = new Vector2(6.5f, 11.5f); m.zRange = new Vector2(8.35f, 11.2f); }

            Solutions["read_plant"] = new Vector3(10.9f, 0, 10.6f);
            Solutions["read_lamp"] = new Vector3(7.4f, 0, 10.4f);
            var amber = new Color(1f, 0.62f, 0.25f); var etch = new Color(0.7f, 0.34f, 0.06f);
            MakeGlyph("FernReadingMark", Solutions["read_plant"] + Vector3.up * 0.012f, Vector3.up, GlyphKind.Tree, 1.0f, GlyphMode.Light, amber, etch).enabled = false;
            MakeGlyph("LampReadingMark", Solutions["read_lamp"] + Vector3.up * 0.012f, Vector3.up, GlyphKind.Star, 1.0f, GlyphMode.Light, amber, etch).enabled = false;
            AddPawTrail("ReadingPawsFern", new Vector3(9.0f, 0f, 8.4f), Solutions["read_plant"], 6);
            AddPawTrail("ReadingPawsLamp", new Vector3(9.0f, 0f, 8.4f), Solutions["read_lamp"], 6);

            // the sleeping cat carved into the west wall: it glows as the house wakes
            var emblemMat = Mats.Blend(new Color(1f, 0.9f, 0.65f, 0.15f), Mats.Glyph(GlyphKind.Cat), Mats.BlendKind.Additive);
            var emb = B.Quad(root, "CatEmblem", new Vector3(6.104f, 1.35f, 10.3f), Vector3.right, new Vector2(2.2f, 2.2f), emblemMat);
            B.Quad(root, "CatEtch", new Vector3(6.102f, 1.35f, 10.3f), Vector3.right, new Vector2(2.2f, 2.2f), Mats.Blend(new Color(0.32f, 0.22f, 0.15f, 0.22f), Mats.Glyph(GlyphKind.Cat), Mats.BlendKind.Alpha));

            var p7go = new GameObject("Puzzle7"); p7go.transform.SetParent(root, false);
            var p7 = p7go.AddComponent<PlacementPuzzle>();
            p7.first = fern; p7.firstPosition = Solutions["read_plant"];
            p7.second = lamp; p7.secondPosition = Solutions["read_lamp"];
            p7.radius = 0.8f;
            p7.id = "p7"; p7.fragmentIndex = 6; p7.title = FragmentInfo.Titles[6]; p7.roomName = "Library"; p7.roomBounds = Rooms["Library"];
            p7.requiredHold = 0.45f; p7.revealPos = new Vector3(9.0f, 1.1f, 9.1f); p7.landPos = new Vector3(9.0f, 0.5f, 9.1f);
            p7.hints = new[]
            {
                "Follow the pawprints to the two floor marks.",
                "Push the fern onto the tree mark and the lamp onto the star mark.",
                "That is all: fern on the tree, lamp on the star."
            };
            var ce = emb.AddComponent<CatEmblem>(); ce.finalPuzzle = p7;
            Puzzles.Add(p7);
        }
    }
}
