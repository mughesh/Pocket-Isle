using System.IO;
using System.Reflection;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>Bridge hooks for the Nov 7 headset notes (Play mode): close ghost shots, shard blast, finale layout.</summary>
    public static class Step18Debug
    {
        const string ArgFile = "Temp/ClaudeBridge/arg.txt";
        static string Arg => File.Exists(ArgFile) ? File.ReadAllText(ArgFile).Trim() : "";

        static IslandBuilder Island => Object.FindFirstObjectByType<IslandBuilder>();

        /// <summary>
        /// Close shots of the ghost (arg = "name"): from the seated head 40 cm out toward the ghost's key point
        /// (Logs/BattleShots/name.png) and from the side (name_side.png). Falls back to the island centre.
        /// </summary>
        public static void GhostClose()
        {
            var island = Island;
            var g = Object.FindFirstObjectByType<GhostHands>();
            if (island == null) return;
            string name = string.IsNullOrEmpty(Arg) ? "18_ghost" : Arg;
            var t = island.transform;
            Vector3 head = t.position + t.rotation * new Vector3(0f, 0.55f, 0.45f);
            Vector3 at = t.TransformPoint(new Vector3(0f, island.CapTopLocal, 0f));
            if (g != null)
                foreach (var r in g.GetComponentsInChildren<Renderer>())
                    if (r.enabled && r is SkinnedMeshRenderer)
                    {
                        at = r.bounds.center;
                        break;
                    }
            Vector3 eye = at + (head - at).normalized * 0.4f;
            BattleDebug.Render($"Logs/BattleShots/{name}.png", eye, Quaternion.LookRotation(at - eye, Vector3.up));
            Vector3 side = at + (t.rotation * new Vector3(-0.35f, 0.12f, 0.05f));
            BattleDebug.Render($"Logs/BattleShots/{name}_side.png", side, Quaternion.LookRotation(at - side, Vector3.up));
            Debug.Log($"[Step18Debug] Ghost close shots {name} at {at}.");
        }

        /// <summary>The Keeper-shard blast at the island centre (arg = shard id, e.g. shard.wind).</summary>
        public static void Blast()
        {
            var island = Island;
            var fx = Object.FindFirstObjectByType<BattleFx>();
            if (island == null || fx == null) return;
            var t = island.transform;
            Vector3 at = t.TransformPoint(new Vector3(0f, island.CapTopLocal + 0.15f, 0.1f));
            ShardBurst.Blast(fx, island, at, string.IsNullOrEmpty(Arg) ? "shard.wind" : Arg);
            Debug.Log($"[Step18Debug] Blast {Arg}.");
        }

        /// <summary>A seated shot of the whole island (arg = name), wider than Step16Debug.Shot.</summary>
        public static void Wide()
        {
            var island = Island;
            if (island == null) return;
            var t = island.transform;
            var eye = t.position + t.rotation * new Vector3(0f, 0.7f, 0.85f);
            var look = t.TransformPoint(new Vector3(0f, island.CapTopLocal + 0.1f, 0f));
            BattleDebug.Render($"Logs/BattleShots/{(string.IsNullOrEmpty(Arg) ? "18_wide" : Arg)}.png", eye, Quaternion.LookRotation(look - eye, Vector3.up));
        }

        /// <summary>
        /// A close shot of a map spot (arg "name,column,row,height tiles,distance m"): from the seated head's side, the given
        /// distance from the spot (Logs/BattleShots/name.png).
        /// </summary>
        public static void Look()
        {
            var island = Island;
            if (island == null) return;
            var a = Arg.Split(',');
            if (a.Length < 5) return;
            float F(int i) => float.Parse(a[i], System.Globalization.CultureInfo.InvariantCulture);
            var t = island.transform;
            Vector3 at = t.TransformPoint(island.TileToLocal(new Vector2(F(1), F(2)), island.Content.capHeight + F(3) * island.map.tileSize));
            Vector3 head = t.position + t.rotation * new Vector3(0f, 0.55f, 0.6f);
            Vector3 eye = at + (head - at).normalized * F(4);
            BattleDebug.Render($"Logs/BattleShots/{a[0]}.png", eye, Quaternion.LookRotation(at - eye, Vector3.up));
            Debug.Log($"[Step18Debug] Look {a[0]} at c{a[1]} r{a[2]}.");
        }

        /// <summary>Where the explorer and the statue's front are (log).</summary>
        public static void Where()
        {
            var e = Object.FindFirstObjectByType<GestureTestPiece>();
            var s = StatueSocket.FindById(ShardHolder.WindId);
            Debug.Log($"[Step18Debug] Explorer c{(e != null ? e.Tile.x : -1)} r{(e != null ? e.Tile.y : -1)}, locked {(e != null && e.Locked)}; statue front {(s != null ? s.FrontTile()?.ToString() : "-")}.");
        }

        /// <summary>Edit mode: wires BattleRunes.pods to the scene's PodRack and saves the scene.</summary>
        public static void WireRunes()
        {
            if (UnityEditor.EditorApplication.isPlaying) return;
            var runes = Object.FindFirstObjectByType<BattleRunes>(FindObjectsInactive.Include);
            var pods = Object.FindFirstObjectByType<PodRack>(FindObjectsInactive.Include);
            if (runes == null || pods == null) return;
            UnityEditor.Undo.RecordObject(runes, "Wire rune pods");
            runes.pods = pods;
            UnityEditor.EditorUtility.SetDirty(runes);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(runes.gameObject.scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(runes.gameObject.scene);
            Debug.Log($"[Step18Debug] BattleRunes.pods = {pods.name}; scene saved.");
        }

        /// <summary>Edit mode: rewrites and bakes the wind tower props (Nov 7: smoother, bigger door).</summary>
        public static void BakeTower()
        {
            if (UnityEditor.EditorApplication.isPlaying) return;
            var sb = new System.Text.StringBuilder("[Step18Debug] Wind tower baked:");
            var list = PropKit.WriteWindTowers();
            UnityEditor.AssetDatabase.SaveAssets();
            foreach (var r in list)
            {
                var rep = PropBaker.Bake(r);
                sb.Append($" {r.name} {rep.triangles} tris (budget {r.triangleBudget});");
            }
            UnityEditor.AssetDatabase.SaveAssets();
            IslandBuilder.RebuildAll();
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Edit mode, Nov 8 notes: the scene's ghost values (aside offset 0: the evolve ghost grabbed beside the pod; thumb
        /// demos the pinch ghost's size; the pinch ghost further in over the island) and the statue's palm-lift ghost.
        /// Saves the scene.
        /// </summary>
        public static void Nov8Scene()
        {
            if (UnityEditor.EditorApplication.isPlaying) return;
            var g = Object.FindFirstObjectByType<GhostHands>(FindObjectsInactive.Include);
            var sb = new System.Text.StringBuilder("[Step18Debug] Nov 8 scene:");
            if (g != null)
            {
                UnityEditor.Undo.RecordObject(g, "Nov 8 ghosts");
                sb.Append($" asideOffset {g.asideOffset} -> 0, thumbScale {g.thumbScale} -> 1, aboveIslandFront {g.aboveIslandFront} -> 0.25;");
                g.asideOffset = 0f;
                g.thumbScale = 1f;
                g.aboveIslandFront = 0.25f;
                g.thumbYaw = 55f;
                g.thumbRoll = 0f;
                if (!string.IsNullOrEmpty(Arg) && float.TryParse(Arg, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float roll))
                {
                    g.thumbRoll = roll;
                    sb.Append($" thumbRoll {roll};");
                }
                UnityEditor.EditorUtility.SetDirty(g);
            }
            foreach (var s in Object.FindObjectsByType<StatueSocket>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                UnityEditor.Undo.RecordObject(s, "Nov 8 statue ghost");
                sb.Append($" {s.id} ghost {s.ghostClip} -> {FinaleDirector.LiftClip};");
                s.ghostClip = FinaleDirector.LiftClip;
                UnityEditor.EditorUtility.SetDirty(s);
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Edit mode, step 18 (Mugesh: no palm ghost for the statue, the shard is pinched or grabbed and placed): the statue
        /// sockets' ghost back to grab_drop (or the clip in arg). Saves the scene.
        /// </summary>
        public static void StatueGhost()
        {
            if (UnityEditor.EditorApplication.isPlaying) return;
            string clip = string.IsNullOrEmpty(Arg) ? "grab_drop" : Arg.Trim();
            var sb = new System.Text.StringBuilder("[Step18Debug] Statue ghost:");
            foreach (var s in Object.FindObjectsByType<StatueSocket>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                UnityEditor.Undo.RecordObject(s, "Statue ghost");
                sb.Append($" {s.id} {s.ghostClip} -> {clip};");
                s.ghostClip = clip;
                UnityEditor.EditorUtility.SetDirty(s);
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log(sb.ToString());
        }

        /// <summary>Play mode: sets a float field on the first object of a type (arg = "Type.field=value").</summary>
        public static void SetFloat()
        {
            var eq = Arg.Split('=');
            var a = eq[0].Split('.');
            if (eq.Length != 2 || a.Length != 2) return;
            var type = typeof(IslandBuilder).Assembly.GetType("Diorama." + a[0]);
            var o = type != null ? Object.FindFirstObjectByType(type) : null;
            var f = type?.GetField(a[1], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (o == null || f == null) return;
            f.SetValue(o, float.Parse(eq[1], System.Globalization.CultureInfo.InvariantCulture));
            Debug.Log($"[Step18Debug] {Arg}.");
        }

        /// <summary>Calls a private parameterless method on the first object of a type (arg = "Type.Method").</summary>
        public static void Call()
        {
            var a = Arg.Split('.');
            if (a.Length != 2) return;
            var type = typeof(IslandBuilder).Assembly.GetType("Diorama." + a[0]);
            var o = type != null ? Object.FindFirstObjectByType(type) : null;
            var m = type?.GetMethod(a[1], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (o == null || m == null)
            {
                Debug.LogWarning($"[Step18Debug] No {Arg}.");
                return;
            }
            var r = m.Invoke(o, null);
            if (r is System.Collections.IEnumerator e) ((MonoBehaviour)o).StartCoroutine(e);
            Debug.Log($"[Step18Debug] Called {Arg}.");
        }
    }
}
