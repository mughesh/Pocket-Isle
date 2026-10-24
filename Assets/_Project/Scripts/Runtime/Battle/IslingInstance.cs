using System;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// One caught or wild Isling (the game's creatures): species, level, HP, XP, nickname. Plain data, serializable.
    /// Stats come from the species' base stats scaled by level (<see cref="BattleMath"/>).
    /// </summary>
    [Serializable]
    public class IslingInstance
    {
        public IslingSpecies species;
        [Min(1)] public int level = 1;
        [Tooltip("Current HP. Use Heal() after creating one in the Inspector.")]
        public int currentHp;
        [Min(0)] public int xp;
        public string nickname;

        public IslingInstance() { }

        public IslingInstance(IslingSpecies species, int level)
        {
            this.species = species;
            this.level = Mathf.Max(1, level);
            currentHp = MaxHp;
        }

        public string Name => !string.IsNullOrEmpty(nickname) ? nickname : species != null ? species.displayName : "?";
        public Element Element => species != null ? species.element : Element.Neutral;

        public int MaxHp => BattleMath.MaxHp(Base.hp, level);
        public int Attack => BattleMath.Stat(Base.attack, level);
        public int Defense => BattleMath.Stat(Base.defense, level);
        public int Speed => BattleMath.Stat(Base.speed, level);
        public bool IsFainted => currentHp <= 0;

        BaseStats Base => species != null ? species.baseStats : default;

        public void Heal() => currentHp = MaxHp;
    }
}
