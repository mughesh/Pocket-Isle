using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 14a: the placement rule, pure so it can be tested. Yaw-only forward from the head (looking straight down or
    /// up falls back to the head's up vector), <see cref="Rule.distance"/> out, the island's top
    /// <see cref="Rule.topBelowEye"/> under the eyes, clamped to at least <see cref="Rule.minBelowEye"/> under the eyes
    /// and, when the floor is known, at least <see cref="Rule.minAboveFloor"/> over it (the eye clamp wins when both
    /// can't hold: someone lying on the floor). The same rule places the shard, the island, the menu's Recenter and
    /// Meta's recenter.
    /// </summary>
    public static class IslandPlacement
    {
        public struct Rule
        {
            public float distance;
            public float topBelowEye;
            public float minBelowEye;
            public float minAboveFloor;

            public static Rule Default => new Rule { distance = 0.55f, topBelowEye = 0.30f, minBelowEye = 0.15f, minAboveFloor = 0.55f };
        }

        /// <summary>Horizontal facing from a head pose (never zero).</summary>
        public static Vector3 Facing(Vector3 forward, Vector3 up)
        {
            var f = new Vector3(forward.x, 0f, forward.z);
            if (f.sqrMagnitude > 0.04f) return f.normalized;
            // Nearly straight down: the top of the head points ahead; straight up: the back of the head does.
            var u = forward.y < 0f ? up : -up;
            u.y = 0f;
            if (u.sqrMagnitude > 1e-6f) return u.normalized;
            return f.sqrMagnitude > 1e-8f ? f.normalized : Vector3.forward;
        }

        /// <summary>World height of the island's top for this eye height.</summary>
        public static float TopHeight(float eyeY, float? floorY, Rule rule)
        {
            float top = eyeY - rule.topBelowEye;
            if (floorY.HasValue) top = Mathf.Max(top, floorY.Value + rule.minAboveFloor);
            return Mathf.Min(top, eyeY - rule.minBelowEye);
        }

        /// <summary>
        /// Where the island's pivot goes (its top is <paramref name="topAbovePivot"/> over it) and its rotation (+Z toward
        /// the head, level).
        /// </summary>
        public static Pose Compute(Vector3 headPos, Vector3 headForward, Vector3 headUp, float? floorY, float topAbovePivot, Rule rule)
        {
            var f = Facing(headForward, headUp);
            var p = headPos + f * rule.distance;
            p.y = TopHeight(headPos.y, floorY, rule) - topAbovePivot;
            return new Pose(p, FaceHead(p, headPos, f));
        }

        /// <summary>
        /// Step 14i: the island's yaw: its front (+Z) along the line from its centre to the head (level), not along the
        /// head's forward. <paramref name="fallback"/> (horizontal, toward the island) when the head is right over it.
        /// </summary>
        public static Quaternion FaceHead(Vector3 islandCenter, Vector3 headPos, Vector3 fallback)
        {
            var to = headPos - islandCenter;
            to.y = 0f;
            if (to.sqrMagnitude < 1e-6f) to = -fallback;
            return Quaternion.LookRotation(to.normalized, Vector3.up);
        }

        /// <summary>Head yaw away from a point (degrees, 0..180).</summary>
        public static float YawOff(Vector3 headPos, Vector3 headForward, Vector3 headUp, Vector3 point)
        {
            var to = point - headPos;
            to.y = 0f;
            if (to.sqrMagnitude < 1e-6f) return 0f;
            return Vector3.Angle(Facing(headForward, headUp), to);
        }

        /// <summary>
        /// Step 14a safety net after a recenter: the island ended too far off the head's yaw or too far away.
        /// </summary>
        public static bool Misplaced(Vector3 headPos, Vector3 headForward, Vector3 headUp, Vector3 island, float maxYaw = 40f, float maxDistance = 0.8f)
        {
            var to = island - headPos;
            to.y = 0f;
            return to.magnitude > maxDistance || YawOff(headPos, headForward, headUp, island) > maxYaw;
        }

        /// <summary>
        /// Holds when the head and the tracking space have stayed within <see cref="maxMove"/> and <see cref="maxTurn"/>
        /// of a reference pose for <see cref="hold"/> seconds. Any larger change restarts the wait from the new pose.
        /// </summary>
        public class SettleTracker
        {
            public float hold = 0.5f, maxMove = 0.01f, maxTurn = 1.5f;

            Vector3 _head, _space;
            Quaternion _headRot, _spaceRot;
            float _since = -1f;

            public float StillFor { get; private set; }
            public bool Settled => StillFor >= hold;

            public void Reset() => _since = -1f;

            public bool Feed(float time, Vector3 head, Quaternion headRot, Vector3 space, Quaternion spaceRot)
            {
                bool moved = _since < 0f || (head - _head).magnitude > maxMove || Quaternion.Angle(headRot, _headRot) > maxTurn ||
                             (space - _space).magnitude > maxMove || Quaternion.Angle(spaceRot, _spaceRot) > maxTurn;
                if (moved)
                {
                    _head = head;
                    _headRot = headRot;
                    _space = space;
                    _spaceRot = spaceRot;
                    _since = time;
                }
                StillFor = time - _since;
                return Settled;
            }
        }

        /// <summary>
        /// Step 14a: before the shard is touched it follows the player. Off = the head is more than <see cref="maxYaw"/>
        /// away from it or the eyes moved more than <see cref="maxRise"/> up or down since it was placed; off for
        /// <see cref="hold"/> s = time to glide.
        /// </summary>
        public class FollowCheck
        {
            public float maxYaw = 35f, maxRise = 0.15f, hold = 1f;
            float _offSince = -1f;

            public void Reset() => _offSince = -1f;

            public static bool IsOff(Vector3 headPos, Vector3 headForward, Vector3 headUp, Vector3 shard, float placedEyeY, float maxYaw, float maxRise) =>
                YawOff(headPos, headForward, headUp, shard) > maxYaw || Mathf.Abs(headPos.y - placedEyeY) > maxRise;

            /// <summary>True once the shard has been off for long enough (then call <see cref="Reset"/> after the glide).</summary>
            public bool Feed(float time, Vector3 headPos, Vector3 headForward, Vector3 headUp, Vector3 shard, float placedEyeY)
            {
                if (!IsOff(headPos, headForward, headUp, shard, placedEyeY, maxYaw, maxRise))
                {
                    _offSince = -1f;
                    return false;
                }
                if (_offSince < 0f) _offSince = time;
                return time - _offSince >= hold;
            }
        }
    }
}
