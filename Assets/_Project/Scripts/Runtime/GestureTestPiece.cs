using System;
using System.Collections.Generic;
using Diorama.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Diorama
{
    public enum MoveMode
    {
        // Step 12a: SmartStep took AutoWalk's slot (0), so the scene moves over to it; old saves are mapped in GameSave.
        [Tooltip("Step 12a: a swipe walks a few tiles along the path (stops at junctions, things to use, grass, 5 tiles); two quick swipes keep walking until a tap.")]
        SmartStep,
        [Tooltip("Swipe starts walking; it keeps going only while the thumb stays off-centre.")]
        HoldToWalk,
        [Tooltip("Swipe starts walking; it keeps going until another swipe, a tap or a blocked tile.")]
        AutoWalk,
    }

    /// <summary>
    /// The explorer (step 9f: the teal-hooded kid, <see cref="explorerPrefab"/> with <see cref="ExplorerView"/>; the old
    /// placeholder cube only when it is missing): walks the world map tile by tile, shown through the island window, plus a
    /// small world-space debug label beside the island (last gesture, tile, zoom, placement; see <see cref="DebugLabels"/>).
    /// <see cref="GoTo"/> walks an A* path to a tile (<see cref="ExplorerPath"/>; used by TileTouch and TileGaze); any
    /// swipe or tap cancels it.
    /// AutoWalk: a swipe sets the walking direction and the cube keeps walking until another swipe, a blocked tile
    /// (water, tree, rock, border, blocking prop), or a tap stops it. HoldToWalk: same, but it also stops when the thumb returns to
    /// rest (read from ThumbHoldProbe; in the Editor, releasing the arrow key). In both modes a second swipe in the
    /// same direction while moving switches to run, and turning keeps the current speed.
    /// Tap is context-sensitive: walking = stop; standing and facing water = fish (<see cref="OnFishTap"/>); otherwise
    /// a little hop. The battle zoom is triggered by encounters (<see cref="EncounterDirector"/>), which also lock the
    /// explorer while a battle runs; Z toggles the zoom in the Editor for testing. No walking while zoomed.
    /// Swipes map to the user's view of the island: forward = away, back = toward, left / right = their left / right.
    /// </summary>
    public class GestureTestPiece : MonoBehaviour
    {
        public IslandBuilder island;
        public WorldWindow window;
        public MicrogestureInput input;
        public IslandPlacer placer;
        public ThumbHoldProbe probe;
        public Material cubeMaterial;
        [Tooltip("Step 9f: the baked explorer (Prefabs/Creatures/Explorer, with ExplorerView). Missing = the old cube and an error.")]
        public CreatureView explorerPrefab;

        [Tooltip("Can be switched live in Play mode.")]
        public MoveMode moveMode = MoveMode.SmartStep;

        [Tooltip("Start tile (column, row) when the island has no world map; a world map has its own start tile.")]
        public Vector2Int startTile = new Vector2Int(10, 7);

        public float cubeSize = 0.018f;   // map metres at zoom 1
        [Tooltip("Seconds per tile when walking.")]
        public float walkSeconds = 0.4f;
        [Tooltip("Seconds per tile when running.")]
        public float runSeconds = 0.2f;

        [Header("Smart steps (step 12a)")]
        [Tooltip("Seconds per tile on a smart step (one swipe).")]
        public float smartSeconds = 0.4f;
        [Tooltip("Seconds per tile when walking on (two quick swipes).")]
        public float continuousSeconds = 0.4f;
        [Tooltip("Most tiles one swipe walks.")]
        [Min(1)] public int smartTiles = SmartStep.SmartTiles;
        [Tooltip("A second swipe the same way within this many seconds keeps walking.")]
        public float doubleSwipeSeconds = 0.45f;

        [Header("Go to tile")]
        [Tooltip("Longest path GoTo will walk, in steps.")]
        public int maxPathSteps = ExplorerPath.DefaultMaxSteps;
        [Tooltip("Paths longer than this many steps are run instead of walked.")]
        public int runPathSteps = 8;

        [Header("Debug")]
        [Tooltip("Shows the world-space debug labels at start. The dev panel's Tools tab toggles them.")]
        public bool showDebugLabels;

        [Header("Hold to walk")]
        [Tooltip("Stop when the thumb comes back within this distance (mm) of its rest point.")]
        public float holdReleaseMm = 5f;
        [Tooltip("Ignore the hold check for this long after a swipe, while the thumb settles.")]
        public float holdGraceSeconds = 0.25f;

        [Tooltip("Hop height in map metres when tapping while standing.")]
        public float hopHeight = 0.012f;

        /// <summary>
        /// Step 11d: things the explorer stands on above the cap at a map point (map metres; the highest wins). Lilymere's
        /// lily pad bridge: it steps up onto the pads and hops from one to the next. None = the cap, as before.
        /// </summary>
        public readonly System.Collections.Generic.List<System.Func<Vector2, float>> groundLifts = new System.Collections.Generic.List<System.Func<Vector2, float>>();

        float GroundLift(Vector2 p)
        {
            float lift = 0f;
            foreach (var g in groundLifts)
                if (g != null) lift = Mathf.Max(lift, g(p));
            return lift;
        }

        /// <summary>Raised when the explorer arrives on a tall grass tile (column, row).</summary>
        public event Action<Vector2Int> OnTallGrassStep;

        /// <summary>Raised whenever the explorer arrives on a tile (column, row).</summary>
        public event Action<Vector2Int> OnStep;

        /// <summary>Raised when a walk is stopped by a prop (the blocked tile): walking into a building's doorway.</summary>
        public event Action<Vector2Int> OnBump;

        /// <summary>Raised on a tap while standing next to water and facing it (the water tile).</summary>
        public event Action<Vector2Int> OnFishTap;

        /// <summary>The GoTo target changed: a tile, or null when the explorer arrived or the path was cancelled.</summary>
        public event Action<Vector2Int?> DestinationChanged;

        /// <summary>A thumb tap stopped a walk (or a GoTo path).</summary>
        public event Action Stopped;

        /// <summary>
        /// Step 12a: extra smart-step stops (from tile, next tile), for a scene's own interactables. Gates and unsolved hand
        /// moments are built in (<see cref="BuiltInStop"/>).
        /// </summary>
        public readonly List<Func<Vector2Int, Vector2Int, WalkStop>> walkStops = new List<Func<Vector2Int, Vector2Int, WalkStop>>();

        /// <summary>Step 12a: a new smart-step plan (the footprints redraw).</summary>
        public event Action PlanChanged;

        /// <summary>Step 12a: the tiles the current smart step will walk (empty when none).</summary>
        public IReadOnlyList<Vector2Int> Plan => _plan;

        /// <summary>Step 12a: how many plan tiles have been stepped onto (the one being stepped onto counts).</summary>
        public int PlanIndex => _planIndex;

        /// <summary>Step 12a: walking on (two quick swipes) rather than a smart step.</summary>
        public bool Continuous => _continuous;

        /// <summary>Step 12a: the tile the current plan starts from.</summary>
        public Vector2Int PlanFrom { get; private set; }

        /// <summary>Step 12a: why the current plan ends (<see cref="SmartStep.Cap"/>, junction, ...).</summary>
        public string PlanEnd { get; private set; }

        /// <summary>Step 12a: smart steps started by a swipe (the walk lessons count them).</summary>
        public int SmartStepsTaken { get; private set; }

        /// <summary>Step 12a: a fresh swipe or the walk-on upgrade (true = walk on).</summary>
        public event Action<bool> Swiped;

        readonly List<Vector2Int> _plan = new List<Vector2Int>();
        int _planIndex;
        bool _continuous;
        Vector2Int _lastSwipe;
        float _lastSwipeAt = -99f;
        HandPuzzle[] _pieces;
        float _piecesAt = -99f;

        Transform _cube;
        ExplorerView _view;

        /// <summary>Step 10b: the explorer's body (its visual, on the island), or null before Start.</summary>
        public Transform Body => _cube;
        float _yaw = 180f;
        TextMeshPro _label;
        Vector2Int _tile, _fromTile;
        Vector2Int _heading;
        Vector2Int _facing = new Vector2Int(0, -1);
        bool _running;
        bool _locked;
        float _hopStart = -1f;
        GestureSource _source;
        float _swipeTime;
        float _stepT = 1f;
        int _count;
        string _last = "none yet";
        bool _zoomedIn;
        readonly List<Vector2Int> _path = new List<Vector2Int>();
        int _pathIndex;
        Vector2Int? _destination;

        IslandMap Map => island.Content;

        /// <summary>The tile the explorer stands on (or is stepping onto).</summary>
        public Vector2Int Tile => _tile;

        /// <summary>Direction of the last swipe (column, row step); starts facing away from the player.</summary>
        public Vector2Int Facing => _facing;

        /// <summary>The tile a GoTo path is heading for, if any.</summary>
        public Vector2Int? Destination => _destination;

        /// <summary>The last step (column, row), zero after a teleport.</summary>
        public Vector2Int LastStep => _tile - _fromTile;

        /// <summary>The direction a swipe walks (zero while standing or following a GoTo path).</summary>
        public Vector2Int Heading => _heading;

        /// <summary>Turns on the spot (step 9h: a battle starts facing the wild Isling). Ignored while walking.</summary>
        public void Face(Vector2Int direction)
        {
            if (direction.sqrMagnitude != 1 || _stepT < 1f) return;
            _facing = direction;
            UpdateExplorer();
        }

        /// <summary>False while a battle or the battle zoom is on, or a hand moment holds it (no walking, no GoTo).</summary>
        public bool CanMove => !_locked && !_heldStill && !Zoomed;

        /// <summary>
        /// Step 14f: a hand moment that needs every pinch (untangling Galeowl) holds the explorer still: gestures and walks
        /// are ignored like in a battle, but <see cref="Locked"/> stays false (hand moments read it as "a battle is on").
        /// </summary>
        public bool HeldStill
        {
            get => _heldStill;
            set
            {
                if (value == _heldStill) return;
                _heldStill = value;
                if (value)
                {
                    Stop();
                    CancelPath();
                }
                Debug.Log($"[GestureTestPiece] {(value ? "Held still by a hand moment" : "Free to walk again")}.");
                RefreshLabel();
            }
        }

        bool _heldStill;

        /// <summary>
        /// Where the explorer stands relative to its tile, in tiles (battles move it behind the party creature's pad).
        /// It slides there smoothly; set back to zero afterwards.
        /// </summary>
        public Vector2 StandOffset { get; set; }

        /// <summary>Scales the explorer (scene swaps shrink it into its tile and pop it up on the entry tile).</summary>
        public float Shrink { get; set; } = 1f;

        Vector2 _standOffset;

        /// <summary>While locked (battles) all gestures are ignored and the explorer stands still.</summary>
        public bool Locked
        {
            get => _locked;
            set
            {
                _locked = value;
                if (value)
                {
                    Stop();
                    CancelPath();
                }
                RefreshLabel();
            }
        }

        void Start()
        {
            if (window == null) Debug.LogError("[GestureTestPiece] No WorldWindow assigned; the map won't scroll or zoom.");
            _tile = _fromTile = SpawnTile;
            if (!Map.CanWalk(_tile))
                Debug.LogWarning($"[GestureTestPiece] Start tile c{_tile.x} r{_tile.y} is '{Map.At(_tile.y, _tile.x)}', not walkable.");

            if (explorerPrefab != null)
            {
                var view = Instantiate(explorerPrefab, island.transform, false);
                view.name = "Explorer";
                _view = view.GetComponent<ExplorerView>();
                if (_view == null) _view = view.gameObject.AddComponent<ExplorerView>();
                _cube = view.transform;
            }
            else
            {
                Debug.LogError("[GestureTestPiece] No explorer prefab assigned (run Step9fSetup.Explorer); showing the placeholder cube.", this);
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Gesture Test Cube";
                Destroy(go.GetComponent<Collider>());
                if (cubeMaterial != null) go.GetComponent<MeshRenderer>().sharedMaterial = cubeMaterial;
                _cube = go.transform;
                _cube.SetParent(island.transform, false);
            }

            var labelGo = new GameObject("Gesture Label");
            labelGo.transform.SetParent(island.transform, false);
            _label = labelGo.AddComponent<TextMeshPro>();
            _label.fontSize = 0.25f;
            _label.alignment = TextAlignmentOptions.Left;
            _label.color = Color.white;
            _label.outlineWidth = 0.2f;
            _label.outlineColor = new Color32(20, 30, 25, 255);
            _label.rectTransform.sizeDelta = new Vector2(0.22f, 0.12f);
            DebugLabels.Visible = showDebugLabels;
            DebugLabels.Track(labelGo);

            input.Gesture += OnGesture;
            if (placer != null) placer.Placed += PositionLabel;
            if (probe == null && moveMode == MoveMode.HoldToWalk) Debug.LogError("[GestureTestPiece] No ThumbHoldProbe assigned; HoldToWalk will stop right after each swipe.");
            PositionLabel();
            UpdateExplorer();
            RefreshLabel();
        }

        void OnDestroy()
        {
            if (input != null)
            {
                input.Gesture -= OnGesture;
            }
            if (placer != null) placer.Placed -= PositionLabel;
        }

        bool Zoomed => window != null && window.IsZoomed;


        /// <summary>
        /// Walks to <paramref name="target"/> along the shortest walkable path (run when it is longer than
        /// <see cref="runPathSteps"/>). False when the explorer can't move now or the tile can't be reached.
        /// </summary>
        public bool GoTo(Vector2Int target)
        {
            if (!CanMove || target == _tile) return false;
            if (!ExplorerPath.Find(Map, _tile, target, _path, maxPathSteps))
            {
                Debug.Log($"[GestureTestPiece] GoTo c{target.x} r{target.y}: no path within {maxPathSteps} steps.");
                return false;
            }
            Stop();
            _pathIndex = 0;
            _running = _path.Count > runPathSteps;
            SetDestination(target);
            _last = $"GoTo c{target.x} r{target.y} ({_path.Count} steps)";
            Debug.Log($"[GestureTestPiece] {_last}, {(_running ? "running" : "walking")}.");
            if (_stepT >= 1f) TryStep();
            RefreshLabel();
            return true;
        }

        /// <summary>True when GoTo would walk to <paramref name="target"/> right now.</summary>
        /// <summary>The world's start tile (the respawn point until shrines exist).</summary>
        public Vector2Int SpawnTile => island.world != null ? island.world.startTile : startTile;

        /// <summary>Puts the explorer on a tile at once (losing a battle sends it back to a shrine; loading a save).</summary>
        public void Teleport(Vector2Int tile, Vector2Int? facing = null)
        {
            Stop();
            CancelPath();
            _tile = _fromTile = tile;
            _stepT = 1f;
            _facing = facing.HasValue && facing.Value.sqrMagnitude == 1 ? facing.Value : new Vector2Int(0, -1);
            UpdateExplorer();
            RefreshLabel();
            Debug.Log($"[GestureTestPiece] Teleported to c{tile.x} r{tile.y}.");
        }

        public bool CanReach(Vector2Int target) => CanMove && target != _tile && ExplorerPath.CanReach(Map, _tile, target, maxPathSteps);

        bool FollowingPath => _destination.HasValue;

        void CancelPath()
        {
            if (!FollowingPath) return;
            _path.Clear();
            _running = false;
            SetDestination(null);
        }

        void SetDestination(Vector2Int? tile)
        {
            _destination = tile;
            DestinationChanged?.Invoke(tile);
        }

        void OnGesture(ThumbGesture gesture, GestureSource source)
        {
            _count++;
            _last = $"{gesture} ({source})";
            Vector2Int d = gesture switch
            {
                ThumbGesture.SwipeLeft => new Vector2Int(-1, 0),
                ThumbGesture.SwipeRight => new Vector2Int(1, 0),
                ThumbGesture.SwipeForward => new Vector2Int(0, -1),
                ThumbGesture.SwipeBack => new Vector2Int(0, 1),
                _ => Vector2Int.zero,
            };
            if (_locked || _heldStill)
            {
                _last += _locked ? " - battle, ignored" : " - hand moment, ignored";
            }
            else if (FollowingPath)
            {
                // Any swipe or tap cancels a GoTo; a swipe then walks its own way.
                CancelPath();
                _last += " - path cancelled";
                if (gesture == ThumbGesture.Tap) Stopped?.Invoke();
                if (gesture != ThumbGesture.Tap && !Zoomed && moveMode == MoveMode.SmartStep) SmartSwipe(d, source);
                else if (gesture != ThumbGesture.Tap && !Zoomed)
                {
                    _heading = _facing = d;
                    _source = source;
                    _swipeTime = Time.unscaledTime;
                    if (_stepT >= 1f) TryStep();
                }
            }
            else if (gesture == ThumbGesture.Tap)
            {
                if (_heading != Vector2Int.zero)
                {
                    Stop();
                    Stopped?.Invoke();
                }
                else if (_stepT >= 1f && !Zoomed) TapWhileStanding();
            }
            else if (Zoomed)
            {
                _last += " - zoomed, no walking";
            }
            else if (moveMode == MoveMode.SmartStep)
            {
                SmartSwipe(d, source);
            }
            else
            {
                if (d == _heading) _running = true;
                _heading = d;
                _facing = d;
                _source = source;
                _swipeTime = Time.unscaledTime;
                if (_stepT >= 1f) TryStep();
            }
            RefreshLabel();
        }

        // ---- smart steps (step 12a) ----

        void SmartSwipe(Vector2Int d, GestureSource source)
        {
            float now = Time.unscaledTime;
            bool twice = SmartStep.WalkOn(_lastSwipe, _lastSwipeAt, d, now, doubleSwipeSeconds, _plan.Count > 0 && _heading != Vector2Int.zero);
            _lastSwipe = d;
            _lastSwipeAt = now;
            _source = source;
            _swipeTime = now;
            if (twice)
            {
                // Walk on from where it is, the way it is going now (it may have turned with the path).
                var way = _stepT < 1f ? _tile - _fromTile : _heading;
                if (way.sqrMagnitude != 1) way = d;
                Replan(way, true);
                _last += " - walk on";
            }
            else Replan(d, false);
            if (_plan.Count == 0)
            {
                // Can't move that way at all: turn to it and bump.
                _heading = Vector2Int.zero;
                _facing = d;
                var front = _tile + d;
                char ch = Map.At(front.y, front.x);
                GameAudio.At(AudioIds.WalkBump, TileWorld(front));
                if (_stepT >= 1f && _view != null) _view.Hop();
                if (ch != IslandMap.Empty && IslandMap.IsWalkable(ch)) OnBump?.Invoke(front);
                _last += $" - can't go ({PlanEnd})";
                UpdateExplorer();
                return;
            }
            if (!twice) SmartStepsTaken++;
            Swiped?.Invoke(twice);
            _heading = _plan[0] - _tile;
            if (_stepT >= 1f) TryStep();
        }

        /// <summary>Plans from the tile being stepped onto (the current step always finishes).</summary>
        void Replan(Vector2Int way, bool continuous)
        {
            _continuous = continuous;
            PlanFrom = _tile;
            PlanEnd = SmartStep.Plan(Map, _tile, way, continuous, continuous ? 60 : smartTiles, PlanStop, _plan);
            _planIndex = 0;
            _running = false;
            PlanChanged?.Invoke();
            Debug.Log($"[GestureTestPiece] {(continuous ? "Walk on" : "Smart step")} {HeadingName(way)} from c{_tile.x} r{_tile.y}: {_plan.Count} tiles, ends at {PlanEnd}.");
        }

        void ClearPlan()
        {
            if (_plan.Count == 0) return;
            _plan.Clear();
            _planIndex = 0;
            _continuous = false;
            PlanChanged?.Invoke();
        }

        WalkStop PlanStop(Vector2Int from, Vector2Int next)
        {
            var s = BuiltInStop(from, next);
            foreach (var f in walkStops)
            {
                if (f == null) continue;
                var x = f(from, next);
                if (x > s) s = x;
            }
            return s;
        }

        /// <summary>Stop before a gate's tiles; stop on the tile that first brings an unsolved hand moment within reach.</summary>
        WalkStop BuiltInStop(Vector2Int from, Vector2Int next)
        {
            var world = island.world;
            if (world == null) return WalkStop.None;
            foreach (var g in world.gates)
                if (g.Covers(next) && !g.Covers(from)) return WalkStop.Before;
            if (SceneSwap.IsInside) return WalkStop.None;
            if (_pieces == null || Time.unscaledTime - _piecesAt > 5f)
            {
                _pieces = FindObjectsByType<HandPuzzle>(FindObjectsSortMode.None);
                _piecesAt = Time.unscaledTime;
            }
            foreach (var p in _pieces)
            {
                if (p == null || p.map != world || !p.WalkStopWanted) continue;
                if (p.InReach(next) && !p.InReach(from)) return WalkStop.On;
            }
            return WalkStop.None;
        }

        /// <summary>A little hop on the spot (waking up in the opening).</summary>
        public void Hop()
        {
            GameAudio.At(AudioIds.WalkHop, TileWorld(_tile));
            if (_view != null) _view.Hop();
            else if (_hopStart < 0f) _hopStart = Time.time;
        }

        /// <summary>Arms up and a hop (step 9f: a puzzle solved).</summary>
        public void Cheer()
        {
            if (_view != null) _view.Cheer();
            else Hop();
        }

        void TapWhileStanding()
        {
            var front = _tile + _facing;
            if (Map.At(front.y, front.x) == IslandMap.Water)
            {
                _last += " - fishing";
                GameAudio.At(AudioIds.PondTap, TileWorld(front));
                OnFishTap?.Invoke(front);
            }
            else Hop();
        }

        void ToggleZoom()
        {
            if (window == null) return;
            // Toggles the target, so a second tap mid-animation reverses it.
            if (_zoomedIn) window.ZoomOut();
            else window.ZoomTo(_tile);
            _zoomedIn = !_zoomedIn;
            RefreshLabel();
            Debug.Log($"[GestureTestPiece] Zoom {(_zoomedIn ? "in" : "out")} on c{_tile.x} r{_tile.y}.");
        }

        void TryStep()
        {
            if (moveMode == MoveMode.SmartStep && _plan.Count > 0 && !FollowingPath)
            {
                if (_planIndex >= _plan.Count && _continuous && PlanEnd == SmartStep.Cap) Replan(_heading, true);
                if (_planIndex >= _plan.Count)
                {
                    // Arrived. The plan stays until the next swipe so the footprints can fade out.
                    if (_heading != Vector2Int.zero) GameAudio.At(AudioIds.WalkStop, TileWorld(_tile));
                    _heading = Vector2Int.zero;
                    _running = false;
                    RefreshLabel();
                    return;
                }
                var step = _plan[_planIndex];
                if (!Map.CanWalk(step) || (step - _tile).sqrMagnitude != 1)
                {
                    _last += " - plan blocked";
                    Stop();
                    RefreshLabel();
                    return;
                }
                _planIndex++;
                _heading = _facing = step - _tile;
                _fromTile = _tile;
                _tile = step;
                _stepT = 0f;
                RefreshLabel();
                return;
            }
            if (FollowingPath)
            {
                if (_pathIndex >= _path.Count)
                {
                    CancelPath();
                    RefreshLabel();
                    return;
                }
                var step = _path[_pathIndex++];
                if (!Map.CanWalk(step) || (step - _tile).sqrMagnitude != 1)
                {
                    _last += " - path blocked";
                    CancelPath();
                    RefreshLabel();
                    return;
                }
                _facing = step - _tile;
                _fromTile = _tile;
                _tile = step;
                _stepT = 0f;
                RefreshLabel();
                return;
            }
            var next = _tile + _heading;
            char ch = Map.At(next.y, next.x);
            if (!Map.CanWalk(next))
            {
                Stop();
                _last += ch == IslandMap.Empty ? " - edge" : IslandMap.IsWalkable(ch) ? " - blocked (prop)" : $" - blocked ({ch})";
                RefreshLabel();
                if (ch != IslandMap.Empty && IslandMap.IsWalkable(ch))
                {
                    GameAudio.At(AudioIds.WalkBump, TileWorld(next));
                    OnBump?.Invoke(next);
                }
                return;
            }
            _fromTile = _tile;
            _tile = next;
            _stepT = 0f;
            RefreshLabel();
        }

        void Stop()
        {
            if (_heading != Vector2Int.zero) GameAudio.At(AudioIds.WalkStop, TileWorld(_tile));
            _heading = Vector2Int.zero;
            _running = false;
            ClearPlan();
        }

        /// <summary>Seconds for the step being taken now.</summary>
        float StepSeconds => moveMode == MoveMode.SmartStep && !FollowingPath
            ? (_continuous ? continuousSeconds : smartSeconds)
            : _running ? runSeconds : walkSeconds;

        /// <summary>Audio hookup: a tile's middle on the cap, in world space.</summary>
        Vector3 TileWorld(Vector2Int t) => island.transform.TransformPoint(island.TileToLocal(t, Map.capHeight));

        /// <summary>A footstep by the tile underfoot (the lodge is wood, the halls stone); tall grass rustles too.</summary>
        void StepSound()
        {
            var at = TileWorld(_tile);
            char ch = Map.At(_tile.y, _tile.x);
            string id;
            if (SceneSwap.IsInside) id = island.world != null && island.world.name.Contains("Lodge") ? AudioIds.StepWood : AudioIds.StepStone;
            else if (ch == IslandMap.Grass || ch == IslandMap.TallGrass) id = AudioIds.StepGrass;
            else if (ch == IslandMap.Path) id = AudioIds.StepPath;
            else id = AudioIds.StepStone;
            GameAudio.At(id, at);
            if (ch == IslandMap.TallGrass) GameAudio.At(AudioIds.GrassRustle, at);
        }

        bool ThumbStillHeld()
        {
            if (_source == GestureSource.Keyboard)
            {
                var kb = Keyboard.current;
                if (kb == null) return false;
                if (_heading.x < 0) return kb.leftArrowKey.isPressed;
                if (_heading.x > 0) return kb.rightArrowKey.isPressed;
                return _heading.y < 0 ? kb.upArrowKey.isPressed : kb.downArrowKey.isPressed;
            }
            // Lost tracking or a hand busy with the island counts as released.
            if (input.IsBlocked(_source)) return false;
            return probe != null && probe.TryGetOffsetMm(_source == GestureSource.LeftHand, out var mm) && mm.magnitude >= holdReleaseMm;
        }

        void Update()
        {
            if (_cube == null) return;
            var kb = Keyboard.current;
            if (kb != null && kb.zKey.wasPressedThisFrame && !_locked && _heading == Vector2Int.zero && _stepT >= 1f) ToggleZoom();

            if (moveMode == MoveMode.HoldToWalk && _heading != Vector2Int.zero &&
                Time.unscaledTime - _swipeTime > holdGraceSeconds && !ThumbStillHeld())
            {
                Stop(); // the current step still finishes on its tile
                RefreshLabel();
            }
            if (_stepT < 1f)
            {
                // Linear so consecutive steps join into a steady walk.
                _stepT = Mathf.Min(1f, _stepT + Time.deltaTime / StepSeconds);
                if (_stepT >= 1f)
                {
                    OnStep?.Invoke(_tile);
                    StepSound();
                    if (Map.At(_tile.y, _tile.x) == IslandMap.TallGrass) OnTallGrassStep?.Invoke(_tile);
                }
            }
            if (_stepT >= 1f && (_heading != Vector2Int.zero || FollowingPath)) TryStep();
            UpdateExplorer();

            // Face the label to the user (yaw only).
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 to = _label.transform.position - cam.transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 1e-6f) _label.transform.rotation = Quaternion.LookRotation(to, Vector3.up);
            }
        }

        /// <summary>Scrolls the window to the explorer, then places the cube against that view.</summary>
        void UpdateExplorer()
        {
            Vector2 p = Vector2.Lerp(_fromTile, _tile, _stepT);
            if (window != null) window.Follow(p);
            _standOffset = Vector2.MoveTowards(_standOffset, StandOffset, Time.deltaTime * 4f);
            p += _standOffset;
            float hop = 0f;
            if (_hopStart >= 0f)
            {
                float u = (Time.time - _hopStart) / 0.3f;
                if (u >= 1f) _hopStart = -1f;
                else hop = hopHeight * 4f * u * (1f - u);
            }
            if (_view == null)
            {
                _cube.localPosition = island.TileToLocal(p, Map.capHeight + cubeSize * 0.5f + hop + GroundLift(p));
                _cube.localScale = Vector3.one * (cubeSize * island.Zoom * Mathf.Max(0.001f, Shrink));
                return;
            }
            // The kid: feet on the cap, bouncing per tile, facing where it walks (map columns run toward -X, rows +Z).
            bool moving = _stepT < 1f;
            _view.Moving = moving;
            _view.Running = _running;
            _view.StepPhase = moving ? _stepT : 0f;
            float yaw = Mathf.Atan2(-_facing.x, _facing.y) * Mathf.Rad2Deg;
            _yaw = Mathf.LerpAngle(_yaw, yaw, 1f - Mathf.Exp(-Time.deltaTime * 14f));
            _cube.localPosition = island.TileToLocal(p, Map.capHeight + _view.Lift + GroundLift(p));
            _cube.localRotation = Quaternion.Euler(0f, _yaw + _view.Glance, 0f) * Quaternion.Euler(_view.Lean, 0f, 0f);
            _cube.localScale = Vector3.one * (island.Zoom * Mathf.Max(0.001f, Shrink));
        }

        void PositionLabel()
        {
            // Beside the island, on the user's right (island -X), a little above the surface.
            var b = island.LocalBounds;
            _label.transform.localPosition = new Vector3(b.min.x - 0.13f, 0.08f, 0f);
            RefreshLabel();
        }

        void RefreshLabel()
        {
            if (_label == null) return;
            string place = placer != null ? placer.Status : "-";
            string move = _locked ? "Battle" : _zoomedIn ? "Zoomed"
                : FollowingPath ? $"{(_running ? "Run" : "Walk")} to c{_destination.Value.x} r{_destination.Value.y}"
                : _heading == Vector2Int.zero ? "Stopped" : $"{(_running ? "Run" : "Walk")} {HeadingName(_heading)}";
            _label.text = $"<b>Mode:</b> {moveMode}\n<b>Last:</b> {_last}\n<b>Count:</b> {_count}\n<b>Tile:</b> c{_tile.x} r{_tile.y}  <b>{move}</b>\n<b>Place:</b> {place}";
        }

        static string HeadingName(Vector2Int h)
        {
            if (h.x < 0) return "left";
            if (h.x > 0) return "right";
            return h.y < 0 ? "forward" : "back";
        }
    }
}
