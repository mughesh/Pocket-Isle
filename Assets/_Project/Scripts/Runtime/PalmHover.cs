using Oculus.Interaction.Input;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// "Hold your palm over it": a tracked palm, facing down, within reach above a point. Used to enter a Hearth Shrine
    /// (palm over the flame) and to leave the lodge (palm over the door arch), so both feel the same.
    /// </summary>
    public class PalmHover
    {
        readonly Hand[] _hands = new Hand[2];

        /// <summary>A free hand's palm faces down within <paramref name="reach"/> above <paramref name="point"/> (world).</summary>
        public bool IsOver(Vector3 point, Vector3 up, float reach, float facing, IslandHandle guard)
        {
            for (int h = 0; h < 2; h++)
            {
                bool left = h == 0;
                var hand = HandLookup.Refresh(ref _hands[h], left ? Handedness.Left : Handedness.Right);
                if (guard != null && guard.IsHandBusy(left)) continue;
                if (!HandLookup.TryGetJoint(hand, HandJointId.HandPalm, out var palm) ||
                    !HandLookup.TryGetJoint(hand, HandJointId.HandWristRoot, out var wrist) ||
                    !HandLookup.TryGetJoint(hand, HandJointId.HandIndex1, out var index) ||
                    !HandLookup.TryGetJoint(hand, HandJointId.HandPinky1, out var pinky)) continue;
                Vector3 d = palm - point;
                float above = Vector3.Dot(d, up);
                if (above < -0.01f || above > reach * 1.5f || (d - up * above).magnitude > reach) continue;
                // Palm normal from the knuckles, mirrored per hand (works whatever the joint axes are).
                Vector3 n = Vector3.Cross(index - wrist, pinky - wrist).normalized;
                if (!left) n = -n;
                if (Vector3.Dot(n, -up) > facing) return true;
            }
            return false;
        }
    }
}
