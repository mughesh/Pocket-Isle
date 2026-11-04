using NUnit.Framework;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>Step 14i: hold to grab the island (timing, cancel, guards, two hands), the island facing the head.</summary>
    public class Step14iTests
    {
        const float Dt = 0.05f;

        static GrabHold Hold() => new GrabHold { holdSeconds = 0.6f, joinSeconds = 0.3f, cancelDistance = 0.04f };

        /// <summary>Grips on a handle for <paramref name="seconds"/>; returns how many frames reported the ring filling.</summary>
        static int Grip(GrabHold h, float seconds, bool otherHeld = false, Vector3 at = default, bool onHandle = true, bool gaze = false, bool below = false)
        {
            int filled = 0;
            for (float t = 0f; t < seconds - 1e-4f; t += Dt)
                if (h.Step(true, onHandle, gaze, below, otherHeld, at, 1f, Dt)) filled++;
            return filled;
        }

        [Test]
        public void Hold_FillsAfterHoldSeconds_ThenHeld()
        {
            var h = Hold();
            Grip(h, 0.5f);
            Assert.AreEqual(GrabHoldState.Arming, h.State, "0.5 s in: still arming");
            Assert.IsTrue(h.Busy, "an arming pinch is the island's (no walk)");
            Assert.AreEqual(1, Grip(h, 0.2f), "fills once, between 0.6 and 0.7 s");
            Assert.AreEqual(GrabHoldState.Held, h.State);
            Assert.AreEqual(0, Grip(h, 1f), "fills only once while held");
            h.Step(false, true, false, false, false, Vector3.zero, 1f, Dt);
            Assert.AreEqual(GrabHoldState.Idle, h.State, "the hand opens: let go");
        }

        [Test]
        public void Hold_ReleasedEarly_DoesNothing_AndNeverWalks()
        {
            var h = Hold();
            Grip(h, 0.4f);
            Assert.IsTrue(h.Busy);
            h.Step(false, true, false, false, false, Vector3.zero, 1f, Dt);
            Assert.AreEqual(GrabHoldState.Idle, h.State);
            Assert.AreEqual(0f, h.Fill);
        }

        [Test]
        public void Hold_MovingAwayCancels_UntilTheHandOpens()
        {
            var h = Hold();
            Grip(h, 0.2f);
            Grip(h, 0.05f, at: new Vector3(0.03f, 0f, 0f));
            Assert.AreEqual(GrabHoldState.Arming, h.State, "3 cm is still on the spot");
            Grip(h, 0.05f, at: new Vector3(0.05f, 0f, 0f));
            Assert.AreEqual(GrabHoldState.Cancelled, h.State, "5 cm away cancels");
            Assert.IsTrue(h.Busy, "a cancelled pinch still goes nowhere (no walk, no select)");
            Assert.AreEqual(0, Grip(h, 2f), "never fills after a cancel");
            h.Step(false, true, false, false, false, Vector3.zero, 1f, Dt);
            Assert.AreEqual(GrabHoldState.Idle, h.State);
        }

        [Test]
        public void Hold_CancelDistanceIsInWorldMetres()
        {
            var h = Hold();
            h.Step(true, true, false, false, false, Vector3.zero, 2f, Dt);
            h.Step(true, true, false, false, false, new Vector3(0.025f, 0f, 0f), 2f, Dt);
            Assert.AreEqual(GrabHoldState.Cancelled, h.State, "2.5 local cm on an island at scale 2 = 5 world cm");
        }

        [Test]
        public void Guards_GazeElsewhere_BelowTheIsland_OffAHandle_NeverArm()
        {
            var gaze = Hold();
            Assert.AreEqual(0, Grip(gaze, 1f, gaze: true));
            Assert.AreEqual(GrabHoldState.Select, gaze.State, "a look + pinch elsewhere");
            Assert.IsFalse(gaze.Busy, "it's free to select or walk");

            var low = Hold();
            Assert.AreEqual(0, Grip(low, 1f, below: true));
            Assert.AreEqual(GrabHoldState.Select, low.State, "a hand resting below the island");

            var off = Hold();
            Assert.AreEqual(0, Grip(off, 1f, onHandle: false));
            Assert.AreEqual(GrabHoldState.Select, off.State);
        }

        [Test]
        public void Guards_AreDecidedWhenThePinchStarts()
        {
            var h = Hold();
            h.Step(true, true, false, false, false, Vector3.zero, 1f, Dt);
            Assert.AreEqual(GrabHoldState.Arming, h.State);
            // A gaze target appearing mid-hold doesn't steal the pinch.
            h.Step(true, true, true, false, false, Vector3.zero, 1f, Dt);
            Assert.AreEqual(GrabHoldState.Arming, h.State);
            // And a pinch that started as a select stays one when the hand slides onto a handle.
            var s = Hold();
            s.Step(true, false, false, false, false, Vector3.zero, 1f, Dt);
            Assert.AreEqual(0, Grip(s, 1f));
            Assert.AreEqual(GrabHoldState.Select, s.State);
        }

        [Test]
        public void TwoHands_SecondHandJoinsWithTheShorterHold()
        {
            var h = Hold();
            Grip(h, 0.25f, otherHeld: true);
            Assert.AreEqual(GrabHoldState.Arming, h.State);
            Assert.IsTrue(h.Join);
            Assert.AreEqual(1, Grip(h, 0.15f, otherHeld: true), "joins by about 0.35 s");
            Assert.AreEqual(GrabHoldState.Held, h.State);
        }

        [Test]
        public void Drop_WaitsForTheHandToOpen()
        {
            var h = Hold();
            Grip(h, 0.7f);
            Assert.AreEqual(GrabHoldState.Held, h.State);
            h.Drop();
            Assert.AreEqual(GrabHoldState.Cancelled, h.State);
            Assert.AreEqual(0, Grip(h, 1f), "no re-grab without opening the hand");
        }

        [Test]
        public void Boards_FaceOnce_ThenStay_UntilTheHeadTurnsAwayWithNoHandNear()
        {
            var f = new BoardFacing();
            f.Open(10f);
            for (int i = 0; i < 100; i++) Assert.IsFalse(f.Step(35f, false, Dt), "25 deg off: never turns (no drift)");
            Assert.AreEqual(10f, f.Yaw);
            for (int i = 0; i < 100; i++) Assert.IsFalse(f.Step(60f, true, Dt), "a hand near: never turns, however long");
            int turned = 0;
            for (int i = 0; i < 25; i++) if (f.Step(60f, false, Dt)) turned++;
            Assert.AreEqual(1, turned, "50 deg off for 1 s, hands away: faces the head once");
            Assert.AreEqual(60f, f.Yaw);
            // A glance away shorter than a second doesn't count.
            for (int i = 0; i < 15; i++) Assert.IsFalse(f.Step(120f, false, Dt));
            Assert.IsFalse(f.Step(60f, false, Dt));
            for (int i = 0; i < 15; i++) Assert.IsFalse(f.Step(120f, false, Dt), "the timer restarted when the head came back");
        }

        [Test]
        public void Journal_EveryIslingHasAShortLine()
        {
            var data = UnityEditor.AssetDatabase.LoadAssetAtPath<JournalData>("Assets/_Project/Data/Resources/JournalData.asset") ?? JournalData.Load();
            Assert.IsNotNull(data, "JournalData baked");
            Assert.AreEqual(46, data.entries.Count);
            foreach (var e in data.entries)
            {
                string line = JournalLore.Line(e.species);
                Assert.IsFalse(string.IsNullOrEmpty(line), $"{e.species.name} has a line");
                Assert.LessOrEqual(line.Length, JournalLore.MaxLength, $"{e.species.name}: '{line}'");
            }
        }

        [Test]
        public void Journal_MatchupsFollowTheTypeChart()
        {
            foreach (var e in Journal.Elements)
            {
                Assert.IsTrue(TypeChart.Beats(e, JournalLore.StrongAgainst(e)), $"{e} strong");
                Assert.IsTrue(TypeChart.Beats(JournalLore.WeakAgainst(e), e), $"{e} weak");
            }
        }

        [Test]
        public void Journal_ChainRunsBaseFirst_WithLevels_AndHidesUnknownForms()
        {
            var a = ScriptableObject.CreateInstance<IslingSpecies>();
            var b = ScriptableObject.CreateInstance<IslingSpecies>();
            a.displayName = "Alpha";
            b.displayName = "Beta";
            a.evolvesTo = b;
            a.evolveLevel = 16;
            var chain = JournalLore.Chain(b, s => s == b ? a : null);
            Assert.AreEqual(2, chain.Count);
            Assert.AreEqual(a, chain[0].species);
            Assert.AreEqual(16, chain[0].evolveLevel);
            Assert.AreEqual(0, chain[1].evolveLevel);
            Assert.AreEqual("Alpha  › Lv 16 ›  ???", JournalLore.ChainLine(chain, s => s == a));
            Assert.AreEqual("Doesn't evolve", JournalLore.ChainLine(JournalLore.Chain(b, s => null), s => true));
            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }

        [Test]
        public void Paths_StraightLegs_NoShortJogs_KeptSegmentsStay()
        {
            var pts = new[]
            {
                new Vector2(11.5f, 3.5f), new Vector2(11.5f, 9.5f), new Vector2(8.5f, 14.5f), new Vector2(9.5f, 19.5f),
                new Vector2(14.5f, 22.5f), new Vector2(13.5f, 27.5f), new Vector2(17.5f, 30.5f), new Vector2(20.5f, 36.5f),
            };
            var legs = EditorTools.StraightPaths.Trail(pts, keep: new[] { 6 });
            Assert.AreEqual(pts[0], legs[0]);
            Assert.AreEqual(pts[pts.Length - 1], legs[legs.Length - 1], "the last waypoint is exact");
            int kept = 0;
            for (int i = 0; i + 1 < legs.Length; i++)
            {
                var d = legs[i + 1] - legs[i];
                bool axis = Mathf.Abs(d.x) < 1e-4f || Mathf.Abs(d.y) < 1e-4f;
                if (!axis)
                {
                    kept++;
                    Assert.AreEqual(pts[6], legs[i], "only the kept segment is diagonal");
                    continue;
                }
                bool firstOrLast = i == 0 || i + 1 == legs.Length - 1;
                if (!firstOrLast && legs[i + 1] != pts[6]) Assert.GreaterOrEqual(d.magnitude, 3f - 1e-4f, $"leg {i}: {legs[i]} to {legs[i + 1]}");
            }
            Assert.AreEqual(1, kept);
            var side = EditorTools.StraightPaths.Side(new[] { new Vector2Int(13, 9), new Vector2Int(17, 10), new Vector2Int(21, 8), new Vector2Int(27, 9) });
            for (int i = 0; i + 1 < side.Count; i++) Assert.AreEqual(1, Mathf.Abs(side[i + 1].x - side[i].x) + Mathf.Abs(side[i + 1].y - side[i].y), "4-connected");
            Assert.AreEqual(new Vector2Int(27, 9), side[side.Count - 1]);
        }

        [Test]
        public void Facing_FrontPointsAlongTheLineToTheHead_NotTheHeadForward()
        {
            // Head at the origin looking 30 deg right; the island placed in front of the gaze, but the head then shifts.
            var head = new Vector3(0f, 1.2f, 0f);
            var forward = Quaternion.Euler(20f, 30f, 0f) * Vector3.forward;
            var pose = IslandPlacement.Compute(head, forward, Vector3.up, 0f, 0.2f, IslandPlacement.Rule.Default);
            var to = head - pose.position;
            to.y = 0f;
            Assert.Less(Vector3.Angle(pose.rotation * Vector3.forward, to), 0.01f, "placement faces the head");
            Assert.AreEqual(0f, (pose.rotation * Vector3.up - Vector3.up).magnitude, 1e-5f, "level");

            var shifted = head + new Vector3(0.2f, 0f, 0f);
            var r = IslandPlacement.FaceHead(pose.position, shifted, forward);
            var to2 = shifted - pose.position;
            to2.y = 0f;
            Assert.Less(Vector3.Angle(r * Vector3.forward, to2), 0.01f);
            Assert.AreEqual(Quaternion.LookRotation(-new Vector3(forward.x, 0f, forward.z).normalized), IslandPlacement.FaceHead(head, head, new Vector3(forward.x, 0f, forward.z).normalized),
                "head right over it: the fallback");
        }
    }
}
