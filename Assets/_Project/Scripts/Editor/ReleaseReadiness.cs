using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Diorama.Audio;
using Diorama.Audio.EditorTools;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Overnight Nov 5: Tools > Pocket Isle > Release > Readiness Report. Reads (never changes) what a store build needs:
    /// package name, version, SDKs, ARM64 + IL2CPP, the dev panel define, the keystore, the manifest's permissions and
    /// hand-tracking features (from the Meta project config and, if an APK exists in Builds/, aapt's dump of it), app name,
    /// icon, splash, APK sizes, every free-plan ElevenLabs clip still in a cue (grouped by cue, with the prompt it was made
    /// from) and the music. Writes Assets/Docs/Release-Readiness.md and Assets/Docs/Audio/Regen-List.md.
    /// </summary>
    public static class ReleaseReadiness
    {
        const string ReportPath = "Assets/Docs/Release-Readiness.md", RegenPath = "Assets/Docs/Audio/Regen-List.md";

        [MenuItem("Tools/Pocket Isle/Release/Readiness Report", priority = 60)]
        public static void Report()
        {
            var sb = new StringBuilder();
            var todo = new List<string>();
            sb.AppendLine("# Release readiness (read-only report)");
            sb.AppendLine();
            sb.AppendLine($"Made by Tools > Pocket Isle > Release > Readiness Report on {DateTime.Now:yyyy-MM-dd HH:mm}. Nothing was changed.");
            sb.AppendLine();
            sb.AppendLine("| Check | Value | OK? |");
            sb.AppendLine("|---|---|---|");
            void Row(string check, string value, bool? ok, string fix = null)
            {
                sb.AppendLine($"| {check} | {value.Replace("|", "/")} | {(ok == null ? "-" : ok.Value ? "yes" : "**no**")} |");
                if (ok == false && fix != null) todo.Add($"{check}: {fix}");
            }

            var android = NamedBuildTarget.Android;
            string id = PlayerSettings.GetApplicationIdentifier(android);
            Row("Package name", string.IsNullOrEmpty(id) ? "(empty)" : id, !string.IsNullOrEmpty(id) && !id.StartsWith("com.Unity", StringComparison.OrdinalIgnoreCase) && !id.Contains("DefaultCompany"),
                "Player Settings > Android > Other > Package Name (e.g. com.mugesh.pocketisle); it can't change after the first store upload.");
            Row("Version name", PlayerSettings.bundleVersion, !string.IsNullOrEmpty(PlayerSettings.bundleVersion));
            Row("Version code", PlayerSettings.Android.bundleVersionCode.ToString(), PlayerSettings.Android.bundleVersionCode >= 1,
                "raise it for every upload");
            Row("Min SDK", PlayerSettings.Android.minSdkVersion.ToString(), (int)PlayerSettings.Android.minSdkVersion >= 29, "Quest needs 29+ (32 is the usual)");
            Row("Target SDK", PlayerSettings.Android.targetSdkVersion.ToString(), PlayerSettings.Android.targetSdkVersion == AndroidSdkVersions.AndroidApiLevelAuto || (int)PlayerSettings.Android.targetSdkVersion >= 32,
                "Meta's store wants a current target SDK (check the Horizon store's rule on upload)");
            var arch = PlayerSettings.Android.targetArchitectures;
            Row("Architectures", arch.ToString(), arch == AndroidArchitecture.ARM64, "Player Settings > Android > Target Architectures: ARM64 only");
            var backend = PlayerSettings.GetScriptingBackend(android);
            Row("Scripting backend", backend.ToString(), backend == ScriptingImplementation.IL2CPP, "IL2CPP");
            string defines = PlayerSettings.GetScriptingDefineSymbols(android);
            Row("Android defines", string.IsNullOrEmpty(defines) ? "(none)" : defines, !defines.Contains("POCKET_DEV"),
                "remove POCKET_DEV from the Android defines (the QA build adds it per build; the release build must not have it)");
            Row("POCKET_DEV in the release build", "Build Release APK passes no extra defines; Build QA APK adds POCKET_DEV for that build only", true);
            bool keystore = PlayerSettings.Android.useCustomKeystore && !string.IsNullOrEmpty(PlayerSettings.Android.keystoreName);
            Row("Keystore", keystore ? $"{Path.GetFileName(PlayerSettings.Android.keystoreName)} alias {PlayerSettings.Android.keyaliasName}" : "not set (debug key)",
                keystore, "Publishing Settings > Keystore Manager: create one, keep it and its passwords OUT of git (back it up; losing it = no updates)");
            Row("App name", PlayerSettings.productName, !string.IsNullOrEmpty(PlayerSettings.productName) && PlayerSettings.productName != "New Unity Project");
            Row("Company", PlayerSettings.companyName, !string.IsNullOrEmpty(PlayerSettings.companyName) && PlayerSettings.companyName != "DefaultCompany");
            var icons = PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Application);
            bool icon = icons != null && icons.Any(t => t != null);
            if (!icon)
            {
                var androidIcons = PlayerSettings.GetIcons(android, IconKind.Application);
                icon = androidIcons != null && androidIcons.Any(t => t != null);
            }
            Row("App icon", icon ? "set" : "none", icon, "Player Settings > Icon: Default Icon = Assets/_Project/Art/Icon/PocketIsle_Icon_1024.png (see Overnight-Nov5.md)");
            Row("Unity splash", PlayerSettings.SplashScreen.show ? "on" : "off", null);

            // Meta project config (the manifest is generated from it at build time).
            string ovr = ReadText("Assets/Oculus/OculusProjectConfig.asset");
            string hand = Field(ovr, "handTrackingSupport") switch { "0" => "controllers only", "1" => "controllers and hands", "2" => "hands only", var v => v };
            Row("Hand tracking (Meta config)", hand, hand == "controllers and hands" || hand == "hands only", "OVRProjectConfig: Hand Tracking Support = Controllers and Hands");
            Row("Hand tracking frequency", Field(ovr, "handTrackingFrequency") == "1" ? "high" : Field(ovr, "handTrackingFrequency"), null);
            Row("Passthrough support", Field(ovr, "_insightPassthroughSupport") switch { "0" => "none", "1" => "supported", "2" => "required", var v => v }, null);
            Row("Scene support", Field(ovr, "sceneSupport") switch { "0" => "none", "1" => "supported", "2" => "required", var v => v }, null);
            Row("Anchor support", Field(ovr, "anchorSupport") == "1" ? "on" : "off", null);
            Row("System splash (Meta)", Field(ovr, "systemSplashScreen").Contains("fileID: 0") ? "none (black)" : "set", null);

            // Built APKs: sizes and the real manifest (aapt).
            sb.AppendLine();
            sb.AppendLine("## Built APKs (Builds/)");
            sb.AppendLine();
            var apks = Directory.Exists("Builds") ? new DirectoryInfo("Builds").GetFiles("*.apk").OrderByDescending(f => f.LastWriteTime).ToList() : new List<FileInfo>();
            if (apks.Count == 0) sb.AppendLine("None.");
            foreach (var f in apks) sb.AppendLine($"- `{f.Name}`: {f.Length / (1024f * 1024f):0.0} MB, {f.LastWriteTime:MMM d HH:mm}");
            var newest = apks.FirstOrDefault();
            if (newest != null)
            {
                string dump = Aapt($"dump badging \"{newest.FullName}\"");
                string perms = Aapt($"dump permissions \"{newest.FullName}\"");
                sb.AppendLine();
                sb.AppendLine($"Manifest of `{newest.Name}` (the newest; a new build regenerates it):");
                sb.AppendLine();
                if (dump == null) sb.AppendLine("- aapt not found (Unity's Android SDK build-tools): skipped.");
                else
                {
                    foreach (Match m in Regex.Matches(dump, @"^(package|sdkVersion|targetSdkVersion|application-label|native-code|uses-feature[^:]*):.*$", RegexOptions.Multiline))
                        sb.AppendLine($"- `{m.Value.Trim()}`");
                    foreach (Match m in Regex.Matches(perms ?? "", @"^uses-permission: name='([^']+)'", RegexOptions.Multiline))
                        sb.AppendLine($"- permission `{m.Groups[1].Value}`");
                }
            }
            sb.AppendLine();
            sb.AppendLine("Build size estimate: the newest release-style APK above (no APK built tonight). The QA build carries the dev panel and is a little larger.");

            // Audio: free-plan clips still in cues.
            var regen = RegenList(out int freeClips, out int freeCues, out string music);
            sb.AppendLine();
            sb.AppendLine("## Audio");
            sb.AppendLine();
            sb.AppendLine($"- **{freeClips} free-plan clips in {freeCues} cues** (no commercial licence; the release build refuses them). Regenerate on a paid plan: `{RegenPath}` lists each cue, its kept variants, the prompt and the length.");
            sb.AppendLine(music);
            if (freeClips > 0) todo.Add($"Audio: {freeClips} free-plan clips in {freeCues} cues: regenerate on a paid plan ({RegenPath}), re-import, Sync Cues.");

            sb.AppendLine();
            sb.AppendLine("## To do before the store build");
            sb.AppendLine();
            if (todo.Count == 0) sb.AppendLine("Nothing.");
            foreach (var t in todo) sb.AppendLine($"1. {t}");

            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, sb.ToString());
            File.WriteAllText(RegenPath, regen);
            AssetDatabase.ImportAsset(ReportPath);
            AssetDatabase.ImportAsset(RegenPath);
            Debug.Log($"[Release] Readiness report {ReportPath}: {todo.Count} to do; {freeClips} free clips in {freeCues} cues ({RegenPath}).\n" + string.Join("\n", todo));
        }

        static string ReadText(string path) => File.Exists(path) ? File.ReadAllText(path) : "";

        static string Field(string yaml, string key)
        {
            var m = Regex.Match(yaml, @"^\s*" + Regex.Escape(key) + @":\s*(.*)$", RegexOptions.Multiline);
            return m.Success ? m.Groups[1].Value.Trim() : "?";
        }

        static string Aapt(string args)
        {
            var root = Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines/AndroidPlayer/SDK/build-tools");
            if (!Directory.Exists(root)) return null;
            var exe = Directory.GetDirectories(root).OrderByDescending(d => d).Select(d => Path.Combine(d, "aapt.exe")).FirstOrDefault(File.Exists);
            if (exe == null) return null;
            try
            {
                var p = Process.Start(new ProcessStartInfo(exe, args) { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true });
                string o = p.StandardOutput.ReadToEnd();
                p.WaitForExit(20000);
                return o;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Release] aapt failed: {e.Message}");
                return null;
            }
        }

        [Serializable]
        class Manifest
        {
            public List<Clip> clips = new List<Clip>();
        }

        [Serializable]
        class Clip
        {
            public string cue = "", file = "", prompt = "", note = "", plan = "";
            public bool loop;
        }

        /// <summary>Every cue with a free-plan clip: its kept variants (file, length), the prompt each was made from.</summary>
        static string RegenList(out int clips, out int cues, out string music)
        {
            var prompts = new Dictionary<string, Clip>();
            var manifestPath = Path.Combine(GenImporter.SourceRoot, "manifest.json");
            if (File.Exists(manifestPath))
                foreach (var c in JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath)).clips)
                    prompts[Path.GetFileNameWithoutExtension(c.file)] = c;
            var sb = new StringBuilder();
            sb.AppendLine("# Free-plan clips to regenerate (paid ElevenLabs plan)");
            sb.AppendLine();
            sb.AppendLine($"Made by Tools > Pocket Isle > Release > Readiness Report on {DateTime.Now:yyyy-MM-dd HH:mm}. Only clips a cue still plays are listed " +
                          "(the cue's kept variants). Prompts from the gen lane's `manifest.json` " + (File.Exists(manifestPath) ? "" : "(**not found**: prompts missing) ") +
                          "Regenerate each with the same prompt and length, drop the files in the gen batch, run Import Gen Batch and Sync Cues.");
            sb.AppendLine();
            clips = 0;
            cues = 0;
            var musicLines = new List<string>();
            foreach (var cue in AudioCueTools.AllCues().OrderBy(c => c.id))
            {
                if (cue.bus == AudioBus.Music)
                    musicLines.Add($"`{cue.id}`: {(cue.clips.Count(x => x != null) == 0 ? (cue.awaitingClips ? "no clip yet (awaiting)" : "no clip") : string.Join(", ", cue.clips.Where(x => x != null).Select(x => $"{x.name} ({x.length:0} s)")))}");
                var free = cue.clips.Where(x => x != null && AssetDatabase.GetAssetPath(x).Contains("_FREE")).ToList();
                if (free.Count == 0) continue;
                cues++;
                clips += free.Count;
                sb.AppendLine($"## `{cue.id}` ({free.Count} free of {cue.clips.Count(x => x != null)}{(cue.loop ? ", loop" : "")})");
                sb.AppendLine();
                var byPrompt = new Dictionary<string, List<string>>();
                foreach (var x in free)
                {
                    string key = Regex.Replace(x.name, "_FREE.*$", "");
                    string prompt = prompts.TryGetValue(key, out var c) ? c.prompt : "(prompt not in the manifest)";
                    if (!byPrompt.TryGetValue(prompt, out var list)) byPrompt[prompt] = list = new List<string>();
                    list.Add($"`{x.name}` {x.length:0.0} s");
                }
                foreach (var kv in byPrompt)
                {
                    sb.AppendLine($"- Kept: {string.Join(", ", kv.Value)}");
                    sb.AppendLine($"  Prompt: {kv.Key}");
                }
                sb.AppendLine();
            }
            if (cues == 0) sb.AppendLine("None: every cue plays licensed or synth clips.");
            var musicSrc = Path.GetFullPath(Path.Combine(GenImporter.SourceRoot, "..", "music", "plan.txt"));
            music = "- Music cues: " + (musicLines.Count == 0 ? "none" : string.Join("; ", musicLines)) + ".\n" +
                    $"- Music sources: `{musicSrc}` " + (File.Exists(musicSrc) ? "(" + string.Join(" / ", File.ReadAllLines(musicSrc).Where(l => !l.StartsWith("#") && l.Trim().Length > 0)
                        .Select(l => l.Split('|').Select(s => s.Trim()).Take(3).Aggregate((a, b) => a + ", " + b))) + ")" : "(not found)") + ".";
            return sb.ToString();
        }
    }
}
