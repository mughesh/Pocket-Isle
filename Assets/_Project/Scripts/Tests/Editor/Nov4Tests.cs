using NUnit.Framework;

namespace Diorama.Tests
{
    /// <summary>Mugesh's Nov 4 notes: the Isling minimum at the temple doors, the island's spawn distance.</summary>
    public class Nov4Tests
    {
        [Test]
        public void Temple_Galewick4_Others6_QuickNone()
        {
            Assert.AreEqual(4, TempleRules.Needed(true, false));
            Assert.AreEqual(6, TempleRules.Needed(false, false));
            Assert.AreEqual(0, TempleRules.Needed(true, true));
            Assert.AreEqual(0, TempleRules.Needed(false, true));
        }

        [Test]
        public void Temple_EnoughIslings_LetsIn()
        {
            Assert.IsFalse(TempleRules.CanEnter(3, 4));
            Assert.IsTrue(TempleRules.CanEnter(4, 4));
            Assert.IsFalse(TempleRules.CanEnter(5, 6));
            Assert.IsTrue(TempleRules.CanEnter(6, 6));
            Assert.IsTrue(TempleRules.CanEnter(1, 0));
        }

        [Test]
        public void Temple_LinePerMinimum()
        {
            // Step 14h: one warning line at every door ({keeper}, {n} filled in).
            Assert.AreEqual(TempleRules.WarnLine, TempleRules.Line(4));
            Assert.AreEqual(TempleRules.WarnLine, TempleRules.Line(6));
        }

        [Test]
        public void Temple_CarriedCountsPods()
        {
            var go = new UnityEngine.GameObject("party");
            try
            {
                var party = go.AddComponent<Party>();
                Assert.AreEqual(0, TempleRules.Carried(party));
                var species = UnityEngine.ScriptableObject.CreateInstance<IslingSpecies>();
                party.members.Add(new IslingInstance(species, 3));
                party.members.Add(new IslingInstance(species, 3));
                party.storage.Add(new IslingInstance(species, 3));
                Assert.AreEqual(2, TempleRules.Carried(party), "storage doesn't count");
                UnityEngine.Object.DestroyImmediate(species);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Island_Spawns55cmAhead()
        {
            Assert.AreEqual(0.55f, IslandPlacement.Rule.Default.distance, 1e-4f);
            Assert.AreEqual(0.55f, EditorTools.Nov4Setup.Distance, 1e-4f);
        }
    }
}
