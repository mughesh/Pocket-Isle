using System.Collections.Generic;
using Oculus.Interaction.Input;
using TMPro;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 2b debug experiment: can we tell "thumb held off-centre after a swipe" from "thumb at rest"?
    /// Reads ISDK hand joint poses (v207, OpenXR joint set) for the hand that last fired a microgesture and
    /// shows the thumb tip's offset from its rest point in mm, in a frame attached to the index finger:
    /// along = towards the index tip, across = towards the back of the hand.
    /// The rest point is the thumb position sampled just before the most recent swipe from that hand.
    /// Does not drive movement.
    /// </summary>
    public class ThumbHoldProbe : MonoBehaviour
    {
        public MicrogestureInput input;

        [Tooltip("Rest point = thumb position this many seconds before the microgesture event.")]
        public float restLookback = 0.5f;
        [Tooltip("Offset (mm) counted as clearly off-centre.")]
        public float heldThresholdMm = 8f;
        [Tooltip("Bar range, +/- mm.")]
        public float barRangeMm = 20f;

        struct Sample
        {
            public float time;
            public Vector2 offset; // metres, (along, across)
        }

        class HandProbe
        {
            public Hand hand;
            public readonly Queue<Sample> history = new Queue<Sample>();
            public Vector2 rest;
            public bool hasRest;
            public Vector2 current;
            public bool valid;
        }

        readonly HandProbe[] _hands = { new HandProbe(), new HandProbe() }; // 0 left, 1 right
        int _active = 1;
        string _lastGesture = "none yet";
        float _lastGestureTime = -1f;
        TextMeshPro _label;

        void Start()
        {
            var go = new GameObject("Thumb Probe Label");
            go.transform.SetParent(transform, false);
            _label = go.AddComponent<TextMeshPro>();
            _label.fontSize = 0.18f;
            _label.alignment = TextAlignmentOptions.Left;
            _label.color = Color.white;
            _label.outlineWidth = 0.2f;
            _label.outlineColor = new Color32(20, 30, 25, 255);
            _label.rectTransform.sizeDelta = new Vector2(0.2f, 0.08f);

            if (input != null) input.Gesture += OnGesture;
            else Debug.LogError("[ThumbHoldProbe] No MicrogestureInput assigned.");
        }

        void OnDestroy()
        {
            if (input != null) input.Gesture -= OnGesture;
        }

        void OnGesture(ThumbGesture gesture, GestureSource source)
        {
            _lastGesture = $"{gesture} ({source})";
            _lastGestureTime = Time.unscaledTime;
            if (source == GestureSource.Keyboard) return;
            _active = source == GestureSource.LeftHand ? 0 : 1;
            var probe = _hands[_active];
            if (TryGetSample(probe, Time.unscaledTime - restLookback, out var rest))
            {
                probe.rest = rest;
                probe.hasRest = true;
            }
        }

        void Update()
        {
            for (int i = 0; i < 2; i++) UpdateHand(_hands[i], i == 0 ? Handedness.Left : Handedness.Right);
            UpdateLabel();
        }

        void UpdateHand(HandProbe probe, Handedness handedness)
        {
            if (probe.hand == null) probe.hand = FindHand(handedness);
            probe.valid = probe.hand != null && probe.hand.IsTrackedDataValid && TryReadOffset(probe.hand, out probe.current);
            if (!probe.valid) return;

            float now = Time.unscaledTime;
            probe.history.Enqueue(new Sample { time = now, offset = probe.current });
            while (probe.history.Count > 0 && probe.history.Peek().time < now - restLookback - 0.5f) probe.history.Dequeue();

            // Until the first swipe there is no measured rest; follow the thumb slowly so the numbers are readable.
            if (!probe.hasRest) probe.rest = Vector2.Lerp(probe.rest == Vector2.zero ? probe.current : probe.rest, probe.current, Time.unscaledDeltaTime);
        }

        static Hand FindHand(Handedness handedness)
        {
            // Raw tracked hands only; SyntheticHand is the visual hand that gets locked to interactables.
            foreach (var hand in FindObjectsByType<Hand>(FindObjectsSortMode.None))
                if (!(hand is SyntheticHand) && hand.Handedness == handedness) return hand;
            return null;
        }

        static bool TryReadOffset(IHand hand, out Vector2 offset)
        {
            offset = default;
            if (!hand.GetJointPose(HandJointId.HandThumbTip, out var thumb) ||
                !hand.GetJointPose(HandJointId.HandIndex1, out var index1) ||
                !hand.GetJointPose(HandJointId.HandIndex3, out var index3) ||
                !hand.GetJointPose(HandJointId.HandMiddle1, out var middle1)) return false;

            Vector3 along = (index3.position - index1.position).normalized;
            Vector3 toMiddle = Vector3.ProjectOnPlane(middle1.position - index1.position, along).normalized;
            Vector3 across = Vector3.Cross(along, toMiddle);
            if (hand.Handedness == Handedness.Left) across = -across; // positive = back of the hand on both hands
            Vector3 d = thumb.position - index1.position;
            offset = new Vector2(Vector3.Dot(d, along), Vector3.Dot(d, across));
            return true;
        }

        bool TryGetSample(HandProbe probe, float time, out Vector2 offset)
        {
            offset = default;
            bool found = false;
            foreach (var s in probe.history)
            {
                if (s.time > time && found) break;
                offset = s.offset;
                found = true;
            }
            return found;
        }

        void UpdateLabel()
        {
            var probe = _hands[_active];
            string handName = _active == 0 ? "Left" : "Right";
            string ago = _lastGestureTime < 0f ? "" : $" {Time.unscaledTime - _lastGestureTime:0.0}s ago";
            if (!probe.valid)
            {
                _label.text = $"<b>{handName}:</b> not tracked\n<b>Last:</b> {_lastGesture}{ago}";
                return;
            }

            Vector2 mm = (probe.current - probe.rest) * 1000f;
            bool held = mm.magnitude >= heldThresholdMm;
            string restNote = probe.hasRest ? "" : " (no swipe yet)";
            _label.text =
                $"<b>{handName}</b>{restNote}  <b>Last:</b> {_lastGesture}{ago}\n" +
                $"<mspace=0.55em>along  {mm.x,6:+0.0;-0.0} {Bar(mm.x)}\n" +
                $"across {mm.y,6:+0.0;-0.0} {Bar(mm.y)}</mspace>\n" +
                (held ? "<color=#7CFC8A><b>OFF-CENTRE</b></color>" : "<color=#BBBBBB>at rest</color>");

            // Float the label above the tracked wrist, facing the user.
            if (probe.hand.GetRootPose(out var wrist))
            {
                _label.transform.position = wrist.position + Vector3.up * 0.1f;
                var cam = Camera.main;
                if (cam != null)
                {
                    Vector3 to = _label.transform.position - cam.transform.position;
                    to.y = 0f;
                    if (to.sqrMagnitude > 1e-6f) _label.transform.rotation = Quaternion.LookRotation(to, Vector3.up);
                }
            }
        }

        string Bar(float mm)
        {
            const int half = 10;
            int n = Mathf.Clamp(Mathf.RoundToInt(mm / barRangeMm * half), -half, half);
            var c = new char[half * 2 + 1];
            for (int i = 0; i < c.Length; i++)
            {
                int pos = i - half;
                c[i] = pos == 0 ? '|' : (n < 0 && pos >= n && pos < 0) || (n > 0 && pos <= n && pos > 0) ? '#' : '-';
            }
            return new string(c);
        }
    }
}
