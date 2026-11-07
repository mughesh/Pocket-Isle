using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 19 (edit mode, bridge or menu): the credits' sky lantern material (Materials/SkyLantern.mat on
    /// Diorama/Sky Lantern; it paints the big lantern's picture too) assigned to the scene's FinaleDirector,
    /// Mugesh.png imported for the painting (alpha is transparency, clamped). Saves the scene.
    /// Menu: Tools > Diorama > Credits > Set Up Sky Lanterns.
    /// </summary>
    public static class Step19Setup
    {
        const string LanternPath = "Assets/_Project/Materials/SkyLantern.mat";

        [MenuItem("Tools/Diorama/Credits/Set Up Sky Lanterns")]
        public static void Lanterns()
        {
            if (EditorApplication.isPlaying) return;
            var lantern = Material(LanternPath, "Diorama/Sky Lantern");
            if (lantern == null) return;
            if (AssetImporter.GetAtPath(CreditsPaintingHook.PicturePath) is TextureImporter imp)
            {
                imp.alphaIsTransparency = true;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.mipmapEnabled = true;
                imp.maxTextureSize = 512;
                imp.SaveAndReimport();
            }
            int n = 0;
            foreach (var f in Object.FindObjectsByType<FinaleDirector>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Undo.RecordObject(f, "Sky lanterns");
                f.lanternMaterial = lantern;
                f.lanternSpacing = 0.175f; // wide enough that the big one never touches a neighbour
                f.lanternBigDelay = 1.2f;
                EditorUtility.SetDirty(f);
                n++;
            }
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Step19Setup] Sky lantern materials on {n} FinaleDirector(s); {CreditsPaintingHook.PicturePath} imported.");
        }

        static Material Material(string path, string shaderName)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError($"[Step19Setup] No shader {shaderName}.");
                return null;
            }
            m = new Material(shader) { name = System.IO.Path.GetFileNameWithoutExtension(path) };
            AssetDatabase.CreateAsset(m, path);
            return m;
        }
    }
}
