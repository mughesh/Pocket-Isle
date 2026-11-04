using NUnit.Framework;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>Step 14k: the grab band runs from the glowing line inward; nothing outside the line grabs.</summary>
    public class Step14kTests
    {
        const float Outset = 0.008f, Cap = 0.006f;

        static GrabZone.Shape Shape(float scale = 0.6f, bool lip = true) => new GrabZone.Shape
        {
            half = new Vector2(0.55f, 0.55f),
            capTop = Cap,
            lineOutset = Outset,
            lineRadius = 0.03f,
            scale = scale,
            outsideWorld = 0.003f,
            insideWorld = 0.035f,
            bandTop = Cap + 0.025f / scale,
            bandBottom = -0.12f,
            cornerSize = 0.1f,
            lipTop = -0.02f,
            lipActive = lip,
        };

        /// <summary>Near the front-right corner on the right edge, this far (world m) outside the line (negative = inside).</summary>
        static Vector3 RightCorner(GrabZone.Shape s, float worldOut, float y = Cap + 0.01f) =>
            new Vector3(s.half.x + Outset + worldOut / s.scale, y, s.half.y - 0.09f); // 9 cm local from the front: the right edge is nearest

        [Test]
        public void Band_GrabsInward_OneAndThreeCentimetres()
        {
            var s = Shape();
            Assert.AreEqual(GrabZone.Kind.Corner, GrabZone.Classify(RightCorner(s, 0f), s), "on the line");
            Assert.AreEqual(GrabZone.Kind.Corner, GrabZone.Classify(RightCorner(s, -0.01f), s), "1 cm inside, above the rim");
            Assert.AreEqual(GrabZone.Kind.Corner, GrabZone.Classify(RightCorner(s, -0.03f), s), "3 cm inside, above the rim");
            Assert.AreEqual(GrabZone.Kind.Corner, GrabZone.Classify(RightCorner(s, -0.03f, -0.04f), s), "3 cm inside, on the rock face");
            Assert.AreEqual(GrabZone.Kind.None, GrabZone.Classify(RightCorner(s, -0.05f), s), "5 cm inside: past the band");
        }

        [Test]
        public void NothingOutsideTheLine()
        {
            var s = Shape();
            Assert.AreEqual(GrabZone.Kind.Corner, GrabZone.Classify(RightCorner(s, 0.002f), s), "2 mm: tracking slack");
            Assert.AreEqual(GrabZone.Kind.None, GrabZone.Classify(RightCorner(s, 0.01f), s), "1 cm outside");
            Assert.AreEqual(GrabZone.Kind.None, GrabZone.Classify(RightCorner(s, 0.01f, -0.04f), s), "1 cm outside, low");
            Assert.AreEqual(GrabZone.Kind.None, GrabZone.Classify(RightCorner(s, 0.03f), s), "3 cm outside");
        }

        [Test]
        public void Band_Height()
        {
            var s = Shape();
            Assert.AreEqual(GrabZone.Kind.Corner, GrabZone.Classify(RightCorner(s, -0.01f, Cap + 0.02f / s.scale), s), "2 cm over the cap");
            Assert.AreEqual(GrabZone.Kind.None, GrabZone.Classify(RightCorner(s, -0.01f, Cap + 0.04f / s.scale), s), "4 cm over the cap");
            Assert.AreEqual(GrabZone.Kind.None, GrabZone.Classify(RightCorner(s, 0f, -0.2f), s), "under the band");
            Assert.AreEqual(GrabZone.Kind.None, GrabZone.Classify(new Vector3(s.half.x, Cap + 0.01f, 0f), s), "the middle of a side");
        }

        [Test]
        public void Band_IsWorldMetres_AtAnyScale()
        {
            foreach (float scale in new[] { 0.3f, 0.6f, 1.2f })
            {
                var s = Shape(scale);
                Assert.AreEqual(GrabZone.Kind.Corner, GrabZone.Classify(RightCorner(s, -0.03f), s), $"scale {scale}: 3 cm in");
                Assert.AreEqual(GrabZone.Kind.None, GrabZone.Classify(RightCorner(s, 0.01f), s), $"scale {scale}: 1 cm out");
            }
        }

        [Test]
        public void Lip_InwardBelowThePods_CornersInBattles()
        {
            var s = Shape();
            var lip = new Vector3(0f, -0.05f, s.half.y + Outset - 0.02f / s.scale);
            Assert.AreEqual(GrabZone.Kind.Lip, GrabZone.Classify(lip, s), "2 cm in from the front line");
            Assert.AreEqual(GrabZone.Kind.None, GrabZone.Classify(new Vector3(0f, -0.05f, s.half.y + Outset + 0.01f / s.scale), s), "1 cm in front");
            Assert.AreEqual(GrabZone.Kind.None, GrabZone.Classify(new Vector3(0f, -0.01f, lip.z), s), "up at the pods");
            Assert.AreEqual(GrabZone.Kind.None, GrabZone.Classify(lip, Shape(lip: false)), "battles and the lodge: corners only");
        }

        [Test]
        public void FrameLightsByTheSameZone()
        {
            var s = Shape();
            Assert.AreEqual(0f, GrabZone.DistanceToHandles(RightCorner(s, -0.02f), s), 1e-4f, "in the band");
            Assert.AreEqual(0.017f, GrabZone.DistanceToHandles(RightCorner(s, 0.02f), s), 0.002f, "2 cm out = 1.7 cm past the slack");
        }
    }
}
