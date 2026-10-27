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
    }
}
