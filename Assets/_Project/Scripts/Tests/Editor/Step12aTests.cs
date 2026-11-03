using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>Step 12a: smart-step stop rules on small synthetic maps (tiles are column, row), and the double-swipe window.</summary>
    public class Step12aTests
    {
        static readonly Vector2Int Right = new Vector2Int(1, 0);

        static IslandMap Map(string layout)
        {
            var m = ScriptableObject.CreateInstance<IslandMap>();
            m.SetLayout(layout);
            return m;
        }

        static (string end, List<Vector2Int> tiles) Plan(string layout, Vector2Int from, Vector2Int dir, bool on = false,
            System.Func<Vector2Int, Vector2Int, WalkStop> stop = null)
        {
            var tiles = new List<Vector2Int>();
            string end = SmartStep.Plan(Map(layout), from, dir, on, on ? 60 : SmartStep.SmartTiles, stop, tiles);
            return (end, tiles);
        }

        [Test]
        public void LongPath_StopsAfterFiveTiles()
        {
            var (end, tiles) = Plan("PPPPPPPPPPPP", Vector2Int.zero, Right);
            Assert.AreEqual(SmartStep.Cap, end);
            Assert.AreEqual(5, tiles.Count);
            Assert.AreEqual(new Vector2Int(5, 0), tiles[4]);
        }

        [Test]
        public void FollowsAnLTurn()
        {
            var (end, tiles) = Plan("GGGGG\nPPPPG\nGGGPG\nGGGPG\nGGGPG\nGGGPG", new Vector2Int(0, 1), Right);
            Assert.AreEqual(SmartStep.Cap, end);
            Assert.AreEqual(new[] { new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(3, 1), new Vector2Int(3, 2), new Vector2Int(3, 3) }, tiles.ToArray());
        }

        [Test]
        public void StopsAtAJunction()
        {
            var (end, tiles) = Plan("GGGPGGGG\nPPPPPPPP\nGGGGGGGG", new Vector2Int(0, 1), Right);
            Assert.AreEqual(SmartStep.Junction, end);
            Assert.AreEqual(new Vector2Int(3, 1), tiles[tiles.Count - 1], "stops on the junction tile");
            // A swipe from the junction leaves it.
            var (_, more) = Plan("GGGPGGGG\nPPPPPPPP\nGGGGGGGG", new Vector2Int(3, 1), Right);
            Assert.AreEqual(4, more.Count);
        }

        [Test]
        public void StopsAtTheEndOfAT()
        {
            var (end, tiles) = Plan("GGGPG\nPPPPG\nGGGPG", new Vector2Int(0, 1), Right);
            Assert.AreEqual(SmartStep.Junction, end);
            Assert.AreEqual(new Vector2Int(3, 1), tiles[tiles.Count - 1]);
        }

        [Test]
        public void WideRoadAndPlaza_AreNotJunctions()
        {
            Assert.AreEqual(SmartStep.Cap, Plan("GGGGGGGG\nPPPPPPPP\nPPPPPPPP\nGGGGGGGG", new Vector2Int(0, 1), Right).end);
            Assert.AreEqual(SmartStep.Cap, Plan("PPPPPPPP\nPPPPPPPP\nPPPPPPPP", new Vector2Int(0, 1), Right).end);
        }

        [Test]
        public void StopsOnTheFirstTallGrassTile_WalkOnDoesNot()
        {
            var (end, tiles) = Plan("PPTTTTTT", Vector2Int.zero, Right);
            Assert.AreEqual(SmartStep.Grass, end);
            Assert.AreEqual(2, tiles.Count);
            Assert.AreEqual(IslandMap.TallGrass, 'T');
            Assert.AreEqual(new Vector2Int(2, 0), tiles[1]);
            Assert.AreEqual(7, Plan("PPTTTTTT", Vector2Int.zero, Right, on: true).tiles.Count);
            // Already in the grass: it walks on through.
            Assert.AreEqual(5, Plan("TTTTTTTT", Vector2Int.zero, Right).tiles.Count);
        }

        [Test]
        public void StopsBeforeABlockedTile_AndCantMoveIntoOne()
        {
            var (end, tiles) = Plan("PPPRPP", Vector2Int.zero, Right);
            Assert.AreEqual(SmartStep.Blocked, end);
            Assert.AreEqual(2, tiles.Count);
            var (end2, none) = Plan("PRPP", Vector2Int.zero, Right);
            Assert.AreEqual(SmartStep.Blocked, end2);
            Assert.AreEqual(0, none.Count, "nothing to walk: the explorer bumps");
        }

        [Test]
        public void StopsAtInteractables()
        {
            var door = new Vector2Int(3, 0);
            var (end, tiles) = Plan("PPPPPPPP", Vector2Int.zero, Right, stop: (a, b) => b == door ? WalkStop.Before : WalkStop.None);
            Assert.AreEqual(SmartStep.Interactable, end);
            Assert.AreEqual(new Vector2Int(2, 0), tiles[tiles.Count - 1], "stops on the tile before");
            // Standing in front of it, a swipe steps on.
            Assert.AreEqual(door, Plan("PPPPPPPP", new Vector2Int(2, 0), Right, stop: (a, b) => b == door ? WalkStop.Before : WalkStop.None).tiles[0]);
            var reach = new Vector2Int(2, 0);
            var (end3, tiles3) = Plan("PPPPPPPP", Vector2Int.zero, Right, on: true, stop: (a, b) => b == reach ? WalkStop.On : WalkStop.None);
            Assert.AreEqual(SmartStep.Interactable, end3);
            Assert.AreEqual(2, tiles3.Count, "walk on stops on the tile in reach too");
        }

        [Test]
        public void OffPath_GoesStraight()
        {
            var (end, tiles) = Plan("GGGGGGGG\nGGGGGGGG", new Vector2Int(0, 1), Right);
            Assert.AreEqual(SmartStep.Cap, end);
            Assert.AreEqual(new Vector2Int(5, 1), tiles[4]);
        }

        [Test]
        public void WideRoadEnd_DoesNotDoubleBack()
        {
            // A 2-wide road ending in grass: no U-turn down the other lane.
            var (_, tiles) = Plan("GGGGGG\nPPPGGR\nPPPGGR\nGGGGGG", new Vector2Int(0, 1), Right);
            foreach (var t in tiles) Assert.IsFalse(t.x < 1, $"doubled back to c{t.x} r{t.y}");
        }

        [Test]
        public void WalkLessons_TaughtInBothSlots()
        {
            var s = UnityEditor.AssetDatabase.LoadAssetAtPath<OnboardingScript>("Assets/_Project/Data/Onboarding/Onboarding_Main.asset");
            if (s == null) Assert.Inconclusive("Onboarding_Main missing (run Setup > Walking + Guidance (12) > Set Up MR_Main (12a walking)).");
            var ids = s.lessons.ConvertAll(l => l.id);
            Assert.IsFalse(ids.Contains("stop"), "the stop lesson is folded into walk.far");
            // Nov 3: walk (4 tiles really walked, any way) then far (walk on, a tap stops it); no turn lesson, no gold tile.
            int walk = ids.IndexOf("walk"), far = ids.IndexOf("far"), look = ids.IndexOf("look");
            Assert.IsFalse(ids.Contains("turn"), string.Join(", ", ids));
            Assert.IsTrue(walk >= 0 && walk < far && far < look, string.Join(", ", ids));
            Assert.AreEqual("swipeWalked", s.lessons[walk].doneWhen);
            StringAssert.Contains("tapStopped", s.lessons[far].doneWhen);
            StringAssert.Contains("walkedOn3", s.lessons[far].doneWhen, "the stop lesson never blocks");
            Assert.IsTrue(string.IsNullOrEmpty(s.lessons[walk].goldTileRule) && string.IsNullOrEmpty(s.lessons[far].goldTileRule));
            var quick = UnityEditor.AssetDatabase.LoadAssetAtPath<QuickJourneyConfig>("Assets/_Project/Data/QuickJourney.asset");
            if (quick != null)
                foreach (var id in new[] { "walk", "far" })
                    Assert.IsFalse(QuickJourney.SkipsLesson(id, true, quick), $"the quick slot teaches {id} too");
        }

        [Test]
        public void ObjectiveLines_MarkedInTheScript_ChatterPlain()
        {
            var s = UnityEditor.AssetDatabase.LoadAssetAtPath<DialogueScript>("Assets/_Project/Data/Dialogue/Script_v1.asset");
            if (s == null) Assert.Inconclusive("Script_v1.asset missing.");
            foreach (var id in new[] { "lesson.walk", "lesson.walk.far", "gw.kites.task", "objective.more", "intro.shard" })
                Assert.IsTrue(s.Find(id) != null && s.Find(id).objective, $"{id} is an objective");
            foreach (var id in new[] { "praise.nice", "realm.lilymere.first", "spirit.poke.idle" })
                Assert.IsTrue(s.Find(id) != null && !s.Find(id).objective, $"{id} is chatter");
            Assert.IsTrue(s.Find("intro.shard").once, "the once flag still reads after objective");
            Assert.AreEqual("shard", s.Find("intro.shard").target);
        }

        [Test]
        public void DoubleSwipeWindow()
        {
            var r = new Vector2Int(1, 0);
            Assert.IsTrue(SmartStep.WalkOn(r, 1f, r, 1.4f, 0.45f, true), "same way within 0.45 s while walking");
            Assert.IsFalse(SmartStep.WalkOn(r, 1f, r, 1.5f, 0.45f, true), "too slow");
            Assert.IsFalse(SmartStep.WalkOn(r, 1f, new Vector2Int(0, 1), 1.2f, 0.45f, true), "a new direction re-plans instead");
            Assert.IsFalse(SmartStep.WalkOn(r, 1f, r, 1.2f, 0.45f, false), "standing: a fresh smart step");
        }
    }
}
