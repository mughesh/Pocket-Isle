using System.IO;
using Diorama.Audio;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>Step 21 bridge (Play mode): the Nov 8 notes' checks (menu in battle, pause mute, the battle row).</summary>
    public static class Step21Debug
    {
        const string ArgFile = "Temp/ClaudeBridge/arg.txt";
        static string Arg => File.Exists(ArgFile) ? File.ReadAllText(ArgFile).Trim() : "";

        /// <summary>What the menu, the pause and the battle row are doing now.</summary>
        public static void Log()
        {
            var runes = Object.FindFirstObjectByType<BattleRunes>(FindObjectsInactive.Include);
            var row = runes != null ? runes.transform.Find("Rune Row") : null;
            Debug.Log($"[Step21Debug] Menu open {GameMenu.IsOpen}, paused {GamePause.IsPaused}, listener paused {AudioListener.pause}, " +
                      $"battle row {(row == null ? "-" : row.gameObject.activeInHierarchy ? "shown" : "hidden")}, music {AudioService.CurrentMusic ?? "-"} " +
                      $"(state {MusicDirector.State}), shard only {Opening.ShardOnly}.");
        }

        /// <summary>Opens the game menu (arg "close" closes it), then logs.</summary>
        public static void Menu()
        {
            var m = Object.FindFirstObjectByType<GameMenu>();
            if (m == null) return;
            if (Arg == "close") m.Close("bridge");
            else m.TryOpen();
            Log();
        }
    }
}
