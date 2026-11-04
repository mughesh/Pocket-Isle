using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>Bridge hooks for step 14k (Play mode). Arguments come from Temp/ClaudeBridge/arg.txt.</summary>
    public static class Step14kDebug
    {
        const string ArgFile = "Temp/ClaudeBridge/arg.txt";
        static string Arg => File.Exists(ArgFile) ? File.ReadAllText(ArgFile).Trim() : "";

        // ---- A: the grab band ----

        /// <summary>The grab band at the near-right corner: what grabs at 1 cm outside, on the line, 1 / 3 / 5 cm inside, high and low.</summary>
        public static void Zone()
        {
            var h = Object.FindFirstObjectByType<IslandHandle>();
            if (h == null || h.island == null) return;
            var s = h.ZoneShape;
            var t = h.island.transform;
            var sb = new StringBuilder($"[Step14kDebug] Grab band (scale {s.scale:0.00}, {h.grabInward * 100f:0.0} cm in, {h.grabPastLine * 1000f:0} mm slack, " +
                                       $"top {s.bandTop:0.000} local = {(s.bandTop - s.capTop) * s.scale * 100f:0.0} cm over the cap, {(s.lipActive ? "explore" : "pods active")}):");
            void Probe(string name, Vector3 local) =>
                sb.Append($"\n  {name}: {h.ZoneAt(t.TransformPoint(local))} (to the zone {h.DistanceToHandles(t.TransformPoint(local)) * 100f:0.0} cm)");
            float line = s.half.x + s.lineOutset, z = s.half.y - 0.09f, y = s.capTop + 0.01f / s.scale;
            foreach (float cm in new[] { 3f, 1f, 0f, -1f, -3f, -5f })
                Probe(cm > 0 ? $"{cm} cm outside the line, 1 cm over the cap" : cm == 0 ? "on the line" : $"{-cm} cm inside, 1 cm over the cap",
                    new Vector3(line + cm * 0.01f / s.scale, y, z));
            Probe("3 cm inside on the rock face (3 cm under the cap)", new Vector3(line - 0.03f / s.scale, s.capTop - 0.03f / s.scale, z));
            Probe("1 cm inside, 4 cm over the cap", new Vector3(line - 0.01f / s.scale, s.capTop + 0.04f / s.scale, z));
            Probe("lip 2 cm in", new Vector3(0f, -0.05f, s.half.y + s.lineOutset - 0.02f / s.scale));
            Probe("lip 1 cm out", new Vector3(0f, -0.05f, s.half.y + s.lineOutset + 0.01f / s.scale));
            var rim = h.rimColliders;
            if (rim != null)
                foreach (var c in rim)
                    if (c is BoxCollider b) sb.Append($"\n  shell box {b.center.ToString("F3")} size {b.size.ToString("F3")}");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// The band lit: a fake palm 2 cm inside the near-right corner (arg "off" clears it), the camera low over that corner,
        /// shot 14k_band.png after the glow has eased in.
        /// </summary>
        public static void BandShot()
        {
            if (Arg == "off")
            {
                IslandGrabFrame.DebugPalm = null;
                return;
            }
            var h = Object.FindFirstObjectByType<IslandHandle>();
            if (h == null || h.island == null) return;
            var s = h.ZoneShape;
            var t = h.island.transform;
            Vector3 palm = t.TransformPoint(new Vector3(s.half.x + s.lineOutset - 0.02f / s.scale, s.capTop + 0.01f / s.scale, s.half.y - 0.09f));
            IslandGrabFrame.DebugPalm = () => palm;
            Vector3 corner = t.TransformPoint(new Vector3(s.half.x, s.capTop, s.half.y));
            Vector3 eye = corner + t.rotation * new Vector3(-0.08f, 0.14f, 0.16f);
            var steps = new List<(double, System.Action)>
            {
                (EditorApplication.timeSinceStartup + 0.6, () =>
                {
                    BattleDebug.Render("Logs/BattleShots/14k_band.png", eye, Quaternion.LookRotation(corner - eye, Vector3.up));
                    Debug.Log($"[Step14kDebug] Band shot 14k_band.png (fake palm in zone: {h.ZoneAt(palm)}).");
                }),
            };
            Schedule(steps);
        }

        /// <summary>Logs and clears the Editor's pause (Error Pause can freeze a flat Play on an exception).</summary>
        public static void Unpause()
        {
            Debug.Log($"[Step14kDebug] Editor paused {EditorApplication.isPaused}, time scale {Time.timeScale}, frame {Time.frameCount}: unpausing.");
            EditorApplication.isPaused = false;
        }

        // ---- B: the Journal ----

        /// <summary>Hovers a Journal cell as a fingertip would (arg = cell, -1 = off); logs its labels.</summary>
        public static void JournalHover()
        {
            JournalBoard.DebugHover = int.TryParse(Arg, out int c) ? c : -1;
            var j = JournalBoard.Instance;
            var sb = new StringBuilder($"[Step14kDebug] Journal hover {JournalBoard.DebugHover}; labels:");
            if (j != null && j.runes != null && j.runes.Root != null)
                foreach (var t in j.runes.Root.GetComponentsInChildren<TMPro.TextMeshPro>())
                    if (t.name.StartsWith("Cell") && !string.IsNullOrEmpty(t.text))
                        sb.Append($"\n  {t.name}: {t.text.Replace("\n", " / ")} (lines {t.textInfo.lineCount}, size {t.fontSize:0.000})");
            Debug.Log(sb.ToString());
        }

        // ---- C: lines in context ----

        static void Go(string label)
        {
            var s = Object.FindFirstObjectByType<GameState>();
            if (s == null) return;
            foreach (var spot in s.Spots)
                if (spot.label == label)
                {
                    s.GoTo(spot);
                    return;
                }
            Debug.LogWarning($"[Step14kDebug] No spot '{label}'.");
        }

        static void Idle()
        {
            var d = Object.FindFirstObjectByType<Dialogue>();
            if (d != null) d.DebugIdle();
        }

        /// <summary>
        /// The flat pass: hub -> lodge -> Cinderwood -> the Forge Hall -> a wild battle, the intros forgotten first; an idle
        /// hint asked for in each place, and a few lines fired in the wrong place. Every line shown logs "[Dialogue] Show ...".
        /// About 100 s. Back up the Editor save first (Editor Play writes it).
        /// </summary>
        public static void Pass()
        {
            Dialogue.DebugForget("lodge.first", "lodge.first.empty", "lodge.inspect", "lodge.take", "lodge.leave", "lodge.exit", "lodge.toss",
                "lodge.inspect.one", "cw.arrive", "realm.cinderwood.first", "cinder.hall.first", "cinder.keeper.first", "hub.return.first",
                "tut.grass", "pod.first", "battle.first", "menu.first", "cinder.village.first");
            double t0 = EditorApplication.timeSinceStartup;
            var steps = new List<(double, System.Action)>();
            void At(double s, string what, System.Action act) => steps.Add((t0 + s, () =>
            {
                Debug.Log($"[Step14kDebug] Pass {s:0} s: {what} (place {Places.Now}).");
                act();
            }));
            At(0, "hub: to the shrine", () => Go("Hub: shrine"));
            At(10, "hub: idle hint", Idle);
            At(14, "hub: lodge and Cinderwood lines fired here", () =>
            {
                Dialogue.Say("lodge.first");
                Dialogue.Say("cw.arrive");
            });
            At(18, "enter the lodge", BattleDebug.EnterLodge);
            At(24, "lodge: grass and hub lines fired here", () =>
            {
                Dialogue.Say("tut.grass");
                Dialogue.Say("hub.return.first");
                Dialogue.Say("gate.fire.first");
            });
            At(32, "lodge: idle hint", Idle);
            At(38, "leave the lodge", BattleDebug.LeaveLodge);
            At(44, "travel to Cinderwood", () => Go("CW: arrival"));
            At(56, "Cinderwood: idle hint", Idle);
            At(58, "Cinderwood: lodge and hub lines fired here", () =>
            {
                Dialogue.Say("lodge.last");
                Dialogue.Say("hub.return.first");
            });
            At(62, "into the Forge Hall", () => Go("Forge Hall"));
            At(74, "hall: idle hint, a Cinderwood map line", () =>
            {
                Idle();
                Dialogue.Say("cinder.village.first");
            });
            At(80, "out to the village", () => Go("CW: village"));
            At(90, "wild battle", BattleDebug.Grass);
            At(100, "battle: idle hint, a map line", () =>
            {
                Idle();
                Dialogue.Say("menu.first");
                Dialogue.Say("lodge.last");
                Dialogue.Say("gate.sealed");
            });
            At(108, "run", BattleDebug.Run);
            At(116, "done", () => { });
            Schedule(steps);
        }

        internal static void Schedule(List<(double at, System.Action act)> steps)
        {
            EditorApplication.CallbackFunction tick = null;
            tick = () =>
            {
                if (!EditorApplication.isPlaying)
                {
                    EditorApplication.update -= tick;
                    return;
                }
                while (steps.Count > 0 && EditorApplication.timeSinceStartup >= steps[0].at)
                {
                    var act = steps[0].act;
                    steps.RemoveAt(0);
                    act();
                }
                if (steps.Count == 0) EditorApplication.update -= tick;
            };
            EditorApplication.update += tick;
        }
    }
}
