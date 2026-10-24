using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace CatOnTheSill
{
    /// <summary>Lives in the Game scene. The house, cat and people are already in the scene; this runs the flow around them.</summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I;
        public GameMode Mode { get; private set; } = GameMode.Boot;

        [Header("Scene references (filled by the scene baker)")]
        public WorldRefs World;
        public CameraRig Rig;
        public CatController Cat;
        public TimeManager Times_;
        public Light sunLight;
        public HintSystem Hints;

        [Header("Editable scene UI (filled by the scene baker)")]
        public UIManager sceneUI;
        public UIManager UI { get; private set; }
        Volume volume;
        float autosave;
        bool endingStarted;

        void Awake()
        {
            I = this;
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 1;
            Physics.IgnoreLayerCollision(Layer.Cat, Layer.Curtain, true);
            Physics.IgnoreLayerCollision(Layer.Cat, Layer.Sensor, true);
            Game.Locked = true;
            // old scenes may still contain baked people: the house belongs to the cat alone
            foreach (var go in FindObjectsByType<Transform>(FindObjectsSortMode.None)) if (go != null && go.name.StartsWith("NPC_")) Destroy(go.gameObject);
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp != null) { urp.shadowDistance = 45f; urp.shadowCascadeCount = 3; }
        }

        void Start()
        {
            Times_.Init(sunLight);
            Rig.Init();
            SetupPostProcessing();

            var audio = new GameObject("Audio").AddComponent<AudioManager>();
            audio.MasterVolume = 1f;
            audio.Muted = false;
            audio.Init();
            audio.SetMusic(0.22f);
            UI = sceneUI;
            if (UI == null)
            {
                UI = new GameObject("UI").AddComponent<UIManager>();
                UI.transform.SetParent(transform, false);
            }
            UI.transform.SetParent(transform, false);
            UI.Init();
            UI.OnResume = () => SetPaused(false);
            UI.OnRestart = () => StartCoroutine(RestartRoutine());
            UI.OnReturnTitle = () => StartCoroutine(ReturnToTitleRoutine());
            UI.OnQuit = Quit;

            Cat.Init(Rig);
            Game.AllFragmentsCollected += OnAllCollected;
            Times_.TimeChanging += OnTimeChanging;
            Times_.TimeMidpoint += OnTimeMidpoint;
            Times_.TimeSettled += OnTimeSettled;

            Times_.SetTime(TimeOfDay.Morning, true);
            Cat.Frozen = true;
            Rig.target = Cat.transform;
            Physics.SyncTransforms();

            bool cont = Session.Continue && Game.LoadFromDisk();
            Session.Continue = false;
            if (!cont) { Game.DeleteSave(); Game.Data = new SaveData(); }
            UI.SetFade(1f);
            StartCoroutine(cont ? ContinueRoutine() : NewGameRoutine());
        }

        void OnDestroy()
        {
            Game.AllFragmentsCollected -= OnAllCollected;
            if (I == this) I = null;
        }

        void SetupPostProcessing()
        {
            var vg = new GameObject("PostFX");
            vg.transform.SetParent(transform, false);
            volume = vg.AddComponent<Volume>();
            volume.isGlobal = true; volume.priority = 10;
            var prof = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = prof.Add<Bloom>(true); bloom.intensity.Override(0.55f); bloom.threshold.Override(1.05f); bloom.scatter.Override(0.7f);
            var tone = prof.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.ACES);
            var vig = prof.Add<Vignette>(true); vig.intensity.Override(0.28f); vig.smoothness.Override(0.5f);
            var ca = prof.Add<ColorAdjustments>(true); ca.postExposure.Override(0.1f); ca.saturation.Override(10f); ca.contrast.Override(8f);
            var wb = prof.Add<WhiteBalance>(true); wb.temperature.Override(4f);
            volume.sharedProfile = prof;
        }

        // ================================================================================== flow
        void BeginPlay(bool restore)
        {
            endingStarted = false;
            Time.timeScale = 1f;
            if (restore)
            {
                Game.RestoreAll();
                Times_.SetTime((TimeOfDay)Mathf.Clamp(Game.Data.time, 0, 5), true);
                if (Game.Data.catPos != null && Game.Data.catPos.Length == 3)
                    Cat.Teleport(new Vector3(Game.Data.catPos[0], Game.Data.catPos[1] + 0.05f, Game.Data.catPos[2]), Game.Data.catYaw);
                else Cat.Teleport(World.catStart, 20f);
            }
            else Cat.Teleport(World.catStart, 20f);
            Rig.target = Cat.transform;
            Rig.mode = CamMode.Follow;
            Rig.SnapBehind(0f);
            Rig.yaw = restore ? Game.Data.catYaw : 20f;
            Cat.Frozen = false;
            Inp.TestMove = null; Inp.TestJump = Inp.TestInteract = Inp.TestAlt = Inp.TestGrab = Inp.TestRun = Inp.TestMeow = false;
            Mode = GameMode.Playing;
            Game.Locked = false;
            UI.HideAll();
            UI.ShowHud(true);
            UI.RefreshHud();
            Inp.Enabled = true;
            LockCursor(true);
            Game.Save();
        }

        IEnumerator NewGameRoutine()
        {
            Mode = GameMode.Intro;
            Inp.Enabled = false;
            string[] pages =
            {
                "A falling star scattered Miso's soul into seven fragments hidden throughout the house.",
                "Follow light and shadow to reveal them. Walk into each glowing orb to collect it."
            };
            UI.FadeTo(0f, 3f);
            yield return UI.ShowIntroSequence(pages, null);
            BeginPlay(false);
            TutorialGuide.Begin(this, false);
        }

        IEnumerator ContinueRoutine()
        {
            yield return null;
            UI.FadeTo(0f, 2.5f);
            BeginPlay(true);
            TutorialGuide.Begin(this, true);
            UI.ShowToast("Welcome back, Miso.");
        }

        IEnumerator RestartRoutine()
        {
            Time.timeScale = 1f;
            UI.ShowPause(false);
            UI.FadeTo(1f, 3f);
            yield return new WaitForSecondsRealtime(0.5f);
            Game.DeleteSave(); Game.Data = new SaveData(); Session.Continue = false;
            SceneManager.LoadScene(Session.GameScene);
        }

        IEnumerator ReturnToTitleRoutine()
        {
            if (Mode != GameMode.Ending && Mode != GameMode.Credits) Game.Save();
            Time.timeScale = 1f;
            UI.FadeTo(1f, 3f);
            yield return new WaitForSecondsRealtime(0.5f);
            SceneManager.LoadScene(Session.MenuScene);
        }

        void Quit()
        {
            Game.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ================================================================================== modes
        void LockCursor(bool on)
        {
            Cursor.lockState = on ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !on;
        }

        public void SetPaused(bool p)
        {
            if (p)
            {
                if (Mode != GameMode.Playing) return;
                Mode = GameMode.Paused;
                Time.timeScale = 0f;
                Inp.Enabled = false;
                UI.ShowPause(true);
                LockCursor(false);
                Game.Save();
            }
            else
            {
                if (Mode != GameMode.Paused) return;
                UI.ShowControls(false);
                UI.ShowPause(false);
                Time.timeScale = 1f;
                Mode = GameMode.Playing;
                Inp.Enabled = true;
                LockCursor(true);
            }
        }

        public void SetUiMode(bool open)
        {
            if (open) { if (Mode == GameMode.Playing) { Mode = GameMode.Time; Inp.Enabled = false; LockCursor(false); } }
            else if (Mode == GameMode.Time) { Mode = GameMode.Playing; Inp.Enabled = true; LockCursor(true); }
        }

        public void ChangeTime(TimeOfDay t)
        {
            if (Times_ == null) return;
            AudioManager.I?.Play2D("whoosh", 0.8f);
            Times_.SetTime(t, false);
            UI.ShowToast(Times.Get(t).name + " - " + Times.Get(t).blurb);
        }

        void OnTimeChanging(TimeOfDay t) { if (Mode == GameMode.Boot) return; UI.FadeTo(0.55f, 1.2f); UI.RefreshTime(); }
        void OnTimeMidpoint(TimeOfDay t) { UI.FadeTo(0f, 1.1f); }
        void OnTimeSettled(TimeOfDay t) { UI.RefreshTime(); if (Mode == GameMode.Playing || Mode == GameMode.Time) Game.Save(); UI.FadeTo(0f, 1.5f); }

        void Update()
        {
            if (Mode == GameMode.Playing || Mode == GameMode.Time)
            {
                Game.Data.playSeconds += Time.deltaTime;
                autosave += Time.deltaTime;
                if (autosave > 25f) { autosave = 0; Game.Save(); }
            }
            if (Inp.MapDown)
            {
                if (UI.MapOpen) UI.HideMap();
                else if (Mode == GameMode.Playing) UI.ShowMap();
            }
            if (Inp.PauseDown)
            {
                if (UI.MapOpen) UI.HideMap();
                else if (UI.SolutionOpen) UI.HideSolutionPicture();
                else if (UI.ControlsOpen) UI.ShowControls(false);
                else if (Mode == GameMode.Playing) SetPaused(true);
                else if (Mode == GameMode.Paused) SetPaused(false);
                else if (Mode == GameMode.Time) UI.CloseTimePanel();
            }
            if (Mode == GameMode.Time && !UI.MapOpen && UI.TimePanelOpen)
            {
                int k = Inp.NumberKeyDown();
                if (k >= 0) { UI.CloseTimePanel(); ChangeTime((TimeOfDay)k); }
            }
            // self-heal: never leave the player without controls when no menu is actually open
            if (!UI.AnyModalOpen)
            {
                if (Mode == GameMode.Time) { Mode = GameMode.Playing; LockCursor(true); }
                if (Mode == GameMode.Playing && (!Inp.Enabled || Time.timeScale <= 0f)) { Inp.Enabled = true; Time.timeScale = 1f; }
            }
            if (Mode == GameMode.Playing && Inp.HintDown) Hints.ShowHintNow();
            if (Mode == GameMode.Playing && Cursor.lockState != CursorLockMode.Locked) LockCursor(true);
        }

        // ================================================================================== ending
        void OnAllCollected()
        {
            if (endingStarted) return;
            endingStarted = true;
            StartCoroutine(EndingRoutine());
        }

        IEnumerator EndingRoutine()
        {
            yield return new WaitForSeconds(2.2f);
            Mode = GameMode.Ending;
            Game.Data.ending = true; Game.Save();
            Cat.Frozen = true; Cat.Release();
            Inp.Enabled = false;
            UI.ShowHud(true);
            UI.SetPrompt("");
            Times_.SetTime(TimeOfDay.Sunrise, false);

            Vector3 cp = Cat.transform.position;
            Vector3 focus = cp + Vector3.up * 0.4f;
            Vector3 bestPos = cp + Vector3.up * 1.0f + Vector3.back; float bestScore = -1f;
            for (int i = 0; i < 16; i++)
            {
                float ang = i * 22.5f * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Sin(ang), 0.28f, Mathf.Cos(ang)).normalized;
                float dist = 2.6f;
                if (Physics.SphereCast(focus, 0.22f, dir, out var hit, dist, Layer.CameraMask, QueryTriggerInteraction.Ignore)) dist = Mathf.Max(0.5f, hit.distance - 0.1f);
                float score = dist + Vector3.Dot(new Vector3(dir.x, 0, dir.z).normalized, Cat.transform.forward) * 0.35f;
                if (score > bestScore) { bestScore = score; bestPos = focus + dir * dist; }
            }
            Rig.fixedLook = cp + Vector3.up * 0.4f;
            Rig.fixedPos = bestPos;
            Rig.mode = CamMode.Fixed;

            var orbs = new List<Transform>();
            for (int i = 0; i < FragmentInfo.Count; i++)
            {
                var o = B.Quad(null, "EndOrb" + i, cp, Vector3.back, Vector2.one * 0.5f, Mats.Blend(FragmentInfo.Colors[i] * 1.3f, Mats.Soft, Mats.BlendKind.Additive));
                var lg = new GameObject("L"); lg.transform.SetParent(o.transform, false);
                var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.color = FragmentInfo.Colors[i]; l.range = 3f; l.intensity = 1.5f;
                orbs.Add(o.transform);
            }
            AudioManager.I?.Play2D("end", 0.9f);
            float t = 0;
            while (t < 7f)
            {
                t += Time.deltaTime;
                float conv = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(4.5f, 7f, t));
                float radius = Mathf.Lerp(0.2f, 0.85f, Mathf.Clamp01(t / 1.5f)) * (1f - conv);
                for (int i = 0; i < orbs.Count; i++)
                {
                    float a = t * 1.2f + i * Mathf.PI * 2f / orbs.Count;
                    orbs[i].position = Cat.Center + new Vector3(Mathf.Cos(a) * radius, 0.25f + Mathf.Sin(t * 1.5f + i) * 0.12f + (1f - conv) * 0.2f, Mathf.Sin(a) * radius);
                    if (Camera.main != null) orbs[i].rotation = Camera.main.transform.rotation;
                    orbs[i].localScale = Vector3.one * Mathf.Lerp(0.5f, 0.15f, conv);
                }
                yield return null;
            }
            FragmentPickup.Burst(Cat.Center + Vector3.up * 0.2f);
            foreach (var o in orbs) Destroy(o.gameObject);
            string[] lines =
            {
                "One by one, the seven lights found their way back to her.",
                "The house breathed out. Every window held the same gold sunrise.",
                "Miso purred, and the whole house remembered her -\nthe warm sill, the long shadow, the little silver moon.",
                "She never left the sill again...\nexcept to chase the sun from room to room."
            };
            yield return UI.ShowEndSequence(lines);
            Mode = GameMode.Credits;
            LockCursor(false);
        }
    }
}





