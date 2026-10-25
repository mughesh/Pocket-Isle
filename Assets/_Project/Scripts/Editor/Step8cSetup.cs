using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 8c (first minutes polish): scene values on MR_Main (island at chest height, shard contact) and the
    /// lessons (re-written by Step 8b's setup, then wired). Safe to re-run.
    /// </summary>
    static class Step8cSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";

        [MenuItem("Tools/Pocket Isle/Set Up MR_Main (8c polish)", priority = 49)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step8c] Stop Play mode first.");
                return;
            }
            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            // The lessons, the script and the onboarding / ghost wiring (8b), with 8c's lesson changes.
            Step8bSetup.SetUp();
            var log = new StringBuilder("[Step8c] MR_Main set up:");

            var placer = Object.FindFirstObjectByType<IslandPlacer>(FindObjectsInactive.Include);
            if (placer != null)
            {
                Undo.RecordObject(placer, "Step 8c");
                placer.belowEye = 0.35f;
                placer.distance = 0.45f;
                EditorUtility.SetDirty(placer);
                log.Append($"\n  island spawn: {placer.belowEye * 100f:0} cm below the eyes, {placer.distance * 100f:0} cm ahead (chest)");
            }
            else log.Append("\n  NO IslandPlacer");

            var opening = Object.FindFirstObjectByType<Opening>(FindObjectsInactive.Include);
            if (opening != null)
            {
                Undo.RecordObject(opening, "Step 8c");
                opening.contactSlack = 0.01f;
                opening.autoTouchSeconds = 10f;
                EditorUtility.SetDirty(opening);
                log.Append($"\n  shard: contact (+{opening.contactSlack * 100f:0} cm), unfolds by itself after {opening.autoTouchSeconds:0} s");
            }

            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }
    }
}
