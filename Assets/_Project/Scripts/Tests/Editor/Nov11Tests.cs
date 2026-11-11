using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>Mugesh's Nov 11 headset notes: text push-back only on bubbles, upright labels, rings, blooms, the 3-Isling warning.</summary>
    public class Nov11Tests
    {
        const string Runtime = "Assets/_Project/Scripts/Runtime";

        [Test]
        public void KeepTextAway_OnlyOnTheSpeechBubble()
        {
            var users = Directory.GetFiles(Runtime, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.EndsWith("KeepTextAway.cs") && File.ReadAllText(f).Contains("KeepTextAway.On("))
                .Select(Path.GetFileName).ToArray();
            CollectionAssert.AreEquivalent(new[] { "DialogueBubble.cs" }, users,
                "the move tablets, caption, cards and chips stay put (they went into the island when he leaned in)");
        }

        [Test]
        public void BattleLabels_StayUpright_NeverRollWithTheHead()
        {
            foreach (var f in new[] { "Battle/BattleHud.cs", "Battle/BattleRunes.cs" })
                StringAssert.DoesNotContain("cam.transform.up", File.ReadAllText(Path.Combine(Runtime, f)), f);
        }

        [Test]
        public void LabelUp_IgnoresAHeadRoll()
        {
            var go = new GameObject("test cam");
            try
            {
                var cam = go.AddComponent<Camera>();
                go.transform.rotation = Quaternion.Euler(40f, 20f, 0f);
                var level = BattleHud.LabelUp(cam);
                go.transform.rotation = Quaternion.Euler(40f, 20f, 25f); // the same look, head tilted
                var rolled = BattleHud.LabelUp(cam);
                Assert.Less(Vector3.Angle(level, rolled), 0.5f);
                Assert.Less(Vector3.Angle(level, Quaternion.Euler(40f, 20f, 0f) * Vector3.up), 0.5f, "no roll: the head's own up");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Bloom_StopsAtTheClosestTile_OnlyApproaching()
        {
            var bloom = new Vector2Int(0, 0);
            // A 2-wide road: the far lane runs 2 tiles off the bloom, walking +x.
            Assert.IsFalse(HealBloom.ClosestApproach(new Vector2Int(-3, 2), new Vector2Int(-2, 2), bloom, 2.3f), "still getting nearer");
            Assert.IsFalse(HealBloom.ClosestApproach(new Vector2Int(-2, 2), new Vector2Int(-1, 2), bloom, 2.3f), "still getting nearer");
            Assert.IsTrue(HealBloom.ClosestApproach(new Vector2Int(-1, 2), new Vector2Int(0, 2), bloom, 2.3f), "abreast: stop");
            Assert.IsFalse(HealBloom.ClosestApproach(new Vector2Int(0, 2), new Vector2Int(1, 2), bloom, 2.3f), "walking away");
            Assert.IsFalse(HealBloom.ClosestApproach(new Vector2Int(-1, 4), new Vector2Int(0, 4), bloom, 2.3f), "too far");
            Assert.IsFalse(HealBloom.ClosestApproach(new Vector2Int(0, 1), new Vector2Int(0, 0), bloom, 2.3f), "onto it heals instead");
        }

        [Test]
        public void Bloom_NamesAndPuffs_PerRealm()
        {
            Assert.AreEqual("dandelion", HealBloom.NameFor("galewick"));
            Assert.AreEqual("ember-lily", HealBloom.NameFor("cinderwood"));
            Assert.AreEqual("lotus", HealBloom.NameFor("lilymere"));
            var g = HealFx.For("galewick");
            var c = HealFx.For("cinderwood");
            var l = HealFx.For("lilymere");
            Assert.AreNotEqual(g.main, c.main);
            Assert.AreNotEqual(c.main, l.main);
            Assert.Greater(c.rise, g.rise, "embers leap, seeds drift");
            Assert.Greater(g.wind, c.wind, "seeds ride the wind");
        }

        [Test]
        public void Bloom_MeetLine_InTheScript()
        {
            var s = UnityEditor.AssetDatabase.LoadAssetAtPath<DialogueScript>("Assets/_Project/Data/Dialogue/Script_v1.asset");
            Assert.IsNotNull(s);
            var e = s.Find(HealBloom.MeetLine);
            Assert.IsNotNull(e, "heal.bloom.meet");
            StringAssert.Contains("{bloom}", e.lines[0].text);
            StringAssert.Contains("Poke", e.lines[0].text);
        }

        [Test]
        public void Temple_ThreeIslings_WarnsEverywhere_NeverAWall()
        {
            Assert.AreEqual(3, TempleRules.Needed(true, false));
            Assert.AreEqual(3, TempleRules.Needed(false, false));
            Assert.AreEqual(2, TempleRules.Needed(true, true), "Nov 11 panel: 2 in the quick slot only");
            Assert.IsFalse(TempleRules.CanEnter(2, 3));
            Assert.IsTrue(TempleRules.CanEnter(3, 3));
            Assert.IsTrue(TempleRules.CanEnter(2, 2), "the quick party of two walks straight in");
        }

        [Test]
        public void PodRing_SlimEnough_NotToReachTheNextPod()
        {
            // Pods sit ~2.4 radii apart on the rim; the ring's outer edge (0.95 of half its size) must stay well inside that.
            Assert.Less(PodHpRings.DefaultSize * 0.95f, 1.4f);
        }
    }
}
