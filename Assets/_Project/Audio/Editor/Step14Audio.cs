using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Diorama.Audio.EditorTools
{
    /// <summary>Step 14d: bakes only the name card's cues (card.rise, card.sink) and syncs (bridge: exec:Diorama.Audio.EditorTools.Step14Audio.Bake).</summary>
    public static class Step14Audio
    {
        public static void Bake()
        {
            SynthBaker.CreateMissing();
            var ids = new HashSet<string>(SynthRemakes.Step14Ids);
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
            AudioCueTools.SyncAll();
            Debug.Log($"[AudioLab] Step 14d: {bake.Count} patches baked ({string.Join(", ", ids)}).");
        }
    }
}
