using NUnit.Framework;
using UnityEditor;
using Diorama.EditorTools;

namespace Diorama.Tests
{
    /// <summary>
    /// Step 10c: the wind roster and the starters' evolutions exist with three moves, the right element and evolution
    /// levels; Galewick's tables are mostly wind with fire visitors and a rare Zeppelope. Needs Set Up Wind Roster (10c).
    /// </summary>
    public class Step10cTests
    {
        static IslingSpecies Load(string name) => AssetDatabase.LoadAssetAtPath<IslingSpecies>($"Assets/_Project/Data/Creatures/Species/{name}.asset");

        [Test]
        public void WindRoster_FiveLinesWithThreeMovesAndAWindAttack()
        {
            if (Load("Kitelet") == null) Assert.Inconclusive("Wind roster not written (Tools > Pocket Isle > Set Up Wind Roster (10c)).");
            var lines = new[] { ("Kitelet", "Kitewyrm", 16), ("Whirlby", "Windwhirl", 16), ("Samarling", "Samaroak", 16), ("Vanehare", "Compasshare", 16), ("Zeppelope", "Skyshiplope", 18) };
            foreach (var (baseName, evoName, level) in lines)
            {
                var s = Load(baseName);
                var e = Load(evoName);
                Assert.IsNotNull(s, baseName);
                Assert.IsNotNull(e, evoName);
                Assert.AreEqual(e, s.evolvesTo, $"{baseName} evolves into {evoName}");
                Assert.AreEqual(level, s.evolveLevel, baseName);
                foreach (var x in new[] { s, e })
                {
                    Assert.AreEqual(Element.Wind, x.element, x.name);
                    Assert.AreEqual(3, x.moves.Length, $"{x.name} has three moves");
                    Assert.IsTrue(System.Array.Exists(x.moves, m => m != null && m.element == Element.Wind && m.kind == MoveKind.Attack), $"{x.name} has a Wind attack");
                    Assert.IsNotNull(x.recipe, $"{x.name} recipe");
                }
            }
        }

        [Test]
        public void Starters_PuffletAndPebbloopEvolveAtFourteenLikeKilnpot()
        {
            var nim = Load("Nimbulpaca");
            var river = Load("Riverloom");
            if (nim == null || river == null) Assert.Inconclusive("Starter evolutions not written yet.");
            Assert.AreEqual(nim, Load("Pufflet").evolvesTo);
            Assert.AreEqual(14, Load("Pufflet").evolveLevel);
            Assert.AreEqual(river, Load("Pebbloop").evolvesTo);
            Assert.AreEqual(14, Load("Pebbloop").evolveLevel);
            Assert.AreEqual(Load("Kilnpot").evolveLevel, Load("Pufflet").evolveLevel, "same level as Kilnpot -> Kilnhearth");
            Assert.AreEqual(Element.Wind, nim.element);
            Assert.AreEqual(Element.Water, river.element);
        }

        [Test]
        public void Galeowl_IsAWindKeeperWhoseWallBreaksToFire()
        {
            var g = Load("Galeowl");
            if (g == null) Assert.Inconclusive("Galeowl not written (Tools > Pocket Isle > Write Galeowl (10d part one)).");
            Assert.IsTrue(g.keeper);
            Assert.AreEqual(Element.Wind, g.element);
            Assert.AreEqual(Element.Fire, g.keeperBreaksWith);
            Assert.AreEqual(Element.Water, Load("Hearthstag").keeperBreaksWith, "Hearthstag unchanged");
            Assert.AreEqual(3, g.moves.Length);
            Assert.AreEqual(MoveKind.Support, g.moves[2].kind, "Gust Wall");
            Assert.AreEqual(11, ShardScaling.KeeperLevel(GaleowlKit.Level, 1, 2), "Lv 11 at one other shard");
        }

        [Test]
        public void GalewickTables_MostlyWind_FireVisitors_RareZeppelope()
        {
            var map = AssetDatabase.LoadAssetAtPath<WorldMap>(GalewickTools.MapPath);
            if (map == null || Load("Kitelet") == null) Assert.Inconclusive("Galewick or the wind roster missing.");
            float wind = 0f, fire = 0f, total = 0f;
            bool zep = false;
            foreach (var z in map.zoneLegend)
            {
                if (z.grassTable == null) continue;
                foreach (var e in z.grassTable.entries)
                {
                    total += e.weight;
                    if (e.species.element == Element.Wind) wind += e.weight;
                    if (e.species.element == Element.Fire) fire += e.weight;
                    if (e.species.name == "Zeppelope")
                    {
                        zep = true;
                        Assert.IsTrue(e.rare);
                        Assert.AreEqual("rare", z.id);
                    }
                }
            }
            Assert.IsTrue(zep, "Zeppelope in the rare pocket");
            Assert.That(wind / total, Is.InRange(0.6f, 0.8f), "about 70% wind");
            Assert.That(fire / total, Is.InRange(0.15f, 0.35f), "fire visitors");
        }
    }
}
