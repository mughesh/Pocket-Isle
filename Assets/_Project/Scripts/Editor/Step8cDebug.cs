using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>Step 8c checks through the Claude bridge (exec:Diorama.EditorTools.Step8cDebug.Method), Play mode.</summary>
    static class Step8cDebug
    {
        /// <summary>A fingertip contact on the waiting shard (nudge, flash, crack).</summary>
        public static void PokeShard()
        {
            var o = Object.FindFirstObjectByType<Opening>();
            if (o != null) o.DebugPoke();
        }

        public static void ChooseStarter()
        {
            var o = Object.FindFirstObjectByType<Opening>();
            if (o != null) o.DebugChoose(0);
        }
    }
}
