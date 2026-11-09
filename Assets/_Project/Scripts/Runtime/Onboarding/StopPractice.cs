using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 27 C: the rules of the swipe-and-tap practice (pure, tested). A few gold tiles, each further along than the last;
    /// a thumb tap that stops the walk on or next to the tile counts; walking past just places the next try ahead.
    /// </summary>
    public static class StopPractice
    {
        /// <summary>The fewest tiles ahead worth a target.</summary>
        public const int Nearest = 2;

        /// <summary>Good stops that pass the practice: three, two in the Quick Journey (the judges' path).</summary>
        public static int Goal(bool quick) => quick ? 2 : 3;

        /// <summary>Tiles along the path to the next target, after this many good stops: 3, 4, 5.</summary>
        public static int Distance(int stops) => 3 + Mathf.Max(0, stops);

        /// <summary>Tiles between two tiles, diagonals count one.</summary>
        public static int Distance(Vector2Int a, Vector2Int b) => Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));

        /// <summary>On the tile or next to it.</summary>
        public static bool Landed(Vector2Int tile, Vector2Int target) => Distance(tile, target) <= 1;

        /// <summary>Two tiles further away than the closest it got (past it, or off the other way).</summary>
        public static bool WalkedPast(int distance, int closest) => distance >= closest + 2;
    }
}
