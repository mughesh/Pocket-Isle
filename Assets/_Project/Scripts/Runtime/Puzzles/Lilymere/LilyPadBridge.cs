using System.Collections;
using Diorama.Audio;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 11b A, Lilymere's lily channel: three lily pads drift slowly on the channel beside the crossing. A fingertip
    /// touching a pad pushes it along the water the way the finger moves (drag on the water, bounces off the banks and
    /// the other pads, <see cref="PadDrift"/>). A pad that comes within reach of an empty bridge slot (three faint gold
    /// rings across the crossing) glides in and clicks (a splash, a ring, a bob). All three: the pads lock into a path,
    /// ripples run along the whole channel and the crossing's tiles open (saved: stage = filled slots, solved = the
    /// path). Look + pinch a pad: it drifts into the nearest free slot by itself. A restored realm counts it as done.
    /// </summary>
    [DefaultExecutionOrder(-20)] // before tile touch reads the blocked fingertips
    public class LilyPadBridge : HandPuzzle
    {
        [Header("The channel (map: column, row)")]
        [Tooltip("Where the three pads start drifting.")]
        public Vector2[] padStart = new Vector2[3];
        [Tooltip("The bridge slots across the crossing, one per channel row.")]
        public Vector2[] slots = new Vector2[3];
        [Tooltip("The channel's water as a rect (pads stay inside it).")]
        public Rect channel;
        [Tooltip("The covers on the crossing (ChannelGap props, their anchors): hidden and opened once the bridge is done.")]
        public Vector2Int[] gapTiles = new Vector2Int[0];
        public PropRecipe padRecipe;
        [Tooltip("Pad size on the island (x the LilyPad prop); the slots' pads span the 2-wide crossing.")]
        public float padScale = 1.5f, slotScale = 2.1f;

        [Header("Push")]
        [Tooltip("A fingertip this close to a pad's middle (metres at the placed island size) pushes it.")]
        public float touchRadius = 0.035f;
        [Tooltip("Fingertip height band over the water (metres) that counts as touching the pad.")]
        public float touchHeight = 0.03f;
        [Tooltip("Share of the fingertip's speed the pad takes.")]
        public float pushShare = 0.8f;
        public float drag = 0.8f, bounce = 0.5f, snapRadius = 0.6f;

        [Header("Guidance")]
        public Material guideMaterial;
        public float ringSize = 1.6f, orbSize = 0.3f;

        [Header("Lines")]
        public string doneLine = "lm.pads.done";
        [Range(0f, 1f)] public float volume = 0.7f;

        PadDrift _drift;
        PropView[] _views;
        GuideRings _rings;
        AudioSource _audio;
        readonly HandState[] _hands = new HandState[2];
        readonly Vector3[] _lastTip = new Vector3[2];
        readonly bool[] _hadTip = new bool[2];
        readonly float[] _bob = new float[3];
        int _gazed = -1;
        bool _quietSolve, _finishing;

        public PadDrift Drift => _drift;
        public override bool Busy => false;
        protected override Vector2 HomeTile => slots.Length > 0 ? slots[slots.Length / 2] : Vector2.zero;
        float Scale => grabGuard != null ? grabGuard.ScaleFactor : 1f;

        public override Vector3? FocusPoint
        {
            get
            {
                if (!Here || IsSolved || _drift == null) return null;
                for (int i = 0; i < _drift.Count; i++)
                    if (!_drift.Locked(i)) return PadWorld(i);
                return null;
            }
        }

        public override float FocusRadius => 0.6f * TileWorld;

        protected override void Start()
        {
            _drift = new PadDrift(padStart, slots, channel) { drag = drag, bounce = bounce, snapRadius = snapRadius };
            _views = new PropView[padStart.Length];
            if (island != null)
                for (int i = 0; i < _views.Length; i++) _views[i] = new PropView($"Lily Pad {i}", island, padRecipe);
            _rings = new GuideRings("Pad Slot", island, guideMaterial, slots.Length, fx);
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
            base.Start();
        }

        protected override void OnDestroy()
        {
            if (_views != null)
                foreach (var v in _views) v?.Destroy();
            _rings?.Destroy();
            if (map != null)
                foreach (var t in gapTiles)
                {
                    map.SetPropHidden(t, false);
                    map.SetPropOpen(t, false);
                }
            base.OnDestroy();
        }

        protected override void ApplyState()
        {
            StopAllCoroutines();
            _finishing = false;
            int all = (1 << slots.Length) - 1;
            _drift.Restore(IsSolved ? all : PuzzleState.Stage(id), padStart);
            Open(IsSolved);
            SetProgressQuiet(Count(_drift.Mask) / (float)Mathf.Max(1, slots.Length));
            Debug.Log($"[LilyPadBridge] {id}: {Count(_drift.Mask)}/{slots.Length} pads in, crossing {(IsSolved ? "open" : "water")}.");
        }

        void Open(bool open)
        {
            foreach (var t in gapTiles)
            {
                map.SetPropHidden(t, open);
                map.SetPropOpen(t, open);
            }
        }

        protected override void Update()
        {
            ReadHands(_hands);
            if (!IsSolved && Here && RealmState.Restored >= 0.99f) QuietSolve();
            if (Usable && !IsSolved && !_finishing)
            {
                Pushes();
                LookPinch();
            }
            else _gazed = -1;
            if (!IsSolved && !_finishing)
            {
                int k = _drift.Tick(Time.deltaTime);
                if (k >= 0) Clicked(k);
                if (_drift.AllIn) StartCoroutine(Finish());
            }
            Draw();
            Guide();
            base.Update();
        }

        // ---- pushing ----

        void Pushes()
        {
            float r = touchRadius * Scale, band = touchHeight * Scale;
            float tileWorld = Mathf.Max(1e-4f, TileWorld);
            for (int h = 0; h < 2; h++)
            {
                var hand = HandRegistry.Get(h == 0 ? Handedness.Left : Handedness.Right);
                if (_hands[h].busy || !HandLookup.TryGetJoint(hand, HandJointId.HandIndexTip, out var tip))
                {
                    _hadTip[h] = false;
                    continue;
                }
                Vector3 vel = _hadTip[h] && Time.deltaTime > 1e-4f ? (tip - _lastTip[h]) / Time.deltaTime : Vector3.zero;
                _lastTip[h] = tip;
                _hadTip[h] = true;
                bool near = false;
                for (int i = 0; i < _drift.Count; i++)
                {
                    if (_drift.Locked(i)) continue;
                    var pad = PadWorld(i);
                    var d = tip - pad;
                    float up = Vector3.Dot(d, island.transform.up);
                    var flat = d - island.transform.up * up;
                    float reach = r + 0.5f * padScale * tileWorld * 0.7f;
                    if (flat.magnitude <= reach * 1.8f) near = true;
                    if (flat.magnitude > reach || up < -band || up > band * 2f) continue;
                    // The finger's planar motion, in tiles a second, pushes it.
                    var vm = MapVelocity(vel);
                    var push = new Vector2(vm.x, vm.z);
                    if (push.sqrMagnitude < 0.01f) continue;
                    MarkInteracted();
                    _drift.Push(i, push * pushShare * Time.deltaTime * 6f);
                }
                if (near) TileTouch.Block(h == 0, 0.4f);
            }
        }

        /// <summary>A world velocity (m/s) as map tiles a second (columns, up, rows).</summary>
        Vector3 MapVelocity(Vector3 v) => WorldToMap(island.transform.position + v) - WorldToMap(island.transform.position);

        void LookPinch()
        {
            if (!HeadGaze.TryGetRay(out var ray))
            {
                _gazed = -1;
                return;
            }
            var pick = new HeadGaze.ConePick(ray, _gazed);
            for (int i = 0; i < _drift.Count; i++)
                if (!_drift.Locked(i)) pick.Consider(i, PadWorld(i), Mathf.Max(HeadGaze.MinRadius * island.transform.lossyScale.x, 0.6f * TileWorld));
            _gazed = pick.Best;
            if (_gazed < 0) return;
            FocusHighlight.Hover(PadWorld(_gazed), FocusRadius);
            if (!GazePinch(_hands)) return;
            MarkInteracted();
            _drift.Steer(_gazed);
            Debug.Log($"[LilyPadBridge] {id}: look + pinch on pad {_gazed}: it drifts to slot {_drift.NearestFree(_gazed)}.");
        }

        void Clicked(int pad)
        {
            LessonComplete();
            PuzzleState.SetStage(id, _drift.Mask);
            SetProgress(Count(_drift.Mask) / (float)Mathf.Max(1, slots.Length));
            _bob[pad] = 1f;
            Debug.Log($"[LilyPadBridge] {id}: pad {pad} clicked into slot {_drift.slotOf[pad]}, {Count(_drift.Mask)}/{slots.Length}.");
            if (!Here) return;
            var at = PadWorld(pad);
            GameAudio.At(AudioIds.PondSplash, at);
            if (fx != null) fx.Burst(at, 14, new Color(0.75f, 0.95f, 1f, 0.9f), TileWorld * 0.5f, 0.5f, TileWorld * 0.04f, island.transform.up * TileWorld * 0.5f);
            StartCoroutine(Ring(new Vector2(_drift.pos[pad].x, _drift.pos[pad].y), 0.6f, 1.6f));
        }

        IEnumerator Finish()
        {
            _finishing = true;
            Debug.Log($"[LilyPadBridge] {id}: all pads in: the pads lock into a path.");
            yield return new WaitForSeconds(0.4f);
            // Ripples run along the whole channel, out from the crossing.
            if (Here)
            {
                GameAudio.At(AudioIds.PondSplash, PadWorld(1));
                for (int k = 0; k < 8; k++)
                {
                    float off = k * 2.2f;
                    StartCoroutine(Ring(new Vector2(slots[1].x - off, slots[1].y), 0.7f, 1.4f));
                    StartCoroutine(Ring(new Vector2(slots[1].x + off, slots[1].y), 0.7f, 1.4f));
                    yield return new WaitForSeconds(0.12f);
                }
            }
            Open(true);
            // Settle every pad exactly on its slot (the last one may still be gliding in).
            int mask = _drift.Mask;
            _drift.Restore(mask, padStart);
            if (!string.IsNullOrEmpty(doneLine)) Dialogue.Say(doneLine);
            if (explorer != null) explorer.Cheer();
            _finishing = false;
            MarkSolved(mask);
        }

        /// <summary>A ripple ring spreading on the water at a map point.</summary>
        IEnumerator Ring(Vector2 tile, float seconds, float tiles)
        {
            if (fx == null) yield break;
            var ring = fx.Get(BattleFx.Shape.Ring);
            if (ring == null) yield break;
            var c = new Color(0.85f, 1f, 1f);
            ring.Set(c, c, 1f);
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float u = t / seconds;
                ring.transform.localPosition = island.TileToLocal(tile, island.Content.capHeight - 0.003f);
                ring.transform.localRotation = Quaternion.identity;
                ring.transform.localScale = Vector3.one * (island.map.tileSize * island.Zoom * Mathf.Lerp(0.2f, tiles, u));
                ring.SetAlpha(Here ? (1f - u) * 0.8f : 0f);
                yield return null;
            }
            fx.Release(ring);
        }

        void QuietSolve()
        {
            if (_quietSolve) return;
            _quietSolve = true;
            Debug.Log($"[LilyPadBridge] {id}: the realm is restored, so the lily pads lie in the crossing.");
            _drift.Restore((1 << slots.Length) - 1, padStart);
            Open(true);
            MarkSolved(_drift.Mask);
        }

        // ---- drawing ----

        void Draw()
        {
            if (_views == null) return;
            for (int i = 0; i < _views.Length; i++)
            {
                var v = _views[i];
                if (v == null) continue;
                _bob[i] = Mathf.MoveTowards(_bob[i], 0f, Time.deltaTime * 2f);
                bool locked = _drift.Locked(i);
                // On the crossing (path tiles, higher than the water) a locked pad sits on top.
                float bob = (locked ? 0.1f : 0f) + 0.04f * Mathf.Sin(Time.time * 1.3f + i * 2f) + 0.08f * _bob[i] * Mathf.Sin(_bob[i] * 18f);
                float yaw = locked ? 0f : 40f * i + Time.time * (6f + 3f * i);
                v.Place(_drift.pos[i], bob, Quaternion.Euler(0f, yaw, 0f), Here, locked ? slotScale : padScale);
            }
        }

        /// <summary>A gold ring over every empty slot while the explorer is near.</summary>
        void Guide()
        {
            if (_rings == null) return;
            bool on = InView && !IsSolved && explorer != null &&
                      Mathf.Max(Mathf.Abs(explorer.Tile.x - HomeTile.x), Mathf.Abs(explorer.Tile.y - HomeTile.y)) <= reach + 3;
            int mask = _drift != null ? _drift.Mask : 0;
            for (int s = 0; s < slots.Length; s++)
            {
                float pulse = 0.8f + 0.2f * Mathf.Sin(Time.time * 3f + s * 0.9f);
                _rings.Draw(s, on && (mask & (1 << s)) == 0, new Vector3(slots[s].x, 0.05f, slots[s].y), ringSize, 1.4f * pulse, orbSize * pulse);
            }
        }

        Vector3 PadWorld(int i) => MapToWorld(new Vector3(_drift.pos[i].x, 0f, _drift.pos[i].y));

        static int Count(int mask)
        {
            int n = 0;
            for (; mask != 0; mask &= mask - 1) n++;
            return n;
        }

        // ---- bridge ----

        /// <summary>Debug (bridge): push pad <paramref name="i"/> at this map velocity (tiles / s).</summary>
        public void DebugPush(int i, Vector2 velocity)
        {
            if (!Here || IsSolved) return;
            MarkInteracted();
            _drift.Push(i, velocity);
        }

        /// <summary>Debug (bridge): look + pinch on pad <paramref name="i"/>.</summary>
        public void DebugSteer(int i)
        {
            if (!Here || IsSolved) return;
            MarkInteracted();
            _drift.Steer(i);
        }

        public string Describe()
        {
            if (_drift == null) return "no drift";
            var sb = new System.Text.StringBuilder($"pads in {Count(_drift.Mask)}/{slots.Length} (mask {_drift.Mask})");
            for (int i = 0; i < _drift.Count; i++) sb.Append($", pad {i} c{_drift.pos[i].x:0.0} r{_drift.pos[i].y:0.0}{(_drift.Locked(i) ? $" in slot {_drift.slotOf[i]}" : "")}");
            return sb.ToString();
        }
    }
}
