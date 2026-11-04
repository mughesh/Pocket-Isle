using NUnit.Framework;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>Step 14i: boards facing, the Journal lines (the hold-to-grab tests went with the hold, step 14j).</summary>
    public class Step14iTests
    {
        const float Dt = 0.05f;

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
