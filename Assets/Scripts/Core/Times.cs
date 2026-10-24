using UnityEngine;

namespace CatOnTheSill
{
    public static class Times
    {
        public static readonly TimeData[] All =
        {
            new TimeData { name = "Sunrise",  blurb = "First light, low and gold.",           az = -35f,  el = 9f,  sunColor = new Color(1f, 0.62f, 0.40f),  sunIntensity = 1.9f, ambient = new Color(0.74f, 0.64f, 0.68f), skyTint = new Color(0.95f, 0.62f, 0.55f), ground = new Color(0.45f, 0.40f, 0.36f), exposure = 1.2f, moon = false, lampLevel = 0.3f },
            new TimeData { name = "Morning",  blurb = "Warm sun slanting from the south-east.", az = -40f,  el = 22f, sunColor = new Color(1f, 0.86f, 0.66f),  sunIntensity = 2.5f, ambient = new Color(0.80f, 0.76f, 0.72f), skyTint = new Color(0.55f, 0.68f, 0.95f), ground = new Color(0.45f, 0.45f, 0.40f), exposure = 1.3f, moon = false, lampLevel = 0f },
            new TimeData { name = "Noon",     blurb = "Bright, high, almost overhead.",        az = -95f,  el = 58f, sunColor = new Color(1f, 0.97f, 0.92f),  sunIntensity = 3.0f, ambient = new Color(0.82f, 0.84f, 0.88f), skyTint = new Color(0.45f, 0.62f, 1f),    ground = new Color(0.50f, 0.50f, 0.46f), exposure = 1.4f, moon = false, lampLevel = 0f },
            new TimeData { name = "Sunset",   blurb = "Long orange shadows from the west.",    az = -150f, el = 14f, sunColor = new Color(1f, 0.52f, 0.26f),  sunIntensity = 2.2f, ambient = new Color(0.72f, 0.60f, 0.60f), skyTint = new Color(0.95f, 0.50f, 0.35f), ground = new Color(0.42f, 0.34f, 0.32f), exposure = 1.15f, moon = false, lampLevel = 0.35f },
            new TimeData { name = "Night",    blurb = "A cool moon rising in the north-east.", az = 35f,   el = 38f, sunColor = new Color(0.52f, 0.66f, 1f),  sunIntensity = 1.5f, ambient = new Color(0.34f, 0.38f, 0.58f), skyTint = new Color(0.10f, 0.12f, 0.30f), ground = new Color(0.06f, 0.07f, 0.12f), exposure = 0.55f, moon = true, lampLevel = 1f },
            new TimeData { name = "Midnight", blurb = "The moon has crossed to the north-west.", az = 145f, el = 52f, sunColor = new Color(0.46f, 0.60f, 1f),  sunIntensity = 1.4f, ambient = new Color(0.30f, 0.34f, 0.54f), skyTint = new Color(0.07f, 0.09f, 0.26f), ground = new Color(0.05f, 0.06f, 0.10f), exposure = 0.5f, moon = true, lampLevel = 1f },
        };

        public static TimeData Get(TimeOfDay t) => All[(int)t];

        /// <summary>Unit vector from the world toward the sun / moon.</summary>
        public static Vector3 ToSun(float azDeg, float elDeg)
        {
            float az = azDeg * Mathf.Deg2Rad, el = elDeg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(el) * Mathf.Cos(az), Mathf.Sin(el), Mathf.Cos(el) * Mathf.Sin(az));
        }

        public static Vector3 ToSun(TimeOfDay t) { var d = All[(int)t]; return ToSun(d.az, d.el); }
    }
}
