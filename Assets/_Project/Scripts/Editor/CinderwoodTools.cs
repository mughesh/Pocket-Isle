using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using static Diorama.EditorTools.StarterKit;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 9a: the real Cinderwood map, 60 x 60 (every realm is 60 x 60, decided Oct 26). One winding 2-tile trail
    /// through five beats (arrival gate, forest, kiln village + shrine, hot springs, fallen log, cliff forge temple)
    /// with side pockets (ember field, lantern glade, berry grove, kiln ruins). Drawn from code with a fixed seed: zone
    /// shapes and story beats are placed by hand here, trees / grass / filler by rules. Re-running rewrites the map.
    /// Coordinates in this file are (x west to east, y south to north; south = the arrival, nearest the player);
    /// <see cref="Tile"/> turns them into the map's (column, row) (row 0 = the back edge).
    /// </summary>
    public static class CinderwoodTools
    {
        public const string MapPath = "Assets/_Project/Data/WorldMap_Cinderwood.asset";
        public const string ArchivePath = "Assets/_Project/Data/Archive/WorldMap_Cinderwood_Test.asset";
        const string TableRoot = "Assets/_Project/Data/Battle/Cinderwood";
        public const int Size = 60;
        const int Seed = 909;

        // ---- story tiles (x, y) ----
        public static readonly Vector2Int GateXY = new Vector2Int(7, 6);      // return gate, 2 x 1 (front y 5)
        public static readonly Vector2Int ArriveXY = new Vector2Int(10, 5);   // you arrive here facing north
        public static readonly Vector2Int ShrineXY = new Vector2Int(24, 33);  // 2 x 2, front y 31
        public static readonly Vector2Int LogXY = new Vector2Int(20, 45);     // 2 x 1 across the trail
        public static readonly Vector2Int DoorXY = new Vector2Int(19, 56);    // forge door, 3 x 1
        // Step 9b: the bellows beside the door and its pipe along the cliff foot. Step 9f (Mugesh, Oct 26: the third of
        // three braziers in a row was out of the window): the braziers sit round the door, lit nearest first: east of it,
        // over the arch (raised, no map prop: TopBrazier) and west of it. The pipe prop (on the door's east tile) sits
        // PropKit.PipeSpan tiles west of the bellows.
        public static readonly Vector2Int BellowsXY = new Vector2Int(23, 55);
        public static readonly Vector2Int PipeXY = new Vector2Int(23 - PropKit.PipeSpan, 55);
        public static readonly Vector2Int[] BrazierXY = { new Vector2Int(22, 55), new Vector2Int(20, 55), new Vector2Int(18, 55) };
        public const int TopBrazier = 1;
        /// <summary>Step 9c: the temple's return gate (2 x 1, front y 51), hidden until the Keeper is calmed (KeeperHall).</summary>
        public static readonly Vector2Int TempleGateXY = new Vector2Int(24, 52);
        static readonly Vector2Int FirstGrassXY = new Vector2Int(15, 7);

        // Step 10f: the smoky hollow on the trail from the village to the springs. The eight path tiles under the smoke (in
        // order along the trail) and the six stone lanterns lining it (in order: the chain runs this way). Everything else
        // walkable beside it turns to rock, so the smoke is the only way through.
        public static readonly Vector2Int[] HollowXY =
        {
            new Vector2Int(28, 33), new Vector2Int(29, 33), new Vector2Int(29, 34), new Vector2Int(30, 34),
            new Vector2Int(29, 35), new Vector2Int(30, 35), new Vector2Int(30, 36), new Vector2Int(31, 36),
        };
        public static readonly Vector2Int[] HollowLanternXY =
        {
            new Vector2Int(30, 33), new Vector2Int(28, 34), new Vector2Int(31, 34), new Vector2Int(28, 35), new Vector2Int(31, 35), new Vector2Int(29, 36),
        };
        /// <summary>
        /// Nov 3 (Mugesh: the step 14c arrival lanterns were bland): a thorny vine across the 2-tile trail 3 rows past the
        /// arrival, and trees either side of it on that row (the signpost at x9 already blocks), so it is the only way on.
        /// The arrival plaza's ember vent (14, 4) burns it (VineBurnPiece). Placed after every random draw: nothing else moves.
        /// </summary>
        public static readonly Vector2Int[] VineXY = { new Vector2Int(11, 8), new Vector2Int(12, 8) };
        public static readonly Vector2Int[] VineTreeXY = { new Vector2Int(10, 8), new Vector2Int(13, 8), new Vector2Int(14, 8), new Vector2Int(15, 8), new Vector2Int(16, 8) };
        public static Vector2Int[] VineTiles => System.Array.ConvertAll(VineXY, Tile);
        /// <summary>The arrival plaza's ember vent (Arrival()): the vine's ember.</summary>
        public static readonly Vector2Int ArriveVentXY = new Vector2Int(14, 4);
        /// <summary>The trail tile just before the vine (landmark).</summary>
        public static readonly Vector2Int VineMarkXY = new Vector2Int(11, 7);

        /// <summary>The trail tile just before the smoke on the village side (landmark, dev panel Go to).</summary>
        public static readonly Vector2Int HollowMarkXY = new Vector2Int(28, 32);
        // Step 10f: the forge's firebox (in the cliff line, just east of the bellows) and the ember vent beside it.
        public static readonly Vector2Int FireboxXY = new Vector2Int(24, 56);
        public static readonly Vector2Int ForgeVentXY = new Vector2Int(26, 54);
        /// <summary>The temple plaza's middle (x, y): where the roaring forge's warmth settles.</summary>
        public static readonly Vector2 TempleXY = new Vector2(20f, 53f);
        public static Vector2Int[] HollowTiles => System.Array.ConvertAll(HollowXY, Tile);
        public static Vector2Int[] HollowLanterns => System.Array.ConvertAll(HollowLanternXY, Tile);
        static readonly Vector2Int PlazaXY = new Vector2Int(15, 33);

        /// <summary>(x, y) to the map's (column, row).</summary>
        public static Vector2Int Tile(Vector2Int xy) => new Vector2Int(xy.x, Size - 1 - xy.y);
        public static Vector2Int Tile(int x, int y) => new Vector2Int(x, Size - 1 - y);
        public static Vector2Int ReturnGate => Tile(GateXY);
        public static Vector2Int Arrive => Tile(ArriveXY);
        /// <summary>Facing north (toward the back edge: row - 1).</summary>
        public static readonly Vector2Int ArriveFacing = new Vector2Int(0, -1);
        public static Vector2Int LogTile => Tile(LogXY);

        // Main trail centre line (2 tiles wide around it) and the 1-wide side paths.
        static readonly Vector2[] Trail =
        {
            new Vector2(11.5f, 3.5f), new Vector2(11.5f, 9.5f), new Vector2(8.5f, 14.5f), new Vector2(9.5f, 19.5f),
            new Vector2(14.5f, 22.5f), new Vector2(13.5f, 27.5f), new Vector2(15.5f, 32.5f), new Vector2(20.5f, 29.5f),
            new Vector2(27.5f, 30.5f), new Vector2(30.5f, 36.5f), new Vector2(34.5f, 39.5f), new Vector2(38.5f, 41.5f),
            new Vector2(35.5f, 44.5f), new Vector2(29.5f, 45.5f), new Vector2(23.5f, 42.5f), new Vector2(20.5f, 44.5f),
            new Vector2(20.5f, 47.5f), new Vector2(17.5f, 50.5f), new Vector2(20.5f, 54.5f),
        };
        /// <summary>
        /// Step 14i (Mugesh: the trail zig-zagged one tile at a time, e.g. by the hot springs): the trail as drawn: straight
        /// legs with right-angle turns, none shorter than 3 tiles (<see cref="StraightPaths"/>). Segment 8 (the village to
        /// the springs through the smoky hollow) keeps its authored steps: the smoke tiles are a hand moment.
        /// </summary>
        static readonly Vector2[] Line = StraightPaths.Trail(Trail, keep: new[] { 8 });
        /// <summary>Step 14i: the longest straight leg the check allows (straight legs replace the winding trail).</summary>
        const int MaxLeg = 20;
        static readonly Vector2Int[][] SidePaths =
        {
            new[] { new Vector2Int(13, 9), new Vector2Int(17, 10), new Vector2Int(21, 8), new Vector2Int(27, 9) },     // ember field
            new[] { new Vector2Int(36, 13), new Vector2Int(36, 15), new Vector2Int(37, 17) },                          // lantern glade
            new[] { new Vector2Int(43, 40), new Vector2Int(45, 38), new Vector2Int(48, 36), new Vector2Int(48, 35) },  // berry grove
            new[] { new Vector2Int(42, 45), new Vector2Int(44, 47), new Vector2Int(45, 50), new Vector2Int(48, 51) },  // kiln ruins (hidden)
        };

        struct Zone
        {
            public char key;
            public string id;
            public RectInt rect; // x, y, inclusive max via xMax / yMax - 1
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
            new Zone('f', "forest", 4, 22, 8, 24),
            new Zone('a', "arrival", 6, 14, 2, 8),
            new Zone('e', "field", 26, 50, 2, 14),
            new Zone('l', "glade", 30, 44, 16, 26),
            new Zone('v', "village", 6, 26, 26, 40),
            new Zone('b', "grove", 46, 56, 24, 36),
            new Zone('s', "springs", 30, 46, 34, 46),
            new Zone('g', "log", 18, 22, 44, 46),
            new Zone('t', "temple", 10, 30, 48, 58),
            new Zone('r', "ruins", 44, 56, 46, 58),
        };

        // ---- prop keys ----
        const char KGate = 'Z', KShrine = 'Y', KHut = 'h', KKiln = 'k', KPots = 'p', KWood = 'w', KLanternPost = 'l',
            KStoneLantern = 'n', KBush = 'b', KLog = 'o', KSign = 's', KLeaves = 'v', KCliff = 'c', KCliffB = 'C',
            KDoor = 'd', KBrazier = 'u', KRuin = 'r', KShards = 'x', KVent = 'e', KBellows = 'B', KPipe = 'i',
            KFence = '-', KFenceTurned = '|', KCornerWS = '1', KCornerNW = '2', KCornerEN = '3', KCornerSE = '4', KSmoke = 'q', KFirebox = 'm', KVine = 'V';

        // ---- generation state ----
        static char[,] _g, _p, _z; // [x, y]
        static HashSet<Vector2Int> _covered;
        static Dictionary<string, PropRecipe> _recipes;
        static SeededRandom _rnd;
        static StringBuilder _log;

        // ================= write =================

        /// <summary>Archives the old map once, writes the new one (same asset, same GUID), its tables and legend.</summary>
        public static WorldMap Write(StringBuilder log)
        {
            _log = log;
            var map = AssetDatabase.LoadAssetAtPath<WorldMap>(MapPath);
            if (map == null)
            {
                Debug.LogError($"[Cinderwood] No {MapPath} (run Set Up MR_Main (8 hub) first).");
                return null;
            }
            if (AssetDatabase.LoadAssetAtPath<WorldMap>(ArchivePath) == null && map.Columns != Size)
            {
                DioramaSceneSetup.EnsureFolder("Assets/_Project/Data/Archive");
                if (AssetDatabase.CopyAsset(MapPath, ArchivePath)) log.Append($"\n  old test map archived to {ArchivePath}");
                else Debug.LogError($"[Cinderwood] Couldn't archive the old map to {ArchivePath}.");
            }

            _recipes = new Dictionary<string, PropRecipe>();
            foreach (var name in new[]
                     {
                         "ReturnGate", HearthShrines.RecipeName, "ClayHut", "Kiln", "PotCluster", "Woodpile", "LanternPost",
                         "StoneLantern", "BerryBush", "FallenLog", "Signpost", "LeafPile", "CliffWall", "ForgeDoor", "Brazier",
                         "RuinedKiln", "PotShards", "EmberVent", "Fence", "FenceCorner", "Bellows", "ForgePipe", "SmokeCloud", "Firebox",
                         PropKit.VineName,
                     })
            {
                var r = PropKit.Load(name);
                if (r == null) Debug.LogError($"[Cinderwood] Prop {name} missing (write the prop kits first).");
                _recipes[name] = r;
            }

            Generate();

            map.seed = Seed;
            map.tileSize = 0.05f;
            map.viewZoom = 1f;
            map.startTile = Arrive;
            map.theme = PropKit.Theme("Cinderwood");
            map.realm = "cinderwood";
            map.arrivalLine = "cw.arrive"; // step 14c (Nov 3: the vine's lesson follows; was realm.cinderwood.first)
            map.propLegend = Legend();
            map.SetLayout(Text(_g));
            map.SetProps(Text(_p));
            map.SetZones(Text(_z));
            // Step 9d: the rest of the fire roster joins (Coalbeetle, Mapadillo, Ashmole, Cindercap; Embercrane a ~5% rare).
            map.grassTable = Table("Encounters_Cinderwood", 3, 5, ("Toastling", 28f, false), ("Waxle", 28f, false), ("Kettlewing", 24f, false), ("Ashmole", 15f, false), ("Glazegolem", 5f, true));
            map.zoneLegend = new List<MapZone>();
            foreach (var z in Zones)
            {
                var table = z.id switch
                {
                    "forest" => Table("Encounters_Forest", 3, 5, ("Toastling", 45f, false), ("Waxle", 22f, false), ("Kettlewing", 8f, false), ("Coalbeetle", 15f, false), ("Mapadillo", 10f, false)),
                    "field" => Table("Encounters_Field", 4, 6, ("Toastling", 30f, false), ("Kettlewing", 20f, false), ("Waxle", 12f, false), ("Coalbeetle", 17f, false), ("Mapadillo", 16f, false), ("Embercrane", 5f, true)),
                    "glade" => Table("Encounters_Glade", 5, 7, ("Waxle", 50f, false), ("Toastling", 20f, false), ("Cindercap", 30f, false)),
                    // Step 9f: Steambara, a Water visitor, soaks at the springs (~35%) and wanders into the grove (~10%).
                    "grove" => Table("Encounters_Grove", 5, 7, ("Kettlewing", 50f, false), ("Toastling", 30f, false), ("Mapadillo", 10f, false), ("Steambara", 10f, false)),
                    "springs" => Table("Encounters_Springs", 6, 8, ("Kettlewing", 40f, false), ("Steambara", 35f, false), ("Waxle", 20f, false), ("Embercrane", 5f, true)),
                    "ruins" => Table("Encounters_Ruins", 7, 9, ("Glazegolem", 25f, true), ("Toastling", 25f, false), ("Waxle", 25f, false), ("Ashmole", 25f, false)),
                    _ => null,
                };
                map.zoneLegend.Add(new MapZone { key = z.key.ToString(), id = z.id, grassTable = table });
            }
            map.landmarks = new List<Landmark>
            {
                new Landmark { name = "start", tile = Arrive },
                new Landmark { name = "firstGrass", tile = Tile(FirstGrassXY) },
                new Landmark { name = "plaza", tile = Tile(PlazaXY), line = "cinder.village.first", radius = 4 },
                new Landmark { name = "shrine", tile = Tile(ShrineXY) },
                new Landmark { name = "springs", tile = Tile(40, 38), line = "cinder.springs.first", radius = 5 },
                new Landmark { name = "log", tile = LogTile, line = "cinder.log.first", radius = 3 },
                new Landmark { name = "temple", tile = Tile(DoorXY + Vector2Int.right), line = "cinder.temple.first", radius = 4 },
                new Landmark { name = "ruins", tile = Tile(50, 52), line = "cinder.ruins.first", radius = 5 },
                new Landmark { name = "hollow", tile = Tile(HollowMarkXY) }, // step 10f (its lesson line says the rest)
                new Landmark { name = "vine", tile = Tile(VineMarkXY) }, // Nov 3: the arrival's vine (was the 14c lanterns)
                new Landmark { name = "field", tile = Tile(38, 8) },
                new Landmark { name = "glade", tile = Tile(37, 21) },
                new Landmark { name = "grove", tile = Tile(51, 30) },
            };
            EditorUtility.SetDirty(map);
            AssetDatabase.SaveAssets();
            log.Append($"\n  {MapPath}: {Size}x{Size} Cinderwood (seed {Seed}), arrive c{Arrive.x} r{Arrive.y} facing north, " +
                       $"return gate c{ReturnGate.x} r{ReturnGate.y}, {map.zoneLegend.Count} zones, {map.landmarks.Count} landmarks");
            return map;
        }

        static List<PropLegend> Legend()
        {
            var legend = new List<PropLegend>();
            void Key(char k, string recipe, float yaw = 0f, bool turned = false)
            {
                if (_recipes.TryGetValue(recipe, out var r) && r != null)
                    legend.Add(new PropLegend { key = k.ToString(), recipe = r, yaw = yaw, turned = turned });
            }
            Key(KGate, "ReturnGate");
            Key(KShrine, HearthShrines.RecipeName);
            Key(KHut, "ClayHut");
            Key(KKiln, "Kiln");
            Key(KPots, "PotCluster");
            Key(KWood, "Woodpile");
            Key(KLanternPost, "LanternPost");
            Key(KStoneLantern, "StoneLantern");
            Key(KBush, "BerryBush");
            Key(KLog, "FallenLog");
            Key(KSign, "Signpost");
            Key(KLeaves, "LeafPile");
            Key(KCliff, "CliffWall");
            Key(KCliffB, "CliffWall", 180f);
            Key(KDoor, "ForgeDoor");
            Key(KBrazier, "Brazier");
            Key(KBellows, "Bellows");
            Key(KPipe, "ForgePipe");
            Key(KRuin, "RuinedKiln");
            Key(KShards, "PotShards");
            Key(KVent, "EmberVent");
            Key(KSmoke, "SmokeCloud");
            Key(KVine, PropKit.VineName);
            Key(KFirebox, "Firebox");
            Key(KFence, "Fence");
            Key(KFenceTurned, "Fence", 0f, true);
            // FenceCorner's rails run to local +X (west on the map) and +Z (south); yaw turns them clockwise from above.
            Key(KCornerWS, "FenceCorner", 0f);
            Key(KCornerNW, "FenceCorner", 90f);
            Key(KCornerEN, "FenceCorner", 180f);
            Key(KCornerSE, "FenceCorner", 270f);
            return legend;
        }

        /// <summary>Step 9f: Cinderwood's wild Islings are 2 levels up (the trail now levels you to ~8 by the temple).</summary>
        public const int WildLevelBonus = 2;

        static EncounterTable Table(string name, int minLevel, int maxLevel, params (string species, float weight, bool rare)[] entries)
        {
            minLevel += WildLevelBonus;
            maxLevel += WildLevelBonus;
            DioramaSceneSetup.EnsureFolder(TableRoot);
            var table = Asset<EncounterTable>($"{TableRoot}/{name}.asset");
            table.habitat = Habitat.Grass;
            var list = new List<EncounterTable.Entry>();
            foreach (var (id, weight, rare) in entries)
            {
                var species = AssetDatabase.LoadAssetAtPath<IslingSpecies>($"Assets/_Project/Data/Creatures/Species/{id}.asset");
                if (species == null)
                {
                    Debug.LogError($"[Cinderwood] Species {id} not found for {name}.");
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

            // Trees everywhere outside the open places, denser toward the map edge; lone trees inside some of them.
            for (int x = 1; x < Size - 1; x++)
                for (int y = 1; y < Size - 1; y++)
                {
                    float open = Openness(x, y);
                    int edge = Mathf.Min(Mathf.Min(x, y), Mathf.Min(Size - 1 - x, Size - 1 - y));
                    float tree = open > 0f ? open : edge <= 2 ? 0.92f : 0.72f;
                    // The forest is a wall: trees, rocks between them (open places only get the odd lone tree).
                    float roll = _rnd.Next();
                    if (roll < tree) _g[x, y] = IslandMap.Tree;
                    else if (open == 0f) _g[x, y] = IslandMap.Rock;
                }

            // The main trail (2 wide) with a clear grass shoulder, then the side paths (1 wide, trees right beside).
            for (int x = 1; x < Size - 1; x++)
                for (int y = 1; y < Size - 1; y++)
                {
                    float d = DistToPolyline(new Vector2(x, y), Line);
                    if (d < 0.95f) _g[x, y] = IslandMap.Path;
                    else if (d < 2.0f && (_g[x, y] == IslandMap.Tree || _g[x, y] == IslandMap.Rock)) _g[x, y] = IslandMap.Grass;
                }
            // Step 14i: side paths in straight legs too.
            foreach (var path in SidePaths)
                foreach (var t in StraightPaths.Side(path))
                    _g[t.x, t.y] = IslandMap.Path;

            Arrival();
            Field();
            Glade();
            Village();
            Grove();
            Springs();
            LogGap();
            Temple();
            Ruins();
            ForestBits();
            Filler();
            Hollow();
            CleanUnderProps();
            SealCutOff();
            ThinDeepForest();
            ArrivalVine();
        }

        /// <summary>Nov 3: last of all (no random draws after it), so adding it moves nothing else.</summary>
        static void ArrivalVine()
        {
            foreach (var t in VineTreeXY) Ground(t.x, t.y, IslandMap.Tree);
            foreach (var v in VineXY) Prop(v, KVine);
        }

        /// <summary>
        /// Perf (step 9a): forest behind the first row of trees along walkable ground keeps about 55% of its trees; the rest turn to
        /// rocks. They sit behind the trail's tree walls, so the forest still reads as dense; a window holds ~25% fewer trees.
        /// </summary>
        static void ThinDeepForest()
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
                            near = In(nx, ny) && IslandMap.IsWalkable(_g[nx, ny]);
                        }
                    if (near || _rnd.Next() >= 0.45f) continue;
                    _g[x, y] = IslandMap.Rock;
                    thinned++;
                }
            _log?.Append($"\n  deep forest thinned: {thinned} trees to rocks");
        }

        /// <summary>Tree chance inside an open place (0 = not open: forest rules apply).</summary>
        static float Openness(int x, int y)
        {
            bool InEll(float cx, float cy, float rx, float ry) => Sq((x - cx) / rx) + Sq((y - cy) / ry) <= 1f + (_rnd.Next() - 0.5f) * 0.25f;
            bool Rect(int x0, int x1, int y0, int y1) => x >= x0 && x <= x1 && y >= y0 && y <= y1;
            if (Rect(7, 14, 2, 7)) return 0.001f;                                       // arrival clearing
            if (InEll(38f, 8f, 12.5f, 6f) || Rect(27, 49, 3, 13)) return 0.025f;       // ember field: a few lone trees
            if (InEll(37f, 21f, 5.6f, 5.6f)) return 0.001f;                            // glade clearing
            if (Rect(7, 25, 26, 39)) return 0.001f;                                    // village
            if (Rect(47, 55, 25, 35)) return 0.001f;                                   // berry grove (inside the fence)
            if (InEll(38f, 40f, 8.5f, 6.2f)) return 0.02f;                             // hot springs
            if (Rect(12, 28, 49, 55)) return 0.001f;                                   // temple plaza
            if (InEll(50f, 52f, 5.5f, 5f)) return 0.01f;                               // kiln ruins
            return 0f;
        }

        static float Sq(float v) => v * v;

        // ---- beats ----

        static void Arrival()
        {
            Fill(7, 13, 3, 5, IslandMap.Path);
            Prop(GateXY, KGate);
            Prop(9, 8, KSign);
            Prop(13, 7, KLeaves);
            Patch(FirstGrassXY.x, FirstGrassXY.y, 1.8f, 1.5f);
            Prop(14, 4, KVent);
        }

        static void Field()
        {
            foreach (var (x, y, rx, ry) in new[] { (30.5f, 5f, 2.2f, 1.7f), (36f, 10.5f, 2.1f, 1.3f), (44f, 5f, 2.1f, 1.5f), (47f, 11f, 1.8f, 1.5f), (38.5f, 4f, 1.3f, 1.1f) })
                Patch(x, y, rx, ry);
            foreach (var t in new[] { (33, 8), (34, 8), (33, 9), (44, 9), (45, 9), (40, 12), (28, 11), (48, 4) }) Ground(t.Item1, t.Item2, IslandMap.Rock);
            foreach (var (x, y) in new[] { (30, 10), (46, 7), (38, 2), (42, 12) }) Prop(x, y, KVent);
            Prop(26, 7, KSign);
        }

        static void Glade()
        {
            for (int i = 0; i < 8; i++)
            {
                float a = (i + 0.5f) * Mathf.PI / 4f;
                Prop(Mathf.RoundToInt(37f + 4.2f * Mathf.Cos(a)), Mathf.RoundToInt(21f + 4.2f * Mathf.Sin(a)), KStoneLantern);
            }
            Patch(35.5f, 23f, 1.5f, 1.2f);
            Patch(40f, 19f, 1.4f, 1.3f);
            Prop(37, 21, KLeaves);
        }

        static void Village()
        {
            Disc(15.5f, 32.5f, 2.4f, IslandMap.Path);
            foreach (var (x, y) in new[] { (9, 37), (13, 38), (18, 37), (8, 32), (17, 27) }) Prop(x, y, KHut);
            foreach (var (x, y) in new[] { (11, 34), (21, 34), (23, 37) }) Prop(x, y, KKiln);
            foreach (var (x, y) in new[] { (10, 34), (20, 35), (11, 29), (22, 36) }) Prop(x, y, KPots);
            foreach (var (x, y) in new[] { (8, 35), (16, 38), (21, 38) }) Prop(x, y, KWood);
            foreach (var (x, y) in new[] { (12, 31), (19, 34), (13, 35), (20, 32) }) Prop(x, y, KLanternPost);
            Prop(ShrineXY, KShrine);
            Prop(16, 25, KSign);
            Prop(27, 33, KSign);
            // An L of fence along the village's south-west edge, and a short run south-east.
            for (int x = 8; x <= 11; x++) Prop(x, 26, KFence);
            Prop(7, 26, KCornerEN);
            for (int y = 27; y <= 28; y++) Prop(7, y, KFenceTurned);
            for (int x = 20; x <= 23; x++) Prop(x, 26, KFence);
            Patch(23f, 39f, 1.4f, 1f);
        }

        static void Grove()
        {
            // Fence round x 47..55, y 25..35, open at (48, 35) and (49, 35) where the side path comes in.
            Prop(47, 35, KCornerSE);
            Prop(55, 35, KCornerWS);
            Prop(47, 25, KCornerEN);
            Prop(55, 25, KCornerNW);
            for (int x = 48; x <= 54; x++)
            {
                if (x > 49) Prop(x, 35, KFence);
                Prop(x, 25, KFence);
            }
            for (int y = 26; y <= 34; y++)
            {
                Prop(47, y, KFenceTurned);
                Prop(55, y, KFenceTurned);
            }
            Fill(48, 49, 34, 34, IslandMap.Grass);
            foreach (int y in new[] { 27, 30, 33 })
                for (int x = 49; x <= 53; x++) Prop(x, y, KBush);
            Fill(49, 53, 28, 29, IslandMap.TallGrass);
            Fill(50, 52, 31, 32, IslandMap.TallGrass);
        }

        static readonly (float x, float y, float r)[] Pools = { (33f, 42f, 1.5f), (40f, 38f, 1.8f), (43f, 42f, 1.4f), (36.5f, 36f, 1.3f) };

        static void Springs()
        {
            foreach (var (px, py, r) in Pools)
                for (int x = Mathf.FloorToInt(px - r - 2); x <= Mathf.CeilToInt(px + r + 2); x++)
                    for (int y = Mathf.FloorToInt(py - r - 2); y <= Mathf.CeilToInt(py + r + 2); y++)
                    {
                        if (!In(x, y) || _g[x, y] == IslandMap.Path) continue;
                        float d = Mathf.Sqrt(Sq(x - px) + Sq(y - py));
                        if (d <= r) _g[x, y] = IslandMap.HotSpring;
                        else if (d <= r + 1.2f && _g[x, y] == IslandMap.Grass && _rnd.Next() < 0.7f) _g[x, y] = IslandMap.TallGrass;
                    }
            foreach (var (x, y) in new[] { (31, 38), (45, 40), (38, 45), (41, 35), (35, 46) }) Ground(x, y, IslandMap.Rock);
            Prop(44, 44, KLeaves);
        }

        static void LogGap()
        {
            // A band of rock right behind the gap: past the log is only reachable through it (the temple waits for 9b).
            for (int x = 8; x <= 32; x++)
                for (int y = 47; y <= 48; y++)
                    if (_g[x, y] != IslandMap.Path) Ground(x, y, IslandMap.Rock);
            foreach (var (x, y) in new[] { (18, 44), (19, 44), (18, 45), (19, 45), (22, 45), (23, 45), (18, 46), (19, 46), (22, 46), (23, 46) })
                Ground(x, y, IslandMap.Rock);
            Prop(LogXY, KLog);
        }

        static void Temple()
        {
            Disc(20f, 53f, 2.8f, IslandMap.Path);
            for (int x = 10; x <= 30; x++)
            {
                for (int y = 57; y <= 58; y++) Ground(x, y, IslandMap.Rock);
                if (x >= DoorXY.x && x <= DoorXY.x + 2) continue;
                Ground(x, 56, IslandMap.Grass);
                Prop(x, 56, x == FireboxXY.x ? KFirebox : (x * 7 + 3) % 5 < 2 ? KCliffB : KCliff);
            }
            Ground(DoorXY.x, DoorXY.y, IslandMap.Grass);
            Prop(DoorXY, KDoor);
            for (int i = 0; i < BrazierXY.Length; i++)
                if (i != TopBrazier) Prop(BrazierXY[i], KBrazier);
            Prop(TempleGateXY, KGate);
            Prop(BellowsXY, KBellows);
            Prop(PipeXY, KPipe);
            foreach (var (x, y) in new[] { (14, 52), (26, 50), (12, 49) }) Prop(x, y, KVent);
            Prop(ForgeVentXY, KVent); // step 10f: the ember's vent
        }

        static void Ruins()
        {
            foreach (var (x, y) in new[] { (47, 53), (51, 56), (54, 50), (49, 48) }) Prop(x, y, KRuin);
            foreach (var (x, y) in new[] { (48, 52), (50, 55), (53, 52), (46, 50), (52, 49), (55, 54) }) Prop(x, y, KShards);
            foreach (var (x, y) in new[] { (50, 53), (47, 56) }) Prop(x, y, KVent);
            Patch(51f, 51f, 2f, 1.5f);
            Patch(53f, 55.5f, 1.4f, 1.2f);
        }

        /// <summary>
        /// Step 10f: the smoky hollow. Smoke on the eight path tiles, stone lanterns on six of the tiles beside them, every
        /// other walkable tile next to the smoke turned to rock (blocking props stay), so the smoke is the only way through.
        /// Runs after the filler, so the rest of the map keeps its seeded layout.
        /// </summary>
        static void Hollow()
        {
            var hollow = new HashSet<Vector2Int>(HollowXY);
            var lanterns = new HashSet<Vector2Int>(HollowLanternXY);
            int walls = 0;
            foreach (var h in HollowXY)
            {
                if (_g[h.x, h.y] != IslandMap.Path) Debug.LogError($"[Cinderwood] Hollow tile x{h.x} y{h.y} isn't on the trail ('{_g[h.x, h.y]}').");
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        var n = new Vector2Int(h.x + dx, h.y + dy);
                        if (!In(n.x, n.y) || hollow.Contains(n) || lanterns.Contains(n) || _g[n.x, n.y] == IslandMap.Path) continue;
                        if (!IslandMap.IsWalkable(_g[n.x, n.y]) || Blocks(n.x, n.y)) continue;
                        ClearProp(n);
                        _g[n.x, n.y] = IslandMap.Rock;
                        walls++;
                    }
            }
            // The village's north edge opened onto the trail back from the springs to the log: a short cut past the
            // springs (and now past the hollow). Close it with forest, so the way on runs through the hollow.
            int edge = 0;
            for (int x = 18; x <= 28; x++)
                for (int y = 40; y <= 41; y++)
                {
                    if (_g[x, y] == IslandMap.Path || !IslandMap.IsWalkable(_g[x, y]) || Blocks(x, y)) continue;
                    ClearProp(new Vector2Int(x, y));
                    _g[x, y] = IslandMap.Tree;
                    edge++;
                }
            foreach (var l in HollowLanternXY)
            {
                ClearProp(l);
                Ground(l.x, l.y, IslandMap.Grass);
                Prop(l, KStoneLantern);
            }
            foreach (var h in HollowXY)
            {
                ClearProp(h);
                Prop(h, KSmoke);
            }
            _log?.Append($"\n  smoky hollow: {HollowXY.Length} smoke tiles, {HollowLanternXY.Length} lanterns, {walls} tiles beside it to rock, {edge} on the village's north edge to forest");
        }

        /// <summary>Takes a 1 x 1 prop off a tile (the hollow's walls and lantern spots).</summary>
        static void ClearProp(Vector2Int t)
        {
            if (_p[t.x, t.y] == IslandMap.Empty) return;
            _p[t.x, t.y] = IslandMap.Empty;
            _covered.Remove(t);
        }

        static bool IsHollow(int x, int y)
        {
            foreach (var h in HollowXY)
                if (h.x == x && h.y == y) return true;
            return false;
        }

        static void ForestBits()
        {
            foreach (var (x, y, rx, ry) in new[] { (13.5f, 13f, 1.3f, 2f), (6f, 17f, 1.5f, 1.5f), (12f, 20f, 1.8f, 1.3f), (17.5f, 25f, 1.5f, 1.2f), (26f, 41.5f, 1.5f, 1f), (7f, 10.5f, 1.3f, 1.2f) })
                Patch(x, y, rx, ry);
            foreach (var (x, y) in new[] { (12, 12), (8, 20), (15, 21) }) Prop(x, y, KLeaves);
            foreach (var (x, y) in new[] { (10, 16), (13, 24) }) Prop(x, y, KLanternPost);
            Prop(13, 10, KSign);
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
                "village" => i % 2 == 0 ? KPots : KWood,
                "field" => i % 2 == 0 ? KVent : KLeaves,
                "springs" or "ruins" => KShards,
                _ => (i % 3) switch { 0 => KLeaves, 1 => KLanternPost, _ => IslandMap.Empty },
            };
            if (key == IslandMap.Empty) Ground(x, y, IslandMap.Rock);
            else Prop(x, y, key);
        }

        /// <summary>Straight runs (rows and columns) of plain grass tiles next to a path.</summary>
        static List<List<Vector2Int>> ShoulderRuns()
        {
            bool Shoulder(int x, int y)
            {
                if (!In(x, y) || _g[x, y] != IslandMap.Grass || _p[x, y] != IslandMap.Empty || _covered.Contains(new Vector2Int(x, y))) return false;
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

        static bool IsPath(int x, int y) => In(x, y) && _g[x, y] == IslandMap.Path;

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

        /// <summary>A ragged tall grass patch over grass, trees and rocks (a clearing in the forest); paths, pools and props stay.</summary>
        static void Patch(float cx, float cy, float rx, float ry)
        {
            for (int x = Mathf.FloorToInt(cx - rx - 1); x <= Mathf.CeilToInt(cx + rx + 1); x++)
                for (int y = Mathf.FloorToInt(cy - ry - 1); y <= Mathf.CeilToInt(cy + ry + 1); y++)
                {
                    if (!In(x, y) || _p[x, y] != IslandMap.Empty || _covered.Contains(new Vector2Int(x, y))) continue;
                    char ch = _g[x, y];
                    if (ch != IslandMap.Grass && ch != IslandMap.Tree && ch != IslandMap.Rock) continue;
                    float d = Sq((x - cx) / (rx + 0.5f)) + Sq((y - cy) / (ry + 0.5f));
                    if (d <= 1f - _rnd.Next() * 0.3f + 0.1f) _g[x, y] = IslandMap.TallGrass;
                }
        }

        static void Prop(Vector2Int xy, char key) => Prop(xy.x, xy.y, key);

        /// <summary>
        /// Places a prop with its anchor (top-left = west-most, north-most tile) at (x, y); its footprint spreads east
        /// and south. Refuses (logs) if a footprint tile is taken; multi-tile props get grass under them.
        /// </summary>
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
                    if (!In(t.x, t.y) || _p[t.x, t.y] != IslandMap.Empty || _covered.Contains(t) || _g[t.x, t.y] == IslandMap.HotSpring)
                    {
                        Debug.LogWarning($"[Cinderwood] {recipe.name} at x{x} y{y}: tile x{t.x} y{t.y} is taken; skipped.");
                        return;
                    }
                    tiles.Add(t);
                }
            foreach (var t in tiles)
            {
                _covered.Add(t);
                if (f != Vector2Int.one || _g[t.x, t.y] == IslandMap.Tree || _g[t.x, t.y] == IslandMap.Rock) _g[t.x, t.y] = IslandMap.Grass;
            }
            _p[x, y] = key;
        }

        static PropRecipe RecipeFor(char key, out bool turned)
        {
            turned = key == KFenceTurned;
            string name = key switch
            {
                KGate => "ReturnGate", KShrine => HearthShrines.RecipeName, KHut => "ClayHut", KKiln => "Kiln", KPots => "PotCluster",
                KWood => "Woodpile", KLanternPost => "LanternPost", KStoneLantern => "StoneLantern", KBush => "BerryBush",
                KLog => "FallenLog", KSign => "Signpost", KLeaves => "LeafPile", KCliff or KCliffB => "CliffWall",
                KDoor => "ForgeDoor", KBrazier => "Brazier", KRuin => "RuinedKiln", KShards => "PotShards", KVent => "EmberVent",
                KBellows => "Bellows", KPipe => "ForgePipe", KSmoke => "SmokeCloud", KFirebox => "Firebox", KVine => PropKit.VineName,
                KFence or KFenceTurned => "Fence", KCornerWS or KCornerNW or KCornerEN or KCornerSE => "FenceCorner",
                _ => null,
            };
            return name != null && _recipes.TryGetValue(name, out var r) ? r : null;
        }

        /// <summary>
        /// Walkable tiles the explorer could never reach (the log counts as lifted) become trees; tall grass bits under
        /// 4 tiles become grass (patches are 6-20 tiles; ragged edges leave crumbs).
        /// </summary>
        static void SealCutOff()
        {
            bool Walkable(int x, int y) => In(x, y) && IslandMap.IsWalkable(_g[x, y]) && !Blocks(x, y);
            var seen = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            seen.Add(ArriveXY);
            queue.Enqueue(ArriveXY);
            while (queue.Count > 0)
            {
                var t = queue.Dequeue();
                foreach (var d in new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
                {
                    var n = t + d;
                    bool log = n.y == LogXY.y && (n.x == LogXY.x || n.x == LogXY.x + 1);
                    // Step 10f: the hollow's smoke burns away, so what lies past it counts as reachable.
                    bool smoke = IsHollow(n.x, n.y);
                    if (seen.Contains(n) || !(Walkable(n.x, n.y) || log || smoke)) continue;
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

        /// <summary>A blocking prop covers this tile.</summary>
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

        /// <summary>Tall grass under a walk-through prop stays; under blocking props it turns to grass.</summary>
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

        /// <summary>4-connected tiles from a to b hugging the straight line.</summary>
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

        /// <summary>
        /// Perf tour stops (column, row): the arrival, then a walkable tile every ~8 tiles along the trail up to the tile
        /// in front of the log (the temple is behind it in 9a). Each stop is well inside a path search from the last.
        /// </summary>
        public static List<Vector2Int> TourStops(WorldMap map)
        {
            var stops = new List<Vector2Int> { Arrive };
            var samples = TrailSamples();
            var logFront = Tile(LogXY + Vector2Int.down);
            for (int i = 8; i < samples.Count; i += 8)
            {
                var s = samples[i];
                if (s.y >= LogXY.y - 0.5f) break;
                var t = Tile(Mathf.RoundToInt(s.x), Mathf.RoundToInt(s.y));
                if (!map.CanWalk(t))
                    foreach (var d in new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
                        if (map.CanWalk(t + d))
                        {
                            t += d;
                            break;
                        }
                if (map.CanWalk(t) && t != stops[stops.Count - 1]) stops.Add(t);
            }
            stops.Add(logFront);
            return stops;
        }

        public static Vector2Int VillageSpot => Tile(15, 31);
        public static Vector2Int SpringsSpot => Tile(37, 41);

        // ================= checks =================

        /// <summary>
        /// The automated layout checks (also an EditMode test): the trail connects arrival, shrine, springs and the log;
        /// every pocket is reachable; nothing walkable is cut off except what lies behind the log (the temple, 9b);
        /// tall grass share and patch sizes; trail bends; filler spacing; edges. Returns the problems (empty = fine).
        /// </summary>
        public static List<string> Check(WorldMap map, StringBuilder report)
        {
            var problems = new List<string>();
            if (map.Columns != Size || map.Rows != Size) problems.Add($"map is {map.Columns}x{map.Rows}, not {Size}x{Size}");

            // Nov 3: the arrival's vine counts as burnt (it is the realm's first moment, right past the arrival).
            bool Walk(Vector2Int t, bool logOpen, bool hollowOpen) => map.CanWalk(t) || logOpen && IsLogTile(map, t) && IslandMap.IsWalkable(map.At(t)) ||
                                                                      hollowOpen && IsHollowTile(t) && IslandMap.IsWalkable(map.At(t)) ||
                                                                      System.Array.IndexOf(VineTiles, t) >= 0 && IslandMap.IsWalkable(map.At(t));
            HashSet<Vector2Int> Flood(bool logOpen, bool hollowOpen = true)
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
                        if (!seen.Contains(n) && Walk(n, logOpen, hollowOpen))
                        {
                            seen.Add(n);
                            queue.Enqueue(n);
                        }
                    }
                }
                return seen;
            }
            if (!map.CanWalk(Arrive)) problems.Add($"arrival tile c{Arrive.x} r{Arrive.y} isn't walkable");
            var reach = Flood(false);
            var reachOpen = Flood(true);
            // Step 10f: with the smoke on the path the village and its shrine are reachable, the springs are not.
            var smoky = Flood(false, false);
            if (!smoky.Contains(Tile(ShrineXY + new Vector2Int(0, -2)))) problems.Add("the shrine front isn't reachable before the smoky hollow");
            if (smoky.Contains(Tile(37, 41))) problems.Add("the smoky hollow can be walked round (the springs are reachable with the smoke on the path)");
            foreach (var h in HollowXY)
                if (map.CanWalk(Tile(h)) || !IslandMap.IsWalkable(map.At(Tile(h)))) problems.Add($"hollow tile x{h.x} y{h.y} isn't a smoke-blocked path tile");

            void Need(string what, Vector2Int xy)
            {
                if (!reach.Contains(Tile(xy))) problems.Add($"{what} (x{xy.x} y{xy.y}) not reachable from the arrival");
            }
            Need("shrine front", ShrineXY + new Vector2Int(0, -2));
            Need("springs trail", new Vector2Int(37, 41));
            Need("log front", LogXY + Vector2Int.down);
            Need("return gate front", GateXY + Vector2Int.down);
            foreach (var z in new[] { "field", "glade", "grove", "ruins", "springs", "village", "forest" })
                if (!reach.Any(t => map.ZoneAt(t) == z && map.CanWalk(t))) problems.Add($"pocket '{z}' has no reachable tile");
            if (reach.Contains(Tile(DoorXY + new Vector2Int(1, -1)))) problems.Add("the forge door is reachable past the log (it should block the trail)");
            if (!reachOpen.Contains(Tile(DoorXY + new Vector2Int(1, -1)))) problems.Add("the forge door isn't reachable even with the log lifted");

            int walkable = 0, tall = 0, cut = 0;
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                {
                    var t = new Vector2Int(c, r);
                    if (!map.CanWalk(t)) continue;
                    walkable++;
                    if (map.At(t) == IslandMap.TallGrass) tall++;
                    if (!reachOpen.Contains(t)) cut++;
                }
            int behindLog = reachOpen.Count - reach.Count;
            if (cut > 0) problems.Add($"{cut} walkable tile(s) cut off from everything");
            float share = walkable > 0 ? tall / (float)walkable : 0f;
            if (share < 0.16f || share > 0.25f) problems.Add($"tall grass is {share:P0} of the walkable land (want ~20%)");

            // Tall grass patches (4-connected), 6-20 tiles; smaller ones are reported, not failed (ragged edges).
            var patches = Components(map, t => map.At(t) == IslandMap.TallGrass);
            int small = patches.Count(p => p < 6), big = patches.Count(p => p > 20);
            if (big > 0) problems.Add($"{big} tall grass patch(es) over 20 tiles: {string.Join(", ", patches.Where(p => p > 20))}");

            // Trail: step 14i: straight legs, none over MaxLeg tiles (was: no straight run over 10 on a winding trail).
            int longest = LongestStraight();
            if (longest > MaxLeg) problems.Add($"a straight trail run of {longest} tiles (max {MaxLeg})");

            // Something every ~8 tiles along the trail: a prop, tall grass, a rock, a pool within 3 tiles.
            int gap = LongestFeatureGap(map);
            if (gap > 9) problems.Add($"{gap} trail tiles in a row with nothing to look at (want ~8)");

            // Edges: the ring inside the border is trees, rocks or cliff.
            int bare = 0, ring = 0;
            for (int i = 1; i < Size - 1; i++)
                foreach (var t in new[] { new Vector2Int(i, 1), new Vector2Int(i, Size - 2), new Vector2Int(1, i), new Vector2Int(Size - 2, i) })
                {
                    ring++;
                    char ch = map.At(t);
                    if (ch != IslandMap.Tree && ch != IslandMap.Rock && map.PropAt(t.y, t.x) == IslandMap.Empty) bare++;
                }
            if (bare > ring * 0.15f) problems.Add($"{bare} of {ring} tiles along the map edge are bare");

            report?.Append($"\n  walkable {walkable}, reachable {reach.Count} (+{behindLog} behind the log), tall grass {share:P0} in {patches.Count} patches " +
                           $"(sizes {string.Join(" ", patches.OrderBy(p => p))}; {small} under 6), longest straight trail {longest}, " +
                           $"longest plain stretch {gap}, bare edge tiles {bare}/{ring}");
            return problems;
        }

        static bool IsHollowTile(Vector2Int t) => IsHollow(t.x, Size - 1 - t.y);

        static bool IsLogTile(WorldMap map, Vector2Int t)
        {
            var log = LogTile;
            return t.y == log.y && (t.x == log.x || t.x == log.x + 1);
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

        /// <summary>Points every tile along the trail's centre line (x, y).</summary>
        static List<Vector2> TrailSamples()
        {
            var list = new List<Vector2>();
            for (int i = 0; i + 1 < Line.Length; i++)
            {
                var a = Line[i];
                var b = Line[i + 1];
                int n = Mathf.Max(1, Mathf.CeilToInt((b - a).magnitude));
                for (int k = 0; k < n; k++) list.Add(Vector2.Lerp(a, b, k / (float)n));
            }
            list.Add(Line[Line.Length - 1]);
            return list;
        }

        /// <summary>
        /// The longest stretch of trail that stays within 0.8 tile of a straight line (the 2-wide trail then reads as
        /// straight): chords between waypoints whose in-between waypoints all lie that close.
        /// </summary>
        static int LongestStraight()
        {
            float best = 0f;
            for (int i = 0; i + 1 < Line.Length; i++)
                for (int j = i + 1; j < Line.Length; j++)
                {
                    Vector2 a = Line[i], ab = Line[j] - a;
                    bool straight = true;
                    for (int k = i + 1; k < j && straight; k++)
                    {
                        var v = Line[k] - a;
                        straight = Mathf.Abs(v.x * ab.y - v.y * ab.x) / ab.magnitude <= 0.8f;
                    }
                    if (!straight) break;
                    best = Mathf.Max(best, ab.magnitude);
                }
            return Mathf.RoundToInt(best);
        }

        static int LongestFeatureGap(WorldMap map)
        {
            int gap = 0, best = 0;
            foreach (var s in TrailSamples())
            {
                bool feature = false;
                for (int dx = -3; dx <= 3 && !feature; dx++)
                    for (int dy = -3; dy <= 3 && !feature; dy++)
                    {
                        var t = Tile(Mathf.RoundToInt(s.x) + dx, Mathf.RoundToInt(s.y) + dy);
                        char ch = map.At(t);
                        feature = ch == IslandMap.TallGrass || ch == IslandMap.Rock || ch == IslandMap.HotSpring || map.PropAt(t.y, t.x) != IslandMap.Empty;
                    }
                gap = feature ? 0 : gap + 1;
                best = Mathf.Max(best, gap);
            }
            return best;
        }
    }
}
