using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// The player's dominant hand (step 8e): whichever hand first touches the opening shard. Ghost hands and
    /// single-hand hints show this hand (mirrored clips). Right until the shard is touched. Saved in the save file.
    /// Step 9g: Settings > Dominant hand Left / Right overrides it (<see cref="GameSettings.DominantHand"/>); Auto keeps
    /// the first-touch rule. The save keeps the first-touch hand either way.
    /// </summary>
    public static class DominantHand
    {
        static bool _known, _left;

        public static bool Known => _known || GameSettings.DominantHand != GameSettings.Hand.Auto;

        public static bool Left => GameSettings.DominantHand switch
        {
            GameSettings.Hand.Left => true,
            GameSettings.Hand.Right => false,
            _ => _left,
        };

        /// <summary>The first touch decides; later calls do nothing.</summary>
        public static void FirstTouch(bool left)
        {
            if (_known) return;
            _known = true;
            _left = left;
            Debug.Log($"[DominantHand] {(left ? "Left" : "Right")} (first to touch the shard){(GameSettings.DominantHand != GameSettings.Hand.Auto ? $"; the settings say {GameSettings.DominantHand}" : "")}.");
        }

        /// <summary>For the save file: "left", "right", or empty while unknown (the first-touch hand, not the setting).</summary>
        public static string Saved => _known ? _left ? "left" : "right" : "";

        public static void Restore(string saved)
        {
            _known = saved == "left" || saved == "right";
            _left = saved == "left";
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Restore(null);
    }
}
