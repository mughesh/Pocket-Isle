using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Every species the save file can name (Data/Creatures/SpeciesRegistry). The stable id is the asset name, so
    /// renaming a species asset breaks old saves: don't. Filled by Tools > Pocket Isle > Set Up MR_Main (7 progression).
    /// </summary>
    [CreateAssetMenu(menuName = "Pocket Isle/Creatures/Species Registry", fileName = "SpeciesRegistry")]
    public class SpeciesRegistry : ScriptableObject
    {
        public List<IslingSpecies> species = new List<IslingSpecies>();

        Dictionary<string, IslingSpecies> _byId;

        public static string IdOf(IslingSpecies s) => s != null ? s.name : string.Empty;

        /// <summary>The species with this id, or null.</summary>
        public IslingSpecies Find(string id)
        {
            if (_byId == null || _byId.Count != species.Count)
            {
                _byId = new Dictionary<string, IslingSpecies>();
                foreach (var s in species)
                    if (s != null) _byId[s.name] = s;
            }
            return !string.IsNullOrEmpty(id) && _byId.TryGetValue(id, out var found) ? found : null;
        }

        void OnValidate() => _byId = null;
    }
}
