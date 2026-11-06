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
            Func<Vector2Int, Vector2Int, WalkStop> stopAt, List<Vector2Int> tiles, bool throughBranches = false)
        {
            tiles.Clear();
            if (map == null || direction.sqrMagnitude != 1) return Blocked;
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
                var s = stopAt != null ? stopAt(cur, next) : WalkStop.None;
                if (s == WalkStop.Before && i > 0) return Interactable;
                char was = map.At(cur.y, cur.x);
                tiles.Add(next);
                cur = next;
                if (s == WalkStop.On) return Interactable;
                if (!continuous && map.At(cur.y, cur.x) == IslandMap.TallGrass && was != IslandMap.TallGrass) return Grass;
            }
            return Cap;
        }
    }
}
