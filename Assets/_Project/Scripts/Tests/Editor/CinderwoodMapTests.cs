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
                    Assert.AreEqual(7, e.minLevel);
                    Assert.AreEqual(9, e.maxLevel);
                }
            Assert.IsTrue(glaze, "Glazegolem in the ruins table");
            Assert.AreEqual("forest", map.ZoneAt(CinderwoodTools.Tile(10, 16)));
            Assert.AreEqual(map.grassTable, map.GrassTableAt(CinderwoodTools.Tile(2, 2)), "outside the zones: the map's table");
        }
    }
}
