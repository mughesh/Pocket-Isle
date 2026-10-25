using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Diorama
{
    /// <summary>
    /// Sets the realm's restoration (<see cref="RealmState"/>) when the scene starts, from the island world's theme,
    /// and fades it for testing: G in the Editor, or three quick left-hand thumb taps in a development build.
    /// The story will drive <see cref="FadeTo"/> later. Remembers each realm's value while another map shows
    /// (<see cref="Remember"/> / <see cref="Recall"/>, keyed by the world map's name; the save keeps them).
    /// </summary>
    [ExecuteAlways]
    public class RealmRestoration : MonoBehaviour
    {
        public IslandBuilder island;
        [Tooltip("Optional. Debug toggle on device: three quick left-hand thumb taps (development builds).")]
        public MicrogestureInput input;
        public float fadeSeconds = 2f;

        float _from, _to, _t = 1f;
        readonly Dictionary<string, float> _byRealm = new Dictionary<string, float>();

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
            if (Application.isPlaying && input != null && (Debug.isDebugBuild || Application.isEditor)) input.TapBurst += OnTapBurst;
        }

        void OnDisable()
        {
            if (input != null) input.TapBurst -= OnTapBurst;
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

        // Exactly three quick taps (five toggle the debug labels, see DebugLabels).
        void OnTapBurst(int count, GestureSource source)
        {
            if (count == 3 && source == GestureSource.LeftHand) Toggle();
        }

        void Update()
        {
            if (!Application.isPlaying) return;
            var kb = Keyboard.current;
            if (kb != null && kb.gKey.wasPressedThisFrame) Toggle();
            if (_t >= 1f) return;
            _t = Mathf.Min(1f, _t + Time.deltaTime / Mathf.Max(0.01f, fadeSeconds));
            RealmState.Restored = Mathf.Lerp(_from, _to, Mathf.SmoothStep(0f, 1f, _t));
        }
    }
}
