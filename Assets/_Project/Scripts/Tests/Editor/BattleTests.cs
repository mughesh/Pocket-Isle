using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Diorama.Tests
{
    public class BattleTests
    {
        readonly List<Object> _made = new List<Object>();
        TypeChart _chart;

        [SetUp]
        public void SetUp() => _chart = Make<TypeChart>();

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

        MoveData Attack(Element element, int power = 40, int accuracy = 100)
        {
            var m = Make<MoveData>();
            m.displayName = $"{element} {power}";
            m.element = element;
            m.kind = MoveKind.Attack;
            m.power = power;
            m.accuracy = accuracy;
            return m;
        }

        MoveData Support(SupportEffect effect)
        {
            var m = Make<MoveData>();
            m.displayName = effect.ToString();
            m.kind = MoveKind.Support;
            m.effect = effect;
            m.healShare = 0.3f;
            return m;
        }

        CreatureInstance Creature(Element element, int speed, int level, params MoveData[] moves)
        {
            var s = Make<CreatureSpecies>();
            s.displayName = $"{element} creature";
            s.element = element;
            s.baseStats = new BaseStats { hp = 40, attack = 10, defense = 10, speed = speed };
            s.moves = moves;
            return new CreatureInstance(s, level);
        }

        // ---- type chart ----

        [Test]
        public void TypeChart_Cycle()
        {
            Assert.AreEqual(Effectiveness.Super, _chart.Matchup(Element.Water, Element.Fire));
            Assert.AreEqual(Effectiveness.Super, _chart.Matchup(Element.Fire, Element.Wind));
            Assert.AreEqual(Effectiveness.Super, _chart.Matchup(Element.Wind, Element.Water));
            Assert.AreEqual(Effectiveness.NotVery, _chart.Matchup(Element.Fire, Element.Water));
            Assert.AreEqual(Effectiveness.NotVery, _chart.Matchup(Element.Water, Element.Wind));
            Assert.AreEqual(Effectiveness.NotVery, _chart.Matchup(Element.Wind, Element.Fire));
        }

        [Test]
        public void TypeChart_SameElementAndNeutral()
        {
            Assert.AreEqual(Effectiveness.NotVery, _chart.Matchup(Element.Fire, Element.Fire));
            _chart.sameElementResists = false;
            Assert.AreEqual(Effectiveness.Normal, _chart.Matchup(Element.Fire, Element.Fire));
            foreach (Element e in System.Enum.GetValues(typeof(Element)))
            {
                Assert.AreEqual(Effectiveness.Normal, _chart.Matchup(Element.Neutral, e));
                Assert.AreEqual(Effectiveness.Normal, _chart.Matchup(e, Element.Neutral));
            }
        }

        [Test]
        public void TypeChart_Multipliers()
        {
            Assert.AreEqual(1.5f, _chart.Multiplier(Element.Water, Element.Fire), 1e-5f);
            Assert.AreEqual(0.67f, _chart.Multiplier(Element.Fire, Element.Water), 1e-5f);
            Assert.AreEqual(1f, _chart.Multiplier(Element.Neutral, Element.Fire), 1e-5f);
        }

        // ---- damage ----

        [Test]
        public void Damage_Formula()
        {
            Assert.AreEqual(13, BattleMath.Damage(5, 40, 10, 10, 1f, 1f));   // 0.333 x 40
            Assert.AreEqual(20, BattleMath.Damage(5, 40, 10, 10, 1.5f, 1f));
            Assert.AreEqual(27, BattleMath.Damage(5, 40, 20, 10, 1f, 1f));   // double attack, double damage
            Assert.AreEqual(1, BattleMath.Damage(1, 1, 1, 100, 0.67f, 0.9f)); // never below 1
        }

        [Test]
        public void Damage_InBattle_StaysInRollRange()
        {
            var fire = Attack(Element.Fire);
            for (int seed = 0; seed < 200; seed++)
            {
                var player = Creature(Element.Fire, 20, 5, fire);
                var wild = Creature(Element.Wind, 5, 5, Support(SupportEffect.Shield));
                var events = new BattleSystem(player, wild, _chart, seed).PlayTurn(0);
                var hit = events.First(e => e.kind == BattleEventKind.Damage);
                Assert.AreEqual(Effectiveness.Super, hit.effectiveness);
                Assert.That(hit.amount, Is.InRange(18, 22), $"seed {seed}"); // 20 x 0.9..1.1
                Assert.AreEqual(wild.MaxHp - hit.amount, wild.currentHp);
            }
        }

        // ---- turn order ----

        [Test]
        public void TurnOrder_FasterGoesFirst()
        {
            var tackle = Attack(Element.Neutral, 10);
            var fast = new BattleSystem(Creature(Element.Fire, 30, 5, tackle), Creature(Element.Fire, 5, 5, tackle), _chart, 1);
            Assert.AreEqual(BattleSide.Player, fast.PlayTurn(0).First(e => e.kind == BattleEventKind.MoveUsed).side);

            var slow = new BattleSystem(Creature(Element.Fire, 5, 5, tackle), Creature(Element.Fire, 30, 5, tackle), _chart, 1);
            Assert.AreEqual(BattleSide.Wild, slow.PlayTurn(0).First(e => e.kind == BattleEventKind.MoveUsed).side);
        }

        [Test]
        public void TurnOrder_TieIsMixed()
        {
            var tackle = Attack(Element.Neutral, 10);
            int playerFirst = 0;
            for (int seed = 0; seed < 200; seed++)
            {
                var b = new BattleSystem(Creature(Element.Fire, 10, 5, tackle), Creature(Element.Fire, 10, 5, tackle), _chart, seed);
                if (b.PlayTurn(0).First(e => e.kind == BattleEventKind.MoveUsed).side == BattleSide.Player) playerFirst++;
            }
            Assert.That(playerFirst, Is.InRange(60, 140));
        }

        [Test]
        public void SameSeed_SameBattle()
        {
            var a = Attack(Element.Fire, 40, 80);
            string Run()
            {
                var b = new BattleSystem(Creature(Element.Fire, 10, 5, a), Creature(Element.Wind, 10, 5, a), _chart, 42);
                var log = new List<string>();
                while (!b.IsOver) log.AddRange(b.PlayTurn(0).Select(e => e.ToString()));
                return string.Join("|", log);
            }
            Assert.AreEqual(Run(), Run());
        }

        // ---- flee ----

        [Test]
        public void Flee_Chance()
        {
            Assert.AreEqual(0.7f, BattleMath.FleeChance(10, 10, 0), 1e-5f);
            Assert.AreEqual(0.85f, BattleMath.FleeChance(10, 10, 1), 1e-5f);
            Assert.AreEqual(1f, BattleMath.FleeChance(30, 5, 0), 1e-5f);
            Assert.AreEqual(0.34f, BattleMath.FleeChance(1, 10, 0), 1e-5f);
        }

        [Test]
        public void Flee_RateMatchesChance()
        {
            var tackle = Attack(Element.Neutral, 10);
            int fled = 0;
            for (int seed = 0; seed < 1000; seed++)
            {
                var b = new BattleSystem(Creature(Element.Fire, 10, 5, tackle), Creature(Element.Fire, 10, 5, tackle), _chart, seed);
                b.TryFlee();
                if (b.Result == BattleResult.Fled) fled++;
            }
            Assert.That(fled, Is.InRange(650, 750));
        }

        [Test]
        public void Flee_FailGivesWildAFreeMove()
        {
            var tackle = Attack(Element.Neutral, 10);
            for (int seed = 0; seed < 100; seed++)
            {
                var b = new BattleSystem(Creature(Element.Fire, 1, 5, tackle), Creature(Element.Fire, 30, 5, tackle), _chart, seed);
                var events = b.TryFlee();
                if (b.Result == BattleResult.Fled) continue;
                Assert.AreEqual(BattleEventKind.FleeFailed, events[0].kind);
                Assert.AreEqual(BattleEventKind.MoveUsed, events[1].kind);
                Assert.AreEqual(BattleSide.Wild, events[1].side);
                return;
            }
            Assert.Fail("Every flee worked at a 34% chance.");
        }

        // ---- support ----

        [Test]
        public void Shield_HalvesNextHitOnce()
        {
            var tackle = Attack(Element.Neutral, 40);
            int Hit(SupportEffect playerMove, out bool absorbed)
            {
                var b = new BattleSystem(Creature(Element.Fire, 30, 5, Support(playerMove)), Creature(Element.Fire, 5, 5, tackle), _chart, 7);
                var hit = b.PlayTurn(0).First(e => e.kind == BattleEventKind.Damage);
                absorbed = hit.shieldAbsorbed;
                Assert.IsFalse(b.player.shielded, "the shield is used up");
                return hit.amount;
            }
            int plain = Hit(SupportEffect.RaiseAttack, out bool a1);
            int shielded = Hit(SupportEffect.Shield, out bool a2);
            Assert.IsFalse(a1);
            Assert.IsTrue(a2);
            Assert.AreEqual(Mathf.Max(1, plain / 2), shielded);
        }

        [Test]
        public void RaiseAttack_HitsHarder()
        {
            var b = new BattleSystem(Creature(Element.Fire, 30, 5, Support(SupportEffect.RaiseAttack), Attack(Element.Neutral)),
                Creature(Element.Fire, 5, 50, Support(SupportEffect.Heal)), _chart, 3);
            b.PlayTurn(0);
            Assert.AreEqual(1, b.player.attackStage);
            Assert.AreEqual(1.5f, BattleMath.StageMultiplier(1), 1e-5f);
            Assert.AreEqual(b.player.creature.Attack * 1.5f, b.player.Attack, 1e-4f);
        }

        [Test]
        public void Heal_RestoresShareCappedAtMax()
        {
            var player = Creature(Element.Fire, 30, 5, Support(SupportEffect.Heal));
            var wild = Creature(Element.Fire, 5, 5, Support(SupportEffect.Shield));
            player.currentHp = 10;
            var b = new BattleSystem(player, wild, _chart, 1);
            var healed = b.PlayTurn(0).First(e => e.kind == BattleEventKind.Healed);
            Assert.AreEqual(Mathf.RoundToInt(player.MaxHp * 0.3f), healed.amount);
            Assert.AreEqual(10 + healed.amount, player.currentHp);

            player.currentHp = player.MaxHp - 2;
            Assert.AreEqual(2, b.PlayTurn(0).First(e => e.kind == BattleEventKind.Healed).amount);
            Assert.AreEqual(player.MaxHp, player.currentHp);
        }

        // ---- end of battle ----

        [Test]
        public void Win_FaintsWildAndGivesXp()
        {
            var player = Creature(Element.Water, 30, 5, Attack(Element.Water, 50));
            var wild = Creature(Element.Fire, 5, 3, Attack(Element.Neutral, 10));
            wild.currentHp = 1;
            var b = new BattleSystem(player, wild, _chart, 5, wildIsRare: true);
            var kinds = b.PlayTurn(0).Select(e => e.kind).ToList();
            CollectionAssert.AreEqual(new[] { BattleEventKind.MoveUsed, BattleEventKind.Damage, BattleEventKind.Fainted, BattleEventKind.XpGained, BattleEventKind.Ended }, kinds);
            Assert.AreEqual(BattleResult.Won, b.Result);
            Assert.AreEqual(BattleMath.XpReward(3, true), player.xp);
            Assert.AreEqual(0, b.PlayTurn(0).Count, "no turns after the end");
        }

        [Test]
        public void Lose_WhenPlayerFaints()
        {
            var player = Creature(Element.Fire, 5, 5, Attack(Element.Neutral, 10));
            var wild = Creature(Element.Water, 30, 5, Attack(Element.Water, 50));
            player.currentHp = 1;
            var b = new BattleSystem(player, wild, _chart, 5);
            b.PlayTurn(0);
            Assert.AreEqual(BattleResult.Lost, b.Result);
            Assert.AreEqual(0, wild.xp);
        }

        [Test]
        public void EncounterTable_RollsWeightsAndLevels()
        {
            var table = Make<EncounterTable>();
            var common = Make<CreatureSpecies>();
            var rare = Make<CreatureSpecies>();
            table.entries = new[]
            {
                new EncounterTable.Entry { species = common, minLevel = 3, maxLevel = 5, weight = 95f },
                new EncounterTable.Entry { species = rare, minLevel = 7, maxLevel = 7, weight = 5f, rare = true },
            };
            var rnd = new SeededRandom(9);
            int rares = 0;
            for (int i = 0; i < 2000; i++)
            {
                Assert.IsTrue(table.Roll(rnd, out var c, out bool isRare));
                Assert.AreEqual(isRare, c.species == rare);
                if (isRare) { rares++; Assert.AreEqual(7, c.level); }
                else Assert.That(c.level, Is.InRange(3, 5));
                Assert.AreEqual(c.MaxHp, c.currentHp);
            }
            Assert.That(rares, Is.InRange(60, 140)); // about 5%
        }
    }
}
