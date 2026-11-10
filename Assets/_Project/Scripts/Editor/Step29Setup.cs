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
