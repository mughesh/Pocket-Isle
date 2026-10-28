using System.Collections.Generic;
using Diorama.Audio;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Audio hookup (main lane): game code plays the audio lane's cues through here. Maps the Settings board's pebbles
    /// (<see cref="VolumeSettings"/>) onto <see cref="AudioService"/>'s buses (UI and Voice follow Effects), starts the
    /// <see cref="AudioDirector"/> (ambience and music per area), and plays the old procedural clip for a cue that has no
    /// clips yet (logged once per cue). Procedural sounds that have no cue at all (steps 10a-10f) go through
    /// <see cref="Procedural"/> so the Effects pebbles still set their level.
    /// </summary>
    public static class GameAudio
    {
        /// <summary>The music bus sits under the effects: the default pebble (3 of 4) lands at 0.55.</summary>
        public const float MusicMix = 0.73f;

        static readonly HashSet<string> FallbackLogged = new HashSet<string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            VolumeSettings.Changed -= OnVolumeChanged;
            VolumeSettings.Changed += OnVolumeChanged;
            ApplyVolumes();
            AudioDirector.Ensure();
        }

        static void OnVolumeChanged(string bus) => ApplyVolumes();

        public static void ApplyVolumes()
        {
            float sfx = SfxVolume;
            AudioService.SetBusVolume(AudioBus.Music, VolumeSettings.Volume(VolumeSettings.Music) * MusicMix);
            AudioService.SetBusVolume(AudioBus.Ambience, VolumeSettings.Volume(VolumeSettings.Ambience));
            AudioService.SetBusVolume(AudioBus.SFX, sfx);
            AudioService.SetBusVolume(AudioBus.UI, sfx);
            AudioService.SetBusVolume(AudioBus.Voice, sfx);
        }

        /// <summary>The Effects pebbles as 0..1, for AudioSources that don't go through AudioService.</summary>
        public static float SfxVolume => VolumeSettings.Volume(VolumeSettings.Sfx);

        /// <summary>A 3D one-shot at a world point (on the island).</summary>
        public static AudioHandle At(string id, Vector3 world, int step = 0) => AudioService.Play(id, world, step);

        /// <summary>A 3D one-shot that follows a transform (a pod in flight).</summary>
        public static AudioHandle On(string id, Transform follow, int step = 0) => AudioService.Play(id, follow, step);

        /// <summary>A head-locked one-shot (UI, jingles).</summary>
        public static AudioHandle Ui(string id, int step = 0) => AudioService.Play2D(id, step);

        /// <summary>
        /// The cue at <paramref name="world"/>, or while it has no clips the procedural <paramref name="fallback"/> through
        /// <paramref name="source"/> (old PuzzleSounds users). <paramref name="share"/> scales both (a softer second puff).
        /// </summary>
        public static AudioHandle Play(string id, Vector3 world, AudioSource source, AudioClip fallback, float volume, float share = 1f)
        {
            if (AudioService.Has(id))
            {
                var h = AudioService.Play(id, world);
                if (share != 1f) h.SetVolume(share);
                return h;
            }
            if (FallbackLogged.Add(id)) Debug.LogWarning($"[GameAudio] Cue '{id}' has no clips: the procedural placeholder plays instead.");
            Procedural(source, fallback, volume * share);
            return default;
        }

        /// <summary>A 3D loop at a world point; keep the handle and Stop it. Silent while the cue has no clips (logged once).</summary>
        public static AudioHandle Loop(string id, Vector3 world, float fadeIn = 0.15f) =>
            Ready(id) ? AudioService.PlayLoop(id, world, fadeIn) : default;

        /// <summary>A 3D loop that follows a transform (a held piece, the palm cloud). Silent while the cue has no clips.</summary>
        public static AudioHandle Loop(string id, Transform follow, float fadeIn = 0.15f) =>
            Ready(id) && follow != null ? AudioService.PlayLoop(id, follow, fadeIn) : default;

        static bool Ready(string id)
        {
            if (AudioService.Has(id)) return true;
            if (FallbackLogged.Add(id)) Debug.LogWarning($"[GameAudio] Cue '{id}' has no clips: its loop stays silent.");
            return false;
        }

        /// <summary>A procedural clip with no cue yet, at the Effects level.</summary>
        public static void Procedural(AudioSource source, AudioClip clip, float volume)
        {
            if (source != null && clip != null) source.PlayOneShot(clip, volume * SfxVolume);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => FallbackLogged.Clear();
    }
}
