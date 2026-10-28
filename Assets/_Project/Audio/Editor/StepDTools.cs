using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Diorama.Audio.EditorTools
{
    /// <summary>Bridge entry points for step audio-D (exec:Diorama.Audio.EditorTools.StepDTools.&lt;Method&gt;).</summary>
    public static class StepDTools
    {
        const string ReportPath = "Logs/audioD-scope.md";

        /// <summary>
        /// The new synth cues: creates their patches and cues, re-applies only the audio-D recipes (other patches keep any
        /// Lab edits), bakes them, rewrites the Galewick / Lilymere ambience profiles, syncs, analyses into Logs/audioD-scope.md.
        /// </summary>
        public static void Synth()
        {
            SynthBaker.CreateMissing();
            var ids = new HashSet<string>(SynthRemakes.AudioDIds);
            var bake = new List<SynthPatch>();
            foreach (var p in SynthBaker.AllPatches())
            {
                if (!ids.Contains(p.id)) continue;
                SynthRecipes.Apply(p, SynthRecipes.Find(p.id));
                EditorUtility.SetDirty(p);
                bake.Add(p);
            }
            AssetDatabase.SaveAssets();
            SynthBaker.BakeAll(bake);
            AmbienceTools.RewriteRealmProfiles();
            AudioCueTools.SyncCues();
            StepBTools.SetCueVolumes();
            AudioCueTools.SyncAll();
            Scope();
            Debug.Log($"[AudioLab] Audio-D synth: {bake.Count} patches baked.");
        }

        /// <summary>The cries of the wind and water rosters: missing profiles, their patches, bake only the new ones, sync.</summary>
        public static void Cries()
        {
            CryTools.MakeProfiles();
            var before = new HashSet<string>(SynthBaker.AllPatches().Select(p => p.id));
            SynthBaker.CreateMissing();
            var fresh = SynthBaker.AllPatches().Where(p => !before.Contains(p.id)).ToList();
            SynthBaker.BakeAll(fresh);
            AudioCueTools.SyncAll();
            var entries = AudioScope.Analyse(new[] { "Cries" }, AudioScope.OutDir);
            var sb = new StringBuilder("# Audio-D cries\n\n| clip | result |\n|---|---|\n");
            foreach (var e in entries.Where(e => fresh.Any(p => p.id == e.cue)))
                sb.AppendLine($"| {Path.GetFileName(e.clipPath)} | {(e.fails.Count == 0 ? "pass" : string.Join(", ", e.fails))} |");
            File.WriteAllText("Logs/audioD-cries.md", sb.ToString());
            Debug.Log($"[AudioLab] Audio-D cries: {fresh.Count} new patches: {string.Join(", ", fresh.Select(p => p.id))}");
        }

        /// <summary>Gen batch 2: Import Gen Batch (copies, levelled copies, pick notes), then the scope report.</summary>
        public static void ImportGen()
        {
            GenImporter.Import(GenImporter.SourceRoot);
            Scope();
        }

        /// <summary>AudioScope over every audio-D cue: Logs/audioD-scope.md (one row per clip).</summary>
        public static void Scope()
        {
            var ids = new HashSet<string>(SynthRemakes.AudioDIds);
            var groups = AudioCueTools.AllCues().Where(c => ids.Contains(c.id)).Select(AudioCueTools.Group).Distinct().ToList();
            var entries = AudioScope.Analyse(groups, AudioScope.OutDir).Where(e => ids.Contains(e.cue)).ToList();
            var sb = new StringBuilder("# Audio-D AudioScope\n\n| cue | clip | kind | result |\n|---|---|---|---|\n");
            foreach (var e in entries.OrderBy(e => e.cue))
                sb.AppendLine($"| {e.cue} | {Path.GetFileName(e.clipPath)} | {e.kind} | {(e.fails.Count == 0 ? "pass" : string.Join(", ", e.fails))} |");
            int synthFails = entries.Count(e => e.fails.Count > 0 && e.clipPath.Contains("/Synth/"));
            sb.AppendLine($"\n{entries.Count} clips, {entries.Count(e => e.fails.Count > 0)} failing ({synthFails} synth).");
            Directory.CreateDirectory("Logs");
            File.WriteAllText(ReportPath, sb.ToString());
            Debug.Log($"[AudioLab] Audio-D scope: {entries.Count} clips, {synthFails} synth failing. {ReportPath}");
        }
    }
}
