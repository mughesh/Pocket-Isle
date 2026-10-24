using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Diorama.Tests
{
    public class PropTests
    {
        static PropRecipe Recipe(Vector2Int footprint, bool blocks)
        {
            var r = ScriptableObject.CreateInstance<PropRecipe>();
            r.footprint = footprint;
            r.blocksWalking = blocks;
            return r;
        }

        static WorldMap Map(string layout, string props, params PropLegend[] legend)
        {
            var m = ScriptableObject.CreateInstance<WorldMap>();
            m.SetLayout(layout);
            m.propLegend = new List<PropLegend>(legend);
            m.SetProps(props);
            return m;
        }

        [Test]
        public void LogBlocksBothTilesOfItsFootprint()
        {
            var log = Recipe(new Vector2Int(2, 1), true);
            var m = Map("PPPP\nGGGG", ".O..\n....", new PropLegend { key = "O", recipe = log });
            Assert.IsTrue(m.CanWalk(new Vector2Int(0, 0)));
            Assert.IsFalse(m.CanWalk(new Vector2Int(1, 0)));
            Assert.IsFalse(m.CanWalk(new Vector2Int(2, 0)));
            Assert.IsTrue(m.CanWalk(new Vector2Int(3, 0)));
            Assert.IsTrue(m.CanWalk(new Vector2Int(1, 1)));
        }

        [Test]
        public void TurnedLogRunsAlongTheColumn()
        {
            var log = Recipe(new Vector2Int(2, 1), true);
            var m = Map("GG\nGG\nGG", ".o\n..\n..", new PropLegend { key = "o", recipe = log, turned = true });
            Assert.IsFalse(m.CanWalk(new Vector2Int(1, 0)));
            Assert.IsFalse(m.CanWalk(new Vector2Int(1, 1)));
            Assert.IsTrue(m.CanWalk(new Vector2Int(0, 0)));
            Assert.IsTrue(m.CanWalk(new Vector2Int(1, 2)));
        }

        [Test]
        public void DecorPropsDontBlockAndTerrainStillDoes()
        {
            var pots = Recipe(Vector2Int.one, false);
            var m = Map("GW", "JJ", new PropLegend { key = "J", recipe = pots });
            Assert.IsTrue(m.CanWalk(new Vector2Int(0, 0)));
            Assert.IsFalse(m.CanWalk(new Vector2Int(1, 0)), "water stays unwalkable under a prop");
        }

        [Test]
        public void HotSpringIsNotWalkable()
        {
            var m = Map("GH", "..");
            Assert.IsFalse(m.CanWalk(new Vector2Int(1, 0)));
        }
    }
}
