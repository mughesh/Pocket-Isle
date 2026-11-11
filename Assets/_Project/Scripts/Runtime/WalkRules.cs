using System;
using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Nov 11 (Mugesh: the explorer walked off the island in Lilymere while the view was held on the lotus garden): the
    /// walk only ever steps onto tiles the island shows right now. Pure rules over the window's centre and footprint;
    /// <see cref="WorldWindow.Shows"/> adds the footprint's round corners.
    /// </summary>
    public static class WalkRules
    {
        /// <summary>
        /// True when the map tile lies inside the island window: the footprint is <paramref name="cols"/> x
        /// <paramref name="rows"/> tiles at zoom 1 round <paramref name="center"/> (column, row; a tile corner for even sizes).
        /// </summary>
        public static bool InWindow(Vector2 center, int cols, int rows, float zoom, Vector2Int tile)
        {
            float z = Mathf.Max(0.01f, zoom);
            float halfX = (cols - 1) * 0.5f / z + 1e-3f, halfY = (rows - 1) * 0.5f / z + 1e-3f;
            return Mathf.Abs(tile.x - center.x) <= halfX && Mathf.Abs(tile.y - center.y) <= halfY;
        }

        /// <summary>Cuts a plan at its first tile that isn't shown; returns how many tiles were cut.</summary>
        public static int Truncate(List<Vector2Int> tiles, Func<Vector2Int, bool> shown)
        {
            if (tiles == null || shown == null) return 0;
            for (int i = 0; i < tiles.Count; i++)
                if (!shown(tiles[i]))
                {
                    int cut = tiles.Count - i;
                    tiles.RemoveRange(i, cut);
                    return cut;
                }
            return 0;
        }
    }
}
