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
    /// Editor: D cycles through every trigger id (ignoring once); the Inspector lists them with Fire buttons.
    /// </summary>
    public class Dialogue : MonoBehaviour
    {
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
        }

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
            foreach (var line in t.lines) _queue.Insert(at++, new Queued { line = line, priority = t.priority, trigger = id });
            Debug.Log($"[Dialogue] {id}: {t.lines.Count} line(s){(t.once ? ", once" : "")}.");
            if (!_current.HasValue) Next();
        }

        bool IsQueuedOrShowing(string id) => _current.HasValue && _current.Value.trigger == id || _queue.Exists(q => q.trigger == id);

        void Next()
        {
            _current = null;
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
            var anchor = AnchorFor(q.line.speaker);
            if (bubble != null) bubble.Show(q.line, anchor);
            Speaking?.Invoke(q.line.speaker);
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
