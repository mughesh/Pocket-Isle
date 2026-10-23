using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Island layout as a text grid, one character per tile:
    /// G grass, T tall grass, P path, W water, F tree, R rock, X blocked border, '.' empty.
    /// The island's own map is its footprint (the window shape); <see cref="WorldMap"/> is the bigger map it shows.
    /// </summary>
    /// <remarks>
    /// The prototype is three.js (right-handed); Unity is left-handed. We mirror X when porting,
    /// so columns run toward -X and creature recipes keep +Z as their front.
    /// </remarks>
    [CreateAssetMenu(menuName = "Diorama/Island Map", fileName = "IslandMap")]
    public class IslandMap : ScriptableObject
    {
        public const char Empty = '.';
        public const char Grass = 'G';
        public const char TallGrass = 'T';
        public const char Path = 'P';
        public const char Water = 'W';
        public const char Tree = 'F';
        public const char Rock = 'R';
        public const char Blocked = 'X';

        public const string PrototypeLayout =
            "..GGGGGGGGGG..\n" +
            ".GGGFGGGGTTTG.\n" +
            "GGFGGGGGGTTTTG\n" +
            "GGGGPPPPGTTTTG\n" +
            "GGGGPGGPGTTTGG\n" +
            "GRGGPGGPPPPGGG\n" +
            "GGGGPGGGGGPGFG\n" +
            "GWWGPPPPPPPGGG\n" +
            "GWWWGGGGGGPGRG\n" +
            "GGWWGTTTGGPGGG\n" +
            "GGGGGTTTTGPGGG\n" +
            ".GFGGTTTTGPPG.\n" +
            ".GGGGGGGGGGGG.\n" +
            "..GGGGGGGGGG..";

        [Tooltip("One row per line. G grass, T tall grass, P path, W water, F tree, R rock, '.' empty.")]
        [TextArea(14, 30)]
        public string layout = PrototypeLayout;

        [Tooltip("Edge length of one tile, in metres.")]
        public float tileSize = 0.05f;

        [Tooltip("Thickness of the coloured cap on top of each tile, in metres.")]
        public float capHeight = 0.006f;

        string[] _rows;

        string[] RowsCache
        {
            get
            {
                if (_rows == null)
                    _rows = (layout ?? string.Empty).Replace("\r", string.Empty)
                        .Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
                return _rows;
            }
        }

        public int Rows => RowsCache.Length;

        public int Columns
        {
            get
            {
                int max = 0;
                foreach (var row in RowsCache) max = Mathf.Max(max, row.Length);
                return max;
            }
        }

        public char At(int row, int column)
        {
            var rows = RowsCache;
            if (row < 0 || row >= rows.Length || column < 0 || column >= rows[row].Length) return Empty;
            return rows[row][column];
        }

        /// <summary>True for tiles the explorer can stand on.</summary>
        public static bool IsWalkable(char tile) =>
            tile == Grass || tile == TallGrass || tile == Path;

        /// <summary>Replaces the layout from code (generators) and drops the row cache.</summary>
        public void SetLayout(string text)
        {
            layout = text;
            _rows = null;
        }

        /// <summary>Tile centre in island-local space, at cap-bottom height (y = 0).</summary>
        public Vector3 TileCenter(int row, int column)
        {
            float cr = (Rows - 1) * 0.5f, cc = (Columns - 1) * 0.5f;
            return new Vector3(-(column - cc) * tileSize, 0f, (row - cr) * tileSize);
        }

        protected virtual void OnValidate() => _rows = null;
    }
}
