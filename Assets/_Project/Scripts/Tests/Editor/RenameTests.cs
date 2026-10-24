using NUnit.Framework;
using UnityEditor;

namespace Diorama.Tests
{
    /// <summary>Step 7b D: the Creature* gameplay types became Isling*; the assets made before must still load.</summary>
    public class RenameTests
    {
        [Test]
        public void SpeciesAssets_LoadAsIslingSpecies()
        {
            var guids = AssetDatabase.FindAssets("t:IslingSpecies", new[] { "Assets/_Project/Data/Creatures/Species" });
            Assert.GreaterOrEqual(guids.Length, 12, "every species asset is found by the new type");
            foreach (var g in guids)
            {
                var s = AssetDatabase.LoadAssetAtPath<IslingSpecies>(AssetDatabase.GUIDToAssetPath(g));
                Assert.IsNotNull(s, AssetDatabase.GUIDToAssetPath(g));
                Assert.IsFalse(string.IsNullOrEmpty(s.displayName), s.name);
                Assert.AreEqual(3, s.moves.Length, s.name);
            }
        }

        [Test]
        public void Registry_StillListsEverySpecies()
        {
            var registry = AssetDatabase.LoadAssetAtPath<SpeciesRegistry>("Assets/_Project/Data/Creatures/SpeciesRegistry.asset");
            Assert.IsNotNull(registry);
            Assert.GreaterOrEqual(registry.species.Count, 12);
            foreach (var s in registry.species) Assert.IsNotNull(s, "no missing references after the rename");
            Assert.IsNotNull(registry.Find("Kilnpot"), "save ids are the asset names, unchanged");
        }

        [Test]
        public void EncounterTables_KeepTheirSpecies()
        {
            foreach (var g in AssetDatabase.FindAssets("t:EncounterTable", new[] { "Assets/_Project/Data/Battle" }))
            {
                var t = AssetDatabase.LoadAssetAtPath<EncounterTable>(AssetDatabase.GUIDToAssetPath(g));
                foreach (var e in t.entries) Assert.IsNotNull(e.species, t.name);
            }
        }
    }
}
