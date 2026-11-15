using System;
using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Diorama
{
    /// <summary>
    /// The new game's lessons (step 8b), one at a time from <see cref="OnboardingScript"/> (Data/Onboarding/Onboarding_Main):
    /// each waits for its start, says its line and keeps it up until the lesson is done (step 9h: <see cref="Dialogue.Pin"/>,
    /// the bubble shrinks to the lesson's hint chip; if the line closes anyway it comes back with the shorter wording),
    /// shows its ghost hand (<see cref="GhostHands"/> listens to <see cref="LessonShown"/>) and the gold focus,
    /// and on done plays a soft chime and the Spirit's praise. Wild battles and fishing stay off until the chain is done
    /// (<see cref="EncountersEnabled"/>); the grass lesson's tutorial patch meets the scripted first species. A battle lost
    /// or fled mid-tutorial goes back to the grass lesson. The current lesson is saved (<see cref="GameSave"/>): quitting
    /// resumes there, and a finished tutorial never shows again.
    /// The <see cref="Opening"/> starts it (<see cref="Begin"/>) and marks the opening's own events (shard touched, island
    /// grabbed, starter picked). If no hand is tracked for <see cref="handsMissingSeconds"/> during a lesson (headset only),
    /// the Spirit asks for them (<see cref="HandsMissing"/>).
    /// Editor and development builds: J completes the current lesson, Shift+J finishes the tutorial.
    /// </summary>
    [DefaultExecutionOrder(90)] // before GameSave (100) restores the saved lesson
    public class Onboarding : MonoBehaviour
    {
        public OnboardingScript script;
        public GestureTestPiece explorer;
        public EncounterDirector encounters;
        public IslandBuilder island;
        public IslandHandle handle;
        public MicrogestureInput input;
        [Tooltip("Saves when a lesson ends (the save is suspended until the starter is picked).")]
        public GameSave save;

        [Header("Tutorial grass")]
        [Tooltip("Landmark in the middle of the tutorial patch.")]
        public string patchLandmark = "firstGrass";
        [Tooltip("Tall grass this many tiles (or fewer) from the landmark is the patch.")]
        [Min(0)] public int patchRadius = 1;

        [Header("Walk lessons")]
        [Tooltip("Steps that complete the walk lesson.")]
        [Min(1)] public int walkSteps = 3;

        [Header("Hands")]
        public float handsMissingSeconds = 3f;

        [Header("Pinch lesson (step 9h)")]
        [Tooltip("The lesson that counts pinches (either hand).")]
        public string pinchLesson = "pinch";
        [Tooltip("Step 14a: after the first of two pinches the line becomes this trigger (\"Nice! Once more.\") and the chip this.")]
        public string pinchOnceLine = "lesson.pinch.once";
        public string pinchAgainChip = "Pinch again";
        [Tooltip("Step 14a: the pinch lesson's praise (\"Perfect!\").")]
        public string pinchDoneLine = "lesson.pinch.done";
        [Min(1)] public int pinchCount = 2;
        [Tooltip("Step 12a: Diorama/GoldMark (Materials/GoldMark) for the lesson's gold tile.")]
        public Material goldMaterial;

        [Tooltip("Pooled particles for the gold sparkle at the fingertips on each pinch.")]
        public BattleFx fx;
        public Color sparkleColor = new Color(1f, 0.82f, 0.35f);

        /// <summary>Lesson event names (anything else in a condition is a <see cref="GameConditions"/> state).</summary>
        public static readonly string[] Events =
        {
            "shardTouched", "islandGrabbed", "starterPicked", "walked3", "stopped", "gazeWalk", "walkedOn",
            "grassBattle", "sentOut", "moveUsed", "podThrown", "wonOrCaught", "battleFailed", "twoHandGrab", "pinched",
            // Nov 3 (Mugesh: one swipe moved one tile, then into the grass, and the lesson never counted it): tiles really
            // walked by swipes, any way; a tap that stops a walk-on; three walk-ons (the stop lesson never blocks).
            "swipeWalked", "tapStopped", "walkedOn3",
            // Step 14f: the last walk lesson ("Swipe to walk, tap to stop") also passes after two finished swipe walks.
            "swipeWalks2",
            // Step 16: the two-hand lesson passes after twoHandsPassSeconds (one-handed players never get stuck).
            "twoHandsTimedOut",
            // Step 27: the practice stops are done, or the time is up (never a wall).
            "practiceDone",
        };

        /// <summary>Step 27: the swipe-and-tap practice (three gold stops, two in the Quick Journey).</summary>
        public const string PracticeLesson = "practice";
        [Tooltip("Step 27: the practice passes after this long (s), however many stops were made.")]
        public float practicePassSeconds = 60f;
        [Tooltip("Step 27: a good stop's short line, and the practice's last line.")]
        public string practiceNiceLine = "lesson.practice.nice", practiceDoneLine = "lesson.practice.done";
        [Tooltip("Step 32: after the second stop: the last round stops on the gold.")]
        public string practiceGoldLine = "lesson.practice.gold";
        [Tooltip("Step 32: the chip while standing, and while the explorer walks (the ghost then shows a tap).")]
        public string practiceSwipeChip = "Swipe to walk", practiceTapChip = "Tap to stop";
        [Tooltip("Step 32: during the walking lesson the explorer stays within this many tiles of the landmark (a square), " +
                 "turning back toward it at the edge, so it never reaches the island's edge however long the player waits.")]
        public string lessonAreaLandmark = "statue";
        [Min(2)] public int lessonAreaRadius = 6;
        float _practiceFor;
        int _practiced, _practiceBest;
        bool _practiceWalking;
        bool Practising => CurrentId == PracticeLesson && Current != null;

        [Tooltip("Step 16: the two-hand (turn, resize) lesson passes after this long without a two-hand grab (s).")]
        public float twoHandsPassSeconds = 8f;
        float _twoHandsFor;

        /// <summary>Nov 3: the walk lesson's id and how many tiles swipes must really walk (any direction).</summary>
        public const string WalkLesson = "walk", FarLesson = "far";
        [Tooltip("Nov 12 evening (Mugesh: one swipe that walked four tiles filled all four dots): swipes in the walk lesson, any " +
                 "direction; a swipe counts once the explorer takes a step from it.")]
        [Min(1)] public int swipeTiles = 4;
        [Tooltip("Nov 12 evening: the walk lesson's line after the first swipe, the middle ones and the last-but-one (like the pinch lesson's \"Nice! Once more.\").")]
        public string swipeNiceLine = "lesson.walk.nice", swipeMoreLine = "lesson.walk.more", swipeLastLine = "lesson.walk.last";
        public string swipeAgainChip = "Swipe again";

        static Onboarding _instance;

        /// <summary>False during the tutorial: tall grass only rustles and fishing is off (the tutorial patch excepted).</summary>
        public static bool EncountersEnabled { get; private set; } = true;

        /// <summary>A lesson chain is running.</summary>
        public static bool Running => _instance != null && _instance._runner != null && _instance._runner.IsRunning;

        /// <summary>Step 17: the running lesson's id (move, twoHands, ...), or null.</summary>
        public static string Lesson => _instance != null ? _instance.CurrentId : null;

        /// <summary>
        /// Step 28: the walk lessons and the tap practice are on: the explorer walks slower, never stops by itself and
        /// nothing on the island reacts to it. Read from the saved lesson, so a reload mid-tutorial resumes it.
        /// </summary>
        public static bool LessonWalking
        {
            get
            {
                var id = Lesson;
                return id == WalkLesson || id == FarLesson || id == PracticeLesson;
            }
        }

        /// <summary>Smoke run: the swipe-and-tap lesson's time limit waits (its walk beats take longer than a minute).</summary>
        [NonSerialized] public static bool DebugHoldPass;

        /// <summary>Step 32: the lesson walk area's middle (the statue), while a walking lesson runs on a map that has it.</summary>
        public static Vector2Int? LessonAreaCentre
        {
            get
            {
                if (_instance == null || !LessonWalking || _instance.island == null || _instance.island.world == null) return null;
                return _instance.island.world.TryGetLandmark(_instance.lessonAreaLandmark, out var c) ? c : (Vector2Int?)null;
            }
        }

        /// <summary>
        /// Step 32: a lesson walk planned from <paramref name="from"/> may not step onto <paramref name="tile"/>: it lies
        /// outside the lesson area and further out than the walk already is (a walk that starts outside can come back in).
        /// </summary>
        public static bool LessonAreaBlocks(Vector2Int from, Vector2Int tile)
        {
            var c = LessonAreaCentre;
            if (!c.HasValue) return false;
            int r = _instance.lessonAreaRadius;
            return StopPractice.Distance(tile, c.Value) > Mathf.Max(r, StopPractice.Distance(from, c.Value));
        }

        /// <summary>Step 32: the ghost the swipe-and-tap lesson wants now: a swipe while standing, a tap while walking; null otherwise.</summary>
        public static string PracticeGhost =>
            _instance != null && _instance.Practising && _instance._shown && _instance.explorer != null
                ? _instance.explorer.Heading != Vector2Int.zero ? "thumb_tap" : GhostHands.SwipeAllClip
                : null;

        /// <summary>A lesson starts teaching: show its ghost.</summary>
        public event Action<LessonData> LessonShown;
        /// <summary>The lesson being taught ended (done = true with praise; false = skipped, rewound or interrupted).</summary>
        public event Action<LessonData, bool> LessonEnded;
        /// <summary>No hand tracked for a while during a lesson (true), or they came back (false).</summary>
        public event Action<bool> HandsMissingChanged;

        public LessonRunner Runner => _runner;
        public LessonData Current => _runner != null && _runner.State == LessonRunner.Phase.Teaching ? _runner.Current : null;
        public bool IsFinished => _runner != null && _runner.IsFinished;

        /// <summary>Nov 3: the tutorial is over (moves, look, grass, the first catch); true with no tutorial in the scene.</summary>
        public static bool Finished => _instance == null || _instance.IsFinished;
        /// <summary>The saved id of the lesson the player is on (null when finished or not started).</summary>
        public string CurrentId => _runner != null && _runner.IsRunning && _runner.Current != null ? _runner.Current.id : null;
        public bool HandsMissing { get; private set; }
        public Vector2Int? GoldTile => _goldTile;

        LessonRunner _runner;
        int _steps, _praiseIndex, _pinches;
        readonly bool[] _wasPinching = { true, true };
        float _lineAway, _untracked;
        bool _helped, _shown;
        Vector2Int? _goldTile;
        Coroutine _goldPath;

        void Awake()
        {
            if (_instance != null && _instance != this) Debug.LogError("[Onboarding] Two Onboarding components in the scene.", this);
            _instance = this;
            if (script == null)
            {
                Debug.LogError("[Onboarding] No OnboardingScript (Tools > Pocket Isle > Set Up MR_Main (8b onboarding)): no lessons.", this);
                return;
            }
            _runner = new LessonRunner(script.lessons, Events) { Skip = QuickJourney.SkipsLesson, SkipAfter = QuickJourney.SkipAfter };
        }

        void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                EncountersEnabled = true;
            }
            if (explorer != null)
            {
                explorer.OnStep -= OnStep;
                explorer.OnLessonStep -= OnStep;
                explorer.Stopped -= OnStopped;
                explorer.Swiped -= OnSwiped;
                explorer.walkStops.Remove(GoldStop);
            }
            if (encounters != null) encounters.BattleEnded -= OnBattleEnded;
        }

        void Start()
        {
            if (explorer == null || encounters == null || island == null)
            {
                Debug.LogError("[Onboarding] Missing explorer, encounters or island (Set Up MR_Main (8b onboarding)).", this);
                enabled = false;
                return;
            }
            explorer.OnStep += OnStep;
            explorer.OnLessonStep += OnStep;
            explorer.Stopped += OnStopped;
            explorer.Swiped += OnSwiped;
            explorer.walkStops.Add(GoldStop);
            encounters.BattleEnded += OnBattleEnded;
            _goldMarks = new GoldMarks("Lesson Gold Tile", island, goldMaterial) { intensity = 1.5f };

            GameConditions.Register("battle.send", () => encounters != null && encounters.AwaitingSendOut);
            GameConditions.Register("battle.out", () => encounters != null && encounters.PlayerOut);
            // Nov 15: as the player sees it (the cradle glow, after the turn has played), not the battle's numbers, which run a turn ahead.
            GameConditions.Register("wildRed", () => encounters != null && encounters.CatchShown);
            GameConditions.Register("canWalk", () => explorer != null && explorer.CanMove && !Opening.InProgress && !SceneSwap.IsBusy && !SceneSwap.IsInside);
            // Step 12a: smart steps since the current lesson showed (the walk lessons never block).
            GameConditions.Register("smartSteps3", () => explorer != null && explorer.SmartStepsTaken - _smartAtShow >= 3);
            GameConditions.Register("smartSteps4", () => explorer != null && explorer.SmartStepsTaken - _smartAtShow >= 4);
            GameConditions.Register("atGoldTile", () => _goldTile.HasValue && explorer != null && explorer.Tile == _goldTile.Value && !explorer.Destination.HasValue);
            GameConditions.Register("tutorial", () => Running);
            GameConditions.Register("encounters", () => EncountersEnabled);

            Dialogue.RegisterTarget("goldTile", GoldTilePoint, GoldTilePulse,
                () => island == null || island.map == null ? 0f : 0.5f * island.map.tileSize * island.Zoom * island.transform.lossyScale.x);

            // Step 9h: every ghost needs somewhere to play.
            foreach (var l in script.lessons)
                if (!string.IsNullOrEmpty(l.ghost) && (l.place == GhostPlace.AtTarget || l.place == GhostPlace.AboveTarget) &&
                    string.IsNullOrEmpty(l.source) && string.IsNullOrEmpty(l.target))
                    Debug.LogWarning($"[Onboarding] Lesson '{l.id}' has a ghost at a target but no source or target: its ghost never shows.", this);
        }

        // ---- start, resume ----

        /// <summary>A new game: lessons from the top, encounters off.</summary>
        public void Begin()
        {
            if (_runner == null) return;
            _steps = 0;
            _runner.Begin(0);
            EncountersEnabled = false;
            Debug.Log($"[Onboarding] New game: {script.lessons.Count} lessons from '{_runner.Current?.id}'; wild battles off until they're done.");
        }

        /// <summary>From the save: resume at a lesson, or nothing at all once finished.</summary>
        public void Restore(string lessonId, bool finished)
        {
            if (_runner == null) return;
            // Step 12a: the stop lesson became the walk-on lesson (it teaches the tap too).
            if (lessonId == "stop") lessonId = PracticeLesson;
            // Nov 3: the turn lesson went (the walk lesson counts real tiles now, any way).
            if (lessonId == "turn") lessonId = PracticeLesson;
            // Nov 12 evening: the far lesson ("Swipe to walk, tap to stop") went: the tap practice teaches the tap.
            if (lessonId == FarLesson) lessonId = PracticeLesson;
            // Step 32: so did the four-swipe walk lesson: swipe and tap are taught together in the practice.
            if (lessonId == WalkLesson) lessonId = PracticeLesson;
            // Step 9h moved the two-hands lesson into the opening: a save that waited for it at the end is done.
            if (finished || lessonId == "twoHands")
            {
                _runner.Finish();
                EncountersEnabled = true;
                return;
            }
            _runner.Resume(lessonId);
            EncountersEnabled = false;
            Debug.Log($"[Onboarding] Resuming the tutorial at '{_runner.Current?.id}' (saved '{lessonId}').");
        }

        /// <summary>Debug skip: jumps to a lesson without praise (the opening's N key).</summary>
        public void SkipTo(string id)
        {
            if (_runner == null || !_runner.IsRunning) return;
            if (id == WalkLesson || id == FarLesson) id = PracticeLesson; // step 32: both went into the swipe-and-tap lesson
            int i = script.IndexOf(id);
            if (i < 0)
            {
                Debug.LogError($"[Onboarding] No lesson '{id}' to skip to.", this);
                return;
            }
            var l = _runner.Current;
            if (l != null) End(l, false);
            _runner.Begin(i);
            Debug.Log($"[Onboarding] Skipped to '{id}'.");
        }

        /// <summary>Debug: the whole tutorial counts as done.</summary>
        public void DebugFinish()
        {
            if (_runner == null || !_runner.IsRunning) return;
            var l = _runner.Current;
            if (l != null) End(l, false);
            _runner.Finish();
            EncountersEnabled = true;
            Debug.Log("[Onboarding] Debug: tutorial finished.");
        }

        /// <summary>Records a lesson event (doing it early completes that lesson when it comes). No-op without an Onboarding.</summary>
        public static void Mark(string name)
        {
            var o = _instance;
            if (o == null || o._runner == null) return;
            if (!o._runner.Mark(name)) Debug.LogError($"[Onboarding] '{name}' isn't a lesson event (Onboarding.Events).", o);
        }

        /// <summary>The grass lesson is on and this tall grass tile is in its patch.</summary>
        public static bool IsTutorialGrass(Vector2Int tile)
        {
            var o = _instance;
            if (o == null || o._runner == null || !o._runner.IsRunning || o._runner.Current == null || !o._runner.Current.tutorialGrass) return false;
            var world = o.island.world;
            if (world == null || !world.TryGetLandmark(o.patchLandmark, out var mid)) return false;
            var d = tile - mid;
            return Mathf.Max(Mathf.Abs(d.x), Mathf.Abs(d.y)) <= o.patchRadius && world.At(tile) == IslandMap.TallGrass;
        }

        // ---- events from the game ----

        void OnStep(Vector2Int tile)
        {
            if (_runner == null || !_runner.IsRunning) return;
            if (++_steps >= walkSteps) _runner.Mark("walked3");
            if (Practising) PracticeStep(tile);
            // Nov 12 evening: a swipe counts in the walk lesson at its first step (one swipe walking on is one swipe).
            if (_swipePending && _shown && _runner.Current != null && _runner.Current.id == WalkLesson && !explorer.Destination.HasValue)
            {
                _swipePending = false;
                SwipeCounted();
            }
            // Step 14f: a swipe walk that reached its end (not a walk-on that carries on, not a look + pinch path).
            if (_runner.Current != null && _runner.Current.id == FarLesson && !explorer.Destination.HasValue &&
                explorer.Plan.Count > 0 && explorer.PlanIndex >= explorer.Plan.Count &&
                !(explorer.Continuous && explorer.PlanEnd == SmartStep.Cap) && ++_swipeWalks >= 2)
                _runner.Mark("swipeWalks2");
        }

        void OnStopped()
        {
            if (_runner == null || !_runner.IsRunning) return;
            _runner.Mark("stopped");
            // Step 14f: any tap that stops a walk (it waited for a walk-on, and players couldn't tell one from a step).
            if (_runner.Current != null && _runner.Current.id == FarLesson) _runner.Mark("tapStopped");
            if (Practising) PracticeStop();
        }

        void OnSwiped(bool walkOn)
        {
            if (_runner == null || !_runner.IsRunning) return;
            if (_runner.Current != null && _runner.Current.id == WalkLesson) _swipePending = true;
            if (!walkOn) return;
            _runner.Mark("walkedOn");
            if (_runner.Current != null && _runner.Current.id == FarLesson && ++_walkOns >= 3) _runner.Mark("walkedOn3");
        }

        /// <summary>Step 12a: a smart step stops on the lesson's gold tile.</summary>
        WalkStop GoldStop(Vector2Int from, Vector2Int next) =>
            Running && _goldTile.HasValue && next == _goldTile.Value && CurrentId != PracticeLesson ? WalkStop.On : WalkStop.None;

        void OnBattleEnded(BattleResult result)
        {
            if (_runner == null || !_runner.IsRunning) return;
            _runner.Mark(result == BattleResult.Won || result == BattleResult.Caught ? "wonOrCaught" : "battleFailed");
        }

        // ---- the chain ----

        void Update()
        {
            if (_runner == null || !_runner.IsRunning) return;
            TrackHands();
            var kb = Keyboard.current;
            if ((Application.isEditor || Debug.isDebugBuild) && kb != null && kb.jKey.wasPressedThisFrame)
            {
                if (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed)
                {
                    DebugFinish();
                    return;
                }
                DebugComplete();
            }

            var lesson = _runner.Current;
            // Step 16: one-handed players: the two-hand lesson passes by itself after a few seconds.
            if (CurrentId == "twoHands" && Current != null)
            {
                _twoHandsFor += Time.deltaTime;
                if (_twoHandsFor >= twoHandsPassSeconds && _twoHandsFor - Time.deltaTime < twoHandsPassSeconds)
                {
                    Debug.Log($"[Onboarding] twoHands: no two-hand grab in {twoHandsPassSeconds:0} s: it passes (one hand is fine).");
                    _runner.Mark("twoHandsTimedOut");
                }
            }
            else _twoHandsFor = 0f;
            if (Practising && _shown && explorer != null)
            {
                bool walking = explorer.Heading != Vector2Int.zero;
                if (walking != _practiceWalking)
                {
                    _practiceWalking = walking;
                    Dialogue.SetChip(walking ? practiceTapChip : practiceSwipeChip);
                }
            }
            if (Practising && !DebugHoldPass)
            {
                _practiceFor += Time.deltaTime;
                if (_practiceFor >= practicePassSeconds && _practiceFor - Time.deltaTime < practicePassSeconds)
                {
                    Debug.Log($"[Onboarding] practice: {_practiced} stop(s) in {practicePassSeconds:0} s: it passes.");
                    _runner.Mark("practiceDone");
                }
            }
            else _practiceFor = 0f;
            var outcome = _runner.Tick(Time.deltaTime);
            switch (outcome)
            {
                case LessonRunner.Outcome.Started:
                    Show(lesson);
                    break;
                case LessonRunner.Outcome.Done:
                    End(lesson, true);
                    break;
                case LessonRunner.Outcome.Skipped:
                case LessonRunner.Outcome.Rewound:
                    End(lesson, false);
                    break;
            }
            if (outcome != LessonRunner.Outcome.None)
                Debug.Log($"[Onboarding] {lesson.id}: {outcome}{(outcome == LessonRunner.Outcome.Started ? "" : $" ({_runner.LastReason})")}" +
                          $"{(_runner.IsFinished ? "" : $"; next '{_runner.Current?.id}'")}.");
            if (outcome == LessonRunner.Outcome.Done || outcome == LessonRunner.Outcome.Skipped)
            {
                if (_runner.IsFinished) EncountersEnabled = true;
                if (save != null) save.Save(_runner.IsFinished ? "tutorial finished" : $"lesson {lesson.id} over");
            }
            if (_runner.IsFinished)
            {
                Debug.Log("[Onboarding] Tutorial finished: wild battles and fishing are on.");
                return;
            }
            var current = _runner.Current;
            if (_runner.State == LessonRunner.Phase.Waiting && current != null && !string.IsNullOrEmpty(current.goldPath) && _goldPath == null)
                _goldPath = StartCoroutine(GoldPath(current));
            if (_runner.State == LessonRunner.Phase.Teaching)
            {
                Teach(current);
                if (current.id == pinchLesson) CountPinches();
            }
        }

        /// <summary>The pinch lesson: each new pinch (either hand) sparkles gold at the fingertips; enough of them = done.</summary>
        void CountPinches()
        {
            for (int h = 0; h < 2; h++)
            {
                var hand = HandRegistry.Get(h == 0 ? Handedness.Left : Handedness.Right);
                bool pinching = hand != null && hand.IsTrackedDataValid && hand.GetIndexFingerIsPinching();
                if (pinching && !_wasPinching[h]) Pinched(hand, h == 0);
                _wasPinching[h] = pinching;
            }
        }

        void Pinched(IHand hand, bool left)
        {
            _pinches++;
            Debug.Log($"[Onboarding] Pinch {_pinches}/{pinchCount} ({(left ? "left" : "right")}).");
            // Step 14a: the first pinch is a moment: a bigger gold burst, a soft chime, a dot fills, "Nice! Once more."
            bool first = _pinches == 1 && pinchCount > 1;
            if (fx != null && hand != null && HandLookup.TryGetJoint(hand, HandJointId.HandThumbTip, out var thumb) &&
                HandLookup.TryGetJoint(hand, HandJointId.HandIndexTip, out var index))
                fx.Burst((thumb + index) * 0.5f, first ? 26 : 10, sparkleColor, first ? 0.09f : 0.06f, first ? 0.6f : 0.45f, first ? 0.005f : 0.004f, Vector3.up * 0.03f);
            Dialogue.SetProgress(_pinches, pinchCount);
            if (first)
            {
                GameAudio.Ui(Diorama.Audio.AudioIds.UiLesson);
                Dialogue.SetChip(pinchAgainChip);
                Dialogue.Repeat(pinchOnceLine);
            }
            if (_pinches >= pinchCount) _runner.Mark("pinched");
        }

        bool _swipePending;
        int _swipes;
        string _cueLine;

        /// <summary>
        /// Nov 12 evening (Mugesh: the four dots filled from one long walk): one swipe of the walk lesson. A dot fills, a
        /// small burst at the explorer, the chime, and the line moves on: "Nice! Now swipe another way.", "Good! Keep
        /// swiping, any way.", "One more swipe!"; the last one ends the lesson.
        /// </summary>
        void SwipeCounted()
        {
            _swipes++;
            Debug.Log($"[Onboarding] Swipe {_swipes}/{swipeTiles}.");
            if (fx != null && explorer != null && island != null && island.Content != null)
                fx.Burst(island.transform.TransformPoint(island.TileToLocal(explorer.Tile, island.Content.capHeight)), 10, sparkleColor, 0.04f, 0.5f, 0.004f, Vector3.up * 0.03f);
            Dialogue.SetProgress(Mathf.Min(_swipes, swipeTiles), swipeTiles);
            if (_swipes >= swipeTiles)
            {
                _runner.Mark("swipeWalked");
                return;
            }
            GameAudio.Ui(Diorama.Audio.AudioIds.UiLesson);
            Dialogue.SetChip(swipeAgainChip);
            _cueLine = SwipeCue(_swipes, swipeTiles, swipeNiceLine, swipeMoreLine, swipeLastLine);
            if (!string.IsNullOrEmpty(_cueLine)) Dialogue.Repeat(_cueLine);
        }

        /// <summary>Nov 12 evening: the walk lesson's line after <paramref name="done"/> of <paramref name="goal"/> swipes.</summary>
        public static string SwipeCue(int done, int goal, string nice, string more, string last) =>
            done >= goal ? null : done == goal - 1 ? last : done == 1 ? nice : more;

        /// <summary>Debug (bridge, smoke run): a swipe of the walk lesson as if the explorer stepped from it.</summary>
        public void DebugSwipe()
        {
            if (_runner != null && _runner.Current != null && _runner.Current.id == WalkLesson && _shown) SwipeCounted();
        }

        /// <summary>Debug (bridge): a pinch as if made with a hand (no sparkle without a tracked hand).</summary>
        public void DebugPinch()
        {
            if (_runner != null && _runner.Current != null && _runner.Current.id == pinchLesson) Pinched(null, false);
        }

        void Show(LessonData l)
        {
            _shown = true;
            _pinches = 0;
            _wasPinching[0] = _wasPinching[1] = true; // a pinch already held doesn't count
            _helped = false;
            _lineAway = 0f;
            _smartAtShow = explorer != null ? explorer.SmartStepsTaken : 0;
            _swipeTiles = _walkOns = _swipeWalks = 0;
            _swipes = 0;
            _swipePending = false;
            _cueLine = null;
            if (l.id == PracticeLesson) StartPractice();
            else if (!string.IsNullOrEmpty(l.goldTileRule)) PickSmartGoldTile(l.goldTileRule);
            else if (l.goldTileSteps > 0) PickGoldTile(l.goldTileSteps);
            if (!string.IsNullOrEmpty(l.goldPath) && _goldPath == null) _goldPath = StartCoroutine(GoldPath(l));
            // Step 9h: one instruction at a time, and it stays until the lesson is done.
            if (!string.IsNullOrEmpty(l.line))
            {
                bool pinch = l.id == pinchLesson;
                bool practice = l.id == PracticeLesson;
                bool walk = l.id == WalkLesson;
                Dialogue.Pin(new[] { l.line, l.again, l.help, script.handsLine, pinch ? pinchOnceLine : null,
                        walk ? swipeNiceLine : null, walk ? swipeMoreLine : null, walk ? swipeLastLine : null }, l.chip,
                    practice ? new[] { practiceNiceLine, practicePerfectLine, practiceGoldLine } : null);
                // Step 14a: a counted lesson shows its progress dots (two pinches).
                if (pinch) Dialogue.SetProgress(0, pinchCount);
                else if (l.id == WalkLesson) Dialogue.SetProgress(0, swipeTiles);
                else if (practice) Dialogue.SetProgress(0, StopPractice.Goal(QuickJourney.Active));
                Dialogue.Repeat(l.line);
            }
            LessonShown?.Invoke(l);
        }

        void Teach(LessonData l)
        {
            if (string.IsNullOrEmpty(l.line)) return;
            bool talking = Dialogue.IsPending(l.line) || !string.IsNullOrEmpty(l.again) && Dialogue.IsPending(l.again) ||
                           !string.IsNullOrEmpty(l.help) && Dialogue.IsPending(l.help) || HandsMissing ||
                           // Step 14a: the pinch lesson's "Nice! Once more." is its line now.
                           l.id == pinchLesson && !string.IsNullOrEmpty(pinchOnceLine) && Dialogue.IsPending(pinchOnceLine) ||
                           // Nov 12 evening: the walk lesson's "Nice! Now swipe another way." is its line after a swipe.
                           !string.IsNullOrEmpty(_cueLine) && Dialogue.IsPending(_cueLine);
            if (!string.IsNullOrEmpty(l.help) && !_helped && _runner.Teaching >= l.helpAfter)
            {
                // The help line takes the lesson line's place (and stays).
                _helped = true;
                _lineAway = 0f;
                Dialogue.Repeat(l.help);
                return;
            }
            // The line stays up; if it closed anyway (its condition paused it, the hands check), it comes back.
            _lineAway = talking ? 0f : _lineAway + Time.deltaTime;
            if (_lineAway < 1f) return;
            _lineAway = 0f;
            var back = _helped && !string.IsNullOrEmpty(l.help) ? l.help : !string.IsNullOrEmpty(_cueLine) ? _cueLine : string.IsNullOrEmpty(l.again) ? l.line : l.again;
            Dialogue.Repeat(back);
        }

        void End(LessonData l, bool done)
        {
            bool praise = done && l.praise && script.praise.Count > 0;
            // The lesson's line and chip close; the praise comes first, then the lines that waited for the lesson.
            // Step 14a: the pinch lesson ends on its own "Perfect!".
            Dialogue.Unpin(done && l.id == pinchLesson && !string.IsNullOrEmpty(pinchDoneLine) ? pinchDoneLine
                : done && l.id == PracticeLesson && _practiced >= StopPractice.Goal(QuickJourney.Active) ? practiceDoneLine
                : praise ? script.praise[_praiseIndex++ % script.praise.Count] : null);
            foreach (var id in new[] { l.line, l.again, l.help, _cueLine })
                if (!string.IsNullOrEmpty(id)) Dialogue.Cancel(id);
            _cueLine = null;
            if (_goldPath != null)
            {
                StopCoroutine(_goldPath);
                _goldPath = null;
            }
            _trail.Clear();
            if (l.goldTileSteps > 0 || !string.IsNullOrEmpty(l.goldTileRule) || l.id == PracticeLesson) _goldTile = null;
            if (done && l.praise) GameAudio.Ui(Diorama.Audio.AudioIds.UiLesson);
            if (HandsMissing) SetHandsMissing(false);
            if (_shown) LessonEnded?.Invoke(l, done);
            _shown = false;
        }

        /// <summary>Debug (J, the Claude bridge): the current lesson counts as done.</summary>
        public void DebugComplete()
        {
            var l = _runner?.Current;
            if (l == null || string.IsNullOrEmpty(l.doneWhen)) return;
            foreach (var part in l.doneWhen.Split('|'))
            {
                var name = part.Split('&')[0].Trim();
                if (_runner.Mark(name))
                {
                    Debug.Log($"[Onboarding] Debug: marked {name} for {l.id}.");
                    return;
                }
            }
            Debug.Log($"[Onboarding] Debug: {l.id} waits for a state ({l.doneWhen}), not an event.");
        }

        // ---- hands ----

        void TrackHands()
        {
            bool any = false;
            for (int h = 0; h < 2; h++)
            {
                bool left = h == 0;
                var hand = HandRegistry.Get(left ? Handedness.Left : Handedness.Right);
                bool tracked = hand != null && hand.IsTrackedDataValid;
                any |= tracked;
            }
            // Flat Editor Play has no hands at all: no "Show me your hands!" there.
            if (!UnityEngine.XR.XRSettings.isDeviceActive || _runner.State != LessonRunner.Phase.Teaching) any = true;
            _untracked = any ? 0f : _untracked + Time.deltaTime;
            bool missing = _untracked >= handsMissingSeconds;
            if (missing != HandsMissing) SetHandsMissing(missing);
        }

        void SetHandsMissing(bool missing)
        {
            HandsMissing = missing;
            Debug.Log($"[Onboarding] Hands {(missing ? "not tracked: asking for them" : "back")}.");
            if (missing && !string.IsNullOrEmpty(script.handsLine)) Dialogue.Repeat(script.handsLine);
            if (!missing && !string.IsNullOrEmpty(script.handsLine)) Dialogue.Cancel(script.handsLine);
            HandsMissingChanged?.Invoke(missing);
        }

        // ---- gold tiles ----

        /// <summary>
        /// Nov 10 (playtest: the way to the grass was very faint): the same gold trail as the Spirit's (<see cref="SpiritTrail"/>),
        /// from the explorer to the lesson's landmark, kept up to date as it walks. It used to flash the tiles one by one.
        /// </summary>
        float _goldSoundAt = -99f, _trailAt;
        readonly List<Vector2Int> _trail = new List<Vector2Int>();
        const int TrailTiles = 14;
        const float TrailTileSize = 0.78f;

        IEnumerator GoldPath(LessonData l)
        {
            _trailAt = Time.time;
            while (true)
            {
                var world = island.world;
                if (world == null || !world.TryGetLandmark(l.goldPath, out var to))
                {
                    _trail.Clear();
                    yield break;
                }
                if (explorer.CanMove && explorer.Tile != to && ExplorerPath.FindAlongRoads(world, explorer.Tile, to, _trail, 400))
                {
                    if (_trail.Count > 0 && Time.time - _goldSoundAt > 8f)
                    {
                        _goldSoundAt = Time.time;
                        GameAudio.At(Diorama.Audio.AudioIds.PathGold, island.transform.TransformPoint(island.TileToLocal(_trail[0], island.Content.capHeight)));
                    }
                }
                else _trail.Clear();
                yield return new WaitForSeconds(0.5f);
            }
        }

        void DrawTrail()
        {
            int n = Mathf.Min(TrailTiles, _trail.Count);
            float age = Time.time - _trailAt;
            for (int i = 0; i < n; i++)
                _goldMarks.Draw(_trail[i], GoldMarks.Tile, 0f, i == _trail.Count - 1 ? TrailTileSize * 1.15f : TrailTileSize, Mathf.Clamp01((age - i * 0.05f) / 0.2f), n > 1 ? i / (float)(n - 1) : 1f, true);
        }

        /// <summary>
        /// Step 12a: the gold tile from a smart-step plan from the explorer (tries all four ways, prefers path tiles and
        /// away from the player): ahead = the 4th tile (else the last, at least 3); turn = one past the first turn (else a
        /// junction stop); far = the end of the longest walk-on stretch (at least 6). Only tiles in view.
        /// </summary>
        void PickSmartGoldTile(string rule)
        {
            _goldTile = null;
            var world = island.world;
            if (world == null || explorer == null) return;
            var plan = new List<Vector2Int>();
            var ways = new[] { new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(0, 1) };
            float best = float.MinValue;
            for (int w = 0; w < ways.Length; w++)
            {
                bool far = rule == "far";
                string end = SmartStep.Plan(world, explorer.Tile, ways[w], far, far ? 12 : rule == "turn" ? 9 : SmartStep.SmartTiles, null, plan);
                // Only tiles in view count.
                int n = 0;
                while (n < plan.Count && island.FootprintContains(island.TileToLocal(plan[n], island.Content.capHeight))) n++;
                if (n == 0) continue;
                Vector2Int? pick = null;
                float score = 0f;
                if (rule == "turn")
                {
                    for (int i = 1; i < n && !pick.HasValue; i++)
                    {
                        var before = i >= 2 ? plan[i - 1] - plan[i - 2] : plan[0] - explorer.Tile;
                        if (plan[i] - plan[i - 1] != before) pick = plan[Mathf.Min(i + 1, n - 1)];
                    }
                    if (pick.HasValue) score = 20f;
                    else if (end == SmartStep.Junction && n == plan.Count && n >= 2)
                    {
                        pick = plan[n - 1];
                        score = 10f;
                    }
                }
                else if (rule == "far")
                {
                    // The end of the straight part (at least 5 tiles), else the end of any walk of 6 or more.
                    int straight = 1;
                    while (straight < n && plan[straight] - plan[straight - 1] == plan[0] - explorer.Tile) straight++;
                    if (straight >= 5)
                    {
                        pick = plan[straight - 1];
                        score = 10f + straight;
                    }
                    else if (n >= 6)
                    {
                        pick = plan[n - 1];
                        score = n;
                    }
                }
                else
                {
                    if (n >= 3) pick = plan[Mathf.Min(3, n - 1)];
                    score = n;
                }
                if (!pick.HasValue || world.At(pick.Value) == IslandMap.TallGrass) continue;
                if (world.At(pick.Value) == IslandMap.Path) score += 3f;
                if (w == 0) score += 1.5f;
                // Not back the way it just came.
                if (ways[w] == -explorer.Facing) score -= 6f;
                if (score > best)
                {
                    best = score;
                    _goldTile = pick;
                }
            }
            if (!_goldTile.HasValue && rule != "ahead") PickSmartGoldTile("ahead");
            Debug.Log(_goldTile.HasValue
                ? $"[Onboarding] Gold tile ({rule}) c{_goldTile.Value.x} r{_goldTile.Value.y} from c{explorer.Tile.x} r{explorer.Tile.y}."
                : $"[Onboarding] No {rule} gold tile in view: the lesson finishes after a few smart steps.");
        }

        // ---- step 27: swipe to walk, tap to stop, on the gold ----

        void StartPractice()
        {
            _practiced = 0;
            _practiceFor = 0f;
            _practiceWalking = false;
            PickPracticeTile();
        }

        /// <summary>The way the walk goes next (zero while standing).</summary>
        Vector2Int WalkWay() =>
            explorer.Heading == Vector2Int.zero ? Vector2Int.zero
            : explorer.PlanIndex < explorer.Plan.Count ? explorer.Plan[explorer.PlanIndex] - explorer.Tile : explorer.Heading;

        /// <summary>
        /// Step 28: the next gold tile, straight on along the walk with a real run-up, in view, never in tall grass. Standing:
        /// the longest clear line from here. Nothing long enough ahead (a wall coming up) = no tile until the walk has turned.
        /// </summary>
        void PickPracticeTile()
        {
            _goldTile = null;
            var world = island.world;
            if (world == null || explorer == null) return;
            int goal = StopPractice.Goal(QuickJourney.Active);
            if (!StopPractice.GoldRound(_practiced, goal))
            {
                Debug.Log($"[Onboarding] Practice round {_practiced + 1}/{goal}: swipe, then any tap that stops the walk.");
                return;
            }
            int want = StopPractice.Distance(_practiced);
            var way = WalkWay();
            var ways = way != Vector2Int.zero ? new[] { way }
                : new[] { explorer.Facing, new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(0, 1) };
            float best = float.MinValue;
            for (int w = 0; w < ways.Length; w++)
            {
                int n = 0;
                for (var t = explorer.Tile + ways[w]; n < want && world.CanWalk(t) && island.FootprintContains(island.TileToLocal(t, island.Content.capHeight)) &&
                                                    !LessonAreaBlocks(explorer.Tile, t); t += ways[w]) n++;
                int along = n;
                while (along >= StopPractice.Nearest && world.At(explorer.Tile + ways[w] * along) == IslandMap.TallGrass) along--;
                if (along < StopPractice.Nearest) continue;
                var pick = explorer.Tile + ways[w] * along;
                float score = along + (world.At(pick) == IslandMap.Path ? 1f : 0f) + (w == 0 ? 3f : 0f);
                if (score <= best) continue;
                best = score;
                _goldTile = pick;
            }
            _practiceBest = _goldTile.HasValue ? StopPractice.Distance(explorer.Tile, _goldTile.Value) : 0;
            Debug.Log(_goldTile.HasValue
                ? $"[Onboarding] Practice: gold tile c{_goldTile.Value.x} r{_goldTile.Value.y}, {want} along from c{explorer.Tile.x} r{explorer.Tile.y}."
                : "[Onboarding] Practice: no gold tile in view: the time limit passes the lesson.");
        }

        /// <summary>Walked past the gold tile: a new one ahead, no fail state.</summary>
        void PracticeStep(Vector2Int tile)
        {
            if (!_goldTile.HasValue)
            {
                if (StopPractice.GoldRound(_practiced, StopPractice.Goal(QuickJourney.Active))) PickPracticeTile();
                return;
            }
            int d = StopPractice.Distance(tile, _goldTile.Value);
            _practiceBest = Mathf.Min(_practiceBest, d);
            // Step 28: the walk turned away from it, or went past: it moves ahead again. Next to it still counts as a stop.
            bool away = d >= 2 && !StopPractice.StraightAhead(tile, WalkWay(), _goldTile.Value);
            if (!away && !StopPractice.WalkedPast(d, _practiceBest)) return;
            Debug.Log("[Onboarding] Practice: not ahead any more: a new gold tile ahead.");
            PickPracticeTile();
        }

        [Tooltip("Nov 11 (Mugesh: stopping only on the gold tile was too hard): a stop on the gold tile's line (\"Perfect!\"); any other tap stop gets the nice line.")]
        public string practicePerfectLine = "lesson.practice.perfect";

        /// <summary>
        /// A thumb tap stopped the walk. Nov 11 (Mugesh: only the gold tile counted and it was hard, the look + pinch lesson
        /// takes any success): every tap that stops the walk counts; on or next to the gold tile it's "Perfect!" with the
        /// burst, elsewhere "Nice stop!" with a small burst at the explorer. The gold tile stays as the thing to aim for.
        /// </summary>
        void PracticeStop()
        {
            bool gold = _goldTile.HasValue && StopPractice.Landed(explorer.Tile, _goldTile.Value);
            _practiced++;
            int goal = StopPractice.Goal(QuickJourney.Active);
            Debug.Log($"[Onboarding] Practice: stop {_practiced}/{goal} on c{explorer.Tile.x} r{explorer.Tile.y}{(gold ? " (the gold tile)" : " (off the gold tile: counts too)")}.");
            if (fx != null)
            {
                var at = island.transform.TransformPoint(island.TileToLocal(gold ? _goldTile.Value : explorer.Tile, island.Content.capHeight));
                fx.Burst(at, gold ? 18 : 8, sparkleColor, gold ? 0.06f : 0.035f, 0.5f, 0.004f, Vector3.up * 0.03f);
            }
            GameAudio.Ui(Diorama.Audio.AudioIds.UiLesson);
            Dialogue.SetProgress(_practiced, goal);
            if (_practiced >= goal)
            {
                _goldTile = null;
                _runner.Mark("practiceDone");
                return;
            }
            bool goldNext = StopPractice.GoldRound(_practiced, goal);
            Dialogue.Repeat(goldNext && !string.IsNullOrEmpty(practiceGoldLine) ? practiceGoldLine
                : gold && !string.IsNullOrEmpty(practicePerfectLine) ? practicePerfectLine : practiceNiceLine);
            PickPracticeTile();
        }

        /// <summary>Debug (bridge): a good stop.</summary>
        public void DebugPracticeStop()
        {
            if (!Practising) return;
            if (_goldTile.HasValue) explorer.Teleport(_goldTile.Value, explorer.Facing);
            PracticeStop();
        }

        // ---- the gold tile's look (step 12a) ----

        GoldMarks _goldMarks;
        int _smartAtShow, _swipeTiles, _walkOns, _swipeWalks;

        void LateUpdate()
        {
            if (_goldMarks == null) return;
            _goldMarks.Begin();
            if (_goldTile.HasValue && Running && explorer != null && explorer.Tile != _goldTile.Value)
                _goldMarks.Draw(_goldTile.Value, GoldMarks.Tile, 0f, 0.92f, 1f, 1f, true);
            DrawTrail();
            _goldMarks.End();
        }

        /// <summary>A walkable tile about <paramref name="steps"/> steps away, in view, not tall grass, toward the island's middle.</summary>
        void PickGoldTile(int steps)
        {
            _goldTile = null;
            var world = island.world;
            if (world == null) return;
            var center = island.ViewCenterTile;
            var path = new List<Vector2Int>();
            float best = float.MaxValue;
            for (int dy = -steps; dy <= steps; dy++)
                for (int dx = -steps; dx <= steps; dx++)
                {
                    var t = explorer.Tile + new Vector2Int(dx, dy);
                    int manhattan = Mathf.Abs(dx) + Mathf.Abs(dy);
                    if (manhattan < steps - 1 || manhattan > steps + 1 || !world.CanWalk(t) || world.At(t) == IslandMap.TallGrass) continue;
                    if (!island.FootprintContains(island.TileToLocal(t, island.Content.capHeight))) continue;
                    if (!ExplorerPath.Find(world, explorer.Tile, t, path, steps + 3)) continue;
                    // Prefer tiles toward the middle of the view and a little toward the player (+rows).
                    float score = (t - center).sqrMagnitude - 0.5f * dy + Mathf.Abs(path.Count - steps) * 2f;
                    if (score < best)
                    {
                        best = score;
                        _goldTile = t;
                    }
                }
            Debug.Log(_goldTile.HasValue
                ? $"[Onboarding] Gold tile c{_goldTile.Value.x} r{_goldTile.Value.y} ({steps} steps from c{explorer.Tile.x} r{explorer.Tile.y})."
                : "[Onboarding] No gold tile in view: the lesson waits for any look + pinch walk.");
        }

        Vector3? GoldTilePoint()
        {
            if (!_goldTile.HasValue || island == null || island.Content == null) return null;
            var local = island.TileToLocal(_goldTile.Value, island.Content.capHeight);
            return island.FootprintContains(local) ? island.transform.TransformPoint(local) : (Vector3?)null;
        }

        float _lastFlash;

        void GoldTilePulse(float k)
        {
            if (!_goldTile.HasValue || k < 0.9f || Time.time - _lastFlash < 0.6f) return;
            _lastFlash = Time.time;
            island.FlashTile(_goldTile.Value, 0.45f);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _instance = null;
            EncountersEnabled = true;
        }
    }
}
