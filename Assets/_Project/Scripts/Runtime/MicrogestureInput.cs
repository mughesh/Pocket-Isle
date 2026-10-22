using System;
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
    /// </summary>
    public class MicrogestureInput : MonoBehaviour
    {
        [Tooltip("Ignore the same gesture from the same hand repeated within this time (s).")]
        public float repeatGuard = 0.15f;

        [Tooltip("Gestures from a hand that is grabbing or touching the island rim are ignored.")]
        public IslandHandle grabGuard;

        public event Action<ThumbGesture, GestureSource> Gesture;

        OVRPlugin.HandTrackingState _state;
        readonly float[] _lastTime = { -1f, -1f };
        readonly OVRPlugin.MicrogestureType[] _lastType = new OVRPlugin.MicrogestureType[2];

        void Update()
        {
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
            Gesture?.Invoke(gesture, source);
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
            if (kb.leftArrowKey.wasPressedThisFrame) Gesture?.Invoke(ThumbGesture.SwipeLeft, GestureSource.Keyboard);
            if (kb.rightArrowKey.wasPressedThisFrame) Gesture?.Invoke(ThumbGesture.SwipeRight, GestureSource.Keyboard);
            if (kb.upArrowKey.wasPressedThisFrame) Gesture?.Invoke(ThumbGesture.SwipeForward, GestureSource.Keyboard);
            if (kb.downArrowKey.wasPressedThisFrame) Gesture?.Invoke(ThumbGesture.SwipeBack, GestureSource.Keyboard);
            if (kb.spaceKey.wasPressedThisFrame) Gesture?.Invoke(ThumbGesture.Tap, GestureSource.Keyboard);
        }
    }
}
