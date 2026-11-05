using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using InputDevices = UnityEngine.XR.InputDevices;
using CommonUsages = UnityEngine.XR.CommonUsages;

namespace Diorama
{
    /// <summary>
    /// Places the island level, in front of the seated user, its +Z facing them (the rule: <see cref="IslandPlacement"/>).
    /// Keeps the user's current island scale. Re-place: the Quest recenter, the menu's Recenter, or R in the Editor.
    /// Moving, turning and resizing by hand is IslandHandle. Nothing else ever moves the island on its own.
    /// The island is parented to the camera rig's TrackingSpace: MRUK world locking moves that transform after the
    /// room loads, and the island has to move with it or it is left behind (it ended up on the floor).
    /// Step 14a: every placement waits for a settled head (0.5 s within 1 cm and 1.5 deg, TrackingSpace still too;
    /// startup at most <see cref="startupSettleMax"/> s, recenters at most 2 s). A recenter is any of: OVRDisplay's
    /// RecenteredPose (only raised on a standalone headset, never over Link), OVRManager.TrackingOriginChangePending
    /// (the OpenXR reference space change), a TrackingSpace jump (> 5 cm / 5 deg in a frame: world lock) or a head jump
    /// in tracking space (> 20 cm / 30 deg in a frame). Safety net: 1 s after a recenter placement, if the island is
    /// more than 40 deg off the head's yaw or 0.8 m away, it is placed once more. Every placement logs head, TrackingSpace,
    /// floor, reason and result (<see cref="LastPlacement"/> goes into bug reports).
    /// </summary>
    public class IslandPlacer : MonoBehaviour
    {
        public IslandBuilder island;

        [Tooltip("Uniform island scale. 0.6 makes the 14-tile island about 42 cm wide.")]
        [Range(0.2f, 1.5f)] public float islandScale = 0.6f;

        [Header("Placement (step 14a)")]
        [Tooltip("Horizontal distance from the head to the island centre, in metres.")]
        public float distance = 0.55f;
        [Tooltip("The island's top sits this far below eye height (m).")]
        public float topBelowEye = 0.30f;
        [Tooltip("...but never higher than this far below the eyes (m)...")]
        public float minBelowEye = 0.15f;
        [Tooltip("...and never lower than this over the floor, when the floor is known (m).")]
        public float minAboveFloor = 0.55f;
        [Tooltip("Old field (pivot below the eyes); step 14a uses Top Below Eye.")]
        [HideInInspector] public float belowEye = 0.35f;

        [Tooltip("Eye height above the floor assumed when there is no head pose at all (seated), in metres.")]
        public float seatedEyeHeight = 1.15f;

        [Tooltip("Wait at most this long for a real head pose before placing anyway (flat Editor Play has none).")]
        public float trackingTimeout = 10f;
        [Tooltip("At startup, wait at most this long for the head to settle once tracked (s).")]
        public float startupSettleMax = 3f;
        [Tooltip("After a recenter, wait at most this long for the head to settle (s).")]
        public float recenterSettleMax = 2f;
        [Tooltip("Step 16 (Mugesh: the island blinked out and back on a Meta recenter): after the settle it glides to the new spot this long (s; 0 = jump).")]
        public float recenterGlideSeconds = 0.4f;
        Coroutine _glide;

        /// <summary>Short text for debug labels.</summary>
        public string Status { get; private set; } = "starting";

        /// <summary>The last placement, for bug reports (head, TrackingSpace, floor, reason, result).</summary>
        public static string LastPlacement { get; private set; } = "none yet";

        public event Action Placed;

        /// <summary>The island has been placed at least once (the opening waits for this).</summary>
        public bool HasPlaced { get; private set; }

        OVRCameraRig _rig;
        Transform _head;

        /// <summary>The rig's centre eye (falls back to Camera.main outside the Meta rig).</summary>
        public Transform Head
        {
            get
            {
                if (_head != null) return _head;
                if (Rig != null && Rig.centerEyeAnchor != null) _head = Rig.centerEyeAnchor;
                else if (Camera.main != null) _head = Camera.main.transform;
                return _head;
            }
        }

        OVRCameraRig Rig => _rig != null ? _rig : _rig = FindFirstObjectByType<OVRCameraRig>();
        Transform Space => Rig != null && Rig.trackingSpace != null ? Rig.trackingSpace : null;

        public IslandPlacement.Rule Rule => new IslandPlacement.Rule
            { distance = distance, topBelowEye = topBelowEye, minBelowEye = minBelowEye, minAboveFloor = minAboveFloor };

        readonly IslandPlacement.SettleTracker _settle = new IslandPlacement.SettleTracker();
        Coroutine _placing;
        bool _starting = true;
        // Jump detection (step 14a): last frame's poses.
        Vector3 _lastSpacePos, _lastHeadLocal;
        Quaternion _lastSpaceRot, _lastHeadLocalRot;
        bool _haveLast;

        IEnumerator Start()
        {
            ApplyScale();
            AttachToTrackingSpace();
            if (OVRManager.display != null) OVRManager.display.RecenteredPose += OnRecenteredPose;
            OVRManager.TrackingOriginChangePending += OnOriginChange;

            float t = 0f;
            while (!TryGetHeadPose(out _, out _, out _, out _) && t < trackingTimeout)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            LogDiagnostics();
            yield return WaitSettled(startupSettleMax, "startup");
            Place("startup");
            _starting = false;
        }

        void OnDestroy()
        {
            if (OVRManager.display != null) OVRManager.display.RecenteredPose -= OnRecenteredPose;
            OVRManager.TrackingOriginChangePending -= OnOriginChange;
        }

        void LogDiagnostics()
        {
            var eye = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
            string xrEye = eye.isValid && eye.TryGetFeatureValue(CommonUsages.centerEyePosition, out var p) ? p.ToString("F2") : "none";
            Debug.Log($"[IslandPlacer] Diagnostics: XR device active {XRSettings.isDeviceActive} ('{XRSettings.loadedDeviceName}'), " +
                      $"OVRManager initialized {OVRManager.OVRManagerinitialized}, HMD present {OVRManager.isHmdPresent}, " +
                      $"tracking origin {(OVRManager.instance != null ? OVRManager.instance.trackingOriginType.ToString() : "no OVRManager")}, " +
                      $"CenterEyeAnchor local {(Head != null ? Head.localPosition.ToString("F2") : "none")}, XR input centre eye {xrEye}.");
        }

        /// <summary>
        /// World-space head pose. Tries the rig's CenterEyeAnchor first, then Unity XR input directly (the anchor stayed
        /// at the origin over Link on the office PC). False when neither has a real pose.
        /// </summary>
        bool TryGetHeadPose(out Vector3 position, out Vector3 forward, out Vector3 up, out string source)
        {
            var head = Head;
            // A tracked centre eye is never exactly at the tracking-space origin.
            if (head != null && head.localPosition.sqrMagnitude > 1e-4f)
            {
                position = head.position;
                forward = head.forward;
                up = head.up;
                source = "CenterEyeAnchor";
                return true;
            }
            var eye = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
            if (eye.isValid && eye.TryGetFeatureValue(CommonUsages.centerEyePosition, out var p) && p.sqrMagnitude > 1e-4f &&
                eye.TryGetFeatureValue(CommonUsages.centerEyeRotation, out var r))
            {
                var space = Space;
                position = space != null ? space.TransformPoint(p) : p;
                var rot = space != null ? space.rotation * r : r;
                forward = rot * Vector3.forward;
                up = rot * Vector3.up;
                source = "XR input";
                return true;
            }
            position = forward = up = default;
            source = null;
            return false;
        }

        /// <summary>World head pose (position, forward, up) from the rig or XR input; false when there is none.</summary>
        public bool TryHeadPose(out Vector3 position, out Vector3 forward, out Vector3 up) => TryGetHeadPose(out position, out forward, out up, out _);

        /// <summary>World floor height when known: the rig tracks from the floor (TrackingSpace y) and the head is above it.</summary>
        public float? FloorY
        {
            get
            {
                var space = Space;
                var ovr = OVRManager.instance;
                if (ovr == null || space == null || !XRSettings.isDeviceActive) return null;
                if (ovr.trackingOriginType != OVRManager.TrackingOrigin.FloorLevel && ovr.trackingOriginType != OVRManager.TrackingOrigin.Stage) return null;
                // A floor-level head is at least 0.5 m up (lying down is about that); lower means the origin is not the floor.
                if (Head == null || Head.localPosition.y < 0.5f) return null;
                return space.position.y;
            }
        }

        void AttachToTrackingSpace()
        {
            if (island == null) return;
            if (Space != null)
            {
                island.transform.SetParent(Space, true);
                Debug.Log("[IslandPlacer] Island parented to the rig's TrackingSpace (moves with MRUK world lock).");
            }
            else Debug.LogWarning("[IslandPlacer] No OVRCameraRig tracking space; island stays in world space.");
        }

        // ---- recenter signals ----

        void OnRecenteredPose() => Recentered("OVRDisplay RecenteredPose");

        void OnOriginChange(OVRManager.TrackingOrigin origin, OVRPose? pose) =>
            Recentered($"tracking origin change ({origin}{(pose.HasValue ? $", moved {pose.Value.position.magnitude:F2} m / {pose.Value.orientation.eulerAngles.y:0} deg" : "")})");

        float _lastRecenter = -10f;

        void Recentered(string signal)
        {
            // Several signals arrive for one recenter: the first starts the wait, the rest within 0.5 s are logged only.
            bool repeat = Time.unscaledTime - _lastRecenter < 0.5f && _placing != null;
            _lastRecenter = Time.unscaledTime;
            Debug.Log($"[IslandPlacer] Recenter signal: {signal}{(repeat ? " (same recenter)" : "")}. {PoseText()}.");
            if (repeat || _starting) return;
            GameAudio.Ui(Diorama.Audio.AudioIds.IslandRecenter);
            if (_placing != null) StopCoroutine(_placing);
            _placing = StartCoroutine(PlaceWhenSettled($"recenter: {signal}", recenterSettleMax, true));
        }

        void LateUpdate()
        {
            var space = Space;
            var head = Head;
            if (space == null || head == null) return;
            // Jumps only count with a live headset: in flat Play the seat camera moves the "head" itself, and following it
            // re-placed the island every frame (it climbed out of sight).
            if (_haveLast && !_starting && XRSettings.isDeviceActive && Time.unscaledTime - _lastRecenter > 2f)
            {
                float spaceMove = (space.position - _lastSpacePos).magnitude, spaceTurn = Quaternion.Angle(space.rotation, _lastSpaceRot);
                bool tracked = head.localPosition.sqrMagnitude > 1e-4f && _lastHeadLocal.sqrMagnitude > 1e-4f;
                float headMove = (head.localPosition - _lastHeadLocal).magnitude, headTurn = Quaternion.Angle(head.localRotation, _lastHeadLocalRot);
                if (spaceMove > 0.05f || spaceTurn > 5f) Recentered($"TrackingSpace jump {spaceMove * 100f:0} cm / {spaceTurn:0} deg");
                else if (tracked && (headMove > 0.2f || headTurn > 30f)) Recentered($"head jump {headMove * 100f:0} cm / {headTurn:0} deg in tracking space");
            }
            _lastSpacePos = space.position;
            _lastSpaceRot = space.rotation;
            _lastHeadLocal = head.localPosition;
            _lastHeadLocalRot = head.localRotation;
            _haveLast = true;
        }

        // ---- placing ----

        string PoseText()
        {
            var head = Head;
            var space = Space;
            string h = head != null ? $"head {head.position:F2} yaw {head.eulerAngles.y:0} pitch {Mathf.DeltaAngle(0f, head.eulerAngles.x):0} (local {head.localPosition:F2})" : "no head";
            string t = space != null ? $"TrackingSpace {space.position:F2} yaw {space.eulerAngles.y:0}" : "no TrackingSpace";
            var floor = FloorY;
            return $"{h}, {t}, floor {(floor.HasValue ? floor.Value.ToString("F2") : "unknown")}";
        }

        IEnumerator WaitSettled(float max, string why)
        {
            _settle.Reset();
            float t = 0f;
            while (t < max)
            {
                var head = Head;
                if (head == null) break;
                var space = Space;
                if (_settle.Feed(Time.unscaledTime, head.position, head.rotation,
                        space != null ? space.position : Vector3.zero, space != null ? space.rotation : Quaternion.identity)) break;
                yield return null;
                t += Time.unscaledDeltaTime;
            }
            Debug.Log($"[IslandPlacer] {why}: head {(t >= max ? $"never settled in {max:0.#} s (placing anyway)" : $"settled after {t:0.00} s")}.");
        }

        IEnumerator PlaceWhenSettled(string reason, float max, bool safetyNet)
        {
            yield return WaitSettled(max, reason);
            Place(reason);
            _placing = null;
            if (!safetyNet) yield break;
            // Safety net: TrackingSpace or a late origin change can still carry the island away after placing.
            yield return new WaitForSecondsRealtime(1f);
            if (island == null || !TryGetHeadPose(out var hp, out var hf, out var hu, out _)) yield break;
            var to = island.transform.position - hp;
            to.y = 0f;
            float off = IslandPlacement.YawOff(hp, hf, hu, island.transform.position);
            bool wrong = IslandPlacement.Misplaced(hp, hf, hu, island.transform.position);
            Debug.Log($"[IslandPlacer] Recenter +1 s: island {to.magnitude:F2} m away, {off:0} deg off the head's yaw{(wrong ? ": placing again" : "")}.");
            if (wrong && _placing == null) Place(reason + " (safety net)");
        }

        void OnValidate()
        {
            if (island != null && Application.isPlaying) ApplyScale();
        }

        void ApplyScale() => island.transform.localScale = Vector3.one * islandScale;

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame) Place("R key");
        }

        /// <summary>The menu's Recenter: places from a settled head (at most 1 s wait).</summary>
        public void Place()
        {
            if (_placing != null) StopCoroutine(_placing);
            _placing = StartCoroutine(PlaceWhenSettled("menu recenter", 1f, false));
        }

        /// <summary>The island's top over its pivot at the user's scale (world m).</summary>
        float TopAbovePivot => island != null ? island.CapTopLocal * islandScale : 0f;

        /// <summary>Where the rule puts the island for the current head pose (false: no head pose).</summary>
        public bool TrySpot(out Pose pose, out float eyeY)
        {
            if (island == null || !TryGetHeadPose(out var hp, out var hf, out var hu, out _))
            {
                pose = default;
                eyeY = 0f;
                return false;
            }
            pose = IslandPlacement.Compute(hp, hf, hu, FloorY, TopAbovePivot, Rule);
            eyeY = hp.y;
            return true;
        }

        void Place(string reason)
        {
            if (island == null)
            {
                Debug.LogError("[IslandPlacer] No island assigned.");
                return;
            }
            Pose pose;
            if (TryGetHeadPose(out var headPos, out var headFwd, out var headUp, out var source))
            {
                pose = IslandPlacement.Compute(headPos, headFwd, headUp, FloorY, TopAbovePivot, Rule);
                Status = $"placed ({source})";
            }
            else
            {
                // No head pose at all: fixed seated spot in the tracking space (floor-level origin), in front of where
                // the Quest's recenter says forward is. Recentering moves the tracking space, so this follows the user.
                var space = Space != null ? Space : transform;
                var fwd = IslandPlacement.Facing(space.forward, space.up);
                var p = space.TransformPoint(new Vector3(0f, seatedEyeHeight, 0f)) + fwd * distance;
                p.y = space.TransformPoint(new Vector3(0f, seatedEyeHeight - topBelowEye, 0f)).y - TopAbovePivot;
                pose = new Pose(p, IslandPlacement.FaceHead(p, space.TransformPoint(new Vector3(0f, seatedEyeHeight, 0f)), fwd));
                source = "seated default (no head pose)";
                Status = source;
            }
            if (HasPlaced && recenterGlideSeconds > 0f && reason.Contains("recenter"))
            {
                if (_glide != null) StopCoroutine(_glide);
                _glide = StartCoroutine(GlideTo(pose, reason, source));
                return;
            }
            Apply(pose, reason, source);
        }

        /// <summary>
        /// Step 16: glides from where the island is to <paramref name="pose"/> (in its parent's space, so a tracking space
        /// that moves meanwhile carries both ends), eased, on unscaled time; then places it there exactly.
        /// </summary>
        IEnumerator GlideTo(Pose pose, string reason, string source)
        {
            var t = island.transform;
            var parent = t.parent;
            Vector3 fromPos = t.localPosition;
            Quaternion fromRot = t.localRotation;
            Vector3 toPos = parent != null ? parent.InverseTransformPoint(pose.position) : pose.position;
            Quaternion toRot = parent != null ? Quaternion.Inverse(parent.rotation) * pose.rotation : pose.rotation;
            Debug.Log($"[IslandPlacer] {reason}: gliding {(fromPos - toPos).magnitude:F2} m to the new spot ({recenterGlideSeconds:0.0#} s).");
            for (float e = 0f; e < recenterGlideSeconds; e += Time.unscaledDeltaTime)
            {
                float k = GlideEase(e / recenterGlideSeconds);
                t.localPosition = Vector3.LerpUnclamped(fromPos, toPos, k);
                t.localRotation = Quaternion.SlerpUnclamped(fromRot, toRot, k);
                yield return null;
            }
            _glide = null;
            var world = parent != null ? new Pose(parent.TransformPoint(toPos), parent.rotation * toRot) : new Pose(toPos, toRot);
            Apply(world, reason + " (glided)", source);
        }

        /// <summary>Step 16: the glide's ease (smooth start and stop, 0..1).</summary>
        public static float GlideEase(float u) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u));

        /// <summary>Puts the island at <paramref name="pose"/> (the shard stage's follow uses this) and logs it.</summary>
        public void Apply(Pose pose, string reason, string source = "rule")
        {
            if (island == null) return;
            island.transform.SetPositionAndRotation(pose.position, pose.rotation);
            float top = pose.position.y + TopAbovePivot;
            string eye = Head != null ? $"{Head.position.y - top:F2} m below the eyes" : "";
            LastPlacement = $"{DateTime.Now:HH:mm:ss} {reason} ({source}): island {pose.position:F2} yaw {pose.rotation.eulerAngles.y:0}, top {eye}. {PoseText()}";
            Debug.Log($"[IslandPlacer] Placed: {LastPlacement}.");
            HasPlaced = true;
            Placed?.Invoke();
        }
    }
}
