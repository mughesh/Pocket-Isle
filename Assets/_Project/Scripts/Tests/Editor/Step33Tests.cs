using System.IO;
using NUnit.Framework;

namespace Diorama.Tests
{
    /// <summary>Step 33: small fixes from the Nov 12 night friend test.</summary>
    public class Step33Tests
    {
        static string Src(string file) => File.ReadAllText("Assets/_Project/Scripts/Runtime/" + file);

        [Test]
        public void PokeRelease_FreesTheHandOnlyWhenTheFingerIsWellOffForAMoment()
        {
            Assert.IsFalse(PokeRelease.ShouldRelease(0.02f, 1f, 0.03f, 0.2f), "still on the button");
            Assert.IsFalse(PokeRelease.ShouldRelease(0.05f, 0.1f, 0.03f, 0.2f), "only just off");
            Assert.IsTrue(PokeRelease.ShouldRelease(0.05f, 0.25f, 0.03f, 0.2f));
        }

        [Test]
        public void IslandGrab_InBattle_NoBlanketLock_CradleKeepOutSmall()
        {
            // Nov 15: the 12 cm 'battle' lock held the near-right corner (beside the catch pod) off all battle.
            var src = Src("IslandHandle.cs");
            StringAssert.DoesNotContain("SetGrabLock(\"battle\"", src);
            StringAssert.Contains("public float cradleClearance = 0.03f;", src);
        }

        [Test]
        public void TitleSettings_AsBigAsTheOtherChoices()
        {
            var src = Src("Title/TitleScreen.cs");
            StringAssert.DoesNotContain("\"Settings\", new Vector2(0.109f, -0.004f), Small", src);
            StringAssert.Contains("runes.Add(\"Settings\", new Vector2(0.124f, 0f), Wide, OpenSettings);", src);
        }

        [Test]
        public void SweepLesson_NotEndedByARestingPinch_GhostNotFadedByAHand()
        {
            StringAssert.Contains("if (_lookHeldFor < lookHoldLessonSeconds) return;", Src("Puzzles/Galewick/CloudBankPiece.cs"));
            StringAssert.Contains("_request.clip != \"palm_sweep\"", Src("Onboarding/GhostHands.cs"));
        }

        [Test]
        public void TempleGate_ShutUntilTheKeeperIsCalmedAndItsShardTaken()
        {
            StringAssert.Contains("return !(k.Calmed && k.ShardTaken);", Src("Puzzles/KeeperHall.cs"));
            StringAssert.Contains("KeeperHall.GateLocked(_scanned, tile)", Src("ShardGates.cs"));
        }

        [Test]
        public void PinchIcon_BackToTheTwoStrokes_ThePalmVersionReadAsGibberish()
        {
            // Nov 13: Mugesh preferred the earlier icon; the palm redraw is reverted.
            StringAssert.DoesNotContain("var palm = new Vector3(-0.42f, 0f, 0f);", Src("Dialogue/PinchIcon.cs"));
        }
    }
}
