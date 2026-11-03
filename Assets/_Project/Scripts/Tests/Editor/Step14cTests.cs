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
        public void QuickRoute_MillKitesTower_RidgeSkipped()
        {
            var solved = new HashSet<string>();
            Assert.AreEqual("mill", QuickJourneyDirector.NextLandmark(solved.Contains));
            solved.Add("gw.mill");
            Assert.AreEqual("kites", QuickJourneyDirector.NextLandmark(solved.Contains));
            solved.Add("gw.kites");
            Assert.AreEqual("tower", QuickJourneyDirector.NextLandmark(solved.Contains), "no ridge stop: its clouds are cleared already");
            Assert.AreEqual("gw.clouds", QuickJourneyDirector.RidgeId);
            Assert.IsFalse(System.Array.Exists(QuickJourneyDirector.Route, r => r.puzzle == QuickJourneyDirector.RidgeId));
            Assert.AreEqual(ShardHolder.WindId, QuickJourneyDirector.ShardId);
        }

        [Test]
        public void QuickEnd_ThreeRunes()
        {
            CollectionAssert.AreEqual(new[] { "Keep exploring", "See the ending", "Title" }, QuickJourneyDirector.EndRunes);
        }

        [Test]
        public void SeeTheEnding_SuspendsTheQuickSave_BeforeTheFinalePlays()
        {
            var go = new UnityEngine.GameObject("Save (test)");
            go.SetActive(false);
            var save = go.AddComponent<GameSave>();
            bool suspendedWhenPlayed = false;
            var r = QuickJourneyDirector.BeginEnding(save, () =>
            {
                suspendedWhenPlayed = save.Suspended;
                return "played";
            });
            Assert.AreEqual("played", r);
            Assert.IsTrue(suspendedWhenPlayed, "the finale's saves (shards placed, finale done) never reach the quick save");
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void QuickPresets_TowerDoor_GalewickBeatsDone()
        {
            var ids = System.Array.ConvertAll(GameState.QuickTowerSolved, p => p.id);
            CollectionAssert.IsSubsetOf(new[] { "gw.arrive", "gw.mill", "gw.kites", "gw.clouds", TowerDoor.TowerId }, ids);
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
