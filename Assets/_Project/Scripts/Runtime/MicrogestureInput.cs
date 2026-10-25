using System;
using Oculus.Interaction.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Diorama
{
    public enum ThumbGesture { SwipeLeft, SwipeRight, SwipeForward, SwipeBack, Tap }

    public enum GestureSource { LeftHand, RightHand, Keyboard }

    /// <summary>
    /// Thumb microgestures from both hands (OVRPlugin hand tracking state, Meta XR Core SDK v207),
    /// plus keyboard arrows and Space as a fallback in the Editor and Meta XR Simulator.
    /// Reads OVRPlugin directly, so it works with any rig that has hand tracking enabled.
    /// A pinch can read as a thumb tap, so taps from a hand that is pinching (or pinched a moment ago) are dropped.
    /// Quick runs of taps from one hand are also reported as a <see cref="TapBurst"/> once the run ends (debug toggles).
    /// </summary>
    public class MicrogestureInput : MonoBehaviour
    {
        [Tooltip("Ignore the same gesture from the same hand repeated within this time (s).")]
        public float repeatGuard = 0.15f;

        [Tooltip("Gestures from a hand that is grabbing or touching the island rim are ignored.")]
        public IslandHandle grabGuard;

        [Tooltip("Taps from a hand are ignored while it pinches and for this long after a pinch starts (s).")]
        public float pinchTapGuard = 0.35f;

        [Tooltip("Taps closer together than this (s) count as one burst.")]
        public float burstGap = 0.45f;

        public event Action<ThumbGesture, GestureSource> Gesture;

        /// <summary>A run of quick taps from one hand ended: (tap count, hand).</summary>
        public event Action<int, GestureSource> TapBurst;

        OVRPlugin.HandTrackingState _state;
        readonly float[] _lastTime = { -1f, -1f };
        readonly OVRPlugin.MicrogestureType[] _lastType = new OVRPlugin.MicrogestureType[2];
        readonly Hand[] _hands = new Hand[2];
        readonly float[] _pinchStart = { -9f, -9f };
        readonly bool[] _pinching = new bool[2];
        readonly int[] _burstCount = new int[3];
        readonly float[] _burstLast = new float[3];

        void Update()
        {
            TrackPinches();
            EndBursts();
            if (OVRPlugin.initialized)
            {
                OVRPlugin.SendMicrogestureHint();
                Poll(OVRPlugin.Hand.HandLeft, 0, GestureSource.LeftHand);
                Poll(OVRPlugin.Hand.HandRight, 1, GestureSource.RightHand);
            }
            PollKeyboard();
        }

        void Poll(OVRPlugin.Hand hand, int index, GestureSource source)
        {
            if (!OVRPlugin.GetHandTrackingState(OVRPlugin.Step.Render, hand, ref _state)) return;
            var type = _state.Microgesture;
            if (!TryMap(type, out var gesture)) return;
            if (IsBlocked(source)) return;
            if (type == _lastType[index] && Time.unscaledTime - _lastTime[index] < repeatGuard) return;
            _lastType[index] = type;
            _lastTime[index] = Time.unscaledTime;
            if (gesture == ThumbGesture.Tap && (_pinching[index] || Time.unscaledTime - _pinchStart[index] < pinchTapGuard)) return;
            Raise(gesture, source);
        }

        void Raise(ThumbGesture gesture, GestureSource source)
        {
            if (gesture == ThumbGesture.Tap)
            {
                int i = (int)source;
                _burstCount[i]++;
                _burstLast[i] = Time.unscaledTime;
            }
            Gesture?.Invoke(gesture, source);
        }

        /// <summary>Test hook (the Claude bridge): a gesture as if the hand made it.</summary>
        public void DebugRaise(ThumbGesture gesture, GestureSource source) => Raise(gesture, source);

        void TrackPinches()
        {
            for (int h = 0; h < 2; h++)
            {
                var hand = HandLookup.Refresh(ref _hands[h], h == 0 ? Handedness.Left : Handedness.Right);
                bool pinching = hand != null && hand.IsTrackedDataValid && hand.GetIndexFingerIsPinching();
                if (pinching && !_pinching[h]) _pinchStart[h] = Time.unscaledTime;
                _pinching[h] = pinching;
            }
        }

        void EndBursts()
        {
            for (int i = 0; i < _burstCount.Length; i++)
            {
                if (_burstCount[i] == 0 || Time.unscaledTime - _burstLast[i] < burstGap) continue;
                int n = _burstCount[i];
                _burstCount[i] = 0;
                TapBurst?.Invoke(n, (GestureSource)i);
            }
        }

        /// <summary>True when this hand is busy with the island, so its thumb gestures don't count.</summary>
        public bool IsBlocked(GestureSource source) =>
            source != GestureSource.Keyboard && grabGuard != null && grabGuard.IsHandBusy(source == GestureSource.LeftHand);

        static bool TryMap(OVRPlugin.MicrogestureType type, out ThumbGesture gesture)
        {
            switch (type)
            {
                case OVRPlugin.MicrogestureType.SwipeLeft: gesture = ThumbGesture.SwipeLeft; return true;
                case OVRPlugin.MicrogestureType.SwipeRight: gesture = ThumbGesture.SwipeRight; return true;
                case OVRPlugin.MicrogestureType.SwipeForward: gesture = ThumbGesture.SwipeForward; return true;
                case OVRPlugin.MicrogestureType.SwipeBackward: gesture = ThumbGesture.SwipeBack; return true;
                case OVRPlugin.MicrogestureType.ThumbTap: gesture = ThumbGesture.Tap; return true;
                default: gesture = default; return false;
            }
        }

        void PollKeyboard()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.leftArrowKey.wasPressedThisFrame) Raise(ThumbGesture.SwipeLeft, GestureSource.Keyboard);
            if (kb.rightArrowKey.wasPressedThisFrame) Raise(ThumbGesture.SwipeRight, GestureSource.Keyboard);
            if (kb.upArrowKey.wasPressedThisFrame) Raise(ThumbGesture.SwipeForward, GestureSource.Keyboard);
            if (kb.downArrowKey.wasPressedThisFrame) Raise(ThumbGesture.SwipeBack, GestureSource.Keyboard);
            if (kb.spaceKey.wasPressedThisFrame) Raise(ThumbGesture.Tap, GestureSource.Keyboard);
        }
    }
}
