using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 16 I: the Hearth Lodge dressing. Writes and bakes the round rug, the hearth stones and the two wall sets
    /// (<see cref="PropKit.WriteLodgeDressing"/>) and puts them in Lodge.asset's props (keys S, A, B). The hearth's floor
    /// glow is runtime (<see cref="LodgeRoom"/>). Menu: Tools > Pocket Isle > Lodge > Dress the Lodge (step 16);
    /// bridge: exec:Diorama.EditorTools.Step16Lodge.Run.
    /// </summary>
    public static class Step16Lodge
    {
        public const string LodgePath = "Assets/_Project/Data/Interiors/Lodge.asset";

        /// <summary>
        /// 8 x 8, row 0 = back. A = bookshelf, fern, cushions (left wall); S = hearth stones (2 x 1, in front of the hearth);
        /// B = side table, lantern, chest (right wall, clear of the door walk). The centre stays free for the Islings.
        /// </summary>
        public const string Grid =
            "wC.E.D.w\n" +
            "vAcS...v\n" +
            "vc....Bv\n" +
            "v..U..cv\n" +
            "........\n" +
            "........\n" +
            "........\n" +
            "........";

        [MenuItem("Tools/Pocket Isle/Lodge/Dress the Lodge (step 16)", priority = 40)]
        public static void Run()
        {
            var lodge = AssetDatabase.LoadAssetAtPath<WorldMap>(LodgePath);
            if (lodge == null)
            {
                Debug.LogError($"[Step16Lodge] No {LodgePath}.");
                return;
            }
            var rec = PropKit.WriteLodgeDressing();
            foreach (var r in rec.Values) PropBaker.Bake(r);
            Apply(lodge, rec);
            Debug.Log($"[Step16Lodge] The lodge is dressed: {string.Join(", ", rec.Keys)} baked; props:\n{Grid}");
        }

        /// <summary>Adds the S, A, B keys (replacing old ones) and the grid; the rug keeps its key U (its pieces changed).</summary>
        public static void Apply(WorldMap lodge, Dictionary<string, PropRecipe> rec)
        {
            var legend = new List<PropLegend>(lodge.propLegend ?? new List<PropLegend>());
            legend.RemoveAll(e => e.key == "S" || e.key == "A" || e.key == "B");
            legend.Add(new PropLegend { key = "S", recipe = rec["LodgeHearthStones"] });
            legend.Add(new PropLegend { key = "A", recipe = rec["LodgeShelfFern"], yaw = 270f });
            legend.Add(new PropLegend { key = "B", recipe = rec["LodgeTableChest"], yaw = 90f });
            lodge.propLegend = legend;
            lodge.SetProps(Grid);
            EditorUtility.SetDirty(lodge);
            AssetDatabase.SaveAssets();
        }
    }
}
