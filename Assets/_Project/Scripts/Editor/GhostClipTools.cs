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

    /// <summary>
    /// The clips the lessons use: record (Play over Link), preview (any Play, flat works: the ghost loops above the island
    /// or at real size, either hand), and tune each recorded clip live (trim, size, speed, key moment).
    /// </summary>
    sealed class GhostClipWindow : EditorWindow
    {
        float _seconds = -1f;
        int _hand, _previewHand = 1, _previewPlace;
        float _previewScale = 1f;
        string _previewing;
        Vector2 _scroll;
        static readonly string[] Hands = { "Auto (right first)", "Right", "Left" };
        static readonly string[] PreviewHands = { "Left", "Right", "Both" };
        static readonly string[] Places = { "Above the island", "At a target (front rim)" };

        void OnGUI()
        {
            EditorGUILayout.HelpBox("Record: Play over Link with hands, press Record, get in place during 3-2-1, do the gesture once.\n" +
                                    "Preview: any Play (flat is fine), press Preview; the ghost loops above the island. Change trim / size / speed below " +
                                    "while it plays. Check / Auto-Trim: Tools > Pocket Isle > Ghost Hands.", MessageType.Info);
            _hand = EditorGUILayout.Popup("Record hand", _hand, Hands);
            _seconds = EditorGUILayout.FloatField(new GUIContent("Record seconds (-1 = default)"), _seconds);
            using (new EditorGUILayout.HorizontalScope())
            {
                _previewHand = EditorGUILayout.Popup("Preview", _previewHand, PreviewHands);
                _previewPlace = EditorGUILayout.Popup(_previewPlace, Places);
            }
            _previewScale = EditorGUILayout.Slider("Preview lesson size", _previewScale, 0.3f, 1.5f);
            var lib = AssetDatabase.LoadAssetAtPath<GhostClipLibrary>(GhostClipTools.LibraryPath);
            if (lib != null)
            {
                EditorGUI.BeginChangeCheck();
                float ts = EditorGUILayout.Slider(new GUIContent("Size at targets (all clips)", "Ghosts at a lesson target show at this share of real hand size. The thumb demos above the island are not affected."), lib.targetScale, 0.3f, 1.5f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(lib, "Ghost target size");
                    lib.targetScale = ts;
                    EditorUtility.SetDirty(lib);
                }
            }
            var recorder = Application.isPlaying ? Object.FindFirstObjectByType<GhostClipRecorder>() : null;
            var ghosts = Application.isPlaying ? Object.FindFirstObjectByType<GhostHands>() : null;
            var library = AssetDatabase.LoadAssetAtPath<GhostClipLibrary>(GhostClipTools.LibraryPath);
            if (Application.isPlaying && (recorder == null || ghosts == null)) EditorGUILayout.HelpBox("No Ghost Hands in the scene (Set Up MR_Main (8b onboarding)).", MessageType.Error);
            if (ghosts != null && ghosts.previewing)
                using (new EditorGUILayout.HorizontalScope())
                {
                    float d = Mathf.Max(0.01f, ghosts.ClipDuration);
                    EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), ghosts.ClipTime / d, $"{_previewing}  {ghosts.ClipTime:0.00} / {d:0.00} s");
                    if (GUILayout.Button("Stop preview", GUILayout.Width(100))) StopPreview(ghosts);
                }
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
                        using (new EditorGUI.DisabledScope(ghosts == null))
                            if (GUILayout.Button(_previewing == w.name ? "Restart" : "Preview", GUILayout.Width(65))) Preview(ghosts, w.name);
                        using (new EditorGUI.DisabledScope(recorder == null || recorder.IsRecording))
                            if (GUILayout.Button("Record", GUILayout.Width(60)))
                                recorder.Record(w.name, w.anchor, _seconds > 0f ? _seconds : w.seconds,
                                    _hand == 1 ? Handedness.Right : _hand == 2 ? Handedness.Left : (Handedness?)null);
                    }
                    EditorGUILayout.LabelField(w.how, EditorStyles.wordWrappedMiniLabel);
                    if (have != null) Tune(have);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        /// <summary>Trim, size, speed and key moment of a recorded clip, live while it previews.</summary>
        static void Tune(GhostHandClip c)
        {
            EditorGUI.BeginChangeCheck();
            float raw = c.RawDuration;
            float start = c.trimStart, end = raw - c.trimEnd;
            EditorGUILayout.MinMaxSlider(new GUIContent($"Keep {start:0.00}-{end:0.00} s of {raw:0.00}"), ref start, ref end, 0f, raw);
            float size = EditorGUILayout.Slider("Size", c.size, 0.3f, 1.5f);
            float speed = EditorGUILayout.Slider("Speed", c.speed, 0.3f, 2f);
            float travel = EditorGUILayout.Slider(new GUIContent("Travel", "How far the hand moves around the key moment (fingers untouched)"), c.travel, 0.1f, 1f);
            float key = EditorGUILayout.Slider(new GUIContent("Key moment", "When the key joint lands on the spot (0-1 of the kept part)"), c.keyTime, 0f, 1f);
            if (!EditorGUI.EndChangeCheck()) return;
            Undo.RecordObject(c, "Tune ghost clip");
            c.trimStart = Mathf.Clamp(start, 0f, raw - 0.1f);
            c.trimEnd = Mathf.Clamp(raw - end, 0f, raw - c.trimStart - 0.1f);
            c.size = size;
            c.speed = speed;
            c.travel = travel;
            c.keyTime = key;
            EditorUtility.SetDirty(c);
        }

        void Preview(GhostHands ghosts, string clip)
        {
            if (ghosts == null) return;
            ghosts.previewing = true;
            _previewing = clip;
            var place = _previewPlace == 0 ? GhostPlace.AboveIsland : GhostPlace.AtTarget;
            if (place == GhostPlace.AtTarget)
            {
                // At the island's front edge (the rim's middle), at the lesson size for targets.
                var island = Object.FindFirstObjectByType<IslandBuilder>();
                Dialogue.RegisterTarget("preview", () =>
                {
                    if (island == null) return null;
                    var b = island.LocalBounds;
                    return island.transform.TransformPoint(new Vector3(b.center.x, island.CapTopLocal, b.max.z)) + Vector3.up * 0.05f;
                }, k => { }, () => 0f);
            }
            ghosts.Show(clip, place, "preview", _previewScale, _previewHand == 0, _previewHand == 2);
        }

        static void StopPreview(GhostHands ghosts)
        {
            ghosts.previewing = false;
            ghosts.Hide();
        }

        void OnDisable()
        {
            var ghosts = Application.isPlaying ? Object.FindFirstObjectByType<GhostHands>() : null;
            if (ghosts != null && ghosts.previewing) StopPreview(ghosts);
        }

        void OnInspectorUpdate() => Repaint();
    }
}
