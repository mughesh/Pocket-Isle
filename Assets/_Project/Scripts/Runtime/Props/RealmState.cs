using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// How restored the current realm is (0 = ash grey and cold, 1 = restored). Mirrored to the shader global
    /// _RealmRestored, which props read for their colours and glows; the island blends its theme colours with it.
    /// </summary>
    public static class RealmState
    {
        static readonly int RestoredId = Shader.PropertyToID("_RealmRestored");
        static float _restored = 1f;

        public static float Restored
        {
            get => _restored;
            set
            {
                _restored = Mathf.Clamp01(value);
                Shader.SetGlobalFloat(RestoredId, _restored);
            }
        }

        /// <summary>Pushes the value to the shader global again (after a domain reload or a scene load).</summary>
        public static void Sync() => Shader.SetGlobalFloat(RestoredId, _restored);
    }
}
