using System.IO;
using UnityEditor;
using UnityEngine;

namespace Diorama.Audio.EditorTools
{
    /// <summary>
    /// Step 20 (the soundtrack goes in), bridge or menu, edit mode. Files: the step 18 First Light track leaves the project
    /// (copied to D:/UNITY/projects/_audio-sources/music/ first), the four new tracks move into Clips/Music under their cue
    /// names with the AssetDatabase (GUIDs kept). Every music clip: 2D, Streaming, Vorbis 0.5, stereo, no normalize. Then the
    /// cues: levels, once / rest / loop, awaitingClips off where a clip exists, the new music.temple cue; Sync Cues (clips,
    /// table, AudioIds). Safe to run again. Menu: Tools > Pocket Isle > Audio > Step 20: Music In.
    /// </summary>
    public static class Step20Audio
    {
        const string Root = "Assets/_Project/Audio/";
        const string Music = Root + "Clips/Music/";
        const string Cues = Root + "Data/Cues/Music/";
        const string SourceDir = "D:/UNITY/projects/_audio-sources/music/";

        static readonly (string from, string to)[] Moves =
        {
            (Root + "Steps_Through_Tall_Grass.mp3", Music + "music.galewick_1.mp3"),
            (Root + "Lanterns_by_the_Spring.mp3", Music + "music.cinderwood_1.mp3"),
            (Root + "Where_the_Lotus_Blooms.mp3", Music + "music.lilymere_1.mp3"),
            (Root + "Rite_of_the_Unbroken.mp3", Music + "music.temple_1.mp3"),
        };

        /// <summary>Cue id, volume, mode (once = plays to its end and stops; rest = once, then rest, then again; loop).</summary>
        static readonly (string id, float volume, bool loop, float rest, string note)[] Levels =
        {
            ("music.title", 0.25f, false, 0f, "Step 20: title screen, once (cut from First Light 0:00-0:43, fades itself)."),
            ("music.hub", 0.25f, false, 0f, "Step 20: Still Isle, once per arrival from a realm or a load (same cut as the title, own copy)."),
            ("music.lodge", 0.18f, false, 0f, "Step 20: Hearth Lodge, once on entering (Lanterns by the Spring tail from 1:32, 27 s)."),
            ("music.finale", 0.4f, false, 0f, "Step 20: the Heart wakes -> credits, once (First Light from the swell at 0:41.5)."),
            ("music.galewick", 0.25f, true, 45f, "Step 20: Steps Through Tall Grass. Galewick restored only (unless Realm music: always)."),
            ("music.cinderwood", 0.25f, true, 45f, "Step 20: Lanterns by the Spring. Cinderwood restored only (unless Realm music: always)."),
            ("music.lilymere", 0.25f, true, 45f, "Step 20: Where the Lotus Blooms. Lilymere restored only (unless Realm music: always)."),
            ("music.temple", 0.25f, true, 0f, "Step 20: Rite of the Unbroken. Keeper halls and Keeper battles (same cue: no restart); loops."),
        };

        [MenuItem("Tools/Pocket Isle/Audio/Step 20: Music In", priority = 120)]
        public static void MusicIn()
        {
            // The step 18 First Light (music.galewick_1) is only a source now: out of the project before Steps takes its name.
            string oldGalewick = Music + "music.galewick_1.mp3";
            var old = AssetImporter.GetAtPath(oldGalewick);
            bool stepsWaiting = File.Exists(Moves[0].from);
            if (old != null && stepsWaiting)
            {
                Directory.CreateDirectory(SourceDir);
                string dest = SourceDir + "First_Light_at_the_Summit.mp3";
                if (!File.Exists(dest)) File.Copy(oldGalewick, dest);
                if (!File.Exists(dest))
                {
                    Debug.LogError($"[Step20Audio] Couldn't copy {oldGalewick} to {dest}; nothing moved.");
                    return;
                }
                AssetDatabase.DeleteAsset(oldGalewick);
                Debug.Log($"[Step20Audio] First Light moved out of the project: {dest}.");
            }
            foreach (var (from, to) in Moves)
            {
                if (!File.Exists(from)) continue;
                string err = AssetDatabase.MoveAsset(from, to);
                Debug.Log(string.IsNullOrEmpty(err) ? $"[Step20Audio] {Path.GetFileName(from)} -> {to}." : $"[Step20Audio] Move {from}: {err}");
            }
            AssetDatabase.Refresh();

            // Every music clip: 2D, streaming, Vorbis ~0.5, stereo, no normalize.
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { Music.TrimEnd('/') }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is AudioImporter imp)) continue;
                var s = imp.defaultSampleSettings;
                s.loadType = AudioClipLoadType.Streaming;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.5f;
                imp.defaultSampleSettings = s;
                imp.forceToMono = false;
                imp.loadInBackground = true;
                var so = new SerializedObject(imp);
                var norm = so.FindProperty("m_Normalize");
                if (norm != null) norm.boolValue = false;
                var three = so.FindProperty("m_3D");
                if (three != null) three.boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
                imp.SaveAndReimport();
            }

            // music.temple: a copy of the wild battle cue (Music bus, 2D), then set below.
            string temple = Cues + "music.temple.asset";
            if (AssetDatabase.LoadAssetAtPath<AudioCue>(temple) == null)
            {
                AssetDatabase.CopyAsset(Cues + "music.battle.wild.asset", temple);
                var t = AssetDatabase.LoadAssetAtPath<AudioCue>(temple);
                t.id = "music.temple";
                t.clips = new AudioClip[0];
                EditorUtility.SetDirty(t);
                AssetDatabase.SaveAssets();
            }
            AudioCueTools.SyncAll();

            foreach (var (id, volume, loop, rest, note) in Levels)
            {
                var cue = AssetDatabase.LoadAssetAtPath<AudioCue>(Cues + id + ".asset");
                if (cue == null)
                {
                    Debug.LogError($"[Step20Audio] No cue {id}.");
                    continue;
                }
                // Its clip by file name (galewick, lilymere and temple have no Sync recipe; the others' Sync gives the same).
                var found = new System.Collections.Generic.List<AudioClip>();
                foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { Music.TrimEnd('/') }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (Path.GetFileName(path).StartsWith(id + "_")) found.Add(AssetDatabase.LoadAssetAtPath<AudioClip>(path));
                }
                cue.clips = found.ToArray();
                cue.volume = volume;
                cue.volumeJitter = 0f;
                cue.loop = loop;
                cue.restSeconds = rest;
                cue.awaitingClips = cue.clips == null || cue.clips.Length == 0;
                cue.notes = note;
                EditorUtility.SetDirty(cue);
                var clip = cue.clips != null && cue.clips.Length > 0 ? cue.clips[0] : null;
                Debug.Log($"[Step20Audio] {id}: {(clip != null ? $"{clip.name} {clip.length:0} s" : "NO CLIP")}, volume {volume:0.00}, " +
                          $"{(!loop ? "once" : rest > 0f ? $"then {rest:0} s rest" : "loops")}.");
            }
            AssetDatabase.SaveAssets();
            AudioCueTools.SyncAll();
        }
    }
}
