using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 9g: the Quick Journey on paper. <see cref="Keeper"/> walks the quick route with the config's dials (the gift
    /// team: the starter raised to <see cref="QuickJourneyConfig.raiseTo"/> plus the two gifts; Cinderwood encounters x
    /// <see cref="QuickJourneyConfig.encounterRate"/>, XP x <see cref="QuickJourneyConfig.xpMultiplier"/>, no rares) and
    /// fights the Keeper with the arrival levels, per starter, for a few gift levels. Target 75-85% for every starter.
    /// <see cref="DryRun"/> estimates the route time per segment from tile counts, walk speed, encounter rate and the
    /// config's per-beat seconds. Target 12 min or less.
    /// </summary>
    public static class QuickJourneySim
    {
        public const string ConfigPath = "Assets/_Project/Data/QuickJourney.asset";
        const string HubPath = "Assets/_Project/Data/WorldMap_StillIsle.asset";

        static QuickJourneyConfig Config => AssetDatabase.LoadAssetAtPath<QuickJourneyConfig>(ConfigPath);
        static ProgressionConfig Progression => AssetDatabase.LoadAssetAtPath<ProgressionConfig>("Assets/_Project/Data/Battle/Progression.asset");
        static WorldMap Hub => AssetDatabase.LoadAssetAtPath<WorldMap>(HubPath);
        static WorldMap Cinder => AssetDatabase.LoadAssetAtPath<WorldMap>(CinderwoodTools.MapPath);

        public struct Result
        {
            public int giftLevel;
            public float arrival, arrivalP10, arrivalP90;
            public float battlesHub, battlesCinder;
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
            var log = new StringBuilder($"[QuickSim] Keeper (Hearthstag Lv {KeeperSim.KeeperLevel}) vs the quick team: the starter (raised to the gift level, " +
                                        $"fights the trail) + {string.Join(" + ", config.giftSpecies.Where(s => s != null).Select(s => s.name))} (gift level, benched). " +
                                        $"Cinderwood: encounters x{config.encounterRate}, XP x{config.xpMultiplier}, rares x{config.rareWeight}.\n");
            log.Append("| Gift Lv | Starter arrives (p10 / mean / p90) | Battles hub / Cinderwood | Kilnpot | Pebbloop | Pufflet |\n|---|---|---|---|---|---|\n");
            foreach (int lv in new[] { 7, 8, 9 })
            {
                var r = Run(config, lv, 600, 400);
                log.Append($"| all {lv} | {r.arrivalP10:0} / {r.arrival:0.0} / {r.arrivalP90:0} | {r.battlesHub:0.0} / {r.battlesCinder:0.0} |");
                foreach (var n in KeeperSim.Starters) log.Append($" {(r.win.TryGetValue(n, out float w) ? w : 0f):P0} |");
                log.Append('\n');
            }
            // The config: each starter with its own gift / raise levels.
            log.Append("| config |");
            var cells = new StringBuilder();
            foreach (var n in KeeperSim.Starters)
            {
                var (gift, raise) = config.LevelsFor(KeeperSim.Load(n));
                var r = Run(config, gift, 600, 1000, 5, raise, n);
                log.Append($" {n} gift {gift}, raised {raise}: arrives {r.arrival:0.0}");
                cells.Append($" {(r.win.TryGetValue(n, out float w) ? w : 0f):P0} |");
            }
            log.Append($" | - |{cells}\n");
            Debug.Log(log.ToString());
        }

        /// <summary>Win % per starter over gift level (rows) x the level the starter is raised to (columns).</summary>
        public static void Sweep()
        {
            var config = Config;
            if (config == null) return;
            var log = new StringBuilder("[QuickSim] Sweep: Keeper win % (rows: gift level of the two friends, columns: the starter's level after the gift).\n");
            foreach (var name in KeeperSim.Starters)
            {
                log.Append($"{name}:\n| Gift \\ starter | 7 | 8 | 9 | 10 |\n|---|---|---|---|---|\n");
                for (int g = 7; g <= 10; g++)
                {
                    log.Append($"| {g} |");
                    for (int s = 7; s <= 10; s++) log.Append($" {Run(config, g, 0, 300, 5, s, name).win[name]:P0} |");
                    log.Append('\n');
                }
            }
            Debug.Log(log.ToString());
        }

        /// <summary>The quick route with gift level <paramref name="giftLevel"/>: arrival levels, then Keeper fights per starter.</summary>
        public static Result Run(QuickJourneyConfig config, int giftLevel, int trailRuns, int fights, int seed = 5, int starterLevel = 0, string only = null)
        {
            var result = new Result { giftLevel = giftLevel, win = new Dictionary<string, float>() };
            var keeper = KeeperSim.Load("Hearthstag");
            var chart = KeeperSim.Chart;
            var prog = Progression;
            var hub = Hub;
            var cinder = Cinder;
            if (keeper == null || chart == null || prog == null || hub == null || cinder == null) return result;
            var gifts = config.giftSpecies.Where(s => s != null).ToList();
            var (hubPath, cinderPath) = Route(hub, cinder);

            var rnd = new SeededRandom(seed);
            var arrivals = new List<int>();
            float bh = 0f, bc = 0f;
            foreach (var name in KeeperSim.Starters.Where(n => only == null || n == only))
            {
                var starter = KeeperSim.Load(name);
                if (starter == null) continue;
                int wins = 0;
                for (int i = 0; i < fights; i++)
                {
                    // The trail (resampled per fight so the arrival level varies like real runs).
                    // The gift raises the starter to the team's level (the config keeps raiseTo = giftLevel).
                    var me = new IslingInstance(starter, starterLevel > 0 ? starterLevel : giftLevel);
                    int since = 0;
                    bh += Walk(hub, hubPath, me, prog, 1f, 1f, rnd, ref since);
                    bc += Walk(cinder, cinderPath, me, prog, config.encounterRate, config.xpMultiplier, rnd, ref since);
                    if (i < trailRuns) arrivals.Add(me.level);
                    var party = new List<IslingInstance> { new IslingInstance(starter, me.level) };
                    foreach (var g in gifts) party.Add(new IslingInstance(g, giftLevel));
                    if (KeeperSim.Fight(party, new IslingInstance(keeper, KeeperSim.KeeperLevel), chart, seed * 7919 + i, out _) == BattleResult.Won) wins++;
                }
                result.win[name] = wins / (float)Mathf.Max(1, fights);
            }
            int runs = Mathf.Max(1, fights * result.win.Count);
            result.battlesHub = bh / runs;
            result.battlesCinder = bc / runs;
            arrivals.Sort();
            if (arrivals.Count > 0)
            {
                result.arrival = (float)arrivals.Average();
                result.arrivalP10 = arrivals[arrivals.Count / 10];
                result.arrivalP90 = arrivals[arrivals.Count * 9 / 10];
            }
            return result;
        }

        /// <summary>The quick route's walking: hub first grass to the fire gate; Cinderwood arrival, first grass, plaza, springs, log, temple.</summary>
        static (List<Vector2Int> hub, List<Vector2Int> cinder) Route(WorldMap hub, WorldMap cinder)
        {
            int fire = hub.gates.FindIndex(g => g.id == "fire");
            var gate = fire >= 0 ? hub.gates[fire].Front(0) : hub.startTile;
            var hubPath = KeeperSim.Chain(hub, KeeperSim.Mark(hub, "firstGrass"), gate);
            var cinderPath = KeeperSim.Chain(cinder, KeeperSim.Mark(cinder, "start"), KeeperSim.Mark(cinder, "firstGrass"), KeeperSim.Mark(cinder, "plaza"),
                KeeperSim.Mark(cinder, "springs"), KeeperSim.Mark(cinder, "log"), KeeperSim.Mark(cinder, "temple"));
            return (hubPath, cinderPath);
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
                // Rares are off in the quick Cinderwood: roll again (the hub keeps them, a few % only).
                for (int k = 0; k < 6 && rare && rate < 1f; k++) table.Roll(rnd, out wild, out rare);
                if (rate >= 1f) table.Roll(rnd, out wild, out rare);
                if (wild == null) continue;
                battles++;
                Diorama.Progression.AddXp(me, Mathf.Max(1, Mathf.RoundToInt(prog.Reward(wild.level, rare) * xpScale)), prog);
            }
            return battles;
        }

        // ---- dry run ----

        [MenuItem("Tools/Pocket Isle/Quick Journey/Dry Run", priority = 900)]
        public static void DryRun()
        {
            var config = Config;
            var hub = Hub;
            var cinder = Cinder;
            if (config == null || hub == null || cinder == null)
            {
                Debug.LogError("[QuickSim] Config or maps missing (Step9gSetup.QuickOnly).");
                return;
            }
            float walk = config.walkSecondsPerTile;
            var r = Run(config, config.giftLevel, 300, 300);
            int fire = hub.gates.FindIndex(g => g.id == "fire");
            var hubGate = fire >= 0 ? hub.gates[fire].Front(0) : hub.startTile;
            int giftWalk = KeeperSim.PathBetween(hub, KeeperSim.Mark(hub, "firstGrass"), hubGate).Count;
            var trail = KeeperSim.Chain(cinder, KeeperSim.Mark(cinder, "start"), KeeperSim.Mark(cinder, "firstGrass"), KeeperSim.Mark(cinder, "plaza"),
                KeeperSim.Mark(cinder, "springs"), KeeperSim.Mark(cinder, "log")).Count;
            int toDoor = KeeperSim.PathBetween(cinder, KeeperSim.Mark(cinder, "log"), KeeperSim.Mark(cinder, "temple")).Count;
            // Home: the forge door to the nearest return gate, then the hub arrival to the statue.
            var door = KeeperSim.Mark(cinder, "temple");
            int home = int.MaxValue;
            Vector2Int arrive = hub.startTile;
            foreach (var g in cinder.gates)
            {
                if (g.id != "home") continue;
                int n = KeeperSim.PathBetween(cinder, door, g.Front(0)).Count;
                if (n > 0 && n < home)
                {
                    home = n;
                    arrive = g.arrive;
                }
            }
            if (home == int.MaxValue) home = 0;
            int toStatue = KeeperSim.PathBetween(hub, arrive, KeeperSim.Mark(hub, "statue")).Count;

            var rows = new List<(string name, float seconds, string how)>
            {
                ("Opening + lessons", config.openingSeconds + config.lessonSeconds + 1.5f * config.battleSeconds,
                    $"opening {config.openingSeconds:0} s, lessons {config.lessonSeconds:0} s, tutorial battle {1.5f * config.battleSeconds:0} s"),
                ("Gift -> gate", config.giftSeconds + giftWalk * walk + r.battlesHub * config.battleSeconds + config.gateSeconds,
                    $"gift {config.giftSeconds:0} s, {giftWalk} tiles, {r.battlesHub:0.0} battles, gate {config.gateSeconds:0} s"),
                ("Trail (arrival -> log)", trail * walk + r.battlesCinder * config.battleSeconds + config.trailTalkSeconds,
                    $"{trail} tiles, {r.battlesCinder:0.0} battles (x{config.encounterRate} rate), Spirit lines {config.trailTalkSeconds:0} s"),
                ("Log", config.logSeconds, "lift and place"),
                ("Forge", toDoor * walk + config.forgeSeconds, $"{toDoor} tiles to the door, bellows {config.forgeSeconds:0} s"),
                ("Keeper", config.keeperSeconds, "meeting, fight, kneel, shard"),
                ("Statue", home * walk + config.gateSeconds + toStatue * walk + config.statueSeconds,
                    $"{home} tiles to the return gate, gate, {toStatue} tiles to the statue, place + end {config.statueSeconds:0} s"),
            };
            float total = rows.Sum(x => x.seconds);
            var log = new StringBuilder($"[QuickSim] Quick Journey dry run ({walk:0.0} s per tile, battle {config.battleSeconds:0} s): {QuickJourney.Format(total)} " +
                                        $"(target 12:00 or less{(total <= 720f ? ", OK" : ", OVER")}).\n| Segment | Time | How |\n|---|---|---|\n");
            foreach (var (name, seconds, how) in rows) log.Append($"| {name} | {QuickJourney.Format(seconds)} | {how} |\n");
            Debug.Log(log.ToString());
        }
    }
}
