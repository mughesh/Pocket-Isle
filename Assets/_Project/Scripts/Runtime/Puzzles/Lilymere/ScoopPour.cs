using System.Collections;
using System.Collections.Generic;
using Diorama.Audio;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 11b C, the hero moment at Lilymere's waterfall basin. A hand cupped palm up, dipped into the lagoon (below the
    /// water surface) and lifted out, carries a wobbly ball of water (<see cref="WaterScoop"/>: bubbles, drips, a jelly
    /// wobble when the hand moves). It stays while the palm stays roughly up; tipped elsewhere it splashes down. Tip the
    /// hand over above the stone basin: a stream pours in and the basin fills a step; two scoops fill it. Full: the basin
    /// glows, cracks run up the frozen waterfall, the ice shatters into sparkles, the waterfall runs (running look, mist),
    /// then the lagoon rises in a wave out from the basin (the dry ring becomes water: <see cref="LilymereLagoon"/> reads
    /// this puzzle, id lm.lagoon) and the half-sunk temple's door surfaces with a gush. Saved (stage = pours).
    /// Look + pinch the water, then the basin: a ball floats over and pours by itself. A restored realm counts it done.
    /// </summary>
    [DefaultExecutionOrder(-20)]
    public class ScoopPour : HandPuzzle
    {
        [Header("The basin (map: column, row)")]
        [Tooltip("The stone basin's middle (its 2 x 2 footprint's centre).")]
        public Vector2 basin;
        [Tooltip("The frozen waterfall prop's anchor (hidden once it runs) and the running view's centre.")]
        public Vector2Int waterfallAnchor;
        public Vector2 waterfallCenter;
        [Tooltip("Where look + pinch picks the water up (a lagoon tile near the basin) and the temple door (the gush).")]
        public Vector2 scoopSpot, templeDoor;
        public PropRecipe ballRecipe, basinWaterRecipe, runningRecipe;

        [Header("Scoop (WaterScoop, metres at the placed island size)")]
        [Range(0f, 1f)] public float cupUp = 0.55f;
        [Range(-1f, 1f)] public float keepUp = 0.1f, tipUp = -0.35f;
        public float dipDepth = 0.01f, liftHeight = 0.02f;
        [Tooltip("Water tiles this many tiles from the basin count (the lagoon's north shore).")]
        public float scoopReach = 14f;
        [Tooltip("Over the basin: this many tiles from its middle, up to this height (metres).")]
        public float basinRadius = 1.2f, basinAbove = 0.3f;
        public int poursNeeded = 2;
        [Tooltip("The ball in the palm (metres across at the placed island size), above the palm.")]
        public float ballSize = 0.03f, ballLift = 0.012f;

        [Header("Two hands (step 11d)")]
        [Tooltip("Both palms up, side by side within this (metres, real hands): one bigger scoop between them.")]
        public float pairGap = 0.08f;
        [Tooltip("While carrying, the hands may drift this much further apart (x Pair Gap).")]
        public float pairSlack = 1.6f;
        [Tooltip("The two-hand ball is this much bigger.")]
        public float pairBall = 1.8f;

        [Header("Breaking free")]
        public float crackSeconds = 1.2f, riseSeconds = 3f;

        [Header("Lines")]
        public string moreLine = "lm.scoop.more", doneLine = "lm.scoop.done";
        [Range(0f, 1f)] public float volume = 0.7f;

        WaterScoop _scoop;
        PropView _ball, _basinWater, _running;
        AudioSource _audio;
        readonly HandState[] _hands = new HandState[2];
        // Step 11d: left, right, and both cupped together (index 2, tried first).
        readonly WaterScoop.Hand[] _measured = new WaterScoop.Hand[3];
        readonly Vector3[] _palmPos = new Vector3[2];
        readonly Vector3[] _palmNormal = new Vector3[2];
        readonly List<Vector2Int> _dry = new List<Vector2Int>();
        Vector3 _ballWorld, _ballVel, _lastPalm;
        float _wobble, _fill, _dripAt, _mistAt;
        bool _pouring, _breaking, _quietSolve, _pickedWater, _running0, _bigBall;
        int _gazed = -1; // 0 = the water, 1 = the basin
        // Debug (bridge): a synthetic hand (palm pose in map space) driving the scoop.
        bool _debugHand, _debugPair;
        Vector3 _debugMap;
        float _debugUp;

        public WaterScoop Scoop => _scoop;
        public bool Running => IsSolved || _running0;
        public override bool Busy => _scoop != null && _scoop.Holder >= 0 || _pouring || _breaking;
        protected override Vector2 HomeTile => basin;
        float Scale => grabGuard != null ? grabGuard.ScaleFactor : 1f;

        public override Vector3? FocusPoint
        {
            get
            {
                if (!Here || IsSolved || _scoop == null) return null;
                return _scoop.State == ScoopState.Held ? BasinWorld(0.6f) : MapToWorld(new Vector3(scoopSpot.x, 0.1f, scoopSpot.y));
            }
        }

        // Step 11d: the two-hand cup ghost plays at the pool (scoop, lift, tip), not as a grab carried to the basin.
        public override Vector3? GhostDropPoint => null;
        public override float FocusRadius => 1f * TileWorld;

        // ---- step 11d: the objective (gold on the pool while scooping, on the basin while carrying; "Pours n/2") ----

        public override bool CountObjective(out int done, out int total)
        {
            total = poursNeeded;
            done = _scoop != null ? _scoop.Pours : 0;
            return true;
        }

        protected override int MarkCount => 2;
        protected override bool MarksOn => base.MarksOn && !_pouring && !_breaking;

        protected override bool Mark(int i, out Vector3 world, out float tiles)
        {
            world = default;
            tiles = 2f;
            if (_scoop == null) return false;
            bool carrying = _scoop.State == ScoopState.Held || _pickedWater;
            if (i == 0 && !carrying)
            {
                world = MapToWorld(new Vector3(scoopSpot.x, -0.05f, scoopSpot.y));
                return true;
            }
            if (i == 1 && carrying)
            {
                world = BasinWorld(0.35f);
                tiles = 2.2f;
                return true;
            }
            return false;
        }

        protected override bool GhostBothHands => true;
        protected override float GhostSpread => pairGap * 0.8f;

        protected override void Start()
        {
            _scoop = new WaterScoop { cupUp = cupUp, keepUp = keepUp, tipUp = tipUp, poursNeeded = poursNeeded };
            if (island != null)
            {
                _ball = new PropView("Water Ball", island, ballRecipe);
                _basinWater = new PropView("Basin Water", island, basinWaterRecipe);
                _running = new PropView("Waterfall Running", island, runningRecipe) { restoredSelf = 1f };
            }
            if (map != null)
                for (int r = 0; r < map.Rows; r++)
                    for (int c = 0; c < map.Columns; c++)
                        if (map.Raw(r, c) == IslandMap.DryBed) _dry.Add(new Vector2Int(c, r));
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
            base.Start();
        }

        protected override void OnDestroy()
        {
            _ball?.Destroy();
            _basinWater?.Destroy();
            _running?.Destroy();
            if (map != null) map.SetPropHidden(waterfallAnchor, false);
            base.OnDestroy();
        }

        protected override void ApplyState()
        {
            StopAllCoroutines();
            _pouring = _breaking = _pickedWater = false;
            _scoop.Restore(IsSolved ? poursNeeded : PuzzleState.Stage(id));
            _fill = _scoop.Pours / (float)Mathf.Max(1, poursNeeded);
            _running0 = IsSolved;
            map.SetPropHidden(waterfallAnchor, IsSolved);
            SetProgressQuiet(_fill);
            Debug.Log($"[ScoopPour] {id}: basin {_scoop.Pours}/{poursNeeded}, waterfall {(IsSolved ? "running, the lagoon full" : "frozen")}.");
        }

        protected override void Update()
        {
            ReadHands(_hands);
            if (!IsSolved && Here && RealmState.Restored >= 0.99f) QuietSolve();
            bool live = Usable && !IsSolved && !_pouring && !_breaking;
            if (live)
            {
                Measure();
                var before = _scoop.State;
                var now = _scoop.Feed(Time.deltaTime, _measured);
                if (now != before) Changed(before, now);
                LookPinch();
            }
            else _gazed = -1;
            Carry();
            Draw();
            base.Update();
        }

        // ---- hands ----

        void Measure()
        {
            float s = Scale, tile = Mathf.Max(1e-4f, TileWorld);
            for (int h = 0; h < 2; h++)
            {
                _palmNormal[h] = Vector3.zero;
                var x = new WaterScoop.Hand { tracked = _hands[h].tracked, busy = _hands[h].busy };
                Vector3 palmPos = default;
                Vector3 normal = Vector3.down;
                if (_debugHand && (h == 0 || _debugPair))
                {
                    x.tracked = true;
                    x.busy = false;
                    palmPos = MapToWorld(_debugMap) + (_debugPair ? island.transform.right * (h == 0 ? -0.025f : 0.025f) * Scale : Vector3.zero);
                    normal = island.transform.up * _debugUp;
                }
                else if (x.tracked)
                {
                    var hand = HandRegistry.Get(h == 0 ? Handedness.Left : Handedness.Right);
                    if (!HandLookup.TryGetJointPose(hand, HandJointId.HandPalm, out var palm))
                    {
                        x.tracked = false;
                        _measured[h] = x;
                        continue;
                    }
                    palmPos = palm.position;
                    // The palmar side is the palm joint's -Y for both hands (ISDK v207, as PalmHover).
                    normal = palm.rotation * Vector3.down;
                }
                if (!x.tracked)
                {
                    _measured[h] = x;
                    continue;
                }
                _palmPos[h] = palmPos;
                _palmNormal[h] = normal.normalized;
                x.up = Vector3.Dot(normal.normalized, island.transform.up);
                Where(ref x, palmPos, s, tile);
                _measured[h] = x;
                if (h == _scoop.Holder || _scoop.Holder == WaterScoop.Both || x.overWater || x.overBasin) TileTouch.Block(h == 0, 0.4f);
                if (h == _scoop.Holder || _scoop.Holder < 0 && h == 0) _lastPalm = palmPos;
            }
            // Both hands cupped together, side by side: one scoop between them (held, the pair may drift a bit apart).
            var both = new WaterScoop.Hand();
            float gap = pairGap * (_scoop.Holder == WaterScoop.Both ? pairSlack : 1f);
            if (_measured[0].tracked && _measured[1].tracked && !_measured[0].busy && !_measured[1].busy &&
                WaterScoop.Cupped(_palmPos[0], _palmNormal[0], _palmPos[1], _palmNormal[1], island.transform.up, gap, out var mid, out float up,
                    _scoop.Holder == WaterScoop.Both ? -2f : cupUp * 0.8f))
            {
                both.tracked = true;
                both.up = up;
                Where(ref both, mid, s, tile);
                if (_scoop.Holder == WaterScoop.Both || _scoop.Holder < 0) _lastPalm = mid;
            }
            _measured[WaterScoop.Both] = both;
            _scoop.dipDepth = dipDepth * s;
            _scoop.liftHeight = liftHeight * s;
        }

        /// <summary>Over the lagoon (and how high above its surface) or over the basin, for a palm (or the pair's middle).</summary>
        void Where(ref WaterScoop.Hand x, Vector3 palmPos, float s, float tile)
        {
            var m = WorldToMap(palmPos);
            var t = new Vector2Int(Mathf.RoundToInt(m.x), Mathf.RoundToInt(m.z));
            x.overWater = map.At(t) == IslandMap.Water && ((Vector2)t - basin).magnitude <= scoopReach;
            // The water sits 0.1 tiles under the cap (the island's water drop); metres in the room.
            x.aboveWater = (m.y + 0.1f) * tile;
            x.overBasin = (new Vector2(m.x, m.z) - basin).magnitude <= basinRadius && m.y * tile >= -0.01f * s && m.y * tile <= basinAbove * s;
        }

        void Changed(ScoopState from, ScoopState to)
        {
            switch (to)
            {
                case ScoopState.Dipped:
                    MarkInteracted();
                    Debug.Log($"[ScoopPour] {id}: {(_scoop.Holder == WaterScoop.Both ? "both hands cupped together dip" : "a cupped hand dips")} into the lagoon ({(_scoop.Holder == 0 ? "left" : _scoop.Holder == 1 ? "right" : "both")}).");
                    Splash(PalmWorld(), 8);
                    break;
                case ScoopState.Held:
                    LessonComplete();
                    Debug.Log($"[ScoopPour] {id}: lifted out: a ball of water sits in the palm.");
                    _ballWorld = PalmWorld();
                    _ballVel = Vector3.zero;
                    _bigBall = _scoop.Holder == WaterScoop.Both;
                    GameAudio.At(AudioIds.PondTap, _ballWorld);
                    break;
                case ScoopState.Pouring:
                    Debug.Log($"[ScoopPour] {id}: tipped over the basin: it pours ({_scoop.Pours}/{poursNeeded}).");
                    StartCoroutine(Pour(_ballWorld));
                    break;
                case ScoopState.Spilled:
                    Debug.Log($"[ScoopPour] {id}: tipped away from the basin: it splashes down.");
                    Splash(_ballWorld, 14);
                    GameAudio.At(AudioIds.PondSplash, _ballWorld);
                    _scoop.Reset();
                    break;
                case ScoopState.Empty:
                    if (from == ScoopState.Dipped) Debug.Log($"[ScoopPour] {id}: the hand turned over in the water: nothing scooped.");
                    break;
            }
        }

        Vector3 PalmWorld() => _lastPalm + island.transform.up * (ballLift * Scale);

        void LookPinch()
        {
            if (!HeadGaze.TryGetRay(out var ray) || _scoop.State != ScoopState.Empty)
            {
                _gazed = -1;
                return;
            }
            var pick = new HeadGaze.ConePick(ray, _gazed);
            float r = Mathf.Max(HeadGaze.MinRadius * island.transform.lossyScale.x, 0.8f * TileWorld);
            pick.Consider(0, MapToWorld(new Vector3(scoopSpot.x, 0f, scoopSpot.y)), r);
            if (_pickedWater) pick.Consider(1, BasinWorld(0.3f), r);
            _gazed = pick.Best;
            if (_gazed < 0) return;
            FocusHighlight.Hover(_gazed == 0 ? MapToWorld(new Vector3(scoopSpot.x, 0f, scoopSpot.y)) : BasinWorld(0.3f), FocusRadius);
            if (!GazePinch(_hands)) return;
            MarkInteracted();
            if (_gazed == 0)
            {
                _pickedWater = true;
                Splash(MapToWorld(new Vector3(scoopSpot.x, 0f, scoopSpot.y)), 8);
                Debug.Log($"[ScoopPour] {id}: look + pinch on the water: now look at the basin and pinch.");
            }
            else LookPour();
        }

        void LookPour()
        {
            _pickedWater = false;
            _bigBall = false;
            LessonComplete();
            _scoop.PourByItself();
            Debug.Log($"[ScoopPour] {id}: look + pinch on the basin: a ball floats over and pours ({_scoop.Pours}/{poursNeeded}).");
            StartCoroutine(FloatThenPour());
        }

        IEnumerator FloatThenPour()
        {
            _pouring = true;
            var from = MapToWorld(new Vector3(scoopSpot.x, 0.2f, scoopSpot.y));
            var to = BasinWorld(1.4f);
            for (float t = 0f; t < 1f; t += Time.deltaTime)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                _ballWorld = Vector3.Lerp(from, to, u) + island.transform.up * TileWorld * 1.5f * Mathf.Sin(u * Mathf.PI);
                yield return null;
            }
            yield return Pour(to, true);
        }

        // ---- carrying ----

        void Carry()
        {
            if (_scoop == null || _scoop.State != ScoopState.Held || _pouring) return;
            var target = PalmWorld();
            // A jelly wobble: the ball lags the palm on a spring.
            var prev = _ballWorld;
            var acc = (target - _ballWorld) * 220f - _ballVel * 18f;
            _ballVel += acc * Time.deltaTime;
            _ballWorld += _ballVel * Time.deltaTime;
            if ((_ballWorld - target).magnitude > 0.05f * Scale) _ballWorld = target;
            _wobble = Mathf.Clamp01(_wobble * Mathf.Exp(-4f * Time.deltaTime) + (_ballWorld - prev).magnitude / Mathf.Max(1e-3f, 0.01f * Scale));
            if (Time.time >= _dripAt && fx != null)
            {
                _dripAt = Time.time + Random.Range(0.18f, 0.4f);
                fx.Burst(_ballWorld - island.transform.up * ballSize * Scale * 0.45f, 1, new Color(0.7f, 0.92f, 1f, 0.9f), TileWorld * 0.03f, 0.5f, TileWorld * 0.025f,
                    -island.transform.up * TileWorld * 2f);
            }
        }

        IEnumerator Pour(Vector3 from, bool alreadyThere = false)
        {
            _pouring = true;
            var to = BasinWorld(0.2f);
            float tile = TileWorld;
            GameAudio.At(AudioIds.PondSplash, to);
            GameAudio.Procedural(_audio, PuzzleSounds.Whoosh, volume * 0.5f);
            float fillFrom = _fill, fillTo = _scoop.Pours / (float)Mathf.Max(1, poursNeeded);
            for (float t = 0f; t < 0.8f; t += Time.deltaTime)
            {
                float u = t / 0.8f;
                _ballWorld = Vector3.Lerp(from, to + island.transform.up * tile * 0.8f, Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, u * 1.6f)));
                _fill = Mathf.Lerp(fillFrom, fillTo, Mathf.SmoothStep(0f, 1f, u));
                if (fx != null && Here)
                    fx.Burst(Vector3.Lerp(_ballWorld, to, Random.value), 2, new Color(0.62f, 0.88f, 0.95f, 0.95f), tile * 0.05f, 0.35f, tile * 0.05f, -island.transform.up * tile * 1.5f);
                yield return null;
            }
            _fill = fillTo;
            if (fx != null && Here) fx.Burst(to, 16, new Color(0.75f, 0.95f, 1f), tile * 0.6f, 0.6f, tile * 0.05f, island.transform.up * tile * 0.6f);
            PuzzleState.SetStage(id, _scoop.Pours);
            SetProgress(_fill);
            _scoop.Reset();
            _pouring = false;
            if (_scoop.Full) StartCoroutine(BreakFree());
            else if (!string.IsNullOrEmpty(moreLine)) Dialogue.Say(moreLine);
        }

        // ---- the falls break free, the lagoon rises ----

        IEnumerator BreakFree()
        {
            _breaking = true;
            float tile = TileWorld;
            Debug.Log($"[ScoopPour] {id}: the basin is full: cracks run up the frozen waterfall.");
            // The basin glows; cracks climb the ice as sparks.
            for (float t = 0f; t < crackSeconds; t += Time.deltaTime)
            {
                float u = t / crackSeconds;
                if (fx != null && Here && Random.value < 0.5f)
                {
                    var at = MapToWorld(new Vector3(waterfallCenter.x + Random.Range(-0.5f, 0.5f), 0.3f + u * 2.1f, waterfallCenter.y + 0.2f));
                    fx.Burst(at, 2, new Color(0.85f, 0.97f, 1f), tile * 0.15f, 0.4f, tile * 0.03f, Vector3.zero);
                }
                if (fx != null && Here && Random.value < 0.2f) fx.Burst(BasinWorld(0.3f), 2, new Color(0.7f, 0.95f, 1f), tile * 0.3f, 0.6f, tile * 0.04f, island.transform.up * tile * 0.4f);
                yield return null;
            }
            // The ice shatters, the waterfall runs.
            GameAudio.At(AudioIds.CatchShatter, MapToWorld(new Vector3(waterfallCenter.x, 1.4f, waterfallCenter.y)));
            if (fx != null && Here)
                for (int k = 0; k < 4; k++)
                    fx.Burst(MapToWorld(new Vector3(waterfallCenter.x, 0.5f + k * 0.55f, waterfallCenter.y + 0.2f)), 16, new Color(0.9f, 0.98f, 1f), tile * 0.9f, 0.8f, tile * 0.05f, island.transform.up * tile * 0.3f);
            map.SetPropHidden(waterfallAnchor, true);
            _running0 = true;
            yield return new WaitForSeconds(0.5f);
            // The lagoon rises: the dry ring floods (one island rebuild), a wave of splashes spreads out from the basin.
            Debug.Log($"[ScoopPour] {id}: the waterfall runs: the lagoon rises ({_dry.Count} dry bed tiles flood).");
            GameAudio.Ui(AudioIds.RestoreSwell);
            MusicDirector.Jingle(3f);
            MarkSolved(_scoop.Pours);
            float maxD = 1f;
            foreach (var d in _dry) maxD = Mathf.Max(maxD, ((Vector2)d - basin).magnitude);
            for (float t = 0f; t < riseSeconds; t += Time.deltaTime)
            {
                float ring = t / riseSeconds * maxD;
                if (fx != null && Here)
                    for (int n = 0; n < 6; n++)
                    {
                        var d = _dry[Random.Range(0, _dry.Count)];
                        if (Mathf.Abs(((Vector2)d - basin).magnitude - ring) > 1.5f) continue;
                        var at = island.TileToLocal(d, island.Content.capHeight);
                        if (!island.FootprintContains(at)) continue;
                        fx.Burst(island.transform.TransformPoint(at), 2, new Color(0.65f, 0.9f, 0.97f), tile * 0.3f, 0.45f, tile * 0.04f, island.transform.up * tile * 0.35f);
                    }
                yield return null;
            }
            // The temple's door surfaces with a gush (step 11c opens it).
            if (fx != null && Here)
                fx.Burst(MapToWorld(new Vector3(templeDoor.x, 0.3f, templeDoor.y)), 30, new Color(0.75f, 0.95f, 1f), tile * 1.2f, 0.9f, tile * 0.06f, island.transform.up * tile * 1.4f);
            if (!string.IsNullOrEmpty(doneLine)) Dialogue.Say(doneLine);
            if (explorer != null) explorer.Cheer();
            _breaking = false;
        }

        void QuietSolve()
        {
            if (_quietSolve) return;
            _quietSolve = true;
            Debug.Log($"[ScoopPour] {id}: the realm is restored, so the waterfall runs and the lagoon is full.");
            _scoop.Restore(poursNeeded);
            _fill = 1f;
            _running0 = true;
            map.SetPropHidden(waterfallAnchor, true);
            MarkSolved(poursNeeded);
        }

        // ---- drawing ----

        void Draw()
        {
            bool here = Here;
            if (_ball != null)
            {
                bool show = here && (_scoop.State == ScoopState.Held || _pouring);
                float jelly = 1f + 0.12f * _wobble * Mathf.Sin(Time.time * 22f);
                var local = island.transform.InverseTransformPoint(_ballWorld);
                float metres = ballSize * Scale / Mathf.Max(1e-4f, island.transform.lossyScale.x) * (_bigBall ? pairBall : 1f);
                _ball.PlaceLocal(local - Vector3.up * metres * 0.5f, Quaternion.Euler(0f, Time.time * 30f, 0f), metres * jelly, show);
            }
            _basinWater?.Place(basin, 0.1f + 0.2f * _fill, Quaternion.identity, here && _fill > 0.01f, 1f);
            if (_running != null)
            {
                _running.Place(waterfallCenter, 0f, Quaternion.identity, here && Running, 1f);
                if (here && Running && fx != null && Time.time >= _mistAt && InView)
                {
                    _mistAt = Time.time + 0.25f;
                    float tile = TileWorld;
                    var foot = MapToWorld(new Vector3(waterfallCenter.x + Random.Range(-0.6f, 0.6f), 0.15f, waterfallCenter.y + 0.4f));
                    fx.Burst(foot, 2, new Color(0.95f, 0.98f, 1f, 0.6f), tile * 0.25f, 1.1f, tile * 0.1f, island.transform.up * tile * 0.35f);
                }
            }
        }

        Vector3 BasinWorld(float height) => MapToWorld(new Vector3(basin.x, height, basin.y));

        void Splash(Vector3 at, int count)
        {
            if (fx == null || !Here) return;
            fx.Burst(at, count, new Color(0.7f, 0.93f, 1f, 0.9f), TileWorld * 0.4f, 0.5f, TileWorld * 0.04f, island.transform.up * TileWorld * 0.5f);
        }

        // ---- bridge ----

        /// <summary>
        /// Debug (bridge): a synthetic left hand at map point (column, height in tiles, row) with its palm normal's
        /// up-component <paramref name="up"/> (1 = palm up, -1 = down), until <see cref="DebugHandOff"/>.
        /// </summary>
        public void DebugHand(Vector3 mapPoint, float up, bool pair = false)
        {
            _debugHand = true;
            _debugPair = pair;
            _debugMap = mapPoint;
            _debugUp = up;
        }

        public void DebugHandOff() => _debugHand = false;

        /// <summary>Debug (bridge): look + pinch on the water, then on the basin.</summary>
        public void DebugLookPour()
        {
            if (!Here || IsSolved || _pouring || _breaking) return;
            MarkInteracted();
            LookPour();
        }

        public string Describe() =>
            $"scoop {(_scoop != null ? _scoop.State.ToString() : "?")}, basin {(_scoop != null ? _scoop.Pours : 0)}/{poursNeeded} (fill {_fill:0.00}), " +
            $"waterfall {(Running ? "running" : "frozen")}, lagoon {(map != null && map.Risen ? "full" : "half dry")}, pouring {_pouring}, breaking {_breaking}";
    }
}
