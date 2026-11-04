using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 14i (Mugesh: boards turned while he touched them): a board faces the head once when it opens (yaw only) and
    /// then stays put. It faces the head again only when the head has been more than <see cref="reFaceAngle"/> round from
    /// it for <see cref="reFaceSeconds"/> with no hand within <see cref="handClear"/> of it. No continuous billboarding.
    /// Pure: the caller feeds the head's yaw (as seen from the board) and whether a hand is near.
    /// </summary>
    public class BoardFacing
    {
        public float reFaceAngle = 30f;
        public float reFaceSeconds = 1f;
        public float handClear = 0.15f;

        /// <summary>The yaw the board faces (degrees).</summary>
        public float Yaw { get; private set; }
        float _off;

        public void Open(float headYaw)
        {
            Yaw = headYaw;
            _off = 0f;
        }

        /// <summary>One frame; true when the board turns to face the head again.</summary>
        public bool Step(float headYaw, bool handNear, float dt)
        {
            bool off = Mathf.Abs(Mathf.DeltaAngle(Yaw, headYaw)) > reFaceAngle && !handNear;
            _off = off ? _off + dt : 0f;
            if (_off < reFaceSeconds) return false;
            Yaw = headYaw;
            _off = 0f;
            return true;
        }

        /// <summary>Distance (world m) from the nearest tracked fingertip or palm to a world point set (a board's corners: its rect).</summary>
        public static float NearestHand(Transform board, Vector2 min, Vector2 max)
        {
            float best = float.MaxValue;
            for (int h = 0; h < 2; h++)
            {
                var hand = HandRegistry.Get(h == 0 ? Oculus.Interaction.Input.Handedness.Left : Oculus.Interaction.Input.Handedness.Right);
                if (hand == null || !hand.IsTrackedDataValid) continue;
                foreach (var j in new[] { Oculus.Interaction.Input.HandJointId.HandIndexTip, Oculus.Interaction.Input.HandJointId.HandPalm })
                {
                    if (!HandLookup.TryGetJoint(hand, j, out var p)) continue;
                    var local = board.InverseTransformPoint(p);
                    var on = new Vector3(Mathf.Clamp(local.x, min.x, max.x), Mathf.Clamp(local.y, min.y, max.y), 0f);
                    best = Mathf.Min(best, (board.TransformPoint(on) - p).magnitude);
                }
            }
            return best;
        }
    }
}
