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

        // ---- untangle (KnotPull), synthetic hand tracks in world metres ----

        static KnotPull Knot() => new KnotPull
        {
            center = Vector3.zero, endA = new Vector3(-0.035f, 0f, 0f), endB = new Vector3(0.035f, 0f, 0f),
            grabRadius = 0.035f, twoHandPull = 0.12f, oneHandPull = 0.15f,
        };

        static KnotPull.Hand Pinch(Vector3 p, bool started = false) => new KnotPull.Hand(true, started, p);
        static readonly KnotPull.Hand Open = new KnotPull.Hand(false, false, Vector3.zero);

        /// <summary>Both hands pinch an end each and pull apart in steps of 1 cm per hand: it snaps past 12 cm apart.</summary>
        [Test]
        public void Untangle_TwoHands_SnapPastTwelveCm()
        {
            var k = Knot();
            Assert.IsFalse(k.Tick(Pinch(k.endA, true), Pinch(k.endB, true)));
            Assert.IsTrue(k.BothHeld);
            int steps = 0;
            for (int i = 1; i <= 10 && !k.Snapped; i++)
            {
                var l = k.endA + Vector3.left * (0.0101f * i);
                var r = k.endB + Vector3.right * (0.0101f * i);
                if (k.Tick(Pinch(l), Pinch(r))) steps = i;
            }
            Assert.IsTrue(k.Snapped);
            Assert.IsTrue(k.SnappedTwoHanded);
            Assert.AreEqual(6, steps, "6 cm each = 12 cm apart");
        }

        [Test]
        public void Untangle_TwoHands_ElevenCmIsNotEnough()
        {
            var k = Knot();
            k.Tick(Pinch(k.endA, true), Pinch(k.endB, true));
            k.Tick(Pinch(k.endA + Vector3.left * 0.055f), Pinch(k.endB + Vector3.right * 0.055f));
            Assert.IsFalse(k.Snapped);
            Assert.AreEqual(11f / 12f, k.Stretch, 0.01f);
        }

        [Test]
        public void Untangle_OneHand_SnapsPastFifteenCmAway()
        {
            var k = Knot();
            k.Tick(Open, Pinch(k.endB, true));
            Assert.AreEqual(1, k.Held(1));
            Assert.IsFalse(k.Tick(Open, Pinch(k.endB + new Vector3(0.14f, 0f, 0f))));
            Assert.IsTrue(k.Tick(Open, Pinch(k.endB + new Vector3(0.151f, 0f, 0f))));
            Assert.IsFalse(k.SnappedTwoHanded);
        }

        [Test]
        public void Untangle_LettingGo_DropsTheEnd_AndAPinchFarAwayTakesNothing()
        {
            var k = Knot();
            k.Tick(Pinch(new Vector3(0.2f, 0f, 0f), true), Open);
            Assert.IsFalse(k.AnyHeld, "pinch 16 cm away");
            k.Tick(Pinch(k.endA, true), Open);
            Assert.AreEqual(0, k.Held(0));
            k.Tick(Open, Open);
            Assert.IsFalse(k.AnyHeld);
            Assert.AreEqual(0f, k.Stretch);
        }

        [Test]
        public void Untangle_AHeldPinch_DoesNotGrabWithoutAStart_AndBusyHandsAreIgnored()
        {
            var k = Knot();
            k.Tick(Pinch(k.endA), Open);
            Assert.IsFalse(k.AnyHeld, "no pinch start");
            k.Tick(new KnotPull.Hand(true, true, k.endA, busy: true), Open);
            Assert.IsFalse(k.AnyHeld, "the hand holds the island");
        }

        [Test]
        public void Untangle_BothHandsOnOneEnd_SecondTakesTheOther()
        {
            var k = Knot();
            k.Tick(Pinch(k.endA, true), Open);
            k.Tick(Pinch(k.endA), Pinch(new Vector3(0.005f, 0f, 0f), true));
            Assert.AreEqual(0, k.Held(0));
            Assert.AreEqual(1, k.Held(1), "end A is taken: the right hand gets end B (within reach)");
        }

        [Test]
        public void Untangle_Loosen_SnapsOnce()
        {
            var k = Knot();
            Assert.IsTrue(k.Loosen());
            Assert.IsFalse(k.Loosen());
            Assert.IsFalse(k.Tick(Pinch(k.endA, true), Pinch(k.endB, true)));
        }

        [Test]
        public void Untangle_KnotMask_ThreeKnotsSolve()
        {
            Assert.AreEqual(7, KeeperUntangle.AllKnots);
            int mask = 0;
            foreach (int i in new[] { 2, 0, 1 }) mask |= 1 << i;
            Assert.AreEqual(KeeperUntangle.AllKnots, mask);
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
