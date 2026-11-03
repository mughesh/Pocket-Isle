using System.Collections.Generic;
using UnityEngine;
using static Diorama.EditorTools.StarterKit;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 14c: the realm name cards (<see cref="RealmNameCard"/>) and the hub's gate signs (<see cref="GateSigns"/>), one
    /// baked prop per realm: the realm's name in the logo's chunky strokes (<see cref="Word"/>) in its colour, its element
    /// emblem on the left, on a board in its style: a wooden plank (Galewick), a charred plank with ember edges
    /// (Cinderwood), a mossy stone tablet (Lilymere), a warm plank (Still Isle). The board runs below the name: the card's
    /// one line of text (TextMeshPro) sits there. Shape units: 1 = a capital's height; the text runs toward -X (the
    /// viewer's right seen from +Z).
    /// </summary>
    public static partial class PropKit
    {
        public struct RealmCardStyle
        {
            public string realm, name, asset, emblem;
            public string letters, board, trim, moss, emblemColor;
            public float trimGlow;
        }

        /// <summary>The four cards (realm ids as on the maps).</summary>
        public static readonly RealmCardStyle[] RealmCards =
        {
            new RealmCardStyle { realm = "galewick", name = "Galewick", asset = "RealmCard_Galewick", emblem = "wind",
                letters = "#3FAE4C", board = "#B48352", trim = "#8A5E36", moss = "#7DBA4A", emblemColor = "#6FD27A" },
            new RealmCardStyle { realm = "cinderwood", name = "Cinderwood", asset = "RealmCard_Cinderwood", emblem = "fire",
                letters = "#FF6A26", board = "#3B2C26", trim = "#FF8A2E", moss = "#5A4032", emblemColor = "#FF9A2E", trimGlow = 1.3f },
            new RealmCardStyle { realm = "lilymere", name = "Lilymere", asset = "RealmCard_Lilymere", emblem = "water",
                letters = "#2E8FE6", board = "#A3ACA6", trim = "#7F8A86", moss = "#5E9C46", emblemColor = "#52B6F2" },
            new RealmCardStyle { realm = "stillisle", name = "Still Isle", asset = "RealmCard_StillIsle", emblem = "home",
                letters = "#D9741A", board = "#C69862", trim = "#97693F", moss = "#7DBA4A", emblemColor = "#FFD27A" },
        };

        /// <summary>Where the card's line of text goes (shape units, the board's lower band): centre y.</summary>
        public const float CardLineY = -0.62f;

        /// <summary>Writes one realm's card recipe (bake it with PropBaker).</summary>
        public static PropRecipe WriteRealmCard(RealmCardStyle style)
        {
            DioramaSceneSetup.EnsureFolder(Root);
            DioramaSceneSetup.EnsureFolder($"{Root}/Shapes");
            var card = Recipe(style.asset, Vector2Int.one, false, false, 5200);
            card.resolution = 84;
            card.outline = 0.05f;
            card.spec = 0.3f;
            card.scaleJitter = 0f;
            card.yawJitter = 0f;
            // Clay letters, Wood board, Stone trim / nails, Leaf moss, Glow embers, Accent emblem.
            card.palette = Palette(H(style.letters), H(style.board), H(style.trim), H(style.moss), H(style.trim, style.trimGlow > 0f ? style.trimGlow : 0.6f),
                H(style.emblemColor, style.emblem == "fire" || style.emblem == "home" ? 0.9f : 0.15f));

            const float em = 1f, r = 0.12f, gap = 0.07f;
            float width = WordWidth(style.name, em, r, gap);
            // Nov 3 (Mugesh): the wind curl crowded the G; a little more room between emblem and name.
            float emblemX = width * 0.5f + 0.78f;
            // The whole card centred on x = 0: the name shifts right (toward -X) by half the emblem's room.
            float shift = -0.39f;
            var pieces = new List<PropPiece>();
            Word(pieces, style.name, em, r, 0f, 0.12f, gap, PropSlot.Clay, style.name.Length * 7 + 3);
            for (int i = 0; i < pieces.Count; i++)
            {
                var p = pieces[i];
                p.position += new Vector3(shift, 0f, 0f);
                pieces[i] = p;
            }
            // Step 14g: a little more room at both ends (the emblem sat tight against the edge and read as a cut-off letter).
            float halfW = width * 0.5f + 1.45f, top = 1.3f, bottom = -1.0f;
            var centre = new Vector3(shift + 0.39f, (top + bottom) * 0.5f, -0.1f);
            var half = new Vector3(halfW, (top - bottom) * 0.5f, 0.1f);
            Board(pieces, style, centre, half);
            Emblem(pieces, style, new Vector3(shift + emblemX, 0.5f, 0.1f));
            card.pieces = pieces;
            UnityEditor.EditorUtility.SetDirty(card);
            return card;
        }

        /// <summary>The width <see cref="Word"/> lays a word out over.</summary>
        static float WordWidth(string word, float em, float r, float gap)
        {
            float total = 0f;
            foreach (char c in word)
            {
                Glyph(c, out float span);
                total += span * em + 2f * r;
            }
            return total + gap * (word.Length - 1);
        }

        static void Board(List<PropPiece> pieces, RealmCardStyle style, Vector3 c, Vector3 half)
        {
            string n = style.asset;
            switch (style.emblem)
            {
                case "fire":
                {
                    // Nov 3 late (Mugesh: "kind of off"): the glowing slab behind the planks read as a flat yellow picture
                    // frame. Now two charred planks like the other wooden cards (the lower one a little crooked), burnt bites
                    // out of two corners, a few embers smouldering at the ends and small flame tongues over the top edge.
                    float ph = half.y * 0.5f - 0.015f;
                    var planks = new List<SdfPrimitive>
                    {
                        Box(c + new Vector3(0f, half.y * 0.5f, 0f), new Vector3(half.x, ph, half.z), 0.07f, 0f),
                        Box(c + new Vector3(0.05f, -half.y * 0.5f, 0f), new Vector3(half.x - 0.05f, ph, half.z), 0.07f, 0.02f, new Vector3(0f, 0f, 0.8f)),
                    };
                    // Burnt bites: the top-right and bottom-left corners eaten away.
                    planks.Add(new SdfPrimitive { kind = SdfKind.Sphere, blend = SdfBlend.Subtract, a = c + new Vector3(-half.x, half.y, 0f), size = new Vector3(0.38f, 0f, 0f), k = 0.08f });
                    planks.Add(new SdfPrimitive { kind = SdfKind.Sphere, blend = SdfBlend.Subtract, a = c + new Vector3(half.x, -half.y, 0f), size = new Vector3(0.32f, 0f, 0f), k = 0.08f });
                    pieces.Add(Weighted(PShape($"{n}_Board", planks.ToArray()), PropSlot.Wood, 1.4f));
                    // Smouldering spots at the board's ends (clear of the name and the line) and flame tongues over the top edge.
                    var spots = new List<SdfPrimitive>();
                    foreach (var (sx, sy, r) in new[] { (-0.93f, -0.55f, 0.15f), (0.93f, -0.6f, 0.14f), (-0.94f, 0.45f, 0.13f) })
                        spots.Add(Ell(c + new Vector3(sx * half.x, sy * half.y, half.z - 0.02f), new Vector3(r * 1.6f, r, 0.07f), 0f));
                    foreach (var (sx, hgt) in new[] { (-0.7f, 0.42f), (-0.25f, 0.3f), (0.3f, 0.38f), (0.75f, 0.26f) })
                        spots.Add(Cone(c + new Vector3(sx * half.x, half.y - 0.05f, 0f), c + new Vector3(sx * half.x + 0.04f, half.y + hgt, 0f), 0.13f, 0.02f, 0f));
                    pieces.Add(Weighted(PShape($"{n}_Fire", spots.ToArray()), PropSlot.Glow, 0.7f));
                    break;
                }
                case "water":
                    // Mossy stone tablet: rounder corners, moss on the top edge and one corner.
                    pieces.Add(Weighted(PShape($"{n}_Board", Box(c, half + new Vector3(0f, 0f, 0.03f), 0.22f, 0f)), PropSlot.Wood, 1.4f));
                    pieces.Add(Weighted(PShape($"{n}_Moss",
                        Ell(new Vector3(c.x + half.x * 0.55f, c.y + half.y - 0.02f, c.z + 0.02f), new Vector3(0.75f, 0.12f, 0.16f), 0.1f),
                        Ell(new Vector3(c.x - half.x * 0.2f, c.y + half.y - 0.01f, c.z + 0.02f), new Vector3(0.55f, 0.1f, 0.15f), 0.1f),
                        Ell(new Vector3(c.x - half.x + 0.12f, c.y - half.y + 0.25f, c.z + 0.04f), new Vector3(0.16f, 0.32f, 0.14f), 0.1f)),
                        PropSlot.Leaf, 0.6f));
                    break;
                default:
                    // Wooden plank: two boards, nail heads at the ends.
                    float h = half.y * 0.5f - 0.015f;
                    pieces.Add(Weighted(PShape($"{n}_Board",
                        Box(c + new Vector3(0f, half.y * 0.5f, 0f), new Vector3(half.x, h, half.z), 0.07f, 0f),
                        Box(c + new Vector3(0.04f, -half.y * 0.5f, 0f), new Vector3(half.x - 0.04f, h, half.z), 0.07f, 0.02f)),
                        PropSlot.Wood, 1.4f));
                    var nails = new List<SdfPrimitive>();
                    foreach (float sx in new[] { -1f, 1f })
                        foreach (float sy in new[] { -1f, 1f })
                            nails.Add(Sphere(c + new Vector3(sx * (half.x - 0.22f), sy * half.y * 0.5f, half.z), 0.07f, 0f));
                    pieces.Add(Weighted(PShape($"{n}_Nails", nails.ToArray()), PropSlot.Stone, 0.3f));
                    if (style.emblem == "home")
                        pieces.Add(Weighted(PShape($"{n}_Sprig",
                            Ell(new Vector3(c.x - half.x + 0.1f, c.y + half.y - 0.05f, c.z + 0.12f), new Vector3(0.28f, 0.11f, 0.08f), 0.05f, new Vector3(0f, 0f, 30f)),
                            Ell(new Vector3(c.x - half.x + 0.32f, c.y + half.y + 0.06f, c.z + 0.12f), new Vector3(0.24f, 0.1f, 0.08f), 0.05f, new Vector3(0f, 0f, -20f))),
                            PropSlot.Leaf, 0.4f));
                    break;
            }
        }

        static void Emblem(List<PropPiece> pieces, RealmCardStyle style, Vector3 at)
        {
            string n = style.asset;
            switch (style.emblem)
            {
                case "wind":
                {
                    // A curl and two gust lines, in the letters' strokes.
                    var prims = new List<SdfPrimitive>();
                    void Stroke(List<Vector2> pts, float r)
                    {
                        for (int i = 1; i < pts.Count; i++)
                        {
                            Vector3 a = at + new Vector3(-pts[i - 1].x, pts[i - 1].y, 0f), b = at + new Vector3(-pts[i].x, pts[i].y, 0f);
                            var d = b - a;
                            prims.Add(Box((a + b) * 0.5f, new Vector3(d.magnitude * 0.5f + r * 0.98f, r, r * 0.95f), r * 0.35f, prims.Count == 0 ? 0f : 0.03f,
                                new Vector3(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg)));
                        }
                    }
                    // A gust: a line that curls up into a spiral at its end, and a shorter one under it.
                    var curl = new List<Vector2> { new Vector2(-0.5f, -0.05f) };
                    for (int k = 0; k <= 14; k++)
                    {
                        float u = k / 14f;
                        float a = (-90f + 330f * u) * Mathf.Deg2Rad;
                        float rad = Mathf.Lerp(0.3f, 0.1f, u);
                        curl.Add(new Vector2(0.08f + rad * Mathf.Cos(a), 0.25f + rad * Mathf.Sin(a)));
                    }
                    Stroke(curl, 0.075f);
                    Stroke(new List<Vector2> { new Vector2(-0.42f, -0.36f), new Vector2(0.18f, -0.36f) }, 0.065f);
                    pieces.Add(Weighted(PShape($"{n}_Emblem", prims.ToArray()), PropSlot.Accent, 0.8f));
                    break;
                }
                case "fire":
                    pieces.Add(Weighted(PShape($"{n}_Emblem",
                        Cone(at + new Vector3(0f, -0.3f, 0f), at + new Vector3(0f, 0.55f, 0f), 0.34f, 0.04f, 0f),
                        Cone(at + new Vector3(-0.22f, -0.32f, 0.02f), at + new Vector3(-0.32f, 0.2f, 0.02f), 0.16f, 0.03f, 0.06f),
                        Cone(at + new Vector3(0.22f, -0.32f, 0.02f), at + new Vector3(0.3f, 0.12f, 0.02f), 0.15f, 0.03f, 0.06f)), PropSlot.Accent, 0.8f));
                    pieces.Add(Weighted(PShape($"{n}_EmblemCore",
                        Cone(at + new Vector3(0f, -0.3f, 0.2f), at + new Vector3(0f, 0.12f, 0.2f), 0.17f, 0.03f, 0f)), PropSlot.Glow, 0.4f));
                    break;
                case "water":
                    pieces.Add(Weighted(PShape($"{n}_Emblem",
                        Sphere(at + new Vector3(0f, -0.12f, 0f), 0.32f, 0f),
                        Cone(at + new Vector3(0f, -0.1f, 0f), at + new Vector3(0f, 0.6f, 0f), 0.28f, 0.03f, 0.12f)), PropSlot.Accent, 0.8f));
                    break;
                default: // home: the hub's lantern light, a little round glow on a stub
                    pieces.Add(Weighted(PShape($"{n}_Emblem", Sphere(at + new Vector3(0f, 0.1f, 0f), 0.3f, 0f)), PropSlot.Accent, 0.6f));
                    pieces.Add(Weighted(PShape($"{n}_EmblemCap",
                        Cone(at + new Vector3(0f, 0.36f, 0f), at + new Vector3(0f, 0.62f, 0f), 0.24f, 0.05f, 0f),
                        Box(at + new Vector3(0f, -0.28f, 0f), new Vector3(0.2f, 0.07f, 0.12f), 0.03f, 0f)), PropSlot.Stone, 0.4f));
                    break;
            }
        }
    }
}
