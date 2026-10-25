using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Diorama
{
    /// <summary>
    /// Draws the floating island with GPU instancing, in two layers:
    /// - Footprint: the rocky base (dirt columns and hanging underside) in the shape of <see cref="map"/>. Fixed to
    ///   this transform; it never scrolls.
    /// - Content: caps, trees, rocks, grass and props of <see cref="world"/> (or of <see cref="map"/> when there is no
    ///   world), seen through the footprint like a window. Props come from the world's props layer (baked PropRecipes,
    ///   one instanced draw per prop type, outline in the same material); a world's <see cref="RealmTheme"/> recolours
    ///   everything by the realm's restoration (<see cref="RealmState"/>). <see cref="SetView"/> picks which part of the map shows and how
    ///   zoomed in it is. Content near the window edge sinks into the base and anything outside the footprint is
    ///   clipped in the shaders (DioramaWindow.hlsl).
    /// Only tiles inside the window are drawn; instance buffers are reused, so scrolling doesn't allocate.
    /// Each content map's instances are built once and kept (<see cref="Prepare"/>), so a scene swap back to a map seen
    /// before (<see cref="SceneSwap"/>: the outdoor map after the lodge) costs nothing; <see cref="swapSink"/> sinks the
    /// content front to back during the swap and <see cref="rimTheme"/> tints the rocky base toward the next map.
    /// Without a world it shows the island map centred, which is the Step 1 prototype layout (same seed, same order).
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

        [Tooltip("Island footprint (window shape). Also the content when no world is set.")]
        public IslandMap map;
        [Tooltip("Optional bigger map shown through the window. Empty = show the island map itself.")]
        public WorldMap world;
        [Tooltip("Same seed and call order as the prototype gives the same layout.")]
        public int seed = 7;

        [Header("Tiles")]
        public Material column;
        public Material underside;
        public Material capGrass;
        public Material capTallGrass;
        public Material capPath;
        public Material capWater;
        [Tooltip("Hot spring caps (H). Diorama/Toon.")]
        public Material capHotSpring;

        [Header("Trees and rocks")]
        public ToonPart trunk;
        public ToonPart leavesLow;
        public ToonPart leavesHigh;
        public ToonPart rock;

        [Header("Grass")]
        public Material grass;
        public int bladesPerGrassTile = 7;
        public int bladesPerTallGrassTile = 46;

        [Header("Map props")]
        [Tooltip("Diorama/Prop material, shared by every prop type (palettes go in property blocks).")]
        public Material propMaterial;
        [Tooltip("Steam puffs drawn over hot spring tiles.")]
        public PropRecipe hotSpringSteam;

        /// <summary>Brightens the rocky sides (0..1, lerp to white). Driven by IslandHandle; not saved.</summary>
        [NonSerialized] public float rimHighlight;

        /// <summary>Battle clearing fade (0 = off, 1 = grass parted, props sunk). See <see cref="SetClearing"/>.</summary>
        [NonSerialized] public float clearingStrength;
        /// <summary>How much content outside the clearing darkens (0..1), usually faded with the zoom.</summary>
        [NonSerialized] public float clearingDim;

        /// <summary>Scene swap: 0 = content shown, 1 = all content sunk into the base (front first). Driven by SceneSwap.</summary>
        [NonSerialized] public float swapSink;
        /// <summary>The swap ripples out from the centre instead of front to back (the opening's unfold, a returning player's rise).</summary>
        [NonSerialized] public bool swapRadial;
        /// <summary>Scene swap: the rocky base blends from the content's theme toward this one by <see cref="rimTint"/>.</summary>
        [NonSerialized] public RealmTheme rimTheme;
        [NonSerialized] public float rimTint;

        // Fixed sizes from the prototype, in metres. The cap mesh reaches down to the column top (-CapSkirt) so
        // water, which sits CapSkirt lower, still shows above the column.
        const float ColumnDepth = 0.04f;
        const float CapSkirt = 0.005f;
        const float WaterDrop = 0.005f;
        // Content this far below the cap is out of sight (tallest tree is about 0.11 m).
        const float SinkDepth = 0.12f;
        const int MaxInstancesPerDraw = 1023;

        // Footprint edge distance texture: texels per tile, padding tiles around the footprint, encoded range (tiles).
        const int EdgeTexelsPerTile = 8;
        const int EdgePadTiles = 3;
        const float EdgeRange = 4f;

        static readonly int GrassPushId = Shader.PropertyToID("_DioramaGrassPush");
        static readonly int DioramaScaleId = Shader.PropertyToID("_DioramaScale");
        static readonly int FlashId = Shader.PropertyToID("_Flash");
        static readonly int WindowClipId = Shader.PropertyToID("_WindowClip");
        static readonly int WorldToIslandId = Shader.PropertyToID("_DioramaWorldToIsland");
        static readonly int IslandToWorldId = Shader.PropertyToID("_DioramaIslandToWorld");
        static readonly int WorldToMapId = Shader.PropertyToID("_DioramaWorldToMap");
        static readonly int MapToWorldId = Shader.PropertyToID("_DioramaMapToWorld");
        static readonly int FootprintId = Shader.PropertyToID("_DioramaFootprint");
        static readonly int FootprintUvId = Shader.PropertyToID("_DioramaFootprintUV");
        static readonly int WindowId = Shader.PropertyToID("_DioramaWindow");
        static readonly int ClearingId = Shader.PropertyToID("_DioramaClearing");
        static readonly int ClearingSizeId = Shader.PropertyToID("_DioramaClearingSize");
        static readonly int ClearingViewId = Shader.PropertyToID("_DioramaClearingView");
        static readonly int ClearingWorldId = Shader.PropertyToID("_DioramaClearingWorld");
        static readonly int SwapId = Shader.PropertyToID("_DioramaSwap");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        static readonly int EmissiveId = Shader.PropertyToID("_Emissive");
        static readonly int ClearColorId = Shader.PropertyToID("_ClearColor");
        static readonly int ShortBaseId = Shader.PropertyToID("_ShortBase");
        static readonly int ShortTipId = Shader.PropertyToID("_ShortTip");
        static readonly int TallBaseId = Shader.PropertyToID("_TallBase");
        static readonly int TallTipId = Shader.PropertyToID("_TallTip");

        MaterialPropertyBlock _flashProps;

        /// <summary>Which theme colour a batch takes (None = the material's own).</summary>
        enum ThemeRole { None, Grass, TallGrass, Path, Water, HotSpring, Dirt, Underside, Trunk, LeavesLow, LeavesHigh, Rock, Blades }

        /// <summary>Fixed to the island (footprint) or part of the scrolling map (content, grouped by map cell).</summary>
        sealed class Batch
        {
            public Mesh mesh;
            public Material material;
            public bool rim;      // rocky sides; takes the hover highlight
            public bool content;  // map content; local matrices are map space, cellStart groups them by cell
            public ThemeRole role;
            public bool outline;  // Toon Outline material: the theme sets its outline colour
            public int passes = 1; // draw calls per chunk (props draw their outline pass from the same material)
            public Color color;   // current theme colour (caps: the tile flash uses it)
            public MaterialPropertyBlock props;
            public readonly List<Matrix4x4> local = new List<Matrix4x4>();
            public int[] cellStart;
            public Matrix4x4[] world = Array.Empty<Matrix4x4>();
            public int count;
        }

        /// <summary>Everything built for one content map (kept while switching maps).</summary>
        sealed class Built
        {
            public IslandMap content;
            public int version = -1;
            public PropRecipe tree, treeAlt;
            public readonly List<Batch> batches = new List<Batch>();
            public readonly List<Mesh> meshes = new List<Mesh>();
            public readonly Dictionary<char, Batch> caps = new Dictionary<char, Batch>();
            public Mesh capMesh;
            public int tiles, blades;
        }

        readonly List<Built> _cache = new List<Built>();
        Built _built = new Built();
        List<Batch> _batches => _built.batches;
        List<Mesh> _meshes => _built.meshes;
        Dictionary<char, Batch> _capBatches => _built.caps;
        readonly HashSet<Material> _warned = new HashSet<Material>();
        Bounds _localBounds;
        Matrix4x4 _lastLocalToWorld, _lastContentToWorld;
        bool _needsRebuild = true;
        bool _matricesDirty = true;
        Texture2D _edgeTexture;
        Vector4 _edgeUv;
        Mesh _capMesh
        {
            get => _built.capMesh;
            set => _built.capMesh = value;
        }

        // View: map tile (column, row) at the island centre, zoom pivot tile and zoom factor.
        Vector2 _viewCenter, _zoomPivot;
        float _zoom = 1f;
        bool _hasView;

        // Battle clearing in map tiles: centre, unit axis, radii (along, across), view corridor end and half width.
        Vector2 _clearCenter, _clearAxis = Vector2.up, _clearRadii, _clearView;
        float _clearViewWidth;

        // Tile flash (tall grass step).
        readonly Matrix4x4[] _flashMatrix = new Matrix4x4[1];
        Vector2Int _flashTile;
        Batch _flashBatch;
        float _flashStart = -1f, _flashSeconds;

        /// <summary>The map shown through the window: the world if set, else the island map.</summary>
        public IslandMap Content => world != null ? world : map;

        /// <summary>The world's realm theme, if any.</summary>
        public RealmTheme Theme => world != null ? world.theme : null;

        PropRecipe ThemeTree
        {
            get
            {
                var t = Theme;
                return t != null && t.treeProp != null && t.treeProp.bakedMesh != null ? t.treeProp : null;
            }
        }

        PropRecipe ThemeTreeAlt
        {
            get
            {
                var t = Theme;
                return ThemeTree != null && t.treePropAlt != null && t.treePropAlt.bakedMesh != null && t.treeAltShare > 0f ? t.treePropAlt : null;
            }
        }

        /// <summary>Marks every island for a rebuild (after props are baked).</summary>
        public static void RebuildAll()
        {
            foreach (var b in FindObjectsByType<IslandBuilder>(FindObjectsInactive.Include, FindObjectsSortMode.None)) b._needsRebuild = true;
        }

        /// <summary>Current zoom factor (1 = the whole window, 3 = battle close-up).</summary>
        public float Zoom => _zoom;

        /// <summary>Number of instanced draw calls issued per camera last frame (outline passes count separately).</summary>
        public int DrawCallCount
        {
            get
            {
                int n = 0;
                foreach (var b in _batches)
                    if (b.material != null) n += (InstanceCount(b) + MaxInstancesPerDraw - 1) / MaxInstancesPerDraw * b.passes;
                return n;
            }
        }

        /// <summary>Instances drawn per camera last frame, all batches.</summary>
        public int InstanceTotal
        {
            get
            {
                int n = 0;
                foreach (var b in _batches) if (b.material != null) n += InstanceCount(b);
                return n;
            }
        }

        static int InstanceCount(Batch b) => b.content ? b.count : b.local.Count;

        /// <summary>Island footprint extent in local space (before this transform's scale).</summary>
        public Bounds LocalBounds
        {
            get
            {
                if (_needsRebuild) Rebuild();
                return _localBounds;
            }
        }

        /// <summary>Half the tile footprint (island-local x, z), centred on the origin: the base's real edge (LocalBounds is padded).</summary>
        public Vector2 FootprintHalfSize => map != null ? new Vector2(map.Columns * map.tileSize * 0.5f, map.Rows * map.tileSize * 0.5f) : Vector2.zero;

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

        // ---- view ----

        /// <summary>
        /// Which part of the content shows. <paramref name="center"/> is the map tile (column, row; fractions slide)
        /// at the island centre; <paramref name="zoom"/> scales the content around <paramref name="pivot"/>, which
        /// keeps its place in the window.
        /// </summary>
        public void SetView(Vector2 center, Vector2 pivot, float zoom)
        {
            _viewCenter = center;
            _zoomPivot = pivot;
            _zoom = Mathf.Max(0.01f, zoom);
            _hasView = true;
        }

        /// <summary>The view that shows the whole content map centred (no scrolling).</summary>
        public Vector2 CenteredView
        {
            get
            {
                var c = Content;
                return c == null ? Vector2.zero : new Vector2((c.Columns - 1) * 0.5f, (c.Rows - 1) * 0.5f);
            }
        }

        Vector2 ViewCenter => _hasView ? _viewCenter : CenteredView;

        /// <summary>Map tile (fractions) at the middle of the island window right now.</summary>
        public Vector2 ViewCenterTile => ViewCenter;
        Vector2 ZoomPivot => _hasView ? _zoomPivot : CenteredView;

        float TileSize => map != null ? map.tileSize : 0.05f;

        /// <summary>Map space (metres) of a tile position: columns run toward -X, rows toward +Z (see IslandMap).</summary>
        Vector3 MapPoint(Vector2 tile) => new Vector3(-tile.x * TileSize, 0f, tile.y * TileSize);

        /// <summary>Map space (metres) to island-local space for the current view.</summary>
        public Matrix4x4 ContentToLocal
        {
            get
            {
                Vector3 pivot = MapPoint(ZoomPivot);
                Vector3 center = MapPoint(ViewCenter);
                return Matrix4x4.TRS(pivot * (1f - _zoom) - center, Quaternion.identity, Vector3.one * _zoom);
            }
        }

        /// <summary>Island-local position of a map tile (fractions allowed), at <paramref name="height"/> above the cap bottom, in map metres.</summary>
        public Vector3 TileToLocal(Vector2 tile, float height = 0f) =>
            ContentToLocal.MultiplyPoint3x4(MapPoint(tile) + Vector3.up * height);

        /// <summary>
        /// Shapes the battle clearing, in map tiles: an ellipse around <paramref name="center"/> stretched along
        /// <paramref name="axis"/>, where grass parts and props sink, plus a corridor to <paramref name="viewEnd"/>
        /// (toward the player) where props also sink. Fade it with <see cref="clearingStrength"/> / <see cref="clearingDim"/>.
        /// </summary>
        public void SetClearing(Vector2 center, Vector2 axis, float radiusAlong, float radiusAcross, Vector2 viewEnd, float viewHalfWidth)
        {
            _clearCenter = center;
            _clearAxis = axis.sqrMagnitude > 1e-6f ? axis.normalized : Vector2.up;
            _clearRadii = new Vector2(radiusAlong, radiusAcross);
            _clearView = viewEnd;
            _clearViewWidth = viewHalfWidth;
        }

        /// <summary>Map tile (fractions) under an island-local point, for the current view.</summary>
        public Vector2 LocalToTile(Vector3 local)
        {
            Vector3 m = ContentToLocal.inverse.MultiplyPoint3x4(local);
            return new Vector2(-m.x / TileSize, m.z / TileSize);
        }

        /// <summary>True when an island-local point lies over a footprint tile (x and z only).</summary>
        public bool FootprintContains(Vector3 local)
        {
            if (map == null) return false;
            float tile = map.tileSize;
            int c = Mathf.RoundToInt((map.Columns - 1) * 0.5f - local.x / tile);
            int r = Mathf.RoundToInt((map.Rows - 1) * 0.5f + local.z / tile);
            return map.At(r, c) != IslandMap.Empty;
        }

        /// <summary>Island-local height of the cap tops for the current view.</summary>
        public float CapTopLocal => TileToLocal(Vector2.zero, Content != null ? Content.capHeight : 0f).y;

        /// <summary>Flashes the cap of one map tile for a moment.</summary>
        public void FlashTile(Vector2Int tile, float seconds = 0.5f)
        {
            var content = Content;
            if (content == null || !_capBatches.TryGetValue(content.At(tile.y, tile.x), out _flashBatch)) return;
            _flashTile = tile;
            _flashSeconds = Mathf.Max(0.05f, seconds);
            _flashStart = Time.time;
        }

        // ---- build ----

        /// <summary>Rebuilds all instances from the maps. Call after changing fields from code.</summary>
        public void Rebuild()
        {
            ReleaseCache();
            _needsRebuild = false;
            if (_edgeTexture != null) ReleaseObject(_edgeTexture);
            _edgeTexture = null;
            BuildCurrent();
        }

        /// <summary>
        /// Makes the current content ready to draw now: reuses its kept build when it is still valid, builds it otherwise.
        /// Called by every draw; SceneSwap calls it at the bottom of a swap so nothing is built while content rises.
        /// </summary>
        public void Prepare()
        {
            if (_needsRebuild)
            {
                Rebuild();
                return;
            }
            var content = Content;
            if (content == null) return;
            if (_built.content == content && _built.version == content.version && _built.tree == ThemeTree && _built.treeAlt == ThemeTreeAlt) return;
            var kept = _cache.Find(b => b.content == content);
            if (kept != null && kept.version == content.version && kept.tree == ThemeTree && kept.treeAlt == ThemeTreeAlt)
            {
                _built = kept;
                TileCount = kept.tiles;
                BladeCount = kept.blades;
                _matricesDirty = true;
                _flashStart = -1f;
                return;
            }
            if (kept != null)
            {
                foreach (var m in kept.meshes) ReleaseObject(m);
                _cache.Remove(kept);
            }
            BuildCurrent();
        }

        /// <summary>Builds the current content into a new kept entry (the footprint edge texture is shared).</summary>
        void BuildCurrent()
        {
            double t0 = Time.realtimeSinceStartupAsDouble;
            _built = new Built();
            _cache.Add(_built);
            _matricesDirty = true;
            _flashStart = -1f;
            BladeCount = 0;
            TileCount = 0;
            if (map == null) return;
            var content = Content;
            _built.content = content;
            _built.version = content.version;
            var treeProp = _built.tree = ThemeTree;
            var treeAlt = _built.treeAlt = ThemeTreeAlt;

            var rnd = new SeededRandom(seed);
            float tile = map.tileSize, cap = map.capHeight;
            int rows = map.Rows, cols = map.Columns;
            float cr = (rows - 1) * 0.5f, cc = (cols - 1) * 0.5f;

            var columnMesh = Keep(ProceduralMeshes.Box(new Vector3(tile, ColumnDepth, tile), new Vector3(0f, -CapSkirt - ColumnDepth * 0.5f, 0f), "IslandColumn"));
            _capMesh = Keep(ProceduralMeshes.Box(new Vector3(tile * 0.998f, cap + CapSkirt, tile * 0.998f), new Vector3(0f, (cap - CapSkirt) * 0.5f, 0f), "IslandCap"));
            var undersideMesh = Keep(ProceduralMeshes.Box(new Vector3(tile * 0.86f, 1f, tile * 0.86f), new Vector3(0f, -0.5f, 0f), "IslandUnderside"));

            // ---- footprint: rocky base, fixed to the island ----
            var columns = AddBatch(columnMesh, column, false, ThemeRole.Dirt);
            var undersides = AddBatch(undersideMesh, underside, false, ThemeRole.Underside);
            columns.rim = undersides.rim = true;
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    if (map.At(r, c) == IslandMap.Empty) continue;
                    Vector3 p = map.TileCenter(r, c);
                    columns.local.Add(Matrix4x4.Translate(new Vector3(p.x, 0f, p.z)));

                    // The prototype spends one random number on a per-tile cap shade here. We keep the call
                    // so the sequence stays identical; the shade itself comes from _OriginJitter in the shader.
                    rnd.Next();

                    float dc = Mathf.Sqrt((r - cr) * (r - cr) + (c - cc) * (c - cc));
                    float depth = Mathf.Max(0.02f, 0.17f - dc * 0.022f + rnd.Next() * 0.035f);
                    undersides.local.Add(Matrix4x4.TRS(new Vector3(p.x, -CapSkirt - ColumnDepth, p.z), Quaternion.identity, new Vector3(1f, depth, 1f)));
                }
            if (_edgeTexture == null) BuildEdgeTexture();

            // ---- content: map space, grouped by cell ----
            int crows = content.Rows, ccols = content.Columns, cells = crows * ccols;
            var caps = new Dictionary<char, Batch>
            {
                { IslandMap.Grass, AddBatch(_capMesh, capGrass, true, ThemeRole.Grass) },
                { IslandMap.TallGrass, AddBatch(_capMesh, capTallGrass, true, ThemeRole.TallGrass) },
                { IslandMap.Path, AddBatch(_capMesh, capPath, true, ThemeRole.Path) },
                { IslandMap.Water, AddBatch(_capMesh, capWater, true, ThemeRole.Water) },
                { IslandMap.HotSpring, AddBatch(_capMesh, capHotSpring, true, ThemeRole.HotSpring) },
            };
            // Trees, rocks and the blocked border stand on grass. The border is drawn as forest.
            caps[IslandMap.Tree] = caps[IslandMap.Rock] = caps[IslandMap.Blocked] = caps[IslandMap.Grass];
            foreach (var kv in caps) _capBatches[kv.Key] = kv.Value;

            var capBatches = new List<Batch>(new HashSet<Batch>(caps.Values));
            bool hasHotSpring = false;
            ForEachCell(content, cells, capBatches, (r, c, ch) =>
            {
                if (!caps.TryGetValue(ch, out var b)) return;
                hasHotSpring |= ch == IslandMap.HotSpring;
                float y = ch == IslandMap.Water || ch == IslandMap.HotSpring ? -WaterDrop : 0f;
                b.local.Add(Matrix4x4.Translate(MapPoint(new Vector2(c, r)) + Vector3.up * y));
                TileCount++;
            });

            // ---- trees (also the blocked border): cone trees, or the theme's tree prop ----
            var treeBatches = new List<Batch>();
            Batch altBatch = null;
            if (treeProp != null)
            {
                treeBatches.Add(AddProp(treeProp));
                if (treeAlt != null) treeBatches.Add(altBatch = AddProp(treeAlt));
            }
            else
            {
                var trunkMesh = Keep(ProceduralMeshes.Cylinder(0.004f, 0.005f, 0.026f, 6, 0.013f, "TreeTrunk"));
                var leafLowMesh = Keep(ProceduralMeshes.Cylinder(0f, 0.022f, 0.036f, 7, 0.038f, "TreeLeavesLow"));
                var leafHighMesh = Keep(ProceduralMeshes.Cylinder(0f, 0.015f, 0.03f, 7, 0.058f, "TreeLeavesHigh"));
                treeBatches.AddRange(AddPart(trunkMesh, trunk, ThemeRole.Trunk));
                treeBatches.AddRange(AddPart(leafLowMesh, leavesLow, ThemeRole.LeavesLow));
                treeBatches.AddRange(AddPart(leafHighMesh, leavesHigh, ThemeRole.LeavesHigh));
            }
            ForEachCell(content, cells, treeBatches, (r, c, ch) =>
            {
                if (ch != IslandMap.Tree && ch != IslandMap.Blocked) return;
                // An open front edge (the side facing the player) where the theme asks for it.
                if (ch == IslandMap.Blocked && r == content.Rows - 1 && Theme != null && !Theme.frontBorderTrees) return;
                Vector3 p = MapPoint(new Vector2(c, r));
                float s = 0.9f + rnd.Next() * 0.35f;
                float ry = rnd.Next() * 6.28f;
                // Prop meshes are in tile units; cone trees are in metres.
                var m = Matrix4x4.TRS(new Vector3(p.x, cap, p.z), Quaternion.Euler(0f, -ry * Mathf.Rad2Deg, 0f), Vector3.one * (treeProp != null ? s * tile : s));
                if (treeProp != null)
                {
                    // Which shape: a stable hash of the tile (the random sequence above stays as it was).
                    bool alt = altBatch != null && TileHash(c, r) < Theme.treeAltShare;
                    (alt ? altBatch : treeBatches[0]).local.Add(m);
                }
                else
                    foreach (var b in treeBatches) b.local.Add(m);
            });

            // ---- rocks: two per rock tile, and a stone rim where a hot spring meets land ----
            var rockMesh = Keep(ProceduralMeshes.Icosahedron(0.011f, "Rock"));
            var rockBatches = AddPart(rockMesh, rock, ThemeRole.Rock);
            ForEachCell(content, cells, rockBatches, (r, c, ch) =>
            {
                Vector3 p = MapPoint(new Vector2(c, r));
                if (ch == IslandMap.HotSpring)
                {
                    foreach (var d in Sides)
                    {
                        char n = content.At(r + d.y, c + d.x);
                        if (n == IslandMap.HotSpring || n == IslandMap.Water || n == IslandMap.Empty) continue;
                        Vector3 edge = MapPoint(new Vector2(c + d.x * 0.42f, r + d.y * 0.42f));
                        Vector3 along = new Vector3(-d.y, 0f, d.x) * tile; // map X is mirrored, the sign doesn't matter here
                        for (int k = 0; k < 3; k++)
                        {
                            Vector3 at = edge + along * ((k - 1) * 0.33f + (rnd.Next() - 0.5f) * 0.1f);
                            float s = 0.45f + rnd.Next() * 0.3f;
                            var m = Matrix4x4.TRS(new Vector3(at.x, cap - 0.002f, at.z), MirroredEulerXYZ(rnd.Next(), rnd.Next() * 6f, rnd.Next()), new Vector3(s, s * 0.6f, s));
                            foreach (var b in rockBatches) b.local.Add(m);
                        }
                    }
                    return;
                }
                if (ch != IslandMap.Rock) return;
                for (int k = 0; k < 2; k++)
                {
                    float x = p.x - (rnd.Next() - 0.5f) * 0.025f;
                    float z = p.z + (rnd.Next() - 0.5f) * 0.025f;
                    float rx = rnd.Next(), ry = rnd.Next() * 6f, rz = rnd.Next();
                    float s = 0.6f + rnd.Next() * 0.7f;
                    var m = Matrix4x4.TRS(new Vector3(x, cap + 0.004f, z), MirroredEulerXYZ(rx, ry, rz), new Vector3(s, s * 0.7f, s));
                    foreach (var b in rockBatches) b.local.Add(m);
                }
            });

            // ---- grass blades ----
            var bladeMesh = Keep(ProceduralMeshes.GrassBlade());
            var blades = AddBatch(bladeMesh, grass, true, ThemeRole.Blades);
            ForEachCell(content, cells, new List<Batch> { blades }, (r, c, ch) =>
            {
                bool tall = ch == IslandMap.TallGrass;
                int n = tall ? bladesPerTallGrassTile
                    : ch == IslandMap.Grass || ch == IslandMap.Tree || ch == IslandMap.Rock || ch == IslandMap.Blocked ? bladesPerGrassTile : 0;
                Vector3 p = MapPoint(new Vector2(c, r));
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
            });
            BladeCount = blades.local.Count;

            BuildProps(content, cells, cap, hasHotSpring);

            // Footprint plus room for zoomed-in trees above and the underside below.
            float halfX = cols * tile * 0.5f + 0.02f, halfZ = rows * tile * 0.5f + 0.02f;
            _localBounds = new Bounds(new Vector3(0f, 0.05f, 0f), new Vector3(halfX * 2f, 0.8f, halfZ * 2f));
            _built.tiles = TileCount;
            _built.blades = BladeCount;
            if (Application.isPlaying)
                Debug.Log($"[Diorama] Built {content.name}: {TileCount} tiles, {BladeCount} blades, {_batches.Count} batches in " +
                          $"{(Time.realtimeSinceStartupAsDouble - t0) * 1000.0:0} ms ({_cache.Count} map(s) kept).");
        }

        /// <summary>0..1 per map tile, the same every build.</summary>
        static float TileHash(int c, int r)
        {
            uint h = (uint)(c * 73856093) ^ (uint)(r * 19349663);
            h ^= h >> 13;
            h *= 0x5bd1e995;
            h ^= h >> 15;
            return (h & 0xFFFFFF) / (float)0x1000000;
        }

        static readonly Vector2Int[] Sides = { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };

        /// <summary>
        /// The world's props layer (one batch per prop type, footprint centred, facing the player unless the recipe
        /// allows any yaw) and steam over hot springs. Own random sequence, so the terrain layout never shifts.
        /// </summary>
        void BuildProps(IslandMap content, int cells, float cap, bool hasHotSpring)
        {
            var wm = content as WorldMap;
            var batches = new Dictionary<PropRecipe, Batch>();
            Batch BatchFor(PropRecipe recipe)
            {
                if (recipe == null) return null;
                if (!batches.TryGetValue(recipe, out var b)) batches[recipe] = b = recipe.bakedMesh != null ? AddProp(recipe) : null;
                return b;
            }

            int rows = content.Rows, cols = content.Columns;
            var missing = new HashSet<string>();
            if (wm != null)
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < cols; c++)
                    {
                        char k = wm.PropAt(r, c);
                        if (k == IslandMap.Empty) continue;
                        if (!wm.TryGetProp(k, out var e)) missing.Add($"'{k}' (not in the legend)");
                        else if (BatchFor(e.recipe) == null) missing.Add($"{e.recipe.name} (not baked)");
                    }
            var steam = hasHotSpring ? BatchFor(hotSpringSteam) : null;
            if (hasHotSpring && steam == null)
                Debug.LogWarning("[Diorama] Hot springs without steam: assign a baked Hot Spring Steam prop on the IslandBuilder.", this);
            if (missing.Count > 0) Debug.LogWarning($"[Diorama] Props skipped: {string.Join(", ", missing)}. Tools > Pocket Isle > Props > Bake All.", this);
            if (batches.Count == 0) return;
            if (propMaterial == null)
            {
                Debug.LogError("[Diorama] The world has props but the IslandBuilder has no Prop Material (Diorama/Prop).", this);
                return;
            }

            float tile = map.tileSize;
            var rnd = new SeededRandom(seed * 7919 + 17);
            var list = new List<Batch>();
            foreach (var b in batches.Values) if (b != null) list.Add(b);
            ForEachCell(content, cells, list, (r, c, ch) =>
            {
                if (steam != null && ch == IslandMap.HotSpring)
                    for (int k = 0; k < 2; k++)
                    {
                        Vector3 at = MapPoint(new Vector2(c + (rnd.Next() - 0.5f) * 0.6f, r + (rnd.Next() - 0.5f) * 0.6f));
                        float s = tile * (0.8f + rnd.Next() * 0.4f);
                        steam.local.Add(Matrix4x4.TRS(new Vector3(at.x, cap, at.z), Quaternion.Euler(0f, rnd.Next() * 360f, 0f), Vector3.one * s));
                    }
                if (wm == null || !wm.TryGetProp(wm.PropAt(r, c), out var e)) return;
                var b = BatchFor(e.recipe);
                if (b == null) return;
                var f = WorldMap.FootprintOf(e);
                var recipe = e.recipe;
                Vector3 p = MapPoint(new Vector2(c + (f.x - 1) * 0.5f, r + (f.y - 1) * 0.5f));
                float yaw = recipe.randomYaw && f == Vector2Int.one
                    ? rnd.Next() * 360f
                    : (e.turned ? 90f : 0f) + e.yaw + (rnd.Next() * 2f - 1f) * recipe.yawJitter;
                float scale = tile * (1f + (rnd.Next() * 2f - 1f) * recipe.scaleJitter);
                b.local.Add(Matrix4x4.TRS(new Vector3(p.x, cap, p.z), Quaternion.Euler(0f, yaw, 0f), Vector3.one * scale));
            });
        }

        Batch AddProp(PropRecipe recipe)
        {
            var b = AddBatch(recipe.bakedMesh, propMaterial, true);
            b.passes = 2;
            recipe.ApplyTo(b.props);
            return b;
        }

        /// <summary>Runs <paramref name="add"/> for every content cell in row-major order and records where each cell's instances start.</summary>
        static void ForEachCell(IslandMap content, int cells, List<Batch> batches, Action<int, int, char> add)
        {
            foreach (var b in batches) b.cellStart = new int[cells + 1];
            int cols = content.Columns;
            for (int r = 0; r < content.Rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    int cell = r * cols + c;
                    foreach (var b in batches) b.cellStart[cell] = b.local.Count;
                    char ch = content.At(r, c);
                    if (ch != IslandMap.Empty) add(r, c, ch);
                }
            foreach (var b in batches) b.cellStart[cells] = b.local.Count;
        }

        /// <summary>
        /// Signed distance (tiles) to the footprint edge, positive inside, as an R8 texture over island-local XZ.
        /// The shaders read it to sink content near the edge and clip what lies outside.
        /// </summary>
        void BuildEdgeTexture()
        {
            int rows = map.Rows, cols = map.Columns;
            int gw = cols + EdgePadTiles * 2, gh = rows + EdgePadTiles * 2;
            int w = gw * EdgeTexelsPerTile, h = gh * EdgeTexelsPerTile;
            if (_edgeTexture == null || _edgeTexture.width != w || _edgeTexture.height != h)
            {
                if (_edgeTexture != null) ReleaseObject(_edgeTexture);
                _edgeTexture = new Texture2D(w, h, TextureFormat.R8, false, true)
                {
                    name = "IslandEdgeDistance",
                    hideFlags = HideFlags.DontSave,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                };
            }

            bool Inside(int r, int c) => map.At(r, c) != IslandMap.Empty;
            var pixels = new byte[w * h];
            int reach = Mathf.CeilToInt(EdgeRange) + 1;
            for (int ty = 0; ty < h; ty++)
                for (int tx = 0; tx < w; tx++)
                {
                    // Continuous grid position: cell (c, r) covers c-0.5..c+0.5.
                    float gc = -EdgePadTiles - 0.5f + (tx + 0.5f) / EdgeTexelsPerTile;
                    float gr = -EdgePadTiles - 0.5f + (ty + 0.5f) / EdgeTexelsPerTile;
                    int c0 = Mathf.RoundToInt(gc), r0 = Mathf.RoundToInt(gr);
                    bool inside = Inside(r0, c0);
                    float best = EdgeRange;
                    for (int r = r0 - reach; r <= r0 + reach; r++)
                        for (int c = c0 - reach; c <= c0 + reach; c++)
                        {
                            if (Inside(r, c) == inside) continue;
                            float dx = Mathf.Max(0f, Mathf.Abs(gc - c) - 0.5f);
                            float dy = Mathf.Max(0f, Mathf.Abs(gr - r) - 0.5f);
                            best = Mathf.Min(best, Mathf.Sqrt(dx * dx + dy * dy));
                        }
                    float s = inside ? best : -best;
                    pixels[ty * w + tx] = (byte)Mathf.RoundToInt(Mathf.Clamp01(s / EdgeRange * 0.5f + 0.5f) * 255f);
                }
            _edgeTexture.SetPixelData(pixels, 0);
            _edgeTexture.Apply(false, false);

            // Island-local (x, z) to uv. Grid column gc = cc - x / tile, grid row gr = cr + z / tile.
            float tile = map.tileSize, cc = (cols - 1) * 0.5f, cr = (rows - 1) * 0.5f;
            float gcMin = -EdgePadTiles - 0.5f, grMin = -EdgePadTiles - 0.5f;
            _edgeUv = new Vector4((cc - gcMin) / gw, -1f / (tile * gw), (cr - grMin) / gh, 1f / (tile * gh));
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

        Batch AddBatch(Mesh mesh, Material material, bool content, ThemeRole role = ThemeRole.None)
        {
            var b = new Batch { mesh = mesh, material = material, content = content, role = role, props = new MaterialPropertyBlock() };
            if (content) b.props.SetFloat(WindowClipId, 1f);
            _batches.Add(b);
            return b;
        }

        List<Batch> AddPart(Mesh mesh, ToonPart part, ThemeRole role)
        {
            var list = new List<Batch> { AddBatch(mesh, part.body, true, role) };
            if (part.outline != null)
            {
                var o = AddBatch(Keep(ProceduralMeshes.WithSmoothedNormals(mesh)), part.outline, true, role);
                o.outline = true;
                list.Add(o);
            }
            return list;
        }

        void ReleaseMeshes()
        {
            ReleaseCache();
            ReleaseObject(_edgeTexture);
            _edgeTexture = null;
        }

        void ReleaseCache()
        {
            foreach (var b in _cache)
                foreach (var m in b.meshes) ReleaseObject(m);
            _cache.Clear();
            _built = new Built();
        }

        static void ReleaseObject(UnityEngine.Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o);
            else DestroyImmediate(o);
        }

        // ---- per frame ----

        void UpdateWorldMatrices()
        {
            var l2w = transform.localToWorldMatrix;
            var c2w = l2w * ContentToLocal;
            bool footprintDirty = _matricesDirty || l2w != _lastLocalToWorld;
            bool contentDirty = _matricesDirty || c2w != _lastContentToWorld;
            if (!footprintDirty && !contentDirty) return;
            _lastLocalToWorld = l2w;
            _lastContentToWorld = c2w;
            _matricesDirty = false;

            GetVisibleCells(out int r0, out int r1, out int c0, out int c1);
            int ccols = Content.Columns;
            foreach (var b in _batches)
            {
                if (!b.content)
                {
                    if (!footprintDirty) continue;
                    if (b.world.Length != b.local.Count) b.world = new Matrix4x4[b.local.Count];
                    for (int i = 0; i < b.local.Count; i++) b.world[i] = l2w * b.local[i];
                    continue;
                }
                if (!contentDirty) continue;
                // Rows are contiguous runs of cells, so each row is one range copy.
                int n = 0;
                for (int r = r0; r <= r1; r++)
                {
                    int start = b.cellStart[r * ccols + c0], end = b.cellStart[r * ccols + c1 + 1];
                    if (b.world.Length < n + end - start) Array.Resize(ref b.world, Mathf.Max(64, (n + end - start) * 2));
                    for (int i = start; i < end; i++) b.world[n++] = c2w * b.local[i];
                }
                b.count = n;
            }
        }

        /// <summary>Content cells that overlap the footprint for the current view, clamped to the map.</summary>
        void GetVisibleCells(out int r0, out int r1, out int c0, out int c1)
        {
            var content = Content;
            // Island-local half size in tiles, mapped back through the view: tile = pivot + (local - (pivot - center)) / zoom.
            float halfC = map.Columns * 0.5f, halfR = map.Rows * 0.5f;
            Vector2 center = ViewCenter, pivot = ZoomPivot;
            float minC = pivot.x + (-halfC - pivot.x + center.x) / _zoom, maxC = pivot.x + (halfC - pivot.x + center.x) / _zoom;
            float minR = pivot.y + (-halfR - pivot.y + center.y) / _zoom, maxR = pivot.y + (halfR - pivot.y + center.y) / _zoom;
            c0 = Mathf.Clamp(Mathf.FloorToInt(minC) - 1, 0, content.Columns - 1);
            c1 = Mathf.Clamp(Mathf.CeilToInt(maxC) + 1, 0, content.Columns - 1);
            r0 = Mathf.Clamp(Mathf.FloorToInt(minR) - 1, 0, content.Rows - 1);
            r1 = Mathf.Clamp(Mathf.CeilToInt(maxR) + 1, 0, content.Rows - 1);
        }

        void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            // Skip material/inspector preview cameras so the island doesn't show up in thumbnails.
            if (cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection) return;
            // Cameras that render a preview scene (editor shot tools) only see that scene.
            if (cam.scene.IsValid() && cam.scene != gameObject.scene) return;
            Draw(cam);
        }

        void SetWindowGlobals()
        {
            var l2w = transform.localToWorldMatrix;
            Shader.SetGlobalMatrix(IslandToWorldId, l2w);
            Shader.SetGlobalMatrix(WorldToIslandId, l2w.inverse);
            Shader.SetGlobalMatrix(WorldToMapId, (l2w * ContentToLocal).inverse);
            Shader.SetGlobalMatrix(MapToWorldId, l2w * ContentToLocal);
            Shader.SetGlobalTexture(FootprintId, _edgeTexture);
            Shader.SetGlobalVector(FootprintUvId, _edgeUv);
            // x: clip plane (column top), y: sinking band half width in footprint tiles (zoomed tiles past it are cut at
            // the rim instead of sunk, or a zoomed window would be mostly bare dirt), z: deepest sink, w: distance range.
            Shader.SetGlobalVector(WindowId, new Vector4(-CapSkirt, 0.5f, SinkDepth * _zoom + CapSkirt, EdgeRange));
            Shader.SetGlobalFloat(DioramaScaleId, transform.lossyScale.y * _zoom);

            // Map space: columns run toward -X, rows toward +Z (MapPoint), so the axis flips its x.
            float tile = TileSize;
            Vector3 c = MapPoint(_clearCenter), v = MapPoint(_clearView);
            Shader.SetGlobalVector(ClearingId, new Vector4(c.x, c.z, -_clearAxis.x, _clearAxis.y));
            Shader.SetGlobalVector(ClearingSizeId, new Vector4(_clearRadii.x * tile, _clearRadii.y * tile, clearingStrength, clearingDim));
            Shader.SetGlobalVector(ClearingViewId, new Vector4(v.x, v.z, _clearViewWidth * tile, 0f));
            Shader.SetGlobalVector(ClearingWorldId, (l2w * ContentToLocal).MultiplyPoint3x4(c));
            Shader.SetGlobalVector(SwapId, new Vector4(swapSink, _localBounds.max.z, Mathf.Max(0.01f, _localBounds.size.z), swapRadial ? 1f : 0f));
        }

        /// <summary>Queues the island's instanced draws for one camera.</summary>
        public void Draw(Camera cam)
        {
            Prepare();
            if (_batches.Count == 0) return;
            UpdateWorldMatrices();
            SetWindowGlobals();
            ApplyTheme();

            var bounds = TransformBounds(transform.localToWorldMatrix, _localBounds);
            if (_flashProps == null)
            {
                _flashProps = new MaterialPropertyBlock();
                _flashProps.SetFloat(WindowClipId, 1f);
            }
            foreach (var b in _batches)
            {
                int count = InstanceCount(b);
                if (b.material == null || count == 0) continue;
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
                    matProps = b.props,
                };
                for (int start = 0; start < count; start += MaxInstancesPerDraw)
                    Graphics.RenderMeshInstanced(rp, b.mesh, 0, b.world, Mathf.Min(MaxInstancesPerDraw, count - start), start);
            }
            DrawFlash(cam, bounds);
        }

        /// <summary>Theme colours for the current restoration, the rim highlight, and the restoration global for props.</summary>
        void ApplyTheme()
        {
            RealmState.Sync();
            foreach (var b in _batches)
                if (b.rim) b.props.SetFloat(FlashId, rimHighlight);
            var theme = Theme;
            if (theme == null) return;
            var t = theme.Evaluate(RealmState.Restored);
            bool tint = rimTheme != null && rimTint > 0f;
            var next = tint ? rimTheme.Evaluate(RealmState.Restored) : default;
            foreach (var b in _batches)
            {
                if (b.role == ThemeRole.None) continue;
                if (b.role == ThemeRole.Blades)
                {
                    b.props.SetColor(ShortBaseId, t.shortBase);
                    b.props.SetColor(ShortTipId, t.shortTip);
                    b.props.SetColor(TallBaseId, t.tallBase);
                    b.props.SetColor(TallTipId, t.tallTip);
                    continue;
                }
                Color c = RoleColor(t, b.role);
                if (tint && b.rim) c = Color.Lerp(c, RoleColor(next, b.role), rimTint);
                b.color = c;
                if (b.outline)
                {
                    // Like the creature outlines: colour x 0.28 in linear, then the theme tint.
                    Color o = (c.linear * 0.28f).gamma * t.outlineTint;
                    o.a = 1f;
                    b.props.SetColor(OutlineColorId, o);
                    continue;
                }
                b.props.SetColor(BaseColorId, c);
                if (b.role == ThemeRole.LeavesLow || b.role == ThemeRole.LeavesHigh) b.props.SetFloat(EmissiveId, t.leafGlow);
                if (b.role == ThemeRole.TallGrass) b.props.SetColor(ClearColorId, new Color(t.grass.r, t.grass.g, t.grass.b, 1f));
            }
        }

        static Color RoleColor(in ThemeColors t, ThemeRole role) => role switch
        {
            ThemeRole.Grass => t.grass,
            ThemeRole.TallGrass => t.tallGrass,
            ThemeRole.Path => t.path,
            ThemeRole.Water => t.water,
            ThemeRole.HotSpring => t.hotSpring,
            ThemeRole.Dirt => t.dirt,
            ThemeRole.Underside => t.underside,
            ThemeRole.Trunk => t.trunk,
            ThemeRole.LeavesLow => t.leavesLow,
            ThemeRole.LeavesHigh => t.leavesHigh,
            ThemeRole.Rock => t.rock,
            _ => Color.magenta,
        };

        void DrawFlash(Camera cam, Bounds bounds)
        {
            if (_flashStart < 0f || _flashBatch == null || _flashBatch.material == null || _capMesh == null) return;
            float t = (Time.time - _flashStart) / _flashSeconds;
            if (t >= 1f)
            {
                _flashStart = -1f;
                return;
            }
            _flashProps.SetFloat(FlashId, 0.75f * Mathf.Sin(t * Mathf.PI));
            if (Theme != null) _flashProps.SetColor(BaseColorId, _flashBatch.color);
            // Slightly larger than the cap so it wins the depth test.
            var local = Matrix4x4.TRS(MapPoint(_flashTile) + Vector3.up * 0.0005f, Quaternion.identity, new Vector3(1.01f, 1.05f, 1.01f));
            _flashMatrix[0] = _lastContentToWorld * local;
            var rp = new RenderParams(_flashBatch.material)
            {
                camera = cam,
                layer = gameObject.layer,
                worldBounds = bounds,
                shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = false,
                matProps = _flashProps,
            };
            Graphics.RenderMeshInstanced(rp, _capMesh, 0, _flashMatrix, 1);
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
