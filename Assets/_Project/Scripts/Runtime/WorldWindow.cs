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

        /// <summary>True while zoomed in or zooming either way.</summary>
        public bool IsZoomed => _zoomTo > 1.0001f || _zoom > 1.0001f;

        public event Action<Vector2Int> ZoomedIn;
        public event Action ZoomedOut;

        Vector2 _center, _pivot;
        bool _hasCenter;
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
            Vector2 d = target - _center;
            float dz = deadZoneTiles;
            _center = target - new Vector2(Mathf.Clamp(d.x, -dz, dz), Mathf.Clamp(d.y, -dz, dz));
            _center = ClampToMap(_center);
            Apply();
        }

        /// <summary>Scales the map content around <paramref name="tile"/> by <paramref name="factor"/>, eased over <paramref name="seconds"/>.</summary>
        public void ZoomTo(Vector2Int tile, float factor, float seconds) => ZoomTo((Vector2)tile, factor, seconds);

        /// <summary>Same, around any map point (fractions allowed), e.g. the middle of a battle stage.</summary>
        public void ZoomTo(Vector2 pivot, float factor, float seconds)
        {
            _pivot = pivot;
            StartZoom(Mathf.Max(1f, factor), seconds);
            ZoomedIn?.Invoke(Vector2Int.RoundToInt(pivot));
        }

        public void ZoomTo(Vector2Int tile) => ZoomTo(tile, defaultZoom, zoomSeconds);

        /// <summary>Back to the whole window around the same pivot.</summary>
        public void ZoomOut(float seconds)
        {
            StartZoom(1f, seconds);
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
            island.SetView(_center, _pivot, _zoom);
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
            return new Vector2(ClampAxis(center.x, fp.Columns, world.Columns), ClampAxis(center.y, fp.Rows, world.Rows));
        }

        static float ClampAxis(float v, int window, int map)
        {
            float half = (window - 1) * 0.5f;
            float lo = half, hi = map - 1 - half;
            return lo > hi ? (map - 1) * 0.5f : Mathf.Clamp(v, lo, hi);
        }
    }
}
