using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 14a: soft sunrays round the Lantern Spirit while an objective is active (a lesson or a hand moment teaching;
    /// off for chatter): warm gold rays on an additive billboard about twice the Spirit's size, turning slowly with a
    /// gentle pulse. One mesh, one draw call (the Grab Frame material: additive, soft across each ray).
    /// </summary>
    [DefaultExecutionOrder(210)] // after the Spirit has moved this frame
    public class SpiritSunrays : MonoBehaviour
    {
        public LanternSpirit spirit;
        [Tooltip("Diorama/Grab Frame material (additive).")]
        public Material material;
        [Range(4, 24)] public int rays = 12;
        [Tooltip("Ray length from the Spirit's middle (x the Spirit's height): inner, outer.")]
        public Vector2 reach = new Vector2(0.3f, 1.05f);
        [Tooltip("Ray width at the root (x the Spirit's height).")]
        public float rootWidth = 0.16f;
        public Color color = new Color(1f, 0.8f, 0.4f);
        [Range(0f, 2f)] public float brightness = 0.55f;
        [Tooltip("Turns per second.")]
        public float spin = 0.04f;
        public float pulseSpeed = 1.3f;
        [Range(0f, 1f)] public float pulseAmount = 0.18f;
        public float fadeSeconds = 0.4f;

        static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        Mesh _mesh;
        MaterialPropertyBlock _block;
        float _level;
        CreatureView _view;

        void Start()
        {
            if (spirit == null || material == null)
            {
                Debug.LogError("[SpiritSunrays] Needs the Spirit and the Grab Frame material (Set Up MR_Main (14a)): no sunrays.", this);
                enabled = false;
                return;
            }
            _mesh = Build();
        }

        void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
        }

        /// <summary>A star of soft rays in the XY plane, radius 1 (alpha fades toward the tips; uv.y across each ray).</summary>
        Mesh Build()
        {
            var verts = new System.Collections.Generic.List<Vector3>();
            var cols = new System.Collections.Generic.List<Color>();
            var uvs = new System.Collections.Generic.List<Vector2>();
            var tris = new System.Collections.Generic.List<int>();
            float inner = reach.x / reach.y;
            for (int i = 0; i < rays; i++)
            {
                float a = Mathf.PI * 2f * i / rays;
                // Alternate long and short rays.
                float len = i % 2 == 0 ? 1f : 0.72f;
                var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                var side = new Vector3(-d.y, d.x, 0f) * (rootWidth / reach.y * 0.5f);
                int b = verts.Count;
                verts.Add(d * inner - side);
                verts.Add(d * inner + side);
                verts.Add(d * len - side * 0.25f);
                verts.Add(d * len + side * 0.25f);
                var root = new Color(color.r, color.g, color.b, 1f);
                var tip = new Color(color.r, color.g, color.b, 0f);
                cols.Add(root);
                cols.Add(root);
                cols.Add(tip);
                cols.Add(tip);
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(0f, 1f));
                uvs.Add(new Vector2(1f, 0f));
                uvs.Add(new Vector2(1f, 1f));
                tris.AddRange(new[] { b, b + 2, b + 1, b + 1, b + 2, b + 3 });
            }
            var mesh = new Mesh { name = "Spirit Sunrays" };
            mesh.SetVertices(verts);
            mesh.SetColors(cols);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        void LateUpdate()
        {
            bool on = !string.IsNullOrEmpty(Dialogue.CurrentObjective) && spirit.Appear > 0.5f && !SceneSwap.IsBusy;
            _level = Mathf.MoveTowards(_level, on ? 1f : 0f, Time.deltaTime / Mathf.Max(0.01f, fadeSeconds));
            if (_level <= 0.001f) return;
            var cam = Camera.main;
            if (cam == null) return;
            if (_view == null) _view = spirit.GetComponentInChildren<CreatureView>();
            float height = (_view != null ? _view.height : 0.05f) * spirit.transform.lossyScale.x * (spirit.Spark ? spirit.sparkScale : 1f);
            Vector3 centre = spirit.transform.position + Vector3.up * (height * 0.5f);
            float pulse = 1f + pulseAmount * Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f);
            var face = Quaternion.LookRotation(centre - cam.transform.position, cam.transform.up) * Quaternion.Euler(0f, 0f, Time.time * spin * 360f);
            var m = Matrix4x4.TRS(centre, face, Vector3.one * (height * reach.y * pulse));
            _block ??= new MaterialPropertyBlock();
            _block.SetFloat(IntensityId, brightness * _level * pulse);
            var rp = new RenderParams(material) { layer = spirit.gameObject.layer, shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off, matProps = _block };
            Graphics.RenderMesh(rp, _mesh, 0, m);
        }
    }
}
