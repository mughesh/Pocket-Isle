using System.Collections.Generic;
using Diorama.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

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
        public void ArrivalLanterns_OneTouchRacesTheLine()
        {
            var c = new LanternChain(4) { raceOnTouch = true, raceStep = 0.15f };
            Assert.IsTrue(c.Touch(1));
            Assert.IsTrue(c.Racing);
            var order = new List<int>();
            for (int i = 0; i < 40 && !c.AllLit; i++)
            {
                int k = c.Tick(0.05f);
                if (k >= 0) order.Add(k);
            }
            CollectionAssert.AreEqual(new[] { 2, 3, 0 }, order, "ahead first, then behind");
            Assert.AreEqual(1, c.Origin);
            // The hollow's own rule is unchanged: one touch lights one.
            var h = new LanternChain(4);
            h.Touch(1);
            Assert.IsFalse(h.Racing);
        }

        [Test]
        public void Cinderwood_FourLanternsPastTheArrival_TrailStillWalks()
        {
            var map = AssetDatabase.LoadAssetAtPath<WorldMap>(CinderwoodTools.MapPath);
            Assert.IsNotNull(map);
            Assert.AreEqual("cw.arrive", map.arrivalLine);
            Assert.AreEqual(4, CinderwoodTools.ArriveLanterns.Length);
            foreach (var t in CinderwoodTools.ArriveLanterns)
            {
                Assert.IsTrue(map.TryGetProp(map.PropAt(t.y, t.x), out var prop), $"lantern at c{t.x} r{t.y}");
                Assert.AreEqual("StoneLantern", prop.recipe.name);
                int d = Mathf.Abs(t.x - CinderwoodTools.Arrive.x) + Mathf.Abs(t.y - CinderwoodTools.Arrive.y);
                Assert.That(d, Is.InRange(2, 8), "just past the arrival");
            }
            Assert.IsTrue(map.TryGetLandmark("lanterns", out var mark));
            Assert.IsTrue(ExplorerPath.CanReach(map, CinderwoodTools.Arrive, mark, 400), "no blocking: the trail walks either way");
        }

        [Test]
        public void NameCard_FirstVisitLong_LaterShort_RestoredLineOnce()
        {
            int f = RealmNameCard.Plan(0, false, out bool line, out bool restored);
            Assert.IsTrue(line, "first visit: name + line");
            Assert.IsFalse(restored);
            f = RealmNameCard.Plan(f, false, out line, out _);
            Assert.IsFalse(line, "later: the name only (short)");
            f = RealmNameCard.Plan(f, true, out line, out restored);
            Assert.IsTrue(line && restored, "first visit after restoring: the restored line");
            RealmNameCard.Plan(f, true, out line, out _);
            Assert.IsFalse(line, "only once");
        }

        [Test]
        public void RealmCards_Baked_UnderNineThousandTriangles()
        {
            foreach (var style in PropKit.RealmCards)
            {
                var r = PropKit.Load(style.asset);
                if (r == null || r.bakedMesh == null) Assert.Inconclusive($"{style.asset} not baked (Set Up MR_Main (14c Galewick first)).");
                Assert.LessOrEqual(r.bakedMesh.triangles.Length / 3, 9000, style.asset);
                Assert.Greater(r.bakedMesh.bounds.size.x, r.bakedMesh.bounds.size.y * 2f, "a wide board");
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
