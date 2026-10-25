using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// The gaze ray used for tile walking, battle runes and dialogue bubbles: head forward pitched down by an offset,
    /// because the island usually sits below the chin and aiming the head straight at it strains the neck.
    /// Adaptive (default): the offset is most of the vertical angle between head forward and the direction to the
    /// island centre, clamped and smoothed, so it suits the island at waist height and at eye level.
    /// Without a HeadGaze in the scene the ray falls back to the fixed default offset (and says so once).
    /// Picking (step 7c) uses a narrow cone around the ray, not the thin ray: <see cref="ConePick"/>.
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
            if (DebugAim != null && DebugAim() is Vector3 aim) ray = new Ray(head.position, (aim - head.position).normalized);
            return true;
        }

        /// <summary>Editor checks (the Claude bridge): the gaze points at this world point instead. Null = normal.</summary>
        public static System.Func<Vector3?> DebugAim;

        // ---- cone picking ----

        /// <summary>Half angle of the gaze cone (degrees): a target this far off the ray still counts.</summary>
        public const float ConeHalfAngle = 2f;
        /// <summary>Smallest gaze volume of anything selectable: radius in metres at island scale 1 (a 4 cm ball).</summary>
        public const float MinRadius = 0.02f;
        /// <summary>The current target's distance from the ray counts this much when comparing (no flicker between neighbours).</summary>
        public const float Sticky = 0.7f;

        static readonly float ConeTan = Mathf.Tan(ConeHalfAngle * Mathf.Deg2Rad);

        /// <summary>Radius of the cone at <paramref name="distance"/> along the ray (world metres).</summary>
        public static float ConeRadius(float distance) => Mathf.Max(0f, distance) * ConeTan;

        /// <summary>
        /// Does the gaze cone touch a ball of <paramref name="radius"/> round <paramref name="centre"/> (a sphere cast
        /// sized by distance)? <paramref name="off"/> = the centre's distance from the ray (lower = nearer the middle).
        /// </summary>
        public static bool InCone(Ray ray, Vector3 centre, float radius, out float off)
        {
            Vector3 to = centre - ray.origin;
            float along = Vector3.Dot(to, ray.direction);
            off = float.MaxValue;
            if (along <= 0f) return false;
            off = (to - ray.direction * along).magnitude;
            return off <= radius + along * ConeTan;
        }

        /// <summary>
        /// Picks one of several targets with the gaze cone: among the ones it touches, the one nearest the ray centre;
        /// the current target gets the <see cref="Sticky"/> bonus. Use as a local: new, Consider each, read Best.
        /// </summary>
        public struct ConePick
        {
            readonly Ray _ray;
            readonly int _current;
            float _bestOff;
            public int Best;

            public ConePick(Ray ray, int current)
            {
                _ray = ray;
                _current = current;
                _bestOff = float.MaxValue;
                Best = -1;
            }

            /// <summary>Target <paramref name="id"/> (not -1): a ball round <paramref name="centre"/>, radius world metres.</summary>
            public bool Consider(int id, Vector3 centre, float radius)
            {
                if (!InCone(_ray, centre, radius, out float off)) return false;
                if (id == _current) off *= Sticky;
                if (off >= _bestOff) return false;
                _bestOff = off;
                Best = id;
                return true;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _instance = null;
            _warned = false;
            DebugAim = null;
        }
    }
}
