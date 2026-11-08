using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>Step 21: the Nov 8 notes (the ending's Islings never stand in a prop).</summary>
    public class Step21Tests
    {
        // A 12 x 12 field, blocked: a stone lantern at (3, 5), the lodge at (8..9, 4..5), the statue at (5..6, 2..3).
        static bool Walkable(Vector2Int t)
        {
            if (t.x < 0 || t.y < 0 || t.x > 11 || t.y > 11) return false;
            if (t == new Vector2Int(3, 5)) return false;
            if (t.x >= 8 && t.x <= 9 && t.y >= 4 && t.y <= 5) return false;
            if (t.x >= 5 && t.x <= 6 && t.y >= 2 && t.y <= 3) return false;
            return true;
        }

        [Test]
        public void FreeSpot_OnAProp_MovesOffIt_FootprintClear()
        {
            var want = new Vector2(3.2f, 5f); // inside the lantern
            var spot = FinaleLogic.FreeSpot(want, 1.3f, Walkable, new List<Vector2>(), 1.8f);
            Assert.IsTrue(spot.HasValue);
            Assert.IsTrue(FinaleLogic.Clear(spot.Value, 1.3f, Walkable), $"{spot.Value}");
            Assert.Less(Vector2.Distance(spot.Value, want), 2.6f, "stays near its arc spot");
        }

        [Test]
        public void FreeSpot_AlreadyClear_StaysPut()
        {
            var want = new Vector2(2f, 9f);
            Assert.AreEqual(want, FinaleLogic.FreeSpot(want, 1.3f, Walkable, new List<Vector2>(), 1.8f));
        }

        [Test]
        public void FreeSpot_KeepsTheGap_AndTheExplorersTile()
        {
            var taken = new List<Vector2> { new Vector2(2f, 9f) };
            var free = new Vector2(4f, 9f);
            var spot = FinaleLogic.FreeSpot(new Vector2(2.5f, 9f), 1.3f, Walkable, taken, 1.8f, free);
            Assert.IsTrue(spot.HasValue);
            Assert.GreaterOrEqual(Vector2.Distance(spot.Value, taken[0]), 1.8f - 1e-4f);
            Assert.GreaterOrEqual(Vector2.Distance(spot.Value, free), 1.3f * 0.5f + 0.5f - 1e-4f);
            Assert.IsTrue(FinaleLogic.Clear(spot.Value, 1.3f, Walkable));
        }

        [Test]
        public void Footprint_WideIsling_CoversItsNeighbours()
        {
            // 1.3 tiles wide on a tile centre reaches into the 8 neighbours; a small one only its own tile.
            Assert.AreEqual(9, FinaleLogic.Footprint(new Vector2(4f, 4f), 1.3f).Count);
            Assert.AreEqual(1, FinaleLogic.Footprint(new Vector2(4f, 4f), 0.8f).Count);
        }

        [Test]
        public void FreeSpot_NeverRightBehindAProp_FromTheSeat()
        {
            // Just behind the lantern at (3, 5) (one row further from the player): clear ground, but hidden from the seat.
            var want = new Vector2(3f, 4f);
            Assert.AreEqual(want, FinaleLogic.FreeSpot(want, 0.8f, Walkable, null, 1.8f), "without the view rule it stays");
            var spot = FinaleLogic.FreeSpot(want, 0.8f, Walkable, null, 1.8f, null, null, 4f, Walkable);
            Assert.IsTrue(spot.HasValue);
            foreach (var t in FinaleLogic.Footprint(spot.Value, 0.8f)) Assert.IsTrue(Walkable(t + Vector2Int.up), $"{spot.Value}: prop in front");
        }

        [Test]
        public void FreeSpot_NothingFits_ReturnsNull()
        {
            Assert.IsNull(FinaleLogic.FreeSpot(new Vector2(50f, 50f), 1.3f, Walkable, null, 1.8f, null, null, 2f));
        }
    }
}
