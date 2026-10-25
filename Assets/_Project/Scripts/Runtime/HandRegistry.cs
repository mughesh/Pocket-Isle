using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// The player's two tracked hands, as the rig's own ISDK interactors see them (step 8e). Resolved from the
    /// interactors under the camera rig, never from a scene-wide search for Hand components, so a copy of a hand
    /// elsewhere in the scene (ghost hands, recorder previews) can never be picked. Every system that reads joints asks
    /// here every frame (no per-system caches). If a resolved hand goes away or is disabled (rig rebuilt), it is
    /// resolved again from the rig, at most once a second while missing.
    /// Why (found Oct 25): the touch-grab interactor carries a HandSphereMap Hand (prefab data, reports Right, feeds
    /// nothing) that only turns on once hands are tracked; the old scene-wide lookup took it as the right hand for
    /// every system that looked after tracking started (shard, palm over the flame / door, lodge pinch).
    /// </summary>
    public static class HandRegistry
    {
        static readonly Hand[] Hands = new Hand[2];
        static readonly float[] NextTry = new float[2];
        static OVRCameraRig _rig;
        static bool _warnedNoRig;

        public static Hand Get(Handedness side)
        {
            int i = side == Handedness.Left ? 0 : 1;
            var hand = Hands[i];
            if (hand != null && hand.isActiveAndEnabled) return hand;
            if (Time.unscaledTime < NextTry[i]) return hand;
            NextTry[i] = Time.unscaledTime + 1f;
            var found = Resolve(side);
            if (found != hand && found != null) Debug.Log($"[HandRegistry] {side} hand: {PathOf(found)}{(hand != null ? " (re-resolved)" : "")}.");
            Hands[i] = found;
            return found;
        }

        /// <summary>The hand the rig's grab interactors of this side use (HandRefs unwrapped); active ones first.</summary>
        static Hand Resolve(Handedness side)
        {
            if (_rig == null) _rig = Object.FindFirstObjectByType<OVRCameraRig>();
            if (_rig == null)
            {
                if (!_warnedNoRig) Debug.LogError("[HandRegistry] No OVRCameraRig in the scene: no hands.");
                _warnedNoRig = true;
                return null;
            }
            Hand inactive = null;
            foreach (var interactor in _rig.GetComponentsInChildren<IHandGrabInteractor>(true))
            {
                var hand = Unwrap(interactor.Hand);
                // A hand built from prefab data (the touch-grab HandSphereMap) is a frozen reference pose, not the player.
                if (hand == null || hand.Handedness != side || hand.ModifyDataFromSource is FromHandPrefabDataSource) continue;
                if (hand.isActiveAndEnabled) return hand;
                if (inactive == null) inactive = hand;
            }
            return inactive;
        }

        static Hand Unwrap(IHand hand)
        {
            for (int depth = 0; depth < 4 && hand is HandRef r; depth++) hand = r.Hand;
            return hand as Hand;
        }

        public static string PathOf(Component c)
        {
            if (c == null) return "none";
            var path = c.name;
            for (var t = c.transform.parent; t != null; t = t.parent) path = t.name + "/" + path;
            return path;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Hands[0] = Hands[1] = null;
            NextTry[0] = NextTry[1] = 0f;
            _rig = null;
            _warnedNoRig = false;
        }
    }
}
