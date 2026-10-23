using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Temporary party for step 5: one Kilnpot that fights every battle. Step 6 replaces it with the six rim pods.
    /// Lives in the scene (not an asset), so Play mode changes don't stick.
    /// </summary>
    public class Party : MonoBehaviour
    {
        public List<CreatureInstance> members = new List<CreatureInstance>();

        /// <summary>The first creature that can still fight, or null.</summary>
        public CreatureInstance Lead
        {
            get
            {
                foreach (var c in members)
                    if (c != null && c.species != null && !c.IsFainted) return c;
                return null;
            }
        }

        void Awake()
        {
            if (members.Count == 0) Debug.LogError("[Party] No creatures in the party; battles can't start.", this);
            HealAll();
        }

        public void HealAll()
        {
            foreach (var c in members)
                if (c != null && c.species != null) c.Heal();
        }
    }
}
