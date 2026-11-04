using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 14h: a Keeper's level follows the team at the door, tuned by sims (KeeperTeamSim), not picked by hand:
    /// <code>level = keeper + clamp(round(Follow * (team - keeper - Offset)) - PerMissing * (3 - fighters), -Down, +Up)</code>
    /// where <c>keeper</c> is its level with the shard scaling, <c>team</c> the average level of the team's three strongest
    /// and <c>fighters</c> how many of those three slots are filled. It follows a weak team down, eases off for a small team
    /// (a lone Lv 5 starter keeps a slim chance) and follows a strong one only up to <see cref="Up"/> levels, and only by
    /// <see cref="Follow"/> (under 1) of each level: levelling up always raises the win chance (the gap
    /// <c>team - level</c> never shrinks as the team grows). The Quick Journey keeps its fixed Keeper.
    /// </summary>
    public static class KeeperScaling
    {
        // Tuned with KeeperTeamSim (Report / Sweep / SweepOffsets), Nov 4.
        public const float Follow = 0.75f;
        public const float Offset = -2f;
        public const int Down = 6, Up = 3, PerMissing = 1;

        /// <summary>
        /// Each Keeper's offset (how many levels above the team's top-3 average it aims, before Follow): Tidemother's water
        /// shield and heals make it harder at the same level, so it aims lower.
        /// </summary>
        public static float OffsetFor(IslingSpecies keeper)
        {
            switch (keeper != null ? keeper.name : "")
            {
                case "Galeowl": return GaleowlOffset;
                case "Hearthstag": return HearthstagOffset;
                case "Tidemother": return TidemotherOffset;
                default: return Offset;
            }
        }

        public const float GaleowlOffset = -1.5f, HearthstagOffset = -1.5f, TidemotherOffset = -0.5f;

        /// <summary>How many of the three fighting slots the team fills.</summary>
        public static int Fighters(IList<IslingInstance> team)
        {
            int n = 0;
            if (team != null)
                for (int i = 0; i < team.Count && i < Party.Size; i++)
                    if (team[i] != null && team[i].species != null) n++;
            return Mathf.Min(3, n);
        }

        /// <summary>Average level of the (up to) three strongest Islings; 0 for an empty team.</summary>
        public static float TeamLevel(IList<IslingInstance> team)
        {
            if (team == null) return 0f;
            var levels = new List<int>();
            for (int i = 0; i < team.Count && i < Party.Size; i++)
                if (team[i] != null && team[i].species != null) levels.Add(team[i].level);
            if (levels.Count == 0) return 0f;
            levels.Sort((a, b) => b.CompareTo(a));
            int n = Mathf.Min(3, levels.Count);
            float sum = 0f;
            for (int i = 0; i < n; i++) sum += levels[i];
            return sum / n;
        }

        /// <summary>The Keeper's level for this team (pure; the parameters are for the sims' sweeps).</summary>
        public static int Level(int keeper, float team, int fighters, float follow = Follow, float offset = Offset, int down = Down, int up = Up,
            int perMissing = PerMissing)
        {
            if (team <= 0f || fighters <= 0) return keeper;
            int shift = Mathf.RoundToInt(follow * (team - keeper - offset)) - perMissing * (3 - Mathf.Min(3, fighters));
            return Mathf.Clamp(keeper + Mathf.Clamp(shift, -down, up), 1, 30);
        }

        public static int Level(int keeper, IList<IslingInstance> team, IslingSpecies species = null) =>
            Level(keeper, TeamLevel(team), Fighters(team), Follow, OffsetFor(species));
    }
}
