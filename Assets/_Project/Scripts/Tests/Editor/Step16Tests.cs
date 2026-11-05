using NUnit.Framework;
using UnityEngine;

namespace Diorama.Tests
{
    public class Step16Tests
    {
        [Test]
        public void GhostFades_WhenAHandIsNear()
        {
            Assert.AreEqual(1f, GhostHands.NearAlpha(0f, 0.08f), 1e-5f);
            Assert.AreEqual(0.08f, GhostHands.NearAlpha(1f, 0.08f), 1e-5f);
            Assert.AreEqual(0.54f, GhostHands.NearAlpha(0.5f, 0.08f), 1e-5f);
            Assert.AreEqual(0.08f, GhostHands.NearAlpha(3f, 0.08f), 1e-5f, "clamped");
        }

        [Test]
        public void AsideGhost_SitsAcrossTheLineOfSight()
        {
            Vector3 d = GhostHands.AsideDir(new Vector3(0.3f, 1f, 2f), false);
            Assert.AreEqual(0f, d.y, 1e-5f);
            Assert.AreEqual(1f, d.magnitude, 1e-4f);
            Assert.AreEqual(-d, GhostHands.AsideDir(new Vector3(0.3f, 1f, 2f), true));
        }
    
        [Test]
        public void ShortLines_ShowAtOnce_LongOnesType()
        {
            Assert.AreEqual(20, DialogueBubble.VisibleChars(20, 0f, 60f, true));
            Assert.AreEqual(0, DialogueBubble.VisibleChars(80, 0f, 60f, false));
            Assert.AreEqual(30, DialogueBubble.VisibleChars(80, 0.5f, 60f, false));
            Assert.AreEqual(80, DialogueBubble.VisibleChars(80, 5f, 60f, false));
        }

        [Test]
        public void ObjectiveArea_BlocksInTheView()
        {
            var head = Vector3.zero;
            var spheres = new System.Collections.Generic.List<Vector4> { new Vector4(0f, 0f, 1f, 0.05f) };
            Assert.IsTrue(ObjectiveArea.Blocks(head, new Vector3(0f, 0f, 0.5f), 0.02f, spheres), "in front of it");
            Assert.IsTrue(ObjectiveArea.Blocks(head, new Vector3(0.05f, 0f, 1f), 0.02f, spheres), "touching");
            Assert.IsFalse(ObjectiveArea.Blocks(head, new Vector3(0.2f, 0f, 1f), 0.02f, spheres), "beside it");
        }
    }
}
