using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Diorama.Tests
{
    public class ExplorerPathTests
    {
        static IslandMap Map(string layout)
        {
            var m = ScriptableObject.CreateInstance<IslandMap>();
            m.SetLayout(layout);
            return m;
        }

        [Test]
        public void StraightLineIsShortest()
        {
            var m = Map("PPPPP");
            var path = new List<Vector2Int>();
            Assert.IsTrue(ExplorerPath.Find(m, new Vector2Int(0, 0), new Vector2Int(4, 0), path));
            Assert.AreEqual(new[] { new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0), new Vector2Int(4, 0) }, path.ToArray());
        }

        [Test]
        public void WalksAroundAWall()
        {
            var m = Map("GRG\nGRG\nGGG");
            var path = new List<Vector2Int>();
            Assert.IsTrue(ExplorerPath.Find(m, new Vector2Int(0, 0), new Vector2Int(2, 0), path));
            Assert.AreEqual(6, path.Count);
            foreach (var t in path) Assert.IsTrue(m.CanWalk(t), $"{t} is not walkable");
            Assert.AreEqual(new Vector2Int(2, 0), path[path.Count - 1]);
        }

        [Test]
        public void BlockedOrUnreachableGoalFails()
        {
            var m = Map("GWG\nGWG");
            var path = new List<Vector2Int>();
            Assert.IsFalse(ExplorerPath.Find(m, new Vector2Int(0, 0), new Vector2Int(2, 0), path));
            Assert.IsFalse(ExplorerPath.Find(m, new Vector2Int(0, 0), new Vector2Int(1, 0), path));
            Assert.AreEqual(0, path.Count);
        }

        [Test]
        public void RespectsTheStepBudget()
        {
            var m = Map("GGGGGGGGGG");
            var path = new List<Vector2Int>();
            Assert.IsFalse(ExplorerPath.Find(m, new Vector2Int(0, 0), new Vector2Int(9, 0), path, 8));
            Assert.IsTrue(ExplorerPath.Find(m, new Vector2Int(0, 0), new Vector2Int(9, 0), path, 9));
        }
    }
}
