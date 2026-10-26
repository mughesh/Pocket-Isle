using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>Step 9f data: Steambara, its tables, the explorer's budget, the five evolutions as full recipes.</summary>
    public class Step9fTests
    {
        const string Species = "Assets/_Project/Data/Creatures/Species";

        static IslingSpecies Load(string name)
        {
            var s = AssetDatabase.LoadAssetAtPath<IslingSpecies>($"{Species}/{name}.asset");
            if (s == null) Assert.Inconclusive($"{name} missing (run the Step9fSetup sections).");
            return s;
        }

        [Test]
        public void Steambara_IsWater_EvolvesAt14_HasAShieldBreakingWaterAttack()
        {
            var s = Load("Steambara");
            Assert.AreEqual(Element.Water, s.element);
            Assert.AreEqual(14, s.evolveLevel);
            Assert.IsNotNull(s.evolvesTo);
            Assert.AreEqual("Onsenbara", s.evolvesTo.name);
            foreach (var species in new[] { s, s.evolvesTo })
            {
                Assert.AreEqual(3, species.moves.Length, species.name);
                bool water = false;
                foreach (var m in species.moves)
                    if (m != null && m.kind == MoveKind.Attack && m.element == new KeeperRules().breaksWith) water = true;
                Assert.IsTrue(water, $"{species.name} has a Water attack");
            }
        }

        [Test]
        public void Steambara_AboutAThirdOfTheSprings_ATenthOfTheGrove()
        {
            Assert.That(Share("Encounters_Springs"), Is.InRange(0.3f, 0.4f));
            Assert.That(Share("Encounters_Grove"), Is.InRange(0.05f, 0.15f));
        }

        static float Share(string table)
        {
            var t = AssetDatabase.LoadAssetAtPath<EncounterTable>($"Assets/_Project/Data/Battle/Cinderwood/{table}.asset");
            if (t == null) t = FindTable(table);
            if (t == null) Assert.Inconclusive($"{table} missing.");
            float all = 0f, bara = 0f;
            foreach (var e in t.entries)
            {
                all += e.weight;
                if (e.species != null && e.species.name == "Steambara") bara += e.weight;
            }
            return all > 0f ? bara / all : 0f;
        }

        static EncounterTable FindTable(string name)
        {
            foreach (var guid in AssetDatabase.FindAssets($"{name} t:EncounterTable"))
            {
                var t = AssetDatabase.LoadAssetAtPath<EncounterTable>(AssetDatabase.GUIDToAssetPath(guid));
                if (t != null && t.name == name) return t;
            }
            return null;
        }

        [Test]
        public void Explorer_Baked_Under3kTriangles_WithItsMovingParts()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Creatures/Explorer.prefab");
            if (prefab == null) Assert.Inconclusive("Explorer not baked (run Step9fSetup.Explorer).");
            Assert.IsNotNull(prefab.GetComponent<ExplorerView>());
            int tris = 0;
            foreach (var f in prefab.GetComponentsInChildren<MeshFilter>(true))
                if (f.sharedMesh != null) tris += f.sharedMesh.triangles.Length / 3;
            Assert.That(tris, Is.LessThanOrEqualTo(3100), "about 3k triangles");
            foreach (var part in new[] { "Part_ExplorerArm_L", "Part_ExplorerArm_R", "Part_ExplorerLeg_L", "Part_ExplorerLeg_R", "Part_ExplorerHood", "Part_ExplorerLantern" })
            {
                bool found = false;
                foreach (var t in prefab.GetComponentsInChildren<Transform>(true))
                    if (t.name == part) found = true;
                Assert.IsTrue(found, part);
            }
        }

        [Test]
        public void FireEvolutions_AreFullRecipes()
        {
            foreach (var n in new[] { "Forgehorn", "Emberdillo", "Cindermole", "Cinderbloom", "Pyrecrane" })
            {
                var s = Load(n);
                Assert.IsNotNull(s.recipe, n);
                Assert.IsNull(s.recipe.baseRecipe, $"{n} is its own recipe, not a modifier");
                Assert.IsNotNull(s.recipe.bakedPrefab, $"{n} baked");
            }
        }
    }
}
