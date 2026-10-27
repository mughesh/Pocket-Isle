using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 10e: Galeowl's tower hall. <see cref="Hall"/> writes and bakes the hall (<see cref="TowerHallTools"/>) and wires
    /// "Hand Puzzles/Galewick/Tower Hall" (the doors out: <see cref="ForgeHall"/>; the window sky: <see cref="TowerHall"/>)
    /// and the tower door into it. Safe to re-run. Bridge: exec:Diorama.EditorTools.Step10eSetup.Hall
    /// </summary>
    public static class Step10eSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";

        [MenuItem("Tools/Pocket Isle/Setup/Galewick (10)/Set Up MR_Main (10e tower hall)", priority = 704)]
        public static void Hall()
        {
            if (!Ready()) return;
            var log = new StringBuilder("[Step10e] Tower hall:");
            var hallMap = TowerHallTools.WriteHall(log);
            var root = GalewickRoot();
            var island = Object.FindFirstObjectByType<IslandBuilder>(FindObjectsInactive.Include);
            var swap = Object.FindFirstObjectByType<SceneSwap>(FindObjectsInactive.Include);
            var explorer = Object.FindFirstObjectByType<GestureTestPiece>(FindObjectsInactive.Include);
            if (root == null || island == null || swap == null || explorer == null)
            {
                Debug.LogError("[Step10e] Missing 'Hand Puzzles/Galewick', the island, scene swap or explorer (Set Up MR_Main (10b Galewick hands)).");
                return;
            }

            var doors = Child<ForgeHall>(root, "Tower Hall");
            doors.swap = swap;
            doors.hall = hallMap;
            doors.island = island;
            doors.explorer = explorer;
            doors.ringMaterial = null;
            doors.firstLine = "";
            doors.doorTiles = System.Array.ConvertAll(TowerHallTools.DoorTiles, t => (Vector2)t);
            doors.keeperSpot = TowerHallTools.KeeperSpot;
            EditorUtility.SetDirty(doors);

            var look = Child<TowerHall>(root, "Tower Hall");
            look.swap = swap;
            look.hall = hallMap;
            look.island = island;
            look.skyRecipe = PropKit.Load("TowerSky");
            look.wispRecipe = PropKit.Load("TowerWisp");
            look.windowAnchor = TowerHallTools.WindowAnchor;
            EditorUtility.SetDirty(look);

            var door = root.GetComponentInChildren<TowerDoor>(true);
            if (door != null)
            {
                Undo.RecordObject(door, "Step 10e");
                door.hall = hallMap;
                door.hallEntry = TowerHallTools.EntryTile;
                EditorUtility.SetDirty(door);
                log.Append($"\n  'Tower Door' leads in (entry c{door.hallEntry.x} r{door.hallEntry.y}); 'Tower Hall' doors c{TowerHallTools.DoorTiles[0].x}-{TowerHallTools.DoorTiles[1].x} r{TowerHallTools.DoorTiles[0].y} lead back out");
            }
            else log.Append("\n  MISSING 'Tower Door' (Set Up MR_Main (10b Galewick hands))");
            Save(log);
        }

        const string GaleowlPath = "Assets/_Project/Data/Creatures/Species/Galeowl.asset";
        const string KiteStringPath = "Assets/_Project/Materials/Props/KiteString.mat";

        /// <summary>Everything for 10e: the hall, then Galeowl, the knots, the wind shard and the statue's wind socket.</summary>
        [MenuItem("Tools/Pocket Isle/Setup/Galewick (10)/Set Up MR_Main (10e Galeowl)", priority = 705)]
        public static void All()
        {
            Hall();
            Keeper();
        }

        /// <summary>
        /// Galeowl's Keeper hall (a second <see cref="KeeperHall"/>, its fields copied from Hearthstag's where shared), the
        /// knots (<see cref="KeeperUntangle"/>), the window clearing, the wind shard with the Spirit and the hub statue's
        /// wind socket. Bridge: exec:Diorama.EditorTools.Step10eSetup.Keeper
        /// </summary>
        public static void Keeper()
        {
            if (!Ready()) return;
            var log = new StringBuilder("[Step10e] Galeowl:");
            var root = GalewickRoot();
            var hallMap = AssetDatabase.LoadAssetAtPath<WorldMap>(TowerHallTools.HallPath);
            var galewick = AssetDatabase.LoadAssetAtPath<WorldMap>(GalewickTools.MapPath);
            var species = AssetDatabase.LoadAssetAtPath<IslingSpecies>(GaleowlPath);
            KeeperHall hearth = null;
            foreach (var k in Object.FindObjectsByType<KeeperHall>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (k.keeperId == KeeperHall.KeeperId) hearth = k;
            var look = root != null ? root.GetComponentInChildren<TowerHall>(true) : null;
            if (root == null || hallMap == null || galewick == null || species == null || hearth == null || look == null)
            {
                Debug.LogError("[Step10e] Missing the Galewick root, the tower hall (run Hall first), the Galewick map, Galeowl (Write Galeowl (10d part one)) or Hearthstag's Keeper Hall.");
                return;
            }

            // The wind shard (with the Spirit, after the fire shard on the circle).
            var windShard = PropKit.WriteWindShardProp();
            AssetDatabase.SaveAssets();
            log.Append($"\n  WindShard: {PropBaker.Bake(windShard).triangles} tris");
            var holder = hearth.holder;
            if (holder != null)
            {
                Undo.RecordObject(holder, "Step 10e");
                holder.otherShards = new[] { new ShardHolder.Shard { id = ShardHolder.WindId, recipe = windShard } };
                EditorUtility.SetDirty(holder);
            }

            var kh = Child<KeeperHall>(root, "Keeper Hall (Galeowl)");
            // Shared with Hearthstag.
            kh.swap = hearth.swap;
            kh.island = hearth.island;
            kh.explorer = hearth.explorer;
            kh.encounters = hearth.encounters;
            kh.stage = hearth.stage;
            kh.fx = hearth.fx;
            kh.realm = hearth.realm;
            kh.holder = holder;
            kh.save = hearth.save;
            kh.ghosts = hearth.ghosts;
            kh.beaconMaterial = hearth.beaconMaterial;
            kh.smokeMaterial = hearth.smokeMaterial;
            kh.spirit = hearth.spirit;
            // Galeowl's own.
            kh.keeperId = "gw.keeper";
            kh.realmName = "Galewick";
            kh.keeperTarget = "galeowl";
            kh.shardTarget = "windShard";
            kh.captionFormat = "{0} whips up a storm!";
            kh.shardColor = Step12bSetup.WindGreen;
            kh.ownShard = ShardHolder.WindId;
            kh.keeperSpecies = species;
            kh.keeperLevel = GaleowlKit.Level;
            kh.shardRecipe = windShard;
            kh.hall = hallMap;
            kh.realmMap = galewick;
            kh.keeperSpot = TowerHallTools.KeeperSpot;
            kh.keeperPad = new Vector2Int(4, 3);
            kh.partyPad = new Vector2Int(4, 6);
            kh.hallEntry = TowerHallTools.EntryTile;
            kh.forgeMouth = TowerHallTools.WindowAnchor + new Vector2(0.5f, 0.15f);
            kh.forgeMouthHeight = 1f;
            kh.forgeFlame = false;
            kh.stomps = false;
            kh.keeperLift = TowerHallTools.PerchHeight;
            kh.restsWhenCalm = false;
            kh.hoots = true;
            kh.approachRestart = 2.5f;
            kh.waveOrigin = GalewickTools.Tile(GalewickTools.TowerXY) + new Vector2(0.5f, 0.5f);
            kh.waveSeconds = 5f;
            kh.templeGate = GalewickTools.Tile(GalewickTools.TowerGateXY);
            kh.meetLine = "gw.keeper.meet";
            kh.shieldLine = "gw.keeper.shield";
            kh.brokenLine = "gw.keeper.shield.break";
            kh.phase2Line = "keeper.phase2";
            kh.winLine = "gw.keeper.win";
            kh.shardLine = "gw.shard.take";
            kh.restoredLine = "gw.restored";
            // Frightened: storm grey, a pale grey storm aura, red-gold eyes; the Gust Wall is airy cyan.
            kh.frightenedFur = new Color(0.42f, 0.46f, 0.52f);
            kh.frightenedBelly = new Color(0.7f, 0.73f, 0.77f);
            kh.smokeLight = new Color(0.66f, 0.7f, 0.76f, 0.5f);
            kh.smokeDark = new Color(0.34f, 0.37f, 0.43f, 0.72f);
            kh.sparks = new Color(0.86f, 0.94f, 1f);
            kh.eyeFrightened = new Color(1f, 0.36f, 0.08f, 0.9f);
            kh.glowColor = new Color(0.62f, 0.9f, 1f);
            kh.shellCore = new Color(0.82f, 0.96f, 1f);
            kh.shellEdge = new Color(0.45f, 0.76f, 0.95f);
            EditorUtility.SetDirty(kh);

            var knots = Child<KeeperUntangle>(root, "Keeper Untangle");
            knots.id = "gw.knots";
            knots.map = hallMap;
            knots.island = hearth.island;
            knots.window = hearth.island.GetComponent<WorldWindow>();
            knots.explorer = hearth.explorer;
            knots.grabGuard = Object.FindFirstObjectByType<IslandHandle>(FindObjectsInactive.Include);
            knots.ghosts = hearth.ghosts;
            knots.fx = hearth.fx;
            knots.reach = 12;
            knots.lessonLine = "gw.keeper.knots";
            knots.lessonChip = "Pull apart!";
            knots.ghostClip = "pinch_pull";
            knots.keeper = kh;
            knots.swap = hearth.swap;
            knots.stringMaterial = AssetDatabase.LoadAssetAtPath<Material>(KiteStringPath);
            EditorUtility.SetDirty(knots);
            kh.untangle = knots;

            Undo.RecordObject(look, "Step 10e");
            look.keeper = kh;
            look.keeperId = kh.keeperId;
            EditorUtility.SetDirty(look);
            log.Append($"\n  'Keeper Hall (Galeowl)': Galeowl Lv {kh.keeperLevel} on its perch (lift {kh.keeperLift} tiles), wave from the tower c{kh.waveOrigin.x} r{kh.waveOrigin.y}, " +
                       $"return gate c{kh.templeGate.x} r{kh.templeGate.y}; 'Keeper Untangle' gw.knots (3 knots, ghost pinch_pull); the window clears on victory");

            // The hub statue's wind socket: the fire socket's wiring on the wind socket, teal.
            StatueSocket fireSocket = null;
            foreach (var s in Object.FindObjectsByType<StatueSocket>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (s.id == ShardHolder.FireId) fireSocket = s;
            if (fireSocket != null)
            {
                var wind = Child<StatueSocket>(fireSocket.transform.parent, "Statue Socket (wind)");
                wind.id = ShardHolder.WindId;
                wind.map = fireSocket.map;
                wind.island = fireSocket.island;
                wind.window = fireSocket.window;
                wind.explorer = fireSocket.explorer;
                wind.grabGuard = fireSocket.grabGuard;
                wind.ghosts = fireSocket.ghosts;
                wind.fx = fireSocket.fx;
                wind.reach = fireSocket.reach;
                wind.lessonLine = fireSocket.lessonLine;
                wind.ghostClip = fireSocket.ghostClip;
                wind.holder = holder;
                wind.shardRecipe = windShard;
                wind.statueRecipe = fireSocket.statueRecipe;
                wind.statueAnchor = fireSocket.statueAnchor;
                wind.statueYaw = fireSocket.statueYaw;
                wind.ringMaterial = fireSocket.ringMaterial;
                wind.socket = PropKit.StatueSockets[2] + new Vector3(0f, 0f, 0.03f);
                wind.rimSlot = PropSlot.Accent;
                wind.shardColor = Step12bSetup.WindGreen;
                wind.drawsStatue = false;
                wind.targetName = "statueSocketWind";
                EditorUtility.SetDirty(wind);
                if (fireSocket.statueRecipe != null && fireSocket.statueRecipe.palette != null && fireSocket.statueRecipe.palette.Length > 5)
                {
                    fireSocket.statueRecipe.palette[(int)PropSlot.Accent] = PropKit.StatueWindRim;
                    EditorUtility.SetDirty(fireSocket.statueRecipe);
                }
                log.Append($"\n  'Statue Socket (wind)': green rim, at the statue c{wind.statueAnchor.x} r{wind.statueAnchor.y}");
            }
            else log.Append("\n  MISSING the fire statue socket (Set Up MR_Main (9c keeper))");

            var game = Object.FindFirstObjectByType<GameState>(FindObjectsInactive.Include);
            if (game != null)
            {
                Undo.RecordObject(game, "Step 10e");
                game.towerHall = hallMap;
                game.towerHallEntry = TowerHallTools.EntryTile;
                EditorUtility.SetDirty(game);
                log.Append("\n  Dev panel: Go to GW: tower hall, Story tab Galewick beats");
            }
            knots.ghostScale = 0.85f;

            DialogueTools.Import();
            log.Append("\n  Script_v1 reimported");
            Save(log);
        }

        /// <summary>Oct 27 (Mugesh): the dev panel's grab frame was too thin. Wider band, a visible frame, deeper reach.</summary>
        public static void PanelGrab()
        {
            if (!Ready()) return;
            var panel = Object.FindFirstObjectByType<DevPanel>(FindObjectsInactive.Include);
            if (panel == null)
            {
                Debug.LogError("[Step10e] No DevPanel in MR_Main.");
                return;
            }
            Undo.RecordObject(panel, "Panel grab");
            panel.frame = 0.018f;
            panel.grabOut = 0.05f;
            panel.grabIn = 0.03f;
            panel.grabDepth = 0.08f;
            EditorUtility.SetDirty(panel);
            Save(new StringBuilder("[Step10e] Dev panel grab: frame 1.8 cm, band 5 cm out / 3 cm in, 8 cm deep, empty spots grab too."));
        }

        static bool Ready()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step10e] Stop Play mode first.");
                return false;
            }
            var active = EditorSceneManager.GetActiveScene();
            if (active.path == ScenePath) return true;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
            EditorSceneManager.OpenScene(ScenePath);
            return true;
        }

        static void Save(StringBuilder log)
        {
            var active = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            AssetDatabase.SaveAssets();
            IslandBuilder.RebuildAll();
            Debug.Log(log.ToString());
        }

        static Transform GalewickRoot()
        {
            var top = GameObject.Find("Hand Puzzles");
            return top != null ? top.transform.Find("Galewick") : null;
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
            Undo.RecordObject(c, "Step 10e");
            return c;
        }
    }
}
