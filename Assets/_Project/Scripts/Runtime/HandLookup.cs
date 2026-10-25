using System.Collections.Generic;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Finds the rig's tracked ISDK hand per side (not the SyntheticHand visuals that lock onto interactables).
    /// The comprehensive rig chains several Hands per side (data source, last-known-good, OVRHands, the interactors'
    /// hand); step 8d: always the end of that chain (the one the interactors use), not whichever FindObjectsByType
    /// returned first (that differed between runs and between the two hands).
    /// </summary>
    public static class HandLookup
    {
        static readonly HashSet<Object> Feeding = new HashSet<Object>();

        public static Hand Find(Handedness handedness)
        {
            var hands = Object.FindObjectsByType<Hand>(FindObjectsSortMode.None);
            Feeding.Clear();
            // Synthetic hands (visuals locked onto interactables) feed from the interactors' hand; they don't count.
            foreach (var hand in hands)
                if (!(hand is SyntheticHand) && hand.ModifyDataFromSource is Object source && source != null) Feeding.Add(source);
            Hand any = null;
            foreach (var hand in hands)
            {
                if (hand is SyntheticHand || hand.Handedness != handedness) continue;
                if (!Feeding.Contains(hand)) return hand;
                if (any == null) any = hand;
            }
            return any;
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

        public static bool TryGetJointPose(IHand hand, HandJointId joint, out Pose pose)
        {
            pose = default;
            return hand != null && hand.IsTrackedDataValid && hand.GetJointPose(joint, out pose);
        }
    }
}
