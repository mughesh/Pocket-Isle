using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 14j (Mugesh: the Journal tabs overlap): the tablet layout rule, pure. Tablets in a board sit at least
    /// <see cref="MinGap"/> apart (board-local m = world m at island scale 1), and still don't touch when one grows
    /// while looked at (<see cref="HoverGap"/>). <see cref="Row"/> lays a row out to that rule; <see cref="Problems"/>
    /// checks any set of tablet rects (every board logs it in the Editor and dev builds when its layout changes).
    /// </summary>
    public static class TabletLayout
    {
        public const float MinGap = 0.008f;
        /// <summary>The gap left while one tablet of a pair is grown by its hover (and pulse).</summary>
        public const float HoverGap = 0.002f;

        public struct Rect
        {
            public string label;
            public Vector2 center, size;
            /// <summary>How much it grows looked at (0.1 = 10%).</summary>
            public float grow;
        }

        /// <summary>Gap between two rects along the axis they are apart on (negative = overlap), at rest.</summary>
        public static float Gap(Vector2 c1, Vector2 s1, Vector2 c2, Vector2 s2, float grow1 = 0f, float grow2 = 0f)
        {
            Vector2 h1 = s1 * (0.5f * (1f + grow1)), h2 = s2 * (0.5f * (1f + grow2));
            float gx = Mathf.Abs(c1.x - c2.x) - h1.x - h2.x;
            float gy = Mathf.Abs(c1.y - c2.y) - h1.y - h2.y;
            return Mathf.Max(gx, gy);
        }

        /// <summary>Every pair closer than the rule (rest gap &lt; MinGap, or touching when one is grown).</summary>
        public static List<string> Problems(IList<Rect> rects, float minGap = MinGap)
        {
            var list = new List<string>();
            for (int i = 0; i < rects.Count; i++)
                for (int j = i + 1; j < rects.Count; j++)
                {
                    var a = rects[i];
                    var b = rects[j];
                    float rest = Gap(a.center, a.size, b.center, b.size);
                    float grown = Mathf.Min(Gap(a.center, a.size, b.center, b.size, a.grow, 0f), Gap(a.center, a.size, b.center, b.size, 0f, b.grow));
                    if (rest < minGap - 1e-5f || grown < HoverGap - 1e-5f)
                        list.Add($"'{a.label}' and '{b.label}': {rest * 1000f:0.0} mm apart ({grown * 1000f:0.0} mm when one grows)");
                }
            return list;
        }

        /// <summary>
        /// A row of <paramref name="count"/> tablets centred on x = 0: each at most <paramref name="maxSize"/>, the row at
        /// most <paramref name="maxWidth"/> wide, gaps of at least MinGap plus what a looked-at tablet grows. Returns the
        /// tablet size; <paramref name="centers"/> gets the x of each.
        /// </summary>
        public static Vector2 Row(int count, float maxWidth, Vector2 maxSize, float grow, List<float> centers)
        {
            centers.Clear();
            if (count <= 0) return maxSize;
            // gap >= MinGap and >= HoverGap + the growth of one side (w * grow / 2).
            float w = maxSize.x;
            for (int k = 0; k < 2; k++)
            {
                float gap = Mathf.Max(MinGap, HoverGap + w * grow * 0.5f) + 0.0005f;
                float fit = (maxWidth - gap * (count - 1)) / count;
                w = Mathf.Min(maxSize.x, fit);
            }
            float g = Mathf.Max(MinGap, HoverGap + w * grow * 0.5f) + 0.0005f;
            float step = w + g, start = -step * (count - 1) * 0.5f;
            for (int i = 0; i < count; i++) centers.Add(start + step * i);
            return new Vector2(w, maxSize.y);
        }
    }
}
