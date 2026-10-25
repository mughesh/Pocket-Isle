using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>
    /// Step 9b hand puzzle kit: pump stroke counting and cooling, carry snap / fall-back and the heavy follow, puzzle
    /// state through the save, and the map's runtime changes (a lifted log opens its tiles without touching the asset).
    /// </summary>
    public class PuzzleTests
    {
        static PumpStrokes Pump() => new PumpStrokes { needed = 12, checkpoint = 4, coolDelay = 4f, coolStep = 2f, downAt = 0.8f, upAt = 0.3f };

        [Test]
        public void Pump_StrokeIsDownPast80ThenBackAbove30()
        {
            var p = Pump();
            Assert.IsFalse(p.Feed(0f));
            Assert.IsFalse(p.Feed(0.7f), "70% isn't down yet");
            Assert.IsFalse(p.Feed(0.2f), "back up without reaching 80%: no stroke");
            Assert.AreEqual(0, p.Strokes);
            Assert.IsFalse(p.Feed(0.85f));
            Assert.IsFalse(p.Feed(0.5f), "halfway back up isn't a stroke yet");
            Assert.IsTrue(p.Feed(0.25f), "down past 80%, back past 30%: one stroke");
            Assert.AreEqual(1, p.Strokes);
            Assert.IsFalse(p.Feed(0.1f), "no double count while it stays up");
            Assert.AreEqual(1, p.Strokes);
        }

        [Test]
        public void Pump_CoolsAfterAPause_NeverBelowALitBrazier()
        {
            var p = Pump();
            for (int i = 0; i < 6; i++) p.Add();
            Assert.AreEqual(1, p.Checkpoints, "4 strokes lit the first brazier");
            Assert.IsFalse(Tick(p, 3.9f), "under 4 s: nothing cools");
            Assert.IsFalse(Tick(p, 2.0f), "4 s idle, then the first step needs 2 more");
            Assert.AreEqual(6, p.Strokes);
            Assert.IsTrue(Tick(p, 0.2f), "6 s idle: one step back");
            Assert.AreEqual(5, p.Strokes);
            Tick(p, 30f);
            Assert.AreEqual(4, p.Strokes, "never below the lit brazier");
            p.Add();
            Assert.AreEqual(0f, p.Idle, 1e-4f, "a stroke resets the pause");
        }

        [Test]
        public void Pump_DoneAfterNeeded_StopsCounting_AndRestoresAtACheckpoint()
        {
            var p = Pump();
            for (int i = 0; i < 12; i++) Assert.IsTrue(p.Add());
            Assert.IsTrue(p.Done);
            Assert.AreEqual(3, p.Checkpoints);
            Assert.IsFalse(p.Add());
            Assert.IsFalse(Tick(p, 60f), "solved: no cooling");
            var q = Pump();
            q.Restore(2 * 4);
            Assert.AreEqual(8, q.Strokes);
            Assert.AreEqual(2, q.Checkpoints);
            Assert.IsFalse(Tick(q, 60f), "a restored checkpoint doesn't cool");
        }

        static bool Tick(PumpStrokes p, float seconds)
        {
            bool any = false;
            for (float t = 0f; t < seconds - 1e-4f; t += 0.05f) any |= p.Tick(Mathf.Min(0.05f, seconds - t));
            return any;
        }

        [Test]
        public void Carry_SnapsToTheNearestSpotInReach_ElseFallsBack()
        {
            var spots = new List<Vector2> { new Vector2(18.5f, 14f), new Vector2(22.5f, 14f) };
            Assert.AreEqual(0, CarryLogic.PickSpot(new Vector2(18.9f, 14.6f), spots, 1.3f));
            Assert.AreEqual(1, CarryLogic.PickSpot(new Vector2(21.8f, 13.5f), spots, 1.3f));
            Assert.AreEqual(-1, CarryLogic.PickSpot(new Vector2(20.5f, 14f), spots, 1.3f), "over the gap between them: falls back");
            Assert.AreEqual(-1, CarryLogic.PickSpot(new Vector2(18.5f, 16f), spots, 1.3f), "too far: falls back");
            Assert.AreEqual(-1, CarryLogic.PickSpot(Vector2.zero, null, 1.3f));
        }

        [Test]
        public void Carry_HeavyFollowLagsAndIsCapped()
        {
            var at = Vector3.zero;
            var target = new Vector3(2f, 1f, 0f);
            var next = CarryLogic.Follow(at, target, 0.22f, 7f, 1f / 72f);
            Assert.Less(next.magnitude, 0.15f, "one frame covers only a little of the way");
            Assert.Greater(next.magnitude, 0f);
            for (int i = 0; i < 200; i++) at = CarryLogic.Follow(at, target, 0.22f, 7f, 1f / 72f);
            Assert.Less((at - target).magnitude, 0.01f, "gets there");
            var capped = CarryLogic.Follow(Vector3.zero, new Vector3(100f, 0f, 0f), 0.01f, 7f, 0.1f);
            Assert.AreEqual(0.7f, capped.magnitude, 1e-3f, "no faster than max speed");
            Assert.AreEqual(0.5f, CarryLogic.DistanceToSegment(new Vector3(0f, 0.5f, 0f), Vector3.left, Vector3.right), 1e-5f);
        }

        [Test]
        public void PuzzleState_SurvivesTheSave_AndOldSavesHaveNone()
        {
            try
            {
                PuzzleState.Restore(null);
                PuzzleState.Solve("cinder.log", 2);
                PuzzleState.SetStage("cinder.bellows", 2);
                var data = new SaveData { puzzles = PuzzleState.ToSaved() };
                var back = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data));
                PuzzleState.Restore(null);
                Assert.IsFalse(PuzzleState.IsSolved("cinder.log"));
                PuzzleState.Restore(back.puzzles);
                Assert.IsTrue(PuzzleState.IsSolved("cinder.log"));
                Assert.AreEqual(2, PuzzleState.Stage("cinder.log"), "the spot it lies on");
                Assert.IsFalse(PuzzleState.IsSolved("cinder.bellows"));
                Assert.AreEqual(2, PuzzleState.Stage("cinder.bellows"), "two lit braziers");

                var old = JsonUtility.FromJson<SaveData>("{\"version\":3,\"map\":\"WorldMap_Cinderwood\"}");
                PuzzleState.Restore(old.puzzles);
                Assert.IsFalse(PuzzleState.IsSolved("cinder.log"), "a save from before 9b: nothing solved");
            }
            finally
            {
                PuzzleState.Restore(null);
            }
        }

        [Test]
        public void Map_RuntimeOpenAndHide_NeverTouchTheAsset()
        {
            var recipe = ScriptableObject.CreateInstance<PropRecipe>();
            recipe.blocksWalking = true;
            recipe.footprint = new Vector2Int(2, 1);
            var map = ScriptableObject.CreateInstance<WorldMap>();
            try
            {
                map.SetLayout("GGGG\nGGGG");
                map.propLegend = new List<PropLegend> { new PropLegend { key = "o", recipe = recipe } };
                map.SetProps(".o..\n....");
                string props = map.props;
                var anchor = new Vector2Int(1, 0);
                Assert.IsFalse(map.CanWalk(anchor));
                Assert.IsFalse(map.CanWalk(new Vector2Int(2, 0)), "both log tiles block");
                int v = map.version;
                map.SetPropOpen(anchor, true);
                Assert.Greater(map.version, v, "the island rebuilds");
                Assert.IsTrue(map.CanWalk(anchor));
                Assert.IsTrue(map.CanWalk(new Vector2Int(2, 0)));
                map.SetPropHidden(anchor, true);
                Assert.IsTrue(map.IsPropHidden(anchor));
                Assert.AreEqual(props, map.props, "the props text (the asset) is unchanged");
                map.ClearRuntimeChanges();
                Assert.IsFalse(map.CanWalk(anchor), "cleared: it blocks again");
                Assert.IsFalse(map.IsPropHidden(anchor));
            }
            finally
            {
                Object.DestroyImmediate(map);
                Object.DestroyImmediate(recipe);
            }
        }
    }
}
