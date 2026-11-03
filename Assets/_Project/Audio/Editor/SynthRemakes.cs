using System;
using System.Collections.Generic;
using System.Globalization;
using static Diorama.Audio.EditorTools.Syn;
using static Diorama.Audio.SynthWave;
using static Diorama.Audio.SynthConfidence;

namespace Diorama.Audio.EditorTools
{
    /// <summary>
    /// Step B remakes on top of batch 1: P0 and P1 sounds rebuilt from the physical models, plus the new cues from SoundList.
    /// Each sound = transient (2-5 kHz) + body (300-1500 Hz) + tail / texture + optional sparkle; sub only as a quiet extra.
    /// Magic set = crystal / glass modes + detuned FM shimmer in D major pentatonic + soft air: warm, never sci-fi.
    /// Confidence (synth-good / ok / weak) is set after reading each group's AudioScope sheet.
    /// </summary>
    public static partial class SynthRemakes
    {
        public const string Set = "batch2";

        static List<SynthRecipes.Recipe> _list;

        /// <summary>Replaces batch 1 recipes by id (keeping their cue settings and extra clips) and adds the new cues.</summary>
        public static void Overlay(List<SynthRecipes.Recipe> list)
        {
            _list = list;
            Opening();
            Hands();
            Walk();
            Battle();
            Cinderwood();
            Supporting();
            Ambience();
            AudioD();
            Step14();
            _list = null;
        }

        static SynthRecipes.Recipe Re(string id, SynthConfidence conf, Action<SynthPatch> build)
        {
            var r = _list.Find(x => x.id == id);
            if (r == null) throw new ArgumentException($"[AudioLab] Remake of unknown cue '{id}'.");
            r.build = build;
            r.set = Set;
            r.confidence = conf;
            return r;
        }

        static SynthRecipes.Recipe New(string group, string id, SynthConfidence conf, Action<AudioCue> cue, Action<SynthPatch> build)
        {
            var r = new SynthRecipes.Recipe { set = Set, group = group, id = id, build = build, cue = cue, confidence = conf };
            _list.Add(r);
            return r;
        }

        // ---- building blocks ----

        /// <summary>A model layer from a preset at a pitch (Hz).</summary>
        static SynthLayer M(string preset, float hz = 440f) => ModelPresets.Layer(preset, hz);

        /// <summary>A model layer from a preset at a note (modal: the first mode).</summary>
        static SynthLayer Mn(string preset, string note)
        {
            var l = ModelPresets.Layer(preset);
            l.note = note;
            return l;
        }

        static SynthLayer Do(this SynthLayer l, Action<SynthLayer> a)
        {
            a(l);
            return l;
        }

        static SynthLayer Scat(this SynthLayer l, int count, float window, float pitch = 2f, float gainJitter = 0.5f, float bunching = 1f)
        {
            l.scatter = new ScatterSettings { count = count, window = window, pitchJitter = pitch, gainJitter = gainJitter, bunching = bunching };
            return l;
        }

        /// <summary>Wind band swept by the pitch envelope: the resonant whoosh.</summary>
        static SynthLayer Whoosh(float fromSemis, float toSemis, float time, float attack = 0.06f, float decay = 0.25f, float hz = 440f) =>
            M("Wind/Whoosh", hz).Pitch(fromSemis, toSemis, time, 0.8f).Env(attack, decay);

        static SynthLayer Puff(float from, float to, float time, float attack = 0.012f, float decay = 0.22f) =>
            M("Breath/Puff").Do(l =>
            {
                l.breath.formantStart = from;
                l.breath.formantEnd = to;
                l.breath.sweepTime = time;
            }).Env(attack, decay);

        /// <summary>Detuned FM partial: two of these a few cents apart make the warm shimmer.</summary>
        static SynthLayer Shimmer(string note, float attack, float decay, float detuneCents = 0f) =>
            L(Sine).Note(note).Fm(2f, 0.5f, decay * 0.6f).Env(attack, decay).Vib(5.3f, 0.05f, 0.1f).Pitch(detuneCents / 100f, detuneCents / 100f, 0f);

        /// <summary>Crystal bar as an instrument for sparkle notes.</summary>
        static SynthLayer Sparkle(float decay = 0.6f) => Mn("Modal/Crystal ting", "D7").Do(l => l.modal.decayScale = decay).Inst();

        static SynthLayer GlassI(float decay = 0.6f) => Mn("Modal/Glass clink", "A6").Do(l => l.modal.decayScale = decay).Inst();

        static SynthLayer Sub(float hz, float decay, float gain) => L(Sine, hz).Pitch(4f, 0f, 0.04f, 0.6f).Env(0.003f, decay).Gain(gain);

        static string N(string note, float start, float length, int layer, float velocity = 1f) =>
            string.Format(CultureInfo.InvariantCulture, "{0}@{1:0.###}:{2:0.###}/{3}*{4:0.##}", note, start, length, layer, velocity);

        /// <summary>Notes rising through D major pentatonic from a degree (0 = D4, 5 = D5, 10 = D6).</summary>
        static IEnumerable<string> Run(int fromDegree, int count, float start, float step, float length, int layer, float velFrom, float velTo)
        {
            for (int i = 0; i < count; i++)
            {
                float u = count > 1 ? i / (float)(count - 1) : 0f;
                yield return N(SynthNotes.Name(SynthNotes.Degree(fromDegree + i)), start + i * step, length, layer, velFrom + (velTo - velFrom) * u);
            }
        }

        static IEnumerable<string> RunDown(int fromDegree, int count, float start, float step, float length, int layer, float velFrom, float velTo)
        {
            for (int i = 0; i < count; i++)
            {
                float u = count > 1 ? i / (float)(count - 1) : 0f;
                yield return N(SynthNotes.Name(SynthNotes.Degree(fromDegree - i)), start + i * step, length, layer, velFrom + (velTo - velFrom) * u);
            }
        }

        static IEnumerable<string> Concat(params IEnumerable<string>[] parts)
        {
            foreach (var p in parts)
                foreach (var s in p) yield return s;
        }

        // ---- P0: opening and magic ----

        static void Opening()
        {
            Re("shard.hum", Good, p => p.Loop(4f, 1f,
                    L(Sine).Note("A4").Fm(1.5f, 0.6f).Env(0.2f, 1f, 1f),
                    L(Sine).Note("E5").Env(0.2f, 1f, 1f).Trem(4f, 0.3f).Gain(0.3f),
                    L(Sine).Note("A5").Vib(5f, 0.08f).Env(0.2f, 1f, 1f).Gain(0.25f),
                    L(Sine).Note("A5").Pitch(0.06f, 0.06f, 0f).Env(0.2f, 1f, 1f).Gain(0.2f),
                    M("Wind/Breeze", 1600f).Gain(0.12f),
                    Sparkle(0.8f))
                .Notes(N("A6", 0.7f, 0.2f, 5, 0.25f), N("D7", 2.3f, 0.2f, 5, 0.2f), N("B6", 3.4f, 0.2f, 5, 0.18f)));

            New("Opening", "shard.touch", Good, SynthBatch1.Sfx(0.8f, 40, 2), p => p.One(1.3f,
                    Mn("Modal/Glass clink", "A6").Gain(0.7f),
                    Shimmer("D6", 0.02f, 0.9f).Gain(0.5f),
                    Shimmer("D6", 0.02f, 0.9f, 7f).Gain(0.4f),
                    M("Breath/Sigh", 1100f).Env(0.05f, 0.6f).Gain(0.25f),
                    Sparkle())
                .Notes(Run(12, 4, 0.06f, 0.07f, 0.3f, 4, 0.6f, 0.35f)).Vars(3, 2f, 0.05f, 0.1f));

            Re("shard.crack", Good, p => p.One(1f,
                    Mn("Modal/Stone clack", "F#6").Gain(0.9f),
                    Mn("Modal/Glass clink", "B6").Gain(0.6f),
                    M("Particles/Dust fall", 700f).Env(0.002f, 0.2f).Gain(0.5f),
                    Shimmer("D6", 0.04f, 0.7f).Gain(0.45f),
                    Shimmer("A6", 0.05f, 0.6f, 6f).Gain(0.3f),
                    Sparkle(0.7f))
                .Notes(N("D7", 0.03f, 0.1f, 5, 0.7f), N("A6", 0.07f, 0.15f, 5, 0.6f), N("E6", 0.11f, 0.2f, 5, 0.5f), N("D6", 0.15f, 0.5f, 5, 0.4f)));

            Re("island.unfold", Ok, p => p.One(2.6f,
                    M("Particles/Big rocks", 360f).Scat(9, 1.8f, 3f, 0.6f, 0.7f).Gain(0.6f),
                    M("Particles/Little rocks", 500f).Scat(14, 1.9f, 4f, 0.6f, 0.7f).Gain(0.45f),
                    M("Particles/Dust fall").Env(0.6f, 1.2f).Gain(0.35f),
                    L(SoftSaw).Note("D4").LP(500f, 1600f, 2f).Env(0.8f, 0.8f, 0f, 0.05f, 0.6f).Gain(0.35f),
                    L(Sine).Note("F#4").Env(0.9f, 0.9f, 0f, 0.05f, 0.6f).Gain(0.4f),
                    L(Sine).Note("D3").Pitch(-5f, 0f, 1.6f).Env(0.8f, 0.8f, 0f, 0.05f, 0.6f).Gain(0.12f),
                    Shimmer("D5", 0.01f, 0.9f).At(1.6f).Gain(0.4f),
                    Shimmer("A5", 0.01f, 0.8f, 6f).At(1.65f).Gain(0.3f)));

            New("Opening", "logo.rise", Ok, SynthBatch1.Sfx(0.8f, 30, 1, 0f, 3f), p => p.One(2.4f,
                    M("Friction/Wood creak", 520f).Do(l =>
                    {
                        l.friction.body = ModalMaterial.Stone;
                        l.friction.rateStart = 40f;
                        l.friction.rateEnd = 80f;
                        l.friction.rateTime = 1.6f;
                        l.friction.irregularity = 0.6f;
                        l.friction.scrape = 0.4f;
                    }).Env(0.2f, 0.4f, 0f, 0.05f, 1.4f).Gain(0.45f),
                    M("Particles/Little rocks", 600f).Scat(6, 1.4f, 4f).Gain(0.3f),
                    L(Sine).Note("A4").Pitch(-12f, 0f, 1.6f, 0.7f).Fm(2f, 0.4f).Env(0.6f, 1f, 0f, 0.05f, 1f).Gain(0.4f),
                    Shimmer("D5", 0.8f, 1.2f).Gain(0.35f),
                    Shimmer("D5", 0.8f, 1.2f, 7f).Gain(0.3f),
                    Sparkle())
                .Notes(Run(10, 5, 1.45f, 0.09f, 0.4f, 5, 0.4f, 0.6f)));

            New("Pods", "pod.wiggle", Ok, SynthBatch1.Sfx(0.8f, 110, 2), p => p.One(0.22f,
                    Mn("Modal/Clay pot tok", "A5").Gain(0.7f),
                    Mn("Modal/Clay pot tok", "B5").At(0.07f).Gain(0.55f),
                    Mn("Modal/Glass clink", "D7").Do(l => l.modal.decayScale = 0.25f).Gain(0.2f)).Vars(3, 1f)).Tier(LoudnessTier.UI);

            New("UI", "ui.lesson", Good, SynthBatch1.Ui(0.8f), p => p.Seq(1f,
                    new[] { L(Sine).Note("A5").Env(0.003f, 0.35f).Fm(4f, 1.5f, 0.03f), Mn("Modal/Crystal ting", "D6").Do(l => l.modal.decayScale = 0.6f) },
                    "A5@0:0.2/0", "D6@0.1:0.6/1", "F#6@0.1:0.6/1*0.35"));
        }

        // ---- P0: hands, island, runes ----

        static void Hands()
        {
            Re("island.grab", Ok, p => p.One(0.35f,
                    M("Friction/Wood creak", 700f).Do(l =>
                    {
                        l.friction.body = ModalMaterial.Stone;
                        l.friction.rateStart = 70f;
                        l.friction.rateEnd = 95f;
                        l.friction.rateTime = 0.25f;
                        l.friction.pressure = 0.4f;
                        l.friction.scrape = 0.6f;
                    }).Env(0.01f, 0.22f).Gain(0.7f),
                    M("Particles/Sand paper").Env(0.01f, 0.18f).Gain(0.35f),
                    Mn("Modal/Stone clack", "E5").Gain(0.3f)).Vars(3, 1f, 0.1f, 0.15f));

            Re("island.release", Ok, p => p.One(0.35f,
                    Mn("Modal/Stone clack", "B4").Do(l => l.modal.exciterMs = 2f).Gain(0.8f),
                    Mn("Modal/Clay pot tok", "D4").Gain(0.45f),
                    M("Particles/Little rocks", 330f).Gain(0.4f),
                    Sub(110f, 0.1f, 0.12f)).Vars(2, 0.8f));

            New("UI", "rune.hold", Good, SynthBatch1.Ui(0.8f, true).And(c => c.notes = "Play with a handle when the hold starts; Stop(0.1) if released early."), p => p.One(2.4f,
                    L(Sine).Note("D5").Pitch(0f, 12f, 2f).Fm(2f, 0.4f).Env(0.15f, 2f, 1f, 0.15f).Gate(2.05f),
                    L(Sine).Note("A5").Pitch(0f, 12f, 2f).Env(0.3f, 2f, 1f, 0.15f).Gate(2.05f).Trem(8f, 0.25f).Gain(0.35f),
                    Sparkle(0.5f),
                    Mn("Modal/Crystal ting", "D6").At(2.02f).Gain(0.7f))
                .Notes(Run(5, 6, 0.3f, 0.32f, 0.3f, 2, 0.25f, 0.55f)));

            New("UI", "menu.open", Ok, SynthBatch1.Ui(0.8f), p => p.One(0.45f,
                    Puff(1000f, 420f, 0.25f, 0.02f, 0.3f).Gain(0.8f),
                    Mn("Modal/Clay pot tok", "E4").Gain(0.4f),
                    L(Sine).Note("A4").Pitch(0f, -5f, 0.25f).Env(0.01f, 0.25f).Gain(0.25f)));

            New("UI", "menu.close", Ok, SynthBatch1.Ui(0.8f), p => p.One(0.45f,
                    Puff(420f, 1000f, 0.25f, 0.03f, 0.3f).Gain(0.8f),
                    Mn("Modal/Clay pot tok", "A4").At(0.05f).Gain(0.35f),
                    L(Sine).Note("D5").Pitch(-5f, 0f, 0.25f).Env(0.03f, 0.25f).Gain(0.25f)));

            New("UI", "path.gold", Good, SynthBatch1.Ui(0.8f, true), p => p.One(1.1f,
                    Whoosh(-3f, 4f, 0.8f, 0.15f, 0.6f, 900f).Gain(0.15f),
                    Sparkle(0.5f))
                .Notes(Run(10, 8, 0.02f, 0.085f, 0.25f, 1, 0.3f, 0.55f)));
        }

        // ---- P0: walk and encounter ----

        static void Walk()
        {
            // Toy-scale synth steps (lighter, higher, shorter); the Kenney steps stay in the same cues to compare.
            Re("step.grass", Ok, p => p.One(0.16f,
                    M("Particles/Crunch (grass step)", 330f).Do(l => l.particles.density = 2500f).LP(5000f).Gain(1f),
                    M("Particles/Leaves", 330f).Env(0.005f, 0.08f).Gain(0.25f),
                    Mn("Modal/Wood knock", "B5").Do(l => l.modal.exciterMs = 2f).Gain(0.12f)).Vars(6, 1.5f, 0.15f, 0.2f));
            Re("step.path", Ok, p => p.One(0.15f,
                    M("Particles/Gravel step", 520f),
                    Mn("Modal/Stone clack", "D6").Do(l => l.modal.exciterMs = 1.5f).Gain(0.25f)).Vars(6, 1.5f, 0.15f, 0.2f));
            Re("step.stone", Ok, p => p.One(0.13f,
                    Mn("Modal/Stone clack", "D6").Do(l => l.modal.exciterMs = 1f).Gain(0.8f),
                    M("Particles/Little rocks", 600f).Gain(0.25f)).Vars(6, 2f, 0.1f, 0.15f));
            Re("step.wood", Ok, p => p.One(0.14f,
                    Mn("Modal/Wood knock", "A5").Do(l => l.modal.exciterMs = 2.5f).Gain(0.8f),
                    M("Particles/Sticks", 500f).Gain(0.25f)).Vars(6, 1.5f, 0.1f, 0.15f));

            Re("grass.rustle", Weak, p => p.One(0.35f,
                    M("Particles/Leaves", 300f).Do(l =>
                    {
                        l.particles.density = 1400f;
                        l.particles.systemDecay = 0.3f;
                        l.particles.gainJitter = 0.4f;
                    }).LP(6000f).Env(0.02f, 0.25f),
                    M("Particles/Crunch (grass step)", 330f).Gain(0.25f)).Vars(3, 1f, 0.15f, 0.2f));

            Re("enc.rustle", Weak, p => p.One(0.95f,
                    M("Particles/Leaves", 330f).Do(l =>
                    {
                        l.particles.density = 3000f;
                        l.particles.gainJitter = 0.3f;
                    }).LP(6000f).Env(0.04f, 0.7f).Trem(12f, 0.6f),
                    M("Particles/Sticks", 300f).Scat(4, 0.6f, 3f).Gain(0.3f),
                    M("Particles/Crunch (grass step)", 420f).Scat(3, 0.5f).Gain(0.2f)));

            Re("zoom.in", Good, p => p.One(0.6f,
                    Whoosh(-7f, 5f, 0.45f, 0.15f, 0.3f),
                    L(Sine).Note("A4").Pitch(-7f, 5f, 0.45f).Env(0.1f, 0.3f).Gain(0.12f)));
            Re("zoom.out", Good, p => p.One(0.6f,
                    Whoosh(5f, -7f, 0.45f, 0.15f, 0.3f),
                    L(Sine).Note("A4").Pitch(5f, -7f, 0.45f).Env(0.1f, 0.3f).Gain(0.12f)));

            Re("clearing.rumble", Ok, p => p.One(1.4f,
                    M("Particles/Leaves").Env(0.2f, 1f).Gain(0.6f),
                    M("Particles/Big rocks", 330f).Scat(6, 1.1f, 3f).Gain(0.5f),
                    Mn("Modal/Wood thud (log)", "D4").Scat(3, 0.9f, 3f).Gain(0.4f),
                    L(PinkNoise).LP(300f, 120f, 1.2f).Env(0.15f, 1f).Gain(0.2f)));

            Re("pads.rise", Ok, p => p.One(0.45f,
                    Mn("Modal/Wood thud (log)", "E4"),
                    M("Particles/Little rocks", 500f).Gain(0.45f),
                    M("Particles/Dust fall").Env(0.005f, 0.2f).Gain(0.25f)).Vars(2, 1f));
        }

        // ---- P0: battle and catch ----

        static void Battle()
        {
            Re("pod.grab", Good, p => p.One(0.18f,
                    Mn("Modal/Glass clink", "B6").Do(l => l.modal.decayScale = 0.2f),
                    Mn("Modal/Clay pot tok", "D6").Gain(0.3f)).Vars(3, 1f));

            Re("pod.throw", Good, p => p.One(0.45f,
                    Whoosh(-5f, 7f, 0.3f, 0.04f, 0.3f, 520f),
                    Puff(700f, 1600f, 0.2f, 0.02f, 0.2f).Gain(0.3f)).Vars(3, 0.5f, 0.1f, 0.15f));

            Re("pod.open", Good, p => p.One(1f,
                    Mn("Modal/Clay pot tok", "A5").Gain(0.6f),
                    M("Bubbles/Steam pop", 700f).Gain(0.5f),
                    Puff(2000f, 900f, 0.05f, 0.003f, 0.06f).Gain(0.3f),
                    Sparkle(0.6f))
                .Notes("A6@0.05:0.2/3*0.7", "B6@0.1:0.2/3*0.6", "D7@0.15:0.4/3*0.6", "A6@0.24:0.4/3*0.4"));

            Re("pod.return", Good, p => p.One(0.42f,
                    L(Sine).Note("D5").Pitch(-12f, 12f, 0.3f, 1.5f).Env(0.02f, 0.3f).Trem(30f, 0.5f),
                    Whoosh(-7f, 7f, 0.3f, 0.02f, 0.25f, 600f).Gain(0.6f)));

            Re("move.charge", Good, p => p.One(0.85f,
                    L(Sine).Note("A4").Pitch(-12f, 7f, 0.8f, 1.4f).Fm(2f, 0.6f).Env(0.3f, 0.2f, 0f, 0.05f, 0.3f).Vib(6f, 0.3f),
                    L(SoftSaw).Note("A4").Pitch(-12f, 7f, 0.8f, 1.4f).Filt(SynthFilter.LowPass, 800f, 4000f, 0.8f, 0.2f).Env(0.3f, 0.2f, 0f, 0.05f, 0.3f).Gain(0.35f),
                    Whoosh(-12f, 7f, 0.8f, 0.4f, 0.3f, 500f).Gain(0.5f)));

            Re("move.shoot.fire", Good, p => p.One(0.45f,
                    M("Fire/Whoomph").Env(0.01f, 0.3f),
                    Whoosh(-3f, 5f, 0.25f, 0.02f, 0.25f, 600f).Gain(0.5f)).Vars(2, 0.6f));
            Re("move.shoot.water", Ok, p => p.One(0.4f,
                    L(Sine, 600f).Pitch(-5f, 12f, 0.12f, 0.6f).Env(0.005f, 0.15f).Gain(0.5f),
                    M("Bubbles/Splash").Env(0.003f, 0.3f).Gain(0.8f),
                    Whoosh(-3f, 5f, 0.2f, 0.01f, 0.2f, 700f).Gain(0.4f)).Vars(2, 0.6f));
            Re("move.shoot.wind", Good, p => p.One(0.45f,
                    M("Wind/Gust").Env(0.02f, 0.3f),
                    Whoosh(-5f, 9f, 0.3f, 0.02f, 0.25f).Gain(0.6f)).Vars(2, 0.5f, 0.1f, 0.2f));
            Re("move.shoot.neutral", Good, p => p.One(0.32f,
                    L(Triangle).Note("E5").Pitch(12f, -5f, 0.15f).Env(0.004f, 0.15f).Gain(0.6f),
                    Mn("Modal/Wood knock", "E5"),
                    Whoosh(0f, 5f, 0.2f, 0.01f, 0.2f).Gain(0.4f)).Vars(2, 0.6f));

            Re("move.impact.fire", Good, p => p.One(0.6f,
                    M("Fire/Whoomph").Env(0.003f, 0.35f),
                    Mn("Modal/Stone clack", "F#5").Gain(0.4f),
                    Mn("Modal/Clay pot tok", "F#4").Gain(0.5f),
                    M("Particles/Little rocks", 880f).Scat(5, 0.25f, 4f).Gain(0.2f),
                    Sub(150f, 0.12f, 0.12f)).Vars(2, 0.5f).Master(2.5f));
            Re("move.impact.water", Ok, p => p.One(0.5f,
                    M("Bubbles/Splash").Do(l => l.bubbles.atStart = 10).Env(0.003f, 0.3f),
                    Puff(1500f, 700f, 0.2f, 0.003f, 0.25f).Gain(0.6f),
                    Mn("Modal/Clay pot tok", "B4").Gain(0.5f)).Vars(2, 0.5f));
            Re("move.impact.wind", Ok, p => p.One(0.5f,
                    M("Wind/Gust").Env(0.01f, 0.35f),
                    Puff(1200f, 500f, 0.25f, 0.005f, 0.3f).Gain(0.5f),
                    Mn("Modal/Wood knock", "E5").Gain(0.35f)).Vars(2, 0.5f, 0.1f, 0.2f));
            Re("move.impact.neutral", Good, p => p.One(0.35f,
                    Mn("Modal/Wood thud (log)", "E4"),
                    Mn("Modal/Stone clack", "A5").Gain(0.2f),
                    Puff(1200f, 600f, 0.1f, 0.003f, 0.12f).Gain(0.4f),
                    M("Particles/Dust fall").Env(0.003f, 0.15f).Gain(0.25f),
                    Sub(110f, 0.12f, 0.08f)).Vars(2, 0.5f));

            Re("move.lunge", Good, p => p.One(0.35f,
                    Whoosh(-7f, 5f, 0.25f, 0.04f, 0.22f),
                    Puff(600f, 1400f, 0.15f, 0.02f, 0.18f).Gain(0.35f)).Vars(2, 0.5f, 0.1f, 0.15f));
            Re("move.slam", Ok, p => p.One(0.7f,
                    Mn("Modal/Wood thud (log)", "E4").Do(l => l.modal.decayScale = 2f),
                    Mn("Modal/Stone clack", "D5").Gain(0.35f),
                    Puff(1000f, 500f, 0.15f, 0.003f, 0.2f).Gain(0.4f),
                    M("Particles/Big rocks", 440f).Scat(5, 0.3f, 3f, 0.6f, 1.5f).Gain(0.25f),
                    M("Particles/Dust fall").At(0.03f).Env(0.01f, 0.3f).Gain(0.25f),
                    Sub(80f, 0.2f, 0.1f)).Master(2.5f));
            Re("move.shield.pop", Good, p => p.One(0.6f,
                    Mn("Modal/Glass clink", "B6").Do(l => l.modal.decayScale = 0.3f).Gain(0.6f),
                    M("Bubbles/Steam pop", 600f).Gain(0.6f),
                    GlassI(0.5f))
                .Notes("B6@0.02:0.1/2*0.7", "A6@0.06:0.1/2*0.6", "D6@0.1:0.3/2*0.5"));

            Re("hit.normal", Good, p => p.One(0.25f,
                    Mn("Modal/Clay pot tok", "B4").Do(l => l.modal.exciterMs = 1.5f),
                    Mn("Modal/Stone clack", "C#6").Gain(0.4f),
                    Puff(1500f, 800f, 0.08f, 0.003f, 0.08f).Gain(0.3f),
                    Sub(140f, 0.08f, 0.12f)).Vars(3, 1f));
            Re("hit.super", Good, p => p.One(0.6f,
                    Mn("Modal/Clay pot tok", "D5"),
                    Mn("Modal/Stone clack", "F#6").Gain(0.5f),
                    Puff(2500f, 1200f, 0.08f, 0.003f, 0.1f).Gain(0.3f),
                    GlassI(0.5f))
                .Notes("A6@0.03:0.15/3*0.6", "D7@0.08:0.3/3*0.6", "B6@0.14:0.3/3*0.4").Vars(2, 0.5f));
            Re("hit.weak", Ok, p => p.One(0.2f,
                    Mn("Modal/Clay pot tok", "A4").Do(l =>
                    {
                        l.modal.exciterMs = 3f;
                        l.modal.damping = 0.5f;
                    }),
                    M("Particles/Dust fall").Env(0.003f, 0.08f).Gain(0.2f)).Vars(2, 0.5f).Master(1f, 3500f));
            Re("faint", Good, p => p.One(1.1f,
                    L(Triangle).Note("A4").Pitch(0f, -12f, 0.6f).Env(0.01f, 0.6f, 0f, 0.1f, 0.1f).Vib(5f, 0.3f, 0.1f),
                    L(Sine).Note("D4").Pitch(0f, -12f, 0.6f).Env(0.01f, 0.6f, 0f, 0.1f, 0.1f).Gain(0.3f),
                    Mn("Modal/Clay pot tok", "D4").Do(l => l.modal.exciterMs = 6f).At(0.72f).Gain(0.6f),
                    M("Particles/Little rocks", 400f).At(0.72f).Gain(0.2f)));

            Re("catch.hit", Good, p => p.One(0.4f,
                    Mn("Modal/Glass clink", "B6").Do(l => l.modal.decayScale = 0.4f),
                    Mn("Modal/Clay pot tok", "A5").Gain(0.5f),
                    Puff(2500f, 1500f, 0.04f, 0.002f, 0.04f).Gain(0.2f)));
            Re("catch.wobble", Good, p => p.One(0.16f,
                    Mn("Modal/Wood knock", "A5").Do(l => l.modal.exciterMs = 1f),
                    Mn("Modal/Clay pot tok", "A4").Gain(0.45f)).Vars(3, 1.5f));
            Re("catch.success", Good, p => p.One(1.4f,
                    Mn("Modal/Wood knock", "E6").Do(l => l.modal.exciterMs = 0.6f).Gain(0.8f),
                    L(Sine).Note("D5").Env(0.004f, 1f).Fm(3.5f, 2f, 0.25f).Gain(0.45f),
                    Sparkle(0.8f))
                .Notes("D6@0.06:0.2/2*0.7", "F#6@0.12:0.2/2*0.7", "A6@0.18:0.2/2*0.7", "D7@0.24:0.7/2*0.8"));
            Re("catch.shatter", Ok, p => p.One(0.7f,
                    Mn("Modal/Glass clink", "A6").Do(l => l.modal.decayScale = 0.35f).Scat(6, 0.15f, 5f, 0.6f, 1.5f).Gain(0.5f),
                    M("Particles/Little rocks", 1000f).Gain(0.4f),
                    GlassI(0.4f))
                .Notes("B6@0.01:0.1/2*0.6", "F#6@0.07:0.1/2*0.5", "D6@0.13:0.3/2*0.4"));
            Re("catch.toRim", Good, p => p.One(0.8f,
                    Whoosh(5f, -5f, 0.3f, 0.03f, 0.3f),
                    Mn("Modal/Glass clink", "E6").Do(l => l.modal.decayScale = 0.4f).At(0.32f),
                    Mn("Modal/Wood knock", "B6").At(0.32f).Gain(0.4f)));
        }

        // ---- P0: Cinderwood story beats ----

        static void Cinderwood()
        {
            New("Pods", "gift.pods", Good, SynthBatch1.Sfx(0.8f, 40, 1), p => p.One(1.5f,
                    Whoosh(-5f, 5f, 0.4f, 0.08f, 0.4f, 700f),
                    Whoosh(-3f, 7f, 0.4f, 0.08f, 0.4f, 800f).At(0.25f).Gain(0.8f),
                    Mn("Modal/Glass clink", "E6").Do(l => l.modal.decayScale = 0.4f).At(0.75f),
                    Mn("Modal/Glass clink", "A6").Do(l => l.modal.decayScale = 0.4f).At(0.98f),
                    Sparkle(0.5f))
                .Notes(Run(10, 5, 0.05f, 0.1f, 0.2f, 4, 0.3f, 0.45f)));

            Re("scene.sink", Ok, p => p.One(1.5f,
                    M("Particles/Big rocks", 400f).Scat(10, 1.1f, 3f),
                    M("Particles/Dust fall").Env(0.1f, 1.1f).Gain(0.4f),
                    Whoosh(3f, -9f, 1.2f, 0.1f, 1f, 500f).Gain(0.45f),
                    L(PinkNoise).LP(300f).Env(0.1f, 1f).Gain(0.1f)));
            Re("scene.rise", Ok, p => p.One(1.4f,
                    M("Particles/Little rocks", 500f).Scat(12, 1f, 3f, 0.5f, 0.7f),
                    M("Particles/Dust fall").Env(0.4f, 0.8f).Gain(0.35f),
                    Whoosh(-9f, 3f, 1.1f, 0.4f, 0.8f, 500f).Gain(0.45f),
                    L(PinkNoise).LP(150f, 400f, 1f).Env(0.4f, 0.9f).Gain(0.15f)));
            Re("gate.enter", Good, p => p.One(1.2f,
                    Whoosh(-9f, 7f, 0.5f, 0.2f, 0.4f, 500f),
                    Shimmer("D5", 0.3f, 0.6f).Gain(0.3f),
                    Sparkle(0.6f))
                .Notes("D6@0.45:0.2/2*0.6", "A6@0.52:0.5/2*0.6", "D7@0.6:0.5/2*0.4"));
            Re("gate.hum", Good, p => p.Loop(4f, 1f,
                    L(Sine).Note("D4").Env(0.2f, 1f, 1f).Vib(0.25f, 0.1f),
                    L(Sine).Note("A4").Env(0.2f, 1f, 1f).Gain(0.5f),
                    L(Sine).Note("A4").Pitch(0.08f, 0.08f, 0f).Env(0.2f, 1f, 1f).Gain(0.35f),
                    L(Buzz).Note("D3").LP(600f).Env(0.2f, 1f, 1f).Trem(0.5f, 0.3f).Gain(0.12f),
                    M("Wind/Breeze", 1200f).Do(l => l.wind.gustRate = 1f).Gain(0.35f)));

            Re("log.lift", Ok, p => p.One(1.1f,
                    M("Friction/Wood creak", 330f).Do(l => l.friction.irregularity = 0.55f).Env(0.05f, 0.4f, 0f, 0.05f, 0.5f),
                    M("Particles/Leaves").At(0.1f).Env(0.05f, 0.5f).Gain(0.3f),
                    Mn("Modal/Wood thud (log)", "E4").At(0.6f),
                    M("Particles/Dust fall").At(0.6f).Env(0.01f, 0.35f).Gain(0.35f)));
            New("Puzzles", "log.drop", Ok, SynthBatch1.Sfx(0.8f, 40, 1), p => p.One(0.7f,
                    Mn("Modal/Wood thud (log)", "D4").Do(l => l.modal.decayScale = 1.5f),
                    Mn("Modal/Wood knock", "B4").Gain(0.6f),
                    M("Particles/Sticks", 380f).Scat(5, 0.2f, 3f, 0.5f, 1.5f).Gain(0.4f),
                    Sub(70f, 0.15f, 0.08f)));
            Re("bellows.pump", Ok, p => p.One(0.55f,
                    M("Friction/Leather squeak", 600f).Gain(0.45f),
                    Puff(450f, 1100f, 0.2f, 0.06f, 0.3f).At(0.04f)).Vars(3, 1f, 0.1f, 0.15f));
            New("Puzzles", "pipe.glow", Good, SynthBatch1.Sfx(0.8f, 60, 1, 0f, 2f), p => p.One(1.2f,
                    Whoosh(-9f, 5f, 1f, 0.3f, 0.6f, 500f),
                    L(Sine).Note("D5").Pitch(0f, 12f, 1f).Env(0.4f, 0.6f).Gain(0.2f),
                    M("Fire/Ember crackle").Env(0.3f, 0.6f).Gain(0.25f)));
            New("Puzzles", "brazier.warm", Ok, SynthBatch1.Sfx(0.8f, 60, 1), p => p.One(1.5f,
                    M("Fire/Ember crackle").Env(0.4f, 0.9f),
                    M("Breath/Sigh", 1800f).Do(l => l.breath.formantEnd = 1400f).Env(0.3f, 0.9f).Gain(0.3f),
                    M("Bubbles/Steam pop", 700f).Scat(4, 1f, 4f).Gain(0.35f)));
            Re("brazier.light", Good, p => p.One(0.9f,
                    M("Fire/Whoomph"),
                    Puff(400f, 1500f, 0.2f, 0.02f, 0.3f).Gain(0.5f),
                    M("Fire/Ember crackle").At(0.15f).Env(0.05f, 0.6f).Gain(0.4f)));
            Re("forge.door", Ok, p => p.One(2.4f,
                    M("Friction/Wood creak", 400f).Do(l =>
                    {
                        l.friction.body = ModalMaterial.Stone;
                        l.friction.rateStart = 25f;
                        l.friction.rateEnd = 45f;
                        l.friction.rateTime = 1.8f;
                        l.friction.irregularity = 0.65f;
                        l.friction.scrape = 0.6f;
                    }).Env(0.2f, 0.4f, 0f, 0.05f, 1.6f).Gain(0.8f),
                    M("Particles/Big rocks", 360f).Scat(6, 1.7f, 3f).Gain(0.4f),
                    M("Breath/Sigh", 2600f).At(0.6f).Env(0.2f, 1f).Gain(0.3f),
                    M("Particles/Little rocks", 900f).Scat(6, 1.5f, 5f).At(0.4f).Gain(0.25f),
                    Mn("Modal/Stone clack", "F4").Do(l => l.modal.exciterMs = 4f).At(1.95f),
                    L(PinkNoise).LP(250f).Env(0.3f, 1.4f).Gain(0.15f)));

            New("Keeper", "keeper.stomp", Weak, SynthBatch1.Sfx(0.8f, 20, 2, 0.03f, 3f), p => p.One(0.55f,
                    Mn("Modal/Stone clack", "D4").Do(l =>
                    {
                        l.modal.exciterMs = 6f;
                        l.modal.decayScale = 3f;
                    }),
                    Mn("Modal/Clay pot tok", "A3").Gain(0.6f),
                    M("Particles/Big rocks", 420f).Scat(6, 0.25f, 3f, 0.6f, 1.5f).Gain(0.45f),
                    M("Particles/Dust fall").At(0.04f).Env(0.01f, 0.3f).Gain(0.25f),
                    Sub(55f, 0.2f, 0.15f)).Vars(2, 1f));
            New("Keeper", "keeper.snort", Ok, SynthBatch1.Sfx(0.8f, 30, 1), p => p.One(0.7f,
                    M("Breath/Snort", 380f),
                    M("Breath/Puff", 500f).At(0.28f).Env(0.01f, 0.25f).Gain(0.5f),
                    M("Fire/Ember crackle").Env(0.05f, 0.5f).Gain(0.2f)).Vars(2, 1f, 0.1f, 0.1f));
            Re("keeper.crown.shield", Good, p => p.One(0.9f,
                    Mn("Modal/Brass clang", "A5").Do(l => l.modal.decayScale = 0.35f),
                    Mn("Modal/Glass clink", "D6").Do(l => l.modal.decayScale = 0.35f).Gain(0.5f),
                    Shimmer("D5", 0.01f, 0.4f).Gain(0.3f)));
            Re("keeper.crown.crack", Good, p => p.One(0.9f,
                    Mn("Modal/Stone clack", "B6").Gain(0.6f),
                    Mn("Modal/Glass clink", "F#6").Do(l => l.modal.decayScale = 0.4f).Gain(0.6f),
                    M("Particles/Little rocks", 900f).Gain(0.35f),
                    GlassI(0.5f))
                .Notes("B6@0.02:0.1/3*0.7", "F#6@0.07:0.15/3*0.6", "D6@0.12:0.4/3*0.5"));
            New("Keeper", "keeper.crown.break", Ok, SynthBatch1.Sfx(0.8f, 20, 1), p => p.One(1.3f,
                    Mn("Modal/Stone clack", "D7").Gain(0.6f),
                    Mn("Modal/Glass clink", "A6").Do(l => l.modal.decayScale = 0.4f).Scat(10, 0.3f, 7f, 0.6f, 1.6f).Gain(0.5f),
                    M("Particles/Little rocks", 1100f).Gain(0.4f),
                    Whoosh(5f, -5f, 0.6f, 0.02f, 0.5f, 900f).Gain(0.3f),
                    Sparkle(0.6f))
                .Notes(RunDown(15, 6, 0.05f, 0.08f, 0.3f, 4, 0.6f, 0.3f)));
            Re("keeper.fury", Ok, p => p.One(1.6f,
                    M("Fire/Fury").Env(0.15f, 1f),
                    M("Wind/Gust", 300f).Env(0.2f, 1f).Gain(0.5f),
                    Sub(55f, 1f, 0.12f).Vib(4f, 1f)));
            New("Keeper", "keeper.calm", Good, SynthBatch1.Sfx(0.8f, 20, 1, 0f, 3f), p => p.One(2.6f,
                    M("Fire/Ember crackle").Env(0.01f, 1.2f).Gain(0.5f),
                    M("Breath/Sigh").Env(0.1f, 1f).Gain(0.4f),
                    Shimmer("D5", 0.5f, 1.6f).Gain(0.35f),
                    Shimmer("F#5", 0.6f, 1.6f, 6f).Gain(0.3f),
                    Shimmer("A5", 0.7f, 1.6f, -5f).Gain(0.25f),
                    Sparkle(0.7f))
                .Notes(Run(10, 7, 0.5f, 0.22f, 0.3f, 5, 0.25f, 0.45f)));
            Re("restore.swell", Good, p => p.One(3.6f,
                    L(SoftSaw).Note("D4").Filt(SynthFilter.LowPass, 400f, 2200f, 1.8f, 0.15f).Env(1f, 1.5f, 0f, 0.05f, 0.5f).Gain(0.4f),
                    L(Sine).Note("A3").Env(1f, 1.5f, 0f, 0.05f, 0.5f).Fm(1f, 0.5f).Gain(0.5f),
                    L(Sine).Note("F#4").Env(1.1f, 1.5f, 0f, 0.05f, 0.5f).Vib(5f, 0.05f).Gain(0.6f),
                    L(Sine).Note("D5").At(1f).Env(0.01f, 2f).Fm(3.5f, 1.5f, 0.4f).Gain(0.5f),
                    M("Fire/Campfire").Env(1f, 1.8f).Gain(0.3f),
                    Sparkle(0.8f))
                .Notes(Run(10, 5, 1.05f, 0.12f, 0.4f, 5, 0.3f, 0.5f)));
            New("Opening", "shard.float", Good, SynthBatch1.Loop(0.8f), p => p.Loop(4f, 1f,
                    L(Sine).Note("D5").Fm(1.5f, 0.5f).Env(0.2f, 1f, 1f),
                    L(Sine).Note("A5").Env(0.2f, 1f, 1f).Trem(3f, 0.3f).Gain(0.35f),
                    L(Sine).Note("A5").Pitch(0.07f, 0.07f, 0f).Env(0.2f, 1f, 1f).Gain(0.25f),
                    M("Wind/Breeze", 1800f).Gain(0.1f),
                    Sparkle(0.7f))
                .Notes(N("D7", 0.5f, 0.2f, 4, 0.3f), N("A6", 1.6f, 0.2f, 4, 0.25f), N("E7", 2.7f, 0.2f, 4, 0.2f), N("B6", 3.5f, 0.2f, 4, 0.2f)));
            New("Opening", "shard.dock", Good, SynthBatch1.Sfx(0.8f, 20, 1), p => p.One(0.8f,
                    Mn("Modal/Stone clack", "A5"),
                    Mn("Modal/Brass clang", "E6").Do(l => l.modal.decayScale = 0.3f).Gain(0.4f),
                    Mn("Modal/Glass clink", "A6").Do(l => l.modal.decayScale = 0.4f).At(0.02f).Gain(0.5f),
                    Mn("Modal/Clay pot tok", "F#4").Gain(0.5f)));
            New("Opening", "statue.socket", Ok, SynthBatch1.Sfx(0.8f, 10, 1, 0f, 3f), p => p.One(3f,
                    Mn("Modal/Stone clack", "G4").Do(l => l.modal.exciterMs = 3f),
                    Mn("Modal/Clay pot tok", "C4").Gain(0.6f),
                    Sub(70f, 0.2f, 0.15f),
                    Shimmer("D4", 0.8f, 1.6f).At(0.2f).Gain(0.4f),
                    Shimmer("F#4", 0.9f, 1.6f, 6f).At(0.2f).Gain(0.35f),
                    Shimmer("A4", 1f, 1.6f, -5f).At(0.2f).Gain(0.3f),
                    Shimmer("D5", 1.1f, 1.6f).At(0.2f).Gain(0.25f),
                    M("Particles/Dust fall").Scat(8, 1.5f, 3f).At(0.3f).Gain(0.3f)));
        }

        // ---- P0: ambience beds (synth placeholders until the gen beds are picked) and sweeteners ----

        /// <summary>3D one-shot scattered on the island by AmbienceDirector: quiet, low priority, never steals.</summary>
        static Action<AudioCue> Sweet() => c =>
        {
            c.bus = AudioBus.Ambience;
            c.spatial = true;
            c.volume = SynthBatch1.CueVolume;
            c.volumeJitter = 0.2f;
            c.pitchMin = 0.96f;
            c.pitchMax = 1.04f;
            c.maxVoices = 3;
            c.minInterval = 0.1f;
            c.priority = 220;
            c.maxDistance = 1.5f;
        };

        static void Ambience()
        {
            const float A = 30f;
            Re("amb.stillisle", Ok, p => p.Loop(A, 3f,
                    M("Wind/Breeze", 520f),
                    M("Wind/Breeze", 1300f).Do(l =>
                    {
                        l.wind.leavesGain = 0.6f;
                        l.wind.gustRate = 0.15f;
                    }).Gain(0.35f)));
            Re("amb.cinderwood", Ok, p => p.Loop(A, 3f,
                    M("Wind/Breeze", 440f).Do(l => l.wind.gustDepth = 0.4f),
                    M("Particles/Leaves", 300f).Do(l => l.particles.density = 150f).LP(4000f).Trem(0.2f, 0.6f).Gain(0.15f),
                    M("Fire/Ember crackle").Do(l => l.fire.crackleRate = 4f).LP(3000f).Gain(0.15f)));
            Re("amb.cinderwood.restored", Ok, p => p.Loop(A, 3f,
                    M("Wind/Breeze", 520f),
                    M("Fire/Campfire").Do(l => l.fire.intensity = 0.45f).Gain(0.35f),
                    M("Particles/Leaves", 330f).Do(l => l.particles.density = 250f).LP(5000f).Gain(0.2f)));
            Re("amb.lodge", Ok, p => p.Loop(A, 3f,
                    M("Fire/Campfire").Do(l =>
                    {
                        l.fire.intensity = 0.45f;
                        l.fire.lapFreq = 520f;
                    }),
                    M("Wind/Breeze", 400f).Do(l => l.wind.q = 3f).Gain(0.2f)));
            Re("amb.pond", Ok, p => p.Loop(A, 3f,
                    M("Bubbles/Spring (steady)").Do(l => l.bubbles.rate = 6f).Gain(0.6f),
                    M("Wind/Breeze", 500f).Gain(0.6f)));
            New("Ambience", "amb.forgehall", Ok, SynthBatch1.Amb(SynthBatch1.CueVolume), p => p.Loop(A, 3f,
                    M("Fire/Campfire").Do(l =>
                    {
                        l.fire.intensity = 0.4f;
                        l.fire.crackleRate = 3f;
                        l.fire.hissGain = 0.05f;
                        l.fire.lapGain = 1f;
                        l.fire.lapFreq = 380f;
                        l.fire.lapRate = 0.8f;
                    }),
                    M("Wind/Breeze", 420f).Do(l => l.wind.q = 3f).Gain(0.3f),
                    M("Bubbles/Drip", 470f).Do(l =>
                    {
                        l.bubbles.atStart = 0;
                        l.bubbles.rate = 0.3f;
                        l.bubbles.decayScale = 4f;
                    }).Env(0.2f, 1f, 1f).Gain(0.5f)));
            New("Ambience", "amb.battle", Good, SynthBatch1.Amb(SynthBatch1.CueVolume), p => p.Loop(A, 3f,
                    M("Wind/Breeze", 600f).Do(l => l.wind.gustDepth = 0.5f)));

            New("Sweeteners", "sw.crackle", Good, Sweet(), p => p.One(0.16f,
                    M("Fire/Ember crackle").Do(l =>
                    {
                        l.fire.intensity = 1f;
                        l.fire.crackleRate = 150f;
                        l.fire.hissGain = 0.05f;
                        l.fire.lapGain = 0f;
                    }).Env(0.003f, 0.12f)).Vars(4, 2f, 0.2f).Master(2f));
            New("Sweeteners", "sw.drip", Good, Sweet(), p => p.One(0.15f, M("Bubbles/Drip", 600f).Do(l => l.bubbles.decayScale = 4f)).Vars(4, 3f));
            New("Sweeteners", "sw.gust", Good, Sweet(), p => p.One(2.4f, M("Wind/Gust", 500f).Env(0.6f, 1.2f)).Vars(3, 2f, 0.2f));
            New("Sweeteners", "sw.creak", Ok, Sweet(), p => p.One(0.8f, M("Friction/Wood creak", 300f).Env(0.05f, 0.5f)).Vars(3, 2f, 0.2f));
            New("Sweeteners", "sw.pebble", Good, Sweet(), p => p.One(0.25f,
                    Mn("Modal/Stone clack", "A6").Do(l => l.modal.exciterMs = 0.6f),
                    M("Particles/Little rocks", 700f).At(0.04f).Gain(0.4f)).Vars(4, 3f));
            // Organic sweeteners come from the gen batch: cue slots wait for its clips.
            foreach (var id in new[] { "sw.bird", "sw.leaves", "sw.insect" })
                New("Sweeteners", id, None, Sweet().And(c =>
                {
                    c.awaitingClips = true;
                    c.notes = "Awaiting the gen batch (Import Gen Batch).";
                }), null);
        }

        // ---- P1 ----

        static void Supporting()
        {
            Re("walk.stop", Good, p => p.One(0.1f, Mn("Modal/Wood knock", "F#6").Do(l => l.modal.exciterMs = 0.6f)).Vars(2, 1f));
            Re("walk.bump", Ok, p => p.One(0.3f,
                    Mn("Modal/Clay pot tok", "E4"),
                    Mn("Modal/Stone clack", "F5").Gain(0.4f),
                    M("Particles/Dust fall").Env(0.003f, 0.08f).Gain(0.2f),
                    L(Sine).Note("A4").Pitch(0f, -3f, 0.1f).Env(0.004f, 0.1f).Gain(0.2f)).Vars(2, 0.8f));
            Re("island.recenter", Good, p => p.One(0.7f,
                    Whoosh(-5f, 5f, 0.4f, 0.15f, 0.3f, 600f),
                    Mn("Modal/Crystal ting", "D6").Do(l => l.modal.decayScale = 0.4f).At(0.3f).Gain(0.2f)));
            Re("battle.run", Good, p => p.One(0.38f,
                    Whoosh(5f, -7f, 0.25f, 0.01f, 0.22f),
                    L(Sine).Note("D5").Pitch(0f, -5f, 0.2f).Env(0.005f, 0.15f).Gain(0.2f)));
            Re("shrine.flame", Good, p => p.Loop(6f, 1.5f, M("Fire/Campfire").Do(l => l.fire.intensity = 0.5f)));
            Re("shrine.flare", Good, p => p.One(0.85f,
                    M("Fire/Whoomph"),
                    Puff(400f, 1400f, 0.2f, 0.02f, 0.3f).Gain(0.4f)));
            Re("gate.sealed", Good, p => p.One(0.35f,
                    Mn("Modal/Clay pot tok", "A4").Do(l => l.modal.exciterMs = 3f),
                    L(Sine).Note("A4").Pitch(3f, -2f, 0.1f).Env(0.004f, 0.2f).Gain(0.4f)));
            Re("pond.tap", Good, p => p.One(0.12f, M("Bubbles/Drip", 520f).Do(l => l.bubbles.decayScale = 5f)).Vars(3, 2f));
            Re("pond.splash", Ok, p => p.One(0.75f,
                    M("Bubbles/Splash").Env(0.003f, 0.5f),
                    Puff(1500f, 700f, 0.3f, 0.005f, 0.35f).Gain(0.6f),
                    M("Particles/Water drops").At(0.08f).Gain(0.5f)));
            Re("lantern.sputter", Ok, p => p.One(0.7f,
                    M("Fire/Ember crackle").Do(l =>
                    {
                        l.fire.intensity = 1f;
                        l.fire.crackleRate = 80f;
                        l.fire.crackleGain = 0.6f;
                        l.fire.lapGain = 0.6f;
                    }).Env(0.01f, 0.5f),
                    M("Breath/Puff").Do(l =>
                    {
                        l.breath.flutterRate = 12f;
                        l.breath.flutterDepth = 0.8f;
                    }).Env(0.02f, 0.4f).Gain(0.4f)).Vars(2, 0f, 0.15f));
            Re("ember.crackle", Good, p => p.Loop(8f, 2f, M("Fire/Ember crackle")));
            Re("steam.hiss", Ok, p => p.Loop(6f, 2f,
                    M("Breath/Sigh", 2600f).Do(l =>
                    {
                        l.breath.formantStart = 3000f;
                        l.breath.formantEnd = 3000f;
                        l.breath.q = 1.5f;
                        l.breath.flutterRate = 4f;
                        l.breath.flutterDepth = 0.4f;
                    }).Env(0.2f, 1f, 1f),
                    M("Bubbles/Spring (steady)").Do(l => l.bubbles.rate = 4f).Gain(0.5f)));
            New("UI", "ui.pebble", Good, SynthBatch1.Ui(0.8f, true), p => p.One(0.12f,
                    Mn("Modal/Stone clack", "F#6").Do(l => l.modal.exciterMs = 0.5f)).Vars(2, 1f));
            Re("spirit.beam", Good, p => p.Loop(4f, 1f,
                    L(Sine).Note("A4").Env(0.2f, 1f, 1f).Vib(0.25f, 0.05f),
                    L(Sine).Note("A5").Env(0.2f, 1f, 1f).Gain(0.4f),
                    L(Buzz).Note("A4").LP(1200f).Env(0.2f, 1f, 1f).Trem(3f, 0.2f).Gain(0.15f)));
        }
    }
}
