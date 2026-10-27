using System.Collections;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 10f, Cinderwood's forge temple, before the bellows: the forge's firebox (a stone mouth in the cliff beside
    /// the bellows, a chimney up the cliff) is cold, so the bellows only give a grey puff (<see cref="PumpPiece.Blocked"/>).
    /// An ember vent near the forge glows faintly with a live ember in it. Hold an open hand palm-up beside the vent (or
    /// pinch the ember): it hops up and sits on your palm, glowing (a warm halo, sparks drifting up), for as long as the
    /// palm stays roughly up. Turn the hand over above the firebox and it tips in; tipped or dropped anywhere else it
    /// falls back to its vent (<see cref="EmberCarry"/>). Look + pinch the vent, then the firebox: it flies in by itself.
    /// Ember in: the firebox roars and a warm spot spreads from it over the temple (<see cref="WarmSpots"/>: the firebox
    /// mouth glows, its chimney smokes, every ember vent along the cliff burns, the pipe warms), and the bellows work as
    /// built in 9b. Saved as solved; already solved bellows (an old save) or a restored realm count it as done.
    /// Honest note: this is virtual light. It can't light the real room; the glow is on the ember and around it, and
    /// the ISDK hand visual keeps its own material (no tint on the hand).
    /// </summary>
    [DefaultExecutionOrder(-20)] // before the gaze walk, tile touch and microgestures read the held hand
    public class ForgeEmberPiece : HandPuzzle
    {
        [Header("Places (map props grid: column, row)")]
        public Vector2Int vent;
        public Vector2Int firebox;
        [Tooltip("The bellows it wakes (blocked until the ember is in).")]
        public PumpPiece bellows;
        public PropRecipe emberRecipe;

        [Header("Shape (tiles)")]
        public float emberSize = 0.34f;
        [Tooltip("The ember resting in its vent, above the cap.")]
        public float ventHeight = 0.08f;
        [Tooltip("The firebox mouth: height above the cap and how far out from the cliff toward the player.")]
        public float mouthHeight = 0.26f, mouthFront = 0.45f;

        [Header("Hands (metres at the placed island size)")]
        [Tooltip("A palm-up hand within this of the vent lifts it.")]
        public float pickRadius = 0.075f;
        [Tooltip("A pinch within this of the ember picks it.")]
        public float pinchRadius = 0.03f;
        [Tooltip("Over the firebox: this far round its mouth (across), and up to this high above it.")]
        public float fireboxRadius = 0.08f, fireboxAbove = 0.18f;
        [Range(0f, 1f)] public float pickUp = 0.6f;
        [Range(-1f, 1f)] public float keepUp = 0.2f;

        [Header("The forge answers")]
        public int warmSlot = 7;
        [Tooltip("Where the warmth settles (the temple plaza's middle) and how far it reaches (tiles).")]
        public Vector2 forgeCentre;
        public float forgeRadius = 9f, forgeEdge = 1.5f, roarSeconds = 2.5f;

        [Header("Lines")]
        public string doneLine = "cw.ember.done";
        [Range(0f, 1f)] public float volume = 0.7f;

        readonly EmberCarry _carry = new EmberCarry();
        readonly HandState[] _hands = new HandState[2];
        readonly EmberCarry.Hand[] _in = new EmberCarry.Hand[2];
        PropView _ember;
        BattleFx.Item _halo;
        AudioSource _audio;
        Vector3 _emberWorld, _fallFrom;
        float _anim, _sparkAt;
        int _gazed = -1; // 0 = the ember / vent, 1 = the firebox
        bool _roaring, _quietSolve;

        public EmberCarry Carry => _carry;
        /// <summary>Debug (bridge shots): while it waits (hovering), it sits here instead (a ghost hand's palm).</summary>
        [System.NonSerialized] public Vector3? debugAt;
        public override bool Busy => _carry.State == EmberState.OnPalm || _carry.State == EmberState.Flying;
        protected override Vector2 HomeTile => vent;

        public override Vector3? FocusPoint => Here && !IsSolved && (_carry.State == EmberState.InVent || _carry.State == EmberState.Hovering) ? _emberWorld : (Vector3?)null;
        public override float FocusRadius => 0.5f * TileWorld;

        float Scale => grabGuard != null ? grabGuard.ScaleFactor : 1f;
        Vector3 VentWorld => MapToWorld(new Vector3(vent.x, ventHeight, vent.y));
        Vector3 HoverWorld => MapToWorld(new Vector3(vent.x, ventHeight + 0.9f + 0.08f * Mathf.Sin(Time.time * 3f), vent.y));
        public Vector3 MouthWorld => MapToWorld(new Vector3(firebox.x, mouthHeight, firebox.y + mouthFront));

        protected override void Start()
        {
            _carry.pickUp = pickUp;
            _carry.keepUp = keepUp;
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
            if (island != null) _ember = new PropView("Forge Ember", island, emberRecipe);
            if (bellows == null) Debug.LogError($"[ForgeEmberPiece] {name}: no bellows to wake (Set Up MR_Main (10f Cinderwood hands)).", this);
            base.Start();
        }

        protected override void OnDestroy()
        {
            _ember?.Destroy();
            ReleaseHalo();
            WarmSpots.Clear(warmSlot);
            if (bellows != null) bellows.Blocked = false;
            base.OnDestroy();
        }

        protected override void ApplyState()
        {
            StopAllCoroutines();
            _roaring = false;
            bool lit = IsSolved || bellows != null && bellows.IsSolved;
            _carry.Restore(lit);
            if (bellows != null) bellows.Blocked = !lit;
            SetProgressQuiet(lit ? 1f : 0f);
            Debug.Log($"[ForgeEmberPiece] {id}: the firebox is {(lit ? "roaring" : "cold, the bellows wait")}.");
        }

        protected override void Update()
        {
            ReadHands(_hands);
            bool lit = IsSolved;
            if (!lit && (bellows != null && bellows.IsSolved || Here && RealmState.Restored >= 0.99f)) QuietSolve();
            if (bellows != null) bellows.Blocked = !IsSolved;
            if (!IsSolved && !_roaring) Hands();
            Draw();
            Warm();
            base.Update();
        }

        // ---- hands ----

        void Hands()
        {
            bool usable = Usable;
            var state = _carry.State;
            if (state == EmberState.InVent || state == EmberState.Hovering || state == EmberState.OnPalm)
            {
                for (int h = 0; h < 2; h++) _in[h] = usable || state == EmberState.OnPalm ? Measure(h) : default;
                var was = state;
                var now = _carry.Feed(Time.deltaTime, _in);
                if (now != was) Changed(was, now);
            }
            if (usable && (_carry.State == EmberState.InVent || _carry.State == EmberState.Hovering)) LookPinch();
            else _gazed = -1;
            if (_carry.State == EmberState.OnPalm)
            {
                // The carrying hand never walks the explorer or swipes.
                IslandHandle.MarkHandHeld(_carry.Holder == 0);
                TileTouch.Block(_carry.Holder == 0, 0.4f);
            }
        }

        EmberCarry.Hand Measure(int h)
        {
            var s = _hands[h];
            var x = new EmberCarry.Hand { tracked = s.tracked, busy = s.busy, pinching = s.pinch, pinchStarted = s.pinchStarted };
            if (!s.tracked) return x;
            var hand = HandRegistry.Get(h == 0 ? Handedness.Left : Handedness.Right);
            if (!HandLookup.TryGetJointPose(hand, HandJointId.HandPalm, out var palm))
            {
                x.tracked = false;
                return x;
            }
            Vector3 up = island.transform.up;
            // The palmar side is the palm joint's -Y for both hands (ISDK v207, as PalmHover).
            Vector3 normal = palm.rotation * Vector3.down;
            x.up = Vector3.Dot(normal, up);
            float scale = Scale;
            x.nearVent = (palm.position - VentWorld).magnitude <= pickRadius * scale;
            x.atEmber = s.pinch && (s.pinchPoint - _emberWorld).magnitude <= pinchRadius * scale + 0.25f * emberSize * TileWorld;
            Vector3 d = palm.position - MouthWorld;
            float above = Vector3.Dot(d, up);
            x.overFirebox = (d - up * above).magnitude <= fireboxRadius * scale && above >= -0.03f * scale && above <= fireboxAbove * scale;
            return x;
        }

        void Changed(EmberState was, EmberState now)
        {
            switch (now)
            {
                case EmberState.OnPalm:
                    MarkInteracted();
                    Debug.Log($"[ForgeEmberPiece] {id}: the ember hops onto the {(_carry.Holder == 0 ? "left" : "right")} palm{(_carry.Pinched ? " (pinched)" : "")}.");
                    if (_audio != null) _audio.PlayOneShot(PuzzleSounds.Catch, volume * 0.5f);
                    if (fx != null) fx.Burst(_emberWorld, 10, new Color(1f, 0.6f, 0.25f), TileWorld * 0.4f, 0.6f, TileWorld * 0.04f, island.transform.up * TileWorld * 0.8f);
                    break;
                case EmberState.Falling:
                    Debug.Log($"[ForgeEmberPiece] {id}: dropped, the ember falls back to its vent.");
                    _fallFrom = _emberWorld;
                    _anim = 0f;
                    break;
                case EmberState.In:
                    Debug.Log($"[ForgeEmberPiece] {id}: tipped into the firebox.");
                    StartCoroutine(Roar(_emberWorld));
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
            if (_carry.State == EmberState.InVent) pick.Consider(0, _emberWorld, r);
            else
            {
                pick.Consider(0, _emberWorld, r);
                pick.Consider(1, MouthWorld, r);
            }
            _gazed = pick.Best;
            if (_gazed < 0) return;
            FocusHighlight.Hover(_gazed == 0 ? _emberWorld : MouthWorld, FocusRadius);
            if (!GazePinch(_hands)) return;
            MarkInteracted();
            if (_carry.State == EmberState.InVent)
            {
                _carry.Hover();
                Debug.Log($"[ForgeEmberPiece] {id}: look + pinch: the ember rises and waits (look + pinch the firebox).");
            }
            else if (_gazed == 1)
            {
                _carry.Fly();
                Debug.Log($"[ForgeEmberPiece] {id}: look + pinch the firebox: the ember flies in.");
                StartCoroutine(FlyIn());
            }
        }

        IEnumerator FlyIn()
        {
            var from = _emberWorld;
            for (float t = 0f; t < 0.9f; t += Time.deltaTime)
            {
                float u = Mathf.SmoothStep(0f, 1f, t / 0.9f);
                _emberWorld = Vector3.Lerp(from, MouthWorld, u) + island.transform.up * TileWorld * 1.2f * Mathf.Sin(u * Mathf.PI);
                yield return null;
            }
            _carry.Arrive();
            Changed(EmberState.Flying, EmberState.In);
        }

        // ---- the forge wakes ----

        IEnumerator Roar(Vector3 at)
        {
            _roaring = true;
            LessonComplete();
            MarkSolved(1);
            if (bellows != null) bellows.Blocked = false;
            if (_audio != null)
            {
                _audio.PlayOneShot(PuzzleSounds.Whoosh, volume);
                _audio.PlayOneShot(PuzzleSounds.Crackle, volume * 0.8f);
            }
            if (explorer != null) explorer.Cheer();
            if (!string.IsNullOrEmpty(doneLine)) Dialogue.Say(doneLine);
            float tileSize = island.map.tileSize;
            Vector2 from = new Vector2(firebox.x, firebox.y);
            float next = 0f;
            for (float t = 0f; t < roarSeconds; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / roarSeconds);
                if (Here) WarmSpots.Set(warmSlot, Vector2.Lerp(from, forgeCentre, k), Mathf.Lerp(1f, forgeRadius, k), forgeEdge, tileSize);
                if (fx != null && Here && t >= next && t < 1.2f)
                {
                    // Flame tongues roll out of the mouth, sparks up the chimney.
                    next = t + 0.1f;
                    float tile = TileWorld;
                    fx.Burst(MouthWorld, 8, new Color(1f, 0.55f, 0.18f), tile * 0.6f, 0.6f, tile * 0.08f, island.transform.up * tile * 0.7f);
                    fx.Burst(MapToWorld(new Vector3(firebox.x, 2.2f, firebox.y)), 4, new Color(1f, 0.7f, 0.3f), tile * 0.3f, 0.9f, tile * 0.04f, island.transform.up * tile * 1.2f);
                }
                yield return null;
            }
            _roaring = false;
        }

        void QuietSolve()
        {
            if (_quietSolve) return;
            _quietSolve = true;
            Debug.Log($"[ForgeEmberPiece] {id}: {(bellows != null && bellows.IsSolved ? "the bellows already lit the forge" : "the realm is restored")}, so the firebox burns (no ember needed).");
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
                    _emberWorld = debugAt ?? Vector3.Lerp(_emberWorld, HoverWorld, Time.deltaTime * 4f);
                    break;
                case EmberState.OnPalm:
                    var hand = HandRegistry.Get(_carry.Holder == 0 ? Handedness.Left : Handedness.Right);
                    if (HandLookup.TryGetJointPose(hand, HandJointId.HandPalm, out var palm))
                    {
                        // Sits in the palm: a little out from it on the palmar side.
                        Vector3 normal = palm.rotation * Vector3.down;
                        _emberWorld = Vector3.Lerp(_emberWorld, palm.position + normal * 0.014f * Scale, Time.deltaTime * 20f);
                    }
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
            if (_ember == null) return;
            if (!show)
            {
                _ember.Hide();
                ReleaseHalo();
                return;
            }
            bool carried = s == EmberState.OnPalm || s == EmberState.Flying || s == EmberState.Hovering;
            // In its vent it glows faintly (a dim pulse); carried it burns bright.
            float pulse = carried ? 1f : 0.45f + 0.15f * Mathf.Sin(Time.time * 2.4f);
            _ember.restoredSelf = carried ? 1f : 0.55f;
            _ember.glowBoostB = new Vector4(carried ? 1.2f : 0.3f, 0f, 0f, 0f);
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
            float size = TileWorld * (carried ? 0.9f : 0.55f) * (0.92f + 0.08f * Mathf.Sin(Time.time * 9f));
            _halo.transform.localScale = Vector3.one * (size / Mathf.Max(1e-4f, fx.transform.lossyScale.x));
            _halo.SetAlpha(0.55f * strength);
        }

        void ReleaseHalo()
        {
            if (_halo == null || fx == null) return;
            fx.Release(_halo);
            _halo = null;
        }

        void Warm()
        {
            if (_roaring) return;
            if (IsSolved && Here) WarmSpots.Set(warmSlot, forgeCentre, forgeRadius, forgeEdge, island.map.tileSize);
            else WarmSpots.Clear(warmSlot);
        }

        // ---- bridge ----

        /// <summary>Debug (bridge): look + pinch on the vent: the ember rises and waits.</summary>
        public void DebugLookPinchVent()
        {
            if (!Here || IsSolved) return;
            MarkInteracted();
            _carry.Hover();
            Debug.Log($"[ForgeEmberPiece] {id}: debug look + pinch on the vent (state {_carry.State}).");
        }

        /// <summary>Debug (bridge): look + pinch the firebox while the ember waits.</summary>
        public void DebugLookPinchFirebox()
        {
            if (!Here || IsSolved || _carry.State != EmberState.Hovering) return;
            _carry.Fly();
            StartCoroutine(FlyIn());
        }

        public string Describe() => $"ember {_carry.State}{(_carry.Holder >= 0 ? $" (hand {_carry.Holder})" : "")}, bellows blocked {(bellows != null && bellows.Blocked)}, spot r {WarmSpots.Radius(warmSlot) / Mathf.Max(1e-4f, island.map.tileSize):0.#} tiles";
    }
}
