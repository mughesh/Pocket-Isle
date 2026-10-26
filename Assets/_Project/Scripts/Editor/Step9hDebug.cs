using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>Step 9h bridge helpers (exec:Diorama.EditorTools.Step9hDebug.X). Shots go to Logs/BattleShots/9h_*.png.</summary>
    public static class Step9hDebug
    {
        /// <summary>A pinch for the pinch lesson (no hands in flat Play).</summary>
        public static void Pinch()
        {
            var o = Object.FindFirstObjectByType<Onboarding>();
            if (o != null) o.DebugPinch();
        }

        public static void Chip() => SetChip(true);
        public static void Full() => SetChip(false);

        static void SetChip(bool chip)
        {
            var b = Object.FindFirstObjectByType<DialogueBubble>();
            if (b != null) b.DebugChip(chip);
        }

        /// <summary>Lesson, bubble and Spirit state in one line.</summary>
        public static void State()
        {
            var o = Object.FindFirstObjectByType<Onboarding>();
            var b = Object.FindFirstObjectByType<DialogueBubble>();
            var g = Object.FindFirstObjectByType<GhostHands>();
            var d = Object.FindFirstObjectByType<Dialogue>();
            Debug.Log($"[Step9hDebug] State: lesson {(o != null ? o.CurrentId ?? "-" : "none")} ({(o != null && o.Runner != null ? o.Runner.State.ToString() : "-")}), " +
                      $"opening {Opening.InProgress}, line {(d != null ? d.CurrentTrigger ?? "-" : "-")}, pinned {Dialogue.IsPinned}, " +
                      $"bubble '{(b != null ? b.Text : "-")}' chip {(b != null && b.IsChip)}, ghost {(g != null ? g.ClipName ?? "-" : "-")} at {(g != null && g.KeyPoint.HasValue ? g.KeyPoint.Value.ToString("F3") : "-")}.");
        }

        /// <summary>From the seat (name in Temp/ClaudeBridge/shot.txt): Logs/BattleShots/9h_{name}.png.</summary>
        public static void Shot()
        {
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            if (island == null) return;
            string name = System.IO.File.Exists("Temp/ClaudeBridge/shot.txt") ? System.IO.File.ReadAllText("Temp/ClaudeBridge/shot.txt").Trim() : "seat";
            var t = island.transform;
            var eye = t.position + t.rotation * new Vector3(0f, 0.42f, 0.5f);
            var rot = Quaternion.LookRotation(t.position + t.rotation * new Vector3(0f, 0.09f, 0f) - eye, Vector3.up);
            var g = Object.FindFirstObjectByType<GhostHands>();
            if (name.StartsWith("ghost_") && g != null && g.KeyPoint.HasValue)
            {
                // Close on the ghost's key point, from the player's side, with the island behind it.
                var key = g.KeyPoint.Value;
                var from = key + (eye - key).normalized * 0.32f + Vector3.up * 0.04f;
                eye = from;
                rot = Quaternion.LookRotation(Vector3.Lerp(key, t.position, 0.25f) - from, Vector3.up);
            }
            BattleDebug.Render($"Logs/BattleShots/9h_{name}.png", eye, rot);
            Debug.Log($"[Step9hDebug] Shot 9h_{name}.png");
        }
    }
}
