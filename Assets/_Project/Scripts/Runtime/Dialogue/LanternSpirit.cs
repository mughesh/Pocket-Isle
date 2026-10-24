using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// The Lantern Spirit companion: hovers above the near-left corner of the island rim (player side), bobbing and
    /// facing the head. A child of the island, so it follows when the island is moved or scaled. While it speaks it
    /// floats up a little and brightens. Its bubble anchor is registered with <see cref="Dialogue"/>.
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

        CreatureView _view;
        Transform _model, _anchor;
        float _speak, _speakTarget;

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
            Place(0f);
        }

        void OnEnable() => Dialogue.Speaking += OnSpeaking;
        void OnDisable() => Dialogue.Speaking -= OnSpeaking;

        void OnSpeaking(DialogueSpeaker? speaker) => _speakTarget = speaker == DialogueSpeaker.Spirit ? 1f : 0f;

        void Update()
        {
            if (_model == null) return;
            _speak = Mathf.MoveTowards(_speak, _speakTarget, Time.deltaTime * 3f);
            Place(Time.time);
            if (_view != null) _view.SetFlash(speakBrighten * Mathf.SmoothStep(0f, 1f, _speak));
        }

        void Place(float t)
        {
            if (island == null || island.map == null) return;
            // The player sits on the island's +Z side; their left is +X.
            var b = island.LocalBounds;
            float lift = Mathf.Sin(t * bobSpeed * Mathf.PI * 2f) * bobHeight + speakLift * Mathf.SmoothStep(0f, 1f, _speak);
            transform.localPosition = new Vector3(b.max.x - cornerOffset.x, island.map.capHeight + cornerOffset.y + lift, b.max.z - cornerOffset.z);
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 to = cam.transform.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.LookRotation(to, Vector3.up);
        }
    }
}
