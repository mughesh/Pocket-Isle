using NUnit.Framework;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>Step 14k: the grab band runs from the glowing line inward; nothing outside the line grabs.</summary>
    public class Step14kTests
    {
        const float Outset = 0.008f, Cap = 0.006f;

        static GrabZone.Shape Shape(float scale = 0.6f, bool lip = true) => new GrabZone.Shape
        {
            half = new Vector2(0.55f, 0.55f),
            capTop = Cap,
            lineOutset = Outset,
            lineRadius = 0.03f,
            scale = scale,
            outsideWorld = 0.003f,
            insideWorld = 0.035f,
            bandTop = Cap + 0.025f / scale,
            bandBottom = -0.12f,
            cornerSize = 0.1f,
            lipTop = -0.02f,
            lipActive = lip,
        };

        /// <summary>Near the front-right corner on the right edge, this far (world m) outside the line (negative = inside).</summary>
        static Vector3 RightCorner(GrabZone.Shape s, float worldOut, float y = Cap + 0.01f) =>
            new Vector3(s.half.x + Outset + worldOut / s.scale, y, s.half.y - 0.09f); // 9 cm local from the front: the right edge is nearest

        [Test]
        public void Band_GrabsInward_OneAndThreeCentimetres()
        {
            var s = Shape();
            Assert.AreEqual(GrabZone.Kind.Corner, GrabZone.Classify(RightCorner(s, 0f), s), "on the line");
            Assert.AreEqual(GrabZone.Kind.Corner, GrabZone.Classify(RightCorner(s, -0.01f), s), "1 cm inside, above the rim");
            Assert.AreEqual(GrabZone.Kind.Corner, GrabZone.Classify(RightCorner(s, -0.03f), s), "3 cm inside, above the rim");
            Assert.AreEqual(GrabZone.Kind.Corner, GrabZone.Classify(RightCorner(s, -0.03f, -0.04f), s), "3 cm inside, on the rock face");
            Assert.AreEqual(GrabZone.Kind.None, GrabZone.Classify(RightCorner(s, -0.05f), s), "5 cm inside: past the band");
        }

        [Test]
        public void NothingOutsideTheLine()
        {
            var s = Shape();
            Assert.AreEqual(GrabZone.Kind.Corner, GrabZone.Classify(RightCorner(s, 0.002f), s), "2 mm: tracking slack");
            Assert.AreEqual(GrabZone.Kind.None, GrabZone.Classify(RightCorner(s, 0.01f), s), "1 cm outside");
            Assert.AreEqual(GrabZone.Kind.None, GrabZone.Classify(RightCorner(s, 0.01f, -0.04f), s), "1 cm outside, low");
            Assert.AreEqual(GrabZone.Kind.None, GrabZone.Classify(RightCorner(s, 0.03f), s), "3 cm outside");
        }

        [Test]
        public void Band_Height()
        {
            var s = Shape();
            Assert.AreEqual(GrabZone.Kind.Corner, GrabZone.Classify(RightCorner(s, -0.01f, Cap + 0.02f / s.scale), s), "2 cm over the cap");
            Assert.AreEqual(GrabZone.Kind.None, GrabZone.Classify(RightCorner(s, -0.01f, Cap + 0.04f / s.scale), s), "4 cm over the cap");
            Assert.AreEqual(GrabZone.Kind.None, GrabZone.Classify(RightCorner(s, 0f, -0.2f), s), "under the band");
            Assert.AreEqual(GrabZone.Kind.None, GrabZone.Classify(new Vector3(s.half.x, Cap + 0.01f, 0f), s), "the middle of a side");
        }

        [Test]
        public void Band_IsWorldMetres_AtAnyScale()
        {
            foreach (float scale in new[] { 0.3f, 0.6f, 1.2f })
            {
                var s = Shape(scale);
                Assert.AreEqual(GrabZone.Kind.Corner, GrabZone.Classify(RightCorner(s, -0.03f), s), $"scale {scale}: 3 cm in");
                Assert.AreEqual(GrabZone.Kind.None, GrabZone.Classify(RightCorner(s, 0.01f), s), $"scale {scale}: 1 cm out");
            }
        }

        [Test]
        public void Lip_InwardBelowThePods_CornersInBattles()
        {
            var s = Shape();
            var lip = new Vector3(0f, -0.05f, s.half.y + Outset - 0.02f / s.scale);
            Assert.AreEqual(GrabZone.Kind.Lip, GrabZone.Classify(lip, s), "2 cm in from the front line");
            Assert.AreEqual(GrabZone.Kind.None, GrabZone.Classify(new Vector3(0f, -0.05f, s.half.y + Outset + 0.01f / s.scale), s), "1 cm in front");
            Assert.AreEqual(GrabZone.Kind.None, GrabZone.Classify(new Vector3(0f, -0.01f, lip.z), s), "up at the pods");
            Assert.AreEqual(GrabZone.Kind.None, GrabZone.Classify(lip, Shape(lip: false)), "battles and the lodge: corners only");
        }

        [Test]
        public void FrameLightsByTheSameZone()
        {
            var s = Shape();
            Assert.AreEqual(0f, GrabZone.DistanceToHandles(RightCorner(s, -0.02f), s), 1e-4f, "in the band");
            Assert.AreEqual(0.017f, GrabZone.DistanceToHandles(RightCorner(s, 0.02f), s), 0.002f, "2 cm out = 1.7 cm past the slack");
        }
    }

    /// <summary>Step 14k C: the Spirit's lines play in context (place + kind).</summary>
    public class Step14kDialogueTests
    {
        readonly System.Collections.Generic.List<Object> _made = new System.Collections.Generic.List<Object>();
        bool _battle;

        [SetUp]
        public void SetUp()
        {
            GameConditions.Reset();
            _battle = false;
            GameConditions.Register("battle", () => _battle);
            Places.Override = "hub";
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _made)
                if (o != null) Object.DestroyImmediate(o);
            _made.Clear();
            GameConditions.Reset();
            Places.Override = null;
        }

        Dialogue Make()
        {
            var script = ScriptableObject.CreateInstance<DialogueScript>();
            _made.Add(script);
            DialogueTrigger T(string id, string context, LineKind kind, bool once = false) => new DialogueTrigger
            {
                id = id, priority = 1, context = context, kind = kind, objective = kind == LineKind.Objective, once = once,
                lines = new System.Collections.Generic.List<DialogueLine> { new DialogueLine { text = id + " line" } },
            };
            script.triggers.Add(T("lesson.grass", "outside & !battle", LineKind.Objective));
            script.triggers.Add(T("lodge.first", "lodge", LineKind.Intro, true));
            script.triggers.Add(T("chatter", "", LineKind.Filler));
            script.triggers.Add(T("hub.story", "hub", LineKind.Story, true));
            script.triggers.Add(T("hub.story2", "hub", LineKind.Story));
            script.triggers.Add(T("lodge.task", "lodge", LineKind.Objective));
            var go = new GameObject("Dialogue 14k Test");
            _made.Add(go);
            var d = go.AddComponent<Dialogue>();
            d.script = script;
            return d;
        }

        [Test]
        public void Places_Read()
        {
            Places.Override = "galewick";
            Assert.IsTrue(Places.Holds("realm"));
            Assert.IsTrue(Places.Holds("outside & !battle"));
            Assert.IsFalse(Places.Holds("hub"));
            Assert.IsFalse(Places.Holds("lodge | hall"));
            Places.Override = "hall";
            Assert.IsTrue(Places.Holds("inside"));
            Assert.IsFalse(Places.Holds("realm"));
            Assert.IsTrue(Places.Holds(""), "no context = anywhere");
        }

        [Test]
        public void ALineOutOfPlace_IsRefused_AndCanComeLater()
        {
            var d = Make();
            Places.Override = "lodge";
            d.Fire("hub.story", false);
            Assert.IsNull(d.CurrentTrigger, "a hub line in the lodge");
            Assert.IsFalse(Dialogue.HasSaid("hub.story"), "the once line stays unsaid");
            Places.Override = "hub";
            d.Fire("hub.story", false);
            Assert.AreEqual("hub.story", d.CurrentTrigger);
        }

        [Test]
        public void LessonFromOutside_DoesntBlockTheLodgeIntro()
        {
            var d = Make();
            d.PinLines(new[] { "lesson.grass" }, "Grass");
            d.Fire("lesson.grass", false);
            Assert.AreEqual("lesson.grass", d.CurrentTrigger);
            Places.Override = "lodge";
            d.Fire("lodge.first", false);
            d.DebugFinishLine();
            Assert.AreEqual("lodge.first", d.CurrentTrigger, "the lodge's intro, not the grass lesson");
        }

        [Test]
        public void Intro_GoesBeforeFiller_FillerWaitsForObjectives()
        {
            var d = Make();
            Places.Override = "lodge";
            d.Fire("chatter", false);
            Assert.AreEqual("chatter", d.CurrentTrigger);
            d.Fire("hub.story2", false); // not here: refused
            d.Fire("chatter", true);     // queued filler
            d.Fire("lodge.task", false);
            d.Fire("lodge.first", false);
            d.DebugFinishLine();
            Assert.AreEqual("lodge.first", d.CurrentTrigger, "the place's intro first");
            d.DebugFinishLine();
            Assert.AreEqual("lodge.task", d.CurrentTrigger, "then the objective, though the filler was queued before it");
            d.DebugFinishLine();
            Assert.AreEqual("chatter", d.CurrentTrigger, "filler last");
        }

        [Test]
        public void QueuedLines_LeftBehind_AreDropped()
        {
            var d = Make();
            d.Fire("hub.story", false);
            d.Fire("hub.story2", false);
            Assert.AreEqual("hub.story", d.CurrentTrigger);
            Places.Override = "cinderwood";
            d.DebugFinishLine();
            Assert.IsNull(d.CurrentTrigger, "the hub line queued before travelling is dropped, not delayed");
            Places.Override = "hub";
            d.DebugFinishLine();
            Assert.IsNull(d.CurrentTrigger, "and doesn't come back");
        }
    }

    /// <summary>Step 14k F: every walk-up trigger fires from any tile within one of what you see, diagonals too.</summary>
    public class Step14kWalkUpTests
    {
        [Test]
        public void Next_IncludesDiagonals_NotTwoAway()
        {
            var fp = new RectInt(10, 10, 2, 2);
            Assert.IsTrue(WalkUp.Next(new Vector2Int(9, 9), fp), "diagonal");
            Assert.IsTrue(WalkUp.Next(new Vector2Int(12, 12), fp), "other diagonal");
            Assert.IsTrue(WalkUp.Next(new Vector2Int(11, 12), fp), "in front");
            Assert.IsFalse(WalkUp.Next(new Vector2Int(11, 13), fp), "two in front");
            Assert.IsFalse(WalkUp.Next(new Vector2Int(10, 10), fp), "on it");
            int n = 0;
            foreach (var _ in WalkUp.Ring(fp)) n++;
            Assert.AreEqual(12, n, "a 2 x 2 has 12 tiles round it");
        }

        [Test]
        public void Shrine_FromThePathInFrontOfItsSteps()
        {
            var s = new Vector2Int(24, 26); // Cinderwood's village shrine: body r26..27, steps r28, the path r29
            Assert.IsTrue(HearthShrines.IsTouching(s, new Vector2Int(24, 29)), "the path row (Mugesh's row)");
            Assert.IsTrue(HearthShrines.IsTouching(s, new Vector2Int(23, 29)), "its diagonal");
            Assert.IsTrue(HearthShrines.IsTouching(s, new Vector2Int(26, 29)), "the other diagonal");
            Assert.IsTrue(HearthShrines.IsTouching(s, new Vector2Int(25, 28)), "on the steps");
            Assert.IsTrue(HearthShrines.IsTouching(s, new Vector2Int(23, 25)), "behind, diagonal");
            Assert.IsFalse(HearthShrines.IsTouching(s, new Vector2Int(24, 30)), "two rows out");
            Assert.IsFalse(HearthShrines.IsTouching(s, new Vector2Int(24, 26)), "the body");
        }

        [Test]
        public void Gate_FrontDiagonals()
        {
            var g = new GateLink { tile = new Vector2Int(7, 53) };
            Assert.IsTrue(ShardGates.AtFront(g, new Vector2Int(6, 54)), "front-left diagonal");
            Assert.IsTrue(ShardGates.AtFront(g, new Vector2Int(9, 54)), "front-right diagonal");
            Assert.IsFalse(ShardGates.AtFront(g, new Vector2Int(10, 54)), "two along");
            Assert.IsFalse(ShardGates.AtFront(g, new Vector2Int(7, 55)), "two in front");
            Assert.IsTrue(ShardGates.StepsIn(g, new Vector2Int(9, 55), new Vector2Int(9, 54)), "walking up onto the diagonal");
            Assert.IsFalse(ShardGates.StepsIn(g, new Vector2Int(10, 54), new Vector2Int(9, 54)), "passing along the row");
        }
    }
}
