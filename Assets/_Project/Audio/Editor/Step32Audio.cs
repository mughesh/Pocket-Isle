using System.Collections.Generic;
using UnityEditor;

namespace Diorama.Audio.EditorTools
{
    /// <summary>
    /// Step 32 C: six Heart voice candidates for Mugesh to pick by ear (Dev panel > Tools > Sound A/B > Heart voice): a call
    /// (voice.call.heart.cN) and its murmur grains (voice.grain.heart.cN) each, generated clips only (the lane's
    /// s32_heart.py picks and copies them into Clips/Gen). A cue with no clips falls back to the synth moo.
    /// </summary>
    [InitializeOnLoad]
    public static class Step32Audio
    {
        const string Set = "Step32";

        static Step32Audio() => SynthRecipes.AddProvider(Recipes);

        static IEnumerable<SynthRecipes.Recipe> Recipes()
        {
            for (int n = 1; n <= AudioAB.HeartCandidates; n++)
            {
                yield return new SynthRecipes.Recipe { set = Set, group = "Cries", id = $"voice.call.heart.c{n}", cue = SynthBatch1.Voice(0.8f, 1) };
                yield return new SynthRecipes.Recipe { set = Set, group = "Cries", id = $"voice.grain.heart.c{n}", cue = SynthBatch1.Voice(0.5f, 2) };
            }
        }
    }
}
