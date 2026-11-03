using System.Collections;
using Diorama.Audio;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 11b B, Lilymere's lotus garden: a hand held palm down over the dry bed, 5-25 cm above the island, for 0.6 s
    /// forms a small grey-white cloud right under the palm (<see cref="RainCloud"/>); it rains (streaks, splash dots) while
    /// the palm stays there, and a finger wiggle rains harder. The cloud follows the hand. Five closed lotus buds sit on
    /// the garden crossing; each fills under the rain (<see cref="BudGarden"/>, ~2 s, ~1 s hard) and blooms into a big
    /// open lotus stepping stone with a pop of petals and a splash. All five: a lotus path across the garden, its tiles
    /// open (saved: stage = bloomed buds, solved = the path). The rain darkens the cracked sand under it for a while.
    /// Look + pinch-hold a bud: a tiny cloud rains on it by itself. A restored realm counts it as done.
    /// Step 14a, stream mode (<see cref="streamTiles"/> set: Lilymere's arrival stream): the "buds" are lily pads stuck
    /// in the stream's empty, sunken mud bed (Nov 3: scattered along it, no gold rings on them). Rain anywhere over the
    /// stream swells it (its mud tiles turn to water over ~<see cref="swellSeconds"/> of rain, spreading out from under
    /// the cloud); a pad lifts and floats once the water reaches its tile, and once all float they drift into a pad bridge
    /// across the trail (<see cref="bridgeSlots"/>; the crossing tiles become walkable). Old saves with
    /// <see cref="legacySolvedId"/> solved count it as solved.
    /// </summary>
    [DefaultExecutionOrder(-20)]
    public class RainGarden : HandPuzzle
    {
        [Header("The garden (map: column, row)")]
        public Vector2[] buds = new Vector2[5];
        [Tooltip("The garden's middle and radii (tiles): a palm over this counts.")]
        public Vector2 gardenCenter;
        public Vector2 gardenRadius = new Vector2(6f, 4f);
        [Tooltip("The covers on the crossing (BedGap props): hidden and opened once every bud bloomed.")]
        public Vector2Int[] gapTiles = new Vector2Int[0];
        public PropRecipe budRecipe, stoneRecipe, cloudRecipe;
        public float budScale = 1.2f, stoneScale = 1.6f;

        [Header("Rain hand (RainCloud)")]
        [Range(0f, 90f)] public float maxAngle = 40f;
        [Tooltip("Palm height above the island (metres at the placed island size).")]
        public float minHeight = 0.05f, maxHeight = 0.25f;
        public float formSeconds = 0.6f;
        [Tooltip("Summed fingertip speed (m/s) that counts as a wiggle.")]
        public float wiggleSpeed = 0.6f;
        [Tooltip("The cloud's rain reaches buds within this many tiles of the point under the palm.")]
        public float rainRadius = 1.3f;
        [Tooltip("Cloud size on the island (x the CloudPuff prop) and how far under the palm it floats (metres).")]
        public float cloudScale = 0.55f, cloudBelow = 0.025f;
        [Tooltip("Step 11d: the cloud's width in the room (metres, about a palm). 0 = Cloud Scale as before.")]
        public float cloudMetres = 0.11f;

        [Header("Garden view (step 11d)")]
        [Tooltip("With the explorer this close (tiles) to the buds' middle, the window glides over to show all five buds.")]
        public float settleReach = 7f;

        [Header("Buds (BudGarden)")]
        public float softSeconds = 2f, hardSeconds = 1f;
        [Tooltip("Look + pinch-hold rains on the looked-at bud this fast (seconds to bloom).")]
        public float lookSeconds = 2.5f;

        [Header("Step 14a: the arrival stream (empty = the lotus garden)")]
        [Tooltip("The stream's dry bed tiles (column, row): they swell to water under the rain.")]
        public Vector2Int[] streamTiles = new Vector2Int[0];
        [Tooltip("The trail's tiles over the stream: water under the pad bridge (walkable) once every pad floats.")]
        public Vector2Int[] crossingTiles = new Vector2Int[0];
        [Tooltip("Where the floating pads drift to (map tiles), one per pad: the bridge across the trail.")]
        public Vector2[] bridgeSlots = new Vector2[0];
        [Tooltip("Seconds of rain over the stream until every tile runs with water.")]
        public float swellSeconds = 2f;
        public float bridgeSeconds = 1.6f;
        [Tooltip("Old saves: this puzzle solved counts as this one solved (the stream replaced the channel's pads).")]
        public string legacySolvedId;

        [Header("Lines")]
        public string doneLine = "lm.rain.done";
        [Range(0f, 1f)] public float volume = 0.7f;

        RainCloud _cloud;
        BudGarden _garden;
        PropView[] _budViews, _stoneViews;
        PropView _cloudView;
        AudioSource _audio;
        readonly HandState[] _hands = new HandState[2];
        readonly Vector3[][] _lastTips = { new Vector3[4], new Vector3[4] };
        readonly bool[] _hadTips = new bool[2];
        readonly float[] _pop = new float[5];
        Vector3 _cloudMap;
        int _gazed = -1;
        bool _lookHold, _quietSolve, _finishing;
        float _dropAt, _dripAt;
        LoopAnchor _rain;
        // Debug (bridge): a synthetic palm-down hand over this map point for a while.
        Vector2? _debugPalm;
        float _debugUntil, _debugWiggle;

        bool Stream => streamTiles != null && streamTiles.Length > 0;
        float _swell, _bridge;
        int _wetCount;
        System.Collections.Generic.List<Vector2Int> _swellOrder;

        /// <summary>Step 14a: how far the stream has swelled (0..1) and the pads have drifted into the bridge (0..1).</summary>
        public float Swell => _swell;
        public float BridgeProgress => _bridge;

        public RainCloud Cloud => _cloud;
        public BudGarden Garden => _garden;
        public override bool Busy => _cloud != null && _cloud.Holder >= 0 || _lookHold;
        protected override Vector2 HomeTile => gardenCenter;
        float Scale => grabGuard != null ? grabGuard.ScaleFactor : 1f;
        /// <summary>One tile in island-local metres (map units), and the water's top under the cap in tiles.</summary>
        float TileLocal => island != null && island.map != null ? Mathf.Max(1e-4f, island.map.tileSize) : 0.05f;
        float WaterTiles => -IslandBuilder.WaterDrop / TileLocal;

        public override Vector3? FocusPoint
        {
            get
            {
                if (!Here || IsSolved || _garden == null) return null;
                // Nov 3: the stream itself is the target (rain on it), not the pads.
                if (Stream) return MapToWorld(new Vector3(gardenCenter.x, 1.6f, gardenCenter.y));
                for (int i = 0; i < buds.Length; i++)
                    if (!_garden.Bloomed(i)) return MapToWorld(new Vector3(buds[i].x, 1.6f, buds[i].y));
                return null;
            }
        }

        public override float FocusRadius => 1.2f * TileWorld;

        // ---- step 11d: the objective (gold on every closed bud, "Lotus n/5") ----

        public override bool CountObjective(out int done, out int total)
        {
            total = buds.Length;
            done = _garden != null ? Count(_garden.Mask) : 0;
            return true;
        }

        protected override int MarkCount => buds.Length;

        protected override bool Mark(int i, out Vector3 world, out float tiles)
        {
            world = default;
            tiles = 1.3f;
            // Nov 3: no rings on the stream's pads (they read as "touch the pads"; the rain goes on the stream).
            if (Stream || _garden == null || i >= buds.Length || _garden.Bloomed(i) || _finishing) return false;
            world = MapToWorld(new Vector3(buds[i].x, 0.05f, buds[i].y));
            return true;
        }

        /// <summary>The cloud's size in CloudPuff units now (step 11d: about a palm across in the room).</summary>
        float CloudScaleNow => cloudMetres > 0f ? cloudMetres / Mathf.Max(1e-4f, TileWorld) : cloudScale;

        protected override void Start()
        {
            _cloud = new RainCloud { maxAngle = maxAngle, formSeconds = formSeconds, wiggleSpeed = wiggleSpeed };
            _garden = new BudGarden(buds.Length) { softSeconds = softSeconds, hardSeconds = hardSeconds };
            _budViews = new PropView[buds.Length];
            _stoneViews = new PropView[buds.Length];
            if (island != null)
            {
                for (int i = 0; i < buds.Length; i++)
                {
                    _budViews[i] = new PropView($"Garden Bud {i}", island, budRecipe) { restoredSelf = 0f };
                    _stoneViews[i] = new PropView($"Lotus Stone {i}", island, stoneRecipe);
                }
                _cloudView = new PropView("Rain Cloud", island, cloudRecipe);
            }
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
            base.Start();
        }

        protected override void OnDestroy()
        {
            if (_budViews != null)
                foreach (var v in _budViews) v?.Destroy();
            if (_stoneViews != null)
                foreach (var v in _stoneViews) v?.Destroy();
            _cloudView?.Destroy();
            _stand?.Destroy();
            if (map != null)
            {
                foreach (var t in gapTiles)
                {
                    map.SetPropHidden(t, false);
                    map.SetPropOpen(t, false);
                }
                foreach (var t in streamTiles) map.SetWet(t, false);
                foreach (var t in crossingTiles)
                {
                    map.SetWet(t, false);
                    map.SetBridged(t, false);
                }
            }
            base.OnDestroy();
        }

        protected override void ApplyState()
        {
            StopAllCoroutines();
            _finishing = false;
            int all = (1 << buds.Length) - 1;
            _garden.Restore(IsSolved ? all : PuzzleState.Stage(id));
            _bridge = IsSolved ? 1f : 0f;
            // A stream with a pad afloat already ran with water.
            SetSwell(IsSolved || _garden.Mask != 0 ? 1f : 0f, null);
            Open(IsSolved);
            SetProgressQuiet(Count(_garden.Mask) / (float)Mathf.Max(1, buds.Length));
            Debug.Log($"[RainGarden] {id}: {Count(_garden.Mask)}/{buds.Length} lotus bloomed, crossing {(IsSolved ? "open" : "dry")}.");
        }

        void Open(bool open)
        {
            foreach (var t in gapTiles)
            {
                // Step 11d: the crossing is the lotus stones only: the bed keeps showing under them. Step 14a: on the
                // stream the cracked cover goes once it runs with water.
                map.SetPropHidden(t, Stream && open);
                map.SetPropOpen(t, open);
            }
            if (!Stream) return;
            foreach (var t in crossingTiles)
            {
                map.SetWet(t, open);
                map.SetBridged(t, open);
            }
        }

        /// <summary>Step 14a: the stream's tiles turn to water up to <paramref name="swell"/>, nearest the rain first.</summary>
        void SetSwell(float swell, Vector2? from)
        {
            if (!Stream || map == null) return;
            _swell = Mathf.Clamp01(swell);
            // The first rain decides where it spreads from.
            if (from.HasValue && _wetCount == 0) _swellOrder = null;
            if (_swellOrder == null)
            {
                _swellOrder = new System.Collections.Generic.List<Vector2Int>(streamTiles);
                var at = from ?? gardenCenter;
                _swellOrder.Sort((a, b) => ((Vector2)a - at).sqrMagnitude.CompareTo(((Vector2)b - at).sqrMagnitude));
            }
            int want = Mathf.CeilToInt(_swell * _swellOrder.Count - 1e-4f);
            // A few tiles at a time (each change rebuilds the island).
            int step = Mathf.Max(1, _swellOrder.Count / 6);
            if (want < _swellOrder.Count && want - _wetCount < step && want > _wetCount) return;
            for (int i = 0; i < _swellOrder.Count; i++) map.SetWet(_swellOrder[i], i < want);
            if (want > _wetCount && want > 0 && Here && fx != null)
                for (int i = _wetCount; i < want; i += 2)
                {
                    var p = MapToWorld(new Vector3(_swellOrder[i].x, 0.05f, _swellOrder[i].y));
                    fx.Burst(p, 2, new Color(0.65f, 0.9f, 0.97f), TileWorld * 0.25f, 0.45f, TileWorld * 0.04f, island.transform.up * TileWorld * 0.3f);
                }
            if (want != _wetCount) Debug.Log($"[RainGarden] {id}: the stream swells ({want}/{_swellOrder.Count} tiles run with water).");
            _wetCount = want;
        }

        protected override void Update()
        {
            ReadHands(_hands);
            if (!IsSolved && Here && (RealmState.Restored >= 0.99f || !string.IsNullOrEmpty(legacySolvedId) && PuzzleState.IsSolved(legacySolvedId))) QuietSolve();
            bool live = Usable && !IsSolved && !_finishing;
            int hand = -1;
            float wiggle = 0f;
            Vector3 under = _cloudMap;
            if (live) hand = RainHand(out wiggle, out under);
            if (hand < 0 && live && _debugPalm.HasValue && Time.time < _debugUntil)
            {
                hand = 2;
                wiggle = _debugWiggle;
                under = new Vector3(_debugPalm.Value.x, _cloudMap.y, _debugPalm.Value.y);
            }
            else if (hand < 0) under = _cloudMap;
            if (hand >= 0)
            {
                if (_cloud.Holder < 0) Debug.Log($"[RainGarden] {id}: palm down over the garden ({(hand == 0 ? "left" : hand == 1 ? "right" : "debug")}), a cloud forms.");
                _cloudMap = under;
                MarkInteracted();
            }
            bool wasRaining = _cloud.Raining;
            _cloud.Feed(Time.deltaTime, hand, wiggle);
            if (_cloud.Raining && !wasRaining) LessonComplete();
            if (live) LookPinch();
            else
            {
                _gazed = -1;
                _lookHold = false;
            }
            if (_cloud.Raining && live && !Stream) RainOnBuds(new Vector2(_cloudMap.x, _cloudMap.z), rainRadius, _cloud.Hard, Time.deltaTime);
            if (_cloud.Raining && live && Stream && _swell < 1f)
                SetSwell(_swell + Time.deltaTime * (_cloud.Hard ? 1.6f : 1f) / Mathf.Max(0.1f, swellSeconds), new Vector2(_cloudMap.x, _cloudMap.z));
            if (_lookHold && _gazed >= 0)
            {
                if (Stream && _swell < 1f) SetSwell(_swell + Time.deltaTime / Mathf.Max(0.1f, lookSeconds), buds[_gazed]);
                else if (!Stream) RainOnBud(_gazed, Time.deltaTime * softSeconds / Mathf.Max(0.1f, lookSeconds), false);
            }
            // Nov 3: a stream pad lifts once the water reaches its tile (~0.7 s).
            if (live && Stream)
                for (int i = 0; i < buds.Length; i++)
                    if (!_garden.Bloomed(i) && map.IsWet(PadTile(i))) RainOnBud(i, Time.deltaTime * 3f, false);
            if (!IsSolved && !_finishing && _garden.AllBloomed) StartCoroutine(Finish());
            Settle();
            Draw();
            base.Update();
        }

        /// <summary>The hand held palm down over the garden in the height band (-1 none), its finger wiggle and the point under it.</summary>
        int RainHand(out float wiggle, out Vector3 under)
        {
            wiggle = 0f;
            under = default;
            int best = -1;
            float s = Scale;
            for (int h = 0; h < 2; h++)
            {
                var hand = HandRegistry.Get(h == 0 ? Handedness.Left : Handedness.Right);
                if (!_hands[h].tracked || _hands[h].busy || _hands[h].Holding || !HandLookup.TryGetJointPose(hand, HandJointId.HandPalm, out var palm))
                {
                    _hadTips[h] = false;
                    continue;
                }
                // The palmar side is the palm joint's -Y for both hands (ISDK v207, as PalmHover).
                Vector3 normal = palm.rotation * Vector3.down;
                float speed = FingerSpeed(h, hand);
                if (!RainCloud.PalmDown(normal, island.transform.up, maxAngle)) continue;
                var m = WorldToMap(palm.position);
                float height = m.y * TileWorld;
                if (height < minHeight * s || height > maxHeight * s) continue;
                float e = Mathf.Sqrt(Sq((m.x - gardenCenter.x) / gardenRadius.x) + Sq((m.z - gardenCenter.y) / gardenRadius.y));
                if (e > 1f) continue;
                TileTouch.Block(h == 0, 0.4f);
                if (best >= 0 && h != _cloud.Holder) continue;
                best = h;
                wiggle = speed;
                under = m;
            }
            return best;
        }

        static float Sq(float v) => v * v;

        /// <summary>Summed speed of the four fingertips (m/s), for the wiggle.</summary>
        float FingerSpeed(int h, IHand hand)
        {
            var ids = new[] { HandJointId.HandIndexTip, HandJointId.HandMiddleTip, HandJointId.HandRingTip, HandJointId.HandPinkyTip };
            float sum = 0f;
            bool all = true;
            for (int k = 0; k < 4; k++)
            {
                if (!HandLookup.TryGetJoint(hand, ids[k], out var tip))
                {
                    all = false;
                    continue;
                }
                if (_hadTips[h] && Time.deltaTime > 1e-4f) sum += (tip - _lastTips[h][k]).magnitude / Time.deltaTime;
                _lastTips[h][k] = tip;
            }
            _hadTips[h] = all;
            return sum;
        }

        void LookPinch()
        {
            _lookHold = false;
            if (!HeadGaze.TryGetRay(out var ray))
            {
                _gazed = -1;
                return;
            }
            var pick = new HeadGaze.ConePick(ray, _gazed);
            for (int i = 0; i < buds.Length; i++)
                if (!_garden.Bloomed(i)) pick.Consider(i, BudWorld(i), Mathf.Max(HeadGaze.MinRadius * island.transform.lossyScale.x, 0.6f * TileWorld));
            _gazed = pick.Best;
            if (_gazed < 0) return;
            FocusHighlight.Hover(BudWorld(_gazed), 0.5f * TileWorld);
            foreach (var h in _hands)
                if (h.pinch && !h.busy) _lookHold = true;
            if (_lookHold) MarkInteracted();
        }

        void RainOnBuds(Vector2 at, float radius, bool hard, float dt)
        {
            for (int i = 0; i < buds.Length; i++)
                if ((buds[i] - at).magnitude <= radius) RainOnBud(i, dt, hard);
        }

        void RainOnBud(int i, float dt, bool hard)
        {
            if (!_garden.Rain(i, dt, hard)) return;
            LessonComplete();
            PuzzleState.SetStage(id, _garden.Mask);
            SetProgress(Count(_garden.Mask) / (float)Mathf.Max(1, buds.Length));
            _pop[i] = 1f;
            Debug.Log($"[RainGarden] {id}: bud {i} bloomed into a lotus stone, {Count(_garden.Mask)}/{buds.Length}.");
            if (!Here) return;
            var p = BudWorld(i);
            GameAudio.Play(AudioIds.LotusBloom, p, _audio, PuzzleSounds.Catch, volume * 0.7f);
            GameAudio.At(AudioIds.PondSplash, p).SetVolume(0.5f);
            if (fx == null) return;
            fx.Burst(p, 18, new Color(0.98f, 0.68f, 0.76f), TileWorld * 0.6f, 0.8f, TileWorld * 0.05f, island.transform.up * TileWorld * 0.6f);
            fx.Burst(p, 10, new Color(0.7f, 0.92f, 1f, 0.9f), TileWorld * 0.5f, 0.5f, TileWorld * 0.04f, island.transform.up * TileWorld * 0.3f);
        }

        IEnumerator Finish()
        {
            _finishing = true;
            Debug.Log($"[RainGarden] {id}: every bud bloomed: a lotus path across the garden.");
            yield return new WaitForSeconds(0.6f);
            if (Stream)
            {
                // Step 14a: the stream runs full; the floating pads drift into a bridge across the trail.
                SetSwell(1f, null);
                Debug.Log($"[RainGarden] {id}: every pad floats: they drift into a bridge across the trail.");
                GameAudio.At(AudioIds.PondSplash, MapToWorld(new Vector3(gardenCenter.x, 0.1f, gardenCenter.y)));
                for (float t = 0f; t < bridgeSeconds; t += Time.deltaTime)
                {
                    _bridge = t / bridgeSeconds;
                    yield return null;
                }
                _bridge = 1f;
            }
            Open(true);
            if (!string.IsNullOrEmpty(doneLine)) Dialogue.Say(doneLine);
            if (explorer != null) explorer.Cheer();
            _finishing = false;
            MarkSolved(_garden.Mask);
        }

        void QuietSolve()
        {
            if (_quietSolve) return;
            _quietSolve = true;
            Debug.Log($"[RainGarden] {id}: {(RealmState.Restored >= 0.99f ? "the realm is restored" : $"{legacySolvedId} was solved (an old save)")}, so the {(Stream ? "pad bridge" : "lotus stones")} {(Stream ? "is" : "are")} open.");
            _garden.Restore((1 << buds.Length) - 1);
            _bridge = 1f;
            SetSwell(1f, null);
            Open(true);
            MarkSolved(_garden.Mask);
        }

        // ---- drawing ----

        void Draw()
        {
            if (_budViews == null) return;
            bool here = Here;
            for (int i = 0; i < buds.Length; i++)
            {
                bool bloomed = _garden.Bloomed(i);
                _pop[i] = Mathf.MoveTowards(_pop[i], 0f, Time.deltaTime * 2.5f);
                // A wetting bud swells a little; a bloomed one pops up big.
                float wet = _garden.fill[i];
                float sway = wet > 0f && !bloomed ? 4f * Mathf.Sin(Time.time * 9f + i) * wet : 0f;
                float pop = 1f + 0.35f * Mathf.Sin(_pop[i] * Mathf.PI);
                if (Stream)
                {
                    // Step 14a / Nov 3: a pad lying on the sunken mud floor (tilted a little), lifting as the water reaches it;
                    // afloat it bobs on the water, then drifts into its bridge slot. Heights put the mesh's underside on the
                    // surface (the pad hangs ~0.1 tile below its pivot: placed by the pivot it sat in the mud / under water).
                    float mud = -IslandBuilder.MudDrop / TileLocal, water = WaterTiles;
                    float budBottom = _budViews[i] != null ? _budViews[i].Bottom * budScale : 0f;
                    float stoneBottom = _stoneViews[i] != null ? _stoneViews[i].Bottom * stoneScale * pop : 0f;
                    _budViews[i]?.Place(buds[i], Mathf.Lerp(mud + 0.005f, water + 0.01f, wet) - budBottom,
                        Quaternion.Euler(6f * (1f - wet) + sway, 40f * i, 4f * (1f - wet)), here && !bloomed, budScale);
                    var at = i < bridgeSlots.Length ? Vector2.Lerp(buds[i], bridgeSlots[i], Mathf.SmoothStep(0f, 1f, _bridge)) : buds[i];
                    float bob = 0.008f * (1f + Mathf.Sin(Time.time * 1.8f + i * 1.3f));
                    _stoneViews[i]?.Place(at, water + 0.01f + bob - stoneBottom, Quaternion.Euler(0f, 30f * i + 25f * _bridge, 0f), here && bloomed, stoneScale * pop);
                    continue;
                }
                _budViews[i]?.Place(buds[i], 0f, Quaternion.Euler(sway, 40f * i, 0f), here && !bloomed, budScale * (1f + 0.25f * wet));
                _stoneViews[i]?.Place(buds[i], 0.02f, Quaternion.Euler(0f, 30f * i, 0f), here && bloomed, stoneScale * pop);
            }
            DrawCloud(here);
        }

        void DrawCloud(bool here)
        {
            if (_cloudView == null) return;
            float size = _cloud != null ? _cloud.Size : 0f;
            bool show = here && size > 0.01f;
            float scale = CloudScaleNow;
            float belowTiles = cloudBelow * Scale / Mathf.Max(1e-4f, TileWorld);
            float h = Mathf.Max(0.6f + 0.25f * scale, _cloudMap.y - belowTiles);
            _cloudView.Place(new Vector2(_cloudMap.x, _cloudMap.z), h - 0.5f * scale, Quaternion.Euler(0f, Time.time * 10f, 0f), show,
                scale * Mathf.SmoothStep(0.2f, 1f, size));
            RainSound(show && _cloud.Raining, h - 0.5f * scale);
            if (!show || !_cloud.Raining || fx == null) return;
            // Rain: streaks falling from the whole cloud, splash dots on the bed; more when hard (step 11d: more, bigger).
            float rate = _cloud.Hard ? 0.006f : 0.015f;
            float tile = TileWorld;
            float spread = Mathf.Max(1f, scale * 0.6f);
            while (Time.time >= _dropAt)
            {
                _dropAt = Mathf.Max(_dropAt + rate, Time.time - 0.1f);
                var from = MapToWorld(new Vector3(_cloudMap.x + Random.Range(-0.6f, 0.6f) * spread, h - 0.5f * scale, _cloudMap.z + Random.Range(-0.45f, 0.45f) * spread));
                fx.Burst(from, 1, new Color(0.75f, 0.9f, 1f, 0.95f), tile * 0.03f, 0.4f, tile * 0.05f, -island.transform.up * tile * h * 2.6f);
                if (Random.value < 0.6f) continue;
                var ground = MapToWorld(new Vector3(_cloudMap.x + Random.Range(-0.7f, 0.7f) * spread, 0.02f, _cloudMap.z + Random.Range(-0.7f, 0.7f) * spread));
                fx.Burst(ground, 1, new Color(0.65f, 0.85f, 0.95f, 0.85f), tile * 0.14f, 0.3f, tile * 0.04f, island.transform.up * tile * 0.15f);
            }
            if (!_rain.Playing && Time.time >= _dripAt)
            {
                // Only while the rain cues have no clips: a drip now and then.
                _dripAt = Time.time + (_cloud.Hard ? 0.6f : 1.1f);
                GameAudio.At(AudioIds.SwDrip, MapToWorld(new Vector3(_cloudMap.x, 0f, _cloudMap.z)));
            }
        }

        /// <summary>rain.soft / rain.hard under the palm cloud, following it; off when it stops raining or leaves the window.</summary>
        void RainSound(bool raining, float height)
        {
            _rain ??= new LoopAnchor(transform, "Rain Sound");
            if (!raining)
            {
                _rain.Off(0.6f);
                return;
            }
            _rain.Set(_cloud.Hard ? AudioIds.RainHard : AudioIds.RainSoft, MapToWorld(new Vector3(_cloudMap.x, height * 0.5f, _cloudMap.z)), 0.4f);
        }

        Vector3 BudWorld(int i) => MapToWorld(new Vector3(buds[i].x, 0.4f, buds[i].y));

        /// <summary>The map tile a stream pad lies on.</summary>
        Vector2Int PadTile(int i) => new Vector2Int(Mathf.RoundToInt(buds[i].x), Mathf.RoundToInt(buds[i].y));

        /// <summary>The buds' middle (map tiles).</summary>
        public Vector2 BudsCenter
        {
            get
            {
                Vector2 sum = Vector2.zero;
                foreach (var b in buds) sum += b;
                return buds.Length > 0 ? sum / buds.Length : gardenCenter;
            }
        }

        bool _settled;
        Vector2 _focusAt;
        GuideRings _stand;

        /// <summary>Step 12b: the garden keeps its own settle (with the stand-here tile); the shared focus lock skips it.</summary>
        public override bool FocusLockable => false;

        /// <summary>The window holds the garden while the explorer is at it (step 11d).</summary>
        public bool Settled => _settled;

        /// <summary>The gold "stand here" tile shows (the window can't hold every bud and the explorer from where they are).</summary>
        public bool StandHereShown { get; private set; }

        /// <summary>
        /// Step 11d (Note 15): at the garden the window glides over so all five buds and the explorer are in view (from
        /// the path only three or four showed), following the explorer within the garden, and back to plain following
        /// when they walk off, the garden is done or a battle takes the view. Too far for one window: a gold tile marks
        /// where to stand.
        /// </summary>
        void Settle()
        {
            if (window == null || explorer == null || island == null || island.map == null) return;
            Vector2 lo = buds.Length > 0 ? buds[0] : gardenCenter, hi = lo;
            foreach (var b in buds)
            {
                lo = Vector2.Min(lo, b);
                hi = Vector2.Max(hi, b);
            }
            var c = BudsCenter;
            Vector2 ex = explorer.Tile;
            bool near = Mathf.Max(Mathf.Abs(ex.x - c.x), Mathf.Abs(ex.y - c.y)) <= settleReach;
            Vector2 all0 = Vector2.Min(lo, ex), all1 = Vector2.Max(hi, ex);
            // The window holds about its width in tiles; keep a tile and a half of margin for the round corners.
            float room = Mathf.Min(island.map.Columns, island.map.Rows) / Mathf.Max(1f, island.Zoom) - 2.5f;
            bool fits = all1.x - all0.x <= room && all1.y - all0.y <= room;
            bool live = Here && !IsSolved && !_finishing && !explorer.Locked && !Opening.InProgress && !Onboarding.Running;
            bool want = live && near && fits;
            Vector2 focus = (all0 + all1) * 0.5f;
            if (want && (!_settled && !window.HasFocus || _settled && (focus - _focusAt).sqrMagnitude > 0.06f))
            {
                if (!_settled) Debug.Log($"[RainGarden] {id}: at the garden: the window settles over all {buds.Length} buds and the explorer.");
                window.SetFocus(focus);
                _focusAt = focus;
                _settled = true;
            }
            else if (!want && _settled)
            {
                _settled = false;
                if (!explorer.Locked) window.SetFocus(null);
            }
            // Too far apart for one window: a gold tile just south of the buds is where to stand.
            Vector2Int standAt = default;
            bool stand = live && !fits && Mathf.Max(Mathf.Abs(ex.x - c.x), Mathf.Abs(ex.y - c.y)) <= settleReach + 4f && StandTile(hi, out standAt);
            if (focusMaterial != null && _stand == null) _stand = new GuideRings("Lotus Stand Here", island, focusMaterial, 1, fx);
            if (_stand != null)
            {
                float pulse = 0.85f + 0.2f * Mathf.Sin(Time.time * 3f);
                _stand.Draw(0, stand, stand ? new Vector3(standAt.x, 0.04f, standAt.y) : default, 1.3f, 1.5f * pulse, 0.35f * pulse);
            }
            if (stand != StandHereShown) Debug.Log($"[RainGarden] {id}: stand-here tile {(stand ? "shown" : "gone")}.");
            StandHereShown = stand;
        }

        /// <summary>The first walkable tile on the buds' column just south of them (toward the player).</summary>
        bool StandTile(Vector2 budsMax, out Vector2Int tile)
        {
            int col = Mathf.RoundToInt(BudsCenter.x - 0.5f);
            for (int row = Mathf.CeilToInt(budsMax.y) + 1; row <= Mathf.CeilToInt(budsMax.y) + 5; row++)
            {
                tile = new Vector2Int(col, row);
                if (map.CanWalk(tile)) return true;
            }
            tile = default;
            return false;
        }

        static int Count(int mask)
        {
            int n = 0;
            for (; mask != 0; mask &= mask - 1) n++;
            return n;
        }

        // ---- bridge ----

        /// <summary>Debug (bridge): a palm held down over map point (column, row), 12 cm up, for <paramref name="seconds"/>; wiggling or not.</summary>
        public void DebugPalm(Vector2 tile, float seconds, bool wiggle)
        {
            _debugPalm = tile;
            _debugUntil = Time.time + seconds;
            _debugWiggle = wiggle ? wiggleSpeed * 2f : 0f;
            _cloudMap = new Vector3(tile.x, 0.12f * Scale / Mathf.Max(1e-4f, TileWorld), tile.y);
        }

        public string Describe()
        {
            if (_garden == null) return "no garden";
            var sb = new System.Text.StringBuilder($"bloomed {Count(_garden.Mask)}/{buds.Length} (mask {_garden.Mask}), cloud {_cloud.Size:0.00}{(_cloud.Raining ? _cloud.Hard ? " raining hard" : " raining" : "")}");
            for (int i = 0; i < buds.Length; i++) sb.Append($", bud {i} {_garden.fill[i]:0.00}");
            if (Stream) sb.Append($", stream {_wetCount}/{streamTiles.Length} wet (swell {_swell:0.00}), bridge {_bridge:0.00}");
            return sb.ToString();
        }
    }
}
