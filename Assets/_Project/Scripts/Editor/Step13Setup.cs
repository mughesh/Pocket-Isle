using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 13, the finale: A the statue's whispers and wake stages, B the Heart of the World, C the finale on the hub,
    /// D the post-game. Safe to re-run.
    /// </summary>
    public static class Step13Setup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";
        const string NL = "\n";

        [MenuItem("Tools/Pocket Isle/Setup/Finale (13)/Set Up MR_Main (13 finale)", priority = 910)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step13] Stop Play mode first.");
                return;
            }
            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            var log = new StringBuilder("[Step13] MR_Main set up:");
            Whispers(log);
            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }

        /// <summary>A: the script's whisper and finale lines, the Heart's bubble colours, the statue's wake stages.</summary>
        static void Whispers(StringBuilder log)
        {
            var script = DialogueTools.Import();
            log.Append(NL + $"  script: {(script != null ? script.triggers.Count + " triggers" : "MISSING")}");
            var bubble = Object.FindFirstObjectByType<DialogueBubble>(FindObjectsInactive.Include);
            if (bubble != null)
            {
                Undo.RecordObject(bubble, "Step 13");
                bubble.heartColor = new Color(1f, 0.97f, 0.86f, 0.94f);
                bubble.heartGlow = new Color(0.98f, 0.82f, 0.45f, 1f);
                bubble.heartText = new Color(0.42f, 0.3f, 0.12f, 1f);
                log.Append(NL + "  bubble: the Heart's voice (gold-white, no tail, slow fade)");
            }
            else log.Append(NL + "  bubble: none (MISSING)");
            PropKit.WriteStatueStages();
            var lit = PropKit.Load(PropKit.StatueLitName);
            var awake = PropKit.Load(PropKit.StatueAwakeName);
            foreach (var s in Object.FindObjectsByType<StatueSocket>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!s.drawsStatue) continue;
                Undo.RecordObject(s, "Step 13");
                s.statueLitRecipe = lit;
                s.statueAwakeRecipe = awake;
                log.Append(NL + $"  statue ({s.id}): wake stages {(lit != null && awake != null ? "lit + awake" : "MISSING")}");
            }
        }
    }
}
