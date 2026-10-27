using Oculus.Interaction.Input;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Touch a tile to walk there. Each index fingertip is mapped into the island and through the window to a map
    /// tile (no colliders, works zoomed or not). Within <see cref="hoverHeight"/> of the surface the tile gets a hover
    /// ring (<see cref="TileCursor"/>); dipping to <see cref="dipHeight"/> and lifting again sends the explorer there
    /// (<see cref="GestureTestPiece.GoTo"/>).
    /// Ignored while the explorer can't move (battle), for a hand that is grabbing or near the rim, while a dialogue
    /// bubble is being poked, and while the fingertip moves fast (a reach or a flick, not a touch).
    /// Oct 27: also while a hand puzzle has <see cref="Block"/>ed that hand (a fingertip at a mill's sails, a gust sweep):
    /// the finger dipping onto the grass after a flick no longer walks the explorer.
    /// </summary>
    public class TileTouch : MonoBehaviour
    {
        public IslandBuilder island;
        public GestureTestPiece explorer;
        public TileCursor cursor;
        [Tooltip("Hands that are grabbing or touching the rim are ignored.")]
        public IslandHandle grabGuard;
        [Tooltip("Optional. Poking the bubble never walks.")]
        public DialogueBubble bubble;

        [Tooltip("Hover ring when the fingertip is this close above the surface (m).")]
        public float hoverHeight = 0.03f;
        [Tooltip("The fingertip counts as touching this close to the surface (m).")]
        public float dipHeight = 0.005f;
        [Tooltip("Lift this far above the dip height to confirm (m).")]
        public float liftMargin = 0.008f;
        [Tooltip("Faster than this (m/s) is a reach or a flick: no hover, and a touch in progress is dropped.")]
        public float maxSpeed = 0.6f;
        [Tooltip("This far below the surface (m), the hand is going through the island: the touch is dropped.")]
        public float maxDepth = 0.02f;

        class Finger
        {
            public Vector3 last;
            public bool hasLast;
            public float speed;
            public bool dipped;
            public Vector2Int tile;
            public Vector2Int reachTile = new Vector2Int(int.MinValue, 0);
            public bool reachable;
        }

        readonly Finger[] _fingers = { new Finger(), new Finger() };
        static readonly float[] _blockedUntil = new float[2];

        /// <summary>A hand puzzle owns this hand's fingertip for the next <paramref name="seconds"/>: its touches don't walk.</summary>
        public static void Block(bool leftHand, float seconds)
        {
            int h = leftHand ? 0 : 1;
            _blockedUntil[h] = Mathf.Max(_blockedUntil[h], Time.time + seconds);
        }

        static bool Blocked(bool leftHand) => Time.time < _blockedUntil[leftHand ? 0 : 1];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetBlocks() => _blockedUntil[0] = _blockedUntil[1] = 0f;

        void Update()
        {
            if (island == null || explorer == null || island.map == null) return;
            for (int h = 0; h < 2; h++) UpdateFinger(_fingers[h], h == 0);
        }

        void UpdateFinger(Finger f, bool left)
        {
            var hand = HandRegistry.Get(left ? Handedness.Left : Handedness.Right);
            if (!HandLookup.TryGetJoint(hand, HandJointId.HandIndexTip, out var tip))
            {
                f.hasLast = f.dipped = false;
                return;
            }
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            float speed = f.hasLast ? (tip - f.last).magnitude / dt : 0f;
            f.speed = f.hasLast ? Mathf.Lerp(f.speed, speed, 0.35f) : 0f;
            f.last = tip;
            f.hasLast = true;

            bool busy = grabGuard != null && grabGuard.IsHandBusy(left) || Blocked(left);
            if (!explorer.CanMove || busy || (bubble != null && bubble.IsBeingPoked) || f.speed > maxSpeed)
            {
                if (f.dipped) Debug.Log($"[TileTouch] {(left ? "Left" : "Right")} touch dropped ({(Blocked(left) ? "a hand puzzle has it" : busy ? "near the rim" : f.speed > maxSpeed ? "moving fast" : "busy")}).");
                f.dipped = false;
                return;
            }

            var t = island.transform;
            Vector3 local = t.InverseTransformPoint(tip);
            if (!island.FootprintContains(local))
            {
                f.dipped = false;
                return;
            }
            float height = (local.y - island.CapTopLocal) * t.lossyScale.y;
            if (height < -maxDepth || height > hoverHeight)
            {
                f.dipped = false;
                return;
            }
            var tile = Vector2Int.RoundToInt(island.LocalToTile(local));
            if (island.Content.At(tile.y, tile.x) == IslandMap.Empty) return;

            if (tile != f.reachTile)
            {
                f.reachTile = tile;
                f.reachable = explorer.CanReach(tile);
            }
            cursor.Hover(tile, f.reachable, 1);

            if (!f.dipped)
            {
                if (height <= dipHeight)
                {
                    f.dipped = true;
                    f.tile = tile;
                }
                return;
            }
            f.tile = tile; // sliding while touching picks the tile under the lift
            if (height > dipHeight + liftMargin)
            {
                f.dipped = false;
                bool ok = explorer.GoTo(f.tile);
                Debug.Log($"[TileTouch] {(left ? "Left" : "Right")} touch on c{f.tile.x} r{f.tile.y}: {(ok ? "walking there" : "can't reach")}.");
                f.reachTile = new Vector2Int(int.MinValue, 0);
            }
        }
    }
}
