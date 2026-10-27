using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Diorama.EditorTools;

namespace Diorama.Tests
{
    /// <summary>
    /// Step 10b: the hand moments' pure rules, fed synthetic hand tracks: wheel flick math, kite release threshold, gust
    /// detection, cloud puff thinning, the tower door rule; and the map gates (drawbridge, cloud bank).
    /// </summary>
    public class Step10bTests
    {
        static float Run(WheelSpin w, float seconds, float dt = 1f / 72f)
        {
            float before = w.Angle;
            for (float t = 0f; t < seconds; t += dt) w.Step(dt);
            return w.Angle - before;
        }

        [Test]
        public void Wheel_OneFlickTurnsAboutAQuarter_FasterTurnsMore_SlowDoesNothing()
        {
            var w = new WheelSpin();
            Assert.IsFalse(w.Flick(0.3f), "0.3 m/s is not a flick");
            Assert.IsTrue(w.Flick(0.5f));
            float turned = Run(w, 6f);
            Assert.AreEqual(Mathf.PI * 0.5f, turned, 0.1f, "a minimum flick turns a quarter");
            Assert.Less(Mathf.Abs(w.Velocity), 0.02f, "and stops");
            var fast = new WheelSpin();
            fast.Flick(1.0f);
            Assert.AreEqual(Mathf.PI, Run(fast, 6f), 0.15f, "twice as fast, half a turn");
            var back = new WheelSpin();
            back.Flick(-0.6f);
            Assert.Less(Run(back, 6f), 0f, "a flick the other way turns it backwards");
        }

        [Test]
        public void Wheel_ThreeFlicksInFourSecondsSolve_SpreadOutDoNot()
        {
            var w = new WheelSpin();
            bool solved = false;
            for (int i = 0; i < 3; i++)
            {
                w.Flick(0.8f);
                for (int f = 0; f < 72; f++) solved |= w.Step(1f / 72f); // a second apart
            }
            Assert.IsTrue(solved && w.Kept, "3 flicks within 4 s keep it turning");
            float spin = Run(w, 3f);
            Assert.Greater(spin, 5f, "kept: it turns on and on");

            var slow = new WheelSpin();
            bool any = false;
            for (int i = 0; i < 3; i++)
            {
                slow.Flick(0.8f);
                for (int f = 0; f < 72 * 3; f++) any |= slow.Step(1f / 72f); // three seconds apart
            }
            Assert.IsFalse(any || slow.Kept, "spread over 6 s is not enough");
            var toy = new WheelSpin { flicksNeeded = 0 };
            for (int i = 0; i < 5; i++) toy.Flick(1f);
            Assert.IsFalse(toy.Step(0.1f), "a toy wheel never solves");
        }

        [Test]
        public void Wheel_HoldRule_SolvesAfterKeepingASpeed()
        {
            var w = new WheelSpin { flicksNeeded = 0, holdSpeed = 1f, holdSeconds = 1f, drag = 1f };
            w.Flick(2.5f);
            bool solved = false;
            for (int f = 0; f < 72 * 2 && !solved; f++) solved = w.Step(1f / 72f);
            Assert.IsTrue(solved);
        }

        [Test]
        public void Kites_FlyAboveTheThresholdOrTossedUp_ElseDriftDown()
        {
            Assert.IsTrue(KiteRules.Flies(0.16f, 0f, 0.15f, 0.6f));
            Assert.IsFalse(KiteRules.Flies(0.10f, 0.2f, 0.15f, 0.6f), "too low, not tossed");
            Assert.IsTrue(KiteRules.Flies(0.07f, 0.8f, 0.15f, 0.6f), "a quick toss up counts");
            Assert.IsFalse(KiteRules.Flies(0.02f, 0.9f, 0.15f, 0.6f), "a toss from the grass itself doesn't");
            Assert.IsTrue(KiteRules.Flies(0.09f, 0f, 0.08f, 0.6f), "the lowered 8 cm threshold");
            var a = KiteRules.Slot(0, 0.3f, 0.08f, 0.15f);
            var b = KiteRules.Slot(1, 0.3f, 0.08f, 0.15f);
            var c = KiteRules.Slot(2, 0.3f, 0.08f, 0.15f);
            Assert.Less(a.x, b.x);
            Assert.Less(b.x, c.x);
            Assert.Less(a.y, c.y, "each next kite flies higher");
            Assert.GreaterOrEqual(a.y, 0.3f);
            Assert.AreEqual(3, KiteRules.Count(7));
            Assert.AreEqual(2, KiteRules.Count(KiteRules.Bit(0) | KiteRules.Bit(2)));
        }

        /// <summary>A palm moving from <paramref name="from"/> at <paramref name="velocity"/> for <paramref name="seconds"/>; returns the gusts.</summary>
        static int Sweep(GustTracker g, Vector3 from, Vector3 velocity, float seconds, bool open = true, bool over = true, float height = 0.12f)
        {
            int gusts = 0;
            const float dt = 1f / 72f;
            for (float t = 0f; t < seconds; t += dt)
            {
                var r = g.Feed(dt, from + velocity * t, open, over, height);
                if (r.HasValue) gusts++;
            }
            return gusts;
        }

        [Test]
        public void Gust_OpenHandSweepingFastOverTheIsland_OnePerCooldown()
        {
            var g = new GustTracker();
            Assert.AreEqual(1, Sweep(g, Vector3.zero, new Vector3(1f, 0f, 0f), 0.25f), "one sweep, one gust");
            g = new GustTracker();
            Assert.GreaterOrEqual(Sweep(g, Vector3.zero, new Vector3(1f, 0f, 0f), 0.7f), 2, "a long sweep repeats after the cooldown");
            Assert.AreEqual(0, Sweep(new GustTracker(), Vector3.zero, new Vector3(0.3f, 0f, 0f), 0.5f), "too slow");
            Assert.AreEqual(0, Sweep(new GustTracker(), Vector3.zero, new Vector3(1f, 0f, 0f), 0.5f, open: false), "a fist or a pinch");
            Assert.AreEqual(0, Sweep(new GustTracker(), Vector3.zero, new Vector3(1f, 0f, 0f), 0.5f, over: false), "not over the island");
            Assert.AreEqual(0, Sweep(new GustTracker(), Vector3.zero, new Vector3(1f, 0f, 0f), 0.5f, height: 0.4f), "too high above it");
            Assert.AreEqual(0, Sweep(new GustTracker(), Vector3.zero, new Vector3(0f, 1.2f, 0f), 0.5f), "straight up isn't a sweep");
        }

        [Test]
        public void Gust_DirectionAndStrengthFollowTheHand_JitterDoesNotCount()
        {
            var g = new GustTracker();
            GustTracker.Gust? first = null;
            const float dt = 1f / 72f;
            for (float t = 0f; t < 0.3f && !first.HasValue; t += dt) first = g.Feed(dt, new Vector3(0f, 0f, -1.4f * t), true, true, 0.1f);
            Assert.IsTrue(first.HasValue);
            Assert.AreEqual(-1f, first.Value.direction.y, 0.05f, "toward -z");
            Assert.Greater(first.Value.strength, 0.7f, "1.4 m/s is strong");
            // Tracking jitter: +-3 mm every frame (worse than Quest hand tracking), no net motion.
            var j = new GustTracker();
            int n = 0;
            for (int f = 0; f < 144; f++)
                if (j.Feed(dt, new Vector3(f % 2 == 0 ? 0.003f : -0.003f, 0f, 0f), true, true, 0.1f).HasValue) n++;
            Assert.AreEqual(0, n, "jitter is smoothed away");
        }

        [Test]
        public void Clouds_ThreeGoodGustsClearAPuff_WeakOnesDont_HoldClearsSlowly()
        {
            var bank = new CloudBank(3);
            Assert.IsFalse(bank.Gust(0, 0.1f), "a weak gust does nothing");
            Assert.AreEqual(1f, bank.thickness[0]);
            int gusts = 0;
            while (!bank.Gone(0) && gusts < 10)
            {
                bank.Gust(0, 0.75f);
                gusts++;
            }
            Assert.That(gusts, Is.InRange(2, 4), "about three good gusts per puff");
            Assert.AreEqual(1, bank.Mask);
            float held = 0f;
            while (!bank.Gone(1) && held < 10f)
            {
                bank.Hold(1, 0.1f);
                held += 0.1f;
            }
            Assert.That(held, Is.InRange(1.5f, 4f), "look + pinch-hold blows it away slowly");
            Assert.IsFalse(bank.AllGone);
            bank.Gust(2, 1f);
            bank.Gust(2, 1f);
            bank.Gust(2, 1f);
            Assert.IsTrue(bank.AllGone);
            var restored = new CloudBank(3);
            restored.Restore(5);
            Assert.IsTrue(restored.Gone(0) && !restored.Gone(1) && restored.Gone(2));
        }

        [Test]
        public void Tower_OpensOnlyWithThreeKitesAtTheDoor()
        {
            Assert.IsFalse(TowerRules.Opens(2, true));
            Assert.IsFalse(TowerRules.Opens(3, false));
            Assert.IsTrue(TowerRules.Opens(3, true));
        }

        [Test]
        public void Map_DrawbridgeAndCloudBankBlockTheWay_UntilOpened()
        {
            var map = AssetDatabase.LoadAssetAtPath<WorldMap>(GalewickTools.MapPath);
            if (map == null) Assert.Inconclusive("Galewick map missing (Set Up MR_Main (10b Galewick hands)).");
            var bridge = GalewickTools.Tile(GalewickTools.CrossingXY);
            Assert.IsTrue(map.TryGetProp(map.PropAt(bridge.y, bridge.x), out var e) && e.recipe.name == "Drawbridge");
            Assert.IsFalse(map.CanWalk(bridge), "the raised drawbridge blocks the crossing");
            foreach (var c in GalewickTools.CloudXY.Concat(GalewickTools.CloudTwinXY)) Assert.IsFalse(map.CanWalk(GalewickTools.Tile(c)), "a cloud puff blocks the 2-wide ridge path (step 12b)");
            try
            {
                map.SetPropOpen(bridge, true);
                Assert.IsTrue(map.CanWalk(bridge), "lowered, it can be walked");
            }
            finally
            {
                map.ClearRuntimeChanges();
            }
            Assert.IsEmpty(GalewickTools.Check(map, null));
        }
    }
}
