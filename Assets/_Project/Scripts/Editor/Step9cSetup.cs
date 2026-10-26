using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 9c: Hearthstag, the restored realm, the first shard. Runs the 9b setup first, then writes and bakes the
    /// Keeper, and wires the Forge Hall meeting and battle, the shard holder, the restoration wave, the temple return
    /// gate and the hub statue's shard socket. Safe to re-run.
    /// </summary>
    public static class Step9cSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";
        public const string ShotDir = "Logs/BattleShots";

        /// <summary>Writes, bakes and registers Hearthstag; turnaround shots calmed and frightened.</summary>
        public static IslingSpecies Keeper(StringBuilder log)
        {
            var species = KeeperKit.Write();
            if (species == null) return null;
            var report = CreatureBaker.Bake(species.recipe);
            Step7Setup.Registry();
            AssetDatabase.SaveAssets();
            log?.Append($"\n  Hearthstag baked: {report}");
            IslingShots.Turnaround(species, $"{ShotDir}/9c_hearthstag.png");
            IslingShots.Turnaround(species, $"{ShotDir}/9c_hearthstag_frightened.png", go =>
            {
                var look = go.AddComponent<KeeperLook>();
                look.calm = 0f;
                look.Paint();
            });
            return species;
        }

        public static void KeeperOnly()
        {
            var log = new StringBuilder("[Step9c] Keeper:");
            Keeper(log);
            Debug.Log(log.ToString());
        }

        [MenuItem("Tools/Pocket Isle/Set Up MR_Main (9c keeper)", priority = 55)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step9c] Stop Play mode first.");
                return;
            }
            Step9bSetup.SetUp(); // 9a map (with the temple gate) + the 9b puzzles
            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            var log = new StringBuilder("[Step9c] MR_Main set up:");
            var keeper = Keeper(log);
            var shardProps = PropKit.WriteShardProps();
            AssetDatabase.SaveAssets();
            foreach (var r in shardProps.Values)
            {
                var rep = PropBaker.Bake(r);
                log.Append($"\n  {r.name}: {rep.triangles} tris");
            }

            var cinder = AssetDatabase.LoadAssetAtPath<WorldMap>(CinderwoodTools.MapPath);
            var hub = AssetDatabase.LoadAssetAtPath<WorldMap>(StillIsleTools.MapPath);
            var hallMap = AssetDatabase.LoadAssetAtPath<WorldMap>(ForgeHallTools.HallPath);
            var island = Object.FindFirstObjectByType<IslandBuilder>(FindObjectsInactive.Include);
            var explorer = Object.FindFirstObjectByType<GestureTestPiece>(FindObjectsInactive.Include);
            var swap = Object.FindFirstObjectByType<SceneSwap>(FindObjectsInactive.Include);
            var fx = Object.FindFirstObjectByType<BattleFx>(FindObjectsInactive.Include);
            var ghosts = Object.FindFirstObjectByType<GhostHands>(FindObjectsInactive.Include);
            var beacon = Step7bSetup.BeaconMaterial();
            if (keeper == null || cinder == null || hub == null || hallMap == null || island == null || swap == null)
            {
                Debug.LogError("[Step9c] Missing the Keeper, a map or the island / scene swap (run the earlier setups).");
                return;
            }
            var rootGo = GameObject.Find("Hand Puzzles");
            var root = rootGo != null ? rootGo.transform : null;
            if (root == null)
            {
                Debug.LogError("[Step9c] No 'Hand Puzzles' (Set Up MR_Main (9b puzzles) failed?).");
                return;
            }

            // The Forge Hall's placeholder ring and line give way to Hearthstag.
            var forgeHall = Object.FindFirstObjectByType<ForgeHall>(FindObjectsInactive.Include);
            if (forgeHall != null)
            {
                Undo.RecordObject(forgeHall, "Step 9c");
                forgeHall.ringMaterial = null;
                forgeHall.firstLine = "";
                EditorUtility.SetDirty(forgeHall);
            }

            // The rim holder.
            var holder = Child<ShardHolder>(root, "Shard Holder");
            Undo.RecordObject(holder, "Step 9c");
            holder.island = island;
            holder.holderRecipe = shardProps["ShardHolder"];
            holder.fireShardRecipe = shardProps["FireShard"];
            holder.cups = PropKit.HolderCups;
            holder.metres = PropKit.ShardMetres * 1.25f; // a little bigger on the rim than the shard's real size
            holder.inset = new Vector2(0.12f, 0.06f);   // in from the footprint's cut corner
            EditorUtility.SetDirty(holder);

            // Hearthstag in the Forge Hall.
            var kh = Child<KeeperHall>(root, "Keeper Hall");
            Undo.RecordObject(kh, "Step 9c");
            kh.swap = swap;
            kh.hall = hallMap;
            kh.realmMap = cinder;
            kh.island = island;
            kh.explorer = explorer;
            kh.encounters = Object.FindFirstObjectByType<EncounterDirector>(FindObjectsInactive.Include);
            kh.stage = Object.FindFirstObjectByType<BattleStage>(FindObjectsInactive.Include);
            kh.fx = fx;
            kh.realm = Object.FindFirstObjectByType<RealmRestoration>(FindObjectsInactive.Include);
            kh.holder = holder;
            kh.save = Object.FindFirstObjectByType<GameSave>(FindObjectsInactive.Include);
            kh.ghosts = ghosts;
            kh.keeperSpecies = keeper;
            kh.keeperLevel = KeeperKit.Level;
            kh.beaconMaterial = beacon;
            kh.shardRecipe = shardProps["FireShard"];
            kh.keeperSpot = new Vector2(4.5f, 2.7f);
            kh.keeperPad = new Vector2Int(4, 3);
            kh.partyPad = new Vector2Int(4, 6);
            kh.hallEntry = ForgeHallTools.EntryTile;
            kh.waveOrigin = CinderwoodTools.Tile(CinderwoodTools.DoorXY + Vector2Int.right);
            kh.templeGate = CinderwoodTools.Tile(CinderwoodTools.TempleGateXY);
            EditorUtility.SetDirty(kh);

            // The hub statue's fire socket.
            var socket = Child<StatueSocket>(root, "Statue Socket");
            Undo.RecordObject(socket, "Step 9c");
            socket.map = hub;
            socket.island = island;
            socket.window = island.GetComponent<WorldWindow>();
            socket.explorer = explorer;
            socket.grabGuard = Object.FindFirstObjectByType<IslandHandle>(FindObjectsInactive.Include);
            socket.ghosts = ghosts;
            socket.fx = fx;
            socket.id = ShardHolder.FireId;
            socket.reach = 6;
            socket.lessonLine = "hub.shard.first";
            socket.ghostClip = "grab_drop";
            socket.holder = holder;
            socket.shardRecipe = shardProps["FireShard"];
            socket.ringMaterial = beacon;
            socket.socket = PropKit.StatueSockets[0] + new Vector3(0f, 0f, 0.03f);
            bool found = false;
            for (int r = 0; r < hub.Rows && !found; r++)
                for (int c = 0; c < hub.Columns && !found; c++)
                    if (hub.TryGetProp(hub.PropAt(r, c), out var e) && e.recipe != null && e.recipe.name == "Statue")
                    {
                        socket.statueAnchor = new Vector2Int(c, r);
                        socket.statueYaw = (e.turned ? 90f : 0f) + e.yaw;
                        socket.statueRecipe = e.recipe;
                        found = true;
                    }
            EditorUtility.SetDirty(socket);
            log.Append($"\n  'Shard Holder' on the rim (back-left), 'Keeper Hall' (Hearthstag Lv {kh.keeperLevel} on pad c{kh.keeperPad.x} r{kh.keeperPad.y}, " +
                       $"wave from c{kh.waveOrigin.x} r{kh.waveOrigin.y}, temple gate c{kh.templeGate.x} r{kh.templeGate.y}), " +
                       $"'Statue Socket' {(found ? $"at the statue c{socket.statueAnchor.x} r{socket.statueAnchor.y}" : "NO STATUE on the hub map")}");

            DialogueTools.Import();
            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }

        static T Child<T>(Transform root, string name) where T : Component
        {
            var t = root.Find(name);
            if (t == null)
            {
                var go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, name);
                go.transform.SetParent(root, false);
                t = go.transform;
            }
            var c = t.GetComponent<T>();
            if (c == null) c = Undo.AddComponent<T>(t.gameObject);
            return c;
        }
    }
}
