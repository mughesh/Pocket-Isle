using System;
using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>A place a <see cref="CarryPiece"/> can be set down: map tile (fractions allowed), height above the cap and turn.</summary>
    [Serializable]
    public struct CarrySpot
    {
        [Tooltip("Map tile (column, row) of the object's centre there.")]
        public Vector2 tile;
        [Tooltip("Resting height above the cap, tiles (on a rock ledge ~0.15).")]
        public float height;
        [Tooltip("Turn there, degrees (map space).")]
        public float yaw;
    }

    /// <summary>
    /// Carry (step 9b hand puzzle kit): a world object you grab and set down on one of N target spots.
    /// Grab with a pinch or a closed fist anywhere within <see cref="grabRadius"/> of it (a generous capsule along its
    /// long side). It is heavy: it only rises until lifted <see cref="liftToMove"/> tiles, then follows the hand with
    /// <see cref="lag"/> seconds of lag and a damped wobble. Released within <see cref="snapRadius"/> of a spot: it drops
    /// onto it and the puzzle is solved (stage = spot + 1). Released anywhere else: it falls back home.
    /// Look + pinch it (it floats up), then look + pinch a spot: it floats there by itself.
    /// The spots glow (rings, Diorama/Beacon) while it's usable, brighter while carried, brightest under it.
    /// The island doesn't draw the map's copy (the listener hides it); this draws its own (<see cref="PropView"/>).
    /// </summary>
    [DefaultExecutionOrder(-20)] // before the gaze walk and tile touch read IsHandBusy
    public class CarryPiece : HandPuzzle
    {
        [Header("Object")]
        public PropRecipe recipe;
        [Tooltip("Map tile (column, row) of its centre at home.")]
        public Vector2 home;
        public float homeHeight;
        public float homeYaw;
        public List<CarrySpot> spots = new List<CarrySpot>();

        [Header("Grab")]
        [Tooltip("Capsule radius round its long axis, tiles.")]
        public float grabRadius = 0.5f;
        [Tooltip("Extra world metres on top of the radius (hand tracking wobble).")]
        public float grabSlack = 0.015f;
        [Tooltip("Half length of the long axis, tiles (a 2 x 1 log: ~0.85).")]
        public float halfLength = 0.85f;

        [Header("Weight")]
        [Tooltip("Follow lag, seconds (heavier = more).")]
        public float lag = 0.22f;
        [Tooltip("Fastest it can move, tiles per second.")]
        public float maxSpeed = 7f;
        [Tooltip("Lift this high (tiles) before it moves sideways.")]
        public float liftToMove = 0.6f;
        [Tooltip("Largest wobble, degrees.")]
        public float wobble = 9f;
        [Tooltip("How hard sideways acceleration tips it.")]
        public float wobbleGain = 12f;

        [Header("Drop")]
        [Tooltip("Released this close (tiles, horizontal) to a spot: it lands there.")]
        public float snapRadius = 1.3f;
        public float dropSeconds = 0.35f;
        public float fallBackSeconds = 0.55f;

        [Header("Look + pinch")]
        [Tooltip("Floats this high (tiles) while picked by look + pinch.")]
        public float floatHeight = 1.1f;
        public float floatSeconds = 0.9f;
        [Tooltip("Picked but no spot chosen for this long: it settles back.")]
        public float pickTimeout = 15f;

        [Header("Spots")]
        [Tooltip("Diorama/Beacon material for the spot rings. Empty = no rings.")]
        public Material spotMaterial;
        public float spotRing = 1.1f;

        public enum Phase { Resting, Held, Picked, Moving, Placed }

        /// <summary>A hand grabbed it, or look + pinch picked it.</summary>
        public event Action<CarryPiece> Grabbed;
        /// <summary>Lifted past <see cref="liftToMove"/> (leaves and dust fall off).</summary>
        public event Action<CarryPiece> Lifted;
        /// <summary>Landed: on a spot (index), or back home (-1).</summary>
        public event Action<CarryPiece, int> Landed;

        public Phase State { get; private set; }
        /// <summary>Where it is now: (column, height in tiles, row).</summary>
        public Vector3 Position => _pos;
        /// <summary>The spot it lies on (-1 = home).</summary>
        public int Spot { get; private set; } = -1;
        /// <summary>The spot under it while carried, or -1.</summary>
        public int Hovered { get; private set; } = -1;
        public override bool Busy => State == Phase.Held || State == Phase.Picked || State == Phase.Moving;

        PropView _view;
        Vector3 _pos, _vel, _grabOffset;
        Vector2 _tilt, _tiltVel;
        float _yaw;
        bool _lifted;
        int _hand = -1;
        bool _byPinch;
        float _pickedAt;
        int _gazed = -1; // 0 = the object, 1 + i = spot i
        readonly HandState[] _hands = new HandState[2];
        readonly List<Vector2> _spotTiles = new List<Vector2>();
        readonly List<Transform> _rings = new List<Transform>();
        readonly List<MeshRenderer> _ringRenderers = new List<MeshRenderer>();
        Mesh _quad;
        MaterialPropertyBlock _ringBlock;
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        static readonly int FillId = Shader.PropertyToID("_Fill");
        static readonly int ModeId = Shader.PropertyToID("_Mode");

        protected override Vector2 HomeTile => home;

        public override Vector3? FocusPoint => _view != null && _view.Visible ? MapToWorld(_pos + Vector3.up * 0.35f) : (Vector3?)null;

        public override float FocusRadius => (halfLength + 0.2f) * TileWorld;

        protected override void Start()
        {
            if (recipe == null)
            {
                Debug.LogError($"[CarryPiece] {name}: no recipe.", this);
                enabled = false;
                return;
            }
            _view = new PropView($"Carry {recipe.name}", island, recipe);
            _ringBlock = new MaterialPropertyBlock();
            if (spotMaterial != null)
            {
                _quad = BattleHud.VerticalQuad();
                for (int i = 0; i < spots.Count; i++)
                {
                    var go = new GameObject($"Carry Spot {i}");
                    go.layer = island.gameObject.layer;
                    go.transform.SetParent(island.transform, false);
                    go.AddComponent<MeshFilter>().sharedMesh = _quad;
                    var r = go.AddComponent<MeshRenderer>();
                    r.sharedMaterial = spotMaterial;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    r.receiveShadows = false;
                    go.SetActive(false);
                    _rings.Add(go.transform);
                    _ringRenderers.Add(r);
                }
            }
            foreach (var s in spots) _spotTiles.Add(s.tile);
            base.Start();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Release(false);
            _view?.Destroy();
            foreach (var r in _rings)
                if (r != null) Destroy(r.gameObject);
            if (_quad != null) Destroy(_quad);
        }

        protected override void ApplyState()
        {
            StopAllCoroutines();
            Release(false);
            int spot = IsSolved ? PuzzleState.Stage(id) - 1 : -1;
            if (spot >= spots.Count) spot = -1;
            Rest(spot);
            SetProgressQuiet(spot >= 0 ? 1f : 0f);
        }

        void Rest(int spot)
        {
            Spot = spot;
            _pos = spot >= 0 ? new Vector3(spots[spot].tile.x, spots[spot].height, spots[spot].tile.y) : new Vector3(home.x, homeHeight, home.y);
            _yaw = spot >= 0 ? spots[spot].yaw : homeYaw;
            _vel = Vector3.zero;
            _tilt = _tiltVel = Vector2.zero;
            _lifted = false;
            State = spot >= 0 ? Phase.Placed : Phase.Resting;
        }

        // ---- per frame ----

        protected override void Update()
        {
            ReadHands(_hands);
            bool usable = Usable;
            if (!Here) Release(false);
            switch (State)
            {
                case Phase.Resting:
                    if (usable) TryGrab();
                    break;
                case Phase.Held:
                    Hold();
                    break;
                case Phase.Picked:
                    UpdatePicked();
                    break;
            }
            if (_hand >= 0) IslandHandle.MarkHandHeld(_hand == 0);
            Draw(usable);
            base.Update();
        }

        void TryGrab()
        {
            for (int h = 0; h < 2; h++)
            {
                var s = _hands[h];
                if (s.busy || !(s.pinchStarted || s.fistStarted)) continue;
                var grip = s.pinchStarted ? s.pinchPoint : s.palm;
                if (!InGrabVolume(grip)) continue;
                _hand = h;
                _byPinch = s.pinchStarted;
                _grabOffset = _pos - WorldToMap(grip);
                _lifted = false;
                State = Phase.Held;
                explorer.Locked = true;
                MarkInteracted();
                Debug.Log($"[CarryPiece] {id}: grabbed by the {(s.left ? "left" : "right")} hand ({(_byPinch ? "pinch" : "palm")}).");
                Grabbed?.Invoke(this);
                return;
            }
            // Look + pinch: pick it (it floats up), then a spot.
            _gazed = GazedTarget(false);
            if (_gazed == 0)
            {
                var p = FocusPoint;
                if (p.HasValue) FocusHighlight.Hover(p.Value, FocusRadius);
                if (GazePinch(_hands))
                {
                    MarkInteracted();
                    State = Phase.Picked;
                    _pickedAt = Time.time;
                    explorer.Locked = true;
                    Debug.Log($"[CarryPiece] {id}: picked by look + pinch; look at a spot and pinch.");
                    Grabbed?.Invoke(this);
                }
            }
        }

        bool InGrabVolume(Vector3 world)
        {
            Vector3 m = WorldToMap(world);
            Vector3 axis = Quaternion.Euler(0f, _yaw, 0f) * Vector3.right * halfLength;
            // Map space mirrors columns (column + = map -X); the axis only needs to lie along the object, either way works.
            Vector3 a = _pos + new Vector3(-axis.x, 0.25f, axis.z), b = _pos + new Vector3(axis.x, 0.25f, -axis.z);
            float slack = grabSlack / Mathf.Max(1e-5f, TileWorld);
            return CarryLogic.DistanceToSegment(m, a, b) <= grabRadius + slack;
        }

        void Hold()
        {
            var s = _hands[_hand];
            if (!s.tracked || !s.Holding)
            {
                Drop();
                return;
            }
            Vector3 target = WorldToMap(s.GripPoint) + _grabOffset;
            if (!_lifted)
            {
                // Heavy: it only comes up at first; sideways once it clears the gap.
                target.x = home.x;
                target.z = home.y;
                target.y = Mathf.Max(homeHeight, target.y);
                if (_pos.y >= homeHeight + liftToMove)
                {
                    _lifted = true;
                    SetProgress(0.5f);
                    Debug.Log($"[CarryPiece] {id}: lifted.");
                    Lifted?.Invoke(this);
                }
            }
            target.y = Mathf.Max(0.05f, Mathf.Min(target.y, 4f));
            Vector3 before = _pos;
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            _pos = CarryLogic.Follow(_pos, target, lag, maxSpeed, dt);
            Vector3 vel = (_pos - before) / dt;
            Vector3 acc = (vel - _vel) / dt;
            _vel = vel;
            // Damped wobble: sideways acceleration tips it, a spring brings it back.
            Vector2 force = new Vector2(-acc.z, acc.x) * wobbleGain;
            _tiltVel += (force - _tilt * 90f - _tiltVel * 11f) * dt;
            _tilt += _tiltVel * dt;
            _tilt = Vector2.ClampMagnitude(_tilt, wobble);
            Hovered = _lifted ? CarryLogic.PickSpot(new Vector2(_pos.x, _pos.z), _spotTiles, snapRadius) : -1;
        }

        void Drop()
        {
            int spot = _lifted ? CarryLogic.PickSpot(new Vector2(_pos.x, _pos.z), _spotTiles, snapRadius) : -1;
            Debug.Log($"[CarryPiece] {id}: released {(spot >= 0 ? $"over spot {spot}" : _lifted ? "away from the spots: back home" : "before it was lifted")}.");
            Release(true);
            StartCoroutine(MoveTo(spot, spot >= 0 ? dropSeconds : _lifted ? fallBackSeconds : 0.2f, false));
        }

        /// <summary>Lets go of the hand (and the explorer).</summary>
        void Release(bool keepMoving)
        {
            if (_hand < 0 && State != Phase.Picked && State != Phase.Moving) return;
            _hand = -1;
            Hovered = -1;
            if (explorer != null) explorer.Locked = false;
            if (!keepMoving && (State == Phase.Held || State == Phase.Picked || State == Phase.Moving)) Rest(Spot);
        }

        void UpdatePicked()
        {
            float t = Time.time - _pickedAt;
            float up = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.5f));
            _pos = new Vector3(home.x, homeHeight + floatHeight * up + 0.06f * Mathf.Sin(t * 2.4f), home.y);
            _tilt = new Vector2(Mathf.Sin(t * 1.7f), Mathf.Sin(t * 1.3f)) * 2f;
            _gazed = GazedTarget(true);
            if (_gazed > 0)
            {
                var s = spots[_gazed - 1];
                FocusHighlight.Hover(MapToWorld(new Vector3(s.tile.x, s.height + 0.3f, s.tile.y)), spotRing * 0.5f * TileWorld);
            }
            Hovered = _gazed > 0 ? _gazed - 1 : -1;
            if (GazePinch(_hands) || t > pickTimeout)
            {
                int spot = _gazed > 0 ? _gazed - 1 : -1;
                Debug.Log($"[CarryPiece] {id}: look + pinch {(spot >= 0 ? $"on spot {spot}: floats there" : t > pickTimeout ? "timed out: settles back" : "elsewhere: settles back")}.");
                _lifted = true;
                Release(true);
                StartCoroutine(MoveTo(spot, spot >= 0 ? floatSeconds : 0.45f, true));
            }
        }

        /// <summary>Glides (floating) or drops to a spot (-1 = home), then lands.</summary>
        System.Collections.IEnumerator MoveTo(int spot, float seconds, bool glide)
        {
            State = Phase.Moving;
            Vector3 from = _pos;
            Vector3 to = spot >= 0 ? new Vector3(spots[spot].tile.x, spots[spot].height, spots[spot].tile.y) : new Vector3(home.x, homeHeight, home.y);
            float fromYaw = _yaw, toYaw = spot >= 0 ? spots[spot].yaw : homeYaw;
            Vector2 fromTilt = _tilt;
            float arc = glide ? 0.5f : 0f;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float u = Mathf.Clamp01(t / seconds);
                float k = glide ? Mathf.SmoothStep(0f, 1f, u) : u * u; // a drop accelerates
                float h = Mathf.Lerp(from.y, to.y, k);
                Vector3 flat = Vector3.Lerp(new Vector3(from.x, 0f, from.z), new Vector3(to.x, 0f, to.z), glide ? k : Mathf.SmoothStep(0f, 1f, u));
                _pos = new Vector3(flat.x, h + arc * Mathf.Sin(u * Mathf.PI), flat.z);
                _yaw = Mathf.LerpAngle(fromYaw, toYaw, Mathf.SmoothStep(0f, 1f, u));
                _tilt = Vector2.Lerp(fromTilt, Vector2.zero, u);
                yield return null;
            }
            Rest(spot);
            Debug.Log($"[CarryPiece] {id}: landed {(spot >= 0 ? $"on spot {spot}" : "back home")}.");
            Landed?.Invoke(this, spot);
            if (spot >= 0) MarkSolved(spot + 1);
            else SetProgress(0f);
        }

        // ---- gaze ----

        /// <summary>0 = the object, 1 + i = spot i, -1 = nothing (spots only while picked).</summary>
        int GazedTarget(bool spotsOnly)
        {
            if (!HeadGaze.TryGetRay(out var ray)) return -1;
            var pick = new HeadGaze.ConePick(ray, _gazed);
            float minR = HeadGaze.MinRadius * island.transform.lossyScale.x;
            if (!spotsOnly) pick.Consider(0, MapToWorld(_pos + Vector3.up * 0.3f), Mathf.Max(minR, (halfLength + 0.1f) * TileWorld));
            else
                for (int i = 0; i < spots.Count; i++)
                    pick.Consider(1 + i, MapToWorld(new Vector3(spots[i].tile.x, spots[i].height + 0.3f, spots[i].tile.y)), Mathf.Max(minR, 0.8f * TileWorld));
            return pick.Best;
        }

        // ---- draw ----

        void Draw(bool usable)
        {
            if (_view == null) return;
            bool show = Here;
            // Held: a slow sway on top of the spring wobble (it's heavy in the hand).
            Vector2 sway = State == Phase.Held && _lifted ? new Vector2(Mathf.Sin(Time.time * 4.1f), Mathf.Sin(Time.time * 3.3f)) * (wobble * 0.15f) : Vector2.zero;
            var rot = Quaternion.Euler(0f, _yaw, 0f) * Quaternion.Euler(_tilt.x + sway.x, 0f, _tilt.y + sway.y);
            _view.windowClip = State != Phase.Held && State != Phase.Picked;
            _view.Place(new Vector2(_pos.x, _pos.z), _pos.y, rot, show);
            DrawSpots(show && !IsSolved && (usable || Busy));
        }

        void DrawSpots(bool on)
        {
            for (int i = 0; i < _rings.Count; i++)
            {
                var s = spots[i];
                Vector3 local = island.TileToLocal(s.tile, island.Content.capHeight + (s.height + 0.08f) * island.map.tileSize);
                bool visible = on && island.FootprintContains(local);
                _rings[i].gameObject.SetActive(visible);
                if (!visible) continue;
                bool carrying = State == Phase.Held && _lifted || State == Phase.Picked;
                float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * 3f + i);
                float intensity = (Hovered == i ? 2.4f : carrying ? 1.6f : 1f) * pulse;
                _rings[i].localPosition = local;
                _rings[i].localRotation = Quaternion.Euler(90f, 0f, 0f);
                _rings[i].localScale = Vector3.one * (island.map.tileSize * island.Zoom * spotRing * (Hovered == i ? 1.15f : 1f));
                _ringBlock.Clear();
                _ringBlock.SetFloat(ModeId, 1f);
                _ringBlock.SetFloat(FillId, 1f);
                _ringBlock.SetFloat(IntensityId, intensity);
                _ringRenderers[i].SetPropertyBlock(_ringBlock);
            }
        }

        /// <summary>Debug (bridge): lands it on a spot as if carried there.</summary>
        public void DebugPlace(int spot)
        {
            if (!Here || IsSolved) return;
            MarkInteracted();
            Release(true);
            _lifted = true;
            StartCoroutine(MoveTo(spot, spot >= 0 ? floatSeconds : 0.45f, true));
        }
    }
}
