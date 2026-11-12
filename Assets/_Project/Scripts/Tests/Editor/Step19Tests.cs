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
            // Nov 12 evening: minimal (no music or font lanterns).
            Assert.AreEqual(3, l.Length);
            Assert.AreEqual("Islings, realms and Keepers", l[0].title);
            StringAssert.StartsWith("Sounds", l[1].title);
            Assert.AreEqual("Pocket Isle", l[2].title);
            Assert.AreEqual("Made by Mugesh", l[2].subtitle);
            Assert.IsFalse(System.Array.Exists(l, x => (x.title + x.subtitle).Contains("Music") || (x.title + x.subtitle).Contains("Font")));
            for (int i = 0; i < l.Length; i++) Assert.AreEqual(i == l.Length - 1, l[i].big, $"lantern {i}");
            foreach (var x in l)
            {
                StringAssert.DoesNotContain("Unity", x.title + x.subtitle);
                StringAssert.DoesNotContain("Meta", x.title + x.subtitle);
            }
            // Step 25: the paid sounds are in, so no lantern carries the free plan's line.
            Assert.IsFalse(System.Array.Exists(l, x => (x.title + x.subtitle).Contains("elevenlabs.io")));
            Assert.IsFalse(CreditsText.Block.Contains("free plan"));
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
            // Nov 12 evening: three lanterns, about 12 s.
            float s = FinaleDirector.LanternSeconds(CreditsText.Lanterns.Length, 2f, 1.6f, 5f, 1.2f);
            Assert.That(s, Is.InRange(10f, 18f));
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
