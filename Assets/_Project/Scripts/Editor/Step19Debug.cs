using System.Collections;
using System.IO;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 19 bridge (Play mode): shots of the credits' sky lanterns from the player's seat. <see cref="ArmShots"/> hooks
    /// the finale's lantern moments: each lantern at reading height (a close shot from the head) and a wide shot of
    /// everything in the air; the big one also a portrait close-up. Logs/BattleShots/19_*.png.
    /// </summary>
    public static class Step19Debug
    {
        const string ArgFile = "Temp/ClaudeBridge/arg.txt";
        static string Arg => File.Exists(ArgFile) ? File.ReadAllText(ArgFile).Trim() : "";
        static int _n;
        static bool _armed;

        public static void ArmShots()
        {
            if (_armed) return;
            _armed = true;
            _n = 0;
            FinaleDirector.Moment += OnMoment;
            UnityEditor.EditorApplication.playModeStateChanged += s =>
            {
                if (s != UnityEditor.PlayModeStateChange.ExitingPlayMode) return;
                FinaleDirector.Moment -= OnMoment;
                _armed = false;
            };
            Debug.Log("[Step19Debug] Lantern shots armed.");
        }

        static void OnMoment(string moment)
        {
            if (!moment.StartsWith("lantern:")) return;
            var f = FinaleDirector.Instance;
            if (f == null) return;
            f.StartCoroutine(Shots(moment.Substring(8), _n++));
        }

        static IEnumerator Shots(string title, int index)
        {
            // Let it turn to the head first.
            yield return new WaitForSecondsRealtime(0.7f);
            var f = FinaleDirector.Instance;
            if (f == null) yield break;
            Transform lantern = null;
            foreach (Transform t in f.island.transform)
                if (t.name == $"Sky lantern ({title})") lantern = t;
            if (lantern == null) yield break;
            Vector3 eye = f.EyeWorld;
            string slug = index + "_" + title.Split(' ')[0].Trim(':', ',').ToLowerInvariant();
            BattleDebug.Render($"Logs/BattleShots/19_read_{slug}.png", eye, Quaternion.LookRotation(lantern.position - eye, Vector3.up));
            // Wide: a step back from the head, looking at the middle of everything over the Heart.
            Vector3 over = f.heart.SocketWorld + Vector3.up * (0.2f * f.island.transform.lossyScale.x);
            Vector3 back = eye + (eye - over).normalized * 0.35f + Vector3.up * 0.05f;
            BattleDebug.Render($"Logs/BattleShots/19_wide_{slug}.png", back, Quaternion.LookRotation(over - back, Vector3.up));
            if (title == "Pocket Isle")
            {
                Vector3 near = Vector3.Lerp(eye, lantern.position, 0.55f);
                BattleDebug.Render("Logs/BattleShots/19_portrait.png", near, Quaternion.LookRotation(lantern.position + Vector3.up * 0.03f - near, Vector3.up));
                // The same without the halo (is the halo what lifts the picture's darks?).
                var halo = lantern.Find("Halo");
                if (halo != null)
                {
                    halo.gameObject.SetActive(false);
                    BattleDebug.Render("Logs/BattleShots/19_portrait_nohalo.png", near, Quaternion.LookRotation(lantern.position + Vector3.up * 0.03f - near, Vector3.up));
                    halo.gameObject.SetActive(true);
                }
            }
            Debug.Log($"[Step19Debug] Shots 19_*_{slug}.png (lantern at {lantern.position:F2}, head {eye:F2}).");
        }

        /// <summary>
        /// The whole ending without saving (as See the ending), the goodbye passed by look + pinch, the lantern shots armed.
        /// </summary>
        public static void Ending()
        {
            ArmShots();
            var f = FinaleDirector.Instance;
            if (f != null) f.StartCoroutine(Pump(f));
        }

        static IEnumerator Pump(FinaleDirector f)
        {
            // Past the title first (Continue), as the player would.
            for (float t = 0f; t < 30f && TitleScreen.Showing; t += 1f)
            {
                RuneBoard.DebugPress("Continue");
                yield return new WaitForSeconds(1f);
            }
            yield return new WaitForSeconds(3f);
            Debug.Log($"[Step19Debug] Ending: {FinaleDirector.DevPlay(false, false)} (title {TitleScreen.Showing}).");
            // It may travel to the hub first.
            for (float t = 0f; t < 60f && !FinaleDirector.Running; t += 0.5f) yield return new WaitForSeconds(0.5f);
            Debug.Log($"[Step19Debug] The finale is running: {FinaleDirector.Running}.");
            while (FinaleDirector.Running)
            {
                f.DebugGazePinch();
                yield return new WaitForSeconds(1.5f);
            }
            Debug.Log("[Step19Debug] The ending is over.");
        }

        /// <summary>Presses the finale's Skip (the private flag, as the Skip rune does).</summary>
        public static void Skip()
        {
            var f = FinaleDirector.Instance;
            if (f == null) return;
            typeof(FinaleDirector).GetField("_skip", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(f, true);
            Debug.Log("[Step19Debug] Skip.");
        }
    }
}
