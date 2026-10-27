using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 11c A: the water roster (<see cref="WaterRosterKit"/>), baked, registered, turnaround shots
    /// (Logs/BattleShots/11c_&lt;name&gt;.png), a lineup with the Kilnpot and Pebbloop lines for scale, and Lilymere's tables
    /// rewritten with them (~70% water, ~20% wind visitors, the rare pond's Bottlewhale ~5%). Bridge:
    /// exec:Diorama.EditorTools.Step11cSetup.Roster (or a line: Marimoss, Drizzcan, Lilyvole, Sailfold, Bottlewhale).
    /// </summary>
    public static class Step11cSetup
    {
        [MenuItem("Tools/Pocket Isle/Set Up Water Roster (11c)", priority = 71)]
        public static void Roster()
        {
            Marimoss();
            Drizzcan();
            Lilyvole();
            Sailfold();
            Bottlewhale();
            Tables();
            Lineup();
        }

        static void Build(string what, params IslingSpecies[] species)
        {
            var log = new StringBuilder($"[Step11c] {what}:");
            foreach (var s in species)
            {
                if (s == null || s.recipe == null)
                {
                    log.Append("\n  MISSING species");
                    continue;
                }
                var r = CreatureBaker.Bake(s.recipe);
                log.Append($"\n  {s.name}: {r}");
                IslingShots.Turnaround(s, $"{Step9cSetup.ShotDir}/11c_{s.name.ToLowerInvariant()}.png");
            }
            Step7Setup.Registry();
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }

        static void Line(string what, IslingSpecies s) => Build(what, s, s != null ? s.evolvesTo : null);

        public static void Marimoss() => Line("Marimoss -> Marimound", WaterRosterKit.Marimoss());
        public static void Drizzcan() => Line("Drizzcan -> Rainurn", WaterRosterKit.Drizzcan());
        public static void Lilyvole() => Line("Lilyvole -> Lotusvole", WaterRosterKit.Lilyvole());
        public static void Sailfold() => Line("Sailfold -> Galleyfold", WaterRosterKit.Sailfold());
        public static void Bottlewhale() => Line("Bottlewhale -> Lighthousewhale (rare)", WaterRosterKit.Bottlewhale());

        /// <summary>Lilymere's tables with the water roster (rewrites the map's tables; layout unchanged, same seed).</summary>
        public static void Tables()
        {
            var log = new StringBuilder("[Step11c] Lilymere tables:");
            var map = LilymereTools.Write(log);
            if (map == null) return;
            var problems = LilymereTools.Check(map, log);
            foreach (var p in problems) log.Append($"\n  PROBLEM: {p}");
            EditorUtility.SetDirty(map);
            AssetDatabase.SaveAssets();
            IslandBuilder.RebuildAll();
            DialogueTools.Import();
            log.Append("\n  Script_v1 reimported (lm.meet.sailfold, lm.meet.bottlewhale)");
            Debug.Log(log.ToString());
        }

        // ================= B: Tidemother =================

        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";
        const string TidemotherPath = "Assets/_Project/Data/Creatures/Species/Tidemother.asset";

        /// <summary>Everything for 11c B: Tidemother's model, the temple hall and the Keeper, the water shard and socket, the door.</summary>
        [MenuItem("Tools/Pocket Isle/Set Up MR_Main (11c Tidemother)", priority = 72)]
        public static void Keeper()
        {
            Tidemother();
            Temple();
        }

        /// <summary>Tidemother's model and species, baked, registered, a turnaround (Logs/BattleShots/11c_tidemother.png).</summary>
        public static void Tidemother()
        {
            var s = TidemotherKit.Write();
            if (s == null) return;
            var r = CreatureBaker.Bake(s.recipe);
            IslingShots.Turnaround(s, $"{Step9cSetup.ShotDir}/11c_tidemother.png");
            Step7Setup.Registry();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Step11c] Tidemother: {r}");
        }

        /// <summary>
        /// The lake temple hall (<see cref="LakeTempleTools"/>), Lilymere's map with the temple's return gate and its link
        /// home, "Hand Puzzles/Lilymere": Tidemother's <see cref="KeeperHall"/> (fields shared with Hearthstag's copied),
        /// the doors out (<see cref="ForgeHall"/>), the water dome (<see cref="LakeTempleHall"/>), the temple door in
        /// (<see cref="TempleDoor"/>), the water shard with the Spirit and the statue's water socket. Safe to re-run.
        /// </summary>
        public static void Temple()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step11c] Stop Play mode first.");
                return;
            }
            var log = new StringBuilder("[Step11c] Tidemother:");
            var lilymere = LilymereTools.Write(log);
            if (lilymere == null) return;
            foreach (var p in LilymereTools.Check(lilymere, log)) Debug.LogError($"[Step11c] Lilymere layout check: {p}");
            var hub = AssetDatabase.LoadAssetAtPath<WorldMap>(StillIsleTools.MapPath);
            if (hub != null) Step8Setup.LinkLilymere(hub, lilymere, log);
            var hallMap = LakeTempleTools.WriteHall(log);
            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            var top = GameObject.Find("Hand Puzzles");
            var root = top != null ? top.transform.Find("Lilymere") : null;
            var species = AssetDatabase.LoadAssetAtPath<IslingSpecies>(TidemotherPath);
            KeeperHall hearth = null;
            foreach (var k in Object.FindObjectsByType<KeeperHall>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (k.keeperId == KeeperHall.KeeperId) hearth = k;
            if (root == null || species == null || hearth == null || hallMap == null)
            {
                Debug.LogError("[Step11c] Missing Hand Puzzles/Lilymere (Set Up MR_Main (11b Lilymere hands)), Tidemother (run Tidemother first) or the Hearthstag Keeper Hall.");
                return;
            }

            // The water shard (with the Spirit, after the fire and wind shards on the circle).
            var waterShard = PropKit.WriteWaterShardProp();
            AssetDatabase.SaveAssets();
            log.Append($"\n  WaterShard: {PropBaker.Bake(waterShard).triangles} tris");
            var holder = hearth.holder;
            if (holder != null)
            {
                Undo.RecordObject(holder, "Step 11c");
                var list = new List<ShardHolder.Shard>();
                if (holder.otherShards != null)
                    foreach (var sh in holder.otherShards)
                        if (sh.id != ShardHolder.WaterId) list.Add(sh);
                list.Add(new ShardHolder.Shard { id = ShardHolder.WaterId, recipe = waterShard });
                holder.otherShards = list.ToArray();
                EditorUtility.SetDirty(holder);
            }

            var doors = Child<ForgeHall>(root, "Lake Temple Hall");
            doors.swap = hearth.swap;
            doors.hall = hallMap;
            doors.island = hearth.island;
            doors.explorer = hearth.explorer;
            doors.ringMaterial = null;
            doors.firstLine = "";
            doors.doorTiles = System.Array.ConvertAll(LakeTempleTools.DoorTiles, t => (Vector2)t);
            doors.keeperSpot = LakeTempleTools.KeeperSpot;
            EditorUtility.SetDirty(doors);

            var kh = Child<KeeperHall>(root, "Keeper Hall (Tidemother)");
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
            kh.keeperId = "lm.keeper";
            kh.realmName = "Lilymere";
            kh.keeperTarget = "tidemother";
            kh.shardTarget = "waterShard";
            kh.captionFormat = "{0} churns up the water!";
            kh.shardColor = new Color(0.45f, 0.72f, 1f);
            kh.ownShard = ShardHolder.WaterId;
            kh.keeperSpecies = species;
            kh.keeperLevel = TidemotherKit.Level;
            kh.shardRecipe = waterShard;
            kh.hall = hallMap;
            kh.realmMap = lilymere;
            kh.keeperSpot = LakeTempleTools.KeeperSpot;
            kh.keeperPad = new Vector2Int(4, 3);
            kh.partyPad = new Vector2Int(4, 7);
            kh.hallEntry = LakeTempleTools.EntryTile;
            kh.forgeMouth = LakeTempleTools.DomeAt;
            kh.forgeMouthHeight = 1.2f;
            kh.forgeFlame = false;
            kh.stomps = false;
            kh.keeperLift = 0f;
            kh.restsWhenCalm = true;
            kh.hoots = false;
            kh.approachRestart = 2.5f;
            kh.untangle = null;
            var temple = LilymereTools.Tile(LilymereTools.TempleXY);
            kh.waveOrigin = temple + new Vector2(1f, 0.5f);
            kh.waveSeconds = 5f;
            kh.templeGate = LilymereTools.Tile(LilymereTools.TempleGateXY);
            kh.meetLine = "lm.keeper.meet";
            kh.shieldLine = "lm.keeper.shield";
            kh.brokenLine = "lm.keeper.shield.break";
            kh.phase2Line = "keeper.phase2";
            kh.winLine = "lm.keeper.win";
            kh.shardLine = "lm.shard.take";
            kh.restoredLine = "lm.restored";
            // Frightened: grey-teal skin, a churning grey-teal aura with spray, worried red-teal eyes; the Lily Shell is lily green.
            kh.frightenedFur = new Color(0.46f, 0.58f, 0.58f);
            kh.frightenedBelly = new Color(0.74f, 0.76f, 0.72f);
            kh.smokeLight = new Color(0.62f, 0.76f, 0.78f, 0.5f);
            kh.smokeDark = new Color(0.3f, 0.42f, 0.46f, 0.72f);
            kh.sparks = new Color(0.8f, 0.95f, 1f);
            kh.eyeFrightened = new Color(0.9f, 0.32f, 0.36f, 0.9f);
            kh.glowColor = new Color(0.6f, 0.95f, 0.85f);
            kh.shellCore = new Color(0.86f, 1f, 0.84f);
            kh.shellEdge = new Color(0.4f, 0.75f, 0.5f);
            EditorUtility.SetDirty(kh);

            var look = Child<LakeTempleHall>(root, "Lake Temple Hall");
            look.swap = hearth.swap;
            look.hall = hallMap;
            look.island = hearth.island;
            look.fx = hearth.fx;
            look.domeRecipe = PropKit.Load("WaterDome");
            look.domeAt = LakeTempleTools.DomeAt;
            look.doorAt = new Vector2(4.5f, 9f);
            look.keeperId = kh.keeperId;
            look.keeper = kh;
            EditorUtility.SetDirty(look);

            var oldDoor = root.Find("Temple Door");
            if (oldDoor != null) GameObjectUtility.RemoveMonoBehavioursWithMissingScript(oldDoor.gameObject);
            var door = Child<TempleDoor>(root, "Temple Door");
            door.map = lilymere;
            door.island = hearth.island;
            door.explorer = hearth.explorer;
            door.swap = hearth.swap;
            door.fx = hearth.fx;
            door.glowMaterial = Step7bSetup.BeaconMaterial();
            door.openWhen = LilymereLagoon.RisenId;
            door.front = new[] { LilymereTools.TempleFront };
            door.doorway = new Vector3(temple.x + 1f, 0.7f, temple.y + 1.45f);
            door.hall = hallMap;
            door.hallEntry = LakeTempleTools.EntryTile;
            EditorUtility.SetDirty(door);
            log.Append($"\n  Keeper Hall (Tidemother): Lv {kh.keeperLevel}, wave from the temple c{kh.waveOrigin.x} r{kh.waveOrigin.y}, return gate c{kh.templeGate.x} r{kh.templeGate.y}; " +
                       $"Temple Door front c{LilymereTools.TempleFront.x} r{LilymereTools.TempleFront.y} opens with {door.openWhen}; the water dome lets go on victory");

            // The hub statue's water socket: the fire socket's wiring on the water socket, blue.
            StatueSocket fireSocket = null;
            foreach (var st in Object.FindObjectsByType<StatueSocket>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (st.id == ShardHolder.FireId) fireSocket = st;
            if (fireSocket != null)
            {
                var water = Child<StatueSocket>(fireSocket.transform.parent, "Statue Socket (water)");
                water.id = ShardHolder.WaterId;
                water.map = fireSocket.map;
                water.island = fireSocket.island;
                water.window = fireSocket.window;
                water.explorer = fireSocket.explorer;
                water.grabGuard = fireSocket.grabGuard;
                water.ghosts = fireSocket.ghosts;
                water.fx = fireSocket.fx;
                water.reach = fireSocket.reach;
                water.lessonLine = fireSocket.lessonLine;
                water.ghostClip = fireSocket.ghostClip;
                water.holder = holder;
                water.shardRecipe = waterShard;
                water.statueRecipe = fireSocket.statueRecipe;
                water.statueAnchor = fireSocket.statueAnchor;
                water.statueYaw = fireSocket.statueYaw;
                water.ringMaterial = fireSocket.ringMaterial;
                water.focusMaterial = fireSocket.focusMaterial;
                water.socket = PropKit.StatueSockets[1] + new Vector3(0f, 0f, 0.03f);
                water.rimSlot = PropSlot.Wood;
                water.shardColor = new Color(0.4f, 0.65f, 1f);
                water.drawsStatue = false;
                water.targetName = "statueSocketWater";
                EditorUtility.SetDirty(water);
                log.Append("\n  Statue Socket (water): blue rim; three placed says statue.three");
            }
            else log.Append("\n  MISSING the fire statue socket");

            var game = Object.FindFirstObjectByType<GameState>(FindObjectsInactive.Include);
            if (game != null)
            {
                Undo.RecordObject(game, "Step 11c");
                game.lakeTemple = hallMap;
                game.lakeTempleEntry = LakeTempleTools.EntryTile;
                EditorUtility.SetDirty(game);
                log.Append("\n  Dev panel: Go to LM: lake temple, Story tab Tidemother / Water shard");
            }
            DialogueTools.Import();
            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            AssetDatabase.SaveAssets();
            IslandBuilder.RebuildAll();
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
            Undo.RecordObject(c, "Step 11c");
            return c;
        }

        static readonly string[] WaterCommons = { "Marimoss", "Drizzcan", "Lilyvole", "Sailfold" };

        /// <summary>
        /// Tidemother (Lv 9 at 0 shards, +2 per other shard) against parties of three: the starter, a Wind helper (Kitelet:
        /// wind lifts the Lily Shell) and a random water common. Target 60-75% at 0 shards; 2 shards is the usual arrival.
        /// </summary>
        public static void Sim()
        {
            var keeper = KeeperSim.Load("Tidemother");
            var helper = KeeperSim.Load("Kitelet");
            if (keeper == null || helper == null)
            {
                Debug.LogError("[Step11c] Tidemother or Kitelet missing.");
                return;
            }
            var commons = new List<IslingSpecies>();
            foreach (var n in WaterCommons)
            {
                var c = KeeperSim.Load(n);
                if (c != null) commons.Add(c);
            }
            var chart = KeeperSim.Chart;
            var log = new StringBuilder($"[Step11c] Tidemother sim (HP base {keeper.baseStats.hp}, atk {keeper.baseStats.attack}, def {keeper.baseStats.defense}); party = starter + Kitelet + a water common:\n");
            log.Append("| Shards | Tidemother Lv | Party Lv | Kilnpot | Pebbloop | Pufflet | All |\n|---|---|---|---|---|---|---|\n");
            var rnd = new SeededRandom(7);
            foreach (int shards in new[] { 0, 2 })
            {
                int kl = ShardScaling.KeeperLevel(TidemotherKit.Level, shards, ShardScaling.KeeperPerShard);
                foreach (int lv in shards == 0 ? new[] { 8, 9, 10 } : new[] { 12, 13, 14 })
                {
                    int f = 0, w = 0;
                    log.Append($"| {shards} | {kl} | {lv} |");
                    foreach (var starterName in KeeperSim.Starters)
                    {
                        var starter = KeeperSim.Load(starterName);
                        int wins = 0;
                        const int fights = 500;
                        for (int i = 0; i < fights; i++)
                        {
                            var common = commons.Count > 0 ? commons[(int)(rnd.Next() * commons.Count) % commons.Count] : helper;
                            var party = new List<IslingInstance> { new IslingInstance(starter, lv), new IslingInstance(helper, lv), new IslingInstance(common, lv) };
                            if (KeeperSim.Fight(party, new IslingInstance(keeper, kl), chart, 13 + i, out _) == BattleResult.Won) wins++;
                        }
                        f += fights;
                        w += wins;
                        log.Append($" {wins / (float)fights:P0} |");
                    }
                    log.Append($" {w / (float)Mathf.Max(1, f):P0} |\n");
                }
            }
            Debug.Log(log.ToString());
        }

        /// <summary>The water Islings in a row with the Kilnpot and Pebbloop lines for scale: Logs/BattleShots/11c_lineup.png.</summary>
        public static void Lineup()
        {
            var names = new[] { "Kilnpot", "Kilnhearth", "Pebbloop", "Riverloom", "Marimoss", "Marimound", "Drizzcan", "Rainurn", "Lilyvole", "Lotusvole",
                "Sailfold", "Galleyfold", "Bottlewhale", "Lighthousewhale" };
            var preview = EditorSceneManager.NewPreviewScene();
            const int w = 2400, h = 700;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            try
            {
                var camGo = new GameObject("Lineup Camera");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camGo, preview);
                var cam = camGo.AddComponent<Camera>();
                cam.scene = preview;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.8f, 0.82f, 0.83f);
                cam.orthographic = true;
                cam.stereoTargetEye = StereoTargetEyeMask.None;
                var lightGo = new GameObject("Lineup Light");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightGo, preview);
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                lightGo.transform.rotation = Quaternion.LookRotation(-new Vector3(-0.45f, 0.85f, 0.5f).normalized);
                float x = 0f, top = 0f;
                var placed = new List<GameObject>();
                foreach (var n in names)
                {
                    var s = AssetDatabase.LoadAssetAtPath<IslingSpecies>($"Assets/_Project/Data/Creatures/Species/{n}.asset");
                    if (s == null || s.Prefab == null) continue;
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(s.Prefab, preview);
                    go.transform.localScale = Vector3.one * 8f;
                    go.transform.rotation = Quaternion.Euler(0f, -25f, 0f);
                    go.GetComponent<CreatureView>().ApplyStyles();
                    var b = new Bounds(go.transform.position, Vector3.zero);
                    foreach (var r in go.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
                    go.transform.position = new Vector3(x + b.extents.x - (b.center.x - go.transform.position.x), -b.min.y, 0f);
                    x += b.size.x + 0.08f;
                    top = Mathf.Max(top, b.size.y);
                    placed.Add(go);
                }
                float width = x;
                cam.orthographicSize = Mathf.Max(top * 0.6f, width / (2f * w / h));
                cam.transform.position = new Vector3(width * 0.5f, cam.orthographicSize * 0.92f, 4f);
                cam.transform.rotation = Quaternion.LookRotation(Vector3.back);
                var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
                cam.targetTexture = rt;
                cam.Render();
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                RenderTexture.active = prev;
                cam.targetTexture = null;
                rt.Release();
                Object.DestroyImmediate(rt);
                Directory.CreateDirectory(Step9cSetup.ShotDir);
                File.WriteAllBytes($"{Step9cSetup.ShotDir}/11c_lineup.png", tex.EncodeToPNG());
                Debug.Log($"[Step11c] Lineup of {placed.Count}: {Step9cSetup.ShotDir}/11c_lineup.png");
            }
            finally
            {
                Object.DestroyImmediate(tex);
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }
    }
}
