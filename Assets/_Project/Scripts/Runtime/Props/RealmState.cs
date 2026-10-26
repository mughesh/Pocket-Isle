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
        static readonly int StillId = Shader.PropertyToID("_RealmStill");
        static readonly int SpinId = Shader.PropertyToID("_WindSpin");
        static float _restored = 1f;
        static float _spin;
        static int _windFrame = -1;

        /// <summary>Step 10a: extra spin pushed into the wind this frame by gusts and flicks (radians). Read once by <see cref="Wind"/>.</summary>
        public static float SpinNudge;

        /// <summary>The wind's accumulated spin angle (radians): windmill sails turn by it (shader global _WindSpin).</summary>
        public static float WindSpin => _spin;

        /// <summary>
        /// Step 10a: once a frame, the wind of the realm showing: how still it is (theme.stillWhenUnrestored, fading out as it
        /// is restored) and the spin of its windmill sails (theme.windSpin x restored, plus any <see cref="SpinNudge"/>).
        /// Realms without wind (null theme, still 0, spin 0) leave sway as it was and nothing spins.
        /// </summary>
        public static void Wind(RealmTheme theme)
        {
            if (Time.frameCount == _windFrame) return;
            _windFrame = Time.frameCount;
            float r = _restored;
            float still = theme != null ? theme.stillWhenUnrestored * (1f - r) : 0f;
            float speed = theme != null ? theme.windSpin * r : 0f;
            _spin += speed * Time.deltaTime + SpinNudge;
            SpinNudge = 0f;
            if (_spin > 10000f) _spin -= 6283.1853f;
            Shader.SetGlobalFloat(StillId, still);
            Shader.SetGlobalFloat(SpinId, _spin);
        }

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
