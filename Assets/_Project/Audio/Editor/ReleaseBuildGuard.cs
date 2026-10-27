using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Diorama.Audio.EditorTools
{
    /// <summary>
    /// Stops the release build while any cue still plays a free-plan ElevenLabs clip: those have no commercial licence.
    /// Only Build Release APK arms it (SessionState <see cref="ArmedKey"/>, set just for that build); QA and perf APKs go
    /// through with the free clips so Mugesh can test on the headset.
    /// </summary>
    public class ReleaseBuildGuard : IPreprocessBuildWithReport
    {
        public const string ArmedKey = "PocketIsle.ReleaseBuild";

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (!SessionState.GetBool(ArmedKey, false)) return;
            var free = GenImporter.FreeClipsInCues();
            if (free.Count > 0)
                throw new BuildFailedException($"[AudioLab] {free.Count} free-plan clips are still in audio cues (no commercial licence). " +
                                               "Run Tools > Pocket Isle > Audio > Release Check for the list; regenerate them on a paid plan or remove them.");
        }
    }
}
