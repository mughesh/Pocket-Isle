using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>Nov 11 bridge: close shots (Logs/BattleShots) from the seat's side of a point, for judging small things.</summary>
    public static class Nov11Debug
    {
        /// <summary>Renders <paramref name="target"/> from <paramref name="distance"/> m along the line to the seat camera.</summary>
        public static void Close(string name, Vector3 target, float distance, float lift = 0f)
        {
            var main = Camera.main;
            if (main == null) return;
            var dir = (main.transform.position - target).normalized;
            var eye = target + dir * distance + Vector3.up * lift;
            BattleDebug.Render($"Logs/BattleShots/{name}.png", eye, Quaternion.LookRotation(target - eye, Vector3.up));
            Debug.Log($"[Nov11Debug] Close shot {name}.");
        }

        /// <summary>The first three rim pods, close.</summary>
        public static void Rings(string name)
        {
            var rack = Object.FindFirstObjectByType<PodRack>();
            if (rack == null || rack.Pod(0) == null) return;
            var a = rack.Pod(0).transform.position;
            var far = rack.Pod(2) != null ? rack.Pod(2) : rack.Pod(1);
            var b = far != null ? far.transform.position : a;
            Close(name, (a + b) * 0.5f, 0.11f, 0f);
        }
    }
}
