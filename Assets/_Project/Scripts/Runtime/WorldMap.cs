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
        [Tooltip("Extra turn in degrees on top of Turned (fence corners, the cliff's second look). The footprint doesn't turn " +
                 "with it: use multiples of 90 on 1x1 props, 180 on bigger ones.")]
        public float yaw;

        public char Key => string.IsNullOrEmpty(key) ? '.' : key[0];
    }

    /// <summary>A named tile on a map (story beats, hint targets: the first tall grass, the statue, the pond).</summary>
    [Serializable]
    public struct Landmark
    {
        public string name;
        public Vector2Int tile;
        [Tooltip("Optional dialogue trigger said the first time the explorer comes within Radius tiles (LandmarkLines).")]
        public string line;
        [Min(0)] public int radius;
    }

    /// <summary>
    /// One entry of a world map's zones legend: the character used in the zones grid, the zone id (forest, field,
    /// springs...) and its own tall grass table (empty = the map's grass table). Later realms reuse the same system.
    /// </summary>
    [Serializable]
    public struct MapZone
    {
        [Tooltip("The character used in the zones grid (first character counts).")]
        public string key;
        public string id;
        [Tooltip("Tall grass encounters in this zone. Empty = the map's grass table.")]
        public EncounterTable grassTable;

        public char Key => string.IsNullOrEmpty(key) ? '.' : key[0];
    }

    /// <summary>
    /// A Shard Gate on this map: the gate prop's anchor tile (top-left of its footprint), where it leads and where the
    /// explorer arrives there. Entered from the tile in front of it (+row). Sealed gates bump.
    /// </summary>
    [Serializable]
    public struct GateLink
    {
        [Tooltip("fire / water / wind / home. Names the dialogue lines (gate.fire.first) and the portal look.")]
        public string id;
        [Tooltip("Anchor tile of the gate prop (its top-left tile).")]
        public Vector2Int tile;
        [Tooltip("The map it leads to. Empty = sealed.")]
        public WorldMap to;
        [Tooltip("Arrival tile on that map (in front of its return gate).")]
        public Vector2Int arrive;
        public Vector2Int arriveFacing;
        [Tooltip("Asleep: the portal is dark under a stone seal and walking in only bumps.")]
        public bool isSealed;
        [Tooltip("Portal shimmer colour.")]
        public Color portal;
        [Tooltip("Optional dialogue trigger the first time the gate comes into the window (gate.fire.first).")]
        public string firstSeenLine;

        public bool Open => !isSealed && to != null;

        /// <summary>True for the gate's two tiles.</summary>
        public bool Covers(Vector2Int t) => t.y == tile.y && (t.x == tile.x || t.x == tile.x + 1);

        /// <summary>The tile in front of one of its two tiles (0 or 1), where the explorer enters from.</summary>
        public Vector2Int Front(int side) => tile + new Vector2Int(side, 1);
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

        [Header("Encounters (outdoor maps)")]
        [Tooltip("Tall grass outside any zone with its own table.")]
        public EncounterTable grassTable;
        public EncounterTable waterTable;
        [Tooltip("Same size as the layout, one character per tile. '.' = no zone. Characters come from the zone legend.")]
        [TextArea(4, 60)]
        public string zones = string.Empty;
        public List<MapZone> zoneLegend = new List<MapZone>();

        [Header("Story")]
        [Tooltip("Realm id for story lines (stillisle, cinderwood).")]
        public string realm;
        [Tooltip("Optional dialogue trigger when a Shard Gate brings you here (realm.cinderwood.first, hub.return.first).")]
        public string arrivalLine;
        public List<GateLink> gates = new List<GateLink>();
        public List<Landmark> landmarks = new List<Landmark>();

        public bool TryGetLandmark(string landmark, out Vector2Int tile)
        {
            foreach (var l in landmarks)
                if (l.name == landmark)
                {
                    tile = l.tile;
                    return true;
                }
            tile = default;
            return false;
        }

        string[] _propRows, _zoneRows;
        Dictionary<char, PropLegend> _legend;
        HashSet<Vector2Int> _blocked;

        /// <summary>The zone legend entry at a tile (column, row). False outside every zone.</summary>
        public bool TryGetZone(Vector2Int tile, out MapZone zone)
        {
            zone = default;
            if (_zoneRows == null)
                _zoneRows = (zones ?? string.Empty).Replace("\r", string.Empty)
                    .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (tile.y < 0 || tile.y >= _zoneRows.Length || tile.x < 0 || tile.x >= _zoneRows[tile.y].Length) return false;
            char k = _zoneRows[tile.y][tile.x];
            if (k == Empty || zoneLegend == null) return false;
            foreach (var z in zoneLegend)
                if (z.Key == k)
                {
                    zone = z;
                    return true;
                }
            return false;
        }

        /// <summary>The zone id at a tile, or null.</summary>
        public string ZoneAt(Vector2Int tile) => TryGetZone(tile, out var z) ? z.id : null;

        /// <summary>The tall grass table for a tile: its zone's own, else the map's.</summary>
        public EncounterTable GrassTableAt(Vector2Int tile) =>
            TryGetZone(tile, out var z) && z.grassTable != null ? z.grassTable : grassTable;

        /// <summary>Replaces the zones grid from code (generators).</summary>
        public void SetZones(string text)
        {
            zones = text;
            _zoneRows = null;
        }

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
            _zoneRows = null;
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
