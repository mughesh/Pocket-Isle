using Diorama.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>Step 14f: the last walk lesson can't trap anyone, the forehead crystal.</summary>
    public class Step14fTests
    {
        static OnboardingScript Lessons()
        {
            var s = AssetDatabase.LoadAssetAtPath<OnboardingScript>("Assets/_Project/Data/Onboarding/Onboarding_Main.asset");
            if (s == null) Assert.Inconclusive("Onboarding_Main missing (Set Up MR_Main (14f menus and fixes)).");
            return s;
        }

        [Test]
        public void FarLesson_SwipeAndTap_ThreeWaysDone_NeverTeachesTwice()
        {
            var s = Lessons();
            var far = s.lessons.Find(l => l.id == Onboarding.FarLesson);
            Assert.IsNotNull(far);
            foreach (var e in new[] { "tapStopped", "atGoldTile", "swipeWalks2" })
                StringAssert.Contains(e, far.doneWhen);
            Assert.IsTrue(far.skipAfter > 0f && far.skipAfter <= 30f, $"passes itself within 30 s ({far.skipAfter})");
            Assert.AreEqual("ahead", far.goldTileRule);
            // Step 16: the walk stops by itself; the lessons show the swipe only (tap still stops, on the Controls card).
            Assert.AreEqual("thumb_swipe_forward", far.ghost);
            Assert.IsFalse(far.chip.ToLowerInvariant().Contains("tap"), far.chip);
            Assert.IsFalse(far.chip.ToLowerInvariant().Contains("twice"), far.chip);
            Assert.IsTrue(System.Array.IndexOf(Onboarding.Events, "swipeWalks2") >= 0);
            foreach (var l in s.lessons)
                Assert.AreNotEqual(Step12aSetup.SwipeTwiceClip, l.ghost, $"lesson {l.id} teaches swiping twice");

            var script = AssetDatabase.LoadAssetAtPath<DialogueScript>("Assets/_Project/Data/Dialogue/Script_v1.asset");
            if (script == null) return;
            foreach (var t in script.triggers)
                if (t.id.StartsWith("lesson."))
                    foreach (var line in t.lines)
                    {
                        Assert.IsFalse(line.text.ToLowerInvariant().Contains("twice"), $"{t.id}: '{line.text}'");
                        Assert.IsFalse(line.text.ToLowerInvariant().Contains("tap to stop") || line.text.ToLowerInvariant().Contains("to stop"), $"{t.id}: '{line.text}' (step 16: no tap to stop)");
                    }
        }

        [Test]
        public void FarLesson_PassesAfter30s_WithNothingDone()
        {
            var far = Lessons().lessons.Find(l => l.id == Onboarding.FarLesson);
            var lessons = new System.Collections.Generic.List<LessonData> { new LessonData { id = far.id, doneWhen = far.doneWhen, skipAfter = far.skipAfter } };
            var runner = new LessonRunner(lessons, Onboarding.Events);
            runner.Begin(0);
            Assert.AreEqual(LessonRunner.Outcome.Started, runner.Tick(0.1f));
            var last = LessonRunner.Outcome.None;
            for (float t = 0f; t < 31f && runner.IsRunning; t += 0.1f) last = runner.Tick(0.1f);
            Assert.IsFalse(runner.IsRunning, "still stuck after 31 s");
            Assert.AreEqual(LessonRunner.Outcome.Skipped, last);
        }

        [Test]
        public void FarLesson_DoneByATapStop()
        {
            var far = Lessons().lessons.Find(l => l.id == Onboarding.FarLesson);
            var lessons = new System.Collections.Generic.List<LessonData> { new LessonData { id = far.id, doneWhen = far.doneWhen } };
            GameConditions.Register("atGoldTile", () => false);
            try
            {
                var runner = new LessonRunner(lessons, Onboarding.Events);
                runner.Begin(0);
                runner.Tick(0.1f);
                Assert.AreEqual(LessonRunner.Outcome.None, runner.Tick(0.1f));
                runner.Mark("tapStopped");
                Assert.AreEqual(LessonRunner.Outcome.Done, runner.Tick(0.1f));
            }
            finally
            {
                GameConditions.Unregister("atGoldTile");
            }
        }

        [Test]
        public void HeartCrystal_Baked_Small()
        {
            var r = PropKit.Load(PropKit.HeartCrystalName);
            if (r == null) Assert.Inconclusive("HeartCrystal not baked (Set Up MR_Main (14f menus and fixes)).");
            int tris = r.bakedMesh != null ? r.bakedMesh.triangles.Length / 3 : 0;
            Assert.IsTrue(tris > 0 && tris <= 2000, $"{tris} tris");
        }

        // ---- B: the tablets ----

        [Test]
        public void Tablet_SharedMesh_EveryPart_SmallAndFrontFacing()
        {
            var m = TabletKit.Build();
            try
            {
                Assert.IsTrue(m.vertexCount > 100 && m.vertexCount < 2000, $"{m.vertexCount} vertices");
                var colors = new System.Collections.Generic.List<Color>();
                m.GetColors(colors);
                foreach (float part in new[] { 0f, 0.25f, 0.5f, 0.75f, 1f })
                    Assert.IsTrue(colors.Exists(c => Mathf.Abs(c.a - part) < 0.01f), $"part {part} missing");
                // The face (normal -z) triangles wind clockwise seen from the front (-z): Unity's front faces. Stretched like
                // the shader does (anchor x the half size less the corner) for a 10 x 4 cm tablet.
                var anchors = new System.Collections.Generic.List<Vector2>();
                m.GetUVs(1, anchors);
                var v = m.vertices;
                for (int i = 0; i < v.Length; i++) v[i] += (Vector3)(anchors[i] * new Vector2(0.05f - TabletKit.Corner, 0.02f - TabletKit.Corner));
                var n = m.normals;
                var t = m.triangles;
                int facing = 0, backwards = 0;
                for (int i = 0; i < t.Length; i += 3)
                {
                    if (n[t[i]].z > -0.99f || n[t[i + 1]].z > -0.99f || n[t[i + 2]].z > -0.99f) continue;
                    Vector3 a = v[t[i]], b = v[t[i + 1]], c = v[t[i + 2]];
                    float cross = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
                    if (Mathf.Abs(cross) < 1e-12f) continue;
                    if (cross < 0f) facing++;
                    else backwards++;
                }
                Assert.Greater(facing, 50);
                Assert.AreEqual(0, backwards, "front triangles wound the wrong way (culled)");
            }
            finally
            {
                Object.DestroyImmediate(m);
            }
        }

        [Test]
        public void Tablet_LabelOnFace_AtLeast4_5To1()
        {
            Assert.AreEqual(21f, TabletKit.Contrast(Color.white, Color.black), 0.01f);
            Assert.AreEqual(1f, TabletKit.Contrast(Color.gray, Color.gray), 0.001f);
            float c = TabletKit.Contrast(TabletKit.LabelColor, TabletKit.FaceColor);
            Assert.GreaterOrEqual(c, 4.5f, $"label on the slab face {c:0.0}:1");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(Step14fSetup.TabletMaterialPath);
            if (mat == null) return;
            Assert.IsTrue(mat.enableInstancing, "the tablets of a board draw as one");
            Assert.AreEqual(TabletKit.Corner, mat.GetFloat("_Corner"), 1e-5f, "the shader's corner radius is the mesh's");
            var face = mat.GetColor("_Stone");
            Assert.GreaterOrEqual(TabletKit.Contrast(TabletKit.LabelColor, face), 4.5f, "the material's stone");
        }

        [Test]
        public void Speaker_OnAndOff_Differ()
        {
            var on = TabletKit.Speaker(true);
            var off = TabletKit.Speaker(false);
            Assert.IsNotNull(on);
            Assert.IsNotNull(off);
            Assert.AreNotEqual(on.vertexCount, off.vertexCount);
        }

        // ---- step 14g: the old sizes, labels fit their tablets ----

        [Test]
        public void Tablets_Lighter_ForTheOldRuneSize()
        {
            // The smallest tablet (a 2.4 cm volume pebble) still has room for its rounded corners.
            Assert.LessOrEqual(TabletKit.Corner * 2f, 0.024f);
            Assert.LessOrEqual(TabletKit.Thickness, 0.004f);
        }

        [Test]
        public void RealmLines_Lilymere_RanDry()
        {
            var card = Object.FindFirstObjectByType<RealmNameCard>(FindObjectsInactive.Include);
            if (card == null) Assert.Inconclusive("no RealmNameCard in the open scene");
            foreach (var c in card.cards)
                if (c.shard == "shard.water")
                {
                    Assert.AreEqual("Where the water ran dry", c.line);
                    Assert.AreEqual("Where the water flows again", c.restoredLine);
                    return;
                }
            Assert.Fail("no Lilymere card");
        }
    }
}
