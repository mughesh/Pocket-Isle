using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>Step 29 D20: soft rings rippling on the water in front of each marked fishing bank, while it's in the window.</summary>
    public class FishSpots : MonoBehaviour
    {
        public IslandBuilder island;
        public BattleFx fx;
        [Tooltip("Seconds between ripples at a spot.")]
        public float every = 1.6f;
        public float rippleSeconds = 1.3f;
        public Color colour = new Color(0.85f, 0.97f, 1f, 1f);

        class Ripple
        {
            public BattleFx.Item item;
            public Vector2 tile;
            public float born;
        }

        readonly List<Ripple> _live = new List<Ripple>();
        readonly Dictionary<Vector2Int, float> _next = new Dictionary<Vector2Int, float>();

        void LateUpdate()
        {
            if (island == null || fx == null) return;
            var world = island.world;
            bool on = world != null && !SceneSwap.IsInside && !SceneSwap.IsBusy && !EncounterDirector.AnyInBattle;
            if (on)
                foreach (var bank in Fishing.Spots(world))
                {
                    var water = Fishing.WaterOf(world, bank);
                    var local = island.TileToLocal(water, island.Content.capHeight);
                    if (!island.FootprintContains(local)) continue;
                    if (_next.TryGetValue(water, out var at) && Time.time < at) continue;
                    _next[water] = Time.time + every;
                    var item = fx.Get(BattleFx.Shape.Ring);
                    if (item == null) continue;
                    item.Set(colour, colour, 0.8f, 1.2f, 0.2f, 0.2f);
                    _live.Add(new Ripple { item = item, tile = water, born = Time.time });
                }
            float tileWorld = island.map != null ? island.map.tileSize * island.Zoom * island.transform.lossyScale.x : 0.03f;
            float toLocal = 1f / Mathf.Max(1e-5f, fx.transform.lossyScale.x);
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                var r = _live[i];
                float u = (Time.time - r.born) / rippleSeconds;
                if (u >= 1f || !on)
                {
                    fx.Release(r.item);
                    _live.RemoveAt(i);
                    continue;
                }
                var local = island.TileToLocal(r.tile, island.Content.capHeight + 0.002f);
                r.item.transform.position = island.transform.TransformPoint(local);
                r.item.transform.rotation = island.transform.rotation * Quaternion.Euler(90f, 0f, 0f);
                r.item.transform.localScale = Vector3.one * tileWorld * Mathf.Lerp(0.3f, 1.1f, u) * toLocal;
                r.item.SetAlpha(0.7f * (1f - u) * Mathf.Clamp01(u * 5f));
            }
        }

        void OnDisable()
        {
            foreach (var r in _live) if (fx != null) fx.Release(r.item);
            _live.Clear();
        }
    }
}
