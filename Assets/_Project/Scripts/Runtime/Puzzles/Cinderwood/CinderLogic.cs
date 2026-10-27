using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 10f: cupping the village flame (pure, EditMode-tested). A palm "faces" the flame when it's within
    /// <c>radius</c> of it and its palm normal points within <c>maxAngle</c> of the flame. Two facing hands cup it; one
    /// hand also counts when it's curled round it (its middle fingertip within the radius of the flame too).
    /// </summary>
    public static class CupRules
    {
        public static bool Facing(Vector3 palm, Vector3 normal, Vector3 flame, float radius, float maxAngle)
        {
            var to = flame - palm;
            float d = to.magnitude;
            if (d > radius) return false;
            return d < 1e-4f || Vector3.Angle(normal, to) <= maxAngle;
        }

        /// <summary>One hand wrapped round the flame: facing it, fingers curled (middle tip near the palm) and reaching round it.</summary>
        public static bool Wraps(Vector3 palm, Vector3 normal, Vector3 middleTip, Vector3 flame, float radius, float maxAngle) =>
            Facing(palm, normal, flame, radius, maxAngle) && (middleTip - flame).magnitude <= radius && (middleTip - palm).magnitude < 0.085f;
    }

    /// <summary>
    /// Step 10f: how warm the cupped flame is (0..1). Both hands cupping fill it in <see cref="twoHands"/> s, one hand
    /// curled round it in <see cref="oneHand"/> s, a look + pinch-hold in <see cref="look"/> s; let go and it cools at
    /// <see cref="cool"/> per second. Full = rekindled (once).
    /// </summary>
    public class CupMeter
    {
        public float twoHands = 2f, oneHand = 3f, look = 3f, cool = 0.5f;
        public float Progress { get; private set; }
        public bool Done { get; private set; }

        /// <summary>Holding at all this frame.</summary>
        public bool Held { get; private set; }

        /// <summary>One frame: how many hands face it, whether one is wrapped round it, a look + pinch-hold. True on the frame it's full.</summary>
        public bool Feed(float dt, int facingHands, bool wrapped, bool lookHold)
        {
            if (Done) return false;
            float rate = facingHands >= 2 ? 1f / Mathf.Max(0.01f, twoHands)
                : wrapped ? 1f / Mathf.Max(0.01f, oneHand)
                : lookHold ? 1f / Mathf.Max(0.01f, look) : 0f;
            Held = rate > 0f;
            Progress = Held ? Mathf.Min(1f, Progress + rate * dt) : Mathf.Max(0f, Progress - cool * dt);
            if (Progress < 1f) return false;
            Done = true;
            return true;
        }

        public void Restore(bool done)
        {
            Done = done;
            Progress = done ? 1f : 0f;
            Held = false;
        }
    }

    /// <summary>
    /// Step 10f: the smoky hollow's lantern line (pure, EditMode-tested). Lanterns are numbered along the path. A touch
    /// lights one; a second touch on another lantern within <see cref="sweepWindow"/> s is a sweep and the fire
    /// <b>races</b> on by itself, one lantern every <see cref="raceStep"/> s, first on in the sweep's direction to the end,
    /// then back from the first touched one to the other end. Look + pinch lights one and the next along catches
    /// <see cref="nudgeDelay"/> s later. <see cref="Tick"/> reports what caught; all lit = <see cref="AllLit"/>.
    /// </summary>
    public class LanternChain
    {
        public float sweepWindow = 1.5f, raceStep = 0.15f, nudgeDelay = 0.5f;
        public readonly bool[] lit;

        readonly List<int> _queue = new List<int>();
        float _time, _nextAt = -1f, _lastTouchAt = -99f;
        int _lastTouch = -1;
        float _queueStep;

        public LanternChain(int count) => lit = new bool[count];

        public int Count => lit.Length;
        /// <summary>The chain's own clock (seconds of Tick) and when the last touch was.</summary>
        public float Clock => _time;
        public float LastTouchAt => _lastTouchAt;
        public bool Racing => _queue.Count > 0;
        /// <summary>Where the race (or the last lantern lit) started: the end the smoke burns from.</summary>
        public int Origin { get; private set; } = -1;

        public bool AllLit
        {
            get
            {
                foreach (var l in lit)
                    if (!l) return false;
                return true;
            }
        }

        /// <summary>Lit lanterns as a bit mask (saved stage).</summary>
        public int Mask
        {
            get
            {
                int m = 0;
                for (int i = 0; i < lit.Length; i++)
                    if (lit[i]) m |= 1 << i;
                return m;
            }
        }

        public void Restore(int mask)
        {
            for (int i = 0; i < lit.Length; i++) lit[i] = (mask & (1 << i)) != 0;
            _queue.Clear();
            _nextAt = -1f;
            _lastTouch = -1;
            Origin = -1;
        }

        /// <summary>A fingertip touched lantern <paramref name="i"/>. True if it lit now (it was dark).</summary>
        public bool Touch(int i)
        {
            if (i < 0 || i >= lit.Length) return false;
            bool sweep = _lastTouch >= 0 && _lastTouch != i && _time - _lastTouchAt <= sweepWindow && !Racing;
            bool litNow = !lit[i];
            lit[i] = true;
            if (sweep) Race(_lastTouch, i);
            else if (litNow && Origin < 0) Origin = i;
            _lastTouch = i;
            _lastTouchAt = _time;
            return litNow;
        }

        /// <summary>Look + pinch on lantern <paramref name="i"/>: it lights, the next one along follows by itself.</summary>
        public bool Nudge(int i)
        {
            if (i < 0 || i >= lit.Length) return false;
            bool litNow = !lit[i];
            lit[i] = true;
            if (litNow && Origin < 0) Origin = i;
            if (Racing) return litNow;
            int next = i + 1 < lit.Length && !lit[i + 1] ? i + 1 : i - 1 >= 0 && !lit[i - 1] ? i - 1 : -1;
            if (next >= 0)
            {
                _queue.Add(next);
                _queueStep = nudgeDelay;
                _nextAt = _time + nudgeDelay;
            }
            return litNow;
        }

        void Race(int from, int to)
        {
            Origin = from;
            int dir = to > from ? 1 : -1;
            _queue.Clear();
            for (int k = to + dir; k >= 0 && k < lit.Length; k += dir)
                if (!lit[k]) _queue.Add(k);
            for (int k = from - dir; k >= 0 && k < lit.Length; k -= dir)
                if (!lit[k]) _queue.Add(k);
            _queueStep = raceStep;
            _nextAt = _time + raceStep;
        }

        /// <summary>Advances the clock; returns the lantern that caught this step, or -1.</summary>
        public int Tick(float dt)
        {
            _time += dt;
            while (_queue.Count > 0 && lit[_queue[0]]) _queue.RemoveAt(0);
            if (_queue.Count == 0 || _time < _nextAt) return -1;
            int i = _queue[0];
            _queue.RemoveAt(0);
            lit[i] = true;
            _nextAt = _time + _queueStep;
            return i;
        }
    }
}
