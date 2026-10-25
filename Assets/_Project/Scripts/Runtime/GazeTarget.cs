using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// "The gaze is on something a look + pinch would select" this frame or the last (step 8e), and where: a pod, a
    /// lodge Isling, a shrine, the bubble (all through <see cref="FocusHighlight.Hover"/>) or a walkable tile
    /// (<see cref="TileGaze"/>). The island's pinch arbiter reads it.
    /// </summary>
    public static class GazeTarget
    {
        static int _frame = -10;
        static Vector3 _point;

        public static void Mark(Vector3 world)
        {
            _frame = Time.frameCount;
            _point = world;
        }

        public static bool TryGet(out Vector3 world)
        {
            world = _point;
            return _frame >= Time.frameCount - 1;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _frame = -10;
    }
}
