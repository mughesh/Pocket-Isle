using Diorama.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>Step 14e: Mugesh's Nov 3 late notes (Cinderwood's vine, the Quick Journey's end, the Spirit's voice).</summary>
    public class Step14eTests
    {
        [Test]
        public void Cinderwood_VineIsTheOnlyWayOn_TheEmberVentIsInThePlaza()
        {
            var map = AssetDatabase.LoadAssetAtPath<WorldMap>(CinderwoodTools.MapPath);
            Assert.IsNotNull(map);
            Assert.AreEqual("cw.arrive", map.arrivalLine);
            foreach (var t in CinderwoodTools.VineTiles)
            {
                Assert.IsTrue(map.TryGetProp(map.PropAt(t.y, t.x), out var prop), $"vine at c{t.x} r{t.y}");
                Assert.AreEqual(PropKit.VineName, prop.recipe.name);
                Assert.IsTrue(prop.recipe.blocksWalking);
            }
            var vent = CinderwoodTools.Tile(CinderwoodTools.ArriveVentXY);
            Assert.IsTrue(map.TryGetProp(map.PropAt(vent.y, vent.x), out var v));
            Assert.AreEqual("EmberVent", v.recipe.name);
            Assert.IsTrue(map.TryGetLandmark("vine", out var mark));
            var beyond = CinderwoodTools.Tile(11, 10);
            try
            {
                Assert.IsTrue(ExplorerPath.CanReach(map, CinderwoodTools.Arrive, mark, 400), "up to the vine");
                Assert.IsTrue(ExplorerPath.CanReach(map, CinderwoodTools.Arrive, vent + Vector2Int.up, 400), "to the ember vent");
                Assert.IsFalse(ExplorerPath.CanReach(map, CinderwoodTools.Arrive, beyond, 400), "the vine blocks the only way on");
                foreach (var t in CinderwoodTools.VineTiles) map.SetPropOpen(t, true);
                Assert.IsTrue(ExplorerPath.CanReach(map, CinderwoodTools.Arrive, beyond, 400), "burnt: the trail walks on");
            }
            finally
            {
                map.ClearRuntimeChanges();
            }
        }

        [Test]
        public void Cinderwood_VineBaked_UnderBudget()
        {
            var r = PropKit.Load(PropKit.VineName);
            if (r == null || r.bakedMesh == null) Assert.Inconclusive("ThornVine not baked (Set Up MR_Main (14e Nov 3 late notes)).");
            int tris = r.bakedMesh.triangles.Length / 3;
            Assert.Greater(tris, 0);
            Assert.LessOrEqual(tris, r.triangleBudget);
        }

        [Test]
        public void QuickEnd_DreamAhead_TheOtherTwoShards()
        {
            CollectionAssert.AreEquivalent(new[] { ShardHolder.FireId, ShardHolder.WaterId }, QuickJourneyDirector.DreamShards);
            CollectionAssert.DoesNotContain(QuickJourneyDirector.DreamShards, QuickJourneyDirector.ShardId);
        }
    }
}
