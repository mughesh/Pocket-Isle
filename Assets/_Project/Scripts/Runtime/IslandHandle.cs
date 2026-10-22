using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Holding the floating island. The ISDK Grabbable on the island uses this as both its one- and two-grab
    /// transformer; grabs can only start on the rim colliders (HandGrabInteractable on the "Rim Grab" child),
    /// never on the play surface.
    /// One hand: the island follows the hand, keeping the grab offset (no rotation).
    /// Two hands: yaw only and uniform scale around the hands' midpoint, scale clamped relative to the start scale.
    /// Release (and every re-place): eases so the top faces the head, pitch clamped.
    /// Also reports which hands are busy with the island so microgestures can ignore them.
    /// </summary>
    [RequireComponent(typeof(Grabbable))]
    public class IslandHandle : MonoBehaviour, ITransformer
    {
        public IslandBuilder island;
        public IslandPlacer placer;
        public HandGrabInteractable rimInteractable;
        [Tooltip("Rim colliders, for the near-rim gesture guard.")]
        public Collider[] rimColliders;

        [Header("Two-hand scale (x the placer's island scale)")]
        public float minScaleFactor = 0.5f;
        public float maxScaleFactor = 2f;

        [Header("Tilt toward the head")]
        public float tiltSeconds = 0.4f;
        [Range(0f, 89f)] public float maxPitch = 40f;

        [Header("Gesture guard and hover")]
        [Tooltip("A hand whose thumb tip or palm is this close to the rim counts as busy.")]
        public float nearRimDistance = 0.05f;
        [Range(0f, 1f)] public float highlight = 0.15f;
        public float highlightFadeSeconds = 0.12f;

        IGrabbable _grabbable;
        Grabbable _grabbableComponent;
        float _baseScale;

        // grab state captured on BeginTransform
        Vector3 _startPos, _offset, _startMid, _startFlat;
        Quaternion _startRot;
        float _startDist, _startScale;

        Quaternion _tiltFrom, _tiltTo;
        float _tiltT = 1f;

        readonly Hand[] _hands = new Hand[2];
        readonly bool[] _near = new bool[2];
        float _glow;

        Transform Target => _grabbable != null ? _grabbable.Transform : transform;
        bool IsGrabbed => _grabbableComponent != null && _grabbableComponent.SelectingPointsCount > 0;

        void Awake() => _grabbableComponent = GetComponent<Grabbable>();

        void Start()
        {
            _baseScale = placer != null ? placer.islandScale : transform.localScale.x;
            _grabbableComponent.WhenPointerEventRaised += OnPointerEvent;
            if (placer != null) placer.Placed += StartTilt;
            if (rimInteractable == null) Debug.LogError("[IslandHandle] No rim HandGrabInteractable assigned; the island can't be grabbed.");
        }

        void OnDestroy()
        {
            if (_grabbableComponent != null) _grabbableComponent.WhenPointerEventRaised -= OnPointerEvent;
            if (placer != null) placer.Placed -= StartTilt;
        }

        void OnPointerEvent(PointerEvent evt)
        {
            if ((evt.Type == PointerEventType.Unselect || evt.Type == PointerEventType.Cancel) && !IsGrabbed) StartTilt();
        }

        // ---- ITransformer ----

        public void Initialize(IGrabbable grabbable) => _grabbable = grabbable;

        public void BeginTransform()
        {
            _tiltT = 1f;
            var points = _grabbable.GrabPoints;
            var t = Target;
            _startPos = t.position;
            _startRot = t.rotation;
            _startScale = t.localScale.x;
            if (points.Count == 1)
            {
                _offset = _startPos - points[0].position;
            }
            else if (points.Count >= 2)
            {
                Vector3 a = points[0].position, b = points[1].position;
                _startMid = (a + b) * 0.5f;
                _startFlat = Flat(b - a);
                _startDist = Mathf.Max(0.01f, Vector3.Distance(a, b));
            }
        }

        public void UpdateTransform()
        {
            var points = _grabbable.GrabPoints;
            var t = Target;
            if (points.Count == 1)
            {
                t.position = points[0].position + _offset;
                return;
            }
            if (points.Count < 2) return;

            Vector3 a = points[0].position, b = points[1].position;
            Vector3 mid = (a + b) * 0.5f;
            Vector3 flat = Flat(b - a);
            float yaw = _startFlat.sqrMagnitude > 1e-6f && flat.sqrMagnitude > 1e-6f
                ? Vector3.SignedAngle(_startFlat, flat, Vector3.up) : 0f;
            float scale = Mathf.Clamp(_startScale * Vector3.Distance(a, b) / _startDist,
                _baseScale * minScaleFactor, _baseScale * maxScaleFactor);
            var yawRot = Quaternion.AngleAxis(yaw, Vector3.up);

            t.SetPositionAndRotation(mid + yawRot * (_startPos - _startMid) * (scale / _startScale), yawRot * _startRot);
            t.localScale = Vector3.one * scale;
        }

        public void EndTransform() { }

        // ---- tilt ----

        /// <summary>Eases the island so its top faces the head (pitch 0..maxPitch), keeping its yaw.</summary>
        public void StartTilt()
        {
            var cam = Camera.main;
            if (cam == null) return;
            var t = Target;
            Vector3 toHead = cam.transform.position - t.position;
            Vector3 flat = new Vector3(toHead.x, 0f, toHead.z);
            float elevation = Mathf.Atan2(toHead.y, flat.magnitude) * Mathf.Rad2Deg; // 90 = head straight above
            float pitch = Mathf.Clamp(90f - elevation, 0f, maxPitch) * Mathf.Deg2Rad;
            Vector3 dir = flat.sqrMagnitude > 1e-6f ? flat.normalized : Vector3.forward;
            Vector3 up = Vector3.up * Mathf.Cos(pitch) + dir * Mathf.Sin(pitch);

            _tiltFrom = t.rotation;
            _tiltTo = Quaternion.FromToRotation(Vector3.up, up) * Quaternion.Euler(0f, YawOf(t.rotation), 0f);
            _tiltT = 0f;
        }

        static float YawOf(Quaternion rotation)
        {
            Vector3 f = rotation * Vector3.forward;
            f.y = 0f;
            if (f.sqrMagnitude < 1e-6f)
            {
                f = rotation * Vector3.up; // island stood on its edge; use where its top points
                f.y = 0f;
            }
            return Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
        }

        // ---- per frame: tilt animation, hand proximity, highlight ----

        void Update()
        {
            if (_tiltT < 1f && !IsGrabbed)
            {
                _tiltT = Mathf.Min(1f, _tiltT + Time.deltaTime / Mathf.Max(0.01f, tiltSeconds));
                Target.rotation = Quaternion.Slerp(_tiltFrom, _tiltTo, Mathf.SmoothStep(0f, 1f, _tiltT));
            }

            _near[0] = IsNearRim(HandLookup.Refresh(ref _hands[0], Handedness.Left));
            _near[1] = IsNearRim(HandLookup.Refresh(ref _hands[1], Handedness.Right));

            bool hover = rimInteractable != null && rimInteractable.State != InteractableState.Normal &&
                         rimInteractable.State != InteractableState.Disabled;
            float target = hover || _near[0] || _near[1] || IsGrabbed ? highlight : 0f;
            _glow = Mathf.MoveTowards(_glow, target, Time.deltaTime * highlight / Mathf.Max(0.01f, highlightFadeSeconds));
            if (island != null) island.rimHighlight = _glow;
        }

        bool IsNearRim(Hand hand)
        {
            if (hand == null || rimColliders == null) return false;
            return NearRim(hand, HandJointId.HandThumbTip) || NearRim(hand, HandJointId.HandPalm);
        }

        bool NearRim(Hand hand, HandJointId joint)
        {
            if (!HandLookup.TryGetJoint(hand, joint, out var p)) return false;
            foreach (var c in rimColliders)
                if (c != null && c.enabled && (c.ClosestPoint(p) - p).sqrMagnitude <= nearRimDistance * nearRimDistance) return true;
            return false;
        }

        /// <summary>True while this hand is grabbing the island or is within nearRimDistance of the rim.</summary>
        public bool IsHandBusy(bool leftHand)
        {
            if (_near[leftHand ? 0 : 1]) return true;
            if (rimInteractable == null) return false;
            var handedness = leftHand ? Handedness.Left : Handedness.Right;
            foreach (var interactor in rimInteractable.SelectingInteractors)
                if (interactor != null && interactor.Hand != null && interactor.Hand.Handedness == handedness) return true;
            return false;
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
