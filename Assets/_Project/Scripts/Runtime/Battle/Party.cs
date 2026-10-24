using System;
using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// The player's creatures: up to <see cref="Size"/> in the rim pods (slot order = pod order) plus a storage list for
    /// catches that don't fit (the Sanctuary later). Lead = the first pod creature that can still fight.
    /// Lives in the scene (not an asset), so Play mode changes don't stick.
    /// </summary>
    public class Party : MonoBehaviour
    {
        public const int Size = 6;

        [Tooltip("The rim pods, in order (at most 6).")]
        public List<CreatureInstance> members = new List<CreatureInstance>();
        [Tooltip("Caught while the pods were full.")]
        public List<CreatureInstance> storage = new List<CreatureInstance>();

        /// <summary>A creature joined (pods or storage) or the party was healed.</summary>
        public event Action Changed;

        /// <summary>The first creature that can still fight, or null.</summary>
        public CreatureInstance Lead
        {
            get
            {
                int i = LeadSlot;
                return i >= 0 ? members[i] : null;
            }
        }

        /// <summary>Pod index of <see cref="Lead"/>, or -1.</summary>
        public int LeadSlot
        {
            get
            {
                for (int i = 0; i < members.Count && i < Size; i++)
                    if (CanFight(i)) return i;
                return -1;
            }
        }

        public bool IsFull => members.Count >= Size;

        /// <summary>True when no pod creature can fight.</summary>
        public bool AllFainted => LeadSlot < 0;

        /// <summary>The creature in pod <paramref name="slot"/>, or null.</summary>
        public CreatureInstance At(int slot) => slot >= 0 && slot < members.Count && slot < Size ? members[slot] : null;

        public bool CanFight(int slot)
        {
            var c = At(slot);
            return c != null && c.species != null && !c.IsFainted;
        }

        /// <summary>Pod index of a party creature, or -1.</summary>
        public int SlotOf(CreatureInstance creature) => creature == null ? -1 : members.IndexOf(creature);

        /// <summary>The first creature after <paramref name="except"/> that can fight (wraps), or -1.</summary>
        public int NextFighter(int except)
        {
            for (int k = 1; k <= Size; k++)
            {
                int i = (except + k + Size) % Size;
                if (i != except && CanFight(i)) return i;
            }
            return CanFight(except) ? except : -1;
        }

        /// <summary>Adds a caught creature to the first free pod (returns its index) or to storage (returns -1).</summary>
        public int Add(CreatureInstance creature)
        {
            if (creature == null) return -1;
            int slot = -1;
            if (!IsFull)
            {
                members.Add(creature);
                slot = members.Count - 1;
            }
            else storage.Add(creature);
            Changed?.Invoke();
            return slot;
        }

        void Awake()
        {
            if (members.Count == 0) Debug.LogError("[Party] No creatures in the party; battles can't start.", this);
            if (members.Count > Size) Debug.LogError($"[Party] {members.Count} creatures in the pods; only {Size} fit (the rest are ignored).", this);
            HealAll();
        }

        public void HealAll()
        {
            foreach (var c in members)
                if (c != null && c.species != null) c.Heal();
            Changed?.Invoke();
        }
    }
}
