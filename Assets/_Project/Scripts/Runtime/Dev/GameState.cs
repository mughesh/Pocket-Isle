using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Diorama
{
    /// <summary>
    /// One place to read and change the game's state from tools (step 9e: the dev panel; later the judges' Quick
    /// Journey reuses it): landmarks to teleport to on every map (a scene swap when it's another map or an interior),
    /// party setup, story flags, snapshots of the save.
    /// Live changes (teleport, levels, healing, species) apply at once and save. Story changes edit the save file and
    /// reload the scene (<see cref="EditSave"/>), so every system restores itself the normal way, exactly like a
    /// relaunch; a Quick Journey preset will be the same thing: a <see cref="SaveData"/> plus a reload.
    /// </summary>
    public class GameState : MonoBehaviour
    {
        public static GameState Instance { get; private set; }

        public Party party;
        public GestureTestPiece explorer;
        public SceneSwap swap;
        public GameSave save;
        public EncounterDirector encounters;
        public RealmRestoration realm;
        public Onboarding onboarding;
        public SpeciesRegistry registry;
        [Header("Maps")]
        public WorldMap hub;
        public WorldMap cinderwood;
        public WorldMap forgeHall;
        [Tooltip("Step 10a: the wind realm (Go to works even while its gate is sealed in builds).")]
        public WorldMap galewick;
        public Vector2Int hallEntry = new Vector2Int(4, 8);
        [Tooltip("Step 10e: Galewick's tower hall (Go to, entered from the tower door).")]
        public WorldMap towerHall;
        [Tooltip("Step 11a: the water realm (Go to works even while its gate is sealed in builds).")]
        public WorldMap lilymere;
        public Vector2Int towerHallEntry = new Vector2Int(4, 8);
        [Tooltip("Step 11c: Lilymere's lake temple hall (Go to, entered from the temple door).")]
        public WorldMap lakeTemple;
        public Vector2Int lakeTempleEntry = new Vector2Int(4, 8);
        [Tooltip("Lesson the tutorial restarts at (the first one after the opening).")]
        public string firstWalkLesson = "walk";
        [Tooltip("Who a preset team gets after the lead (species asset names). Step 9f: Steambara (the Keeper is tuned for a Water Isling in the party) and a fire common.")]
        public string[] presetCommons = { "Steambara", "Kettlewing" };

        public const string LogId = "cinder.log", BellowsId = "cinder.bellows", KeeperId = KeeperHall.KeeperId, ShardId = ShardHolder.FireId;

        /// <summary>A place to jump to: a map tile (or an interior's entry).</summary>
        public struct Spot
        {
            public string label;
            public WorldMap map;
            public Vector2Int tile;
            public Vector2Int facing;
            public bool interior;
            /// <summary>An interior's outdoor map and the landmark at its door.</summary>
            public WorldMap outdoor;
            public string door;
        }

        readonly List<Spot> _spots = new List<Spot>();

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ---- where ----

        public WorldMap CurrentMap => swap != null && swap.Inside ? swap.Interior : explorer != null && explorer.island != null ? explorer.island.world : null;

        public string Where
        {
            get
            {
                var m = CurrentMap;
                return m == null ? "?" : $"{m.name} c{explorer.Tile.x} r{explorer.Tile.y}{(swap != null && swap.Inside ? $" (inside, from {swap.Outdoor.name})" : "")}";
            }
        }

        /// <summary>Landmarks per map (hub first, then Cinderwood, then the Forge Hall).</summary>
        public IReadOnlyList<Spot> Spots
        {
            get
            {
                if (_spots.Count == 0) BuildSpots();
                return _spots;
            }
        }

        void BuildSpots()
        {
            var north = new Vector2Int(0, -1);
            if (hub != null)
            {
                AddMark(hub, "Hub: start", "start");
                AddProp(hub, "Hub: shrine", HearthShrines.RecipeName);
                AddMark(hub, "Hub: statue", "statue");
                AddMark(hub, "Hub: first grass", "firstGrass");
                foreach (var g in hub.gates) Add(hub, $"Hub: {g.id} gate", g.Front(0), north);
            }
            if (cinderwood != null)
            {
                AddMark(cinderwood, "CW: arrival", "start");
                AddMark(cinderwood, "CW: village", "plaza");
                AddMark(cinderwood, "CW: shrine", "shrine");
                AddMark(cinderwood, "CW: hollow", "hollow"); // step 10f
                AddMark(cinderwood, "CW: springs", "springs");
                AddMark(cinderwood, "CW: ruins", "ruins");
                AddMark(cinderwood, "CW: log", "log");
                AddMark(cinderwood, "CW: forge door", "temple");
            }
            if (forgeHall != null)
                _spots.Add(new Spot { label = "Forge Hall", map = forgeHall, tile = hallEntry, facing = north, interior = true, outdoor = cinderwood, door = "temple" });
            if (galewick != null)
            {
                AddMark(galewick, "GW: arrival", "start");
                AddMark(galewick, "GW: village", "plaza");
                AddMark(galewick, "GW: big mill", "mill");
                AddMark(galewick, "GW: shrine", "shrine");
                AddMark(galewick, "GW: kite field", "kites");
                AddMark(galewick, "GW: ridge", "ridge");
                AddMark(galewick, "GW: tower", "tower");
                AddMark(galewick, "GW: rare grass", "rare");
                AddMark(galewick, "GW: fire patch", "fire");
                if (towerHall != null)
                    _spots.Add(new Spot { label = "GW: tower hall", map = towerHall, tile = towerHallEntry, facing = north, interior = true, outdoor = galewick, door = "tower" });
            }
            if (lilymere != null)
            {
                AddMark(lilymere, "LM: arrival", "start");
                AddMark(lilymere, "LM: stream", "stream");
                AddMark(lilymere, "LM: village", "village");
                AddMark(lilymere, "LM: shrine", "shrine");
                AddMark(lilymere, "LM: channel", "channel");
                AddMark(lilymere, "LM: garden", "garden");
                AddMark(lilymere, "LM: basin", "basin");
                AddMark(lilymere, "LM: temple", "temple");
                AddMark(lilymere, "LM: marsh", "marsh");
                AddMark(lilymere, "LM: rare pond", "pond");
                if (lakeTemple != null)
                    _spots.Add(new Spot { label = "LM: lake temple", map = lakeTemple, tile = lakeTempleEntry, facing = north, interior = true, outdoor = lilymere, door = "temple" });
            }
        }

        void AddMark(WorldMap map, string label, string landmark)
        {
            if (map.TryGetLandmark(landmark, out var t)) Add(map, label, t, new Vector2Int(0, -1));
        }

        void AddProp(WorldMap map, string label, string recipeName)
        {
            for (int r = 0; r < map.Rows; r++)
                for (int c = 0; c < map.Columns; c++)
                {
                    char k = map.PropAt(r, c);
                    if (k == '.' || !map.TryGetProp(k, out var entry) || entry.recipe == null || entry.recipe.name != recipeName) continue;
                    // The tile just in front (south, toward the player) of its footprint.
                    var size = WorldMap.FootprintOf(entry);
                    Add(map, label, new Vector2Int(c, r + size.y), new Vector2Int(0, -1));
                    return;
                }
        }

        void Add(WorldMap map, string label, Vector2Int tile, Vector2Int facing) =>
            _spots.Add(new Spot { label = label, map = map, tile = NearestWalkable(map, tile), facing = facing });

        /// <summary>The walkable tile nearest to <paramref name="tile"/> (itself when walkable).</summary>
        public static Vector2Int NearestWalkable(WorldMap map, Vector2Int tile)
        {
            for (int radius = 0; radius < 8; radius++)
                for (int dy = -radius; dy <= radius; dy++)
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != radius) continue;
                        var t = tile + new Vector2Int(dx, dy);
                        if (t.x >= 0 && t.y >= 0 && t.x < map.Columns && t.y < map.Rows && map.CanWalk(t)) return t;
                    }
            return tile;
        }

        /// <summary>Teleports the explorer (scene swap when the spot is on another map or inside). False while busy.</summary>
        public bool GoTo(Spot spot)
        {
            if (swap == null || explorer == null || SceneSwap.IsBusy || encounters != null && encounters.InBattle) return false;
            if (spot.interior)
            {
                var outdoor = spot.outdoor;
                if (outdoor == null || !outdoor.TryGetLandmark(spot.door, out var door)) return false;
                var front = NearestWalkable(outdoor, door + new Vector2Int(0, 1));
                if (swap.Inside) swap.ReturnNow();
                if (explorer.island.world != outdoor) swap.TravelNow(outdoor, front, new Vector2Int(0, -1));
                else explorer.Teleport(front, new Vector2Int(0, -1));
                swap.Enter(spot.map, spot.tile, spot.facing, front, new Vector2Int(0, 1));
            }
            else
            {
                if (swap.Inside) swap.ReturnNow();
                if (explorer.island.world != spot.map) swap.TravelNow(spot.map, spot.tile, spot.facing);
                else explorer.Teleport(spot.tile, spot.facing);
            }
            Debug.Log($"[GameState] Go to {spot.label} ({spot.map.name} c{spot.tile.x} r{spot.tile.y}).");
            if (!spot.interior && save != null) save.Save("dev: go to " + spot.label);
            return true;
        }

        // ---- party ----

        public void HealAll()
        {
            party.HealEveryone();
            Saved("healed");
        }

        /// <summary>The lead's level + <paramref name="delta"/> (healed).</summary>
        public void LeadLevel(int delta)
        {
            var c = party.Lead ?? (party.members.Count > 0 ? party.members[0] : null);
            if (c == null) return;
            SetLevel(c, c.level + delta);
            Saved($"lead to Lv {c.level}");
        }

        public void AllLevels(int level)
        {
            foreach (var c in party.members) SetLevel(c, level);
            Saved($"party to Lv {level}");
        }

        static void SetLevel(IslingInstance c, int level)
        {
            if (c == null) return;
            c.level = Mathf.Clamp(level, 1, 30);
            c.xp = 0;
            c.Heal();
        }

        /// <summary>
        /// A team of three at <paramref name="level"/>: the first pod's species (the starter), Steambara and a fire common; the
        /// other pod creatures move to storage.
        /// </summary>
        public void PresetTeam(int level)
        {
            var lead = party.members.Count > 0 ? party.members[0].species : null;
            var team = new List<IslingInstance>();
            if (lead != null) team.Add(new IslingInstance(lead, level));
            foreach (var n in presetCommons)
            {
                var s = registry != null ? registry.Find(n) : null;
                if (s != null) team.Add(new IslingInstance(s, level));
            }
            var stored = new List<IslingInstance>(party.storage);
            for (int i = 1; i < party.members.Count; i++) stored.Insert(0, party.members[i]);
            party.Restore(team, stored);
            party.NotifyChanged();
            Saved($"team of {team.Count} at Lv {level}");
        }

        public void AddSpecies(IslingSpecies species, int level)
        {
            if (species == null) return;
            int slot = party.Add(new IslingInstance(species, level));
            Saved($"{species.displayName} Lv {level} {(slot >= 0 ? $"in pod {slot + 1}" : "in storage")}");
        }

        public void ClearStorage()
        {
            party.storage.Clear();
            party.NotifyChanged();
            Saved("storage cleared");
        }

        /// <summary>Species the panel can add or fight (no Keepers, not the Spirit).</summary>
        public List<IslingSpecies> Species =>
            registry == null ? new List<IslingSpecies>() : registry.species.Where(s => s != null && !s.keeper && s.Prefab != null && s.name != "LanternSpirit").ToList();

        /// <summary>A wild battle right here (not inside, not in a battle).</summary>
        public bool Fight(IslingSpecies species, int level) =>
            species != null && encounters != null && encounters.DebugEncounter(new IslingInstance(species, level));

        void Saved(string what)
        {
            Debug.Log($"[GameState] {what}.");
            if (save != null) save.Save("dev: " + what);
        }

        // ---- story (save edits + reload) ----

        public bool TutorialDone => onboarding == null || onboarding.CurrentId == null;

        public bool Restored(WorldMap map)
        {
            if (map == null) return false;
            if (swap != null && swap.Outdoor == map && !swap.Inside) return RealmState.Restored >= 0.5f;
            return realm != null && realm.Remembered.TryGetValue(map.name, out float r) && r >= 0.5f;
        }

        public static bool Solved(string id) => PuzzleState.IsSolved(id);

        public static bool ShardHeld => PuzzleState.Stage(ShardId) >= 1 && !PuzzleState.IsSolved(ShardId);

        /// <summary>
        /// Saves now, edits the save file and reloads the scene (the game restores from it like a relaunch). False when
        /// there is no save yet (the opening isn't done).
        /// </summary>
        public bool EditSave(Action<SaveData> edit, string what)
        {
            if (save == null) return false;
            save.Save("dev: before " + what);
            if (SaveGame.Read(GameSave.SavePath, out var data, out var problem) != SaveGame.ReadResult.Loaded)
            {
                Debug.LogWarning($"[GameState] No save to edit ({problem ?? "none yet"}): {what} skipped.");
                return false;
            }
            edit(data);
            SaveGame.Write(GameSave.SavePath, data);
            Debug.Log($"[GameState] Save edited ({what}); reloading.");
            Reload();
            return true;
        }

        /// <summary>Reloads the scene without saving on the way out (it restores from the save file).</summary>
        public void Reload()
        {
            if (save != null) save.Suspended = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public bool SkipTutorial() => EditSave(d =>
        {
            d.tutorialDone = true;
            d.lesson = null;
            d.firstEncounterDone = true;
        }, "skip the tutorial");

        public bool ResetTutorial() => EditSave(d =>
        {
            d.tutorialDone = false;
            d.lesson = firstWalkLesson;
            d.firstEncounterDone = false;
        }, "reset the tutorial");

        public bool SetRestored(WorldMap map, bool on) => map != null && EditSave(d => d.SetRestored(map.name, on ? 1f : 0f), $"{map.name} restored {on}");

        /// <summary>A puzzle solved (at its last stage), at a stage, or reset (<paramref name="stage"/> 0, not solved).</summary>
        public bool SetPuzzle(string id, bool solved, int stage) => EditSave(d => Puzzle(d, id, solved, stage), $"{id} {(solved ? "solved" : $"stage {stage}")}");

        static void Puzzle(SaveData d, string id, bool solved, int stage)
        {
            d.puzzles.RemoveAll(p => p.id == id);
            if (solved || stage > 0) d.puzzles.Add(new SavedPuzzle { id = id, solved = solved, stage = stage });
        }

        public bool ResetStory() => EditSave(d =>
        {
            d.puzzles.Clear();
            if (cinderwood != null) d.SetRestored(cinderwood.name, 0f);
            if (galewick != null) d.SetRestored(galewick.name, 0f);
        }, "reset all story");

        // ---- saves ----

        public static string SnapshotDir => Path.Combine(Application.persistentDataPath, "snapshots");

        static string SlotPath(int slot) => Path.Combine(SnapshotDir, $"slot{slot}.json");

        /// <summary>Slot 0 is the automatic one (taken before a new game).</summary>
        public static string SlotLabel(int slot)
        {
            string path = SlotPath(slot) + ".txt";
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        public bool SaveSlot(int slot)
        {
            if (save == null) return false;
            save.Save($"dev: snapshot {slot}");
            if (!File.Exists(GameSave.SavePath)) return false;
            Directory.CreateDirectory(SnapshotDir);
            File.Copy(GameSave.SavePath, SlotPath(slot), true);
            var lead = party != null ? party.Lead : null;
            File.WriteAllText(SlotPath(slot) + ".txt",
                $"{DateTime.Now:MMM d HH:mm} {(CurrentMap != null ? CurrentMap.name.Replace("WorldMap_", "") : "?")}{(lead != null ? $" {lead.Name} Lv {lead.level}" : "")}");
            Debug.Log($"[GameState] Snapshot {slot}: {SlotLabel(slot)}.");
            return true;
        }

        public bool LoadSlot(int slot)
        {
            if (!File.Exists(SlotPath(slot))) return false;
            if (save != null) save.Suspended = true;
            File.Copy(SlotPath(slot), GameSave.SavePath, true);
            Debug.Log($"[GameState] Snapshot {slot} loaded; reloading.");
            Reload();
            return true;
        }

        /// <summary>A new game (the opening plays) in the slot being played. The current save goes to the automatic slot 0 first.</summary>
        public void NewGame()
        {
            SaveSlot(0);
            if (save != null)
            {
                save.Suspended = true;
                save.DeleteSave();
            }
            SaveSlots.PendingNew = SaveSlots.Active;
            Reload();
        }

        // ---- Quick Journey presets (step 9g) ----

        public enum QuickStage { Start, Gift, Gate, Temple }

        /// <summary>Step 14c: the Galewick beats done at the Quick "temple" preset (the tower hall door), with their saved stages.</summary>
        public static readonly (string id, int stage)[] QuickTowerSolved =
        {
            ("gw.arrive", 3), ("gw.mill", 1), ("gw.kites", 7), ("gw.clouds", 7), ("gw.tower", 1),
        };

        /// <summary>
        /// Jumps into the quick slot at a stage of the route: Start = the opening from the shard; Gift = in the hub at the
        /// first grass with the tutorial done (the gift comes); Gate = after the gift at the wind gate; Temple = Galewick at
        /// the tower hall door, the arrival, mill, kites and tower done (step 14c: the quick realm is Galewick). Writes
        /// quick.json and reloads (like a relaunch).
        /// </summary>
        public void QuickPreset(QuickStage stage)
        {
            if (save != null) save.Suspended = true;
            Diorama.SaveSlots.Choose(Diorama.SaveSlot.Quick);
            if (stage == QuickStage.Start)
            {
                Diorama.SaveSlots.Delete(Diorama.SaveSlot.Quick);
                Diorama.SaveSlots.PendingNew = Diorama.SaveSlot.Quick;
                Debug.Log("[GameState] Quick Journey preset: start (the opening).");
                Reload();
                return;
            }
            var config = QuickJourney.Config;
            var lead = party != null && party.members.Count > 0 ? party.members[0].species : registry != null ? registry.Find("Kilnpot") : null;
            var (gift, raise) = config != null ? config.LevelsFor(lead) : (8, 8);
            var team = new List<IslingInstance>();
            if (lead != null) team.Add(new IslingInstance(lead, stage == QuickStage.Gift ? 6 : raise));
            if (stage != QuickStage.Gift && config != null)
                foreach (var s in config.giftSpecies)
                    if (s != null) team.Add(new IslingInstance(s, gift));
            var map = stage == QuickStage.Temple ? galewick : hub;
            var tile = map != null ? map.startTile : Vector2Int.zero;
            if (stage == QuickStage.Gift && hub != null && hub.TryGetLandmark("firstGrass", out var grass)) tile = NearestWalkable(hub, grass + new Vector2Int(0, 2));
            if (stage == QuickStage.Gate && hub != null)
            {
                int wind = hub.gates.FindIndex(g => g.id == "wind");
                if (wind >= 0) tile = NearestWalkable(hub, hub.gates[wind].Front(0) + new Vector2Int(0, 1));
            }
            if (stage == QuickStage.Temple && galewick != null && galewick.TryGetLandmark("tower", out var door)) tile = NearestWalkable(galewick, door + new Vector2Int(0, 1));
            var data = new SaveData
            {
                party = SaveGame.ToSaved(team),
                map = map != null ? map.name : null,
                explorerX = tile.x,
                explorerY = tile.y,
                facingX = 0,
                facingY = -1,
                tutorialDone = true,
                firstEncounterDone = true,
                said = Dialogue.Said.ToList(),
                caught = team.Select(c => SpeciesRegistry.IdOf(c.species)).Distinct().ToList(),
                walkMode = explorer != null ? explorer.moveMode.ToString() : null,
                dominantHand = DominantHand.Saved,
            };
            if (stage != QuickStage.Gift) data.puzzles.Add(new SavedPuzzle { id = QuickJourney.GiftId, solved = true, stage = 1 });
            if (stage == QuickStage.Temple)
                foreach (var (id, done) in QuickTowerSolved)
                    data.puzzles.Add(new SavedPuzzle { id = id, solved = true, stage = done });
            SaveGame.Write(Diorama.SaveSlots.PathOf(Diorama.SaveSlot.Quick), data);
            Debug.Log($"[GameState] Quick Journey preset: {stage} ({data.map} c{tile.x} r{tile.y}, {string.Join(", ", team.Select(c => $"{c.Name} Lv {c.level}"))}).");
            Reload();
        }

        // ---- a summary (bug reports) ----

        public string Describe()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Where: {Where}");
            sb.AppendLine($"Party: {string.Join(", ", party.members.Select(c => $"{c.Name} Lv {c.level} {c.currentHp}/{c.MaxHp}"))}");
            sb.AppendLine($"Storage: {party.storage.Count}");
            sb.AppendLine($"Tutorial: {(TutorialDone ? "done" : $"at '{onboarding.CurrentId}'")}");
            sb.AppendLine($"Cinderwood restored: {Restored(cinderwood)}");
            if (galewick != null) sb.AppendLine($"Galewick restored: {Restored(galewick)}");
            sb.AppendLine($"Story: {string.Join(", ", PuzzleState.ToSaved().Select(p => $"{p.id} {(p.solved ? "solved" : $"stage {p.stage}")}"))}");
            sb.AppendLine($"In battle: {encounters != null && encounters.InBattle}");
            sb.AppendLine($"Island placement: {IslandPlacer.LastPlacement}");
            sb.AppendLine($"Cheats: one-hit {DevCheats.OneHitKo}, invincible {DevCheats.Invincible}, wild off {DevCheats.WildOff}, rare x{DevCheats.RareBoost}, time x{Time.timeScale}");
            return sb.ToString();
        }
    }
}
