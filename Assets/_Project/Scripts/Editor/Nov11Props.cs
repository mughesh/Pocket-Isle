using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Nov 11 (Mugesh: "many assets look broken; the tower pillars look jagged"): rewrites the recipes fixed after the prop
    /// audit (<see cref="PropAudit"/>) and bakes only those. Re-run safe.
    /// </summary>
    public static class Nov11Props
    {
        public static readonly string[] Fixed =
        {
            "TowerPillar", "HallPillar", "TemplePillar", "TowerWindow", "TempleWindow", "TangledKites",
            "StoneBasin", "ReedClump", "LilyPad", "WaterBall", "BasinWater", "WindTower", "WindTowerOpen",
        };

        public static void Apply()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Nov11Props] Stop Play first.");
                return;
            }
            var all = new Dictionary<string, PropRecipe>();
            foreach (var kv in PropKit.WriteTowerHallProps()) all[kv.Key] = kv.Value;
            foreach (var kv in PropKit.WriteForgeHallProps()) all[kv.Key] = kv.Value;
            foreach (var kv in PropKit.WriteLakeTempleProps()) all[kv.Key] = kv.Value;
            foreach (var kv in PropKit.WriteLilymereProps()) all[kv.Key] = kv.Value;
            foreach (var r in PropKit.WriteWindTowers()) all[r.name] = r;
            AssetDatabase.SaveAssets();
            foreach (var name in Fixed)
            {
                if (!all.TryGetValue(name, out var r))
                {
                    Debug.LogError($"[Nov11Props] {name}: not written.");
                    continue;
                }
                var report = PropBaker.Bake(r);
                Debug.Log($"[Nov11Props] {name}: {(r.bakedMesh != null ? r.bakedMesh.triangles.Length / 3 : 0)} tris at {r.resolution}.");
            }
            AssetDatabase.SaveAssets();
        }
    }
}
