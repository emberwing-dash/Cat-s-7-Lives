using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatOnTheSill
{
    /// <summary>Menu scene: shows the title screen over the diorama and starts the Game scene.</summary>
    public class MenuBoot : MonoBehaviour
    {
        public Light sunLight;
        public TimeManager times;
        public CatModel cat;
        public MainMenuUI menuUi;          // the baked, editable menu canvas (falls back to the code-built one if missing)
        public Transform lookTarget;
        UIManager ui;
        float t;
        bool leaving;

        void Start()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            Inp.Enabled = false;
            times.Init(sunLight);
            times.SetTime(TimeOfDay.Sunset, true);
            var audio = new GameObject("Audio").AddComponent<AudioManager>();
            audio.Init();
            System.Action quit = () =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            };
            if (menuUi != null)
            {
                menuUi.Wire(() => Go(false), quit);
                menuUi.ShowTitle();
                menuUi.SetFade(0f);
                return;
            }
            ui = new GameObject("UI").AddComponent<UIManager>();
            ui.Init();
            ui.OnNewGame = () => Go(false);
            ui.OnContinue = () => Go(true);
            ui.OnQuit = quit;
            ui.ShowTitle();
            ui.SetFade(0f);
        }

        void Go(bool cont)
        {
            if (leaving) return;
            leaving = true;
            Session.Continue = cont;
            if (!cont) { Game.DeleteSave(); Game.Data = new SaveData(); }
            StartCoroutine(Load());
        }

        System.Collections.IEnumerator Load()
        {
            if (menuUi != null) menuUi.FadeTo(1f, 2.5f); else ui.FadeTo(1f, 2.5f);
            yield return new WaitForSecondsRealtime(0.6f);
            UnityEngine.SceneManagement.SceneManager.LoadScene(Session.GameScene);
        }

        void Update()
        {
            t += Time.deltaTime;
            if (cat != null) cat.Tick(Time.deltaTime, 0f, true, 0f, false, false, false, true);
            var cam = Camera.main;
            if (cam != null && lookTarget != null)
            {
                Vector3 basePos = lookTarget.position + new Vector3(0.5f, 0.6f, 2.5f);
                cam.transform.position = basePos + new Vector3(Mathf.Sin(t * 0.15f) * 0.4f, Mathf.Sin(t * 0.2f) * 0.06f, 0);
                cam.transform.rotation = Quaternion.LookRotation(lookTarget.position + new Vector3(1.5f, 0.25f, 0) - cam.transform.position);
            }
        }
    }
}
