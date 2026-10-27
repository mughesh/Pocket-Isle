using System;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Decides which part of the world map the island shows: follows the explorer (with an optional dead zone),
    /// stops at the map borders, and zooms in around a tile for battles. Drives <see cref="IslandBuilder.SetView"/>.
    /// Runs early so the explorer can position itself against this frame's view.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class WorldWindow : MonoBehaviour
    {
        public IslandBuilder island;

        [Tooltip("0 = the explorer stays centred and the map scrolls every step. " +
                 "More = the explorer walks freely this many tiles from the centre before the map scrolls.")]
        [Min(0)] public int deadZoneTiles;

        [Header("Zoom (battle stage)")]
        public float defaultZoom = 3f;
        public float zoomSeconds = 0.6f;

        /// <summary>
        /// The current map's own zoom (interiors: the lodge shows its small room bigger), on top of the battle zoom.
        /// <see cref="IsZoomed"/> ignores it, so the explorer still walks. Setting it re-centres at once.
        /// </summary>
        public float MapZoom
        {
            get => _mapZoom;
            set
            {
                _mapZoom = Mathf.Max(0.1f, value);
                _hasCenter = false;
                _focus = null;
                _easing = false;
                Apply();
            }
        }

        float _mapZoom = 1f;

        /// <summary>True while zoomed in or zooming either way.</summary>
        public bool IsZoomed => _zoomTo > 1.0001f || _zoom > 1.0001f;

        public event Action<Vector2Int> ZoomedIn;
        public event Action ZoomedOut;

        Vector2 _center, _pivot;
        bool _hasCenter;
        Vector2? _focus;
        bool _easing;
        float _zoom = 1f, _zoomFrom = 1f, _zoomTo = 1f, _zoomT = 1f, _zoomDuration;

        IslandMap World => island != null ? island.Content : null;

        /// <summary>
        /// Moves the view to keep <paramref name="explorerTile"/> (column, row; fractions while stepping) in sight.
        /// Applied at once, so call it before positioning anything with <see cref="IslandBuilder.TileToLocal"/>.
        /// </summary>
        public void Follow(Vector2 explorerTile)
        {
            if (island == null || World == null) return;
            // The footprint is an even number of tiles wide, so its centre is a tile corner. Offsetting by half a
            // tile lines map tiles up with the base at rest; the explorer sits half a tile off the exact middle.
            Vector2 target = explorerTile + CenterBias();
            if (!_hasCenter)
            {
                _center = target;
                _hasCenter = true;
            }
            Vector2 wanted;
            if (_focus.HasValue) wanted = ClampToMap(_focus.Value + CenterBias());
            else
            {
                Vector2 d = target - _center;
                float dz = deadZoneTiles;
                wanted = ClampToMap(target - new Vector2(Mathf.Clamp(d.x, -dz, dz), Mathf.Clamp(d.y, -dz, dz)));
            }
            // Moving to or from a battle focus glides; normal following stays locked to the explorer.
            if (_easing)
            {
                _center = Vector2.Lerp(_center, wanted, 1f - Mathf.Exp(-Time.deltaTime * 7f));
                if (!_focus.HasValue && (_center - wanted).sqrMagnitude < 1e-4f) _easing = false;
            }
            else _center = wanted;
            Apply();
        }

        /// <summary>
        /// Centres the view on a map point (battles: the middle of the stage) instead of the explorer; null goes back
        /// to following the explorer. Both directions glide.
        /// </summary>
        public void SetFocus(Vector2? tile)
        {
            _focus = tile;
            _easing = true;
        }

        /// <summary>Step 11d: the view is centred on a focus point (a battle, the lotus garden), not the explorer.</summary>
        public bool HasFocus => _focus.HasValue;

        /// <summary>Where the view centre ends up when focused on <paramref name="tile"/> (clamped to the map).</summary>
        public Vector2 FocusCenter(Vector2 tile) => ClampToMap(tile + CenterBias());

        /// <summary>Scales the map content around <paramref name="tile"/> by <paramref name="factor"/>, eased over <paramref name="seconds"/>.</summary>
        public void ZoomTo(Vector2Int tile, float factor, float seconds) => ZoomTo((Vector2)tile, factor, seconds);

        /// <summary>Same, around any map point (fractions allowed), e.g. the middle of a battle stage.</summary>
        public void ZoomTo(Vector2 pivot, float factor, float seconds)
        {
            _pivot = pivot;
            StartZoom(Mathf.Max(1f, factor), seconds);
            GameAudio.Ui(Diorama.Audio.AudioIds.ZoomIn);
            ZoomedIn?.Invoke(Vector2Int.RoundToInt(pivot));
        }

        public void ZoomTo(Vector2Int tile) => ZoomTo(tile, defaultZoom, zoomSeconds);

        /// <summary>Back to the whole window around the same pivot.</summary>
        public void ZoomOut(float seconds)
        {
            StartZoom(1f, seconds);
            GameAudio.Ui(Diorama.Audio.AudioIds.ZoomOut);
            ZoomedOut?.Invoke();
        }

        public void ZoomOut() => ZoomOut(zoomSeconds);

        void StartZoom(float to, float seconds)
        {
            _zoomFrom = _zoom;
            _zoomTo = to;
            _zoomDuration = Mathf.Max(0.0001f, seconds);
            _zoomT = 0f;
        }

        void Update()
        {
            if (_zoomT >= 1f) return;
            _zoomT = Mathf.Min(1f, _zoomT + Time.deltaTime / _zoomDuration);
            _zoom = Mathf.Lerp(_zoomFrom, _zoomTo, Mathf.SmoothStep(0f, 1f, _zoomT));
            Apply();
        }

        void Apply()
        {
            if (island == null) return;
            if (!_hasCenter) _center = island.CenteredView;
            // A map zoom alone scales around the view centre; a battle zoom keeps its own pivot.
            island.SetView(_center, _zoom > 1.0001f ? _pivot : _center, _zoom * _mapZoom);
        }

        Vector2 CenterBias()
        {
            var fp = island.map;
            return new Vector2(fp.Columns % 2 == 0 ? 0.5f : 0f, fp.Rows % 2 == 0 ? 0.5f : 0f);
        }

        /// <summary>Keeps the whole window inside the map; near a border the explorer walks off-centre instead.</summary>
        Vector2 ClampToMap(Vector2 center)
        {
            var fp = island.map;
            var world = World;
            return new Vector2(ClampAxis(center.x, fp.Columns / _mapZoom, world.Columns), ClampAxis(center.y, fp.Rows / _mapZoom, world.Rows));
        }

        static float ClampAxis(float v, float window, int map)
        {
            float half = (window - 1) * 0.5f;
            float lo = half, hi = map - 1 - half;
            return lo > hi ? (map - 1) * 0.5f : Mathf.Clamp(v, lo, hi);
        }
    }
}
