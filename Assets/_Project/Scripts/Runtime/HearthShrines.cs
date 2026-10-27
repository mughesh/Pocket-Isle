using System;
using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Hearth Shrines on the world map: 2 x 2 props whose recipe is <see cref="RecipeName"/>, each with a soft flame
    /// beacon above it (seen from across the window). Standing on any tile touching a shrine, its flame leans toward
    /// the player and brightens and a glow ring appears over the bowl (the first time the Spirit says <c>shrine.near</c>).
    /// To go in: hold a palm over the bowl, facing down, for <see cref="holdSeconds"/> (the ring fills), or look + pinch
    /// the shrine. Both work whenever the shrine is on the island: from farther away the explorer walks there by itself,
    /// then goes in. Walking up to it no longer goes in (step 8c: that left the palm magic unused). Then the flame
    /// flares, a warm wave heals the rim pods, <see cref="Rested"/> fires (the save
    /// listens) and the island swaps into the Hearth Lodge (<see cref="SceneSwap"/>). The last shrine used is where
    /// the explorer returns after losing (<see cref="RespawnTile"/>, outside).
    /// The Spirit's <c>shrine.first</c> line plays the first time a shrine comes into the window.
    /// </summary>
    public class HearthShrines : MonoBehaviour
    {
        public const string RecipeName = "HearthShrine";

        public IslandBuilder island;
        public WorldWindow window;
        public GestureTestPiece explorer;
        public Party party;
        public PodRack pods;
        public BattleFx fx;
        public SceneSwap swap;
        [Tooltip("The interior a shrine leads to.")]
        public WorldMap lodge;
        [Tooltip("Diorama/Beacon material (flame column and glow ring).")]
        public Material beaconMaterial;
        [Tooltip("Pinches from a hand that is grabbing or touching the rim are ignored; palms only while it holds the island.")]
        public IslandHandle grabGuard;

        [Header("Shape (prop tile units, 1 = one tile)")]
        [Tooltip("Height of the flame bowl's rim above the cap.")]
        public float bowlHeight = 1.42f;
        [Tooltip("Beacon column length (island-local metres).")]
        public float beaconHeight = 0.22f;
        public float beaconWidth = 0.04f;

        [Header("Warm your hand")]
        [Tooltip("Palm facing down over the bowl: facing, height above the bowl rim, horizontal radius (scaled with the island).")]
        public PalmHover palm = new PalmHover();
        public float holdSeconds = 0.8f;
        [Tooltip("Step 9h: a ghost palm hovers over the flame while the explorer stands at a shrine, until the lodge was visited once.")]
        public GhostHands ghosts;
        public string ghostClip = "palm_hover";

        /// <summary>The explorer rested at this shrine (anchor tile): healed, time to save.</summary>
        public event Action<Vector2Int> Rested;

        /// <summary>The last shrine used (anchor tile), or null. Saved and loaded with the game.</summary>
        public Vector2Int? LastShrine { get; set; }

        /// <summary>The map the last shrine is on (world map name). Saved and loaded with the game.</summary>
        public string LastShrineMap { get; set; }

        /// <summary>Where the explorer goes after losing on this map: in front of its last shrine, or the world's start tile.</summary>
        public Vector2Int RespawnTile => RespawnOn(OutdoorMap as WorldMap) ?? explorer.SpawnTile;

        /// <summary>The walkable tile in front of the last shrine if it is on <paramref name="map"/>, else null.</summary>
        Vector2Int? RespawnOn(WorldMap map)
        {
            if (!LastShrine.HasValue || map == null || map.name != LastShrineMap) return null;
            var s = LastShrine.Value;
            if (!map.TryGetProp(map.PropAt(s.y, s.x), out var e) || e.recipe == null || e.recipe.name != RecipeName) return null;
            var front = s + new Vector2Int(0, 2);
            if (map.CanWalk(front)) return front;
            foreach (var t in Touching(s))
                if (map.CanWalk(t)) return t;
            return null;
        }

        /// <summary>
        /// Losing everything: back in front of the last shrine, on its own map (travelling there at once if needed), or
        /// the current map's start tile without one.
        /// </summary>
        public void Respawn()
        {
            var here = OutdoorMap as WorldMap;
            var shrineMap = swap != null && !string.IsNullOrEmpty(LastShrineMap) ? swap.FindWorld(LastShrineMap) : null;
            if (shrineMap != null && shrineMap != here && RespawnOn(shrineMap) is Vector2Int there)
            {
                swap.TravelNow(shrineMap, there, new Vector2Int(0, -1));
                Debug.Log($"[HearthShrines] Respawn at the shrine on {shrineMap.name} c{there.x} r{there.y}.");
                return;
            }
            if (!string.IsNullOrEmpty(LastShrineMap) && shrineMap == null)
                Debug.LogError($"[HearthShrines] The last shrine's map '{LastShrineMap}' isn't in the scene swap's worlds: respawning here.", this);
            explorer.Teleport(RespawnTile);
        }

        public IReadOnlyList<Vector2Int> Shrines => _shrines;

        // ---- step 10f: a dying flame ----

        static readonly HashSet<(string, Vector2Int)> _dormant = new HashSet<(string, Vector2Int)>();

        /// <summary>
        /// Step 10f: a shrine whose flame is nearly out (Cinderwood's village, <see cref="FlameCupPiece"/>): no beacon, no
        /// ring, no ghost palm; it can't be rested at (heal, save, lodge) until it's rekindled. Keyed by map name + anchor.
        /// </summary>
        public static void SetDormant(string map, Vector2Int anchor, bool dormant)
        {
            if (string.IsNullOrEmpty(map)) return;
            if (dormant) _dormant.Add((map, anchor));
            else _dormant.Remove((map, anchor));
        }

        public static bool IsDormant(string map, Vector2Int anchor) => map != null && _dormant.Contains((map, anchor));

        static readonly Dictionary<(string, Vector2Int), float> _dormantFill = new Dictionary<(string, Vector2Int), float>();

        /// <summary>Step 10f: how far a dormant shrine's flame is warmed (0..1): its ring shows and fills while it's cupped.</summary>
        public static void SetDormantFill(string map, Vector2Int anchor, float fill)
        {
            if (string.IsNullOrEmpty(map)) return;
            if (fill <= 0f) _dormantFill.Remove((map, anchor));
            else _dormantFill[(map, anchor)] = Mathf.Clamp01(fill);
        }

        static float DormantFill(string map, Vector2Int anchor) => map != null && _dormantFill.TryGetValue((map, anchor), out float f) ? f : 0f;

        [Tooltip("Step 10f: said once when someone tries to rest at a dormant shrine.")]
        public string dormantLine = "cw.flame.cold";

        bool Dormant(int i) => i >= 0 && i < _shrines.Count && _scanned != null && IsDormant(_scanned.name, _shrines[i]);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetDormant()
        {
            _dormant.Clear();
            _dormantFill.Clear();
        }

        class Beacon
        {
            public Transform column, ring;
            public MeshRenderer columnRenderer, ringRenderer;
            public float near, hold;
        }

        static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        static readonly int FillId = Shader.PropertyToID("_Fill");
        static readonly int ModeId = Shader.PropertyToID("_Mode");

        readonly List<Vector2Int> _shrines = new List<Vector2Int>();
        readonly List<Beacon> _beacons = new List<Beacon>();
        readonly bool[] _wasPinching = { true, true };
        WorldMap _scanned;
        int _scannedVersion = -1;
        int _gazed = -1, _pending = -1;
        bool _entering;
        Mesh _quad;
        MaterialPropertyBlock _block;

        /// <summary>The outdoor map (the shrines live there, also while the lodge shows).</summary>
        IslandMap OutdoorMap => swap != null && swap.Outdoor != null ? swap.Outdoor : island.Content;

        bool Outside => !SceneSwap.IsInside && !SceneSwap.IsBusy;

        static Vector2 Center(Vector2Int shrine) => shrine + new Vector2(0.5f, 0.5f);

        /// <summary>The 12 tiles around a 2 x 2 shrine (corners included).</summary>
        static IEnumerable<Vector2Int> Touching(Vector2Int s)
        {
            for (int dy = -1; dy <= 2; dy++)
                for (int dx = -1; dx <= 2; dx++)
                    if (dx < 0 || dx > 1 || dy < 0 || dy > 1) yield return s + new Vector2Int(dx, dy);
        }

        void Start()
        {
            if (island == null || explorer == null || party == null || pods == null)
            {
                Debug.LogError("[HearthShrines] Needs the island, explorer, party and pod rack (Set Up MR_Main (7b lodge)).", this);
                enabled = false;
                return;
            }
            if (swap == null || lodge == null) Debug.LogError("[HearthShrines] No scene swap or lodge map: shrines heal and save but lead nowhere.", this);
            if (beaconMaterial == null) Debug.LogError("[HearthShrines] No beacon material: shrines have no flame beacon.", this);
            _quad = BattleHud.VerticalQuad();
            _block = new MaterialPropertyBlock();
            explorer.OnStep += OnStep;
            Dialogue.RegisterTarget("shrine", ShrinePoint, k => { }, () => 1.6f * island.map.tileSize * island.Zoom * island.transform.lossyScale.x);
            // Where the ghost's palm hovers: over the bowl of the shrine the explorer stands at, at the palm check's height.
            Dialogue.RegisterTarget("shrineFlame", () => _ghostShrine >= 0 && _ghostShrine < _shrines.Count
                ? island.transform.TransformPoint(island.TileToLocal(Center(_shrines[_ghostShrine]), island.Content.capHeight + bowlHeight * island.map.tileSize)) +
                  island.transform.up * ((palm.height.x + palm.height.y) * 0.5f * island.transform.lossyScale.x)
                : (Vector3?)null, k => { }, () => 0f);
            Scan();
        }

        void OnDestroy()
        {
            if (explorer != null)
            {
                explorer.OnStep -= OnStep;
            }
            if (_quad != null) Destroy(_quad);
        }

        void Scan()
        {
            var world = Outside ? island.world : swap != null ? swap.Outdoor : null;
            if (world == null || world == _scanned && world.version == _scannedVersion) return;
            _scanned = world;
            _scannedVersion = world.version;
            _shrines.Clear();
            for (int r = 0; r < world.Rows; r++)
                for (int c = 0; c < world.Columns; c++)
                    if (world.TryGetProp(world.PropAt(r, c), out var e) && e.recipe != null && e.recipe.name == RecipeName)
                        _shrines.Add(new Vector2Int(c, r));
            foreach (var b in _beacons)
            {
                if (b.column != null) Destroy(b.column.gameObject);
                if (b.ring != null) Destroy(b.ring.gameObject);
            }
            _beacons.Clear();
            foreach (var s in _shrines) _beacons.Add(MakeBeacon(s));
            Debug.Log($"[HearthShrines] {_shrines.Count} shrine(s) on {world.name}: {string.Join(", ", _shrines.ConvertAll(s => $"c{s.x} r{s.y}"))}.");
        }

        Beacon MakeBeacon(Vector2Int s)
        {
            var b = new Beacon();
            if (beaconMaterial == null) return b;
            b.column = Quad($"Shrine Beacon c{s.x} r{s.y}", out b.columnRenderer, 0f);
            b.ring = Quad($"Shrine Ring c{s.x} r{s.y}", out b.ringRenderer, 1f);
            return b;
        }

        Transform Quad(string name, out MeshRenderer r, float mode)
        {
            var go = new GameObject(name);
            go.transform.SetParent(island.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = _quad;
            r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = beaconMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            _block.Clear();
            _block.SetFloat(ModeId, mode);
            r.SetPropertyBlock(_block);
            go.SetActive(false);
            return go.transform;
        }

        // ---- per frame ----

        void Update()
        {
            if (_quad == null) return;
            Scan();
            // A walk to a shrine that got cancelled (a swipe, a new destination) no longer goes in.
            if (_pending >= 0 && !explorer.Destination.HasValue) _pending = -1;
            bool outside = Outside;
            if (!outside) pods.SetExternalGaze(this, false);
            _gazed = outside && !window.IsZoomed && !explorer.Locked ? GazedShrine() : -1;
            pods.SetExternalGaze(this, _gazed >= 0);
            if (_gazed >= 0)
                FocusHighlight.Hover(island.transform.TransformPoint(island.TileToLocal(Center(_shrines[_gazed]), island.Content.capHeight)),
                    1.2f * island.map.tileSize * island.Zoom * island.transform.lossyScale.x);
            int near = -1;
            for (int i = 0; i < _shrines.Count; i++)
            {
                UpdateShrine(i, outside);
                if (_beacons[i].near > 0.5f && near < 0) near = i;
            }
            if (outside) UpdatePinch();
            UpdateGhost(near);
        }

        int _ghostShrine = -1;

        /// <summary>
        /// Step 9h: while the explorer stands at a shrine (and no lesson owns the ghost), a ghost palm hovers over its flame,
        /// until the player has been inside the lodge once.
        /// </summary>
        void UpdateGhost(int near)
        {
            if (ghosts == null) return;
            bool visited = Dialogue.HasSaid("lodge.first") || Dialogue.HasSaid("lodge.first.empty");
            int want = near >= 0 && !visited && !Onboarding.Running && !_entering ? near : -1;
            if (want == _ghostShrine) return;
            _ghostShrine = want;
            if (want >= 0)
            {
                ghosts.Show(ghostClip, GhostPlace.AtTarget, "shrineFlame", 1f, DominantHand.Left, false);
                Debug.Log("[HearthShrines] Ghost palm over the flame.");
            }
            else if (ghosts.ClipName == ghostClip && !ghosts.previewing) ghosts.Hide();
        }

        void UpdateShrine(int i, bool outside)
        {
            var s = _shrines[i];
            var b = _beacons[i];
            Vector3 local = island.TileToLocal(Center(s), island.Content.capHeight + bowlHeight * island.map.tileSize);
            bool dormant = Dormant(i);
            bool visible = outside && !window.IsZoomed && island.FootprintContains(local);
            if (visible && !explorer.Locked && !dormant) Dialogue.Say("shrine.first");
            bool usable = visible && !_entering && !explorer.Locked;
            if (dormant)
            {
                // Step 10f: the flame is nearly out (FlameCupPiece draws it). Trying to rest only gets the Spirit's line.
                b.near = 0f;
                bool resting = usable && palm.IsOver(island.transform.TransformPoint(local), island.transform.up, grabGuard, null);
                b.hold = resting ? b.hold + Time.deltaTime : 0f;
                if (b.hold >= holdSeconds * 2f && !string.IsNullOrEmpty(dormantLine)) Dialogue.Say(dormantLine);
                if (b.column != null)
                {
                    b.column.gameObject.SetActive(false);
                    float fill = DormantFill(_scanned.name, s);
                    b.ring.gameObject.SetActive(visible && fill > 0.01f);
                    if (visible && fill > 0.01f)
                    {
                        b.ring.localPosition = local + Vector3.up * (island.map.tileSize * 0.25f);
                        b.ring.localRotation = Quaternion.Euler(90f, 0f, 0f);
                        b.ring.localScale = Vector3.one * island.map.tileSize * 1.1f;
                        _block.Clear();
                        _block.SetFloat(ModeId, 1f);
                        _block.SetFloat(FillId, fill);
                        _block.SetFloat(IntensityId, 1.4f);
                        b.ringRenderer.SetPropertyBlock(_block);
                    }
                }
                return;
            }
            bool near = usable && IsTouching(s, explorer.Tile);
            b.near = Mathf.MoveTowards(b.near, near ? 1f : 0f, Time.deltaTime * 3f);
            if (near) Dialogue.Say("shrine.near");

            // Warm your hand: the ring fills while a palm faces down over the bowl, wherever the explorer is.
            bool warming = usable && _pending != i && palm.IsOver(island.transform.TransformPoint(local), island.transform.up, grabGuard, "flame");
            b.hold = warming ? b.hold + Time.deltaTime : Mathf.MoveTowards(b.hold, 0f, Time.deltaTime * 2f);
            if (b.hold >= holdSeconds)
            {
                b.hold = 0f;
                // Next to it: in at once. Farther away: the explorer walks there, then goes in (as with look + pinch).
                Pick(i, "palm over the flame");
            }

            if (b.column == null) return;
            b.column.gameObject.SetActive(visible);
            // The ring shows next to it, while a palm fills it from afar, and while the explorer walks there.
            float ringOn = Mathf.Max(b.near, Mathf.Clamp01(b.hold / Mathf.Max(0.01f, holdSeconds) * 4f), _pending == i ? 1f : 0f);
            b.ring.gameObject.SetActive(visible && ringOn > 0.01f);
            if (!visible) return;
            var cam = Camera.main;
            Vector3 up = Vector3.up;
            // Billboard around the island's up; leans toward the head when the explorer stands next to it.
            Quaternion face = Quaternion.identity;
            if (cam != null)
            {
                Vector3 toCam = island.transform.InverseTransformPoint(cam.transform.position) - local;
                Vector3 flat = new Vector3(toCam.x, 0f, toCam.z);
                if (flat.sqrMagnitude > 1e-8f)
                {
                    face = Quaternion.LookRotation(-flat.normalized, up);
                    face = Quaternion.AngleAxis(-18f * b.near, face * Vector3.right) * face;
                }
            }
            b.column.localRotation = face;
            float pulse = 1f + 0.08f * Mathf.Sin(Time.time * 1.6f + i);
            b.column.localScale = new Vector3(beaconWidth * (1f + 0.4f * b.near), beaconHeight * pulse * (1f + 0.25f * b.near), 1f);
            b.column.localPosition = local + face * Vector3.up * (beaconHeight * 0.5f);
            _block.Clear();
            _block.SetFloat(ModeId, 0f);
            _block.SetFloat(IntensityId, (_gazed == i ? 1.6f : 1f) * (1.1f + 0.9f * b.near));
            b.columnRenderer.SetPropertyBlock(_block);

            float ring = island.map.tileSize * 1.1f;
            b.ring.localPosition = local + Vector3.up * (island.map.tileSize * 0.25f);
            b.ring.localRotation = Quaternion.Euler(90f, 0f, 0f);
            b.ring.localScale = Vector3.one * ring * (0.6f + 0.4f * ringOn);
            _block.Clear();
            _block.SetFloat(ModeId, 1f);
            _block.SetFloat(FillId, _pending == i ? 1f : Mathf.Clamp01(b.hold / holdSeconds));
            _block.SetFloat(IntensityId, ringOn * 1.4f);
            b.ringRenderer.SetPropertyBlock(_block);
        }

        static bool IsTouching(Vector2Int s, Vector2Int tile)
        {
            int dx = tile.x - s.x, dy = tile.y - s.y;
            return dx >= -1 && dx <= 2 && dy >= -1 && dy <= 2 && !(dx >= 0 && dx <= 1 && dy >= 0 && dy <= 1);
        }

        int GazedShrine()
        {
            if (!HeadGaze.TryGetRay(out var ray)) return -1;
            float tile = island.transform.lossyScale.x * island.map.tileSize * island.Zoom;
            var pick = new HeadGaze.ConePick(ray, _gazed);
            for (int i = 0; i < _shrines.Count; i++)
            {
                if (Dormant(i)) continue;
                Vector3 local = island.TileToLocal(Center(_shrines[i]), island.Content.capHeight + 0.8f * island.map.tileSize);
                if (!island.FootprintContains(local)) continue;
                pick.Consider(i, island.transform.TransformPoint(local), Mathf.Max(HeadGaze.MinRadius * island.transform.lossyScale.x, 0.9f * tile));
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

        /// <summary>Look + pinch on a shrine (also BattleDebug): enter if next to it, otherwise walk there first.</summary>
        public void Pick(int index, string how)
        {
            if (index < 0 || index >= _shrines.Count || _entering || !Outside) return;
            var s = _shrines[index];
            if (Dormant(index))
            {
                Debug.Log($"[HearthShrines] {how} on the shrine c{s.x} r{s.y}: its flame is nearly out (rekindle it first).");
                if (!string.IsNullOrEmpty(dormantLine)) Dialogue.Say(dormantLine);
                return;
            }
            if (IsTouching(s, explorer.Tile))
            {
                Debug.Log($"[HearthShrines] {how} on the shrine c{s.x} r{s.y}.");
                StartCoroutine(Enter(s));
                return;
            }
            Vector2Int? best = null;
            foreach (var t in Touching(s))
                if (OutdoorMap.CanWalk(t) && explorer.CanReach(t) && (!best.HasValue || (t - explorer.Tile).sqrMagnitude < (best.Value - explorer.Tile).sqrMagnitude))
                    best = t;
            if (best.HasValue && explorer.GoTo(best.Value))
            {
                _pending = index;
                Debug.Log($"[HearthShrines] {how} on the shrine c{s.x} r{s.y}: walking to c{best.Value.x} r{best.Value.y} first.");
            }
            else Debug.Log($"[HearthShrines] {how} on the shrine c{s.x} r{s.y}: no way there.");
        }

        void OnStep(Vector2Int tile)
        {
            if (_pending < 0 || _pending >= _shrines.Count) return;
            if (explorer.Destination.HasValue && explorer.Destination.Value != tile) return;
            var s = _shrines[_pending];
            int pending = _pending;
            _pending = -1;
            if (IsTouching(s, tile) && !Dormant(pending)) StartCoroutine(Enter(s));
        }

        /// <summary>Flame flare, heal wave, save (Rested), then the swap into the lodge.</summary>
        IEnumerator Enter(Vector2Int shrine)
        {
            if (_entering) yield break;
            _entering = true;
            explorer.Locked = true;
            LastShrine = shrine;
            LastShrineMap = OutdoorMap != null ? OutdoorMap.name : null;
            party.HealEveryone();
            pods.WarmWave();
            StartCoroutine(Flare(shrine));
            Debug.Log($"[HearthShrines] Rested at the shrine c{shrine.x} r{shrine.y}: everyone healed; respawn point; into the lodge.");
            Rested?.Invoke(shrine);
            yield return new WaitForSeconds(0.7f);
            explorer.Locked = false;
            if (swap != null && lodge != null) swap.Enter(lodge, lodge.startTile);
            while (SceneSwap.IsBusy) yield return null;
            _entering = false;
        }

        Vector3 FlameWorld(Vector2Int shrine) =>
            island.transform.TransformPoint(island.TileToLocal(Center(shrine), island.Content.capHeight + (bowlHeight + 0.15f) * island.map.tileSize));

        /// <summary>The flame flares: a bright orb swells and fades, embers fly up.</summary>
        IEnumerator Flare(Vector2Int shrine)
        {
            if (fx == null) yield break;
            float scale = island.transform.lossyScale.x * island.map.tileSize * island.Zoom;
            var warm = new Color(1f, 0.7f, 0.3f);
            fx.Burst(FlameWorld(shrine), 36, warm, scale * 0.7f, 0.9f, scale * 0.07f, Vector3.up * scale * 0.9f);
            var orb = fx.Get(BattleFx.Shape.Orb);
            if (orb == null) yield break;
            orb.Set(Color.white, warm, 1f, 1.5f, 1f, 0.3f);
            for (float t = 0f; t < 0.7f; t += Time.deltaTime)
            {
                float u = t / 0.7f;
                orb.transform.position = FlameWorld(shrine);
                orb.transform.localScale = Vector3.one * (scale * (0.4f + 1.1f * Mathf.Sin(u * Mathf.PI)) / Mathf.Max(1e-4f, fx.transform.lossyScale.x));
                orb.SetAlpha(1f - u * u);
                yield return null;
            }
            fx.Release(orb);
        }

        Vector3? ShrinePoint()
        {
            if (this == null || _shrines.Count == 0 || !Outside) return null;
            Vector3? best = null;
            float bestSq = float.MaxValue;
            foreach (var s in _shrines)
            {
                Vector3 local = island.TileToLocal(Center(s), island.Content.capHeight + bowlHeight * island.map.tileSize);
                if (!island.FootprintContains(local)) continue;
                float d = (Center(s) - explorer.Tile).sqrMagnitude;
                if (d < bestSq)
                {
                    bestSq = d;
                    best = island.transform.TransformPoint(local);
                }
            }
            return best;
        }
    }
}
