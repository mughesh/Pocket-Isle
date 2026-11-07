using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 17 J: when Assets/_Project/Art/Credits/Mugesh.png exists, the finale's credits standee uses it: assigned to
    /// <see cref="FinaleDirector.standee"/> as the scene is processed (builds and entering Play), so dropping the file in
    /// is enough and the scene file never changes. No file = no standee (no placeholder).
    /// </summary>
    class CreditsStandeeHook : IProcessSceneWithReport
    {
        public const string PicturePath = "Assets/_Project/Art/Credits/Mugesh.png";

        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(PicturePath);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var f in root.GetComponentsInChildren<FinaleDirector>(true))
                {
                    f.standee = tex;
                    Debug.Log(tex != null ? $"[CreditsStandeeHook] {scene.name}: the credits standee is {PicturePath}."
                                          : $"[CreditsStandeeHook] {scene.name}: no {PicturePath}, no credits standee.");
                }
        }
    }
}
