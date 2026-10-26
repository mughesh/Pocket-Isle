using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 10d, part one (overnight Oct 27): Galeowl's model and species (<see cref="GaleowlKit"/>), baked, registered, a
    /// turnaround shot (Logs/BattleShots/10d_galeowl.png) and the Keeper sim per starter (<see cref="Sim"/>). The tower hall,
    /// untangling, victory, wind shard and restoration are part two. Bridge: exec:Diorama.EditorTools.Step10dSetup.Galeowl / Sim.
    /// </summary>
    public static class Step10dSetup
    {
        [MenuItem("Tools/Pocket Isle/Write Galeowl (10d part one)", priority = 63)]
        public static void Galeowl()
        {
            var s = GaleowlKit.Write();
            if (s == null) return;
            var r = CreatureBaker.Bake(s.recipe);
            IslingShots.Turnaround(s, $"{Step9cSetup.ShotDir}/10d_galeowl.png");
            Step7Setup.Registry();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Step10d] Galeowl: {r}");
        }

        static readonly string[] WindCommons = { "Kitelet", "Whirlby", "Samarling", "Vanehare" };

        /// <summary>
        /// Galeowl (Lv 9 at 0 shards, Lv 11 at 1) against parties of three: the starter, a Fire helper (Toastling, from the
        /// fire-visitor patch: fire breaks the Gust Wall) and a random wind common. Target 60-75% at the level a player
        /// arrives with (~Lv 9-10 at 0 shards).
        /// </summary>
        public static void Sim()
        {
            var keeper = KeeperSim.Load("Galeowl");
            var helper = KeeperSim.Load("Toastling");
            if (keeper == null || helper == null)
            {
                Debug.LogError("[Step10d] Galeowl or Toastling missing.");
                return;
            }
            var commons = new List<IslingSpecies>();
            foreach (var n in WindCommons)
            {
                var c = KeeperSim.Load(n);
                if (c != null) commons.Add(c);
            }
            var chart = KeeperSim.Chart;
            var log = new StringBuilder($"[Step10d] Galeowl sim (HP base {keeper.baseStats.hp}, atk {keeper.baseStats.attack}, def {keeper.baseStats.defense}); party = starter + Toastling + a wind common:\n");
            log.Append("| Shards | Galeowl Lv | Party Lv | Kilnpot | Pebbloop | Pufflet | All |\n|---|---|---|---|---|---|---|\n");
            var rnd = new SeededRandom(5);
            foreach (int shards in new[] { 0, 1 })
            {
                int kl = ShardScaling.KeeperLevel(GaleowlKit.Level, shards, ShardScaling.KeeperPerShard);
                foreach (int lv in shards == 0 ? new[] { 8, 9, 10 } : new[] { 10, 11, 12 })
                {
                    int f = 0, w = 0;
                    log.Append($"| {shards} | {kl} | {lv} |");
                    foreach (var starterName in KeeperSim.Starters)
                    {
                        var starter = KeeperSim.Load(starterName);
                        int wins = 0;
                        const int fights = 500;
                        for (int i = 0; i < fights; i++)
                        {
                            var common = commons.Count > 0 ? commons[(int)(rnd.Next() * commons.Count) % commons.Count] : helper;
                            var party = new List<IslingInstance> { new IslingInstance(starter, lv), new IslingInstance(helper, lv), new IslingInstance(common, lv) };
                            if (KeeperSim.Fight(party, new IslingInstance(keeper, kl), chart, 11 + i, out _) == BattleResult.Won) wins++;
                        }
                        f += fights;
                        w += wins;
                        log.Append($" {wins / (float)fights:P0} |");
                    }
                    log.Append($" {w / (float)Mathf.Max(1, f):P0} |\n");
                }
            }
            Debug.Log(log.ToString());
        }
    }
}
