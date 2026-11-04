using NUnit.Framework;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>Step 14j: the island's grab zone hugs the glowing line (what you see is what grabs).</summary>
    public class Step14jTests
    {
        // Step 14k replaced the 14j grab-zone tests (outside reach, nothing above the cap): see Step14kTests.

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