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
    }
}
