using System.Collections.Generic;
using UnityEngine;

namespace CatOnTheSill
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager I;

        Dictionary<string, AudioClip> clips;
        readonly List<AudioSource> pool = new List<AudioSource>();
        AudioSource room, day, night, music;
        public float MasterVolume = 1f;
        public bool Muted;
        float proxCooldown;

        public void Init()
        {
            I = this;
            clips = ProcAudio.BuildAll();
            for (int i = 0; i < 14; i++)
            {
                var s = new GameObject("SFX" + i).AddComponent<AudioSource>();
                s.transform.SetParent(transform, false);
                s.playOnAwake = false; s.spatialBlend = 0.55f; s.rolloffMode = AudioRolloffMode.Linear; s.minDistance = 2f; s.maxDistance = 30f;
                pool.Add(s);
            }
            room = Loop("amb_room", 0.5f); day = Loop("amb_day", 0f); night = Loop("amb_night", 0f); music = Loop("music", 0.22f);
        }

        AudioSource Loop(string clip, float vol)
        {
            var s = new GameObject("Loop_" + clip).AddComponent<AudioSource>();
            s.transform.SetParent(transform, false);
            s.clip = clips[clip]; s.loop = true; s.volume = vol; s.spatialBlend = 0f; s.playOnAwake = false;
            s.Play();
            return s;
        }

        void Update()
        {
            if (clips == null) return;
            AudioListener.volume = Muted ? 0f : MasterVolume;
            float nightK = 0f;
            if (TimeManager.I != null)
            {
                nightK = SunLight.IsMoon ? 1f : 0f;
                var d = Times.Get(TimeManager.I.Target);
                if (TimeManager.I.Transitioning) nightK = 0.5f;
                else nightK = d.moon ? 1f : 0f;
            }
            float dayK = 1f - nightK;
            day.volume = Mathf.MoveTowards(day.volume, 0.35f * dayK, Time.unscaledDeltaTime * 0.4f);
            night.volume = Mathf.MoveTowards(night.volume, 0.45f * nightK, Time.unscaledDeltaTime * 0.4f);
            proxCooldown -= Time.deltaTime;
        }

        public void SetMusic(float v) { if (music != null) music.volume = v; }

        public void Play(string name, Vector3 pos, float vol = 1f, float pitch = 1f)
        {
            if (clips == null || !clips.TryGetValue(name, out var clip)) return;
            var s = Free();
            if (s == null) return;
            s.transform.position = pos; s.spatialBlend = 0.55f;
            s.clip = clip; s.volume = Mathf.Clamp01(vol); s.pitch = pitch;
            s.Play();
        }

        public void Play2D(string name, float vol = 1f, float pitch = 1f)
        {
            if (clips == null || !clips.TryGetValue(name, out var clip)) return;
            var s = Free();
            if (s == null) return;
            s.spatialBlend = 0f; s.clip = clip; s.volume = Mathf.Clamp01(vol); s.pitch = pitch;
            s.Play();
        }

        AudioSource Free()
        {
            foreach (var s in pool) if (!s.isPlaying) return s;
            return pool[0];
        }

        public void PlayFragment(int index) { Play2D("chime" + Mathf.Clamp(index, 0, 6), 0.9f, 1f); }

        /// <summary>A soft blip whose pitch climbs as the player nears a solution.</summary>
        public void Proximity(float closeness)
        {
            if (proxCooldown > 0f) return;
            proxCooldown = 0.35f;
            Play2D("blip", 0.18f + closeness * 0.12f, Mathf.Lerp(0.8f, 1.6f, closeness));
        }
    }
}
