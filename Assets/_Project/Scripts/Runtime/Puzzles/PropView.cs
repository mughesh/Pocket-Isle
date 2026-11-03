using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// One baked prop drawn on its own on the island (step 9b): a puzzle object that moves, swaps its look or warms up,
    /// which the island's instanced props can't do one by one. Same Diorama/Prop material and palette as the instanced
    /// copy, positioned in map tiles each frame (it scrolls with the window and sinks at the window edge and in swaps
    /// like the rest). <see cref="restoredSelf"/> gives it its own restoration (a brazier warming in a cold realm);
    /// <see cref="glowBoost"/> adds glow per slot on top of the recipe's. Two draw calls (lit + outline) while shown.
    /// </summary>
    public class PropView
    {
        static readonly int WindowClipId = Shader.PropertyToID("_WindowClip");
        static readonly int RestoredSelfId = Shader.PropertyToID("_RestoredSelf");
        static readonly int GlowAId = Shader.PropertyToID("_GlowA");
        static readonly int GlowBId = Shader.PropertyToID("_GlowB");
        static readonly int SpinSelfId = Shader.PropertyToID("_SpinSelf");

        readonly IslandBuilder _island;
        readonly MeshFilter _filter;
        readonly MeshRenderer _renderer;
        readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        PropRecipe _recipe;
        // Step 11d: where it was placed on the map, re-applied at render time (Sync): the window may scroll after the
        // piece placed it this frame (the explorer follows in its own Update), and a frame-old position flickered the
        // spin phase of a turning mill while walking.
        static readonly List<PropView> _all = new List<PropView>();
        Vector2 _tile;
        float _height;
        bool _onMap;
        float _appliedRestored = float.NaN, _appliedClip = float.NaN, _appliedSpin = float.NaN;
        Vector4 _appliedBoostA, _appliedBoostB;

        public readonly Transform transform;

        /// <summary>Own restoration 0..1 (-1 = the realm's).</summary>
        public float restoredSelf = -1f;
        /// <summary>Extra glow per slot (Clay, Wood, Stone, Leaf) and (Glow, Accent, -, -).</summary>
        public Vector4 glowBoostA, glowBoostB;
        /// <summary>Step 10b: own angle (radians) for the recipe's spinning pieces (a flicked windmill); -1000 = the realm's wind.</summary>
        public float spinSelf = -1000f;
        /// <summary>Clipped and sunk at the island window's edge (off while a hand holds it up in the air).</summary>
        public bool windowClip = true;

        public PropRecipe Recipe => _recipe;
        public bool Visible => _renderer != null && _renderer.enabled;
        /// <summary>The mesh's lowest point under its pivot, in shape units (tiles at scale 1); 0 without a mesh.</summary>
        public float Bottom => _filter != null && _filter.sharedMesh != null ? _filter.sharedMesh.bounds.min.y : 0f;

        public PropView(string name, IslandBuilder island, PropRecipe recipe)
        {
            _island = island;
            var go = new GameObject(name);
            go.layer = island.gameObject.layer;
            transform = go.transform;
            transform.SetParent(island.transform, false);
            _filter = go.AddComponent<MeshFilter>();
            _renderer = go.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = island.propMaterial;
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.enabled = false;
            _all.Add(this);
            if (island.propMaterial == null) Debug.LogError($"[PropView] {name}: the IslandBuilder has no Prop Material.", island);
            SetRecipe(recipe);
        }

        /// <summary>Swaps the look (a brazier lighting up). Logs if the recipe isn't baked.</summary>
        public void SetRecipe(PropRecipe recipe)
        {
            if (recipe == _recipe) return;
            _recipe = recipe;
            _filter.sharedMesh = recipe != null ? recipe.bakedMesh : null;
            if (recipe != null && recipe.bakedMesh == null) Debug.LogError($"[PropView] {recipe.name} isn't baked (Tools > Pocket Isle > Props > Bake All).", recipe);
            _appliedRestored = float.NaN;
        }

        /// <summary>
        /// Places it at a map tile (fractions allowed), <paramref name="height"/> tiles above the cap, turned by
        /// <paramref name="rotation"/> (map space: +Z toward the player), <paramref name="scale"/> x a tile. Hidden when
        /// <paramref name="show"/> is false or it lies outside the island window.
        /// </summary>
        public void Place(Vector2 tile, float height, Quaternion rotation, bool show, float scale = 1f) =>
            Place(tile, height, rotation, show, Vector3.one * scale);

        /// <summary>As above with a non-uniform scale (a bellows squashing), in the prop's own axes.</summary>
        public void Place(Vector2 tile, float height, Quaternion rotation, bool show, Vector3 scale)
        {
            if (_renderer == null) return;
            var content = _island.Content;
            float t = _island.map != null ? _island.map.tileSize : 0.05f;
            Vector3 local = _island.TileToLocal(tile, (content != null ? content.capHeight : 0f) + height * t);
            bool inWindow = show && content != null && _filter.sharedMesh != null && NearFootprint(tile, content.capHeight);
            _renderer.enabled = inWindow;
            _onMap = inWindow;
            if (!inWindow) return;
            _tile = tile;
            _height = height;
            transform.localPosition = local;
            transform.localRotation = rotation;
            transform.localScale = scale * (t * _island.Zoom);
            Apply();
        }

        /// <summary>
        /// Places it at an island-local point (on the rim, floating over the island), <paramref name="metres"/> per shape
        /// unit, no window clip (things off the map: the shard holder, a shard in the air).
        /// </summary>
        public void PlaceLocal(Vector3 local, Quaternion rotation, float metres, bool show)
        {
            if (_renderer == null) return;
            _renderer.enabled = show && _filter.sharedMesh != null;
            _onMap = false;
            if (!_renderer.enabled) return;
            windowClip = false;
            transform.localPosition = local;
            transform.localRotation = rotation;
            transform.localScale = Vector3.one * metres;
            Apply();
        }

        bool NearFootprint(Vector2 tile, float cap)
        {
            if (_island.FootprintContains(_island.TileToLocal(tile, cap))) return true;
            // Props on the window's edge still show their sinking half.
            for (int i = 0; i < 4; i++)
            {
                var d = i == 0 ? Vector2.right : i == 1 ? Vector2.left : i == 2 ? Vector2.up : Vector2.down;
                if (_island.FootprintContains(_island.TileToLocal(tile + d, cap))) return true;
            }
            return false;
        }

        void Apply()
        {
            float clip = windowClip ? 1f : 0f;
            if (_appliedRestored == restoredSelf && _appliedClip == clip && _appliedBoostA == glowBoostA && _appliedBoostB == glowBoostB && _appliedSpin == spinSelf) return;
            _block.Clear();
            if (_recipe != null) _recipe.ApplyTo(_block);
            _block.SetFloat(WindowClipId, clip);
            _block.SetFloat(RestoredSelfId, restoredSelf);
            _block.SetFloat(SpinSelfId, spinSelf);
            if (_recipe != null && (glowBoostA != Vector4.zero || glowBoostB != Vector4.zero))
            {
                var p = _recipe.palette;
                float G(int i) => p != null && i < p.Length ? p[i].glow : 0f;
                _block.SetVector(GlowAId, new Vector4(G(0), G(1), G(2), G(3)) + glowBoostA);
                _block.SetVector(GlowBId, new Vector4(G(4), G(5), 0f, 0f) + glowBoostB);
            }
            _renderer.SetPropertyBlock(_block);
            _appliedRestored = restoredSelf;
            _appliedClip = clip;
            _appliedBoostA = glowBoostA;
            _appliedBoostB = glowBoostB;
            _appliedSpin = spinSelf;
        }

        public void Hide()
        {
            if (_renderer != null) _renderer.enabled = false;
        }

        /// <summary>World point of a spot on the prop (shape units, tiles).</summary>
        public Vector3 PointWorld(Vector3 shape) => transform.TransformPoint(shape);

        public void Destroy()
        {
            _all.Remove(this);
            if (transform != null) Object.Destroy(transform.gameObject);
        }

        /// <summary>
        /// Step 11d: the island is about to draw: every view placed on its map this frame goes to where its tile is in
        /// the window now (the view may have scrolled since it was placed).
        /// </summary>
        public static void Sync(IslandBuilder island)
        {
            for (int i = _all.Count - 1; i >= 0; i--)
            {
                var v = _all[i];
                if (v == null || v.transform == null)
                {
                    _all.RemoveAt(i);
                    continue;
                }
                if (v._island != island || !v._onMap || v._renderer == null || !v._renderer.enabled) continue;
                var content = island.Content;
                if (content == null || island.map == null) continue;
                v.transform.localPosition = island.TileToLocal(v._tile, content.capHeight + v._height * island.map.tileSize);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _all.Clear();
    }
}
