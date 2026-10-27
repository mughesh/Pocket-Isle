using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 12c: fixes from Mugesh's Oct 28 midnight test (Notes copy.md): smart steps at the pinch-walk speed, the ridge
    /// clouds back down on the road (still moving), the road north of the ridge open again, Lilymere's own blue-green
    /// palette. Safe to re-run.
    /// </summary>
    public static class Step12cSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";
        const string NL = "\n";

        [MenuItem("Tools/Pocket Isle/Setup/Walking + Guidance (12)/Set Up MR_Main (12c Oct 28 notes)", priority = 902)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step12c] Stop Play mode first.");
                return;
            }
            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            var log = new StringBuilder("[Step12c] MR_Main set up:");
            Walking(log);
            Clouds(log);
            LilymerePalette(log);
            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            AssetDatabase.SaveAssets();
            IslandBuilder.RebuildAll();
            Debug.Log(log.ToString());
        }

        /// <summary>"New walking should be a bit faster: the pinch to walk speed."</summary>
        static void Walking(StringBuilder log)
        {
            var explorer = Object.FindFirstObjectByType<GestureTestPiece>(FindObjectsInactive.Include);
            if (explorer == null)
            {
                log.Append(NL + "  walking: no explorer (MISSING)");
                return;
            }
            Undo.RecordObject(explorer, "Step 12c");
            explorer.smartSeconds = explorer.walkSeconds;
            log.Append(NL + $"  smart step: {explorer.smartSeconds} s a tile (= pinch walk)");
        }

        /// <summary>The ridge: puffs on the road again (bob kept small), the road north to the tower open.</summary>
        static void Clouds(StringBuilder log)
        {
            if (!Step11dSetup.GalewickMap(log)) log.Append(NL + "  Galewick map: NOT rewritten");
            foreach (var bank in Object.FindObjectsByType<CloudBankPiece>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Undo.RecordObject(bank, "Step 12c");
                bank.puffHeight = 0.6f;
                bank.bob = 0.1f;
                log.Append(NL + $"  cloud bank: {bank.puffHeight} tiles up, bob {bank.bob}");
            }
        }

        /// <summary>Lilymere had Galewick's meadow greens: now a wetland (theme + willow trees, <see cref="PropKit.WriteLilymereLook"/>).</summary>
        static void LilymerePalette(StringBuilder log)
        {
            PropKit.WriteLilymereLook();
            log.Append(NL + "  Lilymere palette: wetland teal / blue (theme, willows rebaked)");
        }
    }
}
