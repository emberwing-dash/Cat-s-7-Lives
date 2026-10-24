using UnityEngine;
using UnityEngine.Rendering;

namespace CatOnTheSill
{
    /// <summary>Tiny static helpers that turn primitives into furniture.</summary>
    public static class B
    {
        public static GameObject Empty(Transform parent, string name, Vector3 localPos)
        {
            var g = new GameObject(name);
            if (parent != null) g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos;
            return g;
        }

        static GameObject Make(Mesh mesh, Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat, Col col, bool cast, bool receive)
        {
            var g = new GameObject(name);
            if (parent != null) g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos;
            g.transform.localScale = scale;
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = g.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = cast ? ShadowCastingMode.On : ShadowCastingMode.Off;
            r.receiveShadows = receive;
            switch (col)
            {
                case Col.Box: g.AddComponent<BoxCollider>(); break;
                case Col.Sphere: g.AddComponent<SphereCollider>(); break;
                case Col.Capsule: g.AddComponent<CapsuleCollider>(); break;
            }
            return g;
        }

        /// <summary>Centered box. size is full extents.</summary>
        public static GameObject Box(Transform parent, string name, Vector3 localPos, Vector3 size, Material mat, bool collide = true, bool cast = true)
            => Make(Mats.Cube, parent, name, localPos, size, mat, collide ? Col.Box : Col.None, cast, true);

        /// <summary>Box described by world-space min / max corners (parent should be at identity or the world root).</summary>
        public static GameObject BoxMinMax(Transform parent, string name, Vector3 min, Vector3 max, Material mat, bool collide = true, bool cast = true)
            => Box(parent, name, (min + max) * 0.5f, max - min, mat, collide, cast);

        public static GameObject Sphere(Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat, Col col = Col.None, bool cast = true)
            => Make(Mats.Sphere, parent, name, localPos, scale, mat, col, cast, true);

        public static GameObject Sphere(Transform parent, string name, Vector3 localPos, float diameter, Material mat, Col col = Col.None, bool cast = true)
            => Sphere(parent, name, localPos, Vector3.one * diameter, mat, col, cast);

        /// <summary>Upright cylinder. radius / height in metres. y position is the centre.</summary>
        public static GameObject Cyl(Transform parent, string name, Vector3 localPos, float radius, float height, Material mat, Col col = Col.Box, bool cast = true)
        {
            var g = Make(Mats.Cylinder, parent, name, localPos, new Vector3(radius * 2f, height * 0.5f, radius * 2f), mat, Col.None, cast, true);
            if (col == Col.Box) g.AddComponent<BoxCollider>().size = new Vector3(1, 2, 1) * 1f;      // unit cylinder mesh is 1 wide, 2 tall
            else if (col == Col.Capsule) g.AddComponent<CapsuleCollider>();
            else if (col == Col.Sphere) g.AddComponent<SphereCollider>();
            if (col == Col.Box) { var bc = g.GetComponent<BoxCollider>(); bc.size = new Vector3(1f, 2f, 1f); }
            return g;
        }

        public static GameObject Pyramid(Transform parent, string name, Vector3 localBasePos, Vector3 scale, Material mat, bool collide = false)
            => Make(Mats.Pyramid, parent, name, localBasePos, scale, mat, collide ? Col.Box : Col.None, true, true);

        /// <summary>A flat quad lying on a surface. normal = the direction the visible face points.</summary>
        public static GameObject Quad(Transform parent, string name, Vector3 localPos, Vector3 normal, Vector2 size, Material mat, bool collide = false, float rollDeg = 0f)
        {
            var g = Make(Mats.Quad, parent, name, localPos, new Vector3(size.x, size.y, 1), mat, collide ? Col.Box : Col.None, false, true);
            // Unity's Quad faces -Z when unrotated (visible from -Z side)... its front faces +Z? (Quad normal is -Z) so look opposite.
            Vector3 up = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
            g.transform.rotation = Quaternion.LookRotation(-normal, up) * Quaternion.Euler(0, 0, rollDeg);
            return g;
        }

        public static void Tag<T>(GameObject g) where T : Component { g.AddComponent<T>(); }

        public static void SetLayerRecursive(GameObject g, int layer)
        {
            g.layer = layer;
            foreach (Transform t in g.transform) SetLayerRecursive(t.gameObject, layer);
        }

        public static Bounds RendererBounds(GameObject g)
        {
            var rs = g.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(g.transform.position, Vector3.zero);
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }
    }
}
