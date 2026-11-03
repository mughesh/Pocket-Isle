using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>Step 9g: save slots, hold-to-confirm, the logo's budget.</summary>
    public class Step9gTests
    {
        [Test]
        public void Slots_NoSave_IsANewAdventure()
        {
            Assert.AreEqual(SaveSlot.Adventure, SaveSlots.Pick(false, false, null));
            Assert.AreEqual(SaveSlot.Adventure, SaveSlots.Pick(false, false, SaveSlot.Quick));
        }

        [Test]
        public void Slots_ContinueResumesTheLastPlayedSlot_WhenItHasASave()
        {
            Assert.AreEqual(SaveSlot.Quick, SaveSlots.Pick(true, true, SaveSlot.Quick));
            Assert.AreEqual(SaveSlot.Adventure, SaveSlots.Pick(true, true, SaveSlot.Adventure));
            Assert.AreEqual(SaveSlot.Quick, SaveSlots.Pick(false, true, null));
        }

        [Test]
        public void Slots_LastPlayedWithoutASave_FallsBackToTheSlotThatHasOne()
        {
            Assert.AreEqual(SaveSlot.Adventure, SaveSlots.Pick(true, false, SaveSlot.Quick));
            Assert.AreEqual(SaveSlot.Quick, SaveSlots.Pick(false, true, SaveSlot.Adventure));
        }

        [Test]
        public void Slots_HaveTheirOwnFiles()
        {
            Assert.AreEqual("save.json", SaveSlots.FileOf(SaveSlot.Adventure));
            Assert.AreEqual("quick.json", SaveSlots.FileOf(SaveSlot.Quick));
            Assert.AreNotEqual(SaveSlots.PathOf(SaveSlot.Adventure), SaveSlots.PathOf(SaveSlot.Quick));
        }

        [Test]
        public void Hold_NeedsTwoSecondsKept_AndLettingGoStartsOver()
        {
            float fill = 0f;
            for (int i = 0; i < 19; i++) fill = RuneBoard.HoldStep(fill, true, 0.1f, 2f);
            Assert.Less(fill, 1f, "1.9 s is not enough");
            fill = RuneBoard.HoldStep(fill, true, 0.1f, 2f);
            Assert.AreEqual(1f, fill, 1e-4f, "2 s confirms");
            fill = RuneBoard.HoldStep(0.8f, false, 0.1f, 2f);
            Assert.AreEqual(0f, fill, "letting go empties it");
        }

        [Test]
        public void Settings_PersistInPlayerPrefs_UnderTheBusNames()
        {
            int music = PlayerPrefs.GetInt(VolumeSettings.Music, -1);
            var hand = GameSettings.DominantHand;
            bool fewer = GameSettings.FewerHints;
            try
            {
                VolumeSettings.SetLevel(VolumeSettings.Music, 1);
                Assert.AreEqual(1, PlayerPrefs.GetInt("Music"), "saved under the bus name");
                Assert.AreEqual(0.25f, VolumeSettings.Volume(VolumeSettings.Music), 1e-4f);
                VolumeSettings.SetLevel(VolumeSettings.Music, 9);
                Assert.AreEqual(VolumeSettings.MaxLevel, VolumeSettings.Level(VolumeSettings.Music), "clamped");
                GameSettings.FewerHints = true;
                Assert.IsTrue(GameSettings.FewerHints);
                GameSettings.DominantHand = GameSettings.Hand.Left;
                Assert.IsTrue(DominantHand.Left && DominantHand.Known, "Left overrides the first touch");
                GameSettings.DominantHand = GameSettings.Hand.Right;
                Assert.IsFalse(DominantHand.Left);
            }
            finally
            {
                if (music < 0) PlayerPrefs.DeleteKey(VolumeSettings.Music);
                else PlayerPrefs.SetInt(VolumeSettings.Music, music);
                GameSettings.DominantHand = hand;
                GameSettings.FewerHints = fewer;
            }
        }

        [Test]
        public void Settings_AutoHand_KeepsTheFirstTouchRule()
        {
            var hand = GameSettings.DominantHand;
            try
            {
                GameSettings.DominantHand = GameSettings.Hand.Auto;
                DominantHand.Restore("left");
                Assert.IsTrue(DominantHand.Left);
                DominantHand.Restore(null);
                Assert.IsFalse(DominantHand.Known);
            }
            finally
            {
                GameSettings.DominantHand = hand;
                DominantHand.Restore(null);
            }
        }

        [Test]
        public void Gift_OnlyInTheQuickSlot_Once_BeforeAnyRealm()
        {
            Assert.IsTrue(QuickJourney.GiftAllowed(true, false, false), "quick slot, first time in the hub");
            Assert.IsFalse(QuickJourney.GiftAllowed(false, false, false), "never in the full adventure");
            Assert.IsFalse(QuickJourney.GiftAllowed(true, true, false), "only once");
            Assert.IsFalse(QuickJourney.GiftAllowed(true, false, true), "not late, after a realm (Mugesh: wind realm first, then 2 more Islings)");
        }

        [Test]
        public void Quick_DialsApplyOnlyInTheQuickSlot()
        {
            var c = ScriptableObject.CreateInstance<QuickJourneyConfig>();
            try
            {
                Assert.AreEqual(0.125f, QuickJourney.EncounterChance(0.125f, false, c), 1e-6f);
                Assert.AreEqual(0.0625f, QuickJourney.EncounterChance(0.125f, true, c), 1e-6f);
                Assert.AreEqual(100, QuickJourney.Reward(100, false, c));
                Assert.AreEqual(200, QuickJourney.Reward(100, true, c));
                Assert.AreEqual(1f, QuickJourney.RareScaleFor(false, c));
                Assert.AreEqual(0f, QuickJourney.RareScaleFor(true, c));
                Assert.IsFalse(QuickJourney.SkipsLesson("stop", false, c));
                Assert.IsFalse(QuickJourney.SkipsLesson("far", true, c), "step 12a: the walk lessons are taught in both slots");
                Assert.IsTrue(QuickJourney.SkipsLesson("twoHands", true, c));
                Assert.IsFalse(QuickJourney.SkipsLesson("walk", true, c), "hand-verb lessons stay");
                Assert.AreEqual(0.125f, QuickJourney.EncounterChance(0.125f, true, null), 1e-6f, "no config, no dials");
            }
            finally
            {
                Object.DestroyImmediate(c);
            }
        }

        [Test]
        public void Quick_LessonRunner_PassesOverCutLessons()
        {
            var lessons = new System.Collections.Generic.List<LessonData>
            {
                new LessonData { id = "walk", doneWhen = "walked3" },
                new LessonData { id = "stop", doneWhen = "stopped" },
                new LessonData { id = "look", doneWhen = "gazeWalk" },
            };
            var runner = new LessonRunner(lessons, new[] { "walked3", "stopped", "gazeWalk" }) { Skip = id => id == "stop" };
            runner.Resume("stop");
            Assert.AreEqual("look", runner.Current.id, "a saved cut lesson resumes at the next one");
            runner.Begin(0);
            Assert.AreEqual("walk", runner.Current.id);
        }

        [Test]
        public void Quick_Config_GivesToastlingAndWhirlby_ForGalewick()
        {
            var c = AssetDatabase.LoadAssetAtPath<QuickJourneyConfig>("Assets/_Project/Data/QuickJourney.asset");
            if (c == null) Assert.Inconclusive("Run Step9gSetup.QuickOnly first.");
            // Step 14c: a Galewick journey: fire beats wind, and a wind friend.
            CollectionAssert.AreEquivalent(new[] { "Toastling", "Whirlby" }, System.Array.ConvertAll(c.giftSpecies, s => s != null ? s.name : null));
            Assert.AreEqual(8, c.giftLevel, "the default gift is Lv 8");
            foreach (var n in new[] { "Kilnpot", "Pebbloop", "Pufflet" })
            {
                var s = AssetDatabase.LoadAssetAtPath<IslingSpecies>($"Assets/_Project/Data/Creatures/Species/{n}.asset");
                var (gift, raise) = c.LevelsFor(s);
                Assert.That(gift, Is.InRange(5, 10), $"{n} gift level");
                Assert.That(raise, Is.InRange(6, 11), $"{n} raise level");
            }
            Assert.Less(c.encounterRate, 1f);
            Assert.Greater(c.xpMultiplier, 1f);
        }

        [Test]
        public void Logo_IsBaked_UnderTheBudget()
        {
            var logo = AssetDatabase.LoadAssetAtPath<PropRecipe>("Assets/_Project/Data/Props/TitleLogo.asset");
            if (logo == null || logo.bakedMesh == null) Assert.Inconclusive("Run Step9gSetup.TitleOnly first.");
            Assert.LessOrEqual(logo.bakedMesh.triangles.Length / 3, 8000);
            var size = logo.bakedMesh.bounds.size;
            Assert.Greater(size.x, size.y, "wider than tall");
        }
    }
}
