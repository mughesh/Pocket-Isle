using System;
using System.Collections.Generic;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Diorama
{
    /// <summary>What a recorded clip's root motion is relative to.</summary>
    public enum GhostAnchor
    {
        /// <summary>The player's view toward the island (grabs, pokes, throws): x right, y up, z away from the player.</summary>
        Island,
        /// <summary>The wrist's own pose at the start (thumb gestures): only the fingers move; the ghost shows it at a set angle.</summary>
        Wrist,
    }

    /// <summary>One sample: the hand root's pose in the clip frame and every joint's local pose (ISDK OpenXR joint set).</summary>
    [Serializable]
    public class GhostFrame
    {
        public Vector3 rootPos;
        public Quaternion rootRot = Quaternion.identity;
        public Quaternion[] rot;
        public Vector3[] pos;
    }

    /// <summary>
    /// A hand gesture for the ghost hands (step 8b): joint poses at a fixed rate, recorded from real hands
    /// (<see cref="GhostClipRecorder"/>) or made in code (<see cref="GhostPlaceholders"/>). The ghost puts the clip's
    /// <see cref="keyJoint"/> at <see cref="keyTime"/> on the spot it demonstrates (the shard, a pod, the rim), so a
    /// recording works wherever it is shown. Recorded clips replace placeholders by <see cref="clipName"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Pocket Isle/Ghost Hand Clip", fileName = "GhostClip")]
    public class GhostHandClip : ScriptableObject
    {
        public string clipName;
        [Tooltip("Recorded with the left hand (the ghost mirrors it for the other hand).")]
        public bool leftHand;
        public GhostAnchor anchor;
        public float fps = 30f;
        public bool loop = true;
        [Tooltip("Seconds cut from the start / end of the recording.")]
        [Min(0f)] public float trimStart, trimEnd;
        [Tooltip("The joint that lands on the demonstrated spot (index tip for a poke, palm for grabs).")]
        public HandJointId keyJoint = HandJointId.HandPalm;
        [Tooltip("Pinch grabs (step 9e): the point between the thumb and index tips lands on the spot instead of the key joint.")]
        public bool keyPinch;
        [Tooltip("When it lands there (0..1 of the trimmed clip).")]
        [Range(0f, 1f)] public float keyTime = 0.5f;
        [Tooltip("Shift from the spot (m, x right / y up / z away from the player): beside the runes rather than over them.")]
        public Vector3 placeOffset;
        [Tooltip("A gaze dot shows on the spot until this point of the loop (0..1, 0 = none): look first, then the gesture.")]
        [Range(0f, 1f)] public float gazeDotUntil;
        [Tooltip("Size of this clip's ghost on top of the lesson's size (1 = as recorded).")]
        [Range(0.3f, 1.5f)] public float size = 1f;
        [Tooltip("How far the hand travels, around the key moment (1 = as recorded, 0.3 = a short reach). Fingers are untouched.")]
        [Range(0.1f, 1f)] public float travel = 1f;
        [Tooltip("Playback speed (1 = as recorded).")]
        [Range(0.3f, 2f)] public float speed = 1f;
        [Tooltip("Hold the last pose this long before looping (s).")]
        [Min(0f)] public float holdEnd = 0.3f;
        public List<GhostFrame> frames = new List<GhostFrame>();

        public float StartTime => Mathf.Clamp(trimStart, 0f, RawDuration);
        public float RawDuration => frames.Count > 1 ? (frames.Count - 1) / Mathf.Max(1f, fps) : 0f;
        /// <summary>Playable length (s) after trimming.</summary>
        public float Duration => Mathf.Max(0.05f, RawDuration - trimStart - trimEnd);

        /// <summary>
        /// The pose at <paramref name="t"/> seconds into the trimmed clip, interpolated between samples; mirrored to the
        /// other hand when <paramref name="mirror"/> (across the clip frame's x = 0 plane).
        /// </summary>
        public void Sample(float t, bool mirror, out Vector3 rootPos, out Quaternion rootRot, Quaternion[] rot, Vector3[] pos)
        {
            rootPos = Vector3.zero;
            rootRot = Quaternion.identity;
            if (frames.Count == 0) return;
            float f = (StartTime + Mathf.Clamp(t, 0f, Duration)) * Mathf.Max(1f, fps);
            int a = Mathf.Clamp(Mathf.FloorToInt(f), 0, frames.Count - 1);
            int b = Mathf.Min(a + 1, frames.Count - 1);
            float k = Mathf.Clamp01(f - a);
            var fa = frames[a];
            var fb = frames[b];
            rootPos = Vector3.Lerp(fa.rootPos, fb.rootPos, k);
            rootRot = Quaternion.Slerp(fa.rootRot, fb.rootRot, k);
            int n = Mathf.Min(rot.Length, fa.rot != null ? fa.rot.Length : 0);
            for (int i = 0; i < n; i++)
            {
                rot[i] = Quaternion.Slerp(fa.rot[i], fb.rot[i], k);
                pos[i] = Vector3.Lerp(fa.pos[i], fb.pos[i], k);
            }
            if (!mirror) return;
            rootPos = Mirror(rootPos);
            rootRot = Mirror(rootRot);
            for (int i = 0; i < n; i++)
            {
                rot[i] = Mirror(rot[i]);
                pos[i] = Mirror(pos[i]);
            }
        }

        /// <summary>
        /// Left and right OpenXR hands are mirror images with the same distal (z) and dorsal (y) axes, so a pose mirrors
        /// across x: position (-x, y, z), rotation (x, -y, -z, w). Works for root poses and parent-relative joint poses.
        /// </summary>
        public static Vector3 Mirror(Vector3 p) => new Vector3(-p.x, p.y, p.z);

        public static Quaternion Mirror(Quaternion q) => new Quaternion(q.x, -q.y, -q.z, q.w);
    }

    /// <summary>The recorded clips (Data/Onboarding/GhostClips); a clip here replaces the placeholder of the same name.</summary>
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
        };

        public GhostHandClip Find(string name)
        {
            foreach (var c in clips)
                if (c != null && c.clipName == name) return c;
            return null;
        }
    }
}
