using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Diorama.Tests
{
    public class ProgressionTests
    {
        readonly List<Object> _made = new List<Object>();
        ProgressionConfig _config;

        [SetUp]
        public void SetUp() => _config = Make<ProgressionConfig>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _made) Object.DestroyImmediate(o);
            _made.Clear();
        }

        T Make<T>() where T : ScriptableObject
        {
            var o = ScriptableObject.CreateInstance<T>();
            _made.Add(o);
            return o;
        }

        IslingSpecies Species(string name, int hp = 40)
        {
            var s = Make<IslingSpecies>();
            s.name = name;
            s.displayName = name;
            s.baseStats = new BaseStats { hp = hp, attack = 10, defense = 10, speed = 10 };
            return s;
        }

        [Test]
        public void XpCurve_IsSixTimesLevelPlusTen()
        {
            Assert.AreEqual(40, _config.XpToNext(5));
            Assert.AreEqual(16, _config.XpToNext(1));
            Assert.AreEqual(184, _config.XpToNext(29));
            // (5 + 4 x 5) x 3 (step 9f)
            Assert.AreEqual(75, _config.Reward(5, false));
            Assert.AreEqual(150, _config.Reward(5, true));
        }

        [Test]
        public void AddXp_BelowTheNextLevel_JustAdds()
        {
            var c = new IslingInstance(Species("A"), 5);
            Assert.AreEqual(0, Progression.AddXp(c, 39, _config).Count);
            Assert.AreEqual(5, c.level);
            Assert.AreEqual(39, c.xp);
        }

        [Test]
        public void AddXp_SeveralLevelsAtOnce_CarriesTheRest()
        {
            var c = new IslingInstance(Species("A"), 5);
            // 40 (5->6) + 46 (6->7) + 52 (7->8) = 138, plus 7 left over.
            var ups = Progression.AddXp(c, 145, _config);
            Assert.AreEqual(3, ups.Count);
            CollectionAssert.AreEqual(new[] { 6, 7, 8 }, ups.ConvertAll(u => u.level));
            Assert.AreEqual(8, c.level);
            Assert.AreEqual(7, c.xp);
        }

        [Test]
        public void LevelUp_RaisesMaxHpAndCurrentHpByTheSameAmount()
        {
            var c = new IslingInstance(Species("A", 40), 5);
            c.currentHp = 10;
            int max = c.MaxHp;
            var ups = Progression.AddXp(c, 40, _config);
            int gain = c.MaxHp - max;
            Assert.Greater(gain, 0);
            Assert.AreEqual(gain, ups[0].hpGain);
            Assert.AreEqual(10 + gain, c.currentHp);

            var fainted = new IslingInstance(Species("B", 40), 5) { currentHp = 0 };
            Progression.AddXp(fainted, 40, _config);
            Assert.AreEqual(6, fainted.level);
            Assert.AreEqual(0, fainted.currentHp, "fainted stays fainted until healed");
        }

        [Test]
        public void LevelCap_StopsLevelsAndXp()
        {
            _config.levelCap = 7;
            var c = new IslingInstance(Species("A"), 6);
            Progression.AddXp(c, 10000, _config);
            Assert.AreEqual(7, c.level);
            Assert.AreEqual(0, c.xp);
            Assert.AreEqual(0, Progression.AddXp(c, 500, _config).Count);
            Assert.AreEqual(1f, _config.XpShare(c));
        }

        [Test]
        public void Share_FullToEveryFighter_FaintedToo()
        {
            var c = new IslingInstance(Species("A"), 5) { currentHp = 0 };
            Assert.AreEqual(25, Progression.ShareFor(c, 25, 3, _config));
            _config.faintedShare = false;
            Assert.AreEqual(0, Progression.ShareFor(c, 25, 3, _config));
        }

        [Test]
        public void Evolution_AtTheLevel_KeepsHpRatioLevelXpAndNickname()
        {
            var small = Species("Small", 40);
            var big = Species("Big", 80);
            small.evolvesTo = big;
            small.evolveLevel = 12;
            var c = new IslingInstance(small, 11) { nickname = "Pip", xp = 3 };
            Assert.IsFalse(Progression.CanEvolve(c));
            c.level = 12;
            c.currentHp = c.MaxHp / 2;
            Assert.IsTrue(Progression.CanEvolve(c));
            Assert.IsTrue(Progression.Evolve(c));
            Assert.AreEqual(big, c.species);
            Assert.AreEqual(12, c.level);
            Assert.AreEqual(3, c.xp);
            Assert.AreEqual("Pip", c.Name);
            Assert.AreEqual(c.MaxHp / 2, c.currentHp, 1);
            Assert.IsFalse(Progression.CanEvolve(c), "no further form");
        }

        [Test]
        public void Evolution_NeedsEvolvesTo()
        {
            var lone = Species("Lone");
            lone.evolveLevel = 5;
            var c = new IslingInstance(lone, 20);
            Assert.IsFalse(Progression.CanEvolve(c));
            Assert.IsFalse(Progression.Evolve(c));
            Assert.AreEqual(lone, c.species);
        }
    }
}
