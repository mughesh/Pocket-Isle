using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 14k (Mugesh: at Cinderwood's shrine the guidance didn't come from the row he stood on): the one "stand next to
    /// it" rule for every walk-up trigger (shrines, gates, temple and tower doors, hand moments): any tile within one tile
    /// of the thing's footprint, diagonals included. Pure (tested); footprints are map tiles (column, row; rows grow toward
    /// the player, a prop's key sits on its top-left tile).
    /// </summary>
    public static class WalkUp
    {
        /// <summary>Tiles (Chebyshev) from a tile to a footprint: 0 = on it, 1 = next to it (diagonals too).</summary>
        public static int Distance(Vector2Int tile, RectInt footprint)
        {
            int dx = tile.x < footprint.xMin ? footprint.xMin - tile.x : tile.x > footprint.xMax - 1 ? tile.x - (footprint.xMax - 1) : 0;
            int dy = tile.y < footprint.yMin ? footprint.yMin - tile.y : tile.y > footprint.yMax - 1 ? tile.y - (footprint.yMax - 1) : 0;
            return Mathf.Max(dx, dy);
        }

        /// <summary>Next to the footprint (not on it), diagonals included.</summary>
        public static bool Next(Vector2Int tile, RectInt footprint) => Distance(tile, footprint) == 1;

        /// <summary>
        /// Next to the footprint on its front side (the row below it, one more tile each side: the front diagonals). For
        /// gates and doors you walk into from the front (a gate's back is often the map's edge or a wall).
        /// </summary>
        public static bool Front(Vector2Int tile, RectInt footprint) =>
            tile.y == footprint.yMax && tile.x >= footprint.xMin - 1 && tile.x <= footprint.xMax;

        /// <summary>Every tile next to the footprint (the ring, diagonals included).</summary>
        public static IEnumerable<Vector2Int> Ring(RectInt footprint)
        {
            for (int y = footprint.yMin - 1; y <= footprint.yMax; y++)
                for (int x = footprint.xMin - 1; x <= footprint.xMax; x++)
                {
                    var t = new Vector2Int(x, y);
                    if (Next(t, footprint)) yield return t;
                }
        }
    }
}
