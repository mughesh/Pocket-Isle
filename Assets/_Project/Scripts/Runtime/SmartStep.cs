using System;
using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>What a tile means for a smart step (step 12a): keep going, stop before stepping on it, or stop on it.</summary>
    public enum WalkStop
    {
        None,
        Before,
        On,
    }

    /// <summary>
    /// Step 12a: plans a swipe walk. One swipe = a smart step: walk the swipe direction, follow the path's turns, stop at
    /// the first junction, interactable (<c>stopAt</c>), tall-grass edge (stops on the first grass tile), blocked tile or
    /// <see cref="SmartTiles"/> tiles. Continuous (two quick swipes) stops only at junctions, interactables and blocked
    /// tiles. Pure logic over <see cref="IslandMap"/>; tiles are (column, row).
    /// </summary>
    public static class SmartStep
    {
        public const int SmartTiles = 5;

        public const string Cap = "cap";
        public const string Blocked = "blocked";
        public const string Junction = "junction";
        public const string Grass = "grass";
        public const string Interactable = "interactable";
        public const string Turns = "turns";
        /// <summary>Nov 11 (R51): the plan was cut at the island window's edge (the explorer never leaves the view).</summary>
        public const string View = "view";
        /// <summary>Nov 11 (R52): the walk came onto a road from off it: it stops on the road (a decision point, like a junction).</summary>
        public const string RoadEnter = "road";
        /// <summary>Nov 11 (R52): the road ended under a walk along it: it stops on the last road tile instead of walking off into the grass.</summary>
        public const string RoadEnd = "road end";

        /// <summary>A second swipe the same way within the window while walking = walk on (else a fresh smart step).</summary>
        public static bool WalkOn(Vector2Int lastWay, float lastAt, Vector2Int way, float now, float window, bool walking) =>
            walking && way == lastWay && now - lastAt <= window;

        static bool PathWalk(IslandMap map, Vector2Int t) => map.At(t.y, t.x) == IslandMap.Path && map.CanWalk(t);

        /// <summary>
        /// Nov 12: a road tile, or a single walkable tile in a road's lane with road on both sides along <paramref name="h"/>
        /// (a kite or a landmark left on the grass inside a lane): the walk goes straight over it.
        /// </summary>
        public static bool RoadLike(IslandMap map, Vector2Int t, Vector2Int h) =>
            PathWalk(map, t) || map.CanWalk(t) && PathWalk(map, t - h) && PathWalk(map, t + h);

        static Vector2Int Left(Vector2Int h) => new Vector2Int(-h.y, h.x);

        /// <summary>
        /// A side branch off a path tile: a path tile beside it whose diagonals (ahead and behind) are not path, so it is
        /// a road leaving, not the other lane of a wide road or a plaza.
        /// </summary>
        static bool Branch(IslandMap map, Vector2Int cur, Vector2Int h, Vector2Int side) =>
            PathWalk(map, cur + side) && !PathWalk(map, cur + side + h) && !PathWalk(map, cur + side - h);

        /// <summary>
        /// Fills <paramref name="tiles"/> with the tiles to walk from <paramref name="from"/> (excluding it) and returns why
        /// the walk ends (<see cref="Cap"/>, <see cref="Blocked"/>, ...). The first step always goes the swipe way when
        /// that tile is walkable (a "stop before" there is ignored, so a swipe can leave a stop); empty = can't move.
        /// <paramref name="stopAt"/> gets (from tile, next tile).
        /// </summary>
        public static string Plan(IslandMap map, Vector2Int from, Vector2Int direction, bool continuous, int maxTiles,
            Func<Vector2Int, Vector2Int, WalkStop> stopAt, List<Vector2Int> tiles, bool throughBranches = false, bool corridors = false)
        {
            tiles.Clear();
            if (map == null || direction.sqrMagnitude != 1) return Blocked;
            if (corridors) return PlanCorridor(map, from, direction, maxTiles, stopAt, tiles);
            var cur = from;
            var h = direction;
            bool turned = false;
            for (int i = 0; i < maxTiles; i++)
            {
                var next = cur + h;
                bool turnNow = false;
                if (i > 0 && PathWalk(map, cur))
                {
                    var l = Left(h);
                    bool fwd = PathWalk(map, cur + h);
                    if (fwd)
                    {
                        // Nov 6 (walk until tap): straight on past side roads; a swipe that way mid-walk turns there.
                        if (!throughBranches && (Branch(map, cur, h, l) || Branch(map, cur, h, -l))) return Junction;
                    }
                    else
                    {
                        bool a = PathWalk(map, cur + l), b = PathWalk(map, cur - l);
                        if (a && b) return Junction;
                        if (a || b)
                        {
                            // Two turns in a row (a wide road's end, a zigzag): stop rather than double back.
                            if (turned) return Turns;
                            h = a ? l : -l;
                            next = cur + h;
                            turnNow = true;
                        }
                    }
                }
                turned = turnNow;
                if (!map.CanWalk(next)) return Blocked;
                if (LeavesRoad(map, cur, next, i)) return RoadEnd;
                var s = stopAt != null ? stopAt(cur, next) : WalkStop.None;
                if (s == WalkStop.Before && i > 0) return Interactable;
                char was = map.At(cur.y, cur.x);
                tiles.Add(next);
                cur = next;
                if (s == WalkStop.On) return Interactable;
                if (EntersRoad(map, was, cur)) return RoadEnter;
                if (!continuous && map.At(cur.y, cur.x) == IslandMap.TallGrass && was != IslandMap.TallGrass) return Grass;
            }
            return Cap;
        }

        // ---- Nov 11 (R52): roads are sticky ----

        /// <summary>
        /// Nov 11 (Mugesh: a walk toward a road crossed it straight into the grass beyond): coming onto a road from off it
        /// ends the walk on that road tile. The next swipe follows the road, or crosses it.
        /// </summary>
        static bool EntersRoad(IslandMap map, char was, Vector2Int now) => was != IslandMap.Path && map.At(now.y, now.x) == IslandMap.Path;

        /// <summary>
        /// A walk along a road never wanders off its end into the grass: step <paramref name="i"/> from a road tile onto a
        /// non-road tile is only the swipe's own first steps (<see cref="GoesOn"/>: the road didn't continue that way, e.g.
        /// crossing a two-wide road's other lane), never after following the road.
        /// </summary>
        static bool LeavesRoad(IslandMap map, Vector2Int cur, Vector2Int next, int i) =>
            i >= GoesOn && map.At(cur.y, cur.x) == IslandMap.Path && map.At(next.y, next.x) != IslandMap.Path;

        // ---- step 29: corridors (roads two tiles wide) ----

        /// <summary>A side opening counts when the road reaches this many tiles further that way than it did one step back.</summary>
        public const int OpeningGain = 2;

        /// <summary>A way "goes on" when the road runs at least this many tiles that way (a road's width is 2).</summary>
        public const int GoesOn = 2;

        /// <summary>Road tiles in a row from <paramref name="at"/> (excluded) toward <paramref name="way"/>.</summary>
        public static int RoadRun(IslandMap map, Vector2Int at, Vector2Int way, int max = 8)
        {
            int n = 0;
            for (var t = at + way; n < max && PathWalk(map, t); t += way) n++;
            return n;
        }

        /// <summary>
        /// Step 29: the ways the road opens from <paramref name="cur"/> when walking <paramref name="h"/>, per corridor, not
        /// per tile. Ahead counts when the road runs on at least <see cref="GoesOn"/> tiles. A side counts only where the road
        /// reaches <see cref="OpeningGain"/> or more tiles further that way than it did one tile back: the other lane of a
        /// two-wide road (or a plaza's width) is the same on every step, so it never counts; a road leaving is a jump.
        /// </summary>
        public static void Openings(IslandMap map, Vector2Int cur, Vector2Int h, out bool ahead, out bool left, out bool right)
        {
            var l = Left(h);
            var back = cur - h;
            ahead = RoadRun(map, cur, h) >= GoesOn;
            left = RoadRun(map, cur, l) >= RoadRun(map, back, l) + OpeningGain;
            right = RoadRun(map, cur, -l) >= RoadRun(map, back, -l) + OpeningGain;
        }

        /// <summary>Nov 12: the walk stopped where the road turns (it can't go on straight, a road runs on to a side).</summary>
        public const string Corner = "corner";

        /// <summary>Nov 12, measured only: plain corners turn by themselves (the step 29 behaviour). Off: Mugesh's rule stops at corners.</summary>
        public static bool FollowBends;

        /// <summary>Nov 12: a side road counts when it reaches this many tiles past the corridor's own width.</summary>
        public const int SideReach = 3;

        /// <summary>Nov 12: a side road is at most this many rows wide; wider is a plaza or a stretch where the road is wider.</summary>
        public const int SideMaxRows = 4;

        /// <summary>The corridor's width to side <paramref name="s"/> round <paramref name="p"/>: the fewest road tiles that way within 3 rows.</summary>
        public static int CorridorEdge(IslandMap map, Vector2Int p, Vector2Int h, Vector2Int s)
        {
            int edge = int.MaxValue;
            for (int j = -3; j <= 3; j++)
            {
                var t = p + h * j;
                if (!PathWalk(map, t)) continue;
                edge = Mathf.Min(edge, RoadRun(map, t, s, 16));
            }
            return edge == int.MaxValue ? 0 : edge;
        }

        /// <summary>
        /// Nov 12 (Mugesh's stopping rule): a real side road leaves at <paramref name="cur"/> to side <paramref name="s"/>:
        /// the road reaches at least <see cref="SideReach"/> tiles past the corridor's width there, over 2 to
        /// <see cref="SideMaxRows"/> rows (a lane, not a plaza or a wider stretch), <paramref name="cur"/> is its first row
        /// walking <paramref name="h"/>, and the road goes on straight past it (else it is a corner, stopped at its end).
        /// Ragged edges, notches, stray tiles and stubs never count.
        /// </summary>
        public static bool SideRoad(IslandMap map, Vector2Int cur, Vector2Int h, Vector2Int s)
        {
            if (!PathWalk(map, cur)) return false;
            int need = CorridorEdge(map, cur, h, s) + SideReach;
            bool Wide(Vector2Int t) => PathWalk(map, t) && RoadRun(map, t, s, 16) >= need;
            if (!Wide(cur) || Wide(cur - h)) return false;
            int rows = 1;
            var t = cur + h;
            for (; rows <= SideMaxRows && Wide(t); t += h) rows++;
            if (rows < 2 || rows > SideMaxRows) return false;
            // The road goes on straight past the side road (two tiles at least).
            return RoadRun(map, t - h, h) >= GoesOn + 1;
        }

        /// <summary>
        /// Nov 12 (Mugesh: "it clearly looks like a bad layout for the road", three swipes to cross one field): the swipe walk
        /// goes straight in the swipe's way and stops only where the road turns (straight on is off the road and the road
        /// runs on to a side: <see cref="Corner"/>), where a real side road leaves (<see cref="SideRoad"/>:
        /// <see cref="Junction"/>), at the road's end, one tile before a door or a stop from <paramref name="stopAt"/>, at a
        /// blocked tile, and on a road it came onto from the grass (R52). It never turns by itself. Off the road it walks
        /// straight. A swipe across a road (the road doesn't run that way from the start) may cross the other lane and go on.
        /// </summary>
        static string PlanCorridor(IslandMap map, Vector2Int from, Vector2Int direction, int maxTiles,
            Func<Vector2Int, Vector2Int, WalkStop> stopAt, List<Vector2Int> tiles)
        {
            var cur = from;
            var h = direction;
            var l = Left(h);
            // Along a road from the start (the road runs on this way): its end and its corners stop the walk at once.
            bool along = PathWalk(map, from) && RoadRun(map, from, h) >= GoesOn;
            bool bent = false;
            for (int i = 0; i < maxTiles; i++)
            {
                if (bent && i > 0 && PathWalk(map, cur) && RoadRun(map, cur - h, h) > 2) bent = false; // a new straight: the next corner may bend too
                if (i > 0 && PathWalk(map, cur))
                {
                    if (!RoadLike(map, cur + h, h))
                    {
                        if (along || i >= GoesOn)
                        {
                            int a = RoadRun(map, cur, l), b = RoadRun(map, cur, -l);
                            // Measured option (off by default, Mugesh's rule stops at corners): a plain corner, one way on, turns by itself.
                            if (FollowBends && !bent && (a >= SideReach) != (b >= SideReach) && Mathf.Min(a, b) <= 1)
                            {
                                h = a >= SideReach ? l : -l;
                                l = Left(h);
                                bent = true;
                            }
                            else return a >= SideReach || b >= SideReach ? Corner : RoadEnd;
                        }
                    }
                    else if (SideRoad(map, cur, h, l) || SideRoad(map, cur, h, -l)) return Junction;
                }
                var next = cur + h;
                if (!map.CanWalk(next)) return Blocked;
                if (i >= GoesOn && PathWalk(map, cur) && !RoadLike(map, next, h)) return RoadEnd;
                var s = stopAt != null ? stopAt(cur, next) : WalkStop.None;
                if (s == WalkStop.Before && i > 0) return Interactable;
                bool wasRoad = RoadLike(map, cur, h);
                tiles.Add(next);
                cur = next;
                if (s == WalkStop.On) return Interactable;
                if (!wasRoad && PathWalk(map, cur)) return RoadEnter;
            }
            return Cap;
        }

        /// <summary>How far ahead a roaming walk compares the two ways it could turn.</summary>
        const int RoamLook = 30;

        /// <summary>
        /// Step 28: a lesson walk. Straight on over any walkable tile (roads, grass, tall grass); at something it can't enter
        /// it turns to the side with the longer open run, or back if both sides are shut. It never ends by itself:
        /// <see cref="Cap"/> means "plan more", and only a boxed-in explorer gets <see cref="Blocked"/>.
        /// <paramref name="blocked"/> adds tiles it must keep off (the gates).
        /// </summary>
        public static string PlanRoam(IslandMap map, Vector2Int from, Vector2Int direction, Func<Vector2Int, bool> blocked, int maxTiles, List<Vector2Int> tiles)
        {
            tiles.Clear();
            if (map == null || direction.sqrMagnitude != 1) return Blocked;
            bool Open(Vector2Int t) => map.CanWalk(t) && (blocked == null || !blocked(t));
            int Run(Vector2Int at, Vector2Int way)
            {
                int n = 0;
                for (var t = at + way; n < RoamLook && Open(t); t += way) n++;
                return n;
            }
            var cur = from;
            var h = direction;
            for (int i = 0; i < maxTiles; i++)
            {
                if (!Open(cur + h))
                {
                    var l = Left(h);
                    int a = Run(cur, l), b = Run(cur, -l);
                    if (a > 0 || b > 0) h = a >= b ? l : -l;
                    else if (Open(cur - h)) h = -h;
                    else return Blocked;
                }
                cur += h;
                tiles.Add(cur);
            }
            return Cap;
        }
    }
}
