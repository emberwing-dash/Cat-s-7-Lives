using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CatOnTheSill
{
    /// <summary>Traces reflected sunbeams between emitters, mirrors and sensors and draws them.</summary>
    public class BeamSystem : MonoBehaviour
    {
        public static readonly List<BeamEmitter> Emitters = new List<BeamEmitter>();
        public static BeamSystem I;
        public static bool Record;
        public static readonly List<string> DebugLog = new List<string>();

        class Seg { public LineRenderer lr; }
        readonly List<LineRenderer> lines = new List<LineRenderer>();
        readonly List<Transform> spots = new List<Transform>();
        readonly List<Light> spotLights = new List<Light>();
        int lineUsed, spotUsed;
        Material lineMat, spotMat;
        float next;

        void Awake()
        {
            I = this;
            lineMat = Mats.Blend(Color.white, Mats.Soft, Mats.BlendKind.Additive);
            lineMat.SetTexture("_MainTex", Texture2D.whiteTexture);
            spotMat = Mats.Blend(Color.white, Mats.Soft, Mats.BlendKind.Additive);
        }

        void OnDestroy() { if (I == this) I = null; Emitters.Clear(); }

        void Update()
        {
            if (Time.time < next) return;
            next = Time.time + 0.04f;
            Tick();
        }

        void Tick()
        {
            lineUsed = 0; spotUsed = 0;
            Vector3 L = SunLight.Travel;
            bool run = SunLight.Strength > 0.05f && (TimeManager.I == null || TimeManager.I.Settled);
            if (run)
            {
                foreach (var e in Emitters)
                {
                    if (e == null || Vector3.Dot(L, e.inward) < 0.06f) continue;
                    Color bc = BeamEmitter.ToColor(e.color);
                    for (int cx = 0; cx < e.cols; cx++)
                        for (int cy = 0; cy < e.rows; cy++)
                        {
                            float u = ((cx + 0.5f) / e.cols - 0.5f) * e.width;
                            float v = ((cy + 0.5f) / e.rows - 0.5f) * e.height;
                            Vector3 o = e.center + e.right * u + e.up * v + e.inward * 0.03f;
                            // only rays that really come from the sky (not shadowed by the window frame, wall thickness or a curtain)
                            if (Physics.Raycast(o, SunLight.ToSun, 1.4f, Layer.OccluderMask, QueryTriggerInteraction.Ignore)) continue;
                            Trace(o, L, e.color, bc);
                        }
                }
            }
            for (int i = lineUsed; i < lines.Count; i++) lines[i].gameObject.SetActive(false);
            for (int i = spotUsed; i < spots.Count; i++) { spots[i].gameObject.SetActive(false); }
        }

        void Trace(Vector3 o, Vector3 d, BeamColor color, Color bc)
        {
            bool bounced = false;
            int bounce = 0;
            for (int guard = 0; guard < 24 && bounce < 7; guard++)
            {
                if (!Physics.Raycast(o, d, out var hit, 60f, Layer.BeamMask, QueryTriggerInteraction.Collide))
                {
                    if (bounced) AddLine(o, o + d * 14f, bc, 0.5f);
                    return;
                }
                var sensor = hit.collider.GetComponentInParent<LightSensor>();
                if (hit.collider.isTrigger && sensor == null)
                {
                    // helper volumes (sill triggers, cords, fragments) are invisible to light
                    o = hit.point + d * 0.02f;
                    continue;
                }
                var refl = hit.collider.GetComponentInParent<Reflector>();
                if (Record && DebugLog.Count < 60) DebugLog.Add($"bounce {bounce} from {o:F2} dir {d:F2} hit '{hit.collider.name}' of '{(hit.collider.transform.parent != null ? hit.collider.transform.parent.name : "-")}' at {hit.point:F2} refl={(refl != null)} dot={(refl != null ? Vector3.Dot(d, refl.Normal) : 0f):F2}");
                if (refl != null && hit.collider == refl.plateCollider && Vector3.Dot(d, refl.Normal) < -0.02f)
                {
                    if (bounced) AddLine(o, hit.point, bc, 1f);
                    Vector3 n = refl.Normal;
                    d = Vector3.Reflect(d, n).normalized;
                    o = hit.point + n * 0.012f;
                    bounced = true;
                    bounce++;
                    continue;
                }
                if (sensor != null) sensor.Hit(color);
                if (bounced)
                {
                    AddLine(o, hit.point, bc, 1f);
                    AddSpot(hit.point, hit.normal, bc, sensor != null);
                }
                return;
            }
        }

        void AddLine(Vector3 a, Vector3 b, Color c, float alpha)
        {
            LineRenderer lr;
            if (lineUsed < lines.Count) lr = lines[lineUsed];
            else
            {
                var g = new GameObject("BeamLine");
                g.transform.SetParent(transform, false);
                lr = g.AddComponent<LineRenderer>();
                lr.positionCount = 2; lr.sharedMaterial = lineMat; lr.shadowCastingMode = ShadowCastingMode.Off; lr.receiveShadows = false;
                lr.numCapVertices = 2; lr.textureMode = LineTextureMode.Stretch; lr.useWorldSpace = true;
                lines.Add(lr);
            }
            lineUsed++;
            lr.gameObject.SetActive(true);
            lr.SetPosition(0, a); lr.SetPosition(1, b);
            lr.startWidth = lr.endWidth = 0.05f;
            var col = c; col.a = 0.55f * alpha;
            lr.startColor = lr.endColor = col;
        }

        void AddSpot(Vector3 p, Vector3 n, Color c, bool sensor)
        {
            Transform t;
            Light lt;
            if (spotUsed < spots.Count) { t = spots[spotUsed]; lt = spotLights[spotUsed]; }
            else
            {
                var g = B.Quad(transform, "Spot", Vector3.zero, Vector3.up, Vector2.one, new Material(spotMat));
                t = g.transform;
                var lg = new GameObject("L"); lg.transform.SetParent(t, false);
                lt = lg.AddComponent<Light>(); lt.type = LightType.Point; lt.range = 2.5f; lt.shadows = LightShadows.None;
                spots.Add(t); spotLights.Add(lt);
            }
            spotUsed++;
            t.gameObject.SetActive(true);
            t.position = p + n * 0.015f;
            t.rotation = Quaternion.LookRotation(-n, Mathf.Abs(n.y) > 0.9f ? Vector3.forward : Vector3.up);
            t.localScale = Vector3.one * (sensor ? 0.7f : 0.45f);
            var m = t.GetComponent<MeshRenderer>();
            m.sharedMaterial.SetColor("_Color", new Color(c.r, c.g, c.b, 0.9f));
            lt.transform.localPosition = new Vector3(0, 0, 0.25f);
            lt.color = c; lt.intensity = sensor ? 2.2f : 1.0f;
        }
    }
}
