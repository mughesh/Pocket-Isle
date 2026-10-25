using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// The Forge Hall interior (step 9b placeholder until 9c brings Hearthstag): while it shows, the Keeper's spot is
    /// an empty glowing ring in the middle, the first time in the Spirit says <see cref="firstLine"/> there, and walking
    /// onto a door tile goes back outside (in front of the forge door, facing away: <see cref="ForgeBellows"/>).
    /// Dialogue target: <c>hallKeeper</c>.
    /// </summary>
    public class ForgeHall : MonoBehaviour
    {
        public SceneSwap swap;
        public WorldMap hall;
        public IslandBuilder island;
        public GestureTestPiece explorer;
        [Tooltip("Diorama/Beacon material (the Keeper's ring).")]
        public Material ringMaterial;
        public Vector2[] doorTiles = { new Vector2(4f, 9f), new Vector2(5f, 9f) };
        [Tooltip("The Keeper's spot (tiles, fractions).")]
        public Vector2 keeperSpot = new Vector2(4.5f, 4f);
        [Tooltip("Ring diameter, tiles.")]
        public float ringSize = 2.4f;
        public string firstLine = "cinder.hall.first";

        static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        static readonly int FillId = Shader.PropertyToID("_Fill");
        static readonly int ModeId = Shader.PropertyToID("_Mode");

        Transform _ring;
        MeshRenderer _ringRenderer;
        MaterialPropertyBlock _block;
        Mesh _quad;

        public bool Active => swap != null && hall != null && swap.Inside && swap.Interior == hall;

        void Start()
        {
            if (swap == null || hall == null || island == null || explorer == null)
            {
                Debug.LogError("[ForgeHall] Needs the scene swap, the Forge Hall map, the island and the explorer (Set Up MR_Main (9b puzzles)).", this);
                enabled = false;
                return;
            }
            _block = new MaterialPropertyBlock();
            if (ringMaterial != null)
            {
                _quad = BattleHud.VerticalQuad();
                var go = new GameObject("Forge Hall Keeper Ring");
                go.layer = island.gameObject.layer;
                go.transform.SetParent(island.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = _quad;
                _ringRenderer = go.AddComponent<MeshRenderer>();
                _ringRenderer.sharedMaterial = ringMaterial;
                _ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _ringRenderer.receiveShadows = false;
                _ring = go.transform;
                go.SetActive(false);
            }
            explorer.OnStep += OnStep;
            swap.Finished += OnFinished;
            Dialogue.RegisterTarget("hallKeeper", () => Active ? KeeperWorld(0.4f) : (Vector3?)null, k => { },
                () => 0.5f * ringSize * island.map.tileSize * island.Zoom * island.transform.lossyScale.x);
        }

        void OnDestroy()
        {
            if (explorer != null) explorer.OnStep -= OnStep;
            if (swap != null) swap.Finished -= OnFinished;
            if (_ring != null) Destroy(_ring.gameObject);
            if (_quad != null) Destroy(_quad);
        }

        void OnFinished(bool inside)
        {
            if (!inside || !Active) return;
            Debug.Log("[ForgeHall] Inside the Forge Hall (the Keeper's spot waits for 9c).");
            if (!string.IsNullOrEmpty(firstLine)) Dialogue.Say(firstLine);
        }

        void OnStep(Vector2Int tile)
        {
            if (!Active || SceneSwap.IsBusy) return;
            foreach (var d in doorTiles)
                if (Vector2Int.RoundToInt(d) == tile)
                {
                    Debug.Log("[ForgeHall] Out the door.");
                    swap.Return();
                    return;
                }
        }

        Vector3 KeeperWorld(float height) =>
            island.transform.TransformPoint(island.TileToLocal(keeperSpot, island.Content.capHeight + height * island.map.tileSize));

        void Update()
        {
            if (_ring == null) return;
            bool on = Active;
            _ring.gameObject.SetActive(on);
            if (!on) return;
            _ring.localPosition = island.TileToLocal(keeperSpot, island.Content.capHeight + 0.05f * island.map.tileSize);
            _ring.localRotation = Quaternion.Euler(90f, 0f, 0f);
            _ring.localScale = Vector3.one * (island.map.tileSize * island.Zoom * ringSize);
            _block.Clear();
            _block.SetFloat(ModeId, 1f);
            _block.SetFloat(FillId, 1f);
            _block.SetFloat(IntensityId, 1.1f + 0.35f * Mathf.Sin(Time.time * 1.3f));
            _ringRenderer.SetPropertyBlock(_block);
        }
    }
}
