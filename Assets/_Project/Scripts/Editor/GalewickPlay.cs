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
            // The returning player's title waits for Continue (cancelling it would leave the explorer locked).
            if (TitleScreen.Showing) RuneBoard.DebugPress("Continue");
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

        // ---- step 10b ----

        static WheelPiece Mill
        {
            get
            {
                foreach (var w in Object.FindObjectsByType<WheelPiece>(FindObjectsSortMode.None))
                    if (w.id == "gw.mill") return w;
                return null;
            }
        }

        /// <summary>One flick on the big mill (speed in arg.txt, default 0.9 m/s).</summary>
        public static void Flick()
        {
            var m = Mill;
            float speed = float.TryParse(Arg, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var s) ? s : 0.9f;
            if (m != null) m.DebugFlick(speed);
            Debug.Log($"[GalewickPlay] Mill: here {(m != null && m.Here)}, usable {(m != null && m.Usable)}, angle {(m != null ? m.Angle : 0f):0}, kept {(m != null && m.Spin.Kept)}, solved {(m != null && m.IsSolved)}");
        }

        /// <summary>Three flicks at once (the bridge is too slow to land three inside the 4 s window one call at a time).</summary>
        public static void Flick3()
        {
            var m = Mill;
            if (m == null) return;
            for (int i = 0; i < 3; i++) m.DebugFlick(0.9f);
            Debug.Log($"[GalewickPlay] Mill: 3 flicks, {m.Spin.RecentFlicks} in the window.");
        }

        public static void Describe()
        {
            var k = Object.FindFirstObjectByType<KitePiece>();
            var b = Object.FindFirstObjectByType<CloudBankPiece>();
            var d = Object.FindFirstObjectByType<TowerDoor>();
            var m = Mill;
            Debug.Log($"[GalewickPlay] mill solved {(m != null && m.IsSolved)}; {(k != null ? k.Describe() : "no kites")}; {(b != null ? b.Describe() : "no bank")}; tower open {(d != null && d.IsOpen)}; puzzles {PuzzleState.Describe()}");
        }

        /// <summary>Kite number arg.txt rises and flies (look + pinch).</summary>
        public static void Kite()
        {
            var k = Object.FindFirstObjectByType<KitePiece>();
            if (k == null) return;
            k.DebugFly(int.TryParse(Arg, out int i) ? i : 0);
            Debug.Log($"[GalewickPlay] {k.Describe()}");
        }

        /// <summary>Kite number arg.txt held 6 cm up and let go (too low: it drifts back).</summary>
        public static void KiteLow()
        {
            var k = Object.FindFirstObjectByType<KitePiece>();
            if (k == null) return;
            k.DebugLiftAndRelease(int.TryParse(Arg, out int i) ? i : 0, 0.06f);
            Debug.Log($"[GalewickPlay] {k.Describe()}");
        }

        /// <summary>A gust along the ridge path over the cloud bank (strength in arg.txt, default 0.9).</summary>
        public static void Gust()
        {
            var b = Object.FindFirstObjectByType<CloudBankPiece>();
            if (b == null) return;
            float strength = float.TryParse(Arg, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var s) ? s : 0.9f;
            b.DebugGust(strength);
            Debug.Log($"[GalewickPlay] Cloud bank: {b.Describe()}, here {b.Here}");
        }

        /// <summary>A gust across the island toward the player (the ripple), anywhere in Galewick.</summary>
        public static void GustAnywhere()
        {
            var g = Object.FindFirstObjectByType<GustField>();
            if (g != null) g.DebugGust(new Vector2(0f, 1f));
        }

        /// <summary>A wild battle right here with the species named in arg.txt (Lv 7).</summary>
        public static void Fight()
        {
            var state = GameState.Instance;
            var s = UnityEditor.AssetDatabase.LoadAssetAtPath<IslingSpecies>($"Assets/_Project/Data/Creatures/Species/{Arg}.asset");
            Debug.Log($"[GalewickPlay] Fight {Arg}: {(state != null && state.Fight(s, 7))}");
        }

        public static void Tower()
        {
            var d = Object.FindFirstObjectByType<TowerDoor>();
            if (d != null) d.DebugTry();
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
