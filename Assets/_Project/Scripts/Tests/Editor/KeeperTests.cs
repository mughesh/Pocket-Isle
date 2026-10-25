using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>
    /// Step 9c: the Keeper's battle rules (crown shield: Water breaks it, 3 hits break it, it rises again after 2 turns;
    /// the charge every 3rd turn; phase 2 at 50%; no capture; running always works; no daze) and its saved state.
    /// </summary>
    public class KeeperTests
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
            PuzzleState.Restore(null);
        }

        T Make<T>() where T : ScriptableObject
        {
            var o = ScriptableObject.CreateInstance<T>();
            _made.Add(o);
            return o;
        }

        MoveData Attack(Element element, int power = 40, string name = null)
        {
            var m = Make<MoveData>();
            m.displayName = name ?? $"{element} {power}";
            m.element = element;
            m.kind = MoveKind.Attack;
            m.power = power;
            m.accuracy = 100;
            return m;
        }

        MoveData ShieldMove()
        {
            var m = Make<MoveData>();
            m.displayName = "Crown Shield";
            m.kind = MoveKind.Support;
            m.effect = SupportEffect.Shield;
            return m;
        }

        IslingInstance Creature(Element element, int hp, int speed, int level, params MoveData[] moves)
        {
            var s = Make<IslingSpecies>();
            s.displayName = $"{element} creature";
            s.element = element;
            s.baseStats = new BaseStats { hp = hp, attack = 12, defense = 10, speed = speed };
            s.moves = moves;
            return new IslingInstance(s, level);
        }

        /// <summary>A slow Keeper (the player always moves first) with shot, charge, shield.</summary>
        (BattleSystem battle, KeeperRules rules, MoveData shot, MoveData charge, MoveData shield) Fight(Element playerElement, int keeperHp = 400, MoveData playerMove = null)
        {
            var shot = Attack(Element.Fire, 30, "Ember Shot");
            var charge = Attack(Element.Neutral, 40, "Crown Charge");
            var shield = ShieldMove();
            var keeper = Creature(Element.Fire, keeperHp, 1, 12, shot, charge, shield);
            var player = Creature(playerElement, 900, 50, 14, playerMove ?? Attack(playerElement, 45));
            var rules = new KeeperRules();
            return (new BattleSystem(player, keeper, _chart, 1, false, null, rules), rules, shot, charge, shield);
        }

        static BattleEvent HitOnKeeper(IReadOnlyList<BattleEvent> events) =>
            events.First(e => e.kind == BattleEventKind.Damage && e.side == BattleSide.Wild);

        [Test]
        public void Shield_BlocksMostDamage_ThreeHitsBreakIt()
        {
            var (battle, rules, _, _, _) = Fight(Element.Neutral);
            Assert.IsTrue(rules.Shielded);
            var first = HitOnKeeper(battle.PlayTurn(0));
            Assert.IsTrue(first.blocked, "shielded: blocked");
            Assert.AreEqual(1, first.cracks);
            var second = HitOnKeeper(battle.PlayTurn(0));
            Assert.AreEqual(2, second.cracks);
            var third = battle.PlayTurn(0);
            Assert.IsTrue(HitOnKeeper(third).blocked, "the third hit is still blocked");
            Assert.IsTrue(third.Any(e => e.kind == BattleEventKind.ShieldBroken), "...and breaks it");
            Assert.IsFalse(rules.Shielded);
            var open = HitOnKeeper(battle.PlayTurn(0));
            Assert.IsFalse(open.blocked);
            Assert.Greater(open.amount, first.amount * 5, "broken: about ten times the damage");
        }

        [Test]
        public void Shield_WaterBreaksItAtOnce()
        {
            var (battle, rules, _, _, _) = Fight(Element.Water);
            var events = battle.PlayTurn(0);
            Assert.IsFalse(HitOnKeeper(events).blocked, "Water goes straight through");
            Assert.IsTrue(events.Any(e => e.kind == BattleEventKind.ShieldBroken));
            Assert.IsFalse(rules.Shielded);
        }

        [Test]
        public void Shield_RisesAgainAfterTwoOpenTurns()
        {
            var (battle, rules, _, _, shield) = Fight(Element.Water);
            battle.PlayTurn(0); // breaks it
            Assert.IsFalse(rules.Shielded);
            battle.PlayTurn(0); // open turn 1
            Assert.IsFalse(rules.Shielded);
            Assert.IsFalse(rules.RaiseDue);
            battle.PlayTurn(0); // open turn 2
            Assert.IsTrue(rules.RaiseDue, "two open turns: the shield comes next");
            var events = battle.PlayTurn(0);
            Assert.IsTrue(events.Any(e => e.kind == BattleEventKind.MoveUsed && e.side == BattleSide.Wild && e.move == shield));
            Assert.IsTrue(events.Any(e => e.kind == BattleEventKind.ShieldRaised));
            Assert.IsTrue(rules.Shielded);
            Assert.AreEqual(0, rules.Cracks, "cracks mended");
        }

        [Test]
        public void ChargeEveryThirdTurn()
        {
            // The player only powers up: no hits, so the shield never breaks and never needs raising.
            var puff = Make<MoveData>();
            puff.kind = MoveKind.Support;
            puff.effect = SupportEffect.RaiseAttack;
            var (battle, _, shot, charge, _) = Fight(Element.Neutral, 5000, puff);
            var used = new List<MoveData>();
            for (int i = 0; i < 6; i++)
                used.Add(battle.PlayTurn(0).First(e => e.kind == BattleEventKind.MoveUsed && e.side == BattleSide.Wild).move);
            CollectionAssert.AreEqual(new[] { shot, shot, charge, shot, shot, charge }, used);
        }

        [Test]
        public void Phase2_AtHalfHp_ShotHitsHarder()
        {
            var (battle, rules, _, _, _) = Fight(Element.Water, 120);
            int before = 0, after = 0;
            bool announced = false;
            for (int i = 0; i < 12 && !battle.IsOver; i++)
            {
                var events = battle.PlayTurn(0);
                announced |= events.Any(e => e.kind == BattleEventKind.KeeperPhase2);
                foreach (var e in events)
                    if (e.kind == BattleEventKind.Damage && e.side == BattleSide.Player && e.move.displayName == "Ember Shot")
                    {
                        if (rules.Phase2) after = Mathf.Max(after, e.amount);
                        else before = Mathf.Max(before, e.amount);
                    }
            }
            Assert.IsTrue(announced, "phase 2 announced once below 50%");
            Assert.IsTrue(rules.Phase2);
            Assert.Greater(before, 0);
            Assert.Greater(after, before, "phase 2 shots hit harder");
        }

        [Test]
        public void NoCapture_RunAlwaysWorks_FaintingWinsWithoutDaze()
        {
            var (battle, _, _, _, _) = Fight(Element.Water, 10);
            Assert.IsFalse(battle.CatchUnlocked);
            Assert.AreEqual(0, battle.TryCatch().Count, "no pod against a Keeper");
            for (int i = 0; i < 20 && !battle.IsOver; i++) battle.PlayTurn(0);
            Assert.AreEqual(BattleResult.Won, battle.Result);
            Assert.IsFalse(battle.WildDazed);

            var (run, _, _, _, _) = Fight(Element.Neutral);
            run.TryFlee();
            Assert.AreEqual(BattleResult.Fled, run.Result);
        }

        [Test]
        public void KeeperStateSurvivesTheSave()
        {
            PuzzleState.Restore(null);
            PuzzleState.Solve("cinder.keeper", 1);
            PuzzleState.SetStage("shard.fire", 1);
            var data = new SaveData { puzzles = PuzzleState.ToSaved() };
            data.SetRestored("WorldMap_Cinderwood", 1f);
            var back = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data));
            PuzzleState.Restore(back.puzzles);
            Assert.IsTrue(PuzzleState.IsSolved("cinder.keeper"), "calmed");
            Assert.AreEqual(1, PuzzleState.Stage("shard.fire"), "shard held");
            Assert.IsFalse(PuzzleState.IsSolved("shard.fire"), "not placed yet");
            Assert.AreEqual(1f, back.RestoredFor("WorldMap_Cinderwood", 0f), "realm restored");
            PuzzleState.Solve("shard.fire", 2);
            PuzzleState.Restore(JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(new SaveData { puzzles = PuzzleState.ToSaved() })).puzzles);
            Assert.IsTrue(PuzzleState.IsSolved("shard.fire"), "placed in the statue");
        }
    }
}
