using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 29 G (playtest: a line came right up to the face when the island was pulled close): a bubble, chip or tablet
    /// never comes nearer the head than <see cref="MinDistance"/>. Closer, it slides back along the view ray to that
    /// distance and grows by the same ratio, so it keeps its size in view. Runs after its owner placed it (late), and undoes
    /// its own push first so nothing accumulates when the owner doesn't re-place it every frame. Add with <see cref="On"/>.
    /// </summary>
    [DefaultExecutionOrder(30000)]
    public class KeepTextAway : MonoBehaviour
    {
        public const float MinDistance = 0.45f;

        Vector3 _pushedPos, _basePos, _pushedScale, _baseScale;
        bool _pushed;

        /// <summary>How far it was pushed last frame (tests, the bridge): 1 = not pushed.</summary>
        public float LastScale { get; private set; } = 1f;

        /// <summary>Where a point goes so it is at least <paramref name="min"/> from the head, and the size factor that keeps its angular size (pure).</summary>
        public static Vector3 Pushed(Vector3 head, Vector3 point, float min, out float scale)
        {
            var d = point - head;
            float m = d.magnitude;
            scale = 1f;
            if (m >= min || m < 1e-4f) return point;
            scale = min / m;
            return head + d / m * min;
        }

        /// <summary>Adds (once) the keeper to a text root.</summary>
        public static KeepTextAway On(Component root)
        {
            if (root == null) return null;
            var k = root.GetComponent<KeepTextAway>();
            if (k == null) k = root.gameObject.AddComponent<KeepTextAway>();
            return k;
        }

        void LateUpdate()
        {
            var t = transform;
            if (_pushed)
            {
                // Still where we put it: the owner didn't move it this frame; start from where it really is.
                if ((t.position - _pushedPos).sqrMagnitude < 1e-10f) t.position = _basePos;
                if ((t.localScale - _pushedScale).sqrMagnitude < 1e-12f) t.localScale = _baseScale;
                _pushed = false;
            }
            var cam = Camera.main;
            LastScale = 1f;
            if (cam == null) return;
            var p = Pushed(cam.transform.position, t.position, MinDistance, out float k);
            if (k <= 1.0001f) return;
            _basePos = t.position;
            _baseScale = t.localScale;
            t.position = p;
            t.localScale = _baseScale * k;
            _pushedPos = t.position;
            _pushedScale = t.localScale;
            _pushed = true;
            LastScale = k;
        }
    }
}
