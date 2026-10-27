using System;
using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>Where a lesson's ghost hand shows.</summary>
    public enum GhostPlace
    {
        None,
        /// <summary>At the lesson's dialogue target (shard, rim, pods, runes, cradle, a tile), reaching in from the player's side.</summary>
        AtTarget,
        /// <summary>Floating above the island's front centre (thumb demos), the Spirit beside it.</summary>
        AboveIsland,
        /// <summary>Floating above the lesson's target (a gold tile).</summary>
        AboveTarget,
        /// <summary>
        /// Palm on the side of the island's rocky base (step 8d, the palm grab): one hand on its own side, both hands on
        /// the left and right sides. The clip is turned so its palm faces the island.
        /// </summary>
        IslandSides,
        /// <summary>
        /// Step 9h (the pinch lesson): right beside the player's own dominant hand, following it (~8 cm to the outside,
        /// at hand height). Without a tracked hand, where a resting hand would be.
        /// </summary>
        BesideHand,
        /// <summary>
        /// Step 9h (after the headset test): both hands grab the near rim's corners, the right hand the near-right one,
        /// the left (mirrored) the near-left one, each pinch landing on its corner (the grab clip pulls apart: resize).
        /// </summary>
        RimCorners,
    }

    /// <summary>
    /// One lesson: the Spirit's line (repeated while it waits), what the gold focus and the ghost hand show, and when it
    /// starts, counts as done, gets skipped or goes back to an earlier lesson. Conditions are <see cref="GameConditions"/>
    /// expressions over states (battle, canWalk, ...) and the lesson events marked with <see cref="Onboarding.Mark"/>
    /// (walked3, stopped, ...); an event that happened earlier counts (doing it ahead of time = done on arrival).
    /// </summary>
    [Serializable]
    public class LessonData
    {
        public string id;
        [Tooltip("Dialogue trigger said when the lesson starts.")]
        public string line;
        [Tooltip("Shorter trigger for the reminders (every repeatSeconds while waiting). Empty = the line again.")]
        public string again;
        [Tooltip("Step 9h: the hint chip the lesson's line shrinks to after a few seconds (a short verb: \"Grab the edge\"). Empty = the line itself.")]
        public string chip;
        [Tooltip("Optional trigger said once after helpAfter seconds (walk lessons: the look + pinch alternative).")]
        public string help;
        public float helpAfter = 30f;

        [Header("When")]
        [Tooltip("Waits until this holds (empty = at once).")]
        public string startWhen;
        [Tooltip("Then waits this long before teaching (it can still be done meanwhile).")]
        public float delay;
        [Tooltip("Done (with praise) when this holds; events that happened earlier count.")]
        public string doneWhen;
        [Tooltip("Skipped quietly once this stops holding (empty = always valid).")]
        public string validWhile;
        [Tooltip("Skipped quietly after teaching this long (s). 0 = waits forever.")]
        public float skipAfter;
        [Tooltip("Goes back to rewindTo when this holds (a battle lost or fled mid-tutorial).")]
        public string failWhen;
        public string rewindTo;
        public bool praise = true;

        [Header("Show")]
        [Tooltip("Ghost hand clip name (GhostHandClip.clipName). Empty = no ghost.")]
        public string ghost;
        public GhostPlace place;
        [Tooltip("Dialogue target id the ghost and the Spirit use (shard, rim, starterPods, ghost, goldTile, leadPod, runes, cradle, landmark:x).")]
        public string target;
        [Tooltip("Step 9h, grab clips: the dialogue target the grab frame lands on (the pod, the rim corner); the release frame then lands on target (empty = no release spot). '#' becomes the loop number (starterPod:#).")]
        public string source;
        [Tooltip("The release spot sits this far above the target (m): a toss over the pad.")]
        public float targetLift;
        [Tooltip("Ghost size: 1 = real hand, 0.6 for the demos above the island.")]
        public float ghostScale = 1f;
        [Tooltip("Show both hands (mirrored).")]
        public bool bothHands;
        [Tooltip("Both hands: how far apart (m). 0 = the ghost's default.")]
        public float ghostSpread;

        [Header("Extras")]
        [Tooltip("While this lesson waits, gold tiles run from the explorer to this landmark.")]
        public string goldPath;
        [Tooltip("Picks a gold tile this many steps from the explorer when it starts (target goldTile). 0 = none.")]
        public int goldTileSteps;
        [Tooltip("Step 12a: picks the gold tile from a smart-step plan instead: ahead (3-4 tiles along the path), turn (round a corner), far (the end of a long straight stretch). The smart step stops on it.")]
        public string goldTileRule;
        [Tooltip("While this lesson is the current one, the tutorial grass patch starts the scripted first encounter.")]
        public bool tutorialGrass;
    }

    /// <summary>The opening's lessons in order (Data/Onboarding/Onboarding_Main), run one at a time by <see cref="Onboarding"/>.</summary>
    [CreateAssetMenu(menuName = "Pocket Isle/Onboarding Script", fileName = "Onboarding")]
    public class OnboardingScript : ScriptableObject
    {
        public List<LessonData> lessons = new List<LessonData>();
        [Tooltip("Rotating praise triggers said when a lesson is done.")]
        public List<string> praise = new List<string> { "praise.nice", "praise.like", "praise.perfect" };
        [Tooltip("Said when no hand is tracked for a while during a lesson.")]
        public string handsLine = "lesson.hands";
        [Tooltip("Ghost clip for the hands check (both hands).")]
        public string handsGhost = "hands_up";
        [Tooltip("Unused since step 9h (the lesson line stays until the lesson is done).")]
        public float repeatSeconds = 10f;

        public int IndexOf(string id)
        {
            for (int i = 0; i < lessons.Count; i++)
                if (lessons[i].id == id) return i;
            return -1;
        }
    }
}
