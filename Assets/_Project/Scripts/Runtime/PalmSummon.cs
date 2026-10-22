using Oculus.Interaction.Input;
using TMPro;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// "Bring island here": hold a hand palm-up and still for a moment and a small button appears above the palm.
    /// Touch it with the other hand's index fingertip to re-place the island in front of you.
    /// Confirming is a touch, not a pinch, because palm-toward-you + pinch is Meta's system menu gesture.
    /// </summary>
    public class PalmSummon : MonoBehaviour
    {
        public IslandPlacer placer;
        [Tooltip("Hands grabbing or touching the island rim don't summon.")]
        public IslandHandle handle;
        public Material buttonMaterial;

        [Header("Palm up and still")]
        public float holdSeconds = 1f;
        [Tooltip("Palm counts as still below this speed (m/s).")]
        public float stillSpeed = 0.08f;
        [Tooltip("How closely the palm must face up (dot with world up).")]
        [Range(0f, 1f)] public float palmUpDot = 0.7f;
        [Tooltip("Keep the button this long after the palm turns away, so the other hand can reach it.")]
        public float loseGraceSeconds = 0.35f;

        [Header("Button")]
        public float buttonHeight = 0.09f;
        public float buttonRadius = 0.02f;
        public float cooldownSeconds = 1.5f;

        class HandState
        {
            public Hand hand;
            public float heldFor, lostFor;
            public float speed;
            public Vector3 lastPalm;
            public bool hasLast;
            public bool needsRearm; // after a summon, the palm must turn away before it can summon again
        }

        readonly HandState[] _hands = { new HandState(), new HandState() }; // 0 left, 1 right
        int _shownOn = -1;
        float _cooldown;
        Transform _button;
        TextMeshPro _label;

        void Start()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Summon Button";
            Destroy(go.GetComponent<Collider>());
            if (buttonMaterial != null) go.GetComponent<MeshRenderer>().sharedMaterial = buttonMaterial;
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * buttonRadius * 2f;
            _button = go.transform;

            var labelGo = new GameObject("Summon Label");
            labelGo.transform.SetParent(_button, false);
            labelGo.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            _label = labelGo.AddComponent<TextMeshPro>();
            _label.text = "Touch with your other\nindex finger to\nbring the island here";
            _label.fontSize = 0.12f;
            _label.alignment = TextAlignmentOptions.Center;
            _label.color = Color.white;
            _label.outlineWidth = 0.2f;
            _label.outlineColor = new Color32(20, 30, 25, 255);
            _label.rectTransform.sizeDelta = new Vector2(0.15f, 0.05f);
            labelGo.transform.localScale = Vector3.one / (buttonRadius * 2f); // undo the sphere scale

            _button.gameObject.SetActive(false);
            if (placer == null) Debug.LogError("[PalmSummon] No IslandPlacer assigned.");
        }

        void Update()
        {
            _cooldown -= Time.deltaTime;
            for (int i = 0; i < 2; i++) UpdateHand(i);

            if (_shownOn < 0)
            {
                if (_cooldown > 0f) return;
                for (int i = 0; i < 2; i++)
                    if (_hands[i].heldFor >= holdSeconds) { Show(i); break; }
                return;
            }

            var owner = _hands[_shownOn];
            if (owner.lostFor > loseGraceSeconds) { Hide(); return; }
            if (TryGetPalm(owner.hand, out var palm, out _)) FollowPalm(palm);

            // The other hand's index tip touching the button confirms.
            var other = _hands[1 - _shownOn];
            if (HandLookup.TryGetJoint(other.hand, HandJointId.HandIndexTip, out var tip) &&
                Vector3.Distance(tip, _button.position) <= buttonRadius + 0.02f)
            {
                Debug.Log("[PalmSummon] Bring island here: re-placing in front of the user.");
                if (placer != null) placer.Place();
                Hide();
            }
        }

        void UpdateHand(int i)
        {
            var s = _hands[i];
            var hand = HandLookup.Refresh(ref s.hand, i == 0 ? Handedness.Left : Handedness.Right);
            bool busy = handle != null && handle.IsHandBusy(i == 0);
            if (busy || !TryGetPalm(hand, out var palm, out var palmNormal))
            {
                s.heldFor = 0f;
                s.lostFor += Time.deltaTime;
                s.hasLast = false;
                return;
            }

            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            float speed = s.hasLast ? Vector3.Distance(palm, s.lastPalm) / dt : 0f;
            s.speed = Mathf.Lerp(s.speed, speed, 1f - Mathf.Exp(-dt * 10f));
            s.lastPalm = palm;
            s.hasLast = true;

            bool palmUp = Vector3.Dot(palmNormal, Vector3.up) >= palmUpDot;
            if (palmUp) s.lostFor = 0f;
            else
            {
                s.lostFor += Time.deltaTime;
                s.needsRearm = false;
            }
            s.heldFor = palmUp && !s.needsRearm && s.speed < stillSpeed ? s.heldFor + Time.deltaTime : 0f;
        }

        static bool TryGetPalm(Hand hand, out Vector3 palm, out Vector3 palmNormal)
        {
            palmNormal = default;
            if (!HandLookup.TryGetJoint(hand, HandJointId.HandPalm, out palm) ||
                !HandLookup.TryGetJoint(hand, HandJointId.HandWristRoot, out var wrist) ||
                !HandLookup.TryGetJoint(hand, HandJointId.HandIndex1, out var index1) ||
                !HandLookup.TryGetJoint(hand, HandJointId.HandPinky1, out var pinky1)) return false;
            // Cross gives the back-of-hand normal on the right hand and the palm normal on the left.
            Vector3 n = Vector3.Cross(index1 - wrist, pinky1 - wrist).normalized;
            palmNormal = hand.Handedness == Handedness.Right ? -n : n;
            return true;
        }

        void Show(int handIndex)
        {
            _shownOn = handIndex;
            Debug.Log($"[PalmSummon] Button shown above the {(handIndex == 0 ? "left" : "right")} palm; touch it with the other index finger.");
            if (TryGetPalm(_hands[handIndex].hand, out var palm, out _)) _button.position = palm + Vector3.up * buttonHeight;
            _button.gameObject.SetActive(true);
        }

        void Hide()
        {
            if (_shownOn >= 0)
            {
                _hands[_shownOn].heldFor = 0f;
                _hands[_shownOn].needsRearm = true;
            }
            _shownOn = -1;
            _cooldown = cooldownSeconds;
            _button.gameObject.SetActive(false);
        }

        void FollowPalm(Vector3 palm)
        {
            _button.position = Vector3.Lerp(_button.position, palm + Vector3.up * buttonHeight, 1f - Mathf.Exp(-Time.deltaTime * 15f));
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 to = _button.position - cam.transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 1e-6f) _button.rotation = Quaternion.LookRotation(to, Vector3.up);
        }
    }
}
