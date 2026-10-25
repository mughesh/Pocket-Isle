using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// "The gaze is on something a look + pinch would select" this frame or the last (step 8e): a pod, a lodge Isling,
    /// a shrine, the bubble (all through <see cref="FocusHighlight.Hover"/>) or a walkable tile (<see cref="TileGaze"/>).
    /// The island's pinch arbiter reads it.
    /// </summary>
    public static class GazeTarget
    {
        static int _frame = -10;

        public static void Mark() => _frame = Time.frameCount;

        public static bool Any => _frame >= Time.frameCount - 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _frame = -10;
    }
}
