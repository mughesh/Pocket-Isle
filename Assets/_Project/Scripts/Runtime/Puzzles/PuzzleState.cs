using System;
using System.Collections.Generic;
using System.Linq;

namespace Diorama
{
    /// <summary>
    /// Every hand puzzle's state by id (step 9b): solved, and a stage (how far it got: lit braziers, the spot a log lies
    /// on). <see cref="GameSave"/> restores it from the save before the explorer is placed (a lifted log changes where
    /// it can stand) and saves on every <see cref="Changed"/>. Puzzles read it on Start and again on <see cref="Restored"/>.
    /// </summary>
    public static class PuzzleState
    {
        struct Entry
        {
            public bool solved;
            public int stage;
        }

        static readonly Dictionary<string, Entry> _all = new Dictionary<string, Entry>();

        /// <summary>A puzzle was solved or reached a new stage (the save listens).</summary>
        public static event Action<string> Changed;
        /// <summary>A save was loaded (or a new game started): every puzzle re-reads its state.</summary>
        public static event Action Restored;

        public static bool IsSolved(string id) => id != null && _all.TryGetValue(id, out var e) && e.solved;

        public static int Stage(string id) => id != null && _all.TryGetValue(id, out var e) ? e.stage : 0;

        /// <summary>A checkpoint (a lit brazier). Nothing happens if it's already there.</summary>
        public static void SetStage(string id, int stage)
        {
            if (string.IsNullOrEmpty(id)) return;
            _all.TryGetValue(id, out var e);
            if (e.stage == stage) return;
            e.stage = stage;
            _all[id] = e;
            Changed?.Invoke(id);
        }

        public static void Solve(string id, int stage)
        {
            if (string.IsNullOrEmpty(id)) return;
            _all.TryGetValue(id, out var e);
            if (e.solved && e.stage == stage) return;
            e.solved = true;
            e.stage = stage;
            _all[id] = e;
            Changed?.Invoke(id);
        }

        /// <summary>From the save (null = a new game: nothing solved).</summary>
        public static void Restore(IEnumerable<SavedPuzzle> saved)
        {
            _all.Clear();
            if (saved != null)
                foreach (var p in saved)
                    if (p != null && !string.IsNullOrEmpty(p.id))
                        _all[p.id] = new Entry { solved = p.solved, stage = p.stage };
            Restored?.Invoke();
        }

        public static List<SavedPuzzle> ToSaved() =>
            _all.Select(kv => new SavedPuzzle { id = kv.Key, solved = kv.Value.solved, stage = kv.Value.stage }).ToList();

        public static string Describe() =>
            _all.Count == 0 ? "none" : string.Join(", ", _all.Select(kv => $"{kv.Key} {(kv.Value.solved ? "solved" : $"stage {kv.Value.stage}")}"));

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _all.Clear();
            Changed = null;
            Restored = null;
        }
    }
}
