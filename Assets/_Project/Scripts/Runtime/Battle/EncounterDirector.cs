using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Diorama
{
    /// <summary>
    /// Wild encounters and the battle flow. Each step onto tall grass may roll the grass table; a tap while facing water
    /// rolls the water table. Then: the explorer stops, the grass rustles (or the water ripples), the view centres on
    /// the stage and zooms in, a clearing opens (grass parts, props sink, the rest dims) and two pads rise: the party
    /// creature's near the player, back to them, the wild one's 2-3 tiles farther, facing them. The player sends a
    /// creature out by throwing (or dropping) a rim pod onto the island, look + pinch on a pod, or waiting
    /// <see cref="autoSendSeconds"/>. The battle runs turn by turn from the runes and the pods (throw another party pod to
    /// switch, flick the cradle pod at the wild creature to catch it) until someone is caught, faints for good or the
    /// player runs. Only Run shows until the party creature lands; then its moves pop in. The empty cradle pod is there
    /// from the moment the wild creature appears and can always be thrown: above red HP it wobbles once and shatters
    /// (the turn is used); in the red it glows and has a real chance; a fainted wild creature is dazed for
    /// <see cref="dazedSeconds"/> (the pod pulses) and a pod catches it for sure, otherwise it fades out and the battle
    /// is won. After a win (fainted or caught) every creature that fought gets XP (<see cref="Progression"/>): the one
    /// on the pad shows it (XP bar, level-ups) and evolves there if due. Losing the whole party sends the explorer back
    /// to the last Hearth Shrine used (spawn without one), healed. Then walking resumes.
    /// During the tutorial (<see cref="Onboarding.EncountersEnabled"/> off) tall grass only rustles and fishing is off,
    /// except the tutorial patch while the grass lesson runs: it always meets the scripted first species.
    /// Editor: B starts a grass encounter on the spot; in battle C throws the cradle pod, X switches to the next pod.
    /// </summary>
    public class EncounterDirector : MonoBehaviour
    {
        public GestureTestPiece explorer;
        public WorldWindow window;
        public IslandBuilder island;
        public Party party;
        public BattleStage stage;
        public BattleRunes runes;
        public PodRack pods;
        public TypeChart typeChart;
        public ProgressionConfig progression;
        [Tooltip("Losing sends the explorer to the last shrine used (the start tile without one).")]
        public HearthShrines shrines;

        [Header("First encounter (new game)")]
        [Tooltip("The first tall grass step of a new game always meets this one (easy to beat and catch).")]
        public IslingSpecies firstSpecies;
        [Min(1)] public int firstLevel = 3;

        /// <summary>The scripted first encounter has happened (saved).</summary>
        public bool FirstEncounterDone { get; set; }

        /// <summary>The current outdoor map's tall grass table (each map has its own: <see cref="WorldMap.grassTable"/>; zones can override it, <see cref="GrassTableAt"/>).</summary>
        public EncounterTable GrassTable => island != null && island.world != null ? island.world.grassTable : null;
        /// <summary>The current outdoor map's water table.</summary>
        /// <summary>The tall grass table for a tile of the current map: its zone's own table, else the map's.</summary>
        public EncounterTable GrassTableAt(Vector2Int tile) => island != null && island.world != null ? island.world.GrassTableAt(tile) : null;

        public EncounterTable WaterTable => island != null && island.world != null ? island.world.waterTable : null;

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

        [Header("Pods")]
        [Tooltip("The lead pod is sent out by itself after this long (s).")]
        public float autoSendSeconds = 6f;
        [Tooltip("The same during the tutorial (the send-out lesson's ghost hand shows how first).")]
        public float tutorialAutoSendSeconds = 20f;
        [Tooltip("A catch throw homes in when it heads within this angle of the wild creature (degrees, flat).")]
        public float catchAimAngle = 25f;
        [Tooltip("A send-out throw lands on the pad when it heads within this angle of it (degrees, flat); a drop above the island always does.")]
        public float sendAimAngle = 45f;
        [Tooltip("Slowest release that counts as a throw (m/s).")]
        public float minThrowSpeed = 0.35f;
        [Tooltip("How long a fainted wild creature stays dazed and catchable (s).")]
        public float dazedSeconds = 8f;

        public bool InBattle { get; private set; }

        /// <summary>In battle with the player's Isling out on its pad (its moves can be picked).</summary>
        public bool PlayerOut => InBattle && _activeSlot >= 0;

        /// <summary>Waiting for the player to send an Isling out (toss a pod onto the island).</summary>
        public bool AwaitingSendOut { get; private set; }

        /// <summary>An encounter starts (before the zoom; hint lines close).</summary>
        public event System.Action BattleStarted;

        /// <summary>A battle finished and walking resumed (also after running before a send-out).</summary>
        public event System.Action<BattleResult> BattleEnded;

        /// <summary>A wild creature was caught (it is already in the party or storage).</summary>
        public event System.Action<IslingInstance> Caught;

        [Tooltip("Species not caught yet show up this many times as often (duplicates still spawn).")]
        public float uncaughtWeight = 2f;

        /// <summary>Has this species been caught? Set by the save (GameSave). Null = all count as caught (no boost, no "New!").</summary>
        public System.Func<IslingSpecies, bool> IsCaught;

        /// <summary>Species ids met in battle so far (the first meeting of an uncaught one gets a "New!" sparkle). Saved.</summary>
        public readonly HashSet<string> Seen = new HashSet<string>();

        /// <summary>The battle being played, or null (testing).</summary>
        public BattleSystem CurrentBattle { get; private set; }

        /// <summary>A Keeper battle (step 9c): who, its pad, the party's pad, its rules.</summary>
        public sealed class KeeperFight
        {
            public IslingInstance keeper;
            public Vector2Int keeperPad, myPad;
            public KeeperRules rules = new KeeperRules();
            public string caption;
            [Tooltip("A Keeper is ~2.5x an Isling: the battle zoom stays below this so it fits.")]
            public float maxZoom = 1.8f;
        }

        /// <summary>The Keeper battle being played, or null.</summary>
        public KeeperFight Keeper { get; private set; }

        /// <summary>
        /// Starts a Keeper battle on fixed pads (inside an interior too): no pod to catch it, Keeper rules
        /// (<see cref="KeeperRules"/>), otherwise the usual battle (send out, moves, switching, XP). False if a battle is on.
        /// </summary>
        public bool StartKeeper(KeeperFight fight)
        {
            if (InBattle || fight == null || fight.keeper == null || party.members.Count == 0 || window.IsZoomed || SceneSwap.IsBusy) return false;
            // Step 10d: each Keeper's shield breaks to its own element.
            if (fight.rules != null && fight.keeper.species != null) fight.rules.breaksWith = fight.keeper.species.keeperBreaksWith;
            StartCoroutine(Encounter(null, fight.myPad, fight.keeper, fight));
            return true;
        }

        enum ActKind { None, Move, Run, PodThrow, PodPick }

        struct Act
        {
            public ActKind kind;
            public int index;
            public Vector3 pos, vel; // pod throws: island-local release point and velocity
        }

        SeededRandom _rnd;
        int _stepsSinceBattle = int.MaxValue / 2;
        Act _act;
        bool _waiting;
        int _activeSlot = -1;
        StageLayout _layout;

        void Start()
        {
            if (explorer == null || window == null || island == null || party == null || stage == null || runes == null || pods == null || typeChart == null)
            {
                Debug.LogError("[EncounterDirector] Missing references (run Tools > Diorama > Step 5 > 2. Add Battle to MR_Main, then Tools > Pocket Isle > Set Up MR_Main (6 capture)).", this);
                enabled = false;
                return;
            }
            if (progression == null) Debug.LogError("[EncounterDirector] No ProgressionConfig; wins pay no XP (Tools > Pocket Isle > Set Up MR_Main (7 progression)).", this);
            ShardScaling.Config = progression;
            if (firstSpecies == null) Debug.LogError("[EncounterDirector] No first-encounter species: a new game's first grass rolls the table (Set Up MR_Main (8 hub)).", this);
            MapCreature.HideAll = false;
            int s = seed != 0 ? seed : System.Environment.TickCount;
            _rnd = new SeededRandom(s);
            Debug.Log($"[EncounterDirector] Ready. Grass chance {grassChance:P0} per step, seed {s}.");
            explorer.OnTallGrassStep += OnTallGrassStep;
            explorer.OnFishTap += OnFishTap;
            runes.Chosen += OnChosen;
            pods.Thrown += OnPodThrown;
            pods.Picked += OnPodPicked;
        }

        void OnDestroy()
        {
            if (explorer != null)
            {
                explorer.OnTallGrassStep -= OnTallGrassStep;
                explorer.OnFishTap -= OnFishTap;
            }
            if (runes != null) runes.Chosen -= OnChosen;
            if (pods != null)
            {
                pods.Thrown -= OnPodThrown;
                pods.Picked -= OnPodPicked;
            }
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.bKey.wasPressedThisFrame) StartEncounter(Habitat.Grass);
            if (!Application.isEditor) return;
            if (kb.cKey.wasPressedThisFrame) DebugPick(PodRack.CradleSlot);
            if (kb.xKey.wasPressedThisFrame) DebugPick(party.NextFighter(_activeSlot));
        }

        /// <summary>Same as look + pinch on a pod (testing: Editor keys, the Claude bridge).</summary>
        public void DebugPick(int slot)
        {
            if (slot >= 0) OnPodPicked(slot);
        }

        /// <summary>Starts an encounter on the spot (testing: Editor key B, the Claude bridge, the context menu).</summary>
        public void StartEncounter(Habitat habitat)
        {
            var table = habitat == Habitat.Water ? WaterTable : GrassTableAt(explorer.Tile);
            if (InBattle || table == null || party.members.Count == 0 || window.IsZoomed || SceneSwap.IsInside || SceneSwap.IsBusy) return;
            StartCoroutine(Encounter(table, habitat == Habitat.Water ? explorer.Tile + explorer.Facing : explorer.Tile));
        }

        /// <summary>Testing (the Claude bridge): a grass battle on the spot with this wild Isling. False when one can't start.</summary>
        public bool DebugEncounter(IslingInstance wild)
        {
            var table = GrassTableAt(explorer.Tile);
            if (table == null) table = GrassTable;
            if (InBattle || wild == null || table == null || party.members.Count == 0 || window.IsZoomed || SceneSwap.IsInside || SceneSwap.IsBusy) return false;
            StartCoroutine(Encounter(table, explorer.Tile, wild));
            return true;
        }

        [ContextMenu("Test Grass Encounter")]
        void TestGrass() => StartEncounter(Habitat.Grass);

        [ContextMenu("Test Water Encounter")]
        void TestWater() => StartEncounter(Habitat.Water);

        void OnTallGrassStep(Vector2Int tile)
        {
            var table = GrassTableAt(tile);
            if (InBattle || SceneSwap.IsInside || party.members.Count == 0) return;
            if (table == null)
            {
                Debug.LogError($"[EncounterDirector] {island.world.name} has no grass table: tall grass won't trigger battles.", island.world);
                return;
            }
            if (!Onboarding.EncountersEnabled)
            {
                if (!Onboarding.IsTutorialGrass(tile) || firstSpecies == null) return; // only rustles (the step flash)
                Debug.Log($"[EncounterDirector] Tutorial grass: {firstSpecies.name} Lv {firstLevel} (scripted).");
                Onboarding.Mark("grassBattle");
                StartCoroutine(Encounter(table, tile, new IslingInstance(firstSpecies, firstLevel)));
                return;
            }
            if (!FirstEncounterDone && firstSpecies != null)
            {
                Debug.Log($"[EncounterDirector] First encounter of the game (scripted): {firstSpecies.name} Lv {firstLevel}.");
                StartCoroutine(Encounter(table, tile, new IslingInstance(firstSpecies, firstLevel)));
                return;
            }
            if (++_stepsSinceBattle <= graceSteps) return;
            // Step 9g: the Quick Journey halves it in Cinderwood (QuickJourneyConfig.encounterRate).
            if (DevCheats.WildOff || _rnd.Next() >= QuickJourney.EncounterChance(grassChance)) return;
            StartCoroutine(Encounter(table, tile));
        }

        void OnFishTap(Vector2Int waterTile)
        {
            var table = WaterTable;
            if (InBattle || party.members.Count == 0) return;
            if (!Onboarding.EncountersEnabled)
            {
                Debug.Log("[EncounterDirector] Fishing is off until the tutorial's grass lesson is done.");
                return;
            }
            if (table == null)
            {
                Debug.LogError($"[EncounterDirector] {island.world.name} has no water table: fishing won't work.", island.world);
                return;
            }
            StartCoroutine(Encounter(table, waterTile));
        }

        void OnChosen(int index)
        {
            if (!_waiting || _act.kind != ActKind.None) return;
            _act = new Act
            {
                kind = index == BattleRunes.RunIndex ? ActKind.Run : ActKind.Move,
                index = index,
            };
        }

        void OnPodThrown(int slot, Vector3 pos, Vector3 vel)
        {
            if (_waiting && _act.kind == ActKind.None) _act = new Act { kind = ActKind.PodThrow, index = slot, pos = pos, vel = vel };
            else StartCoroutine(pods.FlyHome(slot));
        }

        void OnPodPicked(int slot)
        {
            if (!_waiting || _act.kind != ActKind.None) return;
            bool usable = slot == PodRack.CradleSlot ? pods.CradleGrab : pods.PartyGrab && slot != _activeSlot && party.CanFight(slot);
            if (usable) _act = new Act { kind = ActKind.PodPick, index = slot };
        }

        IEnumerator Encounter(EncounterTable table, Vector2Int where, IslingInstance scripted = null, KeeperFight keeperFight = null)
        {
            InBattle = true;
            Keeper = keeperFight;
            explorer.Locked = true;
            MapCreature.HideAll = true;
            BattleStarted?.Invoke();
            FirstEncounterDone = true;
            var wild = scripted;
            bool rare = false;
            if (wild == null && (table == null || !table.Roll(_rnd, out wild, out rare, IsCaught, uncaughtWeight)))
            {
                Debug.LogError($"[EncounterDirector] {(table != null ? table.name : "No table")}: no species to roll.", table);
                yield return Finish(false);
                yield break;
            }
            if (scripted == null)
            {
                // Step 10a: realms in any order: wild levels grow with the shards held (not in the hub).
                string realm = island != null && island.world != null ? island.world.realm : null;
                int scaled = ShardScaling.WildLevel(wild.level, realm);
                if (scaled != wild.level)
                {
                    Debug.Log($"[EncounterDirector] {wild.species.name} Lv {wild.level} -> {scaled} ({ShardScaling.Held()} shard(s) held).");
                    wild = new IslingInstance(wild.species, scaled);
                }
            }
            var lead = party.Lead;
            if (lead == null)
            {
                // Shouldn't happen (losing heals the party); never block the game.
                party.HealAll();
                lead = party.Lead;
                Debug.LogWarning("[EncounterDirector] Whole party had fainted at an encounter; healed it.");
            }
            if (lead == null || wild.species.Prefab == null || lead.species.Prefab == null)
            {
                Debug.LogError("[EncounterDirector] Can't battle: empty party or an unbaked species.");
                yield return Finish(false);
                yield break;
            }

            bool water = table != null && table.habitat == Habitat.Water;
            var layout = _layout = keeperFight != null ? KeeperLayout(keeperFight) : Layout(water);
            string zone = island.world != null ? island.world.ZoneAt(where) : null;
            Debug.Log($"[EncounterDirector] {(keeperFight != null ? "Keeper" : water ? "Water" : "Grass")} encounter ({(table != null ? table.name : "no table")}{(zone != null ? $", zone {zone}" : "")}): wild {wild.Name} lv {wild.level}{(rare ? " (rare)" : "")} " +
                      $"on pad c{layout.wildPad.x} r{layout.wildPad.y}, lead {lead.Name} lv {lead.level} for pad c{layout.myPad.x} r{layout.myPad.y}, " +
                      $"zoom x{layout.zoom:F2}{(layout.fallback ? " (fallback pads: no free pair)" : "")}.");

            explorer.StandOffset = layout.explorerSpot - explorer.Tile;
            // Step 9h: the explorer turns to face the wild Isling (it kept its walking direction).
            var toWild = (Vector2)layout.wildPad - (Vector2)layout.explorerSpot;
            explorer.Face(Mathf.Abs(toWild.x) > Mathf.Abs(toWild.y)
                ? new Vector2Int(toWild.x > 0f ? 1 : -1, 0)
                : new Vector2Int(0, toWild.y > 0f ? 1 : -1));
            window.SetFocus(layout.pivot);
            island.SetClearing(layout.pivot, layout.wildPad - layout.myPad, padDistance * 0.5f + 1.3f, 1.6f,
                layout.pivot + layout.towardPlayer * 9f, 1.1f);
            stage.SpawnPads(layout.myPad, layout.wildPad, table != null ? table.habitat : Habitat.Grass);
            if (keeperFight != null)
            {
                window.ZoomTo(layout.pivot, layout.zoom, zoomSeconds);
                StartCoroutine(FadeStage(1f));
                StartCoroutine(stage.RisePads(true));
                yield return new WaitForSeconds(zoomSeconds + 0.1f);
            }
            else if (water)
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

            var gaze = FindFirstObjectByType<HeadGaze>();
            Debug.Log($"[EncounterDirector] Gaze offset now {(gaze != null ? gaze.CurrentOffset : HeadGaze.DefaultOffset):0.0} deg " +
                      $"(adaptive {(gaze != null && gaze.adaptive ? $"{gaze.share:0.00} share, max {gaze.maxOffset:0}" : "off")}).");
            stage.Spawn(wild, BattleSide.Wild, layout.wildPad, layout.myPad);
            runes.Avoid(null, stage.Wild);
            pods.InBattle = true;
            pods.CradleVisible = keeperFight == null; // a Keeper can't be caught
            pods.CradleGlow = pods.CradlePulse = false;
            // First meeting with a species not caught yet: a little "New!" sparkle as it appears.
            bool isNew = keeperFight == null && IsCaught != null && wild.species != null && !IsCaught(wild.species) && Seen.Add(SpeciesRegistry.IdOf(wild.species));
            if (isNew)
            {
                Debug.Log($"[EncounterDirector] First meeting with {wild.species.displayName}: New!");
                StartCoroutine(NewSparkle());
            }
            yield return stage.WildAppears(rare, water ? (Vector2?)where : null);
            // Oct 27 (Mugesh): its name, level and HP show as soon as it appears, not only once a pod is thrown.
            stage.ShowWildBar(wild);
            runes.ShowRunOnly();
            runes.SetCaption(keeperFight != null ? keeperFight.caption ?? $"{wild.Name} blocks the way!" : rare ? $"A rare wild {wild.Name} appeared!" : $"A wild {wild.Name} appeared!");
            if (rare) Dialogue.Say("rare.first");
            // Step 9f: the Water visitor at the hot springs gets its own hint (Water beats the fire Keeper).
            if (keeperFight == null && wild.species != null && wild.species.name == "Steambara") Dialogue.Say("steambara.first");
            // Step 10c: first meetings in Galewick.
            if (keeperFight == null && wild.species != null && wild.species.name == "Kitelet") Dialogue.Say("gw.meet.kitelet");
            if (keeperFight == null && wild.species != null && wild.species.name == "Zeppelope") Dialogue.Say("gw.meet.zeppelope");
            if (keeperFight == null && wild.species != null && wild.species.name == "Sailfold") Dialogue.Say("lm.meet.sailfold");
            if (keeperFight == null && wild.species != null && wild.species.name == "Bottlewhale") Dialogue.Say("lm.meet.bottlewhale");
            yield return new WaitForSeconds(0.6f);

            _activeSlot = -1;
            Dialogue.Say("pod.first");
            bool fledEarly = false;
            yield return SendOut("Grab a pod and toss it in!", () => fledEarly = true);
            if (fledEarly)
            {
                // Run before anything was sent out: the wild one hasn't engaged yet, so it always works.
                Debug.Log("[EncounterDirector] Ran before sending a creature out.");
                runes.SetCaption("Got away safely!");
                yield return new WaitForSeconds(0.5f);
                yield return stage.Outro(BattleResult.Fled);
                yield return Finish(false);
                BattleEnded?.Invoke(BattleResult.Fled);
                yield break;
            }

            var battle = CurrentBattle = new BattleSystem(party.At(_activeSlot), wild, typeChart, (int)(_rnd.Next() * int.MaxValue), rare, HasReserve,
                keeperFight != null ? keeperFight.rules : null);
            Dialogue.Say("battle.first");
            string prompt = null;
            UpdateCradle(battle);
            while (!battle.IsOver)
            {
                if (battle.WildDazed)
                {
                    yield return Dazed(battle, wild);
                    continue;
                }
                if (battle.PlayerMustSwitch)
                {
                    runes.HideMoves();
                    yield return stage.RecallPlayer(pods.HomeWorld(_activeSlot));
                    pods.ActiveSlot = -1;
                    yield return SendOut($"{battle.player.isling.Name} needs a rest. Toss in another pod!");
                    battle.Switch(party.At(_activeSlot));
                    continue;
                }

                runes.SetCaption(prompt ?? $"What will {battle.player.isling.Name} do?");
                prompt = null;
                _act = default;
                _waiting = true;
                pods.PartyGrab = true;
                pods.CradleGrab = true;
                runes.SetInteractable(true);
                while (_act.kind == ActKind.None) yield return null;
                var act = _act;
                _waiting = false;
                runes.SetInteractable(false);
                pods.PartyGrab = pods.CradleGrab = false;

                if (act.kind == ActKind.Move) Onboarding.Mark("moveUsed");
                else if (act.index == PodRack.CradleSlot && act.kind != ActKind.Run) Onboarding.Mark("podThrown");
                IReadOnlyList<BattleEvent> events;
                if (act.kind == ActKind.Move) events = battle.PlayTurn(act.index);
                else if (act.kind == ActKind.Run) events = battle.TryFlee();
                else if (act.index == PodRack.CradleSlot)
                {
                    bool hit = false;
                    yield return CatchThrow(act, battle, wild, h => hit = h);
                    if (!hit)
                    {
                        prompt = $"Missed! Flick it right at {wild.Name}.";
                        continue;
                    }
                    events = battle.LastCatch;
                }
                else
                {
                    Vector3 pad = PodOnPad();
                    if (act.kind == ActKind.PodThrow && !OntoIsland(act, pad))
                    {
                        Debug.Log($"[EncounterDirector] Switch throw went wide ({ThrowInfo(act, pad)}): pod back to its socket.");
                        yield return pods.FlyHome(act.index);
                        continue;
                    }
                    var leaving = battle.player.isling;
                    events = battle.Switch(party.At(act.index));
                    Debug.Log($"[EncounterDirector] Switch: {leaving.Name} out, {party.At(act.index).Name} in.");
                    runes.SetCaption($"Come back, {leaving.Name}!");
                    runes.HideMoves();
                    bool landed = false;
                    var thrownVel = act.kind == ActKind.PodThrow ? act.vel : Vector3.zero;
                    StartCoroutine(Then(pods.FlyTo(act.index, pad, thrownVel, FlightSeconds(act, pad)), () => landed = true));
                    yield return stage.RecallPlayer(pods.HomeWorld(_activeSlot));
                    pods.ActiveSlot = -1;
                    while (!landed) yield return null;
                    yield return Land(act.index);
                }

                foreach (var e in events) Debug.Log($"[Battle] {e}");
                foreach (var e in events)
                    if (e.kind == BattleEventKind.Damage && e.effectiveness != Effectiveness.Normal)
                    {
                        Dialogue.Say("battle.types");
                        break;
                    }
                yield return stage.Play(events, battle, runes.SetCaption);
                UpdateCradle(battle);
            }

            pods.CradlePulse = false;
            var me = battle.player.isling;
            Debug.Log($"[EncounterDirector] Battle over: {battle.Result}. {me.Name} HP {me.currentHp}/{me.MaxHp}, XP {me.xp}. " +
                      $"Party {party.members.Count}/{Party.Size}, storage {party.storage.Count}.");
            if (battle.Result == BattleResult.Won || battle.Result == BattleResult.Caught) yield return Reward(battle, wild, rare);
            yield return stage.Outro(battle.Result);
            if (battle.Result == BattleResult.Won) Dialogue.Say("battle.win.first");
            else if (battle.Result == BattleResult.Lost) Dialogue.Say("battle.lose.first");
            yield return Finish(battle.Result == BattleResult.Lost);
            BattleEnded?.Invoke(battle.Result);
        }

        /// <summary>
        /// After a win: every creature that fought gets its XP. The one on the pad shows it (a big "+XP", its XP bar large,
        /// level-ups) and, if due, evolves there; the others' pods glow with "+XP" (step 9f) and they evolve in their pods
        /// after the zoom-out (<see cref="GrowthDirector"/>).
        /// </summary>
        IEnumerator Reward(BattleSystem battle, IslingInstance wild, bool rare)
        {
            if (battle.Result == BattleResult.Won)
            {
                GameAudio.Ui(Diorama.Audio.AudioIds.JingleVictory);
                Diorama.Audio.MusicDirector.Jingle(3f);
            }
            if (progression == null) yield break;
            runes.HideMoves();
            // Step 9g: x2 in the Quick Journey's Cinderwood (QuickJourneyConfig.xpMultiplier).
            int reward = QuickJourney.Reward(progression.Reward(wild.level, rare));
            var fighters = battle.Participants;
            var onPad = stage.Player != null ? battle.player.isling : null;
            // The benched ones first (their pods light up together), the one on the pad last.
            var order = new List<IslingInstance>(fighters);
            if (order.Remove(onPad)) order.Add(onPad);
            foreach (var c in order)
            {
                int xp = Progression.ShareFor(c, reward, fighters.Count, progression);
                if (xp <= 0) continue;
                float before = progression.XpShare(c);
                int was = c.level;
                var ups = Progression.AddXp(c, xp, progression);
                Debug.Log($"[EncounterDirector] {c.Name} +{xp} XP{(ups.Count > 0 ? $", Lv {was} -> {c.level}" : "")} " +
                          $"({c.xp}/{progression.XpToNext(c.level)}){(c == onPad ? "" : " (in its pod)")}.");
                if (c != onPad)
                {
                    int slot = pods.SlotOf(c);
                    var pod = pods.Pod(slot);
                    if (pod == null) continue;
                    pods.FlashXp(slot);
                    stage.PopXpAt(pod.transform.position, xp);
                    continue;
                }
                runes.SetCaption($"{c.Name} gained {xp} XP!");
                Dialogue.Say("xp.first");
                yield return stage.PlayXp(c, xp, before, ups);
                if (ups.Count > 0) runes.SetCaption($"{c.Name} grew to Lv {c.level}!");
            }
            party.NotifyChanged();
            if (onPad == null || !Progression.CanEvolve(onPad)) yield break;
            runes.SetCaption($"What? {onPad.Name} is evolving!");
            Dialogue.Say("evolve.first");
            yield return stage.Evolve(onPad);
            runes.SetCaption($"It became {onPad.Name}!");
            party.NotifyChanged();
        }

        /// <summary>
        /// The cradle pod shows the catch state: plain glass above red, a warm glow in the red (first time: a pop and the
        /// Spirit's hint), a strong pulse while dazed (set by <see cref="Dazed"/>).
        /// </summary>
        void UpdateCradle(BattleSystem battle)
        {
            bool red = battle.CatchUnlocked;
            if (red == pods.CradleGlow) return;
            pods.CradleGlow = red;
            if (!red) return;
            Debug.Log($"[EncounterDirector] Wild {battle.wild.isling.Name} is in the red ({battle.wild.Hp}/{battle.wild.MaxHp}): the cradle pod glows.");
            pods.PresentCradle();
            if (!battle.WildDazed) Dialogue.Say("capture.first");
        }

        /// <summary>
        /// The cradle pod was thrown or picked: a miss drops it back into the cradle (reports false, no turn used); a hit
        /// flies it to the wild creature and plays the catch (<see cref="BattleSystem.LastCatch"/> holds the events).
        /// </summary>
        IEnumerator CatchThrow(Act act, BattleSystem battle, IslingInstance wild, System.Action<bool> hit)
        {
            Vector3 target = stage.Wild.LocalCenter;
            bool thrown = act.kind == ActKind.PodThrow;
            if (thrown && !Aimed(act, target, catchAimAngle))
            {
                Debug.Log($"[EncounterDirector] Catch throw missed ({ThrowInfo(act, target)}).");
                runes.SetCaption("Missed!");
                yield return pods.Miss(PodRack.CradleSlot, act.vel);
                hit(false);
                yield break;
            }
            bool locked = !battle.CatchUnlocked;
            Debug.Log($"[EncounterDirector] Catch pod {(thrown ? $"thrown ({ThrowInfo(act, target)})" : "sent by look + pinch")}, " +
                      $"{(locked ? "above red: it will break" : $"chance {battle.CatchChance:P0}")}{(battle.WildDazed ? " (dazed)" : "")}.");
            runes.SetCaption("Go, pod!");
            pods.CradlePulse = false;
            yield return pods.FlyTo(PodRack.CradleSlot, target, thrown ? act.vel : Vector3.zero, FlightSeconds(act, target));
            yield return PlayCatch(battle.TryCatch(), wild, battle, locked);
            hit(true);
        }

        /// <summary>
        /// The wild creature fainted and slumps, dazed: the cradle pod pulses and a throw (or look + pinch on it) catches
        /// it for sure. After <see cref="dazedSeconds"/> without a hit it fades out and the battle is won.
        /// </summary>
        IEnumerator Dazed(BattleSystem battle, IslingInstance wild)
        {
            UpdateCradle(battle);
            pods.CradlePulse = true;
            Dialogue.Say("capture.dazed.first");
            Debug.Log($"[EncounterDirector] Wild {wild.Name} is dazed: {dazedSeconds:0} s to throw a pod.");
            runes.SetCaption($"{wild.Name} is dazed! Quick, throw a pod!");
            float left = dazedSeconds;
            while (battle.WildDazed && left > 0f)
            {
                _act = default;
                _waiting = true;
                pods.PartyGrab = false;
                pods.CradleGrab = true;
                while (_act.kind == ActKind.None && left > 0f)
                {
                    // Holding the pod pauses the clock: nobody loses the catch mid-throw.
                    if (!pods.AnyHeld) left -= Time.deltaTime;
                    yield return null;
                }
                var act = _act;
                _waiting = false;
                pods.CradleGrab = false;
                if (act.index == PodRack.CradleSlot && (act.kind == ActKind.PodThrow || act.kind == ActKind.PodPick)) Onboarding.Mark("podThrown");
                if (act.index != PodRack.CradleSlot || act.kind != ActKind.PodThrow && act.kind != ActKind.PodPick)
                {
                    if (act.kind == ActKind.PodThrow) yield return pods.FlyHome(act.index);
                    continue;
                }
                bool hit = false;
                yield return CatchThrow(act, battle, wild, h => hit = h);
                if (hit) yield break;
                yield return pods.WaitForCradlePod();
                pods.CradlePulse = true;
                runes.SetCaption($"Missed! {wild.Name} is still dazed. Throw again!");
            }
            if (!battle.WildDazed) yield break;
            pods.CradlePulse = false;
            Debug.Log($"[EncounterDirector] No pod in {dazedSeconds:0} s: {wild.Name} fades away, battle won.");
            battle.EndDaze();
            runes.SetCaption($"{wild.Name} slipped away.");
            yield return stage.FadeWild();
        }

        bool HasReserve()
        {
            for (int i = 0; i < Party.Size; i++)
                if (i != _activeSlot && party.CanFight(i)) return true;
            return false;
        }

        float AutoSendSeconds => Onboarding.EncountersEnabled ? autoSendSeconds : tutorialAutoSendSeconds;

        /// <summary>Island-local point a pod lands on: the top of the party creature's pad.</summary>
        Vector3 PodOnPad() => stage.PadTopLocal(BattleSide.Player) + Vector3.up * pods.podPrefab.radius;

        float WorldSpeed(Act act) => act.vel.magnitude * island.transform.lossyScale.x;

        bool Aimed(Act act, Vector3 target, float maxAngle) =>
            WorldSpeed(act) >= minThrowSpeed && PodRack.AimAngle(act.pos, act.vel, target) <= maxAngle;

        /// <summary>A drop above the island, or a throw toward the pad.</summary>
        bool OntoIsland(Act act, Vector3 pad) =>
            island.FootprintContains(act.pos) && act.pos.y > island.CapTopLocal - 0.01f || Aimed(act, pad, sendAimAngle);

        string ThrowInfo(Act act, Vector3 target) =>
            $"{WorldSpeed(act):0.00} m/s, {PodRack.AimAngle(act.pos, act.vel, target):0} deg off";

        /// <summary>Longer flights for farther targets; a look + pinch starts at the pod's rest spot.</summary>
        float FlightSeconds(Act act, Vector3 target)
        {
            var pod = pods.Pod(act.index);
            Vector3 from = act.kind == ActKind.PodThrow || pod == null ? act.pos : pod.transform.localPosition;
            return Mathf.Clamp(0.25f + Vector3.Distance(from, target) * 1.2f, 0.3f, 0.65f);
        }

        /// <summary>
        /// Waits for a pod to be thrown or dropped onto the island (or look + pinch, or <see cref="autoSendSeconds"/>
        /// without anyone holding a pod: the lead goes), flies it to the pad and brings the creature out.
        /// Sets <see cref="_activeSlot"/>.
        /// </summary>
        IEnumerator SendOut(string caption, System.Action ran = null)
        {
            runes.SetCaption(caption);
            int lead = party.LeadSlot;
            pods.ActiveSlot = -1;
            pods.GlowSlot = lead;
            pods.PartyGrab = true;
            pods.CradleGrab = false;
            _act = default;
            _waiting = true;
            Vector3 pad = PodOnPad();
            float idle = 0f;
            int chosen = -1;
            string how = "";
            Vector3 vel = Vector3.zero;
            if (ran != null) runes.SetInteractable(true); // only Run shows
            AwaitingSendOut = true;
            while (chosen < 0)
            {
                var act = _act;
                if (act.kind == ActKind.Run && ran != null)
                {
                    _waiting = false;
                    runes.SetInteractable(false);
                    pods.PartyGrab = false;
                    pods.GlowSlot = -1;
                    AwaitingSendOut = false;
                    ran();
                    yield break;
                }
                if (act.kind == ActKind.PodThrow || act.kind == ActKind.PodPick)
                {
                    _act = default;
                    idle = 0f;
                    if (act.index == PodRack.CradleSlot) { }
                    else if (act.kind == ActKind.PodPick) { chosen = act.index; how = "look + pinch"; }
                    else if (OntoIsland(act, pad)) { chosen = act.index; vel = act.vel; how = $"throw ({ThrowInfo(act, pad)})"; }
                    else
                    {
                        Debug.Log($"[EncounterDirector] Send-out throw went wide ({ThrowInfo(act, pad)}): pod back to its socket.");
                        StartCoroutine(pods.FlyHome(act.index));
                    }
                }
                else if (act.kind != ActKind.None) _act = default; // no moves before a creature is out
                else if (pods.AnyHeld) idle = 0f;
                else if ((idle += Time.deltaTime) >= AutoSendSeconds) { chosen = lead; how = $"auto after {AutoSendSeconds:0} s"; }
                if (chosen < 0) yield return null;
            }
            // The player sent it (the lesson's praise); an auto send-out isn't theirs.
            if (!how.StartsWith("auto")) Onboarding.Mark("sentOut");
            _waiting = false;
            AwaitingSendOut = false;
            runes.SetInteractable(false);
            pods.PartyGrab = false;
            pods.GlowSlot = -1;
            Debug.Log($"[EncounterDirector] Send out pod {chosen + 1} ({party.At(chosen).Name}) by {how}.");
            yield return pods.FlyTo(chosen, pad, vel, 0.45f);
            yield return Land(chosen);
        }

        /// <summary>The pod is on the pad: the creature pops out, the empty pod flies home, bars and runes update.</summary>
        IEnumerator Land(int slot)
        {
            var c = party.At(slot);
            _activeSlot = slot;
            pods.ActiveSlot = slot;
            stage.Spawn(c, BattleSide.Player, _layout.myPad, _layout.wildPad);
            runes.Avoid(stage.Player, stage.Wild);
            runes.SetCaption($"Go, {c.Name}!");
            StartCoroutine(pods.FlyHome(slot, 0.5f));
            yield return stage.PodBurst();
            stage.ShowPlayerBar(c);
            runes.Show(c.species.moves);
        }

        /// <summary>
        /// The pod hit: the wild creature goes in, the pod drops and wobbles, then it's caught or the glass cracks and
        /// shatters and the creature bursts out (always, above red: <paramref name="locked"/>). A new pod fills the cradle.
        /// </summary>
        IEnumerator PlayCatch(IReadOnlyList<BattleEvent> events, IslingInstance wild, BattleSystem battle, bool locked)
        {
            if (events.Count == 0) yield break;
            var e = events[0];
            bool caught = e.result == BattleResult.Caught;
            const int slot = PodRack.CradleSlot;
            var pod = pods.Pod(slot);
            if (pod == null) yield break;
            yield return stage.SuckIntoPod(pod.transform.position);
            yield return pods.FlyTo(slot, stage.PadTopLocal(BattleSide.Wild) + Vector3.up * pods.RestHeight(slot), Vector3.zero, 0.3f, 0.005f);
            yield return new WaitForSeconds(0.25f);
            for (int i = 0; i < e.amount; i++)
            {
                yield return pods.Wobble(slot);
                yield return new WaitForSeconds(0.3f);
            }
            if (!caught)
            {
                if (locked)
                {
                    runes.SetCaption("Weaken it first!");
                    Dialogue.Say("capture.locked");
                }
                yield return pods.Shatter(slot, ElementStyle.Of(wild.Element).main);
                yield return stage.BreakFree();
                if (battle.IsOver)
                {
                    // A rare one woke up in the pod and ran off.
                    runes.SetCaption($"{wild.Name} broke free and ran off!");
                    yield return stage.FadeWild();
                }
                else runes.SetCaption($"Oh no! {wild.Name} broke free!");
                yield return new WaitForSeconds(0.5f);
                yield break;
            }

            // Caught while dazed: it wakes up in the pod (1 HP), so it doesn't join as a fainted, cracked pod.
            if (wild.currentHp <= 0) wild.currentHp = 1;
            stage.CatchSparkle(pod.transform.position, "Caught!");
            GameAudio.At(Diorama.Audio.AudioIds.CatchSuccess, pod.transform.position);
            GameAudio.Ui(Diorama.Audio.AudioIds.JingleCatch);
            Diorama.Audio.MusicDirector.Jingle(3f);
            stage.RemoveWild();
            runes.SetCaption($"Gotcha! {wild.Name} joined you!");
            Dialogue.Say("capture.success.first");
            yield return new WaitForSeconds(0.7f);
            if (!party.IsFull)
            {
                int to = party.members.Count;
                pods.AdoptCradlePod(to, wild);
                party.Add(wild);
                Debug.Log($"[EncounterDirector] Caught {wild.Name}: pod {to + 1}.");
                Caught?.Invoke(wild);
                if (pods.Pod(to) != null) GameAudio.On(Diorama.Audio.AudioIds.CatchToRim, pods.Pod(to).transform);
                yield return pods.FlyHome(to, 0.7f);
            }
            else
            {
                party.Add(wild);
                Debug.Log($"[EncounterDirector] Caught {wild.Name}: pods full, sent to storage ({party.storage.Count}).");
                Caught?.Invoke(wild);
                runes.SetCaption($"Pods full: {wild.Name} will wait at the Hearth Lodge.");
                Dialogue.Say("party.full");
                yield return pods.StreakAway(slot);
            }
        }

        static IEnumerator Then(IEnumerator routine, System.Action done)
        {
            yield return routine;
            done();
        }

        IEnumerator Finish(bool lost)
        {
            CurrentBattle = null;
            Keeper = null;
            runes.Hide();
            pods.InBattle = pods.PartyGrab = pods.CradleGrab = pods.CradleVisible = pods.CradleGlow = pods.CradlePulse = false;
            pods.ActiveSlot = pods.GlowSlot = -1;
            _waiting = false;
            AwaitingSendOut = false;
            _activeSlot = -1;
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
            if (lost)
            {
                // Lost inside (a Keeper's hall): outside first, then the shrine.
                if (SceneSwap.IsInside && SceneSwap.Current != null) SceneSwap.Current.ReturnNow();
                bool atShrine = shrines != null && shrines.LastShrine.HasValue;
                if (shrines != null) shrines.Respawn();
                else explorer.Teleport(explorer.SpawnTile);
                party.HealAll();
                Debug.Log($"[EncounterDirector] Lost: explorer back at {(atShrine ? "the last shrine" : "spawn")}, party healed.");
            }
            _stepsSinceBattle = 0;
            explorer.Locked = false;
            MapCreature.HideAll = false;
            InBattle = false;
        }

        IEnumerator NewSparkle()
        {
            yield return new WaitForSeconds(0.25f);
            if (stage.Wild != null) stage.CatchSparkle(stage.Wild.WorldCenter, "New!");
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

        /// <summary>Fixed pads for a Keeper battle; the explorer stands beside the party's pad.</summary>
        StageLayout KeeperLayout(KeeperFight f)
        {
            Vector2 line = ((Vector2)(f.keeperPad - f.myPad)).normalized;
            Vector2 side = new Vector2(-line.y, line.x);
            Vector2 pivot = ((Vector2)f.myPad + f.keeperPad) * 0.5f;
            var layout = new StageLayout
            {
                myPad = f.myPad,
                wildPad = f.keeperPad,
                towardPlayer = -line,
                pivot = pivot,
                explorerSpot = f.myPad - line * 0.35f + side * 0.85f,
            };
            layout.zoom = Mathf.Min(FitZoom(pivot, new[] { (Vector2)f.myPad, f.keeperPad }), f.maxZoom);
            return layout;
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
            bool Free(Vector2Int t) => InMap(t) && map.CanWalk(t);
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
