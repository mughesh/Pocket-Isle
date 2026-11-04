using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Diorama
{
    /// <summary>
    /// Nov 4 / step 14h: at a temple door with fewer Islings in the pods than most guardians bring (Galewick's tower
    /// <see cref="GalewickIslings"/>, the forge and the lake temple <see cref="OtherIslings"/>), the Spirit warns
    /// (<see cref="WarnLine"/>: "{keeper} is strong. Most guardians bring {n} friends.") and two tablets ask
    /// <b>Go in</b> / <b>Not yet</b>. Go in enters as usual: a warning, never a wall (the Keeper is tuned to the team,
    /// <see cref="KeeperScaling"/>). No warning in the Quick Journey.
    /// </summary>
    public static class TempleRules
    {
        public const int GalewickIslings = 4, OtherIslings = 6;
        public const float AskAgainSeconds = 3f;
        public const string WarnLine = "temple.warn", GoIn = "Go in", NotYet = "Not yet";

        /// <summary>Islings suggested at a temple door (0 = no warning).</summary>
        public static int Needed(bool galewick, bool quick) => quick ? 0 : galewick ? GalewickIslings : OtherIslings;

        /// <summary>Enough Islings: no question, straight in.</summary>
        public static bool CanEnter(int carried, int needed) => carried >= needed;

        /// <summary>The Spirit's warning (the same line at every door; {keeper} and {n} are filled in).</summary>
        public static string Line(int needed) => WarnLine;

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
        /// True when the explorer may walk straight in. Otherwise (a small team) the Spirit warns, Go in / Not yet show,
        /// and <paramref name="enter"/> runs on Go in. <paramref name="asked"/> keeps a bump right after "Not yet" quiet.
        /// </summary>
        public static bool Gate(bool galewick, string keeper, ref float asked, Object door, Action enter)
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
            if (ChoiceBoard.IsOpen || Time.time - asked < AskAgainSeconds) return false;
            asked = Time.time;
            Dialogue.SetVar("keeper", keeper);
            Dialogue.SetVar("n", needed.ToString());
            Dialogue.Say(WarnLine);
            string name = door != null ? door.name : "door";
            Debug.Log($"[TempleRules] {name}: {carried} Isling(s) in the pods, most guardians bring {needed}: Go in / Not yet.");
            if (!ChoiceBoard.Ask(GoIn, () =>
                {
                    Debug.Log($"[TempleRules] {name}: Go in with {carried}.");
                    enter?.Invoke();
                }, NotYet, () => Debug.Log($"[TempleRules] {name}: not yet.")))
            {
                // No board to ask with: a warning must never become a wall.
                return true;
            }
            return false;
        }
    }
}
