using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CatOnTheSill
{
    /// <summary>A target that lights up when a reflected beam of the right colour strikes it.</summary>
    public class LightSensor : MonoBehaviour
    {
        public bool acceptAny = true;
        public BeamColor accepts = BeamColor.White;
        float lastHit = -10f;
        public bool Lit => Time.time - lastHit < 0.3f;
        public void Hit(BeamColor c) { if (acceptAny || c == accepts) lastHit = Time.time; }
    }
}
