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
    /// curled round it in <see cref="oneHand"/> s, one hand just facing it in <see cref="oneFacing"/> s, a look +
    /// pinch-hold in <see cref="look"/> s. Headset feedback Oct 27: tracking flickers a hand in and out of the zone, and
    /// cooling on those frames cancelled the filling (stuck halfway), so a drop shorter than <see cref="grace"/> s keeps
    /// filling at the last rate; let go for longer and it cools at <see cref="cool"/> per second. Full = rekindled (once).
    /// </summary>
    public class CupMeter
    {
        public float twoHands = 2f, oneHand = 3f, oneFacing = 4f, look = 3f, cool = 0.25f, grace = 0.35f;
        float _lastRate, _graceLeft;
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
                : lookHold ? 1f / Mathf.Max(0.01f, look)
                : facingHands == 1 ? 1f / Mathf.Max(0.01f, oneFacing) : 0f;
            if (rate > 0f)
            {
                _lastRate = rate;
                _graceLeft = grace;
            }
            else if (_graceLeft > 0f)
            {
                _graceLeft -= dt;
                rate = _lastRate;
            }
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
            _graceLeft = 0f;
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

    /// <summary>Where the forge's ember is (step 10f).</summary>
    public enum EmberState { InVent, OnPalm, Falling, Hovering, Flying, In }

    /// <summary>
    /// Step 10f: carrying an ember on an open palm (pure, EditMode-tested). In its vent, an open hand held palm-up
    /// beside it for <see cref="pickSeconds"/> (or a pinch at the ember) lifts it onto that palm. It stays while the palm
    /// is still roughly up (<see cref="keepUp"/>) or the pinch holds; turned over (or let go) above the firebox it tips in,
    /// anywhere else it falls back to its vent. Lost tracking drops it too. <see cref="Falling"/>, hovering (look +
    /// pinch) and flying are timed by the piece, which calls <see cref="Back"/>, <see cref="Hover"/>, <see cref="Fly"/>
    /// and <see cref="Arrive"/>.
    /// </summary>
    public class EmberCarry
    {
        /// <summary>One hand this frame, already measured by the piece.</summary>
        public struct Hand
        {
            public bool tracked, busy, pinching, pinchStarted;
            /// <summary>Palm normal · up (1 = palm straight up, -1 = palm down).</summary>
            public float up;
            /// <summary>The palm beside the vent (pick-up zone), the pinch point at the ember, the palm over the firebox.</summary>
            public bool nearVent, atEmber, overFirebox;
        }

        public float pickSeconds = 0.35f, pickUp = 0.6f, keepUp = 0.2f;

        public EmberState State { get; private set; } = EmberState.InVent;
        /// <summary>The hand carrying it (0 left, 1 right), -1 = none.</summary>
        public int Holder { get; private set; } = -1;
        public bool Pinched { get; private set; }
        float _hold;

        public static bool PalmUp(float up, float threshold) => up >= threshold;

        /// <summary>One frame. Returns the state now (it changed when it differs from before).</summary>
        public EmberState Feed(float dt, Hand[] hands)
        {
            switch (State)
            {
                case EmberState.InVent:
                case EmberState.Hovering:
                {
                    bool any = false;
                    for (int h = 0; h < hands.Length; h++)
                    {
                        var x = hands[h];
                        if (!x.tracked || x.busy) continue;
                        if (x.pinchStarted && x.atEmber)
                        {
                            Lift(h, true);
                            return State;
                        }
                        if (State == EmberState.InVent && x.nearVent && PalmUp(x.up, pickUp))
                        {
                            any = true;
                            _hold += dt;
                            if (_hold >= pickSeconds)
                            {
                                Lift(h, false);
                                return State;
                            }
                        }
                    }
                    if (!any) _hold = 0f;
                    break;
                }
                case EmberState.OnPalm:
                {
                    var x = Holder >= 0 && Holder < hands.Length ? hands[Holder] : default;
                    if (Pinched && !x.pinching) Pinched = false;
                    if (!x.tracked)
                    {
                        Drop();
                        break;
                    }
                    if (Pinched || PalmUp(x.up, keepUp)) break;
                    // Turned over or let go: in, if it's above the firebox; otherwise it falls.
                    if (x.overFirebox)
                    {
                        State = EmberState.In;
                        Holder = -1;
                    }
                    else Drop();
                    break;
                }
            }
            return State;
        }

        void Lift(int hand, bool pinched)
        {
            State = EmberState.OnPalm;
            Holder = hand;
            Pinched = pinched;
            _hold = 0f;
        }

        void Drop()
        {
            State = EmberState.Falling;
            Holder = -1;
            Pinched = false;
        }

        /// <summary>It fell back into its vent.</summary>
        public void Back()
        {
            State = EmberState.InVent;
            _hold = 0f;
        }

        /// <summary>Look + pinch the vent: it rises and waits.</summary>
        public void Hover()
        {
            if (State == EmberState.InVent) State = EmberState.Hovering;
        }

        /// <summary>Look + pinch the firebox while it waits: it flies there by itself.</summary>
        public void Fly()
        {
            if (State == EmberState.Hovering) State = EmberState.Flying;
        }

        public void Arrive()
        {
            State = EmberState.In;
            Holder = -1;
        }

        /// <summary>From the save: in (lit) or back in its vent.</summary>
        public void Restore(bool lit)
        {
            State = lit ? EmberState.In : EmberState.InVent;
            Holder = -1;
            Pinched = false;
            _hold = 0f;
        }
    }
}
