using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>Nov 15 notes: the finger flame's hold on the thorns, Lilymere's walk over the lily pad bridge, the gaze-pinch ghost.</summary>
    public class Nov15FixTests
    {
        static EmberCarry.Hand[] Hands(bool atEmber = false, bool over = false) =>
            new[] { default(EmberCarry.Hand), new EmberCarry.Hand { tracked = true, atEmber = atEmber, overFirebox = over } };

        [Test]
        public void FingerFlame_LightsOnlyAfterTheHold_AShortSlipKeepsIt()
        {
            var carry = new EmberCarry { fingertip = true, fingerDwell = 3f };
            carry.Feed(0.02f, Hands(atEmber: true));
            Assert.AreEqual(EmberState.Held, carry.State);
            carry.Feed(1.5f, Hands(over: true));
            Assert.AreEqual(EmberState.Held, carry.State, "1.5 s on the thorns: not yet");
            Assert.AreEqual(0.5f, carry.DwellShare, 1e-4f);
            carry.Feed(0.2f, Hands());
            Assert.AreEqual(0.5f, carry.DwellShare, 1e-4f, "a slip under the grace keeps the progress");
            carry.Feed(1.0f, Hands(over: true));
            Assert.AreEqual(EmberState.Held, carry.State);
            Assert.AreEqual(EmberState.In, carry.Feed(0.6f, Hands(over: true)), "3 s on the thorns: lit");
        }

        [Test]
        public void FingerFlame_NoHold_LightsAtOnce()
        {
            var carry = new EmberCarry { fingertip = true };
            carry.Feed(0.02f, Hands(atEmber: true));
            Assert.AreEqual(EmberState.In, carry.Feed(0.02f, Hands(over: true)));
        }

        [Test]
        public void SwipeWalk_GoesStraightOverABridgedStream()
        {
            var map = ScriptableObject.CreateInstance<WorldMap>();
            map.SetLayout("XXXXXX\nXGPPGX\nXGPPGX\nXGPPGX\nXFMMFX\nXFMMFX\nXGPPGX\nXGPPGX\nXGPPGX\nXGPPGX\nXXXXXX");
            var tiles = new List<Vector2Int>();
            SmartStep.Plan(map, new Vector2Int(2, 9), new Vector2Int(0, -1), true, 40, null, tiles, false, true);
            Assert.AreEqual(new Vector2Int(2, 6), tiles[tiles.Count - 1], "unbridged: the road ends at the mud");
            foreach (var t in new[] { new Vector2Int(2, 4), new Vector2Int(3, 4), new Vector2Int(2, 5), new Vector2Int(3, 5) })
            {
                map.SetWet(t, true);
                map.SetBridged(t, true);
            }
            string end = SmartStep.Plan(map, new Vector2Int(2, 9), new Vector2Int(0, -1), true, 40, null, tiles, false, true);
            Assert.AreEqual(new Vector2Int(2, 1), tiles[tiles.Count - 1], $"bridged: one walk to the far end (ended '{end}' at {tiles[tiles.Count - 1]})");
            Object.DestroyImmediate(map);
        }

        [Test]
        public void FocusLock_LettingGo_KeepsAHardFocusSetAtTheSolve()
        {
            var src = File.ReadAllText("Assets/_Project/Scripts/Runtime/Puzzles/FocusLock.cs");
            StringAssert.Contains("if (!explorer.Locked && window.SoftFocus) window.SetFocus(null);", src);
            StringAssert.Contains("wheel.alsoFrame.Add(bridge + new Vector2Int(0, -2));", File.ReadAllText("Assets/_Project/Scripts/Runtime/Puzzles/Galewick/BigMill.cs"));
        }

        [Test]
        public void GazePinchGhost_KeyedOnThePinchPoint_AboveATarget()
        {
            var src = File.ReadAllText("Assets/_Project/Scripts/Runtime/Onboarding/GhostHands.cs");
            StringAssert.Contains("bool pinchKey = _clip.keyPinch || retarget || _request.place == GhostPlace.AboveTarget;", src);
        }
    }
}
