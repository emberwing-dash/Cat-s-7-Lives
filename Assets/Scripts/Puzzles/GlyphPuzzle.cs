using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CatOnTheSill
{
    /// <summary>A puzzle satisfied when all of its glyph targets are satisfied (sunbeam and shadow puzzles).</summary>
    public class GlyphPuzzle : Puzzle
    {
        public List<GlyphTarget> glyphs = new List<GlyphTarget>();
        public MovableObject nearObject;                  // optional: this prop must also sit within nearRadius of nearPos (xz)
        public Vector3 nearPos; public float nearRadius;

        protected override bool Evaluate(out float closeness)
        {
            if (glyphs.Count == 0) { closeness = 0; return false; }
            float sum = 0; bool all = true;
            foreach (var g in glyphs)
            {
                sum += g.Satisfied ? 1f : g.Fraction * 0.5f;
                if (!g.Satisfied) all = false;
            }
            closeness = sum / glyphs.Count;
            if (nearObject != null)
            {
                Vector3 b = nearObject.transform.position;
                if (Vector2.Distance(new Vector2(b.x, b.z), new Vector2(nearPos.x, nearPos.z)) >= nearRadius) { all = false; closeness = Mathf.Min(closeness, 0.9f); }
            }
            return all;
        }
    }
}
