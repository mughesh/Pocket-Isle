using System.IO;
using Oculus.Interaction.Input;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 8b ghost hand clips: saves clips recorded in Play (Link) as assets in Data/Onboarding/GhostClips and adds them
    /// to the library (a recorded clip replaces the placeholder of the same name), imports clips recorded on the headset
    /// (JSON pulled with adb), and the record window (Tools > Pocket Isle > Ghost Hands > Record Ghost Clip).
    /// </summary>
    [InitializeOnLoad]
    public static class GhostClipTools
    {
        public const string Folder = "Assets/_Project/Data/Onboarding/GhostClips";
        public const string LibraryPath = "Assets/_Project/Data/Onboarding/GhostClips.asset";

        static GhostClipTools()
        {
            GhostClipRecorder.Saved = clip => SaveAsset(clip);
        }

        public static GhostClipLibrary Library()
        {
            DioramaSceneSetup.EnsureFolder("Assets/_Project/Data/Onboarding");
            return StarterKit.Asset<GhostClipLibrary>(LibraryPath);
        }

        /// <summary>Writes (or overwrites, same GUID) the clip's asset and puts it in the library.</summary>
        public static GhostHandClip SaveAsset(GhostHandClip clip)
        {
            DioramaSceneSetup.EnsureFolder(Folder);
            string path = $"{Folder}/{clip.clipName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<GhostHandClip>(path);
            GhostHandClip saved;
            if (existing != null)
            {
                // A new take replaces the old one entirely (trim and key settings too); the GUID stays.
                EditorUtility.CopySerialized(clip, existing);
                existing.name = clip.clipName;
                saved = existing;
            }
            else
            {
                AssetDatabase.CreateAsset(clip, path);
                saved = clip;
            }
            EditorUtility.SetDirty(saved);
            var library = Library();
            library.clips.RemoveAll(c => c == null || c.clipName == saved.clipName);
            library.clips.Add(saved);
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            Debug.Log($"[GhostClipTools] Saved {path} ({saved.frames.Count} frames, {(saved.leftHand ? "left" : "right")} hand); the library has {library.clips.Count} recorded clip(s).");
            return saved;
        }

        [MenuItem("Tools/Pocket Isle/Ghost Hands/Import Clips (from the headset)...", priority = 61)]
        static void ImportMenu()
        {
            string dir = EditorUtility.OpenFolderPanel("Folder with ghost clip JSON (adb pull .../files/ghostclips)", Path.GetFullPath("Builds"), "ghostclips");
            if (!string.IsNullOrEmpty(dir)) Import(dir);
        }

        /// <summary>Every *.json in the folder becomes (or replaces) a clip asset.</summary>
        public static void Import(string dir)
        {
            int n = 0;
            foreach (var file in Directory.GetFiles(dir, "*.json"))
            {
                var clip = ScriptableObject.CreateInstance<GhostHandClip>();
                JsonUtility.FromJsonOverwrite(File.ReadAllText(file), clip);
                if (string.IsNullOrEmpty(clip.clipName) || clip.frames.Count < 2)
                {
                    Debug.LogWarning($"[GhostClipTools] {file}: not a ghost clip, skipped.");
                    Object.DestroyImmediate(clip);
                    continue;
                }
                SaveAsset(clip);
                n++;
            }
            Debug.Log($"[GhostClipTools] Imported {n} clip(s) from {dir}.");
        }

        [MenuItem("Tools/Pocket Isle/Ghost Hands/Record Ghost Clip", priority = 60)]
        static void Open() => EditorWindow.GetWindow<GhostClipWindow>("Ghost Clips");
    }

    /// <summary>Lists the clips the lessons use (recorded or placeholder) with a Record button each (Play mode, Link).</summary>
    sealed class GhostClipWindow : EditorWindow
    {
        float _seconds = -1f;
        int _hand;
        Vector2 _scroll;
        static readonly string[] Hands = { "Auto (right first)", "Right", "Left" };

        void OnGUI()
        {
            EditorGUILayout.HelpBox("Play MR_Main over Link with hand tracking. Press Record, put the hand in place during the 3-2-1, " +
                                    "do the gesture once. The clip replaces the placeholder of the same name (Data/Onboarding/GhostClips). " +
                                    "Trim and the key joint / time are on the clip asset.", MessageType.Info);
            _hand = EditorGUILayout.Popup("Hand", _hand, Hands);
            _seconds = EditorGUILayout.FloatField(new GUIContent("Seconds (-1 = default)"), _seconds);
            var recorder = Application.isPlaying ? Object.FindFirstObjectByType<GhostClipRecorder>() : null;
            var library = AssetDatabase.LoadAssetAtPath<GhostClipLibrary>(GhostClipTools.LibraryPath);
            if (Application.isPlaying && recorder == null) EditorGUILayout.HelpBox("No GhostClipRecorder in the scene (Set Up MR_Main (8b onboarding)).", MessageType.Error);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var w in GhostClipLibrary.Wanted)
            {
                var have = library != null ? library.Find(w.name) : null;
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"{w.name}  ({w.anchor}, {w.seconds:0.#} s)", EditorStyles.boldLabel);
                        EditorGUILayout.LabelField(have != null ? "recorded" : "placeholder", GUILayout.Width(80));
                        if (have != null && GUILayout.Button("Select", GUILayout.Width(55))) Selection.activeObject = have;
                        using (new EditorGUI.DisabledScope(recorder == null || recorder.IsRecording))
                            if (GUILayout.Button("Record", GUILayout.Width(60)))
                                recorder.Record(w.name, w.anchor, _seconds > 0f ? _seconds : w.seconds,
                                    _hand == 1 ? Handedness.Right : _hand == 2 ? Handedness.Left : (Handedness?)null);
                    }
                    EditorGUILayout.LabelField(w.how, EditorStyles.wordWrappedMiniLabel);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        void OnInspectorUpdate() => Repaint();
    }
}
