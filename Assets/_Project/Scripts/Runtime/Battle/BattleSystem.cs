using System;
using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    public enum BattleSide { Player, Wild }

    public enum BattleResult { None, Won, Lost, Fled, Caught }

    public enum BattleEventKind
    {
        MoveUsed, Missed, Damage, Shielded, AttackRaised, Healed, Fainted, FleeFailed, Fled, Ended,
        Switched, CatchAttempt, BrokeFree,
        // Keeper battles (step 9c, KeeperRules): its crown shield broke / rose again, it entered phase 2.
        ShieldBroken, ShieldRaised, KeeperPhase2,
    }

    /// <summary>
    /// One thing that happened in a turn, in order. <see cref="side"/> is the creature it happened to: the user of a
    /// move for MoveUsed / Missed / Shielded / AttackRaised / Healed, the one hit for Damage.
    /// CatchAttempt: <see cref="amount"/> = wobbles, <see cref="result"/> = Caught on success.
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
        /// <summary>Damage: a Keeper's crown shield took most of it ("Blocked!"); <see cref="cracks"/> = cracked crystals now.</summary>
        public bool blocked;
        public int cracks;
        public BattleResult result;

        public override string ToString() => kind switch
        {
            BattleEventKind.MoveUsed => $"{side} used {move.Name}",
            BattleEventKind.Damage => $"{side} took {amount} ({effectiveness}{(shieldAbsorbed ? ", shield" : "")}{(blocked ? $", blocked, {cracks} crack(s)" : "")}), HP {hpAfter}",
            BattleEventKind.Healed => $"{side} healed {amount}, HP {hpAfter}",
            BattleEventKind.AttackRaised => $"{side} attack stage {amount}",
            BattleEventKind.CatchAttempt => $"Catch: {amount} wobble(s), {(result == BattleResult.Caught ? "caught" : "broke free")}",
            BattleEventKind.Ended => $"Ended: {result}",
            _ => $"{kind} ({side})",
        };
    }

    /// <summary>One side of the battle: the creature plus battle-only state (stages, shield).</summary>
    public class Battler
    {
        public readonly BattleSide side;
        public readonly IslingInstance isling;
        public int attackStage;
        public bool shielded;

        public Battler(BattleSide side, IslingInstance isling)
        {
            this.side = side;
            this.isling = isling;
        }

        public MoveData[] Moves => isling.species != null && isling.species.moves != null ? isling.species.moves : Array.Empty<MoveData>();
        public int Hp => isling.currentHp;
        public int MaxHp => isling.MaxHp;
        public float Attack => isling.Attack * BattleMath.StageMultiplier(attackStage);
    }

    /// <summary>
    /// 1v1 wild battle rules, no Unity objects in the loop: turn order by speed, accuracy, damage, support effects,
    /// fainting, flee, catching and switching. Each call plays a whole turn and returns what happened, in order, for
    /// the presentation to animate. The same seed and choices always give the same battle.
    /// When the player's creature faints and <c>hasReserve</c> says another one can fight, the battle goes on:
    /// <see cref="PlayerMustSwitch"/> is set until <see cref="Switch"/> brings the next one in (a free switch).
    /// A pod only has a chance once the wild HP bar is red (<see cref="BattleMath.RedHpShare"/>). When the wild creature faints
    /// it is <see cref="WildDazed"/>: the battle waits for one pod (<see cref="TryCatch"/>, a sure catch, rares 75%) or
    /// <see cref="EndDaze"/> (it fades away: won). XP is not paid here: <see cref="Participants"/> says who fought, and
    /// the encounter pays them after a win (<see cref="Progression"/>).
    /// </summary>
    public class BattleSystem
    {
        public Battler player { get; private set; }
        public readonly Battler wild;
        public readonly bool wildIsRare;
        /// <summary>Keeper battle rules, or null for a wild battle.</summary>
        public readonly KeeperRules keeper;
        readonly TypeChart _chart;
        readonly SeededRandom _rnd;
        readonly Func<bool> _hasReserve;
        readonly List<BattleEvent> _turn = new List<BattleEvent>();
        readonly List<IslingInstance> _participants = new List<IslingInstance>();
        int _failedFlees;

        /// <summary>Every party creature that was out in this battle, in order (fainted ones included).</summary>
        public IReadOnlyList<IslingInstance> Participants => _participants;

        /// <summary>Raised for every event as it happens (the same ones the turn methods return).</summary>
        public event Action<BattleEvent> EventRaised;

        public BattleResult Result { get; private set; }
        public bool IsOver => Result != BattleResult.None;

        /// <summary>The player's creature fainted and another can fight: only <see cref="Switch"/> works until then.</summary>
        public bool PlayerMustSwitch { get; private set; }

        /// <summary>The wild creature fainted and slumps on its pad: only a pod or <see cref="EndDaze"/> now.</summary>
        public bool WildDazed { get; private set; }

        /// <summary>A pod can catch: the wild HP bar is red, or it is dazed (before that a pod always breaks).</summary>
        public bool CatchUnlocked => keeper == null && !IsOver && (WildDazed || BattleMath.CatchUnlocked(wild.Hp, wild.MaxHp));

        /// <param name="hasReserve">True while another party creature (not the one fighting) can still fight.</param>
        /// <param name="keeper">Keeper rules (crown shield, phases, no catching), or null.</param>
        public BattleSystem(IslingInstance player, IslingInstance wild, TypeChart chart, int seed, bool wildIsRare = false,
            Func<bool> hasReserve = null, KeeperRules keeper = null)
        {
            this.player = new Battler(BattleSide.Player, player);
            this.wild = new Battler(BattleSide.Wild, wild);
            this.wildIsRare = wildIsRare;
            this.keeper = keeper;
            _chart = chart;
            _rnd = new SeededRandom(seed);
            _hasReserve = hasReserve;
            _participants.Add(player);
        }

        public Battler Get(BattleSide side) => side == BattleSide.Player ? player : wild;
        Battler Other(Battler b) => b == player ? wild : player;

        /// <summary>The player uses move <paramref name="moveIndex"/>; the wild creature answers. Faster one goes first.</summary>
        public IReadOnlyList<BattleEvent> PlayTurn(int moveIndex)
        {
            _turn.Clear();
            if (IsOver || PlayerMustSwitch || WildDazed) return _turn;
            var moves = player.Moves;
            if (moveIndex < 0 || moveIndex >= moves.Length || moves[moveIndex] == null)
            {
                Debug.LogError($"[BattleSystem] {player.isling.Name} has no move in slot {moveIndex}.");
                return _turn;
            }
            var playerMove = moves[moveIndex];
            var wildMove = PickWildMove();
            bool playerFirst = PlayerGoesFirst();
            var first = playerFirst ? player : wild;
            Act(first, playerFirst ? playerMove : wildMove);
            if (!IsOver) Act(Other(first), playerFirst ? wildMove : playerMove);
            EndKeeperTurn();
            return _turn;
        }

        void EndKeeperTurn()
        {
            if (keeper != null && !IsOver) keeper.EndTurn();
        }

        /// <summary>Tries to run. On a miss the wild creature gets a free move.</summary>
        public IReadOnlyList<BattleEvent> TryFlee()
        {
            _turn.Clear();
            if (IsOver || PlayerMustSwitch || WildDazed) return _turn;
            // A Keeper lets you go: you can always come back and try again.
            float chance = keeper != null ? 1f : BattleMath.FleeChance(player.isling.Speed, wild.isling.Speed, _failedFlees);
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

        /// <summary>The events of the last <see cref="TryCatch"/> (until the next turn method runs).</summary>
        public IReadOnlyList<BattleEvent> LastCatch { get; private set; } = Array.Empty<BattleEvent>();

        /// <summary>Chance that a pod thrown now catches the wild creature.</summary>
        public float CatchChance => WildDazed
            ? BattleMath.DazedCatchChance(wildIsRare)
            : BattleMath.CatchChance(wild.Hp, wild.MaxHp, wildIsRare, wild.isling.level, player.isling.level);

        /// <summary>
        /// A pod hit the wild creature: it wobbles, then it's caught or breaks free (and gets its move; a dazed one runs
        /// off instead, which ends the battle as won). Above red HP (<see cref="CatchUnlocked"/> false) the pod always
        /// breaks after one wobble, and it still uses the turn.
        /// </summary>
        public IReadOnlyList<BattleEvent> TryCatch()
        {
            _turn.Clear();
            LastCatch = _turn;
            if (IsOver || PlayerMustSwitch || keeper != null) return _turn;
            bool locked = !CatchUnlocked;
            float chance = locked ? 0f : CatchChance, roll = _rnd.Next();
            bool caught = roll < chance;
            Raise(new BattleEvent
            {
                kind = BattleEventKind.CatchAttempt, side = BattleSide.Wild, amount = locked ? 1 : BattleMath.CatchWobbles(roll, chance),
                result = caught ? BattleResult.Caught : BattleResult.None,
            });
            if (caught)
            {
                End(BattleResult.Caught);
                return _turn;
            }
            Raise(new BattleEvent { kind = BattleEventKind.BrokeFree, side = BattleSide.Wild });
            if (WildDazed) EndDazeInTurn();
            else Act(wild, PickWildMove());
            return _turn;
        }

        /// <summary>No pod reached the dazed wild creature in time: it fades away and the battle is won.</summary>
        public IReadOnlyList<BattleEvent> EndDaze()
        {
            _turn.Clear();
            if (WildDazed && !IsOver) EndDazeInTurn();
            return _turn;
        }

        void EndDazeInTurn()
        {
            WildDazed = false;
            End(BattleResult.Won);
        }

        /// <summary>
        /// Sends in another party creature. After a faint (<see cref="PlayerMustSwitch"/>) it is free; otherwise it uses
        /// the turn and the wild creature attacks the newcomer. Attack stages and shields don't carry over.
        /// </summary>
        public IReadOnlyList<BattleEvent> Switch(IslingInstance next)
        {
            _turn.Clear();
            if (IsOver || WildDazed || next == null || next.IsFainted || next == player.isling) return _turn;
            bool free = PlayerMustSwitch;
            PlayerMustSwitch = false;
            player = new Battler(BattleSide.Player, next);
            if (!_participants.Contains(next)) _participants.Add(next);
            Raise(new BattleEvent { kind = BattleEventKind.Switched, side = BattleSide.Player });
            if (!free)
            {
                Act(wild, PickWildMove());
                EndKeeperTurn();
            }
            return _turn;
        }

        /// <summary>Faster creature first; a tie is a coin flip.</summary>
        public bool PlayerGoesFirst()
        {
            int p = player.isling.Speed, w = wild.isling.Speed;
            return p != w ? p > w : _rnd.Next() < 0.5f;
        }

        /// <summary>Random by weight: attacks are likely, supports only when they would help.</summary>
        public MoveData PickWildMove()
        {
            var moves = wild.Moves;
            if (keeper != null) return keeper.Pick(moves);
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
            if (move == null || user.isling.IsFainted) return; // no usable move, or fainted earlier this turn
            var target = Other(user);
            Raise(new BattleEvent { kind = BattleEventKind.MoveUsed, side = user.side, move = move });

            if (move.kind == MoveKind.Support)
            {
                switch (move.effect)
                {
                    case SupportEffect.Shield when keeper != null && user == wild:
                        keeper.Raise();
                        Raise(new BattleEvent { kind = BattleEventKind.ShieldRaised, side = user.side, move = move });
                        break;
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
                        user.isling.currentHp += heal;
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
            var effectiveness = _chart != null ? _chart.Matchup(move.element, target.isling.Element) : Effectiveness.Normal;
            float typeMul = _chart != null ? _chart.Multiplier(effectiveness) : 1f;
            float roll = Mathf.Lerp(BattleMath.RollMin, BattleMath.RollMax, _rnd.Next());
            // Phase 2: the Keeper's shot (move 1) hits harder.
            int power = keeper != null && user == wild && keeper.Phase2 && user.Moves.Length > 0 && move == user.Moves[0]
                ? Mathf.RoundToInt(move.power * keeper.phase2Power) : move.power;
            int damage = BattleMath.Damage(user.isling.level, power, user.Attack, target.isling.Defense, typeMul, roll);
            bool absorbed = target.shielded;
            if (absorbed)
            {
                damage = Mathf.Max(1, damage / 2);
                target.shielded = false;
            }
            bool blocked = false, broke = false;
            if (keeper != null && target == wild && keeper.Shielded)
            {
                if (move.element == keeper.breaksWith) broke = true;
                else
                {
                    blocked = true;
                    damage = Mathf.Max(1, Mathf.RoundToInt(damage * keeper.blockedShare));
                    keeper.Cracks++;
                    broke = keeper.Cracks >= keeper.hitsToBreak;
                }
                if (broke) keeper.Break();
            }
            target.isling.currentHp = Mathf.Max(0, target.Hp - damage);
            Raise(new BattleEvent
            {
                kind = BattleEventKind.Damage, side = target.side, move = move, amount = damage, hpAfter = target.Hp,
                effectiveness = effectiveness, shieldAbsorbed = absorbed, blocked = blocked, cracks = keeper != null ? keeper.Cracks : 0,
            });
            if (broke) Raise(new BattleEvent { kind = BattleEventKind.ShieldBroken, side = target.side, move = move });
            if (keeper != null && target == wild && !keeper.Phase2 && target.Hp > 0 && target.Hp <= target.MaxHp * keeper.phase2At)
            {
                keeper.Phase2 = true;
                Raise(new BattleEvent { kind = BattleEventKind.KeeperPhase2, side = target.side });
            }
            if (target.Hp > 0) return;

            Raise(new BattleEvent { kind = BattleEventKind.Fainted, side = target.side });
            if (target == wild && keeper != null) End(BattleResult.Won);
            else if (target == wild) WildDazed = true;
            else if (_hasReserve != null && _hasReserve()) PlayerMustSwitch = true;
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
