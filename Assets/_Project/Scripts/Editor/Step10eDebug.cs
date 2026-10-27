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
                sb.Append($"temple gate shown {(Find<KeeperHall>() is KeeperHall kh && kh.realmMap != null ? (!kh.realmMap.IsPropHidden(kh.templeGate)).ToString() : "-")}.");
            Debug.Log(sb.ToString());
        }
    }
}
