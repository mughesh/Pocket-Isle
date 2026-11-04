using System.IO;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Overnight Nov 5: Tools > Pocket Isle > Polish > Create Config: Data/Resources/PolishConfig.asset (all polish items
    /// on) and its critter material (Materials/Props/Critter.mat, Diorama/Critter, GPU instancing on). Re-running keeps an
    /// existing config's switches.
    /// </summary>
    public static class PolishSetup
    {
        const string ConfigPath = "Assets/_Project/Data/Resources/" + PolishConfig.ResourceName + ".asset";
        const string MaterialPath = "Assets/_Project/Materials/Props/Critter.mat";

        [MenuItem("Tools/Pocket Isle/Polish/Create Config", priority = 120)]
        public static void Create()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat == null)
            {
                var shader = Shader.Find("Diorama/Critter");
                if (shader == null)
                {
                    Debug.LogError("[Polish] No Diorama/Critter shader (Shaders/Critter.shader).");
                    return;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(MaterialPath));
                mat = new Material(shader) { name = "Critter", enableInstancing = true };
                AssetDatabase.CreateAsset(mat, MaterialPath);
            }
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            var cfg = AssetDatabase.LoadAssetAtPath<PolishConfig>(ConfigPath);
            if (cfg == null)
            {
                cfg = ScriptableObject.CreateInstance<PolishConfig>();
                AssetDatabase.CreateAsset(cfg, ConfigPath);
            }
            cfg.critterMaterial = mat;
            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Polish] {ConfigPath}: ambient life {cfg.ambientLife}, battle feel {cfg.battleFeel}, restoration waves {cfg.restoreWave}; material {MaterialPath}.");
        }
    }
}
