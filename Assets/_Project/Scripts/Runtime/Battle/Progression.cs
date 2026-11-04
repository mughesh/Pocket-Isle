using System;
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
    /// <see cref="IslingInstance.xp"/> is the progress toward the next level; a level-up carries the rest over.
    /// </summary>
    public static class Progression
    {
        /// <summary>
        /// Adds XP and applies every level-up it reaches (up to the cap). Each one raises max HP, and current HP by the
        /// same amount (a fainted creature stays at 0 until healed). Returns the level-ups in order.
        /// </summary>
        public static List<LevelUp> AddXp(IslingInstance c, int amount, ProgressionConfig config)
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
        public static int ShareFor(IslingInstance c, int reward, int fighters, ProgressionConfig config)
        {
            if (c == null || config == null) return 0;
            if (c.IsFainted && !config.faintedShare) return 0;
            return config.fullShare ? reward : Mathf.Max(1, reward / Mathf.Max(1, fighters));
        }

        /// <summary>True when the creature's species evolves and it has reached the level.</summary>
        public static bool CanEvolve(IslingInstance c)
        {
            if (c == null || c.species == null) return false;
            var next = c.species.evolvesTo;
            return next != null && c.species.evolveLevel > 0 && c.level >= c.species.evolveLevel;
        }

        /// <summary>
        /// Step 14h: a creature evolved (the battle pad or the palm): the creature, now its new form, and the form it was.
        /// The Journal and the save listen.
        /// </summary>
        public static event Action<IslingInstance, IslingSpecies> Evolved;

        /// <summary>Turns the creature into its next form. Keeps the HP ratio, level, XP and nickname.</summary>
        public static bool Evolve(IslingInstance c)
        {
            if (!CanEvolve(c)) return false;
            var from = c.species;
            float ratio = c.MaxHp > 0 ? c.currentHp / (float)c.MaxHp : 0f;
            c.species = c.species.evolvesTo;
            c.currentHp = c.currentHp <= 0 ? 0 : Mathf.Clamp(Mathf.RoundToInt(ratio * c.MaxHp), 1, c.MaxHp);
            Evolved?.Invoke(c, from);
            return true;
        }

        /// <summary>
        /// Step 14h: the pod that is due to evolve next (the first in pod order), or -1. "Due" is never saved: it is the
        /// level against the species' evolve level, so every cause counts (battle XP while benched, the Dev panel, a
        /// debug key, an Isling brought from the lodge already past its level) and a loaded save shows it again.
        /// </summary>
        public static int NextDue(IList<IslingInstance> pods)
        {
            if (pods == null) return -1;
            for (int i = 0; i < pods.Count && i < Party.Size; i++)
                if (CanEvolve(pods[i])) return i;
            return -1;
        }

        /// <summary>Step 14h: raises the creature to its evolve level if it is below (it becomes due). False when it can't evolve.</summary>
        public static bool MakeReady(IslingInstance c)
        {
            if (c == null || c.species == null || c.species.evolvesTo == null || c.species.evolveLevel <= 0) return false;
            if (c.level < c.species.evolveLevel)
            {
                c.level = c.species.evolveLevel;
                c.xp = 0;
                c.Heal();
            }
            return true;
        }
    }

    /// <summary>
    /// Step 14h: "the hand turned palm up" for the evolution moment (the finale's palm test: palm normal · up at least
    /// <see cref="upDot"/> for <see cref="landSeconds"/>). Pure, so tests can feed it synthetic hands.
    /// </summary>
    [Serializable]
    public class PalmUpTimer
    {
        [Tooltip("Palm normal · up at least this (palm up).")]
        public float upDot = 0.55f;
        [Tooltip("Palm up this long (s).")]
        public float landSeconds = 0.35f;

        float _up;

        public void Reset() => _up = 0f;

        /// <summary>One frame; true once the palm has been up long enough.</summary>
        public bool Step(float dt, bool tracked, float palmUp)
        {
            _up = tracked && palmUp >= upDot ? _up + dt : 0f;
            return _up >= landSeconds;
        }
    }
}
