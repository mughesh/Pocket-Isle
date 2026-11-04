using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 14j (Mugesh: the explorer vanished behind Cinderwood's trees): always see the explorer. Each frame it checks
    /// the line from the head to the explorer's chest over the map tiles (<see cref="IslandBuilder.ObstacleTop"/>: trees,
    /// buildings, props). While something stands on that line, island content between the head and the explorer
    /// dissolves in a soft circle about <see cref="radiusTiles"/> x 2 tiles across round the explorer (a dithered cut-out
    /// in DioramaWindow.hlsl, no extra pass), eased in and out over <see cref="fadeSeconds"/>, and the explorer's outline
    /// turns gold. Never cuts at or below its feet. Added by <see cref="ExplorerView"/>; every map and interior.
    /// Step 14k (Mugesh: the cut looked odd): a ghost, not a hole: the props in that circle keep about a third of their
    /// surface on an even screen pattern with a pale edge glow and a pale outline (DioramaWindow.hlsl DioramaGhost*), so
    /// their shape stays. And in tall grass: blades within <see cref="grassRadiusTiles"/> of the explorer on the player's
    /// side lie down (Grass.shader, DioramaGrassSee), always, on the map and in the battle zoom.
    /// </summary>
    [RequireComponent(typeof(CreatureView))]
    public class ExplorerSeeThrough : MonoBehaviour
    {
        [SerializeField] float fadeSeconds = 0.2f;
        [Tooltip("Cut radius round the explorer (tiles): 1 = a circle about 2 tiles across.")]
        [SerializeField] float radiusTiles = 1f;
        [Tooltip("Nothing closer to the explorer than this along the line is cut (tiles): the explorer itself stays whole.")]
        [SerializeField] float keepOutTiles = 0.35f;
        [Tooltip("Nothing lower than the explorer's feet plus this is cut (tiles): the ground stays.")]
        [SerializeField] float feetMarginTiles = 0.12f;
        [Tooltip("How far toward the head the line is checked (tiles).")]
        [SerializeField] float checkTiles = 5f;
        [SerializeField] Color gold = new Color(1f, 0.8f, 0.3f, 1f);
        [Tooltip("Step 14k: grass within this many tiles of the explorer, on the player's side, lies down.")]
        [SerializeField] float grassRadiusTiles = 1.1f;

        static readonly int SeeThroughId = Shader.PropertyToID("_DioramaSeeThrough");
        static readonly int EyeId = Shader.PropertyToID("_DioramaSeeThroughEye");
        static readonly int MiscId = Shader.PropertyToID("_DioramaSeeThroughMisc");
        static readonly int SeenId = Shader.PropertyToID("_DioramaSeen");
        static readonly int SeenColorId = Shader.PropertyToID("_DioramaSeenColor");
        static readonly int GrassSeeId = Shader.PropertyToID("_DioramaGrassSee");
        static readonly int GrassSeeEyeId = Shader.PropertyToID("_DioramaGrassSeeEye");

        CreatureView _view;
        IslandBuilder _island;
        EncounterDirector _encounters;
        float _amount;
        bool _hidden;

        /// <summary>Bridge before/after shots: off.</summary>
        public static bool DebugOff;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => DebugOff = false;

        /// <summary>The check's CPU time last frame (ms), for the perf note.</summary>
        public static float LastMs { get; private set; }

        /// <summary>The explorer is hidden now (what the cut and the gold outline follow).</summary>
        public bool Hidden => _hidden;

        public float Amount => _amount;

        void Awake() => _view = GetComponent<CreatureView>();

        void OnDisable()
        {
            _amount = 0f;
            _hidden = false;
            Shader.SetGlobalVector(SeeThroughId, Vector4.zero);
            Shader.SetGlobalVector(SeenId, Vector4.zero);
            Shader.SetGlobalVector(GrassSeeId, Vector4.zero);
        }

        void LateUpdate()
        {
            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            if (_island == null) _island = FindFirstObjectByType<IslandBuilder>();
            var cam = Camera.main;
            if (_island == null || cam == null || _island.Content == null || _island.map == null)
            {
                OnDisable();
                return;
            }
            var it = _island.transform;
            float cap = _island.Content.capHeight;
            Vector2 at = _island.LocalToTile(it.InverseTransformPoint(transform.position));
            Vector3 origin = _island.TileToLocal(at, cap);
            float tileL = (_island.TileToLocal(at + Vector2.right, cap) - origin).magnitude;
            float perMapMetre = tileL / Mathf.Max(1e-5f, _island.map.tileSize);
            Vector3 feet = it.InverseTransformPoint(transform.position);
            Vector3 chest = feet + Vector3.up * (_view.height * perMapMetre * 0.6f);
            Vector3 eye = it.InverseTransformPoint(cam.transform.position);

            Vector2Int by = default;
            // Step 14k: never in a battle (the near Isling stands between the head and the explorer: it would ghost).
            if (_encounters == null) _encounters = FindFirstObjectByType<EncounterDirector>();
            bool battle = _encounters != null && _encounters.InBattle;
            bool hidden = !DebugOff && !battle && Blocked(chest, eye, tileL, cap, out by);
            if (hidden != _hidden && (Application.isEditor || Debug.isDebugBuild))
                Debug.Log(hidden ? $"[ExplorerSeeThrough] The explorer is hidden (tile {by}): see-through on." : "[ExplorerSeeThrough] The explorer is in view again: see-through off.");
            _hidden = hidden;
            _amount = DebugOff ? 0f : Mathf.MoveTowards(_amount, hidden ? 1f : 0f, Time.deltaTime / Mathf.Max(0.01f, fadeSeconds));

            Shader.SetGlobalVector(SeeThroughId, new Vector4(chest.x, chest.y, chest.z, _amount));
            Shader.SetGlobalVector(EyeId, new Vector4(eye.x, eye.y, eye.z, radiusTiles * tileL));
            Shader.SetGlobalVector(MiscId, new Vector4(feet.y + feetMarginTiles * tileL, keepOutTiles * tileL, 0f, 0f));
            float worldTile = tileL * it.lossyScale.x;
            Shader.SetGlobalVector(SeenId, _amount > 0.001f ? (Vector4)it.TransformPoint(chest) + new Vector4(0f, 0f, 0f, worldTile * 0.6f) : Vector4.zero);
            // Step 14k: the grass round the explorer on the player's side lies down (always, not only when hidden).
            bool grass = !DebugOff && _view.isActiveAndEnabled && !SceneSwap.IsInside;
            Shader.SetGlobalVector(GrassSeeId, grass ? new Vector4(transform.position.x, transform.position.y, transform.position.z, worldTile * grassRadiusTiles) : Vector4.zero);
            Shader.SetGlobalVector(GrassSeeEyeId, cam.transform.position);
            var c = gold;
            c.a = _amount;
            Shader.SetGlobalVector(SeenColorId, c.linear);
            LastMs = (float)((System.Diagnostics.Stopwatch.GetTimestamp() - t0) * 1000.0 / System.Diagnostics.Stopwatch.Frequency);
        }

        /// <summary>Something on the map stands taller than the line from the chest toward the eye (within checkTiles).</summary>
        bool Blocked(Vector3 chest, Vector3 eye, float tileL, float cap, out Vector2Int by)
        {
            by = default;
            Vector3 d = eye - chest;
            float flat = new Vector2(d.x, d.z).magnitude;
            if (flat < 1e-5f || d.y <= 0f) return false;
            float step = tileL * 0.25f, reach = Mathf.Min(flat, checkTiles * tileL);
            for (float s = tileL * 0.6f; s <= reach; s += step) // from just past its own tile
            {
                Vector3 p = chest + d * (s / flat);
                var tile = Vector2Int.RoundToInt(_island.LocalToTile(p));
                float top = _island.ObstacleTop(tile);
                if (top <= 0f) continue;
                float topY = _island.TileToLocal(tile, cap + top).y;
                if (p.y < topY)
                {
                    by = tile;
                    return true;
                }
            }
            return false;
        }
    }
}
