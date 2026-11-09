using NUnit.Framework;

namespace Diorama.Tests
{
    public class Step24Tests
    {
        [Test]
        public void Jingle_SwapsOnlyWhenSwitchedOn_AndTheTwinExists()
        {
            System.Func<string, bool> has = id => id == "jingle.catch.acoustic";
            Assert.AreEqual("jingle.catch", Diorama.Audio.AudioAB.JingleFor("jingle.catch", false, has));
            Assert.AreEqual("jingle.catch.acoustic", Diorama.Audio.AudioAB.JingleFor("jingle.catch", true, has));
            Assert.AreEqual("jingle.shard", Diorama.Audio.AudioAB.JingleFor("jingle.shard", true, has));
            Assert.AreEqual("hit.normal", Diorama.Audio.AudioAB.JingleFor("hit.normal", true, id => true));
            Assert.AreEqual("jingle.catch.acoustic", Diorama.Audio.AudioAB.JingleFor("jingle.catch.acoustic", true, id => true));
        }

        [Test]
        public void SpeakerKey_KeepersAndHeartOnly_SpiritKeepsItsBlip()
        {
            Assert.AreEqual("galeowl", DialogueBubble.SpeakerKey(new DialogueLine { speaker = DialogueSpeaker.Keeper, speakerName = " Galeowl " }));
            Assert.AreEqual("heart", DialogueBubble.SpeakerKey(new DialogueLine { speaker = DialogueSpeaker.Heart }));
            Assert.IsNull(DialogueBubble.SpeakerKey(new DialogueLine { speaker = DialogueSpeaker.Keeper }));
            Assert.IsNull(DialogueBubble.SpeakerKey(new DialogueLine { speaker = DialogueSpeaker.Spirit, speakerName = "Spirit" }));
        }

        [Test]
        public void KeeperLabel_HasNoLevel_OthersDo()
        {
            StringAssert.DoesNotContain("Lv", BattleHud.LabelText("Galeowl", 4, false));
            StringAssert.Contains("Galeowl", BattleHud.LabelText("Galeowl", 4, false));
            StringAssert.Contains("Lv 12", BattleHud.LabelText("Kilnpot", 12, true));
        }

        static WaterScoop Held()
        {
            var s = new WaterScoop();
            var dip = new WaterScoop.Hand { tracked = true, up = 1f, overWater = true, aboveWater = -0.02f };
            s.Feed(0.02f, new[] { dip, default });
            var lift = new WaterScoop.Hand { tracked = true, up = 1f, overWater = true, aboveWater = 0.05f };
            s.Feed(0.02f, new[] { lift, default });
            Assert.AreEqual(ScoopState.Held, s.State);
            return s;
        }

        static WaterScoop.Hand Tilted(float degrees, bool basin = true) =>
            new WaterScoop.Hand { tracked = true, up = UnityEngine.Mathf.Cos(degrees * UnityEngine.Mathf.Deg2Rad), overBasin = basin };

        [Test]
        public void Pour_FlowGrowsWithTheTilt()
        {
            var s = new WaterScoop();
            Assert.AreEqual(0f, s.FlowAt(UnityEngine.Mathf.Cos(10f * UnityEngine.Mathf.Deg2Rad)), 1e-4f, "near level: nothing");
            float dribble = s.FlowAt(UnityEngine.Mathf.Cos(30f * UnityEngine.Mathf.Deg2Rad));
            Assert.Greater(dribble, 0f);
            Assert.Less(dribble, 0.5f, "a dribble");
            Assert.AreEqual(1f, s.FlowAt(UnityEngine.Mathf.Cos(62f * UnityEngine.Mathf.Deg2Rad)), 1e-4f, "full by 60");
        }

        [Test]
        public void Pour_CountsWhenEnoughLanded_NotOnAnAngle()
        {
            var s = Held();
            s.Feed(0.1f, new[] { Tilted(65f), default });
            Assert.AreEqual(ScoopState.Held, s.State, "past the full angle, nothing has landed yet");
            Assert.AreEqual(0, s.Pours);
            Assert.Less(s.Water, 1f, "the hand drains");
            for (int i = 0; i < 200 && s.State == ScoopState.Held; i++) s.Feed(0.02f, new[] { Tilted(65f), default });
            Assert.AreEqual(ScoopState.Pouring, s.State);
            Assert.AreEqual(1, s.Pours);
            Assert.GreaterOrEqual(s.Landed, s.landedNeeded);
        }

        [Test]
        public void Pour_TiltingBackStopsIt_AndAwayFromTheBowlNothingDrains()
        {
            var s = Held();
            s.Feed(0.2f, new[] { Tilted(50f), default });
            float left = s.Water;
            s.Feed(0.2f, new[] { Tilted(5f), default });
            Assert.AreEqual(0f, s.Flow, 1e-4f, "tilted back: stops");
            Assert.AreEqual(left, s.Water, 1e-4f);

            var carry = Held();
            carry.Feed(0.5f, new[] { Tilted(60f, false), default });
            Assert.AreEqual(1f, carry.Water, 1e-4f, "a carry doesn't leak");
            Assert.AreEqual(ScoopState.Held, carry.State);
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
