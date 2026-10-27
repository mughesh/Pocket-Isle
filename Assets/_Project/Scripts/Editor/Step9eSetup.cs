using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 9e: the dev panel, Hearthstag's fury, the Keeper balance and the small fixes. Each section has its own
    /// method (bridge: exec:Diorama.EditorTools.Step9eSetup.X); <see cref="All"/> runs them in order. Safe to re-run.
    /// </summary>
    public static class Step9eSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";

        [MenuItem("Tools/Pocket Isle/Setup/Cinderwood + Hub (9, 10f)/Set Up MR_Main (9e dev panel and fury)", priority = 603)]
        public static void All()
        {
            var log = new StringBuilder("[Step9e] Set up:");
            Panel(log);
            Refine(log);
            Fury(log);
            Balance(log);
            Fixes(log);
            Save(log);
        }

        /// <summary>Hearthstag's battle numbers (<see cref="KeeperKit"/> constants) on the species and the Keeper Hall.</summary>
        public static void Balance(StringBuilder log)
        {
            var keeper = AssetDatabase.LoadAssetAtPath<IslingSpecies>("Assets/_Project/Data/Creatures/Species/Hearthstag.asset");
            if (keeper == null)
            {
                log.Append("\n  MISSING Hearthstag (run Set Up MR_Main (9c keeper))");
                return;
            }
            Undo.RecordObject(keeper, "Step 9e");
            keeper.baseStats.hp = KeeperKit.KeeperHp;
            keeper.baseStats.attack = KeeperKit.KeeperAttack;
            keeper.baseStats.defense = KeeperKit.KeeperDefense;
            EditorUtility.SetDirty(keeper);
            var hall = KeeperHall.FindById(KeeperHall.KeeperId);
            if (hall != null)
            {
                Undo.RecordObject(hall, "Step 9e");
                hall.keeperLevel = KeeperKit.Level;
                EditorUtility.SetDirty(hall);
            }
            var k = new IslingInstance(keeper, KeeperKit.Level);
            log.Append($"\n  Hearthstag Lv {KeeperKit.Level}: HP {k.MaxHp}, atk {k.Attack}, def {k.Defense}, spd {k.Speed}{(hall == null ? " (no Keeper Hall in the open scene)" : "")}");
        }

        public const string SmokeMaterialPath = "Assets/_Project/Materials/Battle/KeeperSmoke.mat";

        /// <summary>A: the "Dev Panel" object (GameState + DevPanel) and its references.</summary>
        public static void Panel(StringBuilder log)
        {
            T Find<T>() where T : Object => Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
            var state = Find<GameState>();
            GameObject go;
            if (state != null) go = state.gameObject;
            else
            {
                go = new GameObject("Dev Panel");
                Undo.RegisterCreatedObjectUndo(go, "Step 9e");
                state = go.AddComponent<GameState>();
            }
            var panel = go.GetComponent<DevPanel>();
            if (panel == null) panel = go.AddComponent<DevPanel>();
            Undo.RecordObject(state, "Step 9e");
            Undo.RecordObject(panel, "Step 9e");
            state.party = Find<Party>();
            state.explorer = Find<GestureTestPiece>();
            state.swap = Find<SceneSwap>();
            state.save = Find<GameSave>();
            state.encounters = Find<EncounterDirector>();
            state.realm = Find<RealmRestoration>();
            state.onboarding = Find<Onboarding>();
            state.registry = state.save != null ? state.save.registry : null;
            state.hub = AssetDatabase.LoadAssetAtPath<WorldMap>("Assets/_Project/Data/WorldMap_StillIsle.asset");
            state.cinderwood = AssetDatabase.LoadAssetAtPath<WorldMap>(CinderwoodTools.MapPath);
            state.forgeHall = AssetDatabase.LoadAssetAtPath<WorldMap>(ForgeHallTools.HallPath);
            state.hallEntry = ForgeHallTools.EntryTile;
            var opening = Find<Opening>();
            if (opening != null) state.firstWalkLesson = opening.firstWalkLesson;
            panel.state = state;
            panel.island = Find<IslandBuilder>();
            panel.grabGuard = Find<IslandHandle>();
            var cursor = Find<TileCursor>();
            panel.hudMaterial = cursor != null ? cursor.hudMaterial : null;
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(panel);
            int missing = new Object[] { state.party, state.explorer, state.swap, state.save, state.encounters, state.realm, state.onboarding, state.registry,
                state.hub, state.cinderwood, state.forgeHall, panel.island, panel.grabGuard, panel.hudMaterial }.Count(o => o == null);
            log.Append($"\n  Dev Panel: GameState + DevPanel wired ({(missing == 0 ? "all references found" : $"{missing} reference(s) MISSING")}).");
        }

        /// <summary>
        /// After Mugesh's test: the panel within reach (size and placement values, the scene had the old ones) and
        /// shards riding with the Spirit (the rim holder is gone).
        /// </summary>
        public static void Refine(StringBuilder log)
        {
            var panel = Object.FindFirstObjectByType<DevPanel>(FindObjectsInactive.Include);
            if (panel != null)
            {
                Undo.RecordObject(panel, "Step 9e");
                panel.size = new Vector2(0.36f, 0.32f);
                panel.distance = 0.55f;
                panel.sideAngle = 14f;
                panel.downAngle = 22f;
                panel.followAngle = 55f;
                EditorUtility.SetDirty(panel);
            }
            var readout = Object.FindFirstObjectByType<DevReadout>(FindObjectsInactive.Include);
            if (readout != null)
            {
                Undo.RecordObject(readout, "Step 9e");
                readout.showOnStart = false;
                EditorUtility.SetDirty(readout);
            }
            var holder = Object.FindFirstObjectByType<ShardHolder>(FindObjectsInactive.Include);
            if (holder != null)
            {
                Undo.RecordObject(holder, "Step 9e");
                holder.spirit = Object.FindFirstObjectByType<LanternSpirit>(FindObjectsInactive.Include);
                holder.carryMetres = 0.022f;
                holder.offerMetres = 0.05f;
                holder.orbitRadius = 0.035f;
                EditorUtility.SetDirty(holder);
            }
            var socket = StatueSocket.FindById(ShardHolder.FireId);
            if (socket != null)
            {
                Undo.RecordObject(socket, "Step 9e");
                socket.grabRadius = 0.07f;
                EditorUtility.SetDirty(socket);
            }
            log.Append($"\n  Refine: panel {(panel != null ? "within reach" : "MISSING")}, readout {(readout != null ? "hidden at start" : "MISSING")}, shards {(holder != null && holder.spirit != null ? "ride with the Spirit" : "MISSING holder or Spirit")}, " +
                       $"socket grab {(socket != null ? "7 cm" : "MISSING")}");
        }

        public static void RefineOnly()
        {
            var log = new StringBuilder("[Step9e] Refine:");
            Refine(log);
            Save(log);
        }

        public static void PanelOnly()
        {
            var log = new StringBuilder("[Step9e] Panel:");
            Panel(log);
            Save(log);
        }

        /// <summary>B: the smoke material and the Keeper Hall's fury wiring (smoke, the Spirit to flinch).</summary>
        public static void Fury(StringBuilder log)
        {
            var smoke = AssetDatabase.LoadAssetAtPath<Material>(SmokeMaterialPath);
            if (smoke == null)
            {
                var shader = Shader.Find("Diorama/Smoke");
                if (shader == null)
                {
                    log.Append("\n  MISSING shader Diorama/Smoke");
                    return;
                }
                smoke = new Material(shader) { name = "KeeperSmoke" };
                AssetDatabase.CreateAsset(smoke, SmokeMaterialPath);
            }
            var hall = KeeperHall.FindById(KeeperHall.KeeperId);
            if (hall == null)
            {
                log.Append("\n  Fury: NO Keeper Hall in the open scene");
                return;
            }
            Undo.RecordObject(hall, "Step 9e");
            hall.smokeMaterial = smoke;
            hall.spirit = Object.FindFirstObjectByType<LanternSpirit>(FindObjectsInactive.Include);
            EditorUtility.SetDirty(hall);
            log.Append($"\n  Fury: smoke {SmokeMaterialPath}, beacon {(hall.beaconMaterial != null ? hall.beaconMaterial.name : "MISSING")}, Spirit {(hall.spirit != null ? "found" : "MISSING")}");
        }

        public static void FuryOnly()
        {
            var log = new StringBuilder("[Step9e] Fury:");
            Fury(log);
            Save(log);
        }

        /// <summary>Clips whose key is the pinch point (pinch grabs of the rim, pods, the shard, the log).</summary>
        public static readonly string[] PinchClips = { "grab_rim_slide", "grab_drop", "grab_toss", "grab_flick" };

        /// <summary>
        /// D: the bubble keeps out of the island, the hover ring's pillar, the ghosts' pinch point on the rim edge and
        /// pinch grabs keyed on the pinch point.
        /// </summary>
        public static void Fixes(StringBuilder log)
        {
            var island = Object.FindFirstObjectByType<IslandBuilder>(FindObjectsInactive.Include);
            foreach (var b in Object.FindObjectsByType<DialogueBubble>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Undo.RecordObject(b, "Step 9e");
                b.island = island;
                EditorUtility.SetDirty(b);
            }
            var cursor = Object.FindFirstObjectByType<TileCursor>(FindObjectsInactive.Include);
            if (cursor != null)
            {
                Undo.RecordObject(cursor, "Step 9e");
                cursor.pillarMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Props/Beacon.mat");
                EditorUtility.SetDirty(cursor);
            }
            var ghosts = Object.FindFirstObjectByType<GhostHands>(FindObjectsInactive.Include);
            if (ghosts != null)
            {
                Undo.RecordObject(ghosts, "Step 9e");
                // The pinch lands just outside the rim's edge, a little below the cap top (the grab shell: 1 cm in, 2.5 cm out).
                ghosts.sideDrop = 0.015f;
                ghosts.sideOut = 0.012f;
                EditorUtility.SetDirty(ghosts);
                if (ghosts.library != null)
                    foreach (var c in ghosts.library.clips)
                    {
                        if (c == null) continue;
                        bool pinch = System.Array.IndexOf(PinchClips, c.clipName) >= 0;
                        if (c.keyPinch == pinch) continue;
                        Undo.RecordObject(c, "Step 9e");
                        c.keyPinch = pinch;
                        EditorUtility.SetDirty(c);
                    }
            }
            log.Append($"\n  Fixes: {(island != null ? "bubble keeps out of the island" : "NO island")}, " +
                       $"{(cursor != null && cursor.pillarMaterial != null ? "hover pillar" : "NO hover pillar")}, " +
                       $"{(ghosts != null ? $"ghost rim pinch {ghosts.sideDrop * 100f:0.#} cm below the cap, {ghosts.sideOut * 100f:0.#} cm out; pinch key on {string.Join(", ", PinchClips)}" : "NO ghosts")}");
        }

        public static void FixesOnly()
        {
            var log = new StringBuilder("[Step9e] Fixes:");
            Fixes(log);
            Save(log);
        }

        static void Save(StringBuilder log)
        {
            AssetDatabase.SaveAssets();
            var active = EditorSceneManager.GetActiveScene();
            if (active.path == ScenePath && !EditorApplication.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(active);
                EditorSceneManager.SaveScene(active);
            }
            Debug.Log(log.ToString());
        }

        public static void BalanceOnly()
        {
            var log = new StringBuilder("[Step9e] Balance:");
            Balance(log);
            Save(log);
        }
    }
}
