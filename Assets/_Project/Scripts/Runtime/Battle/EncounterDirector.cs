using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Diorama
{
    /// <summary>
    /// Wild encounters and the battle flow. Each step onto tall grass may roll the grass table; a tap while facing water
    /// rolls the water table. Then: the explorer stops, the grass rustles (or the water ripples), the island zooms in,
    /// the wild creature hops out in front of the explorer, the lead party creature pops out beside it, and the battle
    /// runs turn by turn from the runes until someone faints or the player runs. Then it zooms out and walking resumes.
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
        public float zoom = 3f;
        public float zoomSeconds = 0.6f;
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
            Layout(water, where, out var wildPos, out var playerPos, out var pivot);
            Debug.Log($"[EncounterDirector] {(water ? "Water" : "Grass")} encounter: wild {wild.Name} lv {wild.level}{(rare ? " (rare)" : "")} " +
                      $"at {wildPos}, {lead.Name} lv {lead.level} at {playerPos}.");

            var rustleTile = Vector2Int.RoundToInt(wildPos);
            if (water)
            {
                window.ZoomTo(pivot, zoom, zoomSeconds);
                yield return stage.Ripple(rustleTile, rippleSeconds);
            }
            else
            {
                StartCoroutine(stage.Rustle(rustleTile, rare, zoomSeconds + 0.2f));
                window.ZoomTo(pivot, zoom, zoomSeconds);
                yield return new WaitForSeconds(zoomSeconds + 0.1f);
            }

            stage.Spawn(wild, BattleSide.Wild, wildPos, playerPos);
            stage.Spawn(lead, BattleSide.Player, playerPos, wildPos);
            yield return stage.WildAppears(rare, water);
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
            if (window.IsZoomed)
            {
                window.ZoomOut(zoomSeconds);
                yield return new WaitForSeconds(zoomSeconds);
            }
            stage.Cleanup();
            _stepsSinceBattle = 0;
            explorer.Locked = false;
            MapCreature.HideAll = false;
            InBattle = false;
        }

        /// <summary>
        /// Where everyone stands, in map tiles. Grass: the wild creature about two tiles in front of the explorer
        /// (another side if that tile is blocked). Water: on the water tile it jumps from. The party creature stands
        /// beside the explorer (a little back when the wild one is close). The zoom centres between all three.
        /// </summary>
        void Layout(bool water, Vector2Int where, out Vector2 wildPos, out Vector2 playerPos, out Vector2 pivot)
        {
            var map = island.Content;
            Vector2Int e = explorer.Tile;
            Vector2Int dir = explorer.Facing;
            if (water)
            {
                dir = where - e;
                var beyond = where + dir;
                wildPos = map.At(beyond.y, beyond.x) == IslandMap.Water ? (Vector2)e + (Vector2)dir * 1.7f : where;
            }
            else
            {
                Vector2Int[] tries = { dir, new Vector2Int(0, -1), new Vector2Int(-dir.y, dir.x), new Vector2Int(dir.y, -dir.x), -dir };
                foreach (var t in tries)
                {
                    var spot = e + t * 2;
                    if (t == Vector2Int.zero || !IslandMap.IsWalkable(map.At(spot.y, spot.x))) continue;
                    dir = t;
                    break;
                }
                wildPos = (Vector2)e + (Vector2)dir * 1.8f;
            }
            Vector2 forward = dir;
            Vector2 side = new Vector2(-forward.y, forward.x);
            // A water creature can be just one tile ahead, so the party creature steps back a little more there.
            float ahead = (wildPos - (Vector2)e).magnitude < 1.5f ? -0.35f : 0.15f;
            playerPos = (Vector2)e + forward * ahead + side * 0.9f;
            var probe = Vector2Int.RoundToInt(playerPos);
            if (!IslandMap.IsWalkable(map.At(probe.y, probe.x))) playerPos = (Vector2)e + forward * ahead - side * 0.9f;
            pivot = ((Vector2)e + wildPos + playerPos) / 3f;
        }
    }
}
