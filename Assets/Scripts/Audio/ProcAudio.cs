using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatOnTheSill
{
    /// <summary>Every sound in the game is synthesised at start-up: no audio assets are required.</summary>
    public static class ProcAudio
    {
        const int SR = 44100;

        static AudioClip Make(string name, float[] data, bool loop = false)
        {
            // soft limiter
            for (int i = 0; i < data.Length; i++) data[i] = (float)Math.Tanh(data[i]);
            var c = AudioClip.Create(name, data.Length, 1, SR, false);
            c.SetData(data, 0);
            return c;
        }

        static float Env(float t, float a, float d)       // attack / exponential decay
        {
            if (t < 0) return 0;
            float att = a <= 0 ? 1 : Mathf.Clamp01(t / a);
            return att * Mathf.Exp(-t / d);
        }

        static float Noise(System.Random r) => (float)(r.NextDouble() * 2.0 - 1.0);

        static float[] Buf(float seconds) => new float[Mathf.CeilToInt(seconds * SR)];

        public static Dictionary<string, AudioClip> BuildAll()
        {
            var d = new Dictionary<string, AudioClip>();
            d["step"] = Step();
            d["jump"] = Jump(); d["land"] = Land();
            d["grab"] = Grab(); d["scrape"] = Scrape(); d["curtain"] = Curtain();
            d["click"] = Click(); d["jingle"] = Jingle(); d["meow"] = Meow();
            d["blip"] = Blip(880f, 0.1f, 0.2f); d["blip2"] = Blip(1320f, 0.14f, 0.3f);
            d["dud"] = Dud(); d["whoosh"] = Whoosh(); d["reveal"] = Reveal(); d["open"] = OpenCabinet();
            d["collect"] = Collect(); d["voice"] = Voice(); d["end"] = EndBell();
            for (int i = 0; i < FragmentInfo.Count; i++) d["chime" + i] = Chime(i);
            d["amb_room"] = RoomTone(); d["amb_day"] = Birds(); d["amb_night"] = Crickets();
            d["music"] = MusicBox();
            return d;
        }

        static AudioClip Step()
        {
            var r = new System.Random(1); var b = Buf(0.11f); float lp = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR; lp += (Noise(r) - lp) * 0.22f;
                b[i] = lp * Env(t, 0.003f, 0.025f) * 1.6f + Mathf.Sin(t * 2 * Mathf.PI * 150f) * Env(t, 0.002f, 0.02f) * 0.25f;
            }
            return Make("step", b);
        }

        static AudioClip Jump()
        {
            var b = Buf(0.18f); float ph = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR; float f = Mathf.Lerp(320f, 640f, t / 0.18f); ph += f / SR;
                b[i] = Mathf.Sin(ph * 2 * Mathf.PI) * Env(t, 0.01f, 0.07f) * 0.25f;
            }
            return Make("jump", b);
        }

        static AudioClip Land()
        {
            var r = new System.Random(2); var b = Buf(0.14f); float lp = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR; lp += (Noise(r) - lp) * 0.12f;
                b[i] = (lp * 1.2f + Mathf.Sin(t * 2 * Mathf.PI * 90f) * 0.5f) * Env(t, 0.002f, 0.04f);
            }
            return Make("land", b);
        }

        static AudioClip Grab()
        {
            var b = Buf(0.1f);
            for (int i = 0; i < b.Length; i++) { float t = i / (float)SR; b[i] = Mathf.Sin(t * 2 * Mathf.PI * 260f) * Env(t, 0.004f, 0.03f) * 0.35f; }
            return Make("grab", b);
        }

        static AudioClip Scrape()
        {
            var r = new System.Random(3); var b = Buf(0.4f); float lp = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR; lp += (Noise(r) - lp) * 0.07f;
                float wob = 0.6f + 0.4f * Mathf.Sin(t * 2 * Mathf.PI * 11f);
                b[i] = lp * wob * Mathf.Sin(Mathf.PI * t / 0.4f) * 1.4f;
            }
            return Make("scrape", b);
        }

        static AudioClip Curtain()
        {
            var r = new System.Random(4); var b = Buf(0.7f); float lp = 0, hp = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR; float n = Noise(r);
                lp += (n - lp) * 0.2f; hp += (lp - hp) * 0.02f;
                b[i] = (lp - hp) * Mathf.Pow(Mathf.Sin(Mathf.PI * t / 0.7f), 2f) * 1.5f;
            }
            return Make("curtain", b);
        }

        static AudioClip Click()
        {
            var b = Buf(0.08f);
            for (int i = 0; i < b.Length; i++) { float t = i / (float)SR; b[i] = (Mathf.Sin(t * 2 * Mathf.PI * 1100f) * 0.4f + (i < 40 ? 0.6f : 0f)) * Env(t, 0.001f, 0.015f); }
            return Make("click", b);
        }

        static AudioClip Jingle()
        {
            var b = Buf(0.9f);
            float[] f = { 1760f, 2349f, 3136f };
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR; float s = 0;
                for (int k = 0; k < 3; k++) { float tt = t - k * 0.11f; s += Mathf.Sin(tt * 2 * Mathf.PI * f[k]) * Env(tt, 0.002f, 0.12f) * 0.25f; }
                b[i] = s;
            }
            return Make("jingle", b);
        }

        static AudioClip Meow()
        {
            var b = Buf(0.62f); float ph = 0, ph2 = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR; float u = t / 0.62f;
                float f = 520f + 330f * Mathf.Sin(Mathf.PI * Mathf.Pow(u, 0.7f)) - 120f * u + Mathf.Sin(t * 2 * Mathf.PI * 6f) * 8f;
                ph += f / SR; ph2 += (f * 2.01f) / SR;
                float env = Mathf.Sin(Mathf.PI * Mathf.Pow(u, 0.6f)); env = Mathf.Pow(env, 1.2f);
                float s = Mathf.Sin(ph * 2 * Mathf.PI) * 0.5f + Mathf.Sin(ph2 * 2 * Mathf.PI) * 0.22f + Mathf.Sin(ph * 6 * Mathf.PI) * 0.12f * (1 - u);
                // nasal "mew" vowel feel
                s *= 0.7f + 0.3f * Mathf.Sin(ph * 4 * Mathf.PI);
                b[i] = s * env * 0.55f;
            }
            return Make("meow", b);
        }

        static AudioClip Blip(float f0, float dur, float vol)
        {
            var b = Buf(dur + 0.05f);
            for (int i = 0; i < b.Length; i++) { float t = i / (float)SR; b[i] = Mathf.Sin(t * 2 * Mathf.PI * f0) * Env(t, 0.004f, dur * 0.35f) * vol; }
            return Make("blip", b);
        }

        static AudioClip Dud()
        {
            var b = Buf(0.9f);
            float[] f = { 392f, 293f };
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR; float s = 0;
                for (int k = 0; k < 2; k++) { float tt = t - k * 0.22f; s += (Mathf.Sin(tt * 2 * Mathf.PI * f[k]) + 0.3f * Mathf.Sin(tt * 4 * Mathf.PI * f[k])) * Env(tt, 0.01f, 0.2f) * 0.25f; }
                b[i] = s;
            }
            return Make("dud", b);
        }

        static AudioClip Whoosh()
        {
            var r = new System.Random(5); var b = Buf(2.8f); float lp = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR; float u = t / 2.8f;
                float k = Mathf.Lerp(0.02f, 0.25f, Mathf.Sin(Mathf.PI * u));
                lp += (Noise(r) - lp) * k;
                float chime = 0;
                for (int n = 0; n < 4; n++) { float tt = t - 0.5f - n * 0.35f; chime += Mathf.Sin(tt * 2 * Mathf.PI * (523f * Mathf.Pow(1.25f, n))) * Env(tt, 0.01f, 0.5f) * 0.1f; }
                b[i] = lp * Mathf.Sin(Mathf.PI * u) * 1.1f + chime + Mathf.Sin(t * 2 * Mathf.PI * (90f + 60f * u)) * Mathf.Sin(Mathf.PI * u) * 0.12f;
            }
            return Make("whoosh", b);
        }

        static AudioClip Reveal()
        {
            var b = Buf(1.6f); float ph = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR; float u = t / 1.6f;
                float f = 400f * Mathf.Pow(3f, u);
                ph += f / SR;
                float trem = 0.6f + 0.4f * Mathf.Sin(t * 2 * Mathf.PI * 14f);
                b[i] = (Mathf.Sin(ph * 2 * Mathf.PI) * 0.3f + Mathf.Sin(ph * 4 * Mathf.PI) * 0.12f) * trem * Mathf.Sin(Mathf.PI * Mathf.Pow(u, 0.7f));
            }
            return Make("reveal", b);
        }

        static AudioClip OpenCabinet()
        {
            var r = new System.Random(6); var b = Buf(0.9f); float lp = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR; lp += (Noise(r) - lp) * 0.04f;
                float creak = Mathf.Sin(t * 2 * Mathf.PI * (180f + 90f * t)) * 0.12f;
                b[i] = (lp * 0.9f + creak) * Mathf.Sin(Mathf.PI * t / 0.9f);
            }
            return Make("open", b);
        }

        static AudioClip Collect()
        {
            var b = Buf(1.4f);
            float[] f = { 784f, 988f, 1175f, 1568f };
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR; float s = 0;
                for (int k = 0; k < 4; k++) { float tt = t - k * 0.07f; s += Mathf.Sin(tt * 2 * Mathf.PI * f[k]) * Env(tt, 0.004f, 0.45f) * 0.2f; }
                b[i] = s;
            }
            return Make("collect", b);
        }

        static AudioClip Voice()
        {
            var b = Buf(0.12f); float ph = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR; ph += 210f / SR;
                b[i] = (Mathf.Sin(ph * 2 * Mathf.PI) + 0.5f * Mathf.Sin(ph * 4 * Mathf.PI) + 0.3f * Mathf.Sin(ph * 6 * Mathf.PI)) * Env(t, 0.01f, 0.04f) * 0.18f;
            }
            return Make("voice", b);
        }

        static AudioClip EndBell()
        {
            var b = Buf(5.5f);
            float[] f = { 261.6f, 329.6f, 392f, 523.3f, 659.3f, 784f, 1046.5f };
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR; float s = 0;
                for (int k = 0; k < 7; k++) { float tt = t - k * 0.38f; s += (Mathf.Sin(tt * 2 * Mathf.PI * f[k]) + 0.25f * Mathf.Sin(tt * 4 * Mathf.PI * f[k])) * Env(tt, 0.005f, 1.6f) * 0.17f; }
                b[i] = s;
            }
            return Make("end", b);
        }

        /// <summary>A unique little arpeggio for each of the seven fragments.</summary>
        static AudioClip Chime(int index)
        {
            int[] roots = { 0, 2, 4, 7, 9, 5, 12 };           // semitones above C5
            int[][] shapes =
            {
                new[] { 0, 4, 7, 12, 16 }, new[] { 0, 3, 7, 10, 15 }, new[] { 0, 4, 7, 11, 14 }, new[] { 0, 2, 7, 9, 14 },
                new[] { 0, 3, 5, 10, 12 }, new[] { 0, 4, 9, 11, 16 }, new[] { 0, 4, 7, 12, 16 }
            };
            var shape = shapes[index % shapes.Length];
            float dur = index == 6 ? 4.2f : 2.6f;
            var b = Buf(dur);
            float step = index == 6 ? 0.34f : 0.17f;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR; float s = 0;
                for (int k = 0; k < shape.Length; k++)
                {
                    float f = 523.25f * Mathf.Pow(2f, (roots[index % roots.Length] + shape[k]) / 12f);
                    float tt = t - k * step;
                    s += (Mathf.Sin(tt * 2 * Mathf.PI * f) + 0.3f * Mathf.Sin(tt * 4 * Mathf.PI * f) + 0.1f * Mathf.Sin(tt * 6.01f * Mathf.PI * f)) * Env(tt, 0.004f, 0.7f) * 0.2f;
                }
                b[i] = s;
            }
            return Make("chime" + index, b);
        }

        static AudioClip RoomTone()
        {
            const float L = 6f; var b = Buf(L);
            // sum of sines with integer cycles per loop -> seamless
            float[] cyc = { 9, 14, 22, 31, 47, 63 };
            float[] amp = { 0.1f, 0.07f, 0.05f, 0.03f, 0.015f, 0.01f };
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR; float s = 0;
                for (int k = 0; k < cyc.Length; k++) s += Mathf.Sin(2 * Mathf.PI * cyc[k] * t / L * (k + 1) * 0.5f + k) * amp[k];
                b[i] = s * (0.8f + 0.2f * Mathf.Sin(2 * Mathf.PI * t / L));
            }
            return Make("amb_room", b);
        }

        static AudioClip Birds()
        {
            const float L = 14f; var b = Buf(L);
            var r = new System.Random(11);
            for (int c = 0; c < 22; c++)
            {
                float start = (float)r.NextDouble() * (L - 1f);
                float f0 = 2400f + (float)r.NextDouble() * 1800f; int notes = 2 + r.Next(3);
                for (int n = 0; n < notes; n++)
                {
                    float t0 = start + n * 0.13f; float dur = 0.09f;
                    int i0 = (int)(t0 * SR); int len = (int)(dur * SR); float ph = 0; float dir = r.Next(2) == 0 ? 1 : -1;
                    for (int i = 0; i < len && i0 + i < b.Length; i++)
                    {
                        float u = i / (float)len; float f = f0 + dir * 900f * u;
                        ph += f / SR;
                        b[i0 + i] += Mathf.Sin(ph * 2 * Mathf.PI) * Mathf.Sin(Mathf.PI * u) * 0.09f;
                    }
                }
            }
            return Make("amb_day", b);
        }

        static AudioClip Crickets()
        {
            const float L = 8f; var b = Buf(L);
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR;
                float chirp = Mathf.Max(0f, Mathf.Sin(2 * Mathf.PI * 3f * t / 1f * 1f)) ;           // 3 Hz groups
                float burst = (Mathf.Sin(2 * Mathf.PI * 38f * t) > 0.2f ? 1f : 0f);
                float g = Mathf.Sin(2 * Mathf.PI * 3f * t) > 0.45f ? 1f : 0f;
                float tone = Mathf.Sin(2 * Mathf.PI * 4300f * t) * 0.5f + Mathf.Sin(2 * Mathf.PI * 4350f * t) * 0.3f;
                float g2 = Mathf.Sin(2 * Mathf.PI * 2f * t + 1.3f) > 0.5f ? 1f : 0f;
                b[i] = (tone * burst * g * 0.05f + tone * (Mathf.Sin(2 * Mathf.PI * 42f * t) > 0.1f ? 1f : 0f) * g2 * 0.03f) * chirp * 0 + tone * burst * g * 0.04f + tone * (Mathf.Sin(2 * Mathf.PI * 41f * t) > 0.1f ? 1f : 0f) * g2 * 0.025f;
            }
            return Make("amb_night", b);
        }

        static AudioClip MusicBox()
        {
            const float bpm = 84f; float beat = 60f / bpm;
            int bars = 8; float L = bars * 4 * beat;
            var b = Buf(L + 3f);
            // gentle pentatonic lullaby (C major pentatonic), two phrases
            int[] melody = { 0, 2, 4, 7, 4, 2, 4, -1, 9, 7, 4, 2, 0, -1, -1, -1, 2, 4, 7, 9, 7, 4, 2, 4, 0, 2, 4, 2, 0, -1, -1, -1 };
            int[] bass = { -12, -5, -8, -5 };
            float[] scale = { 0, 2, 4, 7, 9 };
            for (int n = 0; n < melody.Length; n++)
            {
                if (melody[n] < 0) continue;
                float t0 = n * beat * 2f;
                int oct = melody[n] / 5; int deg = melody[n] % 5;
                float semis = scale[deg] + 12 * oct + 12;
                AddBell(b, t0, 261.63f * Mathf.Pow(2, semis / 12f), 0.16f);
            }
            for (int n = 0; n < 16; n++) AddBell(b, n * beat * 4f / 2f, 261.63f * Mathf.Pow(2, bass[n % 4] / 12f) * 1f, 0.12f);
            // fade ends so the loop is seamless
            int fade = (int)(0.5f * SR);
            int loopLen = (int)(L * SR);
            // fold tail into the head
            for (int i = 0; i < b.Length - loopLen && i < b.Length; i++) { if (loopLen + i < b.Length) b[i] += b[loopLen + i]; }
            var outb = new float[loopLen];
            Array.Copy(b, outb, loopLen);
            return Make("music", outb);
        }

        static void AddBell(float[] b, float t0, float f, float vol)
        {
            int i0 = (int)(t0 * SR);
            int len = (int)(2.4f * SR);
            for (int i = 0; i < len && i0 + i < b.Length; i++)
            {
                float t = i / (float)SR;
                float s = Mathf.Sin(2 * Mathf.PI * f * t) + 0.35f * Mathf.Sin(2 * Mathf.PI * f * 2.76f * t) * Mathf.Exp(-t * 6f) + 0.15f * Mathf.Sin(2 * Mathf.PI * f * 5.4f * t) * Mathf.Exp(-t * 12f);
                b[i0 + i] += s * Env(t, 0.003f, 0.55f) * vol;
            }
        }
    }
}
