using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 12b: fixes from Mugesh's Oct 27 night notes. Shared (A): wind shard green. Safe to re-run.
    /// </summary>
    public static class Step12bSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";

        /// <summary>The wind shard's colour (bursts, the statue glow): green, so fire / wind / water read apart.</summary>
        public static readonly Color WindGreen = new Color(0.42f, 0.88f, 0.32f);

        [MenuItem("Tools/Pocket Isle/Setup/Walking + Guidance (12)/Set Up MR_Main (12b night notes)", priority = 901)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step12b] Stop Play mode first.");
                return;
            }
            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            var log = new StringBuilder("[Step12b] MR_Main set up:");
            ShardColours(log);
            Focus(log);
            Clouds(log);
            Drought(log);
            ExitDoors(log);
            DialogueTools.Import();
            log.Append("\n  Script_v1 reimported (cw.ember.tip)");
            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            AssetDatabase.SaveAssets();
            IslandBuilder.RebuildAll();
            Debug.Log(log.ToString());
        }

        /// <summary>C: Galewick's ridge path 2 wide (the map is rewritten), the cloud bank floating in the air with shadows.</summary>
        static void Clouds(StringBuilder log)
        {
            if (!Step11dSetup.GalewickMap(log)) log.Append("\n  Galewick map: NOT rewritten");
            foreach (var bank in Object.FindObjectsByType<CloudBankPiece>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                // Step 14a: only the ridge's bank (the arrival's has its own twins).
                if (bank.id != "gw.clouds") continue;
                Undo.RecordObject(bank, "Step 12b");
                bank.puffTwins = new Vector2Int[GalewickTools.CloudTwinXY.Length];
                for (int i = 0; i < bank.puffTwins.Length; i++) bank.puffTwins[i] = GalewickTools.Tile(GalewickTools.CloudTwinXY[i]);
                bank.puffHeight = 2.5f;
                bank.shadowMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Glyph.mat");
                log.Append($"\n  cloud bank: 2 rows, {bank.puffHeight} tiles up, shadows {(bank.shadowMaterial != null ? "on" : "MISSING (run 12a setup)")}");
            }
        }

        /// <summary>D10: Lilymere's dry bed and the garden crossing become ochre drought mud (never near-white).</summary>
        static void Drought(StringBuilder log)
        {
            var theme = AssetDatabase.LoadAssetAtPath<RealmTheme>("Assets/_Project/Data/Props/Themes/Lilymere.asset");
            if (theme != null)
            {
                theme.unrestored.dryBed = PropKit.DroughtMud;
                theme.restored.dryBed = PropKit.DroughtMud;
                EditorUtility.SetDirty(theme);
            }
            var gap = AssetDatabase.LoadAssetAtPath<PropRecipe>("Assets/_Project/Data/Props/BedGap.asset");
            if (gap != null && gap.palette != null && gap.palette.Length > 2)
            {
                gap.palette[0] = new PropColor(PropKit.DroughtMud, PropKit.DroughtMud, 0f);
                gap.palette[2] = new PropColor(new Color(0.43f, 0.29f, 0.17f), new Color(0.43f, 0.29f, 0.17f), 0f);
                EditorUtility.SetDirty(gap);
                PropBaker.Bake(gap);
            }
            log.Append($"\n  drought mud: theme {(theme != null ? "done" : "MISSING")}, garden crossing {(gap != null ? "rebaked" : "MISSING")}");
        }

        /// <summary>A4: the way out of every interior glows like a door, with a gold floor arrow.</summary>
        static void ExitDoors(StringBuilder log)
        {
            var swap = Object.FindFirstObjectByType<SceneSwap>(FindObjectsInactive.Include);
            if (swap == null)
            {
                log.Append(NL + "  exit doors: no scene swap (MISSING)");
                return;
            }
            var g = swap.GetComponent<ExitDoorGuide>();
            if (g == null) g = Undo.AddComponent<ExitDoorGuide>(swap.gameObject);
            g.swap = swap;
            g.island = swap.island;
            g.fx = Object.FindFirstObjectByType<BattleFx>(FindObjectsInactive.Include);
            g.beaconMaterial = Step7bSetup.BeaconMaterial();
            log.Append(NL + "  exit doors: glow + floor arrow in every interior");
        }

        const string NL = "\n";

        /// <summary>A1: one focus lock for every hand moment, next to the explorer.</summary>
        static void Focus(StringBuilder log)
        {
            var explorer = Object.FindFirstObjectByType<GestureTestPiece>(FindObjectsInactive.Include);
            if (explorer == null)
            {
                log.Append("\n  focus lock: no explorer (MISSING)");
                return;
            }
            var fl = explorer.GetComponent<FocusLock>();
            if (fl == null) fl = Undo.AddComponent<FocusLock>(explorer.gameObject);
            fl.explorer = explorer;
            fl.island = explorer.island;
            fl.window = explorer.window;
            log.Append($"\n  focus lock: {(fl.window != null ? "on" : "no window (MISSING)")}");
        }

        /// <summary>A6: the wind shard, its statue rim, its bursts go green.</summary>
        static void ShardColours(StringBuilder log)
        {
            var wind = PropKit.WriteWindShardProp();
            AssetDatabase.SaveAssets();
            log.Append($"\n  WindShard green: {PropBaker.Bake(wind).triangles} tris");
            int n = 0;
            foreach (var kh in Object.FindObjectsByType<KeeperHall>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (kh.ownShard == ShardHolder.WindId)
                {
                    Undo.RecordObject(kh, "Step 12b");
                    kh.shardColor = WindGreen;
                    n++;
                }
            foreach (var s in Object.FindObjectsByType<StatueSocket>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (s.shardRecipe != wind) continue;
                Undo.RecordObject(s, "Step 12b");
                s.shardColor = WindGreen;
                n++;
                if (s.statueRecipe != null && s.statueRecipe.palette != null && s.statueRecipe.palette.Length > (int)PropSlot.Accent)
                {
                    s.statueRecipe.palette[(int)PropSlot.Accent] = PropKit.StatueWindRim;
                    EditorUtility.SetDirty(s.statueRecipe);
                }
            }
            // The statue's rims live on the shared statue recipe: rebake it.
            foreach (var s in Object.FindObjectsByType<StatueSocket>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (s.statueRecipe != null && s.shardRecipe == wind)
                {
                    PropBaker.Bake(s.statueRecipe);
                    break;
                }
            log.Append($"\n  wind keeper burst + statue socket green ({n})");
        }
    }
}
