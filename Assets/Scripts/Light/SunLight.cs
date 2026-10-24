using System.Collections.Generic;
using UnityEngine;

namespace CatOnTheSill
{
    /// <summary>
    /// The single source of truth for "where is the light coming from". Every puzzle asks this class whether a
    /// point is lit - exactly the same question the real directional-light shadow map answers - so what the
    /// player sees (light patches, shadows) always matches the puzzle logic.
    /// </summary>
    public static class SunLight
    {
        public static Vector3 ToSun = Times.ToSun(TimeOfDay.Morning);   // unit vector pointing from the world TOWARD the sun / moon
        public static bool IsMoon;
        public static float Strength = 1f;                              // 0..1 overall brightness
        public static int Version;                                      // bumps whenever the direction changes

        static readonly RaycastHit[] buf = new RaycastHit[32];

        public static Vector3 Travel => -ToSun;

        public static void Set(Vector3 toSun, bool moon, float strength)
        {
            if ((toSun - ToSun).sqrMagnitude > 1e-8f) Version++;
            ToSun = toSun.normalized; IsMoon = moon; Strength = strength;
        }

        /// <summary>True when light reaches point p (on a surface facing normal n) unobstructed.</summary>
        public static bool IsLit(Vector3 p, Vector3 n)
        {
            if (Vector3.Dot(n, ToSun) < 0.02f) return false;
            return !Physics.Raycast(p + n * 0.02f, ToSun, 80f, Layer.OccluderMask, QueryTriggerInteraction.Ignore);
        }

        /// <summary>
        /// Shadow test used by shadow puzzles. Returns true when the ray from p to the light is blocked by at
        /// least one of the accepted casters, and by nothing else except other movable props
        /// (i.e. the point would be sunlit if the casters were absent).
        /// </summary>
        public static bool ShadowedBy(Vector3 p, Vector3 n, IList<MovableObject> accepted, ICollection<Collider> staticCasters = null)
        {
            if (Vector3.Dot(n, ToSun) < 0.02f) return false;
            int count = Physics.RaycastNonAlloc(p + n * 0.02f, ToSun, buf, 80f, Layer.OccluderMask, QueryTriggerInteraction.Ignore);
            bool casterHit = false;
            for (int i = 0; i < count; i++)
            {
                var col = buf[i].collider;
                if (staticCasters != null && staticCasters.Contains(col)) { casterHit = true; continue; }
                var m = col.GetComponentInParent<MovableObject>();
                if (m == null) return false;                    // static geometry blocks the light: not a legitimate lit surface
                if (accepted == null || accepted.Contains(m)) casterHit = true;
            }
            return casterHit;
        }

        /// <summary>Projects point p along the light travel direction onto a plane (plane point + normal).</summary>
        public static bool ProjectOntoPlane(Vector3 p, Vector3 planePoint, Vector3 planeNormal, out Vector3 result)
        {
            Vector3 d = Travel;
            float denom = Vector3.Dot(d, planeNormal);
            result = p;
            if (Mathf.Abs(denom) < 1e-5f) return false;
            float t = Vector3.Dot(planePoint - p, planeNormal) / denom;
            if (t < 0) return false;
            result = p + d * t;
            return true;
        }

        public static Vector3 ProjectOntoPlane(Vector3 p, Vector3 toSun, Vector3 planePoint, Vector3 planeNormal)
        {
            Vector3 d = -toSun;
            float denom = Vector3.Dot(d, planeNormal);
            if (Mathf.Abs(denom) < 1e-5f) return p;
            float t = Vector3.Dot(planePoint - p, planeNormal) / denom;
            return p + d * t;
        }
    }
}
