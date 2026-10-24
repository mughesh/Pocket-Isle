using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Tile pathfinding for the explorer: A* over walkable tiles (<see cref="IslandMap.CanWalk"/>), 4-direction steps,
    /// paths up to <see cref="DefaultMaxSteps"/> steps. Tiles are (column, row).
    /// </summary>
    public static class ExplorerPath
    {
        public const int DefaultMaxSteps = 40;

        static readonly Vector2Int[] Steps = { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };

        // Reused between searches (main thread only).
        static readonly Dictionary<Vector2Int, Vector2Int> CameFrom = new Dictionary<Vector2Int, Vector2Int>();
        static readonly Dictionary<Vector2Int, int> Cost = new Dictionary<Vector2Int, int>();
        static readonly List<(int f, int h, Vector2Int tile)> Open = new List<(int, int, Vector2Int)>();

        /// <summary>
        /// Fills <paramref name="path"/> with the tiles to walk from <paramref name="from"/> to <paramref name="to"/>
        /// (excluding the start, including the goal). False when the goal is blocked, unreachable or more than
        /// <paramref name="maxSteps"/> steps away; <paramref name="path"/> is then empty.
        /// </summary>
        public static bool Find(IslandMap map, Vector2Int from, Vector2Int to, List<Vector2Int> path, int maxSteps = DefaultMaxSteps)
        {
            path.Clear();
            if (map == null || from == to || !map.CanWalk(to) || Manhattan(from, to) > maxSteps) return false;
            CameFrom.Clear();
            Cost.Clear();
            Open.Clear();
            Cost[from] = 0;
            Push(Manhattan(from, to), Manhattan(from, to), from);
            while (Open.Count > 0)
            {
                var tile = Pop();
                if (tile == to) break;
                int g = Cost[tile] + 1;
                if (g > maxSteps) continue;
                foreach (var s in Steps)
                {
                    var n = tile + s;
                    if (Cost.TryGetValue(n, out int old) && old <= g) continue;
                    // Don't wander further than the step budget allows (keeps the search small on a big map).
                    int h = Manhattan(n, to);
                    if (g + h > maxSteps || !map.CanWalk(n)) continue;
                    Cost[n] = g;
                    CameFrom[n] = tile;
                    Push(g + h, h, n);
                }
            }
            if (!CameFrom.ContainsKey(to)) return false;
            for (var t = to; t != from; t = CameFrom[t]) path.Add(t);
            path.Reverse();
            return true;
        }

        /// <summary>True when <paramref name="to"/> can be reached within <paramref name="maxSteps"/> steps.</summary>
        public static bool CanReach(IslandMap map, Vector2Int from, Vector2Int to, int maxSteps = DefaultMaxSteps) =>
            from == to || Find(map, from, to, Scratch, maxSteps);

        static readonly List<Vector2Int> Scratch = new List<Vector2Int>();

        static int Manhattan(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        // Small open list: a sorted insert is plenty for a 40-step budget. Ties prefer the tile closer to the goal.
        static void Push(int f, int h, Vector2Int tile)
        {
            int i = Open.Count;
            while (i > 0 && (Open[i - 1].f < f || Open[i - 1].f == f && Open[i - 1].h < h)) i--;
            Open.Insert(i, (f, h, tile));
        }

        static Vector2Int Pop()
        {
            var last = Open[Open.Count - 1];
            Open.RemoveAt(Open.Count - 1);
            return last.tile;
        }
    }
}
