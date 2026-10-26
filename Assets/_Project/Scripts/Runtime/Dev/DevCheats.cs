using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Test switches the dev panel flips (step 9e). All off by default and never saved; the game reads them at a few
    /// hooks (battle damage, wild encounters, rare weights, gaze picks). Inert in a release build (nothing sets them).
    /// </summary>
    public static class DevCheats
    {
        /// <summary>Every hit on the wild creature (or a Keeper, shield or not) knocks it out.</summary>
        public static bool OneHitKo;
        /// <summary>The player's creatures take no damage.</summary>
        public static bool Invincible;
        /// <summary>Tall grass never starts a wild battle (scripted ones still happen).</summary>
        public static bool WildOff;
        /// <summary>Rare entries' weight multiplier (1 = normal, 10 = the panel's "rare x10").</summary>
        public static float RareBoost = 1f;
        /// <summary>The head gaze is on the dev panel this frame: tile and bubble picks leave the pinch alone.</summary>
        public static bool PanelGazed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            OneHitKo = Invincible = WildOff = PanelGazed = false;
            RareBoost = 1f;
        }
    }
}
