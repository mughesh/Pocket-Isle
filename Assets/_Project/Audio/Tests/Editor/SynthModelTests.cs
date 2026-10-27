using System.Collections.Generic;
using Diorama.Audio.EditorTools;
using NUnit.Framework;
using UnityEngine;

namespace Diorama.Audio.Tests
{
    /// <summary>Every model preset renders the same samples for the same seed, differs per seed (stochastic models), and stays under -1 dBFS once levelled.</summary>
    public class SynthModelTests
    {
        static readonly float Ceiling = Mathf.Pow(10f, Loudness.CeilingDb / 20f) + 1e-4f;

        readonly List<SynthPatch> _made = new List<SynthPatch>();

        [TearDown]
        public void TearDown()
        {
            foreach (var p in _made) Object.DestroyImmediate(p);
            _made.Clear();
        }

        SynthPatch Patch(string preset, int seed, float hz = 600f)
        {
            var p = ScriptableObject.CreateInstance<SynthPatch>();
            _made.Add(p);
            p.id = "test." + preset;
            p.duration = 0.8f;
            p.seed = seed;
            p.levelMode = LevelMode.Tier;
            p.tier = LoudnessTier.Big; // the loudest tier pushes hardest against the ceiling
            p.layers = new List<SynthLayer> { ModelPresets.Layer(preset, hz) };
            return p;
        }

        static IEnumerable<string> ModelNames()
        {
            foreach (var n in ModelPresets.Names)
                if (n.Contains("/")) yield return n;
        }

        [Test]
        public void EveryModelIsDeterministicPerSeed()
        {
            foreach (var name in ModelNames())
            {
                var a = SynthRenderer.Render(Patch(name, 7));
                var b = SynthRenderer.Render(Patch(name, 7));
                Assert.AreEqual(a.Length, b.Length, name);
                for (int i = 0; i < a.Length; i++)
                    if (a[i] != b[i]) Assert.Fail($"{name}: sample {i} differs between two renders of seed 7");
            }
        }

        [Test]
        public void StochasticModelsChangeWithTheSeed()
        {
            foreach (var name in ModelNames())
            {
                if (name.StartsWith("Modal/")) continue; // a struck bar is the same every time (variants come from jitter)
                var a = SynthRenderer.Render(Patch(name, 7));
                var b = SynthRenderer.Render(Patch(name, 8));
                bool same = a.Length == b.Length;
                for (int i = 0; same && i < a.Length; i++) same = a[i] == b[i];
                Assert.IsFalse(same, $"{name}: seeds 7 and 8 render the same");
            }
        }

        [Test]
        public void EveryModelStaysUnderTheCeilingAndIsNotSilent()
        {
            foreach (var name in ModelNames())
                foreach (var seed in new[] { 1, 2, 3 })
                {
                    var data = SynthRenderer.Render(Patch(name, seed));
                    float peak = SynthRenderer.Peak(data);
                    Assert.IsFalse(float.IsNaN(peak), $"{name} seed {seed}: NaN");
                    Assert.LessOrEqual(peak, Ceiling, $"{name} seed {seed}: peak {20f * Mathf.Log10(peak):0.00} dBFS");
                    Assert.Greater(peak, 0.01f, $"{name} seed {seed}: silent");
                }
        }

        [Test]
        public void ScatterAndGroupsAreDeterministicAndMoveTheOnset()
        {
            var p = Patch("Modal/Wood knock", 3);
            p.layers[0].scatter = new ScatterSettings { count = 6, window = 0.4f, pitchJitter = 3f, gainJitter = 0.5f };
            var a = SynthRenderer.Render(p);
            var b = SynthRenderer.Render(p);
            CollectionAssert.AreEqual(a, b);

            var q = Patch("Modal/Wood knock", 3);
            q.levelMode = LevelMode.Peak;
            q.groups = new List<LayerGroup> { new LayerGroup { label = "body", offset = 0.2f } };
            q.layers[0].group = 0;
            var late = SynthRenderer.Render(q);
            int first = System.Array.FindIndex(late, s => Mathf.Abs(s) > 0.01f);
            Assert.GreaterOrEqual(first, Mathf.RoundToInt(0.19f * SynthPatch.SampleRate), "group offset should delay the layer");

            q.groups[0].enabled = false;
            Assert.AreEqual(0f, SynthRenderer.Peak(SynthRenderer.Render(q)), "a disabled group is silent");
        }
    }

    /// <summary>Bridge: exec:Diorama.Audio.Tests.AudioTestRun.Direct (results in Logs/AudioLab/tests.txt and the Console).</summary>
    public static class AudioTestRun
    {
        public static void Direct()
        {
            int pass = 0, fail = 0;
            var log = new System.Text.StringBuilder();
            foreach (var type in typeof(AudioTestRun).Assembly.GetTypes())
            {
                var methods = type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                System.Reflection.MethodInfo tearDown = null;
                foreach (var m in methods)
                    if (m.IsDefined(typeof(TearDownAttribute), true)) tearDown = m;
                foreach (var m in methods)
                {
                    if (!m.IsDefined(typeof(TestAttribute), true)) continue;
                    var fixture = System.Activator.CreateInstance(type);
                    try
                    {
                        m.Invoke(fixture, null);
                        pass++;
                        log.AppendLine($"PASS {type.Name}.{m.Name}");
                    }
                    catch (System.Exception e)
                    {
                        fail++;
                        var inner = e.InnerException ?? e;
                        log.AppendLine($"FAIL {type.Name}.{m.Name}: {inner.Message}");
                    }
                    finally
                    {
                        if (tearDown != null) tearDown.Invoke(fixture, null);
                    }
                }
            }
            var summary = $"[AudioTests] {pass} passed, {fail} failed.";
            System.IO.Directory.CreateDirectory("Logs/AudioLab");
            System.IO.File.WriteAllText("Logs/AudioLab/tests.txt", summary + "\n" + log);
            Debug.Log(summary + "\n" + log);
        }
    }
}
