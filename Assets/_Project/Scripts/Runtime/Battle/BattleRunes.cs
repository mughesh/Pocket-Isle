using System;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using Oculus.Interaction.Surfaces;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Diorama
{
    /// <summary>
    /// The battle choices: three move runes, a Catch rune and a Run rune floating just above the island's near rim,
    /// facing the head, with a caption line above them. Pick with head gaze (soft reticle from the centre eye; Quest 3 has no eye
    /// tracking) and a pinch on either hand, or poke a rune with an index finger (Interaction SDK poke: each rune is a
    /// PokeInteractable on a clipped plane, and its plate presses in).
    /// The row moves down and closer if it would cover a battling creature from the head's view.
    /// Editor: 1-5 pick directly, Enter picks the rune under the gaze.
    /// </summary>
    public class BattleRunes : MonoBehaviour
    {
        public IslandBuilder island;
        [Tooltip("Pinches from a hand that is grabbing or touching the rim are ignored.")]
        public IslandHandle grabGuard;
        public Material hudMaterial;

        [Header("Layout (island-local metres)")]
        [Tooltip("Row centre relative to the middle of the near rim, at the cap height.")]
        public Vector3 offset = new Vector3(0f, 0.05f, 0.02f);
        [Tooltip("Where the row goes (added to the offset) when it would cover a creature.")]
        public Vector3 avoidOffset = new Vector3(0f, -0.05f, 0.035f);
        public Vector2 runeSize = new Vector2(0.085f, 0.04f);
        public float spacing = 0.095f;
        public float fontSize = 0.11f;
        public float captionFontSize = 0.12f;

        [Header("Facing")]
        [Tooltip("The row faces the head's direction at battle start, then drifts at most this far (degrees, yaw).")]
        public float maxDrift = 15f;
        [Tooltip("How fast it drifts toward the head (1/s).")]
        public float driftSpeed = 1.5f;
        [Tooltip("The row pitches back so its face points at the eyes, clamped to this (degrees).")]
        public float maxTilt = 40f;

        [Header("Poke")]
        [Tooltip("How far the plate travels in when pressed (island-local metres).")]
        public float pressDepth = 0.008f;

        /// <summary>0-2 = move slot, 3 = catch, 4 = run.</summary>
        public event Action<int> Chosen;

        public const int CatchIndex = 3;
        public const int RunIndex = 4;
        const int Count = 5;

        class Rune
        {
            public Transform root;
            public Transform visual;
            public MeshRenderer plate;
            public TextMeshPro label;
            public PokeInteractable poke;
            public Color tint;
            public bool usable;
            public float hover;
        }

        static readonly int BackColorId = Shader.PropertyToID("_BackColor");
        static readonly int BorderColorId = Shader.PropertyToID("_BorderColor");
        static readonly int FillId = Shader.PropertyToID("_Fill");
        static readonly int AspectId = Shader.PropertyToID("_Aspect");
        static readonly int RadiusId = Shader.PropertyToID("_Radius");
        static readonly int BorderId = Shader.PropertyToID("_Border");

        readonly Rune[] _runes = new Rune[Count];
        Transform _row;
        TextMeshPro _caption;
        MeshRenderer _reticle;
        Mesh _quad;
        MaterialPropertyBlock _block;
        bool _interactable;
        int _hovered = -1;
        readonly Hand[] _hands = new Hand[2];
        readonly bool[] _wasPinching = new bool[2];
        StageActor _avoidA, _avoidB;
        float _avoid;
        float _baseYaw, _yaw, _tilt;
        bool _loggedPokeHover, _loggedPokeSelect, _reportedInteractors;

        void Awake()
        {
            if (hudMaterial == null) Debug.LogError("[BattleRunes] No HUD material assigned.", this);
            _quad = BattleHud.VerticalQuad();
            _block = new MaterialPropertyBlock();
            Build();
            Hide();
        }

        void OnDestroy() => Destroy(_quad);

        void Build()
        {
            _row = new GameObject("Rune Row").transform;
            _row.SetParent(transform, false);
            for (int i = 0; i < _runes.Length; i++)
            {
                var root = new GameObject($"Rune {i + 1}").transform;
                root.SetParent(_row, false);
                root.localPosition = new Vector3((i - (Count - 1) * 0.5f) * spacing, 0f, 0f);
                var rune = new Rune { root = root };
                BuildPoke(rune, i);
                _runes[i] = rune;
            }
            _caption = MakeText(_row, captionFontSize);
            _caption.transform.localPosition = new Vector3(0f, runeSize.y * 0.5f + 0.018f, 0f);

            var reticleGo = new GameObject("Gaze Reticle");
            reticleGo.transform.SetParent(_row, false);
            reticleGo.transform.localScale = Vector3.one * 0.008f;
            reticleGo.AddComponent<MeshFilter>().sharedMesh = _quad;
            _reticle = reticleGo.AddComponent<MeshRenderer>();
            _reticle.sharedMaterial = hudMaterial;
            _block.Clear();
            _block.SetColor(BackColorId, new Color(1f, 1f, 1f, 0.55f));
            _block.SetColor(BorderColorId, new Color(1f, 1f, 1f, 0.9f));
            _block.SetFloat(FillId, 0f);
            _block.SetFloat(AspectId, 1f);
            _block.SetFloat(RadiusId, 0.5f);
            _block.SetFloat(BorderId, 0.12f);
            _reticle.SetPropertyBlock(_block);
        }

        /// <summary>
        /// ISDK poke button, as in the SDK's poke button sample: the surface (plane clipped to the plate) is the button
        /// base, set back by <see cref="pressDepth"/>; the visual (plate + label) sits in front of it and the
        /// PokeInteractableVisual pushes it toward the base while a finger presses.
        /// The row's front faces the head along -Z, so the plane normal faces backward (-Z).
        /// </summary>
        void BuildPoke(Rune rune, int index)
        {
            var surfaceGo = new GameObject("Poke Surface");
            surfaceGo.transform.SetParent(rune.root, false);
            surfaceGo.transform.localPosition = new Vector3(0f, 0f, pressDepth);
            var plane = surfaceGo.AddComponent<PlaneSurface>();
            plane.InjectAllPlaneSurface(PlaneSurface.NormalFacing.Backward, false);
            var clipper = surfaceGo.AddComponent<BoundsClipper>();
            clipper.Size = new Vector3(runeSize.x, runeSize.y, 0.02f);
            var clipped = surfaceGo.AddComponent<ClippedPlaneSurface>();
            clipped.InjectAllClippedPlaneSurface(plane, new IBoundsClipper[] { clipper });
            var poke = surfaceGo.AddComponent<PokeInteractable>();
            poke.InjectAllPokeInteractable(clipped);
            poke.WhenStateChanged += args => OnPokeState(index, args);
            rune.poke = poke;

            var visual = new GameObject("Visual").transform;
            visual.SetParent(rune.root, false);
            var pressVisual = visual.gameObject.AddComponent<PokeInteractableVisual>();
            pressVisual.InjectAllPokeInteractableVisual(poke, surfaceGo.transform);
            rune.visual = visual;

            var plateGo = new GameObject("Plate");
            plateGo.transform.SetParent(visual, false);
            plateGo.transform.localScale = new Vector3(runeSize.x, runeSize.y, 1f);
            plateGo.AddComponent<MeshFilter>().sharedMesh = _quad;
            rune.plate = plateGo.AddComponent<MeshRenderer>();
            rune.plate.sharedMaterial = hudMaterial;
            rune.plate.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rune.label = MakeText(visual, fontSize);
            rune.label.transform.localPosition = new Vector3(0f, 0f, -0.001f);
        }

        void OnPokeState(int index, InteractableStateChangeArgs args)
        {
            if (args.NewState == InteractableState.Hover && !_loggedPokeHover)
            {
                _loggedPokeHover = true;
                Debug.Log($"[BattleRunes] Poke hover on rune {index + 1} (first time; ISDK poke works).");
            }
            if (args.NewState != InteractableState.Select) return;
            if (!_loggedPokeSelect)
            {
                _loggedPokeSelect = true;
                Debug.Log($"[BattleRunes] Poke select on rune {index + 1} (first time).");
            }
            if (_interactable) Pick(index, "poke");
        }

        /// <summary>
        /// The rig's hands need enabled HandPokeInteractors for poking (the locomotion cleanup must not remove them).
        /// ISDK switches a hand's interactor group off while that hand isn't tracked, so this is logged at the first
        /// battle, not at startup.
        /// </summary>
        void ReportPokeInteractors()
        {
            var all = FindObjectsByType<PokeInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int active = 0;
            var names = new System.Text.StringBuilder();
            foreach (var p in all)
            {
                bool on = p.isActiveAndEnabled;
                if (on) active++;
                names.Append($" {p.name}{(on ? "" : " (off)")}");
            }
            if (all.Length == 0) Debug.LogError("[BattleRunes] No PokeInteractor in the scene; poking runes can't work.");
            else if (active == 0) Debug.LogWarning($"[BattleRunes] No PokeInteractor active right now (hands not tracked?). Found:{names}");
            else Debug.Log($"[BattleRunes] Poke interactors: {active} active of {all.Length}:{names}");
        }

        /// <summary>Half the row width (rune centres to the outer edge).</summary>
        float HalfRow => (Count - 1) * 0.5f * spacing + runeSize.x * 0.5f;

        /// <summary>Shows the row with these three moves plus Catch and Run. Not pickable until <see cref="SetInteractable"/>.</summary>
        public void Show(MoveData[] moves, bool canCatch = true)
        {
            gameObject.SetActive(true);
            for (int i = 0; i < 3; i++)
            {
                var m = moves != null && i < moves.Length ? moves[i] : null;
                var r = _runes[i];
                r.usable = m != null;
                r.tint = m != null ? ElementStyle.Of(m.element).main : Color.gray;
                string kind = m == null ? "" : m.kind == MoveKind.Support ? "Support" : m.element.ToString();
                r.label.text = m != null ? $"<b>{m.Name}</b>\n<size=70%>{kind}</size>" : "-";
            }
            _runes[CatchIndex].usable = canCatch;
            _runes[CatchIndex].tint = new Color(0.86f, 0.66f, 0.3f);
            _runes[CatchIndex].label.text = "<b>Catch</b>\n<size=70%>Pod</size>";
            _runes[RunIndex].usable = true;
            _runes[RunIndex].tint = new Color(0.55f, 0.58f, 0.56f);
            _runes[RunIndex].label.text = "<b>Run</b>";
            _hovered = -1;
            if (!_reportedInteractors)
            {
                _reportedInteractors = true;
                ReportPokeInteractors();
            }
            PlaceRow(true);
            Paint();
        }

        public void Hide()
        {
            _interactable = false;
            _avoidA = _avoidB = null;
            _avoid = 0f;
            gameObject.SetActive(false);
        }

        /// <summary>Creatures the row must not cover from the head's view.</summary>
        public void Avoid(StageActor a, StageActor b)
        {
            _avoidA = a;
            _avoidB = b;
        }

        /// <summary>Dims the runes and ignores input while a turn plays.</summary>
        public void SetInteractable(bool on)
        {
            _interactable = on;
            for (int i = 0; i < 2; i++) _wasPinching[i] = true; // a pinch already held doesn't count
            Paint();
        }

        public void SetCaption(string text)
        {
            if (_caption != null) _caption.text = text;
        }

        void Update()
        {
            PlaceRow(false);
            _hovered = _interactable ? GazeHover() : -1;
            for (int i = 0; i < _runes.Length; i++)
            {
                var r = _runes[i];
                r.hover = Mathf.MoveTowards(r.hover, i == _hovered ? 1f : 0f, Time.deltaTime * 8f);
                // Scale the visual only: the poke surface keeps its size.
                r.visual.localScale = Vector3.one * (1f + 0.12f * r.hover);
            }
            Paint();
            if (!_interactable) return;

            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) Pick(0, "key");
                else if (kb.digit2Key.wasPressedThisFrame) Pick(1, "key");
                else if (kb.digit3Key.wasPressedThisFrame) Pick(2, "key");
                else if (kb.digit4Key.wasPressedThisFrame) Pick(CatchIndex, "key");
                else if (kb.digit5Key.wasPressedThisFrame) Pick(RunIndex, "key");
                else if (kb.enterKey.wasPressedThisFrame && _hovered >= 0) Pick(_hovered, "gaze + Enter");
                if (!_interactable) return;
            }
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
                if (pinching && !_wasPinching[h] && !busy && _hovered >= 0) Pick(_hovered, $"gaze + pinch ({(h == 0 ? "left" : "right")})");
                _wasPinching[h] = pinching;
                if (!_interactable) return;
            }
        }

        /// <summary>Picks from code (testing). Ignored unless the runes are waiting for a choice.</summary>
        public void Choose(int index)
        {
            if (_interactable) Pick(index, "code");
        }

        void Pick(int index, string how)
        {
            if (index < 0 || index >= _runes.Length || !_runes[index].usable) return;
            Debug.Log($"[BattleRunes] Picked {(index == RunIndex ? "Run" : index == CatchIndex ? "Catch" : $"move {index + 1}")} by {how}.");
            _interactable = false;
            Paint();
            Chosen?.Invoke(index);
        }

        /// <summary>
        /// Keeps the row above the middle of the near rim (the side toward the player). It faces the head's direction
        /// at battle start (yaw on the island), then only drifts within <see cref="maxDrift"/>; it pitches back so its face
        /// points at the eyes (0 to <see cref="maxTilt"/>).
        /// </summary>
        void PlaceRow(bool snap)
        {
            if (island == null || island.map == null) return;
            var b = island.LocalBounds;
            var cam = Camera.main;
            if (cam != null && Covers(cam.transform.position)) _avoid = Mathf.MoveTowards(_avoid, 1f, Time.deltaTime * 3f);
            _row.localPosition = new Vector3(b.center.x, island.map.capHeight, b.max.z) + offset + avoidOffset * _avoid;
            if (cam != null)
            {
                // Head direction projected on the island, as a yaw in island space (the row's front looks toward -Z).
                Vector3 away = island.transform.InverseTransformDirection(_row.position - cam.transform.position);
                float below = -away.y;
                away.y = 0f;
                if (away.sqrMagnitude > 1e-8f)
                {
                    float headYaw = Mathf.Atan2(away.x, away.z) * Mathf.Rad2Deg;
                    // Head above the row = positive pitch (top leans away, face turns up to the eyes).
                    float tilt = Mathf.Clamp(Mathf.Atan2(below, away.magnitude) * Mathf.Rad2Deg, 0f, maxTilt);
                    if (snap)
                    {
                        _baseYaw = _yaw = headYaw;
                        _tilt = tilt;
                    }
                    float target = _baseYaw + Mathf.Clamp(Mathf.DeltaAngle(_baseYaw, headYaw), -maxDrift, maxDrift);
                    float k = 1f - Mathf.Exp(-Time.deltaTime * driftSpeed);
                    _yaw = Mathf.LerpAngle(_yaw, target, k);
                    _tilt = Mathf.Lerp(_tilt, tilt, k);
                }
            }
            _row.rotation = island.transform.rotation * Quaternion.Euler(_tilt, _yaw, 0f);
        }

        /// <summary>True when the row (runes and caption) sits between the head and either creature's body.</summary>
        bool Covers(Vector3 head)
        {
            if (_avoid >= 1f) return false;
            return Covers(head, _avoidA) || Covers(head, _avoidB);
        }

        bool Covers(Vector3 head, StageActor actor)
        {
            if (actor == null) return false;
            float halfW = HalfRow, top = runeSize.y * 0.5f + 0.03f, bottom = -runeSize.y * 0.5f;
            var t = island.transform;
            Vector3[] points = { actor.WorldCenter, t.TransformPoint(actor.LocalTop), t.TransformPoint(actor.island.TileToLocal(actor.tile, actor.island.Content.capHeight + actor.baseLift)) };
            Vector3 o = _row.InverseTransformPoint(head);
            foreach (var p in points)
            {
                Vector3 q = _row.InverseTransformPoint(p);
                if (q.z <= 0f || o.z >= 0f) continue; // the creature must be behind the row, the head in front
                Vector3 hit = Vector3.Lerp(o, q, -o.z / (q.z - o.z));
                if (Mathf.Abs(hit.x) < halfW && hit.y < top && hit.y > bottom) return true;
            }
            return false;
        }

        int GazeHover()
        {
            if (!HeadGaze.TryGetRay(out var ray))
            {
                _reticle.enabled = false;
                return -1;
            }
            // The row faces the head: its plane is z = 0 in row space, the front looks toward -Z.
            Vector3 o = _row.InverseTransformPoint(ray.origin);
            Vector3 d = _row.InverseTransformDirection(ray.direction);
            if (d.z <= 1e-4f)
            {
                _reticle.enabled = false;
                return -1;
            }
            Vector3 hit = o + d * (-o.z / d.z);
            float scale = _row.lossyScale.x;
            bool near = Mathf.Abs(hit.x) < HalfRow + spacing && Mathf.Abs(hit.y) < runeSize.y * 2.5f;
            _reticle.enabled = near;
            if (near) _reticle.transform.localPosition = new Vector3(hit.x, hit.y, -0.002f / Mathf.Max(1e-4f, scale));
            for (int i = 0; i < _runes.Length; i++)
            {
                Vector3 p = hit - _runes[i].root.localPosition;
                // A little bigger than the plate, so the gaze doesn't flicker between runes.
                if (Mathf.Abs(p.x) < spacing * 0.5f && Mathf.Abs(p.y) < runeSize.y * 0.75f) return _runes[i].usable ? i : -1;
            }
            return -1;
        }

        void Paint()
        {
            foreach (var r in _runes)
            {
                if (r == null || r.plate == null) continue;
                float dim = _interactable && r.usable ? 1f : 0.45f;
                var back = Color.Lerp(new Color(0.07f, 0.09f, 0.08f), r.tint, Mathf.Lerp(0.3f, 0.6f, dim) + 0.25f * r.hover);
                back.a = 0.9f * Mathf.Lerp(0.6f, 1f, dim);
                var border = Color.Lerp(r.tint, Color.white, r.hover * 0.6f);
                border.a = dim;
                _block.Clear();
                _block.SetColor(BackColorId, back);
                _block.SetColor(BorderColorId, border);
                _block.SetFloat(FillId, 0f);
                _block.SetFloat(AspectId, runeSize.x / runeSize.y);
                _block.SetFloat(RadiusId, 0.3f);
                _block.SetFloat(BorderId, 0.07f + 0.05f * r.hover);
                r.plate.SetPropertyBlock(_block);
                var c = r.label.color;
                c.a = Mathf.Lerp(0.5f, 1f, dim);
                r.label.color = c;
            }
        }

        static TextMeshPro MakeText(Transform parent, float size)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshPro>();
            t.fontSize = size;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.color = Color.white;
            t.outlineWidth = 0.2f;
            t.outlineColor = new Color32(20, 30, 25, 255);
            t.rectTransform.sizeDelta = new Vector2(0.3f, 0.04f);
            return t;
        }
    }
}
