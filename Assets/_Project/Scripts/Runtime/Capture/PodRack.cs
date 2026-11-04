using System;
using System.Collections;
using Diorama.Audio;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// The party pods on the island's front rim and the catch cradle. Six brass sockets sit in the rocky side just under
    /// the cap, across the right part of the front face (the Spirit has the near-left corner); pod i holds party slot i.
    /// The cradle floats above the near-right corner and holds an empty pod, twice the party pod's size, for the whole
    /// battle; a pod that shatters is replaced after <see cref="cradleRefillSeconds"/>. Grab volumes are bigger than the
    /// glass (party 5 cm, cradle 8 cm across). The party pod under the gaze (or nearest a reaching hand) grows to
    /// <see cref="focusScale"/> with its grab volume, and its neighbours slide aside so nothing overlaps.
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
        [Tooltip("Step 9f: the XP curve, for the XP bar on the stats card.")]
        public ProgressionConfig progression;

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
        public Vector3 cradleOffset = new Vector3(0.075f, 0.1f, 0.035f);
        [Tooltip("Cradle pod size relative to a party pod.")]
        public float cradleScale = 2f;

        [Header("Grab volumes (diameter, metres at island scale 1)")]
        public float partyGrabSize = 0.05f;
        public float cradleGrabSize = 0.08f;

        [Header("Rim pods grow on gaze")]
        [Tooltip("Size of the looked-at (or reached-for) party pod relative to its rest size.")]
        public float focusScale = 2f;
        [Tooltip("Time to grow (s); shrinking takes a little longer.")]
        public float growSeconds = 0.15f;
        [Tooltip("The pod stays big this long after the gaze or hand leaves (s).")]
        public float shrinkDelay = 0.3f;
        [Tooltip("A fingertip or palm this close to a party pod (m) makes it the big one.")]
        public float handFocusDistance = 0.1f;
        [Tooltip("A fresh empty pod pops into the cradle this long after one shatters (s).")]
        public float cradleRefillSeconds = 1f;

        [Header("Input")]
        [Tooltip("A fingertip this many pod radii from the centre pokes it.")]
        public float pokeRadius = 1.4f;
        [Tooltip("Gaze volume diameter in pod radii (radius = half of this, at least HeadGaze.MinRadius); the gaze cone adds to it.")]
        public float gazeRadius = 2.2f;
        [Tooltip("A pinch with the index tip this close to a pod (m) is a grab, not a gaze pinch.")]
        public float grabPinchDistance = 0.08f;
        [Tooltip("The rim grab pauses while a palm or fingertip is this close to a grabbable pod (m).")]
        public float rimGuardDistance = 0.07f;
        [Tooltip("Pods active (battle, lodge): a grabbable pod comes this far out of its socket (world m).")]
        public float activePopOut = 0.003f;
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
        /// <summary>The cradle pod glows and pulses (the dazed window).</summary>
        public bool CradlePulse { get; set; }
        /// <summary>The gaze is on a pod (its pinch wins over tile gaze).</summary>
        public bool IsGazed { get; private set; }

        /// <summary>
        /// Outside battles: replaces the stats card on look + pinch (the storage shelf at a shrine). Gets the slot, which
        /// can be an empty socket just past the last pod; returns true when it handled the pick.
        /// </summary>
        public Func<int, bool> PickHandler { get; set; }

        /// <summary>Something else has the gaze (a shrine, a lodge Isling): counts as <see cref="IsGazed"/>, so tile gaze doesn't walk.</summary>
        public void SetExternalGaze(object owner, bool on)
        {
            if (on) _externalGaze.Add(owner);
            else _externalGaze.Remove(owner);
        }

        readonly System.Collections.Generic.HashSet<object> _externalGaze = new System.Collections.Generic.HashSet<object>();

        /// <summary>
        /// In the Hearth Lodge: party pods can be grabbed outside battle; letting one go raises <see cref="LodgeDropped"/>
        /// (slot, island-local position and velocity) instead of flying it home (the lodge decides).
        /// </summary>
        public bool LodgeMode { get; set; }

        /// <summary>
        /// Step 8e: exploring (no battle, not in the lodge). The rim pods are inert then: no grab, no poke, no growing
        /// toward a reaching hand (the island's grab zone covers the strip in front of them); look + pinch shows the card.
        /// </summary>
        public bool Explore => !InBattle && !LodgeMode;

        public event Action<int, Vector3, Vector3> LodgeDropped;

        /// <summary>A slot to highlight (a lodge selection), or -1.</summary>
        public int SelectedSlot { get; set; } = -1;

        /// <summary>
        /// Step 14h: the pod whose Isling is ready to evolve (GrowthDirector), or -1. It glows gold and pulses, and while
        /// exploring it alone can be grabbed (grab it and turn the hand palm up) or looked at + pinched (<see cref="DuePicked"/>).
        /// </summary>
        public int DueSlot { get; set; } = -1;

        /// <summary>Step 14h: look + pinch on the glowing <see cref="DueSlot"/> pod (exploring).</summary>
        public event Action<int> DuePicked;

        int _claimed = -1;

        /// <summary>
        /// Step 14h: the evolution takes pod <paramref name="slot"/> out of the rack: a hand holding it lets go (no fly home),
        /// the rack stops placing it and its tiny Isling stays as it is until <see cref="ReturnClaimed"/>.
        /// </summary>
        public bool Claim(int slot)
        {
            var p = Pod(slot);
            if (p == null || slot >= Party.Size) return false;
            _claimed = slot;
            _evolvingSlot = slot;
            p.Detached = true;
            var g = Grab(p);
            if (g != null) g.CanGrab = false;
            if (_card != null) _card.Hide();
            return true;
        }

        public bool IsHeld(int slot)
        {
            var p = Pod(slot);
            var g = p != null ? Grab(p) : null;
            return g != null && g.IsHeld;
        }

        /// <summary>Step 14h: the claimed pod shows its creature as it is now and flies back to its socket.</summary>
        public IEnumerator ReturnClaimed(int slot)
        {
            _claimed = -1;
            var p = Pod(slot);
            if (p != null && party.At(slot) != null) p.Show(party.At(slot));
            _evolvingSlot = -1;
            if (p != null) yield return FlyHome(slot, 0.6f);
        }

        /// <summary>Step 14h: the stats card for a creature at a moving point (the palm), for <paramref name="seconds"/>.</summary>
        public void ShowCardFor(IslingInstance c, Func<Vector3?> where, float seconds)
        {
            if (_card == null || c == null || MenuUp) return;
            _card.Show(c, where, seconds);
            _cardIslandPos = island.transform.position;
            _cardIslandRot = island.transform.rotation;
        }

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
        readonly bool[] _wasPinching = { true, true };
        readonly int[] _poked = { -1, -1 };
        int _gazed = -1;
        bool _rimPaused, _laidOut;
        float _partyGrab, _socketStep;
        readonly float[] _grow = new float[Party.Size];
        readonly float[] _slide = new float[Party.Size];
        readonly float[] _pop = new float[Party.Size];
        int _bigSlot = -1, _evolvingSlot = -1;
        float _bigUntil;
        float _cradleRefillAt = -1f, _cradleIn = 1f;
        float _waveStart = -1f;
        readonly float[] _xpFlash = new float[Party.Size];
        int _gazedEmpty = -1;
        const int EmptyId = 1000; // gaze pick id of the empty socket
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
            if (progression == null) Debug.LogError("[PodRack] No progression config; the stats card shows no XP bar (Set Up MR_Main (9f)).", this);
            _socketBlock = new MaterialPropertyBlock();
            for (int i = 0; i < _xpFlash.Length; i++) _xpFlash[i] = -10f;
            _quad = BattleHud.VerticalQuad();
            if (hudMaterial != null) _card = new PodCard(hudMaterial, _quad, progression);
            Layout();
            if (handle != null) handle.GrabStarted += CloseCard;
            Dialogue.RegisterTarget("leadPod", () => PointOf(party.LeadSlot), k => PulseOf(party.LeadSlot, k), () => FocusRadius(party.LeadSlot));
            // The cradle stays a target while a shattered pod is being replaced.
            // Lodge hints: where an Isling can join (the next empty socket, else the last pod to swap with) and a pod
            // that may be let go (never the only one).
            Dialogue.RegisterTarget("rimSocket", () => party.members.Count < Party.Size ? HomeWorld(party.members.Count) : PointOf(party.members.Count - 1),
                k => { }, () => FocusRadius(Mathf.Min(party.members.Count, Party.Size - 1)));
            Dialogue.RegisterTarget("rimPod", () => party.members.Count > 1 ? PointOf(party.members.Count - 1) : null,
                k => PulseOf(party.members.Count - 1, k), () => FocusRadius(party.members.Count - 1));
            Dialogue.RegisterTarget("cradle", () => CradleVisible ? HomeWorld(CradleSlot) : (Vector3?)null, k => PulseOf(CradleSlot, k), () => FocusRadius(CradleSlot));
        }

        /// <summary>Step 13: pod <paramref name="slot"/>'s world point, or null while it isn't shown.</summary>
        public Vector3? SlotWorld(int slot) => PointOf(slot);

        Vector3? PointOf(int slot)
        {
            if (this == null) return null;
            var p = Pod(slot);
            return p != null && p.gameObject.activeInHierarchy ? p.transform.position : (Vector3?)null;
        }

        /// <summary>Gold focus sphere round a pod (just the pod, not its neighbours).</summary>
        float FocusRadius(int slot)
        {
            var p = Pod(slot);
            return (p != null ? p.WorldRadius : podPrefab.radius * island.transform.lossyScale.x) * 1.2f;
        }

        void PulseOf(int slot, float k)
        {
            if (this == null) return;
            for (int i = 0; i < _pods.Length; i++)
                if (_pods[i] != null) _pods[i].Pulse = i == slot ? k : 0f;
        }

        void OnDisable() => ResumeRim();

        void OnDestroy()
        {
            if (handle != null) handle.GrabStarted -= CloseCard;
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
            if (!SocketRow(out int front, out float z, out float right, out float spanLeft, out float margin, out float step)) return;
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
            // Grab volumes may not overlap their neighbours.
            _socketStep = Mathf.Abs(step);
            _partyGrab = Mathf.Min(partyGrabSize, _socketStep * 0.96f);
            if (_partyGrab < partyGrabSize) Debug.Log($"[PodRack] Party grab volume {_partyGrab * 100f:0.0} cm (sockets {Mathf.Abs(step) * 100f:0.0} cm apart).");
            var b = island.LocalBounds;
            _cradlePos = new Vector3(b.min.x + cradleOffset.x, map.capHeight + cradleOffset.y, b.max.z - cradleOffset.z);
            _cradle = new GameObject("Catch Cradle").transform;
            _cradle.SetParent(island.transform, false);
            _cradle.localPosition = _cradlePos;
            if (socketMesh != null && socketMaterial != null)
            {
                var ring = new GameObject("Cradle Ring");
                ring.transform.SetParent(_cradle, false);
                ring.transform.localPosition = Vector3.down * (podPrefab.radius * 0.75f * cradleScale);
                ring.transform.localScale = Vector3.one * cradleScale;
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

        /// <summary>
        /// The socket row on the front face (island-local): front row, its z, the right end, the left end of the socket
        /// span, the end margin and the socket step (x runs from spanLeft - margin down by step).
        /// </summary>
        bool SocketRow(out int front, out float z, out float right, out float spanLeft, out float margin, out float step)
        {
            var map = island.map;
            int rows = map.Rows, cols = map.Columns;
            float tile = map.tileSize, cr = (rows - 1) * 0.5f, cc = (cols - 1) * 0.5f;
            front = -1;
            int c0 = 0, c1 = 0;
            for (int r = rows - 1; r >= 0 && front < 0; r--)
                for (int c = 0; c < cols; c++)
                {
                    if (map.At(r, c) == IslandMap.Empty) continue;
                    if (front < 0) { front = r; c0 = c; }
                    c1 = c;
                }
            z = right = spanLeft = margin = step = 0f;
            if (front < 0) return false;
            // Columns run toward -X: c0 is the player's left (+X), c1 the right end.
            z = (front - cr) * tile + tile * 0.5f;
            float left = (cc - c0) * tile + tile * 0.5f;
            right = (cc - c1) * tile - tile * 0.5f;
            spanLeft = right + (left - right) * faceShare;
            margin = tile * 0.55f;
            step = (spanLeft - right - 2f * margin) / (Party.Size - 1);
            return true;
        }

        /// <summary>
        /// The front strip the six sockets and their grab volumes use (island-local x, plus a hand's width of clearance).
        /// The island's rim grab stays off it (step 8c: reaching for a pod grabbed the island). Works in edit mode.
        /// </summary>
        public bool SocketBandLocal(out float minX, out float maxX, out float frontZ)
        {
            minX = maxX = frontZ = 0f;
            if (island == null || island.map == null || !SocketRow(out _, out frontZ, out float right, out float spanLeft, out float margin, out float step)) return false;
            float pad = Mathf.Min(partyGrabSize, Mathf.Abs(step) * 0.96f) * 0.5f * focusScale + 0.01f;
            maxX = spanLeft - margin + pad;
            minX = spanLeft - margin - (Party.Size - 1) * step - pad;
            return true;
        }

        /// <summary>Where a pod rests (island-local).</summary>
        public Vector3 HomeLocal(int slot)
        {
            if (slot == CradleSlot)
            {
                float bob = Mathf.Sin(Time.time * 2.2f) * 0.002f;
                return _cradlePos + Vector3.up * bob;
            }
            int i = Mathf.Clamp(slot, 0, Party.Size - 1);
            // Slid aside for a big neighbour; a big pod comes out of the rock face by its extra radius.
            float grow = Mathf.SmoothStep(0f, 1f, _grow[i]);
            // Pods active: a grabbable pod sits a little out of its socket.
            float pop = activePopOut / Mathf.Max(1e-4f, island.transform.lossyScale.x) * Mathf.SmoothStep(0f, 1f, _pop[i]);
            return _socketPos[i] + new Vector3(_slide[i], 0f, podPrefab.radius * (focusScale - 1f) * grow + pop);
        }

        /// <summary>Rest scale of a party pod right now (1, up to <see cref="focusScale"/> while it is the big one).</summary>
        float FocusScale(int slot) => 1f + (focusScale - 1f) * Mathf.SmoothStep(0f, 1f, _grow[slot]);

        public Vector3 HomeWorld(int slot) => island.transform.TransformPoint(HomeLocal(slot));

        public PodView Pod(int slot) => slot >= 0 && slot < _pods.Length ? _pods[slot] : null;

        static PodGrab Grab(PodView p) => p != null ? p.GetComponent<PodGrab>() : null;

        // ---- per frame ----

        void Update()
        {
            if (!_laidOut) return;
            SyncParty();
            UpdateCradle();
            UpdateDropPreview();
            for (int i = 0; i < _pods.Length; i++)
            {
                var p = _pods[i];
                if (p == null) continue;
                var g = Grab(p);
                bool held = g != null && g.IsHeld;
                if (g != null) g.CanGrab = !p.Detached && (held || CanUse(i));
                // Step 14a: a rim pod held in the lodge near its own socket previews the put-back.
                if (held && LodgeMode && !InBattle && i < Party.Size && (HomeWorld(i) - p.transform.position).sqrMagnitude < RimSnapReach * RimSnapReach)
                {
                    _previewSlot = i;
                    _previewFrame = Time.frameCount;
                }
                if (i < Party.Size)
                {
                    _pop[i] = Mathf.MoveTowards(_pop[i], (!Explore || i == DueSlot) && CanUse(i) ? 1f : 0f, Time.deltaTime * 6f);
                    p.Grabbable = _pop[i];
                }
                if (!p.Detached && !held)
                {
                    p.transform.localPosition = HomeLocal(i);
                    p.transform.localRotation = Quaternion.identity;
                    if (i < Party.Size) p.BaseScale = Vector3.one * FocusScale(i);
                }
                p.IslingOut = i == ActiveSlot;
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 6f);
                p.Glow = i == CradleSlot ? (CradlePulse ? 0.9f + 0.6f * pulse : CradleGlow ? 0.8f : 0f)
                    : Mathf.Max(Mathf.Max(i == GlowSlot || i == SelectedSlot ? 1f : i == DueSlot ? 0.7f + 0.8f * pulse : 0f, WaveGlow(i)), XpGlow(i));
                // Step 9h: gold = act on this now (the lead pod to send out, the armed cradle pod; 14h: ready to evolve).
                p.Actionable = i == GlowSlot || i == DueSlot || i == CradleSlot && (CradleGlow || CradlePulse) ? 1f : 0f;
                if (!p.Detached && !held) p.SetGrabDiameter(i == CradleSlot ? cradleGrabSize : _partyGrab * FocusScale(i));
            }
            for (int i = 0; i < Party.Size; i++)
            {
                if (_sockets[i] == null) continue;
                _socketBlock.SetColor(BaseColorId, _pods[i] != null ? SocketLit : SocketDim);
                _sockets[i].SetPropertyBlock(_socketBlock);
            }
            UpdateInput();
            UpdateRimGuard();
            if (_card != null)
            {
                // The card belongs to the island where it was opened: it closes once the island moves or turns.
                if (_card.Visible && ((island.transform.position - _cardIslandPos).sqrMagnitude > 0.02f * 0.02f ||
                                      Quaternion.Angle(island.transform.rotation, _cardIslandRot) > 4f))
                    CloseCard();
                // Nov 3: the menu's runes sit over the pods; the card overlapped them.
                if (_card.Visible && MenuUp)
                {
                    _card.Hide();
                    Debug.Log("[PodRack] Card closed (the menu opened).");
                }
                _card.Tick();
            }
        }

        static bool MenuUp => GameMenu.IsOpen || SettingsBoard.IsOpen || JournalBoard.IsOpen;

        Vector3 _cardIslandPos;
        Quaternion _cardIslandRot;

        void CloseCard()
        {
            if (_card == null || !_card.Visible) return;
            _card.Hide();
            Debug.Log("[PodRack] Card closed (the island moved).");
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
                if (i != _evolvingSlot && (p.Isling != c || p.ShownSpecies != c.species)) p.Show(c);
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
            if (!InBattle) return slot < Party.Size && party.At(slot) != null && (LodgeMode || slot == DueSlot && slot != _claimed);
            if (slot == CradleSlot) return CradleGrab && CradleVisible;
            return PartyGrab && slot != ActiveSlot && party.CanFight(slot);
        }

        void OnGrabbed(PodView p)
        {
            if (_card != null) _card.Hide();
            GameAudio.At(AudioIds.PodGrab, p.transform.position);
            Debug.Log($"[PodRack] Grabbed {p.name}.");
        }

        void OnReleased(PodView p, Vector3 velocityWorld)
        {
            int slot = SlotOf(p);
            if (slot < 0) return;
            // Step 14h: the evolution took it (palm up): it stays where it is.
            if (slot == _claimed)
            {
                p.Detached = true;
                Debug.Log($"[PodRack] {p.name} let go into the evolution.");
                return;
            }
            var t = island.transform;
            Vector3 pos = t.InverseTransformPoint(p.transform.position), vel = t.InverseTransformVector(velocityWorld);
            bool usable = InBattle && CanUse(slot) && Thrown != null;
            Debug.Log($"[PodRack] Released {p.name} at {velocityWorld.magnitude:0.00} m/s{(usable ? "" : " (not usable now: back to its socket)")}.");
            p.Detached = true;
            if (LodgeMode && !InBattle && slot < Party.Size && LodgeDropped != null)
            {
                LodgeDropped(slot, pos, vel);
                return;
            }
            if (usable)
            {
                GameAudio.On(AudioIds.PodThrow, p.transform);
                Thrown(slot, pos, vel);
            }
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
            if (CradlePulse) pop += 0.06f * Mathf.Sin(Time.time * 6f);
            _cradle.localPosition = HomeLocal(CradleSlot);
            _cradle.localScale = Vector3.one * (Mathf.SmoothStep(0f, 1f, _cradleShown) * (1f + pop));
            var pod = _pods[CradleSlot];
            if (CradleVisible && pod == null && Time.time >= _cradleRefillAt)
            {
                pod = _pods[CradleSlot] = Spawn("Catch Pod", CradleSlot);
                pod.Show(null);
                pod.transform.localPosition = HomeLocal(CradleSlot);
                if (_cradleRefillAt >= 0f)
                {
                    // A fresh pod after one shattered: it pops in.
                    _cradleIn = 0f;
                    _cradleRefillAt = -1f;
                    if (fx != null) fx.Burst(HomeWorld(CradleSlot), 10, new Color(1f, 0.88f, 0.54f), World(0.025f), 0.4f, World(0.003f));
                }
            }
            _cradleIn = Mathf.Min(1f, _cradleIn + Time.deltaTime / 0.35f);
            if (pod != null && !pod.Detached && !(Grab(pod) != null && Grab(pod).IsHeld))
            {
                pod.BaseScale = Vector3.one * (cradleScale * Mathf.SmoothStep(0f, 1f, _cradleShown) * (1f + pop) * EaseOutBack(_cradleIn));
                if (_cradleShown <= 0.001f)
                {
                    Destroy(pod.gameObject);
                    _pods[CradleSlot] = null;
                }
            }
        }

        static float EaseOutBack(float t)
        {
            const float c = 1.7f;
            t -= 1f;
            return Mathf.Max(0.001f, 1f + (c + 1f) * t * t * t + c * t * t);
        }

        /// <summary>Waits until the cradle holds a pod again (after a shatter it refills in about a second).</summary>
        public IEnumerator WaitForCradlePod()
        {
            while (CradleVisible && (_pods[CradleSlot] == null || _cradleIn < 1f)) yield return null;
        }

        /// <summary>A warm glow rolls along the rim pods, left to right (a Hearth Shrine heals them).</summary>
        public void WarmWave()
        {
            _waveStart = Time.time;
            for (int i = 0; i < Party.Size; i++)
                if (_pods[i] != null) StartCoroutine(WaveBurst(i, i * 0.08f));
        }

        IEnumerator WaveBurst(int slot, float delay)
        {
            yield return new WaitForSeconds(delay);
            var p = _pods[slot];
            if (p != null && fx != null) fx.Burst(p.transform.position, 8, new Color(1f, 0.75f, 0.4f), World(0.02f), 0.5f, World(0.003f), Vector3.up * World(0.02f));
        }

        /// <summary>Step 9f: a benched Isling that fought got XP: its pod glows up and fades over ~1.6 s.</summary>
        public void FlashXp(int slot)
        {
            if (slot >= 0 && slot < _xpFlash.Length) _xpFlash[slot] = Time.time;
        }

        /// <summary>The party slot whose pod holds this creature, or -1.</summary>
        public int SlotOf(IslingInstance c)
        {
            for (int i = 0; i < Party.Size; i++)
                if (c != null && _pods[i] != null && _pods[i].Isling == c) return i;
            return -1;
        }

        float XpGlow(int slot)
        {
            if (slot < 0 || slot >= _xpFlash.Length) return 0f;
            float u = Time.time - _xpFlash[slot];
            return u <= 0f || u >= 1.6f ? 0f : 1.4f * Mathf.Sin(u / 1.6f * Mathf.PI);
        }

        float WaveGlow(int slot)
        {
            if (_waveStart < 0f) return 0f;
            float u = Time.time - _waveStart - slot * 0.08f;
            if (u > 2f && slot == Party.Size - 1) _waveStart = -1f;
            return u <= 0f || u >= 0.8f ? 0f : 1.6f * Mathf.Sin(u / 0.8f * Mathf.PI);
        }

        /// <summary>Step 14a: a held pod this close (world m) to a rim socket previews it, and dropped there flies in.</summary>
        public const float RimSnapReach = 0.12f;

        PodView _ghost;
        int _previewSlot = -1, _previewFrame = -10;
        float _previewShown;

        /// <summary>
        /// Step 14a: call every frame while a pod is held (starter pick, lodge swap, a put-back): the socket it would fly
        /// into from <paramref name="heldWorld"/> (within <see cref="RimSnapReach"/>) glows with a ghost pod in it.
        /// Returns that socket or -1. <paramref name="emptyOnly"/>: only the next empty socket counts (a starter).
        /// </summary>
        public int PreviewDrop(Vector3 heldWorld, bool emptyOnly = false)
        {
            int slot = emptyOnly ? NextEmptyWithin(heldWorld, RimSnapReach) : NearestSocket(heldWorld, RimSnapReach);
            if (slot >= 0)
            {
                if (slot != _previewSlot) GameAudio.At(AudioIds.PodWiggle, HomeWorld(slot)).SetVolume(0.5f);
                _previewSlot = slot;
                _previewFrame = Time.frameCount;
            }
            return slot;
        }

        int NextEmptyWithin(Vector3 world, float maxWorld)
        {
            int next = Mathf.Min(Party.Size - 1, party.members.Count);
            return (HomeWorld(next) - world).sqrMagnitude <= maxWorld * maxWorld ? next : -1;
        }

        /// <summary>The ghost pod in the previewed socket: grows in, glows and pulses; gone a moment after the preview stops.</summary>
        void UpdateDropPreview()
        {
            bool on = _previewFrame >= Time.frameCount - 1 && _previewSlot >= 0;
            _previewShown = Mathf.MoveTowards(_previewShown, on ? 1f : 0f, Time.deltaTime * 6f);
            if (_previewShown <= 0f)
            {
                if (_ghost != null && _ghost.gameObject.activeSelf) _ghost.gameObject.SetActive(false);
                return;
            }
            if (_ghost == null)
            {
                _ghost = Instantiate(podPrefab, island.transform, false);
                _ghost.name = "Rim Drop Preview";
                foreach (var c in _ghost.GetComponentsInChildren<Collider>(true)) c.enabled = false;
                foreach (var g in _ghost.GetComponentsInChildren<PodGrab>(true)) g.enabled = false;
            }
            if (!_ghost.gameObject.activeSelf) _ghost.gameObject.SetActive(true);
            _ghost.transform.localPosition = HomeLocal(Mathf.Clamp(_previewSlot, 0, Party.Size - 1));
            _ghost.transform.localRotation = Quaternion.identity;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 7f);
            _ghost.BaseScale = Vector3.one * Mathf.SmoothStep(0f, 1f, _previewShown) * (1f + 0.08f * pulse);
            _ghost.Glow = 0.9f + 0.6f * pulse;
        }

        /// <summary>
        /// The socket nearest a world point within <paramref name="maxWorld"/> metres, counting the filled pods and the
        /// next empty socket (a storage pod dropped there joins the party), or -1.
        /// </summary>
        public int NearestSocket(Vector3 world, float maxWorld)
        {
            int best = -1;
            float bestSq = maxWorld * maxWorld;
            int last = Mathf.Min(Party.Size - 1, party.members.Count);
            for (int i = 0; i <= last; i++)
            {
                float d = (HomeWorld(i) - world).sqrMagnitude;
                if (d < bestSq)
                {
                    bestSq = d;
                    best = i;
                }
            }
            return best;
        }

        /// <summary>The cradle pod pops up and glows (the wild creature is in the red).</summary>
        public void PresentCradle()
        {
            _cradlePop = Time.time;
            CradleGlow = true;
            GameAudio.At(AudioIds.CatchAppear, HomeWorld(CradleSlot));
            if (fx != null) fx.Burst(HomeWorld(CradleSlot), 12, new Color(1f, 0.88f, 0.54f), World(0.03f), 0.45f, World(0.003f));
        }

        float World(float local) => local * island.transform.lossyScale.x;

        /// <summary>Resting size of a slot's pod: party pods 1, the cradle pod <see cref="cradleScale"/>.</summary>
        float HomeScale(int slot) => slot == CradleSlot ? cradleScale : 1f;

        /// <summary>Island-local height of a pod's centre above what it sits on (its current radius).</summary>
        public float RestHeight(int slot)
        {
            var p = Pod(slot);
            return podPrefab.radius * (p != null ? p.BaseScale.x : HomeScale(slot));
        }

        // ---- poke, gaze + pinch ----

        void UpdateInput()
        {
            // Gaze cone: the pod nearest the ray among those within gazeRadius pod radii (at least the 4 cm minimum);
            // with a pick handler (the lodge), the next empty socket can be looked at too.
            int previous = _gazed >= 0 ? _gazed : _gazedEmpty >= 0 ? EmptyId + _gazedEmpty : -1;
            _gazed = -1;
            _gazedEmpty = -1;
            if (HeadGaze.TryGetRay(out var ray))
            {
                float min = HeadGaze.MinRadius * island.transform.lossyScale.x;
                var pick = new HeadGaze.ConePick(ray, previous);
                for (int i = 0; i < _pods.Length; i++)
                {
                    var p = _pods[i];
                    if (p == null || p.Detached || !Pointable(i)) continue;
                    pick.Consider(i, p.transform.position, Mathf.Max(min, p.WorldRadius * gazeRadius * 0.5f));
                }
                if (PickHandler != null && !InBattle && party.members.Count < Party.Size)
                {
                    int slot = party.members.Count;
                    pick.Consider(EmptyId + slot, HomeWorld(slot), Mathf.Max(min, podPrefab.radius * island.transform.lossyScale.x * gazeRadius * 0.5f));
                }
                if (pick.Best >= EmptyId) _gazedEmpty = pick.Best - EmptyId;
                else _gazed = pick.Best;
            }
            IsGazed = _gazed >= 0 || _gazedEmpty >= 0 || _externalGaze.Count > 0;
            if (_gazed >= 0) FocusHighlight.Hover(_pods[_gazed].transform.position, _pods[_gazed].WorldRadius * 1.2f);
            UpdateFocus();

            for (int h = 0; h < 2; h++)
            {
                bool left = h == 0;
                var hand = HandRegistry.Get(left ? Handedness.Left : Handedness.Right);
                bool tracked = HandLookup.TryGetJoint(hand, HandJointId.HandIndexTip, out var tip);

                // Poke: a fingertip entering a pod (not while exploring: the pods are inert then).
                int poked = -1;
                if (tracked && !Explore)
                    for (int i = 0; i < _pods.Length; i++)
                    {
                        var p = _pods[i];
                        if (p == null || p.Detached || !Pointable(i)) continue;
                        if ((tip - p.transform.position).sqrMagnitude < Sq(p.WorldRadius * pokeRadius)) poked = i;
                    }
                if (poked >= 0 && poked != _poked[h] && LodgeMode && !InBattle) ShowCard(poked, "poke");
                _poked[h] = poked;

                // Gaze + pinch (not a pinch that grabs a pod).
                bool pinching = tracked && hand.GetIndexFingerIsPinching();
                bool busy = handle != null && handle.IsHandBusy(left);
                int target = _gazed >= 0 ? _gazed : _gazedEmpty;
                if (pinching && !_wasPinching[h] && !busy && target >= 0 && !NearAnyPod(tip, grabPinchDistance))
                {
                    if (!InBattle && PickHandler != null && PickHandler(target)) { }
                    else if (_gazed < 0) { }
                    else if (!InBattle && _gazed == DueSlot && DuePicked != null)
                    {
                        Debug.Log($"[PodRack] Gaze + pinch on the glowing {_pods[_gazed].name}: evolve it.");
                        DuePicked(_gazed);
                    }
                    else if (InBattle)
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

        /// <summary>
        /// Picks the one big party pod: the one held, else the one whose card is showing, else the gazed one, else the
        /// one nearest a reaching hand; it stays big <see cref="shrinkDelay"/> after that ends. Grows it, shrinks the rest
        /// and slides the others aside (dock style) so the big grab volume never overlaps a neighbour's.
        /// </summary>
        void UpdateFocus()
        {
            int focus = _evolvingSlot;
            for (int i = 0; i < Party.Size && focus < 0; i++)
            {
                var p = _pods[i];
                if (p != null && Grab(p) != null && Grab(p).IsHeld) focus = i;
            }
            if (focus < 0 && _card != null && _card.Visible)
                for (int i = 0; i < Party.Size && focus < 0; i++)
                    if (_pods[i] != null && _pods[i].Isling == _card.Showing && !_pods[i].Detached) focus = i;
            if (focus < 0 && _gazed >= 0 && _gazed < Party.Size) focus = _gazed;
            if (focus < 0 && !Explore) focus = NearestToHands();
            // Step 14h: a hand reaching for the glowing pod while exploring grows it (easier to grab).
            if (focus < 0 && Explore && DueSlot >= 0 && NearestToHands() == DueSlot) focus = DueSlot;
            if (focus >= 0)
            {
                if (focus != _bigSlot) Debug.Log($"[PodRack] Pod {focus + 1} grows (gaze, hand or card).");
                _bigSlot = focus;
                _bigUntil = Time.time + shrinkDelay;
            }
            else if (Time.time > _bigUntil) _bigSlot = -1;

            for (int i = 0; i < Party.Size; i++)
            {
                bool big = i == _bigSlot && _pods[i] != null;
                _grow[i] = Mathf.MoveTowards(_grow[i], big ? 1f : 0f, Time.deltaTime / (big ? growSeconds : growSeconds * 1.5f));
            }
            // Centre distance the big grab volume needs from a small neighbour's, beyond the socket step.
            float need = Mathf.Max(0f, _partyGrab * (focusScale + 1f) * 0.5f - _socketStep);
            for (int j = 0; j < Party.Size; j++)
            {
                float slide = 0f;
                for (int k = 0; k < Party.Size; k++)
                    if (k != j && _grow[k] > 0f) slide += Mathf.Sign(_socketPos[j].x - _socketPos[k].x) * need * Mathf.SmoothStep(0f, 1f, _grow[k]);
                _slide[j] = slide;
            }
        }

        /// <summary>The pointable party pod nearest a tracked fingertip or palm, within <see cref="handFocusDistance"/>.</summary>
        int NearestToHands()
        {
            int best = -1;
            float bestSq = handFocusDistance * handFocusDistance;
            for (int h = 0; h < 2; h++)
            {
                var hand = HandRegistry.Get(h == 0 ? Handedness.Left : Handedness.Right);
                foreach (var joint in HandJoints)
                {
                    if (!HandLookup.TryGetJoint(hand, joint, out var j)) continue;
                    for (int i = 0; i < Party.Size; i++)
                    {
                        var p = _pods[i];
                        if (p == null || p.Detached || !Pointable(i)) continue;
                        float d = (p.transform.position - j).sqrMagnitude;
                        if (d < bestSq)
                        {
                            bestSq = d;
                            best = i;
                        }
                    }
                }
            }
            return best;
        }

        static readonly HandJointId[] HandJoints = { HandJointId.HandIndexTip, HandJointId.HandPalm };

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
            if (_card == null || p == null || p.Isling == null || MenuUp) return;
            if (_card.Visible && _card.Showing == p.Isling && how == "gaze + pinch")
            {
                _card.Hide();
                return;
            }
            // Follows its pod, its lower edge above the pod (the old fixed spot hid the pod and stayed put when the island moved).
            _card.Show(p.Isling, () => p != null ? p.transform.position + island.transform.up * (p.WorldRadius + PodCard.HalfHeight + 0.012f) +
                                              island.transform.forward * World(0.02f) : (Vector3?)null, cardSeconds);
            _cardIslandPos = island.transform.position;
            _cardIslandRot = island.transform.rotation;
            Debug.Log($"[PodRack] Card for {p.Isling.Name} ({how}).");
        }

        // ---- rim guard ----

        void UpdateRimGuard()
        {
            var rim = handle != null ? handle.rimInteractable : null;
            if (rim == null) return;
            bool reaching = false;
            // Step 8e: only while pods can be grabbed (battle, lodge); exploring, the strip in front of them is grab zone.
            if ((InBattle && (PartyGrab || CradleGrab) || LodgeMode && !InBattle || Explore && DueSlot >= 0) && rim.SelectingInteractors.Count == 0)
                for (int h = 0; h < 2 && !reaching; h++)
                {
                    var hand = HandRegistry.Get(h == 0 ? Handedness.Left : Handedness.Right);
                    foreach (var joint in new[] { HandJointId.HandIndexTip, HandJointId.HandPalm })
                    {
                        if (!HandLookup.TryGetJoint(hand, joint, out var p)) continue;
                        for (int i = 0; i < _pods.Length && !reaching; i++)
                        {
                            // At least the grab volume plus a little: the hand must not reach it before the guard pauses the rim.
                            float guard = Mathf.Max(rimGuardDistance, World((i == CradleSlot ? cradleGrabSize : _partyGrab) * 0.5f + 0.03f));
                            if (_pods[i] != null && CanUse(i) && (_pods[i].transform.position - p).sqrMagnitude < Sq(guard)) reaching = true;
                        }
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

        /// <summary>
        /// Flies the pod back to its socket or the cradle and hands it back to the rack, growing or shrinking to that
        /// slot's size on the way (a caught cradle pod shrinks to party size).
        /// </summary>
        public IEnumerator FlyHome(int slot, float seconds = 0.45f)
        {
            var p = _pods[slot];
            if (p == null) yield break;
            p.Detached = true;
            var t = p.transform;
            Vector3 from = t.localPosition, s0 = p.BaseScale, s1 = Vector3.one * HomeScale(slot);
            Vector3 control = (from + HomeLocal(slot)) * 0.5f + Vector3.up * 0.05f;
            Quaternion r0 = t.localRotation;
            for (float u = 0f; u < 1f; u += Time.deltaTime / seconds)
            {
                if (t == null) yield break;
                float k = Mathf.SmoothStep(0f, 1f, u);
                t.localPosition = Bezier(from, control, HomeLocal(slot), k);
                t.localRotation = Quaternion.Slerp(r0, Quaternion.identity, u);
                p.BaseScale = Vector3.Lerp(s0, s1, k);
                yield return null;
            }
            if (p == null) yield break;
            p.BaseScale = s1;
            p.Detached = false;
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
            GameAudio.At(AudioIds.CatchWobble, t.position);
            for (float u = 0f; u < 1f; u += Time.deltaTime / 0.45f)
            {
                if (t == null) yield break;
                t.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(u * Mathf.PI * 2f) * 28f * (1f - u * 0.4f));
                yield return null;
            }
            if (t != null) t.localRotation = Quaternion.identity;
        }

        /// <summary>Removes a (detached) party pod at once; the rack makes pods again for whatever the slots hold now.</summary>
        public void Discard(int slot)
        {
            var p = Pod(slot);
            if (p == null) return;
            Destroy(p.gameObject);
            _pods[slot] = null;
        }

        /// <summary>The cradle pod now holds a party creature: it becomes pod <paramref name="slot"/> (a new empty pod fills the cradle).</summary>
        public void AdoptCradlePod(int slot, IslingInstance creature)
        {
            var p = _pods[CradleSlot];
            _pods[CradleSlot] = null;
            if (p == null || slot < 0 || slot >= Party.Size) return;
            if (_pods[slot] != null) Destroy(_pods[slot].gameObject);
            _pods[slot] = p;
            p.name = $"Pod {slot + 1}";
            p.Show(creature); // keeps its cradle size: FlyHome shrinks it on the way to the socket
        }

        /// <summary>
        /// The pod breaks (the creature got out): white cracks race over the glass, then it shatters into glass-tinted
        /// shards with a puff in the creature's element colour, and the brass band drops and fades. A fresh pod fills
        /// the cradle <see cref="cradleRefillSeconds"/> later.
        /// </summary>
        public IEnumerator Shatter(int slot, Color element)
        {
            var p = _pods[slot];
            if (p == null) yield break;
            p.Detached = true;
            var t = p.transform;
            GameAudio.At(AudioIds.CatchShatter, t.position);
            Quaternion r0 = t.localRotation;
            for (float u = 0f; u < 1f; u += Time.deltaTime / 0.3f)
            {
                if (p == null) yield break;
                p.Shatter = u;
                t.localRotation = r0 * Quaternion.Euler(0f, 0f, Mathf.Sin(u * 70f) * 5f * u);
                yield return null;
            }
            if (p == null) yield break;
            Vector3 at = t.position;
            if (fx != null)
            {
                fx.Burst(at, 28, new Color(0.82f, 0.94f, 1f), World(0.08f), 0.55f, World(0.0045f), Vector3.up * World(0.02f), World(0.4f));
                fx.Burst(at, 8, Color.white, World(0.05f), 0.25f, World(0.006f));
                fx.Burst(at, 12, element, World(0.025f), 0.6f, World(0.008f), Vector3.up * World(0.015f));
            }
            StartCoroutine(DropBand(p.DetachBand()));
            _pods[slot] = null;
            Destroy(p.gameObject);
            if (slot == CradleSlot) _cradleRefillAt = Time.time + cradleRefillSeconds;
            Debug.Log($"[PodRack] {p.name} shattered.");
        }

        /// <summary>A broken pod's brass band falls, tumbling, and shrinks away.</summary>
        IEnumerator DropBand(Transform band)
        {
            if (band == null) yield break;
            Vector3 v = new Vector3(UnityEngine.Random.Range(-0.08f, 0.08f), 0.15f, UnityEngine.Random.Range(-0.08f, 0.08f));
            Vector3 spin = UnityEngine.Random.onUnitSphere * 500f, s0 = band.localScale;
            const float seconds = 0.7f;
            for (float u = 0f; u < seconds; u += Time.deltaTime)
            {
                if (band == null) yield break;
                v += Vector3.down * (1.5f * Time.deltaTime);
                band.localPosition += v * Time.deltaTime;
                band.Rotate(spin * Time.deltaTime, Space.Self);
                band.localScale = s0 * (1f - Mathf.SmoothStep(0f, 1f, u / seconds));
                yield return null;
            }
            if (band != null) Destroy(band.gameObject);
        }

        /// <summary>Party full: the pod shrinks into a light that streaks off the island.</summary>
        public IEnumerator StreakAway(int slot)
        {
            var p = _pods[slot];
            if (p == null) yield break;
            _pods[slot] = null;
            p.Detached = true;
            var t = p.transform;
            GameAudio.On(AudioIds.CatchToStorage, t);
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
