using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Flat markers on the island top: a soft ring on the tile a fingertip or the gaze points at (green = the explorer
    /// can walk there, grey = it can't), a small gaze reticle, and a pulsing marker on the explorer's GoTo destination
    /// until it arrives. Pickers (<see cref="TileTouch"/>, <see cref="TileGaze"/>) call <see cref="Hover"/> each
    /// frame; the highest priority wins. Placed in LateUpdate, after the explorer has moved the view.
    /// Step 9e: trees and grass never hide the hover ring: a second, softer copy draws where it is covered (depth test
    /// Greater), and a short thin light pillar (<see cref="pillarMaterial"/>, drawn on top) stands on the tile.
    /// </summary>
    public class TileCursor : MonoBehaviour
    {
        public IslandBuilder island;
        public GestureTestPiece explorer;
        public Material hudMaterial;
        [Tooltip("Diorama/Beacon material for the hover pillar (copied, mode 2, drawn on top). Empty = no pillar.")]
        public Material pillarMaterial;
        [Tooltip("Alpha of the ring where trees or grass cover it.")]
        [Range(0f, 1f)] public float coveredAlpha = 0.55f;
        [Tooltip("Pillar height and width in tiles.")]
        public Vector2 pillarSize = new Vector2(1.6f, 0.14f);

        public Color reachableColor = new Color(0.62f, 0.96f, 0.58f, 0.95f);
        public Color blockedColor = new Color(0.72f, 0.72f, 0.72f, 0.6f);
        public Color markerColor = new Color(1f, 0.82f, 0.42f, 0.95f);
        [Tooltip("Ring diameter as a fraction of a tile.")]
        public float ringSize = 0.92f;
        [Tooltip("Height above the cap, map metres.")]
        public float lift = 0.002f;

        static readonly int BackColorId = Shader.PropertyToID("_BackColor");
        static readonly int BorderColorId = Shader.PropertyToID("_BorderColor");
        static readonly int FillId = Shader.PropertyToID("_Fill");
        static readonly int AspectId = Shader.PropertyToID("_Aspect");
        static readonly int RadiusId = Shader.PropertyToID("_Radius");
        static readonly int BorderId = Shader.PropertyToID("_Border");
        static readonly int ZTestId = Shader.PropertyToID("_ZTest");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int CoreId = Shader.PropertyToID("_Core");
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        /// <summary>Debug shots: holds the hover ring on this tile.</summary>
        [System.NonSerialized] public Vector2Int? debugHover;

        MeshRenderer _ring, _marker, _reticle, _ringCovered, _pillar;
        Material _coveredMaterial, _pillarMaterial;
        Mesh _quad;
        MaterialPropertyBlock _block;
        int _hoverFrame = -1, _hoverPriority, _reticleFrame = -1;
        Vector2Int _hoverTile;
        bool _hoverReachable;
        Vector3 _reticleLocal;
        float _ringAlpha;

        void Awake()
        {
            if (hudMaterial == null) Debug.LogError("[TileCursor] No HUD material assigned.", this);
            _quad = BattleHud.VerticalQuad();
            _block = new MaterialPropertyBlock();
            _ring = Quad("Hover Ring");
            _marker = Quad("Destination Marker");
            _reticle = Quad("Gaze Reticle");
            Paint(_marker, markerColor, 0.35f, 0.16f);
            Paint(_reticle, new Color(1f, 1f, 1f, 0.85f), 0.5f, 0.3f);
            if (hudMaterial != null)
            {
                _coveredMaterial = new Material(hudMaterial) { name = "Hud (covered)", renderQueue = hudMaterial.renderQueue + 1 };
                _coveredMaterial.SetFloat(ZTestId, (float)UnityEngine.Rendering.CompareFunction.Greater);
                _ringCovered = Quad("Hover Ring (covered)");
                _ringCovered.sharedMaterial = _coveredMaterial;
            }
            if (pillarMaterial != null)
            {
                _pillarMaterial = new Material(pillarMaterial) { name = "Hover Pillar", renderQueue = 3100 };
                _pillarMaterial.SetFloat("_Mode", 2f);
                _pillarMaterial.SetFloat(ZTestId, (float)UnityEngine.Rendering.CompareFunction.Always);
                _pillar = Quad("Hover Pillar");
                _pillar.sharedMaterial = _pillarMaterial;
            }
        }

        void OnDestroy()
        {
            Destroy(_quad);
            if (_coveredMaterial != null) Destroy(_coveredMaterial);
            if (_pillarMaterial != null) Destroy(_pillarMaterial);
        }

        MeshRenderer Quad(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(island != null ? island.transform : transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = _quad;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = hudMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.enabled = false;
            return r;
        }

        /// <summary>Shows the hover ring on <paramref name="tile"/> this frame (higher <paramref name="priority"/> wins).</summary>
        public void Hover(Vector2Int tile, bool reachable, int priority)
        {
            if (_hoverFrame == Time.frameCount && priority < _hoverPriority) return;
            _hoverFrame = Time.frameCount;
            _hoverPriority = priority;
            _hoverTile = tile;
            _hoverReachable = reachable;
        }

        /// <summary>Shows the gaze reticle at an island-local point this frame.</summary>
        public void Reticle(Vector3 islandLocal)
        {
            _reticleFrame = Time.frameCount;
            _reticleLocal = islandLocal;
        }

        void LateUpdate()
        {
            if (island == null || island.map == null) return;
            float cap = island.Content.capHeight;
            float tile = island.map.tileSize * island.Zoom;
            var flat = Quaternion.Euler(90f, 0f, 0f);

            if (debugHover.HasValue) Hover(debugHover.Value, true, 99);
            bool hover = _hoverFrame == Time.frameCount;
            _ringAlpha = Mathf.MoveTowards(_ringAlpha, hover ? 1f : 0f, Time.deltaTime * 10f);
            _ring.enabled = _ringAlpha > 0.01f;
            if (_ring.enabled)
            {
                var t = _ring.transform;
                if (hover) t.localPosition = island.TileToLocal(_hoverTile, cap + lift);
                t.localRotation = flat;
                t.localScale = Vector3.one * (tile * ringSize * Mathf.Lerp(0.8f, 1f, _ringAlpha));
                var c = _hoverReachable ? reachableColor : blockedColor;
                c.a *= _ringAlpha;
                Paint(_ring, c, 0.5f, 0.1f);
                if (_ringCovered != null)
                {
                    _ringCovered.transform.SetLocalPositionAndRotation(t.localPosition, t.localRotation);
                    _ringCovered.transform.localScale = t.localScale;
                    var soft = c;
                    soft.a *= coveredAlpha;
                    Paint(_ringCovered, soft, 0.5f, 0.14f);
                }
                if (_pillar != null) PlacePillar(t.localPosition, tile, c);
            }
            if (_ringCovered != null) _ringCovered.enabled = _ring.enabled;
            if (_pillar != null) _pillar.enabled = _ring.enabled && _pillarMaterial != null;

            _reticle.enabled = _reticleFrame == Time.frameCount;
            if (_reticle.enabled)
            {
                var t = _reticle.transform;
                t.localPosition = new Vector3(_reticleLocal.x, island.TileToLocal(Vector2.zero, cap + lift * 1.5f).y, _reticleLocal.z);
                t.localRotation = flat;
                t.localScale = Vector3.one * (tile * 0.16f);
            }

            var dest = explorer != null ? explorer.Destination : null;
            _marker.enabled = dest.HasValue;
            if (dest.HasValue)
            {
                var t = _marker.transform;
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 6f);
                t.localPosition = island.TileToLocal(dest.Value, cap + lift * 1.2f);
                t.localRotation = flat * Quaternion.Euler(0f, 0f, 45f + Time.time * 40f);
                t.localScale = Vector3.one * (tile * Mathf.Lerp(0.42f, 0.56f, pulse));
            }
        }

        /// <summary>The pillar stands on the ring's centre, turned to the head about the island's up.</summary>
        void PlacePillar(Vector3 ringLocal, float tile, Color c)
        {
            var t = _pillar.transform;
            float h = tile * pillarSize.x;
            t.localPosition = ringLocal + Vector3.up * (h * 0.5f);
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 to = island.transform.InverseTransformPoint(cam.transform.position) - ringLocal;
                to.y = 0f;
                if (to.sqrMagnitude > 1e-8f) t.localRotation = Quaternion.LookRotation(-to, Vector3.up);
            }
            t.localScale = new Vector3(tile * pillarSize.y, h, 1f);
            _block.Clear();
            _block.SetColor(ColorId, c);
            _block.SetColor(CoreId, Color.Lerp(c, Color.white, 0.6f));
            _block.SetFloat(IntensityId, 0.7f * c.a);
            _pillar.SetPropertyBlock(_block);
        }

        void Paint(MeshRenderer r, Color c, float radius, float border)
        {
            var back = c;
            back.a *= 0.22f;
            _block.Clear();
            _block.SetColor(BackColorId, back);
            _block.SetColor(BorderColorId, c);
            _block.SetFloat(FillId, 0f);
            _block.SetFloat(AspectId, 1f);
            _block.SetFloat(RadiusId, radius);
            _block.SetFloat(BorderId, border);
            r.SetPropertyBlock(_block);
        }
    }
}
