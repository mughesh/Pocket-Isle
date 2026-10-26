using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Diorama.Audio.EditorTools
{
    /// <summary>
    /// Imported clips (Kenney, the ElevenLabs gen batch) are never played as they are: each gets a trimmed, tier-levelled copy
    /// in Clips/Levelled/&lt;same sub path&gt;.wav (mono 48 kHz), and the cue plays the copy. The source files stay untouched
    /// (and stay out of the build, since nothing references them).
    /// </summary>
    public static class LevelledClips
    {
        public const string LevelledRoot = AudioImportRules.Root + "Levelled/";

        /// <summary>Sources that are levelled before use (music is levelled by the Loop tool, not here).</summary>
        public static bool IsSource(string path) =>
            path.StartsWith(AudioImportRules.Root + "Kenney/") || path.StartsWith(AudioImportRules.Root + "Gen/");

        public static string PathFor(string sourcePath)
        {
            var rel = sourcePath.Substring(AudioImportRules.Root.Length);
            return LevelledRoot + Path.ChangeExtension(rel, ".wav").Replace('\\', '/');
        }

        /// <summary>The levelled copy of each source (made if missing, or always with force); non-sources pass through.</summary>
        public static List<AudioClip> For(IEnumerable<AudioClip> clips, LoudnessTier tier, float trimDb, bool force = false, bool trimEnds = true)
        {
            var list = new List<AudioClip>();
            foreach (var clip in clips)
            {
                if (clip == null) continue;
                var src = AssetDatabase.GetAssetPath(clip);
                if (!IsSource(src))
                {
                    list.Add(clip);
                    continue;
                }
                var dst = PathFor(src);
                if (force || !File.Exists(dst))
                {
                    if (!Bake(clip, dst, tier, trimDb, trimEnds)) continue;
                }
                var levelled = AssetDatabase.LoadAssetAtPath<AudioClip>(dst);
                if (levelled != null) list.Add(levelled);
            }
            return list;
        }

        /// <summary>Trim the ends (not for loops), level the mid band to the tier, peaks under -1 dBFS. Deterministic (same bytes per source).</summary>
        public static bool Bake(AudioClip source, string dst, LoudnessTier tier, float trimDb, bool trimEnds = true)
        {
            var samples = ClipReader.ReadMono48k(source);
            if (samples == null)
            {
                Debug.LogError($"[AudioLab] Can't read {AssetDatabase.GetAssetPath(source)}; no levelled copy made.");
                return false;
            }
            // Speaker-safe: a mostly-sub clip gets a 220 Hz high-pass (4th order) so small speakers play its body.
            if (tier != LoudnessTier.Music && Loudness.Analyse(samples, SynthPatch.SampleRate).lowShare > AudioScope.MaxLowShare - 0.1f) // margin: limiting raises it a little
            {
                var h1 = new Svf();
                var h2 = new Svf();
                for (int i = 0; i < samples.Length; i++)
                {
                    h1.Process(samples[i], 220f, 0.54f, out _, out _, out float y);
                    h2.Process(y, 220f, 1.31f, out _, out _, out y);
                    samples[i] = y;
                }
            }
            if (trimEnds) samples = Loudness.TrimSilence(samples, SynthPatch.SampleRate);
            if (tier != LoudnessTier.Music) Loudness.LevelTo(samples, SynthPatch.SampleRate, Loudness.TierDb(tier) + trimDb); // 2 passes: more limiting squashes peaky clips and raises their low share
            WavIO.Write(dst, samples, 1, SynthPatch.SampleRate);
            AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceUpdate);
            return true;
        }
    }
}
