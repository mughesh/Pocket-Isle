using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 9b checks through the Claude bridge (exec:Diorama.EditorTools.Step9bDebug.X), in Play mode: put the
    /// explorer at the log or the forge, solve pieces as if by hand, take seated shots (Logs/BattleShots/9b_*.png).
    /// </summary>
    static class Step9bDebug
    {
        static T Find<T>() where T : Object => Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);

        static WorldMap Cinderwood => AssetDatabase.LoadAssetAtPath<WorldMap>(CinderwoodTools.MapPath);

        /// <summary>Explorer to (x, y) on Cinderwood facing north (travels there at once if needed).</summary>
        static void Put(int x, int y)
        {
            var swap = Find<SceneSwap>();
            var map = Cinderwood;
            if (!EditorApplication.isPlaying || swap == null || map == null) return;
            if (Opening.InProgress) Debug.LogWarning("[Step9bDebug] The opening is still running.");
            swap.TravelNow(map, CinderwoodTools.Tile(x, y), new Vector2Int(0, -1));
        }

        public static void AtLog() => Put(20, 43);
        public static void PastLog() => Put(20, 49);
        public static void AtForge() => Put(17, 53);

        static CarryPiece Log => Find<CarryPiece>();
        static PumpPiece Bellows => Find<PumpPiece>();

        public static void PlaceLog0() { var p = Log; if (p != null) p.DebugPlace(0); }
        public static void PlaceLog1() { var p = Log; if (p != null) p.DebugPlace(1); }
        public static void DropLogHome() { var p = Log; if (p != null) p.DebugPlace(-1); }

        public static void Pump() { var p = Bellows; if (p != null) p.DebugStroke(); }

        /// <summary>Forgets every puzzle (this Play session; the next save writes it).</summary>
        public static void ResetPuzzles() => PuzzleState.Restore(null);

        public static void Describe() => Debug.Log($"[Step9bDebug] Puzzles: {PuzzleState.Describe()}; explorer " +
                                                   $"{(Find<GestureTestPiece>() is GestureTestPiece e ? $"c{e.Tile.x} r{e.Tile.y}" : "-")}.");

        /// <summary>Close view of a map tile (x, y) from the player's side: Logs/BattleShots/9b_{name}.png.</summary>
        static void Close(string name, float x, float y, float height = 0.5f)
        {
            var island = Find<IslandBuilder>();
            if (island == null) return;
            var t = island.transform;
            var tile = CinderwoodTools.Tile(Mathf.RoundToInt(x), Mathf.RoundToInt(y)) + new Vector2(x - Mathf.Round(x), 0f);
            var at = t.TransformPoint(island.TileToLocal(tile, island.Content.capHeight + height * island.map.tileSize));
            var eye = at + t.rotation * new Vector3(0f, 0.16f, 0.2f) * t.lossyScale.x;
            BattleDebug.Render($"Logs/BattleShots/9b_{name}.png", eye, Quaternion.LookRotation(at - eye, Vector3.up));
        }

        public static void LogShot() => Close("log", 20.5f, 45f, 0.3f);
        public static void ForgeShot() => Close("forge", 18f, 54.5f, 1.2f);

        /// <summary>Seated view of the island: Logs/BattleShots/9b_seat.png, and a closer one.</summary>
        public static void Shot()
        {
            var island = Find<IslandBuilder>();
            if (island == null) return;
            var t = island.transform;
            var eye = t.position + t.rotation * new Vector3(0f, 0.5f, 0.42f);
            BattleDebug.Render("Logs/BattleShots/9b_seat.png", eye, Quaternion.LookRotation(t.position + t.rotation * new Vector3(0f, 0f, 0.02f) - eye, Vector3.up));
            var near = t.position + t.rotation * new Vector3(0f, 0.28f, 0.26f);
            BattleDebug.Render("Logs/BattleShots/9b_near.png", near, Quaternion.LookRotation(t.position + t.rotation * new Vector3(0f, 0.02f, -0.04f) - near, Vector3.up));
        }
    }
}
