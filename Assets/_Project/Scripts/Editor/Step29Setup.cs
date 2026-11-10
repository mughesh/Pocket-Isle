using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>Step 29 setup (bridge): the pod HP ring material, the healing blooms (recipes, bake, landmark tiles, scene).</summary>
    public static class Step29Setup
    {
        const string RingMaterialPath = "Assets/_Project/Data/Resources/PodRing.mat";
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";

        [MenuItem("Tools/Pocket Isle/Setup/Pod HP Rings + Healing Blooms (29)")]
        public static void All()
        {
            RingMaterial();
            Blooms();
            FishingSpots();
        }

        const string GwWaterPath = "Assets/_Project/Data/Battle/Galewick/Encounters_GwWater.asset";

        /// <summary>
        /// Step 29 D20: Galewick had no water table (a tap at its river did nothing). A small one from Lilymere's water species
        /// at Galewick's levels, two marked banks by the river (soft ripples), and the scene's <see cref="FishSpots"/>.
        /// </summary>
        public static void FishingSpots()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Step29Setup] Stop Play first.");
                return;
            }
            var gw = AssetDatabase.LoadAssetAtPath<WorldMap>("Assets/_Project/Data/WorldMap_Galewick.asset");
            var lm = AssetDatabase.LoadAssetAtPath<EncounterTable>("Assets/_Project/Data/Battle/Lilymere/Encounters_LmWater.asset");
            if (gw == null || lm == null)
            {
                Debug.LogError("[Step29Setup] Galewick map or Lilymere's water table missing.");
                return;
            }
            var table = AssetDatabase.LoadAssetAtPath<EncounterTable>(GwWaterPath);
            if (table == null)
            {
                table = ScriptableObject.CreateInstance<EncounterTable>();
                AssetDatabase.CreateAsset(table, GwWaterPath);
            }
            table.habitat = Habitat.Water;
            var entries = new List<EncounterTable.Entry>();
            for (int i = 0; i < lm.entries.Length && i < 2; i++)
            {
                var e = lm.entries[i];
                e.minLevel = 6;
                e.maxLevel = 8;
                e.weight = i == 0 ? 60 : 40;
                e.rare = false;
                entries.Add(e);
            }
            table.entries = entries.ToArray();
            EditorUtility.SetDirty(table);
            gw.waterTable = table;

            // Two banks by the river: walkable, not a bridge, not tall grass, no prop, facing water, apart from each other.
            gw.landmarks.RemoveAll(l => l.name != null && l.name.StartsWith(Fishing.SpotPrefix));
            var picks = new List<Vector2Int>();
            var bridge = new List<Vector2Int>();
            for (int r = 0; r < gw.Rows; r++)
                for (int c = 0; c < gw.Columns; c++)
                    if (gw.At(r, c) == IslandMap.Path && Fishing.Bridge(gw, new Vector2Int(c, r))) bridge.Add(new Vector2Int(c, r));
            Vector2 cross = bridge.Count > 0 ? new Vector2((float)bridge.Average(t => t.x), (float)bridge.Average(t => t.y)) : new Vector2(gw.Columns * 0.5f, gw.Rows * 0.5f);
            var banks = new List<Vector2Int>();
            for (int r = 0; r < gw.Rows; r++)
                for (int c = 0; c < gw.Columns; c++)
                {
                    var t = new Vector2Int(c, r);
                    if (!Fishing.IsBank(gw, t) || gw.At(t) == IslandMap.TallGrass || gw.PropAt(r, c) != IslandMap.Empty) continue;
                    if (gw.landmarks.Any(l => l.tile == t) || gw.gates.Any(g => g.Covers(t) || g.Front(0) == t || g.Front(1) == t)) continue;
                    float d = Vector2.Distance(cross, t);
                    if (d < 4f || d > 12f) continue;
                    banks.Add(t);
                }
            foreach (var t in banks.OrderBy(t => Mathf.Abs(Vector2.Distance(cross, t) - 6f)))
            {
                if (picks.Any(p => Mathf.Abs(p.x - t.x) < 6)) continue;
                picks.Add(t);
                if (picks.Count == 2) break;
            }
            for (int i = 0; i < picks.Count; i++)
                gw.landmarks.Add(new Landmark { name = $"{Fishing.SpotPrefix}{i + 1}", tile = picks[i] });
            EditorUtility.SetDirty(gw);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Step29Setup] Galewick water table set; fishing banks: {string.Join(", ", picks.Select(t => $"c{t.x} r{t.y}"))} (bridge at c{cross.x:0} r{cross.y:0}).");

            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.path.EndsWith("MR_Main.unity")) scene = EditorSceneManager.OpenScene(ScenePath);
            var spots = Object.FindFirstObjectByType<FishSpots>(FindObjectsInactive.Include);
            if (spots == null) spots = new GameObject("Fish Spots").AddComponent<FishSpots>();
            spots.island = Object.FindFirstObjectByType<IslandBuilder>(FindObjectsInactive.Include);
            spots.fx = Object.FindFirstObjectByType<BattleFx>(FindObjectsInactive.Include);
            EditorUtility.SetDirty(spots);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>
        /// Step 29 F: the hand moments' chips in the scene, plainer (one action, verb first, the thing named). By the old
        /// text, so a re-run changes nothing. Logs before -> after.
        /// </summary>
        public static void Chips()
        {
            var map = new Dictionary<string, string>
            {
                { "Push!", "Poke the pads together" }, { "Palm down!", "Turn your palm down" }, { "Sweep!", "Sweep the clouds away" },
                { "Sweep", "Sweep the clouds away" }, { "Cup it!", "Hold your hands round it" }, { "Palm down", "Hold your palm over it" },
                { "Touch them!", "Poke the lanterns" }, { "Pull it up!", "Pinch a kite, lift it" }, { "Scoop!", "Scoop water, pour it" },
                { "Pull apart!", "Pinch a knot, pull" }, { "Flick!", "Flick the sails" },
            };
            var counters = new Dictionary<string, string> { { "Sweep \u00b7 Clouds", "Clouds" }, { "Palm down \u00b7 Pads", "Pads" } };
            var scene = EditorSceneManager.GetActiveScene();
            var log = new System.Text.StringBuilder();
            foreach (var p in Object.FindObjectsByType<HandPuzzle>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                string was = p.lessonChip;
                if (p is ForgeEmberPiece || p is VineBurnPiece) p.lessonChip = "Touch the ember";
                else if (p is CarryPiece && was == "Pick it up!") p.lessonChip = "Grab the log, lift it";
                else if (!string.IsNullOrEmpty(was) && map.TryGetValue(was, out var now)) p.lessonChip = now;
                string c = p.counterLabel;
                foreach (var kv in counters)
                    if (c == System.Text.RegularExpressions.Regex.Unescape(kv.Key)) p.counterLabel = kv.Value;
                if (was != p.lessonChip || c != p.counterLabel)
                {
                    log.Append($"\n  {p.GetType().Name} {p.id}: '{was}' -> '{p.lessonChip}'{(c != p.counterLabel ? $", counter '{c}' -> '{p.counterLabel}'" : "")}");
                    EditorUtility.SetDirty(p);
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Step29Setup] Chips:{log}");
        }

        /// <summary>Step 29: the healing bloom's height in the scene (tiles).</summary>
        public static void BloomHeight()
        {
            var bloom = Object.FindFirstObjectByType<HealBloom>(FindObjectsInactive.Include);
            if (bloom == null) return;
            bloom.heightTiles = 1.4f;
            EditorUtility.SetDirty(bloom);
            EditorSceneManager.MarkSceneDirty(bloom.gameObject.scene);
            EditorSceneManager.SaveScene(bloom.gameObject.scene);
        }

        /// <summary>Resources/PodRing.mat (Diorama/Pod Ring), loaded by PodRack at runtime.</summary>
        public static void RingMaterial()
        {
            var shader = Shader.Find("Diorama/Pod Ring");
            if (shader == null)
            {
                Debug.LogError("[Step29Setup] Shader Diorama/Pod Ring not found.");
                return;
            }
            var mat = AssetDatabase.LoadAssetAtPath<Material>(RingMaterialPath);
            if (mat == null)
            {
                mat = new Material(shader) { name = "PodRing" };
                AssetDatabase.CreateAsset(mat, RingMaterialPath);
            }
            mat.shader = shader;
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Step29Setup] {RingMaterialPath} ready.");
        }

        /// <summary>
        /// The bloom tile on a map: on the walk from the start tile to the Keeper's door, the road tile
        /// <paramref name="back"/> steps before the door's front; beside it a plain grass tile (no prop, no tall grass
        /// within a tile, not next to the door), on the side away from the seat when both are free. Pure over the map.
        /// </summary>
        public static Vector2Int? PickTile(WorldMap map, RectInt door, int back = 6)
        {
            // Walking distance from the door's front ring (over walkable tiles).
            var dist = new Dictionary<Vector2Int, int>();
            var q = new Queue<Vector2Int>();
            foreach (var t in WalkUp.Ring(door))
                if (map.CanWalk(t)) { dist[t] = 0; q.Enqueue(t); }
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                if (dist[c] > back + 6) continue;
                foreach (var d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                {
                    var n = c + d;
                    if (dist.ContainsKey(n) || !map.CanWalk(n)) continue;
                    dist[n] = dist[c] + 1;
                    q.Enqueue(n);
                }
            }
            Vector2Int? best = null;
            float bestScore = float.MaxValue;
            foreach (var kv in dist)
            {
                var t = kv.Key;
                if (kv.Value < back - 2 || kv.Value > back + 3) continue;
                if (map.At(t) != IslandMap.Grass || map.PropAt(t.y, t.x) != IslandMap.Empty || WalkUp.Distance(t, door) <= 2) continue;
                bool road = false, grassNear = false;
                foreach (var d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                    if (map.At(t + d) == IslandMap.Path) road = true;
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                        if (map.At(t + new Vector2Int(dx, dy)) == IslandMap.TallGrass) grassNear = true;
                if (!road || grassNear) continue;
                if (map.gates.Any(g => g.Covers(t) || g.Front(0) == t || g.Front(1) == t)) continue;
                if (map.landmarks.Any(l => l.tile == t)) continue;
                // Closest to "back" steps out; then toward the seat (+row), so it isn't hidden behind the road.
                float score = Mathf.Abs(kv.Value - back) * 10f - t.y * 0.1f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = t;
                }
            }
            return best;
        }

        /// <summary>Recipes, bake, a landmark per realm map, the scene component. Re-run safe.</summary>
        public static void Blooms()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Step29Setup] Stop Play first.");
                return;
            }
            var recipes = PropKit.WriteHealBlooms();
            AssetDatabase.SaveAssets();
            foreach (var r in recipes)
            {
                var report = PropBaker.Bake(r);
                Debug.Log($"[Step29Setup] Baked {r.name}: {(r.bakedMesh != null ? r.bakedMesh.triangles.Length / 3 : 0)} tris.");
            }
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.path.EndsWith("MR_Main.unity")) scene = EditorSceneManager.OpenScene(ScenePath);
            var doors = Step27Setup.SceneDoors();
            var realms = new Dictionary<string, PropRecipe> { { "galewick", recipes[0] }, { "cinderwood", recipes[1] }, { "lilymere", recipes[2] } };
            foreach (var map in Step27Setup.Maps())
            {
                if (!realms.ContainsKey(map.realm ?? "")) continue;
                if (!doors.TryGetValue(map.name, out var door))
                {
                    Debug.LogError($"[Step29Setup] {map.name}: no Keeper door in the scene; no bloom.");
                    continue;
                }
                map.landmarks.RemoveAll(l => l.name == HealBloom.LandmarkName);
                var tile = PickTile(map, door);
                if (!tile.HasValue)
                {
                    Debug.LogError($"[Step29Setup] {map.name}: no free grass tile beside the road before the door; no bloom.");
                    continue;
                }
                map.landmarks.Add(new Landmark { name = HealBloom.LandmarkName, tile = tile.Value });
                EditorUtility.SetDirty(map);
                Debug.Log($"[Step29Setup] {map.name}: healing bloom at c{tile.Value.x} r{tile.Value.y} (door at c{door.x} r{door.y}).");
            }
            AssetDatabase.SaveAssets();

            var bloom = Object.FindFirstObjectByType<HealBloom>(FindObjectsInactive.Include);
            if (bloom == null)
            {
                var go = new GameObject("Heal Blooms");
                bloom = go.AddComponent<HealBloom>();
            }
            bloom.island = Object.FindFirstObjectByType<IslandBuilder>(FindObjectsInactive.Include);
            bloom.explorer = Object.FindFirstObjectByType<GestureTestPiece>(FindObjectsInactive.Include);
            bloom.party = Object.FindFirstObjectByType<Party>(FindObjectsInactive.Include);
            bloom.pods = Object.FindFirstObjectByType<PodRack>(FindObjectsInactive.Include);
            bloom.stage = Object.FindFirstObjectByType<BattleStage>(FindObjectsInactive.Include);
            bloom.grabGuard = Object.FindFirstObjectByType<IslandHandle>(FindObjectsInactive.Include);
            bloom.material = AssetDatabase.LoadAssetAtPath<Material>(Step14cSetup.LogoMaterialPath);
            bloom.kinds = realms.Select(kv => new HealBloom.Kind { realm = kv.Key, recipe = kv.Value }).ToArray();
            EditorUtility.SetDirty(bloom);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Step29Setup] Healing blooms set up, scene saved.");
        }
    }
}
