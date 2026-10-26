using System;
using System.Collections.Generic;
using UnityEngine;

namespace Diorama.Audio
{
    /// <summary>Note names and the one scale every tonal sound uses (D major pentatonic: D E F# A B).</summary>
    public static class SynthNotes
    {
        static readonly int[] Pentatonic = { 2, 4, 6, 9, 11 };

        /// <summary>"A4" = 440 Hz, "F#3", "Bb5". Returns false for anything else.</summary>
        public static bool TryParse(string name, out int midi)
        {
            midi = 0;
            if (string.IsNullOrWhiteSpace(name)) return false;
            name = name.Trim();
            int pc;
            switch (char.ToUpperInvariant(name[0]))
            {
                case 'C': pc = 0; break;
                case 'D': pc = 2; break;
                case 'E': pc = 4; break;
                case 'F': pc = 5; break;
                case 'G': pc = 7; break;
                case 'A': pc = 9; break;
                case 'B': pc = 11; break;
                default: return false;
            }
            int i = 1;
            if (i < name.Length && name[i] == '#') { pc++; i++; }
            else if (i < name.Length && name[i] == 'b') { pc--; i++; }
            if (!int.TryParse(name.Substring(i), out int octave)) return false;
            midi = 12 * (octave + 1) + pc;
            return true;
        }

        public static float Hz(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);

        public static float Hz(string name, float fallback) => TryParse(name, out int m) ? Hz(m) : fallback;

        public static bool InScale(int midi) => Array.IndexOf(Pentatonic, ((midi % 12) + 12) % 12) >= 0;

        /// <summary>The n-th note of D major pentatonic counted from D4 (0 = D4, 5 = D5, -1 = B3).</summary>
        public static int Degree(int n)
        {
            int oct = Mathf.FloorToInt(n / 5f);
            int idx = n - oct * 5;
            return 62 + 12 * oct + Pentatonic[idx] - 2;
        }

        public static string Name(int midi)
        {
            string[] names = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
            return names[((midi % 12) + 12) % 12] + (Mathf.FloorToInt(midi / 12f) - 1);
        }
    }

    /// <summary>Renders a SynthPatch to mono float samples at 48 kHz. Pure C#, no Editor API, deterministic per seed and variant.</summary>
    public static class SynthRenderer
    {
        const int Rate = SynthPatch.SampleRate;
        const float FadeSeconds = 0.003f;

        struct Jitter
        {
            public float pitch, time, filter;
            public static Jitter None => new Jitter { pitch = 1f, time = 1f, filter = 1f };
        }

        /// <summary>Variant 0 is the exact recipe; later variants get seeded pitch, time and filter jitter.</summary>
        public static float[] Render(SynthPatch patch, int variant = 0)
        {
            var rng = new System.Random(patch.seed * 7919 + variant * 104729);
            var j = Jitter.None;
            if (variant > 0)
            {
                j.pitch = Mathf.Pow(2f, Range(rng, -1f, 1f) * patch.jitterPitch / 12f);
                j.time = 1f + Range(rng, -1f, 1f) * patch.jitterTime;
                j.filter = 1f + Range(rng, -1f, 1f) * patch.jitterFilter;
            }

            bool loop = patch.mode == SynthMode.Loop;
            float duration = Mathf.Max(0.05f, patch.duration * (loop ? 1f : j.time));
            float xfade = loop ? Mathf.Min(patch.loopCrossfade, duration * 0.5f) : 0f;
            int length = Mathf.CeilToInt(duration * Rate);
            var buf = new float[length + Mathf.CeilToInt(xfade * Rate)];

            if (patch.mode == SynthMode.Sequence)
            {
                foreach (var n in patch.notes)
                {
                    if (n.layer < 0 || n.layer >= patch.layers.Count) continue;
                    var layer = patch.layers[n.layer];
                    if (layer == null || !layer.enabled) continue;
                    float hz = SynthNotes.Hz(n.note, layer.hz);
                    RenderLayer(layer, buf, hz * j.pitch, n.start * j.time, n.length * j.time, n.velocity, j, rng.Next());
                }
            }
            else
            {
                float total = buf.Length / (float)Rate;
                foreach (var layer in patch.layers)
                {
                    if (layer == null || !layer.enabled) continue;
                    float hz = SynthNotes.Hz(layer.note, layer.hz);
                    float offset = layer.startOffset * j.time;
                    float gate = layer.gate > 0f ? layer.gate * j.time : Mathf.Max(0f, total - offset - layer.amp.release * j.time);
                    if (loop) gate = total; // a loop sustains through the whole render, tail included
                    RenderLayer(layer, buf, hz * j.pitch, offset, gate, 1f, j, rng.Next());
                }
            }

            Master(patch, buf);

            var outBuf = new float[length];
            if (loop)
            {
                // Equal-power crossfade of the tail into the head: sample 0 continues straight from the last sample.
                int x = buf.Length - length;
                for (int i = 0; i < length; i++)
                {
                    if (i < x)
                    {
                        float u = i / (float)x;
                        outBuf[i] = buf[i] * Mathf.Sqrt(u) + buf[length + i] * Mathf.Sqrt(1f - u);
                    }
                    else outBuf[i] = buf[i];
                }
            }
            else Array.Copy(buf, outBuf, length);

            if (patch.normalize) Normalize(outBuf, Mathf.Pow(10f, patch.peakDb / 20f));
            if (!loop) Fades(outBuf);
            return outBuf;
        }

        // ---- one layer ----

        static void RenderLayer(SynthLayer L, float[] buf, float hz, float offset, float gate, float velocity, Jitter j, int seed)
        {
            var rng = new System.Random(seed);
            var env = L.amp;
            float tScale = j.time;
            float a = Mathf.Max(0.003f, env.attack * tScale), h = env.hold * tScale, d = Mathf.Max(0.001f, env.decay * tScale);
            float r = Mathf.Max(0.003f, env.release * tScale);
            float gateLevel = EnvLevel(gate, a, h, d, env.sustain);

            int start = Mathf.Max(0, Mathf.RoundToInt(offset * Rate));
            int end = Mathf.Min(buf.Length, start + Mathf.CeilToInt((gate + r) * Rate));
            double phase = rng.NextDouble(), modPhase = 0;
            if (L.wave == SynthWave.Sine || L.fmIndex > 0f) phase = 0; // tonal attacks start at zero crossing
            float pink0 = 0, pink1 = 0, pink2 = 0, pink3 = 0, pink4 = 0, pink5 = 0, pink6 = 0;
            var svf = new Svf();
            var f1 = new Svf();
            var f2 = new Svf();
            float clickLeft = 0, clickAmp = 0, clickDecay = 0;
            float gain = L.gain * velocity;

            for (int i = start; i < end; i++)
            {
                float t = (i - start) / (float)Rate;

                // amplitude
                float amp = t < gate ? EnvLevel(t, a, h, d, env.sustain) : gateLevel * Release((t - gate) / r);
                if (L.tremoloDepth > 0f && L.tremoloRate > 0f)
                    amp *= 1f - L.tremoloDepth * (0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * L.tremoloRate * t));
                if (amp <= 0f && t > gate) break;

                // pitch
                float semis = Shape(L.pitchStart, L.pitchEnd, L.pitchTime * tScale, L.pitchCurve, t);
                if (L.vibratoDepth > 0f && L.vibratoRate > 0f && t > L.vibratoDelay)
                {
                    float ramp = Mathf.Clamp01((t - L.vibratoDelay) / 0.15f);
                    semis += ramp * L.vibratoDepth * Mathf.Sin(2f * Mathf.PI * L.vibratoRate * t);
                }
                float f = hz * Mathf.Pow(2f, semis / 12f);

                float s;
                switch (L.wave)
                {
                    case SynthWave.WhiteNoise:
                        s = (float)(rng.NextDouble() * 2 - 1);
                        break;
                    case SynthWave.PinkNoise:
                    {
                        // Paul Kellet's refined pink filter.
                        float w = (float)(rng.NextDouble() * 2 - 1);
                        pink0 = 0.99886f * pink0 + w * 0.0555179f;
                        pink1 = 0.99332f * pink1 + w * 0.0750759f;
                        pink2 = 0.96900f * pink2 + w * 0.1538520f;
                        pink3 = 0.86650f * pink3 + w * 0.3104856f;
                        pink4 = 0.55000f * pink4 + w * 0.5329522f;
                        pink5 = -0.7616f * pink5 - w * 0.0168980f;
                        s = (pink0 + pink1 + pink2 + pink3 + pink4 + pink5 + pink6 + w * 0.5362f) * 0.11f;
                        pink6 = w * 0.115926f;
                        break;
                    }
                    default:
                    {
                        double ph = phase;
                        if (L.fmIndex > 0f && L.fmRatio > 0f)
                        {
                            float index = L.fmIndex * (L.fmDecay > 0f ? Mathf.Exp(-t / (L.fmDecay * tScale)) : 1f);
                            ph += index * Math.Sin(2 * Math.PI * modPhase) / (2 * Math.PI);
                            modPhase += f * L.fmRatio / Rate;
                            if (modPhase > 1) modPhase -= Math.Floor(modPhase);
                        }
                        s = Osc(L.wave, ph, f);
                        phase += f / Rate;
                        if (phase > 1) phase -= Math.Floor(phase);
                        break;
                    }
                }

                // crackle: short decaying noise clicks at random
                if (L.crackleDensity > 0f)
                {
                    if (clickLeft <= 0f && rng.NextDouble() < L.crackleDensity / Rate)
                    {
                        clickLeft = L.crackleLength * 0.001f * Rate * (0.5f + (float)rng.NextDouble());
                        clickAmp = (0.3f + 0.7f * (float)rng.NextDouble()) * (rng.Next(2) == 0 ? -1f : 1f);
                        clickDecay = Mathf.Exp(-4f / Mathf.Max(1f, clickLeft));
                    }
                    if (clickLeft > 0f)
                    {
                        s += clickAmp * L.crackleGain * (0.6f + 0.4f * (float)(rng.NextDouble() * 2 - 1));
                        clickAmp *= clickDecay;
                        clickLeft--;
                    }
                }

                if (L.filter != SynthFilter.None)
                {
                    float fc = Shape(L.cutoffStart, L.cutoffEnd, L.cutoffTime * tScale, L.cutoffCurve, t) * j.filter;
                    float q = 0.5f + L.resonance * 11.5f;
                    svf.Process(s, fc, q, out float lo, out float bp, out float hi);
                    s = L.filter == SynthFilter.LowPass ? lo : L.filter == SynthFilter.HighPass ? hi : bp;
                }

                if (L.formant)
                {
                    float ft = L.formantTime * tScale;
                    float fa = Shape(L.f1Start, L.f1End, ft, 1f, t), fb = Shape(L.f2Start, L.f2End, ft, 1f, t);
                    f1.Process(s, fa, L.formantQ, out _, out float b1, out _);
                    f2.Process(s, fb, L.formantQ * 1.2f, out _, out float b2, out _);
                    float voiced = (b1 + 0.7f * b2) * 1.6f;
                    s = Mathf.Lerp(s, voiced, L.formantMix);
                }

                buf[i] += s * amp * gain;
            }
        }

        /// <summary>Band-limited oscillators (additive below ~9 kHz, so the top end stays soft).</summary>
        static float Osc(SynthWave wave, double phase, float f)
        {
            double w = 2 * Math.PI * phase;
            switch (wave)
            {
                case SynthWave.Sine:
                    return (float)Math.Sin(w);
                case SynthWave.Triangle:
                {
                    float p = (float)(phase - Math.Floor(phase));
                    return 4f * Mathf.Abs(p - 0.5f) - 1f;
                }
                case SynthWave.SoftSquare:
                    return (float)(Math.Tanh(2.5 * Math.Sin(w)) / Math.Tanh(2.5));
                case SynthWave.SoftSaw:
                {
                    int n = Harmonics(f);
                    double sum = 0, s1 = Math.Sin(w), s0 = 0, c = 2 * Math.Cos(w), sn = s1;
                    for (int k = 1; k <= n; k++)
                    {
                        sum += sn / Math.Pow(k, 1.15);
                        double next = c * sn - s0;
                        s0 = sn;
                        sn = next;
                    }
                    return (float)(sum * 0.55);
                }
                case SynthWave.Buzz:
                {
                    // Equal-ish harmonics that roll off near the limit: a reedy, nasal source for formants.
                    int n = Harmonics(f);
                    double sum = 0, c0 = 1, c1 = Math.Cos(w), c = 2 * Math.Cos(w), cn = c1;
                    for (int k = 1; k <= n; k++)
                    {
                        double weight = 1.0 - 0.7 * k / (n + 1.0);
                        sum += cn * weight;
                        double next = c * cn - c0;
                        c0 = cn;
                        cn = next;
                    }
                    return (float)(sum / Math.Max(1, n * 0.45));
                }
                default:
                    return 0f;
            }
        }

        static int Harmonics(float f) => Mathf.Clamp(Mathf.FloorToInt(9000f / Mathf.Max(20f, f)), 1, 60);

        /// <summary>Simper's trapezoidal state-variable filter: stable under fast cutoff sweeps.</summary>
        struct Svf
        {
            float ic1, ic2;

            public void Process(float x, float fc, float q, out float low, out float band, out float high)
            {
                fc = Mathf.Clamp(fc, 10f, Rate * 0.45f);
                float g = Mathf.Tan(Mathf.PI * fc / Rate), k = 1f / Mathf.Max(0.3f, q);
                float a1 = 1f / (1f + g * (g + k)), a2 = g * a1, a3 = g * a2;
                float v3 = x - ic2;
                float v1 = a1 * ic1 + a2 * v3;
                float v2 = ic2 + a2 * ic1 + a3 * v3;
                ic1 = 2f * v1 - ic1;
                ic2 = 2f * v2 - ic2;
                low = v2;
                band = k * v1; // unity gain at the centre
                high = x - k * v1 - v2;
            }
        }

        // ---- envelopes ----

        static float EnvLevel(float t, float a, float h, float d, float sustain)
        {
            if (t < a)
            {
                float u = t / a;
                return u * u * (3f - 2f * u); // smooth: no corner at the start
            }
            t -= a;
            if (t < h) return 1f;
            t -= h;
            float k = Mathf.Exp(-4.6f * t / d); // ~1% left after the decay time
            return sustain + (1f - sustain) * k;
        }

        static float Release(float u)
        {
            if (u >= 1f) return 0f;
            float k = Mathf.Exp(-4.6f * u);
            return (k - 0.01f) / 0.99f;
        }

        static float Shape(float from, float to, float time, float curve, float t)
        {
            if (time <= 0f) return to;
            float u = Mathf.Clamp01(t / time);
            return from + (to - from) * Mathf.Pow(u, curve);
        }

        static float Range(System.Random rng, float lo, float hi) => lo + (float)rng.NextDouble() * (hi - lo);

        // ---- master ----

        static void Master(SynthPatch patch, float[] buf)
        {
            // DC removal (one-pole high-pass ~20 Hz), gentle top-end low-pass, then drive into the limiter.
            float r = 1f - 2f * Mathf.PI * 20f / Rate, x1 = 0, y1 = 0;
            var lp1 = new Svf();
            var lp2 = new Svf();
            bool lowpass = patch.lowpass < 15999f;
            for (int i = 0; i < buf.Length; i++)
            {
                float x = buf[i];
                float y = x - x1 + r * y1;
                x1 = x;
                y1 = y;
                if (lowpass)
                {
                    lp1.Process(y, patch.lowpass, 0.54f, out y, out _, out _);
                    lp2.Process(y, patch.lowpass, 1.31f, out y, out _, out _); // 4th order Butterworth pair
                }
                y *= patch.gain;
                if (patch.limiter) y = SoftLimit(y);
                buf[i] = y;
            }
        }

        static float SoftLimit(float x)
        {
            const float knee = 0.7f;
            float a = Mathf.Abs(x);
            if (a <= knee) return x;
            float over = (a - knee) / (1f - knee);
            return Mathf.Sign(x) * (knee + (1f - knee) * (float)Math.Tanh(over));
        }

        static void Normalize(float[] buf, float target)
        {
            float peak = Peak(buf);
            if (peak < 1e-6f) return;
            float k = target / peak;
            for (int i = 0; i < buf.Length; i++) buf[i] *= k;
        }

        static void Fades(float[] buf)
        {
            int n = Mathf.Min(buf.Length / 2, Mathf.CeilToInt(FadeSeconds * Rate));
            for (int i = 0; i < n; i++)
            {
                float u = i / (float)n;
                buf[i] *= u;
                buf[buf.Length - 1 - i] *= u;
            }
        }

        public static float Peak(float[] buf)
        {
            float p = 0f;
            foreach (var s in buf) p = Mathf.Max(p, Mathf.Abs(s));
            return p;
        }

        /// <summary>Quick facts for the Lab and the batch report: peak and RMS in dBFS, share of energy above 9 kHz.</summary>
        public static void Analyze(float[] buf, out float peakDb, out float rmsDb, out float highShare)
        {
            double sum = 0, high = 0;
            var hp1 = new Svf();
            var hp2 = new Svf();
            foreach (var s in buf)
            {
                sum += s * s;
                hp1.Process(s, 9000f, 0.54f, out _, out _, out float h);
                hp2.Process(h, 9000f, 1.31f, out _, out _, out h);
                high += h * h;
            }
            float peak = Peak(buf);
            peakDb = peak > 1e-6f ? 20f * Mathf.Log10(peak) : -120f;
            double rms = buf.Length > 0 ? Math.Sqrt(sum / buf.Length) : 0;
            rmsDb = rms > 1e-6 ? 20f * Mathf.Log10((float)rms) : -120f;
            highShare = sum > 1e-12 ? (float)(high / sum) : 0f;
        }

        /// <summary>Largest jump between the last and first sample, relative to the peak (0 = a perfect loop join).</summary>
        public static float LoopSeam(float[] buf)
        {
            if (buf.Length < 2) return 0f;
            float peak = Mathf.Max(1e-6f, Peak(buf));
            return Mathf.Abs(buf[0] - buf[buf.Length - 1]) / peak;
        }
    }
}
