using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>Step 9g checks through the Claude bridge (exec:Diorama.EditorTools.Step9gDebug.X). Shots in Logs/BattleShots/9g_*.</summary>
    public static class Step9gDebug
    {
        /// <summary>Presses the shown rune whose label starts with the text in Temp/ClaudeBridge/rune.txt.</summary>
        public static void Rune()
        {
            string label = System.IO.File.ReadAllText("Temp/ClaudeBridge/rune.txt").Trim();
            Debug.Log($"[Step9gDebug] Rune '{label}': {(RuneBoard.DebugPress(label) ? "pressed" : "not found")}.");
        }

        public static void Runes() => Debug.Log($"[Step9gDebug] Runes:\n{RuneBoard.DebugList()}");

        public static void State()
        {
            var e = Object.FindFirstObjectByType<GestureTestPiece>();
            Debug.Log($"[Step9gDebug] State: slot {SaveSlots.Active} ({SaveSlots.ActivePath}), adventure {SaveSlots.Exists(SaveSlot.Adventure)}, quick {SaveSlots.Exists(SaveSlot.Quick)}, " +
                      $"title {TitleScreen.Showing}, opening {Opening.InProgress}, paused {GamePause.IsPaused}, explorer {(e != null ? $"c{e.Tile.x} r{e.Tile.y} locked {e.Locked}" : "none")}, " +
                      $"timeScale {Time.timeScale}.\n{RuneBoard.DebugList()}");
        }

        /// <summary>From the seat, a little higher than 9f's shot so the logo and the runes fit: Logs/BattleShots/9g_{name}.png (name in shot.txt).</summary>
        public static void Shot()
        {
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            if (island == null) return;
            string name = System.IO.File.Exists("Temp/ClaudeBridge/shot.txt") ? System.IO.File.ReadAllText("Temp/ClaudeBridge/shot.txt").Trim() : "seat";
            var t = island.transform;
            var eye = t.position + t.rotation * new Vector3(0f, 0.42f, 0.5f);
            var look = t.position + t.rotation * new Vector3(0f, 0.09f, 0f);
            BattleDebug.Render($"Logs/BattleShots/9g_{name}.png", eye, Quaternion.LookRotation(look - eye, Vector3.up));
            Debug.Log($"[Step9gDebug] Shot 9g_{name}.png");
        }

        /// <summary>The baked logo alone, far from the island: front and three-quarter views.</summary>
        public static void LogoShot()
        {
            var recipe = PropKit.Load(PropKit.LogoName);
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Props/Logo.mat");
            if (recipe == null || recipe.bakedMesh == null || material == null)
            {
                Debug.LogError("[Step9gDebug] Logo not baked (Step9gSetup.TitleOnly).");
                return;
            }
            var go = new GameObject("Logo Shot") { hideFlags = HideFlags.DontSave };
            go.AddComponent<MeshFilter>().sharedMesh = recipe.bakedMesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            var block = new MaterialPropertyBlock();
            recipe.ApplyTo(block);
            block.SetFloat("_WindowClip", 0f);
            block.SetFloat("_RestoredSelf", 1f);
            r.SetPropertyBlock(block);
            var at = new Vector3(200f, 200f, 200f);
            go.transform.position = at;
            var c = at + recipe.bakedMesh.bounds.center;
            float d = recipe.bakedMesh.bounds.size.x * 1.15f;
            BattleDebug.Render("Logs/BattleShots/9g_logo_front.png", c + new Vector3(0f, 0.25f, 1f).normalized * d, Quaternion.LookRotation(-new Vector3(0f, 0.25f, 1f), Vector3.up));
            var dir = new Vector3(0.6f, 0.4f, 1f).normalized;
            BattleDebug.Render("Logs/BattleShots/9g_logo_side.png", c + dir * d, Quaternion.LookRotation(-dir, Vector3.up));
            Object.DestroyImmediate(go);
            Debug.Log($"[Step9gDebug] Logo shots: {recipe.bakedMesh.triangles.Length / 3} tris, size {recipe.bakedMesh.bounds.size}.");
        }
    }
}
