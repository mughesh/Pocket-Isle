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

        [Header("Buds (BudGarden)")]
        public float softSeconds = 2f, hardSeconds = 1f;
        [Tooltip("Look + pinch-hold rains on the looked-at bud this fast (seconds to bloom).")]
        public float lookSeconds = 2.5f;

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
        // Debug (bridge): a synthetic palm-down hand over this map point for a while.
        Vector2? _debugPalm;
        float _debugUntil, _debugWiggle;

        public RainCloud Cloud => _cloud;
        public BudGarden Garden => _garden;
        public override bool Busy => _cloud != null && _cloud.Holder >= 0 || _lookHold;
        protected override Vector2 HomeTile => gardenCenter;
        float Scale => grabGuard != null ? grabGuard.ScaleFactor : 1f;

        public override Vector3? FocusPoint
        {
            get
            {
                if (!Here || IsSolved || _garden == null) return null;
                for (int i = 0; i < buds.Length; i++)
                    if (!_garden.Bloomed(i)) return MapToWorld(new Vector3(buds[i].x, 1.6f, buds[i].y));
                return null;
            }
        }

        public override float FocusRadius => 1.2f * TileWorld;

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
            int all = (1 << buds.Length) - 1;
            _garden.Restore(IsSolved ? all : PuzzleState.Stage(id));
            Open(IsSolved);
            SetProgressQuiet(Count(_garden.Mask) / (float)Mathf.Max(1, buds.Length));
            Debug.Log($"[RainGarden] {id}: {Count(_garden.Mask)}/{buds.Length} lotus bloomed, crossing {(IsSolved ? "open" : "dry")}.");
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
            if (_cloud.Raining && live) RainOnBuds(new Vector2(_cloudMap.x, _cloudMap.z), rainRadius, _cloud.Hard, Time.deltaTime);
            if (_lookHold && _gazed >= 0) RainOnBud(_gazed, Time.deltaTime * softSeconds / Mathf.Max(0.1f, lookSeconds), false);
            if (!IsSolved && !_finishing && _garden.AllBloomed) StartCoroutine(Finish());
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
            GameAudio.Procedural(_audio, PuzzleSounds.Catch, volume * 0.7f);
            GameAudio.At(AudioIds.PondSplash, p);
            if (fx == null) return;
            fx.Burst(p, 18, new Color(0.98f, 0.68f, 0.76f), TileWorld * 0.6f, 0.8f, TileWorld * 0.05f, island.transform.up * TileWorld * 0.6f);
            fx.Burst(p, 10, new Color(0.7f, 0.92f, 1f, 0.9f), TileWorld * 0.5f, 0.5f, TileWorld * 0.04f, island.transform.up * TileWorld * 0.3f);
        }

        IEnumerator Finish()
        {
            _finishing = true;
            Debug.Log($"[RainGarden] {id}: every bud bloomed: a lotus path across the garden.");
            yield return new WaitForSeconds(0.6f);
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
            Debug.Log($"[RainGarden] {id}: the realm is restored, so the lotus stones are open.");
            _garden.Restore((1 << buds.Length) - 1);
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
                _budViews[i]?.Place(buds[i], 0f, Quaternion.Euler(sway, 40f * i, 0f), here && !bloomed, budScale * (1f + 0.25f * wet));
                float pop = 1f + 0.35f * Mathf.Sin(_pop[i] * Mathf.PI);
                _stoneViews[i]?.Place(buds[i], 0.02f, Quaternion.Euler(0f, 30f * i, 0f), here && bloomed, stoneScale * pop);
            }
            DrawCloud(here);
        }

        void DrawCloud(bool here)
        {
            if (_cloudView == null) return;
            float size = _cloud != null ? _cloud.Size : 0f;
            bool show = here && size > 0.01f;
            float belowTiles = cloudBelow * Scale / Mathf.Max(1e-4f, TileWorld);
            float h = Mathf.Max(0.6f, _cloudMap.y - belowTiles);
            _cloudView.Place(new Vector2(_cloudMap.x, _cloudMap.z), h - 0.5f * cloudScale, Quaternion.Euler(0f, Time.time * 10f, 0f), show,
                cloudScale * Mathf.SmoothStep(0.2f, 1f, size));
            if (!show || !_cloud.Raining || fx == null) return;
            // Rain: short streaks falling from the cloud, splash dots on the bed; more when hard.
            float rate = _cloud.Hard ? 0.02f : 0.05f;
            float tile = TileWorld;
            while (Time.time >= _dropAt)
            {
                _dropAt = Mathf.Max(_dropAt + rate, Time.time - 0.1f);
                var from = MapToWorld(new Vector3(_cloudMap.x + Random.Range(-0.6f, 0.6f) * cloudScale * 1.6f, h - 0.3f, _cloudMap.z + Random.Range(-0.4f, 0.4f) * cloudScale * 1.6f));
                fx.Burst(from, 1, new Color(0.75f, 0.9f, 1f, 0.9f), tile * 0.02f, 0.35f, tile * 0.03f, -island.transform.up * tile * h * 2.6f);
                var ground = MapToWorld(new Vector3(_cloudMap.x + Random.Range(-1f, 1f), 0.02f, _cloudMap.z + Random.Range(-1f, 1f)));
                fx.Burst(ground, 1, new Color(0.65f, 0.85f, 0.95f, 0.8f), tile * 0.1f, 0.25f, tile * 0.03f, island.transform.up * tile * 0.15f);
            }
            if (Time.time >= _dripAt)
            {
                _dripAt = Time.time + (_cloud.Hard ? 0.6f : 1.1f);
                GameAudio.At(AudioIds.SwDrip, MapToWorld(new Vector3(_cloudMap.x, 0f, _cloudMap.z)));
            }
        }

        Vector3 BudWorld(int i) => MapToWorld(new Vector3(buds[i].x, 0.4f, buds[i].y));

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
            return sb.ToString();
        }
    }
}
