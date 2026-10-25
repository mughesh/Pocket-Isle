using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 9a: Cinderwood, the land. Writes and bakes the 9a props (C), rewrites the Cinderwood map at 60 x 60 with its
    /// zones and per-zone encounter tables (A, B; the old test map is archived once), relinks the hub's fire gate,
    /// moves the three map creatures into the kiln village and runs the layout checks. Safe to re-run.
    /// </summary>
    public static class Step9aSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";

        [MenuItem("Tools/Pocket Isle/Set Up MR_Main (9a Cinderwood)", priority = 53)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step9a] Stop Play mode first.");
                return;
            }
            var log = new StringBuilder("[Step9a] MR_Main set up:");
            Props(log);
            var map = CinderwoodTools.Write(log);
            if (map == null) return;
            var hub = AssetDatabase.LoadAssetAtPath<WorldMap>(StillIsleTools.MapPath);
            if (hub == null)
            {
                Debug.LogError("[Step9a] No hub map (run Set Up MR_Main (8 hub) first).");
                return;
            }
            Step8Setup.Link(hub, map, log);

            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                active = EditorSceneManager.OpenScene(ScenePath);
            }

            // The three map creatures (starters) now stand in the kiln village plaza.
            var homes = new[] { CinderwoodTools.Tile(14, 34), CinderwoodTools.Tile(17, 33), CinderwoodTools.Tile(13, 32) };
            int i = 0;
            foreach (var mc in Object.FindObjectsByType<MapCreature>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID))
            {
                if (mc.map != map || i >= homes.Length) continue;
                if (!map.CanWalk(homes[i])) Debug.LogError($"[Step9a] Village tile c{homes[i].x} r{homes[i].y} for {mc.name} isn't walkable.");
                Undo.RecordObject(mc, "Step 9a");
                mc.homeTile = homes[i++];
                EditorUtility.SetDirty(mc);
            }
            log.Append($"\n  {i} map creature(s) moved to the kiln village plaza");

            var problems = CinderwoodTools.Check(map, log);
            foreach (var p in problems) Debug.LogError($"[Step9a] Layout check: {p}");
            log.Append(problems.Count == 0 ? "\n  layout checks: all passed" : $"\n  layout checks: {problems.Count} problem(s), see the errors");

            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }

        /// <summary>Writes and bakes the 9a props (and the rebuilt AutumnTree). Logs triangles per prop.</summary>
        public static Dictionary<string, PropRecipe> Props(StringBuilder log)
        {
            var props = PropKit.WriteCinderwoodProps();
            AssetDatabase.SaveAssets();
            foreach (var r in props.Values)
            {
                var report = PropBaker.Bake(r);
                log.Append($"\n  {r.name}: {report.triangles} tris (budget {r.triangleBudget})");
            }
            AssetDatabase.SaveAssets();
            IslandBuilder.RebuildAll();
            return props;
        }

        [MenuItem("Tools/Pocket Isle/Props/Write + Bake Cinderwood Props (9a)", priority = 24)]
        public static void PropsMenu()
        {
            var log = new StringBuilder("[Step9a] Cinderwood props written and baked:");
            Props(log);
            Debug.Log(log.ToString());
        }

        /// <summary>Rewrites only the map (no props bake, no scene) and runs the checks: quick iterations.</summary>
        public static void MapOnly()
        {
            var log = new StringBuilder("[Step9a] Cinderwood map:");
            var map = CinderwoodTools.Write(log);
            if (map == null) return;
            var problems = CinderwoodTools.Check(map, log);
            foreach (var p in problems) log.Append($"\n  PROBLEM: {p}");
            IslandBuilder.RebuildAll();
            Debug.Log(log.ToString());
        }
    }
}
