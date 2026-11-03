using System.Text;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>Step 14a bridge helpers: rewrite and check one realm's map without touching the scene.</summary>
    public static class Step14aDebug
    {
        public static void GalewickMap()
        {
            var log = new StringBuilder("[Step14aDebug] Galewick:");
            var map = GalewickTools.Write(log);
            var problems = GalewickTools.Check(map, log);
            log.Append(problems.Count == 0 ? "\n  checks pass" : "\n  PROBLEMS: " + string.Join("; ", problems));
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }
    }
}
