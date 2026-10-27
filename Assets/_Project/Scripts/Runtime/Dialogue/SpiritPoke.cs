using System;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 12a: poke the Lantern Spirit with a fingertip, or look at it and pinch: it says the current objective again in
    /// full (<see cref="Dialogue.RepeatObjective"/>), or with nothing pinned a short idle line, and raises
    /// <see cref="Poked"/> (the Spirit's trail shows the way). The Spirit flinches so the touch reads.
    /// </summary>
    public class SpiritPoke : MonoBehaviour
    {
        public LanternSpirit spirit;
        [Tooltip("Guards hands that are grabbing the island.")]
        public IslandHandle grabGuard;
        [Tooltip("Fingertip reach round the Spirit (m).")]
        public float pokeRadius = 0.025f;
        [Tooltip("Gaze reach round the Spirit for look + pinch (m, at least the gaze cone).")]
        public float gazeRadius = 0.03f;
        [Tooltip("Said when the Spirit is poked with no objective active.")]
        public string idleLine = "spirit.poke.idle";
        [Tooltip("Seconds between pokes.")]
        public float cooldown = 1.2f;

        /// <summary>The Spirit was poked or looked at + pinched (true = an objective was repeated).</summary>
        public static event Action<bool> Poked;

        readonly bool[] _wasNear = new bool[2];
        readonly bool[] _wasPinching = { true, true };
        float _last = -99f;

        void Update()
        {
            if (spirit == null || TitleScreen.Showing || Opening.InProgress || GameMenu.IsOpen) return;
            Vector3 at = spirit.transform.position;
            for (int h = 0; h < 2; h++)
            {
                bool left = h == 0;
                var hand = HandRegistry.Get(left ? Handedness.Left : Handedness.Right);
                bool tracked = hand != null && hand.IsTrackedDataValid;
                bool busy = grabGuard != null && grabGuard.IsHandBusy(left);
                bool near = tracked && !busy && HandLookup.TryGetJoint(hand, HandJointId.HandIndexTip, out var tip) && (tip - at).magnitude < pokeRadius;
                if (near && !_wasNear[h]) Poke("poke", at);
                _wasNear[h] = near;
                bool pinch = tracked && hand.GetIndexFingerIsPinching();
                if (pinch && !_wasPinching[h] && !busy && Looked(at)) Poke("look + pinch", at);
                _wasPinching[h] = pinch;
            }
        }

        bool Looked(Vector3 at)
        {
            if (!HeadGaze.TryGetRay(out var ray)) return false;
            return HeadGaze.InCone(ray, at, Mathf.Max(gazeRadius, HeadGaze.MinRadius), out _);
        }

        void Poke(string how, Vector3 from)
        {
            if (Time.time - _last < cooldown) return;
            _last = Time.time;
            bool objective = Dialogue.RepeatObjective();
            if (!objective && !string.IsNullOrEmpty(idleLine)) Dialogue.Repeat(idleLine);
            if (spirit != null) spirit.Flinch(from - spirit.transform.forward * 0.02f, 0.01f, 0.4f);
            Debug.Log($"[SpiritPoke] {how}: {(objective ? $"repeating {Dialogue.CurrentObjective}" : "no objective, idle line")}.");
            Poked?.Invoke(objective);
        }

        /// <summary>Debug (bridge): a poke as if by a fingertip.</summary>
        public void DebugPoke() => Poke("bridge", spirit != null ? spirit.transform.position : transform.position);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Poked = null;
    }
}
