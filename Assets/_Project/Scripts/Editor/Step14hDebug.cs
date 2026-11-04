using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>Bridge hooks for step 14h (Play mode): the evolution moment, shots in sequence.</summary>
    public static class Step14hDebug
    {
        const string ArgFile = "Temp/ClaudeBridge/arg.txt";
        static string Arg => File.Exists(ArgFile) ? File.ReadAllText(ArgFile).Trim() : "";

        static GrowthDirector Growth => GrowthDirector.Instance;

        /// <summary>The lead to its evolve level (Dev panel "Make lead ready to evolve").</summary>
        public static void Ready()
        {
            var g = GameState.Instance;
            Debug.Log("[Step14hDebug] " + (g != null ? g.LeadReadyToEvolve() : "no GameState"));
        }

        /// <summary>Adds an Isling to the party (arg "SpeciesId,level"), as the Dev panel's "Add to party".</summary>
        public static void Add()
        {
            var a = Arg.Split(',');
            var g = GameState.Instance;
            var s = g != null && g.registry != null ? g.registry.Find(a[0]) : null;
            if (s == null)
            {
                Debug.LogWarning($"[Step14hDebug] No species '{a[0]}'.");
                return;
            }
            g.AddSpecies(s, a.Length > 1 && int.TryParse(a[1], out int lv) ? lv : 5);
        }

        /// <summary>
        /// Section C shots: arg "eyeUp,eyeBack,wild,Species1,Species2,...": each species in pod 1 (Lv 30) on the near pad
        /// against the small wild one, the camera held at a seated head (metres above / behind the island centre);
        /// shot 14h_pad_&lt;species&gt;.png, then it runs. One every 14 s.
        /// </summary>
        public static void PadShots()
        {
            var a = Arg.Split(',');
            if (a.Length < 4) return;
            float up = float.Parse(a[0], System.Globalization.CultureInfo.InvariantCulture);
            float back = float.Parse(a[1], System.Globalization.CultureInfo.InvariantCulture);
            var g = GameState.Instance;
            var wild = g.registry.Find(a[2]);
            var steps = new List<(double at, System.Action act)>();
            double t0 = EditorApplication.timeSinceStartup;
            for (int i = 3; i < a.Length; i++)
            {
                string id = a[i];
                double t = t0 + (i - 3) * 14.0;
                steps.Add((t, () =>
                {
                    var s = g.registry.Find(id);
                    if (s == null || wild == null) return;
                    g.party.members[0] = new IslingInstance(s, 30);
                    g.party.NotifyChanged();
                    bool ok = g.encounters.DebugEncounter(new IslingInstance(wild, 5));
                    Debug.Log($"[Step14hDebug] Pad shot {id} vs {wild.displayName}: {(ok ? "battle" : "refused")}.");
                }));
                steps.Add((t + 4.0, () => BattleDebug.SwitchNext()));
                steps.Add((t + 8.5, () =>
                {
                    var cam = Camera.main;
                    if (cam != null) BattleDebug.Render($"Logs/BattleShots/14h_pad_{id.ToLowerInvariant()}.png", cam.transform.position, cam.transform.rotation);
                    Debug.Log($"[Step14hDebug] Shot 14h_pad_{id.ToLowerInvariant()}.png.");
                }));
                steps.Add((t + 9.5, () => BattleDebug.Run()));
            }
            void Hold()
            {
                var cam = Camera.main;
                var island = g != null && g.explorer != null ? g.explorer.island : null;
                if (cam == null || island == null) return;
                var tr = island.transform;
                Vector3 front = Vector3.ProjectOnPlane(tr.forward, Vector3.up).normalized;
                cam.transform.position = tr.position + front * back + Vector3.up * up;
                cam.transform.LookAt(tr.position);
            }
            EditorApplication.CallbackFunction tick = null;
            tick = () =>
            {
                if (!EditorApplication.isPlaying)
                {
                    EditorApplication.update -= tick;
                    return;
                }
                Hold();
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

        /// <summary>
        /// The Journal: arg "open" (menu, then Journal), "fire|water|wind", "pick N" (grid cell), "close", "lodge" (the
        /// pedestal book), "shot NAME" (from the seated head at the board).
        /// </summary>
        public static void Journal()
        {
            var a = Arg.Split(' ');
            var j = JournalBoard.Instance;
            switch (a[0])
            {
                case "open":
                    BattleDebug.SeatCamera();
                    var menu = Object.FindFirstObjectByType<GameMenu>();
                    if (menu != null) menu.DebugOpenJournal();
                    break;
                case "fire": if (j != null) j.DebugElement(Element.Fire); break;
                case "water": if (j != null) j.DebugElement(Element.Water); break;
                case "wind": if (j != null) j.DebugElement(Element.Wind); break;
                case "pick": if (j != null && a.Length > 1) j.DebugPick(int.Parse(a[1])); break;
                case "close": if (j != null) j.Close(); break;
                case "lodge":
                    var book = Object.FindFirstObjectByType<LodgeJournalBook>();
                    if (book != null) book.Open("bridge");
                    else Debug.LogWarning("[Step14hDebug] No lodge book (enter the lodge first).");
                    break;
                case "shot":
                    var g = Growth;
                    if (g == null || a.Length < 2) return;
                    // From the seated head the board faced (BattleDebug.SeatCamera holds the camera there).
                    var cam = Camera.main;
                    var board = j != null && JournalBoard.IsOpen ? j.runes.Root : g.pods.island.transform;
                    Vector3 eye = cam != null ? cam.transform.position : Eye(g), look = board.position + board.up * 0.15f;
                    BattleDebug.Render($"Logs/BattleShots/{a[1]}.png", eye, Quaternion.LookRotation(look - eye, Vector3.up));
                    Debug.Log($"[Step14hDebug] Shot {a[1]}.png.");
                    break;
            }
        }

        /// <summary>The temple warning as a door shows it (arg "keeper,n"; "yes" / "no" picks).</summary>
        public static void Warn()
        {
            if (Arg == "yes" || Arg == "no")
            {
                ChoiceBoard.DebugPick(Arg == "yes");
                return;
            }
            var a = Arg.Split(',');
            Dialogue.SetVar("keeper", a[0]);
            Dialogue.SetVar("n", a.Length > 1 ? a[1] : "4");
            Dialogue.Say(TempleRules.WarnLine);
            ChoiceBoard.Ask(TempleRules.GoIn, () => Debug.Log("[Step14hDebug] Go in."), TempleRules.NotYet, () => Debug.Log("[Step14hDebug] Not yet."));
        }

        /// <summary>Which pod is due, whether the show runs, the pinned objective.</summary>
        public static void State()
        {
            var g = Growth;
            if (g == null)
            {
                Debug.Log("[Step14hDebug] No GrowthDirector.");
                return;
            }
            var c = g.party.At(g.Due);
            Debug.Log($"[Step14hDebug] Due pod {g.Due + 1}{(c != null ? $" ({c.Name} Lv {c.level})" : "")}, showing {g.Showing}, " +
                      $"pod glow slot {g.pods.DueSlot + 1}, objective '{Dialogue.CurrentObjective}', talking {Dialogue.IsTalking}.");
        }

        /// <summary>
        /// A synthetic hand holds the due pod palm up in front of the near rim (arg "prefix[,down]": shots prefix_1.. over
        /// the show; "down" = palm down, so the 6 s fallback runs above the hand).
        /// </summary>
        public static void Palm()
        {
            var g = Growth;
            if (g == null || g.Due < 0)
            {
                Debug.LogWarning("[Step14hDebug] Nothing is due (exec Ready first).");
                return;
            }
            var a = Arg.Split(',');
            bool down = a.Length > 1 && a[1] == "down";
            var island = g.pods.island.transform;
            Vector3 home = g.pods.HomeWorld(g.Due);
            Vector3 toward = Vector3.ProjectOnPlane(island.forward, Vector3.up).normalized;
            g.debugPalmUp = down ? -0.9f : 1f;
            g.debugPalm = home + Vector3.up * 0.1f + toward * 0.08f;
            Debug.Log($"[Step14hDebug] Synthetic hand holds pod {g.Due + 1} palm {(down ? "down" : "up")} at {g.debugPalm.Value}.");
            if (!string.IsNullOrEmpty(a[0])) Sequence(a[0], down ? new[] { 1f, 6.6f, 8.5f, 10.5f, 12.4f, 14.2f, 18f } : new[] { 0.6f, 1.4f, 2.6f, 4.2f, 6.2f, 8.2f, 12.5f });
            EndAfter(down ? 24f : 18f);
        }

        /// <summary>Look + pinch on the glowing pod (arg "prefix": shots prefix_1..).</summary>
        public static void Pinch()
        {
            var g = Growth;
            if (g == null || !g.DebugPinchDue())
            {
                Debug.LogWarning("[Step14hDebug] Nothing is due (exec Ready first).");
                return;
            }
            if (!string.IsNullOrEmpty(Arg)) Sequence(Arg, new[] { 0.6f, 1.4f, 2.6f, 4.2f, 6.2f, 8.2f, 12.5f });
        }

        static void EndAfter(float seconds)
        {
            double at = EditorApplication.timeSinceStartup + seconds;
            EditorApplication.CallbackFunction tick = null;
            tick = () =>
            {
                if (EditorApplication.timeSinceStartup < at) return;
                EditorApplication.update -= tick;
                if (Growth != null) Growth.debugPalm = null;
            };
            EditorApplication.update += tick;
        }

        /// <summary>A seated player's head in front of the island (flat Play has no head: the camera sits at the origin).</summary>
        static Vector3 Eye(GrowthDirector g)
        {
            var t = g.pods.island.transform;
            Vector3 front = Vector3.ProjectOnPlane(t.forward, Vector3.up).normalized;
            return t.position + front * 0.55f + Vector3.up * 0.32f;
        }

        /// <summary>A shot of the island from the seated player's head (arg = name).</summary>
        public static void Shot()
        {
            var g = Growth;
            if (g == null) return;
            Vector3 eye = Eye(g), look = g.pods.island.transform.position + Vector3.up * 0.05f;
            BattleDebug.Render($"Logs/BattleShots/{Arg}.png", eye, Quaternion.LookRotation(look - eye, Vector3.up));
            Debug.Log($"[Step14hDebug] Shot {Arg}.png.");
        }

        /// <summary>Shots from the player's head toward the show (or the island) at these times.</summary>
        static void Sequence(string prefix, float[] times)
        {
            var queue = new List<(double at, string name)>();
            double t0 = EditorApplication.timeSinceStartup;
            for (int i = 0; i < times.Length; i++) queue.Add((t0 + times[i], $"{prefix}_{i + 1}"));
            EditorApplication.CallbackFunction tick = null;
            tick = () =>
            {
                if (!EditorApplication.isPlaying)
                {
                    EditorApplication.update -= tick;
                    return;
                }
                if (queue.Count == 0 || EditorApplication.timeSinceStartup < queue[0].at) return;
                var (_, name) = queue[0];
                queue.RemoveAt(0);
                if (queue.Count == 0) EditorApplication.update -= tick;
                var g = Growth;
                if (g == null) return;
                Vector3 look = g.debugPalm ?? g.pods.island.transform.position;
                Vector3 eye = Eye(g);
                BattleDebug.Render($"Logs/BattleShots/{name}.png", eye, Quaternion.LookRotation(look - eye, Vector3.up));
                Debug.Log($"[Step14hDebug] Shot {name}.png (showing {g.Showing}).");
            };
            EditorApplication.update += tick;
        }
    }
}
