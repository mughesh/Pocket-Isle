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
