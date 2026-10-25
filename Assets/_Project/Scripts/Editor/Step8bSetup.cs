using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static Diorama.EditorTools.StarterKit;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 8b: writes the opening's lessons (Data/Onboarding/Onboarding_Main, overwrites), re-imports Script_v1 and adds
    /// the Onboarding object to MR_Main (wired to the opening and the save). Safe to re-run.
    /// </summary>
    static class Step8bSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";
        public const string LessonsPath = "Assets/_Project/Data/Onboarding/Onboarding_Main.asset";

        [MenuItem("Tools/Pocket Isle/Set Up MR_Main (8b onboarding)", priority = 48)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step8b] Stop Play mode first.");
                return;
            }
            var log = new StringBuilder("[Step8b] MR_Main set up:");
            var lessons = WriteLessons();
            log.Append($"\n  {LessonsPath}: {lessons.lessons.Count} lessons ({string.Join(", ", lessons.lessons.ConvertAll(l => l.id))})");
            DialogueTools.Import();
            log.Append("\n  Script_v1 reimported (lesson lines, praise, while conditions)");

            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            var opening = Object.FindFirstObjectByType<Opening>(FindObjectsInactive.Include);
            var save = Object.FindFirstObjectByType<GameSave>(FindObjectsInactive.Include);
            var director = Object.FindFirstObjectByType<EncounterDirector>(FindObjectsInactive.Include);
            if (opening == null || save == null || director == null)
            {
                Debug.LogError("[Step8b] MR_Main needs the opening (8 hub), the save (7) and the encounter director.");
                return;
            }

            var onboarding = Object.FindFirstObjectByType<Onboarding>(FindObjectsInactive.Include);
            if (onboarding == null)
            {
                var go = new GameObject("Onboarding");
                Undo.RegisterCreatedObjectUndo(go, "Onboarding");
                onboarding = go.AddComponent<Onboarding>();
            }
            Undo.RecordObject(onboarding, "Step 8b");
            onboarding.script = lessons;
            onboarding.explorer = opening.explorer;
            onboarding.encounters = director;
            onboarding.island = opening.island;
            onboarding.handle = opening.handle;
            onboarding.input = Object.FindFirstObjectByType<MicrogestureInput>(FindObjectsInactive.Include);
            onboarding.save = save;
            EditorUtility.SetDirty(onboarding);
            log.Append($"\n  'Onboarding' (lessons, tutorial grass = {onboarding.patchLandmark} +-{onboarding.patchRadius}, walk lesson {onboarding.walkSteps} steps; input {(onboarding.input != null ? "linked" : "MISSING")})");

            Undo.RecordObject(opening, "Step 8b");
            opening.onboarding = onboarding;
            EditorUtility.SetDirty(opening);
            Undo.RecordObject(save, "Step 8b");
            save.onboarding = onboarding;
            EditorUtility.SetDirty(save);
            log.Append("\n  the opening starts the lessons; the save keeps the current lesson (save v3; older saves count the tutorial as done)");

            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }

        /// <summary>The opening's lessons in order (step-08b prompt, section B).</summary>
        public static OnboardingScript WriteLessons()
        {
            DioramaSceneSetup.EnsureFolder("Assets/_Project/Data/Onboarding");
            var s = Asset<OnboardingScript>(LessonsPath);
            const string battleFail = "battleFailed";
            s.lessons = new List<LessonData>
            {
                new LessonData
                {
                    id = "shard", line = "intro.shard", startWhen = "opening.shard", delay = 3f, validWhile = "opening.shard",
                    doneWhen = "shardTouched", ghost = "poke", place = GhostPlace.AtTarget, target = "shard",
                },
                new LessonData
                {
                    id = "move", line = "intro.world", again = "lesson.move.again", startWhen = "opening.move", validWhile = "opening.move",
                    doneWhen = "islandGrabbed", skipAfter = 12f, ghost = "grab_rim_slide", place = GhostPlace.AtTarget, target = "rim",
                },
                new LessonData
                {
                    id = "starter", line = "intro.starter", again = "lesson.starter.again", startWhen = "opening.starters",
                    doneWhen = "starterPicked", ghost = "grab_drop", place = GhostPlace.AtTarget, target = "starterPods",
                },
                new LessonData
                {
                    id = "walk", line = "lesson.walk", again = "lesson.walk.again", help = "lesson.walk.help", helpAfter = 30f,
                    startWhen = "canWalk", delay = 1.5f, doneWhen = "walked3",
                    ghost = "thumb_swipe_forward", place = GhostPlace.AboveIsland, target = "ghost", ghostScale = 0.6f,
                },
                new LessonData
                {
                    id = "stop", line = "lesson.stop", again = "lesson.stop.again", startWhen = "canWalk", delay = 0.8f, doneWhen = "stopped",
                    ghost = "thumb_tap", place = GhostPlace.AboveIsland, target = "ghost", ghostScale = 0.6f,
                },
                new LessonData
                {
                    id = "look", line = "lesson.look", again = "lesson.look.again", startWhen = "canWalk", delay = 0.8f,
                    doneWhen = "atGoldTile | gazeWalk", goldTileSteps = 3,
                    ghost = "pinch", place = GhostPlace.AboveTarget, target = "goldTile", ghostScale = 0.6f,
                },
                new LessonData
                {
                    id = "grass", line = "lesson.grass", again = "lesson.grass.again", startWhen = "canWalk", delay = 0.8f,
                    doneWhen = "grassBattle", praise = false, target = "landmark:firstGrass", goldPath = "firstGrass", tutorialGrass = true,
                },
                new LessonData
                {
                    id = "send", line = "pod.first", again = "lesson.send.again", startWhen = "battle.send", validWhile = "battle.send",
                    doneWhen = "sentOut", failWhen = battleFail, rewindTo = "grass",
                    ghost = "grab_toss", place = GhostPlace.AtTarget, target = "leadPod",
                },
                new LessonData
                {
                    id = "rune", line = "battle.first", again = "lesson.rune.again", startWhen = "battle.out", validWhile = "battle",
                    doneWhen = "moveUsed", failWhen = battleFail, rewindTo = "grass",
                    ghost = "pinch", place = GhostPlace.AtTarget, target = "runes",
                },
                new LessonData
                {
                    id = "catch", line = "capture.first", again = "lesson.catch.again", startWhen = "wildRed", validWhile = "battle",
                    doneWhen = "podThrown", failWhen = battleFail, rewindTo = "grass",
                    ghost = "grab_flick", place = GhostPlace.AtTarget, target = "cradle",
                },
                new LessonData
                {
                    id = "free", doneWhen = "wonOrCaught", failWhen = battleFail, rewindTo = "grass", praise = false,
                },
            };
            s.praise = new List<string> { "praise.nice", "praise.like", "praise.perfect" };
            s.handsLine = "lesson.hands";
            s.handsGhost = "hands_up";
            s.repeatSeconds = 10f;
            EditorUtility.SetDirty(s);
            AssetDatabase.SaveAssets();
            return s;
        }
    }
}
