using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 11a: Lilymere's lagoon level. While unrestored the lagoon is half dry (its outer ring of dry bed tiles shows);
    /// once the lagoon has risen (puzzle <see cref="RisenId"/>, step 11b C) or the realm is restored, those tiles are water
    /// (<see cref="WorldMap.SetRisen"/>). Reads the state every frame, so a save loaded late or the dev panel's restored
    /// toggle is picked up on its own. The map's runtime change is never serialized.
    /// </summary>
    public class LilymereLagoon : MonoBehaviour
    {
        public const string RisenId = "lm.lagoon";

        public IslandBuilder island;
        [Tooltip("WorldMap_Lilymere.")]
        public WorldMap map;
        [Tooltip("Restoration at or above this counts as risen.")]
        [Range(0f, 1f)] public float restoredAt = 0.99f;

        bool _logged;

        void Update()
        {
            if (map == null) return;
            bool showing = island != null && island.world == map;
            // RealmState.Restored is the realm on show; elsewhere only the saved puzzle counts.
            bool risen = PuzzleState.IsSolved(RisenId) || showing && RealmState.Restored >= restoredAt;
            if (risen == map.Risen) return;
            map.SetRisen(risen);
            if (!_logged || risen) Debug.Log($"[LilymereLagoon] The lagoon is {(risen ? "full (dry bed under water)" : "half dry")}.");
            _logged = true;
        }

        void OnDestroy()
        {
            if (map != null) map.SetRisen(false);
        }
    }
}
