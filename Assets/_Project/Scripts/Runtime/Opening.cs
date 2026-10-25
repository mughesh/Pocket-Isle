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
    /// (<c>intro.explorer</c>, a gold run of tiles toward the first tall grass) and the first grass holds the scripted
    /// first encounter. Nothing is saved until the starter is picked, so quitting earlier replays the opening.
    /// A returning player (a save loaded) skips all this: the island rises from the middle where it was placed in about
    /// <see cref="returnRiseSeconds"/>. <c>intro.statue</c> plays the first time the explorer gets near the statue.
    /// Editor and development builds: N skips the opening (Kilnpot).
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
        [Tooltip("A fingertip or palm this close (m) touches it.")]
        public float touchRadius = 0.08f;
        [Tooltip("The Spirit's 'Touch it?' after this long without a touch (s).")]
        public float hintSeconds = 3f;
        [Tooltip("It unfolds by itself after this long (s).")]
        public float autoTouchSeconds = 6f;

        [Header("Unfold")]
        public float growSeconds = 1.2f;
        public float riseSeconds = 1.6f;
        [Tooltip("intro.world waits this long for a grab of the rim (s).")]
        public float moveWaitSeconds = 12f;
        public float returnRiseSeconds = 2f;

        [Header("Starter pods")]
        [Tooltip("Pod size against a rim pod.")]
        public float podScale = 3.2f;
        [Tooltip("Row of floating pods, island-local metres: spacing, height above the cap, toward the player.")]
        public Vector3 podRow = new Vector3(0.11f, 0.1f, 0.26f);
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

        enum Stage { None, Shard, Unfold, Spirit, Move, Starters, Walk, Done }

        Stage _stage;
        Transform _shard;
        MeshRenderer _shardRenderer;
        MaterialPropertyBlock _block;
        Mesh _shardMesh;
        float _scale;
        bool _touched;
        readonly List<PodView> _starterPods = new List<PodView>();
        int _gazedPod = -1, _saidPod = -1, _chosen = -1;
        float _gazeSince;
        readonly Hand[] _hands = new Hand[2];
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
            // The rim glows itself (HintGlow); no gold sphere, it would outline the trees along the front.
            Dialogue.RegisterTarget("rim", RimPoint, k => { if (handle != null) handle.HintGlow = k; }, () => 0f);
            Dialogue.RegisterTarget("starterPods", () => _starterPods.Count > 1 && _starterPods[1] != null ? _starterPods[1].transform.position : (Vector3?)null,
                k => { }, () => podRow.x * 0.5f * island.transform.lossyScale.x);
            _run = StartCoroutine(save.Loaded ? Returning() : NewGame());
        }

        void OnDestroy()
        {
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
            for (float t = 0f; t < returnRiseSeconds; t += Time.deltaTime)
            {
                float u = t / returnRiseSeconds;
                island.swapSink = 1f - Mathf.SmoothStep(0f, 1f, u);
                explorer.Shrink = EaseOutBack(Mathf.Clamp01((u - 0.6f) / 0.4f));
                spirit.Appear = Mathf.Clamp01((u - 0.7f) / 0.3f);
                yield return null;
            }
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
            explorer.Locked = true;
            explorer.Shrink = 0f;
            spirit.Appear = 0f;
            island.swapRadial = true;
            island.swapSink = 1f;
            // Never exactly 0: the island's world-to-island matrix must stay invertible (window clipping uses it).
            island.transform.localScale = Vector3.one * HiddenScale;
            Debug.Log("[Opening] New game: the shard arrives.");
            yield return WaitPlaced();

            // The shard drifts in and settles where the island will be.
            _stage = Stage.Shard;
            Vector3 rest = island.transform.position + Vector3.up * 0.06f;
            MakeShard(rest + island.transform.rotation * driftFrom);
            Vector3 from = _shard.position;
            for (float t = 0f; t < driftSeconds; t += Time.deltaTime)
            {
                float u = Mathf.SmoothStep(0f, 1f, t / driftSeconds);
                _shard.position = Vector3.Lerp(from, rest, u) + Vector3.up * (0.12f * Mathf.Sin(u * Mathf.PI));
                SpinShard(0.6f);
                yield return null;
            }

            // Waits for a hand (or gives in after a while).
            float waited = 0f;
            bool hinted = false;
            while (!_touched && waited < autoTouchSeconds)
            {
                waited += Time.deltaTime;
                _shard.position = rest + Vector3.up * (0.008f * Mathf.Sin(Time.time * 1.7f));
                SpinShard(0.35f);
                SetShardGlow(0.35f + 0.25f * Mathf.Sin(Time.time * 2.2f), 0.2f);
                _touched = ShardTouched();
                if (!hinted && waited >= hintSeconds && !_touched)
                {
                    hinted = true;
                    Dialogue.Say("intro.shard");
                }
                yield return null;
            }
            Debug.Log($"[Opening] Shard {(_touched ? "touched" : "unfolds by itself")} after {waited:0.0} s.");
            Dialogue.Cancel("intro.shard");

            // Cracks open with light; the base grows out of it; the tiles rise from the middle.
            _stage = Stage.Unfold;
            Vector3 shardHome = _shard.position;
            for (float t = 0f; t < 0.5f; t += Time.deltaTime)
            {
                float u = t / 0.5f;
                _shard.position = shardHome + UnityEngine.Random.insideUnitSphere * (0.004f * u);
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

            // A spark leaves the statue and becomes the Spirit by the rim.
            _stage = Stage.Spirit;
            yield return Spark();
            Dialogue.Say("intro.arrive");
            yield return WaitLines(14f);

            // Moving the island is prompted, not forced.
            _stage = Stage.Move;
            Dialogue.Say("intro.world");
            float since = 0f;
            bool grabbed = false;
            while (since < moveWaitSeconds && !grabbed)
            {
                since += Time.deltaTime;
                grabbed = handle != null && handle.IsGrabbed;
                yield return null;
            }
            while (handle != null && handle.IsGrabbed) yield return null;
            if (handle != null) handle.HintGlow = 0f;
            Debug.Log($"[Opening] Move prompt: {(grabbed ? "the island was moved" : "no grab, moving on")}.");
            yield return WaitLines(8f);

            // Starter choice.
            _stage = Stage.Starters;
            yield return RisePods();
            Dialogue.Say("intro.starter");
            while (_chosen < 0) yield return null;
            yield return Choose(_chosen);

            // Walking.
            _stage = Stage.Walk;
            explorer.Locked = false;
            Dialogue.Say("intro.explorer");
            Finish();
            yield return GoldPath();
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
            foreach (var p in _starterPods)
                if (p != null) Destroy(p.gameObject);
            _starterPods.Clear();
            island.transform.localScale = Vector3.one * _scale;
            EndRise();
            if (handle != null) handle.HintGlow = 0f;
            if (party.members.Count == 0 && starters.Length > 0 && starters[0] != null) party.Add(new IslingInstance(starters[0], starterLevel));
            _stage = Stage.Done;
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
        }

        void SpinShard(float speed) => _shard.rotation = Quaternion.Euler(8f * Mathf.Sin(Time.time * 0.9f), Time.time * speed * 40f, 6f * Mathf.Cos(Time.time * 0.7f));

        void SetShardGlow(float crack, float emissive)
        {
            if (_shardRenderer == null) return;
            _block.Clear();
            _block.SetFloat(CrackPulseId, Mathf.Clamp01(crack));
            _block.SetFloat(EmissiveId, emissive);
            _shardRenderer.SetPropertyBlock(_block);
        }

        /// <summary>A fingertip or palm near the shard, or a pinch while looking at it.</summary>
        bool ShardTouched()
        {
            if (_shard == null) return false;
            float reach = touchRadius + shardSize * 0.4f;
            bool looking = HeadGaze.TryGetRay(out var ray) && Vector3.Cross(ray.direction, _shard.position - ray.origin).magnitude < shardSize * 0.9f &&
                           Vector3.Dot(ray.direction, _shard.position - ray.origin) > 0f;
            for (int h = 0; h < 2; h++)
            {
                var hand = HandLookup.Refresh(ref _hands[h], h == 0 ? Handedness.Left : Handedness.Right);
                if (hand == null || !hand.IsTrackedDataValid) continue;
                foreach (var joint in new[] { HandJointId.HandIndexTip, HandJointId.HandPalm, HandJointId.HandThumbTip })
                    if (HandLookup.TryGetJoint(hand, joint, out var p) && (p - _shard.position).sqrMagnitude < reach * reach) return true;
                bool pinching = hand.GetIndexFingerIsPinching();
                if (pinching && !_wasPinching[h] && looking) return true;
                _wasPinching[h] = pinching;
            }
            return false;
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

        Vector3 PodLocal(int i) =>
            new Vector3(podRow.x * (1 - i), island.CapTopLocal + podRow.y, island.LocalBounds.center.z + podRow.z);

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
                pod.transform.localScale = Vector3.zero;
                var grab = pod.GetComponent<PodGrab>();
                if (grab != null)
                {
                    int index = i;
                    grab.Released += (g, v) => OnPodReleased(index);
                }
                _starterPods.Add(pod);
            }
            for (float t = 0f; t < 1.4f; t += Time.deltaTime)
            {
                for (int i = 0; i < _starterPods.Count; i++)
                {
                    float u = Mathf.Clamp01((t - i * 0.15f) / 1.1f);
                    float k = Mathf.SmoothStep(0f, 1f, u);
                    var p = _starterPods[i].transform;
                    p.localPosition = Vector3.Lerp(statue, PodLocal(i), k) + Vector3.up * (0.06f * Mathf.Sin(u * Mathf.PI));
                    p.localScale = Vector3.one * podScale * EaseOutBack(u);
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
            for (int i = 0; i < _starterPods.Count; i++)
            {
                var pod = _starterPods[i];
                if (pod == null) continue;
                var grab = pod.GetComponent<PodGrab>();
                if (grab != null && grab.IsHeld) continue;
                bool focus = i == _gazedPod;
                var target = PodLocal(i) + Vector3.up * (0.006f * Mathf.Sin(t * 1.6f + i));
                pod.transform.localPosition = Vector3.Lerp(pod.transform.localPosition, target, 1f - Mathf.Exp(-Time.deltaTime * 8f));
                float size = podScale * (focus ? 1.45f : 1f);
                pod.transform.localScale = Vector3.one * Mathf.Lerp(pod.transform.localScale.x, size, 1f - Mathf.Exp(-Time.deltaTime * 10f));
                // The starter inside waves: the pod wiggles while looked at.
                pod.transform.localRotation = Quaternion.Euler(0f, 0f, focus ? 12f * Mathf.Sin(t * 9f) : 0f);
                pod.Hover = focus ? 1f : 0f;
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
                var hand = HandLookup.Refresh(ref _hands[h], left ? Handedness.Left : Handedness.Right);
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
            var others = new List<(Transform t, Vector3 from)>();
            for (int i = 0; i < _starterPods.Count; i++)
                if (i != index && _starterPods[i] != null) others.Add((_starterPods[i].transform, _starterPods[i].transform.localPosition));
            for (float t = 0f; t < 0.8f; t += Time.deltaTime)
            {
                float u = Mathf.SmoothStep(0f, 1f, t / 0.8f);
                if (chosen != null)
                {
                    float f = Mathf.Clamp01(t / 0.5f);
                    chosen.transform.position = Vector3.Lerp(fromChosen, pods.HomeWorld(0), Mathf.SmoothStep(0f, 1f, f)) +
                                                Vector3.up * (0.03f * island.transform.lossyScale.x * Mathf.Sin(f * Mathf.PI));
                    chosen.transform.localScale = Vector3.one * Mathf.Lerp(podScale, 1f, f);
                }
                foreach (var (tr, from) in others)
                {
                    tr.localPosition = Vector3.Lerp(from, statue, u);
                    tr.localScale = Vector3.one * podScale * (1f - u);
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

        // ---- walking ----

        /// <summary>Tiles toward the first tall grass flash one after another until the explorer sets off.</summary>
        IEnumerator GoldPath()
        {
            if (island.world == null || !island.world.TryGetLandmark("firstGrass", out var grass)) yield break;
            var path = new List<Vector2Int>();
            var start = explorer.Tile;
            if (!ExplorerPath.Find(island.world, start, grass, path, 60)) yield break;
            for (float t = 0f; t < 25f && explorer.Tile == start; )
            {
                foreach (var tile in path)
                {
                    if (explorer.Tile != start) yield break;
                    island.FlashTile(tile, 0.4f);
                    yield return new WaitForSeconds(0.12f);
                    t += 0.12f;
                }
                yield return new WaitForSeconds(0.8f);
                t += 0.8f;
            }
        }

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

        Vector3? RimPoint()
        {
            if (_stage != Stage.Move || island == null) return null;
            var b = island.LocalBounds;
            return island.transform.TransformPoint(new Vector3(b.center.x, island.CapTopLocal - 0.02f, b.max.z));
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
