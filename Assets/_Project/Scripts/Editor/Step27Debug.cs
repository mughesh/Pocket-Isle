using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>Step 27 bridge: seat shots (Logs/BattleShots) and a held gaze on the door in view.</summary>
    public static class Step27Debug
    {
        /// <summary>The seat camera's view now, to Logs/BattleShots/{name}.png.</summary>
        public static void Shot(string name)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[Step27Debug] No camera.");
                return;
            }
            System.IO.Directory.CreateDirectory("Logs/BattleShots");
            BattleDebug.Render($"Logs/BattleShots/{name}.png", cam.transform.position, cam.transform.rotation);
            Debug.Log($"[Step27Debug] Shot {name}.");
        }

        static bool _hold;

        /// <summary>While on: the tower door counts as looked at every frame (what FocusHighlight then shows).</summary>
        public static void HoldGaze(bool on)
        {
            if (on == _hold) return;
            _hold = on;
            if (on) EditorApplication.update += Tick;
            else EditorApplication.update -= Tick;
        }

        static void Tick()
        {
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            if (!EditorApplication.isPlaying || island == null || island.world == null || island.Content == null)
            {
                HoldGaze(false);
                return;
            }
            if (!island.world.TryGetLandmark("tower", out var tile)) return;
            float size = island.map.tileSize;
            FocusHighlight.Hover(island.transform.TransformPoint(island.TileToLocal(new Vector2(tile.x + 0.5f, tile.y - 0.25f), island.Content.capHeight + 0.75f * size)),
                1.6f * size * island.Zoom * island.transform.lossyScale.x);
        }
    }
}
