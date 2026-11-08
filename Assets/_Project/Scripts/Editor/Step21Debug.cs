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

        /// <summary>
        /// The walk lesson's swipe ghost (thumb_swipe_all over the island), frozen at a few points, shot from the seat
        /// (Logs/BattleShots/21_swipe_*.png); logs the measured fingers / back of the hand and where they point in the world
        /// (the back of the hand should face up and out, the fingers away from the head).
        /// </summary>
        public static void SwipeGhost()
        {
            var g = Object.FindFirstObjectByType<GhostHands>();
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            if (g == null || island == null) return;
            g.Show(GhostHands.SwipeAllClip, GhostPlace.AboveIsland, null, 0.6f, false, false);
            var t = island.transform;
            var eye = t.position + t.rotation * new Vector3(0f, 0.5f, 0.42f);
            var at = t.position + t.rotation * new Vector3(0f, 0.12f, 0f);
            int n = 0;
            foreach (float u in new[] { 0.05f, 0.2f, 0.55f, 0.8f })
            {
                g.debugFreeze = u;
                g.DebugPose();
                var m = g.LastMeasured;
                Debug.Log($"[Step21Debug] Swipe ghost at {u:0.00}: measured fingers {m.fingers:F2}, back {m.back:F2} (clip frame); {g.DebugInfo()}");
                BattleDebug.Render($"Logs/BattleShots/21_swipe_{n}.png", eye, Quaternion.LookRotation(at - eye, Vector3.up));
                var close = Vector3.Lerp(eye, at, 0.65f);
                BattleDebug.Render($"Logs/BattleShots/21_swipe_{n}_close.png", close, Quaternion.LookRotation(at - close, Vector3.up));
                var side = at + t.rotation * new Vector3(0.35f, 0.05f, 0f);
                BattleDebug.Render($"Logs/BattleShots/21_swipe_{n}_side.png", side, Quaternion.LookRotation(at - side, Vector3.up));
                n++;
            }
            g.debugFreeze = -1f;
            g.Hide();
        }

        /// <summary>
        /// The statue's voice: says a Heart line (arg = line id, default heart.thanks) and counts the voice.heart blips
        /// sounding over the next 5 s (any AudioSource playing a voice.heart clip, counted per start).
        /// </summary>
        public static void HeartVoice()
        {
            string id = string.IsNullOrEmpty(Arg) ? "heart.thanks" : Arg;
            Dialogue.Say(id);
            double end = UnityEditor.EditorApplication.timeSinceStartup + 5.0;
            var seen = new System.Collections.Generic.HashSet<int>();
            int blips = 0;
            UnityEditor.EditorApplication.CallbackFunction tick = null;
            tick = () =>
            {
                if (!Application.isPlaying || UnityEditor.EditorApplication.timeSinceStartup > end)
                {
                    UnityEditor.EditorApplication.update -= tick;
                    Debug.Log($"[Step21Debug] Heart line {id}: {blips} voice.heart blip(s) in 5 s.");
                    return;
                }
                foreach (var s in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
                {
                    bool on = s.isPlaying && s.clip != null && s.clip.name.StartsWith("voice.heart");
                    int key = s.GetInstanceID();
                    if (on && seen.Add(key)) blips++;
                    else if (!on) seen.Remove(key);
                }
            };
            UnityEditor.EditorApplication.update += tick;
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
