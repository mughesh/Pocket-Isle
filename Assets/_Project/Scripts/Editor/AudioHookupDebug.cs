using Diorama.Audio;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>Audio hookup checks through the Claude bridge (exec:Diorama.EditorTools.AudioHookupDebug.State), in Play.</summary>
    static class AudioHookupDebug
    {
        public static void State()
        {
            Debug.Log($"[AudioHookupDebug] Area {AudioDirector.Area ?? "none"}, ambience profile {(AmbienceDirector.Current != null ? AmbienceDirector.Current.name : "none")}, " +
                      $"music {MusicDirector.State} (gain {AudioService.MusicGain:0.00}), distance scale {AudioService.DistanceScale:0.00}, " +
                      $"buses music {AudioService.GetBusVolume(AudioBus.Music):0.00} / ambience {AudioService.GetBusVolume(AudioBus.Ambience):0.00} / " +
                      $"sfx {AudioService.GetBusVolume(AudioBus.SFX):0.00}, listener paused {AudioListener.pause}, restored {RealmState.Restored:0.00}.");
        }
    }
}
