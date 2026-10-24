using UnityEngine;

namespace CatOnTheSill
{
    public struct TimeData
    {
        public string name;
        public string blurb;
        public float az, el;           // azimuth (deg from +X toward +Z) and elevation of the light source
        public Color sunColor;
        public float sunIntensity;
        public Color ambient;
        public Color skyTint;
        public Color ground;
        public float exposure;
        public bool moon;
        public float lampLevel;        // how bright indoor lamps are
    }
}
