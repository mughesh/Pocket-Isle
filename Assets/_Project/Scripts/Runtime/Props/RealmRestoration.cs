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
    }
}
