using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Diorama.Audio.EditorTools
{
    /// <summary>Bridge entry points for step audio-B (exec:Diorama.Audio.EditorTools.StepBTools.&lt;Method&gt;).</summary>
    public static class StepBTools
    {
        /// <summary>
        /// Part 1: every recipe patch except the cries back to its recipe (now tier-levelled), bake, rebuild the levelled
        /// copies of the imports, all cue volumes to CueVolume, sync, analyse into Temp/audio-analysis/.
        /// </summary>
        public static void RebakeToTiers()
        {
            var baked = new List<SynthPatch>();
            foreach (var p in SynthBaker.AllPatches())
            {
                if (p.recipe == "cry") continue;
                var r = SynthRecipes.Find(p.id);
                if (r == null || r.build == null) continue;
                SynthRecipes.Apply(p, r);
                EditorUtility.SetDirty(p);
                baked.Add(p);
            }
            AssetDatabase.SaveAssets();
            SynthBaker.BakeAll(baked);
            AudioCueTools.SyncCues(true);
            SetCueVolumes();
            AudioCueTools.SyncAll();
            AudioScope.AnalyseAll();
            Debug.Log($"[AudioLab] Re-baked {baked.Count} patches to their tiers.");
        }

        /// <summary>Part 2: every model preset rendered and tier-levelled into Temp/audio-analysis/Models.png (Claude reads it).</summary>
        public static void ModelGallery()
        {
            var pitch = new Dictionary<string, float>
            {
                ["Modal/Wood knock"] = 700f, ["Modal/Wood thud (log)"] = 260f, ["Modal/Stone clack"] = 900f, ["Modal/Glass clink"] = 1800f,
                ["Modal/Clay pot tok"] = 420f, ["Modal/Brass clang"] = 700f, ["Modal/Crystal ting"] = 1400f,
                ["Friction/Wood creak"] = 320f, ["Friction/Rope creak"] = 520f, ["Friction/Chain creak"] = 900f, ["Friction/Leather squeak"] = 700f,
            };
            var renders = new List<KeyValuePair<string, float[]>>();
            foreach (var name in ModelPresets.Names)
            {
                if (!name.Contains("/")) continue;
                var p = ScriptableObject.CreateInstance<SynthPatch>();
                p.id = name;
                p.duration = 1.2f;
                p.levelMode = LevelMode.Tier;
                p.tier = LoudnessTier.Island;
                var layer = ModelPresets.Layer(name, pitch.TryGetValue(name, out float hz) ? hz : 440f);
                if (layer.amp.sustain > 0f) layer.gate = 1f; // steady presets: one second, then release
                p.layers = new List<SynthLayer> { layer };
                renders.Add(new KeyValuePair<string, float[]>(name.Replace('/', '-'), SynthRenderer.Render(p)));
                Object.DestroyImmediate(p);
            }
            AudioScope.Sheet("Models", renders, Loudness.TierDb(LoudnessTier.Island));
            Debug.Log($"[AudioLab] Model gallery: {renders.Count} presets in {AudioScope.OutDir}Models.png");
        }

        /// <summary>Every cue except music and cries plays at SynthBatch1.CueVolume.</summary>
        public static void SetCueVolumes()
        {
            foreach (var c in AudioCueTools.AllCues())
            {
                var g = AudioCueTools.Group(c);
                if (g == "Music" || g == "Cries" || Mathf.Approximately(c.volume, SynthBatch1.CueVolume)) continue;
                c.volume = SynthBatch1.CueVolume;
                EditorUtility.SetDirty(c);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
