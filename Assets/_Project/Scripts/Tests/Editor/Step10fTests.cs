using NUnit.Framework;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>
    /// Step 10f: Cinderwood's hand moments, fed synthetic hand poses: cupping the village flame (two hands, one hand
    /// curled round it, hands that don't cup).
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
            Assert.AreEqual(-1, Feed(one, 5f, 1), "one hand that only faces it never rekindles it");
            Assert.AreEqual(3f, Feed(one, 4f, 1, true) / 72f, 0.05f, "one hand curled round it: 3 s");

            var look = new CupMeter();
            Assert.AreEqual(3f, Feed(look, 4f, 0, false, true) / 72f, 0.05f, "look + pinch-hold: 3 s");

            var cools = new CupMeter();
            Feed(cools, 1.5f, 2);
            Assert.AreEqual(0.75f, cools.Progress, 0.02f);
            Feed(cools, 1f, 0);
            Assert.AreEqual(0.25f, cools.Progress, 0.02f, "let go: cools at 0.5 a second");
            Assert.IsFalse(cools.Done);
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
    }
}
