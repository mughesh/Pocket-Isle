using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Soft blob shadow on the real surface straight below the island (floor, table, bed), found with an MRUK
    /// room raycast. Without scene data there is no shadow; the game keeps playing. Logs which path it takes.
    /// </summary>
    public class IslandShadow : MonoBehaviour
    {
        public IslandBuilder island;
        public Material shadowMaterial;

        [Tooltip("No shadow when the surface is further below than this (m).")]
        public float maxHeight = 2.5f;
        [Range(0f, 1f)] public float maxAlpha = 0.45f;
        [Tooltip("Shadow width relative to the island's footprint when touching the surface.")]
        public float sizeFactor = 1.05f;
        [Tooltip("Extra width per metre of height (softer, wider when higher).")]
        public float spreadPerMetre = 0.5f;

        static readonly int AlphaId = Shader.PropertyToID("_Alpha");

        Transform _quad;
        MeshRenderer _renderer;
        MaterialPropertyBlock _props;
        int _loggedPath = -1; // 0 no scene data, 1 shadow on

        void Start()
        {
            var go = new GameObject("Blob Shadow Quad");
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = BuildQuad();
            _renderer = go.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = shadowMaterial;
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _quad = go.transform;
            _props = new MaterialPropertyBlock();
            _renderer.enabled = false;
            if (shadowMaterial == null) Debug.LogError("[IslandShadow] No shadow material assigned.");
        }

        void LateUpdate()
        {
            if (island == null || _renderer == null) return;
            var mruk = MRUK.Instance;
            var room = mruk != null && mruk.IsInitialized ? mruk.GetCurrentRoom() : null;
            if (room == null)
            {
                LogPath(0, "[IslandShadow] No MRUK scene data: island floats without a shadow.");
                _renderer.enabled = false;
                return;
            }
            LogPath(1, "[IslandShadow] MRUK room loaded: shadow on the surface below the island.");

            Vector3 origin = island.transform.position;
            if (!room.Raycast(new Ray(origin, Vector3.down), maxHeight, out var hit, out _))
            {
                _renderer.enabled = false;
                return;
            }

            float height = origin.y - hit.point.y;
            var b = island.LocalBounds;
            float footprint = Mathf.Max(b.size.x, b.size.z) * island.transform.lossyScale.x;
            float size = footprint * (sizeFactor + height * spreadPerMetre);
            float alpha = maxAlpha * Mathf.Clamp01(1f - height / maxHeight);

            _quad.SetPositionAndRotation(hit.point + hit.normal * 0.003f,
                Quaternion.FromToRotation(Vector3.up, hit.normal) * Quaternion.Euler(0f, island.transform.eulerAngles.y, 0f));
            _quad.localScale = new Vector3(size, 1f, size);
            _props.SetFloat(AlphaId, alpha);
            _renderer.SetPropertyBlock(_props);
            _renderer.enabled = alpha > 0.005f;
        }

        void LogPath(int path, string message)
        {
            if (_loggedPath == path) return;
            _loggedPath = path;
            Debug.Log(message);
        }

        static Mesh BuildQuad()
        {
            var mesh = new Mesh { name = "BlobShadowQuad" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f), new Vector3(-0.5f, 0f, 0.5f),
                new Vector3(0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, -0.5f),
            };
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
