using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 22 (Mugesh, Nov 8: the intro shard's cracks look like one flat texture; the lava Isling too): deep cracks
    /// (Toon _CrackDepth / _CrackFlow / _CrackInner) on the opening shard's material and on the cracked Islings: their
    /// recipes (for the next bake) and their baked prefabs' piece styles (no re-bake needed).
    /// </summary>
    public static class Step22Setup
    {
        const string ShardMat = "Assets/_Project/Materials/Props/OpeningShard.mat";

        /// <summary>(recipe / prefab name, depth, flow, inner glow).</summary>
        static readonly (string name, float depth, float flow, float inner)[] Islings =
        {
            ("Glazegolem", 0.1f, 0.8f, 0.25f),
            ("Kilngolem", 0.12f, 1f, 0.35f), // evolves from Glazegolem: its cracks are inherited, its prefab still holds its own style
            ("Cindercap", 0.08f, 0.7f, 0.2f),
            ("Cinderbloom", 0.08f, 0.7f, 0.2f),
        };

        [MenuItem("Tools/Pocket Isle/Step 22/Deep Cracks (shard + lava Islings)")]
        public static void Crystal()
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(ShardMat);
            if (m == null)
            {
                Debug.LogError($"[Step22Setup] No shard material at {ShardMat}.");
                return;
            }
            m.SetFloat("_CrackOn", 1f);
            m.SetFloat("_CrackDepth", 0.3f);
            m.SetFloat("_CrackFlow", 1f);
            m.SetFloat("_CrackInner", 0.45f);
            m.SetFloat("_CrackScale", 2.6f);
            m.SetFloat("_CrackWidth", 0.05f);
            m.SetFloat("_CrackPulse", 0.6f);
            // A smoky crystal (was grey stone with a green top) so the veins inside read.
            m.SetColor("_BaseColor", new Color(0.26f, 0.25f, 0.3f));
            m.SetColor("_TopColor", new Color(0.4f, 0.38f, 0.46f));
            m.SetFloat("_Emissive", 0.25f);
            m.SetFloat("_Spec", 0.9f);
            EditorUtility.SetDirty(m);

            int recipes = 0, pieces = 0;
            foreach (var (name, depth, flow, inner) in Islings)
            {
                var r = AssetDatabase.LoadAssetAtPath<CreatureRecipe>($"Assets/_Project/Data/Creatures/Recipes/{name}.asset");
                if (r == null) Debug.LogError($"[Step22Setup] No recipe {name}.");
                else
                {
                    var so = new SerializedObject(r);
                    so.FindProperty("zones.crackDepth").floatValue = depth;
                    so.FindProperty("zones.crackFlow").floatValue = flow;
                    so.FindProperty("zones.crackInner").floatValue = inner;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    recipes++;
                }
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Prefabs/Creatures/{name}.prefab");
                var view = prefab != null ? prefab.GetComponentInChildren<CreatureView>(true) : null;
                if (view == null)
                {
                    Debug.LogError($"[Step22Setup] No prefab with a CreatureView for {name}.");
                    continue;
                }
                var vo = new SerializedObject(view);
                var list = vo.FindProperty("pieces");
                for (int i = 0; i < list.arraySize; i++)
                {
                    var st = list.GetArrayElementAtIndex(i).FindPropertyRelative("style");
                    if (!st.FindPropertyRelative("cracks").boolValue) continue;
                    st.FindPropertyRelative("crackDepth").floatValue = depth;
                    st.FindPropertyRelative("crackFlow").floatValue = flow;
                    st.FindPropertyRelative("crackInner").floatValue = inner;
                    pieces++;
                }
                vo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(prefab);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Step22Setup] Deep cracks: the shard (depth 0.3, flow 1, inner 0.45), {recipes} recipes, {pieces} cracked prefab pieces.");
        }
    }
}
