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

        /// <summary>
        /// A frame sequence from a seated head toward the battle (arg "prefix,count,interval s"): Logs/BattleShots/prefix_n.png.
        /// </summary>
        public static void Frames()
        {
            var a = Arg.Split(',');
            string prefix = a[0];
            int count = a.Length > 1 && int.TryParse(a[1], out int c) ? c : 6;
            float every = a.Length > 2 && float.TryParse(a[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float e) ? e : 0.15f;
            int n = 0;
            double next = EditorApplication.timeSinceStartup;
            EditorApplication.CallbackFunction tick = null;
            tick = () =>
            {
                if (!EditorApplication.isPlaying || n >= count)
                {
                    EditorApplication.update -= tick;
                    return;
                }
                if (EditorApplication.timeSinceStartup < next) return;
                next += every;
                n++;
                var island = Island;
                if (island == null) return;
                var t = island.transform;
                Vector3 look = t.TransformPoint(new Vector3(0f, island.CapTopLocal + 0.05f, 0f));
                var actors = Object.FindObjectsByType<StageActor>(FindObjectsSortMode.None);
                if (actors.Length > 0)
                {
                    Vector3 sum = Vector3.zero;
                    foreach (var s in actors) sum += s.WorldCenter;
                    look = sum / actors.Length;
                }
                var eye = t.position + t.rotation * new Vector3(0.05f, 0.45f, 0.5f);
                BattleDebug.Render($"Logs/BattleShots/{prefix}_{n}.png", eye, Quaternion.LookRotation(look - eye, Vector3.up));
            };
            EditorApplication.update += tick;
            Debug.Log($"[Step16Debug] Frames {prefix}: {count} every {every:0.00} s.");
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

        /// <summary>
        /// Raises a thumb swipe from the right hand through MicrogestureInput's own event (arg = left/right/forward/back),
        /// then logs the explorer's tile 1.5 s later: proves the game's swipe path without a headset.
        /// </summary>
        public static void Swipe()
        {
            var input = Object.FindFirstObjectByType<MicrogestureInput>();
            var explorer = Object.FindFirstObjectByType<GestureTestPiece>();
            if (input == null || explorer == null)
            {
                Debug.Log("[Step16Debug] Swipe: no input or explorer.");
                return;
            }
            var g = Arg switch
            {
                "left" => ThumbGesture.SwipeLeft,
                "forward" => ThumbGesture.SwipeForward,
                "back" => ThumbGesture.SwipeBack,
                _ => ThumbGesture.SwipeRight,
            };
            var from = explorer.Tile;
            typeof(MicrogestureInput).GetMethod("Raise", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(input, new object[] { g, GestureSource.RightHand });
            double at = EditorApplication.timeSinceStartup + 1.5;
            EditorApplication.CallbackFunction tick = null;
            tick = () =>
            {
                if (EditorApplication.timeSinceStartup < at) return;
                EditorApplication.update -= tick;
                Debug.Log($"[Step16Debug] Swipe {g}: c{from.x} r{from.y} -> c{explorer.Tile.x} r{explorer.Tile.y} (can move {explorer.CanMove}: locked {explorer.Locked}, held still {explorer.HeldStill}, zoomed {Object.FindFirstObjectByType<WorldWindow>()?.IsZoomed}; opening {Opening.InProgress}, finale {FinaleDirector.Running}, swap {SceneSwap.IsBusy}, menu {GameMenu.IsOpen}; blocked {input.IsBlocked(GestureSource.RightHand)}).");
            };
            EditorApplication.update += tick;
        }
    }
}
