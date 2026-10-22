using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Diorama
{
    /// <summary>
    /// Builds the floating island from an <see cref="IslandMap"/> and draws it with GPU instancing:
    /// dirt columns, coloured caps, the hanging underside, trees, rocks and grass blades.
    /// Everything is placed relative to this transform, so moving or scaling it moves the whole island.
    /// Port of the island and grass sections of creature-diorama-v2.html, same seed, same layout.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class IslandBuilder : MonoBehaviour
    {
        [Serializable]
        public struct ToonPart
        {
            public Material body;
            [Tooltip("Optional. Diorama/Toon Outline material.")]
            public Material outline;
        }

        public IslandMap map;
        [Tooltip("Same seed and call order as the prototype gives the same layout.")]
        public int seed = 7;

        [Header("Tiles")]
        public Material column;
        public Material underside;
        public Material capGrass;
        public Material capTallGrass;
        public Material capPath;
        public Material capWater;

        [Header("Props")]
        public ToonPart trunk;
        public ToonPart leavesLow;
        public ToonPart leavesHigh;
        public ToonPart rock;

        [Header("Grass")]
        public Material grass;
        public int bladesPerGrassTile = 7;
        public int bladesPerTallGrassTile = 46;

        /// <summary>Brightens the rocky sides (0..1, lerp to white). Driven by IslandHandle; not saved.</summary>
        [NonSerialized] public float rimHighlight;

        // Fixed sizes from the prototype, in metres.
        const float ColumnDepth = 0.04f;
        const float WaterDrop = 0.008f;
        const int MaxInstancesPerDraw = 1023;

        static readonly int GrassPushId = Shader.PropertyToID("_DioramaGrassPush");
        static readonly int DioramaScaleId = Shader.PropertyToID("_DioramaScale");
        static readonly int FlashId = Shader.PropertyToID("_Flash");
        MaterialPropertyBlock _rimProps;

        sealed class Batch
        {
            public Mesh mesh;
            public Material material;
            public bool rim; // rocky sides; takes the hover highlight
            public readonly List<Matrix4x4> local = new List<Matrix4x4>();
            public Matrix4x4[] world = Array.Empty<Matrix4x4>();
        }

        readonly List<Batch> _batches = new List<Batch>();
        readonly List<Mesh> _meshes = new List<Mesh>();
        readonly HashSet<Material> _warned = new HashSet<Material>();
        Bounds _localBounds;
        Matrix4x4 _lastLocalToWorld;
        bool _needsRebuild = true;
        bool _matricesDirty = true;

        /// <summary>Number of instanced draw calls issued per camera (outlines count separately).</summary>
        public int DrawCallCount
        {
            get
            {
                int n = 0;
                foreach (var b in _batches)
                    if (b.material != null) n += (b.local.Count + MaxInstancesPerDraw - 1) / MaxInstancesPerDraw;
                return n;
            }
        }

        /// <summary>Island extent in local space (before this transform's scale).</summary>
        public Bounds LocalBounds
        {
            get
            {
                if (_needsRebuild) Rebuild();
                return _localBounds;
            }
        }

        public int BladeCount { get; private set; }
        public int TileCount { get; private set; }

        /// <summary>Bends grass away from a world point. Strength 0 turns it off.</summary>
        public static void SetGrassPush(Vector3 worldPoint, float strength) =>
            Shader.SetGlobalVector(GrassPushId, new Vector4(worldPoint.x, worldPoint.y, worldPoint.z, strength));

        void OnEnable()
        {
            _needsRebuild = true;
            SetGrassPush(Vector3.zero, 0f);
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            ReleaseMeshes();
        }

        void OnValidate() => _needsRebuild = true;

        /// <summary>Rebuilds all instances from the map. Call after changing fields from code.</summary>
        public void Rebuild()
        {
            ReleaseMeshes();
            _batches.Clear();
            _needsRebuild = false;
            _matricesDirty = true;
            BladeCount = 0;
            TileCount = 0;
            if (map == null) return;

            var rnd = new SeededRandom(seed);
            float tile = map.tileSize, cap = map.capHeight;
            int rows = map.Rows, cols = map.Columns;
            float cr = (rows - 1) * 0.5f, cc = (cols - 1) * 0.5f;

            var columnMesh = Keep(ProceduralMeshes.Box(new Vector3(tile, ColumnDepth, tile), new Vector3(0f, -ColumnDepth * 0.5f, 0f), "IslandColumn"));
            var capMesh = Keep(ProceduralMeshes.Box(new Vector3(tile * 0.998f, cap, tile * 0.998f), new Vector3(0f, cap * 0.5f, 0f), "IslandCap"));
            var undersideMesh = Keep(ProceduralMeshes.Box(new Vector3(tile * 0.86f, 1f, tile * 0.86f), new Vector3(0f, -0.5f, 0f), "IslandUnderside"));

            var columns = AddBatch(columnMesh, column);
            var undersides = AddBatch(undersideMesh, underside);
            columns.rim = undersides.rim = true;
            var caps = new Dictionary<char, Batch>
            {
                { IslandMap.Grass, AddBatch(capMesh, capGrass) },
                { IslandMap.TallGrass, AddBatch(capMesh, capTallGrass) },
                { IslandMap.Path, AddBatch(capMesh, capPath) },
                { IslandMap.Water, AddBatch(capMesh, capWater) },
            };
            caps[IslandMap.Tree] = caps[IslandMap.Grass];
            caps[IslandMap.Rock] = caps[IslandMap.Grass];

            // ---- tiles ----
            var tiles = new List<(int r, int c, char ch)>();
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    char ch = map.At(r, c);
                    if (ch != IslandMap.Empty && caps.ContainsKey(ch)) tiles.Add((r, c, ch));
                }
            TileCount = tiles.Count;

            foreach (var t in tiles)
            {
                Vector3 p = map.TileCenter(t.r, t.c);
                float y = t.ch == IslandMap.Water ? -WaterDrop : 0f;
                var m = Matrix4x4.Translate(new Vector3(p.x, y, p.z));
                columns.local.Add(m);
                caps[t.ch].local.Add(m);

                // The prototype spends one random number on a per-tile cap shade here. We keep the call
                // so the sequence stays identical; the shade itself comes from _OriginJitter in the shader.
                rnd.Next();

                float dc = Mathf.Sqrt((t.r - cr) * (t.r - cr) + (t.c - cc) * (t.c - cc));
                float depth = Mathf.Max(0.02f, 0.17f - dc * 0.022f + rnd.Next() * 0.035f);
                undersides.local.Add(Matrix4x4.TRS(new Vector3(p.x, -ColumnDepth, p.z), Quaternion.identity, new Vector3(1f, depth, 1f)));
            }

            // ---- trees ----
            var trunkMesh = Keep(ProceduralMeshes.Cylinder(0.004f, 0.005f, 0.026f, 6, 0.013f, "TreeTrunk"));
            var leafLowMesh = Keep(ProceduralMeshes.Cylinder(0f, 0.022f, 0.036f, 7, 0.038f, "TreeLeavesLow"));
            var leafHighMesh = Keep(ProceduralMeshes.Cylinder(0f, 0.015f, 0.03f, 7, 0.058f, "TreeLeavesHigh"));
            var treeBatches = new List<Batch>();
            treeBatches.AddRange(AddPart(trunkMesh, trunk));
            treeBatches.AddRange(AddPart(leafLowMesh, leavesLow));
            treeBatches.AddRange(AddPart(leafHighMesh, leavesHigh));

            foreach (var t in tiles)
            {
                if (t.ch != IslandMap.Tree) continue;
                Vector3 p = map.TileCenter(t.r, t.c);
                float s = 0.9f + rnd.Next() * 0.35f;
                float ry = rnd.Next() * 6.28f;
                var m = Matrix4x4.TRS(new Vector3(p.x, cap, p.z), Quaternion.Euler(0f, -ry * Mathf.Rad2Deg, 0f), Vector3.one * s);
                foreach (var b in treeBatches) b.local.Add(m);
            }

            // ---- rocks: two per rock tile ----
            var rockMesh = Keep(ProceduralMeshes.Icosahedron(0.011f, "Rock"));
            var rockBatches = AddPart(rockMesh, rock);
            foreach (var t in tiles)
            {
                if (t.ch != IslandMap.Rock) continue;
                Vector3 p = map.TileCenter(t.r, t.c);
                for (int k = 0; k < 2; k++)
                {
                    float x = p.x - (rnd.Next() - 0.5f) * 0.025f;
                    float z = p.z + (rnd.Next() - 0.5f) * 0.025f;
                    float rx = rnd.Next(), ry = rnd.Next() * 6f, rz = rnd.Next();
                    float s = 0.6f + rnd.Next() * 0.7f;
                    var m = Matrix4x4.TRS(new Vector3(x, cap + 0.004f, z), MirroredEulerXYZ(rx, ry, rz), new Vector3(s, s * 0.7f, s));
                    foreach (var b in rockBatches) b.local.Add(m);
                }
            }

            // ---- grass blades ----
            var bladeMesh = Keep(ProceduralMeshes.GrassBlade());
            var blades = AddBatch(bladeMesh, grass);
            foreach (var t in tiles)
            {
                bool tall = t.ch == IslandMap.TallGrass;
                int n = tall ? bladesPerTallGrassTile
                    : t.ch == IslandMap.Grass || t.ch == IslandMap.Tree || t.ch == IslandMap.Rock ? bladesPerGrassTile : 0;
                Vector3 p = map.TileCenter(t.r, t.c);
                for (int k = 0; k < n; k++)
                {
                    float x = p.x - (rnd.Next() - 0.5f) * tile * 0.96f;
                    float z = p.z + (rnd.Next() - 0.5f) * tile * 0.96f;
                    float h = tall ? 0.05f + rnd.Next() * 0.035f : 0.014f + rnd.Next() * 0.016f;
                    float w = tall ? 0.0065f + rnd.Next() * 0.002f : 0.005f;
                    float ry = rnd.Next() * Mathf.PI;
                    float tilt = (rnd.Next() - 0.5f) * 0.35f;
                    blades.local.Add(Matrix4x4.TRS(new Vector3(x, cap, z), MirroredEulerXYZ(tilt, ry, 0f), new Vector3(w, h, w)));
                }
            }
            BladeCount = blades.local.Count;

            // Generous local bounds: full grid, deepest underside, tallest tree.
            float halfX = cols * tile * 0.5f + 0.02f, halfZ = rows * tile * 0.5f + 0.02f;
            _localBounds = new Bounds(new Vector3(0f, -0.07f, 0f), new Vector3(halfX * 2f, 0.38f, halfZ * 2f));
        }

        /// <summary>
        /// three.js Euler 'XYZ' rotation (radians) converted to Unity across our X mirror:
        /// the X angle stays, Y and Z flip sign.
        /// </summary>
        static Quaternion MirroredEulerXYZ(float x, float y, float z) =>
            Quaternion.AngleAxis(x * Mathf.Rad2Deg, Vector3.right) *
            Quaternion.AngleAxis(-y * Mathf.Rad2Deg, Vector3.up) *
            Quaternion.AngleAxis(-z * Mathf.Rad2Deg, Vector3.forward);

        Mesh Keep(Mesh mesh)
        {
            _meshes.Add(mesh);
            return mesh;
        }

        Batch AddBatch(Mesh mesh, Material material)
        {
            var b = new Batch { mesh = mesh, material = material };
            _batches.Add(b);
            return b;
        }

        List<Batch> AddPart(Mesh mesh, ToonPart part)
        {
            var list = new List<Batch> { AddBatch(mesh, part.body) };
            if (part.outline != null) list.Add(AddBatch(Keep(ProceduralMeshes.WithSmoothedNormals(mesh)), part.outline));
            return list;
        }

        void ReleaseMeshes()
        {
            foreach (var m in _meshes)
            {
                if (m == null) continue;
                if (Application.isPlaying) Destroy(m);
                else DestroyImmediate(m);
            }
            _meshes.Clear();
        }

        void UpdateWorldMatrices()
        {
            var l2w = transform.localToWorldMatrix;
            if (!_matricesDirty && l2w == _lastLocalToWorld) return;
            _lastLocalToWorld = l2w;
            _matricesDirty = false;
            foreach (var b in _batches)
            {
                if (b.world.Length != b.local.Count) b.world = new Matrix4x4[b.local.Count];
                for (int i = 0; i < b.local.Count; i++) b.world[i] = l2w * b.local[i];
            }
        }

        void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            // Skip material/inspector preview cameras so the island doesn't show up in thumbnails.
            if (cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection) return;
            Draw(cam);
        }

        /// <summary>Queues the island's instanced draws for one camera.</summary>
        public void Draw(Camera cam)
        {
            if (_needsRebuild) Rebuild();
            if (_batches.Count == 0) return;
            UpdateWorldMatrices();
            Shader.SetGlobalFloat(DioramaScaleId, transform.lossyScale.y);

            var bounds = TransformBounds(transform.localToWorldMatrix, _localBounds);
            bool highlight = rimHighlight > 0.001f;
            if (highlight)
            {
                if (_rimProps == null) _rimProps = new MaterialPropertyBlock();
                _rimProps.SetFloat(FlashId, rimHighlight);
            }
            foreach (var b in _batches)
            {
                if (b.material == null || b.world.Length == 0) continue;
                if (!b.material.enableInstancing)
                {
                    if (_warned.Add(b.material)) Debug.LogWarning($"[Diorama] Enable GPU Instancing on material '{b.material.name}'.", b.material);
                    continue;
                }
                var rp = new RenderParams(b.material)
                {
                    camera = cam,
                    layer = gameObject.layer,
                    worldBounds = bounds,
                    shadowCastingMode = ShadowCastingMode.Off,
                    receiveShadows = false,
                    matProps = highlight && b.rim ? _rimProps : null,
                };
                for (int start = 0; start < b.world.Length; start += MaxInstancesPerDraw)
                {
                    int count = Mathf.Min(MaxInstancesPerDraw, b.world.Length - start);
                    Graphics.RenderMeshInstanced(rp, b.mesh, 0, b.world, count, start);
                }
            }
        }

        static Bounds TransformBounds(Matrix4x4 m, Bounds b)
        {
            Vector3 c = m.MultiplyPoint3x4(b.center);
            Vector3 e = b.extents;
            Vector3 ax = m.MultiplyVector(new Vector3(e.x, 0, 0));
            Vector3 ay = m.MultiplyVector(new Vector3(0, e.y, 0));
            Vector3 az = m.MultiplyVector(new Vector3(0, 0, e.z));
            Vector3 ext = new Vector3(
                Mathf.Abs(ax.x) + Mathf.Abs(ay.x) + Mathf.Abs(az.x),
                Mathf.Abs(ax.y) + Mathf.Abs(ay.y) + Mathf.Abs(az.y),
                Mathf.Abs(ax.z) + Mathf.Abs(ay.z) + Mathf.Abs(az.z));
            return new Bounds(c, ext * 2f);
        }
    }
}
