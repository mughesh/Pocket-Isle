using NUnit.Framework;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>Step 14j: the island's grab zone hugs the glowing line (what you see is what grabs).</summary>
    public class Step14jTests
    {
        const float Outset = 0.008f;

        static GrabZone.Shape Shape(float scale = 0.6f, bool lip = true) => new GrabZone.Shape
        {
            half = new Vector2(0.55f, 0.55f),
            capTop = 0.006f,
            lineOutset = Outset,
            lineRadius = 0.03f,
            scale = scale,
            outsideWorld = 0.012f,
            insideLocal = 0.035f,
            bandTop = 0.02f,
            bandBottom = -0.12f,
            cornerSize = 0.1f,
            lipTop = -0.02f,
            lipActive = lip,
        };

        /// <summary>A point on the right side face near the front-right corner, this far (world m) outside the line.</summary>
        static Vector3 RightCorner(GrabZone.Shape s, float worldOut, float y = -0.03f) =>
            new Vector3(s.half.x + Outset + worldOut / s.scale, y, s.half.y - 0.05f);

        [Test]
        public void Corner_GrabsOnAndJustOutsideTheLine_NotThreeCentimetresOut()
        {
            var s = Shape();
            Assert.AreEqual(GrabZone.Kind.Corner, GrabZone.Classify(RightCorner(s, 0f), s), "on the line");
            Assert.AreEqual(GrabZone.Kind.Corner, GrabZone.Classify(RightCorner(s, 0.01f), s), "1 cm outside");
            Assert.AreEqual(GrabZone.Kind.None, GrabZone.Classify(RightCorner(s, 0.015f), s), "1.5 cm outside: past the 1.2 cm reach");
            Assert.AreEqual(GrabZone.Kind.None, GrabZone.Classify(RightCorner(s, 0.03f), s), "3 cm outside");
            Assert.AreEqual(GrabZone.Kind.None, GrabZone.Classify(RightCorner(s, 0.10f), s), "10 cm outside (grabbed before 14j)");
        }

        [Test]
        public void Corner_GrabsInwardOverTheRock_NeverOverThePlayArea()
        {
            var s = Shape();
            var rock = new Vector3(s.half.x - 0.02f, -0.04f, s.half.y - 0.02f);
            Assert.AreEqual(GrabZone.Kind.Corner, GrabZone.Classify(rock, s), "2 local cm in, on the rock face");
            Assert.AreEqual(GrabZone.Kind.None, GrabZone.Classify(new Vector3(rock.x, 0.01f, rock.z), s), "above the cap: the play area");
            Assert.AreEqual(GrabZone.Kind.None, GrabZone.Classify(new Vector3(s.half.x - 0.06f, -0.04f, s.half.y - 0.06f), s), "deep inside the rock");
        }

        [Test]
        public void HeightBand_And_TheSidesMiddle_NeverGrab()
        {
            var s = Shape();
            Assert.AreEqual(GrabZone.Kind.None, GrabZone.Classify(RightCorner(s, 0f, -0.2f), s), "under the band (a hand resting below)");
            Assert.AreEqual(GrabZone.Kind.None, GrabZone.Classify(RightCorner(s, 0f, 0.05f), s), "well above the cap");
            Assert.AreEqual(GrabZone.Kind.None, GrabZone.Classify(new Vector3(s.half.x + Outset, -0.03f, 0f), s), "the middle of a side");
        }

        [Test]
        public void Lip_OnlyBelowThePods_AndOnlyWhileExploring()
        {
            var s = Shape();
            var lip = new Vector3(0f, -0.05f, s.half.y + Outset);
            Assert.AreEqual(GrabZone.Kind.Lip, GrabZone.Classify(lip, s));
            Assert.AreEqual(GrabZone.Kind.None, GrabZone.Classify(new Vector3(0f, -0.01f, lip.z), s), "up at the pods");
            Assert.AreEqual(GrabZone.Kind.None, GrabZone.Classify(lip + Vector3.forward * (0.03f / s.scale), s), "3 cm in front of the lip");
            var pods = Shape(lip: false);
            Assert.AreEqual(GrabZone.Kind.None, GrabZone.Classify(lip, pods), "battles and the lodge: corners only");
            Assert.AreEqual(GrabZone.Kind.Corner, GrabZone.Classify(RightCorner(pods, 0f), pods));
        }

        [Test]
        public void TwoHands_EachCornerGrabs()
        {
            var s = Shape();
            var right = RightCorner(s, 0.005f);
            var left = new Vector3(-right.x, right.y, right.z);
            var back = new Vector3(-right.x, right.y, -right.z);
            Assert.AreEqual(GrabZone.Kind.Corner, GrabZone.Classify(right, s));
            Assert.AreEqual(GrabZone.Kind.Corner, GrabZone.Classify(left, s));
            Assert.AreEqual(GrabZone.Kind.Corner, GrabZone.Classify(back, s));
        }

        [Test]
        public void ReachOutsideIsWorldMetres_AtAnyIslandScale()
        {
            foreach (float scale in new[] { 0.3f, 0.6f, 1.2f })
            {
                var s = Shape(scale);
                Assert.AreEqual(GrabZone.Kind.Corner, GrabZone.Classify(RightCorner(s, 0.01f), s), $"scale {scale}: 1 cm");
                Assert.AreEqual(GrabZone.Kind.None, GrabZone.Classify(RightCorner(s, 0.03f), s), $"scale {scale}: 3 cm");
            }
        }

        [Test]
        public void PodsWin_WithinAPodsReach()
        {
            var pod = new Vector3(0.1f, 0.02f, 0.34f);
            Assert.IsTrue(GrabZone.PodWins(pod + Vector3.right * 0.02f, pod, 0.03f));
            Assert.IsFalse(GrabZone.PodWins(pod + Vector3.right * 0.05f, pod, 0.03f));
        }

        [Test]
        public void FrameLightsByTheSameZone()
        {
            var s = Shape();
            Assert.AreEqual(0f, GrabZone.DistanceToHandles(RightCorner(s, 0.005f), s), 1e-4f, "in the zone: full");
            Assert.AreEqual(0.018f, GrabZone.DistanceToHandles(RightCorner(s, 0.03f), s), 0.002f, "3 cm out = 1.8 cm past the zone");
        }

        // ---- B: nothing overlaps ----

        [Test]
        public void Row_KeepsTheGap_EvenWithOneTabletGrown()
        {
            var xs = new System.Collections.Generic.List<float>();
            foreach (int n in new[] { 2, 3, 4, 5, 6 })
            {
                var size = TabletLayout.Row(n, 0.35f, new Vector2(0.074f, 0.03f), RuneBoard.HoverGrow, xs);
                Assert.AreEqual(n, xs.Count);
                var rects = new System.Collections.Generic.List<TabletLayout.Rect>();
                for (int i = 0; i < n; i++) rects.Add(new TabletLayout.Rect { label = $"t{i}", center = new Vector2(xs[i], 0f), size = size, grow = RuneBoard.HoverGrow });
                CollectionAssert.IsEmpty(TabletLayout.Problems(rects), $"{n} tablets");
                Assert.LessOrEqual(xs[n - 1] + size.x * 0.5f - (xs[0] - size.x * 0.5f), 0.35f + 1e-4f, "fits the width");
            }
        }

        [Test]
        public void Problems_FindsTouchingTablets()
        {
            var a = new TabletLayout.Rect { label = "Fire", center = new Vector2(0f, 0f), size = new Vector2(0.074f, 0.03f), grow = 0.1f };
            var b = new TabletLayout.Rect { label = "Water", center = new Vector2(0.078f, 0f), size = a.size, grow = 0.1f };
            Assert.AreEqual(1, TabletLayout.Problems(new[] { a, b }).Count, "4 mm apart");
            b.center.x = 0.074f + 0.009f;
            Assert.AreEqual(0, TabletLayout.Problems(new[] { a, b }).Count, "9 mm apart, 5.3 mm when one grows");
            var below = new TabletLayout.Rect { label = "Page", center = new Vector2(0f, -0.035f), size = a.size, grow = 0.1f };
            Assert.AreEqual(1, TabletLayout.Problems(new[] { a, below }).Count, "5 mm under it");
        }

        [Test]
        public void HaloStaysInsideHalfTheGap()
        {
            Assert.Less(TabletKit.HaloReach * 2f, TabletLayout.MinGap, "two neighbours' halos never meet");
        }

        [Test]
        public void BattleRunes_KeepTheGap()
        {
            var go = new GameObject("runes");
            try
            {
                var r = go.AddComponent<BattleRunes>();
                CollectionAssert.IsEmpty(r.LayoutProblems());
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}