using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction.Input;
using Diorama.Audio;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 10f, Cinderwood's smoky hollow (the trail from the kiln village to the hot springs): thick dark smoke puffs
    /// sit on the path tiles and block walking, six dead stone lanterns line it. A fingertip touching a lantern's top
    /// lights it; touch two in a row within 1.5 s (a sweep) and the fire races down the rest of the line by itself, one
    /// every 0.15 s with a spark trail jumping between them (<see cref="LanternChain"/>). Look + pinch a lantern: it
    /// lights and the next one follows 0.5 s later. All lit: the smoke burns off from the lit end in rolling puffs and
    /// its tiles open (saved: stage = lit lanterns, solved = the smoke gone). The lit hollow stays warm: each lit lantern
    /// owns a <see cref="WarmSpots"/> slot, so the island's own instanced lanterns burn (no extra draws). The smoke is
    /// the map's instanced props until it burns; then it is taken off the map in one go (one island rebuild) and drawn
    /// by short-lived views that roll away. A realm already restored counts it as cleared.
    /// Oct 27 (Mugesh's headset notes): its lesson waits until the village flame is rekindled (<see cref="waitFor"/>; the
    /// two are close and both started at once), and every unlit lantern carries a gold ring floating over its top (the
    /// drop spots' look, <see cref="GuideRings"/>) while the explorer is near, brighter under a fingertip or the gaze.
    /// </summary>
    [DefaultExecutionOrder(-20)] // before tile touch reads the blocked fingertips
    public class LanternHollowPiece : HandPuzzle
    {
        [Header("The hollow (map props grid: column, row)")]
        [Tooltip("Stone lanterns in order along the path (the chain runs this way).")]
        public Vector2Int[] lanterns = new Vector2Int[6];
        [Tooltip("Smoke puffs on the path tiles, in order along the path.")]
        public Vector2Int[] smoke = new Vector2Int[8];
        public PropRecipe smokeRecipe;

        [Header("Touch")]
        [Tooltip("The lantern's top (its cap), tiles above the cap.")]
        public float topHeight = 0.72f;
        [Tooltip("A fingertip this close to the top lights it (metres at the placed island size).")]
        public float touchRadius = 0.04f;

        [Header("Nov 11: the burning fingertip")]
        [Tooltip("Mugesh: light the lanterns with the fire finger. Touch the village flame (or a lit lantern) and the index tip catches fire; " +
                 "poke each lantern with it. Look + pinch still lights one without fire. Off = a cold fingertip touch lights them.")]
        public bool fingertipFire = true;
        [Tooltip("A fingertip this close (m) to the village flame or a lit lantern's top catches fire.")]
        public float fireTouchRadius = 0.035f;
        [Tooltip("The flame goes out by itself after this long (s), or a second after the hand is lost.")]
        public float fireSeconds = 20f;

        [Header("Guidance")]
        [Tooltip("Its lesson waits until this puzzle is solved (the village flame). Empty = no wait.")]
        public string waitFor = "cw.flame";
        [Tooltip("The beacon material (ring mode), as the carried log's drop spots. Empty = no rings.")]
        public Material guideMaterial;
        [Tooltip("Ring size (tiles across) and height over the lantern's top (tiles).")]
        public float ringSize = 0.9f, ringLift = 0.2f, orbSize = 0.35f;

        [Header("Chain (LanternChain)")]
        public float sweepWindow = 1.5f, raceStep = 0.15f, nudgeDelay = 0.5f;
        [Tooltip("Step 14c (the arrival lanterns): one touch sends the fire down the whole line.")]
        public bool raceOnTouch;

        [Header("All lit, no smoke (step 14c arrival)")]
        [Tooltip("Embers drifting up from every lantern once the line is lit.")]
        public int embersPerLantern = 0;

        [Header("Warmth")]
        [Tooltip("WarmSpots slot of the first lantern; the rest follow.")]
        public int firstSlot = 1;
        public float spotRadius = 0.45f;

        [Header("Smoke burning off")]
        public float burnStep = 0.15f, burnSeconds = 0.9f;

        [Header("Lines")]
        public string doneLine = "cw.lanterns.done";
        [Range(0f, 1f)] public float volume = 0.7f;

        LanternChain _chain;
        FingerFlame _flame;
        FlameCupPiece _source;
        int _fireHand = -1;
        float _fireAt, _fireLostAt = -1f, _burnVolume;
        LoopAnchor _burn;
        readonly HandState[] _hands = new HandState[2];
        readonly bool[] _touching = new bool[2];
        PropView[] _burning;
        GuideRings _rings;
        AudioSource _audio;
        int _gazed = -1, _lastLit = -1;
        readonly int[] _near = { -1, -1 };
        bool _burnRunning, _quietSolve;
        float _wispAt;

        public LanternChain Chain => _chain;
        /// <summary>Nov 11: the hand whose fingertip burns (-1 = none).</summary>
        public int FireHand => _fireHand;
        public override bool Busy => false;
        public bool Burning => _burnRunning;
        protected override Vector2 HomeTile => lanterns.Length > 0 ? (Vector2)(lanterns[0] + lanterns[lanterns.Length - 1]) * 0.5f : Vector2.zero;

        public override Vector3? FocusPoint
        {
            get
            {
                if (!Here || IsSolved || _chain == null) return null;
                int best = -1;
                float bestSq = float.MaxValue;
                for (int i = 0; i < lanterns.Length; i++)
                {
                    if (_chain.lit[i]) continue;
                    float d = explorer != null ? ((Vector2)(lanterns[i] - explorer.Tile)).sqrMagnitude : i;
                    if (d < bestSq)
                    {
                        bestSq = d;
                        best = i;
                    }
                }
                return best >= 0 ? TopWorld(best) : (Vector3?)null;
            }
        }

        public override float FocusRadius => 0.5f * TileWorld;

        /// <summary>Step 14c: lanterns lit of all (the chip's "Lanterns n/N").</summary>
        public override bool CountObjective(out int done, out int total)
        {
            total = lanterns.Length;
            done = _chain != null ? Count(_chain.Mask) : 0;
            return true;
        }

        /// <summary>The village flame first (or a restored realm): the lesson and the rings wait for it.</summary>
        protected override bool Available => string.IsNullOrEmpty(waitFor) || PuzzleState.IsSolved(waitFor) || RealmState.Restored >= 0.99f;

        float Scale => grabGuard != null ? grabGuard.ScaleFactor : 1f;

        protected override void Start()
        {
            _chain = new LanternChain(lanterns.Length) { sweepWindow = sweepWindow, raceStep = raceStep, nudgeDelay = nudgeDelay, raceOnTouch = raceOnTouch };
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
            _burning = new PropView[smoke.Length];
            if (island != null)
                for (int i = 0; i < smoke.Length; i++) _burning[i] = new PropView($"Hollow Smoke {i}", island, smokeRecipe);
            _rings = new GuideRings("Lantern Guide", island, guideMaterial, lanterns.Length, fx);
            _flame = new FingerFlame(fx);
            if (guideMaterial == null) Debug.LogError($"[LanternHollowPiece] {name}: no guide material, so no gold rings (Set Up MR_Main (10f Cinderwood hands)).", this);
            base.Start();
        }

        // ---- Nov 11: the fire finger ----

        /// <summary>Where a cold fingertip catches fire: the village flame (once rekindled) and every lit lantern's top.</summary>
        bool FireSource(Vector3 tip, float r, out Vector3 at)
        {
            at = default;
            if (_source == null) _source = FindFirstObjectByType<FlameCupPiece>();
            if (_source != null && _source.IsSolved && _source.Here && (tip - _source.FlameWorld).magnitude <= r * 1.6f)
            {
                at = _source.FlameWorld;
                return true;
            }
            for (int i = 0; i < lanterns.Length; i++)
                if (_chain.lit[i] && (tip - TopWorld(i)).magnitude <= r)
                {
                    at = TopWorld(i);
                    return true;
                }
            return false;
        }

        void Fire()
        {
            float r = fireTouchRadius * Scale;
            if (_fireHand >= 0)
            {
                var s = _hands[_fireHand];
                bool lost = !s.tracked || !s.tipTracked || s.busy;
                if (lost && _fireLostAt < 0f) _fireLostAt = Time.time;
                if (!lost) _fireLostAt = -1f;
                string why = Time.time - _fireAt > fireSeconds ? "timeout" : lost && Time.time - _fireLostAt > 1f ? "hand lost" : null;
                if (why != null)
                {
                    Debug.Log($"[LanternHollowPiece] {id}: the fingertip flame goes out ({why}).");
                    _flame.Hide();
                    _fireHand = -1;
                    return;
                }
                if (!lost)
                {
                    _flame.Show(s.indexTip, island.transform.up);
                    TileTouch.Block(_fireHand == 0, 0.4f);
                }
                return;
            }
            for (int h = 0; h < 2; h++)
            {
                var s = _hands[h];
                if (!s.tracked || !s.tipTracked || s.busy) continue;
                if (!FireSource(s.indexTip, r, out var at)) continue;
                MarkInteracted();
                _fireHand = h;
                _fireAt = Time.time;
                _fireLostAt = -1f;
                _flame.Catch(s.indexTip, island.transform.up);
                FocusHighlight.Hover(at, FocusRadius);
                Debug.Log($"[LanternHollowPiece] {id}: the {(h == 0 ? "left" : "right")} fingertip caught fire; poke the lanterns with it.");
            }
        }

        /// <summary>A quiet steady burn over the lit lanterns (one loop, louder with more lit), once the clip exists.</summary>
        void BurnSound()
        {
            int lit = _chain != null ? Count(_chain.Mask) : 0;
            bool on = Here && lit > 0 && !_burnRunning && InView && AudioService.Has(AudioIds.LanternBurn);
            if (!on)
            {
                _burn?.Off(0.4f);
                return;
            }
            Vector3 sum = Vector3.zero;
            for (int i = 0; i < lanterns.Length; i++)
                if (_chain.lit[i]) sum += TopWorld(i);
            _burn ??= new LoopAnchor(transform, "Lantern Burn");
            _burn.Set(AudioIds.LanternBurn, sum / lit, 0.4f);
            _burnVolume = Mathf.MoveTowards(_burnVolume, Mathf.Min(1f, 0.25f + 0.1f * lit), Time.deltaTime);
            _burn.Handle.SetVolume(_burnVolume * volume);
        }

        protected override void OnDestroy()
        {
            if (_burning != null)
                foreach (var v in _burning) v?.Destroy();
            _rings?.Destroy();
            _flame?.Hide();
            _burn?.Off(0f);
            if (map != null)
                foreach (var t in smoke)
                {
                    map.SetPropHidden(t, false);
                    map.SetPropOpen(t, false);
                }
            for (int i = 0; i < lanterns.Length; i++) WarmSpots.Clear(firstSlot + i);
            base.OnDestroy();
        }

        protected override void ApplyState()
        {
            StopAllCoroutines();
            _burnRunning = false;
            int all = (1 << lanterns.Length) - 1;
            _chain.Restore(IsSolved ? all : PuzzleState.Stage(id));
            bool clear = IsSolved;
            foreach (var t in smoke)
            {
                map.SetPropHidden(t, clear);
                map.SetPropOpen(t, clear);
            }
            if (_burning != null)
                foreach (var v in _burning) v?.Hide();
            SetProgressQuiet(Count(_chain.Mask) / (float)Mathf.Max(1, lanterns.Length));
            Debug.Log($"[LanternHollowPiece] {id}: {Count(_chain.Mask)}/{lanterns.Length} lanterns lit, smoke {(clear ? "gone" : "on the path")}.");
        }

        protected override void Update()
        {
            ReadHands(_hands);
            if (!IsSolved && Here && RealmState.Restored >= 0.99f) QuietSolve();
            if (Usable && !IsSolved && !_burnRunning)
            {
                if (fingertipFire) Fire();
                Touches();
                LookPinch();
            }
            else
            {
                _gazed = -1;
                if (_fireHand >= 0)
                {
                    _flame.Hide();
                    _fireHand = -1;
                }
            }
            BurnSound();
            if (!IsSolved)
            {
                int k = _chain.Tick(Time.deltaTime);
                if (k >= 0) Lit(k, "it caught");
                if (_chain.AllLit && !_burnRunning && Here) StartCoroutine(Burn());
            }
            Warm();
            Wisps();
            Guide();
            base.Update();
        }

        /// <summary>Oct 27 headset: dark wisps curl up off the smoke now and then, so it reads as smoke, not blocks.</summary>
        void Wisps()
        {
            if (IsSolved || _burnRunning || fx == null || !InView || smoke.Length == 0 || Time.time < _wispAt) return;
            _wispAt = Time.time + 0.3f;
            var t = smoke[Random.Range(0, smoke.Length)];
            float tile = TileWorld;
            var at = MapToWorld(new Vector3(t.x + Random.Range(-0.3f, 0.3f), 0.85f, t.y + Random.Range(-0.3f, 0.3f)));
            fx.Burst(at, 2, new Color(0.3f, 0.29f, 0.28f, 0.7f), tile * 0.12f, 1.6f, tile * 0.13f, island.transform.up * tile * 0.35f);
        }

        // ---- lighting ----

        void Touches()
        {
            float r = touchRadius * Scale;
            for (int h = 0; h < 2; h++)
            {
                var hand = HandRegistry.Get(h == 0 ? Handedness.Left : Handedness.Right);
                if (_hands[h].busy || !HandLookup.TryGetJoint(hand, HandJointId.HandIndexTip, out var tip))
                {
                    _touching[h] = false;
                    _near[h] = -1;
                    continue;
                }
                int touched = -1;
                bool near = false;
                float best = r * 2.5f;
                _near[h] = -1;
                for (int i = 0; i < lanterns.Length; i++)
                {
                    float d = (tip - TopWorld(i)).magnitude;
                    if (d <= r * 3f) near = true;
                    if (d <= r) touched = i;
                    if (d < best && !_chain.lit[i])
                    {
                        best = d;
                        _near[h] = i;
                    }
                }
                // A fingertip at the lanterns never walks the explorer.
                if (near) TileTouch.Block(h == 0, 0.4f);
                bool was = _touching[h];
                _touching[h] = touched >= 0;
                if (touched < 0 || was) continue;
                // Nov 11: only a burning fingertip lights one (a cold touch does nothing; look + pinch is the other door).
                if (fingertipFire && h != _fireHand) continue;
                MarkInteracted();
                if (fingertipFire && !_chain.lit[touched]) _flame.Flare(TopWorld(touched), island.transform.up);
                bool litNow = _chain.Touch(touched);
                Debug.Log($"[LanternHollowPiece] {id}: lantern {touched} touched ({(h == 0 ? "left" : "right")}){(_chain.Racing ? ", the fire races on" : "")}.");
                if (litNow) Lit(touched, "touched");
            }
        }

        void LookPinch()
        {
            if (!HeadGaze.TryGetRay(out var ray))
            {
                _gazed = -1;
                return;
            }
            var pick = new HeadGaze.ConePick(ray, _gazed);
            for (int i = 0; i < lanterns.Length; i++)
                if (!_chain.lit[i]) pick.Consider(i, TopWorld(i), Mathf.Max(HeadGaze.MinRadius * island.transform.lossyScale.x, 0.6f * TileWorld));
            _gazed = pick.Best;
            if (_gazed < 0) return;
            FocusHighlight.Hover(TopWorld(_gazed), FocusRadius);
            if (!GazePinch(_hands)) return;
            MarkInteracted();
            int i0 = _gazed;
            if (_chain.Nudge(i0)) Lit(i0, "look + pinch");
        }

        void Lit(int i, string how)
        {
            LessonComplete();
            int from = _lastLit;
            _lastLit = i;
            PuzzleState.SetStage(id, _chain.Mask);
            SetProgress(Count(_chain.Mask) / (float)Mathf.Max(1, lanterns.Length));
            Debug.Log($"[LanternHollowPiece] {id}: lantern {i} lit ({how}), {Count(_chain.Mask)}/{lanterns.Length}.");
            if (Here) GameAudio.Play(AudioIds.LanternLight, TopWorld(i), _audio, PuzzleSounds.Catch, volume * 0.6f);
            if (fx == null || !Here) return;
            float tile = TileWorld;
            fx.Burst(TopWorld(i) + Vector3.down * tile * 0.25f, 14, new Color(1f, 0.66f, 0.28f), tile * 0.45f, 0.7f, tile * 0.05f, island.transform.up * tile * 0.6f);
            // A spark trail jumps along the path from the one before.
            if (from >= 0 && from != i) StartCoroutine(Sparks(from, i));
        }

        IEnumerator Sparks(int a, int b)
        {
            const int steps = 5;
            float seconds = Mathf.Min(raceStep, 0.15f);
            for (int s = 1; s <= steps; s++)
            {
                if (fx != null && Here)
                {
                    float u = s / (float)(steps + 1);
                    var p = Vector3.Lerp(TopWorld(a), TopWorld(b), u) + island.transform.up * TileWorld * 0.25f * Mathf.Sin(u * Mathf.PI);
                    fx.Burst(p, 3, new Color(1f, 0.75f, 0.35f), TileWorld * 0.15f, 0.45f, TileWorld * 0.035f, Vector3.zero);
                }
                yield return new WaitForSeconds(seconds / steps);
            }
        }

        // ---- the smoke burns off ----

        IEnumerator Burn()
        {
            _burnRunning = true;
            // From the lit end: puffs nearest the lantern the fire started at go first.
            var origin = (Vector2)lanterns[Mathf.Clamp(_chain.Origin >= 0 ? _chain.Origin : 0, 0, lanterns.Length - 1)];
            var order = new List<int>();
            for (int i = 0; i < smoke.Length; i++) order.Add(i);
            order.Sort((x, y) => ((Vector2)smoke[x] - origin).sqrMagnitude.CompareTo(((Vector2)smoke[y] - origin).sqrMagnitude));
            Debug.Log($"[LanternHollowPiece] {id}: all lanterns lit{(smoke.Length > 0 ? $", the smoke burns off from lantern {_chain.Origin}" : "")}.");
            if (embersPerLantern > 0) StartCoroutine(Embers());
            // Off the map in one go (one island rebuild), the views roll each puff away.
            foreach (var t in smoke)
            {
                map.SetPropHidden(t, true);
                map.SetPropOpen(t, true);
            }
            GameAudio.Play(AudioIds.SmokeClear, TopWorld(Mathf.Clamp(_chain.Origin >= 0 ? _chain.Origin : 0, 0, lanterns.Length - 1)), _audio, PuzzleSounds.Whoosh, volume);
            float total = burnStep * (order.Count - 1) + burnSeconds;
            float tile = TileWorld;
            var started = new bool[smoke.Length];
            for (float t = 0f; t < total; t += Time.deltaTime)
            {
                for (int k = 0; k < order.Count; k++)
                {
                    int i = order[k];
                    float u = Mathf.Clamp01((t - k * burnStep) / burnSeconds);
                    if (u > 0f && !started[i])
                    {
                        started[i] = true;
                        if (fx != null && Here)
                        {
                            var at = MapToWorld(new Vector3(smoke[i].x, 0.4f, smoke[i].y));
                            fx.Burst(at, 10, new Color(0.33f, 0.29f, 0.36f, 0.85f), tile * 0.5f, 1.1f, tile * 0.16f, island.transform.up * tile * 0.9f);
                            fx.Burst(at, 8, new Color(1f, 0.55f, 0.2f), tile * 0.7f, 0.6f, tile * 0.04f, island.transform.up * tile * 0.5f);
                        }
                    }
                    var v = _burning[i];
                    if (v == null) continue;
                    // Rolls up and away: swells a little, rises, shrinks to nothing.
                    float size = u <= 0f ? 1f : Mathf.Lerp(1.1f, 0f, Mathf.SmoothStep(0f, 1f, u));
                    v.Place(smoke[i], u * 0.6f, Quaternion.Euler(0f, 31f * i + u * 90f, 0f), Here && size > 0.02f, size);
                }
                yield return null;
            }
            foreach (var v in _burning) v?.Hide();
            _burnRunning = false;
            if (!string.IsNullOrEmpty(doneLine)) Dialogue.Say(doneLine);
            if (explorer != null) explorer.Cheer();
            MarkSolved(_chain.Mask);
        }

        /// <summary>Step 14c: the line is lit: embers drift up from every lantern for a couple of seconds (the arrival's aha).</summary>
        IEnumerator Embers()
        {
            float tile = TileWorld;
            for (int wave = 0; wave < 4; wave++)
            {
                if (fx != null && Here)
                    for (int i = 0; i < lanterns.Length; i++)
                    {
                        var at = TopWorld(i) + island.transform.up * tile * 0.1f;
                        fx.Burst(at, Mathf.Max(1, embersPerLantern / 2), new Color(1f, 0.6f + 0.15f * (wave % 2), 0.22f), tile * 0.35f, 1.8f, tile * 0.03f,
                            island.transform.up * tile * 0.7f, -tile * 0.15f);
                    }
                yield return new WaitForSeconds(0.45f);
            }
        }

        void QuietSolve()
        {
            if (_quietSolve) return;
            _quietSolve = true;
            Debug.Log($"[LanternHollowPiece] {id}: the realm is restored, so the hollow's lanterns burn and the smoke is gone.");
            _chain.Restore((1 << lanterns.Length) - 1);
            foreach (var t in smoke)
            {
                map.SetPropHidden(t, true);
                map.SetPropOpen(t, true);
            }
            MarkSolved(_chain.Mask);
        }

        void Warm()
        {
            float ts = island != null && island.map != null ? island.map.tileSize : 0.05f;
            for (int i = 0; i < lanterns.Length; i++)
            {
                if (Here && _chain != null && _chain.lit[i]) WarmSpots.Set(firstSlot + i, lanterns[i], spotRadius, 0.15f, ts);
                else WarmSpots.Clear(firstSlot + i);
            }
        }

        /// <summary>A gold ring over every unlit lantern's top while the explorer is near; brighter under a fingertip or the gaze.</summary>
        void Guide()
        {
            if (_rings == null) return;
            bool on = InView && Available && !IsSolved && !_burnRunning && _chain != null && explorer != null &&
                      Mathf.Max(Mathf.Abs(explorer.Tile.x - HomeTile.x), Mathf.Abs(explorer.Tile.y - HomeTile.y)) <= reach + 3;
            if (!Usable) _near[0] = _near[1] = -1;
            for (int i = 0; i < lanterns.Length; i++)
            {
                bool hot = _near[0] == i || _near[1] == i || _gazed == i;
                float pulse = 0.8f + 0.2f * Mathf.Sin(Time.time * 3f + i * 0.9f);
                _rings.Draw(i, on && !_chain.lit[i], new Vector3(lanterns[i].x, topHeight + ringLift, lanterns[i].y), ringSize * (hot ? 1.2f : 1f), hot ? 2.6f : 1.6f * pulse, orbSize * (hot ? 1.3f : pulse));
            }
        }

        Vector3 TopWorld(int i) => MapToWorld(new Vector3(lanterns[i].x, topHeight, lanterns[i].y));

        static int Count(int mask)
        {
            int n = 0;
            for (; mask != 0; mask &= mask - 1) n++;
            return n;
        }

        // ---- bridge ----

        /// <summary>Debug (bridge): a fingertip touch on lantern <paramref name="i"/>.</summary>
        public void DebugTouch(int i)
        {
            if (!Here || IsSolved || i < 0 || i >= lanterns.Length) return;
            MarkInteracted();
            if (fingertipFire && _fireHand < 0) DebugCatchFire(1);
            float since = _chain.Clock - _chain.LastTouchAt;
            bool litNow = _chain.Touch(i);
            Debug.Log($"[LanternHollowPiece] {id}: {since:0.00} s since the last touch; {i}{(_chain.Racing ? ", the fire races on" : "")} (clock {_chain.Clock:0.00}, frame {Time.frameCount}).");
            if (litNow) Lit(i, "debug touch");
        }

        /// <summary>Debug (bridge): look + pinch on lantern <paramref name="i"/>.</summary>
        public void DebugNudge(int i)
        {
            if (!Here || IsSolved || i < 0 || i >= lanterns.Length) return;
            MarkInteracted();
            if (_chain.Nudge(i)) Lit(i, "debug look + pinch");
        }

        /// <summary>Debug (bridge): the fingertip catches fire from the village flame (hand 0 / 1).</summary>
        public void DebugCatchFire(int hand)
        {
            if (!Here || IsSolved || _fireHand >= 0) return;
            _fireHand = Mathf.Clamp(hand, 0, 1);
            _fireAt = Time.time;
            _fireLostAt = -1f;
            Debug.Log($"[LanternHollowPiece] {id}: debug: the {(hand == 0 ? "left" : "right")} fingertip caught fire.");
        }

        public string Describe() =>
            $"lanterns {Count(_chain != null ? _chain.Mask : 0)}/{lanterns.Length} (mask {(_chain != null ? _chain.Mask : 0)}), racing {(_chain != null && _chain.Racing)}, burning {_burnRunning}, fire hand {_fireHand}, " +
            $"smoke tiles open {(map != null && smoke.Length > 0 && map.IsPropOpen(smoke[0]))}";
    }
}
