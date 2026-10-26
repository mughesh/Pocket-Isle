using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 9d checks through the Claude bridge (exec:Diorama.EditorTools.Step9dDebug.X), in Play: meet each new Isling
    /// in turn on the spot (MeetNext), catch it (BattleDebug.WildToRed + BattleDebug.Catch), then bring every caught one
    /// to its evolution level and let them evolve (EvolveCaught), Party to list the result.
    /// </summary>
    static class Step9dDebug
    {
        static readonly string[] Roster = { "Coalbeetle", "Mapadillo", "Ashmole", "Cindercap", "Embercrane", "Kettlewing" };
        static int _next;

        static T Find<T>() where T : Object => Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);

        static IslingSpecies Load(string name) => AssetDatabase.LoadAssetAtPath<IslingSpecies>($"Assets/_Project/Data/Creatures/Species/{name}.asset");

        /// <summary>A wild battle on the spot with the next new Isling (Lv 6).</summary>
        public static void MeetNext()
        {
            var d = Find<EncounterDirector>();
            if (!EditorApplication.isPlaying || d == null) return;
            var s = Load(Roster[_next % Roster.Length]);
            _next++;
            if (s == null) return;
            bool ok = d.DebugEncounter(new IslingInstance(s, 6));
            Debug.Log($"[Step9dDebug] Meet {s.name}: {(ok ? "battle" : "refused (a battle on, inside, or zoomed)")}.");
        }

        /// <summary>Every caught new Isling to its evolution level, then the pods evolve (GrowthDirector).</summary>
        public static void EvolveCaught()
        {
            var party = Find<Party>();
            var growth = Find<GrowthDirector>();
            if (party == null || growth == null) return;
            foreach (var c in party.members)
                if (c != null && c.species != null && c.species.evolvesTo != null && System.Array.IndexOf(Roster, c.species.name) >= 0)
                {
                    c.level = Mathf.Max(c.level, c.species.evolveLevel);
                    Debug.Log($"[Step9dDebug] {c.Name} to Lv {c.level} (evolves at {c.species.evolveLevel} into {c.species.evolvesTo.name}).");
                }
            growth.EvolveDue();
        }

        public static void Party()
        {
            var party = Find<Party>();
            if (party == null) return;
            var sb = new System.Text.StringBuilder("[Step9dDebug] Party:");
            foreach (var c in party.members) sb.Append($" {c.species.name} Lv {c.level};");
            sb.Append(" storage:");
            foreach (var c in party.storage) sb.Append($" {c.species.name};");
            Debug.Log(sb.ToString());
        }
    }
}
