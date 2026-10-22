using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

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
            while (!HeadHasPose() && t < trackingTimeout)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            // A few more frames so the pose settles.
            for (int i = 0; i < 5; i++) yield return null;
            if (!HeadHasPose()) Debug.LogWarning($"[IslandPlacer] No head pose after {trackingTimeout:0} s, placing from the current camera pose.");
            Place("startup");
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

        // A tracked centre eye is never exactly at the tracking-space origin.
        bool HeadHasPose() => Head != null && Head.localPosition.sqrMagnitude > 1e-4f;

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
            var head = Head;
            if (head == null)
            {
                Debug.LogError("[IslandPlacer] No OVRCameraRig centre eye or main camera found; can't place the island.");
                return;
            }
            Vector3 fwd = Flat(head.forward);
            Vector3 pos = head.position + fwd * distance + Vector3.down * belowEye;
            island.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(-fwd, Vector3.up));
            Status = $"waist, eye y {head.localPosition.y:0.00} m";
            Debug.Log($"[IslandPlacer] Placed ({reason}). Head {head.position:F2} (local {head.localPosition:F2}), island {pos:F2}.");
            Placed?.Invoke();
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.forward;
        }
    }
}
