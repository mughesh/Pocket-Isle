using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 14f: the mossy carved stone tablet every button wears (<see cref="RuneBoard"/>, <see cref="BattleRunes"/>).
    /// One shared procedural mesh for every tablet in the game, drawn with Diorama/Tablet (Materials/Tablet, GPU
    /// instanced: the tablets of a board are one draw). The shader stretches it to each tablet's size (anchors + fixed
    /// offsets), so corners, bevel, moss and inlay keep their size. Board-local metres, the front faces -z.
    /// Parts (vertex colour alpha): stone slab with a bevel, moss lumps on the top edge, a carved inlay groove round the
    /// face (gold on the primary tablet), a dark rim behind (the outline; gold when looked at), and a halo behind that:
    /// a soft dark shadow and a faint warm glow, so it never blends into a white wall or a dark room.
    /// </summary>
    public static class TabletKit
    {
        /// <summary>Corner radius, bevel, thickness (m). The material's _Corner must match <see cref="Corner"/>.</summary>
        // Step 14g: lighter for the pre-14f rune sizes (7 x 3.4 cm): a thinner slab, a narrower bevel, smaller moss and halo.
        public const float Corner = 0.0045f, Bevel = 0.001f, Thickness = 0.0035f;
        /// <summary>Labels sit this far in front of the slab's back (in front of its face).</summary>
        public const float LabelZ = -(Thickness + 0.001f);
        /// <summary>How far a press sinks the slab (m).</summary>
        public const float Sink = 0.0025f;

        /// <summary>The carved label's fill (warm cream) and its carved edge.</summary>
        public static readonly Color LabelColor = new Color(0.97f, 0.91f, 0.75f);
        public static readonly Color LabelLit = new Color(1f, 0.96f, 0.82f);
        /// <summary>Step 14g: a label on a gold-filled (selected) tablet.</summary>
        public static readonly Color LabelOnGold = new Color(0.2f, 0.14f, 0.07f);
        public static readonly Color32 LabelEdge = new Color32(36, 28, 18, 255);
        /// <summary>The slab face as the shader draws it lit (sRGB, for the contrast check).</summary>
        public static readonly Color FaceColor = new Color(0.30f, 0.33f, 0.29f);
        public static readonly Color GoldInlay = new Color(1f, 0.8f, 0.36f);

        static readonly int SizeId = Shader.PropertyToID("_Size");
        static readonly int StateId = Shader.PropertyToID("_State");
        static readonly int InlayId = Shader.PropertyToID("_Inlay");
        static readonly int FaceId = Shader.PropertyToID("_Face");
        static Mesh _mesh;

        /// <summary>One tablet: its renderer and how it looks now.</summary>
        public class View
        {
            public MeshRenderer renderer;
            public Transform transform;
            public Vector2 size;
            /// <summary>Inlay colour and how much it shows (primary gold 1; 0 = a plain carved groove).</summary>
            public Color inlay;
            /// <summary>Nov 4: the stone face's colour (a = how much; 0 = plain stone): element tablets, battle moves.</summary>
            public Color face;
            readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
            Vector4 _state = new Vector4(-1f, 0f, 0f, 0f);
            Color _inlay, _face;

            /// <summary>hover 0..1 (gold rim, brighter), dim 0..1 (disabled), fill 0..1 (hold runes), glow 0..1 (a pulse).</summary>
            public void Paint(float hover, float dim, float fill, float glow)
            {
                var s = new Vector4(hover, dim, fill, glow);
                if (s == _state && inlay == _inlay && face == _face) return;
                _state = s;
                _inlay = inlay;
                _face = face;
                _block.SetVector(SizeId, new Vector4(size.x, size.y, 0f, 0f));
                _block.SetVector(StateId, s);
                _block.SetColor(InlayId, inlay);
                _block.SetColor(FaceId, face);
                renderer.SetPropertyBlock(_block);
            }

            /// <summary>Pressed 0..1: the slab sinks back a few millimetres.</summary>
            public void Press(float pressed) => Press(pressed, 0f);

            /// <summary>Step 14k: pressed 0..1 sinks it; <paramref name="lift"/> (board-local m) raises it toward the player (hover pop).</summary>
            public void Press(float pressed, float lift) => transform.localPosition = new Vector3(0f, 0f, Sink * Mathf.Clamp01(pressed) - lift);
        }

        /// <summary>A tablet under <paramref name="parent"/> (its centre), <paramref name="size"/> board-local metres.</summary>
        public static View Make(Transform parent, Material material, Vector2 size, Color inlay)
        {
            var go = new GameObject("Tablet");
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = SharedMesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            var v = new View { renderer = r, transform = go.transform, size = size, inlay = inlay };
            v.Paint(0f, 0f, 0f, 0f);
            return v;
        }

        /// <summary>A label carved into a tablet: warm cream with a dark carved edge, just in front of the face.</summary>
        public static void StyleLabel(TextMeshPro t)
        {
            t.color = LabelColor;
            t.outlineWidth = 0.24f;
            t.outlineColor = LabelEdge;
            var p = t.transform.localPosition;
            t.transform.localPosition = new Vector3(p.x, p.y, LabelZ);
        }

        // ---- contrast (WCAG 2), pure ----

        /// <summary>Relative luminance of an sRGB colour.</summary>
        public static float Luminance(Color c)
        {
            float L(float v) => v <= 0.03928f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);
            return 0.2126f * L(c.r) + 0.7152f * L(c.g) + 0.0722f * L(c.b);
        }

        /// <summary>Contrast ratio of two sRGB colours (1..21).</summary>
        public static float Contrast(Color a, Color b)
        {
            float la = Luminance(a), lb = Luminance(b);
            return (Mathf.Max(la, lb) + 0.05f) / (Mathf.Min(la, lb) + 0.05f);
        }

        // ---- the mesh ----

        public static Mesh SharedMesh => _mesh != null ? _mesh : _mesh = Build();

        const int Seg = 5; // segments per corner

        static readonly List<Vector3> P = new List<Vector3>();
        static readonly List<Vector3> N = new List<Vector3>();
        static readonly List<Color> C = new List<Color>();
        static readonly List<Vector2> A = new List<Vector2>();
        static readonly List<int> T = new List<int>();

        const float Stone = 0f, Moss = 0.25f, Inlay = 0.5f, Rim = 0.75f, Halo = 1f;

        /// <summary>Builds the shared tablet mesh (about 600 vertices).</summary>
        public static Mesh Build()
        {
            P.Clear(); N.Clear(); C.Clear(); A.Clear(); T.Clear();
            float r = Corner;
            // The halo first (blends with what's behind), then the rim, the slab, the inlay, the moss.
            HaloRings(r);
            Disk(r + 0.0011f, 0.0004f, new Color(0f, 0f, 0f, Rim));
            Slab(r);
            Ring(r - 0.0022f - 0.0009f, r - 0.0022f, -Thickness - 0.0002f, new Color(1f, 1f, 1f, Inlay));
            MossLumps(r);
            var m = new Mesh { name = "Tablet (shared)" };
            m.SetVertices(P);
            m.SetNormals(N);
            m.SetColors(C);
            m.SetUVs(1, A);
            m.SetTriangles(T, 0);
            // The shader moves every vertex by up to half a tablet: bounds big enough for the largest board tablet.
            m.bounds = new Bounds(Vector3.zero, new Vector3(0.6f, 0.3f, 0.05f));
            m.UploadMeshData(true);
            return m;
        }

        /// <summary>Ring point i (0..Ring-1) of a rounded rectangle: its corner anchor and the offset at radius 1.</summary>
        static void RingPoint(int i, out Vector2 anchor, out Vector2 dir)
        {
            int corner = i / (Seg + 1), k = i % (Seg + 1);
            float a = (corner * 90f + k * 90f / Seg) * Mathf.Deg2Rad;
            anchor = corner switch { 0 => new Vector2(1f, 1f), 1 => new Vector2(-1f, 1f), 2 => new Vector2(-1f, -1f), _ => new Vector2(1f, -1f) };
            dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        static int RingCount => 4 * (Seg + 1);

        static int V(Vector3 p, Vector3 n, Color c, Vector2 anchor)
        {
            P.Add(p);
            N.Add(n);
            C.Add(c);
            A.Add(anchor);
            return P.Count - 1;
        }

        /// <summary>A ring of vertices at radius <paramref name="radius"/>, depth z (normals from <paramref name="normal"/>).</summary>
        static int AddRing(float radius, float z, Color c, System.Func<Vector2, Vector3> normal, float dy = 0f, bool vary = false)
        {
            int first = P.Count;
            for (int i = 0; i < RingCount; i++)
            {
                RingPoint(i, out var anchor, out var dir);
                var col = c;
                if (vary)
                {
                    float h = 0.94f + 0.12f * Hash(i * 7.3f + radius * 911f);
                    col = new Color(h, h, h, c.a);
                }
                V(new Vector3(dir.x * radius, dir.y * radius + dy, z), normal(dir), col, anchor);
            }
            return first;
        }

        /// <summary>Quads between two rings; front-facing (clockwise from the front) when <paramref name="a"/> is the outer ring.</summary>
        static void Strip(int a, int b)
        {
            int n = RingCount;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                T.Add(a + i); T.Add(b + i); T.Add(b + j);
                T.Add(a + i); T.Add(b + j); T.Add(a + j);
            }
        }

        static void Fan(int center, int ring)
        {
            int n = RingCount;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                T.Add(center); T.Add(ring + j); T.Add(ring + i);
            }
        }

        /// <summary>Step 14j: how far the halo reaches past the slab's edge (was 8 mm + a 1.6 mm drop: neighbours' halos
        /// met in an 8-12 mm gap and the tablets read as overlapping). Under half of <see cref="TabletLayout.MinGap"/>.</summary>
        public const float HaloReach = 0.0035f;

        static void HaloRings(float r)
        {
            const float dy = -0.0008f, z = 0.003f;
            var back = Vector3.back;
            int c = V(new Vector3(0f, dy, z), back, new Color(1f, 0f, 0f, Halo), Vector2.zero);
            int a = AddRing(r, z, new Color(1f, 0f, 0f, Halo), d => back, dy);
            int b = AddRing(r + 0.0009f, z, new Color(0.55f, 0.45f, 0f, Halo), d => back, dy);
            int e = AddRing(r + 0.002f, z, new Color(0f, 0.6f, 0f, Halo), d => back, dy);
            int f = AddRing(r + HaloReach - 0.0008f, z, new Color(0f, 0f, 0f, Halo), d => back, dy);
            Fan(c, a);
            Strip(b, a);
            Strip(e, b);
            Strip(f, e);
        }

        static void Disk(float radius, float z, Color col)
        {
            int c = V(new Vector3(0f, 0f, z), Vector3.back, col, Vector2.zero);
            int ring = AddRing(radius, z, col, d => Vector3.back);
            Fan(c, ring);
        }

        static void Slab(float r)
        {
            var stone = new Color(1f, 1f, 1f, Stone);
            int c = V(new Vector3(0f, 0f, -Thickness), Vector3.back, stone, Vector2.zero);
            int face = AddRing(r - Bevel, -Thickness, stone, d => Vector3.back, 0f, true);
            int bevel = AddRing(r, -Thickness + Bevel, stone, d => new Vector3(d.x, d.y, -1f).normalized, 0f, true);
            int bevel2 = AddRing(r, -Thickness + Bevel, stone, d => new Vector3(d.x, d.y, 0f), 0f, true);
            int back = AddRing(r, 0f, stone, d => new Vector3(d.x, d.y, 0f), 0f, true);
            Fan(c, face);
            Strip(bevel, face);
            Strip(back, bevel2);
        }

        /// <summary>A flat ring on the face between two radii (the inlay groove).</summary>
        static void Ring(float inner, float outer, float z, Color col)
        {
            int a = AddRing(outer, z, col, d => Vector3.back);
            int b = AddRing(inner, z, col, d => Vector3.back);
            Strip(a, b);
        }

        /// <summary>Soft moss lumps along the top edge (fixed, so every tablet carries the same tuft pattern, stretched).</summary>
        static void MossLumps(float r)
        {
            // x along the top edge (-1..1 of the straight part), width, height (m).
            var lumps = new (float x, float w, float h)[]
            {
                (-0.95f, 0.0085f, 0.0022f), (-0.8f, 0.0065f, 0.0017f), (-0.66f, 0.0075f, 0.002f),
                (0.3f, 0.0065f, 0.0018f), (0.5f, 0.009f, 0.0024f), (0.7f, 0.006f, 0.0016f),
            };
            int i = 0;
            foreach (var l in lumps)
            {
                float shade = 0.85f + 0.3f * Hash(i++ * 3.1f);
                Lump(new Vector2(l.x, 1f), new Vector3(0f, r - 0.0004f, -Thickness * 0.5f), l.w, l.h, Thickness * 1.1f,
                     new Color(shade, shade, shade, Moss));
            }
        }

        /// <summary>A flattened dome on the top edge (6 around, 2 rings and a top).</summary>
        static void Lump(Vector2 anchor, Vector3 at, float w, float h, float d, Color col)
        {
            const int around = 8;
            int first = P.Count;
            for (int ring = 0; ring < 2; ring++)
            {
                float el = ring == 0 ? 0f : 50f * Mathf.Deg2Rad;
                for (int k = 0; k < around; k++)
                {
                    float a = k * Mathf.PI * 2f / around;
                    var n = new Vector3(Mathf.Cos(a) * Mathf.Cos(el), Mathf.Sin(el), Mathf.Sin(a) * Mathf.Cos(el));
                    V(at + new Vector3(n.x * w * 0.5f, n.y * h, n.z * d * 0.5f), n, col, anchor);
                }
            }
            int top = V(at + new Vector3(0f, h, 0f), Vector3.up, col, anchor);
            for (int k = 0; k < around; k++)
            {
                int k2 = (k + 1) % around;
                int a0 = first + k, a1 = first + k2, b0 = first + around + k, b1 = first + around + k2;
                T.Add(a0); T.Add(b1); T.Add(a1);
                T.Add(a0); T.Add(b0); T.Add(b1);
                T.Add(b0); T.Add(top); T.Add(b1);
            }
        }

        // ---- step 14f: the speaker icon (the game menu's music toggle) ----

        static Mesh _speakerOn, _speakerOff;

        /// <summary>A little speaker drawn for a 3 cm tablet (on: two sound waves; off: an X), front-facing, drawn as inlay.</summary>
        public static Mesh Speaker(bool on)
        {
            if (on && _speakerOn != null) return _speakerOn;
            if (!on && _speakerOff != null) return _speakerOff;
            P.Clear(); N.Clear(); C.Clear(); A.Clear(); T.Clear();
            var col = new Color(1f, 1f, 1f, Inlay);
            Poly(col, new Vector2(-0.0085f, -0.003f), new Vector2(-0.0085f, 0.003f), new Vector2(-0.0045f, 0.003f), new Vector2(-0.0045f, -0.003f));
            Poly(col, new Vector2(-0.0045f, -0.003f), new Vector2(-0.0045f, 0.003f), new Vector2(0.0005f, 0.0072f), new Vector2(0.0005f, -0.0072f));
            if (on)
            {
                Arc(col, new Vector2(0.001f, 0f), 0.0042f, 0.0056f);
                Arc(col, new Vector2(0.001f, 0f), 0.0076f, 0.009f);
            }
            else
            {
                Bar(col, new Vector2(0.0032f, -0.0042f), new Vector2(0.0092f, 0.0042f), 0.0008f);
                Bar(col, new Vector2(0.0032f, 0.0042f), new Vector2(0.0092f, -0.0042f), 0.0008f);
            }
            var m = new Mesh { name = on ? "Speaker (on)" : "Speaker (off)" };
            m.SetVertices(P);
            m.SetNormals(N);
            m.SetColors(C);
            m.SetUVs(1, A);
            m.SetTriangles(T, 0);
            m.RecalculateBounds();
            m.UploadMeshData(true);
            return on ? _speakerOn = m : _speakerOff = m;
        }

        static Mesh _menuBars;

        /// <summary>Step 16: the menu stone's icon, three bars ("≡") drawn for a 3 cm tablet, front-facing, as inlay.</summary>
        public static Mesh MenuBars()
        {
            if (_menuBars != null) return _menuBars;
            P.Clear(); N.Clear(); C.Clear(); A.Clear(); T.Clear();
            var col = new Color(1f, 1f, 1f, Inlay);
            foreach (float y in new[] { -0.0055f, 0f, 0.0055f })
                Poly(col, new Vector2(-0.0075f, y - 0.0012f), new Vector2(-0.0075f, y + 0.0012f), new Vector2(0.0075f, y + 0.0012f), new Vector2(0.0075f, y - 0.0012f));
            var m = new Mesh { name = "Menu bars" };
            m.SetVertices(P);
            m.SetNormals(N);
            m.SetColors(C);
            m.SetUVs(1, A);
            m.SetTriangles(T, 0);
            m.RecalculateBounds();
            m.UploadMeshData(true);
            return _menuBars = m;
        }

        /// <summary>A convex polygon, points clockwise as seen from the front.</summary>
        static void Poly(Color col, params Vector2[] pts)
        {
            int first = P.Count;
            foreach (var p in pts) V(new Vector3(p.x, p.y, 0f), Vector3.back, col, Vector2.zero);
            for (int i = 1; i < pts.Length - 1; i++)
            {
                T.Add(first); T.Add(first + i); T.Add(first + i + 1);
            }
        }

        /// <summary>A sound wave: a ring segment from -50 to 50 degrees.</summary>
        static void Arc(Color col, Vector2 c, float r0, float r1)
        {
            const int steps = 6;
            for (int i = 0; i < steps; i++)
            {
                float a1 = Mathf.Lerp(-50f, 50f, i / (float)steps) * Mathf.Deg2Rad, a2 = Mathf.Lerp(-50f, 50f, (i + 1) / (float)steps) * Mathf.Deg2Rad;
                Vector2 d1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)), d2 = new Vector2(Mathf.Cos(a2), Mathf.Sin(a2));
                Poly(col, c + d1 * r0, c + d2 * r0, c + d2 * r1, c + d1 * r1);
            }
        }

        static void Bar(Color col, Vector2 a, Vector2 b, float halfWidth)
        {
            var d = (b - a).normalized;
            var n = new Vector2(-d.y, d.x) * halfWidth;
            Poly(col, a - n, a + n, b + n, b - n);
        }

        static float Hash(float x)
        {
            float s = Mathf.Sin(x * 12.9898f) * 43758.5453f;
            return s - Mathf.Floor(s);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _mesh = _speakerOn = _speakerOff = null;
    }
}
