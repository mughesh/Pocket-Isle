using System.IO;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Galewick bridge helpers for flat Editor Play (steps 10a-10d): exec:Diorama.EditorTools.GalewickPlay.X. Arguments go
    /// in Temp/ClaudeBridge/arg.txt (a dev panel spot label, "x,y" Galewick tiles, or a shot name). Shots land in
    /// Logs/BattleShots/{arg}.png from the seat.
    /// </summary>
    public static class GalewickPlay
    {
        const string ArgFile = "Temp/ClaudeBridge/arg.txt";

        static string Arg => File.Exists(ArgFile) ? File.ReadAllText(ArgFile).Trim() : "";

        static GestureTestPiece Explorer => Object.FindFirstObjectByType<GestureTestPiece>();

        static Vector2Int ArgTile
        {
            get
            {
                var parts = Arg.Split(',');
                return parts.Length == 2 && int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y) ? GalewickTools.Tile(x, y) : Vector2Int.zero;
            }
        }

        /// <summary>Past the title, the seated camera, wild battles off (walk tests).</summary>
        public static void Start()
        {
            var title = Object.FindFirstObjectByType<TitleScreen>();
            if (title != null && TitleScreen.Showing) title.Cancel();
            BattleDebug.SeatCamera();
            DevCheats.WildOff = true;
            Debug.Log($"[GalewickPlay] Started: title {TitleScreen.Showing}, opening {Opening.InProgress}, wild off.");
        }

        public static void WildOn() => DevCheats.WildOff = false;

        /// <summary>Dev panel Go to the spot named in arg.txt (e.g. "GW: kite field").</summary>
        public static void Go()
        {
            var state = GameState.Instance;
            if (state == null) return;
            foreach (var s in state.Spots)
                if (s.label == Arg)
                {
                    Debug.Log($"[GalewickPlay] Go to {s.label}: {state.GoTo(s)}");
                    return;
                }
            Debug.LogWarning($"[GalewickPlay] No spot '{Arg}'.");
        }

        /// <summary>Walk (path search) to the Galewick tile "x,y" in arg.txt.</summary>
        public static void Walk()
        {
            var e = Explorer;
            var t = ArgTile;
            if (e == null) return;
            Debug.Log($"[GalewickPlay] Walk to c{t.x} r{t.y} ({Arg}): {e.GoTo(t)}; can reach {e.CanReach(t)}");
        }

        /// <summary>Teleport to the Galewick tile "x,y" in arg.txt (facing north).</summary>
        public static void Teleport()
        {
            var e = Explorer;
            if (e == null) return;
            e.Teleport(ArgTile, new Vector2Int(0, -1));
            Debug.Log($"[GalewickPlay] Teleported to c{e.Tile.x} r{e.Tile.y}.");
        }

        public static void Restore()
        {
            var r = Object.FindFirstObjectByType<RealmRestoration>();
            if (r != null) r.FadeTo(1f);
        }

        public static void Unrestore()
        {
            var r = Object.FindFirstObjectByType<RealmRestoration>();
            if (r != null) r.FadeTo(0f);
        }

        /// <summary>A seated shot to Logs/BattleShots/{arg}.png.</summary>
        public static void Shot()
        {
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            if (island == null) return;
            string name = string.IsNullOrEmpty(Arg) ? "10_play" : Arg;
            var t = island.transform;
            var eye = t.position + t.rotation * new Vector3(0f, 0.42f, 0.5f);
            var look = t.position + t.rotation * new Vector3(0f, 0.09f, 0f);
            BattleDebug.Render($"Logs/BattleShots/{name}.png", eye, Quaternion.LookRotation(look - eye, Vector3.up));
            Debug.Log($"[GalewickPlay] Shot {name}.png");
        }

        /// <summary>A lower, closer shot (props up close) to Logs/BattleShots/{arg}.png.</summary>
        public static void CloseShot()
        {
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            if (island == null) return;
            string name = string.IsNullOrEmpty(Arg) ? "10_close" : Arg;
            var t = island.transform;
            var eye = t.position + t.rotation * new Vector3(0.04f, 0.22f, 0.38f);
            var look = t.position + t.rotation * new Vector3(0f, 0.06f, -0.06f);
            BattleDebug.Render($"Logs/BattleShots/{name}.png", eye, Quaternion.LookRotation(look - eye, Vector3.up));
            Debug.Log($"[GalewickPlay] Close shot {name}.png");
        }

        public static void State()
        {
            var e = Explorer;
            var island = e != null ? e.island : null;
            var map = island != null ? island.world : null;
            Debug.Log($"[GalewickPlay] State: {(GameState.Instance != null ? GameState.Instance.Where : "?")}, restored {RealmState.Restored:0.##}, wind spin {RealmState.WindSpin:0.0}, " +
                      $"theme {(map != null && map.theme != null ? map.theme.name : "none")}, swap busy {SceneSwap.IsBusy}, locked {(e != null && e.Locked)}, " +
                      $"destination {(e != null && e.Destination.HasValue ? e.Destination.Value.ToString() : "none")}, puzzles {PuzzleState.Describe()}");
        }

        /// <summary>Uses the gate with id arg.txt ("wind", "home") on the current map: walks there first if needed.</summary>
        public static void Gate()
        {
            var gates = Object.FindFirstObjectByType<ShardGates>();
            if (gates == null) return;
            int i = gates.IndexOf(Arg);
            Debug.Log($"[GalewickPlay] Gate '{Arg}' index {i}.");
            if (i >= 0) gates.Pick(i, "bridge");
        }
    }
}
