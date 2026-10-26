using System.Text;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 9d: the rest of the fire roster, one line at a time (bridge: exec:Diorama.EditorTools.Step9dSetup.X).
    /// Each writes the line (<see cref="FireRosterKit"/>), bakes it, rebuilds the species registry and saves turnaround
    /// sheets to Logs/BattleShots/9d_&lt;name&gt;.png. <see cref="Tables"/> rewrites the Cinderwood zone tables with them.
    /// </summary>
    public static class Step9dSetup
    {
        static void Build(string what, params IslingSpecies[] species)
        {
            var log = new StringBuilder($"[Step9d] {what}:");
            foreach (var s in species)
            {
                if (s == null || s.recipe == null)
                {
                    log.Append("\n  MISSING species");
                    continue;
                }
                var r = CreatureBaker.Bake(s.recipe);
                log.Append($"\n  {s.name}: {r}");
                IslingShots.Turnaround(s, $"{Step9cSetup.ShotDir}/9d_{s.name.ToLowerInvariant()}.png");
            }
            Step7Setup.Registry();
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }

        static IslingSpecies Load(string name) => AssetDatabase.LoadAssetAtPath<IslingSpecies>($"Assets/_Project/Data/Creatures/Species/{name}.asset");

        public static void Steamswan() => Build("Steamswan", FireRosterKit.Steamswan());

        static void Line(string what, IslingSpecies s) => Build(what, s, s != null ? s.evolvesTo : null);

        public static void Coalbeetle() => Line("Coalbeetle -> Forgehorn", FireRosterKit.Coalbeetle());
        public static void Mapadillo() => Line("Mapadillo -> Emberdillo", FireRosterKit.Mapadillo());
        public static void Ashmole() => Line("Ashmole -> Cindermole", FireRosterKit.Ashmole());
        public static void Cindercap() => Line("Cindercap -> Cinderbloom", FireRosterKit.Cindercap());
        public static void Embercrane() => Line("Embercrane -> Pyrecrane", FireRosterKit.Embercrane());
        public static void GlazegolemRare() => Line("Glazegolem rare look", FireRosterKit.GlazegolemRare());

        /// <summary>Rewrites the Cinderwood map and its zone tables (with the new Islings) and runs the layout checks.</summary>
        public static void Tables() => Step9aSetup.MapOnly();
    }
}
