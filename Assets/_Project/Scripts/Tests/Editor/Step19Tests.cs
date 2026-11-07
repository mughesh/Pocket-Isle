using NUnit.Framework;

namespace Diorama.Tests
{
    /// <summary>Step 19: the credits as sky lanterns.</summary>
    public class Step19Tests
    {
        [Test]
        public void Lanterns_InOrder_BigPortraitLast_SameCreditsAsTheCard()
        {
            var l = CreditsText.Lanterns;
            Assert.AreEqual(5, l.Length);
            Assert.AreEqual("Islings, realms and Keepers", l[0].title);
            Assert.AreEqual("Music", l[1].title);
            StringAssert.StartsWith("Sounds", l[2].title);
            StringAssert.StartsWith("Font", l[3].title);
            Assert.AreEqual("Pocket Isle", l[4].title);
            Assert.AreEqual("Made by Mugesh", l[4].subtitle);
            for (int i = 0; i < l.Length; i++) Assert.AreEqual(i == l.Length - 1, l[i].big, $"lantern {i}");
            foreach (var x in l)
            {
                StringAssert.DoesNotContain("Unity", x.title + x.subtitle);
                StringAssert.DoesNotContain("Meta", x.title + x.subtitle);
            }
            // The free plan's required credit is on a lantern too (until step 15b).
            Assert.IsTrue(System.Array.Exists(l, x => x.title.Contains("elevenlabs.io")));
        }

        [Test]
        public void Lanterns_TwoReadingAtOnce_NeverShareASlot()
        {
            // A lantern reads for 3 s and a new one comes every 2 s: neighbours overlap in time, so their slots differ.
            for (int i = 0; i < 3; i++) Assert.AreNotEqual(FinaleDirector.LanternSlot(i), FinaleDirector.LanternSlot(i + 1));
        }

        [Test]
        public void Lanterns_About15To18Seconds()
        {
            float s = FinaleDirector.LanternSeconds(CreditsText.Lanterns.Length, 2f, 1.6f, 5f, 1.2f);
            Assert.That(s, Is.InRange(14f, 18f));
        }

        [Test]
        public void Lantern_IsAFewHundredTriangles()
        {
            int tris = SkyLantern.BodyTriangles;
            SkyLantern.DestroyShared();
            Assert.That(tris, Is.InRange(50, 400));
        }
    }
}
