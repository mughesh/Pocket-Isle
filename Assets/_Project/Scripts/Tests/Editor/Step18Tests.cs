using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>Nov 7 headset notes: the Hall of Fame keeps the explorer's spot free; the Spirit's lines are now or never.</summary>
    public class Step18Tests
    {
        readonly List<Object> _made = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _made)
                if (o != null) Object.DestroyImmediate(o);
            _made.Clear();
        }

        Dialogue MakeDialogue()
        {
            var script = ScriptableObject.CreateInstance<DialogueScript>();
            _made.Add(script);
            DialogueTrigger T(string id, string target = null, int lines = 1)
            {
                var t = new DialogueTrigger { id = id, priority = 2, target = target, lines = new List<DialogueLine>() };
                for (int i = 0; i < lines; i++) t.lines.Add(new DialogueLine { text = id + " " + i });
                return t;
            }
            script.triggers.Add(T("lesson"));
            script.triggers.Add(T("story"));
            script.triggers.Add(T("seq", null, 3));
            var go = new GameObject("Dialogue Test");
            _made.Add(go);
            var d = go.AddComponent<Dialogue>();
            d.script = script;
            return d;
        }

        [Test]
        public void StoryLine_HeldBackByALesson_GoesStale_NotSaidLate()
        {
            var d = MakeDialogue();
            d.PinLines(new[] { "lesson" }, "Do it");
            d.Fire("lesson", false);
            d.Fire("story", false);
            d.DebugAge(d.staleAfter * 0.5f);
            d.UnpinLines();
            Assert.AreEqual("story", d.CurrentTrigger, "held back only briefly: it still shows after the lesson");

            d.ClearAll();
            d.PinLines(new[] { "lesson" }, "Do it");
            d.Fire("lesson", false);
            d.Fire("story", true);
            d.DebugAge(d.staleAfter + 0.1f);
            d.UnpinLines();
            Assert.AreNotEqual("story", d.CurrentTrigger, "held back past staleAfter: dropped, not said late");
        }

        [Test]
        public void Sequence_NotHeldBack_DoesNotAge()
        {
            var d = MakeDialogue();
            d.Fire("seq", false);
            d.DebugAge(60f);
            d.DebugFinishLine();
            Assert.AreEqual("seq", d.CurrentTrigger, "nothing holds it back (no lesson, card or battle): the second line still comes");
        }

        [Test]
        public void HallArc_KeepsExplorerFree_NoOverlap_InFront()
        {
            var c = new Vector2(30f, 30f);
            var explorer = c + new Vector2(0f, 3f);
            for (int n = 1; n <= 6; n++)
            {
                const float w = 1.3f, minR = 2.65f;
                var spots = FinaleLogic.HallArc(c, n, w, minR, 1.4f, explorer);
                Assert.AreEqual(n, spots.Length);
                if (n > 1) Assert.GreaterOrEqual(FinaleLogic.MinGap(spots), 1.4f * w - 1e-3f, $"n={n}: centres too close");
                foreach (var s in spots)
                {
                    Assert.GreaterOrEqual(Vector2.Distance(s, explorer), 1.4f * w * 0.9f, $"n={n}: on the explorer");
                    Assert.GreaterOrEqual(Vector2.Distance(s, c), minR - 1e-3f, $"n={n}: inside the statue clearance");
                    Assert.LessOrEqual(Vector2.Angle(s - c, Vector2.up), 100.01f, $"n={n}: behind the statue");
                }
            }
        }
    }
}
