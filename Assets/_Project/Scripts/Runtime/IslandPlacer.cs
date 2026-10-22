using System;
using System.Collections;
using Meta.XR.MRUtilityKit;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Diorama
{
    /// <summary>
    /// Places the island in front of the seated user at about waist height, facing them. The island's +Z
    /// faces the user. Keeps the user's current island scale. Re-place: PalmSummon, or press R in the Editor.
    /// Moving, turning and resizing by hand is IslandHandle.
    /// Startup: waits until the rig's centre eye has a real pose (over Link, OVRPlugin can report the head as
    /// tracked while the camera is still at the rig origin on the floor), places, and places again once the
    /// MRUK room has loaded (world locking can shift the rig) unless the user has already moved the island.
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

        Transform _head;
        bool _userMoved;

        /// <summary>The rig's centre eye (falls back to Camera.main outside the Meta rig).</summary>
        public Transform Head
        {
            get
            {
                if (_head != null) return _head;
                var rig = FindFirstObjectByType<OVRCameraRig>();
                if (rig != null && rig.centerEyeAnchor != null) _head = rig.centerEyeAnchor;
                else if (Camera.main != null) _head = Camera.main.transform;
                return _head;
            }
        }

        IEnumerator Start()
        {
            ApplyScale();
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

            if (MRUK.Instance != null) MRUK.Instance.RegisterSceneLoadedCallback(OnSceneLoaded);
            else Debug.Log("[IslandPlacer] No MRUK in the scene; no re-place after room load.");
        }

        void OnSceneLoaded()
        {
            if (_userMoved) Debug.Log("[IslandPlacer] MRUK room loaded; island already moved by the user, leaving it.");
            else Place("after MRUK room load");
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

        /// <summary>Called by IslandHandle when the user grabs the island, so automatic re-placing stops.</summary>
        public void MarkUserMoved() => _userMoved = true;

        /// <summary>Places the island in front of the user at waist height.</summary>
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
            Vector3 toUser = Flat(head.position - pos);
            island.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(toUser, Vector3.up));
            Status = $"waist, eye y {head.position.y:0.00} m";
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
