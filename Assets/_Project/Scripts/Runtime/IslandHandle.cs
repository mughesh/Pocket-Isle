using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Holding the floating island. The ISDK Grabbable on the island uses this as both its one- and two-grab
    /// transformer; grabs can only start on the rim colliders (HandGrabInteractable on the "Rim Grab" child),
    /// never on the play surface. Step 8d: palm grabs only (a pinch never moves the island; pinch is for pods, runes
    /// and look + pinch) on the rocky base skirt all around; <see cref="IslandGrabFrame"/> shows where (it replaced the
    /// rock brighten). The island always stays level (parallel to the floor).
    /// Its front (+Z: the Spirit, runes and pods side) always turns to face the head (yaw only, head position projected
    /// on the floor plane): while held, after release, and when idle if the player has shifted more than
    /// <see cref="idleAngle"/> for <see cref="idleSeconds"/> (not during the battle zoom).
    /// One hand: the island follows the hand, turning around it. Two hands: uniform scale around the hands' midpoint,
    /// scale clamped relative to the placer's scale; no manual yaw.
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

        [Header("Face the player")]
        [Tooltip("Turn the island's front toward the head while held, after release and when the player shifts.")]
        public bool autoFace = true;
        [Tooltip("Easing time while held or just released (s).")]
        public float faceSeconds = 0.25f;
        [Tooltip("Idle: turn back when the head is more than this far off the front (degrees)...")]
        public float idleAngle = 35f;
        [Tooltip("...for longer than this (s).")]
        public float idleSeconds = 1.5f;
        [Tooltip("Idle turn easing time (s).")]
        public float idleTurnSeconds = 1f;
        [Tooltip("No idle turning while the battle zoom is on.")]
        public WorldWindow window;

        [Header("Gesture guard")]
        [Tooltip("A hand whose thumb tip or palm is this close to the rim counts as busy.")]
        public float nearRimDistance = 0.05f;

        IGrabbable _grabbable;
        Grabbable _grabbableComponent;
        float _baseScale;

        // grab state captured on BeginTransform
        Vector3 _startPos, _offset, _startMid;
        float _startYaw, _startDist, _startScale;

        // facing: current eased yaw, whether a turn is in progress, how long the head has been off the front
        float _yaw;
        int _yawFrame = -1;
        bool _turning;
        float _turnSeconds, _offFor;

        readonly Hand[] _hands = new Hand[2];
        readonly bool[] _near = new bool[2];

        Transform Target => _grabbable != null ? _grabbable.Transform : transform;
        /// <summary>A hand is holding the island (the opening waits for this).</summary>
        public bool IsGrabbed => _grabbableComponent != null && _grabbableComponent.SelectingPointsCount > 0;

        /// <summary>Hint glow (the move and two-hands lessons): the grab frame pulses, 0..1.</summary>
        public float HintGlow { get; set; }

        /// <summary>A hand took hold of the island (the first grab point; pod cards close).</summary>
        public event System.Action GrabStarted;

        void Awake() => _grabbableComponent = GetComponent<Grabbable>();

        void Start()
        {
            _baseScale = placer != null ? placer.islandScale : transform.localScale.x;
            if (rimInteractable == null) Debug.LogError("[IslandHandle] No rim HandGrabInteractable assigned; the island can't be grabbed.");
            // The base skirt's front middle (the two-hands tip); the grab frame glows, no gold sphere.
            Dialogue.RegisterTarget("islandRim", () =>
            {
                if (island == null) return null;
                return island.transform.TransformPoint(new Vector3(0f, island.CapTopLocal - 0.05f, island.FootprintHalfSize.y));
            }, k => HintGlow = k, () => 0f);
        }

        // ---- ITransformer ----

        public void Initialize(IGrabbable grabbable) => _grabbable = grabbable;

        public void BeginTransform()
        {
            var points = _grabbable.GrabPoints;
            var t = Target;
            _startPos = t.position;
            _startYaw = _yaw = YawOf(t.rotation);
            _startScale = t.localScale.x;
            if (points.Count == 1)
            {
                _offset = _startPos - points[0].position;
                GrabStarted?.Invoke();
            }
            else if (points.Count >= 2)
            {
                Onboarding.Mark("twoHandGrab");
                Vector3 a = points[0].position, b = points[1].position;
                _startMid = (a + b) * 0.5f;
                _startDist = Mathf.Max(0.01f, Vector3.Distance(a, b));
            }
        }

        public void UpdateTransform()
        {
            if (_grabbable == null) return;
            var points = _grabbable.GrabPoints;
            var t = Target;
            if (points.Count == 1)
            {
                // Turns around the hand, so the grabbed rim stays in the hand.
                Vector3 hand = points[0].position;
                AdvanceYaw(hand + _offset, faceSeconds);
                var turn = Quaternion.Euler(0f, _yaw - _startYaw, 0f);
                t.SetPositionAndRotation(hand + turn * _offset, Quaternion.Euler(0f, _yaw, 0f));
                return;
            }
            if (points.Count < 2) return;

            Vector3 a = points[0].position, b = points[1].position;
            Vector3 mid = (a + b) * 0.5f;
            float scale = Mathf.Clamp(_startScale * Vector3.Distance(a, b) / _startDist,
                _baseScale * minScaleFactor, _baseScale * maxScaleFactor);
            Vector3 fromMid = (_startPos - _startMid) * (scale / _startScale);
            AdvanceYaw(mid + fromMid, faceSeconds);
            var yawRot = Quaternion.Euler(0f, _yaw - _startYaw, 0f);
            t.SetPositionAndRotation(mid + yawRot * fromMid, Quaternion.Euler(0f, _yaw, 0f));
            t.localScale = Vector3.one * scale;
        }

        public void EndTransform()
        {
            // Finish easing to face the head (Update carries on).
            if (autoFace) StartTurn(faceSeconds);
        }

        Transform Head => placer != null ? placer.Head : Camera.main != null ? Camera.main.transform : null;

        /// <summary>Yaw that points the island's front (+Z) from <paramref name="center"/> at the head.</summary>
        bool TryGetFacingYaw(Vector3 center, out float yaw)
        {
            yaw = 0f;
            var head = Head;
            if (head == null) return false;
            Vector3 to = Flat(head.position - center);
            if (to.sqrMagnitude < 1e-4f) return false;
            yaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            return true;
        }

        /// <summary>Eases <see cref="_yaw"/> toward facing the head from <paramref name="center"/>, once per frame.</summary>
        void AdvanceYaw(Vector3 center, float seconds)
        {
            if (!autoFace || _yawFrame == Time.frameCount) return;
            _yawFrame = Time.frameCount;
            if (!TryGetFacingYaw(center, out float target)) return;
            // Exponential ease; about 95% of the way in 'seconds'.
            _yaw = Mathf.LerpAngle(_yaw, target, 1f - Mathf.Exp(-Time.deltaTime * 3f / Mathf.Max(0.01f, seconds)));
        }

        void StartTurn(float seconds)
        {
            _yaw = YawOf(Target.rotation);
            _turning = true;
            _turnSeconds = seconds;
        }

        /// <summary>Not held: finish a turn, or start one when the player has shifted off the front for a while.</summary>
        void UpdateFacing()
        {
            if (!autoFace || IsGrabbed)
            {
                _offFor = 0f;
                return;
            }
            var t = Target;
            if (!TryGetFacingYaw(t.position, out float target)) return;
            float off = Mathf.Abs(Mathf.DeltaAngle(YawOf(t.rotation), target));
            bool zoomed = window != null && window.IsZoomed;
            if (!_turning)
            {
                _offFor = off > idleAngle && !zoomed ? _offFor + Time.deltaTime : 0f;
                if (_offFor < idleSeconds) return;
                _offFor = 0f;
                Debug.Log($"[IslandHandle] Head {off:0} deg off the front for {idleSeconds} s: turning to face it.");
                StartTurn(idleTurnSeconds);
            }
            AdvanceYaw(t.position, _turnSeconds);
            t.rotation = Quaternion.Euler(0f, _yaw, 0f);
            if (Mathf.Abs(Mathf.DeltaAngle(_yaw, target)) < 0.3f) _turning = false;
        }

        static float YawOf(Quaternion rotation)
        {
            Vector3 f = rotation * Vector3.forward;
            f.y = 0f;
            return f.sqrMagnitude > 1e-6f ? Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg : 0f;
        }

        // ---- per frame: hand proximity, highlight ----

        void Update()
        {
            _near[0] = IsNearRim(HandLookup.Refresh(ref _hands[0], Handedness.Left));
            _near[1] = IsNearRim(HandLookup.Refresh(ref _hands[1], Handedness.Right));
            UpdateFacing();
        }

        void LateUpdate()
        {
            // The Grabbable only calls UpdateTransform when a grab point moves; keep turning while the hands are still.
            if (IsGrabbed && autoFace) UpdateTransform();
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

        /// <summary>The island's size against where it was placed (two-hand resize): 1 = as placed.</summary>
        public float ScaleFactor => _baseScale > 1e-4f ? Target.localScale.x / _baseScale : 1f;

        /// <summary>This hand is hovering the base skirt, ready to palm-grab (ISDK hover), or holding it.</summary>
        public bool IsHandHovering(bool leftHand)
        {
            if (rimInteractable == null) return false;
            var handedness = leftHand ? Handedness.Left : Handedness.Right;
            foreach (var interactor in rimInteractable.Interactors)
                if (interactor != null && interactor.Hand != null && interactor.Hand.Handedness == handedness) return true;
            return false;
        }

        /// <summary>Distance (m) from a world point to the grab zone (0 inside it); the grab frame fades in by it.</summary>
        public float DistanceToZone(Vector3 p)
        {
            float best = float.MaxValue;
            if (rimColliders == null) return best;
            foreach (var c in rimColliders)
                if (c != null && c.enabled) best = Mathf.Min(best, (c.ClosestPoint(p) - p).magnitude);
            return best;
        }

        /// <summary>True while this hand holds the island (not merely near the rim).</summary>
        public bool IsHandGrabbing(bool leftHand)
        {
            if (rimInteractable == null) return false;
            var handedness = leftHand ? Handedness.Left : Handedness.Right;
            foreach (var interactor in rimInteractable.SelectingInteractors)
                if (interactor != null && interactor.Hand != null && interactor.Hand.Handedness == handedness) return true;
            return false;
        }

        /// <summary>True while this hand is grabbing the island or is within nearRimDistance of the rim.</summary>
        public bool IsHandBusy(bool leftHand)
        {
            return _near[leftHand ? 0 : 1] || IsHandGrabbing(leftHand);
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
