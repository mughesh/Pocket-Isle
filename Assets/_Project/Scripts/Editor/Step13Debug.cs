using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 13 checks through the Claude bridge (exec:Diorama.EditorTools.Step13Debug.X), in Play after
    /// GalewickPlay.Start. Shots in Logs/BattleShots/13_*. The save is real: put it back after.
    /// </summary>
    static class Step13Debug
    {
        const string ArgFile = "Temp/ClaudeBridge/arg.txt";
        static string Arg => System.IO.File.Exists(ArgFile) ? System.IO.File.ReadAllText(ArgFile).Trim() : "";

        static T Find<T>() where T : Object => Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);

        static string ShardId(string e) => "shard." + e.Trim();

        /// <summary>
        /// arg "placed;held" (elements, comma separated: "fire,wind;water"): those shards in the statue, those held by the
        /// Spirit, the rest not found; the finale not started; the explorer in front of the statue on the hub.
        /// </summary>
        public static void Shards()
        {
            var parts = (Arg + ";").Split(';');
            var saved = PuzzleState.ToSaved();
            saved.RemoveAll(p => p.id.StartsWith("shard.") || p.id == FinaleLogic.StageId || p.id == FinaleLogic.DoneId);
            foreach (var e in parts[0].Split(','))
                if (e.Trim().Length > 0) saved.Add(new SavedPuzzle { id = ShardId(e), solved = true, stage = 2 });
            foreach (var e in parts[1].Split(','))
                if (e.Trim().Length > 0) saved.Add(new SavedPuzzle { id = ShardId(e), solved = false, stage = 1 });
            PuzzleState.Restore(saved);
            ToStatue();
            Debug.Log($"[Step13Debug] Shards placed [{parts[0]}], held [{parts[1]}]; statue placed {StatueSocket.PlacedCount}.");
        }

        /// <summary>The hub, in front of the statue (4 tiles), facing it.</summary>
        public static void ToStatue()
        {
            var socket = StatueSocket.FindById(ShardHolder.FireId);
            var swap = Find<SceneSwap>();
            var hub = AssetDatabase.LoadAssetAtPath<WorldMap>(StillIsleTools.MapPath);
            if (socket == null || swap == null || hub == null) return;
            if (SceneSwap.IsInside) swap.ReturnNow();
            var front = socket.statueAnchor + new Vector2Int(1, 4);
            swap.TravelNow(hub, front, new Vector2Int(0, -1));
        }

        /// <summary>arg element: that held shard flies into its socket (as if carried).</summary>
        public static void Place()
        {
            var s = StatueSocket.FindById(ShardId(Arg));
            if (s == null)
            {
                Debug.LogWarning($"[Step13Debug] No socket {ShardId(Arg)}.");
                return;
            }
            s.DebugPlace();
            Debug.Log($"[Step13Debug] Placing {s.id} (held {ShardHolder.Holds(s.id)}, here {s.Here}).");
        }

        /// <summary>
        /// A shot of the statue / Heart to Logs/BattleShots/{name}.png. arg "name[,view]": view front (default), 34, side,
        /// high (the seat), close.
        /// </summary>
        public static void Shot()
        {
            var a = Arg.Split(',');
            string name = a[0].Length > 0 ? a[0] : "13_statue";
            string view = a.Length > 1 ? a[1].Trim() : "front";
            var island = Find<IslandBuilder>();
            var socket = StatueSocket.FindById(ShardHolder.FireId);
            if (island == null || socket == null) return;
            var t = island.transform;
            float tile = island.map.tileSize * island.Zoom * t.lossyScale.x;
            Vector3 c = t.TransformPoint(island.TileToLocal(socket.Center, island.Content.capHeight)) + t.up * (1.3f * tile);
            Vector3 fwd = t.forward, right = t.right;
            Vector3 eye;
            switch (view)
            {
                case "34": eye = c + (fwd * 0.75f + right * 0.55f) * (9f * tile) + t.up * (2.2f * tile); break;
                case "side": eye = c + right * (9f * tile) + t.up * (1.4f * tile); break;
                case "high": eye = t.position + t.rotation * new Vector3(0f, 0.42f, 0.5f); break;
                case "close": eye = c + fwd * (6f * tile) + t.up * (1.2f * tile); break;
                default: eye = c + fwd * (9f * tile) + t.up * (1.8f * tile); break;
            }
            BattleDebug.Render($"Logs/BattleShots/{name}.png", eye, Quaternion.LookRotation(c - eye, Vector3.up));
            Debug.Log($"[Step13Debug] Shot {name}.png ({view}).");
        }

        public static void State()
        {
            var ids = new List<string>();
            foreach (var p in PuzzleState.ToSaved())
                if (p.id.StartsWith("shard.") || p.id.StartsWith("finale")) ids.Add($"{p.id}:{p.stage}{(p.solved ? "+" : "")}");
            var d = Find<Dialogue>();
            Debug.Log($"[Step13Debug] {string.Join(" ", ids)}; placed {StatueSocket.PlacedCount}; dialogue '{(d != null ? d.CurrentTrigger : null)}'.");
        }
    }
}
