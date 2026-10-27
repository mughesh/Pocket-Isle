using System;
using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Where collected shards live (step 9c; step 9e after Mugesh's test: with the Lantern Spirit, not on a rim
    /// holder). A held shard circles the Spirit's lantern, small; several held shards share the circle evenly (step 10e).
    /// Near its statue socket (<see cref="SetOffering"/>, set by each <see cref="StatueSocket"/>) the Spirit hands it over:
    /// it floats out to a spot in front of the player, big enough to grab easily, and shrinks back as it nears the socket.
    /// <see cref="PuzzleState"/> per shard ("shard.fire", "shard.wind"): stage 1 = held, solved = placed in the statue.
    /// Off the map: they stay with the Spirit while the map scrolls.
    /// </summary>
    public class ShardHolder : MonoBehaviour
    {
        public const string FireId = "shard.fire";
        public const string WindId = "shard.wind";

        [Serializable]
        public class Shard
        {
            [Tooltip("Saved id (stage 1 = held, solved = placed).")]
            public string id;
            public PropRecipe recipe;
        }

        public IslandBuilder island;
        [Tooltip("The shards ride with it.")]
        public LanternSpirit spirit;
        [Tooltip("Cinderwood's fire shard (shard.fire).")]
        public PropRecipe fireShardRecipe;
        [Tooltip("Step 10e: the other realms' shards, in circle order after the fire shard.")]
        public Shard[] otherShards = new Shard[0];
        [Tooltip("Metres per shard shape unit while it rides with the Spirit (small) and while offered (big, easy to grab).")]
        public float carryMetres = 0.022f;
        public float offerMetres = 0.05f;
        [Tooltip("Circle round the lantern (island-local metres): radius, height above the Spirit's origin.")]
        public float orbitRadius = 0.035f;
        public float orbitHeight = 0.03f;
        [Tooltip("Where an offered shard floats: island-local x / z share of the half footprint (z toward the player), height above the cap (m).")]
        public Vector3 offerSpot = new Vector3(0.15f, 0.12f, 0.6f);
        [Tooltip("Two shards offered at once: x share of the half footprint between them.")]
        public float offerSpread = 0.3f;

        class Entry
        {
            public string id;
            public PropView view;
            public bool outNow, offering;
            public float offer;
        }

        readonly List<Entry> _shards = new List<Entry>();
        readonly List<string> _order = new List<string>();

        /// <summary>Shard <paramref name="id"/> is held (with the Spirit, not placed yet).</summary>
        public static bool Holds(string id) => PuzzleState.Stage(id) >= 1 && !PuzzleState.IsSolved(id);

        /// <summary>The fire shard is held (with the Spirit, not placed yet).</summary>
        public static bool HasFire => Holds(FireId);

        /// <summary>
        /// Shard <paramref name="id"/>'s place on the lantern circle: the held shards in <paramref name="order"/> (and
        /// <paramref name="id"/> itself, flying in) share it evenly. <paramref name="count"/> = how many share it.
        /// </summary>
        public static int CircleSlot(string id, IReadOnlyList<string> order, out int count)
        {
            int slot = 0;
            count = 0;
            foreach (var o in order)
            {
                if (o != id && !Holds(o)) continue;
                if (o == id) slot = count;
                count++;
            }
            if (count == 0) count = 1;
            return slot;
        }

        /// <summary>Hides the Spirit's fire shard (a hand or a flight carries it).</summary>
        public bool FireOut { get => IsOut(FireId); set => SetOut(FireId, value); }

        /// <summary>The Spirit offers the fire shard (near its statue).</summary>
        public bool Offering { get => Find(FireId)?.offering ?? false; set => SetOffering(FireId, value); }

        /// <summary>0 = the fire shard is with the Spirit, 1 = fully offered (eases between them).</summary>
        public float OfferAmount => OfferAmountOf(FireId);

        /// <summary>The fire shard's size (metres per shape unit) right now.</summary>
        public float ShardMetres => ShardMetresOf(FireId);

        void Awake()
        {
            _shards.Add(new Entry { id = FireId });
            if (otherShards != null)
                foreach (var s in otherShards)
                    if (s != null && !string.IsNullOrEmpty(s.id) && Find(s.id) == null) _shards.Add(new Entry { id = s.id });
            foreach (var e in _shards) _order.Add(e.id);
        }

        void Start()
        {
            if (island == null || fireShardRecipe == null)
            {
                Debug.LogError("[ShardHolder] Needs the island and the shard recipe (Set Up MR_Main (9e dev panel and fury)).", this);
                enabled = false;
                return;
            }
            if (spirit == null) Debug.LogError("[ShardHolder] No Lantern Spirit: held shards float over the island's near-left corner.", this);
            foreach (var e in _shards)
            {
                var recipe = e.id == FireId ? fireShardRecipe : RecipeOf(e.id);
                if (recipe == null)
                {
                    Debug.LogError($"[ShardHolder] No recipe for {e.id}.", this);
                    continue;
                }
                e.view = new PropView($"{e.id} (with the Spirit)", island, recipe) { restoredSelf = 1f };
            }
        }

        void OnDestroy()
        {
            foreach (var e in _shards) e.view?.Destroy();
        }

        PropRecipe RecipeOf(string id)
        {
            if (otherShards != null)
                foreach (var s in otherShards)
                    if (s != null && s.id == id) return s.recipe;
            return null;
        }

        Entry Find(string id)
        {
            foreach (var e in _shards)
                if (e.id == id) return e;
            return null;
        }

        public bool IsOut(string id) => Find(id)?.outNow ?? false;

        /// <summary>Hides the Spirit's shard <paramref name="id"/> (a hand or a flight carries it).</summary>
        public void SetOut(string id, bool value)
        {
            var e = Find(id);
            if (e != null) e.outNow = value;
        }

        /// <summary>The Spirit offers shard <paramref name="id"/> (near its socket): it floats out in front of the player, big.</summary>
        public void SetOffering(string id, bool value)
        {
            var e = Find(id);
            if (e != null) e.offering = value;
        }

        public float OfferAmountOf(string id) => Find(id)?.offer ?? 0f;

        /// <summary>Shard <paramref name="id"/>'s size (metres per shape unit) right now.</summary>
        public float ShardMetresOf(string id) => Mathf.Lerp(carryMetres, offerMetres, OfferAmountOf(id));

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

        Vector3 OfferLocal(float x)
        {
            var half = island.FootprintHalfSize;
            return new Vector3(half.x * x, island.CapTopLocal + offerSpot.y, half.y * offerSpot.z);
        }

        /// <summary>
        /// Island-local point where shard <paramref name="id"/> is now: circling the lantern (held shards share the circle
        /// evenly; a shard flying in already takes its place) or offered.
        /// </summary>
        public Vector3 CupLocal(string id)
        {
            int offered = 0, offerSlot = 0;
            foreach (var e in _shards)
            {
                if (e.offer > 0f)
                {
                    if (e.id == id) offerSlot = offered;
                    offered++;
                }
            }
            int slot = CircleSlot(id, _order, out int count);
            float a = Time.time * 1.6f + slot * Mathf.PI * 2f / count;
            Vector3 orbit = LanternLocal + new Vector3(Mathf.Cos(a), 0.2f * Mathf.Sin(a * 2f), Mathf.Sin(a)) * orbitRadius;
            float x = offerSpot.x + (offerSlot - (offered - 1) * 0.5f) * offerSpread;
            Vector3 offer = OfferLocal(x) + Vector3.up * (0.006f * Mathf.Sin(Time.time * 2f));
            float k = Mathf.SmoothStep(0f, 1f, OfferAmountOf(id));
            return Vector3.Lerp(orbit, offer, k) + Vector3.up * (0.04f * Mathf.Sin(k * Mathf.PI));
        }

        public Vector3 CupWorld(string id) => island.transform.TransformPoint(CupLocal(id));

        void Update()
        {
            foreach (var e in _shards)
            {
                if (e.view == null) continue;
                bool held = Holds(e.id);
                e.offer = Mathf.MoveTowards(e.offer, e.offering && held ? 1f : 0f, Time.deltaTime / 0.8f);
                bool show = !Opening.InProgress && held && !e.outNow && (spirit == null || spirit.Appear > 0.5f);
                e.view.PlaceLocal(CupLocal(e.id), Quaternion.Euler(0f, Time.time * 60f, 0f), ShardMetresOf(e.id), show);
            }
        }
    }
}
