namespace Diorama
{
    /// <summary>
    /// Step 11d, one rule for every objective (pure, EditMode-tested): the chip counts ("Kites 1/3"), and a lesson with a
    /// counter stays pinned until the objective is done, pinning again whenever the player comes back to it. Without a
    /// counter the 10b rule holds: pinned from the lesson until the action first works.
    /// </summary>
    public static class ObjectiveRules
    {
        public static string Chip(string label, int done, int total) => $"{label} {done}/{total}";

        /// <summary>Whether the lesson line should be pinned now.</summary>
        public static bool WantPin(bool counter, bool lessonComplete, bool solved, bool usable, bool started)
        {
            if (solved || !usable) return false;
            return counter || !lessonComplete && started;
        }

        /// <summary>Bits set in a mask (done objects).</summary>
        public static int Count(int mask)
        {
            int n = 0;
            for (; mask != 0; mask &= mask - 1) n++;
            return n;
        }
    }
}
