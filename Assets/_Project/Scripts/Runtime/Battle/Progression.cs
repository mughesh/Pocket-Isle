using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>One level gained: the new level and how much max HP it added.</summary>
    public struct LevelUp
    {
        public int level;
        public int hpGain;
    }

    /// <summary>
    /// XP, levels and evolution rules, no Unity objects in the loop (EditMode-tested).
    /// <see cref="CreatureInstance.xp"/> is the progress toward the next level; a level-up carries the rest over.
    /// </summary>
    public static class Progression
    {
        /// <summary>
        /// Adds XP and applies every level-up it reaches (up to the cap). Each one raises max HP, and current HP by the
        /// same amount (a fainted creature stays at 0 until healed). Returns the level-ups in order.
        /// </summary>
        public static List<LevelUp> AddXp(CreatureInstance c, int amount, ProgressionConfig config)
        {
            var ups = new List<LevelUp>();
            if (c == null || c.species == null || config == null || amount <= 0) return ups;
            if (c.level >= config.levelCap)
            {
                c.xp = 0;
                return ups;
            }
            c.xp += amount;
            while (c.level < config.levelCap && c.xp >= config.XpToNext(c.level))
            {
                c.xp -= config.XpToNext(c.level);
                int before = c.MaxHp;
                c.level++;
                int gain = c.MaxHp - before;
                if (!c.IsFainted) c.currentHp = Mathf.Min(c.MaxHp, c.currentHp + gain);
                ups.Add(new LevelUp { level = c.level, hpGain = gain });
            }
            if (c.level >= config.levelCap) c.xp = 0;
            return ups;
        }

        /// <summary>The XP each of these creatures gets for a win (the config's share rules).</summary>
        public static int ShareFor(CreatureInstance c, int reward, int fighters, ProgressionConfig config)
        {
            if (c == null || config == null) return 0;
            if (c.IsFainted && !config.faintedShare) return 0;
            return config.fullShare ? reward : Mathf.Max(1, reward / Mathf.Max(1, fighters));
        }

        /// <summary>True when the creature's species evolves and it has reached the level.</summary>
        public static bool CanEvolve(CreatureInstance c)
        {
            if (c == null || c.species == null) return false;
            var next = c.species.evolvesTo;
            return next != null && c.species.evolveLevel > 0 && c.level >= c.species.evolveLevel;
        }

        /// <summary>Turns the creature into its next form. Keeps the HP ratio, level, XP and nickname.</summary>
        public static bool Evolve(CreatureInstance c)
        {
            if (!CanEvolve(c)) return false;
            float ratio = c.MaxHp > 0 ? c.currentHp / (float)c.MaxHp : 0f;
            c.species = c.species.evolvesTo;
            c.currentHp = c.currentHp <= 0 ? 0 : Mathf.Clamp(Mathf.RoundToInt(ratio * c.MaxHp), 1, c.MaxHp);
            return true;
        }
    }
}
