using Diorama.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>Step 14f: the last walk lesson can't trap anyone, the forehead crystal.</summary>
    public class Step14fTests
    {
        static OnboardingScript Lessons()
        {
            var s = AssetDatabase.LoadAssetAtPath<OnboardingScript>("Assets/_Project/Data/Onboarding/Onboarding_Main.asset");
            if (s == null) Assert.Inconclusive("Onboarding_Main missing (Set Up MR_Main (14f menus and fixes)).");
            return s;
        }

        [Test]
        public void FarLesson_SwipeAndTap_ThreeWaysDone_NeverTeachesTwice()
        {
            var s = Lessons();
            var far = s.lessons.Find(l => l.id == Onboarding.FarLesson);
            Assert.IsNotNull(far);
            foreach (var e in new[] { "tapStopped", "atGoldTile", "swipeWalks2" })
                StringAssert.Contains(e, far.doneWhen);
            Assert.IsTrue(far.skipAfter > 0f && far.skipAfter <= 30f, $"passes itself within 30 s ({far.skipAfter})");
            Assert.AreEqual("ahead", far.goldTileRule);
            Assert.AreEqual(Step12aSetup.SwipeTapClip, far.ghost);
            Assert.IsFalse(far.chip.ToLowerInvariant().Contains("twice"), far.chip);
            Assert.IsTrue(System.Array.IndexOf(Onboarding.Events, "swipeWalks2") >= 0);
            foreach (var l in s.lessons)
                Assert.AreNotEqual(Step12aSetup.SwipeTwiceClip, l.ghost, $"lesson {l.id} teaches swiping twice");

            var script = AssetDatabase.LoadAssetAtPath<DialogueScript>("Assets/_Project/Data/Dialogue/Script_v1.asset");
            if (script == null) return;
            foreach (var t in script.triggers)
                if (t.id.StartsWith("lesson."))
                    foreach (var line in t.lines)
                        Assert.IsFalse(line.text.ToLowerInvariant().Contains("twice"), $"{t.id}: '{line.text}'");
        }

        [Test]
        public void FarLesson_PassesAfter30s_WithNothingDone()
        {
            var far = Lessons().lessons.Find(l => l.id == Onboarding.FarLesson);
            var lessons = new System.Collections.Generic.List<LessonData> { new LessonData { id = far.id, doneWhen = far.doneWhen, skipAfter = far.skipAfter } };
            var runner = new LessonRunner(lessons, Onboarding.Events);
            runner.Begin(0);
            Assert.AreEqual(LessonRunner.Outcome.Started, runner.Tick(0.1f));
            var last = LessonRunner.Outcome.None;
            for (float t = 0f; t < 31f && runner.IsRunning; t += 0.1f) last = runner.Tick(0.1f);
            Assert.IsFalse(runner.IsRunning, "still stuck after 31 s");
            Assert.AreEqual(LessonRunner.Outcome.Skipped, last);
        }

        [Test]
        public void FarLesson_DoneByATapStop()
        {
            var far = Lessons().lessons.Find(l => l.id == Onboarding.FarLesson);
            var lessons = new System.Collections.Generic.List<LessonData> { new LessonData { id = far.id, doneWhen = far.doneWhen } };
            GameConditions.Register("atGoldTile", () => false);
            try
            {
                var runner = new LessonRunner(lessons, Onboarding.Events);
                runner.Begin(0);
                runner.Tick(0.1f);
                Assert.AreEqual(LessonRunner.Outcome.None, runner.Tick(0.1f));
                runner.Mark("tapStopped");
                Assert.AreEqual(LessonRunner.Outcome.Done, runner.Tick(0.1f));
            }
            finally
            {
                GameConditions.Unregister("atGoldTile");
            }
        }

        [Test]
        public void HeartCrystal_Baked_Small()
        {
            var r = PropKit.Load(PropKit.HeartCrystalName);
            if (r == null) Assert.Inconclusive("HeartCrystal not baked (Set Up MR_Main (14f menus and fixes)).");
            int tris = r.bakedMesh != null ? r.bakedMesh.triangles.Length / 3 : 0;
            Assert.IsTrue(tris > 0 && tris <= 2000, $"{tris} tris");
        }
    }
}
