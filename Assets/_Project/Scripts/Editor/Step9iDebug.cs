using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>Step 9i bridge helpers (exec:Diorama.EditorTools.Step9iDebug.X), in Play. Shots go to Logs/BattleShots/9i_*.png.</summary>
    public static class Step9iDebug
    {
        /// <summary>Into the lodge the way a shrine does it (the explorer comes in at the back door and steps in).</summary>
        public static void Enter()
        {
            var title = Object.FindFirstObjectByType<TitleScreen>();
            if (title != null && TitleScreen.Showing) title.Cancel();
            BattleDebug.SeatCamera();
            BattleDebug.EnterLodge();
        }

        /// <summary>The lodge from the seated default view: Logs/BattleShots/9i_lodge.png.</summary>
        public static void Shot()
        {
            var cam = Camera.main;
            if (cam == null) return;
            BattleDebug.Render("Logs/BattleShots/9i_lodge.png", cam.transform.position, cam.transform.rotation);
            var e = Object.FindFirstObjectByType<GestureTestPiece>();
            Debug.Log($"[Step9iDebug] Lodge shot; explorer c{(e != null ? e.Tile.x : -1)} r{(e != null ? e.Tile.y : -1)} facing {(e != null ? e.Facing : Vector2Int.zero)}.");
        }

        /// <summary>Swipe toward the back door: the explorer walks out.</summary>
        public static void WalkOut()
        {
            var e = Object.FindFirstObjectByType<GestureTestPiece>();
            var room = Object.FindFirstObjectByType<LodgeRoom>();
            if (e == null || room == null) return;
            Debug.Log($"[Step9iDebug] GoTo door c{room.doorTiles[0].x} r{room.doorTiles[0].y}: {e.GoTo(room.doorTiles[0])}");
        }
    }
}
