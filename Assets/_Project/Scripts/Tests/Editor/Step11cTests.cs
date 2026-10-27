using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Diorama.EditorTools;

namespace Diorama.Tests
{
    /// <summary>
    /// Step 11c: the water roster (evolutions, three moves with a Water attack), Lilymere's tables (mostly water, the rare
    /// Bottlewhale only in the pond), Tidemother (a Water Keeper whose shield breaks to Wind) and the lake temple's return
    /// gate (a prop and a link home).
    /// </summary>
    public class Step11cTests
    {
        static IslingSpecies Load(string name) => AssetDatabase.LoadAssetAtPath<IslingSpecies>($"Assets/_Project/Data/Creatures/Species/{name}.asset");

        [Test]
        public void WaterRoster_EvolvesAtSixteen_RareAtEighteen_ThreeMovesWithAWaterAttack()
        {
            foreach (var (from, to, level) in new[] { ("Marimoss", "Marimound", 16), ("Drizzcan", "Rainurn", 16), ("Lilyvole", "Lotusvole", 16),
                         ("Sailfold", "Galleyfold", 16), ("Bottlewhale", "Lighthousewhale", 18) })
            {
                var s = Load(from);
                Assert.IsNotNull(s, from);
                Assert.AreEqual(Element.Water, s.element, from);
                Assert.IsNotNull(s.evolvesTo, from);
                Assert.AreEqual(to, s.evolvesTo.name, from);
                Assert.AreEqual(level, s.evolveLevel, from);
                foreach (var x in new[] { s, s.evolvesTo })
                {
                    Assert.AreEqual(3, x.moves.Length, x.name);
                    Assert.IsTrue(System.Array.Exists(x.moves, m => m != null && m.element == Element.Water && m.kind == MoveKind.Attack), $"{x.name}: a Water attack");
                    Assert.IsNotNull(x.Prefab, $"{x.name} baked");
                }
            }
        }

        [Test]
        public void LilymereTables_MostlyWater_BottlewhaleOnlyRareInThePond()
        {
            var map = AssetDatabase.LoadAssetAtPath<WorldMap>(LilymereTools.MapPath);
            Assert.IsNotNull(map);
            float water = 0f, all = 0f;
            foreach (var z in map.zoneLegend)
            {
                if (z.grassTable == null) continue;
                foreach (var e in z.grassTable.entries)
                {
                    all += e.weight;
                    if (e.species.element == Element.Water) water += e.weight;
                    if (e.species.name == "Bottlewhale")
                    {
                        Assert.AreEqual("pond", z.id, "Bottlewhale only in the rare pond");
                        Assert.IsTrue(e.rare);
                    }
                }
            }
            Assert.Greater(water / all, 0.6f, "about 70% water");
            Assert.Less(water / all, 0.85f, "with wind visitors");
        }

        [Test]
        public void Tidemother_WaterKeeper_ShieldBreaksToWind()
        {
            var s = Load("Tidemother");
            Assert.IsNotNull(s);
            Assert.IsTrue(s.keeper);
            Assert.AreEqual(Element.Water, s.element);
            Assert.AreEqual(Element.Wind, s.keeperBreaksWith);
            Assert.IsTrue(System.Array.Exists(s.moves, m => m != null && m.effect == SupportEffect.Shield), "Lily Shell");
            Assert.AreEqual(9, TidemotherKit.Level);
        }

        [Test]
        public void LakeTempleReturnGate_HasItsPropAndALinkHome()
        {
            var map = AssetDatabase.LoadAssetAtPath<WorldMap>(LilymereTools.MapPath);
            var gate = LilymereTools.Tile(LilymereTools.TempleGateXY);
            Assert.IsTrue(map.TryGetProp(map.PropAt(gate.y, gate.x), out var e) && e.recipe != null && e.recipe.name == "ReturnGate", "a ReturnGate prop by the temple");
            Assert.IsTrue(map.gates.Exists(g => g.tile == gate && g.to != null && g.id == "home"), "a link home");
            Assert.IsTrue(IslandMap.IsWalkable(map.At(gate.y + 1, gate.x)), "its front is walkable");
            Assert.AreEqual("shard.water", ShardHolder.WaterId);
        }
    }
}
