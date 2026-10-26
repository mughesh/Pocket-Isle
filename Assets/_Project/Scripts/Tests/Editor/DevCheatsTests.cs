using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>Step 9e: the dev panel's switches reach the battle and encounter rules, and are off by default.</summary>
    public class DevCheatsTests
    {
        readonly List<Object> _made = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            DevCheats.OneHitKo = DevCheats.Invincible = DevCheats.WildOff = false;
            DevCheats.RareBoost = 1f;
            foreach (var o in _made) Object.DestroyImmediate(o);
            _made.Clear();
        }

        T Make<T>() where T : ScriptableObject
        {
            var o = ScriptableObject.CreateInstance<T>();
            _made.Add(o);
            return o;
        }

        IslingInstance Creature(int hp, int speed)
        {
            var move = Make<MoveData>();
            move.displayName = "Hit";
            move.kind = MoveKind.Attack;
            move.power = 40;
            move.accuracy = 100;
            var s = Make<IslingSpecies>();
            s.baseStats = new BaseStats { hp = hp, attack = 10, defense = 10, speed = speed };
            s.moves = new[] { move };
            return new IslingInstance(s, 5);
        }

        [Test]
        public void OffByDefault()
        {
            Assert.IsFalse(DevCheats.OneHitKo || DevCheats.Invincible || DevCheats.WildOff);
            Assert.AreEqual(1f, DevCheats.RareBoost);
        }

        [Test]
        public void OneHitKo_KnocksOutTheWildOne_EvenAKeeper()
        {
            DevCheats.OneHitKo = true;
            var battle = new BattleSystem(Creature(40, 20), Creature(400, 1), null, 1, false, null, new KeeperRules());
            battle.PlayTurn(0);
            Assert.AreEqual(BattleResult.Won, battle.Result);
        }

        [Test]
        public void Invincible_PlayerTakesNoDamage()
        {
            DevCheats.Invincible = true;
            var me = Creature(40, 1);
            var battle = new BattleSystem(me, Creature(400, 20), null, 1);
            for (int i = 0; i < 5; i++) battle.PlayTurn(0);
            Assert.AreEqual(me.MaxHp, me.currentHp);
        }

        [Test]
        public void RareBoost_MakesRaresCommon()
        {
            var common = Make<IslingSpecies>();
            var rare = Make<IslingSpecies>();
            var table = Make<EncounterTable>();
            table.entries = new[]
            {
                new EncounterTable.Entry { species = common, minLevel = 3, maxLevel = 3, weight = 95f },
                new EncounterTable.Entry { species = rare, minLevel = 3, maxLevel = 3, weight = 5f, rare = true },
            };
            int Rares()
            {
                var rnd = new SeededRandom(7);
                int n = 0;
                for (int i = 0; i < 2000; i++)
                    if (table.Roll(rnd, out _, out bool r) && r) n++;
                return n;
            }
            int normal = Rares();
            DevCheats.RareBoost = 10f;
            int boosted = Rares();
            Assert.That(normal, Is.InRange(50, 160));
            Assert.That(boosted, Is.GreaterThan(normal * 4));
        }
    }
}
