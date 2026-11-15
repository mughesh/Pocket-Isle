using System;
using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Nov 11 (Mugesh: "the walk has changed so often, simulate it everywhere"): plans the swipe walk from every walkable
    /// tile of a map in all four directions, the way the game does (<see cref="SmartStep.Plan"/> on corridors, walking on
    /// until a tap, replanned when a plan runs out), and checks every plan against the walking rules: only walkable tiles,
    /// never through a road (R52), never off a road after following it, never through a junction, never past a stop
    /// (gates, doors, shrines, the healing bloom), never ending on a blocked tile, and it ends. Pure; the Editor's
    /// Nov11Walk.Sim writes the report, Nov11WalkTests asserts it is clean.
    /// </summary>
    public static class WalkSim
    {
        public const int MaxTiles = 200;
        public const int MaxRounds = 10;

        public const string OffMap = "off the map or onto a blocked tile";
        public const string ThroughRoad = "walked through a road instead of stopping on it";
        public const string OffRoad = "walked off a road after following it";
        public const string ThroughJunction = "walked through a junction";
        public const string Turned = "turned by itself (Nov 12: the walk goes straight)";
        public const string PastStop = "walked past a stop (gate, door, shrine, bloom)";
        public const string EndsBlocked = "ends on a blocked tile";
        public const string Loops = "never ends (walks on after " + "10 plans)";

        public sealed class Report
        {
            public string map;
            public int starts, plans, failed;
            public readonly Dictionary<string, int> byKind = new Dictionary<string, int>();
            public readonly Dictionary<string, int> ends = new Dictionary<string, int>();
            /// <summary>"c12 r7 north: walked through a road ... at c12 r5", at most <see cref="MaxListed"/>.</summary>
            public readonly List<string> failures = new List<string>();
            public const int MaxListed = 80;

            public bool Clean => failed == 0;

            public override string ToString()
            {
                var sb = new System.Text.StringBuilder($"{map}: {starts} starts, {plans} plans, {failed} failed");
                foreach (var k in byKind) sb.Append($"; {k.Value} x {k.Key}");
                return sb.ToString();
            }
        }

        static readonly Vector2Int[] Ways = { new Vector2Int(0, -1), new Vector2Int(0, 1), new Vector2Int(-1, 0), new Vector2Int(1, 0) };
        static readonly string[] WayNames = { "forward", "back", "left", "right" };

        /// <summary>
        /// The game's built-in stops on a map (GestureTestPiece.BuiltInStop without the scene's hand moments): before a
        /// gate, on the first tile beside a door / shrine footprint, on the closest tile to the healing bloom, and
        /// optionally on the first tall grass (before the first wild Isling).
        /// </summary>
        public static Func<Vector2Int, Vector2Int, WalkStop> GameStops(WorldMap world, List<RectInt> footprints, bool firstGrass)
        {
            bool bloom = HealBloom.TryGetTile(world, out var bloomTile);
            return (from, next) =>
            {
                foreach (var g in world.gates)
                    if (g.Covers(next) && !g.Covers(from)) return WalkStop.Before;
                if (footprints != null)
                    foreach (var fp in footprints)
                        if (WalkUp.Distance(next, fp) == 1 && WalkUp.Distance(from, fp) > 1) return WalkStop.On;
                if (firstGrass && world.At(next) == IslandMap.TallGrass && world.At(from) != IslandMap.TallGrass) return WalkStop.On;
                if (bloom && HealBloom.ClosestApproach(from, next, bloomTile, 2.3f)) return WalkStop.On;
                return WalkStop.None;
            };
        }

        /// <summary>Every walkable tile x four ways, walking on until something stops it.</summary>
        public static Report Run(IslandMap map, Func<Vector2Int, Vector2Int, WalkStop> stop, bool corridors = true, int maxTiles = MaxTiles)
        {
            var rep = new Report { map = map != null ? map.name : "?" };
            if (map == null) return rep;
            var tiles = new List<Vector2Int>();
            for (int r = 0; r < map.Rows; r++)
                for (int c = 0; c < map.Columns; c++)
                {
                    var start = new Vector2Int(c, r);
                    if (!map.CanWalk(start)) continue;
                    rep.starts++;
                    for (int w = 0; w < Ways.Length; w++) Walk(map, start, Ways[w], WayNames[w], stop, corridors, maxTiles, tiles, rep);
                }
            return rep;
        }

        static void Walk(IslandMap map, Vector2Int start, Vector2Int way, string wayName, Func<Vector2Int, Vector2Int, WalkStop> stop,
            bool corridors, int maxTiles, List<Vector2Int> tiles, Report rep)
        {
            var cur = start;
            var h = way;
            string end = null;
            for (int round = 0; round < MaxRounds; round++)
            {
                end = SmartStep.Plan(map, cur, h, true, maxTiles, stop, tiles, false, corridors);
                rep.plans++;
                Check(map, cur, tiles, stop, start, wayName, rep);
                if (end != SmartStep.Cap || tiles.Count == 0) break;
                var last = tiles[tiles.Count - 1];
                var prev = tiles.Count > 1 ? tiles[tiles.Count - 2] : cur;
                h = last - prev;
                cur = last;
                if (round == MaxRounds - 1) end = Loops;
            }
            string key = end ?? SmartStep.Blocked;
            rep.ends[key] = rep.ends.TryGetValue(key, out int n) ? n + 1 : 1;
            if (end == Loops) Fail(rep, start, wayName, Loops, cur);
        }

        static bool Road(IslandMap map, Vector2Int t) => map.IsRoad(t);

        static void Check(IslandMap map, Vector2Int from, List<Vector2Int> tiles, Func<Vector2Int, Vector2Int, WalkStop> stop, Vector2Int start, string way, Report rep)
        {
            int n = tiles.Count;
            if (n == 0) return;
            for (int k = 0; k < n; k++)
            {
                var t = tiles[k];
                var prev = k > 0 ? tiles[k - 1] : from;
                bool last = k == n - 1;
                if (!map.CanWalk(t))
                {
                    Fail(rep, start, way, last ? EndsBlocked : OffMap, t);
                    continue;
                }
                if ((t - prev).sqrMagnitude != 1) Fail(rep, start, way, OffMap, t);
                var dir = t - prev;
                bool prevRoad = SmartStep.RoadLike(map, prev, dir);
                if (!last && Road(map, t) && !prevRoad) Fail(rep, start, way, ThroughRoad, t);
                if (k >= SmartStep.GoesOn && Road(map, prev) && !SmartStep.RoadLike(map, t, dir)) Fail(rep, start, way, OffRoad, t);
                if (k >= 1 && Road(map, prev))
                {
                    // Nov 12: straight only, through no real side road.
                    var before = k > 1 ? tiles[k - 2] : from;
                    var h = prev - before;
                    if (t - prev != h) Fail(rep, start, way, Turned, prev);
                    var side = new Vector2Int(-h.y, h.x);
                    if (SmartStep.SideRoad(map, prev, h, side) || SmartStep.SideRoad(map, prev, h, -side)) Fail(rep, start, way, ThroughJunction, prev);
                }
                if (stop == null) continue;
                var s = stop(prev, t);
                if (!last && s == WalkStop.On) Fail(rep, start, way, PastStop, t);
                if (k > 0 && s == WalkStop.Before) Fail(rep, start, way, PastStop, t);
            }
        }

        static void Fail(Report rep, Vector2Int start, string way, string kind, Vector2Int at)
        {
            rep.failed++;
            rep.byKind[kind] = rep.byKind.TryGetValue(kind, out int n) ? n + 1 : 1;
            if (rep.failures.Count < Report.MaxListed) rep.failures.Add($"c{start.x} r{start.y} {way}: {kind} at c{at.x} r{at.y}");
        }
    }
}
