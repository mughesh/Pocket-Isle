using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 14h: tunes <see cref="KeeperScaling"/> against win-chance targets (bridge: exec:Diorama.EditorTools.KeeperTeamSim.Report /
    /// Sweep). For each Keeper at its level in the normal order (Galeowl first, no shards; Hearthstag with 1; Tidemother with 2)
    /// the teams at the door, built from the starters and the Islings on that realm's route (its grass, zones and water,
    /// rares left out), at levels relative to the Keeper (k = its level; Galeowl k = 8):
    /// <list type="bullet">
    /// <item>"1 Isling, Lv 5": the starter alone at k - 3 (target ~15%);</item>
    /// <item>"4 Islings, Lv 7-10": the starter + 3 route Islings at k - 1 .. k + 2 (target ~70%);</item>
    /// <item>"6 Islings, well levelled": the starter + 5 at k + 1 .. k + 3 (target at least 85%).</item>
    /// </list>
    /// Fights run the real battle rules with KeeperSim's player (best attack, shield breaker first, sensible switches).
    /// Monotonic check: a 4-Isling team from k - 3 to k + 6, one level at a time; the win chance must never drop.
    /// </summary>
    public static class KeeperTeamSim
    {
        public struct Scenario
        {
            public string name;
            public int count, lo, hi;
            public float target;
            public bool atLeast;
        }

        public static readonly Scenario[] Scenarios =
        {
            new Scenario { name = "1 Isling, Lv 5", count = 1, lo = -3, hi = -3, target = 0.15f },
            new Scenario { name = "4 Islings, Lv 7-10", count = 4, lo = -1, hi = 2, target = 0.70f },
            new Scenario { name = "6 Islings, well levelled", count = 6, lo = 1, hi = 3, target = 0.85f, atLeast = true },
        };

        public struct KeeperCase
        {
            public string name, realm;
            public int keeperLevel;
            public List<IslingSpecies> route;
        }

        public static List<KeeperCase> Cases()
        {
            var legs = RealmOrderSim.Order();
            var list = new List<KeeperCase>();
            for (int k = 0; k < legs.Count; k++)
            {
                var l = legs[k];
                list.Add(new KeeperCase
                {
                    name = l.keeper,
                    realm = l.realm,
                    keeperLevel = ShardScaling.KeeperLevel(l.keeperLevel, k, ShardScaling.KeeperPerShard),
                    route = Route(l.map),
                });
            }
            return list;
        }

        /// <summary>The non-rare, non-Keeper Islings of a realm's encounter tables.</summary>
        static List<IslingSpecies> Route(WorldMap map)
        {
            var set = new List<IslingSpecies>();
            void Add(EncounterTable t)
            {
                if (t == null || t.entries == null) return;
                foreach (var e in t.entries)
                    if (e.species != null && !e.rare && !e.species.keeper && !set.Contains(e.species)) set.Add(e.species);
            }
            if (map == null) return set;
            Add(map.grassTable);
            Add(map.waterTable);
            if (map.zoneLegend != null)
                foreach (var z in map.zoneLegend)
                    if (z.id != "rare") Add(z.grassTable);
            return set;
        }

        /// <summary>Win chance of a scenario against a Keeper with the given scaling knobs.</summary>
        public static float Win(KeeperCase c, int count, int lo, int hi, int runs, int seed, float follow, float offset, int down, int up,
            out float meanKeeperLevel, bool scale = true, int perMissing = KeeperScaling.PerMissing)
        {
            var chart = KeeperSim.Chart;
            var keeper = KeeperSim.Load(c.name);
            var starters = KeeperSim.Starters.Select(KeeperSim.Load).Where(s => s != null).ToList();
            var rnd = new SeededRandom(seed);
            int wins = 0;
            float levels = 0f;
            for (int run = 0; run < runs; run++)
            {
                int Lv() => Mathf.Clamp(c.keeperLevel + lo + Mathf.Min(hi - lo, (int)(rnd.Next() * (hi - lo + 1))), 1, 30);
                var team = new List<IslingInstance> { new IslingInstance(starters[run % starters.Count], Lv()) };
                while (team.Count < count && c.route.Count > 0) team.Add(new IslingInstance(c.route[(int)(rnd.Next() * c.route.Count) % c.route.Count], Lv()));
                int kl = scale ? KeeperScaling.Level(c.keeperLevel, KeeperScaling.TeamLevel(team), KeeperScaling.Fighters(team), follow, offset, down, up, perMissing) : c.keeperLevel;
                levels += kl;
                if (KeeperSim.Fight(team, new IslingInstance(keeper, kl), chart, seed * 7919 + run, out _) == BattleResult.Won) wins++;
            }
            meanKeeperLevel = levels / runs;
            return wins / (float)runs;
        }

        public static void Report() => Debug.Log(Run(KeeperScaling.Follow, float.NaN, KeeperScaling.Down, KeeperScaling.Up, 600, true, KeeperScaling.PerMissing));

        /// <summary>Each Keeper's own offset (the other knobs fixed at KeeperScaling's).</summary>
        public static void SweepOffsets()
        {
            var log = new StringBuilder("[KeeperTeamSim] Offsets (200 fights per cell: 1 Isling / 4 / 6):\n");
            foreach (var c in Cases())
                foreach (float off in new[] { -2.5f, -2f, -1.5f, -1f, -0.5f, 0f })
                {
                    var parts = new List<string>();
                    float score = 0f;
                    foreach (var s in Scenarios)
                    {
                        float w = Win(c, s.count, s.lo, s.hi, 200, 5, KeeperScaling.Follow, off, KeeperScaling.Down, KeeperScaling.Up, out _, true, KeeperScaling.PerMissing);
                        float err = s.atLeast ? Mathf.Max(0f, s.target - w) : Mathf.Abs(w - s.target);
                        score += err * err;
                        parts.Add($"{w:P0}");
                    }
                    log.Append($"  {c.name} offset {off}: {string.Join(" / ", parts)}  (score {score:0.000})\n");
                }
            Debug.Log(log.ToString());
        }

        /// <summary>The tables for one set of knobs (and, with <paramref name="before"/>, the old fixed Keeper beside it).</summary>
        public static string Run(float follow, float offsetAll, int down, int up, int runs, bool before, int perMissing)
        {
            var log = new StringBuilder($"[KeeperTeamSim] level = keeper + clamp(round({follow:0.##} x (team - keeper - offset)) - {perMissing} x (3 - fighters), -{down}, +{up}); " +
                                        $"team = average of the 3 strongest; {runs} fights per row.\n");
            foreach (var c in Cases())
            {
                float offset = float.IsNaN(offsetAll) ? KeeperScaling.OffsetFor(KeeperSim.Load(c.name)) : offsetAll;
                log.Append($"\n**{c.name}** ({c.realm}, Lv {c.keeperLevel} in the normal order, offset {offset:0.#}; route: {string.Join(", ", c.route.Select(s => s.name))})\n");
                log.Append("| Team at the door | Levels | Keeper Lv (mean) | Win | Target |" + (before ? " Fixed Keeper (before) |" : "") + "\n|---|---|---|---|---|" + (before ? "---|" : "") + "\n");
                foreach (var s in Scenarios)
                {
                    float w = Win(c, s.count, s.lo, s.hi, runs, 11, follow, offset, down, up, out float kl, true, perMissing);
                    string lv = s.lo == s.hi ? $"{c.keeperLevel + s.lo}" : $"{c.keeperLevel + s.lo}-{c.keeperLevel + s.hi}";
                    log.Append($"| {s.name} | {lv} | {kl:0.0} | {w:P0} | {(s.atLeast ? ">= " : "~")}{s.target:P0} |");
                    if (before) log.Append($" {Win(c, s.count, s.lo, s.hi, runs, 11, follow, offset, down, up, out _, false):P0} |");
                    log.Append('\n');
                }
                log.Append("Monotonic (4 Islings, all at one level): ");
                float last = -1f;
                bool ok = true;
                for (int d = -3; d <= 6; d++)
                {
                    float w = Win(c, 4, d, d, runs / 2, 23, follow, offset, down, up, out _, true, perMissing);
                    log.Append($"Lv {c.keeperLevel + d} {w:P0}  ");
                    if (w + 0.06f < last) ok = false; // sampling noise allowance (300 fights per level)
                    last = Mathf.Max(last, w);
                }
                log.Append(ok ? "(never drops)\n" : "(DROPS)\n");
            }
            return log.ToString();
        }

        /// <summary>Grid search: the knobs that come closest to the three targets for all three Keepers.</summary>
        public static void Sweep()
        {
            var cases = Cases();
            var results = new List<(float score, string knobs)>();
            int up = 3;
            foreach (float follow in new[] { 0.75f, 0.9f })
                foreach (float offset in new[] { -2f, -1f })
                    foreach (int down in new[] { 6, 9 })
                        foreach (int perMissing in new[] { 1, 2, 3, 4 })
                        {
                            float score = 0f;
                            var parts = new List<string>();
                            foreach (var c in cases)
                                foreach (var s in Scenarios)
                                {
                                    float w = Win(c, s.count, s.lo, s.hi, 100, 5, follow, offset, down, up, out _, true, perMissing);
                                    float err = s.atLeast ? Mathf.Max(0f, s.target - w) : Mathf.Abs(w - s.target);
                                    score += err * err;
                                    parts.Add($"{w:P0}");
                                }
                            results.Add((score, $"follow {follow}, offset {offset}, down {down}, up {up}, per missing {perMissing}: {string.Join(" ", parts)}"));
                        }
            var log = new StringBuilder("[KeeperTeamSim] Sweep (100 fights per cell; per Keeper: 1 Isling / 4 / 6), best first:\n");
            foreach (var r in results.OrderBy(r => r.score).Take(12)) log.Append($"  {r.score:0.000}  {r.knobs}\n");
            Debug.Log(log.ToString());
        }
    }
}
