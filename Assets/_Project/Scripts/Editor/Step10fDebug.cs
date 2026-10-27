using System.IO;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 10f checks through the Claude bridge (exec:Diorama.EditorTools.Step10fDebug.X), in Play. Start with
    /// GalewickPlay.Start; shots with GalewickPlay.Shot / CloseShot (name in arg.txt). Arguments in Temp/ClaudeBridge/arg.txt.
    /// </summary>
    static class Step10fDebug
    {
        const string ArgFile = "Temp/ClaudeBridge/arg.txt";

        static string Arg => File.Exists(ArgFile) ? File.ReadAllText(ArgFile).Trim() : "";

        static T Find<T>() where T : Object => Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);

        static float ArgFloat(float fallback) =>
            float.TryParse(Arg, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float v) ? v : fallback;

        /// <summary>Teleport to the Cinderwood tile "x,y" (map x, y as in CinderwoodTools) in arg.txt, travelling there if needed.</summary>
        public static void Teleport()
        {
            var parts = Arg.Split(',');
            if (parts.Length != 2 || !int.TryParse(parts[0], out int x) || !int.TryParse(parts[1], out int y)) return;
            var map = AssetDatabase.LoadAssetAtPath<WorldMap>(CinderwoodTools.MapPath);
            var swap = Find<SceneSwap>();
            var e = Find<GestureTestPiece>();
            if (map == null || swap == null || e == null) return;
            var tile = CinderwoodTools.Tile(x, y);
            if (SceneSwap.IsInside) swap.ReturnNow();
            if (e.island.world != map) swap.TravelNow(map, tile, new Vector2Int(0, -1));
            else e.Teleport(tile, new Vector2Int(0, -1));
            Debug.Log($"[Step10fDebug] At x{x} y{y} (c{tile.x} r{tile.y}), walkable {map.CanWalk(tile)}.");
        }

        /// <summary>Drops the puzzle id in arg.txt from the save state (back to unsolved), every puzzle re-reads.</summary>
        public static void Reset()
        {
            var saved = PuzzleState.ToSaved();
            int n = saved.RemoveAll(p => p.id == Arg);
            PuzzleState.Restore(saved);
            Debug.Log($"[Step10fDebug] Reset {Arg} ({n} entry).");
        }

        // ---- A: the village flame ----

        /// <summary>Cups the flame with both hands for arg.txt seconds (default 2.5).</summary>
        public static void Cup()
        {
            var cup = Find<FlameCupPiece>();
            if (cup == null) return;
            cup.DebugCup(ArgFloat(2.5f));
        }

        public static void State()
        {
            var cup = Find<FlameCupPiece>();
            var shrines = Find<HearthShrines>();
            Debug.Log($"[Step10fDebug] State: {(GameState.Instance != null ? GameState.Instance.Where : "?")}, restored {RealmState.Restored:0.##}; " +
                      $"flame: {(cup != null ? $"{cup.Describe()}, usable {cup.Usable}, lesson {cup.LessonShowing}" : "none")}; " +
                      $"shrine dormant {(cup != null && HearthShrines.IsDormant(cup.map.name, cup.shrine))}, last shrine {(shrines != null && shrines.LastShrine.HasValue ? shrines.LastShrine.Value.ToString() : "none")}; " +
                      $"puzzles {PuzzleState.Describe()}");
        }
    }
}
