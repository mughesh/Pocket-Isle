using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 10f: Cinderwood's hand moments. Writes and bakes their props, rewrites the Cinderwood map (B: the smoky
    /// hollow, same seed) and wires under "Hand Puzzles/Cinderwood": A, cup the village flame (FlameCupPiece on the kiln
    /// village's Hearth Shrine); B, the hollow's lanterns and smoke (LanternHollowPiece); C, the ember for the forge's
    /// firebox (ForgeEmberPiece, which keeps the 9b bellows blocked until it's in). Safe to re-run.
    /// </summary>
    public static class Step10fSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";
        const string RootName = "Hand Puzzles";

        // WarmSpots slots (Cinderwood): the village, the six hollow lanterns, the forge.
        public const int VillageSlot = 0, FirstLanternSlot = 1, ForgeSlot = 7;
        public const string EmberId = "cw.ember";
        public const string HollowId = "cw.hollow";

        public const string FlameId = "cw.flame";
        /// <summary>The kiln village plaza's middle (x, y), where the flame's warm pulse settles.</summary>
        public static readonly Vector2 VillageXY = new Vector2(15.5f, 32.5f);

        [MenuItem("Tools/Pocket Isle/Set Up MR_Main (10f Cinderwood hands)", priority = 67)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step10f] Stop Play mode first.");
                return;
            }
            var log = new StringBuilder("[Step10f] MR_Main set up:");
            Props(log);
            // B: the map with the smoky hollow (the props first: the smoke recipe must exist).
            var map = CinderwoodTools.Write(log);
            if (map == null) return;
            EditorUtility.SetDirty(map);
            AssetDatabase.SaveAssets();
            var problems = CinderwoodTools.Check(map, log);
            foreach (var p in problems) Debug.LogError($"[Step10f] Layout check: {p}");
            log.Append(problems.Count == 0 ? "\n  layout checks: all passed (the smoky hollow gates the way to the springs)" : $"\n  layout checks: {problems.Count} problem(s)");
            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            var root = Root();
            Flame(root, map, log);
            Hollow(root, map, log);
            Ember(root, map, log);
            var tour = Object.FindFirstObjectByType<PerfTour>(FindObjectsInactive.Include);
            if (tour != null)
            {
                Undo.RecordObject(tour, "Step 10f");
                tour.cinderwoodTrail = CinderwoodTools.TourStops(map);
                EditorUtility.SetDirty(tour);
            }
            DialogueTools.Import();
            log.Append("\n  Script_v1 reimported (cw.* lines)");

            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            AssetDatabase.SaveAssets();
            IslandBuilder.RebuildAll();
            Debug.Log(log.ToString());
        }

        public static void Props(StringBuilder log)
        {
            var props = PropKit.WriteCinderwoodMomentProps();
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
            var top = GameObject.Find(RootName);
            if (top == null)
            {
                top = new GameObject(RootName);
                Undo.RegisterCreatedObjectUndo(top, RootName);
            }
            var t = top.transform.Find("Cinderwood");
            if (t == null)
            {
                var go = new GameObject("Cinderwood");
                Undo.RegisterCreatedObjectUndo(go, "Cinderwood");
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
            Undo.RecordObject(c, "Step 10f");
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

        static string Baked(params (string name, PropRecipe r)[] recipes)
        {
            var missing = new List<string>();
            foreach (var (n, r) in recipes)
                if (r == null || r.bakedMesh == null) missing.Add(n);
            return missing.Count > 0 ? $"; NOT BAKED: {string.Join(", ", missing)}" : "";
        }

        // ---- A: cup the flame ----

        static void Flame(Transform root, WorldMap map, StringBuilder log)
        {
            var cup = Child<FlameCupPiece>(root, "Village Flame");
            Common(cup, map);
            cup.id = FlameId;
            cup.reach = 2; // the trail passes right in front; farther and its lesson overlaps the smoky hollow's
            cup.lessonLine = "cw.flame";
            cup.lessonChip = "Cup it!";
            cup.ghostClip = "cup_hands";
            cup.ghostScale = 1f;
            cup.shrine = CinderwoodTools.Tile(CinderwoodTools.ShrineXY);
            cup.coldShrine = PropKit.Load("HearthShrineCold");
            cup.wisp = PropKit.Load("ShrineWisp");
            cup.wispBase = 1.36f;
            cup.wispSize = new Vector2(0.75f, 1.4f);
            cup.warmSlot = VillageSlot;
            cup.villageCentre = new Vector2(VillageXY.x, CinderwoodTools.Size - 1 - VillageXY.y);
            cup.villageRadius = 10f;
            cup.villageEdge = 1.5f;
            EditorUtility.SetDirty(cup);
            log.Append($"\n  'Village Flame' {Wired(cup)}: shrine c{cup.shrine.x} r{cup.shrine.y}, cup {cup.radius * 100f:0} cm / {cup.facingAngle:0} deg " +
                       $"(two hands {cup.twoHandSeconds} s, one hand {cup.oneHandSeconds} s, look + pinch {cup.lookSeconds} s), warmth {cup.villageRadius} tiles round " +
                       $"c{cup.villageCentre.x} r{cup.villageCentre.y}; lesson cw.flame + ghost cup_hands" +
                       Baked(("HearthShrineCold", cup.coldShrine), ("ShrineWisp", cup.wisp)));
        }

        // ---- C: ember in your palm ----

        static void Ember(Transform root, WorldMap map, StringBuilder log)
        {
            PumpPiece bellows = null;
            foreach (var p in Object.FindObjectsByType<PumpPiece>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (p.id == GameState.BellowsId) bellows = p;
            var ember = Child<ForgeEmberPiece>(root, "Forge Ember");
            Common(ember, map);
            ember.id = EmberId;
            ember.reach = 4;
            ember.lessonLine = "cw.ember";
            ember.lessonChip = "Palm up!";
            ember.ghostClip = "palm_carry";
            ember.ghostScale = 1f;
            ember.vent = CinderwoodTools.Tile(CinderwoodTools.ForgeVentXY);
            ember.firebox = CinderwoodTools.Tile(CinderwoodTools.FireboxXY);
            ember.bellows = bellows;
            ember.emberRecipe = PropKit.Load("Ember");
            ember.emberSize = 0.34f;
            ember.warmSlot = ForgeSlot;
            ember.forgeCentre = new Vector2(CinderwoodTools.TempleXY.x, CinderwoodTools.Size - 1 - CinderwoodTools.TempleXY.y);
            ember.forgeRadius = 9f;
            EditorUtility.SetDirty(ember);
            log.Append($"\n  'Forge Ember' {Wired(ember)}: vent c{ember.vent.x} r{ember.vent.y}, firebox c{ember.firebox.x} r{ember.firebox.y}, " +
                       $"palm up within {ember.pickRadius * 100f:0.#} cm for {0.35f} s, tip over within {ember.fireboxRadius * 100f:0} cm of the mouth; " +
                       $"bellows {(bellows != null ? "blocked until it's in" : "MISSING (Set Up MR_Main (9b puzzles))")}; warmth {ember.forgeRadius} tiles; lesson cw.ember + ghost palm_carry" +
                       Baked(("Ember", ember.emberRecipe), ("Firebox", PropKit.Load("Firebox"))));
        }

        // ---- B: the smoky hollow ----

        static void Hollow(Transform root, WorldMap map, StringBuilder log)
        {
            var hollow = Child<LanternHollowPiece>(root, "Smoky Hollow");
            Common(hollow, map);
            hollow.id = HollowId;
            hollow.reach = 4;
            hollow.lessonLine = "cw.lanterns";
            hollow.lessonChip = "Touch them!";
            hollow.ghostClip = "poke";
            hollow.ghostScale = 1f;
            hollow.lanterns = CinderwoodTools.HollowLanterns;
            hollow.smoke = CinderwoodTools.HollowTiles;
            hollow.smokeRecipe = PropKit.Load("SmokeCloud");
            hollow.firstSlot = FirstLanternSlot;
            EditorUtility.SetDirty(hollow);
            // Lit, the stone lanterns' little window has to read from the seat: a stronger glow (palette only, no re-bake;
            // the glade's ring shares it, so it burns brighter once Cinderwood is restored too).
            var stone = PropKit.Load("StoneLantern");
            if (stone != null && stone.palette != null && stone.palette.Length > (int)PropSlot.Glow)
            {
                stone.palette[(int)PropSlot.Glow].glow = 1.9f;
                EditorUtility.SetDirty(stone);
            }
            log.Append($"\n  'Smoky Hollow' {Wired(hollow)}: {hollow.lanterns.Length} lanterns, {hollow.smoke.Length} smoke tiles; touch {hollow.touchRadius * 100f:0.#} cm, " +
                       $"a sweep within {hollow.sweepWindow} s races {hollow.raceStep} s apart, look + pinch lights the next {hollow.nudgeDelay} s later; " +
                       $"warm slots {hollow.firstSlot}-{hollow.firstSlot + hollow.lanterns.Length - 1}; lesson cw.lanterns + ghost poke" + Baked(("SmokeCloud", hollow.smokeRecipe)));
        }
    }
}
