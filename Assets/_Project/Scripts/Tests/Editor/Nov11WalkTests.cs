using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>
    /// Nov 11 evening, section A: the explorer never leaves the island window (R51), roads are entered with a stop and
    /// never wandered off (R52), and the walking simulation is clean on every map.
    /// </summary>
    public class Nov11WalkTests
    {
        static IslandMap Map(string layout)
        {
            var m = ScriptableObject.CreateInstance<IslandMap>();
            m.SetLayout(layout);
            return m;
        }

        // ---- R51: the window ----

        [Test]
        public void InWindow_EvenFootprint_CoversItsTilesAndNoMore()
        {
            // A 14-wide footprint centred on the corner between tiles 20 and 21 shows tiles 14..27.
            var c = new Vector2(20.5f, 20.5f);
            Assert.IsTrue(WalkRules.InWindow(c, 14, 14, 1f, new Vector2Int(14, 20)));
            Assert.IsTrue(WalkRules.InWindow(c, 14, 14, 1f, new Vector2Int(27, 20)));
            Assert.IsFalse(WalkRules.InWindow(c, 14, 14, 1f, new Vector2Int(13, 20)));
            Assert.IsFalse(WalkRules.InWindow(c, 14, 14, 1f, new Vector2Int(28, 20)));
            Assert.IsFalse(WalkRules.InWindow(c, 14, 14, 1f, new Vector2Int(20, 28)));
            // Zoomed in twice: 7 tiles, 18..23.
            Assert.IsTrue(WalkRules.InWindow(c, 14, 14, 2f, new Vector2Int(18, 20)));
            Assert.IsTrue(WalkRules.InWindow(c, 14, 14, 2f, new Vector2Int(23, 20)));
            Assert.IsFalse(WalkRules.InWindow(c, 14, 14, 2f, new Vector2Int(17, 20)));
            Assert.IsFalse(WalkRules.InWindow(c, 14, 14, 2f, new Vector2Int(24, 20)));
        }

        [Test]
        public void Truncate_CutsThePlanAtTheFirstHiddenTile()
        {
            var plan = new List<Vector2Int> { new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0), new Vector2Int(4, 0) };
            Assert.AreEqual(2, WalkRules.Truncate(plan, t => t.x <= 2));
            Assert.AreEqual(2, plan.Count);
            Assert.AreEqual(0, WalkRules.Truncate(plan, t => true));
            Assert.AreEqual(2, plan.Count);
            Assert.AreEqual(2, WalkRules.Truncate(plan, t => false), "nothing shown: nothing to walk");
            Assert.IsEmpty(plan);
        }

        [Test]
        public void Explorer_GuardsEveryStep_WithTheWindow()
        {
            var src = System.IO.File.ReadAllText("Assets/_Project/Scripts/Runtime/GestureTestPiece.cs");
            Assert.AreEqual(3, System.Text.RegularExpressions.Regex.Matches(src, @"!CanStepTo\(").Count, "the plan step, the GoTo step and the plain step");
            StringAssert.Contains("KeepPlanInView();", src);
            StringAssert.Contains("window.Shows(t)", src);
            StringAssert.Contains("if (window == null || !window.HasFocus) return;", src, "a following view scrolls with the walk: the plan is cut only under a held view");
        }

        // ---- R52: roads ----

        const string Road =
            "GGGGGGGG\n" +
            "GGGGGGGG\n" +
            "PPPPPPGG\n" +
            "PPPPPPGG\n" +
            "GGGGGGGG\n" +
            "GGGGGGGG";

        [Test]
        public void Walk_EnteringARoadFromGrass_StopsOnTheRoad()
        {
            var map = Map(Road);
            var tiles = new List<Vector2Int>();
            foreach (bool corridors in new[] { true, false })
            {
                // From the grass south of the road, swiping north (row - 1).
                string end = SmartStep.Plan(map, new Vector2Int(2, 5), new Vector2Int(0, -1), true, 200, null, tiles, false, corridors);
                Assert.AreEqual(SmartStep.RoadEnter, end, $"corridors {corridors}");
                CollectionAssert.AreEqual(new[] { new Vector2Int(2, 4), new Vector2Int(2, 3) }, tiles, "grass, then the first road tile, and stop");
            }
        }

        [Test]
        public void Walk_CrossingTheOtherLane_IsTheSwipesOwnWay()
        {
            var map = Map(Road);
            var tiles = new List<Vector2Int>();
            // Standing on the near lane, swiping north: over the far lane and on into the grass (the road doesn't go that way).
            string end = SmartStep.Plan(map, new Vector2Int(2, 3), new Vector2Int(0, -1), true, 200, null, tiles, false, true);
            Assert.AreNotEqual(SmartStep.RoadEnd, end);
            Assert.AreEqual(new Vector2Int(2, 2), tiles[0]);
            Assert.AreEqual(new Vector2Int(2, 1), tiles[1], "off the far lane into the grass");
            Assert.AreEqual(new Vector2Int(2, 0), tiles[2]);
        }

        [Test]
        public void Walk_AlongARoad_StopsAtItsEnd_NeverInTheGrassBeyond()
        {
            var map = Map(Road);
            var tiles = new List<Vector2Int>();
            string end = SmartStep.Plan(map, new Vector2Int(0, 2), new Vector2Int(1, 0), true, 200, null, tiles, false, true);
            Assert.AreEqual(SmartStep.RoadEnd, end);
            Assert.AreEqual(new Vector2Int(5, 2), tiles[tiles.Count - 1], "the last road tile");
            Assert.IsTrue(tiles.All(t => map.At(t.y, t.x) == IslandMap.Path));
        }

        [Test]
        public void Walk_FromTheRoadsEnd_StepsOntoTheGrass()
        {
            var map = Map(Road);
            var tiles = new List<Vector2Int>();
            foreach (bool corridors in new[] { true, false })
            {
                string end = SmartStep.Plan(map, new Vector2Int(5, 2), new Vector2Int(1, 0), true, 200, null, tiles, false, corridors);
                Assert.AreNotEqual(SmartStep.RoadEnd, end, $"corridors {corridors}");
                Assert.AreEqual(new Vector2Int(6, 2), tiles[0], "the swipe points at grass and the road doesn't go on: off it goes");
            }
        }

        // ---- the simulation ----

        [Test]
        public void WalkSim_FindsAWalkThroughARoad()
        {
            // A planner that ignores the road rules: the sim must flag it.
            var map = Map(Road);
            var rep = WalkSim.Run(map, null, corridors: true);
            Assert.IsTrue(rep.Clean, "the real planner is clean here: " + string.Join("; ", rep.failures));
            Assert.Greater(rep.ends[SmartStep.RoadEnter], 0);
        }

        [Test]
        public void WalkSim_EveryRealm_IsClean()
        {
            foreach (bool firstGrass in new[] { false, true })
            {
                var reports = EditorTools.Nov11Walk.RunAll(firstGrass);
                Assert.AreEqual(EditorTools.Nov11Walk.Maps.Length, reports.Count, "every map loads");
                var bad = reports.Where(r => !r.Clean).ToList();
                Assert.IsEmpty(bad, string.Join("\n", bad.Select(r => r + "\n  " + string.Join("\n  ", r.failures.Take(12)))));
                foreach (var r in reports)
                {
                    Assert.Greater(r.starts, 0, r.map);
                    Assert.IsFalse(r.ends.ContainsKey(WalkSim.Loops), r.map + " loops");
                    Assert.IsFalse(r.ends.ContainsKey(SmartStep.Cap), r.map + ": every walk ends for a reason");
                }
            }
        }
    }
}
