using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Diorama.Audio.EditorTools
{
    /// <summary>
    /// Cue assets from the recipes and baked clips, the cue table, validation, and AudioIds.cs.
    /// Tools/Pocket Isle/Audio/Sync Cues does all of it: create missing cues (recipe settings), refresh every cue's clips
    /// (synth variants + extra clips such as Kenney), rebuild the table, validate, write AudioIds.cs.
    /// </summary>
    [InitializeOnLoad]
    public static class AudioCueTools
    {
        public const string CueRoot = SynthBaker.AudioRoot + "Data/Cues/";
        public const string TablePath = SynthBaker.AudioRoot + "Data/Resources/AudioCueTable.asset";
        public const string IdsPath = SynthBaker.AudioRoot + "Runtime/AudioIds.cs";
        public const string ValidationPath = "Logs/AudioLab/validation.txt";

        static AudioCueTools()
        {
            AudioLabWindow.ExtraRows = CueOnlyRows;
        }

        [MenuItem("Tools/Pocket Isle/Audio/Sync Cues (create, clips, table, AudioIds)", priority = 91)]
        public static void SyncAll()
        {
            int made = SyncCues();
            var table = BuildTable();
            var report = Validate(table);
            bool idsChanged = WriteIds(table);
            Debug.Log($"[AudioLab] Synced cues: {table.cues.Count} in the table, {made} new. {report}" +
                      (idsChanged ? " AudioIds.cs rewritten." : ""));
        }

        [MenuItem("Tools/Pocket Isle/Audio/Validate Cue Table", priority = 92)]
        public static void ValidateMenu()
        {
            var table = AssetDatabase.LoadAssetAtPath<AudioCueTable>(TablePath);
            if (table == null)
            {
                Debug.LogError("[AudioLab] No cue table yet: run Sync Cues.");
                return;
            }
            Debug.Log("[AudioLab] " + Validate(table));
        }

        public static List<AudioCue> AllCues()
        {
            var list = new List<AudioCue>();
            if (!AssetDatabase.IsValidFolder(CueRoot.TrimEnd('/'))) return list;
            foreach (var guid in AssetDatabase.FindAssets("t:AudioCue", new[] { CueRoot.TrimEnd('/') }))
            {
                var c = AssetDatabase.LoadAssetAtPath<AudioCue>(AssetDatabase.GUIDToAssetPath(guid));
                if (c != null) list.Add(c);
            }
            list.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            return list;
        }

        public static string Group(AudioCue cue)
        {
            var path = AssetDatabase.GetAssetPath(cue);
            if (!path.StartsWith(CueRoot)) return "";
            var rel = path.Substring(CueRoot.Length);
            int slash = rel.LastIndexOf('/');
            return slash > 0 ? rel.Substring(0, slash) : "";
        }

        /// <summary>Creates cues that are missing (recipe settings) and refreshes every cue's clip list.</summary>
        public static int SyncCues()
        {
            var cues = AllCues().ToDictionary(c => c.id, c => c);
            var patches = SynthBaker.AllPatches().ToDictionary(p => p.id, p => p);
            int made = 0;
            foreach (var r in SynthRecipes.All())
            {
                if (!cues.TryGetValue(r.id, out var cue))
                {
                    var dir = CueRoot + r.group;
                    Directory.CreateDirectory(dir);
                    cue = ScriptableObject.CreateInstance<AudioCue>();
                    cue.id = r.id;
                    r.cue?.Invoke(cue);
                    AssetDatabase.CreateAsset(cue, $"{dir}/{r.id}.asset");
                    cues[r.id] = cue;
                    made++;
                }
                var clips = new List<AudioClip>();
                patches.TryGetValue(r.id, out var patch);
                if (!string.IsNullOrEmpty(r.mixWith) && patch != null) clips.AddRange(MixBaker.Clips(patch, r));
                else if (patch != null) clips.AddRange(SynthBaker.Clips(patch));
                clips.AddRange(ExtraClips(r.extraClips));
                if (!cue.clips.SequenceEqual(clips))
                {
                    cue.clips = clips.ToArray();
                    EditorUtility.SetDirty(cue);
                }
            }
            AssetDatabase.SaveAssets();
            return made;
        }

        /// <summary>Clips whose asset path starts with any prefix (relative to Assets/_Project/Audio/).</summary>
        public static List<AudioClip> ExtraClips(string[] prefixes)
        {
            var list = new List<AudioClip>();
            if (prefixes == null) return list;
            foreach (var prefix in prefixes)
            {
                var full = SynthBaker.AudioRoot + prefix;
                var dir = Path.GetDirectoryName(full).Replace('\\', '/');
                if (!AssetDatabase.IsValidFolder(dir)) continue;
                var paths = AssetDatabase.FindAssets("t:AudioClip", new[] { dir })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(p => p.StartsWith(full) && Path.GetDirectoryName(p).Replace('\\', '/') == dir)
                    .OrderBy(p => p, System.StringComparer.Ordinal);
                foreach (var p in paths) list.Add(AssetDatabase.LoadAssetAtPath<AudioClip>(p));
            }
            return list;
        }

        /// <summary>Resets a cue's settings to its recipe (keeps clips and review).</summary>
        public static void Revert(AudioCue cue)
        {
            var r = SynthRecipes.Find(cue.id);
            if (r == null) return;
            var clips = cue.clips;
            var review = cue.review;
            var fresh = ScriptableObject.CreateInstance<AudioCue>();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(fresh), cue);
            Object.DestroyImmediate(fresh);
            cue.id = r.id;
            cue.clips = clips;
            cue.review = review;
            r.cue?.Invoke(cue);
            EditorUtility.SetDirty(cue);
        }

        public static AudioCueTable BuildTable()
        {
            var table = AssetDatabase.LoadAssetAtPath<AudioCueTable>(TablePath);
            if (table == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(TablePath));
                table = ScriptableObject.CreateInstance<AudioCueTable>();
                AssetDatabase.CreateAsset(table, TablePath);
            }
            var cues = AllCues();
            if (!table.cues.SequenceEqual(cues))
            {
                table.cues = cues;
                EditorUtility.SetDirty(table);
                AssetDatabase.SaveAssets();
            }
            return table;
        }

        /// <summary>Duplicate ids, cues without clips, clips no cue plays. Full list in Logs/AudioLab/validation.txt.</summary>
        public static string Validate(AudioCueTable table)
        {
            var sb = new StringBuilder();
            int dup = 0, empty = 0, unused = 0, waiting = 0;
            foreach (var g in table.cues.Where(c => c != null).GroupBy(c => c.id).Where(g => g.Count() > 1))
            {
                dup++;
                sb.AppendLine($"DUPLICATE id '{g.Key}': {string.Join(", ", g.Select(AssetDatabase.GetAssetPath))}");
            }
            var used = new HashSet<AudioClip>();
            // Mix sources (the synth sparkle and the Kenney glass behind catch.shatter's mixes) are used through the mixes.
            foreach (var r in SynthRecipes.All())
            {
                if (string.IsNullOrEmpty(r.mixWith)) continue;
                foreach (var x in ExtraClips(new[] { r.mixWith })) used.Add(x);
                var patch = SynthBaker.AllPatches().Find(p => p.id == r.id);
                if (patch != null) foreach (var x in SynthBaker.Clips(patch)) used.Add(x);
            }
            foreach (var c in table.cues.Where(c => c != null))
            {
                if (string.IsNullOrEmpty(c.id)) sb.AppendLine($"NO ID: {AssetDatabase.GetAssetPath(c)}");
                var clips = c.clips.Where(x => x != null).ToArray();
                if (clips.Length != c.clips.Length) sb.AppendLine($"NULL CLIP in '{c.id}'");
                if (clips.Length == 0)
                {
                    if (c.awaitingClips) waiting++;
                    else
                    {
                        empty++;
                        sb.AppendLine($"NO CLIPS: '{c.id}'");
                    }
                }
                foreach (var x in clips) used.Add(x);
            }
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { AudioImportRules.Root.TrimEnd('/') }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null && !used.Contains(clip))
                {
                    unused++;
                    sb.AppendLine($"UNUSED CLIP: {path}");
                }
            }
            var summary = $"Validation: {dup} duplicate ids, {empty} cues without clips, {waiting} awaiting clips, {unused} unused clips.";
            Directory.CreateDirectory(Path.GetDirectoryName(ValidationPath));
            File.WriteAllText(ValidationPath, summary + "\n" + sb);
            if (dup > 0 || empty > 0) Debug.LogWarning("[AudioLab] " + summary + " See " + ValidationPath);
            return summary;
        }

        /// <summary>Writes AudioIds.cs (const strings, so hooks are compile-checked). Returns true if the file changed.</summary>
        public static bool WriteIds(AudioCueTable table)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// Generated by Tools/Pocket Isle/Audio/Sync Cues. Do not edit by hand.");
            sb.AppendLine("namespace Diorama.Audio");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>Every cue id as a constant: AudioService.Play2D(AudioIds.PodThrow).</summary>");
            sb.AppendLine("    public static class AudioIds");
            sb.AppendLine("    {");
            var names = new HashSet<string>();
            foreach (var c in table.cues.Where(c => c != null && !string.IsNullOrEmpty(c.id)).OrderBy(c => c.id, System.StringComparer.Ordinal))
            {
                var name = Identifier(c.id);
                if (!names.Add(name)) continue;
                sb.AppendLine($"        public const string {name} = \"{c.id}\";");
            }
            sb.AppendLine();
            sb.AppendLine("        /// <summary>The cry cue of a species: cry.&lt;speciesId&gt;.</summary>");
            sb.AppendLine("        public static string Cry(string speciesId) => \"cry.\" + speciesId;");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            var text = sb.ToString().Replace("\r\n", "\n");
            if (File.Exists(IdsPath) && File.ReadAllText(IdsPath).Replace("\r\n", "\n") == text) return false;
            File.WriteAllText(IdsPath, text);
            AssetDatabase.ImportAsset(IdsPath);
            return true;
        }

        /// <summary>"move.shoot.fire" -> MoveShootFire, "cry.kilnpot" -> CryKilnpot.</summary>
        public static string Identifier(string id)
        {
            var sb = new StringBuilder();
            bool upper = true;
            foreach (var ch in id)
            {
                if (!char.IsLetterOrDigit(ch))
                {
                    upper = true;
                    continue;
                }
                sb.Append(upper ? char.ToUpperInvariant(ch) : ch);
                upper = false;
            }
            if (sb.Length == 0 || char.IsDigit(sb[0])) sb.Insert(0, '_');
            return sb.ToString();
        }

        static IEnumerable<AudioLabWindow.ReviewRow> CueOnlyRows()
        {
            foreach (var c in AllCues())
            {
                var cue = c;
                var r = SynthRecipes.Find(c.id);
                yield return new AudioLabWindow.ReviewRow
                {
                    id = c.id,
                    group = Group(c),
                    gap = r != null && r.gap,
                    review = c.review,
                    owner = c,
                    info = c.awaitingClips ? "Awaiting clips (Lyria)" : $"{c.clips.Length} clips",
                    play = () =>
                    {
                        var clip = cue.Pick();
                        if (clip != null) AudioPreview.Play(clip, cue.loop);
                        else Debug.Log($"[AudioLab] '{cue.id}' has no clips{(cue.awaitingClips ? " yet (awaiting Lyria)" : "")}.");
                    },
                };
            }
        }
    }
}
