using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Diorama
{
    /// <summary>
    /// Growth outside the battle flow: party creatures that are due evolve in their rim pods after a battle (the one on
    /// the pad already evolved there) or after a debug level-up.
    /// Debug (Editor and development builds, also BattleDebug): X = +50 XP to the lead (outside battles; in battle X
    /// switches), L = the lead levels up.
    /// </summary>
    public class GrowthDirector : MonoBehaviour
    {
        public Party party;
        public PodRack pods;
        public EncounterDirector encounters;
        public ProgressionConfig progression;

        bool _evolving;

        void Start()
        {
            if (party == null || pods == null || encounters == null || progression == null)
            {
                Debug.LogError("[GrowthDirector] Needs the party, pod rack, encounter director and progression config " +
                               "(Tools > Pocket Isle > Set Up MR_Main (7 progression)).", this);
                enabled = false;
                return;
            }
            encounters.BattleEnded += OnBattleEnded;
        }

        void OnDestroy()
        {
            if (encounters != null) encounters.BattleEnded -= OnBattleEnded;
        }

        void OnBattleEnded(BattleResult result) => EvolveDue();

        void Update()
        {
            if (!Application.isEditor && !Debug.isDebugBuild) return;
            var kb = Keyboard.current;
            if (kb == null || encounters.InBattle) return;
            if (kb.xKey.wasPressedThisFrame) AddXpToLead(50);
            if (kb.lKey.wasPressedThisFrame) LevelUpLead();
        }

        /// <summary>Debug: XP to the lead creature; any level-ups and a due evolution follow.</summary>
        public void AddXpToLead(int amount)
        {
            var c = party.Lead;
            if (c == null || !enabled) return;
            int was = c.level;
            var ups = Progression.AddXp(c, amount, progression);
            Debug.Log($"[GrowthDirector] Debug: {c.Name} +{amount} XP{(ups.Count > 0 ? $", Lv {was} -> {c.level}" : "")} ({c.xp}/{progression.XpToNext(c.level)}).");
            party.NotifyChanged();
            EvolveDue();
        }

        /// <summary>Debug: exactly enough XP for the lead's next level.</summary>
        public void LevelUpLead()
        {
            var c = party.Lead;
            if (c == null || !enabled) return;
            if (c.level >= progression.levelCap)
            {
                Debug.Log($"[GrowthDirector] Debug: {c.Name} is at the level cap ({progression.levelCap}).");
                return;
            }
            AddXpToLead(progression.XpToNext(c.level) - c.xp);
        }

        /// <summary>Evolves every pod creature that is due, one after another (not during a battle).</summary>
        public void EvolveDue()
        {
            if (!_evolving && !encounters.InBattle && isActiveAndEnabled) StartCoroutine(EvolveAll());
        }

        IEnumerator EvolveAll()
        {
            _evolving = true;
            for (int i = 0; i < Party.Size; i++)
            {
                if (!Progression.CanEvolve(party.At(i))) continue;
                Dialogue.Say("evolve.first");
                yield return pods.EvolveInPod(i);
                party.NotifyChanged();
            }
            _evolving = false;
        }
    }
}
