using System.Collections.Generic;
using UnityEditor;

namespace Diorama.Audio.EditorTools
{
    /// <summary>
    /// Nov 11 evening fix list, section G: four new cues that hold generated clips only (no synth stand-in, like the step 24
    /// voices): the fingertip catching fire, the thorn vine burning, the lit hollow lanterns' quiet burn, and the hold ring's
    /// chime. A cue with no clip yet stays silent and the game plays its fallback (<see cref="AudioService.Has"/>).
    /// Sync Cues builds AudioIds from the recipes, so the ids live here.
    /// </summary>
    [InitializeOnLoad]
    public static class Nov11Audio
    {
        const string Set = "Nov11";

        static Nov11Audio() => SynthRecipes.AddProvider(Recipes);

        static IEnumerable<SynthRecipes.Recipe> Recipes()
        {
            yield return new SynthRecipes.Recipe { set = Set, group = "Cinderwood", id = "finger.catch", cue = SynthBatch1.Sfx(0.7f, 60, 2) };
            yield return new SynthRecipes.Recipe { set = Set, group = "Cinderwood", id = "thorn.burn", cue = SynthBatch1.Sfx(0.7f, 60, 2) };
            yield return new SynthRecipes.Recipe { set = Set, group = "Cinderwood", id = "lantern.burn", cue = SynthBatch1.Loop(0.3f, true, AudioBus.Ambience, 2f) };
            yield return new SynthRecipes.Recipe { set = Set, group = "Shrine", id = "hold.done", cue = SynthBatch1.Sfx(0.8f, 40, 2) };
        }
    }
}
