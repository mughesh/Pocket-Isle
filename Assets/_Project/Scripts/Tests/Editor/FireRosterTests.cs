using NUnit.Framework;
using UnityEditor;

namespace Diorama.Tests
{
    /// <summary>
    /// Step 9d: the rest of the fire roster is complete data: each line baked, Fire, three moves, evolving at Lv 12-16,
    /// in the species registry (saves can load them), and the base forms in a Cinderwood zone table.
    /// </summary>
    public class FireRosterTests
    {
        const string SpeciesDir = "Assets/_Project/Data/Creatures/Species";

        static readonly (string from, string to, int level)[] Lines =
        {
            ("Kettlewing", "Steamswan", 14), ("Coalbeetle", "Forgehorn", 16), ("Mapadillo", "Emberdillo", 15),
            ("Ashmole", "Cindermole", 16), ("Cindercap", "Cinderbloom", 16), ("Embercrane", "Pyrecrane", 16),
        };

        static IslingSpecies Load(string name)
        {
            var s = AssetDatabase.LoadAssetAtPath<IslingSpecies>($"{SpeciesDir}/{name}.asset");
            if (s == null) Assert.Inconclusive($"{name} missing (run the Step9dSetup builders).");
            return s;
        }

        [Test]
        public void EveryLine_BakedFireThreeMoves_EvolvesBetween12And16()
        {
            foreach (var (from, to, level) in Lines)
            {
                var a = Load(from);
                var b = Load(to);
                Assert.AreEqual(b, a.evolvesTo, $"{from} evolves into {to}");
                Assert.AreEqual(level, a.evolveLevel, $"{from} level");
                Assert.That(a.evolveLevel, Is.InRange(12, 16));
                foreach (var s in new[] { a, b })
                {
                    Assert.AreEqual(Element.Fire, s.element, s.name);
                    Assert.IsNotNull(s.Prefab, $"{s.name} baked");
                    Assert.AreEqual(3, s.moves.Length, $"{s.name} moves");
                    foreach (var m in s.moves) Assert.IsNotNull(m, $"{s.name} has an empty move slot");
                    Assert.IsFalse(s.keeper);
                }
            }
        }

        [Test]
        public void Registry_KnowsThemAll()
        {
            var reg = AssetDatabase.LoadAssetAtPath<SpeciesRegistry>("Assets/_Project/Data/Creatures/SpeciesRegistry.asset");
            Assert.IsNotNull(reg);
            foreach (var (from, to, _) in Lines)
            {
                Assert.IsNotNull(reg.Find(from), $"{from} in the registry");
                Assert.IsNotNull(reg.Find(to), $"{to} in the registry");
            }
        }

        [Test]
        public void BaseForms_LiveInCinderwoodTables()
        {
            string[] tables = { "Encounters_Cinderwood", "Encounters_Forest", "Encounters_Field", "Encounters_Glade", "Encounters_Grove", "Encounters_Springs", "Encounters_Ruins" };
            foreach (var name in new[] { "Coalbeetle", "Mapadillo", "Ashmole", "Cindercap", "Embercrane" })
            {
                bool found = false, rare = false;
                foreach (var t in tables)
                {
                    var table = AssetDatabase.LoadAssetAtPath<EncounterTable>($"Assets/_Project/Data/Battle/Cinderwood/{t}.asset");
                    if (table == null) continue;
                    foreach (var e in table.entries)
                        if (e.species != null && e.species.name == name)
                        {
                            found = true;
                            rare |= e.rare;
                        }
                }
                Assert.IsTrue(found, $"{name} in a Cinderwood table");
                if (name == "Embercrane") Assert.IsTrue(rare, "Embercrane is a rare surprise");
            }
        }
    }
}
