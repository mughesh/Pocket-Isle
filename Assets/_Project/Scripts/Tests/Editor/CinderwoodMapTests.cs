using NUnit.Framework;
using UnityEditor;
using Diorama.EditorTools;

namespace Diorama.Tests
{
    /// <summary>
    /// Step 9a: the 60 x 60 Cinderwood map's layout checks (trail connects arrival, shrine, springs and the log; every
    /// pocket reachable; nothing cut off except behind the log; tall grass share; trail bends; filler spacing; edges)
    /// and its per-zone tables. Needs Set Up MR_Main (9a Cinderwood) to have written the map.
    /// </summary>
    public class CinderwoodMapTests
    {
        static WorldMap Load()
        {
            var map = AssetDatabase.LoadAssetAtPath<WorldMap>(CinderwoodTools.MapPath);
            if (map == null) Assert.Inconclusive($"{CinderwoodTools.MapPath} missing (run Tools > Pocket Isle > Set Up MR_Main (9a Cinderwood)).");
            return map;
        }

        [Test]
        public void Layout_PassesTheChecks()
        {
            var problems = CinderwoodTools.Check(Load(), null);
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void Zones_HaveTheirOwnTables_RuinsHoldRareGlazegolem()
        {
            var map = Load();
            Assert.AreEqual("ruins", map.ZoneAt(CinderwoodTools.Tile(50, 52)));
            var ruins = map.GrassTableAt(CinderwoodTools.Tile(50, 52));
            Assert.AreNotEqual(map.grassTable, ruins);
            bool glaze = false;
            foreach (var e in ruins.entries)
                if (e.species != null && e.species.name == "Glazegolem")
                {
                    glaze = true;
                    Assert.IsTrue(e.rare);
                    // Lv 7-9 + the step 9f wild level bonus.
                    Assert.AreEqual(7 + CinderwoodTools.WildLevelBonus, e.minLevel);
                    Assert.AreEqual(9 + CinderwoodTools.WildLevelBonus, e.maxLevel);
                }
            Assert.IsTrue(glaze, "Glazegolem in the ruins table");
            Assert.AreEqual("forest", map.ZoneAt(CinderwoodTools.Tile(10, 16)));
            Assert.AreEqual(map.grassTable, map.GrassTableAt(CinderwoodTools.Tile(2, 2)), "outside the zones: the map's table");
        }

        static string PropName(WorldMap map, UnityEngine.Vector2Int xy)
        {
            var t = CinderwoodTools.Tile(xy);
            return map.TryGetProp(map.PropAt(t.y, t.x), out var e) && e.recipe != null ? e.recipe.name : null;
        }

        [Test]
        public void Forge_BellowsPipeAndBraziersInARow_DoorFrontReachableOnceTheLogIsOpen()
        {
            var map = Load();
            Assert.AreEqual("Bellows", PropName(map, CinderwoodTools.BellowsXY));
            Assert.AreEqual("ForgePipe", PropName(map, CinderwoodTools.PipeXY));
            for (int i = 0; i < CinderwoodTools.BrazierXY.Length; i++)
                if (i != CinderwoodTools.TopBrazier) Assert.AreEqual("Brazier", PropName(map, CinderwoodTools.BrazierXY[i]), $"brazier at {CinderwoodTools.BrazierXY[i]}");
            Assert.AreEqual("ForgeDoor", PropName(map, CinderwoodTools.DoorXY));
            var log = CinderwoodTools.LogTile;
            var front = CinderwoodTools.Tile(CinderwoodTools.DoorXY + new UnityEngine.Vector2Int(1, -1));
            var logFront = CinderwoodTools.Tile(CinderwoodTools.LogXY + UnityEngine.Vector2Int.down);
            try
            {
                Assert.IsFalse(ExplorerPath.CanReach(map, logFront, front, 400), "the log blocks the way to the forge");
                map.SetPropOpen(log, true);
                Assert.IsTrue(ExplorerPath.CanReach(map, logFront, front, 400), "log moved: the forge door is reachable");
                var door = CinderwoodTools.Tile(CinderwoodTools.DoorXY);
                Assert.IsFalse(map.CanWalk(door + UnityEngine.Vector2Int.right), "the shut door blocks");
                map.SetPropOpen(door, true);
                Assert.IsTrue(map.CanWalk(door + UnityEngine.Vector2Int.right), "the open door can be walked into");
            }
            finally
            {
                map.ClearRuntimeChanges();
            }
        }

        [Test]
        public void ForgeHall_EntryReachesTheDoor_KeeperSpotIsClear()
        {
            var hall = AssetDatabase.LoadAssetAtPath<WorldMap>(ForgeHallTools.HallPath);
            if (hall == null) Assert.Inconclusive($"{ForgeHallTools.HallPath} missing (run Tools > Pocket Isle > Set Up MR_Main (9b puzzles)).");
            Assert.IsTrue(hall.CanWalk(ForgeHallTools.EntryTile));
            foreach (var d in ForgeHallTools.DoorTiles)
            {
                Assert.IsTrue(hall.CanWalk(d), $"door tile {d} walkable (stepping on it leads out)");
                Assert.IsTrue(ExplorerPath.CanReach(hall, ForgeHallTools.EntryTile, d, 40));
            }
            var k = ForgeHallTools.KeeperSpot;
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    Assert.IsTrue(hall.CanWalk(new UnityEngine.Vector2Int(UnityEngine.Mathf.RoundToInt(k.x) + dx, UnityEngine.Mathf.RoundToInt(k.y) + dy)), "the Keeper's spot is open floor");
        }
    }
}
