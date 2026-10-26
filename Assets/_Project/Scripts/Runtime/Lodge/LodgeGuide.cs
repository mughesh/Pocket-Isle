using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// First lodge visit, step by step (step 7c): a short hint chain where each line waits for its action, comes back
    /// after <see cref="repromptSeconds"/> without it, and never repeats once done. Its targets use the gold focus
    /// (<see cref="FocusHighlight"/>): while a step waits, its target pulses softly now and then.
    /// Two chains, picked on the first visit (step 8c):
    /// Lodge with Islings: 1. lodge.first (room) → 2. lodge.inspect (nearest wandering Isling) until one is inspected →
    /// 3. lodge.take (the pedestal Isling, then a rim socket) until one joins the pods → 4. lodge.leave (a rim pod) until
    /// one is let go → 5. lodge.exit (the door arch), after step 4 or <see cref="exitAfterSeconds"/>, until the player leaves.
    /// Empty lodge (the usual first visit: the caught Isling rides in the rim pods): 1. lodge.first.empty → 2. lodge.toss
    /// (a rim pod, ghost toss) until one is let go → 3. lodge.inspect.one (it) → 4. lodge.take → 5. lodge.exit. With a
    /// single Isling (nothing to leave) it goes from the welcome to the exit. Nothing is dropped automatically.
    /// Progress lives in the said lines (saved with the game): the welcome said tells the chain, the last said step is
    /// the one still waiting.
    /// </summary>
    public class LodgeGuide : MonoBehaviour
    {
        public LodgeRoom room;
        public SceneSwap swap;
        [Tooltip("Optional: ghost hands for the toss step (step 8c).")]
        public GhostHands ghosts;
        [Tooltip("Optional: which hand the player used last (the ghost shows that one).")]
        public Onboarding onboarding;
        [Tooltip("A waiting step's line comes back after this long without its action (s).")]
        public float repromptSeconds = 15f;
        [Tooltip("The exit hint comes this long after the leave / toss hint even if no pod was let go (s).")]
        public float exitAfterSeconds = 30f;

        /// <summary>One hint chain: per step its line, gold target, the room action it waits for (null = only its line) and ghost clip.</summary>
        sealed class Chain
        {
            public string name;
            public string[] steps, targets, waits, ghosts;
            /// <summary>This step gives way to the exit after exitAfterSeconds.</summary>
            public int giveUp;
        }

        static readonly Chain Full = new Chain
        {
            name = "lodge with Islings",
            steps = new[] { "lodge.first", "lodge.inspect", "lodge.take", "lodge.leave", "lodge.exit" },
            targets = new[] { "lodgeCenter", "lodgeIsling", "pedestal", "rimPod", "lodgeDoor" },
            waits = new[] { null, "inspect", "swap", "deposit", null },
            ghosts = new string[5],
            giveUp = 3,
        };

        static readonly Chain Empty = new Chain
        {
            name = "empty lodge",
            steps = new[] { "lodge.first.empty", "lodge.toss", "lodge.inspect.one", "lodge.take", "lodge.exit" },
            targets = new[] { "lodgeCenter", "rimPod", "lodgeIsling", "pedestal", "lodgeDoor" },
            waits = new[] { null, "deposit", "inspect", "swap", null },
            ghosts = new[] { null, "grab_toss", null, null, null },
            giveUp = 1,
        };

        Chain _chain;
        int _step = -1;          // index of the step whose action we wait for (-1 = not started, steps.Length = all done)
        float _promptedAt, _stepStarted;
        bool _wasTalking, _socketPhase;

        string[] Steps => _chain.steps;
        int Exit => Steps.Length - 1;

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

        static bool Said(string id)
        {
            foreach (var s in Dialogue.Said) if (s == id) return true;
            return false;
        }

        /// <summary>The chain: the one whose welcome was said, else by the room (no stored Islings = the empty lodge chain).</summary>
        Chain Pick()
        {
            if (Said(Empty.steps[0])) return Empty;
            if (Said(Full.steps[0])) return Full;
            return room.party != null && room.party.storage.Count == 0 ? Empty : Full;
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
                _chain = Pick();
                int resume = Resume();
                if (resume >= Steps.Length)
                {
                    _step = resume;
                    return;
                }
                Debug.Log($"[LodgeGuide] Hint chain: {_chain.name}.");
                // Never heard anything: from the top. Heard the welcome only: on to the next. Otherwise the waiting step again.
                if (resume <= 0) Begin(resume < 0 ? 0 : 1);
                else Begin(resume, true);
                return;
            }
            if (_step >= Steps.Length) return;

            bool talking = Dialogue.IsTalking;
            if (_wasTalking && !talking) OnLineDone();
            _wasTalking = talking;

            // The welcome waits for nothing but its line; the give-up step makes way for the exit after a while.
            if (_step == 0 && !talking && Time.time - _promptedAt > 0.5f) Begin(1);
            else if (_step == _chain.giveUp && Time.time - _stepStarted > exitAfterSeconds && !talking) Begin(Exit);
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
            HideGhost();
            if (step >= Steps.Length)
            {
                FocusHighlight.Clear();
                Debug.Log("[LodgeGuide] All lodge hints done.");
                return;
            }
            // Empty lodge with a single Isling: nothing to leave here, so straight to the door.
            if (_chain == Empty && step == 1 && room.party != null && room.party.members.Count < 2)
            {
                Debug.Log("[LodgeGuide] Only one Isling in the pods: skipping the toss, inspect and take hints.");
                Begin(Exit);
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
            Pending(_chain.targets[_step]);
            string ghost = _chain.ghosts[_step];
            if (ghosts != null && !string.IsNullOrEmpty(ghost))
                // Step 9h: a grab ghost grabs the real pod (the step's target) and lets go over the room's middle.
                ghosts.Show(ghost, GhostPlace.AtTarget, "lodgeCenter", 1f, DominantHand.Left, false, 0f, _chain.targets[_step]);
        }

        void HideGhost()
        {
            if (ghosts != null && ghosts.ClipName != null && !ghosts.previewing) ghosts.Hide();
        }

        /// <summary>After lodge.take's line, the gold moves on from the pedestal to the rim socket ("then").</summary>
        void OnLineDone()
        {
            if (Steps[_step] == "lodge.take" && !_socketPhase)
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

        /// <summary>The waiting step's action moves on; so does the next step's (done ahead: that hint is skipped).</summary>
        void OnActed(string what)
        {
            if (_step < 0 || _step >= Steps.Length) return;
            int next = -1;
            if (_chain.waits[_step] == what) next = _step + 1;
            else if (_step + 1 < Steps.Length && _chain.waits[_step + 1] == what) next = _step + 2;
            if (next < 0) return;
            for (int i = 1; i < next; i++) Dialogue.Cancel(Steps[i]);
            FocusHighlight.Clear();
            Begin(next);
        }

        void OnReturned(WorldMap map)
        {
            HideGhost();
            // Step 9b: the Forge Hall is an interior too; without a lodge visit this session there is no chain.
            if (_chain != null && _step == Exit)
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
