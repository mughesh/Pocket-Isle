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

        /// <summary>Lodge: aims the gaze at the first room Isling (the next frames) and logs what the room sees.</summary>
        public static void AimLodgeIsling()
        {
            var room = Object.FindFirstObjectByType<LodgeRoom>();
            var li = room != null ? room.First : null;
            if (li == null)
            {
                Debug.Log("[Step8cDebug] No room Isling.");
                return;
            }
            HeadGaze.DebugAim = () => li != null ? li.island.transform.TransformPoint(li.LocalCenter) : (Vector3?)null;
            Debug.Log($"[Step8cDebug] Gaze on {li.Isling.Name}.");
        }

        /// <summary>Lodge: a pinch as the room handles it (look + pinch), then the gaze goes back to normal.</summary>
        public static void LodgePinch()
        {
            var room = Object.FindFirstObjectByType<LodgeRoom>();
            if (room == null) return;
            var f = typeof(LodgeRoom).GetField("_gazed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var gazed = f.GetValue(room) as LodgeIsling;
            typeof(LodgeRoom).GetMethod("Pinch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(room, null);
            Debug.Log($"[Step8cDebug] Lodge pinch: gazed {(gazed != null ? gazed.Isling.Name : "nothing")}.");
            HeadGaze.DebugAim = null;
        }

        /// <summary>Battle: the lead pod is sent out as if thrown.</summary>
        public static void SendLead()
        {
            var d = Object.FindFirstObjectByType<EncounterDirector>();
            var party = Object.FindFirstObjectByType<Party>();
            if (d != null && party != null) d.DebugPick(party.LeadSlot);
        }

        /// <summary>The two-hands tip's ghost, held as a preview (lessons and the hands check leave it alone).</summary>
        public static void GhostTwoHands()
        {
            var g = Object.FindFirstObjectByType<GhostHands>();
            if (g == null) return;
            g.previewing = true;
            g.Show("grab_rim_slide", GhostPlace.AtTarget, "islandRim", 1f, false, true, 0.3f);
        }

        public static void ChooseStarter()
        {
            var o = Object.FindFirstObjectByType<Opening>();
            if (o != null) o.DebugChoose(0);
        }
    }
}
