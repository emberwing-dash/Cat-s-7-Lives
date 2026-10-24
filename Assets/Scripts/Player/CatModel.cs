using UnityEngine;
using UnityEngine.Rendering;

namespace CatOnTheSill
{
    /// <summary>A procedurally built, procedurally animated cat. +Z is forward, the origin sits at the paws.</summary>
    public class CatModel : MonoBehaviour
    {
        [SerializeField] Transform body, headPivot, tailBase;
        [SerializeField] Transform[] legs = new Transform[4];     // FL FR BL BR
        [SerializeField] Transform[] tail = new Transform[6];
        [SerializeField] Transform earL, earR, eyeL, eyeR;
        [SerializeField] Transform sitPivot;
        float phase, blinkTimer = 3f, blink, twitch, sit, t;
        [SerializeField] Vector3 bodyBase;
        Vector3 lookTarget;
        bool hasLook;

        public void Build()
        {
            Color fur = new Color(0.90f, 0.56f, 0.26f), dark = new Color(0.58f, 0.31f, 0.12f), white = new Color(0.98f, 0.95f, 0.90f), pink = new Color(1f, 0.62f, 0.68f);
            var mFur = Mats.Lit(fur, 0.15f); var mDark = Mats.Lit(dark, 0.15f); var mWhite = Mats.Lit(white, 0.15f); var mPink = Mats.Lit(pink, 0.3f);
            var mEye = Mats.Emissive(new Color(0.45f, 0.95f, 0.45f), 0.7f); var mPupil = Mats.Lit(new Color(0.02f, 0.02f, 0.02f), 0.9f);

            sitPivot = B.Empty(transform, "SitPivot", new Vector3(0, 0, -0.1f)).transform;
            body = B.Empty(sitPivot, "Body", new Vector3(0, 0, 0.1f)).transform;
            bodyBase = body.localPosition;

            // torso
            B.Sphere(body, "Torso", new Vector3(0, 0.26f, 0), new Vector3(0.25f, 0.23f, 0.50f), mFur, Col.None);
            B.Sphere(body, "Haunch", new Vector3(0, 0.25f, -0.17f), new Vector3(0.29f, 0.27f, 0.30f), mFur, Col.None);
            B.Sphere(body, "Chest", new Vector3(0, 0.26f, 0.16f), new Vector3(0.25f, 0.25f, 0.26f), mWhite, Col.None);
            B.Sphere(body, "Belly", new Vector3(0, 0.19f, 0.0f), new Vector3(0.20f, 0.14f, 0.38f), mWhite, Col.None);
            for (int i = 0; i < 4; i++)
                B.Sphere(body, "Stripe" + i, new Vector3(0, 0.366f, -0.2f + i * 0.12f), new Vector3(0.15f - i * 0.008f, 0.02f, 0.04f), mDark, Col.None);

            // head
            headPivot = B.Empty(body, "HeadPivot", new Vector3(0, 0.34f, 0.27f)).transform;
            B.Sphere(headPivot, "Head", new Vector3(0, 0.08f, 0.06f), new Vector3(0.28f, 0.24f, 0.25f), mFur, Col.None);
            B.Sphere(headPivot, "Muzzle", new Vector3(0, 0.035f, 0.175f), new Vector3(0.13f, 0.09f, 0.09f), mWhite, Col.None);
            B.Sphere(headPivot, "Nose", new Vector3(0, 0.07f, 0.215f), new Vector3(0.035f, 0.025f, 0.025f), mPink, Col.None);
            B.Sphere(headPivot, "Cheek", new Vector3(0, 0.06f, 0.1f), new Vector3(0.3f, 0.15f, 0.08f), mDark, Col.None).transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
            eyeL = MakeEye(headPivot, -1, mEye, mPupil);
            eyeR = MakeEye(headPivot, 1, mEye, mPupil);
            earL = MakeEar(headPivot, -1, mFur, mPink);
            earR = MakeEar(headPivot, 1, mFur, mPink);
            var wm = Mats.Lit(new Color(0.95f, 0.95f, 0.92f), 0.2f);
            for (int s = -1; s <= 1; s += 2)
                for (int w = 0; w < 3; w++)
                {
                    var wh = B.Box(headPivot, "Whisker", new Vector3(s * 0.1f, 0.045f + (w - 1) * 0.012f, 0.19f), new Vector3(0.13f, 0.004f, 0.004f), wm, false, false);
                    wh.transform.localRotation = Quaternion.Euler((w - 1) * -8f, s * (-10f + (w - 1) * -8f), 0);
                    wh.transform.localPosition += new Vector3(s * 0.04f, 0, 0);
                }

            // legs
            float[] lx = { -0.085f, 0.085f, -0.095f, 0.095f };
            float[] lz = { 0.17f, 0.17f, -0.19f, -0.19f };
            for (int i = 0; i < 4; i++)
            {
                var pv = B.Empty(body, "Leg" + i, new Vector3(lx[i], 0.2f, lz[i])).transform;
                legs[i] = pv;
                B.Cyl(pv, "Limb", new Vector3(0, -0.095f, 0), 0.036f, 0.19f, i < 2 ? mFur : mFur, Col.None);
                B.Sphere(pv, "Paw", new Vector3(0, -0.185f, 0.012f), new Vector3(0.075f, 0.05f, 0.098f), mWhite, Col.None);
            }

            // tail
            tailBase = B.Empty(body, "TailBase", new Vector3(0, 0.30f, -0.3f)).transform;
            Transform prev = tailBase;
            for (int i = 0; i < tail.Length; i++)
            {
                var seg = B.Empty(prev, "Tail" + i, i == 0 ? Vector3.zero : new Vector3(0, 0, -0.1f)).transform;
                tail[i] = seg;
                var m = i == tail.Length - 1 ? mDark : (i % 2 == 0 ? mFur : mDark);
                B.Sphere(seg, "Seg", new Vector3(0, 0, -0.05f), new Vector3(0.055f - i * 0.002f, 0.055f - i * 0.002f, 0.13f), m, Col.None);
                prev = seg;
            }
            foreach (var r in GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.On;
        }

        Transform MakeEye(Transform parent, int side, Material eye, Material pupil)
        {
            var root = B.Empty(parent, "Eye", new Vector3(side * 0.075f, 0.115f, 0.15f)).transform;
            B.Sphere(root, "Ball", Vector3.zero, new Vector3(0.062f, 0.066f, 0.04f), eye, Col.None);
            B.Sphere(root, "Pupil", new Vector3(0, 0, 0.012f), new Vector3(0.018f, 0.052f, 0.03f), pupil, Col.None);
            return root;
        }

        Transform MakeEar(Transform parent, int side, Material fur, Material inner)
        {
            var root = B.Empty(parent, "Ear", new Vector3(side * 0.088f, 0.17f, 0.045f)).transform;
            root.localRotation = Quaternion.Euler(-8f, 0, side * -16f);
            B.Pyramid(root, "Outer", Vector3.zero, new Vector3(0.115f, 0.15f, 0.055f), fur);
            B.Pyramid(root, "Inner", new Vector3(0, 0.005f, 0.02f), new Vector3(0.07f, 0.1f, 0.025f), inner);
            return root;
        }

        public void LookAt(Vector3 world) { lookTarget = world; hasLook = true; }
        public void ClearLook() { hasLook = false; }

        public void Tick(float dt, float speed01, bool grounded, float vy, bool mantling, bool grabbing, bool pawing, bool idleLong)
        {
            t += dt;
            // ---- gait
            float moveAmt = Mathf.Clamp01(speed01);
            float freq = Mathf.Lerp(6f, 15f, Mathf.Clamp01(speed01));
            if (moveAmt > 0.02f) phase += dt * freq;
            float amp = Mathf.Lerp(0.0f, 0.65f, Mathf.Clamp01(moveAmt * 1.2f));
            float airT = grounded ? 0f : 1f;
            float rise = Mathf.Clamp(vy / 6f, -1f, 1f);
            sit = Mathf.MoveTowards(sit, (idleLong && grounded && moveAmt < 0.02f && !grabbing) ? 1f : 0f, dt * 2.4f);

            float[] sw = new float[4];
            sw[0] = Mathf.Sin(phase) * amp; sw[3] = sw[0];
            sw[1] = Mathf.Sin(phase + Mathf.PI) * amp; sw[2] = sw[1];
            for (int i = 0; i < 4; i++)
            {
                float airAngle = i < 2 ? Mathf.Lerp(-55f, 25f, (rise + 1f) * 0.5f) : Mathf.Lerp(50f, -10f, (rise + 1f) * 0.5f);
                float groundAngle = sw[i] * Mathf.Rad2Deg * 0.85f;
                float a = Mathf.Lerp(groundAngle, airAngle, airT);
                if (i >= 2) a = Mathf.Lerp(a, -88f, sit);      // hind legs fold forward when sitting
                else a = Mathf.Lerp(a, 8f, sit);
                if (pawing && i == 1) a = Mathf.Lerp(a, -70f + Mathf.Sin(t * 18f) * 18f, 1f);
                legs[i].localRotation = Quaternion.Euler(a, 0, 0);
            }

            // ---- body
            float bob = Mathf.Abs(Mathf.Sin(phase)) * 0.018f * moveAmt;
            float breathe = Mathf.Sin(t * 2.1f) * 0.008f;
            body.localPosition = bodyBase + new Vector3(0, bob + breathe - sit * 0.05f, 0);
            float pitch = airT * (-rise * 18f) + (grabbing ? 10f : 0f);
            sitPivot.localRotation = Quaternion.Euler(pitch - sit * 38f, 0, Mathf.Sin(phase) * 2.5f * moveAmt);
            if (mantling) sitPivot.localRotation = Quaternion.Euler(-35f, 0, 0);

            // ---- head
            Quaternion headRot = Quaternion.Euler(sit * -10f + Mathf.Sin(t * 0.7f) * 2f, 0, 0);
            if (hasLook)
            {
                Vector3 local = sitPivot.InverseTransformPoint(lookTarget) - body.localPosition - headPivot.localPosition;
                local = headPivot.parent.InverseTransformPoint(lookTarget) - headPivot.localPosition;
                float yaw = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -55f, 55f);
                float pit = Mathf.Clamp(-Mathf.Atan2(local.y, new Vector2(local.x, local.z).magnitude) * Mathf.Rad2Deg, -30f, 35f);
                headRot = Quaternion.Euler(pit, yaw, 0);
            }
            headPivot.localRotation = Quaternion.Slerp(headPivot.localRotation, headRot, 1f - Mathf.Exp(-8f * dt));

            // ---- tail
            float wag = Mathf.Lerp(0.5f, 1f, moveAmt);
            for (int i = 0; i < tail.Length; i++)
            {
                float curl = 7f + (airT > 0.5f ? 0f : 0f);
                float sway = Mathf.Sin(t * 1.8f - i * 0.55f) * 11f * wag;
                float basePitch = i == 0 ? Mathf.Lerp(38f, 10f, moveAmt) + sit * -15f : curl;
                tail[i].localRotation = Quaternion.Euler(basePitch, sway, 0);
            }

            // ---- ears & eyes
            twitch -= dt;
            if (twitch < 0) twitch = Random.Range(2.5f, 6f);
            float tw = twitch > 0 && twitch < 0.25f ? Mathf.Sin(twitch * 40f) * 10f : 0f;
            earL.localRotation = Quaternion.Euler(-8f, 0, 16f + tw); earR.localRotation = Quaternion.Euler(-8f, 0, -16f);
            blinkTimer -= dt;
            if (blinkTimer < 0) { blink = 0.14f; blinkTimer = Random.Range(2.5f, 5.5f); }
            float eyeScale = 1f;
            if (blink > 0) { blink -= dt; eyeScale = Mathf.Abs(blink / 0.14f - 0.5f) * 2f; }
            eyeL.localScale = new Vector3(1, Mathf.Max(0.08f, eyeScale), 1);
            eyeR.localScale = new Vector3(1, Mathf.Max(0.08f, eyeScale), 1);
        }
    }
}
