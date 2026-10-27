using System;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Pump (step 9b hand puzzle kit): a pad you poke down with a fingertip; it springs back up. A stroke = pushed past
    /// <see cref="downAt"/> of its travel and back above <see cref="upAt"/> (<see cref="PumpStrokes"/>): one poke, one
    /// pump. Either index fingertip counts when it comes down onto the pad from above within <see cref="pressRadius"/>.
    /// <see cref="strokesNeeded"/> strokes solve it; every <see cref="checkpoint"/> strokes is a saved stage that never
    /// cools away; between them, stopping for <see cref="coolDelay"/> s loses a stroke every <see cref="coolStep"/> s.
    /// Look + pinch the pad = one stroke by itself.
    /// Mugesh, Oct 26: a poke reads better than the first version's lever. The piece draws nothing itself: what it
    /// pumps (the bellows squashing under the finger, pipes, braziers) is the listener's (<see cref="ForgeBellows"/>),
    /// from <see cref="Travel"/>, <see cref="Stroked"/> and <see cref="Cooled"/>.
    /// </summary>
    [DefaultExecutionOrder(-20)] // before the gaze walk and tile touch read IsHandBusy
    public class PumpPiece : HandPuzzle
    {
        [Header("Press pad")]
        [Tooltip("Map tile (column, row) of the pad's centre.")]
        public Vector2 pad;
        [Tooltip("The pad's top at rest, tiles above the cap.")]
        public float padHeight = 0.55f;
        [Tooltip("How far it goes down, tiles (all the way = travel 1).")]
        public float pressTravel = 0.3f;
        [Tooltip("A fingertip within this many tiles of the pad's centre (horizontally) presses it.")]
        public float pressRadius = 0.5f;
        [Tooltip("Extra world metres on the radius (hand tracking wobble).")]
        public float pressSlack = 0.015f;
        [Tooltip("Spring return, travel per second (1 = the whole way in a second).")]
        public float spring = 5f;
        [Tooltip("Look + pinch: one stroke takes this long.")]
        public float autoStrokeSeconds = 0.6f;

        [Header("Strokes")]
        public int strokesNeeded = 12;
        [Tooltip("Every this many strokes is a saved stage (a lit brazier).")]
        public int checkpoint = 4;
        public float coolDelay = 4f, coolStep = 2f;
        [Range(0.5f, 1f)] public float downAt = 0.7f;
        [Range(0f, 0.5f)] public float upAt = 0.3f;

        /// <summary>A stroke counted (strokes so far).</summary>
        public event Action<PumpPiece, int> Stroked;
        /// <summary>Heat cooled away one stroke (strokes left).</summary>
        public event Action<PumpPiece, int> Cooled;
        /// <summary>Step 10f: a stroke while <see cref="Blocked"/> (it pumps, nothing comes of it).</summary>
        public event Action<PumpPiece> BlockedStroke;

        /// <summary>
        /// Step 10f: the pad still moves and strokes still register, but they don't count (Cinderwood's bellows while the
        /// forge's firebox is cold, <see cref="ForgeEmberPiece"/>). Its lesson waits until it's free. Runtime only.
        /// </summary>
        [NonSerialized] public bool Blocked;

        protected override bool Available => !Blocked;

        /// <summary>0 = pad up at rest, 1 = pushed all the way down.</summary>
        public float Travel { get; private set; }
        public int Strokes => _strokes.Strokes;
        public int Checkpoints => _strokes.Checkpoints;
        public override bool Busy => _hand >= 0 || _auto >= 0f;

        readonly PumpStrokes _strokes = new PumpStrokes();
        readonly HandState[] _hands = new HandState[2];
        readonly bool[] _above = new bool[2];
        int _hand = -1;
        float _auto = -1f;
        bool _gazed;

        protected override Vector2 HomeTile => pad;

        public override Vector3? FocusPoint => Here ? PadWorld : (Vector3?)null;

        public override float FocusRadius => 0.6f * TileWorld;

        /// <summary>The pad's top now (world).</summary>
        public Vector3 PadWorld => MapToWorld(new Vector3(pad.x, padHeight - pressTravel * Travel, pad.y));

        protected override void Start()
        {
            _strokes.needed = Mathf.Max(1, strokesNeeded);
            _strokes.checkpoint = Mathf.Max(0, checkpoint);
            _strokes.coolDelay = coolDelay;
            _strokes.coolStep = coolStep;
            _strokes.downAt = downAt;
            _strokes.upAt = upAt;
            base.Start();
        }

        protected override void ApplyState()
        {
            _hand = -1;
            _auto = -1f;
            Travel = 0f;
            _strokes.Restore(IsSolved ? _strokes.needed : PuzzleState.Stage(id) * _strokes.checkpoint);
            SetProgressQuiet(_strokes.Strokes / (float)_strokes.needed);
        }

        protected override void Update()
        {
            ReadHands(_hands);
            bool usable = Usable && !IsSolved;
            float pressed = usable ? Press() : 0f;
            if (!Here) _hand = -1;
            if (_auto >= 0f) AutoStroke();
            else
            {
                // The pad follows a finger down at once and springs back up when it lifts.
                Travel = Mathf.Max(pressed, Mathf.MoveTowards(Travel, 0f, spring * Time.deltaTime));
                if (usable && _hand < 0) LookPinch();
                else _gazed = false;
            }
            if (_hand >= 0) IslandHandle.MarkHandHeld(_hand == 0);
            if (Blocked)
            {
                // Pumped while blocked: the pad goes down and up, the stroke is only reported.
                if (_strokes.Feed(Travel))
                {
                    _strokes.Restore(_strokes.Strokes - 1);
                    Debug.Log($"[PumpPiece] {id}: a stroke while blocked (nothing happens).");
                    BlockedStroke?.Invoke(this);
                }
            }
            else if (_strokes.Feed(Travel)) OnStroke();
            if (!IsSolved && !Blocked && _strokes.Tick(Time.deltaTime))
            {
                SetProgress(_strokes.Strokes / (float)_strokes.needed);
                Debug.Log($"[PumpPiece] {id}: cooled to {_strokes.Strokes} stroke(s).");
                Cooled?.Invoke(this, _strokes.Strokes);
            }
            base.Update();
        }

        /// <summary>How far a fingertip pushes the pad down (0..1); sets <see cref="_hand"/> to the pressing hand or -1.</summary>
        float Press()
        {
            float best = 0f;
            int was = _hand;
            _hand = -1;
            float slack = pressSlack / Mathf.Max(1e-5f, TileWorld);
            for (int h = 0; h < 2; h++)
            {
                var hand = HandRegistry.Get(h == 0 ? Oculus.Interaction.Input.Handedness.Left : Oculus.Interaction.Input.Handedness.Right);
                if (_hands[h].busy && was != h || !HandLookup.TryGetJoint(hand, Oculus.Interaction.Input.HandJointId.HandIndexTip, out var tip))
                {
                    _above[h] = false;
                    continue;
                }
                Vector3 m = WorldToMap(tip);
                bool over = new Vector2(m.x - pad.x, m.z - pad.y).magnitude <= pressRadius + slack;
                // Only a finger that came down onto the pad from above presses it (not one coming in from the side or
                // below), and one pushed far through it lets go.
                if (!over || m.y < padHeight - pressTravel - 0.8f)
                {
                    _above[h] = false;
                    continue;
                }
                if (m.y >= padHeight)
                {
                    _above[h] = true;
                    continue;
                }
                if (!_above[h]) continue;
                float depth = Mathf.Clamp01((padHeight - m.y) / Mathf.Max(1e-3f, pressTravel));
                if (depth > best || _hand < 0)
                {
                    best = Mathf.Max(best, depth);
                    _hand = h;
                }
            }
            if (_hand >= 0 && was < 0)
            {
                MarkInteracted();
                Debug.Log($"[PumpPiece] {id}: pressed by the {(_hand == 0 ? "left" : "right")} fingertip.");
            }
            return best;
        }

        void LookPinch()
        {
            _gazed = Gazed();
            if (!_gazed) return;
            FocusHighlight.Hover(PadWorld, FocusRadius);
            if (GazePinch(_hands))
            {
                MarkInteracted();
                _auto = 0f;
                Debug.Log($"[PumpPiece] {id}: look + pinch: one stroke.");
            }
        }

        void AutoStroke()
        {
            _auto += Time.deltaTime;
            float u = Mathf.Clamp01(_auto / Mathf.Max(0.1f, autoStrokeSeconds));
            // Down quickly, up with the spring.
            Travel = u < 0.4f ? Mathf.SmoothStep(0f, 1f, u / 0.4f) : 1f - Mathf.SmoothStep(0f, 1f, (u - 0.4f) / 0.6f);
            if (u >= 1f)
            {
                _auto = -1f;
                Travel = 0f;
            }
        }

        bool Gazed()
        {
            if (!HeadGaze.TryGetRay(out var ray)) return false;
            var pick = new HeadGaze.ConePick(ray, _gazed ? 0 : -1);
            pick.Consider(0, PadWorld, Mathf.Max(HeadGaze.MinRadius * island.transform.lossyScale.x, 0.7f * TileWorld));
            return pick.Best == 0;
        }

        void OnStroke()
        {
            int s = _strokes.Strokes;
            SetProgress(s / (float)_strokes.needed);
            Debug.Log($"[PumpPiece] {id}: stroke {s}/{_strokes.needed}.");
            Stroked?.Invoke(this, s);
            if (_strokes.Done) MarkSolved(_strokes.Checkpoints);
            else if (_strokes.checkpoint > 0 && s % _strokes.checkpoint == 0) PuzzleState.SetStage(id, _strokes.Checkpoints);
        }

        /// <summary>Debug (bridge): one stroke as if pumped.</summary>
        public void DebugStroke()
        {
            if (!Here || IsSolved) return;
            MarkInteracted();
            _auto = 0f;
        }
    }
}
