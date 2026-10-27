using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 12a: smart-step walking and guidance. Writes the GoldMark material, puts the footprint preview
    /// (<see cref="WalkPreview"/>) next to the explorer and switches it to smart steps. Safe to re-run.
    /// </summary>
    public static class Step12aSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";
        const string MaterialPath = "Assets/_Project/Materials/GoldMark.mat";

        /// <summary>The walk-on lesson's ghost: two quick thumb swipes, then a tap (built from the recorded clips).</summary>
        public const string SwipeTwiceClip = "thumb_swipe_twice_tap";

        [MenuItem("Tools/Pocket Isle/Setup/Walking + Guidance (12)/Set Up MR_Main (12a walking)", priority = 900)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step12a] Stop Play mode first.");
                return;
            }
            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            var log = new StringBuilder("[Step12a] MR_Main set up:");
            var mat = GoldMaterial();
            var explorer = Object.FindFirstObjectByType<GestureTestPiece>(FindObjectsInactive.Include);
            if (explorer == null)
            {
                Debug.LogError("[Step12a] No explorer (GestureTestPiece) in MR_Main.");
                return;
            }
            Undo.RecordObject(explorer, "Step 12a");
            explorer.moveMode = MoveMode.SmartStep;
            explorer.smartSeconds = 0.53f;
            explorer.continuousSeconds = 0.4f;
            explorer.smartTiles = SmartStep.SmartTiles;
            explorer.doubleSwipeSeconds = 0.45f;
            var preview = explorer.GetComponent<WalkPreview>();
            if (preview == null) preview = Undo.AddComponent<WalkPreview>(explorer.gameObject);
            preview.explorer = explorer;
            preview.island = explorer.island;
            preview.material = mat;
            preview.footTiles = 0.9f;
            preview.ringTiles = 1.05f;
            log.Append($"\n  explorer: smart steps ({explorer.smartSeconds} s a tile, walk on {explorer.continuousSeconds} s, double swipe {explorer.doubleSwipeSeconds} s), footprints on");
            Extra(log, mat);
            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }

        /// <summary>Later parts of 12a (lessons, Spirit's trail) wire their pieces here.</summary>
        static void Extra(StringBuilder log, Material gold)
        {
            Lessons(log, gold);
            var settings = Object.FindFirstObjectByType<SettingsBoard>(FindObjectsInactive.Include);
            if (settings != null)
            {
                Undo.RecordObject(settings, "Step 12a");
                settings.glyphMaterial = GlyphMaterial();
                log.Append("\n  settings: Controls card (game menu + title Settings)");
            }
            else log.Append("\n  settings board: MISSING");
        }

        static Material GlyphMaterial()
        {
            const string path = "Assets/_Project/Materials/Glyph.mat";
            var shader = Shader.Find("Diorama/Glyph");
            if (shader == null) Debug.LogError("[Step12a] Shader Diorama/Glyph not found.");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader) { name = "Glyph" };
                AssetDatabase.CreateAsset(m, path);
            }
            else if (shader != null && m.shader != shader) m.shader = shader;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>B: the walk lessons (walk, turn, far), their ghost, the gold tile look, both slots, the lines.</summary>
        static void Lessons(StringBuilder log, Material gold)
        {
            var clip = BuildSwipeTwice();
            log.Append(clip != null ? $"\n  ghost {SwipeTwiceClip}: {clip.frames.Count} frames" : $"\n  ghost {SwipeTwiceClip}: MISSING (no recorded swipe / tap clip)");
            var lessons = Step8bSetup.WriteLessons();
            log.Append($"\n  lessons: {string.Join(", ", lessons.lessons.ConvertAll(l => l.id))}");
            var onboarding = Object.FindFirstObjectByType<Onboarding>(FindObjectsInactive.Include);
            if (onboarding != null)
            {
                Undo.RecordObject(onboarding, "Step 12a");
                onboarding.goldMaterial = gold;
                log.Append("\n  onboarding: gold tile look");
            }
            else log.Append("\n  onboarding: MISSING");
            var quick = AssetDatabase.LoadAssetAtPath<QuickJourneyConfig>("Assets/_Project/Data/QuickJourney.asset");
            if (quick != null && quick.cutLessons != null && System.Array.IndexOf(quick.cutLessons, "stop") >= 0)
            {
                // The quick slot cut the old stop lesson; the walk lessons are taught in both slots now.
                quick.cutLessons = System.Array.FindAll(quick.cutLessons, id => id != "stop");
                EditorUtility.SetDirty(quick);
            }
            if (quick != null) log.Append($"\n  quick slot cuts: {string.Join(", ", quick.cutLessons)}");
            DialogueTools.Import();
            log.Append("\n  Script_v1 reimported (lesson.walk*, lesson.walk.turn*, lesson.walk.far*)");
        }

        static GhostHandClip BuildSwipeTwice()
        {
            var lib = GhostClipTools.Library();
            var swipe = lib.Find("thumb_swipe_forward");
            var tap = lib.Find("thumb_tap");
            if (swipe == null || tap == null || swipe.frames.Count < 2 || tap.frames.Count < 2) return null;
            var c = ScriptableObject.CreateInstance<GhostHandClip>();
            EditorUtility.CopySerialized(swipe, c);
            c.clipName = SwipeTwiceClip;
            c.name = SwipeTwiceClip;
            c.trimStart = c.trimEnd = 0f;
            c.frames = new System.Collections.Generic.List<GhostFrame>();
            // Each swipe sped up (every 3rd of 4 frames kept), a short beat, the second swipe, a pause, then the tap.
            void Add(GhostHandClip from, int keepOf4, int hold)
            {
                int a = Mathf.RoundToInt(from.StartTime * from.fps);
                int b = Mathf.Max(a + 1, from.frames.Count - Mathf.RoundToInt(from.trimEnd * from.fps));
                for (int i = a; i < b; i++)
                    if (i % 4 < keepOf4) c.frames.Add(from.frames[i]);
                for (int i = 0; i < hold; i++) c.frames.Add(from.frames[b - 1]);
            }
            Add(swipe, 3, 2);
            Add(swipe, 3, 12);
            Add(tap, 4, 8);
            c.holdEnd = 0.6f;
            return GhostClipTools.SaveAsset(c);
        }

        internal static Material GoldMaterial()
        {
            var shader = Shader.Find("Diorama/GoldMark");
            if (shader == null) Debug.LogError("[Step12a] Shader Diorama/GoldMark not found.");
            var m = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (m == null)
            {
                m = new Material(shader) { name = "GoldMark" };
                AssetDatabase.CreateAsset(m, MaterialPath);
            }
            else if (shader != null && m.shader != shader) m.shader = shader;
            m.SetColor("_Color", new Color(1f, 0.74f, 0.12f));
            m.SetColor("_Core", new Color(1f, 0.95f, 0.62f));
            m.SetColor("_Outline", new Color(0.16f, 0.08f, 0f));
            m.renderQueue = 3009;
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
