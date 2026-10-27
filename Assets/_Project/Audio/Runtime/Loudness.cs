using System;
using UnityEngine;

namespace Diorama.Audio
{
    /// <summary>
    /// Loudness tiers: every baked or imported clip is levelled so its mid band (250 Hz - 6 kHz, what small speakers and
    /// ears hear) sits at its tier, with peaks held under a -1 dBFS ceiling. The loudness hierarchy lives here, not in cue volumes.
    /// </summary>
    public enum LoudnessTier { UI, Steps, Island, Moves, Big, Bed, Sweetener, Hum, Voice, Music }

    /// <summary>How a patch is levelled at bake time. Peak = batch 1 behaviour (peak to peakDb), kept for the cries until step C.</summary>
    public enum LevelMode { Peak, Tier }

    /// <summary>Facts about one clip, for AudioScope and the Lab.</summary>
    public struct ClipStats
    {
        public float seconds, peakDb, midDb, lowShare, tailShare, silenceShare;
    }

    /// <summary>Mid-band level, tier levelling with a look-ahead peak limiter, silence trim, and the AudioScope stats. Pure C#.</summary>
    public static class Loudness
    {
        public const float CeilingDb = -1f;
        /// <summary>Frames this far under the loudest frame count as silence (and are trimmed from the ends).</summary>
        public const float SilenceDb = 40f;
        /// <summary>The main body ends when the level first falls this far under the loudest frame.</summary>
        public const float BodyDropDb = 12f;
        const float FrameSeconds = 0.01f;

        /// <summary>Starting tiers from the step B brief (tune by ear).</summary>
        public static float TierDb(LoudnessTier tier)
        {
            switch (tier)
            {
                case LoudnessTier.UI: return -20f;
                case LoudnessTier.Steps: return -22f;
                case LoudnessTier.Island: return -18f;
                case LoudnessTier.Moves: return -16f;
                case LoudnessTier.Big: return -13f;
                case LoudnessTier.Bed: return -28f;
                case LoudnessTier.Sweetener: return -24f;
                case LoudnessTier.Hum: return -24f;
                case LoudnessTier.Voice: return -19f;
                default: return -16f;
            }
        }

        /// <summary>Mid-band (250 Hz - 6 kHz) RMS in dB over the frames that aren't silence.</summary>
        public static float MidLevelDb(float[] buf, int rate)
        {
            if (buf == null || buf.Length == 0) return -120f;
            var mid = BandPass(buf, rate);
            var active = ActiveMask(FrameEnergies(buf, rate), out _);
            int n = Frame(rate);
            double sum = 0;
            long count = 0;
            for (int f = 0; f < active.Length; f++)
            {
                if (!active[f]) continue;
                for (int i = f * n; i < Mathf.Min(buf.Length, (f + 1) * n); i++)
                {
                    sum += mid[i] * mid[i];
                    count++;
                }
            }
            return count > 0 && sum > 1e-14 ? 10f * Mathf.Log10((float)(sum / count)) : -120f;
        }

        /// <summary>Scales the clip so its mid level hits the target, then holds peaks under the ceiling.</summary>
        /// <param name="passes">Limiting a peaky clip lowers its mid level a little; each pass makes that up (imports need more).</param>
        public static void LevelTo(float[] buf, int rate, float targetDb, float ceilingDb = CeilingDb, int passes = 2)
        {
            for (int pass = 0; pass < passes; pass++)
            {
                float mid = MidLevelDb(buf, rate);
                if (mid < -110f) return;
                float g = Mathf.Pow(10f, (targetDb - mid) / 20f);
                for (int i = 0; i < buf.Length; i++) buf[i] *= g;
                Limit(buf, rate, Mathf.Pow(10f, ceilingDb / 20f));
            }
        }

        /// <summary>
        /// Look-ahead peak limiter: the gain falls 5 ms before a peak and recovers over ~80 ms, so transients are turned down
        /// instead of clipped. Never exceeds the ceiling.
        /// </summary>
        public static void Limit(float[] buf, int rate, float ceiling)
        {
            if (SynthRenderer.Peak(buf) <= ceiling) return;
            int n = buf.Length, look = Mathf.Max(1, Mathf.RoundToInt(0.005f * rate));
            var req = new float[n];
            for (int i = 0; i < n; i++)
            {
                float a = Mathf.Abs(buf[i]);
                req[i] = a > ceiling ? ceiling / a : 1f;
            }
            // Sliding minimum over [i, i + look] (monotonic deque).
            var m = new float[n];
            var dq = new int[n + look + 1];
            int head = 0, tail = 0, next = 0;
            for (int i = 0; i < n; i++)
            {
                int upto = Mathf.Min(n - 1, i + look);
                while (next <= upto)
                {
                    while (tail > head && req[dq[tail - 1]] >= req[next]) tail--;
                    dq[tail++] = next++;
                }
                while (dq[head] < i) head++;
                m[i] = req[dq[head]];
            }
            // Release, then a trailing box average of `look` samples: still under every requirement, but no gain steps.
            float rel = 1f / (0.08f * rate);
            for (int i = 1; i < n; i++) m[i] = Mathf.Min(m[i], m[i - 1] + rel);
            double acc = 0;
            var g = new float[n];
            for (int i = 0; i < n; i++)
            {
                acc += m[i];
                if (i >= look) acc -= m[i - look];
                g[i] = (float)(acc / Mathf.Min(i + 1, look));
            }
            for (int i = 0; i < n; i++) buf[i] = Mathf.Clamp(buf[i] * g[i], -ceiling, ceiling);
        }

        /// <summary>Cuts leading and trailing frames more than SilenceDb under the loudest frame; 1 ms / 5 ms fades.</summary>
        public static float[] TrimSilence(float[] buf, int rate)
        {
            var e = FrameEnergies(buf, rate);
            var active = ActiveMask(e, out _);
            int n = Frame(rate), first = Array.IndexOf(active, true), last = Array.LastIndexOf(active, true);
            if (first < 0) return buf;
            int a = Mathf.Max(0, first * n - Mathf.RoundToInt(0.002f * rate));
            int b = Mathf.Min(buf.Length, (last + 1) * n + Mathf.RoundToInt(0.005f * rate));
            if (a == 0 && b == buf.Length) return buf;
            var outBuf = new float[b - a];
            Array.Copy(buf, a, outBuf, 0, outBuf.Length);
            int fi = Mathf.Min(outBuf.Length / 2, Mathf.RoundToInt(0.001f * rate));
            int fo = Mathf.Min(outBuf.Length / 2, Mathf.RoundToInt(0.005f * rate));
            if (a > 0)
                for (int i = 0; i < fi; i++) outBuf[i] *= i / (float)fi;
            for (int i = 0; i < fo; i++) outBuf[outBuf.Length - 1 - i] *= i / (float)fo;
            return outBuf;
        }

        /// <summary>Duration, peak, mid level, low share (&lt; 250 Hz), tail share (after the body), silence share.</summary>
        public static ClipStats Analyse(float[] buf, int rate)
        {
            var s = new ClipStats { seconds = buf.Length / (float)rate, peakDb = -120f, midDb = -120f };
            if (buf.Length == 0) return s;
            float peak = SynthRenderer.Peak(buf);
            s.peakDb = peak > 1e-6f ? 20f * Mathf.Log10(peak) : -120f;
            s.midDb = MidLevelDb(buf, rate);

            var lp1 = new Svf();
            var lp2 = new Svf();
            double total = 0, low = 0;
            foreach (var x in buf)
            {
                total += x * x;
                lp1.Process(x, 250f, 0.54f, rate, out float y, out _, out _);
                lp2.Process(y, 250f, 1.31f, rate, out y, out _, out _);
                low += y * y;
            }
            s.lowShare = total > 1e-14 ? (float)(low / total) : 0f;

            var e = FrameEnergies(buf, rate);
            var active = ActiveMask(e, out int loudest);
            int silent = 0;
            foreach (var a in active)
                if (!a) silent++;
            s.silenceShare = silent / (float)e.Length;

            double eTotal = 0, eTail = 0;
            float bodyFloor = e[loudest] * Mathf.Pow(10f, -BodyDropDb / 10f);
            int bodyEnd = e.Length;
            for (int f = loudest; f < e.Length; f++)
                if (e[f] < bodyFloor)
                {
                    bodyEnd = f;
                    break;
                }
            for (int f = 0; f < e.Length; f++)
            {
                eTotal += e[f];
                if (f >= bodyEnd) eTail += e[f];
            }
            s.tailShare = eTotal > 1e-14 ? (float)(eTail / eTotal) : 0f;
            return s;
        }

        // ---- helpers ----

        static int Frame(int rate) => Mathf.Max(1, Mathf.RoundToInt(FrameSeconds * rate));

        /// <summary>Mean square per 10 ms frame.</summary>
        public static float[] FrameEnergies(float[] buf, int rate)
        {
            int n = Frame(rate), frames = Mathf.Max(1, (buf.Length + n - 1) / n);
            var e = new float[frames];
            for (int f = 0; f < frames; f++)
            {
                double sum = 0;
                int a = f * n, b = Mathf.Min(buf.Length, a + n);
                for (int i = a; i < b; i++) sum += buf[i] * buf[i];
                e[f] = b > a ? (float)(sum / (b - a)) : 0f;
            }
            return e;
        }

        static bool[] ActiveMask(float[] e, out int loudest)
        {
            loudest = 0;
            for (int f = 1; f < e.Length; f++)
                if (e[f] > e[loudest]) loudest = f;
            float floor = e[loudest] * Mathf.Pow(10f, -SilenceDb / 10f);
            var active = new bool[e.Length];
            for (int f = 0; f < e.Length; f++) active[f] = e[f] > floor && e[f] > 1e-14f;
            return active;
        }

        static float[] BandPass(float[] buf, int rate)
        {
            var outBuf = new float[buf.Length];
            var h1 = new Svf();
            var h2 = new Svf();
            var l1 = new Svf();
            var l2 = new Svf();
            for (int i = 0; i < buf.Length; i++)
            {
                h1.Process(buf[i], 250f, 0.54f, rate, out _, out _, out float y);
                h2.Process(y, 250f, 1.31f, rate, out _, out _, out y);
                l1.Process(y, 6000f, 0.54f, rate, out y, out _, out _);
                l2.Process(y, 6000f, 1.31f, rate, out y, out _, out _);
                outBuf[i] = y;
            }
            return outBuf;
        }
    }

    /// <summary>Simper's trapezoidal state-variable filter: stable under fast cutoff sweeps. Band output has unity gain at the centre.</summary>
    public struct Svf
    {
        float ic1, ic2;

        public void Process(float x, float fc, float q, int rate, out float low, out float band, out float high)
        {
            fc = Mathf.Clamp(fc, 10f, rate * 0.45f);
            float g = Mathf.Tan(Mathf.PI * fc / rate), k = 1f / Mathf.Max(0.3f, q);
            float a1 = 1f / (1f + g * (g + k)), a2 = g * a1, a3 = g * a2;
            float v3 = x - ic2;
            float v1 = a1 * ic1 + a2 * v3;
            float v2 = ic2 + a2 * ic1 + a3 * v3;
            ic1 = 2f * v1 - ic1;
            ic2 = 2f * v2 - ic2;
            low = v2;
            band = k * v1;
            high = x - k * v1 - v2;
        }

        public void Process(float x, float fc, float q, out float low, out float band, out float high) =>
            Process(x, fc, q, SynthPatch.SampleRate, out low, out band, out high);

        public void Reset() => ic1 = ic2 = 0f;
    }
}
