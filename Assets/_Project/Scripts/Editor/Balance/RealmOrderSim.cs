using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 14c: the full adventure in the Galewick-first order (wind, fire, water). One run walks the hub's lesson
    /// trail to the wind gate, then each realm's trail to its Keeper with the shards held so far (wild +3 per shard, the
    /// Keeper +2 per other shard), the starter fighting and winning every battle (XP to the one on the pad), and catches
    /// 1-2 of the wilds it meets in each realm (benched, caught at their level). Each Keeper is fought by the starter +
    /// the two latest catches. Targets: 60-75% per starter at each Keeper. Bridge:
    /// exec:Diorama.EditorTools.RealmOrderSim.Report.
    /// </summary>
    public static class RealmOrderSim
    {
        const string HubPath = "Assets/_Project/Data/WorldMap_StillIsle.asset";

        public struct Leg
        {
            public string realm, keeper, gate, helper, helperAt;
            public WorldMap map;
            public string[] stops;
            public int keeperLevel;
        }

        public struct Stats
        {
            public float arrive, p10, p90, battles;
            public Dictionary<string, float> win;
        }

        static WorldMap Load(string path) => AssetDatabase.LoadAssetAtPath<WorldMap>(path);

        /// <summary>The three realm legs in the Galewick-first order.</summary>
        public static List<Leg> Order() => new List<Leg>
        {
            new Leg { realm = "Galewick", keeper = "Galeowl", gate = "wind", map = Load(GalewickTools.MapPath), keeperLevel = GaleowlKit.Level,
                helper = "Toastling", helperAt = "fire",
                stops = new[] { "start", "firstGrass", "plaza", "mill", "kites", "ridge", "tower" } },
            new Leg { realm = "Cinderwood", keeper = "Hearthstag", gate = "fire", map = Load(CinderwoodTools.MapPath), keeperLevel = KeeperKit.Level,
                helper = KeeperSim.Helper, helperAt = "springs",
                stops = new[] { "start", "firstGrass", "plaza", "springs", "log", "temple" } },
            new Leg { realm = "Lilymere", keeper = "Tidemother", gate = "water", map = Load(LilymereTools.MapPath), keeperLevel = TidemotherKit.Level,
                helper = "Kitelet", helperAt = "wind",
                stops = new[] { "start", "firstGrass", "stream", "village", "channel", "garden", "basin", "temple" } },
        };

        public static void Report() => Debug.Log(Run(1000, new[] { 1f, 1.5f, 2f }));

        /// <summary>The report for <paramref name="runs"/> runs per starter and each grass-wander factor.</summary>
        public static string Run(int runs, float[] wanders, int[] keeperBase = null, int[] helperExtra = null)
        {
            var hub = Load(HubPath);
            var legs = Order();
            for (int k = 0; k < legs.Count; k++)
                if (keeperBase != null && keeperBase[k] > 0)
                {
                    var l = legs[k];
                    l.keeperLevel = keeperBase[k];
                    legs[k] = l;
                }
            var prog = AssetDatabase.LoadAssetAtPath<ProgressionConfig>("Assets/_Project/Data/Battle/Progression.asset");
            var chart = KeeperSim.Chart;
            var log = new StringBuilder($"[RealmOrderSim] Galewick first, then Cinderwood, then Lilymere ({runs} runs per starter). Keepers: " +
                                        string.Join(", ", legs.Select((l, i) => $"{l.keeper} Lv {l.keeperLevel}+{i * ShardScaling.KeeperPerShard} at {i} shard(s)")) +
                                        $"; wild +{ShardScaling.WildPerShard} per shard; XP x{prog.rewardMultiplier}.\n" +
                                        "Party at each Keeper: the starter (fought every battle) + the realm's shield-breaking visitor (caught at its patch's level) " +
                                        "+ the latest other catch (0-1 per realm, from the trail's grass); catches are benched.\n" +
                                        (helperExtra != null ? $"Visitor levels +{string.Join("/", helperExtra)}.\n" : ""));
            if (hub == null || legs.Any(l => l.map == null) || prog == null || chart == null) return log.Append("Maps / progression / chart missing.").ToString();
            var hubPath = KeeperSim.Chain(hub, KeeperSim.Mark(hub, "start"), KeeperSim.Mark(hub, "firstGrass"), hub.gates.Find(g => g.id == "wind").Front(0));
            var paths = legs.Select(l => Chain(l.map, l.stops)).ToList();
            log.Append("Trail tiles: hub " + hubPath.Count + ", " + string.Join(", ", legs.Select((l, i) => $"{l.realm} {paths[i].Count} ({Grass(l.map, paths[i])} grass)")) + "\n");
            foreach (float wander in wanders)
            {
                log.Append($"\n**Grass x{wander:0.0}**\n| Keeper | Shards | Keeper Lv | Starter arrives (p10 / mean / p90) | Battles in the realm | Visitor Lv | Kilnpot | Pebbloop | Pufflet |\n|---|---|---|---|---|---|---|---|---|\n");
                var rows = new Stats[legs.Count];
                for (int k = 0; k < legs.Count; k++) rows[k] = new Stats { win = new Dictionary<string, float>() };
                var levels = new List<int>[legs.Count];
                for (int k = 0; k < legs.Count; k++) levels[k] = new List<int>();
                var battles = new float[legs.Count];
                var helperLevels = new float[legs.Count];
                foreach (var name in KeeperSim.Starters)
                {
                    var starterSpecies = KeeperSim.Load(name);
                    var wins = new int[legs.Count];
                    var rnd = new SeededRandom(17);
                    for (int run = 0; run < runs; run++)
                    {
                        var me = new IslingInstance(starterSpecies, 5);
                        // The tutorial battle and the first encounter (Lv 3, scripted), then the hub's grass to the wind gate.
                        Progression.AddXp(me, prog.Reward(3, false), prog);
                        Progression.AddXp(me, prog.Reward(3, false), prog);
                        int since = 0;
                        var caught = new List<IslingInstance>();
                        Walk(hub, hubPath, wander, 0, me, prog, rnd, ref since, null, 0);
                        for (int k = 0; k < legs.Count; k++)
                        {
                            var leg = legs[k];
                            int want = rnd.Next() < 0.5f ? 0 : 1;
                            battles[k] += Walk(leg.map, paths[k], wander, k * ShardScaling.WildPerShard, me, prog, rnd, ref since, caught, want);
                            levels[k].Add(me.level);
                            var party = new List<IslingInstance> { new IslingInstance(me.species, me.level) };
                            var help = Helper(leg, k * ShardScaling.WildPerShard + (helperExtra != null ? helperExtra[k] : 0), rnd);
                            helperLevels[k] += help != null ? help.level : 0;
                            if (help != null) party.Add(help);
                            for (int c = caught.Count - 1; c >= 0 && party.Count < 3; c--) party.Add(new IslingInstance(caught[c].species, caught[c].level));
                            var keeper = KeeperSim.Load(leg.keeper);
                            int kl = ShardScaling.KeeperLevel(leg.keeperLevel, k, ShardScaling.KeeperPerShard);
                            if (KeeperSim.Fight(party, new IslingInstance(keeper, kl), chart, 31 * run + k, out _) == BattleResult.Won) wins[k]++;
                        }
                    }
                    for (int k = 0; k < legs.Count; k++) rows[k].win[name] = wins[k] / (float)runs;
                }
                for (int k = 0; k < legs.Count; k++)
                {
                    var l = levels[k];
                    l.Sort();
                    log.Append($"| {legs[k].keeper} | {k} | {ShardScaling.KeeperLevel(legs[k].keeperLevel, k, ShardScaling.KeeperPerShard)} | " +
                               $"{l[l.Count / 10]} / {l.Average():0.0} / {l[l.Count * 9 / 10]} | {battles[k] / l.Count:0.0} | {legs[k].helper} {helperLevels[k] / l.Count:0.0} |");
                    foreach (var n in KeeperSim.Starters) log.Append($" {rows[k].win[n]:P0} |");
                    log.Append('\n');
                }
            }
            return log.ToString();
        }

        /// <summary>Step 14c tuning: Keeper base levels and visitor level bumps around the current ones, grass x1.5.</summary>
        public static void Sweep()
        {
            var log = new StringBuilder("[RealmOrderSim] Sweep (grass x1.5, 500 runs)\n");
            foreach (var (kb, he) in new[]
                     {
                         (new[] { 9, 10, 9 }, new[] { 0, 0, 0 }), (new[] { 9, 11, 8 }, new[] { 1, 0, 0 }), (new[] { 9, 11, 9 }, new[] { 2, 0, 2 }),
                         (new[] { 8, 12, 9 }, new[] { 1, 0, 2 }), (new[] { 9, 12, 8 }, new[] { 2, 0, 0 }), (new[] { 8, 11, 8 }, new[] { 2, 0, 2 }),
                     })
                log.Append($"\nKeepers base {string.Join("/", kb)}, visitors +{string.Join("/", he)}:").Append(Run(500, new[] { 1.5f }, kb, he).Split(new[] { "**Grass" }, System.StringSplitOptions.None)[1]);
            Debug.Log(log.ToString());
        }

        /// <summary>Hearthstag at one shard vs KeeperSim's parties (starter + Steambara + a fire common) by party level.</summary>
        public static void OneShard()
        {
            var keeper = KeeperSim.Load("Hearthstag");
            var log = new StringBuilder($"[RealmOrderSim] Hearthstag Lv {ShardScaling.KeeperLevel(KeeperKit.Level, 1, 2)} (1 shard), KeeperSim parties with Steambara:\n");
            foreach (int lv in new[] { 11, 12, 13 })
            {
                var row = KeeperSim.Run(keeper, ShardScaling.KeeperLevel(KeeperKit.Level, 1, 2), lv, 200, 1, KeeperSim.Load(KeeperSim.Helper));
                log.Append($"Lv {lv}: {string.Join(", ", row.Select(kv => $"{kv.Key} {kv.Value.WinRate:P0}"))}\n");
            }
            Debug.Log(log.ToString());
        }

        /// <summary>
        /// One walk: 3 grace steps after a battle, then 12.5% per tall-grass step (each tile <paramref name="wander"/> times).
        /// The first <paramref name="catches"/> wilds met are caught (no XP for a catch: it's weakened, then pinched), the rest beaten.
        /// </summary>
        static int Walk(WorldMap map, List<Vector2Int> path, float wander, int levelBonus, IslingInstance me, ProgressionConfig prog, SeededRandom rnd,
            ref int since, List<IslingInstance> caught, int catches)
        {
            int battles = 0, got = 0;
            foreach (var t in path)
            {
                if (map.At(t) != IslandMap.TallGrass) continue;
                int repeats = Mathf.FloorToInt(wander) + (rnd.Next() < wander - Mathf.Floor(wander) ? 1 : 0);
                for (int r = 0; r < repeats; r++)
                {
                    if (++since <= 3 || rnd.Next() >= 0.125f) continue;
                    since = 0;
                    var table = map.GrassTableAt(t);
                    if (table == null || !table.Roll(rnd, out var wild, out bool rare)) continue;
                    battles++;
                    int level = Mathf.Clamp(wild.level + levelBonus, 1, 30);
                    if (caught != null && got < catches && !rare)
                    {
                        got++;
                        caught.Add(new IslingInstance(wild.species, level));
                    }
                    Progression.AddXp(me, prog.Reward(level, rare), prog);
                }
            }
            return battles;
        }

        /// <summary>The realm's visitor at the level its patch rolls (one battle, caught), or null when the patch has none.</summary>
        static IslingInstance Helper(Leg leg, int levelBonus, SeededRandom rnd)
        {
            var species = KeeperSim.Load(leg.helper);
            var table = leg.map.GrassTableAt(KeeperSim.Mark(leg.map, leg.helperAt));
            if (species == null) return null;
            int level = 0;
            for (int i = 0; i < 40 && table != null; i++)
                if (table.Roll(rnd, out var w, out _) && w.species == species)
                {
                    level = w.level;
                    break;
                }
            if (level == 0) level = 6;
            return new IslingInstance(species, Mathf.Clamp(level + levelBonus, 1, 30));
        }

        static int Grass(WorldMap map, List<Vector2Int> path) => path.Count(t => map.At(t) == IslandMap.TallGrass);

        /// <summary>The trail through the named landmarks; a leg the plain search can't walk (mud, dry bed under a hand moment) crosses those too.</summary>
        internal static List<Vector2Int> Chain(WorldMap map, string[] stops)
        {
            var all = new List<Vector2Int>();
            for (int i = 1; i < stops.Length; i++)
            {
                var a = KeeperSim.Mark(map, stops[i - 1]);
                var b = KeeperSim.Mark(map, stops[i]);
                var leg = KeeperSim.PathBetween(map, a, b);
                if (leg.Count == 0 && a != b) leg = Across(map, a, b);
                if (leg.Count == 0 && a != b) Debug.LogWarning($"[RealmOrderSim] {map.name}: no path {stops[i - 1]} -> {stops[i]}.");
                all.AddRange(leg);
            }
            return all;
        }

        /// <summary>Like <see cref="KeeperSim.PathBetween"/>, mud and dry bed counting as walkable (they open once their moment is done).</summary>
        static List<Vector2Int> Across(WorldMap map, Vector2Int from, Vector2Int to)
        {
            var prev = new Dictionary<Vector2Int, Vector2Int> { [from] = from };
            var q = new Queue<Vector2Int>();
            q.Enqueue(from);
            var dirs = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                if (c == to) break;
                foreach (var d in dirs)
                {
                    var n = c + d;
                    if (prev.ContainsKey(n) || n.x < 0 || n.y < 0 || n.y >= map.Rows || n.x >= map.Columns) continue;
                    char ch = map.At(n);
                    if (n != to && !IslandMap.IsWalkable(ch) && ch != IslandMap.Mud && ch != IslandMap.DryBed) continue;
                    prev[n] = c;
                    q.Enqueue(n);
                }
            }
            var path = new List<Vector2Int>();
            if (!prev.ContainsKey(to)) return path;
            for (var c = to; c != from; c = prev[c]) path.Add(c);
            path.Reverse();
            return path;
        }
    }
}
