using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Diorama
{
    /// <summary>
    /// Wild encounters and the battle flow. Each step onto tall grass may roll the grass table; a tap while facing water
    /// rolls the water table. Then: the explorer stops, the grass rustles (or the water ripples), the view centres on
    /// the stage and zooms in, a clearing opens (grass parts, props sink, the rest dims) and two pads rise: the party
    /// creature's near the player, back to them, the wild one's 2-3 tiles farther, facing them. The battle runs turn by
    /// turn from the runes until someone faints or the player runs. Then everything returns and walking resumes.
    /// Editor: B starts a grass encounter on the spot.
    /// </summary>
    public class EncounterDirector : MonoBehaviour
    {
        public GestureTestPiece explorer;
        public WorldWindow window;
        public IslandBuilder island;
        public Party party;
        public BattleStage stage;
        public BattleRunes runes;
        public TypeChart typeChart;

        [Header("Encounter tables (this region)")]
        public EncounterTable grassTable;
        public EncounterTable waterTable;

        [Header("Tuning")]
        [Tooltip("Chance per step onto tall grass.")]
        [Range(0f, 1f)] public float grassChance = 0.125f;
        [Tooltip("Steps after a battle before tall grass can trigger again.")]
        [Min(0)] public int graceSteps = 3;
        [Tooltip("Zoom range; the zoom picks the largest value that fits both pads and their labels inside the island.")]
        public Vector2 zoomRange = new Vector2(2.2f, 3f);
        public float zoomSeconds = 0.6f;
        [Tooltip("Tiles between the two pads (2-3).")]
        [Range(2, 3)] public int padDistance = 3;
        [Tooltip("How much content outside the clearing darkens during a battle.")]
        [Range(0f, 0.6f)] public float focusDim = 0.25f;
        [Tooltip("Ripples before a water creature jumps out (s).")]
        public float rippleSeconds = 1f;
        [Tooltip("0 = a new random seed every session.")]
        public int seed;

        public bool InBattle { get; private set; }

        SeededRandom _rnd;
        int _stepsSinceBattle = int.MaxValue / 2;
        int _choice = -1;

        void Start()
        {
            if (explorer == null || window == null || island == null || party == null || stage == null || runes == null || typeChart == null)
            {
                Debug.LogError("[EncounterDirector] Missing references (run Tools > Diorama > Step 5 > 2. Add Battle to MR_Main).", this);
                enabled = false;
                return;
            }
            if (grassTable == null) Debug.LogError("[EncounterDirector] No grass encounter table; tall grass won't trigger battles.", this);
            if (waterTable == null) Debug.LogError("[EncounterDirector] No water encounter table; fishing won't work.", this);
            MapCreature.HideAll = false;
            int s = seed != 0 ? seed : System.Environment.TickCount;
            _rnd = new SeededRandom(s);
            Debug.Log($"[EncounterDirector] Ready. Grass chance {grassChance:P0} per step, seed {s}.");
            explorer.OnTallGrassStep += OnTallGrassStep;
            explorer.OnFishTap += OnFishTap;
            runes.Chosen += OnChosen;
        }

        void OnDestroy()
        {
            if (explorer != null)
            {
                explorer.OnTallGrassStep -= OnTallGrassStep;
                explorer.OnFishTap -= OnFishTap;
            }
            if (runes != null) runes.Chosen -= OnChosen;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.bKey.wasPressedThisFrame) StartEncounter(Habitat.Grass);
        }

        /// <summary>Starts an encounter on the spot (testing: Editor key B, the Claude bridge, the context menu).</summary>
        public void StartEncounter(Habitat habitat)
        {
            var table = habitat == Habitat.Water ? waterTable : grassTable;
            if (InBattle || table == null || window.IsZoomed) return;
            StartCoroutine(Encounter(table, habitat == Habitat.Water ? explorer.Tile + explorer.Facing : explorer.Tile));
        }

        [ContextMenu("Test Grass Encounter")]
        void TestGrass() => StartEncounter(Habitat.Grass);

        [ContextMenu("Test Water Encounter")]
        void TestWater() => StartEncounter(Habitat.Water);

        void OnTallGrassStep(Vector2Int tile)
        {
            if (InBattle || grassTable == null) return;
            if (++_stepsSinceBattle <= graceSteps) return;
            if (_rnd.Next() >= grassChance) return;
            StartCoroutine(Encounter(grassTable, tile));
        }

        void OnFishTap(Vector2Int waterTile)
        {
            if (InBattle || waterTable == null) return;
            StartCoroutine(Encounter(waterTable, waterTile));
        }

        void OnChosen(int index) => _choice = index;

        IEnumerator Encounter(EncounterTable table, Vector2Int where)
        {
            InBattle = true;
            explorer.Locked = true;
            MapCreature.HideAll = true;
            if (!table.Roll(_rnd, out var wild, out bool rare))
            {
                Debug.LogError($"[EncounterDirector] {table.name} has no species to roll.", table);
                yield return Finish();
                yield break;
            }
            var lead = party.Lead;
            if (lead == null)
            {
                // Temporary until step 6 adds the losing rules (shrine, heal): never block the test.
                party.HealAll();
                lead = party.Lead;
                Debug.Log("[EncounterDirector] Whole party had fainted; healed it (temporary rule until step 6).");
            }
            if (lead == null || wild.species.Prefab == null || lead.species.Prefab == null)
            {
                Debug.LogError("[EncounterDirector] Can't battle: empty party or an unbaked species.");
                yield return Finish();
                yield break;
            }

            bool water = table.habitat == Habitat.Water;
            var layout = Layout(water);
            Debug.Log($"[EncounterDirector] {(water ? "Water" : "Grass")} encounter: wild {wild.Name} lv {wild.level}{(rare ? " (rare)" : "")} " +
                      $"on pad c{layout.wildPad.x} r{layout.wildPad.y}, {lead.Name} lv {lead.level} on pad c{layout.myPad.x} r{layout.myPad.y}, " +
                      $"zoom x{layout.zoom:F2}{(layout.fallback ? " (fallback pads: no free pair)" : "")}.");

            explorer.StandOffset = layout.explorerSpot - explorer.Tile;
            window.SetFocus(layout.pivot);
            island.SetClearing(layout.pivot, layout.wildPad - layout.myPad, padDistance * 0.5f + 1.3f, 1.6f,
                layout.pivot + layout.towardPlayer * 9f, 1.1f);
            stage.SpawnPads(layout.myPad, layout.wildPad, table.habitat);
            if (water)
            {
                window.ZoomTo(layout.pivot, layout.zoom, zoomSeconds);
                StartCoroutine(FadeStage(1f));
                StartCoroutine(stage.RisePads(true));
                yield return stage.Ripple(where, rippleSeconds);
            }
            else
            {
                StartCoroutine(stage.Rustle(layout.wildPad, rare, zoomSeconds + 0.2f));
                window.ZoomTo(layout.pivot, layout.zoom, zoomSeconds);
                StartCoroutine(FadeStage(1f));
                StartCoroutine(stage.RisePads(true));
                yield return new WaitForSeconds(zoomSeconds + 0.1f);
            }

            stage.Spawn(wild, BattleSide.Wild, layout.wildPad, layout.myPad);
            stage.Spawn(lead, BattleSide.Player, layout.myPad, layout.wildPad);
            runes.Avoid(stage.Player, stage.Wild);
            yield return stage.WildAppears(rare, water ? (Vector2?)where : null);
            runes.Show(lead.species.moves);
            runes.SetCaption(rare ? $"A rare wild {wild.Name} appeared!" : $"A wild {wild.Name} appeared!");
            yield return new WaitForSeconds(0.3f);
            runes.SetCaption($"Go, {lead.Name}!");
            yield return stage.PodBurst();
            stage.ShowBars(lead, wild);

            var battle = new BattleSystem(lead, wild, typeChart, (int)(_rnd.Next() * int.MaxValue), rare);
            while (!battle.IsOver)
            {
                runes.SetCaption($"What will {lead.Name} do?");
                _choice = -1;
                runes.SetInteractable(true);
                while (_choice < 0) yield return null;
                runes.SetInteractable(false);
                var events = _choice == BattleRunes.RunIndex ? battle.TryFlee() : battle.PlayTurn(_choice);
                foreach (var e in events) Debug.Log($"[Battle] {e}");
                yield return stage.Play(events, battle, runes.SetCaption);
            }

            Debug.Log($"[EncounterDirector] Battle over: {battle.Result}. {lead.Name} HP {lead.currentHp}/{lead.MaxHp}, XP {lead.xp}.");
            yield return stage.Outro(battle.Result);
            if (battle.Result == BattleResult.Lost)
            {
                party.HealAll();
                Debug.Log("[EncounterDirector] Lost: party healed (temporary rule until step 6).");
            }
            yield return Finish();
        }

        IEnumerator Finish()
        {
            runes.Hide();
            explorer.StandOffset = Vector2.zero;
            window.SetFocus(null);
            StartCoroutine(stage.RisePads(false));
            if (window.IsZoomed || island.clearingStrength > 0f)
            {
                window.ZoomOut(zoomSeconds);
                StartCoroutine(FadeStage(0f));
                yield return new WaitForSeconds(zoomSeconds);
            }
            island.clearingStrength = island.clearingDim = 0f;
            stage.Cleanup();
            _stepsSinceBattle = 0;
            explorer.Locked = false;
            MapCreature.HideAll = false;
            InBattle = false;
        }

        /// <summary>Clearing, pads and dim fade in or out together with the zoom.</summary>
        IEnumerator FadeStage(float to)
        {
            float from = island.clearingStrength;
            for (float t = 0f; t < zoomSeconds; t += Time.deltaTime)
            {
                float k = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / zoomSeconds));
                island.clearingStrength = k;
                island.clearingDim = k * focusDim;
                yield return null;
            }
            island.clearingStrength = to;
            island.clearingDim = to * focusDim;
        }

        struct StageLayout
        {
            public Vector2Int myPad, wildPad;
            public Vector2 towardPlayer, pivot, explorerSpot;
            public float zoom;
            public bool fallback;
        }

        static readonly Vector2Int[] Directions =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1),
        };

        /// <summary>
        /// Picks the two pad tiles (Pokemon framing): the party creature's pad on the side of the encounter nearest the
        /// player, the wild pad 2-3 tiles farther away on the same line. Pad tiles avoid water, trees and rocks (a pond
        /// battle may put the wild pad on the water); with no free pair the best pair is used anyway.
        /// The zoom centres between the pads and is as big as fits inside the island footprint.
        /// </summary>
        StageLayout Layout(bool water)
        {
            var map = island.Content;
            Vector2Int e = explorer.Tile;
            var cam = Camera.main;
            Vector2 toward = Vector2.up; // rows run toward the player's side of the island by default
            if (cam != null)
            {
                Vector2 head = island.LocalToTile(island.transform.InverseTransformPoint(cam.transform.position));
                if ((head - e).sqrMagnitude > 0.01f) toward = (head - e).normalized;
            }

            bool InMap(Vector2Int t) => t.x >= 1 && t.y >= 1 && t.x < map.Columns - 1 && t.y < map.Rows - 1;
            bool Free(Vector2Int t) => InMap(t) && IslandMap.IsWalkable(map.At(t.y, t.x));
            bool WildFree(Vector2Int t) => Free(t) || water && InMap(t) && map.At(t.y, t.x) == IslandMap.Water;

            float best = float.NegativeInfinity, bestAny = float.NegativeInfinity;
            Vector2Int my = e, wild = e - Vector2Int.RoundToInt(toward) * padDistance, myAny = my, wildAny = wild;
            var nearer = new Vector2Int(Mathf.RoundToInt(toward.x), Mathf.RoundToInt(toward.y));
            Vector2Int[] myCandidates = { e, e + nearer, e + new Vector2Int(nearer.y, -nearer.x), e + new Vector2Int(-nearer.y, nearer.x) };
            foreach (var m in myCandidates)
                foreach (var dir in Directions)
                {
                    bool diagonal = dir.x != 0 && dir.y != 0;
                    for (int d = padDistance; d >= 2; d--)
                    {
                        int steps = diagonal ? Mathf.Max(1, Mathf.RoundToInt(d / 1.41f)) : d;
                        var w = m + dir * steps;
                        if (!InMap(m) || !InMap(w)) continue;
                        Vector2 away = ((Vector2)dir).normalized;
                        float score = Vector2.Dot(away, -toward) * 4f - (padDistance - d) * 0.6f - (m - e).magnitude
                                      + (water && map.At(w.y, w.x) == IslandMap.Water ? 1f : 0f);
                        if (score > bestAny) { bestAny = score; myAny = m; wildAny = w; }
                        if (Free(m) && WildFree(w) && score > best) { best = score; my = m; wild = w; }
                    }
                }
            bool fallback = float.IsNegativeInfinity(best);
            if (fallback) { my = myAny; wild = wildAny; }

            Vector2 line = ((Vector2)(wild - my)).normalized;
            Vector2 side = new Vector2(-line.y, line.x);
            Vector2 pivot = ((Vector2)my + wild) * 0.5f;
            var layout = new StageLayout
            {
                myPad = my,
                wildPad = wild,
                towardPlayer = toward,
                pivot = pivot,
                // Just behind the party creature's pad, to one side.
                explorerSpot = my - line * 0.35f + side * 0.85f,
                fallback = fallback,
            };
            layout.zoom = FitZoom(pivot, new[] { (Vector2)my, wild });
            return layout;
        }

        /// <summary>
        /// Largest zoom in <see cref="zoomRange"/> that keeps these map points (plus a label margin) inside the footprint,
        /// once the view has centred on the pivot (near the map border it can't centre fully).
        /// </summary>
        float FitZoom(Vector2 pivot, Vector2[] points)
        {
            const float margin = 1.2f; // tiles at final size: pad radius plus room for the HP bar
            float halfX = island.map.Columns * 0.5f - margin, halfZ = island.map.Rows * 0.5f - margin;
            Vector2 center = window.FocusCenter(pivot);
            // Island-local tiles: columns run toward -X, rows toward +Z.
            Vector2 pl = new Vector2(-(pivot.x - center.x), pivot.y - center.y);
            float z = zoomRange.y;
            foreach (var p in points)
            {
                Vector2 d = new Vector2(-(p.x - pivot.x), p.y - pivot.y);
                z = Mathf.Min(z, Mathf.Min(Limit(pl.x, d.x, halfX), Limit(pl.y, d.y, halfZ)));
            }
            return Mathf.Clamp(z, zoomRange.x, zoomRange.y);
        }

        static float Limit(float pivot, float delta, float half)
        {
            if (Mathf.Abs(delta) < 1e-4f) return float.MaxValue;
            return delta > 0f ? (half - pivot) / delta : (half + pivot) / -delta;
        }
    }
}
