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
