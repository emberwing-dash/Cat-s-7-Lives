using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CatOnTheSill
{
    /// <summary>A rectangular aperture that sends sample rays into the room (one per grid cell).</summary>
    public class BeamEmitter : MonoBehaviour
    {
        public Vector3 center, right, up, inward;
        public float width, height;
        public int cols = 3, rows = 2;
        public BeamColor color = BeamColor.White;
        void OnEnable() { BeamSystem.Emitters.Add(this); }
        void OnDisable() { BeamSystem.Emitters.Remove(this); }

        public static Color ToColor(BeamColor c)
        {
            switch (c)
            {
                case BeamColor.Gold: return new Color(1f, 0.82f, 0.35f);
                case BeamColor.Red: return new Color(1f, 0.35f, 0.3f);
                case BeamColor.Blue: return new Color(0.4f, 0.6f, 1f);
                default: return new Color(1f, 0.95f, 0.85f);
            }
        }
    }
}
