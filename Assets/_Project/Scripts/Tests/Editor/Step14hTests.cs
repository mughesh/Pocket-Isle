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
    }
}
