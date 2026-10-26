using System.Text;
using Meta.XR.MRUtilityKit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 9h: first minutes clarity. One method per part (bridge: exec:Diorama.EditorTools.Step9hSetup.X),
    /// <see cref="All"/> for everything. Safe to re-run.
    /// </summary>
    public static class Step9hSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";

        [MenuItem("Tools/Pocket Isle/Set Up MR_Main (9h first minutes)", priority = 61)]
        public static void All()
        {
            var log = new StringBuilder("[Step9h] Set up:");
            Fixes(log);
            Save(log);
        }

        public static void FixesOnly()
        {
            var log = new StringBuilder("[Step9h] Fixes:");
            Fixes(log);
            Save(log);
        }

        static T Find<T>() where T : Object => Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);

        static T Component<T>(GameObject go) where T : Component => go.TryGetComponent(out T c) ? c : Undo.AddComponent<T>(go);

        /// <summary>A: MRUK's startup load off (it starts the room scan with no scene data), the silent room loader instead.</summary>
        public static void Fixes(StringBuilder log)
        {
            var mruk = Find<MRUK>();
            var placer = Find<IslandPlacer>();
            if (mruk == null || placer == null)
            {
                log.Append("\n  MISSING MRUK or IslandPlacer: open MR_Main.");
                return;
            }
            Undo.RecordObject(mruk, "Step 9h");
            mruk.SceneSettings.LoadSceneOnStartup = false;
            EditorUtility.SetDirty(mruk);
            var loader = Component<RoomSceneLoader>(mruk.gameObject);
            loader.placer = placer;
            EditorUtility.SetDirty(loader);
            log.Append($"\n  MRUK '{mruk.name}': LoadSceneOnStartup off; RoomSceneLoader (no scan, no permission prompt) after the island is placed.");
        }

        static void Save(StringBuilder log)
        {
            AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path == ScenePath && !EditorApplication.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                log.Append("\n  Scene saved.");
            }
            else log.Append($"\n  Scene NOT saved (active: {scene.path}, playing {EditorApplication.isPlaying}).");
            Debug.Log(log.ToString());
        }
    }
}
