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
