using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// The Lantern Spirit companion: hovers above the near-left corner of the island rim (player side), bobbing and
    /// facing the head. A child of the island, so it follows when the island is moved or scaled. While it speaks it
    /// floats up a little and brightens. Its bubble anchor is registered with <see cref="Dialogue"/>.
    /// When a line has a target (<see cref="Dialogue.GetTarget"/>) it flies there in about <see cref="flySeconds"/>
    /// (eased, small arc) and parks beside it (left or right as the player sees it, whichever is more open; never above
    /// or in front of it), shining its lantern on it (<see cref="FocusHighlight"/>), and drifts home
    /// <see cref="lingerSeconds"/> after the line. It never leaves the central <see cref="fovHalfAngle"/> cone of the
    /// head: a target outside it makes the Spirit stop at the edge and lean toward it.
    /// </summary>
    public class LanternSpirit : MonoBehaviour
    {
        public IslandBuilder island;
        [Tooltip("Baked Lantern Spirit prefab (CreatureView).")]
        public GameObject prefab;

        [Tooltip("From the near-left footprint corner, island-local metres (x inward from the left edge, z inward from the near edge).")]
        public Vector3 cornerOffset = new Vector3(0.05f, 0.075f, 0.04f);
        public float bobHeight = 0.006f;
        public float bobSpeed = 1.1f;
        [Tooltip("Extra height while speaking.")]
        public float speakLift = 0.012f;
        [Tooltip("Body brightening while speaking (0..1 towards white).")]
        public float speakBrighten = 0.18f;

        [Header("Flying to a target")]
        public float flySeconds = 0.5f;
        [Tooltip("Arc height of a flight (island-local metres).")]
        public float flyArc = 0.02f;
        [Tooltip("Parking spot beside the target: x sideways as the player sees it, y up, z toward the player (metres at island scale 1). Keep y and z small so it never covers the target.")]
        public Vector3 targetOffset = new Vector3(0.065f, 0.012f, 0f);
        [Tooltip("Stays by the target this long after the line ends (s).")]
        public float lingerSeconds = 1.5f;
        [Tooltip("Half angle of the head cone the Spirit stays inside (degrees).")]
        public float fovHalfAngle = 35f;

        CreatureView _view;
        Transform _model, _anchor;
        float _speak, _speakTarget;
        bool _away, _clamped;
        float _sideSign = 1f;
        Vector3 _targetWorld, _flyFrom, _lastGoal;
        float _flyStart = -999f, _lingerUntil = -999f;

        void Start()
        {
            if (prefab == null)
            {
                Debug.LogError("[LanternSpirit] No prefab assigned (bake LanternSpirit: Tools > Pocket Isle > Dialogue > Set Up MR_Main).", this);
                return;
            }
            var go = Instantiate(prefab, transform, false);
            go.name = "Lantern Spirit Model";
            _model = go.transform;
            _view = go.GetComponent<CreatureView>();
            _anchor = new GameObject("Bubble Anchor").transform;
            _anchor.SetParent(transform, false);
            float top = _view != null ? _view.height * 1.25f : 0.06f;
            _anchor.localPosition = Vector3.up * top;
            Dialogue.RegisterSpeaker(DialogueSpeaker.Spirit, _anchor);
            // The lantern (its glowing glass body): where the focus beam starts.
            var lantern = new GameObject("Lantern").transform;
            lantern.SetParent(go.transform, false);
            lantern.localPosition = Vector3.up * (_view != null ? _view.centerHeight : 0.02f);
            FocusHighlight.SetLantern(lantern);
            if (island != null && island.map != null) transform.localPosition = _lastGoal = HomeLocal();
        }

        void OnEnable() => Dialogue.Speaking += OnSpeaking;
        void OnDisable() => Dialogue.Speaking -= OnSpeaking;

        void OnSpeaking(DialogueSpeaker? speaker) => _speakTarget = speaker == DialogueSpeaker.Spirit ? 1f : 0f;

        void Update()
        {
            if (_model == null || island == null || island.map == null) return;
            _speak = Mathf.MoveTowards(_speak, _speakTarget, Time.deltaTime * 3f);
            UpdateTarget();
            Place(Time.time);
            if (_view != null) _view.SetFlash(speakBrighten * Mathf.SmoothStep(0f, 1f, _speak));
        }

        /// <summary>Active target: go (and keep the linger timer fresh). Lost: home now. None: home after the linger.</summary>
        void UpdateTarget()
        {
            var state = Dialogue.GetTarget(out var world);
            bool away;
            if (state == Dialogue.TargetState.Active)
            {
                _targetWorld = world;
                _lingerUntil = Time.time + lingerSeconds;
                away = true;
            }
            else away = state == Dialogue.TargetState.None && _away && Time.time < _lingerUntil;
            if (away != _away)
            {
                _away = away;
                _flyFrom = transform.localPosition;
                _flyStart = Time.time;
                Debug.Log(away ? "[LanternSpirit] Flying to the line's target." : "[LanternSpirit] Drifting home.");
            }
        }

        void Place(float t)
        {
            Vector3 goal = _away ? TargetLocal(_targetWorld) : HomeLocal();
            // A target that jumps (a pod flies off, a new target) starts a fresh flight from here.
            if (_away && (goal - _lastGoal).magnitude > 0.06f && Time.time - _flyStart > flySeconds)
            {
                _flyFrom = transform.localPosition;
                _flyStart = Time.time;
            }
            _lastGoal = goal;
            float u = Mathf.Clamp01((Time.time - _flyStart) / Mathf.Max(0.05f, flySeconds));
            float k = Mathf.SmoothStep(0f, 1f, u);
            float lift = Mathf.Sin(t * bobSpeed * Mathf.PI * 2f) * bobHeight + speakLift * Mathf.SmoothStep(0f, 1f, _speak);
            Vector3 pos = u < 1f ? Vector3.Lerp(_flyFrom, goal, k) + Vector3.up * (flyArc * Mathf.Sin(u * Mathf.PI)) : goal;
            transform.localPosition = pos + Vector3.up * lift * (u < 1f ? k : 1f);

            var cam = Camera.main;
            if (cam == null) return;
            // Faces the head; at the edge of the view cone it turns toward the target instead (leaning in).
            Vector3 look = _away && _clamped ? _targetWorld - transform.position : cam.transform.position - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.LookRotation(look, Vector3.up) * Quaternion.Euler(_away && _clamped ? 12f : 0f, 0f, 0f);
        }

        /// <summary>The near-left corner of the rim (the player sits on the island's +Z side; their left is +X).</summary>
        Vector3 HomeLocal()
        {
            var b = island.LocalBounds;
            return new Vector3(b.max.x - cornerOffset.x, island.map.capHeight + cornerOffset.y, b.max.z - cornerOffset.z);
        }

        /// <summary>
        /// Beside the target as the player sees it, at about its height, on the more open side: inside the head's view
        /// cone first, then toward the island's middle (so it stays over the island), keeping the side it already took
        /// unless the other is clearly better. Pulled back inside the view cone. Returned in the parent's space.
        /// </summary>
        Vector3 TargetLocal(Vector3 target)
        {
            var parent = transform.parent != null ? transform.parent : island.transform;
            float scale = island.transform.lossyScale.x;
            var cam = Camera.main;
            Vector3 head = cam != null ? cam.transform.position : target + island.transform.forward;
            Vector3 toPlayer = head - target;
            toPlayer.y = 0f;
            toPlayer = toPlayer.sqrMagnitude > 1e-6f ? toPlayer.normalized : island.transform.forward;
            Vector3 side = Vector3.Cross(Vector3.up, toPlayer).normalized;
            Vector3 lift = Vector3.up * targetOffset.y + toPlayer * targetOffset.z;
            float Score(float sign)
            {
                Vector3 w = target + (side * sign * targetOffset.x + lift) * scale;
                float score = Vector3.Dot(side * sign, (island.transform.position - target).normalized); // toward the middle
                if (cam != null && Vector3.Angle(cam.transform.forward, w - head) > fovHalfAngle) score -= 2f;
                if (sign == _sideSign) score += 0.35f; // stay put
                return score;
            }
            _sideSign = Score(1f) >= Score(-1f) ? 1f : -1f;
            Vector3 world = target + (side * _sideSign * targetOffset.x + lift) * scale;

            _clamped = false;
            if (cam != null)
            {
                Vector3 dir = world - head;
                float max = fovHalfAngle * Mathf.Deg2Rad;
                if (Vector3.Angle(cam.transform.forward, dir) > fovHalfAngle)
                {
                    world = head + Vector3.RotateTowards(cam.transform.forward, dir, max, 0f).normalized * dir.magnitude;
                    _clamped = true;
                }
            }
            return parent.InverseTransformPoint(world);
        }
    }
}
