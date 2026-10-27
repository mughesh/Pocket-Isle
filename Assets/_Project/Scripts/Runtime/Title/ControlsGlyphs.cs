using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 12a: the Controls card's little drawn glyphs, painted once in code (capsules and circles, cream on clear with
    /// a dark edge, gold for the motion): 0 = thumb swipe (a few steps), 1 = two swipes and a tap (keep going, stop),
    /// 2 = look + pinch (go there), 3 = pinch / grab (things on the island).
    /// </summary>
    public static class ControlsGlyphs
    {
        public const int Count = 4;
        const int Size = 96;
        static readonly Texture2D[] Cache = new Texture2D[Count];

        static readonly Color Skin = new Color(0.97f, 0.93f, 0.84f);
        static readonly Color Edge = new Color(0.16f, 0.12f, 0.08f);
        static readonly Color Gold = new Color(1f, 0.76f, 0.2f);

        public static Texture2D Get(int kind)
        {
            kind = Mathf.Clamp(kind, 0, Count - 1);
            if (Cache[kind] != null) return Cache[kind];
            var t = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { name = $"Controls Glyph {kind}", wrapMode = TextureWrapMode.Clamp };
            var px = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                    px[y * Size + x] = Shade(kind, new Vector2((x + 0.5f) / Size, (y + 0.5f) / Size));
            t.SetPixels(px);
            t.Apply(false, true);
            Cache[kind] = t;
            return t;
        }

        static float Capsule(Vector2 p, Vector2 a, Vector2 b, float r)
        {
            var pa = p - a;
            var ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Mathf.Max(1e-6f, Vector2.Dot(ba, ba)));
            return (pa - ba * h).magnitude - r;
        }

        static float Circle(Vector2 p, Vector2 c, float r) => (p - c).magnitude - r;

        /// <summary>A side-on hand: index finger along the top, thumb resting on it (the microgesture pose).</summary>
        static float SideHand(Vector2 p, Vector2 o)
        {
            float palm = Capsule(p, o + new Vector2(-0.2f, -0.12f), o + new Vector2(0.02f, -0.12f), 0.13f);
            float index = Capsule(p, o + new Vector2(0.02f, -0.02f), o + new Vector2(0.3f, -0.02f), 0.045f);
            float middle = Capsule(p, o + new Vector2(0.02f, -0.12f), o + new Vector2(0.26f, -0.14f), 0.042f);
            float thumb = Capsule(p, o + new Vector2(-0.08f, 0.04f), o + new Vector2(0.12f, 0.06f), 0.042f);
            return Mathf.Min(Mathf.Min(palm, index), Mathf.Min(middle, thumb));
        }

        static float Arrow(Vector2 p, Vector2 a, Vector2 b)
        {
            var dir = (b - a).normalized;
            var side = new Vector2(-dir.y, dir.x);
            float stem = Capsule(p, a, b, 0.02f);
            float h1 = Capsule(p, b, b - dir * 0.09f + side * 0.07f, 0.02f);
            float h2 = Capsule(p, b, b - dir * 0.09f - side * 0.07f, 0.02f);
            return Mathf.Min(stem, Mathf.Min(h1, h2));
        }

        static Color Shade(int kind, Vector2 p)
        {
            float hand, gold = 1f;
            switch (kind)
            {
                case 0:
                    hand = SideHand(p, new Vector2(0.45f, 0.42f));
                    gold = Arrow(p, new Vector2(0.32f, 0.72f), new Vector2(0.7f, 0.72f));
                    break;
                case 1:
                    hand = SideHand(p, new Vector2(0.45f, 0.36f));
                    gold = Mathf.Min(Arrow(p, new Vector2(0.14f, 0.7f), new Vector2(0.44f, 0.7f)),
                        Arrow(p, new Vector2(0.46f, 0.7f), new Vector2(0.76f, 0.7f)));
                    // The tap: a dot with a ring, after the swipes.
                    gold = Mathf.Min(gold, Mathf.Min(Circle(p, new Vector2(0.88f, 0.7f), 0.035f), Mathf.Abs(Circle(p, new Vector2(0.88f, 0.7f), 0.07f)) - 0.012f));
                    break;
                case 2:
                {
                    // An eye at the top, a pinching hand below it, a dotted look line to a gold spot.
                    var e = p - new Vector2(0.3f, 0.75f);
                    float eye = Mathf.Max(Mathf.Abs(e.y) - 0.09f * (1f - Mathf.Clamp01(Mathf.Abs(e.x) / 0.17f)), Mathf.Abs(e.x) - 0.17f);
                    hand = Mathf.Min(eye, Mathf.Min(Capsule(p, new Vector2(0.18f, 0.22f), new Vector2(0.36f, 0.32f), 0.1f),
                        Mathf.Min(Capsule(p, new Vector2(0.36f, 0.36f), new Vector2(0.5f, 0.44f), 0.04f), Capsule(p, new Vector2(0.36f, 0.26f), new Vector2(0.5f, 0.42f), 0.04f))));
                    float pupil = Circle(p, new Vector2(0.32f, 0.75f), 0.04f);
                    float dots = 1f;
                    for (int i = 0; i < 4; i++) dots = Mathf.Min(dots, Circle(p, Vector2.Lerp(new Vector2(0.48f, 0.72f), new Vector2(0.8f, 0.42f), i / 3.5f), 0.018f));
                    gold = Mathf.Min(Mathf.Min(dots, Circle(p, new Vector2(0.84f, 0.36f), 0.07f)), pupil);
                    break;
                }
                default:
                    // A pinch closing on a small orb (a pod, a kite) sitting on the island's edge.
                    hand = Mathf.Min(Capsule(p, new Vector2(0.16f, 0.62f), new Vector2(0.38f, 0.62f), 0.13f),
                        Mathf.Min(Capsule(p, new Vector2(0.4f, 0.7f), new Vector2(0.6f, 0.56f), 0.045f), Capsule(p, new Vector2(0.4f, 0.5f), new Vector2(0.6f, 0.5f), 0.045f)));
                    gold = Mathf.Min(Circle(p, new Vector2(0.68f, 0.52f), 0.07f), Capsule(p, new Vector2(0.18f, 0.2f), new Vector2(0.86f, 0.2f), 0.025f));
                    break;
            }
            float aa = 1.5f / Size;
            float handIn = Mathf.Clamp01(0.5f - hand / aa);
            float handEdge = Mathf.Clamp01(0.5f - (hand - 0.018f) / aa);
            float goldIn = Mathf.Clamp01(0.5f - gold / aa);
            float goldEdge = Mathf.Clamp01(0.5f - (gold - 0.014f) / aa);
            var c = new Color(Edge.r, Edge.g, Edge.b, Mathf.Max(handEdge, goldEdge));
            c = Color.Lerp(c, Skin, handIn);
            c = Color.Lerp(c, Gold, goldIn);
            c.a = Mathf.Max(c.a, Mathf.Max(handIn, goldIn));
            return c;
        }
    }
}
