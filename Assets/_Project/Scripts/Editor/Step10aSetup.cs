using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 10a: Galewick, the land. Writes and bakes the wind props and theme, writes the 60 x 60 map with its zones,
    /// tables and landmarks, links the hub's wind gate to Galewick's return gate (sealed in builds until
    /// GalewickConfig.open), registers the map with the scene swap, the gates and the dev panel, adds the drifting seeds
    /// and runs the layout checks. Safe to re-run.
    /// </summary>
    public static class Step10aSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";

        [MenuItem("Tools/Pocket Isle/Set Up MR_Main (10a Galewick)", priority = 60)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step10a] Stop Play mode first.");
                return;
            }
            var log = new StringBuilder("[Step10a] MR_Main set up:");
            Props(log);
            var config = GalewickTools.Config();
            log.Append($"\n  {GalewickTools.ConfigPath}: builds open {config.open}, Editor open {config.openInEditor}");
            var map = GalewickTools.Write(log);
            if (map == null) return;
            var hub = AssetDatabase.LoadAssetAtPath<WorldMap>(StillIsleTools.MapPath);
            if (hub == null)
            {
                Debug.LogError("[Step10a] No hub map (run Set Up MR_Main (8 hub) first).");
                return;
            }
            Step8Setup.LinkGalewick(hub, map, log);

            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            Scene(map, config, log);
            DialogueTools.Import();
            log.Append("\n  Script_v1 reimported (realm.galewick.first, gw.*.first)");

            var problems = GalewickTools.Check(map, log);
            foreach (var p in problems) Debug.LogError($"[Step10a] Layout check: {p}");
            log.Append(problems.Count == 0 ? "\n  layout checks: all passed" : $"\n  layout checks: {problems.Count} problem(s), see the errors");

            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            AssetDatabase.SaveAssets();
            IslandBuilder.RebuildAll();
            Debug.Log(log.ToString());
        }

        /// <summary>Scene wiring: travel list, gates' config, dev panel map, seeds drift.</summary>
        static void Scene(WorldMap map, GalewickConfig config, StringBuilder log)
        {
            var swap = Object.FindFirstObjectByType<SceneSwap>(FindObjectsInactive.Include);
            if (swap != null)
            {
                Undo.RecordObject(swap, "Step 10a");
                if (!swap.worlds.Contains(map)) swap.worlds.Add(map);
                EditorUtility.SetDirty(swap);
                log.Append($"\n  scene swap worlds: {string.Join(", ", swap.worlds.ConvertAll(w => w != null ? w.name : "null"))}");
            }
            else Debug.LogError("[Step10a] No SceneSwap in MR_Main.");

            var gates = Object.FindFirstObjectByType<ShardGates>(FindObjectsInactive.Include);
            if (gates != null)
            {
                Undo.RecordObject(gates, "Step 10a");
                gates.galewick = config;
                EditorUtility.SetDirty(gates);
            }
            else Debug.LogError("[Step10a] No ShardGates in MR_Main.");

            var state = Object.FindFirstObjectByType<GameState>(FindObjectsInactive.Include);
            if (state != null)
            {
                Undo.RecordObject(state, "Step 10a");
                state.galewick = map;
                EditorUtility.SetDirty(state);
                log.Append("\n  dev panel: Galewick spots (GW: arrival, village, big mill, kite field, ridge, tower, shrine, rare, fire) + restored toggle");
            }
            else Debug.LogError("[Step10a] No GameState in MR_Main (Set Up MR_Main (9e)).");

            var island = Object.FindFirstObjectByType<IslandBuilder>(FindObjectsInactive.Include);
            if (island != null)
            {
                // Seeds drift over restored Galewick (dandelion fluff rising off the tall grass and hills).
                var seeds = Step6FixesSetup.Drift(island, "Drifting Seeds", RealmDrift.Kind.Seeds);
                seeds.treeWeight = 0.35f;
                seeds.tallGrassWeight = 0.8f;
                seeds.fireWeight = 0f;
                seeds.groundWeight = 0.3f;
                seeds.waterWeight = 0f;
                var mat = Step6FixesSetup.DriftMaterial("DriftingSeeds", 0f, new Color(0.98f, 0.97f, 0.9f), new Color(1f, 1f, 0.95f), 0.0035f,
                    new Vector2(0.1f, 0.2f), new Vector2(5f, 8f), 0.7f, UnityEngine.Rendering.BlendMode.One, UnityEngine.Rendering.BlendMode.One);
                seeds.GetComponent<MeshRenderer>().sharedMaterial = mat;
                EditorUtility.SetDirty(seeds);
                log.Append("\n  'Drifting Seeds' drift under the island (shows only where the theme asks: Galewick restored)");
            }
        }

        /// <summary>Writes and bakes the 10a props. Logs triangles per prop.</summary>
        public static Dictionary<string, PropRecipe> Props(StringBuilder log)
        {
            var props = PropKit.WriteGalewickProps();
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

        /// <summary>Props only (bridge): quick look iterations.</summary>
        public static void PropsOnly()
        {
            var log = new StringBuilder("[Step10a] Galewick props written and baked:");
            Props(log);
            Debug.Log(log.ToString());
        }

        /// <summary>Rewrites only the map (no props bake, no scene) and runs the checks.</summary>
        public static void MapOnly()
        {
            var log = new StringBuilder("[Step10a] Galewick map:");
            var map = GalewickTools.Write(log);
            if (map == null) return;
            var problems = GalewickTools.Check(map, log);
            foreach (var p in problems) log.Append($"\n  PROBLEM: {p}");
            IslandBuilder.RebuildAll();
            Debug.Log(log.ToString());
        }
    }
}
