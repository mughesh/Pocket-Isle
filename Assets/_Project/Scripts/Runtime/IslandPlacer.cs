using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Diorama
{
    /// <summary>
    /// Places the island in front of the seated user at about waist height, facing them. The island's +Z
    /// faces the user. Keeps the user's current island scale. Re-place: PalmSummon, or press R in the Editor.
    /// Moving, turning and resizing by hand is IslandHandle.
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

        [Tooltip("Wait at most this long for head tracking before placing anyway (flat Editor Play has none).")]
        public float trackingTimeout = 3f;

        /// <summary>Short text for debug labels.</summary>
        public string Status { get; private set; } = "starting";

        public event Action Placed;

        Transform Head => Camera.main != null ? Camera.main.transform : null;

        IEnumerator Start()
        {
            ApplyScale();
            // Placing before the first tracked head pose puts the island relative to the rig origin (the floor).
            float t = 0f;
            while (!HeadTracked() && t < trackingTimeout)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            yield return null; // let the camera pick up the tracked pose this frame
            if (!HeadTracked()) Debug.LogWarning("[IslandPlacer] No head tracking after timeout, placing from the current camera pose.");
            Place();
        }

        static bool HeadTracked() => OVRPlugin.initialized && OVRPlugin.GetNodePositionTracked(OVRPlugin.Node.Head);

        void OnValidate()
        {
            if (island != null && Application.isPlaying) ApplyScale();
        }

        void ApplyScale() => island.transform.localScale = Vector3.one * islandScale;

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame) Place();
        }

        /// <summary>Places the island in front of the user at waist height.</summary>
        public void Place()
        {
            if (island == null || Head == null) return;
            Vector3 fwd = Flat(Head.forward);
            Vector3 pos = Head.position + fwd * distance + Vector3.down * belowEye;
            Vector3 toUser = Flat(Head.position - pos);
            island.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(toUser, Vector3.up));
            Status = $"waist, eye y {Head.position.y:0.00} m";
            Debug.Log($"[IslandPlacer] Placed in front at waist height. Head y {Head.position.y:0.00}, island y {pos.y:0.00}.");
            Placed?.Invoke();
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.forward;
        }
    }
}
