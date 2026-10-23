using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// A realm map: a large tile grid, same codes as <see cref="IslandMap"/> plus X (blocked border).
    /// The island shows a window of it (see <see cref="WorldWindow"/>). Plain text so it can be hand-edited.
    /// Generate a test map with Tools > Diorama > Step 3 > Generate Test World.
    /// </summary>
    [CreateAssetMenu(menuName = "Diorama/World Map", fileName = "WorldMap")]
    public class WorldMap : IslandMap
    {
        [Tooltip("Generator seed. Same seed, same map.")]
        public int seed = 1234;

        [Tooltip("Explorer start tile (column, row).")]
        public Vector2Int startTile;

        public char At(Vector2Int tile) => At(tile.y, tile.x);
    }
}
