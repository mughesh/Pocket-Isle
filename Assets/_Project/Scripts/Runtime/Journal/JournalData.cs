using System;
using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 14h: the Isling Journal's fixed data, baked at edit time (Tools > Pocket Isle > Journal > Bake Journal):
    /// every Isling in Journal order (Fire, Water, Wind; each line base first), its portrait cell in the atlas and where
    /// it lives. Loaded from Resources (<see cref="ResourcePath"/>), so nothing in the scene needs wiring.
    /// </summary>
    [CreateAssetMenu(menuName = "Pocket Isle/Journal Data", fileName = "JournalData")]
    public class JournalData : ScriptableObject
    {
        public const string ResourcePath = "JournalData";

        [Serializable]
        public struct Entry
        {
            public IslingSpecies species;
            [Tooltip("The form it evolves from (null for a base form).")]
            public IslingSpecies from;
            [Tooltip("Portrait cell in the atlas (row-major from the top left).")]
            public int cell;
            [Tooltip("\"Galewick, kite field\", or \"Rare\".")]
            public string hint;
        }

        public List<Entry> entries = new List<Entry>();
        [Tooltip("Every portrait, front 3/4 view on a transparent background; silhouettes are the same cells drawn black.")]
        public Texture2D atlas;
        [Tooltip("Diorama/Portrait with the atlas.")]
        public Material portraitMaterial;
        [Min(1)] public int columns = 8;
        [Min(1)] public int rows = 8;

        static JournalData _loaded;

        /// <summary>The baked asset (Resources), or null with an error once.</summary>
        public static JournalData Load()
        {
            if (_loaded != null) return _loaded;
            _loaded = Resources.Load<JournalData>(ResourcePath);
            if (_loaded == null && !_warned)
            {
                _warned = true;
                Debug.LogError("[Journal] No Resources/JournalData (Tools > Pocket Isle > Journal > Bake Journal): the Journal can't open.");
            }
            return _loaded;
        }

        static bool _warned;

        /// <summary>The atlas UV rect of a cell.</summary>
        public Rect CellRect(int cell)
        {
            float w = 1f / columns, h = 1f / rows;
            int c = cell % columns, r = cell / columns;
            return new Rect(c * w, 1f - (r + 1) * h, w, h);
        }

        public int IndexOf(IslingSpecies s)
        {
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].species == s) return i;
            return -1;
        }
    }
}
