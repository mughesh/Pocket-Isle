using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Shard Gates on the current outdoor map (<see cref="WorldMap.gates"/>): each gate prop gets a portal surface
    /// (shader Diorama/Portal) in its opening. Walk into a gate from its front tiles, or look at it and pinch (the explorer
    /// walks to the front first), and the island travels to the linked map (<see cref="SceneSwap.Travel"/>) with the
    /// explorer arriving in front of that map's return gate. A sealed gate shows a stone seal: walking in or pinching
    /// gives a soft bump and the Spirit's <c>gate.sealed</c> line. Lines: a gate's first-seen line the first time it comes
    /// into the window, the arrival line of the map you travel to.
    /// Dialogue targets: <c>gate:fire</c> (any gate id) and <c>gate:last</c> (the gate last bumped or entered).
    /// </summary>
    public class ShardGates : MonoBehaviour
    {
        public IslandBuilder island;
        public WorldWindow window;
        public GestureTestPiece explorer;
        public SceneSwap swap;
        public PodRack pods;
        public BattleFx fx;
        [Tooltip("Diorama/Portal material.")]
        public Material portalMaterial;
        [Tooltip("Pinches from a hand that is grabbing or touching the rim are ignored.")]
        public IslandHandle grabGuard;
        [Tooltip("Step 10a: the wind gate stays sealed in builds until Galewick is finished (GalewickConfig.open).")]
        public GalewickConfig galewick;

        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int ShapeId = Shader.PropertyToID("_Shape");
        static readonly int ArchStartId = Shader.PropertyToID("_ArchStart");
        static readonly int SealedId = Shader.PropertyToID("_Sealed");
        static readonly int BumpId = Shader.PropertyToID("_Bump");
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        /// <summary>The portal opening of each gate look, in prop units (tiles) from the gate's centre on the ground.</summary>
        struct Look
        {
            public bool flat;       // lies flat (the well's water) instead of standing in an arch
            public Vector2 size;    // width, height (flat: width, depth)
            public float bottom;    // standing: bottom edge height; flat: surface height
            public float archStart; // 0..1 of the height where the round top starts
        }

        static Look LookFor(string id)
        {
            switch (id)
            {
                case "water": return new Look { flat = true, size = new Vector2(1.34f, 0.52f), bottom = 0.27f };
                case "wind": return new Look { size = new Vector2(0.86f, 0.8f), bottom = 0f, archStart = 0.37f };
                default: return new Look { size = new Vector2(1.04f, 1.48f), bottom = 0f, archStart = 0.63f };
            }
        }

        class Portal
        {
            public GateLink link;
            public Look look;
            public Transform quad;
            public MeshRenderer renderer;
            public float bump;
            public bool seen;
        }

        readonly List<Portal> _portals = new List<Portal>();
        readonly bool[] _wasPinching = { true, true };
        WorldMap _scanned;
        int _gazed = -1, _pending = -1, _last = -1;
        bool _entering;
        Mesh _quad;
        MaterialPropertyBlock _block;

        bool Outside => !SceneSwap.IsInside && !SceneSwap.IsBusy;

        void Start()
        {
            if (island == null || window == null || explorer == null || swap == null)
            {
                Debug.LogError("[ShardGates] Needs the island, world window, explorer and scene swap (Set Up MR_Main (8 hub)).", this);
                enabled = false;
                return;
            }
            if (portalMaterial == null) Debug.LogError("[ShardGates] No portal material: gates have no portal surface.", this);
            _quad = BattleHud.VerticalQuad();
            _block = new MaterialPropertyBlock();
            explorer.OnStep += OnStep;
            explorer.OnBump += OnBump;
            swap.TravelFinished += OnTravelFinished;
            Dialogue.RegisterTargetResolver("gate", arg => new Dialogue.Target
            {
                point = () => GatePoint(arg == "last" ? _last : _portals.FindIndex(p => p.link.id == arg)),
                pulse = k => { },
                radius = () => 1.1f * island.map.tileSize * island.Zoom * island.transform.lossyScale.x,
            });
            Scan();
        }

        void OnDestroy()
        {
            if (explorer != null)
            {
                explorer.OnStep -= OnStep;
                explorer.OnBump -= OnBump;
            }
            if (swap != null) swap.TravelFinished -= OnTravelFinished;
            if (_quad != null) Destroy(_quad);
        }

        void Scan()
        {
            if (!Outside || island.world == _scanned) return;
            _scanned = island.world;
            foreach (var p in _portals)
                if (p.quad != null) Destroy(p.quad.gameObject);
            _portals.Clear();
            _gazed = _pending = _last = -1;
            if (_scanned == null) return;
            foreach (var link in _scanned.gates)
            {
                var p = new Portal { link = link, look = LookFor(link.id) };
                if (portalMaterial != null)
                {
                    var go = new GameObject($"Gate Portal {link.id} c{link.tile.x} r{link.tile.y}");
                    go.transform.SetParent(island.transform, false);
                    go.AddComponent<MeshFilter>().sharedMesh = _quad;
                    p.renderer = go.AddComponent<MeshRenderer>();
                    p.renderer.sharedMaterial = portalMaterial;
                    p.renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    p.renderer.receiveShadows = false;
                    p.quad = go.transform;
                    go.SetActive(false);
                }
                _portals.Add(p);
            }
            Debug.Log($"[ShardGates] {_portals.Count} gate(s) on {_scanned.name}: " +
                      string.Join(", ", _portals.ConvertAll(p => $"{p.link.id} c{p.link.tile.x} r{p.link.tile.y} {(IsOpen(p.link) ? "-> " + p.link.to.name : "sealed")}")) + ".");
        }

        // ---- per frame ----

        void Update()
        {
            if (_quad == null) return;
            Scan();
            bool outside = Outside;
            _gazed = outside && !window.IsZoomed && !explorer.Locked && !_entering ? GazedGate() : -1;
            if (pods != null) pods.SetExternalGaze(this, _gazed >= 0);
            if (_gazed >= 0)
            {
                var c = GatePoint(_gazed);
                if (c.HasValue) FocusHighlight.Hover(c.Value, 1.1f * island.map.tileSize * island.Zoom * island.transform.lossyScale.x);
            }
            for (int i = 0; i < _portals.Count; i++) UpdatePortal(i, outside);
            if (outside) UpdatePinch();
        }

        Vector2 Center(GateLink g) => g.tile + new Vector2(0.5f, 0f);

        void UpdatePortal(int i, bool outside)
        {
            var p = _portals[i];
            float tile = island.map.tileSize;
            Vector3 ground = island.TileToLocal(Center(p.link), island.Content.capHeight);
            bool visible = outside && !window.IsZoomed && !Hidden(i) && island.FootprintContains(ground);
            if (visible && !p.seen && !explorer.Locked && !Onboarding.Running)
            {
                p.seen = true;
                if (!string.IsNullOrEmpty(p.link.firstSeenLine)) Dialogue.Say(p.link.firstSeenLine);
            }
            p.bump = Mathf.MoveTowards(p.bump, 0f, Time.deltaTime * 2.5f);
            if (p.quad == null) return;
            p.quad.gameObject.SetActive(visible);
            if (!visible) return;
            float z = island.Zoom;
            var look = p.look;
            if (look.flat)
            {
                p.quad.localPosition = ground + Vector3.up * (look.bottom * tile * z);
                p.quad.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            else
            {
                p.quad.localPosition = ground + Vector3.up * ((look.bottom + look.size.y * 0.5f) * tile * z);
                p.quad.localRotation = Quaternion.identity;
            }
            p.quad.localScale = new Vector3(look.size.x * tile * z, look.size.y * tile * z, 1f);
            _block.Clear();
            _block.SetColor(ColorId, p.link.portal);
            _block.SetFloat(ShapeId, look.flat ? 1f : 0f);
            _block.SetFloat(ArchStartId, look.archStart);
            _block.SetFloat(SealedId, IsOpen(p.link) ? 0f : 1f);
            _block.SetFloat(BumpId, p.bump);
            _block.SetFloat(IntensityId, _gazed == i ? 1.25f : 1f);
            p.renderer.SetPropertyBlock(_block);
        }

        /// <summary>A gate whose prop the map hides for now (step 9c: the temple's return gate before the Keeper is calmed).</summary>
        bool Hidden(int i) => _scanned != null && _scanned.IsPropHidden(_portals[i].link.tile);

        Vector3? GatePoint(int i)
        {
            if (this == null || i < 0 || i >= _portals.Count || !Outside || Hidden(i)) return null;
            var p = _portals[i];
            float h = p.look.flat ? p.look.bottom + 0.2f : p.look.bottom + p.look.size.y * 0.5f;
            var local = island.TileToLocal(Center(p.link), island.Content.capHeight + h * island.map.tileSize);
            return island.FootprintContains(local) ? island.transform.TransformPoint(local) : (Vector3?)null;
        }

        int GazedGate()
        {
            if (!HeadGaze.TryGetRay(out var ray)) return -1;
            float tile = island.transform.lossyScale.x * island.map.tileSize * island.Zoom;
            var pick = new HeadGaze.ConePick(ray, _gazed);
            for (int i = 0; i < _portals.Count; i++)
            {
                var c = GatePoint(i);
                if (c.HasValue) pick.Consider(i, c.Value, Mathf.Max(HeadGaze.MinRadius * island.transform.lossyScale.x, 0.9f * tile));
            }
            return pick.Best;
        }

        void UpdatePinch()
        {
            for (int h = 0; h < 2; h++)
            {
                bool left = h == 0;
                var hand = HandRegistry.Get(left ? Handedness.Left : Handedness.Right);
                bool pinching = hand != null && hand.IsTrackedDataValid && hand.GetIndexFingerIsPinching();
                bool busy = grabGuard != null && grabGuard.IsHandBusy(left);
                if (pinching && !_wasPinching[h] && !busy && _gazed >= 0) Pick(_gazed, $"look + pinch ({(left ? "left" : "right")})");
                _wasPinching[h] = pinching;
            }
        }

        bool AtFront(GateLink g, Vector2Int tile) => tile == g.Front(0) || tile == g.Front(1);

        /// <summary>Open, and (step 10a) the wind gate only while Galewick is allowed (the Editor, or a build after 10d).</summary>
        public bool IsOpen(GateLink g) => g.Open && (g.id != "wind" || galewick == null || galewick.IsOpen);

        /// <summary>Look + pinch on a gate (also Step8Debug): use it from its front, otherwise walk there first.</summary>
        public void Pick(int index, string how)
        {
            if (index < 0 || index >= _portals.Count || _entering || !Outside) return;
            var g = _portals[index].link;
            if (AtFront(g, explorer.Tile))
            {
                Debug.Log($"[ShardGates] {how} on the {g.id} gate.");
                Use(index);
                return;
            }
            var map = island.Content;
            Vector2Int? best = null;
            for (int side = 0; side < 2; side++)
            {
                var t = g.Front(side);
                if (map.CanWalk(t) && explorer.CanReach(t) && (!best.HasValue || (t - explorer.Tile).sqrMagnitude < (best.Value - explorer.Tile).sqrMagnitude))
                    best = t;
            }
            if (best.HasValue && explorer.GoTo(best.Value))
            {
                _pending = index;
                Debug.Log($"[ShardGates] {how} on the {g.id} gate: walking to c{best.Value.x} r{best.Value.y} first.");
            }
            else Debug.Log($"[ShardGates] {how} on the {g.id} gate: no way there.");
        }

        /// <summary>The gate index with this id on the current map, or -1 (debug).</summary>
        public int IndexOf(string id) => _portals.FindIndex(p => p.link.id == id);

        void OnStep(Vector2Int tile)
        {
            if (_pending < 0 || _pending >= _portals.Count) return;
            if (explorer.Destination.HasValue && explorer.Destination.Value != tile) return;
            int i = _pending;
            _pending = -1;
            if (AtFront(_portals[i].link, tile)) Use(i);
        }

        /// <summary>Walking into a gate from its front tiles uses it.</summary>
        void OnBump(Vector2Int blocked)
        {
            if (_entering || !Outside) return;
            for (int i = 0; i < _portals.Count; i++)
            {
                var g = _portals[i].link;
                if (!g.Covers(blocked) || explorer.Tile != blocked + Vector2Int.up) continue;
                Debug.Log($"[ShardGates] Walked into the {g.id} gate.");
                Use(i);
                return;
            }
        }

        void Use(int i)
        {
            if (Hidden(i)) return;
            _last = i;
            var p = _portals[i];
            if (!IsOpen(p.link))
            {
                p.bump = 1f;
                Dialogue.Say("gate.sealed");
                Debug.Log($"[ShardGates] The {p.link.id} gate is sealed: bump.");
                return;
            }
            StartCoroutine(Enter(i));
        }

        IEnumerator Enter(int i)
        {
            if (_entering) yield break;
            _entering = true;
            var link = _portals[i].link;
            explorer.Locked = true;
            var c = GatePoint(i);
            if (fx != null && c.HasValue)
            {
                float scale = island.transform.lossyScale.x * island.map.tileSize * island.Zoom;
                fx.Burst(c.Value, 30, link.portal, scale * 0.8f, 0.8f, scale * 0.06f, Vector3.up * scale * 0.4f);
            }
            Debug.Log($"[ShardGates] Through the {link.id} gate to {link.to.name} (arrive c{link.arrive.x} r{link.arrive.y}).");
            yield return new WaitForSeconds(0.35f);
            explorer.Locked = false;
            if (!swap.Travel(link.to, link.arrive, link.arriveFacing)) Debug.LogError($"[ShardGates] Travel to {link.to.name} refused (busy or inside).", this);
            while (SceneSwap.IsBusy) yield return null;
            _entering = false;
        }

        void OnTravelFinished(WorldMap to)
        {
            if (to != null && !string.IsNullOrEmpty(to.arrivalLine)) Dialogue.Say(to.arrivalLine);
        }
    }
}
