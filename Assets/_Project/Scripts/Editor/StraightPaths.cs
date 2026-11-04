using System.Collections.Generic;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 14i F (Mugesh: paths zig-zag one tile at a time): turns the realm generators' authored path waypoints into
    /// straight legs with right-angle turns. A diagonal step between two waypoints becomes an L that carries on along the
    /// previous leg's axis first (no extra turn); a sideways offset shorter than <see cref="MinLeg"/> tiles is dropped (the
    /// waypoint moves onto the straight line; the next one absorbs it), so there are no one-tile jogs. Pinned waypoints are
    /// kept exactly (a gate, the last tile); kept segments stay as authored (a hand moment drawn on purpose, e.g. the
    /// smoky hollow's steps). Pure.
    /// </summary>
    public static class StraightPaths
    {
        public const int MinLeg = 3;

        /// <summary>
        /// 2-wide trails (waypoints on half tiles, drawn by distance to the polyline). <paramref name="pinned"/>: waypoint
        /// indices hit exactly (the first and last always are); <paramref name="keep"/>: segment indices (i to i + 1) kept as
        /// authored, both ends pinned.
        /// </summary>
        public static Vector2[] Trail(Vector2[] pts, ICollection<int> pinned = null, ICollection<int> keep = null, int minLeg = MinLeg)
        {
            var output = new List<Vector2>();
            if (pts == null || pts.Length == 0) return output.ToArray();
            output.Add(pts[0]);
            Vector2 cur = pts[0];
            bool? vertical = null;
            for (int i = 1; i < pts.Length; i++)
            {
                bool kept = keep != null && keep.Contains(i - 1);
                bool exact = i == pts.Length - 1 || pinned != null && pinned.Contains(i) || kept || keep != null && keep.Contains(i);
                if (kept)
                {
                    // Reach the kept segment's start exactly, then draw it as authored.
                    if (cur != pts[i - 1]) cur = Leg(output, cur, pts[i - 1], ref vertical);
                    output.Add(pts[i]);
                    cur = pts[i];
                    vertical = null;
                    continue;
                }
                Vector2 b = pts[i];
                float dx = b.x - cur.x, dy = b.y - cur.y;
                if (!exact)
                {
                    if (Mathf.Abs(dx) < minLeg) dx = 0f;
                    if (Mathf.Abs(dy) < minLeg) dy = 0f;
                }
                cur = Leg(output, cur, cur + new Vector2(dx, dy), ref vertical);
            }
            return output.ToArray();
        }

        /// <summary>Adds the L (or straight leg) from <paramref name="a"/> to <paramref name="b"/>; returns b.</summary>
        static Vector2 Leg(List<Vector2> output, Vector2 a, Vector2 b, ref bool? vertical)
        {
            Vector2 d = b - a;
            if (d.sqrMagnitude < 1e-6f) return a;
            // The last leg's direction (never double back over it: that leaves a dead-end stub).
            Vector2 last = output.Count >= 2 ? output[output.Count - 1] - output[output.Count - 2] : Vector2.zero;
            if (Mathf.Abs(d.x) > 1e-4f && Mathf.Abs(d.y) > 1e-4f)
            {
                // Carry on along the last leg's axis first (no extra turn); with no last leg, the longer way first.
                bool upFirst = vertical ?? Mathf.Abs(d.y) >= Mathf.Abs(d.x);
                Vector2 first = upFirst ? new Vector2(0f, d.y) : new Vector2(d.x, 0f);
                if (Vector2.Dot(first, last) < 0f) upFirst = !upFirst;
                output.Add(upFirst ? new Vector2(a.x, b.y) : new Vector2(b.x, a.y));
                vertical = !upFirst;
            }
            else vertical = Mathf.Abs(d.x) < 1e-4f;
            output.Add(b);
            return b;
        }

        /// <summary>1-wide side paths (tile waypoints): the tiles of straight legs through the (snapped) waypoints.</summary>
        public static List<Vector2Int> Side(Vector2Int[] pts, int minLeg = MinLeg)
        {
            var poly = new Vector2[pts.Length];
            for (int i = 0; i < pts.Length; i++) poly[i] = pts[i];
            var legs = Trail(poly, null, null, minLeg);
            var tiles = new List<Vector2Int>();
            for (int i = 0; i + 1 < legs.Length; i++)
            {
                var a = Vector2Int.RoundToInt(legs[i]);
                var b = Vector2Int.RoundToInt(legs[i + 1]);
                var step = new Vector2Int(System.Math.Sign(b.x - a.x), System.Math.Sign(b.y - a.y));
                for (var t = a; ; t += step)
                {
                    if (tiles.Count == 0 || tiles[tiles.Count - 1] != t) tiles.Add(t);
                    if (t == b || step == Vector2Int.zero) break;
                }
            }
            if (tiles.Count == 0 && pts.Length > 0) tiles.Add(pts[0]);
            return tiles;
        }

        /// <summary>Shortest leg between turns of a polyline (tiles); the first and last legs count too.</summary>
        public static float ShortestLeg(Vector2[] legs, ICollection<int> skipSegments = null)
        {
            float best = float.MaxValue;
            for (int i = 0; i + 1 < legs.Length; i++)
                if (skipSegments == null || !skipSegments.Contains(i)) best = Mathf.Min(best, (legs[i + 1] - legs[i]).magnitude);
            return best;
        }
    }
}
