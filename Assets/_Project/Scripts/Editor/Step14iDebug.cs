using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>Bridge hooks for step 14i (Play mode): the island-lesson ghosts, facing, shots.</summary>
    public static class Step14iDebug
    {
        const string ArgFile = "Temp/ClaudeBridge/arg.txt";
        static string Arg => File.Exists(ArgFile) ? File.ReadAllText(ArgFile).Trim() : "";

        static IslandBuilder Island => Object.FindFirstObjectByType<IslandBuilder>();

        static Vector3 Corner(bool right)
        {
            var island = Island;
            var half = island.FootprintHalfSize;
            return island.transform.TransformPoint(new Vector3(right ? half.x : -half.x, island.CapTopLocal - 0.02f, half.y));
        }

        /// <summary>The move lesson's ghost (one hand on the dominant corner) or, arg "two", the two-hands ghost.</summary>
        public static void Ghost()
        {
            var g = Object.FindFirstObjectByType<GhostHands>();
            if (g == null) return;
            if (Arg == "two") g.Show("grab_rim_slide", GhostPlace.RimCorners, null, 1f, DominantHand.Left, true);
            else if (Arg == "off") g.Hide();
            else g.Show("grab_rim_slide", GhostPlace.AtTarget, null, 1f, DominantHand.Left, false, 0f, "rimCorner");
        }

        /// <summary>Shots from a seated head toward the near corners at these times (arg "prefix,t1,t2,...").</summary>
        public static void Shots()
        {
            var a = Arg.Split(',');
            var queue = new List<(double at, string name)>();
            double t0 = EditorApplication.timeSinceStartup;
            for (int i = 1; i < a.Length; i++)
                if (float.TryParse(a[i], out float t)) queue.Add((t0 + t, $"{a[0]}_{i}"));
            EditorApplication.CallbackFunction tick = null;
            tick = () =>
            {
                if (!EditorApplication.isPlaying || queue.Count == 0)
                {
                    EditorApplication.update -= tick;
                    return;
                }
                if (EditorApplication.timeSinceStartup < queue[0].at) return;
                Take(queue[0].name);
                queue.RemoveAt(0);
            };
            EditorApplication.update += tick;
        }

        public static void Shot() => Take(string.IsNullOrEmpty(Arg) ? "14i_shot" : Arg);

        static void Take(string name)
        {
            var island = Island;
            if (island == null) return;
            var t = island.transform;
            var eye = t.position + t.rotation * new Vector3(0.05f, 0.5f, 0.55f);
            var look = (Corner(true) + Corner(false)) * 0.5f + Vector3.up * 0.02f;
            BattleDebug.Render($"Logs/BattleShots/{name}.png", eye, Quaternion.LookRotation(look - eye, Vector3.up));
            Debug.Log($"[Step14iDebug] Shot {name}.png.");
        }

        /// <summary>Travels to a Dev panel spot (arg = its label, e.g. "CW: springs").</summary>
        public static void Go()
        {
            var s = Object.FindFirstObjectByType<GameState>();
            if (s == null) return;
            foreach (var spot in s.Spots)
                if (spot.label == Arg)
                {
                    s.GoTo(spot);
                    Debug.Log($"[Step14iDebug] Going to {Arg}.");
                    return;
                }
            Debug.LogWarning($"[Step14iDebug] No spot '{Arg}'.");
        }

        /// <summary>Logs the island's yaw error against the line to the head, and each hand's hold state.</summary>
        public static void Facing()
        {
            var h = Object.FindFirstObjectByType<IslandHandle>();
            if (h == null) return;
            Debug.Log($"[Step14iDebug] Facing error {h.FacingError():0.0} deg.");
        }

        /// <summary>Turns the island off the head by arg degrees (default 12) to watch the idle re-face.</summary>
        public static void Turn()
        {
            var island = Island;
            if (island == null) return;
            float deg = float.TryParse(Arg, out float d) ? d : 12f;
            island.transform.rotation = Quaternion.Euler(0f, deg, 0f) * island.transform.rotation;
            Debug.Log($"[Step14iDebug] Island turned {deg:0} deg.");
        }
    }
}
