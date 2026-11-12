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
        public void FarLesson_Gone_ThePracticeTeachesTheTap_NeverTwice()
        {
            // Nov 12 evening (Mugesh: a stale tap lesson came between the swipes and the two-tap practice): the far lesson
            // went; the practice teaches the tap, with its dots and a line after every stop.
            var s = Lessons();
            Assert.IsNull(s.lessons.Find(l => l.id == Onboarding.FarLesson), "the far lesson is out of the chain");
            var practice = s.lessons.Find(l => l.id == Onboarding.PracticeLesson);
            Assert.IsNotNull(practice);
            Assert.AreEqual(GhostHands.SwipeAllClip, practice.ghost, "step 32: opens on the swipe, a tap while walking");
            foreach (var l in s.lessons)
                Assert.AreNotEqual(Step12aSetup.SwipeTwiceClip, l.ghost, $"lesson {l.id} teaches swiping twice");

            var script = AssetDatabase.LoadAssetAtPath<DialogueScript>("Assets/_Project/Data/Dialogue/Script_v1.asset");
            if (script == null) return;
            foreach (var t in script.triggers)
                if (t.id.StartsWith("lesson."))
                    foreach (var line in t.lines)
                        Assert.IsFalse(line.text.ToLowerInvariant().Contains("twice"), $"{t.id}: '{line.text}'");
            StringAssert.Contains("tap", script.Find("lesson.practice").lines[0].text.ToLowerInvariant());
            StringAssert.Contains("again", script.Find("lesson.practice.nice").lines[0].text.ToLowerInvariant(), "a stop says what's next");
        }

        [Test]
        public void SwipeLesson_FourSwipes_ALineAfterEach()
        {
            // Nov 12 evening (Mugesh: one swipe that walked on filled all four dots): swipes count, not tiles.
            Assert.AreEqual("nice", Onboarding.SwipeCue(1, 4, "nice", "more", "last"));
            Assert.AreEqual("more", Onboarding.SwipeCue(2, 4, "nice", "more", "last"));
            Assert.AreEqual("last", Onboarding.SwipeCue(3, 4, "nice", "more", "last"));
            Assert.IsNull(Onboarding.SwipeCue(4, 4, "nice", "more", "last"));
            var script = AssetDatabase.LoadAssetAtPath<DialogueScript>("Assets/_Project/Data/Dialogue/Script_v1.asset");
            if (script == null) return;
            foreach (var id in new[] { "lesson.walk.nice", "lesson.walk.more", "lesson.walk.last" })
                Assert.IsTrue(script.Find(id) != null && script.Find(id).objective, $"{id} is a lesson line");
            var src = System.IO.File.ReadAllText("Assets/_Project/Scripts/Runtime/Onboarding/Onboarding.cs");
            StringAssert.DoesNotContain("_swipeTiles++", src, "one long walk isn't four swipes");
        }

        [Test]
        public void OldSave_AtTheFarLesson_ResumesAtThePractice()
        {
            var src = System.IO.File.ReadAllText("Assets/_Project/Scripts/Runtime/Onboarding/Onboarding.cs");
            StringAssert.Contains("if (lessonId == FarLesson) lessonId = PracticeLesson;", src);
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
