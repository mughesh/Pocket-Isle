using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// The recorded clips (Data/Onboarding/GhostClips); a clip here replaces the placeholder of the same name.
    /// Nov 15: lives in its own file. It used to sit inside GhostHandClip.cs, so Unity had no script for it: the asset
    /// saved "m_Script: 0", the Editor still found the class by name, but a build loaded no library and every ghost
    /// fell back to the code-made placeholder clips.
    /// </summary>
    [CreateAssetMenu(menuName = "Pocket Isle/Ghost Clip Library", fileName = "GhostClips")]
    public class GhostClipLibrary : ScriptableObject
    {
        public List<GhostHandClip> clips = new List<GhostHandClip>();
        [Tooltip("Ghosts at a lesson target (shard, rim, pods, runes, cradle) show at this share of real hand size; the thumb demos above the island keep their lesson size.")]
        [Range(0.3f, 1.5f)] public float targetScale = 0.75f;

        /// <summary>The clips the lessons use, in recording order, with how to hold the hand for each.</summary>
        public static readonly (string name, GhostAnchor anchor, float seconds, string how)[] Wanted =
        {
            ("poke", GhostAnchor.Island, 2f, "Sit as in play. Reach in and poke a spot on the island top with your index finger, pull back."),
            ("grab_rim_slide", GhostAnchor.Island, 3f, "Reach to the island's near edge, grab the rocky rim, slide it ~10 cm right, let go, pull back."),
            ("grab_drop", GhostAnchor.Island, 3f, "Grab an imaginary pod floating above the island, set it down on the near rim, open the hand, pull back."),
            ("thumb_swipe_forward", GhostAnchor.Wrist, 2f, "Hand relaxed as for microgestures. Slide the thumb forward along the side of the index finger once."),
            ("thumb_tap", GhostAnchor.Wrist, 2f, "Same pose. Lift the thumb and tap it down on the side of the index finger once."),
            ("pinch", GhostAnchor.Wrist, 2f, "Hand relaxed, palm sideways. Pinch thumb and index together once and open again."),
            ("grab_toss", GhostAnchor.Island, 2.5f, "Grab a pod at the rim, toss it gently up onto the island, open the hand."),
            ("grab_flick", GhostAnchor.Island, 2.5f, "Grab a pod at the near-right corner, flick it toward the island's far middle, open the hand."),
            ("palm_hover", GhostAnchor.Island, 2.5f, "Palm down, hover it over a spot ~10 cm above the island, hold still."),
            ("hands_up", GhostAnchor.Island, 2.5f, "Raise one hand in front of you, palm away, fingers up, wiggle the fingers."),
            ("finger_flick", GhostAnchor.Island, 2f, "Point your index finger at a spot on the island, brush it quickly sideways ~8 cm (like flicking a sail), pull back."),
            ("palm_sweep", GhostAnchor.Island, 2.5f, "Open flat hand, palm down, ~5 cm above the island: sweep it fast from right to left across the island, lift away."),
            ("pinch_pull", GhostAnchor.Island, 2.5f, "Right hand: pinch a spot just right of the island centre, pull it ~8 cm out to the right, let go (shown mirrored on both hands)."),
            ("grab_palm_up", GhostAnchor.Island, 3f, "Evolve: pinch a pod at the near rim, lift it ~10 cm, open the hand and turn the palm up under it, hold still."),
        };

        public GhostHandClip Find(string name)
        {
            foreach (var c in clips)
                if (c != null && c.clipName == name) return c;
            return null;
        }
    }
}
