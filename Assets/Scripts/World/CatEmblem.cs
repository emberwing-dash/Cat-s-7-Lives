using System.Collections.Generic;
using UnityEngine;

namespace CatOnTheSill
{
    /// <summary>The cat-shaped carving on the living-room wall: it brightens with every fragment recovered.</summary>
    public class CatEmblem : MonoBehaviour
    {
        public Puzzle finalPuzzle;
        Material glow;
        float pulse;
        void Start() { glow = GetComponent<Renderer>().material; }
        void Update()
        {
            pulse += Time.deltaTime;
            float f = Game.Collected / 7f;
            float a = (finalPuzzle != null && finalPuzzle.Solved ? 0.9f : 0.12f + 0.3f * f) * (0.85f + 0.15f * Mathf.Sin(pulse * 1.5f));
            if (glow != null) glow.SetColor("_Color", new Color(1f, 0.9f, 0.65f, a));
        }
    }
}
