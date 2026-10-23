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
