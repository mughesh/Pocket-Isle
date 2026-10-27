using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Diorama.Audio.EditorTools
{
    /// <summary>
    /// Imports the ElevenLabs batch made by the gen lane (its manifest.json, outside the repo) into Clips/Gen/&lt;cueId&gt;/,
    /// keeping each file's name (plus _FREE for free-plan clips). Sync Cues then adds their levelled copies to the cue next to
    /// the synth variants (synth clips are never removed). Re-running copies only what's new.
    /// Source folder: EditorPrefs "PocketIsle.Audio.GenRoot", default ../_audio-sources/elevenlabs next to the project.
    /// </summary>
    public static class GenImporter
    {
        public const string GenRoot = AudioImportRules.Root + "Gen/";
        public const string PickNote = "Gen vs synth: pick.";
        const string PrefKey = "PocketIsle.Audio.GenRoot";

        [Serializable]
        class Manifest
        {
            public List<Clip> clips = new List<Clip>();
        }

        [Serializable]
        class Clip
        {
            public string cue = "", file = "", original = "", plan = "", prompt = "", note = "", why = "";
            public bool loop;
        }

        public static string SourceRoot
        {
            get
            {
                var saved = EditorPrefs.GetString(PrefKey, "");
                if (!string.IsNullOrEmpty(saved)) return saved;
                return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "_audio-sources", "elevenlabs"));
            }
            set => EditorPrefs.SetString(PrefKey, value ?? "");
        }

        [MenuItem("Tools/Pocket Isle/Audio/Import Gen Batch", priority = 93)]
        public static void ImportMenu() => Import(SourceRoot);

        /// <summary>Bridge: test the importer on a manifest somewhere else (e.g. a fake one in Temp).</summary>
        public static void ImportFromTemp() => Import(Path.GetFullPath("Temp/gen-test"));

        /// <summary>Copies new manifest clips, syncs cues (levelled copies), marks the cues for a pick, analyses. Returns clips copied.</summary>
        public static int Import(string root)
        {
            var manifestPath = Path.Combine(root, "manifest.json");
            if (!File.Exists(manifestPath))
            {
                Debug.LogError($"[AudioLab] No gen manifest at {manifestPath}. Set the folder with GenImporter.SourceRoot (EditorPrefs {PrefKey}).");
                return 0;
            }
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
            var known = new HashSet<string>(SynthRecipes.All().Select(r => r.id));
            int copied = 0, skipped = 0, missing = 0;
            var unknown = new SortedSet<string>();
            var touched = new Dictionary<string, List<Clip>>();
            foreach (var c in manifest.clips)
            {
                if (string.IsNullOrEmpty(c.cue) || string.IsNullOrEmpty(c.file)) continue;
                var src = Path.Combine(root, c.file);
                if (!File.Exists(src))
                {
                    missing++;
                    Debug.LogWarning($"[AudioLab] Gen manifest lists {c.file}, but it isn't there.");
                    continue;
                }
                if (!known.Contains(c.cue)) unknown.Add(c.cue);
                var dst = DestPath(c);
                if (!touched.TryGetValue(c.cue, out var list)) touched[c.cue] = list = new List<Clip>();
                list.Add(c);
                if (File.Exists(dst))
                {
                    skipped++;
                    continue;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(dst));
                File.Copy(src, dst);
                AssetDatabase.ImportAsset(dst);
                copied++;
            }
            AudioCueTools.SyncAll();
            MarkForPick(touched);
            Debug.Log($"[AudioLab] Gen import from {root}: {copied} new, {skipped} already here, {missing} missing." +
                      (unknown.Count > 0 ? $" No cue yet for: {string.Join(", ", unknown)} (copied; they join a cue once its recipe exists)." : ""));
            return copied;
        }

        /// <summary>Clips/Gen/&lt;cue&gt;/&lt;name&gt;[_FREE].&lt;ext&gt;</summary>
        static string DestPath(Clip c)
        {
            var name = Path.GetFileNameWithoutExtension(c.file);
            bool free = !string.Equals(c.plan, "paid", StringComparison.OrdinalIgnoreCase);
            if (free && !name.EndsWith("_FREE")) name += "_FREE";
            return $"{GenRoot}{c.cue}/{name}{Path.GetExtension(c.file).ToLowerInvariant()}";
        }

        /// <summary>Review note on every cue that now has gen variants: "Gen vs synth: pick." plus the gen lane's verdicts.</summary>
        static void MarkForPick(Dictionary<string, List<Clip>> touched)
        {
            var patches = SynthBaker.AllPatches().GroupBy(p => p.id).ToDictionary(g => g.Key, g => g.First());
            var cues = AudioCueTools.AllCues().ToDictionary(c => c.id, c => c);
            foreach (var kv in touched)
            {
                UnityEngine.Object owner = null;
                AudioReview review = null;
                if (patches.TryGetValue(kv.Key, out var p))
                {
                    owner = p;
                    review = p.review;
                }
                else if (cues.TryGetValue(kv.Key, out var c))
                {
                    owner = c;
                    review = c.review;
                }
                if (review == null) continue;
                if (!string.IsNullOrEmpty(review.note) && !review.note.StartsWith("Gen vs synth") && !review.note.StartsWith("Compare:")) continue;
                var verdicts = string.Join("; ", kv.Value.Select(x => $"{Path.GetFileNameWithoutExtension(x.file)} {x.note}" + (string.IsNullOrEmpty(x.why) ? "" : $" ({x.why})")));
                var plan = kv.Value.Any(x => !string.Equals(x.plan, "paid", StringComparison.OrdinalIgnoreCase)) ? " Free-plan clips: listening only." : "";
                var note = $"{PickNote}{plan} Gen lane: {verdicts}";
                if (review.note == note) continue;
                review.note = note;
                EditorUtility.SetDirty(owner);
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>Every clip a cue plays that came from a free-plan generation (no commercial licence).</summary>
        public static List<string> FreeClipsInCues()
        {
            var list = new List<string>();
            foreach (var cue in AudioCueTools.AllCues())
                foreach (var clip in cue.clips)
                {
                    if (clip == null) continue;
                    var path = AssetDatabase.GetAssetPath(clip);
                    if (path.Contains("_FREE")) list.Add($"{cue.id}: {path}");
                }
            return list;
        }
    }
}
