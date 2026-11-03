using System.Collections.Generic;
using Diorama.EditorTools;
using NUnit.Framework;
using UnityEditor;

namespace Diorama.Tests
{
    /// <summary>Step 14c: Galewick first (guidance order, open gates, Keeper levels).</summary>
    public class Step14cTests
    {
        [Test]
        public void HubGuidance_WindThenFireThenWater_SkipsShutGates()
        {
            var home = new HashSet<string>();
            var shut = new HashSet<string>();
            string Next() => SpiritTrail.NextGate(home.Contains, g => !shut.Contains(g));
            Assert.AreEqual("wind", Next(), "Galewick first");
            home.Add(ShardHolder.WindId);
            Assert.AreEqual("fire", Next(), "then Cinderwood");
            home.Add(ShardHolder.FireId);
            Assert.AreEqual("water", Next(), "then Lilymere");
            home.Add(ShardHolder.WaterId);
            Assert.IsNull(Next());
            home.Clear();
            shut.Add("wind");
            Assert.AreEqual("fire", Next(), "a shut gate is skipped");
        }

        [Test]
        public void EveryRealmGate_OpenInBuilds()
        {
            foreach (var path in new[] { GalewickTools.ConfigPath, LilymereTools.ConfigPath })
            {
                var c = AssetDatabase.LoadAssetAtPath<GalewickConfig>(path);
                Assert.IsNotNull(c, path);
                Assert.IsTrue(c.open, path);
            }
        }

        [Test]
        public void KeeperLevels_ForTheGalewickFirstOrder()
        {
            Assert.AreEqual(8, ShardScaling.KeeperLevel(GaleowlKit.Level, 0, 2), "Galeowl at 0 shards");
            Assert.AreEqual(14, ShardScaling.KeeperLevel(KeeperKit.Level, 1, 2), "Hearthstag at 1 shard");
            Assert.AreEqual(13, ShardScaling.KeeperLevel(TidemotherKit.Level, 2, 2), "Tidemother at 2 shards");
        }
    }
}
