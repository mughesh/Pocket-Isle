using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Diorama.Audio.EditorTools
{
    /// <summary>
    /// Starting points for the physical models: the Lab's "Model preset" row applies one to a layer, and recipes build layers
    /// from them (ModelPresets.Layer("Particles/Crunch")). Each preset sets the model, its settings and a fitting envelope.
    /// </summary>
    public static class ModelPresets
    {
        static readonly List<KeyValuePair<string, Action<SynthLayer>>> Table = new List<KeyValuePair<string, Action<SynthLayer>>>();

        public static string[] Names => Table.Select(kv => kv.Key).ToArray();

        static void Add(string name, Action<SynthLayer> apply) => Table.Add(new KeyValuePair<string, Action<SynthLayer>>(name, apply));

        /// <summary>Applies the preset to a layer (keeps its label, note, offset, gain and group).</summary>
        public static bool Apply(string name, SynthLayer layer)
        {
            foreach (var kv in Table)
                if (kv.Key == name)
                {
                    kv.Value(layer);
                    return true;
                }
            Debug.LogError($"[AudioLab] No model preset '{name}'.");
            return false;
        }

        /// <summary>A fresh layer from a preset.</summary>
        public static SynthLayer Layer(string name, float hz = 440f)
        {
            var l = new SynthLayer { hz = hz, label = name };
            Apply(name, l);
            return l;
        }

        // One-shot: envelope stays open, the model shapes the sound. Steady: for beds and loops.
        static void OneShot(SynthLayer l, float seconds) => l.amp = new SynthEnvelope { attack = 0.003f, hold = seconds, decay = 0.05f, sustain = 0f, release = 0.02f };
        static void Steady(SynthLayer l) => l.amp = new SynthEnvelope { attack = 0.2f, hold = 0f, decay = 1f, sustain = 1f, release = 0.2f };

        static void Modal(SynthLayer l, ModalMaterial m, ModalExciter e, float ms, float decayScale = 1f, float damping = 0f, float detune = 0f)
        {
            l.model = SynthModel.Modal;
            l.modal = new ModalSettings { material = m, exciter = e, exciterMs = ms, decayScale = decayScale, damping = damping, detune = detune };
            OneShot(l, 3f);
        }

        static void Particles(SynthLayer l, float density, float systemDecay, float grainMs, float r1, float r2, float r3, float q, float fj, float gj = 0.7f)
        {
            l.model = SynthModel.Particles;
            l.particles = new ParticleSettings { density = density, systemDecay = systemDecay, grainMs = grainMs, res1 = r1, res2 = r2, res3 = r3, q = q, freqJitter = fj, gainJitter = gj };
            if (systemDecay > 0f) OneShot(l, systemDecay * 0.8f);
            else Steady(l);
        }

        static ModelPresets()
        {
            Add("Oscillator (plain)", l => l.model = SynthModel.Oscillator);

            // Modal: the layer's note is the first mode.
            Add("Modal/Wood knock", l => Modal(l, ModalMaterial.Wood, ModalExciter.Mallet, 0.8f));
            Add("Modal/Wood thud (log)", l => Modal(l, ModalMaterial.Wood, ModalExciter.Mallet, 4f, 2f, 0.4f));
            Add("Modal/Stone clack", l => Modal(l, ModalMaterial.Stone, ModalExciter.Click, 0.3f));
            Add("Modal/Glass clink", l => Modal(l, ModalMaterial.Glass, ModalExciter.Click, 0.2f, 0.6f));
            Add("Modal/Clay pot tok", l => Modal(l, ModalMaterial.ClayPot, ModalExciter.Mallet, 1f));
            Add("Modal/Brass clang", l => Modal(l, ModalMaterial.Brass, ModalExciter.Mallet, 0.6f, 0.8f, 0.2f));
            Add("Modal/Crystal ting", l => Modal(l, ModalMaterial.Crystal, ModalExciter.Mallet, 0.4f, 1f, 0f, 0.002f));

            // Particles (PhISEM / STK Shakers-like): the resonances read at the layer's own pitch (keep Hz at 440 unless sweeping).
            Add("Particles/Crunch (grass step)", l => Particles(l, 1500f, 0.18f, 1.2f, 1800f, 3600f, 0f, 2f, 0.5f));
            Add("Particles/Gravel step", l => Particles(l, 1000f, 0.2f, 2f, 2200f, 4200f, 0f, 4f, 0.6f));
            Add("Particles/Little rocks", l => Particles(l, 500f, 0.25f, 3f, 3200f, 0f, 0f, 12f, 0.5f));
            Add("Particles/Big rocks", l => Particles(l, 150f, 0.3f, 5f, 1500f, 0f, 0f, 10f, 0.5f));
            Add("Particles/Sticks", l => Particles(l, 400f, 0.25f, 3f, 1400f, 2300f, 0f, 6f, 0.3f));
            Add("Particles/Sand paper", l => Particles(l, 2000f, 0f, 0.6f, 4500f, 0f, 0f, 1.5f, 0.2f));
            Add("Particles/Leaves", l => Particles(l, 400f, 0f, 2f, 2500f, 5000f, 0f, 1.5f, 0.5f));
            Add("Particles/Dust fall", l => Particles(l, 1200f, 0.4f, 0.8f, 3000f, 6000f, 0f, 1.2f, 0.4f, 0.9f));
            Add("Particles/Water drops", l => Particles(l, 40f, 0.5f, 8f, 450f, 600f, 750f, 25f, 0.9f));

            // Bubbles (van den Doel)
            Add("Bubbles/Drip", l =>
            {
                l.model = SynthModel.Bubbles;
                l.bubbles = new BubbleSettings { atStart = 1, rate = 0f, radiusMin = 2f, radiusMax = 2.6f, rise = 0.15f, decayScale = 2.5f, ampJitter = 0f };
                OneShot(l, 0.3f);
            });
            Add("Bubbles/Splash", l =>
            {
                l.model = SynthModel.Bubbles;
                l.bubbles = new BubbleSettings { atStart = 6, rate = 60f, radiusMin = 1f, radiusMax = 5f, rise = 0.1f, decayScale = 1.5f, ampJitter = 0.6f };
                OneShot(l, 0.4f);
            });
            Add("Bubbles/Spring (steady)", l =>
            {
                l.model = SynthModel.Bubbles;
                l.bubbles = new BubbleSettings { atStart = 0, rate = 18f, radiusMin = 1.5f, radiusMax = 4f, rise = 0.12f, decayScale = 2f, ampJitter = 0.7f };
                Steady(l);
            });
            Add("Bubbles/Steam pop", l =>
            {
                l.model = SynthModel.Bubbles;
                l.bubbles = new BubbleSettings { atStart = 1, rate = 0f, radiusMin = 1.2f, radiusMax = 1.6f, rise = 0.4f, decayScale = 4f, ampJitter = 0f };
                OneShot(l, 0.1f);
            });

            // Fire (Farnell)
            Add("Fire/Ember crackle", l =>
            {
                l.model = SynthModel.Fire;
                l.fire = new FireSettings { intensity = 0.5f, crackleRate = 14f, crackleFreq = 2800f, crackleGain = 1.2f, hissGain = 0.12f, hissFreq = 3500f, lapGain = 0.15f, lapFreq = 500f, lapRate = 2f };
                Steady(l);
            });
            Add("Fire/Campfire", l =>
            {
                l.model = SynthModel.Fire;
                l.fire = new FireSettings { intensity = 0.7f, crackleRate = 30f, crackleFreq = 2400f, crackleGain = 1f, hissGain = 0.3f, hissFreq = 3000f, lapGain = 0.6f, lapFreq = 450f, lapRate = 3f };
                Steady(l);
            });
            Add("Fire/Whoomph", l =>
            {
                l.model = SynthModel.Fire;
                l.fire = new FireSettings { intensity = 1f, crackleRate = 60f, crackleFreq = 2000f, crackleGain = 0.8f, hissGain = 0.5f, hissFreq = 2500f, lapGain = 1.2f, lapFreq = 380f, lapRate = 6f };
                l.amp = new SynthEnvelope { attack = 0.03f, hold = 0.05f, decay = 0.6f, sustain = 0f, release = 0.1f };
            });
            Add("Fire/Fury", l =>
            {
                l.model = SynthModel.Fire;
                l.fire = new FireSettings { intensity = 1f, crackleRate = 120f, crackleFreq = 1800f, crackleGain = 1f, hissGain = 0.6f, hissFreq = 2200f, lapGain = 1.2f, lapFreq = 320f, lapRate = 8f };
                Steady(l);
            });

            // Wind (Farnell)
            Add("Wind/Breeze", l =>
            {
                l.model = SynthModel.Wind;
                l.wind = new WindSettings { freq = 600f, q = 1.2f, gustRate = 0.25f, gustDepth = 0.6f, baseLevel = 0.3f, whistleGain = 0f, leavesGain = 0.3f };
                Steady(l);
            });
            Add("Wind/Gust", l =>
            {
                l.model = SynthModel.Wind;
                l.wind = new WindSettings { freq = 800f, q = 1.6f, gustRate = 1.5f, gustDepth = 0.9f, baseLevel = 0.2f, whistleGain = 0.4f, whistleFreq = 1500f, whistleThreshold = 0.6f, leavesGain = 0.4f };
                l.amp = new SynthEnvelope { attack = 0.25f, hold = 0.1f, decay = 0.6f, sustain = 0f, release = 0.2f };
            });
            Add("Wind/Whoosh", l =>
            {
                l.model = SynthModel.Wind;
                l.wind = new WindSettings { freq = 900f, q = 2.5f, gustRate = 4f, gustDepth = 0.3f, baseLevel = 0.8f, whistleGain = 0.25f, whistleFreq = 1800f, whistleThreshold = 0.8f };
                l.amp = new SynthEnvelope { attack = 0.08f, hold = 0f, decay = 0.25f, sustain = 0f, release = 0.05f };
                l.pitchStart = -7f;
                l.pitchEnd = 5f;
                l.pitchTime = 0.3f;
            });
            Add("Wind/Whistle", l =>
            {
                l.model = SynthModel.Wind;
                l.wind = new WindSettings { freq = 1000f, q = 1.5f, gustRate = 0.6f, gustDepth = 0.9f, baseLevel = 0.4f, whistleGain = 0.8f, whistleFreq = 1200f, whistleThreshold = 0.45f };
                Steady(l);
            });

            // Friction (Farnell creaking door): the layer's note is the body pitch.
            Add("Friction/Wood creak", l =>
            {
                l.model = SynthModel.Friction;
                l.friction = new FrictionSettings { rateStart = 9f, rateEnd = 28f, rateTime = 0.7f, irregularity = 0.35f, pressure = 0.6f, body = ModalMaterial.Wood, bodyDecay = 0.8f, scrape = 0.15f };
                OneShot(l, 0.8f);
            });
            Add("Friction/Rope creak", l =>
            {
                l.model = SynthModel.Friction;
                l.friction = new FrictionSettings { rateStart = 35f, rateEnd = 60f, rateTime = 0.4f, irregularity = 0.25f, pressure = 0.45f, body = ModalMaterial.ClayPot, bodyDecay = 0.5f, scrape = 0.3f };
                OneShot(l, 0.5f);
            });
            Add("Friction/Chain creak", l =>
            {
                l.model = SynthModel.Friction;
                l.friction = new FrictionSettings { rateStart = 14f, rateEnd = 10f, rateTime = 0.6f, irregularity = 0.6f, pressure = 0.8f, body = ModalMaterial.Brass, bodyDecay = 0.25f, scrape = 0.1f };
                OneShot(l, 0.6f);
            });
            Add("Friction/Leather squeak", l =>
            {
                l.model = SynthModel.Friction;
                l.friction = new FrictionSettings { rateStart = 70f, rateEnd = 130f, rateTime = 0.25f, irregularity = 0.15f, pressure = 0.4f, body = ModalMaterial.ClayPot, bodyDecay = 0.3f, scrape = 0.4f };
                OneShot(l, 0.3f);
            });

            // Breath (Farnell)
            Add("Breath/Puff", l =>
            {
                l.model = SynthModel.Breath;
                l.breath = new BreathSettings { formantStart = 500f, formantEnd = 1200f, sweepTime = 0.12f, sweepCurve = 0.5f, secondFormant = 2.4f, q = 2.5f, pinkMix = 0.6f };
                l.amp = new SynthEnvelope { attack = 0.015f, hold = 0f, decay = 0.25f, sustain = 0f, release = 0.05f };
            });
            Add("Breath/Snort", l =>
            {
                l.model = SynthModel.Breath;
                l.breath = new BreathSettings { formantStart = 900f, formantEnd = 450f, sweepTime = 0.25f, sweepCurve = 0.7f, secondFormant = 2.2f, q = 4f, pinkMix = 0.4f, flutterRate = 35f, flutterDepth = 0.7f };
                l.amp = new SynthEnvelope { attack = 0.01f, hold = 0.05f, decay = 0.3f, sustain = 0f, release = 0.05f };
            });
            Add("Breath/Sigh", l =>
            {
                l.model = SynthModel.Breath;
                l.breath = new BreathSettings { formantStart = 1100f, formantEnd = 500f, sweepTime = 0.8f, sweepCurve = 1f, secondFormant = 2.6f, q = 2f, pinkMix = 0.7f };
                l.amp = new SynthEnvelope { attack = 0.15f, hold = 0.1f, decay = 0.8f, sustain = 0f, release = 0.1f };
            });
        }
    }
}
