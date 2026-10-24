using System.Collections;
using UnityEngine;

namespace CatOnTheSill
{
    [RequireComponent(typeof(CharacterController))]
    public class CatController : MonoBehaviour
    {
        public static CatController I;

        public CameraRig rig;
        public bool Frozen;

        public const float WalkSpeed = 1.9f, RunSpeed = 4.3f, Accel = 15f, Decel = 22f, Gravity = -22f, JumpVel = 6.7f;
        public const float MaxLedge = 1.5f;

        CharacterController cc;
        CatModel model;
        Vector3 hvel;
        float vy;
        bool grounded;
        float coyote, jumpBuf;
        Vector3 lastSafe;
        float safeTimer;
        bool mantling;
        float idleTime;
        float pawTimer;
        float stepTimer;
        float pushTimer;
        float mantleT, stuckT, insideT;
        Vector3 lastHitNormal; float lastHitTime = -10f;

        public MovableObject Grabbed { get; private set; }
        public bool Grounded => grounded;
        public bool IsMoving => hvel.magnitude > 0.2f;
        public Vector3 Velocity => hvel;
        public Vector3 Center => transform.position + Vector3.up * 0.25f;
        public bool IsMantling => mantling;

        /// <summary>Distance from a world point to the cat's body (a vertical segment from paws to shoulders).</summary>
        public float DistanceTo(Vector3 p)
        {
            Vector3 a = transform.position + Vector3.up * 0.08f, b = transform.position + Vector3.up * 0.5f;
            Vector3 ab = b - a; float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector3.Distance(p, a + ab * t);
        }

        public void Init(CameraRig r)
        {
            I = this;
            rig = r;
            Setup();
            lastSafe = transform.position;
        }

        /// <summary>Configures the controller and builds the cat model if it is not already in the scene (the baker calls this).</summary>
        public void Setup()
        {
            cc = GetComponent<CharacterController>();
            cc.height = 0.5f; cc.radius = 0.17f; cc.center = new Vector3(0, 0.25f, 0);
            cc.stepOffset = 0.28f; cc.skinWidth = 0.03f; cc.slopeLimit = 55f; cc.minMoveDistance = 0f;
            model = GetComponentInChildren<CatModel>();
            if (model == null)
            {
                var mroot = new GameObject("Model");
                mroot.transform.SetParent(transform, false);
                model = mroot.AddComponent<CatModel>();
                model.Build();
            }
            B.SetLayerRecursive(gameObject, Layer.Cat);
        }

        public void Teleport(Vector3 pos, float yaw)
        {
            bool was = cc.enabled; cc.enabled = false;
            transform.position = pos; transform.rotation = Quaternion.Euler(0, yaw, 0);
            cc.enabled = was;
            hvel = Vector3.zero; vy = 0; lastSafe = pos;
            Release();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            bool control = !Frozen && !mantling && Inp.Enabled;

            Vector2 mv = control ? Inp.Move : Vector2.zero;
            Vector3 camF = rig != null ? rig.PlanarForward : Vector3.forward;
            Vector3 camR = new Vector3(camF.z, 0, -camF.x);
            Vector3 wish = camF * mv.y + camR * mv.x;
            if (wish.sqrMagnitude > 1f) wish.Normalize();

            // ---- grab handling
            if (control)
            {
                if (Inp.GrabHeld) { if (Grabbed == null) TryGrab(); }
                else if (Grabbed != null) Release();
            }
            else if (Grabbed != null && !mantling) Release();
            if (Grabbed != null && (!grounded || (Grabbed.ClosestPoint(Center) - Center).magnitude > 1.1f)) Release();

            bool run = control && Inp.RunHeld && Grabbed == null;
            float maxSpeed = Grabbed != null ? 1.05f : (run ? RunSpeed : WalkSpeed);
            if (pushTimer > 0) { maxSpeed = Mathf.Min(maxSpeed, 1.2f); pushTimer -= dt; }
            Vector3 target = wish * maxSpeed;
            float rate = target.sqrMagnitude > hvel.sqrMagnitude ? Accel : Decel;
            if (!grounded) rate *= 0.45f;
            hvel = Vector3.MoveTowards(hvel, target, rate * dt);

            // ---- facing
            if (Grabbed != null)
            {
                Vector3 to = Grabbed.transform.position - transform.position; to.y = 0;
                if (to.sqrMagnitude > 0.001f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to.normalized), 1f - Mathf.Exp(-14f * dt));
            }
            else if (!mantling && hvel.sqrMagnitude > 0.04f && wish.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(hvel.normalized), 1f - Mathf.Exp(-13f * dt));
            }

            // ---- jumping
            if (control && Inp.JumpDown) jumpBuf = 0.15f;
            jumpBuf -= dt;
            coyote = grounded ? 0.12f : coyote - dt;
            if (!mantling && jumpBuf > 0f && coyote > 0f && Grabbed == null && !Frozen)
            {
                Vector3 dir = wish.sqrMagnitude > 0.01f ? wish.normalized : Flat(transform.forward);
                jumpBuf = 0; coyote = 0;
                if (!TryMantle(dir))
                {
                    vy = JumpVel; grounded = false;
                    AudioManager.I?.Play("jump", transform.position, 0.6f, Random.Range(0.95f, 1.1f));
                }
            }

            if (mantling)
            {
                // watchdog: a mantle that never finishes would freeze the cat for good
                mantleT += dt;
                if (mantleT > 2.5f) { StopAllCoroutines(); cc.enabled = true; mantling = false; vy = 0f; hvel = Vector3.zero; Physics.SyncTransforms(); mantleT = 0f; }
                else { model.Tick(dt, 0.4f, false, 2f, true, false, false, false); return; }
            }
            else mantleT = 0f;

            // ---- gravity & move
            if (grounded && vy <= 0f) vy = -2.5f; else vy += Gravity * dt;
            vy = Mathf.Max(vy, -18f);
            Vector3 before = transform.position;
            Vector3 motion = (hvel + Vector3.up * vy) * dt;
            if (Grabbed != null) IgnoreGrabbedCollision(true);
            var flags = cc.Move(motion);
            grounded = (flags & CollisionFlags.Below) != 0 || (vy <= 0.01f && ProbeGround());
            UnstickCheck(dt, control && Grabbed == null && wish.sqrMagnitude > 0.25f, before);
            if ((flags & CollisionFlags.Above) != 0 && vy > 0f) vy = 0f;

            // grabbed object follows the cat
            if (Grabbed != null)
            {
                Vector3 actual = transform.position - before; actual.y = 0;
                Vector3 moved = Grabbed.TryMove(actual);
                Vector3 deficit = actual - moved;
                if (deficit.sqrMagnitude > 1e-6f) { cc.Move(-deficit); hvel *= 0.5f; }
            }

            // ---- safety net
            if (grounded)
            {
                safeTimer -= dt;
                if (safeTimer <= 0f && transform.position.y > -0.5f && Physics.CheckSphere(transform.position + Vector3.up * 0.08f, 0.1f, Layer.SolidMask) == false)
                { lastSafe = transform.position; safeTimer = 0.5f; }
            }
            if (transform.position.y < -4f) { Teleport(lastSafe + Vector3.up * 0.1f, transform.eulerAngles.y); }

            // ---- feedback
            float hs = hvel.magnitude;
            if (grounded && hs > 0.5f)
            {
                stepTimer -= dt * Mathf.Lerp(1f, 1.9f, hs / RunSpeed);
                if (stepTimer <= 0f) { stepTimer = 0.36f; AudioManager.I?.Play("step", transform.position, Mathf.Lerp(0.18f, 0.35f, hs / RunSpeed), Random.Range(0.9f, 1.15f)); }
            }
            if (hs < 0.1f && grounded && control && pawTimer <= 0f && Grabbed == null) idleTime += dt; else idleTime = 0f;
            pawTimer -= dt;
            model.Tick(dt, hs / RunSpeed, grounded, vy, false, Grabbed != null, pawTimer > 0f, idleTime > 6f);
        }

        /// <summary>
        /// Gets the cat out of corners: if the player is pushing to move but the cat is not going anywhere it hops and is
        /// nudged away from whatever it is wedged against; if it ends up inside geometry it goes back to the last safe spot.
        /// </summary>
        void UnstickCheck(float dt, bool trying, Vector3 before)
        {
            Vector3 d = transform.position - before; d.y = 0;
            bool barely = d.magnitude < WalkSpeed * dt * 0.15f;
            if (trying && barely && grounded) stuckT += dt; else stuckT = Mathf.Max(0f, stuckT - dt * 2f);
            if (stuckT > 0.7f)
            {
                stuckT = 0.2f;
                vy = JumpVel * 0.75f; grounded = false;
                if (Time.time - lastHitTime < 0.5f) hvel = new Vector3(lastHitNormal.x, 0, lastHitNormal.z).normalized * 1.8f;
            }
            // wedged inside solid geometry (e.g. after an object slid onto the cat)?
            bool inside = Physics.CheckCapsule(transform.position + Vector3.up * 0.14f, transform.position + Vector3.up * 0.34f, 0.1f, Layer.SolidMask, QueryTriggerInteraction.Ignore);
            if (inside && trying) insideT += dt; else insideT = 0f;
            if (insideT > 1.2f) { insideT = 0f; Teleport(lastSafe + Vector3.up * 0.1f, transform.eulerAngles.y); }
        }

        void LateUpdate()
        {
            // glance toward whatever the cat can interact with
            var focus = InteractionSystem.I != null ? InteractionSystem.I.FocusPoint : null;
            if (focus.HasValue) model.LookAt(focus.Value); else model.ClearLook();
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v.sqrMagnitude < 1e-6f ? Vector3.forward : v.normalized; }

        bool ProbeGround()
        {
            return Physics.SphereCast(transform.position + Vector3.up * 0.3f, 0.12f, Vector3.down, out _, 0.22f, Layer.SolidMask, QueryTriggerInteraction.Ignore);
        }

        // -------------------------------------------------------------------------------------------- mantling
        bool TryMantle(Vector3 dir)
        {
            Vector3 feet = transform.position;
            for (int pass = 0; pass < 2; pass++)
            {
                foreach (float d in new[] { 0.34f, 0.5f, 0.66f, 0.82f })
                {
                    Vector3 origin = feet + dir * d + Vector3.up * (MaxLedge + 0.15f);
                    if (!Physics.Raycast(origin, Vector3.down, out var hit, MaxLedge + 0.15f - 0.08f, Layer.SolidMask, QueryTriggerInteraction.Ignore)) continue;
                    float rise = hit.point.y - feet.y;
                    if (rise < 0.3f || rise > MaxLedge || hit.normal.y < 0.85f) continue;
                    if (hit.collider.GetComponentInParent<Climbable>() == null) continue;
                    Vector3 end = hit.point + Vector3.up * 0.015f;
                    if (Physics.CheckCapsule(end + Vector3.up * 0.2f, end + Vector3.up * 0.3f, 0.145f, Layer.SolidMask, QueryTriggerInteraction.Ignore)) continue;
                    // a wall must stand between us and the top (otherwise it's just a step)
                    StartCoroutine(MantleRoutine(feet, end, rise, dir));
                    return true;
                }
            }
            return false;
        }

        IEnumerator MantleRoutine(Vector3 start, Vector3 end, float rise, Vector3 dir)
        {
            mantling = true; cc.enabled = false;
            AudioManager.I?.Play("jump", transform.position, 0.7f, 0.9f);
            float dur = 0.34f + 0.22f * rise;
            transform.rotation = Quaternion.LookRotation(dir);
            for (float t = 0; t < 1f; t += Time.deltaTime / dur)
            {
                float e = t * t * (3f - 2f * t);
                Vector3 p = Vector3.Lerp(start, end, e);
                p.y += Mathf.Sin(Mathf.PI * t) * 0.22f;
                transform.position = p;
                yield return null;
            }
            transform.position = end;
            cc.enabled = true; mantling = false; vy = 0f; hvel = dir * 0.8f; grounded = true;
            Physics.SyncTransforms();
            AudioManager.I?.Play("land", transform.position, 0.5f, 1f);
        }

        // -------------------------------------------------------------------------------------------- grab / push
        void TryGrab()
        {
            MovableObject best = null; float bd = 0.6f;
            foreach (var m in MovableObject.All)
            {
                if (m == null || !m.grabbable) continue;
                float d = (m.ClosestPoint(Center) - Center).magnitude;
                if (d < bd)
                {
                    Vector3 to = m.ClosestPoint(Center) - transform.position; to.y = 0;
                    if (to.sqrMagnitude > 0.0001f && Vector3.Dot(to.normalized, transform.forward) < 0.15f && d > 0.3f) continue;
                    bd = d; best = m;
                }
            }
            if (best == null) return;
            Grabbed = best;
            AudioManager.I?.Play("grab", transform.position, 0.4f, 1f);
        }

        public void Release()
        {
            if (Grabbed != null) IgnoreGrabbedCollision(false);
            Grabbed = null;
        }

        MovableObject ignored;
        void IgnoreGrabbedCollision(bool on)
        {
            if (on)
            {
                if (ignored == Grabbed) return;
                if (ignored != null) SetIgnore(ignored, false);
                ignored = Grabbed; SetIgnore(ignored, true);
            }
            else if (ignored != null) { SetIgnore(ignored, false); ignored = null; }
        }

        void SetIgnore(MovableObject m, bool v)
        {
            foreach (var c in m.Colliders) if (c != null && cc != null && cc.enabled) Physics.IgnoreCollision(cc, c, v);
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            lastHitNormal = hit.normal; lastHitTime = Time.time;
            var m = hit.collider.GetComponentInParent<MovableObject>();
            if (m == null || m == Grabbed) return;
            if (hit.moveDirection.y < -0.3f) return;
            Vector3 push = hit.moveDirection; push.y = 0;
            if (push.sqrMagnitude < 0.01f) return;
            push.Normalize();
            if (hvel.sqrMagnitude < 0.05f || Vector3.Dot(hvel.normalized, push) < 0.45f) return;
            var moved = m.TryMove(push * (m.pushSpeed * Time.deltaTime));
            if (moved.sqrMagnitude > 1e-8f)
            {
                pushTimer = 0.15f;
                if (Time.frameCount % 20 == 0) AudioManager.I?.Play("scrape", m.transform.position, 0.35f, Random.Range(0.9f, 1.1f));
            }
        }

        // -------------------------------------------------------------------------------------------- misc actions
        public void Paw() { pawTimer = 0.5f; }

        public void Meow()
        {
            AudioManager.I?.Play("meow", transform.position, 0.8f, Random.Range(0.92f, 1.08f));
            Paw();
            UIManager.I?.ShowDialogue("Miso", CatLines.Next(), new Color(1f, 0.7f, 0.35f));
        }
    }
}
