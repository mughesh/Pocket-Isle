using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>
    /// Nov 12 overnight, section A: Mugesh's stopping rule (straight on, stop only where the road turns or a real side road
    /// leaves) and the swipe count on every realm's routes (no unexplained stop, no leg needing more swipes than the budget).
    /// </summary>
    public class Nov12WalkTests
    {
        static readonly Vector2Int E = new Vector2Int(1, 0), N = new Vector2Int(0, -1);

        static IslandMap Map(params string[] rows)
        {
            var m = ScriptableObject.CreateInstance<IslandMap>();
            m.SetLayout(string.Join("\n", rows));
            return m;
        }

        static (string end, List<Vector2Int> tiles) Walk(IslandMap map, Vector2Int from, Vector2Int dir)
        {
            var tiles = new List<Vector2Int>();
            string end = SmartStep.Plan(map, from, dir, true, 200, null, tiles, false, true);
            return (end, tiles);
        }

        [Test]
        public void Rule_AStubOfTwo_ANotch_AndAStrayTile_NeverStopTheWalk()
        {
            var map = Map(
                "XXXXXXXXXXXXXXXX",
                "XGGGPGGGGGGPGGGX",
                "XGGGPGGGGGGGGGGX",
                "XPPPPPPPPPPPPPPX",
                "XPPPPPPGPPPPPPPX",
                "XGGGGGGGGGGGGGGX",
                "XXXXXXXXXXXXXXXX");
            // Row 3: a stub of two at column 4 (rows 1-2), a stray road tile at column 11 row 1 (not touching).
            // Row 4: a notch (grass) at column 7, a lane gap the walk steps over.
            foreach (int row in new[] { 3, 4 })
            {
                var (end, tiles) = Walk(map, new Vector2Int(1, row), E);
                Assert.AreEqual(14, tiles[tiles.Count - 1].x, $"row {row}: straight to the road's end, {end}");
                Assert.AreNotEqual(SmartStep.Junction, end);
            }
        }

        [Test]
        public void Rule_AWiderStretch_AndAPlaza_NeverStop()
        {
            var map = Map(
                "XXXXXXXXXXXXXXXX",
                "XGGGGPPPPPPGGGGX",
                "XGGGGPPPPPPGGGGX",
                "XGGGGPPPPPPGGGGX",
                "XPPPPPPPPPPPPPPX",
                "XPPPPPPPPPPPPPPX",
                "XGGGGGGGGGGGGGGX",
                "XXXXXXXXXXXXXXXX");
            foreach (int row in new[] { 4, 5 })
            {
                var (end, tiles) = Walk(map, new Vector2Int(1, row), E);
                Assert.AreEqual(14, tiles[tiles.Count - 1].x, $"row {row}: a plaza six wide is not a side road ({end})");
            }
        }

        [Test]
        public void Rule_ARealSideRoad_StopsOnce_OnItsFirstRow()
        {
            var map = Map(
                "XXXXXXXXXXXXXXXX",
                "XGGGGGPPGGGGGGGX",
                "XGGGGGPPGGGGGGGX",
                "XGGGGGPPGGGGGGGX",
                "XPPPPPPPPPPPPPPX",
                "XPPPPPPPPPPPPPPX",
                "XGGGGGGGGGGGGGGX",
                "XXXXXXXXXXXXXXXX");
            var (end, tiles) = Walk(map, new Vector2Int(1, 4), E);
            Assert.AreEqual(SmartStep.Junction, end);
            Assert.AreEqual(new Vector2Int(6, 4), tiles[tiles.Count - 1]);
            var (end2, more) = Walk(map, tiles[tiles.Count - 1], E);
            Assert.AreEqual(14, more[more.Count - 1].x, $"no second stop on the side road's other lane ({end2})");
            var (_, up) = Walk(map, tiles[tiles.Count - 1], N);
            Assert.AreEqual(1, up[up.Count - 1].y, "the swipe up the side road walks it to its end");
        }

        [Test]
        public void Rule_ACorner_Stops_AndNeverTurnsByItself()
        {
            var map = Map(
                "XXXXXXXXXX",
                "XGGGGGGPPX",
                "XGGGGGGPPX",
                "XGGGGGGPPX",
                "XPPPPPPPPX",
                "XPPPPPPPPX",
                "XXXXXXXXXX");
            var (end, tiles) = Walk(map, new Vector2Int(1, 4), E);
            Assert.AreEqual(SmartStep.Corner, end);
            Assert.IsTrue(tiles.All(t => t.y == 4));
        }

        [Test]
        public void Rule_AKiteInTheLane_IsSteppedOver()
        {
            // A single walkable grass tile inside a lane (a kite or landmark left on the grass): straight over it.
            var map = Map("XXXXXXXXXXXX", "XPPPPGPPPPPX", "XPPPPPPPPPPX", "XXXXXXXXXXXX");
            var (end, tiles) = Walk(map, new Vector2Int(1, 1), E);
            Assert.AreEqual(10, tiles[tiles.Count - 1].x, end);
        }

        [Test]
        public void SwipeSim_FlagsAJog()
        {
            // A two-wide road north that shifts one tile east: the stop at the shift is unexplained.
            var map = Map(
                "XXXXXXX",
                "XGGPPGX",
                "XGGPPGX",
                "XGGPPGX",
                "XGPPGGX",
                "XGPPGGX",
                "XGPPGGX",
                "XXXXXXX");
            var zone = new HashSet<Vector2Int> { new Vector2Int(3, 1) };
            var leg = SwipeSim.Route(map, new Vector2Int(2, 6), zone, null, "a", "b");
            Assert.IsTrue(leg.reached);
            Assert.Greater(leg.Unexplained, 0, "a jog is not a turn");
        }

        [Test]
        public void SwipeSim_EveryRealm_EveryStopIsExplained_AndEveryLegReached()
        {
            // The one known exception: the statue sits on the hub's crossroads, so the way round it from the fire gate turns
            // twice two tiles apart (east to the statue, north round it, the ring's corner). Real geometry, kept.
            var allowed = new HashSet<(string, int, int)> { ("WorldMap_StillIsle", 13, 14) };
            var results = EditorTools.Nov12Walk.RunAll();
            Assert.AreEqual(4, results.Count, "four realms");
            var bad = new List<string>();
            foreach (var r in results)
                foreach (var l in r.main.Concat(r.pairs))
                {
                    if (!l.reached) bad.Add($"{r.map} {l.from} -> {l.to}: not reached");
                    foreach (var s in l.stops.Where(s => !s.Explained && !allowed.Contains((r.map, s.tile.x, s.tile.y)))) bad.Add($"{r.map} {l.from} -> {l.to}: c{s.tile.x} r{s.tile.y} {s.why}");
                }
            Assert.IsEmpty(bad, string.Join("\n", bad.Take(20)));
        }

        [Test]
        public void SwipeSim_NoLegNeedsMoreSwipesThanTheBudget()
        {
            // The budget is the AFTER table of the Nov 12 run (Logs/WalkSim/swipes_after_budget.txt). Fewer is fine: then
            // copy the new budget over this file.
            var path = "Assets/_Project/Scripts/Tests/Editor/Nov12SwipeBudget.txt";
            var budget = new Dictionary<string, int>();
            foreach (var line in File.ReadAllLines(path))
            {
                var p = line.Split('|');
                if (p.Length == 4) budget[$"{p[0]}|{p[1]}|{p[2]}"] = int.Parse(p[3]);
            }
            Assert.Greater(budget.Count, 20);
            var worse = new List<string>();
            foreach (var r in EditorTools.Nov12Walk.RunAll())
                foreach (var l in r.main.Concat(r.pairs))
                    if (budget.TryGetValue($"{r.map}|{l.from}|{l.to}", out int max) && l.swipes > max)
                        worse.Add($"{r.map} {l.from} -> {l.to}: {l.swipes} swipes, budget {max}");
            Assert.IsEmpty(worse, string.Join("\n", worse));
        }
    }
}
