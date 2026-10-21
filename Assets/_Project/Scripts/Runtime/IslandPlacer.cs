using System;
using System.Collections;
using Meta.XR.MRUtilityKit;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Diorama
{
    /// <summary>
    /// Puts the island on the best real table once MRUK has loaded the room. Until then, or when there
    /// is no table or no scene data, it floats in front of the user. The island's +Z faces the user.
    /// Re-place: pinch both index fingers and hold, or press R.
    /// </summary>
    public class IslandPlacer : MonoBehaviour
    {
        public IslandBuilder island;

        [Tooltip("Uniform island scale. 0.6 makes the 14-tile island about 42 cm wide.")]
        [Range(0.2f, 1.5f)] public float islandScale = 0.6f;

        [Tooltip("Gap between the island's lowest point and the table top, in metres.")]
        public float heightOffset = 0.03f;

        [Header("Fallback (no table)")]
        public float fallbackDistance = 0.45f;
        public float fallbackBelowEye = 0.30f;

        [Header("Table search")]
        [Tooltip("Ignore tables further than this from the user (horizontal metres).")]
        public float maxTableDistance = 2.5f;

        [Header("Re-place")]
        public float replaceHoldSeconds = 1.0f;

        /// <summary>Short text for debug labels, e.g. "table 1.2 x 0.6 m".</summary>
        public string Status { get; private set; } = "starting";

        public event Action Placed;

        Transform Head => Camera.main != null ? Camera.main.transform : null;

        OVRPlugin.HandState _hand;
        float _pinchHeld;
        bool _waitForRelease;

        IEnumerator Start()
        {
            ApplyScale();
            // Give head tracking a moment so the fallback spot is relative to the real head pose.
            yield return new WaitForSeconds(0.3f);
            Place();
            if (MRUK.Instance != null) MRUK.Instance.RegisterSceneLoadedCallback(Place);
            else Status = "fallback (no MRUK in scene)";
        }

        void OnValidate()
        {
            if (island != null && Application.isPlaying) ApplyScale();
        }

        void ApplyScale() => island.transform.localScale = Vector3.one * islandScale;

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame) Place();
            UpdateTwoHandPinch();
        }

        void UpdateTwoHandPinch()
        {
            bool both = OVRPlugin.initialized && IsIndexPinching(OVRPlugin.Hand.HandLeft) && IsIndexPinching(OVRPlugin.Hand.HandRight);
            if (!both)
            {
                _pinchHeld = 0f;
                _waitForRelease = false;
                return;
            }
            if (_waitForRelease) return;
            _pinchHeld += Time.deltaTime;
            if (_pinchHeld >= replaceHoldSeconds)
            {
                _waitForRelease = true;
                Place();
            }
        }

        bool IsIndexPinching(OVRPlugin.Hand hand)
        {
            if (!OVRPlugin.GetHandState(OVRPlugin.Step.Render, hand, ref _hand)) return false;
            bool valid = (_hand.Status & OVRPlugin.HandStatus.HandTracked) != 0 &&
                         (_hand.Status & OVRPlugin.HandStatus.InputStateValid) != 0;
            return valid && (_hand.Pinches & OVRPlugin.HandFingerPinch.Index) != 0;
        }

        /// <summary>Places the island on the best table, or in front of the user if there is none.</summary>
        public void Place()
        {
            if (island == null || Head == null) return;
            ApplyScale();
            if (!TryPlaceOnTable()) PlaceInFront();
            Placed?.Invoke();
        }

        bool TryPlaceOnTable()
        {
            var mruk = MRUK.Instance;
            var room = mruk != null && mruk.IsInitialized ? mruk.GetCurrentRoom() : null;
            if (room == null) return false;

            Vector3 head = Head.position;
            Vector3 look = Flat(Head.forward);
            MRUKAnchor best = null;
            Vector3 bestCenter = default;
            float bestScore = float.MaxValue;

            foreach (var anchor in room.Anchors)
            {
                if (anchor == null || !anchor.HasAnyLabel(MRUKAnchor.SceneLabels.TABLE) || !anchor.PlaneRect.HasValue) continue;
                // Horizontal surfaces face up: MRUK plane normals are the anchor's forward axis.
                if (anchor.transform.forward.y < 0.9f) continue;

                Vector3 center = anchor.transform.TransformPoint(anchor.PlaneRect.Value.center);
                Vector3 to = Flat(center - head);
                float dist = to.magnitude;
                if (dist > maxTableDistance) continue;
                float facing = dist > 0.01f ? Vector3.Dot(look, to / dist) : 1f;
                float score = dist + (1f - facing) * 0.5f; // near, and roughly where the user looks
                if (score < bestScore)
                {
                    bestScore = score;
                    best = anchor;
                    bestCenter = center;
                }
            }
            if (best == null) return false;

            float drop = -island.LocalBounds.min.y * islandScale;
            Vector3 pos = bestCenter + Vector3.up * (drop + heightOffset);
            SetPose(pos, head);
            var size = best.PlaneRect.Value.size;
            Status = $"table {size.x:0.00} x {size.y:0.00} m";
            return true;
        }

        void PlaceInFront()
        {
            Vector3 fwd = Flat(Head.forward);
            Vector3 pos = Head.position + fwd * fallbackDistance + Vector3.down * fallbackBelowEye;
            SetPose(pos, Head.position);
            Status = MRUK.Instance != null && MRUK.Instance.IsInitialized ? "fallback (no table found)" : "fallback (no room data yet)";
        }

        void SetPose(Vector3 position, Vector3 lookFrom)
        {
            Vector3 toUser = Flat(lookFrom - position);
            if (toUser.sqrMagnitude < 1e-6f) toUser = Vector3.back;
            island.transform.SetPositionAndRotation(position, Quaternion.LookRotation(toUser, Vector3.up));
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.forward;
        }
    }
}
