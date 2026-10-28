using System;
using System.Collections.Generic;
using static Diorama.Audio.EditorTools.Syn;
using static Diorama.Audio.SynthWave;
using static Diorama.Audio.SynthConfidence;

namespace Diorama.Audio.EditorTools
{
    /// <summary>
    /// Step audio-D: the sounds for everything built after the audio merge (Galewick, the Cinderwood hand moments,
    /// Lilymere, the finale). Same style as batch 2: transient + body + texture + optional sparkle, toy-sized, warm.
    /// Every cue gets a synth version; the gen batch 2 clips (beds, water, frogs, ice, stone) join the same cues for a pick.
    /// Loops that follow a hand or a piece (wind.sweep, flame.cup, water.slosh, rain.*) are steady; the game sets their
    /// volume and pitch.
    /// </summary>
    public static partial class SynthRemakes
    {
        /// <summary>Loop whole cycles per loop length, so the crossfade joins in phase.</summary>
        static float Cyc(int k, float loopSeconds) => k / loopSeconds;

        /// <summary>Every cue id step audio-D added or remade (StepDTools bakes only these).</summary>
        public static readonly List<string> AudioDIds = new List<string>();

        static void AudioD()
        {
            int before = _list.Count;
            Galewick();
            CinderwoodHands();
            Lilymere();
            Finale();
            AudioDIds.Clear();
            for (int i = before; i < _list.Count; i++) AudioDIds.Add(_list[i].id);
            AudioDIds.Add("log.drop");
            // The finale's music slot (Mugesh makes the track): a cue without clips, like the other music slots.
            _list.Add(new SynthRecipes.Recipe { set = Set, group = "Music", id = "music.finale", cue = SynthBatch1.Music(), extraClips = new[] { "Clips/Music/music.finale" } });
        }

        // ---- Galewick ----

        static void Galewick()
        {
            New("Galewick", "wind.sweep", Ok, SynthBatch1.Loop(0.8f).And(c => c.notes = "Follows the hand's sweep speed: SetVolume / SetPitch from GustField."), p => p.Loop(4f, 1f,
                    M("Wind/Breeze", 900f).Do(l =>
                    {
                        l.wind.gustRate = Cyc(2, 4f);
                        l.wind.gustDepth = 0.25f;
                        l.wind.baseLevel = 0.7f;
                        l.wind.leavesGain = 0.5f;
                    }),
                    M("Wind/Breeze", 1700f).Do(l =>
                    {
                        l.wind.q = 2.5f;
                        l.wind.gustDepth = 0.4f;
                        l.wind.whistleGain = 0.2f;
                    }).Gain(0.3f)));

            New("Galewick", "mill.turn", Ok, SynthBatch1.Loop(0.8f).And(c => c.notes = "The big mill turning: one 3D loop at the mill hub."), p => p.Loop(4f, 1f,
                    M("Friction/Wood creak", 360f).Do(l =>
                    {
                        l.friction.rateStart = 9f;
                        l.friction.rateEnd = 9f;
                        l.friction.irregularity = 0.5f;
                        l.friction.pressure = 0.45f;
                    }).Env(0.2f, 1f, 1f).Trem(Cyc(2, 4f), 0.7f).Gain(0.7f),
                    Mn("Modal/Wood knock", "E4").Do(l => l.modal.exciterMs = 3f).Inst(),
                    M("Wind/Breeze", 750f).Do(l => l.wind.gustRate = Cyc(1, 4f)).Gain(0.25f))
                .Notes(N("E4", 0.4f, 0.1f, 1, 0.5f), N("D4", 1.4f, 0.1f, 1, 0.4f), N("E4", 2.4f, 0.1f, 1, 0.5f), N("D4", 3.4f, 0.1f, 1, 0.4f)));

            New("Galewick", "mill.stop", Ok, SynthBatch1.Sfx(0.8f, 40, 1), p => p.One(0.9f,
                    M("Friction/Wood creak", 300f).Do(l =>
                    {
                        l.friction.rateStart = 20f;
                        l.friction.rateEnd = 6f;
                        l.friction.rateTime = 0.45f;
                    }).Env(0.01f, 0.45f).Gain(0.6f),
                    Mn("Modal/Wood thud (log)", "D4").At(0.42f),
                    Mn("Modal/Wood knock", "A4").At(0.43f).Gain(0.5f),
                    M("Particles/Dust fall").At(0.44f).Env(0.01f, 0.25f).Gain(0.25f),
                    Sub(90f, 0.12f, 0.08f).At(0.42f)));

            New("Galewick", "kite.tug", Ok, SynthBatch1.Sfx(0.8f, 80, 2), p => p.One(0.45f,
                    M("Friction/Rope creak", 620f).Env(0.005f, 0.2f).Gain(0.6f),
                    M("Breath/Puff").Do(l =>
                    {
                        l.breath.flutterRate = 26f;
                        l.breath.flutterDepth = 0.8f;
                        l.breath.formantStart = 900f;
                        l.breath.formantEnd = 1500f;
                    }).Env(0.01f, 0.3f).Gain(0.6f),
                    Whoosh(-3f, 5f, 0.2f, 0.02f, 0.2f, 900f).Gain(0.3f)).Vars(3, 1.5f, 0.1f, 0.15f));

            New("Galewick", "cloud.part", Good, SynthBatch1.Sfx(0.8f, 60, 2), p => p.One(1.4f,
                    M("Breath/Sigh", 900f).Do(l =>
                    {
                        l.breath.formantStart = 700f;
                        l.breath.formantEnd = 1600f;
                        l.breath.sweepTime = 0.9f;
                        l.breath.pinkMix = 0.8f;
                    }).Env(0.25f, 0.9f).Gain(0.7f),
                    M("Wind/Gust", 700f).Env(0.15f, 0.9f).Gain(0.4f),
                    Shimmer("D5", 0.3f, 0.8f).Gain(0.25f),
                    Sparkle(0.5f))
                .Notes(Run(9, 4, 0.35f, 0.1f, 0.25f, 3, 0.25f, 0.35f)).Vars(2, 1f, 0.1f, 0.15f));

            New("Galewick", "knot.pull", Ok, SynthBatch1.Sfx(0.8f, 80, 2), p => p.One(0.35f,
                    M("Friction/Rope creak", 540f).Do(l =>
                    {
                        l.friction.rateStart = 40f;
                        l.friction.rateEnd = 15f;
                        l.friction.rateTime = 0.2f;
                    }).Env(0.005f, 0.22f).Gain(0.7f),
                    Mn("Modal/Wood knock", "B5").Do(l => l.modal.exciterMs = 1.5f).At(0.2f).Gain(0.45f)).Vars(3, 1.5f, 0.1f, 0.15f));

            New("Galewick", "keeper.knot.snap", Good, SynthBatch1.Sfx(0.8f, 40, 2), p => p.One(0.7f,
                    L(SoftSaw).Note("A3").Pitch(7f, 0f, 0.05f).LP(2400f, 700f, 0.4f).Env(0.002f, 0.45f).Vib(9f, 0.25f).Gain(0.6f),
                    L(Triangle).Note("A4").Pitch(5f, 0f, 0.04f).Env(0.002f, 0.3f).Gain(0.35f),
                    Mn("Modal/Wood knock", "E5").Do(l => l.modal.exciterMs = 0.8f).Gain(0.6f),
                    Whoosh(5f, -5f, 0.2f, 0.005f, 0.2f, 1100f).Gain(0.35f)).Vars(2, 1f));

            New("Galewick", "tower.door", Ok, SynthBatch1.Sfx(0.8f, 20, 1, 0f, 3f), p => p.One(2.2f,
                    M("Friction/Wood creak", 420f).Do(l =>
                    {
                        l.friction.body = ModalMaterial.Stone;
                        l.friction.rateStart = 30f;
                        l.friction.rateEnd = 50f;
                        l.friction.rateTime = 1.4f;
                        l.friction.irregularity = 0.6f;
                        l.friction.scrape = 0.6f;
                    }).Env(0.15f, 0.4f, 0f, 0.05f, 1.2f).Gain(0.7f),
                    M("Wind/Gust", 750f).At(0.3f).Env(0.3f, 1.2f).Gain(0.8f),
                    M("Particles/Little rocks", 800f).Scat(5, 1.3f, 5f).At(0.2f).Gain(0.25f),
                    Mn("Modal/Stone clack", "F4").Do(l => l.modal.exciterMs = 4f).At(1.7f).Gain(0.8f)));

            New("Voices", "voice.galeowl", Good, SynthBatch1.Voice(0.8f, 1), p => p.One(1.1f,
                    L(Sine).Note("F#4").Pitch(1f, -1f, 0.25f).Env(0.05f, 0.22f, 0f, 0.05f, 0.08f).Vib(5f, 0.15f).Gain(0.8f),
                    L(Sine).Note("D4").Pitch(1f, -2f, 0.35f).At(0.42f).Env(0.06f, 0.35f, 0f, 0.05f, 0.12f).Vib(5f, 0.15f).Gain(0.8f),
                    M("Breath/Sigh", 700f).Do(l => l.breath.pinkMix = 0.9f).Env(0.04f, 0.25f).Gain(0.25f),
                    M("Breath/Sigh", 600f).Do(l => l.breath.pinkMix = 0.9f).At(0.42f).Env(0.05f, 0.35f).Gain(0.25f)).Vars(2, 1f, 0.05f, 0.1f));

            const float A = 30f;
            New("Ambience", "amb.galewick", Ok, SynthBatch1.Amb(SynthBatch1.CueVolume), p => p.Loop(A, 3f,
                    M("Wind/Breeze", 480f).Do(l =>
                    {
                        l.wind.gustRate = 0.08f;
                        l.wind.gustDepth = 0.35f;
                        l.wind.baseLevel = 0.25f;
                        l.wind.leavesGain = 0.15f;
                    })));
            New("Ambience", "amb.galewick.restored", Ok, SynthBatch1.Amb(SynthBatch1.CueVolume), p => p.Loop(A, 3f,
                    M("Wind/Breeze", 620f).Do(l =>
                    {
                        l.wind.gustRate = 0.3f;
                        l.wind.gustDepth = 0.6f;
                        l.wind.leavesGain = 0.6f;
                    }),
                    M("Wind/Breeze", 1400f).Do(l => l.wind.leavesGain = 0.8f).Gain(0.3f)));
        }

        // ---- Cinderwood hand moments ----

        static void CinderwoodHands()
        {
            New("Cinderwood", "flame.cup", Ok, SynthBatch1.Loop(0.8f).And(c => c.notes = "Cupping the village flame: swells with the cup's progress (SetVolume / SetPitch)."), p => p.Loop(4f, 1f,
                    M("Fire/Campfire").Do(l =>
                    {
                        l.fire.intensity = 0.55f;
                        l.fire.lapRate = Cyc(8, 4f);
                    }),
                    M("Fire/Ember crackle").Do(l => l.fire.crackleRate = 30f).Gain(0.4f)));

            New("Cinderwood", "flame.rekindle", Good, SynthBatch1.Sfx(0.8f, 30, 1), p => p.One(1.6f,
                    M("Fire/Whoomph"),
                    Puff(400f, 1500f, 0.25f, 0.02f, 0.35f).Gain(0.45f),
                    M("Fire/Campfire").At(0.1f).Env(0.1f, 1.2f).Gain(0.5f),
                    Shimmer("D5", 0.15f, 1f).Gain(0.3f),
                    Shimmer("A5", 0.2f, 1f, 6f).Gain(0.25f),
                    Sparkle(0.6f))
                .Notes(Run(10, 5, 0.2f, 0.09f, 0.3f, 5, 0.3f, 0.5f)));

            New("Cinderwood", "lantern.light", Good, SynthBatch1.Sfx(0.8f, 60, 3), p => p.One(0.8f,
                    Mn("Modal/Glass clink", "E6").Do(l => l.modal.decayScale = 0.35f).Gain(0.6f),
                    M("Fire/Whoomph").At(0.04f).Env(0.01f, 0.3f).Gain(0.55f),
                    M("Fire/Ember crackle").At(0.1f).Env(0.05f, 0.5f).Gain(0.35f),
                    Sparkle(0.4f))
                .Notes(N("A6", 0.08f, 0.2f, 3, 0.35f), N("D7", 0.16f, 0.3f, 3, 0.3f)).Vars(3, 2f, 0.05f, 0.1f));

            New("Cinderwood", "smoke.clear", Ok, SynthBatch1.Sfx(0.8f, 60, 2), p => p.One(1.5f,
                    M("Breath/Sigh", 1200f).Do(l =>
                    {
                        l.breath.formantStart = 1400f;
                        l.breath.formantEnd = 500f;
                        l.breath.sweepTime = 1.1f;
                        l.breath.pinkMix = 0.8f;
                    }).Env(0.2f, 1f).Gain(0.7f),
                    M("Wind/Gust", 600f).Env(0.2f, 1f).Gain(0.4f),
                    Shimmer("A5", 0.6f, 0.7f).Gain(0.15f)).Vars(2, 1f, 0.1f, 0.15f));

            New("Cinderwood", "ember.pick", Good, SynthBatch1.Sfx(0.8f, 60, 2), p => p.One(0.4f,
                    Mn("Modal/Stone clack", "A6").Do(l => l.modal.exciterMs = 1.5f).Gain(0.25f),
                    Mn("Modal/Clay pot tok", "E5").Gain(0.5f),
                    M("Fire/Ember crackle").Do(l =>
                    {
                        l.fire.intensity = 1f;
                        l.fire.crackleRate = 120f;
                        l.fire.lapGain = 0.6f;
                    }).Env(0.005f, 0.32f).Gain(0.8f),
                    Puff(900f, 1600f, 0.15f, 0.01f, 0.15f).Gain(0.3f)).Vars(2, 1f));

            New("Cinderwood", "ember.drop", Good, SynthBatch1.Sfx(0.8f, 40, 1), p => p.One(0.9f,
                    Mn("Modal/Clay pot tok", "E4").Do(l => l.modal.exciterMs = 3f).Gain(0.6f),
                    M("Bubbles/Steam pop", 700f).Scat(5, 0.4f, 4f).Gain(0.4f),
                    M("Fire/Ember crackle").Do(l => l.fire.crackleRate = 90f).At(0.03f).Env(0.01f, 0.6f).Gain(0.6f),
                    M("Breath/Sigh", 2600f).At(0.03f).Env(0.02f, 0.4f).Gain(0.25f)));

            New("Cinderwood", "firebox.roar", Good, SynthBatch1.Sfx(0.8f, 20, 1, 0f, 2.5f), p => p.One(2.2f,
                    M("Fire/Whoomph"),
                    M("Fire/Fury").At(0.1f).Env(0.2f, 1.4f).Gain(0.6f),
                    M("Fire/Campfire").At(0.2f).Env(0.3f, 1.4f).Gain(0.5f),
                    Puff(300f, 1200f, 0.4f, 0.03f, 0.5f).Gain(0.4f),
                    Sub(80f, 0.4f, 0.1f)));

            // Mugesh (Oct 27): the old drop was almost silent. A heavier knock with a real wooden body, a bounce and bark.
            Re("log.drop", Ok, p => p.One(0.55f,
                    Mn("Modal/Wood thud (log)", "D4").Do(l =>
                    {
                        l.modal.decayScale = 1.1f;
                        l.modal.exciterMs = 4f;
                    }),
                    Mn("Modal/Wood knock", "A4").Do(l => l.modal.exciterMs = 2.5f).Gain(0.8f),
                    Mn("Modal/Wood thud (log)", "F#4").At(0.16f).Gain(0.4f),
                    Mn("Modal/Wood knock", "B4").At(0.17f).Gain(0.3f),
                    M("Particles/Sticks", 380f).Scat(6, 0.25f, 3f, 0.5f, 1.5f).Gain(0.45f),
                    M("Particles/Dust fall").At(0.02f).Env(0.01f, 0.3f).Gain(0.3f),
                    Sub(90f, 0.15f, 0.12f))).Tier(LoudnessTier.Moves);
        }

        // ---- Lilymere ----

        static void Lilymere()
        {
            New("Lilymere", "lily.click", Good, SynthBatch1.Sfx(0.8f, 60, 2), p => p.One(0.3f,
                    Mn("Modal/Wood knock", "D5").Do(l =>
                    {
                        l.modal.exciterMs = 2f;
                        l.modal.damping = 0.4f;
                    }),
                    M("Bubbles/Splash").Do(l => l.bubbles.atStart = 4).Env(0.003f, 0.25f).Gain(0.55f),
                    M("Bubbles/Spring (steady)").Do(l => l.bubbles.rate = 40f).Env(0.003f, 0.26f).Gain(0.35f)).Vars(3, 1f, 0f, 0.05f).Seed(11));

            New("Lilymere", "lotus.bloom", Good, SynthBatch1.Sfx(0.8f, 40, 2), p => p.One(1.2f,
                    M("Bubbles/Steam pop", 900f).Gain(0.6f),
                    Puff(700f, 1700f, 0.2f, 0.01f, 0.25f).Gain(0.4f),
                    Shimmer("F#5", 0.05f, 0.8f).Gain(0.3f),
                    Sparkle(0.6f))
                .Notes(Run(12, 5, 0.06f, 0.07f, 0.3f, 3, 0.55f, 0.3f)).Vars(3, 2f, 0.05f, 0.1f));

            New("Lilymere", "channel.ripple", Good, SynthBatch1.Sfx(0.8f, 30, 1, 0f, 2.5f), p => p.One(2f,
                    M("Bubbles/Spring (steady)").Do(l => l.bubbles.rate = 30f).Env(0.3f, 1.4f).Gain(0.7f),
                    M("Particles/Water drops").Env(0.2f, 1.2f).Gain(0.4f),
                    Whoosh(-5f, 3f, 1f, 0.3f, 1f, 600f).Gain(0.3f),
                    Shimmer("D5", 0.4f, 1f).Gain(0.2f)));

            New("Lilymere", "water.slosh", Ok, SynthBatch1.Loop(0.8f).And(c => c.notes = "Water held in the palm: follows the hand, louder when it moves."), p => p.Loop(2f, 0.5f,
                    M("Bubbles/Spring (steady)").Do(l =>
                    {
                        l.bubbles.rate = 14f;
                        l.bubbles.radiusMin = 2.5f;
                        l.bubbles.radiusMax = 5f;
                    }).Env(0.1f, 1f, 1f).Trem(Cyc(2, 2f), 0.6f),
                    M("Particles/Water drops").Do(l => l.particles.systemDecay = 0f).Env(0.1f, 1f, 1f).Trem(Cyc(2, 2f), 0.8f).LP(4000f).Gain(0.3f)));

            New("Lilymere", "rain.soft", Ok, SynthBatch1.Loop(0.8f).And(c => c.notes = "Under the palm cloud (RainGarden): follows the cloud."), p => p.Loop(6f, 1.5f,
                    M("Particles/Water drops").Do(l =>
                    {
                        l.particles.density = 90f;
                        l.particles.systemDecay = 0f;
                    }).Env(0.2f, 1f, 1f).LP(6000f),
                    L(PinkNoise).BP(1800f).Env(0.2f, 1f, 1f).Gain(0.08f)));
            New("Lilymere", "rain.hard", Ok, SynthBatch1.Loop(0.8f), p => p.Loop(6f, 1.5f,
                    M("Particles/Water drops").Do(l =>
                    {
                        l.particles.density = 300f;
                        l.particles.systemDecay = 0f;
                    }).Env(0.2f, 1f, 1f).LP(6500f),
                    M("Bubbles/Spring (steady)").Do(l => l.bubbles.rate = 20f).Gain(0.3f),
                    L(PinkNoise).BP(1500f).Env(0.2f, 1f, 1f).Gain(0.15f)));

            New("Lilymere", "frog.croak", Ok, SynthBatch1.Sfx(0.8f, 150, 2, 0.08f), p => p.One(0.35f,
                    L(Buzz).Note("D3").Pitch(2f, -1f, 0.12f).Env(0.005f, 0.07f, 0f, 0.02f, 0.02f).Formant(500f, 700f, 1300f, 1100f, 0.1f, 8f, 0.9f).Trem(35f, 0.55f),
                    L(Buzz).Note("E3").Pitch(2f, -1f, 0.12f).At(0.13f).Env(0.005f, 0.1f, 0f, 0.02f, 0.03f).Formant(500f, 750f, 1300f, 1150f, 0.12f, 8f, 0.9f).Trem(35f, 0.55f).Gain(0.9f))
                .Vars(4, 1.2f, 0.05f, 0.15f).Master(1f, 5000f));

            New("Lilymere", "frog.chorus", Ok, SynthBatch1.Sfx(0.8f, 40, 1), p => p.One(1.75f,
                    L(Buzz).Note("D3").Pitch(2f, -1f, 0.12f).Env(0.005f, 0.08f, 0f, 0.02f, 0.02f).Formant(500f, 700f, 1300f, 1100f, 0.1f, 8f, 0.9f).Trem(35f, 0.8f).Scat(10, 1.7f, 4f, 0.5f, 0.5f),
                    L(Buzz).Note("A3").Pitch(2f, -1f, 0.1f).Env(0.005f, 0.06f, 0f, 0.02f, 0.02f).Formant(600f, 800f, 1500f, 1300f, 0.08f, 8f, 0.9f).Trem(40f, 0.8f).Scat(9, 1.7f, 4f, 0.5f, 0.6f).Gain(0.7f),
                    L(Buzz).Note("F#2").Pitch(2f, -1f, 0.14f).Env(0.005f, 0.1f, 0f, 0.02f, 0.03f).Formant(400f, 600f, 1100f, 950f, 0.12f, 8f, 0.9f).Trem(30f, 0.8f).Scat(5, 1.6f, 3f, 0.4f, 0.6f).Gain(0.6f))
                .Master(1f, 5000f));

            New("Lilymere", "water.scoop", Ok, SynthBatch1.Sfx(0.8f, 60, 2), p => p.One(0.6f,
                    M("Bubbles/Splash").Do(l => l.bubbles.atStart = 8).Env(0.003f, 0.35f),
                    Puff(1500f, 700f, 0.2f, 0.005f, 0.25f).Gain(0.4f),
                    M("Particles/Water drops").At(0.15f).Env(0.02f, 0.4f).Gain(0.5f)).Vars(2, 1f, 0f));

            New("Lilymere", "water.pour", Ok, SynthBatch1.Sfx(0.8f, 40, 1), p => p.One(1.3f,
                    M("Bubbles/Spring (steady)").Do(l => l.bubbles.rate = 60f).Env(0.05f, 0.4f, 0f, 0.05f, 0.7f).Gain(0.8f),
                    M("Particles/Water drops").Do(l => l.particles.systemDecay = 0f).Env(0.05f, 0.4f, 0f, 0.05f, 0.7f).Gain(0.5f),
                    M("Bubbles/Splash").At(0.08f).Env(0.003f, 0.4f).Gain(0.5f)));

            New("Lilymere", "water.spill", Ok, SynthBatch1.Sfx(0.8f, 60, 2), p => p.One(0.6f,
                    M("Bubbles/Splash").Do(l => l.bubbles.atStart = 10).Env(0.003f, 0.4f),
                    M("Particles/Water drops").Scat(4, 0.4f, 3f).Gain(0.5f),
                    M("Bubbles/Drip", 600f).Scat(5, 0.35f, 4f).At(0.1f).Gain(0.3f)).Vars(2, 1f));

            New("Lilymere", "ice.crack", Good, SynthBatch1.Sfx(0.8f, 40, 2), p => p.One(0.9f,
                    Mn("Modal/Glass clink", "F#6").Do(l => l.modal.decayScale = 0.25f).Scat(7, 0.6f, 6f, 0.6f, 1.4f).Gain(0.6f),
                    Mn("Modal/Stone clack", "B5").Do(l => l.modal.exciterMs = 0.7f).Gain(0.7f),
                    M("Friction/Wood creak", 900f).Do(l =>
                    {
                        l.friction.body = ModalMaterial.Glass;
                        l.friction.rateStart = 60f;
                        l.friction.rateEnd = 20f;
                        l.friction.rateTime = 0.5f;
                    }).Env(0.01f, 0.5f).Gain(0.35f),
                    M("Particles/Little rocks", 1200f).Gain(0.25f)).Vars(3, 2f, 0.1f, 0.1f));

            New("Lilymere", "ice.shatter", Good, SynthBatch1.Sfx(0.8f, 20, 1), p => p.One(1.5f,
                    Mn("Modal/Stone clack", "E6").Gain(0.6f),
                    Mn("Modal/Glass clink", "A6").Do(l => l.modal.decayScale = 0.35f).Scat(12, 0.5f, 7f, 0.6f, 1.6f).Gain(0.55f),
                    M("Particles/Little rocks", 1200f).Gain(0.4f),
                    M("Bubbles/Splash").At(0.3f).Env(0.003f, 0.5f).Gain(0.5f),
                    M("Particles/Water drops").At(0.35f).Gain(0.35f),
                    GlassI(0.4f))
                .Notes(RunDown(14, 5, 0.04f, 0.07f, 0.25f, 5, 0.5f, 0.25f)));

            New("Lilymere", "waterfall.run", Ok, SynthBatch1.Loop(0.8f, true, AudioBus.Ambience, 1.5f).And(c => c.notes = "One 3D loop at the waterfall once it runs."), p => p.Loop(6f, 1.5f,
                    L(PinkNoise).BP(1200f).Env(0.2f, 1f, 1f).Trem(Cyc(3, 6f), 0.15f).Gain(0.35f),
                    M("Bubbles/Spring (steady)").Do(l => l.bubbles.rate = 45f).Gain(0.7f),
                    M("Particles/Water drops").Do(l =>
                    {
                        l.particles.density = 150f;
                        l.particles.systemDecay = 0f;
                    }).Env(0.2f, 1f, 1f).Gain(0.4f)));

            New("Lilymere", "lagoon.rise", Good, SynthBatch1.Sfx(0.8f, 10, 1, 0f, 3f), p => p.One(3.6f,
                    M("Bubbles/Spring (steady)").Do(l => l.bubbles.rate = 40f).Env(1f, 2f, 0f, 0.05f, 0.4f).Gain(0.7f),
                    L(PinkNoise).LP(300f, 1600f, 2.5f).Env(1.2f, 2f).Gain(0.25f),
                    Whoosh(-9f, 3f, 2.5f, 1f, 1.5f, 500f).Gain(0.35f),
                    Shimmer("D5", 1.2f, 1.6f).Gain(0.3f),
                    Shimmer("F#5", 1.4f, 1.6f, 6f).Gain(0.25f),
                    Sparkle(0.7f))
                .Notes(Run(10, 6, 1.6f, 0.15f, 0.35f, 5, 0.3f, 0.5f)));

            const float A = 30f;
            New("Ambience", "amb.lilymere", Ok, SynthBatch1.Amb(SynthBatch1.CueVolume), p => p.Loop(A, 3f,
                    M("Wind/Breeze", 450f).Do(l =>
                    {
                        l.wind.gustRate = 0.1f;
                        l.wind.gustDepth = 0.3f;
                        l.wind.leavesGain = 0.4f;
                    }),
                    M("Bubbles/Drip", 470f).Do(l =>
                    {
                        l.bubbles.atStart = 0;
                        l.bubbles.rate = 0.2f;
                        l.bubbles.decayScale = 4f;
                    }).Env(0.2f, 1f, 1f).Gain(0.4f)));
            New("Ambience", "amb.lilymere.restored", Ok, SynthBatch1.Amb(SynthBatch1.CueVolume), p => p.Loop(A, 3f,
                    M("Bubbles/Spring (steady)").Do(l => l.bubbles.rate = 5f).Gain(0.6f),
                    L(PinkNoise).BP(700f).Env(0.2f, 1f, 1f).Trem(Cyc(9, A), 0.4f).Gain(0.12f),
                    M("Wind/Breeze", 520f).Do(l => l.wind.leavesGain = 0.6f).Gain(0.6f)));
        }

        // ---- Finale ----

        static void Finale()
        {
            // Heartbeat: lub-dub every 1.5 s (40 bpm); body 80-300 Hz plus a soft knock, never sub only.
            New("Finale", "heart.beat", Good, SynthBatch1.Loop(0.8f, true, AudioBus.SFX, 2.5f).And(c => c.notes = "The Heart's veins pulse: loop at its chest from the wake to the Hall of Fame."), p => p.Loop(1.5f, 0.04f,
                    L(Sine, 150f).Pitch(5f, 0f, 0.06f).Env(0.006f, 0.2f).Inst(),
                    L(Sine, 300f).Pitch(5f, 0f, 0.05f).Env(0.004f, 0.1f).Inst().Gain(0.35f),
                    Mn("Modal/Clay pot tok", "D3").Do(l =>
                    {
                        l.modal.exciterMs = 6f;
                        l.modal.damping = 0.6f;
                    }).Inst().Gain(0.5f),
                    L(Sine, 70f).Env(0.01f, 0.2f).Inst().Gain(0.3f))
                .Notes("D4@0.08:0.2/0", "D4@0.08:0.1/1", "D4@0.08:0.1/2*0.8", "D4@0.08:0.2/3",
                    "D4@0.4:0.2/0*0.75", "D4@0.4:0.1/1*0.6", "D4@0.4:0.1/2*0.5", "D4@0.4:0.2/3*0.6"));

            New("Finale", "heart.crack", Ok, SynthBatch1.Sfx(0.8f, 10, 2, 0.03f, 3f), p => p.One(1.6f,
                    M("Particles/Sand paper").Env(0.05f, 1.5f).Gain(0.35f),
                    Mn("Modal/Stone clack", "D4").Do(l =>
                    {
                        l.modal.exciterMs = 3f;
                        l.modal.decayScale = 2f;
                    }).Scat(7, 1.2f, 4f, 0.5f, 0.8f),
                    Mn("Modal/Stone clack", "A5").Do(l => l.modal.exciterMs = 0.8f).Scat(8, 1.3f, 5f, 0.6f, 1.2f).Gain(0.45f),
                    M("Particles/Big rocks", 380f).Scat(6, 1.4f, 3f, 0.6f, 0.9f).Gain(0.5f),
                    M("Particles/Dust fall").Env(0.1f, 1.6f).Gain(0.45f),
                    M("Particles/Little rocks", 700f).Scat(12, 1.6f, 4f, 0.6f, 0.6f).At(0.1f).Gain(0.4f),
                    Sub(80f, 0.3f, 0.1f)).Vars(2, 1f, 0.1f, 0.1f));

            New("Finale", "heart.wings", Good, SynthBatch1.Sfx(0.8f, 10, 1, 0f, 3f), p => p.One(2.6f,
                    Whoosh(-7f, 3f, 1.6f, 0.5f, 1.4f, 450f),
                    Whoosh(-5f, 5f, 1.4f, 0.6f, 1.2f, 900f).At(0.2f).Gain(0.6f),
                    M("Breath/Puff").Do(l =>
                    {
                        l.breath.flutterRate = 14f;
                        l.breath.flutterDepth = 0.6f;
                        l.breath.formantStart = 500f;
                        l.breath.formantEnd = 1200f;
                        l.breath.sweepTime = 1.2f;
                    }).Env(0.5f, 1.2f).Gain(0.5f),
                    M("Friction/Wood creak", 300f).Do(l =>
                    {
                        l.friction.body = ModalMaterial.Stone;
                        l.friction.rateStart = 15f;
                        l.friction.rateEnd = 30f;
                    }).Env(0.3f, 0.8f).Gain(0.25f),
                    Shimmer("D4", 0.8f, 1.4f).Gain(0.3f),
                    Shimmer("A4", 0.9f, 1.4f, 6f).Gain(0.25f)));

            New("Finale", "heart.hum", Good, SynthBatch1.Voice(0.8f, 1).And(c => c.notes = "Once when a whisper of the Heart starts (no typing ticks)."), p => p.One(2.4f,
                    L(Buzz).Note("D4").Env(0.6f, 1.4f).Vib(3f, 0.15f).Formant(400f, 400f, 800f, 700f, 1.6f, 6f, 0.85f).HP(200f).LP(1600f).Gain(0.5f),
                    L(Sine).Note("A4").Env(0.6f, 1.4f).Gain(0.3f),
                    M("Breath/Sigh", 700f).Do(l =>
                    {
                        l.breath.formantStart = 500f;
                        l.breath.formantEnd = 900f;
                        l.breath.sweepTime = 1.2f;
                        l.breath.pinkMix = 0.9f;
                    }).Env(0.5f, 1.4f).Gain(0.5f),
                    Shimmer("D5", 0.8f, 1.2f).Gain(0.12f)));

            New("Finale", "spark.chime", Good, SynthBatch1.Sfx(0.8f, 30, 2), p => p.One(1.2f,
                    Mn("Modal/Glass clink", "A6").Do(l => l.modal.decayScale = 0.6f).Gain(0.6f),
                    Shimmer("D6", 0.01f, 0.9f).Gain(0.4f),
                    Shimmer("D6", 0.01f, 0.9f, 6f).Gain(0.3f),
                    Sparkle(0.6f))
                .Notes(N("F#6", 0.06f, 0.2f, 3, 0.4f), N("A6", 0.12f, 0.3f, 3, 0.35f), N("D7", 0.2f, 0.5f, 3, 0.3f)).Vars(2, 1f));

            New("Finale", "credits.tablet", Ok, SynthBatch1.Sfx(0.8f, 60, 2), p => p.One(1.1f,
                    M("Friction/Wood creak", 500f).Do(l =>
                    {
                        l.friction.body = ModalMaterial.Stone;
                        l.friction.rateStart = 50f;
                        l.friction.rateEnd = 70f;
                        l.friction.rateTime = 0.6f;
                        l.friction.scrape = 0.7f;
                        l.friction.pressure = 0.35f;
                    }).Env(0.1f, 0.5f, 0f, 0.05f, 0.2f).Gain(0.6f),
                    M("Particles/Little rocks", 700f).Scat(4, 0.7f, 4f).Gain(0.25f),
                    Mn("Modal/Stone clack", "G5").Do(l => l.modal.exciterMs = 3f).At(0.75f).Gain(0.6f)).Vars(3, 1.5f, 0.1f, 0.1f));

            New("Finale", "gates.bloom", Good, SynthBatch1.Sfx(0.8f, 10, 1, 0f, 3f), p => p.One(3.4f,
                    M("Fire/Campfire").Env(1f, 1.6f).Gain(0.45f),
                    M("Particles/Water drops").At(0.4f).Env(0.8f, 1.6f).Gain(0.35f),
                    M("Bubbles/Spring (steady)").Do(l => l.bubbles.rate = 20f).At(0.4f).Env(0.8f, 1.6f).Gain(0.3f),
                    Whoosh(-9f, 7f, 2.2f, 1f, 1.2f, 700f).Gain(0.5f),
                    Shimmer("D5", 1.2f, 1.6f).Gain(0.3f),
                    Sparkle(0.7f))
                .Notes(Run(10, 8, 1f, 0.16f, 0.35f, 5, 0.25f, 0.55f)));
        }
    }
}
