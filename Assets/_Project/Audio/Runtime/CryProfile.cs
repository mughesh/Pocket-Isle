using System;
using System.Collections.Generic;
using UnityEngine;

namespace Diorama.Audio
{
    public enum CryElement { Neutral, Fire, Water, Wind }

    /// <summary>Vowel targets for the formant glides (F1 / F2 of a small voice).</summary>
    public enum CryVowel { EE, EH, AH, OH, OO, ER }

    /// <summary>One syllable of a cry: a formant glide from one vowel to another with a pitch bend.</summary>
    [Serializable]
    public class CrySyllable
    {
        public CryVowel from = CryVowel.EE;
        public CryVowel to = CryVowel.OO;
        [Range(0.03f, 0.6f)] public float length = 0.14f;
        [Tooltip("Semitones at the start and end of the syllable.")] [Range(-12f, 12f)] public float pitchStart;
        [Range(-12f, 12f)] public float pitchEnd;
        [Tooltip("Silence after it, seconds.")] [Range(0f, 0.3f)] public float gap = 0.04f;
        [Tooltip("Rolled-r flutter (brrp, chirr); 0 = none.")] [Range(0f, 40f)] public float trill;
        [Range(0f, 1f)] public float trillDepth;
    }

    /// <summary>
    /// The voice of one species (keyed by species id = the species asset name). The Audio Lab turns it into the patch and cue
    /// cry.&lt;speciesId&gt;. Body size sets the pitch; syllables are formant glides; the element adds a timbre layer.
    /// An evolved form names its base form and reuses that voice lower, slower and with a sub layer.
    /// </summary>
    [CreateAssetMenu(menuName = "Pocket Isle/Audio/Cry Profile", fileName = "Cry")]
    public class CryProfile : ScriptableObject
    {
        public string speciesId = "";
        public CryElement element;
        [Tooltip("Body height in metres from the creature recipe (read when the profile was made).")] public float bodyHeight = 0.045f;
        [Tooltip("Voice pitch in Hz (tiny ~800, huge ~180), snapped to D major pentatonic.")] [Range(80f, 1200f)] public float basePitch = 600f;
        public List<CrySyllable> syllables = new List<CrySyllable>();

        [Header("Evolved form")]
        [Tooltip("Base species id: when set, this cry is that voice, shifted. The syllables above are ignored.")] public string evolvedFrom = "";
        [Range(-12f, 12f)] public float semitoneShift;
        [Tooltip("Time stretch: 1.2 = 20% slower.")] [Range(0.5f, 2f)] public float slow = 1f;
        public bool sub;

        [Header("Character")]
        [Tooltip("The Keeper: big, low, a crackle bed under it, still gentle.")] public bool keeper;
        [Range(0f, 2f)] public float gain = 1f;
    }
}
