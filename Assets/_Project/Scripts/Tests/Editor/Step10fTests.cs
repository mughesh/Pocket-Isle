using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Diorama.EditorTools;

namespace Diorama.Tests
{
    /// <summary>
    /// Step 10f: Cinderwood's hand moments, fed synthetic hand poses: cupping the village flame (two hands, one hand
    /// curled round it, hands that don't cup); the hollow's lantern chain timing; the smoke blocking the trail and the
    /// map connecting once it's gone; carrying the forge's ember palm-up and tipping it in.
    /// </summary>
    public class Step10fTests
    {
        static readonly Vector3 Flame = new Vector3(0f, 1f, 0.5f);
        const float Radius = 0.1f, Angle = 60f;

        static int Feed(CupMeter m, float seconds, int hands, bool wrapped = false, bool look = false)
        {
            int frames = 0;
            for (float t = 0f; t < seconds; t += 1f / 72f, frames++)
                if (m.Feed(1f / 72f, hands, wrapped, look)) return frames;
            return -1;
        }

        [Test]
        public void Cup_PalmsEitherSideTurnedIn_Face_TurnedAwayOrFar_DoNot()
        {
            // Both palms 6 cm either side of the flame, palms turned toward it.
            var left = Flame + new Vector3(-0.06f, 0f, 0f);
            var right = Flame + new Vector3(0.06f, 0f, 0f);
            Assert.IsTrue(CupRules.Facing(left, Vector3.right, Flame, Radius, Angle));
            Assert.IsTrue(CupRules.Facing(right, Vector3.left, Flame, Radius, Angle));
            Assert.IsTrue(CupRules.Facing(right, (Vector3.left + Vector3.up * 1.2f).normalized, Flame, Radius, Angle), "tilted 50 degrees still counts");
            Assert.IsFalse(CupRules.Facing(right, Vector3.right, Flame, Radius, Angle), "palm turned away");
            Assert.IsFalse(CupRules.Facing(right, Vector3.down, Flame, Radius, Angle), "palm flat, facing the floor beside it");
            Assert.IsFalse(CupRules.Facing(Flame + new Vector3(0.14f, 0f, 0f), Vector3.left, Flame, Radius, Angle), "14 cm away");
            // Right over it, palm down, counts (a palm turned toward the flame from above).
            Assert.IsTrue(CupRules.Facing(Flame + Vector3.up * 0.07f, Vector3.down, Flame, Radius, Angle));
        }

        [Test]
        public void Cup_OneHandCurledRound_Wraps_FlatOrOpenHand_DoesNot()
        {
            var palm = Flame + new Vector3(0.06f, 0f, 0f);
            // Fingers curled round: the middle tip just past the flame, 7 cm from the palm.
            var curled = Flame + new Vector3(-0.01f, 0.01f, 0.02f);
            Assert.IsTrue(CupRules.Wraps(palm, Vector3.left, curled, Flame, Radius, Angle));
            // Open hand pointing away: the tip 18 cm out.
            Assert.IsFalse(CupRules.Wraps(palm, Vector3.left, palm + new Vector3(0f, 0f, 0.18f), Flame, Radius, Angle));
            // Curled but turned away.
            Assert.IsFalse(CupRules.Wraps(palm, Vector3.right, curled, Flame, Radius, Angle));
        }

        [Test]
        public void CupMeter_TwoHandsTwoSeconds_OneHandThree_LookThree_LettingGoCools()
        {
            var two = new CupMeter();
            int f = Feed(two, 3f, 2);
            Assert.AreEqual(2f, f / 72f, 0.05f, "two hands: 2 s");
            Assert.IsTrue(two.Done);
            Assert.AreEqual(-1, Feed(two, 1f, 2), "done fires once");

            var one = new CupMeter();
            Assert.AreEqual(3f, Feed(one, 4f, 1, true) / 72f, 0.05f, "one hand curled round it: 3 s");
            var facing = new CupMeter();
            Assert.AreEqual(4f, Feed(facing, 5f, 1) / 72f, 0.05f, "one hand only facing it: 4 s");

            var look = new CupMeter();
            Assert.AreEqual(3f, Feed(look, 4f, 0, false, true) / 72f, 0.05f, "look + pinch-hold: 3 s");

            var cools = new CupMeter();
            Feed(cools, 1.5f, 2);
            Assert.AreEqual(0.75f, cools.Progress, 0.02f);
            Feed(cools, 1f, 0);
            // 0.35 s of grace still filling (+0.175), then 0.65 s cooling at 0.25 a second (-0.16).
            Assert.AreEqual(0.76f, cools.Progress, 0.03f, "let go: a short grace, then it cools slowly");
            Assert.IsFalse(cools.Done);
        }

        [Test]
        public void CupMeter_TrackingFlickerEveryOtherFewFrames_StillFillsInAboutTwoSeconds()
        {
            // Headset Oct 27: two hands in the zone, tracking dropping one out for 4 frames in every 8.
            var m = new CupMeter();
            int frame = 0;
            float t = 0f;
            for (; t < 4f && !m.Done; t += 1f / 72f, frame++) m.Feed(1f / 72f, frame % 8 < 4 ? 2 : 0, false, false);
            Assert.IsTrue(m.Done, "it still rekindles");
            Assert.Less(t, 2.2f, "about as fast as a steady hold");
        }

        [Test]
        public void HearthShrines_Dormant_IsPerMapAndAnchor()
        {
            HearthShrines.SetDormant("WorldMap_Cinderwood", new Vector2Int(24, 26), true);
            try
            {
                Assert.IsTrue(HearthShrines.IsDormant("WorldMap_Cinderwood", new Vector2Int(24, 26)));
                Assert.IsFalse(HearthShrines.IsDormant("WorldMap_StillIsle", new Vector2Int(24, 26)), "other maps' shrines stay lit");
                Assert.IsFalse(HearthShrines.IsDormant("WorldMap_Cinderwood", new Vector2Int(10, 10)));
            }
            finally
            {
                HearthShrines.SetDormant("WorldMap_Cinderwood", new Vector2Int(24, 26), false);
            }
            Assert.IsFalse(HearthShrines.IsDormant("WorldMap_Cinderwood", new Vector2Int(24, 26)));
        }

        // ---- B: the smoky hollow ----

        static List<(int lantern, float at)> RunChain(LanternChain c, float seconds)
        {
            var caught = new List<(int, float)>();
            float t = 0f;
            for (; t < seconds; t += 1f / 72f)
            {
                int k = c.Tick(1f / 72f);
                if (k >= 0) caught.Add((k, t));
            }
            return caught;
        }

        [Test]
        public void Lanterns_SweepOfTwo_RacesDownTheLine015Apart_ThenBack()
        {
            var c = new LanternChain(6);
            Assert.IsTrue(c.Touch(2));
            RunChain(c, 0.5f);
            Assert.IsTrue(c.Touch(3), "a second lantern within 1.5 s");
            Assert.IsTrue(c.Racing);
            var caught = RunChain(c, 2f);
            Assert.AreEqual(new[] { 4, 5, 1, 0 }, caught.ConvertAll(x => x.lantern).ToArray(), "on in the sweep's direction, then back past the first");
            for (int i = 1; i < caught.Count; i++) Assert.AreEqual(0.15f, caught[i].at - caught[i - 1].at, 0.03f, "0.15 s apart");
            Assert.AreEqual(0.15f, caught[0].at, 0.03f);
            Assert.IsTrue(c.AllLit);
            Assert.AreEqual(2, c.Origin, "the smoke burns from where the sweep started");
        }

        [Test]
        public void Lanterns_TouchesTooFarApartInTime_DontRace_LookPinchLightsTheNext()
        {
            var c = new LanternChain(6);
            c.Touch(0);
            RunChain(c, 2f);
            c.Touch(1);
            Assert.IsFalse(c.Racing, "2 s apart is two single touches");
            Assert.AreEqual(0b11, c.Mask);
            Assert.IsTrue(c.Nudge(3));
            var caught = RunChain(c, 1f);
            Assert.AreEqual(1, caught.Count);
            Assert.AreEqual(4, caught[0].lantern, "the next one along");
            Assert.AreEqual(0.5f, caught[0].at, 0.03f, "0.5 s later");
            var last = new LanternChain(3);
            last.Restore(0b011);
            last.Nudge(2);
            Assert.IsTrue(last.AllLit, "the last one: nothing further to catch");
            Assert.AreEqual(0, RunChain(last, 1f).Count);
        }

        static WorldMap Cinderwood()
        {
            var map = AssetDatabase.LoadAssetAtPath<WorldMap>(CinderwoodTools.MapPath);
            if (map == null) Assert.Inconclusive($"{CinderwoodTools.MapPath} missing (run Tools > Pocket Isle > Set Up MR_Main (10f Cinderwood hands)).");
            return map;
        }

        static HashSet<Vector2Int> Flood(WorldMap map, Vector2Int from)
        {
            var seen = new HashSet<Vector2Int> { from };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                var t = queue.Dequeue();
                foreach (var d in new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
                    if (seen.Add(t + d) && map.CanWalk(t + d)) queue.Enqueue(t + d);
                    else if (!map.CanWalk(t + d)) seen.Remove(t + d);
            }
            return seen;
        }

        [Test]
        public void Hollow_SmokeBlocksTheTrail_LanternsLineIt_GoneItConnects()
        {
            var map = Cinderwood();
            map.ClearRuntimeChanges();
            try
            {
                foreach (var t in CinderwoodTools.HollowTiles)
                {
                    Assert.AreEqual("SmokeCloud", map.TryGetProp(map.PropAt(t.y, t.x), out var e) ? e.recipe.name : "none", $"smoke on c{t.x} r{t.y}");
                    Assert.IsFalse(map.CanWalk(t), "smoke blocks walking");
                }
                foreach (var t in CinderwoodTools.HollowLanterns)
                    Assert.AreEqual("StoneLantern", map.TryGetProp(map.PropAt(t.y, t.x), out var e) ? e.recipe.name : "none", $"lantern on c{t.x} r{t.y}");
                var springs = CinderwoodTools.SpringsSpot;
                var shrineFront = CinderwoodTools.Tile(CinderwoodTools.ShrineXY + new Vector2Int(0, -2));
                var smoky = Flood(map, CinderwoodTools.Arrive);
                Assert.IsTrue(smoky.Contains(shrineFront), "the village is reachable with the smoke on");
                Assert.IsFalse(smoky.Contains(springs), "the springs are not");
                foreach (var t in CinderwoodTools.HollowTiles) map.SetPropOpen(t, true);
                Assert.IsTrue(Flood(map, CinderwoodTools.Arrive).Contains(springs), "smoke gone: the trail connects to the springs");
            }
            finally
            {
                map.ClearRuntimeChanges();
            }
        }

        // ---- C: ember in your palm ----

        static EmberCarry.Hand Palm(float up, bool nearVent = false, bool overFirebox = false, bool pinching = false, bool pinchStarted = false, bool atEmber = false) =>
            new EmberCarry.Hand { tracked = true, up = up, nearVent = nearVent, overFirebox = overFirebox, pinching = pinching, pinchStarted = pinchStarted, atEmber = atEmber };

        static EmberState Hold(EmberCarry c, float seconds, EmberCarry.Hand left, EmberCarry.Hand right)
        {
            var hands = new[] { left, right };
            for (float t = 0f; t < seconds; t += 1f / 72f) c.Feed(1f / 72f, hands);
            return c.State;
        }

        [Test]
        public void Ember_PalmUpBesideTheVent_HopsOn_StaysWhileUp_TippedOverTheFirebox_GoesIn()
        {
            var c = new EmberCarry();
            Assert.AreEqual(EmberState.InVent, Hold(c, 0.2f, default, Palm(0.95f, nearVent: true)), "0.2 s is not enough");
            Assert.AreEqual(EmberState.OnPalm, Hold(c, 0.3f, default, Palm(0.95f, nearVent: true)));
            Assert.AreEqual(1, c.Holder, "on the right palm");
            Assert.AreEqual(EmberState.OnPalm, Hold(c, 2f, default, Palm(0.5f)), "tilted but still roughly up: carried");
            Assert.AreEqual(EmberState.OnPalm, Hold(c, 0.5f, default, Palm(0.9f, overFirebox: true)), "over the firebox palm up: still on the palm");
            Assert.AreEqual(EmberState.In, Hold(c, 1f / 72f, default, Palm(-0.8f, overFirebox: true)), "turned over above the firebox: in");
        }

        [Test]
        public void Ember_TippedElsewhere_OrHandLost_FallsBack_PalmDownBesideTheVent_DoesNotLift()
        {
            var c = new EmberCarry();
            Assert.AreEqual(EmberState.InVent, Hold(c, 1f, Palm(-0.9f, nearVent: true), default), "palm down beside it: no");
            Hold(c, 0.5f, Palm(0.9f, nearVent: true), default);
            Assert.AreEqual(EmberState.OnPalm, c.State);
            Assert.AreEqual(EmberState.Falling, Hold(c, 1f / 72f, Palm(-0.2f), default), "turned over away from the firebox: it falls");
            c.Back();
            Assert.AreEqual(EmberState.InVent, c.State);
            Hold(c, 0.5f, Palm(0.9f, nearVent: true), default);
            Assert.AreEqual(EmberState.Falling, Hold(c, 1f / 72f, default, default), "tracking lost: it falls");
        }

        [Test]
        public void Ember_Pinched_IsCarriedWhileThePinchHolds_LetGoOverTheFireboxGoesIn_LookPinchFlies()
        {
            var c = new EmberCarry();
            Hold(c, 1f / 72f, Palm(-0.5f, pinching: true, pinchStarted: true, atEmber: true), default);
            Assert.AreEqual(EmberState.OnPalm, c.State);
            Assert.IsTrue(c.Pinched);
            Assert.AreEqual(EmberState.OnPalm, Hold(c, 1f, Palm(-0.5f, pinching: true), default), "pinched: the palm can face any way");
            Assert.AreEqual(EmberState.In, Hold(c, 1f / 72f, Palm(-0.5f, overFirebox: true), default), "pinch opened over the firebox: in");

            var look = new EmberCarry();
            look.Hover();
            Assert.AreEqual(EmberState.Hovering, look.State);
            look.Fly();
            Assert.AreEqual(EmberState.Flying, look.State);
            look.Arrive();
            Assert.AreEqual(EmberState.In, look.State);
        }
    }
}
