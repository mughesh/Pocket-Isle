using UnityEngine;

namespace Diorama.Audio
{
    /// <summary>Headset A/B switches on the Dev panel (remembered on the device). Both start on the old sound until Mugesh picks.</summary>
    public static class AudioAB
    {
        const string JinglesKey = "PocketIsle.AB.AcousticJingles", VoicesKey = "PocketIsle.AB.NewVoices";
        const string JinglePrefix = "jingle.", AcousticSuffix = ".acoustic";

        /// <summary>Jingles: false = synth, true = the acoustic takes.</summary>
        public static bool AcousticJingles
        {
            get => PlayerPrefs.GetInt(JinglesKey, 0) == 1;
            set => PlayerPrefs.SetInt(JinglesKey, value ? 1 : 0);
        }

        /// <summary>Keeper and Heart voices: false = the re-pitched blip, true = entrance call + voice grains. The Spirit keeps its blip either way.</summary>
        public static bool NewVoices
        {
            get => PlayerPrefs.GetInt(VoicesKey, 0) == 1;
            set => PlayerPrefs.SetInt(VoicesKey, value ? 1 : 0);
        }

        /// <summary>The cue to play for <paramref name="id"/>: a jingle's acoustic twin when that switch is on and the twin has clips.</summary>
        public static string Resolve(string id) => JingleFor(id, AcousticJingles, AudioService.Has);

        /// <summary>The rule on its own, so a test doesn't touch the device's saved switch.</summary>
        public static string JingleFor(string id, bool acoustic, System.Func<string, bool> has)
        {
            if (!acoustic || string.IsNullOrEmpty(id) || !id.StartsWith(JinglePrefix) || id.EndsWith(AcousticSuffix)) return id;
            var twin = id + AcousticSuffix;
            return has(twin) ? twin : id;
        }

        public static string GrainCue(string speaker) => "voice.grain." + speaker;
        public static string CallCue(string speaker) => "voice.call." + speaker;
    }
}
