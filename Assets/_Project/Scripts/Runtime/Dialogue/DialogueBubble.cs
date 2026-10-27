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
    /// While the line has a target (step 7c), the bubble is smaller (<see cref="sideWidth"/> of the width, at most two
    /// lines) and sits on the far side of the Spirit from the target, tail toward the Spirit, pushed further away if it
    /// would touch the target in the player's view.
    /// </summary>
    public class DialogueBubble : MonoBehaviour
    {
        /// <summary>
        /// Things the bubble must not cover in the player's view: each listener adds spheres (xyz world centre, w radius),
        /// the bubble moves up until it clears them (step 8d: the floating starter pods).
        /// </summary>
        public static event Action<System.Collections.Generic.List<Vector4>> KeepClear;

        readonly System.Collections.Generic.List<Vector4> _keep = new System.Collections.Generic.List<Vector4>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => KeepClear = null;

        public Material hudMaterial;
        [Tooltip("Pinches from a hand that is grabbing or touching the rim are ignored.")]
        public IslandHandle grabGuard;
        [Tooltip("The bubble is kept out of this island's solid (step 9e).")]
        public IslandBuilder island;

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

        [Header("Pointing at a target")]
        [Tooltip("Width share of the bubble while the line points at something.")]
        [Range(0.3f, 1f)] public float sideWidth = 0.6f;
        [Tooltip("At most this many lines while pointing (the text shrinks down to 70% to fit).")]
        public int sideMaxLines = 2;
        [Tooltip("Gap between the Spirit and the bubble's edge (metres).")]
        public float sideGap = 0.012f;

        [Header("Timing")]
        public float charsPerSecond = 45f;
        [Tooltip("Step 9h reading pace: a line stays up at least holdBase + holdPerChar x characters after it is typed in.")]
        public float holdBase = 1.5f;
        public float holdPerChar = 0.06f;

        [Header("Lesson chip (step 9h)")]
        [Tooltip("A lesson line that stays (pinned) shrinks to its hint chip after this long (s).")]
        public float chipAfter = 6f;
        [Tooltip("Chip text size (share of the font size).")]
        [Range(0.5f, 1f)] public float chipFontScale = 0.85f;
        [Tooltip("The small hand icon in the chip (metres).")]
        public float iconSize = 0.014f;
        [Tooltip("Looking at the chip or the Spirit opens the full line; it shrinks back this long after the look leaves (s).")]
        public float reopenSeconds = 3f;
        [Tooltip("Looking within this angle of the Spirit counts as looking at it (degrees).")]
        public float spiritGazeDegrees = 8f;

        [Header("View")]
        [Tooltip("Half of the central view cone the bubble stays inside (degrees).")]
        public float maxViewAngle = 30f;
        [Tooltip("Step 9h: the bubble's bottom stays this far above the island's top (world metres) wherever the island is between the head and the bubble.")]
        public float aboveIslandMargin = 0.015f;

        /// <summary>The line has been shown for its full time (never for a pinned lesson line).</summary>
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
        bool _pointing;
        float _sideSign = 1f;
        int _chars;
        bool _showing;
        readonly bool[] _wasPinching = new bool[2];
        // Step 9h: a pinned lesson line (never finishes) and its hint chip.
        DialogueLine _line;
        string _chipText;
        bool _chip;
        float _endAt, _lookedAt = -999f, _popStart = -1f;
        Transform _icon;
        MeshRenderer _iconRenderer;
        Mesh _iconMesh;

        /// <summary>The shown line stays until the lesson is done (a chip after a while).</summary>
        public bool IsPinned => _showing && _chipText != null;
        /// <summary>Showing the short chip instead of the full line.</summary>
        public bool IsChip => _showing && _chip;
        public string Text => _showing ? _text.text : null;

        /// <summary>Seconds a line of <paramref name="chars"/> characters stays up after it is typed in.</summary>
        public static float HoldSeconds(int chars, float holdBase, float holdPerChar) => holdBase + holdPerChar * chars;

        void Awake()
        {
            if (hudMaterial == null) Debug.LogError("[DialogueBubble] No HUD material assigned.", this);
            _quad = BattleHud.VerticalQuad();
            _block = new MaterialPropertyBlock();
            Build();
            _root.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            Destroy(_quad);
            if (_iconMesh != null) Destroy(_iconMesh);
        }

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

            // The chip's hand icon: one solid mesh in the HUD material (every uv at the middle = the back colour).
            var iconGo = new GameObject("Chip Hand");
            iconGo.transform.SetParent(_visual, false);
            _iconMesh = HandIconMesh();
            iconGo.AddComponent<MeshFilter>().sharedMesh = _iconMesh;
            _iconRenderer = iconGo.AddComponent<MeshRenderer>();
            _iconRenderer.sharedMaterial = hudMaterial;
            _iconRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _iconRenderer.receiveShadows = false;
            _iconRenderer.sortingOrder = 2;
            _icon = iconGo.transform;
            iconGo.SetActive(false);
        }

        /// <summary>
        /// An open hand (palm, four fingers, thumb) as flat capsules in a 1 x 1 box round the origin, the fingers up,
        /// the thumb to the left. Every uv is (0.5, 0.5) so the HUD shader fills it with the back colour.
        /// </summary>
        static Mesh HandIconMesh()
        {
            var verts = new System.Collections.Generic.List<Vector3>();
            var tris = new System.Collections.Generic.List<int>();
            void Capsule(Vector2 a, Vector2 b, float r)
            {
                const int n = 8;
                Vector2 d = (b - a).sqrMagnitude > 1e-8f ? (b - a).normalized : Vector2.up;
                Vector2 side = new Vector2(-d.y, d.x);
                int c = verts.Count;
                verts.Add((a + b) * 0.5f);
                // Half circle round b, then round a (a closed fan round the middle).
                for (int i = 0; i <= n; i++)
                {
                    float ang = Mathf.PI * i / n;
                    verts.Add(b + (side * Mathf.Cos(ang) + d * Mathf.Sin(ang)) * r);
                }
                for (int i = 0; i <= n; i++)
                {
                    float ang = Mathf.PI * i / n;
                    verts.Add(a - (side * Mathf.Cos(ang) + d * Mathf.Sin(ang)) * r);
                }
                int ring = 2 * (n + 1);
                for (int i = 0; i < ring; i++)
                {
                    tris.Add(c);
                    tris.Add(c + 1 + (i + 1) % ring);
                    tris.Add(c + 1 + i);
                }
            }
            Capsule(new Vector2(-0.1f, -0.22f), new Vector2(0.1f, -0.05f), 0.22f); // palm
            Capsule(new Vector2(-0.19f, 0.02f), new Vector2(-0.2f, 0.3f), 0.075f); // index
            Capsule(new Vector2(-0.05f, 0.04f), new Vector2(-0.05f, 0.42f), 0.075f); // middle
            Capsule(new Vector2(0.09f, 0.03f), new Vector2(0.1f, 0.36f), 0.072f); // ring
            Capsule(new Vector2(0.22f, -0.02f), new Vector2(0.25f, 0.2f), 0.065f); // little
            Capsule(new Vector2(-0.26f, -0.2f), new Vector2(-0.42f, -0.02f), 0.08f); // thumb
            var mesh = new Mesh { name = "Chip Hand Icon" };
            mesh.SetVertices(verts);
            var uv = new Vector2[verts.Count];
            for (int i = 0; i < uv.Length; i++) uv[i] = new Vector2(0.5f, 0.5f);
            mesh.uv = uv;
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
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

        /// <summary>
        /// Shows a line. <paramref name="chip"/> set = a lesson line that stays (pinned): it never finishes, shrinks to
        /// that chip text (with a small hand) after <see cref="chipAfter"/>, and opens again while the chip or the Spirit
        /// is looked at. Poking or pinching it doesn't skip it.
        /// </summary>
        public void Show(DialogueLine line, Transform anchor, string chip = null)
        {
            _anchor = anchor;
            _line = line;
            _chipText = string.IsNullOrEmpty(chip) ? null : chip;
            _popStart = -1f;
            _root.gameObject.SetActive(true);
            Layout(line.text, false);

            _chars = line.text.Length;
            _text.maxVisibleCharacters = 0;
            _typeSeconds = _chars / Mathf.Max(1f, charsPerSecond);
            _holdSeconds = HoldSeconds(_chars, holdBase, holdPerChar);
            _shownAt = Time.time;
            _blipAt = 0;
            _endAt = _shownAt + _typeSeconds + _holdSeconds;
            _showing = true;
            for (int i = 0; i < 2; i++) _wasPinching[i] = true; // a pinch already held doesn't skip
            Place(true);
        }

        /// <summary>Sizes the panel round the full line or the chip (icon + short text).</summary>
        void Layout(string text, bool chip)
        {
            _chip = chip;
            _text.text = text;
            _text.maxVisibleCharacters = 99999;
            _pointing = _line.speaker == DialogueSpeaker.Spirit && Dialogue.GetTarget(out _) == Dialogue.TargetState.Active;
            float width = chip ? maxWidth : _pointing ? maxWidth * sideWidth : maxWidth;
            _text.fontSize = chip ? fontSize * chipFontScale : fontSize;
            var pref = _text.GetPreferredValues(text, width, 1f);
            if (_pointing && !chip)
            {
                // At most sideMaxLines lines: shrink the text a little if it runs longer.
                float lineHeight = _text.GetPreferredValues("Ag", width, 1f).y;
                for (int i = 0; i < 6 && pref.y > lineHeight * (sideMaxLines + 0.4f); i++)
                {
                    _text.fontSize *= 0.94f;
                    lineHeight = _text.GetPreferredValues("Ag", width, 1f).y;
                    pref = _text.GetPreferredValues(text, width, 1f);
                }
            }
            _text.ForceMeshUpdate();
            float w = Mathf.Min(width, pref.x), h = pref.y;
            float icon = chip ? iconSize + padding.x * 0.6f : 0f;
            _text.rectTransform.sizeDelta = new Vector2(w + 0.002f, h + 0.002f);
            _text.transform.localPosition = new Vector3(icon * 0.5f, 0f, -0.001f);
            _size = new Vector2(w + icon + padding.x * 2f, Mathf.Max(h, chip ? iconSize : 0f) + padding.y * 2f);
            _icon.gameObject.SetActive(chip);
            if (chip)
            {
                _icon.localPosition = new Vector3(-(w + icon) * 0.5f + iconSize * 0.5f - padding.x * 0.3f, 0f, -0.001f);
                _icon.localScale = new Vector3(iconSize, iconSize, 1f);
                Paint(_iconRenderer, outlineColor, 1f, 0f, 0f);
            }
            _panel.localScale = new Vector3(_size.x, _size.y, 1f);
            float tail = 0.016f;
            _tail.localScale = new Vector3(tail, tail, 1f);
            PlaceTail();
            _clipper.Size = new Vector3(_size.x, _size.y, 0.02f);

            var back = _line.speaker == DialogueSpeaker.Keeper ? keeperColor : _line.speaker == DialogueSpeaker.Heart ? heartColor : spiritColor;
            Paint(_panelRenderer, back, _size.x / _size.y, Mathf.Min(0.45f, (chip ? 0.5f * _size.y : 0.012f) / _size.y), 0.0018f / _size.y);
            Paint(_tailRenderer, back, 1f, 0.1f, 0.12f);
        }

        /// <summary>
        /// A more important line is waiting (step 9h reading pace): this one ends once it has been up for its minimum
        /// time, not at once. A pinned lesson line isn't affected.
        /// </summary>
        public void FinishSoon()
        {
            if (!_showing || _chipText != null) return;
            _endAt = Mathf.Min(_endAt, Mathf.Max(Time.time, _shownAt + HoldSeconds(_chars, holdBase, holdPerChar)));
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
            bool pop = _showing && _chipText != null;
            _showing = false; // IsGazed clears next frame, so a skipping pinch doesn't also reach tile gaze
            _chipText = null;
            if (pop && _root.gameObject.activeSelf)
            {
                // A lesson's chip pops when the lesson is done.
                _popStart = Time.time;
                return;
            }
            _root.gameObject.SetActive(false);
        }

        void Skip(string how)
        {
            float t = Time.time - _shownAt;
            if (t < _typeSeconds)
            {
                // The first poke or pinch shows the whole line; the next one skips it.
                _shownAt = Time.time - _typeSeconds;
                _endAt = Mathf.Max(_endAt, Time.time + HoldSeconds(_chars, holdBase, holdPerChar) * 0.5f);
                Debug.Log($"[DialogueBubble] {how}: the whole line at once.");
                return;
            }
            if (_chipText != null)
            {
                // A lesson line stays until the lesson is done; on the chip, the poke opens the full line.
                if (_chip) Reopen(how);
                return;
            }
            Debug.Log($"[DialogueBubble] Skipped by {how}.");
            _showing = false;
            Skipped?.Invoke();
        }

        void Reopen(string why)
        {
            _lookedAt = Time.time;
            if (!_chip) return;
            Layout(_line.text, false);
            Debug.Log($"[DialogueBubble] Chip opened ({why}): {_line.text}");
        }

        int _blipAt;

        /// <summary>Audio hookup: a wordless voice blip every 3 typed characters (Spirit, Keeper, Heart each their own).</summary>
        void Blip(int shown)
        {
            if (_chip || shown < _blipAt + 3 || shown >= _chars) return;
            _blipAt = shown;
            string id = _line.speaker == DialogueSpeaker.Keeper ? Diorama.Audio.AudioIds.VoiceKeeper
                : _line.speaker == DialogueSpeaker.Heart ? Diorama.Audio.AudioIds.VoiceHeart : Diorama.Audio.AudioIds.SpiritBlip;
            GameAudio.At(id, _root.position);
        }

        void Update()
        {
            if (_popStart >= 0f)
            {
                float u = (Time.time - _popStart) / 0.18f;
                if (u >= 1f)
                {
                    _popStart = -1f;
                    _root.gameObject.SetActive(false);
                }
                else _root.localScale = Vector3.one * (1f + 0.35f * u) * (1f - u * u);
            }
            if (!_showing)
            {
                IsGazed = false;
                return;
            }
            float t = Time.time - _shownAt;
            _text.maxVisibleCharacters = _chip ? 99999 : Mathf.Min(_chars, Mathf.CeilToInt(t * charsPerSecond));
            Blip(_text.maxVisibleCharacters);
            _appear = Mathf.Min(1f, t / 0.15f);
            Place(false);
            if (_chipText == null && Time.time >= _endAt)
            {
                _showing = false;
                Finished?.Invoke();
                return;
            }
            GazePinch();
            if (_chipText != null) UpdateChip(t);
        }

        /// <summary>Pinned: the full line for <see cref="chipAfter"/>, then the chip; looking at it or the Spirit reopens it.</summary>
        void UpdateChip(float t)
        {
            if (LookingAtSpirit() || IsGazed && _chip) Reopen(IsGazed ? "looked at" : "looked at the Spirit");
            bool chipTime = t >= Mathf.Max(chipAfter, _typeSeconds + 1f) && Time.time - _lookedAt >= reopenSeconds;
            if (chipTime && !_chip)
            {
                Layout(_chipText, true);
                Debug.Log($"[DialogueBubble] Chip: {_chipText}");
            }
        }

        bool LookingAtSpirit()
        {
            if (_anchor == null || !HeadGaze.TryGetRay(out var ray)) return false;
            Vector3 spirit = _anchor.parent != null ? _anchor.parent.position : _anchor.position;
            return Vector3.Angle(ray.direction, spirit - ray.origin) < spiritGazeDegrees;
        }

        /// <summary>Debug (bridge): the chip now, or the full line again.</summary>
        public void DebugChip(bool chip)
        {
            if (!_showing || _chipText == null) return;
            if (chip)
            {
                _lookedAt = -999f;
                Layout(_chipText, true);
            }
            else Reopen("debug");
        }

        /// <summary>Tail under the bubble, or (pointing) on the edge facing the Spirit.</summary>
        void PlaceTail()
        {
            _tail.localPosition = _pointing
                ? new Vector3(-_sideSign * _size.x * 0.5f, -_size.y * 0.15f, 0.0005f)
                : new Vector3(0f, -_size.y * 0.5f, 0.0005f);
        }

        /// <summary>
        /// Pointing: beside the Spirit on the far side from the target (as the player sees it), pushed sideways until it
        /// doesn't touch the target's focus sphere in the view.
        /// </summary>
        Vector3 SidePosition(Camera cam, Vector3 spirit, Vector3 target, float radius)
        {
            Vector3 right = cam.transform.right;
            float d = Vector3.Dot(spirit - target, right);
            if (Mathf.Abs(d) > 0.004f) _sideSign = Mathf.Sign(d);
            Vector3 pos = spirit + right * (_sideSign * (sideGap + _size.x * 0.5f)) + Vector3.up * (_size.y * 0.25f);
            var head = cam.transform;
            for (int i = 0; i < 3; i++)
            {
                Vector3 b = head.InverseTransformPoint(pos), t = head.InverseTransformPoint(target);
                if (b.z < 0.05f || t.z < 0.05f) break;
                // The target at the bubble's depth.
                float k = b.z / t.z;
                Vector2 tp = new Vector2(t.x, t.y) * k;
                float tr = radius * k + 0.006f;
                float dx = Mathf.Abs(tp.x - b.x) - _size.x * 0.5f, dy = Mathf.Abs(tp.y - b.y) - _size.y * 0.5f;
                float ox = Mathf.Max(dx, 0f), oy = Mathf.Max(dy, 0f);
                if (ox * ox + oy * oy >= tr * tr) break;
                pos += right * (_sideSign * (tr - dx + 0.004f));
            }
            return pos;
        }

        /// <summary>Above the anchor at a fixed real size, facing the head, inside the central view.</summary>
        void Place(bool snap)
        {
            var cam = Camera.main;
            Vector3 target = _anchor != null ? _anchor.position + Vector3.up * (heightAbove + _size.y * 0.5f) : _root.position;
            if (_pointing && cam != null && _anchor != null && Dialogue.GetTarget(out var focus, out var t) == Dialogue.TargetState.Active)
            {
                float radius = t != null && t.radius != null ? t.radius() : 0.03f;
                float before = _sideSign;
                target = SidePosition(cam, _anchor.parent != null ? _anchor.parent.position + Vector3.up * 0.01f : _anchor.position, focus, Mathf.Max(radius, 0.02f));
                if (before != _sideSign) PlaceTail();
            }
            if (cam != null) target = ClearOfKeeps(cam, target);
            if (cam != null)
            {
                Vector3 head = cam.transform.position, fwd = cam.transform.forward, dir = target - head;
                float angle = Vector3.Angle(fwd, dir);
                if (angle > maxViewAngle && dir.sqrMagnitude > 1e-6f)
                    target = head + Vector3.RotateTowards(dir, fwd * dir.magnitude, (angle - maxViewAngle) * Mathf.Deg2Rad, 0f);
            }
            // Never inside the island (step 9e): the whole panel stays above the cap or beside the rim.
            if (island != null && island.map != null)
                target = island.PushOutWorld(target, new Vector3(_size.x * 0.5f + 0.005f, _size.y * 0.5f + 0.008f, _size.x * 0.5f + 0.005f));
            if (island != null && island.map != null && cam != null) target = KeepAboveIsland(cam.transform.position, target);
            _root.position = snap ? target : Vector3.Lerp(_root.position, target, 1f - Mathf.Exp(-Time.deltaTime * 10f));
            _root.localScale = Vector3.one * Mathf.SmoothStep(0.6f, 1f, _appear);
            if (cam == null) return;
            Vector3 away = _root.position - cam.transform.position;
            if (away.sqrMagnitude > 1e-6f) _root.rotation = Quaternion.LookRotation(away, Vector3.up);
        }

        /// <summary>
        /// Step 9h: when the island lies between the head and the bubble's bottom edge (seen from above), or under it,
        /// the bottom is lifted above the island's top plus <see cref="aboveIslandMargin"/>: it never sinks into it, also
        /// when the Spirit flies low over the middle or the player sits far back.
        /// </summary>
        Vector3 KeepAboveIsland(Vector3 head, Vector3 center)
        {
            var t = island.transform;
            float s = Mathf.Max(1e-4f, t.lossyScale.x);
            Vector3 h = t.InverseTransformPoint(head);
            Vector3 b = t.InverseTransformPoint(center - Vector3.up * (_size.y * 0.5f));
            var half = island.FootprintHalfSize + new Vector2(0.02f, 0.02f);
            float top = island.CapTopLocal + aboveIslandMargin / s;
            if (b.y >= top || !CrossesFootprint(new Vector2(h.x, h.z), new Vector2(b.x, b.z), half)) return center;
            return center + t.up * ((top - b.y) * s);
        }

        /// <summary>The segment a..b (island-local xz) touches the footprint rectangle |x| &lt;= half.x, |z| &lt;= half.y.</summary>
        public static bool CrossesFootprint(Vector2 a, Vector2 b, Vector2 half)
        {
            float t0 = 0f, t1 = 1f;
            Vector2 d = b - a;
            for (int axis = 0; axis < 2; axis++)
            {
                float p = axis == 0 ? a.x : a.y, dp = axis == 0 ? d.x : d.y, h = axis == 0 ? half.x : half.y;
                if (Mathf.Abs(dp) < 1e-7f)
                {
                    if (p < -h || p > h) return false;
                    continue;
                }
                float u0 = (-h - p) / dp, u1 = (h - p) / dp;
                if (u0 > u1) (u0, u1) = (u1, u0);
                t0 = Mathf.Max(t0, u0);
                t1 = Mathf.Min(t1, u1);
                if (t0 > t1) return false;
            }
            return true;
        }

        /// <summary>Moves the bubble up (in the view) until it covers none of the <see cref="KeepClear"/> spheres.</summary>
        Vector3 ClearOfKeeps(Camera cam, Vector3 pos)
        {
            if (KeepClear == null) return pos;
            _keep.Clear();
            KeepClear(_keep);
            var head = cam.transform;
            for (int pass = 0; pass < 4; pass++)
            {
                bool moved = false;
                foreach (var k in _keep)
                {
                    Vector3 b = head.InverseTransformPoint(pos), t = head.InverseTransformPoint(k);
                    if (b.z < 0.05f || t.z < 0.05f) continue;
                    // The sphere at the bubble's depth.
                    float s = b.z / t.z;
                    Vector2 tp = new Vector2(t.x, t.y) * s;
                    float tr = k.w * s + 0.006f;
                    float dx = Mathf.Abs(tp.x - b.x) - _size.x * 0.5f, dy = Mathf.Abs(tp.y - b.y) - _size.y * 0.5f;
                    float ox = Mathf.Max(dx, 0f), oy = Mathf.Max(dy, 0f);
                    if (ox * ox + oy * oy >= tr * tr) continue;
                    // Up until the bubble's lower edge clears the top of the sphere.
                    pos += head.up * (tp.y + tr - (b.y - _size.y * 0.5f) + 0.004f);
                    moved = true;
                }
                if (!moved) break;
            }
            return pos;
        }

        void GazePinch()
        {
            if (!HeadGaze.TryGetRay(out var ray)) return;
            Vector3 o = _root.InverseTransformPoint(ray.origin);
            Vector3 d = _root.InverseTransformDirection(ray.direction);
            bool looking = false;
            if (d.z > 1e-4f)
            {
                Vector3 hit = o + d * (-o.z / d.z);
                looking = Mathf.Abs(hit.x) < _size.x * 0.6f && Mathf.Abs(hit.y) < _size.y * 0.8f;
            }
            IsGazed = looking;
            for (int h = 0; h < 2; h++)
            {
                var hand = HandRegistry.Get(h == 0 ? Handedness.Left : Handedness.Right);
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
