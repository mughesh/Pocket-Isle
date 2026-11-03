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
            Assert.AreEqual(Step12aSetup.SwipeTapClip, far.ghost);
            Assert.IsFalse(far.chip.ToLowerInvariant().Contains("twice"), far.chip);
            Assert.IsTrue(System.Array.IndexOf(Onboarding.Events, "swipeWalks2") >= 0);
            foreach (var l in s.lessons)
                Assert.AreNotEqual(Step12aSetup.SwipeTwiceClip, l.ghost, $"lesson {l.id} teaches swiping twice");

            var script = AssetDatabase.LoadAssetAtPath<DialogueScript>("Assets/_Project/Data/Dialogue/Script_v1.asset");
            if (script == null) return;
            foreach (var t in script.triggers)
                if (t.id.StartsWith("lesson."))
                    foreach (var line in t.lines)
                        Assert.IsFalse(line.text.ToLowerInvariant().Contains("twice"), $"{t.id}: '{line.text}'");
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

        // ---- A2: the readable text rule ----

        [Test]
        public void TextRule_9mmCaps_AtTheDefaultIslandScale()
        {
            // Nov 3 late: 9 mm (14 mm made the menus far too big).
            Assert.AreEqual(0.009f, TextRule.IslandFont * TextRule.CapPerFont * TextRule.DefaultIslandScale, 1e-4f);
            Assert.AreEqual(0.009f, TextRule.WorldFont * TextRule.CapPerFont, 1e-4f);
            Assert.AreEqual(0.09f / TextRule.CapPerFont / 0.6f / 10f, TextRule.IslandFont, 1e-3f);
            Assert.AreEqual(TextRule.IslandFont, RuneBoard.LabelFont);
            Assert.AreEqual(TextRule.IslandFont, TextRule.Island(0.11f));
            Assert.AreEqual(0.5f, TextRule.Island(0.5f));
            Assert.Greater(TextRule.Width("Settings", 0.34f), TextRule.Width("Set", 0.34f));
            Assert.Greater(TextRule.Width("Settings", 0.34f, true), TextRule.Width("Settings", 0.34f));
        }

        [Test]
        public void Tablets_FitTheirLabels()
        {
            foreach (var label in new[] { "Resume", "Controls", "Settings", "Title", "Continue", "New journey", "Keep exploring" })
            {
                var s = RuneBoard.Fit(label);
                Assert.Greater(s.x, TextRule.Width(label, RuneBoard.LabelFont, true), label);
                Assert.Greater(s.y, RuneBoard.LabelFont * TextRule.CapPerFont * 2f, label);
            }
            var two = RuneBoard.Fit("Continue", "Quick journey", true);
            Assert.Greater(two.y, RuneBoard.Fit("Continue", null, true).y, "a second line makes it taller");
            Assert.Greater(RuneBoard.Fit("Resume", null, true).y, RuneBoard.Fit("Resume").y, "the primary is larger");
        }

        [Test]
        public void ControlsLines_TwoLinesAtMost_AtTheReadableSize()
        {
            float width = RuneBoard.RowWidth(new RuneBoard.Item("Walk", null), new RuneBoard.Item("Keep going", null), new RuneBoard.Item("Go there", null));
            foreach (var c in SettingsBoard.Controls)
            {
                string plain = System.Text.RegularExpressions.Regex.Replace(c.text, "<[^>]+>", "");
                float w = TextRule.Width(plain, RuneBoard.LabelFont);
                Assert.LessOrEqual(w, width * 1.85f, $"{c.rune}: '{plain}' runs over two lines");
            }
        }

        [Test]
        public void BattleRunes_EveryMoveName_FitsTheGrid()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:MoveData"))
            {
                var m = AssetDatabase.LoadAssetAtPath<MoveData>(AssetDatabase.GUIDToAssetPath(guid));
                if (m == null) continue;
                Assert.LessOrEqual(RuneBoard.Fit(m.Name).x, 0.34f, $"'{m.Name}' makes the battle grid too wide");
            }
        }

        [Test]
        public void RealmLines_FitTwoLinesUnderTheCard()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            var card = Object.FindFirstObjectByType<RealmNameCard>(FindObjectsInactive.Include);
            if (card == null) Assert.Inconclusive($"no RealmNameCard in {scene.name}");
            foreach (var c in card.cards)
                foreach (var line in new[] { c.line, c.restoredLine })
                    if (!string.IsNullOrEmpty(line))
                        Assert.LessOrEqual(TextRule.Width(line, TextRule.IslandFont, true), Mathf.Max(card.width, 0.5f) * 1.9f, line);
        }
    }
}
