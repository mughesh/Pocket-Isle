using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 10a: realms can be done in any order, so wild Islings and Keepers grow with the shards the player holds
    /// (+3 wild levels and +2 Keeper levels per shard, <see cref="ProgressionConfig"/>). A shard counts once it is taken
    /// (held in the rim holder or placed in the statue). The hub's grass never scales (the tutorial lives there), and a
    /// Keeper never counts its own realm's shard.
    /// </summary>
    public static class ShardScaling
    {
        /// <summary>Saved puzzle ids of the three shards.</summary>
        public static readonly string[] ShardIds = { "shard.fire", "shard.wind", "shard.water" };

        /// <summary>The scene's tuning (EncounterDirector sets it); defaults to 3 / 2 when unset (tests, tools).</summary>
        public static ProgressionConfig Config;

        public static int WildPerShard => Config != null ? Config.wildLevelsPerShard : 3;
        public static int KeeperPerShard => Config != null ? Config.keeperLevelsPerShard : 2;

        /// <summary>Shards taken so far (held or placed), not counting <paramref name="except"/>.</summary>
        public static int Held(string except = null)
        {
            int n = 0;
            foreach (var id in ShardIds)
                if (id != except && (PuzzleState.Stage(id) >= 1 || PuzzleState.IsSolved(id))) n++;
            return n;
        }

        /// <summary>A wild Isling's level on a map of <paramref name="realm"/> with <paramref name="held"/> shards (pure).</summary>
        public static int WildLevel(int level, string realm, int held, int perShard) =>
            realm == "stillisle" ? level : Mathf.Clamp(level + Mathf.Max(0, held) * perShard, 1, 30);

        /// <summary>A Keeper's level with <paramref name="held"/> other shards (pure).</summary>
        public static int KeeperLevel(int level, int held, int perShard) => Mathf.Clamp(level + Mathf.Max(0, held) * perShard, 1, 30);

        /// <summary>A wild level for the current shards.</summary>
        public static int WildLevel(int level, string realm) => WildLevel(level, realm, Held(), WildPerShard);

        /// <summary>A Keeper level for the current shards (its own realm's shard excluded).</summary>
        public static int KeeperLevel(int level, string ownShard) => KeeperLevel(level, Held(ownShard), KeeperPerShard);
    }
}
