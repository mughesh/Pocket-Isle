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

        /// <summary>Seat view and a close view of the current hint target (gold halo, Spirit, ghost): Logs/BattleShots/hint_*.png.</summary>
        public static void TargetShot()
        {
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            if (island == null) return;
            var t = island.transform;
            var state = Dialogue.GetTarget(out var world);
            Vector3 key = state == Dialogue.TargetState.Active ? world : t.position;
            var seat = t.position + t.rotation * new Vector3(0f, 0.32f, 0.5f);
            BattleDebug.Render("Logs/BattleShots/hint_seat.png", seat, Quaternion.LookRotation(key - seat, Vector3.up));
            var near = key + t.rotation * new Vector3(0.04f, 0.07f, 0.2f);
            BattleDebug.Render("Logs/BattleShots/hint_near.png", near, Quaternion.LookRotation(key - near, Vector3.up));
            Debug.Log($"[Step8cDebug] Hint shots ({state}, target {key:F2}).");
        }

        public static void ChooseStarter()
        {
            var o = Object.FindFirstObjectByType<Opening>();
            if (o != null) o.DebugChoose(0);
        }
    }
}
