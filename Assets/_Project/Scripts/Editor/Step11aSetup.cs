using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 11a: Lilymere, the land. Writes and bakes the water props and theme, writes the 60 x 60 map with its zones,
    /// tables and landmarks, links the hub's well gate to Lilymere's return gate (sealed in builds until
    /// LilymereConfig.open), registers the map with the scene swap, the gates and the dev panel, adds the lagoon's level
    /// (LilymereLagoon) and runs the layout checks. Safe to re-run.
    /// </summary>
    public static class Step11aSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";

        [MenuItem("Tools/Pocket Isle/Set Up MR_Main (11a Lilymere)", priority = 62)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step11a] Stop Play mode first.");
                return;
            }
            var log = new StringBuilder("[Step11a] MR_Main set up:");
            Props(log);
            var config = LilymereTools.Config();
            log.Append($"\n  {LilymereTools.ConfigPath}: builds open {config.open}, Editor open {config.openInEditor}");
            var map = LilymereTools.Write(log);
            if (map == null) return;
            var hub = AssetDatabase.LoadAssetAtPath<WorldMap>(StillIsleTools.MapPath);
            if (hub == null)
            {
                Debug.LogError("[Step11a] No hub map (run Set Up MR_Main (8 hub) first).");
                return;
            }
            Step8Setup.LinkLilymere(hub, map, log);

            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            Scene(map, config, log);
            Diorama.Audio.EditorTools.AmbienceTools.CreateProfiles();
            log.Append("\n  ambience profiles: LilymereFaded / LilymereRestored (amb.pond bed until a Lilymere one is made)");
            DialogueTools.Import();
            log.Append("\n  Script_v1 reimported (gate.water.first, realm.lilymere.first, lm.*.first)");

            var problems = LilymereTools.Check(map, log);
            foreach (var p in problems) Debug.LogError($"[Step11a] Layout check: {p}");
            log.Append(problems.Count == 0 ? "\n  layout checks: all passed" : $"\n  layout checks: {problems.Count} problem(s), see the errors");

            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            AssetDatabase.SaveAssets();
            IslandBuilder.RebuildAll();
            Debug.Log(log.ToString());
        }

        /// <summary>Scene wiring: travel list, gates' config, dev panel map, the lagoon level.</summary>
        static void Scene(WorldMap map, LilymereConfig config, StringBuilder log)
        {
            var swap = Object.FindFirstObjectByType<SceneSwap>(FindObjectsInactive.Include);
            if (swap != null)
            {
                Undo.RecordObject(swap, "Step 11a");
                if (!swap.worlds.Contains(map)) swap.worlds.Add(map);
                EditorUtility.SetDirty(swap);
                log.Append($"\n  scene swap worlds: {string.Join(", ", swap.worlds.ConvertAll(w => w != null ? w.name : "null"))}");
            }
            else Debug.LogError("[Step11a] No SceneSwap in MR_Main.");

            var gates = Object.FindFirstObjectByType<ShardGates>(FindObjectsInactive.Include);
            if (gates != null)
            {
                Undo.RecordObject(gates, "Step 11a");
                gates.lilymere = config;
                EditorUtility.SetDirty(gates);
            }
            else Debug.LogError("[Step11a] No ShardGates in MR_Main.");

            var state = Object.FindFirstObjectByType<GameState>(FindObjectsInactive.Include);
            if (state != null)
            {
                Undo.RecordObject(state, "Step 11a");
                state.lilymere = map;
                EditorUtility.SetDirty(state);
                log.Append("\n  dev panel: Lilymere spots (LM: arrival, village, shrine, channel, garden, basin, temple, marsh, rare pond) + restored toggle");
            }
            else Debug.LogError("[Step11a] No GameState in MR_Main (Set Up MR_Main (9e)).");

            var island = Object.FindFirstObjectByType<IslandBuilder>(FindObjectsInactive.Include);
            if (island != null)
            {
                var lagoon = Object.FindFirstObjectByType<LilymereLagoon>(FindObjectsInactive.Include);
                if (lagoon == null)
                {
                    var go = new GameObject("Lilymere Lagoon");
                    Undo.RegisterCreatedObjectUndo(go, "Lilymere Lagoon");
                    lagoon = go.AddComponent<LilymereLagoon>();
                }
                lagoon.island = island;
                lagoon.map = map;
                EditorUtility.SetDirty(lagoon);
                log.Append("\n  'Lilymere Lagoon': the dry ring turns to water once lm.lagoon is solved or the realm restored");
            }
        }

        /// <summary>Writes and bakes the 11a props. Logs triangles per prop.</summary>
        public static Dictionary<string, PropRecipe> Props(StringBuilder log)
        {
            var props = PropKit.WriteLilymereProps();
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
            var log = new StringBuilder("[Step11a] Lilymere props written and baked:");
            Props(log);
            Debug.Log(log.ToString());
        }

        /// <summary>Rewrites only the map (no props bake, no scene) and runs the checks.</summary>
        public static void MapOnly()
        {
            var log = new StringBuilder("[Step11a] Lilymere map:");
            var map = LilymereTools.Write(log);
            if (map == null) return;
            var problems = LilymereTools.Check(map, log);
            foreach (var p in problems) log.Append($"\n  PROBLEM: {p}");
            IslandBuilder.RebuildAll();
            Debug.Log(log.ToString());
        }

        /// <summary>Bridge: the map as text (Logs/lilymere_map.txt): ground, then props.</summary>
        public static void DumpMap()
        {
            var map = AssetDatabase.LoadAssetAtPath<WorldMap>(LilymereTools.MapPath);
            if (map == null) return;
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText("Logs/lilymere_map.txt", map.layout + "\n\n" + map.props);
            Debug.Log("[Step11a] Wrote Logs/lilymere_map.txt.");
        }
    }
}
