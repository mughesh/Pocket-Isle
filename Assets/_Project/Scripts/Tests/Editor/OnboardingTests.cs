using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>
    /// Step 8b: the lesson chain (order, done ahead of time, skips, rewinds, resume), line expiry when a battle starts,
    /// wild battles gated by the tutorial, and the lesson in the save.
    /// </summary>
    public class OnboardingTests
    {
        readonly List<Object> _made = new List<Object>();
        bool _battle, _canWalk;

        [SetUp]
        public void SetUp()
        {
            GameConditions.Reset();
            _battle = false;
            _canWalk = true;
            GameConditions.Register("battle", () => _battle);
            GameConditions.Register("canWalk", () => _canWalk);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _made)
                if (o != null) Object.DestroyImmediate(o);
            _made.Clear();
            GameConditions.Reset();
        }

        static readonly string[] Events = { "walked3", "stopped", "gazeWalk", "grassBattle", "sentOut", "wonOrCaught", "battleFailed" };

        static List<LessonData> Chain() => new List<LessonData>
        {
            new LessonData { id = "walk", startWhen = "canWalk", doneWhen = "walked3" },
            new LessonData { id = "stop", startWhen = "canWalk", doneWhen = "stopped" },
            new LessonData { id = "look", startWhen = "canWalk", doneWhen = "gazeWalk", skipAfter = 5f },
            new LessonData { id = "grass", startWhen = "canWalk", doneWhen = "grassBattle" },
            new LessonData { id = "send", startWhen = "battle", validWhile = "battle", doneWhen = "sentOut", failWhen = "battleFailed", rewindTo = "grass" },
            new LessonData { id = "free", doneWhen = "wonOrCaught", failWhen = "battleFailed", rewindTo = "grass" },
        };

        [Test]
        public void Lessons_RunInOrder_OneAtATime()
        {
            var r = new LessonRunner(Chain(), Events);
            r.Begin();
            Assert.AreEqual(LessonRunner.Outcome.Started, r.Tick(0.1f));
            Assert.AreEqual("walk", r.Current.id);
            Assert.AreEqual(LessonRunner.Outcome.None, r.Tick(0.1f), "waits for its done event");
            r.Mark("walked3");
            Assert.AreEqual(LessonRunner.Outcome.Done, r.Tick(0.1f));
            Assert.AreEqual("stop", r.Current.id);
            Assert.AreEqual(LessonRunner.Phase.Waiting, r.State);
            Assert.AreEqual(LessonRunner.Outcome.Started, r.Tick(0.1f));
        }

        [Test]
        public void Lessons_WaitForTheirStartCondition()
        {
            var r = new LessonRunner(Chain(), Events);
            _canWalk = false;
            r.Begin();
            for (int i = 0; i < 10; i++) Assert.AreEqual(LessonRunner.Outcome.None, r.Tick(0.1f));
            Assert.AreEqual(LessonRunner.Phase.Waiting, r.State);
            _canWalk = true;
            Assert.AreEqual(LessonRunner.Outcome.Started, r.Tick(0.1f));
        }

        [Test]
        public void SomethingDoneEarly_CompletesItsLessonWhenItComesUp()
        {
            var r = new LessonRunner(Chain(), Events);
            r.Begin();
            r.Tick(0.1f);
            r.Mark("stopped"); // tapped to stop during the walk lesson
            r.Mark("walked3");
            Assert.AreEqual(LessonRunner.Outcome.Done, r.Tick(0.1f));
            Assert.AreEqual("stop", r.Current.id);
            Assert.AreEqual(LessonRunner.Outcome.Done, r.Tick(0.1f), "done on arrival, no teaching");
            Assert.AreEqual("done ahead", r.LastReason);
            Assert.AreEqual("look", r.Current.id);
        }

        [Test]
        public void Lesson_SkipsAfterItsTime_AndWhenItStopsBeingValid()
        {
            var r = new LessonRunner(Chain(), Events);
            r.Begin(2); // look, skipAfter 5
            Assert.AreEqual(LessonRunner.Outcome.Started, r.Tick(0.1f));
            for (int i = 0; i < 49; i++) Assert.AreEqual(LessonRunner.Outcome.None, r.Tick(0.1f));
            Assert.AreEqual(LessonRunner.Outcome.Skipped, r.Tick(0.2f));
            Assert.AreEqual("grass", r.Current.id);

            r.Begin(4); // send: only valid in battle
            _battle = true;
            Assert.AreEqual(LessonRunner.Outcome.Started, r.Tick(0.1f));
            _battle = false;
            Assert.AreEqual(LessonRunner.Outcome.Skipped, r.Tick(0.1f));
            Assert.AreEqual("free", r.Current.id);

            r.Begin(4); // not in battle yet: waits, isn't skipped before its moment
            for (int i = 0; i < 10; i++) Assert.AreEqual(LessonRunner.Outcome.None, r.Tick(0.1f));
            Assert.AreEqual("send", r.Current.id);
        }

        [Test]
        public void LostOrFledBattle_RewindsToTheGrassLesson_AndForgetsItsEvents()
        {
            var r = new LessonRunner(Chain(), Events);
            r.Begin(3);
            r.Tick(0.1f);
            r.Mark("grassBattle");
            _battle = true;
            Assert.AreEqual(LessonRunner.Outcome.Done, r.Tick(0.1f));
            Assert.AreEqual(LessonRunner.Outcome.Started, r.Tick(0.1f));
            Assert.AreEqual("send", r.Current.id);
            r.Mark("battleFailed");
            Assert.AreEqual(LessonRunner.Outcome.Rewound, r.Tick(0.1f));
            Assert.AreEqual("grass", r.Current.id);
            Assert.IsFalse(r.Happened("grassBattle"), "the grass lesson needs a new encounter");
            Assert.IsFalse(r.Happened("battleFailed"), "no second rewind from the old failure");
        }

        [Test]
        public void Resume_PicksUpTheSavedLesson_BattleLessonsGoBackToTheGrass()
        {
            var r = new LessonRunner(Chain(), Events);
            r.Resume("stop");
            Assert.AreEqual("stop", r.Current.id);
            r.Resume("send");
            Assert.AreEqual("grass", r.Current.id, "the battle is gone after a reload");
            r.Resume("free");
            Assert.AreEqual("grass", r.Current.id);
            r.Resume("nonsense");
            Assert.AreEqual("walk", r.Current.id);
        }

        [Test]
        public void LastLessonDone_FinishesTheChain()
        {
            var r = new LessonRunner(Chain(), Events);
            r.Begin(5);
            r.Mark("wonOrCaught");
            Assert.AreEqual(LessonRunner.Outcome.Done, r.Tick(0.1f));
            Assert.IsTrue(r.IsFinished);
            Assert.IsFalse(r.IsRunning);
            Assert.AreEqual(LessonRunner.Outcome.None, r.Tick(0.1f));
        }

        [Test]
        public void Conditions_NotAndOr()
        {
            Assert.IsTrue(GameConditions.Check(""));
            Assert.IsTrue(GameConditions.Check("canWalk & !battle"));
            _battle = true;
            Assert.IsFalse(GameConditions.Check("canWalk & !battle"));
            Assert.IsTrue(GameConditions.Check("!canWalk | battle"));
            Assert.IsFalse(GameConditions.IsKnown("nope"), "unknown names read false and log an error");
            Assert.IsTrue(GameConditions.Check("walked3", n => n == "walked3" ? true : (bool?)null), "lesson events answer first");
        }

        // ---- dialogue expiry ----

        Dialogue MakeDialogue()
        {
            var script = ScriptableObject.CreateInstance<DialogueScript>();
            _made.Add(script);
            DialogueTrigger T(string id, int priority, string validWhile, bool once = false) => new DialogueTrigger
            {
                id = id, priority = priority, validWhile = validWhile, once = once,
                lines = new List<DialogueLine> { new DialogueLine { text = id + " line" } },
            };
            script.triggers.Add(T("hint", 1, null));
            script.triggers.Add(T("walkHint", 1, "!battle"));
            script.triggers.Add(T("battleHint", 1, "battle"));
            script.triggers.Add(T("story", 2, null));
            script.triggers.Add(T("onceHint", 1, null, true));
            var go = new GameObject("Dialogue Test");
            _made.Add(go);
            var d = go.AddComponent<Dialogue>();
            d.script = script;
            return d;
        }

        [Test]
        public void BattleStart_ClosesHintLines_KeepsStoryAndBattleLines()
        {
            var d = MakeDialogue();
            d.Fire("hint", false);
            Assert.AreEqual("hint", d.CurrentTrigger);
            _battle = true;
            d.Expire("battle started");
            Assert.IsNull(d.CurrentTrigger, "a plain hint closes at once");

            d.Fire("battleHint", false);
            Assert.AreEqual("battleHint", d.CurrentTrigger);
            d.Expire("battle started");
            Assert.AreEqual("battleHint", d.CurrentTrigger, "a line that says it holds in battle stays");

            d.ClearAll();
            d.Fire("story", false);
            d.Expire("battle started");
            Assert.AreEqual("story", d.CurrentTrigger, "story lines are not hints");
        }

        [Test]
        public void ControlHint_NeverShowsInBattle()
        {
            var d = MakeDialogue();
            _battle = true;
            d.Fire("walkHint", false);
            Assert.IsNull(d.CurrentTrigger, "not queued while its condition is false");
            _battle = false;
            d.Fire("walkHint", false);
            Assert.AreEqual("walkHint", d.CurrentTrigger);
        }

        [Test]
        public void OnceHint_DroppedBeforeItShowed_CanComeBack()
        {
            var d = MakeDialogue();
            d.Fire("story", false);
            d.Fire("onceHint", false); // waits behind the story line
            _battle = true;
            d.Expire("battle started");
            Assert.AreEqual("story", d.CurrentTrigger);
            d.ClearAll();
            _battle = false;
            d.Fire("onceHint", false);
            Assert.AreEqual("onceHint", d.CurrentTrigger, "it never showed, so 'once' didn't use it up");
        }

        // ---- encounters gated, save ----

        [Test]
        public void NewGame_TurnsWildBattlesOff_UntilTheTutorialIsDone()
        {
            var script = ScriptableObject.CreateInstance<OnboardingScript>();
            _made.Add(script);
            script.lessons = Chain();
            var go = new GameObject("Onboarding Test");
            _made.Add(go);
            var o = go.AddComponent<Onboarding>();
            o.script = script;
            typeof(Onboarding).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(o, null);
            try
            {
                Assert.IsTrue(Onboarding.EncountersEnabled, "on by default (no tutorial)");
                o.Begin();
                Assert.IsFalse(Onboarding.EncountersEnabled, "new game: tall grass only rustles");
                Assert.AreEqual("walk", o.CurrentId);
                Assert.IsFalse(Onboarding.IsTutorialGrass(Vector2Int.zero), "not the grass lesson");
                o.DebugFinish();
                Assert.IsTrue(Onboarding.EncountersEnabled);
                Assert.IsNull(o.CurrentId, "a finished tutorial saves as done");

                o.Restore("stop", false);
                Assert.IsFalse(Onboarding.EncountersEnabled, "resumed mid-tutorial");
                Assert.AreEqual("stop", o.CurrentId);
                o.Restore(null, true);
                Assert.IsTrue(Onboarding.EncountersEnabled, "returning player who finished it");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
            Assert.IsTrue(Onboarding.EncountersEnabled, "no Onboarding left: battles on");
        }

        [Test]
        public void Save_KeepsTheLesson_OldSavesCountTheTutorialAsDone()
        {
            string dir = Path.Combine(Path.GetTempPath(), "PocketIsleOnboardingTests_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                string path = Path.Combine(dir, SaveGame.FileName);
                SaveGame.Write(path, new SaveData { lesson = "stop", tutorialDone = false });
                Assert.AreEqual(SaveGame.ReadResult.Loaded, SaveGame.Read(path, out var data, out _));
                Assert.AreEqual("stop", data.lesson);
                Assert.IsFalse(data.tutorialDone);

                File.WriteAllText(path, "{\"version\":2,\"map\":\"WorldMap_StillIsle\"}");
                Assert.AreEqual(SaveGame.ReadResult.Loaded, SaveGame.Read(path, out data, out _));
                Assert.IsTrue(data.tutorialDone, "step 8 players never see the lessons");
                Assert.AreEqual(SaveData.CurrentVersion, data.version);
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }
    }
}
