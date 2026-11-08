using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>Step 22 bridge checks (Nov 8 notes): the menu's spacing in the room.</summary>
    public static class Step22Debug
    {
        static string Arg => System.IO.File.Exists("Temp/ClaudeBridge/arg.txt") ? System.IO.File.ReadAllText("Temp/ClaudeBridge/arg.txt").Trim() : "";

        /// <summary>
        /// Shoots the game menu's board from the seat (Logs/BattleShots/22_&lt;arg&gt;.png and a close one) and logs each
        /// visible tablet pair's gap in the room (cm), from the tablets' rects and the board's scale.
        /// </summary>
        public static void MenuShot()
        {
            string name = string.IsNullOrEmpty(Arg) ? "menu" : Arg;
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            var menu = Object.FindFirstObjectByType<GameMenu>();
            if (island == null || menu == null || menu.runes == null) return;
            var t = island.transform;
            var eye = t.position + t.rotation * new Vector3(0f, 0.5f, 0.42f);
            var board = menu.runes.transform;
            if (Camera.main != null) Camera.main.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(board.position - eye, Vector3.up));
            BattleDebug.Render($"Logs/BattleShots/22_{name}.png", eye, Quaternion.LookRotation(t.position - eye, Vector3.up));
            var close = Vector3.Lerp(eye, board.position, 0.5f);
            BattleDebug.Render($"Logs/BattleShots/22_{name}_close.png", close, Quaternion.LookRotation(board.position - close, Vector3.up));
            float s = board.lossyScale.x;
            var p = menu.runes.LayoutProblems();
            Debug.Log($"[Step22Debug] {name}: board scale {s:0.00}, rule {menu.runes.MinGapLocal * 100f:0.0} cm local = {TabletLayout.MinRoomGap * 100f:0.0} cm in the room; " +
                      $"{(p.Count == 0 ? "ok" : string.Join("; ", p))}.\n{RuneBoard.DebugList()}");
        }
    }
}
