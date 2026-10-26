using System.Collections.Generic;
using UnityEngine;
using static Diorama.EditorTools.StarterKit;

namespace Diorama.EditorTools
{
    /// <summary>
    /// The title logo. Step 9h (v2, built from the island's own art so it belongs to the game): two props baked in the
    /// same shape units and drawn together by <see cref="TitleLogo"/>:
    /// - <see cref="LogoName"/>, the letters: chunky rounded capsule strokes, slightly uneven heights and a small tilt per
    ///   letter. "Pocket" warm amber with a lighter top band and a darker lower band; "Isle" grass green with a cream band
    ///   and moss caps dripping over the top edges; the I's dot is a tiny glowing lantern (the Spirit's gold).
    /// - <see cref="LogoBaseName"/>, a mini Still Isle chunk: a grid of tile columns (grass caps, a path strip, grey earth
    ///   sides), a rocky underside of uneven depth, tall-grass tufts, and the hub's own tree, statue and lantern shapes
    ///   (their SDF shapes reused, scaled down).
    /// No font: letters are strokes (lines and elliptic arcs). Shape units: 1 = the height of a capital "P". Seen from the
    /// front (+Z) the viewer's right is -X, so text runs toward -X.
    /// </summary>
    public static partial class PropKit
    {
        public const string LogoName = "TitleLogo";
        public const string LogoBaseName = "TitleLogoBase";

        /// <summary>The letters (step 9h v2).</summary>
        public static PropRecipe WriteLogo()
        {
            DioramaSceneSetup.EnsureFolder(Root);
            DioramaSceneSetup.EnsureFolder($"{Root}/Shapes");
            var logo = Recipe(LogoName, Vector2Int.one, false, false, 4500);
            logo.resolution = 48;
            logo.outline = 0.035f;
            logo.spec = 0.2f;
            logo.scaleJitter = 0f;
            logo.yawJitter = 0f;
            // Clay amber body, Wood dark amber (lower band, lantern cap), Stone warm cream (bands), Leaf grass green,
            // Glow the lantern, Accent moss.
            logo.palette = Palette(H("#F2A33A"), H("#B9651F"), H("#FCE6B4"), H("#6CB94A"), H("#FFD27A", 1.2f), H("#3F7D38"));

            var pieces = new List<PropPiece>();
            // "Isle" in front on the grass, "Pocket" above and a little behind it.
            Word(pieces, "Isle", 1.3f, 0.18f, 0.12f, 0.2f, 0.05f, LetterStyle.Isle, 11);
            Word(pieces, "Pocket", 0.95f, 0.14f, 1.86f, -0.08f, 0.035f, LetterStyle.Pocket, 3);

            logo.pieces = pieces;
            UnityEditor.EditorUtility.SetDirty(logo);
            return logo;
        }

        /// <summary>The mini Still Isle chunk the letters stand on (step 9h v2).</summary>
        public static PropRecipe WriteLogoBase()
        {
            DioramaSceneSetup.EnsureFolder(Root);
            DioramaSceneSetup.EnsureFolder($"{Root}/Shapes");
            var b = Recipe(LogoBaseName, Vector2Int.one, false, false, 3000);
            // Coarse: each piece is meshed on its own grid, and flat tile caps on a fine one keep thousands of triangles.
            b.resolution = 18;
            b.outline = 0.03f;
            b.spec = 0.15f;
            b.scaleJitter = 0f;
            b.yawJitter = 0f;
            // Clay path, Wood trunk, Stone earth / rock / statue, Leaf grass, Glow lantern light, Accent the hub tree's canopy.
            b.palette = Palette(H("#EEE3C6"), H("#6B5442"), H("#9C978A"), H("#93BE6B"), H("#FFD98A", 0.9f), H("#4E9A84"));

            var pieces = new List<PropPiece>();
            const int cols = 6, rows = 3;
            const float tile = 0.7f, gap = 0.012f;
            var rng = new System.Random(9);
            float R(float a, float c) => a + (float)rng.NextDouble() * (c - a);
            var rock = new List<SdfPrimitive>();
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    float x = (c - (cols - 1) * 0.5f) * tile, z = (r - (rows - 1) * 0.5f) * tile;
                    // The front row's middle tiles are the path.
                    bool path = r == rows - 1 && (c == 2 || c == 3) || r == 1 && c == 2;
                    float depth = R(0.28f, 0.42f);
                    pieces.Add(Weighted(PShape($"LogoBase_Col{r}{c}",
                        Box(new Vector3(x, -depth * 0.5f - 0.05f, z), new Vector3(tile * 0.5f - gap, depth * 0.5f, tile * 0.5f - gap), 0.035f, 0f)),
                        PropSlot.Stone, 0.5f));
                    pieces.Add(Weighted(PShape($"LogoBase_Cap{r}{c}",
                        Box(new Vector3(x, -0.04f, z), new Vector3(tile * 0.5f - gap * 0.5f, 0.045f, tile * 0.5f - gap * 0.5f), 0.03f, 0f)),
                        path ? PropSlot.Clay : PropSlot.Leaf, 0.6f));
                    // The rocky underside hangs deeper toward the middle, unevenly.
                    float mid = 1f - Mathf.Abs(c - (cols - 1) * 0.5f) / (cols * 0.5f);
                    rock.Add(Ell(new Vector3(x + R(-0.1f, 0.1f), -0.45f - mid * R(0.25f, 0.45f), z * 0.7f),
                        new Vector3(tile * 0.55f, 0.2f + mid * R(0.15f, 0.3f), tile * 0.45f), 0.15f).Bumps(0.05f, 5f));
                }
            rock.Add(Ell(new Vector3(0.1f, -1.25f, 0f), new Vector3(0.6f, 0.35f, 0.4f), 0.3f).Bumps(0.05f, 6f));
            rock.Add(new SdfPrimitive { kind = SdfKind.Plane, blend = SdfBlend.Intersect, a = new Vector3(0f, -0.32f, 0f), b = Vector3.up, k = 0.03f });
            pieces.Add(Weighted(PShape("LogoBase_Rock", rock.ToArray()), PropSlot.Stone, 2.2f));

            // Tall-grass tufts (chunky blades, the grass shader's colour) at the front left and back right.
            var tufts = new List<SdfPrimitive>();
            foreach (var at in new[] { new Vector2(1.75f, 0.45f), new Vector2(1.45f, 0.6f), new Vector2(-1.25f, -0.6f) })
                for (int i = 0; i < 4; i++)
                {
                    var foot = new Vector3(at.x + R(-0.12f, 0.12f), 0f, at.y + R(-0.1f, 0.1f));
                    var tip = foot + new Vector3(R(-0.08f, 0.08f), R(0.3f, 0.45f), R(-0.05f, 0.05f));
                    tufts.Add(Cone(foot, tip, 0.06f, 0.015f, tufts.Count == 0 ? 0f : 0.02f));
                }
            pieces.Add(Weighted(PShape("LogoBase_Tufts", tufts.ToArray()), PropSlot.Leaf, 0.6f));

            // The hub's own props, reused: the tree at the back right, the statue at the back left, a lantern at the front right.
            Reuse(pieces, "CalmTree", new Vector3(-1.85f, 0f, -0.45f), 1.5f, s => s == PropSlot.Leaf ? PropSlot.Accent : s == PropSlot.Wood ? PropSlot.Wood : (PropSlot?)null);
            Reuse(pieces, "Statue", new Vector3(1.8f, 0f, -0.45f), 1.0f, s => s == PropSlot.Stone ? PropSlot.Stone : (PropSlot?)null);
            Reuse(pieces, "HubLantern", new Vector3(-1.95f, 0f, 0.5f), 0.65f, s => s == PropSlot.Glow ? PropSlot.Glow : PropSlot.Stone);

            b.pieces = pieces;
            UnityEditor.EditorUtility.SetDirty(b);
            return b;
        }

        /// <summary>
        /// Copies a baked prop's pieces (its SDF shapes) onto the logo base: <paramref name="height"/> tall, standing on
        /// <paramref name="at"/>, slots mapped into the base palette (null = left out).
        /// </summary>
        static void Reuse(List<PropPiece> pieces, string recipe, Vector3 at, float height, System.Func<PropSlot, PropSlot?> map)
        {
            var src = Load(recipe);
            if (src == null || src.bakedMesh == null)
            {
                Debug.LogError($"[PropKit] Logo base: {recipe} isn't baked; left out.");
                return;
            }
            var bounds = src.bakedMesh.bounds;
            float k = height / Mathf.Max(1e-4f, bounds.size.y);
            var lift = new Vector3(-bounds.center.x * k, -bounds.min.y * k, -bounds.center.z * k);
            foreach (var p in src.pieces)
            {
                var slot = map(p.slot);
                if (p.shape == null || !slot.HasValue) continue;
                var q = p;
                q.position = at + lift + p.position * k;
                q.scale = (p.scale <= 0f ? 1f : p.scale) * k;
                q.slot = slot.Value;
                q.motion = PropMotion.None;
                q.budgetWeight = 0.5f;
                pieces.Add(q);
            }
        }

        static PropPiece Weighted(SdfShape shape, PropSlot slot, float weight)
        {
            var p = Piece(shape, slot);
            p.budgetWeight = weight;
            return p;
        }

        enum LetterStyle { Pocket, Isle }

        /// <summary>
        /// Lays out <paramref name="word"/> centred on x = 0: letters about <paramref name="em"/> tall (a little uneven),
        /// strokes of radius <paramref name="r"/>, baseline at <paramref name="baseline"/>, at depth <paramref name="z"/>,
        /// each letter tilted a few degrees. Each letter is its body plus its style's bands, caps and drips.
        /// </summary>
        static void Word(List<PropPiece> pieces, string word, float em, float r, float baseline, float z, float gap, LetterStyle style, int seed)
        {
            var rng = new System.Random(seed);
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            var glyphs = new List<(char c, List<List<Vector2>> strokes, float span, float top)>();
            float total = 0f;
            foreach (char c in word)
            {
                var strokes = Glyph(c, out float span);
                float top = 0f;
                foreach (var s in strokes)
                    foreach (var p in s)
                        top = Mathf.Max(top, p.y);
                glyphs.Add((c, strokes, span * em, top));
                total += span * em + 2f * r;
            }
            total += gap * (word.Length - 1);
            float x = -total * 0.5f + r;
            int index = 0;
            foreach (var (c, strokes, span, top) in glyphs)
            {
                // Hand-made: a little taller or shorter, a little up or down, a small tilt about the letter's middle.
                float size = em * R(0.95f, 1.06f);
                float lift = R(-0.03f, 0.04f);
                float tilt = R(-5f, 5f) * Mathf.Deg2Rad;
                var centre = new Vector2(span * 0.5f / em, top * 0.5f);
                Vector3 At(Vector2 p)
                {
                    var d = p - centre;
                    var q = centre + new Vector2(d.x * Mathf.Cos(tilt) - d.y * Mathf.Sin(tilt), d.x * Mathf.Sin(tilt) + d.y * Mathf.Cos(tilt));
                    return new Vector3(-(x + q.x * size), baseline + lift + q.y * size, z);
                }
                var spine = new List<(Vector3 a, Vector3 b)>();
                foreach (var stroke in strokes)
                    for (int i = 1; i < stroke.Count; i++) spine.Add((At(stroke[i - 1]), At(stroke[i])));
                float yTop = float.MinValue, yLow = float.MaxValue;
                foreach (var (a, b) in spine)
                {
                    yTop = Mathf.Max(yTop, Mathf.Max(a.y, b.y));
                    yLow = Mathf.Min(yLow, Mathf.Min(a.y, b.y));
                }
                yTop += r;
                yLow -= r;
                string name = $"Logo_{word}_{c}{index}";
                SdfPrimitive[] Strokes(float radius, params SdfPrimitive[] cuts)
                {
                    var prims = new List<SdfPrimitive>();
                    foreach (var (a, b) in spine) prims.Add(Cone(a, b, radius, radius, prims.Count == 0 ? 0f : 0.02f));
                    prims.AddRange(cuts);
                    return prims.ToArray();
                }
                SdfPrimitive Above(float y) => new SdfPrimitive { kind = SdfKind.Plane, blend = SdfBlend.Intersect, a = new Vector3(0f, y, 0f), b = Vector3.down, k = 0.01f };
                SdfPrimitive Below(float y) => new SdfPrimitive { kind = SdfKind.Plane, blend = SdfBlend.Intersect, a = new Vector3(0f, y, 0f), b = Vector3.up, k = 0.01f };
                float h = yTop - yLow;
                if (style == LetterStyle.Pocket)
                {
                    pieces.Add(Weighted(PShape(name, Strokes(r)), PropSlot.Clay, 1f));
                    // A lighter band along the top (the bevel's highlight), a darker one along the bottom.
                    pieces.Add(Weighted(PShape(name + "_Top", Strokes(r + 0.012f, Above(yTop - 0.17f * h))), PropSlot.Stone, 0.45f));
                    pieces.Add(Weighted(PShape(name + "_Low", Strokes(r + 0.012f, Below(yLow + 0.2f * h))), PropSlot.Wood, 0.45f));
                }
                else
                {
                    pieces.Add(Weighted(PShape(name, Strokes(r)), PropSlot.Leaf, 1f));
                    // A cream highlight band under the moss.
                    pieces.Add(Weighted(PShape(name + "_Band", Strokes(r + 0.012f, Above(yTop - 0.36f * h), Below(yTop - 0.24f * h))), PropSlot.Stone, 0.4f));
                    // Moss caps over the top edges, dripping down the front.
                    var moss = new List<SdfPrimitive>(Strokes(r + 0.018f, Above(yTop - 0.13f * h)));
                    int drips = 0;
                    foreach (var (a, b) in spine)
                    {
                        var p = a.y > b.y ? a : b;
                        if (p.y + r < yTop - 0.02f || drips >= 2) continue;
                        float len = R(0.14f, 0.28f);
                        var foot = p + new Vector3(R(-0.05f, 0.05f), -0.1f * h, r + 0.004f);
                        moss.Add(Cone(foot, foot + Vector3.down * len, 0.045f, 0.028f, 0.03f));
                        drips++;
                    }
                    pieces.Add(Weighted(PShape(name + "_Moss", moss.ToArray()), PropSlot.Accent, 0.6f));
                    if (c == 'I')
                    {
                        // The I's dot: a tiny lantern floating above it (glowing body, a dark cap and a hook).
                        var dot = new Vector3((spine[0].a.x + spine[0].b.x) * 0.5f, yTop + 0.24f, z + 0.04f);
                        pieces.Add(Weighted(PShape(name + "_Lantern", Sphere(dot, 0.1f, 0f)), PropSlot.Glow, 0.4f));
                        pieces.Add(Weighted(PShape(name + "_LanternCap",
                            Cone(dot + Vector3.up * 0.085f, dot + Vector3.up * 0.15f, 0.085f, 0.025f, 0f),
                            Torus(dot + Vector3.up * 0.185f, 0.035f, 0.014f, new Vector3(90f, 0f, 0f), 0.01f)), PropSlot.Wood, 0.3f));
                    }
                }
                x += span + 2f * r + gap;
                index++;
            }
        }

        /// <summary>Stroke centrelines of one letter (x from 0, baseline y = 0, cap height 1, x-height 0.62).</summary>
        static List<List<Vector2>> Glyph(char c, out float span)
        {
            var s = new List<List<Vector2>>();
            List<Vector2> Line(params float[] xy)
            {
                var l = new List<Vector2>();
                for (int i = 0; i + 1 < xy.Length; i += 2) l.Add(new Vector2(xy[i], xy[i + 1]));
                return l;
            }
            switch (c)
            {
                case 'P':
                    s.Add(Line(0f, 0f, 0f, 1f));
                    var bowl = Line(0f, 1f, 0.2f, 1f);
                    bowl.AddRange(Arc(0.2f, 0.75f, 0.27f, 0.25f, 90f, -90f, 10));
                    bowl.Add(new Vector2(0f, 0.5f));
                    s.Add(bowl);
                    span = 0.47f;
                    break;
                case 'o':
                    s.Add(Arc(0.29f, 0.31f, 0.29f, 0.31f, 0f, 360f, 18));
                    span = 0.58f;
                    break;
                case 'c':
                    s.Add(Arc(0.29f, 0.31f, 0.29f, 0.31f, 45f, 315f, 14));
                    span = 0.5f;
                    break;
                case 'k':
                    s.Add(Line(0f, 0f, 0f, 1f));
                    s.Add(Line(0.38f, 0.62f, 0.03f, 0.27f));
                    s.Add(Line(0.15f, 0.38f, 0.4f, 0f));
                    span = 0.4f;
                    break;
                case 'e':
                    var e = Line(0.03f, 0.31f);
                    e.AddRange(Arc(0.29f, 0.31f, 0.29f, 0.31f, 0f, 318f, 16));
                    s.Add(e);
                    span = 0.58f;
                    break;
                case 't':
                    var stem = Line(0.15f, 0.95f);
                    stem.AddRange(Arc(0.33f, 0.17f, 0.18f, 0.17f, 180f, 285f, 6));
                    s.Add(stem);
                    s.Add(Line(0f, 0.62f, 0.36f, 0.62f));
                    span = 0.38f;
                    break;
                case 's':
                    var top = Arc(0.25f, 0.465f, 0.23f, 0.155f, 25f, 270f, 10);
                    top.AddRange(Arc(0.25f, 0.155f, 0.23f, 0.155f, 90f, -155f, 10).GetRange(1, 10));
                    s.Add(top);
                    span = 0.48f;
                    break;
                case 'l':
                    // A small foot curl, so it doesn't read as the capital I beside it.
                    var l = Line(0f, 1f);
                    l.AddRange(Arc(0.17f, 0.17f, 0.17f, 0.17f, 180f, 285f, 6));
                    s.Add(l);
                    span = 0.21f;
                    break;
                default: // I
                    s.Add(Line(0f, 0f, 0f, 1f));
                    span = 0f;
                    break;
            }
            return s;
        }

        /// <summary><paramref name="n"/>+1 points on an ellipse from <paramref name="from"/> to <paramref name="to"/> degrees.</summary>
        static List<Vector2> Arc(float cx, float cy, float rx, float ry, float from, float to, int n)
        {
            var l = new List<Vector2>();
            for (int i = 0; i <= n; i++)
            {
                float a = Mathf.Lerp(from, to, i / (float)n) * Mathf.Deg2Rad;
                l.Add(new Vector2(cx + rx * Mathf.Cos(a), cy + ry * Mathf.Sin(a)));
            }
            return l;
        }
    }
}
