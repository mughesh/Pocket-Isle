using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// The shard holder (step 9c): a small brass cradle on the island's rim at its back-left corner (as the player sees
    /// it), three cups for the fire, water and wind shards. A shard sits in its cup while held (<see cref="PuzzleState"/>
    /// "shard.fire": stage 1 = held, solved = placed in the statue). Off the map: it stays put while the map scrolls.
    /// </summary>
    public class ShardHolder : MonoBehaviour
    {
        public const string FireId = "shard.fire";

        public IslandBuilder island;
        public PropRecipe holderRecipe;
        public PropRecipe fireShardRecipe;
        [Tooltip("Cups in the holder's shape units (fire, water, wind).")]
        public Vector3[] cups = { new Vector3(0.62f, 0.16f, 0f), new Vector3(0f, 0.16f, 0f), new Vector3(-0.62f, 0.16f, 0f) };
        [Tooltip("Metres per shape unit.")]
        public float metres = 0.026f;
        [Tooltip("Inset from the back-left corner of the footprint (island-local metres: sideways, toward the middle).")]
        public Vector2 inset = new Vector2(0.12f, 0.06f);

        PropView _holder, _fire;

        /// <summary>The fire shard is in its cup (held, not placed yet).</summary>
        public static bool HasFire => PuzzleState.Stage(FireId) >= 1 && !PuzzleState.IsSolved(FireId);

        /// <summary>Hides the cup's shard (a hand or a flight carries it).</summary>
        public bool FireOut { get; set; }

        void Start()
        {
            if (island == null || holderRecipe == null)
            {
                Debug.LogError("[ShardHolder] Needs the island and the holder recipe (Set Up MR_Main (9c keeper)).", this);
                enabled = false;
                return;
            }
            _holder = new PropView("Shard Holder", island, holderRecipe) { restoredSelf = 1f };
            if (fireShardRecipe != null) _fire = new PropView("Shard Holder Fire", island, fireShardRecipe) { restoredSelf = 1f };
        }

        void OnDestroy()
        {
            _holder?.Destroy();
            _fire?.Destroy();
        }

        /// <summary>Island-local point of the holder's origin (on the rim top, back-left).</summary>
        public Vector3 Local
        {
            get
            {
                var half = island.FootprintHalfSize;
                // Island +X is the player's left, -Z the back.
                return new Vector3(half.x - inset.x, island.map.capHeight, -half.y + inset.y);
            }
        }

        /// <summary>Island-local point where cup <paramref name="i"/>'s shard sits.</summary>
        public Vector3 CupLocal(int i) => Local + cups[Mathf.Clamp(i, 0, cups.Length - 1)] * metres + Vector3.up * (0.25f * metres);

        public Vector3 CupWorld(int i) => island.transform.TransformPoint(CupLocal(i));

        void Update()
        {
            if (_holder == null) return;
            bool show = !Opening.InProgress;
            _holder.PlaceLocal(Local, Quaternion.identity, metres, show);
            if (_fire != null)
                _fire.PlaceLocal(CupLocal(0), Quaternion.Euler(0f, Time.time * 40f, 0f), metres * 0.75f, show && HasFire && !FireOut);
        }
    }
}
