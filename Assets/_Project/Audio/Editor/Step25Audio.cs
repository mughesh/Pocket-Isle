using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Diorama.Audio.EditorTools
{
    /// <summary>
    /// Step 25 bridge: re-levels the re-cut footsteps and fixed beds, retires clips that failed the measurements (moved to
    /// _step25_dropped folders no cue reads) and archives stray free-plan files.
    /// </summary>
    public static class Step25Audio
    {
        public const string Dropped = "_step25_dropped";
        const string DropList = "Logs/AudioLab/step25_drop.txt";
        const string RelevelList = "Logs/AudioLab/step25_relevel.txt";

        /// <summary>True for a free-plan file that sits outside the archive (pure).</summary>
        public static bool IsStrayFree(string path)
        {
            var p = path.Replace('\\', '/');
            return Path.GetFileName(p).Contains("_FREE") && !p.Contains("/_free_archive/") && !p.EndsWith(".meta");
        }

        /// <summary>Every free-plan audio file outside the archive folders, cue or no cue.</summary>
        public static List<string> StrayFreeFiles()
        {
            var root = AudioImportRules.Root.TrimEnd('/');
            return Directory.GetFiles(root, "*_FREE*", SearchOption.AllDirectories)
                .Where(IsStrayFree).Select(p => p.Replace('\\', '/')).OrderBy(p => p, System.StringComparer.Ordinal).ToList();
        }

        /// <summary>Bridge: moves the stray free-plan files into _free_archive.</summary>
        public static void ArchiveStrayFree()
        {
            int moved = 0;
            foreach (var path in StrayFreeFiles())
            {
                var dst = path.Replace("/Gen/", $"/Gen/{Step24Audio.Archive}/");
                if (Step24Audio.Move(path, dst)) moved++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Step25Audio] Archived {moved} stray free files, {StrayFreeFiles().Count} left outside the archive.");
        }

        /// <summary>Bridge: re-bakes the levelled copies of the cues named in step25_relevel.txt (one id a line) from Clips/Gen.</summary>
        public static void Relevel()
        {
            var ids = new HashSet<string>(File.ReadAllLines(RelevelList).Select(l => l.Trim()).Where(l => l.Length > 0));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            int baked = 0;
            foreach (var cue in AudioCueTools.AllCues().Where(c => ids.Contains(c.id)))
            {
                var patch = SynthBaker.AllPatches().Find(p => p.id == cue.id);
                var tier = AudioTiers.ForCue(cue, patch);
                float trim = AudioTiers.TrimFor(cue, patch);
                baked += LevelledClips.For(AudioCueTools.ExtraClips(new[] { $"Clips/Gen/{cue.id}/" }), tier, trim, true, !cue.loop).Count;
            }
            AudioCueTools.SyncAll();
            Debug.Log($"[Step25Audio] Re-levelled {baked} clips in {ids.Count} cues.");
        }

        /// <summary>Bridge: moves the source and levelled copy of each file named in step25_drop.txt (paths under Clips/Gen) out of its cue, then syncs.</summary>
        public static void Drop()
        {
            int moved = 0;
            foreach (var line in File.ReadAllLines(DropList).Select(l => l.Trim()).Where(l => l.Length > 0))
            {
                var rel = line.Replace('\\', '/');
                var levelled = LevelledClips.LevelledRoot + "Gen/" + rel;
                if (Step24Audio.Move(GenImporter.GenRoot + rel, $"{GenImporter.GenRoot}{Dropped}/{rel}")) moved++;
                Step24Audio.Move(levelled, $"{LevelledClips.LevelledRoot}Gen/{Dropped}/{rel}");
            }
            AssetDatabase.SaveAssets();
            AudioCueTools.SyncAll();
            Debug.Log($"[Step25Audio] Dropped {moved} clips.");
        }
    }
}
