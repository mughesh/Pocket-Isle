using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 9f checks in flat Play (bridge: exec:Diorama.EditorTools.Step9fDebug.X): the explorer close-up and walk, a
    /// one-hit win for the XP display, the pod card. Shots go to Logs/BattleShots/9f_play_*.png.
    /// </summary>
    public static class Step9fDebug
    {
        static T Find<T>() where T : Object => Object.FindFirstObjectByType<T>();

        /// <summary>Seated view of the island.</summary>
        public static void Seat(string name)
        {
            var island = Find<IslandBuilder>();
            if (island == null) return;
            var t = island.transform;
            var eye = t.position + t.rotation * new Vector3(0f, 0.5f, 0.42f);
            BattleDebug.Render($"Logs/BattleShots/9f_play_{name}.png", eye, Quaternion.LookRotation(t.position + t.rotation * new Vector3(0f, 0f, 0.02f) - eye, Vector3.up));
        }

        public static void ShotSeat() => Seat("seat");
        public static void ShotXp() => Seat("xp");
        public static void ShotXp2() => Seat("xp2");

        /// <summary>The explorer from 45 degrees above, close.</summary>
        public static void ShotExplorer() => Close("explorer", 45f);
        public static void ShotExplorerLow() => Close("explorer_low", 20f);

        static void Close(string name, float pitch)
        {
            var island = Find<IslandBuilder>();
            var e = GameObject.Find("Explorer");
            if (island == null || e == null)
            {
                Debug.Log("[Step9fDebug] No explorer.");
                return;
            }
            var t = island.transform;
            Vector3 c = e.transform.position + t.up * 0.012f * t.lossyScale.x;
            float d = 0.09f * t.lossyScale.x;
            var eye = c + t.rotation * (Quaternion.Euler(-pitch, 0f, 0f) * new Vector3(0f, 0f, d));
            BattleDebug.Render($"Logs/BattleShots/9f_play_{name}.png", eye, Quaternion.LookRotation(c - eye, t.up));
        }

        /// <summary>Walks the explorer a few tiles along a free row (GoTo), to see the walk.</summary>
        public static void Walk()
        {
            var e = Find<GestureTestPiece>();
            if (e == null) return;
            foreach (var d in new[] { new Vector2Int(4, 0), new Vector2Int(-4, 0), new Vector2Int(0, 4), new Vector2Int(0, -4) })
                if (e.CanReach(e.Tile + d))
                {
                    e.GoTo(e.Tile + d);
                    Debug.Log($"[Step9fDebug] Walking to c{e.Tile.x + d.x} r{e.Tile.y + d.y}.");
                    return;
                }
            Debug.Log("[Step9fDebug] No free tile 4 away.");
        }

        /// <summary>Close shot of the party creature on its pad (the XP display), from the seated side.</summary>
        public static void ShotPad1() => Pad("pad1");
        public static void ShotPad2() => Pad("pad2");
        public static void ShotPad3() => Pad("pad3");

        static void Pad(string name)
        {
            var stage = Find<BattleStage>();
            var island = Find<IslandBuilder>();
            if (stage == null || stage.Player == null || island == null)
            {
                Debug.Log("[Step9fDebug] No party creature on the stage.");
                return;
            }
            var t = island.transform;
            Vector3 c = stage.Player.transform.position + t.up * 0.05f;
            var eye = c + t.rotation * new Vector3(0f, 0.12f, 0.26f);
            BattleDebug.Render($"Logs/BattleShots/9f_play_{name}.png", eye, Quaternion.LookRotation(c - eye, Vector3.up));
        }

        public static void Cheer()
        {
            var e = Find<GestureTestPiece>();
            if (e != null) e.Cheer();
        }

        /// <summary>One-hit KO on, a wild grass battle starts (then BattleDebug.SwitchNext sends the lead, Move1 wins).</summary>
        public static void Fight()
        {
            DevCheats.OneHitKo = true;
            BattleDebug.Grass();
        }

        /// <summary>Opens the lead pod's stats card (as a lodge poke would) and shoots it close.</summary>
        public static void Card()
        {
            var rack = Find<PodRack>();
            if (rack == null) return;
            var m = typeof(PodRack).GetMethod("ShowCard", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            m.Invoke(rack, new object[] { 0, "debug" });
            var card = GameObject.Find("Pod Card");
            if (card == null) return;
            // Head-on: the card faces away from the main camera, so look along its forward.
            var eye = card.transform.position - card.transform.forward * 0.12f;
            BattleDebug.Render("Logs/BattleShots/9f_play_card.png", eye, card.transform.rotation);
        }
    }
}
