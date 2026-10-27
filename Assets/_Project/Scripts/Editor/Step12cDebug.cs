using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>Step 12c checks through the Claude bridge (exec:Diorama.EditorTools.Step12cDebug.X). Shots in Logs/BattleShots/12c_*.</summary>
    static class Step12cDebug
    {
        const string ArgFile = "Temp/ClaudeBridge/arg.txt";
        static string Arg => System.IO.File.Exists(ArgFile) ? System.IO.File.ReadAllText(ArgFile).Trim() : "";

        /// <summary>A shot from the head, looking between the island and the open dev panel, to Logs/BattleShots/{arg}.png.</summary>
        public static void ShotPanel()
        {
            var cam = Camera.main;
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            var panel = GameObject.Find("Dev Panel UI");
            if (cam == null || island == null || panel == null)
            {
                Debug.LogWarning("[Step12cDebug] Needs the camera, the island and the open dev panel.");
                return;
            }
            Vector3 eye = cam.transform.position, look = (island.transform.position + panel.transform.position) * 0.5f;
            string name = string.IsNullOrEmpty(Arg) ? "12c_dev_panel" : Arg;
            BattleDebug.Render($"Logs/BattleShots/{name}.png", eye, Quaternion.LookRotation(look - eye, Vector3.up));
            Vector3 toPanel = panel.transform.position - eye, toIsland = island.transform.position - eye;
            Debug.Log($"[Step12cDebug] Shot {name}.png; panel {Vector3.Angle(toPanel, toIsland):0} deg from the island, {toPanel.magnitude:0.00} m away.");
        }

        /// <summary>Presses the dev panel button whose label starts with arg (tabs, the X, Up / Down).</summary>
        public static void Press() => Debug.Log($"[Step12cDebug] Press '{Arg}': {(DevPanel.DebugPress(Arg) ? "ok" : "not found")}.");
    }
}
