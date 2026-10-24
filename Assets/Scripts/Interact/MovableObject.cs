using System.Collections.Generic;
using UnityEngine;

namespace CatOnTheSill
{
    /// <summary>A prop that can be pushed by walking into it or dragged by holding the grab key.</summary>
    public class MovableObject : MonoBehaviour, IPersist
    {
        public static readonly List<MovableObject> All = new List<MovableObject>();

        public string id = "movable";
        public string displayName = "object";
        public Vector2 xRange = new Vector2(-1000, 1000);
        public Vector2 zRange = new Vector2(-1000, 1000);
        public float pushSpeed = 0.9f;
        public bool grabbable = true;

        Collider[] cols;
        Rigidbody rb;
        public Vector3 HomePos { get; private set; }
        public float HomeYaw { get; private set; }

        public string PersistId => "mov:" + id;

        void Awake()
        {
            All.Add(this);
            HomePos = transform.position; HomeYaw = transform.eulerAngles.y;
            rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true; rb.useGravity = false;
            Game.Register(this);
        }

        void OnDestroy() { All.Remove(this); Game.Unregister(this); }

        public Vector3 CenterOfMass => B.RendererBounds(gameObject).center;

        public Collider[] Colliders { get { if (cols == null) cols = GetComponentsInChildren<Collider>(); return cols; } }

        public Vector3 ClosestPoint(Vector3 p)
        {
            Vector3 best = transform.position; float bd = float.MaxValue;
            foreach (var c in Colliders)
            {
                if (c == null || c.isTrigger) continue;
                Vector3 q = c.ClosestPoint(p); float d = (q - p).sqrMagnitude;
                if (d < bd) { bd = d; best = q; }
            }
            return best;
        }

        /// <summary>Slides the object horizontally, respecting its allowed area and any obstacles. Returns the displacement actually applied.</summary>
        public Vector3 TryMove(Vector3 delta)
        {
            delta.y = 0;
            if (delta.sqrMagnitude < 1e-8f) return Vector3.zero;
            Vector3 pos = transform.position;
            Vector3 target = pos + delta;
            target.x = Mathf.Clamp(target.x, xRange.x, xRange.y);
            target.z = Mathf.Clamp(target.z, zRange.x, zRange.y);
            delta = target - pos;
            float dist = delta.magnitude;
            if (dist < 1e-5f) return Vector3.zero;
            Vector3 dir = delta / dist;
            float allowed = dist;

            foreach (var c in Colliders)
            {
                if (c == null || c.isTrigger || !c.enabled) continue;
                RaycastHit[] hits = null;
                if (c is BoxCollider bc)
                {
                    Vector3 s = bc.transform.lossyScale;
                    Vector3 half = new Vector3(bc.size.x * Mathf.Abs(s.x), bc.size.y * Mathf.Abs(s.y), bc.size.z * Mathf.Abs(s.z)) * 0.5f;
                    half = Vector3.Max(half - new Vector3(0.012f, 0.012f, 0.012f), new Vector3(0.01f, 0.01f, 0.01f));
                    hits = Physics.BoxCastAll(bc.transform.TransformPoint(bc.center), half, dir, bc.transform.rotation, dist + 0.02f, Layer.MovableBlockMask, QueryTriggerInteraction.Ignore);
                }
                else if (c is SphereCollider sc)
                {
                    Vector3 s = sc.transform.lossyScale;
                    float r = sc.radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z)) - 0.012f;
                    hits = Physics.SphereCastAll(sc.transform.TransformPoint(sc.center), Mathf.Max(0.01f, r), dir, dist + 0.02f, Layer.MovableBlockMask, QueryTriggerInteraction.Ignore);
                }
                else if (c is CapsuleCollider cc)
                {
                    Vector3 s = cc.transform.lossyScale;
                    float r = cc.radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z)) - 0.012f;
                    float h = Mathf.Max(cc.height * Mathf.Abs(s.y), r * 2f);
                    Vector3 center = cc.transform.TransformPoint(cc.center);
                    Vector3 a = center + Vector3.up * (h * 0.5f - r), b = center - Vector3.up * (h * 0.5f - r);
                    hits = Physics.CapsuleCastAll(a, b, Mathf.Max(0.01f, r), dir, dist + 0.02f, Layer.MovableBlockMask, QueryTriggerInteraction.Ignore);
                }
                if (hits == null) continue;
                foreach (var h in hits)
                {
                    if (h.collider == null || h.collider.transform.IsChildOf(transform)) continue;
                    if (h.distance <= 0.0001f && h.point == Vector3.zero) continue;     // started inside it: ignore
                    if (Mathf.Abs(h.normal.y) > 0.6f) continue;                          // floors / table tops are not obstacles
                    allowed = Mathf.Min(allowed, Mathf.Max(0f, h.distance - 0.01f));
                }
            }
            Vector3 moved = dir * allowed;
            if (allowed > 1e-5f)
            {
                transform.position = pos + moved;
                if (rb != null) rb.position = transform.position;
                Physics.SyncTransforms();
                WorldState.Dirty();
            }
            return moved;
        }

        public void SetPose(Vector3 pos, float yaw)
        {
            transform.position = pos;
            transform.rotation = Quaternion.Euler(0, yaw, 0);
            if (rb != null) { rb.position = pos; rb.rotation = transform.rotation; }
            Physics.SyncTransforms();
            WorldState.Dirty();
        }

        public void ResetHome() { SetPose(HomePos, HomeYaw); }

        public string Capture()
        {
            var p = transform.position;
            return $"{p.x:F3},{p.y:F3},{p.z:F3},{transform.eulerAngles.y:F1}";
        }

        public void Restore(string s)
        {
            var parts = s.Split(',');
            if (parts.Length < 4) return;
            if (!float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x)) return;
            float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y);
            float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float z);
            float.TryParse(parts[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float yaw);
            // Never restore into an impossible place: stay inside the allowed area
            x = Mathf.Clamp(x, xRange.x, xRange.y); z = Mathf.Clamp(z, zRange.x, zRange.y);
            SetPose(new Vector3(x, HomePos.y, z), yaw);
        }
    }
}
