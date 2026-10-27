using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 11c checks through the Claude bridge (exec:Diorama.EditorTools.Step11cDebug.X), in Play. Start with
    /// GalewickPlay.Start; the battle with Step9cDebug.KeeperToOne / Step10eDebug.Move0. The save is real: put it back after.
    /// </summary>
    static class Step11cDebug
    {
        static T Find<T>() where T : Object => Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);

        static KeeperHall Hall => KeeperHall.FindById("lm.keeper");

        static StatueSocket Socket(string id)
        {
            foreach (var s in Object.FindObjectsByType<StatueSocket>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (s.id == id) return s;
            return null;
        }

        /// <summary>Tidemother frightened, no water shard, the lagoon risen (the temple door open), at the temple door.</summary>
        public static void Reset()
        {
            var saved = PuzzleState.ToSaved();
            saved.RemoveAll(p => p.id == "lm.keeper" || p.id == ShardHolder.WaterId || p.id == LilymereLagoon.RisenId);
            saved.Add(new SavedPuzzle { id = LilymereLagoon.RisenId, solved = true, stage = 2 });
            PuzzleState.Restore(saved);
            var g = Find<GameState>();
            if (g != null)
                foreach (var s in g.Spots)
                    if (s.label == "LM: temple") Debug.Log($"[Step11cDebug] Go to the temple: {g.GoTo(s)}.");
        }

        /// <summary>Into the temple through its door (open once the lagoon rose).</summary>
        public static void Enter()
        {
            var d = Find<TempleDoor>();
            if (d == null) return;
            Debug.Log($"[Step11cDebug] Temple door open {d.IsOpen}: entering.");
            d.DebugEnter();
        }

        public static void TakeWater()
        {
            var k = Hall;
            if (k != null) k.CollectShard("bridge");
        }

        /// <summary>The fire and wind shards already in the statue, the water shard held, in front of the statue.</summary>
        public static void WaterToHub()
        {
            var saved = PuzzleState.ToSaved();
            saved.RemoveAll(p => p.id == ShardHolder.FireId || p.id == ShardHolder.WindId || p.id == ShardHolder.WaterId);
            saved.Add(new SavedPuzzle { id = ShardHolder.FireId, solved = true, stage = 2 });
            saved.Add(new SavedPuzzle { id = ShardHolder.WindId, solved = true, stage = 2 });
            saved.Add(new SavedPuzzle { id = ShardHolder.WaterId, solved = false, stage = 1 });
            PuzzleState.Restore(saved);
            var socket = Socket(ShardHolder.WaterId);
            var swap = Find<SceneSwap>();
            var hub = AssetDatabase.LoadAssetAtPath<WorldMap>(StillIsleTools.MapPath);
            if (socket == null || swap == null || hub == null) return;
            if (SceneSwap.IsInside) swap.ReturnNow();
            var front = socket.statueAnchor + new Vector2Int(1, 4);
            swap.TravelNow(hub, front, new Vector2Int(0, -1));
            Debug.Log($"[Step11cDebug] Water shard held, two in the statue; hub at c{front.x} r{front.y}.");
        }

        public static void PlaceWater()
        {
            var s = Socket(ShardHolder.WaterId);
            if (s != null) s.DebugPlace();
        }

        public static void State()
        {
            var k = Hall;
            var look = Find<LakeTempleHall>();
            var door = Find<TempleDoor>();
            Debug.Log($"[Step11cDebug] Tidemother: calmed {(k != null && k.Calmed)}, model {(k != null && k.Model != null ? "shown" : "none")}, shard waiting {(k != null && k.ShardWaiting)}; " +
                      $"shard.water stage {PuzzleState.Stage(ShardHolder.WaterId)}{(PuzzleState.IsSolved(ShardHolder.WaterId) ? " placed" : "")}; dome held {(look != null ? look.Held : -1f):0.##}; " +
                      $"door open {(door != null && door.IsOpen)}; realm {RealmState.Restored:0.##}; temple gate shown {(k != null && k.realmMap != null ? (!k.realmMap.IsPropHidden(k.templeGate)).ToString() : "-")}; " +
                      $"statue placed {StatueSocket.PlacedCount}; battle {(Find<EncounterDirector>() is EncounterDirector e && e.CurrentBattle != null ? $"{e.CurrentBattle.wild.isling.Name} Lv {e.CurrentBattle.wild.isling.level} {e.CurrentBattle.wild.Hp}/{e.CurrentBattle.wild.MaxHp}" : "none")}");
        }
    }
}
