using System.Collections.Generic;
using UnityEngine;

namespace Diorama.Audio
{
    /// <summary>
    /// Where sweeteners may play: a disc (centre, radius) on the island's play surface, up to height above it. World space.
    /// The main lane feeds it from the island (AmbienceDirector.Bounds = ...) when the island moves or scales.
    /// </summary>
    public struct IslandBounds
    {
        public Vector3 centre;
        public float radius, height;

        public IslandBounds(Vector3 centre, float radius = 0.35f, float height = 0.05f)
        {
            this.centre = centre;
            this.radius = radius;
            this.height = height;
        }

        /// <summary>A uniform random point on the disc, lifted by up to height.</summary>
        public Vector3 RandomPoint()
        {
            var p = Random.insideUnitCircle * radius;
            return centre + new Vector3(p.x, Random.value * height, p.y);
        }
    }

    /// <summary>
    /// Ambience: a 2D bed per area (crossfaded) and sweeteners as 3D one-shots at random points on the island, every few
    /// seconds, never more than a set's maxAtOnce, never the same clip twice in a row (AudioCue.Pick).
    /// SetArea(profile, crossfade) on area changes; Duck(dB, fade, pauseSweeteners) for battle and dialogue.
    /// Created on first use, survives scene swaps.
    /// </summary>
    public class AmbienceDirector : MonoBehaviour
    {
        static AmbienceDirector _instance;
        static bool _boundsSet, _warnedBounds;
        static IslandBounds _bounds = new IslandBounds(Vector3.zero);

        AmbienceProfile _profile;
        AudioHandle _bed;
        float _duckDb, _duckFrom, _duckTo, _duckT = 1f, _duckTime;
        bool _sweetPaused;
        readonly List<float> _next = new List<float>();
        readonly List<List<AudioHandle>> _live = new List<List<AudioHandle>>();

        static AmbienceDirector Instance
        {
            get
            {
                if (_instance == null && Application.isPlaying)
                {
                    var go = new GameObject("AmbienceDirector");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<AmbienceDirector>();
                }
                return _instance;
            }
        }

        /// <summary>The island surface sweeteners land on. Until the main lane sets it: a 0.35 m disc in front of the listener (logged once).</summary>
        public static IslandBounds Bounds
        {
            get => _bounds;
            set
            {
                _bounds = value;
                _boundsSet = true;
            }
        }

        public static AmbienceProfile Current => _instance != null ? _instance._profile : null;

        /// <summary>Switches area: the old bed fades out while the new one fades in; sweeteners restart on the new sets.</summary>
        public static void SetArea(AmbienceProfile profile, float crossfade = 2f)
        {
            var d = Instance;
            if (d == null || d._profile == profile) return;
            d._bed.Stop(crossfade);
            d._bed = default;
            d._profile = profile;
            d._next.Clear();
            d._live.Clear();
            if (profile == null)
            {
                Debug.Log("[Ambience] Area cleared.");
                return;
            }
            if (!string.IsNullOrEmpty(profile.bed))
            {
                d._bed = AudioService.PlayLoop2D(profile.bed, Mathf.Max(0.01f, crossfade));
                d.ApplyBedVolume();
            }
            foreach (var s in profile.sweeteners)
            {
                d._next.Add(Time.unscaledTime + Random.Range(s.minInterval * 0.5f, s.maxInterval));
                d._live.Add(new List<AudioHandle>());
            }
            Debug.Log($"[Ambience] Area {profile.name}: bed '{profile.bed}', {profile.sweeteners.Count} sweetener sets.");
        }

        /// <summary>Lowers the bed by amountDb (e.g. -6 for battle and dialogue; 0 = back to normal) over fade seconds.</summary>
        public static void Duck(float amountDb, float fade = 0.5f, bool pauseSweeteners = false)
        {
            var d = Instance;
            if (d == null) return;
            d._duckFrom = d._duckDb;
            d._duckTo = Mathf.Min(0f, amountDb);
            d._duckTime = Mathf.Max(0.01f, fade);
            d._duckT = 0f;
            d._sweetPaused = pauseSweeteners;
        }

        void ApplyBedVolume()
        {
            if (_profile == null) return;
            _bed.SetVolume(_profile.bedVolume * Mathf.Pow(10f, _duckDb / 20f));
        }

        void Update()
        {
            if (_duckT < 1f)
            {
                _duckT = Mathf.Min(1f, _duckT + Time.unscaledDeltaTime / _duckTime);
                _duckDb = Mathf.Lerp(_duckFrom, _duckTo, _duckT);
                ApplyBedVolume();
            }
            if (_profile == null || _sweetPaused || AudioListener.pause) return;
            float now = Time.unscaledTime;
            for (int i = 0; i < _profile.sweeteners.Count && i < _next.Count; i++)
            {
                if (now < _next[i]) continue;
                var s = _profile.sweeteners[i];
                _next[i] = now + Random.Range(s.minInterval, Mathf.Max(s.minInterval, s.maxInterval));
                var live = _live[i];
                live.RemoveAll(h => !h.IsValid);
                if (live.Count >= s.maxAtOnce || string.IsNullOrEmpty(s.cue)) continue;
                var h = AudioService.Play(s.cue, SweetenerPoint());
                if (!h.IsValid) continue;
                h.SetVolume(s.volume * (1f - s.volumeJitter * Random.value));
                h.SetPitch(1f + s.pitchJitter * Random.Range(-1f, 1f));
                live.Add(h);
            }
        }

        static Vector3 SweetenerPoint()
        {
            if (!_boundsSet)
            {
                if (!_warnedBounds)
                {
                    _warnedBounds = true;
                    Debug.LogWarning("[Ambience] AmbienceDirector.Bounds not set by the island yet: sweeteners use a 0.35 m disc 0.5 m in front of the listener.");
                }
                var listener = FindAnyObjectByType<AudioListener>();
                var t = listener != null ? listener.transform : null;
                var centre = t != null ? t.position + Vector3.ProjectOnPlane(t.forward, Vector3.up).normalized * 0.5f + Vector3.down * 0.3f : Vector3.zero;
                return new IslandBounds(centre).RandomPoint();
            }
            return _bounds.RandomPoint();
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _instance = null;
            _boundsSet = false;
            _warnedBounds = false;
            _bounds = new IslandBounds(Vector3.zero);
        }
    }
}
