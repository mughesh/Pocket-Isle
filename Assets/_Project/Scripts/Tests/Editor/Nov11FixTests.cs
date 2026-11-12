using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>
    /// The Nov 11 evening fix list (Assets/Docs/Prompts/fix-list-nov11.md), sections A4-A5 and B-F: small props off the
    /// roads, the hollow straight, the element move breathing on the player's turn, any tap stop counting in the practice,
    /// the thorns burning before they crumble, the hollow's lanterns lit with the fire finger, the thicker hold rings, the
    /// bloom's poke ghost, the Galewick lodge by the mill, the mill showing its bridge, chips off the explorer, new voices on.
    /// </summary>
    public class Nov11FixTests
    {
        const string Runtime = "Assets/_Project/Scripts/Runtime";

        static string Source(string file) => File.ReadAllText(Path.Combine(Runtime, file));

        static WorldMap Realm(string name) => AssetDatabase.LoadAssetAtPath<WorldMap>($"Assets/_Project/Data/WorldMap_{name}.asset");

        static readonly string[] Realms = { "StillIsle", "Galewick", "Cinderwood", "Lilymere" };

        // ---- A4 ----

        [Test]
        public void SmallClutter_IsOffTheRoads_InEveryRealm()
        {
            var small = new[] { "HayBale", "MossBoulder", "ReedClump", "BerryBush", "Woodpile" };
            var bad = new System.Collections.Generic.List<string>();
            foreach (var name in Realms)
            {
                var map = Realm(name);
                bool Road(Vector2Int t) => map.At(t.y, t.x) == IslandMap.Path;
                for (int r = 0; r < map.Rows; r++)
                    for (int c = 0; c < map.Columns; c++)
                    {
                        if (!map.TryGetProp(map.PropAt(r, c), out var e) || !small.Contains(e.recipe.name)) continue;
                        var t = new Vector2Int(c, r);
                        if (Road(t) || Road(t + Vector2Int.up) || Road(t + Vector2Int.down) || Road(t + Vector2Int.left) || Road(t + Vector2Int.right))
                            bad.Add($"{name} {e.recipe.name} c{c} r{r}");
                    }
            }
            Assert.IsEmpty(bad, string.Join(", ", bad));
        }

        [Test]
        public void Galewick_NoWallOrBaleAcrossARoadLane()
        {
            var map = Realm("Galewick");
            Assert.IsTrue(map.CanWalk(new Vector2Int(37, 40)), "the wall tile in the north lane east of the plaza is road now");
            Assert.IsTrue(map.CanWalk(new Vector2Int(35, 40)) && map.CanWalk(new Vector2Int(38, 26)) && map.CanWalk(new Vector2Int(30, 38)), "the hay bales' lane tiles");
            Assert.AreEqual(IslandMap.Path, map.At(40, 37));
        }

        // ---- A5 ----

        [Test]
        public void Hollow_IsAStraightBand_SmokeAndLanternsOnIt()
        {
            var map = Realm("Cinderwood");
            for (int r = 22; r <= 30; r++)
                for (int c = 29; c <= 30; c++) Assert.AreEqual(IslandMap.Path, map.Raw(r, c), $"band c{c} r{r}");
            for (int r = 23; r <= 26; r++)
                for (int c = 29; c <= 30; c++) Assert.AreEqual("SmokeCloud", map.TryGetProp(map.PropAt(r, c), out var e) ? e.recipe.name : "none", $"smoke c{c} r{r}");
            foreach (var t in new[] { new Vector2Int(31, 26), new Vector2Int(28, 25), new Vector2Int(31, 25), new Vector2Int(28, 24), new Vector2Int(31, 24), new Vector2Int(28, 23) })
                Assert.AreEqual("StoneLantern", map.TryGetProp(map.PropAt(t.y, t.x), out var e) ? e.recipe.name : "none", $"lantern c{t.x} r{t.y}");
            Assert.IsTrue(map.TryGetLandmark("hollow", out var mark) && mark == new Vector2Int(29, 27));
            // The old staircase tiles are gone.
            foreach (var t in new[] { new Vector2Int(28, 27), new Vector2Int(27, 28), new Vector2Int(28, 28) })
                Assert.AreNotEqual(IslandMap.Path, map.Raw(t.y, t.x), $"old jog c{t.x} r{t.y}");
            var piece = Object.FindFirstObjectByType<LanternHollowPiece>(FindObjectsInactive.Include);
            if (piece == null) return; // the scene isn't open
            Assert.AreEqual(new Vector2Int(29, 26), piece.smoke[0], "the scene's smoke moved with the map");
            Assert.AreEqual(new Vector2Int(31, 26), piece.lanterns[0]);
        }

        [Test]
        public void Jogs_NeverGrowAgain()
        {
            var ceiling = new System.Collections.Generic.Dictionary<string, int> { { "StillIsle", 4 }, { "Cinderwood", 14 }, { "Galewick", 11 }, { "Lilymere", 10 } };
            foreach (var name in Realms)
                Assert.LessOrEqual(EditorTools.PathAudit.JogPairs(Realm(name)).Count, ceiling[name], name + " (the Nov 11 count: the rest are 2-wide corners and plaza edges)");
        }

        [Test]
        public void NoGrassHoles_InsideRoads()
        {
            foreach (var name in Realms)
            {
                var map = Realm(name);
                bool Road(Vector2Int t) => map.Raw(t.y, t.x) == IslandMap.Path;
                for (int r = 0; r < map.Rows; r++)
                    for (int c = 0; c < map.Columns; c++)
                    {
                        var t = new Vector2Int(c, r);
                        if (map.Raw(r, c) != IslandMap.Grass || map.PropAt(r, c) != IslandMap.Empty) continue;
                        Assert.IsFalse(Road(t + Vector2Int.left) && Road(t + Vector2Int.right) || Road(t + Vector2Int.up) && Road(t + Vector2Int.down), $"{name} c{c} r{r} is a hole in a road");
                    }
            }
        }

        // ---- B6 ----

        [Test]
        public void BattleRunes_TheIslingsOwnElementMove_Breathes()
        {
            MoveData M(Element e, MoveKind k)
            {
                var m = ScriptableObject.CreateInstance<MoveData>();
                m.element = e;
                m.kind = k;
                return m;
            }
            var gust = M(Element.Wind, MoveKind.Attack);
            var ember = M(Element.Fire, MoveKind.Attack);
            var guard = M(Element.Neutral, MoveKind.Support);
            try
            {
                Assert.AreEqual(1, BattleRunes.BeatIndex(new[] { gust, ember, guard }, Element.Fire), "its own element");
                Assert.AreEqual(0, BattleRunes.BeatIndex(new[] { gust, ember, guard }, Element.Water), "none of its own: the first attack");
                Assert.AreEqual(1, BattleRunes.BeatIndex(new[] { guard, ember }, Element.Water), "never a support move");
                Assert.AreEqual(-1, BattleRunes.BeatIndex(new[] { guard }, Element.Fire));
                Assert.AreEqual(-1, BattleRunes.BeatIndex(null, Element.Fire));
            }
            finally
            {
                Object.DestroyImmediate(gust);
                Object.DestroyImmediate(ember);
                Object.DestroyImmediate(guard);
            }
            Assert.AreEqual(0f, BattleRunes.BeatWave(0f, 1.2f), 1e-4f);
            Assert.AreEqual(1f, BattleRunes.BeatWave(0.6f, 1.2f), 1e-4f, "fully out half way through a breath");
            Assert.AreEqual(0f, BattleRunes.BeatWave(1.2f, 1.2f), 1e-4f);
            StringAssert.Contains("runes.Show(c.species.moves, c.Element)", Source("Battle/EncounterDirector.cs"));
        }

        // ---- C7 ----

        [Test]
        public void Practice_AnyTapStopCounts_GoldIsPerfect()
        {
            var src = Source("Onboarding/Onboarding.cs");
            StringAssert.DoesNotContain("if (!_goldTile.HasValue || !StopPractice.Landed(explorer.Tile, _goldTile.Value)) return;", src);
            StringAssert.Contains("bool gold = _goldTile.HasValue && StopPractice.Landed(explorer.Tile, _goldTile.Value);", src);
            var s = AssetDatabase.LoadAssetAtPath<DialogueScript>("Assets/_Project/Data/Dialogue/Script_v1.asset");
            var e = s.Find("lesson.practice.perfect");
            Assert.IsNotNull(e, "lesson.practice.perfect");
            StringAssert.StartsWith("Perfect!", e.lines[0].text); // Nov 12 evening: "Perfect! Once more." (says what's next)
            StringAssert.Contains("Nice stop!", s.Find("lesson.practice.nice").lines[0].text);
        }

        // ---- D8 ----

        [Test]
        public void Thorns_BurnBeforeTheyCrumble()
        {
            Assert.GreaterOrEqual(VineBurnPiece.SpreadSeconds, 1.2f);
            var src = Source("Puzzles/Cinderwood/VineBurnPiece.cs");
            StringAssert.Contains("float total = SpreadSeconds + burnSeconds;", src);
            StringAssert.Contains("int from = NearestVine(at);", src, "the fire starts where the finger touched");
        }

        // ---- D9 ----

        [Test]
        public void HollowLanterns_LightWithTheFireFinger()
        {
            var src = Source("Puzzles/Cinderwood/LanternHollowPiece.cs");
            StringAssert.Contains("public bool fingertipFire = true;", src);
            StringAssert.Contains("if (fingertipFire && h != _fireHand) continue;", src, "a cold touch does nothing");
            StringAssert.Contains("_flame.Flare(TopWorld(touched)", src, "the finger keeps burning");
            StringAssert.Contains("if (_chain.Nudge(i0)) Lit(i0, \"look + pinch\");", src, "look + pinch stays the other door");
            var s = AssetDatabase.LoadAssetAtPath<DialogueScript>("Assets/_Project/Data/Dialogue/Script_v1.asset");
            StringAssert.Contains("village flame", s.Find("cw.lanterns").lines[0].text);
        }

        // ---- D10 ----

        [Test]
        public void HoldRings_AreWiderBrighter_WithAChime()
        {
            var shader = File.ReadAllText("Assets/_Project/Shaders/Beacon.shader");
            StringAssert.Contains("_Band", shader);
            var src = Source("HearthShrines.cs");
            StringAssert.Contains("public float ringBand = 0.24f;", src);
            StringAssert.Contains("GameAudio.At(HoldDoneCue", src);
            StringAssert.Contains("GameAudio.At(HearthShrines.HoldDoneCue", Source("Puzzles/Cinderwood/FlameCupPiece.cs"));
        }

        // ---- D11 ----

        [Test]
        public void HealBloom_ShowsAPokeGhost_Once()
        {
            var src = Source("HealBloom.cs");
            StringAssert.Contains("Dialogue.RegisterTarget(GhostTarget", src);
            StringAssert.Contains("ghosts.Show(GhostClip, GhostPlace.AtTarget, GhostTarget", src);
            StringAssert.Contains("_ghosted.Add(realm", src, "once per realm");
            Assert.AreEqual("poke", HealBloom.GhostClip);
        }

        // ---- E12 ----

        [Test]
        public void GalewickLodge_SitsRoadsideByTheMill()
        {
            var map = Realm("Galewick");
            var at = new Vector2Int(34, 33);
            Assert.AreEqual(HearthShrines.RecipeName, map.TryGetProp(map.PropAt(at.y, at.x), out var e) ? e.recipe.name : "none");
            Assert.AreNotEqual('Y', map.PropAt(40, 14), "the old pocket is empty");
            Assert.IsTrue(map.TryGetLandmark("shrine", out var l) && l == at);
            // Its front steps are plain grass, walkable, beside the road's east lane.
            foreach (var t in new[] { new Vector2Int(34, 35), new Vector2Int(35, 35) })
            {
                Assert.IsTrue(map.CanWalk(t), $"steps c{t.x} r{t.y}");
                Assert.AreEqual(IslandMap.Grass, map.Raw(t.y, t.x));
            }
            Assert.AreEqual(IslandMap.Path, map.Raw(33, 33), "the road runs past its west side");
            Assert.AreEqual(IslandMap.Path, map.Raw(34, 33));
            // The stub road to the old pocket is gone.
            foreach (var t in new[] { new Vector2Int(14, 43), new Vector2Int(15, 43), new Vector2Int(14, 44), new Vector2Int(15, 44), new Vector2Int(16, 44) })
                Assert.AreEqual(IslandMap.Grass, map.Raw(t.y, t.x), $"stub c{t.x} r{t.y}");
            // Still reachable from the arrival, with the drawbridge up.
            Assert.IsTrue(ExplorerPath.CanReach(map, map.startTile, new Vector2Int(34, 35), 200));
        }

        // ---- E13 ----

        [Test]
        public void Mill_FramesAndHoldsTheBridge()
        {
            var src = Source("Puzzles/Galewick/BigMill.cs");
            StringAssert.Contains("wheel.alsoFrame.Add(bridge);", src);
            StringAssert.Contains("window.SetFocus(ShowPoint);", src);
            StringAssert.Contains("yield return new WaitForSeconds(holdAfterSeconds);", src);
            StringAssert.Contains("window.SetFocus(null);", src);
            StringAssert.Contains("protected override void ExtraFrame(List<Vector2> into) => into.AddRange(alsoFrame);", Source("Puzzles/WheelPiece.cs"));
        }

        // ---- F14 ----

        [Test]
        public void Explorer_KeepsBubblesOffItself()
        {
            var src = Source("GestureTestPiece.cs");
            StringAssert.Contains("DialogueBubble.KeepClear += KeepClearOfMe;", src);
            StringAssert.Contains("DialogueBubble.KeepClear -= KeepClearOfMe;", src);
        }

        // ---- G19 ----

        [Test]
        public void NewVoices_AreTheDefault()
        {
            StringAssert.Contains("PlayerPrefs.GetInt(VoicesKey, 1) == 1", File.ReadAllText("Assets/_Project/Audio/Runtime/AudioAB.cs"));
            StringAssert.Contains("Heading(\"Sound A/B\");", Source("Dev/DevPanel.cs"), "the row lives under Tools > Sound A/B");
        }
    }
}
