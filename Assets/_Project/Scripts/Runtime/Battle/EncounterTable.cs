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
            public IslingSpecies species;
            [Min(1)] public int minLevel;
            [Min(1)] public int maxLevel;
            [Tooltip("Relative chance. Rares should add up to about 5% of the total.")]
            [Min(0f)] public float weight;
            [Tooltip("Gold sparkle when it appears.")]
            public bool rare;
        }

        public Habitat habitat;
        public Entry[] entries = Array.Empty<Entry>();

        /// <summary>
        /// Picks an entry by weight and a level in its range. False when the table is empty.
        /// Species not caught yet (<paramref name="isCaught"/> false) count <paramref name="uncaughtWeight"/> times.
        /// </summary>
        public bool Roll(SeededRandom rnd, out IslingInstance creature, out bool rare, Func<IslingSpecies, bool> isCaught = null, float uncaughtWeight = 1f)
        {
            creature = null;
            rare = false;
            // Step 9g: the Quick Journey has no rare surprises in Cinderwood (QuickJourneyConfig.rareWeight).
            float rareScale = DevCheats.RareBoost * QuickJourney.RareScale;
            float Weight(Entry e) => e.species == null ? 0f : e.weight * (isCaught != null && !isCaught(e.species) ? uncaughtWeight : 1f) * (e.rare ? rareScale : 1f);
            float total = 0f;
            foreach (var e in entries) total += Weight(e);
            if (total <= 0f) return false;

            float pick = rnd.Next() * total;
            foreach (var e in entries)
            {
                float w = Weight(e);
                if (w <= 0f) continue;
                pick -= w;
                if (pick >= 0f) continue;
                int lo = Mathf.Max(1, Mathf.Min(e.minLevel, e.maxLevel)), hi = Mathf.Max(lo, e.maxLevel);
                creature = new IslingInstance(e.species, lo + Mathf.Min(hi - lo, (int)(rnd.Next() * (hi - lo + 1))));
                rare = e.rare;
                return true;
            }
            return false;
        }
    }
}
