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

        [MenuItem("Tools/Pocket Isle/Set Up MR_Main (9e dev panel and fury)")]
        public static void All()
        {
            var log = new StringBuilder("[Step9e] Set up:");
            Balance(log);
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
            var hall = Object.FindFirstObjectByType<KeeperHall>(FindObjectsInactive.Include);
            if (hall != null)
            {
                Undo.RecordObject(hall, "Step 9e");
                hall.keeperLevel = KeeperKit.Level;
                EditorUtility.SetDirty(hall);
            }
            var k = new IslingInstance(keeper, KeeperKit.Level);
            log.Append($"\n  Hearthstag Lv {KeeperKit.Level}: HP {k.MaxHp}, atk {k.Attack}, def {k.Defense}, spd {k.Speed}{(hall == null ? " (no Keeper Hall in the open scene)" : "")}");
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
