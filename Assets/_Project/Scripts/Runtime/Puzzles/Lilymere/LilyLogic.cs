using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 11b A: lily pads drifting on the channel (pure, EditMode-tested). Map plane (column, row) in tiles. A push sets
    /// a pad's velocity (capped); drag slows it; it bounces off the channel's banks (<see cref="bounds"/>) and off other
    /// pads with a little bounce. A drifting pad within <see cref="snapRadius"/> of a free slot glides in and locks
    /// (<see cref="Tick"/> reports it). Locked pads never move again. All slots full = <see cref="AllIn"/>.
    /// </summary>
    public class PadDrift
    {
        public float drag = 0.8f, maxSpeed = 2.5f, bounce = 0.5f, radius = 0.42f, snapRadius = 0.6f, glide = 4f, idle = 0.05f;
        public Rect bounds;
        public readonly Vector2[] pos, vel;
        public readonly int[] slotOf;
        public readonly Vector2[] slots;
        readonly bool[] _slotFull;
        float _time;

        public PadDrift(Vector2[] pads, Vector2[] slots, Rect bounds)
        {
            pos = (Vector2[])pads.Clone();
            vel = new Vector2[pads.Length];
            slotOf = new int[pads.Length];
            for (int i = 0; i < slotOf.Length; i++) slotOf[i] = -1;
            this.slots = slots;
            _slotFull = new bool[slots.Length];
            this.bounds = bounds;
        }

        public int Count => pos.Length;
        public bool Locked(int i) => slotOf[i] >= 0;

        public bool AllIn
        {
            get
            {
                foreach (var f in _slotFull)
                    if (!f) return false;
                return true;
            }
        }

        /// <summary>Filled slots as a bit mask (saved stage).</summary>
        public int Mask
        {
            get
            {
                int m = 0;
                for (int i = 0; i < _slotFull.Length; i++)
                    if (_slotFull[i]) m |= 1 << i;
                return m;
            }
        }

        /// <summary>A fingertip pushed pad <paramref name="i"/> at this velocity (tiles / s). Ignored once locked.</summary>
        public void Push(int i, Vector2 velocity)
        {
            if (i < 0 || i >= pos.Length || Locked(i)) return;
            vel[i] = Vector2.ClampMagnitude(vel[i] + velocity, maxSpeed);
        }

        /// <summary>The nearest free slot to pad <paramref name="i"/>, or -1.</summary>
        public int NearestFree(int i)
        {
            int best = -1;
            float bestD = float.MaxValue;
            for (int s = 0; s < slots.Length; s++)
            {
                if (_slotFull[s]) continue;
                float d = (slots[s] - pos[i]).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    best = s;
                }
            }
            return best;
        }

        /// <summary>Look + pinch: pad <paramref name="i"/> heads for the nearest free slot by itself.</summary>
        public void Steer(int i)
        {
            if (i < 0 || i >= pos.Length || Locked(i)) return;
            int s = NearestFree(i);
            if (s < 0) return;
            // Just enough speed that the drag stops it at the slot (it locks a little before).
            var to = slots[s] - pos[i];
            vel[i] = to.normalized * (to.magnitude * drag * 1.1f);
        }

        /// <summary>Advances the drift; returns the pad that locked into a slot this step, or -1.</summary>
        public int Tick(float dt)
        {
            _time += dt;
            int locked = -1;
            for (int i = 0; i < pos.Length; i++)
            {
                if (Locked(i))
                {
                    // Glides the last bit into its slot.
                    pos[i] = Vector2.Lerp(pos[i], slots[slotOf[i]], 1f - Mathf.Exp(-glide * dt));
                    vel[i] = Vector2.zero;
                    continue;
                }
                // A faint idle drift so they never sit dead still.
                vel[i] += new Vector2(Mathf.Sin(_time * 0.7f + i * 2.1f), Mathf.Cos(_time * 0.5f + i * 1.3f)) * idle * dt;
                vel[i] *= Mathf.Exp(-drag * dt);
                pos[i] += vel[i] * dt;
                // Banks.
                if (pos[i].x < bounds.xMin + radius) { pos[i].x = bounds.xMin + radius; vel[i].x = Mathf.Abs(vel[i].x) * bounce; }
                if (pos[i].x > bounds.xMax - radius) { pos[i].x = bounds.xMax - radius; vel[i].x = -Mathf.Abs(vel[i].x) * bounce; }
                if (pos[i].y < bounds.yMin + radius) { pos[i].y = bounds.yMin + radius; vel[i].y = Mathf.Abs(vel[i].y) * bounce; }
                if (pos[i].y > bounds.yMax - radius) { pos[i].y = bounds.yMax - radius; vel[i].y = -Mathf.Abs(vel[i].y) * bounce; }
                if (locked < 0)
                {
                    int s = NearestFree(i);
                    if (s >= 0 && (slots[s] - pos[i]).magnitude <= snapRadius)
                    {
                        slotOf[i] = s;
                        _slotFull[s] = true;
                        vel[i] = Vector2.zero;
                        locked = i;
                    }
                }
            }
            // Pads bump each other (locked ones act as walls).
            for (int a = 0; a < pos.Length; a++)
                for (int b = a + 1; b < pos.Length; b++)
                {
                    var d = pos[b] - pos[a];
                    float m = d.magnitude, min = radius * 2f;
                    if (m >= min || m < 1e-5f) continue;
                    var n = d / m;
                    float push = min - m;
                    bool la = Locked(a), lb = Locked(b);
                    if (la && lb) continue;
                    if (!la) pos[a] -= n * (lb ? push : push * 0.5f);
                    if (!lb) pos[b] += n * (la ? push : push * 0.5f);
                    float rel = Vector2.Dot(vel[b] - vel[a], n);
                    if (rel >= 0f) continue;
                    var j = n * (-(1f + bounce) * rel * (la || lb ? 1f : 0.5f));
                    if (!la) vel[a] -= j;
                    if (!lb) vel[b] += j;
                }
            return locked;
        }

        /// <summary>From the save: slots in the mask are full (their pads locked there), the rest drift where they started.</summary>
        public void Restore(int mask, Vector2[] start)
        {
            for (int s = 0; s < _slotFull.Length; s++) _slotFull[s] = false;
            for (int i = 0; i < pos.Length; i++)
            {
                slotOf[i] = -1;
                vel[i] = Vector2.zero;
                pos[i] = i < start.Length ? start[i] : pos[i];
            }
            int pad = 0;
            for (int s = 0; s < slots.Length && pad < pos.Length; s++)
            {
                if ((mask & (1 << s)) == 0) continue;
                _slotFull[s] = true;
                slotOf[pad] = s;
                pos[pad] = slots[s];
                pad++;
            }
        }
    }

    /// <summary>
    /// Step 11b B: a hand held like a little cloud (pure, EditMode-tested). Palm down = the palm normal within
    /// <see cref="maxAngle"/> of straight down, at <see cref="minHeight"/>..<see cref="maxHeight"/> metres above the
    /// island, over the garden. Held <see cref="formSeconds"/> s, a cloud forms and rains while it stays; a finger wiggle
    /// (summed fingertip speed over <see cref="wiggleSpeed"/> m/s) rains harder. Drops under <see cref="grace"/> s keep it.
    /// </summary>
    public class RainCloud
    {
        public float maxAngle = 40f, minHeight = 0.05f, maxHeight = 0.25f, formSeconds = 0.6f, wiggleSpeed = 0.6f, grace = 0.3f, fade = 0.5f;
        float _held, _gone, _wiggle;

        /// <summary>0..1: the cloud forming (and fading when let go).</summary>
        public float Size { get; private set; }
        public bool Raining => Size >= 1f;
        /// <summary>Raining hard (fingers wiggling).</summary>
        public bool Hard { get; private set; }
        /// <summary>The hand that holds it, -1 = none.</summary>
        public int Holder { get; private set; } = -1;

        /// <summary>Palm down within the angle (normal · island up ≤ -cos angle).</summary>
        public static bool PalmDown(Vector3 normal, Vector3 up, float maxAngle) => Vector3.Angle(normal, -up) <= maxAngle;

        /// <summary>One frame. <paramref name="hand"/>: which hand is palm down over the garden in range (-1 none); its summed fingertip speed.</summary>
        public void Feed(float dt, int hand, float fingerSpeed)
        {
            if (hand >= 0 && (Holder < 0 || hand == Holder))
            {
                Holder = hand;
                _gone = 0f;
                _held += dt;
                Size = Mathf.Clamp01(_held / Mathf.Max(0.01f, formSeconds));
                // A short smoothing so one twitch isn't a wiggle.
                _wiggle = Mathf.Lerp(_wiggle, fingerSpeed, 1f - Mathf.Exp(-8f * dt));
                Hard = Raining && _wiggle >= wiggleSpeed;
                return;
            }
            if (Holder >= 0)
            {
                _gone += dt;
                if (_gone < grace) return;
                Holder = -1;
                _held = 0f;
                Hard = false;
                _wiggle = 0f;
            }
            Size = Mathf.Max(0f, Size - dt / Mathf.Max(0.01f, fade));
        }
    }

    /// <summary>
    /// Step 11b B: the closed lotus buds in the dry bed (pure). Each fills under the rain, 0 → 1 over
    /// <see cref="softSeconds"/> s (<see cref="hardSeconds"/> while it rains hard); full = it blooms (reported once).
    /// </summary>
    public class BudGarden
    {
        public float softSeconds = 2f, hardSeconds = 1f;
        public readonly float[] fill;

        public BudGarden(int count) => fill = new float[count];

        public int Count => fill.Length;
        public bool Bloomed(int i) => fill[i] >= 1f;

        public bool AllBloomed
        {
            get
            {
                foreach (var f in fill)
                    if (f < 1f) return false;
                return true;
            }
        }

        public int Mask
        {
            get
            {
                int m = 0;
                for (int i = 0; i < fill.Length; i++)
                    if (fill[i] >= 1f) m |= 1 << i;
                return m;
            }
        }

        /// <summary>Rain on bud <paramref name="i"/> for <paramref name="dt"/>. True on the step it blooms.</summary>
        public bool Rain(int i, float dt, bool hard)
        {
            if (i < 0 || i >= fill.Length || fill[i] >= 1f) return false;
            fill[i] = Mathf.Min(1f, fill[i] + dt / Mathf.Max(0.01f, hard ? hardSeconds : softSeconds));
            return fill[i] >= 1f;
        }

        public void Restore(int mask)
        {
            for (int i = 0; i < fill.Length; i++) fill[i] = (mask & (1 << i)) != 0 ? 1f : 0f;
        }
    }

    /// <summary>Where the scooped water is (step 11b C).</summary>
    public enum ScoopState { Empty, Dipped, Held, Pouring, Spilled }

    /// <summary>
    /// Step 11b C, scoop and pour (pure, EditMode-tested). A hand cupped palm up (normal · up ≥ <see cref="cupUp"/>)
    /// dipped into the lagoon (palm below the water surface + <see cref="dipDepth"/>) and lifted out above the surface
    /// + <see cref="liftHeight"/> while still palm up: a ball of water sits in it (<see cref="ScoopState.Held"/>). It
    /// stays while the palm stays roughly up (≥ <see cref="keepUp"/>); tipped over (≤ <see cref="tipUp"/>) above the
    /// basin it pours, anywhere else it spills. A hand lost for under <see cref="lostGrace"/> s keeps it. Pours fill the
    /// basin a step (<see cref="Pours"/>); <see cref="poursNeeded"/> fill it.
    /// </summary>
    public class WaterScoop
    {
        public struct Hand
        {
            public bool tracked, busy;
            /// <summary>Palm normal · island up (1 = palm straight up).</summary>
            public float up;
            /// <summary>Palm height above the water surface (metres; negative = under water) while over lagoon water.</summary>
            public float aboveWater;
            public bool overWater, overBasin;
        }

        public float cupUp = 0.55f, keepUp = 0.1f, tipUp = -0.35f, dipDepth = 0.01f, liftHeight = 0.02f, lostGrace = 0.3f;
        public int poursNeeded = 2;

        public ScoopState State { get; private set; } = ScoopState.Empty;
        public int Holder { get; private set; } = -1;
        public int Pours { get; private set; }
        public bool Full => Pours >= poursNeeded;
        float _lost;

        public ScoopState Feed(float dt, Hand[] hands)
        {
            switch (State)
            {
                case ScoopState.Empty:
                    for (int h = 0; h < hands.Length; h++)
                    {
                        var x = hands[h];
                        if (x.tracked && !x.busy && x.overWater && x.up >= cupUp && x.aboveWater <= dipDepth)
                        {
                            State = ScoopState.Dipped;
                            Holder = h;
                            return State;
                        }
                    }
                    break;
                case ScoopState.Dipped:
                {
                    var x = Hold(hands);
                    if (!x.tracked || x.up < keepUp) Reset();
                    else if (!x.overWater || x.aboveWater >= liftHeight) State = ScoopState.Held;
                    break;
                }
                case ScoopState.Held:
                {
                    var x = Hold(hands);
                    if (!x.tracked)
                    {
                        _lost += dt;
                        if (_lost >= lostGrace) State = ScoopState.Spilled;
                        break;
                    }
                    _lost = 0f;
                    if (x.up > tipUp) break;
                    if (x.overBasin && !Full)
                    {
                        State = ScoopState.Pouring;
                        Pours++;
                    }
                    else State = ScoopState.Spilled;
                    break;
                }
            }
            return State;
        }

        Hand Hold(Hand[] hands) => Holder >= 0 && Holder < hands.Length ? hands[Holder] : default;

        /// <summary>Look + pinch the water, then the basin: a ball floats over and pours (one step).</summary>
        public void PourByItself()
        {
            if (Full) return;
            Pours++;
            State = ScoopState.Pouring;
            Holder = -1;
        }

        /// <summary>The pour or spill finished playing: an empty hand again.</summary>
        public void Reset()
        {
            State = ScoopState.Empty;
            Holder = -1;
            _lost = 0f;
        }

        public void Restore(int pours)
        {
            Pours = Mathf.Clamp(pours, 0, poursNeeded);
            Reset();
        }
    }
}
