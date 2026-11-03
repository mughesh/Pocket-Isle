using System.Collections;
using Diorama.Audio;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Nov 3 (Mugesh: Cinderwood's arrival lanterns were bland): a thorny vine has grown across the trail just past the
    /// arrival (trees either side, the only way on), and an ember vent glows in the arrival plaza. Pick and place, the
    /// forge's way (<see cref="EmberCarry"/>): pinch or grab the ember and it comes with that hand, glowing; let go over the
    /// vine and it catches, anywhere else the ember falls back to its vent. Gold rings show the way (the vent's while the
    /// ember waits, the vine's brighter while it's carried), gold sparks stream from the carried ember to the vine. Look +
    /// pinch the vent, then the vine: the ember flies there by itself. Caught: fire runs along the vine, it chars, shrinks
    /// into the ground in a burst of embers and smoke, its tiles open (saved, solved). The forge's ember later asks the
    /// same hands again, with the bellows after it. A realm already restored counts it as burnt.
    /// The vine is the map's instanced props until it burns; then it is taken off the map in one go (one island rebuild)
    /// and drawn by short-lived views that burn away.
    /// </summary>
    [DefaultExecutionOrder(-20)] // before the gaze walk, tile touch and microgestures read the held hand
    public class VineBurnPiece : HandPuzzle
    {
        [Header("Places (map props grid: column, row)")]
        public Vector2Int vent;
        [Tooltip("The vine's tiles across the trail.")]
        public Vector2Int[] vine = new Vector2Int[2];
        public PropRecipe emberRecipe, vineRecipe;

        [Header("Shape (tiles)")]
        public float emberSize = 0.34f;
        [Tooltip("The ember resting in its vent, above the cap.")]
        public float ventHeight = 0.08f;
        [Tooltip("The vine's middle, above the cap (where the ember is let go).")]
        public float vineHeight = 0.4f;

        [Header("Hands (metres at the placed island size)")]
        [Tooltip("A pinch or grab started this close to the ember picks it.")]
        public float grabRadius = 0.04f;
        [Tooltip("Letting go over the vine: this far from a vine tile's middle (across), and from a little below up to this high above it.")]
        public float vineRadius = 0.07f, vineAbove = 0.18f;

        [Header("Guidance (gold rings, tiles across)")]
        [Tooltip("The beacon material (ring mode), as the carried log's drop spots. Empty = no rings.")]
        public Material guideMaterial;
        public float ventRing = 0.9f, vineRing = 1.6f;

        [Header("Burning")]
        public float burnSeconds = 1.6f;

        [Header("Lines")]
        [Tooltip("Said (the Spirit points at the vine) when the ember is picked up, at most every 12 s.")]
        public string tipLine = "cw.vine.tip";
        [Tooltip("The pinned chip while the ember is carried.")]
        public string tipChip = "Drop it on the vine!";
        public string doneLine = "cw.vine.done";
        [Range(0f, 1f)] public float volume = 0.7f;

        readonly EmberCarry _carry = new EmberCarry();
        readonly HandState[] _hands = new HandState[2];
        readonly EmberCarry.Hand[] _in = new EmberCarry.Hand[2];
        readonly Vector3[] _grip = new Vector3[2];
        PropView _ember;
        PropView[] _burning;
        BattleFx.Item _halo;
        GuideRings _rings;
        AudioSource _audio;
        Vector3 _emberWorld, _fallFrom;
        float _anim, _sparkAt, _trailAt, _tipSaidAt = -99f;
        int _gazed = -1; // 0 = the ember / vent, 1 = the vine
        bool _burnRunning, _quietSolve;

        public EmberCarry Carry => _carry;
        public bool Burning => _burnRunning;
        public override bool Busy => _carry.State == EmberState.Held || _carry.State == EmberState.Flying;
        protected override Vector2 HomeTile => VineMiddle;

        public override Vector3? FocusPoint => Here && !IsSolved && (_carry.State == EmberState.InVent || _carry.State == EmberState.Hovering) ? _emberWorld : (Vector3?)null;
        public override float FocusRadius => 0.5f * TileWorld;
        /// <summary>The lesson's grab_drop ghost picks the ember and lets go over the vine.</summary>
        public override Vector3? GhostDropPoint => Here && !IsSolved ? VineWorld : (Vector3?)null;

        float Scale => grabGuard != null ? grabGuard.ScaleFactor : 1f;
        Vector2 VineMiddle
        {
            get
            {
                if (vine == null || vine.Length == 0) return vent;
                Vector2 sum = Vector2.zero;
                foreach (var t in vine) sum += t;
                return sum / vine.Length;
            }
        }

        Vector3 VentWorld => MapToWorld(new Vector3(vent.x, ventHeight, vent.y));
        Vector3 HoverWorld => MapToWorld(new Vector3(vent.x, ventHeight + 0.9f + 0.08f * Mathf.Sin(Time.time * 3f), vent.y));
        public Vector3 VineWorld => MapToWorld(new Vector3(VineMiddle.x, vineHeight, VineMiddle.y));
        Vector3 TileTop(Vector2Int t) => MapToWorld(new Vector3(t.x, vineHeight, t.y));

        protected override void Start()
        {
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
            if (island != null)
            {
                _ember = new PropView("Vine Ember", island, emberRecipe);
                _burning = new PropView[vine.Length];
                for (int i = 0; i < vine.Length; i++) _burning[i] = new PropView($"Burning Vine {i}", island, vineRecipe);
            }
            _rings = new GuideRings("Vine Guide", island, guideMaterial, 2, fx);
            Dialogue.RegisterTarget("vine", () => Here && !IsSolved ? VineWorld : (Vector3?)null, null, () => 0.8f * TileWorld);
            if (guideMaterial == null) Debug.LogError($"[VineBurnPiece] {name}: no guide material, so no gold rings (Set Up MR_Main (14e Nov 3 late notes)).", this);
            if (emberRecipe == null || vineRecipe == null) Debug.LogError($"[VineBurnPiece] {name}: no ember or vine recipe (Set Up MR_Main (14e Nov 3 late notes)).", this);
            base.Start();
        }

        protected override void OnDestroy()
        {
            _ember?.Destroy();
            if (_burning != null)
                foreach (var v in _burning) v?.Destroy();
            _rings?.Destroy();
            ReleaseHalo();
            if (map != null)
                foreach (var t in vine)
                {
                    map.SetPropHidden(t, false);
                    map.SetPropOpen(t, false);
                }
            base.OnDestroy();
        }

        protected override void ApplyState()
        {
            StopAllCoroutines();
            _burnRunning = false;
            bool burnt = IsSolved;
            _carry.Restore(burnt);
            foreach (var t in vine)
            {
                map.SetPropHidden(t, burnt);
                map.SetPropOpen(t, burnt);
            }
            if (_burning != null)
                foreach (var v in _burning) v?.Hide();
            SetProgressQuiet(burnt ? 1f : 0f);
            Debug.Log($"[VineBurnPiece] {id}: the vine is {(burnt ? "burnt, the trail open" : "across the trail")}.");
        }

        protected override void Update()
        {
            ReadHands(_hands);
            if (!IsSolved && Here && RealmState.Restored >= 0.99f) QuietSolve();
            if (!IsSolved && !_burnRunning) Hands();
            Draw();
            base.Update();
        }

        // ---- hands ----

        void Hands()
        {
            bool usable = Usable;
            var state = _carry.State;
            if (state == EmberState.InVent || state == EmberState.Hovering || state == EmberState.Held)
            {
                for (int h = 0; h < 2; h++) _in[h] = usable || state == EmberState.Held ? Measure(h) : default;
                var was = state;
                var now = _carry.Feed(Time.deltaTime, _in);
                if (now != was) Changed(was, now);
            }
            if (usable && (_carry.State == EmberState.InVent || _carry.State == EmberState.Hovering)) LookPinch();
            else _gazed = -1;
            if (_carry.State == EmberState.Held)
            {
                // The carrying hand never walks the explorer or swipes.
                IslandHandle.MarkHandHeld(_carry.Holder == 0);
                TileTouch.Block(_carry.Holder == 0, 0.4f);
            }
        }

        EmberCarry.Hand Measure(int h)
        {
            var s = _hands[h];
            var x = new EmberCarry.Hand { tracked = s.tracked, busy = s.busy, holding = s.Holding, gripStarted = s.pinchStarted || s.fistStarted };
            float scale = Scale;
            float reach = Mathf.Max(grabRadius * scale + 0.25f * emberSize * TileWorld, grabAssist);
            bool waiting = _carry.State == EmberState.InVent || _carry.State == EmberState.Hovering;
            if (s.tracked && !s.busy && waiting && (s.GripPoint - _emberWorld).magnitude <= reach) FocusHighlight.Hover(_emberWorld, FocusRadius);
            if (!s.tracked || !s.Holding) return x;
            _grip[h] = s.GripPoint;
            x.atEmber = (_grip[h] - _emberWorld).magnitude <= reach;
            x.overFirebox = OverVine(_grip[h], scale);
            return x;
        }

        /// <summary>Over any vine tile: within <see cref="vineRadius"/> across, from a little below up to <see cref="vineAbove"/> above.</summary>
        bool OverVine(Vector3 p, float scale)
        {
            Vector3 up = island.transform.up;
            foreach (var t in vine)
            {
                Vector3 d = p - TileTop(t);
                float above = Vector3.Dot(d, up);
                if ((d - up * above).magnitude <= vineRadius * scale && above >= -0.06f * scale && above <= vineAbove * scale) return true;
            }
            return false;
        }

        void Changed(EmberState was, EmberState now)
        {
            switch (now)
            {
                case EmberState.Held:
                    MarkInteracted();
                    Dialogue.SetChip(tipChip);
                    if (Time.time - _tipSaidAt > 12f && !string.IsNullOrEmpty(tipLine))
                    {
                        _tipSaidAt = Time.time;
                        Dialogue.Repeat(tipLine);
                    }
                    Debug.Log($"[VineBurnPiece] {id}: picked up ({(_carry.Holder == 0 ? "left" : "right")} hand, {(_hands[_carry.Holder].pinch ? "pinch" : "grab")}).");
                    GameAudio.Play(AudioIds.EmberPick, _emberWorld, _audio, PuzzleSounds.Catch, volume * 0.5f);
                    if (fx != null) fx.Burst(_emberWorld, 10, new Color(1f, 0.6f, 0.25f), TileWorld * 0.4f, 0.6f, TileWorld * 0.04f, island.transform.up * TileWorld * 0.8f);
                    break;
                case EmberState.Falling:
                    Debug.Log($"[VineBurnPiece] {id}: let go away from the vine, the ember falls back to its vent.");
                    _fallFrom = _emberWorld;
                    _anim = 0f;
                    break;
                case EmberState.In:
                    Debug.Log($"[VineBurnPiece] {id}: dropped on the vine.");
                    StartCoroutine(Burn(_emberWorld));
                    break;
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
            float r = Mathf.Max(HeadGaze.MinRadius * island.transform.lossyScale.x, 0.6f * TileWorld);
            pick.Consider(0, _emberWorld, r);
            if (_carry.State == EmberState.Hovering) pick.Consider(1, VineWorld, Mathf.Max(r, 0.9f * TileWorld));
            _gazed = pick.Best;
            if (_gazed < 0) return;
            FocusHighlight.Hover(_gazed == 0 ? _emberWorld : VineWorld, FocusRadius);
            if (!GazePinch(_hands)) return;
            MarkInteracted();
            if (_carry.State == EmberState.InVent)
            {
                _carry.Hover();
                GameAudio.At(AudioIds.EmberPick, _emberWorld);
                Debug.Log($"[VineBurnPiece] {id}: look + pinch: the ember rises and waits (look + pinch the vine).");
            }
            else if (_gazed == 1)
            {
                _carry.Fly();
                Debug.Log($"[VineBurnPiece] {id}: look + pinch the vine: the ember flies there.");
                StartCoroutine(FlyIn());
            }
        }

        IEnumerator FlyIn()
        {
            var from = _emberWorld;
            for (float t = 0f; t < 0.9f; t += Time.deltaTime)
            {
                float u = Mathf.SmoothStep(0f, 1f, t / 0.9f);
                _emberWorld = Vector3.Lerp(from, VineWorld, u) + island.transform.up * TileWorld * 1.2f * Mathf.Sin(u * Mathf.PI);
                yield return null;
            }
            _carry.Arrive();
            Changed(EmberState.Flying, EmberState.In);
        }

        // ---- the vine burns ----

        IEnumerator Burn(Vector3 at)
        {
            _burnRunning = true;
            LessonComplete();
            GameAudio.Play(AudioIds.EmberDrop, at, _audio, PuzzleSounds.Crackle, volume * 0.8f);
            GameAudio.Play(AudioIds.FireboxRoar, at, _audio, PuzzleSounds.Whoosh, volume);
            // Off the map in one go (one island rebuild); the views burn away.
            foreach (var t in vine)
            {
                map.SetPropHidden(t, true);
                map.SetPropOpen(t, true);
            }
            float tile = TileWorld;
            float next = 0f;
            for (float t = 0f; t < burnSeconds; t += Time.deltaTime)
            {
                float u = Mathf.Clamp01(t / burnSeconds);
                for (int i = 0; i < vine.Length; i++)
                {
                    var v = _burning != null ? _burning[i] : null;
                    if (v == null) continue;
                    // Fire runs out from the middle: each tile starts a little later, chars, then sinks to nothing.
                    float k = Mathf.Clamp01((u - 0.12f * i) / 0.8f);
                    float size = Mathf.Lerp(1f, 0f, Mathf.SmoothStep(0f, 1f, k));
                    v.restoredSelf = 1f - k;
                    v.glowBoostB = new Vector4(2f * Mathf.Sin(k * Mathf.PI), 0f, 0f, 0f);
                    v.Place(vine[i], -0.25f * k, Quaternion.identity, Here && size > 0.02f, size);
                }
                if (fx != null && Here && t >= next)
                {
                    next = t + 0.08f;
                    foreach (var vt in vine)
                    {
                        var p = MapToWorld(new Vector3(vt.x + Random.Range(-0.4f, 0.4f), Random.Range(0.1f, 0.6f), vt.y + Random.Range(-0.3f, 0.3f)));
                        fx.Burst(p, 4, new Color(1f, 0.55f + Random.Range(0f, 0.2f), 0.18f), tile * 0.5f, 0.5f, tile * 0.07f, island.transform.up * tile * 0.9f);
                        if (u > 0.4f) fx.Burst(p, 2, new Color(0.3f, 0.28f, 0.27f, 0.8f), tile * 0.2f, 1.3f, tile * 0.12f, island.transform.up * tile * 0.5f);
                    }
                }
                yield return null;
            }
            if (_burning != null)
                foreach (var v in _burning) v?.Hide();
            if (fx != null && Here) fx.Burst(VineWorld, 26, new Color(1f, 0.7f, 0.3f), tile * 1.2f, 0.9f, tile * 0.05f, island.transform.up * tile * 1.2f);
            _burnRunning = false;
            MarkSolved(1);
            if (explorer != null) explorer.Cheer();
            if (!string.IsNullOrEmpty(doneLine)) Dialogue.Say(doneLine);
            Debug.Log($"[VineBurnPiece] {id}: the vine burnt away, the trail is open.");
        }

        void QuietSolve()
        {
            if (_quietSolve) return;
            _quietSolve = true;
            Debug.Log($"[VineBurnPiece] {id}: the realm is restored, so the vine is gone (no ember needed).");
            _carry.Restore(true);
            MarkSolved(1);
        }

        // ---- looks ----

        void Draw()
        {
            var s = _carry.State;
            bool show = Here && !IsSolved && s != EmberState.In;
            switch (s)
            {
                case EmberState.InVent:
                    _emberWorld = VentWorld;
                    break;
                case EmberState.Hovering:
                    _emberWorld = Vector3.Lerp(_emberWorld, HoverWorld, Time.deltaTime * 4f);
                    break;
                case EmberState.Held:
                    if (_carry.Holder >= 0) _emberWorld = Vector3.Lerp(_emberWorld, _grip[_carry.Holder], Time.deltaTime * 25f);
                    break;
                case EmberState.Falling:
                    _anim += Time.deltaTime / 0.5f;
                    float u = Mathf.Clamp01(_anim);
                    _emberWorld = Vector3.Lerp(_fallFrom, VentWorld, u * u) + island.transform.up * TileWorld * 0.6f * Mathf.Sin(u * Mathf.PI) * (1f - u);
                    if (u >= 1f)
                    {
                        _carry.Back();
                        if (fx != null && Here) fx.Burst(VentWorld, 6, new Color(1f, 0.5f, 0.2f), TileWorld * 0.3f, 0.5f, TileWorld * 0.03f, island.transform.up * TileWorld * 0.3f);
                    }
                    break;
            }
            Guide(show);
            if (_ember == null) return;
            if (!show)
            {
                _ember.Hide();
                ReleaseHalo();
                return;
            }
            bool carried = s == EmberState.Held || s == EmberState.Flying || s == EmberState.Hovering;
            float pulse = carried ? 1f : 0.55f + 0.2f * Mathf.Sin(Time.time * 2.4f);
            _ember.restoredSelf = carried ? 1f : 0.7f;
            _ember.glowBoostB = new Vector4(carried ? 1.2f : 0.5f, 0f, 0f, 0f);
            var local = island.transform.InverseTransformPoint(_emberWorld);
            float metres = emberSize * TileWorld / Mathf.Max(1e-5f, island.transform.lossyScale.x);
            if (carried) _ember.PlaceLocal(local, Quaternion.Euler(Time.time * 50f, Time.time * 70f, 0f), metres, true);
            else
            {
                _ember.windowClip = true;
                _ember.Place(new Vector2(vent.x, vent.y), ventHeight - emberSize * 0.3f, Quaternion.Euler(0f, Time.time * 20f, 0f), true, emberSize);
            }
            Halo(pulse, carried);
            if (carried && fx != null && Time.time >= _sparkAt)
            {
                _sparkAt = Time.time + 0.16f;
                fx.Burst(_emberWorld, 1, new Color(1f, 0.7f, 0.3f), TileWorld * 0.15f, 0.8f, TileWorld * 0.03f, island.transform.up * TileWorld * 0.9f);
            }
        }

        bool Near => explorer != null && Mathf.Max(Mathf.Abs(explorer.Tile.x - VineMiddle.x), Mathf.Abs(explorer.Tile.y - VineMiddle.y)) <= reach + 3;

        /// <summary>
        /// The gold rings: the vent's while the ember waits there, the vine's always while unburnt (dim), brighter while
        /// the ember is carried, brightest when letting go would drop it on. Carried: gold sparks stream toward the vine.
        /// </summary>
        void Guide(bool show)
        {
            if (_rings == null) return;
            var s = _carry.State;
            bool on = show && InView && Near && !_burnRunning;
            bool waiting = s == EmberState.InVent;
            bool carried = s == EmberState.Held || s == EmberState.Hovering;
            float pulse = 0.8f + 0.2f * Mathf.Sin(Time.time * 3f);
            _rings.Draw(0, on && waiting, new Vector3(vent.x, ventHeight + 0.05f, vent.y), ventRing, 1.5f * pulse);
            float glow = _carry.OverFirebox || _gazed == 1 ? 2.6f : carried ? 1.8f * pulse : 0.8f * pulse;
            var mid = VineMiddle;
            _rings.Draw(1, on && s != EmberState.Falling, new Vector3(mid.x, vineHeight + 0.3f, mid.y), vineRing * (_carry.OverFirebox ? 1.15f : 1f), glow, (carried ? 0.75f : 0.4f) * pulse);
            if (!on || !carried || fx == null || Time.time < _trailAt) return;
            // A dotted line of sparks drifting from the ember to the vine: "it goes there".
            _trailAt = Time.time + 0.07f;
            float u = Mathf.Repeat(Time.time * 0.9f, 1f);
            float tile = TileWorld;
            var p = Vector3.Lerp(_emberWorld, VineWorld, u) + island.transform.up * tile * 0.8f * Mathf.Sin(u * Mathf.PI);
            fx.Burst(p, 1, new Color(1f, 0.78f, 0.3f), tile * 0.02f, 0.45f, tile * 0.05f, Vector3.zero);
        }

        void Halo(float strength, bool carried)
        {
            if (fx == null) return;
            if (_halo == null)
            {
                _halo = fx.Get(BattleFx.Shape.Orb);
                if (_halo == null) return;
                _halo.Set(new Color(1f, 0.85f, 0.55f), new Color(1f, 0.5f, 0.18f), 1f, 1.5f, 1f, 0.3f);
            }
            _halo.transform.position = _emberWorld;
            float size = TileWorld * (carried ? 0.9f : 0.6f) * (0.92f + 0.08f * Mathf.Sin(Time.time * 9f));
            _halo.transform.localScale = Vector3.one * (size / Mathf.Max(1e-4f, fx.transform.lossyScale.x));
            _halo.SetAlpha(0.6f * strength);
        }

        void ReleaseHalo()
        {
            if (_halo == null || fx == null) return;
            fx.Release(_halo);
            _halo = null;
        }

        // ---- bridge ----

        /// <summary>Debug (bridge): look + pinch on the vent: the ember rises and waits.</summary>
        public void DebugLookPinchVent()
        {
            if (!Here || IsSolved) return;
            MarkInteracted();
            _carry.Hover();
            Debug.Log($"[VineBurnPiece] {id}: debug look + pinch on the vent (state {_carry.State}).");
        }

        /// <summary>Debug (bridge): look + pinch the vine while the ember waits.</summary>
        public void DebugLookPinchVine()
        {
            if (!Here || IsSolved || _carry.State != EmberState.Hovering) return;
            _carry.Fly();
            StartCoroutine(FlyIn());
        }

        public string Describe() => $"ember {_carry.State}{(_carry.Holder >= 0 ? $" (hand {_carry.Holder})" : "")}, vine {(IsSolved ? "burnt" : _burnRunning ? "burning" : "across the trail")}";
    }
}
