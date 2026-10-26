using System.Text;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 10a: Hearthstag for a player who did another realm first (1 shard held: Keeper +2 levels, wild Islings +3).
    /// Same fights as <see cref="KeeperSim"/>; the goal is about the same win odds as Cinderwood with 0 shards, with the
    /// party ~3 levels higher (the wilds they beat were 3 levels higher). Bridge: exec:Diorama.EditorTools.ShardSim.Report.
    /// </summary>
    public static class ShardSim
    {
        public static void Report()
        {
            var keeper = KeeperSim.Load("Hearthstag");
            if (keeper == null)
            {
                Debug.LogError("[ShardSim] Hearthstag species missing.");
                return;
            }
            var helper = KeeperSim.Load(KeeperSim.Helper);
            int baseLevel = KeeperSim.KeeperLevel;
            var log = new StringBuilder($"[ShardSim] Hearthstag base Lv {baseLevel}; +{ShardScaling.KeeperPerShard} per other shard, wild +{ShardScaling.WildPerShard} per shard.\n");
            log.Append("| Shards | Keeper Lv | Party Lv | Kilnpot | Pebbloop | Pufflet | All |\n|---|---|---|---|---|---|---|\n");
            foreach (int shards in new[] { 0, 1 })
            {
                int kl = ShardScaling.KeeperLevel(baseLevel, shards, ShardScaling.KeeperPerShard);
                foreach (int lv in shards == 0 ? new[] { 8, 9 } : new[] { 9, 10, 11, 12 })
                {
                    var row = KeeperSim.Run(keeper, kl, lv, 600, 1, helper);
                    int f = 0, w = 0;
                    log.Append($"| {shards} | {kl} | {lv} |");
                    foreach (var n in KeeperSim.Starters)
                    {
                        var o = row.TryGetValue(n, out var x) ? x : default;
                        f += o.fights;
                        w += o.wins;
                        log.Append($" {o.WinRate:P0} |");
                    }
                    log.Append($" {w / (float)Mathf.Max(1, f):P0} |\n");
                }
            }
            log.Append("Parties: the starter + Steambara + one random fire common (as in the 9f sim).");
            Debug.Log(log.ToString());
        }
    }
}
