using NUnit.Framework;

namespace Diorama.Tests
{
    public class Step24Tests
    {
        [Test]
        public void KeeperLabel_HasNoLevel_OthersDo()
        {
            StringAssert.DoesNotContain("Lv", BattleHud.LabelText("Galeowl", 4, false));
            StringAssert.Contains("Galeowl", BattleHud.LabelText("Galeowl", 4, false));
            StringAssert.Contains("Lv 12", BattleHud.LabelText("Kilnpot", 12, true));
        }

        [Test]
        public void TempleDoor_AsksAgainAtOnce_NeverASilentWall()
        {
            Assert.IsNull(TempleRules.NotAsking(false, 10f, -99f), "first bump asks");
            Assert.IsNotNull(TempleRules.NotAsking(false, 10.5f, 10f), "a double bump is only debounced");
            Assert.IsNull(TempleRules.NotAsking(false, 11.1f, 10f), "a bump a second later asks again");
            Assert.IsNotNull(TempleRules.NotAsking(true, 50f, 10f), "the board is already up");
        }
    }
}
