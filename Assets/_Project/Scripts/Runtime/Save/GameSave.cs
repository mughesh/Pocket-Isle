using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Diorama
{
    /// <summary>
    /// Save / load (one JSON file, <see cref="SaveGame"/>). On start: a save restores the party and storage, the
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

        readonly HashSet<string> _caught = new HashSet<string>();
        bool _ready;

        public static string SavePath => SaveGame.DefaultPath;

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
            Load();
            _ready = true;
            if (shrines != null) shrines.Rested += OnRested;
            if (encounters != null)
            {
                encounters.Caught += OnCaught;
                encounters.BattleEnded += OnBattleEnded;
            }
            if (swap != null) swap.Finished += OnSwapFinished;
        }

        void OnSwapFinished(bool inside) => Save(inside ? "entered " + swap.Interior.name : "left the interior");

        void OnDestroy()
        {
            if (shrines != null) shrines.Rested -= OnRested;
            if (encounters != null)
            {
                encounters.Caught -= OnCaught;
                encounters.BattleEnded -= OnBattleEnded;
            }
            if (swap != null) swap.Finished -= OnSwapFinished;
        }

        void OnRested(Vector2Int shrine) => Save("shrine", true);

        void OnCaught(CreatureInstance c)
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
                    NoteParty();
                    break;
                case SaveGame.ReadResult.Bad:
                    Debug.LogWarning($"[GameSave] The save was unreadable ({problem}): renamed to {SaveGame.BadFileName}, new game.");
                    NoteParty();
                    break;
                default:
                    Apply(data);
                    break;
            }
        }

        void Apply(SaveData data)
        {
            var problems = new List<string>();
            var pods = SaveGame.FromSaved(data.party, registry, problems);
            var stored = SaveGame.FromSaved(data.storage, registry, problems);
            if (pods.Count == 0)
            {
                Debug.LogWarning("[GameSave] The save has no usable pod creature; keeping the new-game party.");
                pods = party.members;
            }
            party.Restore(pods.Take(Party.Size).ToList(), stored);

            var tile = new Vector2Int(data.explorerX, data.explorerY);
            var facing = new Vector2Int(data.facingX, data.facingY);
            if (explorer.island != null && explorer.island.Content != null && explorer.island.Content.CanWalk(tile)) explorer.Teleport(tile, facing);
            else problems.Add($"explorer tile c{tile.x} r{tile.y} isn't walkable (map changed?): left at spawn");

            if (shrines != null) shrines.LastShrine = data.hasShrine ? new Vector2Int(data.shrineX, data.shrineY) : (Vector2Int?)null;
            if (realm != null) realm.SetNow(data.RestoredFor(RealmId, RealmState.Restored));
            Dialogue.RestoreSaid(data.said);
            _caught.Clear();
            foreach (var id in data.caught) _caught.Add(id);
            NoteParty();
            if (Enum.TryParse(data.walkMode, out MoveMode mode)) explorer.moveMode = mode;
            if (data.inside)
            {
                var interior = swap != null ? swap.FindInterior(data.interior) : null;
                if (interior == null) problems.Add($"saved inside '{data.interior}', which isn't a known interior: outside instead");
                else
                {
                    swap.SetReturn(explorer.Tile, explorer.Facing);
                    swap.EnterNow(interior, new Vector2Int(data.insideX, data.insideY), new Vector2Int(data.insideFacingX, data.insideFacingY));
                }
            }

            foreach (var p in problems) Debug.LogWarning($"[GameSave] {p}");
            Debug.Log($"[GameSave] Loaded {SavePath} (saved {data.savedAt}): {Describe(party.members)}; storage {party.storage.Count}; " +
                      $"explorer c{data.explorerX} r{data.explorerY}{(data.inside ? $" (inside {data.interior} at c{data.insideX} r{data.insideY})" : "")}; last shrine {(data.hasShrine ? $"c{data.shrineX} r{data.shrineY}" : "none")}; " +
                      $"realm {RealmState.Restored:0.##}; {data.said.Count} lines said; {_caught.Count} species caught; {explorer.moveMode}.");
        }

        /// <summary>Everything in the pods and storage counts as caught (the starter too).</summary>
        void NoteParty()
        {
            foreach (var c in party.members.Concat(party.storage))
                if (c != null && c.species != null) _caught.Add(SpeciesRegistry.IdOf(c.species));
        }

        static string Describe(List<CreatureInstance> list) =>
            string.Join(", ", list.Where(c => c != null).Select(c => $"{c.Name} Lv {c.level}"));

        // ---- save ----

        /// <summary>Writes the save now. <paramref name="puff"/> shows "Saved" by the Spirit.</summary>
        public void Save(string reason, bool puff = false)
        {
            if (!_ready || !enabled) return;
            try
            {
                bool inside = swap != null && swap.Inside;
                var outdoor = inside ? swap.ReturnTile : explorer.Tile;
                var outdoorFacing = inside ? swap.ReturnFacing : explorer.Facing;
                var data = new SaveData
                {
                    party = SaveGame.ToSaved(party.members),
                    storage = SaveGame.ToSaved(party.storage),
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
                    said = Dialogue.Said.ToList(),
                    caught = _caught.ToList(),
                    walkMode = explorer.moveMode.ToString(),
                };
                if (data.hasShrine)
                {
                    data.shrineX = shrines.LastShrine.Value.x;
                    data.shrineY = shrines.LastShrine.Value.y;
                }
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
            hud.PopText(hud.transform.InverseTransformPoint(world), "Saved", new Color(1f, 0.92f, 0.7f), 0.7f, 1.2f);
        }
    }
}
