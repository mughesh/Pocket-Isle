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
    }
}
