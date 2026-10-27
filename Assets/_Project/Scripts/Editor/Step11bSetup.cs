using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 11b: Lilymere's hand moments. Writes and bakes the 11b props (the crossings' covers, the lotus stone, the water
    /// ball, the basin's water), rewrites the Lilymere map with the covers on the two crossings (same seed), and wires under
    /// "Hand Puzzles/Lilymere": A, the lily pad bridge (LilyPadBridge, lm.pads); B, the rain hand (RainGarden, lm.rain);
    /// C, scoop and pour (ScoopPour, lm.lagoon: the lagoon rises). Safe to re-run.
    /// </summary>
    public static class Step11bSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";
        const string RootName = "Hand Puzzles";
        const int Size = LilymereTools.Size;

        static Vector2 Map(float x, float y) => new Vector2(x, Size - 1 - y);
        static Vector2Int MapI(Vector2Int xy) => new Vector2Int(xy.x, Size - 1 - xy.y);

        [MenuItem("Tools/Pocket Isle/Set Up MR_Main (11b Lilymere hands)", priority = 63)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step11b] Stop Play mode first.");
                return;
            }
            var log = new StringBuilder("[Step11b] MR_Main set up:");
            Step11aSetup.Props(log);
            var map = LilymereTools.Write(log);
            if (map == null) return;
            var problems = LilymereTools.Check(map, log);
            foreach (var p in problems) Debug.LogError($"[Step11b] Layout check: {p}");
            log.Append(problems.Count == 0 ? "\n  layout checks: all passed (the covers gate the channel and the garden)" : $"\n  layout checks: {problems.Count} problem(s)");
            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            var root = Root();
            Pads(root, map, log);
            Rain(root, map, log);
            Scoop(root, map, log);
            DialogueTools.Import();
            log.Append("\n  Script_v1 reimported (lm.pads, lm.rain, lm.scoop lines)");
            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            AssetDatabase.SaveAssets();
            IslandBuilder.RebuildAll();
            Debug.Log(log.ToString());
        }

        static Transform Root()
        {
            var top = GameObject.Find(RootName);
            if (top == null)
            {
                top = new GameObject(RootName);
                Undo.RegisterCreatedObjectUndo(top, RootName);
            }
            var t = top.transform.Find("Lilymere");
            if (t == null)
            {
                var go = new GameObject("Lilymere");
                Undo.RegisterCreatedObjectUndo(go, "Lilymere");
                go.transform.SetParent(top.transform, false);
                t = go.transform;
            }
            return t;
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
            Undo.RecordObject(c, "Step 11b");
            return c;
        }

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

        static string Wired(HandPuzzle p) =>
            p.island == null || p.explorer == null || p.window == null ? "MISSING island / explorer / window" :
            p.ghosts == null ? "no ghost hands (no ghost lesson)" : "wired";

        // ---- A: the lily pad bridge ----

        static void Pads(Transform root, WorldMap map, StringBuilder log)
        {
            var pads = Child<LilyPadBridge>(root, "Lily Pad Bridge");
            Common(pads, map);
            pads.id = "lm.pads";
            pads.reach = 4;
            pads.lessonLine = "lm.pads";
            pads.lessonChip = "Push!";
            pads.ghostClip = "poke";
            var c = LilymereTools.CrossingXY;
            // One slot per channel row across the 2-wide crossing; the pads start drifting a few tiles either side.
            pads.slots = new[] { Map(c.x + 0.5f, LilymereTools.ChannelY0), Map(c.x + 0.5f, LilymereTools.ChannelY0 + 1), Map(c.x + 0.5f, LilymereTools.ChannelY1) };
            pads.padStart = new[] { Map(c.x - 3.5f, LilymereTools.ChannelY0 + 1), Map(c.x + 4.5f, LilymereTools.ChannelY0), Map(c.x + 6f, LilymereTools.ChannelY1) };
            float row0 = Size - 1 - LilymereTools.ChannelY1 - 0.5f, row1 = Size - 1 - LilymereTools.ChannelY0 + 0.5f;
            pads.channel = Rect.MinMaxRect(c.x - 9.5f, row0, c.x + 10.5f, row1);
            pads.gapTiles = LilymereTools.GateTiles(false).Select(MapI).ToArray();
            pads.padRecipe = PropKit.Load("LilyPad");
            pads.guideMaterial = Step7bSetup.BeaconMaterial();
            EditorUtility.SetDirty(pads);
            log.Append($"\n  'Lily Pad Bridge' {Wired(pads)}: {pads.slots.Length} slots at c{pads.slots[0].x:0.#}, {pads.gapTiles.Length} covered tiles; push {pads.touchRadius * 100f:0.#} cm, " +
                       $"drag {pads.drag}, snap {pads.snapRadius} tiles");
        }

        // ---- B: the rain hand ----

        static void Rain(Transform root, WorldMap map, StringBuilder log)
        {
            var rain = Child<RainGarden>(root, "Rain Garden");
            Common(rain, map);
            rain.id = "lm.rain";
            rain.reach = 5;
            rain.lessonLine = "lm.rain";
            rain.lessonChip = "Palm down!";
            rain.ghostClip = "palm_sweep";
            var gates = LilymereTools.GateTiles(true);
            float y0 = gates.Min(g => g.y), y1 = gates.Max(g => g.y);
            float x = LilymereTools.GardenPathX + 0.5f;
            rain.buds = new Vector2[5];
            for (int i = 0; i < 5; i++) rain.buds[i] = Map(x, Mathf.Lerp(y0, y1, i / 4f));
            rain.gardenCenter = Map(LilymereTools.GardenCenter.x, LilymereTools.GardenCenter.y);
            rain.gardenRadius = new Vector2(LilymereTools.GardenRx + 1.5f, LilymereTools.GardenRy + 1.5f);
            rain.gapTiles = gates.Select(MapI).ToArray();
            rain.budRecipe = PropKit.Load("Lotus");
            rain.stoneRecipe = PropKit.Load("LotusStone");
            rain.cloudRecipe = PropKit.Load("CloudPuff");
            EditorUtility.SetDirty(rain);
            log.Append($"\n  'Rain Garden' {Wired(rain)}: {rain.buds.Length} buds, {rain.gapTiles.Length} covered tiles; palm down within {rain.maxAngle:0} deg, " +
                       $"{rain.minHeight * 100f:0}-{rain.maxHeight * 100f:0} cm up, {rain.formSeconds} s to form, wiggle {rain.wiggleSpeed} m/s; bud {rain.softSeconds} s ({rain.hardSeconds} s hard)" +
                       (rain.cloudRecipe == null ? "; NO CloudPuff (Galewick props)" : ""));
        }

        // ---- C: scoop and pour ----

        static void Scoop(Transform root, WorldMap map, StringBuilder log)
        {
            var scoop = Child<ScoopPour>(root, "Scoop And Pour");
            Common(scoop, map);
            scoop.id = LilymereLagoon.RisenId;
            scoop.reach = 6;
            scoop.lessonLine = "lm.scoop";
            scoop.lessonChip = "Scoop!";
            scoop.ghostClip = "grab_drop";
            var b = LilymereTools.BasinXY;
            scoop.basin = Map(b.x + 0.5f, b.y - 0.5f);
            var w = LilymereTools.WaterfallXY;
            scoop.waterfallAnchor = MapI(w);
            scoop.waterfallCenter = Map(w.x + 0.5f, w.y);
            // The lagoon's open water nearest the basin, and the temple's door.
            var basinXY = new Vector2(b.x + 0.5f, b.y - 0.5f);
            Vector2Int best = default;
            float bestD = float.MaxValue;
            for (int r = 0; r < map.Rows; r++)
                for (int col = 0; col < map.Columns; col++)
                {
                    if (map.Raw(r, col) != IslandMap.Water) continue;
                    var xy = new Vector2(col, Size - 1 - r);
                    if ((xy - LilymereTools.LagoonCenter).magnitude > LilymereTools.LagoonRx && xy.x < LilymereTools.InletX0) continue;
                    float d = (xy - basinXY).magnitude;
                    if (d < bestD)
                    {
                        bestD = d;
                        best = new Vector2Int(col, r);
                    }
                }
            scoop.scoopSpot = best;
            var t = LilymereTools.TempleXY;
            scoop.templeDoor = Map(t.x + 1f, t.y - 1f);
            scoop.ballRecipe = PropKit.Load("WaterBall");
            scoop.basinWaterRecipe = PropKit.Load("BasinWater");
            scoop.runningRecipe = PropKit.Load("WaterfallRunning");
            EditorUtility.SetDirty(scoop);
            log.Append($"\n  'Scoop And Pour' {Wired(scoop)}: basin c{scoop.basin.x:0.#} r{scoop.basin.y:0.#}, scoop spot c{best.x} r{best.y} ({bestD:0.0} tiles off), " +
                       $"cup up {scoop.cupUp}, keep {scoop.keepUp}, tip {scoop.tipUp}, dip {scoop.dipDepth * 100f:0} cm, lift {scoop.liftHeight * 100f:0} cm, {scoop.poursNeeded} pours");
        }
    }
}
