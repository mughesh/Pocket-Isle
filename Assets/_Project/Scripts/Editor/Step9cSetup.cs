using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 9c: Hearthstag, the restored realm, the first shard. Runs the 9b setup first, then writes and bakes the
    /// Keeper, and wires the Forge Hall meeting and battle, the shard holder, the restoration wave, the temple return
    /// gate and the hub statue's shard socket. Safe to re-run.
    /// </summary>
    public static class Step9cSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";
        public const string ShotDir = "Logs/BattleShots";

        /// <summary>Writes, bakes and registers Hearthstag; turnaround shots calmed and frightened.</summary>
        public static IslingSpecies Keeper(StringBuilder log)
        {
            var species = KeeperKit.Write();
            if (species == null) return null;
            var report = CreatureBaker.Bake(species.recipe);
            Step7Setup.Registry();
            AssetDatabase.SaveAssets();
            log?.Append($"\n  Hearthstag baked: {report}");
            IslingShots.Turnaround(species, $"{ShotDir}/9c_hearthstag.png");
            IslingShots.Turnaround(species, $"{ShotDir}/9c_hearthstag_frightened.png", go =>
            {
                var look = go.AddComponent<KeeperLook>();
                look.calm = 0f;
                look.Paint();
            });
            return species;
        }

        public static void KeeperOnly()
        {
            var log = new StringBuilder("[Step9c] Keeper:");
            Keeper(log);
            Debug.Log(log.ToString());
        }
    }
}
