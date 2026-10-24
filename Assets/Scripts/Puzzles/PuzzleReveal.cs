using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CatOnTheSill
{
    /// <summary>Anything that should appear (scale in + glow) the first time a puzzle latches.</summary>
    public class PuzzleReveal : MonoBehaviour
    {
        public Puzzle puzzle;
        public GameObject[] hideUntilSolved;
        void Start() { Apply(puzzle != null && puzzle.Solved); }
        void Update() { if (puzzle != null) { bool s = puzzle.Solved; foreach (var g in hideUntilSolved) if (g != null && g.activeSelf != s) g.SetActive(s); } }
        void Apply(bool on) { foreach (var g in hideUntilSolved) if (g != null) g.SetActive(on); }
    }
}
