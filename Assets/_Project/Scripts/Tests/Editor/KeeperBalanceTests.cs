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
        public void Lv8PartyWithSteambara_WinsAboutTwoInThree()
        {
            var keeper = Keeper();
            var helper = KeeperSim.Load(KeeperSim.Helper);
            if (helper == null) Assert.Inconclusive("Steambara missing (run Step9fSetup.Steambara).");
            var row = KeeperSim.Run(keeper, KeeperKit.Level, 8, 200, 1, helper);
            Assert.AreEqual(3, row.Count, "three starters");
            Assert.That(row["Kilnpot"].WinRate, Is.InRange(0.5f, 0.85f), "Kilnpot");
            Assert.That(row["Pufflet"].WinRate, Is.InRange(0.5f, 0.85f), "Pufflet");
            Assert.That(row["Pebbloop"].WinRate, Is.GreaterThan(0.6f), "Pebbloop");
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
