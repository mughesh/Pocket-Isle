using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 12a: the smart step's preview. The moment a swipe is read, gold footprints appear on the tiles to the stop
    /// tile (a quick ripple outward) and the stop tile gets a soft ring; each footprint fades as the explorer steps onto
    /// it, the ring once it arrives. Walking on (two quick swipes) shows only the next few tiles.
    /// Step 29 (two colours: gold is only "where to go next"): the preview is white / soft cyan chevrons, and at a junction
    /// stop small white chevrons point down every open way (not back) for <see cref="junctionSeconds"/> or until the next swipe.
    /// </summary>
    public class WalkPreview : MonoBehaviour
    {
        public GestureTestPiece explorer;
        public IslandBuilder island;
        [Tooltip("Diorama/GoldMark (Materials/GoldMark).")]
        public Material material;

        [Tooltip("Footprint size, in tiles.")]
        public float footTiles = 0.9f;
        [Tooltip("Stop ring size, in tiles.")]
        public float ringTiles = 1.05f;
        [Tooltip("Seconds a footprint takes to fade once stepped on.")]
        public float fadeSeconds = 0.35f;
        [Tooltip("Seconds between footprints appearing (the ripple outward).")]
        public float appearStagger = 0.04f;
        [Tooltip("Footprints shown ahead while walking on.")]
        [Min(1)] public int walkOnAhead = 6;
        [Range(0f, 3f)] public float intensity = 1.4f;
        [Tooltip("Step 29: seconds the junction chevrons stay after the walk stops at a junction.")]
        public float junctionSeconds = 2f;
        [Tooltip("Step 29: junction chevron size, in tiles.")]
        public float junctionTiles = 0.7f;

        GoldMarks _junction;
        Vector2Int _junctionTile;
        readonly List<Vector2Int> _junctionWays = new List<Vector2Int>();
        float _junctionAt = -99f;
        bool _junctionArmed;

        /// <summary>Junction chevrons drawn last frame (tests, the bridge).</summary>
        public int JunctionShown => _junction != null ? _junction.Shown : 0;

        /// <summary>The open ways from a junction tile, not back the way the walk came (pure, step 29).</summary>
        public static void OpenWays(IslandMap map, Vector2Int tile, Vector2Int arrived, List<Vector2Int> ways)
        {
            ways.Clear();
            foreach (var d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                if (d == -arrived) continue;
                if (SmartStep.RoadRun(map, tile, d) >= SmartStep.GoesOn) ways.Add(d);
            }
        }

        GoldMarks _marks;
        float _planAt;
        readonly float[] _steppedAt = new float[64];
        int _seenIndex;

        /// <summary>Marks drawn last frame (tests, the bridge).</summary>
        public int Shown => _marks != null ? _marks.Shown : 0;

        void Start()
        {
            _marks = new GoldMarks("Walk Footprint", island, material) { walkColours = true };
            _junction = new GoldMarks("Junction Chevron", island, material) { walkColours = true };
            if (explorer != null) explorer.PlanChanged += OnPlan;
        }

        void OnDestroy()
        {
            if (explorer != null) explorer.PlanChanged -= OnPlan;
            if (_marks != null) _marks.Destroy();
            if (_junction != null) _junction.Destroy();
        }

        void OnPlan()
        {
            _junctionAt = -99f; // the next swipe clears the chevrons
            _junctionArmed = explorer != null && explorer.PlanEnd == SmartStep.Junction && explorer.Plan.Count > 0;
            _planAt = Time.time;
            _seenIndex = 0;
            for (int i = 0; i < _steppedAt.Length; i++) _steppedAt[i] = -1f;
        }

        void LateUpdate()
        {
            if (_marks == null || explorer == null) return;
            _marks.intensity = intensity;
            _marks.Begin();
            var plan = explorer.Plan;
            int n = Mathf.Min(plan.Count, _steppedAt.Length);
            for (; _seenIndex < explorer.PlanIndex && _seenIndex < n; _seenIndex++) _steppedAt[_seenIndex] = Time.time;
            int last = explorer.Continuous ? Mathf.Min(n, explorer.PlanIndex + walkOnAhead) : n;
            for (int k = 0; k < last; k++)
            {
                var step = k == 0 ? plan[0] - explorer.PlanFrom : plan[k] - plan[k - 1];
                if (step.sqrMagnitude != 1) step = explorer.Facing;
                float a = Alpha(k);
                if (a <= 0f) continue;
                bool stop = k == n - 1 && !explorer.Continuous;
                _marks.Draw(plan[k], GoldMarks.Chevron, GoldMarks.Yaw(step), footTiles * 0.8f, a * (stop ? 0.8f : 0.9f), n > 1 ? k / (float)(n - 1) : 1f);
                if (stop) _marks.Draw(plan[k], GoldMarks.Ring, 0f, ringTiles, RingAlpha(k));
            }
            _marks.End();
            DrawJunction();
        }

        void DrawJunction()
        {
            if (_junction == null) return;
            _junction.Begin();
            var plan = explorer.Plan;
            if (_junctionArmed && plan.Count > 0 && explorer.PlanIndex >= plan.Count && explorer.Tile == plan[plan.Count - 1] && explorer.IsStill)
            {
                _junctionArmed = false;
                _junctionTile = explorer.Tile;
                var arrived = plan.Count > 1 ? plan[plan.Count - 1] - plan[plan.Count - 2] : plan[0] - explorer.PlanFrom;
                OpenWays(island.world, _junctionTile, arrived, _junctionWays);
                _junctionAt = Time.time;
                Debug.Log($"[WalkPreview] Junction at c{_junctionTile.x} r{_junctionTile.y}: {_junctionWays.Count} way(s) open.");
            }
            float age = Time.time - _junctionAt;
            if (age < junctionSeconds && explorer.Tile == _junctionTile)
            {
                float a = Mathf.Clamp01(age / 0.15f) * Mathf.Clamp01((junctionSeconds - age) / 0.5f);
                foreach (var w in _junctionWays)
                    _junction.Draw(_junctionTile + w, GoldMarks.Chevron, GoldMarks.Yaw(w), junctionTiles, a);
            }
            _junction.End();
        }

        float Alpha(int k)
        {
            float appear = Mathf.Clamp01((Time.time - _planAt - k * appearStagger) / 0.12f);
            float s = _steppedAt[k];
            if (s < 0f) return appear;
            // Stepped onto: fade from the moment its step began (it is gone as the explorer lands).
            return appear * Mathf.Clamp01(1f - (Time.time - s) / fadeSeconds);
        }

        float RingAlpha(int k)
        {
            float appear = Mathf.Clamp01((Time.time - _planAt - k * appearStagger) / 0.12f);
            float s = _steppedAt[k];
            if (s < 0f) return appear;
            // Stays through the last step and a beat after arriving.
            return appear * Mathf.Clamp01(1f - (Time.time - s - explorer.smartSeconds - 0.3f) / 0.4f);
        }
    }
}
