using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// XP and level tuning (Data/Battle/Progression.asset): the XP curve, the level cap and the win reward.
    /// Rules that use it live in <see cref="Progression"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Pocket Isle/Battle/Progression Config", fileName = "Progression")]
    public class ProgressionConfig : ScriptableObject
    {
        [Header("XP to the next level = a x level + b")]
        public int a = 6;
        public int b = 10;
        [Min(1)] public int levelCap = 30;

        [Header("Win reward = base + perLevel x wild level (x rare)")]
        public int rewardBase = 5;
        public int rewardPerLevel = 4;
        [Min(1f)] public float rareMultiplier = 2f;

        [Header("Sharing")]
        [Tooltip("Every creature that fought in the battle gets the full reward.")]
        public bool fullShare = true;
        [Tooltip("Creatures that fainted during the battle still get their share (cozy rule).")]
        public bool faintedShare = true;

        /// <summary>XP needed to go from <paramref name="level"/> to the next one.</summary>
        public int XpToNext(int level) => Mathf.Max(1, a * Mathf.Max(1, level) + b);

        /// <summary>XP a win (wild creature fainted or caught) pays each creature that fought.</summary>
        public int Reward(int wildLevel, bool rare) =>
            Mathf.Max(1, Mathf.RoundToInt((rewardBase + rewardPerLevel * Mathf.Max(1, wildLevel)) * (rare ? rareMultiplier : 1f)));

        /// <summary>0..1 progress toward the next level (1 at the cap).</summary>
        public float XpShare(IslingInstance c) =>
            c == null ? 0f : c.level >= levelCap ? 1f : Mathf.Clamp01(c.xp / (float)XpToNext(c.level));
    }
}
