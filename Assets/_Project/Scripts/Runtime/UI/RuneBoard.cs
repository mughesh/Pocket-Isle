using System;
using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using Oculus.Interaction.Surfaces;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Diorama
{
    /// <summary>
    /// A board of stone runes over the island's near rim, facing the head (step 9g: title, game menu, settings, the Quick
    /// Journey's end). Built like <see cref="BattleRunes"/>, the same look and the same two ways to press: poke a rune with
    /// a fingertip (ISDK PokeInteractable, the plate presses in) or look at it and pinch either hand. The looked-at rune
    /// grows with a gold border (gold = focus); <see cref="DefaultFocus"/> is gold while nothing is looked at. A
    /// <see cref="Rune.hold"/> rune needs the pinch or the poke kept for <see cref="holdSeconds"/> (its plate fills gold).
    /// Runs on unscaled time: it works while <see cref="GamePause"/> has the game stopped.
    /// Layout is board-local metres (x right, y up from the bottom row, island-local scale); the board's front faces the head.
    /// Editor: 1-9 press the runes in the order they were added (held for hold runes), Enter the looked-at or focused one.
    /// </summary>
    public class RuneBoard : MonoBehaviour
    {
        public IslandBuilder island;
        [Tooltip("Pinches from a hand that is grabbing or touching the rim are ignored.")]
        public IslandHandle grabGuard;
        public Material hudMaterial;

        [Header("Layout (island-local metres)")]
        [Tooltip("Board origin (the bottom row's centre) relative to the middle of the near rim, at the cap height.")]
        public Vector3 offset = new Vector3(0f, 0.05f, 0.02f);
        public float fontSize = 0.11f;

        [Header("Facing")]
        [Tooltip("The board faces the head when shown, then drifts at most this far (degrees, yaw).")]
        public float maxDrift = 15f;
        public float driftSpeed = 1.5f;
        public float maxTilt = 40f;

        [Header("Press")]
        public float pressDepth = 0.008f;
        [Tooltip("Hold runes (start over) need the pinch or poke kept this long (s).")]
        public float holdSeconds = 2f;

        public class Rune
        {
            public string label;
            public Action picked;
            public bool hold;
            /// <summary>Lit (a toggle that is on, a lit volume pebble).</summary>
            public bool selected;
            public Color tint = new Color(0.55f, 0.58f, 0.56f);
            public Vector2 center, size;
            public float radius = 0.3f;
            internal Transform root, visual;
            internal MeshRenderer plate;
            internal TextMeshPro text;
            internal PokeInteractable poke;
            internal float hover, appear, appearAt, fill;
            internal bool poked;

            public void SetLabel(string text)
            {
                if (this.text != null && this.text.text != text) this.text.text = text;
            }
        }

        public static readonly Color Gold = new Color(1f, 0.85f, 0.45f);
        static readonly int BackColorId = Shader.PropertyToID("_BackColor");
        static readonly int BorderColorId = Shader.PropertyToID("_BorderColor");
        static readonly int FillColorId = Shader.PropertyToID("_FillColor");
        static readonly int FillId = Shader.PropertyToID("_Fill");
        static readonly int InsetId = Shader.PropertyToID("_Inset");
        static readonly int AspectId = Shader.PropertyToID("_Aspect");
        static readonly int RadiusId = Shader.PropertyToID("_Radius");
        static readonly int BorderId = Shader.PropertyToID("_Border");

        /// <summary>Boards showing right now (the bridge presses runes on them).</summary>
        static readonly List<RuneBoard> Showing = new List<RuneBoard>();

        readonly List<Rune> _runes = new List<Rune>();
        readonly List<GameObject> _extras = new List<GameObject>();
        Transform _root;
        MeshRenderer _reticle;
        Mesh _quad;
        MaterialPropertyBlock _block;
        Rune _hovered, _holding;
        int _holdHand = -1;
        readonly bool[] _wasPinching = { true, true };
        float _baseYaw, _yaw, _tilt;
        Vector2 _min, _max;

        /// <summary>Gold while nothing is looked at (Continue on the title); null = nothing is gold until looked at.</summary>
        public Rune DefaultFocus { get; set; }

        public bool IsShown => _root != null && _root.gameObject.activeSelf;

        public IReadOnlyList<Rune> Runes => _runes;

        void Awake()
        {
            if (hudMaterial == null) Debug.LogError($"[RuneBoard] {name}: no HUD material.", this);
            _quad = BattleHud.VerticalQuad();
            _block = new MaterialPropertyBlock();
            _root = new GameObject("Board").transform;
            _root.SetParent(transform, false);
            var reticleGo = new GameObject("Gaze Reticle");
            reticleGo.transform.SetParent(_root, false);
            reticleGo.transform.localScale = Vector3.one * 0.008f;
            reticleGo.AddComponent<MeshFilter>().sharedMesh = _quad;
            _reticle = reticleGo.AddComponent<MeshRenderer>();
            _reticle.sharedMaterial = hudMaterial;
            _reticle.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _block.SetColor(BackColorId, new Color(1f, 1f, 1f, 0.55f));
            _block.SetColor(BorderColorId, new Color(1f, 1f, 1f, 0.9f));
            _block.SetFloat(FillId, 0f);
            _block.SetFloat(AspectId, 1f);
            _block.SetFloat(RadiusId, 0.5f);
            _block.SetFloat(BorderId, 0.12f);
            _reticle.SetPropertyBlock(_block);
            _root.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            Showing.Remove(this);
            if (_quad != null) Destroy(_quad);
        }

        void OnDisable() => Showing.Remove(this);

        // ---- building ----

        /// <summary>Removes every rune, label and backing.</summary>
        public void Clear()
        {
            foreach (var r in _runes)
                if (r.root != null) Destroy(r.root.gameObject);
            foreach (var go in _extras)
                if (go != null) Destroy(go);
            _runes.Clear();
            _extras.Clear();
            _hovered = _holding = null;
            _holdHand = -1;
            DefaultFocus = null;
            _min = _max = Vector2.zero;
        }

        /// <summary>A rune at <paramref name="center"/> (board-local metres). <paramref name="sub"/> = a small second line.</summary>
        public Rune Add(string label, Vector2 center, Vector2 size, Action picked, string sub = null)
        {
            var r = new Rune { label = label, picked = picked, center = center, size = size };
            var root = new GameObject($"Rune {label}").transform;
            root.SetParent(_root, false);
            root.localPosition = new Vector3(center.x, center.y, 0f);
            r.root = root;
            BuildPoke(r);
            r.text = MakeText(r.visual, fontSize * Mathf.Clamp(size.y / 0.04f, 0.6f, 1.2f));
            r.text.rectTransform.sizeDelta = new Vector2(size.x * 1.1f, size.y);
            r.text.transform.localPosition = new Vector3(0f, 0f, -0.001f);
            r.SetLabel(string.IsNullOrEmpty(sub) ? $"<b>{label}</b>" : $"<b>{label}</b>\n<size=75%>{sub}</size>");
            _runes.Add(r);
            Grow(center, size);
            return r;
        }

        /// <summary>A plain caption (row names, titles, the credits card).</summary>
        public TextMeshPro AddText(string text, Vector2 center, float font, TextAlignmentOptions align = TextAlignmentOptions.Center, float width = 0.3f)
        {
            var t = MakeText(_root, font);
            t.alignment = align;
            t.rectTransform.sizeDelta = new Vector2(width, font * 0.5f);
            t.transform.localPosition = new Vector3(center.x, center.y, -0.001f);
            t.text = text;
            _extras.Add(t.gameObject);
            return t;
        }

        /// <summary>A dark stone slab behind the runes (settings, credits).</summary>
        public MeshRenderer AddBacking(Vector2 center, Vector2 size)
        {
            var go = new GameObject("Backing");
            go.transform.SetParent(_root, false);
            go.transform.localPosition = new Vector3(center.x, center.y, 0.004f);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            go.AddComponent<MeshFilter>().sharedMesh = _quad;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = hudMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.sortingOrder = -1;
            _block.Clear();
            _block.SetColor(BackColorId, new Color(0.16f, 0.17f, 0.16f, 0.9f));
            _block.SetColor(BorderColorId, new Color(0.62f, 0.6f, 0.52f, 0.85f));
            _block.SetFloat(FillId, 0f);
            _block.SetFloat(AspectId, size.x / Mathf.Max(1e-4f, size.y));
            _block.SetFloat(RadiusId, 0.08f);
            _block.SetFloat(BorderId, 0.012f / Mathf.Max(1e-4f, size.y));
            r.SetPropertyBlock(_block);
            _extras.Add(go);
            Grow(center, size);
            return r;
        }

        void Grow(Vector2 center, Vector2 size)
        {
            _min = Vector2.Min(_min, center - size * 0.5f);
            _max = Vector2.Max(_max, center + size * 0.5f);
        }

        /// <summary>The poke button like the battle runes': a plane clipped to the plate, set back by the press depth.</summary>
        void BuildPoke(Rune r)
        {
            var surfaceGo = new GameObject("Poke Surface");
            surfaceGo.transform.SetParent(r.root, false);
            surfaceGo.transform.localPosition = new Vector3(0f, 0f, pressDepth);
            var plane = surfaceGo.AddComponent<PlaneSurface>();
            plane.InjectAllPlaneSurface(PlaneSurface.NormalFacing.Backward, false);
            var clipper = surfaceGo.AddComponent<BoundsClipper>();
            clipper.Size = new Vector3(r.size.x, r.size.y, 0.02f);
            var clipped = surfaceGo.AddComponent<ClippedPlaneSurface>();
            clipped.InjectAllClippedPlaneSurface(plane, new IBoundsClipper[] { clipper });
            var poke = surfaceGo.AddComponent<PokeInteractable>();
            poke.InjectAllPokeInteractable(clipped);
            poke.WhenStateChanged += args => OnPokeState(r, args);
            r.poke = poke;

            var visual = new GameObject("Visual").transform;
            visual.SetParent(r.root, false);
            var press = visual.gameObject.AddComponent<PokeInteractableVisual>();
            press.InjectAllPokeInteractableVisual(poke, surfaceGo.transform);
            r.visual = visual;

            var plateGo = new GameObject("Plate");
            plateGo.transform.SetParent(visual, false);
            plateGo.transform.localScale = new Vector3(r.size.x, r.size.y, 1f);
            plateGo.AddComponent<MeshFilter>().sharedMesh = _quad;
            r.plate = plateGo.AddComponent<MeshRenderer>();
            r.plate.sharedMaterial = hudMaterial;
            r.plate.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void OnPokeState(Rune r, InteractableStateChangeArgs args)
        {
            if (!IsShown || r.appear < 0.6f) return;
            if (args.NewState == InteractableState.Select)
            {
                if (!r.hold) Pick(r, "poke");
                else
                {
                    r.poked = true;
                    StartHold(r, -1);
                }
            }
            else if (args.PreviousState == InteractableState.Select) r.poked = false;
        }

        // ---- show, hide ----

        /// <summary>Shows the board facing the head; the runes pop in one after another.</summary>
        public void Show()
        {
            bool was = IsShown;
            _root.gameObject.SetActive(true);
            if (!Showing.Contains(this)) Showing.Add(this);
            for (int i = 0; i < _runes.Count; i++)
            {
                var r = _runes[i];
                if (!was) r.appear = 0f;
                r.appearAt = Time.unscaledTime + i * 0.05f;
                r.fill = 0f;
            }
            _hovered = _holding = null;
            for (int h = 0; h < 2; h++) _wasPinching[h] = true; // a pinch already held doesn't count
            Place(true);
            Paint();
        }

        public void Hide()
        {
            Showing.Remove(this);
            if (_root != null) _root.gameObject.SetActive(false);
            _hovered = _holding = null;
        }

        // ---- every frame ----

        void Update()
        {
            if (!IsShown) return;
            float dt = Time.unscaledDeltaTime;
            Place(false);
            _hovered = GazeHover();
            foreach (var r in _runes)
            {
                r.hover = Mathf.MoveTowards(r.hover, r == _hovered || _hovered == null && r == DefaultFocus ? 1f : 0f, dt * 8f);
                bool popping = Time.unscaledTime >= r.appearAt;
                r.appear = Mathf.MoveTowards(r.appear, popping ? 1f : 0f, dt / 0.25f);
                r.visual.localScale = Vector3.one * (Mathf.Max(0.001f, EaseOutBack(r.appear)) * (1f + 0.1f * r.hover));
            }
            Keys();
            Pinches();
            UpdateHold(dt);
            Paint();
        }

        void Keys()
        {
            if (!Application.isEditor) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            Key[] digits = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };
            for (int i = 0; i < digits.Length && i < _runes.Count; i++)
                if (kb[digits[i]].wasPressedThisFrame) Press(_runes[i], "key");
            var focus = _hovered ?? DefaultFocus;
            if (focus != null && kb.enterKey.wasPressedThisFrame) Press(focus, "gaze + Enter");
            // Hold runes by key: the key stays down.
            if (_holding != null && _holdHand == -2)
            {
                int i = _runes.IndexOf(_holding);
                bool down = i >= 0 && i < digits.Length && kb[digits[i]].isPressed || kb.enterKey.isPressed;
                if (!down) StopHold();
            }
        }

        void Press(Rune r, string how)
        {
            if (r.hold) StartHold(r, -2);
            else Pick(r, how);
        }

        void Pinches()
        {
            for (int h = 0; h < 2; h++)
            {
                var hand = HandRegistry.Get(h == 0 ? Handedness.Left : Handedness.Right);
                bool pinching = hand != null && hand.IsTrackedDataValid && hand.GetIndexFingerIsPinching();
                bool busy = grabGuard != null && grabGuard.IsHandBusy(h == 0);
                if (pinching && !_wasPinching[h] && !busy && _hovered != null)
                {
                    if (_hovered.hold) StartHold(_hovered, h);
                    else Pick(_hovered, $"gaze + pinch ({(h == 0 ? "left" : "right")})");
                }
                if (!pinching && _holdHand == h) StopHold();
                _wasPinching[h] = pinching;
                if (!IsShown) return;
            }
        }

        void StartHold(Rune r, int hand)
        {
            if (_holding != null && _holding != r) StopHold();
            _holding = r;
            _holdHand = hand;
        }

        void StopHold()
        {
            if (_holding != null) _holding.fill = 0f;
            _holding = null;
            _holdHand = -1;
        }

        void UpdateHold(float dt)
        {
            if (_holding == null) return;
            var r = _holding;
            // A pinch hold keeps counting while the rune stays looked at; a poke hold while the finger stays pressed in.
            bool kept = _holdHand == -2 || (_holdHand >= 0 ? _hovered == r : r.poked);
            r.fill = HoldStep(r.fill, kept, dt, holdSeconds);
            if (!kept)
            {
                StopHold();
                return;
            }
            if (r.fill >= 1f)
            {
                StopHold();
                Pick(r, "hold");
            }
        }

        /// <summary>Hold-to-confirm (pure, tested): fills toward 1 over <paramref name="seconds"/> while kept; letting go empties it.</summary>
        public static float HoldStep(float fill, bool kept, float dt, float seconds) =>
            kept ? Mathf.MoveTowards(fill, 1f, dt / Mathf.Max(0.1f, seconds)) : 0f;

        void Pick(Rune r, string how)
        {
            if (!IsShown || !_runes.Contains(r)) return;
            Debug.Log($"[RuneBoard] {name}: {r.label} ({how}).");
            r.poked = false;
            r.picked?.Invoke();
        }

        /// <summary>
        /// Keeps the board above the middle of the near rim. It faces the head when shown, then only drifts within
        /// <see cref="maxDrift"/>, and pitches back so its face points at the eyes (as the battle runes do).
        /// </summary>
        void Place(bool snap)
        {
            if (island == null || island.map == null) return;
            var b = island.LocalBounds;
            _root.localPosition = new Vector3(b.center.x, island.map.capHeight, b.max.z) + offset;
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 away = island.transform.InverseTransformDirection(_root.position - cam.transform.position);
                float below = -away.y;
                away.y = 0f;
                if (away.sqrMagnitude > 1e-8f)
                {
                    float headYaw = Mathf.Atan2(away.x, away.z) * Mathf.Rad2Deg;
                    float tilt = Mathf.Clamp(Mathf.Atan2(below, away.magnitude) * Mathf.Rad2Deg, 0f, maxTilt);
                    if (snap)
                    {
                        _baseYaw = _yaw = headYaw;
                        _tilt = tilt;
                    }
                    float target = _baseYaw + Mathf.Clamp(Mathf.DeltaAngle(_baseYaw, headYaw), -maxDrift, maxDrift);
                    float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime * driftSpeed);
                    _yaw = Mathf.LerpAngle(_yaw, target, k);
                    _tilt = Mathf.Lerp(_tilt, tilt, k);
                }
            }
            _root.rotation = island.transform.rotation * Quaternion.Euler(_tilt, _yaw, 0f);
        }

        Rune GazeHover()
        {
            if (!HeadGaze.TryGetRay(out var ray))
            {
                _reticle.enabled = false;
                return null;
            }
            Vector3 o = _root.InverseTransformPoint(ray.origin);
            Vector3 d = _root.InverseTransformDirection(ray.direction);
            if (d.z <= 1e-4f)
            {
                _reticle.enabled = false;
                return null;
            }
            Vector3 hit = o + d * (-o.z / d.z);
            float scale = _root.lossyScale.x;
            bool near = hit.x > _min.x - 0.05f && hit.x < _max.x + 0.05f && hit.y > _min.y - 0.04f && hit.y < _max.y + 0.04f;
            _reticle.enabled = near;
            if (near) _reticle.transform.localPosition = new Vector3(hit.x, hit.y, -0.002f / Mathf.Max(1e-4f, scale));
            // Gaze cone: each plate grows by the cone's radius at the board; the nearest centre wins, the hovered one stickier.
            float cone = HeadGaze.ConeRadius((_root.TransformPoint(hit) - ray.origin).magnitude) / Mathf.Max(1e-4f, scale);
            Rune best = null;
            float bestScore = float.MaxValue;
            foreach (var r in _runes)
            {
                if (r.appear < 0.3f) continue;
                Vector2 p = new Vector2(hit.x, hit.y) - r.center;
                float hx = r.size.x * 0.5f, hy = r.size.y * 0.5f;
                // Small runes (volume pebbles) get a smaller cone so neighbours stay apart.
                float c = Mathf.Min(cone, Mathf.Min(hx, hy) * 0.8f);
                if (Mathf.Abs(p.x) > hx + c || Mathf.Abs(p.y) > hy + c) continue;
                float score = Mathf.Max(Mathf.Abs(p.x) / hx, Mathf.Abs(p.y) / hy) * (r == _hovered ? HeadGaze.Sticky : 1f);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = r;
                }
            }
            return best;
        }

        void Paint()
        {
            foreach (var r in _runes)
            {
                if (r.plate == null) continue;
                float lit = r.selected ? 1f : 0f;
                var back = Color.Lerp(new Color(0.07f, 0.09f, 0.08f), r.tint, 0.38f + 0.3f * lit + 0.2f * r.hover);
                back.a = 0.92f;
                var border = Color.Lerp(Color.Lerp(r.tint, Color.white, 0.25f * lit), Gold, r.hover);
                border.a = 0.75f + 0.25f * Mathf.Max(lit, r.hover);
                _block.Clear();
                _block.SetColor(BackColorId, back);
                _block.SetColor(BorderColorId, border);
                _block.SetColor(FillColorId, new Color(Gold.r, Gold.g, Gold.b, 0.75f));
                _block.SetFloat(FillId, r.fill);
                _block.SetFloat(InsetId, 0.12f);
                _block.SetFloat(AspectId, r.size.x / Mathf.Max(1e-4f, r.size.y));
                _block.SetFloat(RadiusId, r.radius);
                _block.SetFloat(BorderId, 0.07f + 0.06f * r.hover);
                r.plate.SetPropertyBlock(_block);
            }
        }

        // ---- helpers ----

        static float EaseOutBack(float t)
        {
            const float c = 1.7f;
            t -= 1f;
            return 1f + (c + 1f) * t * t * t + c * t * t;
        }

        static TextMeshPro MakeText(Transform parent, float size)
        {
            // Made active and parented afterwards: under a hidden board TMP never wakes and has no material to outline.
            var go = new GameObject("Text");
            var t = go.AddComponent<TextMeshPro>();
            t.fontSize = size;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.color = Color.white;
            t.outlineWidth = 0.2f;
            t.outlineColor = new Color32(20, 30, 25, 255);
            t.rectTransform.sizeDelta = new Vector2(0.3f, 0.04f);
            go.transform.SetParent(parent, false);
            return t;
        }

        // ---- bridge (Editor checks) ----

        /// <summary>Presses the first shown rune whose label starts with <paramref name="label"/> (hold runes too).</summary>
        public static bool DebugPress(string label)
        {
            foreach (var board in Showing.ToArray())
                foreach (var r in board._runes)
                    if (r.label.StartsWith(label, StringComparison.OrdinalIgnoreCase))
                    {
                        board.Pick(r, "bridge");
                        return true;
                    }
            Debug.Log($"[RuneBoard] No shown rune '{label}'.");
            return false;
        }

        /// <summary>The labels of every shown rune (bridge).</summary>
        public static string DebugList()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var board in Showing)
            {
                sb.Append(board.name).Append(':');
                foreach (var r in board._runes) sb.Append(" [").Append(r.label).Append(r.selected ? "*" : "").Append(']');
                sb.Append('\n');
            }
            return sb.Length > 0 ? sb.ToString() : "(no board shown)";
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Showing.Clear();
    }
}
