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

        static bool _armed;

        /// <summary>The whole ending (Step19Debug.Ending) plus a seat shot of the Hall of Fame as the Spark comes out
        /// (Logs/BattleShots/21_hall.png), every Isling's spot then on the cap.</summary>
        public static void Ending()
        {
            if (!_armed)
            {
                _armed = true;
                FinaleDirector.Moment += OnMoment;
                UnityEditor.EditorApplication.playModeStateChanged += s =>
                {
                    if (s != UnityEditor.PlayModeStateChange.ExitingPlayMode) return;
                    FinaleDirector.Moment -= OnMoment;
                    _armed = false;
                };
            }
            Step19Debug.Ending();
        }

        static void OnMoment(string moment)
        {
            if (moment != "spark.out") return;
            var f = FinaleDirector.Instance;
            if (f == null) return;
            Vector3 eye = f.EyeWorld;
            Vector3 at = f.island.transform.position;
            BattleDebug.Render("Logs/BattleShots/21_hall.png", eye, Quaternion.LookRotation(at - eye, Vector3.up));
            Debug.Log("[Step21Debug] Hall of Fame shot: Logs/BattleShots/21_hall.png.");
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
