using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CatOnTheSill
{
    /// <summary>Third-person camera: mouse / stick / arrow-key orbit, wheel zoom, wall collision, plus a title-screen orbit.</summary>
    public class CameraRig : MonoBehaviour
    {
        public Camera cam;
        public Transform target;
        public CamMode mode = CamMode.Orbit;
        public float yaw = 0f, pitch = 24f, distance = 2.3f;
        public float minDistance = 1.0f, maxDistance = 4.2f;
        public Vector3 orbitCenter = new Vector3(9, 0.5f, 6);
        public Vector3 orbitLookOffset = new Vector3(-5.2f, 0, 0);
        public float orbitRadius = 24f, orbitHeight = 17f, orbitAngle;

        // fixed (cinematics)
        public Vector3 fixedPos, fixedLook;

        Vector3 pivotSmooth;
        float curDist;
        bool initialised;

        public Vector3 PlanarForward
        {
            get { Vector3 f = Quaternion.Euler(0, yaw, 0) * Vector3.forward; return f; }
        }

        /// <summary>Creates the camera object (done once by the scene baker).</summary>
        public void EnsureCamera()
        {
            if (cam != null) return;
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.transform.SetParent(transform, false);
            cam = go.AddComponent<Camera>();
            cam.nearClipPlane = 0.06f; cam.farClipPlane = 900f; cam.fieldOfView = 62f;
            cam.clearFlags = CameraClearFlags.Skybox;
            go.AddComponent<AudioListener>();
        }

        public void Init()
        {
            EnsureCamera();
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
            data.renderShadows = true;
            curDist = distance;
        }

        void LateUpdate()
        {
            if (cam == null) return;
            float dt = Time.unscaledDeltaTime;
            switch (mode)
            {
                case CamMode.Orbit:
                {
                    orbitAngle += 5f * dt;
                    float a = orbitAngle * Mathf.Deg2Rad;
                    Vector3 p = orbitCenter + new Vector3(Mathf.Sin(a) * orbitRadius * 0.5f, orbitHeight, -orbitRadius * 0.8f + Mathf.Cos(a) * orbitRadius * 0.25f);
                    cam.transform.position = Vector3.Lerp(cam.transform.position, p, 1f - Mathf.Exp(-3f * dt));
                    var look = Quaternion.LookRotation(orbitCenter + orbitLookOffset - cam.transform.position);
                    cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, look, 1f - Mathf.Exp(-4f * dt));
                    break;
                }
                case CamMode.Fixed:
                {
                    cam.transform.position = Vector3.Lerp(cam.transform.position, fixedPos, 1f - Mathf.Exp(-2.5f * dt));
                    var look = Quaternion.LookRotation(fixedLook - cam.transform.position);
                    cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, look, 1f - Mathf.Exp(-3f * dt));
                    break;
                }
                default:
                    if (target != null) Follow(dt);
                    break;
            }
        }

        public void SnapBehind(float yawDeg)
        {
            yaw = yawDeg; pitch = 22f; initialised = false;
        }
        public void FocusHint(Vector3 worldTarget)
        {
            if (mode != CamMode.Follow || target == null) return;
            Vector3 pivot = target.position + Vector3.up * 0.36f;
            Vector3 direction = worldTarget - pivot;
            if (direction.sqrMagnitude < 0.01f) return;
            yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            pitch = -Mathf.Asin(Mathf.Clamp(direction.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;
            pitch = Mathf.Clamp(pitch, -8f, 68f);
        }


        void Follow(float dt)
        {
            if (Time.timeScale > 0f && Inp.Enabled)
            {
                Vector2 look = Inp.Look;
                yaw += look.x;
                pitch = Mathf.Clamp(pitch + look.y, -8f, 68f);
                distance = Mathf.Clamp(distance + Inp.Zoom, minDistance, maxDistance);
            }
            Vector3 pivot = target.position + Vector3.up * 0.36f;
            if (!initialised) { pivotSmooth = pivot; curDist = distance; initialised = true; cam.transform.position = pivot - Vector3.forward * 2f; }
            pivotSmooth = Vector3.Lerp(pivotSmooth, pivot, 1f - Mathf.Exp(-16f * dt));
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
            Vector3 dir = rot * Vector3.back;

            float want = distance;
            float radius = 0.2f;
            if (Physics.SphereCast(pivotSmooth, radius, dir, out var hit, distance, Layer.CameraMask, QueryTriggerInteraction.Ignore))
                want = Mathf.Max(0.35f, hit.distance - 0.02f);
            // collide instantly, relax gradually
            curDist = want < curDist ? want : Mathf.Lerp(curDist, want, 1f - Mathf.Exp(-5f * dt));
            Vector3 pos = pivotSmooth + dir * curDist;
            cam.transform.position = pos;
            cam.transform.rotation = rot;
            // if the camera is nearly on top of the cat, hide nothing - cat is small and cute
        }
    }
}

