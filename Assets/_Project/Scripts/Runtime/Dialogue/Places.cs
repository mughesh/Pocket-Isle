using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 14k: where the player is now, for the Spirit's line contexts (<see cref="DialogueTrigger.context"/>): the place
    /// names a context expression may use, read through <see cref="GameConditions.Check(string, System.Func{string, bool?})"/>
    /// as its extra values. hub (the Still Isle outside), realm (any realm outside), cinderwood / galewick / lilymere (that
    /// realm outside), lodge (the Hearth Lodge), hall (a Keeper's hall: Forge Hall, Tower Hall, Lake Temple Hall), outside,
    /// inside. "battle" and the other game conditions come from <see cref="GameConditions"/>.
    /// </summary>
    public static class Places
    {
        public const string HubMap = "WorldMap_StillIsle", LodgeMap = "Lodge";

        /// <summary>Tests: the place to report instead of the scene's ("hub", "lodge", "hall", "cinderwood", ...); null = the scene.</summary>
        public static string Override;

        /// <summary>The place now, one word (hub, cinderwood, galewick, lilymere, lodge, hall; "outside" without a scene swap).</summary>
        public static string Now
        {
            get
            {
                if (!string.IsNullOrEmpty(Override)) return Override;
                var swap = SceneSwap.Current;
                if (swap == null) return "outside";
                if (swap.Inside) return swap.Interior != null && swap.Interior.name == LodgeMap ? "lodge" : "hall";
                var world = swap.island != null ? swap.island.world : null;
                if (world == null) return "outside";
                if (world.name == HubMap) return "hub";
                string n = world.name.StartsWith("WorldMap_") ? world.name.Substring(9) : world.name;
                return n.ToLowerInvariant();
            }
        }

        static bool Inside(string now) => now == "lodge" || now == "hall";

        /// <summary>A place name's value now, or null when the name isn't a place (GameConditions then looks it up).</summary>
        public static bool? Value(string name)
        {
            switch (name)
            {
                case "any": return true;
                case "outside": return !Inside(Now);
                case "inside": return Inside(Now);
                case "hub": return Now == "hub";
                case "lodge": return Now == "lodge";
                case "hall": return Now == "hall";
                case "realm":
                {
                    string now = Now;
                    return !Inside(now) && now != "hub" && now != "outside";
                }
                case "cinderwood":
                case "galewick":
                case "lilymere":
                    return Now == name;
                default: return null;
            }
        }

        /// <summary>The context holds here and now (empty = anywhere).</summary>
        public static bool Holds(string context) => string.IsNullOrWhiteSpace(context) || GameConditions.Check(context, Value);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Override = null;
    }
}
