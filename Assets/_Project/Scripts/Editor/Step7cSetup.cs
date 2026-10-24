using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 7c: the lodge's door archway (props rewritten and baked), the room's palm-over-the-arch exit, and the rest of
    /// the 7c wiring as it lands. Run after Set Up MR_Main (7b lodge). Safe to re-run.
    /// </summary>
    static class Step7cSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";

        [MenuItem("Tools/Pocket Isle/Set Up MR_Main (7c)", priority = 46)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step7c] Stop Play mode first.");
                return;
            }
            var log = new System.Text.StringBuilder("[Step7c] MR_Main set up:");
            LodgeTools.WriteLodge(log);

            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            var room = Object.FindFirstObjectByType<LodgeRoom>(FindObjectsInactive.Include);
            if (room == null)
            {
                Debug.LogError("[Step7c] No Lodge Room (run Set Up MR_Main (7b lodge) first).");
                return;
            }
            room.beaconMaterial = Step7bSetup.BeaconMaterial();
            room.archHeight = 0.76f;
            EditorUtility.SetDirty(room);
            log.Append("\n  lodge door: 2-tile glowing archway; leave by walking onto it, look + pinch, or palm over it (ring)");

            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }
    }
}
