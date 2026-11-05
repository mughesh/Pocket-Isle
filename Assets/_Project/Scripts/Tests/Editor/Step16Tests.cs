using NUnit.Framework;
using UnityEngine;

namespace Diorama.Tests
{
    public class Step16Tests
    {
        [Test]
        public void GhostFades_WhenAHandIsNear()
        {
            Assert.AreEqual(1f, GhostHands.NearAlpha(0f, 0.08f), 1e-5f);
            Assert.AreEqual(0.08f, GhostHands.NearAlpha(1f, 0.08f), 1e-5f);
            Assert.AreEqual(0.54f, GhostHands.NearAlpha(0.5f, 0.08f), 1e-5f);
            Assert.AreEqual(0.08f, GhostHands.NearAlpha(3f, 0.08f), 1e-5f, "clamped");
        }

        [Test]
        public void AsideGhost_SitsAcrossTheLineOfSight()
        {
            Vector3 d = GhostHands.AsideDir(new Vector3(0.3f, 1f, 2f), false);
            Assert.AreEqual(0f, d.y, 1e-5f);
            Assert.AreEqual(1f, d.magnitude, 1e-4f);
            Assert.AreEqual(-d, GhostHands.AsideDir(new Vector3(0.3f, 1f, 2f), true));
        }
    
        [Test]
        public void ShortLines_ShowAtOnce_LongOnesType()
        {
            Assert.AreEqual(20, DialogueBubble.VisibleChars(20, 0f, 60f, true));
            Assert.AreEqual(0, DialogueBubble.VisibleChars(80, 0f, 60f, false));
            Assert.AreEqual(30, DialogueBubble.VisibleChars(80, 0.5f, 60f, false));
            Assert.AreEqual(80, DialogueBubble.VisibleChars(80, 5f, 60f, false));
        }

        [Test]
        public void ObjectiveArea_BlocksInTheView()
        {
            var head = Vector3.zero;
            var spheres = new System.Collections.Generic.List<Vector4> { new Vector4(0f, 0f, 1f, 0.05f) };
            Assert.IsTrue(ObjectiveArea.Blocks(head, new Vector3(0f, 0f, 0.5f), 0.02f, spheres), "in front of it");
            Assert.IsTrue(ObjectiveArea.Blocks(head, new Vector3(0.05f, 0f, 1f), 0.02f, spheres), "touching");
            Assert.IsFalse(ObjectiveArea.Blocks(head, new Vector3(0.2f, 0f, 1f), 0.02f, spheres), "beside it");
        }
    
        [Test]
        public void ShardShimmer_GrowsAsAHandComesNear()
        {
            Assert.AreEqual(0f, Opening.ShimmerAmount(float.PositiveInfinity, 0.06f, 0.4f), 1e-5f, "no hands");
            Assert.AreEqual(0f, Opening.ShimmerAmount(0.5f, 0.06f, 0.4f), 1e-5f);
            Assert.AreEqual(1f, Opening.ShimmerAmount(0.03f, 0.06f, 0.4f), 1e-5f);
            float mid = Opening.ShimmerAmount(0.23f, 0.06f, 0.4f);
            Assert.That(mid, Is.InRange(0.3f, 0.7f));
        }

        [Test]
        public void MenuStone_IsSmall_AndTheMenuLineNamesIt()
        {
            Assert.That(MenuStone.Size.x, Is.InRange(0.02f, 0.04f));
            var script = UnityEditor.AssetDatabase.LoadAssetAtPath<DialogueScript>("Assets/_Project/Data/Dialogue/Script_v1.asset");
            var t = script.triggers.Find(x => x.id == "menu.first");
            StringAssert.Contains("Poke the stone on the rim", t.lines[0].text);
        }
    }
}
