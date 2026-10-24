using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// The Hearth Lodge while it shows (<see cref="SceneSwap"/> into <see cref="lodge"/>): the first
    /// <see cref="maxInRoom"/> stored Islings live in the room (<see cref="LodgeIsling"/>); the next ones sleep in the
    /// honeycomb pods. What the hands can do:
    /// - look + pinch (or poke) an Isling: it hops onto the pedestal and its full card shows (pinch elsewhere closes it);
    /// - look + pinch a honeycomb pod: that Isling comes out into the room (the last one in the room goes to sleep);
    /// - grab an Isling (it curls into a pod) and drop it on a rim socket: empty = joins the party, full = swap (the rim
    ///   Isling pops out into the room); dropped anywhere else it pops back out on the floor;
    /// - grab a rim pod and drop or toss it on the floor: that Isling joins the room (never the last one: it wobbles and
    ///   the Spirit says so);
    /// - with an Isling on the pedestal, look + pinch a rim pod (or the empty socket) = the same swap;
    /// - walk onto the door tiles, or look + pinch the door: back outside.
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
        public Vector2Int[] cozyTiles = { new Vector2Int(2, 1), new Vector2Int(5, 1), new Vector2Int(6, 2) };
        [Tooltip("Centre of the honeycomb pod wall (lodge tiles; its anchor plus half its footprint).")]
        public Vector2 honeycombCenter = new Vector2(1.5f, 0f);

        [Header("Limits and sizes")]
        [Tooltip("Islings walking the room (the next six sleep in the honeycomb). Each costs about 9 draws (body pieces, outlines, eyes): lower it if the lodge drops frames.")]
        [Range(1, 16)] public int maxInRoom = 12;
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
        readonly Hand[] _hands = new Hand[2];
        readonly bool[] _wasPinching = { true, true };
        readonly int[] _poked = { -1, -1 };
        IslingCard _card;
        Mesh _quad;
        LodgeIsling _onPedestal, _gazed;
        int _gazedComb = -1;
        bool _gazedDoor;
        Vector2Int? _spawnHint;
        readonly HashSet<IslingSpecies> _warned = new HashSet<IslingSpecies>();

        public bool Active => swap != null && swap.Inside && swap.Interior == lodge;

        float Tile => island.map.tileSize;
        float Cap => island.Content.capHeight;
        float WorldScale => island.transform.lossyScale.x;

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
            swap.Entered += OnEntered;
            swap.Returned += OnReturned;
            pods.LodgeDropped += OnRimDropped;
            explorer.OnStep += OnStep;
            Dialogue.RegisterTarget("lodgeCenter", () => Active ? island.transform.TransformPoint(island.TileToLocal(new Vector2(3.5f, 3.5f), Cap + 0.4f * Tile)) : (Vector3?)null, k => { });
            Dialogue.RegisterTarget("pedestal", () => Active ? island.transform.TransformPoint(PedestalTopLocal) : (Vector3?)null, k => { });
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

        void OnEntered(WorldMap map)
        {
            if (map != lodge || !enabled) return;
            pods.LodgeMode = true;
            pods.PickHandler = OnRimPicked;
            Sync();
            Dialogue.Say("lodge.first");
            Debug.Log($"[LodgeRoom] Inside: {Mathf.Min(maxInRoom, party.storage.Count)} Isling(s) in the room, " +
                      $"{Mathf.Clamp(party.storage.Count - maxInRoom, 0, Cells.Length)} asleep in the honeycomb, {party.members.Count} in the pods.");
        }

        void OnReturned(WorldMap map)
        {
            Close();
            foreach (var i in _islings) if (i != null) Destroy(i.gameObject);
            _islings.Clear();
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
            for (int k = 0; k < _comb.Length; k++)
            {
                int index = maxInRoom + k;
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
            Dialogue.Say("lodge.swap.first");
            Debug.Log($"[LodgeRoom] {li.Isling.Name} on the pedestal.");
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
            int index = maxInRoom + k;
            if (index >= party.storage.Count) return;
            var c = party.storage[index];
            party.BringForward(index);
            _spawnHint = FreeTileNear(honeycombCenter + Vector2.up * 1.5f);
            Burst(island.transform.TransformPoint(CombLocal(k)));
            Debug.Log($"[LodgeRoom] {c.Name} wakes up and hops out of the honeycomb.");
        }

        void OnStep(Vector2Int tile)
        {
            if (!Active) return;
            foreach (var d in doorTiles)
                if (d == tile)
                {
                    Debug.Log("[LodgeRoom] Out the door.");
                    swap.Return();
                    return;
                }
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
            if (free && HeadGaze.TryGetRay(out var ray))
            {
                float best = float.MaxValue;
                float r = 0.6f * Tile * island.Zoom * WorldScale;
                foreach (var li in _islings)
                {
                    if (li == null || li.Leaving || li.Hidden) continue;
                    float off = Off(ray, island.transform.TransformPoint(li.LocalCenter));
                    if (off < r && off < best)
                    {
                        best = off;
                        _gazed = li;
                    }
                }
                for (int k = 0; k < _comb.Length; k++)
                {
                    if (_comb[k] == null) continue;
                    float off = Off(ray, _comb[k].transform.position);
                    if (off < r * 0.6f && off < best)
                    {
                        best = off;
                        _gazed = null;
                        _gazedComb = k;
                    }
                }
                Vector3 door = island.transform.TransformPoint(island.TileToLocal(new Vector2(3.5f, 7f), Cap + 0.2f * Tile));
                float doorOff = Off(ray, door);
                if (doorOff < r * 1.2f && doorOff < best)
                {
                    _gazed = null;
                    _gazedComb = -1;
                    _gazedDoor = true;
                }
            }
            foreach (var li in _islings)
                if (li != null && li.View != null && li != _onPedestal) li.View.SetFlash(li == _gazed ? 0.25f : 0f);
            pods.SetExternalGaze(this, _gazed != null || _gazedComb >= 0 || _gazedDoor);
            if (!free) return;

            for (int h = 0; h < 2; h++)
            {
                bool left = h == 0;
                var hand = HandLookup.Refresh(ref _hands[h], left ? Handedness.Left : Handedness.Right);
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

                bool pinching = tracked && hand.GetIndexFingerIsPinching();
                bool busy = grabGuard != null && grabGuard.IsHandBusy(left);
                if (pinching && !_wasPinching[h] && !busy) Pinch();
                _wasPinching[h] = pinching;
            }
        }

        static float Off(Ray ray, Vector3 point)
        {
            Vector3 to = point - ray.origin;
            float along = Vector3.Dot(to, ray.direction);
            return along <= 0f ? float.MaxValue : (to - ray.direction * along).magnitude;
        }

        void Pinch()
        {
            if (_gazed != null) Inspect(_gazed);
            else if (_gazedComb >= 0) Wake(_gazedComb);
            else if (_gazedDoor)
            {
                Debug.Log("[LodgeRoom] Look + pinch on the door.");
                Close();
                swap.Return();
            }
            // A pinch on nothing (not a rim pod either) closes the card.
            else if (_card.Visible && !pods.IsGazed) Close();
        }
    }
}
