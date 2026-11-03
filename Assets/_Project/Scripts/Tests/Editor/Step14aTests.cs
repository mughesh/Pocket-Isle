using NUnit.Framework;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>
    /// Step 14a: the placement rule (seated, standing, looking down / up, floor clamp, recenter shifts), the settled-pose
    /// wait and the shard's follow check.
    /// </summary>
    public class Step14aTests
    {
        static readonly IslandPlacement.Rule Rule = IslandPlacement.Rule.Default;
        const float Top = 0.05f;

        static Pose Place(Vector3 head, Quaternion look, float? floor = 0f) =>
            IslandPlacement.Compute(head, look * Vector3.forward, look * Vector3.up, floor, Top, Rule);

        [Test]
        public void Seated_45cmAhead_Top30cmBelowEyes_FacingThePlayer()
        {
            var p = Place(new Vector3(0f, 1.15f, 0f), Quaternion.identity);
            Assert.AreEqual(0.45f, p.position.z, 1e-4f);
            Assert.AreEqual(0f, p.position.x, 1e-4f);
            Assert.AreEqual(1.15f - 0.30f, p.position.y + Top, 1e-4f);
            Assert.AreEqual(-1f, (p.rotation * Vector3.forward).z, 1e-4f, "+Z faces the head");
        }

        [Test]
        public void Standing_SameRule_TopFollowsTheEyes()
        {
            var p = Place(new Vector3(0f, 1.65f, 0f), Quaternion.identity);
            Assert.AreEqual(1.35f, p.position.y + Top, 1e-4f);
        }

        [Test]
        public void LowEyes_FloorClampWins_UnlessItWouldGoAboveTheEyeLimit()
        {
            // Reclined low: eyes 0.8 m: 0.5 m by the rule, clamped up to 0.55 over the floor.
            Assert.AreEqual(0.55f, IslandPlacement.TopHeight(0.8f, 0f, Rule), 1e-4f);
            // Lying on the floor: eyes 0.5 m; never above 0.15 below the eyes.
            Assert.AreEqual(0.35f, IslandPlacement.TopHeight(0.5f, 0f, Rule), 1e-4f);
            // Floor unknown: just the rule.
            Assert.AreEqual(0.5f, IslandPlacement.TopHeight(0.8f, null, Rule), 1e-4f);
        }

        [Test]
        public void LookingDownOrUp_YawOnly_SameSpotAsLookingLevel()
        {
            var head = new Vector3(0.2f, 1.2f, -0.3f);
            var yaw = Quaternion.Euler(0f, 30f, 0f);
            var level = Place(head, yaw);
            foreach (float pitch in new[] { 45f, 89.5f, 90f, -60f, -90f })
            {
                var p = Place(head, yaw * Quaternion.Euler(pitch, 0f, 0f));
                Assert.Less((p.position - level.position).magnitude, 1e-3f, $"pitch {pitch}");
                Assert.Less(Quaternion.Angle(p.rotation, level.rotation), 0.1f, $"pitch {pitch}");
            }
        }

        [Test]
        public void RecenterShift_PlacementMovesWithTheHead()
        {
            var a = Place(new Vector3(0f, 1.2f, 0f), Quaternion.identity);
            var b = Place(new Vector3(1f, 1.2f, 2f), Quaternion.Euler(0f, 90f, 0f));
            Assert.AreEqual(new Vector3(1.45f, a.position.y, 2f).ToString("F3"), b.position.ToString("F3"));
            Assert.IsFalse(IslandPlacement.Misplaced(new Vector3(1f, 1.2f, 2f), Vector3.right, Vector3.up, b.position));
            // The old spot after the shift: way off.
            Assert.IsTrue(IslandPlacement.Misplaced(new Vector3(1f, 1.2f, 2f), Vector3.right, Vector3.up, a.position));
        }

        [Test]
        public void Misplaced_Over40DegOrOver80cm()
        {
            var head = new Vector3(0f, 1.2f, 0f);
            Assert.IsFalse(IslandPlacement.Misplaced(head, Vector3.forward, Vector3.up, Quaternion.Euler(0f, 35f, 0f) * Vector3.forward * 0.5f));
            Assert.IsTrue(IslandPlacement.Misplaced(head, Vector3.forward, Vector3.up, Quaternion.Euler(0f, 45f, 0f) * Vector3.forward * 0.5f));
            Assert.IsTrue(IslandPlacement.Misplaced(head, Vector3.forward, Vector3.up, Vector3.forward * 0.9f));
        }

        [Test]
        public void Settle_NeedsHalfASecondWithin1cmAnd1_5Deg()
        {
            var s = new IslandPlacement.SettleTracker();
            float t = 0f;
            var r = Quaternion.identity;
            Assert.IsFalse(s.Feed(t, Vector3.zero, r, Vector3.zero, r));
            // Tiny jitter keeps it settling.
            for (; t < 0.45f; t += 0.05f) Assert.IsFalse(s.Feed(t, new Vector3(0.004f, 0f, 0f), Quaternion.Euler(0f, 1f, 0f), Vector3.zero, r));
            // A 2 cm move restarts the wait.
            Assert.IsFalse(s.Feed(0.5f, new Vector3(0.02f, 0f, 0f), r, Vector3.zero, r));
            Assert.IsFalse(s.Feed(0.9f, new Vector3(0.02f, 0f, 0f), r, Vector3.zero, r));
            Assert.IsTrue(s.Feed(1.0f, new Vector3(0.02f, 0f, 0f), r, Vector3.zero, r));
            // TrackingSpace turning (world lock) restarts it too.
            Assert.IsFalse(s.Feed(1.1f, new Vector3(0.02f, 0f, 0f), r, Vector3.zero, Quaternion.Euler(0f, 2f, 0f)));
        }

        [Test]
        public void Follow_After1sOff_ByYawOrEyeHeight()
        {
            var f = new IslandPlacement.FollowCheck();
            var head = new Vector3(0f, 1.2f, 0f);
            var shard = new Vector3(0f, 0.9f, 0.45f);
            var away = Quaternion.Euler(0f, 50f, 0f) * Vector3.forward;
            Assert.IsFalse(f.Feed(0f, head, Vector3.forward, Vector3.up, shard, 1.2f), "in front: stays");
            Assert.IsFalse(f.Feed(0.1f, head, away, Vector3.up, shard, 1.2f));
            Assert.IsFalse(f.Feed(0.6f, head, away, Vector3.up, shard, 1.2f));
            Assert.IsFalse(f.Feed(0.9f, head, Vector3.forward, Vector3.up, shard, 1.2f), "looked back: resets");
            Assert.IsFalse(f.Feed(1.5f, head, away, Vector3.up, shard, 1.2f));
            Assert.IsTrue(f.Feed(2.5f, head, away, Vector3.up, shard, 1.2f));
            f.Reset();
            // Stood up 20 cm.
            Assert.IsFalse(f.Feed(3f, head + Vector3.up * 0.2f, Vector3.forward, Vector3.up, shard, 1.2f));
            Assert.IsTrue(f.Feed(4f, head + Vector3.up * 0.2f, Vector3.forward, Vector3.up, shard, 1.2f));
            Assert.IsFalse(IslandPlacement.FollowCheck.IsOff(head + Vector3.up * 0.1f, Vector3.forward, Vector3.up, shard, 1.2f, 35f, 0.15f));
        }
    }
}
