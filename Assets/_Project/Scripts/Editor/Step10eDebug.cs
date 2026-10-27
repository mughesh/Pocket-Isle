using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 10e checks through the Claude bridge (exec:Diorama.EditorTools.Step10eDebug.X), in Play. The Cinderwood
    /// regression run: <see cref="CinderReset"/>, Step9cDebug.ToHall, wait for the battle, Step9cDebug.KeeperToOne, a
    /// move rune (Step9gDebug.Rune), Step9cDebug.TakeShard, wait for the wave, Step9cDebug.GrantShardInHub,
    /// Step9cDebug.PlaceShard, <see cref="State"/> after each.
    /// </summary>
    static class Step10eDebug
    {
        static T Find<T>() where T : Object => Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);

        /// <summary>Hearthstag frightened again, the fire shard neither held nor placed, Cinderwood unrestored.</summary>
        public static void CinderReset()
        {
            if (!EditorApplication.isPlaying) return;
            var saved = PuzzleState.ToSaved();
            saved.RemoveAll(p => p.id == "cinder.keeper" || p.id == "shard.fire");
            PuzzleState.Restore(saved);
            var realm = Find<RealmRestoration>();
            var cinder = AssetDatabase.LoadAssetAtPath<WorldMap>(CinderwoodTools.MapPath);
            if (realm != null && cinder != null) realm.Remember(cinder, 0f);
            Debug.Log("[Step10eDebug] Cinderwood's Keeper and fire shard reset.");
        }

        /// <summary>Party Lv 12+ and healed; the tower open; in front of the tower door; into the tower hall.</summary>
        public static void ToTowerHall()
        {
            if (!EditorApplication.isPlaying) return;
            var party = Find<Party>();
            if (party != null)
            {
                foreach (var c in party.members)
                {
                    c.level = Mathf.Max(c.level, 12);
                    c.Heal();
                }
                party.NotifyChanged();
            }
            var saved = PuzzleState.ToSaved();
            saved.RemoveAll(p => p.id == TowerDoor.TowerId);
            saved.Add(new SavedPuzzle { id = TowerDoor.TowerId, solved = true, stage = 1 });
            PuzzleState.Restore(saved);
            var swap = Find<SceneSwap>();
            var map = AssetDatabase.LoadAssetAtPath<WorldMap>(GalewickTools.MapPath);
            var hall = AssetDatabase.LoadAssetAtPath<WorldMap>(TowerHallTools.HallPath);
            if (swap == null || map == null || hall == null) return;
            var front = GalewickTools.TowerFront;
            if (SceneSwap.IsInside) swap.ReturnNow();
            swap.TravelNow(map, front, new Vector2Int(0, -1));
            swap.Enter(hall, TowerHallTools.EntryTile, new Vector2Int(0, -1), front, new Vector2Int(0, 1));
            Debug.Log("[Step10eDebug] Party Lv 12+, into the tower hall.");
        }

        /// <summary>The tower hall's sky clear amount from arg.txt (0..1).</summary>
        public static void Sky()
        {
            var h = Find<TowerHall>();
            if (h == null) return;
            float.TryParse(System.IO.File.ReadAllText("Temp/ClaudeBridge/arg.txt").Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out h.clear);
            Debug.Log($"[Step10eDebug] Sky clear {h.clear:0.##}.");
        }

        static KeeperHall Galeowl()
        {
            foreach (var k in Object.FindObjectsByType<KeeperHall>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (k.keeperId == "gw.keeper") return k;
            return null;
        }

        static StatueSocket Socket(string id)
        {
            foreach (var s in Object.FindObjectsByType<StatueSocket>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (s.id == id) return s;
            return null;
        }

        static int Arg(int fallback)
        {
            try { return int.Parse(System.IO.File.ReadAllText("Temp/ClaudeBridge/arg.txt").Trim()); }
            catch { return fallback; }
        }

        /// <summary>Galeowl tangled and frightened again, the wind shard neither held nor placed, Galewick unrestored.</summary>
        public static void GaleowlReset()
        {
            if (!EditorApplication.isPlaying) return;
            var saved = PuzzleState.ToSaved();
            saved.RemoveAll(p => p.id == "gw.keeper" || p.id == "gw.knots" || p.id == ShardHolder.WindId);
            PuzzleState.Restore(saved);
            var realm = Find<RealmRestoration>();
            var gw = AssetDatabase.LoadAssetAtPath<WorldMap>(GalewickTools.MapPath);
            if (realm != null && gw != null) realm.Remember(gw, 0f);
            Debug.Log("[Step10eDebug] Galeowl, its knots and the wind shard reset.");
        }

        /// <summary>Knot arg.txt (0..2) snaps as if pulled apart.</summary>
        public static void Knot()
        {
            var u = Find<KeeperUntangle>();
            if (u != null) u.DebugSnap(Arg(0));
            Debug.Log($"[Step10eDebug] {(u != null ? u.Describe() : "no untangle")}");
        }

        /// <summary>Look + pinch on knot arg.txt (it loosens in 2 s).</summary>
        public static void KnotGaze()
        {
            var u = Find<KeeperUntangle>();
            if (u != null) u.DebugGaze(Arg(0));
            Debug.Log($"[Step10eDebug] {(u != null ? u.Describe() : "no untangle")}");
        }

        public static void TakeWind()
        {
            var k = Galeowl();
            if (k != null) k.CollectShard("bridge");
        }

        /// <summary>Gives the wind shard (held) and goes to the hub in front of the statue.</summary>
        public static void WindToHub()
        {
            if (!EditorApplication.isPlaying) return;
            if (!PuzzleState.IsSolved(ShardHolder.WindId)) PuzzleState.SetStage(ShardHolder.WindId, 1);
            var socket = Socket(ShardHolder.WindId);
            var swap = Find<SceneSwap>();
            var hub = AssetDatabase.LoadAssetAtPath<WorldMap>(StillIsleTools.MapPath);
            if (socket == null || swap == null || hub == null) return;
            if (SceneSwap.IsInside) swap.ReturnNow();
            var front = socket.statueAnchor + new Vector2Int(1, 4);
            swap.TravelNow(hub, front, new Vector2Int(0, -1));
            Debug.Log($"[Step10eDebug] Wind shard held; hub at c{front.x} r{front.y} in front of the statue.");
        }

        /// <summary>The fire shard already in the statue (for the two-shard line).</summary>
        public static void FirePlaced()
        {
            var saved = PuzzleState.ToSaved();
            saved.RemoveAll(p => p.id == ShardHolder.FireId);
            saved.Add(new SavedPuzzle { id = ShardHolder.FireId, solved = true, stage = 2 });
            PuzzleState.Restore(saved);
            Debug.Log($"[Step10eDebug] Fire shard placed; statue placed {StatueSocket.PlacedCount}.");
        }

        /// <summary>Dev panel Go to "GW: tower hall".</summary>
        public static void GoHall()
        {
            var g = Find<GameState>();
            if (g == null) return;
            foreach (var s in g.Spots)
                if (s.label == "GW: tower hall") Debug.Log($"[Step10eDebug] Go to tower hall: {g.GoTo(s)}.");
        }

        public static void PlaceWind()
        {
            var s = Socket(ShardHolder.WindId);
            if (s != null) s.DebugPlace();
        }

        public static void Hoot()
        {
            var k = Galeowl();
            if (k != null) k.Snort();
        }

        public static void GaleowlState()
        {
            var k = Galeowl();
            var u = Find<KeeperUntangle>();
            var enc = Find<EncounterDirector>();
            var b = enc != null ? enc.CurrentBattle : null;
            Debug.Log($"[Step10eDebug] Galeowl: calmed {(k != null && k.Calmed)}, idle {(k != null && k.Idle)}, model {(k != null && k.Model != null ? (k.Model.gameObject.activeInHierarchy ? "shown" : "hidden") : "none")}, " +
                      $"{(u != null ? u.Describe() : "no untangle")}; battle {(b == null ? "none" : $"{b.wild.isling.Name} Lv {b.wild.isling.level} {b.wild.Hp}/{b.wild.MaxHp}")}; " +
                      $"shard.wind stage {PuzzleState.Stage(ShardHolder.WindId)}{(PuzzleState.IsSolved(ShardHolder.WindId) ? " placed" : "")}; shard waiting {(k != null && k.ShardWaiting)}; " +
                      $"realm {RealmState.Restored:0.##}; sky clear {(Find<TowerHall>() is TowerHall h ? h.clear : -1f):0.##}; statue placed {StatueSocket.PlacedCount}; " +
                      $"tower gate shown {(k != null && k.realmMap != null ? (!k.realmMap.IsPropHidden(k.templeGate)).ToString() : "-")}.");
        }

        /// <summary>The first move rune, as if poked.</summary>
        public static void Move0()
        {
            var r = Find<BattleRunes>();
            if (r != null) r.Choose(0);
        }

        /// <summary>Puzzles, battle, keeper phase, the statue's sockets.</summary>
        public static void State()
        {
            var enc = Find<EncounterDirector>();
            var b = enc != null ? enc.CurrentBattle : null;
            var swap = Find<SceneSwap>();
            var island = Find<IslandBuilder>();
            var sb = new System.Text.StringBuilder("[Step10eDebug] ");
            sb.Append($"keeper {(PuzzleState.IsSolved("cinder.keeper") ? "calmed" : "frightened")}, shard.fire stage {PuzzleState.Stage("shard.fire")} {(PuzzleState.IsSolved("shard.fire") ? "placed" : "")}; ");
            sb.Append($"realm {RealmState.Restored:0.##}; map {(island != null && island.world != null ? island.world.name : "-")}; inside {(SceneSwap.IsInside && swap != null ? swap.Interior.name : "no")}; ");
            sb.Append($"battle {(b == null ? "none" : $"{b.wild.isling.Name} {b.wild.Hp}/{b.wild.MaxHp}")}; in battle {(enc != null && enc.InBattle)}; ");
            foreach (var k in Object.FindObjectsByType<KeeperHall>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                sb.Append($"hall '{k.name}' shard waiting {k.ShardWaiting}; ");
            foreach (var s in Object.FindObjectsByType<StatueSocket>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                sb.Append($"socket {s.id} solved {s.IsSolved}; ");
            if (island != null && island.world != null)
                sb.Append($"temple gate shown {(KeeperHall.FindById(KeeperHall.KeeperId) is KeeperHall kh && kh.realmMap != null ? (!kh.realmMap.IsPropHidden(kh.templeGate)).ToString() : "-")}.");
            Debug.Log(sb.ToString());
        }
    }
}
