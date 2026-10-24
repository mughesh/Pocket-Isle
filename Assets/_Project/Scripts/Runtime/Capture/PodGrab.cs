using System;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Picks a pod up with the Interaction SDK (pinch or palm grab on its sphere collider; the Grabbable's default
    /// transformer makes it follow the hand) and reports the release with the hand's throw velocity, measured from the
    /// pod's own positions over the last <see cref="velocityWindow"/> seconds. The flight after a release is not physics:
    /// <see cref="PodRack"/> and the encounter animate it (aim assist).
    /// </summary>
    [RequireComponent(typeof(Grabbable))]
    public class PodGrab : MonoBehaviour
    {
        public HandGrabInteractable interactable;
        [Tooltip("Release velocity is averaged over this much time (s).")]
        public float velocityWindow = 0.08f;

        /// <summary>A hand picked the pod up.</summary>
        public event Action<PodGrab> Grabbed;
        /// <summary>The hand let go: world velocity (m/s).</summary>
        public event Action<PodGrab, Vector3> Released;

        const int Samples = 12;
        readonly Vector3[] _pos = new Vector3[Samples];
        readonly float[] _time = new float[Samples];
        int _count, _head;
        Grabbable _grabbable;
        bool _held;

        public bool IsHeld => _held;

        /// <summary>Whether a hand can pick it up now. Turning it off while held drops it (a release with no speed).</summary>
        public bool CanGrab
        {
            get => interactable != null && interactable.enabled;
            set
            {
                if (interactable != null && interactable.enabled != value) interactable.enabled = value;
            }
        }

        void Awake()
        {
            _grabbable = GetComponent<Grabbable>();
            if (interactable == null) interactable = GetComponentInChildren<HandGrabInteractable>(true);
            if (interactable == null) Debug.LogError("[PodGrab] No HandGrabInteractable; the pod can't be grabbed.", this);
        }

        void OnEnable() => _grabbable.WhenPointerEventRaised += OnPointerEvent;
        void OnDisable() => _grabbable.WhenPointerEventRaised -= OnPointerEvent;

        void OnPointerEvent(PointerEvent e)
        {
            if (e.Type == PointerEventType.Select && !_held)
            {
                _held = true;
                _count = 0;
                Record();
                Grabbed?.Invoke(this);
            }
            else if ((e.Type == PointerEventType.Unselect || e.Type == PointerEventType.Cancel) && _held && _grabbable.SelectingPointsCount <= 1)
            {
                Record();
                _held = false;
                var v = e.Type == PointerEventType.Cancel ? Vector3.zero : Velocity();
                Released?.Invoke(this, v);
            }
        }

        void LateUpdate()
        {
            if (_held) Record();
        }

        void Record()
        {
            _pos[_head] = transform.position;
            _time[_head] = Time.time;
            _head = (_head + 1) % Samples;
            _count = Mathf.Min(_count + 1, Samples);
        }

        /// <summary>Average velocity over the newest samples inside the window.</summary>
        Vector3 Velocity()
        {
            if (_count < 2) return Vector3.zero;
            int newest = (_head - 1 + Samples) % Samples;
            int oldest = newest;
            for (int k = 1; k < _count; k++)
            {
                int i = (newest - k + Samples) % Samples;
                oldest = i;
                if (_time[newest] - _time[i] >= velocityWindow) break;
            }
            float dt = _time[newest] - _time[oldest];
            return dt > 1e-4f ? (_pos[newest] - _pos[oldest]) / dt : Vector3.zero;
        }
    }
}
