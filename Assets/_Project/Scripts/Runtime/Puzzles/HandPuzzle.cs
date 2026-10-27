using System;
using System.Collections.Generic;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Base of the guardian-hand puzzle pieces (step 9b: <see cref="CarryPiece"/>, <see cref="PumpPiece"/>; a
    /// <see cref="IWheelPiece"/> later). Island-local, positioned in map tiles, so they work at any island scale and
    /// while the map scrolls. Each piece:
    /// - only works while its <see cref="map"/> shows outside, it's in the window and the explorer is within
    ///   <see cref="reach"/> tiles (<see cref="Usable"/>);
    /// - reports <see cref="ProgressChanged"/> and <see cref="Solved"/> and keeps its state in <see cref="PuzzleState"/>
    ///   (saved), re-reading it on Start and when a save loads;
    /// - teaches itself the first time it's usable in a session: its <see cref="lessonLine"/>, the gold focus and a
    ///   ghost hand (<see cref="ghostClip"/>) at its dialogue target <c>puzzle:&lt;id&gt;</c>, until the player first
    ///   touches it;
    /// - reports the hand holding it to <see cref="IslandHandle.MarkHandHeld"/> (gaze walking, tile touch and
    ///   microgestures leave that hand alone).
    /// Scene-specific effects (dust, lines, the trail opening) live in listeners (<see cref="FallenLog"/>).
    /// </summary>
    public abstract class HandPuzzle : MonoBehaviour
    {
        [Header("Puzzle")]
        [Tooltip("Save id (cinder.log). Also the dialogue / ghost target puzzle:<id>.")]
        public string id;
        [Tooltip("The map it lives on: it only shows and works while this map is outside.")]
        public WorldMap map;
        public IslandBuilder island;
        public WorldWindow window;
        public GestureTestPiece explorer;
        [Tooltip("Pinches from a hand busy with the island are ignored.")]
        public IslandHandle grabGuard;
        [Tooltip("Optional: the lesson's ghost hand.")]
        public GhostHands ghosts;
        [Tooltip("Optional: dust, sparks and glows.")]
        public BattleFx fx;

        [Header("Reach")]
        [Tooltip("Usable while the explorer is this close (tiles, either axis).")]
        [Min(0)] public int reach = 4;

        [Header("Lesson (first time it's usable in a session, until it's first touched)")]
        [Tooltip("Dialogue trigger (a once line).")]
        public string lessonLine;
        [Tooltip("Ghost clip at puzzle:<id>. Empty = no ghost.")]
        public string ghostClip;
        public float ghostScale = 1f;
        [Tooltip("Step 10b: when set, the lesson line is pinned (it stays, shrinking to this chip) until the lesson is complete " +
                 "(LessonComplete, or solved), not just until the first touch. Empty = the 9b behaviour (said once).")]
        public string lessonChip;

        /// <summary>0..1 (a pump's strokes, a carry lifted / placed).</summary>
        public event Action<HandPuzzle, float> ProgressChanged;
        public event Action<HandPuzzle> Solved;

        public float Progress { get; private set; }
        public bool IsSolved => PuzzleState.IsSolved(id);
        public bool LessonShowing { get; private set; }
        /// <summary>A hand (or look + pinch) works it right now.</summary>
        public abstract bool Busy { get; }

        /// <summary>Where the ghost's key point and the gold focus go (world), or null when not shown.</summary>
        public abstract Vector3? FocusPoint { get; }
        /// <summary>Gold focus radius (world).</summary>
        public virtual float FocusRadius => 0.8f * TileWorld;
        /// <summary>The map tile it sits on (reach, in view).</summary>
        protected abstract Vector2 HomeTile { get; }
        /// <summary>Reads <see cref="PuzzleState"/> (on Start and when a save loads).</summary>
        protected abstract void ApplyState();

        protected bool Interacted;
        static readonly Dictionary<string, HandPuzzle> _byId = new Dictionary<string, HandPuzzle>();
        readonly bool[] _wasPinching = { true, true };
        readonly bool[] _wasFist = { true, true };

        // ---- where ----

        /// <summary>Its map shows outside (not inside an interior, not mid swap).</summary>
        public virtual bool Here => island != null && map != null && island.world == map && !SceneSwap.IsInside && !SceneSwap.IsBusy;

        public bool InView => Here && (window == null || !window.IsZoomed) && island.FootprintContains(island.TileToLocal(HomeTile, island.Content.capHeight));

        public bool Reachable => explorer != null && Mathf.Max(Mathf.Abs(explorer.Tile.x - HomeTile.x), Mathf.Abs(explorer.Tile.y - HomeTile.y)) <= reach + 0.5f;

        /// <summary>In view, the explorer near, no battle, opening or tutorial.</summary>
        public bool Usable => InView && Reachable && !Opening.InProgress && !Onboarding.Running && (!explorer.Locked || Busy);

        /// <summary>One map tile in world metres right now.</summary>
        protected float TileWorld => island != null && island.map != null ? island.map.tileSize * island.Zoom * island.transform.lossyScale.x : 0.05f;

        // ---- lifetime ----

        protected virtual void Start()
        {
            if (island == null || explorer == null || map == null || string.IsNullOrEmpty(id))
            {
                Debug.LogError($"[{GetType().Name}] {name}: needs an id, the island, the explorer and its map (Set Up MR_Main (9b puzzles)).", this);
                enabled = false;
                return;
            }
            _byId[id] = this;
            Dialogue.RegisterTargetResolver("puzzle", arg => _byId.TryGetValue(arg, out var p) && p != null
                ? new Dialogue.Target { point = () => p != null ? p.FocusPoint : null, pulse = k => { }, radius = () => p != null ? p.FocusRadius : 0f }
                : null);
            // Step 9h: where a carried piece's ghost lets go (the nearest spot), "puzzleTo:<id>".
            Dialogue.RegisterTargetResolver("puzzleTo", arg => _byId.TryGetValue(arg, out var p) && p != null
                ? new Dialogue.Target { point = () => p != null ? p.GhostDropPoint : null, pulse = k => { }, radius = () => 0f }
                : null);
            PuzzleState.Restored += OnRestored;
            ApplyState();
        }

        protected virtual void OnDestroy()
        {
            if (_pinned) Dialogue.Unpin();
            PuzzleState.Restored -= OnRestored;
            if (id != null && _byId.TryGetValue(id, out var p) && p == this) _byId.Remove(id);
            EndLesson();
        }

        void OnRestored()
        {
            Interacted = false;
            ApplyState();
        }

        protected virtual void Update()
        {
            UpdateLesson();
        }

        // ---- progress ----

        protected void SetProgress(float p)
        {
            p = Mathf.Clamp01(p);
            if (Mathf.Approximately(p, Progress)) return;
            Progress = p;
            ProgressChanged?.Invoke(this, p);
        }

        /// <summary>Restores progress without raising the event (loading).</summary>
        protected void SetProgressQuiet(float p) => Progress = Mathf.Clamp01(p);

        protected void MarkSolved(int stage)
        {
            PuzzleState.Solve(id, stage);
            SetProgress(1f);
            EndLesson();
            Debug.Log($"[{GetType().Name}] {id} solved (stage {stage}).");
            Solved?.Invoke(this);
        }

        /// <summary>First touch this session: the lesson is over.</summary>
        protected void MarkInteracted()
        {
            if (!Interacted) Debug.Log($"[{GetType().Name}] {id}: first touch{(LessonShowing ? ", lesson over" : "")}.");
            Interacted = true;
            EndLesson();
        }

        // ---- lesson ----

        /// <summary>Step 9h: where the lesson's grab ghost lets go (a carry's spot); null = the ghost plays at the piece as before.</summary>
        public virtual Vector3? GhostDropPoint => null;

        /// <summary>Step 10e: the ghost shows both hands (mirrored), this far apart (world metres; 0 = the ghost's default).</summary>
        protected virtual bool GhostBothHands => false;
        protected virtual float GhostSpread => 0f;

        /// <summary>Whether it can be worked at all right now (the statue socket: only while a shard is held).</summary>
        protected virtual bool Available => true;

        void UpdateLesson()
        {
            bool want = !Interacted && !IsSolved && Usable && Available && !Busy;
            if (want && !LessonShowing) BeginLesson();
            else if (!want && LessonShowing) EndLesson();
            UpdatePin();
        }

        bool _pinned, _lessonComplete;

        /// <summary>Step 10b: the lesson's pinned line closes (the action it taught worked once).</summary>
        protected void LessonComplete()
        {
            if (_lessonComplete) return;
            _lessonComplete = true;
            UpdatePin();
        }

        /// <summary>The pinned lesson line stays while the lesson shows or was touched but not yet completed, and the piece is usable.</summary>
        void UpdatePin()
        {
            if (string.IsNullOrEmpty(lessonChip) || string.IsNullOrEmpty(lessonLine)) return;
            bool want = !_lessonComplete && !IsSolved && Usable && Available && (LessonShowing || Interacted);
            if (want == _pinned) return;
            _pinned = want;
            if (want)
            {
                Dialogue.Pin(new[] { lessonLine }, lessonChip);
                Dialogue.Say(lessonLine);
            }
            else Dialogue.Unpin();
        }

        void BeginLesson()
        {
            LessonShowing = true;
            if (!string.IsNullOrEmpty(lessonLine) && string.IsNullOrEmpty(lessonChip)) Dialogue.Say(lessonLine);
            FocusHighlight.SetPending(() => this != null ? FocusPoint : null, FocusRadius);
            if (ghosts != null && !string.IsNullOrEmpty(ghostClip))
            {
                // A piece you carry: the ghost grabs it and lets go over its spot (step 9h); others play at the piece.
                if (GhostDropPoint.HasValue)
                    ghosts.Show(ghostClip, GhostPlace.AtTarget, "puzzleTo:" + id, ghostScale, DominantHand.Left, false, 0f, "puzzle:" + id);
                else ghosts.Show(ghostClip, GhostPlace.AtTarget, "puzzle:" + id, ghostScale, DominantHand.Left, GhostBothHands, GhostSpread);
            }
            Debug.Log($"[{GetType().Name}] {id}: lesson ({lessonLine}, ghost {(string.IsNullOrEmpty(ghostClip) ? "none" : ghostClip)}).");
        }

        void EndLesson()
        {
            if (!LessonShowing) return;
            LessonShowing = false;
            FocusHighlight.Clear();
            if (ghosts != null && ghosts.ClipName == ghostClip && !ghosts.previewing) ghosts.Hide();
        }

        // ---- hands ----

        /// <summary>One hand this frame: tracked, pinching (thumb + index) or a closed fist (palm grab), and where.</summary>
        protected struct HandState
        {
            public bool left, tracked, pinch, fist, pinchStarted, fistStarted, busy;
            public Vector3 pinchPoint, palm;
            public bool Holding => pinch || fist;
            /// <summary>The pinch point while pinching, the palm while a fist.</summary>
            public Vector3 GripPoint => pinch ? pinchPoint : palm;
        }

        /// <summary>Both hands this frame (call once per Update; the started flags are edges since the last call).</summary>
        protected void ReadHands(HandState[] hands)
        {
            for (int h = 0; h < 2; h++)
            {
                bool left = h == 0;
                var hand = HandRegistry.Get(left ? Handedness.Left : Handedness.Right);
                var s = new HandState { left = left, tracked = hand != null && hand.IsTrackedDataValid };
                if (s.tracked)
                {
                    s.pinch = hand.GetIndexFingerIsPinching() && IslandHandle.TryGetPinchPoint(hand, out s.pinchPoint);
                    bool palm = HandLookup.TryGetJoint(hand, HandJointId.HandPalm, out s.palm);
                    s.fist = palm && IsFist(hand, s.palm);
                }
                s.busy = grabGuard != null && grabGuard.IsHandBusy(left);
                s.pinchStarted = s.pinch && !_wasPinching[h];
                s.fistStarted = s.fist && !_wasFist[h];
                _wasPinching[h] = s.pinch;
                _wasFist[h] = s.fist;
                hands[h] = s;
            }
        }

        /// <summary>Middle and ring fingertips curled in near the palm (a palm grab).</summary>
        static bool IsFist(IHand hand, Vector3 palm)
        {
            const float curled = 0.055f;
            return HandLookup.TryGetJoint(hand, HandJointId.HandMiddleTip, out var middle) && (middle - palm).magnitude < curled &&
                   HandLookup.TryGetJoint(hand, HandJointId.HandRingTip, out var ring) && (ring - palm).magnitude < curled;
        }

        // ---- map space ----

        /// <summary>A world point as (map tile column, height in tiles above the cap, map tile row).</summary>
        protected Vector3 WorldToMap(Vector3 world)
        {
            Vector3 local = island.transform.InverseTransformPoint(world);
            Vector2 tile = island.LocalToTile(local);
            float height = (local.y - island.CapTopLocal) / Mathf.Max(1e-6f, island.map.tileSize * island.Zoom);
            return new Vector3(tile.x, height, tile.y);
        }

        /// <summary>(column, height in tiles, row) to world.</summary>
        protected Vector3 MapToWorld(Vector3 m) =>
            island.transform.TransformPoint(island.TileToLocal(new Vector2(m.x, m.z), island.Content.capHeight + m.y * island.map.tileSize));

        /// <summary>A pinch rising edge on a hand that isn't busy with the island (look + pinch).</summary>
        protected static bool GazePinch(HandState[] hands)
        {
            foreach (var h in hands)
                if (h.pinchStarted && !h.busy) return true;
            return false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _byId.Clear();
    }

    /// <summary>
    /// Stub for the third hand puzzle piece, not built yet (Lilymere's sluice wheel, Galewick's windmill): a wheel you
    /// turn round an axis by gripping its rim. Progress = turns / turns needed; spring-less, it keeps its angle; decay
    /// optional (the water pushes it back). Implement as a <see cref="HandPuzzle"/> like <see cref="PumpPiece"/>.
    /// </summary>
    public interface IWheelPiece
    {
        /// <summary>Axis in map space (column, up, row), through <see cref="Hub"/>.</summary>
        Vector3 Axis { get; }
        /// <summary>The wheel's centre (column, height in tiles, row).</summary>
        Vector3 Hub { get; }
        /// <summary>Total turned (degrees, signed: only the wanted direction counts toward <see cref="TurnsNeeded"/>).</summary>
        float Angle { get; }
        float TurnsNeeded { get; }
        /// <summary>Look + pinch the rim: turns it a quarter by itself.</summary>
        void NudgeQuarter();
    }
}
