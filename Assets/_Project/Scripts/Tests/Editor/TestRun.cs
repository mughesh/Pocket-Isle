using System.Text;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>Runs the Edit mode tests and logs one summary line (for the Claude bridge: exec:Diorama.Tests.TestRun.EditMode).</summary>
    public static class TestRun
    {
        public static void EditMode()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Callbacks());
            api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode, assemblyNames = new[] { "Diorama.Tests.Editor" } }));
        }

        /// <summary>
        /// Calls every [Test] in this assembly directly (with [SetUp] / [TearDown]), without the Test Runner. Its pipeline
        /// can stall at the prebuild step while the Editor is in the background; these tests are plain, so this is enough.
        /// </summary>
        public static void Direct()
        {
            int pass = 0, fail = 0;
            var failures = new StringBuilder();
            foreach (var type in typeof(TestRun).Assembly.GetTypes())
            {
                var methods = type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                System.Reflection.MethodInfo setUp = null, tearDown = null;
                foreach (var m in methods)
                {
                    if (m.IsDefined(typeof(NUnit.Framework.SetUpAttribute), true)) setUp = m;
                    if (m.IsDefined(typeof(NUnit.Framework.TearDownAttribute), true)) tearDown = m;
                }
                foreach (var m in methods)
                {
                    if (!m.IsDefined(typeof(NUnit.Framework.TestAttribute), true)) continue;
                    var fixture = System.Activator.CreateInstance(type);
                    try
                    {
                        if (setUp != null) setUp.Invoke(fixture, null);
                        m.Invoke(fixture, null);
                        pass++;
                    }
                    catch (System.Exception e)
                    {
                        fail++;
                        var inner = e is System.Reflection.TargetInvocationException && e.InnerException != null ? e.InnerException : e;
                        failures.Append($"\n  FAIL {type.Name}.{m.Name}: {inner.Message}");
                    }
                    finally
                    {
                        try { if (tearDown != null) tearDown.Invoke(fixture, null); }
                        catch (System.Exception e) { failures.Append($"\n  TEARDOWN {type.Name}.{m.Name}: {e.Message}"); }
                    }
                }
            }
            Debug.Log($"[TestRun] Direct: {pass} passed, {fail} failed.{failures}");
        }

        class Callbacks : ICallbacks
        {
            readonly StringBuilder _failures = new StringBuilder();

            public void RunStarted(ITestAdaptor testsToRun) => _failures.Clear();

            public void RunFinished(ITestResultAdaptor result) =>
                Debug.Log($"[TestRun] Edit mode: {result.PassCount} passed, {result.FailCount} failed, {result.SkipCount} skipped.{_failures}");

            public void TestStarted(ITestAdaptor test) { }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (!result.HasChildren && result.TestStatus == TestStatus.Failed)
                    _failures.Append($"\n  FAIL {result.Test.Name}: {result.Message}");
            }
        }
    }
}
