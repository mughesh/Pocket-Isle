using NUnit.Framework;

namespace Diorama.Tests
{
    /// <summary>Step 17 (Nov 7 video review): catch aim assist, battle-only lines and the other pure rules.</summary>
    public class Step17Tests
    {
        // ---- A1: aim assist ----

        [Test]
        public void Catch_WideAimCounts_SidewaysBackwardDropMiss()
        {
            const float min = 0.35f, angle = 70f, fwd = 0.15f;
            Assert.IsTrue(EncounterDirector.CatchAimed(0.8f, 55f, 0.45f, min, angle, fwd), "55 deg off with speed: aimed");
            Assert.IsTrue(EncounterDirector.CatchAimed(0.5f, 0f, 0.5f, min, angle, fwd));
            Assert.IsFalse(EncounterDirector.CatchAimed(0.9f, 110f, -0.3f, min, angle, fwd), "sideways/backward");
            Assert.IsFalse(EncounterDirector.CatchAimed(0.2f, 5f, 0.2f, min, angle, fwd), "a drop");
            Assert.IsFalse(EncounterDirector.CatchAimed(0.9f, 20f, 0.05f, min, angle, fwd), "straight down: no forward speed");
        }

        // ---- A4: battle-only lines ----

        [Test]
        public void BattleLines_FlowAndBattleContext_Only()
        {
            Assert.IsTrue(Dialogue.IsBattleLine("capture.first", null));
            Assert.IsTrue(Dialogue.IsBattleLine("battle.types", null));
            Assert.IsTrue(Dialogue.IsBattleLine("lm.meet.sailfold", null));
            Assert.IsFalse(Dialogue.IsBattleLine("spirit.poke.idle", new DialogueTrigger { id = "spirit.poke.idle" }));
            Assert.IsFalse(Dialogue.IsBattleLine("lesson.walk", new DialogueTrigger { id = "lesson.walk", context = "outside & !battle" }));
            Assert.IsTrue(Dialogue.IsBattleLine("lesson.send.again", new DialogueTrigger { id = "lesson.send.again", validWhile = "battle", context = "battle" }));
        }
    }
}
