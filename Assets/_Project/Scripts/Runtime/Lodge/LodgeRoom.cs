using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// The Hearth Lodge while it shows (<see cref="SceneSwap"/> into <see cref="lodge"/>): the first
    /// <see cref="maxInRoom"/> stored Islings live in the room (<see cref="LodgeIsling"/>); the rest sleep in the
    /// honeycomb, six cells a page (a small page arrow beside it when there are more; poke it or look + pinch).
    /// What the hands can do:
    /// - look + pinch (or poke) an Isling: it hops onto the pedestal and its full card shows (pinch elsewhere closes it);
    /// - look + pinch a honeycomb pod: that Isling comes out into the room (the one that has wandered longest goes to sleep);
    /// - grab an Isling (it curls into a pod) and drop it on a rim socket: empty = joins the party, full = swap (the rim
    ///   Isling pops out into the room); dropped anywhere else it pops back out on the floor;
    /// - grab a rim pod and drop or toss it on the floor: that Isling joins the room (never the last one: it wobbles and
    ///   the Spirit says so);
    /// - with an Isling on the pedestal, look + pinch a rim pod (or the empty socket) = the same swap;
    /// - walk out onto either door tile (a swipe toward it or a tap / look + pinch on it, <see cref="AimedOut"/>) or look +
    ///   pinch the door arch: back outside.
    /// </summary>
    public class LodgeRoom : MonoBehaviour
    {
        public IslandBuilder island;
        public SceneSwap swap;
        public WorldMap lodge;
        public GestureTestPiece explorer;
        public Party party;
        public PodRack pods;
        public ProgressionConfig progression;
        public BattleFx fx;
        public Material hudMaterial;
        [Tooltip("Pinches from a hand that is grabbing or touching the rim are ignored.")]
        public IslandHandle grabGuard;

        [Header("Room (lodge tiles)")]
        public Vector2Int pedestalTile = new Vector2Int(4, 5);
        [Tooltip("Pedestal top above the floor (prop tile units).")]
        public float pedestalTop = 0.5f;
        public Vector2Int[] doorTiles = { new Vector2Int(3, 7), new Vector2Int(4, 7) };
        [Tooltip("Door arch height (prop tile units; matches PropKit's LodgeDoor).")]
        public float archHeight = 0.76f;
        public Vector2Int[] cozyTiles = { new Vector2Int(2, 1), new Vector2Int(5, 1), new Vector2Int(6, 2) };
        [Tooltip("Centre of the honeycomb pod wall (lodge tiles; its anchor plus half its footprint).")]
        public Vector2 honeycombCenter = new Vector2(1.5f, 0f);

        [Tooltip("The step from inside onto a door tile (column, row): rows grow toward the door.")]
        public Vector2Int doorOutward = new Vector2Int(0, 1);

        [Header("Door ring")]
        [Tooltip("Diorama/Beacon material (the ring over the arch while it is looked at).")]
        public Material beaconMaterial;

        [Header("Limits and sizes")]
        [Tooltip("Islings walking the room (the rest sleep in the honeycomb). Each costs about 9 draws (body pieces, outlines, eyes): lower it if the lodge drops frames.")]
        [Range(1, 16)] public int maxInRoom = 8;
        [Tooltip("Grab volume of an Isling in the room (diameter, island-local metres).")]
        public float grabSize = 0.07f;
        [Tooltip("A dropped Isling pod lands in a rim socket within this distance (island-local metres).")]
        public float dropDistance = 0.06f;

        // Honeycomb cells (prop tile units, from the prop's centre): matches PropKit's LodgeHoneycomb.
        static readonly Vector3[] Cells =
        {
            new Vector3(-0.55f, 0.42f, 0.02f), new Vector3(0f, 0.42f, 0.02f), new Vector3(0.55f, 0.42f, 0.02f),
            new Vector3(-0.55f, 0.96f, 0.02f), new Vector3(0f, 0.96f, 0.02f), new Vector3(0.55f, 0.96f, 0.02f),
        };

        class Grabber
        {
            public PodView pod;
            public PodGrab grab;
        }

        readonly List<LodgeIsling> _islings = new List<LodgeIsling>();
        readonly Dictionary<LodgeIsling, Grabber> _grabbers = new Dictionary<LodgeIsling, Grabber>();
        readonly PodView[] _comb = new PodView[6];
        readonly bool[] _wasPinching = { true, true };
        readonly int[] _poked = { -1, -1 };
        IslingCard _card;
        Mesh _quad;
        LodgeIsling _onPedestal, _gazed;
        int _gazedComb = -1, _gazeId = -1, _page;
        bool _gazedDoor, _gazedPage;
        const int CombId = 100, DoorId = 200, PageId = 300; // gaze pick ids
        Transform _pageArrow;
        TMPro.TextMeshPro _pageText;
        readonly Dictionary<LodgeIsling, float> _since = new Dictionary<LodgeIsling, float>();
        Transform _doorRing;
        MeshRenderer _doorRingRenderer;
        MaterialPropertyBlock _block;
        Vector2Int? _spawnHint;
        readonly HashSet<IslingSpecies> _warned = new HashSet<IslingSpecies>();

        public bool Active => swap != null && swap.Inside && swap.Interior == lodge;

        /// <summary>The player did something in the lodge: "inspect" (pedestal), "swap" (an Isling joined the pods), "deposit" (a rim pod let go). LodgeGuide listens.</summary>
        public event System.Action<string> Acted;

        float Tile => island.map.tileSize;
        float Cap => island.Content.capHeight;
        float WorldScale => island.transform.lossyScale.x;
        /// <summary>One lodge tile in world metres right now.</summary>
        float TileWorld => Tile * island.Zoom * WorldScale;

        void Start()
        {
            if (island == null || swap == null || lodge == null || explorer == null || party == null || pods == null || hudMaterial == null)
            {
                Debug.LogError("[LodgeRoom] Needs the island, scene swap, lodge map, explorer, party, pod rack and HUD material (Set Up MR_Main (7b lodge)).", this);
                enabled = false;
                return;
            }
            _quad = BattleHud.VerticalQuad();
            _card = new IslingCard(hudMaterial, _quad, island.transform);
            _block = new MaterialPropertyBlock();
            MakeDoorRing();
            if (beaconMaterial == null) Debug.LogError("[LodgeRoom] No beacon material: the door arch shows no ring when looked at (Set Up MR_Main (7c)).", this);
            swap.Entered += OnEntered;
            swap.Returned += OnReturned;
            pods.LodgeDropped += OnRimDropped;
            explorer.OnStep += OnStep;
            // The room as a whole has no outline to light (a tiny focus sphere): the beam alone points into it.
            Dialogue.RegisterTarget("lodgeCenter", () => Active ? island.transform.TransformPoint(island.TileToLocal(new Vector2(3.5f, 3.5f), Cap + 0.4f * Tile)) : (Vector3?)null, k => { }, () => 0.0001f);
            Dialogue.RegisterTarget("pedestal", () => Active ? island.transform.TransformPoint(PedestalTopLocal) : (Vector3?)null, k => { }, () => 0.75f * TileWorld);
            Dialogue.RegisterTarget("lodgeIsling", NearestWanderer, k => { }, () => 0.5f * TileWorld);
            Dialogue.RegisterTarget("lodgeDoor", () => Active ? DoorWorld(0.3f) : (Vector3?)null, k => { }, () => 0.9f * TileWorld);
            // Loaded inside before this Start ran.
            if (Active) OnEntered(lodge);
        }

        void OnDestroy()
        {
            if (swap != null)
            {
                swap.Entered -= OnEntered;
                swap.Returned -= OnReturned;
            }
            if (pods != null) pods.LodgeDropped -= OnRimDropped;
            if (explorer != null) explorer.OnStep -= OnStep;
            if (_card != null) _card.Destroy();
            if (_quad != null) Destroy(_quad);
        }

        Vector3 PedestalTopLocal => island.TileToLocal(pedestalTile, Cap + pedestalTop * Tile);

        /// <summary>The room Isling nearest the explorer (not on the pedestal, not held), for the inspect hint.</summary>
        Vector3? NearestWanderer()
        {
            if (this == null || !Active) return null;
            LodgeIsling best = null;
            float bestSq = float.MaxValue;
            foreach (var li in _islings)
            {
                if (li == null || li.Leaving || li.Hidden || li == _onPedestal) continue;
                float d = (island.LocalToTile(li.LocalCenter) - explorer.Tile).sqrMagnitude;
                if (d < bestSq)
                {
                    bestSq = d;
                    best = li;
                }
            }
            return best != null ? island.transform.TransformPoint(best.LocalCenter) : (Vector3?)null;
        }

        void OnEntered(WorldMap map)
        {
            if (map != lodge || !enabled) return;
            pods.LodgeMode = true;
            pods.PickHandler = OnRimPicked;
            Sync();
            // The welcome is LodgeGuide's first step (lodge.first, or lodge.first.empty for an empty lodge).
            Debug.Log($"[LodgeRoom] Inside: {Mathf.Min(maxInRoom, party.storage.Count)} Isling(s) in the room, " +
                      $"{Mathf.Max(0, party.storage.Count - maxInRoom)} asleep in the honeycomb ({Cells.Length} a page), {party.members.Count} in the pods.");
        }

        void OnReturned(WorldMap map)
        {
            Close();
            foreach (var i in _islings) if (i != null) Destroy(i.gameObject);
            _islings.Clear();
            _since.Clear();
            _page = 0;
            if (_pageArrow != null) _pageArrow.gameObject.SetActive(false);
            foreach (var g in _grabbers.Values) if (g.pod != null) Destroy(g.pod.gameObject);
            _grabbers.Clear();
            for (int k = 0; k < _comb.Length; k++)
            {
                if (_comb[k] != null) Destroy(_comb[k].gameObject);
                _comb[k] = null;
            }
            pods.LodgeMode = false;
            if (pods.PickHandler == OnRimPicked) pods.PickHandler = null;
            pods.SetExternalGaze(this, false);
            pods.SelectedSlot = -1;
            if (_doorRing != null) _doorRing.gameObject.SetActive(false);
        }

        void Update()
        {
            if (_card == null) return;
            _card.Tick();
            if (!Active) return;
            Sync();
            UpdateGrabbers();
            UpdateInput();
            if (_onPedestal != null && _card.Visible)
                _card.Show(_onPedestal.Isling, progression, PedestalTopLocal + Vector3.up * (_onPedestal.View.height * island.Zoom + 0.012f));
        }

        // ---- who is where ----

        /// <summary>Room Islings = the first ones in storage; the next six sleep in the honeycomb.</summary>
        void Sync()
        {
            var storage = party.storage;
            int inRoom = Mathf.Min(maxInRoom, storage.Count);
            for (int i = _islings.Count - 1; i >= 0; i--)
            {
                var li = _islings[i];
                if (li == null)
                {
                    _islings.RemoveAt(i);
                    continue;
                }
                int index = storage.IndexOf(li.Isling);
                if (index >= 0 && index < inRoom) continue;
                if (_grabbers.TryGetValue(li, out var g) && g.grab != null && g.grab.IsHeld) continue;
                if (li == _onPedestal) Close();
                li.Leave();
                DropGrabber(li);
                _islings.RemoveAt(i);
            }
            // At most two new Islings a frame: entering the lodge with a full room doesn't spike one frame.
            int spawned = 0;
            for (int i = 0; i < inRoom && spawned < 2; i++)
            {
                var c = storage[i];
                if (_islings.Exists(l => l.Isling == c)) continue;
                Spawn(c, _spawnHint ?? FreeTile());
                _spawnHint = null;
                spawned++;
            }
            int sleepers = Mathf.Max(0, storage.Count - maxInRoom), pages = Mathf.Max(1, (sleepers + Cells.Length - 1) / Cells.Length);
            if (_page >= pages) _page = 0;
            UpdatePageArrow(pages);
            for (int k = 0; k < _comb.Length; k++)
            {
                int index = maxInRoom + _page * Cells.Length + k;
                var c = index < storage.Count ? storage[index] : null;
                var p = _comb[k];
                if (c == null)
                {
                    if (p != null) Destroy(p.gameObject);
                    _comb[k] = null;
                    continue;
                }
                if (p == null)
                {
                    p = _comb[k] = Instantiate(pods.podPrefab, island.transform, false);
                    p.name = $"Honeycomb Pod {k + 1}";
                    var grab = p.GetComponent<PodGrab>();
                    if (grab != null) grab.CanGrab = false;
                }
                if (p.Isling != c || p.ShownSpecies != c.species) p.Show(c);
                p.BaseScale = Vector3.one * (island.Zoom * 0.85f);
                p.transform.localPosition = CombLocal(k);
                p.transform.localRotation = Quaternion.identity;
                p.Hover = k == _gazedComb ? 1f : 0f;
            }
        }

        Vector3 CombLocal(int k)
        {
            var cell = Cells[k];
            return island.TileToLocal(new Vector2(honeycombCenter.x - cell.x, honeycombCenter.y + cell.z), Cap + cell.y * Tile);
        }

        void Spawn(IslingInstance c, Vector2Int tile)
        {
            var prefab = c.species != null ? c.species.Prefab : null;
            if (prefab == null)
            {
                if (c.species != null && _warned.Add(c.species)) Debug.LogError($"[LodgeRoom] {c.species.displayName} has no baked prefab; it can't walk the lodge.");
                return;
            }
            var go = Instantiate(prefab, island.transform);
            go.name = $"Lodge {c.Name}";
            var li = go.AddComponent<LodgeIsling>();
            li.island = island;
            li.Init(this, c, tile);
            _islings.Add(li);
            _since[li] = Time.time;
            MakeGrabber(li);
        }

        /// <summary>A walkable floor tile nobody stands on (not the pedestal, door or the explorer's).</summary>
        public bool CanStand(Vector2Int tile, LodgeIsling who)
        {
            if (!lodge.CanWalk(tile) || tile == pedestalTile || tile == explorer.Tile) return false;
            foreach (var d in doorTiles) if (d == tile) return false;
            foreach (var other in _islings)
                if (other != null && other != who && !other.Leaving && other.Covers(tile)) return false;
            return true;
        }

        /// <summary>Next to the hearth (row 1) or on a cushion: a place to doze.</summary>
        public bool IsCozy(Vector2Int tile)
        {
            if (tile.y <= 1) return true;
            foreach (var c in cozyTiles) if (c == tile) return true;
            return false;
        }

        Vector2Int FreeTile()
        {
            var free = new List<Vector2Int>();
            for (int r = 1; r < lodge.Rows - 1; r++)
                for (int c = 0; c < lodge.Columns; c++)
                {
                    var t = new Vector2Int(c, r);
                    if (CanStand(t, null)) free.Add(t);
                }
            return free.Count > 0 ? free[Random.Range(0, free.Count)] : lodge.startTile;
        }

        Vector2Int FreeTileNear(Vector2 near)
        {
            Vector2Int best = lodge.startTile;
            float bestSq = float.MaxValue;
            for (int r = 0; r < lodge.Rows; r++)
                for (int c = 0; c < lodge.Columns; c++)
                {
                    var t = new Vector2Int(c, r);
                    float d = (t - near).sqrMagnitude;
                    if (d < bestSq && CanStand(t, null))
                    {
                        bestSq = d;
                        best = t;
                    }
                }
            return best;
        }

        // ---- grabbing a room Isling ----

        void MakeGrabber(LodgeIsling li)
        {
            var p = Instantiate(pods.podPrefab, island.transform, false);
            p.name = $"Grab {li.Isling.Name}";
            p.Show(li.Isling);
            var g = new Grabber { pod = p, grab = p.GetComponent<PodGrab>() };
            ShowPod(g, false);
            if (g.grab != null)
            {
                g.grab.Grabbed += _ => OnIslingGrabbed(li);
                g.grab.Released += (_, v) => OnIslingReleased(li);
            }
            _grabbers[li] = g;
        }

        void DropGrabber(LodgeIsling li)
        {
            if (!_grabbers.TryGetValue(li, out var g)) return;
            if (g.pod != null) Destroy(g.pod.gameObject);
            _grabbers.Remove(li);
        }

        /// <summary>Hidden: just a grab volume around the Isling. Shown: the pod with the Isling curled inside.</summary>
        static void ShowPod(Grabber g, bool on)
        {
            if (g.pod.glass != null) g.pod.glass.enabled = on;
            if (g.pod.band != null) g.pod.band.enabled = on;
            g.pod.IslingOut = !on;
        }

        void UpdateGrabbers()
        {
            foreach (var kv in _grabbers)
            {
                var li = kv.Key;
                var g = kv.Value;
                if (li == null || g.pod == null) continue;
                bool held = g.grab != null && g.grab.IsHeld;
                if (g.grab != null) g.grab.CanGrab = held || !li.Leaving && li.Grow > 0.9f && li != _onPedestal;
                if (held) continue;
                g.pod.BaseScale = Vector3.one;
                g.pod.transform.localPosition = li.LocalCenter;
                g.pod.transform.localRotation = Quaternion.identity;
                g.pod.SetGrabDiameter(grabSize);
            }
        }

        void OnIslingGrabbed(LodgeIsling li)
        {
            if (li == null || !_grabbers.TryGetValue(li, out var g)) return;
            if (li == _onPedestal) Close();
            li.Hidden = true;
            ShowPod(g, true);
            Debug.Log($"[LodgeRoom] Picked up {li.Isling.Name}: it curls into a pod.");
        }

        void OnIslingReleased(LodgeIsling li)
        {
            if (li == null || !_grabbers.TryGetValue(li, out var g) || g.pod == null) return;
            int slot = pods.NearestSocket(g.pod.transform.position, dropDistance * WorldScale);
            int index = party.storage.IndexOf(li.Isling);
            if (slot >= 0 && index >= 0)
            {
                var outgoing = party.At(slot);
                Vector2 near = island.LocalToTile(island.transform.InverseTransformPoint(pods.HomeWorld(slot)));
                if (party.SwapWithStorage(index, slot))
                {
                    Acted?.Invoke("swap");
                    _spawnHint = FreeTileNear(near);
                    Burst(pods.HomeWorld(slot));
                    Debug.Log($"[LodgeRoom] {li.Isling.Name} into pod {slot + 1}{(outgoing != null ? $"; {outgoing.Name} pops out into the room" : " (new pod)")}.");
                    return; // Sync removes this Isling and spawns the outgoing one.
                }
            }
            // Anywhere else: back out on the floor under the drop.
            Vector2 at = island.LocalToTile(island.transform.InverseTransformPoint(g.pod.transform.position));
            li.PopAt(FreeTileNear(at));
            ShowPod(g, false);
            Burst(g.pod.transform.position);
            Debug.Log($"[LodgeRoom] {li.Isling.Name} dropped on the floor.");
        }

        // ---- a rim pod dropped in the lodge ----

        void OnRimDropped(int slot, Vector3 local, Vector3 vel)
        {
            if (!Active)
            {
                StartCoroutine(pods.FlyHome(slot));
                return;
            }
            bool nearHome = (local - pods.HomeLocal(slot)).magnitude < dropDistance * 0.6f && vel.magnitude < 0.3f;
            if (nearHome)
            {
                StartCoroutine(pods.FlyHome(slot));
                return;
            }
            if (party.members.Count <= 1)
            {
                StartCoroutine(Refuse(slot));
                return;
            }
            var c = party.At(slot);
            if (!party.Deposit(slot))
            {
                StartCoroutine(Refuse(slot));
                return;
            }
            pods.Discard(slot);
            Acted?.Invoke("deposit");
            Vector2 at = island.LocalToTile(local);
            _spawnHint = FreeTileNear(at);
            Burst(island.transform.TransformPoint(local));
            Debug.Log($"[LodgeRoom] {c.Name} let go from pod {slot + 1}: into the room.");
        }

        /// <summary>Testing (BattleDebug): as if rim pod <paramref name="slot"/> was dropped in the middle of the floor.</summary>
        public void DebugDropRimPod(int slot)
        {
            var p = pods.Pod(slot);
            if (p == null) return;
            p.Detached = true;
            OnRimDropped(slot, island.TileToLocal(new Vector2(3.5f, 4f), Cap + Tile), Vector3.down);
        }

        /// <summary>Testing: as if the pedestal Isling was swapped with a look + pinch on rim pod <paramref name="slot"/>.</summary>
        public bool DebugPedestalSwap(int slot) => OnRimPicked(slot);

        IEnumerator Refuse(int slot)
        {
            Debug.Log("[LodgeRoom] The last Isling in the pods stays.");
            yield return pods.FlyHome(slot);
            yield return pods.Wobble(slot);
            Dialogue.Say("lodge.last");
        }

        /// <summary>Look + pinch on a rim pod or the empty socket: with an Isling on the pedestal, swap it in.</summary>
        bool OnRimPicked(int slot)
        {
            if (!Active || _onPedestal == null) return false;
            var li = _onPedestal;
            int index = party.storage.IndexOf(li.Isling);
            var outgoing = party.At(slot);
            Close();
            if (index < 0 || !party.SwapWithStorage(index, slot)) return true;
            Acted?.Invoke("swap");
            _spawnHint = FreeTileNear(pedestalTile + Vector2Int.down);
            Burst(pods.HomeWorld(slot));
            Debug.Log($"[LodgeRoom] Look + pinch: {li.Isling.Name} into pod {slot + 1}{(outgoing != null ? $", {outgoing.Name} out into the room" : "")}.");
            return true;
        }

        // ---- inspect, honeycomb, door ----

        /// <summary>Hops an Isling onto the pedestal and shows its card (again = close). Also BattleDebug.</summary>
        public void Inspect(LodgeIsling li)
        {
            if (li == null) return;
            if (li == _onPedestal)
            {
                Close();
                return;
            }
            Close();
            _onPedestal = li;
            li.ToPedestal(pedestalTile, pedestalTop * Tile);
            _card.Show(li.Isling, progression, PedestalTopLocal + Vector3.up * (li.View.height * island.Zoom + 0.012f));
            Debug.Log($"[LodgeRoom] {li.Isling.Name} on the pedestal.");
            Acted?.Invoke("inspect");
        }

        /// <summary>The first room Isling (BattleDebug).</summary>
        public LodgeIsling First => _islings.Count > 0 ? _islings[0] : null;

        public void Close()
        {
            if (_onPedestal != null) _onPedestal.OffPedestal(FreeTileNear(pedestalTile + Vector2Int.down));
            _onPedestal = null;
            if (_card != null) _card.Hide();
        }

        /// <summary>A honeycomb sleeper comes out into the room (the last one in the room goes to sleep in its place).</summary>
        public void Wake(int k)
        {
            int index = maxInRoom + _page * Cells.Length + k;
            if (index >= party.storage.Count) return;
            var c = party.storage[index];
            // The Isling that has wandered longest (not on the pedestal, not held) takes the sleeper's cell.
            LodgeIsling oldest = null;
            foreach (var li in _islings)
            {
                if (li == null || li.Leaving || li == _onPedestal || _grabbers.TryGetValue(li, out var g) && g.grab != null && g.grab.IsHeld) continue;
                float since = _since.TryGetValue(li, out float t) ? t : 0f;
                if (oldest == null || since < (_since.TryGetValue(oldest, out float o) ? o : 0f)) oldest = li;
            }
            int sleeper = oldest != null ? party.storage.IndexOf(oldest.Isling) : -1;
            if (sleeper >= 0 && sleeper < maxInRoom) party.SwapStorage(index, sleeper);
            else party.BringForward(index);
            if (oldest != null) Debug.Log($"[LodgeRoom] {oldest.Isling.Name} (wandering longest) goes to sleep in the honeycomb.");
            _spawnHint = FreeTileNear(honeycombCenter + Vector2.up * 1.5f);
            Burst(island.transform.TransformPoint(CombLocal(k)));
            Debug.Log($"[LodgeRoom] {c.Name} wakes up and hops out of the honeycomb.");
        }

        void OnStep(Vector2Int tile)
        {
            if (!Active || !IsDoor(tile)) return;
            var dest = explorer.Destination;
            if (!AimedOut(explorer.LastStep, doorOutward, explorer.Heading, dest.HasValue && IsDoor(dest.Value)))
            {
                Debug.Log($"[LodgeRoom] On the door tile c{tile.x} r{tile.y} but not walking out (step {explorer.LastStep}, heading {explorer.Heading}): staying in.");
                return;
            }
            Debug.Log("[LodgeRoom] Out the door.");
            Leave();
        }

        bool IsDoor(Vector2Int tile)
        {
            foreach (var d in doorTiles)
                if (d == tile) return true;
            return false;
        }

        /// <summary>The middle of the door arch's opening, <paramref name="height"/> tiles above the floor (world).</summary>
        Vector3 DoorWorld(float height) => island.transform.TransformPoint(DoorLocal(height));

        Vector3 DoorLocal(float height) => island.TileToLocal(DoorCenter, Cap + height * Tile);

        Vector2 DoorCenter
        {
            get
            {
                Vector2 sum = Vector2.zero;
                foreach (var d in doorTiles) sum += d;
                return doorTiles.Length > 0 ? sum / doorTiles.Length : new Vector2(3.5f, 7f);
            }
        }

        static readonly int ModeId = Shader.PropertyToID("_Mode");
        static readonly int FillId = Shader.PropertyToID("_Fill");
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        /// <summary>
        /// The ring over the arch while it is looked at (look + pinch leaves). Step 9h: no palm-over-the-arch exit any
        /// more, a hand resting near the door took players out by accident.
        /// </summary>
        void UpdateDoorRing(bool free)
        {
            if (_doorRing == null) return;
            bool show = free && _gazedDoor;
            _doorRing.gameObject.SetActive(show);
            if (!show) return;
            _doorRing.localPosition = DoorLocal(archHeight + 0.15f);
            _doorRing.localRotation = Quaternion.Euler(90f, 0f, 0f);
            _doorRing.localScale = Vector3.one * Tile * island.Zoom * 1.3f;
            _block.Clear();
            _block.SetFloat(ModeId, 1f);
            _block.SetFloat(FillId, 0f);
            _block.SetFloat(IntensityId, 1.2f);
            _doorRingRenderer.SetPropertyBlock(_block);
        }

        /// <summary>
        /// Step 9h: a step onto a door tile leaves only when the move was aimed out of the room: the step goes toward the
        /// door (<paramref name="outward"/>) and either a swipe walks that way or the tapped / looked-at destination is a
        /// door tile. A path that only passes the door, or ends beside it, stays inside.
        /// </summary>
        public static bool AimedOut(Vector2Int step, Vector2Int outward, Vector2Int heading, bool destinationIsDoor) =>
            step == outward && (heading == outward || destinationIsDoor);

        void Leave()
        {
            if (SceneSwap.IsBusy) return;
            Close();
            swap.Return();
        }

        void MakeDoorRing()
        {
            if (beaconMaterial == null) return;
            var go = new GameObject("Lodge Door Ring");
            go.transform.SetParent(island.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = _quad;
            _doorRingRenderer = go.AddComponent<MeshRenderer>();
            _doorRingRenderer.sharedMaterial = beaconMaterial;
            _doorRingRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _doorRingRenderer.receiveShadows = false;
            _doorRing = go.transform;
            go.SetActive(false);
        }

        static float Sq(float x) => x * x;
        readonly bool[] _pagePoked = new bool[2];

        /// <summary>More sleepers than cells: a small "1/3 >" beside the honeycomb (poke or look + pinch for the next page).</summary>
        void UpdatePageArrow(int pages)
        {
            if (_pageArrow == null)
            {
                var go = new GameObject("Honeycomb Page");
                go.transform.SetParent(island.transform, false);
                _pageText = go.AddComponent<TMPro.TextMeshPro>();
                _pageText.fontSize = 0.12f;
                _pageText.alignment = TMPro.TextAlignmentOptions.Center;
                _pageText.rectTransform.sizeDelta = new Vector2(0.06f, 0.02f);
                _pageText.fontStyle = TMPro.FontStyles.Bold;
                _pageArrow = go.transform;
            }
            bool show = pages > 1;
            if (_pageArrow.gameObject.activeSelf != show) _pageArrow.gameObject.SetActive(show);
            if (!show) return;
            _pageText.text = $"{_page + 1}/{pages} >";
            // Beside the honeycomb (toward the room's middle), at the height of its upper row.
            _pageArrow.localPosition = island.TileToLocal(new Vector2(honeycombCenter.x + 1.35f, honeycombCenter.y + 0.2f), Cap + 0.7f * Tile);
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 away = _pageArrow.position - cam.transform.position;
                if (away.sqrMagnitude > 1e-6f) _pageArrow.rotation = Quaternion.LookRotation(away, island.transform.up);
            }
            _pageArrow.localScale = Vector3.one;
        }

        public void NextPage(string how)
        {
            int sleepers = Mathf.Max(0, party.storage.Count - maxInRoom), pages = Mathf.Max(1, (sleepers + Cells.Length - 1) / Cells.Length);
            if (pages <= 1) return;
            _page = (_page + 1) % pages;
            Debug.Log($"[LodgeRoom] Honeycomb page {_page + 1}/{pages} ({how}).");
        }

        void Burst(Vector3 world)
        {
            if (fx != null) fx.Burst(world, 14, new Color(1f, 0.85f, 0.5f), 0.03f * WorldScale, 0.45f, 0.003f * WorldScale);
        }

        // ---- gaze, pinch, poke ----

        void UpdateInput()
        {
            bool free = !SceneSwap.IsBusy && !explorer.Locked;
            _gazed = null;
            _gazedComb = -1;
            _gazedDoor = false;
            _gazedPage = false;
            if (free && HeadGaze.TryGetRay(out var ray))
            {
                // Gaze cone over everything selectable (at least the 4 cm minimum each): Islings 0.., honeycomb pods
                // CombId + k, the door arch DoorId; the one nearest the ray wins, the current one is a little stickier.
                float tile = Tile * island.Zoom * WorldScale, min = HeadGaze.MinRadius * WorldScale;
                var pick = new HeadGaze.ConePick(ray, _gazeId);
                for (int i = 0; i < _islings.Count; i++)
                {
                    var li = _islings[i];
                    if (li == null || li.Leaving || li.Hidden) continue;
                    // Step 8c: a bigger ball than its body (0.45 tile was too small to hold with the head on a wandering Isling).
                    pick.Consider(i, island.transform.TransformPoint(li.LocalCenter), Mathf.Max(min, 0.75f * tile));
                }
                for (int k = 0; k < _comb.Length; k++)
                    if (_comb[k] != null) pick.Consider(CombId + k, _comb[k].transform.position, Mathf.Max(min, 0.3f * tile));
                if (_pageArrow != null && _pageArrow.gameObject.activeSelf) pick.Consider(PageId, _pageArrow.position, Mathf.Max(min, 0.3f * tile));
                // The whole arch (two tiles wide, about 0.8 tile tall) plus a margin.
                pick.Consider(DoorId, DoorWorld(0.4f), Mathf.Max(min, 1.15f * tile));
                _gazeId = pick.Best;
                if (pick.Best >= PageId) _gazedPage = true;
                else if (pick.Best >= DoorId) _gazedDoor = true;
                else if (pick.Best >= CombId) _gazedComb = pick.Best - CombId;
                else if (pick.Best >= 0) _gazed = _islings[pick.Best];
            }
            else _gazeId = -1;
            if (_gazed != null)
            {
                _lastGazed = _gazed;
                _lastGazedAt = Time.time;
            }
            foreach (var li in _islings)
                if (li != null) li.Noticed = li == _gazed;
            // White = what you're looking at (FocusHighlight; gold is only for hints).
            if (_gazed != null) FocusHighlight.Hover(island.transform.TransformPoint(_gazed.LocalCenter), 0.5f * TileWorld);
            else if (_gazedComb >= 0) FocusHighlight.Hover(_comb[_gazedComb].transform.position, 0.3f * TileWorld);
            else if (_gazedDoor) FocusHighlight.Hover(DoorWorld(0f), 0.9f * TileWorld);
            if (_pageText != null) _pageText.color = _gazedPage ? Color.white : new Color(1f, 0.8f, 0.4f);
            pods.SetExternalGaze(this, _gazed != null || _gazedComb >= 0 || _gazedDoor || _gazedPage);
            UpdateDoorRing(free);
            if (!free) return;

            for (int h = 0; h < 2; h++)
            {
                bool left = h == 0;
                var hand = HandRegistry.Get(left ? Handedness.Left : Handedness.Right);
                bool tracked = HandLookup.TryGetJoint(hand, HandJointId.HandIndexTip, out var tip);

                // Poke an Isling.
                int poked = -1;
                if (tracked)
                    for (int i = 0; i < _islings.Count; i++)
                    {
                        var li = _islings[i];
                        if (li == null || li.Leaving || li.Hidden) continue;
                        float r = 0.4f * Tile * island.Zoom * WorldScale;
                        if ((island.transform.TransformPoint(li.LocalCenter) - tip).sqrMagnitude < r * r) poked = i;
                    }
                if (poked >= 0 && poked != _poked[h] && _islings[poked] != _onPedestal) Inspect(_islings[poked]);
                _poked[h] = poked;
                bool pagePoke = tracked && _pageArrow != null && _pageArrow.gameObject.activeSelf &&
                                (tip - _pageArrow.position).sqrMagnitude < Sq(0.3f * TileWorld);
                if (pagePoke && !_pagePoked[h]) NextPage("poke");
                _pagePoked[h] = pagePoke;

                bool pinching = tracked && hand.GetIndexFingerIsPinching();
                bool busy = grabGuard != null && grabGuard.IsHandBusy(left);
                if (pinching && !_wasPinching[h] && !busy) Pinch();
                _wasPinching[h] = pinching;
            }
        }

        LodgeIsling _lastGazed;
        float _lastGazedAt = -1f;
        /// <summary>A pinch just after the gaze slipped off an Isling still counts for it (the head moves as the hand pinches).</summary>
        const float GazeGrace = 0.35f;

        void Pinch()
        {
            bool other = _gazedComb >= 0 || _gazedPage || _gazedDoor;
            var isling = _gazed != null ? _gazed
                       : !other && _lastGazed != null && !_lastGazed.Hidden && !_lastGazed.Leaving && Time.time - _lastGazedAt < GazeGrace ? _lastGazed : null;
            if (isling != null) Inspect(isling);
            else if (_gazedComb >= 0) Wake(_gazedComb);
            else if (_gazedPage) NextPage("look + pinch");
            else if (_gazedDoor)
            {
                Debug.Log("[LodgeRoom] Look + pinch on the door arch.");
                Leave();
            }
            // A pinch on nothing (not a rim pod either) closes the card.
            else if (_card.Visible && !pods.IsGazed) Close();
        }
    }
}
