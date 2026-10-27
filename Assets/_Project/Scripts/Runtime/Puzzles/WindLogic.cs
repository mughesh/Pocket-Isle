using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 10b: a wheel's spin (pure, EditMode-tested): angular velocity with drag, flicks that add spin by their speed,
    /// and two ways to solve it: <see cref="flicksNeeded"/> flicks within <see cref="flickWindow"/> seconds, or (when
    /// <see cref="holdSeconds"/> &gt; 0) keeping above <see cref="holdSpeed"/> for that long. Once solved (or told to
    /// <see cref="Keep"/>) it settles at <see cref="keepSpeed"/> and never stops.
    /// </summary>
    public class WheelSpin
    {
        /// <summary>Exponential drag per second (1.5 = a spin loses ~78% of its speed in a second).</summary>
        public float drag = 1.5f;
        /// <summary>One flick at the minimum speed turns the wheel this far before it stops (radians; a quarter turn).</summary>
        public float flickTurn = Mathf.PI * 0.5f;
        /// <summary>Fingertip speed that counts as a flick (m/s).</summary>
        public float minFlickSpeed = 0.5f;
        /// <summary>Faster flicks add more, up to this many times the minimum.</summary>
        public float maxFlickScale = 2.5f;
        public int flicksNeeded = 3;
        public float flickWindow = 4f;
        public float holdSpeed, holdSeconds;
        /// <summary>Speed once it keeps turning (radians per second).</summary>
        public float keepSpeed = 2.2f;

        public float Angle { get; private set; }
        public float Velocity { get; private set; }
        public bool Kept { get; private set; }
        public int FlickCount { get; private set; }
        public float Held { get; private set; }

        readonly List<float> _flicks = new List<float>();
        float _time;

        /// <summary>Flicks inside the window right now.</summary>
        public int RecentFlicks
        {
            get
            {
                _flicks.RemoveAll(t => _time - t > flickWindow);
                return _flicks.Count;
            }
        }

        /// <summary>
        /// A flick with a signed tangential speed (m/s). Returns true if it counted (fast enough). The spin it adds turns the
        /// wheel <see cref="flickTurn"/> x (speed / min, capped) before drag stops it.
        /// </summary>
        public bool Flick(float tangentialSpeed)
        {
            float speed = Mathf.Abs(tangentialSpeed);
            if (speed < minFlickSpeed) return false;
            float scale = Mathf.Min(maxFlickScale, speed / minFlickSpeed);
            float add = drag * flickTurn * scale * Mathf.Sign(tangentialSpeed);
            // A flick against the turn first stops it.
            Velocity = Mathf.Sign(Velocity) != Mathf.Sign(add) && Mathf.Abs(Velocity) > 0.01f ? add * 0.5f : Velocity + add;
            FlickCount++;
            _flicks.Add(_time);
            return true;
        }

        /// <summary>Advances by <paramref name="dt"/>. Returns true on the frame it becomes solved (flicks or hold).</summary>
        public bool Step(float dt)
        {
            _time += dt;
            if (Kept)
            {
                float dir = Velocity < 0f ? -1f : 1f;
                Velocity = Mathf.MoveTowards(Velocity, dir * keepSpeed, keepSpeed * dt);
                Angle += Velocity * dt;
                return false;
            }
            Angle += Velocity * dt;
            Velocity *= Mathf.Exp(-drag * dt);
            if (Mathf.Abs(Velocity) < 0.01f) Velocity = 0f;
            bool solved = false;
            if (flicksNeeded > 0 && RecentFlicks >= flicksNeeded) solved = true;
            if (holdSeconds > 0f)
            {
                Held = Mathf.Abs(Velocity) >= holdSpeed ? Held + dt : 0f;
                if (Held >= holdSeconds) solved = true;
            }
            if (solved) Keep();
            return solved;
        }

        /// <summary>Keeps turning from now on (solved, or the realm's wind blows).</summary>
        public void Keep()
        {
            Kept = true;
            if (Mathf.Abs(Velocity) < 0.1f) Velocity = 0.1f;
        }

        /// <summary>Back to a still wheel (a save loaded unsolved).</summary>
        public void Reset()
        {
            Kept = false;
            Velocity = 0f;
            _flicks.Clear();
            FlickCount = 0;
            Held = 0f;
        }
    }

    /// <summary>
    /// Step 11d: does a fingertip flick a wheel (pure, EditMode-tested)? Within <paramref name="nearPlane"/> of the
    /// sails' plane, inside the disc (<paramref name="reach"/>, not at the hub), moving along the turn (tangential part)
    /// at least <paramref name="minSpeed"/>. The signed tangential speed (counter-clockwise as seen from the player is +),
    /// and whether the fingertip is close enough to the sails that its touches shouldn't walk the explorer.
    /// </summary>
    public static class WheelRules
    {
        public static bool Flick(Vector3 tip, Vector3 velocity, Vector3 hub, Vector3 normal, float reach, float nearPlane, float hubRadius,
            float minSpeed, float guardRange, out float speed, out bool guard)
        {
            speed = 0f;
            Vector3 rel = tip - hub;
            float off = Vector3.Dot(rel, normal);
            Vector3 inPlane = rel - normal * off;
            float r = inPlane.magnitude;
            guard = Mathf.Abs(off) < guardRange && r < reach + guardRange;
            if (Mathf.Abs(off) > nearPlane || r > reach || r < hubRadius) return false;
            Vector3 tangent = Vector3.Cross(normal, inPlane / r);
            speed = Vector3.Dot(velocity, tangent);
            return Mathf.Abs(speed) >= minSpeed;
        }
    }

    /// <summary>
    /// Step 10b: gust detection for one hand (pure): an open, flat hand (not pinching, not a fist, not holding the island)
    /// moving faster than <see cref="minSpeed"/> over the island footprint, at most <see cref="maxHeight"/> above it, is a
    /// gust in the direction it moves, strength from its speed. One gust per <see cref="cooldown"/>. Velocity is smoothed
    /// over a few frames so tracking jitter doesn't count.
    /// </summary>
    public class GustTracker
    {
        public float minSpeed = 0.6f;
        /// <summary>Speed that gives full strength (m/s).</summary>
        public float fullSpeed = 1.6f;
        public float maxHeight = 0.3f;
        public float cooldown = 0.3f;
        /// <summary>Velocity smoothing (0..1 per frame at 72 fps; higher = steadier).</summary>
        public float smoothing = 0.5f;

        public struct Gust
        {
            /// <summary>Where (island-local metres, xz) and which way (unit, xz) it blew, 0..1 strong.</summary>
            public Vector2 origin, direction;
            public float strength, speed;
        }

        Vector3 _last;
        Vector3 _velocity;
        bool _has;
        float _cool;

        /// <summary>Horizontal speed right now (m/s, smoothed).</summary>
        public float Speed => new Vector2(_velocity.x, _velocity.z).magnitude;

        /// <summary>
        /// One sample: palm position in metres in the island's frame (x, z across, y up, already scaled to the real world),
        /// whether the hand counts (open and free), whether it's over the footprint, and its height above the island top
        /// (metres). Returns a gust or null.
        /// </summary>
        public Gust? Feed(float dt, Vector3 palm, bool open, bool overIsland, float heightAbove)
        {
            if (dt <= 0f) return null;
            _cool = Mathf.Max(0f, _cool - dt);
            if (!_has)
            {
                _last = palm;
                _has = true;
                return null;
            }
            var v = (palm - _last) / dt;
            _last = palm;
            float k = 1f - Mathf.Pow(smoothing, dt * 72f);
            _velocity = Vector3.Lerp(_velocity, v, k);
            float raw = new Vector2(v.x, v.z).magnitude;
            if (!open || !overIsland || heightAbove < -0.03f || heightAbove > maxHeight || _cool > 0f) return null;
            var flat = new Vector2(_velocity.x, _velocity.z);
            float speed = flat.magnitude;
            if (speed < minSpeed) return null;
            _cool = cooldown;
            // The smoothed speed gates it (no jitter spikes); the strength reads the hand's speed now (capped at twice that).
            speed = Mathf.Max(speed, Mathf.Min(raw, speed * 2f));
            return new Gust
            {
                origin = new Vector2(palm.x, palm.z),
                direction = flat.normalized,
                speed = speed,
                strength = Mathf.Clamp01(0.35f + 0.65f * (speed - minSpeed) / Mathf.Max(0.01f, fullSpeed - minSpeed)),
            };
        }

        /// <summary>The hand was lost: start over (no velocity spike when it comes back).</summary>
        public void Lose()
        {
            _has = false;
            _velocity = Vector3.zero;
        }
    }

    /// <summary>Step 10b: kite rules (pure).</summary>
    public static class KiteRules
    {
        /// <summary>
        /// A kite let go at <paramref name="height"/> above the island top (island-scaled metres) with an upward speed of
        /// <paramref name="upSpeed"/> (m/s) flies when it's above <paramref name="threshold"/>, or tossed up faster than
        /// <paramref name="tossSpeed"/>; otherwise it drifts back down.
        /// </summary>
        public static bool Flies(float height, float upSpeed, float threshold, float tossSpeed) =>
            height >= threshold || upSpeed >= tossSpeed && height > threshold * 0.35f;

        /// <summary>Bit of kite <paramref name="i"/> in the saved stage (flown kites).</summary>
        public static int Bit(int i) => 1 << i;

        public static int Count(int mask)
        {
            int n = 0;
            for (int i = 0; i < 3; i++)
                if ((mask & Bit(i)) != 0) n++;
            return n;
        }

        /// <summary>The flying slot of kite number <paramref name="order"/> (0..2): fanned left, middle, right, each higher.</summary>
        public static Vector3 Slot(int order, float baseHeight, float step, float spread) =>
            new Vector3((order - 1) * spread, baseHeight + order * step, -0.04f * (order % 2));
    }

    /// <summary>
    /// Step 10b: the cloud bank (pure): each puff thins by a gust passing over it (stronger gusts thin more; about three
    /// good ones clear a puff) or slowly under a look + pinch-hold. A puff at 0 is gone.
    /// </summary>
    public class CloudBank
    {
        public float perGust = 0.36f;
        public float minGust = 0.25f;
        /// <summary>Thinning per second while held (look + pinch).</summary>
        public float holdRate = 0.4f;
        public readonly float[] thickness;

        public CloudBank(int puffs)
        {
            thickness = new float[puffs];
            for (int i = 0; i < puffs; i++) thickness[i] = 1f;
        }

        public bool Gone(int i) => thickness[i] <= 0f;

        public bool AllGone
        {
            get
            {
                foreach (var t in thickness)
                    if (t > 0f) return false;
                return true;
            }
        }

        /// <summary>A gust of <paramref name="strength"/> over puff <paramref name="i"/>. True when that puff just went.</summary>
        public bool Gust(int i, float strength)
        {
            if (Gone(i) || strength < minGust) return false;
            thickness[i] = Mathf.Max(0f, thickness[i] - perGust * Mathf.Lerp(0.75f, 1.25f, Mathf.InverseLerp(minGust, 1f, strength)));
            if (thickness[i] < 0.02f) thickness[i] = 0f;
            return Gone(i);
        }

        /// <summary>Held for <paramref name="dt"/> seconds. True when it just went.</summary>
        public bool Hold(int i, float dt)
        {
            if (Gone(i)) return false;
            thickness[i] = Mathf.Max(0f, thickness[i] - holdRate * dt);
            return Gone(i);
        }

        /// <summary>Gone puffs as a bit mask (saved stage).</summary>
        public int Mask
        {
            get
            {
                int m = 0;
                for (int i = 0; i < thickness.Length; i++)
                    if (Gone(i)) m |= 1 << i;
                return m;
            }
        }

        public void Restore(int mask)
        {
            for (int i = 0; i < thickness.Length; i++) thickness[i] = (mask & (1 << i)) != 0 ? 0f : 1f;
        }
    }

    /// <summary>Step 10b: the wind tower door's rule (pure): it opens when the explorer arrives with all three kites.</summary>
    public static class TowerRules
    {
        public const int KitesNeeded = 3;

        public static bool Opens(int kitesFlying, bool atDoor) => atDoor && kitesFlying >= KitesNeeded;
    }
}
