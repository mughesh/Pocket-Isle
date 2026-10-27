using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 10b: Galewick's hand moments. Writes and bakes the puzzle props (drawbridge, ditch water, mill gear, the
    /// tower open + its door), rewrites the map with the raised drawbridge on the crossing and the cloud bank on the
    /// ridge path, and wires under "Hand Puzzles/Galewick": the big mill (WheelPiece + BigMill), the two small mills
    /// (toy wheels), the kites (KitePiece), the gust (GustField), the cloud bank (CloudBankPiece) and the tower door
    /// (TowerDoor). Re-runs 10a first (props, map, gates). Safe to re-run.
    /// </summary>
    public static class Step10bSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";
        const string StringMaterialPath = "Assets/_Project/Materials/Props/KiteString.mat";

        [MenuItem("Tools/Pocket Isle/Set Up MR_Main (10b Galewick hands)", priority = 61)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step10b] Stop Play mode first.");
                return;
            }
            var log = new StringBuilder("[Step10b] MR_Main set up:");
            Props(log); // first: the 10a map needs the drawbridge prop
            Step10aSetup.SetUp();
            var map = AssetDatabase.LoadAssetAtPath<WorldMap>(GalewickTools.MapPath);
            if (map == null) return;
            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            var root = Root();
            var mill = BigMill(root, map, log);
            SmallMills(root, map, log);
            var kites = Kites(root, map, log);
            var gust = Gust(root, map, log);
            Clouds(root, map, gust, log);
            Tower(root, map, kites, log);
            DialogueTools.Import();
            log.Append("\n  Script_v1 reimported (gw.* lines)");

            var problems = GalewickTools.Check(map, log);
            foreach (var p in problems) Debug.LogError($"[Step10b] Layout check: {p}");
            log.Append(problems.Count == 0 ? "\n  layout checks: all passed (drawbridge and cloud bank gate the way)" : $"\n  layout checks: {problems.Count} problem(s)");

            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            AssetDatabase.SaveAssets();
            IslandBuilder.RebuildAll();
            Debug.Log(log.ToString());
        }

        public static void Props(StringBuilder log)
        {
            var props = PropKit.WriteGalewickPuzzleProps();
            AssetDatabase.SaveAssets();
            foreach (var r in props.Values)
            {
                var report = PropBaker.Bake(r);
                log.Append($"\n  {r.name}: {report.triangles} tris (budget {r.triangleBudget})");
            }
            AssetDatabase.SaveAssets();
        }

        static Transform Root()
        {
            var top = GameObject.Find("Hand Puzzles");
            if (top == null)
            {
                top = new GameObject("Hand Puzzles");
                Undo.RegisterCreatedObjectUndo(top, "Hand Puzzles");
            }
            var t = top.transform.Find("Galewick");
            if (t == null)
            {
                var go = new GameObject("Galewick");
                Undo.RegisterCreatedObjectUndo(go, "Galewick");
                go.transform.SetParent(top.transform, false);
                t = go.transform;
            }
            return t;
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
            Undo.RecordObject(c, "Step 10b");
            return c;
        }

        static T Also<T>(Component on) where T : Component
        {
            var c = on.GetComponent<T>();
            if (c == null) c = Undo.AddComponent<T>(on.gameObject);
            Undo.RecordObject(c, "Step 10b");
            return c;
        }

        static void Common(HandPuzzle p, WorldMap map)
        {
            p.map = map;
            p.island = Object.FindFirstObjectByType<IslandBuilder>(FindObjectsInactive.Include);
            p.window = Object.FindFirstObjectByType<WorldWindow>(FindObjectsInactive.Include);
            p.explorer = Object.FindFirstObjectByType<GestureTestPiece>(FindObjectsInactive.Include);
            p.grabGuard = Object.FindFirstObjectByType<IslandHandle>(FindObjectsInactive.Include);
            p.ghosts = Object.FindFirstObjectByType<GhostHands>(FindObjectsInactive.Include);
            p.fx = Object.FindFirstObjectByType<BattleFx>(FindObjectsInactive.Include);
        }

        static string Wired(HandPuzzle p) =>
            p.island == null || p.explorer == null || p.window == null ? "MISSING island / explorer / window" :
            p.ghosts == null ? "no ghost hands (no ghost lesson)" : "wired";

        // ---- A: the windmills ----

        static WheelPiece BigMill(Transform root, WorldMap map, StringBuilder log)
        {
            var wheel = Child<WheelPiece>(root, "Big Mill");
            Common(wheel, map);
            wheel.id = "gw.mill";
            wheel.reach = 4;
            wheel.lessonLine = "gw.mill";
            wheel.lessonChip = "Flick!";
            wheel.ghostClip = "finger_flick";
            wheel.anchor = GalewickTools.Tile(GalewickTools.MillXY);
            wheel.footprint = new Vector2Int(2, 2);
            wheel.recipe = PropKit.Load("Windmill");
            wheel.hubShape = PropKit.MillHub;
            wheel.radius = 1.25f;
            wheel.flicksNeeded = 3;
            wheel.flickWindow = 4f;
            var mill = Also<BigMill>(wheel);
            mill.wheel = wheel;
            mill.island = wheel.island;
            mill.explorer = wheel.explorer;
            mill.fx = wheel.fx;
            mill.bridge = GalewickTools.Tile(GalewickTools.CrossingXY);
            // On the tower's front, just right of the door as the player sees it (higher column), above it.
            var c = (Vector2)wheel.anchor + new Vector2(0.5f, 0.5f);
            mill.gearTile = c + new Vector2(0.4f, 0.68f);
            mill.gearHeight = 0.85f;
            mill.bridgeRecipe = PropKit.Load("Drawbridge");
            mill.gapRecipe = PropKit.Load("DitchGap");
            mill.gearRecipe = PropKit.Load("MillGear");
            EditorUtility.SetDirty(wheel);
            EditorUtility.SetDirty(mill);
            log.Append($"\n  'Big Mill' {Wired(wheel)}: c{wheel.anchor.x} r{wheel.anchor.y}, 3 flicks in 4 s lower the drawbridge at c{mill.bridge.x} r{mill.bridge.y}; lesson gw.mill + ghost finger_flick");
            return wheel;
        }

        static void SmallMills(Transform root, WorldMap map, StringBuilder log)
        {
            var spots = new[] { new Vector2Int(17, 21), new Vector2Int(31, 20) };
            for (int i = 0; i < spots.Length; i++)
            {
                var wheel = Child<WheelPiece>(root, $"Small Mill {i}");
                Common(wheel, map);
                wheel.id = $"gw.smallmill.{i}";
                wheel.reach = 4;
                wheel.lessonLine = null;
                wheel.lessonChip = null;
                wheel.ghostClip = null;
                wheel.anchor = GalewickTools.Tile(spots[i]);
                wheel.footprint = Vector2Int.one;
                wheel.recipe = PropKit.Load("SmallMill");
                wheel.hubShape = new Vector3(0f, 1.32f, 0.34f);
                wheel.radius = 0.68f;
                wheel.flicksNeeded = 0;
                EditorUtility.SetDirty(wheel);
            }
            log.Append("\n  'Small Mill 0/1': toy wheels (flick for fun, no puzzle)");
        }

        // ---- B: the kites ----

        static KitePiece Kites(Transform root, WorldMap map, StringBuilder log)
        {
            var kites = Child<KitePiece>(root, "Kites");
            Common(kites, map);
            kites.id = "gw.kites";
            kites.reach = 7;
            kites.lessonLine = "gw.kites";
            kites.lessonChip = "Pull it up!";
            kites.ghostClip = "grab_toss";
            kites.kites = new Vector2Int[GalewickTools.KiteXY.Length];
            kites.recipes = new PropRecipe[GalewickTools.KiteXY.Length];
            for (int i = 0; i < kites.kites.Length; i++)
            {
                kites.kites[i] = GalewickTools.Tile(GalewickTools.KiteXY[i]);
                kites.recipes[i] = PropKit.Load(PropKit.KiteNames[i]);
            }
            kites.stringMaterial = StringMaterial();
            var t = GalewickTools.Tile(GalewickTools.TowerXY);
            kites.tiedAt = new Vector3(t.x + 0.5f, 4.1f, t.y + 0.5f);
            kites.encounters = Object.FindFirstObjectByType<EncounterDirector>(FindObjectsInactive.Include);
            EditorUtility.SetDirty(kites);
            log.Append($"\n  'Kites' {Wired(kites)}: 3 kites, lift {kites.liftThreshold * 100f:0} cm (island-scaled) or a toss {kites.tossSpeed} m/s, fly {kites.flyHeight * 100f:0}-{(kites.flyHeight + 2f * kites.flyStep) * 100f:0} cm up; lesson gw.kites + ghost grab_toss");
            return kites;
        }

        static Material StringMaterial()
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(StringMaterialPath);
            if (m == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null)
                {
                    Debug.LogError("[Step10b] URP Unlit shader not found: no kite string material.");
                    return null;
                }
                m = new Material(shader) { name = "KiteString" };
                AssetDatabase.CreateAsset(m, StringMaterialPath);
            }
            m.SetColor("_BaseColor", new Color(0.93f, 0.9f, 0.82f));
            m.SetFloat("_Cull", 0f);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        // ---- C: the gust and the cloud bank ----

        static GustField Gust(Transform root, WorldMap map, StringBuilder log)
        {
            var gust = Child<GustField>(root, "Gust");
            gust.map = map;
            gust.island = Object.FindFirstObjectByType<IslandBuilder>(FindObjectsInactive.Include);
            gust.window = Object.FindFirstObjectByType<WorldWindow>(FindObjectsInactive.Include);
            gust.encounters = Object.FindFirstObjectByType<EncounterDirector>(FindObjectsInactive.Include);
            gust.grabGuard = Object.FindFirstObjectByType<IslandHandle>(FindObjectsInactive.Include);
            gust.fx = Object.FindFirstObjectByType<BattleFx>(FindObjectsInactive.Include);
            EditorUtility.SetDirty(gust);
            log.Append($"\n  'Gust': open hand over the island > {gust.minSpeed} m/s within {gust.maxHeight * 100f:0} cm, cooldown {gust.cooldown} s");
            return gust;
        }

        static void Clouds(Transform root, WorldMap map, GustField gust, StringBuilder log)
        {
            var bank = Child<CloudBankPiece>(root, "Cloud Bank");
            Common(bank, map);
            bank.id = "gw.clouds";
            bank.reach = 4;
            bank.lessonLine = "gw.gust";
            bank.lessonChip = "Sweep!";
            bank.ghostClip = "palm_sweep";
            bank.puffs = new Vector2Int[GalewickTools.CloudXY.Length];
            for (int i = 0; i < bank.puffs.Length; i++) bank.puffs[i] = GalewickTools.Tile(GalewickTools.CloudXY[i]);
            bank.recipe = PropKit.Load("CloudPuff");
            bank.gusts = gust;
            EditorUtility.SetDirty(bank);
            log.Append($"\n  'Cloud Bank' {Wired(bank)}: 3 puffs on the ridge path, ~3 gusts each; lesson gw.gust + ghost palm_sweep");
        }

        // ---- D: the tower door ----

        static void Tower(Transform root, WorldMap map, KitePiece kites, StringBuilder log)
        {
            var door = Child<TowerDoor>(root, "Tower Door");
            door.map = map;
            door.island = kites.island;
            door.explorer = kites.explorer;
            door.kites = kites;
            door.swap = Object.FindFirstObjectByType<SceneSwap>(FindObjectsInactive.Include);
            door.fx = kites.fx;
            door.tower = GalewickTools.Tile(GalewickTools.TowerXY);
            var f = GalewickTools.TowerFront;
            door.front = new[] { f, f + Vector2Int.right };
            door.openRecipe = PropKit.Load("WindTowerOpen");
            door.doorRecipe = PropKit.Load("WindTowerDoor");
            EditorUtility.SetDirty(door);
            log.Append($"\n  'Tower Door': front c{f.x}-{f.x + 1} r{f.y}, opens with 3 kites flying");
        }
    }
}
