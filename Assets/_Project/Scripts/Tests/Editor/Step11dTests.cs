using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Diorama.EditorTools;

namespace Diorama.Tests
{
    /// <summary>
    /// Step 11d: the objective rule (counter chip, pinned until done, passing count lines), pads that stay three in
    /// their channel, the two-hand cup, the mill flick zone and its continuous angle, the return gates after a Keeper
    /// win, and touch-walking off by default.
    /// </summary>
    public class Step11dTests
    {
        readonly List<Object> _made = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _made)
                if (o != null) Object.DestroyImmediate(o);
            _made.Clear();
        }

        // ---- A: the objective rule ----

        [Test]
        public void Counter_ChipCounts_StaysPinnedUntilDone_AndPinsAgainOnReturn()
        {
            Assert.AreEqual("Kites 1/3", ObjectiveRules.Chip("Kites", 1, 3));
            // With a counter: pinned whenever it's usable and not done, even after the first success (the 10b rule unpinned then).
            Assert.IsTrue(ObjectiveRules.WantPin(true, true, false, true, false), "pinned before any touch, after the lesson");
            Assert.IsFalse(ObjectiveRules.WantPin(true, true, false, false, true), "walked off: unpinned");
            Assert.IsTrue(ObjectiveRules.WantPin(true, true, false, true, true), "back again: pinned again");
            Assert.IsFalse(ObjectiveRules.WantPin(true, false, true, true, true), "done: never pinned");
            // Without a counter: the 10b rule (from the lesson until the action first works).
            Assert.IsFalse(ObjectiveRules.WantPin(false, true, false, true, true));
            Assert.IsTrue(ObjectiveRules.WantPin(false, false, false, true, true));
            Assert.IsFalse(ObjectiveRules.WantPin(false, false, false, true, false), "not started yet");
            Assert.AreEqual(2, ObjectiveRules.Count(5));
        }

        Dialogue MakeDialogue()
        {
            var script = ScriptableObject.CreateInstance<DialogueScript>();
            _made.Add(script);
            DialogueTrigger T(string id, params string[] lines)
            {
                var t = new DialogueTrigger { id = id, priority = 1, lines = new List<DialogueLine>() };
                foreach (var l in lines) t.lines.Add(new DialogueLine { text = l });
                return t;
            }
            script.triggers.Add(T("task", "The tower needs kites.", "Pinch one and lift it."));
            script.triggers.Add(T("one", "One!"));
            script.triggers.Add(T("story", "Something else."));
            var go = new GameObject("Dialogue 11d Test");
            _made.Add(go);
            var d = go.AddComponent<Dialogue>();
            d.script = script;
            return d;
        }

        [Test]
        public void PassingLine_ShowsOverTheLesson_ThenTheLessonComesBack_ChipUpdates()
        {
            var d = MakeDialogue();
            d.PinLines(new[] { "task" }, "Kites 0/3", new[] { "one" });
            d.Fire("task", false);
            Assert.AreEqual("task", d.CurrentTrigger);
            d.DebugFinishLine();
            Assert.AreEqual("task", d.CurrentTrigger, "the task's second line stays (pinned)");
            d.Fire("one", false);
            Assert.AreEqual("one", d.CurrentTrigger, "a count line passes over the lesson at once");
            d.Fire("story", false);
            Assert.AreEqual("one", d.CurrentTrigger, "story lines still wait for the lesson");
            d.DebugFinishLine();
            Assert.AreEqual("task", d.CurrentTrigger, "the lesson line comes back after the count");
            d.SetChipText("Kites 1/3");
            Assert.AreEqual("Kites 1/3", d.PinnedChip);
            d.UnpinLines();
            Assert.IsNull(d.PinnedChip);
            Assert.AreEqual("story", d.CurrentTrigger, "the waiting line shows once the objective is done");
        }

        // ---- D: lily pads ----

        [Test]
        public void Pads_StayOnTheChannelsWater_AndARespawnedPadIsBackAtItsStart()
        {
            // Water only on rows 0..2 between columns -2 and 6 (a ragged bank the rect doesn't know).
            bool Water(Vector2 p)
            {
                var t = new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y));
                return t.x >= -2 && t.x <= 6 && t.y >= 0 && t.y <= 2;
            }
            var start = new[] { new Vector2(0f, 1f), new Vector2(3f, 0f), new Vector2(5f, 2f) };
            var d = new PadDrift(start, new[] { new Vector2(20f, 0f), new Vector2(20f, 1f), new Vector2(20f, 2f) }, Rect.MinMaxRect(-10f, -0.5f, 30f, 2.5f))
            {
                idle = 0f, isWater = Water,
            };
            d.Push(0, new Vector2(-2.5f, 0f));
            d.Push(2, new Vector2(2.5f, 0f));
            for (float t = 0f; t < 5f; t += 0.02f) d.Tick(0.02f);
            for (int i = 0; i < 3; i++) Assert.IsTrue(Water(d.pos[i]), $"pad {i} stayed on the water ({d.pos[i]})");
            d.Respawn(0, start[0]);
            Assert.AreEqual(start[0], d.pos[0]);
            Assert.AreEqual(Vector2.zero, d.vel[0]);
            Assert.AreEqual(3, d.Count, "always three pads");
        }

        // ---- D: two hands cupped ----

        [Test]
        public void Scoop_TwoHandsCuppedTogether_ScoopFirst_OneHandStillWorks()
        {
            Vector3 up = Vector3.up;
            Assert.IsTrue(WaterScoop.Cupped(new Vector3(-0.03f, 0f, 0f), up, new Vector3(0.03f, 0f, 0f), up, up, 0.08f, out var mid, out float avg));
            Assert.AreEqual(0f, mid.x, 1e-5f);
            Assert.AreEqual(1f, avg, 1e-5f);
            Assert.IsFalse(WaterScoop.Cupped(new Vector3(-0.06f, 0f, 0f), up, new Vector3(0.06f, 0f, 0f), up, up, 0.08f, out _, out _), "too far apart");
            Assert.IsFalse(WaterScoop.Cupped(Vector3.zero, up, new Vector3(0.03f, 0f, 0f), Vector3.down, up, 0.08f, out _, out _), "one palm down");
            Assert.IsTrue(WaterScoop.Cupped(Vector3.zero, Vector3.right, new Vector3(0.03f, 0f, 0f), Vector3.right, up, 0.08f, out _, out float tipped, -2f),
                "carried: the pair stays a pair while it tips");
            Assert.AreEqual(0f, tipped, 1e-5f);

            var s = new WaterScoop();
            var dip = new WaterScoop.Hand { tracked = true, up = 1f, overWater = true, aboveWater = -0.02f };
            var hands = new[] { dip, dip, dip };
            Assert.AreEqual(ScoopState.Dipped, s.Feed(0.02f, hands));
            Assert.AreEqual(WaterScoop.Both, s.Holder, "both hands together are tried first");
            var lifted = new WaterScoop.Hand { tracked = true, up = 1f, overWater = true, aboveWater = 0.05f };
            Assert.AreEqual(ScoopState.Held, s.Feed(0.02f, new[] { lifted, lifted, lifted }));
            var tip = new WaterScoop.Hand { tracked = true, up = -0.6f, overBasin = true };
            Assert.AreEqual(ScoopState.Pouring, s.Feed(0.02f, new[] { tip, tip, tip }));
            Assert.AreEqual(1, s.Pours);

            // One cupped hand alone (the pair isn't cupped): the fallback.
            var one = new WaterScoop();
            Assert.AreEqual(ScoopState.Dipped, one.Feed(0.02f, new[] { default, dip, default }));
            Assert.AreEqual(1, one.Holder);
        }

        // ---- C: the mill ----

        [Test]
        public void MillFlick_ThickerZone_LowerSpeed_AnglesAccumulate()
        {
            Vector3 hub = Vector3.zero, normal = Vector3.forward;
            // 3 cm in front of the sails' plane (outside the old 2.2 cm), 0.4 m/s along the turn (below the old 0.5).
            var tip = new Vector3(0f, 0.04f, 0.03f);
            var vel = new Vector3(-0.4f, 0f, 0f);
            Assert.IsTrue(WheelRules.Flick(tip, vel, hub, normal, 0.07f, 0.04f, 0.008f, 0.35f, 0.06f, out float speed, out bool guard));
            Assert.Greater(speed, 0.35f);
            Assert.IsTrue(guard);
            Assert.IsFalse(WheelRules.Flick(tip, vel, hub, normal, 0.07f, 0.022f, 0.008f, 0.5f, 0.06f, out _, out _), "the old zone missed it");
            Assert.IsFalse(WheelRules.Flick(tip, new Vector3(0f, 0f, -0.6f), hub, normal, 0.07f, 0.04f, 0.008f, 0.35f, 0.06f, out _, out _), "a push into the sails isn't a flick");
            // The angle only ever accumulates: steps and Keep never jump it back.
            var spin = new WheelSpin();
            spin.Flick(0.9f);
            float last = spin.Angle;
            for (int i = 0; i < 300; i++)
            {
                if (i == 100) spin.Keep();
                spin.Step(1f / 72f);
                Assert.GreaterOrEqual(spin.Angle, last - 1e-5f, "never jumps back");
                Assert.Less(spin.Angle - last, 0.2f, "no jumps forward either");
                last = spin.Angle;
            }
        }

        // ---- C: the return gates after a Keeper win ----

        [Test]
        public void ReturnGates_GalewickTowerAndCinderwoodTemple_HaveTheirPropAndALinkHome()
        {
            Check(GalewickTools.MapPath, GalewickTools.Tile(GalewickTools.TowerGateXY), "Galewick tower");
            Check("Assets/_Project/Data/WorldMap_Cinderwood.asset", CinderwoodTools.Tile(CinderwoodTools.TempleGateXY), "Cinderwood temple");
        }

        static void Check(string path, Vector2Int gate, string what)
        {
            var map = AssetDatabase.LoadAssetAtPath<WorldMap>(path);
            Assert.IsNotNull(map, path);
            Assert.IsTrue(map.TryGetProp(map.PropAt(gate.y, gate.x), out var e) && e.recipe != null && e.recipe.name == "ReturnGate",
                $"{what}: a ReturnGate prop at c{gate.x} r{gate.y}");
            Assert.IsTrue(map.gates.Exists(g => g.tile == gate && g.to != null && g.id == "home"), $"{what}: a gate link home at c{gate.x} r{gate.y}");
            var front = gate + new Vector2Int(0, 1);
            Assert.IsTrue(IslandMap.IsWalkable(map.At(front.y, front.x)), $"{what}: the gate's front tile is walkable");
        }

        // ---- B: touch walking ----

        [Test]
        public void TouchWalk_OffByDefault()
        {
            const string key = "pocketisle.touchWalk";
            bool had = PlayerPrefs.HasKey(key);
            int old = PlayerPrefs.GetInt(key, 0);
            try
            {
                PlayerPrefs.DeleteKey(key);
                Assert.IsFalse(GameSettings.TouchWalk, "off until turned on in Settings");
                GameSettings.TouchWalk = true;
                Assert.IsTrue(GameSettings.TouchWalk);
            }
            finally
            {
                if (had) PlayerPrefs.SetInt(key, old);
                else PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }
    }
}
