using Diorama.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>Step 9i: the lodge door on the back wall, the explorer coming in from it, the exit rule with the new outward.</summary>
    public class Step9iTests
    {
        static readonly Vector2Int Out = new Vector2Int(0, -1);

        static WorldMap Lodge()
        {
            var lodge = AssetDatabase.LoadAssetAtPath<WorldMap>(LodgeTools.LodgePath);
            if (lodge == null) Assert.Inconclusive($"{LodgeTools.LodgePath} missing (run Tools > Pocket Isle > Set Up MR_Main (9i lodge back door)).");
            return lodge;
        }

        [Test]
        public void Lodge_DoorOnTheBackWall_WalkableAndReachable()
        {
            var lodge = Lodge();
            Assert.AreEqual('D', lodge.PropAt(0, LodgeTools.DoorTiles[0].x), "the arch prop sits on the first door tile, row 0");
            for (int c = 0; c < lodge.Columns; c++)
                Assert.AreNotEqual('D', lodge.PropAt(lodge.Rows - 1, c), $"no door left on the near edge (c{c})");
            foreach (var d in LodgeTools.DoorTiles)
            {
                Assert.AreEqual(0, d.y, "door on the back wall");
                Assert.IsTrue(lodge.CanWalk(d), $"door tile {d} walkable (stepping on it leads out)");
                Assert.IsTrue(ExplorerPath.CanReach(lodge, lodge.startTile, d, 20), $"door tile {d} reachable from the entry");
            }
        }

        [Test]
        public void Lodge_ExplorerComesInAtTheBackDoor_StepsTowardThePlayer()
        {
            var lodge = Lodge();
            var entry = lodge.startTile;
            Assert.AreEqual(LodgeTools.EntryTile, entry);
            Assert.Contains(entry + Out, LodgeTools.DoorTiles, "the entry is just inside a door tile");
            var walkIn = LodgeRoom.WalkInTile(entry, Out);
            Assert.AreEqual(entry + Vector2Int.up, walkIn, "one row toward the player");
            Assert.IsTrue(lodge.CanWalk(entry));
            Assert.IsTrue(lodge.CanWalk(walkIn));
            // Step 14k: the pedestal is gone.
            foreach (var t in new[] { entry, walkIn })
            {
                CollectionAssert.DoesNotContain(LodgeTools.DoorTiles, t, "never on the door");
                CollectionAssert.DoesNotContain(LodgeTools.CushionTiles, t, "no cushion on the walk in");
            }
        }

        [Test]
        public void Lodge_RoomDefaults_MatchTheMap()
        {
            var d = new LodgeRoomDefaults();
            CollectionAssert.AreEqual(LodgeTools.DoorTiles, d.doorTiles);
            CollectionAssert.AreEqual(LodgeTools.CushionTiles, d.cozyTiles);
            Assert.AreEqual(Out, d.outward);
            var lodge = Lodge();
            foreach (var c in LodgeTools.CushionTiles) Assert.AreEqual('c', lodge.PropAt(c.y, c.x), $"cushion prop on {c}");
        }

        [Test]
        public void Lodge_SwipeTowardTheBackDoor_Leaves_SidewaysOrTowardThePlayer_StaysIn()
        {
            Assert.IsTrue(LodgeRoom.AimedOut(Out, Out, Out, false), "swipe toward the back");
            Assert.IsTrue(LodgeRoom.AimedOut(Out, Out, Vector2Int.zero, true), "tap / look + pinch on a door tile");
            Assert.IsFalse(LodgeRoom.AimedOut(Vector2Int.up, Out, Vector2Int.up, false), "walking toward the player");
            Assert.IsFalse(LodgeRoom.AimedOut(Vector2Int.right, Out, Vector2Int.right, false), "sideways along the back row");
            Assert.IsFalse(LodgeRoom.AimedOut(Out, Out, Vector2Int.zero, false), "a path crossing the door to elsewhere");
        }

        /// <summary>LodgeRoom's serialized defaults, read off a throwaway component.</summary>
        class LodgeRoomDefaults
        {
            public readonly Vector2Int[] doorTiles, cozyTiles;
            public readonly Vector2Int outward;

            public LodgeRoomDefaults()
            {
                var go = new GameObject("lodge defaults");
                try
                {
                    go.SetActive(false);
                    var r = go.AddComponent<LodgeRoom>();
                    doorTiles = r.doorTiles;
                    cozyTiles = r.cozyTiles;
                    outward = r.doorOutward;
                }
                finally
                {
                    Object.DestroyImmediate(go);
                }
            }
        }
    }
}
