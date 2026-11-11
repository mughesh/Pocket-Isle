using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>
    /// The Nov 11 panel review's Tier 1 (Assets/Docs/Panel-Review.md): the Quick Journey's ending owns the stage (R48),
    /// Keepers are never "Wild" (R49), one voice at the catch (R50), bubbles level with the head, the finale's Skip off the
    /// pods, the walk help at 12 s, and the approved Tier 2: 2 Islings at the quick slot's doors, shorter opening waits there.
    /// </summary>
    public class PanelTests
    {
        const string Runtime = "Assets/_Project/Scripts/Runtime";

        static string Source(string file) => File.ReadAllText(Path.Combine(Runtime, file));

        // ---- T1-1 ----

        [Test]
        public void QuickEnding_HoldsTheGrowthDirector()
        {
            StringAssert.Contains("!QuickJourneyDirector.EndingUp", Source("Battle/GrowthDirector.cs"), "R48: no glow, no evolve.ready while the ending is up");
            var qj = Source("Quick/QuickJourneyDirector.cs");
            StringAssert.Contains("EndingUp = true;", qj);
            StringAssert.Contains("EndingUp = false;", qj);
            Assert.IsFalse(QuickJourneyDirector.EndingUp, "a static: down between scenes");
        }

        // ---- T1-3 ----

        [Test]
        public void Keeper_IsNeverWild_InCaptions()
        {
            Assert.AreEqual("Wild Vanehare", BattleRunes.WildName("Vanehare", false));
            Assert.AreEqual("Galeowl", BattleRunes.WildName("Galeowl", true));
            Assert.AreEqual("Wild Vanehare used Gust!", BattleRunes.MoveLine("Vanehare", "Gust", true, false));
            Assert.AreEqual("Galeowl used Gale Shot!", BattleRunes.MoveLine("Galeowl", "Gale Shot", true, true));
            Assert.AreEqual("Toastling used Ember!", BattleRunes.MoveLine("Toastling", "Ember", false, false));
            Assert.AreEqual("Wild Vanehare fainted!", BattleRunes.FaintLine("Vanehare", true, false, false));
            Assert.AreEqual("Wild Vanehare is dazed!", BattleRunes.FaintLine("Vanehare", true, false, true));
            Assert.AreEqual("Galeowl is calm!", BattleRunes.FaintLine("Galeowl", true, true, false));
            Assert.AreEqual("Toastling fainted!", BattleRunes.FaintLine("Toastling", false, false, false));
            StringAssert.DoesNotContain("$\"Wild {who}", Source("Battle/BattleStage.cs"), "every caption goes through BattleRunes");
        }

        // ---- T1-4 ----

        [Test]
        public void Catch_TheSpiritWaitsForTheToast()
        {
            var src = Source("Battle/EncounterDirector.cs");
            int wait = src.IndexOf("JournalToast.Showing", System.StringComparison.Ordinal);
            int say = src.IndexOf("Dialogue.Say(\"capture.success.first\")", System.StringComparison.Ordinal);
            Assert.Greater(wait, 0, "the catch waits for the toast");
            Assert.Greater(say, wait, "R50: the Spirit's line comes after the wait");
            Assert.AreEqual(say, src.LastIndexOf("Dialogue.Say(\"capture.success.first\")", System.StringComparison.Ordinal), "said once");
        }

        // ---- T1-5 ----

        [Test]
        public void Bubbles_StayLevel_WithTheHead()
        {
            var src = Source("Dialogue/DialogueBubble.cs");
            StringAssert.DoesNotContain("LookRotation(away, Vector3.up)", src, "R43 for bubbles: the head's up with the roll out");
            StringAssert.DoesNotContain("cam.transform.position, Vector3.up)", src);
            Assert.AreEqual(2, Regex(src, @"LookRotation\([^;]*BattleHud\.LabelUp\(cam\)\)"), "both facings (placed and parked)");
        }

        static int Regex(string s, string pattern) => System.Text.RegularExpressions.Regex.Matches(s, pattern).Count;

        // ---- T1-6 ----

        [Test]
        public void FinaleSkip_SitsBesideThePods_NotOnThem()
        {
            var src = Source("Finale/FinaleDirector.cs");
            StringAssert.Contains("runes.Anchor = SkipSpot;", src, "R17: nothing over the pods");
            StringAssert.Contains("SocketBandLocal(out float minX, out float maxX", src, "the Journal tablet's rim rule");
            Assert.AreEqual(0.07f, FinaleDirector.SkipSize.x, 1e-4f, "the tablet's size is unchanged");
        }

        // ---- T1-2 ----

        [Test]
        public void WalkLesson_OffersLookAndPinch_At12s()
        {
            var s = AssetDatabase.LoadAssetAtPath<OnboardingScript>("Assets/_Project/Data/Onboarding/Onboarding_Main.asset");
            Assert.IsNotNull(s);
            var walk = s.lessons.First(l => l.id == "walk");
            Assert.AreEqual("lesson.walk.help", walk.help);
            Assert.AreEqual(12f, walk.helpAfter, 1e-3f, "the second door before a judge gives up (was 30)");
            // The far lesson has no help line; the rest keep 30.
            foreach (var l in s.lessons.Where(l => l.id != "walk" && l.id != "starter"))
                Assert.AreEqual(30f, l.helpAfter, 1e-3f, l.id);
        }

        // ---- T2-1 ----

        [Test]
        public void Temple_TwoInTheQuickSlot_ThreeEverywhereElse()
        {
            Assert.AreEqual(2, TempleRules.QuickIslings);
            Assert.AreEqual(2, TempleRules.Needed(true, true));
            Assert.AreEqual(2, TempleRules.Needed(false, true));
            Assert.AreEqual(3, TempleRules.Needed(true, false));
            Assert.AreEqual(3, TempleRules.Needed(false, false));
            Assert.IsTrue(TempleRules.CanEnter(2, TempleRules.Needed(true, true)), "the quick party of two: no warning");
            Assert.IsFalse(TempleRules.CanEnter(1, TempleRules.Needed(true, true)), "one Isling still warns");
        }

        // ---- T2-2 ----

        [Test]
        public void QuickSlot_ShorterOpeningWaits_OnlyThere()
        {
            var c = ScriptableObject.CreateInstance<QuickJourneyConfig>();
            try
            {
                c.skipAfterOverrides = new[]
                {
                    new QuickJourneyConfig.LessonTime { id = "move", skipAfter = 25f },
                    new QuickJourneyConfig.LessonTime { id = "twoHands", skipAfter = 20f },
                };
                Assert.AreEqual(25f, QuickJourney.SkipAfter("move", 45f, true, c), 1e-3f);
                Assert.AreEqual(20f, QuickJourney.SkipAfter("twoHands", 45f, true, c), 1e-3f);
                Assert.AreEqual(60f, QuickJourney.SkipAfter("far", 60f, true, c), 1e-3f, "no override: the lesson's own");
                Assert.AreEqual(45f, QuickJourney.SkipAfter("move", 45f, false, c), 1e-3f, "the full adventure keeps 45");
                Assert.AreEqual(45f, QuickJourney.SkipAfter("move", 45f, true, null), 1e-3f);
                Assert.IsFalse(QuickJourney.SkipsLesson("twoHands", true, c), "the two-hand lesson stays (resize is judged)");
            }
            finally
            {
                Object.DestroyImmediate(c);
            }
        }

        [Test]
        public void QuickSlot_Asset_Has25sMove_20sTwoHands_NoCuts()
        {
            var c = AssetDatabase.LoadAssetAtPath<QuickJourneyConfig>("Assets/_Project/Data/QuickJourney.asset");
            Assert.IsNotNull(c);
            Assert.AreEqual(25f, QuickJourney.SkipAfter("move", 45f, true, c), 1e-3f);
            Assert.AreEqual(20f, QuickJourney.SkipAfter("twoHands", 45f, true, c), 1e-3f);
            Assert.IsEmpty(c.cutLessons, "no lesson is cut in the quick slot");
            var s = AssetDatabase.LoadAssetAtPath<OnboardingScript>("Assets/_Project/Data/Onboarding/Onboarding_Main.asset");
            Assert.AreEqual(45f, s.lessons.First(l => l.id == "move").skipAfter, 1e-3f, "the full adventure's 45 s stays in the lesson");
            Assert.AreEqual(45f, s.lessons.First(l => l.id == "twoHands").skipAfter, 1e-3f);
        }

        [Test]
        public void LessonRunner_UsesTheSkipAfterHook()
        {
            var lessons = new System.Collections.Generic.List<LessonData>
            {
                new LessonData { id = "move", startWhen = "go", skipAfter = 45f },
            };
            var r = new LessonRunner(lessons, new[] { "go" }) { SkipAfter = (id, s) => id == "move" ? 2f : s };
            r.Begin();
            r.Mark("go");
            Assert.AreEqual(LessonRunner.Outcome.Started, r.Tick(1f));
            Assert.AreEqual(LessonRunner.Outcome.None, r.Tick(1f));
            Assert.AreEqual(LessonRunner.Outcome.Skipped, r.Tick(1.1f), "skipped at the hook's 2 s, not the lesson's 45");
            StringAssert.Contains("skipped after 2 s", r.LastReason);
        }

        // ---- the tower hall (T1-7): asset values only ----

        [Test]
        public void TowerHall_RestoredPalette_IsWarmerThanSlate()
        {
            var t = AssetDatabase.LoadAssetAtPath<RealmTheme>("Assets/_Project/Data/Interiors/TowerHallTheme.asset");
            Assert.IsNotNull(t);
            Assert.Greater(t.restored.path.r, t.restored.path.b + 0.05f, "a cream path band, not blue-grey");
            Assert.Greater(t.restored.path.r, t.restored.grass.r + 0.1f, "the band reads against the floor");
            Assert.GreaterOrEqual(t.lightColor.r, t.lightColor.b, "warm light, not blue");
        }
    }
}
