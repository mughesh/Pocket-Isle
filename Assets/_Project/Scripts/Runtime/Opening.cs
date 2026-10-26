using System;
using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Diorama
{
    /// <summary>
    /// The first minutes of a new game (step 8), no menus. The real room shows nothing but a small mossy rock shard that
    /// drifts in from about 1.5 m away and settles where the island will be, turning and glowing in its cracks. Touch it
    /// (fingertip or palm near it, or look at it and pinch; it unfolds by itself after <see cref="autoTouchSeconds"/>):
    /// it cracks open with light, the rocky base grows out of it, the Still Isle rises from the middle outward and the
    /// explorer pops up and stretches. A spark leaves the statue and becomes the Lantern Spirit by the rim
    /// (<c>intro.arrive</c>), which then points at the rocky edge (<c>intro.world</c>, waits for a grab or 12 s). Three
    /// glass pods rise from the statue and float in a row above the island, a starter turning in each
    /// (<c>intro.starter</c>): looking at one makes it grow and wiggle and the Spirit names it; grab it and drop it on
    /// the rim, or look + pinch it, and it is yours (the other two sink back into the statue). Then the explorer can walk
    /// and the walk lessons start. Nothing is saved until the starter is picked, so quitting earlier replays the opening.
    /// A returning player (a save loaded) skips all this: the island rises from the middle where it was placed in about
    /// <see cref="returnRiseSeconds"/>. <c>intro.statue</c> plays the first time the explorer gets near the statue.
    /// Step 8b: the hints (touch the shard, move the island, pick a starter, then walking) are <see cref="Onboarding"/>
    /// lessons. The opening starts the chain, publishes its stages (<c>opening.shard</c>, <c>opening.move</c>,
    /// <c>opening.starters</c>) and marks what the player did (<c>shardTouched</c>, <c>islandGrabbed</c>, <c>starterPicked</c>).
    /// Editor and development builds: N skips the opening (Kilnpot) and the lessons up to <see cref="firstWalkLesson"/>.
    /// Step 9g: the <see cref="TitleScreen"/> shows after the explorer stretches and before the Spirit (a new player picks
    /// Full adventure or Quick journey; skipped when "New journey" already picked the slot), and over the returning rise
    /// (Continue / New journey / Settings; the explorer stays locked until Continue).
    /// </summary>
    [DefaultExecutionOrder(110)] // after GameSave (100) has loaded the save
    public class Opening : MonoBehaviour
    {
        public IslandBuilder island;
        public IslandPlacer placer;
        public IslandHandle handle;
        public GestureTestPiece explorer;
        public Party party;
        public PodRack pods;
        public LanternSpirit spirit;
        public BattleFx fx;
        public GameSave save;
        [Tooltip("Optional. Runs the lessons (step 8b).")]
        public Onboarding onboarding;
        [Tooltip("Optional. The logo and the slot runes (step 9g).")]
        public TitleScreen title;
        [Tooltip("N (skip) jumps the lessons to this one.")]
        public string firstWalkLesson = "walk";
        [Tooltip("Diorama/Toon material for the shard (mossy stone, gold cracks).")]
        public Material shardMaterial;

        [Header("Starters (left to right as the player sees them)")]
        public IslingSpecies[] starters = new IslingSpecies[3];
        [Min(1)] public int starterLevel = 5;

        [Header("Shard")]
        [Tooltip("Shard height (m).")]
        public float shardSize = 0.12f;
        [Tooltip("Where it drifts in from, relative to its resting spot (island space: x to the player's left, z toward the player).")]
        public Vector3 driftFrom = new Vector3(0.8f, 0.3f, -1.2f);
        public float driftSeconds = 3.5f;
        [Tooltip("A fingertip this close (m) to the shard's visible surface touches it (the palm joint, inside the hand, gets 1 cm more).")]
        public float contactSlack = 0.015f;
        [Tooltip("It unfolds by itself after this long (s).")]
        public float autoTouchSeconds = 10f;
        [Tooltip("A touch pushes the shard away from the finger at this speed (m/s); a damped spring brings it back.")]
        public float nudgeSpeed = 0.45f;
        public float nudgeSpring = 220f;
        [Range(0.05f, 1f)] public float nudgeDamping = 0.3f;

        [Header("Unfold")]
        public float growSeconds = 1.2f;
        public float riseSeconds = 1.6f;
        [Tooltip("intro.world waits this long for a grab of the rim (s).")]
        public float moveWaitSeconds = 12f;
        [Tooltip("Step 9h: the pinch lesson waits this long for two pinches, then the opening moves on (s).")]
        public float pinchWaitSeconds = 45f;
        public float returnRiseSeconds = 2f;

        [Header("Starter pods")]
        [Tooltip("Largest floating pod size against a rim pod; smaller if the arc can't fit three (and a grown one) with the gaps.")]
        public float podScale = 2.4f;
        [Tooltip("The looked-at pod grows this much; its neighbours slide apart (dock-style) so they never touch.")]
        public float podFocusGrow = 1.25f;
        [Tooltip("Floating pods (step 8e: always over the island): arc width (x the island's width), height above the cap (m at the placed island size), toward the player from the island's middle (x half the island's depth).")]
        public Vector3 podArc = new Vector3(0.7f, 0.1f, 0.25f);
        [Tooltip("The outer pods float this much nearer the player than the middle one (island-local m): a shallow arc.")]
        public float podArcBow = 0.06f;
        [Tooltip("Smallest gap between floating pods (x a pod's diameter).")]
        public float podMinGap = 0.4f;
        [Tooltip("A dropped pod picks a rim socket within this distance (world m).")]
        public float dropReach = 0.12f;

        [Header("Statue")]
        [Tooltip("intro.statue when the explorer is within this many tiles of the statue's middle.")]
        public int statueNear = 3;

        /// <summary>True while a new game's opening runs (no saving, no encounters: the party is empty).</summary>
        public static bool InProgress { get; private set; }

        /// <summary>The starter was picked and walking is free.</summary>
        public event Action Finished;

        const float HiddenScale = 0.001f;

        static readonly int CrackPulseId = Shader.PropertyToID("_CrackPulse");
        static readonly int EmissiveId = Shader.PropertyToID("_Emissive");

        enum Stage { None, Shard, Unfold, Spirit, Pinch, Move, TwoHands, Starters, Walk, Done }

        Stage _stage;
        Transform _shard;
        MeshRenderer _shardRenderer;
        MaterialPropertyBlock _block;
        Mesh _shardMesh;
        float _scale;
        bool _touched, _shardWaiting;
        // One continuous spin across drift, hover and crack; the nudge spring on top of the base position.
        float _spinAngle, _spinSpeed;
        Vector3 _nudge, _nudgeVel;
        readonly bool[] _wasTouching = new bool[2];
        readonly List<PodView> _starterPods = new List<PodView>();
        int _gazedPod = -1, _saidPod = -1, _chosen = -1;
        float _gazeSince;
        readonly bool[] _wasPinching = { true, true };
        Coroutine _run;

        void Start()
        {
            if (island == null || placer == null || explorer == null || party == null || pods == null || spirit == null || save == null)
            {
                Debug.LogError("[Opening] Missing references (Set Up MR_Main (8 hub)): no opening.", this);
                enabled = false;
                return;
            }
            _block = new MaterialPropertyBlock();
            _scale = placer.islandScale;
            Dialogue.RegisterTarget("shard", () => _shard != null ? _shard.position : (Vector3?)null, k => { }, () => shardSize * 0.8f);
            // The shard's surface facing the player: where the ghost's fingertip lands (the centre put it inside).
            Dialogue.RegisterTarget("shardFront", ShardFront, k => { }, () => shardSize * 0.8f);
            // The rim glows itself (HintGlow); no gold sphere, it would outline the trees along the front.
            Dialogue.RegisterTarget("rim", RimPoint, k => { if (handle != null) handle.HintGlow = k; }, () => 0f);
            Dialogue.RegisterTarget("starterPods", () => _starterPods.Count > 1 && _starterPods[1] != null ? _starterPods[1].transform.position : (Vector3?)null,
                k => { }, () => pods.podPrefab != null ? pods.podPrefab.radius * FloatScale * podFocusGrow * island.transform.lossyScale.x : 0.03f);
            // Step 9h, the starter ghost: grabs the pod the player looks at, else the next one each loop (left, middle,
            // right; "starterPod:<loop>"), and sets it on the rim socket the chosen pod flies to.
            Dialogue.RegisterTargetResolver("starterPod", arg => new Dialogue.Target
            {
                point = () =>
                {
                    var p = StarterPodForGhost(arg);
                    return p != null ? p.transform.position : (Vector3?)null;
                },
                pulse = k => { },
                radius = () => 0f,
            });
            Dialogue.RegisterTarget("starterSocket", () => _stage == Stage.Starters && pods != null ? pods.HomeWorld(0) : (Vector3?)null, k => { }, () => 0f);
            DialogueBubble.KeepClear += StarterPodsInView;
            GameConditions.Register("opening.shard", () => _shardWaiting);
            GameConditions.Register("opening.pinch", () => _stage == Stage.Pinch);
            GameConditions.Register("opening.move", () => _stage == Stage.Move);
            GameConditions.Register("opening.twoHands", () => _stage == Stage.TwoHands);
            GameConditions.Register("opening.starters", () => _stage == Stage.Starters && _starterPods.Count > 0);
            _run = StartCoroutine(save.Loaded ? Returning() : NewGame());
        }

        void OnDestroy()
        {
            DialogueBubble.KeepClear -= StarterPodsInView;
            InProgress = false;
            if (_shardMesh != null) Destroy(_shardMesh);
        }

        void Update()
        {
            if ((Application.isEditor || Debug.isDebugBuild) && InProgress)
            {
                var kb = Keyboard.current;
                if (kb != null && kb.nKey.wasPressedThisFrame) Skip();
            }
            if (_stage == Stage.Starters) UpdateStarters();
            CheckStatue();
        }

        // ---- returning player ----

        IEnumerator Returning()
        {
            explorer.Locked = true;
            explorer.Shrink = 0f;
            spirit.Appear = 0f;
            island.swapRadial = true;
            island.swapSink = 1f;
            yield return WaitPlaced();
            Debug.Log($"[Opening] Returning player: the island rises on {island.world.name} c{explorer.Tile.x} r{explorer.Tile.y}.");
            // Step 9g: the logo's and runes' first draw happens here, not as the title shows.
            if (title != null) yield return WarmUp();
            if (title != null) title.BeginReturning(returnRiseSeconds);
            for (float t = 0f; t < returnRiseSeconds; t += Time.deltaTime)
            {
                float u = t / returnRiseSeconds;
                island.swapSink = 1f - Mathf.SmoothStep(0f, 1f, u);
                explorer.Shrink = EaseOutBack(Mathf.Clamp01((u - 0.6f) / 0.4f));
                spirit.Appear = Mathf.Clamp01((u - 0.7f) / 0.3f);
                yield return null;
            }
            // Step 9g: the island is up; gameplay waits for Continue.
            island.swapSink = 0f;
            island.swapRadial = false;
            explorer.Shrink = 1f;
            spirit.Appear = 1f;
            if (title != null) yield return title.Returning();
            EndRise();
            _stage = Stage.Done;
        }

        void EndRise()
        {
            island.swapSink = 0f;
            island.swapRadial = false;
            explorer.Shrink = 1f;
            spirit.Appear = 1f;
            explorer.Locked = false;
        }

        IEnumerator WaitPlaced()
        {
            for (float t = 0f; !placer.HasPlaced && t < 15f; t += Time.unscaledDeltaTime) yield return null;
        }

        // ---- new game ----

        IEnumerator NewGame()
        {
            InProgress = true;
            save.Suspended = true;
            party.Restore(new List<IslingInstance>(), new List<IslingInstance>());
            if (onboarding != null) onboarding.Begin();
            explorer.Locked = true;
            explorer.Shrink = 0f;
            spirit.Appear = 0f;
            island.swapRadial = true;
            island.swapSink = 1f;
            // Never exactly 0: the island's world-to-island matrix must stay invertible (window clipping uses it).
            island.transform.localScale = Vector3.one * HiddenScale;
            Debug.Log("[Opening] New game: the shard arrives.");
            yield return WaitPlaced();

            // First use of the opening's shaders, drawn once and tiny before anything shows (the device hitched up to
            // 97 ms when the shard, flash, Spirit spark and pods first drew).
            yield return WarmUp();

            // The shard drifts in and settles where the island will be.
            _stage = Stage.Shard;
            Vector3 rest = island.transform.position + Vector3.up * 0.06f;
            MakeShard(rest + island.transform.rotation * driftFrom);
            Vector3 from = _shard.position;
            _spinSpeed = 0.6f;
            for (float t = 0f; t < driftSeconds; t += Time.deltaTime)
            {
                float u = Mathf.SmoothStep(0f, 1f, t / driftSeconds);
                MoveShard(Vector3.Lerp(from, rest, u) + Vector3.up * (0.12f * Mathf.Sin(u * Mathf.PI)), 0.6f);
                yield return null;
            }

            // Waits for a hand (or gives in after a while). The touch lesson hints after a moment.
            float waited = 0f;
            _shardWaiting = true;
            while (!_touched && waited < autoTouchSeconds)
            {
                waited += Time.deltaTime;
                // The bob fades in so the hover carries on from the drift without a jump.
                float bob = 0.008f * Mathf.Clamp01(waited / 1.2f);
                MoveShard(rest + Vector3.up * (bob * Mathf.Sin(Time.time * 1.7f)), 0.35f);
                SetShardGlow(0.35f + 0.25f * Mathf.Sin(Time.time * 2.2f), 0.2f);
                _touched = ShardTouched();
                yield return null;
            }
            _shardWaiting = false;
            if (_touched) Onboarding.Mark("shardTouched");
            Debug.Log($"[Opening] Shard {(_touched ? "touched" : "unfolds by itself")} after {waited:0.0} s.");

            // Cracks open with light (the nudge still springs back); the base grows out of it; the tiles rise from the middle.
            _stage = Stage.Unfold;
            Vector3 shardHome = _shard.position - _nudge;
            for (float t = 0f; t < 0.5f; t += Time.deltaTime)
            {
                float u = t / 0.5f;
                MoveShard(shardHome + UnityEngine.Random.insideUnitSphere * (0.004f * u), 0.35f + u);
                SetShardGlow(u, 0.2f + 0.8f * u);
                yield return null;
            }
            Burst(_shard.position, 46, new Color(1f, 0.85f, 0.45f), 0.25f);
            StartCoroutine(Flash(_shard.position, shardSize * 1.6f, 0.6f));
            for (float t = 0f; t < growSeconds + riseSeconds * 0.6f; t += Time.deltaTime)
            {
                float g = Mathf.Clamp01(t / growSeconds);
                island.transform.localScale = Vector3.one * Mathf.Max(HiddenScale, _scale * EaseOutBack(g));
                if (_shard != null)
                {
                    MoveShard(shardHome, 1.35f);
                    float s = 1f - Mathf.Clamp01(t / 0.3f);
                    _shard.localScale = Vector3.one * shardSize * s;
                    if (s <= 0f)
                    {
                        Destroy(_shard.gameObject);
                        _shard = null;
                    }
                }
                float r = Mathf.Clamp01((t - growSeconds * 0.4f) / riseSeconds);
                island.swapSink = 1f - Mathf.SmoothStep(0f, 1f, r);
                yield return null;
            }
            for (float t = 0f; island.swapSink > 0f; t += Time.deltaTime)
            {
                island.swapSink = Mathf.Max(0f, island.swapSink - Time.deltaTime / (riseSeconds * 0.4f));
                yield return null;
            }
            island.transform.localScale = Vector3.one * _scale;
            island.swapRadial = false;

            // The explorer sits up and stretches.
            for (float t = 0f; t < 0.45f; t += Time.deltaTime)
            {
                explorer.Shrink = EaseOutBack(t / 0.45f);
                yield return null;
            }
            explorer.Shrink = 1f;
            yield return new WaitForSeconds(0.3f);
            explorer.Hop();
            yield return new WaitForSeconds(0.45f);
            explorer.Hop();
            yield return new WaitForSeconds(0.4f);

            // Step 9g: the title (the logo out of the statue, Full adventure / Quick journey), unless "New journey" picked.
            if (SaveSlots.PendingNew.HasValue)
            {
                Debug.Log($"[Opening] New journey in {SaveSlots.Label(SaveSlots.PendingNew.Value)}: no slot question.");
                SaveSlots.PendingNew = null;
            }
            else if (title != null)
                yield return title.NewPlayer(island.transform.InverseTransformPoint(LandmarkWorld("statue", 1.2f) ?? island.transform.position));

            // A spark leaves the statue and becomes the Spirit by the rim.
            _stage = Stage.Spirit;
            yield return Spark();
            Dialogue.Say("intro.arrive");
            yield return WaitLines(14f);

            // Step 9h: pinch is taught before anything needs it (the pinch lesson, beside the player's own hand).
            _stage = Stage.Pinch;
            for (float t = 0f; onboarding != null && onboarding.CurrentId == "pinch" && t < pinchWaitSeconds; t += Time.deltaTime) yield return null;
            Debug.Log($"[Opening] Pinch lesson {(onboarding != null && onboarding.CurrentId == "pinch" ? $"not done in {pinchWaitSeconds:0} s: moving on" : "done")}.");
            if (onboarding != null && onboarding.CurrentId == "pinch") onboarding.SkipTo("move");
            yield return WaitLines(4f);

            // Moving the island is prompted (the move lesson), not forced.
            _stage = Stage.Move;
            float since = 0f;
            bool grabbed = false;
            while (since < moveWaitSeconds && !grabbed)
            {
                since += Time.deltaTime;
                grabbed = handle != null && handle.IsGrabbed;
                yield return null;
            }
            if (grabbed) Onboarding.Mark("islandGrabbed");
            while (handle != null && handle.IsGrabbed) yield return null;
            if (handle != null) handle.HintGlow = 0f;
            Debug.Log($"[Opening] Move prompt: {(grabbed ? "the island was moved" : "no grab, moving on")}.");
            yield return WaitLines(8f);

            // Step 9h: two hands (turn, resize) right after one hand moved it (the lesson skips itself after 15 s; the
            // Quick Journey cuts it).
            _stage = Stage.TwoHands;
            for (float t = 0f; onboarding != null && onboarding.CurrentId == "twoHands" && t < 20f; t += Time.deltaTime) yield return null;
            while (handle != null && handle.IsGrabbed) yield return null;
            yield return WaitLines(4f);

            // Starter choice.
            _stage = Stage.Starters;
            yield return RisePods();
            while (_chosen < 0) yield return null;
            yield return Choose(_chosen);
            Onboarding.Mark("starterPicked");

            // Walking: the walk lessons take over.
            _stage = Stage.Walk;
            explorer.Locked = false;
            Finish();
        }

        void Finish()
        {
            InProgress = false;
            save.Suspended = false;
            save.Save("starter chosen");
            Debug.Log($"[Opening] Done: {party.Lead?.Name} in pod 1, walking is free.");
            Finished?.Invoke();
        }

        /// <summary>Editor / development: everything at once, Kilnpot as the starter.</summary>
        void Skip()
        {
            if (_run != null) StopCoroutine(_run);
            if (_shard != null) Destroy(_shard.gameObject);
            if (title != null) title.Cancel();
            foreach (var p in _starterPods)
                if (p != null) Destroy(p.gameObject);
            _starterPods.Clear();
            island.transform.localScale = Vector3.one * _scale;
            EndRise();
            if (handle != null) handle.HintGlow = 0f;
            if (party.members.Count == 0 && starters.Length > 0 && starters[0] != null) party.Add(new IslingInstance(starters[0], starterLevel));
            _stage = Stage.Done;
            _shardWaiting = false;
            if (onboarding != null) onboarding.SkipTo(firstWalkLesson);
            Debug.Log("[Opening] Skipped (N).");
            Finish();
        }

        // ---- shard ----

        void MakeShard(Vector3 at)
        {
            var go = new GameObject("Opening Shard");
            go.transform.SetParent(island.transform.parent, true);
            go.transform.position = at;
            go.transform.localScale = Vector3.one * shardSize;
            _shardMesh = ShardMesh();
            go.AddComponent<MeshFilter>().sharedMesh = _shardMesh;
            _shardRenderer = go.AddComponent<MeshRenderer>();
            _shardRenderer.sharedMaterial = shardMaterial;
            _shardRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (shardMaterial == null) Debug.LogError("[Opening] No shard material: the shard is pink.", this);
            _shard = go.transform;
            var e = _shardMesh.bounds.extents;
            _shardRadius = (e.x + e.y + e.z) / 3f * shardSize;
            // Step 8d: contact against the visible surface (a convex trigger, never simulated), not an average sphere.
            go.layer = 2; // Ignore Raycast
            _shardCollider = go.AddComponent<MeshCollider>();
            _shardCollider.convex = true;
            _shardCollider.isTrigger = true;
            _shardCollider.sharedMesh = _shardMesh;
            _spinAngle = 0f;
            _nudge = _nudgeVel = Vector3.zero;
        }

        float _shardRadius;
        MeshCollider _shardCollider;
        // Dev readout: the closest each hand got to the surface during the current approach (m).
        readonly float[] _approachMin = { float.MaxValue, float.MaxValue };

        Vector3? ShardFront()
        {
            if (_shard == null) return null;
            var cam = Camera.main;
            Vector3 c = _shard.position;
            Vector3 toHead = cam != null ? cam.transform.position - c : island.transform.forward;
            toHead.y = 0f;
            return c + (toHead.sqrMagnitude > 1e-6f ? toHead.normalized : Vector3.forward) * _shardRadius;
        }

        /// <summary>
        /// Places the shard at <paramref name="basePos"/> plus the nudge spring, and turns it. The spin speed eases toward
        /// <paramref name="spinSpeed"/> and the angle accumulates, so changing state never restarts the turn.
        /// </summary>
        void MoveShard(Vector3 basePos, float spinSpeed)
        {
            float dt = Time.deltaTime;
            _spinSpeed = Mathf.MoveTowards(_spinSpeed, spinSpeed, dt * 0.6f);
            _spinAngle = Mathf.Repeat(_spinAngle + _spinSpeed * 40f * dt, 360f);
            // Damped spring back to the base position (semi-implicit Euler, stable at these rates).
            float c = 2f * nudgeDamping * Mathf.Sqrt(nudgeSpring);
            _nudgeVel += (-nudgeSpring * _nudge - c * _nudgeVel) * dt;
            _nudge += _nudgeVel * dt;
            _shard.SetPositionAndRotation(basePos + _nudge,
                Quaternion.Euler(8f * Mathf.Sin(Time.time * 0.9f), _spinAngle, 6f * Mathf.Cos(Time.time * 0.7f)));
        }

        void SetShardGlow(float crack, float emissive)
        {
            if (_shardRenderer == null) return;
            _block.Clear();
            _block.SetFloat(CrackPulseId, Mathf.Clamp01(crack));
            _block.SetFloat(EmissiveId, emissive);
            _shardRenderer.SetPropertyBlock(_block);
        }

        static readonly HandJointId[] TouchJoints = { HandJointId.HandIndexTip, HandJointId.HandMiddleTip, HandJointId.HandThumbTip, HandJointId.HandPalm };

        /// <summary>
        /// A fingertip or the palm in contact with the shard's visible surface (closest point on its convex collider,
        /// within <see cref="contactSlack"/>), or a pinch while looking at it. A contact nudges the shard away from the
        /// hand (it springs back) and flashes where it was touched.
        /// </summary>
        bool ShardTouched()
        {
            if (_shard == null) return false;
            Vector3 c = _shard.position;
            bool looking = HeadGaze.TryGetRay(out var ray) && Vector3.Cross(ray.direction, c - ray.origin).magnitude < shardSize * 0.9f &&
                           Vector3.Dot(ray.direction, c - ray.origin) > 0f;
            bool readout = DevReadout.Active;
            if (readout) DevReadout.Sphere(c, _shardRadius + contactSlack, new Color(1f, 0.5f, 0.2f, 0.5f));
            for (int h = 0; h < 2; h++)
            {
                var hand = HandRegistry.Get(h == 0 ? Handedness.Left : Handedness.Right);
                if (hand == null || !hand.IsTrackedDataValid)
                {
                    _wasTouching[h] = false;
                    if (readout) DevReadout.Text($"shard{h}", $"shard {(h == 0 ? "L" : "R")}: no hand ({(hand != null ? hand.name : "none")})");
                    continue;
                }
                bool touching = false;
                float best = float.MaxValue, bestSphere = float.MaxValue;
                string bestJoint = "";
                foreach (var joint in TouchJoints)
                {
                    if (!HandLookup.TryGetJoint(hand, joint, out var p)) continue;
                    Vector3 surface = SurfacePoint(p);
                    float d = (surface - p).magnitude;
                    float reach = contactSlack + (joint == HandJointId.HandPalm ? 0.01f : 0f);
                    if (d < best)
                    {
                        best = d;
                        bestJoint = joint.ToString();
                        // The old test (step 8c): distance outside the average sphere.
                        bestSphere = (p - c).magnitude - _shardRadius;
                    }
                    if (readout && joint == HandJointId.HandIndexTip)
                        DevReadout.Line(p, surface, d <= reach ? Color.green : Color.yellow);
                    if (d > reach) continue;
                    touching = true;
                    if (_wasTouching[h]) break;
                    // Pushed from the finger into the shard (inside it: from the head's side).
                    Vector3 dir = d > 1e-4f ? surface - p : c - p;
                    if (dir.sqrMagnitude < 1e-6f && Camera.main != null) dir = Camera.main.transform.forward;
                    dir.Normalize();
                    DominantHand.FirstTouch(h == 0);
                    Contact(surface, dir, $"{(h == 0 ? "left" : "right")} {joint}, {d * 100f:0.0} cm from the surface");
                    _wasTouching[h] = true;
                    return true;
                }
                _wasTouching[h] = touching;
                LogApproach(h, best, bestSphere, bestJoint);
                if (readout)
                    DevReadout.Text($"shard{h}", $"shard {(h == 0 ? "L" : "R")}: {bestJoint} {best * 100f:0.0} cm to surface (need {contactSlack * 100f:0.0}), " +
                                                 $"old 8c sphere test {bestSphere * 100f:0.0} cm (touched below 1.0) [{hand.name}]");
                bool pinching = hand.GetIndexFingerIsPinching();
                if (pinching && !_wasPinching[h] && looking)
                {
                    var toHead = Camera.main != null ? (Camera.main.transform.position - c).normalized : Vector3.back;
                    DominantHand.FirstTouch(h == 0);
                    Contact(c + toHead * _shardRadius, -toHead * 0.4f, $"{(h == 0 ? "left" : "right")} look + pinch");
                    return true;
                }
                _wasPinching[h] = pinching;
            }
            return false;
        }

        /// <summary>Closest point on the shard's visible surface (the point itself when inside).</summary>
        Vector3 SurfacePoint(Vector3 p)
        {
            if (_shardCollider == null) return _shard.position + (p - _shard.position).normalized * _shardRadius;
            // The pose is passed in: the shard moves every frame and transforms aren't synced to physics until the next step.
            return Physics.ClosestPoint(p, _shardCollider, _shard.position, _shard.rotation);
        }

        /// <summary>Dev builds: one log line per approach (a hand comes within 6 cm, then leaves): how close it got.</summary>
        void LogApproach(int h, float distance, float sphere, string joint)
        {
            if (!Application.isEditor && !Debug.isDebugBuild) return;
            if (distance < 0.06f)
            {
                _approachMin[h] = Mathf.Min(_approachMin[h], distance);
                return;
            }
            if (_approachMin[h] == float.MaxValue) return;
            Debug.Log($"[Opening] Shard approach ({(h == 0 ? "left" : "right")}): closest {_approachMin[h] * 100f:0.0} cm to the surface " +
                      $"(contact at {contactSlack * 100f:0.0}), no touch. Last joint {joint}, old sphere test {sphere * 100f:0.0} cm.");
            _approachMin[h] = float.MaxValue;
        }

        /// <summary>Sells the touch: a push away from the hand (damped spring back), a small flash and sparks at the spot.</summary>
        void Contact(Vector3 at, Vector3 push, string how)
        {
            _nudgeVel += push * nudgeSpeed;
            StartCoroutine(Flash(at, shardSize * 0.45f, 0.3f));
            Burst(at, 10, new Color(1f, 0.9f, 0.6f), 0.08f);
            Debug.Log($"[Opening] Shard touched ({how}).");
        }

        /// <summary>Test hook (the Claude bridge): a fingertip contact from the player's side.</summary>
        public void DebugPoke()
        {
            if (!_shardWaiting || _shard == null || Camera.main == null) return;
            var dir = (_shard.position - Camera.main.transform.position).normalized;
            Contact(_shard.position - dir * _shardRadius, dir, "debug poke");
            _touched = true;
        }

        /// <summary>
        /// Draws everything the opening shows for the first time (shard, fx orb, ring and dots, a starter pod with its
        /// Isling, a gate portal) once, tiny, in front of the camera, so their shader pipelines exist before the shard
        /// appears. Vulkan builds pipelines at first draw; a ShaderVariantCollection warmup doesn't on Vulkan.
        /// </summary>
        IEnumerator WarmUp()
        {
            var cam = Camera.main;
            if (cam == null) yield break;
            float started = Time.realtimeSinceStartup;
            Vector3 at = cam.transform.position + cam.transform.forward * 0.8f;
            var tiny = Matrix4x4.TRS(at, Quaternion.identity, Vector3.one * 0.002f);
            var mesh = ShardMesh();
            var orb = fx != null ? fx.Get(BattleFx.Shape.Orb) : null;
            var ring = fx != null ? fx.Get(BattleFx.Shape.Ring) : null;
            foreach (var item in new[] { orb, ring })
            {
                if (item == null) continue;
                item.Set(Color.white, new Color(1f, 0.85f, 0.45f), 1f, 1.4f, 1f, 0.3f);
                item.transform.position = at;
                item.transform.localScale = Vector3.one * (0.002f / Mathf.Max(1e-4f, fx.transform.lossyScale.x));
            }
            if (fx != null) fx.Burst(at, 2, Color.white, 0f, 0.1f, 0.0005f);
            PodView pod = null;
            if (pods != null && pods.podPrefab != null && starters.Length > 0 && starters[0] != null)
            {
                pod = Instantiate(pods.podPrefab, island.transform.parent, false);
                pod.name = "Warmup Pod";
                pod.Show(new IslingInstance(starters[0], starterLevel));
                pod.transform.position = at;
                pod.BaseScale = Vector3.one * 0.02f;
            }
            var gates = FindFirstObjectByType<ShardGates>();
            var portal = gates != null ? gates.portalMaterial : null;
            var hud = title != null && title.runes != null ? title.runes.hudMaterial : null;
            if (title != null) title.WarmUp(at);
            for (int f = 0; f < 3; f++)
            {
                if (shardMaterial != null) Graphics.RenderMesh(new RenderParams(shardMaterial), mesh, 0, tiny);
                if (portal != null) Graphics.RenderMesh(new RenderParams(portal), mesh, 0, tiny);
                if (hud != null) Graphics.RenderMesh(new RenderParams(hud), mesh, 0, tiny);
                yield return null;
            }
            if (fx != null)
            {
                fx.Release(orb);
                fx.Release(ring);
            }
            if (pod != null) Destroy(pod.gameObject);
            if (title != null) title.EndWarmUp();
            Destroy(mesh);
            Debug.Log($"[Opening] Shader warmup: shard, fx, pod{(portal != null ? ", portal" : "")}{(title != null ? ", logo" : "")} drawn once ({(Time.realtimeSinceStartup - started) * 1000f:0} ms).");
        }

        /// <summary>A lumpy, slightly tall rock (a jittered, subdivided icosahedron), about 1 unit tall, origin in the middle.</summary>
        static Mesh ShardMesh()
        {
            float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
            var v = new List<Vector3>
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1),
            };
            var f = new List<int>
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
            };
            var mid = new Dictionary<long, int>();
            int Mid(int a, int b)
            {
                long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                if (mid.TryGetValue(key, out int i)) return i;
                v.Add((v[a] + v[b]) * 0.5f);
                mid[key] = v.Count - 1;
                return v.Count - 1;
            }
            var sub = new List<int>();
            for (int i = 0; i < f.Count; i += 3)
            {
                int a = f[i], b = f[i + 1], c = f[i + 2];
                int ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
                sub.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
            }
            var rnd = new SeededRandom(1729);
            for (int i = 0; i < v.Count; i++)
            {
                var n = v[i].normalized;
                float r = 0.42f + 0.12f * rnd.Next();
                // Taller than wide, a little narrower at the bottom.
                v[i] = new Vector3(n.x * r * (0.85f + 0.15f * n.y), n.y * r * 1.25f, n.z * r * (0.85f + 0.15f * n.y));
            }
            // Flat shading: every face gets its own vertices, so the shard reads as chipped stone.
            var verts = new Vector3[sub.Count];
            var tris = new int[sub.Count];
            for (int i = 0; i < sub.Count; i++)
            {
                verts[i] = v[sub[i]];
                tris[i] = i;
            }
            var mesh = new Mesh { name = "Opening Shard" };
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // ---- spirit ----

        IEnumerator Spark()
        {
            var start = LandmarkWorld("statue", 2.2f) ?? island.transform.position;
            var end = spirit.HomeWorld;
            var orb = fx != null ? fx.Get(BattleFx.Shape.Orb) : null;
            var gold = new Color(1f, 0.85f, 0.45f);
            if (orb != null) orb.Set(Color.white, gold, 1f, 1.4f, 1f, 0.3f);
            float scale = island.transform.lossyScale.x;
            for (float t = 0f; t < 1.1f; t += Time.deltaTime)
            {
                float u = Mathf.SmoothStep(0f, 1f, t / 1.1f);
                var p = Vector3.Lerp(start, end, u) + Vector3.up * (0.08f * scale * Mathf.Sin(u * Mathf.PI));
                if (orb != null)
                {
                    orb.transform.position = p;
                    orb.transform.localScale = Vector3.one * (0.012f * scale / Mathf.Max(1e-4f, fx.transform.lossyScale.x));
                }
                yield return null;
            }
            if (orb != null) fx.Release(orb);
            Burst(end, 18, gold, 0.06f);
            for (float t = 0f; t < 0.35f; t += Time.deltaTime)
            {
                spirit.Appear = EaseOutBack(t / 0.35f);
                yield return null;
            }
            spirit.Appear = 1f;
        }

        // ---- starter pods ----

        readonly List<float> _podX = new List<float>();

        /// <summary>
        /// The floating pods' size (x a rim pod): <see cref="podScale"/>, or smaller so that all of them, one grown by
        /// <see cref="podFocusGrow"/>, plus the gaps fit across the arc (step 8e). Island-local, so it holds at any island size.
        /// </summary>
        float FloatScale
        {
            get
            {
                float radius = pods.podPrefab != null ? pods.podPrefab.radius : 0.015f;
                int n = Mathf.Max(1, _starterPods.Count > 0 ? _starterPods.Count : starters.Length);
                float halfSpan = island.FootprintHalfSize.x * podArc.x;
                float fit = halfSpan / ((n - 1) * (1f + podMinGap) + podFocusGrow) / radius;
                return Mathf.Min(podScale, fit);
            }
        }

        /// <summary>
        /// Where floating pod <paramref name="i"/> rests (island-local): on a shallow arc across <see cref="podArc"/>.x of
        /// the island's width (outer pods nearer the player), <see cref="podArc"/>.y above the cap, always inside the
        /// island's footprint. Index 0 is the player's left.
        /// </summary>
        Vector3 PodLocal(int i)
        {
            var half2 = island.FootprintHalfSize;
            float half = Mathf.Max(1e-4f, half2.x * podArc.x);
            float x = i < _podX.Count ? _podX[i] : 0f;
            float k = Mathf.Clamp01(Mathf.Abs(x) / half);
            float r = (pods.podPrefab != null ? pods.podPrefab.radius : 0.015f) * FloatScale * podFocusGrow;
            float z = Mathf.Min(half2.y * podArc.z + podArcBow * k * k, half2.y - r);
            float above = podArc.y / Mathf.Max(0.05f, placer.islandScale);
            return new Vector3(x, island.CapTopLocal + above, z);
        }

        /// <summary>
        /// Spreads the pods evenly (their outer edges at the arc's ends), then dock-style: the looked-at pod grows and its
        /// neighbours slide outward until every gap is at least <see cref="podMinGap"/> of a pod's diameter.
        /// </summary>
        void LayoutPods(int focus)
        {
            int n = Mathf.Max(1, _starterPods.Count);
            float r0 = (pods.podPrefab != null ? pods.podPrefab.radius : 0.015f) * FloatScale;
            float span = island.FootprintHalfSize.x * podArc.x;
            float half = span - r0;
            float gap = podMinGap * 2f * r0;
            _podX.Clear();
            for (int i = 0; i < n; i++) _podX.Add(n == 1 ? 0f : Mathf.Lerp(half, -half, i / (float)(n - 1)));
            float R(int i) => r0 * (i == focus ? podFocusGrow : 1f);
            int from = focus >= 0 ? focus : n / 2;
            // x falls with the index (index 0 is the player's left, +X): each neighbour keeps its distance or moves out.
            for (int j = from + 1; j < n; j++) _podX[j] = Mathf.Min(_podX[j], _podX[j - 1] - R(j - 1) - R(j) - gap);
            for (int j = from - 1; j >= 0; j--) _podX[j] = Mathf.Max(_podX[j], _podX[j + 1] + R(j + 1) + R(j) + gap);
            // Still over the island: a row pushed past an end shifts back (FloatScale makes the whole row fit).
            float over = _podX[0] + R(0) - span, under = -span - (_podX[n - 1] - R(n - 1));
            float shift = over > 0f ? -over : under > 0f ? under : 0f;
            for (int i = 0; i < n; i++) _podX[i] += shift;
        }

        /// <summary>The floating pods, for the speech bubble to keep clear of (step 8d).</summary>
        void StarterPodsInView(List<Vector4> keep)
        {
            if (_stage != Stage.Starters) return;
            foreach (var p in _starterPods)
                if (p != null)
                {
                    var c = p.transform.position;
                    keep.Add(new Vector4(c.x, c.y, c.z, p.WorldRadius * 1.15f));
                }
        }

        IEnumerator RisePods()
        {
            if (pods.podPrefab == null)
            {
                Debug.LogError("[Opening] No pod prefab on the pod rack: no starter pods; giving the first starter.", this);
                _chosen = 0;
                yield break;
            }
            var statue = island.transform.InverseTransformPoint(LandmarkWorld("statue", 1.2f) ?? island.transform.position);
            for (int i = 0; i < starters.Length; i++)
            {
                if (starters[i] == null)
                {
                    Debug.LogError($"[Opening] Starter {i + 1} not set.", this);
                    continue;
                }
                var pod = Instantiate(pods.podPrefab, island.transform, false);
                pod.name = $"Starter Pod {starters[i].name}";
                pod.Show(new IslingInstance(starters[i], starterLevel));
                pod.transform.localPosition = statue;
                // BaseScale, not localScale: PodView rewrites localScale from it every frame (held pods shrank to rim size).
                pod.BaseScale = Vector3.zero;
                var grab = pod.GetComponent<PodGrab>();
                if (grab != null)
                {
                    int index = i;
                    grab.Released += (g, v) => OnPodReleased(index);
                }
                _starterPods.Add(pod);
            }
            LayoutPods(-1);
            for (float t = 0f; t < 1.4f; t += Time.deltaTime)
            {
                for (int i = 0; i < _starterPods.Count; i++)
                {
                    float u = Mathf.Clamp01((t - i * 0.15f) / 1.1f);
                    float k = Mathf.SmoothStep(0f, 1f, u);
                    var p = _starterPods[i].transform;
                    p.localPosition = Vector3.Lerp(statue, PodLocal(i), k) + Vector3.up * (0.06f * Mathf.Sin(u * Mathf.PI));
                    _starterPods[i].BaseScale = Vector3.one * FloatScale * EaseOutBack(u);
                }
                yield return null;
            }
        }

        void UpdateStarters()
        {
            if (_chosen >= 0) return;
            float t = Time.time;
            _gazedPod = GazedPod();
            if (pods != null) pods.SetExternalGaze(this, _gazedPod >= 0);
            if (_gazedPod >= 0 && _gazedPod != _saidPod && Time.time - _gazeSince > 0.35f)
            {
                _saidPod = _gazedPod;
                Dialogue.Say("starter." + starters[_gazedPod].name.ToLowerInvariant());
            }
            bool gazedHeld = _gazedPod >= 0 && _gazedPod < _starterPods.Count && _starterPods[_gazedPod] != null &&
                             _starterPods[_gazedPod].GetComponent<PodGrab>() is PodGrab gg && gg.IsHeld;
            LayoutPods(gazedHeld ? -1 : _gazedPod);
            for (int i = 0; i < _starterPods.Count; i++)
            {
                var pod = _starterPods[i];
                if (pod == null) continue;
                var grab = pod.GetComponent<PodGrab>();
                bool held = grab != null && grab.IsHeld;
                bool focus = i == _gazedPod && !held;
                // Floating and held pods stay big; they shrink to rim size only as they snap into the socket (Choose).
                float size = FloatScale * (focus ? podFocusGrow : 1f);
                pod.BaseScale = Vector3.one * Mathf.Lerp(pod.BaseScale.x, size, 1f - Mathf.Exp(-Time.deltaTime * 10f));
                if (held) continue;
                var target = PodLocal(i) + Vector3.up * (0.006f * Mathf.Sin(t * 1.6f + i));
                pod.transform.localPosition = Vector3.Lerp(pod.transform.localPosition, target, 1f - Mathf.Exp(-Time.deltaTime * 8f));
                // The starter inside waves: the pod wiggles while looked at.
                pod.transform.localRotation = Quaternion.Euler(0f, 0f, focus ? 12f * Mathf.Sin(t * 9f) : 0f);
                pod.Hover = focus ? 1f : 0f;
                // Step 9h: every starter pod is gold while you pick (the looked-at one white).
                pod.Actionable = 1f;
            }
            UpdatePodPinch();
        }

        int GazedPod()
        {
            if (!HeadGaze.TryGetRay(out var ray)) return -1;
            var pick = new HeadGaze.ConePick(ray, _gazedPod);
            for (int i = 0; i < _starterPods.Count; i++)
                if (_starterPods[i] != null)
                    pick.Consider(i, _starterPods[i].transform.position, Mathf.Max(HeadGaze.MinRadius * island.transform.lossyScale.x, _starterPods[i].WorldRadius * 1.6f));
            int best = pick.Best;
            if (best != _gazedPod) _gazeSince = Time.time;
            return best;
        }

        void UpdatePodPinch()
        {
            for (int h = 0; h < 2; h++)
            {
                bool left = h == 0;
                var hand = HandRegistry.Get(left ? Handedness.Left : Handedness.Right);
                bool pinching = hand != null && hand.IsTrackedDataValid && hand.GetIndexFingerIsPinching();
                bool busy = handle != null && handle.IsHandBusy(left);
                bool holding = false;
                foreach (var p in _starterPods)
                    if (p != null && p.GetComponent<PodGrab>() is PodGrab g && g.IsHeld) holding = true;
                if (pinching && !_wasPinching[h] && !busy && !holding && _gazedPod >= 0)
                {
                    Debug.Log($"[Opening] Look + pinch picks {starters[_gazedPod].name}.");
                    _chosen = _gazedPod;
                }
                _wasPinching[h] = pinching;
            }
        }

        void OnPodReleased(int index)
        {
            if (_chosen >= 0 || index >= _starterPods.Count || _starterPods[index] == null) return;
            var world = _starterPods[index].transform.position;
            int socket = pods.NearestSocket(world, dropReach);
            if (socket < 0)
            {
                Debug.Log($"[Opening] {starters[index].name}'s pod dropped away from the rim: it floats back.");
                return;
            }
            Debug.Log($"[Opening] {starters[index].name}'s pod dropped on the rim (socket {socket + 1}).");
            _chosen = index;
        }

        /// <summary>Test hook (the Claude bridge): picks a starter as if looked at and pinched.</summary>
        public void DebugChoose(int index)
        {
            if (_stage == Stage.Starters && _chosen < 0 && index >= 0 && index < _starterPods.Count) _chosen = index;
        }

        IEnumerator Choose(int index)
        {
            if (pods != null) pods.SetExternalGaze(this, false);
            Dialogue.Cancel("starter." + starters[index].name.ToLowerInvariant());
            var statue = island.transform.InverseTransformPoint(LandmarkWorld("statue", 1.2f) ?? island.transform.position);
            var chosen = index < _starterPods.Count ? _starterPods[index] : null;
            Vector3 fromChosen = chosen != null ? chosen.transform.position : Vector3.zero;
            float chosenFrom = chosen != null ? chosen.BaseScale.x : FloatScale;
            var others = new List<(PodView pod, Vector3 from)>();
            for (int i = 0; i < _starterPods.Count; i++)
                if (i != index && _starterPods[i] != null) others.Add((_starterPods[i], _starterPods[i].transform.localPosition));
            for (float t = 0f; t < 0.8f; t += Time.deltaTime)
            {
                float u = Mathf.SmoothStep(0f, 1f, t / 0.8f);
                if (chosen != null)
                {
                    float f = Mathf.Clamp01(t / 0.5f);
                    chosen.transform.position = Vector3.Lerp(fromChosen, pods.HomeWorld(0), Mathf.SmoothStep(0f, 1f, f)) +
                                                Vector3.up * (0.03f * island.transform.lossyScale.x * Mathf.Sin(f * Mathf.PI));
                    // Big until the last 0.3 s of the flight, then eases to rim size as it snaps in.
                    float shrink = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.2f) / 0.3f));
                    chosen.BaseScale = Vector3.one * Mathf.Lerp(chosenFrom, 1f, shrink);
                }
                foreach (var (pod, from) in others)
                {
                    pod.transform.localPosition = Vector3.Lerp(from, statue, u);
                    pod.BaseScale = Vector3.one * FloatScale * (1f - u);
                }
                yield return null;
            }
            foreach (var p in _starterPods)
                if (p != null) Destroy(p.gameObject);
            _starterPods.Clear();
            party.Add(new IslingInstance(starters[index], starterLevel));
            Burst(pods.HomeWorld(0), 16, new Color(1f, 0.85f, 0.45f), 0.05f);
            Debug.Log($"[Opening] Starter: {starters[index].name} Lv {starterLevel} in pod 1.");
        }

        // ---- statue ----

        void CheckStatue()
        {
            if (InProgress || explorer.Locked || island.world == null || !island.world.TryGetLandmark("statue", out var s)) return;
            var d = explorer.Tile - s;
            if (Mathf.Max(Mathf.Abs(d.x), Mathf.Abs(d.y)) <= statueNear) Dialogue.Say("intro.statue");
        }

        // ---- helpers ----

        IEnumerator WaitLines(float max)
        {
            for (float t = 0f; Dialogue.IsTalking && t < max; t += Time.deltaTime) yield return null;
        }

        /// <summary>A landmark's world point, <paramref name="tilesUp"/> tiles above its cap.</summary>
        Vector3? LandmarkWorld(string name, float tilesUp)
        {
            if (island.world == null || !island.world.TryGetLandmark(name, out var tile)) return null;
            return island.transform.TransformPoint(island.TileToLocal(tile, island.Content.capHeight + tilesUp * island.map.tileSize));
        }

        /// <summary>The move lesson's spot: the base skirt on the player's right (the ghost palm-grabs the side, step 8d).</summary>
        /// <summary>The looked-at starter pod, else pod (loop mod count): index 0 is the player's left.</summary>
        PodView StarterPodForGhost(string loop)
        {
            if (_stage != Stage.Starters || _starterPods.Count == 0) return null;
            if (_gazedPod >= 0 && _gazedPod < _starterPods.Count && _starterPods[_gazedPod] != null) return _starterPods[_gazedPod];
            int.TryParse(loop, out int n);
            return _starterPods[Mathf.Abs(n) % _starterPods.Count];
        }

        Vector3? RimPoint()
        {
            if (_stage != Stage.Move || island == null) return null;
            return island.transform.TransformPoint(new Vector3(-island.FootprintHalfSize.x, island.CapTopLocal - 0.05f, 0f));
        }

        void Burst(Vector3 at, int count, Color color, float speed)
        {
            if (fx == null) return;
            float s = island.transform.lossyScale.x;
            fx.Burst(at, count, color, speed * Mathf.Max(0.3f, s), 0.9f, 0.004f * Mathf.Max(0.3f, s), Vector3.up * 0.05f);
        }

        IEnumerator Flash(Vector3 at, float size, float seconds)
        {
            var orb = fx != null ? fx.Get(BattleFx.Shape.Orb) : null;
            if (orb == null) yield break;
            orb.Set(Color.white, new Color(1f, 0.85f, 0.45f), 1f, 1.5f, 1f, 0.3f);
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float u = t / seconds;
                orb.transform.position = at;
                orb.transform.localScale = Vector3.one * (size * (0.4f + 1.4f * Mathf.Sin(u * Mathf.PI)) / Mathf.Max(1e-4f, fx.transform.lossyScale.x));
                orb.SetAlpha(1f - u * u);
                yield return null;
            }
            fx.Release(orb);
        }

        static float EaseOutBack(float t)
        {
            t = Mathf.Clamp01(t);
            const float c = 1.7f;
            t -= 1f;
            return 1f + (c + 1f) * t * t * t + c * t * t;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => InProgress = false;
    }
}
