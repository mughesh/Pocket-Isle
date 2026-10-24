using System;
using System.Collections;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// The party pods on the island's front rim and the catch cradle. Six brass sockets sit in the rocky side just under
    /// the cap, across the right part of the front face (the Spirit has the near-left corner); pod i holds party slot i.
    /// The cradle floats above the near-right corner and holds an empty pod during battles.
    /// Outside battle: poke a pod, or look at it and pinch, for its stats card.
    /// In battle (flags set by <see cref="EncounterDirector"/>): pods that may be used can be grabbed (ISDK) and
    /// thrown (<see cref="Thrown"/>), or looked at + pinched (<see cref="Picked"/>). While a hand reaches for a pod
    /// the rim grab pauses, so the hand takes the pod instead of the island.
    /// Flights (to a pad, home, a miss, wobbles) are coroutines here; the encounter decides where pods go.
    /// </summary>
    public class PodRack : MonoBehaviour
    {
        public const int CradleSlot = Party.Size;

        public IslandBuilder island;
        public Party party;
        [Tooltip("Pod prefab (PodView + PodGrab).")]
        public PodView podPrefab;
        [Tooltip("Grab guard, and the rim grab that pauses while a hand reaches for a pod.")]
        public IslandHandle handle;
        public BattleFx fx;
        public Material hudMaterial;

        [Header("Sockets")]
        public Mesh socketMesh;
        public Material socketMaterial;
        [Tooltip("Share of the front face the sockets use, from its right end.")]
        [Range(0.3f, 1f)] public float faceShare = 0.67f;
        [Tooltip("Socket centre below the cap top (island-local metres).")]
        public float socketDepth = 0.03f;
        [Tooltip("Pod centre in front of the rock face (island-local metres).")]
        public float podOut = 0.011f;

        [Header("Cradle")]
        [Tooltip("From the near-right footprint corner: x inward, y above the cap, z inward (island-local metres).")]
        public Vector3 cradleOffset = new Vector3(0.075f, 0.06f, 0.035f);

        [Header("Input")]
        [Tooltip("A fingertip this many pod radii from the centre pokes it.")]
        public float pokeRadius = 1.4f;
        [Tooltip("The gaze ray picks a pod within this many pod radii.")]
        public float gazeRadius = 2.2f;
        [Tooltip("A pinch with the index tip this close to a pod (m) is a grab, not a gaze pinch.")]
        public float grabPinchDistance = 0.08f;
        [Tooltip("The rim grab pauses while a palm or fingertip is this close to a grabbable pod (m).")]
        public float rimGuardDistance = 0.07f;
        public float cardSeconds = 4f;

        /// <summary>A pod was let go while it could be used: slot, island-local position and velocity.</summary>
        public event Action<int, Vector3, Vector3> Thrown;
        /// <summary>Look + pinch on a pod that can be used (battle).</summary>
        public event Action<int> Picked;

        public bool InBattle { get; set; }
        /// <summary>Party pods that can fight (not the one out) can be grabbed and picked.</summary>
        public bool PartyGrab { get; set; }
        public bool CradleGrab { get; set; }
        public bool CradleVisible { get; set; }
        /// <summary>The pod whose creature is out on the field (empty glass), or -1.</summary>
        public int ActiveSlot { get; set; } = -1;
        public int GlowSlot { get; set; } = -1;
        public bool CradleGlow { get; set; }
        /// <summary>The gaze is on a pod (its pinch wins over tile gaze).</summary>
        public bool IsGazed { get; private set; }

        public bool AnyHeld
        {
            get
            {
                foreach (var p in _pods)
                    if (p != null && Grab(p) != null && Grab(p).IsHeld) return true;
                return false;
            }
        }

        readonly PodView[] _pods = new PodView[Party.Size + 1];
        readonly MeshRenderer[] _sockets = new MeshRenderer[Party.Size];
        readonly Vector3[] _socketPos = new Vector3[Party.Size];
        Transform _cradle;
        MeshRenderer _cradleRing;
        Vector3 _cradlePos;
        float _cradleShown, _cradlePop = -1f;
        PodCard _card;
        Mesh _quad;
        readonly Hand[] _hands = new Hand[2];
        readonly bool[] _wasPinching = { true, true };
        readonly int[] _poked = { -1, -1 };
        int _gazed = -1;
        bool _rimPaused, _laidOut;
        MaterialPropertyBlock _socketBlock;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly Color SocketLit = new Color(0.79f, 0.6f, 0.26f), SocketDim = new Color(0.36f, 0.3f, 0.23f);

        void Start()
        {
            if (island == null || party == null || podPrefab == null)
            {
                Debug.LogError("[PodRack] Needs the island, the party and the pod prefab (Tools > Pocket Isle > Set Up MR_Main (6 capture)).", this);
                enabled = false;
                return;
            }
            if (hudMaterial == null) Debug.LogError("[PodRack] No HUD material; stats cards won't show.", this);
            _socketBlock = new MaterialPropertyBlock();
            _quad = BattleHud.VerticalQuad();
            if (hudMaterial != null) _card = new PodCard(hudMaterial, _quad);
            Layout();
        }

        void OnDisable() => ResumeRim();

        void OnDestroy()
        {
            if (_card != null) _card.Destroy();
            if (_quad != null) Destroy(_quad);
        }

        // ---- layout ----

        /// <summary>Socket and cradle positions from the island footprint (fixed: the rim doesn't scroll).</summary>
        void Layout()
        {
            var map = island.map;
            int rows = map.Rows, cols = map.Columns;
            float tile = map.tileSize, cr = (rows - 1) * 0.5f, cc = (cols - 1) * 0.5f;
            int front = -1, c0 = 0, c1 = 0;
            for (int r = rows - 1; r >= 0 && front < 0; r--)
                for (int c = 0; c < cols; c++)
                {
                    if (map.At(r, c) == IslandMap.Empty) continue;
                    if (front < 0) { front = r; c0 = c; }
                    c1 = c;
                }
            if (front < 0) return;
            // Columns run toward -X: c0 is the player's left (+X), c1 the right end.
            float z = (front - cr) * tile + tile * 0.5f;
            float left = (cc - c0) * tile + tile * 0.5f, right = (cc - c1) * tile - tile * 0.5f;
            float spanLeft = right + (left - right) * faceShare;
            float margin = tile * 0.55f;
            float step = (spanLeft - right - 2f * margin) / (Party.Size - 1);
            float y = map.capHeight - socketDepth;
            for (int i = 0; i < Party.Size; i++)
            {
                float x = spanLeft - margin - i * step;
                _socketPos[i] = new Vector3(x, y, z + podOut);
                if (socketMesh != null && socketMaterial != null)
                {
                    var go = new GameObject($"Pod Socket {i + 1}");
                    go.transform.SetParent(island.transform, false);
                    go.transform.localPosition = new Vector3(x, y, z + 0.0015f);
                    go.AddComponent<MeshFilter>().sharedMesh = socketMesh;
                    _sockets[i] = go.AddComponent<MeshRenderer>();
                    _sockets[i].sharedMaterial = socketMaterial;
                    _sockets[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
            var b = island.LocalBounds;
            _cradlePos = new Vector3(b.min.x + cradleOffset.x, map.capHeight + cradleOffset.y, b.max.z - cradleOffset.z);
            _cradle = new GameObject("Catch Cradle").transform;
            _cradle.SetParent(island.transform, false);
            _cradle.localPosition = _cradlePos;
            if (socketMesh != null && socketMaterial != null)
            {
                var ring = new GameObject("Cradle Ring");
                ring.transform.SetParent(_cradle, false);
                ring.transform.localPosition = Vector3.down * (podPrefab.radius * 0.75f);
                ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                ring.AddComponent<MeshFilter>().sharedMesh = socketMesh;
                _cradleRing = ring.AddComponent<MeshRenderer>();
                _cradleRing.sharedMaterial = socketMaterial;
                _cradleRing.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            _cradle.gameObject.SetActive(false);
            _laidOut = true;
            Debug.Log($"[PodRack] {Party.Size} sockets on the front face (row {front}, x {_socketPos[0].x:0.000} to {_socketPos[Party.Size - 1].x:0.000}), cradle at {_cradlePos}.");
        }

        /// <summary>Where a pod rests (island-local).</summary>
        public Vector3 HomeLocal(int slot)
        {
            if (slot == CradleSlot)
            {
                float bob = Mathf.Sin(Time.time * 2.2f) * 0.002f;
                return _cradlePos + Vector3.up * bob;
            }
            return _socketPos[Mathf.Clamp(slot, 0, Party.Size - 1)];
        }

        public Vector3 HomeWorld(int slot) => island.transform.TransformPoint(HomeLocal(slot));

        public PodView Pod(int slot) => slot >= 0 && slot < _pods.Length ? _pods[slot] : null;

        static PodGrab Grab(PodView p) => p != null ? p.GetComponent<PodGrab>() : null;

        // ---- per frame ----

        void Update()
        {
            if (!_laidOut) return;
            SyncParty();
            UpdateCradle();
            for (int i = 0; i < _pods.Length; i++)
            {
                var p = _pods[i];
                if (p == null) continue;
                var g = Grab(p);
                bool held = g != null && g.IsHeld;
                if (g != null) g.CanGrab = !p.Detached && (held || CanUse(i));
                if (!p.Detached && !held)
                {
                    p.transform.localPosition = HomeLocal(i);
                    p.transform.localRotation = Quaternion.identity;
                }
                p.CreatureOut = i == ActiveSlot;
                p.Glow = i == CradleSlot ? (CradleGlow ? 1f : 0.25f) : i == GlowSlot ? 1f : 0f;
            }
            for (int i = 0; i < Party.Size; i++)
            {
                if (_sockets[i] == null) continue;
                _socketBlock.SetColor(BaseColorId, _pods[i] != null ? SocketLit : SocketDim);
                _sockets[i].SetPropertyBlock(_socketBlock);
            }
            UpdateInput();
            UpdateRimGuard();
            if (_card != null) _card.Tick();
        }

        /// <summary>One pod per party creature; a changed creature is re-shown.</summary>
        void SyncParty()
        {
            for (int i = 0; i < Party.Size; i++)
            {
                var c = party.At(i);
                var p = _pods[i];
                if (c == null || c.species == null)
                {
                    if (p != null && !p.Detached) Destroy(p.gameObject);
                    continue;
                }
                if (p == null)
                {
                    p = _pods[i] = Spawn($"Pod {i + 1}", i);
                    p.transform.localPosition = HomeLocal(i);
                }
                if (p.Creature != c) p.Show(c);
            }
        }

        PodView Spawn(string name, int slot)
        {
            var p = Instantiate(podPrefab, island.transform, false);
            p.name = name;
            p.BaseScale = Vector3.one;
            var g = Grab(p);
            if (g != null)
            {
                g.CanGrab = false;
                g.Grabbed += _ => OnGrabbed(p);
                g.Released += (_, v) => OnReleased(p, v);
            }
            return p;
        }

        int SlotOf(PodView p) => Array.IndexOf(_pods, p);

        /// <summary>Can this pod be grabbed or picked right now?</summary>
        bool CanUse(int slot)
        {
            if (!InBattle) return false;
            if (slot == CradleSlot) return CradleGrab && CradleVisible;
            return PartyGrab && slot != ActiveSlot && party.CanFight(slot);
        }

        void OnGrabbed(PodView p)
        {
            if (_card != null) _card.Hide();
            Debug.Log($"[PodRack] Grabbed {p.name}.");
        }

        void OnReleased(PodView p, Vector3 velocityWorld)
        {
            int slot = SlotOf(p);
            if (slot < 0) return;
            var t = island.transform;
            Vector3 pos = t.InverseTransformPoint(p.transform.position), vel = t.InverseTransformVector(velocityWorld);
            bool usable = CanUse(slot) && Thrown != null;
            Debug.Log($"[PodRack] Released {p.name} at {velocityWorld.magnitude:0.00} m/s{(usable ? "" : " (not usable now: back to its socket)")}.");
            p.Detached = true;
            if (usable) Thrown(slot, pos, vel);
            else StartCoroutine(FlyHome(slot));
        }

        void UpdateCradle()
        {
            float target = CradleVisible ? 1f : 0f;
            _cradleShown = Mathf.MoveTowards(_cradleShown, target, Time.deltaTime * 4f);
            if (_cradle == null) return;
            _cradle.gameObject.SetActive(_cradleShown > 0.001f);
            float pop = 0f;
            if (_cradlePop >= 0f)
            {
                float u = (Time.time - _cradlePop) / 0.45f;
                if (u >= 1f) _cradlePop = -1f;
                else pop = Mathf.Sin(u * Mathf.PI) * 0.35f;
            }
            _cradle.localPosition = HomeLocal(CradleSlot);
            _cradle.localScale = Vector3.one * (Mathf.SmoothStep(0f, 1f, _cradleShown) * (1f + pop));
            var pod = _pods[CradleSlot];
            if (CradleVisible && pod == null)
            {
                pod = _pods[CradleSlot] = Spawn("Catch Pod", CradleSlot);
                pod.Show(null);
                pod.transform.localPosition = HomeLocal(CradleSlot);
            }
            if (pod != null && !pod.Detached && !(Grab(pod) != null && Grab(pod).IsHeld))
            {
                pod.BaseScale = Vector3.one * (Mathf.SmoothStep(0f, 1f, _cradleShown) * (1f + pop));
                if (_cradleShown <= 0.001f)
                {
                    Destroy(pod.gameObject);
                    _pods[CradleSlot] = null;
                }
            }
        }

        /// <summary>The cradle pod pops up and glows (the Catch rune).</summary>
        public void PresentCradle()
        {
            _cradlePop = Time.time;
            CradleGlow = true;
            if (fx != null) fx.Burst(HomeWorld(CradleSlot), 12, new Color(1f, 0.88f, 0.54f), World(0.03f), 0.45f, World(0.003f));
        }

        float World(float local) => local * island.transform.lossyScale.x;

        // ---- poke, gaze + pinch ----

        void UpdateInput()
        {
            // Gaze: the pod closest to the ray, within gazeRadius pod radii.
            _gazed = -1;
            if (HeadGaze.TryGetRay(out var ray))
            {
                float best = float.MaxValue;
                for (int i = 0; i < _pods.Length; i++)
                {
                    var p = _pods[i];
                    if (p == null || p.Detached || !Pointable(i)) continue;
                    Vector3 to = p.transform.position - ray.origin;
                    float along = Vector3.Dot(to, ray.direction);
                    if (along <= 0f) continue;
                    float off = (to - ray.direction * along).magnitude / Mathf.Max(1e-5f, p.WorldRadius);
                    if (off < gazeRadius && off < best)
                    {
                        best = off;
                        _gazed = i;
                    }
                }
            }
            IsGazed = _gazed >= 0;

            for (int h = 0; h < 2; h++)
            {
                bool left = h == 0;
                var hand = HandLookup.Refresh(ref _hands[h], left ? Handedness.Left : Handedness.Right);
                bool tracked = HandLookup.TryGetJoint(hand, HandJointId.HandIndexTip, out var tip);

                // Poke: a fingertip entering a pod.
                int poked = -1;
                if (tracked)
                    for (int i = 0; i < _pods.Length; i++)
                    {
                        var p = _pods[i];
                        if (p == null || p.Detached || !Pointable(i)) continue;
                        if ((tip - p.transform.position).sqrMagnitude < Sq(p.WorldRadius * pokeRadius)) poked = i;
                    }
                if (poked >= 0 && poked != _poked[h] && !InBattle) ShowCard(poked, "poke");
                _poked[h] = poked;

                // Gaze + pinch (not a pinch that grabs a pod).
                bool pinching = tracked && hand.GetIndexFingerIsPinching();
                bool busy = handle != null && handle.IsHandBusy(left);
                if (pinching && !_wasPinching[h] && !busy && _gazed >= 0 && !NearAnyPod(tip, grabPinchDistance))
                {
                    if (InBattle)
                    {
                        Debug.Log($"[PodRack] Gaze + pinch ({(left ? "left" : "right")}) on {_pods[_gazed].name}.");
                        Picked?.Invoke(_gazed);
                    }
                    else ShowCard(_gazed, "gaze + pinch");
                }
                _wasPinching[h] = pinching;
            }

            for (int i = 0; i < _pods.Length; i++)
                if (_pods[i] != null) _pods[i].Hover = i == _gazed || i == _poked[0] || i == _poked[1] ? 1f : 0f;
        }

        /// <summary>Outside battle every party pod shows its card; in battle only usable pods respond.</summary>
        bool Pointable(int slot) => InBattle ? CanUse(slot) : slot < Party.Size;

        bool NearAnyPod(Vector3 point, float distance)
        {
            foreach (var p in _pods)
                if (p != null && (p.transform.position - point).sqrMagnitude < distance * distance) return true;
            return false;
        }

        static float Sq(float x) => x * x;

        void ShowCard(int slot, string how)
        {
            var p = _pods[slot];
            if (_card == null || p == null || p.Creature == null) return;
            if (_card.Visible && _card.Showing == p.Creature && how == "gaze + pinch")
            {
                _card.Hide();
                return;
            }
            Vector3 up = island.transform.up;
            Vector3 anchor = p.transform.position + up * World(0.055f) + island.transform.forward * World(0.03f);
            _card.Show(p.Creature, anchor, cardSeconds);
            Debug.Log($"[PodRack] Card for {p.Creature.Name} ({how}).");
        }

        // ---- rim guard ----

        void UpdateRimGuard()
        {
            var rim = handle != null ? handle.rimInteractable : null;
            if (rim == null) return;
            bool reaching = false;
            if (InBattle && (PartyGrab || CradleGrab) && rim.SelectingInteractors.Count == 0)
                for (int h = 0; h < 2 && !reaching; h++)
                {
                    var hand = HandLookup.Refresh(ref _hands[h], h == 0 ? Handedness.Left : Handedness.Right);
                    foreach (var joint in new[] { HandJointId.HandIndexTip, HandJointId.HandPalm })
                    {
                        if (!HandLookup.TryGetJoint(hand, joint, out var p)) continue;
                        for (int i = 0; i < _pods.Length && !reaching; i++)
                            if (_pods[i] != null && CanUse(i) && (_pods[i].transform.position - p).sqrMagnitude < Sq(rimGuardDistance)) reaching = true;
                    }
                }
            if (reaching && !_rimPaused)
            {
                _rimPaused = true;
                rim.enabled = false;
                Debug.Log("[PodRack] Hand reaching for a pod: rim grab paused.");
            }
            else if (!reaching && _rimPaused) ResumeRim();
        }

        void ResumeRim()
        {
            if (!_rimPaused) return;
            _rimPaused = false;
            var rim = handle != null ? handle.rimInteractable : null;
            if (rim != null) rim.enabled = true;
        }

        // ---- flights (island-local) ----

        /// <summary>Horizontal angle (degrees, island plane) between a throw and the direction to a target.</summary>
        public static float AimAngle(Vector3 from, Vector3 velocity, Vector3 target)
        {
            Vector2 v = new Vector2(velocity.x, velocity.z), d = new Vector2(target.x - from.x, target.z - from.z);
            if (v.sqrMagnitude < 1e-8f || d.sqrMagnitude < 1e-8f) return 180f;
            return Vector2.Angle(v, d);
        }

        /// <summary>Arcs a pod from where it is to <paramref name="target"/>, leaving along the throw if there is one.</summary>
        public IEnumerator FlyTo(int slot, Vector3 target, Vector3 velocity, float seconds, float arc = 0.04f)
        {
            var p = _pods[slot];
            if (p == null) yield break;
            p.Detached = true;
            var t = p.transform;
            Vector3 from = t.localPosition;
            float dist = Vector3.Distance(from, target);
            Vector3 dir = velocity.sqrMagnitude > 1e-4f ? velocity.normalized : (target - from).normalized;
            Vector3 control = from + dir * (dist * 0.35f) + Vector3.up * (arc + dist * 0.2f);
            Quaternion r0 = t.localRotation;
            for (float u = 0f; u < 1f; u += Time.deltaTime / Mathf.Max(0.05f, seconds))
            {
                if (t == null) yield break;
                float k = Mathf.SmoothStep(0f, 1f, u) * 0.6f + u * 0.4f;
                t.localPosition = Bezier(from, control, target, k);
                t.localRotation = Quaternion.Slerp(r0, Quaternion.Euler(u * 540f, 0f, 0f), u);
                yield return null;
            }
            if (t == null) yield break;
            t.localPosition = target;
            t.localRotation = Quaternion.identity;
        }

        /// <summary>Flies the pod back to its socket or the cradle and hands it back to the rack.</summary>
        public IEnumerator FlyHome(int slot, float seconds = 0.45f)
        {
            var p = _pods[slot];
            if (p == null) yield break;
            p.Detached = true;
            var t = p.transform;
            Vector3 from = t.localPosition;
            Vector3 control = (from + HomeLocal(slot)) * 0.5f + Vector3.up * 0.05f;
            Quaternion r0 = t.localRotation;
            for (float u = 0f; u < 1f; u += Time.deltaTime / seconds)
            {
                if (t == null) yield break;
                t.localPosition = Bezier(from, control, HomeLocal(slot), Mathf.SmoothStep(0f, 1f, u));
                t.localRotation = Quaternion.Slerp(r0, Quaternion.identity, u);
                yield return null;
            }
            if (p != null) p.Detached = false;
        }

        /// <summary>A throw that missed: falls along its path, bounces with a puff and comes back.</summary>
        public IEnumerator Miss(int slot, Vector3 velocity)
        {
            var p = _pods[slot];
            if (p == null) yield break;
            p.Detached = true;
            var t = p.transform;
            Vector3 from = t.localPosition, v = Vector3.ClampMagnitude(velocity, 1.2f);
            const float gravity = 2.5f, seconds = 0.4f;
            for (float u = 0f; u < seconds; u += Time.deltaTime)
            {
                if (t == null) yield break;
                t.localPosition = from + v * u + Vector3.down * (0.5f * gravity * u * u);
                yield return null;
            }
            if (fx != null && t != null) fx.Burst(t.position, 10, new Color(0.9f, 0.88f, 0.8f), World(0.03f), 0.35f, World(0.003f));
            yield return FlyHome(slot, 0.5f);
        }

        /// <summary>One wobble on the ground (rocks side to side).</summary>
        public IEnumerator Wobble(int slot)
        {
            var p = _pods[slot];
            if (p == null) yield break;
            var t = p.transform;
            for (float u = 0f; u < 1f; u += Time.deltaTime / 0.45f)
            {
                if (t == null) yield break;
                t.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(u * Mathf.PI * 2f) * 28f * (1f - u * 0.4f));
                yield return null;
            }
            if (t != null) t.localRotation = Quaternion.identity;
        }

        /// <summary>The cradle pod now holds a party creature: it becomes pod <paramref name="slot"/> (a new empty pod fills the cradle).</summary>
        public void AdoptCradlePod(int slot, CreatureInstance creature)
        {
            var p = _pods[CradleSlot];
            _pods[CradleSlot] = null;
            if (p == null || slot < 0 || slot >= Party.Size) return;
            if (_pods[slot] != null) Destroy(_pods[slot].gameObject);
            _pods[slot] = p;
            p.name = $"Pod {slot + 1}";
            p.Show(creature);
            p.BaseScale = Vector3.one;
        }

        /// <summary>The pod breaks open (the creature got out): a burst, and it is gone (a new one fills the cradle).</summary>
        public void Burst(int slot)
        {
            var p = _pods[slot];
            if (p == null) return;
            if (fx != null) fx.Burst(p.transform.position, 22, new Color(1f, 0.95f, 0.8f), World(0.05f), 0.4f, World(0.004f));
            Destroy(p.gameObject);
            _pods[slot] = null;
        }

        /// <summary>Party full: the pod shrinks into a light that streaks off the island.</summary>
        public IEnumerator StreakAway(int slot)
        {
            var p = _pods[slot];
            if (p == null) yield break;
            _pods[slot] = null;
            p.Detached = true;
            var t = p.transform;
            Vector3 from = t.localPosition, to = from + new Vector3(0.25f, 0.6f, -0.2f);
            var light = fx != null ? fx.Get(BattleFx.Shape.Orb) : null;
            var gold = new Color(1f, 0.85f, 0.45f);
            if (light != null) light.Set(Color.white, gold, 1f, 1.5f, 1f, 0.2f);
            for (float u = 0f; u < 1f; u += Time.deltaTime / 0.8f)
            {
                float k = u * u;
                Vector3 pos = Vector3.Lerp(from, to, k);
                if (t != null)
                {
                    t.localPosition = pos;
                    p.BaseScale = Vector3.one * Mathf.Max(0f, 1f - u * 3f);
                }
                if (light != null)
                {
                    light.transform.position = island.transform.TransformPoint(pos);
                    light.transform.localScale = Vector3.one * (World(0.012f * (1f - u * 0.5f)) / Mathf.Max(1e-4f, fx.transform.lossyScale.x));
                    light.SetAlpha(1f - u * u);
                }
                if (fx != null && Time.frameCount % 2 == 0) fx.Burst(island.transform.TransformPoint(pos), 2, gold, World(0.01f), 0.4f, World(0.003f));
                yield return null;
            }
            if (light != null) fx.Release(light);
            if (p != null) Destroy(p.gameObject);
        }

        static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            float s = 1f - t;
            return s * s * a + 2f * s * t * b + t * t * c;
        }
    }
}
