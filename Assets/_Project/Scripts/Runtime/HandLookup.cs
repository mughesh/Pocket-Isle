using Oculus.Interaction.Input;
using UnityEngine;

namespace Diorama
{
    /// <summary>Finds the rig's raw tracked ISDK hands (not the SyntheticHand visuals that lock onto interactables).</summary>
    public static class HandLookup
    {
        public static Hand Find(Handedness handedness)
        {
            foreach (var hand in Object.FindObjectsByType<Hand>(FindObjectsSortMode.None))
                if (!(hand is SyntheticHand) && hand.Handedness == handedness) return hand;
            return null;
        }

        /// <summary>Re-finds the hand only when the cached one is gone.</summary>
        public static Hand Refresh(ref Hand cached, Handedness handedness)
        {
            if (cached == null) cached = Find(handedness);
            return cached;
        }

        public static bool TryGetJoint(IHand hand, HandJointId joint, out Vector3 position)
        {
            position = default;
            if (hand == null || !hand.IsTrackedDataValid || !hand.GetJointPose(joint, out var pose)) return false;
            position = pose.position;
            return true;
        }
    }
}
