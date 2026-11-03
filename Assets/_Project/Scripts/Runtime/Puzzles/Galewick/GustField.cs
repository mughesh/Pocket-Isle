using System;
using Diorama.Audio;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Galewick's wind toy (step 10b, "fan the wind"): an open, flat hand (fingers out, not pinching, not a fist, not
    /// holding the island) sweeping over the island faster than <see cref="GustTracker.minSpeed"/> within
    /// <see cref="GustTracker.maxHeight"/> above it is a gust (<see cref="GustTracker"/>, either hand). The world answers
    /// everywhere at once: a bright ripple band runs across all the grass on the island in the sweep direction and flattens
    /// it (grass shader globals _DioramaGust*), wind streaks and loose leaves fly along the sweep (pooled BattleFx), every
    /// windmill's sails get a nudge (RealmState.SpinNudge). Works anywhere in Galewick, restored or not; not in battles,
    /// zoom, swaps or interiors. <see cref="Gusted"/> tells listeners (the cloud bank) where and which way it blew.
    /// </summary>
    [DefaultExecutionOrder(-15)]
    public class GustField : MonoBehaviour
    {
        public WorldMap map;
        public IslandBuilder island;
        public WorldWindow window;
        public EncounterDirector encounters;
        [Tooltip("Hands busy with the island (or a puzzle) don't gust.")]
        public IslandHandle grabGuard;
        public BattleFx fx;

        [Header("Detection (GustTracker; real-world metres)")]
        public float minSpeed = 0.6f;
        public float fullSpeed = 1.6f;
        public float maxHeight = 0.3f;
        public float cooldown = 0.3f;
        [Tooltip("Fingertips (index, middle, ring) at least this far from the palm = an open hand (metres).")]
        public float openReach = 0.07f;

        [Header("Ripple (island-local metres)")]
        [Tooltip("How long the band takes to cross the island at full strength (seconds; weaker gusts are slower).")]
        public float crossSeconds = 1.4f;
        public float bandWidth = 0.1f;
        [Tooltip("Nov 3: wind streaks ride the band across the island every this many seconds (0 = only the burst at the hand).")]
        public float trailEvery = 0.06f;
        [Tooltip("Sails turn this much extra per gust at full strength (radians).")]
        public float spinNudge = 1.2f;
        [Tooltip("The whoosh of a gust at full strength (weaker gusts are quieter and a little lower).")]
        [Range(0f, 1f)] public float volume = 0.55f;

        /// <summary>A gust: island-local origin (xz), unit direction (xz), strength 0..1.</summary>
        public event Action<Vector2, Vector2, float> Gusted;

        /// <summary>Gusts so far this session (debug).</summary>
        public int Count { get; private set; }

        // Nov 3: two bands, so a sweep back the other way starts its own wave while the first keeps crossing (with one
        // band the second sweep restarted it: a left-right sweep cut each wave off).
        static readonly int[] GustId = { Shader.PropertyToID("_DioramaGust"), Shader.PropertyToID("_DioramaGust2") };
        static readonly int[] GustFrontId = { Shader.PropertyToID("_DioramaGustFront"), Shader.PropertyToID("_DioramaGustFront2") };
        static readonly int[] GustDirId = { Shader.PropertyToID("_DioramaGustDir"), Shader.PropertyToID("_DioramaGustDir2") };

        /// <summary>One gust's band crossing the island (island-local xz).</summary>
        class Band
        {
            public Vector2 origin, dir;
            public float strength, front = 99f, end, from, cross, trailAt;
            public bool Live => front <= end;
            public float Progress => Mathf.Clamp01((front - from) / Mathf.Max(0.01f, end - from));
        }

        readonly GustTracker[] _trackers = { new GustTracker(), new GustTracker() };
        readonly Band[] _bands = { new Band(), new Band() };
        int _sweepBand;
        AudioSource _audio;
        LoopAnchor _sweep;
        float _sweepPeak;

        bool Active => island != null && map != null && island.world == map && !SceneSwap.IsInside && !SceneSwap.IsBusy &&
                       (window == null || !window.IsZoomed) && (encounters == null || !encounters.InBattle) && !GamePause.IsPaused;

        void OnDisable()
        {
            foreach (var id in GustFrontId) Shader.SetGlobalVector(id, new Vector4(99f, 1f, 0f, 0f));
            _sweep?.Off(0.2f);
        }

        void Update()
        {
            foreach (var t in _trackers)
            {
                t.minSpeed = minSpeed;
                t.fullSpeed = fullSpeed;
                t.maxHeight = maxHeight;
                t.cooldown = cooldown;
            }
            if (Active)
                for (int h = 0; h < 2; h++) Sample(h);
            else
                foreach (var t in _trackers) t.Lose();
            Animate();
        }

        void Sample(int h)
        {
            var hand = HandRegistry.Get(h == 0 ? Handedness.Left : Handedness.Right);
            if (hand == null || !hand.IsTrackedDataValid || !HandLookup.TryGetJoint(hand, HandJointId.HandPalm, out var palm))
            {
                _trackers[h].Lose();
                return;
            }
            bool open = !hand.GetIndexFingerIsPinching() && (grabGuard == null || !grabGuard.IsHandBusy(h == 0)) && Open(hand, palm);
            Vector3 local = island.transform.InverseTransformPoint(palm);
            float scale = island.transform.lossyScale.x;
            bool over = island.FootprintContains(local);
            float height = (local.y - island.CapTopLocal) * scale;
            var gust = _trackers[h].Feed(Time.deltaTime, local * scale, open, over, height);
            if (gust.HasValue) TileTouch.Block(h == 0, 0.8f); // the sweeping hand's fingertips brush the grass: no walking
            if (gust.HasValue) Blow(gust.Value.origin / scale, gust.Value.direction, gust.Value.strength, $"{(h == 0 ? "left" : "right")} hand, {gust.Value.speed:0.00} m/s");
        }

        bool Open(IHand hand, Vector3 palm)
        {
            foreach (var j in new[] { HandJointId.HandIndexTip, HandJointId.HandMiddleTip, HandJointId.HandRingTip })
                if (!HandLookup.TryGetJoint(hand, j, out var tip) || (tip - palm).magnitude < openReach) return false;
            return true;
        }

        /// <summary>Bands crossing now (debug).</summary>
        public int LiveBands => (_bands[0].Live ? 1 : 0) + (_bands[1].Live ? 1 : 0);

        /// <summary>A gust at island-local <paramref name="origin"/> (xz) blowing <paramref name="dir"/> (xz, unit).</summary>
        public void Blow(Vector2 origin, Vector2 dir, float strength, string how, bool notify = true)
        {
            if (!Active) return;
            Count++;
            // A free band, else the one furthest across (its wave is nearly done).
            int slot = !_bands[0].Live ? 0 : !_bands[1].Live ? 1 : _bands[0].Progress >= _bands[1].Progress ? 0 : 1;
            var b = _bands[slot];
            b.origin = origin;
            b.dir = dir.sqrMagnitude > 1e-6f ? dir.normalized : Vector2.up;
            b.strength = Mathf.Clamp01(strength);
            // The band starts just behind the island's upwind edge and crosses to the far edge.
            var half = island.FootprintHalfSize;
            float reach = Mathf.Abs(b.dir.x) * half.x + Mathf.Abs(b.dir.y) * half.y + bandWidth * 2f;
            float centre = -Vector2.Dot(b.origin, b.dir); // the island centre's offset along the sweep, from the origin
            b.front = b.from = centre - reach;
            b.end = centre + reach;
            b.cross = (b.end - b.front) / Mathf.Max(0.1f, crossSeconds * Mathf.Lerp(1.6f, 1f, b.strength));
            RealmState.SpinNudge += spinNudge * b.strength;
            Debug.Log($"[GustField] Gust {Count} by the {how}: strength {b.strength:0.00}, toward ({b.dir.x:0.0}, {b.dir.y:0.0}), band {slot} ({LiveBands} crossing).");
            Streaks(b);
            if (_audio == null)
            {
                _audio = gameObject.AddComponent<AudioSource>();
                _audio.playOnAwake = false;
                _audio.spatialBlend = 0f;
            }
            _audio.pitch = Mathf.Lerp(0.85f, 1.1f, b.strength) * UnityEngine.Random.Range(0.95f, 1.05f);
            if (AudioService.Has(AudioIds.WindSweep))
            {
                // wind.sweep follows the newest sweep: louder and higher for a faster hand, fading as its band crosses (Animate).
                _sweep ??= new LoopAnchor(transform, "Wind Sweep Sound");
                _sweep.Set(AudioIds.WindSweep, island.transform.TransformPoint(new Vector3(b.origin.x, island.CapTopLocal, b.origin.y)), 0.08f);
                _sweep.Handle.SetPitch(Mathf.Lerp(0.85f, 1.2f, b.strength));
                _sweepPeak = Mathf.Lerp(0.45f, 1f, b.strength);
                _sweepBand = slot;
                _sweep.Handle.SetVolume(_sweepPeak);
            }
            else GameAudio.Play(AudioIds.WindSweep, transform.position, _audio, PuzzleSounds.Breeze, volume * Mathf.Lerp(0.5f, 1f, b.strength));
            if (notify) Gusted?.Invoke(b.origin, b.dir, b.strength);
        }

        void Animate()
        {
            if (_sweep != null && _sweep.Playing)
            {
                var sb = _bands[_sweepBand];
                float u = sb.Progress;
                _sweep.Handle.SetVolume(_sweepPeak * (1f - u * u));
                if (!sb.Live) _sweep.Off(0.4f);
            }
            for (int i = 0; i < _bands.Length; i++)
            {
                var b = _bands[i];
                if (!b.Live || island == null)
                {
                    Shader.SetGlobalVector(GustFrontId[i], new Vector4(99f, 1f, 0f, 0f));
                    continue;
                }
                b.front += b.cross * Time.deltaTime;
                // The shader works in map space: the island-local origin and the front, through the current view.
                var toLocal = island.ContentToLocal;
                var mapOrigin = toLocal.inverse.MultiplyPoint3x4(new Vector3(b.origin.x, 0f, b.origin.y));
                float zoom = Mathf.Max(1e-4f, island.Zoom);
                float fade = Mathf.Clamp01((b.end - b.front) / (bandWidth * 3f));
                Shader.SetGlobalVector(GustId[i], new Vector4(mapOrigin.x, mapOrigin.z, b.dir.x, b.dir.y));
                Shader.SetGlobalVector(GustFrontId[i], new Vector4(b.front / zoom, bandWidth / zoom, b.strength * fade, 0f));
                var worldDir = island.transform.TransformDirection(new Vector3(b.dir.x, 0f, b.dir.y)).normalized;
                Shader.SetGlobalVector(GustDirId[i], new Vector4(worldDir.x, worldDir.y, worldDir.z, 0f));
                Trail(b, fade);
            }
        }

        /// <summary>
        /// Nov 3: streaks and a leaf or two along the band's front while it crosses, so the wind keeps travelling after
        /// the hand has gone (it ended almost with the sweep before).
        /// </summary>
        void Trail(Band b, float fade)
        {
            if (fx == null || trailEvery <= 0f || Time.time < b.trailAt) return;
            b.trailAt = Time.time + trailEvery;
            float scale = island.transform.lossyScale.x;
            float tile = island.map.tileSize * island.Zoom * scale;
            var half = island.FootprintHalfSize;
            var perp = new Vector2(-b.dir.y, b.dir.x);
            float across = Mathf.Abs(perp.x) * half.x + Mathf.Abs(perp.y) * half.y;
            var worldDir = island.transform.TransformDirection(new Vector3(b.dir.x, 0f, b.dir.y)).normalized;
            float strength = b.strength * Mathf.Lerp(0.4f, 1f, fade);
            for (int i = 0; i < 2; i++)
            {
                var p = b.origin + b.dir * b.front + perp * UnityEngine.Random.Range(-across, across);
                var local = new Vector3(p.x, island.CapTopLocal + 0.012f + UnityEngine.Random.Range(0f, 0.02f), p.y);
                if (!island.FootprintContains(local)) continue;
                var at = island.transform.TransformPoint(local);
                fx.Burst(at, 2, new Color(1f, 1f, 1f, 0.45f * fade + 0.1f), tile * 0.2f, 0.6f, tile * 0.025f, worldDir * tile * 7f * strength);
                if (UnityEngine.Random.value < 0.25f)
                    fx.Burst(at, 1, UnityEngine.Random.value < 0.5f ? new Color(0.62f, 0.8f, 0.36f) : new Color(0.9f, 0.78f, 0.38f), tile * 0.3f, 0.9f,
                        tile * 0.04f, worldDir * tile * 5f * strength + Vector3.up * tile * 0.6f, tile * 2f);
            }
        }

        void Streaks(Band b)
        {
            if (fx == null) return;
            float scale = island.transform.lossyScale.x;
            float tile = island.map.tileSize * island.Zoom * scale;
            var worldDir = island.transform.TransformDirection(new Vector3(b.dir.x, 0f, b.dir.y)).normalized;
            for (int i = 0; i < 5; i++)
            {
                float along = (i - 1) * 0.07f;
                var local = new Vector3(b.origin.x + b.dir.x * along, island.CapTopLocal + 0.02f + 0.015f * (i % 3), b.origin.y + b.dir.y * along);
                var at = island.transform.TransformPoint(local);
                fx.Burst(at, 4, new Color(1f, 1f, 1f, 0.55f), tile * 0.25f, 0.7f, tile * 0.03f, worldDir * tile * 9f * b.strength);
                if (i % 2 == 0)
                    fx.Burst(at, 2, i % 4 == 0 ? new Color(0.62f, 0.8f, 0.36f) : new Color(0.9f, 0.78f, 0.38f), tile * 0.4f, 0.8f, tile * 0.045f,
                        worldDir * tile * 6f * b.strength + Vector3.up * tile * 0.8f, tile * 2f);
            }
        }

        /// <summary>Debug (bridge): a gust across the middle of the island toward <paramref name="dir"/> (island xz).</summary>
        public void DebugGust(Vector2 dir, float strength = 0.9f)
        {
            if (!Active) return;
            Blow(-dir.normalized * 0.12f, dir, strength, "bridge");
        }
    }
}
