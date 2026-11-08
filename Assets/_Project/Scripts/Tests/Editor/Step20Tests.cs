using Diorama.Audio;
using NUnit.Framework;
using UnityEditor;

namespace Diorama.Tests
{
    /// <summary>Step 20: the soundtrack (cue settings and the state -> cue map).</summary>
    public class Step20Tests
    {
        static AudioCue Cue(string id) => AssetDatabase.LoadAssetAtPath<AudioCue>($"Assets/_Project/Audio/Data/Cues/Music/{id}.asset");

        static readonly (string id, float volume, bool loop, float rest)[] Expected =
        {
            ("music.title", 0.42f, false, 0f), ("music.hub", 0.40f, false, 0f), ("music.lodge", 0.35f, false, 0f),
            ("music.finale", 0.38f, false, 0f), ("music.galewick", 0.30f, true, 45f), ("music.cinderwood", 0.28f, true, 45f),
            ("music.lilymere", 0.30f, true, 45f), ("music.temple", 0.28f, true, 0f),
        };

        [Test]
        public void MusicCues_HaveTheirTrack_Level_AndMode()
        {
            foreach (var e in Expected) Check(e.id, e.volume, e.loop, e.rest);
        }

        static void Check(string id, float volume, bool loop, float rest)
        {
            var c = Cue(id);
            Assert.IsNotNull(c, id);
            Assert.IsTrue(c.clips != null && c.clips.Length == 1 && c.clips[0] != null, $"{id}: one clip");
            StringAssert.StartsWith(id + "_", c.clips[0].name);
            Assert.IsFalse(c.awaitingClips, id);
            Assert.AreEqual(volume, c.volume, 1e-4f, id);
            Assert.AreEqual(loop, c.loop, id);
            Assert.AreEqual(rest, c.restSeconds, 1e-4f, id);
            Assert.AreEqual(AudioBus.Music, c.bus, id);
            Assert.IsFalse(c.spatial, id);
        }

        [Test]
        public void States_MapToTheirCues()
        {
            Assert.AreEqual(AudioIds.MusicTitle, MusicDirector.CueFor(MusicState.Title));
            Assert.AreEqual(AudioIds.MusicTemple, MusicDirector.CueFor(MusicState.Temple));
            Assert.AreEqual(AudioIds.MusicLodge, MusicDirector.CueFor(MusicState.Lodge));
            Assert.AreEqual(AudioIds.MusicHub, MusicDirector.CueFor(MusicState.Hub));
            Assert.IsNull(MusicDirector.CueFor(MusicState.None));
            Assert.AreEqual(3f, MusicDirector.AreaCrossfade);
            Assert.AreEqual(0.6f, MusicDirector.BattleCrossfade);
        }
    }
}
