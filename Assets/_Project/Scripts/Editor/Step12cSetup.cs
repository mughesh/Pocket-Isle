using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 12c: fixes from Mugesh's Oct 28 midnight test (Notes copy.md): smart steps at the pinch-walk speed, the ridge
    /// clouds back down on the road (still moving), the road north of the ridge open again, Lilymere's own blue-green
    /// palette. Safe to re-run.
    /// </summary>
    public static class Step12cSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";
        const string NL = "\n";
        public const string WaterSheetPath = "Assets/_Project/Materials/Props/WaterSheet.mat";

        [MenuItem("Tools/Pocket Isle/Setup/Walking + Guidance (12)/Set Up MR_Main (12c Oct 28 notes)", priority = 902)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step12c] Stop Play mode first.");
                return;
            }
            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            var log = new StringBuilder("[Step12c] MR_Main set up:");
            Walking(log);
            Clouds(log);
            LilymerePalette(log);
            DevPanelSpot(log);
            CliffBowl(log);
            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            AssetDatabase.SaveAssets();
            IslandBuilder.RebuildAll();
            Debug.Log(log.ToString());
        }

        /// <summary>"New walking should be a bit faster: the pinch to walk speed."</summary>
        static void Walking(StringBuilder log)
        {
            var explorer = Object.FindFirstObjectByType<GestureTestPiece>(FindObjectsInactive.Include);
            if (explorer == null)
            {
                log.Append(NL + "  walking: no explorer (MISSING)");
                return;
            }
            Undo.RecordObject(explorer, "Step 12c");
            explorer.smartSeconds = explorer.walkSeconds;
            log.Append(NL + $"  smart step: {explorer.smartSeconds} s a tile (= pinch walk)");
        }

        /// <summary>The dev panel rework (QA builds): bigger for the headed groups, beside the island on the dominant-hand side.</summary>
        static void DevPanelSpot(StringBuilder log)
        {
            var panel = Object.FindFirstObjectByType<DevPanel>(FindObjectsInactive.Include);
            if (panel == null)
            {
                log.Append(NL + "  dev panel: none in MR_Main (MISSING)");
                return;
            }
            Undo.RecordObject(panel, "Step 12c");
            panel.size = new Vector2(0.42f, 0.36f);
            panel.besideIsland = true;
            panel.besideHeight = 0.08f;
            panel.besideGap = -0.12f;
            panel.besideBack = 0.08f;
            log.Append(NL + "  dev panel: 42 x 36 cm, beside the island (dominant hand), close rune, scroll");
        }

        /// <summary>
        /// Lilymere's waterfall (12b D11-D13): the cliff with the carved bowl on the map (its map is rewritten), the frozen /
        /// running fall from the bowl's lip, pours into the bowl, the water sheet across the palms, the two-hand ghost
        /// carried from the inlet to the bowl.
        /// </summary>
        static void CliffBowl(StringBuilder log)
        {
            PropKit.WriteCliffBowl();
            var map = LilymereTools.Write(log);
            if (map != null)
            {
                var problems = LilymereTools.Check(map, log);
                foreach (var p in problems) Debug.LogError($"[Step12c] Lilymere layout check: {p}");
                log.Append(problems.Count == 0 ? NL + "  Lilymere layout checks: all passed" : NL + $"  Lilymere layout checks: {problems.Count} problem(s)");
            }
            var mat = AssetDatabase.LoadAssetAtPath<Material>(WaterSheetPath);
            if (mat == null)
            {
                var shader = Shader.Find("Diorama/Water Sheet");
                if (shader == null) Debug.LogError("[Step12c] Shader Diorama/Water Sheet not found.");
                else
                {
                    mat = new Material(shader) { name = "WaterSheet", enableInstancing = true };
                    AssetDatabase.CreateAsset(mat, WaterSheetPath);
                }
            }
            if (mat != null)
            {
                // Oct 28: "very little water": less see-through.
                mat.SetFloat("_Alpha", 0.86f);
                EditorUtility.SetDirty(mat);
            }
            foreach (var scoop in Object.FindObjectsByType<ScoopPour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Undo.RecordObject(scoop, "Step 12c");
                scoop.runningRecipe = PropKit.Load("BowlFallRunning");
                scoop.frozenRecipe = PropKit.Load("BowlFallFrozen");
                scoop.bowl = scoop.waterfallCenter;
                scoop.waterSheetMaterial = mat;
                // Oct 28 (Mugesh: two hands were bad, one hand kind of worked): one cupped hand, a bigger pool in the
                // palm, an easier tip and a forgiving pour zone (the bowl, the fall and its pool).
                scoop.twoHands = false;
                scoop.basinRadius = 1.8f;
                scoop.basinAbove = 0.35f;
                scoop.tipUp = -0.25f;
                scoop.palmPool = 0.036f;
                scoop.sheetDome = 0.012f;
                scoop.ghostClip = "cup_carry";
                log.Append(NL + $"  {scoop.id}: pours into the cliff bowl at c{scoop.bowl.x:0.#} r{scoop.bowl.y:0.#} ({scoop.bowlRim} tiles up), " +
                           $"fall {(scoop.frozenRecipe != null && scoop.runningRecipe != null ? "frozen + running" : "MISSING")}, water sheet {(mat != null ? "on" : "MISSING")}, ghost cup_carry");
            }
        }

        /// <summary>The ridge: puffs on the road again (bob kept small), the road north to the tower open.</summary>
        static void Clouds(StringBuilder log)
        {
            if (!Step11dSetup.GalewickMap(log)) log.Append(NL + "  Galewick map: NOT rewritten");
            foreach (var bank in Object.FindObjectsByType<CloudBankPiece>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Undo.RecordObject(bank, "Step 12c");
                bank.puffHeight = 0.6f;
                bank.bob = 0.1f;
                log.Append(NL + $"  cloud bank: {bank.puffHeight} tiles up, bob {bank.bob}");
            }
        }

        /// <summary>Lilymere had Galewick's meadow greens: now a wetland (theme + willow trees, <see cref="PropKit.WriteLilymereLook"/>).</summary>
        static void LilymerePalette(StringBuilder log)
        {
            PropKit.WriteLilymereLook();
            log.Append(NL + "  Lilymere palette: wetland teal / blue (theme, willows rebaked)");
        }
    }
}
