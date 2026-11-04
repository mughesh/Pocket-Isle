using System.Text;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 14h, the growth loop: reimports the script (evolve.ready / evolve.done, the Keepers' thanks), wires the
    /// GrowthDirector's pop text. Safe to re-run.
    /// </summary>
    public static class Step14hSetup
    {
        const string NL = "\n";

        [MenuItem("Tools/Pocket Isle/Setup/First Minute (14)/Set Up MR_Main (14h growth loop)", priority = 1008)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step14h] Stop Play mode first.");
                return;
            }
            var log = new StringBuilder("[Step14h] MR_Main set up:");
            DialogueTools.Import();
            log.Append(NL + "  Script_v1 reimported (evolve.ready, evolve.done, keeper.thanks.evolve.*, journal.first)");
            JournalBake.Bake();
            log.Append(NL + "  Journal baked (portraits atlas, Resources/JournalData)");

            // The GrowthDirector finds its pop text (BattleHud) at runtime: no scene change for A.
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }
    }
}
