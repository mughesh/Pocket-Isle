using System.IO;
using NUnit.Framework;

namespace Diorama.Tests
{
    /// <summary>Nov 13 (afternoon): the view lines up with a 2-wide road; the game menu can quit the app.</summary>
    public class Nov13bTests
    {
        static string Src(string file) => File.ReadAllText("Assets/_Project/Scripts/Runtime/" + file);

        [Test]
        public void View_HoldsStillWhileTheExplorerIsWithinHalfATile()
        {
            // Centred on the road (c14.5): either lane keeps it there.
            Assert.AreEqual(14.5f, WorldWindow.HoldAxis(14.5f, 14f, 0.5f, 0f), 1e-5f);
            Assert.AreEqual(14.5f, WorldWindow.HoldAxis(14.5f, 15f, 0.5f, 0f), 1e-5f);
            // Walking on: the view follows half a tile behind.
            Assert.AreEqual(15.5f, WorldWindow.HoldAxis(14.5f, 16f, 0.5f, 0f), 1e-5f);
            Assert.AreEqual(13.5f, WorldWindow.HoldAxis(14.5f, 13f, 0.5f, 0f), 1e-5f);
            // Odd axis: always on the explorer.
            Assert.AreEqual(9f, WorldWindow.HoldAxis(7f, 9f, 0f, 0f), 1e-5f);
        }

        [Test]
        public void GameMenu_SpeakerSlotIsARedQuitThatAsksFirst()
        {
            var src = Src("Title/GameMenu.cs");
            StringAssert.DoesNotContain("TabletKit.Speaker(", src);
            StringAssert.Contains("runes.AddIcon(\"Quit\", TabletKit.Power()", src);
            StringAssert.Contains("Application.Quit();", src);
            StringAssert.Contains("save.Save(\"menu: quit\")", src);
            Assert.IsNotNull(TabletKit.Power());
        }

        [Test]
        public void GazedProp_Swells()
        {
            StringAssert.Contains("island.SetPropSwell(", Src("Dialogue/FocusHighlight.cs"));
            StringAssert.Contains("c2w * b.local[i] * grow", Src("IslandBuilder.cs"));
        }

        [Test]
        public void HealBloom_PopsUpWhatItDid()
        {
            var src = Src("HealBloom.cs");
            StringAssert.Contains("Popup(HealedText,", src);
            StringAssert.Contains("Popup(HealthyText,", src);
            Assert.AreEqual("Islings' health restored!", HealBloom.HealedText);
        }
    }
}
