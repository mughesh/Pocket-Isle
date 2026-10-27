using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 12b (Mugesh loved the lotus garden's view settle): when the explorer comes within reach of any hand moment,
    /// the island window glides to frame that moment's pieces (<see cref="HandPuzzle.FramePoints"/>) and the explorer,
    /// and holds while it's active (the explorer can still walk inside it); it lets go when the moment is solved, the
    /// explorer walks out of reach, or a battle takes the view. Too far apart for one window: plain following.
    /// </summary>
    public class FocusLock : MonoBehaviour
    {
        public GestureTestPiece explorer;
        public IslandBuilder island;
        public WorldWindow window;
        [Tooltip("Tiles of margin kept inside the window (its round corners).")]
        public float margin = 2.5f;
        [Tooltip("The held centre moves when the framing changes by more than this (tiles).")]
        public float moveAfter = 0.25f;

        readonly List<Vector2> _points = new List<Vector2>();
        HandPuzzle[] _pieces;
        float _piecesAt = -99f;
        HandPuzzle _holding;
        Vector2 _at;

        /// <summary>The moment the view holds now (null = following the explorer).</summary>
        public HandPuzzle Holding => _holding;

        void LateUpdate()
        {
            if (window == null || explorer == null || island == null || island.map == null) return;
            if (_pieces == null || Time.unscaledTime - _piecesAt > 3f)
            {
                _pieces = FindObjectsByType<HandPuzzle>(FindObjectsSortMode.None);
                _piecesAt = Time.unscaledTime;
            }
            HandPuzzle best = null;
            Vector2 focus = default;
            float bestDist = float.MaxValue;
            bool calm = !Opening.InProgress && !Onboarding.Running && !explorer.Locked && !SceneSwap.IsBusy;
            if (calm)
                foreach (var p in _pieces)
                {
                    if (p == null || !p.FocusLockable || !p.ActiveHere || !p.InReach(explorer.Tile)) continue;
                    _points.Clear();
                    p.FramePoints(_points);
                    _points.Add(explorer.Tile);
                    Vector2 lo = _points[0], hi = lo;
                    foreach (var q in _points)
                    {
                        lo = Vector2.Min(lo, q);
                        hi = Vector2.Max(hi, q);
                    }
                    float room = Mathf.Min(island.map.Columns, island.map.Rows) / Mathf.Max(1f, island.Zoom) - margin;
                    if (hi.x - lo.x > room || hi.y - lo.y > room) continue;
                    var home = p.Home;
                    float d = (home - (Vector2)explorer.Tile).sqrMagnitude;
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = p;
                        focus = (lo + hi) * 0.5f;
                    }
                }
            if (best != null)
            {
                if (_holding != best || (focus - _at).sqrMagnitude > moveAfter * moveAfter)
                {
                    if (_holding != best) Debug.Log($"[FocusLock] Holding the view on {best.id}.");
                    window.SetFocus(focus);
                    _at = focus;
                    _holding = best;
                }
            }
            else if (_holding != null)
            {
                Debug.Log($"[FocusLock] Letting go of {(_holding != null ? _holding.id : "?")}.");
                _holding = null;
                // A battle owns the view while the explorer is locked; it clears the focus itself.
                if (!explorer.Locked) window.SetFocus(null);
            }
        }
    }
}
