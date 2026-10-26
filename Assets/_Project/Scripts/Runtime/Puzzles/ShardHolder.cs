using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Where collected shards live (step 9c; step 9e after Mugesh's test: with the Lantern Spirit, not on a rim
    /// holder). A held shard circles the Spirit's lantern, small. Near its statue (<see cref="Offering"/>, set by
    /// <see cref="StatueSocket"/>) the Spirit hands it over: it floats out to a spot in front of the player, big enough to
    /// grab easily, and shrinks back as it nears the socket. <see cref="PuzzleState"/> "shard.fire": stage 1 = held,
    /// solved = placed in the statue. Off the map: it stays with the Spirit while the map scrolls.
    /// </summary>
    public class ShardHolder : MonoBehaviour
    {
        public const string FireId = "shard.fire";

        public IslandBuilder island;
        [Tooltip("The shards ride with it.")]
        public LanternSpirit spirit;
        public PropRecipe fireShardRecipe;
        [Tooltip("Metres per shard shape unit while it rides with the Spirit (small) and while offered (big, easy to grab).")]
        public float carryMetres = 0.022f;
        public float offerMetres = 0.05f;
        [Tooltip("Circle round the lantern (island-local metres): radius, height above the Spirit's origin.")]
        public float orbitRadius = 0.035f;
        public float orbitHeight = 0.03f;
        [Tooltip("Where an offered shard floats: island-local x / z share of the half footprint (z toward the player), height above the cap (m).")]
        public Vector3 offerSpot = new Vector3(0.15f, 0.12f, 0.6f);

        PropView _fire;
        float _offer;

        /// <summary>The fire shard is held (with the Spirit, not placed yet).</summary>
        public static bool HasFire => PuzzleState.Stage(FireId) >= 1 && !PuzzleState.IsSolved(FireId);

        /// <summary>Hides the Spirit's shard (a hand or a flight carries it).</summary>
        public bool FireOut { get; set; }

        /// <summary>The Spirit offers the shard (near its statue): it floats out in front of the player, big.</summary>
        public bool Offering { get; set; }

        /// <summary>0 = with the Spirit, 1 = fully offered (eases between them).</summary>
        public float OfferAmount => _offer;

        /// <summary>The shard's size (metres per shape unit) right now.</summary>
        public float ShardMetres => Mathf.Lerp(carryMetres, offerMetres, _offer);

        void Start()
        {
            if (island == null || fireShardRecipe == null)
            {
                Debug.LogError("[ShardHolder] Needs the island and the shard recipe (Set Up MR_Main (9e dev panel and fury)).", this);
                enabled = false;
                return;
            }
            if (spirit == null) Debug.LogError("[ShardHolder] No Lantern Spirit: held shards float over the island's near-left corner.", this);
            _fire = new PropView("Fire Shard (with the Spirit)", island, fireShardRecipe) { restoredSelf = 1f };
        }

        void OnDestroy() => _fire?.Destroy();

        /// <summary>Island-local point of the Spirit's lantern (or the near-left corner without a Spirit).</summary>
        Vector3 LanternLocal
        {
            get
            {
                if (spirit != null) return island.transform.InverseTransformPoint(spirit.transform.position) + Vector3.up * orbitHeight;
                var half = island.FootprintHalfSize;
                return new Vector3(half.x - 0.05f, island.CapTopLocal + 0.08f, half.y - 0.04f);
            }
        }

        Vector3 OfferLocal
        {
            get
            {
                var half = island.FootprintHalfSize;
                return new Vector3(half.x * offerSpot.x, island.CapTopLocal + offerSpot.y, half.y * offerSpot.z);
            }
        }

        /// <summary>Island-local point where shard <paramref name="i"/> is now (circling the lantern, or offered).</summary>
        public Vector3 CupLocal(int i)
        {
            float a = Time.time * 1.6f + i * 2.1f;
            Vector3 orbit = LanternLocal + new Vector3(Mathf.Cos(a), 0.2f * Mathf.Sin(a * 2f), Mathf.Sin(a)) * orbitRadius;
            Vector3 offer = OfferLocal + Vector3.up * (0.006f * Mathf.Sin(Time.time * 2f));
            float k = Mathf.SmoothStep(0f, 1f, _offer);
            return Vector3.Lerp(orbit, offer, k) + Vector3.up * (0.04f * Mathf.Sin(k * Mathf.PI));
        }

        public Vector3 CupWorld(int i) => island.transform.TransformPoint(CupLocal(i));

        void Update()
        {
            if (_fire == null) return;
            _offer = Mathf.MoveTowards(_offer, Offering && HasFire ? 1f : 0f, Time.deltaTime / 0.8f);
            bool show = !Opening.InProgress && HasFire && !FireOut && (spirit == null || spirit.Appear > 0.5f);
            _fire.PlaceLocal(CupLocal(0), Quaternion.Euler(0f, Time.time * 60f, 0f), ShardMetres, show);
        }
    }
}
