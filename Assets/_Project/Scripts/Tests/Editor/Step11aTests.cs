using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Diorama.EditorTools;

namespace Diorama.Tests
{
    /// <summary>
    /// Step 11a: the Lilymere map's layout checks, its zones and story props, the lagoon's dry ring rising to water (runtime
    /// only), and the well gate both ways (sealed in builds). Needs Set Up MR_Main (11a Lilymere) to have written the map.
    /// </summary>
    public class Step11aTests
    {
        static WorldMap Load()
        {
            var map = AssetDatabase.LoadAssetAtPath<WorldMap>(LilymereTools.MapPath);
            if (map == null) Assert.Inconclusive($"{LilymereTools.MapPath} missing (run Tools > Pocket Isle > Set Up MR_Main (11a Lilymere)).");
            return map;
        }

        static string PropName(WorldMap map, Vector2Int xy)
        {
            var t = LilymereTools.Tile(xy);
            return map.TryGetProp(map.PropAt(t.y, t.x), out var e) && e.recipe != null ? e.recipe.name : null;
        }

        [Test]
        public void Layout_PassesTheChecks()
        {
            var map = Load();
            map.ClearRuntimeChanges();
            var problems = LilymereTools.Check(map, null);
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void Zones_Landmarks_AndStoryProps()
        {
            var map = Load();
            Assert.AreEqual("lilymere", map.realm);
            Assert.AreEqual("realm.lilymere.first", map.arrivalLine);
            Assert.AreEqual("pond", map.ZoneAt(LilymereTools.Tile(50, 46)));
            Assert.AreNotEqual(map.grassTable, map.GrassTableAt(LilymereTools.Tile(50, 46)));
            Assert.IsNotNull(map.waterTable, "the lagoon's water has its own table");
            foreach (var name in new[] { "start", "village", "shrine", "channel", "garden", "basin", "temple", "marsh", "pond" })
                Assert.IsTrue(map.TryGetLandmark(name, out var t) && map.CanWalk(GameState.NearestWalkable(map, t)), $"landmark {name}");
            Assert.AreEqual("LakeTemple", PropName(map, LilymereTools.TempleXY));
            Assert.AreEqual("StoneBasin", PropName(map, LilymereTools.BasinXY));
            Assert.AreEqual("CliffBowl", PropName(map, LilymereTools.WaterfallXY)); // step 12c: the cliff with the bowl
            Assert.IsTrue(map.CanWalk(LilymereTools.TempleFront), "the temple door's front is walkable");
            Assert.IsTrue(map.CanWalk(LilymereTools.BasinFront), "the basin's front is walkable");
            for (int y = LilymereTools.ChannelY0; y <= LilymereTools.ChannelY1; y++)
                Assert.AreEqual(IslandMap.Water, map.At(LilymereTools.Tile(LilymereTools.CrossingXY.x - 2, y)), "the channel is water beside the crossing");
        }

        [Test]
        public void Lagoon_DryRingRisesToWater_RuntimeOnly_NotWalkable()
        {
            var map = Load();
            map.ClearRuntimeChanges();
            Vector2Int dry = default;
            bool found = false;
            for (int r = 0; r < map.Rows && !found; r++)
                for (int c = 0; c < map.Columns && !found; c++)
                    if (map.Raw(r, c) == IslandMap.DryBed)
                    {
                        dry = new Vector2Int(c, r);
                        found = true;
                    }
            Assert.IsTrue(found, "the map has dry bed tiles");
            Assert.AreEqual(IslandMap.DryBed, map.At(dry));
            Assert.IsFalse(map.CanWalk(dry), "dry bed isn't walkable");
            int version = map.version;
            map.SetRisen(true);
            try
            {
                Assert.AreEqual(IslandMap.Water, map.At(dry), "risen: water");
                Assert.AreEqual(IslandMap.DryBed, map.Raw(dry.y, dry.x), "the layout itself is untouched");
                Assert.Greater(map.version, version, "the island rebuilds");
                Assert.IsFalse(map.CanWalk(dry));
            }
            finally
            {
                map.ClearRuntimeChanges();
            }
            Assert.IsFalse(map.Risen);
            Assert.AreEqual(IslandMap.DryBed, map.At(dry));
        }

        [Test]
        public void WellGate_LeadsToLilymereAndBack_SealedInBuilds()
        {
            var map = Load();
            var hub = AssetDatabase.LoadAssetAtPath<WorldMap>(StillIsleTools.MapPath);
            var water = hub.gates.Find(g => g.id == "water");
            Assert.AreEqual(map, water.to);
            Assert.IsTrue(map.CanWalk(water.arrive), "arrival in Lilymere is walkable");
            var home = map.gates.Find(g => g.id == "home");
            Assert.AreEqual(hub, home.to);
            Assert.IsTrue(home.arrive == water.Front(0) || home.arrive == water.Front(1), "you come back in front of the well gate");
            Assert.IsTrue(hub.CanWalk(home.arrive));
            var config = AssetDatabase.LoadAssetAtPath<LilymereConfig>(LilymereTools.ConfigPath);
            Assert.IsNotNull(config);
            Assert.IsFalse(config.open, "builds keep the well gate sealed until Lilymere is finished");
            Assert.IsTrue(config.openInEditor);
            var theme = map.theme;
            Assert.IsNotNull(theme);
            Assert.Greater(theme.waterRipples, 0f, "restored water ripples");
            Assert.AreEqual(0f, theme.startRestored);
            Assert.AreNotEqual(theme.unrestored.path, theme.unrestored.dryBed, "the path and the dry bed read apart");
        }
    }
}
