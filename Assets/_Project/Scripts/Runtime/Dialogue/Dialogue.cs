using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Diorama
{
    /// <summary>
    /// Plays dialogue lines by trigger id, one bubble at a time. Any system calls <see cref="Say"/> with an id from the
    /// script; all its lines queue up. "once" triggers are remembered (in memory for now; step 7 saves them).
    /// A higher priority cuts off a lower one that is showing (idle hints are 0). Idle hints come after
    /// <see cref="idleSeconds"/> without input, at most one per <see cref="idleGap"/>, never during a battle.
    /// Fires the hooks it can see itself: tut.grass (first tall grass step) and tut.pond (first time next to water).
    /// A trigger can name a target anchor (systems register them by id, like speakers): while its lines show, the target
    /// pulses and <see cref="TryGetTarget"/> gives its point, so the Spirit flies over to it. A target that disappears
    /// mid-line counts as lost (the Spirit goes home, the line finishes there).
    /// Lines expire (step 8b): a trigger's <see cref="DialogueTrigger.validWhile"/> (a <see cref="GameConditions"/>
    /// expression) must hold while it shows, or the bubble closes at once and its queued lines are dropped. When a battle
    /// or a scene swap starts, every hint line (priority 1 or lower) that doesn't say it still holds is dropped. A "once"
    /// trigger dropped before any of its lines showed may fire again later. No idle hints while the tutorial runs.
    /// Editor: D cycles through every trigger id (ignoring once); the Inspector lists them with Fire buttons.
    /// Step 9h: one instruction at a time, and it stays. While a lesson teaches, its triggers are pinned (<see cref="Pin"/>):
    /// the lesson's last line never times out (the bubble shrinks it to a hint chip), only pinned triggers may show, and
    /// every other line waits in the queue until <see cref="Unpin"/>. A more important line no longer cuts the one showing
    /// at once: that one ends when it has been up for its minimum reading time.
    /// </summary>
    public class Dialogue : MonoBehaviour
    {
        /// <summary>
        /// Something a line can point at: its world point (null while hidden), a 0..1 pulse of its own (runes, pods), and
        /// the radius (world metres) of the gold focus sphere round the point (<see cref="FocusHighlight"/>).
        /// </summary>
        public class Target
        {
            public Func<Vector3?> point;
            public Action<float> pulse;
            public Func<float> radius;
        }

        public enum TargetState { None, Active, Lost }

        public DialogueScript script;
        public DialogueBubble bubble;
        public GestureTestPiece explorer;
        public MicrogestureInput input;
        public EncounterDirector encounters;

        [Header("Idle hints")]
        public float idleSeconds = 40f;
        public float idleGap = 120f;

        static Dialogue _instance;

        struct Queued
        {
            public DialogueLine line;
            public int priority;
            public string trigger;
            public string target;
            public string validWhile;
        }

        static readonly Dictionary<string, Target> _targets = new Dictionary<string, Target>();
        static readonly Dictionary<string, Func<string, Target>> _resolvers = new Dictionary<string, Func<string, Target>>();
        Target _target;
        bool _targetLost;
        readonly HashSet<string> _warnedTargets = new HashSet<string>();

        readonly List<Queued> _queue = new List<Queued>();
        readonly HashSet<string> _said = new HashSet<string>();
        readonly HashSet<string> _shown = new HashSet<string>();
        readonly HashSet<string> _notNow = new HashSet<string>();
        bool _wasSwapping;
        readonly Dictionary<DialogueSpeaker, Transform> _anchors = new Dictionary<DialogueSpeaker, Transform>();
        Queued? _current;
        readonly HashSet<string> _pinned = new HashSet<string>();
        string _chip;
        float _lastInput, _lastIdle = -999f;
        int _lastHint = -1, _debugIndex = -1;

        /// <summary>Raised when a line starts (speaker) and when the bubble closes (null): the Spirit perks up while talking.</summary>
        public static event Action<DialogueSpeaker?> Speaking;

        /// <summary>Trigger ids already said (once triggers). Step 7 saves and restores these.</summary>
        public static IEnumerable<string> Said => _instance != null ? _instance._said : Array.Empty<string>();

        /// <summary>A "once" trigger was already said (this session or a loaded save).</summary>
        public static bool HasSaid(string triggerId) => _instance != null && _instance._said.Contains(triggerId);

        /// <summary>Lines already said in an earlier session (loading a save): "once" lines stay quiet.</summary>
        public static void RestoreSaid(IEnumerable<string> ids)
        {
            if (_instance == null || ids == null) return;
            foreach (var id in ids) _instance._said.Add(id);
        }

        /// <summary>Queues every line of a trigger. Does nothing without a Dialogue in the scene.</summary>
        public static void Say(string triggerId)
        {
            if (_instance != null) _instance.Fire(triggerId, false);
        }

        /// <summary>Queues a trigger's lines even if it is "once" and was said (lesson reminders). Not while it is still queued or showing.</summary>
        public static void Repeat(string triggerId)
        {
            if (_instance != null) _instance.Fire(triggerId, false, true);
        }

        /// <summary>A trigger's lines are showing or waiting in the queue.</summary>
        public static bool IsPending(string triggerId) => _instance != null && _instance.IsQueuedOrShowing(triggerId);

        /// <summary>Where a speaker's bubble goes (above this point). Unregistered speakers use the Spirit's.</summary>
        public static void RegisterSpeaker(DialogueSpeaker speaker, Transform anchor)
        {
            if (_instance != null) _instance._anchors[speaker] = anchor;
            else _pendingAnchors[speaker] = anchor;
        }

        // Speakers that register before the Dialogue wakes up.
        static readonly Dictionary<DialogueSpeaker, Transform> _pendingAnchors = new Dictionary<DialogueSpeaker, Transform>();

        public static bool IsTalking => _instance != null && _instance._current.HasValue;

        /// <summary>A lesson is teaching: only its own lines show (<see cref="Pin"/>).</summary>
        public static bool IsPinned => _instance != null && _instance._pinned.Count > 0;

        /// <summary>
        /// Step 9h: a lesson starts teaching. Only these triggers may show until <see cref="Unpin"/>; the last line of the
        /// one showing stays (no time-out), shrinking to <paramref name="chip"/> after a while. Other lines queue.
        /// </summary>
        public static void Pin(IEnumerable<string> triggers, string chip)
        {
            if (_instance != null) _instance.PinLines(triggers, chip);
        }

        /// <summary><see cref="Pin"/> on this Dialogue (tests).</summary>
        public void PinLines(IEnumerable<string> triggers, string chip)
        {
            var d = this;
            d._pinned.Clear();
            foreach (var t in triggers)
                if (!string.IsNullOrEmpty(t)) d._pinned.Add(t);
            d._chip = chip;
            // A line that is not the lesson's ends after its minimum time; then only lesson lines show.
            if (d._current.HasValue && !d._pinned.Contains(d._current.Value.trigger) && d.bubble != null) d.bubble.FinishSoon();
            Debug.Log($"[Dialogue] Pinned for the lesson: {string.Join(", ", d._pinned)} (chip '{chip}').");
        }

        /// <summary>
        /// The lesson is over: its lines close (the chip pops), <paramref name="first"/> (the praise) shows next, then the
        /// lines that waited.
        /// </summary>
        public static void Unpin(string first = null)
        {
            if (_instance != null) _instance.UnpinLines(first);
        }

        /// <summary><see cref="Unpin"/> on this Dialogue (tests).</summary>
        public void UnpinLines(string first = null)
        {
            var d = this;
            if (d._pinned.Count == 0) return;
            var pinned = new List<string>(d._pinned);
            d._pinned.Clear();
            d._chip = null;
            d._queue.RemoveAll(q => pinned.Contains(q.trigger));
            bool showingLesson = d._current.HasValue && pinned.Contains(d._current.Value.trigger);
            if (!string.IsNullOrEmpty(first)) d.FireFirst(first);
            if (showingLesson || !d._current.HasValue) d.Next();
        }

        /// <summary>Registers a target anchor by id (leadPod, cradle, ...). Re-registering replaces it.</summary>
        public static void RegisterTarget(string id, Func<Vector3?> point, Action<float> pulse, Func<float> radius = null) =>
            _targets[id] = new Target { point = point, pulse = pulse, radius = radius };

        /// <summary>Resolves ids with an argument, "prefix:arg" (tile:12,7).</summary>
        public static void RegisterTargetResolver(string prefix, Func<string, Target> resolve) => _resolvers[prefix] = resolve;

        /// <summary>The current line's target: Active with its world point, Lost once it disappeared, or None.</summary>
        public static TargetState GetTarget(out Vector3 world) => GetTarget(out world, out _);

        /// <summary>The current line's target and its <see cref="Target"/> (for the focus sphere).</summary>
        public static TargetState GetTarget(out Vector3 world, out Target target)
        {
            world = default;
            target = null;
            var d = _instance;
            if (d == null || d._target == null) return TargetState.None;
            target = d._target;
            if (d._targetLost) return TargetState.Lost;
            var p = d._target.point != null ? d._target.point() : null;
            if (!p.HasValue) return TargetState.Lost;
            world = p.Value;
            return TargetState.Active;
        }

        /// <summary>A registered target by id (also "prefix:arg"), for hints that point without a line (LodgeGuide).</summary>
        public static bool TryGetTarget(string id, out Target target)
        {
            target = Resolve(id);
            return target != null;
        }

        static Target Resolve(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_targets.TryGetValue(id, out var t)) return t;
            int colon = id.IndexOf(':');
            if (colon > 0 && _resolvers.TryGetValue(id.Substring(0, colon), out var r)) return r(id.Substring(colon + 1));
            return null;
        }

        void Awake()
        {
            if (_instance != null && _instance != this) Debug.LogError("[Dialogue] Two Dialogue components in the scene.", this);
            _instance = this;
            foreach (var kv in _pendingAnchors) _anchors[kv.Key] = kv.Value;
            _pendingAnchors.Clear();
            if (script == null) Debug.LogError("[Dialogue] No script assigned. Tools > Pocket Isle > Dialogue > Import Script_v1.", this);
            if (bubble == null) Debug.LogError("[Dialogue] No bubble assigned.", this);
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
            if (encounters != null) encounters.BattleStarted -= OnBattleStarted;
        }

        void Start()
        {
            _lastInput = Time.time;
            if (encounters != null)
            {
                GameConditions.Register("battle", () => encounters != null && encounters.InBattle);
                encounters.BattleStarted += OnBattleStarted;
            }
            GameConditions.Register("swap", () => SceneSwap.IsBusy);
            if (bubble != null)
            {
                bubble.Finished += Next;
                bubble.Skipped += Next;
            }
            if (explorer != null)
            {
                explorer.OnTallGrassStep += _ => Say("tut.grass");
                explorer.OnStep += OnStep;
            }
            if (input != null) input.Gesture += (g, s) => _lastInput = Time.time;
            if (explorer != null && explorer.island != null)
            {
                var island = explorer.island;
                RegisterTargetResolver("tile", arg =>
                {
                    var parts = arg.Split(',');
                    if (parts.Length != 2 || !int.TryParse(parts[0], out int x) || !int.TryParse(parts[1], out int y)) return null;
                    var tile = new Vector2Int(x, y);
                    float last = -1f;
                    return new Target
                    {
                        point = () =>
                        {
                            if (island == null || island.Content == null) return null;
                            var local = island.TileToLocal(tile, island.Content.capHeight);
                            return island.FootprintContains(local) ? island.transform.TransformPoint(local) : (Vector3?)null;
                        },
                        // The tile cap flashes on each pulse peak.
                        pulse = k =>
                        {
                            if (island == null || k < 0.95f || Time.time - last < 0.5f) return;
                            last = Time.time;
                            island.FlashTile(tile, 0.35f);
                        },
                        radius = () => island == null || island.map == null ? 0f : 0.5f * island.map.tileSize * island.Zoom * island.transform.lossyScale.x,
                    };
                });
                // A named tile on the current map (WorldMap.landmarks), a tile up: big props like the statue.
                RegisterTargetResolver("landmark", arg => new Target
                {
                    point = () =>
                    {
                        if (island == null || island.world == null || !island.world.TryGetLandmark(arg, out var tile)) return null;
                        var local = island.TileToLocal(tile, island.Content.capHeight + island.map.tileSize);
                        return island.FootprintContains(local) ? island.transform.TransformPoint(local) : (Vector3?)null;
                    },
                    pulse = k => { },
                    radius = () => island == null || island.map == null ? 0f : 1.4f * island.map.tileSize * island.Zoom * island.transform.lossyScale.x,
                });
            }
        }

        void OnStep(Vector2Int tile)
        {
            var map = explorer.island.Content;
            foreach (var d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                var n = tile + d;
                if (map.At(n.y, n.x) != IslandMap.Water) continue;
                Say("tut.pond");
                return;
            }
        }

        /// <summary>Queues a trigger's lines; force = ignore "once" (debug).</summary>
        public void Fire(string id, bool force) => Fire(id, force, force);

        void Fire(string id, bool force, bool ignoreOnce)
        {
            if (script == null) return;
            var t = script.Find(id);
            if (t == null)
            {
                Debug.LogWarning($"[Dialogue] No trigger '{id}' in {script.name}.", this);
                return;
            }
            if (t.once && _said.Contains(id) && !ignoreOnce) return;
            if (!GameConditions.Check(t.validWhile))
            {
                if (_notNow.Add(id)) Debug.Log($"[Dialogue] {id}: not now ({t.validWhile}).");
                return;
            }
            _notNow.Remove(id);
            if (IsQueuedOrShowing(id) && !force) return;
            _said.Add(id);
            bool pinned = _pinned.Contains(id);
            if (_pinned.Count > 0)
            {
                if (pinned)
                {
                    // The lesson's own line (help, hands, a reminder) takes the lesson's place at once.
                    _queue.RemoveAll(q => _pinned.Contains(q.trigger));
                    int first = 0;
                    foreach (var line in t.lines) _queue.Insert(first++, Make(line, t, id));
                    Debug.Log($"[Dialogue] {id}: {t.lines.Count} lesson line(s).");
                    if (!_current.HasValue || _pinned.Contains(_current.Value.trigger)) Next();
                    return;
                }
                Debug.Log($"[Dialogue] {id}: waits until the lesson is done.");
            }
            else if (_current.HasValue && t.priority > _current.Value.priority)
            {
                // The lower lines that wait go; the one showing ends once it has been readable (reading pace).
                _queue.RemoveAll(q => q.priority < t.priority);
                if (_current.Value.priority <= 0) _current = null;
                else if (bubble != null) bubble.FinishSoon();
            }
            int at = _queue.FindIndex(q => q.priority < t.priority);
            if (at < 0) at = _queue.Count;
            foreach (var line in t.lines) _queue.Insert(at++, Make(line, t, id));
            Debug.Log($"[Dialogue] {id}: {t.lines.Count} line(s){(t.once ? ", once" : "")}.");
            if (!_current.HasValue) Next();
        }

        static Queued Make(DialogueLine line, DialogueTrigger t, string id) =>
            new Queued { line = line, priority = t.priority, trigger = id, target = t.target, validWhile = t.validWhile };

        /// <summary>Queues a trigger's lines in front of everything (the praise after a lesson).</summary>
        void FireFirst(string id)
        {
            var t = script != null ? script.Find(id) : null;
            if (t == null)
            {
                Debug.LogWarning($"[Dialogue] No trigger '{id}'.", this);
                return;
            }
            _said.Add(id);
            int at = 0;
            foreach (var line in t.lines) _queue.Insert(at++, Make(line, t, id));
        }

        bool IsQueuedOrShowing(string id) => _current.HasValue && _current.Value.trigger == id || _queue.Exists(q => q.trigger == id);

        void Next()
        {
            _current = null;
            SetTarget(null);
            // Lines that stopped applying while they waited are dropped.
            while (_queue.Count > 0 && !GameConditions.Check(_queue[0].validWhile)) Drop(0, "no longer applies");
            // While a lesson teaches only its own lines show; the others keep waiting.
            int next = _pinned.Count == 0 ? (_queue.Count > 0 ? 0 : -1) : _queue.FindIndex(q => _pinned.Contains(q.trigger));
            if (next < 0)
            {
                if (bubble != null) bubble.Hide();
                Speaking?.Invoke(null);
                return;
            }
            var q = _queue[next];
            _queue.RemoveAt(next);
            _current = q;
            _shown.Add(q.trigger);
            _lastInput = Time.time;
            SetTarget(q.target);
            var anchor = AnchorFor(q.line.speaker);
            // The lesson's last line stays (pinned) until the lesson is done.
            bool stays = _pinned.Contains(q.trigger) && !_queue.Exists(o => o.trigger == q.trigger);
            if (bubble != null) bubble.Show(q.line, anchor, stays ? (string.IsNullOrEmpty(_chip) ? q.line.text : _chip) : null);
            Speaking?.Invoke(q.line.speaker);
        }

        void SetTarget(string id)
        {
            if (_target != null && _target.pulse != null) _target.pulse(0f);
            _target = Resolve(id);
            _targetLost = false;
            if (!string.IsNullOrEmpty(id) && _target == null && _warnedTargets.Add(id))
                Debug.LogWarning($"[Dialogue] Target '{id}' isn't registered (yet); the line plays at home.", this);
            else if (_target != null) Debug.Log($"[Dialogue] Pointing at {id}.");
        }

        /// <summary>The target pulses while its line shows; once it disappears it stays lost for the rest of the line.</summary>
        void UpdateTarget()
        {
            if (_target == null || _targetLost) return;
            var p = _target.point != null ? _target.point() : null;
            if (!p.HasValue)
            {
                _targetLost = true;
                if (_target.pulse != null) _target.pulse(0f);
                Debug.Log("[Dialogue] Target gone mid-line: the Spirit goes home.");
                return;
            }
            if (_target.pulse != null) _target.pulse(0.5f + 0.5f * Mathf.Sin(Time.time * 6f));
        }

        Transform AnchorFor(DialogueSpeaker speaker)
        {
            if (_anchors.TryGetValue(speaker, out var a) && a != null) return a;
            if (_anchors.TryGetValue(DialogueSpeaker.Spirit, out a) && a != null) return a;
            return transform;
        }

        /// <summary>Removes a queued line; a "once" trigger that never showed a line may fire again later.</summary>
        void Drop(int index, string why)
        {
            var q = _queue[index];
            _queue.RemoveAt(index);
            if (_queue.Exists(o => o.trigger == q.trigger)) return;
            if (!_shown.Contains(q.trigger) && _said.Remove(q.trigger)) Debug.Log($"[Dialogue] {q.trigger} dropped before it showed ({why}): it can come again.");
            else Debug.Log($"[Dialogue] {q.trigger} dropped ({why}).");
        }

        void OnBattleStarted() => Expire("battle started");

        /// <summary>The trigger whose line shows now, or null.</summary>
        public string CurrentTrigger => _current.HasValue ? _current.Value.trigger : null;

        /// <summary>
        /// A battle or scene swap started (called by those events; public for tests): hint lines (priority 1 or lower)
        /// close and drop unless they still hold.
        /// </summary>
        public void Expire(string why)
        {
            for (int i = _queue.Count - 1; i >= 0; i--)
                if (_queue[i].priority <= 1 && !Holds(_queue[i])) Drop(i, why);
            if (_current.HasValue && _current.Value.priority <= 1 && !Holds(_current.Value))
            {
                Debug.Log($"[Dialogue] {_current.Value.trigger} closed ({why}).");
                DropTrigger(_current.Value.trigger, why);
                Next();
            }
        }

        // An explicit validWhile that holds keeps a line through a battle or swap; lines without one are plain hints.
        static bool Holds(Queued q) => !string.IsNullOrEmpty(q.validWhile) && GameConditions.Check(q.validWhile);

        void DropTrigger(string trigger, string why)
        {
            for (int i = _queue.Count - 1; i >= 0; i--)
                if (_queue[i].trigger == trigger) Drop(i, why);
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (Application.isEditor && kb != null && kb.dKey.wasPressedThisFrame) DebugCycle();
            bool swapping = SceneSwap.IsBusy;
            if (swapping && !_wasSwapping) Expire("scene swap");
            _wasSwapping = swapping;
            if (_current.HasValue && !GameConditions.Check(_current.Value.validWhile))
            {
                Debug.Log($"[Dialogue] {_current.Value.trigger} closed: {_current.Value.validWhile} stopped holding.");
                DropTrigger(_current.Value.trigger, "condition ended");
                Next();
            }
            if (kb != null && kb.anyKey.wasPressedThisFrame) _lastInput = Time.time;
            UpdateTarget();
            TryIdleHint();
        }

        void TryIdleHint()
        {
            if (script == null || script.idleHints.Count == 0 || _current.HasValue || _queue.Count > 0) return;
            // Step 9g: Settings > Spirit hints > Fewer.
            if (GameSettings.FewerHints) return;
            if (Time.time - _lastInput < idleSeconds || Time.time - _lastIdle < idleGap) return;
            if (encounters != null && encounters.InBattle || explorer != null && explorer.Locked || Onboarding.Running) return;
            _lastIdle = Time.time;
            int i = UnityEngine.Random.Range(0, script.idleHints.Count);
            if (i == _lastHint && script.idleHints.Count > 1) i = (i + 1) % script.idleHints.Count;
            _lastHint = i;
            _queue.Add(new Queued { line = script.idleHints[i], priority = 0, trigger = "idle" });
            Debug.Log("[Dialogue] idle hint.");
            Next();
        }

        void DebugCycle()
        {
            if (script == null || script.triggers.Count == 0) return;
            _debugIndex = (_debugIndex + 1) % script.triggers.Count;
            var id = script.triggers[_debugIndex].id;
            Debug.Log($"[Dialogue] Debug D: {id} ({_debugIndex + 1}/{script.triggers.Count}).");
            ClearAll();
            Fire(id, true);
        }

        /// <summary>Stops a trigger's lines (showing or queued): its hint was acted on already.</summary>
        public static void Cancel(string triggerId)
        {
            var d = _instance;
            if (d == null) return;
            d._queue.RemoveAll(q => q.trigger == triggerId);
            if (d._current.HasValue && d._current.Value.trigger == triggerId) d.Next();
        }

        /// <summary>Drops everything queued and closes the bubble.</summary>
        public void ClearAll()
        {
            _pinned.Clear();
            _chip = null;
            _queue.Clear();
            _current = null;
            SetTarget(null);
            if (bubble != null) bubble.Hide();
        }

        /// <summary>Fires an idle hint now (debug).</summary>
        public void DebugIdle()
        {
            _lastInput = -999f;
            _lastIdle = -999f;
            TryIdleHint();
        }
    }
}
