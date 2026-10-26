using NUnit.Framework;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>Step 9h: lodge exit rule, bubble over the island, reading pace, lesson lines that stay.</summary>
    public class Step9hTests
    {
        [Test]
        public void ReadingPace_AtLeastOneAndAHalfSecondsPlusPerCharacter()
        {
            var go = new GameObject("bubble test");
            try
            {
                var b = go.AddComponent<DialogueBubble>();
                foreach (int n in new[] { 0, 10, 40, 70 })
                    Assert.GreaterOrEqual(DialogueBubble.HoldSeconds(n, b.holdBase, b.holdPerChar), 1.5f + 0.06f * n - 1e-4f, $"{n} characters");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Lessons_EveryTaughtLessonHasAChip()
        {
            var s = UnityEditor.AssetDatabase.LoadAssetAtPath<OnboardingScript>("Assets/_Project/Data/Onboarding/Onboarding_Main.asset");
            Assert.IsNotNull(s);
            foreach (var l in s.lessons)
                if (!string.IsNullOrEmpty(l.line)) Assert.IsFalse(string.IsNullOrEmpty(l.chip), $"{l.id} has a line but no chip");
        }

        [Test]
        public void Retarget_FindsTheGrabAndTheRelease_InASyntheticClip()
        {
            // Open hand, closes at sample 3, carried, opens at sample 7.
            var gaps = new[] { 0.06f, 0.05f, 0.03f, 0.012f, 0.01f, 0.011f, 0.02f, 0.045f, 0.06f };
            GhostRetarget.Thresholds(gaps, out float close, out float open);
            Assert.IsTrue(GhostRetarget.FindGrab(gaps, close, open, out int grab, out int release));
            Assert.AreEqual(3, grab);
            Assert.AreEqual(7, release);
        }

        [Test]
        public void Retarget_NoGrabInAPokeClip()
        {
            var gaps = new[] { 0.06f, 0.055f, 0.05f, 0.052f, 0.06f };
            Assert.IsFalse(GhostRetarget.FindGrab(gaps, 0.015f, 0.03f, out _, out _));
        }

        [Test]
        public void Retarget_SourceShiftBeforeTheGrab_TargetShiftAfterTheRelease_BlendedBetween()
        {
            var s = new Vector3(0.1f, 0f, 0f);
            var t = new Vector3(0f, 0f, 0.2f);
            Assert.AreEqual(s, GhostRetarget.Offset(0.2f, 0.5f, 1.5f, s, t));
            Assert.AreEqual(t, GhostRetarget.Offset(1.6f, 0.5f, 1.5f, s, t));
            var mid = GhostRetarget.Offset(1.0f, 0.5f, 1.5f, s, t);
            Assert.AreEqual(0.05f, mid.x, 1e-4f);
            Assert.AreEqual(0.1f, mid.z, 1e-4f);
        }

        [Test]
        public void LogoV2_LettersAndBase_UnderTenThousandTriangles()
        {
            var letters = UnityEditor.AssetDatabase.LoadAssetAtPath<PropRecipe>("Assets/_Project/Data/Props/TitleLogo.asset");
            var chunk = UnityEditor.AssetDatabase.LoadAssetAtPath<PropRecipe>("Assets/_Project/Data/Props/TitleLogoBase.asset");
            if (letters == null || letters.bakedMesh == null || chunk == null || chunk.bakedMesh == null) Assert.Inconclusive("Run Step9hSetup.LogoOnly first.");
            Assert.LessOrEqual(letters.bakedMesh.triangles.Length / 3 + chunk.bakedMesh.triangles.Length / 3, 10000);
        }

        static readonly Vector2Int Out = new Vector2Int(0, 1);

        [Test]
        public void Lodge_SwipeTowardTheDoor_Leaves()
        {
            Assert.IsTrue(LodgeRoom.AimedOut(Out, Out, Out, false));
        }

        [Test]
        public void Lodge_TapOnTheDoorTile_Leaves()
        {
            Assert.IsTrue(LodgeRoom.AimedOut(Out, Out, Vector2Int.zero, true));
        }

        [Test]
        public void Lodge_PathPassingOrEndingBesideTheDoor_StaysIn()
        {
            // A path that crosses the door tile to somewhere else.
            Assert.IsFalse(LodgeRoom.AimedOut(Out, Out, Vector2Int.zero, false));
            // Sideways along the door row (a swipe left), or a teleport (no step).
            Assert.IsFalse(LodgeRoom.AimedOut(Vector2Int.left, Out, Vector2Int.left, false));
            Assert.IsFalse(LodgeRoom.AimedOut(Vector2Int.zero, Out, Vector2Int.zero, true));
        }

        [Test]
        public void Bubble_SegmentOverTheFootprint_Crosses()
        {
            var half = new Vector2(0.3f, 0.3f);
            // Head in front of the island, bubble over its far half.
            Assert.IsTrue(DialogueBubble.CrossesFootprint(new Vector2(0f, 0.8f), new Vector2(0.1f, -0.1f), half));
            // Head in front, bubble beside the island (to the right, nearer than the island's back).
            Assert.IsFalse(DialogueBubble.CrossesFootprint(new Vector2(0.5f, 0.8f), new Vector2(0.45f, 0.1f), half));
            // Seen past the island's far corner: crosses.
            Assert.IsTrue(DialogueBubble.CrossesFootprint(new Vector2(0f, 0.8f), new Vector2(0.4f, -0.6f), half));
        }
    }
}
