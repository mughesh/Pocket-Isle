using Oculus.Interaction.Input;
using UnityEngine;

namespace Diorama
{
    /// <summary>Joint reads on a hand that may be untracked or missing (the hands come from <see cref="HandRegistry"/>).</summary>
    public static class HandLookup
    {
        public static bool TryGetJoint(IHand hand, HandJointId joint, out Vector3 position)
        {
            position = default;
            if (hand == null || !hand.IsTrackedDataValid || !hand.GetJointPose(joint, out var pose)) return false;
            position = pose.position;
            return true;
        }

        public static bool TryGetJointPose(IHand hand, HandJointId joint, out Pose pose)
        {
            pose = default;
            return hand != null && hand.IsTrackedDataValid && hand.GetJointPose(joint, out pose);
        }
    }
}
