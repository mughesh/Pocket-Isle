using NUnit.Framework;
using Diorama.EditorTools;

namespace Diorama.Tests
{
    /// <summary>
    /// Step 9e: Hearthstag is beatable at a normal pace. A Lv 9 party of 3 (each starter + two random fire commons, the
    /// simple best-move AI in <see cref="KeeperSim"/>) wins about 60-75% (band widened for the smaller sample).
    /// </summary>
    public class KeeperBalanceTests
    {
        [Test]
        public void Lv9PartyOfThree_WinsAboutTwoInThree()
        {
            var keeper = KeeperSim.Load("Hearthstag");
            if (keeper == null) Assert.Inconclusive("Hearthstag missing (run Set Up MR_Main (9c keeper)).");
            int fights = 0, wins = 0;
            foreach (var kv in KeeperSim.Run(keeper, KeeperKit.Level, 9, 200))
            {
                fights += kv.Value.fights;
                wins += kv.Value.wins;
            }
            Assert.AreEqual(600, fights, "three starters x 200");
            Assert.That(wins / (float)fights, Is.InRange(0.55f, 0.8f));
        }

        [Test]
        public void KeeperHp_AboutTwiceANormalOne()
        {
            var keeper = KeeperSim.Load("Hearthstag");
            if (keeper == null) Assert.Inconclusive("Hearthstag missing.");
            var k = new IslingInstance(keeper, KeeperKit.Level);
            Assert.That(k.MaxHp / (float)BattleMath.MaxHp(40, KeeperKit.Level), Is.InRange(2f, 2.4f));
        }
    }
}
