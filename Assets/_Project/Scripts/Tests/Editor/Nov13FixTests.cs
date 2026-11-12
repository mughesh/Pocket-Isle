using System.IO;
using Diorama.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>Nov 13 headset notes on the first release build: the statue's middle, the title logo, the shrine's crackle.</summary>
    public class Nov13FixTests
    {
        [Test]
        public void Statue_StandsOnTheRoadsMiddle_TheSquareIsEvenAroundIt()
        {
            var map = AssetDatabase.LoadAssetAtPath<WorldMap>("Assets/_Project/Data/WorldMap_StillIsle.asset");
            Assert.IsNotNull(map);
            Assert.IsTrue(map.TryGetProp('S', out var e));
            var f = WorldMap.FootprintOf(e);
            float middle = StillIsleTools.StatueAnchor.x + (f.x - 1) * 0.5f + e.nudge.x;
            // The road north of the statue: its paved columns' middle.
            int row = StillIsleTools.StatueAnchor.y - 4, first = -1, last = -1;
            for (int c = 0; c < map.Columns && last < 0; c++)
                if (map.At(row, c) == 'P') { if (first < 0) first = c; }
                else if (first >= 0) last = c - 1;
            Assert.IsTrue(first >= 0 && last >= first, $"no road at r{row}");
            Assert.AreEqual((first + last) * 0.5f, middle, 0.01f, $"road c{first}-c{last} at r{row}; statue middle c{middle}");
        }

        [Test]
        public void TitleLogo_FloatsClearOfTheStatuesHead()
        {
            Assert.GreaterOrEqual(Nov13Setup.LogoHeight, 0.3f);
            StringAssert.Contains($"height: {Nov13Setup.LogoHeight}", File.ReadAllText("Assets/_Project/Scenes/MR_Main.unity"));
        }

        [Test]
        public void ShrineFlame_QuietUnderTheShard_HalfAsLoud_NoSynthCrackle()
        {
            string src = File.ReadAllText("Assets/_Project/Scripts/Runtime/Audio/LandmarkLoops.cs");
            StringAssert.Contains("Opening.ShardOnly", src);
            StringAssert.Contains("cue = AudioIds.ShrineFlame, recipes = new[] { HearthShrines.RecipeName }, volume = 0.5f", src);
            var cue = AssetDatabase.LoadAssetAtPath<Diorama.Audio.AudioCue>("Assets/_Project/Audio/Data/Cues/Shrine/shrine.flame.asset");
            Assert.IsNotNull(cue);
            Assert.Greater(cue.clips.Length, 0);
            foreach (var c in cue.clips) StringAssert.DoesNotContain("/Synth/", AssetDatabase.GetAssetPath(c));
        }
    }
}
