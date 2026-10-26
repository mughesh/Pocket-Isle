using System.Collections.Generic;
using UnityEngine;
using static Diorama.EditorTools.StarterKit;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 9g: the title logo (sheet 17, option 1): chunky rounded "Pocket" (orange, smaller) over "Isle" (green, big),
    /// standing on a mini floating island (grass cap, rocky brown underside, grey rocks behind, a tiny tree, a lantern).
    /// No font: each letter is hand-drawn as strokes (lines and elliptic arcs) of round capsules melted together, so the
    /// letters come out puffy like the concept. Baked like a prop (one mesh, the prop shader, outline); drawn by
    /// <see cref="TitleLogo"/>, never placed on a map. Shape units: 1 = the height of a capital "P". Seen from the front
    /// (+Z) the viewer's right is -X, so text runs toward -X.
    /// </summary>
    public static partial class PropKit
    {
        public const string LogoName = "TitleLogo";

        public static PropRecipe WriteLogo()
        {
            DioramaSceneSetup.EnsureFolder(Root);
            DioramaSceneSetup.EnsureFolder($"{Root}/Shapes");
            var logo = Recipe(LogoName, Vector2Int.one, false, false, 7000);
            logo.resolution = 44;
            logo.outline = 0.035f;
            logo.spec = 0.3f;
            logo.scaleJitter = 0f;
            logo.yawJitter = 0f;
            logo.palette = Palette(H("#F4A53C"), H("#7A5236"), H("#9C988F"), H("#62AE48"), H("#FFD27A", 1.1f), H("#3E9440"));

            var pieces = new List<PropPiece>();
            // "Isle" in front on the grass, "Pocket" above and a little behind it.
            Word(pieces, "Isle", 1.25f, 0.17f, 0.12f, 0.16f, 0.04f, PropSlot.Accent);
            Word(pieces, "Pocket", 1f, 0.13f, 1.47f, -0.1f, 0.03f, PropSlot.Clay);

            pieces.Add(Weighted(PShape("Logo_Grass",
                Ell(new Vector3(0f, -0.1f, 0f), new Vector3(2.6f, 0.2f, 0.95f), 0.1f).Bumps(0.025f, 6f),
                Ell(new Vector3(-1.6f, -0.08f, 0.25f), new Vector3(0.9f, 0.18f, 0.6f), 0.2f).Bumps(0.02f, 7f),
                Ell(new Vector3(1.5f, -0.08f, -0.2f), new Vector3(1f, 0.18f, 0.6f), 0.2f).Bumps(0.02f, 7f)), PropSlot.Leaf, 1.6f));
            pieces.Add(Weighted(PShape("Logo_Rock",
                Ell(new Vector3(0f, -0.42f, 0f), new Vector3(2.45f, 0.42f, 0.88f), 0.1f).Bumps(0.06f, 4f),
                Ell(new Vector3(0.15f, -0.9f, 0.05f), new Vector3(1.4f, 0.5f, 0.58f), 0.3f).Bumps(0.06f, 5f),
                Ell(new Vector3(-0.15f, -1.35f, 0f), new Vector3(0.55f, 0.38f, 0.36f), 0.3f).Bumps(0.04f, 6f),
                new SdfPrimitive { kind = SdfKind.Plane, blend = SdfBlend.Intersect, a = new Vector3(0f, -0.14f, 0f), b = Vector3.up, k = 0.04f }),
                PropSlot.Wood, 1.8f));
            pieces.Add(Weighted(PShape("Logo_Stones",
                Ell(new Vector3(-1.25f, 0.85f, -0.55f), new Vector3(0.36f, 0.95f, 0.3f), 0.1f).Bumps(0.04f, 6f),
                Ell(new Vector3(-0.6f, 0.55f, -0.62f), new Vector3(0.3f, 0.65f, 0.28f), 0.15f).Bumps(0.035f, 7f),
                Ell(new Vector3(1.2f, 0.6f, -0.6f), new Vector3(0.32f, 0.6f, 0.28f), 0.1f).Bumps(0.035f, 7f),
                Floor(-0.1f)), PropSlot.Stone, 1.2f));
            // A tiny tree at the back left and a lantern right of "Isle".
            pieces.Add(Weighted(PShape("Logo_Trunk",
                Cone(new Vector3(2.05f, -0.05f, -0.35f), new Vector3(2.02f, 0.5f, -0.35f), 0.07f, 0.05f, 0f)), PropSlot.Wood, 0.15f));
            pieces.Add(Weighted(PShape("Logo_Canopy",
                Sphere(new Vector3(2.02f, 0.78f, -0.35f), 0.3f, 0f).Bumps(0.02f, 9f),
                Sphere(new Vector3(1.86f, 0.62f, -0.24f), 0.2f, 0.1f).Bumps(0.02f, 9f)), PropSlot.Leaf, 0.5f));
            pieces.Add(Weighted(PShape("Logo_LanternFrame",
                Cone(new Vector3(-1.78f, -0.05f, 0.42f), new Vector3(-1.78f, 0.5f, 0.42f), 0.04f, 0.035f, 0f),
                Torus(new Vector3(-1.78f, 0.53f, 0.42f), 0.1f, 0.025f, Vector3.zero, 0.02f),
                Cone(new Vector3(-1.78f, 0.78f, 0.42f), new Vector3(-1.78f, 0.9f, 0.42f), 0.13f, 0.02f, 0.02f)), PropSlot.Wood, 0.3f));
            pieces.Add(Weighted(PShape("Logo_LanternLight",
                Sphere(new Vector3(-1.78f, 0.66f, 0.42f), 0.1f, 0f)), PropSlot.Glow, 0.15f));

            logo.pieces = pieces;
            UnityEditor.EditorUtility.SetDirty(logo);
            return logo;
        }

        static PropPiece Weighted(SdfShape shape, PropSlot slot, float weight)
        {
            var p = Piece(shape, slot);
            p.budgetWeight = weight;
            return p;
        }

        /// <summary>
        /// Lays out <paramref name="word"/> centred on x = 0: letters <paramref name="em"/> tall, strokes of radius
        /// <paramref name="r"/>, baseline at <paramref name="baseline"/>, at depth <paramref name="z"/>. One piece per letter.
        /// </summary>
        static void Word(List<PropPiece> pieces, string word, float em, float r, float baseline, float z, float gap, PropSlot slot)
        {
            var glyphs = new List<(char c, List<List<Vector2>> strokes, float span)>();
            float total = 0f;
            foreach (char c in word)
            {
                var strokes = Glyph(c, out float span);
                glyphs.Add((c, strokes, span * em));
                total += span * em + 2f * r;
            }
            total += gap * (word.Length - 1);
            float x = -total * 0.5f + r;
            foreach (var (c, strokes, span) in glyphs)
            {
                var prims = new List<SdfPrimitive>();
                foreach (var stroke in strokes)
                    for (int i = 1; i < stroke.Count; i++)
                    {
                        var a = new Vector3(-(x + stroke[i - 1].x * em), baseline + stroke[i - 1].y * em, z);
                        var b = new Vector3(-(x + stroke[i].x * em), baseline + stroke[i].y * em, z);
                        // The first capsule of the letter starts the union; the rest melt in with a small blend.
                        prims.Add(Cone(a, b, r, r, prims.Count == 0 ? 0f : 0.02f));
                    }
                pieces.Add(Weighted(PShape($"Logo_{word}_{c}{pieces.Count}", prims.ToArray()), slot, 1f));
                x += span + 2f * r + gap;
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
