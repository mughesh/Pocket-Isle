using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>Nov 15 notes, flat Play probes. Bridge: exec:Diorama.EditorTools.Nov15Debug.MillView.</summary>
    public static class Nov15Debug
    {
        /// <summary>What the view holds by the big mill: the focus lock's moment, its frame points, and whether the mill and the drawbridge show.</summary>
        public static void MillView()
        {
            var lockr = Object.FindFirstObjectByType<FocusLock>();
            var mill = Object.FindFirstObjectByType<BigMill>();
            var window = Object.FindFirstObjectByType<WorldWindow>();
            var e = Object.FindFirstObjectByType<GestureTestPiece>();
            if (lockr == null || mill == null || window == null || e == null)
            {
                Debug.LogError("[Nov15Debug] Needs Play with the focus lock, the big mill, the window and the explorer.");
                return;
            }
            var pts = new List<Vector2>();
            mill.wheel.FramePoints(pts);
            string Shows(Vector2Int t) => window.Shows(t) ? "shows" : "OUT";
            var b = mill.bridge;
            Debug.Log($"[Nov15Debug] Mill view: explorer c{e.Tile.x} r{e.Tile.y}, holding {(lockr.Holding != null ? lockr.Holding.id : "none")}, " +
                      $"mill solved {mill.wheel.IsSolved}, active {mill.wheel.ActiveHere}, in reach {mill.wheel.InReach(e.Tile)}; " +
                      $"frame {string.Join(" ", pts.Select(p => $"({p.x:0.#},{p.y:0.#})"))}; " +
                      $"mill c{mill.wheel.anchor.x} r{mill.wheel.anchor.y} {Shows(mill.wheel.anchor)}, bridge c{b.x} r{b.y} {Shows(b)}, c{b.x + 1} r{b.y + 1} {Shows(b + Vector2Int.one)}, " +
                      $"bridge far edge r{b.y - 1} {Shows(new Vector2Int(b.x, b.y - 1))}.");
        }
    }
}
