using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 9b: guardian-hand puzzles. Runs the 9a setup first (it writes and bakes the Cinderwood props including the
    /// 9b forge ones, rewrites the map and reimports the script), then wires the fallen log (Carry), the forge
    /// bellows (Pump) and writes the Forge Hall interior behind the forge door. Safe to re-run.
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
            Forge(root, map, log);
            Hall(root, log);

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

        // ---- D: the Forge Hall (placeholder until 9c) ----

        static void Hall(Transform root, StringBuilder log)
        {
            var hallMap = ForgeHallTools.WriteHall(log);
            var swap = Object.FindFirstObjectByType<SceneSwap>(FindObjectsInactive.Include);
            if (swap == null)
            {
                log.Append("\n  Forge Hall: NO scene swap in the scene");
                return;
            }
            Undo.RecordObject(swap, "Step 9b");
            swap.interiors.RemoveAll(m => m == null);
            if (!swap.interiors.Contains(hallMap)) swap.interiors.Add(hallMap);
            EditorUtility.SetDirty(swap);

            var hall = Child<ForgeHall>(root, "Forge Hall");
            Undo.RecordObject(hall, "Step 9b");
            hall.swap = swap;
            hall.hall = hallMap;
            hall.island = Object.FindFirstObjectByType<IslandBuilder>(FindObjectsInactive.Include);
            hall.explorer = Object.FindFirstObjectByType<GestureTestPiece>(FindObjectsInactive.Include);
            hall.ringMaterial = Step7bSetup.BeaconMaterial();
            hall.doorTiles = System.Array.ConvertAll(ForgeHallTools.DoorTiles, t => (Vector2)t);
            hall.keeperSpot = ForgeHallTools.KeeperSpot;
            EditorUtility.SetDirty(hall);

            var forge = root.GetComponentInChildren<ForgeBellows>(true);
            if (forge != null)
            {
                Undo.RecordObject(forge, "Step 9b");
                forge.hall = hallMap;
                forge.hallEntry = ForgeHallTools.EntryTile;
                EditorUtility.SetDirty(forge);
            }
            log.Append($"\n  'Forge Hall' wired: in through the open forge door (entry c{ForgeHallTools.EntryTile.x} r{ForgeHallTools.EntryTile.y}), " +
                       $"out on the door tiles; Keeper ring at c{hall.keeperSpot.x} r{hall.keeperSpot.y}; {(forge != null ? "the forge door leads in" : "NO forge to lead in")}");
        }

        // ---- C: the forge (bellows, pipes, braziers, door) ----

        static void Forge(Transform root, WorldMap map, StringBuilder log)
        {
            var pump = Child<PumpPiece>(root, "Forge Bellows");
            var forge = pump.GetComponent<ForgeBellows>();
            if (forge == null) forge = Undo.AddComponent<ForgeBellows>(pump.gameObject);
            Undo.RecordObject(pump, "Step 9b");
            Undo.RecordObject(forge, "Step 9b");
            Common(pump, map);
            var bellows = CinderwoodTools.Tile(CinderwoodTools.BellowsXY);
            pump.id = "cinder.bellows";
            pump.reach = 4;
            pump.lessonLine = "cinder.bellows.first";
            pump.ghostClip = "pump_handle";
            pump.ghostScale = 1f;
            pump.handleRecipe = PropKit.Load("BellowsHandle");
            // Shape +X is map west (lower columns), +Z the player's side (higher rows).
            pump.hinge = bellows + new Vector2(-PropKit.BellowsHinge.x, PropKit.BellowsHinge.z);
            pump.hingeHeight = PropKit.BellowsHinge.y;
            pump.armLength = PropKit.HandleArm;
            pump.armYaw = PropKit.HandleYaw;
            pump.upAngle = PropKit.HandleRestAngle;
            pump.downAngle = -12f;
            pump.spring = 4f;
            pump.grabRadius = 0.55f;
            pump.strokesNeeded = 12;
            pump.checkpoint = 4;
            pump.coolDelay = 4f;
            pump.coolStep = 2f;

            forge.pump = pump;
            forge.island = pump.island;
            forge.explorer = pump.explorer;
            forge.swap = Object.FindFirstObjectByType<SceneSwap>(FindObjectsInactive.Include);
            forge.fx = pump.fx;
            forge.bellows = bellows;
            forge.braziers = System.Array.ConvertAll(CinderwoodTools.BrazierXY, CinderwoodTools.Tile);
            forge.door = CinderwoodTools.Tile(CinderwoodTools.DoorXY);
            forge.bodyRecipe = PropKit.Load("BellowsBody");
            forge.brazierCold = PropKit.Load("Brazier");
            forge.brazierWarm = PropKit.Load("BrazierWarm");
            forge.brazierLit = PropKit.Load("BrazierLit");
            forge.doorFrame = PropKit.Load("ForgeDoorFrame");
            forge.doorLeaf = PropKit.Load("ForgeDoorLeaf");
            forge.pipeHeight = PropKit.PipeHeight;
            forge.pipeBack = PropKit.PipeBack;
            EditorUtility.SetDirty(pump);
            EditorUtility.SetDirty(forge);

            var missing = new List<string>();
            foreach (var (n, r) in new[] { ("BellowsHandle", pump.handleRecipe), ("BellowsBody", forge.bodyRecipe), ("Brazier", forge.brazierCold),
                         ("BrazierWarm", forge.brazierWarm), ("BrazierLit", forge.brazierLit), ("ForgeDoorFrame", forge.doorFrame), ("ForgeDoorLeaf", forge.doorLeaf) })
                if (r == null || r.bakedMesh == null) missing.Add(n);
            log.Append($"\n  'Forge Bellows' {Missing(pump)}: hinge c{pump.hinge.x:0.##} r{pump.hinge.y:0.##}, braziers " +
                       string.Join(", ", System.Array.ConvertAll(forge.braziers, b => $"c{b.x} r{b.y}")) + $", door c{forge.door.x} r{forge.door.y}, " +
                       $"{pump.strokesNeeded} pumps ({pump.checkpoint} per brazier), lesson cinder.bellows.first + ghost pump_handle" +
                       (missing.Count > 0 ? $"; NOT BAKED: {string.Join(", ", missing)}" : ""));
        }
    }
}
