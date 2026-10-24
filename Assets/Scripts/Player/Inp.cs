using UnityEngine;
using UnityEngine.InputSystem;

namespace CatOnTheSill
{
    /// <summary>Thin wrapper over the Input System so gameplay code reads like a classic input manager.</summary>
    public static class Inp
    {
        public static bool Enabled = true;
        public static float MouseSensitivity = 0.11f;

        // test hooks: let automated tests drive the cat
        public static Vector2? TestMove;
        public static bool TestJump, TestInteract, TestAlt, TestGrab, TestRun, TestMeow;

        static Keyboard K => Keyboard.current;
        static Mouse M => Mouse.current;
        static Gamepad G => Gamepad.current;

        public static Vector2 Move
        {
            get
            {
                if (!Enabled) return Vector2.zero;
                if (TestMove.HasValue) return TestMove.Value;
                Vector2 v = Vector2.zero;
                var k = K;
                if (k != null)
                {
                    if (k.wKey.isPressed) v.y += 1;
                    if (k.sKey.isPressed) v.y -= 1;
                    if (k.dKey.isPressed) v.x += 1;
                    if (k.aKey.isPressed) v.x -= 1;
                }
                var g = G;
                if (g != null)
                {
                    Vector2 s = g.leftStick.ReadValue();
                    if (s.magnitude > 0.18f) v += s;
                }
                return Vector2.ClampMagnitude(v, 1f);
            }
        }

        /// <summary>Camera rotation this frame in degrees (x = yaw, y = pitch).</summary>
        public static Vector2 Look
        {
            get
            {
                if (!Enabled) return Vector2.zero;
                Vector2 v = Vector2.zero;
                var m = M;
                if (m != null && Cursor.lockState == CursorLockMode.Locked) { var d = m.delta.ReadValue(); v += new Vector2(d.x, -d.y) * MouseSensitivity; }
                var k = K;
                if (k != null)
                {
                    float dt = Time.unscaledDeltaTime;
                    if (k.leftArrowKey.isPressed) v.x -= 110f * dt;
                    if (k.rightArrowKey.isPressed) v.x += 110f * dt;
                    if (k.upArrowKey.isPressed) v.y -= 70f * dt;
                    if (k.downArrowKey.isPressed) v.y += 70f * dt;
                }
                var g = G;
                if (g != null)
                {
                    Vector2 s = g.rightStick.ReadValue();
                    if (s.magnitude > 0.18f) v += new Vector2(s.x, -s.y) * 140f * Time.unscaledDeltaTime;
                }
                return v;
            }
        }

        public static float Zoom
        {
            get
            {
                if (!Enabled) return 0;
                float z = 0;
                if (M != null) z -= M.scroll.ReadValue().y * 0.002f;
                if (K != null) { if (K.zKey.isPressed) z -= 1.5f * Time.unscaledDeltaTime; if (K.xKey.isPressed) z += 1.5f * Time.unscaledDeltaTime; }
                if (G != null) { if (G.dpad.up.isPressed) z -= 1.5f * Time.unscaledDeltaTime; if (G.dpad.down.isPressed) z += 1.5f * Time.unscaledDeltaTime; }
                return z;
            }
        }

        public static bool JumpDown => Enabled && (TestConsume(ref TestJump) || (K != null && K.spaceKey.wasPressedThisFrame) || (G != null && G.buttonSouth.wasPressedThisFrame));
        public static bool RunHeld => Enabled && (TestRun || (K != null && (K.leftShiftKey.isPressed || K.rightShiftKey.isPressed)) || (G != null && G.leftTrigger.ReadValue() > 0.4f));
        public static bool InteractDown => Enabled && (TestConsume(ref TestInteract) || (K != null && K.eKey.wasPressedThisFrame) || (G != null && G.buttonWest.wasPressedThisFrame));
        public static bool AltDown => Enabled && (TestConsume(ref TestAlt) || (K != null && K.qKey.wasPressedThisFrame) || (G != null && G.buttonEast.wasPressedThisFrame));
        public static bool GrabHeld => Enabled && (TestGrab || (K != null && K.fKey.isPressed) || (G != null && G.rightShoulder.isPressed));
        public static bool MeowDown => Enabled && (TestConsume(ref TestMeow) || (G != null && G.buttonNorth.wasPressedThisFrame));
        public static bool HintDown => Enabled && ((K != null && K.hKey.wasPressedThisFrame) || (G != null && G.dpad.left.wasPressedThisFrame));

        static bool TestConsume(ref bool f) { if (f) { f = false; return true; } return false; }

        // These work even when gameplay input is disabled (menus)
        public static bool MapDown => K != null && K.mKey.wasPressedThisFrame;
        public static bool PauseDown => (K != null && (K.escapeKey.wasPressedThisFrame || K.pKey.wasPressedThisFrame)) || (G != null && G.startButton.wasPressedThisFrame);
        public static bool ConfirmDown => (K != null && (K.eKey.wasPressedThisFrame || K.enterKey.wasPressedThisFrame || K.spaceKey.wasPressedThisFrame)) || (G != null && (G.buttonSouth.wasPressedThisFrame || G.buttonWest.wasPressedThisFrame));
        public static bool AnyKeyDown => (K != null && K.anyKey.wasPressedThisFrame) || (M != null && M.leftButton.wasPressedThisFrame) || (G != null && G.buttonSouth.wasPressedThisFrame);

        public static int NumberKeyDown()
        {
            if (K == null) return -1;
            if (K.digit1Key.wasPressedThisFrame) return 0;
            if (K.digit2Key.wasPressedThisFrame) return 1;
            if (K.digit3Key.wasPressedThisFrame) return 2;
            if (K.digit4Key.wasPressedThisFrame) return 3;
            if (K.digit5Key.wasPressedThisFrame) return 4;
            if (K.digit6Key.wasPressedThisFrame) return 5;
            return -1;
        }
    }
}
