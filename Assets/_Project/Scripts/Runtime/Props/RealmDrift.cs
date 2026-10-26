using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Small drifting things that show the realm's state, drawn as one mesh of 16 x 16 camera-facing quads in one call
    /// with the Diorama/Realm Drift shader (no CPU work per quad, no ParticleSystem). Each slot covers one world tile
    /// around the view centre; the shader hashes the tile, so they scroll with the map.
    /// Embers: rise over trees, tall grass and fire props, more as the realm is restored (the element flows).
    /// Leaves: a few fall from the trees while it is not restored (the element is still), fewer as it is restored.
    /// A weight texture (one texel per world tile) says where; the amount follows <see cref="RealmState.Restored"/>.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class RealmDrift : MonoBehaviour
    {
        public enum Kind { Embers, Leaves, Seeds }

        const int Grid = 16;

        public IslandBuilder island;
        public Kind kind = Kind.Embers;

        [Header("Weights per tile (chance of one)")]
        [Range(0f, 1f)] public float treeWeight = 1f;
        [Range(0f, 1f)] public float tallGrassWeight = 1f;
        [Range(0f, 1f)] public float fireWeight = 1f;
        [Range(0f, 1f)] public float groundWeight = 0.55f;
        [Range(0f, 1f)] public float waterWeight = 0.15f;

        static readonly int GridId = Shader.PropertyToID("_DriftGrid");
        static readonly int MapId = Shader.PropertyToID("_DriftMap");
        static readonly int WeightsId = Shader.PropertyToID("_DriftWeights");

        MeshRenderer _renderer;
        MaterialPropertyBlock _block;
        Texture2D _weights;
        IslandMap _builtFor;
        int _builtVersion = -1;

        /// <summary>0..1 share of the weighted tiles that show one right now.</summary>
        float Amount
        {
            get
            {
                var theme = island != null ? island.Theme : null;
                if (kind == Kind.Embers) return RealmState.Restored * (theme != null ? theme.embers : 1f);
                // Step 10a: seeds drift over restored Galewick (only realms whose theme asks for them).
                if (kind == Kind.Seeds) return RealmState.Restored * (theme != null ? theme.seeds : 0f);
                return (1f - RealmState.Restored) * (theme != null ? theme.leaves : 1f);
            }
        }

        void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();
            _block = new MaterialPropertyBlock();
            GetComponent<MeshFilter>().sharedMesh = BuildMesh();
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
        }

        void OnDestroy()
        {
            var mf = GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null) Destroy(mf.sharedMesh);
            if (_weights != null) Destroy(_weights);
        }

        void LateUpdate()
        {
            // Not indoors (scene swap): embers and leaves belong to the outdoor realm.
            if (island == null || island.map == null || island.Content == null || SceneSwap.IsInside || SceneSwap.IsBusy)
            {
                _renderer.enabled = false;
                return;
            }
            float amount = Amount;
            bool show = amount > 0.001f;
            _renderer.enabled = show;
            if (!show) return;
            var content = island.Content;
            if (content != _builtFor || content.version != _builtVersion) BuildWeights(content);

            Vector2 view = island.ViewCenterTile;
            var origin = new Vector2(Mathf.Floor(view.x) - Grid / 2 + 1, Mathf.Floor(view.y) - Grid / 2 + 1);
            _renderer.GetPropertyBlock(_block);
            _block.SetVector(GridId, new Vector4(origin.x, origin.y, content.Columns, content.Rows));
            _block.SetVector(MapId, new Vector4(island.map.tileSize, content.capHeight, island.transform.lossyScale.x, amount));
            _block.SetTexture(WeightsId, _weights);
            _renderer.SetPropertyBlock(_block);
        }

        /// <summary>Quads at the origin; the shader places them. Bounds are huge so the renderer is never culled.</summary>
        static Mesh BuildMesh()
        {
            int n = Grid * Grid;
            var verts = new Vector3[n * 4];
            var corner = new Vector2[n * 4];
            var slot = new Vector2[n * 4];
            var tris = new int[n * 6];
            Vector2[] c = { new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(-1f, 1f), new Vector2(1f, 1f) };
            for (int i = 0; i < n; i++)
            {
                var s = new Vector2(i % Grid, i / Grid);
                for (int k = 0; k < 4; k++)
                {
                    corner[i * 4 + k] = c[k];
                    slot[i * 4 + k] = s;
                }
                int v = i * 4, t = i * 6;
                tris[t] = v; tris[t + 1] = v + 2; tris[t + 2] = v + 1;
                tris[t + 3] = v + 1; tris[t + 4] = v + 2; tris[t + 5] = v + 3;
            }
            var mesh = new Mesh { name = "Realm Drift" };
            mesh.vertices = verts;
            mesh.uv = corner;
            mesh.uv2 = slot;
            mesh.triangles = tris;
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000f);
            return mesh;
        }

        void BuildWeights(IslandMap content)
        {
            _builtFor = content;
            _builtVersion = content.version;
            int cols = content.Columns, rows = content.Rows;
            if (_weights == null || _weights.width != cols || _weights.height != rows)
            {
                if (_weights != null) Destroy(_weights);
                _weights = new Texture2D(cols, rows, TextureFormat.R8, false, true)
                {
                    name = $"{kind} Weights", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp,
                };
            }
            var w = new float[cols * rows];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    w[r * cols + c] = TileWeight(content.At(r, c));
            // Fire props (kilns, lanterns, braziers) light up their tile and the eight around it.
            int fires = 0;
            if (fireWeight > 0f && content is WorldMap world)
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < cols; c++)
                    {
                        if (!world.TryGetProp(world.PropAt(r, c), out var entry) || entry.recipe == null || !IsFire(entry.recipe.name)) continue;
                        fires++;
                        for (int dr = -1; dr <= 1; dr++)
                            for (int dc = -1; dc <= 1; dc++)
                            {
                                int rr = r + dr, cc = c + dc;
                                if (rr >= 0 && rr < rows && cc >= 0 && cc < cols) w[rr * cols + cc] = Mathf.Max(w[rr * cols + cc], fireWeight);
                            }
                    }
            var px = new Color32[w.Length];
            for (int i = 0; i < w.Length; i++) px[i] = new Color32((byte)Mathf.RoundToInt(w[i] * 255f), 0, 0, 255);
            _weights.SetPixels32(px);
            _weights.Apply(false);

            // Expected count on the island right now at full amount, for the log.
            float sum = 0f;
            var map = island.map;
            Vector2 view = island.ViewCenterTile;
            for (int r = 0; r < map.Rows; r++)
                for (int c = 0; c < map.Columns; c++)
                {
                    if (map.At(r, c) == IslandMap.Empty) continue;
                    int wr = Mathf.FloorToInt(view.y) + r - map.Rows / 2, wc = Mathf.FloorToInt(view.x) + c - map.Columns / 2;
                    if (wr >= 0 && wr < rows && wc >= 0 && wc < cols) sum += w[wr * cols + wc];
                }
            Debug.Log($"[RealmDrift] {kind} weights for {content.name} ({cols}x{rows}, {fires} fire props): about {sum:0} on the island at full amount.");
        }

        float TileWeight(char t) => t switch
        {
            IslandMap.Tree => treeWeight,
            IslandMap.TallGrass => tallGrassWeight,
            IslandMap.Water => waterWeight,
            IslandMap.HotSpring => waterWeight,
            IslandMap.Empty => 0f,
            _ => groundWeight,
        };

        static bool IsFire(string recipe) =>
            recipe.Contains("Kiln") || recipe.Contains("Lantern") || recipe.Contains("Brazier");
    }
}
