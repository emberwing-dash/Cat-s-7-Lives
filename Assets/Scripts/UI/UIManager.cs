using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace CatOnTheSill
{
    /// <summary>All of the game's interface, built from code: title, HUD, dialogue, pause, time picker, ending.</summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager I;

        // ---- main menu pieces, so the MainMenu scene can bake them into the hierarchy (see MainMenuUI)
        Button titleHow, titleQuit, controlsBack;
        public struct MenuParts { public GameObject title, controls; public Button play, how, quit, back; public Image fade; }

        /// <summary>Builds just the title screen, the How to Play panel and the fade overlay under the given canvas.</summary>
        public static MenuParts BuildMenuParts(RectTransform canvasRoot, Font f)
        {
            var host = new GameObject("tmpUIBuilder");
            var m = host.AddComponent<UIManager>();
            m.font = f; m.root = canvasRoot;
            m.BuildTitle(); m.BuildControls(); m.BuildFade();
            var parts = new MenuParts { title = m.titlePanel, controls = m.controlsPanel, play = m.firstTitleButton, how = m.titleHow, quit = m.titleQuit, back = m.controlsBack, fade = m.fadeImg };
            DestroyImmediate(host);
            return parts;
        }

        /// <summary>Editor-time: creates the complete gameplay UI as an editable scene hierarchy.</summary>
        public static UIManager BakeGameUI(Font builtinFont)
        {
            var host = new GameObject("GameUI");
            var ui = host.AddComponent<UIManager>();
            ui.IgnoredEditorInit(builtinFont);
            return ui;
        }

        void IgnoredEditorInit(Font builtinFont)
        {
            I = this;
            font = builtinFont;
            var cg = new GameObject("GameCanvas", typeof(RectTransform));
            cg.transform.SetParent(transform, false);
            canvas = cg.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
            var scaler = cg.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1200); scaler.matchWidthOrHeight = 0.5f; scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; scaler.referencePixelsPerUnit = 100f;
            cg.AddComponent<GraphicRaycaster>(); root = cg.GetComponent<RectTransform>();
            BuildHud(); BuildTitle(); BuildControls(); BuildSolutionPanel(); BuildMapPanel(); BuildPause(); BuildTimePanel(); BuildIntro(); BuildEnd(); BuildFade();
            built = true;
            HideAll(); hudPanel.SetActive(true);
        }


        public Action OnNewGame, OnContinue, OnResume, OnRestart, OnReturnTitle, OnQuit;
        public bool HintsEnabled = true;
        public bool AudioOn = true;

        Canvas canvas;
        Font font;
        RectTransform root;

        // panels
        GameObject titlePanel, controlsPanel, hudPanel, pausePanel, timePanel, introPanel, endPanel, confirmPanel, solutionPanel;
        Button continueButton, firstTitleButton, timeFirstButton, resumeButton, solutionCloseButton;
        Text promptText, toastText, hintText, dialogueName, dialogueBody, counterText, timeLabel, timeBlurb, pauseHintLabel, pauseAudioLabel, introText, controlsBody, solutionTitle, solutionCaption;
        Text flashTitle, flashBody, flashCount;
        Image toastBg, hintBg, dialogueBg, promptBg, fadeImg, timeIcon, flashBg;
        HintStage hintStage;
        GameObject mapPanel; Button mapCloseButton;
        readonly Image[] orbs = new Image[FragmentInfo.Count];
        readonly Image[] orbGlow = new Image[FragmentInfo.Count];
        readonly Button[] timeButtons = new Button[6];
        readonly Text[] timeButtonLabels = new Text[6];

        float toastT, hintT, dialogueT, flashT, orbPulse;
        readonly Queue<string> toastQueue = new Queue<string>();
        string pendingPrompt = "";
        float fadeTarget, fadeSpeed = 2f;
        SkyWindow timeWindow;
        public bool TimePanelOpen => timePanel != null && timePanel.activeSelf;
        public bool ControlsOpen => controlsPanel != null && controlsPanel.activeSelf;
        public bool DialogueActive => dialogueT > 0f;

        static readonly Color Cream = new Color(1f, 0.95f, 0.82f);
        static readonly Color Ink = new Color(0.12f, 0.09f, 0.14f, 0.82f);
        static readonly Color Gold = new Color(1f, 0.82f, 0.4f);

        bool built;

        void EnsureEventSystem()
        {
            var es = EventSystem.current != null ? EventSystem.current : FindFirstObjectByType<EventSystem>();
            if (es == null)
            {
                var go = new GameObject("EventSystem");
                go.transform.SetParent(transform, false);
                es = go.AddComponent<EventSystem>();
            }

            var module = es.GetComponent<InputSystemUIInputModule>();
            if (module == null)
            {
                var oldModule = es.GetComponent<StandaloneInputModule>();
                if (oldModule != null) Destroy(oldModule);
                module = es.gameObject.AddComponent<InputSystemUIInputModule>();
            }
            module.AssignDefaultActions();
            es.enabled = true;
            module.enabled = true;
        }

        public void Init()
        {
            I = this;
            EnsureEventSystem();
            UiClickFallback.Ensure();
            if (built)
            {
                Game.FragmentCollected -= OnFragmentCollected;
                Game.Toast -= ShowToast;
                Game.FragmentCollected += OnFragmentCollected;
                Game.Toast += ShowToast;
                HideAll();
                return;
            }
            // a soft, handwritten-feeling system font instead of the default one (first one installed wins)
            font = UiFont.Pick();

            // EventSystem is configured by EnsureEventSystem above.

            var editableCanvas = transform.Find("GameCanvas");
            if (editableCanvas != null) editableCanvas.gameObject.SetActive(false);

            var cg = new GameObject("Canvas");
            canvas = cg.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = cg.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960, 600);
            scaler.referenceResolution = new Vector2(1920, 1200);
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;
            cg.AddComponent<GraphicRaycaster>();
            root = cg.GetComponent<RectTransform>();

            BuildHud();
            BuildTitle();
            BuildControls();
            BuildSolutionPanel();
            BuildMapPanel();
            BuildPause();
            BuildTimePanel();
            BuildIntro();
            BuildEnd();
            BuildFade();
            built = true;

            Game.FragmentCollected += OnFragmentCollected;
            Game.Toast += ShowToast;
            HideAll();
        }

        void OnDestroy()
        {
            Game.FragmentCollected -= OnFragmentCollected;
            Game.Toast -= ShowToast;
        }

        public void UseCamera(Camera cam) { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = 1f; }
        public void UseOverlay() { canvas.renderMode = RenderMode.ScreenSpaceOverlay; }

        // =================================================================== builders
        RectTransform R(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            var g = new GameObject(name, typeof(RectTransform));
            g.transform.SetParent(parent, false);
            var r = g.GetComponent<RectTransform>();
            r.anchorMin = aMin; r.anchorMax = aMax; r.offsetMin = oMin; r.offsetMax = oMax;
            return r;
        }

        RectTransform Full(string name, Transform parent) => R(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        RectTransform Anchored(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var r = R(name, parent, anchor, anchor, Vector2.zero, Vector2.zero);
            r.pivot = pivot; r.anchoredPosition = pos; r.sizeDelta = size;
            return r;
        }

        Image Img(string name, Transform parent, Color c, bool rounded = true)
        {
            var r = Full(name, parent);
            var i = r.gameObject.AddComponent<Image>();
            i.color = c; i.raycastTarget = false;
            if (rounded) { i.sprite = Mats.RoundedSprite(); i.type = Image.Type.Sliced; }
            return i;
        }

        Text Txt(string name, Transform parent, string s, int size, Color c, TextAnchor a = TextAnchor.MiddleCenter, FontStyle st = FontStyle.Normal)
        {
            var r = Full(name, parent);
            var t = r.gameObject.AddComponent<Text>();
            t.font = font; t.text = s; t.fontSize = size; t.color = c; t.alignment = a; t.fontStyle = st;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false; t.supportRichText = true;
            var sh = r.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.55f); sh.effectDistance = new Vector2(1.5f, -1.5f);
            return t;
        }

        internal static Sprite spriteCache(GlyphKind k)
        {
            return Sprite.Create(Mats.Glyph(k), new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 100);
        }

        static readonly Color ButtonOrange = new Color(0.98f, 0.66f, 0.33f);
        static readonly Color ButtonInk = new Color(0.30f, 0.15f, 0.08f);

        /// <summary>A cat-shaped button: orange tabby pill with two ears, a paw print and dark brown text.</summary>
        Button Btn(string name, Transform parent, string label, Vector2 pos, Vector2 size, Action onClick, int fontSize = 30)
        {
            var r = Anchored(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
            var img = r.gameObject.AddComponent<Image>();
            img.sprite = Mats.RoundedSprite(); img.type = Image.Type.Sliced; img.color = ButtonOrange;
            var b = r.gameObject.AddComponent<Button>();
            var cb = b.colors;
            cb.normalColor = Color.white; cb.highlightedColor = new Color(1.12f, 1.1f, 1.0f); cb.selectedColor = new Color(1.1f, 1.08f, 0.98f);
            cb.pressedColor = new Color(0.85f, 0.8f, 0.7f); cb.fadeDuration = 0.08f;
            b.colors = cb; b.targetGraphic = img;
            // ears (same colour, tucked behind the top edge)
            for (int side = -1; side <= 1; side += 2)
            {
                var ear = Anchored("Ear", r, new Vector2(0.5f, 1), new Vector2(0.5f, 0.2f), new Vector2(side * size.x * 0.36f, -2), new Vector2(size.y * 0.5f, size.y * 0.5f));
                ear.localRotation = Quaternion.Euler(0, 0, -side * 12f);
                var ei = ear.gameObject.AddComponent<Image>(); ei.sprite = spriteCache(GlyphKind.Ear); ei.color = ButtonOrange; ei.raycastTarget = false;
                ear.SetAsFirstSibling();
            }
            // paw print
            var paw = Anchored("Paw", r, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(size.y * 0.55f, 0), new Vector2(size.y * 0.5f, size.y * 0.5f));
            var pi = paw.gameObject.AddComponent<Image>(); pi.sprite = spriteCache(GlyphKind.Paw); pi.color = new Color(0.45f, 0.22f, 0.1f, 0.85f); pi.raycastTarget = false;
            var t = Txt("Label", r, label, fontSize, ButtonInk, TextAnchor.MiddleCenter, FontStyle.Bold);
            var sh = t.GetComponent<Shadow>(); if (sh != null) sh.effectColor = new Color(1f, 0.95f, 0.85f, 0.35f);
            b.onClick.AddListener(() =>
            {
                try { AudioManager.I?.Play2D("meow", 0.25f, UnityEngine.Random.Range(1.3f, 1.6f)); } catch (Exception) { }
                onClick?.Invoke();
            });
            return b;
        }

        void Panel(ref GameObject go, string name, Color tint, bool block = true)
        {
            var r = Full(name, root);
            go = r.gameObject;
            var img = go.AddComponent<Image>();
            img.color = tint; img.raycastTarget = block;
        }

        Text objTitle, objBody, compassLabel; Image objBg, compassImg;

        public void SetObjective(string title, string body)
        {
            if (objTitle == null) return;
            objTitle.text = title; objBody.text = body;
            objBg.gameObject.SetActive(!string.IsNullOrEmpty(title));
        }

        public void SetCompass(bool on, float angle, float dist)
        {
            if (compassImg == null) return;
            compassImg.gameObject.SetActive(on);
            if (!on) return;
            compassImg.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
            compassLabel.text = dist < 1.2f ? "here!" : Mathf.RoundToInt(dist) + " m";
        }

        void BuildHud()
        {
            var r = Full("HUD", root); hudPanel = r.gameObject;
            var ob = Anchored("Objective", r, new Vector2(0, 1), new Vector2(0, 1), new Vector2(34, -100), new Vector2(680, 160));
            objBg = ob.gameObject.AddComponent<Image>(); objBg.sprite = Mats.RoundedSprite(); objBg.type = Image.Type.Sliced; objBg.color = new Color(0.1f, 0.07f, 0.12f, 0.72f); objBg.raycastTarget = false;
            objTitle = Txt("T", ob, "", 30, Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            objTitle.rectTransform.offsetMin = new Vector2(20, 0); objTitle.rectTransform.offsetMax = new Vector2(-20, -12);
            objBody = Txt("B", ob, "", 24, Cream, TextAnchor.UpperLeft);
            objBody.rectTransform.offsetMin = new Vector2(20, 8); objBody.rectTransform.offsetMax = new Vector2(-20, -54);
            objBg.gameObject.SetActive(false);
            var cp = Anchored("Compass", r, new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -110), new Vector2(64, 64));
            compassImg = cp.gameObject.AddComponent<Image>(); compassImg.raycastTarget = false;
            compassImg.sprite = Sprite.Create(Mats.Glyph(GlyphKind.Arrow), new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 100);
            compassImg.color = new Color(1f, 0.85f, 0.35f);
            var cl = Anchored("CompassLabel", r, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -148), new Vector2(200, 36));
            compassLabel = Txt("t", cl, "", 24, Cream);
            compassImg.gameObject.SetActive(false);

            // fragment orbs, top-left
            var bar = Anchored("Fragments", r, new Vector2(0, 1), new Vector2(0, 1), new Vector2(34, -30), new Vector2(430, 52));
            var bg = bar.gameObject.AddComponent<Image>(); bg.sprite = Mats.RoundedSprite(); bg.type = Image.Type.Sliced; bg.color = new Color(0.1f, 0.07f, 0.12f, 0.62f); bg.raycastTarget = false;
            for (int i = 0; i < FragmentInfo.Count; i++)
            {
                var o = Anchored("Orb" + i, bar, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(34 + i * 40, 0), new Vector2(26, 26));
                var glow = Anchored("Glow", o, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(56, 56));
                var gi = glow.gameObject.AddComponent<Image>(); gi.sprite = Mats.SoftSprite(); gi.color = new Color(1, 1, 1, 0); gi.raycastTarget = false;
                var oi = o.gameObject.AddComponent<Image>(); oi.sprite = Mats.SoftSprite(); oi.color = new Color(0.3f, 0.28f, 0.35f, 0.9f); oi.raycastTarget = false;
                orbs[i] = oi; orbGlow[i] = gi;
            }
            counterText = Txt("Counter", bar, "0 / 7", 26, Cream, TextAnchor.MiddleRight, FontStyle.Bold);
            counterText.rectTransform.offsetMax = new Vector2(-18, 0);

            // time of day, top-right
            var tb = Anchored("TimeBox", r, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-34, -30), new Vector2(250, 52));
            var tbg = tb.gameObject.AddComponent<Image>(); tbg.sprite = Mats.RoundedSprite(); tbg.type = Image.Type.Sliced; tbg.color = new Color(0.1f, 0.07f, 0.12f, 0.62f); tbg.raycastTarget = false;
            var ic = Anchored("Icon", tb, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(30, 0), new Vector2(34, 34));
            timeIcon = ic.gameObject.AddComponent<Image>(); timeIcon.raycastTarget = false;
            timeIcon.sprite = Sprite.Create(Mats.Glyph(GlyphKind.Sun), new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 100);
            timeLabel = Txt("TimeLabel", tb, "Morning", 26, Cream, TextAnchor.MiddleLeft, FontStyle.Bold);
            timeLabel.rectTransform.offsetMin = new Vector2(56, 0);

            // prompt, bottom-center
            var pr = Anchored("Prompt", r, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 54), new Vector2(1200, 54));
            promptBg = pr.gameObject.AddComponent<Image>(); promptBg.sprite = Mats.RoundedSprite(); promptBg.type = Image.Type.Sliced; promptBg.color = new Color(0.1f, 0.07f, 0.12f, 0f); promptBg.raycastTarget = false;
            promptText = Txt("Text", pr, "", 28, Cream);

            // toast, top-center
            var ts = Anchored("Toast", r, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -34), new Vector2(900, 56));
            toastBg = ts.gameObject.AddComponent<Image>(); toastBg.sprite = Mats.RoundedSprite(); toastBg.type = Image.Type.Sliced; toastBg.color = new Color(0.1f, 0.07f, 0.12f, 0f); toastBg.raycastTarget = false;
            toastText = Txt("Text", ts, "", 28, Gold, TextAnchor.MiddleCenter, FontStyle.Italic);

            // hint solution card, left side under the objective
            var hn = Anchored("Hint", r, new Vector2(0, 1), new Vector2(0, 1), new Vector2(34, -280), new Vector2(620, 190));
            hintBg = hn.gameObject.AddComponent<Image>(); hintBg.sprite = Mats.RoundedSprite(); hintBg.type = Image.Type.Sliced; hintBg.color = new Color(0.1f, 0.07f, 0.12f, 0f); hintBg.raycastTarget = false;
            var solutionIcon = hn.gameObject.AddComponent<Outline>(); solutionIcon.effectColor = new Color(1f, 0.82f, 0.4f, 0.55f); solutionIcon.effectDistance = new Vector2(2f, -2f);
            hintText = Txt("Text", hn, "", 27, new Color(0.95f, 0.98f, 1f), TextAnchor.MiddleLeft, FontStyle.Bold);
            hintText.rectTransform.offsetMin = new Vector2(24, 12); hintText.rectTransform.offsetMax = new Vector2(-24, -12);

            // dialogue
            var dg = Anchored("Dialogue", r, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 124), new Vector2(1080, 170));
            dialogueBg = dg.gameObject.AddComponent<Image>(); dialogueBg.sprite = Mats.RoundedSprite(); dialogueBg.type = Image.Type.Sliced; dialogueBg.color = new Color(0.1f, 0.07f, 0.12f, 0f); dialogueBg.raycastTarget = false;
            dialogueName = Txt("Name", dg, "", 30, Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            dialogueName.rectTransform.offsetMin = new Vector2(30, 0); dialogueName.rectTransform.offsetMax = new Vector2(-30, -16);
            dialogueBody = Txt("Body", dg, "", 28, Cream, TextAnchor.UpperLeft);
            dialogueBody.rectTransform.offsetMin = new Vector2(30, 8); dialogueBody.rectTransform.offsetMax = new Vector2(-30, -58);

            // fragment flash (center)
            var fl = Anchored("Flash", r, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 140), new Vector2(980, 250));
            flashBg = fl.gameObject.AddComponent<Image>(); flashBg.sprite = Mats.RoundedSprite(); flashBg.type = Image.Type.Sliced; flashBg.color = new Color(0.08f, 0.06f, 0.14f, 0f); flashBg.raycastTarget = false;
            flashCount = Txt("Count", fl, "", 26, Gold, TextAnchor.UpperCenter, FontStyle.Bold); flashCount.rectTransform.offsetMax = new Vector2(0, -18);
            flashTitle = Txt("Title", fl, "", 46, Cream, TextAnchor.UpperCenter, FontStyle.Bold); flashTitle.rectTransform.offsetMax = new Vector2(0, -56);
            flashBody = Txt("Body", fl, "", 26, new Color(0.9f, 0.9f, 1f), TextAnchor.UpperCenter, FontStyle.Italic);
            flashBody.rectTransform.offsetMin = new Vector2(60, 10); flashBody.rectTransform.offsetMax = new Vector2(-60, -122);
        }

        void BuildTitle()
        {
            Panel(ref titlePanel, "Title", new Color(0.05f, 0.03f, 0.1f, 0.0f));
            var r = titlePanel.GetComponent<RectTransform>();
            var shade = Img("Shade", r, new Color(0.04f, 0.02f, 0.1f, 0.55f), false);
            shade.rectTransform.anchorMax = new Vector2(0.42f, 1f);
            var title = Anchored("Name", r, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(90, 230), new Vector2(760, 330));
            var t = Txt("T", title, "CAT'S\n7 LIVES", 118, Cream, TextAnchor.MiddleLeft, FontStyle.Bold);
            t.lineSpacing = 0.85f;
            var sub = Anchored("Sub", r, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(94, 40), new Vector2(740, 80));
            Txt("S", sub, "a cozy little puzzle about windows, light and the hours of the day", 30, new Color(1f, 0.85f, 0.6f), TextAnchor.UpperLeft, FontStyle.Italic);
            firstTitleButton = BtnL("New", r, "Play", new Vector2(94, -90), () => OnNewGame?.Invoke());
#if !UNITY_WEBGL
            titleHow = BtnL("How", r, "How to Play", new Vector2(94, -210), () => ShowControls(true));
            titleQuit = BtnL("Quit", r, "Quit", new Vector2(94, -330), () => OnQuit?.Invoke());
#endif
            var foot = Anchored("Foot", r, new Vector2(0f, 0), new Vector2(0f, 0), new Vector2(94, 54), new Vector2(780, 80));
            Txt("F", foot, "Seven soul fragments are scattered through the house. Follow the light.", 26, new Color(0.95f, 0.9f, 1f, 0.9f), TextAnchor.LowerLeft, FontStyle.Italic);
        }

        Button BtnL(string name, Transform parent, string label, Vector2 pos, Action onClick)
        {
            var b = Btn(name, parent, label, Vector2.zero, new Vector2(400, 84), onClick);
            var rt = b.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f); rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = pos;
            return b;
        }

        // ---- pictorial "How to Play": key caps and pictures, almost no words
        RectTransform KeyCap(Transform parent, string label, Vector2 pos, Vector2 size, int fs = 34)
        {
            var r = Anchored("Key_" + label, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
            var shadow = Anchored("S", r, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -6), size);
            var si = shadow.gameObject.AddComponent<Image>(); si.sprite = Mats.RoundedSprite(); si.type = Image.Type.Sliced; si.color = new Color(0.45f, 0.25f, 0.12f); si.raycastTarget = false;
            var top = Anchored("T", r, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            var ti = top.gameObject.AddComponent<Image>(); ti.sprite = Mats.RoundedSprite(); ti.type = Image.Type.Sliced; ti.color = new Color(1f, 0.93f, 0.78f); ti.raycastTarget = false;
            var t = Txt("L", top, label, fs, ButtonInk, TextAnchor.MiddleCenter, FontStyle.Bold);
            var sh = t.GetComponent<Shadow>(); if (sh != null) sh.enabled = false;
            return r;
        }

        Image Pic(Transform parent, GlyphKind k, Vector2 pos, float size, Color c)
        {
            var r = Anchored("Pic_" + k, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));
            var i = r.gameObject.AddComponent<Image>(); i.sprite = spriteCache(k); i.color = c; i.raycastTarget = false;
            return i;
        }

        RectTransform Tile(Transform parent, Vector2 pos, string caption)
        {
            var r = Anchored("Tile", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(330, 280));
            var i = r.gameObject.AddComponent<Image>(); i.sprite = Mats.RoundedSprite(); i.type = Image.Type.Sliced; i.color = new Color(0.33f, 0.2f, 0.12f, 0.95f); i.raycastTarget = false;
            var c = Anchored("Cap", r, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(310, 46));
            Txt("t", c, caption, 30, new Color(1f, 0.85f, 0.6f), TextAnchor.MiddleCenter, FontStyle.Bold);
            return r;
        }

        void BuildControls()
        {
            Panel(ref controlsPanel, "Controls", new Color(0.05f, 0.03f, 0.06f, 0.9f));
            var r = controlsPanel.GetComponent<RectTransform>();
            var card = Anchored("Card", r, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(1160, 1000));
            var ci = card.gameObject.AddComponent<Image>(); ci.sprite = Mats.RoundedSprite(); ci.type = Image.Type.Sliced; ci.color = new Color(0.22f, 0.13f, 0.09f, 0.97f);
            var h = Anchored("H", card, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -34), new Vector2(900, 80));
            Txt("T", h, "How to Play", 54, Cream, TextAnchor.MiddleCenter, FontStyle.Bold);

            Color orange = new Color(1f, 0.7f, 0.35f);
            float x0 = -375, x1 = 0, x2 = 375, y0 = 205, y1 = -105;

            // WALK
            var t = Tile(card, new Vector2(x0, y0), "Walk");
            KeyCap(t, "W", new Vector2(0, 62), new Vector2(64, 64)); KeyCap(t, "A", new Vector2(-72, -10), new Vector2(64, 64));
            KeyCap(t, "S", new Vector2(0, -10), new Vector2(64, 64)); KeyCap(t, "D", new Vector2(72, -10), new Vector2(64, 64));
            Pic(t, GlyphKind.Paw, new Vector2(-110, -92), 58, orange); Pic(t, GlyphKind.Paw, new Vector2(110, -92), 58, orange);
            // LOOK
            t = Tile(card, new Vector2(x1, y0), "Look");
            var mouse = Anchored("Mouse", t, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-70, 20), new Vector2(86, 126));
            var mi = mouse.gameObject.AddComponent<Image>(); mi.sprite = Mats.RoundedSprite(); mi.type = Image.Type.Sliced; mi.color = new Color(1f, 0.93f, 0.78f); mi.raycastTarget = false;
            var mline = Anchored("L", mouse, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(6, 46));
            var ml = mline.gameObject.AddComponent<Image>(); ml.color = new Color(0.45f, 0.25f, 0.12f); ml.raycastTarget = false;
            Pic(t, GlyphKind.Eye, new Vector2(65, 20), 110, orange);
            Pic(t, GlyphKind.Arrow, new Vector2(-70, -88), 44, new Color(1f, 0.85f, 0.5f)).rectTransform.localRotation = Quaternion.Euler(0, 0, 90);
            Pic(t, GlyphKind.Arrow, new Vector2(-20, -88), 44, new Color(1f, 0.85f, 0.5f)).rectTransform.localRotation = Quaternion.Euler(0, 0, -90);
            // JUMP
            t = Tile(card, new Vector2(x2, y0), "Jump & climb");
            KeyCap(t, "SPACE", new Vector2(0, 52), new Vector2(230, 64), 30);
            Pic(t, GlyphKind.Arrow, new Vector2(-70, -60), 90, orange);
            var box = Pic(t, GlyphKind.Square, new Vector2(75, -78), 70, new Color(1f, 0.85f, 0.5f));
            // PAW
            t = Tile(card, new Vector2(x0, y1), "Paw & meow");
            KeyCap(t, "E", new Vector2(-60, 40), new Vector2(86, 86), 46);
            Pic(t, GlyphKind.Paw, new Vector2(70, 40), 110, orange);
            Pic(t, GlyphKind.Star, new Vector2(-60, -64), 44, new Color(1f, 0.9f, 0.5f)); Pic(t, GlyphKind.Sun, new Vector2(0, -64), 44, new Color(1f, 0.9f, 0.5f)); Pic(t, GlyphKind.Moon, new Vector2(60, -64), 44, new Color(0.7f, 0.85f, 1f));
            // DRAG
            t = Tile(card, new Vector2(x1, y1), "Hold to drag");
            KeyCap(t, "F", new Vector2(-70, 40), new Vector2(86, 86), 46);
            Pic(t, GlyphKind.Square, new Vector2(60, 40), 86, new Color(1f, 0.85f, 0.5f));
            Pic(t, GlyphKind.Arrow, new Vector2(115, 40), 44, orange).rectTransform.localRotation = Quaternion.Euler(0, 0, -90);
            Pic(t, GlyphKind.Paw, new Vector2(-10, -62), 52, orange);
            // HOURS
            t = Tile(card, new Vector2(x2, y1), "Change the hour");
            Pic(t, GlyphKind.Sun, new Vector2(-70, 38), 100, new Color(1f, 0.85f, 0.35f));
            Pic(t, GlyphKind.Moon, new Vector2(70, 38), 100, new Color(0.7f, 0.85f, 1f));
            KeyCap(t, "E", new Vector2(0, -62), new Vector2(60, 60), 34);

            // goal strip: collect seven glowing orbs
            var goal = Anchored("Goal", card, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 170), new Vector2(1040, 100));
            var gi = goal.gameObject.AddComponent<Image>(); gi.sprite = Mats.RoundedSprite(); gi.type = Image.Type.Sliced; gi.color = new Color(0.1f, 0.07f, 0.12f, 0.85f); gi.raycastTarget = false;
            for (int i = 0; i < 7; i++)
            {
                var o = Anchored("Orb" + i, goal, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-300 + i * 100, 0), new Vector2(70, 70));
                var oi = o.gameObject.AddComponent<Image>(); oi.sprite = Mats.SoftSprite(); oi.color = FragmentInfo.Colors[i]; oi.raycastTarget = false;
                var ring = Anchored("R", o, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(62, 62));
                var rg = ring.gameObject.AddComponent<Image>(); rg.sprite = spriteCache(GlyphKind.Ring); rg.color = new Color(1, 1, 1, 0.9f); rg.raycastTarget = false;
            }
            controlsBack = Btn("Back", card, "Back", new Vector2(0, -440), new Vector2(300, 80), () => ShowControls(false));
        }

        void BuildSolutionPanel()
        {
            Panel(ref solutionPanel, "SolutionPicture", new Color(0.03f, 0.02f, 0.05f, 0.9f));
            var r = solutionPanel.GetComponent<RectTransform>();
            var card = Anchored("Card", r, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1120, 760));
            var ci = card.gameObject.AddComponent<Image>(); ci.sprite = Mats.RoundedSprite(); ci.type = Image.Type.Sliced; ci.color = new Color(0.22f, 0.13f, 0.09f, 0.98f);
            solutionTitle = Txt("Title", card, "SOLUTION", 56, Cream, TextAnchor.MiddleCenter, FontStyle.Bold);
            solutionTitle.rectTransform.offsetMin = new Vector2(40, 630); solutionTitle.rectTransform.offsetMax = new Vector2(-40, -30);
            hintStage = HintStage.Create(card, font);
            solutionCaption = Txt("Caption", card, "", 30, new Color(1f, 0.88f, 0.62f), TextAnchor.MiddleCenter, FontStyle.Italic);
            solutionCaption.rectTransform.offsetMin = new Vector2(80, 105); solutionCaption.rectTransform.offsetMax = new Vector2(-80, -570);
            solutionCloseButton = Btn("Close", card, "Back", new Vector2(0, -315), new Vector2(300, 78), HideSolutionPicture);
            solutionPanel.SetActive(false);
        }

        public void ShowSolutionPicture(int fragmentIndex, string caption)
        {
            if (solutionPanel == null) return;
            int i = Mathf.Clamp(fragmentIndex, 0, 6);
            solutionTitle.text = "HOW TO  •  SOUL FRAGMENT";
            solutionCaption.text = caption;
            hintStage.Play(PuzzleGuide.LastKind, PuzzleGuide.LastGlyph, PuzzleGuide.LastNight, PuzzleGuide.LastHour);
            solutionPanel.SetActive(true);
            solutionPanel.transform.SetAsLastSibling();
            Select(solutionCloseButton);
            GameManager.I?.SetUiMode(true);
        }

        void BuildMapPanel()
        {
            Panel(ref mapPanel, "MapPanel", new Color(0.03f, 0.02f, 0.05f, 0.9f));
            var r = mapPanel.GetComponent<RectTransform>();
            var card = Anchored("Card", r, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100, 860));
            var ci = card.gameObject.AddComponent<Image>(); ci.sprite = Mats.RoundedSprite(); ci.type = Image.Type.Sliced; ci.color = new Color(0.22f, 0.13f, 0.09f, 0.98f);
            var title = Txt("Title", card, "MAP", 50, Cream, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.offsetMin = new Vector2(40, 770); title.rectTransform.offsetMax = new Vector2(-40, -20);
            MapView.Create(card, font);
            // legend
            var lg = Anchored("Legend", card, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 96), new Vector2(1000, 44));
            Pic(lg, GlyphKind.Ring, new Vector2(-380, 0), 36, new Color(0.15f, 0.08f, 0.04f));
            var t1 = Txt("t1", lg, "puzzle to solve", 22, Cream, TextAnchor.MiddleLeft); t1.rectTransform.offsetMin = new Vector2(120, 0);
            Pic(lg, GlyphKind.Star, new Vector2(-110, 0), 36, new Color(1f, 0.9f, 0.5f));
            var t2 = Txt("t2", lg, "orb waiting - go get it", 22, Cream, TextAnchor.MiddleLeft); t2.rectTransform.offsetMin = new Vector2(390, 0);
            Pic(lg, GlyphKind.Arrow, new Vector2(240, 0), 32, new Color(1f, 0.7f, 0.3f));
            var t3 = Txt("t3", lg, "you", 22, Cream, TextAnchor.MiddleLeft); t3.rectTransform.offsetMin = new Vector2(790, 0);
            mapCloseButton = Btn("Close", card, "Back  (M)", new Vector2(0, -388), new Vector2(300, 78), HideMap);
            mapPanel.SetActive(false);
        }

        public bool SolutionOpen => solutionPanel != null && solutionPanel.activeSelf;
        public bool MapOpen => mapPanel != null && mapPanel.activeSelf;

        public void ShowMap()
        {
            if (mapPanel == null) return;
            mapPanel.SetActive(true);
            mapPanel.transform.SetAsLastSibling();
            Select(mapCloseButton);
            GameManager.I?.SetUiMode(true);
        }

        public void HideMap()
        {
            if (mapPanel != null && mapPanel.activeSelf) { mapPanel.SetActive(false); GameManager.I?.SetUiMode(false); }
        }

        public void HideSolutionPicture()
        {
            if (solutionPanel != null) solutionPanel.SetActive(false);
            GameManager.I?.SetUiMode(false);
        }


        void BuildPause()
        {
            Panel(ref pausePanel, "Pause", new Color(0.04f, 0.03f, 0.08f, 0.72f));
            var r = pausePanel.GetComponent<RectTransform>();
            var h = Anchored("H", r, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 290), new Vector2(900, 100));
            Txt("T", h, "Paused", 78, Cream, TextAnchor.MiddleCenter, FontStyle.Bold);
            resumeButton = Btn("Resume", r, "Resume", new Vector2(0, 110), new Vector2(460, 84), () => OnResume?.Invoke());
            Btn("Restart", r, "Restart", new Vector2(0, -30), new Vector2(460, 84), () => ShowConfirm("Restart from the very beginning?\nYour progress will be erased.", () => OnRestart?.Invoke()));
            Btn("Title", r, "Main Menu", new Vector2(0, -170), new Vector2(460, 84), () => OnReturnTitle?.Invoke());

            // confirm dialog
            Panel(ref confirmPanel, "Confirm", new Color(0, 0, 0, 0.6f));
            var cr = confirmPanel.GetComponent<RectTransform>();
            confirmPanel.transform.SetParent(pausePanel.transform, false);
            var box = Anchored("Box", cr, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 320));
            var bi = box.gameObject.AddComponent<Image>(); bi.sprite = Mats.RoundedSprite(); bi.type = Image.Type.Sliced; bi.color = new Color(0.15f, 0.1f, 0.2f, 0.98f);
            confirmText = Txt("Msg", box, "", 34, Cream, TextAnchor.UpperCenter);
            confirmText.rectTransform.offsetMin = new Vector2(30, 100); confirmText.rectTransform.offsetMax = new Vector2(-30, -40);
            Btn("Yes", box, "Yes", new Vector2(-150, -105), new Vector2(240, 72), () => { var a = confirmAction; confirmPanel.SetActive(false); a?.Invoke(); });
            Btn("No", box, "No", new Vector2(150, -105), new Vector2(240, 72), () => confirmPanel.SetActive(false));
            confirmPanel.SetActive(false);
        }

        Text confirmText; Action confirmAction;
        void ShowConfirm(string msg, Action a) { confirmText.text = msg; confirmAction = a; confirmPanel.SetActive(true); }

        void BuildTimePanel()
        {
            Panel(ref timePanel, "TimePanel", new Color(0.04f, 0.03f, 0.1f, 0.55f));
            var r = timePanel.GetComponent<RectTransform>();
            var box = Anchored("Box", r, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 640));
            var bi = box.gameObject.AddComponent<Image>(); bi.sprite = Mats.RoundedSprite(); bi.type = Image.Type.Sliced; bi.color = new Color(0.12f, 0.09f, 0.18f, 0.95f);
            var h = Anchored("H", box, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(900, 70));
            Txt("T", h, "Look out the window...", 50, Cream, TextAnchor.MiddleCenter, FontStyle.Bold);
            var sub = Anchored("S", box, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -105), new Vector2(900, 44));
            Txt("T", sub, "Choose the hour. Light, shadows and shadows' shapes all change with it.", 26, new Color(1f, 0.85f, 0.6f), TextAnchor.MiddleCenter, FontStyle.Italic);
            timeBlurb = Txt("Blurb", box, "", 26, new Color(0.85f, 0.95f, 1f), TextAnchor.MiddleCenter, FontStyle.Italic);
            timeBlurb.rectTransform.offsetMin = new Vector2(30, 90); timeBlurb.rectTransform.offsetMax = new Vector2(-30, -520);
            for (int i = 0; i < 6; i++)
            {
                int ti = i;
                int col = i % 3, row = i / 3;
                var b = Btn("T" + i, box, Times.All[i].name, new Vector2(-310 + col * 310, 50 - row * 130), new Vector2(280, 100), () => ChooseTime((TimeOfDay)ti), 34);
                timeButtons[i] = b; timeButtonLabels[i] = b.GetComponentInChildren<Text>();
                var evt = b.gameObject.AddComponent<EventTrigger>();
                var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                enter.callback.AddListener(_ => timeBlurb.text = Times.All[ti].blurb);
                evt.triggers.Add(enter);
                var sel = new EventTrigger.Entry { eventID = EventTriggerType.Select };
                sel.callback.AddListener(_ => timeBlurb.text = Times.All[ti].blurb);
                evt.triggers.Add(sel);
            }
            timeFirstButton = timeButtons[0];
            timePanel.SetActive(false);
        }

        void BuildIntro()
        {
            Panel(ref introPanel, "Intro", new Color(0.02f, 0.015f, 0.05f, 0.96f));
            var r = introPanel.GetComponent<RectTransform>();
            introText = Txt("T", r, "", 40, Cream, TextAnchor.MiddleCenter, FontStyle.Italic);
            introText.rectTransform.offsetMin = new Vector2(260, 140); introText.rectTransform.offsetMax = new Vector2(-260, -140);
            introText.lineSpacing = 1.2f;
            var skip = Anchored("Skip", r, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 60), new Vector2(900, 50));
            Txt("S", skip, "press E, Space or click to continue", 24, new Color(1, 1, 1, 0.5f));
        }

        void BuildEnd()
        {
            Panel(ref endPanel, "End", new Color(0.02f, 0.015f, 0.05f, 0f));
            var r = endPanel.GetComponent<RectTransform>();
            endText = Txt("T", r, "", 44, Cream, TextAnchor.MiddleCenter, FontStyle.Italic);
            endText.rectTransform.offsetMin = new Vector2(240, 220); endText.rectTransform.offsetMax = new Vector2(-240, -140);
            endText.lineSpacing = 1.2f;
            endTitle = Txt("Title", r, "", 100, Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            endTitle.rectTransform.offsetMin = new Vector2(100, 200); endTitle.rectTransform.offsetMax = new Vector2(-100, 0);
            endThanks = Txt("Thanks", r, "", 34, Cream, TextAnchor.MiddleCenter, FontStyle.Italic);
            endThanks.rectTransform.offsetMin = new Vector2(100, 190); endThanks.rectTransform.offsetMax = new Vector2(-100, -700);
            endButton = Btn("Back", r, "Return to Title", new Vector2(0, -400), new Vector2(460, 84), () => OnReturnTitle?.Invoke());
            endButton.gameObject.SetActive(false);
            endPanel.GetComponent<Image>().raycastTarget = false;
        }

        Text endText, endTitle, endThanks; Button endButton;

        void BuildFade()
        {
            var r = Full("Fade", root);
            r.SetAsLastSibling();
            fadeImg = r.gameObject.AddComponent<Image>();
            fadeImg.color = new Color(0, 0, 0, 0); fadeImg.raycastTarget = false;
        }

        // =================================================================== visibility
        public void HideAll()
        {
            titlePanel.SetActive(false); controlsPanel.SetActive(false); solutionPanel.SetActive(false); if (mapPanel != null) mapPanel.SetActive(false); hudPanel.SetActive(false);
            pausePanel.SetActive(false); timePanel.SetActive(false); introPanel.SetActive(false); endPanel.SetActive(false);
            confirmPanel.SetActive(false);
        }

        void Select(Selectable s)
        {
            if (s == null || EventSystem.current == null) return;
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(s.gameObject);
        }

        public void ShowTitle()
        {
            HideAll();
            titlePanel.SetActive(true);
            Select(firstTitleButton);
        }

        public void ShowHud(bool on) { hudPanel.SetActive(on); if (on) RefreshHud(); }
        public void ShowPause(bool on)
        {
            pausePanel.SetActive(on);
            if (!on) confirmPanel.SetActive(false);
            if (on) Select(resumeButton);
        }

        public void ShowControls(bool on)
        {
            controlsPanel.SetActive(on);
            controlsPanel.transform.SetAsLastSibling();
            fadeImg.transform.SetAsLastSibling();
            if (on) Select(controlsPanel.GetComponentInChildren<Button>());
            else if (titlePanel.activeSelf) Select(firstTitleButton);
            else if (pausePanel.activeSelf) Select(resumeButton);
        }

        // Modal state includes the pictorial solution panel below.
        public bool AnyModalOpen => titlePanel.activeSelf || pausePanel.activeSelf || timePanel.activeSelf || controlsPanel.activeSelf || solutionPanel.activeSelf || MapOpen || introPanel.activeSelf || endPanel.activeSelf;
        public void OpenTimePanel(SkyWindow w)
        {
            timeWindow = w;
            timePanel.SetActive(true);
            UpdateTimeButtons();
            timeBlurb.text = Times.Get(TimeManager.Current).blurb;
            Select(timeButtons[(int)TimeManager.Current]);
            GameManager.I?.SetUiMode(true);
        }

        public void CloseTimePanel()
        {
            timePanel.SetActive(false);
            GameManager.I?.SetUiMode(false);
        }

        public void ChooseTime(TimeOfDay t)
        {
            CloseTimePanel();
            if (TimeManager.I == null) return;
            if (t == TimeManager.I.Target && TimeManager.I.Settled) return;
            GameManager.I?.ChangeTime(t);
        }

        void UpdateTimeButtons()
        {
            for (int i = 0; i < 6; i++)
            {
                bool cur = i == (int)TimeManager.Current;
                timeButtonLabels[i].text = Times.All[i].name + (cur ? "  *" : "");
                timeButtonLabels[i].color = cur ? Gold : Cream;
            }
        }

        // =================================================================== HUD updates
        public void SetPrompt(string s) { pendingPrompt = s; }

        public void ShowToast(string s) { toastQueue.Enqueue(s); }

        public void ShowHint(string s)
        {
            if (!HintsEnabled) return;
            hintText.text = "<b>SOLUTION</b>\n" + s; hintT = 15f;
        }

        public void ShowDialogue(string speaker, string text, Color? nameColor = null)
        {
            dialogueName.text = speaker; dialogueName.color = nameColor ?? Gold;
            dialogueBody.text = text;
            dialogueT = 2.5f + text.Length * 0.045f;
            AudioManager.I?.Play2D("voice", 0.35f, UnityEngine.Random.Range(0.9f, 1.3f));
        }

        public void HideDialogue() { dialogueT = 0; }

        public void FragmentFlash(int index)
        {
            flashTitle.text = FragmentInfo.Titles[index];
            flashBody.text = FragmentInfo.Memories[index];
            flashCount.text = $"SOUL FRAGMENT RECOVERED   {Game.Collected} / {FragmentInfo.Count}";
            flashT = 7f; orbPulse = 2f;
        }

        void OnFragmentCollected(int i) { lastCollected = i; RefreshHud(); FragmentFlash(i); }

        public void RefreshHud()
        {
            counterText.text = $"{Game.Collected} / {FragmentInfo.Count}";
            for (int i = 0; i < FragmentInfo.Count; i++)
            {
                bool has = Game.Has(i);
                orbs[i].color = has ? FragmentInfo.Colors[i] : new Color(0.3f, 0.28f, 0.35f, 0.9f);
                orbGlow[i].color = has ? new Color(FragmentInfo.Colors[i].r, FragmentInfo.Colors[i].g, FragmentInfo.Colors[i].b, 0.55f) : new Color(1, 1, 1, 0);
            }
            RefreshTime();
        }

        public void RefreshTime()
        {
            if (TimeManager.I == null) return;
            var t = TimeManager.I.Target;
            var d = Times.Get(t);
            timeLabel.text = d.name;
            timeIcon.sprite = Sprite.Create(Mats.Glyph(d.moon ? GlyphKind.Moon : GlyphKind.Sun), new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 100);
            timeIcon.color = d.moon ? new Color(0.7f, 0.82f, 1f) : Color.Lerp(new Color(1f, 0.6f, 0.3f), new Color(1f, 0.9f, 0.5f), t == TimeOfDay.Noon || t == TimeOfDay.Morning ? 1f : 0f);
        }

        // =================================================================== fades & sequences
        public void FadeTo(float a, float speed = 2f) { fadeTarget = a; fadeSpeed = speed; }
        public void SetFade(float a) { fadeTarget = a; fadeImg.color = new Color(0, 0, 0, a); }
        public float FadeAlpha => fadeImg.color.a;

        public IEnumerator ShowIntroSequence(string[] pages, Action done)
        {
            HideAll();
            introPanel.SetActive(true);
            foreach (var p in pages)
            {
                introText.text = p;
                introText.color = new Color(Cream.r, Cream.g, Cream.b, 0);
                for (float t = 0; t < 1f; t += Time.unscaledDeltaTime * 1.4f) { introText.color = new Color(Cream.r, Cream.g, Cream.b, t); yield return null; }
                introText.color = Cream;
                yield return null;
                while (!(Inp.ConfirmDown || (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame))) yield return null;
                for (float t = 0; t < 1f; t += Time.unscaledDeltaTime * 2.5f) { introText.color = new Color(Cream.r, Cream.g, Cream.b, 1 - t); yield return null; }
            }
            introPanel.SetActive(false);
            done?.Invoke();
        }

        public IEnumerator ShowEndSequence(string[] lines)
        {
            hudPanel.SetActive(false);
            endPanel.SetActive(true);
            endButton.gameObject.SetActive(false);
            var img = endPanel.GetComponent<Image>();
            endText.text = ""; endTitle.text = ""; endThanks.text = "";
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime * 0.35f) { img.color = new Color(0.02f, 0.015f, 0.05f, t * 0.55f); yield return null; }
            foreach (var l in lines)
            {
                endText.text = l;
                endText.color = new Color(Cream.r, Cream.g, Cream.b, 0);
                for (float t = 0; t < 1f; t += Time.unscaledDeltaTime * 0.8f) { endText.color = new Color(Cream.r, Cream.g, Cream.b, t); yield return null; }
                float wait = 1.8f + l.Length * 0.04f;
                for (float t = 0; t < wait; t += Time.unscaledDeltaTime) { if (Inp.ConfirmDown) break; yield return null; }
                for (float t = 0; t < 1f; t += Time.unscaledDeltaTime * 1.6f) { endText.color = new Color(Cream.r, Cream.g, Cream.b, 1 - t); yield return null; }
            }
            endText.text = "";
            endTitle.text = "The End";
            endTitle.color = new Color(Gold.r, Gold.g, Gold.b, 0);
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime * 0.6f) { endTitle.color = new Color(Gold.r, Gold.g, Gold.b, t); yield return null; }
            endThanks.text = "Thank you for playing  -  Cat on the Sill";
            endButton.gameObject.SetActive(true);
            Select(endButton);
        }

        public bool EndShowing => endPanel != null && endPanel.activeSelf;

        // =================================================================== per-frame
        void Update()
        {
            float dt = Time.unscaledDeltaTime;

            // prompt
            bool showPrompt = hudPanel.activeSelf && !string.IsNullOrEmpty(pendingPrompt) && !timePanel.activeSelf;
            if (showPrompt) promptText.text = pendingPrompt;
            float pa = Mathf.MoveTowards(promptBg.color.a, showPrompt ? 0.55f : 0f, dt * 5f);
            promptBg.color = new Color(0.1f, 0.07f, 0.12f, pa);
            promptText.color = new Color(Cream.r, Cream.g, Cream.b, pa / 0.55f);
            var prr = promptBg.rectTransform;
            if (showPrompt) prr.sizeDelta = new Vector2(Mathf.Clamp(promptText.preferredWidth + 70, 240, 1500), 54);

            // toast
            if (toastT <= 0f && toastQueue.Count > 0) { toastText.text = toastQueue.Dequeue(); toastT = 3.4f; }
            toastT -= dt;
            float ta = toastT > 0 ? Mathf.Clamp01(Mathf.Min(toastT, 3.4f - toastT) * 3f) : 0f;
            toastBg.color = new Color(0.1f, 0.07f, 0.12f, ta * 0.6f);
            toastText.color = new Color(Gold.r, Gold.g, Gold.b, ta);
            toastBg.rectTransform.sizeDelta = new Vector2(Mathf.Clamp(toastText.preferredWidth + 80, 300, 1400), 56);

            // hint
            hintT -= dt;
            float ha = hintT > 0 ? Mathf.Clamp01(Mathf.Min(hintT, 15f - hintT) * 2f) : 0f;
            hintBg.color = new Color(0.08f, 0.06f, 0.12f, ha * 0.9f);
            hintText.color = new Color(0.95f, 0.98f, 1f, ha);
            hintBg.rectTransform.sizeDelta = new Vector2(620, Mathf.Clamp(hintText.preferredHeight + 50, 120, 260));
            if (hintT <= 0) hintText.text = hintT > -1 ? hintText.text : hintText.text;

            // dialogue
            dialogueT -= dt;
            float da = dialogueT > 0 ? Mathf.Clamp01(dialogueT * 3f) : 0f;
            dialogueBg.color = new Color(0.1f, 0.07f, 0.12f, da * 0.82f);
            dialogueName.color = new Color(dialogueName.color.r, dialogueName.color.g, dialogueName.color.b, da);
            dialogueBody.color = new Color(Cream.r, Cream.g, Cream.b, da);

            // fragment flash
            flashT -= dt;
            float fa = flashT > 0 ? Mathf.Clamp01(Mathf.Min(flashT, 7f - flashT) * 1.2f) : 0f;
            flashBg.color = new Color(0.08f, 0.06f, 0.14f, fa * 0.82f);
            flashTitle.color = new Color(Cream.r, Cream.g, Cream.b, fa);
            flashBody.color = new Color(0.9f, 0.9f, 1f, fa);
            flashCount.color = new Color(Gold.r, Gold.g, Gold.b, fa);

            // orb pulse
            orbPulse = Mathf.Max(0, orbPulse - dt);
            for (int i = 0; i < FragmentInfo.Count; i++)
            {
                if (!Game.Has(i)) continue;
                float s = 1f + 0.12f * Mathf.Sin(Time.unscaledTime * 2f + i) + (orbPulse > 0 && i == lastCollected ? 0.5f * orbPulse : 0f);
                orbs[i].rectTransform.localScale = Vector3.one * s;
            }

            // fade overlay
            var c = fadeImg.color;
            c.a = Mathf.MoveTowards(c.a, fadeTarget, dt * fadeSpeed);
            fadeImg.color = c;
            if (c.a < 0.003f) fadeImg.raycastTarget = false; else fadeImg.raycastTarget = c.a > 0.6f;
        }

        int lastCollected = -1;
        void LateUpdate() { }

        public void NoteCollected(int i) { lastCollected = i; }
    }
}





























