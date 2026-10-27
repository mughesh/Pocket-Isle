using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 10f: cupping the village flame (pure, EditMode-tested). A palm "faces" the flame when it's within
    /// <c>radius</c> of it and its palm normal points within <c>maxAngle</c> of the flame. Two facing hands cup it; one
    /// hand also counts when it's curled round it (its middle fingertip within the radius of the flame too).
    /// </summary>
    public static class CupRules
    {
        public static bool Facing(Vector3 palm, Vector3 normal, Vector3 flame, float radius, float maxAngle)
        {
            var to = flame - palm;
            float d = to.magnitude;
            if (d > radius) return false;
            return d < 1e-4f || Vector3.Angle(normal, to) <= maxAngle;
        }

        /// <summary>One hand wrapped round the flame: facing it, fingers curled (middle tip near the palm) and reaching round it.</summary>
        public static bool Wraps(Vector3 palm, Vector3 normal, Vector3 middleTip, Vector3 flame, float radius, float maxAngle) =>
            Facing(palm, normal, flame, radius, maxAngle) && (middleTip - flame).magnitude <= radius && (middleTip - palm).magnitude < 0.085f;
    }

    /// <summary>
    /// Step 10f: how warm the cupped flame is (0..1). Both hands cupping fill it in <see cref="twoHands"/> s, one hand
    /// curled round it in <see cref="oneHand"/> s, a look + pinch-hold in <see cref="look"/> s; let go and it cools at
    /// <see cref="cool"/> per second. Full = rekindled (once).
    /// </summary>
    public class CupMeter
    {
        public float twoHands = 2f, oneHand = 3f, look = 3f, cool = 0.5f;
        public float Progress { get; private set; }
        public bool Done { get; private set; }

        /// <summary>Holding at all this frame.</summary>
        public bool Held { get; private set; }

        /// <summary>One frame: how many hands face it, whether one is wrapped round it, a look + pinch-hold. True on the frame it's full.</summary>
        public bool Feed(float dt, int facingHands, bool wrapped, bool lookHold)
        {
            if (Done) return false;
            float rate = facingHands >= 2 ? 1f / Mathf.Max(0.01f, twoHands)
                : wrapped ? 1f / Mathf.Max(0.01f, oneHand)
                : lookHold ? 1f / Mathf.Max(0.01f, look) : 0f;
            Held = rate > 0f;
            Progress = Held ? Mathf.Min(1f, Progress + rate * dt) : Mathf.Max(0f, Progress - cool * dt);
            if (Progress < 1f) return false;
            Done = true;
            return true;
        }

        public void Restore(bool done)
        {
            Done = done;
            Progress = done ? 1f : 0f;
            Held = false;
        }
    }
}
