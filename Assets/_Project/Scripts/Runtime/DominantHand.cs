using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// The player's dominant hand (step 8e): whichever hand first touches the opening shard. Ghost hands and
    /// single-hand hints show this hand (mirrored clips). Right until the shard is touched. Saved in the save file.
    /// </summary>
    public static class DominantHand
    {
        public static bool Known { get; private set; }
        public static bool Left { get; private set; }

        /// <summary>The first touch decides; later calls do nothing.</summary>
        public static void FirstTouch(bool left)
        {
            if (Known) return;
            Known = true;
            Left = left;
            Debug.Log($"[DominantHand] {(left ? "Left" : "Right")} (first to touch the shard).");
        }

        /// <summary>For the save file: "left", "right", or empty while unknown.</summary>
        public static string Saved => Known ? Left ? "left" : "right" : "";

        public static void Restore(string saved)
        {
            Known = saved == "left" || saved == "right";
            Left = saved == "left";
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Restore(null);
    }
}
