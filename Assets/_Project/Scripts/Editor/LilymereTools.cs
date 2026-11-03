using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 11a: the Lilymere map, 60 x 60 (lily lagoon, RealmsDesign.md). One trail through five beats: the arrival by the
    /// return gate, the stilt village (huts round shallows, a plank bridge over them, the Hearth Shrine), the lily channel
    /// (3 rows of water across the map, crossed at one 2-wide spot: 11b's lily pad bridge), the lotus garden (a dry cracked
    /// pond bed the trail crosses: 11b's rain), the waterfall basin under the cliff (11b's scoop and pour) and the lake
    /// temple on the lagoon's north shore (11c). Side pockets: the reed marsh, a rare pond, a wind-visitor meadow, a
    /// lookout jetty. The lagoon is half dry while unrestored: its outer ring is dry bed (D), water once the map rises
    /// (<see cref="WorldMap.SetRisen"/>). Drawn from code with a fixed seed like <see cref="GalewickTools"/>; same
    /// coordinates (x west to east, y south to north; south = the arrival), <see cref="Tile"/> turns them into (column, row).
    /// </summary>
    public static class LilymereTools
    {
        public const string MapPath = "Assets/_Project/Data/WorldMap_Lilymere.asset";
        public const string ConfigPath = "Assets/_Project/Data/Lilymere/LilymereConfig.asset";
        const string TableRoot = "Assets/_Project/Data/Battle/Lilymere";
        public const int Size = 60;
        const int Seed = 1131;

        // ---- story tiles (x, y) ----
        public static readonly Vector2Int GateXY = new Vector2Int(8, 6);      // return gate, 2 x 1 (front y 5)
        public static readonly Vector2Int ArriveXY = new Vector2Int(11, 5);   // arrive here facing north
        public static readonly Vector2Int ShrineXY = new Vector2Int(14, 19);  // 2 x 2, front y 17
        static readonly Vector2Int PlazaXY = new Vector2Int(17, 14);
        // Step 14a: north of the arrival stream (it was on the stream's rows).
        static readonly Vector2Int FirstGrassXY = new Vector2Int(15, 11);
        /// <summary>
        /// Step 14a: the arrival stream: a thin trickle (dry bed tiles, rows y 8..9) across the arrival meadow from the
        /// west woods to the east woods, crossing the trail 3 tiles past the arrival. Rain swells it (RainGarden, stream
        /// mode, lm.stream); the three lily pads stuck in the mud on its south bank float and drift into a bridge over the
        /// trail's crossing tiles (x 11..12).
        /// </summary>
        public const int StreamY0 = 8, StreamY1 = 9, StreamX0 = 5, StreamX1 = 18;
        /// <summary>Nov 3: the pads stuck in the mud, scattered along the bed either side of the crossing (x, y; fractional).</summary>
        public static readonly Vector2[] StreamPadsXY = { new Vector2(7.6f, 8.3f), new Vector2(9.4f, 8.75f), new Vector2(14.1f, 8.25f), new Vector2(15.9f, 8.7f) };
        /// <summary>Their bridge slots: one on each of the crossing's four tiles (x, y).</summary>
        public static readonly Vector2[] StreamBridgeXY = { new Vector2(11f, 8f), new Vector2(11f, 9f), new Vector2(12f, 8f), new Vector2(12f, 9f) };
        public static Vector2 MapPoint(Vector2 xy) => new Vector2(xy.x, Size - 1 - xy.y);
        /// <summary>The village shallows (an ellipse of water the trail crosses on a plank bridge).</summary>
        static readonly Vector2 ShallowsCenter = new Vector2(24f, 15f);
        const float ShallowsRx = 4.6f, ShallowsRy = 3.8f;
        /// <summary>The lily channel rows (water) and its 2-wide crossing (11b A: the lily pad bridge).</summary>
        public const int ChannelY0 = 24, ChannelY1 = 26;
        public static readonly Vector2Int CrossingXY = new Vector2Int(33, 26); // west-north tile; covers x 33..34, y 24..26
        /// <summary>The lotus garden: a dry pond bed (D) the trail crosses north to south (11b B: rain, lotus stepping stones).</summary>
        public static readonly Vector2 GardenCenter = new Vector2(40.5f, 36f);
        public const float GardenRx = 5.2f, GardenRy = 3.4f;
        /// <summary>The trail's 2-wide crossing over the garden bed, x 40..41.</summary>
        public const int GardenPathX = 40;
        /// <summary>The lagoon: inner water, outer dry ring (water once risen). Its north shore holds the temple.</summary>
        public static readonly Vector2 LagoonCenter = new Vector2(17f, 41f);
        public const float LagoonRx = 9f, LagoonRy = 7f, LagoonWetShare = 0.62f;
        /// <summary>The waterfall basin (2 x 2) under the cliff, the frozen waterfall (2 x 1) on the cliff face above it.</summary>
        public static readonly Vector2Int BasinXY = new Vector2Int(30, 50);
        public static readonly Vector2Int WaterfallXY = new Vector2Int(30, 52);
        /// <summary>The lake temple (3 x 2), half-sunk on the lagoon's north shore; door front y 47 (end of the causeway).</summary>
        public static readonly Vector2Int TempleXY = new Vector2Int(16, 50);
        /// <summary>The lookout jetty: planks from the causeway south onto the lagoon (x 22, y 44..46).</summary>
        static readonly Vector2Int JettyXY = new Vector2Int(22, 44);

        public static Vector2Int Tile(Vector2Int xy) => new Vector2Int(xy.x, Size - 1 - xy.y);
        public static Vector2Int Tile(int x, int y) => new Vector2Int(x, Size - 1 - y);
        public static Vector2Int ReturnGate => Tile(GateXY);
        public static Vector2Int Arrive => Tile(ArriveXY);
        public static readonly Vector2Int ArriveFacing = new Vector2Int(0, -1);
        public static Vector2Int TempleFront => Tile(TempleXY.x + 1, TempleXY.y - 2);
        /// <summary>Step 11c: the temple's return gate (2 x 1, west of the temple, its front on the shore path; shown once Tidemother is calm).</summary>
        public static readonly Vector2Int TempleGateXY = new Vector2Int(13, 49);
        public static Vector2Int BasinFront => Tile(BasinXY.x, BasinXY.y - 2);

        // The main trail (2 wide around the centre line), straight legs only (Mugesh, Oct 27: zig-zags are hard to walk).
        static readonly Vector2[] Trail =
        {
            new Vector2(11.5f, 3.5f), new Vector2(11.5f, 13.5f), new Vector2(33.5f, 13.5f), new Vector2(33.5f, 30.5f),
            new Vector2(40.5f, 30.5f), new Vector2(40.5f, 43.5f), new Vector2(30.5f, 43.5f), new Vector2(30.5f, 47.5f),
        };
        /// <summary>The causeway from the basin plaza west to the temple door, along the lagoon's north shore.</summary>
        static readonly Vector2[] Causeway = { new Vector2(30.5f, 47.5f), new Vector2(17.5f, 47.5f) };
        // Side paths (1 wide), straight legs between the corners.
        static readonly Vector2Int[][] SidePaths =
        {
            new[] { new Vector2Int(11, 16), new Vector2Int(11, 18), new Vector2Int(6, 18), new Vector2Int(6, 22) },  // reed marsh (south of the channel)
            new[] { new Vector2Int(14, 14), new Vector2Int(14, 16) },                                                  // shrine
            new[] { new Vector2Int(34, 16), new Vector2Int(46, 16), new Vector2Int(46, 12) },                         // wind meadow
            new[] { new Vector2Int(42, 42), new Vector2Int(50, 42), new Vector2Int(50, 47) },                         // rare pond
        };

        struct Zone
        {
            public char key;
            public string id;
            public RectInt rect;
            public Zone(char key, string id, int x0, int x1, int y0, int y1)
            {
                this.key = key;
                this.id = id;
                rect = new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
            }
        }

        // Later entries win where rectangles overlap.
        static readonly Zone[] Zones =
        {
            new Zone('v', "village", 4, 32, 8, 22),
            new Zone('a', "arrival", 4, 18, 2, 10),
            new Zone('w', "wind", 37, 56, 6, 22),
            new Zone('c', "channel", 14, 48, 23, 32),
            new Zone('m', "marsh", 2, 10, 16, 23),
            new Zone('g', "garden", 34, 48, 32, 44),
            new Zone('p', "pond", 44, 57, 40, 55),
            new Zone('b', "basin", 18, 40, 45, 57),
        };

        // ---- prop keys ----
        const char KGate = 'Z', KShrine = 'Y', KHut = 'h', KPlank = '=', KReeds = 'r', KLily = 'l', KLotus = 'o',
            KWaterfall = 'Q', KBasin = 'B', KTemple = 'T', KStone = 's', KSign = 'i', KFence = '-', KChannelGap = '~', KBedGap = ':';

        static readonly (char key, string recipe, bool turned)[] Keys =
        {
            (KGate, "ReturnGate", false), (KShrine, HearthShrines.RecipeName, false), (KHut, "StiltHut", false),
            (KPlank, "PlankBridge", false), (KReeds, "ReedClump", false), (KLily, "LilyPad", false), (KLotus, "Lotus", false),
            (KWaterfall, "CliffBowl", false), (KBasin, "StoneBasin", false), (KTemple, "LakeTemple", false),
            (KStone, "SteppingStone", false), (KSign, "Signpost", false), (KFence, "Fence", false),
            (KChannelGap, "ChannelGap", false), (KBedGap, "BedGap", false),
        };

        // Keys that may stand on a path tile (the trail goes under or over them) and on water.
        static readonly HashSet<char> OnPath = new HashSet<char> { KGate, KShrine, KPlank, KStone, KChannelGap, KBedGap };
        static readonly HashSet<char> OnWater = new HashSet<char> { KLily, KLotus };

        // ---- generation state ----
        static char[,] _g, _p, _z; // [x, y]
        static HashSet<Vector2Int> _covered;
        static Dictionary<string, PropRecipe> _recipes;
        static SeededRandom _rnd;
        static StringBuilder _log;

        // ================= write =================

        /// <summary>Writes (or rewrites) the Lilymere map asset, its zone tables and legend. Returns it.</summary>
        public static WorldMap Write(StringBuilder log)
        {
            _log = log;
            var map = AssetDatabase.LoadAssetAtPath<WorldMap>(MapPath);
            if (map == null)
            {
                map = ScriptableObject.CreateInstance<WorldMap>();
                AssetDatabase.CreateAsset(map, MapPath);
                log.Append($"\n  created {MapPath}");
            }
            var island = AssetDatabase.LoadAssetAtPath<IslandMap>("Assets/_Project/Data/IslandMap_Prototype.asset");
            _recipes = new Dictionary<string, PropRecipe>();
            foreach (var k in Keys)
            {
                if (_recipes.ContainsKey(k.recipe)) continue;
                var r = PropKit.Load(k.recipe);
                if (r == null) Debug.LogError($"[Lilymere] Prop {k.recipe} missing (write the prop kits first).");
                _recipes[k.recipe] = r;
            }

            Generate();

            map.seed = Seed;
            map.tileSize = 0.05f;
            if (island != null) map.capHeight = island.capHeight;
            map.viewZoom = 1f;
            map.startTile = Arrive;
            map.theme = PropKit.Theme("Lilymere");
            map.realm = "lilymere";
            map.arrivalLine = "realm.lilymere.first";
            map.propLegend = Legend();
            map.SetLayout(Text(_g));
            map.SetProps(Text(_p));
            map.SetZones(Text(_z));
            // Placeholder tables until step 11c's water roster: the water Islings there are, wind visitors in the meadow.
            // Levels a step above Galewick's (the third realm); shards held add more at runtime (ShardScaling).
            map.grassTable = Table("Encounters_Lilymere", 6, 8, ("Marimoss", 35f, false), ("Lilyvole", 25f, false), ("Drizzcan", 15f, false), ("Kitelet", 15f, false), ("Pebbloop", 10f, false));
            // Step 11c: the water roster (~70% water, ~20% wind visitors, the rare pond's Bottlewhale ~5%).
            map.waterTable = Table("Encounters_LmWater", 6, 8, ("Sailfold", 45f, false), ("Pebbloop", 25f, false), ("Marimoss", 15f, false), ("Steambara", 15f, false));
            map.waterTable.habitat = Habitat.Water;
            map.zoneLegend = new List<MapZone>();
            foreach (var z in Zones)
            {
                var table = z.id switch
                {
                    "arrival" => Table("Encounters_LmArrival", 4, 6, ("Marimoss", 40f, false), ("Lilyvole", 30f, false), ("Kitelet", 20f, false), ("Pebbloop", 10f, false)),
                    "village" => Table("Encounters_LmVillage", 4, 6, ("Lilyvole", 35f, false), ("Drizzcan", 30f, false), ("Samarling", 20f, false), ("Marimoss", 10f, false), ("Steambara", 5f, false)),
                    "channel" => Table("Encounters_LmChannel", 5, 7, ("Sailfold", 35f, false), ("Drizzcan", 25f, false), ("Vanehare", 20f, false), ("Lilyvole", 10f, false), ("Pebbloop", 10f, false)),
                    "marsh" => Table("Encounters_LmMarsh", 5, 7, ("Marimoss", 40f, false), ("Drizzcan", 30f, false), ("Steambara", 20f, false), ("Kitelet", 10f, false)),
                    "garden" => Table("Encounters_LmGarden", 6, 8, ("Lilyvole", 35f, false), ("Marimoss", 25f, false), ("Vanehare", 20f, false), ("Sailfold", 10f, false), ("Pebbloop", 10f, false)),
                    "pond" => Table("Encounters_LmPond", 7, 9, ("Sailfold", 35f, false), ("Drizzcan", 25f, false), ("Samarling", 20f, false), ("Steambara", 15f, false), ("Bottlewhale", 5f, true)),
                    "wind" => Table("Encounters_LmWind", 5, 7, ("Kitelet", 35f, false), ("Samarling", 25f, false), ("Vanehare", 20f, false), ("Marimoss", 20f, false)),
                    _ => null,
                };
                map.zoneLegend.Add(new MapZone { key = z.key.ToString(), id = z.id, grassTable = table });
            }
            map.landmarks = new List<Landmark>
            {
                new Landmark { name = "start", tile = Arrive },
                new Landmark { name = "firstGrass", tile = Tile(FirstGrassXY) },
                new Landmark { name = "stream", tile = Tile(11, StreamY0 - 2) },
                new Landmark { name = "village", tile = Tile(PlazaXY), line = "lm.village.first", radius = 4 },
                new Landmark { name = "shrine", tile = Tile(ShrineXY) },
                new Landmark { name = "channel", tile = Tile(CrossingXY.x, ChannelY0 - 2), line = "lm.channel.first", radius = 3 },
                new Landmark { name = "garden", tile = Tile(GardenPathX, Mathf.RoundToInt(GardenCenter.y - GardenRy) - 2), line = "lm.garden.first", radius = 4 },
                new Landmark { name = "basin", tile = BasinFront, line = "lm.basin.first", radius = 3 },
                new Landmark { name = "temple", tile = TempleFront, line = "lm.temple.first", radius = 4 },
                new Landmark { name = "marsh", tile = Tile(6, 20), line = "lm.marsh.first", radius = 3 },
                new Landmark { name = "pond", tile = Tile(50, 46), line = "lm.pond.first", radius = 4 },
                new Landmark { name = "wind", tile = Tile(46, 13) },
                new Landmark { name = "jetty", tile = Tile(JettyXY.x, JettyXY.y + 2) },
            };
            EditorUtility.SetDirty(map.waterTable);
            EditorUtility.SetDirty(map);
            AssetDatabase.SaveAssets();
            log.Append($"\n  {MapPath}: {Size}x{Size} Lilymere (seed {Seed}), arrive c{Arrive.x} r{Arrive.y} facing north, " +
                       $"return gate c{ReturnGate.x} r{ReturnGate.y}, {map.zoneLegend.Count} zones, {map.landmarks.Count} landmarks");
            return map;
        }

        /// <summary>The Lilymere config asset (well gate open in the Editor, sealed in builds), created once.</summary>
        public static LilymereConfig Config()
        {
            var c = AssetDatabase.LoadAssetAtPath<LilymereConfig>(ConfigPath);
            if (c != null) return c;
            DioramaSceneSetup.EnsureFolder("Assets/_Project/Data/Lilymere");
            c = ScriptableObject.CreateInstance<LilymereConfig>();
            c.open = false;
            c.openInEditor = true;
            AssetDatabase.CreateAsset(c, ConfigPath);
            AssetDatabase.SaveAssets();
            return c;
        }

        static List<PropLegend> Legend()
        {
            var legend = new List<PropLegend>();
            foreach (var (k, recipe, turned) in Keys)
                if (_recipes.TryGetValue(recipe, out var r) && r != null)
                    legend.Add(new PropLegend { key = k.ToString(), recipe = r, turned = turned });
            return legend;
        }

        static EncounterTable Table(string name, int minLevel, int maxLevel, params (string species, float weight, bool rare)[] entries)
        {
            minLevel += CinderwoodTools.WildLevelBonus;
            maxLevel += CinderwoodTools.WildLevelBonus;
            DioramaSceneSetup.EnsureFolder(TableRoot);
            var table = StarterKit.Asset<EncounterTable>($"{TableRoot}/{name}.asset");
            table.habitat = Habitat.Grass;
            var list = new List<EncounterTable.Entry>();
            foreach (var (id, weight, rare) in entries)
            {
                var species = AssetDatabase.LoadAssetAtPath<IslingSpecies>($"Assets/_Project/Data/Creatures/Species/{id}.asset");
                if (species == null)
                {
                    Debug.LogError($"[Lilymere] Species {id} not found for {name}.");
                    continue;
                }
                list.Add(new EncounterTable.Entry { species = species, minLevel = minLevel, maxLevel = maxLevel, weight = weight, rare = rare });
            }
            table.entries = list.ToArray();
            EditorUtility.SetDirty(table);
            return table;
        }

        static string Text(char[,] grid)
        {
            var sb = new StringBuilder(Size * (Size + 1));
            for (int row = 0; row < Size; row++)
            {
                int y = Size - 1 - row;
                for (int x = 0; x < Size; x++) sb.Append(grid[x, y]);
                if (row < Size - 1) sb.Append('\n');
            }
            return sb.ToString();
        }

        // ================= generate =================

        static bool In(int x, int y) => x > 0 && y > 0 && x < Size - 1 && y < Size - 1;

        static void Generate()
        {
            _rnd = new SeededRandom(Seed);
            _g = new char[Size, Size];
            _p = new char[Size, Size];
            _z = new char[Size, Size];
            _covered = new HashSet<Vector2Int>();
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                {
                    _g[x, y] = In(x, y) ? IslandMap.Grass : IslandMap.Blocked;
                    _p[x, y] = IslandMap.Empty;
                    _z[x, y] = IslandMap.Empty;
                }
            foreach (var z in Zones)
                for (int x = z.rect.xMin; x < z.rect.xMax; x++)
                    for (int y = z.rect.yMin; y < z.rect.yMax; y++)
                        if (In(x, y)) _z[x, y] = z.key;

            // Woods everywhere outside the open places, a few lone trees inside.
            for (int x = 1; x < Size - 1; x++)
                for (int y = 1; y < Size - 1; y++)
                {
                    float open = Openness(x, y);
                    int edge = Mathf.Min(Mathf.Min(x, y), Mathf.Min(Size - 1 - x, Size - 1 - y));
                    float tree = open > 0f ? open : edge <= 2 ? 0.95f : 0.8f;
                    float roll = _rnd.Next();
                    if (roll < tree) _g[x, y] = IslandMap.Tree;
                    else if (open == 0f) _g[x, y] = IslandMap.Rock;
                }

            Lagoon();
            Cliff();
            Channel();
            Shallows();
            Garden();
            Walls();
            Inlet();

            // The main trail (2 wide) with a grass shoulder, the causeway, the side paths (1 wide).
            foreach (var line in new[] { Trail, Causeway })
                for (int x = 1; x < Size - 1; x++)
                    for (int y = 1; y < Size - 1; y++)
                    {
                        float d = DistToPolyline(new Vector2(x, y), line);
                        if (d < 0.95f) _g[x, y] = IslandMap.Path;
                        else if (d < 2.0f && (_g[x, y] == IslandMap.Tree || _g[x, y] == IslandMap.Rock)) _g[x, y] = IslandMap.Grass;
                    }
            foreach (var path in SidePaths)
                for (int i = 0; i + 1 < path.Length; i++)
                    foreach (var t in Walk4(path[i], path[i + 1]))
                        _g[t.x, t.y] = IslandMap.Path;

            Arrival();
            Stream();
            Village();
            Gates();
            ChannelBanks();
            GardenDressing();
            Basin();
            Temple();
            Marsh();
            RarePond();
            WindMeadow();
            Filler();
            CleanUnderProps();
            SealCutOff();
            ThinBackWoods();
        }

        /// <summary>Tree chance inside an open place (0 = not open: woods rules apply).</summary>
        static float Openness(int x, int y)
        {
            bool InEll(float cx, float cy, float rx, float ry) => Sq((x - cx) / rx) + Sq((y - cy) / ry) <= 1f + (_rnd.Next() - 0.5f) * 0.25f;
            bool Rect(int x0, int x1, int y0, int y1) => x >= x0 && x <= x1 && y >= y0 && y <= y1;
            if (Rect(5, 18, 2, 11)) return 0.004f;                                           // arrival meadow
            if (Rect(9, 36, 9, 22)) return 0.012f;                                           // stilt village
            if (Rect(14, 48, 22, 23) || Rect(28, 48, 27, 32)) return 0.01f;                  // channel banks
            if (InEll(GardenCenter.x, GardenCenter.y, GardenRx + 3.5f, GardenRy + 3.5f)) return 0.006f; // lotus garden
            if (InEll(LagoonCenter.x, LagoonCenter.y, LagoonRx + 2.5f, LagoonRy + 2f)) return 0.004f;   // lagoon shore
            if (Rect(18, 40, 42, 49)) return 0.004f;                                         // basin plaza, causeway
            if (Rect(3, 9, 16, 22)) return 0.02f;                                            // reed marsh
            if (InEll(50f, 47f, 5.5f, 5f)) return 0.006f;                                    // rare pond
            if (InEll(46f, 13f, 7f, 6f)) return 0.01f;                                       // wind meadow
            return 0f;
        }

        static float Sq(float v) => v * v;

        static float Ell(Vector2 c, float rx, float ry, int x, int y) => Mathf.Sqrt(Sq((x - c.x) / rx) + Sq((y - c.y) / ry));

        // ---- water beats ----

        /// <summary>The lagoon: water in the middle, a ring of dry bed round it (water once the map rises), a sandy shore.</summary>
        static void Lagoon()
        {
            for (int x = 1; x < Size - 1; x++)
                for (int y = 1; y < Size - 1; y++)
                {
                    float e = Ell(LagoonCenter, LagoonRx, LagoonRy, x, y);
                    if (e <= LagoonWetShare) _g[x, y] = IslandMap.Water;
                    else if (e <= 1f) _g[x, y] = IslandMap.DryBed;
                    else if (e <= 1.22f && (_g[x, y] == IslandMap.Tree || _g[x, y] == IslandMap.Rock)) _g[x, y] = IslandMap.Grass;
                }
        }

        /// <summary>The cliff along the north (y 51..57, x 20..44), the waterfall's face; the basin sits at its foot.</summary>
        static void Cliff()
        {
            for (int x = 20; x <= 44; x++)
                for (int y = 51; y < Size - 1; y++)
                    _g[x, y] = _rnd.Next() < 0.55f ? IslandMap.Rock : IslandMap.Tree;
        }

        /// <summary>The lily channel across the map (water, woods at its ends) with the 2-wide crossing (path in 11a).</summary>
        static void Channel()
        {
            for (int x = 1; x < Size - 1; x++)
                for (int y = ChannelY0; y <= ChannelY1; y++)
                {
                    bool crossing = x >= CrossingXY.x && x <= CrossingXY.x + 1;
                    if (crossing) _g[x, y] = IslandMap.Path;
                    else if (x >= 13 && x <= 49) _g[x, y] = IslandMap.Water;
                    else _g[x, y] = IslandMap.Tree;
                }
        }

        /// <summary>The village shallows: an ellipse of water (the trail crosses it on planks).</summary>
        static void Shallows()
        {
            for (int x = 1; x < Size - 1; x++)
                for (int y = 1; y < Size - 1; y++)
                    if (Ell(ShallowsCenter, ShallowsRx, ShallowsRy, x, y) <= 1f) _g[x, y] = IslandMap.Water;
        }

        /// <summary>The lotus garden: a dry cracked pond bed (D) the 2-wide trail crosses (path in 11a; 11b's lotus stones).</summary>
        static void Garden()
        {
            for (int x = 1; x < Size - 1; x++)
                for (int y = 1; y < Size - 1; y++)
                {
                    float e = Ell(GardenCenter, GardenRx, GardenRy, x, y);
                    if (e <= 1f) _g[x, y] = IslandMap.DryBed;
                    else if (e <= 1.6f && (_g[x, y] == IslandMap.Tree || _g[x, y] == IslandMap.Rock)) _g[x, y] = IslandMap.Grass;
                }
        }

        /// <summary>
        /// Woods that make the puzzles the only ways on: north of the channel the bank west of the trail is forest up to
        /// the lagoon's shore (else the lagoon shore would lead round to the basin), and a band through the garden's middle
        /// is forest on both sides of the dry bed (so the trail has to cross it).
        /// </summary>
        static void Walls()
        {
            for (int x = 1; x < 28; x++)
                for (int y = ChannelY1 + 1; y <= 33; y++)
                    if (_g[x, y] != IslandMap.Water && _g[x, y] != IslandMap.DryBed) _g[x, y] = IslandMap.Tree;
            for (int x = 28; x < Size - 1; x++)
                for (int y = Mathf.RoundToInt(GardenCenter.y) - 1; y <= Mathf.RoundToInt(GardenCenter.y) + 1; y++)
                    if (Ell(GardenCenter, GardenRx, GardenRy, x, y) > 1f) _g[x, y] = IslandMap.Tree;
        }

        /// <summary>
        /// Step 11b: a little inlet of the lagoon under the causeway, near the basin, where a cupped hand scoops water
        /// (x 25..27 water; x 22..24 dry bed joining it to the lagoon's ring, water once the lagoon rises).
        /// </summary>
        static void Inlet()
        {
            for (int x = InletX0 - 3; x <= InletX0 + 2; x++)
                for (int y = 45; y <= 46; y++)
                    _g[x, y] = x >= InletX0 ? IslandMap.Water : _g[x, y] == IslandMap.Water ? IslandMap.Water : IslandMap.DryBed;
        }

        /// <summary>The inlet's west water column (x); it runs 3 tiles east, rows y 45..46.</summary>
        public const int InletX0 = 25;

        // ---- places ----

        static void Arrival()
        {
            Fill(7, 13, 3, 5, IslandMap.Path);
            Prop(GateXY, KGate);
            // Step 14a: the sign by the arrival, off the stream's rows.
            Prop(10, 6, KSign);
            Patch(FirstGrassXY.x, FirstGrassXY.y, 1.8f, 1.4f);
            Patch(6.5f, 11.5f, 1.3f, 1.0f);
            Patch(16.5f, 4.5f, 1.2f, 1.1f);
            foreach (var (x, y) in new[] { (5, 4), (16, 9) }) Prop(x, y, KReeds);
        }

        /// <summary>
        /// Step 14a: the arrival stream's bed. Nov 3: all of it is sunken mud (the trail's tiles too: no path across, the
        /// pads are the only way), a bare south bank (kept so the layout around it doesn't re-roll).
        /// </summary>
        static void Stream()
        {
            for (int x = StreamX0; x <= StreamX1; x++)
            {
                for (int y = StreamY0; y <= StreamY1; y++)
                    _g[x, y] = IslandMap.Mud;
                // The muddy south bank where the pads are stuck: plain ground, no tall grass.
                if (x >= 13 && x <= 17 && _g[x, StreamY0 - 1] == IslandMap.TallGrass) _g[x, StreamY0 - 1] = IslandMap.Grass;
            }
            foreach (var (x, y) in new[] { (6, 10), (17, 10) }) Prop(x, y, KReeds);
        }

        /// <summary>A bed that fills with water: the lagoon's dry sand or the stream's mud.</summary>
        static bool Bed(char ch) => ch == IslandMap.DryBed || ch == IslandMap.Mud;

        /// <summary>Step 14a: the trail's tiles over the arrival stream.</summary>
        public static bool IsStreamGate(int x, int y) => y >= StreamY0 && y <= StreamY1 && x >= StreamX0 && x <= StreamX1 && x >= 11 && x <= 12;

        static void Village()
        {
            Disc(PlazaXY.x + 0.5f, PlazaXY.y + 0.5f, 2.1f, IslandMap.Path);
            // The plank bridge: the trail's tiles over the shallows carry planks.
            for (int x = 1; x < Size - 1; x++)
                for (int y = 13; y <= 14; y++)
                    if (Ell(ShallowsCenter, ShallowsRx, ShallowsRy, x, y) <= 1.05f) Prop(x, y, KPlank);
            // Stilt huts round the shallows (on their banks), lily pads on the water.
            foreach (var (x, y) in new[] { (20, 20), (26, 21), (28, 11), (20, 10), (29, 18) }) Prop(x, y, KHut);
            foreach (var (x, y) in new[] { (21, 16), (23, 17), (26, 16), (27, 16), (23, 12), (26, 12) }) Prop(x, y, KLily);
            Prop(ShrineXY, KShrine);
            Prop(15, 12, KSign);
            foreach (var (x, y) in new[] { (19, 18), (29, 15), (19, 12) }) Prop(x, y, KReeds);
            for (int x = 9; x <= 12; x++) Prop(x, 21, KFence);
            Patch(8f, 11f, 1.3f, 1.1f);
            Patch(30.5f, 9.5f, 1.6f, 1.2f);
            Patch(10f, 15.5f, 1.2f, 1.4f);
            Patch(31.5f, 20.5f, 1.2f, 1.3f);
            Patch(16f, 21.5f, 1.3f, 0.9f);
        }

        /// <summary>
        /// Step 11b: the puzzle gates. Open water covers the channel crossing until the lily pads lock in (LilyPadBridge),
        /// cracked bed covers the garden crossing until the lotus stones bloom (RainGarden). Both block walking.
        /// </summary>
        static void Gates()
        {
            for (int x = 1; x < Size - 1; x++)
                for (int y = 1; y < Size - 1; y++)
                {
                    if (!IsGate(x, y) || _g[x, y] != IslandMap.Path) continue;
                    Prop(x, y, KBedGap);
                }
            // Step 14a: the channel crossing is a plain plank bridge now (the stream by the arrival teaches the pads).
            for (int x = CrossingXY.x; x <= CrossingXY.x + 1; x++)
                for (int y = ChannelY0; y <= ChannelY1; y++)
                    Prop(x, y, KPlank);
        }

        /// <summary>Step 11b: the channel crossing's tiles or the garden crossing's tiles (x, y), for the pieces.</summary>
        public static List<Vector2Int> GateTiles(bool garden)
        {
            var list = new List<Vector2Int>();
            for (int y = 1; y < Size - 1; y++)
                for (int x = 1; x < Size - 1; x++)
                    if (IsGate(x, y) && IsGardenGate(x, y) == garden) list.Add(new Vector2Int(x, y));
            return list;
        }

        static void ChannelBanks()
        {
            // Reeds and lily pads along the channel; the crossing itself stays bare for 11b's pads.
            foreach (var x in new[] { 16, 21, 27, 39, 44, 47 }) Prop(x, ChannelY1 + 1 + (x % 2), KReeds);
            foreach (var x in new[] { 18, 24, 29, 37, 42 }) Prop(x, ChannelY0 - 1 - (x % 2), KReeds);
            foreach (var (x, y) in new[] { (17, 25), (22, 24), (26, 26), (30, 25), (37, 24), (41, 26), (45, 25) }) Prop(x, y, KLily);
            Prop(31, 22, KSign);
            Patch(24f, 29.5f, 1.8f, 1.2f);
            Patch(45f, 29.5f, 1.6f, 1.2f);
            Patch(19f, 21.5f, 1.2f, 0.8f);
            Patch(38f, 21f, 1.4f, 1f);
        }

        static void GardenDressing()
        {
            // Closed lotus buds in the cracked bed (they open once restored), a few stepping stones at its edge.
            foreach (var (x, y) in new[] { (37, 37), (38, 34), (43, 37), (44, 35), (38, 38), (43, 34) }) Prop(x, y, KLotus);
            foreach (var (x, y) in new[] { (35, 35), (46, 36) }) Prop(x, y, KReeds);
            Prop(37, 29, KSign);
            Patch(35f, 41f, 1.4f, 1.2f);
            Patch(46.5f, 40.5f, 1.4f, 1.2f);
            Patch(44.5f, 31f, 1.2f, 0.9f);
        }

        static void Basin()
        {
            Disc(BasinXY.x + 0.5f, 47.5f, 2.2f, IslandMap.Path);
            Fill(BasinXY.x - 1, BasinXY.x + 2, BasinXY.y - 1, BasinXY.y, IslandMap.Grass);
            Fill(WaterfallXY.x, WaterfallXY.x + 1, WaterfallXY.y, WaterfallXY.y, IslandMap.Grass);
            Prop(BasinXY, KBasin);
            Prop(WaterfallXY, KWaterfall);
            foreach (var (x, y) in new[] { (27, 49), (34, 49) }) Prop(x, y, KReeds);
            Patch(36f, 46.5f, 1.4f, 1f);
        }

        static void Temple()
        {
            Fill(TempleXY.x - 1, TempleXY.x + 3, TempleXY.y - 1, TempleXY.y, IslandMap.Grass);
            Prop(TempleXY, KTemple);
            // Step 11c: the return gate beside the temple, its front on the grass by the causeway.
            Ground(TempleGateXY.x, TempleGateXY.y, IslandMap.Grass);
            Ground(TempleGateXY.x + 1, TempleGateXY.y, IslandMap.Grass);
            for (int x = TempleGateXY.x; x <= TempleXY.x; x++) Ground(x, TempleGateXY.y - 1, IslandMap.Grass);
            Prop(TempleGateXY, KGate);
            // Lily pads and lotus on the lagoon's open water.
            foreach (var (x, y) in new[] { (14, 40), (19, 43), (21, 39), (16, 37), (13, 43) }) Prop(x, y, KLily);
            foreach (var (x, y) in new[] { (18, 41), (15, 42) }) Prop(x, y, KLotus);
        }

        static void Marsh()
        {
            // Tall reeds are the marsh's tall grass; reed clumps and cattails stand between them.
            Patch(3.8f, 19.5f, 1.1f, 1.7f);
            Patch(8.2f, 20.8f, 0.9f, 1.1f);
            foreach (var (x, y) in new[] { (8, 17), (3, 22), (9, 22) }) Prop(x, y, KReeds);
            // The lookout jetty: planks from the causeway out over the lagoon.
            for (int y = JettyXY.y; y <= JettyXY.y + 2; y++)
            {
                Ground(JettyXY.x, y, IslandMap.Path);
                Prop(JettyXY.x, y, KPlank);
            }
        }

        static void RarePond()
        {
            Disc(52.5f, 49.5f, 1.8f, IslandMap.Water);
            Patch(48f, 49f, 1.6f, 1.4f);
            Patch(53.5f, 45.5f, 1.4f, 1.2f);
            foreach (var (x, y) in new[] { (52, 50), (53, 49) }) Prop(x, y, KLily);
            Prop(55, 51, KReeds);
        }

        static void WindMeadow()
        {
            Patch(46f, 13f, 2.2f, 1.6f);
            Patch(42.5f, 19.5f, 1.4f, 1f);
            Patch(51f, 17f, 1.3f, 1.3f);
            Prop(48, 16, KStone);
        }

        // ---- filler: never more than 6 plain grass tiles in a row beside a path ----

        static void Filler()
        {
            int placed = 0;
            for (int pass = 0; pass < 6; pass++)
            {
                bool any = false;
                foreach (var run in ShoulderRuns())
                {
                    if (run.Count <= 6) continue;
                    var t = run[run.Count / 2];
                    PlaceFiller(t.x, t.y, placed++);
                    any = true;
                }
                if (!any) break;
            }
            _log?.Append($"\n  filler beside the paths: {placed}");
        }

        static void PlaceFiller(int x, int y, int i)
        {
            if (i % 3 == 2) Ground(x, y, IslandMap.Rock);
            else Prop(x, y, KReeds);
        }

        static List<List<Vector2Int>> ShoulderRuns()
        {
            bool Shoulder(int x, int y)
            {
                if (!In(x, y) || _g[x, y] != IslandMap.Grass || _p[x, y] != IslandMap.Empty || _covered.Contains(new Vector2Int(x, y))) return false;
                if (IsStory(x, y)) return false;
                return IsPath(x + 1, y) || IsPath(x - 1, y) || IsPath(x, y + 1) || IsPath(x, y - 1);
            }
            var runs = new List<List<Vector2Int>>();
            for (int dir = 0; dir < 2; dir++)
                for (int a = 1; a < Size - 1; a++)
                {
                    var run = new List<Vector2Int>();
                    for (int b = 1; b < Size; b++)
                    {
                        int x = dir == 0 ? b : a, y = dir == 0 ? a : b;
                        if (b < Size - 1 && Shoulder(x, y)) run.Add(new Vector2Int(x, y));
                        else
                        {
                            if (run.Count > 0) runs.Add(run);
                            run = new List<Vector2Int>();
                        }
                    }
                }
            return runs;
        }

        /// <summary>Tiles the story needs free (the crossing's banks, the basin and temple fronts).</summary>
        static bool IsStory(int x, int y)
        {
            if (x >= CrossingXY.x - 1 && x <= CrossingXY.x + 2 && (y == ChannelY0 - 1 || y == ChannelY1 + 1)) return true;
            if (y >= BasinXY.y - 2 && y <= BasinXY.y - 1 && x >= BasinXY.x - 1 && x <= BasinXY.x + 2) return true;
            return y >= TempleXY.y - 2 && y <= TempleXY.y - 1 && x >= TempleXY.x - 1 && x <= TempleXY.x + 3;
        }

        static bool IsPath(int x, int y) => In(x, y) && _g[x, y] == IslandMap.Path;

        /// <summary>
        /// A puzzle gate tile (x, y): the arrival stream's crossing (step 14a; it replaced the channel crossing's pads) or
        /// the garden crossing (11b B); open once solved.
        /// </summary>
        public static bool IsGate(int x, int y) => IsStreamGate(x, y) || IsGardenGate(x, y);

        /// <summary>The trail's tiles inside the garden's dry bed (x 40..41).</summary>
        public static bool IsGardenGate(int x, int y) =>
            x >= GardenPathX && x <= GardenPathX + 1 && Ell(GardenCenter, GardenRx, GardenRy, x, y) <= 1f;

        // ---- helpers ----

        static void Ground(int x, int y, char tile)
        {
            if (In(x, y)) _g[x, y] = tile;
        }

        static void Fill(int x0, int x1, int y0, int y1, char tile)
        {
            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++) Ground(x, y, tile);
        }

        static void Disc(float cx, float cy, float r, char tile)
        {
            for (int x = Mathf.FloorToInt(cx - r); x <= Mathf.CeilToInt(cx + r); x++)
                for (int y = Mathf.FloorToInt(cy - r); y <= Mathf.CeilToInt(cy + r); y++)
                    if (Sq(x - cx) + Sq(y - cy) <= r * r) Ground(x, y, tile);
        }

        static void Patch(float cx, float cy, float rx, float ry)
        {
            for (int x = Mathf.FloorToInt(cx - rx - 1); x <= Mathf.CeilToInt(cx + rx + 1); x++)
                for (int y = Mathf.FloorToInt(cy - ry - 1); y <= Mathf.CeilToInt(cy + ry + 1); y++)
                {
                    if (!In(x, y) || _p[x, y] != IslandMap.Empty || _covered.Contains(new Vector2Int(x, y)) || IsStory(x, y)) continue;
                    char ch = _g[x, y];
                    if (ch != IslandMap.Grass && ch != IslandMap.Tree && ch != IslandMap.Rock) continue;
                    if (y >= ChannelY0 && y <= ChannelY1) continue; // never opens the channel's wooded ends
                    float d = Sq((x - cx) / (rx + 0.5f)) + Sq((y - cy) / (ry + 0.5f));
                    if (d <= 1f - _rnd.Next() * 0.3f + 0.1f) _g[x, y] = IslandMap.TallGrass;
                }
        }

        static void Prop(Vector2Int xy, char key) => Prop(xy.x, xy.y, key);

        /// <summary>Places a prop with its anchor (north-west tile) at (x, y); its footprint spreads east and south. Logs if a tile is taken.</summary>
        static void Prop(int x, int y, char key)
        {
            var recipe = RecipeFor(key, out bool turned);
            if (recipe == null) return;
            var f = recipe.Footprint;
            if (turned) f = new Vector2Int(f.y, f.x);
            var tiles = new List<Vector2Int>();
            for (int dx = 0; dx < f.x; dx++)
                for (int dy = 0; dy < f.y; dy++)
                {
                    var t = new Vector2Int(x + dx, y - dy);
                    bool wet = In(t.x, t.y) && (_g[t.x, t.y] == IslandMap.Water || Bed(_g[t.x, t.y]));
                    if (!In(t.x, t.y) || _p[t.x, t.y] != IslandMap.Empty || _covered.Contains(t) ||
                        wet && !OnWater.Contains(key) || !wet && OnWater.Contains(key) ||
                        _g[t.x, t.y] == IslandMap.Path && !OnPath.Contains(key))
                    {
                        Debug.LogWarning($"[Lilymere] {recipe.name} at x{x} y{y}: tile x{t.x} y{t.y} is taken; skipped.");
                        return;
                    }
                    tiles.Add(t);
                }
            foreach (var t in tiles)
            {
                _covered.Add(t);
                char ch = _g[t.x, t.y];
                if (ch == IslandMap.Path || ch == IslandMap.Water || Bed(ch)) continue; // planks, stones and pads keep their ground
                if (f != Vector2Int.one || ch == IslandMap.Tree || ch == IslandMap.Rock) _g[t.x, t.y] = IslandMap.Grass;
            }
            _p[x, y] = key;
        }

        static PropRecipe RecipeFor(char key, out bool turned)
        {
            turned = false;
            foreach (var k in Keys)
                if (k.key == key)
                {
                    turned = k.turned;
                    return _recipes.TryGetValue(k.recipe, out var r) ? r : null;
                }
            return null;
        }

        /// <summary>Walkable tiles the explorer could never reach become woods; tall grass crumbs under 4 tiles become grass.</summary>
        static void SealCutOff()
        {
            // Nov 3: the stream's crossing is mud until the pads bridge it; it still leads on.
            bool Walkable(int x, int y) => In(x, y) && (IsStreamGate(x, y) || IslandMap.IsWalkable(_g[x, y]) && (!Blocks(x, y) || IsGate(x, y)));
            var seen = new HashSet<Vector2Int> { ArriveXY };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(ArriveXY);
            while (queue.Count > 0)
            {
                var t = queue.Dequeue();
                foreach (var d in new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
                {
                    var n = t + d;
                    if (seen.Contains(n) || !Walkable(n.x, n.y)) continue;
                    seen.Add(n);
                    queue.Enqueue(n);
                }
            }
            int sealedTiles = 0, crumbs = 0;
            for (int x = 1; x < Size - 1; x++)
                for (int y = 1; y < Size - 1; y++)
                    if (Walkable(x, y) && !seen.Contains(new Vector2Int(x, y)))
                    {
                        _g[x, y] = IslandMap.Tree;
                        sealedTiles++;
                    }
            var visited = new HashSet<Vector2Int>();
            for (int x = 1; x < Size - 1; x++)
                for (int y = 1; y < Size - 1; y++)
                {
                    if (_g[x, y] != IslandMap.TallGrass || visited.Contains(new Vector2Int(x, y))) continue;
                    var part = new List<Vector2Int>();
                    var stack = new Stack<Vector2Int>();
                    stack.Push(new Vector2Int(x, y));
                    visited.Add(new Vector2Int(x, y));
                    while (stack.Count > 0)
                    {
                        var t = stack.Pop();
                        part.Add(t);
                        foreach (var d in new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
                        {
                            var n = t + d;
                            if (!In(n.x, n.y) || visited.Contains(n) || _g[n.x, n.y] != IslandMap.TallGrass) continue;
                            visited.Add(n);
                            stack.Push(n);
                        }
                    }
                    if (part.Count >= 4) continue;
                    foreach (var t in part) _g[t.x, t.y] = IslandMap.Grass;
                    crumbs += part.Count;
                }
            _log?.Append($"\n  sealed {sealedTiles} unreachable tile(s), {crumbs} tall grass crumb(s) back to grass");
        }

        /// <summary>Perf: trees behind the first row along walkable ground or water keep about 60%; the rest turn to rocks.</summary>
        static void ThinBackWoods()
        {
            int thinned = 0;
            for (int x = 1; x < Size - 1; x++)
                for (int y = 1; y < Size - 1; y++)
                {
                    if (_g[x, y] != IslandMap.Tree) continue;
                    bool near = false;
                    for (int dx = -1; dx <= 1 && !near; dx++)
                        for (int dy = -1; dy <= 1 && !near; dy++)
                        {
                            int nx = x + dx, ny = y + dy;
                            near = In(nx, ny) && (IslandMap.IsWalkable(_g[nx, ny]) || _g[nx, ny] == IslandMap.Water || Bed(_g[nx, ny]));
                        }
                    if (near || _rnd.Next() >= 0.4f) continue;
                    _g[x, y] = IslandMap.Rock;
                    thinned++;
                }
            _log?.Append($"\n  back woods thinned: {thinned} to rocks");
        }

        static bool Blocks(int x, int y)
        {
            if (!_covered.Contains(new Vector2Int(x, y))) return false;
            for (int ax = x - 2; ax <= x; ax++)
                for (int ay = y; ay <= y + 2; ay++)
                {
                    if (!In(ax, ay) || _p[ax, ay] == IslandMap.Empty) continue;
                    var r = RecipeFor(_p[ax, ay], out bool turned);
                    if (r == null) continue;
                    var f = turned ? new Vector2Int(r.Footprint.y, r.Footprint.x) : r.Footprint;
                    if (x < ax + f.x && ay - y < f.y) return r.blocksWalking;
                }
            return false;
        }

        static void CleanUnderProps()
        {
            foreach (var t in _covered)
                if (_g[t.x, t.y] == IslandMap.TallGrass) _g[t.x, t.y] = IslandMap.Grass;
        }

        static float DistToPolyline(Vector2 p, Vector2[] pts)
        {
            float best = float.MaxValue;
            for (int i = 0; i + 1 < pts.Length; i++)
            {
                Vector2 a = pts[i], ab = pts[i + 1] - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                best = Mathf.Min(best, (a + ab * t - p).magnitude);
            }
            return best;
        }

        static List<Vector2Int> Walk4(Vector2Int a, Vector2Int b)
        {
            var list = new List<Vector2Int> { a };
            var p = a;
            for (int guard = 0; p != b && guard < 200; guard++)
            {
                var d = b - p;
                p = d.x != 0 ? new Vector2Int(p.x + System.Math.Sign(d.x), p.y) : new Vector2Int(p.x, p.y + System.Math.Sign(d.y));
                list.Add(p);
            }
            return list;
        }

        public static Vector2Int VillageSpot => Tile(PlazaXY);

        // ================= checks =================

        /// <summary>
        /// The automated layout checks (also an EditMode test): the trail connects the arrival, the shrine, the village, the
        /// channel crossing, the garden, the basin and the temple door; every pocket is reachable; nothing walkable is cut off;
        /// the channel is only crossed at the crossing and the garden only through its gate tiles (so 11b's lily pads and
        /// lotus stones gate them); the lagoon has dry bed to rise; tall grass share and patch sizes; the edge ring is woods.
        /// Returns the problems (empty = fine).
        /// </summary>
        public static List<string> Check(WorldMap map, StringBuilder report)
        {
            var problems = new List<string>();
            if (map.Columns != Size || map.Rows != Size) problems.Add($"map is {map.Columns}x{map.Rows}, not {Size}x{Size}");

            // closed = gate tiles (x, y) that stay shut; every other puzzle gate counts as open (solved).
            bool Walk(Vector2Int t, ICollection<Vector2Int> closed)
            {
                var xy = new Vector2Int(t.x, Size - 1 - t.y);
                if (closed != null && closed.Contains(xy)) return false;
                // The stream's crossing is mud until the pads bridge it.
                return map.CanWalk(t) || IsStreamGate(xy.x, xy.y) || IsGate(xy.x, xy.y) && IslandMap.IsWalkable(map.At(t));
            }
            HashSet<Vector2Int> Flood(ICollection<Vector2Int> closed)
            {
                var seen = new HashSet<Vector2Int>();
                var queue = new Queue<Vector2Int>();
                if (!map.CanWalk(Arrive)) return seen;
                seen.Add(Arrive);
                queue.Enqueue(Arrive);
                while (queue.Count > 0)
                {
                    var t = queue.Dequeue();
                    foreach (var d in new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
                    {
                        var n = t + d;
                        if (seen.Contains(n) || !Walk(n, closed)) continue;
                        seen.Add(n);
                        queue.Enqueue(n);
                    }
                }
                return seen;
            }
            if (!map.CanWalk(Arrive)) problems.Add($"arrival tile c{Arrive.x} r{Arrive.y} isn't walkable");
            var reach = Flood(null);
            void Need(string what, Vector2Int xy)
            {
                if (!reach.Contains(Tile(xy))) problems.Add($"{what} (x{xy.x} y{xy.y}) not reachable from the arrival");
            }
            Need("shrine front", ShrineXY + new Vector2Int(0, -2));
            Need("village plaza", PlazaXY);
            Need("channel crossing", CrossingXY);
            Need("stream crossing", new Vector2Int(11, StreamY0));
            Need("garden crossing", new Vector2Int(GardenPathX, Mathf.RoundToInt(GardenCenter.y)));
            Need("basin front", BasinXY + new Vector2Int(0, -2));
            Need("temple door front", TempleXY + new Vector2Int(1, -2));
            Need("temple return gate front", TempleGateXY + Vector2Int.down);
            Need("return gate front", GateXY + Vector2Int.down);
            Need("jetty end", JettyXY);
            foreach (var z in new[] { "arrival", "village", "channel", "marsh", "garden", "pond", "wind", "basin" })
                if (!reach.Any(t => map.ZoneAt(t) == z && map.CanWalk(t))) problems.Add($"pocket '{z}' has no reachable tile");

            // Step 14a: the stream crossing is the only way off the arrival meadow, the garden gate the only way past the
            // garden. The channel crossing is a plank bridge (always open) but still the only way over the channel.
            var noStream = new HashSet<Vector2Int>();
            var noCrossing = new HashSet<Vector2Int>();
            var noGarden = new HashSet<Vector2Int>();
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                {
                    if (IsStreamGate(x, y)) noStream.Add(new Vector2Int(x, y));
                    if (x >= CrossingXY.x && x <= CrossingXY.x + 1 && y >= ChannelY0 && y <= ChannelY1) noCrossing.Add(new Vector2Int(x, y));
                    if (IsGardenGate(x, y)) noGarden.Add(new Vector2Int(x, y));
                }
            if (Flood(noStream).Contains(Tile(PlazaXY)))
                problems.Add("the village is reachable without the stream crossing (the stream leaks)");
            if (Flood(noCrossing).Contains(Tile(GardenPathX, Mathf.RoundToInt(GardenCenter.y - GardenRy) - 1)))
                problems.Add("the garden is reachable without the channel crossing (the channel leaks)");
            if (Flood(noGarden).Contains(Tile(BasinXY + new Vector2Int(0, -2))))
                problems.Add("the basin is reachable without crossing the garden (the garden leaks)");

            int walkable = 0, tall = 0, cut = 0, dry = 0;
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                {
                    var t = new Vector2Int(c, r);
                    if (map.Raw(r, c) == IslandMap.DryBed) dry++;
                    if (!Walk(t, null)) continue;
                    walkable++;
                    if (map.At(t) == IslandMap.TallGrass) tall++;
                    if (!reach.Contains(t)) cut++;
                }
            if (cut > 0) problems.Add($"{cut} walkable tile(s) cut off from everything");
            if (dry < 40) problems.Add($"only {dry} dry bed tiles (the lagoon needs a ring to rise)");
            float share = walkable > 0 ? tall / (float)walkable : 0f;
            if (share < 0.12f || share > 0.28f) problems.Add($"tall grass is {share:P0} of the walkable land (want ~15-25%)");
            var patches = Components(map, t => map.At(t) == IslandMap.TallGrass);
            int big = patches.Count(p => p > 22);
            if (big > 0) problems.Add($"{big} tall grass patch(es) over 22 tiles: {string.Join(", ", patches.Where(p => p > 22))}");

            int bare = 0, ring = 0;
            for (int i = 1; i < Size - 1; i++)
                foreach (var t in new[] { new Vector2Int(i, 1), new Vector2Int(i, Size - 2), new Vector2Int(1, i), new Vector2Int(Size - 2, i) })
                {
                    ring++;
                    char ch = map.At(t);
                    if (ch != IslandMap.Tree && ch != IslandMap.Rock && map.PropAt(t.y, t.x) == IslandMap.Empty) bare++;
                }
            if (bare > ring * 0.15f) problems.Add($"{bare} of {ring} tiles along the map edge are bare");

            report?.Append($"\n  walkable {walkable}, reachable {reach.Count}, dry bed {dry}, tall grass {share:P0} in {patches.Count} patches " +
                           $"(sizes {string.Join(" ", patches.OrderBy(p => p))}), bare edge tiles {bare}/{ring}");
            return problems;
        }

        static List<int> Components(WorldMap map, System.Func<Vector2Int, bool> member)
        {
            var seen = new HashSet<Vector2Int>();
            var sizes = new List<int>();
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                {
                    var s = new Vector2Int(c, r);
                    if (seen.Contains(s) || !member(s)) continue;
                    int n = 0;
                    var stack = new Stack<Vector2Int>();
                    stack.Push(s);
                    seen.Add(s);
                    while (stack.Count > 0)
                    {
                        var t = stack.Pop();
                        n++;
                        foreach (var d in new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
                        {
                            var q = t + d;
                            if (q.x < 0 || q.y < 0 || q.x >= Size || q.y >= Size || seen.Contains(q) || !member(q)) continue;
                            seen.Add(q);
                            stack.Push(q);
                        }
                    }
                    sizes.Add(n);
                }
            return sizes;
        }
    }
}
