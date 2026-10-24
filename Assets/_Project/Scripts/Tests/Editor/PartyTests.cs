using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Diorama.Tests
{
    public class PartyTests
    {
        readonly List<Object> _made = new List<Object>();
        Party _party;

        [SetUp]
        public void SetUp()
        {
            var go = new GameObject("Party Test");
            _made.Add(go);
            _party = go.AddComponent<Party>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _made) Object.DestroyImmediate(o);
            _made.Clear();
        }

        IslingInstance C(string name)
        {
            var s = ScriptableObject.CreateInstance<IslingSpecies>();
            _made.Add(s);
            s.name = s.displayName = name;
            return new IslingInstance(s, 5);
        }

        [Test]
        public void Swap_ExchangesAPodAndAStorageCreature()
        {
            var a = C("A");
            var b = C("B");
            var s = C("S");
            _party.members.AddRange(new[] { a, b });
            _party.storage.Add(s);
            Assert.IsTrue(_party.SwapWithStorage(0, 1));
            Assert.AreEqual(s, _party.members[1]);
            Assert.AreEqual(b, _party.storage[0]);
            Assert.AreEqual(2, _party.members.Count);
        }

        [Test]
        public void Swap_IntoTheNextEmptySocket_Adds()
        {
            _party.members.Add(C("A"));
            var s = C("S");
            _party.storage.Add(s);
            Assert.IsTrue(_party.SwapWithStorage(0, 1));
            Assert.AreEqual(2, _party.members.Count);
            Assert.AreEqual(0, _party.storage.Count);
            Assert.IsFalse(_party.SwapWithStorage(0, 1), "nothing left in storage");
        }

        [Test]
        public void Swap_RejectsGapsAndBadIndexes()
        {
            _party.members.Add(C("A"));
            _party.storage.Add(C("S"));
            Assert.IsFalse(_party.SwapWithStorage(0, 3), "no gap past the next empty socket");
            Assert.IsFalse(_party.SwapWithStorage(2, 0));
            Assert.IsFalse(_party.SwapWithStorage(0, Party.Size));
        }

        [Test]
        public void Lodge_DepositGoesToTheFront_BringForwardWakesASleeper()
        {
            var a = C("A");
            var b = C("B");
            var s1 = C("S1");
            var s2 = C("S2");
            _party.members.AddRange(new[] { a, b });
            _party.storage.AddRange(new[] { s1, s2 });
            Assert.IsTrue(_party.Deposit(1));
            CollectionAssert.AreEqual(new[] { b, s1, s2 }, _party.storage, "a dropped pod joins the room (front of storage)");
            Assert.IsTrue(_party.BringForward(2));
            CollectionAssert.AreEqual(new[] { s2, b, s1 }, _party.storage);
            Assert.IsFalse(_party.BringForward(0), "already in front");
            Assert.IsFalse(_party.BringForward(5));
        }

        [Test]
        public void Lodge_SwapStorage_TradesASleeperAndARoomIsling()
        {
            var r1 = C("R1");
            var r2 = C("R2");
            var s1 = C("S1");
            _party.storage.AddRange(new[] { r1, r2, s1 });
            Assert.IsTrue(_party.SwapStorage(2, 0), "the sleeper wakes into the oldest wanderer's place");
            CollectionAssert.AreEqual(new[] { s1, r2, r1 }, _party.storage);
            Assert.IsFalse(_party.SwapStorage(1, 1));
            Assert.IsFalse(_party.SwapStorage(0, 3));
        }

        [Test]
        public void Encounters_UncaughtSpeciesCountTwice()
        {
            var caught = ScriptableObject.CreateInstance<IslingSpecies>();
            var fresh = ScriptableObject.CreateInstance<IslingSpecies>();
            var table = ScriptableObject.CreateInstance<EncounterTable>();
            _made.Add(caught);
            _made.Add(fresh);
            _made.Add(table);
            table.entries = new[]
            {
                new EncounterTable.Entry { species = caught, minLevel = 2, maxLevel = 2, weight = 1f },
                new EncounterTable.Entry { species = fresh, minLevel = 2, maxLevel = 2, weight = 1f },
            };
            var rnd = new SeededRandom(7);
            int freshCount = 0, rolls = 3000;
            for (int i = 0; i < rolls; i++)
            {
                Assert.IsTrue(table.Roll(rnd, out var c, out _, s => s == caught, 2f));
                if (c.species == fresh) freshCount++;
            }
            Assert.AreEqual(2f / 3f, freshCount / (float)rolls, 0.04f, "uncaught: 2 of 3 rolls");
        }

        [Test]
        public void Deposit_NeverEmptiesThePods()
        {
            var a = C("A");
            var b = C("B");
            _party.members.AddRange(new[] { a, b });
            Assert.IsTrue(_party.Deposit(0));
            Assert.AreEqual(b, _party.members[0]);
            Assert.AreEqual(a, _party.storage[0]);
            Assert.IsFalse(_party.Deposit(0), "the last creature stays");
            Assert.AreEqual(1, _party.members.Count);
        }
    }
}
