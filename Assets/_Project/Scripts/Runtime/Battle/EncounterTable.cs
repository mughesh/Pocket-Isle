using System;
using UnityEngine;

namespace Diorama
{
    public enum Habitat { Grass, Water }

    /// <summary>Which wild creatures a region's tall grass (or ponds) holds, and how often each shows up.</summary>
    [CreateAssetMenu(menuName = "Pocket Isle/Battle/Encounter Table", fileName = "EncounterTable")]
    public class EncounterTable : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public CreatureSpecies species;
            [Min(1)] public int minLevel;
            [Min(1)] public int maxLevel;
            [Tooltip("Relative chance. Rares should add up to about 5% of the total.")]
            [Min(0f)] public float weight;
            [Tooltip("Gold sparkle when it appears.")]
            public bool rare;
        }

        public Habitat habitat;
        public Entry[] entries = Array.Empty<Entry>();

        /// <summary>Picks an entry by weight and a level in its range. False when the table is empty.</summary>
        public bool Roll(SeededRandom rnd, out CreatureInstance creature, out bool rare)
        {
            creature = null;
            rare = false;
            float total = 0f;
            foreach (var e in entries)
                if (e.species != null) total += e.weight;
            if (total <= 0f) return false;

            float pick = rnd.Next() * total;
            foreach (var e in entries)
            {
                if (e.species == null || e.weight <= 0f) continue;
                pick -= e.weight;
                if (pick >= 0f) continue;
                int lo = Mathf.Max(1, Mathf.Min(e.minLevel, e.maxLevel)), hi = Mathf.Max(lo, e.maxLevel);
                creature = new CreatureInstance(e.species, lo + Mathf.Min(hi - lo, (int)(rnd.Next() * (hi - lo + 1))));
                rare = e.rare;
                return true;
            }
            return false;
        }
    }
}
