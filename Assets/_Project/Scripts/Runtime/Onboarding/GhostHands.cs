using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Translucent ghost hands that show a lesson's gesture (step 8b): a copy of the rig's own ISDK hand mesh (one per
    /// side, made at start, every behaviour and collider stripped: never grabbable, never a gaze or ray target) driven by
    /// a <see cref="GhostHandClip"/>, looped with a short fade at the loop point, drawn with <c>Diorama/Ghost Hand</c>
    /// (pale blue, bright rim, pulled toward the eye so the island never hides it). One draw per hand.
    /// Placement: at the lesson's target (real size, reaching in from the player's side), above the island's front
    /// (thumb demos, <see cref="Onboarding"/> lesson scale, the Spirit beside it: dialogue target <c>ghost</c>) or above a
    /// target tile. It shows the player's dominant hand (<see cref="DominantHand"/>, mirroring the clip when needed). After
    /// <see cref="struggleLoops"/> loops without the lesson done it slows down and grows a little. With no hands tracked it
    /// shows both hands raised. Recorded clips (<see cref="library"/>) replace the code placeholders by name.
    /// </summary>
    [DefaultExecutionOrder(120)]
    public class GhostHands : MonoBehaviour
    {
        public Onboarding onboarding;
        public IslandBuilder island;
        [Tooltip("Step 11d: a tiny gold spark where a pinch ghost's thumb and index close (optional).")]
        public BattleFx fx;
        [Tooltip("The rig's hand visuals the ghosts copy (mesh + skeleton).")]
        public HandVisual leftSource, rightSource;
        public Material material;
        [Tooltip("Recorded clips; a clip here replaces the placeholder of the same name.")]
        public GhostClipLibrary library;

        [Header("Placement")]
        [Tooltip("Above-island demos float this high over the island's front half (m).")]
        public float aboveIslandHeight = 0.12f;
        [Tooltip("How far toward the player from the island's middle the demo floats (share of the half depth).")]
        [Range(0f, 1f)] public float aboveIslandFront = 0.55f;
        [Tooltip("Above-target demos float this high over the target (m).")]
        public float aboveTargetHeight = 0.07f;
        [Header("Thumb demos (step 17 F9)")]
        [Tooltip("Unused since Nov 7 (beside the rim the ghost went dull: the real hand rests there and fades it). Was: offset from the dominant front corner.")]
        public Vector3 thumbRimOffset = new Vector3(0.06f, 0.05f, 0.05f);
        [Tooltip("Nov 7: thumb demos float over the island's centre, this high over the cap (m at island scale 1).")]
        public float thumbCenterHeight = 0.09f;
        [Tooltip("Nov 7: the walk lesson's composed clip (forward swipe, then left, then right): the thumb's sweep across the index finger (degrees) and each side's seconds.")]
        public float sideSwipeAngle = 28f;
        public float sideSwipeSeconds = 0.75f;
        [Tooltip("Thumb demos are this much bigger than the lesson's scale (they read tiny over the map).")]
        public float thumbScale = 1.6f;
        [Tooltip("Nov 7 (Mugesh: the swipe ghost looked tilted, fingers toward the upper left; Nov 8 still tilted at 30): the thumb demo turned this many degrees about up (+ = clockwise from above) so the fingers point straight away.")]
        public float thumbYaw = 55f;
        [Tooltip("Nov 8 (Mugesh: still tilted, the thumb leaning left): the thumb demo rolled this many degrees about its forward axis (- = its left side up).")]
        public float thumbRoll = 0f;
        [Header("Step 21: thumb demos beside the island, with arrows")]
        [Tooltip("Mugesh (video, Nov 8): over the island's middle the swipe ghost read as a big hand lying on the map. Thumb demos float " +
                 "beside the island on the dominant hand's side instead, facing the player: this far out past the side rim, this high over " +
                 "the cap, this far toward the player from the middle (island m at scale 1).")]
        public bool thumbBeside = true;
        public Vector3 thumbBesideSpot = new Vector3(0.02f, 0.13f, 0.06f);
        [Tooltip("Step 21 (Mugesh, after the side spot: keep it over the island's centre, up): the demo floats this high over the cap's centre, facing the head (wins over Thumb Beside).")]
        public bool thumbOverCentre = true;
        public float thumbCentreUp = 0.18f;
        [Tooltip("A gold arrow for each swipe stroke (the way the explorer walks for it): length and width at the ghost's scale.")]
        public bool thumbArrows = true;
        public float arrowLength = 0.07f, arrowWidth = 0.035f;
        [Tooltip("Step 21: the arrow's material (Diorama/Stroke Arrow, Materials/StrokeArrow): a glowing gradient ribbon.")]
        public Material arrowMaterial;
        [Tooltip("Step 21: the swipe demo's thumb travel (m at hand size): across the index finger (left / right) and along it (forward / back).")]
        public float swipeAcross = 0.016f, swipeAlong = 0.02f;
        [Tooltip("Step 21: each stroke's seconds: out, a beat, back, the pause before the next.")]
        public float strokeOut = 0.22f, strokeHold = 0.15f, strokeBack = 0.25f, strokePause = 0.25f;
        [Tooltip("Step 21 (Mugesh set this pose by hand on Nov 8: upright, fingers up and away, palm in): the thumb demo's hand " +
                 "root turned this way in the frame facing the head (Euler, degrees). Off = the clip's own turn (measuredWrist).")]
        public bool fixedThumbPose = true;
        public Vector3 thumbRootEuler = new Vector3(-18.1f, -13.1f, -88.5f);
        [Tooltip("Step 21 (Mugesh, 4th time: the swipe ghost tilted): a wrist-anchored clip is turned so the hand's own measured " +
                 "fingers (wrist to middle knuckle) point along Wrist Fingers and its measured back of the hand along Wrist Back. " +
                 "Before, the wrist joint's axes were assumed to be fingers = z, back = y; they aren't, so the hand came out tilted " +
                 "and Thumb Yaw / Thumb Roll were guesses on top. With this on they are ignored.")]
        public bool measuredWrist = true;
        [Tooltip("Nov 7 (Mugesh: the gold point looks really nice, use it wherever it helps): every ghost demo gets the gold dot and trail on the joint that does the work (thumb tip, pinch point, fingertip or palm), not only thumb demos.")]
        public bool keyDots = true;
        [Tooltip("A gold dot on the ghost's thumb tip and this many trail dots behind it (the slide).")]
        public int thumbTrailDots = 6;
        public float thumbDotSize = 0.009f;
        [Tooltip("How a wrist-anchored clip (thumb gestures) is shown: where the fingers and the back of the hand point (right hand; x right, y up, z away from the player).")]
        public Vector3 wristFingers = new Vector3(-0.35f, 0.25f, 1f);
        public Vector3 wristDorsal = new Vector3(1f, 0.35f, 0.25f);
        [Tooltip("Step 16: replaces wristDorsal: the back of the hand up and out, a little toward the head, so the palm faces " +
                 "inward/down (a palm toward the face is Meta's system gesture: the judges' video opened the system menu).")]
        public Vector3 wristBack = new Vector3(0.75f, 0.6f, -0.15f);
        [Tooltip("Both hands (show me your hands) sit this far apart (m).")]
        public float bothSpread = 0.18f;
        [Tooltip("Step 9h: the island-move ghost grabs the near rim this far in from the dominant side's corner (island-local m).")]
        public float cornerInset = 0.05f;
        [Tooltip("Island sides (pinch grab, step 8e): the clip's key joint sits this far below the cap top (island-local m) and this far out from the base (world m).")]
        public float sideDrop = 0.04f;
        public float sideOut = 0.08f;
        [Tooltip("Beside the player's hand (step 9h, the pinch lesson): this far to the outside of the dominant hand (m).")]
        public float besideHandOut = 0.08f;
        [Tooltip("Without a tracked hand: where a resting hand would be, from the head (x outward, y down, z forward; m).")]
        public Vector3 restingHand = new Vector3(0.16f, 0.3f, 0.32f);

        [Header("Feel")]
        public float fadeSeconds = 0.3f;
        [Tooltip("Struggle helper: after this many loops the ghost slows down and grows.")]
        public int struggleLoops = 3;
        public float struggleSpeed = 0.6f;
        public float struggleScale = 1.2f;
        [Tooltip("Gaze dot size (m).")]
        public float dotSize = 0.012f;
        [Tooltip("Step 16: a real hand (index tip or palm) this close to the lesson's target fades the ghost (m), so it never hides what you reach for.")]
        public float handNearRadius = 0.16f;
        [Tooltip("Step 16: the ghost's alpha while a real hand is near the target.")]
        [Range(0f, 1f)] public float handNearAlpha = 0.08f;
        [Tooltip("Step 16: seconds to fade out / back in when a hand comes near / leaves.")]
        public float handNearSeconds = 0.2f;
        [Tooltip("Step 16: 'aside' ghosts (finale lift, evolution, shard) are this much smaller and sit this far to the side of the target (m), never between the head and it.")]
        [Range(0.2f, 1f)] public float asideScale = 0.6f;
        [Tooltip("Nov 6 (Mugesh: the evolve ghost grabbed the wrong spot on the rim): 0 = aside ghosts stay on their target " +
                 "(only smaller); the fade near a real hand keeps the target visible. Was 0.1 m to the side.")]
        public float asideOffset = 0f;

        const int JointCount = (int)HandJointId.HandEnd;
        static readonly int AlphaId = Shader.PropertyToID("_Alpha");

        class Ghost
        {
            public bool left;
            public Transform root;
            public Transform[] joints = new Transform[JointCount];
            public SkinnedMeshRenderer renderer;
            public MaterialPropertyBlock block;
        }

        struct Request
        {
            public string clip;
            public GhostPlace place;
            public string target, source;
            public float scale, spread, lift;
            public bool left, both, aside;
            public Vector2 turn;
        }

        /// <summary>A clip's grab and release (seconds into the trimmed clip), found once from its thumb-index gap.</summary>
        struct Grab
        {
            public bool has;
            public float grab, release;
        }

        readonly Dictionary<GhostHandClip, Grab> _grabs = new Dictionary<GhostHandClip, Grab>();

        Ghost _left, _right;
        Transform _dot;
        MeshRenderer _dotRenderer;
        MaterialPropertyBlock _dotBlock;
        readonly Dictionary<string, GhostHandClip> _placeholders = new Dictionary<string, GhostHandClip>();
        readonly Quaternion[] _rot = new Quaternion[JointCount];
        readonly Vector3[] _pos = new Vector3[JointCount];
        Request _request, _saved;
        bool _showing, _hasSaved;
        GhostHandClip _clip;
        float _lastT = -1f;
        float _time, _visible, _struggle, _handNear;
        int _loops;
        Vector3 _keyWorld;
        bool _keyValid;
        readonly HashSet<string> _warned = new HashSet<string>();

        /// <summary>Step 16: the scene's ghost hands (for <see cref="ObjectiveArea"/>).</summary>
        public static GhostHands Current { get; private set; }
        /// <summary>Step 16: where a grab clip lets go (the drop spot), while it shows.</summary>
        public Vector3? ReleasePoint => IsShowing && _keyValid && _releaseValid ? _releaseWorld : (Vector3?)null;
        Vector3 _releaseWorld;
        bool _releaseValid;

        /// <summary>A ghost is showing (or fading out).</summary>
        public bool IsShowing => _showing || _visible > 0.001f;
        /// <summary>Where the demonstrated spot is now (the "ghost" dialogue target).</summary>
        public Vector3? KeyPoint => IsShowing && _keyValid ? _keyWorld : (Vector3?)null;
        public string ClipName => _showing ? _request.clip : null;
        /// <summary>Seconds into the clip (trimmed) and its playable length, for the preview's progress bar.</summary>
        public float ClipTime => _clip != null ? Mathf.Min(_time * Mathf.Max(0.05f, _clip.speed), _clip.Duration) : 0f;
        public float ClipDuration => _clip != null ? _clip.Duration : 0f;

        void Start()
        {
            Current = this;
            if (material == null) Debug.LogError("[GhostHands] No material (Set Up MR_Main (8b onboarding)): no ghost hands.", this);
            _left = Build(leftSource, true);
            _right = Build(rightSource, false);
            if (_left == null && _right == null)
            {
                Debug.LogError("[GhostHands] No hand visual to copy (leftSource / rightSource): no ghost hands.", this);
                enabled = false;
                return;
            }
            LoadDeviceClips();
            MakePlaceholders();
            ComposeSwipeAll();
            MakeDot();
            MakeThumbDots();
            Dialogue.RegisterTarget("ghost", () => KeyPoint, k => { }, () => 0f);
            // Step 9h: the island-move ghost grabs the near rim's corner on the dominant side (island +X is the player's left).
            Dialogue.RegisterTarget("rimCorner", RimCorner, k => { }, () => 0f);
            if (onboarding != null)
            {
                onboarding.LessonShown += OnLessonShown;
                onboarding.LessonEnded += OnLessonEnded;
                onboarding.HandsMissingChanged += OnHandsMissing;
            }
            SetVisible(0f);
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
            if (onboarding != null)
            {
                onboarding.LessonShown -= OnLessonShown;
                onboarding.LessonEnded -= OnLessonEnded;
                onboarding.HandsMissingChanged -= OnHandsMissing;
            }
            foreach (var c in _placeholders.Values)
                if (c != null) Destroy(c);
        }

        // ---- lessons ----

        /// <summary>Editor preview (Ghost Clips window): lessons leave the ghost alone while it is set.</summary>
        [System.NonSerialized] public bool previewing;

        void OnLessonShown(LessonData l)
        {
            if (previewing) return;
            _hasSaved = false;
            if (string.IsNullOrEmpty(l.ghost) || l.place == GhostPlace.None)
            {
                Hide();
                return;
            }
            Show(l.ghost, l.place, l.target, l.ghostScale, DominantHand.Left, l.bothHands, l.ghostSpread, l.source, l.targetLift, turn: l.ghostTurn);
        }

        /// <summary>The near rim's corner on the dominant side, a little below the cap (where a hand takes the rocky edge).</summary>
        Vector3? RimCorner() => CornerOf(DominantHand.Left);

        /// <summary>The near rim's corner on the player's left or right (island +X is the player's left).</summary>
        Vector3? CornerOf(bool left)
        {
            if (island == null || island.map == null) return null;
            var half = island.FootprintHalfSize;
            float x = left ? half.x - cornerInset : -(half.x - cornerInset);
            return island.transform.TransformPoint(new Vector3(x, island.CapTopLocal - sideDrop, half.y));
        }

        void OnLessonEnded(LessonData l, bool done)
        {
            if (previewing) return;
            _hasSaved = false;
            Hide();
        }

        void OnHandsMissing(bool missing)
        {
            if (previewing) return;
            if (missing)
            {
                _saved = _request;
                _hasSaved = _showing;
                Show(onboarding != null && onboarding.script != null ? onboarding.script.handsGhost : "hands_up", GhostPlace.AboveIsland, null, 0.8f, false, true);
                return;
            }
            if (_hasSaved) Show(_saved.clip, _saved.place, _saved.target, _saved.scale, _saved.left, _saved.both, _saved.spread, turn: _saved.turn);
            else Hide();
            _hasSaved = false;
        }

        /// <summary>
        /// Shows a clip (restarts it). Left = show the left hand (the clip is mirrored if it was recorded with the other).
        /// At a target the size is also x the library's <see cref="GhostClipLibrary.targetScale"/>. Spread = both hands' gap (0 = default).
        /// </summary>
        /// <param name="source">Step 9h, grab clips: where the grab lands; <paramref name="target"/> is then where the release lands.</param>
        /// <param name="lift">The release spot sits this far above the target (m).</param>
        /// <param name="aside">Step 16: smaller and off to the dominant side of the target (finale lift, evolution, shard).</param>
        public void Show(string clip, GhostPlace place, string target, float scale, bool left, bool both, float spread = 0f, string source = null, float lift = 0f, bool aside = false, Vector2 turn = default)
        {
            var c = Find(clip);
            if (c == null)
            {
                if (_warned.Add(clip)) Debug.LogError($"[GhostHands] No clip '{clip}' (recorded or placeholder).", this);
                Hide();
                return;
            }
            _request = new Request
            {
                clip = clip, place = place, target = target, source = source, scale = Mathf.Max(0.1f, scale), left = left, both = both,
                spread = spread, lift = lift, aside = aside, turn = turn,
            };
            _clip = c;
            _showing = true;
            _time = 0f;
            _lastT = -1f;
            _loops = 0;
            _struggle = 0f;
            var grab = GrabOf(c);
            Debug.Log($"[GhostHands] {clip} ({(c.hideFlags == HideFlags.DontSave ? "placeholder" : "recorded")}) {place}" +
                      $"{(string.IsNullOrEmpty(source) ? "" : $" grabs at {source} ({(grab.has ? $"grab {grab.grab:0.00} s, release {grab.release:0.00} s" : "no grab in the clip: key frame there")})")}" +
                      $"{(string.IsNullOrEmpty(target) ? "" : (string.IsNullOrEmpty(source) ? " at " : ", lets go at ") + target)}, " +
                      $"{(both ? "both hands" : left ? "left hand" : "right hand")}, x{scale:0.##}.");
        }

        public void Hide()
        {
            if (_showing && _request.clip != null) Debug.Log($"[GhostHands] {_request.clip}: hidden.");
            _showing = false;
        }

        bool _nearLogged;

        Request _beforePreview;
        bool _hadBeforePreview;

        /// <summary>
        /// Nov 3: the Controls card's demo. Plays a clip over the island (on unscaled time: the menu pauses the game) and
        /// keeps lessons from replacing it; <see cref="EndPreview"/> puts back whatever was showing before.
        /// </summary>
        public void BeginPreview(string clip, GhostPlace place, float scale)
        {
            if (!previewing)
            {
                _beforePreview = _request;
                _hadBeforePreview = _showing;
            }
            previewing = true;
            Show(clip, place, null, scale, DominantHand.Left, false);
        }

        public void EndPreview()
        {
            if (!previewing) return;
            previewing = false;
            var r = _beforePreview;
            if (_hadBeforePreview) Show(r.clip, r.place, r.target, r.scale, r.left, r.both, r.spread, r.source, r.lift);
            else Hide();
            _hadBeforePreview = false;
        }

        GhostHandClip Find(string name)
        {
            var recorded = library != null ? library.Find(name) : null;
            if (recorded != null && recorded.frames.Count > 1) return recorded;
            return _placeholders.TryGetValue(name, out var p) ? p : null;
        }

        // ---- every frame ----

        /// <summary>Debug shots: holds the clip at this point (0..1), fully visible. Negative = plays normally.</summary>
        [System.NonSerialized] public float debugFreeze = -1f;

        void LateUpdate()
        {
            // Step 32: the swipe-and-tap lesson: a swipe while the explorer stands, a tap as soon as it walks.
            var want = previewing || _hasSaved ? null : Onboarding.PracticeGhost;
            if (want != null && _showing && _request.clip != want)
                Show(want, _request.place, _request.target, _request.scale, _request.left, _request.both, _request.spread, turn: _request.turn);
            Tick(previewing ? Time.unscaledDeltaTime : Time.deltaTime);
        }

        /// <summary>Debug: the right ghost's skeleton wiring and where its thumb and index tips are now.</summary>
        public string DebugInfo()
        {
            var g = _right ?? _left;
            if (g == null) return "no ghost";
            int mapped = 0, inBones = 0;
            foreach (var j in g.joints)
            {
                if (j == null) continue;
                mapped++;
                if (System.Array.IndexOf(g.renderer.bones, j) >= 0) inBones++;
            }
            var thumb = g.joints[(int)HandJointId.HandThumbTip];
            var index = g.joints[(int)HandJointId.HandIndexTip];
            return $"{g.renderer.name}: {mapped} joints mapped, {inBones} are skin bones (of {g.renderer.bones.Length}), root {g.root.name} (parent {g.root.parent.name}), " +
                   $"rootBone {(g.renderer.rootBone != null ? g.renderer.rootBone.name : "-")}, thumb tip {(thumb != null ? thumb.position.ToString("F3") : "-")}, index tip {(index != null ? index.position.ToString("F3") : "-")}, " +
                   $"thumb3 local {(g.joints[(int)HandJointId.HandThumb3] != null ? g.joints[(int)HandJointId.HandThumb3].localEulerAngles.ToString("F0") : "-")}";
        }

        /// <summary>
        /// Debug (step 9e): for each clip, at its key time, the thumb-to-index tip gap and how far the palm sits from the
        /// pinch point (cm, right ghost, unscaled): a pinch grab whose key is the palm misses its spot by that much.
        /// </summary>
        public string DescribeKeys()
        {
            var g = _right ?? _left;
            if (g == null) return "no ghost";
            var sb = new System.Text.StringBuilder();
            var names = new List<string>(_placeholders.Keys);
            if (library != null)
                foreach (var c in library.clips)
                    if (c != null && !names.Contains(c.clipName)) names.Add(c.clipName);
            foreach (var n in names)
            {
                var c = Find(n);
                if (c == null) continue;
                c.Sample(c.keyTime * c.Duration, c.leftHand != g.left, out _, out _, _rot, _pos);
                Vector3 thumb = KeyInRoot(g, HandJointId.HandThumbTip), index = KeyInRoot(g, HandJointId.HandIndexTip), palm = KeyInRoot(g, HandJointId.HandPalm);
                Vector3 pinch = (thumb + index) * 0.5f;
                sb.Append($"\n  {n}: key {(c.keyPinch ? "pinch point" : c.keyJoint.ToString())}, tips {Vector3.Distance(thumb, index) * 100f:0.0} cm apart, palm {Vector3.Distance(palm, pinch) * 100f:0.0} cm from the pinch point");
            }
            return sb.ToString();
        }

        /// <summary>Poses the ghosts now (debug shots render right after).</summary>
        public void DebugPose() => Tick(0f);

        /// <summary>
        /// Step 16 (debug): which way the showing ghost's palm faces, against the head: the back of the hand is
        /// cross(index knuckle - wrist, little knuckle - wrist) for a right hand (flipped for the left).
        /// </summary>
        public string DebugPalm()
        {
            var cam = Camera.main;
            var sb = new System.Text.StringBuilder();
            foreach (var g in new[] { _left, _right })
            {
                if (g == null || g.renderer == null || !g.renderer.enabled) continue;
                var w = g.joints[(int)HandJointId.HandWristRoot];
                var i = g.joints[(int)HandJointId.HandIndex1];
                var p = g.joints[(int)HandJointId.HandPinky1];
                var palm = g.joints[(int)HandJointId.HandPalm];
                if (w == null || i == null || p == null || palm == null) continue;
                Vector3 back = Vector3.Cross(i.position - w.position, p.position - w.position).normalized * (g.left ? -1f : 1f);
                Vector3 toHead = cam != null ? (cam.transform.position - palm.position).normalized : Vector3.up;
                sb.Append($"{(g.left ? "L" : "R")}: back of the hand {back:F2}, up {back.y:F2}, palm toward the head {-Vector3.Dot(back, toHead):F2}. ");
            }
            return sb.Length > 0 ? sb.ToString() : "no ghost showing";
        }

        void Tick(float dt)
        {
            // Nov 10 (playtest): the menu stops the clock, so a lesson's ghost froze over the menu's tablets. The Controls card's own ghost still plays.
            if (Dialogue.Held && !previewing)
            {
                SetVisible(0f);
                _keyValid = false;
                return;
            }
            _visible = debugFreeze >= 0f && _showing ? 1f : Mathf.MoveTowards(_visible, _showing ? 1f : 0f, dt / Mathf.Max(0.01f, fadeSeconds));
            if (_clip == null || _visible <= 0f)
            {
                SetVisible(0f);
                _keyValid = false;
                return;
            }
            if (!TryPlace(out var point, out var frame))
            {
                // The target isn't in view (scrolled away, not spawned yet): hide until it is.
                SetVisible(0f);
                _keyValid = false;
                return;
            }
            // Step 9h: a grab clip grabs at the source and lets go at the target.
            var grab = string.IsNullOrEmpty(_request.source) ? default : GrabOf(_clip);
            Vector3? release = null;
            if (grab.has && !string.IsNullOrEmpty(_request.target) && Dialogue.TryGetTarget(_request.target, out var rt) && rt.point != null)
            {
                var p = rt.point();
                if (p.HasValue) release = p.Value + Vector3.up * _request.lift;
            }

            float speed = Mathf.Lerp(1f, struggleSpeed, _struggle) * Mathf.Max(0.05f, _clip.speed);
            float loopLength = _clip.Duration / speed + _clip.holdEnd;
            _time += dt;
            if (_time >= loopLength)
            {
                _time -= loopLength;
                _loops++;
                if (_loops == struggleLoops && _showing) Debug.Log($"[GhostHands] {_request.clip}: {struggleLoops} loops without the lesson done: slower and bigger.");
            }
            _struggle = Mathf.MoveTowards(_struggle, _loops >= struggleLoops ? 1f : 0f, dt);
            float t = Mathf.Min(_time * speed, _clip.Duration);
            float loopFade = Mathf.SmoothStep(0f, 1f, _time / 0.15f) * Mathf.SmoothStep(0f, 1f, (loopLength - _time) / 0.2f);
            if (debugFreeze >= 0f)
            {
                t = debugFreeze * _clip.Duration;
                loopFade = 1f;
            }
            // Step 16: a real hand near the target (or the release spot) fades the ghost so it never hides the target.
            // Nov 8 (Mugesh: the first pinch ghost vanished under his resting hand): demos over the island have no target
            // to uncover, so they never fade for a hand.
            // Step 33 (friend test: the wind-blow ghost disappeared): the sweep demo plays over the clouds, not on a small
            // target a hand could hide, so a resting hand nearby no longer fades it.
            bool near = _request.place != GhostPlace.BesideHand && _request.place != GhostPlace.AboveIsland && debugFreeze < 0f &&
                        _request.clip != "palm_sweep" &&
                        (HandNear(point, handNearRadius) || (release.HasValue && HandNear(release.Value, handNearRadius)));
            if (near != _nearLogged && _showing)
            {
                _nearLogged = near;
                if (near) Debug.Log($"[GhostHands] {_request.clip}: faded, a real hand is near its target.");
            }
            _handNear = Mathf.MoveTowards(_handNear, near ? 1f : 0f, dt / Mathf.Max(0.01f, handNearSeconds));
            float alpha = _visible * loopFade * NearAlpha(_handNear, handNearAlpha);
            if (_request.aside)
            {
                // Off to the dominant hand's side of the target, across the line of sight (never between head and target).
                Vector3 side = AsideDir(point, _request.left);
                point += side * asideOffset;
                if (release.HasValue) release = release.Value + side * asideOffset;
            }
            if (_request.turn != Vector2.zero && Camera.main != null)
            {
                // Step 22: seen from the head, coming in from the dominant hand's side with the wrist raised: head-on it foreshortens into a claw.
                var along = point - Camera.main.transform.position;
                along.y = 0f;
                if (along.sqrMagnitude > 1e-6f)
                    frame = Quaternion.LookRotation(along, Vector3.up) * Quaternion.Euler(_request.turn.x, _request.left ? _request.turn.y : -_request.turn.y, 0f);
            }
            float placeScale = (_request.place == GhostPlace.AtTarget || _request.place == GhostPlace.IslandSides || _request.place == GhostPlace.RimCorners) && library != null ? library.targetScale : 1f;
            float scale = _request.scale * placeScale * Mathf.Max(0.05f, _clip.size) * Mathf.Lerp(1f, struggleScale, _struggle) * (_request.aside ? asideScale : 1f)
                          * (ThumbDemo ? thumbScale : 1f);

            var a = _request.both ? _left ?? _right : _request.left ? _left ?? _right : _right ?? _left;
            var b = _request.both ? (a == _left ? _right : _left) : null;
            if (_request.place == GhostPlace.RimCorners)
            {
                // Each hand pinches its own near corner (the clip mirrored for the left hand).
                var g2 = GrabOf(_clip);
                foreach (var h in new[] { _left, _right })
                {
                    if (h == null) continue;
                    var corner = CornerOf(h.left);
                    if (!corner.HasValue) Hidden(h);
                    else Pose(h, corner.Value, frame, t, scale, alpha, 0f, g2.has ? g2.grab : -1f, g2.release, null);
                }
            }
            else if (_request.place == GhostPlace.IslandSides)
            {
                // Each hand on its own side of the base, the clip turned so the palm faces the island.
                Pose(a, SidePoint(a.left), frame, t, scale, alpha, SideYaw(a.left));
                if (b != null) Pose(b, SidePoint(b.left), frame, t, scale, alpha, SideYaw(b.left));
                else Hidden(a == _left ? _right : _left);
            }
            else
            {
                Vector3 side = frame * Vector3.right * ((_request.spread > 0f ? _request.spread : bothSpread) * 0.5f);
                Vector3 sideA = b != null ? (a.left ? -side : side) : Vector3.zero;
                Pose(a, point + sideA, frame, t, scale, alpha, 0f,
                    grab.has ? grab.grab : -1f, grab.release, release.HasValue ? release.Value + sideA : (Vector3?)null);
                // Step 12c: the second hand of a two-hand carry travels with the first (it used to stay at the source).
                Vector3 sideB = b != null ? (b.left ? -side : side) : Vector3.zero;
                if (b != null) Pose(b, point + sideB, frame, t, scale, alpha, 0f,
                    grab.has ? grab.grab : -1f, grab.release, release.HasValue ? release.Value + sideB : (Vector3?)null);
                else Hidden(a == _left ? _right : _left);
            }

            _keyWorld = point;
            _keyValid = true;
            _releaseValid = release.HasValue;
            if (release.HasValue) _releaseWorld = release.Value;
            UpdateDot(point, t / Mathf.Max(0.01f, _clip.Duration), alpha);
            UpdateThumbDots(keyDots || ThumbDemo ? a : null, alpha, scale);
            UpdateArrow(ThumbDemo ? a : null, t, alpha, scale, frame);
            Spark(grab, t, point, alpha, scale);
        }

        // ---- step 17 F9: thumb demos ----

        /// <summary>A thumb microgesture demo over the island (now shown beside the front rim, bigger, with a thumb-tip trail).</summary>
        bool ThumbDemo => _request.place == GhostPlace.AboveIsland && _request.clip != null && _request.clip.StartsWith("thumb_", System.StringComparison.Ordinal);

        Transform[] _thumbDots;
        MeshRenderer[] _thumbRenderers;
        MaterialPropertyBlock _thumbBlock;
        Vector3[] _thumbTrail;
        int _thumbHead, _thumbCount;

        void MakeThumbDots()
        {
            int n = Mathf.Max(1, thumbTrailDots + 1);
            _thumbDots = new Transform[n];
            _thumbRenderers = new MeshRenderer[n];
            _thumbTrail = new Vector3[n];
            _thumbBlock = new MaterialPropertyBlock();
            for (int i = 0; i < n; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = i == 0 ? "Ghost Thumb Dot" : $"Ghost Thumb Trail {i}";
                Destroy(go.GetComponent<Collider>());
                go.transform.SetParent(transform, false);
                _thumbDots[i] = go.transform;
                _thumbRenderers[i] = go.GetComponent<MeshRenderer>();
                _thumbRenderers[i].sharedMaterial = material;
                _thumbRenderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _thumbRenderers[i].enabled = false;
            }
        }

        static readonly int FillId = Shader.PropertyToID("_Fill"), RimId = Shader.PropertyToID("_Rim"),
            FillAlphaId = Shader.PropertyToID("_FillAlpha"), GlowId = Shader.PropertyToID("_Glow");
        static readonly Color ThumbGold = new Color(1f, 0.8f, 0.3f);

        /// <summary>A glowing gold dot on the ghost thumb's tip, and a short fading trail of where it was (the slide).</summary>
        void UpdateThumbDots(Ghost g, float alpha, float scale)
        {
            if (_thumbDots == null) return;
            Vector3? at = g != null ? KeyDotPoint(g) : null;
            bool on = at.HasValue && alpha > 0.002f;
            if (!on)
            {
                foreach (var r in _thumbRenderers) r.enabled = false;
                _thumbCount = 0;
                return;
            }
            int n = _thumbTrail.Length;
            _thumbHead = (_thumbHead + 1) % n;
            _thumbTrail[_thumbHead] = at.Value;
            _thumbCount = Mathf.Min(_thumbCount + 1, n);
            float size = thumbDotSize * Mathf.Max(0.5f, scale);
            for (int i = 0; i < n; i++)
            {
                var r = _thumbRenderers[i];
                // Trail dots are only drawn while the tip moves (a still thumb stacks them on the dot).
                Vector3 p = _thumbTrail[(_thumbHead - i + n) % n];
                bool show = i < _thumbCount && (i == 0 || (p - _thumbTrail[_thumbHead]).sqrMagnitude > Sq(size * 0.4f * i));
                r.enabled = show;
                if (!show) continue;
                float k = 1f - i / (float)n;
                _thumbDots[i].position = p;
                // Step 22: a tap pulses the dot big (swipes get the arrow).
                float pulse = _tapPulse ? 1.8f + 0.6f * Mathf.Sin(Time.time * 14f) : 1f + 0.2f * Mathf.Sin(Time.time * 10f);
                _thumbDots[i].localScale = Vector3.one * size * (i == 0 ? pulse : 0.4f + 0.5f * k);
                _thumbBlock.SetColor(FillId, ThumbGold);
                _thumbBlock.SetColor(RimId, new Color(1f, 0.93f, 0.6f));
                _thumbBlock.SetFloat(FillAlphaId, 0.85f);
                _thumbBlock.SetFloat(GlowId, 1.6f);
                _thumbBlock.SetFloat(AlphaId, alpha * (i == 0 ? 1f : k * 0.8f));
                r.SetPropertyBlock(_thumbBlock);
            }
        }

        static float Sq(float x) => x * x;

        /// <summary>Nov 7: where the gold dot sits on a ghost: the thumb tip (thumb demos), the pinch point (pinch and grab
        /// clips), the index tip (pokes, flicks) or the palm (palm clips).</summary>
        Vector3? KeyDotPoint(Ghost g)
        {
            Transform J(HandJointId id) => g.joints[(int)id];
            if (_clip == null) return null;
            string n = _clip.clipName ?? "";
            if (n.StartsWith("thumb_", System.StringComparison.Ordinal)) return J(HandJointId.HandThumbTip) != null ? J(HandJointId.HandThumbTip).position : (Vector3?)null;
            if (_clip.keyPinch || n.StartsWith("pinch", System.StringComparison.Ordinal) || n.StartsWith("grab", System.StringComparison.Ordinal) && n != "grab_palm_up")
            {
                var t = J(HandJointId.HandThumbTip);
                var i = J(HandJointId.HandIndexTip);
                return t != null && i != null ? (t.position + i.position) * 0.5f : (Vector3?)null;
            }
            var k = J(_clip.keyJoint);
            if (k == null) k = J(HandJointId.HandPalm);
            return k != null ? k.position : (Vector3?)null;
        }

        /// <summary>Step 16: the ghost's alpha share for a hand-near amount (0 = no hand near, 1 = a hand at the target).</summary>
        public static float NearAlpha(float near, float nearAlpha) => Mathf.Lerp(1f, nearAlpha, Mathf.Clamp01(near));

        /// <summary>Step 16: either tracked hand's index tip or palm is within <paramref name="radius"/> of a point.</summary>
        public static bool HandNear(Vector3 point, float radius)
        {
            float r2 = radius * radius;
            foreach (var side in new[] { Handedness.Left, Handedness.Right })
            {
                var hand = HandRegistry.Get(side);
                if (hand == null || !hand.IsTrackedDataValid) continue;
                if (HandLookup.TryGetJoint(hand, HandJointId.HandIndexTip, out var tip) && (tip - point).sqrMagnitude < r2) return true;
                if (HandLookup.TryGetJoint(hand, HandJointId.HandPalm, out var palm) && (palm - point).sqrMagnitude < r2) return true;
            }
            return false;
        }

        /// <summary>Step 16: horizontal direction across the line of sight to a point, toward the dominant hand's side.</summary>
        public static Vector3 AsideDir(Vector3 point, bool left)
        {
            var cam = Camera.main;
            Vector3 view = cam != null ? point - cam.transform.position : Vector3.forward;
            view.y = 0f;
            if (view.sqrMagnitude < 1e-6f) view = Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, view.normalized);
            return left ? -right : right;
        }

        /// <summary>Step 11d: a pinch ghost's thumb and index close: a tiny gold spark at the pinch (where it grabs).</summary>
        void Spark(Grab grab, float t, Vector3 point, float alpha, float scale)
        {
            float at = grab.has ? grab.grab : _clip.keyTime * _clip.Duration;
            bool crossed = _lastT >= 0f && _lastT < at && t >= at;
            _lastT = t;
            bool pinchClip = _clip.keyPinch || _clip.clipName == "pinch_pull" || _clip.clipName == "pinch";
            if (!crossed || !pinchClip || fx == null || alpha < 0.3f || debugFreeze >= 0f) return;
            Vector3 p = point;
            if (grab.has && !string.IsNullOrEmpty(_request.source) && Dialogue.TryGetTarget(_request.source, out var st) && st.point != null)
            {
                var sp = st.point();
                if (sp.HasValue) p = sp.Value;
            }
            fx.Burst(p, 6, new Color(1f, 0.82f, 0.3f), 0.012f * scale, 0.35f, 0.0025f * scale, Vector3.up * 0.015f);
        }

        /// <summary>
        /// Where the demonstrated spot is and the view frame (x right, z away from the player). The island's front turns
        /// to face the player, so its back is "away"; without an island, the yaw from the head.
        /// </summary>
        bool TryPlace(out Vector3 point, out Quaternion frame)
        {
            point = default;
            frame = Quaternion.identity;
            var cam = Camera.main;
            Vector3 head = cam != null ? cam.transform.position : island.transform.position + island.transform.forward * 0.5f + Vector3.up * 0.4f;
            switch (_request.place)
            {
                case GhostPlace.AboveIsland:
                {
                    if (island == null) return false;
                    if (ThumbDemo && thumbOverCentre)
                    {
                        // Over the island's centre, raised clear of the map, facing the head (upright, never lying on it).
                        var it3 = island.transform;
                        point = it3.TransformPoint(new Vector3(0f, island.CapTopLocal, 0f)) + Vector3.up * (thumbCentreUp * it3.lossyScale.x);
                        var look3 = point - head;
                        look3.y = 0f;
                        frame = look3.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(look3, Vector3.up) : Quaternion.identity;
                        return true;
                    }
                    if (ThumbDemo && thumbBeside)
                    {
                        // Step 21: beside the island on the dominant side (island +X is the player's left), raised, facing the head.
                        var it2 = island.transform;
                        var half = island.FootprintHalfSize;
                        float sx = _request.left ? 1f : -1f;
                        point = it2.TransformPoint(new Vector3(sx * (half.x + thumbBesideSpot.x), island.CapTopLocal + thumbBesideSpot.y, thumbBesideSpot.z));
                        var look2 = point - head;
                        look2.y = 0f;
                        frame = look2.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(look2, Vector3.up) : Quaternion.identity;
                        return true;
                    }
                    if (ThumbDemo)
                    {
                        // Nov 7 (Mugesh: beside the rim it went dull, the real hand rests there and fades it): over the
                        // island's centre, big, with the gold thumb dot.
                        var it = island.transform;
                        point = it.TransformPoint(new Vector3(0f, island.CapTopLocal, 0f)) + Vector3.up * (thumbCenterHeight * it.lossyScale.x);
                        break;
                    }
                    var b = island.LocalBounds;
                    var local = new Vector3(b.center.x, island.CapTopLocal, b.center.z + b.extents.z * aboveIslandFront);
                    point = island.transform.TransformPoint(local) + Vector3.up * aboveIslandHeight;
                    break;
                }
                case GhostPlace.RimCorners:
                {
                    if (island == null) return false;
                    var b = island.LocalBounds;
                    point = island.transform.TransformPoint(new Vector3(b.center.x, island.CapTopLocal - sideDrop, island.FootprintHalfSize.y));
                    break;
                }
                case GhostPlace.IslandSides:
                {
                    if (island == null) return false;
                    var b = island.LocalBounds;
                    point = island.transform.TransformPoint(new Vector3(b.center.x, island.CapTopLocal - sideDrop, b.center.z));
                    break;
                }
                case GhostPlace.BesideHand:
                {
                    // Follows the player's own dominant hand, a little to its outside, at its height.
                    bool left = _request.left;
                    Vector3 headFwd = cam != null ? cam.transform.forward : Vector3.forward;
                    headFwd.y = 0f;
                    headFwd = headFwd.sqrMagnitude > 1e-6f ? headFwd.normalized : Vector3.forward;
                    Vector3 outward = Vector3.Cross(Vector3.up, headFwd) * (left ? -1f : 1f);
                    var hand = HandRegistry.Get(left ? Handedness.Left : Handedness.Right);
                    Vector3 at = hand != null && hand.IsTrackedDataValid && HandLookup.TryGetJoint(hand, HandJointId.HandPalm, out var palm)
                        ? palm
                        : head + outward * restingHand.x - Vector3.up * restingHand.y + headFwd * restingHand.z;
                    point = at + outward * besideHandOut;
                    var look = point - head;
                    look.y = 0f;
                    frame = look.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(look, Vector3.up) : Quaternion.LookRotation(headFwd, Vector3.up);
                    return true;
                }
                case GhostPlace.AtTarget:
                case GhostPlace.AboveTarget:
                {
                    // With a source (step 9h), the clip is pinned there and only lets go at the target.
                    string id = string.IsNullOrEmpty(_request.source) ? _request.target : _request.source.Replace("#", _loops.ToString());
                    if (!Dialogue.TryGetTarget(id, out var target) || target.point == null)
                    {
                        if (_warned.Add("target:" + id)) Debug.LogWarning($"[GhostHands] {_request.clip}: target '{id}' isn't registered (yet): the ghost waits.", this);
                        return false;
                    }
                    var p = target.point();
                    if (!p.HasValue) return false;
                    point = p.Value + (_request.place == GhostPlace.AboveTarget ? Vector3.up * aboveTargetHeight : Vector3.zero);
                    break;
                }
                default:
                    return false;
            }
            Vector3 fwd = island != null ? -island.transform.forward : point - head;
            fwd.y = 0f;
            frame = fwd.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(fwd, Vector3.up) : Quaternion.identity;
            return true;
        }

        /// <summary>The palm's spot on the base skirt, on the left or right side as the player sees it (island +X is the player's left).</summary>
        Vector3 SidePoint(bool leftSide)
        {
            var half = island.FootprintHalfSize;
            var t = island.transform;
            var local = new Vector3(leftSide ? half.x : -half.x, island.CapTopLocal - sideDrop, 0f);
            return t.TransformPoint(local) + t.right * (leftSide ? sideOut : -sideOut);
        }

        /// <summary>
        /// The pinch recording reaches away from the player into the island's near edge; at a side it is turned a
        /// quarter so it reaches into that side (left hand on the player's left: fingers toward the player's right).
        /// </summary>
        static float SideYaw(bool leftSide) => leftSide ? 90f : -90f;

        /// <summary>
        /// Poses one ghost so the clip's key joint at its key time sits on <paramref name="point"/>, the clip turned
        /// <paramref name="yaw"/> degrees about up (island sides).
        /// </summary>
        /// <summary>
        /// Poses one ghost so the clip's key joint at its key time sits on <paramref name="point"/>, the clip turned
        /// <paramref name="yaw"/> degrees about up (island sides). Step 9h: with a grab time, the pinch point at the grab
        /// sits on <paramref name="point"/> instead, and (with <paramref name="release"/>) the pinch point at the release
        /// is moved onto that spot, blended over the carry (<see cref="GhostRetarget"/>).
        /// </summary>
        void Pose(Ghost g, Vector3 point, Quaternion frame, float t, float scale, float alpha, float yaw = 0f,
            float grabTime = -1f, float releaseTime = -1f, Vector3? release = null)
        {
            bool mirror = _clip.leftHand != g.left;
            var view = Quaternion.identity;
            var offset = _clip.placeOffset;
            if (g.left) offset = GhostHandClip.Mirror(offset);
            bool retarget = grabTime >= 0f;
            float travel = Mathf.Clamp(_clip.travel, 0.05f, 1f);

            // Where the key joint is at the key time (the grab), relative to the clip frame's origin.
            _clip.Sample(retarget ? grabTime : _clip.keyTime * _clip.Duration, mirror, out var kp, out var kr, _rot, _pos);
            if (_clip.anchor == GhostAnchor.Wrist) view = measuredWrist ? MeasuredWristView(g, kr) : WristView(g.left);
            // Step 21: the thumb demo holds the pose Mugesh set (relative to the head-facing frame), mirrored for a left ghost.
            if (ThumbDemo && fixedThumbPose)
            {
                var want = Quaternion.Euler(thumbRootEuler);
                if (g.left) want = GhostHandClip.Mirror(want);
                view = want * Quaternion.Inverse(kr);
            }
            Vector3 keyInRoot = _clip.keyPinch || retarget ? PinchInRoot(g) : KeyInRoot(g, _clip.keyJoint);
            if (yaw != 0f) frame *= Quaternion.Euler(0f, yaw, 0f);
            if (ThumbDemo && !measuredWrist && thumbYaw != 0f) frame *= Quaternion.Euler(0f, thumbYaw, 0f);
            if (ThumbDemo && !measuredWrist && thumbRoll != 0f) frame *= Quaternion.Euler(0f, 0f, thumbRoll);
            Vector3 key = view * kp + view * kr * keyInRoot;
            Vector3 origin = point + frame * (offset * scale) - frame * (key * scale);

            Vector3 shift = Vector3.zero;
            if (retarget && release.HasValue)
            {
                _clip.Sample(releaseTime, mirror, out var lp, out var lr, _rot, _pos);
                lp = kp + (lp - kp) * travel;
                Vector3 letGo = origin + frame * ((view * lp + view * lr * PinchInRoot(g)) * scale);
                shift = GhostRetarget.Offset(t, grabTime, releaseTime, Vector3.zero, release.Value - letGo);
            }

            _clip.Sample(t, mirror, out var rp, out var rr, _rot, _pos);
            rp = kp + (rp - kp) * travel;
            for (int i = 0; i < JointCount; i++)
                if (g.joints[i] != null) g.joints[i].SetLocalPositionAndRotation(_pos[i], _rot[i]);
            float parentScale = g.root.parent != null ? g.root.parent.lossyScale.x : 1f;
            g.root.localScale = Vector3.one * (scale / Mathf.Max(1e-5f, parentScale));
            g.root.SetPositionAndRotation(origin + frame * (view * rp * scale) + shift, frame * view * rr);
            g.block.SetFloat(AlphaId, alpha);
            g.renderer.SetPropertyBlock(g.block);
            g.renderer.enabled = alpha > 0.002f;
        }

        Vector3 PinchInRoot(Ghost g) => (KeyInRoot(g, HandJointId.HandThumbTip) + KeyInRoot(g, HandJointId.HandIndexTip)) * 0.5f;

        /// <summary>
        /// Step 9h: a clip's grab and release from its thumb-index gap (sampled at 30 Hz on the ghost's skeleton), found
        /// once. No grab = a poke or palm clip (its key frame goes on the target as before).
        /// </summary>
        Grab GrabOf(GhostHandClip c)
        {
            if (c == null) return default;
            if (_grabs.TryGetValue(c, out var known)) return known;
            var g = _right ?? _left;
            var result = new Grab();
            if (c.carryFrom >= 0f && c.carryTo > c.carryFrom)
                // Step 12c: a cupped carry marks its own start and end (no thumb-index gap to read).
                result = new Grab { has = true, grab = c.carryFrom * c.Duration, release = c.carryTo * c.Duration };
            else if (g != null && c.anchor == GhostAnchor.Island)
            {
                var gaps = new List<float>();
                const float step = 1f / 30f;
                for (float t = 0f; t <= c.Duration + 1e-4f; t += step)
                {
                    c.Sample(t, false, out _, out _, _rot, _pos);
                    gaps.Add(Vector3.Distance(KeyInRoot(g, HandJointId.HandThumbTip), KeyInRoot(g, HandJointId.HandIndexTip)));
                }
                GhostRetarget.Thresholds(gaps, out float close, out float open);
                if (GhostRetarget.FindGrab(gaps, close, open, out int gi, out int ri))
                    result = new Grab { has = true, grab = Mathf.Min(gi * step, c.Duration), release = Mathf.Min(ri * step, c.Duration) };
            }
            _grabs[c] = result;
            Debug.Log($"[GhostHands] {c.clipName}: {(result.has ? $"grab at {result.grab:0.00} s, release at {result.release:0.00} s (of {c.Duration:0.00})" : "no grab (one contact point)")}.");
            return result;
        }

        /// <summary>Debug shots: holds the clip at its grab (0) or release (1) frame.</summary>
        public void DebugFreezeAt(int which)
        {
            var g = GrabOf(_clip);
            if (_clip == null || !g.has) return;
            debugFreeze = (which == 0 ? g.grab : g.release) / Mathf.Max(0.01f, _clip.Duration);
        }

        /// <summary>The key joint's position in the hand root's frame (unscaled) with the joints as just sampled.</summary>
        Vector3 KeyInRoot(Ghost g, HandJointId joint)
        {
            int k = (int)joint;
            if (k < 0 || k >= JointCount || g.joints[k] == null) return Vector3.zero;
            // Walk up the joint chain applying the sampled local poses (root = identity, scale 1).
            Vector3 p = Vector3.zero;
            var t = g.joints[k];
            while (t != null && t != g.root)
            {
                int i = System.Array.IndexOf(g.joints, t);
                Vector3 lp = i >= 0 ? _pos[i] : t.localPosition;
                Quaternion lr = i >= 0 ? _rot[i] : t.localRotation;
                p = lp + lr * p;
                t = t.parent;
            }
            return p;
        }

        /// <summary>
        /// Step 21: the turn that puts this hand's measured fingers (wrist to middle knuckle) on <see cref="wristFingers"/> and
        /// its measured back of the hand on <see cref="wristBack"/> (mirrored for a left ghost), from the joints as just
        /// sampled at the key time with the root turned by <paramref name="rootRot"/>. No axis convention assumed.
        /// </summary>
        Quaternion MeasuredWristView(Ghost g, Quaternion rootRot)
        {
            Vector3 wrist = rootRot * KeyInRoot(g, HandJointId.HandWristRoot);
            Vector3 middle = rootRot * KeyInRoot(g, HandJointId.HandMiddle1);
            Vector3 index = rootRot * KeyInRoot(g, HandJointId.HandIndex1);
            Vector3 little = rootRot * KeyInRoot(g, HandJointId.HandPinky1);
            Vector3 fingers = middle - wrist;
            Vector3 across = index - little;
            if (fingers.sqrMagnitude < 1e-8f || across.sqrMagnitude < 1e-8f) return WristView(g.left);
            // Right hand: little -> index knuckle x fingers = the back of the hand; a left hand's is the other way.
            Vector3 back = Vector3.Cross(across, fingers) * (g.left ? -1f : 1f);
            var want = WristView(g.left);
            var have = Quaternion.LookRotation(fingers, back);
            LastMeasured = (fingers.normalized, back.normalized);
            return want * Quaternion.Inverse(have);
        }

        /// <summary>Step 21 (bridge): the last measured fingers / back directions in the clip frame.</summary>
        public (Vector3 fingers, Vector3 back) LastMeasured { get; private set; }

        Quaternion WristView(bool left)
        {
            var f = wristFingers;
            var d = wristBack;
            if (left)
            {
                f = GhostHandClip.Mirror(f);
                d = GhostHandClip.Mirror(d);
            }
            return Quaternion.LookRotation(f, d);
        }

        static void Hidden(Ghost g)
        {
            if (g != null && g.renderer != null) g.renderer.enabled = false;
        }

        void SetVisible(float alpha)
        {
            if (alpha <= 0f)
            {
                Hidden(_left);
                Hidden(_right);
                if (_dotRenderer != null) _dotRenderer.enabled = false;
                if (_thumbRenderers != null) foreach (var r in _thumbRenderers) r.enabled = false;
            }
        }

        // ---- gaze dot ----

        void MakeDot()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Ghost Gaze Dot";
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            _dot = go.transform;
            _dotRenderer = go.GetComponent<MeshRenderer>();
            _dotRenderer.sharedMaterial = material;
            _dotRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _dotRenderer.enabled = false;
            _dotBlock = new MaterialPropertyBlock();
        }

        void UpdateDot(Vector3 point, float u, float alpha)
        {
            if (_dotRenderer == null) return;
            bool on = _clip.gazeDotUntil > 0f && u <= _clip.gazeDotUntil + 0.05f;
            _dotRenderer.enabled = on && alpha > 0.002f;
            if (!_dotRenderer.enabled) return;
            Vector3 at = point;
            if (_request.place == GhostPlace.AboveTarget) at -= Vector3.up * aboveTargetHeight;
            _dot.position = at;
            float pulse = 1f + 0.25f * Mathf.Sin(Time.time * 10f);
            _dot.localScale = Vector3.one * dotSize * pulse;
            float fade = Mathf.Clamp01((_clip.gazeDotUntil + 0.05f - u) / 0.05f);
            _dotBlock.SetFloat(AlphaId, alpha * fade);
            _dotRenderer.SetPropertyBlock(_dotBlock);
        }

        // ---- building the ghosts ----

        /// <summary>Copies a hand visual (inactive first, so none of its behaviours wake up) and keeps only the OpenXR skinned hand.</summary>
        Ghost Build(HandVisual source, bool left)
        {
            if (source == null) return null;
            var holder = new GameObject(left ? "Ghost Hand L" : "Ghost Hand R");
            holder.SetActive(false);
            holder.transform.SetParent(transform, false);
            var clone = Instantiate(source.gameObject, holder.transform);
            clone.name = source.name + " (ghost)";

            var g = new Ghost { left = left, block = new MaterialPropertyBlock() };
            var srcRoot = source.Root != null ? source.Root : source.Joints.Count > 0 && source.Joints[0] != null ? source.Joints[0].parent : null;
            g.root = Map(source.transform, clone.transform, srcRoot);
            for (int i = 0; i < JointCount && i < source.Joints.Count; i++) g.joints[i] = Map(source.transform, clone.transform, source.Joints[i]);
            foreach (var smr in clone.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (g.renderer == null && System.Array.IndexOf(smr.bones, g.joints[(int)HandJointId.HandWristRoot]) >= 0) g.renderer = smr;
                else smr.enabled = false; // the OVR-skeleton mesh
            }
            if (!StripToMesh(clone)) Debug.LogError($"[GhostHands] {clone.name} still has hand components after stripping.", this);
            if (g.root == null || g.renderer == null)
            {
                Debug.LogError($"[GhostHands] {source.name}: no OpenXR skinned hand / root found; no {(left ? "left" : "right")} ghost.", this);
                Destroy(holder);
                return null;
            }
            for (var t = g.renderer.transform; t != null && t != holder.transform; t = t.parent) t.gameObject.SetActive(true);
            for (var t = g.root; t != null && t != holder.transform; t = t.parent) t.gameObject.SetActive(true);
            g.renderer.sharedMaterials = new[] { material };
            g.renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            g.renderer.receiveShadows = false;
            g.renderer.updateWhenOffscreen = true;
            g.renderer.enabled = false;
            holder.SetActive(true);
            return g;
        }

        /// <summary>
        /// Leaves a copied hand visual as bare meshes and transforms: every behaviour (Hand, data sources, interactors,
        /// visuals), collider and rigidbody goes, so the copy can never track, grab, be pointed at or be taken for the
        /// player's hand. Later components can depend on earlier ones (RequireComponent), so last-added first.
        /// Returns false if anything hand-like is left.
        /// </summary>
        public static bool StripToMesh(GameObject clone)
        {
            var behaviours = clone.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var mb in behaviours) mb.enabled = false;
            for (int i = behaviours.Length - 1; i >= 0; i--) DestroyImmediate(behaviours[i]);
            foreach (var col in clone.GetComponentsInChildren<Collider>(true)) DestroyImmediate(col);
            foreach (var rb in clone.GetComponentsInChildren<Rigidbody>(true)) DestroyImmediate(rb);
            return clone.GetComponentsInChildren<Oculus.Interaction.Input.IHand>(true).Length == 0 &&
                   clone.GetComponentsInChildren<MonoBehaviour>(true).Length == 0;
        }

        static Transform Map(Transform srcTop, Transform cloneTop, Transform src)
        {
            if (src == null) return null;
            if (src == srcTop) return cloneTop;
            var path = src.name;
            for (var t = src.parent; t != null && t != srcTop; t = t.parent) path = t.name + "/" + path;
            return cloneTop.Find(path);
        }

        /// <summary>Clips recorded on this headset (GhostClipRecorder JSON) count until the Editor import makes them assets.</summary>
        void LoadDeviceClips()
        {
            if (Application.isEditor || library == null) return;
            var dir = System.IO.Path.Combine(Application.persistentDataPath, GhostClipRecorder.DeviceFolder);
            if (!System.IO.Directory.Exists(dir)) return;
            foreach (var file in System.IO.Directory.GetFiles(dir, "*.json"))
            {
                try
                {
                    var clip = ScriptableObject.CreateInstance<GhostHandClip>();
                    JsonUtility.FromJsonOverwrite(System.IO.File.ReadAllText(file), clip);
                    if (string.IsNullOrEmpty(clip.clipName) || clip.frames.Count < 2) continue;
                    library.clips.RemoveAll(c => c != null && c.clipName == clip.clipName);
                    library.clips.Add(clip);
                    Debug.Log($"[GhostHands] Using {clip.clipName} recorded on this headset ({clip.frames.Count} frames).");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[GhostHands] Couldn't read {file}: {e.Message}");
                }
            }
        }

        /// <summary>Nov 7: the walk lesson's demo: the recorded forward swipe, then a swipe left and a swipe right.</summary>
        public const string SwipeAllClip = "thumb_swipe_all";

        /// <summary>
        /// Nov 7 (Mugesh: add the left-right swipe to the same animation): builds <see cref="SwipeAllClip"/> from the
        /// forward swipe (recorded, or the placeholder): its frames, then the rest pose with the thumb swept across the
        /// index finger one way and back, then the other way and back. The sweep turns the thumb's first joint about the
        /// index finger's direction, so the tip moves across the finger, not along it.
        /// </summary>
        void ComposeSwipeAll()
        {
            var fwd = ClipNamed("thumb_swipe_forward");
            var g = _right ?? _left;
            if (fwd == null || fwd.frames.Count < 2 || g == null) return;
            int from = Mathf.Clamp(Mathf.RoundToInt(fwd.StartTime * fwd.fps), 0, fwd.frames.Count - 1);
            // The resting hand: the recording's first frame (the thumb resting on the side of the index finger).
            var rest = Copy(fwd.frames[from]);
            for (int i = 0; i < JointCount; i++)
            {
                _rot[i] = rest.rot[i];
                _pos[i] = rest.pos[i];
            }
            Vector3 wrist = KeyInRoot(g, HandJointId.HandWristRoot);
            Vector3 across = KeyInRoot(g, HandJointId.HandIndex1) - KeyInRoot(g, HandJointId.HandPinky1);
            Vector3 fingers = KeyInRoot(g, HandJointId.HandMiddle1) - wrist;
            // The back of the hand (right hand: little -> index knuckle x fingers): left / right swipes cross the finger this way.
            Vector3 dorsal = Vector3.Cross(across, fingers).normalized * (g.left ? -1f : 1f);
            // Forward / back follow the knuckles (wrist to the middle knuckle), not the curled index: the index's tip curls
            // toward the palm, so its own direction ran sideways and every stroke came out a quarter turn off (Nov 8 shots).
            Vector3 along = (fingers - dorsal * Vector3.Dot(fingers, dorsal)).normalized;
            // The recorded swipe's most-back and most-forward frames (the thumb bent, so it can still move): the demo's
            // neutral is halfway, and forward / back start their solve from those real poses (a straight thumb can't be
            // pulled back by small turns: at full stretch every bend moves the tip sideways).
            int to2 = Mathf.Clamp(fwd.frames.Count - 1 - Mathf.RoundToInt(fwd.trimEnd * fwd.fps), from, fwd.frames.Count - 1);
            GhostFrame backF = rest, fwdF = rest;
            float lo = float.MaxValue, hi = float.MinValue;
            for (int fi = from; fi <= to2; fi++)
            {
                for (int i = 0; i < JointCount; i++)
                {
                    _rot[i] = fwd.frames[fi].rot[i];
                    _pos[i] = fwd.frames[fi].pos[i];
                }
                float d = Vector3.Dot(KeyInRoot(g, HandJointId.HandThumbTip), along);
                if (d < lo) { lo = d; backF = fwd.frames[fi]; }
                if (d > hi) { hi = d; fwdF = fwd.frames[fi]; }
            }
            rest = Blend(Copy(rest), backF, 1f);
            rest = Blend(rest, fwdF, 0.5f);
            for (int i = 0; i < JointCount; i++)
            {
                _rot[i] = rest.rot[i];
                _pos[i] = rest.pos[i];
            }
            Vector3 restTip = KeyInRoot(g, HandJointId.HandThumbTip);
            float neutralMiss = (hi - lo) * 0.5f; // the recording's own half travel along the finger (log)

            // Step 21 (Mugesh, Nov 8 video and stills: a slow swipe and a tap): the thumb tip really slides, left, right,
            // forward, back, looping. Each stroke's end pose is solved on the skeleton (the thumb's joints turned until its tip
            // sits the stroke's distance from rest), out quickly, a beat, back.
            var strokes = new (char kind, Vector3 dir, float dist)[]
            {
                ('L', -dorsal, swipeAcross), ('R', dorsal, swipeAcross), ('F', along, swipeAlong), ('B', -along, swipeAlong),
            };
            Build(SwipeAllClip, strokes, false);
            // Step 22 (Mugesh, Nov 8: the swipe-and-tap lesson showed the four swipes and no tap): one swipe left with its
            // arrow, then the recorded tap's thumb on the same resting hand (the dot pulses).
            Build(SwipeTapClip, new[] { strokes[0] }, true);

            void Build(string clipName, (char kind, Vector3 dir, float dist)[] list, bool withTap)
            {
            var clip = ScriptableObject.CreateInstance<GhostHandClip>();
            clip.hideFlags = HideFlags.DontSave;
            clip.name = clip.clipName = clipName;
            clip.anchor = fwd.anchor;
            clip.leftHand = fwd.leftHand;
            clip.fps = 30f;
            clip.keyJoint = HandJointId.HandThumbTip;
            clip.keyTime = 0f;
            clip.placeOffset = fwd.placeOffset;
            clip.size = fwd.size;
            clip.travel = 1f;
            clip.holdEnd = 0f;
            var segs = new List<Stroke>();
            int Frames(float s) => Mathf.Max(1, Mathf.RoundToInt(s * clip.fps));
            void Hold(GhostFrame f, int n)
            {
                for (int i = 0; i < n; i++) clip.frames.Add(Copy(f));
            }
            string solved = "";
            Hold(rest, Frames(strokePause));
            foreach (var s in list)
            {
                // Nov 15 (Mugesh recorded each direction): a recorded stroke plays its own thumb motion on the shared hand.
                var rec = RecordedStroke(s.kind);
                if (rec != null)
                {
                    segs.Add(InsertRecorded(clip, rec, rest, s.kind, s.dir, restTip));
                    solved += $" {s.kind} recorded";
                    Hold(rest, Frames(strokePause));
                    continue;
                }
                // Forward / back (Mugesh: the recorded thumb's own reach didn't show): the whole thumb slides along the
                // finger from its base, a little more than a real thumb, so it reads; left / right are solved across the
                // finger from the neutral.
                float miss = 0f;
                var end = s.kind == 'F' || s.kind == 'B' ? SlideThumb(g, rest, s.dir * s.dist)
                    : SolveThumbTip(g, rest, restTip + s.dir * s.dist, out miss);
                solved += $" {s.kind} {miss * 1000f:0.0} mm off";
                int s0 = clip.frames.Count;
                int outN = Frames(strokeOut), backN = Frames(strokeBack);
                for (int i = 1; i <= outN; i++) clip.frames.Add(Blend(rest, end, Mathf.SmoothStep(0f, 1f, i / (float)outN)));
                Hold(end, Frames(strokeHold));
                for (int i = 1; i <= backN; i++) clip.frames.Add(Blend(end, rest, Mathf.SmoothStep(0f, 1f, i / (float)backN)));
                segs.Add(new Stroke { t0 = s0 / clip.fps, t1 = (s0 + outN + Frames(strokeHold)) / clip.fps, kind = s.kind, dir = s.dir, restTip = restTip });
                Hold(rest, Frames(strokePause));
            }
            var tap = withTap ? ClipNamed("thumb_tap") : null;
            if (tap != null && tap.frames.Count > 1)
            {
                int ta = Mathf.Clamp(Mathf.RoundToInt(tap.StartTime * tap.fps), 0, tap.frames.Count - 1);
                int tb = Mathf.Clamp(tap.frames.Count - 1 - Mathf.RoundToInt(tap.trimEnd * tap.fps), ta, tap.frames.Count - 1);
                GhostFrame TapAt(int i)
                {
                    var f = Copy(rest);
                    foreach (var j in TapJoints)
                        if ((int)j < tap.frames[i].rot.Length) f.rot[(int)j] = tap.frames[i].rot[(int)j];
                    return f;
                }
                // Into the tap's first thumb pose, the tap at the recording's pace, back to rest: no jump between them.
                int s0 = clip.frames.Count;
                var first = TapAt(ta);
                int inN = Frames(0.12f), outN = Frames(0.15f);
                for (int i = 1; i <= inN; i++) clip.frames.Add(Blend(rest, first, Mathf.SmoothStep(0f, 1f, i / (float)inN)));
                float stepT = tap.fps / clip.fps;
                for (float fi = ta; fi <= tb; fi += stepT) clip.frames.Add(TapAt(Mathf.Min(tb, Mathf.RoundToInt(fi))));
                var last = clip.frames[clip.frames.Count - 1];
                for (int i = 1; i <= outN; i++) clip.frames.Add(Blend(last, rest, Mathf.SmoothStep(0f, 1f, i / (float)outN)));
                segs.Add(new Stroke { t0 = s0 / clip.fps, t1 = clip.frames.Count / clip.fps, kind = 'T', dir = Vector3.zero, restTip = restTip });
                Hold(rest, Frames(strokePause * 2f));
            }
            else if (withTap) Debug.LogError("[GhostHands] No thumb_tap clip (recorded or placeholder): the swipe-and-tap demo has no tap.", this);
            _placeholders[clip.name] = clip;
            _strokes[clip] = segs;
            Debug.Log($"[GhostHands] {clip.name}: {clip.frames.Count} frames ({clip.Duration:0.0} s), looping {string.Join(", ", segs.ConvertAll(x => x.kind.ToString()))} " +
                      $"({swipeAcross * 100f:0.0} / {swipeAlong * 100f:0.0} cm at hand size); recorded half travel {neutralMiss * 1000f:0.0} mm, solved:{solved}.");
            }
        }

        /// <summary>Nov 15: the recorded clip for a walk-demo stroke (L R F B), or null (that stroke stays solved).</summary>
        GhostHandClip RecordedStroke(char kind)
        {
            string n = kind == 'L' ? "thumb_swipe_left" : kind == 'R' ? "thumb_swipe_right" : kind == 'F' ? "thumb_swipe_forward" : kind == 'B' ? "thumb_swipe_back" : null;
            var c = n != null && library != null ? library.Find(n) : null;
            return c != null && c.frames.Count > 1 ? c : null;
        }

        /// <summary>
        /// Nov 15: appends a recorded stroke to a composed clip: the recording's thumb joints (mirrored when it was recorded
        /// with the other hand) on the shared resting hand, so every stroke shares one wrist and fingers; a short blend in
        /// from rest and back out. The arrow grows until the thumb tip is furthest from its resting spot.
        /// </summary>
        Stroke InsertRecorded(GhostHandClip clip, GhostHandClip rec, GhostFrame rest, char kind, Vector3 dir, Vector3 restTip)
        {
            var g = _right ?? _left;
            bool mirror = rec.leftHand != clip.leftHand;
            var rot = new Quaternion[JointCount];
            var pos = new Vector3[JointCount];
            GhostFrame At(float t)
            {
                rec.Sample(t, mirror, out _, out _, rot, pos);
                var f = Copy(rest);
                foreach (var j in TapJoints)
                    if ((int)j < f.rot.Length) f.rot[(int)j] = rot[(int)j];
                return f;
            }
            int Frames(float s) => Mathf.Max(1, Mathf.RoundToInt(s * clip.fps));
            int s0 = clip.frames.Count;
            var first = At(0f);
            int inN = Frames(0.12f), outN = Frames(0.15f);
            for (int i = 1; i <= inN; i++) clip.frames.Add(Blend(rest, first, Mathf.SmoothStep(0f, 1f, i / (float)inN)));
            float step = Mathf.Max(0.05f, rec.speed) / clip.fps;
            int peak = clip.frames.Count;
            float far = -1f;
            Vector3 peakTip = restTip;
            for (float t = 0f; t <= rec.Duration + 1e-4f; t += step)
            {
                var f = At(t);
                clip.frames.Add(f);
                for (int i = 0; i < JointCount; i++)
                {
                    _rot[i] = f.rot[i];
                    _pos[i] = f.pos[i];
                }
                Vector3 tip = KeyInRoot(g, HandJointId.HandThumbTip);
                float d = (tip - restTip).sqrMagnitude;
                if (d > far)
                {
                    far = d;
                    peak = clip.frames.Count - 1;
                    peakTip = tip;
                }
            }
            var last = clip.frames[clip.frames.Count - 1];
            for (int i = 1; i <= outN; i++) clip.frames.Add(Blend(last, rest, Mathf.SmoothStep(0f, 1f, i / (float)outN)));
            // The arrow keeps the stroke's clean axis (a real forward swipe drifts sideways; a measured arrow read as the left one).
            Vector3 moved = peakTip - restTip;
            Debug.Log($"[GhostHands] {clip.clipName} {kind}: recorded {rec.clipName} ({rec.Duration:0.00} s{(mirror ? ", mirrored" : "")}), tip travel {moved.magnitude * 1000f:0} mm, " +
                      $"{(moved.sqrMagnitude > 1e-6f ? Vector3.Angle(moved, dir) : 0f):0} deg off the {kind} arrow.");
            return new Stroke { t0 = s0 / clip.fps, t1 = peak / clip.fps, kind = kind, dir = dir, restTip = restTip };
        }

        /// <summary>Step 22: the far lesson's demo: a swipe left with its arrow, then a tap.</summary>
        public const string SwipeTapClip = "thumb_swipe_left_tap";

        static readonly HandJointId[] TapJoints = { HandJointId.HandThumb1, HandJointId.HandThumb2, HandJointId.HandThumb3, HandJointId.HandThumbTip };

        /// <summary>The thumb's joints, base to tip, that the stroke solver turns.</summary>
        static readonly HandJointId[] ThumbChain = { HandJointId.HandThumb1, HandJointId.HandThumb2, HandJointId.HandThumb3 };

        /// <summary>
        /// Step 21: the rest pose with the thumb turned so its tip reaches <paramref name="target"/> (hand root frame):
        /// Jacobian-transpose steps on the thumb's three joints about their local axes, from the rest pose.
        /// </summary>
        GhostFrame SolveThumbTip(Ghost g, GhostFrame rest, Vector3 target, out float miss)
        {
            var f = Copy(rest);
            for (int i = 0; i < JointCount; i++)
            {
                _rot[i] = f.rot[i];
                _pos[i] = f.pos[i];
            }
            const float eps = 0.5f;
            var grad = new float[ThumbChain.Length * 3];
            miss = 0f;
            for (int it = 0; it < 120; it++)
            {
                Vector3 tip = KeyInRoot(g, HandJointId.HandThumbTip);
                Vector3 err = target - tip;
                miss = err.magnitude;
                if (miss < 0.0004f) break;
                for (int c = 0; c < ThumbChain.Length; c++)
                {
                    int j = (int)ThumbChain[c];
                    var keep = _rot[j];
                    for (int ax = 0; ax < 3; ax++)
                    {
                        Vector3 axis = ax == 0 ? Vector3.right : ax == 1 ? Vector3.up : Vector3.forward;
                        _rot[j] = keep * Quaternion.AngleAxis(eps, axis);
                        grad[c * 3 + ax] = Vector3.Dot(err, (KeyInRoot(g, HandJointId.HandThumbTip) - tip) / eps);
                        _rot[j] = keep;
                    }
                }
                // Step size: degrees per metre of error along the gradient, capped.
                float norm = 0f;
                foreach (var v in grad) norm += v * v;
                if (norm < 1e-12f) break;
                // Degrees along the gradient that would close the error at first order, capped per step.
                float step = Mathf.Clamp(miss / Mathf.Sqrt(norm), 0f, 3f);
                for (int c = 0; c < ThumbChain.Length; c++)
                {
                    int j = (int)ThumbChain[c];
                    for (int ax = 0; ax < 3; ax++)
                    {
                        Vector3 axis = ax == 0 ? Vector3.right : ax == 1 ? Vector3.up : Vector3.forward;
                        _rot[j] = _rot[j] * Quaternion.AngleAxis(step * grad[c * 3 + ax] / Mathf.Sqrt(norm) * 1f, axis);
                    }
                }
            }
            foreach (var j in ThumbChain) f.rot[(int)j] = _rot[(int)j];
            return f;
        }

        static GhostFrame Blend(GhostFrame a, GhostFrame b, float k)
        {
            var f = Copy(a);
            for (int i = 0; i < f.rot.Length; i++) f.rot[i] = Quaternion.Slerp(a.rot[i], b.rot[i], k);
            for (int i = 0; i < f.pos.Length && i < b.pos.Length; i++) f.pos[i] = Vector3.Lerp(a.pos[i], b.pos[i], k);
            return f;
        }

        /// <summary>Step 21: the rest pose with the thumb's base moved by <paramref name="offset"/> (hand root frame), so
        /// the whole thumb slides along the finger.</summary>
        GhostFrame SlideThumb(Ghost g, GhostFrame rest, Vector3 offset)
        {
            var f = Copy(rest);
            for (int i = 0; i < JointCount; i++)
            {
                _rot[i] = rest.rot[i];
                _pos[i] = rest.pos[i];
            }
            int t1 = (int)HandJointId.HandThumb1;
            var parent = RootRotOf(g, g.joints[t1] != null ? g.joints[t1].parent : null);
            f.pos[t1] = rest.pos[t1] + Quaternion.Inverse(parent) * offset;
            return f;
        }

        /// <summary>Step 21: one stroke of the composed swipe demo (clip seconds, out and its beat): its kind (L R F B),
        /// the thumb tip's direction and its resting tip (hand root frame).</summary>
        class Stroke
        {
            public float t0, t1;
            public char kind;
            public Vector3 dir, restTip;
        }

        readonly Dictionary<GhostHandClip, List<Stroke>> _strokes = new Dictionary<GhostHandClip, List<Stroke>>();

        /// <summary>Step 21 (bridge): the right ghost's index direction, back-of-hand normal and thumb direction in the
        /// head-facing frame (x right, y up, z away from the head), from its posed joints.</summary>
        public string DebugAxes(Quaternion frame)
        {
            var g = _right;
            if (g == null) return "-";
            Vector3 P(HandJointId id) => g.joints[(int)id] != null ? g.joints[(int)id].position : Vector3.zero;
            var inv = Quaternion.Inverse(frame);
            Vector3 index = (P(HandJointId.HandIndex3) - P(HandJointId.HandIndex1)).normalized;
            Vector3 across = P(HandJointId.HandIndex1) - P(HandJointId.HandPinky1);
            Vector3 fingers = P(HandJointId.HandMiddle1) - P(HandJointId.HandWristRoot);
            Vector3 dorsal = Vector3.Cross(across, fingers).normalized;
            Vector3 thumb = (P(HandJointId.HandThumbTip) - P(HandJointId.HandThumb1)).normalized;
            return $"index {inv * index:F2}, back of hand {inv * dorsal:F2}, thumb {inv * thumb:F2}, fingers {inv * fingers.normalized:F2}";
        }

        /// <summary>Step 21 (bridge): the stroke playing now (L, R, F, B) or "-".</summary>
        public string StrokeNow => _arrowStroke == null ? "-" : _arrowStroke.kind.ToString();

        // ---- step 21: the stroke arrow ----

        MeshRenderer _arrow;
        MeshFilter _arrowFilter;
        Mesh _arrowMesh;
        MaterialPropertyBlock _arrowBlock;
        Stroke _arrowStroke;
        bool _tapPulse;
        float _arrowShow;
        static readonly int ArrowAlphaId = Shader.PropertyToID("_Alpha");

        /// <summary>
        /// Step 21: the arrow along a stroke, rebuilt each frame as a ribbon facing the camera: from the thumb's resting
        /// tip on the index finger, along the swipe line, <paramref name="length"/> long. A wide faint glow under a
        /// tapering core; the colour runs from clear at the tail to bright at the head (vertex colour: a coverage, r core
        /// whiteness, g glow).
        /// </summary>
        void BuildArrow(Vector3 tail, Vector3 dir, float length, float width, Vector3 toCam)
        {
            Vector3 side = Vector3.Cross(dir, toCam);
            if (side.sqrMagnitude < 1e-8f) side = Vector3.Cross(dir, Vector3.up);
            side.Normalize();
            var tr = _arrow.transform;
            tr.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            tr.localScale = Vector3.one;
            var v = new List<Vector3>();
            var c = new List<Color>();
            var t = new List<int>();
            void Quad(Vector3 a, Vector3 b, Vector3 cc, Vector3 d, Color ca, Color cb, Color ccc, Color cd)
            {
                int i = v.Count;
                v.Add(a); v.Add(b); v.Add(cc); v.Add(d);
                c.Add(ca); c.Add(cb); c.Add(ccc); c.Add(cd);
                t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            }
            float shaft = length * 0.72f;
            Vector3 headBase = tail + dir * shaft, tip = tail + dir * length;
            // Glow: a wide soft band along the shaft and around the head.
            float gw = width * 0.55f;
            Color g0 = new Color(0f, 1f, 0f, 0f), g1 = new Color(0f, 1f, 0f, 0.22f);
            Quad(tail - side * gw * 0.2f, headBase - side * gw, headBase + side * gw, tail + side * gw * 0.2f, g0, g1, g1, g0);
            int gh = v.Count;
            v.Add(headBase - side * width * 0.75f); v.Add(tip + dir * width * 0.35f); v.Add(headBase + side * width * 0.75f);
            c.Add(new Color(0f, 1f, 0f, 0.18f)); c.Add(new Color(0f, 1f, 0f, 0f)); c.Add(new Color(0f, 1f, 0f, 0.18f));
            t.AddRange(new[] { gh, gh + 1, gh + 2 });
            // Core: a shaft that tapers in from a thin tail and brightens to the head.
            float cw = width * 0.22f;
            Color c0 = new Color(0f, 0.3f, 0f, 0f), c1 = new Color(0.55f, 0.6f, 0f, 1f);
            Quad(tail - side * cw * 0.25f, headBase - side * cw, headBase + side * cw, tail + side * cw * 0.25f, c0, c1, c1, c0);
            // Head: a bright triangle, white at its point.
            int h = v.Count;
            v.Add(headBase - side * width * 0.5f); v.Add(tip); v.Add(headBase + side * width * 0.5f);
            c.Add(new Color(0.45f, 0.6f, 0f, 1f)); c.Add(new Color(1f, 0.8f, 0f, 1f)); c.Add(new Color(0.45f, 0.6f, 0f, 1f));
            t.AddRange(new[] { h, h + 1, h + 2 });
            _arrowMesh.Clear();
            _arrowMesh.SetVertices(v);
            _arrowMesh.SetColors(c);
            _arrowMesh.SetTriangles(t, 0);
            _arrowMesh.RecalculateBounds();
        }

        readonly Dictionary<(Stroke, bool), (Vector3 tail, Vector3 dir)> _arrowLocal = new Dictionary<(Stroke, bool), (Vector3, Vector3)>();

        /// <summary>
        /// Nov 15: a stroke's resting thumb tip and arrow axis in this ghost's root frame: the clip sampled just before the
        /// stroke (mirrored for the other hand, as it plays), the hand's axes measured on this skeleton (the back of the hand
        /// flipped for a left hand), the stroke kind mapped as when it was built (L toward the palm side, R the back, F / B
        /// along the fingers). On the right ghost this matches the build exactly.
        /// </summary>
        (Vector3 tail, Vector3 dir) ArrowLocal(Ghost g, Stroke st)
        {
            if (_arrowLocal.TryGetValue((st, g.left), out var known)) return known;
            _clip.Sample(Mathf.Max(0f, st.t0 - 1f / Mathf.Max(1f, _clip.fps)), _clip.leftHand != g.left, out _, out _, _rot, _pos);
            Vector3 tip = KeyInRoot(g, HandJointId.HandThumbTip);
            Vector3 wrist = KeyInRoot(g, HandJointId.HandWristRoot);
            Vector3 fingers = KeyInRoot(g, HandJointId.HandMiddle1) - wrist;
            Vector3 across = KeyInRoot(g, HandJointId.HandIndex1) - KeyInRoot(g, HandJointId.HandPinky1);
            Vector3 dorsal = Vector3.Cross(across, fingers).normalized * (g.left ? -1f : 1f);
            Vector3 along = (fingers - dorsal * Vector3.Dot(fingers, dorsal)).normalized;
            Vector3 dir = st.kind == 'L' ? -dorsal : st.kind == 'R' ? dorsal : st.kind == 'F' ? along : st.kind == 'B' ? -along : st.dir;
            var result = (tip, dir);
            _arrowLocal[(st, g.left)] = result;
            Debug.Log($"[GhostHands] {_clip.clipName} {st.kind} arrow on the {(g.left ? "left" : "right")} ghost: tail {tip * 100f:F1} cm in its root" +
                      $"{(g.left ? "" : $" (built {st.restTip * 100f:F1}, {Vector3.Angle(dir, st.dir):0} deg)")}.");
            return result;
        }

        /// <summary>
        /// Step 21: while a stroke of the swipe demo plays, a glowing gold arrow runs from the thumb's resting tip on the
        /// index finger along the line of the swipe, growing with the thumb, and fades in the pause after it.
        /// </summary>
        void UpdateArrow(Ghost g, float t, float alpha, float scale, Quaternion frame)
        {
            if (!thumbArrows || g == null || _clip == null || !_strokes.TryGetValue(_clip, out var segs) || alpha <= 0.002f || arrowMaterial == null)
            {
                if (_arrow != null) _arrow.enabled = false;
                _arrowStroke = null;
                _tapPulse = false;
                return;
            }
            if (_arrow == null)
            {
                var go = new GameObject("Ghost Stroke Arrow");
                go.transform.SetParent(transform, false);
                _arrowFilter = go.AddComponent<MeshFilter>();
                _arrowMesh = new Mesh { name = "Ghost Stroke Arrow" };
                _arrowMesh.MarkDynamic();
                _arrowFilter.sharedMesh = _arrowMesh;
                _arrow = go.AddComponent<MeshRenderer>();
                _arrow.sharedMaterial = arrowMaterial;
                _arrow.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _arrowBlock = new MaterialPropertyBlock();
            }
            Stroke now = null;
            foreach (var st in segs)
                if (t >= st.t0 && t <= st.t1 + strokeBack + 0.1f) now = st;
            if (now != _arrowStroke)
            {
                _arrowStroke = now;
                _arrowShow = 0f;
            }
            _tapPulse = now != null && now.kind == 'T';
            if (now == null || g.root == null || now.kind == 'T')
            {
                _arrow.enabled = false;
                return;
            }
            // Grows over the stroke's out, stays through its beat, fades as the thumb comes back.
            float grow = Mathf.Clamp01((t - now.t0) / Mathf.Max(0.05f, strokeOut));
            float fade = 1f - Mathf.Clamp01((t - now.t1) / Mathf.Max(0.05f, strokeBack + 0.1f));
            // Nov 15 (Mugesh: on the left hand the arrow sat way below the thumb): the stroke's rest tip and axis were found on
            // the right skeleton; the left one is mirrored, so they're worked out again on the ghost that shows (cached).
            var local = ArrowLocal(g, now);
            Vector3 tail = g.root.TransformPoint(local.tail);
            Vector3 dir = g.root.TransformDirection(local.dir).normalized;
            var cam = Camera.main;
            Vector3 toCam = cam != null ? (cam.transform.position - tail).normalized : -(frame * Vector3.forward);
            float len = arrowLength * scale * Mathf.Lerp(0.25f, 1f, Mathf.SmoothStep(0f, 1f, grow));
            BuildArrow(tail, dir, len, arrowWidth * scale, toCam);
            _arrowBlock.SetFloat(ArrowAlphaId, alpha * fade);
            _arrow.SetPropertyBlock(_arrowBlock);
            _arrow.enabled = true;
        }

        GhostHandClip ClipNamed(string name)
        {
            var recorded = library != null ? library.Find(name) : null;
            if (recorded != null && recorded.frames.Count > 1) return recorded;
            return _placeholders.TryGetValue(name, out var p) ? p : null;
        }

        static GhostFrame Copy(GhostFrame f) => new GhostFrame
        {
            rootPos = f.rootPos, rootRot = f.rootRot, rot = (Quaternion[])f.rot.Clone(), pos = (Vector3[])f.pos.Clone(),
        };

        /// <summary>A joint's rotation in the hand root's frame with the joints as just sampled (_rot).</summary>
        Quaternion RootRotOf(Ghost g, Transform t)
        {
            Quaternion r = Quaternion.identity;
            while (t != null && t != g.root)
            {
                int i = System.Array.IndexOf(g.joints, t);
                r = (i >= 0 ? _rot[i] : t.localRotation) * r;
                t = t.parent;
            }
            return r;
        }

        void MakePlaceholders()
        {
            var g = _right ?? _left;
            var rot = new Quaternion[JointCount];
            var pos = new Vector3[JointCount];
            for (int i = 0; i < JointCount; i++)
            {
                var j = g.joints[i];
                rot[i] = j != null ? j.localRotation : Quaternion.identity;
                pos[i] = j != null ? j.localPosition : Vector3.zero;
            }
            for (int i = 0; i < JointCount; i++)
            {
                _rot[i] = rot[i];
                _pos[i] = pos[i];
            }
            Vector3 wrist = KeyInRoot(g, HandJointId.HandWristRoot), middle = KeyInRoot(g, HandJointId.HandMiddle1);
            Vector3 index = KeyInRoot(g, HandJointId.HandIndex1), pinky = KeyInRoot(g, HandJointId.HandPinky1);
            Debug.Log($"[GhostHands] Rest pose in the {(g.left ? "left" : "right")} root frame: wrist {wrist * 100f:F1} cm, fingers {(middle - wrist).normalized:F2}, " +
                      $"index-pinky normal {Vector3.Cross(index - wrist, pinky - wrist).normalized:F2}, wrist local rot {rot[(int)HandJointId.HandWristRoot].eulerAngles:F0}, palm local rot {rot[(int)HandJointId.HandPalm].eulerAngles:F0}.");
            foreach (var name in GhostPlaceholders.Names)
            {
                var c = GhostPlaceholders.Make(name, rot, pos);
                if (c == null) continue;
                // Built on the left skeleton only when there is no right one: it is a left-hand clip then.
                c.leftHand = g.left;
                _placeholders[name] = c;
            }
            int recorded = 0;
            if (library != null)
                foreach (var c in library.clips)
                    if (c != null && c.frames.Count > 1) recorded++;
            Debug.Log($"[GhostHands] Ready: {(_left != null ? "left" : "")}{(_left != null && _right != null ? " + " : "")}{(_right != null ? "right" : "")} ghost, " +
                      $"{_placeholders.Count} placeholder clips, {recorded} recorded.");
        }
    }
}
