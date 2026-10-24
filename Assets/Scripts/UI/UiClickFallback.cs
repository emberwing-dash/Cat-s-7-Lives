using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CatOnTheSill
{
    /// <summary>
    /// Safety net for built (.exe) players: if the EventSystem does not deliver a mouse click to a button (the pause
    /// menu while the cursor has just been unlocked and Time.timeScale is 0), the click is detected here directly from
    /// the mouse and the topmost button under the pointer is pressed. When the EventSystem does handle the click, this
    /// does nothing, so buttons never fire twice.
    /// </summary>
    public class UiClickFallback : MonoBehaviour
    {
        static UiClickFallback inst;
        static int handledFrame = -10;

        readonly HashSet<Button> seen = new HashSet<Button>();
        int pendingFrame = -10;
        Vector2 pendingPos;

        public static void Ensure()
        {
            if (inst != null) return;
            var g = new GameObject("UiClickFallback");
            inst = g.AddComponent<UiClickFallback>();
        }

        void OnDestroy() { if (inst == this) inst = null; }

        void LateUpdate()
        {
            // every button reports when it was clicked the normal way
            seen.RemoveWhere(b => b == null);
            var all = FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var b in all)
                if (seen.Add(b)) b.onClick.AddListener(() => handledFrame = Time.frameCount);

            // a click that was not handled normally in the frame of the release, or the one after it, is handled here
            if (pendingFrame >= 0 && Time.frameCount > pendingFrame)
            {
                int pf = pendingFrame; pendingFrame = -10;
                if (handledFrame < pf) PressAt(all, pendingPos);
            }
            var m = Mouse.current;
            if (m != null && m.leftButton.wasReleasedThisFrame) { pendingFrame = Time.frameCount; pendingPos = m.position.ReadValue(); }
        }

        static void PressAt(Button[] all, Vector2 pos)
        {
            Button hit = null; long bestKey = long.MinValue;
            foreach (var b in all)
            {
                if (b == null || !b.IsInteractable() || !b.gameObject.activeInHierarchy) continue;
                var canvas = b.GetComponentInParent<Canvas>();
                if (canvas == null) continue;
                var root = canvas.rootCanvas;
                Camera cam = root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
                if (!RectTransformUtility.RectangleContainsScreenPoint((RectTransform)b.transform, pos, cam)) continue;
                // the topmost wins: higher canvas order, then the panel that is last under the canvas
                Transform t = b.transform; while (t.parent != null && t.parent != root.transform) t = t.parent;
                long key = (long)root.sortingOrder * 100000L + t.GetSiblingIndex();
                if (key >= bestKey) { bestKey = key; hit = b; }
            }
            if (hit != null) hit.onClick.Invoke();
        }
    }
}
