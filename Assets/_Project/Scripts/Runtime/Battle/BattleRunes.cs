using System;
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
    /// The battle choices: three move runes and a smaller Run rune floating just above the island's near rim,
    /// facing the head, with a caption line above them. Pick with head gaze (soft reticle from the centre eye; Quest 3 has no eye
    /// tracking) and a pinch on either hand, or poke a rune with an index finger (Interaction SDK poke: each rune is a
    /// PokeInteractable on a clipped plane, and its plate presses in).
    /// The row moves down and closer if it would cover a battling creature from the head's view.
    /// Before a creature is out only Run shows (<see cref="ShowRunOnly"/>); its moves pop in one after another when it
    /// lands (<see cref="Show"/>) and shrink away while a switch pod flies (<see cref="HideMoves"/>). Catching has no
    /// rune: the cradle pod is always there.
    /// Editor: 1-3 pick a move, 4 Run, Enter picks the rune under the gaze.
    /// </summary>
    public class BattleRunes : MonoBehaviour
    {
        public IslandBuilder island;
        [Tooltip("Pinches from a hand that is grabbing or touching the rim are ignored.")]
        public IslandHandle grabGuard;
        public Material hudMaterial;
        [Tooltip("Step 14f: Diorama/Tablet (Materials/Tablet): the runes are mossy stone tablets, the move's element on the inlay.")]
        public Material tabletMaterial;

        [Header("Layout (island-local metres)")]
        [Tooltip("Row centre relative to the middle of the near rim, at the cap height.")]
        public Vector3 offset = new Vector3(0f, 0.05f, 0.02f);
        [Tooltip("Where the row goes (added to the offset) when it would cover a creature.")]
        public Vector3 avoidOffset = new Vector3(0f, -0.05f, 0.035f);
        [Tooltip("Unused since step 14f: the runes are a 2 x 2 grid of tablets sized to the longest move name (TextRule).")]
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

        [Tooltip("Run plate width relative to a move rune.")]
        public float runWidth = 0.7f;
        [Tooltip("Delay between the move runes popping in (s).")]
        public float popStagger = 0.1f;

        /// <summary>0-2 = move slot, 3 = run.</summary>
        public event Action<int> Chosen;

        public const int RunIndex = 3;
        const int Count = 4;

        class Rune
        {
            public Transform root;
            public Transform visual;
            public MeshRenderer plate;
            public TabletKit.View tablet;
            public BoundsClipper clipper;
            public TextMeshPro label;
            public PokeInteractable poke;
            public Color tint;
            public bool usable;
            public bool shown;
            public float appear;    // 0..1, scales the visual in (and out)
            public float appearAt;  // time it may start popping in
            public float hover;
            public float pulse;     // dialogue highlight
            public float press;     // step 14f: sinks after a pick
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
        readonly bool[] _wasPinching = new bool[2];
        StageActor _avoidA, _avoidB;
        float _avoid;
        float _baseYaw, _yaw, _tilt;
        bool _loggedPokeHover, _loggedPokeSelect, _reportedInteractors;

        void Awake()
        {
            if (hudMaterial == null) Debug.LogError("[BattleRunes] No HUD material assigned.", this);
            if (tabletMaterial == null) Debug.LogError("[BattleRunes] No tablet material (Set Up MR_Main (14f menus and fixes)): plain plates.", this);
            _quad = BattleHud.VerticalQuad();
            _block = new MaterialPropertyBlock();
            _size = new Vector2(MinWidth, RuneBoard.TabletH);
            Build();
            Hide();
            Dialogue.RegisterTarget("runes", () => RunePoint(1), k => PulseRunes(0, 2, k));
            // The attack lesson: one move rune to poke, the one nearest the player's head (step 8c).
            Dialogue.RegisterTarget("nearestRune", () => RunePoint(NearestMove()), k => PulseRunes(NearestMove(), NearestMove(), k));
        }

        int NearestMove()
        {
            var cam = Camera.main;
            if (cam == null || this == null) return 1;
            int best = 1;
            float bestD = float.MaxValue;
            for (int i = 0; i < 3; i++)
            {
                if (_runes[i] == null || !_runes[i].shown) continue;
                // Near-ties go to the middle one.
                float d = (_runes[i].root.position - cam.transform.position).sqrMagnitude - (i == 1 ? 1e-4f : 0f);
                if (d < bestD)
                {
                    bestD = d;
                    best = i;
                }
            }
            return best;
        }

        Vector3? RunePoint(int index) =>
            this != null && gameObject.activeInHierarchy && _runes[index].shown ? _runes[index].root.position : (Vector3?)null;

        void PulseRunes(int from, int to, float k)
        {
            if (this == null) return;
            for (int i = 0; i < _runes.Length; i++) _runes[i].pulse = i >= from && i <= to ? k : 0f;
        }

        void OnDestroy() => Destroy(_quad);

        // Step 12c (Oct 28 notes: the bubble sat over a rune and ate the press): the Spirit's bubble keeps off shown runes.
        void OnEnable() => DialogueBubble.KeepClear += KeepClear;

        void OnDisable() => DialogueBubble.KeepClear -= KeepClear;

        void KeepClear(System.Collections.Generic.List<Vector4> keep)
        {
            foreach (var r in _runes)
            {
                if (r == null || !r.shown || r.root == null) continue;
                var p = r.root.position;
                keep.Add(new Vector4(p.x, p.y, p.z, _size.x * 0.55f * r.root.lossyScale.x));
            }
        }

        void Build()
        {
            _row = new GameObject("Rune Row").transform;
            _row.SetParent(transform, false);
            for (int i = 0; i < _runes.Length; i++)
            {
                var root = new GameObject($"Rune {i + 1}").transform;
                root.SetParent(_row, false);
                root.localPosition = RunePos(i);
                var rune = new Rune { root = root };
                BuildPoke(rune, i, PlateSize(i));
                _runes[i] = rune;
            }
            _caption = MakeText(_row, RuneBoard.LabelFont);
            _caption.transform.localPosition = new Vector3(0f, CaptionY, 0f);

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
        // ---- step 14f: a 2 x 2 grid at the readable size (a row of four was wider than the view) ----

        /// <summary>The narrowest a tablet gets; they widen to the longest move name shown.</summary>
        const float MinWidth = 0.2f;
        Vector2 _size;

        /// <summary>Every tablet is the same size (moves and Run).</summary>
        Vector2 PlateSize(int index) => _size;

        /// <summary>Rune centre: moves 1 and 2 on top, move 3 and Run below (Run bottom right).</summary>
        Vector3 RunePos(int index)
        {
            float x = (index == 0 || index == 2 ? -0.5f : 0.5f) * (_size.x + RuneBoard.Gap);
            float y = (index < 2 ? 0.5f : -0.5f) * (_size.y + RuneBoard.Gap);
            return new Vector3(x, y, 0f);
        }

        float CaptionY => _size.y + RuneBoard.Gap * 0.5f + 0.035f;

        /// <summary>Widens every tablet to fit these move names.</summary>
        void Fit(MoveData[] moves)
        {
            float w = MinWidth;
            if (moves != null)
                foreach (var m in moves)
                    if (m != null) w = Mathf.Max(w, RuneBoard.Fit(m.Name).x);
            if (Mathf.Approximately(w, _size.x)) return;
            _size.x = w;
            for (int i = 0; i < _runes.Length; i++)
            {
                var r = _runes[i];
                r.root.localPosition = RunePos(i);
                if (r.clipper != null) r.clipper.Size = new Vector3(_size.x, _size.y, 0.02f);
                if (r.tablet != null) r.tablet.size = _size;
                if (r.label != null) r.label.rectTransform.sizeDelta = new Vector2(_size.x * 0.96f, _size.y);
            }
            _caption.transform.localPosition = new Vector3(0f, CaptionY, 0f);
        }

        void BuildPoke(Rune rune, int index, Vector2 size)
        {
            var surfaceGo = new GameObject("Poke Surface");
            surfaceGo.transform.SetParent(rune.root, false);
            surfaceGo.transform.localPosition = new Vector3(0f, 0f, pressDepth);
            var plane = surfaceGo.AddComponent<PlaneSurface>();
            plane.InjectAllPlaneSurface(PlaneSurface.NormalFacing.Backward, false);
            var clipper = surfaceGo.AddComponent<BoundsClipper>();
            clipper.Size = new Vector3(size.x, size.y, 0.02f);
            rune.clipper = clipper;
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

            if (tabletMaterial != null)
            {
                rune.tablet = TabletKit.Make(visual, tabletMaterial, size, Color.clear);
                rune.label = MakeText(rune.tablet.transform, RuneBoard.LabelFont);
                rune.label.rectTransform.sizeDelta = new Vector2(size.x * 0.92f, size.y);
                TabletKit.StyleLabel(rune.label);
                return;
            }
            var plateGo = new GameObject("Plate");
            plateGo.transform.SetParent(visual, false);
            plateGo.transform.localScale = new Vector3(size.x, size.y, 1f);
            plateGo.AddComponent<MeshFilter>().sharedMesh = _quad;
            rune.plate = plateGo.AddComponent<MeshRenderer>();
            rune.plate.sharedMaterial = hudMaterial;
            rune.plate.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rune.label = MakeText(visual, RuneBoard.LabelFont);
            rune.label.transform.localPosition = new Vector3(0f, 0f, -0.001f);
        }

        void OnPokeState(int index, InteractableStateChangeArgs args)
        {
            if (args.NewState == InteractableState.Hover && index >= 0 && index < _runes.Length && _runes[index].shown)
                GameAudio.At(AudioIds.RuneHover, _runes[index].visual.position);
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
            if (_interactable && !GamePause.IsPaused && _runes[index].shown && _runes[index].appear > 0.6f) Pick(index, "poke");
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
        float HalfRow => _size.x + RuneBoard.Gap * 0.5f;

        /// <summary>Battle start, no creature out yet: the row with only the Run rune.</summary>
        public void ShowRunOnly()
        {
            bool wasHidden = !gameObject.activeSelf;
            Open();
            for (int i = 0; i < 3; i++) SetShown(_runes[i], false, 0f);
            SetShown(_runes[RunIndex], true, 0f);
            if (wasHidden) PlaceRow(true);
            Paint();
        }

        /// <summary>Shrinks the move runes away (a switch pod is flying); Run stays.</summary>
        public void HideMoves()
        {
            for (int i = 0; i < 3; i++) SetShown(_runes[i], false, 0f);
        }

        void SetShown(Rune r, bool shown, float delay)
        {
            if (shown && !r.shown) r.appearAt = Time.time + delay;
            r.shown = shown;
            r.poke.gameObject.SetActive(shown);
        }

        void Open()
        {
            gameObject.SetActive(true);
            _runes[RunIndex].usable = true;
            _runes[RunIndex].tint = new Color(0.55f, 0.58f, 0.56f);
            _runes[RunIndex].label.text = "<b>Run</b>";
            if (!_reportedInteractors)
            {
                _reportedInteractors = true;
                ReportPokeInteractors();
            }
        }

        /// <summary>
        /// The creature is out: its three moves pop in one after another (Run stays). Not pickable until
        /// <see cref="SetInteractable"/>.
        /// </summary>
        public void Show(MoveData[] moves)
        {
            bool wasHidden = !gameObject.activeSelf;
            Open();
            Fit(moves);
            for (int i = 0; i < 3; i++)
            {
                var m = moves != null && i < moves.Length ? moves[i] : null;
                var r = _runes[i];
                r.usable = m != null;
                r.tint = m != null ? ElementStyle.Of(m.element).main : Color.gray;
                // Step 14f: the move's element shows as the inlay's colour; its "Water / Support" second line was dropped
                // (it didn't fit at the readable size).
                r.label.text = m != null ? $"<b>{m.Name}</b>" : "-";
                if (r.shown) r.appear = 0f; // re-pop with the new moves
                r.shown = false;
                SetShown(r, true, i * popStagger);
            }
            SetShown(_runes[RunIndex], true, 0f);
            _hovered = -1;
            if (wasHidden) PlaceRow(true);
            Paint();
        }

        public void Hide()
        {
            _interactable = false;
            foreach (var r in _runes)
            {
                SetShown(r, false, 0f);
                r.appear = 0f;
            }
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
            int was = _hovered;
            _hovered = _interactable ? GazeHover() : -1;
            if (_hovered >= 0 && _hovered != was) GameAudio.At(AudioIds.RuneHover, _runes[_hovered].visual.position);
            for (int i = 0; i < _runes.Length; i++)
            {
                var r = _runes[i];
                r.hover = Mathf.MoveTowards(r.hover, i == _hovered ? 1f : 0f, Time.deltaTime * 8f);
                bool popping = r.shown && Time.time >= r.appearAt;
                r.appear = Mathf.MoveTowards(r.appear, popping ? 1f : 0f, Time.deltaTime / (popping ? 0.25f : 0.15f));
                float a = r.shown ? EaseOutBack(r.appear) : r.appear;
                r.visual.gameObject.SetActive(r.appear > 0.001f);
                // Scale the visual only: the poke surface keeps its size.
                r.visual.localScale = Vector3.one * (Mathf.Max(0.001f, a) * (1f + 0.12f * r.hover + 0.08f * r.pulse));
                r.press = Mathf.MoveTowards(r.press, 0f, Time.deltaTime / 0.3f);
                if (r.tablet != null) r.tablet.Press(r.press > 0.5f ? 1f : r.press * 2f);
            }
            Paint();
            // Step 9g: no picks while the game menu (or anything else) has the game paused.
            if (!_interactable || GamePause.IsPaused) return;

            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) Pick(0, "key");
                else if (kb.digit2Key.wasPressedThisFrame) Pick(1, "key");
                else if (kb.digit3Key.wasPressedThisFrame) Pick(2, "key");
                else if (kb.digit4Key.wasPressedThisFrame) Pick(RunIndex, "key");
                else if (kb.enterKey.wasPressedThisFrame && _hovered >= 0) Pick(_hovered, "gaze + Enter");
                if (!_interactable) return;
            }
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
            if (index < 0 || index >= _runes.Length || !_runes[index].usable || !_runes[index].shown) return;
            Debug.Log($"[BattleRunes] Picked {(index == RunIndex ? "Run" : $"move {index + 1}")} by {how}.");
            GameAudio.At(index == RunIndex ? AudioIds.BattleRun : AudioIds.RuneSelect, _runes[index].visual.position);
            GameAudio.At(AudioIds.StepStone, _runes[index].visual.position);
            _runes[index].press = 1f;
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
            float halfW = HalfRow, top = CaptionY + 0.03f, bottom = -(_size.y + RuneBoard.Gap * 0.5f);
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
            bool near = Mathf.Abs(hit.x) < HalfRow + 0.05f && Mathf.Abs(hit.y) < _size.y * 2.5f;
            _reticle.enabled = near;
            if (near) _reticle.transform.localPosition = new Vector3(hit.x, hit.y, -0.002f / Mathf.Max(1e-4f, scale));
            // Gaze cone: each plate (at least the 4 cm minimum) grows by the cone's radius at the row; the one whose
            // centre is nearest wins, the hovered one a little stickier.
            float cone = HeadGaze.ConeRadius((_row.TransformPoint(hit) - ray.origin).magnitude) / Mathf.Max(1e-4f, scale);
            float min = HeadGaze.MinRadius * (island != null ? island.transform.lossyScale.x : 1f) / Mathf.Max(1e-4f, scale);
            int best = -1;
            float bestScore = float.MaxValue;
            for (int i = 0; i < _runes.Length; i++)
            {
                if (!_runes[i].shown) continue;
                Vector3 p = hit - _runes[i].root.localPosition;
                float hx = Mathf.Max(PlateSize(i).x * 0.5f, min), hy = Mathf.Max(_size.y * 0.5f, min);
                if (Mathf.Abs(p.x) > hx + cone || Mathf.Abs(p.y) > hy + cone) continue;
                float score = Mathf.Max(Mathf.Abs(p.x) / hx, Mathf.Abs(p.y) / hy) * (i == _hovered ? HeadGaze.Sticky : 1f);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = i;
                }
            }
            return best >= 0 && _runes[best].usable ? best : -1;
        }

        void Paint()
        {
            for (int i = 0; i < _runes.Length; i++)
            {
                var r = _runes[i];
                if (r != null && r.tablet != null)
                {
                    // The move's element on the inlay (Run: a plain groove); usable = full, else dimmed.
                    bool on = _interactable && r.usable;
                    var inlay = r.tint;
                    inlay.a = i == RunIndex ? 0f : 0.9f;
                    r.tablet.inlay = inlay;
                    r.tablet.Paint(r.hover, on ? 0f : 0.65f, 0f, 0.7f * r.pulse);
                    var lc = Color.Lerp(TabletKit.LabelColor, TabletKit.LabelLit, Mathf.Max(r.hover, r.pulse));
                    lc.a = on ? 1f : 0.55f;
                    r.label.color = lc;
                    continue;
                }
                if (r == null || r.plate == null) continue;
                float dim = _interactable && r.usable ? 1f : 0.45f;
                float glow = 0.7f * r.pulse;
                var back = Color.Lerp(new Color(0.07f, 0.09f, 0.08f), r.tint, Mathf.Lerp(0.3f, 0.6f, dim) + 0.25f * r.hover + 0.2f * glow);
                back.a = 0.9f * Mathf.Lerp(0.6f, 1f, dim);
                var border = Color.Lerp(r.tint, Color.white, Mathf.Clamp01(r.hover * 0.6f + glow));
                border.a = dim;
                _block.Clear();
                _block.SetColor(BackColorId, back);
                _block.SetColor(BorderColorId, border);
                _block.SetFloat(FillId, 0f);
                _block.SetFloat(AspectId, PlateSize(i).x / _size.y);
                _block.SetFloat(RadiusId, 0.3f);
                _block.SetFloat(BorderId, 0.07f + 0.05f * r.hover + 0.04f * glow);
                r.plate.SetPropertyBlock(_block);
                var c = r.label.color;
                c.a = Mathf.Lerp(0.5f, 1f, dim);
                r.label.color = c;
            }
        }

        static float EaseOutBack(float t)
        {
            const float c = 1.7f;
            t -= 1f;
            return 1f + (c + 1f) * t * t * t + c * t * t;
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
