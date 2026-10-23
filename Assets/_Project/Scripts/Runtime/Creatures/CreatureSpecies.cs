using System;
using UnityEngine;

namespace Diorama
{
    public enum Element { Neutral, Fire, Water, Wind }

    [Serializable]
    public struct BaseStats
    {
        public int hp;
        public int attack;
        public int defense;
        public int speed;
    }

    /// <summary>
    /// A species: name, element, look (recipe), evolution and battle numbers (base stats and three moves:
    /// an element attack, a Neutral attack and a support).
    /// </summary>
    [CreateAssetMenu(menuName = "Pocket Isle/Creatures/Species", fileName = "Species")]
    public class CreatureSpecies : ScriptableObject
    {
        public string displayName;
        public Element element;
        public CreatureRecipe recipe;

        [Header("Evolution")]
        public CreatureSpecies evolvesTo;
        [Min(0)] public int evolveLevel;

        [Header("Battle")]
        [Tooltip("Stats at level 5; they grow with level (BattleMath).")]
        public BaseStats baseStats = new BaseStats { hp = 40, attack = 10, defense = 10, speed = 10 };
        [Tooltip("Element attack, Neutral attack, support.")]
        public MoveData[] moves = new MoveData[3];

        public GameObject Prefab => recipe != null ? recipe.bakedPrefab : null;
    }
}
