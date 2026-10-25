using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// A pump's stroke counting, no scene objects (EditMode-tested). A stroke = the handle pushed past
    /// <see cref="downAt"/> of its travel and back above <see cref="upAt"/>. Heat cools when you stop: after
    /// <see cref="coolDelay"/> seconds without a stroke, one stroke is lost every <see cref="coolStep"/> seconds, never
    /// below the last checkpoint (every <see cref="checkpoint"/> strokes: a lit brazier never goes out).
    /// </summary>
    public class PumpStrokes
    {
        public float downAt = 0.8f, upAt = 0.3f;
        public int needed = 12, checkpoint = 4;
        public float coolDelay = 4f, coolStep = 2f;

        float _cool;

        public int Strokes { get; private set; }
        /// <summary>Pushed past <see cref="downAt"/>, waiting to come back up.</summary>
        public bool Down { get; private set; }
        /// <summary>Seconds since the last stroke.</summary>
        public float Idle { get; private set; }
        public bool Done => Strokes >= needed;
        /// <summary>Checkpoints passed (lit braziers).</summary>
        public int Checkpoints => checkpoint > 0 ? Mathf.Min(Strokes, needed) / checkpoint : 0;
        /// <summary>Cooling never goes below this.</summary>
        public int Floor => Done ? needed : Checkpoints * checkpoint;

        /// <summary>The handle's travel this frame (0 = up at rest, 1 = all the way down). True when a stroke completes.</summary>
        public bool Feed(float travel)
        {
            if (Done) return false;
            if (!Down)
            {
                if (travel >= downAt) Down = true;
                return false;
            }
            if (travel > upAt) return false;
            Down = false;
            return Add();
        }

        /// <summary>One stroke (look + pinch on the handle does a whole stroke at once). False once done.</summary>
        public bool Add()
        {
            if (Done) return false;
            Strokes++;
            Idle = 0f;
            _cool = 0f;
            return true;
        }

        /// <summary>Time passes. True when a stroke cooled away this tick.</summary>
        public bool Tick(float dt)
        {
            Idle += dt;
            if (Done || Strokes <= Floor || Idle < coolDelay)
            {
                _cool = 0f;
                return false;
            }
            _cool += dt;
            if (_cool < coolStep) return false;
            _cool -= coolStep;
            Strokes--;
            return true;
        }

        /// <summary>From the save: start at this many strokes (a checkpoint).</summary>
        public void Restore(int strokes)
        {
            Strokes = Mathf.Clamp(strokes, 0, needed);
            Down = false;
            Idle = 0f;
            _cool = 0f;
        }
    }

    /// <summary>A carried object's drop and follow rules, no scene objects (EditMode-tested). Units are map tiles.</summary>
    public static class CarryLogic
    {
        /// <summary>The spot nearest to <paramref name="at"/> within <paramref name="snapRadius"/> (horizontal), or -1 (it falls back home).</summary>
        public static int PickSpot(Vector2 at, IReadOnlyList<Vector2> spots, float snapRadius)
        {
            int best = -1;
            float bestSq = snapRadius * snapRadius;
            if (spots == null) return -1;
            for (int i = 0; i < spots.Count; i++)
            {
                float d = (spots[i] - at).sqrMagnitude;
                if (d <= bestSq)
                {
                    bestSq = d;
                    best = i;
                }
            }
            return best;
        }

        /// <summary>
        /// One step of a heavy follow: eases toward <paramref name="target"/> with a time constant of
        /// <paramref name="lag"/> seconds, no faster than <paramref name="maxSpeed"/> (tiles per second).
        /// </summary>
        public static Vector3 Follow(Vector3 current, Vector3 target, float lag, float maxSpeed, float dt)
        {
            if (dt <= 0f) return current;
            Vector3 eased = lag > 1e-4f ? Vector3.Lerp(current, target, 1f - Mathf.Exp(-dt / lag)) : target;
            return Vector3.MoveTowards(current, eased, Mathf.Max(0f, maxSpeed) * dt);
        }

        /// <summary>Distance from <paramref name="p"/> to the segment a..b.</summary>
        public static float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float t = ab.sqrMagnitude > 1e-8f ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
            return (a + ab * t - p).magnitude;
        }
    }
}
