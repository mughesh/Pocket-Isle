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

        /// <summary>Step audio-D: Lilymere back to its drought (pads, rain garden, lagoon, Tidemother, water shard unsolved).</summary>
        public static void LilyReset()
        {
            var saved = PuzzleState.ToSaved();
            saved.RemoveAll(p => p.id == "lm.pads" || p.id == "lm.stream" || p.id == "lm.rain" || p.id == "lm.lagoon" || p.id == "lm.keeper" || p.id == ShardHolder.WaterId);
            PuzzleState.Restore(saved);
            Debug.Log($"[AudioHookupDebug] Lilymere reset: {PuzzleState.Describe()}");
        }

        /// <summary>Step audio-D: plays the cue id in arg.txt at the island's middle, then lists the voices (it should be there).</summary>
        public static void PlayTest()
        {
            var id = System.IO.File.Exists("Temp/ClaudeBridge/arg.txt") ? System.IO.File.ReadAllText("Temp/ClaudeBridge/arg.txt").Trim() : "";
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            var at = island != null ? island.transform.TransformPoint(new Vector3(0f, island.CapTopLocal, 0f)) : Vector3.zero;
            var h = AudioService.Play(id, at);
            Debug.Log($"[AudioHookupDebug] PlayTest {id}: has clips {AudioService.Has(id)}, handle valid {h.IsValid}.");
            Voices();
        }

        /// <summary>Step audio-D: every busy AudioService voice (cue id, loop or one-shot), to catch loops left running.</summary>
        public static void Voices()
        {
            const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static |
                                                     System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
            var service = typeof(AudioService).GetField("_instance", F)?.GetValue(null);
            if (service == null)
            {
                Debug.Log("[AudioHookupDebug] Voices: no AudioService yet.");
                return;
            }
            var lines = new System.Collections.Generic.List<string>();
            foreach (var listName in new[] { "_3d", "_2d" })
                foreach (var v in (System.Collections.IEnumerable)typeof(AudioService).GetField(listName, F).GetValue(service))
                {
                    var t = v.GetType();
                    var cue = t.GetField("cue", F).GetValue(v) as AudioCue;
                    if (cue == null) continue;
                    bool loop = (bool)t.GetField("looping", F).GetValue(v);
                    lines.Add($"{cue.id}{(loop ? " (loop)" : "")}");
                }
            Debug.Log($"[AudioHookupDebug] Voices ({lines.Count}): {string.Join(", ", lines)}.");
        }
    }
}
