using NUnit.Framework;
using Diorama.EditorTools;

namespace Diorama.Tests
{
    /// <summary>
    /// Step 9f curve targets (<see cref="KeeperSim"/>): the shortest trail reaches the temple at about Lv 8, and a Lv 8
    /// party of 3 (the starter + Steambara + a random fire common, Water leads, the simple best-move AI) beats Hearthstag
    /// about 60-75% of the time with Kilnpot or Pufflet (bands widened for the smaller samples). Pebbloop's party has two
    /// Water Islings and wins more; it only has to stay above the band.
    /// </summary>
    public class KeeperBalanceTests
    {
        static IslingSpecies Keeper()
        {
            var keeper = KeeperSim.Load("Hearthstag");
            if (keeper == null) Assert.Inconclusive("Hearthstag missing (run Set Up MR_Main (9c keeper)).");
            return keeper;
        }

        [Test]
        public void OneShardLv12PartyWithSteambara_Wins()
        {
            // Step 14c: Cinderwood comes second (one shard held): Hearthstag Lv 12 + 2. A Lv 12 party (the usual arrival)
            // wins most fights, a Lv 11 one mostly only with Pebbloop (water breaks the crown); RealmOrderSim has the full trail.
            var keeper = Keeper();
            var helper = KeeperSim.Load(KeeperSim.Helper);
            if (helper == null) Assert.Inconclusive("Steambara missing (run Step9fSetup.Steambara).");
            int level = ShardScaling.KeeperLevel(KeeperKit.Level, 1, 2);
            var row = KeeperSim.Run(keeper, level, 12, 200, 1, helper);
            Assert.AreEqual(3, row.Count, "three starters");
            foreach (var kv in row) Assert.That(kv.Value.WinRate, Is.InRange(0.65f, 1f), kv.Key);
            Assert.That(KeeperSim.Run(keeper, level, 11, 200, 1, helper)["Pebbloop"].WinRate, Is.GreaterThan(0.6f), "Pebbloop at Lv 11");
        }

        [Test]
        public void ShortestTrail_ArrivesAboutLv8()
        {
            var t = KeeperSim.Trail(400, 1f);
            if (t.grassHub + t.grassCinder == 0) Assert.Inconclusive("Maps or progression missing.");
            Assert.That(t.level, Is.InRange(7.7f, 9.2f));
        }

        [Test]
        public void KeeperHp_AboutTwoAndAHalfTimesANormalOne()
        {
            var k = new IslingInstance(Keeper(), KeeperKit.Level);
            Assert.That(k.MaxHp / (float)BattleMath.MaxHp(40, KeeperKit.Level), Is.InRange(2.3f, 3f));
        }
    }
}
