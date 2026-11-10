using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>Step 28 and the Nov 10 playtest: lesson walking, the tap lesson, the two ghosts, the trail to the grass.</summary>
    public class Step28Tests
    {
        const string HubPath = "Assets/_Project/Data/Worlds/WorldMap_StillIsle.asset";

        static WorldMap Hub()
        {
            var guids = AssetDatabase.FindAssets("WorldMap_StillIsle t:WorldMap");
            if (guids.Length == 0) Assert.Inconclusive("WorldMap_StillIsle missing.");
            return AssetDatabase.LoadAssetAtPath<WorldMap>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        static OnboardingScript Lessons()
        {
            var s = AssetDatabase.LoadAssetAtPath<OnboardingScript>("Assets/_Project/Data/Onboarding/Onboarding_Main.asset");
            if (s == null) Assert.Inconclusive("Onboarding_Main missing.");
            return s;
        }

        static bool OnGate(WorldMap w, Vector2Int t)
        {
            foreach (var g in w.gates)
                if (g.Covers(t)) return true;
            return false;
        }

        static readonly Vector2Int[] Ways = { new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(0, 1) };

        [Test]
        public void LessonWalk_from_the_start_never_ends_by_itself_in_any_direction()
        {
            var w = Hub();
            var tiles = new List<Vector2Int>();
            foreach (var way in Ways)
            {
                string end = SmartStep.PlanRoam(w, w.startTile, way, t => OnGate(w, t), 400, tiles);
                Assert.AreEqual(SmartStep.Cap, end, $"way {way}");
                Assert.AreEqual(400, tiles.Count, $"way {way}");
                var cur = w.startTile;
                foreach (var t in tiles)
                {
                    Assert.AreEqual(1, (t - cur).sqrMagnitude, $"one tile at a time, at {t}");
                    Assert.IsTrue(w.CanWalk(t), $"{t} walkable");
                    Assert.IsFalse(OnGate(w, t), $"{t} is a gate");
                    cur = t;
                }
            }
        }

        [Test]
        public void LessonWalk_goes_past_the_road_edge_straight_on()
        {
            var w = Hub();
            var tiles = new List<Vector2Int>();
            SmartStep.PlanRoam(w, w.startTile, new Vector2Int(-1, 0), t => OnGate(w, t), 12, tiles);
            bool leftRoad = false;
            foreach (var t in tiles) leftRoad |= w.At(t) != IslandMap.Path;
            Assert.IsTrue(leftRoad, "the old rule stopped at the road's edge: 1 tile from the start");
            Assert.Greater(tiles.Count, 4);
        }

        [Test]
        public void LessonWalk_turns_to_the_longer_side_at_a_wall_and_back_when_shut_in()
        {
            var map = ScriptableObject.CreateInstance<IslandMap>();
            try
            {
                // One corridor: a boxed-in explorer has no way to go but back.
                map.layout = ".....\n.GGG.\n.....";
                var tiles = new List<Vector2Int>();
                Assert.AreEqual(SmartStep.Cap, SmartStep.PlanRoam(map, new Vector2Int(2, 1), new Vector2Int(1, 0), null, 6, tiles));
                Assert.AreEqual(new Vector2Int(3, 1), tiles[0]);
                Assert.AreEqual(new Vector2Int(2, 1), tiles[1], "turned around at the wall");
            }
            finally
            {
                Object.DestroyImmediate(map);
            }
        }

        [Test]
        public void Far_lesson_passes_only_by_a_real_tap_or_a_long_wait()
        {
            var far = Lessons().lessons.Find(l => l.id == Onboarding.FarLesson);
            Assert.AreEqual("tapStopped", far.doneWhen, "auto-stopping on a gold tile used to pass it without a tap");
            Assert.IsTrue(string.IsNullOrEmpty(far.goldTileRule), "no gold tile to stop on in this lesson");
            Assert.GreaterOrEqual(far.skipAfter, 60f);
        }

        [Test]
        public void Both_tap_lessons_show_the_swipe_and_then_the_tap()
        {
            var s = Lessons();
            Assert.AreEqual(GhostHands.SwipeTapClip, s.lessons.Find(l => l.id == Onboarding.FarLesson).ghost);
            Assert.AreEqual(GhostHands.SwipeTapClip, s.lessons.Find(l => l.id == Onboarding.PracticeLesson).ghost);
        }

        [Test]
        public void Practice_target_needs_a_runup_and_lies_straight_ahead()
        {
            Assert.GreaterOrEqual(StopPractice.Distance(0), 4);
            var at = new Vector2Int(10, 10);
            Assert.IsTrue(StopPractice.StraightAhead(at, new Vector2Int(1, 0), new Vector2Int(14, 10)));
            Assert.IsFalse(StopPractice.StraightAhead(at, new Vector2Int(-1, 0), new Vector2Int(14, 10)), "behind");
            Assert.IsFalse(StopPractice.StraightAhead(at, new Vector2Int(1, 0), new Vector2Int(14, 11)), "off the line");
            Assert.IsFalse(StopPractice.StraightAhead(at, Vector2Int.zero, new Vector2Int(14, 10)), "standing");
        }

        [Test]
        public void Grass_trail_reaches_the_first_grass_from_every_tile_the_lessons_can_reach()
        {
            var w = Hub();
            Assert.IsTrue(w.TryGetLandmark("firstGrass", out var grass));
            var path = new List<Vector2Int>();
            var seen = new HashSet<Vector2Int> { w.startTile };
            var open = new Queue<Vector2Int>();
            open.Enqueue(w.startTile);
            int checkedTiles = 0;
            while (open.Count > 0)
            {
                var t = open.Dequeue();
                if (t != grass)
                {
                    Assert.IsTrue(ExplorerPath.FindAlongRoads(w, t, grass, path, 400), $"no trail from {t}");
                    checkedTiles++;
                }
                foreach (var way in Ways)
                {
                    var n = t + way;
                    if (seen.Contains(n) || !w.CanWalk(n) || OnGate(w, n)) continue;
                    seen.Add(n);
                    open.Enqueue(n);
                }
            }
            Assert.Greater(checkedTiles, 50);
        }

        [Test]
        public void Nothing_is_lesson_walking_outside_a_tutorial() => Assert.IsFalse(Onboarding.LessonWalking);
    }
}
