using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>Nov 18: the heartbeat stops with the finale; Galewick's arrival clouds can't be walked round; a poke stuck
    /// on a button that went away mid-press is freed.</summary>
    public class Nov18Tests
    {
        static string Src(string file) => File.ReadAllText("Assets/_Project/Scripts/Runtime/" + file);

        [Test]
        public void Heartbeat_IsNeverAFireAndForgetLoop()
        {
            var src = Src("Finale/FinaleDirector.cs");
            StringAssert.DoesNotContain("Sfx(AudioIds.HeartBeat", src);
            StringAssert.DoesNotContain("GameAudio.At(AudioIds.HeartBeat", src);
        }

        [Test]
        public void GalewickArrival_CloudsAreTheOnlyWayNorth()
        {
            var map = AssetDatabase.LoadAssetAtPath<IslandMap>("Assets/_Project/Data/WorldMap_Galewick.asset");
            Assert.IsNotNull(map);
            // Row 50 holds the two puffs on the path (c10, c11); every other tile of the arrival meadow's row is shut.
            for (int c = 4; c <= 18; c++)
            {
                bool path = c == 10 || c == 11;
                Assert.AreEqual(path, IslandMap.IsWalkable(map.Raw(50, c)), $"c{c} r50");
            }
        }

        [Test]
        public void Poke_OrphanedPassIsCleared()
        {
            Assert.IsTrue(PokeRelease.IsOrphanedPass(true, false, false));
            Assert.IsFalse(PokeRelease.IsOrphanedPass(true, true, false));
            Assert.IsFalse(PokeRelease.IsOrphanedPass(true, false, true));
            Assert.IsFalse(PokeRelease.IsOrphanedPass(false, false, false));
        }

        [Test]
        public void ReleaseChecks_CodeAboveEveryBuiltApk()
        {
            Assert.AreEqual(14, EditorTools.ReleaseChecks.HighestCode(new[] { "PocketIsle-Release-1.0.0-13.apk", "PocketIsle-Release-1.0.0-14.apk", "PocketIsle-QA.apk", "PocketIsle-Release-TEST-NOT-FOR-SUBMISSION.apk" }));
            Assert.AreEqual(0, EditorTools.ReleaseChecks.HighestCode(new string[0]));
        }

        [Test]
        public void ReleaseChecks_BadgingCatchesTheUsualMistakes()
        {
            string good = "package: name='com.mugesh.pocketisle' versionCode='15' versionName='1.0.0'\nsdkVersion:'32'\ntargetSdkVersion:'34'\n" +
                          "application-label:'Pocket Isle'\napplication-icon-640:'res/mipmap/app_icon.png'\nnative-code: 'arm64-v8a'\n";
            CollectionAssert.IsEmpty(EditorTools.ReleaseChecks.CheckBadging(good, 15, "1.0.0", "Pocket Isle"));
            string bad = good.Replace("15", "14").Replace("Pocket Isle", "Pocket isle") + "application-debuggable\nuses-permission: name='android.permission.RECORD_AUDIO'\n";
            var f = EditorTools.ReleaseChecks.CheckBadging(bad, 15, "1.0.0", "Pocket Isle");
            Assert.AreEqual(4, f.Count, string.Join("\n", f));
        }

        [Test]
        public void Rune_ArmsOnlyWithNoFingerInOrThroughIt()
        {
            var size = new Vector2(0.074f, 0.028f);
            // Pushed through the new board's Controls tablet (the Settings press): not armed.
            Assert.IsTrue(RuneBoard.FingerThrough(new Vector3(0.01f, 0f, 0.03f), size, 0.01f, 0.15f));
            // Resting on its face, or just in front: still counts.
            Assert.IsTrue(RuneBoard.FingerThrough(new Vector3(0f, 0.005f, -0.005f), size, 0.01f, 0.15f));
            // In front (approaching for a press), beside it, or far behind: clear.
            Assert.IsFalse(RuneBoard.FingerThrough(new Vector3(0f, 0f, -0.04f), size, 0.01f, 0.15f));
            Assert.IsFalse(RuneBoard.FingerThrough(new Vector3(0.08f, 0f, 0.02f), size, 0.01f, 0.15f));
            Assert.IsFalse(RuneBoard.FingerThrough(new Vector3(0f, 0f, 0.3f), size, 0.01f, 0.15f));
            var src = File.ReadAllText("Assets/_Project/Scripts/Runtime/UI/RuneBoard.cs");
            StringAssert.Contains("poke.enabled = false;\n            r.armed = false;", src.Replace("\r\n", "\n"));
        }
    }
}
