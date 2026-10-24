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

        /// <summary>
        /// Swaps storage creature <paramref name="storageIndex"/> with pod <paramref name="slot"/>; a slot just past the
        /// last filled pod adds it to the pods instead (it leaves storage). The pods never end up empty.
        /// </summary>
        public bool SwapWithStorage(int storageIndex, int slot)
        {
            if (storageIndex < 0 || storageIndex >= storage.Count || slot < 0 || slot >= Size || slot > members.Count) return false;
            var incoming = storage[storageIndex];
            if (incoming == null || incoming.species == null) return false;
            if (slot == members.Count)
            {
                storage.RemoveAt(storageIndex);
                members.Add(incoming);
            }
            else
            {
                storage[storageIndex] = members[slot];
                members[slot] = incoming;
            }
            Changed?.Invoke();
            return true;
        }

        /// <summary>Moves pod <paramref name="slot"/>'s creature to storage. Refused for the last creature in the pods.</summary>
        public bool Deposit(int slot)
        {
            if (members.Count <= 1 || slot < 0 || slot >= members.Count) return false;
            storage.Add(members[slot]);
            members.RemoveAt(slot);
            Changed?.Invoke();
            return true;
        }

        /// <summary>Replaces everything (loading a save).</summary>
        public void Restore(List<CreatureInstance> pods, List<CreatureInstance> stored)
        {
            members = pods ?? new List<CreatureInstance>();
            storage = stored ?? new List<CreatureInstance>();
            Changed?.Invoke();
        }

        /// <summary>Heals the pods and storage (a Hearth Shrine).</summary>
        public void HealEveryone()
        {
            foreach (var c in storage)
                if (c != null && c.species != null) c.Heal();
            HealAll();
        }

        /// <summary>Tells listeners a creature changed in place (XP, level, evolution).</summary>
        public void NotifyChanged() => Changed?.Invoke();

        public void HealAll()
        {
            foreach (var c in members)
                if (c != null && c.species != null) c.Heal();
            Changed?.Invoke();
        }
    }
}
