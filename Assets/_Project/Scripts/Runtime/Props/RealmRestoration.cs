using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Diorama
{
    /// <summary>
    /// Sets the realm's restoration (<see cref="RealmState"/>) when the scene starts, from the island world's theme,
    /// and fades it for testing: G in the Editor, or the dev panel's Story tab (step 9e: the thumb-tap toggle is gone,
    /// taps also stop walking).
    /// The story will drive <see cref="FadeTo"/> later. Remembers each realm's value while another map shows
    /// (<see cref="Remember"/> / <see cref="Recall"/>, keyed by the world map's name; the save keeps them).
    /// </summary>
    [ExecuteAlways]
    public class RealmRestoration : MonoBehaviour
    {
        public IslandBuilder island;
        public float fadeSeconds = 2f;

        float _from, _to, _t = 1f;
        readonly Dictionary<string, float> _byRealm = new Dictionary<string, float>();
        static readonly int WaveId = Shader.PropertyToID("_RestoreWave");
        float _waveT = -1f, _waveSeconds;
        Vector2 _waveOrigin;
        float _waveReach;

        /// <summary>A restoration wave is rippling out (step 9c).</summary>
        public bool Waving => _waveT >= 0f;

        /// <summary>
        /// Restores the realm in a wave rippling out from a map tile over <paramref name="seconds"/> (step 9c, the Keeper
        /// calmed): props inside the ring light up first, the rest of the realm warms with it.
        /// </summary>
        public void Wave(Vector2 originTile, float seconds)
        {
            var map = island != null ? island.Content : null;
            float tile = island != null && island.map != null ? island.map.tileSize : 0.05f;
            _waveOrigin = new Vector2(-originTile.x * tile, originTile.y * tile); // map space: columns run toward -X
            _waveOriginTile = originTile;
            _waveReach = map != null ? new Vector2(map.Columns, map.Rows).magnitude * tile : 4f;
            _waveSeconds = Mathf.Max(0.5f, seconds);
            _waveT = 0f;
            _from = RealmState.Restored;
            _to = 1f;
            _t = 1f; // the wave drives it
            Debug.Log($"[Realm] Restoration wave from c{originTile.x:0.#} r{originTile.y:0.#} over {_waveSeconds:0.#} s.");
        }

        /// <summary>Realms left behind (map name, restoration). The current map's value is <see cref="RealmState.Restored"/>.</summary>
        public IReadOnlyDictionary<string, float> Remembered => _byRealm;

        public void Remember(WorldMap map, float restored)
        {
            if (map != null) Remember(map.name, restored);
        }

        public void Remember(string realm, float restored)
        {
            if (!string.IsNullOrEmpty(realm)) _byRealm[realm] = Mathf.Clamp01(restored);
        }

        /// <summary>The map's remembered restoration, or its theme's start value the first time.</summary>
        public float Recall(WorldMap map)
        {
            if (map == null) return 1f;
            if (_byRealm.TryGetValue(map.name, out float v)) return v;
            return map.theme != null ? map.theme.startRestored : 1f;
        }

        public void Forget() => _byRealm.Clear();

        float StartValue
        {
            get
            {
                var theme = island != null && island.world != null ? island.world.theme : null;
                return theme != null ? theme.startRestored : 1f;
            }
        }

        void OnEnable()
        {
            RealmState.Restored = StartValue;
            _to = RealmState.Restored;
            _t = 1f;
        }

        void OnDisable()
        {
            Shader.SetGlobalVector(WaveId, new Vector4(0f, 0f, -1f, 1f));
        }

        /// <summary>Fades the realm to <paramref name="restored"/> (0..1) over <see cref="fadeSeconds"/>.</summary>
        public void FadeTo(float restored)
        {
            _from = RealmState.Restored;
            _to = Mathf.Clamp01(restored);
            _t = 0f;
            Debug.Log($"[Realm] Fading to {(_to > 0.5f ? "restored" : "unrestored")} ({_to:0.##}).");
        }

        public void Toggle() => FadeTo(_to > 0.5f ? 0f : 1f);

        /// <summary>Sets the restoration at once, no fade (loading a save).</summary>
        public void SetNow(float restored)
        {
            RealmState.Restored = restored;
            _from = _to = RealmState.Restored;
            _t = 1f;
        }

        void Update()
        {
            if (!Application.isPlaying) return;
            var kb = Keyboard.current;
            if (kb != null && kb.gKey.wasPressedThisFrame) Toggle();
            if (_waveT >= 0f)
            {
                _waveT += Time.deltaTime / _waveSeconds;
                float k = Mathf.Clamp01(_waveT);
                RealmState.Restored = Mathf.Lerp(_from, 1f, Mathf.SmoothStep(0f, 1f, k));
                float tile = island != null && island.map != null ? island.map.tileSize : 0.05f;
                Shader.SetGlobalVector(WaveId, k >= 1f ? new Vector4(0f, 0f, -1f, 1f)
                    : new Vector4(_waveOrigin.x, _waveOrigin.y, _waveReach * Mathf.Sqrt(k), tile * 6f));
                if (k < 1f && PolishConfig.RestoreWaveOn) WaveFront(_waveReach * Mathf.Sqrt(k) / tile);
                if (k >= 1f)
                {
                    _waveT = -1f;
                    _to = 1f;
                }
                return;
            }
            if (_t >= 1f) return;
            _t = Mathf.Min(1f, _t + Time.deltaTime / Mathf.Max(0.01f, fadeSeconds));
            RealmState.Restored = Mathf.Lerp(_from, _to, Mathf.SmoothStep(0f, 1f, _t));
        }

        // ---- Nov 5: the wave front you can see (PolishConfig.restoreWave) ----

        Vector2 _waveOriginTile;
        BattleFx _fx;
        float _ringAcc, _liftAcc;

        /// <summary>
        /// A soft ring of the realm's colour just ahead of the wave, and leaves / petals / sparkles lifting where it passes:
        /// dots of the pooled battle particle system, only where the ring crosses the island window (no extra draw).
        /// </summary>
        void WaveFront(float radiusTiles)
        {
            if (island == null || island.world == null || island.Content == null) return;
            if (_fx == null) _fx = FindFirstObjectByType<BattleFx>();
            if (_fx == null) return;
            var cfg = PolishConfig.Get();
            Palette(island.world.name, out var ring, out var liftA, out var liftB);
            float tileWorld = island.map.tileSize * island.Zoom * island.transform.lossyScale.x;
            float cap = island.Content.capHeight;
            Vector3 up = island.transform.up;
            _ringAcc += Time.deltaTime * 160f;
            _liftAcc += Time.deltaTime * (cfg != null ? cfg.waveLifts : 0f);
            int guard = 0;
            while ((_ringAcc >= 1f || _liftAcc >= 1f) && guard++ < 40)
            {
                bool lift = _liftAcc >= 1f && (_ringAcc < 1f || Random.value < 0.4f);
                float r = radiusTiles + (lift ? Random.Range(-0.6f, 0f) : Random.Range(0.2f, 0.7f));
                float a = Random.value * Mathf.PI * 2f;
                var local = island.TileToLocal(_waveOriginTile + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, cap);
                if (lift) _liftAcc -= 1f;
                else _ringAcc -= 1f;
                if (!island.FootprintContains(local)) continue;
                var world = island.transform.TransformPoint(local);
                if (lift)
                    _fx.Burst(world, 1, Random.value < 0.5f ? liftA : liftB, tileWorld * 0.25f, 1.3f, tileWorld * 0.1f, up * tileWorld * Random.Range(0.8f, 1.6f));
                else
                    _fx.Burst(world + up * tileWorld * 0.08f, 1, ring, tileWorld * 0.05f, 0.4f, tileWorld * 0.13f, up * tileWorld * 0.2f);
            }
        }

        static void Palette(string map, out Color ring, out Color liftA, out Color liftB)
        {
            string m = map.ToLowerInvariant();
            if (m.Contains("cinder"))
            {
                ring = new Color(1f, 0.62f, 0.25f);
                liftA = new Color(1f, 0.45f, 0.15f); // autumn leaves
                liftB = new Color(1f, 0.85f, 0.35f);
            }
            else if (m.Contains("gale"))
            {
                ring = new Color(0.75f, 1f, 0.8f);
                liftA = new Color(1f, 1f, 0.95f); // petals and seeds
                liftB = new Color(1f, 0.8f, 0.9f);
            }
            else if (m.Contains("lily"))
            {
                ring = new Color(0.45f, 0.9f, 1f);
                liftA = new Color(0.85f, 0.97f, 1f); // sparkles
                liftB = new Color(1f, 0.7f, 0.85f); // lotus petals
            }
            else
            {
                ring = new Color(0.7f, 1f, 0.55f);
                liftA = new Color(0.6f, 0.95f, 0.45f);
                liftB = new Color(1f, 0.9f, 0.5f);
            }
        }
    }
}
