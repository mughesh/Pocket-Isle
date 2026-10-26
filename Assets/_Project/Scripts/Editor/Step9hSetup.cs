using System.Text;
using Meta.XR.MRUtilityKit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 9h: first minutes clarity. One method per part (bridge: exec:Diorama.EditorTools.Step9hSetup.X),
    /// <see cref="All"/> for everything. Safe to re-run.
    /// </summary>
    public static class Step9hSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";

        [MenuItem("Tools/Pocket Isle/Set Up MR_Main (9h first minutes)", priority = 61)]
        public static void All()
        {
            var log = new StringBuilder("[Step9h] Set up:");
            Fixes(log);
            Lessons(log);
            Logo(log);
            Save(log);
        }

        public static void LogoOnly()
        {
            var log = new StringBuilder("[Step9h] Logo:");
            Logo(log);
            Save(log);
        }

        /// <summary>E: logo v2, the letters and the mini island baked, wired to the title's logo.</summary>
        public static void Logo(StringBuilder log)
        {
            var letters = PropKit.WriteLogo();
            log.Append($"\n  Letters: {PropBaker.Bake(letters)}");
            var chunk = PropKit.WriteLogoBase();
            log.Append($"\n  Base: {PropBaker.Bake(chunk)}");
            int tris = (letters.bakedMesh != null ? letters.bakedMesh.triangles.Length / 3 : 0) + (chunk.bakedMesh != null ? chunk.bakedMesh.triangles.Length / 3 : 0);
            log.Append($"\n  Logo total {tris} triangles (budget 10000), 2 renderers x 2 passes = 4 draws (budget 8).");
            // v3: one thick dark brown outline round letters and island (the reference's sticker look).
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Props/Logo.mat");
            if (mat != null)
            {
                mat.SetColor("_OutlineTint", PropKit.LogoOutline);
                EditorUtility.SetDirty(mat);
            }
            else log.Append("\n  MISSING Logo.mat (Step9gSetup.TitleOnly first).");
            var logo = Find<TitleLogo>();
            if (logo == null)
            {
                log.Append("\n  MISSING TitleLogo (Step9gSetup.TitleOnly first).");
                return;
            }
            Undo.RecordObject(logo, "Step 9h");
            logo.recipe = letters;
            logo.baseRecipe = chunk;
            EditorUtility.SetDirty(logo);
        }

        public static void LessonsOnly()
        {
            var log = new StringBuilder("[Step9h] Lessons:");
            Lessons(log);
            Save(log);
        }

        /// <summary>
        /// B (and C's pinch lesson): the lessons rewritten (chips, the starter's help line), Script_v1 re-imported (the
        /// shorter opening), the bubble's reading pace.
        /// </summary>
        public static void Lessons(StringBuilder log)
        {
            var lessons = Step8bSetup.WriteLessons();
            log.Append($"\n  {Step8bSetup.LessonsPath}: {lessons.lessons.Count} lessons ({string.Join(", ", lessons.lessons.ConvertAll(l => l.id + (string.IsNullOrEmpty(l.chip) ? "" : $" '{l.chip}'")))})");
            var script = DialogueTools.Import();
            log.Append($"\n  Script_v1 re-imported: {(script != null ? script.triggers.Count + " triggers" : "FAILED")}");
            var bubble = Find<DialogueBubble>();
            if (bubble == null)
            {
                log.Append("\n  MISSING DialogueBubble.");
                return;
            }
            var onboarding = Find<Onboarding>();
            var fx = Find<BattleFx>();
            if (onboarding != null)
            {
                Undo.RecordObject(onboarding, "Step 9h");
                onboarding.fx = fx;
                EditorUtility.SetDirty(onboarding);
            }
            log.Append($"\n  Pinch lesson: sparkle fx {(onboarding != null && fx != null ? "linked" : "MISSING")}.");
            // The ghost palm over a shrine's flame (until the first lodge visit).
            var shrines = Find<HearthShrines>();
            var ghosts = Find<GhostHands>();
            if (shrines != null)
            {
                Undo.RecordObject(shrines, "Step 9h");
                shrines.ghosts = ghosts;
                EditorUtility.SetDirty(shrines);
            }
            log.Append($"\n  Shrine palm ghost: {(shrines != null && ghosts != null ? "linked" : "MISSING")}.");
            Undo.RecordObject(bubble, "Step 9h");
            bubble.holdBase = 1.5f;
            bubble.holdPerChar = 0.06f;
            EditorUtility.SetDirty(bubble);
            log.Append($"\n  Bubble: reading pace {bubble.holdBase} s + {bubble.holdPerChar} s a character after typing, chip after {bubble.chipAfter} s.");
        }

        public static void FixesOnly()
        {
            var log = new StringBuilder("[Step9h] Fixes:");
            Fixes(log);
            Save(log);
        }

        static T Find<T>() where T : Object => Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);

        static T Component<T>(GameObject go) where T : Component => go.TryGetComponent(out T c) ? c : Undo.AddComponent<T>(go);

        /// <summary>A: MRUK's startup load off (it starts the room scan with no scene data), the silent room loader instead.</summary>
        public static void Fixes(StringBuilder log)
        {
            var mruk = Find<MRUK>();
            var placer = Find<IslandPlacer>();
            if (mruk == null || placer == null)
            {
                log.Append("\n  MISSING MRUK or IslandPlacer: open MR_Main.");
                return;
            }
            Undo.RecordObject(mruk, "Step 9h");
            mruk.SceneSettings.LoadSceneOnStartup = false;
            EditorUtility.SetDirty(mruk);
            var loader = Component<RoomSceneLoader>(mruk.gameObject);
            loader.placer = placer;
            EditorUtility.SetDirty(loader);
            log.Append($"\n  MRUK '{mruk.name}': LoadSceneOnStartup off; RoomSceneLoader (no scan, no permission prompt) after the island is placed.");
        }

        static void Save(StringBuilder log)
        {
            AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path == ScenePath && !EditorApplication.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                log.Append("\n  Scene saved.");
            }
            else log.Append($"\n  Scene NOT saved (active: {scene.path}, playing {EditorApplication.isPlaying}).");
            Debug.Log(log.ToString());
        }
    }
}
