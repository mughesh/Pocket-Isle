using System;
using System.Collections.Generic;
using Diorama.Audio;
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
    /// Step 14f: every rune is a mossy carved stone tablet (<see cref="TabletKit"/>): looked at it grows with a gold rim and
    /// its label brightens, pressed it sinks with a stone tap, disabled it dims; <see cref="Rune.primary"/> = a gold inlay.
    /// </summary>
    public class RuneBoard : MonoBehaviour
    {
        public IslandBuilder island;
        [Tooltip("Pinches from a hand that is grabbing or touching the rim are ignored.")]
        public IslandHandle grabGuard;
        public Material hudMaterial;
        [Tooltip("Step 14f: Diorama/Tablet (Materials/Tablet), shared by every board and the battle runes.")]
        public Material tabletMaterial;

        [Header("Layout (island-local metres)")]
        [Tooltip("Board origin (the bottom row's centre) relative to the middle of the near rim, at the cap height.")]
        public Vector3 offset = new Vector3(0f, 0.05f, 0.02f);
        public float fontSize = 0.11f;

        [Header("Facing")]
        [Tooltip("The board faces the head when shown, then drifts at most this far (degrees, yaw).")]
        public float maxDrift = 15f;
        public float driftSpeed = 1.5f;
        [Tooltip("Nov 3: the head this far round the island from where the board was faced (a recenter, the island re-placed " +
                 "while it showed) faces it at the head again (degrees).")]
        public float refaceAngle = 35f;
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
            public Color tint = DefaultTint;
            /// <summary>Step 14f: the main action (Resume, Continue): a gold inlay round its face (make it a little larger too).</summary>
            public bool primary;
            /// <summary>Step 14f: shown dimmed, can't be picked.</summary>
            public bool disabled;
            public Vector2 center, size;
            public float radius = 0.3f;
            internal Transform root, visual;
            internal MeshRenderer plate;
            internal TabletKit.View tablet;
            internal TextMeshPro text;
            internal PokeInteractable poke;
            internal float hover, appear, appearAt, fill, press;
            internal bool poked;

            public void SetLabel(string text)
            {
                if (this.text != null && this.text.text != text) this.text.text = text;
            }
        }

        public static readonly Color Gold = new Color(1f, 0.85f, 0.45f);
        public static readonly Color DefaultTint = new Color(0.55f, 0.58f, 0.56f);
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

        /// <summary>
        /// Nov 3: the board floats here (world point, the bottom row's centre) instead of over the near rim: the Quick
        /// Journey's end over the statue, away from the rim the hands rest on. Null = the near rim. Cleared by <see cref="Clear"/>.
        /// </summary>
        public Func<Vector3?> Anchor { get; set; }

        public bool IsShown => _root != null && _root.gameObject.activeSelf;

        public IReadOnlyList<Rune> Runes => _runes;

        void Awake()
        {
            if (hudMaterial == null) Debug.LogError($"[RuneBoard] {name}: no HUD material.", this);
            if (tabletMaterial == null) Debug.LogError($"[RuneBoard] {name}: no tablet material (Set Up MR_Main (14f menus and fixes)): plain plates.", this);
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
            Anchor = null;
            _min = _max = Vector2.zero;
        }

        // ---- step 14f: readable labels, tablets sized to them ----

        /// <summary>Every label's font size (the readable rule, <see cref="TextRule"/>); primary tablets a little bigger.</summary>
        public static float LabelFont => TextRule.IslandFont;
        public const float PrimaryScale = 1.12f;
        /// <summary>A one-line tablet's height, the extra for a second line, the side padding and the gap between tablets.</summary>
        public const float TabletH = 0.062f, LineH = 0.04f, PadX = 0.036f, Gap = 0.016f;

        /// <summary>The tablet size that fits a label (bold) and an optional second line (regular).</summary>
        public static Vector2 Fit(string label, string sub = null, bool primary = false)
        {
            float f = LabelFont * (primary ? PrimaryScale : 1f);
            float w = Mathf.Max(TextRule.Width(label, f, true), string.IsNullOrEmpty(sub) ? 0f : TextRule.Width(sub, LabelFont)) + PadX;
            float h = TabletH * (primary ? PrimaryScale : 1f) + (string.IsNullOrEmpty(sub) ? 0f : LineH);
            return new Vector2(Mathf.Max(w, h * 1.3f), h);
        }

        /// <summary>One entry of a <see cref="Row"/>.</summary>
        public struct Item
        {
            public string label, sub;
            public Action picked;
            public bool primary;
            public Item(string label, Action picked, string sub = null, bool primary = false)
            {
                this.label = label;
                this.picked = picked;
                this.sub = sub;
                this.primary = primary;
            }
        }

        /// <summary>A centred row of tablets at height <paramref name="y"/> (their middles), each sized to its label.</summary>
        public Rune[] Row(float y, params Item[] items)
        {
            var sizes = new Vector2[items.Length];
            float total = -Gap;
            for (int i = 0; i < items.Length; i++)
            {
                sizes[i] = Fit(items[i].label, items[i].sub, items[i].primary);
                total += sizes[i].x + Gap;
            }
            var runes = new Rune[items.Length];
            float x = -total * 0.5f;
            for (int i = 0; i < items.Length; i++)
            {
                runes[i] = Add(items[i].label, new Vector2(x + sizes[i].x * 0.5f, y), sizes[i], items[i].picked, items[i].sub, items[i].primary);
                x += sizes[i].x + Gap;
            }
            return runes;
        }

        /// <summary>The middle of a row of height <paramref name="above"/> stacked over one at <paramref name="y"/> of height <paramref name="below"/>.</summary>
        public static float Above(float y, float below, float above) => y + below * 0.5f + Gap + above * 0.5f;

        /// <summary>The tallest tablet of these labels (a row's height).</summary>
        public static float RowHeight(params Item[] items)
        {
            float h = 0f;
            foreach (var it in items) h = Mathf.Max(h, Fit(it.label, it.sub, it.primary).y);
            return h;
        }

        /// <summary>The width a <see cref="Row"/> of these labels takes.</summary>
        public static float RowWidth(params Item[] items)
        {
            float total = -Gap;
            foreach (var it in items) total += Fit(it.label, it.sub, it.primary).x + Gap;
            return total;
        }

        /// <summary>
        /// A square tablet carrying a little picture instead of words (the speaker): <paramref name="label"/> names it for
        /// the logs and the bridge. The picture is drawn with the tablet material in the label colour.
        /// </summary>
        public Rune AddIcon(string label, Mesh icon, Vector2 center, float side, Action picked)
        {
            var r = Add(label, center, new Vector2(side, side), picked);
            if (r.text != null) r.text.gameObject.SetActive(false);
            if (icon != null && r.tablet != null && tabletMaterial != null)
            {
                var go = new GameObject("Icon");
                go.layer = gameObject.layer;
                go.transform.SetParent(r.tablet.transform, false);
                go.transform.localPosition = new Vector3(0f, 0f, TabletKit.LabelZ);
                go.transform.localScale = Vector3.one * (side / 0.03f);
                go.AddComponent<MeshFilter>().sharedMesh = icon;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = tabletMaterial;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var block = new MaterialPropertyBlock();
                block.SetVector("_Size", Vector4.zero);
                block.SetVector("_State", Vector4.zero);
                var c = TabletKit.LabelColor;
                block.SetColor("_Inlay", new Color(c.r, c.g, c.b, 1f));
                mr.SetPropertyBlock(block);
            }
            return r;
        }

        /// <summary>A rune at <paramref name="center"/> (board-local metres). <paramref name="sub"/> = a second line.</summary>
        public Rune Add(string label, Vector2 center, Vector2 size, Action picked, string sub = null, bool primary = false)
        {
            var r = new Rune { label = label, picked = picked, center = center, size = size, primary = primary };
            var root = new GameObject($"Rune {label}").transform;
            root.SetParent(_root, false);
            root.localPosition = new Vector3(center.x, center.y, 0f);
            r.root = root;
            BuildPoke(r);
            // Step 14f: every label at the readable size (the second line too); the tablet is sized to fit it (Fit).
            r.text = MakeText(r.tablet != null ? r.tablet.transform : r.visual, LabelFont * (primary ? PrimaryScale : 1f));
            r.text.rectTransform.sizeDelta = new Vector2(size.x * 0.96f, size.y);
            r.text.lineSpacing = -12f;
            r.text.transform.localPosition = new Vector3(0f, 0f, -0.001f);
            if (r.tablet != null) TabletKit.StyleLabel(r.text);
            r.SetLabel(string.IsNullOrEmpty(sub) ? $"<b>{label}</b>" : $"<b>{label}</b>\n<size={100f / (primary ? PrimaryScale : 1f):0}%>{sub}</size>");
            _runes.Add(r);
            Grow(center, size);
            return r;
        }

        /// <summary>A plain caption (row names, titles, the credits card).</summary>
        public TextMeshPro AddText(string text, Vector2 center, float font, TextAlignmentOptions align = TextAlignmentOptions.Center, float width = 0.3f)
        {
            // Step 14f: never under the readable size.
            font = TextRule.Island(font);
            var t = MakeText(_root, font);
            t.alignment = align;
            t.rectTransform.sizeDelta = new Vector2(width, font * 0.5f);
            t.transform.localPosition = new Vector3(center.x, center.y, -0.001f);
            t.text = text;
            _extras.Add(t.gameObject);
            return t;
        }

        /// <summary>Step 12a: a flat picture on the board (the Controls card's glyphs), with a Diorama/Glyph material.</summary>
        public MeshRenderer AddImage(Texture texture, Material material, Vector2 center, Vector2 size)
        {
            if (texture == null || material == null) return null;
            var go = new GameObject("Image");
            go.transform.SetParent(_root, false);
            go.transform.localPosition = new Vector3(center.x, center.y, -0.002f);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            go.AddComponent<MeshFilter>().sharedMesh = _quad;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var block = new MaterialPropertyBlock();
            block.SetTexture("_MainTex", texture);
            r.SetPropertyBlock(block);
            _extras.Add(go);
            Grow(center, size);
            return r;
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
            _block.SetColor(BackColorId, new Color(0.19f, 0.2f, 0.19f, 0.97f));
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

            if (tabletMaterial != null)
            {
                r.tablet = TabletKit.Make(visual, tabletMaterial, r.size, Color.clear);
                return;
            }
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

        /// <summary>Shows the board facing the head; the runes pop in one after another, <paramref name="stagger"/> s apart.</summary>
        public void Show(float stagger = 0.05f)
        {
            bool was = IsShown;
            _root.gameObject.SetActive(true);
            if (!Showing.Contains(this)) Showing.Add(this);
            for (int i = 0; i < _runes.Count; i++)
            {
                var r = _runes[i];
                if (!was) r.appear = 0f;
                r.appearAt = Time.unscaledTime + i * stagger;
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
            var was = _hovered;
            _hovered = GazeHover();
            if (_hovered != null && _hovered != was) GameAudio.At(AudioIds.RuneHover, _hovered.visual.position);
            foreach (var r in _runes)
            {
                r.hover = Mathf.MoveTowards(r.hover, r == _hovered || _hovered == null && r == DefaultFocus ? 1f : 0f, dt * 8f);
                bool popping = Time.unscaledTime >= r.appearAt;
                r.appear = Mathf.MoveTowards(r.appear, popping ? 1f : 0f, dt / 0.25f);
                r.visual.localScale = Vector3.one * (Mathf.Max(0.001f, EaseOutBack(r.appear)) * (1f + 0.1f * r.hover));
                r.press = Mathf.MoveTowards(r.press, 0f, dt / 0.3f);
                if (r.tablet != null) r.tablet.Press(r.press > 0.5f ? 1f : r.press * 2f);
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

        AudioHandle _holdSound;

        void StartHold(Rune r, int hand)
        {
            if (_holding != null && _holding != r) StopHold();
            if (_holding != r) _holdSound = GameAudio.At(AudioIds.RuneHold, r.visual.position);
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
                // Let go early: the rising tone stops short of its ting.
                _holdSound.Stop(0.1f);
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
            if (r.disabled)
            {
                Debug.Log($"[RuneBoard] {name}: {r.label} is disabled ({how}).");
                return;
            }
            Debug.Log($"[RuneBoard] {name}: {r.label} ({how}).");
            r.press = 1f;
            if (r.visual != null) GameAudio.At(AudioIds.StepStone, r.visual.position);
            // A hold rune already rang at the end of its fill tone.
            if (!r.hold) GameAudio.At(AudioIds.RuneSelect, r.visual.position);
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
            var at = Anchor?.Invoke();
            _root.localPosition = at.HasValue ? _root.parent.InverseTransformPoint(at.Value) : new Vector3(b.center.x, island.map.capHeight, b.max.z) + offset;
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
                    else if (Mathf.Abs(Mathf.DeltaAngle(_baseYaw, headYaw)) > refaceAngle)
                    {
                        // Nov 3: the island moved under the board (recenter, startup placement): it stayed facing the old
                        // spot, crossways or away. Face the head again (turn there at once when it was far off).
                        Debug.Log($"[RuneBoard] {name}: the head is {Mathf.DeltaAngle(_baseYaw, headYaw):0} deg round from the board's facing: facing it again.");
                        if (Mathf.Abs(Mathf.DeltaAngle(_yaw, headYaw)) > 60f) _yaw = headYaw;
                        _baseYaw = headYaw;
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
                if (r.appear < 0.3f || r.disabled) continue;
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
                if (r.tablet != null)
                {
                    // The inlay: gold on the primary, the tint on a lit toggle or a coloured rune (Dev), else a plain groove.
                    bool tinted = r.tint != DefaultTint;
                    var inlay = r.primary ? TabletKit.GoldInlay : r.selected ? Color.Lerp(r.tint, Gold, 0.5f) : r.tint;
                    inlay.a = r.primary ? 1f : r.selected ? 0.95f : tinted ? 0.7f : 0f;
                    r.tablet.inlay = inlay;
                    r.tablet.Paint(r.hover, r.disabled ? 1f : 0f, r.fill, r.selected ? 0.35f : 0f);
                    var c = Color.Lerp(TabletKit.LabelColor, TabletKit.LabelLit, Mathf.Max(r.hover, r.selected ? 0.6f : 0f));
                    c.a = r.disabled ? 0.5f : 1f;
                    if (r.text != null && r.text.color != c) r.text.color = c;
                    continue;
                }
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
