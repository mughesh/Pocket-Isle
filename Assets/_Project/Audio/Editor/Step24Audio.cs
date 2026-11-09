using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Diorama.Audio.EditorTools
{
    /// <summary>
    /// Step 24 + step 23 Part 2: the cues for the Keeper and Heart voices and the acoustic jingles (generated clips only), and the
    /// paid-plan import: the paid batch goes in, each cue's free-plan clips move to _free_archive folders no cue reads.
    /// </summary>
    [InitializeOnLoad]
    public static class Step24Audio
    {
        const string Set = "Step24";
        internal const string Archive = "_free_archive";
        public static readonly string[] Speakers = { "galeowl", "hearthstag", "tidemother", "heart" };
        public static readonly string[] Jingles = { "jingle.catch", "jingle.levelup", "jingle.victory", "jingle.evolve", "jingle.shard", "jingle.keeper" };

        static Step24Audio() => SynthRecipes.AddProvider(Recipes);

        static IEnumerable<SynthRecipes.Recipe> Recipes()
        {
            foreach (var s in Speakers)
            {
                yield return new SynthRecipes.Recipe { set = Set, group = "Cries", id = "voice.call." + s, cue = SynthBatch1.Voice(0.6f, 1) };
                yield return new SynthRecipes.Recipe { set = Set, group = "Cries", id = "voice.grain." + s, cue = SynthBatch1.Voice(0.5f, 2) };
            }
            foreach (var j in Jingles)
                yield return new SynthRecipes.Recipe { set = Set, group = "Jingles", id = j + ".acoustic", cue = SynthBatch1.Jingle(SynthBatch1.JingleVolume) };
        }

        public static string PaidRoot => Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "_audio-sources", "elevenlabs-paid"));

        /// <summary>Bridge: imports the paid batch, archives the free clips of every cue that now has paid ones, re-syncs. Lists any cue left with a free clip.</summary>
        public static void ImportPaid()
        {
            int copied = GenImporter.Import(PaidRoot);
            int moved = ArchiveFree(out var kept);
            AudioCueTools.SyncAll();
            var free = GenImporter.FreeClipsInCues();
            Debug.Log($"[Step24Audio] Paid import: {copied} new, {moved} free files archived, {free.Count} free clips still in cues." +
                      (kept.Count > 0 ? " No paid clips yet for: " + string.Join(", ", kept) : ""));
        }

        static int ArchiveFree(out List<string> kept)
        {
            kept = new List<string>();
            int moved = 0;
            string genRoot = GenImporter.GenRoot.TrimEnd('/');
            string levelledRoot = LevelledClips.LevelledRoot + "Gen";
            foreach (var dir in Directory.GetDirectories(genRoot))
            {
                var cue = Path.GetFileName(dir);
                if (cue == Archive) continue;
                var wavs = Directory.GetFiles(dir, "*.wav").Concat(Directory.GetFiles(dir, "*.mp3")).ToList();
                var free = wavs.Where(f => Path.GetFileName(f).Contains("_FREE")).ToList();
                if (free.Count == 0) continue;
                if (free.Count == wavs.Count && cue != "hit.weak") // hit.weak: near-silent paid takes were dropped, its synth takes stay
                {
                    kept.Add(cue);
                    continue;
                }
                foreach (var f in free)
                {
                    var name = Path.GetFileName(f);
                    if (Move($"{genRoot}/{cue}/{name}", $"{genRoot}/{Archive}/{cue}/{name}")) moved++;
                    var levelled = Path.ChangeExtension(name, ".wav");
                    Move($"{levelledRoot}/{cue}/{levelled}", $"{levelledRoot}/{Archive}/{cue}/{levelled}");
                }
            }
            AssetDatabase.SaveAssets();
            return moved;
        }

        internal static bool Move(string from, string to)
        {
            if (!File.Exists(from)) return false;
            EnsureFolder(Path.GetDirectoryName(to).Replace('\\', '/'));
            var error = AssetDatabase.MoveAsset(from, to);
            if (!string.IsNullOrEmpty(error)) Debug.LogError($"[Step24Audio] Can't archive {from}: {error}");
            return string.IsNullOrEmpty(error);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        /// <summary>Bridge: per cue the paid batch touched: clip count and length. Logs/AudioLab/paid.txt.</summary>
        public static void Report()
        {
            var sb = new StringBuilder("cue | clips | seconds min-max | tier\n");
            foreach (var cue in AudioCueTools.AllCues())
            {
                var clips = cue.clips.Where(c => c != null && AssetDatabase.GetAssetPath(c).Contains("/Levelled/Gen/")).ToList();
                if (clips.Count == 0) continue;
                sb.AppendLine($"{cue.id} | {clips.Count} of {cue.clips.Length} | {clips.Min(c => c.length):0.00}-{clips.Max(c => c.length):0.00} | {AudioTiers.ForCue(cue, null)}");
            }
            Directory.CreateDirectory("Logs/AudioLab");
            File.WriteAllText("Logs/AudioLab/paid.txt", sb.ToString());
            Debug.Log("[Step24Audio] Report written: Logs/AudioLab/paid.txt");
        }
    }
}
