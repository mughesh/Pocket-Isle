using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Diorama.Audio.EditorTools
{
    /// <summary>Patch assets on disk, and baking them to .wav: Patches/&lt;group&gt;/&lt;id&gt;.asset -> Clips/Synth/&lt;group&gt;/&lt;id&gt;[_vN].wav.</summary>
    public static class SynthBaker
    {
        public const string AudioRoot = "Assets/_Project/Audio/";
        public const string PatchRoot = AudioRoot + "Patches/";
        public const string SynthClipRoot = AudioRoot + "Clips/Synth/";

        public static List<SynthPatch> AllPatches()
        {
            var list = new List<SynthPatch>();
            foreach (var guid in AssetDatabase.FindAssets("t:SynthPatch", new[] { PatchRoot.TrimEnd('/') }))
            {
                var p = AssetDatabase.LoadAssetAtPath<SynthPatch>(AssetDatabase.GUIDToAssetPath(guid));
                if (p != null) list.Add(p);
            }
            list.Sort((a, b) => string.CompareOrdinal(Group(a) + "/" + a.id, Group(b) + "/" + b.id));
            return list;
        }

        /// <summary>The patch's folder under Patches/ ("Pods", "Cries"...).</summary>
        public static string Group(SynthPatch patch)
        {
            var path = AssetDatabase.GetAssetPath(patch);
            if (!path.StartsWith(PatchRoot)) return "";
            var rel = path.Substring(PatchRoot.Length);
            int slash = rel.LastIndexOf('/');
            return slash > 0 ? rel.Substring(0, slash) : "";
        }

        public static string ClipPath(SynthPatch patch, int variant)
        {
            string name = patch.variants > 1 ? $"{patch.id}_v{variant + 1}" : patch.id;
            var group = Group(patch);
            return SynthClipRoot + (group.Length > 0 ? group + "/" : "") + name + ".wav";
        }

        public static List<AudioClip> Clips(SynthPatch patch)
        {
            var list = new List<AudioClip>();
            for (int v = 0; v < patch.variants; v++)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath(patch, v));
                if (clip != null) list.Add(clip);
            }
            return list;
        }

        /// <summary>Writes every variant, removes stale ones (a lower variant count, or the plain name), imports.</summary>
        public static void Bake(SynthPatch patch, bool import = true)
        {
            if (string.IsNullOrEmpty(patch.id))
            {
                Debug.LogError($"[AudioLab] {patch.name} has no id; not baked.");
                return;
            }
            var keep = new HashSet<string>();
            for (int v = 0; v < patch.variants; v++)
            {
                var path = ClipPath(patch, v);
                WavIO.Write(path, SynthRenderer.Render(patch, v), 1, SynthPatch.SampleRate);
                keep.Add(path);
            }
            var dir = Path.GetDirectoryName(ClipPath(patch, 0)).Replace('\\', '/');
            if (Directory.Exists(dir))
                foreach (var file in Directory.GetFiles(dir, patch.id + "*.wav"))
                {
                    var p = file.Replace('\\', '/');
                    var stem = Path.GetFileNameWithoutExtension(p);
                    bool ours = stem == patch.id || (stem.StartsWith(patch.id + "_v") && int.TryParse(stem.Substring(patch.id.Length + 2), out _));
                    if (ours && !keep.Contains(p)) AssetDatabase.DeleteAsset(p);
                }
            if (import)
                foreach (var p in keep) AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
        }

        public static void BakeAll(IList<SynthPatch> patches)
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < patches.Count; i++)
                {
                    if (EditorUtility.DisplayCancelableProgressBar("Baking sounds", patches[i].id, i / (float)patches.Count)) break;
                    Bake(patches[i], false);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }
            AssetDatabase.Refresh();
            Debug.Log($"[AudioLab] Baked {patches.Count} patches.");
        }

        /// <summary>Creates patch assets for recipes that have none yet (existing ones are left alone).</summary>
        public static int CreateMissing()
        {
            var have = new HashSet<string>();
            foreach (var p in AllPatches()) have.Add(p.id);
            int made = 0;
            foreach (var r in SynthRecipes.All())
            {
                if (have.Contains(r.id)) continue;
                var dir = PatchRoot + r.group;
                Directory.CreateDirectory(dir);
                var patch = ScriptableObject.CreateInstance<SynthPatch>();
                SynthRecipes.Apply(patch, r);
                AssetDatabase.CreateAsset(patch, $"{dir}/{r.id}.asset");
                made++;
            }
            AssetDatabase.SaveAssets();
            return made;
        }

        /// <summary>Bridge: create missing patches and bake everything.</summary>
        public static void CreateAndBakeAll()
        {
            int made = CreateMissing();
            BakeAll(AllPatches());
            Debug.Log($"[AudioLab] Created {made} new patches.");
        }
    }
}
