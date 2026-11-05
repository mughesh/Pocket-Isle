using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Diorama.Audio.EditorTools
{
    /// <summary>
    /// Step 16 B: the jingles quieter and warmer. Measure writes Logs/Step16-Jingles.md (append): each jingle's clip peak,
    /// mid level, share of energy above 3 kHz, cue volume and the level it plays at, against the median of the game's other
    /// one-shot cues. Bake re-applies the jingle recipes, bakes, syncs and sets the cue volume.
    /// Bridge: exec:Diorama.Audio.EditorTools.Step16Audio.MeasureBefore / Bake / MeasureAfter.
    /// </summary>
    public static class Step16Audio
    {
        public static readonly string[] Ids = { "jingle.catch", "jingle.levelup", "jingle.evolve", "jingle.shard", "jingle.keeper", "jingle.victory" };
        const string Report = "Logs/Step16-Jingles.md";

        public static void MeasureBefore() => Measure("Before");
        public static void MeasureAfter() => Measure("After");

        /// <summary>Step 16 E: the shard's hum (reworked) and its shimmer (new), baked and synced (AudioIds rewritten).</summary>
        public static void BakeOpening()
        {
            SynthBaker.CreateMissing();
            var ids = new HashSet<string> { "shard.hum", "shard.shimmer" };
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
            AudioCueTools.SyncAll();
            Debug.Log($"[Step16Audio] Baked {bake.Count} opening patches ({string.Join(", ", ids)}).");
        }

        public static void Bake()
        {
            SynthBaker.CreateMissing();
            var ids = new HashSet<string>(Ids);
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
            AudioCueTools.SyncCues();
            foreach (var c in AudioCueTools.AllCues().Where(c => ids.Contains(c.id)))
            {
                c.volume = SynthBatch1.JingleVolume;
                EditorUtility.SetDirty(c);
            }
            AssetDatabase.SaveAssets();
            AudioCueTools.SyncAll();
            Debug.Log($"[Step16Audio] Baked {bake.Count} jingles, cue volume {SynthBatch1.JingleVolume}.");
        }

        public static void Measure(string label)
        {
            var cues = AudioCueTools.AllCues();
            var ids = new HashSet<string>(Ids);
            var others = new List<float>();
            var sb = new StringBuilder();
            sb.AppendLine($"## {label} ({System.DateTime.Now:yyyy-MM-dd HH:mm})");
            sb.AppendLine("| cue | clip s | peak dBFS | mid dB | >3 kHz share | cue vol | plays at (mid + vol) |");
            sb.AppendLine("|---|---|---|---|---|---|---|");
            var rows = new List<(string id, float level)>();
            foreach (var cue in cues)
            {
                string group = AudioCueTools.Group(cue);
                bool jingle = ids.Contains(cue.id);
                if (!jingle && (cue.loop || group == "Music" || group == "Ambience" || group == "Cries")) continue;
                var clip = cue.clips != null ? cue.clips.FirstOrDefault(x => x != null) : null;
                if (clip == null) continue;
                var buf = ClipReader.ReadMono48k(clip);
                if (buf == null || buf.Length == 0) continue;
                var st = Loudness.Analyse(buf, SynthPatch.SampleRate);
                float vol = 20f * Mathf.Log10(Mathf.Max(1e-4f, cue.volume));
                float level = st.midDb + vol;
                if (!jingle)
                {
                    others.Add(level);
                    continue;
                }
                float hf = HighShare(buf, SynthPatch.SampleRate, 3000f);
                rows.Add((cue.id, level));
                sb.AppendLine($"| {cue.id} | {st.seconds:0.00} | {st.peakDb:0.0} | {st.midDb:0.0} | {hf:0.00} | {cue.volume:0.00} | {level:0.0} |");
            }
            others.Sort();
            float median = others.Count > 0 ? others[others.Count / 2] : 0f;
            sb.AppendLine();
            sb.AppendLine($"Median of {others.Count} other one-shot cues (mid + vol): {median:0.0} dB. Jingles above it: " +
                          string.Join(", ", rows.Select(r => $"{r.id.Substring(7)} {r.level - median:+0.0;-0.0}")) + " dB.");
            sb.AppendLine();
            Directory.CreateDirectory("Logs");
            File.AppendAllText(Report, sb.ToString());
            Debug.Log($"[Step16Audio] {label}: written to {Report}.\n{sb}");
        }

        /// <summary>Share of the signal's energy above <paramref name="hz"/> (2nd-order high-pass, RBJ biquad).</summary>
        public static float HighShare(float[] x, int rate, float hz)
        {
            double w = 2 * System.Math.PI * hz / rate, cs = System.Math.Cos(w), al = System.Math.Sin(w) / (2 * 0.7071);
            double b0 = (1 + cs) / 2, b1 = -(1 + cs), b2 = (1 + cs) / 2, a0 = 1 + al, a1 = -2 * cs, a2 = 1 - al;
            b0 /= a0; b1 /= a0; b2 /= a0; a1 /= a0; a2 /= a0;
            double x1 = 0, x2 = 0, y1 = 0, y2 = 0, all = 0, hi = 0;
            foreach (float s in x)
            {
                double y = b0 * s + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
                x2 = x1; x1 = s; y2 = y1; y1 = y;
                all += s * s;
                hi += y * y;
            }
            return all > 0 ? (float)(hi / all) : 0f;
        }
    }
}
