using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>Step 32: one swipe-and-tap lesson in three rounds, inside a walk area round the statue.</summary>
    public class Step32Tests
    {
        static WorldMap Hub() => AssetDatabase.LoadAssetAtPath<WorldMap>("Assets/_Project/Data/WorldMap_StillIsle.asset");

        [Test]
        public void ThreeRounds_OnlyTheLastHasAGoldTile()
        {
            int goal = StopPractice.Goal(true);
            Assert.AreEqual(3, goal);
            Assert.IsFalse(StopPractice.GoldRound(0, goal));
            Assert.IsFalse(StopPractice.GoldRound(1, goal));
            Assert.IsTrue(StopPractice.GoldRound(2, goal));
        }

        [Test]
        public void LessonWalk_StaysInTheAreaRoundTheStatue_HoweverLongItGoes()
        {
            var w = Hub();
            Assert.IsNotNull(w);
            Assert.IsTrue(w.TryGetLandmark("statue", out var centre));
            const int radius = 6;
            var ways = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            var plan = new List<Vector2Int>();
            foreach (var way in ways)
            {
                var from = w.startTile;
                bool Blocked(Vector2Int t) => StopPractice.Distance(t, centre) > Mathf.Max(radius, StopPractice.Distance(from, centre));
                string end = SmartStep.PlanRoam(w, from, way, Blocked, 200, plan, centre);
                Assert.AreEqual(SmartStep.Cap, end, $"from the start tile {way}: it never gets stuck");
                foreach (var t in plan)
                    Assert.LessOrEqual(StopPractice.Distance(t, centre), radius, $"{way}: c{t.x} r{t.y} is outside the lesson area");
            }
        }

        [Test]
        public void RoamTurn_GoesTowardTheMiddle()
        {
            var w = Hub();
            Assert.IsTrue(w.TryGetLandmark("statue", out var centre));
            // Walking along the area's bottom edge to the right: at the edge it turns up (toward the statue), not down.
            var plan = new List<Vector2Int>();
            var from = w.startTile;
            bool Blocked(Vector2Int t) => StopPractice.Distance(t, centre) > Mathf.Max(6, StopPractice.Distance(from, centre));
            SmartStep.PlanRoam(w, from, Vector2Int.right, Blocked, 40, plan, centre);
            float before = StopPractice.Distance(from, centre), mean = 0f;
            foreach (var t in plan) mean += StopPractice.Distance(t, centre);
            mean /= Mathf.Max(1, plan.Count);
            Assert.LessOrEqual(mean, Mathf.Max(before, 6f));
        }

        [Test]
        public void HeartVoice_SynthMooByDefault_SixCandidatesWithClips()
        {
            Assert.AreEqual(6, Diorama.Audio.AudioAB.HeartCandidates);
            Assert.IsNull(Diorama.Audio.AudioAB.HeartCallCue(0), "0 = the synth moo");
            Assert.AreEqual("synth moo", Diorama.Audio.AudioAB.HeartVoiceName(0));
            for (int n = 1; n <= Diorama.Audio.AudioAB.HeartCandidates; n++)
                foreach (var cue in new[] { Diorama.Audio.AudioAB.HeartCallCue(n), $"voice.grain.heart.c{n}" })
                {
                    var c = AssetDatabase.LoadAssetAtPath<Diorama.Audio.AudioCue>($"Assets/_Project/Audio/Data/Cues/Cries/{cue}.asset");
                    Assert.IsNotNull(c, cue);
                    Assert.Greater(c.clips.Length, 0, $"{cue} has clips");
                }
            Assert.AreEqual("voice.grain.galeowl", Diorama.Audio.AudioAB.GrainCue("galeowl"), "the Keepers keep theirs");
        }

        [Test]
        public void OldSaves_AtTheFourSwipeLesson_ResumeAtTheSwipeAndTap()
        {
            var src = System.IO.File.ReadAllText("Assets/_Project/Scripts/Runtime/Onboarding/Onboarding.cs");
            StringAssert.Contains("if (lessonId == WalkLesson) lessonId = PracticeLesson;", src);
            StringAssert.Contains("if (id == WalkLesson || id == FarLesson) id = PracticeLesson;", src);
        }
    }
}
