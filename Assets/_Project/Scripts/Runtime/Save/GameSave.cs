using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Diorama
{
    /// <summary>
    /// Save / load (one JSON file per slot, <see cref="SaveGame"/>; step 9g: the active <see cref="SaveSlots"/> slot). On start: a save restores the party and storage, the
    /// explorer's tile and facing, the last shrine, the realm's restoration, the dialogue lines already said, the caught
    /// species and the walk mode; no save = the scene's new game (Kilnpot Lv 5 for now); a bad save is renamed and the
    /// game starts new. Autosaves at a shrine (with a small "Saved" puff by the Spirit), after a catch, after a battle,
    /// when the app pauses or loses focus (headset off) and on quit. Runs after the other scripts' Start (order 100).
    /// Debug (Editor and development builds, also BattleDebug): K = save now, Shift+K = delete the save.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class GameSave : MonoBehaviour
    {
        public Party party;
        public GestureTestPiece explorer;
        public HearthShrines shrines;
        public EncounterDirector encounters;
        public RealmRestoration realm;
        public SpeciesRegistry registry;
        [Tooltip("Interiors (Hearth Lodge): saved inside = loaded inside.")]
        public SceneSwap swap;
        [Tooltip("For the \"Saved\" puff.")]
        public BattleHud hud;
        [Tooltip("The puff appears above it (the Lantern Spirit).")]
        public Transform spirit;
        [Tooltip("Optional. Saves which tutorial lesson the player is on.")]
        public Onboarding onboarding;

        readonly HashSet<string> _caught = new HashSet<string>();
        bool _ready;

        /// <summary>The active slot's file (step 9g: save.json = Full adventure, quick.json = Quick Journey).</summary>
        public static string SavePath => SaveSlots.ActivePath;

        /// <summary>A save was found and applied at start (returning player: no opening).</summary>
        public bool Loaded { get; private set; }

        /// <summary>No saving while set (the opening, before a starter is chosen: quitting then replays the opening).</summary>
        public bool Suspended { get; set; }

        /// <summary>Species ids caught so far (for the future "Caught 10 / 30" counter).</summary>
        public IReadOnlyCollection<string> Caught => _caught;

        /// <summary>The outdoor realm (its world map's name), also while an interior shows.</summary>
        string RealmId
        {
            get
            {
                var world = swap != null ? swap.Outdoor : explorer != null && explorer.island != null ? explorer.island.world : null;
                return world != null ? world.name : "none";
            }
        }

        void Start()
        {
            if (party == null || explorer == null || registry == null)
            {
                Debug.LogError("[GameSave] Needs the party, explorer and species registry (Set Up MR_Main (7 progression)); " +
                               "the game won't load or save.", this);
                enabled = false;
                return;
            }
            if (encounters != null) encounters.IsCaught = s => s != null && _caught.Contains(SpeciesRegistry.IdOf(s));
            Load();
            _ready = true;
            PuzzleState.Changed += OnPuzzleChanged;
            if (shrines != null) shrines.Rested += OnRested;
            if (encounters != null)
            {
                encounters.Caught += OnCaught;
                encounters.BattleEnded += OnBattleEnded;
            }
            if (swap != null)
            {
                swap.Finished += OnSwapFinished;
                swap.TravelFinished += OnTravelFinished;
            }
        }

        void OnSwapFinished(bool inside) => Save(inside ? "entered " + swap.Interior.name : "left the interior");

        void OnTravelFinished(WorldMap to) => Save("travelled to " + to.name);

        void OnPuzzleChanged(string id) => Save($"puzzle {id} {(PuzzleState.IsSolved(id) ? "solved" : $"stage {PuzzleState.Stage(id)}")}");

        void OnDestroy()
        {
            PuzzleState.Changed -= OnPuzzleChanged;
            if (shrines != null) shrines.Rested -= OnRested;
            if (encounters != null)
            {
                encounters.Caught -= OnCaught;
                encounters.BattleEnded -= OnBattleEnded;
            }
            if (swap != null)
            {
                swap.Finished -= OnSwapFinished;
                swap.TravelFinished -= OnTravelFinished;
            }
        }

        void OnRested(Vector2Int shrine) => Save("shrine", true);

        void OnCaught(IslingInstance c)
        {
            _caught.Add(SpeciesRegistry.IdOf(c.species));
            Save("catch");
        }

        void OnBattleEnded(BattleResult result) => Save($"battle {result}");

        void OnApplicationPause(bool paused)
        {
            if (paused) Save("app paused");
        }

        // The Editor loses focus all the time (clicking another window); only devices save on it.
        void OnApplicationFocus(bool focused)
        {
            if (!focused && !Application.isEditor) Save("focus lost");
        }

        void OnApplicationQuit() => Save("quit");

        void Update()
        {
            if (!Application.isEditor && !Debug.isDebugBuild) return;
            var kb = Keyboard.current;
            if (kb == null || !kb.kKey.wasPressedThisFrame) return;
            if (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed) DeleteSave();
            else Save("K key", true);
        }

        // ---- load ----

        void Load()
        {
            var result = SaveGame.Read(SavePath, out var data, out var problem);
            switch (result)
            {
                case SaveGame.ReadResult.NoSave:
                    Debug.Log($"[GameSave] No save at {SavePath}: new game ({Describe(party.members)}).");
                    PuzzleState.Restore(null);
                    NoteParty();
                    break;
                case SaveGame.ReadResult.Bad:
                    Debug.LogWarning($"[GameSave] The save was unreadable ({problem}): renamed to {SaveGame.BadFileName}, new game.");
                    PuzzleState.Restore(null);
                    NoteParty();
                    break;
                default:
                    Apply(data);
                    break;
            }
        }

        void Apply(SaveData data)
        {
            Loaded = true;
            var problems = new List<string>();
            var pods = SaveGame.FromSaved(data.party, registry, problems);
            var stored = SaveGame.FromSaved(data.storage, registry, problems);
            if (pods.Count == 0)
            {
                Debug.LogWarning("[GameSave] The save has no usable pod creature; keeping the new-game party.");
                pods = party.members;
            }
            party.Restore(pods.Take(Party.Size).ToList(), stored);

            // Puzzles first: a lifted log or an open door changes where the explorer can stand.
            PuzzleState.Restore(data.puzzles);

            // Every realm's restoration first, so the map we land on reads its own.
            if (realm != null)
            {
                realm.Forget();
                foreach (var r in data.realms) realm.Remember(r.realm, r.restored);
            }
            var tile = new Vector2Int(data.explorerX, data.explorerY);
            var facing = new Vector2Int(data.facingX, data.facingY);
            var world = swap != null && !string.IsNullOrEmpty(data.map) ? swap.FindWorld(data.map) : null;
            if (world == null) problems.Add($"saved on map '{data.map}', which isn't in the scene swap's worlds: staying on {RealmId}");
            if (world != null && world.CanWalk(tile)) swap.TravelNow(world, tile, facing);
            else if (world == null && explorer.island != null && explorer.island.Content != null && explorer.island.Content.CanWalk(tile)) explorer.Teleport(tile, facing);
            else if (world != null && world.CanWalk(world.startTile))
            {
                // The map changed under the save (step 9a rewrote Cinderwood): start that map at its arrival.
                problems.Add($"explorer tile c{tile.x} r{tile.y} isn't walkable on {world.name} (map changed?): moved to its start c{world.startTile.x} r{world.startTile.y}");
                swap.TravelNow(world, world.startTile, new Vector2Int(0, -1));
            }
            else problems.Add($"explorer tile c{tile.x} r{tile.y} isn't walkable on {(world != null ? world.name : RealmId)} (map changed?): left at spawn");

            if (shrines != null)
            {
                shrines.LastShrine = data.hasShrine ? new Vector2Int(data.shrineX, data.shrineY) : (Vector2Int?)null;
                shrines.LastShrineMap = data.hasShrine ? data.shrineMap : null;
            }
            if (encounters != null) encounters.FirstEncounterDone = data.firstEncounterDone;
            if (onboarding != null) onboarding.Restore(data.lesson, data.tutorialDone);
            if (realm != null) realm.SetNow(data.RestoredFor(RealmId, realm.Recall(swap != null ? swap.Outdoor : explorer.island.world)));
            Dialogue.RestoreSaid(data.said);
            _caught.Clear();
            foreach (var id in data.caught) _caught.Add(id);
            if (encounters != null && data.seen != null)
            {
                encounters.Seen.Clear();
                foreach (var id in data.seen) encounters.Seen.Add(id);
            }
            NoteParty();
            if (Enum.TryParse(data.walkMode, out MoveMode mode)) explorer.moveMode = mode;
            DominantHand.Restore(data.dominantHand);
            if (data.inside)
            {
                var interior = swap != null ? swap.FindInterior(data.interior) : null;
                if (interior == null) problems.Add($"saved inside '{data.interior}', which isn't a known interior: outside instead");
                else
                {
                    swap.SetReturn(explorer.Tile, explorer.Facing);
                    var inTile = new Vector2Int(data.insideX, data.insideY);
                    // The room's layout changed (9i: the lodge door moved to the back wall): a tile that's no longer floor = its entry.
                    if (!interior.CanWalk(inTile))
                    {
                        problems.Add($"inside tile c{inTile.x} r{inTile.y} isn't walkable on {interior.name} (layout changed?): moved to its entry c{interior.startTile.x} r{interior.startTile.y}");
                        inTile = interior.startTile;
                    }
                    swap.EnterNow(interior, inTile, new Vector2Int(data.insideFacingX, data.insideFacingY));
                }
            }

            foreach (var p in problems) Debug.LogWarning($"[GameSave] {p}");
            Debug.Log($"[GameSave] Loaded {SavePath} (saved {data.savedAt}): {Describe(party.members)}; storage {party.storage.Count}; " +
                      $"{data.map} c{data.explorerX} r{data.explorerY}{(data.inside ? $" (inside {data.interior} at c{data.insideX} r{data.insideY})" : "")}; last shrine {(data.hasShrine ? $"c{data.shrineX} r{data.shrineY}" : "none")}; " +
                      $"realm {RealmState.Restored:0.##}; {data.said.Count} lines said; {_caught.Count} species caught; {explorer.moveMode}; " +
                      $"puzzles {PuzzleState.Describe()}.");
        }

        /// <summary>Everything in the pods and storage counts as caught (the starter too).</summary>
        void NoteParty()
        {
            foreach (var c in party.members.Concat(party.storage))
                if (c != null && c.species != null) _caught.Add(SpeciesRegistry.IdOf(c.species));
        }

        static string Describe(List<IslingInstance> list) =>
            string.Join(", ", list.Where(c => c != null).Select(c => $"{c.Name} Lv {c.level}"));

        // ---- save ----

        /// <summary>Writes the save now. <paramref name="puff"/> shows "Saved" by the Spirit.</summary>
        public void Save(string reason, bool puff = false)
        {
            if (!_ready || !enabled || Suspended) return;
            try
            {
                bool inside = swap != null && swap.Inside;
                var outdoor = inside ? swap.ReturnTile : explorer.Tile;
                var outdoorFacing = inside ? swap.ReturnFacing : explorer.Facing;
                var data = new SaveData
                {
                    party = SaveGame.ToSaved(party.members),
                    storage = SaveGame.ToSaved(party.storage),
                    map = RealmId,
                    explorerX = outdoor.x,
                    explorerY = outdoor.y,
                    facingX = outdoorFacing.x,
                    facingY = outdoorFacing.y,
                    inside = inside,
                    interior = inside ? swap.Interior.name : null,
                    insideX = explorer.Tile.x,
                    insideY = explorer.Tile.y,
                    insideFacingX = explorer.Facing.x,
                    insideFacingY = explorer.Facing.y,
                    hasShrine = shrines != null && shrines.LastShrine.HasValue,
                    shrineMap = shrines != null ? shrines.LastShrineMap : null,
                    firstEncounterDone = encounters != null && encounters.FirstEncounterDone,
                    lesson = onboarding != null ? onboarding.CurrentId : null,
                    tutorialDone = onboarding == null || onboarding.CurrentId == null,
                    said = Dialogue.Said.ToList(),
                    caught = _caught.ToList(),
                    seen = encounters != null ? encounters.Seen.ToList() : new List<string>(),
                    walkMode = explorer.moveMode.ToString(),
                    dominantHand = DominantHand.Saved,
                    puzzles = PuzzleState.ToSaved(),
                };
                if (data.hasShrine)
                {
                    data.shrineX = shrines.LastShrine.Value.x;
                    data.shrineY = shrines.LastShrine.Value.y;
                }
                if (realm != null)
                    foreach (var r in realm.Remembered) data.SetRestored(r.Key, r.Value);
                data.SetRestored(RealmId, swap != null ? swap.OutdoorRestored : RealmState.Restored);
                SaveGame.Write(SavePath, data);
                Debug.Log($"[GameSave] Saved ({reason}): {data.party.Count} pods, {data.storage.Count} in storage, explorer c{data.explorerX} r{data.explorerY}" +
                          $"{(inside ? $", inside {data.interior} at c{data.insideX} r{data.insideY}" : "")}.");
                if (puff) Puff();
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameSave] Save failed ({reason}): {e.Message}");
            }
        }

        public void DeleteSave()
        {
            SaveGame.Delete(SavePath);
            Debug.Log($"[GameSave] Deleted {SavePath}. The next start is a new game (quit without saving: autosave on quit writes it again).");
        }

        void Puff()
        {
            if (hud == null || spirit == null) return;
            Vector3 world = spirit.position + Vector3.up * 0.035f * hud.transform.lossyScale.x;
            GameAudio.Ui(Diorama.Audio.AudioIds.UiSaved);
            hud.PopText(hud.transform.InverseTransformPoint(world), "Saved", new Color(1f, 0.92f, 0.7f), 0.7f, 1.2f);
        }
    }
}
