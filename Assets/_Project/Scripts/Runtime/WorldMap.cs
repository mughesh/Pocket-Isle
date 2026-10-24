using System;
using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>One entry of a world map's props legend: a map character and the prop it stands for.</summary>
    [Serializable]
    public struct PropLegend
    {
        [Tooltip("The character used in the props grid (first character counts).")]
        public string key;
        public PropRecipe recipe;
        [Tooltip("Turned a quarter: the footprint swaps columns and rows (a 2x1 log lying along a column).")]
        public bool turned;

        public char Key => string.IsNullOrEmpty(key) ? '.' : key[0];
    }

    /// <summary>
    /// A realm map: a large tile grid, same codes as <see cref="IslandMap"/> plus X (blocked border), and a second
    /// grid of the same size for props ('.' = none, other characters from <see cref="propLegend"/>).
    /// The island shows a window of it (see <see cref="WorldWindow"/>). Plain text so both can be hand-edited.
    /// Generate a test map with Tools > Diorama > Step 3 > Generate Test World; dress it with
    /// Tools > Pocket Isle > Props > Dress Test World.
    /// </summary>
    [CreateAssetMenu(menuName = "Diorama/World Map", fileName = "WorldMap")]
    public class WorldMap : IslandMap
    {
        [Tooltip("Generator seed. Same seed, same map.")]
        public int seed = 1234;

        [Tooltip("Explorer start tile (column, row).")]
        public Vector2Int startTile;

        [Header("Props")]
        [Tooltip("Same size as the layout, one character per tile. '.' = no prop. Characters come from the legend.")]
        [TextArea(14, 60)]
        public string props = string.Empty;
        public List<PropLegend> propLegend = new List<PropLegend>();

        [Tooltip("Content zoom while this map shows (interiors smaller than the island window: the lodge ~1.6). 1 = outdoor maps.")]
        [Min(0.1f)] public float viewZoom = 1f;

        [Header("Look")]
        [Tooltip("Realm colours (restored / unrestored). Empty = the island materials' own colours.")]
        public RealmTheme theme;

        string[] _propRows;
        Dictionary<char, PropLegend> _legend;
        HashSet<Vector2Int> _blocked;

        public char At(Vector2Int tile) => At(tile.y, tile.x);

        string[] PropRows
        {
            get
            {
                if (_propRows == null)
                    _propRows = (props ?? string.Empty).Replace("\r", string.Empty)
                        .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                return _propRows;
            }
        }

        /// <summary>The props grid character at a tile ('.' = none).</summary>
        public char PropAt(int row, int column)
        {
            var rows = PropRows;
            if (row < 0 || row >= rows.Length || column < 0 || column >= rows[row].Length) return Empty;
            return rows[row][column];
        }

        /// <summary>The legend entry for a props grid character. False for '.' and unknown characters.</summary>
        public bool TryGetProp(char key, out PropLegend entry)
        {
            if (_legend == null)
            {
                _legend = new Dictionary<char, PropLegend>();
                foreach (var e in propLegend ?? new List<PropLegend>())
                    if (e.Key != Empty && e.recipe != null) _legend[e.Key] = e;
            }
            return _legend.TryGetValue(key, out entry);
        }

        /// <summary>Tiles covered by a prop placed with this legend entry at (column, row): the first is the anchor.</summary>
        public static Vector2Int FootprintOf(in PropLegend entry)
        {
            var f = entry.recipe != null ? entry.recipe.Footprint : Vector2Int.one;
            return entry.turned ? new Vector2Int(f.y, f.x) : f;
        }

        /// <summary>True if a blocking prop covers this tile (column, row).</summary>
        public bool PropBlocks(Vector2Int tile)
        {
            if (_blocked == null)
            {
                _blocked = new HashSet<Vector2Int>();
                var rows = PropRows;
                for (int r = 0; r < rows.Length; r++)
                    for (int c = 0; c < rows[r].Length; c++)
                    {
                        if (!TryGetProp(rows[r][c], out var e) || !e.recipe.blocksWalking) continue;
                        var f = FootprintOf(e);
                        for (int dy = 0; dy < f.y; dy++)
                            for (int dx = 0; dx < f.x; dx++)
                                _blocked.Add(new Vector2Int(c + dx, r + dy));
                    }
            }
            return _blocked.Contains(tile);
        }

        public override bool CanWalk(Vector2Int tile) => base.CanWalk(tile) && !PropBlocks(tile);

        /// <summary>Replaces the props grid from code (dressing tools) and drops the caches.</summary>
        public void SetProps(string text)
        {
            props = text;
            ClearPropCaches();
            version++;
        }

        void ClearPropCaches()
        {
            _propRows = null;
            _legend = null;
            _blocked = null;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            ClearPropCaches();
        }
    }
}
