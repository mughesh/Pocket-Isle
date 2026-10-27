using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>
    /// Step 13: the statue's whispers by element and shard count, its wake stages, the finale's resume rules, the palm
    /// goodbye with a synthetic palm, Hall of Fame data (old saves too), collection counts and the Keeper rematch level.
    /// </summary>
    public class Step13Tests
    {
        [SetUp, TearDown]
        public void Clear() => PuzzleState.Restore(new List<SavedPuzzle>());

        [Test]
        public void Whisper_PerElement_ThenTheSpiritByCount()
        {
            CollectionAssert.AreEqual(new[] { "heart.dream.fire", "spirit.dream.1" }, FinaleLogic.WhisperLines(ShardHolder.FireId, 1));
            CollectionAssert.AreEqual(new[] { "heart.dream.wind", "spirit.dream.2" }, FinaleLogic.WhisperLines(ShardHolder.WindId, 2));
            CollectionAssert.AreEqual(new[] { "heart.dream.water" }, FinaleLogic.WhisperLines(ShardHolder.WaterId, 3), "the third starts the finale instead");
            CollectionAssert.AreEqual(new[] { "heart.dream.water", "spirit.dream.1" }, FinaleLogic.WhisperLines(ShardHolder.WaterId, 1), "any realm order");
        }

        [Test]
        public void Whisper_LinesAreInTheScript()
        {
            var script = UnityEditor.AssetDatabase.LoadAssetAtPath<DialogueScript>("Assets/_Project/Data/Dialogue/Script_v1.asset");
            Assert.IsNotNull(script);
            foreach (var id in new[] { "heart.dream.fire", "heart.dream.wind", "heart.dream.water", "spirit.dream.1", "spirit.dream.2" })
            {
                var t = script.Find(id);
                Assert.IsNotNull(t, id);
                Assert.IsFalse(t.objective, id + " is chatter");
            }
            Assert.AreEqual(DialogueSpeaker.Heart, script.Find("heart.dream.fire").lines[0].speaker);
            Assert.AreEqual(DialogueSpeaker.Spirit, script.Find("spirit.dream.1").lines[0].speaker);
        }

        [Test]
        public void Statue_WakesWithEachShard()
        {
            Assert.AreEqual(0, FinaleLogic.StatueStage(0));
            Assert.AreEqual(1, FinaleLogic.StatueStage(1));
            Assert.AreEqual(2, FinaleLogic.StatueStage(2));
            Assert.AreEqual(3, FinaleLogic.StatueStage(3));
            Assert.AreEqual(3, FinaleLogic.StatueStage(5));
        }

        // ---- the finale's state machine ----

        [Test]
        public void Finale_StartsWithTheThirdShard_ResumesAtTheGoodbye()
        {
            Assert.AreEqual(FinaleStep.None, FinaleLogic.ResumeStep(2, 0, false), "two shards: nothing yet");
            Assert.AreEqual(FinaleStep.Wake, FinaleLogic.ResumeStep(3, 0, false), "the third shard: from the wake");
            foreach (var step in new[] { FinaleStep.Wake, FinaleStep.Understanding, FinaleStep.Goodbye, FinaleStep.EyesOpen, FinaleStep.HallOfFame, FinaleStep.Credits, FinaleStep.Spark })
                Assert.AreEqual(FinaleStep.Goodbye, FinaleLogic.ResumeStep(3, (int)step, false), $"quit during {step}: the Spirit asks again");
            Assert.AreEqual(FinaleStep.Done, FinaleLogic.ResumeStep(3, (int)FinaleStep.Done, true));
            Assert.AreEqual(FinaleStep.Done, FinaleLogic.ResumeStep(3, 0, true), "done stays done");
        }

        [Test]
        public void Finale_StepsInOrder_LockWhilePlaying()
        {
            var s = FinaleStep.Wake;
            var seen = new List<FinaleStep> { s };
            while (s != FinaleStep.Done) seen.Add(s = FinaleLogic.Next(s));
            CollectionAssert.AreEqual(new[] { FinaleStep.Wake, FinaleStep.Understanding, FinaleStep.Goodbye, FinaleStep.EyesOpen, FinaleStep.HallOfFame,
                FinaleStep.Credits, FinaleStep.Spark, FinaleStep.Done }, seen);
            Assert.IsFalse(FinaleLogic.Locks(FinaleStep.None));
            Assert.IsTrue(FinaleLogic.Locks(FinaleStep.Goodbye));
            Assert.IsFalse(FinaleLogic.Locks(FinaleStep.Done), "free play after");
        }

        [Test]
        public void Finale_SavedProgress_RoundTrips()
        {
            PuzzleState.SetStage(FinaleLogic.StageId, (int)FinaleStep.HallOfFame);
            var saved = PuzzleState.ToSaved();
            PuzzleState.Restore(new List<SavedPuzzle>());
            Assert.AreEqual(0, PuzzleState.Stage(FinaleLogic.StageId));
            PuzzleState.Restore(saved);
            Assert.AreEqual(FinaleStep.Goodbye, FinaleLogic.ResumeStep(3, PuzzleState.Stage(FinaleLogic.StageId), PuzzleState.IsSolved(FinaleLogic.DoneId)));
            PuzzleState.Solve(FinaleLogic.DoneId, 1);
            Assert.AreEqual(FinaleStep.Done, FinaleLogic.ResumeStep(3, PuzzleState.Stage(FinaleLogic.StageId), PuzzleState.IsSolved(FinaleLogic.DoneId)));
        }

        // ---- the palm goodbye (synthetic palm) ----

        static readonly Vector3 Socket = new Vector3(0f, 1.2f, 0.5f);

        static string Run(PalmHandoff h, float seconds, bool tracked, float up, bool open, Vector3 palm, bool gaze = false)
        {
            string said = null;
            bool first = true;
            for (float t = 0f; t < seconds; t += 0.05f)
            {
                var line = h.Step(0.05f, tracked, up, open, palm, Socket, 1f, gaze && first);
                first = false;
                if (line != null) said = line;
            }
            return said;
        }

        [Test]
        public void Palm_OpenHandUp_TheSpiritLands_LiftedToTheSocket_ItGoes()
        {
            var h = new PalmHandoff();
            h.Reset();
            Assert.IsNull(Run(h, 0.2f, true, 0.9f, true, new Vector3(0f, 1f, 0.9f)), "not yet: held up a moment first");
            Assert.AreEqual("spirit.finale.hand", Run(h, 0.3f, true, 0.9f, true, new Vector3(0f, 1f, 0.9f)));
            Assert.AreEqual(PalmHandoff.State.OnPalm, h.Now);
            Assert.IsNull(Run(h, 1f, true, 0.9f, true, Socket + new Vector3(0f, -0.1f, 0.1f)), "14 cm away: it stays on the palm");
            Assert.AreEqual("spirit.finale.3", Run(h, 0.1f, true, 0.9f, true, Socket + new Vector3(0f, -0.03f, 0.03f)), "within 6 cm: it floats in");
            Assert.AreEqual(PalmHandoff.State.ToSocket, h.Now);
        }

        [Test]
        public void Palm_DownOrAFist_NoLanding_TurnedAway_ItFloatsBack()
        {
            var h = new PalmHandoff();
            h.Reset();
            Assert.IsNull(Run(h, 1f, true, -0.8f, true, Vector3.zero), "palm down");
            Assert.IsNull(Run(h, 1f, true, 0.9f, false, Vector3.zero), "a fist");
            Assert.IsNull(Run(h, 1f, false, 0.9f, true, Vector3.zero), "untracked");
            Assert.AreEqual(PalmHandoff.State.Waiting, h.Now);
            Run(h, 0.5f, true, 0.9f, true, Vector3.zero);
            Assert.AreEqual(PalmHandoff.State.OnPalm, h.Now);
            Run(h, 1f, true, -0.5f, true, Vector3.zero);
            Assert.AreEqual(PalmHandoff.State.Waiting, h.Now, "turned over: back to wait in front of the Heart");
        }

        [Test]
        public void Palm_Idle30s_TheWaitLine_NeverForced_GazePinchGoesByItself()
        {
            var h = new PalmHandoff();
            h.Reset();
            Assert.AreEqual("spirit.finale.wait", Run(h, 30.1f, false, 0f, false, Vector3.zero));
            Assert.AreEqual(PalmHandoff.State.Waiting, h.Now, "it only waits");
            Assert.AreEqual("spirit.finale.3", Run(h, 1.6f, false, 0f, false, Vector3.zero, true), "look + pinch the Heart: by itself after a moment");
            Assert.AreEqual(PalmHandoff.State.ToSocket, h.Now);
        }

        // ---- Hall of Fame data ----

        [Test]
        public void HallOfFame_MetIn_RoundTripsAndOldSavesHaveNone()
        {
            var species = ScriptableObject.CreateInstance<IslingSpecies>();
            species.name = "Testling";
            species.displayName = "Testling";
            try
            {
                var c = new IslingInstance(species, 12) { metIn = "Galewick" };
                var saved = SaveGame.ToSaved(c);
                Assert.AreEqual("Galewick", saved.metIn);
                var data = new SaveData();
                data.party.Add(saved);
                data.hallOfFame.Add(new SavedHallOfFame { date = "2026-11-03", party = new List<SavedIsling> { saved } });
                var back = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data));
                Assert.AreEqual("Galewick", back.party[0].metIn);
                Assert.AreEqual(1, back.hallOfFame.Count);
                Assert.AreEqual(12, back.hallOfFame[0].party[0].level);
                // A save from before step 13: no Hall of Fame, no "met in".
                var old = JsonUtility.FromJson<SaveData>("{\"version\":3,\"party\":[{\"species\":\"Kilnpot\",\"level\":5}]}");
                Assert.IsNotNull(old.hallOfFame);
                Assert.AreEqual(0, old.hallOfFame.Count);
                Assert.IsTrue(string.IsNullOrEmpty(old.party[0].metIn));
            }
            finally
            {
                Object.DestroyImmediate(species);
            }
        }

        [Test]
        public void HallOfFame_OldSaves_GuessByRoster_StartersOnTheStillIsle()
        {
            var a = ScriptableObject.CreateInstance<IslingSpecies>();
            var b = ScriptableObject.CreateInstance<IslingSpecies>();
            try
            {
                var rosters = new Dictionary<string, HashSet<IslingSpecies>> { ["Cinderwood"] = new HashSet<IslingSpecies> { a } };
                Assert.AreEqual("Cinderwood", FinaleLogic.GuessMetIn(a, rosters));
                Assert.AreEqual(FinaleLogic.StillIsle, FinaleLogic.GuessMetIn(b, rosters), "not in a realm's roster: a starter or a gift");
                Assert.AreEqual("Lilymere", FinaleLogic.RealmName("lilymere"));
                Assert.AreEqual(FinaleLogic.StillIsle, FinaleLogic.RealmName("stillisle"));
                Assert.AreEqual("Kilnpot · Lv 18\nmet in Cinderwood", FinaleLogic.Card("Kilnpot", 18, "Cinderwood"));
            }
            finally
            {
                Object.DestroyImmediate(a);
                Object.DestroyImmediate(b);
            }
        }

        // ---- collection (post-game) ----

        [Test]
        public void Collection_RealmRosters_IncludeEvolutions_NoKeepers()
        {
            foreach (var path in new[] { "Assets/_Project/Data/WorldMap_Cinderwood.asset", "Assets/_Project/Data/WorldMap_Galewick.asset", "Assets/_Project/Data/WorldMap_Lilymere.asset" })
            {
                var map = UnityEditor.AssetDatabase.LoadAssetAtPath<WorldMap>(path);
                Assert.IsNotNull(map, path);
                var roster = FinaleLogic.Roster(map);
                Assert.Greater(roster.Count, 5, path);
                foreach (var s in roster)
                {
                    Assert.IsFalse(s.keeper, $"{path}: {s.name} is a Keeper");
                    if (s.evolvesTo != null) Assert.IsTrue(roster.Contains(s.evolvesTo), $"{path}: {s.name} evolves into {s.evolvesTo.name}, missing");
                }
                Assert.AreEqual(0, FinaleLogic.CaughtOf(roster, new List<string>()));
                var all = new List<string>();
                foreach (var s in roster) all.Add(SpeciesRegistry.IdOf(s));
                Assert.AreEqual(roster.Count, FinaleLogic.CaughtOf(roster, all));
            }
            Assert.AreEqual("Cinderwood 9/12 · Galewick 8/11", FinaleLogic.CollectionLine(new List<(string, int, int)> { ("Cinderwood", 9, 12), ("Galewick", 8, 11) }));
        }

        [Test]
        public void Rematch_FiveLevelsOverTheLastFight()
        {
            Assert.AreEqual(14, FinaleLogic.RematchLevel(9));
            Assert.AreEqual(6, FinaleLogic.RematchLevel(0));
        }
    }
}
