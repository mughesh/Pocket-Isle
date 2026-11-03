using System;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 14c: a small sign above each of the hub's realm gates: the realm's name in its colour and its emblem (the
    /// realm's baked name card, PropKit.RealmCards, drawn small), readable from the default view. The sign over the gate
    /// the Spirit's trail points to next (<see cref="SpiritTrail.NextGate"/>: wind, then fire, then water) glows gold.
    /// Shown while its gate tile is inside the island window (like the gate portals), hub only.
    /// </summary>
    public class GateSigns : MonoBehaviour
    {
        [Serializable]
        public struct Sign
        {
            [Tooltip("Gate id on the hub map: wind / fire / water.")]
            public string gate;
            public PropRecipe recipe;
            [Tooltip("Top of the gate's arch (tiles above the cap): the sign floats just over it.")]
            public float archTop;
        }

        public IslandBuilder island;
        public WorldWindow window;
        public ShardGates gates;
        [Tooltip("Diorama/Prop, window clip off (Materials/Props/Logo.mat).")]
        public Material material;
        public Sign[] signs = new Sign[0];

        [Tooltip("Sign width in tiles.")]
        public float widthTiles = 2.1f;
        [Tooltip("Gap between the arch's top and the sign's bottom (tiles).")]
        public float gapTiles = 0.15f;
        [Tooltip("Gold glow on the next gate's sign: outline tint and the letters' glow.")]
        public Color gold = new Color(1f, 0.82f, 0.3f, 1f);

        static readonly int WindowClipId = Shader.PropertyToID("_WindowClip");
        static readonly int RestoredSelfId = Shader.PropertyToID("_RestoredSelf");
        static readonly int ClearSinkId = Shader.PropertyToID("_ClearSink");
        static readonly int OutlineTintId = Shader.PropertyToID("_OutlineTint");
        static readonly int GlowAId = Shader.PropertyToID("_GlowA");

        Transform[] _views;
        MeshRenderer[] _renderers;
        MaterialPropertyBlock _block;

        /// <summary>The gate whose sign glows now (tests, the bridge).</summary>
        public string Lit { get; private set; }

        void Start()
        {
            if (island == null || material == null)
            {
                Debug.LogError("[GateSigns] Needs the island and the logo material (Set Up MR_Main (14c Galewick first)).", this);
                enabled = false;
                return;
            }
            _block = new MaterialPropertyBlock();
            _views = new Transform[signs.Length];
            _renderers = new MeshRenderer[signs.Length];
            for (int i = 0; i < signs.Length; i++)
            {
                var mesh = signs[i].recipe != null ? signs[i].recipe.bakedMesh : null;
                if (mesh == null)
                {
                    Debug.LogError($"[GateSigns] The {signs[i].gate} gate's card isn't baked (Set Up MR_Main (14c Galewick first)).", this);
                    continue;
                }
                var go = new GameObject($"Gate Sign {signs[i].gate}");
                go.transform.SetParent(island.transform, false);
                var inner = new GameObject("Card");
                inner.transform.SetParent(go.transform, false);
                inner.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = inner.AddComponent<MeshRenderer>();
                r.sharedMaterial = material;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                // Centred on its bounds, bottom edge at the pivot.
                var b = mesh.bounds;
                inner.transform.localPosition = -new Vector3(b.center.x, b.min.y, b.center.z);
                _views[i] = go.transform;
                _renderers[i] = r;
                go.SetActive(false);
            }
        }

        void LateUpdate()
        {
            if (_views == null) return;
            var world = island.world;
            bool hub = world != null && world.realm == "stillisle" && !SceneSwap.IsInside && !SceneSwap.IsBusy && (window == null || !window.IsZoomed);
            Lit = hub ? SpiritTrail.NextGate(PuzzleState.IsSolved, IsOpen) : null;
            float tile = island.map != null ? island.map.tileSize : 0.05f;
            float z = island.Zoom;
            for (int i = 0; i < _views.Length; i++)
            {
                var v = _views[i];
                if (v == null) continue;
                int g = hub ? world.gates.FindIndex(x => x.id == signs[i].gate) : -1;
                if (g < 0)
                {
                    v.gameObject.SetActive(false);
                    continue;
                }
                var link = world.gates[g];
                var centre = new Vector2(link.tile.x + 0.5f, link.tile.y);
                var ground = island.TileToLocal(centre, island.Content.capHeight);
                bool show = island.FootprintContains(ground);
                v.gameObject.SetActive(show);
                if (!show) continue;
                var mesh = _renderers[i].GetComponent<MeshFilter>().sharedMesh;
                float scale = widthTiles * tile * z / Mathf.Max(1e-4f, mesh.bounds.size.x);
                v.localPosition = ground + Vector3.up * ((ArchTop(world, link, signs[i].archTop) + gapTiles) * tile * z);
                v.localRotation = Quaternion.identity;
                v.localScale = Vector3.one * scale;
                bool lit = Lit == signs[i].gate;
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 3f);
                _block.Clear();
                signs[i].recipe.ApplyTo(_block);
                _block.SetFloat(WindowClipId, 0f);
                _block.SetFloat(RestoredSelfId, 1f);
                _block.SetFloat(ClearSinkId, 0f);
                if (lit)
                {
                    _block.SetColor(OutlineTintId, new Color(gold.r, gold.g, gold.b, Mathf.Lerp(0.7f, 1f, pulse)));
                    var r = signs[i].recipe;
                    _block.SetVector(GlowAId, new Vector4(Mathf.Lerp(0.35f, 0.7f, pulse), r.Color(PropSlot.Wood).glow, r.Color(PropSlot.Stone).glow, r.Color(PropSlot.Leaf).glow));
                }
                _renderers[i].SetPropertyBlock(_block);
            }
        }

        /// <summary>
        /// Nov 3 (Mugesh: the Galewick sign overlapped its arch): the top of the gate prop's own mesh (tiles: props bake in
        /// tile units), never below <paramref name="least"/>.
        /// </summary>
        static float ArchTop(WorldMap world, GateLink link, float least)
        {
            if (world.TryGetProp(world.PropAt(link.tile.y, link.tile.x), out var prop) && prop.recipe != null && prop.recipe.bakedMesh != null)
                return Mathf.Max(least, prop.recipe.bakedMesh.bounds.max.y);
            return least;
        }

        bool IsOpen(string id)
        {
            var world = island.world;
            int g = world != null ? world.gates.FindIndex(x => x.id == id) : -1;
            return g >= 0 && (gates == null || gates.IsOpen(world.gates[g]));
        }
    }
}
