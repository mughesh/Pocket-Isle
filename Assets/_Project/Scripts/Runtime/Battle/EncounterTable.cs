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
        /// <summary>Step 29 E22: a caught species while the table has an uncaught one.</summary>
        public const float CaughtWeight = 0.3f;

        /// <summary>One entry's weight before the rare scale (pure; step 29 E22).</summary>
        public static float WeightOf(Entry e, Func<IslingSpecies, bool> isCaught, bool anyUncaught, float uncaughtWeight)
        {
            if (e.species == null) return 0f;
            if (e.rare || isCaught == null) return e.weight;
            bool caught = isCaught(e.species);
            return e.weight * (caught ? (anyUncaught ? CaughtWeight : 1f) : uncaughtWeight);
        }

        /// <summary>Each entry's chance (0..1) with this caught set (the report's odds; rare at its own weight).</summary>
        public float[] Odds(Func<IslingSpecies, bool> isCaught, float uncaughtWeight)
        {
            bool anyUncaught = false;
            if (isCaught != null)
                foreach (var e in entries)
                    if (e.species != null && !e.rare && e.weight > 0f && !isCaught(e.species)) anyUncaught = true;
            var w = new float[entries.Length];
            float total = 0f;
            for (int i = 0; i < entries.Length; i++) total += w[i] = WeightOf(entries[i], isCaught, anyUncaught, uncaughtWeight);
            for (int i = 0; i < w.Length; i++) w[i] = total > 0f ? w[i] / total : 0f;
            return w;
        }

        public bool Roll(SeededRandom rnd, out IslingInstance creature, out bool rare, Func<IslingSpecies, bool> isCaught = null, float uncaughtWeight = 1f)
        {
            creature = null;
            rare = false;
            // Step 9g: the Quick Journey has no rare surprises in Cinderwood (QuickJourneyConfig.rareWeight).
            float rareScale = DevCheats.RareBoost * QuickJourney.RareScale;
            // Step 29 E22 (playtests: the same Isling again and again): a species already caught weighs CaughtWeight while
            // the table still has an uncaught one; rare entries keep their weight (rare stays rare).
            bool anyUncaught = false;
            if (isCaught != null)
                foreach (var e in entries)
                    if (e.species != null && !e.rare && e.weight > 0f && !isCaught(e.species)) anyUncaught = true;
            float Weight(Entry e) => WeightOf(e, isCaught, anyUncaught, uncaughtWeight) * (e.rare ? rareScale : 1f);
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
