using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// A stored Isling living in the Hearth Lodge (its baked prefab, outlines on): wanders tile to tile with slow hops,
    /// idles, now and then dozes by the hearth or on a cushion, and hops onto the pedestal when inspected. Stands on the
    /// lodge map like a map creature (scrolls, scales with the zoom, sinks with the scene swap). Driven by
    /// <see cref="LodgeRoom"/>.
    /// </summary>
    [RequireComponent(typeof(CreatureView))]
    public class LodgeIsling : MonoBehaviour
    {
        public IslandBuilder island;
        public IslingInstance Isling { get; private set; }
        public CreatureView View { get; private set; }
        public Vector2Int Tile => _tile;
        public bool OnPedestal { get; private set; }
        /// <summary>0..1 size (pops in and out).</summary>
        public float Grow { get; set; }
        /// <summary>Hidden while it is curled into a pod in a hand.</summary>
        public bool Hidden { get; set; }
        /// <summary>Looked at: it stays put (no new hops), so the gaze doesn't lose it.</summary>
        public bool Noticed { get; set; }

        static readonly Vector2Int[] Steps = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        LodgeRoom _room;
        Vector2Int _tile, _from;
        float _moveStart = -1f, _nextHop, _yaw, _yawTarget, _sleepUntil = -1f, _lift, _liftFrom, _liftTo;
        float _growTarget = 1f;
        bool _leaving;

        public bool Asleep => _sleepUntil > Time.time;
        public bool Leaving => _leaving;

        public void Init(LodgeRoom room, IslingInstance creature, Vector2Int tile)
        {
            _room = room;
            Isling = creature;
            View = GetComponent<CreatureView>();
            View.lookAtPlayer = true;
            _tile = _from = tile;
            _yaw = _yawTarget = Random.Range(-40f, 40f);
            _nextHop = Time.time + Random.Range(1f, 4f);
            Grow = 0f;
        }

        void Start() => View.SetWindowClip(true);

        /// <summary>Shrinks away and is destroyed (into a pod, or the lodge closes).</summary>
        public void Leave()
        {
            _leaving = true;
            _growTarget = 0f;
        }

        /// <summary>Hops onto the pedestal tile (standing on its top) or back down to <paramref name="down"/>.</summary>
        public void ToPedestal(Vector2Int pedestal, float topLift)
        {
            Wake();
            MoveTo(pedestal, topLift);
            OnPedestal = true;
            _yawTarget = 0f;
        }

        public void OffPedestal(Vector2Int down)
        {
            MoveTo(down, 0f);
            OnPedestal = false;
            _nextHop = Time.time + Random.Range(2f, 4f);
        }

        /// <summary>Puts it on a tile at once (a pod dropped on the floor) and pops it in.</summary>
        public void PopAt(Vector2Int tile)
        {
            _tile = _from = tile;
            _moveStart = -1f;
            Grow = 0f;
            Hidden = false;
            OnPedestal = false;
            _lift = _liftTo = 0f;
            View.Hop();
        }

        void MoveTo(Vector2Int tile, float lift)
        {
            _from = _tile;
            _tile = tile;
            _moveStart = Time.time;
            _liftFrom = _lift;
            _liftTo = lift;
            Vector2Int d = tile - _from;
            if (d != Vector2Int.zero) _yawTarget = Mathf.Atan2(-d.x, d.y) * Mathf.Rad2Deg;
            View.Hop();
        }

        void Wake()
        {
            if (_sleepUntil < 0f) return;
            _sleepUntil = -1f;
            if (View.IsDazed) View.Revive();
        }

        void Update()
        {
            Grow = Mathf.MoveTowards(Grow, Hidden ? 0f : _growTarget, Time.deltaTime / (_growTarget > 0f ? 0.3f : 0.25f));
            if (_leaving)
            {
                if (Grow <= 0.001f) Destroy(gameObject);
                return;
            }
            if (_sleepUntil >= 0f && Time.time >= _sleepUntil) Wake();
            if (OnPedestal || Hidden || Asleep || View.IsHopping || _room == null) return;
            if (Noticed) _nextHop = Mathf.Max(_nextHop, Time.time + 1f);
            if (Time.time < _nextHop) return;
            _nextHop = Time.time + Random.Range(2.5f, 6f);
            float roll = Random.value;
            if (roll < 0.15f && _room.IsCozy(_tile))
            {
                // Doze off by the hearth or on a cushion.
                _sleepUntil = Time.time + Random.Range(8f, 16f);
                View.Daze();
                return;
            }
            if (roll < 0.75f && TryPickStep(out var next)) MoveTo(next, 0f);
            else
            {
                _yawTarget = Random.Range(-35f, 35f);
                View.Hop();
            }
        }

        bool TryPickStep(out Vector2Int next)
        {
            // Drift toward the hearth a little more often than away from it.
            var preferred = new List<Vector2Int>(Steps);
            if (Random.value < 0.35f) preferred.Insert(0, Vector2Int.down);
            int start = Random.Range(0, preferred.Count);
            for (int i = 0; i < preferred.Count; i++)
            {
                next = _tile + preferred[(start + i) % preferred.Count];
                if (_room.CanStand(next, this)) return true;
            }
            next = _tile;
            return false;
        }

        /// <summary>The tile it is on or hopping to (others keep off both).</summary>
        public bool Covers(Vector2Int tile) => _tile == tile || _moveStart >= 0f && _from == tile;

        void LateUpdate()
        {
            if (island == null || island.Content == null) return;
            float u = 1f;
            if (_moveStart >= 0f)
            {
                u = Mathf.Clamp01((Time.time - _moveStart) / Mathf.Max(0.05f, View.hopSeconds));
                if (u >= 1f) _moveStart = -1f;
            }
            Vector2 p = Vector2.Lerp(_from, _tile, Mathf.SmoothStep(0f, 1f, u));
            _lift = Mathf.Lerp(_liftFrom, _liftTo, Mathf.SmoothStep(0f, 1f, u));
            _yaw = Mathf.LerpAngle(_yaw, _yawTarget, 1f - Mathf.Exp(-Time.deltaTime * 6f));
            transform.localPosition = island.TileToLocal(p, island.Content.capHeight + _lift);
            transform.localRotation = Quaternion.Euler(0f, _yaw, 0f);
            transform.localScale = Vector3.one * (island.Zoom * Mathf.Max(0.0001f, EaseOutBack(Grow)));
        }

        /// <summary>Island-local body centre (gaze, poke, the grab pod).</summary>
        public Vector3 LocalCenter => transform.localPosition + Vector3.up * (View.centerHeight * transform.localScale.y);

        static float EaseOutBack(float t)
        {
            if (t <= 0f) return 0f;
            const float c = 1.7f;
            t -= 1f;
            return 1f + (c + 1f) * t * t * t + c * t * t;
        }
    }
}
