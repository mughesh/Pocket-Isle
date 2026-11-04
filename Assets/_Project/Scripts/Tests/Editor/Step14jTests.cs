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
    }
}
