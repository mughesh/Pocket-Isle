using System.Collections;
using System.Collections.Generic;
using Diorama.Audio;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Cinderwood's forge (step 9b), the scene side of a <see cref="PumpPiece"/>: bellows → pipes → braziers → door.
    /// Each pump squashes the bellows (they follow the finger poking the pad) with a puff, and a glow pulse runs along the pipe to the
    /// next unlit brazier, whose coals brighten a step (4 pumps per brazier). The 4th lights it with a flame burst and a
    /// cheer (<see cref="brazierLines"/>). Cooling takes coals back a step, never a lit brazier. All three lit: the
    /// forge door glows from inside, its iron bands heat up, it sinks into the ground with steam and sparks, the Spirit
    /// says <see cref="openLine"/> and the door tiles open (saved). Walking onto them goes into the Forge Hall
    /// (<see cref="hall"/>); coming out lands in front of the door facing away.
    /// The island doesn't draw the map's bellows, braziers and door: this draws them (<see cref="PropView"/>).
    /// Step 10f: while the firebox is cold (<see cref="ForgeEmberPiece"/>, <see cref="PumpPiece.Blocked"/>) a pump only
    /// gives a weak grey puff and the Spirit's <see cref="coldLine"/>.
    /// </summary>
    public class ForgeBellows : MonoBehaviour
    {
        public PumpPiece pump;
        public IslandBuilder island;
        public GestureTestPiece explorer;
        public SceneSwap swap;
        public BattleFx fx;

        [Header("Map anchors (props grid: column, row)")]
        public Vector2Int bellows;
        [Tooltip("Lit in this order (nearest the bellows first). Step 9f: east of the door, over the arch, west of it.")]
        public Vector2Int[] braziers = new Vector2Int[3];
        [Tooltip("Step 9f: per brazier, x = how far back toward the cliff and y = how high it stands (tiles). Mugesh, Oct 26: " +
                 "they sit round the door (right, over the arch, left) so all three stay in the window.")]
        public Vector2[] brazierLift = new Vector2[0];
        [Tooltip("The forge door (3 x 1, its left tile).")]
        public Vector2Int door;

        [Header("Looks")]
        public PropRecipe bodyRecipe;
        public PropRecipe brazierCold, brazierWarm, brazierLit;
        public PropRecipe doorFrame, doorLeaf;

        [Header("Pipe (tiles, matches PropKit's ForgePipe)")]
        public float pipeHeight = 0.72f;
        public float pipeBack = 0.38f;
        [Tooltip("The bellows' nozzle in its own shape units (+X = west).")]
        public Vector3 nozzle = new Vector3(0.38f, 0.3f, -0.3f);
        [Tooltip("Step 9f: the pipe climbs the door's sides and runs over the arch at this height (tiles), this far out from the frame's edges.")]
        public float pipeOverDoor = 2.95f;
        public float pipeDoorMargin = 0.75f;
        [Tooltip("Glow pulse speed along the pipe, tiles per second.")]
        public float pulseSpeed = 8f;

        [Header("Door")]
        public float glowSeconds = 1.6f;
        public float openSeconds = 2.6f;
        [Tooltip("How far the door sinks (tiles): all the way under the cap.")]
        public float sinkDepth = 2.9f;
        [Tooltip("Iron and inside glow while braziers are lit, before it opens (0..1 per lit brazier share).")]
        public float preheat = 0.35f;

        [Header("Into the forge")]
        [Tooltip("The Forge Hall interior. Empty = the doorway leads nowhere yet.")]
        public WorldMap hall;
        public Vector2Int hallEntry = new Vector2Int(4, 8);

        [Header("Lines")]
        public string[] brazierLines = { "cinder.brazier.one", "cinder.brazier.two", "cinder.brazier.three" };
        public string openLine = "cinder.forge.open";
        [Tooltip("Step 10f: pumped while the firebox is cold (said at most every few seconds).")]
        public string coldLine = "cw.forge.cold";
        [Range(0f, 1f)] public float volume = 0.7f;

        PropView _body, _frame, _leaf;
        PropView[] _braziers;
        AudioSource _audio;
        int _shown;          // strokes the braziers show (a pulse has to arrive first)
        float _doorHeat, _sink;
        bool _open, _opening;
        bool _warnedHall;
        float _coldSaidAt = -99f;

        WorldMap Map => pump != null ? pump.map : null;
        int PerBrazier => Mathf.Max(1, pump.checkpoint);

        void Start()
        {
            if (pump == null || island == null || Map == null || braziers == null || braziers.Length == 0)
            {
                Debug.LogError("[ForgeBellows] Needs its PumpPiece (with a map), the island and the brazier tiles (Set Up MR_Main (9b puzzles)).", this);
                enabled = false;
                return;
            }
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
            _body = new PropView("Forge Bellows Body", island, bodyRecipe);
            _frame = new PropView("Forge Door Frame", island, doorFrame);
            _leaf = new PropView("Forge Door Leaf", island, doorLeaf);
            _braziers = new PropView[braziers.Length];
            for (int i = 0; i < braziers.Length; i++) _braziers[i] = new PropView($"Forge Brazier {i}", island, brazierCold);
            pump.Stroked += OnStroked;
            pump.Cooled += OnCooled;
            pump.BlockedStroke += OnBlockedStroke;
            if (explorer != null) explorer.OnStep += OnStep;
            PuzzleState.Restored += Apply;
            Apply();
        }

        void OnDestroy()
        {
            if (pump != null)
            {
                pump.Stroked -= OnStroked;
                pump.Cooled -= OnCooled;
                pump.BlockedStroke -= OnBlockedStroke;
            }
            if (explorer != null) explorer.OnStep -= OnStep;
            PuzzleState.Restored -= Apply;
            _body?.Destroy();
            _frame?.Destroy();
            _leaf?.Destroy();
            if (_braziers != null)
                foreach (var b in _braziers) b?.Destroy();
            var map = Map;
            if (map == null) return;
            map.SetPropHidden(bellows, false);
            foreach (var b in braziers) map.SetPropHidden(b, false);
            map.SetPropHidden(door, false);
            map.SetPropOpen(door, false);
        }

        /// <summary>The map's copies hidden (drawn here); braziers, door heat and the doorway from the saved state.</summary>
        void Apply()
        {
            StopAllCoroutines();
            var map = Map;
            map.SetPropHidden(bellows, true);
            foreach (var b in braziers) map.SetPropHidden(b, true);
            map.SetPropHidden(door, true);
            _shown = pump.Strokes;
            _open = pump.IsSolved;
            _opening = false;
            _doorHeat = _open ? 1f : preheat * pump.Checkpoints / braziers.Length;
            _sink = _open ? 1f : 0f;
            map.SetPropOpen(door, _open);
            Debug.Log($"[ForgeBellows] {map.name}: {pump.Checkpoints}/{braziers.Length} braziers lit, door {(_open ? "open" : "shut")}.");
        }

        // ---- per frame ----

        void Update()
        {
            if (_body == null) return;
            bool here = pump.Here;
            float t = pump.Travel;
            // The bellows follow the finger: the pad's top goes down by the press travel, the bag bulges a little.
            float squash = pump.padHeight > 1e-3f ? pump.pressTravel * t / pump.padHeight : 0f;
            _body.Place(bellows, 0f, Quaternion.identity, here, new Vector3(1f + 0.08f * t, 1f - squash, 1f + 0.08f * t));
            for (int i = 0; i < braziers.Length; i++) DrawBrazier(i, here);
            Vector2 doorCenter = door + new Vector2(1f, 0f);
            _frame.restoredSelf = _doorHeat;
            _frame.glowBoostB = _open ? new Vector4(0.25f * Mathf.Sin(Time.time * 2.3f), 0f, 0f, 0f) : Vector4.zero;
            _frame.Place(doorCenter, 0f, Quaternion.identity, here);
            _leaf.restoredSelf = _doorHeat;
            _leaf.Place(doorCenter, -sinkDepth * _sink, Quaternion.identity, here && _sink < 0.999f);
        }

        void DrawBrazier(int i, bool here)
        {
            int steps = Mathf.Clamp(_shown - i * PerBrazier, 0, PerBrazier);
            var view = _braziers[i];
            if (steps >= PerBrazier)
            {
                view.SetRecipe(brazierLit);
                view.restoredSelf = 1f;
            }
            else if (steps > 0)
            {
                view.SetRecipe(brazierWarm);
                view.restoredSelf = steps / (float)PerBrazier;
            }
            else
            {
                view.SetRecipe(brazierCold);
                // Step 10f: the realm's own restoration, so the roaring firebox's warm spot doesn't light a cold brazier.
                view.restoredSelf = RealmState.Restored;
            }
            view.Place(BrazierTile(i), Lift(i).y, Quaternion.identity, here);
        }

        // ---- pumping ----

        void OnStroked(PumpPiece p, int strokes)
        {
            GameAudio.Play(AudioIds.BellowsPump, MapWorld(NozzleMap), _audio, PuzzleSounds.Puff, volume);
            float tile = TileWorld;
            if (fx != null) fx.Burst(MapWorld(NozzleMap), 8, new Color(0.95f, 0.9f, 0.85f, 0.8f), tile * 0.5f, 0.5f, tile * 0.05f, Vector3.up * tile * 0.3f);
            int target = Mathf.Clamp((strokes - 1) / PerBrazier, 0, braziers.Length - 1);
            StartCoroutine(Pulse(target, strokes));
        }

        /// <summary>Step 10f: pumped with the firebox cold: a weak grey puff, nothing flows.</summary>
        void OnBlockedStroke(PumpPiece p)
        {
            GameAudio.Play(AudioIds.BellowsPump, MapWorld(NozzleMap), _audio, PuzzleSounds.Puff, volume, 0.45f);
            float tile = TileWorld;
            if (fx != null) fx.Burst(MapWorld(NozzleMap), 5, new Color(0.55f, 0.53f, 0.52f, 0.6f), tile * 0.25f, 0.5f, tile * 0.04f, Vector3.up * tile * 0.15f);
            if (!string.IsNullOrEmpty(coldLine) && Time.time - _coldSaidAt > 8f)
            {
                _coldSaidAt = Time.time;
                Dialogue.Say(coldLine);
            }
        }

        void OnCooled(PumpPiece p, int strokes)
        {
            int was = _shown;
            _shown = Mathf.Min(_shown, strokes);
            if (_shown == was || fx == null) return;
            int i = Mathf.Clamp(_shown / PerBrazier, 0, braziers.Length - 1);
            float tile = TileWorld;
            fx.Burst(MapWorld(BrazierTop(i, 0.6f)), 6, new Color(0.5f, 0.48f, 0.46f, 0.7f), tile * 0.2f, 0.9f, tile * 0.05f, Vector3.up * tile * 0.5f);
        }

        Vector2 Lift(int i) => brazierLift != null && i < brazierLift.Length ? brazierLift[i] : Vector2.zero;

        /// <summary>Where brazier <paramref name="i"/> stands (map column, row; back toward the cliff is a lower row).</summary>
        Vector2 BrazierTile(int i) => new Vector2(braziers[i].x, braziers[i].y - Lift(i).x);

        Vector3 BrazierTop(int i, float above)
        {
            var t = BrazierTile(i);
            return new Vector3(t.x, Lift(i).y + above, t.y);
        }

        /// <summary>
        /// The pulse's way along the pipe to brazier <paramref name="i"/> (map units): the nozzle, the riser, then along
        /// the cliff foot; past the door's near side (the bellows is east of the door) it climbs, runs over the arch, and
        /// comes down the far side for a brazier beyond it; a raised brazier is fed from below.
        /// </summary>
        List<Vector3> PipePath(int i)
        {
            var b = braziers[i];
            float z = bellows.y - pipeBack, lift = Lift(i).y;
            float near = door.x + 2 + pipeDoorMargin, far = door.x - pipeDoorMargin;
            var path = new List<Vector3> { NozzleMap, new Vector3(NozzleMap.x, pipeHeight, z) };
            if (b.x < near)
            {
                path.Add(new Vector3(near, pipeHeight, z));
                path.Add(new Vector3(near, pipeOverDoor, z));
                if (lift > 0f)
                {
                    path.Add(new Vector3(b.x, pipeOverDoor, z));
                    path.Add(BrazierTop(i, 0.3f));
                    return path;
                }
                path.Add(new Vector3(far, pipeOverDoor, z));
                path.Add(new Vector3(far, pipeHeight, z));
            }
            path.Add(new Vector3(b.x, pipeHeight, z));
            path.Add(new Vector3(b.x, 0.62f, b.y - 0.17f));
            return path;
        }

        /// <summary>A glow runs from the nozzle up the riser, along the pipe and into brazier <paramref name="i"/>.</summary>
        IEnumerator Pulse(int i, int strokes)
        {
            var path = PipePath(i);
            if (path.Count > 0) GameAudio.At(AudioIds.PipeGlow, MapWorld(path[0]));
            var orb = fx != null ? fx.Get(BattleFx.Shape.Orb) : null;
            var warm = new Color(1f, 0.62f, 0.25f);
            if (orb != null) orb.Set(Color.white, warm, 1f, 1.5f, 1f, 0.3f);
            for (int s = 0; s < path.Count - 1; s++)
            {
                float length = (path[s + 1] - path[s]).magnitude;
                for (float d = 0f; d < length; d += pulseSpeed * Time.deltaTime)
                {
                    if (orb != null && fx != null)
                    {
                        orb.transform.position = MapWorld(Vector3.Lerp(path[s], path[s + 1], d / Mathf.Max(1e-4f, length)));
                        orb.transform.localScale = Vector3.one * (0.3f * TileWorld / Mathf.Max(1e-4f, fx.transform.lossyScale.x));
                        orb.SetAlpha(pump.Here ? 0.9f : 0f);
                    }
                    yield return null;
                }
            }
            if (orb != null) fx.Release(orb);
            Arrive(i, strokes);
        }

        void Arrive(int i, int strokes)
        {
            if (strokes <= _shown) return;
            _shown = Mathf.Max(_shown, strokes);
            float tile = TileWorld;
            Vector3 top = MapWorld(BrazierTop(i, 0.62f));
            bool lit = strokes % PerBrazier == 0;
            if (fx != null) fx.Burst(top, lit ? 30 : 8, new Color(1f, 0.65f, 0.25f), tile * (lit ? 0.9f : 0.35f), lit ? 0.9f : 0.5f, tile * 0.06f, Vector3.up * tile * (lit ? 1f : 0.4f));
            if (!lit)
            {
                GameAudio.At(AudioIds.BrazierWarm, top);
                return;
            }
            GameAudio.Play(AudioIds.BrazierLight, top, _audio, PuzzleSounds.Whoosh, volume);
            int n = strokes / PerBrazier;
            Debug.Log($"[ForgeBellows] Brazier {n} lit.");
            if (n - 1 < brazierLines.Length && !string.IsNullOrEmpty(brazierLines[n - 1])) Dialogue.Say(brazierLines[n - 1]);
            if (!_open && !_opening) _doorHeat = Mathf.Max(_doorHeat, preheat * n / braziers.Length);
            if (n >= braziers.Length && !_open && !_opening) StartCoroutine(OpenDoor());
        }

        // ---- the door ----

        IEnumerator OpenDoor()
        {
            _opening = true;
            Debug.Log("[ForgeBellows] All braziers lit: the forge door opens.");
            if (explorer != null) explorer.Cheer();
            if (!string.IsNullOrEmpty(openLine)) Dialogue.Say(openLine);
            float from = _doorHeat;
            for (float t = 0f; t < glowSeconds; t += Time.deltaTime)
            {
                _doorHeat = Mathf.Lerp(from, 1f, Mathf.SmoothStep(0f, 1f, t / glowSeconds));
                if (fx != null && Random.value < 0.25f) Sparks(6);
                yield return null;
            }
            _doorHeat = 1f;
            GameAudio.Play(AudioIds.ForgeDoor, MapWorld(new Vector3(door.x + 1f, 0.6f, door.y + 0.3f)), _audio, PuzzleSounds.Rumble, volume);
            float next = 0f;
            for (float t = 0f; t < openSeconds; t += Time.deltaTime)
            {
                _sink = Mathf.SmoothStep(0f, 1f, t / openSeconds);
                if (fx != null && t >= next)
                {
                    next = t + 0.12f;
                    float tile = TileWorld;
                    // Steam rolls out along the threshold, sparks shower from the arch.
                    Vector3 sill = MapWorld(new Vector3(door.x + 1f + Random.Range(-0.8f, 0.8f), 0.1f, door.y + 0.3f));
                    fx.Burst(sill, 5, new Color(0.95f, 0.93f, 0.9f, 0.75f), tile * 0.4f, 1.2f, tile * 0.09f, Vector3.up * tile * 0.6f);
                    Sparks(4);
                }
                yield return null;
            }
            _sink = 1f;
            _open = true;
            _opening = false;
            Map.SetPropOpen(door, true);
            Debug.Log("[ForgeBellows] The forge door is open (walk in).");
        }

        void Sparks(int count)
        {
            float tile = TileWorld;
            Vector3 arch = MapWorld(new Vector3(door.x + 1f + Random.Range(-0.7f, 0.7f), Random.Range(1.2f, 2.4f), door.y + 0.35f));
            fx.Burst(arch, count, new Color(1f, 0.7f, 0.3f), tile * 0.6f, 0.6f, tile * 0.035f, Vector3.zero, tile * 2f);
        }

        // ---- into the forge ----

        void OnStep(Vector2Int tile)
        {
            if (!_open || !pump.Here || tile.y != door.y || tile.x < door.x || tile.x > door.x + 2) return;
            if (hall == null || swap == null)
            {
                if (!_warnedHall) Debug.LogWarning("[ForgeBellows] The forge doorway leads nowhere yet (no Forge Hall map).", this);
                _warnedHall = true;
                return;
            }
            var front = new Vector2Int(door.x + 1, door.y + 1);
            Debug.Log($"[ForgeBellows] Into the forge from c{tile.x} r{tile.y}.");
            swap.Enter(hall, hallEntry, new Vector2Int(0, -1), front, new Vector2Int(0, 1));
        }

        // ---- map space ----

        float TileWorld => island.map.tileSize * island.Zoom * island.transform.lossyScale.x;

        /// <summary>The nozzle (column, height, row): shape +X is map west (lower columns).</summary>
        Vector3 NozzleMap => new Vector3(bellows.x - nozzle.x, nozzle.y, bellows.y + nozzle.z);

        Vector3 MapWorld(Vector3 m) =>
            island.transform.TransformPoint(island.TileToLocal(new Vector2(m.x, m.z), island.Content.capHeight + m.y * island.map.tileSize));
    }
}
