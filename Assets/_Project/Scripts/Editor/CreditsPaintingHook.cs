using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 19 (was the step 17 J standee hook): when Mugesh's cartoon exists, the big credits lantern carries it as a
    /// framed painting: assigned to <see cref="FinaleDirector.painting"/> as the scene is processed (builds and entering
    /// Play), so the file is enough and the scene file never changes. No file = the big lantern shows only its text.
    /// </summary>
    class CreditsPaintingHook : IProcessSceneWithReport
    {
        public const string PicturePath = "Assets/_Project/Art/Credits/Mugesh.png";

        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(PicturePath);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var f in root.GetComponentsInChildren<FinaleDirector>(true))
                {
                    f.painting = tex;
                    Debug.Log(tex != null ? $"[CreditsPaintingHook] {scene.name}: the big lantern's painting is {PicturePath}."
                                          : $"[CreditsPaintingHook] {scene.name}: no {PicturePath}, the big lantern shows only its text.");
                }
        }
    }
}
