using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 19: one credit as a glowing paper sky lantern (the Heart releases them; <see cref="FinaleDirector"/> flies
    /// them). A slightly tapered, chamfered paper box (wider at the top) with dark bamboo ribs at the corners, a bamboo
    /// ring round the open bottom, a tiny flame, the paper glowing from inside (<c>Diorama/Sky Lantern</c>), an additive
    /// halo (the Fx soft dot) and the credit printed on the front panel in dark brown ink. The big one can carry a framed
    /// painting above its text. Built in metres at island scale 1, pivot in its middle, front = -Z (the reader's side).
    /// Plain class; about 150 triangles (+60 with the frame).
    /// </summary>
    public class SkyLantern
    {
        public const float Height = 0.14f, TopHalf = 0.062f, BottomHalf = 0.052f, Chamfer = 0.011f;
        /// <summary>Cap height of the first line (m at island scale 1, normal lantern) and the smallest it may shrink to.</summary>
        public const float CapHeight = 0.012f, MinCapHeight = 0.0085f;

        static readonly int AlphaId = Shader.PropertyToID("_Alpha");
        static readonly int PhaseId = Shader.PropertyToID("_Phase");
        static readonly int MainTexId = Shader.PropertyToID("_MainTex");

        static readonly Color Paper = new Color(1f, 0.9f, 0.72f);
        static readonly Color Bamboo = new Color(0.36f, 0.23f, 0.12f);
        static readonly Color Wood = new Color(0.42f, 0.26f, 0.14f);
        static readonly Color GoldRim = new Color(1f, 0.8f, 0.36f);
        static readonly Color Backing = new Color(1f, 0.86f, 0.62f);
        public static readonly Color Ink = new Color(0.27f, 0.13f, 0.05f);

        readonly Transform _root, _body;
        readonly MeshRenderer _renderer, _frame;
        readonly Texture2D _picture;
        readonly Mesh _haloMesh;
        readonly Transform _halo;
        readonly TextMeshPro _text;
        readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        readonly Color[] _haloColors = new Color[4];
        readonly float _phase;

        public Transform transform => _root;
        public float Size { get; }
        public bool HasPicture => _picture != null;
        /// <summary>The first line's cap height after auto-size (m at island scale 1, this lantern's size included).</summary>
        public float FirstLineCap { get; private set; }
        public string Title { get; }

        static Mesh _sharedBody, _sharedFrame;

        /// <summary>
        /// <paramref name="size"/> 1 = a normal lantern, 1.6 = the big one. <paramref name="picture"/> (optional, the big
        /// one) hangs framed above the text, cropped to <paramref name="crop"/> (uv rect); the lantern shader paints it
        /// (opaque, in the frame's mesh), the texture set per lantern.
        /// </summary>
        public SkyLantern(Transform parent, Material lantern, Material halo, int layer, string title, string subtitle, float size,
                          Texture2D picture = null, Rect crop = default)
        {
            Size = size;
            Title = title;
            _phase = Random.Range(0f, 6.28f);
            _root = new GameObject($"Sky lantern ({title})").transform;
            _root.gameObject.layer = layer;
            _root.SetParent(parent, false);
            _body = new GameObject("Body").transform;
            _body.gameObject.layer = layer;
            _body.SetParent(_root, false);
            _renderer = Renderer(_body, "Paper", SharedBody(), lantern);

            bool framed = picture != null;
            // The text: on the front panel, leaning with the taper. The big one's text sits under the painting.
            float textTop = framed ? -0.004f : Height * 0.5f - 0.012f, textBottom = -Height * 0.5f + 0.014f;
            float yc = (textTop + textBottom) * 0.5f;
            var go = new GameObject("Print");
            go.layer = layer;
            go.transform.SetParent(_body, false);
            float lean = Mathf.Atan2(TopHalf - BottomHalf, Height) * Mathf.Rad2Deg;
            go.transform.localPosition = new Vector3(0f, yc, -HalfAt(yc) - 0.0012f);
            go.transform.localRotation = Quaternion.Euler(-lean, 0f, 0f);
            _text = go.AddComponent<TextMeshPro>();
            if (_text.font == null) _text.font = TMP_Settings.defaultFontAsset;
            var face = _text.font.faceInfo;
            float capPerSize = face.capLine / Mathf.Max(1f, face.pointSize) * 0.1f; // TMP in 3D: 0.1 m per point at pointSize
            _text.text = string.IsNullOrEmpty(subtitle) ? $"<b>{title}</b>" : $"<b>{title}</b>\n<size=80%>{subtitle}</size>";
            _text.alignment = TextAlignmentOptions.Center;
            _text.textWrappingMode = TextWrappingModes.Normal;
            _text.overflowMode = TextOverflowModes.Overflow;
            _text.enableAutoSizing = true;
            _text.fontSizeMax = CapHeight / capPerSize;
            _text.fontSizeMin = MinCapHeight / capPerSize;
            _text.lineSpacing = -12f;
            _text.color = Ink;
            _text.sortingOrder = 4;
            _text.rectTransform.sizeDelta = new Vector2(BottomHalf * 2f - 0.012f, textTop - textBottom);
            _text.ForceMeshUpdate();
            FirstLineCap = _text.fontSize * capPerSize * size;

            if (framed)
            {
                // A small wooden frame with a gold inner rim, hung on the front above the text; the picture inside.
                float aspect = crop.width * picture.width / Mathf.Max(1f, crop.height * picture.height);
                float ph = 0.05f, pw = ph * aspect, fy = Height * 0.5f - 0.012f - ph * 0.5f - 0.006f;
                var frame = new GameObject("Painting").transform;
                frame.gameObject.layer = layer;
                frame.SetParent(_body, false);
                frame.localPosition = new Vector3(0f, fy, -HalfAt(fy) - 0.0005f);
                frame.localRotation = Quaternion.Euler(-lean, 0f, 0f);
                _frame = Renderer(frame, "Frame", SharedFrame(pw, ph, crop), lantern);
                _picture = picture;
            }

            // The halo: a soft additive dot round it, turned to the camera every frame.
            _halo = new GameObject("Halo").transform;
            _halo.gameObject.layer = layer;
            _halo.SetParent(_root, false);
            _haloMesh = Quad(Height * 2.1f, Height * 2.1f, new Rect(0f, 0f, 1f, 1f));
            _haloMesh.MarkDynamic();
            Renderer(_halo, null, _haloMesh, halo);
            _root.gameObject.SetActive(false);
        }

        /// <summary>The paper's half width at height <paramref name="y"/> (pivot in the middle).</summary>
        static float HalfAt(float y) => Mathf.Lerp(BottomHalf, TopHalf, y / Height + 0.5f);

        static MeshRenderer Renderer(Transform parent, string name, Mesh mesh, Material material)
        {
            var go = name == null ? parent.gameObject : new GameObject(name);
            go.layer = parent.gameObject.layer;
            if (name != null) go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            return r;
        }

        /// <summary>
        /// Places it: world middle, yaw (+Z away from the reader), world size of a normal lantern (island scale), fade,
        /// a little roll for the sway. The halo turns to <paramref name="eye"/>.
        /// </summary>
        public void Place(Vector3 world, float yaw, float scale, float alpha, float roll, Vector3 eye)
        {
            bool on = alpha > 0.01f;
            if (_root.gameObject.activeSelf != on) _root.gameObject.SetActive(on);
            if (!on) return;
            float parentScale = _root.parent != null ? _root.parent.lossyScale.x : 1f;
            _root.SetPositionAndRotation(world, Quaternion.Euler(0f, yaw, 0f));
            _root.localScale = Vector3.one * (scale * Size / Mathf.Max(1e-4f, parentScale));
            _body.localRotation = Quaternion.Euler(0f, 0f, roll);
            _block.SetFloat(AlphaId, alpha);
            _block.SetFloat(PhaseId, _phase);
            _renderer.SetPropertyBlock(_block);
            if (_frame != null)
            {
                _block.SetTexture(MainTexId, _picture);
                _frame.SetPropertyBlock(_block);
                _block.Clear();
            }
            _text.color = new Color(Ink.r, Ink.g, Ink.b, alpha);
            Vector3 toEye = eye - _halo.position;
            if (toEye.sqrMagnitude > 1e-6f) _halo.rotation = Quaternion.LookRotation(-toEye, Vector3.up);
            float flick = 1f + 0.06f * Mathf.Sin(Time.time * 9f + _phase);
            var c = new Color(1f, 0.62f, 0.25f, 0.28f * alpha * flick);
            for (int i = 0; i < 4; i++) _haloColors[i] = c;
            _haloMesh.colors = _haloColors;
        }

        public void Destroy()
        {
            if (_root != null) Object.Destroy(_root.gameObject);
            if (_haloMesh != null) Object.Destroy(_haloMesh);
        }

        /// <summary>The shared meshes (the lantern body and the painting's frame) go when the credits end.</summary>
        public static void DestroyShared()
        {
            foreach (var m in new[] { _sharedBody, _sharedFrame })
                if (m != null)
                {
                    if (Application.isPlaying) Object.Destroy(m);
                    else Object.DestroyImmediate(m);
                }
            _sharedBody = _sharedFrame = null;
        }

        /// <summary>Triangles in a lantern body (the report's budget line).</summary>
        public static int BodyTriangles => SharedBody().triangles.Length / 3;
        public static int FrameTriangles => _sharedFrame != null ? _sharedFrame.triangles.Length / 3 : 0;

        // ---- meshes ----

        class Builder
        {
            public readonly List<Vector3> v = new List<Vector3>();
            public readonly List<Vector3> n = new List<Vector3>();
            public readonly List<Vector2> uv = new List<Vector2>();
            public readonly List<Vector2> uv1 = new List<Vector2>();
            public readonly List<Color> c = new List<Color>();
            public readonly List<int> t = new List<int>();

            public int Add(Vector3 p, Vector3 normal, float kind, float h, Color col, float outline, Vector2 tex = default)
            {
                v.Add(p);
                n.Add(normal);
                uv.Add(new Vector2(kind, h));
                uv1.Add(tex);
                // Vertex colours reach the shader as they are: authored in sRGB, so to linear in a linear project.
                var lin = QualitySettings.activeColorSpace == ColorSpace.Linear ? col.linear : col;
                c.Add(new Color(lin.r, lin.g, lin.b, outline));
                return v.Count - 1;
            }

            /// <summary>A quad a-b-c-d (clockwise seen from its front), each corner its own normal.</summary>
            public void Quad(int a, int b, int cc, int d)
            {
                t.Add(a); t.Add(b); t.Add(cc);
                t.Add(a); t.Add(cc); t.Add(d);
            }

            /// <summary>An axis box (min..max) with flat faces; <paramref name="skipBottom"/> leaves the bottom off.</summary>
            public void Box(Vector3 min, Vector3 max, Color col, float outline, bool skipBack = false)
            {
                var ctr = (min + max) * 0.5f;
                void Face(Vector3 nrm, Vector3 a, Vector3 b, Vector3 cc, Vector3 d)
                {
                    // Outline normals point away from the box's middle (a smooth hull for the outline pass).
                    int i0 = Add(a, (a - ctr).normalized, 0f, 0f, col, outline);
                    int i1 = Add(b, (b - ctr).normalized, 0f, 0f, col, outline);
                    int i2 = Add(cc, (cc - ctr).normalized, 0f, 0f, col, outline);
                    int i3 = Add(d, (d - ctr).normalized, 0f, 0f, col, outline);
                    Quad(i0, i1, i2, i3);
                }
                float x0 = min.x, y0 = min.y, z0 = min.z, x1 = max.x, y1 = max.y, z1 = max.z;
                Face(Vector3.back, new Vector3(x0, y0, z0), new Vector3(x0, y1, z0), new Vector3(x1, y1, z0), new Vector3(x1, y0, z0));
                if (!skipBack) Face(Vector3.forward, new Vector3(x1, y0, z1), new Vector3(x1, y1, z1), new Vector3(x0, y1, z1), new Vector3(x0, y0, z1));
                Face(Vector3.left, new Vector3(x0, y0, z1), new Vector3(x0, y1, z1), new Vector3(x0, y1, z0), new Vector3(x0, y0, z0));
                Face(Vector3.right, new Vector3(x1, y0, z0), new Vector3(x1, y1, z0), new Vector3(x1, y1, z1), new Vector3(x1, y0, z1));
                Face(Vector3.up, new Vector3(x0, y1, z0), new Vector3(x0, y1, z1), new Vector3(x1, y1, z1), new Vector3(x1, y1, z0));
                Face(Vector3.down, new Vector3(x0, y0, z1), new Vector3(x0, y0, z0), new Vector3(x1, y0, z0), new Vector3(x1, y0, z1));
            }

            public Mesh Mesh(string name)
            {
                var m = new Mesh { name = name };
                m.SetVertices(v);
                m.SetNormals(n);
                m.SetUVs(0, uv);
                m.SetUVs(1, uv1);
                m.SetColors(c);
                m.SetTriangles(t, 0);
                m.RecalculateBounds();
                return m;
            }
        }

        /// <summary>The octagon (a square with chamfered corners) of half width <paramref name="s"/>, counter-clockwise from above.</summary>
        static Vector2[] Octagon(float s, float ch)
        {
            return new[]
            {
                new Vector2(s - ch, -s), new Vector2(s, -s + ch), new Vector2(s, s - ch), new Vector2(s - ch, s),
                new Vector2(-s + ch, s), new Vector2(-s, s - ch), new Vector2(-s, -s + ch), new Vector2(-s + ch, -s),
            };
        }

        static Mesh SharedBody()
        {
            if (_sharedBody != null) return _sharedBody;
            var b = new Builder();
            float y0 = -Height * 0.5f, y1 = Height * 0.5f;
            var lo = Octagon(BottomHalf, Chamfer);
            var hi = Octagon(TopHalf, Chamfer * 1.15f);
            // The paper walls: per-face corners (flat faces), outline normals radial (smooth hull).
            for (int i = 0; i < 8; i++)
            {
                int j = (i + 1) % 8;
                Vector3 a = new Vector3(lo[i].x, y0, lo[i].y), bb = new Vector3(lo[j].x, y0, lo[j].y);
                Vector3 c = new Vector3(hi[j].x, y1, hi[j].y), d = new Vector3(hi[i].x, y1, hi[i].y);
                int ia = b.Add(a, Radial(a), 1f, 0f, Paper, 1f);
                int ib = b.Add(bb, Radial(bb), 1f, 0f, Paper, 1f);
                int ic = b.Add(c, Radial(c), 1f, 1f, Paper, 1f);
                int id = b.Add(d, Radial(d), 1f, 1f, Paper, 1f);
                // Seen from outside, counter-clockwise from above = a, d, c, b clockwise from the front.
                b.Quad(ia, id, ic, ib);
            }
            // The top: a low rounded roof (a smaller octagon a little higher), then flat.
            var roof = Octagon(TopHalf * 0.8f, Chamfer);
            float yr = y1 + 0.008f;
            for (int i = 0; i < 8; i++)
            {
                int j = (i + 1) % 8;
                Vector3 a = new Vector3(hi[i].x, y1, hi[i].y), bb = new Vector3(hi[j].x, y1, hi[j].y);
                Vector3 c = new Vector3(roof[j].x, yr, roof[j].y), d = new Vector3(roof[i].x, yr, roof[i].y);
                int ia = b.Add(a, Radial(a) + Vector3.up * 0.5f, 1f, 1f, Paper, 1f);
                int ib = b.Add(bb, Radial(bb) + Vector3.up * 0.5f, 1f, 1f, Paper, 1f);
                int ic = b.Add(c, Vector3.up, 1f, 1f, Paper, 1f);
                int id = b.Add(d, Vector3.up, 1f, 1f, Paper, 1f);
                b.Quad(ia, id, ic, ib);
            }
            int top = b.Add(new Vector3(0f, yr, 0f), Vector3.up, 1f, 1f, Paper, 1f);
            var rim = new int[8];
            for (int i = 0; i < 8; i++) rim[i] = b.Add(new Vector3(roof[i].x, yr, roof[i].y), Vector3.up, 1f, 1f, Paper, 1f);
            for (int i = 0; i < 8; i++)
            {
                b.t.Add(top); b.t.Add(rim[(i + 1) % 8]); b.t.Add(rim[i]);
            }
            // The open bottom: the lit inside (no outline), the flame hanging in it.
            int mid = b.Add(new Vector3(0f, y0 + 0.002f, 0f), Vector3.down, 2f, 0f, Paper, 0f);
            var ring = new int[8];
            for (int i = 0; i < 8; i++) ring[i] = b.Add(new Vector3(lo[i].x * 0.97f, y0 + 0.002f, lo[i].y * 0.97f), Vector3.down, 2f, 0f, Paper, 0f);
            for (int i = 0; i < 8; i++)
            {
                b.t.Add(mid); b.t.Add(ring[i]); b.t.Add(ring[(i + 1) % 8]);
            }
            Flame(b, new Vector3(0f, y0 - 0.004f, 0f), 0.006f);
            // Bamboo ribs at the four chamfered corners (thin dark strips just proud of the paper).
            foreach (int i in new[] { 0, 2, 4, 6 })
            {
                // The chamfer faces run lo[i]..lo[i + 1] for even i.
                Vector2 l0 = (lo[i] + lo[i + 1]) * 0.5f, h0 = (hi[i] + hi[i + 1]) * 0.5f;
                Rib(b, new Vector3(l0.x, y0, l0.y), new Vector3(h0.x, y1, h0.y), 0.0032f);
            }
            // The bamboo ring round the bottom.
            for (int i = 0; i < 8; i++)
            {
                int j = (i + 1) % 8;
                Vector3 a = new Vector3(lo[i].x, 0f, lo[i].y) * 1.06f, bb = new Vector3(lo[j].x, 0f, lo[j].y) * 1.06f;
                float r0 = y0 - 0.004f, r1 = y0 + 0.007f;
                int ia = b.Add(a + Vector3.up * r0, Radial(a), 0f, 0f, Bamboo, 0.7f);
                int ib = b.Add(bb + Vector3.up * r0, Radial(bb), 0f, 0f, Bamboo, 0.7f);
                int ic = b.Add(bb + Vector3.up * r1, Radial(bb), 0f, 0f, Bamboo, 0.7f);
                int id = b.Add(a + Vector3.up * r1, Radial(a), 0f, 0f, Bamboo, 0.7f);
                b.Quad(ia, id, ic, ib);
                // Its top and underside lips.
                Vector3 ai = new Vector3(lo[i].x, 0f, lo[i].y) * 0.96f, bi = new Vector3(lo[j].x, 0f, lo[j].y) * 0.96f;
                int ja = b.Add(a + Vector3.up * r1, Vector3.up, 0f, 0f, Bamboo, 0f);
                int jb = b.Add(bb + Vector3.up * r1, Vector3.up, 0f, 0f, Bamboo, 0f);
                int jc = b.Add(bi + Vector3.up * r1, Vector3.up, 0f, 0f, Bamboo, 0f);
                int jd = b.Add(ai + Vector3.up * r1, Vector3.up, 0f, 0f, Bamboo, 0f);
                b.Quad(ja, jd, jc, jb);
                int ka = b.Add(a + Vector3.up * r0, Vector3.down, 0f, 0f, Bamboo * 0.8f, 0f);
                int kb = b.Add(bb + Vector3.up * r0, Vector3.down, 0f, 0f, Bamboo * 0.8f, 0f);
                int kc = b.Add(bi + Vector3.up * r0, Vector3.down, 0f, 0f, Bamboo * 0.8f, 0f);
                int kd = b.Add(ai + Vector3.up * r0, Vector3.down, 0f, 0f, Bamboo * 0.8f, 0f);
                b.Quad(ka, kb, kc, kd);
            }
            _sharedBody = b.Mesh("Sky lantern");
            return _sharedBody;
        }

        static Vector3 Radial(Vector3 p) => new Vector3(p.x, 0f, p.z).normalized;

        static void Rib(Builder b, Vector3 from, Vector3 to, float w)
        {
            Vector3 out0 = Radial(from), out1 = Radial(to);
            Vector3 side = Vector3.Cross(Vector3.up, out0).normalized * (w * 0.5f);
            Vector3 f0 = from + out0 * 0.0015f, f1 = to + out1 * 0.0015f;
            // Front face and two sides (the back is inside the paper).
            int a = b.Add(f0 - side, out0, 0f, 0f, Bamboo, 0.5f), bb = b.Add(f0 + side, out0, 0f, 0f, Bamboo, 0.5f);
            int c = b.Add(f1 + side, out1, 0f, 1f, Bamboo, 0.5f), d = b.Add(f1 - side, out1, 0f, 1f, Bamboo, 0.5f);
            b.Quad(a, bb, c, d);
        }

        static void Flame(Builder b, Vector3 at, float r)
        {
            var tip = at + Vector3.up * r * 1.6f;
            var bot = at - Vector3.up * r * 0.8f;
            var ring = new int[4];
            var up = new int[4];
            var dn = new int[4];
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f;
                var p = at + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r;
                ring[i] = b.Add(p, (p - at).normalized, 3f, 0f, Color.white, 0f);
                up[i] = b.Add(tip, Vector3.up, 3f, 0f, Color.white, 0f);
                dn[i] = b.Add(bot, Vector3.down, 3f, 0f, Color.white, 0f);
            }
            for (int i = 0; i < 4; i++)
            {
                int j = (i + 1) % 4;
                b.t.Add(ring[i]); b.t.Add(up[i]); b.t.Add(ring[j]);
                b.t.Add(ring[j]); b.t.Add(dn[i]); b.t.Add(ring[i]);
            }
        }

        /// <summary>The painting's frame: a warm backing, four wooden bars and a thin gold rim inside them (front = -Z).</summary>
        static Mesh SharedFrame(float pw, float ph, Rect crop)
        {
            if (_sharedFrame != null) return _sharedFrame;
            var b = new Builder();
            float bar = 0.0055f, depth = 0.0035f, gold = 0.0016f;
            float hw = pw * 0.5f, hh = ph * 0.5f;
            // The picture over its warm backing (the picture's own transparent corners show the backing).
            int a = b.Add(new Vector3(-hw, -hh, -0.0012f), Vector3.back, 4f, 0f, Backing, 0f, new Vector2(crop.xMin, crop.yMin));
            int bb = b.Add(new Vector3(-hw, hh, -0.0012f), Vector3.back, 4f, 0f, Backing, 0f, new Vector2(crop.xMin, crop.yMax));
            int c = b.Add(new Vector3(hw, hh, -0.0012f), Vector3.back, 4f, 0f, Backing, 0f, new Vector2(crop.xMax, crop.yMax));
            int d = b.Add(new Vector3(hw, -hh, -0.0012f), Vector3.back, 4f, 0f, Backing, 0f, new Vector2(crop.xMax, crop.yMin));
            b.Quad(a, bb, c, d);
            // Wooden bars (outlined).
            b.Box(new Vector3(-hw - bar, hh, -depth), new Vector3(hw + bar, hh + bar, 0f), Wood, 0.5f, true);
            b.Box(new Vector3(-hw - bar, -hh - bar, -depth), new Vector3(hw + bar, -hh, 0f), Wood, 0.5f, true);
            b.Box(new Vector3(-hw - bar, -hh, -depth), new Vector3(-hw, hh, 0f), Wood, 0.5f, true);
            b.Box(new Vector3(hw, -hh, -depth), new Vector3(hw + bar, hh, 0f), Wood, 0.5f, true);
            // The gold inner rim: four thin strips on the bars' inner edge, a hair in front.
            float z = -depth - 0.0004f;
            void Strip(float x0, float y0, float x1, float y1)
            {
                int i0 = b.Add(new Vector3(x0, y0, z), Vector3.back, 0f, 0f, GoldRim, 0f);
                int i1 = b.Add(new Vector3(x0, y1, z), Vector3.back, 0f, 0f, GoldRim, 0f);
                int i2 = b.Add(new Vector3(x1, y1, z), Vector3.back, 0f, 0f, GoldRim, 0f);
                int i3 = b.Add(new Vector3(x1, y0, z), Vector3.back, 0f, 0f, GoldRim, 0f);
                b.Quad(i0, i1, i2, i3);
            }
            Strip(-hw - gold * 0.3f, hh - gold * 0.3f, hw + gold * 0.3f, hh + gold);
            Strip(-hw - gold * 0.3f, -hh - gold, hw + gold * 0.3f, -hh + gold * 0.3f);
            Strip(-hw - gold, -hh, -hw + gold * 0.3f, hh);
            Strip(hw - gold * 0.3f, -hh, hw + gold, hh);
            _sharedFrame = b.Mesh("Painting frame");
            return _sharedFrame;
        }

        /// <summary>A quad facing -Z (w x h, centred) with <paramref name="uv"/> as its texture rect.</summary>
        static Mesh Quad(float w, float h, Rect uv)
        {
            var m = new Mesh { name = "Lantern quad" };
            m.vertices = new[]
            {
                new Vector3(-w * 0.5f, -h * 0.5f, 0f), new Vector3(-w * 0.5f, h * 0.5f, 0f),
                new Vector3(w * 0.5f, h * 0.5f, 0f), new Vector3(w * 0.5f, -h * 0.5f, 0f),
            };
            m.uv = new[] { new Vector2(uv.xMin, uv.yMin), new Vector2(uv.xMin, uv.yMax), new Vector2(uv.xMax, uv.yMax), new Vector2(uv.xMax, uv.yMin) };
            m.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            m.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            m.RecalculateBounds();
            return m;
        }
    }
}
