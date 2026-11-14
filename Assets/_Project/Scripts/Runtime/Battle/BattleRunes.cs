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
        public Vector2 runeSize = new Vector2(0.085f, 0.04f);
        [Tooltip("Step 14j: centre to centre (was 'spacing' 0.095: a looked-at move grew into its neighbour); the rule is TabletLayout's.")]
        public float runeStep = 0.097f;
        public float fontSize = 0.11f;
        public float captionFontSize = 0.12f;
        [Tooltip("Step 16: the battle line sits on a dark rounded pill, cream text, this much larger than captionFontSize.")]
        public float captionScale = 1.2f;
        public Color captionText = new Color(1f, 0.95f, 0.84f);
        public Color captionPill = new Color(0.13f, 0.1f, 0.08f, 0.86f);

        [Header("Over the arena (step 24: in front of the rim the island looked like it was behind a counter)")]
        [Tooltip("The row sits on the arena's front band, never in front of the rim. Off = the old place in front of the rim (offset, avoidOffset).")]
        public bool overArena = true;
        [Tooltip("The row's front edge this far inside the tile footprint's front edge (island-local m).")]
        public float arenaInset = 0.04f;
        [Tooltip("The row's front edge this far over the cap (island-local m; 0.03 is about 1.8 cm at the island's usual 0.6 size).")]
        public float arenaHover = 0.03f;

        [Header("Clear of the pods (Nov 7: the moves were in the way of the rim pods)")]
        [Tooltip("The rim pods: a hand below the row and this close to a pod (island-local m) means it is reaching for a pod.")]
        public PodRack pods;
        public float podReach = 0.12f;
        [Tooltip("Then the row moves this far (island-local m: up, toward the island's back) until the hand leaves.")]
        public Vector3 duckOffset = new Vector3(0f, 0.07f, -0.04f);

        [Header("Facing")]
        [Tooltip("Nov 7 (Mugesh: the menu rows stopped turning but the moves still did): the row is fixed to the island's front at lockedTilt, never turning with the head (like RuneBoard.rimLocked).")]
        public bool lockedToIsland = true;
        public float lockedTilt = 35f;
        [Tooltip("Step 14i: the row faces the head at battle start, then stays; it faces it again only when the head has been " +
                 "this far round (degrees) for a second with no hand within 15 cm.")]
        public float turnAwayAngle = 30f;
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
            public float shake;     // step 29: a press off-turn
            public bool beat;       // Nov 11: the Isling's own element move: glows and breathes on the player's turn until a pick
        }

        [Header("Nov 11: which move to press")]
        [Tooltip("Mugesh: nothing showed which button to poke. The Isling's own element move glows and breathes in scale (1 to this) on the player's turn until a move is chosen.")]
        public float beatScale = 1.08f;
        [Tooltip("One breath, in seconds.")]
        public float beatSeconds = 1.2f;

        /// <summary>Nov 11: the breathing move's scale and glow (0..1) at time <paramref name="t"/> (pure).</summary>
        public static float BeatWave(float t, float seconds) => 0.5f - 0.5f * Mathf.Cos(t / Mathf.Max(0.01f, seconds) * 2f * Mathf.PI);

        /// <summary>
        /// Nov 11: the move to press first: the Isling's own element attack, else its first attack (never Run or a support move).
        /// </summary>
        public static int BeatIndex(MoveData[] moves, Element element)
        {
            if (moves == null) return -1;
            int first = -1;
            for (int i = 0; i < Mathf.Min(3, moves.Length); i++)
            {
                var m = moves[i];
                if (m == null || m.kind == MoveKind.Support) continue;
                if (m.element == element) return i;
                if (first < 0) first = i;
            }
            return first;
        }

        /// <summary>The move that breathes on the player's turn (-1 = none), tests and the bridge.</summary>
        public int BeatMove { get; private set; } = -1;

        [Header("Step 29: whose turn")]
        [Tooltip("Off-turn the moves sink this far (row-local m, down) and this far back, grey, with no hover.")]
        public float offTurnSink = 0.016f;
        public float offTurnBack = 0.008f;
        [Tooltip("How grey the moves go off-turn (0..1).")]
        [Range(0f, 1f)] public float offTurnGrey = 0.85f;
        [Tooltip("Seconds to sink or rise.")]
        public float turnSeconds = 0.25f;
        public const string ChooseCaption = "Choose your attack";

        float _up;          // 0 = off-turn (sunk, grey), 1 = the player's turn
        float _risePulse;   // one soft gold rim pulse when the moves rise

        /// <summary>Step 29: 0 = sunk and grey (not the player's turn), 1 = up (tests, the bridge).</summary>
        public float Up => _up;

        /// <summary>Step 29: off-turn presses that only shook (tests, the bridge).</summary>
        public int OffTurnPresses { get; private set; }

        /// <summary>The caption while the wild (or Keeper) side acts.</summary>
        public static string TheirTurn(string name) => $"{name}'s turn...";

        /// <summary>Nov 11 panel (R49): the other side's name in captions: "Wild Vanehare", but a Keeper is never wild.</summary>
        public static string WildName(string name, bool keeper) => keeper ? name : $"Wild {name}";

        /// <summary>The move caption: "Wild Vanehare used Gust!", "Galeowl used Gale Shot!", "Toastling used Ember!".</summary>
        public static string MoveLine(string name, string move, bool wild, bool keeper) => $"{(wild ? WildName(name, keeper) : name)} used {move}!";

        /// <summary>The knock-out caption: a wild one faints or is dazed; a Keeper calms down ("Galeowl is calm!"); the player's faints.</summary>
        public static string FaintLine(string name, bool wild, bool keeper, bool dazed)
        {
            if (dazed) return $"{WildName(name, keeper)} is dazed!";
            if (wild && keeper) return $"{name} is calm!";
            return $"{(wild ? WildName(name, false) : name)} fainted!";
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
        MeshRenderer _captionPill;
        MeshRenderer _reticle;
        Mesh _quad;
        MaterialPropertyBlock _block;
        bool _interactable;
        int _hovered = -1;
        readonly bool[] _wasPinching = new bool[2];
        StageActor _avoidA, _avoidB;
        float _avoid, _duck;
        bool _ducked;

        /// <summary>
        /// Nov 7: a hand under the row (lower than its bottom edge) and near a rim pod or the cradle pod is reaching for a
        /// pod, not a move: the row gets out of its way (<see cref="duckOffset"/>).
        /// </summary>
        /// <summary>
        /// Step 21: a hand reaches for a pod, holds one, or let go of one under <see cref="PodGrace"/> s ago: rune picks by
        /// poke or gaze + pinch are ignored (the grab's pinch and the lift through the row are not move presses).
        /// </summary>
        bool PodBusy => _ducked || (pods != null && pods.AnyHeld) || Time.unscaledTime < _podFreeAt;

        public const float PodGrace = 0.5f;

        static bool Takeable(PodView pod)
        {
            var g = pod.GetComponent<PodGrab>();
            return g != null && (g.CanGrab || g.IsHeld);
        }
        float _podFreeAt;

        static readonly HandJointId[] TipOnly = { HandJointId.HandIndexTip };
        static readonly HandJointId[] TipAndPalm = { HandJointId.HandIndexTip, HandJointId.HandPalm };

        bool ReachingForPod()
        {
            if (pods == null || island == null || _row == null) return false;
            var t = island.transform;
            float bottom = _row.localPosition.y - runeSize.y * 0.5f;
            bool reaching = false;
            for (int h = 0; h < 2 && !reaching; h++)
            {
                var hand = HandRegistry.Get(h == 0 ? Handedness.Left : Handedness.Right);
                if (hand == null || !hand.IsTrackedDataValid) continue;
                // Step 24: over the arena the palm of a hand poking a move is right above the pods, so only the fingertip counts (R5).
                foreach (var joint in overArena ? TipOnly : TipAndPalm)
                {
                    if (!HandLookup.TryGetJoint(hand, joint, out var w)) continue;
                    Vector3 p = t.InverseTransformPoint(w);
                    if (p.y > bottom) continue;
                    for (int i = 0; i <= Party.Size && !reaching; i++)
                    {
                        var pod = pods.Pod(i);
                        // Step 21 (Mugesh: Run didn't work after the first move): only a pod a hand can take counts. The
                        // empty sockets sit right under Run, so a finger going for Run ducked the row away from it.
                        if (pod != null && pod.isActiveAndEnabled && Takeable(pod) && (t.InverseTransformPoint(pod.transform.position) - p).magnitude < podReach) reaching = true;
                    }
                }
            }
            if (reaching != _ducked)
            {
                _ducked = reaching;
                Debug.Log(reaching ? "[BattleRunes] A hand reaches for a pod under the row: the moves step up and back." : "[BattleRunes] The hand left the pods: the moves come back.");
            }
            return reaching;
        }
        float _yaw, _tilt, _tiltTarget;
        readonly BoardFacing _facing = new BoardFacing();
        bool _loggedPokeHover, _loggedPokeSelect, _reportedInteractors;

        void Awake()
        {
            if (hudMaterial == null) Debug.LogError("[BattleRunes] No HUD material assigned.", this);
            if (tabletMaterial == null) Debug.LogError("[BattleRunes] No tablet material (Set Up MR_Main (14f menus and fixes)): plain plates.", this);
            _quad = BattleHud.VerticalQuad();
            _block = new MaterialPropertyBlock();
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

        void OnDestroy()
        {
            Destroy(_quad);
            if (_captionTextMat != null) Destroy(_captionTextMat);
            if (_captionPillMat != null) Destroy(_captionPillMat);
        }

        // Step 12c (Oct 28 notes: the bubble sat over a rune and ate the press): the Spirit's bubble keeps off shown runes.
        void OnEnable() => DialogueBubble.KeepClear += KeepClear;

        void OnDisable() => DialogueBubble.KeepClear -= KeepClear;

        void KeepClear(System.Collections.Generic.List<Vector4> keep)
        {
            foreach (var r in _runes)
            {
                if (r == null || !r.shown || r.root == null) continue;
                var p = r.root.position;
                keep.Add(new Vector4(p.x, p.y, p.z, runeSize.x * 0.55f * r.root.lossyScale.x));
            }
            // Nov 10: and off the battle line (three spheres along the pill).
            if (_captionPill != null && _captionPill.gameObject.activeInHierarchy)
            {
                var t = _captionPill.transform;
                float w = t.lossyScale.x, radius = t.lossyScale.y * 0.8f;
                for (int k = -1; k <= 1; k++)
                {
                    var p = t.position + t.right * (k * w * 0.36f);
                    keep.Add(new Vector4(p.x, p.y, p.z, radius));
                }
            }
        }

        /// <summary>Nov 10: the shown moves' plates in the world, so a label can keep off them. False with none up.</summary>
        public bool TryGetRowBounds(out Bounds bounds)
        {
            bounds = default;
            bool any = false;
            for (int i = 0; i < _runes.Length; i++)
            {
                var r = _runes[i];
                if (r == null || !r.shown || r.root == null) continue;
                var half = new Vector3(PlateSize(i).x, runeSize.y, 0f) * 0.5f;
                for (int sx = -1; sx <= 1; sx += 2)
                    for (int sy = -1; sy <= 1; sy += 2)
                    {
                        var p = r.root.TransformPoint(new Vector3(sx * half.x, sy * half.y, 0f));
                        if (any) bounds.Encapsulate(p);
                        else bounds = new Bounds(p, Vector3.zero);
                        any = true;
                    }
            }
            return any;
        }

        /// <summary>
        /// Nov 14 (Mugesh: Steambara's body stood in front of the attack buttons in the lake temple): the name of a creature
        /// whose body reaches through the shown plates toward the head, or null. Its body box is shrunk to 80% (bodies are
        /// round, the box corners are air). The smoke run notes it.
        /// </summary>
        public string BodyThroughRow()
        {
            if (_row == null || !TryGetRowBounds(out _)) return null;
            foreach (var a in new[] { _avoidA, _avoidB })
            {
                if (a == null || !a.TryGetBodyBounds(out var b)) continue;
                float halfW = HalfRow, top = runeSize.y * 0.5f, bottom = -runeSize.y * 0.5f;
                for (int i = 0; i < 8; i++)
                {
                    var p = b.center + Vector3.Scale(b.extents * 0.8f, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var q = _row.InverseTransformPoint(p);
                    // The row's front looks toward -Z: a body corner on that side, within the plates' face, is through it.
                    if (q.z < 0f && Mathf.Abs(q.x) < halfW && q.y < top && q.y > bottom) return a.name;
                }
            }
            return null;
        }

        void Build()
        {
            _row = new GameObject("Rune Row").transform;
            _row.SetParent(transform, false);
            for (int i = 0; i < _runes.Length; i++)
            {
                var root = new GameObject($"Rune {i + 1}").transform;
                root.SetParent(_row, false);
                root.localPosition = new Vector3(RuneX(i), 0f, 0f);
                var rune = new Rune { root = root };
                BuildPoke(rune, i, PlateSize(i));
                _runes[i] = rune;
            }
            // Nov 10 (playtest: the line ran into Run and through props): its own billboard over the wild Isling, on top of
            // everything like the battle labels, away from the moves.
            _captionRoot = new GameObject("Caption").transform;
            _captionRoot.SetParent(transform, false);
            _caption = MakeText(_captionRoot, captionFontSize * captionScale);
            _caption.color = captionText;
            _caption.outlineWidth = 0.08f;
            _captionTextMat = new Material(_caption.fontSharedMaterial) { name = "Battle Line (on top)", renderQueue = 3995 };
            _captionTextMat.SetFloat(ShaderUtilities.ShaderTag_ZTestMode, (float)UnityEngine.Rendering.CompareFunction.Always);
            _caption.fontSharedMaterial = _captionTextMat;
            // Step 16: a dark rounded pill behind the line (it was white text on the busy island).
            var pillGo = new GameObject("Caption Pill");
            pillGo.transform.SetParent(_captionRoot, false);
            pillGo.transform.localPosition = new Vector3(0f, 0f, 0.0008f);
            pillGo.AddComponent<MeshFilter>().sharedMesh = _quad;
            _captionPill = pillGo.AddComponent<MeshRenderer>();
            if (hudMaterial != null)
            {
                _captionPillMat = new Material(hudMaterial) { name = "Battle Line Pill (on top)", renderQueue = 3990 };
                _captionPillMat.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                _captionPill.sharedMaterial = _captionPillMat;
            }
            _captionPill.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            pillGo.SetActive(false);

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
        /// <summary>Plate size: Run is narrower.</summary>
        /// <summary>Step 14j: how much a looked-at (and pulsing) move grows.</summary>
        public const float HoverGrow = 0.2f;

        /// <summary>Step 14j: the row against the tablet layout rule (<see cref="TabletLayout"/>).</summary>
        public System.Collections.Generic.List<string> LayoutProblems()
        {
            var rects = new System.Collections.Generic.List<TabletLayout.Rect>();
            for (int i = 0; i < Count; i++)
                rects.Add(new TabletLayout.Rect { label = i == RunIndex ? "Run" : $"Move {i + 1}", center = new Vector2(RuneX(i), 0f), size = PlateSize(i), grow = HoverGrow });
            return TabletLayout.Problems(rects);
        }

        Vector2 PlateSize(int index) => index == RunIndex ? new Vector2(runeSize.x * runWidth, runeSize.y) : runeSize;

        /// <summary>Rune centre along the row: three moves at <see cref="runeStep"/>, Run tucked closer at the right end.</summary>
        float RuneX(int index)
        {
            float runGap = runeStep - runeSize.x * (1f - runWidth) * 0.5f;
            float width = 2f * runeStep + runGap;
            float x = index == RunIndex ? 2f * runeStep + runGap : index * runeStep;
            return x - width * 0.5f;
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
                rune.label = MakeText(rune.tablet.transform, fontSize);
                rune.label.rectTransform.sizeDelta = new Vector2(size.x * 0.92f, size.y);
                TabletKit.StyleLabel(rune.label);
                TextRule.Fit(rune.label, fontSize);
                return;
            }
            var plateGo = new GameObject("Plate");
            plateGo.transform.SetParent(visual, false);
            plateGo.transform.localScale = new Vector3(size.x, size.y, 1f);
            plateGo.AddComponent<MeshFilter>().sharedMesh = _quad;
            rune.plate = plateGo.AddComponent<MeshRenderer>();
            rune.plate.sharedMaterial = hudMaterial;
            rune.plate.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rune.label = MakeText(visual, fontSize);
            rune.label.transform.localPosition = new Vector3(0f, 0f, -0.001f);
        }

        void OnPokeState(int index, InteractableStateChangeArgs args)
        {
            // Step 29: off-turn, no hover sound.
            if (args.NewState == InteractableState.Hover && _interactable && index >= 0 && index < _runes.Length && _runes[index].shown)
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
            if (!_interactable && index >= 0 && index < _runes.Length && _runes[index].shown && !GamePause.IsPaused)
            {
                OffTurnPress(index, "poke");
                return;
            }
            if (_interactable && !GamePause.IsPaused && _runes[index].shown && _runes[index].appear > 0.6f)
            {
                if (PodBusy) Debug.Log($"[BattleRunes] Poke on rune {index + 1} ignored: a hand is on a pod (switch).");
                else Pick(index, "poke");
            }
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
        float HalfRow => Mathf.Max(-RuneX(0) + runeSize.x * 0.5f, RuneX(RunIndex) + PlateSize(RunIndex).x * 0.5f);

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
        public void Show(MoveData[] moves, Element element = Element.Neutral)
        {
            bool wasHidden = !gameObject.activeSelf;
            Open();
            BeatMove = BeatIndex(moves, element);
            for (int i = 0; i < 3; i++)
            {
                var m = moves != null && i < moves.Length ? moves[i] : null;
                var r = _runes[i];
                r.usable = m != null;
                r.beat = i == BeatMove;
                r.tint = m != null ? ElementStyle.Of(m.element).main : Color.gray;
                string kind = m == null ? "" : m.kind == MoveKind.Support ? "Support" : m.element.ToString();
                r.label.text = m != null ? $"<b>{m.Name}</b>\n<size=70%>{kind}</size>" : "-";
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
            if (on && !_interactable) _risePulse = 1f;
            _interactable = on;
            for (int i = 0; i < 2; i++) _wasPinching[i] = true; // a pinch already held doesn't count
            Paint();
        }

        public void SetCaption(string text)
        {
            if (_caption != null) _caption.text = text;
            FitPill(text);
        }

        /// <summary>Step 16: the pill hugs the line (a little padding), hidden with no text.</summary>
        void FitPill(string text)
        {
            if (_captionPill == null || _caption == null) return;
            bool on = !string.IsNullOrEmpty(text);
            _captionPill.gameObject.SetActive(on);
            if (!on) return;
            Vector2 size = _caption.GetPreferredValues(text, 10f, 10f);
            var pill = PillSize(size, captionFontSize * captionScale);
            _captionPill.transform.localScale = new Vector3(pill.x, pill.y, 1f);
            var block = new MaterialPropertyBlock();
            block.SetColor(BackColorId, captionPill);
            block.SetColor(BorderColorId, new Color(1f, 0.85f, 0.5f, 0.35f));
            block.SetFloat(BorderId, 0.05f);
            block.SetFloat(FillId, 0f);
            block.SetFloat(AspectId, pill.x / Mathf.Max(1e-4f, pill.y));
            block.SetFloat(RadiusId, 0.5f);
            _captionPill.SetPropertyBlock(block);
        }

        /// <summary>Step 16: the caption pill's size for a text size (padding grows with the font).</summary>
        public static Vector2 PillSize(Vector2 text, float font) => new Vector2(text.x + font * 0.18f, text.y + font * 0.08f);

        void Update()
        {
            // Step 21 (Mugesh, battle + menu shot: the menu row lay over the moves and the battle line): the battle row
            // and its line step aside while the game menu (or its Journal / Settings) is open, and come back on close.
            bool menu = GameMenu.IsOpen;
            if (_row.gameObject.activeSelf == menu) _row.gameObject.SetActive(!menu);
            if (_captionRoot.gameObject.activeSelf == menu) _captionRoot.gameObject.SetActive(!menu);
            PlaceRow(false);
            int was = _hovered;
            _hovered = _interactable ? GazeHover() : -1;
            if (!_interactable && _reticle != null) _reticle.enabled = false;
            _up = Mathf.MoveTowards(_up, _interactable ? 1f : 0f, Time.deltaTime / Mathf.Max(0.01f, turnSeconds));
            _risePulse = Mathf.MoveTowards(_risePulse, 0f, Time.deltaTime / 0.9f);
            float sink = 1f - Mathf.SmoothStep(0f, 1f, _up);
            if (_hovered >= 0 && _hovered != was) GameAudio.At(AudioIds.RuneHover, _runes[_hovered].visual.position);
            for (int i = 0; i < _runes.Length; i++)
            {
                var r = _runes[i];
                r.hover = Mathf.MoveTowards(r.hover, i == _hovered ? 1f : 0f, Time.deltaTime * 8f);
                bool popping = r.shown && Time.time >= r.appearAt;
                r.appear = Mathf.MoveTowards(r.appear, popping ? 1f : 0f, Time.deltaTime / (popping ? 0.25f : 0.15f));
                float a = r.shown ? EaseOutBack(r.appear) : r.appear;
                r.visual.gameObject.SetActive(r.appear > 0.001f);
                // Scale the visual only: the poke surface keeps its size. Nov 11: the Isling's element move breathes on its turn.
                float beat = r.beat && _interactable && r.usable ? BeatWave(Time.time, beatSeconds) : 0f;
                r.visual.localScale = Vector3.one * (Mathf.Max(0.001f, a) * (1f + 0.12f * r.hover + 0.08f * r.pulse + (beatScale - 1f) * beat));
                r.press = Mathf.MoveTowards(r.press, 0f, Time.deltaTime / 0.3f);
                r.shake = Mathf.MoveTowards(r.shake, 0f, Time.deltaTime / 0.3f);
                float wobble = r.shake > 0f ? Mathf.Sin(Time.time * 70f) * 0.004f * r.shake : 0f;
                r.root.localPosition = new Vector3(RuneX(i) + wobble, -offTurnSink * sink, offTurnBack * sink);
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
                // Step 21 (Mugesh: picking a pod to switch pressed a move, he could never switch): the pod grab is a pinch
                // too, so no gaze pick while a hand reaches for, holds or has just let go of a pod.
                bool busy = grabGuard != null && grabGuard.IsHandBusy(h == 0) || PodBusy;
                if (pinching && !_wasPinching[h] && PodBusy && _hovered >= 0)
                    Debug.Log($"[BattleRunes] Pinch on rune {_hovered + 1} ignored: a hand is on a pod (switch).");
                if (pinching && !_wasPinching[h] && !busy && _hovered >= 0) Pick(_hovered, $"gaze + pinch ({(h == 0 ? "left" : "right")})");
                _wasPinching[h] = pinching;
                if (!_interactable) return;
            }
        }

        /// <summary>Picks from code (testing). Ignored unless the runes are waiting for a choice (step 29: off-turn it only shakes).</summary>
        public void Choose(int index)
        {
            if (_interactable) Pick(index, "code");
            else OffTurnPress(index, "code");
        }

        /// <summary>Step 29 (playtests: moves pressed during the wild Isling's turn): a tiny shake, nothing queued, no sound.</summary>
        void OffTurnPress(int index, string how)
        {
            if (index < 0 || index >= _runes.Length || !_runes[index].shown) return;
            _runes[index].shake = 1f;
            OffTurnPresses++;
            Debug.Log($"[BattleRunes] {(index == RunIndex ? "Run" : $"Move {index + 1}")} pressed off-turn by {how}: shake, nothing queued.");
        }

        void Pick(int index, string how)
        {
            if (index < 0 || index >= _runes.Length || !_runes[index].usable || !_runes[index].shown) return;
            if (!_interactable)
            {
                OffTurnPress(index, how);
                return;
            }
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
        /// at battle start (yaw on the island), then stays (step 14i, <see cref="BoardFacing"/>); it pitches back so its face
        /// points at the eyes (0 to <see cref="maxTilt"/>).
        /// </summary>
        void PlaceRow(bool snap)
        {
            if (island == null || island.map == null) return;
            var b = island.LocalBounds;
            var cam = Camera.main;
            // Step 24: over the arena the row has nowhere to dodge to (avoidOffset would push it back over the rim).
            if (!overArena && cam != null && Covers(cam.transform.position)) _avoid = Mathf.MoveTowards(_avoid, 1f, Time.deltaTime * 3f);
            bool reach = ReachingForPod();
            if (reach || (pods != null && pods.AnyHeld)) _podFreeAt = Time.unscaledTime + PodGrace;
            _duck = Mathf.MoveTowards(_duck, reach ? 1f : 0f, Time.deltaTime * (_duck < 0.5f ? 6f : 2f));
            Vector3 rest = overArena ? ArenaRest(b.center.x) : new Vector3(b.center.x, island.map.capHeight, b.max.z) + offset + avoidOffset * _avoid;
            _row.localPosition = rest + duckOffset * Mathf.SmoothStep(0f, 1f, _duck);
            PlaceCaption(cam);
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
                    _facing.reFaceAngle = turnAwayAngle;
                    if (snap)
                    {
                        _facing.Open(headYaw);
                        _yaw = headYaw;
                        _tilt = _tiltTarget = tilt;
                    }
                    else
                    {
                        bool handNear = BoardFacing.NearestHand(_row, new Vector2(-0.16f, -0.02f), new Vector2(0.16f, 0.14f)) < _facing.handClear;
                        if (_facing.Step(headYaw, handNear, Time.deltaTime))
                        {
                            Debug.Log("[BattleRunes] The head turned away for 1 s, no hand near: facing it again.");
                            _tiltTarget = tilt;
                        }
                        float k = 1f - Mathf.Exp(-Time.deltaTime * 7.5f);
                        _yaw = Mathf.LerpAngle(_yaw, _facing.Yaw, k);
                        _tilt = Mathf.Lerp(_tilt, _tiltTarget, k);
                    }
                }
            }
            if (lockedToIsland)
            {
                _yaw = 180f;
                _tilt = lockedTilt;
            }
            _row.rotation = island.transform.rotation * Quaternion.Euler(_tilt, _yaw, 0f);
        }

        /// <summary>
        /// Step 24: the row's centre with its bottom (front) edge <see cref="arenaInset"/> inside the footprint's front edge and
        /// <see cref="arenaHover"/> over the cap. The row leans back by <see cref="lockedTilt"/>, so the bottom edge is its nearest.
        /// </summary>
        Vector3 ArenaRest(float x)
        {
            float rad = lockedTilt * Mathf.Deg2Rad, half = runeSize.y * 0.5f;
            return new Vector3(x, island.map.capHeight + arenaHover + half * Mathf.Cos(rad), island.FootprintHalfSize.y - arenaInset - half * Mathf.Sin(rad));
        }

        [Tooltip("Nov 10: the battle line sits this far over the wild Isling's head (island-local m).")]
        public float captionOverHead = 0.03f;
        [Tooltip("Nov 10: and stays under this much of the view's height (0..1).")]
        [Range(0.5f, 1f)] public float captionMaxViewportY = 0.86f;
        Transform _captionRoot;
        Material _captionTextMat, _captionPillMat;

        /// <summary>
        /// The battle line: above the wild Isling (the spot the Spirit's battle words read best), facing the head. Nov 12
        /// (Mugesh, shot 04: "Choose your attack" sat behind Galeowl's head: a Keeper is so tall that the view's top edge
        /// pushed the line down into it): when that spot covers a creature, an HP plate or the moves on screen, the line
        /// goes beside the wild Isling (left, then right), else over everything; a clear spot is kept while it stays clear.
        /// </summary>
        void PlaceCaption(Camera cam)
        {
            if (_captionRoot == null) return;
            var b = island.LocalBounds;
            Vector3 local = _avoidB != null ? _avoidB.LocalTop + Vector3.up * captionOverHead
                : new Vector3(b.center.x, island.map.capHeight + 0.14f, b.min.z + (b.max.z - b.min.z) * 0.35f);
            Vector3 world = island.transform.TransformPoint(local);
            if (cam != null)
            {
                Vector3 up = BattleHud.LabelUp(cam);
                bool shown = _captionPill != null && _captionPill.gameObject.activeSelf;
                if (_avoidB != null && shown)
                {
                    // Nov 12 evening (Mugesh: the line shook with Galeowl and sometimes went inside it): the spot is found from
                    // the body alone and held in the island's space; it is re-checked twice a second and only moves when the
                    // held spot is covered.
                    if (!_captionHeldOk || Time.time >= _captionCheckAt)
                    {
                        _captionCheckAt = Time.time + captionRecheckSeconds;
                        _captionHeld = island.transform.InverseTransformPoint(ClearCaptionSpot(cam, world, up));
                        _captionHeldOk = true;
                    }
                    world = island.transform.TransformPoint(_captionHeld);
                }
                else
                {
                    _captionHeldOk = false;
                    world = UnderTop(cam, world);
                }
                Vector3 away = world - cam.transform.position;
                if (away.sqrMagnitude > 1e-8f) _captionRoot.rotation = Quaternion.LookRotation(away, up);
            }
            _captionRoot.position = world;
        }

        Vector3 UnderTop(Camera cam, Vector3 world)
        {
            Vector3 v = cam.WorldToViewportPoint(world);
            return v.z > 0f && v.y > captionMaxViewportY ? cam.ViewportToWorldPoint(new Vector3(v.x, captionMaxViewportY, v.z)) : world;
        }

        readonly System.Collections.Generic.List<Rect> _captionKeepOff = new System.Collections.Generic.List<Rect>();
        readonly System.Collections.Generic.List<Bounds> _captionPlates = new System.Collections.Generic.List<Bounds>();
        readonly Vector3[] _captionSpots = new Vector3[6];
        int _captionSpot;
        [Tooltip("Nov 12 evening: the battle line's held spot is re-checked this often (s).")]
        public float captionRecheckSeconds = 0.5f;
        Vector3 _captionHeld;
        bool _captionHeldOk;
        float _captionCheckAt;
        BattleHud _captionHud;
        DialogueBubble _captionBubble;

        /// <summary>Nov 12: which spot the battle line took (0 over the head, 1 left, 2 right, 3 over everything, 4 low left, 5 low right; tests, the bridge).</summary>
        public int CaptionSpot => _captionSpot;

        /// <summary>Nov 12: the first spot (over the head, left, right, over everything) whose pill covers nothing to keep clear.</summary>
        Vector3 ClearCaptionSpot(Camera cam, Vector3 over, Vector3 up)
        {
            Vector3 fwd = (over - cam.transform.position).normalized;
            Vector3 right = Vector3.Cross(up, fwd).normalized;
            float s = _captionRoot.lossyScale.x;
            var ps = _captionPill.transform.localScale;
            float hw = ps.x * s * 0.5f, hh = ps.y * s * 0.5f, gap = 0.008f * island.transform.lossyScale.x;

            _captionKeepOff.Clear();
            KeepOff(cam, _avoidA);
            KeepOff(cam, _avoidB);
            if (_captionHud == null) _captionHud = FindFirstObjectByType<BattleHud>();
            if (_captionHud != null)
            {
                _captionHud.PlateBounds(_captionPlates);
                foreach (var pb in _captionPlates) _captionKeepOff.Add(ViewRect(cam, pb));
            }
            if (TryGetRowBounds(out var row)) _captionKeepOff.Add(ViewRect(cam, row));
            if (_captionBubble == null) _captionBubble = FindFirstObjectByType<DialogueBubble>();
            if (_captionBubble != null && _captionBubble.TryGetPanelBounds(out var bubble)) _captionKeepOff.Add(ViewRect(cam, bubble));

            _captionSpots[0] = UnderTop(cam, over);
            bool hasBody = _avoidB.TryGetBodyBounds(out var vb);
            if (!hasBody) vb = new Bounds(over, Vector3.zero);
            if (hasBody)
            {
                var ar = new Vector3(Mathf.Abs(right.x), Mathf.Abs(right.y), Mathf.Abs(right.z));
                float halfW = Vector3.Dot(vb.extents, ar);
                Vector3 chest = vb.center + up * (vb.extents.y * 0.45f);
                _captionSpots[1] = chest - right * (halfW + hw + gap);
                _captionSpots[2] = chest + right * (halfW + hw + gap);
                // Over everything: the wild Isling and every HP plate.
                float top = Mathf.Abs(Vector3.Dot(vb.extents, new Vector3(Mathf.Abs(up.x), Mathf.Abs(up.y), Mathf.Abs(up.z))));
                foreach (var pb in _captionPlates)
                    for (int i = 0; i < 8; i++)
                    {
                        var q = pb.center + Vector3.Scale(pb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                        top = Mathf.Max(top, Vector3.Dot(q - vb.center, up));
                    }
                _captionSpots[3] = vb.center + up * (top + hh + gap);
                // Beside it and lower (the first battle: the Spirit's bubble over its head reached the chest spot, and the
                // line went onto the wild Isling's plate for 2.6 s): at its waist, on either side.
                Vector3 waist = vb.center - up * (vb.extents.y * 0.25f);
                _captionSpots[4] = waist - right * (halfW + hw + gap);
                _captionSpots[5] = waist + right * (halfW + hw + gap);
            }
            else _captionSpots[1] = _captionSpots[2] = _captionSpots[3] = _captionSpots[4] = _captionSpots[5] = _captionSpots[0];

            float Cost(Vector3 at)
            {
                var a = cam.WorldToViewportPoint(at - right * hw - up * hh);
                var c = cam.WorldToViewportPoint(at + right * hw + up * hh);
                if (a.z <= 0f || c.z <= 0f) return 1e6f;
                var r = Rect.MinMaxRect(Mathf.Min(a.x, c.x), Mathf.Min(a.y, c.y), Mathf.Max(a.x, c.x), Mathf.Max(a.y, c.y));
                float cost = 0f;
                foreach (var k in _captionKeepOff)
                    if (r.Overlaps(k))
                        cost += (Mathf.Min(r.xMax, k.xMax) - Mathf.Max(r.xMin, k.xMin)) * (Mathf.Min(r.yMax, k.yMax) - Mathf.Max(r.yMin, k.yMin));
                // Off the view (or over its top limit) counts too.
                cost += Mathf.Max(0f, r.yMax - captionMaxViewportY) * r.width + Mathf.Max(0f, 0.02f - r.xMin) * r.height + Mathf.Max(0f, r.xMax - 0.98f) * r.height;
                return cost;
            }

            // The held spot stays while it covers next to nothing (a tenth of the pill): no hopping with the owl's bob.
            if (_captionHeldOk)
            {
                Vector3 held = island.transform.TransformPoint(_captionHeld);
                var a0 = cam.WorldToViewportPoint(held - right * hw - up * hh);
                var c0 = cam.WorldToViewportPoint(held + right * hw + up * hh);
                float pillArea = Mathf.Abs((c0.x - a0.x) * (c0.y - a0.y));
                if (Cost(held) <= pillArea * 0.1f) return held;
            }
            if (Cost(_captionSpots[_captionSpot]) <= 1e-6f) return _captionSpots[_captionSpot];
            // Beside the wild Isling: the side away from its HP plate first (the plate sits beside it too).
            bool plateLeft = false;
            float nearest = float.MaxValue;
            foreach (var pb in _captionPlates)
            {
                float d = (pb.center - vb.center).sqrMagnitude;
                if (d >= nearest) continue;
                nearest = d;
                plateLeft = Vector3.Dot(pb.center - vb.center, right) < 0f;
            }
            int[] order = plateLeft ? new[] { 0, 2, 5, 3, 1, 4 } : new[] { 0, 1, 4, 3, 2, 5 };
            int best = 0;
            float bestCost = float.MaxValue;
            foreach (int i in order)
            {
                float c = Cost(_captionSpots[i]);
                if (c <= 1e-6f)
                {
                    best = i;
                    break;
                }
                if (c < bestCost)
                {
                    bestCost = c;
                    best = i;
                }
            }
            if (best != _captionSpot)
                Debug.Log($"[BattleRunes] The battle line moves {(best == 0 ? "over the wild Isling's head" : best == 1 ? "to its left" : best == 2 ? "to its right" : best == 3 ? "over everything" : best == 4 ? "low to its left" : "low to its right")} (clear of the creatures, plates and moves).");
            _captionSpot = best;
            return _captionSpots[best];
        }

        void KeepOff(Camera cam, StageActor actor)
        {
            if (actor != null && actor.TryGetBodyBounds(out var vb)) _captionKeepOff.Add(ViewRect(cam, vb));
        }

        static Rect ViewRect(Camera cam, Bounds b)
        {
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            for (int i = 0; i < 8; i++)
            {
                var p = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var v = cam.WorldToViewportPoint(p);
                if (v.z <= 0f) continue;
                x0 = Mathf.Min(x0, v.x);
                y0 = Mathf.Min(y0, v.y);
                x1 = Mathf.Max(x1, v.x);
                y1 = Mathf.Max(y1, v.y);
            }
            return x1 >= x0 ? Rect.MinMaxRect(x0, y0, x1, y1) : new Rect(-10f, -10f, 0f, 0f);
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
            bool near = Mathf.Abs(hit.x) < HalfRow + runeStep && Mathf.Abs(hit.y) < runeSize.y * 2.5f;
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
                float hx = Mathf.Max(PlateSize(i).x * 0.5f, min), hy = Mathf.Max(runeSize.y * 0.5f, min);
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
                    float grey = offTurnGrey * (1f - Mathf.SmoothStep(0f, 1f, _up));
                    var tint = Color.Lerp(r.tint, Grey(r.tint), grey);
                    var inlay = tint;
                    inlay.a = i == RunIndex ? 0f : 0.9f;
                    r.tablet.inlay = inlay;
                    // Nov 4 (Mugesh: only a coloured border): the whole face takes the move's element colour.
                    var face = tint;
                    face.a = i == RunIndex ? 0f : 0.8f;
                    r.tablet.face = face;
                    // Step 29: one soft gold rim pulse as the moves rise for the player's turn. Nov 11: the element move keeps glowing with its breath.
                    float beat = r.beat && on ? BeatWave(Time.time, beatSeconds) : 0f;
                    r.tablet.Paint(r.hover, on ? 0f : 0.65f, 0f, Mathf.Max(0.7f * r.pulse, r.usable ? 0.8f * Mathf.Sin(_risePulse * Mathf.PI) : 0f, 0.25f + 0.55f * beat));
                    var lc = Color.Lerp(TabletKit.LabelColor, TabletKit.LabelLit, Mathf.Max(r.hover, r.pulse, 0.5f * beat));
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
                _block.SetFloat(AspectId, PlateSize(i).x / runeSize.y);
                _block.SetFloat(RadiusId, 0.3f);
                _block.SetFloat(BorderId, 0.07f + 0.05f * r.hover + 0.04f * glow);
                r.plate.SetPropertyBlock(_block);
                var c = r.label.color;
                c.a = Mathf.Lerp(0.5f, 1f, dim);
                r.label.color = c;
            }
        }

        static Color Grey(Color c)
        {
            float l = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
            return new Color(l, l, l, c.a);
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
