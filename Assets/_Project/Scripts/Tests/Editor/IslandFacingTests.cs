using NUnit.Framework;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>Nov 15 (Mugesh: a small move to the side turned the held island too eagerly).</summary>
    public class IslandFacingTests
    {
        [Test]
        public void SmallMove_KeepsTheGrabbedYaw()
        {
            Assert.AreEqual(180f, IslandHandle.DeadZoneYaw(180f, 195f, 20f), 1e-3f);
            Assert.AreEqual(180f, IslandHandle.DeadZoneYaw(180f, 162f, 20f), 1e-3f);
        }

        [Test]
        public void BigMove_TurnsByTheExtraOnly_NoJump()
        {
            Assert.AreEqual(180f, IslandHandle.DeadZoneYaw(180f, 200f, 20f), 1e-3f);
            Assert.AreEqual(190f, IslandHandle.DeadZoneYaw(180f, 210f, 20f), 1e-3f);
            Assert.AreEqual(170f, IslandHandle.DeadZoneYaw(180f, 150f, 20f), 1e-3f);
            Assert.AreEqual(-40f, Mathf.DeltaAngle(0f, IslandHandle.DeadZoneYaw(350f, 300f, 20f)), 1e-3f, "wraps round 0");
        }

        [Test]
        public void ZeroZone_FacesTheHeadExactly()
        {
            Assert.AreEqual(0f, Mathf.DeltaAngle(197f, IslandHandle.DeadZoneYaw(180f, 197f, 0f)), 1e-3f);
        }
    }
}
