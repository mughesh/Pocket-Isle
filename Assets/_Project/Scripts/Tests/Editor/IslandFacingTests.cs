using NUnit.Framework;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>Nov 15 (Mugesh: a little left of centre the grabbed island turned toward the head and read as tilted).</summary>
    public class IslandFacingTests
    {
        [Test]
        public void ALittleToTheSide_StaysParallel()
        {
            Assert.AreEqual(180f, IslandHandle.ParallelYaw(180f, 197f, 35f), 1e-3f, "17 deg left: square to the forward");
            Assert.AreEqual(180f, IslandHandle.ParallelYaw(180f, 150f, 35f), 1e-3f, "30 deg right: square to the forward");
        }

        [Test]
        public void FarToTheSide_TurnsByTheExtraOnly()
        {
            Assert.AreEqual(190f, IslandHandle.ParallelYaw(180f, 225f, 35f), 1e-3f);
            Assert.AreEqual(160f, IslandHandle.ParallelYaw(180f, 125f, 35f), 1e-3f);
            // No jump at the cone's edge.
            Assert.AreEqual(180f, IslandHandle.ParallelYaw(180f, 215f, 35f), 1e-3f);
        }

        [Test]
        public void WrapsRoundZero()
        {
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, IslandHandle.ParallelYaw(10f, -20f, 35f) - 10f), 1e-3f);
            Assert.AreEqual(-25f, Mathf.DeltaAngle(0f, IslandHandle.ParallelYaw(350f, 300f, 35f)), 1e-3f, "50 deg right of 350: 15 past the cone");
        }

        [Test]
        public void ConeZero_FacesTheHead()
        {
            Assert.AreEqual(0f, Mathf.DeltaAngle(197f, IslandHandle.ParallelYaw(180f, 197f, 0f)), 1e-3f);
        }
    }
}
