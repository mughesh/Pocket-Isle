using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// The Lantern Spirit companion: hovers above the near-left corner of the island rim (player side), bobbing and
    /// facing the head. A child of the island, so it follows when the island is moved or scaled. While it speaks it
    /// floats up a little and brightens. Its bubble anchor is registered with <see cref="Dialogue"/>.
    /// When a line has a target (<see cref="Dialogue.GetTarget"/>) it flies there in about <see cref="flySeconds"/>
    /// (eased, small arc) and parks beside it (left or right as the player sees it, whichever is more open; never above
    /// or in front of it), shining its lantern on it (<see cref="FocusHighlight"/>), and drifts home
    /// <see cref="lingerSeconds"/> after the line. It never leaves the central <see cref="fovHalfAngle"/> cone of the
    /// head: a target outside it makes the Spirit stop at the edge and lean toward it.
    /// Step 14a: it parks just above the interaction point instead (the target's radius plus <see cref="aboveOffset"/>,
    /// a little toward the player), so it sits over the ghost hand or the thing to touch without covering either; its
    /// bubble sits above it.
    /// Step 27 (a player: the text "keeps moving spots"): it moves when the target changes, not per line, stays put at least
    /// <see cref="parkSeconds"/> once parked and glides over <see cref="glideSeconds"/> (<see cref="SpiritPark"/>).
    /// </summary>
    public class LanternSpirit : MonoBehaviour
    {
        public IslandBuilder island;
        [Tooltip("Baked Lantern Spirit prefab (CreatureView).")]
        public GameObject prefab;

        [Tooltip("From the near-left footprint corner, island-local metres (x inward from the left edge, z inward from the near edge).")]
        public Vector3 cornerOffset = new Vector3(0.05f, 0.075f, 0.04f);
        public float bobHeight = 0.006f;
        public float bobSpeed = 1.1f;
        [Tooltip("Extra height while speaking.")]
        public float speakLift = 0.012f;
        [Tooltip("Body brightening while speaking (0..1 towards white).")]
        public float speakBrighten = 0.18f;
        [Tooltip("Extra brightness while the lantern shines on a hint target (step 8c: replaces the beam line).")]
        public float shineBrighten = 0.35f;

        [Header("Flying to a target")]
        [Tooltip("Step 27: seconds a move glides.")]
        public float glideSeconds = 0.6f;
        [Tooltip("Step 27: it stays put at least this long once parked (s).")]
        public float parkSeconds = 5f;
        [Tooltip("Step 27: the parked spot is kept unless the wanted spot moved this far (island-local metres).")]
        public float staysWithin = 0.06f;
        [Tooltip("Arc height of a flight (island-local metres).")]
        public float flyArc = 0.02f;
        [Tooltip("Old (before step 14a): parking beside the target.")]
        [HideInInspector] public Vector3 targetOffset = new Vector3(0.065f, 0.012f, 0f);
        [Tooltip("Step 14a: parking spot above the target: y over its top (the target's radius plus this), z toward the player (metres at island scale 1).")]
        public Vector2 aboveOffset = new Vector2(0.1f, 0.015f);
        [Tooltip("Stays by the target this long after the line ends (s).")]
        public float lingerSeconds = 1.5f;
        [Tooltip("Half angle of the head cone the Spirit stays inside (degrees).")]
        public float fovHalfAngle = 35f;
        [Tooltip("Kept out of the island's solid by this much (island-scale metres): x = beside the rim, y = feet above the cap.")]
        public Vector2 keepOut = new Vector2(0.025f, 0.012f);

        [Header("In a battle (step 9h)")]
        [Tooltip("During a battle the Spirit waits beside the near rim on the non-dominant side, out of the line from the head to the pads: x outside the side edge, y above the cap, z in from the near edge (island-local metres).")]
        public Vector3 battleOffset = new Vector3(0.035f, 0.06f, 0.05f);

        /// <summary>
        /// Flinches back from a world point (step 9e: a Keeper's stomp): a quick hop away and up that eases back
        /// over <paramref name="seconds"/>.
        /// </summary>
        public void Flinch(Vector3 fromWorld, float distance = 0.035f, float seconds = 0.9f)
        {
            if (island == null) return;
            Vector3 away = transform.position - fromWorld;
            away.y = 0f;
            away = away.sqrMagnitude > 1e-6f ? away.normalized : -island.transform.forward;
            var parent = transform.parent != null ? transform.parent : island.transform;
            _flinch = parent.InverseTransformVector((away + Vector3.up * 0.6f) * (distance * island.transform.lossyScale.x));
            _flinchStart = Time.time;
            _flinchSeconds = Mathf.Max(0.1f, seconds);
            Debug.Log("[LanternSpirit] Flinches back.");
        }

        Vector3 _flinch;
        float _flinchStart = -999f, _flinchSeconds = 1f;

        /// <summary>0 = hidden (before the opening's spark arrives), 1 = shown. Scales the model.</summary>
        public float Appear { get; set; } = 1f;

        /// <summary>
        /// Step 13: the finale holds the Spirit at a world point instead (before the Heart's forehead, on the player's
        /// palm, in the Heart's socket, at the explorer's lantern); null = its own flight as before.
        /// </summary>
        [System.NonSerialized] public System.Func<Vector3?> hold;
        [Tooltip("Step 13: how quickly it follows a held point (1/s).")]
        public float holdSharpness = 7f;
        [Tooltip("Step 13: the Spark after the finale (smaller, warmer, no acorn cap): its size and glow.")]
        public float sparkScale = 0.72f, sparkGlow = 0.35f;

        /// <summary>Step 13: the Spark (after the finale): smaller, warmer, no stone cap.</summary>
        public bool Spark { get; private set; }

        public void SetSpark(bool on)
        {
            Spark = on;
            if (_model == null) return;
            var cap = _model.Find("Rig/Model/Part_AcornCap");
            if (cap == null)
                foreach (var t in _model.GetComponentsInChildren<Transform>(true))
                    if (t.name.StartsWith("Part_AcornCap")) cap = t;
            if (cap != null) cap.gameObject.SetActive(!on);
            Debug.Log($"[LanternSpirit] {(on ? "The Spark: smaller, warmer, no cap" : "The Lantern Spirit again")}.");
        }

        /// <summary>Where the Spirit rests (world), above the near-left corner of the rim.</summary>
        /// <summary>Nov 12: where the Spirit floats in the tutorial (world): over the island's centre, above the statue.</summary>
        public Vector3 CentreWorld => island != null && island.map != null ? island.transform.TransformPoint(CentreLocal()) : transform.position;

        public Vector3 HomeWorld => island != null && island.map != null ? island.transform.TransformPoint(HomeLocal()) : transform.position;

        CreatureView _view;
        Transform _model, _anchor;
        float _speak, _speakTarget;
        bool _away, _clamped, _battle;
        Vector3 _targetWorld, _flyFrom;
        object _targetKey;
        bool _forceMove;
        readonly SpiritPark _park = new SpiritPark();
        float _targetRadius;
        float _flyStart = -999f, _lingerUntil = -999f;

        void Start()
        {
            if (prefab == null)
            {
                Debug.LogError("[LanternSpirit] No prefab assigned (bake LanternSpirit: Tools > Pocket Isle > Dialogue > Set Up MR_Main).", this);
                return;
            }
            var go = Instantiate(prefab, transform, false);
            go.name = "Lantern Spirit Model";
            _model = go.transform;
            _view = go.GetComponent<CreatureView>();
            _anchor = new GameObject("Bubble Anchor").transform;
            _anchor.SetParent(transform, false);
            float top = _view != null ? _view.height * 1.25f : 0.06f;
            _anchor.localPosition = Vector3.up * top;
            Dialogue.RegisterSpeaker(DialogueSpeaker.Spirit, _anchor);
            // The lantern (its glowing glass body): where the focus beam starts.
            var lantern = new GameObject("Lantern").transform;
            lantern.SetParent(go.transform, false);
            lantern.localPosition = Vector3.up * (_view != null ? _view.centerHeight : 0.02f);
            FocusHighlight.SetLantern(lantern);
            if (island != null && island.map != null) transform.localPosition = HomeLocal();
            ObjectiveArea.HookBubble();
        }

        void OnEnable() => Dialogue.Speaking += OnSpeaking;
        void OnDisable() => Dialogue.Speaking -= OnSpeaking;

        void OnSpeaking(DialogueSpeaker? speaker) => _speakTarget = speaker == DialogueSpeaker.Spirit ? 1f : 0f;

        void Update()
        {
            if (_model == null || island == null || island.map == null) return;
            float appear = Mathf.Clamp01(Appear);
            _model.localScale = Vector3.one * (appear * (Spark ? sparkScale : 1f));
            if (_model.gameObject.activeSelf != appear > 0.001f) _model.gameObject.SetActive(appear > 0.001f);
            _speak = Mathf.MoveTowards(_speak, _speakTarget, Time.deltaTime * 3f);
            UpdateTarget();
            bool battle = GameConditions.Check("battle");
            if (battle != _battle)
            {
                _battle = battle;
                _forceMove = true; // the battle spot and back aren't a hop: they move at once
                Debug.Log(battle ? "[LanternSpirit] Battle: waiting beside the near rim (right side, left if the right is in the way)." : "[LanternSpirit] Battle over: back to the corner.");
            }
            var held = hold != null ? hold() : null;
            if (held.HasValue) Hold(held.Value);
            else Place(Time.time);
            if (_view != null) _view.SetFlash(Mathf.Max(Mathf.Max(speakBrighten * Mathf.SmoothStep(0f, 1f, _speak), shineBrighten * FocusHighlight.Shine), Spark ? sparkGlow : 0f));
        }

        /// <summary>Step 13: follows a held point (eased), bobbing a little, facing the head; flies home from there when let go.</summary>
        void Hold(Vector3 world)
        {
            float k = 1f - Mathf.Exp(-Time.deltaTime * holdSharpness);
            transform.position = Vector3.Lerp(transform.position, world, k);
            _flyFrom = transform.localPosition;
            _flyStart = Time.time;
            _away = false;
            _forceMove = true;
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 look = cam.transform.position - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.LookRotation(look, Vector3.up);
        }

        /// <summary>Active target: go (and keep the linger timer fresh). Lost: home now. None: home after the linger.</summary>
        void UpdateTarget()
        {
            var state = Dialogue.GetTarget(out var world, out var target);
            bool away;
            if (state == Dialogue.TargetState.Active)
            {
                _targetWorld = world;
                _targetKey = target;
                _targetRadius = target != null && target.radius != null ? Mathf.Max(0f, target.radius()) : 0f;
                _lingerUntil = Time.time + lingerSeconds;
                away = true;
            }
            else away = state == Dialogue.TargetState.None && _away && Time.time < _lingerUntil;
            if (away != _away)
            {
                _away = away;
                Debug.Log(away ? "[LanternSpirit] A line's target: flying when parked long enough." : "[LanternSpirit] Target gone: drifting home when parked long enough.");
            }
        }

        void Place(float t)
        {
            // Nov 12 (Mugesh: the Spirit hid behind the statue in the grab lessons; all the information at one angle): the
            // whole tutorial it floats over the island's centre, above the statue (its bubble over it, the ghost hands below);
            // in the grab lesson over the corner the ghost grabs; from the grass lesson on, its rim corner as before.
            var park = TutorialPark;
            if (park != _tutorialPark)
            {
                _tutorialPark = park;
                _forceMove = true;
                Debug.Log(park == TutorialSpot.Centre ? $"[LanternSpirit] Tutorial ({Onboarding.Lesson ?? "opening"}): over the island's centre, above the statue." :
                    park == TutorialSpot.GrabCorner ? "[LanternSpirit] Grab lesson: over the corner the ghost grabs." : "[LanternSpirit] Tutorial over: back to the rim corner.");
            }
            Vector3 wanted = park == TutorialSpot.Centre ? CentreLocal() : park == TutorialSpot.GrabCorner ? GrabCornerLocal() : _away ? TargetLocal(_targetWorld) : HomeLocal();
            object key = park == TutorialSpot.Centre ? "centre" : park == TutorialSpot.GrabCorner ? "grabCorner" : _away ? _targetKey : _battle ? "battle" : _homeLeft ? "homeLeft" : (object)"home";
            // Step 27: one goal at a time; a new target waits out the park, and the head moving doesn't count as a move.
            if (_park.Update(key, wanted, Time.time, _forceMove, parkSeconds, staysWithin, glideSeconds))
            {
                _flyFrom = transform.localPosition;
                _flyStart = Time.time;
            }
            _forceMove = false;
            Vector3 goal = _park.Goal;
            float u = Mathf.Clamp01((Time.time - _flyStart) / Mathf.Max(0.05f, glideSeconds));
            float k = Mathf.SmoothStep(0f, 1f, u);
            float lift = Mathf.Sin(t * bobSpeed * Mathf.PI * 2f) * bobHeight + speakLift * Mathf.SmoothStep(0f, 1f, _speak);
            Vector3 pos = u < 1f ? Vector3.Lerp(_flyFrom, goal, k) + Vector3.up * (flyArc * Mathf.Sin(u * Mathf.PI)) : goal;
            transform.localPosition = pos + Vector3.up * lift * (u < 1f ? k : 1f);
            float f = (Time.time - _flinchStart) / _flinchSeconds;
            if (f >= 0f && f < 1f) transform.localPosition += _flinch * (Mathf.Sin(Mathf.Min(1f, f * 5f) * Mathf.PI * 0.5f) * (1f - Mathf.SmoothStep(0.2f, 1f, f)));
            // Never inside the island: beside its rim or above the cap (step 9e).
            float s = island.transform.lossyScale.x;
            transform.position = island.PushOutWorld(transform.position, new Vector3(keepOut.x, keepOut.y, keepOut.x) * s);

            var cam = Camera.main;
            if (cam == null) return;
            // Faces the head; at the edge of the view cone it turns toward the target instead (leaning in).
            Vector3 look = _away && _clamped ? _targetWorld - transform.position : cam.transform.position - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.LookRotation(look, Vector3.up) * Quaternion.Euler(_away && _clamped ? 12f : 0f, 0f, 0f);
        }

        [Header("Step 16: out of the objective's way")]
        [Tooltip("Nov 10: however low the target is, the Spirit hovers at least this far over the island's top (island-scale m).")]
        public float minHover = 0.09f;
        [Tooltip("Parks this far beside the target's edge (island-scale m), on the player's right first, then left.")]
        public float sideGap = 0.05f;
        [Tooltip("And this high above the target's middle (island-scale m).")]
        public float sideLift = 0.02f;
        [Tooltip("The bubble's middle, from the Spirit: x further out to the side, y up (island-scale m), for the clear check.")]
        public Vector2 bubbleProbe = new Vector2(0.07f, 0.05f);
        [Tooltip("Radii (world m) of the Spirit and its bubble for the clear check.")]
        public float spiritProbeRadius = 0.025f, bubbleProbeRadius = 0.04f;

        readonly System.Collections.Generic.List<Vector4> _avoid = new System.Collections.Generic.List<Vector4>();
        int _side = -1;
        bool _homeLeft;
        float _homeCheckAt;

        /// <summary>
        /// The near rim's corner on the player's right (Mugesh, step 16), or the left one while the right is in the
        /// objective's way (the ghost or its drop spot there). In a battle: beside the near rim, outside the side edge.
        /// Island +X is the player's left.
        /// </summary>
        Vector3 HomeLocal()
        {
            if (Time.time >= _homeCheckAt)
            {
                _homeCheckAt = Time.time + 1f;
                bool wasLeft = _homeLeft;
                _homeLeft = false;
                var cam = Camera.main;
                if (cam != null)
                {
                    _avoid.Clear();
                    ObjectiveArea.Gather(_avoid);
                    if (_avoid.Count > 0 && ObjectiveArea.Blocks(cam.transform.position, island.transform.TransformPoint(HomeLocal(false)), spiritProbeRadius, _avoid) &&
                        !ObjectiveArea.Blocks(cam.transform.position, island.transform.TransformPoint(HomeLocal(true)), spiritProbeRadius, _avoid))
                        _homeLeft = true;
                }
                if (wasLeft != _homeLeft) Debug.Log($"[LanternSpirit] Home: the {(_homeLeft ? "left (the right is in the objective's way)" : "right")} corner.");
            }
            return HomeLocal(_homeLeft);
        }

        [Tooltip("Nov 12: in the tutorial the Spirit floats over the island's centre: x = this high over the cap (above the statue, near eye level), y = this far toward the player from the centre (island-local m).")]
        public Vector2 centrePark = new Vector2(0.22f, 0.03f);
        [Tooltip("Nov 12: in the grab lesson, this high over the corner the ghost grabs (island-local m).")]
        public float grabCornerLift = 0.1f;

        enum TutorialSpot { None, Centre, GrabCorner }
        TutorialSpot _tutorialPark;

        /// <summary>Nov 12: the lessons from the greeting to the look lesson keep the Spirit over the centre; "grass" frees it.</summary>
        public static bool CentreLesson(string lesson, bool opening) =>
            lesson == null ? opening : lesson == "shard" || lesson == "pinch" || lesson == "twoHands" || lesson == "starter" ||
                                       lesson == "walk" || lesson == "far" || lesson == "practice" || lesson == "look";

        TutorialSpot TutorialPark
        {
            get
            {
                if (_battle || FinaleDirector.Running) return TutorialSpot.None;
                var id = Onboarding.Lesson;
                if (id == "move") return TutorialSpot.GrabCorner;
                return CentreLesson(id, Opening.InProgress) ? TutorialSpot.Centre : TutorialSpot.None;
            }
        }

        Vector3 CentreLocal()
        {
            var b = island.LocalBounds;
            return new Vector3(b.center.x, island.map.capHeight + centrePark.x, b.center.z + centrePark.y);
        }

        /// <summary>The near corner on the dominant side, where the move ghost grabs (<see cref="Opening"/>'s rim point).</summary>
        Vector3 GrabCornerLocal()
        {
            var half = island.FootprintHalfSize;
            // Island +X is the player's left.
            float x = DominantHand.Left ? half.x - 0.05f : -(half.x - 0.05f);
            return new Vector3(x, island.CapTopLocal + grabCornerLift, half.y - 0.02f);
        }

        Vector3 HomeLocal(bool left)
        {
            var b = island.LocalBounds;
            if (_battle)
            {
                float x = left ? b.max.x + battleOffset.x : b.min.x - battleOffset.x;
                return new Vector3(x, island.map.capHeight + battleOffset.y, b.max.z - battleOffset.z);
            }
            return new Vector3(left ? b.max.x - cornerOffset.x : b.min.x + cornerOffset.x, island.map.capHeight + cornerOffset.y, b.max.z - cornerOffset.z);
        }

        /// <summary>
        /// Nov 10 (playtest: the lantern sat on the island's floor; step 16 parked it beside the target's middle, and a target
        /// on the ground is at floor height): it hovers over the active interaction again like step 14a: straight above the
        /// target (its radius plus <see cref="aboveOffset"/>), else above and to the player's right, above and to the left, or
        /// higher: the first spot where neither the Spirit nor its bubble covers the objective's area
        /// (<see cref="ObjectiveArea"/>: the ghost, the drop spot, the battle's creatures). Never under
        /// <see cref="minHover"/> over the island. Pulled back inside the view cone. Returned in the parent's space.
        /// </summary>
        /// <summary>Candidate 0 straight above, 1 and 2 above and to one side, 3 higher.</summary>
        Vector3 HoverSpot(Vector3 target, int candidate, Vector3 side, Vector3 toPlayer, float scale)
        {
            float above = (_targetRadius + aboveOffset.x * scale) * (candidate == 3 ? 1.5f : 1f);
            Vector3 spot = target + Vector3.up * above + toPlayer * (aboveOffset.y * scale);
            if (candidate == 1 || candidate == 2) spot += side * (_targetRadius + sideGap * scale);
            float floor = island.transform.TransformPoint(new Vector3(0f, island.CapTopLocal + minHover, 0f)).y;
            if (spot.y < floor) spot.y = floor;
            return spot;
        }

        Vector3 TargetLocal(Vector3 target)
        {
            var parent = transform.parent != null ? transform.parent : island.transform;
            float scale = island.transform.lossyScale.x;
            var cam = Camera.main;
            Vector3 head = cam != null ? cam.transform.position : target + island.transform.forward;
            Vector3 toPlayer = head - target;
            toPlayer.y = 0f;
            toPlayer = toPlayer.sqrMagnitude > 1e-6f ? toPlayer.normalized : island.transform.forward;
            Vector3 right = Vector3.Cross(Vector3.up, -toPlayer);

            _avoid.Clear();
            ObjectiveArea.Gather(_avoid);
            Vector3 world = default;
            int pick = -1;
            for (int pass = 0; pass < 2 && pick < 0; pass++)
                for (int i = 0; i < 4; i++)
                {
                    // Keep the side chosen last while it stays clear (no flip-flopping), then right, left, further right, further left.
                    int c = pass == 0 ? _side : i;
                    if (c < 0) break;
                    Vector3 side = c == 2 ? -right : right;
                    Vector3 spot = HoverSpot(target, c, side, toPlayer, scale);
                    Vector3 bubble = spot + side * (bubbleProbe.x * scale) + Vector3.up * (bubbleProbe.y * scale);
                    bool clear = !ObjectiveArea.Blocks(head, spot, spiritProbeRadius, _avoid) && !ObjectiveArea.Blocks(head, bubble, bubbleProbeRadius, _avoid);
                    if (clear || pass == 1 && i == 3 && pick < 0)
                    {
                        pick = clear ? c : 0;
                        world = clear ? spot : HoverSpot(target, 0, right, toPlayer, scale);
                        break;
                    }
                    if (pass == 0) break;
                }
            if (pick != _side)
            {
                Debug.Log($"[LanternSpirit] Hovers {(pick == 0 ? "above" : pick == 1 ? "above and right of" : pick == 2 ? "above and left of" : "high above")} the target.");
                _side = pick;
            }

            _clamped = false;
            if (cam != null)
            {
                Vector3 dir = world - head;
                float max = fovHalfAngle * Mathf.Deg2Rad;
                if (Vector3.Angle(cam.transform.forward, dir) > fovHalfAngle)
                {
                    world = head + Vector3.RotateTowards(cam.transform.forward, dir, max, 0f).normalized * dir.magnitude;
                    _clamped = true;
                }
            }
            return parent.InverseTransformPoint(world);
        }
    }
}
