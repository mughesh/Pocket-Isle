using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 12c (Oct 27 notes: "water on the hand can be more than just a ball"): the water a cupped hand or both cupped
    /// hands carry at Lilymere's scoop, and the stream it pours. A soft domed sheet (a meniscus) stretched across both palm
    /// centres (its long axis from palm to palm, so its edges follow each palm), or a smaller pool in one palm; it wobbles
    /// when the hands move and thins as it pours. The stream is a ribbon falling on a curve from the sheet's lowest edge to
    /// the pour target, narrowing as it goes. Both are run-time meshes in real metres (the hands are real size), drawn with
    /// <c>Diorama/Water Sheet</c>. Plain class, owned by <see cref="ScoopPour"/>.
    /// </summary>
    public class PalmWater
    {
        const int Rings = 5, Segments = 22, StreamSteps = 14;

        readonly GameObject _sheetGo, _streamGo;
        readonly MeshRenderer _sheetRenderer, _streamRenderer;
        readonly Mesh _sheet, _stream;
        readonly Vector3[] _sheetVerts = new Vector3[1 + Rings * Segments];
        readonly Vector3[] _sheetNormals = new Vector3[1 + Rings * Segments];
        readonly Color[] _sheetColors = new Color[1 + Rings * Segments];
        readonly Vector3[] _streamVerts = new Vector3[StreamSteps * 2];
        readonly Vector3[] _streamNormals = new Vector3[StreamSteps * 2];
        readonly Color[] _streamColors = new Color[StreamSteps * 2];

        /// <summary>Where the sheet's edge is lowest (world), for the stream and the drips.</summary>
        public Vector3 LowEdge { get; private set; }
        public bool Shown => _sheetRenderer != null && _sheetRenderer.enabled;

        public PalmWater(string name, Material material, int layer)
        {
            if (material == null) Debug.LogError($"[PalmWater] {name}: no Water Sheet material (Set Up MR_Main (12c Oct 28 notes)); the water in the hands won't show.");
            _sheet = new Mesh { name = name + " Sheet" };
            _sheet.MarkDynamic();
            _stream = new Mesh { name = name + " Stream" };
            _stream.MarkDynamic();
            _sheetGo = Make(name + " Sheet", _sheet, material, layer, out _sheetRenderer);
            _streamGo = Make(name + " Stream", _stream, material, layer, out _streamRenderer);

            // The sheet: a centre vertex and rings of a disc, triangles fanned out.
            var tris = new int[(Segments + (Rings - 1) * Segments * 2) * 3];
            int k = 0;
            for (int s = 0; s < Segments; s++)
            {
                tris[k++] = 0;
                tris[k++] = 1 + (s + 1) % Segments;
                tris[k++] = 1 + s;
            }
            for (int r = 0; r < Rings - 1; r++)
                for (int s = 0; s < Segments; s++)
                {
                    int a = 1 + r * Segments + s, b = 1 + r * Segments + (s + 1) % Segments;
                    int c = a + Segments, d = b + Segments;
                    tris[k++] = a; tris[k++] = b; tris[k++] = c;
                    tris[k++] = b; tris[k++] = d; tris[k++] = c;
                }
            _sheet.vertices = _sheetVerts;
            _sheet.triangles = tris;

            var st = new int[(StreamSteps - 1) * 6];
            k = 0;
            for (int i = 0; i < StreamSteps - 1; i++)
            {
                int a = i * 2, b = a + 1, c = a + 2, d = a + 3;
                st[k++] = a; st[k++] = c; st[k++] = b;
                st[k++] = b; st[k++] = c; st[k++] = d;
            }
            _stream.vertices = _streamVerts;
            _stream.triangles = st;
        }

        static GameObject Make(string name, Mesh mesh, Material material, int layer, out MeshRenderer renderer)
        {
            var go = new GameObject(name) { layer = layer };
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.enabled = false;
            return go;
        }

        /// <summary>
        /// Draws the sheet: centred on <paramref name="centre"/>, its long axis along <paramref name="axis"/> (half length
        /// <paramref name="halfLong"/>), <paramref name="halfWide"/> across, facing <paramref name="up"/>, domed by
        /// <paramref name="dome"/> (metres); <paramref name="wobble"/> 0..1 ripples it, <paramref name="amount"/> 0..1 thins it.
        /// The lowest edge point (toward <paramref name="tipToward"/>) is kept for the stream.
        /// </summary>
        public void Sheet(bool show, Vector3 centre, Vector3 axis, Vector3 up, float halfLong, float halfWide, float dome, float wobble, float amount,
            Vector3 tipToward)
        {
            if (_sheetRenderer == null) return;
            _sheetRenderer.enabled = show && amount > 0.02f;
            if (!_sheetRenderer.enabled) return;
            up = up.sqrMagnitude > 1e-6f ? up.normalized : Vector3.up;
            axis = Vector3.ProjectOnPlane(axis, up);
            if (axis.sqrMagnitude < 1e-8f) axis = Vector3.ProjectOnPlane(Vector3.right, up);
            axis.Normalize();
            var side = Vector3.Cross(up, axis);
            float t = Time.time;
            float shrink = Mathf.Lerp(0.55f, 1f, amount);
            halfLong *= shrink;
            halfWide *= shrink;
            dome *= amount;
            _sheetVerts[0] = centre + up * dome;
            _sheetNormals[0] = up;
            _sheetColors[0] = new Color(1f, 1f, 1f, Mathf.Lerp(0.5f, 1f, amount));
            float lowest = float.MaxValue;
            var tipDir = Vector3.ProjectOnPlane(tipToward, up);
            for (int r = 0; r < Rings; r++)
            {
                float u = (r + 1f) / Rings;
                for (int s = 0; s < Segments; s++)
                {
                    float ang = s / (float)Segments * Mathf.PI * 2f;
                    float cx = Mathf.Cos(ang), cz = Mathf.Sin(ang);
                    // A meniscus: domed in the middle, rounding down at the rim; ripples travel out when it wobbles.
                    float h = dome * (1f - u * u) + wobble * 0.004f * Mathf.Sin(t * 18f - u * 7f + ang * 2f) * u;
                    var p = centre + axis * (cx * halfLong * u) + side * (cz * halfWide * u) + up * h;
                    int i = 1 + r * Segments + s;
                    _sheetVerts[i] = p;
                    // The rim curls down a little, so the edge catches the fresnel like a real meniscus.
                    _sheetNormals[i] = (up + (axis * cx + side * cz) * (u * u * 0.9f)).normalized;
                    _sheetColors[i] = new Color(1f, 1f, 1f, Mathf.Lerp(0.5f, 1f, amount) * (1f - 0.25f * u * u));
                    if (r == Rings - 1)
                    {
                        float score = Vector3.Dot(p - centre, tipDir) * -1f + Vector3.Dot(p, Vector3.up);
                        if (score < lowest)
                        {
                            lowest = score;
                            LowEdge = p;
                        }
                    }
                }
            }
            _sheet.vertices = _sheetVerts;
            _sheet.normals = _sheetNormals;
            _sheet.colors = _sheetColors;
            _sheet.RecalculateBounds();
        }

        /// <summary>
        /// Draws the pouring stream from <paramref name="from"/> down to <paramref name="to"/> on a falling curve,
        /// <paramref name="width"/> metres wide at the top, narrowing; <paramref name="flow"/> 0..1 fades it.
        /// </summary>
        public void Stream(bool show, Vector3 from, Vector3 to, float width, float flow, Camera cam)
        {
            if (_streamRenderer == null) return;
            _streamRenderer.enabled = show && flow > 0.02f;
            if (!_streamRenderer.enabled) return;
            Vector3 view = cam != null ? cam.transform.position : from + Vector3.back;
            float t = Time.time;
            for (int i = 0; i < StreamSteps; i++)
            {
                float u = i / (StreamSteps - 1f);
                var p = StreamPoint(from, to, u);
                var next = i < StreamSteps - 1 ? StreamPoint(from, to, (i + 1f) / (StreamSteps - 1f)) : p + (p - StreamPoint(from, to, (i - 1f) / (StreamSteps - 1f)));
                var along = (next - p).normalized;
                var toView = (view - p).normalized;
                var across = Vector3.Cross(along, toView).normalized;
                float w = width * Mathf.Lerp(1f, 0.45f, u) * (1f + 0.15f * Mathf.Sin(t * 25f + u * 12f)) * Mathf.Lerp(0.4f, 1f, flow);
                _streamVerts[i * 2] = p - across * w * 0.5f;
                _streamVerts[i * 2 + 1] = p + across * w * 0.5f;
                _streamNormals[i * 2] = _streamNormals[i * 2 + 1] = toView;
                var c = new Color(1f, 1f, 1f, flow * Mathf.Lerp(1f, 0.55f, u));
                _streamColors[i * 2] = _streamColors[i * 2 + 1] = c;
            }
            _stream.vertices = _streamVerts;
            _stream.normals = _streamNormals;
            _stream.colors = _streamColors;
            _stream.RecalculateBounds();
        }

        // Out a little first, then down (gravity): the horizontal move eases in early, the drop late.
        static Vector3 StreamPoint(Vector3 from, Vector3 to, float u) =>
            new Vector3(Mathf.Lerp(from.x, to.x, Mathf.Sqrt(u)), Mathf.Lerp(from.y, to.y, u * u), Mathf.Lerp(from.z, to.z, Mathf.Sqrt(u)));

        public void Hide()
        {
            if (_sheetRenderer != null) _sheetRenderer.enabled = false;
            if (_streamRenderer != null) _streamRenderer.enabled = false;
        }

        public void Destroy()
        {
            if (_sheetGo != null) Object.Destroy(_sheetGo);
            if (_streamGo != null) Object.Destroy(_streamGo);
            if (_sheet != null) Object.Destroy(_sheet);
            if (_stream != null) Object.Destroy(_stream);
        }
    }
}
