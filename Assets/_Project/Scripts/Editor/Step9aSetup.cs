using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 9a: Cinderwood, the land. C = the new props and the tree fix (written and baked here).
    /// Safe to re-run (overwrites the 9a props).
    /// </summary>
    public static class Step9aSetup
    {
        /// <summary>Writes and bakes the 9a props (and the rebuilt AutumnTree). Logs triangles per prop.</summary>
        public static Dictionary<string, PropRecipe> Props(StringBuilder log)
        {
            var props = PropKit.WriteCinderwoodProps();
            AssetDatabase.SaveAssets();
            foreach (var r in props.Values)
            {
                var report = PropBaker.Bake(r);
                log.Append($"\n  {r.name}: {report.triangles} tris (budget {r.triangleBudget})");
            }
            AssetDatabase.SaveAssets();
            IslandBuilder.RebuildAll();
            return props;
        }

        [MenuItem("Tools/Pocket Isle/Props/Write + Bake Cinderwood Props (9a)", priority = 24)]
        public static void PropsMenu()
        {
            var log = new StringBuilder("[Step9a] Cinderwood props written and baked:");
            Props(log);
            Debug.Log(log.ToString());
        }
    }
}
