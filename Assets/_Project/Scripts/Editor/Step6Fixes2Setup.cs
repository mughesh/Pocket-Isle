using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 6 fixes 2: the gaze ray a bit lower (HeadGaze values in MR_Main) and the dialogue script re-imported
    /// (capture.locked now points at the cradle). Safe to re-run.
    /// </summary>
    static class Step6Fixes2Setup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";

        [MenuItem("Tools/Pocket Isle/Set Up MR_Main (6 fixes 2)", priority = 43)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step6Fixes2] Stop Play mode first.");
                return;
            }
            DialogueTools.Import();

            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            var gaze = Object.FindFirstObjectByType<HeadGaze>();
            if (gaze == null)
            {
                Debug.LogError("[Step6Fixes2] MR_Main has no HeadGaze (run Tools > Diorama > Step 5 setups first).");
                return;
            }
            Undo.RecordObject(gaze, "6 fixes 2 gaze");
            gaze.offset = HeadGaze.DefaultOffset;
            gaze.share = 0.85f;
            gaze.maxOffset = 30f;
            EditorUtility.SetDirty(gaze);

            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            Debug.Log($"[Step6Fixes2] MR_Main: gaze offset {gaze.offset} deg, adaptive share {gaze.share}, max {gaze.maxOffset} deg; script re-imported.");
        }
    }
}
