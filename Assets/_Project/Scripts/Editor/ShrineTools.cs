using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 7: the Hearth Shrine prop (written, baked) and two shrines on a world map: one 2-5 tiles from the start and
    /// one near the tall grass closest to the start, each on free grass with a walkable front tile (+row, where the
    /// explorer stands to rest). Re-running moves them (earlier shrines are removed first).
    /// </summary>
    static class ShrineTools
    {
        public const char ShrineKey = 'Y';

        /// <summary>Writes and bakes the prop, places the shrines; returns a report line.</summary>
        public static string SetUp(WorldMap world)
        {
            var recipe = PropKit.WriteHearthShrine();
            AssetDatabase.SaveAssets();
            PropBaker.Bake(recipe);
            return "HearthShrine prop written and baked; " + Place(world, recipe);
        }

        static string Place(WorldMap world, PropRecipe recipe)
        {
            world.propLegend.RemoveAll(e => e.Key == ShrineKey);
            world.propLegend.Add(new PropLegend { key = ShrineKey.ToString(), recipe = recipe });

            int rows = world.Rows, cols = world.Columns;
            var props = new char[rows, cols];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    char k = world.PropAt(r, c);
                    props[r, c] = k == ShrineKey ? IslandMap.Empty : k;
                }
            var start = world.startTile;
            var reserved = new HashSet<Vector2Int>();
            void Reserve(Vector2Int t, int radius)
            {
                for (int dy = -radius; dy <= radius; dy++)
                    for (int dx = -radius; dx <= radius; dx++)
                        reserved.Add(t + new Vector2Int(dx, dy));
            }
            Reserve(start, 1);
            foreach (var mc in Object.FindObjectsByType<MapCreature>(FindObjectsSortMode.None)) Reserve(mc.homeTile, 1);

            bool In(Vector2Int t) => t.x > 1 && t.y > 1 && t.x < cols - 2 && t.y < rows - 2;
            bool Walk(Vector2Int t) => In(t) && IslandMap.IsWalkable(world.At(t)) && props[t.y, t.x] == IslandMap.Empty;
            bool Free(Vector2Int t)
            {
                var front = t + Vector2Int.up;
                if (!In(t) || world.At(t) != IslandMap.Grass || reserved.Contains(t) || !Walk(front)) return false;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                        if (props[t.y + dy, t.x + dx] != IslandMap.Empty) return false;
                return true;
            }
            Vector2Int? Pick(Vector2Int center, int from, int to, System.Func<Vector2Int, bool> extra)
            {
                for (int d = from; d <= to; d++)
                    foreach (var t in Ring(center, d).OrderBy(t => (t - center).sqrMagnitude))
                        if (Free(t) && extra(t)) return t;
                return null;
            }

            var placed = new List<Vector2Int>();
            var a = Pick(start, 2, 5, t => true);
            if (a.HasValue) placed.Add(a.Value);

            Vector2Int? grass = null;
            for (int d = 1; d < Mathf.Max(rows, cols) && grass == null; d++)
                foreach (var t in Ring(start, d))
                    if (In(t) && world.At(t) == IslandMap.TallGrass)
                    {
                        grass = t;
                        break;
                    }
            Vector2Int? b = grass.HasValue
                ? Pick(grass.Value, 1, 4, t => placed.All(p => Mathf.Max(Mathf.Abs(p.x - t.x), Mathf.Abs(p.y - t.y)) >= 4))
                : null;
            if (b.HasValue) placed.Add(b.Value);
            foreach (var t in placed) props[t.y, t.x] = ShrineKey;

            var sb = new System.Text.StringBuilder();
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++) sb.Append(props[r, c]);
                if (r < rows - 1) sb.Append('\n');
            }
            world.SetProps(sb.ToString());
            EditorUtility.SetDirty(world);
            AssetDatabase.SaveAssets();
            string Where(Vector2Int? t) => t.HasValue ? $"c{t.Value.x} r{t.Value.y} (rest on c{t.Value.x} r{t.Value.y + 1})" : "NOT PLACED (no free tile)";
            return $"shrines (props key '{ShrineKey}'): near spawn {Where(a)}; near the tall grass at " +
                   $"{(grass.HasValue ? $"c{grass.Value.x} r{grass.Value.y}" : "?")}: {Where(b)}";
        }

        static IEnumerable<Vector2Int> Ring(Vector2Int center, int d)
        {
            for (int dy = -d; dy <= d; dy++)
                for (int dx = -d; dx <= d; dx++)
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) == d) yield return center + new Vector2Int(dx, dy);
        }
    }
}
