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

        IslingInstance Creature(Element element, int speed, int level, params MoveData[] moves)
        {
            var s = Make<IslingSpecies>();
            s.displayName = $"{element} creature";
            s.element = element;
            s.baseStats = new BaseStats { hp = 40, attack = 10, defense = 10, speed = speed };
            s.moves = moves;
            return new IslingInstance(s, level);
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
                // A fainted wild creature is dazed (waiting for a pod), not over.
                for (int turn = 0; turn < 100 && !b.IsOver && !b.WildDazed; turn++) log.AddRange(b.PlayTurn(0).Select(e => e.ToString()));
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
            Assert.AreEqual(b.player.isling.Attack * 1.5f, b.player.Attack, 1e-4f);
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
        public void Win_FaintsWild_ThenItIsDazedUntilTheWindowEnds()
        {
            var player = Creature(Element.Water, 30, 5, Attack(Element.Water, 50));
            var wild = Creature(Element.Fire, 5, 3, Attack(Element.Neutral, 10));
            wild.currentHp = 1;
            var b = new BattleSystem(player, wild, _chart, 5, wildIsRare: true);
            var kinds = b.PlayTurn(0).Select(e => e.kind).ToList();
            CollectionAssert.AreEqual(new[] { BattleEventKind.MoveUsed, BattleEventKind.Damage, BattleEventKind.Fainted }, kinds);
            Assert.IsTrue(b.WildDazed);
            Assert.IsFalse(b.IsOver);
            Assert.AreEqual(0, player.xp, "XP is paid by the encounter after the win, not mid-turn");
            CollectionAssert.AreEqual(new[] { player }, b.Participants.ToArray());
            Assert.AreEqual(0, b.PlayTurn(0).Count, "no moves while it is dazed");
            Assert.AreEqual(0, b.TryFlee().Count);
            var end = b.EndDaze();
            Assert.AreEqual(BattleEventKind.Ended, end[0].kind);
            Assert.AreEqual(BattleResult.Won, b.Result, "no pod in time = won");
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
            var common = Make<IslingSpecies>();
            var rare = Make<IslingSpecies>();
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

        // ---- catching ----

        [Test]
        public void CatchChance_LowHpRarityAndLevel()
        {
            float full = BattleMath.CatchChance(20, 20, false, 5, 5);
            float half = BattleMath.CatchChance(10, 20, false, 5, 5);
            float low = BattleMath.CatchChance(1, 20, false, 5, 5);
            Assert.That(full, Is.EqualTo(0.19f).Within(0.01f));
            Assert.Less(full, half);
            Assert.Less(half, low);
            Assert.Less(BattleMath.CatchChance(10, 20, true, 5, 5), half, "rare is harder");
            Assert.Less(BattleMath.CatchChance(10, 20, false, 9, 5), half, "a higher level wild one is harder");
            Assert.Greater(BattleMath.CatchChance(10, 20, false, 3, 7), half, "a lower level wild one is easier");
            Assert.AreEqual(0.95f, BattleMath.CatchChance(0, 20, false, 1, 50), 1e-5f);
            Assert.That(BattleMath.CatchChance(20, 20, true, 50, 1), Is.InRange(0.05f, 0.06f), "worst case stays catchable");
        }

        [Test]
        public void CatchWobbles_ThreeOnSuccessFewerOnBadMisses()
        {
            Assert.AreEqual(3, BattleMath.CatchWobbles(0.1f, 0.5f));
            Assert.AreEqual(3, BattleMath.CatchWobbles(0.55f, 0.5f));
            Assert.AreEqual(2, BattleMath.CatchWobbles(0.7f, 0.5f));
            Assert.AreEqual(1, BattleMath.CatchWobbles(0.95f, 0.5f));
        }

        [Test]
        public void TryCatch_SuccessEndsTheBattle_FailureGivesTheWildOneATurn()
        {
            var tackle = Attack(Element.Neutral);
            int caught = 0, broke = 0;
            for (int seed = 0; seed < 100; seed++)
            {
                var wild = Creature(Element.Fire, 10, 5, tackle);
                wild.currentHp = Mathf.FloorToInt(wild.MaxHp * BattleMath.RedHpShare);
                var b = new BattleSystem(Creature(Element.Water, 10, 5, tackle), wild, _chart, seed);
                var events = b.TryCatch();
                Assert.AreEqual(BattleEventKind.CatchAttempt, events[0].kind);
                Assert.That(events[0].amount, Is.InRange(1, 3));
                if (events[0].result == BattleResult.Caught)
                {
                    caught++;
                    Assert.AreEqual(3, events[0].amount);
                    Assert.AreEqual(BattleResult.Caught, b.Result);
                    Assert.AreEqual(BattleEventKind.Ended, events[events.Count - 1].kind);
                }
                else
                {
                    broke++;
                    Assert.IsFalse(b.IsOver);
                    Assert.AreEqual(BattleEventKind.BrokeFree, events[1].kind);
                    Assert.AreEqual(BattleEventKind.MoveUsed, events[2].kind);
                    Assert.AreEqual(BattleSide.Wild, events[2].side);
                }
            }
            // A quarter HP at equal levels is about 76%.
            Assert.That(caught, Is.InRange(60, 92));
            Assert.Greater(broke, 0);
        }

        [Test]
        public void Catch_AboveRedAlwaysBreaksAndUsesTheTurn_ChanceAtRed()
        {
            var tackle = Attack(Element.Neutral);
            for (int seed = 0; seed < 50; seed++)
            {
                var wild = Creature(Element.Fire, 20, 5, tackle);
                var b = new BattleSystem(Creature(Element.Water, 10, 5, tackle), wild, _chart, seed);
                wild.currentHp = Mathf.FloorToInt(wild.MaxHp * BattleMath.RedHpShare) + 1;
                Assert.IsFalse(b.CatchUnlocked);
                var events = b.TryCatch();
                Assert.AreEqual(BattleEventKind.CatchAttempt, events[0].kind);
                Assert.AreEqual(1, events[0].amount, "one wobble, then it breaks");
                Assert.AreNotEqual(BattleResult.Caught, events[0].result);
                Assert.AreEqual(BattleEventKind.BrokeFree, events[1].kind);
                Assert.IsTrue(events.Any(e => e.kind == BattleEventKind.MoveUsed && e.side == BattleSide.Wild), "the wild creature gets its move");
                Assert.IsFalse(b.IsOver);
            }
            var w = Creature(Element.Fire, 20, 5, tackle);
            var red = new BattleSystem(Creature(Element.Water, 10, 5, tackle), w, _chart, 1);
            w.currentHp = Mathf.FloorToInt(w.MaxHp * BattleMath.RedHpShare);
            Assert.IsTrue(red.CatchUnlocked);
            Assert.Greater(red.CatchChance, 0.5f);
        }

        [Test]
        public void Dazed_SureCatch_RaresThreeQuarters()
        {
            Assert.AreEqual(1f, BattleMath.DazedCatchChance(false));
            Assert.AreEqual(0.75f, BattleMath.DazedCatchChance(true));
            var tackle = Attack(Element.Neutral, power: 200);
            int rareCaught = 0;
            for (int seed = 0; seed < 100; seed++)
            {
                foreach (bool rare in new[] { false, true })
                {
                    var wild = Creature(Element.Fire, 5, 5, tackle);
                    wild.currentHp = 1;
                    var b = new BattleSystem(Creature(Element.Water, 500, 5, tackle), wild, _chart, seed, rare);
                    b.PlayTurn(0);
                    Assert.IsTrue(b.WildDazed);
                    Assert.IsTrue(b.CatchUnlocked);
                    Assert.AreEqual(BattleMath.DazedCatchChance(rare), b.CatchChance);
                    var events = b.TryCatch();
                    Assert.IsTrue(b.IsOver, "a pod at a dazed one always ends the battle");
                    if (!rare) Assert.AreEqual(BattleResult.Caught, b.Result);
                    else if (b.Result == BattleResult.Caught) rareCaught++;
                    else
                    {
                        Assert.AreEqual(BattleResult.Won, b.Result, "a rare that breaks free runs off: won");
                        Assert.IsFalse(events.Any(e => e.kind == BattleEventKind.MoveUsed), "a dazed one doesn't attack");
                    }
                }
            }
            Assert.That(rareCaught, Is.InRange(60, 90));
        }

        // ---- switching ----

        [Test]
        public void Switch_UsesTheTurn_TheWildOneHitsTheNewcomer()
        {
            var tackle = Attack(Element.Neutral);
            var first = Creature(Element.Water, 10, 5, tackle);
            var second = Creature(Element.Fire, 10, 5, tackle);
            var b = new BattleSystem(first, Creature(Element.Wind, 10, 5, tackle), _chart, 3);
            b.player.attackStage = 2;
            var events = b.Switch(second);
            Assert.AreEqual(BattleEventKind.Switched, events[0].kind);
            Assert.AreSame(second, b.player.isling);
            Assert.AreEqual(0, b.player.attackStage, "stages don't carry over");
            var hit = events.First(e => e.kind == BattleEventKind.Damage);
            Assert.AreEqual(BattleSide.Player, hit.side);
            Assert.AreEqual(second.MaxHp - hit.amount, second.currentHp);
            Assert.AreEqual(first.MaxHp, first.currentHp, "the one that left wasn't hit");
            Assert.AreEqual(0, b.Switch(second).Count, "can't switch to the one already fighting");
        }

        [Test]
        public void Faint_WithReserve_KeepsTheBattleGoing_UntilAFreeSwitch()
        {
            var tackle = Attack(Element.Neutral, power: 200);
            var weak = Creature(Element.Water, 1, 5, tackle);
            var fresh = Creature(Element.Fire, 1, 5, tackle);
            var wild = Creature(Element.Wind, 50, 5, tackle);
            wild.currentHp = wild.MaxHp * 10; // survives anything
            var b = new BattleSystem(weak, wild, _chart, 1, false, () => !fresh.IsFainted);
            var events = b.PlayTurn(0);
            Assert.IsTrue(weak.IsFainted);
            Assert.IsFalse(b.IsOver);
            Assert.IsTrue(b.PlayerMustSwitch);
            Assert.IsFalse(events.Any(e => e.kind == BattleEventKind.MoveUsed && e.side == BattleSide.Player), "a fainted creature doesn't act");
            Assert.AreEqual(0, b.PlayTurn(0).Count, "no moves until the switch");
            Assert.AreEqual(0, b.TryCatch().Count);
            events = b.Switch(fresh);
            Assert.AreEqual(1, events.Count, "a switch after a faint is free");
            Assert.IsFalse(b.PlayerMustSwitch);
            b.PlayTurn(0);
            Assert.AreEqual(BattleResult.Lost, b.Result, "no reserve left");
        }

        [Test]
        public void Party_LeadAddAndStorage()
        {
            var go = new GameObject("Party");
            try
            {
                var party = go.AddComponent<Party>();
                var tackle = Attack(Element.Neutral);
                var a = Creature(Element.Fire, 10, 5, tackle);
                var b = Creature(Element.Water, 10, 5, tackle);
                party.members.Add(a);
                party.members.Add(b);
                Assert.AreSame(a, party.Lead);
                a.currentHp = 0;
                Assert.AreEqual(1, party.LeadSlot);
                Assert.AreEqual(1, party.NextFighter(0));
                b.currentHp = 0;
                Assert.IsTrue(party.AllFainted);
                int changed = 0;
                party.Changed += () => changed++;
                for (int i = 2; i < Party.Size; i++) Assert.AreEqual(i, party.Add(Creature(Element.Wind, 10, 3, tackle)));
                Assert.IsTrue(party.IsFull);
                Assert.AreEqual(-1, party.Add(Creature(Element.Wind, 10, 3, tackle)), "full: goes to storage");
                Assert.AreEqual(1, party.storage.Count);
                party.HealAll();
                Assert.AreSame(a, party.Lead);
                Assert.AreEqual(6, changed); // four pods, one stored, one heal
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
