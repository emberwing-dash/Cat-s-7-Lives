using UnityEngine;

namespace CatOnTheSill
{
    /// <summary>Physics layers (unnamed layers are fine - we only use their numbers).</summary>
    public static class Layer
    {
        public const int Cat = 8;
        public const int Sensor = 9;
        public const int Npc = 10;
        public const int Curtain = 11;      // hangs in windows: blocks light and beams but never blocks the cat
        public const int Structure = 13;    // walls, floors, ceilings: the only things the camera collides with

        /// <summary>Everything that blocks sunlight / moonlight (walls, furniture, movable props).</summary>
        public const int OccluderMask = (1 << 0) | (1 << 11) | (1 << 13);
        /// <summary>Reflected beams stop at occluders and are received by sensors.</summary>
        public const int BeamMask = (1 << 0) | (1 << 9) | (1 << 11) | (1 << 13);
        public const int CameraMask = 1 << 13;
        /// <summary>Solid things the cat can stand on / be blocked by (furniture + structure).</summary>
        public const int SolidMask = (1 << 0) | (1 << 13);
        public const int MovableBlockMask = (1 << 0) | (1 << 10) | (1 << 13);
    }
}
