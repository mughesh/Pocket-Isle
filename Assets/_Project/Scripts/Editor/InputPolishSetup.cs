using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 5d scene wiring: swipe auto-walk at the new speeds, thumb-hold probe off, debug labels hidden, smaller
    /// explorer cube, and a "Tile Input" object with the tile cursor, fingertip touch and gaze + pinch pickers.
    /// </summary>
    static class InputPolishSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";
        const string HudPath = "Assets/_Project/Materials/Battle/Hud.mat";

        [MenuItem("Tools/Pocket Isle/Set Up MR_Main (5d tile input)", priority = 40)]
        public static void SetUpMain()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[InputPolish] Stop Play mode first.");
                return;
            }
            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            var explorer = Object.FindFirstObjectByType<GestureTestPiece>();
            var input = Object.FindFirstObjectByType<MicrogestureInput>();
            var handle = Object.FindFirstObjectByType<IslandHandle>();
            var bubble = Object.FindFirstObjectByType<DialogueBubble>();
            var hud = AssetDatabase.LoadAssetAtPath<Material>(HudPath);
            if (island == null || explorer == null || hud == null)
            {
                Debug.LogError("[InputPolish] MR_Main needs the island, the explorer and Materials/Battle/Hud.mat.");
                return;
            }

            Undo.RecordObject(explorer, "5d explorer");
            explorer.moveMode = MoveMode.AutoWalk;
            explorer.walkSeconds = 0.4f;
            explorer.runSeconds = 0.2f;
            explorer.cubeSize = 0.018f;
            explorer.showDebugLabels = false;
            if (input != null && input.grabGuard == null) input.grabGuard = handle;
            foreach (var probe in Object.FindObjectsByType<ThumbHoldProbe>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                probe.enabled = false;

            var old = GameObject.Find("Tile Input");
            if (old != null) Object.DestroyImmediate(old);
            var go = new GameObject("Tile Input");
            var cursor = go.AddComponent<TileCursor>();
            cursor.island = island;
            cursor.explorer = explorer;
            cursor.hudMaterial = hud;
            var touch = go.AddComponent<TileTouch>();
            touch.island = island;
            touch.explorer = explorer;
            touch.cursor = cursor;
            touch.grabGuard = handle;
            touch.bubble = bubble;
            var gaze = go.AddComponent<TileGaze>();
            gaze.island = island;
            gaze.explorer = explorer;
            gaze.cursor = cursor;
            gaze.grabGuard = handle;
            gaze.bubble = bubble;

            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            Debug.Log($"[InputPolish] MR_Main: AutoWalk 0.4/0.2 s, thumb probe off, Tile Input added{(handle == null ? " (no IslandHandle: no grab guard)" : "")}{(bubble == null ? " (no dialogue bubble)" : "")}.");
        }
    }
}
