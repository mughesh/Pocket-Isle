using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>
    /// Step 18 (Nov 8 long play): battles must not wipe a cozy player. Evolving heals, wild Islings never knock out a
    /// full-HP party Isling of their level or higher in under 3 hits (2 when super effective), a forced switch-in acts
    /// first, and the pod lights up at a third of the HP bar.
    /// </summary>
    public class Step18bTests
    {
        const string SpeciesDir = "Assets/_Project/Data/Creatures/Species";
        static readonly string[] Wilds = { "Samarling", "Vanehare", "Kitelet", "Ashmole", "Coalbeetle" };

        static IslingSpecies Load(string name)
        {
            var s = AssetDatabase.LoadAssetAtPath<IslingSpecies>($"{SpeciesDir}/{name}.asset");
            if (s == null) Assert.Inconclusive($"{name} missing.");
            return s;
        }

        static List<IslingSpecies> PartySpecies()
        {
            var reg = AssetDatabase.LoadAssetAtPath<SpeciesRegistry>("Assets/_Project/Data/Creatures/SpeciesRegistry.asset");
            if (reg == null) Assert.Inconclusive("No species registry.");
            var list = new List<IslingSpecies>();
            foreach (var s in reg.species)
                if (s != null && !s.keeper && s.name != "LanternSpirit" && s.moves != null && s.moves.Length > 0) list.Add(s);
            return list;
        }

        static TypeChart Chart()
        {
            var c = AssetDatabase.LoadAssetAtPath<TypeChart>("Assets/_Project/Data/Battle/TypeChart.asset");
            if (c == null) Assert.Inconclusive("No type chart.");
            return c;
        }

        /// <summary>
        /// Fewest hits the wild one needs on a full-HP defender: its hardest-hitting attack at the top roll (1.1), with
        /// the wild damage share. Returns the hits and whether that move is super effective.
        /// </summary>
        public static int HitsToKo(IslingInstance wild, IslingInstance target, TypeChart chart, out bool super) =>
            HitsToKo(wild, target, chart, BattleMath.WildDamageShare, out super);

        public static int HitsToKo(IslingInstance wild, IslingInstance target, TypeChart chart, float share, out bool super)
        {
            int best = 0;
            super = false;
            foreach (var m in wild.species.moves)
            {
                if (m == null || m.kind != MoveKind.Attack) continue;
                var e = chart.Matchup(m.element, target.Element);
                int d = BattleMath.Damage(wild.level, m.power, wild.Attack, target.Defense,
                    chart.Multiplier(e) * share, BattleMath.RollMax);
                if (d > best)
                {
                    best = d;
                    super = e == Effectiveness.Super;
                }
            }
            return best <= 0 ? int.MaxValue : Mathf.CeilToInt(target.MaxHp / (float)best);
        }

        [Test]
        public void WildIslings_NeverOneOrTwoShotAPartyIslingOfTheirLevel()
        {
            var chart = Chart();
            var party = PartySpecies();
            var sb = new StringBuilder($"[Step18] Wild hits-to-KO (share {BattleMath.WildDamageShare}): wild Lv / party +0,+1,+2 -> fewest hits (worst defender)\n");
            var fails = new List<string>();
            foreach (var name in Wilds)
            {
                var ws = Load(name);
                for (int lv = 5; lv <= 8; lv++)
                {
                    var wild = new IslingInstance(ws, lv);
                    sb.Append($"{name} Lv {lv}:");
                    for (int plus = 0; plus <= 2; plus++)
                    {
                        int worst = int.MaxValue;
                        string who = "";
                        foreach (var ps in party)
                        {
                            var t = new IslingInstance(ps, lv + plus);
                            int hits = HitsToKo(wild, t, chart, out bool super);
                            if (hits < worst)
                            {
                                worst = hits;
                                who = ps.name + (super ? "*" : "");
                            }
                            if (hits < (super ? 2 : 3)) fails.Add($"{name} Lv {lv} KOs {ps.name} Lv {lv + plus} in {hits}{(super ? " (super)" : "")}");
                        }
                        sb.Append($" +{plus}: {worst} ({who})");
                    }
                    sb.Append('\n');
                }
            }
            Debug.Log(sb.ToString());
            Assert.IsEmpty(fails, string.Join("\n", fails));
        }

        [Test]
        public void WildShareSweep_Logged()
        {
            var chart = Chart();
            var party = PartySpecies();
            var sb = new StringBuilder("[Step18] Wild share sweep (fails of the 3-hit rule):");
            for (float share = 1f; share > 0.55f; share -= 0.05f)
            {
                int fails = 0;
                foreach (var name in Wilds)
                {
                    var ws = Load(name);
                    for (int lv = 5; lv <= 8; lv++)
                        for (int plus = 0; plus <= 2; plus++)
                            foreach (var ps in party)
                            {
                                int hits = HitsToKo(new IslingInstance(ws, lv), new IslingInstance(ps, lv + plus), chart, share, out bool super);
                                if (hits < (super ? 2 : 3)) fails++;
                            }
                }
                sb.Append($" {share:0.00}: {fails};");
            }
            Debug.Log(sb.ToString());
        }

        [Test]
        public void Evolving_ComesOutAtFullHp_EvenFromFainted()
        {
            var s = Load("Toastling");
            if (s.evolvesTo == null) Assert.Inconclusive("Toastling doesn't evolve.");
            var c = new IslingInstance(s, s.evolveLevel) { currentHp = 0 };
            Assert.IsTrue(c.IsFainted);
            Assert.IsTrue(Progression.Evolve(c));
            Assert.AreEqual(s.evolvesTo, c.species);
            Assert.AreEqual(c.MaxHp, c.currentHp);
            Assert.IsFalse(c.IsFainted);

            var d = new IslingInstance(s, s.evolveLevel);
            d.currentHp = 3;
            Progression.Evolve(d);
            Assert.AreEqual(d.MaxHp, d.currentHp);
        }

        [Test]
        public void RestAfterWin_HealsAQuarter_FaintedStayFainted()
        {
            var s = Load("Toastling");
            var a = new IslingInstance(s, 6);
            var b = new IslingInstance(s, 6) { currentHp = 0 };
            var c = new IslingInstance(s, 6);
            a.currentHp = 2;
            c.currentHp = c.MaxHp - 1;
            var healed = Party.RestAfterWin(new List<IslingInstance> { a, b, c, null });
            Assert.AreEqual(2 + Mathf.CeilToInt(a.MaxHp * Party.RestShare), a.currentHp);
            Assert.AreEqual(0, b.currentHp);
            Assert.AreEqual(c.MaxHp, c.currentHp);
            Assert.AreEqual(2, healed);
        }

        [Test]
        public void ForcedSwitchIn_ActsFirst()
        {
            var chart = Chart();
            var slow = Load("Samarling");
            var fast = Load("Vanehare");
            // A slow party Isling against a much faster wild one: normally the wild one strikes first.
            for (int seed = 1; seed < 20; seed++)
            {
                var p1 = new IslingInstance(slow, 5) { currentHp = 1 };
                var p2 = new IslingInstance(slow, 5);
                var w = new IslingInstance(fast, 9);
                var b = new BattleSystem(p1, w, chart, seed, hasReserve: () => !p2.IsFainted);
                // Knock out p1 with whatever happens; repeat turns until it must switch.
                for (int i = 0; i < 10 && !b.PlayerMustSwitch && !b.IsOver; i++) b.PlayTurn(0);
                if (!b.PlayerMustSwitch) continue;
                b.Switch(p2);
                var turn = b.PlayTurn(0);
                foreach (var e in turn)
                {
                    if (e.kind != BattleEventKind.MoveUsed) continue;
                    Assert.AreEqual(BattleSide.Player, e.side, $"seed {seed}: the switched-in Isling moves first");
                    break;
                }
                // The turn after that follows speed again.
                if (b.IsOver || b.PlayerMustSwitch || b.WildDazed) return;
                Assert.IsFalse(b.PlayerGoesFirst() && w.Speed > p2.Speed);
                return;
            }
            Assert.Inconclusive("No seed produced a faint.");
        }

        [Test]
        public void PodLights_AtAThird()
        {
            Assert.AreEqual(0.33f, BattleMath.RedHpShare, 1e-4f);
            Assert.IsTrue(BattleMath.CatchUnlocked(14, 100));
            Assert.IsTrue(BattleMath.CatchUnlocked(33, 100));
            Assert.IsFalse(BattleMath.CatchUnlocked(34, 100));
        }
    }
}
