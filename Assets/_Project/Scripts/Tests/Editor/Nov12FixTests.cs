using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>Nov 12 overnight, sections B and C: text that never overlaps, the sounds that got worse, the finale's pace.</summary>
    public class Nov12FixTests
    {
        static string Source(string file) => File.ReadAllText(file);

        static WorldMap Realm(string name) => AssetDatabase.LoadAssetAtPath<WorldMap>($"Assets/_Project/Data/WorldMap_{name}.asset");

        static float PropTop(WorldMap map, string recipe)
        {
            foreach (var e in map.propLegend)
                if (e.recipe != null && e.recipe.name == recipe && e.recipe.bakedMesh != null) return e.recipe.bakedMesh.bounds.max.y;
            return 0f;
        }

        [Test]
        public void DoorChip_SitsOverTheTower_AndTheArchBesideIt()
        {
            // Shot 05: the tower door's chip ran through the stone arch and the tower. It sits over the tallest of them now.
            var map = Realm("Galewick");
            Vector2Int tower = default;
            for (int r = 0; r < map.Rows; r++)
                for (int c = 0; c < map.Columns; c++)
                    if (map.TryGetProp(map.PropAt(r, c), out var e) && e.recipe != null && e.recipe.name == "WindTower") tower = new Vector2Int(c, r);
            Assert.AreNotEqual(Vector2Int.zero, tower, "the wind tower is on the map");
            float chip = DoorEntry.ChipHeightTiles(map, new RectInt(tower.x, tower.y, 2, 2), DoorEntry.ChipReach);
            Assert.GreaterOrEqual(chip, PropTop(map, "WindTower"), "over the tower");
            Assert.GreaterOrEqual(chip, PropTop(map, "ReturnGate"), "over the arch");
        }

        [Test]
        public void BattleLine_KeepsOffCreaturesPlatesMovesAndTheBubble()
        {
            var src = Source("Assets/_Project/Scripts/Runtime/Battle/BattleRunes.cs");
            StringAssert.Contains("ClearCaptionSpot", src);
            StringAssert.Contains("PlateBounds", src);
            StringAssert.Contains("TryGetPanelBounds", src);
        }

        [Test]
        public void KeeperPlate_GoesBesideTheBody_NeverInsideIt()
        {
            // Nov 12 evening: on the floor beside the body alone (a shield bubble pushed Galeowl's plate far off).
            var src = Source("Assets/_Project/Scripts/Runtime/Battle/BattleHud.cs");
            StringAssert.Contains("FloorSpot(b, cam, feet", src);
            StringAssert.Contains("TryGetBodyBounds", src);
            StringAssert.Contains("TryGetBodyBounds", Source("Assets/_Project/Scripts/Runtime/Battle/BattleRunes.cs"));
            Assert.AreEqual("<b>Galeowl</b>", BattleHud.LabelText("Galeowl", 11, false), "a Keeper's plate is its name, no level");
        }

        [Test]
        public void SmokeRun_ChecksTheLayoutAtEveryBeat()
        {
            var src = Source("Assets/_Project/Scripts/Editor/SmokeRun.cs");
            StringAssert.Contains("WatchLayout();", src);
            StringAssert.Contains("TextLayoutCheck.Check", src);
        }

        [Test]
        public void ShrineFill_KeepsItsNov11Take_TheBreezeIsSynth()
        {
            // Nov 12 evening: the pressure cooker was the restoration breeze's paid takes (8-9 kHz hiss), not the shrine fill.
            var cue = AssetDatabase.LoadAssetAtPath<Diorama.Audio.AudioCue>("Assets/_Project/Audio/Data/Cues/Shrine/shrine.fill.asset");
            Assert.IsNotNull(cue);
            Assert.AreEqual(2, cue.clips.Length, "the Nov 11 take is back");
            var breeze = AssetDatabase.LoadAssetAtPath<Diorama.Audio.AudioCue>("Assets/_Project/Audio/Data/Cues/Realm/restore.breeze.asset");
            Assert.IsNotNull(breeze);
            Assert.AreEqual(1, breeze.clips.Length);
            StringAssert.Contains("/Synth/", AssetDatabase.GetAssetPath(breeze.clips[0]), "the soft synth breeze");
        }

        [Test]
        public void HeartCall_FirstLineOfEveryScene()
        {
            Assert.IsTrue(DialogueBubble.HeartCallsNow(false, -999f, 0f), "the first line ever");
            Assert.IsFalse(DialogueBubble.HeartCallsNow(true, 10f, 15f), "the same scene");
            Assert.IsTrue(DialogueBubble.HeartCallsNow(true, 10f, 10f + DialogueBubble.HeartSceneGap + 1f), "a new scene");
            Assert.GreaterOrEqual(DialogueBubble.HeartMurmurFloor, 0.9f);
        }

        [Test]
        public void Finale_LinesHoldAboutASecond_PlusALittlePerLetter_AtMostThreeAndAHalf()
        {
            Assert.AreEqual(1f, DialogueBubble.FinaleHoldSeconds(0), 1e-4f);
            Assert.AreEqual(2.4f, DialogueBubble.FinaleHoldSeconds(40), 1e-4f);
            Assert.AreEqual(3.5f, DialogueBubble.FinaleHoldSeconds(200), 1e-4f);
            Assert.Less(DialogueBubble.FinaleHoldSeconds(40), DialogueBubble.HoldSeconds(40, 1.5f, 0.06f));
        }
    }
}
