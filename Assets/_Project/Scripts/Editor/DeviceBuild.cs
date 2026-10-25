using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Device builds (step 7c perf gate): Builds/PocketIsle.apk from the enabled scenes with the current Android player
    /// settings (IL2CPP, ARM64). Release = development off (for perf numbers). Info logs what a build would use.
    /// Install and launch with adb (the step 7c handoff has the commands).
    /// </summary>
    static class DeviceBuild
    {
        public const string ApkPath = "Builds/PocketIsle.apk";

        [MenuItem("Tools/Pocket Isle/Device/Log Build Settings", priority = 80)]
        public static void Info()
        {
            Debug.Log($"[DeviceBuild] Active target {EditorUserBuildSettings.activeBuildTarget}, development {EditorUserBuildSettings.development}, " +
                      $"backend {PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android)}, " +
                      $"architectures {PlayerSettings.Android.targetArchitectures}, id {PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android)}, " +
                      $"version {PlayerSettings.bundleVersion} ({PlayerSettings.Android.bundleVersionCode}), " +
                      $"scenes {string.Join(", ", EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path))}.");
        }

        [MenuItem("Tools/Pocket Isle/Device/Build APK (release)", priority = 81)]
        public static void BuildRelease() => Build(false);

        [MenuItem("Tools/Pocket Isle/Device/Build APK (development)", priority = 82)]
        public static void BuildDevelopment() => Build(true);

        static void Build(bool development)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[DeviceBuild] Stop Play mode first.");
                return;
            }
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.LogError($"[DeviceBuild] The active target is {EditorUserBuildSettings.activeBuildTarget}; switch to the Meta Quest (Android) build profile first.");
                return;
            }
            Info();
            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = development ? BuildOptions.Development : BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            if (s.result == BuildResult.Succeeded)
                Debug.Log($"[DeviceBuild] Built {ApkPath} ({(development ? "development" : "release")}): {s.totalSize / (1024f * 1024f):0.0} MB in {s.totalTime.TotalMinutes:0.0} min.");
            else
                Debug.LogError($"[DeviceBuild] Build {s.result}: {s.totalErrors} error(s). See the Console / Editor.log.");
        }
    }
}
