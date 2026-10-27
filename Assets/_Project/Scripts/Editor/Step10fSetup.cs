using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 10f: Cinderwood's hand moments. Writes and bakes their props and wires under "Hand Puzzles": A, cup the
    /// village flame (FlameCupPiece on the kiln village's Hearth Shrine). Safe to re-run.
    /// </summary>
    public static class Step10fSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";
        const string RootName = "Hand Puzzles";

        // WarmSpots slots (Cinderwood): the village, the six hollow lanterns, the forge.
        public const int VillageSlot = 0;

        public const string FlameId = "cw.flame";
        /// <summary>The kiln village plaza's middle (x, y), where the flame's warm pulse settles.</summary>
        public static readonly Vector2 VillageXY = new Vector2(15.5f, 32.5f);

        [MenuItem("Tools/Pocket Isle/Set Up MR_Main (10f Cinderwood hands)", priority = 67)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step10f] Stop Play mode first.");
                return;
            }
            var log = new StringBuilder("[Step10f] MR_Main set up:");
            Props(log);
            var map = AssetDatabase.LoadAssetAtPath<WorldMap>(CinderwoodTools.MapPath);
            if (map == null)
            {
                Debug.LogError("[Step10f] No Cinderwood map (Set Up MR_Main (9a Cinderwood) first).");
                return;
            }
            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            var root = Root();
            Flame(root, map, log);
            DialogueTools.Import();
            log.Append("\n  Script_v1 reimported (cw.* lines)");

            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            AssetDatabase.SaveAssets();
            IslandBuilder.RebuildAll();
            Debug.Log(log.ToString());
        }

        public static void Props(StringBuilder log)
        {
            var props = PropKit.WriteCinderwoodMomentProps();
            AssetDatabase.SaveAssets();
            foreach (var r in props.Values)
            {
                var report = PropBaker.Bake(r);
                log.Append($"\n  {r.name}: {report.triangles} tris (budget {r.triangleBudget})");
            }
            AssetDatabase.SaveAssets();
        }

        static Transform Root()
        {
            var top = GameObject.Find(RootName);
            if (top == null)
            {
                top = new GameObject(RootName);
                Undo.RegisterCreatedObjectUndo(top, RootName);
            }
            var t = top.transform.Find("Cinderwood");
            if (t == null)
            {
                var go = new GameObject("Cinderwood");
                Undo.RegisterCreatedObjectUndo(go, "Cinderwood");
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
            Undo.RecordObject(c, "Step 10f");
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

        static string Baked(params (string name, PropRecipe r)[] recipes)
        {
            var missing = new List<string>();
            foreach (var (n, r) in recipes)
                if (r == null || r.bakedMesh == null) missing.Add(n);
            return missing.Count > 0 ? $"; NOT BAKED: {string.Join(", ", missing)}" : "";
        }

        // ---- A: cup the flame ----

        static void Flame(Transform root, WorldMap map, StringBuilder log)
        {
            var cup = Child<FlameCupPiece>(root, "Village Flame");
            Common(cup, map);
            cup.id = FlameId;
            cup.reach = 4;
            cup.lessonLine = "cw.flame";
            cup.lessonChip = "Cup it!";
            cup.ghostClip = "cup_hands";
            cup.ghostScale = 1f;
            cup.shrine = CinderwoodTools.Tile(CinderwoodTools.ShrineXY);
            cup.coldShrine = PropKit.Load("HearthShrineCold");
            cup.wisp = PropKit.Load("ShrineWisp");
            cup.wispBase = 1.36f;
            cup.wispSize = new Vector2(0.75f, 1.4f);
            cup.warmSlot = VillageSlot;
            cup.villageCentre = new Vector2(VillageXY.x, CinderwoodTools.Size - 1 - VillageXY.y);
            cup.villageRadius = 10f;
            cup.villageEdge = 1.5f;
            EditorUtility.SetDirty(cup);
            log.Append($"\n  'Village Flame' {Wired(cup)}: shrine c{cup.shrine.x} r{cup.shrine.y}, cup {cup.radius * 100f:0} cm / {cup.facingAngle:0} deg " +
                       $"(two hands {cup.twoHandSeconds} s, one hand {cup.oneHandSeconds} s, look + pinch {cup.lookSeconds} s), warmth {cup.villageRadius} tiles round " +
                       $"c{cup.villageCentre.x} r{cup.villageCentre.y}; lesson cw.flame + ghost cup_hands" +
                       Baked(("HearthShrineCold", cup.coldShrine), ("ShrineWisp", cup.wisp)));
        }
    }
}
