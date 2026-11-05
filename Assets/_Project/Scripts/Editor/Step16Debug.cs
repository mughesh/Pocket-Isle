using System.IO;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>Bridge hooks for step 16 (Play mode): ghost previews, shots from a seated head.</summary>
    public static class Step16Debug
    {
        const string ArgFile = "Temp/ClaudeBridge/arg.txt";
        static string Arg => File.Exists(ArgFile) ? File.ReadAllText(ArgFile).Trim() : "";

        static IslandBuilder Island => Object.FindFirstObjectByType<IslandBuilder>();

        /// <summary>Plays a ghost clip over the island (arg = clip name; "off" ends it).</summary>
        public static void Ghost()
        {
            var g = Object.FindFirstObjectByType<GhostHands>();
            if (g == null) return;
            if (Arg == "off") g.EndPreview();
            else g.BeginPreview(string.IsNullOrEmpty(Arg) ? "thumb_swipe_forward" : Arg, GhostPlace.AboveIsland, 0.6f);
            Debug.Log($"[Step16Debug] Ghost {Arg}.");
        }

        /// <summary>A seated head's shot of the island's near half (arg = file name, optional ",side" for a side view).</summary>
        public static void Shot()
        {
            var island = Island;
            if (island == null) return;
            var a = Arg.Split(',');
            string name = string.IsNullOrEmpty(a[0]) ? "16_shot" : a[0];
            bool side = a.Length > 1 && a[1] == "side";
            var t = island.transform;
            var eye = t.position + t.rotation * (side ? new Vector3(0.45f, 0.35f, 0.3f) : new Vector3(0.05f, 0.5f, 0.55f));
            var look = t.TransformPoint(new Vector3(0f, island.CapTopLocal + 0.08f, island.FootprintHalfSize.y * 0.4f));
            BattleDebug.Render($"Logs/BattleShots/{name}.png", eye, Quaternion.LookRotation(look - eye, Vector3.up));
            Debug.Log($"[Step16Debug] Shot {name}.png.");
        }

        public static void Palm()
        {
            var g = Object.FindFirstObjectByType<GhostHands>();
            if (g != null) Debug.Log($"[Step16Debug] Ghost palm: {g.DebugPalm()}");
        }

        /// <summary>The Spirit's spot, the bubble's and the objective spheres (log).</summary>
        public static void Spirit()
        {
            var s = Object.FindFirstObjectByType<LanternSpirit>();
            var list = new System.Collections.Generic.List<Vector4>();
            ObjectiveArea.Gather(list);
            Debug.Log($"[Step16Debug] Spirit at {(s != null ? s.transform.position.ToString("F3") : "-")}, objective spheres {list.Count}: {string.Join(" ", list)}.");
        }
    }
}
