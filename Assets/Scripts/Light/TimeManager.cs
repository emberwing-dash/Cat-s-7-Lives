using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CatOnTheSill
{
    /// <summary>Owns the day/night cycle: the directional light, ambient, sky, moon, stars and indoor lamps.</summary>
    public class TimeManager : MonoBehaviour
    {
        public static TimeManager I;
        public const float TransitionSeconds = 2.8f;

        public TimeOfDay Target { get; private set; } = TimeOfDay.Morning;
        public bool Transitioning { get; private set; }
        public bool Settled => !Transitioning;
        public float LampLevel { get; private set; }

        public event Action<TimeOfDay> TimeChanging;
        public event Action<TimeOfDay> TimeMidpoint;
        public event Action<TimeOfDay> TimeSettled;

        Light sun;
        Material skyMat;
        TimeOfDay from;
        float u = 1f;
        bool midFired;
        float azFrom, azTo, elFrom, elTo;
        TimeData dFrom, dTo;
        readonly List<LampLight> lamps = new List<LampLight>();
        SkyDome dome;

        public static TimeOfDay Current => I != null ? I.Target : TimeOfDay.Morning;

        public void Init(Light sunLight)
        {
            I = this;
            sun = sunLight;
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 1f;
            sun.shadowBias = 0.02f;
            sun.shadowNormalBias = 0.3f;
            sun.shadowNearPlane = 0.1f;
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.reflectionIntensity = 0.35f;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            var sh = Shader.Find("Skybox/Procedural");
            if (sh != null)
            {
                skyMat = new Material(sh) { name = "SkyProc" };
                RenderSettings.skybox = skyMat;
            }
            foreach (var l in LampLight.All) RegisterLamp(l);
            dome = new GameObject("SkyDome").AddComponent<SkyDome>();
            dome.transform.SetParent(transform, false);
            dome.Build();
            SetTime(TimeOfDay.Morning, true);
        }

        void Awake() { if (I == null) I = this; }

        public void RegisterLamp(LampLight l) { if (!lamps.Contains(l)) { lamps.Add(l); l.Apply(LampLevel); } }
        public void UnregisterLamp(LampLight l) { lamps.Remove(l); }

        public void Advance() { SetTime((TimeOfDay)(((int)Target + 1) % 6), false); }

        public void SetTime(TimeOfDay t, bool instant)
        {
            if (!instant && t == Target && !Transitioning) return;
            from = Target;
            dFrom = CurrentBlend();
            Target = t;
            dTo = Times.Get(t);
            azFrom = instant ? dTo.az : CurrentAz; elFrom = instant ? dTo.el : CurrentEl;
            azTo = dTo.az; elTo = dTo.el;
            if (instant)
            {
                dFrom = dTo; u = 1f; Transitioning = false; midFired = true;
                Apply(1f);
                TimeChanging?.Invoke(t);
                TimeSettled?.Invoke(t);
                return;
            }
            u = 0f; midFired = false; Transitioning = true;
            TimeChanging?.Invoke(t);
        }

        float CurrentAz, CurrentEl;

        TimeData CurrentBlend() { return Lerp(dFrom, dTo, Mathf.SmoothStep(0, 1, u)); }

        static TimeData Lerp(TimeData a, TimeData b, float k)
        {
            var r = b;
            r.sunColor = Color.Lerp(a.sunColor, b.sunColor, k);
            r.sunIntensity = Mathf.Lerp(a.sunIntensity, b.sunIntensity, k);
            r.ambient = Color.Lerp(a.ambient, b.ambient, k);
            r.skyTint = Color.Lerp(a.skyTint, b.skyTint, k);
            r.ground = Color.Lerp(a.ground, b.ground, k);
            r.exposure = Mathf.Lerp(a.exposure, b.exposure, k);
            r.lampLevel = Mathf.Lerp(a.lampLevel, b.lampLevel, k);
            return r;
        }

        void Update()
        {
            if (!Transitioning) return;
            u += Time.deltaTime / TransitionSeconds;
            if (!midFired && u >= 0.5f) { midFired = true; TimeMidpoint?.Invoke(Target); }
            if (u >= 1f)
            {
                u = 1f; Transitioning = false;
                Apply(1f);
                TimeSettled?.Invoke(Target);
                return;
            }
            Apply(u);
        }

        void Apply(float uu)
        {
            float k = Mathf.SmoothStep(0, 1, uu);
            var d = Lerp(dFrom, dTo, k);
            float az = azFrom + Mathf.DeltaAngle(azFrom, azTo) * k;
            float el = Mathf.Lerp(elFrom, elTo, k);
            CurrentAz = az; CurrentEl = el;
            Vector3 toSun = Times.ToSun(az, el);

            float dip = 1f - 0.65f * Mathf.Sin(Mathf.PI * uu) * (uu < 1f ? 1f : 0f);
            bool moon = dTo.moon && k > 0.5f || (!dTo.moon && dFrom.moon && k < 0.5f);
            if (!Transitioning && uu >= 1f) moon = dTo.moon;
            float intensity = d.sunIntensity * dip;

            sun.transform.rotation = Quaternion.LookRotation(-toSun, Vector3.up);
            sun.color = d.sunColor;
            sun.intensity = intensity;
            RenderSettings.ambientLight = d.ambient * Mathf.Lerp(1f, 0.8f, Mathf.Sin(Mathf.PI * uu));
            SunLight.Set(toSun, moon, Mathf.Clamp01(intensity / 2f));

            if (skyMat != null)
            {
                skyMat.SetColor("_SkyTint", d.skyTint);
                skyMat.SetColor("_GroundColor", d.ground);
                skyMat.SetFloat("_Exposure", d.exposure * dip * 0.9f + 0.05f);
                skyMat.SetFloat("_AtmosphereThickness", moon ? 0.7f : 1.0f);
                skyMat.SetFloat("_SunSize", moon ? 0f : 0.045f);
                if (moon) { skyMat.EnableKeyword("_SUNDISK_NONE"); skyMat.DisableKeyword("_SUNDISK_SIMPLE"); skyMat.DisableKeyword("_SUNDISK_HIGH_QUALITY"); }
                else { skyMat.DisableKeyword("_SUNDISK_NONE"); skyMat.EnableKeyword("_SUNDISK_SIMPLE"); }
            }
            float night = moon ? Mathf.Clamp01(Mathf.Abs(k - 0.5f) * 2f) * 0.5f + 0.5f : 0f;
            if (dTo.moon && dFrom.moon) night = 1f;
            else if (!dTo.moon && !dFrom.moon) night = 0f;
            dome.Apply(toSun, moon, night, d.sunColor);

            LampLevel = d.lampLevel;
            foreach (var l in lamps) if (l != null) l.Apply(LampLevel);
            if (uu >= 1f || Time.frameCount % 24 == 0) DynamicGI.UpdateEnvironment();
        }
    }
}
