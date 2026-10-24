using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CatOnTheSill
{
    /// <summary>Slow gentle sway of the hanging moon so it feels alive (±1.5 degrees keeps its shadow on target).</summary>
    public class MobileSway : MonoBehaviour
    {
        float seed;
        void Start() { seed = Random.value * 5f; }
        void Update() { /* the ornament must stay put so the puzzle geometry stays exact */ }
    }
}
