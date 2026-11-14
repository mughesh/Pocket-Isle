using System.Collections;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 34: a short hand moment that wakes a Keeper in its hall before the meeting, the way Galeowl's untangle gates
    /// it (<see cref="KeeperHall.wake"/> takes any <see cref="HandPuzzle"/>). While it's unsolved the Keeper waits in its
    /// "before" pose: this piece draws a second baked body of it (<see cref="beforePrefab"/>: Tidemother withdrawn into
    /// its shell, Hearthstag kneeling) and the hall hides its own model. Solved (saved: its own puzzle id, stage = steps
    /// done), the hall starts the meeting and plays <see cref="WakeBeat"/> first: the before body swaps for the hall's
    /// standing model under a burst. A Keeper already calmed skips it; a lost or fled battle doesn't reset it.
    /// The moment never walks the explorer (<see cref="GestureTestPiece.HeldStill"/> while it runs, hands near the body
    /// are blocked from tile touch) and lets go on every exit (log <c>[HandPuzzle] lock / unlock &lt;id&gt;</c>). With
    /// nothing happening for <see cref="hintEvery"/> s the Spirit repeats the hint.
    /// </summary>
    public abstract class KeeperWakePiece : HandPuzzle
    {
        [Header("Keeper wake (step 34)")]
        public KeeperHall keeper;
        public SceneSwap swap;
        [Tooltip("The Keeper's baked 'before' body. Empty = the hall's own model (dazed) is used.")]
        public GameObject beforePrefab;
        [Tooltip("Hands this close to the Keeper (share of its height) don't walk the explorer.")]
        public float touchGuard = 0.9f;
        [Tooltip("Nothing done for this long (s): the Spirit repeats the hint.")]
        public float hintEvery = 20f;
        [Range(0f, 1f)] public float volume = 0.8f;

        /// <summary>The before body (null until the hall first shows, or with no prefab).</summary>
        protected StageActor Body;
        protected KeeperLook BodyLook;
        protected KeeperFury BodyFury;
        protected readonly HandState[] Hands = new HandState[2];
        /// <summary>Solved just now: the before body stays until <see cref="WakeBeat"/> swaps it.</summary>
        protected bool Waking;
        bool _held;
        float _quiet, _wakingFor;

        /// <summary>The before body is drawn (the hall's own model hides).</summary>
        public bool BeforeShown => Body != null && Body.gameObject.activeSelf;
        /// <summary>Hints repeated so far (tests, the bridge).</summary>
        public int Hints { get; private set; }

        bool Inside => island != null && swap != null && map != null && swap.Inside && swap.Interior == map;
        public override bool Here => Inside && !SceneSwap.IsBusy;
        protected override bool Available => !IsSolved && keeper != null && keeper.Model != null && !keeper.Calmed && keeper.Idle;
        protected override Vector2 HomeTile => keeper != null ? keeper.keeperSpot : Vector2.zero;
        protected float Scale => Mathf.Max(1e-4f, island.transform.lossyScale.x);

        /// <summary>The body the player acts on: the before body, or the hall's model when there is none.</summary>
        protected StageActor Actor => BeforeShown ? Body : keeper != null ? keeper.Model : null;

        /// <summary>World size of the acted-on body's height.</summary>
        protected float BodyHeight(StageActor a) => a.View.height * island.Zoom * Scale * a.grow;

        protected override void Start()
        {
            if (keeper == null || swap == null)
            {
                Debug.LogError($"[{GetType().Name}] Needs the Keeper hall and the scene swap (Tools > Pocket Isle > Setup > Step 34).", this);
                enabled = false;
                return;
            }
            if (beforePrefab == null) Debug.LogError($"[{GetType().Name}] {name}: no 'before' body: the hall's own model is used.", this);
            base.Start();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            SetHeld(false, "destroyed");
            if (Body != null) Destroy(Body.gameObject);
        }

        protected virtual void OnDisable() => SetHeld(false, "disabled");

        /// <summary>
        /// Which body shows (pure, tested): the before body while the player is in the hall, the Keeper isn't calmed and the
        /// moment is unsolved (or its waking beat still plays).
        /// </summary>
        public static bool WantsBefore(bool inside, bool calmed, bool solved, bool waking) => inside && !calmed && (!solved || waking);

        /// <summary>The moment runs (pure, tested): never once solved, calmed, or while the hall meets, fights or celebrates.</summary>
        public static bool Runs(bool here, bool calmed, bool solved, bool hallIdle, bool usable) => here && !calmed && !solved && hallIdle && usable;

        protected override void Update()
        {
            ReadHands(Hands);
            bool inside = Inside && keeper != null;
            if (Waking)
            {
                // Failsafe: the hall never played the beat (left mid-way): the standing model comes back.
                _wakingFor += Time.deltaTime;
                if (!inside || _wakingFor > 8f) Waking = false;
            }
            else _wakingFor = 0f;
            ShowBody(beforePrefab != null && inside && WantsBefore(true, keeper.Calmed, IsSolved, Waking));
            var actor = Actor;
            bool here = Here && actor != null && actor.gameObject.activeInHierarchy;
            if (here && !IsSolved) GuardTouch(actor);
            bool working = !Waking && Runs(here, keeper != null && keeper.Calmed, IsSolved, keeper != null && keeper.Idle, Usable) && Available;
            SetHeld(working, working ? "the wake moment is on" : IsSolved ? "woken" : "not usable (left, a menu, a swap)");
            if (working)
            {
                Work(actor);
                Hint();
            }
            else
            {
                Rest();
                _quiet = 0f;
            }
            if (BeforeShown) Pose(Body);
            base.Update();
        }

        void ShowBody(bool show)
        {
            if (show && Body == null)
            {
                var go = Instantiate(beforePrefab, island.transform);
                go.name = $"{keeper.KeeperLabel} (before)";
                Body = go.AddComponent<StageActor>();
                Body.island = island;
                Body.tile = keeper.keeperSpot;
                Body.yaw = 0f;
                Body.baseLift = keeper.keeperLift * island.map.tileSize;
                BodyLook = keeper.DressLook(go);
                BodyLook.calm = 0f;
                BodyFury = keeper.DressFury(go);
                BodyFury.idleStomps = false;
                BodyFury.snorts = false;
                Dress(Body);
                Debug.Log($"[{GetType().Name}] {id}: {keeper.KeeperLabel} waits in its 'before' pose.");
            }
            if (Body != null && Body.gameObject.activeSelf != show) Body.gameObject.SetActive(show);
        }

        /// <summary>The before body goes, the hall's standing model shows (the same frame: no gap).</summary>
        protected void SwapToStanding(KeeperHall hall)
        {
            Waking = false;
            ShowBody(false);
            if (hall.Model != null)
            {
                hall.Model.gameObject.SetActive(true);
                hall.Model.View.Revive();
            }
        }

        void SetHeld(bool on, string why)
        {
            if (on == _held) return;
            _held = on;
            if (explorer != null) explorer.HeldStill = on;
            Debug.Log($"[HandPuzzle] {(on ? "lock" : "unlock")} {id}: {why}.");
        }

        /// <summary>Hands near the Keeper never walk the explorer (pokes, cupped palms, a dip toward the floor).</summary>
        void GuardTouch(StageActor actor)
        {
            float guard = BodyHeight(actor) * touchGuard;
            var c = actor.WorldCenter;
            for (int h = 0; h < 2; h++)
            {
                var s = Hands[h];
                if (!s.tracked) continue;
                if ((s.palm - c).magnitude < guard + 0.06f || s.pinch && (s.pinchPoint - c).magnitude < guard) TileTouch.Block(h == 0, 0.8f);
            }
        }

        void Hint()
        {
            if (Busy)
            {
                _quiet = 0f;
                return;
            }
            _quiet += Time.deltaTime;
            if (_quiet < hintEvery || string.IsNullOrEmpty(lessonLine)) return;
            _quiet = 0f;
            Hints++;
            Dialogue.Repeat(lessonLine);
            Debug.Log($"[{GetType().Name}] {id}: nothing for {hintEvery:0} s, the Spirit repeats the hint ({lessonLine}).");
        }

        /// <summary>Something was done (a knock, a cup): the lesson's ghost goes, the hint clock restarts.</summary>
        protected void Touched()
        {
            _quiet = 0f;
            MarkInteracted();
        }

        /// <summary>The before body was just made: hide or pose what the bake can't.</summary>
        protected virtual void Dress(StageActor body)
        {
        }

        /// <summary>Every frame the before body shows: breathing, the dying fire.</summary>
        protected virtual void Pose(StageActor body)
        {
        }

        /// <summary>Every frame the moment can be worked: read the hands.</summary>
        protected abstract void Work(StageActor actor);

        /// <summary>Every frame it can't (left the hall, a menu, solved): let go.</summary>
        protected virtual void Rest()
        {
        }

        /// <summary>The waking beat, played by the hall before the meeting: ends with <see cref="SwapToStanding"/> done.</summary>
        public abstract IEnumerator WakeBeat(KeeperHall hall);

        /// <summary>Debug (bridge, smoke run): one step by look + pinch (a knock; the cup held a while).</summary>
        public abstract void DebugLookPinch();

        public abstract string Describe();
    }
}
