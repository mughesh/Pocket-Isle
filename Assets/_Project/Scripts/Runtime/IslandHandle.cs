using System.Collections.Generic;
using Diorama.Audio;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Holding the floating island. The ISDK Grabbable on the island uses this as both its one- and two-grab
    /// transformer; grabs can only start on the rim colliders (HandGrabInteractable on the "Rim Grab" child),
    /// never on the play surface: a thin shell around the rocky base skirt; <see cref="IslandGrabFrame"/> shows where.
    /// Step 8e: pinch or palm. Two modes: Explore (the whole skirt, the front pod strip too; the rim pods are inert) and
    /// Pods active (battles and the lodge: the pods can be grabbed, so the strip in front of them is not grab zone).
    /// Every pinch is resolved once, as it starts: an island grab only when the pinch point (or palm) is in the shell,
    /// and, if the gaze is on something selectable at that moment, only when the hand had been in the shell for
    /// <see cref="grabHoverSeconds"/> first; otherwise it is a look + pinch (gaze select). The ISDK grab asks
    /// <see cref="Filter"/>, the gaze systems ask <see cref="IsHandBusy"/>, so both always agree.
    /// The island always stays level (parallel to the floor).
    /// Its front (+Z: the Spirit, runes and pods side) always turns to face the head (yaw only, head position projected
    /// on the floor plane): while held, after release, and when idle if the player has shifted more than
    /// <see cref="reFaceAngle"/> for <see cref="idleSeconds"/> (not during the battle zoom, a grab or a lesson ghost; step 17:
    /// that idle turn is off by default, <see cref="idleReface"/>).
    /// One hand: the island follows the hand, turning around it. Two hands: uniform scale around the hands' midpoint,
    /// scale clamped relative to the placer's scale; no manual yaw.
    /// Also reports which hands are busy with the island so microgestures can ignore them.
    /// Step 14a: only the handles grab: the four corners (always, plain grabs) and the front lip (the near face of the
    /// base at least <see cref="lipBelowPods"/> under the pods' centre line; Explore only, and a look elsewhere needs the
    /// short hover first). A pinch point or palm within a pod's radius (+ <see cref="podClearance"/>) never grabs: the
    /// pods win. Everywhere else on the rim, pinches go to pods and objects. Two hands = any two handles.
    /// Step 14j: the step 14i hold to grab is reverted (Mugesh couldn't grab at all); the plain grab is back with a tight
    /// zone (<see cref="GrabZone"/>): at most <see cref="grabOutside"/> world m outside the glowing line, <see cref="grabInside"/>
    /// in over the rock, between <see cref="grabBandTop"/> and <see cref="grabBandBottom"/>. A pinch is judged by its pinch
    /// point only (the palm counts only for a palm grab).
    /// </summary>
    [RequireComponent(typeof(Grabbable))]
    public class IslandHandle : MonoBehaviour, ITransformer, IGameObjectFilter
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
        [Tooltip("Nov 15 (Mugesh: a small move to the side turned it too eagerly): while held, the island keeps the yaw it was " +
                 "grabbed with until facing the head would need more than this (degrees); past it, it turns by the extra only. " +
                 "0 = always face the head exactly.")]
        public float holdStraightAngle = 20f;
        [Tooltip("Nov 15: after a release the island settles to face the head exactly, over about this long (s).")]
        public float releaseSettleSeconds = 0.6f;
        [Tooltip("Step 17 G: turn to face the head by itself when idle (off: leaning in 10 cm for fine work turned the island 10-12 deg, which read as a tilt; the island root moves only by a grab, a recenter or the first placement). The turn while held and after a release stays.")]
        public bool idleReface;
        [Tooltip("Idle: turn back when the head is more than this far off the front (degrees)... (step 14i: was 35, a new name so the scene takes 6)")]
        public float reFaceAngle = 6f;
        [Tooltip("...for longer than this (s).")]
        public float idleSeconds = 1.5f;
        [Tooltip("Idle turn easing time (s).")]
        public float idleTurnSeconds = 1f;
        [Tooltip("No idle turning while the battle zoom is on.")]
        public WorldWindow window;
        [Tooltip("Step 14i: no idle turning while a lesson ghost demonstrates on the island (found at Start when empty).")]
        public GhostHands ghosts;

        [Header("Modes and pinch (step 8e)")]
        [Tooltip("Pods active while it is in the lodge or a battle.")]
        public PodRack pods;
        [Tooltip("Pods active from the encounter's start to the zoom-out.")]
        public EncounterDirector encounters;
        [Tooltip("Grab shell over the front pod strip: grab zone while exploring, off while the pods are active.")]
        public Collider[] podStripColliders;
        [Tooltip("Gaze on something selectable away from the hand: a pinch on the lip is a grab only after the hand was in the zone this long (s).")]
        public float grabHoverSeconds = 0.15f;
        [Tooltip("The gaze target counts as 'away from the hand' beyond this distance from the pinch point (world m); nearer, the player is looking at what they grab.")]
        public float gazeAwayDistance = 0.15f;
        [Tooltip("Corners (island-local m from each corner, along both edges; the corner brackets): plain grabs, no grab-or-select rule.")]
        public float cornerSize = 0.1f;

        [Header("Grab zone (step 14j: what you see is what grabs)")]
        [Tooltip("The glowing frame (its line's outset and corner radius); found at Start when empty.")]
        public IslandGrabFrame frame;
        [Tooltip("Step 14k: slack past the glowing line (world m; tracking jitter only: nothing outside the line grabs).")]
        public float grabPastLine = 0.003f;
        [Tooltip("Step 14k: the band's depth from the line inward, over the rim (world m).")]
        public float grabInward = 0.035f;
        [Tooltip("Step 14k: the band reaches this far above the cap top (world m: a pinch on the rim's top closes above it).")]
        public float grabAboveCap = 0.025f;
        [Tooltip("Bottom of the band (island-local y; the shell reaches -0.13).")]
        public float grabBandBottom = -0.12f;
        [Tooltip("A pinch point counts as a pinch (not a palm grab) at this index pinch strength.")]
        [Range(0f, 1f)] public float pinchStrengthForPoint = 0.4f;

        [Header("Handles (step 14a)")]
        [Tooltip("The front lip grabs only this far (world m) or more below the rim pods' centre line.")]
        public float lipBelowPods = 0.02f;
        [Tooltip("A pinch point or palm this close to a pod's surface (world m) is the pod's, never the island's.")]
        public float podClearance = 0.01f;
        [Tooltip("Step 17: a pinch this close to the cradle pod's glass (world m) goes to the pod, never the island corner under it. " +
                 "Nov 15: was 12 cm (cradlePodClearance), which covered the whole near-right corner in a battle.")]
        public float cradleClearance = 0.03f;

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

        /// <summary>One hand's pinch resolution. Grab = the current intent while not pinching, frozen once a pinch starts.</summary>
        struct PinchState
        {
            public int frame;
            public bool pinching, grab, inZone;
            public float zoneSince;
            public string last;
        }

        readonly PinchState[] _pinch = new PinchState[2];
        readonly Dictionary<GameObject, int> _interactorSide = new Dictionary<GameObject, int>();
        bool _podsActive, _modeLogged;

        float _releaseAt = -1f, _lastScale;
        AudioHandle _scaleLoop;

        Transform Target => _grabbable != null ? _grabbable.Transform : transform;
        /// <summary>A hand is holding the island (the opening waits for this).</summary>
        public bool IsGrabbed => _grabbableComponent != null && _grabbableComponent.SelectingPointsCount > 0;

        /// <summary>Hint glow (the move and two-hands lessons): the grab frame pulses, 0..1.</summary>
        public float HintGlow { get; set; }

        /// <summary>A hand took hold of the island (the first grab point; pod cards close).</summary>
        public event System.Action GrabStarted;

        void Awake() => _grabbableComponent = GetComponent<Grabbable>();

        void OnDestroy()
        {
            if (placer != null) placer.Placed -= LogFacing;
        }

        void Start()
        {
            _baseScale = placer != null ? placer.islandScale : transform.localScale.x;
            if (rimInteractable == null) Debug.LogError("[IslandHandle] No rim HandGrabInteractable assigned; the island can't be grabbed.");
            else rimInteractable.InjectOptionalInteractorFilters(new List<IGameObjectFilter> { this });
            if (pods == null || encounters == null) Debug.LogError("[IslandHandle] No pod rack / encounters (Set Up MR_Main (8e)): always in Explore mode.", this);
            ApplyMode(true);
            if (ghosts == null) ghosts = FindFirstObjectByType<GhostHands>();
            if (frame == null) frame = FindFirstObjectByType<IslandGrabFrame>();
            WidenShell();
            if (placer != null) placer.Placed += LogFacing;
            // The base skirt's front middle (the two-hands tip); the grab frame glows, no gold sphere.
            Dialogue.RegisterTarget("islandRim", () =>
            {
                if (island == null) return null;
                return island.transform.TransformPoint(new Vector3(0f, island.CapTopLocal - 0.05f, island.FootprintHalfSize.y));
            }, k => HintGlow = k, () => 0f);
        }

        /// <summary>
        /// Step 14k: the ISDK shell boxes (step 8e: 4 cm inside the base edge, top 1.2 cm over the cap) grow inward and up
        /// to hold the inward band at the smallest island size; <see cref="Filter"/> keeps only the band. Trigger boxes: no
        /// raycast uses them (tile gaze and pokes have their own surfaces).
        /// </summary>
        void WidenShell()
        {
            if (rimColliders == null || island == null) return;
            var half = island.FootprintHalfSize;
            float minScale = Mathf.Max(0.05f, (placer != null ? placer.islandScale : transform.localScale.x) * minScaleFactor);
            float edgeIn = grabInward / minScale - (frame != null ? frame.outset : 0.008f) + 0.005f; // local m inside the base edge
            float top = island.CapTopLocal + grabAboveCap / minScale + 0.005f;
            int widened = 0;
            foreach (var c in rimColliders)
            {
                if (!(c is BoxCollider b)) continue;
                Vector3 min = b.center - b.size * 0.5f, max = b.center + b.size * 0.5f;
                max.y = Mathf.Max(max.y, top);
                if (Mathf.Abs(Mathf.Abs(b.center.z) - half.y) < 0.01f)
                {
                    if (b.center.z > 0f) min.z = Mathf.Min(min.z, half.y - edgeIn);
                    else max.z = Mathf.Max(max.z, -half.y + edgeIn);
                }
                else if (b.center.x > 0f) min.x = Mathf.Min(min.x, half.x - edgeIn);
                else max.x = Mathf.Max(max.x, -half.x + edgeIn);
                b.center = (min + max) * 0.5f;
                b.size = max - min;
                widened++;
            }
            Debug.Log($"[IslandHandle] Step 14k grab band: {grabInward * 100f:0.0} cm in from the line, {grabPastLine * 1000f:0} mm slack outside, " +
                      $"up to {grabAboveCap * 100f:0.0} cm over the cap; {widened} shell boxes widened to {edgeIn:0.000} local inside the edge, top {top:0.000}.");
        }

        // ---- ITransformer ----

        public void Initialize(IGrabbable grabbable) => _grabbable = grabbable;

        public void BeginTransform()
        {
            var points = _grabbable.GrabPoints;
            var t = Target;
            _startPos = t.position;
            _startYaw = _yaw = YawOf(t.rotation);
            _grabAt = Time.time;
            _startScale = t.localScale.x;
            // ISDK ends and begins again when a second hand joins or leaves: only a fresh grab scrapes.
            if (_releaseAt < 0f) GameAudio.At(AudioIds.IslandGrab, points.Count > 0 ? points[0].position : t.position);
            _releaseAt = -1f;
            _scaleLoop.Stop(0.2f);
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
                _scaleLoop = AudioService.PlayLoop(AudioIds.IslandScale, t, 0.2f);
                _scaleLoop.SetVolume(0f);
                _lastScale = _startScale;
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
                HeldYaw(hand, _offset);
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
            HeldYaw(mid, fromMid);
            var yawRot = Quaternion.Euler(0f, _yaw - _startYaw, 0f);
            t.SetPositionAndRotation(mid + yawRot * fromMid, Quaternion.Euler(0f, _yaw, 0f));
            t.localScale = Vector3.one * scale;
            // The creak follows how fast it resizes.
            float speed = Mathf.Abs(scale - _lastScale) / Mathf.Max(1e-4f, _startScale * Time.deltaTime);
            _lastScale = scale;
            _scaleLoop.SetVolume(Mathf.Clamp01(speed * 2f));
            _scaleLoop.SetPitch(scale >= _startScale ? 1.05f : 0.95f);
        }

        public void EndTransform()
        {
            _scaleLoop.Stop(0.2f);
            _releaseAt = Time.time;
            // Nov 15 (Mugesh: with a recenter a little off, the held-straight island kept that error): after a release it
            // settles gently to face the head exactly (the hold-straight zone only calms it while held).
            Debug.Log($"[IslandHandle] Released: {FacingError():0.0} deg off the line to the head; settling to face it over {releaseSettleSeconds:0.0#} s.");
            if (autoFace) StartTurn(releaseSettleSeconds);
            _logFacingAfterTurn = true;
        }

        bool _logFacingAfterTurn;

        /// <summary>Step 14i: the yaw error (front vs the line from the island's centre to the head) after a placement.</summary>
        public float FacingError()
        {
            var t = Target;
            return TryGetFacingYaw(t.position, out float target) ? Mathf.DeltaAngle(YawOf(t.rotation), target) : 0f;
        }

        void LogFacing() => Debug.Log($"[IslandHandle] Facing after placement: {FacingError():0.0} deg off the line to the head.");

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

        float _grabAt;

        /// <summary>
        /// Nov 15 (Mugesh: every grab left a slight tilt): while held, the front faces the head exactly from where the
        /// island really is, like a Quest window being moved. Before, the facing was worked out from
        /// <c>pivot + offset</c> before the turn around the hand, so it aimed from the wrong spot (a few degrees off while
        /// held, then a twist after release), and it eased behind the hand. Now: the centre after the turn (solved in a
        /// few steps, since the centre depends on the turn), followed exactly; only the first <see cref="faceSeconds"/> of
        /// a grab blend in from the yaw it had, so a grab never snaps.
        /// </summary>
        void HeldYaw(Vector3 pivot, Vector3 offset)
        {
            if (!autoFace || _yawFrame == Time.frameCount) return;
            _yawFrame = Time.frameCount;
            float y = _yaw;
            // Half steps: a corner grab can be as far from the centre as the head is, and full steps would overshoot.
            for (int i = 0; i < 10; i++)
            {
                Vector3 center = pivot + Quaternion.Euler(0f, y - _startYaw, 0f) * offset;
                if (!TryGetFacingYaw(center, out float face)) return;
                y += 0.5f * Mathf.DeltaAngle(y, DeadZoneYaw(_startYaw, face, holdStraightAngle));
            }
            float k = Mathf.SmoothStep(0f, 1f, (Time.time - _grabAt) / Mathf.Max(0.01f, faceSeconds));
            _yaw = Mathf.LerpAngle(_startYaw, y, k);
        }

        /// <summary>
        /// Nov 15: the grabbed yaw (<paramref name="startYaw"/>) while facing the head (<paramref name="faceYaw"/>) is within
        /// <paramref name="deadZone"/> of it; past that, turned toward the head by the extra only (no jump at the edge).
        /// </summary>
        public static float DeadZoneYaw(float startYaw, float faceYaw, float deadZone)
        {
            float off = Mathf.DeltaAngle(startYaw, faceYaw);
            return startYaw + Mathf.Sign(off) * Mathf.Max(0f, Mathf.Abs(off) - Mathf.Max(0f, deadZone));
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
            // Step 14i: never while a lesson ghost demonstrates on the island (its targets would slide away).
            bool ghost = ghosts != null && ghosts.IsShowing && !ghosts.previewing;
            if (!_turning)
            {
                _offFor = idleReface && off > reFaceAngle && !zoomed && !ghost ? _offFor + Time.deltaTime : 0f;
                if (_offFor < idleSeconds) return;
                _offFor = 0f;
                Debug.Log($"[IslandHandle] Head {off:0.0} deg off the front for {idleSeconds} s: turning to face it.");
                StartTurn(idleTurnSeconds);
            }
            AdvanceYaw(t.position, _turnSeconds);
            _facingWriter = _logFacingAfterTurn ? "facing turn after a release" : "idle facing turn (head off the front)";
            _facingFrame = Time.frameCount;
            t.rotation = Quaternion.Euler(0f, _yaw, 0f);
            if (Mathf.Abs(Mathf.DeltaAngle(_yaw, target)) < 0.3f)
            {
                _turning = false;
                if (_logFacingAfterTurn) Debug.Log($"[IslandHandle] Facing after release: {FacingError():0.0} deg off the line to the head.");
                _logFacingAfterTurn = false;
            }
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
            // The release sound waits a beat so a hand change (end + begin) isn't a release.
            if (_releaseAt >= 0f && Time.time - _releaseAt > 0.08f)
            {
                _releaseAt = -1f;
                if (!IsGrabbed) GameAudio.At(AudioIds.IslandRelease, Target.position);
            }
            ApplyMode(false);
            ApplyLocks();
            Resolve(0);
            Resolve(1);
            if (DevReadout.Active)
                for (int h = 0; h < 2; h++)
                    if (_pinch[h].last != null) DevReadout.Text($"pinch{h}", _pinch[h].last);
            UpdateFacing();
        }

        void LateUpdate()
        {
            // The Grabbable only calls UpdateTransform when a grab point moves; keep turning while the hands are still.
            if (IsGrabbed && autoFace) UpdateTransform();
            WatchPose();
        }

        // ---- step 17 G: who moves the island root ----

        /// <summary>A writer of the island root's pose says so just before (the placer, the opening); read by the pose log.</summary>
        public static void MarkPoseWriter(string who)
        {
            _poseWriter = who;
            _poseWriterFrame = Time.frameCount;
        }

        static string _poseWriter;
        static int _poseWriterFrame = -10;
        Vector3 _lastLocalPos;
        Quaternion _lastLocalRot = Quaternion.identity;
        bool _poseKnown;
        float _poseLoggedAt = -10f;
        string _facingWriter;
        int _facingFrame = -10;

        /// <summary>
        /// Step 17 G: logs every change of the island root's pose in its parent (the tracking space moving it is not a
        /// change) over 2 mm or 0.3 deg, with the writer this frame: a grab, the idle facing turn, the placer, or unknown.
        /// </summary>
        void WatchPose()
        {
            var t = Target;
            if (!_poseKnown)
            {
                _lastLocalPos = t.localPosition;
                _lastLocalRot = t.localRotation;
                _poseKnown = true;
                return;
            }
            float moved = (t.localPosition - _lastLocalPos).magnitude * (t.parent != null ? t.parent.lossyScale.x : 1f);
            float turned = Quaternion.Angle(t.localRotation, _lastLocalRot);
            if (moved < 0.002f && turned < 0.3f || Time.unscaledTime - _poseLoggedAt < 0.5f) return; // a smooth move logs every 0.5 s, summed
            _poseLoggedAt = Time.unscaledTime;
            string who = IsGrabbed ? "grab" : _facingFrame >= Time.frameCount - 1 ? _facingWriter
                : _poseWriterFrame >= Time.frameCount - 1 ? _poseWriter : "unknown";
            // A grab moves it every frame: log grabs only in a dev readout, not the console.
            if (who != "grab")
            {
                var e = (Quaternion.Inverse(_lastLocalRot) * t.localRotation).eulerAngles;
                Debug.Log($"[IslandHandle] Root moved {moved * 1000f:0} mm, turned {turned:0.0} deg (pitch {Mathf.DeltaAngle(0f, e.x):0.0}, yaw {Mathf.DeltaAngle(0f, e.y):0.0}, roll {Mathf.DeltaAngle(0f, e.z):0.0}) by: {who}.");
            }
            _lastLocalPos = t.localPosition;
            _lastLocalRot = t.localRotation;
        }

        // ---- modes ----

        /// <summary>Battles (encounter start to zoom-out), the lodge and (14h) a pod ready to evolve: the rim pods can be grabbed.</summary>
        public bool PodsActive => encounters != null && encounters.InBattle || pods != null && (pods.InBattle || pods.LodgeMode || pods.DueSlot >= 0);

        /// <summary>Switches the pod strip's grab shell with the mode. Never mid-grab: a held island keeps its zone until let go.</summary>
        void ApplyMode(bool force)
        {
            bool active = PodsActive;
            if (!force && (active == _podsActive || IsGrabbed)) return;
            if (!force || !_modeLogged) Debug.Log($"[IslandHandle] Mode: {(active ? "pods active (grab the corners)" : "explore (grab the corners or the front lip)")}.");
            _modeLogged = true;
            _podsActive = active;
            if (podStripColliders != null)
                foreach (var c in podStripColliders)
                    if (c != null) c.enabled = !active;
        }

        // ---- pinch: grab or select ----

        /// <summary>Which island handle a point is on (step 14a).</summary>
        public enum Zone { None, Corner, Lip }

        /// <summary>Step 14j: the grab zone's shape now (the frame line, the island's scale, the mode).</summary>
        public GrabZone.Shape ZoneShape
        {
            get
            {
                var t = island.transform;
                return new GrabZone.Shape
                {
                    half = island.FootprintHalfSize,
                    capTop = island.CapTopLocal,
                    lineOutset = frame != null ? frame.outset : 0.008f,
                    lineRadius = frame != null ? frame.cornerRadius : 0.03f,
                    scale = Mathf.Max(1e-4f, t.lossyScale.x),
                    outsideWorld = grabPastLine,
                    insideWorld = grabInward,
                    bandTop = island.CapTopLocal + grabAboveCap / Mathf.Max(1e-4f, t.lossyScale.x),
                    bandBottom = grabBandBottom,
                    cornerSize = cornerSize,
                    lipTop = t.InverseTransformPoint(new Vector3(t.position.x, PodLineY - lipBelowPods, t.position.z)).y,
                    lipActive = !_podsActive,
                };
            }
        }

        public Zone ZoneAt(Vector3 p)
        {
            if (island == null || NearPod(p)) return Zone.None;
            var k = GrabZone.Classify(island.transform.InverseTransformPoint(p), ZoneShape);
            return k == GrabZone.Kind.Corner ? Zone.Corner : k == GrabZone.Kind.Lip ? Zone.Lip : Zone.None;
        }

        /// <summary>World height of the rim pods' centre line (the cap top when there are no pods).</summary>
        public float PodLineY
        {
            get
            {
                if (pods != null && pods.podPrefab != null) return pods.HomeWorld(0).y;
                return island != null ? island.transform.TransformPoint(new Vector3(0f, island.CapTopLocal, 0f)).y : 0f;
            }
        }

        bool NearPod(Vector3 p)
        {
            if (pods == null || pods.podPrefab == null || island == null) return false;
            float r = pods.podPrefab.radius * island.transform.lossyScale.x + podClearance;
            for (int i = 0; i < Party.Size; i++)
            {
                var pod = pods.Pod(i);
                if (pod != null && pod.isActiveAndEnabled && GrabZone.PodWins(p, pod.transform.position, Mathf.Max(r, pod.WorldRadius + podClearance))) return true;
            }
            // Step 17 A2: the cradle pod sits just off the near-right corner (an always-grab handle) and was missed here,
            // so a pinch on it grabbed the island. It wins within cradleClearance of its glass.
            var cradle = pods.Pod(PodRack.CradleSlot);
            if (cradle != null && cradle.isActiveAndEnabled && GrabZone.PodWins(p, cradle.transform.position, cradle.WorldRadius + cradleClearance)) return true;
            return false;
        }

        // ---- step 17: grab locks ----

        readonly Dictionary<string, string> _locks = new Dictionary<string, string>();
        bool _rimLockedOff;

        /// <summary>
        /// Step 17 (A2, G14): island grab off while any lock is held (a battle, a hand puzzle, a hand reaching for a pod).
        /// A grab already in progress is never cut; the lock takes effect at its release. Recenter and placement still move it.
        /// </summary>
        public void SetGrabLock(string key, bool on, string why = null)
        {
            if (on == _locks.ContainsKey(key)) return;
            if (on) _locks[key] = why ?? key;
            else _locks.Remove(key);
            Debug.Log($"[IslandHandle] Grab lock '{key}' {(on ? "on" : "off")}{(why != null ? $" ({why})" : "")}: island grab {(GrabLocked ? "off" : "on")}.");
        }

        /// <summary>Some lock holds: no new island grab can start.</summary>
        public bool GrabLocked => _locks.Count > 0;

        public string GrabLockReasons => string.Join(", ", _locks.Values);

        /// <summary>Applies the locks to the rim interactable (never while it is held).</summary>
        void ApplyLocks()
        {
            // Nov 15 (Mugesh: still not grabbable in a battle): the step 33 'battle' lock (any hand within 12 cm of a pod) is
            // gone. The catch pod sits at the near-right corner, so it held that corner off all battle. Pods still win where
            // it matters: a pinch on or next to a pod (NearPod) and a hand reaching a pod (PodRack's podReach guard).
            if (rimInteractable == null) return;
            bool off = GrabLocked && !IsGrabbed;
            if (off == _rimLockedOff) return;
            if (!off && IsGrabbed) return;
            _rimLockedOff = off;
            rimInteractable.enabled = !off;
        }

        /// <summary>Distance (world m) to the nearest handle zone (0 inside it): the frame lights by it.</summary>
        public float DistanceToHandles(Vector3 p)
        {
            if (island == null) return float.MaxValue;
            return GrabZone.DistanceToHandles(island.transform.InverseTransformPoint(p), ZoneShape);
        }

        /// <summary>The front lip is a handle now (Explore mode).</summary>
        public bool LipActive => !_podsActive;

        /// <summary>Between the thumb and index tips (where a pinch closes).</summary>
        public static bool TryGetPinchPoint(IHand hand, out Vector3 point)
        {
            point = default;
            if (!HandLookup.TryGetJoint(hand, HandJointId.HandThumbTip, out var thumb) || !HandLookup.TryGetJoint(hand, HandJointId.HandIndexTip, out var index)) return false;
            point = (thumb + index) * 0.5f;
            return true;
        }

        /// <summary>Updates one hand's pinch state once per frame (whoever asks first: the ISDK filter, a gaze system or Update).</summary>
        void Resolve(int h)
        {
            ref var s = ref _pinch[h];
            if (s.frame == Time.frameCount) return;
            s.frame = Time.frameCount;
            var hand = HandRegistry.Get(h == 0 ? Handedness.Left : Handedness.Right);
            bool tracked = hand != null && hand.IsTrackedDataValid;
            Vector3 pinchPoint = default, palmPoint = default;
            bool hasPinch = tracked && TryGetPinchPoint(hand, out pinchPoint);
            bool hasPalm = tracked && HandLookup.TryGetJoint(hand, HandJointId.HandPalm, out palmPoint);
            // Step 14j: a pinch is judged by its pinch point only; the palm counts only for a palm grab (index not
            // pinching). Step 14a: only the handles (corners, front lip); a pod in reach of the pinch point always wins.
            bool pinchy = hasPinch && hand.GetFingerPinchStrength(HandFinger.Index) >= pinchStrengthForPoint;
            var zp = hasPinch ? ZoneAt(pinchPoint) : Zone.None;
            var zh = hasPalm && !pinchy ? ZoneAt(palmPoint) : Zone.None;
            bool podFirst = hasPinch && NearPod(pinchPoint) || hasPalm && !pinchy && NearPod(palmPoint);
            bool locked = GrabLocked;
            bool corner = !locked && !podFirst && (zp == Zone.Corner || zh == Zone.Corner);
            bool inZone = corner || !locked && !podFirst && (zp == Zone.Lip || zh == Zone.Lip);
            if (inZone && !s.inZone) s.zoneSince = Time.time;
            s.inZone = inZone;
            bool pinching = tracked && hand.GetIndexFingerIsPinching();
            if (!s.pinching || !pinching)
            {
                // Free, or a pinch starting now: decide (the decision holds while the pinch lasts). Only a gaze target
                // away from the hand competes (looking at the edge you grab is not a look + pinch elsewhere).
                bool gaze = hasPinch && GazeTarget.TryGet(out var gazePoint) && (gazePoint - pinchPoint).magnitude > gazeAwayDistance;
                float inFor = inZone ? Time.time - s.zoneSince : 0f;
                s.grab = corner || inZone && (!gaze || inFor >= grabHoverSeconds);
                if (pinching && (Application.isEditor || Debug.isDebugBuild))
                {
                    string side = h == 0 ? "L" : "R";
                    s.last = s.grab ? $"pinch {side}: GRAB ({(corner ? "corner" : $"front lip {inFor:0.00} s")}{(gaze ? ", gaze on a target away from the hand" : "")})"
                           : locked ? $"pinch {side}: select (island grab locked: {GrabLockReasons})"
                           : inZone ? $"pinch {side}: select (gaze on a target away from the hand, on the lip only {inFor:0.00} s < {grabHoverSeconds:0.00})"
                           : podFirst ? $"pinch {side}: select (a pod is in reach: pods win)"
                           : $"pinch {side}: select (not on a handle: {(hasPinch ? DistanceToHandles(pinchPoint) * 100f : -1f):0.0} cm away)";
                    Debug.Log($"[IslandHandle] {s.last}, {(_podsActive ? "pods active" : "explore")}.");
                }
            }
            s.pinching = pinching;
        }

        /// <summary>ISDK interactor filter on the rim grab: may this interactor's hand grab the island now?</summary>
        public bool Filter(GameObject interactor)
        {
            if (!_interactorSide.TryGetValue(interactor, out int h))
            {
                var grab = interactor.GetComponent<IHandGrabInteractor>();
                h = grab != null && grab.Hand != null ? grab.Hand.Handedness == Handedness.Left ? 0 : 1 : -1;
                _interactorSide[interactor] = h;
            }
            if (IsHandGrabbing(h == 0)) return true;
            if (GrabLocked) return false;
            if (h < 0) return true;
            Resolve(h);
            return _pinch[h].grab;
        }

        /// <summary>Step 14j: this hand's pinch point (or palm, for a palm grab) is in the grab zone now: the frame's bright hover.</summary>
        public bool InGrabZone(bool leftHand)
        {
            int h = leftHand ? 0 : 1;
            Resolve(h);
            return _pinch[h].inZone;
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

        /// <summary>
        /// True while this hand holds the island, or its pinch (or the pinch it would make now) is an island grab:
        /// gaze selects and microgestures ignore it.
        /// </summary>
        public bool IsHandBusy(bool leftHand)
        {
            if (IsHandGrabbing(leftHand)) return true;
            if (_heldElsewhere[leftHand ? 0 : 1] >= Time.frameCount - 1) return true;
            int h = leftHand ? 0 : 1;
            Resolve(h);
            return _pinch[h].grab;
        }

        static readonly int[] _heldElsewhere = { -10, -10 };

        /// <summary>
        /// A hand is holding something else on the island this frame (step 9b: a puzzle's log or pump handle): the gaze
        /// walk, tile touch, shrines and microgestures ignore it, like a hand holding the island. Call every frame.
        /// </summary>
        public static void MarkHandHeld(bool leftHand) => _heldElsewhere[leftHand ? 0 : 1] = Time.frameCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetHeld() => _heldElsewhere[0] = _heldElsewhere[1] = -10;

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
