using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>Step 29: two-wide roads (A0), corridor junctions and stops (A), and the rest of the Nov 10 playtest fixes.</summary>
    public class Step29Tests
    {
        static readonly Vector2Int E = new Vector2Int(1, 0), W = new Vector2Int(-1, 0), N = new Vector2Int(0, -1), S = new Vector2Int(0, 1);

        static IslandMap Map(params string[] rows)
        {
            var m = ScriptableObject.CreateInstance<IslandMap>();
            m.SetLayout(string.Join("\n", rows));
            return m;
        }

        static (string end, List<Vector2Int> tiles) Walk(IslandMap map, Vector2Int from, Vector2Int dir,
            System.Func<Vector2Int, Vector2Int, WalkStop> stop = null)
        {
            var tiles = new List<Vector2Int>();
            string end = SmartStep.Plan(map, from, dir, true, 200, stop, tiles, false, true);
            return (end, tiles);
        }

        // ---- A0: widening ----

        [Test]
        public void Widen_OneWideRoad_GetsALaneOnOneSide()
        {
            var rows = new[] { "XXXXXXXX", "XGGGGGGX", "XPPPPPPX", "XGGGGGGX", "XXXXXXXX" };
            var res = EditorTools.Step29Paths.Widen(rows, t => false, t => false);
            Assert.AreEqual(6, res.Widened.Count);
            int y = res.Widened[0].y;
            Assert.IsTrue(res.Widened.All(t => t.y == y), "one consistent side");
            Assert.IsTrue(y == 1 || y == 3);
        }

        [Test]
        public void Widen_NeverOpensAWayRoundABlocker_AndKeepsBridges()
        {
            // The road crosses water: both sides wet = a bridge, kept.
            var bridge = EditorTools.Step29Paths.Widen(new[] { "XXXXX", "XWWWX", "XPPPX", "XWWWX", "XXXXX" }, t => false, t => false);
            Assert.AreEqual(0, bridge.Widened.Count);
            Assert.AreEqual(1, bridge.Exceptions.Count);
            // Trees on one side join the road's area; a tree touching another walkable area does not open.
            var rows = new[] { "XXXXXXX", "XGGFGGX", "XFFFFFX", "XPPPPPX", "XFFFFFX", "XXXXXXX" };
            var res = EditorTools.Step29Paths.Widen(rows, t => false, t => false);
            foreach (var t in res.Widened)
                Assert.AreNotEqual(2, t.y, $"c{t.x} r{t.y} would join the road to the grass above");
        }

        [Test]
        public void RealMaps_HaveNoOneWideRoadsLeft_ExceptTheListed()
        {
            // Listed exceptions and spots left one wide (Run-29 A0): Lilymere's lagoon crossing (c22 r13-15) and the path
            // through tall grass (c46 r44-47), Cinderwood c48 r23 and c13-14 r50.
            var allowed = new HashSet<(string, int, int)>
            {
                ("WorldMap_Lilymere", 22, 13), ("WorldMap_Lilymere", 22, 14), ("WorldMap_Lilymere", 22, 15),
                ("WorldMap_Lilymere", 46, 44), ("WorldMap_Lilymere", 46, 45), ("WorldMap_Lilymere", 46, 46), ("WorldMap_Lilymere", 46, 47),
                ("WorldMap_Cinderwood", 48, 23), ("WorldMap_Cinderwood", 13, 50), ("WorldMap_Cinderwood", 14, 50),
                // Lane gaps: the lane tile is a landmark or a scene script's tile on grass or tall grass (left as it is),
                // so the corridor is still two walkable tiles wide there.
                ("WorldMap_Galewick", 22, 7), ("WorldMap_Galewick", 43, 19), ("WorldMap_Galewick", 38, 27), ("WorldMap_Galewick", 36, 40),
                ("WorldMap_Galewick", 35, 41), ("WorldMap_Galewick", 37, 41), ("WorldMap_Cinderwood", 49, 24),
                ("WorldMap_Lilymere", 7, 37), ("WorldMap_Lilymere", 6, 38),
            };
            var bad = new List<string>();
            foreach (var name in new[] { "StillIsle", "Galewick", "Cinderwood", "Lilymere" })
            {
                var map = AssetDatabase.LoadAssetAtPath<WorldMap>($"Assets/_Project/Data/WorldMap_{name}.asset");
                Assert.IsNotNull(map, name);
                bool P(int c, int r) => map.Raw(r, c) == IslandMap.Path;
                for (int r = 0; r < map.Rows; r++)
                    for (int c = 0; c < map.Columns; c++)
                    {
                        if (!P(c, r)) continue;
                        bool h = P(c - 1, r) || P(c + 1, r), v = P(c, r - 1) || P(c, r + 1);
                        bool one = h && !P(c, r - 1) && !P(c, r + 1) || v && !P(c - 1, r) && !P(c + 1, r);
                        if (one && !allowed.Contains((map.name, c, r))) bad.Add($"{map.name} c{c} r{r}");
                    }
            }
            Assert.IsEmpty(bad, "one-wide road tiles: " + string.Join(", ", bad));
        }

        // ---- A: corridors ----

        [Test]
        public void Corridor_TwoWideStraight_NeverStops()
        {
            var map = Map("XXXXXXXXXXXX", "XPPPPPPPPPPX", "XPPPPPPPPPPX", "XXXXXXXXXXXX");
            foreach (var row in new[] { 1, 2 })
            {
                var (end, tiles) = Walk(map, new Vector2Int(1, row), E);
                Assert.AreEqual(SmartStep.Blocked, end, $"row {row}");
                Assert.AreEqual(9, tiles.Count, $"row {row} walks to the end");
                Assert.IsTrue(tiles.All(t => t.y == row), "keeps to its lane");
            }
        }

        [Test]
        public void Corridor_TwoWideLBend_TurnsByItself_InEitherLane()
        {
            // Road along rows 5-6 to the east, bends north up columns 7-8.
            var map = Map(
                "XXXXXXXXXX",
                "XGGGGGGPPX",
                "XGGGGGGPPX",
                "XGGGGGGPPX",
                "XGGGGGGPPX",
                "XPPPPPPPPX",
                "XPPPPPPPPX",
                "XXXXXXXXXX");
            foreach (var row in new[] { 5, 6 })
            {
                var (end, tiles) = Walk(map, new Vector2Int(1, row), E);
                Assert.AreNotEqual(SmartStep.Junction, end, $"lane row {row}: a bend is not a junction");
                Assert.AreEqual(1, tiles[tiles.Count - 1].y, $"lane row {row}: walks up to the top");
                int turnX = tiles.First(t => t.y < row).x;
                Assert.IsTrue(tiles.Where(t => t.y < 5).All(t => t.x == turnX), "stays in one lane after the bend");
            }
        }

        [Test]
        public void Corridor_TwoWideT_StopsOnce_AtTheSideRoad()
        {
            var map = Map(
                "XXXXXXXXXXXXXX",
                "XGGGGGPPGGGGGX",
                "XGGGGGPPGGGGGX",
                "XPPPPPPPPPPPPX",
                "XPPPPPPPPPPPPX",
                "XXXXXXXXXXXXXX");
            foreach (var row in new[] { 3, 4 })
            {
                var (end, tiles) = Walk(map, new Vector2Int(1, row), E);
                Assert.AreEqual(SmartStep.Junction, end, $"lane row {row}");
                Assert.AreEqual(new Vector2Int(6, row), tiles[tiles.Count - 1], $"lane row {row}: stops where the side road opens");
                // The next swipe on goes past it to the end.
                var (end2, more) = Walk(map, tiles[tiles.Count - 1], E);
                Assert.AreEqual(SmartStep.Blocked, end2, "past the junction: no second stop on the side road's other lane");
                Assert.AreEqual(12, more[more.Count - 1].x);
            }
        }

        [Test]
        public void Corridor_ThreeWidePlaza_WalksThrough()
        {
            var map = Map("XXXXXXXXXXXX", "XPPPPPPPPPPX", "XPPPPPPPPPPX", "XPPPPPPPPPPX", "XXXXXXXXXXXX");
            for (int row = 1; row <= 3; row++)
            {
                var (end, tiles) = Walk(map, new Vector2Int(1, row), E);
                Assert.AreEqual(SmartStep.Blocked, end, $"row {row}");
                Assert.AreEqual(10, tiles[tiles.Count - 1].x);
            }
        }

        [Test]
        public void Corridor_Crossroads_Stops_AndChevronsShowThreeWays()
        {
            var map = Map(
                "XXXXXXXXXXXX",
                "XGGGGPPGGGGX",
                "XGGGGPPGGGGX",
                "XPPPPPPPPPPX",
                "XPPPPPPPPPPX",
                "XGGGGPPGGGGX",
                "XGGGGPPGGGGX",
                "XXXXXXXXXXXX");
            var (end, tiles) = Walk(map, new Vector2Int(1, 3), E);
            Assert.AreEqual(SmartStep.Junction, end);
            var at = tiles[tiles.Count - 1];
            Assert.AreEqual(5, at.x);
            var ways = new List<Vector2Int>();
            WalkPreview.OpenWays(map, at, E, ways);
            CollectionAssert.AreEquivalent(new[] { E, N, S }, ways);
        }

        [Test]
        public void Corridor_StopsBesideADoor_AndAtTheFirstGrass()
        {
            var map = Map("XXXXXXXXXXXX", "XPPPPPPPPPPX", "XPPPPPPPPPPX", "XXXXXXXXXXXX");
            var door = new RectInt(8, 0, 1, 1);
            WalkStop Stop(Vector2Int from, Vector2Int next) =>
                WalkUp.Distance(next, door) == 1 && WalkUp.Distance(from, door) > 1 ? WalkStop.On : WalkStop.None;
            var (end, tiles) = Walk(map, new Vector2Int(1, 1), E, Stop);
            Assert.AreEqual(SmartStep.Interactable, end);
            Assert.AreEqual(new Vector2Int(7, 1), tiles[tiles.Count - 1], "stops on the first tile beside it");
        }
        // ---- B: whose turn ----

        [Test]
        public void Battle_TheirTurnLine_AndChooseLine()
        {
            Assert.AreEqual("Wild Vanehare's turn...", BattleRunes.TheirTurn("Wild Vanehare"));
            Assert.AreEqual("Choose your attack", BattleRunes.ChooseCaption);
        }

        // ---- C: health ----

        [Test]
        public void Rings_Colours_LowAndFaintedBadges()
        {
            Assert.AreEqual(PodHpRings.Green, PodHpRings.ColourFor(0.8f, false));
            Assert.AreEqual(PodHpRings.Yellow, PodHpRings.ColourFor(0.4f, false));
            Assert.AreEqual(PodHpRings.Red, PodHpRings.ColourFor(0.2f, false));
            Assert.AreEqual(PodHpRings.Grey, PodHpRings.ColourFor(0f, true));
            Assert.AreEqual("!", PodHpRings.Badge(0.25f, false), "a quarter or less blinks with !");
            Assert.AreEqual("", PodHpRings.Badge(0.3f, false));
            Assert.AreEqual("zz", PodHpRings.Badge(0f, true));
        }

        [Test]
        public void Bloom_Tiles_OnEveryRealm_BesideTheRoad_NotInGrass()
        {
            foreach (var name in new[] { "Galewick", "Cinderwood", "Lilymere" })
            {
                var map = AssetDatabase.LoadAssetAtPath<WorldMap>($"Assets/_Project/Data/WorldMap_{name}.asset");
                Assert.IsTrue(HealBloom.TryGetTile(map, out var t), $"{name} has a bloom");
                Assert.AreEqual(IslandMap.Grass, map.At(t), $"{name}: on plain grass");
                Assert.IsTrue(map.CanWalk(t), $"{name}: walkable (never in the way)");
                bool road = false;
                foreach (var d in new[] { E, W, N, S }) road |= map.At(t + d) == IslandMap.Path;
                Assert.IsTrue(road, $"{name}: beside the road");
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                        Assert.AreNotEqual(IslandMap.TallGrass, map.At(t + new Vector2Int(dx, dy)), $"{name}: not hidden in tall grass");
            }
        }

        [Test]
        public void Tired_UnderHalf_OrFainted_AndCardLines()
        {
            var go = new GameObject("party");
            try
            {
                var party = go.AddComponent<Party>();
                var species = AssetDatabase.FindAssets("t:IslingSpecies").Select(g => AssetDatabase.LoadAssetAtPath<IslingSpecies>(AssetDatabase.GUIDToAssetPath(g))).First(x => x != null);
                var c = new IslingInstance(species, 5);
                party.members.Add(c);
                Assert.IsFalse(TempleRules.AnyTired(party));
                Assert.AreEqual("", PodCard.HealTip(c));
                c.currentHp = c.MaxHp / 2 - 1;
                Assert.IsTrue(TempleRules.AnyTired(party));
                Assert.AreEqual(PodCard.TiredLine, PodCard.HealTip(c));
                c.currentHp = 0;
                Assert.AreEqual(PodCard.FaintedLine, PodCard.HealTip(c));
                Assert.IsTrue(HealBloom.AnyHurt(party));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        // ---- D: the burning fingertip ----

        static EmberCarry.Hand[] Hands(bool tracked = true, bool atEmber = false, bool over = false) =>
            new[] { default(EmberCarry.Hand), new EmberCarry.Hand { tracked = tracked, atEmber = atEmber, overFirebox = over } };

        [Test]
        public void Fingertip_TouchCatches_PokeLights_NoGripNeeded()
        {
            var carry = new EmberCarry { fingertip = true };
            Assert.AreEqual(EmberState.Held, carry.Feed(0.02f, Hands(atEmber: true)), "a touch is enough");
            Assert.AreEqual(1, carry.Holder);
            Assert.AreEqual(EmberState.Held, carry.Feed(0.5f, Hands()));
            Assert.AreEqual(EmberState.In, carry.Feed(0.02f, Hands(over: true)), "poking the target lights it");
            Assert.AreEqual(-1, carry.Holder);
        }

        [Test]
        public void Fingertip_NeverStuck_TimesOut_OrHandLost()
        {
            var carry = new EmberCarry { fingertip = true, fingerTimeout = 20f, fingerLost = 1f };
            carry.Feed(0.02f, Hands(atEmber: true));
            for (int i = 0; i < 19; i++) carry.Feed(1f, Hands());
            Assert.AreEqual(EmberState.Held, carry.State);
            Assert.AreEqual(EmberState.Falling, carry.Feed(1.1f, Hands()), "20 s unlit: the flame floats back");
            Assert.AreEqual("timeout", carry.BackReason);
            carry.Back();
            carry.Feed(0.02f, Hands(atEmber: true));
            carry.Feed(0.6f, Hands(tracked: false));
            Assert.AreEqual(EmberState.Held, carry.State, "a short tracking drop keeps it");
            Assert.AreEqual(EmberState.Falling, carry.Feed(0.6f, Hands(tracked: false)), "lost for a second: back");
            Assert.AreEqual(-1, carry.Holder);
        }
        // ---- D20, E, G, I ----

        [Test]
        public void Fishing_NeverFromABridge_OnlyFromABank()
        {
            var map = Map("XXXXXX", "XGGGGX", "XWPWWX", "XWPWWX", "XGPGGX", "XXXXXX");
            Assert.IsTrue(Fishing.Bridge(map, new Vector2Int(2, 2)), "water both sides: a bridge");
            Assert.IsFalse(Fishing.CanFish(map, new Vector2Int(2, 2), new Vector2Int(1, 2)), "no fishing off the bridge");
            Assert.IsFalse(Fishing.IsBank(map, new Vector2Int(2, 3)), "no pond line on the bridge");
            Assert.IsTrue(Fishing.IsBank(map, new Vector2Int(3, 1)), "grass above the water is a bank");
            Assert.IsTrue(Fishing.CanFish(map, new Vector2Int(3, 1), new Vector2Int(3, 2)));
            var gw = AssetDatabase.LoadAssetAtPath<WorldMap>("Assets/_Project/Data/WorldMap_Galewick.asset");
            Assert.IsNotNull(gw.waterTable, "Galewick can be fished");
            Assert.AreEqual(2, Fishing.Spots(gw).Count(), "two marked banks by the river");
            foreach (var t in Fishing.Spots(gw)) Assert.IsTrue(Fishing.IsBank(gw, t), $"c{t.x} r{t.y} is a real bank");
        }

        [Test]
        public void Encounters_CaughtWeighLess_WhileOneIsUncaught_RareStaysRare()
        {
            var a = ScriptableObject.CreateInstance<IslingSpecies>();
            var b = ScriptableObject.CreateInstance<IslingSpecies>();
            var r = ScriptableObject.CreateInstance<IslingSpecies>();
            var table = ScriptableObject.CreateInstance<EncounterTable>();
            try
            {
                table.entries = new[]
                {
                    new EncounterTable.Entry { species = a, minLevel = 2, maxLevel = 2, weight = 50f },
                    new EncounterTable.Entry { species = b, minLevel = 2, maxLevel = 2, weight = 45f },
                    new EncounterTable.Entry { species = r, minLevel = 2, maxLevel = 2, weight = 5f, rare = true },
                };
                var odds = table.Odds(s => s == a, 1f);
                Assert.AreEqual(15f / 65f, odds[0], 1e-3f, "caught: x0.3");
                Assert.AreEqual(5f / 65f, odds[2], 1e-3f, "rare keeps its weight");
                var all = table.Odds(s => s != r, 1f);
                Assert.AreEqual(50f / 100f, all[0], 1e-3f, "every common caught: back to normal");
            }
            finally
            {
                Object.DestroyImmediate(a); Object.DestroyImmediate(b); Object.DestroyImmediate(r); Object.DestroyImmediate(table);
            }
        }

        [Test]
        public void Starter_TypeLine_AndSignature()
        {
            Assert.AreEqual("Fire · strong vs Wind, weak vs Water", PodCard.TypeLine(Element.Fire));
            Assert.AreEqual("Water · strong vs Fire, weak vs Wind", PodCard.TypeLine(Element.Water));
        }

        [Test]
        public void Text_NeverCloserThan45cm_KeepsItsSize()
        {
            var head = Vector3.zero;
            var p = KeepTextAway.Pushed(head, new Vector3(0f, 0f, 0.25f), 0.45f, out float k);
            Assert.AreEqual(0.45f, p.magnitude, 1e-4f, "slides back along the view ray");
            Assert.AreEqual(0.45f / 0.25f, k, 1e-4f, "grows by the same ratio");
            var far = KeepTextAway.Pushed(head, new Vector3(0f, 0.1f, 0.6f), 0.45f, out float k2);
            Assert.AreEqual(1f, k2);
            Assert.AreEqual(new Vector3(0f, 0.1f, 0.6f), far);
        }

        [Test]
        public void BugReport_KeepsTheLastTwoMinutes()
        {
            var q = new Queue<(float at, string line)>();
            for (int i = 0; i < 300; i++) q.Enqueue((i, "line " + i));
            BugReport.Trim(q, 300f, 120f, 6000);
            Assert.AreEqual(120, q.Count);
            Assert.AreEqual("line 180", q.Peek().line);
        }
    }
}
