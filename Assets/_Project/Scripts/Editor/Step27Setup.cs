using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

namespace Diorama.EditorTools
{
    /// <summary>Step 27 D: tall grass that hides a door becomes plain grass, by hand in the saved maps (not through a generator, which would re-roll them).</summary>
    public static class Step27Setup
    {
        /// <summary>Every door on a map, from the map and the scene's door scripts (footprint in tiles).</summary>
        public static List<(string name, RectInt rect, bool wedge)> Doors(WorldMap map, Dictionary<string, RectInt> sceneDoors)
        {
            var list = new List<(string, RectInt, bool)>();
            foreach (var g in map.gates) list.Add(($"gate {g.id}", new RectInt(g.tile.x, g.tile.y, 2, 1), true));
            for (int r = 0; r < map.Rows; r++)
                for (int c = 0; c < map.Columns; c++)
                    if (map.TryGetProp(map.PropAt(r, c), out var e) && e.recipe != null && e.recipe.name == HearthShrines.RecipeName)
                        list.Add(($"shrine c{c} r{r}", new RectInt(c, r, 1, 1), false));
            if (sceneDoors.TryGetValue(map.name, out var door)) list.Add(("Keeper door", door, true));
            return list;
        }

        /// <summary>The Keeper doors in the open scene, by map name.</summary>
        public static Dictionary<string, RectInt> SceneDoors()
        {
            var doors = new Dictionary<string, RectInt>();
            var tower = Object.FindFirstObjectByType<TowerDoor>(FindObjectsInactive.Include);
            if (tower != null && tower.map != null) doors[tower.map.name] = new RectInt(tower.tower.x, tower.tower.y, 2, 2);
            var forge = Object.FindFirstObjectByType<ForgeBellows>(FindObjectsInactive.Include);
            if (forge != null && forge.pump != null && forge.pump.map != null) doors[forge.pump.map.name] = new RectInt(forge.door.x, forge.door.y, 3, 1);
            var temple = Object.FindFirstObjectByType<TempleDoor>(FindObjectsInactive.Include);
            if (temple != null && temple.map != null && temple.front.Length > 0) doors[temple.map.name] = new RectInt(temple.front[0].x - 1, temple.front[0].y - 1, 3, 1);
            return doors;
        }

        /// <summary>The playable maps (not the archive).</summary>
        public static IEnumerable<WorldMap> Maps()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:WorldMap", new[] { "Assets/_Project/Data" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("/Archive/")) continue;
                var map = AssetDatabase.LoadAssetAtPath<WorldMap>(path);
                if (map != null) yield return map;
            }
        }

        /// <summary>Bridge: the tall grass near every door becomes grass; logs each tile.</summary>
        public static void ClearDoorGrass()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/MR_Main.unity");
            var scene = SceneDoors();
            int total = 0;
            foreach (var map in Maps())
            {
                var rows = map.layout.Replace("\r", "").Split('\n');
                var log = new StringBuilder();
                int n = 0;
                foreach (var (name, rect, wedge) in Doors(map, scene))
                    for (int y = Mathf.Max(0, rect.yMin - 2); y < rows.Length; y++)
                        for (int x = Mathf.Max(0, rect.xMin - 8); x < rows[y].Length && x < rect.xMax + 8; x++)
                        {
                            if (rows[y][x] != IslandMap.TallGrass || !DoorClearance.Clears(rect, new Vector2Int(x, y), wedge)) continue;
                            rows[y] = rows[y].Substring(0, x) + IslandMap.Grass + rows[y].Substring(x + 1);
                            log.Append($" c{x} r{y} ({name})");
                            n++;
                        }
                if (n == 0)
                {
                    Debug.Log($"[Step27Setup] {map.name}: nothing to clear.");
                    continue;
                }
                map.SetLayout(string.Join("\n", rows));
                EditorUtility.SetDirty(map);
                Debug.Log($"[Step27Setup] {map.name}: {n} tall grass tile(s) to grass:{log}.");
                total += n;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Step27Setup] Door grass cleared: {total} tile(s).");
        }
    }
}
