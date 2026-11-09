using System.Linq;
using Diorama.Audio;
using Diorama.Audio.EditorTools;
using NUnit.Framework;
using UnityEditor;

namespace Diorama.Tests
{
    /// <summary>Step 25: the paid-sound credits, the stricter Release Check, the Still Isle bed and the re-cut steps.</summary>
    public class Step25Tests
    {
        static AudioCue Cue(string id) => AudioCueTools.AllCues().First(c => c.id == id);

        [Test]
        public void Credits_ShowTheAfterRegenLine()
        {
            Assert.IsTrue(CreditsText.PaidSoundsDone);
            StringAssert.Contains(CreditsText.SoundsAfterRegen, CreditsText.Block);
            StringAssert.DoesNotContain("free plan", CreditsText.Block);
        }

        [Test]
        public void ReleaseCheck_StrayFreeFile_OnlyOutsideTheArchive()
        {
            Assert.IsTrue(Step25Audio.IsStrayFree("Assets/_Project/Audio/Clips/Gen/restore.swell/restore.swell_1_FREE.wav"));
            Assert.IsFalse(Step25Audio.IsStrayFree("Assets/_Project/Audio/Clips/Gen/_free_archive/restore.swell/restore.swell_1_FREE.wav"));
            Assert.IsFalse(Step25Audio.IsStrayFree("Assets/_Project/Audio/Clips/Gen/step.grass/step.grass_21_s1.wav"));
        }

        [Test]
        public void NoFreeFile_OutsideTheArchive() => CollectionAssert.IsEmpty(Step25Audio.StrayFreeFiles());

        [Test]
        public void StillIsle_PlaysTheLowPassedSynthBed()
        {
            var cue = Cue("amb.stillisle");
            Assert.AreEqual(1200f, cue.lowPassHz, 0.1f);
            Assert.AreEqual(1, cue.clips.Length);
            StringAssert.Contains("/Synth/", AssetDatabase.GetAssetPath(cue.clips[0]));
        }

        [Test]
        public void Steps_KeepKenneyAndEnoughPaidSlices()
        {
            foreach (var id in new[] { "step.grass", "step.path", "step.stone", "step.wood" })
            {
                var paths = Cue(id).clips.Select(AssetDatabase.GetAssetPath).ToList();
                Assert.That(paths.Count(p => p.Contains("/Levelled/Kenney/")), Is.GreaterThan(0), id);
                Assert.That(paths.Count(p => p.Contains("/Levelled/Gen/")), Is.GreaterThanOrEqualTo(4), id);
                Assert.IsFalse(paths.Any(p => p.Contains(Step25Audio.Dropped)), id);
            }
        }
    }
}
