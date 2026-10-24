using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// The gaze ray used for tile walking, battle runes and dialogue bubbles: head forward pitched down by an offset,
    /// because the island usually sits below the chin and aiming the head straight at it strains the neck.
    /// Adaptive (default): the offset is most of the vertical angle between head forward and the direction to the
    /// island centre, clamped and smoothed, so it suits the island at waist height and at eye level.
    /// Without a HeadGaze in the scene the ray falls back to the fixed default offset (and says so once).
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public class HeadGaze : MonoBehaviour
    {
        public const float DefaultOffset = 23f;

        public IslandBuilder island;
        [Tooltip("Fixed downward pitch (degrees) when not adaptive, or when there is no island.")]
        public float offset = DefaultOffset;
        [Tooltip("Pitch toward the island centre instead of a fixed offset.")]
        public bool adaptive = true;
        [Tooltip("Adaptive: share of the angle between head forward and the island centre that is taken up.")]
        [Range(0f, 1f)] public float share = 0.85f;
        [Tooltip("Adaptive: clamp (degrees).")]
        public float maxOffset = 30f;
        [Tooltip("Adaptive: smoothing time (s).")]
        public float smoothSeconds = 0.3f;

        static HeadGaze _instance;
        static bool _warned;
        float _current = DefaultOffset;
        int _frame = -1;

        /// <summary>Current downward pitch of the gaze ray in degrees.</summary>
        public float CurrentOffset => _current;

        void OnEnable() => _instance = this;

        void OnDisable()
        {
            if (_instance == this) _instance = null;
        }

        void Update() => Refresh();

        void Refresh()
        {
            if (_frame == Time.frameCount) return;
            _frame = Time.frameCount;
            var cam = Camera.main;
            float target = offset;
            if (adaptive && cam != null && island != null)
            {
                var head = cam.transform;
                Vector3 toIsland = island.transform.position - head.position;
                float pitchFwd = PitchDown(head.forward), pitchIsland = PitchDown(toIsland);
                target = Mathf.Clamp((pitchIsland - pitchFwd) * share, 0f, maxOffset);
            }
            _current = Mathf.Lerp(_current, target, 1f - Mathf.Exp(-Time.deltaTime * 3f / Mathf.Max(0.01f, smoothSeconds)));
        }

        static float PitchDown(Vector3 dir) => Mathf.Atan2(-dir.y, new Vector2(dir.x, dir.z).magnitude) * Mathf.Rad2Deg;

        /// <summary>The gaze ray in world space (false without a main camera).</summary>
        public static bool TryGetRay(out Ray ray)
        {
            ray = default;
            var cam = Camera.main;
            if (cam == null) return false;
            float pitch = DefaultOffset;
            if (_instance != null)
            {
                _instance.Refresh();
                pitch = _instance._current;
            }
            else if (!_warned)
            {
                _warned = true;
                Debug.LogWarning($"[HeadGaze] No HeadGaze in the scene; gaze uses the fixed {DefaultOffset} deg offset.");
            }
            var head = cam.transform;
            ray = new Ray(head.position, Quaternion.AngleAxis(pitch, head.right) * head.forward);
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _instance = null;
            _warned = false;
        }
    }
}
