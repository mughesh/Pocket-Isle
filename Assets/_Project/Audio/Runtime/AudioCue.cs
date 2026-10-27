using System;
using UnityEngine;

namespace Diorama.Audio
{
    public enum AudioBus { Music, Ambience, SFX, UI, Voice }

    /// <summary>
    /// One game sound by id (e.g. pod.throw): which clips, how loud, which bus, 2D or 3D, and how often it may play.
    /// Game code plays it through AudioService with the constant from AudioIds.
    /// </summary>
    [CreateAssetMenu(menuName = "Pocket Isle/Audio/Audio Cue", fileName = "Cue")]
    public class AudioCue : ScriptableObject
    {
        public string id = "";
        [Tooltip("Picked at random, never the same one twice in a row.")] public AudioClip[] clips = new AudioClip[0];

        [Header("Level")]
        [Range(0f, 1f)] public float volume = 0.8f;
        [Tooltip("Random dip per play, 0..1 of the volume.")] [Range(0f, 0.5f)] public float volumeJitter = 0.05f;
        [Range(0.25f, 3f)] public float pitchMin = 1f;
        [Range(0.25f, 3f)] public float pitchMax = 1f;
        [Tooltip("Semitones per step for rising sequences: Play(id, step: n).")] [Range(0f, 12f)] public float pitchStepSemitones;

        [Header("Routing")]
        public AudioBus bus = AudioBus.SFX;
        [Tooltip("3D: placed in the room (built-in panning). Off: plays in the head (UI, music, ambience).")] public bool spatial = true;
        [Range(0.01f, 5f)] public float minDistance = 0.1f;
        [Range(0.05f, 20f)] public float maxDistance = 1.5f;
        public bool loop;

        [Header("Limits")]
        [Range(1, 16)] public int maxVoices = 4;
        [Tooltip("Seconds; a retrigger sooner than this is skipped.")] [Range(0f, 2f)] public float minInterval = 0.03f;
        [Tooltip("0 = most important (never stolen first), 255 = least.")] [Range(0, 255)] public int priority = 128;

        [Header("Notes")]
        [Tooltip("A slot waiting for clips (Lyria music): no clips is expected, not an error.")] public bool awaitingClips;
        [TextArea(1, 3)] public string notes = "";
        [Tooltip("Review for cues without a synth patch (Kenney, music). Patch-backed cues keep it on the patch.")]
        public AudioReview review = new AudioReview();

        [NonSerialized] int _last = -1;
        [NonSerialized] public float lastPlayTime = -999f;

        /// <summary>A random clip, never the previous one when there is a choice.</summary>
        public AudioClip Pick()
        {
            if (clips == null || clips.Length == 0) return null;
            if (clips.Length == 1) return clips[0];
            int i = UnityEngine.Random.Range(0, clips.Length - 1);
            if (i >= _last && _last >= 0) i++;
            _last = i;
            return clips[i];
        }

        public float PickPitch(int step) =>
            UnityEngine.Random.Range(pitchMin, pitchMax) * Mathf.Pow(2f, step * pitchStepSemitones / 12f);

        public float PickVolume() => volume * (1f - UnityEngine.Random.value * volumeJitter);
    }
}
