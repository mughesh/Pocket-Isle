using System.IO;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>Step 12b checks through the Claude bridge (exec:Diorama.EditorTools.Step12bDebug.X), in Play.</summary>
    static class Step12bDebug
    {
        const string ArgFile = "Temp/ClaudeBridge/arg.txt";
        static string Arg => File.Exists(ArgFile) ? File.ReadAllText(ArgFile).Trim() : "";

        /// <summary>The cloud bank's height (arg, tiles) live, and where its puffs are drawn.</summary>
        public static void CloudHeight()
        {
            var b = Object.FindFirstObjectByType<CloudBankPiece>();
            if (b == null) return;
            if (float.TryParse(Arg, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float h)) b.puffHeight = h;
            var r = GameObject.Find("Cloud Bank 0");
            var mr = r != null ? r.GetComponent<MeshRenderer>() : null;
            Debug.Log($"[Step12bDebug] cloud height {b.puffHeight}; view 0 {(r != null ? $"at {r.transform.localPosition}, enabled {(mr != null && mr.enabled)}, active {r.activeInHierarchy}" : "not found")}.");
        }
    }
}
