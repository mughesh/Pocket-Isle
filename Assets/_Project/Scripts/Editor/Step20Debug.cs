using System.IO;
using Diorama.Audio;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>Step 20 bridge (Play mode): the music checks (Continue past the title, the realm music switch, a readout).</summary>
    public static class Step20Debug
    {
        const string ArgFile = "Temp/ClaudeBridge/arg.txt";
        static string Arg => File.Exists(ArgFile) ? File.ReadAllText(ArgFile).Trim() : "";

        /// <summary>Presses Continue on the title (as the player).</summary>
        public static void Continue() => Debug.Log($"[Step20Debug] Continue: {RuneBoard.DebugPress("Continue")}.");

        /// <summary>arg "always" / "reward": the realm music switch (Dev panel > Tools > Realm music).</summary>
        public static void RealmMusic()
        {
            MusicDirector.RealmMusicBeforeRestore = Arg == "always";
            Log();
        }

        /// <summary>What plays now.</summary>
        public static void Log() =>
            Debug.Log($"[Step20Debug] Music state {MusicDirector.State}, cue {AudioService.CurrentMusic ?? "-"} " +
                      $"(sounding {AudioService.IsSounding(AudioService.CurrentMusic)}), area {AudioDirector.Area}, restored {RealmState.Restored:0.00}.");
    }
}
