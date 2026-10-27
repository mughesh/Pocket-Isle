using NUnit.Framework;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>
    /// Step 11b: Lilymere's hand moments, logic only (synthetic hands): pads pushed into slots, the palm-down rain and the
    /// wiggle, bud timing, scoop (dip + lift) and pour (tip over the basin) or spill.
    /// </summary>
    public class Step11bTests
    {
        static PadDrift Pads() => new PadDrift(
            new[] { new Vector2(0.5f, 1f), new Vector2(8f, 0f), new Vector2(10f, 2f) },
            new[] { new Vector2(4f, 0f), new Vector2(4f, 1f), new Vector2(4f, 2f) },
            Rect.MinMaxRect(-6f, -0.5f, 14f, 2.5f)) { idle = 0f };

        static void Run(PadDrift d, float seconds, System.Action<int> locked = null)
        {
            for (float t = 0f; t < seconds; t += 0.02f)
            {
                int k = d.Tick(0.02f);
                if (k >= 0) locked?.Invoke(k);
            }
        }

        [Test]
        public void Pads_APushedPadDriftsWithDrag_ClicksIntoTheNearestSlot()
        {
            var d = Pads();
            int clicked = -1;
            d.Push(0, new Vector2(2.5f, 0f));
            Run(d, 6f, k => clicked = k);
            Assert.AreEqual(0, clicked, "pad 0 glided into a slot");
            Assert.AreEqual(1, d.slotOf[0], "the slot in its row");
            Assert.AreEqual(2, d.Mask);
            Assert.IsFalse(d.AllIn);
            // A soft push doesn't reach: drag stops it short.
            var e = Pads();
            e.Push(0, new Vector2(0.5f, 0f));
            Run(e, 4f);
            Assert.IsFalse(e.Locked(0), "too soft to reach the slot");
            Assert.Less(e.pos[0].x, 3f);
        }

        [Test]
        public void Pads_BounceOffTheBank_AndOffEachOther_LookPinchSteers_AllInLocks()
        {
            var d = Pads();
            d.Push(1, new Vector2(0f, -2.5f));
            Run(d, 0.6f);
            Assert.GreaterOrEqual(d.pos[1].y, d.bounds.yMin + d.radius - 1e-3f, "kept off the bank");
            Assert.Greater(d.vel[1].y, -0.01f, "bounced back");
            // Two pads meeting push apart.
            var e = new PadDrift(new[] { new Vector2(0f, 1f), new Vector2(2f, 1f) }, new[] { new Vector2(20f, 0f) }, Rect.MinMaxRect(-5f, -1f, 30f, 3f)) { idle = 0f };
            e.Push(0, new Vector2(2.5f, 0f));
            Run(e, 1.5f);
            Assert.GreaterOrEqual((e.pos[1] - e.pos[0]).magnitude, e.radius * 2f - 0.02f, "they don't overlap");
            Assert.Greater(e.pos[1].x, 2.05f, "the other one was knocked on");
            // Look + pinch takes each into a free slot.
            for (int i = 0; i < 3; i++)
            {
                d.Steer(i);
                Run(d, 4f);
            }
            Assert.IsTrue(d.AllIn, d.Mask.ToString());
            d.Push(0, new Vector2(2f, 2f));
            Run(d, 0.5f);
            Assert.IsTrue(d.Locked(0), "locked pads stay");
        }

        [Test]
        public void Rain_PalmDownFormsACloud_WiggleRainsHard_ShortDropsKeepIt()
        {
            Assert.IsTrue(RainCloud.PalmDown(Vector3.down, Vector3.up, 40f));
            Assert.IsTrue(RainCloud.PalmDown(Quaternion.Euler(35f, 0f, 0f) * Vector3.down, Vector3.up, 40f));
            Assert.IsFalse(RainCloud.PalmDown(Quaternion.Euler(60f, 0f, 0f) * Vector3.down, Vector3.up, 40f));
            Assert.IsFalse(RainCloud.PalmDown(Vector3.up, Vector3.up, 40f), "palm up is not a cloud");
            var c = new RainCloud();
            for (int i = 0; i < 25; i++) c.Feed(0.02f, 0, 0.1f);
            Assert.IsFalse(c.Raining, "0.5 s: still forming");
            for (int i = 0; i < 10; i++) c.Feed(0.02f, 0, 0.1f);
            Assert.IsTrue(c.Raining, "0.7 s: raining");
            Assert.IsFalse(c.Hard);
            for (int i = 0; i < 20; i++) c.Feed(0.02f, 0, 1.5f);
            Assert.IsTrue(c.Hard, "wiggling rains hard");
            for (int i = 0; i < 10; i++) c.Feed(0.02f, -1, 0f);
            Assert.IsTrue(c.Raining, "a 0.2 s tracking drop keeps it");
            c.Feed(0.02f, 0, 0f);
            for (int i = 0; i < 40; i++) c.Feed(0.02f, -1, 0f);
            Assert.IsFalse(c.Raining, "let go: it fades");
            Assert.AreEqual(-1, c.Holder);
        }

        [Test]
        public void Buds_FillInTwoSeconds_OneWhenHard_BloomOnce()
        {
            var g = new BudGarden(5);
            int blooms = 0;
            for (int i = 0; i < 95; i++) if (g.Rain(0, 0.02f, false)) blooms++;
            Assert.AreEqual(0, blooms, "1.9 s: not yet");
            for (int i = 0; i < 10; i++) if (g.Rain(0, 0.02f, false)) blooms++;
            Assert.AreEqual(1, blooms, "2 s: bloomed once");
            for (int i = 0; i < 52; i++) if (g.Rain(1, 0.02f, true)) blooms++;
            Assert.AreEqual(2, blooms, "about 1 s when hard");
            Assert.AreEqual(3, g.Mask);
            g.Restore(31);
            Assert.IsTrue(g.AllBloomed);
        }

        static WaterScoop.Hand H(float up, float above, bool water, bool basin = false) =>
            new WaterScoop.Hand { tracked = true, up = up, aboveWater = above, overWater = water, overBasin = basin };

        static ScoopState Feed(WaterScoop s, WaterScoop.Hand h) => s.Feed(0.02f, new[] { h, default });

        [Test]
        public void Scoop_DipAndLift_CarriesWater_TipOverTheBasinPours_TwoFillIt()
        {
            var s = new WaterScoop();
            Assert.AreEqual(ScoopState.Empty, Feed(s, H(0.9f, 0.05f, true)), "above the water: nothing");
            Assert.AreEqual(ScoopState.Empty, Feed(s, H(-0.9f, -0.01f, true)), "palm down in the water: nothing");
            Assert.AreEqual(ScoopState.Dipped, Feed(s, H(0.9f, -0.01f, true)), "cupped and in");
            Assert.AreEqual(ScoopState.Dipped, Feed(s, H(0.9f, 0.01f, true)));
            Assert.AreEqual(ScoopState.Held, Feed(s, H(0.9f, 0.03f, true)), "lifted out");
            Assert.AreEqual(ScoopState.Held, Feed(s, H(0.4f, 0.2f, false)), "carried, palm roughly up");
            Assert.AreEqual(ScoopState.Held, Feed(s, H(0.4f, 0.2f, false, true)), "over the basin, not tipped yet");
            Assert.AreEqual(ScoopState.Pouring, Feed(s, H(-0.6f, 0.2f, false, true)), "tipped over the basin");
            Assert.AreEqual(1, s.Pours);
            s.Reset();
            Feed(s, H(0.9f, -0.02f, true));
            Feed(s, H(0.9f, 0.05f, true));
            Feed(s, H(-0.8f, 0.1f, false, true));
            Assert.IsTrue(s.Full, "two scoops fill it");
        }

        [Test]
        public void Scoop_TippedAway_Spills_LostBriefly_Keeps_LookPour()
        {
            var s = new WaterScoop();
            Feed(s, H(0.9f, -0.02f, true));
            Feed(s, H(0.9f, 0.05f, true));
            Assert.AreEqual(ScoopState.Held, s.State);
            Assert.AreEqual(ScoopState.Held, s.Feed(0.2f, new[] { default(WaterScoop.Hand), default }), "lost 0.2 s: kept");
            Assert.AreEqual(ScoopState.Held, Feed(s, H(0.8f, 0.1f, false)));
            Assert.AreEqual(ScoopState.Spilled, Feed(s, H(-0.7f, 0.1f, false)), "tipped away from the basin");
            Assert.AreEqual(0, s.Pours);
            s.Reset();
            s.PourByItself();
            Assert.AreEqual(ScoopState.Pouring, s.State);
            Assert.AreEqual(1, s.Pours);
            s.Restore(1);
            Assert.AreEqual(ScoopState.Empty, s.State);
            Assert.AreEqual(1, s.Pours);
        }
    }
}
