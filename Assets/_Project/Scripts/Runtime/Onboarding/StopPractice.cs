using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 27 C: the rules of the swipe-and-tap practice (pure, tested). A few gold tiles, each further along than the last;
    /// a thumb tap that stops the walk on or next to the tile counts; walking past just places the next try ahead.
    /// </summary>
    public static class StopPractice
    {
        /// <summary>The fewest tiles ahead worth a target (step 28: a real run-up, time to see it coming and tap).</summary>
        public const int Nearest = 4;

        /// <summary>Good stops that pass the practice: three, two in the Quick Journey (the judges' path).</summary>
        public static int Goal(bool quick) => quick ? 2 : 3;

        /// <summary>Tiles along the walk to the next target, after this many good stops: 4, 5, 6.</summary>
        public static int Distance(int stops) => Nearest + Mathf.Max(0, stops);

        /// <summary>Step 28: the target lies straight on along <paramref name="way"/> from <paramref name="tile"/>.</summary>
        public static bool StraightAhead(Vector2Int tile, Vector2Int way, Vector2Int target)
        {
            var d = target - tile;
            return way.sqrMagnitude == 1 && d.x * way.y == d.y * way.x && d.x * way.x + d.y * way.y > 0;
        }

        /// <summary>Tiles between two tiles, diagonals count one.</summary>
        public static int Distance(Vector2Int a, Vector2Int b) => Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));

        /// <summary>On the tile or next to it.</summary>
        public static bool Landed(Vector2Int tile, Vector2Int target) => Distance(tile, target) <= 1;

        /// <summary>Two tiles further away than the closest it got (past it, or off the other way).</summary>
        public static bool WalkedPast(int distance, int closest) => distance >= closest + 2;
    }
}
