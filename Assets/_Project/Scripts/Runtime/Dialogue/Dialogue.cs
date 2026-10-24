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
    /// Editor: D cycles through every trigger id (ignoring once); the Inspector lists them with Fire buttons.
    /// </summary>
    public class Dialogue : MonoBehaviour
    {
        /// <summary>Something a line can point at: its world point (null while hidden) and a 0..1 highlight.</summary>
        public class Target
        {
            public Func<Vector3?> point;
            public Action<float> pulse;
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
        }

        static readonly Dictionary<string, Target> _targets = new Dictionary<string, Target>();
        static readonly Dictionary<string, Func<string, Target>> _resolvers = new Dictionary<string, Func<string, Target>>();
        Target _target;
        bool _targetLost;
        readonly HashSet<string> _warnedTargets = new HashSet<string>();

        readonly List<Queued> _queue = new List<Queued>();
        readonly HashSet<string> _said = new HashSet<string>();
        readonly Dictionary<DialogueSpeaker, Transform> _anchors = new Dictionary<DialogueSpeaker, Transform>();
        Queued? _current;
        float _lastInput, _lastIdle = -999f;
        int _lastHint = -1, _debugIndex = -1;

        /// <summary>Raised when a line starts (speaker) and when the bubble closes (null): the Spirit perks up while talking.</summary>
        public static event Action<DialogueSpeaker?> Speaking;

        /// <summary>Trigger ids already said (once triggers). Step 7 saves and restores these.</summary>
        public static IEnumerable<string> Said => _instance != null ? _instance._said : Array.Empty<string>();

        /// <summary>Queues every line of a trigger. Does nothing without a Dialogue in the scene.</summary>
        public static void Say(string triggerId)
        {
            if (_instance != null) _instance.Fire(triggerId, false);
        }

        /// <summary>Where a speaker's bubble goes (above this point). Unregistered speakers use the Spirit's.</summary>
        public static void RegisterSpeaker(DialogueSpeaker speaker, Transform anchor)
        {
            if (_instance != null) _instance._anchors[speaker] = anchor;
            else _pendingAnchors[speaker] = anchor;
        }

        // Speakers that register before the Dialogue wakes up.
        static readonly Dictionary<DialogueSpeaker, Transform> _pendingAnchors = new Dictionary<DialogueSpeaker, Transform>();

        public static bool IsTalking => _instance != null && _instance._current.HasValue;

        /// <summary>Registers a target anchor by id (leadPod, cradle, ...). Re-registering replaces it.</summary>
        public static void RegisterTarget(string id, Func<Vector3?> point, Action<float> pulse) =>
            _targets[id] = new Target { point = point, pulse = pulse };

        /// <summary>Resolves ids with an argument, "prefix:arg" (tile:12,7).</summary>
        public static void RegisterTargetResolver(string prefix, Func<string, Target> resolve) => _resolvers[prefix] = resolve;

        /// <summary>The current line's target: Active with its world point, Lost once it disappeared, or None.</summary>
        public static TargetState GetTarget(out Vector3 world)
        {
            world = default;
            var d = _instance;
            if (d == null || d._target == null) return TargetState.None;
            if (d._targetLost) return TargetState.Lost;
            var p = d._target.point != null ? d._target.point() : null;
            if (!p.HasValue) return TargetState.Lost;
            world = p.Value;
            return TargetState.Active;
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
        }

        void Start()
        {
            _lastInput = Time.time;
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
                    };
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
        public void Fire(string id, bool force)
        {
            if (script == null) return;
            var t = script.Find(id);
            if (t == null)
            {
                Debug.LogWarning($"[Dialogue] No trigger '{id}' in {script.name}.", this);
                return;
            }
            if (t.once && _said.Contains(id) && !force) return;
            if (IsQueuedOrShowing(id) && !force) return;
            _said.Add(id);
            if (_current.HasValue && t.priority > _current.Value.priority)
            {
                // Cut off the lower line (and anything else lower that waits) for this one.
                _queue.RemoveAll(q => q.priority < t.priority);
                _current = null;
            }
            int at = _queue.FindIndex(q => q.priority < t.priority);
            if (at < 0) at = _queue.Count;
            foreach (var line in t.lines) _queue.Insert(at++, new Queued { line = line, priority = t.priority, trigger = id, target = t.target });
            Debug.Log($"[Dialogue] {id}: {t.lines.Count} line(s){(t.once ? ", once" : "")}.");
            if (!_current.HasValue) Next();
        }

        bool IsQueuedOrShowing(string id) => _current.HasValue && _current.Value.trigger == id || _queue.Exists(q => q.trigger == id);

        void Next()
        {
            _current = null;
            SetTarget(null);
            if (_queue.Count == 0)
            {
                if (bubble != null) bubble.Hide();
                Speaking?.Invoke(null);
                return;
            }
            var q = _queue[0];
            _queue.RemoveAt(0);
            _current = q;
            _lastInput = Time.time;
            SetTarget(q.target);
            var anchor = AnchorFor(q.line.speaker);
            if (bubble != null) bubble.Show(q.line, anchor);
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

        void Update()
        {
            var kb = Keyboard.current;
            if (Application.isEditor && kb != null && kb.dKey.wasPressedThisFrame) DebugCycle();
            if (kb != null && kb.anyKey.wasPressedThisFrame) _lastInput = Time.time;
            UpdateTarget();
            TryIdleHint();
        }

        void TryIdleHint()
        {
            if (script == null || script.idleHints.Count == 0 || _current.HasValue || _queue.Count > 0) return;
            if (Time.time - _lastInput < idleSeconds || Time.time - _lastIdle < idleGap) return;
            if (encounters != null && encounters.InBattle || explorer != null && explorer.Locked) return;
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

        /// <summary>Drops everything queued and closes the bubble.</summary>
        public void ClearAll()
        {
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
