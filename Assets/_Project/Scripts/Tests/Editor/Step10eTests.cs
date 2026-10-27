using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>
    /// Step 10e: one Keeper flow for every realm. The Keeper hall's defaults are Cinderwood's (its scene data stays as it
    /// was), the shard holder shares the Spirit's circle between held shards, saved ids per realm stay apart.
    /// </summary>
    public class Step10eTests
    {
        static readonly string[] Order = { ShardHolder.FireId, ShardHolder.WindId };

        [SetUp, TearDown]
        public void Clear() => PuzzleState.Restore(new List<SavedPuzzle>());

        [Test]
        public void KeeperHall_DefaultsAreCinderwoods()
        {
            var go = new GameObject("KeeperHall test");
            try
            {
                var k = go.AddComponent<KeeperHall>();
                Assert.AreEqual(KeeperHall.KeeperId, k.keeperId);
                Assert.AreEqual("cinder.keeper", k.keeperId);
                Assert.AreEqual(ShardHolder.FireId, k.ownShard);
                Assert.AreEqual("hearthstag", k.keeperTarget);
                Assert.AreEqual("fireShard", k.shardTarget);
                Assert.AreEqual("Cinderwood", k.realmName);
                Assert.AreEqual("Hearthstag blocks the way, crown ablaze!", string.Format(k.captionFormat, "Hearthstag"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void StatueSocket_DefaultsAreTheFireSocket()
        {
            var go = new GameObject("StatueSocket test");
            try
            {
                var s = go.AddComponent<StatueSocket>();
                Assert.AreEqual(PropSlot.Clay, s.rimSlot);
                Assert.IsTrue(s.drawsStatue);
                Assert.AreEqual("statueSocket", s.targetName);
                Assert.AreEqual("hub.shard.more", s.moreLine);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ShardHolder_OneShard_HasTheWholeCircle()
        {
            Assert.AreEqual(0, ShardHolder.CircleSlot(ShardHolder.FireId, Order, out int n));
            Assert.AreEqual(1, n, "flying in: it takes its place already");
            PuzzleState.SetStage(ShardHolder.FireId, 1);
            Assert.IsTrue(ShardHolder.HasFire);
            Assert.AreEqual(0, ShardHolder.CircleSlot(ShardHolder.FireId, Order, out n));
            Assert.AreEqual(1, n);
        }

        [Test]
        public void ShardHolder_TwoShards_ShareTheCircleEvenly()
        {
            PuzzleState.SetStage(ShardHolder.FireId, 1);
            PuzzleState.SetStage(ShardHolder.WindId, 1);
            Assert.AreEqual(0, ShardHolder.CircleSlot(ShardHolder.FireId, Order, out int n));
            Assert.AreEqual(2, n);
            Assert.AreEqual(1, ShardHolder.CircleSlot(ShardHolder.WindId, Order, out n));
            Assert.AreEqual(2, n);
        }

        [Test]
        public void ShardHolder_PlacedShards_LeaveTheCircle()
        {
            PuzzleState.SetStage(ShardHolder.FireId, 1);
            PuzzleState.Solve(ShardHolder.FireId, 2); // placed in the statue
            PuzzleState.SetStage(ShardHolder.WindId, 1);
            Assert.IsFalse(ShardHolder.Holds(ShardHolder.FireId));
            Assert.IsTrue(ShardHolder.Holds(ShardHolder.WindId));
            Assert.AreEqual(0, ShardHolder.CircleSlot(ShardHolder.WindId, Order, out int n));
            Assert.AreEqual(1, n);
        }

        [Test]
        public void ShardHolder_WindFirst_FireFlyingIn_TakesSlotZero()
        {
            PuzzleState.SetStage(ShardHolder.WindId, 1);
            Assert.AreEqual(0, ShardHolder.CircleSlot(ShardHolder.WindId, Order, out int n));
            Assert.AreEqual(1, n);
            Assert.AreEqual(0, ShardHolder.CircleSlot(ShardHolder.FireId, Order, out n));
            Assert.AreEqual(2, n);
        }

        [Test]
        public void KeeperIds_PerRealm_AreSavedApart()
        {
            PuzzleState.Solve("cinder.keeper", 1);
            PuzzleState.SetStage(ShardHolder.FireId, 1);
            Assert.IsFalse(PuzzleState.IsSolved("gw.keeper"));
            Assert.IsFalse(ShardHolder.Holds(ShardHolder.WindId));
            Assert.AreEqual(1, ShardScaling.Held(ShardHolder.WindId), "Galeowl grows with the fire shard");
            Assert.AreEqual(0, ShardScaling.Held(ShardHolder.FireId));
        }
    }
}
