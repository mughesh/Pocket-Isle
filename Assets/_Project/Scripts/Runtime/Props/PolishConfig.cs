using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Overnight Nov 5: the switches for the additive visual polish (ambient life, battle feel, restoration waves), in
    /// Resources/PolishConfig.asset (Tools > Pocket Isle > Polish > Create Config). Every item is off when its switch is off;
    /// a missing asset logs an error once and turns all of them off (no silent defaults).
    /// </summary>
    [CreateAssetMenu(menuName = "Pocket Isle/Polish Config")]
    public class PolishConfig : ScriptableObject
    {
        public const string ResourceName = "PolishConfig";

        [Header("Ambient life (island-local, inside the window)")]
        public bool ambientLife = true;
        [Tooltip("Unlit instanced material for the winged critters (Diorama/Critter).")]
        public Material critterMaterial;
        [Tooltip("How many of each when the realm is faded, as a share of the restored count.")]
        [Range(0f, 1f)] public float fadedShare = 0.35f;
        public int hubButterflies = 3;
        public int cinderwoodFireflies = 14;
        public int lilymereDragonflies = 2;
        public Vector2 galewickBirdEvery = new Vector2(10f, 22f);
        public int lilymereSparkles = 10;

        [Header("Battle feel")]
        public bool battleFeel = true;
        [Tooltip("Hit-stop on a super-effective hit: the stage's tweens hold this long (s).")]
        public float hitStop = 0.04f;

        [Header("Restoration waves")]
        public bool restoreWave = true;
        [Tooltip("Lifted leaves / petals / sparkles per second at the wave front.")]
        public float waveLifts = 70f;

        static PolishConfig _cached;
        static bool _warned;

        /// <summary>The config (null, with one error, when the asset is missing: every polish item off).</summary>
        public static PolishConfig Get()
        {
            if (_cached != null) return _cached;
            _cached = Resources.Load<PolishConfig>(ResourceName);
            if (_cached == null && !_warned)
            {
                _warned = true;
                Debug.LogError("[Polish] No Resources/PolishConfig.asset (Tools > Pocket Isle > Polish > Create Config): ambient life, battle feel and restoration waves are off.");
            }
            return _cached;
        }

        public static bool AmbientOn => Get() is PolishConfig c && c.ambientLife;
        public static bool BattleFeelOn => Get() is PolishConfig c && c.battleFeel;
        public static bool RestoreWaveOn => Get() is PolishConfig c && c.restoreWave;
    }
}
