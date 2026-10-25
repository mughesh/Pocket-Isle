using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 8: the Still Isle hub (map, props, theme, visitors tables) becomes the island's world; the test map is
    /// renamed Cinderwood and gets a return gate; Shard Gates link the two (fire open, water and wind sealed); the
    /// scripted first encounter. Run after Set Up MR_Main (7c). Safe to re-run (it rewrites the hub map and props).
    /// </summary>
    static class Step8Setup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";
        const string OldTestPath = "Assets/_Project/Data/WorldMap_Test.asset";
        const string CinderwoodPath = "Assets/_Project/Data/WorldMap_Cinderwood.asset";
        const string PortalMaterialPath = "Assets/_Project/Materials/Props/Portal.mat";
        const char ReturnGateKey = 'Z';
        /// <summary>Cinderwood's return gate (anchor, 2 x 1), two tiles right of its start; you arrive in front of it.</summary>
        static readonly Vector2Int CinderwoodGate = new Vector2Int(30, 26);

        static readonly Color FireColor = new Color(1f, 0.55f, 0.22f);
        static readonly Color WaterColor = new Color(0.36f, 0.72f, 1f);
        static readonly Color WindColor = new Color(0.86f, 0.95f, 1f);
        static readonly Color HomeColor = new Color(1f, 0.85f, 0.48f);

        [MenuItem("Tools/Pocket Isle/Set Up MR_Main (8 hub)", priority = 47)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step8] Stop Play mode first.");
                return;
            }
            var log = new StringBuilder("[Step8] MR_Main set up:");
            var hub = StillIsleTools.Write(log);
            var cinderwood = Cinderwood(log);
            if (cinderwood == null) return;
            Link(hub, cinderwood, log);

            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            var swap = Object.FindFirstObjectByType<SceneSwap>(FindObjectsInactive.Include);
            var director = Object.FindFirstObjectByType<EncounterDirector>(FindObjectsInactive.Include);
            if (island == null || swap == null || director == null)
            {
                Debug.LogError("[Step8] MR_Main needs the island, the scene swap (7b) and the encounter director.");
                return;
            }

            // The map creatures (starters near Cinderwood's start) belong to Cinderwood.
            int bound = 0;
            foreach (var mc in Object.FindObjectsByType<MapCreature>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mc.map == cinderwood) continue;
                Undo.RecordObject(mc, "Step 8 hub");
                mc.map = cinderwood;
                EditorUtility.SetDirty(mc);
                bound++;
            }
            if (bound > 0) log.Append($"\n  {bound} map creature(s) now only show on {cinderwood.name}");

            Undo.RecordObject(island, "Step 8 hub");
            island.world = hub;
            EditorUtility.SetDirty(island);
            island.Rebuild();
            log.Append($"\n  the island shows {hub.name} (new games start there)");

            Undo.RecordObject(swap, "Step 8 hub");
            swap.worlds = new List<WorldMap> { hub, cinderwood };
            swap.realm = Object.FindFirstObjectByType<RealmRestoration>(FindObjectsInactive.Include);
            EditorUtility.SetDirty(swap);
            log.Append($"\n  scene swap: worlds {hub.name} + {cinderwood.name} (map-to-map travel), realm restoration {(swap.realm != null ? "linked" : "MISSING")}");

            Undo.RecordObject(director, "Step 8 hub");
            director.firstSpecies = AssetDatabase.LoadAssetAtPath<IslingSpecies>("Assets/_Project/Data/Creatures/Species/Toastling.asset");
            director.firstLevel = 3;
            EditorUtility.SetDirty(director);
            log.Append("\n  first encounter of a new game: Toastling Lv 3 on the first tall grass step");

            var gates = Object.FindFirstObjectByType<ShardGates>(FindObjectsInactive.Include);
            if (gates == null)
            {
                var go = new GameObject("Shard Gates");
                Undo.RegisterCreatedObjectUndo(go, "Shard Gates");
                gates = go.AddComponent<ShardGates>();
            }
            var shrines = Object.FindFirstObjectByType<HearthShrines>(FindObjectsInactive.Include);
            gates.island = island;
            gates.window = island.GetComponent<WorldWindow>();
            gates.explorer = swap.explorer;
            gates.swap = swap;
            gates.pods = Object.FindFirstObjectByType<PodRack>(FindObjectsInactive.Include);
            gates.fx = Object.FindFirstObjectByType<BattleFx>(FindObjectsInactive.Include);
            gates.grabGuard = shrines != null ? shrines.grabGuard : null;
            gates.portalMaterial = PortalMaterial();
            EditorUtility.SetDirty(gates);
            log.Append("\n  'Shard Gates' (portals, walk in or look + pinch, sealed bump, arrival lines)");

            WireOpening(island, swap, director, log);
            var tour = Object.FindFirstObjectByType<PerfTour>(FindObjectsInactive.Include);
            if (tour != null)
            {
                tour.opening = Object.FindFirstObjectByType<Opening>(FindObjectsInactive.Include);
                tour.gates = gates;
                EditorUtility.SetDirty(tour);
                log.Append("\n  Perf Tour: step 8 tour (perftour8.txt: opening, hub with the statue, gate travel)");
            }

            DialogueTools.Import();
            log.Append("\n  Script_v1 reimported (gate and opening lines)");

            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }

        /// <summary>The test map, renamed Cinderwood (same GUID), with its tables, realm id, arrival line and return gate.</summary>
        static WorldMap Cinderwood(StringBuilder log)
        {
            if (AssetDatabase.LoadAssetAtPath<WorldMap>(CinderwoodPath) == null && AssetDatabase.LoadAssetAtPath<WorldMap>(OldTestPath) != null)
            {
                string error = AssetDatabase.MoveAsset(OldTestPath, CinderwoodPath);
                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogError($"[Step8] Renaming the test map failed: {error}");
                    return null;
                }
                log.Append($"\n  {OldTestPath} renamed to {CinderwoodPath} (same GUID; old saves migrate)");
            }
            var map = AssetDatabase.LoadAssetAtPath<WorldMap>(CinderwoodPath);
            if (map == null)
            {
                Debug.LogError($"[Step8] No {CinderwoodPath}.");
                return null;
            }
            map.realm = "cinderwood";
            map.arrivalLine = "realm.cinderwood.first";
            map.grassTable = AssetDatabase.LoadAssetAtPath<EncounterTable>("Assets/_Project/Data/Battle/Encounters_TestGrass.asset");
            map.waterTable = AssetDatabase.LoadAssetAtPath<EncounterTable>("Assets/_Project/Data/Battle/Encounters_TestWater.asset");

            var gate = PropKit.Load("ReturnGate");
            map.propLegend.RemoveAll(e => e.Key == ReturnGateKey);
            map.propLegend.Add(new PropLegend { key = ReturnGateKey.ToString(), recipe = gate });
            var rows = new List<char[]>();
            for (int r = 0; r < map.Rows; r++)
            {
                var row = new char[map.Columns];
                for (int c = 0; c < row.Length; c++)
                {
                    char k = map.PropAt(r, c);
                    row[c] = k == ReturnGateKey ? IslandMap.Empty : k;
                }
                rows.Add(row);
            }
            var g = CinderwoodGate;
            bool free = true;
            for (int dx = 0; dx < 2; dx++)
            {
                var t = g + new Vector2Int(dx, 0);
                var front = g + new Vector2Int(dx, 1);
                if (map.At(t) != IslandMap.Grass || rows[t.y][t.x] != IslandMap.Empty) free = false;
                if (!IslandMap.IsWalkable(map.At(front)) || rows[front.y][front.x] != IslandMap.Empty) free = false;
            }
            if (!free) Debug.LogError($"[Step8] Cinderwood's return gate spot c{g.x} r{g.y} isn't free grass with a walkable front: placed anyway, check the map.");
            rows[g.y][g.x] = ReturnGateKey;
            var sb = new StringBuilder();
            for (int r = 0; r < rows.Count; r++)
            {
                sb.Append(rows[r]);
                if (r < rows.Count - 1) sb.Append('\n');
            }
            map.SetProps(sb.ToString());
            EditorUtility.SetDirty(map);
            log.Append($"\n  {map.name}: realm cinderwood, test tables, return gate c{g.x} r{g.y} (arrive c{g.x} r{g.y + 1})");
            return map;
        }

        /// <summary>Hub fire gate <-> Cinderwood return gate; hub water and wind sealed.</summary>
        static void Link(WorldMap hub, WorldMap cinderwood, StringBuilder log)
        {
            hub.realm = "stillisle";
            hub.arrivalLine = "hub.return.first";
            var toHub = StillIsleTools.FireGate + new Vector2Int(1, 1);
            var toCinder = CinderwoodGate + new Vector2Int(0, 1);
            hub.gates = new List<GateLink>
            {
                new GateLink { id = "fire", tile = StillIsleTools.FireGate, to = cinderwood, arrive = toCinder, arriveFacing = new Vector2Int(0, 1), portal = FireColor, firstSeenLine = "gate.fire.first" },
                new GateLink { id = "water", tile = StillIsleTools.WaterGate, isSealed = true, portal = WaterColor },
                new GateLink { id = "wind", tile = StillIsleTools.WindGate, isSealed = true, portal = WindColor },
            };
            cinderwood.gates = new List<GateLink>
            {
                new GateLink { id = "home", tile = CinderwoodGate, to = hub, arrive = toHub, arriveFacing = new Vector2Int(0, 1), portal = HomeColor },
            };
            EditorUtility.SetDirty(hub);
            EditorUtility.SetDirty(cinderwood);
            AssetDatabase.SaveAssets();
            log.Append($"\n  gates: hub fire c{StillIsleTools.FireGate.x} r{StillIsleTools.FireGate.y} <-> Cinderwood home c{CinderwoodGate.x} r{CinderwoodGate.y}; water and wind sealed");
        }

        // ---- C: the opening ----

        const string ShardMaterialPath = "Assets/_Project/Materials/Props/OpeningShard.mat";

        static void WireOpening(IslandBuilder island, SceneSwap swap, EncounterDirector director, StringBuilder log)
        {
            var opening = Object.FindFirstObjectByType<Opening>(FindObjectsInactive.Include);
            if (opening == null)
            {
                var go = new GameObject("Opening");
                Undo.RegisterCreatedObjectUndo(go, "Opening");
                opening = go.AddComponent<Opening>();
            }
            opening.island = island;
            opening.placer = Object.FindFirstObjectByType<IslandPlacer>(FindObjectsInactive.Include);
            opening.handle = Object.FindFirstObjectByType<IslandHandle>(FindObjectsInactive.Include);
            opening.explorer = swap.explorer;
            opening.party = director.party;
            opening.pods = director.pods;
            opening.spirit = Object.FindFirstObjectByType<LanternSpirit>(FindObjectsInactive.Include);
            opening.fx = Object.FindFirstObjectByType<BattleFx>(FindObjectsInactive.Include);
            opening.save = Object.FindFirstObjectByType<GameSave>(FindObjectsInactive.Include);
            opening.shardMaterial = ShardMaterial();
            IslingSpecies S(string id) => AssetDatabase.LoadAssetAtPath<IslingSpecies>($"Assets/_Project/Data/Creatures/Species/{id}.asset");
            opening.starters = new[] { S("Kilnpot"), S("Pebbloop"), S("Pufflet") };
            opening.podScale = 3.2f;
            opening.podRow = new Vector3(0.11f, 0.1f, 0.26f);
            EditorUtility.SetDirty(opening);
            bool ok = opening.placer != null && opening.handle != null && opening.spirit != null && opening.save != null && opening.pods != null;
            log.Append($"\n  'Opening' (new game: shard, unfold, Spirit, move prompt, starter pods Kilnpot / Pebbloop / Pufflet; returning: 2 s rise){(ok ? "" : " - SOME REFERENCES MISSING")}");
        }

        /// <summary>Mossy grey stone with gold cracks (Diorama/Toon), outline on.</summary>
        static Material ShardMaterial()
        {
            var shader = Shader.Find("Diorama/Toon");
            var m = AssetDatabase.LoadAssetAtPath<Material>(ShardMaterialPath);
            if (m == null)
            {
                m = new Material(shader) { name = "OpeningShard" };
                AssetDatabase.CreateAsset(m, ShardMaterialPath);
            }
            m.SetColor("_BaseColor", new Color(0.62f, 0.6f, 0.55f));
            m.SetColor("_TopColor", new Color(0.5f, 0.64f, 0.37f));
            m.SetColor("_CrackColor", new Color(1f, 0.78f, 0.35f));
            m.SetFloat("_CrackOn", 1f);
            m.SetFloat("_Grad", 1f);
            m.SetFloat("_CrackScale", 3.2f);
            m.SetFloat("_CrackWidth", 0.05f);
            m.SetFloat("_Emissive", 0.2f);
            m.SetFloat("_OutlineWidth", 0.03f);
            m.SetColor("_OutlineColor", new Color(0.16f, 0.15f, 0.13f));
            m.enableInstancing = true;
            CreatureBaker.SetOutlinePass(m, true);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Deletes the save and enters Play: the opening plays (a new game).</summary>
        [MenuItem("Tools/Pocket Isle/New Game", priority = 30)]
        static void NewGame()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step8] Stop Play mode first (stopping autosaves, so delete after stopping).");
                return;
            }
            SaveGame.Delete(GameSave.SavePath);
            Debug.Log($"[Step8] New game: deleted {GameSave.SavePath}; entering Play (N skips the opening).");
            EditorApplication.isPlaying = true;
        }

        static Material PortalMaterial()
        {
            var shader = Shader.Find("Diorama/Portal");
            if (shader == null) Debug.LogError("[Step8] Shader Diorama/Portal not found.");
            var m = AssetDatabase.LoadAssetAtPath<Material>(PortalMaterialPath);
            if (m == null)
            {
                m = new Material(shader) { name = "Portal" };
                AssetDatabase.CreateAsset(m, PortalMaterialPath);
            }
            else if (shader != null && m.shader != shader) m.shader = shader;
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
