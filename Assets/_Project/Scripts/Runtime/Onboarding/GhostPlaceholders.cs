using System.Collections.Generic;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Procedural stand-ins for the ghost clips until Mugesh records them (step 8b): keyframed finger curls and hand
    /// motion on top of the hand mesh's own rest pose, right hand (the ghost mirrors for the left). Curls turn each finger
    /// joint about its local x (OpenXR joints: z toward the fingertip, y the back of the hand, so +x flexes toward the
    /// palm on both hands). Hand directions are in the clip frame: x right, y up, z away from the player.
    /// </summary>
    public static class GhostPlaceholders
    {
        /// <summary>One key: where the hand is, which way the fingers and the back of the hand point, how curled each finger is.</summary>
        struct Key
        {
            public float t;
            public Vector3 pos, fingers, dorsal;
            public float thumb, index, middle, ring, pinky;
            /// <summary>Thumb out, away from the index (+), or in along it (-).</summary>
            public float swing;
            /// <summary>Thumb tip slid toward the index tip (thumb swipe).</summary>
            public float slide;
        }

        static readonly int[][] Fingers =
        {
            new[] { (int)HandJointId.HandIndex1, (int)HandJointId.HandIndex2, (int)HandJointId.HandIndex3 },
            new[] { (int)HandJointId.HandMiddle1, (int)HandJointId.HandMiddle2, (int)HandJointId.HandMiddle3 },
            new[] { (int)HandJointId.HandRing1, (int)HandJointId.HandRing2, (int)HandJointId.HandRing3 },
            new[] { (int)HandJointId.HandPinky1, (int)HandJointId.HandPinky2, (int)HandJointId.HandPinky3 },
        };
        static readonly float[] FingerAngles = { 72f, 95f, 55f };

        // Tuning (degrees per unit), checked against shots of the ISDK hand.
        public static float ThumbCurlBase = 22f, ThumbCurl2 = 30f, ThumbCurl3 = 45f, ThumbSwing = 28f, ThumbSlide = 26f;

        public static readonly string[] Names =
        {
            "poke", "grab_rim_slide", "grab_drop", "thumb_swipe_forward", "thumb_tap", "pinch", "grab_toss", "grab_flick", "palm_hover", "hands_up",
            "finger_flick", "palm_sweep",
        };

        /// <summary>Builds a placeholder clip from the right hand's rest pose (joint-local), or null for an unknown name.</summary>
        public static GhostHandClip Make(string name, Quaternion[] bindRot, Vector3[] bindPos)
        {
            var keys = Keys(name, out var anchor, out float seconds, out var keyJoint, out float keyTime, out var offset, out float gaze);
            if (keys == null) return null;
            var clip = ScriptableObject.CreateInstance<GhostHandClip>();
            clip.hideFlags = HideFlags.DontSave;
            clip.name = clip.clipName = name;
            clip.anchor = anchor;
            clip.fps = 30f;
            clip.keyJoint = keyJoint;
            clip.keyTime = keyTime;
            clip.placeOffset = offset;
            clip.gazeDotUntil = gaze;
            clip.holdEnd = 0.25f;
            int n = Mathf.CeilToInt(seconds * clip.fps) + 1;
            for (int f = 0; f < n; f++)
            {
                float u = f / (float)(n - 1);
                var k = Evaluate(keys, u);
                var frame = new GhostFrame
                {
                    rootPos = k.pos,
                    rootRot = Quaternion.LookRotation(k.fingers, k.dorsal),
                    rot = (Quaternion[])bindRot.Clone(),
                    pos = (Vector3[])bindPos.Clone(),
                };
                Pose(frame, k);
                clip.frames.Add(frame);
            }
            return clip;
        }

        static void Pose(GhostFrame f, Key k)
        {
            float[] curls = { k.index, k.middle, k.ring, k.pinky };
            for (int i = 0; i < Fingers.Length; i++)
                for (int j = 0; j < 3; j++)
                {
                    int id = Fingers[i][j];
                    f.rot[id] = f.rot[id] * Quaternion.AngleAxis(curls[i] * FingerAngles[j], Vector3.right);
                }
            int t1 = (int)HandJointId.HandThumb1, t2 = (int)HandJointId.HandThumb2, t3 = (int)HandJointId.HandThumb3;
            f.rot[t1] = f.rot[t1] * Quaternion.AngleAxis(k.swing * ThumbSwing, Vector3.up) *
                        Quaternion.AngleAxis(k.thumb * ThumbCurlBase - k.slide * ThumbSlide, Vector3.right);
            f.rot[t2] = f.rot[t2] * Quaternion.AngleAxis(k.thumb * ThumbCurl2 - k.slide * ThumbSlide * 0.5f, Vector3.right);
            f.rot[t3] = f.rot[t3] * Quaternion.AngleAxis(k.thumb * ThumbCurl3 - k.slide * ThumbSlide * 0.6f, Vector3.right);
        }

        static Key Evaluate(List<Key> keys, float u)
        {
            if (u <= keys[0].t) return keys[0];
            for (int i = 1; i < keys.Count; i++)
            {
                if (u > keys[i].t) continue;
                var a = keys[i - 1];
                var b = keys[i];
                float k = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a.t, b.t, u));
                return new Key
                {
                    t = u,
                    pos = Vector3.Lerp(a.pos, b.pos, k),
                    fingers = Vector3.Slerp(a.fingers.normalized, b.fingers.normalized, k),
                    dorsal = Vector3.Slerp(a.dorsal.normalized, b.dorsal.normalized, k),
                    thumb = Mathf.Lerp(a.thumb, b.thumb, k),
                    index = Mathf.Lerp(a.index, b.index, k),
                    middle = Mathf.Lerp(a.middle, b.middle, k),
                    ring = Mathf.Lerp(a.ring, b.ring, k),
                    pinky = Mathf.Lerp(a.pinky, b.pinky, k),
                    swing = Mathf.Lerp(a.swing, b.swing, k),
                    slide = Mathf.Lerp(a.slide, b.slide, k),
                };
            }
            return keys[keys.Count - 1];
        }

        // ---- the clips ----

        static Key Open(float t, Vector3 pos, Vector3 fingers, Vector3 dorsal, float curl = 0.12f) => new Key
        {
            t = t, pos = pos, fingers = fingers, dorsal = dorsal,
            thumb = curl, index = curl, middle = curl * 1.2f, ring = curl * 1.4f, pinky = curl * 1.6f, swing = -0.2f,
        };

        static Key Closed(float t, Vector3 pos, Vector3 fingers, Vector3 dorsal) => new Key
        {
            t = t, pos = pos, fingers = fingers, dorsal = dorsal,
            thumb = 0.75f, index = 0.72f, middle = 0.78f, ring = 0.82f, pinky = 0.85f, swing = 0.45f,
        };

        /// <summary>
        /// The relaxed microgesture hand, wrist frame: the thumb rests bent against the side of the index; sliding forward
        /// straightens it along the index toward the tip; lifting raises it off the index (a tap comes back down).
        /// </summary>
        static Key Relaxed(float t, float slide = 0f, float lift = 0f) => new Key
        {
            t = t, fingers = Vector3.forward, dorsal = Vector3.up,
            thumb = 0.5f - slide * 0.35f - lift * 0.25f, index = 0.42f, middle = 0.66f, ring = 0.74f, pinky = 0.8f,
            swing = -0.1f - slide * 0.6f + lift * 0.9f,
        };

        static List<Key> Keys(string name, out GhostAnchor anchor, out float seconds, out HandJointId keyJoint, out float keyTime, out Vector3 offset, out float gaze)
        {
            anchor = GhostAnchor.Island;
            keyJoint = HandJointId.HandPalm;
            keyTime = 0.3f;
            offset = Vector3.zero;
            gaze = 0f;
            seconds = 2f;
            Vector3 down = new Vector3(0f, -0.45f, 1f), downBack = new Vector3(0f, 1f, 0.45f);
            switch (name)
            {
                case "poke":
                {
                    seconds = 1.6f;
                    keyJoint = HandJointId.HandIndexTip;
                    keyTime = 0.45f;
                    Key Point(float t, Vector3 p) => new Key
                    {
                        t = t, pos = p, fingers = down, dorsal = downBack,
                        thumb = 0.55f, index = 0.04f, middle = 0.85f, ring = 0.9f, pinky = 0.92f, swing = 0.5f,
                    };
                    return new List<Key>
                    {
                        Point(0f, new Vector3(0f, 0.03f, -0.07f)), Point(0.45f, Vector3.zero), Point(0.62f, Vector3.zero),
                        Point(1f, new Vector3(0f, 0.03f, -0.07f)),
                    };
                }
                case "grab_rim_slide":
                {
                    seconds = 2.6f;
                    keyTime = 0.3f;
                    Vector3 f = new Vector3(0f, -0.55f, 1f), d = new Vector3(0f, 1f, 0.55f);
                    return new List<Key>
                    {
                        Open(0f, new Vector3(0f, 0.03f, -0.08f), f, d), Open(0.25f, Vector3.zero, f, d), Closed(0.36f, Vector3.zero, f, d),
                        Closed(0.7f, new Vector3(0.09f, 0f, 0f), f, d), Open(0.8f, new Vector3(0.09f, 0.005f, 0f), f, d),
                        Open(1f, new Vector3(0.09f, 0.03f, -0.08f), f, d),
                    };
                }
                case "grab_drop":
                {
                    seconds = 2.8f;
                    keyTime = 0.32f;
                    gaze = 0.2f;
                    Vector3 f = new Vector3(0f, -0.6f, 1f), d = new Vector3(0f, 1f, 0.6f);
                    Vector3 rim = new Vector3(0f, -0.07f, -0.06f);
                    return new List<Key>
                    {
                        Open(0f, new Vector3(0f, 0.05f, -0.1f), f, d), Open(0.2f, new Vector3(0f, 0.05f, -0.1f), f, d), Open(0.32f, Vector3.zero, f, d),
                        Closed(0.4f, Vector3.zero, f, d), Closed(0.72f, rim, f, d), Open(0.8f, rim, f, d),
                        Open(1f, rim + new Vector3(0f, 0.04f, -0.07f), f, d),
                    };
                }
                case "thumb_swipe_forward":
                    anchor = GhostAnchor.Wrist;
                    seconds = 1.4f;
                    keyTime = 0.5f;
                    return new List<Key> { Relaxed(0f), Relaxed(0.18f), Relaxed(0.5f, 1f), Relaxed(0.62f, 1f), Relaxed(0.75f, 0.5f, 0.4f), Relaxed(0.9f), Relaxed(1f) };
                case "thumb_tap":
                    anchor = GhostAnchor.Wrist;
                    seconds = 1.2f;
                    keyTime = 0.5f;
                    return new List<Key> { Relaxed(0f), Relaxed(0.15f), Relaxed(0.38f, 0f, 1f), Relaxed(0.5f), Relaxed(1f) };
                case "pinch":
                {
                    anchor = GhostAnchor.Wrist;
                    seconds = 1.6f;
                    keyTime = 0.5f;
                    gaze = 0.3f;
                    offset = new Vector3(0.07f, -0.05f, -0.05f);
                    Key P(float t, float k) => new Key
                    {
                        t = t, fingers = Vector3.forward, dorsal = Vector3.up,
                        thumb = Mathf.Lerp(0.1f, 0.6f, k), index = Mathf.Lerp(0.22f, 0.55f, k), middle = 0.45f, ring = 0.55f, pinky = 0.6f,
                        swing = Mathf.Lerp(0.6f, -0.2f, k),
                    };
                    return new List<Key> { P(0f, 0f), P(0.32f, 0f), P(0.5f, 1f), P(0.68f, 1f), P(0.85f, 0f), P(1f, 0f) };
                }
                case "grab_toss":
                {
                    seconds = 2.2f;
                    keyTime = 0.25f;
                    Vector3 f = new Vector3(0f, -0.4f, 1f), d = new Vector3(0f, 1f, 0.4f);
                    return new List<Key>
                    {
                        Open(0f, new Vector3(0f, 0.04f, -0.06f), f, d), Open(0.25f, Vector3.zero, f, d), Closed(0.33f, Vector3.zero, f, d),
                        Closed(0.55f, new Vector3(0f, 0.06f, 0.05f), new Vector3(0f, 0.1f, 1f), Vector3.up),
                        Open(0.62f, new Vector3(0f, 0.08f, 0.07f), new Vector3(0f, 0.1f, 1f), Vector3.up),
                        Open(1f, new Vector3(0f, 0.05f, -0.03f), f, d),
                    };
                }
                case "grab_flick":
                {
                    seconds = 2f;
                    keyTime = 0.25f;
                    Vector3 f = new Vector3(0f, -0.4f, 1f), d = new Vector3(0f, 1f, 0.4f);
                    Vector3 back = new Vector3(0f, 0.6f, 1f), backD = new Vector3(0f, 1f, -0.6f);
                    return new List<Key>
                    {
                        Open(0f, new Vector3(0f, 0.04f, -0.06f), f, d), Open(0.25f, Vector3.zero, f, d), Closed(0.33f, Vector3.zero, f, d),
                        Closed(0.45f, new Vector3(0f, 0.025f, -0.035f), back, backD),
                        Closed(0.56f, new Vector3(-0.02f, 0.05f, 0.07f), f, d), Open(0.6f, new Vector3(-0.025f, 0.055f, 0.08f), f, d),
                        Open(1f, new Vector3(0f, 0.05f, -0.03f), f, d),
                    };
                }
                case "palm_hover":
                {
                    seconds = 2f;
                    keyTime = 0.5f;
                    Vector3 f = Vector3.forward, d = Vector3.up;
                    return new List<Key>
                    {
                        Open(0f, new Vector3(0f, 0.012f, 0f), f, d, 0.15f), Open(0.5f, new Vector3(0f, -0.008f, 0f), f, d, 0.15f),
                        Open(1f, new Vector3(0f, 0.012f, 0f), f, d, 0.15f),
                    };
                }
                case "finger_flick":
                {
                    // Oct 27: the mill's sails. A pointing finger brushes quickly sideways across the top of the sails
                    // (the turn's direction), then comes back slowly, pulled toward the player so it doesn't read as a flick back.
                    seconds = 1.6f;
                    keyJoint = HandJointId.HandIndexTip;
                    keyTime = 0.3f;
                    // Rolled on its side (the back of the hand to the right, the palm leading the brush to the left) so the
                    // pointing finger shows from the seat.
                    Vector3 f = new Vector3(0f, -0.3f, 1f), d = new Vector3(1f, 0.25f, 0.1f);
                    Key Point(float t, Vector3 p) => new Key
                    {
                        t = t, pos = p, fingers = f, dorsal = d,
                        thumb = 0.55f, index = 0.04f, middle = 0.85f, ring = 0.9f, pinky = 0.92f, swing = 0.5f,
                    };
                    return new List<Key>
                    {
                        Point(0f, new Vector3(0.045f, 0.03f, -0.02f)), Point(0.3f, new Vector3(0.045f, 0.03f, 0f)),
                        Point(0.42f, new Vector3(-0.045f, 0.03f, 0f)), Point(0.55f, new Vector3(-0.05f, 0.03f, -0.01f)),
                        Point(0.8f, new Vector3(0f, 0.04f, -0.06f)), Point(1f, new Vector3(0.045f, 0.03f, -0.02f)),
                    };
                }
                case "palm_sweep":
                {
                    // Oct 27: the gust. A flat open hand, palm down, sweeps fast across over the island, then lifts back.
                    seconds = 2.2f;
                    keyTime = 0.35f;
                    Vector3 f = Vector3.forward, d = Vector3.up;
                    return new List<Key>
                    {
                        Open(0f, new Vector3(0.14f, 0.07f, 0f), f, d, 0.08f), Open(0.22f, new Vector3(0.14f, 0.04f, 0f), f, d, 0.08f),
                        Open(0.45f, new Vector3(-0.14f, 0.04f, 0f), f, d, 0.08f), Open(0.55f, new Vector3(-0.16f, 0.06f, 0f), f, d, 0.08f),
                        Open(1f, new Vector3(0.14f, 0.07f, 0f), f, d, 0.08f),
                    };
                }
                case "hands_up":
                {
                    seconds = 1.6f;
                    keyTime = 0.5f;
                    Vector3 f = new Vector3(0f, 1f, 0.12f), d = new Vector3(0f, 0.12f, -1f);
                    Key W(float t, float a, float b) => new Key
                    {
                        t = t, pos = Vector3.zero, fingers = f, dorsal = d,
                        thumb = 0.05f, index = a, middle = b, ring = a, pinky = b, swing = -0.4f,
                    };
                    return new List<Key> { W(0f, 0.04f, 0.04f), W(0.25f, 0.32f, 0.04f), W(0.5f, 0.04f, 0.32f), W(0.75f, 0.32f, 0.04f), W(1f, 0.04f, 0.04f) };
                }
            }
            return null;
        }
    }
}
