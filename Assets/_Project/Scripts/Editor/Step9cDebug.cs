using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 9c checks through the Claude bridge (exec:Diorama.EditorTools.Step9cDebug.X), in Play: jump into the Forge
    /// Hall with a Lv 14 party, push the Keeper to 1 HP, take the shard, grant it in the hub, place it, shots
    /// (Logs/BattleShots/9c_*.png).
    /// </summary>
    static class Step9cDebug
    {
        static T Find<T>() where T : Object => Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);

        /// <summary>Party to Lv 14 and healed; log and forge solved; Cinderwood in front of the forge door; into the hall.</summary>
        public static void ToHall()
        {
            if (!EditorApplication.isPlaying) return;
            var party = Find<Party>();
            if (party != null)
            {
                foreach (var c in party.members)
                {
                    c.level = Mathf.Max(c.level, 14);
                    c.Heal();
                }
                party.NotifyChanged();
            }
            // Through Restore, so the log, the forge and its door redraw as solved.
            var saved = PuzzleState.ToSaved();
            saved.RemoveAll(p => p.id == "cinder.log" || p.id == "cinder.bellows");
            saved.Add(new SavedPuzzle { id = "cinder.log", solved = true, stage = 2 });
            saved.Add(new SavedPuzzle { id = "cinder.bellows", solved = true, stage = 3 });
            PuzzleState.Restore(saved);
            var swap = Find<SceneSwap>();
            var map = AssetDatabase.LoadAssetAtPath<WorldMap>(CinderwoodTools.MapPath);
            var hall = AssetDatabase.LoadAssetAtPath<WorldMap>(ForgeHallTools.HallPath);
            if (swap == null || map == null || hall == null) return;
            var front = CinderwoodTools.Tile(CinderwoodTools.DoorXY + new Vector2Int(1, -1));
            if (SceneSwap.IsInside) swap.ReturnNow();
            swap.TravelNow(map, front, new Vector2Int(0, -1));
            swap.Enter(hall, ForgeHallTools.EntryTile, new Vector2Int(0, -1), front, new Vector2Int(0, 1));
            Debug.Log("[Step9cDebug] Party Lv 14+, into the Forge Hall.");
        }

        /// <summary>The Keeper at 1 HP (the next hit wins).</summary>
        public static void KeeperToOne()
        {
            var enc = Find<EncounterDirector>();
            var b = enc != null ? enc.CurrentBattle : null;
            if (b == null || b.keeper == null) return;
            b.wild.isling.currentHp = 1;
            Debug.Log("[Step9cDebug] Keeper at 1 HP.");
        }

        public static void TakeShard()
        {
            var k = Find<KeeperHall>();
            if (k != null) k.CollectShard("bridge");
        }

        /// <summary>Gives the fire shard (held) and goes to the hub in front of the statue.</summary>
        public static void GrantShardInHub()
        {
            if (!EditorApplication.isPlaying) return;
            PuzzleState.SetStage(ShardHolder.FireId, 1);
            var socket = Find<StatueSocket>();
            var swap = Find<SceneSwap>();
            var hub = AssetDatabase.LoadAssetAtPath<WorldMap>(StillIsleTools.MapPath);
            if (socket == null || swap == null || hub == null) return;
            if (SceneSwap.IsInside) swap.ReturnNow();
            var front = socket.statueAnchor + new Vector2Int(1, 4);
            swap.TravelNow(hub, front, new Vector2Int(0, -1));
            Debug.Log($"[Step9cDebug] Fire shard held; hub at c{front.x} r{front.y} in front of the statue.");
        }

        public static void PlaceShard()
        {
            var s = Find<StatueSocket>();
            if (s != null) s.DebugPlace();
        }

        public static void Snort()
        {
            var k = Find<KeeperHall>();
            if (k != null) k.Snort();
        }

        public static void Describe()
        {
            var e = Find<GestureTestPiece>();
            var enc = Find<EncounterDirector>();
            var b = enc != null ? enc.CurrentBattle : null;
            Debug.Log($"[Step9cDebug] Puzzles: {PuzzleState.Describe()}; realm {RealmState.Restored:0.##}; " +
                      $"inside {(SceneSwap.IsInside ? Find<SceneSwap>().Interior.name : "no")}; explorer {(e != null ? $"c{e.Tile.x} r{e.Tile.y}" : "-")}; " +
                      $"battle {(b == null ? "none" : $"{b.wild.isling.Name} {b.wild.Hp}/{b.wild.MaxHp}{(b.keeper != null ? $", shield {(b.keeper.Shielded ? "up" : "down")} cracks {b.keeper.Cracks} turn {b.keeper.Turn} phase2 {b.keeper.Phase2}" : "")}")}.");
        }

        /// <summary>Seated view: Logs/BattleShots/9c_{name}.png.</summary>
        public static void Seat(string name)
        {
            var island = Find<IslandBuilder>();
            if (island == null) return;
            var t = island.transform;
            var eye = t.position + t.rotation * new Vector3(0f, 0.5f, 0.42f);
            BattleDebug.Render($"Logs/BattleShots/9c_{name}.png", eye, Quaternion.LookRotation(t.position + t.rotation * new Vector3(0f, 0f, 0.02f) - eye, Vector3.up));
        }

        public static void ShotMeet() => Seat("meet");
        public static void ShotBattle() => Seat("battle");
        public static void ShotVictory() => Seat("victory");
        public static void ShotShard() => Seat("shard");
        public static void ShotWave() => Seat("wave");
        public static void ShotRestored() => Seat("restored");
        public static void ShotHub() => Seat("hub");
        public static void ShotStatue() => Seat("statue");
    }
}
