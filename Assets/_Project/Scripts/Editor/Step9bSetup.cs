using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 9b: guardian-hand puzzles. Runs the 9a setup first (it rewrites the Cinderwood map and reimports the script),
    /// then wires the fallen log (Carry). Safe to re-run.
    /// </summary>
    public static class Step9bSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";
        const string RootName = "Hand Puzzles";

        /// <summary>The two rock ledges beside the log gap (x, y): a spot's centre is its anchor + half a tile.</summary>
        public static readonly Vector2Int[] LedgeXY = { new Vector2Int(18, 45), new Vector2Int(22, 45) };
        /// <summary>The log lies this high on a ledge's rocks (tiles).</summary>
        const float LedgeHeight = 0.18f;

        [MenuItem("Tools/Pocket Isle/Set Up MR_Main (9b puzzles)", priority = 54)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step9b] Stop Play mode first.");
                return;
            }
            Step9aSetup.SetUp();
            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            var log = new StringBuilder("[Step9b] MR_Main set up:");
            var map = AssetDatabase.LoadAssetAtPath<WorldMap>(CinderwoodTools.MapPath);
            if (map == null)
            {
                Debug.LogError("[Step9b] No Cinderwood map (the 9a setup failed?).");
                return;
            }
            var root = Root();
            FallenLog(root, map, log);

            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }

        static Transform Root()
        {
            var go = GameObject.Find(RootName);
            if (go == null)
            {
                go = new GameObject(RootName);
                Undo.RegisterCreatedObjectUndo(go, RootName);
            }
            return go.transform;
        }

        static T Child<T>(Transform root, string name) where T : Component
        {
            var t = root.Find(name);
            if (t == null)
            {
                var go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, name);
                go.transform.SetParent(root, false);
                t = go.transform;
            }
            var c = t.GetComponent<T>();
            if (c == null) c = Undo.AddComponent<T>(t.gameObject);
            return c;
        }

        /// <summary>The scene refs every piece needs.</summary>
        static void Common(HandPuzzle p, WorldMap map)
        {
            p.map = map;
            p.island = Object.FindFirstObjectByType<IslandBuilder>(FindObjectsInactive.Include);
            p.window = Object.FindFirstObjectByType<WorldWindow>(FindObjectsInactive.Include);
            p.explorer = Object.FindFirstObjectByType<GestureTestPiece>(FindObjectsInactive.Include);
            p.grabGuard = Object.FindFirstObjectByType<IslandHandle>(FindObjectsInactive.Include);
            p.ghosts = Object.FindFirstObjectByType<GhostHands>(FindObjectsInactive.Include);
            p.fx = Object.FindFirstObjectByType<BattleFx>(FindObjectsInactive.Include);
        }

        static string Missing(HandPuzzle p) =>
            p.island == null || p.explorer == null || p.window == null ? "MISSING island / explorer / window" :
            p.ghosts == null ? "no ghost hands (no ghost lesson)" : "wired";

        // ---- B: the fallen log ----

        static void FallenLog(Transform root, WorldMap map, StringBuilder log)
        {
            var piece = Child<CarryPiece>(root, "Fallen Log");
            var listener = piece.GetComponent<FallenLog>();
            if (listener == null) listener = Undo.AddComponent<FallenLog>(piece.gameObject);
            Undo.RecordObject(piece, "Step 9b");
            Undo.RecordObject(listener, "Step 9b");
            Common(piece, map);
            var anchor = CinderwoodTools.LogTile;
            if (!map.TryGetProp(map.PropAt(anchor.y, anchor.x), out var entry) || entry.recipe == null || entry.recipe.name != "FallenLog")
            {
                log.Append($"\n  fallen log: NO FallenLog prop at c{anchor.x} r{anchor.y}");
                return;
            }
            piece.id = "cinder.log";
            piece.reach = 3;
            piece.lessonLine = "cinder.log.hands";
            piece.ghostClip = "grab_drop";
            piece.ghostScale = 1f;
            piece.recipe = entry.recipe;
            var f = WorldMap.FootprintOf(entry);
            piece.home = anchor + new Vector2((f.x - 1) * 0.5f, (f.y - 1) * 0.5f);
            piece.homeHeight = 0f;
            piece.homeYaw = (entry.turned ? 90f : 0f) + entry.yaw;
            piece.halfLength = Mathf.Max(f.x, f.y) * 0.5f - 0.1f;
            piece.spots = new List<CarrySpot>();
            foreach (var xy in LedgeXY)
            {
                var t = CinderwoodTools.Tile(xy);
                piece.spots.Add(new CarrySpot { tile = t + new Vector2(0.5f, 0f), height = LedgeHeight, yaw = piece.homeYaw });
            }
            piece.spotMaterial = Step7bSetup.BeaconMaterial();
            listener.piece = piece;
            listener.anchor = anchor;
            listener.island = piece.island;
            listener.explorer = piece.explorer;
            listener.fx = piece.fx;
            EditorUtility.SetDirty(piece);
            EditorUtility.SetDirty(listener);
            log.Append($"\n  'Fallen Log' {Missing(piece)}: home c{piece.home.x} r{piece.home.y}, ledges " +
                       string.Join(", ", piece.spots.ConvertAll(s => $"c{s.tile.x} r{s.tile.y}")) + ", lesson cinder.log.hands + ghost grab_drop");
        }
    }
}
