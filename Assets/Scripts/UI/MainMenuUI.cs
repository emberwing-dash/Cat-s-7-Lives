using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace CatOnTheSill
{
    /// <summary>
    /// The main menu interface as a real, editable hierarchy (MenuCanvas in the MainMenu scene): move, resize, recolour
    /// or restyle anything under it. This component only wires the buttons, so keep the references below assigned.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        public GameObject titlePanel, controlsPanel;
        public Button playButton, howToPlayButton, quitButton, backButton;
        public Image fade;
        [Tooltip("Texts that still use the built-in font get a handwritten system font at runtime. Assign your own font to a Text to keep it.")]
        public bool useHandwrittenFont = true;

        float fadeTarget, fadeSpeed = 2f;

        /// <summary>Editor-time: creates the canvas, event system and all menu panels.</summary>
        public static MainMenuUI Bake(Font builtinFont)
        {
            var cg = new GameObject("MenuCanvas", typeof(RectTransform));
            var canvas = cg.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
            var scaler = cg.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1200); scaler.matchWidthOrHeight = 0.5f;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.referencePixelsPerUnit = 100f;
            cg.AddComponent<GraphicRaycaster>();

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

            var p = UIManager.BuildMenuParts((RectTransform)cg.transform, builtinFont);
            var ui = cg.AddComponent<MainMenuUI>();
            ui.titlePanel = p.title; ui.controlsPanel = p.controls;
            ui.playButton = p.play; ui.howToPlayButton = p.how; ui.quitButton = p.quit; ui.backButton = p.back; ui.fade = p.fade;
            p.title.name = "TitleScreen"; p.controls.name = "HowToPlayScreen"; p.fade.gameObject.name = "FadeOverlay";
            p.controls.SetActive(false);
            return ui;
        }

        void Awake()
        {
            UiClickFallback.Ensure();
            if (useHandwrittenFont) ApplyFont();
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
        }

        void ApplyFont()
        {
            var f = UiFont.Pick();
            if (f == null) return;
            foreach (var t in GetComponentsInChildren<Text>(true))
                if (t.font == null || t.font.name.Contains("LegacyRuntime") || t.font.name.Contains("Arial")) t.font = f;
        }

        public void Wire(Action onPlay, Action onQuit)
        {
            Hook(playButton, onPlay);
            Hook(quitButton, onQuit);
            Hook(howToPlayButton, () => ShowControls(true));
            Hook(backButton, () => ShowControls(false));
        }

        static void Hook(Button b, Action a)
        {
            if (b == null) return;
            b.onClick.AddListener(() =>
            {
                try { AudioManager.I?.Play2D("meow", 0.25f, UnityEngine.Random.Range(1.3f, 1.6f)); } catch (Exception) { }
                a?.Invoke();
            });
        }

        public void ShowTitle()
        {
            if (controlsPanel != null) controlsPanel.SetActive(false);
            if (titlePanel != null) titlePanel.SetActive(true);
            Select(playButton);
        }

        public void ShowControls(bool on)
        {
            if (controlsPanel != null) controlsPanel.SetActive(on);
            if (fade != null) fade.transform.SetAsLastSibling();
            Select(on ? backButton : playButton);
        }

        static void Select(Selectable s)
        {
            if (s == null || EventSystem.current == null) return;
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(s.gameObject);
        }

        public void SetFade(float a) { fadeTarget = a; if (fade != null) fade.color = new Color(0, 0, 0, a); }
        public void FadeTo(float a, float speed) { fadeTarget = a; fadeSpeed = speed; }

        void Update()
        {
            if (fade != null && !Mathf.Approximately(fade.color.a, fadeTarget))
                fade.color = new Color(0, 0, 0, Mathf.MoveTowards(fade.color.a, fadeTarget, fadeSpeed * Time.unscaledDeltaTime));
            if (controlsPanel != null && controlsPanel.activeSelf && Inp.PauseDown) ShowControls(false);
        }
    }
}



