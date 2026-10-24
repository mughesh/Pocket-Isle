using System;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using Oculus.Interaction.Surfaces;
using TMPro;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// World-space speech bubble above a speaker: rounded warm-cream panel with a dark outline and a short tail, text
    /// typed in at <see cref="charsPerSecond"/>, held for <see cref="holdBase"/> + <see cref="holdPerChar"/> x characters.
    /// Faces the head, keeps a fixed real-world size (readable at 0.6-0.8 m) and is pulled inside the central view
    /// (<see cref="maxViewAngle"/> from the head's forward). Poke it, or look at it and pinch, to skip to the next line.
    /// </summary>
    public class DialogueBubble : MonoBehaviour
    {
        public Material hudMaterial;
        [Tooltip("Pinches from a hand that is grabbing or touching the rim are ignored.")]
        public IslandHandle grabGuard;

        [Header("Look")]
        public float fontSize = 0.14f;
        [Tooltip("Text wraps at this width (metres).")]
        public float maxWidth = 0.2f;
        public Vector2 padding = new Vector2(0.012f, 0.009f);
        public float heightAbove = 0.045f;
        public Color spiritColor = new Color(1f, 0.965f, 0.89f, 0.97f);
        public Color keeperColor = new Color(0.97f, 0.85f, 0.71f, 0.97f);
        public Color heartColor = new Color(0.86f, 0.92f, 0.84f, 0.97f);
        public Color outlineColor = new Color(0.23f, 0.16f, 0.12f, 1f);

        [Header("Timing")]
        public float charsPerSecond = 45f;
        public float holdBase = 1.2f;
        public float holdPerChar = 0.05f;

        [Header("View")]
        [Tooltip("Half of the central view cone the bubble stays inside (degrees).")]
        public float maxViewAngle = 30f;

        /// <summary>The line has been shown for its full time.</summary>
        public event Action Finished;
        /// <summary>Poke or gaze + pinch.</summary>
        public event Action Skipped;

        /// <summary>The head is looking at the shown bubble (its pinch wins over tile gaze).</summary>
        public bool IsGazed { get; private set; }

        /// <summary>A fingertip is hovering or pressing the shown bubble.</summary>
        public bool IsBeingPoked => _showing && _poke != null && _poke.State != InteractableState.Normal;

        static readonly int BackColorId = Shader.PropertyToID("_BackColor");
        static readonly int BorderColorId = Shader.PropertyToID("_BorderColor");
        static readonly int FillId = Shader.PropertyToID("_Fill");
        static readonly int AspectId = Shader.PropertyToID("_Aspect");
        static readonly int RadiusId = Shader.PropertyToID("_Radius");
        static readonly int BorderId = Shader.PropertyToID("_Border");

        Transform _root, _panel, _tail, _visual;
        MeshRenderer _panelRenderer, _tailRenderer;
        TextMeshPro _text;
        BoundsClipper _clipper;
        PokeInteractable _poke;
        Mesh _quad;
        MaterialPropertyBlock _block;
        Transform _anchor;
        Vector2 _size;
        float _shownAt, _typeSeconds, _holdSeconds, _appear;
        int _chars;
        bool _showing;
        readonly Hand[] _hands = new Hand[2];
        readonly bool[] _wasPinching = new bool[2];

        void Awake()
        {
            if (hudMaterial == null) Debug.LogError("[DialogueBubble] No HUD material assigned.", this);
            _quad = BattleHud.VerticalQuad();
            _block = new MaterialPropertyBlock();
            Build();
            _root.gameObject.SetActive(false);
        }

        void OnDestroy() => Destroy(_quad);

        void Build()
        {
            _root = new GameObject("Bubble").transform;
            _root.SetParent(transform, false);

            // Poke surface at the root (the front faces the head along -Z, so the plane normal faces backward);
            // the visual sits a little in front and presses in.
            var surfaceGo = new GameObject("Poke Surface");
            surfaceGo.transform.SetParent(_root, false);
            surfaceGo.transform.localPosition = new Vector3(0f, 0f, 0.006f);
            var plane = surfaceGo.AddComponent<PlaneSurface>();
            plane.InjectAllPlaneSurface(PlaneSurface.NormalFacing.Backward, false);
            _clipper = surfaceGo.AddComponent<BoundsClipper>();
            var clipped = surfaceGo.AddComponent<ClippedPlaneSurface>();
            clipped.InjectAllClippedPlaneSurface(plane, new IBoundsClipper[] { _clipper });
            _poke = surfaceGo.AddComponent<PokeInteractable>();
            _poke.InjectAllPokeInteractable(clipped);
            _poke.WhenStateChanged += args =>
            {
                if (args.NewState == InteractableState.Select && _showing) Skip("poke");
            };

            _visual = new GameObject("Visual").transform;
            _visual.SetParent(_root, false);
            _visual.gameObject.AddComponent<PokeInteractableVisual>().InjectAllPokeInteractableVisual(_poke, surfaceGo.transform);

            _panel = Quad("Panel", 1, out _panelRenderer);
            _tail = Quad("Tail", 0, out _tailRenderer);
            _tail.localRotation = Quaternion.Euler(0f, 0f, 45f);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(_visual, false);
            textGo.transform.localPosition = new Vector3(0f, 0f, -0.001f);
            _text = textGo.AddComponent<TextMeshPro>();
            _text.fontSize = fontSize;
            _text.alignment = TextAlignmentOptions.Center;
            _text.textWrappingMode = TextWrappingModes.Normal;
            _text.color = outlineColor;
            _text.rectTransform.sizeDelta = new Vector2(maxWidth, 0.2f);
            _text.sortingOrder = 2;
        }

        Transform Quad(string name, int order, out MeshRenderer renderer)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_visual, false);
            go.AddComponent<MeshFilter>().sharedMesh = _quad;
            renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = hudMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = order;
            return go.transform;
        }

        public void Show(DialogueLine line, Transform anchor)
        {
            _anchor = anchor;
            _root.gameObject.SetActive(true);
            _text.text = line.text;
            _text.maxVisibleCharacters = 99999;
            _text.ForceMeshUpdate();
            var pref = _text.GetPreferredValues(line.text, maxWidth, 1f);
            float w = Mathf.Min(maxWidth, pref.x), h = pref.y;
            _text.rectTransform.sizeDelta = new Vector2(w + 0.002f, h + 0.002f);
            _size = new Vector2(w + padding.x * 2f, h + padding.y * 2f);
            _panel.localScale = new Vector3(_size.x, _size.y, 1f);
            float tail = 0.016f;
            _tail.localScale = new Vector3(tail, tail, 1f);
            _tail.localPosition = new Vector3(0f, -_size.y * 0.5f, 0.0005f);
            _clipper.Size = new Vector3(_size.x, _size.y, 0.02f);

            var back = line.speaker == DialogueSpeaker.Keeper ? keeperColor : line.speaker == DialogueSpeaker.Heart ? heartColor : spiritColor;
            Paint(_panelRenderer, back, _size.x / _size.y, Mathf.Min(0.45f, 0.012f / _size.y), 0.0018f / _size.y);
            Paint(_tailRenderer, back, 1f, 0.1f, 0.12f);

            _chars = line.text.Length;
            _text.maxVisibleCharacters = 0;
            _typeSeconds = _chars / Mathf.Max(1f, charsPerSecond);
            _holdSeconds = holdBase + holdPerChar * _chars;
            _shownAt = Time.time;
            _showing = true;
            for (int i = 0; i < 2; i++) _wasPinching[i] = true; // a pinch already held doesn't skip
            Place(true);
        }

        void Paint(MeshRenderer r, Color back, float aspect, float radius, float border)
        {
            _block.Clear();
            _block.SetColor(BackColorId, back);
            _block.SetColor(BorderColorId, outlineColor);
            _block.SetFloat(FillId, 0f);
            _block.SetFloat(AspectId, aspect);
            _block.SetFloat(RadiusId, radius);
            _block.SetFloat(BorderId, Mathf.Clamp(border, 0.02f, 0.3f));
            r.SetPropertyBlock(_block);
        }

        public void Hide()
        {
            _showing = false; // IsGazed clears next frame, so a skipping pinch doesn't also reach tile gaze
            _root.gameObject.SetActive(false);
        }

        void Skip(string how)
        {
            Debug.Log($"[DialogueBubble] Skipped by {how}.");
            _showing = false;
            Skipped?.Invoke();
        }

        void Update()
        {
            if (!_showing)
            {
                IsGazed = false;
                return;
            }
            float t = Time.time - _shownAt;
            _text.maxVisibleCharacters = Mathf.Min(_chars, Mathf.CeilToInt(t * charsPerSecond));
            _appear = Mathf.Min(1f, t / 0.15f);
            Place(false);
            if (t >= _typeSeconds + _holdSeconds)
            {
                _showing = false;
                Finished?.Invoke();
                return;
            }
            GazePinch();
        }

        /// <summary>Above the anchor at a fixed real size, facing the head, inside the central view.</summary>
        void Place(bool snap)
        {
            var cam = Camera.main;
            Vector3 target = _anchor != null ? _anchor.position + Vector3.up * (heightAbove + _size.y * 0.5f) : _root.position;
            if (cam != null)
            {
                Vector3 head = cam.transform.position, fwd = cam.transform.forward, dir = target - head;
                float angle = Vector3.Angle(fwd, dir);
                if (angle > maxViewAngle && dir.sqrMagnitude > 1e-6f)
                    target = head + Vector3.RotateTowards(dir, fwd * dir.magnitude, (angle - maxViewAngle) * Mathf.Deg2Rad, 0f);
            }
            _root.position = snap ? target : Vector3.Lerp(_root.position, target, 1f - Mathf.Exp(-Time.deltaTime * 10f));
            _root.localScale = Vector3.one * Mathf.SmoothStep(0.6f, 1f, _appear);
            if (cam == null) return;
            Vector3 away = _root.position - cam.transform.position;
            if (away.sqrMagnitude > 1e-6f) _root.rotation = Quaternion.LookRotation(away, Vector3.up);
        }

        void GazePinch()
        {
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 o = _root.InverseTransformPoint(cam.transform.position);
            Vector3 d = _root.InverseTransformDirection(cam.transform.forward);
            bool looking = false;
            if (d.z > 1e-4f)
            {
                Vector3 hit = o + d * (-o.z / d.z);
                looking = Mathf.Abs(hit.x) < _size.x * 0.6f && Mathf.Abs(hit.y) < _size.y * 0.8f;
            }
            IsGazed = looking;
            for (int h = 0; h < 2; h++)
            {
                var hand = HandLookup.Refresh(ref _hands[h], h == 0 ? Handedness.Left : Handedness.Right);
                if (hand == null || !hand.IsTrackedDataValid)
                {
                    _wasPinching[h] = false;
                    continue;
                }
                bool pinching = hand.GetIndexFingerIsPinching();
                bool busy = grabGuard != null && grabGuard.IsHandBusy(h == 0);
                if (pinching && !_wasPinching[h] && !busy && looking)
                {
                    _wasPinching[h] = true;
                    Skip($"gaze + pinch ({(h == 0 ? "left" : "right")})");
                    return;
                }
                _wasPinching[h] = pinching;
            }
        }
    }
}
