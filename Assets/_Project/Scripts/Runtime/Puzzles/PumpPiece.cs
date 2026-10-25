using System;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Pump (step 9b hand puzzle kit): a lever handle on a hinge with a spring return. Pinch (or close a fist on) the
    /// grip and push it down; let go and it springs back up. A stroke = pushed past ~80% of its travel and back above
    /// 30% (<see cref="PumpStrokes"/>). <see cref="strokesNeeded"/> strokes solve it; every <see cref="checkpoint"/>
    /// strokes is a saved stage that never cools away; between them, stopping for <see cref="coolDelay"/> s loses a
    /// stroke every <see cref="coolStep"/> s. Look + pinch the grip = one whole stroke by itself.
    /// The handle (<see cref="handleRecipe"/>, modelled with its hinge at the origin and the grip along +Z) is drawn here;
    /// what it pumps (bellows, pipes, braziers) is the listener's (<see cref="ForgeBellows"/>), from <see cref="Travel"/>,
    /// <see cref="Stroked"/> and <see cref="Cooled"/>.
    /// </summary>
    [DefaultExecutionOrder(-20)] // before the gaze walk and tile touch read IsHandBusy
    public class PumpPiece : HandPuzzle
    {
        [Header("Handle")]
        public PropRecipe handleRecipe;
        [Tooltip("Hinge: map tile (column, row) and height above the cap (tiles).")]
        public Vector2 hinge;
        public float hingeHeight = 0.8f;
        [Tooltip("Hinge to grip, tiles.")]
        public float armLength = 1f;
        [Tooltip("Which way the arm points, degrees (map space; 0 = toward the player).")]
        public float armYaw;
        [Tooltip("Arm angle above horizontal at rest (up) and fully pushed (down), degrees.")]
        public float upAngle = 28f, downAngle = -12f;
        [Tooltip("Spring return, travel per second (1 = the whole way in a second).")]
        public float spring = 4f;

        [Header("Grab")]
        [Tooltip("Grab ball round the grip, tiles.")]
        public float grabRadius = 0.55f;
        [Tooltip("Extra world metres (hand tracking wobble).")]
        public float grabSlack = 0.015f;
        [Tooltip("Look + pinch: one stroke takes this long.")]
        public float autoStrokeSeconds = 0.7f;

        [Header("Strokes")]
        public int strokesNeeded = 12;
        [Tooltip("Every this many strokes is a saved stage (a lit brazier).")]
        public int checkpoint = 4;
        public float coolDelay = 4f, coolStep = 2f;
        [Range(0.5f, 1f)] public float downAt = 0.8f;
        [Range(0f, 0.5f)] public float upAt = 0.3f;

        /// <summary>A stroke counted (strokes so far).</summary>
        public event Action<PumpPiece, int> Stroked;
        /// <summary>Heat cooled away one stroke (strokes left).</summary>
        public event Action<PumpPiece, int> Cooled;

        /// <summary>0 = handle up at rest, 1 = pushed all the way down.</summary>
        public float Travel { get; private set; }
        public int Strokes => _strokes.Strokes;
        public int Checkpoints => _strokes.Checkpoints;
        public override bool Busy => _hand >= 0 || _auto >= 0f;

        readonly PumpStrokes _strokes = new PumpStrokes();
        readonly HandState[] _hands = new HandState[2];
        PropView _view;
        int _hand = -1;
        float _grabAngleOffset;
        float _auto = -1f;
        bool _gazed;

        protected override Vector2 HomeTile => hinge;

        public override Vector3? FocusPoint => _view != null && _view.Visible ? GripWorld : (Vector3?)null;

        public override float FocusRadius => 0.6f * TileWorld;

        /// <summary>The grip's world point now.</summary>
        public Vector3 GripWorld => MapToWorld(Grip(Angle));

        float Angle => Mathf.Lerp(upAngle, downAngle, Travel);

        Quaternion ArmTurn => Quaternion.Euler(0f, armYaw, 0f);

        /// <summary>The grip in (column, height, row) for an arm angle.</summary>
        Vector3 Grip(float angle)
        {
            // Map space: +Z toward the player, columns run toward -X.
            Vector3 dir = ArmTurn * (Quaternion.Euler(-angle, 0f, 0f) * Vector3.forward) * armLength;
            return new Vector3(hinge.x - dir.x, hingeHeight + dir.y, hinge.y + dir.z);
        }

        protected override void Start()
        {
            _strokes.needed = Mathf.Max(1, strokesNeeded);
            _strokes.checkpoint = Mathf.Max(0, checkpoint);
            _strokes.coolDelay = coolDelay;
            _strokes.coolStep = coolStep;
            _strokes.downAt = downAt;
            _strokes.upAt = upAt;
            if (handleRecipe != null) _view = new PropView($"Pump Handle {handleRecipe.name}", island, handleRecipe);
            else Debug.LogError($"[PumpPiece] {name}: no handle recipe: nothing to grab.", this);
            base.Start();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _view?.Destroy();
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
            if (!Here) _hand = -1;
            if (_hand >= 0) Hold();
            else if (_auto >= 0f) AutoStroke();
            else
            {
                Travel = Mathf.MoveTowards(Travel, 0f, spring * Time.deltaTime);
                if (usable) TryGrab();
                else _gazed = false;
            }
            if (_hand >= 0) IslandHandle.MarkHandHeld(_hand == 0);
            if (_strokes.Feed(Travel)) OnStroke();
            if (!IsSolved && _strokes.Tick(Time.deltaTime))
            {
                SetProgress(_strokes.Strokes / (float)_strokes.needed);
                Debug.Log($"[PumpPiece] {id}: cooled to {_strokes.Strokes} stroke(s).");
                Cooled?.Invoke(this, _strokes.Strokes);
            }
            if (_view != null) _view.Place(hinge, hingeHeight, ArmTurn * Quaternion.Euler(-Angle, 0f, 0f), Here);
            base.Update();
        }

        void TryGrab()
        {
            for (int h = 0; h < 2; h++)
            {
                var s = _hands[h];
                if (s.busy || !(s.pinchStarted || s.fistStarted)) continue;
                var grip = s.pinchStarted ? s.pinchPoint : s.palm;
                float slack = grabSlack / Mathf.Max(1e-5f, TileWorld);
                if ((WorldToMap(grip) - Grip(Angle)).magnitude > grabRadius + slack) continue;
                _hand = h;
                _grabAngleOffset = Angle - HandAngle(grip);
                MarkInteracted();
                Debug.Log($"[PumpPiece] {id}: handle grabbed by the {(s.left ? "left" : "right")} hand ({(s.pinchStarted ? "pinch" : "palm")}).");
                return;
            }
            _gazed = Gazed();
            if (!_gazed) return;
            FocusHighlight.Hover(GripWorld, FocusRadius);
            if (GazePinch(_hands))
            {
                MarkInteracted();
                _auto = 0f;
                Debug.Log($"[PumpPiece] {id}: look + pinch: one stroke.");
            }
        }

        /// <summary>The hand's angle round the hinge, in the arm's vertical plane (degrees above horizontal).</summary>
        float HandAngle(Vector3 world)
        {
            Vector3 m = WorldToMap(world);
            Vector3 v = new Vector3(-(m.x - hinge.x), m.y - hingeHeight, m.z - hinge.y); // back to map-space axes
            Vector3 along = ArmTurn * Vector3.forward;
            return Mathf.Atan2(v.y, Vector3.Dot(new Vector3(v.x, 0f, v.z), along)) * Mathf.Rad2Deg;
        }

        void Hold()
        {
            var s = _hands[_hand];
            if (!s.tracked || !s.Holding)
            {
                Debug.Log($"[PumpPiece] {id}: handle released at {Travel:0.00}.");
                _hand = -1;
                return;
            }
            float angle = HandAngle(s.GripPoint) + _grabAngleOffset;
            float span = upAngle - downAngle;
            Travel = Mathf.Clamp01(Mathf.Abs(span) > 1e-3f ? (upAngle - angle) / span : 0f);
        }

        void AutoStroke()
        {
            _auto += Time.deltaTime;
            float u = Mathf.Clamp01(_auto / Mathf.Max(0.1f, autoStrokeSeconds));
            // Down quickly, up with the spring.
            Travel = u < 0.45f ? Mathf.SmoothStep(0f, 1f, u / 0.45f) : 1f - Mathf.SmoothStep(0f, 1f, (u - 0.45f) / 0.55f);
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
            pick.Consider(0, GripWorld, Mathf.Max(HeadGaze.MinRadius * island.transform.lossyScale.x, 0.7f * TileWorld));
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
