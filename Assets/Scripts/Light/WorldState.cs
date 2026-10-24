using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CatOnTheSill
{
    /// <summary>Bumped whenever anything that can block light moves, so cached beams rebuild.</summary>
    public static class WorldState
    {
        public static int Version;
        public static void Dirty() { Version++; }
    }
}
