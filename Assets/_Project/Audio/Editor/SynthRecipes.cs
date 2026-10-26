using System;
using System.Collections.Generic;
using UnityEngine;

namespace Diorama.Audio.EditorTools
{
    /// <summary>
    /// The code defaults behind every patch ("Revert to recipe defaults" and "Create missing patches" rebuild from these).
    /// Recipe sets register a provider; each recipe is keyed by the cue id it bakes.
    /// </summary>
    public static class SynthRecipes
    {
        public class Recipe
        {
            public string id, group, set;
            public bool gap;
            /// <summary>The synth patch; null for a cue without one (Kenney clips, music slots).</summary>
            public Action<SynthPatch> build;
            /// <summary>Cue settings (bus, volume, 2D / 3D...), applied when the cue is created or reverted.</summary>
            public Action<AudioCue> cue;
            /// <summary>Asset path prefixes of extra clips the cue also plays (e.g. Clips/Kenney/step.grass).</summary>
            public string[] extraClips;
        }

        static readonly List<Func<IEnumerable<Recipe>>> Providers = new List<Func<IEnumerable<Recipe>>>();

        public static void AddProvider(Func<IEnumerable<Recipe>> provider) => Providers.Add(provider);

        public static List<Recipe> All()
        {
            var list = new List<Recipe>();
            foreach (var p in Providers) list.AddRange(p());
            return list;
        }

        public static Recipe Find(string id)
        {
            foreach (var r in All())
                if (r.id == id) return r;
            return null;
        }

        /// <summary>Resets every field to the recipe, keeping the review verdict and note.</summary>
        public static void Apply(SynthPatch patch, Recipe recipe)
        {
            var review = patch.review;
            var fresh = ScriptableObject.CreateInstance<SynthPatch>();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(fresh), patch);
            UnityEngine.Object.DestroyImmediate(fresh);
            patch.id = recipe.id;
            patch.recipe = recipe.set;
            patch.gap = recipe.gap;
            patch.review = review ?? new AudioReview();
            recipe.build(patch);
        }
    }

    /// <summary>Short fluent helpers so recipes read like a patch sheet.</summary>
    public static class Syn
    {
        public static SynthLayer L(SynthWave wave, float hz = 440f, string label = "") =>
            new SynthLayer { wave = wave, hz = hz, label = label };

        public static SynthLayer Note(this SynthLayer l, string note) { l.note = note; return l; }
        public static SynthLayer Gain(this SynthLayer l, float g) { l.gain = g; return l; }
        public static SynthLayer At(this SynthLayer l, float seconds) { l.startOffset = seconds; return l; }
        public static SynthLayer Gate(this SynthLayer l, float seconds) { l.gate = seconds; return l; }

        public static SynthLayer Env(this SynthLayer l, float attack, float decay, float sustain = 0f, float release = 0.05f, float hold = 0f)
        {
            l.amp = new SynthEnvelope { attack = attack, hold = hold, decay = decay, sustain = sustain, release = release };
            return l;
        }

        public static SynthLayer Pitch(this SynthLayer l, float start, float end, float time, float curve = 1f)
        {
            l.pitchStart = start;
            l.pitchEnd = end;
            l.pitchTime = time;
            l.pitchCurve = curve;
            return l;
        }

        public static SynthLayer Vib(this SynthLayer l, float rate, float depth, float delay = 0f)
        {
            l.vibratoRate = rate;
            l.vibratoDepth = depth;
            l.vibratoDelay = delay;
            return l;
        }

        public static SynthLayer Trem(this SynthLayer l, float rate, float depth)
        {
            l.tremoloRate = rate;
            l.tremoloDepth = depth;
            return l;
        }

        public static SynthLayer Fm(this SynthLayer l, float ratio, float index, float decay = 0f)
        {
            l.fmRatio = ratio;
            l.fmIndex = index;
            l.fmDecay = decay;
            return l;
        }

        public static SynthLayer Filt(this SynthLayer l, SynthFilter type, float from, float to, float time = 0.2f, float res = 0.1f, float curve = 1f)
        {
            l.filter = type;
            l.cutoffStart = from;
            l.cutoffEnd = to;
            l.cutoffTime = time;
            l.resonance = res;
            l.cutoffCurve = curve;
            return l;
        }

        public static SynthLayer LP(this SynthLayer l, float from, float to = -1f, float time = 0.2f, float res = 0.1f) =>
            l.Filt(SynthFilter.LowPass, from, to < 0f ? from : to, time, res);

        public static SynthLayer HP(this SynthLayer l, float from, float to = -1f, float time = 0.2f, float res = 0.1f) =>
            l.Filt(SynthFilter.HighPass, from, to < 0f ? from : to, time, res);

        public static SynthLayer BP(this SynthLayer l, float from, float to = -1f, float time = 0.2f, float res = 0.3f) =>
            l.Filt(SynthFilter.BandPass, from, to < 0f ? from : to, time, res);

        public static SynthLayer Formant(this SynthLayer l, float f1a, float f1b, float f2a, float f2b, float time, float q = 8f, float mix = 1f)
        {
            l.formant = true;
            l.f1Start = f1a;
            l.f1End = f1b;
            l.f2Start = f2a;
            l.f2End = f2b;
            l.formantTime = time;
            l.formantQ = q;
            l.formantMix = mix;
            return l;
        }

        public static SynthLayer Crackle(this SynthLayer l, float density, float lengthMs = 2f, float gain = 0.5f)
        {
            l.crackleDensity = density;
            l.crackleLength = lengthMs;
            l.crackleGain = gain;
            return l;
        }

        // ---- patch helpers ----

        public static SynthPatch One(this SynthPatch p, float duration, params SynthLayer[] layers)
        {
            p.mode = SynthMode.OneShot;
            p.duration = duration;
            p.layers = new List<SynthLayer>(layers);
            return p;
        }

        public static SynthPatch Loop(this SynthPatch p, float duration, float crossfade, params SynthLayer[] layers)
        {
            p.mode = SynthMode.Loop;
            p.duration = duration;
            p.loopCrossfade = crossfade;
            p.layers = new List<SynthLayer>(layers);
            return p;
        }

        /// <summary>Jingle: instruments, then notes as "D5@0.0:0.3/0" (note @ start : length / layer, optional *velocity).</summary>
        public static SynthPatch Seq(this SynthPatch p, float duration, SynthLayer[] instruments, params string[] notes)
        {
            p.mode = SynthMode.Sequence;
            p.duration = duration;
            p.layers = new List<SynthLayer>(instruments);
            p.notes = new List<SynthNote>();
            foreach (var n in notes) p.notes.Add(ParseNote(n));
            return p;
        }

        public static SynthNote ParseNote(string s)
        {
            var note = new SynthNote();
            float vel = 1f;
            int star = s.IndexOf('*');
            if (star >= 0)
            {
                vel = float.Parse(s.Substring(star + 1), System.Globalization.CultureInfo.InvariantCulture);
                s = s.Substring(0, star);
            }
            int at = s.IndexOf('@'), colon = s.IndexOf(':'), slash = s.IndexOf('/');
            note.note = s.Substring(0, at);
            note.start = F(s.Substring(at + 1, colon - at - 1));
            note.length = F(slash > 0 ? s.Substring(colon + 1, slash - colon - 1) : s.Substring(colon + 1));
            note.layer = slash > 0 ? int.Parse(s.Substring(slash + 1)) : 0;
            note.velocity = vel;
            return note;
        }

        static float F(string s) => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture);

        public static SynthPatch Vars(this SynthPatch p, int count, float pitch = 0.5f, float time = 0.08f, float filter = 0.1f)
        {
            p.variants = count;
            p.jitterPitch = pitch;
            p.jitterTime = time;
            p.jitterFilter = filter;
            return p;
        }

        public static SynthPatch Master(this SynthPatch p, float gain = 1f, float lowpass = 11000f, float peakDb = -3f)
        {
            p.gain = gain;
            p.lowpass = lowpass;
            p.peakDb = peakDb;
            return p;
        }

        public static SynthPatch Seed(this SynthPatch p, int seed) { p.seed = seed; return p; }
    }
}
