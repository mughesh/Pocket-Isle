using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>Step 22 bridge checks (Nov 8 notes): the menu's spacing in the room.</summary>
    public static class Step22Debug
    {
        static string Arg => System.IO.File.Exists("Temp/ClaudeBridge/arg.txt") ? System.IO.File.ReadAllText("Temp/ClaudeBridge/arg.txt").Trim() : "";

        /// <summary>
        /// Shoots the game menu's board from the seat (Logs/BattleShots/22_&lt;arg&gt;.png and a close one) and logs each
        /// visible tablet pair's gap in the room (cm), from the tablets' rects and the board's scale.
        /// </summary>
        /// <summary>
        /// Edit mode: the opening shard (arg "shard", default) or a creature prefab (arg = its name) far above the scene,
        /// shot from four sides and twice in time (Logs/BattleShots/22_crystal_&lt;name&gt;_N.png), to judge the deep cracks.
        /// </summary>
        public static void CrystalShots()
        {
            string name = string.IsNullOrEmpty(Arg) ? "shard" : Arg;
            var at = new Vector3(0f, 60f, 0f);
            GameObject go;
            float size;
            if (name == "shard")
            {
                var mk = typeof(Opening).GetMethod("ShardMesh", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                var mesh = mk != null ? (Mesh)mk.Invoke(null, null) : null;
                var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Props/OpeningShard.mat");
                if (mesh == null || mat == null)
                {
                    Debug.LogError("[Step22Debug] No shard mesh or material.");
                    return;
                }
                go = new GameObject("Step22 Shard");
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = mat;
                go.transform.localScale = Vector3.one * 0.12f;
                size = 0.14f;
            }
            else
            {
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Prefabs/Creatures/{name}.prefab");
                if (prefab == null)
                {
                    Debug.LogError($"[Step22Debug] No creature prefab {name}.");
                    return;
                }
                go = Object.Instantiate(prefab);
                var view = go.GetComponentInChildren<CreatureView>(true);
                if (view != null) view.SendMessage("Awake", SendMessageOptions.DontRequireReceiver);
                size = 0.16f;
            }
            go.transform.position = at;
            var b = new Bounds(at, Vector3.zero);
            foreach (var r in go.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
            var c = b.center;
            BattleDebug.Background = new Color(0.1f, 0.1f, 0.12f);
            try
            {
                for (int i = 0; i < 4; i++)
                {
                    var dir = Quaternion.Euler(12f, i * 90f + 20f, 0f) * Vector3.back;
                    var eye = c + dir * Mathf.Max(size * 2.2f, b.extents.magnitude * 2.6f);
                    BattleDebug.Render($"Logs/BattleShots/22_crystal_{name}_{i}.png", eye, Quaternion.LookRotation(c - eye, Vector3.up));
                }
            }
            finally
            {
                BattleDebug.Background = null;
                Object.DestroyImmediate(go);
            }
            Debug.Log($"[Step22Debug] Crystal shots of {name}: Logs/BattleShots/22_crystal_{name}_0..3.png.");
        }

        public static void MenuShot()
        {
            string name = string.IsNullOrEmpty(Arg) ? "menu" : Arg;
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            var menu = Object.FindFirstObjectByType<GameMenu>();
            if (island == null || menu == null || menu.runes == null) return;
            var t = island.transform;
            var eye = t.position + t.rotation * new Vector3(0f, 0.5f, 0.42f);
            var board = menu.runes.transform;
            if (Camera.main != null) Camera.main.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(board.position - eye, Vector3.up));
            BattleDebug.Render($"Logs/BattleShots/22_{name}.png", eye, Quaternion.LookRotation(t.position - eye, Vector3.up));
            var close = Vector3.Lerp(eye, board.position, 0.5f);
            BattleDebug.Render($"Logs/BattleShots/22_{name}_close.png", close, Quaternion.LookRotation(board.position - close, Vector3.up));
            float s = board.lossyScale.x;
            var p = menu.runes.LayoutProblems();
            Debug.Log($"[Step22Debug] {name}: board scale {s:0.00}, rule {menu.runes.MinGapLocal * 100f:0.0} cm local = {TabletLayout.MinRoomGap * 100f:0.0} cm in the room; " +
                      $"{(p.Count == 0 ? "ok" : string.Join("; ", p))}.\n{RuneBoard.DebugList()}");
        }
    }
}
