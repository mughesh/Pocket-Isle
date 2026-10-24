using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// First lodge visit, step by step (step 7c): a short hint chain where each line waits for its action, comes back
    /// after <see cref="repromptSeconds"/> without it, and never repeats once done. Its targets use the gold focus
    /// (<see cref="FocusHighlight"/>): while a step waits, its target pulses softly now and then.
    /// 1. lodge.first (room) → 2. lodge.inspect (nearest wandering Isling) until one is inspected →
    /// 3. lodge.take (the pedestal Isling, then a rim socket) until one joins the pods →
    /// 4. lodge.leave (a rim pod) until one is let go → 5. lodge.exit (the door arch), after step 4 or
    /// <see cref="exitAfterSeconds"/>, until the player leaves.
    /// Progress lives in the said lines (saved with the game): the last said step is the one still waiting.
    /// </summary>
    public class LodgeGuide : MonoBehaviour
    {
        public LodgeRoom room;
        public SceneSwap swap;
        [Tooltip("A waiting step's line comes back after this long without its action (s).")]
        public float repromptSeconds = 15f;
        [Tooltip("The exit hint comes this long after the leave hint even if no pod was let go (s).")]
        public float exitAfterSeconds = 30f;

        static readonly string[] Steps = { "lodge.first", "lodge.inspect", "lodge.take", "lodge.leave", "lodge.exit" };
        static readonly string[] StepTargets = { "lodgeCenter", "lodgeIsling", "pedestal", "rimPod", "lodgeDoor" };

        int _step = -1;          // index of the step whose action we wait for (-1 = not started, Steps.Length = all done)
        float _promptedAt, _stepStarted;
        bool _wasTalking, _socketPhase;

        void Start()
        {
            if (room == null || swap == null)
            {
                Debug.LogError("[LodgeGuide] Needs the lodge room and the scene swap (Set Up MR_Main (7c)).", this);
                enabled = false;
                return;
            }
            room.Acted += OnActed;
            swap.Returned += OnReturned;
        }

        void OnDestroy()
        {
            if (room != null) room.Acted -= OnActed;
            if (swap != null) swap.Returned -= OnReturned;
        }

        bool Said(string id)
        {
            foreach (var s in Dialogue.Said) if (s == id) return true;
            return false;
        }

        /// <summary>Where the chain stands from the said lines: the last said step still waits; all said = done.</summary>
        int Resume()
        {
            int last = -1;
            for (int i = 0; i < Steps.Length; i++) if (Said(Steps[i])) last = i;
            if (last == Steps.Length - 1) return Steps.Length;
            return last;
        }

        void Update()
        {
            if (!room.Active)
            {
                _step = -1;
                return;
            }
            if (SceneSwap.IsBusy) return;
            if (_step < 0)
            {
                int resume = Resume();
                if (resume >= Steps.Length)
                {
                    _step = resume;
                    return;
                }
                // Never heard anything: from the top. Heard the welcome only: on to inspecting. Otherwise the waiting step again.
                if (resume <= 0) Begin(resume < 0 ? 0 : 1);
                else Begin(resume, true);
                return;
            }
            if (_step >= Steps.Length) return;

            bool talking = Dialogue.IsTalking;
            if (_wasTalking && !talking) OnLineDone();
            _wasTalking = talking;

            // Step 1 waits for nothing but its line; step 4 gives way to the exit after a while.
            if (_step == 0 && !talking && Time.time - _promptedAt > 0.5f) Begin(1);
            else if (_step == 3 && Time.time - _stepStarted > exitAfterSeconds && !talking) Begin(4);
            else if (!talking && Time.time - _promptedAt > repromptSeconds)
            {
                Debug.Log($"[LodgeGuide] {Steps[_step]} again (no action for {repromptSeconds:0} s).");
                Prompt(true);
            }
        }

        void Begin(int step, bool resumed = false)
        {
            _step = step;
            _stepStarted = Time.time;
            _socketPhase = false;
            if (step >= Steps.Length)
            {
                FocusHighlight.Clear();
                Debug.Log("[LodgeGuide] All lodge hints done.");
                return;
            }
            Debug.Log($"[LodgeGuide] Step {step + 1}/{Steps.Length}: {Steps[step]}{(resumed ? " (resumed)" : "")}.");
            Prompt(resumed);
        }

        void Prompt(bool force)
        {
            _promptedAt = Time.time;
            _socketPhase = false;
            var d = FindDialogue();
            if (d != null) d.Fire(Steps[_step], force);
            Pending(StepTargets[_step]);
        }

        /// <summary>After lodge.take's line, the gold moves on from the pedestal to the rim socket ("then").</summary>
        void OnLineDone()
        {
            if (_step == 2 && !_socketPhase)
            {
                _socketPhase = true;
                Pending("rimSocket");
            }
        }

        static void Pending(string targetId)
        {
            if (Dialogue.TryGetTarget(targetId, out var t) && t.point != null)
                FocusHighlight.SetPending(t.point, t.radius != null ? t.radius() : 0f);
        }

        void OnActed(string what)
        {
            if (_step < 0 || _step >= Steps.Length) return;
            int next = what == "inspect" && _step == 1 ? 2
                     : what == "swap" && _step <= 2 && _step >= 1 ? 3
                     : what == "deposit" && _step == 3 ? 4
                     : -1;
            if (next < 0) return;
            for (int i = 1; i < next; i++) Dialogue.Cancel(Steps[i]);
            FocusHighlight.Clear();
            Begin(next);
        }

        void OnReturned(WorldMap map)
        {
            if (_step == 4)
            {
                // Leaving was the last step's action: mark it done.
                _step = Steps.Length;
                FocusHighlight.Clear();
            }
            _step = -1;
        }

        Dialogue _dialogue;

        Dialogue FindDialogue()
        {
            if (_dialogue == null) _dialogue = FindFirstObjectByType<Dialogue>();
            return _dialogue;
        }
    }
}
