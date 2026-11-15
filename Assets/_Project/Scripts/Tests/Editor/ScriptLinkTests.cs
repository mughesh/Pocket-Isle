using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEditor;

namespace Diorama.Tests
{
    /// <summary>
    /// Nov 15 (Mugesh: the build showed the code-made ghost hands, the Editor the recorded ones): a ScriptableObject or
    /// MonoBehaviour class that shares a file with another class has no script asset, so its assets save "m_Script: 0".
    /// The Editor still finds the class by name; a build does not, and the asset loads empty.
    /// </summary>
    public class ScriptLinkTests
    {
        [Test]
        public void EveryOurAsset_HasItsScriptLinked()
        {
            var bad = new StringBuilder();
            foreach (var path in Directory.GetFiles("Assets/_Project", "*.*", SearchOption.AllDirectories))
            {
                if (!(path.EndsWith(".asset") || path.EndsWith(".unity") || path.EndsWith(".prefab"))) continue;
                using (var r = new StreamReader(path))
                {
                    string line;
                    int n = 0;
                    while ((line = r.ReadLine()) != null)
                    {
                        n++;
                        if (line.Contains("m_Script: {fileID: 0}")) bad.Append($"\n  {path.Replace('\\', '/')}:{n}");
                    }
                }
            }
            Assert.IsEmpty(bad.ToString(), "Assets with no script linked (they load empty in a build):" + bad);
        }

        [Test]
        public void GhostClipLibrary_HasItsOwnScript()
        {
            var lib = AssetDatabase.LoadAssetAtPath<GhostClipLibrary>("Assets/_Project/Data/Onboarding/GhostClips.asset");
            Assert.IsNotNull(lib, "GhostClips.asset doesn't load as a GhostClipLibrary.");
            Assert.IsNotNull(MonoScript.FromScriptableObject(lib), "GhostClipLibrary has no script asset (a build loads it empty).");
            Assert.Greater(lib.clips.Count, 0);
        }
    }
}
