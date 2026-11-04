using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>Step 14h: the growth loop (evolution in the palm, the Keeper's thanks, readable pads, the Journal, temples).</summary>
    public class Step14hTests
    {
        readonly List<Object> _made = new List<Object>();

        [TearDown]
        public void Clean()
        {
            foreach (var o in _made)
                if (o != null) Object.DestroyImmediate(o);
            _made.Clear();
        }

        IslingSpecies Species(string name, Element element = Element.Fire, IslingSpecies evolvesTo = null, int evolveLevel = 0)
        {
            var s = ScriptableObject.CreateInstance<IslingSpecies>();
            s.name = name;
            s.displayName = name;
            s.element = element;
            s.evolvesTo = evolvesTo;
            s.evolveLevel = evolveLevel;
            _made.Add(s);
            return s;
        }

        ProgressionConfig Config()
        {
            var c = ScriptableObject.CreateInstance<ProgressionConfig>();
            _made.Add(c);
            return c;
        }

        // ---- A: due from every cause, one at a time ----

        [Test]
        public void Due_EveryCause_FirstPodFirst()
        {
            var big = Species("Big");
            var small = Species("Small", Element.Fire, big, 10);
            var plain = Species("Plain");
            var pods = new List<IslingInstance> { new IslingInstance(plain, 20), new IslingInstance(small, 9), new IslingInstance(small, 5) };
            Assert.AreEqual(-1, Progression.NextDue(pods), "nobody at the level yet");
            Assert.AreEqual(-1, Progression.NextDue(null));

            // Battle XP while benched.
            var cfg = Config();
            Progression.AddXp(pods[1], cfg.XpToNext(9), cfg);
            Assert.AreEqual(10, pods[1].level);
            Assert.AreEqual(1, Progression.NextDue(pods), "XP took it to its level");

            // The Dev panel / a debug key sets the level directly; a lodge Isling arrives already past it.
            pods[1].level = 9;
            pods[2].level = 14;
            Assert.AreEqual(2, Progression.NextDue(pods), "past its level counts");

            // "Make ready".
            Assert.IsTrue(Progression.MakeReady(pods[1]));
            Assert.AreEqual(10, pods[1].level);
            Assert.AreEqual(1, Progression.NextDue(pods), "the first pod in order goes first");
            Assert.IsFalse(Progression.MakeReady(pods[0]), "a species that doesn't evolve");
            Assert.AreEqual(20, pods[0].level, "MakeReady never lowers or touches others");
        }

        [Test]
        public void Due_OneAtATime_TheNextAfterAnEvolution()
        {
            var big = Species("Big");
            var small = Species("Small", Element.Fire, big, 10);
            var pods = new List<IslingInstance> { new IslingInstance(small, 12), new IslingInstance(small, 11) };
            IslingInstance seen = null;
            IslingSpecies was = null;
            void OnEvolved(IslingInstance c, IslingSpecies from) { seen = c; was = from; }
            Progression.Evolved += OnEvolved;
            try
            {
                Assert.AreEqual(0, Progression.NextDue(pods));
                Assert.IsTrue(Progression.Evolve(pods[0]));
                Assert.AreSame(pods[0], seen, "the Journal hears of it");
                Assert.AreSame(small, was);
                Assert.AreEqual(1, Progression.NextDue(pods), "then the next");
                Progression.Evolve(pods[1]);
                Assert.AreEqual(-1, Progression.NextDue(pods));
            }
            finally
            {
                Progression.Evolved -= OnEvolved;
            }
        }

        [Test]
        public void PalmUp_SyntheticHands()
        {
            var t = new PalmUpTimer();
            for (int i = 0; i < 60; i++) Assert.IsFalse(t.Step(0.02f, true, -0.9f), "palm down never");
            for (int i = 0; i < 60; i++) Assert.IsFalse(t.Step(0.02f, false, 1f), "untracked never");
            for (int i = 0; i < 60; i++) Assert.IsFalse(t.Step(0.02f, true, 0.4f), "tilted under 0.55 never");
            t.Reset();
            Assert.IsFalse(t.Step(0.2f, true, 0.9f));
            Assert.IsFalse(t.Step(0.1f, true, -0.2f), "a flicker down starts over");
            Assert.IsFalse(t.Step(0.2f, true, 0.9f));
            Assert.IsTrue(t.Step(0.2f, true, 0.9f), "0.4 s palm up");
        }

        [Test]
        public void DialogueVars_FillNames()
        {
            Dialogue.SetVar("name", "Toastling");
            Assert.AreEqual("Toastling is glowing... it's ready to grow!", Dialogue.Fill("{name} is glowing... it's ready to grow!"));
            Assert.AreEqual("No braces", Dialogue.Fill("No braces"));
            Assert.AreEqual("{unknown} stays", Dialogue.Fill("{unknown} stays"));
        }

        [Test]
        public void Script_HasTheGrowthLines()
        {
            var s = UnityEditor.AssetDatabase.LoadAssetAtPath<DialogueScript>("Assets/_Project/Data/Dialogue/Script_v1.asset");
            if (s == null) Assert.Inconclusive("Script_v1.asset missing.");
            var ready = s.Find(GrowthDirector.ReadyLine);
            Assert.IsNotNull(ready, "evolve.ready (run the 14h setup to reimport the script)");
            Assert.IsTrue(ready.objective);
            StringAssert.Contains("{name}", ready.lines[0].text);
            Assert.IsNotNull(s.Find(GrowthDirector.DoneLine));
            Assert.IsNotNull(s.Find(GrowthDirector.FirstLine));
            foreach (var shard in new[] { "shard.wind", "shard.fire", "shard.water" })
            {
                var t = s.Find(KeeperThanks.Line(shard));
                Assert.IsNotNull(t, KeeperThanks.Line(shard));
                Assert.AreEqual(DialogueSpeaker.Keeper, t.lines[0].speaker, "the Keeper's own voice");
            }
        }

        // ---- B: the Keeper's thanks ----

        [Test]
        public void KeeperThanks_OncePerSave_StarterBecomesReady()
        {
            var grown = Species("Grown");
            var starter = Species("Starter", Element.Fire, grown, 16);
            var other = Species("Other", Element.Water, Species("OtherBig"), 12);
            var pods = new List<IslingInstance> { new IslingInstance(other, 6), new IslingInstance(starter, 9) };
            var flags = new HashSet<string>();
            var first = KeeperThanks.Apply(flags.Contains, id => flags.Add(id), pods, null, new[] { starter }, out int was);
            Assert.AreSame(pods[1], first, "the starter, not the first pod");
            Assert.AreEqual(9, was);
            Assert.AreEqual(16, pods[1].level, "raised to its evolve level");
            Assert.AreEqual(6, pods[0].level, "nobody else");
            Assert.AreEqual(1, Progression.NextDue(pods));
            Assert.IsNull(KeeperThanks.Apply(flags.Contains, id => flags.Add(id), pods, null, new[] { starter }, out _), "once per save");
        }

        [Test]
        public void KeeperThanks_StarterAlreadyEvolved_OrInStorage()
        {
            var grown = Species("Grown");
            var starter = Species("Starter", Element.Fire, grown, 16);
            var evolved = new List<IslingInstance> { new IslingInstance(grown, 18) };
            Assert.IsNull(KeeperThanks.Starter(evolved, null, new[] { starter }), "its line has evolved");
            var stored = new List<IslingInstance> { new IslingInstance(starter, 20) };
            Assert.AreSame(stored[0], KeeperThanks.Starter(evolved, stored, new[] { starter }), "storage counts too");
            Assert.AreEqual("keeper.thanks.evolve.wind", KeeperThanks.Line("shard.wind"));
            Assert.AreEqual("keeper.thanks.evolve.fire", KeeperThanks.Line("shard.fire"));
            Assert.AreEqual("keeper.thanks.evolve.water", KeeperThanks.Line("shard.water"));
        }

        // ---- C: readable battles ----

        static readonly Vector3 NearFeet = new Vector3(0f, 0f, 0.1f), WildFeet = new Vector3(0f, 0f, -0.15f);

        [Test]
        public void PadFit_TallNearIsling_1_3xItsOpponent()
        {
            var r = PadFit.Fit(new Vector3(0f, 1f, 0.3f), NearFeet, 0.09f, WildFeet, 0.04f, 0.5f, false);
            Assert.IsTrue(r.ratio);
            Assert.AreEqual(1.3f * 0.04f / 0.09f, r.near, 1e-4f);
            Assert.AreEqual(1f, r.wild);
            var same = PadFit.Fit(new Vector3(0f, 1f, 0.3f), NearFeet, 0.04f, WildFeet, 0.045f, 0.5f, false);
            Assert.AreEqual(1f, same.near, "close sizes stay");
            Assert.AreEqual(1f, same.wild);
        }

        [Test]
        public void PadFit_LowEyes_NearTopUnderTheSightline()
        {
            Vector3 eye = new Vector3(0f, 0.08f, 0.6f);
            var r = PadFit.Fit(eye, NearFeet, 0.05f, WildFeet, 0.05f, 0.5f, false);
            Assert.IsTrue(r.sightline);
            Assert.Less(r.near, 1f);
            float allowed = PadFit.AllowedNearHeight(eye, NearFeet, WildFeet, 0.05f * r.wild, 0.5f);
            Assert.LessOrEqual(0.05f * r.near, allowed + 1e-5f, "its top stays under the line, margin included");
            // From high above (looking down) nothing needs to shrink.
            var top = PadFit.Fit(new Vector3(0f, 0.6f, 0.4f), NearFeet, 0.05f, WildFeet, 0.05f, 0.5f, false);
            Assert.AreEqual(1f, top.near);
            Assert.IsFalse(top.sightline);
            Assert.IsTrue(float.IsPositiveInfinity(PadFit.AllowedNearHeight(new Vector3(0f, -0.1f, 0.6f), NearFeet, WildFeet, 0.05f, 0.5f)), "eyes below: no rule");
        }

        [Test]
        public void PadFit_WildSide_KeepersExempt_NeverBelowTheFloor()
        {
            Vector3 eye = new Vector3(0f, 1f, 0.3f);
            var wild = PadFit.Fit(eye, NearFeet, 0.04f, WildFeet, 0.15f, 0.5f, false);
            Assert.AreEqual(PadFit.MinFit, wild.wild, 1e-4f, "a huge wild one shrinks, never below the floor");
            var keeper = PadFit.Fit(eye, NearFeet, 0.04f, WildFeet, 0.15f, 0.5f, true);
            Assert.AreEqual(1f, keeper.wild, "a Keeper towers");
            for (float h = 0.02f; h < 0.2f; h += 0.01f)
            {
                var r = PadFit.Fit(new Vector3(0f, 0.12f, 0.6f), NearFeet, h, WildFeet, 0.04f, 0.5f, false);
                Assert.That(r.near, Is.InRange(PadFit.MinFit, 1f));
                Assert.That(r.wild, Is.InRange(PadFit.MinFit, 1f));
            }
        }

        // ---- D: the Journal ----

        [Test]
        public void Journal_StatesAndLines()
        {
            var big = Species("Big", Element.Water);
            var small = Species("Small", Element.Water, big, 12);
            var e = new JournalData.Entry { species = small, hint = "Lilymere, the pond" };
            var eb = new JournalData.Entry { species = big, from = small };
            var caught = new HashSet<string>();
            var seen = new HashSet<string>();
            Assert.AreEqual(Journal.State.Unseen, Journal.StateOf(small, caught, seen));
            Assert.AreEqual("???", Journal.NameLine(e, Journal.State.Unseen));
            Assert.AreEqual("Lilymere, the pond", Journal.StatusLine(e, Journal.State.Unseen, 0, caught));
            Assert.AreEqual("???", Journal.StatusLine(eb, Journal.State.Unseen, 0, caught), "its base isn't caught yet");
            Assert.AreEqual("Rare", Journal.StatusLine(new JournalData.Entry { species = small, hint = "Rare" }, Journal.State.Unseen, 0, caught));
            Assert.AreEqual("A starter", Journal.StatusLine(new JournalData.Entry { species = small }, Journal.State.Unseen, 0, caught));

            seen.Add("Small");
            Assert.AreEqual(Journal.State.Seen, Journal.StateOf(small, caught, seen));
            Assert.AreEqual("Small", Journal.NameLine(e, Journal.State.Seen));
            Assert.AreEqual("Seen", Journal.StatusLine(e, Journal.State.Seen, 0, caught));

            caught.Add("Small");
            Assert.AreEqual(Journal.State.Caught, Journal.StateOf(small, caught, seen), "caught wins over seen");
            var owned = new List<IslingInstance> { new IslingInstance(small, 7), new IslingInstance(small, 11) };
            Assert.AreEqual("Lv 11", Journal.StatusLine(e, Journal.State.Caught, Journal.BestLevel(small, owned), caught), "its best one");
            Assert.AreEqual("Caught", Journal.StatusLine(e, Journal.State.Caught, 0, caught), "none with the player now");
            Assert.AreEqual("Evolves from Small", Journal.StatusLine(eb, Journal.State.Unseen, 0, caught), "once the base is caught");
            Assert.AreEqual("New Isling! Toastling · 13 / 46", Journal.ToastLine("Toastling", 13, 46));
        }

        [Test]
        public void Journal_CountsAndOldSaves()
        {
            var fire = Species("F1", Element.Fire);
            var water = Species("W1", Element.Water);
            var wind = Species("A1", Element.Wind);
            var entries = new List<JournalData.Entry>
            {
                new JournalData.Entry { species = fire }, new JournalData.Entry { species = water }, new JournalData.Entry { species = wind },
            };
            var caught = new HashSet<string>();
            // An old save: the pods and the lodge count as caught, nothing as seen.
            int added = Journal.Migrate(caught, new[] { new IslingInstance(fire, 5), new IslingInstance(wind, 9), new IslingInstance(fire, 3) });
            Assert.AreEqual(2, added);
            Assert.AreEqual(2, Journal.Found(entries, caught));
            Assert.AreEqual(3, Journal.Total(entries));
            Assert.AreEqual(1, Journal.Found(entries, caught, Element.Fire));
            Assert.AreEqual(0, Journal.Found(entries, caught, Element.Water));
            Assert.AreEqual(0, Journal.Migrate(caught, new[] { new IslingInstance(fire, 5) }), "nothing twice");
        }

        [Test]
        public void Journal_Order_LinesTogether_ElementsInOrder()
        {
            var b2 = Species("B2", Element.Fire);
            var b1 = Species("B1", Element.Fire, b2, 14);
            var w = Species("W", Element.Water);
            var a = Species("A", Element.Wind);
            var keeper = Species("K", Element.Wind);
            keeper.keeper = true;
            var spirit = Species(Journal.SpiritId, Element.Neutral);
            var order = Journal.Order(new[] { a, b2, w, keeper, b1, spirit });
            CollectionAssert.AreEqual(new[] { b1, b2, w, a }, order, "Fire (base, then its form), Water, Wind; no Keeper, no Spirit");
        }

        [Test]
        public void Journal_Hints_FromTheTables()
        {
            var s = Species("S");
            var r = Species("R");
            var grass = ScriptableObject.CreateInstance<EncounterTable>();
            grass.entries = new[] { new EncounterTable.Entry { species = s, weight = 1f }, new EncounterTable.Entry { species = r, weight = 0.1f, rare = true } };
            _made.Add(grass);
            var tables = new List<(string, string, EncounterTable)> { ("Galewick", "kite field", grass) };
            Assert.AreEqual("Galewick, kite field", Journal.Hint(s, tables));
            Assert.AreEqual("Rare", Journal.Hint(r, tables));
            Assert.IsNull(Journal.Hint(Species("Nowhere"), tables));
        }

        // ---- E: temples warn; Keepers tuned to the team ----

        [Test]
        public void Temple_Warns_UnderTheSuggestedTeam_NeverInTheQuickJourney()
        {
            Assert.AreEqual(4, TempleRules.Needed(true, false));
            Assert.AreEqual(6, TempleRules.Needed(false, false));
            Assert.AreEqual(0, TempleRules.Needed(true, true), "no warning in the Quick Journey");
            Assert.IsFalse(TempleRules.CanEnter(3, 4), "3 at Galewick: the warning");
            Assert.IsTrue(TempleRules.CanEnter(6, 6), "a full team: straight in");
            Assert.AreEqual("temple.warn", TempleRules.Line(6));
            var s = UnityEditor.AssetDatabase.LoadAssetAtPath<DialogueScript>("Assets/_Project/Data/Dialogue/Script_v1.asset");
            if (s == null) return;
            var t = s.Find(TempleRules.WarnLine);
            Assert.IsNotNull(t, "temple.warn (run the 14h setup)");
            StringAssert.Contains("{keeper}", t.lines[0].text);
            StringAssert.Contains("{n}", t.lines[0].text);
        }

        [Test]
        public void KeeperScaling_MonotonicInTeamLevel_Capped()
        {
            foreach (var keeper in new[] { 8, 14, 13 })
                foreach (float off in new[] { KeeperScaling.GaleowlOffset, KeeperScaling.HearthstagOffset, KeeperScaling.TidemotherOffset })
                    for (int fighters = 1; fighters <= 3; fighters++)
                    {
                        float lastGap = float.NegativeInfinity;
                        int lastLevel = 0;
                        // Whole team levels (every fighter one level up). A single Isling's level-up moves the average
                        // by a third and can meet the Keeper's whole-level step: then the edge dips for that one step.
                        for (float team = 1f; team <= 30f; team += 1f)
                        {
                            int l = KeeperScaling.Level(keeper, team, fighters, KeeperScaling.Follow, off);
                            float gap = team - l;
                            Assert.GreaterOrEqual(gap, lastGap - 1e-4f, $"Keeper {keeper}, team {team}: levelling up never shrinks the gap");
                            Assert.GreaterOrEqual(l, lastLevel, "it never drops as the team grows");
                            Assert.LessOrEqual(l, keeper + KeeperScaling.Up, "capped above");
                            Assert.GreaterOrEqual(l, keeper - KeeperScaling.Down, "floored below");
                            lastGap = gap;
                            lastLevel = l;
                        }
                    }
        }

        [Test]
        public void KeeperScaling_TeamOfThreeStrongest_SmallTeamsEased()
        {
            var s = Species("T");
            var team = new List<IslingInstance> { new IslingInstance(s, 5), new IslingInstance(s, 12), new IslingInstance(s, 10), new IslingInstance(s, 8) };
            Assert.AreEqual(10f, KeeperScaling.TeamLevel(team), 1e-4f, "the three strongest: 12, 10, 8");
            Assert.AreEqual(3, KeeperScaling.Fighters(team));
            Assert.AreEqual(1, KeeperScaling.Fighters(new[] { new IslingInstance(s, 5) }));
            int alone = KeeperScaling.Level(8, 8f, 1), three = KeeperScaling.Level(8, 8f, 3);
            Assert.Less(alone, three, "a lone Isling meets an easier Keeper");
            Assert.AreEqual(8, KeeperScaling.Level(8, 0f, 0), "no team: unchanged");
        }

        // ---- F: carrying the Spirit to the crystal ----

        [Test]
        public void Finale_SaysTheCrystal_GhostLiftsPalmToCrystal()
        {
            CollectionAssert.Contains(GhostPlaceholders.Names, FinaleDirector.LiftClip);
            var s = UnityEditor.AssetDatabase.LoadAssetAtPath<DialogueScript>("Assets/_Project/Data/Dialogue/Script_v1.asset");
            if (s == null) return;
            StringAssert.Contains("gold crystal on its forehead", s.Find("spirit.finale.hand").lines[0].text);
        }

        [Test]
        public void Journal_Baked_46Islings_20_14_12()
        {
            var data = Resources.Load<JournalData>(JournalData.ResourcePath);
            if (data == null) Assert.Inconclusive("Bake the Journal (Tools > Pocket Isle > Journal > Bake Journal).");
            Assert.AreEqual(46, Journal.Total(data.entries));
            Assert.AreEqual(20, Journal.Total(data.entries, Element.Fire));
            Assert.AreEqual(14, Journal.Total(data.entries, Element.Water));
            Assert.AreEqual(12, Journal.Total(data.entries, Element.Wind));
            Assert.IsNotNull(data.atlas);
            Assert.IsNotNull(data.portraitMaterial);
            var cells = new HashSet<int>();
            foreach (var e in data.entries)
            {
                Assert.IsTrue(Journal.InJournal(e.species), e.species.name);
                Assert.IsTrue(cells.Add(e.cell), $"{e.species.name}: a cell of its own");
                Assert.Less(e.cell, data.columns * data.rows);
                if (e.from == null) Assert.IsFalse(string.IsNullOrEmpty(e.hint) && e.species.name != "Kilnpot", $"{e.species.name} has a home");
            }
        }
    }
}
