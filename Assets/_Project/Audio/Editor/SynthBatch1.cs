using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using static Diorama.Audio.EditorTools.Syn;
using static Diorama.Audio.SynthWave;

namespace Diorama.Audio.EditorTools
{
    /// <summary>
    /// Sound batch 1: every game cue from the audio brief. Style: cozy toy, small and close. Soft attacks, little above
    /// ~9 kHz, short tails; wood, glass, bell, kalimba, marimba, soft air. Tonal sounds and jingles use D major pentatonic.
    /// Loudness hierarchy lives in the loudness tiers (bake-time levelling); every cue plays at CueVolume (step B).
    /// Loop rates (tremolo, vibrato) are whole cycles per loop so the crossfade joins in phase.
    /// </summary>
    [InitializeOnLoad]
    public static class SynthBatch1
    {
        public const string Set = "batch1";
        /// <summary>One volume for every cue: the tiers carry the loud / quiet order. The volume arguments below are batch 1 history.</summary>
        public const float CueVolume = 0.8f;

        static List<SynthRecipes.Recipe> _list;

        static SynthBatch1() => SynthRecipes.AddProvider(All);

        public static IEnumerable<SynthRecipes.Recipe> All()
        {
            if (_list == null)
            {
                _list = new List<SynthRecipes.Recipe>();
                Build();
            }
            return _list;
        }

        static SynthRecipes.Recipe Add(string group, string id, Action<SynthPatch> build, Action<AudioCue> cue, bool gap = false, params string[] extraClips)
        {
            var r = new SynthRecipes.Recipe { set = Set, group = group, id = id, build = build, cue = cue, gap = gap, extraClips = extraClips };
            _list.Add(r);
            return r;
        }

        // ---- cue presets ----

        public static Action<AudioCue> Sfx(float volume, int priority = 128, int voices = 4, float pitchJitter = 0.03f, float maxDistance = 1.5f) => c =>
        {
            c.bus = AudioBus.SFX;
            c.spatial = true;
            c.volume = volume;
            c.pitchMin = 1f - pitchJitter;
            c.pitchMax = 1f + pitchJitter;
            c.priority = priority;
            c.maxVoices = voices;
            c.maxDistance = maxDistance;
        };

        public static Action<AudioCue> Ui(float volume, bool spatial = false) => c =>
        {
            c.bus = AudioBus.UI;
            c.spatial = spatial;
            c.volume = volume;
            c.pitchMin = 0.99f;
            c.pitchMax = 1.01f;
            c.maxVoices = 2;
            c.minInterval = 0.05f;
            c.priority = 90;
        };

        public static Action<AudioCue> Loop(float volume, bool spatial = true, AudioBus bus = AudioBus.SFX, float maxDistance = 1.5f) => c =>
        {
            c.bus = bus;
            c.spatial = spatial;
            c.volume = volume;
            c.volumeJitter = 0f;
            c.loop = true;
            c.maxVoices = 4;
            c.minInterval = 0f;
            c.priority = 60;
            c.maxDistance = maxDistance;
        };

        public static Action<AudioCue> Amb(float volume) => c =>
        {
            c.bus = AudioBus.Ambience;
            c.spatial = false;
            c.volume = volume;
            c.volumeJitter = 0f;
            c.loop = true;
            c.maxVoices = 1;
            c.minInterval = 0f;
            c.priority = 40;
        };

        public static Action<AudioCue> Jingle(float volume) => c =>
        {
            c.bus = AudioBus.SFX;
            c.spatial = false;
            c.volume = volume;
            c.volumeJitter = 0f;
            c.maxVoices = 1;
            c.minInterval = 0.5f;
            c.priority = 20;
            c.notes = "Duck music while it plays.";
        };

        public static Action<AudioCue> Voice(float volume, int voices = 2) => c =>
        {
            c.bus = AudioBus.Voice;
            c.spatial = true;
            c.volume = volume;
            c.pitchMin = 0.97f;
            c.pitchMax = 1.03f;
            c.maxVoices = voices;
            c.minInterval = 0.04f;
            c.priority = 70;
        };

        public static Action<AudioCue> Music() => c =>
        {
            c.bus = AudioBus.Music;
            c.spatial = false;
            c.volume = 0.6f;
            c.volumeJitter = 0f;
            c.loop = true;
            c.maxVoices = 1;
            c.minInterval = 0f;
            c.priority = 0;
            c.awaitingClips = true;
            c.notes = "Awaiting Lyria: drop the track in Clips/Music/, cut the loop with Audio Lab > Loop tool, add the _loop clip here.";
        };

        static Action<AudioCue> And(this Action<AudioCue> a, Action<AudioCue> b) => c =>
        {
            a(c);
            b(c);
        };

        // ---- instruments ----

        static SynthLayer Bell(string note, float decay = 1.2f, float ratio = 3.5f, float index = 2f) =>
            L(Sine).Note(note).Env(0.004f, decay).Fm(ratio, index, decay * 0.25f);

        static SynthLayer Glass(string note, float decay = 0.6f) =>
            L(Sine).Note(note).Env(0.003f, decay).Fm(2.76f, 1.4f, 0.08f);

        static SynthLayer Marimba(string note, float decay = 0.5f) =>
            L(Sine).Note(note).Env(0.003f, decay).Fm(4f, 2f, 0.025f);

        static SynthLayer Kalimba(string note, float decay = 0.8f) =>
            L(Sine).Note(note).Env(0.003f, decay).Fm(7f, 1.2f, 0.02f);

        static SynthLayer Wood(float hz, float decay = 0.06f) =>
            L(Triangle, hz).Pitch(4f, 0f, 0.015f).Env(0.003f, decay).BP(hz * 1.2f, -1f, 0.1f, 0.45f);

        static SynthLayer Thump(float hz, float decay = 0.2f, float drop = 6f) =>
            L(Sine, hz).Pitch(drop, 0f, 0.05f, 0.6f).Env(0.003f, decay);

        static SynthLayer Air(float from, float to, float time, float attack, float decay, float res = 0.35f, float curve = 1f) =>
            L(PinkNoise).Filt(SynthFilter.BandPass, from, to, time, res, curve).Env(attack, decay);

        static SynthLayer Dust(float cutoff, float decay, float attack = 0.003f) => L(PinkNoise).LP(cutoff).Env(attack, decay);

        static SynthLayer Bubble() => L(Sine, 600f).Pitch(0f, 10f, 0.03f, 0.6f).Env(0.003f, 0.05f).Inst();

        static SynthLayer Pluck(string note, float decay = 0.35f) =>
            L(SoftSaw).Note(note).Env(0.004f, decay).Filt(SynthFilter.LowPass, 3500f, 700f, 0.25f, 0.15f, 0.6f);

        static string N(string note, float start, float length, int layer, float velocity = 1f) =>
            string.Format(CultureInfo.InvariantCulture, "{0}@{1:0.###}:{2:0.###}/{3}*{4:0.##}", note, start, length, layer, velocity);

        /// <summary>Seeded scatter of short notes (bubbles, chirps, sparkles) between two times.</summary>
        static IEnumerable<string> Scatter(int seed, int count, float from, float to, string[] pool, float length, int layer, float velLo = 0.6f, float velHi = 1f)
        {
            var rng = new Random(seed);
            for (int i = 0; i < count; i++)
            {
                float t = from + (float)rng.NextDouble() * (to - from);
                yield return N(pool[rng.Next(pool.Length)], t, length, layer, velLo + (float)rng.NextDouble() * (velHi - velLo));
            }
        }

        /// <summary>Bird calls: groups of 2-4 chirps at random times.</summary>
        static IEnumerable<string> Birds(int seed, int groups, float from, float to, int layer)
        {
            var rng = new Random(seed);
            string[] pool = { "A6", "B6", "D7", "F#6", "E6" };
            for (int g = 0; g < groups; g++)
            {
                float t = from + (float)rng.NextDouble() * (to - from);
                int chirps = 2 + rng.Next(3);
                string a = pool[rng.Next(pool.Length)], b = pool[rng.Next(pool.Length)];
                float vel = 0.25f + 0.25f * (float)rng.NextDouble();
                for (int c = 0; c < chirps; c++) yield return N(c % 2 == 0 ? a : b, t + c * (0.09f + 0.03f * (float)rng.NextDouble()), 0.05f, layer, vel);
            }
        }

        static float Cycles(int k, float loopSeconds) => k / loopSeconds;

        // ---- the batch ----

        static void Build()
        {
            // Island
            Add("Island", "island.grab", p => p.One(0.3f,
                    L(WhiteNoise).BP(700f, 500f, 0.2f, 0.45f).Env(0.006f, 0.18f).Crackle(180f, 1.2f, 0.35f).Gain(0.8f),
                    Thump(110f, 0.08f, 3f).Gain(0.5f)).Vars(3, 0.6f),
                Sfx(0.45f, 100));
            Add("Island", "island.release", p => p.One(0.35f,
                    Thump(90f, 0.14f, 4f),
                    Dust(900f, 0.09f).Gain(0.5f)).Vars(2, 0.5f),
                Sfx(0.45f, 100));
            Add("Island", "island.scale", p => p.Loop(3f, 0.8f,
                    L(Buzz, 38f).Vib(Cycles(2, 3f), 3f).BP(750f, -1f, 0.2f, 0.6f).Env(0.1f, 1f, 1f).Trem(Cycles(4, 3f), 0.7f),
                    L(Buzz, 55f).Vib(Cycles(1, 3f), 4f).BP(1100f, -1f, 0.2f, 0.5f).Env(0.1f, 1f, 1f).Trem(Cycles(3, 3f), 0.8f).Gain(0.5f)),
                Loop(0.35f), false, "Clips/Kenney/island.scale");
            Add("Island", "island.recenter", p => p.One(0.7f,
                    Air(300f, 1800f, 0.35f, 0.18f, 0.3f, 0.35f, 0.7f),
                    Bell("D6", 0.5f).At(0.3f).Gain(0.15f)),
                Ui(0.45f));

            // Walk
            Add("Walk", "walk.hop", p => p.One(0.18f,
                    L(Sine).Note("D5").Pitch(-5f, 7f, 0.08f, 0.6f).Env(0.003f, 0.1f),
                    L(Triangle).Note("D6").Pitch(-5f, 7f, 0.08f, 0.6f).Env(0.003f, 0.05f).Gain(0.2f)).Vars(3, 0.8f),
                Sfx(0.3f, 150, 3));
            Add("Walk", "walk.stop", p => p.One(0.1f, Wood(1400f, 0.04f)).Vars(2, 0.6f), Sfx(0.25f, 160, 2));
            Add("Walk", "walk.bump", p => p.One(0.28f,
                    Thump(120f, 0.12f),
                    Dust(600f, 0.06f).Gain(0.5f),
                    L(Sine).Note("A4").Pitch(0f, -3f, 0.1f).Env(0.004f, 0.1f).Gain(0.25f)).Vars(2, 0.5f),
                Sfx(0.45f, 120, 2), false, "Clips/Kenney/walk.bump");
            Add("Walk", "grass.rustle", p => p.One(0.38f,
                    L(WhiteNoise).BP(3200f, -1f, 0.2f, 0.25f).Env(0.03f, 0.25f).Trem(28f, 0.7f),
                    L(PinkNoise).HP(1500f).Env(0.02f, 0.2f).Crackle(60f, 1f, 0.25f).Gain(0.4f)).Vars(3, 0f, 0.15f, 0.25f).Master(1f, 8000f),
                Sfx(0.3f, 150, 3, 0.06f));

            // Encounter
            Add("Encounter", "enc.rustle", p => p.One(0.95f,
                    L(WhiteNoise).BP(2500f, -1f, 0.2f, 0.2f).Env(0.05f, 0.6f).Trem(17f, 0.8f),
                    L(WhiteNoise).BP(1200f, -1f, 0.2f, 0.3f).Env(0.05f, 0.5f).Trem(11f, 0.7f).At(0.1f).Gain(0.7f),
                    Dust(700f, 0.5f, 0.08f).Gain(0.5f).Crackle(40f, 1.5f, 0.3f)).Master(1f, 8500f),
                Sfx(0.6f, 80, 2));
            Add("Encounter", "enc.sting", p => p.Seq(0.9f, new[] { Pluck("A4"), Bell("D6", 0.6f).Gain(0.3f) },
                    "A4@0:0.15/0", "D5@0.14:0.4/0", "D6@0.14:0.5/1"),
                Jingle(0.6f));
            Add("Encounter", "enc.rare", p => p.Seq(1.5f, new[] { Pluck("A4"), Glass("D6", 0.7f).Gain(0.6f), Bell("D6", 1f).Gain(0.4f) },
                    "A4@0:0.15/0", "D5@0.14:0.3/0",
                    "D6@0.2:0.2/1", "E6@0.26:0.2/1", "A6@0.32:0.2/1", "B6@0.38:0.2/1", "D7@0.44:0.6/1", "D6@0.44:0.8/2"),
                Jingle(0.7f));
            Add("Encounter", "pond.tap", p => p.One(0.16f,
                    L(Sine, 900f).Pitch(0f, 12f, 0.04f, 0.5f).Env(0.003f, 0.06f),
                    L(WhiteNoise).HP(3000f).Env(0.003f, 0.02f).Gain(0.15f)).Vars(3, 1.5f),
                Sfx(0.35f, 140, 3), true);
            Add("Encounter", "pond.splash", p => p.One(0.75f,
                    L(WhiteNoise).BP(1500f, 700f, 0.3f, 0.2f).Env(0.005f, 0.35f),
                    Dust(1200f, 0.4f, 0.01f).Gain(0.6f),
                    Bubble())
                .Notes(Scatter(11, 7, 0.05f, 0.45f, new[] { "A5", "B5", "D6", "E6" }, 0.05f, 2, 0.3f, 0.7f)),
                Sfx(0.5f, 90, 2), true);

            // Zoom / space
            Add("Zoom", "zoom.in", p => p.One(0.6f,
                    Air(400f, 2500f, 0.4f, 0.15f, 0.25f, 0.45f, 0.8f),
                    L(Sine).Note("A4").Pitch(-7f, 5f, 0.45f).Env(0.1f, 0.3f).Gain(0.15f)),
                Ui(0.4f));
            Add("Zoom", "zoom.out", p => p.One(0.6f,
                    Air(2500f, 400f, 0.4f, 0.15f, 0.25f, 0.45f, 0.8f),
                    L(Sine).Note("A4").Pitch(5f, -7f, 0.45f).Env(0.1f, 0.3f).Gain(0.15f)),
                Ui(0.4f));
            Add("Zoom", "clearing.rumble", p => p.One(1.4f,
                    L(PinkNoise).LP(300f, 120f, 1.2f).Env(0.15f, 1f),
                    L(Sine, 55f).Pitch(0f, -5f, 1.2f).Env(0.2f, 0.9f).Gain(0.6f),
                    L(Silence).Env(0.1f, 1f).Crackle(25f, 3f, 0.3f)),
                Sfx(0.55f, 90, 1, 0.02f, 3f));
            Add("Zoom", "pads.rise", p => p.One(0.45f,
                    Thump(75f, 0.25f, 7f),
                    Dust(500f, 0.15f, 0.005f).Gain(0.5f).Crackle(80f, 2f, 0.3f)).Vars(2, 0.5f),
                Sfx(0.5f, 100, 2));

            // Pods
            Add("Pods", "pod.lead", p => p.One(0.8f, Glass("A6", 0.6f), L(Sine).Note("A5").Env(0.01f, 0.5f).Gain(0.3f)), Sfx(0.4f, 100, 2));
            Add("Pods", "pod.grab", p => p.One(0.16f,
                    L(Sine).Note("B6").Env(0.003f, 0.08f).Fm(2.76f, 1f, 0.03f),
                    L(WhiteNoise).BP(4500f, -1f, 0.1f, 0.3f).Env(0.002f, 0.008f).Gain(0.1f)).Vars(3, 1f).Master(1f, 8000f),
                Sfx(0.35f, 110, 2));
            Add("Pods", "pod.throw", p => p.One(0.45f,
                    Air(600f, 2200f, 0.25f, 0.04f, 0.3f, 0.5f, 0.6f),
                    L(WhiteNoise).BP(3000f, -1f, 0.1f, 0.2f).Env(0.06f, 0.2f).Gain(0.3f)).Vars(3, 0f, 0.1f, 0.15f),
                Sfx(0.5f, 60, 2));
            Add("Pods", "pod.open", p => p.One(1f,
                    L(Sine, 500f).Pitch(12f, 0f, 0.03f).Env(0.003f, 0.05f),
                    L(WhiteNoise).BP(2500f, -1f, 0.1f, 0.3f).Env(0.002f, 0.02f).Gain(0.3f),
                    Glass("A6", 0.5f).Inst())
                .Notes("A6@0.05:0.2/2*0.7", "B6@0.1:0.2/2*0.6", "D7@0.15:0.4/2*0.6", "A6@0.24:0.4/2*0.4"),
                Sfx(0.6f, 60, 2));
            Add("Pods", "pod.return", p => p.One(0.42f,
                    L(Sine).Note("D5").Pitch(-12f, 12f, 0.3f, 1.5f).Env(0.02f, 0.3f).Trem(30f, 0.5f),
                    Air(800f, 3000f, 0.3f, 0.02f, 0.25f, 0.3f).Gain(0.4f)),
                Sfx(0.45f, 80, 2));

            // Runes
            Add("Runes", "rune.hover", p => p.One(0.06f, L(Triangle, 2200f).Env(0.003f, 0.025f).LP(4000f)), Ui(0.15f, true));
            Add("Runes", "rune.select", p => p.One(0.7f, Wood(1000f), Bell("A5", 0.6f).Gain(0.5f)), Ui(0.45f, true));
            Add("Runes", "battle.run", p => p.One(0.38f,
                    Air(2000f, 500f, 0.25f, 0.01f, 0.22f),
                    L(Sine).Note("D5").Pitch(0f, -5f, 0.2f).Env(0.005f, 0.15f).Gain(0.2f)),
                Sfx(0.5f, 80, 1));

            // Moves
            Add("Moves", "move.charge", p => p.One(0.85f,
                    L(SoftSaw).Note("A3").Pitch(-12f, 7f, 0.8f, 1.4f).Filt(SynthFilter.LowPass, 400f, 3000f, 0.8f, 0.2f).Env(0.3f, 0.2f, 0f, 0.05f, 0.3f).Vib(6f, 0.3f),
                    Air(500f, 3000f, 0.8f, 0.4f, 0.3f, 0.3f).Trem(14f, 0.4f).Gain(0.3f)),
                Sfx(0.55f, 80, 2));
            Add("Moves", "move.shoot.fire", p => p.One(0.45f,
                    L(WhiteNoise).BP(1200f, 600f, 0.3f, 0.3f).Env(0.01f, 0.3f).Crackle(150f, 1.5f, 0.5f),
                    L(Sine, 200f).Pitch(7f, -5f, 0.2f).Env(0.005f, 0.15f).Gain(0.4f)).Vars(2, 0.6f).Master(1f, 9000f),
                Sfx(0.6f, 70, 2));
            Add("Moves", "move.shoot.water", p => p.One(0.4f,
                    L(Sine, 300f).Pitch(-5f, 12f, 0.12f, 0.6f).Env(0.005f, 0.15f),
                    Air(1500f, 1500f, 0.1f, 0.005f, 0.12f).Gain(0.3f),
                    Bubble())
                .Notes(Scatter(21, 4, 0.05f, 0.25f, new[] { "A5", "D6", "E6" }, 0.05f, 2, 0.3f, 0.6f)).Vars(2, 0.6f),
                Sfx(0.6f, 70, 2));
            Add("Moves", "move.shoot.wind", p => p.One(0.45f,
                    Air(400f, 1600f, 0.2f, 0.02f, 0.3f, 0.55f),
                    L(WhiteNoise).BP(3500f, -1f, 0.2f, 0.25f).Env(0.02f, 0.2f).Gain(0.25f)).Vars(2, 0f, 0.1f, 0.2f).Master(1f, 8500f),
                Sfx(0.6f, 70, 2));
            Add("Moves", "move.shoot.neutral", p => p.One(0.32f,
                    L(Triangle).Note("E5").Pitch(12f, -5f, 0.15f).Env(0.004f, 0.15f),
                    Thump(150f, 0.08f).Gain(0.4f)).Vars(2, 0.6f),
                Sfx(0.6f, 70, 2));
            Add("Moves", "move.impact.fire", p => p.One(0.6f,
                    L(WhiteNoise).BP(1500f, -1f, 0.2f, 0.2f).Env(0.003f, 0.3f).Crackle(300f, 2f, 0.8f),
                    Dust(400f, 0.25f, 0.004f).Gain(0.7f),
                    Thump(90f, 0.15f, 5f).Gain(0.5f)).Vars(2, 0.5f).Master(1f, 9000f),
                Sfx(0.65f, 60, 2));
            Add("Moves", "move.impact.water", p => p.One(0.7f,
                    L(WhiteNoise).BP(1800f, 800f, 0.2f, 0.2f).Env(0.004f, 0.3f),
                    Dust(900f, 0.5f, 0.004f).Gain(0.5f),
                    Bubble())
                .Notes(Scatter(31, 8, 0.03f, 0.45f, new[] { "F#5", "A5", "B5", "D6", "E6" }, 0.05f, 2, 0.4f, 0.8f)).Vars(2, 0.5f),
                Sfx(0.65f, 60, 2), true);
            Add("Moves", "move.impact.wind", p => p.One(0.6f,
                    Air(1200f, 500f, 0.4f, 0.01f, 0.45f, 0.4f),
                    L(WhiteNoise).BP(3000f, -1f, 0.2f, 0.25f).Env(0.01f, 0.25f).Gain(0.3f)).Vars(2, 0f, 0.1f, 0.2f).Master(1f, 8500f),
                Sfx(0.6f, 60, 2));
            Add("Moves", "move.impact.neutral", p => p.One(0.35f,
                    Thump(100f, 0.18f, 8f),
                    Dust(700f, 0.08f).Gain(0.6f),
                    L(Triangle, 220f).Pitch(0f, -3f, 0.1f).Env(0.003f, 0.12f).Gain(0.3f)).Vars(2, 0.5f),
                Sfx(0.65f, 60, 2));
            Add("Moves", "move.lunge", p => p.One(0.35f, Air(500f, 1500f, 0.18f, 0.05f, 0.2f, 0.45f, 0.7f)).Vars(2, 0f, 0.1f, 0.15f),
                Sfx(0.55f, 70, 2), false, "Clips/Kenney/move.lunge");
            Add("Moves", "move.slam", p => p.One(1.1f,
                    Thump(70f, 0.3f, 7f),
                    Dust(400f, 0.12f).Gain(0.7f).Crackle(60f, 2f, 0.3f),
                    L(Sine).Note("D3").Env(0.004f, 0.9f).Fm(1.41f, 1.5f, 0.3f).Gain(0.35f)),
                Sfx(0.7f, 60, 2), false, "Clips/Kenney/move.slam");
            Add("Moves", "move.shield.up", p => p.One(1.2f,
                    L(Sine).Note("A5").Fm(2f, 1.5f).Env(0.35f, 0.5f, 0f, 0.05f, 0.2f).Trem(7f, 0.2f),
                    L(Sine).Note("D6").Fm(3f, 0.8f).Env(0.4f, 0.5f, 0f, 0.05f, 0.2f).Gain(0.5f),
                    L(Sine).Note("D5").Env(0.3f, 0.6f).Gain(0.4f)),
                Sfx(0.5f, 70, 2));
            Add("Moves", "move.shield.pop", p => p.One(0.6f,
                    L(WhiteNoise).BP(3000f, -1f, 0.1f, 0.3f).Env(0.002f, 0.03f).Gain(0.4f),
                    L(Sine, 700f).Pitch(12f, 0f, 0.02f).Env(0.003f, 0.06f),
                    Glass("B6", 0.3f).Inst())
                .Notes("B6@0.02:0.1/2*0.7", "A6@0.06:0.1/2*0.6", "D6@0.1:0.3/2*0.5"),
                Sfx(0.6f, 60, 2));

            // Hits
            Add("Hits", "hit.normal", p => p.One(0.25f,
                    Thump(140f, 0.12f),
                    L(PinkNoise).BP(1200f).Env(0.003f, 0.06f).Gain(0.6f),
                    L(Triangle).Note("A4").Env(0.003f, 0.08f).Gain(0.2f)).Vars(3, 0.8f),
                Sfx(0.65f, 50, 3));
            Add("Hits", "hit.super", p => p.One(0.6f,
                    Thump(150f, 0.14f),
                    L(WhiteNoise).BP(2500f, -1f, 0.1f, 0.25f).Env(0.003f, 0.1f).Gain(0.4f),
                    Glass("A6", 0.4f).Inst())
                .Notes("A6@0.03:0.15/2*0.6", "D7@0.08:0.3/2*0.6", "B6@0.14:0.3/2*0.4").Vars(2, 0.5f).Master(1f, 8000f),
                Sfx(0.8f, 40, 2));
            Add("Hits", "hit.weak", p => p.One(0.2f,
                    Thump(110f, 0.1f, 3f),
                    Dust(400f, 0.07f, 0.004f).Gain(0.5f)).Vars(2, 0.5f).Master(1f, 2500f),
                Sfx(0.5f, 60, 3));
            Add("Hits", "faint", p => p.One(0.95f,
                    L(Triangle).Note("A4").Pitch(0f, -12f, 0.6f).Env(0.01f, 0.6f, 0f, 0.1f, 0.1f).Vib(5f, 0.3f, 0.1f),
                    L(Sine).Note("D4").Pitch(0f, -12f, 0.6f).Env(0.01f, 0.6f, 0f, 0.1f, 0.1f).Gain(0.3f)),
                Sfx(0.6f, 50, 1));
            Add("Hits", "hp.low", p => p.Loop(1f, 0.04f,
                    L(Sine, 80f).Pitch(3f, 0f, 0.05f).Env(0.01f, 0.15f).Inst(),
                    L(Sine).Note("A3").Env(0.01f, 0.2f).Inst().Gain(0.25f))
                .Notes("D4@0.1:0.1/0", "D4@0.1:0.1/1*0.6", "D4@0.32:0.1/0*0.7"),
                Loop(0.4f, false, AudioBus.UI));

            // Catch
            Add("Catch", "catch.appear", p => p.One(0.7f,
                    Glass("D6", 0.5f),
                    Air(600f, 2000f, 0.25f, 0.08f, 0.2f).Gain(0.4f),
                    L(Sine).Note("A5").Env(0.01f, 0.4f).Gain(0.3f)),
                Sfx(0.55f, 50, 1));
            Add("Catch", "catch.hit", p => p.One(0.4f,
                    L(Sine, 400f).Pitch(7f, 0f, 0.03f).Env(0.003f, 0.08f),
                    Glass("B6", 0.3f).Gain(0.4f),
                    L(WhiteNoise).BP(3000f, -1f, 0.1f, 0.3f).Env(0.002f, 0.01f).Gain(0.2f)),
                Sfx(0.6f, 40, 1));
            Add("Catch", "catch.wobble", p => p.One(0.16f, Wood(900f), L(Triangle, 450f).Env(0.003f, 0.05f).Gain(0.4f)).Vars(3, 1.5f),
                Sfx(0.5f, 40, 1, 0.02f));
            Add("Catch", "catch.success", p => p.One(1.4f,
                    Wood(1300f, 0.05f),
                    Bell("D5", 1f).Gain(0.5f),
                    Glass("D6", 0.6f).Inst())
                .Notes("D6@0.06:0.2/2*0.7", "F#6@0.12:0.2/2*0.7", "A6@0.18:0.2/2*0.7", "D7@0.24:0.7/2*0.8"),
                Sfx(0.95f, 10, 1));
            Add("Catch", "catch.shatter", p => p.One(0.8f,
                    L(WhiteNoise).BP(4000f, -1f, 0.1f, 0.2f).Env(0.002f, 0.15f).Crackle(200f, 1f, 0.4f).Gain(0.3f),
                    Glass("B6", 0.3f).Inst())
                .Notes("B6@0.01:0.1/1*0.8", "A6@0.04:0.1/1*0.7", "F#6@0.07:0.1/1*0.6", "E6@0.1:0.15/1*0.5", "D6@0.13:0.3/1*0.5").Master(1f, 9000f),
                Sfx(0.7f, 40, 1));
            Add("Catch", "catch.dazed", p => p.Loop(2f, 0.1f,
                    Glass("A6", 0.25f).Vib(6f, 0.4f).Inst())
                .Notes("A6@0.15:0.1/0*0.6", "B6@0.4:0.1/0*0.5", "D7@0.65:0.1/0*0.6", "B6@0.9:0.1/0*0.5",
                       "A6@1.15:0.1/0*0.6", "B6@1.4:0.1/0*0.5", "D7@1.65:0.1/0*0.6").Master(1f, 7000f),
                Loop(0.35f));
            Add("Catch", "catch.toRim", p => p.One(0.8f,
                    Air(1800f, 700f, 0.3f, 0.03f, 0.3f),
                    Glass("E6", 0.3f).At(0.32f),
                    Wood(1600f, 0.03f).At(0.32f).Gain(0.5f)),
                Sfx(0.5f, 60, 2));
            Add("Catch", "catch.toStorage", p => p.One(0.9f,
                    L(Sine).Note("D5").Pitch(0f, 24f, 0.5f, 1.6f).Env(0.02f, 0.5f).Trem(20f, 0.3f),
                    Air(800f, 4000f, 0.5f, 0.05f, 0.4f).Gain(0.4f),
                    Glass("D7", 0.4f).At(0.5f).Gain(0.5f)),
                Sfx(0.55f, 60, 1));

            // Progression
            Add("Progression", "xp.tick", p => p.One(0.12f, Marimba("D6", 0.08f)).Master(1f, 7500f),
                Ui(0.35f).And(c =>
                {
                    c.pitchStepSemitones = 1f;
                    c.maxVoices = 3;
                    c.minInterval = 0.03f;
                    c.notes = "Play2D(id, step: n): +1 semitone per tick.";
                }));
            Add("Progression", "evo.rise", p => p.Loop(4f, 1f,
                    L(Sine).Note("A4").Fm(2f, 1f).Env(0.2f, 1f, 1f).Trem(Cycles(36, 4f), 0.3f),
                    L(Sine).Note("D5").Env(0.2f, 1f, 1f).Gain(0.6f),
                    L(PinkNoise).BP(3000f, -1f, 0.2f, 0.3f).Env(0.2f, 1f, 1f).Trem(Cycles(52, 4f), 0.5f).Gain(0.2f),
                    L(Sine).Note("A5").Vib(Cycles(20, 4f), 0.1f).Env(0.2f, 1f, 1f).Gain(0.4f)),
                Loop(0.5f).And(c => c.notes = "Raise with handle.SetPitch while the glow climbs."));
            Add("Progression", "evo.burst", p => p.One(1.6f,
                    L(PinkNoise).LP(2000f, 300f, 0.6f).Env(0.005f, 0.6f),
                    Thump(70f, 0.4f).Gain(0.6f),
                    Glass("D6", 1f).Inst(),
                    Bell("D5", 1.2f).Gain(0.4f))
                .Notes("D6@0.02:0.5/2*0.6", "A6@0.02:0.5/2*0.5", "D7@0.05:0.8/2*0.5"),
                Sfx(0.85f, 20, 1));

            // Spirit
            Add("Spirit", "spirit.appear", p => p.One(1.4f,
                    Bell("D6", 1f),
                    Glass("A6", 0.6f).At(0.08f).Gain(0.5f),
                    Glass("B6", 0.6f).At(0.16f).Gain(0.4f)),
                Voice(0.5f, 1));
            Add("Spirit", "spirit.blip", p => p.One(0.14f,
                    L(Buzz).Note("A5").Pitch(2f, 0f, 0.05f).Env(0.004f, 0.06f).Formant(300f, 700f, 2300f, 1300f, 0.08f, 8f, 0.8f),
                    L(Sine).Note("A6").Env(0.003f, 0.08f).Fm(3.5f, 1f, 0.03f).Gain(0.35f)).Vars(6, 3f, 0.1f, 0.3f).Master(1f, 7500f),
                Voice(0.35f));
            Add("Spirit", "spirit.beam", p => p.Loop(4f, 1f,
                    L(Sine).Note("A3").Env(0.2f, 1f, 1f).Vib(Cycles(1, 4f), 0.05f),
                    L(Sine).Note("A4").Env(0.2f, 1f, 1f).Gain(0.4f),
                    L(Buzz).Note("A3").LP(600f).Env(0.2f, 1f, 1f).Trem(Cycles(12, 4f), 0.2f).Gain(0.2f)),
                Loop(0.3f, true, AudioBus.Voice));
            Add("Spirit", "spirit.focus", p => p.One(0.7f,
                    L(Sine).Note("A5").Env(0.03f, 0.5f).Fm(2f, 1.5f, 0.2f),
                    L(Sine).Note("D5").Env(0.05f, 0.5f).Gain(0.4f)),
                Voice(0.4f, 1));

            // Shrine / UI
            Add("Shrine", "shrine.flame", p => p.Loop(6f, 1.5f,
                    L(PinkNoise).LP(500f).Env(0.2f, 1f, 1f).Trem(Cycles(4, 6f), 0.3f).Gain(0.5f),
                    L(Silence).Env(0.1f, 1f, 1f).Crackle(40f, 2f, 0.8f),
                    L(Silence).Env(0.1f, 1f, 1f).Crackle(90f, 0.7f, 0.3f).HP(1500f)),
                Loop(0.35f));
            Add("Shrine", "shrine.fill", p => p.Loop(2f, 0.5f,
                    L(Sine).Note("D5").Env(0.1f, 1f, 1f),
                    L(Sine).Note("A5").Env(0.1f, 1f, 1f).Gain(0.4f),
                    L(Sine).Note("D6").Env(0.1f, 1f, 1f).Trem(Cycles(10, 2f), 0.1f).Gain(0.2f)),
                Loop(0.35f).And(c => c.notes = "handle.SetPitch(1 + fill) as the ring fills."));
            Add("Shrine", "shrine.flare", p => p.One(0.85f,
                    L(PinkNoise).Filt(SynthFilter.LowPass, 300f, 3000f, 0.15f, 0.1f, 0.6f).Env(0.01f, 0.5f),
                    Thump(80f, 0.3f).Gain(0.6f),
                    L(Silence).Env(0.01f, 0.6f).Crackle(120f, 2f, 0.6f)),
                Sfx(0.6f, 50, 1));
            Add("Shrine", "shrine.heal", p => p.One(2.2f,
                    L(SoftSaw).Note("D4").LP(1200f).Env(0.4f, 1.2f, 0f, 0.05f, 0.3f).Vib(4.5f, 0.06f).Gain(0.5f),
                    L(Sine).Note("F#4").Env(0.45f, 1.2f, 0f, 0.05f, 0.3f).Gain(0.7f),
                    L(Sine).Note("A4").Env(0.5f, 1.2f, 0f, 0.05f, 0.3f).Gain(0.7f),
                    L(Sine).Note("D5").Env(0.55f, 1.2f, 0f, 0.05f, 0.3f).Fm(2f, 0.4f).Gain(0.5f)),
                Sfx(0.6f, 30, 1));
            Add("Shrine", "ui.saved", p => p.Seq(0.8f, new[] { Marimba("A5", 0.4f), Bell("D6", 0.6f).Gain(0.4f) },
                    "A5@0:0.15/0", "D6@0.12:0.4/0", "D6@0.12:0.5/1"),
                Ui(0.35f));

            // Travel
            Add("Travel", "scene.sink", p => p.One(1.5f,
                    L(PinkNoise).LP(600f, 150f, 1.2f).Env(0.1f, 1.1f),
                    L(Sine, 60f).Pitch(0f, -7f, 1.2f).Env(0.1f, 1f).Gain(0.6f),
                    L(Silence).Env(0.1f, 1f).Crackle(30f, 3f, 0.3f)),
                Sfx(0.5f, 50, 1, 0.02f, 3f));
            Add("Travel", "scene.rise", p => p.One(1.4f,
                    L(PinkNoise).LP(150f, 700f, 1.1f).Env(0.4f, 0.9f),
                    L(Sine, 50f).Pitch(-5f, 3f, 1.1f).Env(0.4f, 0.8f).Gain(0.6f),
                    L(Silence).Env(0.3f, 0.9f).Crackle(30f, 3f, 0.3f)),
                Sfx(0.5f, 50, 1, 0.02f, 3f));
            Add("Travel", "gate.hum", p => p.Loop(4f, 1f,
                    L(Sine).Note("D3").Env(0.2f, 1f, 1f).Vib(Cycles(1, 4f), 0.1f),
                    L(Sine).Note("A3").Env(0.2f, 1f, 1f).Gain(0.5f),
                    L(Buzz).Note("D2").LP(400f).Env(0.2f, 1f, 1f).Trem(Cycles(2, 4f), 0.3f).Gain(0.2f)),
                Loop(0.3f, true, AudioBus.SFX, 1f));
            Add("Travel", "gate.enter", p => p.One(1.2f,
                    Air(400f, 3000f, 0.5f, 0.2f, 0.4f, 0.35f, 0.7f),
                    Glass("D6", 0.5f).Inst())
                .Notes("D6@0.45:0.2/1*0.6", "A6@0.52:0.5/1*0.6"),
                Sfx(0.6f, 40, 1));
            Add("Travel", "gate.sealed", p => p.One(0.35f,
                    L(Sine).Note("A3").Pitch(3f, -2f, 0.1f).Env(0.004f, 0.2f),
                    L(Triangle).Note("D3").Env(0.004f, 0.15f).Gain(0.3f),
                    Dust(500f, 0.06f).Gain(0.4f)),
                Sfx(0.45f, 80, 1));

            // Lodge
            Add("Lodge", "lodge.podTick", p => p.One(0.15f,
                    L(Sine).Note("E6").Env(0.003f, 0.1f).Fm(2.76f, 1f, 0.02f),
                    Wood(1600f, 0.03f).Gain(0.4f)).Vars(2, 0.8f),
                Sfx(0.3f, 120, 2));

            // Realm
            Add("Realm", "lantern.sputter", p => p.One(0.7f,
                    L(Silence).Env(0.01f, 0.5f).Crackle(120f, 2f, 0.9f).Trem(6f, 0.8f),
                    L(PinkNoise).LP(900f).Env(0.02f, 0.3f).Trem(9f, 0.7f).Gain(0.4f)).Vars(2, 0f, 0.15f).Master(2.5f),
                Sfx(0.4f, 120, 2));
            Add("Realm", "ember.crackle", p => p.Loop(8f, 2f,
                    L(Silence).Env(0.1f, 1f, 1f).Crackle(30f, 3f, 1f),
                    L(Silence).Env(0.1f, 1f, 1f).Crackle(70f, 0.8f, 0.5f).HP(1500f),
                    L(PinkNoise).LP(250f).Env(0.1f, 1f, 1f).Trem(Cycles(2, 8f), 0.4f).Gain(0.3f)).Master(2f),
                Loop(0.35f, true, AudioBus.Ambience, 2f));
            Add("Realm", "steam.hiss", p => p.Loop(6f, 2f,
                    L(WhiteNoise).BP(3500f, -1f, 0.2f, 0.2f).Env(0.2f, 1f, 1f).Trem(Cycles(2, 6f), 0.5f).Gain(0.6f),
                    L(PinkNoise).BP(1200f).Env(0.2f, 1f, 1f).Trem(Cycles(1, 6f), 0.6f).Gain(0.3f)).Master(1f, 9000f),
                Loop(0.3f, true, AudioBus.Ambience, 2f));
            Add("Realm", "restore.swell", p => p.One(3.6f,
                    L(SoftSaw).Note("D3").Filt(SynthFilter.LowPass, 300f, 2000f, 1.8f, 0.15f).Env(1f, 1.5f, 0f, 0.05f, 0.5f).Gain(0.6f),
                    L(Sine).Note("A3").Env(1f, 1.5f, 0f, 0.05f, 0.5f).Fm(1f, 0.5f),
                    L(Sine).Note("F#4").Env(1.1f, 1.5f, 0f, 0.05f, 0.5f).Vib(5f, 0.05f).Gain(0.7f),
                    L(Sine).Note("D5").At(1f).Env(0.01f, 2f).Fm(3.5f, 1.5f, 0.4f).Gain(0.5f)),
                Sfx(0.8f, 20, 1, 0f, 3f));

            // Puzzles
            Add("Puzzles", "log.lift", p => p.One(1f,
                    L(Buzz, 45f).Vib(1.2f, 3f).BP(800f, -1f, 0.2f, 0.6f).Env(0.05f, 0.4f, 0f, 0.05f, 0.2f).Gain(0.6f),
                    Thump(70f, 0.25f).At(0.55f),
                    Dust(500f, 0.15f).At(0.55f).Gain(0.6f).Crackle(60f, 2f, 0.3f)),
                Sfx(0.6f, 60, 2));
            Add("Puzzles", "bellows.pump", p => p.One(0.5f,
                    Air(400f, 900f, 0.3f, 0.1f, 0.25f, 0.3f),
                    L(WhiteNoise).BP(2000f, -1f, 0.2f, 0.2f).Env(0.1f, 0.2f).Gain(0.2f)).Vars(3, 0f, 0.1f, 0.15f),
                Sfx(0.5f, 70, 2));
            Add("Puzzles", "brazier.light", p => p.One(0.9f,
                    L(PinkNoise).Filt(SynthFilter.LowPass, 200f, 2500f, 0.12f, 0.1f, 0.6f).Env(0.01f, 0.6f),
                    L(Sine, 70f).Pitch(5f, -3f, 0.3f).Env(0.01f, 0.3f).Gain(0.6f),
                    L(Silence).At(0.1f).Env(0.01f, 0.6f).Crackle(120f, 2f, 0.6f)),
                Sfx(0.65f, 50, 2));
            Add("Puzzles", "forge.door", p => p.One(2.4f,
                    L(PinkNoise).LP(250f).Env(0.3f, 1.2f, 0f, 0.05f, 0.8f).Trem(6f, 0.3f),
                    L(Silence).Env(0.2f, 1.5f, 0f, 0.05f, 0.5f).Crackle(50f, 3f, 0.5f),
                    L(Sine, 45f).Vib(2f, 0.5f).Env(0.3f, 1.2f, 0f, 0.05f, 0.6f).Gain(0.5f),
                    Thump(60f, 0.3f).At(1.9f)),
                Sfx(0.65f, 40, 1, 0f, 3f));

            // Keeper
            Add("Keeper", "keeper.crown.shield", p => p.One(1.3f,
                    L(Sine).Note("D5").Fm(2f, 1.5f).Env(0.3f, 0.6f, 0f, 0.05f, 0.2f),
                    L(Sine).Note("A4").Env(0.3f, 0.6f, 0f, 0.05f, 0.2f).Gain(0.6f),
                    L(Silence).Env(0.2f, 0.8f).Crackle(60f, 2f, 0.5f)),
                Sfx(0.6f, 30, 1));
            Add("Keeper", "keeper.crown.crack", p => p.One(0.9f,
                    L(WhiteNoise).BP(3500f, -1f, 0.1f, 0.3f).Env(0.002f, 0.05f).Gain(0.5f),
                    L(Sine, 600f).Pitch(7f, -12f, 0.05f).Env(0.003f, 0.1f),
                    Dust(500f, 0.12f).Gain(0.5f),
                    Glass("B6", 0.4f).Inst())
                .Notes("B6@0.02:0.1/3*0.7", "F#6@0.07:0.15/3*0.6", "D6@0.12:0.4/3*0.5"),
                Sfx(0.75f, 20, 1));
            Add("Keeper", "keeper.fury", p => p.One(1.6f,
                    L(PinkNoise).LP(200f).Env(0.15f, 1f).Trem(8f, 0.4f),
                    L(Sine, 50f).Vib(4f, 1f).Env(0.15f, 1f).Gain(0.6f),
                    L(Silence).Env(0.1f, 1f).Crackle(150f, 2.5f, 1f),
                    L(Buzz, 40f).LP(300f).Env(0.2f, 0.9f).Gain(0.3f)),
                Sfx(0.7f, 20, 1, 0.03f, 3f));

            // Opening
            Add("Opening", "shard.hum", p => p.Loop(4f, 1f,
                    L(Sine).Note("A4").Fm(1.5f, 0.6f).Env(0.2f, 1f, 1f),
                    L(Sine).Note("E5").Env(0.2f, 1f, 1f).Trem(Cycles(16, 4f), 0.3f).Gain(0.3f),
                    L(Sine).Note("A5").Vib(Cycles(20, 4f), 0.08f).Env(0.2f, 1f, 1f).Gain(0.2f)),
                Loop(0.35f));
            Add("Opening", "shard.crack", p => p.One(0.9f,
                    L(WhiteNoise).BP(3500f, -1f, 0.1f, 0.3f).Env(0.002f, 0.05f).Gain(0.5f),
                    L(Sine, 800f).Pitch(7f, -12f, 0.05f).Env(0.003f, 0.1f),
                    Glass("D7", 0.5f).Inst())
                .Notes("D7@0.02:0.1/2*0.7", "A6@0.06:0.15/2*0.6", "E6@0.1:0.2/2*0.5", "D6@0.14:0.5/2*0.4"),
                Sfx(0.7f, 20, 1));
            Add("Opening", "island.unfold", p => p.One(2.6f,
                    L(PinkNoise).LP(150f, 1200f, 2f).Env(0.8f, 0.8f, 0f, 0.05f, 0.6f).Crackle(40f, 3f, 0.4f),
                    L(SoftSaw).Note("D2").LP(200f, 800f, 2f).Env(0.8f, 0.8f, 0f, 0.05f, 0.6f).Gain(0.5f),
                    L(Sine).Note("D3").Pitch(-5f, 0f, 1.6f).Env(0.8f, 0.8f, 0f, 0.05f, 0.6f).Gain(0.6f),
                    Bell("D5", 0.9f).At(1.6f).Gain(0.4f)),
                Sfx(0.7f, 20, 1, 0f, 3f));
            Add("Opening", "explorer.stretch", p => p.One(0.42f,
                    L(Buzz).Note("D5").Pitch(-3f, 5f, 0.25f, 0.7f).Env(0.02f, 0.25f, 0f, 0.05f, 0.05f).Vib(7f, 0.3f, 0.1f).Formant(300f, 270f, 870f, 2300f, 0.3f, 8f, 0.7f),
                    L(Sine).Note("D6").Pitch(-3f, 5f, 0.25f, 0.7f).Env(0.02f, 0.2f).Gain(0.2f)),
                Voice(0.35f, 1));

            // Ambience (30 s loops, 2D)
            const float A = 30f;
            Add("Ambience", "amb.stillisle", p => p.Loop(A, 2f,
                    L(PinkNoise).BP(500f, -1f, 0.2f, 0.1f).Env(0.5f, 1f, 1f).Trem(Cycles(3, A), 0.5f).Gain(0.6f),
                    L(PinkNoise).BP(1400f, -1f, 0.2f, 0.1f).Env(0.5f, 1f, 1f).Trem(Cycles(4, A), 0.7f).Gain(0.3f),
                    L(Sine, 1760f).Pitch(5f, 0f, 0.04f).Vib(22f, 1.5f).Env(0.004f, 0.07f).Inst())
                .Notes(Birds(41, 6, 2.5f, 27.5f, 2)).Master(1f, 9000f),
                Amb(0.3f), true);
            Add("Ambience", "amb.cinderwood", p => p.Loop(A, 3f,
                    L(PinkNoise).BP(700f, -1f, 0.2f, 0.1f).Env(0.5f, 1f, 1f).Trem(Cycles(3, A), 0.6f),
                    L(WhiteNoise).BP(2500f, -1f, 0.2f, 0.1f).Env(0.5f, 1f, 1f).Trem(Cycles(5, A), 0.8f).Gain(0.3f),
                    L(WhiteNoise).BP(3500f, -1f, 0.2f, 0.15f).Env(0.5f, 1f, 1f).Trem(Cycles(270, A), 0.6f).Gain(0.12f)).Master(1f, 8500f),
                Amb(0.3f));
            Add("Ambience", "amb.cinderwood.restored", p => p.Loop(A, 3f,
                    L(PinkNoise).BP(700f, -1f, 0.2f, 0.1f).Env(0.5f, 1f, 1f).Trem(Cycles(3, A), 0.6f),
                    L(WhiteNoise).BP(2500f, -1f, 0.2f, 0.1f).Env(0.5f, 1f, 1f).Trem(Cycles(5, A), 0.8f).Gain(0.25f),
                    L(Silence).Env(0.5f, 1f, 1f).Crackle(12f, 3f, 0.6f),
                    L(PinkNoise).LP(200f).Env(0.5f, 1f, 1f).Trem(Cycles(4, A), 0.3f).Gain(0.25f)).Master(1f, 8500f),
                Amb(0.3f));
            Add("Ambience", "amb.lodge", p => p.Loop(A, 2f,
                    L(PinkNoise).LP(350f).Env(0.5f, 1f, 1f).Trem(Cycles(6, A), 0.3f).Gain(0.6f),
                    L(Silence).Env(0.5f, 1f, 1f).Crackle(18f, 3f, 1f),
                    L(Silence).Env(0.5f, 1f, 1f).Crackle(50f, 0.8f, 0.4f).HP(1500f)),
                Amb(0.3f));
            Add("Ambience", "amb.pond", p => p.Loop(A, 2f,
                    L(PinkNoise).LP(600f).Env(0.5f, 1f, 1f).Trem(Cycles(21, A), 0.9f),
                    L(PinkNoise).BP(1200f, -1f, 0.2f, 0.2f).Env(0.5f, 1f, 1f).Trem(Cycles(33, A), 0.9f).Gain(0.4f),
                    Bubble())
                .Notes(Scatter(51, 14, 2.5f, 27.5f, new[] { "A5", "B5", "D6" }, 0.05f, 2, 0.15f, 0.35f)),
                Amb(0.3f), true);

            // Jingles (marimba + bell, D major pentatonic)
            var mb = new[] { Marimba("D5", 0.5f), Bell("D6", 1.2f).Gain(0.5f), L(SoftSaw).Note("D3").LP(900f).Env(0.2f, 1.5f).Gain(0.4f) };
            Add("Jingles", "jingle.catch", p => p.Seq(1.6f, mb,
                    "D5@0:0.12/0", "F#5@0.09:0.12/0", "A5@0.18:0.12/0", "D6@0.27:0.3/0", "D6@0.4:1/1", "A6@0.4:1/1*0.6"),
                Jingle(0.9f));
            Add("Jingles", "jingle.victory", p => p.Seq(2.3f, mb,
                    "A4@0:0.12/0", "D5@0.12:0.12/0", "F#5@0.24:0.12/0", "A5@0.36:0.25/0",
                    "E5@0.62:0.12/0", "F#5@0.74:0.12/0", "D6@0.86:0.5/0", "D6@0.86:1.2/1", "D3@0.86:1.2/2"),
                Jingle(0.9f));
            Add("Jingles", "jingle.levelup", p => p.Seq(1.5f, mb,
                    "D5@0:0.06/0", "E5@0.06:0.06/0", "F#5@0.12:0.06/0", "A5@0.18:0.06/0", "B5@0.24:0.06/0", "D6@0.3:0.3/0",
                    "D6@0.3:1/1", "A6@0.3:1/1*0.6"),
                Jingle(0.95f));
            Add("Jingles", "jingle.evolve", p => p.Seq(2.7f, new[] { mb[0], mb[1], mb[2], Glass("D7", 0.8f).Gain(0.4f) },
                    "D5@0:0.25/0", "A5@0.25:0.25/0", "F#5@0.5:0.25/0", "D6@0.75:0.4/0", "A6@1.1:0.5/0",
                    "D6@1.1:1.3/1", "D3@0:1.6/2", "A6@1.25:0.3/3", "B6@1.33:0.3/3", "D7@1.41:0.6/3"),
                Jingle(0.95f));
            Add("Jingles", "jingle.keeper", p => p.Seq(3f, mb,
                    "D4@0:0.2/0", "A4@0.2:0.2/0", "D5@0.4:0.3/0", "E5@0.7:0.15/0", "F#5@0.85:0.15/0", "A5@1:0.4/0",
                    "B5@1.4:0.15/0", "A5@1.55:0.15/0", "D6@1.7:0.6/0", "D6@1.7:1.2/1", "A5@1.7:1.2/1*0.6", "D3@0:1.6/2", "D3@1.7:1.2/2"),
                Jingle(0.95f));
            Add("Jingles", "jingle.shard", p => p.Seq(2.3f, new[] { Glass("D6", 0.6f), mb[1] },
                    "A5@0:0.15/0", "D6@0.08:0.15/0", "E6@0.16:0.15/0", "A6@0.24:0.15/0", "B6@0.32:0.15/0", "D7@0.4:0.8/0",
                    "D6@0.4:1.4/1", "A6@0.4:1.4/1*0.5"),
                Jingle(0.9f));

            // Kenney CC0 (Part 4): catch.shatter mixes in a light glass impact; cues without a synth patch.
            _list.Find(r => r.id == "catch.shatter").mixWith = "Clips/Kenney/catch.shatter";
            Action<AudioCue> step = Sfx(0.25f, 150, 3, 0.05f).And(c =>
            {
                c.volumeJitter = 0.1f;
                c.minInterval = 0.06f;
                c.notes = "Pick by tile: grass / path / stone (halls) / wood (lodge).";
            });
            foreach (var id in new[] { "step.grass", "step.path", "step.stone", "step.wood" })
                Add("Walk", id, null, step, false, "Clips/Kenney/" + id);
            Add("Lodge", "lodge.door", null, Sfx(0.5f, 60, 1), false, "Clips/Kenney/lodge.door");
            Add("Lodge", "lodge.page", null, Ui(0.4f, true), false, "Clips/Kenney/lodge.page");

            // Music slots (no clips yet)
            foreach (var id in new[] { "music.title", "music.hub", "music.cinderwood", "music.battle.wild", "music.battle.keeper", "music.lodge" })
                _list.Add(new SynthRecipes.Recipe { set = Set, group = "Music", id = id, cue = Music(), extraClips = new[] { "Clips/Music/" + id } });

            foreach (var r in _list)
                if (r.cue != null && r.group != "Music") r.cue = r.cue.And(c => c.volume = CueVolume);
        }
    }
}
