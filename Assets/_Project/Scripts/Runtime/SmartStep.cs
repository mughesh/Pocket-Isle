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

        /// <summary>
        /// Step 29: a swipe walk on corridors. Straight on along the road; at a bend (one way on, not ahead) it turns by
        /// itself at the first tile that opens that way (so it keeps to its lane); at a real junction (two or more ways on)
        /// it stops (<see cref="Junction"/>) and the next swipe picks; a dead end, a blocked tile or a stop from
        /// <paramref name="stopAt"/> ends it. Off the road it walks straight. Only a tap or these stop it.
        /// </summary>
        static string PlanCorridor(IslandMap map, Vector2Int from, Vector2Int direction, int maxTiles,
            Func<Vector2Int, Vector2Int, WalkStop> stopAt, List<Vector2Int> tiles)
        {
            var cur = from;
            var h = direction;
            for (int i = 0; i < maxTiles; i++)
            {
                if (i > 0 && PathWalk(map, cur))
                {
                    Openings(map, cur, h, out bool ahead, out bool a, out bool b);
                    int ways = (ahead ? 1 : 0) + (a ? 1 : 0) + (b ? 1 : 0);
                    if (ways >= 2) return Junction;
                    if (!ahead && (a || b)) h = a ? Left(h) : -Left(h);
                    // Not ahead and no opening: walk to the road's end (or the other lane's corner) and stop there.
                }
                var next = cur + h;
                if (!map.CanWalk(next)) return Blocked;
                if (LeavesRoad(map, cur, next, i)) return RoadEnd;
                var s = stopAt != null ? stopAt(cur, next) : WalkStop.None;
                if (s == WalkStop.Before && i > 0) return Interactable;
                char was = map.At(cur.y, cur.x);
                tiles.Add(next);
                cur = next;
                if (s == WalkStop.On) return Interactable;
                if (EntersRoad(map, was, cur)) return RoadEnter;
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
