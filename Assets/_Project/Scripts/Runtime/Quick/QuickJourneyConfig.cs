using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// The Quick Journey's dials (step 9g; Data/QuickJourney.asset): the judges' ~10 minute route through the same game
    /// in its own save slot. Only applied while the quick slot plays (<see cref="QuickJourney"/>); the Full adventure's
    /// numbers stay as they are. Tune here, no code.
    /// </summary>
    [CreateAssetMenu(menuName = "Pocket Isle/Quick Journey Config", fileName = "QuickJourney")]
    public class QuickJourneyConfig : ScriptableObject
    {
        [Header("Lessons")]
        [Tooltip("Lesson ids skipped in the quick slot (every lesson that teaches a hand verb stays).")]
        public string[] cutLessons = { "twoHands" };

        [Header("Dials in the quick realm (Galewick since step 14c)")]
        [Tooltip("Nov 3 (no gift any more): the quick realm's Keeper is this many levels off in the quick slot (negative = easier).")]
        public int keeperLevelOffset = -2;
        [Tooltip("x the tall-grass encounter chance.")]
        [Range(0f, 2f)] public float encounterRate = 0.5f;
        [Tooltip("x the win XP, on top of the normal curve.")]
        [Min(0f)] public float xpMultiplier = 2f;
        [Tooltip("x the weight of rare entries (0 = no rare surprises, so no rare-only detours).")]
        [Min(0f)] public float rareWeight = 0f;
        [Tooltip("Gold tiles toward the next goal (wind gate, mill, kites, tower, home gate, statue) when the explorer stands still.")]
        public bool goldPathHint = true;
        [Tooltip("Seconds standing still before the gold path shows, and between shows.")]
        public float goldPathAfter = 4f, goldPathEvery = 7f;
        [Tooltip("Gold tiles shown each time.")]
        [Min(1)] public int goldPathTiles = 14;

        [Header("Dry run estimates (Tools > Pocket Isle > Quick Journey Dry Run)")]
        [Tooltip("Shard, unfold, title, Spirit, move and starter (s).")]
        public float openingSeconds = 75f;
        [Tooltip("Walk, look, grass lessons (s, before the tutorial battle).")]
        public float lessonSeconds = 70f;
        [Tooltip("One wild battle including the zoom in and out (s); the tutorial battle teaches, so it counts 1.5x.")]
        public float battleSeconds = 45f;
        [Tooltip("Seconds per tile walked (auto-walk 0.4; players stop and look around).")]
        public float walkSecondsPerTile = 0.5f;
        public float giftSeconds = 15f, gateSeconds = 6f;
        [Tooltip("Step 14c, Galewick: the arrival sweep, the mill flick, flying the kites (on top of the walk), tying them at the tower, the untangle (s).")]
        public float arriveSeconds = 30f, millSeconds = 40f, kitesSeconds = 75f, towerSeconds = 30f, untangleSeconds = 60f;
        [Tooltip("Galeowl: meeting, fight, shard (s).")]
        public float keeperSeconds = 150f;
        [Tooltip("The shard into the statue, the whisper, the end card (s).")]
        public float statueSeconds = 40f;
        [Tooltip("Spirit lines along the trail (arrival, village, mill, kites, ridge) (s).")]
        public float trailTalkSeconds = 25f;
    }
}
