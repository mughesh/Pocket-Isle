using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>Step 22 (Nov 8 notes): each walk lesson keeps its own demo; tablets keep a gap you can see in the room.</summary>
    public class Step22Tests
    {
        static OnboardingScript Lessons()
        {
            var s = AssetDatabase.LoadAssetAtPath<OnboardingScript>("Assets/_Project/Data/Onboarding/Onboarding_Main.asset");
            if (s == null) Assert.Inconclusive("Onboarding_Main missing.");
            return s;
        }

        [Test]
        public void WalkLessons_FourSwipes_ThenSwipeAndTap()
        {
            var s = Lessons();
            var walk = s.lessons.Find(l => l.id == Onboarding.WalkLesson);
            var far = s.lessons.Find(l => l.id == Onboarding.FarLesson);
            Assert.AreEqual(GhostHands.SwipeAllClip, walk.ghost, "the walk lesson shows left, right, forward, back");
            Assert.AreEqual(GhostHands.SwipeTapClip, far.ghost, "the far lesson shows a swipe, then the tap that stops");
        }

        [Test]
        public void SwipeTapDemo_NotShadowedByARecording()
        {
            // Step 22: a recorded clip named like the composed demo replaces it (no strokes, no arrow, no tap pulse).
            foreach (var guid in AssetDatabase.FindAssets("t:GhostHandClip"))
            {
                var c = AssetDatabase.LoadAssetAtPath<GhostHandClip>(AssetDatabase.GUIDToAssetPath(guid));
                if (c == null) continue;
                Assert.AreNotEqual(GhostHands.SwipeTapClip, c.clipName, AssetDatabase.GUIDToAssetPath(guid));
                Assert.AreNotEqual(GhostHands.SwipeAllClip, c.clipName, AssetDatabase.GUIDToAssetPath(guid));
            }
        }

        [Test]
        public void CrackField_FindsPointsOnlyInTheCracks()
        {
            var mesh = new Mesh { vertices = new[] { new Vector3(-1f, -1f, 0.2f), new Vector3(1f, -1f, 0.2f), new Vector3(0f, 1f, 0.2f) }, triangles = new[] { 0, 1, 2 } };
            try
            {
                var found = new System.Collections.Generic.List<CrackField.Point>();
                CrackField.FindPoints(mesh, 3.2f, 0.075f, 40, found);
                Assert.AreEqual(40, found.Count);
                foreach (var p in found) Assert.Less(CrackField.Distance(p.position, 3.2f), 0.075f);
            }
            finally { Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void RoomGap_GrowsAsTheIslandShrinks()
        {
            var go = new GameObject("board");
            try
            {
                var b = go.AddComponent<RuneBoard>();
                go.transform.localScale = Vector3.one;
                Assert.AreEqual(TabletLayout.MinRoomGap, b.MinGapLocal, 1e-5f);
                go.transform.localScale = Vector3.one * 0.6f;
                Assert.AreEqual(TabletLayout.MinRoomGap / 0.6f, b.MinGapLocal, 1e-4f, "at the island's start size");
                Assert.GreaterOrEqual(b.MinGapLocal * 0.6f, 0.012f - 1e-5f, "at least 1.2 cm in the room");
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void TabletLayout_FlagsA14mmGapAtStartSize()
        {
            var rects = new[]
            {
                new TabletLayout.Rect { label = "a", center = new Vector2(0f, 0f), size = new Vector2(0.022f, 0.022f), grow = 0.1f },
                new TabletLayout.Rect { label = "b", center = new Vector2(0.036f, 0f), size = new Vector2(0.022f, 0.022f), grow = 0.1f },
            };
            Assert.AreEqual(1, TabletLayout.Problems(rects, TabletLayout.MinRoomGap / 0.6f).Count, "the old 3.6 cm pebble step");
            rects[1].center = new Vector2(0.044f, 0f);
            Assert.AreEqual(0, TabletLayout.Problems(rects, TabletLayout.MinRoomGap / 0.6f).Count, "the step 22 pebble step");
        }
    }
}
