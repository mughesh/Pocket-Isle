using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Nov 18 (Mugesh: "I tend to make mistakes, can the checks run by themselves?"): what Build Release APK checks around
    /// the build. Before: the store name, a version code above every release APK already in Builds/, uncommitted game work,
    /// scenes in the build. After: the APK itself (aapt + apksigner: version, name, not debuggable, ARM64, SDK levels,
    /// excludeFromRecents, no removed permissions, signed with the release key, not the debug key). The result goes in a
    /// dialog and in Builds/&lt;apk&gt;-check.txt with the headset checklist that only a person can do.
    /// </summary>
    public static class ReleaseChecks
    {
        public const string StoreName = "Pocket Isle";
        public const string PackageId = "com.mugesh.pocketisle";
        public const int MinSdk = 32, TargetSdk = 34;
        const string ReleaseCertDn = "CN=Mugesh, O=Pocket Isle";

        public const string HeadsetChecklist =
            "On the headset (only you can check these):\n" +
            "  1. Uninstall the old build first (back up the save if you want it), then install this one.\n" +
            "  2. Cold start: title comes up fast, no Dev button anywhere.\n" +
            "  3. Hands only: opening, first walk, a battle and capture, Settings poked fast.\n" +
            "  4. Headset off/on and Meta button: pauses and resumes cleanly.\n" +
            "  5. In-game Quit, relaunch, Continue resumes.\n" +
            "After uploading: install from the channel (not sideload) and launch once; check the dashboard shows the new version code.";

        // ---- before the build ----

        /// <summary>
        /// Runs before the build. Returns false to stop. <paramref name="code"/> is raised above every release APK already
        /// built (so a second build never reuses a code a channel already has).
        /// </summary>
        public static bool Before(ref int code)
        {
            // Store name.
            if (PlayerSettings.productName != StoreName)
            {
                int pick = EditorUtility.DisplayDialogComplex("Release check: app name",
                    $"The app name is \"{PlayerSettings.productName}\"; the store title is \"{StoreName}\".\n\n" +
                    "The headset library shows this name. Saves on the headset are not affected (they go by the package id).",
                    $"Fix it to \"{StoreName}\"", "Cancel build", "Build with the current name");
                if (pick == 1) return false;
                if (pick == 0)
                {
                    PlayerSettings.productName = StoreName;
                    Debug.Log($"[ReleaseChecks] App name set to \"{StoreName}\".");
                }
            }
            // Package id.
            string id = PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android);
            if (id != PackageId)
            {
                EditorUtility.DisplayDialog("Release check: package id", $"The package id is \"{id}\", the store app is \"{PackageId}\". Build stopped.", "OK");
                return false;
            }
            // Version code above anything already built.
            int built = HighestBuiltCode(ReleaseBuild.ApkDir);
            if (built >= code)
            {
                Debug.Log($"[ReleaseChecks] Builds/ already has release code {built}; this build uses {built + 1} (was {code}).");
                code = built + 1;
            }
            // Scenes.
            if (!EditorBuildSettings.scenes.Any(s => s.enabled))
            {
                EditorUtility.DisplayDialog("Release check: scenes", "No scene is enabled in the build list. Build stopped.", "OK");
                return false;
            }
            // Uncommitted game work.
            var dirty = Uncommitted();
            if (dirty.Count > 0)
            {
                string list = string.Join("\n", dirty.Take(12)) + (dirty.Count > 12 ? $"\n... and {dirty.Count - 12} more" : "");
                if (!EditorUtility.DisplayDialog("Release check: uncommitted changes",
                        "These game files have changes that aren't committed (the build includes them, git doesn't):\n\n" + list,
                        "Build anyway", "Cancel build"))
                    return false;
            }
            return true;
        }

        /// <summary>Highest code in Builds/PocketIsle-Release-x.y.z-N.apk names; 0 when none (pure on a file list: see <see cref="HighestCode"/>).</summary>
        public static int HighestBuiltCode(string dir) =>
            Directory.Exists(dir) ? HighestCode(Directory.GetFiles(dir, "PocketIsle-Release-*.apk").Select(Path.GetFileName)) : 0;

        public static int HighestCode(IEnumerable<string> names)
        {
            int best = 0;
            foreach (var n in names)
            {
                var m = Regex.Match(n ?? "", @"^PocketIsle-Release-[\d.]+-(\d+)\.apk$");
                if (m.Success) best = Math.Max(best, int.Parse(m.Groups[1].Value));
            }
            return best;
        }

        /// <summary>Changed or new files under Assets/_Project, Assets/Resources, Packages and ProjectSettings (docs and notes ignored).</summary>
        static List<string> Uncommitted()
        {
            var lines = Run("git", "status --porcelain -- Assets/_Project Assets/Resources Packages/manifest.json ProjectSettings", out int exit);
            if (exit != 0) return new List<string>();
            return lines.Split('\n').Select(l => l.TrimEnd()).Where(l => l.Length > 3)
                .Where(l => !l.Contains("Assets/Docs/") && !l.EndsWith(".md\"") && !l.EndsWith(".md"))
                // The build itself rewrites the version code; the check runs before it, so ProjectSettings counts.
                .ToList();
        }

        // ---- after the build ----

        /// <summary>Bridge: the same with no dialog (log only).</summary>
        public static void CheckLastQuiet()
        {
            int code = HighestBuiltCode(ReleaseBuild.ApkDir);
            if (code > 0) After(ReleaseBuild.ReleaseApk(code), code, false);
        }

        /// <summary>Runs the APK checks on the newest release APK in Builds/ (no build).</summary>
        [MenuItem("Tools/Pocket Isle/Release/Check Last Release APK", priority = 74)]
        public static void CheckLast()
        {
            int code = HighestBuiltCode(ReleaseBuild.ApkDir);
            if (code == 0)
            {
                EditorUtility.DisplayDialog("Release check", "No release APK in Builds/ yet.", "OK");
                return;
            }
            After(ReleaseBuild.ReleaseApk(code), code);
        }

        /// <summary>Checks the built APK, writes the report next to it and shows it. True when nothing failed.</summary>
        public static bool After(string apk, int code, bool dialog = true)
        {
            var fails = new List<string>();
            var notes = new List<string>();
            if (!File.Exists(apk)) fails.Add($"No APK at {apk}.");
            else
            {
                string aapt = Tool("aapt.exe");
                if (aapt == null) fails.Add("aapt not found (Android SDK build-tools); APK contents not checked.");
                else
                {
                    string badging = Run(aapt, $"dump badging \"{apk}\"", out _);
                    fails.AddRange(CheckBadging(badging, code, ReleaseBuild.VersionName, StoreName));
                    string manifest = Run(aapt, $"dump xmltree \"{apk}\" AndroidManifest.xml", out _);
                    if (!manifest.Contains("android:excludeFromRecents")) fails.Add("excludeFromRecents is missing on the launching activity (Meta rule VRC.Quest.Packaging).");
                    notes.Add("aapt: manifest and badging checked.");
                }
                string signer = Signer(apk);
                if (signer == null) notes.Add("apksigner not found: check the signature by hand.");
                else if (signer.Contains("Android Debug")) fails.Add("Signed with the DEBUG key: the store rejects it.");
                else if (!signer.Contains(ReleaseCertDn)) fails.Add($"Signed with an unexpected certificate: {signer.Trim()}");
                else notes.Add($"Signed with the release key ({ReleaseCertDn}).");
                notes.Add($"Size {new FileInfo(apk).Length / (1024f * 1024f):0.0} MB.");
            }

            bool ok = fails.Count == 0;
            string report = $"{(ok ? "PASS" : "FAIL")}  {Path.GetFileName(apk)}  (version {ReleaseBuild.VersionName}, code {code}, {DateTime.Now:yyyy-MM-dd HH:mm})\n\n" +
                            (ok ? "" : "Problems:\n  - " + string.Join("\n  - ", fails) + "\n\n") +
                            "Checked:\n  - " + string.Join("\n  - ", notes) + "\n\n" + HeadsetChecklist + "\n";
            try { File.WriteAllText(Path.ChangeExtension(apk, null) + "-check.txt", report); }
            catch (Exception e) { Debug.LogWarning($"[ReleaseChecks] Could not write the report: {e.Message}"); }
            if (ok) Debug.Log("[ReleaseChecks] " + report);
            else Debug.LogError("[ReleaseChecks] " + report);
            if (dialog) EditorUtility.DisplayDialog(ok ? "Release build: all checks passed" : "Release build: PROBLEMS FOUND - don't upload", report, "OK");
            return ok;
        }

        /// <summary>Pure (tests): problems in an <c>aapt dump badging</c> output.</summary>
        public static List<string> CheckBadging(string badging, int code, string versionName, string label)
        {
            var f = new List<string>();
            if (string.IsNullOrEmpty(badging))
            {
                f.Add("aapt returned nothing.");
                return f;
            }
            string Get(string pattern)
            {
                var m = Regex.Match(badging, pattern);
                return m.Success ? m.Groups[1].Value : null;
            }
            if (Get(@"package: name='([^']*)'") != PackageId) f.Add($"Package id is {Get(@"package: name='([^']*)'")}, expected {PackageId}.");
            if (Get(@"versionCode='(\d+)'") != code.ToString()) f.Add($"Version code is {Get(@"versionCode='(\d+)'")}, expected {code}.");
            if (Get(@"versionName='([^']*)'") != versionName) f.Add($"Version name is {Get(@"versionName='([^']*)'")}, expected {versionName}.");
            string name = Get(@"application-label:'([^']*)'");
            if (name != label) f.Add($"App name is \"{name}\", expected \"{label}\".");
            if (badging.Contains("application-debuggable")) f.Add("The APK is debuggable (a development build): the store rejects it.");
            if (Get(@"sdkVersion:'(\d+)'") != MinSdk.ToString()) f.Add($"minSdk is {Get(@"sdkVersion:'(\d+)'")}, expected {MinSdk}.");
            if (Get(@"targetSdkVersion:'(\d+)'") != TargetSdk.ToString()) f.Add($"targetSdk is {Get(@"targetSdkVersion:'(\d+)'")}, expected {TargetSdk}.");
            string abi = Get(@"native-code: (.*)");
            if (abi == null || abi.Trim() != "'arm64-v8a'") f.Add($"Native code is {abi ?? "none"}, expected only 'arm64-v8a'.");
            if (!Regex.IsMatch(badging, @"application-icon-\d+:'[^']+'")) f.Add("No app icon in the APK.");
            foreach (var perm in ReleaseBuild.RemovedPermissions)
                if (badging.Contains($"uses-permission: name='{perm}'")) f.Add($"Has {perm}, which the release build should remove.");
            return f;
        }

        // ---- tools ----

        static string BuildTools()
        {
            string sdk = null;
            try { sdk = UnityEditor.Android.AndroidExternalToolsSettings.sdkRootPath; } catch { }
            if (string.IsNullOrEmpty(sdk)) sdk = Path.Combine(Path.GetDirectoryName(EditorApplication.applicationPath), "Data/PlaybackEngines/AndroidPlayer/SDK");
            string bt = Path.Combine(sdk, "build-tools");
            return Directory.Exists(bt) ? Directory.GetDirectories(bt).OrderBy(d => d).LastOrDefault() : null;
        }

        static string Tool(string file)
        {
            string bt = BuildTools();
            string p = bt != null ? Path.Combine(bt, file) : null;
            return p != null && File.Exists(p) ? p : null;
        }

        static string Signer(string apk)
        {
            string bt = BuildTools();
            string jar = bt != null ? Path.Combine(bt, "lib/apksigner.jar") : null;
            if (jar == null || !File.Exists(jar)) return null;
            string jdk = null;
            try { jdk = UnityEditor.Android.AndroidExternalToolsSettings.jdkRootPath; } catch { }
            if (string.IsNullOrEmpty(jdk)) jdk = Path.Combine(Path.GetDirectoryName(EditorApplication.applicationPath), "Data/PlaybackEngines/AndroidPlayer/OpenJDK");
            string java = Path.Combine(jdk, "bin/java.exe");
            if (!File.Exists(java)) return null;
            string output = Run(java, $"-jar \"{jar}\" verify --print-certs \"{apk}\"", out int exit);
            if (exit != 0) return "apksigner: the signature does not verify. " + output;
            var dn = output.Split('\n').FirstOrDefault(l => l.Contains("certificate DN"));
            return dn ?? output;
        }

        static string Run(string exe, string args, out int exit)
        {
            try
            {
                var psi = new ProcessStartInfo(exe, args)
                {
                    UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true,
                    WorkingDirectory = Directory.GetCurrentDirectory(),
                };
                using (var p = Process.Start(psi))
                {
                    string o = p.StandardOutput.ReadToEnd();
                    string e = p.StandardError.ReadToEnd();
                    p.WaitForExit(60000);
                    exit = p.ExitCode;
                    return o + e;
                }
            }
            catch (Exception ex)
            {
                exit = -1;
                return ex.Message;
            }
        }
    }
}
