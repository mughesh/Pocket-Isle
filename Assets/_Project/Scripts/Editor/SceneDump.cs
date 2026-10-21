using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Writes the open scene's hierarchy (active state + component types) to Logs/ClaudeBridge/hierarchy.txt
    /// so Claude can inspect prefab-heavy scenes such as the Building Block rigs.
    /// Batch mode: -executeMethod Diorama.EditorTools.SceneDump.DumpMrMain
    /// </summary>
    static class SceneDump
    {
        const string OutFile = "Logs/ClaudeBridge/hierarchy.txt";

        [MenuItem("Tools/Diorama/Debug/Dump Open Scene Hierarchy")]
        static void DumpOpenScene() => Dump(SceneManager.GetActiveScene());

        static void DumpMrMain() => Dump(EditorSceneManager.OpenScene("Assets/_Project/Scenes/MR_Main.unity"));

        static void Dump(Scene scene)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# {scene.path}");
            foreach (var root in scene.GetRootGameObjects()) Write(root.transform, 0, sb);
            Directory.CreateDirectory(Path.GetDirectoryName(OutFile));
            File.WriteAllText(OutFile, sb.ToString());
            Debug.Log($"[Diorama] Hierarchy written to {Path.GetFullPath(OutFile)}");
        }

        static void Write(Transform t, int depth, StringBuilder sb)
        {
            var comps = t.GetComponents<Component>()
                .Where(c => c != null && !(c is Transform))
                .Select(c => c is Behaviour b && !b.enabled ? $"({c.GetType().Name})" : c.GetType().Name);
            sb.Append(' ', depth * 2)
              .Append(t.gameObject.activeSelf ? "" : "[off] ")
              .Append(t.name).Append("  : ").AppendLine(string.Join(", ", comps));
            foreach (Transform child in t) Write(child, depth + 1, sb);
        }
    }
}
