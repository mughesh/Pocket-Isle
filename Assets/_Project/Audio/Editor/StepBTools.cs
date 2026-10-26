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
