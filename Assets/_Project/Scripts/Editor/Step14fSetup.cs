using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 14f (Nov 3 night, design chat): A the last walk lesson becomes "Swipe to walk, tap to stop" (lessons rewritten,
    /// the swipe + tap ghost built, the script reimported), the Heart's forehead crystal (baked, wired). Code-only parts
    /// need no setup: pinches on hand moments never walk, the untangle holds the explorer still. B: the stone tablet
    /// material (Materials/Tablet, Diorama/Tablet, instanced) on every rune board and the battle runes. Safe to re-run.
    /// </summary>
    public static class Step14fSetup
    {
        const string NL = "\n";
        public const string TabletMaterialPath = "Assets/_Project/Materials/Tablet.mat";

        [MenuItem("Tools/Pocket Isle/Setup/First Minute (14)/Set Up MR_Main (14f menus and fixes)", priority = 1005)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step14f] Stop Play mode first.");
                return;
            }
            var log = new StringBuilder("[Step14f] MR_Main set up:");
            DialogueTools.Import();
            log.Append(NL + "  Script_v1 reimported (lesson.walk.far: Swipe to walk, tap to stop)");
            var clip = Step12aSetup.BuildSwipeTap();
            log.Append(NL + (clip != null ? $"  ghost {Step12aSetup.SwipeTapClip}: {clip.frames.Count} frames" : $"  ghost {Step12aSetup.SwipeTapClip}: MISSING (no recorded swipe / tap clip)"));
            var lessons = Step8bSetup.WriteLessons();
            log.Append(NL + $"  lessons rewritten ({lessons.lessons.Count}): far = swipe + tap, done on a tap stop, the gold tile or two swipe walks, passes after 30 s");
            Crystal(log);
            Tablets(log);
            RealmLines(log);
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }

        /// <summary>B: the tablet material, on every rune board and the battle runes.</summary>
        static void Tablets(StringBuilder log)
        {
            var shader = Shader.Find("Diorama/Tablet");
            if (shader == null)
            {
                log.Append(NL + "  tablets: no Diorama/Tablet shader (MISSING)");
                return;
            }
            var mat = AssetDatabase.LoadAssetAtPath<Material>(TabletMaterialPath);
            if (mat == null)
            {
                mat = new Material(shader) { name = "Tablet" };
                AssetDatabase.CreateAsset(mat, TabletMaterialPath);
            }
            mat.shader = shader;
            mat.enableInstancing = true;
            mat.SetFloat("_Corner", TabletKit.Corner);
            // A stronger warm glow round each slab: it kept them apart from a dark room and the dark settings card.
            mat.SetFloat("_HaloLight", 0.42f);
            EditorUtility.SetDirty(mat);
            int boards = 0;
            foreach (var b in Object.FindObjectsByType<RuneBoard>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Undo.RecordObject(b, "Step 14f");
                b.tabletMaterial = mat;
                EditorUtility.SetDirty(b);
                boards++;
            }
            int rows = 0;
            foreach (var r in Object.FindObjectsByType<BattleRunes>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Undo.RecordObject(r, "Step 14f");
                r.tabletMaterial = mat;
                EditorUtility.SetDirty(r);
                rows++;
            }
            log.Append(NL + $"  tablets: Materials/Tablet (instanced) on {boards} rune board(s) and {rows} battle rune row(s)");
        }

        /// <summary>A2: the realm name cards' lines, shortened to fit at the readable size (two lines at most).</summary>
        static void RealmLines(StringBuilder log)
        {
            var card = Object.FindFirstObjectByType<RealmNameCard>(FindObjectsInactive.Include);
            if (card == null)
            {
                log.Append(NL + "  realm lines: no RealmNameCard (MISSING)");
                return;
            }
            Undo.RecordObject(card, "Step 14f");
            int n = 0;
            for (int i = 0; i < card.cards.Length; i++)
            {
                var c = card.cards[i];
                string shard = c.shard ?? "";
                (string line, string restored) = shard switch
                {
                    "shard.wind" => ("Where the wind stopped", "The wind blows again"),
                    "shard.fire" => ("Where the fire went cold", "The fire burns again"),
                    "shard.water" => ("Where the water froze", "The water flows again"),
                    _ => (c.line, c.restoredLine),
                };
                if (c.line != line || c.restoredLine != restored) n++;
                c.line = line;
                c.restoredLine = restored;
                card.cards[i] = c;
            }
            EditorUtility.SetDirty(card);
            log.Append(NL + $"  realm name card lines shortened ({n} realms): 'Where the wind stopped' / 'The wind blows again', ...");
        }

        static void Crystal(StringBuilder log)
        {
            var recipe = PropKit.WriteHeartCrystal();
            var heart = Object.FindFirstObjectByType<HeartOfWorld>(FindObjectsInactive.Include);
            if (heart == null)
            {
                log.Append(NL + "  crystal baked, but no Heart of the World in the scene (Set Up MR_Main (13 finale)) (MISSING)");
                return;
            }
            Undo.RecordObject(heart, "Step 14f");
            heart.crystalRecipe = recipe;
            EditorUtility.SetDirty(heart);
            log.Append(NL + $"  forehead crystal baked and on the Heart ({recipe.name})");
        }
    }
}
