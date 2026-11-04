using UnityEngine;

namespace Diorama
{
    /// <summary>Where one hand's island grab stands (step 14i).</summary>
    public enum GrabHoldState
    {
        /// <summary>Not pinching (or fisting).</summary>
        Idle,
        /// <summary>Pinching on a handle: the gold ring fills.</summary>
        Arming,
        /// <summary>The ring filled: the island is held until the hand opens.</summary>
        Held,
        /// <summary>The hand moved off the handle while arming: this pinch goes nowhere until it opens.</summary>
        Cancelled,
        /// <summary>The pinch belongs to something else (a look + pinch, a pod, a walk).</summary>
        Select,
    }

    /// <summary>
    /// Step 14i (Mugesh rests his hand on the armrest and pinches near a corner): one hand's hold-to-grab, pure. A pinch
    /// (or fist) that starts on a handle arms the grab and fills over <see cref="holdSeconds"/> (or <see cref="joinSeconds"/>
    /// when the other hand already holds the island); full = held. Opening the hand before it fills does nothing, and
    /// moving more than <see cref="cancelDistance"/> off the spot cancels it. A pinch that starts while the gaze is on
    /// something else, or with the hand below the island's base, never arms. While arming, held or cancelled the pinch is
    /// the island's (<see cref="Busy"/>): it never walks or selects.
    /// </summary>
    public class GrabHold
    {
        public float holdSeconds = 0.6f;
        public float joinSeconds = 0.3f;
        public float cancelDistance = 0.04f;

        public GrabHoldState State { get; private set; }
        /// <summary>The ring, 0..1 (1 while held).</summary>
        public float Fill { get; private set; }
        /// <summary>This arm joins a grab the other hand already holds (the shorter hold).</summary>
        public bool Join { get; private set; }
        public bool Gripping { get; private set; }
        /// <summary>Why the last pinch went where it did (logs).</summary>
        public string Why { get; private set; } = "";
        /// <summary>Where the arm started (island-local).</summary>
        public Vector3 ArmLocal { get; private set; }

        /// <summary>The pinch is the island's: gaze walks, selects and microgestures ignore it.</summary>
        public bool Busy => State == GrabHoldState.Arming || State == GrabHoldState.Held || State == GrabHoldState.Cancelled;

        /// <summary>
        /// One frame. <paramref name="local"/> is the hand's grab point in island space and <paramref name="scale"/> the
        /// island's world scale (the cancel distance is in world metres). Returns true on the frame the ring fills.
        /// </summary>
        public bool Step(bool grip, bool onHandle, bool gazeAway, bool below, bool otherHeld, Vector3 local, float scale, float dt)
        {
            bool start = grip && !Gripping;
            Gripping = grip;
            if (!grip)
            {
                State = GrabHoldState.Idle;
                Fill = 0f;
                Join = false;
                return false;
            }
            if (start)
            {
                Fill = 0f;
                Join = otherHeld;
                if (!onHandle) Decide(GrabHoldState.Select, "not on a handle");
                else if (gazeAway) Decide(GrabHoldState.Select, "the gaze is on something else: look + pinch");
                else if (below) Decide(GrabHoldState.Select, "the hand is below the island (resting)");
                else
                {
                    ArmLocal = local;
                    Decide(GrabHoldState.Arming, Join ? "joining the other hand's grab" : "arming");
                }
                return false;
            }
            if (State != GrabHoldState.Arming) return false;
            if ((local - ArmLocal).magnitude * Mathf.Max(1e-4f, scale) > cancelDistance)
            {
                Decide(GrabHoldState.Cancelled, "moved off the handle while arming");
                Fill = 0f;
                return false;
            }
            Join |= otherHeld;
            Fill += dt / Mathf.Max(0.01f, Join ? joinSeconds : holdSeconds);
            if (Fill < 1f) return false;
            Fill = 1f;
            Decide(GrabHoldState.Held, Join ? "joined" : "held");
            return true;
        }

        /// <summary>The grab ended from outside (the island let go, a scene swap): wait for the hand to open.</summary>
        public void Drop()
        {
            if (State == GrabHoldState.Held || State == GrabHoldState.Arming) State = Gripping ? GrabHoldState.Cancelled : GrabHoldState.Idle;
            Fill = 0f;
        }

        void Decide(GrabHoldState state, string why)
        {
            State = state;
            Why = why;
        }
    }
}
