using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Battle numbers in one place. Tuned so a level 5 vs level 5 fight takes 3-4 neutral hits each way
    /// (about 13 damage against 40 HP); a strong hit takes 2-3.
    /// </summary>
    public static class BattleMath
    {
        public const int MaxStage = 6;
        public const float RollMin = 0.9f, RollMax = 1.1f;

        /// <summary>Stats grow with level: x1.0 of base at level 5, x2.6 at level 25.</summary>
        static float Growth(int level) => 0.6f + 0.08f * Mathf.Max(1, level);

        public static int MaxHp(int baseHp, int level) => Mathf.Max(1, Mathf.RoundToInt(baseHp * Growth(level)));

        public static int Stat(int baseStat, int level) => Mathf.Max(1, Mathf.RoundToInt(baseStat * Growth(level)));

        /// <summary>Damage scale from the attacker's level: 0.33 at level 5.</summary>
        public static float LevelFactor(int level) => (2f * level + 10f) / 60f;

        /// <summary>Attack stage multiplier: +1 = x1.5, +2 = x2, -1 = x0.67.</summary>
        public static float StageMultiplier(int stage) => stage >= 0 ? (2f + stage) / 2f : 2f / (2f - stage);

        /// <summary>level factor x power x attack / defense x type multiplier x random roll (0.9-1.1). At least 1.</summary>
        public static int Damage(int level, int power, float attack, float defense, float typeMultiplier, float roll) =>
            Mathf.Max(1, Mathf.RoundToInt(LevelFactor(level) * power * attack / Mathf.Max(1f, defense) * typeMultiplier * roll));

        /// <summary>Equal speed = 70%. Faster helps, every failed try adds 15%. Always at least 10%.</summary>
        public static float FleeChance(int playerSpeed, int wildSpeed, int failedTries) =>
            Mathf.Clamp(0.3f + 0.4f * playerSpeed / Mathf.Max(1f, wildSpeed) + 0.15f * failedTries, 0.1f, 1f);

        /// <summary>
        /// Catch chance: low HP helps most (full HP about 19%, one HP about 95% at equal levels), rare halves it,
        /// every level the wild one is above the player's creature takes 5% off (and below adds 5%, up to +20%).
        /// Always 5-95%.
        /// </summary>
        public static float CatchChance(int hp, int maxHp, bool rare, int wildLevel, int playerLevel)
        {
            float hpShare = Mathf.Clamp01(hp / (float)Mathf.Max(1, maxHp));
            float levels = Mathf.Clamp(1f + 0.05f * (playerLevel - wildLevel), 0.6f, 1.2f);
            return Mathf.Clamp(0.95f * (1f - 0.8f * hpShare) * (rare ? 0.45f : 1f) * levels, 0.05f, 0.95f);
        }

        /// <summary>HP share at or below which the HP bar turns red and catching unlocks.</summary>
        public const float RedHpShare = 0.25f;

        /// <summary>Catching is locked until the wild creature's HP bar is red (or it is dazed).</summary>
        public static bool CatchUnlocked(int hp, int maxHp) => hp <= maxHp * RedHpShare;

        /// <summary>A pod thrown at a dazed (fainted) wild creature: a sure catch, rares 75%.</summary>
        public static float DazedCatchChance(bool rare) => rare ? 0.75f : 1f;

        /// <summary>How often the pod wobbles: 3 on a catch; on a miss 1-3, more the closer the roll was.</summary>
        public static int CatchWobbles(float roll, float chance)
        {
            if (roll < chance) return 3;
            float miss = (roll - chance) / Mathf.Max(1e-4f, 1f - chance);
            return miss < 0.25f ? 3 : miss < 0.6f ? 2 : 1;
        }
    }
}
