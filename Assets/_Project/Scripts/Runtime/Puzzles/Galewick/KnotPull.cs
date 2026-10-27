using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// One glowing knot on Galeowl (step 10e), pure logic: two loose ends (A, B). A hand that starts a pinch near an end
    /// holds it. Both ends held (one per hand) and pulled apart more than <see cref="twoHandPull"/> beyond where they were
    /// grabbed: it snaps. One end held and pulled away from the knot more than <see cref="oneHandPull"/>: it snaps too, so
    /// it never needs two hands. World metres in, no Unity objects: the tests feed it synthetic hand tracks.
    /// </summary>
    public sealed class KnotPull
    {
        public struct Hand
        {
            public bool pinching, started, busy;
            public Vector3 point;

            public Hand(bool pinching, bool started, Vector3 point, bool busy = false)
            {
                this.pinching = pinching;
                this.started = started;
                this.point = point;
                this.busy = busy;
            }
        }

        /// <summary>World points this frame: the knot and its two loose ends (they move with the owl's breathing).</summary>
        public Vector3 center, endA, endB;
        public float grabRadius = 0.035f;
        public float twoHandPull = 0.12f;
        public float oneHandPull = 0.15f;

        readonly int[] _hold = { -1, -1 };
        readonly Vector3[] _start = new Vector3[2];
        readonly Vector3[] _now = new Vector3[2];
        float _startSpan = -1f;

        public bool Snapped { get; private set; }
        /// <summary>How far the pull is toward snapping (0..1; for the strings' stretch and the glow).</summary>
        public float Stretch { get; private set; }
        /// <summary>The end hand <paramref name="h"/> holds (0 = A, 1 = B), or -1.</summary>
        public int Held(int h) => _hold[h];
        public bool AnyHeld => _hold[0] >= 0 || _hold[1] >= 0;
        public bool BothHeld => _hold[0] >= 0 && _hold[1] >= 0;
        /// <summary>The snap came from two hands pulling apart (else one hand, or <see cref="Loosen"/>).</summary>
        public bool SnappedTwoHanded { get; private set; }

        /// <summary>Where end <paramref name="e"/> is drawn: on the hand holding it, else its rest point.</summary>
        public Vector3 EndNow(int e)
        {
            for (int h = 0; h < 2; h++)
                if (_hold[h] == e) return _now[h];
            return e == 0 ? endA : endB;
        }

        /// <summary>One frame of both hands (0 = left, 1 = right). True on the frame it snaps.</summary>
        public bool Tick(Hand left, Hand right)
        {
            if (Snapped) return false;
            Grab(0, left);
            Grab(1, right);
            if (BothHeld)
            {
                if (_startSpan < 0f) _startSpan = (_start[0] - _start[1]).magnitude;
                float pull = (_now[0] - _now[1]).magnitude - _startSpan;
                Stretch = Mathf.Clamp01(pull / twoHandPull);
                if (pull >= twoHandPull) return Snap(true);
            }
            else
            {
                _startSpan = -1f;
                int h = _hold[0] >= 0 ? 0 : _hold[1] >= 0 ? 1 : -1;
                if (h < 0)
                {
                    Stretch = 0f;
                    return false;
                }
                float pull = (_now[h] - center).magnitude - (_start[h] - center).magnitude;
                Stretch = Mathf.Clamp01(pull / oneHandPull);
                if (pull >= oneHandPull) return Snap(false);
            }
            return false;
        }

        void Grab(int h, Hand hand)
        {
            if (!hand.pinching)
            {
                _hold[h] = -1;
                return;
            }
            if (_hold[h] >= 0)
            {
                _now[h] = hand.point;
                return;
            }
            if (!hand.started || hand.busy) return;
            int other = _hold[1 - h];
            float dA = other == 0 ? float.MaxValue : (hand.point - endA).magnitude;
            float dB = other == 1 ? float.MaxValue : (hand.point - endB).magnitude;
            int e = dA <= dB ? 0 : 1;
            if (Mathf.Min(dA, dB) > grabRadius) return;
            _hold[h] = e;
            _start[h] = _now[h] = hand.point;
            _startSpan = -1f;
        }

        /// <summary>Look + pinch loosened it (after its 2 s), or the bridge.</summary>
        public bool Loosen() => !Snapped && Snap(false);

        bool Snap(bool twoHanded)
        {
            Snapped = true;
            SnappedTwoHanded = twoHanded;
            Stretch = 1f;
            _hold[0] = _hold[1] = -1;
            return true;
        }

        public void Reset()
        {
            Snapped = SnappedTwoHanded = false;
            Stretch = 0f;
            _hold[0] = _hold[1] = -1;
            _startSpan = -1f;
        }
    }
}
