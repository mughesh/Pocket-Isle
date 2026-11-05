using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 16 (grounding): a soft, permanent warm glow ring under the island's rocky underside in every scene, like the
    /// finale's ring of light but much subtler, so the island reads as resting on something even without a table. One
    /// quad in the Beacon material (ring mode), breathing slowly. Made at runtime by <see cref="FinaleDirector"/> (it holds
    /// the Beacon material); hidden while the finale's own ring shows.
    /// </summary>
    public class IslandUnderGlow : MonoBehaviour
    {
        IslandBuilder _island;
        MeshRenderer _renderer;
        MaterialPropertyBlock _block;

        [Tooltip("Glow strength (the finale ring uses about 1.2).")]
        public float intensity = 0.14f;
        [Tooltip("Ring size as a share of the island's footprint.")]
        public float size = 0.95f;
        public Color edge = new Color(1f, 0.72f, 0.42f, 1f), core = new Color(1f, 0.9f, 0.72f, 1f);

        /// <summary>Hidden (the finale's ring is up).</summary>
        public static bool Suppressed;

        /// <summary>Step 16: the Beacon material, for other soft glows (the lodge hearth's floor glow).</summary>
        public static Material Beacon { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Suppressed = false;
            Beacon = null;
        }

        public static IslandUnderGlow Ensure(IslandBuilder island, Material beacon)
        {
            if (island == null || beacon == null) return null;
            Beacon = beacon;
            var existing = island.GetComponentInChildren<IslandUnderGlow>(true);
            if (existing != null) return existing;
            var go = new GameObject("Island Under Glow");
            go.layer = island.gameObject.layer;
            go.transform.SetParent(island.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = BattleHud.VerticalQuad();
            var g = go.AddComponent<IslandUnderGlow>();
            g._island = island;
            g._renderer = go.AddComponent<MeshRenderer>();
            g._renderer.sharedMaterial = beacon;
            g._renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            g._renderer.receiveShadows = false;
            g._block = new MaterialPropertyBlock();
            Debug.Log("[IslandUnderGlow] A soft glow under the island (grounding).");
            return g;
        }

        void LateUpdate()
        {
            if (_island == null || _island.map == null || _renderer == null) return;
            bool on = !Suppressed && _island.transform.lossyScale.x > 0.01f;
            if (_renderer.enabled != on) _renderer.enabled = on;
            if (!on) return;
            var b = _island.LocalBounds;
            transform.localPosition = new Vector3(b.center.x, b.min.y - 0.012f, b.center.z);
            transform.localRotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            transform.localScale = Vector3.one * (Mathf.Max(b.size.x, b.size.z) * size);
            _block.Clear();
            _block.SetFloat("_Mode", 1f);
            _block.SetFloat("_Fill", 1f);
            _block.SetColor("_Color", edge);
            _block.SetColor("_Core", core);
            _block.SetFloat("_Intensity", intensity * (0.9f + 0.1f * Mathf.Sin(Time.time * 0.8f)));
            _renderer.SetPropertyBlock(_block);
        }
    }
}
