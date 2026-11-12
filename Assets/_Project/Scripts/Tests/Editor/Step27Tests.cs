using System.Collections.Generic;
using Diorama.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Diorama.Tests
{
    public class Step27Tests
    {
        // ---- B: the Journal's first page ----

        [Test]
        public void Journal_opens_on_the_realm_you_are_in()
        {
            Assert.AreEqual(Element.Wind, Journal.OpeningTab("galewick", 9, 9, 0));
            Assert.AreEqual(Element.Fire, Journal.OpeningTab("cinderwood", 0, 9, 9));
            Assert.AreEqual(Element.Water, Journal.OpeningTab("lilymere", 9, 0, 9));
        }

        [Test]
        public void Journal_elsewhere_opens_on_the_most_found_and_a_tie_is_wind()
        {
            Assert.AreEqual(Element.Fire, Journal.OpeningTab("hub", 4, 2, 1));
            Assert.AreEqual(Element.Water, Journal.OpeningTab("lodge", 1, 5, 2));
            Assert.AreEqual(Element.Wind, Journal.OpeningTab("hub", 0, 0, 0), "none found");
            Assert.AreEqual(Element.Wind, Journal.OpeningTab("hub", 3, 3, 1), "a tie");
        }

        // ---- C: the stop practice ----

        [Test]
        public void Practice_goal_is_three_and_two_in_the_quick_journey()
        {
            Assert.AreEqual(3, StopPractice.Goal(false));
            Assert.AreEqual(2, StopPractice.Goal(true));
        }

        [Test]
        public void Practice_targets_get_further()
        {
            Assert.Less(StopPractice.Distance(0), StopPractice.Distance(1));
            Assert.Less(StopPractice.Distance(1), StopPractice.Distance(2));
        }

        [Test]
        public void Practice_counts_on_or_next_to_the_gold_tile()
        {
            var gold = new Vector2Int(10, 10);
            Assert.IsTrue(StopPractice.Landed(gold, gold));
            Assert.IsTrue(StopPractice.Landed(new Vector2Int(11, 9), gold));
            Assert.IsFalse(StopPractice.Landed(new Vector2Int(12, 10), gold));
        }

        [Test]
        public void Practice_walked_past_needs_two_tiles_beyond_the_closest()
        {
            Assert.IsFalse(StopPractice.WalkedPast(1, 0));
            Assert.IsTrue(StopPractice.WalkedPast(2, 0));
        }

        [Test]
        public void Practice_lesson_exists_after_far_and_before_look()
        {
            var s = AssetDatabase.LoadAssetAtPath<OnboardingScript>("Assets/_Project/Data/Onboarding/Onboarding_Main.asset");
            int practice = s.IndexOf(Onboarding.PracticeLesson);
            Assert.GreaterOrEqual(practice, 0);
            Assert.AreEqual(s.IndexOf(Onboarding.WalkLesson) + 1, practice, "Nov 12 evening: straight after the swipes (the far lesson went)");
            Assert.AreEqual(s.IndexOf("look"), practice + 1);
            Assert.AreEqual("Swipe to walk · Tap to stop", s.lessons[practice].chip);
            Assert.AreEqual("practiceDone", s.lessons[practice].doneWhen);
        }

        // ---- A / D: doors ----

        [Test]
        public void Door_clearance_covers_the_front_and_the_view_but_not_far_tiles()
        {
            var door = new RectInt(17, 3, 2, 2);
            Assert.IsTrue(DoorClearance.Clears(door, new Vector2Int(17, 5)), "the front tile");
            Assert.IsTrue(DoorClearance.Clears(door, new Vector2Int(15, 6)), "toward the seat, to the side");
            Assert.IsFalse(DoorClearance.Clears(door, new Vector2Int(17, 12)), "far in front");
            Assert.IsFalse(DoorClearance.Clears(door, new Vector2Int(10, 4)), "far to the side");
        }

        [Test]
        public void No_door_on_any_map_has_tall_grass_in_its_clearance()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Project/Scenes/MR_Main.unity");
            var scene = Step27Setup.SceneDoors();
            Assert.AreEqual(3, scene.Count, "tower, forge, lake temple");
            var bad = new List<string>();
            foreach (var map in Step27Setup.Maps())
                foreach (var (name, rect, wedge) in Step27Setup.Doors(map, scene))
                    for (int y = 0; y < map.Rows; y++)
                        for (int x = 0; x < map.Columns; x++)
                            if (map.Raw(y, x) == IslandMap.TallGrass && DoorClearance.Clears(rect, new Vector2Int(x, y), wedge))
                                bad.Add($"{map.name} {name} c{x} r{y}");
            Assert.IsEmpty(bad, string.Join(", ", bad));
        }

        [Test]
        public void Door_is_near_within_four_tiles()
        {
            var door = new RectInt(10, 10, 2, 1);
            Assert.IsTrue(DoorEntry.Near(new Vector2Int(10, 14), door));
            Assert.IsFalse(DoorEntry.Near(new Vector2Int(10, 15), door));
        }

        // ---- E: the Spirit stays put ----

        [Test]
        public void Spirit_ignores_small_changes_of_the_same_target()
        {
            var p = new SpiritPark();
            p.Update("a", Vector3.zero, 0f, false, 5f, 0.06f, 0.6f);
            Assert.IsFalse(p.Update("a", Vector3.right * 0.05f, 10f, false, 5f, 0.06f, 0.6f));
            Assert.AreEqual(Vector3.zero, p.Goal);
        }

        [Test]
        public void Spirit_waits_out_the_park_before_a_new_target()
        {
            var p = new SpiritPark();
            p.Update("a", Vector3.zero, 0f, false, 5f, 0.06f, 0.6f);
            Assert.IsTrue(p.Update("b", Vector3.one, 100f, false, 5f, 0.06f, 0.6f), "parked long enough");
            Assert.IsFalse(p.Update("c", Vector3.up, 103f, false, 5f, 0.06f, 0.6f), "3 s after the glide began");
            Assert.AreEqual(Vector3.one, p.Goal);
            Assert.IsTrue(p.Update("c", Vector3.up, 105.7f, false, 5f, 0.06f, 0.6f), "5 s after it landed");
        }

        [Test]
        public void Spirit_force_moves_past_the_park()
        {
            var p = new SpiritPark();
            p.Update("a", Vector3.zero, 0f, false, 5f, 0.06f, 0.6f);
            p.Update("b", Vector3.one, 100f, false, 5f, 0.06f, 0.6f);
            Assert.IsTrue(p.Update("battle", Vector3.up, 100.5f, true, 5f, 0.06f, 0.6f));
        }

        [Test]
        public void Spirit_follows_a_target_that_really_moved()
        {
            var p = new SpiritPark();
            p.Update("a", Vector3.zero, 0f, false, 5f, 0.06f, 0.6f);
            p.Update("b", Vector3.one, 100f, false, 5f, 0.06f, 0.6f);
            Assert.IsTrue(p.Update("b", Vector3.one * 2f, 100.7f, false, 5f, 0.06f, 0.6f));
        }

        [Test]
        public void Reading_floor_is_the_step_27_numbers()
        {
            Assert.AreEqual(1.2f, DialogueBubble.ReadBase, 1e-4f);
            Assert.AreEqual(0.05f, DialogueBubble.ReadPerChar, 1e-4f);
            Assert.AreEqual(3.2f, DialogueBubble.HoldSeconds(40, DialogueBubble.ReadBase, DialogueBubble.ReadPerChar), 1e-4f);
        }

        [Test]
        public void Chatter_is_quiet_in_lessons_battles_and_the_first_minutes()
        {
            Assert.IsTrue(Dialogue.ChatterQuiet(true, false, false, 999f), "lesson");
            Assert.IsTrue(Dialogue.ChatterQuiet(false, true, false, 999f), "puzzle (pinned)");
            Assert.IsTrue(Dialogue.ChatterQuiet(false, false, true, 999f), "battle");
            Assert.IsTrue(Dialogue.ChatterQuiet(false, false, false, 179f), "first minutes");
            Assert.IsFalse(Dialogue.ChatterQuiet(false, false, false, 181f));
        }

        // ---- F: the pinch icon ----

        [Test]
        public void Pinch_icon_stays_clear_of_the_target_seen_from_the_head()
        {
            Vector3 head = new Vector3(0f, 0.6f, -0.5f), target = Vector3.zero;
            int pick = -1;
            var at = PinchIcon.Spot(target, 0.04f, 0.015f, 0.015f, head, null, 0f, ref pick);
            Assert.IsFalse(PinchIcon.Covers(head, at, 0.015f, target, 0.02f));
            Assert.Greater(at.y, target.y);
        }

        [Test]
        public void Pinch_icon_moves_off_the_explorer()
        {
            Vector3 head = new Vector3(0f, 0.6f, -0.5f), target = Vector3.zero;
            int pick = -1;
            var plain = PinchIcon.Spot(target, 0.04f, 0.015f, 0.015f, head, null, 0f, ref pick);
            // The explorer exactly on the line from the head through the plain spot.
            Vector3 explorer = Vector3.Lerp(head, plain, 0.98f);
            pick = -1;
            var at = PinchIcon.Spot(target, 0.04f, 0.015f, 0.015f, head, explorer, 0.02f, ref pick);
            Assert.AreNotEqual(plain, at);
            Assert.IsFalse(PinchIcon.Covers(head, at, 0.015f, explorer, 0.02f));
        }

        [Test]
        public void Pinch_icon_keeps_its_side_while_clear()
        {
            Vector3 head = new Vector3(0f, 0.6f, -0.5f);
            int pick = 2;
            PinchIcon.Spot(Vector3.zero, 0.04f, 0.015f, 0.015f, head, null, 0f, ref pick);
            Assert.AreEqual(2, pick);
        }
    }
}
