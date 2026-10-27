using System.Collections;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 11c: the lake temple's look inside. Behind Tidemother the hoarded water is piled up in a glassy dome
    /// (<see cref="domeRecipe"/>) that sways a little while she clutches it. Her calming (<see cref="KeeperHall.HallLit"/>)
    /// lets it go: it slumps and flows out toward the door as a wave with splashes, and stays gone (saved through the
    /// Keeper: calmed = no dome). Doors out are the <see cref="ForgeHall"/> component beside it; in, <see cref="TempleDoor"/>.
    /// </summary>
    public class LakeTempleHall : MonoBehaviour
    {
        public SceneSwap swap;
        public WorldMap hall;
        public IslandBuilder island;
        public BattleFx fx;
        public PropRecipe domeRecipe;
        [Tooltip("The dome's middle on the hall map (tiles) and how big it is drawn (x a tile).")]
        public Vector2 domeAt = new Vector2(4.5f, 1.2f);
        public float domeScale = 1f;
        [Tooltip("Where the released water runs to (tiles): the door.")]
        public Vector2 doorAt = new Vector2(4.5f, 9f);
        [Tooltip("Saved Keeper id: calmed = the water is free.")]
        public string keeperId = "lm.keeper";
        public KeeperHall keeper;
        public float flowSeconds = 2.4f;

        PropView _dome;
        float _held = 1f; // 1 = the dome is there, 0 = gone
        float _flow = -1f;

        public bool Active => swap != null && hall != null && swap.Inside && swap.Interior == hall;
        public float Held => _held;

        void Start()
        {
            if (swap == null || hall == null || island == null || domeRecipe == null)
            {
                Debug.LogError("[LakeTempleHall] Needs the scene swap, the temple hall map, the island and the dome recipe (Set Up MR_Main (11c Tidemother)).", this);
                enabled = false;
                return;
            }
            _dome = new PropView("Lake Temple Water Dome", island, domeRecipe);
            PuzzleState.Restored += ApplyState;
            if (keeper != null) keeper.HallLit += OnHallLit;
            ApplyState();
        }

        void OnDestroy()
        {
            PuzzleState.Restored -= ApplyState;
            if (keeper != null) keeper.HallLit -= OnHallLit;
            _dome?.Destroy();
        }

        void ApplyState()
        {
            StopAllCoroutines();
            _flow = -1f;
            _held = PuzzleState.IsSolved(keeperId) ? 0f : 1f;
        }

        void OnHallLit() => StartCoroutine(Release());

        /// <summary>She lets the water go: the dome slumps and runs out of the hall toward the door, splashing.</summary>
        IEnumerator Release()
        {
            Debug.Log("[LakeTempleHall] Tidemother lets the water go: it flows out of the hall.");
            for (float t = 0f; t < flowSeconds; t += Time.deltaTime)
            {
                _flow = t / flowSeconds;
                _held = 1f - Mathf.SmoothStep(0f, 1f, _flow);
                if (fx != null && Active && Random.value < 0.6f)
                {
                    var p = Vector2.Lerp(domeAt, doorAt, _flow) + new Vector2(Random.Range(-1.5f, 1.5f), Random.Range(-0.4f, 0.4f));
                    float tile = island.map.tileSize * island.Zoom * island.transform.lossyScale.x;
                    var w = island.transform.TransformPoint(island.TileToLocal(p, island.Content.capHeight + 0.1f * island.map.tileSize));
                    fx.Burst(w, 4, new Color(0.7f, 0.93f, 1f, 0.9f), tile * 0.5f, 0.6f, tile * 0.05f, island.transform.up * tile * 0.6f);
                }
                yield return null;
            }
            _held = 0f;
            _flow = -1f;
        }

        void Update()
        {
            if (_dome == null) return;
            bool on = Active && _held > 0.01f;
            // Clutched: a slow uneasy sway; flowing: it slumps low and wide, sliding toward the door.
            float sway = 0.03f * Mathf.Sin(Time.time * 1.1f);
            float slump = _flow >= 0f ? _flow : 0f;
            var at = Vector2.Lerp(domeAt, doorAt, slump * 0.6f);
            var scale = new Vector3(1f + slump * 0.8f + sway, Mathf.Max(0.05f, _held) * (1f - sway), 1f + slump * 0.4f) * domeScale;
            _dome.Place(at, 0f, Quaternion.identity, on, scale);
        }
    }
}
