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
