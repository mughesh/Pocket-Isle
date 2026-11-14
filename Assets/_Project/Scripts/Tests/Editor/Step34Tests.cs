using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>
    /// Step 34 (Assets/Docs/Prompts/step-34-keeper-wakes.md): a hand moment wakes each Keeper. The hall's gate takes any
    /// HandPuzzle (Galeowl's untangle keeps its place), the before body shows only while the moment is unsolved, the save
    /// and skip rules, the shell's three knocks, the crown's cup, the calm flame, the two baked bodies, the lines, the
    /// scene wiring and the Dev panel's filming jumps.
    /// </summary>
    public class Step34Tests
    {
        const string Runtime = "Assets/_Project/Scripts/Runtime/";
        const string Scene = "Assets/_Project/Scenes/MR_Main.unity";

        static string Src(string file) => File.ReadAllText(Runtime + file);

        static CreatureRecipe Recipe(string name) => AssetDatabase.LoadAssetAtPath<CreatureRecipe>($"Assets/_Project/Data/Creatures/Recipes/{name}.asset");

        // ---- A: the gate ----

        [Test]
        public void Gate_TakesAnyHandPuzzle_AndKeepsTheOldSceneField()
        {
            var field = typeof(KeeperHall).GetField("wake");
            Assert.NotNull(field, "KeeperHall.wake");
            Assert.AreEqual(typeof(HandPuzzle), field.FieldType);
            var former = field.GetCustomAttributes(typeof(UnityEngine.Serialization.FormerlySerializedAsAttribute), false)
                .Cast<UnityEngine.Serialization.FormerlySerializedAsAttribute>().Select(a => a.oldName).ToArray();
            CollectionAssert.Contains(former, "untangle", "Galeowl's scene reference must survive the rename");
            Assert.IsTrue(typeof(HandPuzzle).IsAssignableFrom(typeof(KeeperUntangle)));
            Assert.IsTrue(typeof(HandPuzzle).IsAssignableFrom(typeof(KeeperWakePiece)));
            var src = Src("Puzzles/KeeperHall.cs");
            StringAssert.Contains("public bool Tangled => wake != null && !wake.IsSolved;", src);
            // The untangle's own beat (it shakes free) stays for any plain piece; a wake piece plays its own.
            StringAssert.Contains("else if (wake != null && beat == null)", src);
            StringAssert.Contains("yield return beat.WakeBeat(this);", src);
        }

        [Test]
        public void Gate_TheHallsModelHides_WhileTheBeforeBodyShows()
        {
            var src = Src("Puzzles/KeeperHall.cs");
            StringAssert.Contains("&& !BeforeBody;", src);
            StringAssert.Contains("bool BeforeBody => wake is KeeperWakePiece p && p != null && p.BeforeShown;", src);
        }

        // ---- D: save and skip rules ----

        [Test]
        public void BeforeBody_OnlyWhileUnsolved_NeverOnceCalmed()
        {
            Assert.IsTrue(KeeperWakePiece.WantsBefore(true, false, false, false), "first time in: the before pose");
            Assert.IsFalse(KeeperWakePiece.WantsBefore(false, false, false, false), "not in the hall");
            Assert.IsFalse(KeeperWakePiece.WantsBefore(true, true, false, false), "an older save with the Keeper calmed skips the wake");
            Assert.IsFalse(KeeperWakePiece.WantsBefore(true, true, true, false), "a rematch skips it");
            Assert.IsFalse(KeeperWakePiece.WantsBefore(true, false, true, false), "woken, then a lost or fled battle: it stays woken");
            Assert.IsTrue(KeeperWakePiece.WantsBefore(true, false, true, true), "solved this frame: the before body stays for the waking beat");
        }

        [Test]
        public void Moment_RunsOnlyUnsolved_InAnIdleHall()
        {
            Assert.IsTrue(KeeperWakePiece.Runs(true, false, false, true, true));
            Assert.IsFalse(KeeperWakePiece.Runs(true, true, false, true, true), "calmed");
            Assert.IsFalse(KeeperWakePiece.Runs(true, false, true, true, true), "solved (a lost battle doesn't reset it)");
            Assert.IsFalse(KeeperWakePiece.Runs(true, false, false, false, true), "the hall is meeting or fighting");
            Assert.IsFalse(KeeperWakePiece.Runs(false, false, false, true, true), "not here");
            Assert.IsFalse(KeeperWakePiece.Runs(true, false, false, true, false), "not usable (a menu, the explorer locked)");
        }

        [Test]
        public void Moment_NeverWalksTheExplorer_AndLogsEveryLockAndUnlock()
        {
            var src = Src("Puzzles/KeeperWakePiece.cs");
            StringAssert.Contains("explorer.HeldStill = on;", src);
            StringAssert.Contains("[HandPuzzle] {(on ? \"lock\" : \"unlock\")} {id}", src);
            StringAssert.Contains("TileTouch.Block(", src);
            // Every exit lets go: destroyed, disabled, and each frame the moment can't run.
            StringAssert.Contains("SetHeld(false, \"destroyed\")", src);
            StringAssert.Contains("SetHeld(false, \"disabled\")", src);
            StringAssert.Contains("SetHeld(working,", src);
            // The hint repeats; it is never a wall.
            StringAssert.Contains("Dialogue.Repeat(lessonLine);", src);
            Assert.AreEqual(20f, ObjectField<ShellKnockPiece>().hintEvery);
        }

        static T ObjectField<T>() where T : Component
        {
            var go = new GameObject("probe") { hideFlags = HideFlags.HideAndDontSave };
            go.SetActive(false);
            var c = go.AddComponent<T>();
            _probes.Add(go);
            return c;
        }

        static readonly System.Collections.Generic.List<GameObject> _probes = new System.Collections.Generic.List<GameObject>();

        [TearDown]
        public void Clean()
        {
            foreach (var g in _probes)
                if (g != null) Object.DestroyImmediate(g);
            _probes.Clear();
        }

        // ---- B: the shell ----

        [Test]
        public void Shell_ThreeKnocks_RisingPitch()
        {
            Assert.AreEqual(3, ShellKnockPiece.Knocks);
            Assert.Less(ShellKnockPiece.KnockPitch(1), ShellKnockPiece.KnockPitch(2));
            Assert.Less(ShellKnockPiece.KnockPitch(2), ShellKnockPiece.KnockPitch(3));
            var src = Src("Puzzles/Lilymere/ShellKnockPiece.cs");
            StringAssert.Contains("PuzzleState.SetStage(id, _count);", src); // saved: stage = knocks done
            StringAssert.Contains("MarkSolved(Knocks);", src);
            StringAssert.Contains("\"look + pinch\"", src); // look + pinch also counts as a knock
        }

        [Test]
        public void Shell_AFingertipInsideKnocks_OutsideDoesNot()
        {
            var half = new Vector3(0.085f, 0.062f, 0.085f);
            Assert.IsTrue(ShellKnockPiece.InShell(Vector3.zero, half));
            Assert.IsTrue(ShellKnockPiece.InShell(new Vector3(0.08f, 0f, 0f), half));
            Assert.IsTrue(ShellKnockPiece.InShell(new Vector3(0f, 0.06f, 0f), half));
            Assert.IsFalse(ShellKnockPiece.InShell(new Vector3(0f, 0.07f, 0f), half), "above the dome");
            Assert.IsFalse(ShellKnockPiece.InShell(new Vector3(0.07f, 0.05f, 0f), half), "past the curve");
            // Leaving needs a little more room than entering (no double knock from a wobbling finger).
            Assert.IsTrue(ShellKnockPiece.InShell(new Vector3(0f, 0.07f, 0f), half, 1.15f));
            Assert.IsFalse(ShellKnockPiece.InShell(Vector3.zero, Vector3.zero));
        }

        // ---- C: the crown ----

        [Test]
        public void Crown_TheFireGrowsWithTheCup_AndShrinksBack()
        {
            var size = new Vector2(0.1f, 0.95f);
            Assert.AreEqual(0.1f, CrownKindlePiece.FireSize(size, 0f), 1e-4f);
            Assert.AreEqual(0.95f, CrownKindlePiece.FireSize(size, 1f), 1e-4f);
            Assert.Less(CrownKindlePiece.FireSize(size, 0.3f), CrownKindlePiece.FireSize(size, 0.6f));

            // The village flame's meter: two palms fill it in 2 s, one curled hand in 3 s, look + pinch-hold in 3 s.
            var two = new CupMeter();
            bool done = false;
            for (int i = 0; i < 205 && !done; i++) done = two.Feed(0.01f, 2, false, false);
            Assert.IsTrue(done, "two palms, ~2 s");
            var look = new CupMeter();
            for (int i = 0; i < 150; i++) look.Feed(0.01f, 0, false, true);
            Assert.AreEqual(0.5f, look.Progress, 0.02f, "look + pinch-hold: half way after 1.5 s");
            for (int i = 0; i < 200; i++) look.Feed(0.01f, 0, false, false); // 0.35 s grace, then 0.25 / s
            Assert.Less(look.Progress, 0.3f, "let go: it shrinks back");
            Assert.IsFalse(look.Done);
        }

        [Test]
        public void Crown_UsesTheFlameCupsRules_TheHoldRingAndTheRisingTone()
        {
            var src = Src("Puzzles/Cinderwood/CrownKindlePiece.cs");
            StringAssert.Contains("CupRules.Facing(", src);
            StringAssert.Contains("CupRules.Wraps(", src);
            StringAssert.Contains("_block.SetFloat(BandId, ringBand);", src);
            StringAssert.Contains("SetPitch(0.92f + 0.3f * _meter.Progress)", src);
            StringAssert.Contains("HearthShrines.HoldDoneCue", src);
            var p = ObjectField<CrownKindlePiece>();
            Assert.AreEqual(0.24f, p.ringBand, 1e-4f, "the step-30 wide ring");
            Assert.AreEqual(2f, p.twoHandSeconds);
            Assert.AreEqual(3f, p.lookSeconds);
        }

        [Test]
        public void CrownFlame_DyingSmall_RagingBig_CalmSteady()
        {
            Assert.AreEqual(1f, KeeperFury.Burn(1f, 1f, 0f), 1e-4f, "frightened: the full fire");
            Assert.AreEqual(0.06f, KeeperFury.Burn(0.6f, 0.1f, 0f), 1e-4f, "kneeling: a wisp");
            Assert.AreEqual(0f, KeeperFury.Burn(0f, 1f, 0f), 1e-4f, "Galeowl and Tidemother calm: no flame, as before");
            Assert.AreEqual(KeeperFury.CalmFlameSize, KeeperFury.Burn(0f, 1f, 1f), 1e-4f, "Hearthstag calm: a small steady flame");
            Assert.Less(KeeperFury.CalmFlameSize, 0.5f);
            Assert.Greater(KeeperFury.Burn(1f, 1.3f, 0f), 1f, "in the fight it burns bigger");
            var hall = Src("Puzzles/KeeperHall.cs");
            StringAssert.Contains("if (calmCrownFlame) _fury.calmFlame = _look.calm;", hall);
            StringAssert.Contains("CalmBattleFlame(stage.Wild.gameObject);", hall); // the rematch body too
        }

        // ---- the baked bodies ----

        [Test]
        public void BeforeBodies_AreBaked_AtTheStandingModelsScale()
        {
            foreach (var (before, standing) in new[] { ("Hearthstag_Kneel", "Hearthstag"), ("Tidemother_Shell", "Tidemother") })
            {
                var b = Recipe(before);
                var s = Recipe(standing);
                Assert.NotNull(b, before);
                Assert.NotNull(b.bakedPrefab, before + " prefab");
                var bv = b.bakedPrefab.GetComponent<CreatureView>();
                var sv = s.bakedPrefab.GetComponent<CreatureView>();
                Assert.AreEqual(sv.rigScale, bv.rigScale, sv.rigScale * 0.01f, before + ": same model scale as " + standing);
                Assert.LessOrEqual(b.triangleBudget, s.triangleBudget, before + ": no bigger budget");
            }
            Assert.Less(Recipe("Hearthstag_Kneel").height, Recipe("Hearthstag").height * 0.8f, "kneeling is clearly lower");
            // The kneeling stag keeps its five crown crystals (the dying fire sits on them).
            var crystals = Recipe("Hearthstag_Kneel").bakedPrefab.GetComponentsInChildren<Renderer>(true).Count(r => r.name.Contains("CrownCrystal"));
            Assert.AreEqual(5, crystals);
            // The shell has no tail; the standing turtle does.
            Assert.IsFalse(Recipe("Tidemother_Shell").parts.Any(p => p.part != null && p.part.name == "TurtleTail"));
        }

        // ---- lines, scene, jumps ----

        [Test]
        public void Lines_AreInTheAssetAndTheScriptFile()
        {
            var asset = File.ReadAllText("Assets/_Project/Data/Dialogue/Script_v1.asset");
            var md = File.ReadAllText("Assets/Docs/Story/Script_v1.md");
            foreach (var id in new[] { "lm.keeper.shell", "cw.keeper.kindle" })
            {
                StringAssert.Contains("- id: " + id, asset);
                StringAssert.Contains("`" + id + "`", md);
            }
            StringAssert.Contains("target: puzzle:lm.shell", asset);
            StringAssert.Contains("target: puzzle:cw.kindle", asset);
        }

        [Test]
        public void Scene_EveryKeeperHallHasItsHandMoment()
        {
            var scene = File.ReadAllText(Scene);
            Assert.AreEqual(3, Regex.Matches(scene, @"\n  wake: \{fileID: [1-9]\d*\}").Count, "Galeowl's knots, Tidemother's shell, Hearthstag's fire");
            Assert.AreEqual(0, Regex.Matches(scene, @"\n  untangle: ").Count, "the old field name is gone from the saved scene");
            StringAssert.Contains("id: lm.shell", scene);
            StringAssert.Contains("id: cw.kindle", scene);
            StringAssert.Contains("lessonChip: Knock on its shell", scene);
            StringAssert.Contains("lessonChip: Cup the fire", scene);
            Assert.AreEqual(1, Regex.Matches(scene, @"\n  calmCrownFlame: 1").Count, "only Hearthstag keeps a crown flame when calm");
        }

        [Test]
        public void DevPanel_HasTheTwoFilmingJumps_AndTheQuickJourneyIsUntouched()
        {
            var labels = GameState.FilmJumps.Select(j => j.label).ToArray();
            CollectionAssert.AreEqual(new[] { "Tidemother: shell", "Hearthstag: kneeling" }, labels);
            StringAssert.Contains("GameState.FilmJumps", Src("Dev/DevPanel.cs"));
            // The Quick Journey is Galewick only: its presets know nothing of the new moments.
            foreach (var (id, _) in GameState.QuickTowerSolved)
            {
                Assert.AreNotEqual("lm.shell", id);
                Assert.AreNotEqual("cw.kindle", id);
            }
        }
    }
}

