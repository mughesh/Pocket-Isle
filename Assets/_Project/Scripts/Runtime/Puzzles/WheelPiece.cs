using System;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Wheel (step 10b, the hand puzzle kit's third piece; Galewick's windmills): sails on a hub facing the player (+Z in
    /// map space) that you flick round with a fingertip. A fingertip moving across the sails' disc (within
    /// <see cref="nearPlane"/> of their plane, inside <see cref="radius"/> + <see cref="slack"/>) faster than
    /// <see cref="WheelSpin.minFlickSpeed"/> along the turning direction adds spin by its speed; drag slows it again
    /// (<see cref="WheelSpin"/>). <see cref="flicksNeeded"/> flicks within <see cref="flickWindow"/> s solve it (0 = a toy that
    /// never solves); then it keeps turning, saved. Look + pinch the sails = one flick. The realm restored = it turns anyway.
    /// It draws its own prop (<see cref="recipe"/>, the map's copy at <see cref="anchor"/> hidden) with the spinning pieces
    /// at its own angle (PropView.spinSelf).
    /// </summary>
    [DefaultExecutionOrder(-20)]
    public class WheelPiece : HandPuzzle, IWheelPiece
    {
        [Header("Wheel")]
        [Tooltip("The prop's anchor on the map (props grid: column, row); drawn here, hidden on the island.")]
        public Vector2Int anchor;
        [Tooltip("The prop's footprint (tiles): where its centre is.")]
        public Vector2Int footprint = Vector2Int.one;
        public PropRecipe recipe;
        [Tooltip("The hub, in the prop's shape units (tiles; +Z faces the player): PropKit.MillHub for the big mill.")]
        public Vector3 hubShape = new Vector3(0f, 2.25f, 0.62f);
        [Tooltip("Sail reach (tiles).")]
        public float radius = 1.25f;
        [Tooltip("A fingertip this close to the sails' plane counts (world metres; step 11d: 4 cm, was 2.2).")]
        public float nearPlane = 0.04f;
        [Tooltip("Extra world metres on the radius (hand tracking wobble; step 11d: 2.5 cm, was 1.5).")]
        public float slack = 0.025f;
        [Tooltip("Tiles round the sails (in front, behind and past the tips) where a fingertip doesn't walk the explorer.")]
        public float touchGuard = 1.2f;
        [Tooltip("Per hand: after a flick, the next counts after this long (one swipe = one flick).")]
        public float handCooldown = 0.25f;

        [Header("Spin (WheelSpin)")]
        public float drag = 1.5f;
        [Tooltip("Step 11d: 0.35 m/s (was 0.5): the flick was hard to hit.")]
        public float minFlickSpeed = 0.35f;
        public float maxFlickScale = 2.5f;
        [Tooltip("Flicks within the window that solve it. 0 = a toy (never solves, no lesson).")]
        public int flicksNeeded = 3;
        public float flickWindow = 4f;
        [Tooltip("Turning speed once solved or restored (radians a second).")]
        public float keepSpeed = 2.2f;
        [Tooltip("Look + pinch: a flick this fast (m/s).")]
        public float lookFlickSpeed = 0.8f;

        /// <summary>A flick counted (tangential speed, flicks in the window).</summary>
        public event Action<WheelPiece, float, int> Flicked;

        public WheelSpin Spin { get; } = new WheelSpin();
        public override bool Busy => false;
        public override Vector3? FocusPoint => Here ? HubWorld : (Vector3?)null;
        public override float FocusRadius => radius * 0.9f * TileWorld;

        // IWheelPiece
        public Vector3 Axis => Vector3.forward;
        public Vector3 Hub => new Vector3(Center.x - hubShape.x, hubShape.y, Center.y + hubShape.z);
        public float Angle => Spin.Angle * Mathf.Rad2Deg;
        public float TurnsNeeded => flicksNeeded;
        public void NudgeQuarter() => DoFlick(lookFlickSpeed, "nudge");

        // ---- step 11d: the objective (gold on the sails, "Flicks n/3") ----

        public override bool CountObjective(out int done, out int total)
        {
            total = flicksNeeded;
            done = IsSolved ? flicksNeeded : Mathf.Min(Spin.RecentFlicks, flicksNeeded);
            return flicksNeeded > 0;
        }

        protected override int MarkCount => flicksNeeded > 0 ? 1 : 0;

        protected override bool Mark(int i, out Vector3 world, out float tiles)
        {
            world = HubWorld;
            tiles = radius * 1.6f;
            return true;
        }

        PropView _view;
        readonly HandState[] _hands = new HandState[2];
        // Step 11d: index and middle fingertips both flick (hand x 2 + finger).
        static readonly HandJointId[] FlickTips = { HandJointId.HandIndexTip, HandJointId.HandMiddleTip };
        readonly Vector3[] _lastTip = new Vector3[4];
        readonly bool[] _hasTip = new bool[4];
        float _jolt;
        readonly float[] _cool = new float[2];
        bool _gazed;

        Vector2 Center => anchor + new Vector2((footprint.x - 1) * 0.5f, (footprint.y - 1) * 0.5f);
        protected override Vector2 HomeTile => Center;

        /// <summary>The hub now (world).</summary>
        public Vector3 HubWorld => MapToWorld(Hub);

        protected override void Start()
        {
            Spin.drag = drag;
            Spin.minFlickSpeed = minFlickSpeed;
            Spin.maxFlickScale = maxFlickScale;
            Spin.flicksNeeded = flicksNeeded;
            Spin.flickWindow = flickWindow;
            Spin.keepSpeed = keepSpeed;
            if (flicksNeeded <= 0)
            {
                lessonLine = null;
                ghostClip = null;
                lessonChip = null;
            }
            if (island != null && recipe != null) _view = new PropView($"Wheel {id}", island, recipe);
            base.Start();
        }

        protected override void OnDestroy()
        {
            _view?.Destroy();
            if (map != null) map.SetPropHidden(anchor, false);
            base.OnDestroy();
        }

        protected override void ApplyState()
        {
            Spin.Reset();
            if (IsSolved) Spin.Keep();
            SetProgressQuiet(IsSolved ? 1f : 0f);
            if (map != null && _view != null) map.SetPropHidden(anchor, true);
        }

        protected override bool Available => flicksNeeded > 0;

        protected override void Update()
        {
            ReadHands(_hands);
            bool usable = Usable;
            if (usable) Fingertips();
            else for (int k = 0; k < _hasTip.Length; k++) _hasTip[k] = false;
            if (usable && !IsSolved) LookPinch();
            else _gazed = false;
            if (!Spin.Kept && RealmState.Restored > 0.5f && Here) Spin.Keep();
            if (Spin.Step(Time.deltaTime) && flicksNeeded > 0 && !IsSolved)
            {
                Debug.Log($"[WheelPiece] {id}: {flicksNeeded} flicks within {flickWindow:0.#} s: it keeps turning.");
                if (explorer != null) explorer.Cheer();
                MarkSolved(1);
            }
            _jolt = Mathf.MoveTowards(_jolt, 0f, Time.deltaTime * 4f);
            if (_view != null)
            {
                // The sails' angle is the wheel's own, accumulated (never recomputed from time or reset by the map scrolling).
                _view.spinSelf = Spin.Angle;
                _view.Place(Center, 0f, Quaternion.identity, Here, 1f + 0.06f * Mathf.Sin(_jolt * Mathf.PI));
            }
            for (int h = 0; h < 2; h++) _cool[h] = Mathf.Max(0f, _cool[h] - Time.deltaTime);
            base.Update();
        }

        /// <summary>
        /// Index or middle fingertips crossing the sails' disc fast enough along the turn add spin (step 11d: either
        /// finger, a thicker zone, a lower speed; any movement through the disc with a part along the turn counts).
        /// </summary>
        void Fingertips()
        {
            Vector3 hub = HubWorld;
            Vector3 normal = (MapToWorld(Hub + Vector3.forward) - hub).normalized; // toward the player
            float tile = TileWorld;
            float reach = radius * tile + slack;
            for (int h = 0; h < 2; h++)
            {
                var hand = HandRegistry.Get(h == 0 ? Handedness.Left : Handedness.Right);
                for (int f = 0; f < FlickTips.Length; f++)
                {
                    int k = h * 2 + f;
                    if (_hands[h].busy || !HandLookup.TryGetJoint(hand, FlickTips[f], out var tip))
                    {
                        _hasTip[k] = false;
                        continue;
                    }
                    if (!_hasTip[k] || Time.deltaTime <= 0f)
                    {
                        _lastTip[k] = tip;
                        _hasTip[k] = true;
                        continue;
                    }
                    Vector3 velocity = (tip - _lastTip[k]) / Time.deltaTime;
                    _lastTip[k] = tip;
                    if (!WheelRules.Flick(tip, velocity, hub, normal, reach, nearPlane, 0.15f * tile, minFlickSpeed, touchGuard * tile, out float speed, out bool guard))
                    {
                        // A fingertip at the sails is flicking, not touching tiles: its dips onto the grass don't walk (Oct 27).
                        if (guard) TileTouch.Block(h == 0, 0.8f);
                        continue;
                    }
                    TileTouch.Block(h == 0, 0.8f);
                    if (_cool[h] > 0f) continue;
                    _cool[h] = handCooldown;
                    MarkInteracted();
                    DoFlick(speed, $"{(h == 0 ? "left" : "right")} {(f == 0 ? "index" : "middle")} fingertip");
                }
            }
        }

        void DoFlick(float speed, string how)
        {
            if (!Spin.Flick(speed)) return;
            int recent = Spin.RecentFlicks;
            _jolt = 1f;
            Debug.Log($"[WheelPiece] {id}: flick by the {how} at {speed:0.00} m/s ({recent} in {flickWindow:0.#} s).");
            if (fx != null)
            {
                float tile = TileWorld;
                fx.Burst(HubWorld, 6, new Color(0.96f, 0.95f, 0.9f, 0.8f), tile * 0.6f, 0.5f, tile * 0.04f, Vector3.up * tile * 0.2f);
            }
            Flicked?.Invoke(this, speed, recent);
        }

        void LookPinch()
        {
            _gazed = Gazed();
            if (!_gazed) return;
            FocusHighlight.Hover(HubWorld, FocusRadius);
            if (GazePinch(_hands))
            {
                MarkInteracted();
                DoFlick(lookFlickSpeed, "look + pinch");
            }
        }

        bool Gazed()
        {
            if (!HeadGaze.TryGetRay(out var ray)) return false;
            var pick = new HeadGaze.ConePick(ray, _gazed ? 0 : -1);
            pick.Consider(0, HubWorld, Mathf.Max(HeadGaze.MinRadius * island.transform.lossyScale.x, radius * TileWorld));
            return pick.Best == 0;
        }

        /// <summary>Debug (bridge): a flick as if by a fingertip at <paramref name="speed"/> m/s.</summary>
        public void DebugFlick(float speed = 0.9f)
        {
            if (!Here) return;
            MarkInteracted();
            DoFlick(speed, "bridge");
        }
    }
}
