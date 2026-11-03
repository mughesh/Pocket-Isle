using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 9g, a Galewick journey since step 14c: the Quick Journey on paper. <see cref="Keeper"/> walks the quick route
    /// with the config's dials (the gift team: the starter raised to <see cref="QuickJourneyConfig.raiseTo"/> plus the two
    /// gifts; Galewick encounters x <see cref="QuickJourneyConfig.encounterRate"/>, XP x
    /// <see cref="QuickJourneyConfig.xpMultiplier"/>, no rares) and fights Galeowl at 0 shards, per starter. Target 75-85%
    /// for every starter. <see cref="DryRun"/> estimates the route time per beat from tile counts, walk speed, encounter
    /// rate and the config's per-beat seconds. Target 12 min or less.
    /// </summary>
    public static class QuickJourneySim
    {
        public const string ConfigPath = "Assets/_Project/Data/QuickJourney.asset";
        const string HubPath = "Assets/_Project/Data/WorldMap_StillIsle.asset";
        const string Keeper_ = "Galeowl";

        /// <summary>The wind friends the gift could be (step 14c: picked by <see cref="SweepWind"/>).</summary>
        public static readonly string[] WindGifts = { "Kitelet", "Whirlby", "Samarling", "Vanehare" };

        /// <summary>The Galewick legs: arrival to the tower (the ridge's clouds are cleared already in the quick slot).</summary>
        static readonly string[] GalewickStops = { "start", "firstGrass", "plaza", "mill", "kites", "ridge", "tower" };

        static QuickJourneyConfig Config => AssetDatabase.LoadAssetAtPath<QuickJourneyConfig>(ConfigPath);
        static ProgressionConfig Progression => AssetDatabase.LoadAssetAtPath<ProgressionConfig>("Assets/_Project/Data/Battle/Progression.asset");
        static WorldMap Hub => AssetDatabase.LoadAssetAtPath<WorldMap>(HubPath);
        static WorldMap Gale => AssetDatabase.LoadAssetAtPath<WorldMap>(GalewickTools.MapPath);

        public struct Result
        {
            public int giftLevel;
            public float arrival, arrivalP10, arrivalP90;
            public float battlesHub, battlesRealm;
            public Dictionary<string, float> win;
            public float Min => win.Count > 0 ? win.Values.Min() : 0f;
            public float Max => win.Count > 0 ? win.Values.Max() : 0f;
        }

        // ---- Keeper ----

        [MenuItem("Tools/Pocket Isle/Quick Journey/Keeper Sim", priority = 901)]
        public static void Keeper()
        {
            var config = Config;
            if (config == null)
            {
                Debug.LogError($"[QuickSim] {ConfigPath} missing (Step9gSetup.QuickOnly).");
                return;
            }
            var gifts = config.giftSpecies.Where(s => s != null).ToList();
            var log = new StringBuilder($"[QuickSim] Keeper ({Keeper_} Lv {GaleowlKit.Level}, 0 shards) vs the quick team: the starter (raised, fights the trail) + " +
                                        $"{string.Join(" + ", gifts.Select(s => s.name))} (gift level, benched; the fire one leads: it breaks the Gust Wall). " +
                                        $"Galewick: encounters x{config.encounterRate}, XP x{config.xpMultiplier}, rares x{config.rareWeight}.\n");
            log.Append("| Starter | Gift Lv | Raised to | Starter at Galeowl (p10 / mean / p90) | Battles hub / Galewick | Win |\n|---|---|---|---|---|---|\n");
            foreach (var n in KeeperSim.Starters)
            {
                var (gift, raise) = config.LevelsFor(KeeperSim.Load(n));
                var r = Run(config, gifts, gift, raise, 600, 1000, 5, n);
                log.Append($"| {n} | {gift} | {raise} | {r.arrivalP10:0} / {r.arrival:0.0} / {r.arrivalP90:0} | {r.battlesHub:0.0} / {r.battlesRealm:0.0} | " +
                           $"{(r.win.TryGetValue(n, out float w) ? w : 0f):P0} |\n");
            }
            Debug.Log(log.ToString());
        }

        /// <summary>Step 14c: Toastling + each wind friend, win % per starter over gift level x the starter's raised level.</summary>
        public static void SweepWind()
        {
            var config = Config;
            var toast = KeeperSim.Load("Toastling");
            if (config == null || toast == null) return;
            var log = new StringBuilder($"[QuickSim] Wind gift sweep vs {Keeper_} Lv {GaleowlKit.Level}: Toastling + the wind friend at the gift level (rows), the starter raised to (columns).\n");
            foreach (var wind in WindGifts)
            {
                var w = KeeperSim.Load(wind);
                if (w == null) continue;
                var gifts = new List<IslingSpecies> { toast, w };
                foreach (var name in KeeperSim.Starters)
                {
                    log.Append($"{wind} / {name}:");
                    for (int g = 6; g <= 9; g++)
                    {
                        log.Append($" | gift {g}:");
                        for (int s = 6; s <= 9; s++) log.Append($" {Run(config, gifts, g, s, 0, 300, 5, name).win[name] * 100f,3:0}");
                    }
                    log.Append('\n');
                }
            }
            Debug.Log(log.ToString());
        }

        /// <summary>Bridge: one starter with the config's gifts at given levels; Temp/ClaudeBridge/arg.txt "Kilnpot,6,7" (starter, gift, raised).</summary>
        public static void Try()
        {
            var a = System.IO.File.ReadAllText("Temp/ClaudeBridge/arg.txt").Split(',');
            var config = Config;
            var r = Run(config, config.giftSpecies.Where(s => s != null).ToList(), int.Parse(a[1]), int.Parse(a[2]), 600, 1000, 5, a[0]);
            Debug.Log($"[QuickSim] Try {a[0]} gift {a[1]} raised {a[2]}: arrives {r.arrival:0.0}, win {r.win[a[0]]:P0}");
        }

        /// <summary>The quick route with the gifts at <paramref name="giftLevel"/>: arrival levels, then Galeowl fights per starter.</summary>
        public static Result Run(QuickJourneyConfig config, List<IslingSpecies> gifts, int giftLevel, int starterLevel, int trailRuns, int fights, int seed = 5,
            string only = null)
        {
            var result = new Result { giftLevel = giftLevel, win = new Dictionary<string, float>() };
            var keeper = KeeperSim.Load(Keeper_);
            var chart = KeeperSim.Chart;
            var prog = Progression;
            var hub = Hub;
            var gale = Gale;
            if (keeper == null || chart == null || prog == null || hub == null || gale == null) return result;
            var (hubPath, realmPath) = Route(hub, gale);

            var rnd = new SeededRandom(seed);
            var arrivals = new List<int>();
            float bh = 0f, br = 0f;
            foreach (var name in KeeperSim.Starters.Where(n => only == null || n == only))
            {
                var starter = KeeperSim.Load(name);
                if (starter == null) continue;
                int wins = 0;
                for (int i = 0; i < fights; i++)
                {
                    // The trail (resampled per fight so the arrival level varies like real runs).
                    var me = new IslingInstance(starter, starterLevel > 0 ? starterLevel : giftLevel);
                    int since = 0;
                    bh += Walk(hub, hubPath, me, prog, 1f, 1f, rnd, ref since);
                    br += Walk(gale, realmPath, me, prog, config.encounterRate, config.xpMultiplier, rnd, ref since);
                    if (i < trailRuns) arrivals.Add(me.level);
                    var party = new List<IslingInstance> { new IslingInstance(starter, me.level) };
                    foreach (var g in gifts) party.Add(new IslingInstance(g, giftLevel));
                    if (KeeperSim.Fight(party, new IslingInstance(keeper, GaleowlKit.Level), chart, seed * 7919 + i, out _) == BattleResult.Won) wins++;
                }
                result.win[name] = wins / (float)Mathf.Max(1, fights);
            }
            int runs = Mathf.Max(1, fights * result.win.Count);
            result.battlesHub = bh / runs;
            result.battlesRealm = br / runs;
            arrivals.Sort();
            if (arrivals.Count > 0)
            {
                result.arrival = (float)arrivals.Average();
                result.arrivalP10 = arrivals[arrivals.Count / 10];
                result.arrivalP90 = arrivals[arrivals.Count * 9 / 10];
            }
            return result;
        }

        /// <summary>The quick route's walking: hub first grass to the wind gate; Galewick arrival through the village, mill, kites and ridge to the tower.</summary>
        static (List<Vector2Int> hub, List<Vector2Int> realm) Route(WorldMap hub, WorldMap gale) =>
            (KeeperSim.Chain(hub, KeeperSim.Mark(hub, "firstGrass"), WindGate(hub)), RealmOrderSim.Chain(gale, GalewickStops));

        static Vector2Int WindGate(WorldMap hub)
        {
            int wind = hub.gates.FindIndex(g => g.id == "wind");
            return wind >= 0 ? hub.gates[wind].Front(0) : hub.startTile;
        }

        /// <summary>One walk along <paramref name="path"/>: 3 grace steps after a battle, then 12.5% x rate per tall-grass step; no rares; the walker wins and gets XP x scale.</summary>
        static int Walk(WorldMap map, List<Vector2Int> path, IslingInstance me, ProgressionConfig prog, float rate, float xpScale, SeededRandom rnd, ref int since)
        {
            int battles = 0;
            foreach (var t in path)
            {
                if (map.At(t) != IslandMap.TallGrass) continue;
                if (++since <= 3 || rnd.Next() >= 0.125f * rate) continue;
                since = 0;
                var table = map.GrassTableAt(t);
                if (table == null) continue;
                IslingInstance wild = null;
                bool rare = true;
                // Rares are off in the quick realm: roll again (the hub keeps them, a few % only).
                for (int k = 0; k < 6 && rare && rate < 1f; k++) table.Roll(rnd, out wild, out rare);
                if (rate >= 1f) table.Roll(rnd, out wild, out rare);
                if (wild == null) continue;
                battles++;
                Diorama.Progression.AddXp(me, Mathf.Max(1, Mathf.RoundToInt(prog.Reward(wild.level, rare) * xpScale)), prog);
            }
            return battles;
        }

        // ---- dry run ----

        public struct Beat
        {
            public string name, how;
            public float seconds;
        }

        /// <summary>The per-beat estimate (step 14c route): opening + lessons, gift, gate, arrival, mill, kites, tower, untangle, Galeowl, home, statue.</summary>
        public static List<Beat> Beats(QuickJourneyConfig config, WorldMap hub, WorldMap gale, Result r)
        {
            float walk = config.walkSecondsPerTile;
            int Tiles(string a, string b) => RealmOrderSim.Chain(gale, new[] { a, b }).Count;
            int giftWalk = KeeperSim.PathBetween(hub, KeeperSim.Mark(hub, "firstGrass"), WindGate(hub)).Count;
            int toMill = Tiles("start", "firstGrass") + Tiles("firstGrass", "plaza") + Tiles("plaza", "mill");
            int toKites = Tiles("mill", "kites");
            int toTower = Tiles("kites", "ridge") + Tiles("ridge", "tower");
            int realmTiles = toMill + toKites + toTower;
            // Battles spread over the realm's walk by tiles.
            float Battles(int tiles) => realmTiles > 0 ? r.battlesRealm * tiles / realmTiles : 0f;
            // Home: the tower door to the nearest return gate, then the hub arrival to the statue.
            var door = KeeperSim.Mark(gale, "tower");
            int home = int.MaxValue;
            Vector2Int arrive = hub.startTile;
            foreach (var g in gale.gates)
            {
                if (g.id != "home") continue;
                int n = RealmOrderSim.Chain(gale, new[] { "tower", "start" }).Count;
                int direct = KeeperSim.PathBetween(gale, door, g.Front(0)).Count;
                if (direct > 0) n = direct;
                if (n > 0 && n < home)
                {
                    home = n;
                    arrive = g.arrive;
                }
            }
            if (home == int.MaxValue) home = 0;
            int toStatue = KeeperSim.PathBetween(hub, arrive, KeeperSim.Mark(hub, "statue")).Count;
            float talk = config.trailTalkSeconds;
            return new List<Beat>
            {
                new Beat { name = "Opening + lessons", seconds = config.openingSeconds + config.lessonSeconds + 1.5f * config.battleSeconds,
                    how = $"opening {config.openingSeconds:0} s, lessons {config.lessonSeconds:0} s, tutorial battle {1.5f * config.battleSeconds:0} s" },
                new Beat { name = "Gift", seconds = config.giftSeconds, how = "two pods, the levels raised, quick.gate" },
                new Beat { name = "To the wind gate", seconds = giftWalk * walk + r.battlesHub * config.battleSeconds + config.gateSeconds,
                    how = $"{giftWalk} tiles, {r.battlesHub:0.0} battles, gate {config.gateSeconds:0} s" },
                new Beat { name = "Arrival sweep", seconds = config.arriveSeconds, how = "two cloud puffs on the path" },
                new Beat { name = "Mill", seconds = toMill * walk + Battles(toMill) * config.battleSeconds + config.millSeconds + talk * 0.4f,
                    how = $"{toMill} tiles, {Battles(toMill):0.0} battles, flick {config.millSeconds:0} s, village lines" },
                new Beat { name = "Kites", seconds = toKites * walk + Battles(toKites) * config.battleSeconds + config.kitesSeconds + talk * 0.3f,
                    how = $"{toKites} tiles, {Battles(toKites):0.0} battles, three kites {config.kitesSeconds:0} s" },
                new Beat { name = "Tower", seconds = toTower * walk + Battles(toTower) * config.battleSeconds + config.towerSeconds + talk * 0.3f,
                    how = $"{toTower} tiles (the ridge's clouds already gone), {Battles(toTower):0.0} battles, tie the kites {config.towerSeconds:0} s" },
                new Beat { name = "Untangle", seconds = config.untangleSeconds, how = "Galeowl's knots" },
                new Beat { name = "Galeowl", seconds = config.keeperSeconds, how = "meeting, fight, shard" },
                new Beat { name = "Home", seconds = home * walk + config.gateSeconds + toStatue * walk,
                    how = $"{home} tiles to the return gate, gate, {toStatue} tiles to the statue" },
                new Beat { name = "Statue + end", seconds = config.statueSeconds, how = "place, whisper, quick.end, runes" },
            };
        }

        [MenuItem("Tools/Pocket Isle/Quick Journey/Dry Run", priority = 900)]
        public static void DryRun()
        {
            var config = Config;
            var hub = Hub;
            var gale = Gale;
            if (config == null || hub == null || gale == null)
            {
                Debug.LogError("[QuickSim] Config or maps missing (Step9gSetup.QuickOnly).");
                return;
            }
            var gifts = config.giftSpecies.Where(s => s != null).ToList();
            var r = Run(config, gifts, config.giftLevel, config.raiseTo, 300, 300);
            var rows = Beats(config, hub, gale, r);
            float total = rows.Sum(x => x.seconds);
            var log = new StringBuilder($"[QuickSim] Quick Journey dry run, Galewick ({config.walkSecondsPerTile:0.0} s per tile, battle {config.battleSeconds:0} s): " +
                                        $"{QuickJourney.Format(total)} (target 12:00 or less{(total <= 720f ? ", OK" : ", OVER")}).\n| Beat | Time | How |\n|---|---|---|\n");
            foreach (var b in rows) log.Append($"| {b.name} | {QuickJourney.Format(b.seconds)} | {b.how} |\n");
            Debug.Log(log.ToString());
        }
    }
}
