using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using static Diorama.EditorTools.StarterKit;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 10a: the Galewick map, 60 x 60 (windmill highlands, RealmsDesign.md). One trail through five beats: arrival
    /// meadow + return gate, windmill village (cottages, two small mills, the big mill by a ditch: the drawbridge spot,
    /// step 10b), the kite field (an open loop with three kite spots), the windy ridge (a narrow path where the cloud bank
    /// sits, 10b) and the wind tower on the ridge top. Side pockets: a fire-visitor patch, a rare grass patch, a shrine near
    /// the village. Rolling hills come from hillocks (the theme's tree prop on F tiles), dry-stone walls and fences.
    /// Drawn from code with a fixed seed like <see cref="CinderwoodTools"/>; same coordinates: (x west to east, y south to
    /// north; south = the arrival, nearest the player), <see cref="Tile"/> turns them into (column, row).
    /// </summary>
    public static class GalewickTools
    {
        public const string MapPath = "Assets/_Project/Data/WorldMap_Galewick.asset";
        public const string ConfigPath = "Assets/_Project/Data/Galewick/GalewickConfig.asset";
        const string TableRoot = "Assets/_Project/Data/Battle/Galewick";
        public const int Size = 60;
        const int Seed = 1021;

        // ---- story tiles (x, y) ----
        public static readonly Vector2Int GateXY = new Vector2Int(7, 6);      // return gate, 2 x 1 (front y 5)
        public static readonly Vector2Int ArriveXY = new Vector2Int(10, 5);   // arrive here facing north
        public static readonly Vector2Int ShrineXY = new Vector2Int(14, 19);  // 2 x 2, front y 17
        /// <summary>The big mill (2 x 2, anchor = its north-west tile), on the ditch's south bank west of the crossing.</summary>
        public static readonly Vector2Int MillXY = new Vector2Int(28, 25);
        /// <summary>The ditch rows (water) and the 2-wide crossing over it (the drawbridge, step 10b).</summary>
        public const int DitchY0 = 27, DitchY1 = 28;
        public static readonly Vector2Int CrossingXY = new Vector2Int(32, 28); // west-north tile; covers x 32..33, y 27..28
        /// <summary>The kite field loop's centre and radii (tiles).</summary>
        public static readonly Vector2 KiteCenter = new Vector2(40f, 36f);
        public const float KiteRx = 7f, KiteRy = 4.6f;
        /// <summary>The three kite spots (10b), on the grass beside the loop.</summary>
        public static readonly Vector2Int[] KiteXY = { new Vector2Int(35, 37), new Vector2Int(43, 39), new Vector2Int(44, 34) };
        /// <summary>The narrow ridge path's cloud bank tiles (10b: three puffs block it, west to east).</summary>
        public static readonly Vector2Int[] CloudXY = { new Vector2Int(21, 47), new Vector2Int(22, 47), new Vector2Int(23, 47) };
        public static readonly Vector2Int TowerXY = new Vector2Int(17, 56);   // 2 x 2, door front y 54
        /// <summary>Step 10d: the tower's return gate (2 x 1), hidden until the Keeper is calmed.</summary>
        public static readonly Vector2Int TowerGateXY = new Vector2Int(23, 53);
        static readonly Vector2Int FirstGrassXY = new Vector2Int(14, 8);
        static readonly Vector2Int PlazaXY = new Vector2Int(24, 20);

        public static Vector2Int Tile(Vector2Int xy) => new Vector2Int(xy.x, Size - 1 - xy.y);
        public static Vector2Int Tile(int x, int y) => new Vector2Int(x, Size - 1 - y);
        public static Vector2Int ReturnGate => Tile(GateXY);
        public static Vector2Int Arrive => Tile(ArriveXY);
        public static readonly Vector2Int ArriveFacing = new Vector2Int(0, -1);
        /// <summary>The tower door's front tiles (where the explorer stands to enter, step 10b).</summary>
        public static Vector2Int TowerFront => Tile(TowerXY.x, TowerXY.y - 2);

        // The main trail (2 wide around the centre line). The kite loop and the narrow ridge path are drawn separately.
        // Oct 27 (Mugesh: zig-zag paths are hard to walk): every leg runs straight north-south or east-west, so a swipe
        // walks a whole leg; diagonal legs rasterised into one-tile stair steps that stopped the walk at every jog.
        static readonly Vector2[] Trail =
        {
            new Vector2(10.5f, 3.5f), new Vector2(10.5f, 13.5f), new Vector2(24.5f, 13.5f), new Vector2(24.5f, 22.5f),
            new Vector2(32.5f, 22.5f), new Vector2(32.5f, 33.5f),
        };
        // North of the kite loop up to the ridge's east end, and from the ridge's west end up to the tower plaza.
        static readonly Vector2[] TrailNorth =
        {
            new Vector2(36.5f, 40.5f), new Vector2(36.5f, 45.5f), new Vector2(27.5f, 45.5f), new Vector2(27.5f, 47f),
        };
        static readonly Vector2[] TrailTop =
        {
            new Vector2(19.5f, 47f), new Vector2(19.5f, 52.5f),
        };
        // Side paths (1 wide), straight legs between the corners.
        static readonly Vector2Int[][] SidePaths =
        {
            new[] { new Vector2Int(29, 22), new Vector2Int(29, 18), new Vector2Int(44, 18), new Vector2Int(44, 15), new Vector2Int(48, 15) }, // fire-visitor patch
            new[] { new Vector2Int(47, 38), new Vector2Int(51, 38), new Vector2Int(51, 49) },                                                 // rare pocket
            new[] { new Vector2Int(14, 13), new Vector2Int(14, 16) },                                                                         // shrine
        };
        /// <summary>The kite field's loop (Oct 27: a rectangle, was an ellipse of stair steps): x and y ranges of its 1-wide ring.</summary>
        static readonly RectInt KiteLoop = new RectInt(33, 32, 14, 8);

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
            new Zone('v', "village", 4, 40, 9, 26),
            new Zone('a', "arrival", 4, 18, 2, 10),
            new Zone('f', "fire", 41, 56, 8, 24),
            new Zone('k', "kites", 26, 50, 29, 43),
            new Zone('r', "ridge", 10, 36, 44, 50),
            new Zone('x', "rare", 44, 57, 44, 57),
            new Zone('t', "tower", 8, 30, 51, 58),
        };

        // ---- prop keys ----
        const char KGate = 'Z', KShrine = 'Y', KCottage = 'h', KMill = 'M', KSmallMill = 'm', KWall = 'w', KWallTurned = 'W',
            KFence = '-', KFenceTurned = '|', KSign = 's', KKitePost = 'p', KHay = 'b', KCloud = 'c', KTower = 'T',
            KKiteRed = 'k', KKiteTeal = 'K', KKiteGold = 'g', KVent = 'e', KLeaves = 'v', KStones = 'o',
            KBridge = 'D', KBank = 'C';

        static readonly (char key, string recipe, bool turned)[] Keys =
        {
            (KGate, "ReturnGate", false), (KShrine, HearthShrines.RecipeName, false), (KCottage, "Cottage", false),
            (KMill, "Windmill", false), (KSmallMill, "SmallMill", false), (KWall, "StoneWall", false), (KWallTurned, "StoneWall", true),
            (KFence, "Fence", false), (KFenceTurned, "Fence", true), (KSign, "Signpost", false), (KKitePost, "KitePost", false),
            (KHay, "HayBale", false), (KCloud, "CloudPuff", false), (KTower, "WindTower", false),
            (KKiteRed, "Kite", false), (KKiteTeal, "KiteTeal", false), (KKiteGold, "KiteGold", false),
            (KVent, "EmberVent", false), (KLeaves, "LeafPile", false), (KStones, "SteppingStones", false),
            (KBridge, "Drawbridge", false), (KBank, "CloudPuff", false),
        };

        // ---- generation state ----
        static char[,] _g, _p, _z; // [x, y]
        static HashSet<Vector2Int> _covered;
        static Dictionary<string, PropRecipe> _recipes;
        static SeededRandom _rnd;
        static StringBuilder _log;

        // ================= write =================

        /// <summary>Writes (or rewrites) the Galewick map asset, its zone tables and legend. Returns it.</summary>
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
                if (r == null) Debug.LogError($"[Galewick] Prop {k.recipe} missing (write the prop kits first).");
                _recipes[k.recipe] = r;
            }

            Generate();

            map.seed = Seed;
            map.tileSize = 0.05f;
            if (island != null) map.capHeight = island.capHeight;
            map.viewZoom = 1f;
            map.startTile = Arrive;
            map.theme = PropKit.Theme("Galewick");
            map.realm = "galewick";
            map.arrivalLine = "realm.galewick.first";
            map.propLegend = Legend();
            map.SetLayout(Text(_g));
            map.SetProps(Text(_p));
            map.SetZones(Text(_z));
            // Placeholder tables until step 10c's wind roster: Pufflet (the wind starter) and fire visitors. Levels like
            // Cinderwood's (+ its wild bonus); shards held add more at runtime (ShardScaling).
            // Step 10c: once the wind roster exists, ~70% wind, ~20% fire visitors, the rare pocket's Zeppelope ~5%.
            bool wind = AssetDatabase.LoadAssetAtPath<IslingSpecies>("Assets/_Project/Data/Creatures/Species/Kitelet.asset") != null;
            map.grassTable = wind
                ? Table("Encounters_Galewick", 5, 7, ("Kitelet", 40f, false), ("Whirlby", 30f, false), ("Toastling", 30f, false))
                : Table("Encounters_Galewick", 5, 7, ("Pufflet", 50f, false), ("Kettlewing", 30f, false), ("Toastling", 20f, false));
            map.zoneLegend = new List<MapZone>();
            foreach (var z in Zones)
            {
                var table = wind ? WindTable(z.id) : z.id switch
                {
                    "arrival" => Table("Encounters_GwArrival", 3, 5, ("Pufflet", 55f, false), ("Kettlewing", 25f, false), ("Toastling", 20f, false)),
                    "village" => Table("Encounters_GwVillage", 3, 5, ("Pufflet", 45f, false), ("Kettlewing", 30f, false), ("Toastling", 25f, false)),
                    "kites" => Table("Encounters_GwKites", 4, 6, ("Pufflet", 50f, false), ("Kettlewing", 35f, false), ("Toastling", 15f, false)),
                    "ridge" => Table("Encounters_GwRidge", 5, 7, ("Pufflet", 50f, false), ("Kettlewing", 30f, false), ("Coalbeetle", 20f, false)),
                    "rare" => Table("Encounters_GwRare", 6, 8, ("Pufflet", 60f, false), ("Kettlewing", 30f, false), ("Embercrane", 10f, true)),
                    "fire" => Table("Encounters_GwFire", 4, 6, ("Toastling", 35f, false), ("Coalbeetle", 35f, false), ("Ashmole", 30f, false)),
                    _ => null,
                };
                map.zoneLegend.Add(new MapZone { key = z.key.ToString(), id = z.id, grassTable = table });
            }
            map.landmarks = new List<Landmark>
            {
                new Landmark { name = "start", tile = Arrive },
                new Landmark { name = "firstGrass", tile = Tile(FirstGrassXY) },
                new Landmark { name = "plaza", tile = Tile(PlazaXY), line = "gw.village.first", radius = 4 },
                new Landmark { name = "shrine", tile = Tile(ShrineXY) },
                new Landmark { name = "mill", tile = Tile(MillXY.x + 1, MillXY.y - 2), line = "gw.mill.first", radius = 3 },
                new Landmark { name = "kites", tile = Tile(Vector2Int.RoundToInt(KiteCenter)), line = "gw.kites.first", radius = 6 },
                new Landmark { name = "ridge", tile = Tile(CloudXY[2] + Vector2Int.right * 2), line = "gw.ridge.first", radius = 3 },
                new Landmark { name = "tower", tile = TowerFront, line = "gw.tower.first", radius = 4 },
                new Landmark { name = "rare", tile = Tile(51, 51), line = "gw.rare.first", radius = 4 },
                new Landmark { name = "fire", tile = Tile(49, 15) },
            };
            EditorUtility.SetDirty(map);
            AssetDatabase.SaveAssets();
            log.Append($"\n  {MapPath}: {Size}x{Size} Galewick (seed {Seed}), arrive c{Arrive.x} r{Arrive.y} facing north, " +
                       $"return gate c{ReturnGate.x} r{ReturnGate.y}, {map.zoneLegend.Count} zones, {map.landmarks.Count} landmarks");
            return map;
        }

        /// <summary>Step 10c: the zone tables with the wind roster.</summary>
        static EncounterTable WindTable(string zone) => zone switch
        {
            "arrival" => Table("Encounters_GwArrival", 3, 5, ("Kitelet", 35f, false), ("Whirlby", 30f, false), ("Pufflet", 15f, false), ("Toastling", 20f, false)),
            "village" => Table("Encounters_GwVillage", 3, 5, ("Whirlby", 30f, false), ("Kitelet", 25f, false), ("Vanehare", 20f, false), ("Toastling", 15f, false), ("Coalbeetle", 10f, false)),
            "kites" => Table("Encounters_GwKites", 4, 6, ("Kitelet", 40f, false), ("Vanehare", 25f, false), ("Samarling", 15f, false), ("Kettlewing", 20f, false)),
            "ridge" => Table("Encounters_GwRidge", 5, 7, ("Samarling", 30f, false), ("Vanehare", 25f, false), ("Whirlby", 20f, false), ("Coalbeetle", 15f, false), ("Pufflet", 10f, false)),
            "rare" => Table("Encounters_GwRare", 6, 8, ("Samarling", 35f, false), ("Vanehare", 30f, false), ("Kitelet", 30f, false), ("Zeppelope", 5f, true)),
            "fire" => Table("Encounters_GwFire", 4, 6, ("Toastling", 30f, false), ("Coalbeetle", 30f, false), ("Ashmole", 30f, false), ("Whirlby", 10f, false)),
            _ => null,
        };

        /// <summary>The Galewick config asset (wind gate open in the Editor, sealed in builds), created once.</summary>
        public static GalewickConfig Config()
        {
            var c = AssetDatabase.LoadAssetAtPath<GalewickConfig>(ConfigPath);
            if (c != null) return c;
            DioramaSceneSetup.EnsureFolder("Assets/_Project/Data/Galewick");
            c = ScriptableObject.CreateInstance<GalewickConfig>();
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
            var table = Asset<EncounterTable>($"{TableRoot}/{name}.asset");
            table.habitat = Habitat.Grass;
            var list = new List<EncounterTable.Entry>();
            foreach (var (id, weight, rare) in entries)
            {
                var species = AssetDatabase.LoadAssetAtPath<IslingSpecies>($"Assets/_Project/Data/Creatures/Species/{id}.asset");
                if (species == null)
                {
                    Debug.LogError($"[Galewick] Species {id} not found for {name}.");
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

            // Hills everywhere outside the open places (hillocks are the theme's tree), a few lone ones inside.
            for (int x = 1; x < Size - 1; x++)
                for (int y = 1; y < Size - 1; y++)
                {
                    float open = Openness(x, y);
                    int edge = Mathf.Min(Mathf.Min(x, y), Mathf.Min(Size - 1 - x, Size - 1 - y));
                    float hill = open > 0f ? open : edge <= 2 ? 0.95f : 0.78f;
                    float roll = _rnd.Next();
                    if (roll < hill) _g[x, y] = IslandMap.Tree;
                    else if (open == 0f) _g[x, y] = IslandMap.Rock;
                }

            Ditch();
            RidgeBand();

            // The main trail (2 wide) with a grass shoulder, the north legs, the side paths (1 wide).
            foreach (var line in new[] { Trail, TrailNorth, TrailTop })
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
            Village();
            KiteField();
            Ridge();
            Tower();
            FirePatch();
            RarePocket();
            Filler();
            CleanUnderProps();
            SealCutOff();
            ThinBackHills();
        }

        /// <summary>Tree (hillock) chance inside an open place (0 = not open: hill rules apply).</summary>
        static float Openness(int x, int y)
        {
            bool InEll(float cx, float cy, float rx, float ry) => Sq((x - cx) / rx) + Sq((y - cy) / ry) <= 1f + (_rnd.Next() - 0.5f) * 0.25f;
            bool Rect(int x0, int x1, int y0, int y1) => x >= x0 && x <= x1 && y >= y0 && y <= y1;
            if (Rect(5, 17, 2, 11)) return 0.004f;                                      // arrival meadow
            if (Rect(10, 38, 11, 26)) return 0.012f;                                    // village and its fields
            if (InEll(KiteCenter.x, KiteCenter.y, KiteRx + 4.5f, KiteRy + 4.5f)) return 0.006f; // kite field
            if (InEll(48f, 16f, 6.5f, 6f)) return 0.01f;                                // fire-visitor patch
            if (InEll(51f, 51f, 5f, 4.5f)) return 0.004f;                               // rare pocket
            if (Rect(11, 26, 50, 55)) return 0.002f;                                    // tower plaza
            return 0f;
        }

        static float Sq(float v) => v * v;

        // ---- beats ----

        /// <summary>The ditch across the whole map (water, hills at its ends) with the 2-wide crossing (path in 10a; 10b's drawbridge).</summary>
        static void Ditch()
        {
            for (int x = 1; x < Size - 1; x++)
                for (int y = DitchY0; y <= DitchY1; y++)
                {
                    bool crossing = x >= CrossingXY.x && x <= CrossingXY.x + 1;
                    if (crossing) _g[x, y] = IslandMap.Path;
                    else if (x >= 16 && x <= 46) _g[x, y] = IslandMap.Water;
                    else _g[x, y] = IslandMap.Tree;
                }
        }

        /// <summary>A band of hills under the ridge (y 45..48): past it only by the narrow ridge path (the cloud bank, 10b).</summary>
        static void RidgeBand()
        {
            for (int x = 1; x < Size - 1; x++)
                for (int y = 46; y <= 48; y++)
                    if (x < 44) _g[x, y] = y == 47 ? IslandMap.Tree : (_rnd.Next() < 0.7f ? IslandMap.Tree : IslandMap.Rock);
        }

        static void Arrival()
        {
            Fill(6, 12, 3, 5, IslandMap.Path);
            Prop(GateXY, KGate);
            Prop(9, 8, KSign);
            Patch(FirstGrassXY.x, FirstGrassXY.y, 1.8f, 1.4f);
            Patch(6f, 9.5f, 1.4f, 1.2f);
            Patch(15.5f, 9f, 1.3f, 1.2f);
            Prop(15, 4, KHay);
            Prop(16, 6, KKitePost);
            for (int x = 3; x <= 5; x++) Prop(x, 11, KWall);
        }

        static void Village()
        {
            Disc(PlazaXY.x + 0.5f, PlazaXY.y + 0.5f, 2.3f, IslandMap.Path);
            foreach (var (x, y) in new[] { (19, 24), (25, 25), (18, 18), (26, 18) }) Prop(x, y, KCottage);
            foreach (var (x, y) in new[] { (17, 21), (31, 20) }) Prop(x, y, KSmallMill);
            Prop(MillXY, KMill);
            Prop(ShrineXY, KShrine);
            foreach (var (x, y) in new[] { (22, 22), (30, 21), (35, 19), (35, 24), (12, 23) }) Prop(x, y, KHay);
            foreach (var (x, y) in new[] { (23, 15), (30, 16) }) Prop(x, y, KKitePost);
            Prop(26, 15, KSign);
            Prop(34, 22, KSign);
            // Fields: dry-stone walls along their edges, a fenced garden behind the cottages.
            for (int x = 10; x <= 15; x++) Prop(x, 26, KWall);
            for (int y = 19; y <= 23; y++) Prop(37, y, KWallTurned);
            for (int x = 22; x <= 26; x++) Prop(x, 12, KWall);
            for (int x = 21; x <= 24; x++) Prop(x, 26, KFence);
            Patch(13f, 15f, 1.7f, 1.4f);
            Patch(33f, 14f, 1.8f, 1.4f);
            Patch(11f, 21.5f, 1.4f, 1.5f);
            Patch(37.5f, 16f, 1.3f, 1.6f);
            Patch(34f, 25f, 1.2f, 0.8f);
            Patch(15.5f, 23.5f, 1.6f, 1.2f);
            Patch(29f, 13.5f, 1.6f, 1.1f);
            Patch(21.5f, 20.5f, 1.2f, 1f);
        }

        static void KiteField()
        {
            // The loop: a 1-wide rectangular ring path round the open middle.
            for (int x = 1; x < Size - 1; x++)
                for (int y = 28; y < 46; y++)
                {
                    float e = Mathf.Sqrt(Sq((x - KiteCenter.x) / KiteRx) + Sq((y - KiteCenter.y) / KiteRy));
                    bool inX = x >= KiteLoop.xMin && x <= KiteLoop.xMax, inY = y >= KiteLoop.yMin && y <= KiteLoop.yMax;
                    if (inX && (y == KiteLoop.yMin || y == KiteLoop.yMax) || inY && (x == KiteLoop.xMin || x == KiteLoop.xMax)) _g[x, y] = IslandMap.Path;
                    else if (e < 1.45f && (_g[x, y] == IslandMap.Tree || _g[x, y] == IslandMap.Rock)) _g[x, y] = IslandMap.Grass;
                }
            foreach (var (x, y, rx, ry) in new[] { (40f, 36f, 2.4f, 1.6f), (31.5f, 33f, 1.5f, 1.3f), (47f, 37f, 1.4f, 2f), (38f, 42.5f, 1.8f, 0.9f), (43f, 30.5f, 1.8f, 0.8f) })
                Patch(x, y, rx, ry);
            foreach (var (x, y) in new[] { (32, 39), (48, 34), (40, 30) }) Prop(x, y, KKitePost);
            Patch(33f, 41f, 1.2f, 1f);
            Patch(46.5f, 41.5f, 1.2f, 1f);
            foreach (var (x, y) in new[] { (29, 36), (49, 40) }) Prop(x, y, KHay);
            Prop(KiteXY[0], KKiteRed);
            Prop(KiteXY[1], KKiteTeal);
            Prop(KiteXY[2], KKiteGold);
            Prop(35, 31, KSign);
            for (int y = 31; y <= 35; y++) Prop(27, y, KWallTurned);
        }

        static void Ridge()
        {
            // Step 10b: the cloud bank (three puffs on the narrow path) and the drawbridge (raised) are puzzle gates.
            foreach (var c in CloudXY) Ground(c.x, c.y, IslandMap.Path);
            // The narrow 1-wide ridge path through the hill band (y 47), the cloud bank's tiles on it.
            for (int x = 19; x <= 26; x++) Ground(x, 47, IslandMap.Path);
            for (int x = 19; x <= 26; x++)
            {
                Ground(x, 46, IslandMap.Tree);
                if (x >= 21) Ground(x, 48, IslandMap.Tree);
            }
            // Step 11d (Note 7): low rocks, not hillocks, right beside the cloud bank, so its puffs aren't hidden.
            for (int x = CloudXY[0].x - 1; x <= CloudXY[CloudXY.Length - 1].x + 1; x++)
            {
                Ground(x, 46, IslandMap.Rock);
                Ground(x, 48, IslandMap.Rock);
            }
            // Clouds parked on the hills either side (the bank on the path comes in 10b).
            foreach (var (x, y) in new[] { (27, 44), (16, 46), (30, 42), (13, 49) }) Prop(x, y, KCloud);
            Patch(30f, 44.5f, 1.6f, 1f);
            Patch(25f, 43.5f, 1.4f, 0.9f);
            Prop(28, 47, KSign);
            foreach (var c in CloudXY) Prop(c, KBank);
            Prop(CrossingXY, KBridge);
        }

        static void Tower()
        {
            Disc(18f, 52.5f, 2.6f, IslandMap.Path);
            Fill(TowerXY.x - 1, TowerXY.x + 2, TowerXY.y - 1, TowerXY.y, IslandMap.Grass);
            Prop(TowerXY, KTower);
            // Step 11d (Note 9): the return gate beside the tower (hidden until Galeowl is calmed), its front on the plaza.
            for (int x = 20; x <= TowerGateXY.x + 1; x++) Ground(x, TowerGateXY.y - 1, IslandMap.Path);
            Ground(TowerGateXY.x, TowerGateXY.y, IslandMap.Path);
            Ground(TowerGateXY.x + 1, TowerGateXY.y, IslandMap.Path);
            Prop(TowerGateXY, KGate);
            for (int x = 12; x <= 24; x++)
                if (x < TowerXY.x - 1 || x > TowerXY.x + 2) Ground(x, 56, IslandMap.Tree);
            foreach (var (x, y) in new[] { (14, 51), (22, 51) }) Prop(x, y, KKitePost);
            Prop(12, 53, KCloud);
            Prop(24, 54, KCloud);
            Patch(13.5f, 51.5f, 1.2f, 1f);
            Patch(23f, 51f, 1.3f, 1f);
        }

        static void FirePatch()
        {
            // Fire visitors: a scorched corner with a few ember vents and leaf piles (fire beats wind).
            foreach (var (x, y) in new[] { (46, 18), (51, 14), (48, 11), (52, 19) }) Prop(x, y, KVent);
            foreach (var (x, y) in new[] { (50, 17), (45, 13) }) Prop(x, y, KLeaves);
            Patch(48f, 16f, 2.2f, 1.6f);
            Patch(51.5f, 12f, 1.3f, 1.2f);
        }

        static void RarePocket()
        {
            Patch(51f, 51f, 2.2f, 1.8f);
            Patch(54f, 47.5f, 1.2f, 1.2f);
            Prop(48, 53, KCloud);
            Prop(53, 54, KKitePost);
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
            string zone = ZoneId(x, y);
            char key = zone switch
            {
                "fire" => i % 2 == 0 ? KVent : KLeaves,
                "village" => i % 3 == 0 ? KHay : i % 3 == 1 ? KStones : IslandMap.Empty,
                _ => (i % 3) switch { 0 => KStones, 1 => KHay, _ => IslandMap.Empty },
            };
            if (key == IslandMap.Empty) Ground(x, y, IslandMap.Rock);
            else Prop(x, y, key);
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

        /// <summary>Tiles the story needs free (the kite spots and the tower door front).</summary>
        static bool IsStory(int x, int y)
        {
            var t = new Vector2Int(x, y);
            foreach (var k in KiteXY)
                if ((t - k).sqrMagnitude <= 2) return true;
            return y >= TowerXY.y - 2 && y <= TowerXY.y - 1 && x >= TowerXY.x - 1 && x <= TowerXY.x + 2;
        }

        static bool IsPath(int x, int y) => In(x, y) && _g[x, y] == IslandMap.Path;

        /// <summary>A puzzle gate tile (x, y): the drawbridge over the ditch or a cloud bank puff (open once solved).</summary>
        public static bool IsGate(int x, int y)
        {
            if (x >= CrossingXY.x && x <= CrossingXY.x + 1 && y >= DitchY0 && y <= DitchY1) return true;
            foreach (var c in CloudXY)
                if (c.x == x && c.y == y) return true;
            return false;
        }

        static string ZoneId(int x, int y)
        {
            foreach (var z in Zones) if (z.key == _z[x, y]) return z.id;
            return null;
        }

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
                    if (y >= 45 && y <= 48 && !(x > 26 && x < 44)) continue; // never opens the ridge band
                    if (y >= DitchY0 - 0 && y <= DitchY1) continue;
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
                    if (!In(t.x, t.y) || _p[t.x, t.y] != IslandMap.Empty || _covered.Contains(t) || _g[t.x, t.y] == IslandMap.Water ||
                        _g[t.x, t.y] == IslandMap.Path && key != KGate && key != KShrine && key != KBridge && key != KBank)
                    {
                        Debug.LogWarning($"[Galewick] {recipe.name} at x{x} y{y}: tile x{t.x} y{t.y} is taken; skipped.");
                        return;
                    }
                    tiles.Add(t);
                }
            foreach (var t in tiles)
            {
                _covered.Add(t);
                if (key == KBridge) continue; // the crossing stays path under the drawbridge
                if (f != Vector2Int.one || _g[t.x, t.y] == IslandMap.Tree || _g[t.x, t.y] == IslandMap.Rock) _g[t.x, t.y] = IslandMap.Grass;
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

        /// <summary>Walkable tiles the explorer could never reach become hills; tall grass crumbs under 4 tiles become grass.</summary>
        static void SealCutOff()
        {
            bool Walkable(int x, int y) => In(x, y) && IslandMap.IsWalkable(_g[x, y]) && (!Blocks(x, y) || IsGate(x, y));
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

        /// <summary>Perf: hills behind the first row along walkable ground keep about 60%; the rest turn to rocks.</summary>
        static void ThinBackHills()
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
                            near = In(nx, ny) && (IslandMap.IsWalkable(_g[nx, ny]) || _g[nx, ny] == IslandMap.Water);
                        }
                    if (near || _rnd.Next() >= 0.4f) continue;
                    _g[x, y] = IslandMap.Rock;
                    thinned++;
                }
            _log?.Append($"\n  back hills thinned: {thinned} to rocks");
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
            Vector2 dir = b - a;
            for (int guard = 0; p != b && guard < 200; guard++)
            {
                var d = b - p;
                var sx = new Vector2Int(p.x + System.Math.Sign(d.x), p.y);
                var sy = new Vector2Int(p.x, p.y + System.Math.Sign(d.y));
                float Off(Vector2Int q) => Mathf.Abs((q.x - a.x) * dir.y - (q.y - a.y) * dir.x);
                p = d.x == 0 ? sy : d.y == 0 ? sx : Off(sx) <= Off(sy) ? sx : sy;
                list.Add(p);
            }
            return list;
        }

        public static Vector2Int VillageSpot => Tile(PlazaXY);
        public static Vector2Int KiteSpot => Tile(36, 34);

        // ================= checks =================

        /// <summary>
        /// The automated layout checks (also an EditMode test): the trail connects the arrival, the shrine, the village, the
        /// crossing, the kite field, the ridge path and the tower door; every pocket is reachable; nothing walkable is cut off;
        /// the ditch and the ridge band are only crossed at the crossing and the ridge path (so 10b's drawbridge and cloud bank
        /// gate them); tall grass share and patch sizes; the edge ring is hills. Returns the problems (empty = fine).
        /// <paramref name="blocked"/> = extra tiles (x, y) that count as blocked (10b: raised drawbridge, the cloud bank).
        /// </summary>
        public static List<string> Check(WorldMap map, StringBuilder report, ICollection<Vector2Int> blocked = null)
        {
            var problems = new List<string>();
            if (map.Columns != Size || map.Rows != Size) problems.Add($"map is {map.Columns}x{map.Rows}, not {Size}x{Size}");

            // closed = gate tiles (x, y) that stay shut; every other puzzle gate counts as open (solved).
            bool Walk(Vector2Int t, ICollection<Vector2Int> closed)
            {
                var xy = new Vector2Int(t.x, Size - 1 - t.y);
                if (closed != null && closed.Contains(xy)) return false;
                return map.CanWalk(t) || IsGate(xy.x, xy.y) && IslandMap.IsWalkable(map.At(t));
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
            Need("big mill front", MillXY + new Vector2Int(1, -2));
            Need("crossing", CrossingXY);
            Need("kite field", Vector2Int.RoundToInt(KiteCenter) + Vector2Int.down * Mathf.RoundToInt(KiteRy));
            foreach (var k in KiteXY) Need("kite spot", k);
            foreach (var c in CloudXY) Need("ridge path", c);
            Need("tower door front", TowerXY + new Vector2Int(0, -2));
            Need("return gate front", GateXY + Vector2Int.down);
            Need("tower return gate front", TowerGateXY + Vector2Int.down);
            foreach (var z in new[] { "arrival", "village", "kites", "ridge", "rare", "fire", "tower" })
                if (!reach.Any(t => map.ZoneAt(t) == z && map.CanWalk(t))) problems.Add($"pocket '{z}' has no reachable tile");

            // The crossing and the ridge path are the only ways north (10b gates them).
            var noCrossing = new HashSet<Vector2Int>();
            for (int x = CrossingXY.x; x <= CrossingXY.x + 1; x++)
                for (int y = DitchY0; y <= DitchY1; y++) noCrossing.Add(new Vector2Int(x, y));
            if (Flood(noCrossing).Contains(Tile(KiteXY[0]))) problems.Add("the kite field is reachable without the crossing (the ditch leaks)");
            var noRidge = new HashSet<Vector2Int>(CloudXY);
            if (Flood(noRidge).Contains(Tile(TowerXY + new Vector2Int(0, -2)))) problems.Add("the tower is reachable without the ridge path (the hill band leaks)");
            if (blocked != null && Flood(blocked).Contains(Tile(TowerXY + new Vector2Int(0, -2)))) problems.Add("the tower is reachable past the blocked tiles");

            // Step 10b: with the props in place (bridge raised, clouds parked) the kite field and the tower are shut.
            if (map.CanWalk(Tile(CrossingXY)) == false && map.CanWalk(Tile(CloudXY[0])) == false)
            {
                var shut = new HashSet<Vector2Int>();
                for (int x = 0; x < Size; x++)
                    for (int y = 0; y < Size; y++)
                        if (IsGate(x, y)) shut.Add(new Vector2Int(x, y));
                if (Flood(shut).Contains(Tile(KiteXY[0]))) problems.Add("the kite field is reachable with the drawbridge up");
            }

            int walkable = 0, tall = 0, cut = 0;
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                {
                    var t = new Vector2Int(c, r);
                    if (!Walk(t, null)) continue;
                    walkable++;
                    if (map.At(t) == IslandMap.TallGrass) tall++;
                    if (!reach.Contains(t)) cut++;
                }
            if (cut > 0) problems.Add($"{cut} walkable tile(s) cut off from everything");
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

            report?.Append($"\n  walkable {walkable}, reachable {reach.Count}, tall grass {share:P0} in {patches.Count} patches " +
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
