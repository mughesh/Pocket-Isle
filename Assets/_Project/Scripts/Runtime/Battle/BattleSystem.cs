using System;
using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    public enum BattleSide { Player, Wild }

    public enum BattleResult { None, Won, Lost, Fled }

    public enum BattleEventKind { MoveUsed, Missed, Damage, Shielded, AttackRaised, Healed, Fainted, FleeFailed, Fled, XpGained, Ended }

    /// <summary>
    /// One thing that happened in a turn, in order. <see cref="side"/> is the creature it happened to: the user of a
    /// move for MoveUsed / Missed / Shielded / AttackRaised / Healed, the one hit for Damage.
    /// </summary>
    public struct BattleEvent
    {
        public BattleEventKind kind;
        public BattleSide side;
        public MoveData move;
        public int amount;
        public int hpAfter;
        public Effectiveness effectiveness;
        public bool shieldAbsorbed;
        public BattleResult result;

        public override string ToString() => kind switch
        {
            BattleEventKind.MoveUsed => $"{side} used {move.Name}",
            BattleEventKind.Damage => $"{side} took {amount} ({effectiveness}{(shieldAbsorbed ? ", shield" : "")}), HP {hpAfter}",
            BattleEventKind.Healed => $"{side} healed {amount}, HP {hpAfter}",
            BattleEventKind.AttackRaised => $"{side} attack stage {amount}",
            BattleEventKind.XpGained => $"{side} +{amount} XP",
            BattleEventKind.Ended => $"Ended: {result}",
            _ => $"{kind} ({side})",
        };
    }

    /// <summary>One side of the battle: the creature plus battle-only state (stages, shield).</summary>
    public class Battler
    {
        public readonly BattleSide side;
        public readonly CreatureInstance creature;
        public int attackStage;
        public bool shielded;

        public Battler(BattleSide side, CreatureInstance creature)
        {
            this.side = side;
            this.creature = creature;
        }

        public MoveData[] Moves => creature.species != null && creature.species.moves != null ? creature.species.moves : Array.Empty<MoveData>();
        public int Hp => creature.currentHp;
        public int MaxHp => creature.MaxHp;
        public float Attack => creature.Attack * BattleMath.StageMultiplier(attackStage);
    }

    /// <summary>
    /// 1v1 wild battle rules, no Unity objects in the loop: turn order by speed, accuracy, damage, support effects,
    /// fainting, flee. Each call plays a whole turn and returns what happened, in order, for the presentation to
    /// animate. The same seed and choices always give the same battle.
    /// </summary>
    public class BattleSystem
    {
        public readonly Battler player, wild;
        public readonly bool wildIsRare;
        readonly TypeChart _chart;
        readonly SeededRandom _rnd;
        readonly List<BattleEvent> _turn = new List<BattleEvent>();
        int _failedFlees;

        /// <summary>Raised for every event as it happens (the same ones the turn methods return).</summary>
        public event Action<BattleEvent> EventRaised;

        public BattleResult Result { get; private set; }
        public bool IsOver => Result != BattleResult.None;

        public BattleSystem(CreatureInstance player, CreatureInstance wild, TypeChart chart, int seed, bool wildIsRare = false)
        {
            this.player = new Battler(BattleSide.Player, player);
            this.wild = new Battler(BattleSide.Wild, wild);
            this.wildIsRare = wildIsRare;
            _chart = chart;
            _rnd = new SeededRandom(seed);
        }

        public Battler Get(BattleSide side) => side == BattleSide.Player ? player : wild;
        Battler Other(Battler b) => b == player ? wild : player;

        /// <summary>The player uses move <paramref name="moveIndex"/>; the wild creature answers. Faster one goes first.</summary>
        public IReadOnlyList<BattleEvent> PlayTurn(int moveIndex)
        {
            _turn.Clear();
            if (IsOver) return _turn;
            var moves = player.Moves;
            if (moveIndex < 0 || moveIndex >= moves.Length || moves[moveIndex] == null)
            {
                Debug.LogError($"[BattleSystem] {player.creature.Name} has no move in slot {moveIndex}.");
                return _turn;
            }
            var playerMove = moves[moveIndex];
            var wildMove = PickWildMove();
            bool playerFirst = PlayerGoesFirst();
            var first = playerFirst ? player : wild;
            Act(first, playerFirst ? playerMove : wildMove);
            if (!IsOver) Act(Other(first), playerFirst ? wildMove : playerMove);
            return _turn;
        }

        /// <summary>Tries to run. On a miss the wild creature gets a free move.</summary>
        public IReadOnlyList<BattleEvent> TryFlee()
        {
            _turn.Clear();
            if (IsOver) return _turn;
            float chance = BattleMath.FleeChance(player.creature.Speed, wild.creature.Speed, _failedFlees);
            if (_rnd.Next() < chance)
            {
                Raise(new BattleEvent { kind = BattleEventKind.Fled, side = BattleSide.Player });
                End(BattleResult.Fled);
                return _turn;
            }
            _failedFlees++;
            Raise(new BattleEvent { kind = BattleEventKind.FleeFailed, side = BattleSide.Player });
            Act(wild, PickWildMove());
            return _turn;
        }

        /// <summary>Faster creature first; a tie is a coin flip.</summary>
        public bool PlayerGoesFirst()
        {
            int p = player.creature.Speed, w = wild.creature.Speed;
            return p != w ? p > w : _rnd.Next() < 0.5f;
        }

        /// <summary>Random by weight: attacks are likely, supports only when they would help.</summary>
        public MoveData PickWildMove()
        {
            var moves = wild.Moves;
            float total = 0f;
            foreach (var m in moves) total += WildWeight(m);
            if (total <= 0f) return null;
            float pick = _rnd.Next() * total;
            foreach (var m in moves)
            {
                pick -= WildWeight(m);
                if (pick < 0f) return m;
            }
            return null;
        }

        float WildWeight(MoveData m)
        {
            if (m == null) return 0f;
            if (m.kind == MoveKind.Attack) return 3f;
            return m.effect switch
            {
                SupportEffect.Shield => wild.shielded ? 0f : 1f,
                SupportEffect.RaiseAttack => wild.attackStage >= 2 ? 0f : 1f,
                SupportEffect.Heal => wild.Hp < wild.MaxHp * 0.6f ? 2f : 0f,
                _ => 0f,
            };
        }

        void Act(Battler user, MoveData move)
        {
            if (move == null) return; // a creature with no usable move skips its turn
            var target = Other(user);
            Raise(new BattleEvent { kind = BattleEventKind.MoveUsed, side = user.side, move = move });

            if (move.kind == MoveKind.Support)
            {
                switch (move.effect)
                {
                    case SupportEffect.Shield:
                        user.shielded = true;
                        Raise(new BattleEvent { kind = BattleEventKind.Shielded, side = user.side, move = move });
                        break;
                    case SupportEffect.RaiseAttack:
                        user.attackStage = Mathf.Min(BattleMath.MaxStage, user.attackStage + 1);
                        Raise(new BattleEvent { kind = BattleEventKind.AttackRaised, side = user.side, move = move, amount = user.attackStage });
                        break;
                    case SupportEffect.Heal:
                        int heal = Mathf.Min(user.MaxHp - user.Hp, Mathf.Max(1, Mathf.RoundToInt(user.MaxHp * move.healShare)));
                        user.creature.currentHp += heal;
                        Raise(new BattleEvent { kind = BattleEventKind.Healed, side = user.side, move = move, amount = heal, hpAfter = user.Hp });
                        break;
                }
                return;
            }

            if (_rnd.Next() * 100f >= move.accuracy)
            {
                Raise(new BattleEvent { kind = BattleEventKind.Missed, side = user.side, move = move });
                return;
            }
            var effectiveness = _chart != null ? _chart.Matchup(move.element, target.creature.Element) : Effectiveness.Normal;
            float typeMul = _chart != null ? _chart.Multiplier(effectiveness) : 1f;
            float roll = Mathf.Lerp(BattleMath.RollMin, BattleMath.RollMax, _rnd.Next());
            int damage = BattleMath.Damage(user.creature.level, move.power, user.Attack, target.creature.Defense, typeMul, roll);
            bool absorbed = target.shielded;
            if (absorbed)
            {
                damage = Mathf.Max(1, damage / 2);
                target.shielded = false;
            }
            target.creature.currentHp = Mathf.Max(0, target.Hp - damage);
            Raise(new BattleEvent
            {
                kind = BattleEventKind.Damage, side = target.side, move = move, amount = damage, hpAfter = target.Hp,
                effectiveness = effectiveness, shieldAbsorbed = absorbed,
            });
            if (target.Hp > 0) return;

            Raise(new BattleEvent { kind = BattleEventKind.Fainted, side = target.side });
            if (target == wild)
            {
                int xp = BattleMath.XpReward(wild.creature.level, wildIsRare);
                player.creature.xp += xp;
                Raise(new BattleEvent { kind = BattleEventKind.XpGained, side = BattleSide.Player, amount = xp });
                End(BattleResult.Won);
            }
            else End(BattleResult.Lost);
        }

        void End(BattleResult result)
        {
            Result = result;
            Raise(new BattleEvent { kind = BattleEventKind.Ended, result = result });
        }

        void Raise(BattleEvent e)
        {
            _turn.Add(e);
            EventRaised?.Invoke(e);
        }
    }
}
