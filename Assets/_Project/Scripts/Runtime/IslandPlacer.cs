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
    /// Places the island level, in front of the seated user at about waist height, its +Z facing them.
    /// Keeps the user's current island scale. Re-place: the Quest recenter (hold the Meta button, or the system
    /// menu's recenter), or R in the Editor. Moving, turning and resizing by hand is IslandHandle.
    /// The island is parented to the camera rig's TrackingSpace: MRUK world locking moves that transform after the
    /// room loads, and the island has to move with it or it is left behind (it ended up on the floor).
    /// Startup waits until the centre eye has a real pose; over Link, OVRPlugin can report the head as tracked
    /// while the camera is still at the rig origin.
    /// </summary>
    public class IslandPlacer : MonoBehaviour
    {
        public IslandBuilder island;

        [Tooltip("Uniform island scale. 0.6 makes the 14-tile island about 42 cm wide.")]
        [Range(0.2f, 1.5f)] public float islandScale = 0.6f;

        [Header("Placement")]
        [Tooltip("Horizontal distance from the head to the island centre, in metres.")]
        public float distance = 0.45f;
        [Tooltip("How far below eye height the island sits, in metres. About 0.55 is waist level when seated.")]
        public float belowEye = 0.55f;

        [Tooltip("Eye height above the floor assumed when there is no head pose at all (seated), in metres.")]
        public float seatedEyeHeight = 1.15f;

        [Tooltip("Wait at most this long for a real head pose before placing anyway (flat Editor Play has none).")]
        public float trackingTimeout = 10f;

        /// <summary>Short text for debug labels.</summary>
        public string Status { get; private set; } = "starting";

        public event Action Placed;

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

        IEnumerator Start()
        {
            ApplyScale();
            AttachToTrackingSpace();
            if (OVRManager.display != null) OVRManager.display.RecenteredPose += OnRecentered;

            float t = 0f;
            while (!TryGetHeadPose(out _, out _, out _) && t < trackingTimeout)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            // A few more frames so the pose settles.
            for (int i = 0; i < 5; i++) yield return null;
            LogDiagnostics();
            Place("startup");
        }

        void LogDiagnostics()
        {
            var eye = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
            string xrEye = eye.isValid && eye.TryGetFeatureValue(CommonUsages.centerEyePosition, out var p) ? p.ToString("F2") : "none";
            Debug.Log($"[IslandPlacer] Diagnostics: XR device active {XRSettings.isDeviceActive} ('{XRSettings.loadedDeviceName}'), " +
                      $"OVRManager initialized {OVRManager.OVRManagerinitialized}, HMD present {OVRManager.isHmdPresent}, " +
                      $"CenterEyeAnchor local {(Head != null ? Head.localPosition.ToString("F2") : "none")}, XR input centre eye {xrEye}.");
        }

        /// <summary>
        /// World-space head pose. Tries the rig's CenterEyeAnchor first, then Unity XR input directly (the anchor stayed
        /// at the origin over Link on the office PC). False when neither has a real pose.
        /// </summary>
        bool TryGetHeadPose(out Vector3 position, out Vector3 forward, out string source)
        {
            var head = Head;
            // A tracked centre eye is never exactly at the tracking-space origin.
            if (head != null && head.localPosition.sqrMagnitude > 1e-4f)
            {
                position = head.position;
                forward = head.forward;
                source = "CenterEyeAnchor";
                return true;
            }
            var eye = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
            if (eye.isValid && eye.TryGetFeatureValue(CommonUsages.centerEyePosition, out var p) && p.sqrMagnitude > 1e-4f &&
                eye.TryGetFeatureValue(CommonUsages.centerEyeRotation, out var r))
            {
                var space = Rig != null && Rig.trackingSpace != null ? Rig.trackingSpace : null;
                position = space != null ? space.TransformPoint(p) : p;
                forward = space != null ? space.rotation * (r * Vector3.forward) : r * Vector3.forward;
                source = "XR input";
                return true;
            }
            position = default;
            forward = default;
            source = null;
            return false;
        }

        void OnDestroy()
        {
            if (OVRManager.display != null) OVRManager.display.RecenteredPose -= OnRecentered;
        }

        void AttachToTrackingSpace()
        {
            if (island == null) return;
            if (Rig != null && Rig.trackingSpace != null)
            {
                island.transform.SetParent(Rig.trackingSpace, true);
                Debug.Log("[IslandPlacer] Island parented to the rig's TrackingSpace (moves with MRUK world lock).");
            }
            else Debug.LogWarning("[IslandPlacer] No OVRCameraRig tracking space; island stays in world space.");
        }

        void OnRecentered() => StartCoroutine(PlaceAfterFrames("Quest recenter", 2));

        IEnumerator PlaceAfterFrames(string reason, int frames)
        {
            for (int i = 0; i < frames; i++) yield return null; // the camera picks up the new pose
            Place(reason);
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

        /// <summary>Places the island in front of the user at waist height, level.</summary>
        public void Place() => Place("request");

        void Place(string reason)
        {
            if (island == null)
            {
                Debug.LogError("[IslandPlacer] No island assigned.");
                return;
            }
            Vector3 pos, fwd;
            if (TryGetHeadPose(out var headPos, out var headFwd, out var source))
            {
                fwd = Flat(headFwd);
                pos = headPos + fwd * distance + Vector3.down * belowEye;
                Status = $"waist ({source})";
                Debug.Log($"[IslandPlacer] Placed ({reason}) from {source}. Head {headPos:F2}, island {pos:F2}.");
            }
            else
            {
                // No head pose at all: fixed seated spot in the tracking space (floor-level origin), in front of where
                // the Quest's recenter says forward is. Recentering moves the tracking space, so this follows the user.
                var space = Rig != null && Rig.trackingSpace != null ? Rig.trackingSpace : transform;
                fwd = Flat(space.forward);
                pos = space.TransformPoint(new Vector3(0f, seatedEyeHeight - belowEye, distance));
                Status = "seated default (no head pose)";
                Debug.LogWarning($"[IslandPlacer] Placed ({reason}) at the seated default: no head pose from the rig or XR input. Island {pos:F2}.");
            }
            island.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(-fwd, Vector3.up));
            Placed?.Invoke();
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.forward;
        }
    }
}
