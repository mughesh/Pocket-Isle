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
    /// Step 13: a shard locking in makes the statue whisper a memory in the Heart's voice (<see cref="FinaleLogic.WhisperLines"/>,
    /// its own bubble rising from the statue's head: <see cref="HeartVoice"/>), then the Spirit answers. The statue wakes a
    /// little with each: one placed = its closed eyes glow faintly (<see cref="statueLitRecipe"/>), two = it breathes
    /// slowly and its moss sways (<see cref="statueAwakeRecipe"/>), three = the finale (<see cref="FinaleDirector"/>, which
    /// hides it with <see cref="HideStatue"/> once the Heart stands there).
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

        [Header("Step 13: the statue wakes (drawing socket only)")]
        [Tooltip("One shard placed: the closed eyes glow faintly. Empty = Statue Recipe.")]
        public PropRecipe statueLitRecipe;
        [Tooltip("Two placed: the moss sways (and the statue breathes). Empty = the lit one.")]
        public PropRecipe statueAwakeRecipe;
        [Tooltip("Breathing with two placed: height pulse (share) and seconds per breath.")]
        public float breathe = 0.014f, breathSeconds = 4.5f;
        [Tooltip("The Heart's voice: its bubble rises from this high above the statue's base (tiles).")]
        public float voiceHeight = 2.6f;

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
        [Tooltip("Step 11c: said instead when this makes all three shards placed (the finale comes later).")]
        public string threeLine = "statue.three";

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
        bool _flying, _quiet;
        Vector3 _shardWorld, _grabOffset;
        float _pulse;
        bool _statueHidden;

        static readonly List<StatueSocket> All = new List<StatueSocket>();

        /// <summary>Step 13: the finale's Heart stands in the statue's place (the statue isn't drawn; the shards ride the Heart).</summary>
        public static bool HideStatue;
        /// <summary>Step 13: the statue shakes this much while it wakes (tiles).</summary>
        public static float Shake;
        /// <summary>Step 13: a shard locked in (the socket, shards placed now).</summary>
        public static event System.Action<StatueSocket, int> Placed;
        /// <summary>Step 13: where the Heart's voice comes from (above the statue's head; the Heart moves it to its own).</summary>
        public static Transform HeartVoice { get; private set; }

        /// <summary>Step 13: the statue's middle on the map (column, row) and its turn (the Heart stands there too).</summary>
        public Vector2 Center => StatueCenter;
        public float Yaw => statueYaw;

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

        protected override bool GhostAside => true;
        public override bool Busy => _hand >= 0 || _flying;
        /// <summary>Step 16: the offer (lesson, Spirit's offer, focus lock) only starts next to the statue (walk-up rule).</summary>
        protected override bool Available => ShardHolder.Holds(id) && NearStatue;
        /// <summary>Step 16: the view stays on the statue while it whispers (the explorer is locked then).</summary>
        public override bool KeepViewWhileLocked => _scene;

        [Header("Step 16")]
        [Tooltip("The offer starts with the explorer this many tiles from the statue's footprint (1 = next to it).")]
        [Min(1)] public int offerDistance = 1;
        [Tooltip("Released this close to the socket (world m) it snaps in (forgiving; the old snapRadius stays as a minimum).")]
        public float forgivingSnap = 0.12f;
        [Tooltip("The explorer stays locked at most this long while the statue whispers and the Spirit answers (s).")]
        public float sceneLockSeconds = 30f;

        bool _scene;

        /// <summary>The statue's tiles (its prop's footprint on the map; 3 x 4 if the map has no prop there).</summary>
        RectInt StatueRect
        {
            get
            {
                var size = new Vector2Int(3, 4);
                if (map != null && map.TryGetProp(map.PropAt(statueAnchor.y, statueAnchor.x), out var e)) size = WorldMap.FootprintOf(e);
                return new RectInt(statueAnchor, size);
            }
        }

        bool NearStatue => explorer != null && WalkUp.Distance(explorer.Tile, StatueRect) <= offerDistance;
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
            if (drawsStatue && HeartVoice == null)
            {
                HeartVoice = new GameObject("Heart Voice").transform;
                HeartVoice.SetParent(island.transform, false);
                Dialogue.RegisterSpeaker(DialogueSpeaker.Heart, HeartVoice);
            }
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
            if (drawsStatue && HeartVoice != null)
            {
                Destroy(HeartVoice.gameObject);
                HeartVoice = null;
            }
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
            bool hide = PlacedCount > 0 || HideStatue;
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
            bool near = NearStatue;
            if (holder != null) holder.SetOffering(id, here && !IsSolved && held && Usable && near);
            if (_hand >= 0) Hold();
            else if (!_flying && !IsSolved && held && Usable && near) TryGrab();
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
                bool near = (_shardWorld - SocketWorld).magnitude <= Mathf.Max(snapRadius, forgivingSnap) * Mathf.Max(1f, island.transform.lossyScale.x);
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
            else if (_scene) EndScene("the shard flew back");
        }

        IEnumerator LockIn()
        {
            if (!_quiet) BeginScene();
            MarkSolved(2); // saved: placed (shards placed 1 / 3)
            _pulse = 1f;
            float tile = TileWorld;
            GameAudio.At(Diorama.Audio.AudioIds.StatueSocket, SocketWorld);
            if (!_quiet)
            {
                GameAudio.Ui(Diorama.Audio.AudioIds.JingleShard);
                Diorama.Audio.MusicDirector.Jingle(3f);
            }
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
            if (_quiet)
            {
                // Nov 3: the Quick Journey's dream ahead flies the other shards in without their whispers.
                _quiet = false;
                Placed?.Invoke(this, placed);
                yield break;
            }
            // Step 13: the statue whispers a memory (the Heart's voice), the Spirit answers; the third starts the finale.
            var lines = FinaleLogic.WhisperLines(id, placed);
            foreach (var line in lines) Dialogue.Say(line);
            Debug.Log($"[StatueSocket] Whisper: {string.Join(", ", lines)}.");
            Placed?.Invoke(this, placed);
            // Step 16: the explorer waits while the statue whispers and the Spirit answers (no walking into the lodge mid-scene).
            yield return new WaitForSeconds(0.5f);
            for (float t = 0f; t < sceneLockSeconds; t += Time.deltaTime)
            {
                bool pending = Dialogue.IsTalking;
                foreach (var l in lines) pending |= Dialogue.IsPending(l);
                if (!pending || FinaleDirector.Running) break;
                yield return null;
            }
            EndScene(FinaleDirector.Running ? "the finale takes over" : "the whisper is over");
        }

        void BeginScene()
        {
            _scene = true;
            if (explorer != null) explorer.Locked = true;
            Debug.Log("[StatueSocket] Shard placed: the explorer waits while the statue whispers; the view stays on the statue.");
        }

        void EndScene(string why)
        {
            if (!_scene) return;
            _scene = false;
            // The finale locks the explorer itself; leave it to it.
            if (explorer != null && !FinaleDirector.Running) explorer.Locked = false;
            Debug.Log($"[StatueSocket] Scene over ({why}): the explorer can walk again.");
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
                // Step 13: it wakes a little with each shard; it breathes with two; it shakes while the finale wakes it.
                int stage = FinaleLogic.StatueStage(PlacedCount);
                var look = stage >= 2 && statueAwakeRecipe != null ? statueAwakeRecipe : stage >= 1 && statueLitRecipe != null ? statueLitRecipe : statueRecipe;
                if (look != null) _statue.SetRecipe(look);
                float breath = stage >= 2 ? 1f + breathe * Mathf.Sin(Time.time * Mathf.PI * 2f / Mathf.Max(0.5f, breathSeconds)) : 1f;
                Vector2 shake = Shake > 0f ? new Vector2(Mathf.Sin(Time.time * 47f), Mathf.Sin(Time.time * 39f + 1f)) * Shake : Vector2.zero;
                _statue.Place(StatueCenter + shake, 0f, rot, here && PlacedCount > 0 && !HideStatue, new Vector3(1f, breath, 1f));
            }
            if (drawsStatue && HeartVoice != null && island.Content != null)
                HeartVoice.position = MapToWorld(new Vector3(StatueCenter.x, voiceHeight, StatueCenter.y));
            if (_shard != null)
            {
                bool show = here && (_hand >= 0 || _flying || solved && !HideStatue);
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            HideStatue = false;
            Shake = 0f;
            Placed = null;
            HeartVoice = null;
        }

        /// <summary>
        /// Nov 3 (the Quick Journey's See the ending): the held shard flies from the Spirit into its socket by itself, no
        /// jingle, no whisper. False when it can't (not in the hub, not held).
        /// </summary>
        public bool FlyInQuiet()
        {
            if (!Here || IsSolved || !ShardHolder.Holds(id) || holder == null || _flying) return false;
            _quiet = true;
            StartCoroutine(Fly(holder.CupWorld(id), true));
            return true;
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
