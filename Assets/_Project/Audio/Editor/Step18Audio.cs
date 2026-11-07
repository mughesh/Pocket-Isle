using UnityEditor;
using UnityEngine;

namespace Diorama.Audio.EditorTools
{
    /// <summary>
    /// Step 18 bridge: the new dialogue voices (each Keeper its own, a special Heart voice). Creates the missing patches,
    /// re-applies the Heart's changed recipe to its patch, bakes only these, then syncs the cues and AudioIds.
    /// </summary>
    public static class Step18Audio
    {
        public static readonly string[] Ids = { "voice.heart", "voice.keeper.galeowl", "voice.keeper.hearthstag", "voice.keeper.tidemother" };

        public static void Voices() => Bake(Ids);

        /// <summary>Step 19: the credits' lantern launch sound.</summary>
        public static void Lantern() => Bake("credits.lantern");

        /// <summary>Creates missing patches, re-applies each recipe, bakes only these ids, then syncs the cues and AudioIds.</summary>
        public static void Bake(params string[] ids)
        {
            SynthBaker.CreateMissing();
            foreach (var id in ids)
            {
                SynthPatch patch = null;
                foreach (var p in SynthBaker.AllPatches())
                    if (p.id == id) patch = p;
                var recipe = SynthRecipes.Find(id);
                if (patch == null || recipe == null)
                {
                    Debug.LogError($"[Step18Audio] {id}: no {(patch == null ? "patch" : "recipe")}.");
                    continue;
                }
                SynthRecipes.Apply(patch, recipe);
                EditorUtility.SetDirty(patch);
                SynthBaker.Bake(patch);
                Debug.Log($"[Step18Audio] Baked {id} ({patch.variants} variants, {patch.duration:0.00} s).");
            }
            AssetDatabase.SaveAssets();
            AudioCueTools.SyncAll();
        }
    }
}
