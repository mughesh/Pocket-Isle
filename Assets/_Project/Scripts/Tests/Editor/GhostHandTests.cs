using NUnit.Framework;
using Oculus.Interaction.Input;
using UnityEditor;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>Step 8e: a ghost hand copy keeps meshes only, so it can never be taken for the player's hand.</summary>
    public class GhostHandTests
    {
        const string RigHandVisual = "Packages/com.meta.xr.sdk.interaction/Runtime/Prefabs/Hands/OVRHandVisualRight.prefab";

        [Test]
        public void StrippedCopyHasNoHand()
        {
            var root = new GameObject("Hand copy");
            try
            {
                root.AddComponent<Hand>();
                var child = new GameObject("Mesh");
                child.transform.SetParent(root.transform, false);
                child.AddComponent<SkinnedMeshRenderer>();
                child.AddComponent<SphereCollider>();
                Assert.IsTrue(GhostHands.StripToMesh(root));
                Assert.AreEqual(0, root.GetComponentsInChildren<IHand>(true).Length);
                Assert.AreEqual(0, root.GetComponentsInChildren<Collider>(true).Length);
                Assert.IsNotNull(child.GetComponent<SkinnedMeshRenderer>());
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void StrippedRigHandVisualHasNoHand()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RigHandVisual);
            if (prefab == null) Assert.Ignore($"No {RigHandVisual}.");
            var copy = (GameObject)Object.Instantiate(prefab);
            try
            {
                Assert.IsTrue(GhostHands.StripToMesh(copy));
                Assert.AreEqual(0, copy.GetComponentsInChildren<IHand>(true).Length);
                Assert.AreEqual(0, copy.GetComponentsInChildren<MonoBehaviour>(true).Length);
            }
            finally
            {
                Object.DestroyImmediate(copy);
            }
        }
    }
}
