using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 12b: fixes from Mugesh's Oct 27 night notes. Shared (A): wind shard green. Safe to re-run.
    /// </summary>
    public static class Step12bSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";

        /// <summary>The wind shard's colour (bursts, the statue glow): green, so fire / wind / water read apart.</summary>
        public static readonly Color WindGreen = new Color(0.42f, 0.88f, 0.32f);

        [MenuItem("Tools/Pocket Isle/Setup/Walking + Guidance (12)/Set Up MR_Main (12b night notes)", priority = 901)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step12b] Stop Play mode first.");
                return;
            }
            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            var log = new StringBuilder("[Step12b] MR_Main set up:");
            ShardColours(log);
            Focus(log);
            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            AssetDatabase.SaveAssets();
            IslandBuilder.RebuildAll();
            Debug.Log(log.ToString());
        }

        /// <summary>A1: one focus lock for every hand moment, next to the explorer.</summary>
        static void Focus(StringBuilder log)
        {
            var explorer = Object.FindFirstObjectByType<GestureTestPiece>(FindObjectsInactive.Include);
            if (explorer == null)
            {
                log.Append("\n  focus lock: no explorer (MISSING)");
                return;
            }
            var fl = explorer.GetComponent<FocusLock>();
            if (fl == null) fl = Undo.AddComponent<FocusLock>(explorer.gameObject);
            fl.explorer = explorer;
            fl.island = explorer.island;
            fl.window = explorer.window;
            log.Append($"\n  focus lock: {(fl.window != null ? "on" : "no window (MISSING)")}");
        }

        /// <summary>A6: the wind shard, its statue rim, its bursts go green.</summary>
        static void ShardColours(StringBuilder log)
        {
            var wind = PropKit.WriteWindShardProp();
            AssetDatabase.SaveAssets();
            log.Append($"\n  WindShard green: {PropBaker.Bake(wind).triangles} tris");
            int n = 0;
            foreach (var kh in Object.FindObjectsByType<KeeperHall>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (kh.ownShard == ShardHolder.WindId)
                {
                    Undo.RecordObject(kh, "Step 12b");
                    kh.shardColor = WindGreen;
                    n++;
                }
            foreach (var s in Object.FindObjectsByType<StatueSocket>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (s.shardRecipe != wind) continue;
                Undo.RecordObject(s, "Step 12b");
                s.shardColor = WindGreen;
                n++;
                if (s.statueRecipe != null && s.statueRecipe.palette != null && s.statueRecipe.palette.Length > (int)PropSlot.Accent)
                {
                    s.statueRecipe.palette[(int)PropSlot.Accent] = PropKit.StatueWindRim;
                    EditorUtility.SetDirty(s.statueRecipe);
                }
            }
            // The statue's rims live on the shared statue recipe: rebake it.
            foreach (var s in Object.FindObjectsByType<StatueSocket>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (s.statueRecipe != null && s.shardRecipe == wind)
                {
                    PropBaker.Bake(s.statueRecipe);
                    break;
                }
            log.Append($"\n  wind keeper burst + statue socket green ({n})");
        }
    }
}
