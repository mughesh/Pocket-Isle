using System;
using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 16: where the player is looking and reaching for the current objective, as world spheres (xyz centre, w
    /// radius): the ghost hand's key point and its drop spot, plus whatever listeners add (the battle's creatures).
    /// The Spirit parks clear of them (<see cref="LanternSpirit"/>) and the bubble moves off them
    /// (<see cref="DialogueBubble.KeepClear"/>).
    /// </summary>
    public static class ObjectiveArea
    {
        /// <summary>Listeners add their spheres (battle: both creatures).</summary>
        public static event Action<List<Vector4>> Collect;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Collect = null;
            _hooked = false;
        }

        static bool _hooked;

        /// <summary>Ghost radius around its key point and drop spot (world m).</summary>
        public const float GhostRadius = 0.045f;

        public static void Gather(List<Vector4> into)
        {
            var g = GhostHands.Current;
            if (g != null && g.IsShowing)
            {
                if (g.KeyPoint.HasValue) into.Add(Sphere(g.KeyPoint.Value, GhostRadius));
                if (g.ReleasePoint.HasValue) into.Add(Sphere(g.ReleasePoint.Value, GhostRadius));
            }
            Collect?.Invoke(into);
        }

        /// <summary>The bubble keeps clear of the objective area too (hooked once, the first time a Spirit starts).</summary>
        public static void HookBubble()
        {
            if (_hooked) return;
            _hooked = true;
            DialogueBubble.KeepClear += Gather;
        }

        public static Vector4 Sphere(Vector3 c, float r) => new Vector4(c.x, c.y, c.z, r);

        /// <summary>
        /// A ball at <paramref name="at"/> (radius <paramref name="radius"/>) overlaps any sphere as seen from
        /// <paramref name="head"/> (angular test), or touches one in space.
        /// </summary>
        public static bool Blocks(Vector3 head, Vector3 at, float radius, IList<Vector4> spheres)
        {
            Vector3 d = at - head;
            float dl = d.magnitude;
            if (dl < 1e-4f) return false;
            float ra = Mathf.Asin(Mathf.Clamp01(radius / dl));
            foreach (var s in spheres)
            {
                Vector3 c = new Vector3(s.x, s.y, s.z);
                if ((c - at).magnitude < s.w + radius) return true;
                Vector3 e = c - head;
                float el = e.magnitude;
                if (el < 1e-4f) continue;
                float rs = Mathf.Asin(Mathf.Clamp01(s.w / el));
                if (Vector3.Angle(d, e) * Mathf.Deg2Rad < ra + rs) return true;
            }
            return false;
        }
    }
}
