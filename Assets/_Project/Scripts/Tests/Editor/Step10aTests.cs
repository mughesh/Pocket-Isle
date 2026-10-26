using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Diorama.EditorTools;

namespace Diorama.Tests
{
    /// <summary>
    /// Step 10a: the Galewick map's layout checks, its zones and tables, the wind gate both ways (sealed in builds by
    /// default), and the shard level scaling. Needs Set Up MR_Main (10a Galewick) to have written the map.
    /// </summary>
    public class Step10aTests
    {
        static WorldMap Load()
        {
            var map = AssetDatabase.LoadAssetAtPath<WorldMap>(GalewickTools.MapPath);
            if (map == null) Assert.Inconclusive($"{GalewickTools.MapPath} missing (run Tools > Pocket Isle > Set Up MR_Main (10a Galewick)).");
            return map;
        }

        [Test]
        public void Layout_PassesTheChecks()
        {
            var problems = GalewickTools.Check(Load(), null);
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void Zones_HaveTheirOwnTables_RealmIsGalewick()
        {
            var map = Load();
            Assert.AreEqual("galewick", map.realm);
            Assert.AreEqual("realm.galewick.first", map.arrivalLine);
            Assert.AreEqual("kites", map.ZoneAt(GalewickTools.Tile(GalewickTools.KiteXY[0])));
            Assert.AreEqual("fire", map.ZoneAt(GalewickTools.Tile(48, 15)));
            Assert.AreNotEqual(map.grassTable, map.GrassTableAt(GalewickTools.Tile(48, 15)));
            foreach (var name in new[] { "start", "plaza", "mill", "kites", "ridge", "tower", "shrine", "rare", "fire" })
                Assert.IsTrue(map.TryGetLandmark(name, out var t) && map.CanWalk(GameState.NearestWalkable(map, t)), $"landmark {name}");
        }

        static string PropName(WorldMap map, Vector2Int xy)
        {
            var t = GalewickTools.Tile(xy);
            return map.TryGetProp(map.PropAt(t.y, t.x), out var e) && e.recipe != null ? e.recipe.name : null;
        }

        [Test]
        public void StoryProps_MillTowerKitesAndShrineSitWhereThePuzzlesExpectThem()
        {
            var map = Load();
            Assert.AreEqual("Windmill", PropName(map, GalewickTools.MillXY));
            Assert.AreEqual("WindTower", PropName(map, GalewickTools.TowerXY));
            for (int i = 0; i < GalewickTools.KiteXY.Length; i++)
                Assert.AreEqual(PropKit.KiteNames[i], PropName(map, GalewickTools.KiteXY[i]), $"kite {i}");
            Assert.IsTrue(map.CanWalk(GalewickTools.TowerFront), "the tower door's front is walkable");
            foreach (var c in GalewickTools.CloudXY)
                Assert.AreEqual(IslandMap.Path, map.At(GalewickTools.Tile(c)), "the cloud bank sits on the ridge path");
        }

        [Test]
        public void WindGate_LeadsToGalewickAndBack_SealedInBuildsByDefault()
        {
            var map = Load();
            var hub = AssetDatabase.LoadAssetAtPath<WorldMap>(StillIsleTools.MapPath);
            var wind = hub.gates.Find(g => g.id == "wind");
            Assert.AreEqual(map, wind.to);
            Assert.IsTrue(map.CanWalk(wind.arrive), "arrival in Galewick is walkable");
            var home = map.gates.Find(g => g.id == "home");
            Assert.AreEqual(hub, home.to);
            Assert.IsTrue(home.arrive == wind.Front(0) || home.arrive == wind.Front(1), "you come back in front of the wind gate");
            Assert.IsTrue(hub.CanWalk(home.arrive));
            var config = AssetDatabase.LoadAssetAtPath<GalewickConfig>(GalewickTools.ConfigPath);
            Assert.IsNotNull(config);
            Assert.IsFalse(config.open, "builds keep the wind gate sealed until step 10d passes (Mugesh decides)");
            Assert.IsTrue(config.openInEditor);
            var theme = map.theme;
            Assert.IsNotNull(theme);
            Assert.Greater(theme.stillWhenUnrestored, 0.5f, "unrestored Galewick is still");
            Assert.Greater(theme.windSpin, 0f, "restored sails turn");
            Assert.AreEqual(0f, theme.startRestored);
        }

        [Test]
        public void ShardScaling_AddsWildAndKeeperLevelsPerShard_NotInTheHub()
        {
            Assert.AreEqual(7, ShardScaling.WildLevel(7, "cinderwood", 0, 3));
            Assert.AreEqual(10, ShardScaling.WildLevel(7, "galewick", 1, 3));
            Assert.AreEqual(13, ShardScaling.WildLevel(7, "galewick", 2, 3));
            Assert.AreEqual(7, ShardScaling.WildLevel(7, "stillisle", 2, 3), "the hub never scales");
            Assert.AreEqual(12, ShardScaling.KeeperLevel(10, 1, 2));
            Assert.AreEqual(30, ShardScaling.WildLevel(29, "galewick", 3, 3), "capped at 30");
        }

        [Test]
        public void ShardScaling_CountsHeldAndPlacedShards_ExceptTheKeepersOwn()
        {
            PuzzleState.Restore(new System.Collections.Generic.List<SavedPuzzle>());
            try
            {
                Assert.AreEqual(0, ShardScaling.Held());
                PuzzleState.SetStage("shard.fire", 1);
                Assert.AreEqual(1, ShardScaling.Held());
                Assert.AreEqual(0, ShardScaling.Held("shard.fire"), "Hearthstag doesn't count its own shard");
                PuzzleState.Solve("shard.fire", 1);
                PuzzleState.SetStage("shard.wind", 1);
                Assert.AreEqual(2, ShardScaling.Held());
                Assert.AreEqual(1, ShardScaling.Held("shard.wind"));
            }
            finally
            {
                PuzzleState.Restore(new System.Collections.Generic.List<SavedPuzzle>());
            }
        }
    }
}
