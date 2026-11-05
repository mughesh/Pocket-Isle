using System;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// The Quick Journey's rules (step 9g): active only while the quick save slot plays. Systems ask here instead of
    /// reading the config: the encounter chance, XP and rare weight in the quick realm (Galewick since step 14c; <see cref="EncounterDirector"/>,
    /// <see cref="EncounterTable"/>) and the lessons cut (<see cref="LessonRunner.Skip"/>). The pure overloads (explicit
    /// "quick" flag) are what the tests check. The route's timer lives in PlayerPrefs (opening start to the shard in the statue).
    /// </summary>
    public static class QuickJourney
    {
        public const string GiftId = "quick.gift", EndId = "quick.end";
        const string TimeKey = "pocketisle.quickSeconds", DoneKey = "pocketisle.quickDone";

        /// <summary>Set by <see cref="QuickJourneyDirector"/> while the scene has one.</summary>
        public static QuickJourneyConfig Config { get; internal set; }

        /// <summary>The director that set <see cref="Config"/> (only it clears it).</summary>
        internal static UnityEngine.Object Owner { get; set; }

        /// <summary>The explorer is in the quick realm (Galewick, outside or in the tower hall); set by the director every frame.</summary>
        public static bool InQuickRealm { get; internal set; }

        /// <summary>Step 14c: the quick route is done (the wind shard home, the end card shown): the full adventure's guidance takes over.</summary>
        public static bool Finished => PuzzleState.IsSolved(EndId);

        public static bool Active => Config != null && SaveSlots.Active == SaveSlot.Quick;

        static bool Dials => InQuickRealm && Active;

        // ---- dials (live) ----

        public static float EncounterChance(float baseChance) => EncounterChance(baseChance, Dials, Config);

        public static int Reward(int reward) => Reward(reward, Dials, Config);

        public static float RareScale => RareScaleFor(Dials, Config);

        public static bool SkipsLesson(string id) => SkipsLesson(id, Active, Config);

        // ---- dials (pure, tested) ----

        public static float EncounterChance(float baseChance, bool quick, QuickJourneyConfig c) =>
            quick && c != null ? Mathf.Clamp01(baseChance * c.encounterRate) : baseChance;

        public static int Reward(int reward, bool quick, QuickJourneyConfig c) =>
            quick && c != null ? Mathf.Max(1, Mathf.RoundToInt(reward * c.xpMultiplier)) : reward;

        public static float RareScaleFor(bool quick, QuickJourneyConfig c) => quick && c != null ? c.rareWeight : 1f;

        /// <summary>
        /// Step 12b (Mugesh's Oct 27 night note): the gift fires only in the quick slot, once, and only before the first realm
        /// is visited; a player who leaves the hub first never gets it late (stage 2 = skipped).
        /// </summary>
        public static bool GiftAllowed(bool quickSlot, bool giftDone, bool realmVisited) => quickSlot && !giftDone && !realmVisited;

        /// <summary>The gift's puzzle stage when it was skipped (a realm visited first).</summary>
        public const int GiftSkipped = 2;
        /// <summary>Nov 3: the send-off that replaced the gift (no Islings given).</summary>
        public const int SentOff = 3;

        /// <summary>Nov 3: the quick realm's Keeper this many levels off (negative = easier) in the quick slot (pure).</summary>
        public static int KeeperLevel(int level, bool quick, bool quickRealm, QuickJourneyConfig c) =>
            quick && quickRealm && c != null ? Mathf.Max(1, level + c.keeperLevelOffset) : level;

        public static int KeeperLevel(int level) => KeeperLevel(level, Active, InQuickRealm, Config);

        /// <summary>
        /// Nov 5 (Mugesh): the quick Keeper stays easy, but a strong team (top-3 average above
        /// <see cref="QuickJourneyConfig.strongTeamLevel"/>) gets 1 extra level per 2 above it, up to
        /// <see cref="QuickJourneyConfig.strongTeamMaxExtra"/> (pure). A typical quick team (Lv 7) adds nothing.
        /// </summary>
        public static int StrongTeamExtra(float teamLevel, bool quick, bool quickRealm, QuickJourneyConfig c)
        {
            if (!quick || !quickRealm || c == null) return 0;
            return Mathf.Clamp(Mathf.FloorToInt((teamLevel - c.strongTeamLevel) / 2f), 0, c.strongTeamMaxExtra);
        }

        public static int StrongTeamExtra(float teamLevel) => StrongTeamExtra(teamLevel, Active, InQuickRealm, Config);

        public static bool SkipsLesson(string id, bool quick, QuickJourneyConfig c) =>
            quick && c != null && c.cutLessons != null && Array.IndexOf(c.cutLessons, id) >= 0;

        // ---- timer ----

        public static float Seconds
        {
            get => PlayerPrefs.GetFloat(TimeKey, 0f);
            internal set => PlayerPrefs.SetFloat(TimeKey, value);
        }

        public static bool TimerDone
        {
            get => PlayerPrefs.GetInt(DoneKey, 0) == 1;
            internal set => PlayerPrefs.SetInt(DoneKey, value ? 1 : 0);
        }

        public static string Format(float seconds) => $"{Mathf.FloorToInt(seconds / 60f)}:{Mathf.FloorToInt(seconds % 60f):00}";
    }
}
