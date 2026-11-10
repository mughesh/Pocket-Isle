using System;
using System.Collections.Generic;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 27 A (first outside playtest: nobody knew how to go in): every way a player tries to enter a door works. Besides
    /// walking in, look + pinch on the door or a fingertip on it walks the explorer up (the walk-up rule of the door) and
    /// in. An open door the explorer is near, or that is the current objective, says so: its portal grows brighter and
    /// taller (<see cref="Boost"/>, drawn by <see cref="DoorGlow"/>), gold runs along the ground into the doorway, and a
    /// chip names the two ways in. Plain C#, owned by the door's scene script, which calls <see cref="Tick"/> every frame.
    /// </summary>
    public class DoorEntry
    {
        /// <summary>Within this many tiles of the door the signifier shows.</summary>
        public const int NearTiles = 4;
        public const string ChipText = "Walk in, or look and pinch";
        public const string NoWayLine = "door.noway";
        const float PokeCooldown = 1.2f, WaveGap = 1.4f, WaveStep = 0.1f, ChipWidth = 0.15f;

        readonly MonoBehaviour _owner;
        readonly IslandBuilder _island;
        readonly GestureTestPiece _explorer;
        readonly Func<bool> _here, _open, _objective;
        readonly Func<Vector2Int, bool> _atFront;
        readonly RectInt _footprint;
        readonly Vector2Int[] _walkTo;
        readonly Func<Vector3> _doorway;
        readonly float _hoverTiles;
        readonly Action _use;

        readonly PodRack _pods;
        readonly IslandHandle _guard;
        readonly WorldWindow _window;
        readonly StoneTablet _chip;

        readonly bool[] _wasPinching = { true, true }, _wasNear = new bool[2];
        readonly List<Vector2Int> _wave = new List<Vector2Int>();
        bool _pending, _gazed;
        float _lastPoke = -99f, _nextWave, _chipAlpha;
        int _waveIndex;

        /// <summary>0..1: how strongly the open door says "come in" (the portal's height and brightness).</summary>
        public float Boost { get; private set; }

        /// <summary>The chip is showing (tests, the bridge).</summary>
        public bool ChipShowing => _chipAlpha > 0.01f;

        /// <summary>Tiles a door counts as near from (Chebyshev, pure).</summary>
        public static bool Near(Vector2Int tile, RectInt footprint) => WalkUp.Distance(tile, footprint) <= NearTiles;

        /// <param name="owner">The door's script (names the door in the log, owns the pods' gaze claim).</param>
        /// <param name="footprint">The door's tiles (map).</param>
        /// <param name="walkTo">Tiles in front of it, the walk goes to the nearest reachable one.</param>
        /// <param name="doorway">The doorway's middle (column, height in tiles, row).</param>
        /// <param name="use">What bumping the door does (go in, or say why not).</param>
        public DoorEntry(MonoBehaviour owner, IslandBuilder island, GestureTestPiece explorer, Func<bool> here, Func<bool> open, Func<bool> objective,
            Func<Vector2Int, bool> atFront, RectInt footprint, Vector2Int[] walkTo, Func<Vector3> doorway, float hoverTiles, Action use)
        {
            _owner = owner;
            _island = island;
            _explorer = explorer;
            _here = here;
            _open = open;
            _objective = objective;
            _atFront = atFront;
            _footprint = footprint;
            _walkTo = walkTo;
            _doorway = doorway;
            _hoverTiles = hoverTiles;
            _use = use;
            _pods = UnityEngine.Object.FindFirstObjectByType<PodRack>(FindObjectsInactive.Include);
            _guard = UnityEngine.Object.FindFirstObjectByType<IslandHandle>(FindObjectsInactive.Include);
            _window = UnityEngine.Object.FindFirstObjectByType<WorldWindow>(FindObjectsInactive.Include);
            if (_pods == null || _guard == null) Debug.LogError("[DoorEntry] No PodRack or IslandHandle in the scene: a pinch on a door may also walk or pick a pod.", owner);
            var bubble = UnityEngine.Object.FindFirstObjectByType<DialogueBubble>(FindObjectsInactive.Include);
            if (bubble != null && bubble.hudMaterial != null)
            {
                _chip = new StoneTablet($"{owner.name} Chip", island.transform, bubble.hudMaterial, island.gameObject.layer);
                _chip.Slate();
                _chip.Set(ChipText, bubble.fontSize * 0.85f, ChipWidth, new Vector2(0.012f, 0.008f));
            }
            else Debug.LogError("[DoorEntry] No DialogueBubble with a HUD material: the door shows no chip.", owner);
            _explorer.OnStep += OnStep;
        }

        public void Destroy()
        {
            if (_explorer != null) _explorer.OnStep -= OnStep;
            if (_pods != null) _pods.SetExternalGaze(_owner, false);
            if (_chip != null && _chip.transform != null) UnityEngine.Object.Destroy(_chip.transform.gameObject);
        }

        float TileWorld => _island.map.tileSize * _island.Zoom * _island.transform.lossyScale.x;

        Vector3 Local(Vector3 map) => _island.TileToLocal(new Vector2(map.x, map.z), _island.Content.capHeight + map.y * _island.map.tileSize);

        bool Visible(Vector3 map) => _island.FootprintContains(_island.TileToLocal(new Vector2(map.x, map.z), _island.Content.capHeight));

        bool Free => _explorer.CanMove && (_window == null || !_window.IsZoomed) && !Dialogue.Held && !ChoiceBoard.IsOpen && !EncounterDirector.AnyInBattle;

        /// <summary>One frame: the walk that was asked for, the gaze, pinch and poke, the signifier.</summary>
        public void Tick()
        {
            bool here = _here != null && _here() && _island.Content != null;
            Vector3 door = here ? _doorway() : default;
            bool visible = here && Visible(door);
            bool free = visible && Free && !Onboarding.LessonWalking;

            // A walk to the door that got cancelled (a swipe, a new destination) no longer goes in.
            if (_pending && !_explorer.Destination.HasValue && !_atFront(_explorer.Tile)) _pending = false;

            bool gazed = free && Gazed(door);
            if (gazed != _gazed && _pods != null) _pods.SetExternalGaze(_owner, gazed);
            _gazed = gazed;
            if (gazed) FocusHighlight.Hover(_island.transform.TransformPoint(Local(door)), _hoverTiles * TileWorld);
            UpdateHands(free, gazed, door);

            bool open = here && _open();
            bool near = open && Near(_explorer.Tile, _footprint);
            Boost = Mathf.MoveTowards(Boost, near || open && _objective != null && _objective() ? 1f : 0f, Time.deltaTime * 2f);
            Wave(near && free);
            Chip(near && visible && free, door);
        }

        bool Gazed(Vector3 door)
        {
            if (!HeadGaze.TryGetRay(out var ray)) return false;
            var pick = new HeadGaze.ConePick(ray, _gazed ? 0 : -1);
            pick.Consider(0, _island.transform.TransformPoint(Local(door)), Mathf.Max(HeadGaze.MinRadius * _island.transform.lossyScale.x, 0.9f * TileWorld));
            return pick.Best == 0;
        }

        void UpdateHands(bool free, bool gazed, Vector3 door)
        {
            Vector3 world = _island.Content != null ? _island.transform.TransformPoint(Local(door)) : default;
            float reach = Mathf.Max(0.03f, 0.8f * TileWorld);
            for (int h = 0; h < 2; h++)
            {
                bool left = h == 0;
                var hand = HandRegistry.Get(left ? Handedness.Left : Handedness.Right);
                bool tracked = hand != null && hand.IsTrackedDataValid;
                bool busy = _guard != null && _guard.IsHandBusy(left);
                bool pinching = tracked && hand.GetIndexFingerIsPinching();
                if (pinching && !_wasPinching[h] && !busy && gazed)
                {
                    if (IslandHandle.TryGetPinchPoint(hand, out var at) && HandPuzzle.ClaimsPinch(at, out _)) { }
                    else Pick($"look + pinch ({(left ? "left" : "right")})");
                }
                _wasPinching[h] = pinching;
                bool near = free && tracked && !busy && HandLookup.TryGetJoint(hand, HandJointId.HandIndexTip, out var tip) && (tip - world).magnitude < reach;
                if (near && !_wasNear[h] && Time.time - _lastPoke > PokeCooldown)
                {
                    _lastPoke = Time.time;
                    Pick($"poke ({(left ? "left" : "right")})");
                }
                _wasNear[h] = near;
            }
        }

        /// <summary>Look + pinch or a poke on the door (also the bridge): in from the front, otherwise walk up first.</summary>
        public void Pick(string how)
        {
            if (Onboarding.LessonWalking) return; // step 28: inert during the walk lessons
            string name = _owner != null ? _owner.name : "door";
            if (_atFront(_explorer.Tile))
            {
                Debug.Log($"[DoorEntry] {name}: {how}, at the door.");
                Use();
                return;
            }
            Vector2Int? best = null;
            var map = _island.world;
            foreach (var t in _walkTo)
                if (map != null && map.CanWalk(t) && _explorer.CanReach(t) && (!best.HasValue || (t - _explorer.Tile).sqrMagnitude < (best.Value - _explorer.Tile).sqrMagnitude))
                    best = t;
            if (best.HasValue && _explorer.GoTo(best.Value))
            {
                _pending = true;
                Debug.Log($"[DoorEntry] {name}: {how}, walking to c{best.Value.x} r{best.Value.y} first.");
                return;
            }
            TempleRules.Refused(_owner, $"{how}: no way there from c{_explorer.Tile.x} r{_explorer.Tile.y}");
            Dialogue.Repeat(NoWayLine);
        }

        void OnStep(Vector2Int tile)
        {
            if (!_pending || _explorer.Destination.HasValue && _explorer.Destination.Value != tile) return;
            _pending = false;
            if (_atFront(tile)) Use();
        }

        void Use()
        {
            _pending = false;
            _use?.Invoke();
        }

        /// <summary>Gold running along the ground from the explorer into the doorway, one tile after another.</summary>
        void Wave(bool on)
        {
            if (!on)
            {
                _wave.Clear();
                return;
            }
            if (_wave.Count > 0)
            {
                if (Time.time < _nextWave) return;
                _island.FlashTile(_wave[_waveIndex], 0.4f);
                _nextWave = Time.time + WaveStep;
                if (++_waveIndex >= _wave.Count)
                {
                    _wave.Clear();
                    _nextWave = Time.time + WaveGap;
                }
                return;
            }
            if (Time.time < _nextWave || _walkTo.Length == 0 || _atFront(_explorer.Tile) || _island.world == null) return;
            if (!ExplorerPath.Find(_island.world, _explorer.Tile, _walkTo[0], _wave, NearTiles * 3)) _wave.Clear();
            _waveIndex = 0;
            _nextWave = Time.time;
        }

        void Chip(bool on, Vector3 door)
        {
            _chipAlpha = Mathf.MoveTowards(_chipAlpha, on ? 1f : 0f, Time.deltaTime * 4f);
            if (_chip == null) return;
            if (_chipAlpha <= 0.01f)
            {
                _chip.Place(Vector3.zero, 0f, 0f);
                return;
            }
            // Beside the door, never over it (R15): on the side that is inside the window.
            float aside = _footprint.width * 0.5f + 0.4f + (_chip.Size.x * 0.5f) / Mathf.Max(1e-4f, TileWorld);
            var spot = new Vector3(door.x + aside, door.y + 0.3f, door.z + 1.2f);
            if (!Visible(spot)) spot.x = door.x - aside;
            var world = _island.transform.TransformPoint(Local(spot));
            var cam = Camera.main;
            // The bubble's size (it has to be read, however small the island is), square to the view.
            _chip.Place(world, 1f, _chipAlpha, cam != null ? cam.transform.rotation : (Quaternion?)null);
        }
    }

    /// <summary>
    /// Step 27 D: the tiles kept clear of tall grass round a door (pure, tested on the real maps): within 1.5 tiles of its
    /// footprint and front row, and the wedge toward the player, who sees the door from in front and to the side.
    /// </summary>
    public static class DoorClearance
    {
        public const float Radius = 1.5f;
        public const int WedgeRows = 2; // more cut into Galewick's Isling levels (KeeperBalanceTests)

        /// <summary>
        /// True when tall grass on <paramref name="tile"/> would hide the door at <paramref name="footprint"/>.
        /// <paramref name="wedge"/> off = only the 1.5 tiles round it (a shrine: nothing stands up in it to hide).
        /// </summary>
        public static bool Clears(RectInt footprint, Vector2Int tile, bool wedge = true)
        {
            float cx = footprint.xMin + footprint.width * 0.5f;
            int rows = tile.y - (footprint.yMax - 1);
            if (wedge && rows >= 1 && rows <= WedgeRows && Mathf.Abs(tile.x + 0.5f - cx) <= footprint.width * 0.5f + 1f + rows) return true;
            // Round the door and its front row.
            var around = new RectInt(footprint.xMin, footprint.yMin, footprint.width, footprint.height + 1);
            float dx = Mathf.Max(around.xMin - tile.x, 0, tile.x - (around.xMax - 1));
            float dy = Mathf.Max(around.yMin - tile.y, 0, tile.y - (around.yMax - 1));
            return dx * dx + dy * dy <= Radius * Radius;
        }
    }
}
