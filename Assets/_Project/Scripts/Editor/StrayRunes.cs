using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Nov 6: removes menu runes that got saved at the scene root (the game builds them at runtime under its rune board;
    /// a save during Play-adjacent bridge work left five orphans that would float at the room origin in a build).
    /// </summary>
    public static class StrayRunes
    {
        [MenuItem("Tools/Pocket Isle/Debug/Remove Stray Runes From Scene", priority = 200)]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[StrayRunes] Stop Play mode first.");
                return;
            }
            var scene = EditorSceneManager.GetActiveScene();
            int removed = 0;
            foreach (var go in scene.GetRootGameObjects())
            {
                if (!go.name.StartsWith("Rune ")) continue;
                Debug.Log($"[StrayRunes] Removing root '{go.name}' ({go.transform.childCount} children).");
                Object.DestroyImmediate(go);
                removed++;
            }
            if (removed > 0) EditorSceneManager.SaveScene(scene);
            Debug.Log($"[StrayRunes] {removed} stray rune(s) removed from {scene.path}.");
        }
    }
}
