using Oculus.Interaction.Input;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Look at a tile and pinch to walk there, for reclined players with the island further away. The gaze ray
    /// (<see cref="HeadGaze"/>: head forward pitched down) hits the island top; the tile gets a hover ring and a
    /// reticle (<see cref="TileCursor"/>, below a fingertip hover in priority), and a pinch on either hand sends the explorer there (<see cref="GestureTestPiece.GoTo"/>).
    /// A dialogue bubble under the gaze takes the pinch instead (bubble, then runes, then tiles; the runes only exist
    /// in battle, when this is off). MicrogestureInput drops the thumb tap a pinch can also produce.
    /// </summary>
    public class TileGaze : MonoBehaviour
    {
        public IslandBuilder island;
        public GestureTestPiece explorer;
        public TileCursor cursor;
        [Tooltip("Pinches from a hand that is grabbing or touching the rim are ignored.")]
        public IslandHandle grabGuard;
        [Tooltip("Optional. A bubble under the gaze takes the pinch.")]
        public DialogueBubble bubble;
        [Tooltip("Optional. A rim pod under the gaze takes the pinch.")]
        public PodRack pods;

        readonly Hand[] _hands = new Hand[2];
        readonly bool[] _wasPinching = { true, true };
        Vector2Int _reachTile = new Vector2Int(int.MinValue, 0);
        bool _reachable;

        void Update()
        {
            if (island == null || explorer == null || island.map == null) return;
            bool onTile = TryGetGazeTile(out var tile, out var hit);
            bool bubbleFirst = bubble != null && bubble.IsGazed || pods != null && pods.IsGazed;
            bool usable = onTile && !bubbleFirst && explorer.CanMove;
            if (usable)
            {
                if (tile != _reachTile)
                {
                    _reachTile = tile;
                    _reachable = explorer.CanReach(tile);
                }
                cursor.Hover(tile, _reachable, 0);
                cursor.Reticle(hit);
            }

            for (int h = 0; h < 2; h++)
            {
                var hand = HandLookup.Refresh(ref _hands[h], h == 0 ? Handedness.Left : Handedness.Right);
                bool pinching = hand != null && hand.IsTrackedDataValid && hand.GetIndexFingerIsPinching();
                bool busy = grabGuard != null && grabGuard.IsHandBusy(h == 0);
                if (pinching && !_wasPinching[h] && !busy && usable)
                {
                    bool ok = explorer.GoTo(tile);
                    Debug.Log($"[TileGaze] Gaze + pinch ({(h == 0 ? "left" : "right")}) on c{tile.x} r{tile.y}: {(ok ? "walking there" : "can't reach")}.");
                    _reachTile = new Vector2Int(int.MinValue, 0);
                }
                _wasPinching[h] = pinching;
            }
        }

        /// <summary>The map tile under the gaze ray on the island top, and the island-local hit point.</summary>
        bool TryGetGazeTile(out Vector2Int tile, out Vector3 hit)
        {
            tile = default;
            hit = default;
            if (!HeadGaze.TryGetRay(out var ray)) return false;
            var t = island.transform;
            Vector3 o = t.InverseTransformPoint(ray.origin);
            Vector3 d = t.InverseTransformDirection(ray.direction);
            float top = island.CapTopLocal;
            if (d.y > -1e-4f || o.y <= top) return false;
            hit = o + d * ((top - o.y) / d.y);
            if (!island.FootprintContains(hit)) return false;
            tile = Vector2Int.RoundToInt(island.LocalToTile(hit));
            return island.Content.At(tile.y, tile.x) != IslandMap.Empty;
        }
    }
}
