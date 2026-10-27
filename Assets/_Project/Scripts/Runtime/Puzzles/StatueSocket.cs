using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// The hub statue's fire socket (step 9c), a Carry-style hand puzzle: while the fire shard is held
    /// (<see cref="ShardHolder"/>, it rides with the Spirit), near the statue the Spirit offers it (it floats out in front
    /// of the player, big), the red socket glows and the Spirit points at it the first time (<see cref="HandPuzzle.lessonLine"/>,
    /// ghost: grab the shard). Grab the shard (pinch or fist near it): it follows the hand and shrinks to socket size as
    /// it nears the socket; let go near the socket and it locks in, anywhere else it flies back to the offer spot.
    /// Look + pinch the shard: it floats into the socket by itself. Locked in: a light pulse, the statue's red rims and
    /// cracks glow, moss falls away, the Spirit feels warmer ("shard.fire" solved, saved). The island stops drawing the
    /// statue then and this draws it (glowing) with the shard in its socket.
    /// Step 10e: one socket per shard (<see cref="HandPuzzle.id"/> = the shard's id: fire, wind). The statue is drawn
    /// once, by the socket with <see cref="drawsStatue"/>, with every placed socket's rim lit.
    /// </summary>
    [DefaultExecutionOrder(-20)]
    public class StatueSocket : HandPuzzle
    {
        public ShardHolder holder;
        public PropRecipe shardRecipe;
        public PropRecipe statueRecipe;
        [Tooltip("The statue prop's anchor on the hub map (top-left of its 3 x 3).")]
        public Vector2Int statueAnchor;
        public float statueYaw;
        [Tooltip("The fire socket in the statue's shape units (PropKit.StatueSockets[0]).")]
        public Vector3 socket = new Vector3(0.24f, 1.03f, 0.52f);
        [Tooltip("Diorama/Beacon material: the socket's glow ring.")]
        public Material ringMaterial;
        [Tooltip("Step 10e: this socket's rim on the statue prop (fire = Clay, water = Wood, wind = Accent): lit once placed.")]
        public PropSlot rimSlot = PropSlot.Clay;
        [Tooltip("Sparks when it locks in.")]
        public Color shardColor = new Color(1f, 0.6f, 0.3f);
        [Tooltip("Step 10e: this socket draws the lit statue (one per statue; the others add their shard and rim).")]
        public bool drawsStatue = true;
        [Tooltip("Dialogue target name (one per socket).")]
        public string targetName = "statueSocket";

        [Header("Grab")]
        [Tooltip("World metres round the shard.")]
        public float grabRadius = 0.07f;
        [Tooltip("Released this close to the socket (world metres): it locks in.")]
        public float snapRadius = 0.06f;

        [Header("Lines")]
        public string placedLine = "hub.shard.placed";
        public string moreLine = "hub.shard.more";
        [Tooltip("Step 10e: said instead of the more line when this makes two shards placed.")]
        public string twoLine = "statue.two";

        static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        static readonly int FillId = Shader.PropertyToID("_Fill");
        static readonly int ModeId = Shader.PropertyToID("_Mode");

        readonly HandState[] _hands = new HandState[2];
        PropView _shard, _statue;
        Transform _ring;
        MeshRenderer _ringRenderer;
        MaterialPropertyBlock _block;
        Mesh _quad;
        int _hand = -1;
        bool _flying;
        Vector3 _shardWorld, _grabOffset;
        float _pulse;
        bool _statueHidden;

        static readonly List<StatueSocket> All = new List<StatueSocket>();

        /// <summary>The scene's socket for shard <paramref name="id"/>, inactive ones too (editor tools, the bridge).</summary>
        public static StatueSocket FindById(string id)
        {
            foreach (var s in FindObjectsByType<StatueSocket>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (s.id == id) return s;
            return null;
        }

        /// <summary>Shards placed in the statue (every socket in the scene).</summary>
        public static int PlacedCount
        {
            get
            {
                int n = 0;
                foreach (var s in All)
                    if (s != null && s.IsSolved) n++;
                return n;
            }
        }

        public override bool Busy => _hand >= 0 || _flying;
        protected override bool Available => ShardHolder.Holds(id);
        protected override Vector2 HomeTile => statueAnchor + Vector2.one;

        public override Vector3? FocusPoint => Here && ShardHolder.Holds(id) && holder != null ? holder.CupWorld(id) : (Vector3?)null;
        public override float FocusRadius => 0.045f * island.transform.lossyScale.x;

        Vector2 StatueCenter => statueAnchor + Vector2.one;

        /// <summary>Map (column, height in tiles, row) of the socket.</summary>
        Vector3 SocketMap
        {
            get
            {
                var s = Quaternion.Euler(0f, statueYaw, 0f) * socket;
                return new Vector3(StatueCenter.x - s.x, s.y, StatueCenter.y + s.z);
            }
        }

        public Vector3 SocketWorld => MapToWorld(SocketMap);

        /// <summary>The statue's line points at the socket for the shard the Spirit carries (the wind one if only that is held).</summary>
        Vector3 LessonSocketWorld
        {
            get
            {
                if (drawsStatue && !ShardHolder.Holds(id))
                    foreach (var s in All)
                        if (s != null && s != this && ShardHolder.Holds(s.id)) return s.SocketWorld;
                return SocketWorld;
            }
        }

        protected override void Start()
        {
            _block = new MaterialPropertyBlock();
            All.Add(this);
            if (shardRecipe != null) _shard = new PropView($"{id} (statue)", island, shardRecipe) { restoredSelf = 1f };
            if (statueRecipe != null && drawsStatue) _statue = new PropView("Statue (lit)", island, statueRecipe);
            if (ringMaterial != null)
            {
                _quad = BattleHud.VerticalQuad();
                var go = new GameObject("Statue Socket Glow");
                go.layer = island.gameObject.layer;
                go.transform.SetParent(island.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = _quad;
                _ringRenderer = go.AddComponent<MeshRenderer>();
                _ringRenderer.sharedMaterial = ringMaterial;
                _ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _ring = go.transform;
                go.SetActive(false);
            }
            Dialogue.RegisterTarget(targetName, () => Here ? LessonSocketWorld : (Vector3?)null, k => { }, () => 0.5f * TileWorld);
            base.Start();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            All.Remove(this);
            _shard?.Destroy();
            _statue?.Destroy();
            if (_ring != null) Destroy(_ring.gameObject);
            if (_quad != null) Destroy(_quad);
            if (map != null && drawsStatue) map.SetPropHidden(statueAnchor, false);
        }

        protected override void ApplyState()
        {
            _hand = -1;
            _flying = false;
            if (holder != null) holder.SetOut(id, false);
            SyncStatue(true);
            SetProgressQuiet(IsSolved ? 1f : 0f);
        }

        /// <summary>Any shard placed: the island stops drawing the statue; the drawing socket draws it lit, shards in.</summary>
        void SyncStatue(bool force)
        {
            if (!drawsStatue || map == null) return;
            bool hide = PlacedCount > 0;
            if (!force && hide == _statueHidden) return;
            _statueHidden = hide;
            map.SetPropHidden(statueAnchor, hide);
        }

        protected override void Update()
        {
            ReadHands(_hands);
            bool here = Here;
            SyncStatue(false);
            bool held = ShardHolder.Holds(id);
            if (holder != null) holder.SetOffering(id, here && !IsSolved && held && Usable);
            if (_hand >= 0) Hold();
            else if (!_flying && !IsSolved && held && Usable) TryGrab();
            if (_hand >= 0) IslandHandle.MarkHandHeld(_hand == 0);
            Draw(here);
            base.Update();
        }

        void TryGrab()
        {
            if (holder == null) return;
            Vector3 cup = holder.CupWorld(id);
            for (int h = 0; h < 2; h++)
            {
                var s = _hands[h];
                if (s.busy || !(s.pinchStarted || s.fistStarted)) continue;
                var grip = s.pinchStarted ? s.pinchPoint : s.palm;
                if ((grip - cup).magnitude > grabRadius * Mathf.Max(1f, island.transform.lossyScale.x)) continue;
                _hand = h;
                _shardWorld = cup;
                _grabOffset = cup - grip;
                holder.SetOut(id, true);
                MarkInteracted();
                Debug.Log($"[StatueSocket] {id} taken from the holder ({(s.left ? "left" : "right")} {(s.pinchStarted ? "pinch" : "palm")}).");
                return;
            }
            // Look + pinch on the shard in its cup: it floats into the socket.
            if (HeadGaze.TryGetRay(out var ray))
            {
                var pick = new HeadGaze.ConePick(ray, -1);
                pick.Consider(0, cup, Mathf.Max(HeadGaze.MinRadius * island.transform.lossyScale.x, 0.05f * island.transform.lossyScale.x));
                if (pick.Best == 0)
                {
                    FocusHighlight.Hover(cup, 0.045f * island.transform.lossyScale.x);
                    if (GazePinch(_hands))
                    {
                        MarkInteracted();
                        Debug.Log($"[StatueSocket] Look + pinch on {id}: it floats to the socket.");
                        StartCoroutine(Fly(cup, true));
                    }
                }
            }
        }

        void Hold()
        {
            var s = _hands[_hand];
            if (!s.tracked || !s.Holding)
            {
                _hand = -1;
                bool near = (_shardWorld - SocketWorld).magnitude <= snapRadius * Mathf.Max(1f, island.transform.lossyScale.x);
                Debug.Log($"[StatueSocket] Released {(near ? "at the socket: it locks in" : "away from the socket: back to the Spirit's offer spot")}.");
                StartCoroutine(Fly(_shardWorld, near));
                return;
            }
            _shardWorld = Vector3.Lerp(_shardWorld, s.GripPoint + _grabOffset * 0.3f, 1f - Mathf.Exp(-Time.deltaTime / 0.05f));
        }

        /// <summary>Flies the shard to the socket (locks in) or back to the offer spot.</summary>
        IEnumerator Fly(Vector3 from, bool toSocket)
        {
            _flying = true;
            if (holder != null) holder.SetOut(id, true);
            float seconds = toSocket ? 0.7f : 0.5f;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / seconds);
                Vector3 to = toSocket ? SocketWorld : holder != null ? holder.CupWorld(id) : from;
                _shardWorld = Vector3.Lerp(from, to, k) + island.transform.up * (0.03f * Mathf.Sin(k * Mathf.PI));
                yield return null;
            }
            _flying = false;
            if (holder != null) holder.SetOut(id, false);
            if (toSocket) StartCoroutine(LockIn());
        }

        IEnumerator LockIn()
        {
            MarkSolved(2); // saved: placed (shards placed 1 / 3)
            _pulse = 1f;
            float tile = TileWorld;
            if (fx != null)
            {
                fx.Burst(SocketWorld, 40, shardColor, tile * 1.2f, 0.8f, tile * 0.05f);
                // Moss falls away from the shoulders and head.
                foreach (var m in new[] { new Vector3(0f, 1.52f, -0.06f), new Vector3(0f, 2.3f, 0.12f), new Vector3(0.45f, 0.72f, -0.3f) })
                {
                    var s = Quaternion.Euler(0f, statueYaw, 0f) * m;
                    fx.Burst(MapToWorld(new Vector3(StatueCenter.x - s.x, s.y, StatueCenter.y + s.z)), 18, new Color(0.45f, 0.6f, 0.32f), tile * 0.5f, 1.2f, tile * 0.05f, Vector3.zero, tile * 3f);
                }
            }
            int placed = PlacedCount;
            Debug.Log($"[StatueSocket] {id} locks into the statue: {placed} / 3 placed.");
            yield return new WaitForSeconds(0.8f);
            if (!string.IsNullOrEmpty(placedLine)) Dialogue.Say(placedLine);
            string next = placed == 2 && !string.IsNullOrEmpty(twoLine) ? twoLine : moreLine;
            if (!string.IsNullOrEmpty(next)) Dialogue.Say(next);
        }

        void Draw(bool here)
        {
            _pulse = Mathf.MoveTowards(_pulse, 0f, Time.deltaTime * 0.6f);
            bool solved = IsSolved;
            var rot = Quaternion.Euler(0f, statueYaw, 0f);
            if (_statue != null)
            {
                _statue.restoredSelf = -1f;
                // Each placed socket's rim and the cracks glow (the cracks are the Glow slot), pulsing with the newest.
                var a = Vector4.zero;
                var b = Vector4.zero;
                float pulse = 0f;
                foreach (var s in All)
                {
                    if (s == null || !s.IsSolved) continue;
                    pulse = Mathf.Max(pulse, s._pulse);
                    float rim = 1.1f + 1.5f * s._pulse;
                    int slot = (int)s.rimSlot;
                    if (slot < 4) a[slot] += rim;
                    else b[slot - 4] += rim;
                }
                b.x += 0.6f + 0.8f * pulse;
                _statue.glowBoostA = a;
                _statue.glowBoostB = b;
                _statue.Place(StatueCenter, 0f, rot, here && PlacedCount > 0);
            }
            if (_shard != null)
            {
                bool show = here && (_hand >= 0 || _flying || solved);
                Vector3 world = solved && !_flying ? SocketWorld : _shardWorld;
                // Big in the hand, socket size as it nears the socket.
                float big = holder != null ? holder.offerMetres : 0.026f * 0.8f;
                float near = Mathf.Clamp01((world - SocketWorld).magnitude / (0.15f * Mathf.Max(1f, island.transform.lossyScale.x)));
                float metres = solved && !_flying ? 0.026f * 0.8f : Mathf.Lerp(0.026f * 0.8f, big, near);
                if (show) _shard.PlaceLocal(island.transform.InverseTransformPoint(world), Quaternion.Euler(0f, Time.time * 50f, 0f), metres, true);
                else _shard.Hide();
            }
            if (_ring != null)
            {
                bool glow = here && !solved && ShardHolder.Holds(id) && Usable;
                _ring.gameObject.SetActive(glow);
                if (glow)
                {
                    var cam = Camera.main;
                    _ring.position = SocketWorld + island.transform.up * 0.001f;
                    if (cam != null) _ring.rotation = Quaternion.LookRotation(_ring.position - cam.transform.position, island.transform.up);
                    _ring.localScale = Vector3.one * (0.5f * island.map.tileSize * island.Zoom);
                    _block.Clear();
                    _block.SetFloat(ModeId, 1f);
                    _block.SetFloat(FillId, 1f);
                    _block.SetFloat(IntensityId, 1.4f + 0.6f * Mathf.Sin(Time.time * 3f) + (_hand >= 0 ? 0.8f : 0f));
                    _ringRenderer.SetPropertyBlock(_block);
                }
            }
        }

        /// <summary>Debug (bridge): the shard goes into the socket as if carried there.</summary>
        public void DebugPlace()
        {
            if (!Here || IsSolved || !ShardHolder.Holds(id) || holder == null) return;
            MarkInteracted();
            StartCoroutine(Fly(holder.CupWorld(id), true));
        }
    }
}
