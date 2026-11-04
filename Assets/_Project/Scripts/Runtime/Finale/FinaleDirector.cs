using System.Collections;
using System.Collections.Generic;
using Diorama.Audio;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 13 C: the finale, on the hub at the game's normal scale, when the third shard locks into the statue (any realm
    /// order). Walking, wild battles and the menu's Title wait (<see cref="Running"/>); Settings and Resume stay. About
    /// 2.5-3 minutes:
    /// 1. Wake: the sockets flare, light runs up the statue, stone flakes and moss fall, a burst of dust and gold and the
    ///    statue is the Heart (<see cref="HeartOfWorld"/>); it breathes, lifts its head: heart.wake.
    /// 2. Understanding: the Spirit floats before the Heart's empty forehead: spirit.finale.1.
    /// 3. The goodbye in your palm (the player acts): spirit.finale.2 pinned, chip "Hold out your hand". An open hand palm up:
    ///    the Spirit lands on it, glowing (chip "Lift it to the Heart"); within ~6 cm of the forehead it floats off into
    ///    the socket (spirit.finale.3, a chime). Look + pinch the Heart: it goes by itself. 30 s idle: spirit.finale.wait
    ///    (<see cref="PalmHandoff"/>).
    /// 4. The Heart opens its eyes: gold eyes, wings unfold, the crown blazes; the gates bloom (fire, water, wind), the hub's
    ///    grass waves, warm motes rise into the room, a golden ring of light under the island: heart.thanks.
    /// 5. Hall of Fame: heart.friends; the party comes out of the rim pods one by one onto the path in front of the Heart,
    ///    each with a stone card (name · Lv · met in); a few Lodge Islings wander out; everyone cheers. Saved (GameSave).
    /// 6. Credits: stone tablets rise one at a time over the island's far side, then sink.
    /// 7. The spark returns: a tiny spark slips from the Heart's eye into the explorer's lantern and hops out as the
    ///    guide, the Spark (<see cref="LanternSpirit.SetSpark"/>): spark.hello. Free play (<see cref="FinaleLogic.DoneId"/>).
    /// Progress is saved as the puzzle <see cref="FinaleLogic.StageId"/>; quitting in the middle restarts at the goodbye.
    /// </summary>
    public class FinaleDirector : MonoBehaviour
    {
        public IslandBuilder island;
        public WorldWindow window;
        public GestureTestPiece explorer;
        public HeartOfWorld heart;
        public LanternSpirit spirit;
        public Party party;
        public GameSave save;
        public EncounterDirector encounters;
        public PodRack pods;
        public BattleFx fx;
        public GhostHands ghosts;
        public IslandHandle grabGuard;
        [Tooltip("The HUD material (cards, credits tablets).")]
        public Material hudMaterial;
        [Tooltip("Diorama/Beacon: the golden ring of light under the island.")]
        public Material ringMaterial;
        [Tooltip("The hub (the finale only plays there) and the three realms (rosters, where an old save's Islings were met).")]
        public WorldMap hub;
        public WorldMap[] realms;
        [Tooltip("The starters (and what they evolve into) were met on the Still Isle (old saves without a \"met in\").")]
        public IslingSpecies[] starters;

        [Header("Feel")]
        public PalmHandoff handoff = new PalmHandoff();
        [Tooltip("The window frames the Heart this far in front of it (tiles) and zooms in this much.")]
        public float frameAhead = 1.2f, zoom = 1.15f;
        [Tooltip("The Spirit waits this far in front of the forehead (tiles).")]
        public float waitInFront = 2.2f;
        [Tooltip("In the socket: its size (share of its own) and how far its feet sit below the socket's middle (tiles), so its body fills the setting.")]
        public float inSocketSize = 0.42f, inSocketDrop = 0.32f;
        [Tooltip("Step 14f: the lantern dissolving into a gold mote (s), the mote's size at its biggest (tiles).")]
        public float dissolveSeconds = 0.3f, moteSize = 0.35f;
        const float FlightSeconds = 1.4f;
        [Tooltip("Ghost hand for the goodbye (an open hand palm up).")]
        public string ghostClip = "palm_carry";
        [Tooltip("Hall of Fame: seconds between Islings coming out; Lodge Islings that wander out too.")]
        public float hallGap = 2.6f;
        [Min(0)] public int lodgeGuests = 2;
        [Tooltip("Hall of Fame cards float this high over their Isling (tiles), every other one higher so they don't overlap.")]
        public Vector2 cardHeight = new Vector2(1.8f, 2.9f);
        [Tooltip("Credits: each tablet's seconds (rise, read, sink), and how high it rises over the far rim (island metres, from / to).")]
        public float tabletSeconds = 5.2f;
        public Vector2 tabletRise = new Vector2(0.1f, 0.27f);
        [Header("Post-game (step 13 D)")]
        [Tooltip("The Spar? / Not now runes for the Keeper rematch (the Quick Journey's end board).")]
        public RuneBoard runes;
        [Tooltip("The collection card stays this long after a poke (s).")]
        public float collectionSeconds = 7f;
        [Tooltip("The Spark's hint comes this long after arriving in a realm with Islings still to meet (s), once a session.")]
        public float hintAfter = 15f;
        [TextArea(1, 3)] public string[] credits =
        {
            "Pocket Isle", "Made by Mugesh\n(Heisenberg: The XR Guy)", "Islings, realms and Keepers:\noriginal designs",
            "Built with Unity\nand the Meta XR SDK", "Sounds: ElevenLabs\nand synthesised", "Music: to come", "Thank you for playing.",
        };

        /// <summary>The finale is playing (walking, battles and the menu's Title wait).</summary>
        public static bool Running { get; private set; }
        public static FinaleDirector Instance { get; private set; }
        /// <summary>Debug (bridge shots): a moment of the finale ("palm.land", "socket", "spark.out", "done").</summary>
        public static event System.Action<string> Moment;
        public FinaleStep Step { get; private set; }
        public PalmHandoff.State Palm => handoff.Now;

        /// <summary>Debug (bridge): a synthetic palm (map column, height in tiles, row) and how much it faces up.</summary>
        [System.NonSerialized] public Vector3? debugPalm;
        [System.NonSerialized] public float debugPalmUp = 1f;
        bool _debugGaze;

        static readonly Color Gold = new Color(1f, 0.82f, 0.4f);
        static readonly Color FireC = new Color(1f, 0.5f, 0.2f), WaterC = new Color(0.4f, 0.75f, 1f), WindC = new Color(0.6f, 0.95f, 0.5f);

        readonly List<MapCreature> _guests = new List<MapCreature>();
        readonly List<StoneTablet> _cards = new List<StoneTablet>();
        readonly List<(StoneTablet card, MapCreature who)> _labels = new List<(StoneTablet, MapCreature)>();
        readonly List<float> _labelHeight = new List<float>();
        Vector3 _hold;
        bool _holding;
        float _readySince = -1f;
        Transform _ring;
        MeshRenderer _ringRenderer;
        MaterialPropertyBlock _block;
        float _ringAlpha;
        BattleFx.Item _halo, _shine, _pulse;
        float _pulseAt = -1f, _twinkleAt;
        readonly bool[] _wasPinching = { true, true };

        float Unit => heart != null ? heart.UnitWorld : 0.05f;
        float IslandScale => island != null ? island.transform.lossyScale.x : 1f;
        Vector2 Center => heart != null && heart.statue != null ? heart.statue.Center : Vector2.zero;
        bool Done => PuzzleState.IsSolved(FinaleLogic.DoneId);

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Running = false;
            StatueSocket.Shake = 0f;
            Clean();
        }

        void Start()
        {
            // Step 13 D: every Keeper hall gets the friendly rematch.
            foreach (var hall in FindObjectsByType<KeeperHall>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (hall.GetComponent<KeeperRematch>() == null)
                {
                    var r = hall.gameObject.AddComponent<KeeperRematch>();
                    r.hall = hall;
                    r.encounters = encounters;
                    r.runes = runes;
                }
            if (island == null || window == null || explorer == null || heart == null || spirit == null || party == null || hub == null)
            {
                Debug.LogError("[FinaleDirector] Needs the island, window, explorer, Heart, Spirit, party and the hub (Set Up MR_Main (13 finale)).", this);
                enabled = false;
                return;
            }
            _block = new MaterialPropertyBlock();
            PuzzleState.Restored += ApplySaved;
            Dialogue.RegisterTarget("heart", () => heart.Showing ? heart.HeadPointWorld(new Vector3(0f, 2.1f, 0.4f)) : (Vector3?)null, k => { }, () => 1.2f * Unit);
            Dialogue.RegisterTarget("finalePalm", () => Running && Step == FinaleStep.Goodbye ? PalmGuide : (Vector3?)null, k => { }, () => 0.03f * IslandScale);
            spirit.hold = () => _holding ? _hold : (Vector3?)null;
            ApplySaved();
        }

        void OnDisable() => PuzzleState.Restored -= ApplySaved;

        /// <summary>After a load: the Heart stays awake and the guide is the Spark once the finale is done.</summary>
        void ApplySaved()
        {
            if (Running) return;
            bool done = Done;
            if (done) Awake_(1f);
            else if (FinaleLogic.ResumeStep(StatueSocket.PlacedCount, PuzzleState.Stage(FinaleLogic.StageId), false) == FinaleStep.None) Asleep();
            spirit.SetSpark(done);
        }

        void Asleep()
        {
            heart.appear = 0f;
            heart.crystal = 0f;
            StatueSocket.HideStatue = false;
        }

        /// <summary>The Heart as it stays after the finale (or snapped awake to resume at the goodbye).</summary>
        void Awake_(float eyes)
        {
            heart.appear = 1f;
            heart.eyes = eyes;
            heart.wings = eyes > 0.5f ? 0.3f : 0f;
            heart.look = 1f;
            heart.lift = 8f;
            heart.veins = 1f;
            heart.crown = heart.gems = Vector3.one * (eyes > 0.5f ? 1f : 0.35f);
            heart.blaze = 0f;
            heart.crystal = eyes > 0.5f ? 1f : 0f;
            StatueSocket.HideStatue = true;
        }

        void Update()
        {
            if (Running)
            {
                if (!explorer.Locked) explorer.Locked = true;
                UpdateRing();
                UpdateLabels();
                return;
            }
            // The third shard locked in (any realm order), or a finale left in the middle: start when the hub is calm.
            var step = FinaleLogic.ResumeStep(StatueSocket.PlacedCount, PuzzleState.Stage(FinaleLogic.StageId), Done);
            bool calm = step != FinaleStep.None && step != FinaleStep.Done && heart.Here && !TitleScreen.Showing && !Opening.InProgress &&
                        !SceneSwap.IsBusy && !SceneSwap.IsInside && (encounters == null || !encounters.InBattle) && !Dialogue.IsTalking && !GameMenu.IsOpen;
            if (!calm)
            {
                _readySince = -1f;
                return;
            }
            if (_readySince < 0f) _readySince = Time.time;
            if (Time.time - _readySince < 1.2f) return;
            StartCoroutine(Run(step));
        }

        void LateUpdate()
        {
            CrystalShine();
            if (!Running && Done) PostGame();
            else if (_collection != null) _collection.Hide();
        }

        // ---- post-game (step 13 D) ----

        StoneTablet _collection;
        float _collectionUntil = -1f, _rosterAt, _sparkleAt, _arrivedAt;
        readonly bool[] _complete = new bool[3];
        readonly int[] _caught = new int[3], _total = new int[3];
        readonly HashSet<string> _hinted = new HashSet<string>();
        WorldMap _world;

        /// <summary>Realm index (Cinderwood, Galewick, Lilymere) -> the crown piece / gate element.</summary>
        static readonly string[] GateIds = { "fire", "wind", "water" };
        static readonly string[] HintIds = { "spark.rare.cinder", "spark.rare.gale", "spark.rare.lily" };

        void PostGame()
        {
            if (Time.time >= _rosterAt) Rosters(out _);
            bool onHub = heart.Showing;
            if (onHub && PokedHeart()) ShowCollection();
            // The card floats over the Heart's crown while it shows.
            if (_collection != null)
            {
                float a = Mathf.Clamp01((_collectionUntil - Time.time) / 0.6f);
                if (onHub && a > 0f) _collection.Place(heart.HeadPointWorld(new Vector3(0f, 3.3f, 0.3f)), IslandScale, a);
                else _collection.Hide();
            }
            // A completed roster: its crown piece wears a halo, its gate sparkles gold.
            var crown = Vector3.one;
            for (int i = 0; i < 3; i++)
                if (_complete[i]) crown[i == 0 ? 0 : i == 1 ? 2 : 1] = 1.6f + 0.6f * Mathf.Sin(Time.time * 2.4f + i);
            heart.crown = crown;
            if (onHub && fx != null && Time.time >= _sparkleAt)
            {
                _sparkleAt = Time.time + 0.45f;
                for (int i = 0; i < 3; i++)
                {
                    if (!_complete[i]) continue;
                    foreach (var g in hub.gates)
                        if (g.id == GateIds[i]) fx.Burst(MapWorld(InWindow(new Vector2(g.tile.x + 0.5f, g.tile.y)), Random.Range(0.3f, 1.6f)), 4, Gold, Unit * 0.8f, 0.9f, Unit * 0.05f, Vector3.up, -Unit);
                }
            }
            SparkHint();
        }

        /// <summary>Caught of each realm's roster (cached a couple of seconds); true when all three are complete.</summary>
        bool Rosters(out string line)
        {
            _rosterAt = Time.time + 2f;
            var caught = new HashSet<string>(save != null ? save.Caught : (IEnumerable<string>)new string[0]);
            var parts = new List<(string, int, int)>();
            bool all = true;
            for (int i = 0; i < 3; i++)
            {
                var map = realms != null && i < realms.Length ? realms[i] : null;
                var roster = FinaleLogic.Roster(map);
                _total[i] = roster.Count;
                _caught[i] = FinaleLogic.CaughtOf(roster, caught);
                _complete[i] = _total[i] > 0 && _caught[i] >= _total[i];
                all &= _complete[i];
                if (map != null) parts.Add((FinaleLogic.RealmName(map.realm), _caught[i], _total[i]));
            }
            line = FinaleLogic.CollectionLine(parts);
            return all;
        }

        /// <summary>A fingertip on the Heart's head or chest, or look + pinch on it.</summary>
        bool PokedHeart()
        {
            if (Time.time < _pokeAt) return false;
            bool poked = GazeOnHeart();
            for (int h = 0; h < 2 && !poked; h++)
            {
                var hand = HandRegistry.Get(h == 0 ? Handedness.Left : Handedness.Right);
                if (!HandLookup.TryGetJoint(hand, HandJointId.HandIndexTip, out var tip)) continue;
                poked = (tip - heart.HeadPointWorld(new Vector3(0f, 2.1f, 0.4f))).magnitude < 0.7f * Unit ||
                        (tip - heart.ChestWorld).magnitude < 0.6f * Unit;
            }
            if (poked) _pokeAt = Time.time + 2f;
            return poked;
        }

        float _pokeAt;

        /// <summary>The Isling collection per realm over the Heart, and a warm line.</summary>
        public void ShowCollection()
        {
            bool all = Rosters(out var line);
            if (_collection == null) _collection = new StoneTablet("Collection card", island.transform, hudMaterial, island.gameObject.layer);
            _collection.Set("Isling collection\n" + line, 0.15f, 0.5f, new Vector2(0.02f, 0.014f));
            _collectionUntil = Time.time + collectionSeconds;
            heart.nod = 0f;
            StartCoroutine(Nod());
            Dialogue.Say(all ? "heart.collection.full" : "heart.collection");
            Debug.Log($"[FinaleDirector] Collection: {line}{(all ? " (complete)" : "")}.");
        }

        IEnumerator Nod()
        {
            for (float t = 0f; t < 1.4f; t += Time.deltaTime)
            {
                heart.nod = 9f * Mathf.Sin(t / 1.4f * Mathf.PI);
                yield return null;
            }
            heart.nod = 0f;
        }

        /// <summary>The Spark's hint: in a realm with Islings still to meet, a little while after arriving (once a session).</summary>
        void SparkHint()
        {
            var world = island.world;
            if (world != _world)
            {
                _world = world;
                _arrivedAt = Time.time;
            }
            if (realms == null || SceneSwap.IsInside || Dialogue.IsTalking || Time.time - _arrivedAt < hintAfter) return;
            for (int i = 0; i < realms.Length && i < 3; i++)
            {
                if (realms[i] != world || _complete[i] || !_hinted.Add(HintIds[i])) continue;
                Dialogue.Say(HintIds[i]);
                Debug.Log($"[FinaleDirector] The Spark's hint in {world.name}: {_caught[i]}/{_total[i]} met.");
            }
        }

        // ---- the sequence ----

        IEnumerator Run(FinaleStep from)
        {
            Running = true;
            Debug.Log($"[FinaleDirector] The finale starts ({from}).");
            explorer.Locked = true;
            var front = new Vector2Int(Mathf.RoundToInt(Center.x), Mathf.RoundToInt(Center.y) + 3);
            if (explorer.Tile != front && island.Content != null && island.Content.CanWalk(front)) explorer.Teleport(front, new Vector2Int(0, -1));
            window.SetFocus(Center + new Vector2(0f, frameAhead));
            window.ZoomTo(Center, zoom, 2f);
            if (from <= FinaleStep.Wake) yield return Wake();
            else
            {
                Awake_(0f);
                Beat(true);
            }
            if (from <= FinaleStep.Understanding) yield return Understanding();
            yield return Goodbye();
            yield return EyesOpen();
            yield return HallOfFame();
            Beat(false);
            yield return Credits();
            yield return SparkReturns();
            Finish();
        }

        void SaveStep(FinaleStep step)
        {
            Step = step;
            PuzzleState.SetStage(FinaleLogic.StageId, (int)step);
            Debug.Log($"[FinaleDirector] {step}.");
        }

        IEnumerator Wake()
        {
            SaveStep(FinaleStep.Wake);
            float tile = Unit;
            // All three sockets flare.
            var colours = new[] { FireC, WaterC, WindC };
            var ids = new[] { ShardHolder.FireId, ShardHolder.WaterId, ShardHolder.WindId };
            for (int i = 0; i < 3; i++)
            {
                var s = StatueSocket.FindById(ids[i]);
                if (s != null && fx != null) fx.Burst(s.SocketWorld, 36, colours[i], tile * 1.4f, 0.9f, tile * 0.06f);
                Sfx(AudioIds.HeartCrack, AudioIds.StatueSocket, s != null ? s.SocketWorld : heart.ChestWorld);
                yield return new WaitForSeconds(0.5f);
            }
            // Light runs up through the cracks; the statue shakes; stone flakes and moss fall.
            for (float t = 0f; t < 6.5f; t += 0.25f)
            {
                StatueSocket.Shake = Mathf.Lerp(0.004f, 0.045f, t / 6.5f);
                if (fx != null)
                {
                    float h = Mathf.Repeat(t * 0.9f, 2.4f);
                    fx.Burst(heart.BodyPointWorld(new Vector3(Random.Range(-0.4f, 0.4f), h, 0.45f)), 6, Gold, tile * 0.8f, 0.7f, tile * 0.04f, Vector3.up, -tile * 2f);
                    if (t > 2f)
                        fx.Burst(heart.BodyPointWorld(new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(0.6f, 2.3f), Random.Range(-0.2f, 0.5f))), 8,
                            Random.value < 0.5f ? new Color(0.62f, 0.6f, 0.55f) : new Color(0.45f, 0.6f, 0.32f), tile * 1.2f, 1.1f, tile * 0.06f, Vector3.zero, tile * 4f);
                }
                if (Mathf.Repeat(t, 1.5f) < 0.01f) Sfx(AudioIds.HeartCrack, AudioIds.StatueSocket, heart.ChestWorld);
                yield return new WaitForSeconds(0.25f);
            }
            // The statue becomes the Heart: a burst of dust and gold covers the change.
            if (fx != null)
            {
                fx.Burst(heart.BodyPointWorld(new Vector3(0f, 1.2f, 0f)), 70, new Color(0.85f, 0.82f, 0.75f), tile * 2.6f, 1.4f, tile * 0.12f);
                fx.Burst(heart.BodyPointWorld(new Vector3(0f, 1.6f, 0.3f)), 50, Gold, tile * 3f, 1.6f, tile * 0.06f, Vector3.up, -tile);
            }
            GameAudio.Ui(AudioIds.JingleShard);
            StatueSocket.Shake = 0f;
            StatueSocket.HideStatue = true;
            heart.eyes = 0f;
            heart.wings = 0f;
            heart.look = 0f;
            heart.lift = 0f;
            heart.blaze = 0f;
            for (float t = 0f; t < 1.6f; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / 1.6f);
                heart.appear = k;
                heart.veins = k;
                heart.crown = heart.gems = Vector3.one * (0.35f * k);
                yield return null;
            }
            heart.appear = 1f;
            if (AudioService.Has(AudioIds.HeartBeat)) Beat(true);
            else Sfx(AudioIds.HeartBeat, AudioIds.StatueSocket, heart.ChestWorld);
            // It breathes and lifts its head toward the player.
            for (float t = 0f; t < 3f; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / 3f);
                heart.lift = 10f * k;
                heart.look = 0.7f * k;
                yield return null;
            }
            yield return Line("heart.wake", 12f);
        }

        IEnumerator Understanding()
        {
            SaveStep(FinaleStep.Understanding);
            _holding = true;
            for (float t = 0f; t < 1.5f; t += Time.deltaTime)
            {
                _hold = WaitPoint;
                yield return null;
            }
            yield return Line("spirit.finale.1", 10f);
        }

        /// <summary>In the forehead's setting (its feet a little below the middle, so the glowing body fills it).</summary>
        Vector3 InSocket => heart.SocketWorld - island.transform.up * (inSocketDrop * Unit);

        /// <summary>Before the forehead, toward the player, a little up.</summary>
        Vector3 WaitPoint
        {
            get
            {
                var socket = heart.SocketWorld;
                var cam = Camera.main;
                Vector3 toward = cam != null ? cam.transform.position - socket : island.transform.forward;
                toward.y = 0f;
                toward = toward.sqrMagnitude > 1e-6f ? toward.normalized : island.transform.forward;
                return socket + toward * (waitInFront * Unit) + Vector3.up * (0.4f * Unit);
            }
        }

        /// <summary>Where the ghost shows the open palm: in front of the island's near rim, a little up.</summary>
        Vector3 PalmGuide
        {
            get
            {
                var b = island.LocalBounds;
                return island.transform.TransformPoint(new Vector3(0f, island.CapTopLocal + 0.12f, b.max.z + 0.05f));
            }
        }

        IEnumerator Goodbye()
        {
            SaveStep(FinaleStep.Goodbye);
            _holding = true;
            heart.look = 0.7f;
            handoff.Reset();
            Dialogue.Pin(new[] { "spirit.finale.2", "spirit.finale.hand" }, "Hold out your hand", new[] { "spirit.finale.wait" });
            Dialogue.Say("spirit.finale.2");
            if (ghosts != null && !string.IsNullOrEmpty(ghostClip)) ghosts.Show(ghostClip, GhostPlace.AtTarget, "finalePalm", 1f, DominantHand.Left, false);
            Vector3 socketFrom = default;
            float flyT = -1f;
            while (true)
            {
                Vector3 socket = heart.SocketWorld;
                ReadPalm(out bool tracked, out float up, out bool open, out Vector3 palm, out Vector3 normal);
                bool gaze = GazeOnHeart();
                var was = handoff.Now;
                string line = handoff.Step(Time.deltaTime, tracked, up, open, palm, socket, IslandScale, gaze);
                if (line == "spirit.finale.hand")
                {
                    Dialogue.SetChip("Lift it to the Heart");
                    Dialogue.Say(line);
                    if (ghosts != null) ghosts.Hide();
                    Sfx(AudioIds.SparkChime, AudioIds.UiSaved, palm);
                    Debug.Log("[FinaleDirector] The Spirit lands on the palm.");
                    Moment?.Invoke("palm.land");
                }
                else if (line == "spirit.finale.wait") Dialogue.Say(line);
                else if (line == "spirit.finale.3")
                {
                    Dialogue.Unpin();
                    Dialogue.Say(line);
                    socketFrom = _hold;
                    flyT = 0f;
                    Debug.Log($"[FinaleDirector] The Spirit floats off {(was == PalmHandoff.State.OnPalm ? "the palm" : "by itself (look + pinch)")} into the Heart's forehead.");
                }
                if (was == PalmHandoff.State.OnPalm && handoff.Now == PalmHandoff.State.Waiting) Debug.Log("[FinaleDirector] The palm turned away: the Spirit floats back to wait.");

                switch (handoff.Now)
                {
                    case PalmHandoff.State.Waiting:
                        _hold = WaitPoint;
                        Halo(false, default);
                        break;
                    case PalmHandoff.State.OnPalm:
                        // It sits on the palm, glowing, a warm halo round the hand.
                        _hold = palm + normal * 0.03f + Vector3.up * (0.004f * Mathf.Sin(Time.time * 3f));
                        Halo(true, palm + normal * 0.02f);
                        if (fx != null && Random.value < Time.deltaTime * 4f) fx.Burst(palm + normal * 0.02f, 2, Gold, 0.02f, 0.6f, 0.004f, Vector3.up, -0.03f);
                        break;
                    case PalmHandoff.State.ToSocket:
                        Halo(false, default);
                        flyT += Time.deltaTime;
                        MoteFlight(flyT, socketFrom);
                        break;
                }
                if (handoff.Now == PalmHandoff.State.ToSocket && flyT >= FlightSeconds) break;
                yield return null;
            }
            // Step 14f: the light goes into the crystal; the lantern stays hidden until the Spark comes out of it.
            ReleaseMote();
            spirit.Appear = 0f;
            _hold = InSocket;
            yield return Ignite();
        }

        IEnumerator EyesOpen()
        {
            SaveStep(FinaleStep.EyesOpen);
            Sfx(AudioIds.HeartWings, AudioIds.StatueSocket, heart.ChestWorld);
            heart.look = 1f;
            for (float t = 0f; t < 3f; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / 3f);
                heart.eyes = Mathf.Clamp01(t / 0.6f);
                heart.wings = k;
                heart.blaze = Mathf.Sin(k * Mathf.PI) * 0.8f;
                heart.crown = heart.gems = Vector3.one * Mathf.Lerp(0.35f, 1f, k);
                _hold = InSocket;
                yield return null;
            }
            heart.blaze = 0.25f;
            // The hub answers: the gates bloom, the grass waves, warm motes rise, a golden ring under the island.
            StartCoroutine(HubAnswers(9f));
            yield return Line("heart.thanks", 12f);
            yield return new WaitForSeconds(2f);
        }

        IEnumerator HubAnswers(float seconds)
        {
            Sfx(AudioIds.GatesBloom, AudioIds.JingleShard, heart.ChestWorld);
            float tile = Unit;
            for (float t = 0f; t < seconds; t += 0.2f)
            {
                _ringAlpha = Mathf.Clamp01(t / 2f);
                if (fx != null)
                {
                    foreach (var g in hub.gates)
                    {
                        Vector3 at = MapWorld(InWindow(new Vector2(g.tile.x + 0.5f, g.tile.y)), 0.9f);
                        switch (g.id)
                        {
                            case "fire": fx.Burst(at, 6, FireC, tile * 1.2f, 0.8f, tile * 0.07f, Vector3.up, -tile * 2.5f); break;
                            case "water": fx.Burst(at + Vector3.up * tile, 6, WaterC, tile * 1.4f, 1f, tile * 0.05f, Vector3.zero, tile * 2f); break;
                            case "wind": fx.Burst(at, 5, WindC, tile * 1.8f, 0.9f, tile * 0.05f, Vector3.up, -tile); break;
                        }
                    }
                    // Warm motes rise off the island into the room.
                    var b = island.LocalBounds;
                    var p = island.transform.TransformPoint(new Vector3(Random.Range(b.min.x, b.max.x), island.CapTopLocal, Random.Range(b.min.z, b.max.z)));
                    fx.Burst(p, 3, Gold, tile * 0.5f, 2.4f, tile * 0.05f, Vector3.up, -tile * 3f);
                }
                // The grass waves: a push sweeping round the Heart.
                float a = t * 2.2f;
                IslandBuilder.SetGrassPush(MapWorld(Center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (2.5f + 2f * Mathf.Sin(t * 0.7f)), 0f), 0.8f * Mathf.Abs(Mathf.Sin(t * 3f)));
                yield return new WaitForSeconds(0.2f);
            }
            IslandBuilder.SetGrassPush(Vector3.zero, 0f);
        }

        /// <summary>A map point, pulled toward the Heart until it is inside the island window.</summary>
        Vector2 InWindow(Vector2 tile)
        {
            for (int i = 0; i < 20; i++)
            {
                if (island.FootprintContains(island.TileToLocal(tile, island.Content.capHeight))) return tile;
                tile = Vector2.MoveTowards(tile, Center, 0.5f);
            }
            return tile;
        }

        IEnumerator HallOfFame()
        {
            SaveStep(FinaleStep.HallOfFame);
            Dialogue.Say("heart.friends");
            yield return new WaitForSeconds(2.5f);
            var rosters = Rosters();
            int n = Mathf.Min(Party.Size, party.members.Count);
            for (int i = 0; i < n; i++)
            {
                var c = party.members[i];
                if (c == null || c.species == null || c.species.Prefab == null) continue;
                if (string.IsNullOrEmpty(c.metIn)) c.metIn = IsStarterLine(c.species) ? FinaleLogic.StillIsle : FinaleLogic.GuessMetIn(c.species, rosters);
                var tile = new Vector2Int(Mathf.FloorToInt(Center.x - (n - 1) * 0.5f + i + 0.5f), Mathf.RoundToInt(Center.y) + 2);
                yield return ComeOut(i, tile);
                var who = Spawn(c, tile, false);
                if (who != null)
                {
                    var card = new StoneTablet($"Hall of Fame card {i}", island.transform, hudMaterial, island.gameObject.layer);
                    card.Set(FinaleLogic.Card(c.Name, c.level, c.metIn), 0.07f, 0.1f, new Vector2(0.007f, 0.005f));
                    _labels.Add((card, who));
                    _labelHeight.Add(i % 2 == 0 ? cardHeight.x : cardHeight.y);
                    _cards.Add(card);
                    GameAudio.At(AudioIds.UiSaved, MapWorld(tile, 1f));
                    Debug.Log($"[FinaleDirector] Hall of Fame: {c.Name} Lv {c.level}, met in {c.metIn}.");
                }
                yield return new WaitForSeconds(hallGap);
            }
            // A few of the Lodge's Islings wander out too.
            int guests = 0;
            for (int i = 0; i < party.storage.Count && guests < lodgeGuests; i++)
            {
                var c = party.storage[i];
                if (c == null || c.species == null || c.species.Prefab == null) continue;
                int side = guests % 2 == 0 ? -1 : 1;
                var tile = new Vector2Int(Mathf.RoundToInt(Center.x) + side * (4 + guests / 2), Mathf.RoundToInt(Center.y) + 1 + guests);
                Spawn(c, tile, true);
                guests++;
                yield return new WaitForSeconds(0.8f);
            }
            if (save != null) save.AddHallOfFame();
            // Everyone cheers.
            GameAudio.Ui(AudioIds.JingleVictory);
            for (int i = 0; i < 3; i++)
            {
                MapCreature.HopNear(hub, Center, 10f);
                yield return new WaitForSeconds(0.7f);
            }
            yield return new WaitForSeconds(4f);
            foreach (var (card, _) in _labels) card.Hide();
            _labels.Clear();
            _labelHeight.Clear();
        }

        /// <summary>A sparkle leaves pod <paramref name="slot"/> and arcs to the tile (the Isling comes out of its pod).</summary>
        IEnumerator ComeOut(int slot, Vector2Int tile)
        {
            Vector3 to = MapWorld(tile, 0.3f);
            Vector3 from = pods != null && pods.SlotWorld(slot).HasValue ? pods.SlotWorld(slot).Value : to + Vector3.up * 0.05f;
            GameAudio.At(AudioIds.UiSaved, from);
            for (float t = 0f; t < 0.6f; t += 0.05f)
            {
                float k = t / 0.6f;
                if (fx != null) fx.Burst(Vector3.Lerp(from, to, k) + Vector3.up * (0.05f * IslandScale * Mathf.Sin(k * Mathf.PI)), 3, Gold, 0.01f, 0.4f, Unit * 0.05f);
                yield return new WaitForSeconds(0.05f);
            }
            if (fx != null) fx.Burst(to, 20, Color.white, Unit * 1.2f, 0.6f, Unit * 0.06f);
        }

        MapCreature Spawn(IslingInstance c, Vector2Int tile, bool wander)
        {
            var go = Instantiate(c.species.Prefab, island.transform, false);
            go.name = $"Finale {c.Name}";
            go.SetActive(false);
            var mc = go.AddComponent<MapCreature>();
            mc.island = island;
            mc.explorer = explorer;
            mc.homeTile = tile;
            mc.map = hub;
            mc.wander = wander;
            mc.wanderRadius = 1;
            go.SetActive(true);
            var view = go.GetComponent<CreatureView>();
            if (view != null) view.Hop();
            _guests.Add(mc);
            return mc;
        }

        void UpdateLabels()
        {
            for (int i = 0; i < _labels.Count; i++)
            {
                var (card, who) = _labels[i];
                if (who == null) continue;
                card.Place(MapWorld(who.Tile, _labelHeight[i]), IslandScale, 1f);
            }
        }

        IEnumerator Credits()
        {
            SaveStep(FinaleStep.Credits);
            var tablet = new StoneTablet("Credits tablet", island.transform, hudMaterial, island.gameObject.layer);
            _cards.Add(tablet);
            var b = island.LocalBounds;
            foreach (var text in credits)
            {
                if (string.IsNullOrEmpty(text)) continue;
                tablet.Set(text, 0.22f, 0.42f, new Vector2(0.02f, 0.014f));
                Sfx(AudioIds.CreditsTablet, AudioIds.StatueSocket, island.transform.TransformPoint(new Vector3(0f, island.CapTopLocal, b.min.z)));
                Debug.Log($"[FinaleDirector] Credits: {text.Replace('\n', ' ')}");
                for (float t = 0f; t < tabletSeconds; t += Time.deltaTime)
                {
                    // Rises from behind the far rim, stays readable, sinks again.
                    float rise = Mathf.SmoothStep(0f, 1f, t / 0.9f) * (1f - Mathf.SmoothStep(0f, 1f, (t - tabletSeconds + 0.9f) / 0.9f));
                    var local = new Vector3(0f, island.CapTopLocal + Mathf.Lerp(tabletRise.x, tabletRise.y, rise), b.min.z + 0.02f);
                    tablet.Place(island.transform.TransformPoint(local), IslandScale, Mathf.Clamp01(rise * 1.5f));
                    yield return null;
                }
            }
            tablet.Hide();
        }

        IEnumerator SparkReturns()
        {
            SaveStep(FinaleStep.Spark);
            // A tiny spark slips from the forehead crystal down into the explorer's lantern...
            Vector3 eye = heart.SocketWorld;
            if (fx != null) fx.Burst(eye, 16, Gold, Unit * 0.6f, 0.6f, Unit * 0.04f);
            // The explorer's lantern: its tile, at about its hip (the explorer's own transform isn't where it stands).
            Vector3 lantern = MapWorld(explorer.Tile, 0.55f);
            Sfx(AudioIds.SparkChime, AudioIds.UiSaved, eye);
            for (float t = 0f; t < 1.6f; t += 0.05f)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / 1.6f);
                if (fx != null) fx.Burst(Vector3.Lerp(eye, lantern, k) + Vector3.up * (Unit * Mathf.Sin(k * Mathf.PI)), 3, Gold, 0.005f, 0.5f, Unit * 0.04f);
                yield return new WaitForSeconds(0.05f);
            }
            if (fx != null) fx.Burst(lantern, 24, Gold, Unit, 0.7f, Unit * 0.05f);
            // ...and hops out as the guide: the Spark.
            spirit.SetSpark(true);
            spirit.Appear = 0f;
            _hold = lantern;
            yield return new WaitForSeconds(0.4f);
            for (float t = 0f; t < 1f; t += Time.deltaTime)
            {
                spirit.Appear = Mathf.SmoothStep(0f, 1f, t);
                _hold = lantern + Vector3.up * (1.6f * Unit * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI * 0.5f));
                yield return null;
            }
            spirit.Appear = 1f;
            Moment?.Invoke("spark.out");
            yield return Line("spark.hello", 10f);
            _holding = false;
        }

        void Finish()
        {
            PuzzleState.Solve(FinaleLogic.DoneId, 1);
            SaveStep(FinaleStep.Done);
            Running = false;
            Awake_(1f);
            _holding = false;
            spirit.Appear = 1f;
            _ringAlpha = 0f;
            UpdateRing();
            window.SetFocus(null);
            window.ZoomTo(Center, 1f, 2f);
            explorer.Locked = false;
            // The Lodge's guests go home; the party back into the pods.
            foreach (var g in _guests)
                if (g != null)
                {
                    if (fx != null) fx.Burst(MapWorld(g.Tile, 0.3f), 12, Color.white, Unit, 0.5f, Unit * 0.05f);
                    Destroy(g.gameObject);
                }
            _guests.Clear();
            if (save != null) save.Save("finale done", true);
            Debug.Log("[FinaleDirector] The finale is done: free play.");
            Moment?.Invoke("done");
        }

        void Clean()
        {
            _beat?.Off(0.3f);
            foreach (var g in _guests)
                if (g != null) Destroy(g.gameObject);
            _guests.Clear();
            foreach (var c in _cards) c.Destroy();
            _cards.Clear();
            if (_collection != null) _collection.Destroy();
            _labels.Clear();
            if (_ring != null) Destroy(_ring.gameObject);
        }

        // ---- the palm ----

        /// <summary>The hand most palm-up now (real, or the bridge's synthetic palm).</summary>
        void ReadPalm(out bool tracked, out float up, out bool open, out Vector3 palm, out Vector3 normal)
        {
            tracked = false;
            up = -1f;
            open = false;
            palm = default;
            normal = Vector3.up;
            if (debugPalm.HasValue)
            {
                tracked = true;
                open = true;
                up = debugPalmUp;
                palm = MapWorld(new Vector2(debugPalm.Value.x, debugPalm.Value.z), debugPalm.Value.y);
                normal = Vector3.up * Mathf.Sign(debugPalmUp);
                return;
            }
            for (int h = 0; h < 2; h++)
            {
                var hand = HandRegistry.Get(h == 0 ? Handedness.Left : Handedness.Right);
                if (hand == null || !hand.IsTrackedDataValid || grabGuard != null && grabGuard.IsHandBusy(h == 0)) continue;
                if (!HandLookup.TryGetJointPose(hand, HandJointId.HandPalm, out var pose)) continue;
                // The palmar side is the palm joint's -Y for both hands (ISDK v207, as ScoopPour / PalmHover).
                Vector3 n = pose.rotation * Vector3.down;
                float u = Vector3.Dot(n, Vector3.up);
                if (u <= up) continue;
                tracked = true;
                up = u;
                open = !HandPuzzle.IsFistPose(hand, pose.position);
                palm = pose.position;
                normal = n;
            }
        }

        /// <summary>Looking at the Heart's head and a fresh pinch.</summary>
        bool GazeOnHeart()
        {
            if (_debugGaze)
            {
                _debugGaze = false;
                return true;
            }
            bool pinch = false;
            for (int h = 0; h < 2; h++)
            {
                var hand = HandRegistry.Get(h == 0 ? Handedness.Left : Handedness.Right);
                bool p = hand != null && hand.IsTrackedDataValid && hand.GetIndexFingerIsPinching() && (grabGuard == null || !grabGuard.IsHandBusy(h == 0));
                if (p && !_wasPinching[h]) pinch = true;
                _wasPinching[h] = p;
            }
            if (!pinch || !HeadGaze.TryGetRay(out var ray)) return false;
            var head = heart.HeadPointWorld(new Vector3(0f, 2.1f, 0.4f));
            return Vector3.Angle(ray.direction, head - ray.origin) < 9f;
        }

        void Halo(bool on, Vector3 at)
        {
            if (fx == null) return;
            if (!on)
            {
                if (_halo != null)
                {
                    fx.Release(_halo);
                    _halo = null;
                }
                return;
            }
            if (_halo == null)
            {
                _halo = fx.Get(BattleFx.Shape.Orb);
                if (_halo == null) return;
                _halo.Set(new Color(1f, 0.9f, 0.6f), new Color(1f, 0.62f, 0.25f), 1f, 1.5f, 1f, 0.3f);
            }
            _halo.transform.position = at;
            _halo.transform.localScale = Vector3.one * (0.022f / Mathf.Max(1e-4f, fx.transform.lossyScale.x));
            _halo.SetAlpha(0.4f + 0.1f * Mathf.Sin(Time.time * 4f));
        }

        // ---- step 14f: the Spirit turns to light and flies into the forehead crystal ----

        BattleFx.Item _mote;

        /// <summary>
        /// The flight, <paramref name="t"/> seconds in: the lantern dissolves into a bright gold mote where it was
        /// (<see cref="dissolveSeconds"/>), then the mote arcs into the crystal. The lantern mesh is never near the forehead.
        /// </summary>
        void MoteFlight(float t, Vector3 from)
        {
            float d = Mathf.Clamp01(t / dissolveSeconds);
            spirit.Appear = 1f - Mathf.SmoothStep(0f, 1f, d);
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - dissolveSeconds) / (FlightSeconds - dissolveSeconds)));
            Vector3 at = Vector3.Lerp(from, heart.SocketWorld, k) + Vector3.up * (0.6f * Unit * Mathf.Sin(k * Mathf.PI));
            _hold = at;
            if (fx == null) return;
            if (_mote == null)
            {
                _mote = fx.Get(BattleFx.Shape.Orb);
                if (_mote == null) return;
                _mote.Set(new Color(1f, 0.97f, 0.8f), new Color(1f, 0.72f, 0.28f), 1f, 1.5f, 1f, 0.3f);
            }
            // It swells as the lantern goes, then tightens into a point of light on the way in.
            float size = moteSize * Unit * Mathf.Lerp(Mathf.SmoothStep(0f, 1f, d), 0.55f, k);
            _mote.transform.position = at;
            _mote.transform.localScale = Vector3.one * (size / Mathf.Max(1e-4f, fx.transform.lossyScale.x));
            _mote.SetAlpha(1f);
            if (Random.value < Time.deltaTime * 30f) fx.Burst(at, 2, Gold, Unit * 0.05f, 0.5f, Unit * 0.03f);
        }

        void ReleaseMote()
        {
            if (_mote != null && fx != null) fx.Release(_mote);
            _mote = null;
        }

        /// <summary>The crystal takes the light: it ignites, a soft ring of light spreads from it, the heartbeat thumps.</summary>
        IEnumerator Ignite()
        {
            Vector3 c = heart.SocketWorld;
            Sfx(AudioIds.SparkChime, AudioIds.StatueSocket, c);
            Sfx(AudioIds.HeartBeat, AudioIds.StatueSocket, c);
            if (fx != null) fx.Burst(c, 30, Gold, Unit * 1.2f, 0.9f, Unit * 0.04f);
            Moment?.Invoke("crystal.lit");
            BattleFx.Item ring = fx != null ? fx.Get(BattleFx.Shape.Ring) : null;
            if (ring != null) ring.Set(new Color(1f, 0.92f, 0.6f), new Color(1f, 0.75f, 0.3f), 1f);
            for (float t = 0f; t < 1f; t += Time.deltaTime)
            {
                heart.crystal = Mathf.SmoothStep(0f, 1f, t / 0.4f);
                if (ring != null)
                {
                    // Faces the player, widening and fading.
                    var cam = Camera.main;
                    Vector3 toward = cam != null ? cam.transform.position - heart.SocketWorld : island.transform.forward;
                    ring.transform.SetPositionAndRotation(heart.SocketWorld, Quaternion.FromToRotation(Vector3.up, toward));
                    ring.transform.localScale = Vector3.one * (Unit * Mathf.Lerp(0.3f, 2.4f, t) / Mathf.Max(1e-4f, fx.transform.lossyScale.x));
                    ring.SetAlpha(0.8f * (1f - t));
                }
                yield return null;
            }
            heart.crystal = 1f;
            if (ring != null) fx.Release(ring);
        }

        // ---- Nov 4: the lit crystal radiates ----

        /// <summary>
        /// While the forehead crystal is lit (Mugesh: it looked blank bright yellow): a soft gold halo breathing with the
        /// heartbeat, a ring of light swelling out of it on each beat, and now and then a twinkle drifting up.
        /// </summary>
        void CrystalShine()
        {
            float c = heart != null && heart.Showing ? Mathf.Clamp01(heart.crystal) : 0f;
            if (fx == null || c < 0.01f)
            {
                if (_shine != null) fx.Release(_shine);
                if (_pulse != null) fx.Release(_pulse);
                _shine = _pulse = null;
                return;
            }
            Vector3 at = heart.SocketWorld;
            float inv = 1f / Mathf.Max(1e-4f, fx.transform.lossyScale.x);
            float phase = Time.time * heart.beatRate;
            float beat = HeartOfWorld.Heartbeat(phase);
            if (_shine == null)
            {
                _shine = fx.Get(BattleFx.Shape.Orb);
                if (_shine != null) _shine.Set(new Color(1f, 0.88f, 0.55f), new Color(1f, 0.66f, 0.22f), 1f, 2.2f, 0.12f, 0.35f);
            }
            if (_shine != null)
            {
                _shine.transform.position = at;
                _shine.transform.localScale = Vector3.one * (Unit * (0.42f + 0.1f * beat) * c * inv);
                _shine.SetAlpha(c * (0.45f + 0.35f * beat));
            }
            // One ring per beat, facing the player, widening and fading.
            float cycle = phase - Mathf.Floor(phase);
            if (_pulse == null)
            {
                _pulse = fx.Get(BattleFx.Shape.Ring);
                if (_pulse != null) _pulse.Set(new Color(1f, 0.92f, 0.6f), new Color(1f, 0.72f, 0.3f), 1f);
            }
            if (_pulse != null)
            {
                var cam = Camera.main;
                Vector3 toward = cam != null ? cam.transform.position - at : island.transform.forward;
                float k = Mathf.Clamp01((cycle - 0.08f) / 0.7f);
                _pulse.transform.SetPositionAndRotation(at, Quaternion.FromToRotation(Vector3.up, toward));
                _pulse.transform.localScale = Vector3.one * (Unit * Mathf.Lerp(0.25f, 1.1f, k) * inv);
                _pulse.SetAlpha(cycle < 0.08f ? 0f : 0.5f * c * (1f - k));
            }
            if (Time.time >= _twinkleAt)
            {
                _twinkleAt = Time.time + Random.Range(0.25f, 0.6f);
                fx.Burst(at + Random.insideUnitSphere * (Unit * 0.12f), 1, Gold, Unit * 0.12f, 0.9f, Unit * 0.025f);
            }
        }

        // ---- the golden ring under the island ----

        void UpdateRing()
        {
            if (ringMaterial == null) return;
            if (_ring == null)
            {
                var go = new GameObject("Finale Ring");
                go.layer = island.gameObject.layer;
                go.transform.SetParent(island.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = BattleHud.VerticalQuad();
                _ringRenderer = go.AddComponent<MeshRenderer>();
                _ringRenderer.sharedMaterial = ringMaterial;
                _ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _ring = go.transform;
            }
            bool on = _ringAlpha > 0.01f;
            _ring.gameObject.SetActive(on);
            if (!on) return;
            var b = island.LocalBounds;
            _ring.localPosition = new Vector3(0f, b.min.y - 0.01f, 0f);
            _ring.localRotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            _ring.localScale = Vector3.one * (Mathf.Max(b.size.x, b.size.z) * 1.25f);
            _block.Clear();
            _block.SetFloat("_Mode", 1f);
            _block.SetFloat("_Fill", 1f);
            _block.SetFloat("_Intensity", _ringAlpha * (1.2f + 0.3f * Mathf.Sin(Time.time * 2f)));
            _ringRenderer.SetPropertyBlock(_block);
        }

        // ---- helpers ----

        IEnumerator Line(string id, float max)
        {
            Dialogue.Say(id);
            yield return null;
            for (float t = 0f; t < max && Dialogue.IsPending(id); t += Time.deltaTime) yield return null;
        }

        Vector3 MapWorld(Vector2 tile, float height) =>
            island.transform.TransformPoint(island.TileToLocal(tile, island.Content.capHeight + height * island.map.tileSize));

        bool IsStarterLine(IslingSpecies s)
        {
            if (starters == null) return false;
            foreach (var st in starters)
                for (var x = st; x != null; x = x.evolvesTo == x ? null : x.evolvesTo)
                    if (x == s) return true;
            return false;
        }

        Dictionary<string, HashSet<IslingSpecies>> Rosters()
        {
            var d = new Dictionary<string, HashSet<IslingSpecies>>();
            if (realms != null)
                foreach (var m in realms)
                    if (m != null) d[FinaleLogic.RealmName(m.realm)] = FinaleLogic.Roster(m);
            return d;
        }

        /// <summary>heart.beat: the Heart's slow lub-dub loop at its chest, from the wake to the end of the Hall of Fame.</summary>
        void Beat(bool on)
        {
            _beat ??= new LoopAnchor(transform, "Heartbeat Sound");
            if (on && heart != null) _beat.Set(AudioIds.HeartBeat, heart.ChestWorld, 1f);
            else _beat.Off(1.5f);
        }

        LoopAnchor _beat;

        static void Sfx(string id, string fallback, Vector3 at)
        {
            if (AudioService.Has(id)) GameAudio.At(id, at);
            else if (!string.IsNullOrEmpty(fallback)) GameAudio.At(fallback, at);
        }

        // ---- dev panel / bridge ----

        /// <summary>
        /// Dev panel (Story > Finale): the save edited to all three shards placed and the finale not done, back on the hub
        /// in front of the statue; it plays from the wake, or (<paramref name="goodbye"/>) from the goodbye. Step 14c: the
        /// Quick Journey's See the ending passes <paramref name="persist"/> false (its save is suspended: nothing written).
        /// </summary>
        public static string DevPlay(bool goodbye, bool persist = true)
        {
            var f = Instance;
            if (f == null) return "no finale in this scene";
            if (Running) return "already playing";
            var saved = PuzzleState.ToSaved();
            saved.RemoveAll(p => p.id == ShardHolder.FireId || p.id == ShardHolder.WindId || p.id == ShardHolder.WaterId || p.id == FinaleLogic.StageId || p.id == FinaleLogic.DoneId);
            foreach (var id in new[] { ShardHolder.FireId, ShardHolder.WindId, ShardHolder.WaterId }) saved.Add(new SavedPuzzle { id = id, solved = true, stage = 2 });
            if (goodbye) saved.Add(new SavedPuzzle { id = FinaleLogic.StageId, solved = false, stage = (int)FinaleStep.Goodbye });
            PuzzleState.Restore(saved);
            f.spirit.SetSpark(false);
            f.spirit.Appear = 1f;
            f.Asleep();
            var swap = FindFirstObjectByType<SceneSwap>();
            var front = new Vector2Int(Mathf.RoundToInt(f.Center.x), Mathf.RoundToInt(f.Center.y) + 3);
            if (swap != null)
            {
                if (SceneSwap.IsInside) swap.ReturnNow();
                swap.TravelNow(f.hub, front, new Vector2Int(0, -1));
            }
            if (persist && f.save != null) f.save.Save("dev: finale " + (goodbye ? "from the goodbye" : "from the wake"));
            return goodbye ? "from the goodbye" : "from the wake";
        }

        /// <summary>Debug (bridge): look + pinch on the Heart once.</summary>
        public void DebugGazePinch() => _debugGaze = true;

        public string Describe() =>
            $"step {Step}, running {Running}, palm {handoff.Now}, heart appear {heart.appear:0.##} eyes {heart.eyes:0.##} wings {heart.wings:0.##}, " +
            $"statue hidden {StatueSocket.HideStatue}, spark {spirit.Spark}, guests {_guests.Count}, done {Done}, stage {PuzzleState.Stage(FinaleLogic.StageId)}";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Running = false;
            Instance = null;
        }
    }
}
