using NUnit.Framework;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>Nov 7 headset notes: the Hall of Fame keeps the explorer's spot free.</summary>
    public class Step18Tests
    {
        [Test]
        public void HallArc_KeepsExplorerFree_NoOverlap_InFront()
        {
            var c = new Vector2(30f, 30f);
            var explorer = c + new Vector2(0f, 3f);
            for (int n = 1; n <= 6; n++)
            {
                const float w = 1.3f, minR = 2.65f;
                var spots = FinaleLogic.HallArc(c, n, w, minR, 1.4f, explorer);
                Assert.AreEqual(n, spots.Length);
                if (n > 1) Assert.GreaterOrEqual(FinaleLogic.MinGap(spots), 1.4f * w - 1e-3f, $"n={n}: centres too close");
                foreach (var s in spots)
                {
                    Assert.GreaterOrEqual(Vector2.Distance(s, explorer), 1.4f * w * 0.9f, $"n={n}: on the explorer");
                    Assert.GreaterOrEqual(Vector2.Distance(s, c), minR - 1e-3f, $"n={n}: inside the statue clearance");
                    Assert.LessOrEqual(Vector2.Angle(s - c, Vector2.up), 100.01f, $"n={n}: behind the statue");
                }
            }
        }
    }
}
