using NUnit.Framework;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>Step 14d: Mugesh's Nov 3 evening notes (portals you walk up to, the walk lesson, the speed).</summary>
    public class Step14dTests
    {
        [Test]
        public void Portal_SteppingOntoTheFrontHeadingIn_GoesThrough_SidewaysDoesNot()
        {
            var g = new GateLink { id = "wind", tile = new Vector2Int(10, 5) };
            var front = g.Front(0);
            Assert.IsTrue(ShardGates.StepsIn(g, front + Vector2Int.up, front), "walking up to the arch");
            Assert.IsFalse(ShardGates.StepsIn(g, front + Vector2Int.right, front), "passing along the front row");
            Assert.IsFalse(ShardGates.StepsIn(g, front + Vector2Int.up * 2, front + Vector2Int.up), "one tile short");
        }

        [Test]
        public void Walking_FasterThanBefore()
        {
            Assert.Less(EditorTools.Step14dSetup.WalkSeconds, 0.4f);
            Assert.Less(EditorTools.Step14dSetup.RunSeconds, EditorTools.Step14dSetup.WalkSeconds);
        }
    }
}
