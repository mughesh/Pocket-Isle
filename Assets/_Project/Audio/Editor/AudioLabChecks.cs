using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Diorama.Audio.EditorTools
{
    /// <summary>
    /// Checks for the parts of the audio tools that can be verified without ears (run through the bridge:
    /// exec:Diorama.Audio.EditorTools.AudioLabChecks.Report). Output: Logs/AudioLab/report.txt.
    /// </summary>
    public static class AudioLabChecks
    {
        public const string ReportPath = "Logs/AudioLab/report.txt";

        /// <summary>Renders every patch (all variants) and lists peak, RMS, high-frequency share, loop seam and problems.</summary>
        public static void Report()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Audio Lab report {System.DateTime.Now:yyyy-MM-dd HH:mm}  preview backend: {AudioPreview.Backend}");
            sb.AppendLine("id | mode | s | peak dB | rms dB | >9k % | seam (x usual step) | flags");
            int problems = 0;
            foreach (var p in SynthBaker.AllPatches())
            {
                for (int v = 0; v < p.variants; v++)
                {
                    var data = SynthRenderer.Render(p, v);
                    SynthRenderer.Analyze(data, out float peak, out float rms, out float high);
                    float seam = SynthRenderer.LoopSeam(data);
                    var flags = new StringBuilder();
                    if (float.IsNaN(peak) || float.IsNaN(rms)) flags.Append(" NaN");
                    if (rms < -45f) flags.Append(" quiet");
                    if (high > 0.02f) flags.Append(" bright");
                    if (p.mode == SynthMode.Loop && seam > 4f) flags.Append(" seam");
                    if (p.mode != SynthMode.Loop && (Mathf.Abs(data[0]) > 0.01f || Mathf.Abs(data[data.Length - 1]) > 0.01f)) flags.Append(" click");
                    if (flags.Length > 0) problems++;
                    sb.AppendLine($"{p.id}{(p.variants > 1 ? $"_v{v + 1}" : "")} | {p.mode} | {data.Length / (float)SynthPatch.SampleRate:0.00} | {peak:0.0} | {rms:0.0} | {high * 100f:0.00} | {seam:0.0} |{flags}");
                }
            }
            sb.AppendLine($"problems: {problems}");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, sb.ToString());
        }

        /// <summary>
        /// Release check: fails while any cue plays a free-plan ElevenLabs clip (no commercial licence), listing them; also
        /// runs the cue validation. Logs/AudioLab/release.txt. Returns true when it passes.
        /// </summary>
        [MenuItem("Tools/Pocket Isle/Audio/Release Check", priority = 94)]
        public static bool ReleaseCheck()
        {
            var free = GenImporter.FreeClipsInCues();
            var table = AudioCueTools.BuildTable();
            var validation = AudioCueTools.Validate(table);
            var sb = new StringBuilder();
            bool pass = free.Count == 0;
            sb.AppendLine(pass ? "PASS release check" : $"FAIL release check: {free.Count} free-plan clips in cues (no commercial licence). Regenerate on a paid plan or remove them:");
            foreach (var f in free) sb.AppendLine("  " + f);
            sb.AppendLine(validation);
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText("Logs/AudioLab/release.txt", sb.ToString());
            if (pass) Debug.Log("[AudioLab] Release check passed. " + validation);
            else Debug.LogError("[AudioLab] " + sb);
            return pass;
        }

        /// <summary>Source vs imported (build) size of everything under Clips/, per folder: Logs/AudioLab/sizes.txt.</summary>
        public static void SizeReport()
        {
            var orig = typeof(AudioImporter).GetProperty("origSize", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            var comp = typeof(AudioImporter).GetProperty("compSize", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            var totals = new System.Collections.Generic.SortedDictionary<string, long[]>();
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { AudioImportRules.Root.TrimEnd('/') }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var imp = AssetImporter.GetAtPath(path) as AudioImporter;
                if (imp == null) continue;
                var key = Path.GetDirectoryName(path).Replace('\\', '/').Substring(AudioImportRules.Root.Length);
                if (!totals.TryGetValue(key, out var t)) totals[key] = t = new long[3];
                t[0] += new FileInfo(path).Length;
                t[1] += comp != null ? System.Convert.ToInt64(comp.GetValue(imp)) : 0;
                t[2]++;
            }
            var sb = new StringBuilder($"folder | files | on disk KB | imported KB (compSize via {(comp != null ? "reflection" : "missing")})\n");
            long d = 0, c = 0, n = 0;
            foreach (var kv in totals)
            {
                sb.AppendLine($"{kv.Key} | {kv.Value[2]} | {kv.Value[0] / 1024} | {kv.Value[1] / 1024}");
                d += kv.Value[0];
                c += kv.Value[1];
                n += kv.Value[2];
            }
            sb.AppendLine($"TOTAL | {n} | {d / 1024} | {c / 1024}");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText("Logs/AudioLab/sizes.txt", sb.ToString());
        }

        /// <summary>Resets every recipe patch to its code recipe (keeps reviews), bakes and syncs cues. Only for unreviewed batches.</summary>
        public static void ReapplyRecipesAndBake()
        {
            int n = 0;
            foreach (var p in SynthBaker.AllPatches())
            {
                var r = SynthRecipes.Find(p.id);
                if (r == null || r.build == null) continue;
                SynthRecipes.Apply(p, r);
                EditorUtility.SetDirty(p);
                n++;
            }
            AssetDatabase.SaveAssets();
            SynthBaker.CreateAndBakeAll();
            AudioCueTools.SyncAll();
            Debug.Log($"[AudioLab] Reapplied {n} recipes.");
        }

        /// <summary>Bakes a throwaway one-shot and loop, logs their import settings, then deletes them.</summary>
        public static void BakeProbe()
        {
            const string dir = SynthBaker.PatchRoot + "_Probe";
            Directory.CreateDirectory(dir);
            var sb = new StringBuilder();
            var shot = ScriptableObject.CreateInstance<SynthPatch>();
            shot.id = "probe.shot";
            shot.One(0.4f, Syn.L(SynthWave.Triangle).Note("D5").Env(0.004f, 0.3f)).Vars(3);
            var loop = ScriptableObject.CreateInstance<SynthPatch>();
            loop.id = "probe.loop";
            loop.Loop(4f, 1f, Syn.L(SynthWave.PinkNoise).LP(900f).Env(0.2f, 1f, 1f));
            AssetDatabase.CreateAsset(shot, dir + "/probe.shot.asset");
            AssetDatabase.CreateAsset(loop, dir + "/probe.loop.asset");
            foreach (var p in new[] { shot, loop })
            {
                SynthBaker.Bake(p);
                for (int v = 0; v < p.variants; v++)
                {
                    var path = SynthBaker.ClipPath(p, v);
                    var imp = (AudioImporter)AssetImporter.GetAtPath(path);
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    var s = imp.defaultSampleSettings;
                    sb.AppendLine($"{path}: {clip.length:0.000} s {clip.channels} ch {clip.frequency} Hz, {s.loadType} {s.compressionFormat} q{s.quality} mono {imp.forceToMono}");
                }
            }
            shot.variants = 1;
            SynthBaker.Bake(shot);
            sb.AppendLine($"after variants=1: {string.Join(", ", Directory.GetFiles(SynthBaker.SynthClipRoot + "_Probe", "*.wav"))}");
            AssetDatabase.DeleteAsset(dir);
            AssetDatabase.DeleteAsset(SynthBaker.SynthClipRoot + "_Probe");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText("Logs/AudioLab/bake.txt", sb.ToString());
        }

        /// <summary>Plays a short test tone through the preview path and logs whether Unity reports it playing.</summary>
        public static void PreviewProbe()
        {
            var p = ScriptableObject.CreateInstance<SynthPatch>();
            p.One(0.6f, Syn.L(SynthWave.Sine).Note("A5").Env(0.005f, 0.4f).Fm(3.5f, 2f, 0.1f));
            var clip = AudioPreview.MakeClip("probe", SynthRenderer.Render(p));
            AudioPreview.Play(clip);
            bool playing = AudioPreview.IsPlaying;
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText("Logs/AudioLab/preview.txt", $"backend {AudioPreview.Backend}, playing right after Play: {playing}, clip {clip.length:0.00} s\n");
            Object.DestroyImmediate(p);
        }
    }
}
