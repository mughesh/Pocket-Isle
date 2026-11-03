using System.Collections;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 13 D: after the finale, poke a calm Keeper in its hall: "Spar?" / "Not now". A friendly match five levels
    /// over the last fight (<see cref="FinaleLogic.RematchLevel"/>), playful: no fury aura (the hall only dresses its own
    /// story battle), a softer shield (two hits break it), XP as usual, no shard, and losing doesn't send you to the shrine.
    /// Win or lose, it bows. Added to every <see cref="KeeperHall"/> at start by <see cref="FinaleDirector"/>.
    /// </summary>
    public class KeeperRematch : MonoBehaviour
    {
        public KeeperHall hall;
        public EncounterDirector encounters;
        public RuneBoard runes;

        bool _asking, _fighting, _bowing;

        public bool Fighting => _fighting;

        void OnEnable()
        {
            if (hall != null) hall.Snorted += OnSnorted;
        }

        void Start()
        {
            // Added at runtime: OnEnable ran before the fields were set.
            if (hall != null)
            {
                hall.Snorted -= OnSnorted;
                hall.Snorted += OnSnorted;
            }
        }

        void OnDisable()
        {
            if (hall != null) hall.Snorted -= OnSnorted;
            if (encounters != null) encounters.BattleEnded -= OnEnded;
        }

        void OnSnorted()
        {
            if (_asking || _fighting || _bowing || hall == null || !hall.Calmed || !PuzzleState.IsSolved(FinaleLogic.DoneId)) return;
            if (encounters == null || encounters.InBattle || runes == null || FinaleDirector.Running) return;
            Ask();
        }

        void Ask()
        {
            _asking = true;
            Dialogue.Say("keeper.spar");
            runes.Clear();
            var pair = runes.Row(0f, new RuneBoard.Item("Spar?", Spar), new RuneBoard.Item("Not now", NotNow));
            var yes = pair[0];
            runes.DefaultFocus = yes;
            runes.Show();
            Debug.Log($"[KeeperRematch] {Name}: Spar? / Not now.");
        }

        string Name => hall != null && hall.keeperSpecies != null ? hall.keeperSpecies.displayName : "the Keeper";

        void NotNow()
        {
            runes.Hide();
            runes.Clear();
            _asking = false;
            Debug.Log($"[KeeperRematch] {Name}: not now.");
        }

        /// <summary>The friendly match (also the bridge).</summary>
        public void Spar()
        {
            if (runes != null)
            {
                runes.Hide();
                runes.Clear();
            }
            _asking = false;
            if (hall == null || hall.keeperSpecies == null || encounters == null) return;
            int last = ShardScaling.KeeperLevel(hall.keeperLevel, hall.ownShard);
            int level = FinaleLogic.RematchLevel(last);
            var fight = new EncounterDirector.KeeperFight
            {
                keeper = new IslingInstance(hall.keeperSpecies, level),
                keeperPad = hall.keeperPad,
                myPad = hall.partyPad,
                caption = $"{Name} wants a friendly match!",
                rules = new KeeperRules { hitsToBreak = 2 },
                friendly = true,
            };
            if (!encounters.StartKeeper(fight))
            {
                Debug.LogWarning($"[KeeperRematch] {Name}: the match didn't start (a battle on, an empty party or a zoom).");
                return;
            }
            _fighting = true;
            encounters.BattleEnded += OnEnded;
            Debug.Log($"[KeeperRematch] A friendly match with {Name} Lv {level} (last fight Lv {last}).");
        }

        void OnEnded(BattleResult result)
        {
            encounters.BattleEnded -= OnEnded;
            _fighting = false;
            Debug.Log($"[KeeperRematch] The friendly match ended ({result}): {Name} bows.");
            StartCoroutine(Bow());
        }

        IEnumerator Bow()
        {
            _bowing = true;
            yield return new WaitForSeconds(2.2f);
            if (hall != null) hall.Snort();
            Dialogue.Say("keeper.spar.bow");
            yield return new WaitForSeconds(2f);
            _bowing = false;
        }
    }
}
