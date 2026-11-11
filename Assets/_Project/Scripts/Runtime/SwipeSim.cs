using System;
using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Nov 12 (Mugesh: "the simulation is supposed to measure the critical thing: how many swipes the user does, like
    /// clicks"): counts the swipes a player needs from one place to the next. The player swipes every time the explorer
    /// stops and taps once when the walk passes the destination; the count is the fewest swipes any player could do (a
    /// search over every stop tile, four swipes each). Every stop on the way is explained: (a) a real turn of the road,
    /// (b) a real side road, (c) one tile before a door or a stop, or it is listed as unexplained. Pure; the Editor's
    /// Nov12Walk.Routes writes the tables, Nov12WalkTests keeps them from getting worse.
    /// </summary>
    public static class SwipeSim
    {
        public const int MaxSwipes = 30;

        public const string KindTurn = "turn";
        public const string KindBranch = "branch";
        public const string KindStop = "stop";
        /// <summary>The player tapped to stop (where the walk came closest to a place in the meadow).</summary>
        public const string Tap = "tap";
        /// <summary>The walk came onto a road from the grass and stopped on it (R52): joining a road is a choice of way.</summary>
        public const string KindJoin = "joins road";
        /// <summary>The road really ends: nothing ahead within 2 tiles to either side (not a jog).</summary>
        public const string KindEnd = "road's end";
        /// <summary>A stop off the roads (a tree or a fence in the meadow): the stopping rule is about roads.</summary>
        public const string KindOffRoad = "off road";

        static readonly Vector2Int[] Ways = { new Vector2Int(0, -1), new Vector2Int(0, 1), new Vector2Int(-1, 0), new Vector2Int(1, 0) };

        public struct StopInfo
        {
            public Vector2Int tile, heading;
            public string end, kind, why;
            public bool Explained => kind != null;
        }

        public sealed class Leg
        {
            public string from, to;
            public bool reached;
            public int swipes, taps, tiles;
            public readonly List<StopInfo> stops = new List<StopInfo>();

            public int Explained
            {
                get
                {
                    int n = 0;
                    foreach (var s in stops) if (s.Explained) n++;
                    return n;
                }
            }

            public int Unexplained => stops.Count - Explained;

            /// <summary>Swipes this leg should need at most: one per explained stop, plus the first.</summary>
            public int Target => Explained + 1;

            public bool Ok => reached && Unexplained == 0;
        }

        static bool Road(IslandMap map, Vector2Int t) => map.At(t.y, t.x) == IslandMap.Path && map.CanWalk(t);

        static Vector2Int Left(Vector2Int h) => new Vector2Int(-h.y, h.x);

        static int Run(IslandMap map, Vector2Int at, Vector2Int way, int max = 12)
        {
            int n = 0;
            for (var t = at + way; n < max && Road(map, t); t += way) n++;
            return n;
        }

        /// <summary>
        /// (a) A real turn: the road can't go on straight from <paramref name="p"/> but runs on at least 3 tiles to a side.
        /// A jog (the road shifts sideways by a tile or two) is not one.
        /// </summary>
        public static bool RealTurn(IslandMap map, Vector2Int p, Vector2Int h)
        {
            if (!Road(map, p) || Road(map, p + h)) return false;
            var l = Left(h);
            return Run(map, p, l) >= 3 || Run(map, p, -l) >= 3;
        }

        /// <summary>
        /// (b) A real side road at <paramref name="p"/>: on one side the road reaches at least 3 tiles further than the
        /// corridor's own width does, over 2 to 4 rows that include <paramref name="p"/> (a lane, not a plaza or a stretch
        /// where the road is merely wider), and the lane runs on (it isn't a stub).
        /// </summary>
        public static bool RealBranch(IslandMap map, Vector2Int p, Vector2Int h)
        {
            if (!Road(map, p)) return false;
            var l = Left(h);
            foreach (var s in new[] { l, -l })
            {
                int edge = CorridorEdge(map, p, h, s);
                int need = edge + 3;
                if (Run(map, p, s, 16) < need) continue;
                int rows = 1;
                for (var t = p + h; Road(map, t) && Run(map, t, s, 16) >= need && rows < 8; t += h) rows++;
                for (var t = p - h; Road(map, t) && Run(map, t, s, 16) >= need && rows < 8; t -= h) rows++;
                if (rows >= 2 && rows <= 4) return true;
            }
            return false;
        }

        /// <summary>The corridor's width to side <paramref name="s"/>: the fewest road tiles that way within 3 rows of p.</summary>
        public static int CorridorEdge(IslandMap map, Vector2Int p, Vector2Int h, Vector2Int s)
        {
            int edge = int.MaxValue;
            for (int j = -3; j <= 3; j++)
            {
                var t = p + h * j;
                if (!Road(map, t)) continue;
                edge = Mathf.Min(edge, Run(map, t, s, 16));
            }
            return edge == int.MaxValue ? 0 : edge;
        }

        /// <summary>Walkable tiles round a place: within 1 tile (Chebyshev), else within 2; a place in the meadow (off the roads) within 2.</summary>
        public static HashSet<Vector2Int> Zone(IslandMap map, Vector2Int place)
        {
            var zone = new HashSet<Vector2Int>();
            bool meadow = map.CanWalk(place) && !Road(map, place);
            for (int reach = meadow ? 2 : 1; reach <= 2 && zone.Count == 0; reach++)
                for (int dy = -reach; dy <= reach; dy++)
                    for (int dx = -reach; dx <= reach; dx++)
                    {
                        var t = place + new Vector2Int(dx, dy);
                        if (t.x >= 0 && t.y >= 0 && t.x < map.Columns && t.y < map.Rows && map.CanWalk(t)) zone.Add(t);
                    }
            return zone;
        }

        /// <summary>One swipe from <paramref name="from"/>: the tiles walked (plans that run out are planned on, like the game).</summary>
        public static string Swipe(IslandMap map, Vector2Int from, Vector2Int way, Func<Vector2Int, Vector2Int, WalkStop> stop, List<Vector2Int> walked)
        {
            walked.Clear();
            var tiles = new List<Vector2Int>();
            var cur = from;
            var h = way;
            string end = SmartStep.Blocked;
            for (int round = 0; round < WalkSim.MaxRounds; round++)
            {
                end = SmartStep.Plan(map, cur, h, true, WalkSim.MaxTiles, stop, tiles, false, true);
                walked.AddRange(tiles);
                if (end != SmartStep.Cap || tiles.Count == 0) break;
                var last = tiles[tiles.Count - 1];
                h = last - (tiles.Count > 1 ? tiles[tiles.Count - 2] : cur);
                cur = last;
            }
            return end;
        }

        /// <summary>A road tile costs 1 to cross, anything else walkable 3: the player follows the roads.</summary>
        public static Dictionary<Vector2Int, int> Distances(IslandMap map, HashSet<Vector2Int> zone)
        {
            var dist = new Dictionary<Vector2Int, int>();
            var open = new SortedSet<(int d, int x, int y)>();
            foreach (var z in zone)
            {
                dist[z] = 0;
                open.Add((0, z.x, z.y));
            }
            while (open.Count > 0)
            {
                var top = open.Min;
                open.Remove(top);
                var t = new Vector2Int(top.x, top.y);
                if (dist[t] < top.d) continue;
                foreach (var w in Ways)
                {
                    var n = t + w;
                    if (n.x < 0 || n.y < 0 || n.x >= map.Columns || n.y >= map.Rows || !map.CanWalk(n)) continue;
                    int nd = top.d + (Road(map, n) && Road(map, t) ? 1 : 3);
                    if (dist.TryGetValue(n, out int old) && old <= nd) continue;
                    if (dist.ContainsKey(n)) open.Remove((old, n.x, n.y));
                    dist[n] = nd;
                    open.Add((nd, n.x, n.y));
                }
            }
            return dist;
        }

        /// <summary>
        /// The player of the spec: every time the explorer stops, swipe toward the next place (the swipe that brings the
        /// explorer closest to it along the roads), tap once if the walk passes it. Every stop on the way is classified.
        /// Gives up (not reached) when no swipe gets closer, or after <see cref="MaxSwipes"/>.
        /// </summary>
        public static Leg Route(IslandMap map, Vector2Int start, HashSet<Vector2Int> zone, Func<Vector2Int, Vector2Int, WalkStop> stop, string from, string to)
        {
            var leg = new Leg { from = from, to = to };
            if (zone.Contains(start))
            {
                leg.reached = true;
                return leg;
            }
            var dist = Distances(map, zone);
            if (!dist.ContainsKey(start)) return leg;
            var walked = new List<Vector2Int>();
            var best = new List<Vector2Int>();
            var cur = start;
            var visited = new HashSet<Vector2Int> { start };
            while (leg.swipes < MaxSwipes)
            {
                int bestValue = int.MaxValue, bestHit = -1;
                string bestEnd = null;
                best.Clear();
                foreach (var w in Ways)
                {
                    string end = Swipe(map, cur, w, stop, walked);
                    if (walked.Count == 0) continue;
                    int hit = walked.FindIndex(zone.Contains);
                    if (hit < 0)
                    {
                        // The player taps where the walk comes closest (a meadow place off the road), if that beats where it stops.
                        int kMin = walked.Count - 1;
                        for (int k = 0; k < walked.Count - 1; k++)
                            if (dist.TryGetValue(walked[k], out int dk) && dk < (dist.TryGetValue(walked[kMin], out int dm) ? dm : int.MaxValue)) kMin = k;
                        if (kMin < walked.Count - 1)
                        {
                            walked.RemoveRange(kMin + 1, walked.Count - kMin - 1);
                            end = Tap;
                        }
                    }
                    int value = hit >= 0 ? -1000 + hit : dist.TryGetValue(walked[walked.Count - 1], out int d) ? d : int.MaxValue - 1;
                    if (value < bestValue || value == bestValue && walked.Count < best.Count)
                    {
                        bestValue = value;
                        bestHit = hit;
                        bestEnd = end;
                        best.Clear();
                        best.AddRange(walked);
                    }
                }
                if (bestHit < 0 && (best.Count == 0 || bestValue >= dist[cur]))
                {
                    // No swipe ends closer (the road bends away first): swipe the way the road route starts, like a player
                    // who follows the road. Never the same stop twice.
                    Vector2Int way = default;
                    int wayDist = int.MaxValue;
                    foreach (var w in Ways)
                        if (dist.TryGetValue(cur + w, out int d) && d < wayDist)
                        {
                            wayDist = d;
                            way = w;
                        }
                    if (wayDist == int.MaxValue) return leg;
                    bestEnd = Swipe(map, cur, way, stop, walked);
                    if (walked.Count == 0 || visited.Contains(walked[walked.Count - 1])) return leg;
                    best.Clear();
                    best.AddRange(walked);
                    bestHit = walked.FindIndex(zone.Contains);
                }
                leg.swipes++;
                if (bestHit >= 0)
                {
                    leg.reached = true;
                    leg.tiles += bestHit + 1;
                    if (bestHit < best.Count - 1) leg.taps++;
                    return leg;
                }
                leg.tiles += best.Count;
                if (bestEnd == Tap) leg.taps++;
                var last = best[best.Count - 1];
                var prev = best.Count > 1 ? best[best.Count - 2] : cur;
                var info = Classify(map, last, last - prev, bestEnd);
                // Two stops at one place (a junction stopped at twice, lane by lane) are one too many.
                                // A turn next to a junction is real geometry (a ring round the statue); the same kind twice in a row is not.
                if (info.Explained && leg.stops.Count > 0 && info.kind == leg.stops[leg.stops.Count - 1].kind &&
                    (info.kind == KindBranch || info.kind == KindTurn) && Chebyshev(leg.stops[leg.stops.Count - 1].tile, last) <= 2)
                {
                    info.why = "a second stop at the same junction";
                    info.kind = null;
                }
                leg.stops.Add(info);
                visited.Add(last);
                cur = last;
            }
            return leg;
        }

        /// <summary>The road ends at p walking h: no road ahead (2 tiles on) within 2 tiles to either side.</summary>
        public static bool RoadEnds(IslandMap map, Vector2Int p, Vector2Int h)
        {
            if (SmartStep.RoadLike(map, p + h, h)) return false;
            var l = Left(h);
            for (int a = 1; a <= 2; a++)
                for (int k = -2; k <= 2; k++)
                {
                    var t = p + h * a + l * k;
                    if (t.x >= 0 && t.y >= 0 && t.x < map.Columns && t.y < map.Rows && Road(map, t)) return false;
                }
            return true;
        }

        static int Chebyshev(Vector2Int a, Vector2Int b) => Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));

        /// <summary>Why the explorer stopped at <paramref name="p"/> (arrived walking <paramref name="h"/>), or null kind = unexplained.</summary>
        public static StopInfo Classify(IslandMap map, Vector2Int p, Vector2Int h, string end)
        {
            var s = new StopInfo { tile = p, heading = h, end = end };
            if (end == Tap) s.kind = Tap;
            else if (end == SmartStep.Interactable) s.kind = KindStop;
            else if (end == SmartStep.RoadEnter) s.kind = KindJoin;
            else if (!Road(map, p)) s.kind = KindOffRoad;
            else if (RealTurn(map, p, h)) s.kind = KindTurn;
            else if (RealBranch(map, p, h)) s.kind = KindBranch;
            else if (RoadEnds(map, p, h)) s.kind = KindEnd;
            else s.why = end == SmartStep.Junction ? "a junction that isn't a side road"
                : end == SmartStep.RoadEnter ? "came onto a road from the grass"
                : end == SmartStep.Blocked ? "blocked"
                : !Road(map, p + h) ? "the road shifts or ends (no road runs on 3 tiles to a side)"
                : end;
            return s;
        }
    }
}
