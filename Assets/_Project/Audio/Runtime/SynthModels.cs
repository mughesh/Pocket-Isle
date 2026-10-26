using System;
using System.Collections.Generic;
using UnityEngine;

namespace Diorama.Audio
{
    /// <summary>What a layer is: the plain oscillator / noise wave, or one of the physical models below.</summary>
    public enum SynthModel { Oscillator, Modal, Particles, Bubbles, Fire, Wind, Friction, Breath }

    public enum ModalMaterial { Custom, Wood, Stone, Glass, ClayPot, Brass, Crystal }
    public enum ModalExciter { Click, Mallet, Noise }

    [Serializable]
    public class ModalMode
    {
        [Tooltip("Frequency as a multiple of the layer's pitch.")] [Range(0.1f, 20f)] public float ratio = 1f;
        [Tooltip("Seconds to fall 60 dB.")] [Range(0.005f, 8f)] public float decay = 0.3f;
        [Range(0f, 2f)] public float gain = 1f;
    }

    /// <summary>
    /// Modal resonator bank (modal synthesis, as in STK ModalBar): damped sine modes rung by a click, mallet or noise burst.
    /// The layer's note / Hz is the first mode. Material presets: wood, stone, glass, clay pot, brass, crystal.
    /// </summary>
    [Serializable]
    public class ModalSettings
    {
        public ModalMaterial material = ModalMaterial.Wood;
        public ModalExciter exciter = ModalExciter.Mallet;
        [Tooltip("Mallet / noise burst length, ms. Longer = softer, fewer high modes.")] [Range(0.1f, 20f)] public float exciterMs = 1.5f;
        [Range(0.05f, 4f)] public float decayScale = 1f;
        [Tooltip("Higher modes die faster (felt, wet wood).")] [Range(0f, 1f)] public float damping;
        [Tooltip("Where it is struck: 0.5 = middle (odd modes only), near 0 = edge (all modes).")] [Range(0f, 1f)] public float strike = 0.22f;
        [Tooltip("Random spread per mode (shimmer, beating).")] [Range(0f, 0.02f)] public float detune;
        [Tooltip("Used when the material is Custom.")] public List<ModalMode> modes = new List<ModalMode>();
    }

    /// <summary>
    /// Particle collisions (Cook's PhISEM, as in STK Shakers): a shake energy that decays, random collisions while it lasts,
    /// each a short noise grain through up to three resonances. Footsteps, gravel, pebbles, debris, rustle, leaves.
    /// </summary>
    [Serializable]
    public class ParticleSettings
    {
        [Tooltip("Collisions per second at full energy.")] [Range(1f, 3000f)] public float density = 300f;
        [Tooltip("Seconds for the shake energy to die away; 0 = steady (beds, rustles held by the envelope).")] [Range(0f, 10f)] public float systemDecay = 0.1f;
        [Tooltip("Length of one grain, ms.")] [Range(0.2f, 60f)] public float grainMs = 3f;
        [Tooltip("Resonance frequencies, Hz (0 = off). Scaled by the layer's pitch envelope.")] [Range(0f, 8000f)] public float res1 = 2000f;
        [Range(0f, 8000f)] public float res2;
        [Range(0f, 8000f)] public float res3;
        [Range(0.5f, 40f)] public float q = 4f;
        [Tooltip("Random retune of the resonances per collision.")] [Range(0f, 1f)] public float freqJitter = 0.3f;
        [Tooltip("Random size of each collision.")] [Range(0f, 1f)] public float gainJitter = 0.7f;
    }

    /// <summary>
    /// Bubbles (van den Doel): each bubble a sine at the Minnaert frequency of its radius, damped by d = 0.13 f + 0.0072 f^1.5,
    /// pitch rising as it nears the surface. Drips, splashes, springs, steam pops.
    /// </summary>
    [Serializable]
    public class BubbleSettings
    {
        [Tooltip("Bubbles at time 0.")] [Range(0, 16)] public int atStart = 1;
        [Tooltip("New bubbles per second while the layer sounds.")] [Range(0f, 300f)] public float rate;
        [Tooltip("Radius range, mm (2 mm ~ 1.6 kHz, 4 mm ~ 0.8 kHz). The pitch envelope scales it.")] [Range(0.3f, 15f)] public float radiusMin = 2f;
        [Range(0.3f, 15f)] public float radiusMax = 4f;
        [Tooltip("Pitch rise (xi); 0.1 is a typical drip.")] [Range(0f, 1f)] public float rise = 0.1f;
        [Tooltip("Longer than nature, so small bubbles read on small speakers.")] [Range(0.25f, 8f)] public float decayScale = 1.5f;
        [Range(0f, 1f)] public float ampJitter = 0.5f;
    }

    /// <summary>Fire (Farnell, Designing Sound): sparse sharp crackles through a band-pass, flickering hiss, low lapping with slow AM.</summary>
    [Serializable]
    public class FireSettings
    {
        [Range(0f, 1f)] public float intensity = 0.6f;
        [Tooltip("Crackles per second at full intensity.")] [Range(0f, 300f)] public float crackleRate = 25f;
        [Range(300f, 6000f)] public float crackleFreq = 2500f;
        [Range(0f, 2f)] public float crackleGain = 1f;
        [Range(0f, 2f)] public float hissGain = 0.3f;
        [Range(1000f, 8000f)] public float hissFreq = 3000f;
        [Range(0f, 2f)] public float lapGain = 0.5f;
        [Range(100f, 1500f)] public float lapFreq = 450f;
        [Tooltip("Lapping flicker, Hz.")] [Range(0.2f, 12f)] public float lapRate = 3f;
    }

    /// <summary>Wind (Farnell): noise through a band-pass moved by slow random gusts; whistle partials when gusts are strong.</summary>
    [Serializable]
    public class WindSettings
    {
        [Tooltip("Centre of the wind band, Hz (moves with the gusts and the pitch envelope).")] [Range(100f, 4000f)] public float freq = 700f;
        [Range(0.3f, 12f)] public float q = 1.5f;
        [Tooltip("Gust changes per second.")] [Range(0.02f, 6f)] public float gustRate = 0.4f;
        [Range(0f, 1f)] public float gustDepth = 0.6f;
        [Range(0f, 1f)] public float baseLevel = 0.35f;
        [Range(0f, 2f)] public float whistleGain = 0.3f;
        [Range(300f, 4000f)] public float whistleFreq = 1400f;
        [Tooltip("Gust strength where the whistle starts.")] [Range(0f, 1f)] public float whistleThreshold = 0.6f;
        [Tooltip("High flutter band that follows the gusts (leaves).")] [Range(0f, 2f)] public float leavesGain;
    }

    /// <summary>
    /// Friction / creak (Farnell, creaking door): stick-slip pulses at a rate that moves from start to end, each slip ringing a
    /// small modal body (the layer's note is the body pitch).
    /// </summary>
    [Serializable]
    public class FrictionSettings
    {
        [Tooltip("Slips per second at the start / end.")] [Range(1f, 300f)] public float rateStart = 12f;
        [Range(1f, 300f)] public float rateEnd = 30f;
        [Range(0.01f, 10f)] public float rateTime = 0.6f;
        [Range(0f, 1f)] public float irregularity = 0.3f;
        [Tooltip("Harder = sharper, louder slips.")] [Range(0f, 1f)] public float pressure = 0.6f;
        public ModalMaterial body = ModalMaterial.Wood;
        [Range(0.05f, 4f)] public float bodyDecay = 0.6f;
        [Tooltip("Rubbing noise between the slips.")] [Range(0f, 1f)] public float scrape = 0.2f;
    }

    /// <summary>Air puff / breath (Farnell): noise through two formant band-passes with a fast sweep; optional flutter (snorts).</summary>
    [Serializable]
    public class BreathSettings
    {
        [Range(150f, 5000f)] public float formantStart = 600f;
        [Range(150f, 5000f)] public float formantEnd = 1400f;
        [Range(0.01f, 4f)] public float sweepTime = 0.15f;
        [Tooltip("1 linear, below 1 fast then slow.")] [Range(0.2f, 6f)] public float sweepCurve = 0.6f;
        [Tooltip("Second formant as a multiple of the first.")] [Range(1f, 4f)] public float secondFormant = 2.4f;
        [Range(0.5f, 20f)] public float q = 3f;
        [Tooltip("0 = white (hissy), 1 = pink (softer).")] [Range(0f, 1f)] public float pinkMix = 0.5f;
        [Tooltip("Irregular flutter, Hz (nostrils, lips). 0 = off.")] [Range(0f, 80f)] public float flutterRate;
        [Range(0f, 1f)] public float flutterDepth;
    }

    /// <summary>Several layers moved and levelled together (transient, body, tail, sparkle): a layer joins with its group index.</summary>
    [Serializable]
    public class LayerGroup
    {
        public string label = "";
        public bool enabled = true;
        [Range(0f, 30f)] public float offset;
        [Range(0f, 2f)] public float gain = 1f;
    }

    /// <summary>Plays the layer N times at random points of a window (debris, sparks, drips). Count 0 = off.</summary>
    [Serializable]
    public class ScatterSettings
    {
        [Range(0, 64)] public int count;
        [Tooltip("Seconds after the layer's start in which the copies land.")] [Range(0f, 30f)] public float window = 0.5f;
        [Tooltip("Random pitch per copy, semitones.")] [Range(0f, 12f)] public float pitchJitter = 2f;
        [Range(0f, 1f)] public float gainJitter = 0.5f;
        [Tooltip("Above 1 bunches copies at the start (debris settling), 1 = even.")] [Range(0.2f, 4f)] public float bunching = 1f;
    }

    /// <summary>One running model voice: Next(t, f, k) with t the layer time, f the current pitch in Hz, k = f / 440 (noise models scale by it).</summary>
    public interface IModelVoice
    {
        float Next(float t, float f, float k);
    }

    /// <summary>The model voices. Pure C#, deterministic per seed.</summary>
    public static class SynthModels
    {
        const int Rate = SynthPatch.SampleRate;

        public static IModelVoice Create(SynthLayer layer, int seed)
        {
            var rng = new System.Random(seed);
            switch (layer.model)
            {
                case SynthModel.Modal: return new ModalVoice(layer.modal ?? new ModalSettings(), rng);
                case SynthModel.Particles: return new ParticleVoice(layer.particles ?? new ParticleSettings(), rng);
                case SynthModel.Bubbles: return new BubbleVoice(layer.bubbles ?? new BubbleSettings(), rng);
                case SynthModel.Fire: return new FireVoice(layer.fire ?? new FireSettings(), rng);
                case SynthModel.Wind: return new WindVoice(layer.wind ?? new WindSettings(), rng);
                case SynthModel.Friction: return new FrictionVoice(layer.friction ?? new FrictionSettings(), rng);
                case SynthModel.Breath: return new BreathVoice(layer.breath ?? new BreathSettings(), rng);
                default: return null;
            }
        }

        /// <summary>(ratio, seconds to -60 dB, gain) per material.</summary>
        public static ModalMode[] Modes(ModalMaterial m, List<ModalMode> custom)
        {
            float[,] t;
            switch (m)
            {
                case ModalMaterial.Wood: t = new[,] { { 1f, 0.12f, 1f }, { 2.777f, 0.06f, 0.45f }, { 4f, 0.045f, 0.2f }, { 7.107f, 0.03f, 0.2f } }; break;
                case ModalMaterial.Stone: t = new[,] { { 1f, 0.07f, 1f }, { 1.62f, 0.055f, 0.8f }, { 2.34f, 0.05f, 0.7f }, { 3.13f, 0.04f, 0.5f }, { 4.2f, 0.03f, 0.4f }, { 5.1f, 0.025f, 0.3f } }; break;
                case ModalMaterial.Glass: t = new[,] { { 1f, 0.9f, 1f }, { 2.32f, 0.6f, 0.55f }, { 4.25f, 0.4f, 0.35f }, { 6.63f, 0.25f, 0.2f }, { 9.38f, 0.15f, 0.1f } }; break;
                case ModalMaterial.ClayPot: t = new[,] { { 1f, 0.22f, 1f }, { 1.48f, 0.14f, 0.55f }, { 2.1f, 0.1f, 0.45f }, { 2.7f, 0.08f, 0.3f }, { 3.6f, 0.05f, 0.2f } }; break;
                case ModalMaterial.Brass:
                    // Bell partials: hum, prime, tierce, quint, nominal...
                    t = new[,] { { 0.5f, 2f, 0.5f }, { 1f, 1.5f, 1f }, { 1.183f, 1.2f, 0.6f }, { 1.506f, 1f, 0.5f }, { 2f, 0.9f, 0.6f }, { 2.514f, 0.6f, 0.3f }, { 2.662f, 0.5f, 0.25f }, { 3.011f, 0.4f, 0.2f } };
                    break;
                case ModalMaterial.Crystal:
                    // Free bar modes, each paired with a slightly detuned twin for a slow shimmer.
                    t = new[,] { { 1f, 1.6f, 1f }, { 1.003f, 1.6f, 0.6f }, { 2.756f, 1f, 0.5f }, { 2.762f, 1f, 0.3f }, { 5.404f, 0.6f, 0.25f }, { 8.933f, 0.4f, 0.12f } };
                    break;
                default:
                    return custom != null && custom.Count > 0 ? custom.ToArray() : new[] { new ModalMode() };
            }
            var modes = new ModalMode[t.GetLength(0)];
            for (int i = 0; i < modes.Length; i++) modes[i] = new ModalMode { ratio = t[i, 0], decay = t[i, 1], gain = t[i, 2] };
            return modes;
        }

        // ---- helpers ----

        static float White(System.Random rng) => (float)(rng.NextDouble() * 2 - 1);

        /// <summary>Paul Kellet's refined pink filter on white noise.</summary>
        struct Pink
        {
            float b0, b1, b2, b3, b4, b5, b6;

            public float Next(float w)
            {
                b0 = 0.99886f * b0 + w * 0.0555179f;
                b1 = 0.99332f * b1 + w * 0.0750759f;
                b2 = 0.96900f * b2 + w * 0.1538520f;
                b3 = 0.86650f * b3 + w * 0.3104856f;
                b4 = 0.55000f * b4 + w * 0.5329522f;
                b5 = -0.7616f * b5 - w * 0.0168980f;
                float s = (b0 + b1 + b2 + b3 + b4 + b5 + b6 + w * 0.5362f) * 0.11f;
                b6 = w * 0.115926f;
                return s;
            }
        }

        /// <summary>A smooth random control signal in 0..1: a new target every 1/rate seconds, cosine-interpolated.</summary>
        struct SmoothRandom
        {
            float _a, _b;
            double _phase;
            bool _init;

            public float Next(float rate, System.Random rng)
            {
                if (!_init)
                {
                    _a = (float)rng.NextDouble();
                    _b = (float)rng.NextDouble();
                    _init = true;
                }
                _phase += rate / Rate;
                if (_phase >= 1)
                {
                    _phase -= Math.Floor(_phase);
                    _a = _b;
                    _b = (float)rng.NextDouble();
                }
                float u = 0.5f - 0.5f * Mathf.Cos(Mathf.PI * (float)_phase);
                return _a + (_b - _a) * u;
            }
        }

        // ---- modal ----

        sealed class ModalVoice : IModelVoice
        {
            readonly float[] _ratio, _r, _gain;
            readonly double[] _y1, _y2;
            readonly double[] _c, _norm;
            readonly ModalExciter _exciter;
            readonly int _excLen;
            readonly System.Random _rng;
            float _lastF = -1f;
            int _n;

            public ModalVoice(ModalSettings s, System.Random rng, float decayMul = 1f)
            {
                _rng = rng;
                _exciter = s.exciter;
                _excLen = Mathf.Max(1, Mathf.RoundToInt(s.exciterMs * 0.001f * Rate));
                var modes = Modes(s.material, s.modes);
                int n = modes.Length;
                _ratio = new float[n];
                _r = new float[n];
                _gain = new float[n];
                _y1 = new double[n];
                _y2 = new double[n];
                _c = new double[n];
                _norm = new double[n];
                for (int i = 0; i < n; i++)
                {
                    var m = modes[i];
                    _ratio[i] = m.ratio * (1f + s.detune * (float)(rng.NextDouble() * 2 - 1));
                    float decay = Mathf.Max(0.005f, m.decay * s.decayScale * decayMul / (1f + s.damping * 3f * Mathf.Max(0f, m.ratio - 1f)));
                    _r[i] = Mathf.Exp(-6.91f / (decay * Rate));
                    // Strike position: a mode with a node at the strike point is barely excited.
                    _gain[i] = m.gain * (0.25f + 0.75f * Mathf.Abs(Mathf.Sin(Mathf.PI * Mathf.Max(0.02f, s.strike) * (i + 1))));
                }
            }

            public float Next(float t, float f, float k) => Process(Excite(), f);

            float Excite()
            {
                int n = _n++;
                if (n >= _excLen * 4 + 32) return 0f;
                switch (_exciter)
                {
                    case ModalExciter.Click:
                        return (n == 0 ? 1f : 0f) + 0.3f * White(_rng) * Mathf.Exp(-n / (0.0003f * Rate));
                    case ModalExciter.Mallet:
                    {
                        // Soft pulse with unit area: fast rise, exponential fall (tau = length / 3). Its spectrum is a smooth
                        // low-pass with no nulls, so a longer mallet softens the high modes without cancelling any body mode.
                        float tau = Mathf.Max(1f, _excLen / 3f), rise = 1f - Mathf.Exp(-(n + 1) / Mathf.Max(1f, 0.1f * tau));
                        return rise * Mathf.Exp(-n / tau) / tau;
                    }
                    default:
                        return White(_rng) * Mathf.Exp(-n / (float)_excLen) * 2f / _excLen;
                }
            }

            /// <summary>Rings every mode with input x at body pitch f (Hz).</summary>
            public float Process(float x, float f)
            {
                if (!Mathf.Approximately(f, _lastF))
                {
                    _lastF = f;
                    for (int i = 0; i < _ratio.Length; i++)
                    {
                        float hz = f * _ratio[i];
                        if (hz > 16000f || hz < 20f)
                        {
                            _c[i] = 0;
                            _norm[i] = 0;
                            continue;
                        }
                        double w = 2 * Math.PI * hz / Rate;
                        _c[i] = 2 * _r[i] * Math.Cos(w);
                        _norm[i] = Math.Sin(w); // impulse response then peaks at ~1
                    }
                }
                double sum = 0;
                for (int i = 0; i < _ratio.Length; i++)
                {
                    double r2 = _r[i] * (double)_r[i];
                    double y = _c[i] * _y1[i] - r2 * _y2[i] + x * _norm[i];
                    _y2[i] = _y1[i];
                    _y1[i] = y;
                    sum += y * _gain[i];
                }
                return (float)sum;
            }
        }

        // ---- particles (PhISEM) ----

        sealed class ParticleVoice : IModelVoice
        {
            readonly ParticleSettings _s;
            readonly System.Random _rng;
            readonly float[] _base, _freq;
            readonly Svf[] _bp;
            readonly float _grain;
            float _level;
            bool _shaken;

            public ParticleVoice(ParticleSettings s, System.Random rng)
            {
                _s = s;
                _rng = rng;
                var list = new List<float>();
                foreach (var f in new[] { s.res1, s.res2, s.res3 })
                    if (f > 0f) list.Add(f);
                _base = list.ToArray();
                _freq = (float[])_base.Clone();
                _bp = new Svf[_base.Length];
                _grain = Mathf.Exp(-6.91f / (Mathf.Max(0.2f, s.grainMs) * 0.001f * Rate));
            }

            public float Next(float t, float f, float k)
            {
                float energy = _s.systemDecay > 0f ? Mathf.Exp(-6.91f * t / _s.systemDecay) : 1f;
                bool first = !_shaken;
                _shaken = true;
                if (first || _rng.NextDouble() < _s.density * energy / Rate) // the shake itself is always a collision
                {
                    _level += (1f - _s.gainJitter * (float)_rng.NextDouble()) * energy;
                    if (_level > 2f) _level = 2f;
                    for (int i = 0; i < _base.Length; i++) _freq[i] = _base[i] * (1f + _s.freqJitter * (float)(_rng.NextDouble() * 2 - 1) * 0.5f);
                }
                float noise = White(_rng) * _level;
                _level *= _grain;
                if (_base.Length == 0) return noise;
                float sum = 0f;
                for (int i = 0; i < _base.Length; i++)
                {
                    _bp[i].Process(noise, _freq[i] * k, _s.q, out _, out float b, out _);
                    sum += b;
                }
                return sum * Mathf.Sqrt(_s.q) / _base.Length;
            }
        }

        // ---- bubbles (van den Doel) ----

        sealed class BubbleVoice : IModelVoice
        {
            struct Bubble
            {
                public float f0, d, sigma, amp, age;
                public double phase;
            }

            readonly BubbleSettings _s;
            readonly System.Random _rng;
            readonly List<Bubble> _live = new List<Bubble>();
            bool _started;

            public BubbleVoice(BubbleSettings s, System.Random rng)
            {
                _s = s;
                _rng = rng;
            }

            void Spawn(float k)
            {
                if (_live.Count >= 48) return;
                float rMin = Mathf.Min(_s.radiusMin, _s.radiusMax), rMax = Mathf.Max(_s.radiusMin, _s.radiusMax);
                float rmm = rMin + (rMax - rMin) * (float)_rng.NextDouble();
                float f0 = 3.26f / (rmm * 0.001f) * k; // Minnaert
                f0 = Mathf.Clamp(f0, 60f, 9000f);
                float d = 0.13f * f0 + 0.0072f * Mathf.Pow(f0, 1.5f);
                _live.Add(new Bubble
                {
                    f0 = f0,
                    d = d / _s.decayScale,
                    sigma = _s.rise * d,
                    amp = (1f - _s.ampJitter * (float)_rng.NextDouble()) * Mathf.Sqrt(rmm / 3f),
                });
            }

            public float Next(float t, float f, float k)
            {
                if (!_started)
                {
                    _started = true;
                    for (int i = 0; i < _s.atStart; i++) Spawn(k);
                }
                if (_s.rate > 0f && _rng.NextDouble() < _s.rate / Rate) Spawn(k);
                float sum = 0f;
                for (int i = _live.Count - 1; i >= 0; i--)
                {
                    var b = _live[i];
                    float env = Mathf.Exp(-b.d * b.age);
                    if (env < 0.001f)
                    {
                        _live.RemoveAt(i);
                        continue;
                    }
                    // Smooth 0.3 ms onset so a bubble never clicks in.
                    float onset = Mathf.Min(1f, b.age / 0.0003f);
                    sum += b.amp * env * onset * (float)Math.Sin(2 * Math.PI * b.phase);
                    b.phase += b.f0 * (1f + b.sigma * b.age) / Rate;
                    if (b.phase > 1) b.phase -= Math.Floor(b.phase);
                    b.age += 1f / Rate;
                    _live[i] = b;
                }
                return sum;
            }
        }

        // ---- fire (Farnell) ----

        sealed class FireVoice : IModelVoice
        {
            readonly FireSettings _s;
            readonly System.Random _rng;
            Svf _crackBp, _hissHp, _lapBp;
            SmoothRandom _hissAm, _lapAm;
            float _cAmp, _cCoef, _cFreq;

            public FireVoice(FireSettings s, System.Random rng)
            {
                _s = s;
                _rng = rng;
                _cFreq = s.crackleFreq;
            }

            public float Next(float t, float f, float k)
            {
                float i = _s.intensity;
                if (_rng.NextDouble() < _s.crackleRate * i / Rate)
                {
                    float size = (float)_rng.NextDouble();
                    _cAmp = 0.2f + 0.8f * size * size;
                    _cFreq = _s.crackleFreq * (0.6f + 0.8f * (float)_rng.NextDouble());
                    _cCoef = Mathf.Exp(-6.91f / ((0.001f + 0.003f * (float)_rng.NextDouble()) * Rate));
                }
                float c = 0f;
                if (_cAmp > 1e-4f)
                {
                    _crackBp.Process(White(_rng) * _cAmp, _cFreq * k, 2f, out _, out c, out _);
                    _cAmp *= _cCoef;
                }
                _hissHp.Process(White(_rng), _s.hissFreq * k, 0.7f, out _, out _, out float hiss);
                float ha = _hissAm.Next(30f, _rng);
                _lapBp.Process(White(_rng), _s.lapFreq * k, 1.2f, out _, out float lap, out _);
                float la = 0.35f + 0.65f * _lapAm.Next(_s.lapRate, _rng);
                return c * _s.crackleGain * 4f + hiss * _s.hissGain * ha * ha * 2f * i + lap * _s.lapGain * la * (0.4f + i);
            }
        }

        // ---- wind (Farnell) ----

        sealed class WindVoice : IModelVoice
        {
            readonly WindSettings _s;
            readonly System.Random _rng;
            Pink _pink;
            Svf _band, _whistle, _leaves;
            SmoothRandom _gust, _gust2, _flutter;

            public WindVoice(WindSettings s, System.Random rng)
            {
                _s = s;
                _rng = rng;
            }

            public float Next(float t, float f, float k)
            {
                float sm = 0.75f * _gust.Next(_s.gustRate, _rng) + 0.25f * _gust2.Next(_s.gustRate * 3.1f, _rng);
                float g = Mathf.Clamp01(_s.baseLevel + _s.gustDepth * (sm - 0.3f) * 1.6f);
                float w = White(_rng);
                _band.Process(_pink.Next(w) * 3f, _s.freq * k * (0.6f + 0.8f * g), _s.q, out _, out float main, out _);
                float s = main * g;
                if (_s.whistleGain > 0f && g > _s.whistleThreshold)
                {
                    float over = (g - _s.whistleThreshold) / Mathf.Max(0.05f, 1f - _s.whistleThreshold);
                    _whistle.Process(w, _s.whistleFreq * k * (1f + 0.4f * g), 25f, out _, out float wh, out _);
                    s += wh * over * _s.whistleGain * 3f;
                }
                if (_s.leavesGain > 0f)
                {
                    _leaves.Process(White(_rng), 3800f * k, 0.9f, out _, out float lv, out _);
                    float fl = _flutter.Next(18f, _rng);
                    s += lv * g * g * fl * _s.leavesGain;
                }
                return s;
            }
        }

        // ---- friction / creak (Farnell) ----

        sealed class FrictionVoice : IModelVoice
        {
            readonly FrictionSettings _s;
            readonly System.Random _rng;
            readonly ModalVoice _body;
            Svf _scrape;
            double _phase;
            float _interval = 1f, _pulse;
            int _pulseLeft, _pulseLen;

            public FrictionVoice(FrictionSettings s, System.Random rng)
            {
                _s = s;
                _rng = rng;
                _body = new ModalVoice(new ModalSettings { material = s.body, decayScale = s.bodyDecay, strike = 0.3f, exciter = ModalExciter.Click }, rng);
                _pulseLen = Mathf.Max(2, Mathf.RoundToInt((0.0003f + (1f - s.pressure) * 0.002f) * Rate));
            }

            public float Next(float t, float f, float k)
            {
                float u = _s.rateTime > 0f ? Mathf.Clamp01(t / _s.rateTime) : 1f;
                float rate = Mathf.Lerp(_s.rateStart, _s.rateEnd, u);
                _phase += rate / Rate / _interval;
                float x = 0f;
                if (_phase >= 1)
                {
                    _phase -= Math.Floor(_phase);
                    _interval = 1f + _s.irregularity * (float)(_rng.NextDouble() * 2 - 1) * 0.8f;
                    _pulse = (0.4f + 0.6f * _s.pressure) * (1f - 0.5f * _s.irregularity * (float)_rng.NextDouble());
                    _pulseLeft = _pulseLen;
                }
                if (_pulseLeft > 0)
                {
                    // Slip: a short half-sine kick into the body.
                    float v = Mathf.Sin(Mathf.PI * (_pulseLen - _pulseLeft + 0.5f) / _pulseLen);
                    x = _pulse * v * 2f / _pulseLen * 8f;
                    _pulseLeft--;
                }
                float ring = _body.Process(x, f);
                _scrape.Process(White(_rng), f * 4f, 1.5f, out _, out float sc, out _);
                return ring + sc * _s.scrape * 0.3f * (_pulseLeft > 0 ? 1f : 0.3f);
            }
        }

        // ---- breath / air puff (Farnell) ----

        sealed class BreathVoice : IModelVoice
        {
            readonly BreathSettings _s;
            readonly System.Random _rng;
            Pink _pink;
            Svf _f1, _f2;
            SmoothRandom _flutter;

            public BreathVoice(BreathSettings s, System.Random rng)
            {
                _s = s;
                _rng = rng;
            }

            public float Next(float t, float f, float k)
            {
                float w = White(_rng);
                float n = Mathf.Lerp(w, _pink.Next(w) * 3f, _s.pinkMix);
                float u = _s.sweepTime > 0f ? Mathf.Pow(Mathf.Clamp01(t / _s.sweepTime), _s.sweepCurve) : 1f;
                float fc = Mathf.Lerp(_s.formantStart, _s.formantEnd, u) * k;
                _f1.Process(n, fc, _s.q, out _, out float a, out _);
                _f2.Process(n, fc * _s.secondFormant, _s.q * 1.3f, out _, out float b, out _);
                float s = (a + 0.6f * b) * Mathf.Sqrt(_s.q);
                if (_s.flutterRate > 0f && _s.flutterDepth > 0f)
                    s *= 1f - _s.flutterDepth * _flutter.Next(_s.flutterRate, _rng);
                return s;
            }
        }
    }
}
