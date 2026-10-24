using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Switches the island's content to another <see cref="WorldMap"/> and back: shrine to Hearth Lodge now, realm gates
    /// (step 8) and temple arenas (step 9) later. The swap takes <see cref="seconds"/>: the content sinks into the base
    /// front to back while the rocky base tints toward the next map's theme and the explorer shrinks into its tile; at
    /// the bottom the map, zoom (<see cref="WorldMap.viewZoom"/>) and light change; then the next map rises and the
    /// explorer pops up on the entry tile. <see cref="Return"/> goes back to the outdoor map, tile and facing.
    /// Inside an interior the realm restoration reads as 1 (interior props burn steady) and map creatures hide.
    /// </summary>
    public class SceneSwap : MonoBehaviour
    {
        public IslandBuilder island;
        public WorldWindow window;
        public GestureTestPiece explorer;
        [Tooltip("The main light (interiors can recolour it). Empty = RenderSettings.sun.")]
        public Light sun;
        [Tooltip("Every interior map a save can name (by asset name).")]
        public List<WorldMap> interiors = new List<WorldMap>();
        public float seconds = 1.2f;

        static SceneSwap _instance;

        /// <summary>True while an interior shows (also during the swap back until its bottom).</summary>
        public static bool IsInside => _instance != null && _instance.Inside;
        /// <summary>True while a swap animates.</summary>
        public static bool IsBusy => _instance != null && _instance.Busy;

        public bool Inside { get; private set; }
        public bool Busy { get; private set; }
        /// <summary>The interior showing, or null.</summary>
        public WorldMap Interior { get; private set; }
        /// <summary>The outdoor world (the one shown when not inside).</summary>
        public WorldMap Outdoor => Inside ? _outdoor : island != null ? island.world : null;
        /// <summary>Outdoor tile and facing to come back to.</summary>
        public Vector2Int ReturnTile { get; private set; }
        public Vector2Int ReturnFacing { get; private set; }
        /// <summary>The outdoor realm's restoration (RealmState reads 1 while inside).</summary>
        public float OutdoorRestored => Inside ? _outdoorRestored : RealmState.Restored;

        /// <summary>At the bottom of a swap into an interior (old content hidden, new not risen yet).</summary>
        public event Action<WorldMap> Entered;
        /// <summary>At the bottom of a swap back outside.</summary>
        public event Action<WorldMap> Returned;
        /// <summary>A swap finished rising (inside = true when it went in).</summary>
        public event Action<bool> Finished;

        WorldMap _outdoor;
        float _outdoorRestored;
        Color _lightColor;
        float _lightIntensity;
        bool _lightSaved;

        void OnEnable() => _instance = this;

        void OnDisable()
        {
            if (_instance == this) _instance = null;
        }

        void Start()
        {
            if (island == null || window == null || explorer == null)
            {
                Debug.LogError("[SceneSwap] Needs the island, world window and explorer (Set Up MR_Main (7b lodge)).", this);
                enabled = false;
            }
            if (sun == null) sun = RenderSettings.sun;
        }

        public WorldMap FindInterior(string name) => interiors.Find(m => m != null && m.name == name);

        /// <summary>Swaps into <paramref name="interior"/> with the explorer on <paramref name="entryTile"/>. False while busy or already inside.</summary>
        public bool Enter(WorldMap interior, Vector2Int entryTile, Vector2Int? facing = null)
        {
            if (!enabled || Busy || Inside || interior == null) return false;
            StartCoroutine(Swap(interior, entryTile, facing ?? new Vector2Int(0, -1), true));
            return true;
        }

        /// <summary>Swaps back to the outdoor map, tile and facing.</summary>
        public bool Return()
        {
            if (!enabled || Busy || !Inside) return false;
            StartCoroutine(Swap(_outdoor, ReturnTile, ReturnFacing, false));
            return true;
        }

        /// <summary>Puts the island inside at once (loading a save made inside). The explorer stands outside already.</summary>
        public void EnterNow(WorldMap interior, Vector2Int tile, Vector2Int facing)
        {
            if (!enabled || interior == null || Inside) return;
            var light = LightFor(interior.theme, true);
            SwapAtBottom(interior, tile, facing, true);
            SetLight(light, light, 1f);
            island.Prepare();
            Finished?.Invoke(true);
        }

        IEnumerator Swap(WorldMap to, Vector2Int tile, Vector2Int facing, bool entering)
        {
            Busy = true;
            explorer.Locked = true;
            double t0 = Time.realtimeSinceStartupAsDouble;
            float half = Mathf.Max(0.1f, seconds * 0.5f);
            var fromLight = LightNow();
            var toLight = LightFor(to.theme, entering);
            island.rimTheme = to.theme;
            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / half);
                island.swapSink = k;
                island.rimTint = k;
                explorer.Shrink = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / (half * 0.6f)));
                SetLight(fromLight, toLight, k * 0.5f);
                yield return null;
            }
            island.swapSink = 1f;
            explorer.Shrink = 0f;
            double b0 = Time.realtimeSinceStartupAsDouble;
            SwapAtBottom(to, tile, facing, entering);
            island.Prepare();
            double bottomMs = (Time.realtimeSinceStartupAsDouble - b0) * 1000.0;
            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / half);
                island.swapSink = 1f - k;
                SetLight(fromLight, toLight, 0.5f + k * 0.5f);
                // The explorer pops up near the end, after its tile has risen.
                float u = Mathf.Clamp01((t / half - 0.55f) / 0.45f);
                explorer.Shrink = EaseOutBack(u);
                yield return null;
            }
            island.swapSink = 0f;
            explorer.Shrink = 1f;
            SetLight(fromLight, toLight, 1f);
            explorer.Locked = false;
            Busy = false;
            Debug.Log($"[SceneSwap] {(entering ? $"Inside {to.name}" : $"Back outside on {to.name}")} at c{tile.x} r{tile.y}: " +
                      $"{(Time.realtimeSinceStartupAsDouble - t0):0.00} s, swap at the bottom {bottomMs:0.0} ms.");
            Finished?.Invoke(entering);
        }

        /// <summary>The hidden moment: map, zoom, explorer, restoration, map creatures.</summary>
        void SwapAtBottom(WorldMap to, Vector2Int tile, Vector2Int facing, bool entering)
        {
            if (entering)
            {
                _outdoor = island.world;
                _outdoorRestored = RealmState.Restored;
                if (!Inside)
                {
                    ReturnTile = explorer.Tile;
                    ReturnFacing = explorer.Facing;
                }
            }
            island.world = to;
            island.rimTint = 0f;
            island.rimTheme = null;
            window.MapZoom = to.viewZoom;
            explorer.Teleport(tile, facing);
            Inside = entering;
            Interior = entering ? to : null;
            MapCreature.HideAll = entering;
            RealmState.Restored = entering ? 1f : _outdoorRestored;
            if (entering) Entered?.Invoke(to);
            else Returned?.Invoke(to);
        }

        /// <summary>Puts the explorer back outside for a save made inside (before <see cref="EnterNow"/>).</summary>
        public void SetReturn(Vector2Int tile, Vector2Int facing)
        {
            ReturnTile = tile;
            ReturnFacing = facing;
        }

        // ---- light ----

        (Color color, float intensity) LightNow() => sun != null ? (sun.color, sun.intensity) : (Color.white, 1f);

        (Color color, float intensity) LightFor(RealmTheme theme, bool entering)
        {
            if (sun == null) return (Color.white, 1f);
            if (!_lightSaved)
            {
                _lightColor = sun.color;
                _lightIntensity = sun.intensity;
                _lightSaved = true;
            }
            if (entering && theme != null && theme.overrideLight) return (theme.lightColor, theme.lightIntensity);
            return (_lightColor, _lightIntensity);
        }

        void SetLight((Color color, float intensity) a, (Color color, float intensity) b, float k)
        {
            if (sun == null) return;
            sun.color = Color.Lerp(a.color, b.color, k);
            sun.intensity = Mathf.Lerp(a.intensity, b.intensity, k);
        }

        static float EaseOutBack(float t)
        {
            const float c = 1.7f;
            t -= 1f;
            return Mathf.Max(0f, 1f + (c + 1f) * t * t * t + c * t * t);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _instance = null;
    }
}
