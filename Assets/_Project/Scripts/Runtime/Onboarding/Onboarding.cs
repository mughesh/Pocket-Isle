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
        [Min(1)] public int pinchCount = 2;
        [Tooltip("Pooled particles for the gold sparkle at the fingertips on each pinch.")]
        public BattleFx fx;
        public Color sparkleColor = new Color(1f, 0.82f, 0.35f);

        [Header("Chime")]
        [Range(0f, 1f)] public float chimeVolume = 0.35f;

        /// <summary>Lesson event names (anything else in a condition is a <see cref="GameConditions"/> state).</summary>
        public static readonly string[] Events =
        {
            "shardTouched", "islandGrabbed", "starterPicked", "walked3", "stopped", "gazeWalk",
            "grassBattle", "sentOut", "moveUsed", "podThrown", "wonOrCaught", "battleFailed", "twoHandGrab", "pinched",
        };

        static Onboarding _instance;

        /// <summary>False during the tutorial: tall grass only rustles and fishing is off (the tutorial patch excepted).</summary>
        public static bool EncountersEnabled { get; private set; } = true;

        /// <summary>A lesson chain is running.</summary>
        public static bool Running => _instance != null && _instance._runner != null && _instance._runner.IsRunning;

        /// <summary>A lesson starts teaching: show its ghost.</summary>
        public event Action<LessonData> LessonShown;
        /// <summary>The lesson being taught ended (done = true with praise; false = skipped, rewound or interrupted).</summary>
        public event Action<LessonData, bool> LessonEnded;
        /// <summary>No hand tracked for a while during a lesson (true), or they came back (false).</summary>
        public event Action<bool> HandsMissingChanged;

        public LessonRunner Runner => _runner;
        public LessonData Current => _runner != null && _runner.State == LessonRunner.Phase.Teaching ? _runner.Current : null;
        public bool IsFinished => _runner != null && _runner.IsFinished;
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
        AudioSource _audio;

        void Awake()
        {
            if (_instance != null && _instance != this) Debug.LogError("[Onboarding] Two Onboarding components in the scene.", this);
            _instance = this;
            if (script == null)
            {
                Debug.LogError("[Onboarding] No OnboardingScript (Tools > Pocket Isle > Set Up MR_Main (8b onboarding)): no lessons.", this);
                return;
            }
            _runner = new LessonRunner(script.lessons, Events) { Skip = QuickJourney.SkipsLesson };
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
                explorer.Stopped -= OnStopped;
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
            explorer.Stopped += OnStopped;
            encounters.BattleEnded += OnBattleEnded;

            GameConditions.Register("battle.send", () => encounters != null && encounters.AwaitingSendOut);
            GameConditions.Register("battle.out", () => encounters != null && encounters.PlayerOut);
            GameConditions.Register("wildRed", () => encounters != null && encounters.InBattle && encounters.CurrentBattle != null && encounters.CurrentBattle.CatchUnlocked);
            GameConditions.Register("canWalk", () => explorer != null && explorer.CanMove && !Opening.InProgress && !SceneSwap.IsBusy && !SceneSwap.IsInside);
            GameConditions.Register("atGoldTile", () => _goldTile.HasValue && explorer != null && explorer.Tile == _goldTile.Value && !explorer.Destination.HasValue);
            GameConditions.Register("tutorial", () => Running);
            GameConditions.Register("encounters", () => EncountersEnabled);

            Dialogue.RegisterTarget("goldTile", GoldTilePoint, GoldTilePulse,
                () => island == null || island.map == null ? 0f : 0.5f * island.map.tileSize * island.Zoom * island.transform.lossyScale.x);

            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
            _audio.clip = MakeChime();
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
            if (finished)
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
        }

        void OnStopped()
        {
            if (_runner != null && _runner.IsRunning) _runner.Mark("stopped");
        }

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
            if (fx != null && hand != null && HandLookup.TryGetJoint(hand, HandJointId.HandThumbTip, out var thumb) &&
                HandLookup.TryGetJoint(hand, HandJointId.HandIndexTip, out var index))
                fx.Burst((thumb + index) * 0.5f, 10, sparkleColor, 0.06f, 0.45f, 0.004f, Vector3.up * 0.03f);
            if (_pinches >= pinchCount) _runner.Mark("pinched");
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
            if (l.goldTileSteps > 0) PickGoldTile(l.goldTileSteps);
            if (!string.IsNullOrEmpty(l.goldPath) && _goldPath == null) _goldPath = StartCoroutine(GoldPath(l));
            // Step 9h: one instruction at a time, and it stays until the lesson is done.
            if (!string.IsNullOrEmpty(l.line))
            {
                Dialogue.Pin(new[] { l.line, l.again, l.help, script.handsLine }, l.chip);
                Dialogue.Repeat(l.line);
            }
            LessonShown?.Invoke(l);
        }

        void Teach(LessonData l)
        {
            if (string.IsNullOrEmpty(l.line)) return;
            bool talking = Dialogue.IsPending(l.line) || !string.IsNullOrEmpty(l.again) && Dialogue.IsPending(l.again) ||
                           !string.IsNullOrEmpty(l.help) && Dialogue.IsPending(l.help) || HandsMissing;
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
            var back = _helped && !string.IsNullOrEmpty(l.help) ? l.help : string.IsNullOrEmpty(l.again) ? l.line : l.again;
            Dialogue.Repeat(back);
        }

        void End(LessonData l, bool done)
        {
            bool praise = done && l.praise && script.praise.Count > 0;
            // The lesson's line and chip close; the praise comes first, then the lines that waited for the lesson.
            Dialogue.Unpin(praise ? script.praise[_praiseIndex++ % script.praise.Count] : null);
            foreach (var id in new[] { l.line, l.again, l.help })
                if (!string.IsNullOrEmpty(id)) Dialogue.Cancel(id);
            if (_goldPath != null)
            {
                StopCoroutine(_goldPath);
                _goldPath = null;
            }
            if (l.goldTileSteps > 0) _goldTile = null;
            if (done && l.praise && _audio != null && _audio.clip != null) _audio.PlayOneShot(_audio.clip, chimeVolume);
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

        /// <summary>Gold tiles run from the explorer to the lesson's landmark until it gets there.</summary>
        IEnumerator GoldPath(LessonData l)
        {
            var path = new List<Vector2Int>();
            while (true)
            {
                var world = island.world;
                if (world == null || !world.TryGetLandmark(l.goldPath, out var to)) yield break;
                if (explorer.CanMove && explorer.Tile != to && ExplorerPath.Find(world, explorer.Tile, to, path, 60))
                {
                    var from = explorer.Tile;
                    foreach (var tile in path)
                    {
                        if (explorer.Tile != from) break;
                        island.FlashTile(tile, 0.4f);
                        yield return new WaitForSeconds(0.12f);
                    }
                }
                yield return new WaitForSeconds(1.2f);
            }
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

        // ---- chime ----

        /// <summary>A soft two-note bell (placeholder until the audio step).</summary>
        static AudioClip MakeChime()
        {
            const int rate = 44100;
            const float seconds = 0.9f;
            int n = (int)(rate * seconds);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float a = Note(t, 0f, 1046.5f) + Note(t, 0.11f, 1568f);
                data[i] = a * 0.4f;
            }
            var clip = AudioClip.Create("Lesson Chime", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Note(float t, float start, float hz)
        {
            t -= start;
            if (t < 0f) return 0f;
            float env = Mathf.Min(1f, t / 0.005f) * Mathf.Exp(-t * 5f);
            return env * (Mathf.Sin(2f * Mathf.PI * hz * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * hz * 2.01f * t));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _instance = null;
            EncountersEnabled = true;
        }
    }
}
