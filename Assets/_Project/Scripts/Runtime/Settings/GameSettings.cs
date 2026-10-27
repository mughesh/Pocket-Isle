using System;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Player settings kept in PlayerPrefs across launches and both save slots (step 9g): which hand is dominant
    /// (Auto = whichever hand first touches the shard) and how chatty the Spirit is (Fewer = no idle hints; lessons still run).
    /// </summary>
    public static class GameSettings
    {
        public enum Hand { Auto = 0, Left = 1, Right = 2 }

        const string HandKey = "pocketisle.hand";
        const string HintsKey = "pocketisle.fewerHints";
        const string TouchWalkKey = "pocketisle.touchWalk";

        public static event Action Changed;

        public static Hand DominantHand
        {
            get
            {
                int v = PlayerPrefs.GetInt(HandKey, 0);
                return v >= 0 && v <= 2 ? (Hand)v : Hand.Auto;
            }
            set
            {
                PlayerPrefs.SetInt(HandKey, (int)value);
                PlayerPrefs.Save();
                Debug.Log($"[GameSettings] Dominant hand: {value}.");
                Changed?.Invoke();
            }
        }

        /// <summary>
        /// Step 11d (Note 1): touching a tile with a fingertip walks the explorer there. Off by default: it fought the
        /// hand moments on the island. Walking stays with the thumb swipe and look + pinch.
        /// </summary>
        public static bool TouchWalk
        {
            get => PlayerPrefs.GetInt(TouchWalkKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(TouchWalkKey, value ? 1 : 0);
                PlayerPrefs.Save();
                Debug.Log($"[GameSettings] Touch tiles to walk: {(value ? "on" : "off")}.");
                Changed?.Invoke();
            }
        }

        /// <summary>Fewer = no idle hints from the Spirit (lessons and story lines still play).</summary>
        public static bool FewerHints
        {
            get => PlayerPrefs.GetInt(HintsKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(HintsKey, value ? 1 : 0);
                PlayerPrefs.Save();
                Debug.Log($"[GameSettings] Spirit hints: {(value ? "fewer" : "on")}.");
                Changed?.Invoke();
            }
        }
    }

    /// <summary>
    /// Volume per audio bus, 0-4 (step 9g settings: five pebbles, 0 = off), saved in PlayerPrefs under the bus names the
    /// audio lane uses: <c>Music</c>, <c>SFX</c>, <c>Ambience</c>. <see cref="GameAudio"/> maps them onto AudioService's
    /// buses (UI and Voice follow Effects; the music sits under the effects) and listens to <see cref="Changed"/>. Not
    /// called AudioSettings: that would hide UnityEngine.AudioSettings (dspTime) from every script in this namespace.
    /// </summary>
    public static class VolumeSettings
    {
        public const string Music = "Music", Sfx = "SFX", Ambience = "Ambience";
        public const int MaxLevel = 4;
        public const int DefaultLevel = 3;

        /// <summary>A bus's level changed (the bus name).</summary>
        public static event Action<string> Changed;

        public static int Level(string bus) => Mathf.Clamp(PlayerPrefs.GetInt(bus, DefaultLevel), 0, MaxLevel);

        public static void SetLevel(string bus, int level)
        {
            level = Mathf.Clamp(level, 0, MaxLevel);
            PlayerPrefs.SetInt(bus, level);
            PlayerPrefs.Save();
            Debug.Log($"[VolumeSettings] {bus} {level}/{MaxLevel}.");
            Changed?.Invoke(bus);
        }

        /// <summary>0..1 for an AudioSource or mixer (level / 4).</summary>
        public static float Volume(string bus) => Level(bus) / (float)MaxLevel;
    }
}
