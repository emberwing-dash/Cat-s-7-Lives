using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CatOnTheSill
{
    /// <summary>One window in the house. 'center' etc. describe the opening on the inner face of the wall.</summary>
    public class WindowSpec
    {
        public string id;
        public Vector3 center;           // centre of the opening on the inner wall face
        public Vector3 inward;           // unit vector pointing into the room
        public Vector3 right;            // unit vector = up x inward
        public float width, height;
        public int panes = 1;
        public float[] splits;           // optional unequal pane boundaries as fractions 0..1 across the width (excluding 0 and 1)
        public Color? beamTint;
        public Color tint = new Color(0.75f, 0.88f, 1f, 0.07f);
        public WindowBeam beam;
        public BeamEmitter emitter;
        public bool slit;
        public Transform sillTop;
        public Transform frameRoot;
    }
}
