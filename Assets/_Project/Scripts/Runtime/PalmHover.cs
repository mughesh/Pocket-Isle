using System;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// "Hold your palm over it": a tracked palm, facing down, a little above a point. Used to enter a Hearth Shrine
    /// (palm over the flame) and to leave the lodge (palm over the door arch), so both feel the same.
    /// Step 8d: the palm's facing comes from the palm joint's own orientation (OpenXR hand skeleton, ISDK v207: the
    /// palmar side is the joint's -Y for both hands), not a cross product of the knuckles; the zone is a cylinder above
    /// the point, scaled with the island; only a hand that is holding the island is ignored (near the rim is fine).
    /// </summary>
    [Serializable]
    public class PalmHover
    {
        [Tooltip("Palm-down · palm normal must be above this.")]
        [Range(0f, 1f)] public float facing = 0.5f;
        [Tooltip("Palm height above the point (m at the placed island size): min, max.")]
        public Vector2 height = new Vector2(0.03f, 0.15f);
        [Tooltip("Palm within this horizontal distance of the point (m at the placed island size).")]
        public float radius = 0.06f;

        /// <summary>
        /// A free hand's palm faces down over <paramref name="point"/> (world; <paramref name="up"/> = the island's up).
        /// <paramref name="label"/> names the dev readout line (null = none).
        /// </summary>
        public bool IsOver(Vector3 point, Vector3 up, IslandHandle guard, string label)
        {
            float scale = guard != null ? guard.ScaleFactor : 1f;
            float r = radius * scale, lo = height.x * scale, hi = height.y * scale;
            bool readout = label != null && DevReadout.Active;
            if (readout)
            {
                DevReadout.Circle(point + up * lo, up, r, new Color(1f, 0.6f, 0.2f, 0.6f));
                DevReadout.Circle(point + up * hi, up, r, new Color(1f, 0.6f, 0.2f, 0.6f));
            }
            bool over = false;
            for (int h = 0; h < 2; h++)
            {
                bool left = h == 0;
                var hand = HandRegistry.Get(left ? Handedness.Left : Handedness.Right);
                string side = left ? "L" : "R";
                if (!HandLookup.TryGetJointPose(hand, HandJointId.HandPalm, out var palm))
                {
                    if (readout) DevReadout.Text($"{label}{h}", $"{label} {side}: no hand");
                    continue;
                }
                Vector3 d = palm.position - point;
                float above = Vector3.Dot(d, up);
                float across = (d - up * above).magnitude;
                Vector3 normal = palm.rotation * Vector3.down;
                float dot = Vector3.Dot(normal, -up);
                bool grabbing = guard != null && guard.IsHandGrabbing(left);
                bool ok = !grabbing && above >= lo && above <= hi && across <= r && dot > facing;
                over |= ok;
                if (!readout) continue;
                DevReadout.Line(palm.position, palm.position + normal * 0.05f, ok ? Color.green : dot > facing ? Color.yellow : Color.red);
                // The step 8c normal (cross of the knuckles, mirrored per hand), to compare.
                string old = "-";
                if (HandLookup.TryGetJoint(hand, HandJointId.HandWristRoot, out var wrist) && HandLookup.TryGetJoint(hand, HandJointId.HandIndex1, out var index) &&
                    HandLookup.TryGetJoint(hand, HandJointId.HandPinky1, out var pinky))
                {
                    Vector3 n = Vector3.Cross(index - wrist, pinky - wrist).normalized;
                    if (!left) n = -n;
                    old = Vector3.Dot(n, -up).ToString("0.00");
                }
                DevReadout.Text($"{label}{h}", $"{label} {side}: {(ok ? "OVER" : "-")} facing {dot:0.00} (>{facing:0.0}, 8c cross {old}), " +
                                               $"height {above * 100f:0.0} cm ({lo * 100f:0}-{hi * 100f:0}), off {across * 100f:0.0} cm (<{r * 100f:0}){(grabbing ? ", GRABBING" : "")}");
            }
            return over;
        }
    }
}
