using Diorama.Audio;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>Audio hookup checks through the Claude bridge (exec:Diorama.EditorTools.AudioHookupDebug.State), in Play.</summary>
    static class AudioHookupDebug
    {
        /// <summary>
        /// Project Settings > Audio for the hookup (48 kHz, DSP 256, 32 / 64 voices, no spatializer), set through Unity's
        /// own settings object: editing ProjectSettings/AudioManager.asset on disk while the Editor is open gets overwritten.
        /// </summary>
        public static void ApplyProjectAudio()
        {
            var objs = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/AudioManager.asset");
            if (objs == null || objs.Length == 0)
            {
                Debug.LogError("[AudioHookupDebug] No AudioManager settings object.");
                return;
            }
            var so = new UnityEditor.SerializedObject(objs[0]);
            so.FindProperty("m_SampleRate").intValue = 48000;
            // "Best latency" is the requested size; m_DSPBufferSize is what the device ends up with (Unity rewrites it).
            so.FindProperty("m_RequestedDSPBufferSize").intValue = 256;
            so.FindProperty("m_DSPBufferSize").intValue = 256;
            so.FindProperty("m_RealVoiceCount").intValue = 32;
            so.FindProperty("m_VirtualVoiceCount").intValue = 64;
            so.FindProperty("m_SpatializerPlugin").stringValue = "";
            so.ApplyModifiedPropertiesWithoutUndo();
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log($"[AudioHookupDebug] Audio settings: DSP requested {so.FindProperty("m_RequestedDSPBufferSize").intValue}, sample rate {so.FindProperty("m_SampleRate").intValue}, " +
                      $"voices {so.FindProperty("m_RealVoiceCount").intValue} / {so.FindProperty("m_VirtualVoiceCount").intValue}, spatializer '{so.FindProperty("m_SpatializerPlugin").stringValue}'.");
        }

        public static void State()
        {
            Debug.Log($"[AudioHookupDebug] Area {AudioDirector.Area ?? "none"}, ambience profile {(AmbienceDirector.Current != null ? AmbienceDirector.Current.name : "none")}, " +
                      $"music {MusicDirector.State} (gain {AudioService.MusicGain:0.00}), distance scale {AudioService.DistanceScale:0.00}, " +
                      $"buses music {AudioService.GetBusVolume(AudioBus.Music):0.00} / ambience {AudioService.GetBusVolume(AudioBus.Ambience):0.00} / " +
                      $"sfx {AudioService.GetBusVolume(AudioBus.SFX):0.00}, listener paused {AudioListener.pause}, restored {RealmState.Restored:0.00}.");
        }
    }
}
