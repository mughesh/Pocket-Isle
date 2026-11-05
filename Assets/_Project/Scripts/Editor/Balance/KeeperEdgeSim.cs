using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Nov 5: the Keeper's level and the win chance for many teams at the door (party size, levels, shards held), in the
    /// full game and in the Quick Journey, so odd teams (a dev-levelled party of six, a lone starter) are checked without
    /// playing to the Keeper. Tools > Pocket Isle > Balance > Keeper Edge Cases, or exec:Diorama.EditorTools.KeeperEdgeSim.Report.
    /// Writes Logs/KeeperEdgeSim.md.
    /// </summary>
    public static class KeeperEdgeSim
    {
        struct Team
        {
            public string name;
            public int count, level;
        }

        static readonly Team[] Teams =
        {
            new Team { name = "starter alone", count = 1, level = 5 },
            new Team { name = "2 friends", count = 2, level = 6 },
            new Team { name = "3 friends", count = 3, level = 7 },
            new Team { name = "4 friends", count = 4, level = 9 },
            new Team { name = "6 friends", count = 6, level = 10 },
            new Team { name = "6 friends", count = 6, level = 12 },
            new Team { name = "6 friends", count = 6, level = 16 },
            new Team { name = "6 friends", count = 6, level = 20 },
            new Team { name = "2 strong", count = 2, level = 16 },
        };

        const int Runs = 200;

        [MenuItem("Tools/Pocket Isle/Balance/Keeper Edge Cases", priority = 210)]
        public static void Report()
        {
            var quickCfg = AssetDatabase.LoadAssetAtPath<QuickJourneyConfig>("Assets/_Project/Data/QuickJourney.asset");
            int quickOffset = quickCfg != null ? quickCfg.keeperLevelOffset : 0;
            var chart = KeeperSim.Chart;
            var starters = KeeperSim.Starters.Select(KeeperSim.Load).Where(s => s != null).ToList();
            var legs = RealmOrderSim.Order();
            var sb = new StringBuilder();
            sb.Append($"# Keeper edge cases ({DateTime.Now:yyyy-MM-dd HH:mm})\n\n");
            sb.Append($"Win = {Runs} fights with the sim player. Quick offset {quickOffset}. Keeper level formula: KeeperScaling (follow {KeeperScaling.Follow}, up {KeeperScaling.Up}, down {KeeperScaling.Down}).\n\n");

            for (int k = 0; k < legs.Count; k++)
            {
                var leg = legs[k];
                var keeper = KeeperSim.Load(leg.keeper);
                var route = Route(leg.map);
                bool hasQuick = leg.realm == "Galewick";
                sb.Append($"## {leg.keeper} (base Lv {leg.keeperLevel})\n\n");
                sb.Append("| Team | Shards held | Full game: Keeper Lv / win | Full, up 5: Lv / win |" + (hasQuick ? " Quick Journey now: Lv / win | Quick with strong-team bump: Lv / win |" : "") + "\n");
                sb.Append("|---|---|---|---|" + (hasQuick ? "---|---|" : "") + "\n");
                foreach (var t in Teams)
                    for (int shards = 0; shards <= 2; shards++)
                    {
                        int baseLv = ShardScaling.KeeperLevel(leg.keeperLevel, shards, ShardScaling.KeeperPerShard);
                        var team = MakeTeam(t, starters, route, 0);
                        int full = KeeperScaling.Level(baseLv, team, keeper);
                        int up5 = KeeperScaling.Level(baseLv, KeeperScaling.TeamLevel(team), KeeperScaling.Fighters(team), KeeperScaling.Follow, KeeperScaling.OffsetFor(keeper), KeeperScaling.Down, 5);
                        string row = $"| {t.name}, Lv {t.level} | {shards} | Lv {full} / {Win(t, starters, route, keeper, full, chart):P0} | Lv {up5} / {Win(t, starters, route, keeper, up5, chart):P0} |";
                        if (hasQuick)
                        {
                            if (shards == 0)
                            {
                                int quick = QuickJourney.KeeperLevel(baseLv, true, true, quickCfg);
                                int follow = quick + QuickJourney.StrongTeamExtra(KeeperScaling.TeamLevel(team), true, true, quickCfg);
                                row += $" Lv {quick} / {Win(t, starters, route, keeper, quick, chart):P0} | Lv {follow} / {Win(t, starters, route, keeper, follow, chart):P0} |";
                            }
                            else row += " - | - |";
                        }
                        sb.Append(row + "\n");
                    }
                sb.Append('\n');
            }
            string path = "Logs/KeeperEdgeSim.md";
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText(path, sb.ToString());
            Debug.Log($"[KeeperEdgeSim] Wrote {path}.");
        }

        static List<IslingInstance> MakeTeam(Team t, List<IslingSpecies> starters, List<IslingSpecies> route, int run)
        {
            var rnd = new SeededRandom(1000 + run);
            var team = new List<IslingInstance> { new IslingInstance(starters[run % starters.Count], t.level) };
            while (team.Count < t.count && route.Count > 0) team.Add(new IslingInstance(route[(int)(rnd.Next() * route.Count) % route.Count], t.level));
            return team;
        }

        static float Win(Team t, List<IslingSpecies> starters, List<IslingSpecies> route, IslingSpecies keeper, int keeperLevel, TypeChart chart)
        {
            int wins = 0;
            for (int run = 0; run < Runs; run++)
                if (KeeperSim.Fight(MakeTeam(t, starters, route, run), new IslingInstance(keeper, keeperLevel), chart, 31 * 7919 + run, out _) == BattleResult.Won)
                    wins++;
            return wins / (float)Runs;
        }

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
    }
}
