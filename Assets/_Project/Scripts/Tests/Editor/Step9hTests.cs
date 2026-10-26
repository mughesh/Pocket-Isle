using NUnit.Framework;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>Step 9h: lodge exit rule, bubble over the island.</summary>
    public class Step9hTests
    {
        static readonly Vector2Int Out = new Vector2Int(0, 1);

        [Test]
        public void Lodge_SwipeTowardTheDoor_Leaves()
        {
            Assert.IsTrue(LodgeRoom.AimedOut(Out, Out, Out, false));
        }

        [Test]
        public void Lodge_TapOnTheDoorTile_Leaves()
        {
            Assert.IsTrue(LodgeRoom.AimedOut(Out, Out, Vector2Int.zero, true));
        }

        [Test]
        public void Lodge_PathPassingOrEndingBesideTheDoor_StaysIn()
        {
            // A path that crosses the door tile to somewhere else.
            Assert.IsFalse(LodgeRoom.AimedOut(Out, Out, Vector2Int.zero, false));
            // Sideways along the door row (a swipe left), or a teleport (no step).
            Assert.IsFalse(LodgeRoom.AimedOut(Vector2Int.left, Out, Vector2Int.left, false));
            Assert.IsFalse(LodgeRoom.AimedOut(Vector2Int.zero, Out, Vector2Int.zero, true));
        }

        [Test]
        public void Bubble_SegmentOverTheFootprint_Crosses()
        {
            var half = new Vector2(0.3f, 0.3f);
            // Head in front of the island, bubble over its far half.
            Assert.IsTrue(DialogueBubble.CrossesFootprint(new Vector2(0f, 0.8f), new Vector2(0.1f, -0.1f), half));
            // Head in front, bubble beside the island (to the right, nearer than the island's back).
            Assert.IsFalse(DialogueBubble.CrossesFootprint(new Vector2(0.5f, 0.8f), new Vector2(0.45f, 0.1f), half));
            // Seen past the island's far corner: crosses.
            Assert.IsTrue(DialogueBubble.CrossesFootprint(new Vector2(0f, 0.8f), new Vector2(0.4f, -0.6f), half));
        }
    }
}
