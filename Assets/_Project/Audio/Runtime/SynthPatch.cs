using System;
using System.Collections.Generic;
using UnityEngine;

namespace Diorama.Audio
{
    /// <summary>Silence: no source, for layers that are only crackle.</summary>
    public enum SynthWave { Sine, Triangle, SoftSquare, SoftSaw, WhiteNoise, PinkNoise, Buzz, Silence }
    public enum SynthFilter { None, LowPass, HighPass, BandPass }
    public enum SynthMode { OneShot, Loop, Sequence }
    public enum ReviewStatus { None, Approved, Redo }
    /// <summary>Claude's honest read of a synth sound after looking at its spectrogram (step B): good, ok, or weak (thin, buzzy, noisy).</summary>
    public enum SynthConfidence { None, Good, Ok, Weak }

    /// <summary>Mugesh's verdict on a sound, written in the Audio Lab's Review tab. The next session reads the Redo notes.</summary>
    [Serializable]
    public class AudioReview
    {
        public ReviewStatus status;
        [TextArea(1, 4)] public string note = "";
    }

    /// <summary>Attack, hold, decay to sustain, release. Times in seconds; the gate is the layer's.</summary>
    [Serializable]
    public class SynthEnvelope
    {
        [Range(0.003f, 4f)] public float attack = 0.005f;
        [Range(0f, 4f)] public float hold;
        [Range(0.001f, 8f)] public float decay = 0.2f;
        [Range(0f, 1f)] public float sustain;
        [Range(0.003f, 8f)] public float release = 0.05f;
    }

    /// <summary>One voice of a patch: an oscillator or noise source, shaped, filtered and mixed in.</summary>
    [Serializable]
    public class SynthLayer
    {
        public bool enabled = true;
        public string label = "";
        [Tooltip("Plays only through the patch's notes (bubbles, chirps, sparkles), not on its own.")] public bool instrument;
        [Tooltip("Oscillator = the wave below. The others are physical models (settings in their own section).")] public SynthModel model = SynthModel.Oscillator;
        public SynthWave wave = SynthWave.Sine;
        [Tooltip("Note name like D5 or F#4; overrides Hz when set.")] public string note = "";
        [Range(20f, 4000f)] public float hz = 440f;
        [Tooltip("Seconds held before release; 0 = until the end of the sound.")] [Range(0f, 30f)] public float gate;
        [Range(0f, 30f)] public float startOffset;
        [Range(0f, 2f)] public float gain = 1f;

        [Header("Pitch envelope (semitones)")]
        [Range(-48f, 48f)] public float pitchStart;
        [Range(-48f, 48f)] public float pitchEnd;
        [Range(0f, 30f)] public float pitchTime = 0.1f;
        [Tooltip("1 linear, below 1 fast then slow, above 1 slow then fast.")] [Range(0.1f, 6f)] public float pitchCurve = 1f;

        public SynthEnvelope amp = new SynthEnvelope();

        [Header("Vibrato / tremolo")]
        [Range(0f, 20f)] public float vibratoRate;
        [Range(0f, 4f)] public float vibratoDepth;
        [Range(0f, 4f)] public float vibratoDelay;
        [Range(0f, 30f)] public float tremoloRate;
        [Range(0f, 1f)] public float tremoloDepth;

        [Header("FM (bells, marimba, glass)")]
        [Range(0f, 16f)] public float fmRatio;
        [Range(0f, 12f)] public float fmIndex;
        [Tooltip("Seconds for the FM index to fall to a third; 0 = constant.")] [Range(0f, 4f)] public float fmDecay;

        [Header("Filter")]
        public SynthFilter filter = SynthFilter.None;
        [Range(20f, 16000f)] public float cutoffStart = 2000f;
        [Range(20f, 16000f)] public float cutoffEnd = 2000f;
        [Range(0f, 30f)] public float cutoffTime = 0.2f;
        [Range(0.1f, 6f)] public float cutoffCurve = 1f;
        [Range(0f, 1f)] public float resonance = 0.1f;

        [Header("Formant pair (vowel glides)")]
        public bool formant;
        [Range(150f, 1500f)] public float f1Start = 700f;
        [Range(150f, 1500f)] public float f1End = 300f;
        [Range(500f, 4000f)] public float f2Start = 1200f;
        [Range(500f, 4000f)] public float f2End = 2200f;
        [Range(0.01f, 4f)] public float formantTime = 0.2f;
        [Range(2f, 20f)] public float formantQ = 8f;
        [Range(0f, 1f)] public float formantMix = 1f;

        [Header("Crackle")]
        [Tooltip("Clicks per second.")] [Range(0f, 400f)] public float crackleDensity;
        [Tooltip("Click length in ms.")] [Range(0.2f, 20f)] public float crackleLength = 2f;
        [Range(0f, 2f)] public float crackleGain = 0.5f;

        [Header("Model settings (used by the chosen model)")]
        public ModalSettings modal = new ModalSettings();
        public ParticleSettings particles = new ParticleSettings();
        public BubbleSettings bubbles = new BubbleSettings();
        public FireSettings fire = new FireSettings();
        public WindSettings wind = new WindSettings();
        public FrictionSettings friction = new FrictionSettings();
        public BreathSettings breath = new BreathSettings();

        [Header("Group and scatter")]
        [Tooltip("Index into the patch's layer groups (-1 = none): the group's offset and gain apply too.")] [Range(-1, 7)] public int group = -1;
        public ScatterSettings scatter = new ScatterSettings();

        public SynthLayer Clone() => JsonUtility.FromJson<SynthLayer>(JsonUtility.ToJson(this));
    }

    /// <summary>One note of a jingle (or of an instrument layer in a one-shot or loop). The layer index picks the instrument.</summary>
    [Serializable]
    public class SynthNote
    {
        public string note = "D5";
        [Range(0f, 10f)] public float start;
        [Range(0.02f, 10f)] public float length = 0.25f;
        [Range(0, 15)] public int layer;
        [Range(0f, 1.5f)] public float velocity = 1f;
    }

    /// <summary>
    /// A recipe for one sound, baked at edit time to Clips/Synth/&lt;group&gt;/&lt;id&gt;[_vN].wav (mono, 48 kHz, 16-bit).
    /// One-shot: all layers play once. Loop: an extra tail is rendered and crossfaded into the head, so the clip loops seamlessly.
    /// Sequence: the notes play, each through one of the layers (the layers are instruments, not played on their own).
    /// </summary>
    [CreateAssetMenu(menuName = "Pocket Isle/Audio/Synth Patch", fileName = "Patch")]
    public class SynthPatch : ScriptableObject
    {
        public const int SampleRate = 48000;

        [Tooltip("The cue id this patch bakes for, e.g. pod.throw.")] public string id = "";
        [Tooltip("Recipe set that rebuilds the defaults (batch1, cry). Empty = no recipe.")] public string recipe = "";
        public SynthMode mode = SynthMode.OneShot;
        [Range(0.05f, 40f)] public float duration = 0.5f;
        public int seed = 1;
        [Range(1, 8)] public int variants = 1;

        [Header("Variant jitter (variant 1 is always the exact recipe)")]
        [Range(0f, 4f)] public float jitterPitch = 0.5f;
        [Range(0f, 0.5f)] public float jitterTime = 0.08f;
        [Range(0f, 0.5f)] public float jitterFilter = 0.1f;

        public List<SynthLayer> layers = new List<SynthLayer>();
        [Tooltip("Transient / body / tail / sparkle sets: move and level several layers together.")] public List<LayerGroup> groups = new List<LayerGroup>();

        [Header("Loop mode")]
        [Range(0.05f, 8f)] public float loopCrossfade = 1f;

        [Header("Notes (Sequence mode, or instrument layers; D major pentatonic: D E F# A B)")]
        public List<SynthNote> notes = new List<SynthNote>();

        [Header("Master")]
        [Tooltip("Drive into the soft limiter.")] [Range(0f, 4f)] public float gain = 1f;
        public bool limiter = true;
        [Tooltip("Keeps the top end soft (the brief: little above ~9 kHz); 16000 = off.")] [Range(1000f, 16000f)] public float lowpass = 9000f;
        [Tooltip("Level at bake time (off = raw render).")] public bool normalize = true;
        [Tooltip("Tier: mid band (250 Hz - 6 kHz) to the tier's level, peaks under -1 dBFS, ends trimmed. Peak: batch 1 (cries).")]
        public LevelMode levelMode = LevelMode.Peak;
        public LoudnessTier tier = LoudnessTier.Island;
        [Tooltip("Fine trim against the tier, dB.")] [Range(-6f, 6f)] public float tierTrimDb;
        [Tooltip("Peak mode only.")] [Range(-24f, 0f)] public float peakDb = -3f;

        [Header("Review")]
        [Tooltip("Organic sound synth may not nail (marked with a star in the brief); flagged in the Review tab.")] public bool gap;
        [Tooltip("synth-good / synth-ok / synth-weak, set by the recipe after reading the spectrogram.")] public SynthConfidence confidence;
        public AudioReview review = new AudioReview();
    }
}
