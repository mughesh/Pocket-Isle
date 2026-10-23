using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Stands a creature on a world map tile, shown through the island window: it scrolls with the map, scales with
    /// the zoom and sinks / clips at the rim like the tiles. Now and then it hops to a free walkable tile near home,
    /// then turns back to face the player.
    /// Lives on a child of the island; positions itself in LateUpdate, after the explorer has moved the view.
    /// </summary>
    [RequireComponent(typeof(CreatureView))]
    public class MapCreature : MonoBehaviour
    {
        public IslandBuilder island;
        [Tooltip("Optional: keeps clear of the explorer's tile.")]
        public GestureTestPiece explorer;
        public Vector2Int homeTile;

        [Header("Idle hops")]
        public bool wander = true;
        [Tooltip("Stays within this many tiles of home.")]
        [Min(0)] public int wanderRadius = 2;
        [Tooltip("Seconds between hops (random in range).")]
        public Vector2 hopInterval = new Vector2(3f, 7f);
        [Tooltip("Chance a hop goes to the next tile instead of staying put.")]
        [Range(0f, 1f)] public float moveChance = 0.6f;

        static readonly List<MapCreature> All = new List<MapCreature>();
        static readonly Vector2Int[] Steps = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        CreatureView _view;
        Vector2Int _tile, _from;
        float _moveStart = -1f;
        float _nextHop;
        float _yaw, _yawTarget;

        public Vector2Int Tile => _tile;

        /// <summary>Hides every map creature (battles: only the two fighters stand on the stage).</summary>
        public static bool HideAll { get; set; }

        void Awake()
        {
            _view = GetComponent<CreatureView>();
            _tile = _from = homeTile;
        }

        void OnEnable()
        {
            All.Add(this);
            _nextHop = Time.time + Random.Range(hopInterval.x, hopInterval.y);
        }

        void OnDisable() => All.Remove(this);

        void Start()
        {
            if (island == null) Debug.LogError($"[MapCreature] {name}: no island assigned.", this);
            _view.SetWindowClip(true);
        }

        void Update()
        {
            if (!wander || island == null || _view.IsFainted) return;
            if (Time.time >= _nextHop && !_view.IsHopping)
            {
                _nextHop = Time.time + Random.Range(hopInterval.x, hopInterval.y);
                if (Random.value < moveChance && TryPickStep(out var next))
                {
                    _from = _tile;
                    _tile = next;
                    _moveStart = Time.time;
                    Vector2Int d = next - _from;
                    _yawTarget = Mathf.Atan2(-d.x, d.y) * Mathf.Rad2Deg; // map columns run toward -X, rows toward +Z
                }
                _view.Hop();
            }
            // Face the player (+Z) again a moment after landing.
            if (!_view.IsHopping && _moveStart < 0f) _yawTarget = 0f;
        }

        bool TryPickStep(out Vector2Int next)
        {
            var map = island.Content;
            int start = Random.Range(0, Steps.Length);
            for (int i = 0; i < Steps.Length; i++)
            {
                next = _tile + Steps[(start + i) % Steps.Length];
                if (Mathf.Abs(next.x - homeTile.x) > wanderRadius || Mathf.Abs(next.y - homeTile.y) > wanderRadius) continue;
                if (!IslandMap.IsWalkable(map.At(next.y, next.x)) || Occupied(next)) continue;
                return true;
            }
            next = _tile;
            return false;
        }

        bool Occupied(Vector2Int tile)
        {
            if (explorer != null && (explorer.Tile - tile).sqrMagnitude <= 1) return true;
            foreach (var c in All)
                if (c != this && (c._tile == tile || c._from == tile)) return true;
            return false;
        }

        void LateUpdate()
        {
            if (_moveStart >= 0f && Time.time - _moveStart >= _view.hopSeconds) _moveStart = -1f;
            _yaw = Mathf.LerpAngle(_yaw, _yawTarget, 1f - Mathf.Exp(-Time.deltaTime * 8f));
            SnapToMap();
        }

        /// <summary>
        /// Places the creature on its tile for the island's current view (also usable in edit mode, where it stands on
        /// the home tile). Hides it while it is out of the window, so it costs no draw calls there.
        /// </summary>
        public void SnapToMap()
        {
            if (island == null || island.map == null) return;
            if (_view == null) _view = GetComponent<CreatureView>();
            Vector2 p = Application.isPlaying ? _tile : homeTile;
            if (Application.isPlaying && _moveStart >= 0f)
            {
                float u = (Time.time - _moveStart) / Mathf.Max(0.05f, _view.hopSeconds);
                p = Vector2.Lerp(_from, _tile, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u)));
            }
            Vector3 local = island.TileToLocal(p, island.Content.capHeight);
            transform.localPosition = local;
            transform.localRotation = Quaternion.Euler(0f, _yaw, 0f);
            transform.localScale = Vector3.one * island.Zoom;

            // Past the rim by more than half a tile it is fully sunk and clipped anyway.
            var b = island.LocalBounds;
            float margin = island.map.tileSize * 0.5f;
            bool inside = local.x > b.min.x - margin && local.x < b.max.x + margin &&
                          local.z > b.min.z - margin && local.z < b.max.z + margin;
            bool show = inside && !(Application.isPlaying && HideAll);
            if (_view.rig != null && _view.rig.gameObject.activeSelf != show) _view.rig.gameObject.SetActive(show);
        }
    }
}
