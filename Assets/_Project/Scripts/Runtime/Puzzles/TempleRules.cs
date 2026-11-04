using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Nov 4 (Mugesh: the Keepers fell to a single Isling): a temple door lets the explorer in only with enough Islings in
    /// the pods. Galewick's tower needs <see cref="GalewickIslings"/>, the forge and the lake temple
    /// <see cref="OtherIslings"/>. The Quick Journey has no minimum (one Isling, timed). Short: the Spirit says
    /// <see cref="Line"/> (at most every <see cref="NagSeconds"/>) and the door stays shut to walking in.
    /// </summary>
    public static class TempleRules
    {
        public const int GalewickIslings = 4, OtherIslings = 6;
        public const float NagSeconds = 8f;

        /// <summary>Islings needed at a temple door (0 = no minimum).</summary>
        public static int Needed(bool galewick, bool quick) => quick ? 0 : galewick ? GalewickIslings : OtherIslings;

        public static bool CanEnter(int carried, int needed) => carried >= needed;

        /// <summary>The Spirit's line when short.</summary>
        public static string Line(int needed) => needed >= OtherIslings ? "temple.few6" : "temple.few4";

        /// <summary>Islings in the pods (fainted ones count: the shrine heals them).</summary>
        public static int Carried(Party party)
        {
            if (party == null) return 0;
            int n = 0;
            for (int i = 0; i < party.members.Count && i < Party.Size; i++)
                if (party.members[i] != null && party.members[i].species != null) n++;
            return n;
        }

        static Party _party;

        /// <summary>
        /// True when the explorer may walk in. Otherwise the Spirit explains (throttled by <paramref name="nagged"/>) and
        /// the caller keeps the door shut.
        /// </summary>
        public static bool Gate(bool galewick, ref float nagged, Object door)
        {
            int needed = Needed(galewick, QuickJourney.Active);
            if (needed <= 0) return true;
            if (_party == null) _party = Object.FindFirstObjectByType<Party>();
            if (_party == null)
            {
                Debug.LogError("[TempleRules] No Party in the scene: can't count Islings, letting the explorer in.", door);
                return true;
            }
            int carried = Carried(_party);
            if (CanEnter(carried, needed)) return true;
            if (Time.time - nagged >= NagSeconds)
            {
                nagged = Time.time;
                Dialogue.Say(Line(needed));
                Debug.Log($"[TempleRules] {door.name}: {carried} Isling(s) in the pods, needs {needed}. Door stays shut.", door);
            }
            return false;
        }
    }
}
