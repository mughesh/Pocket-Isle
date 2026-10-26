using System.Collections.Generic;
using UnityEngine;
using static Diorama.EditorTools.StarterKit;

namespace Diorama.EditorTools
{
    /// <summary>
    /// The title logo, v3 (step 9h after Mugesh's headset look: v2's tile chunk read badly, v1's green letters vanished
    /// into the green grass). Like the reference sheet: puffy capsule-stroke letters with one thick dark brown outline
    /// (Logo.mat's <c>_OutlineTint</c>), warm amber "Pocket" stacked on deep green "Isle", and "Isle" standing on the
    /// island's front lip so it reads against the brown earth and the dark rock, not the grass; grey rock spires with
    /// moss tops behind, a glowing lantern beside "Isle". Two props in the same shape units, drawn by
    /// <see cref="TitleLogo"/>: <see cref="LogoName"/> (letters) and <see cref="LogoBaseName"/> (the floating island).
    /// No font: letters are strokes (lines and elliptic arcs). Shape units: 1 = the height of a capital "P". Seen from
    /// the front (+Z) the viewer's right is -X, so text runs toward -X.
    /// </summary>
    public static partial class PropKit
    {
        public const string LogoName = "TitleLogo";
        public const string LogoBaseName = "TitleLogoBase";
        /// <summary>The logo's one outline colour (letters and island), set on Logo.mat.</summary>
        public static readonly Color LogoOutline = new Color(0.2f, 0.12f, 0.07f, 1f);

        /// <summary>The letters.</summary>
        public static PropRecipe WriteLogo()
        {
            DioramaSceneSetup.EnsureFolder(Root);
            DioramaSceneSetup.EnsureFolder($"{Root}/Shapes");
            var logo = Recipe(LogoName, Vector2Int.one, false, false, 6000);
            logo.resolution = 48;
            logo.outline = 0.075f;
            logo.spec = 0.3f;
            logo.scaleJitter = 0f;
            logo.yawJitter = 0f;
            // Clay amber ("Pocket"), Leaf deep green ("Isle"); the rest unused.
            logo.palette = Palette(H("#F7A537"), H("#7A5236"), H("#9C988F"), H("#3F8C3B"), H("#FFD27A", 1.1f), H("#3E9440"));

            var pieces = new List<PropPiece>();
            // "Isle" on the island's front lip (over the earth band), "Pocket" stacked above, touching it.
            Word(pieces, "Isle", 1.12f, 0.17f, -0.1f, 0.82f, 0.06f, PropSlot.Leaf, 11);
            Word(pieces, "Pocket", 0.9f, 0.15f, 1.3f, 0.55f, 0.03f, PropSlot.Clay, 3);
            logo.pieces = pieces;
            UnityEditor.EditorUtility.SetDirty(logo);
            return logo;
        }

        /// <summary>The floating island the letters stand on.</summary>
        public static PropRecipe WriteLogoBase()
        {
            DioramaSceneSetup.EnsureFolder(Root);
            DioramaSceneSetup.EnsureFolder($"{Root}/Shapes");
            var b = Recipe(LogoBaseName, Vector2Int.one, false, false, 3600);
            b.resolution = 40;
            b.outline = 0.05f;
            b.spec = 0.15f;
            b.scaleJitter = 0f;
            b.yawJitter = 0f;
            // Clay lantern frame, Wood earth, Stone rock, Leaf grass, Glow lantern light, Accent moss.
            b.palette = Palette(H("#4A3A2E"), H("#7A5234"), H("#9A958C"), H("#86C24A"), H("#FFD27A", 1.2f), H("#6BAE3E"));

            var pieces = new List<PropPiece>();
            pieces.Add(Weighted(PShape("LogoBase_Grass",
                Ell(new Vector3(0f, -0.1f, 0f), new Vector3(2.55f, 0.2f, 1.05f), 0.1f).Bumps(0.02f, 6f),
                Ell(new Vector3(-1.55f, -0.08f, 0.2f), new Vector3(0.95f, 0.18f, 0.7f), 0.2f).Bumps(0.02f, 7f),
                Ell(new Vector3(1.5f, -0.08f, -0.15f), new Vector3(1f, 0.18f, 0.7f), 0.2f).Bumps(0.02f, 7f)), PropSlot.Leaf, 1.6f));
            pieces.Add(Weighted(PShape("LogoBase_Earth",
                Ell(new Vector3(0f, -0.3f, 0.02f), new Vector3(2.5f, 0.24f, 1.02f), 0.1f).Bumps(0.03f, 5f),
                new SdfPrimitive { kind = SdfKind.Plane, blend = SdfBlend.Intersect, a = new Vector3(0f, -0.17f, 0f), b = Vector3.up, k = 0.03f }),
                PropSlot.Wood, 1.2f));
            pieces.Add(Weighted(PShape("LogoBase_Rock",
                Ell(new Vector3(0f, -0.55f, 0f), new Vector3(2.35f, 0.38f, 0.9f), 0.1f).Bumps(0.06f, 4f),
                Ell(new Vector3(0.15f, -0.95f, 0.05f), new Vector3(1.4f, 0.5f, 0.6f), 0.3f).Bumps(0.06f, 5f),
                Ell(new Vector3(-0.15f, -1.4f, 0f), new Vector3(0.55f, 0.38f, 0.36f), 0.3f).Bumps(0.04f, 6f),
                new SdfPrimitive { kind = SdfKind.Plane, blend = SdfBlend.Intersect, a = new Vector3(0f, -0.4f, 0f), b = Vector3.up, k = 0.04f }),
                PropSlot.Stone, 1.8f));
            // Rock spires behind the words, moss on top (the reference's grey crags).
            pieces.Add(Weighted(PShape("LogoBase_Spires",
                Ell(new Vector3(0.45f, 0.95f, -0.6f), new Vector3(0.45f, 1.15f, 0.36f), 0.1f).Bumps(0.05f, 5f),
                Ell(new Vector3(-0.35f, 0.75f, -0.68f), new Vector3(0.38f, 0.92f, 0.32f), 0.15f).Bumps(0.05f, 6f),
                Ell(new Vector3(1.25f, 0.5f, -0.5f), new Vector3(0.32f, 0.62f, 0.3f), 0.12f).Bumps(0.04f, 7f),
                Ell(new Vector3(-1.1f, 0.38f, -0.55f), new Vector3(0.28f, 0.48f, 0.26f), 0.12f).Bumps(0.04f, 7f),
                Floor(-0.1f)), PropSlot.Stone, 1.4f));
            pieces.Add(Weighted(PShape("LogoBase_Moss",
                Ell(new Vector3(0.45f, 2.02f, -0.58f), new Vector3(0.34f, 0.13f, 0.3f), 0.05f).Bumps(0.02f, 9f),
                Ell(new Vector3(-0.35f, 1.6f, -0.66f), new Vector3(0.29f, 0.11f, 0.26f), 0.05f).Bumps(0.02f, 9f),
                Ell(new Vector3(1.25f, 1.08f, -0.48f), new Vector3(0.24f, 0.09f, 0.22f), 0.05f).Bumps(0.02f, 9f)), PropSlot.Accent, 0.5f));
            pieces.Add(Weighted(PShape("LogoBase_Stones",
                Ell(new Vector3(2.0f, 0.02f, 0.55f), new Vector3(0.22f, 0.14f, 0.18f), 0.05f).Bumps(0.02f, 8f),
                Ell(new Vector3(1.72f, -0.02f, 0.78f), new Vector3(0.14f, 0.09f, 0.12f), 0.05f).Bumps(0.02f, 8f)), PropSlot.Stone, 0.3f));
            // A small lantern on the grass beside "Isle" (its light pulses: TitleLogo).
            var lantern = new Vector3(-2.0f, 0f, 0.5f);
            pieces.Add(Weighted(PShape("LogoBase_LanternFrame",
                Cone(lantern + new Vector3(0f, 0.02f, 0f), lantern + new Vector3(0f, 0.08f, 0f), 0.17f, 0.15f, 0f),
                Cone(lantern + new Vector3(0f, 0.45f, 0f), lantern + new Vector3(0f, 0.55f, 0f), 0.17f, 0.05f, 0.02f),
                Cone(lantern + new Vector3(0.12f, 0.08f, 0f), lantern + new Vector3(0.12f, 0.45f, 0f), 0.025f, 0.025f, 0.01f),
                Cone(lantern + new Vector3(-0.12f, 0.08f, 0f), lantern + new Vector3(-0.12f, 0.45f, 0f), 0.025f, 0.025f, 0.01f),
                Torus(lantern + new Vector3(0f, 0.6f, 0f), 0.05f, 0.018f, new Vector3(90f, 0f, 0f), 0.01f)), PropSlot.Clay, 0.4f));
            pieces.Add(Weighted(PShape("LogoBase_LanternLight",
                Ell(lantern + new Vector3(0f, 0.27f, 0f), new Vector3(0.11f, 0.17f, 0.11f), 0f)), PropSlot.Glow, 0.2f));

            b.pieces = pieces;
            UnityEditor.EditorUtility.SetDirty(b);
            return b;
        }

        static PropPiece Weighted(SdfShape shape, PropSlot slot, float weight)
        {
            var p = Piece(shape, slot);
            p.budgetWeight = weight;
            return p;
        }

        /// <summary>
        /// Lays out <paramref name="word"/> centred on x = 0: letters about <paramref name="em"/> tall (a little uneven,
        /// a small tilt each: hand-made), strokes of radius <paramref name="r"/>, baseline at <paramref name="baseline"/>,
        /// at depth <paramref name="z"/>. One piece per letter.
        /// </summary>
        static void Word(List<PropPiece> pieces, string word, float em, float r, float baseline, float z, float gap, PropSlot slot, int seed)
        {
            var rng = new System.Random(seed);
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            var glyphs = new List<(char c, List<List<Vector2>> strokes, float span, float top)>();
            float total = 0f;
            foreach (char c in word)
            {
                var strokes = Glyph(c, out float span);
                float top = 0f;
                foreach (var st in strokes)
                    foreach (var p in st)
                        top = Mathf.Max(top, p.y);
                glyphs.Add((c, strokes, span * em, top));
                total += span * em + 2f * r;
            }
            total += gap * (word.Length - 1);
            float x = -total * 0.5f + r;
            int index = 0;
            foreach (var (c, strokes, span, top) in glyphs)
            {
                float size = em * R(0.96f, 1.05f);
                float lift = R(-0.03f, 0.04f);
                float tilt = R(-4f, 4f) * Mathf.Deg2Rad;
                var centre = new Vector2(span * 0.5f / em, top * 0.5f);
                Vector3 At(Vector2 p)
                {
                    var d = p - centre;
                    var q = centre + new Vector2(d.x * Mathf.Cos(tilt) - d.y * Mathf.Sin(tilt), d.x * Mathf.Sin(tilt) + d.y * Mathf.Cos(tilt));
                    return new Vector3(-(x + q.x * size), baseline + lift + q.y * size, z);
                }
                var prims = new List<SdfPrimitive>();
                foreach (var stroke in strokes)
                    for (int i = 1; i < stroke.Count; i++)
                        // The first capsule of the letter starts the union; the rest melt in with a small blend.
                        prims.Add(Cone(At(stroke[i - 1]), At(stroke[i]), r, r, prims.Count == 0 ? 0f : 0.02f));
                pieces.Add(Weighted(PShape($"Logo_{word}_{c}{index}", prims.ToArray()), slot, 1f));
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
                default: // I, with short bars top and bottom so it never reads as the "l" beside it
                    s.Add(Line(0.13f, 0f, 0.13f, 1f));
                    s.Add(Line(0f, 1f, 0.26f, 1f));
                    s.Add(Line(0f, 0f, 0.26f, 0f));
                    span = 0.26f;
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
