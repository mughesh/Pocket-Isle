using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 14d (Mugesh's Nov 3 evening notes on 14c): faster walking, no quick gift (Galeowl easier in the quick slot
    /// instead), the name card centred higher with its sound, the shrine locked through the tutorial, portals you walk up
    /// to, the trail after every moment, better Galewick / Cinderwood boards, the walk lesson reworked. Safe to re-run.
    /// </summary>
    public static class Step14dSetup
    {
        const string NL = "\n";

        /// <summary>Seconds per tile (was 0.4 walk / 0.2 run).</summary>
        public const float WalkSeconds = 0.28f, RunSeconds = 0.15f;

        /// <summary>Galeowl in the quick slot (QuickJourneySim.Keeper: Lv 4 = Kilnpot 100%, Pebbloop 64%, Pufflet 90%).</summary>
        public const int QuickKeeperOffset = -4;

        [MenuItem("Tools/Pocket Isle/Setup/First Minute (14)/Set Up MR_Main (14d Nov 3 evening notes)", priority = 1003)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step14d] Stop Play mode first.");
                return;
            }
            var log = new StringBuilder("[Step14d] MR_Main set up:");
            DialogueTools.Import();
            log.Append(NL + "  Script_v1 reimported (shrine.later, quick.gate send-off, the walk lessons)");
            var lessons = Step8bSetup.WriteLessons();
            log.Append(NL + $"  lessons rewritten ({lessons.lessons.Count}): walk counts 4 tiles really walked, far ends on a tap stop, no turn lesson");
            var explorer = Object.FindFirstObjectByType<GestureTestPiece>(FindObjectsInactive.Include);
            if (explorer != null)
            {
                Undo.RecordObject(explorer, "Step 14d");
                explorer.walkSeconds = explorer.smartSeconds = explorer.continuousSeconds = WalkSeconds;
                explorer.runSeconds = RunSeconds;
                EditorUtility.SetDirty(explorer);
                log.Append(NL + $"  walking {WalkSeconds} s a tile (was 0.4), running {RunSeconds} (was 0.2)");
            }
            var config = AssetDatabase.LoadAssetAtPath<QuickJourneyConfig>(QuickJourneySim.ConfigPath);
            if (config != null)
            {
                config.keeperLevelOffset = QuickKeeperOffset;
                EditorUtility.SetDirty(config);
                log.Append(NL + $"  Quick Journey: no gift, Galeowl {QuickKeeperOffset} levels in the quick slot");
            }
            var card = Object.FindFirstObjectByType<RealmNameCard>(FindObjectsInactive.Include);
            if (card != null)
            {
                Undo.RecordObject(card, "Step 14d");
                card.width = 0.26f;
                card.height = 0.25f;
                card.far = 0.1f;
                EditorUtility.SetDirty(card);
                log.Append(NL + "  name card: centred over the island, 25 cm up, card.rise / card.sink");
            }
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }
    }
}
