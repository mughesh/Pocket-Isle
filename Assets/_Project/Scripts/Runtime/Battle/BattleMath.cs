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

        public static int XpReward(int wildLevel, bool rare) => (5 + 4 * wildLevel) * (rare ? 2 : 1);
    }
}
