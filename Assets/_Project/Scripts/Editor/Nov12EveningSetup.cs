using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Nov 12 evening notes (Mugesh), the scene and map side:
    /// the plaza's small mill toy (it still drew at its old tile on the paved plaza after the overnight edit moved the prop)
    /// goes beside the lower cottage; the ridge's three cloud puffs are spread along the road (side by side they read as
    /// one cloud); the statue sockets take a grab from 9 cm (was 7). Tools > Pocket Isle > Fixes > Nov 12 Evening (or the bridge).
    /// </summary>
    public static class Nov12EveningSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";
        const string MapPath = "Assets/_Project/Data/WorldMap_Galewick.asset";

        /// <summary>The small mill toy's new tile (props grid column, row): beside the lower cottage, off the plaza.</summary>
        public static readonly Vector2Int MillFrom = new Vector2Int(34, 38), MillTo = new Vector2Int(28, 42);

        /// <summary>The ridge puffs (props grid column, row 12) and their twins (row 11): three apart instead of side by side.</summary>
        public static readonly int[] PuffColumns = { 21, 24, 27 };
        public const int PuffRow = 12, PuffTwinRow = 11;
        static readonly int[] OldPuffColumns = { 21, 22, 23 };

        [MenuItem("Tools/Pocket Isle/Fixes/Nov 12 Evening")]
        public static void Apply()
        {
            var log = new StringBuilder("[Nov12Evening]");
            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Nov12Evening] Stop Play mode first.");
                return;
            }

            // ---- the map's props grid ----
            var map = AssetDatabase.LoadAssetAtPath<WorldMap>(MapPath);
            if (map == null)
            {
                Debug.LogError($"[Nov12Evening] No map at {MapPath}.");
                return;
            }
            var rows = map.props.Replace("\r", "").Split('\n');
            int RowIndex(int row)
            {
                int n = -1;
                for (int i = 0; i < rows.Length; i++)
                    if (rows[i].Trim().Length > 0 && ++n == row) return i;
                return -1;
            }
            char Get(Vector2Int t) => rows[RowIndex(t.y)][t.x];
            void Set(Vector2Int t, char c)
            {
                int i = RowIndex(t.y);
                var a = rows[i].ToCharArray();
                a[t.x] = c;
                rows[i] = new string(a);
            }

            if (Get(MillFrom) == 'm')
            {
                if (Get(MillTo) != '.') Debug.LogError($"[Nov12Evening] c{MillTo.x} r{MillTo.y} isn't free ('{Get(MillTo)}').");
                else
                {
                    Set(MillFrom, '.');
                    Set(MillTo, 'm');
                    log.Append($"\n  Small mill prop c{MillFrom.x} r{MillFrom.y} -> c{MillTo.x} r{MillTo.y} (beside the lower cottage).");
                }
            }
            else log.Append($"\n  Small mill prop already moved ('{Get(MillFrom)}' at c{MillFrom.x} r{MillFrom.y}, '{Get(MillTo)}' at c{MillTo.x} r{MillTo.y}).");

            foreach (int c in OldPuffColumns)
            {
                if (Get(new Vector2Int(c, PuffRow)) == 'C') Set(new Vector2Int(c, PuffRow), '.');
                if (Get(new Vector2Int(c, PuffTwinRow)) == 'C') Set(new Vector2Int(c, PuffTwinRow), '.');
            }
            foreach (int c in PuffColumns)
            {
                Set(new Vector2Int(c, PuffRow), 'C');
                Set(new Vector2Int(c, PuffTwinRow), 'C');
            }
            log.Append($"\n  Ridge puffs on the props grid: c{string.Join(", c", PuffColumns)} (rows {PuffTwinRow}-{PuffRow}).");
            Undo.RecordObject(map, "Nov 12 evening map");
            map.SetProps(string.Join("\n", rows));
            EditorUtility.SetDirty(map);
            AssetDatabase.SaveAssetIfDirty(map);

            // ---- the scene ----
            int mills = 0, banks = 0, sockets = 0;
            foreach (var w in Object.FindObjectsByType<WheelPiece>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (w.id != "gw.smallmill.1") continue;
                Undo.RecordObject(w, "Nov 12 evening mill");
                w.anchor = MillTo;
                EditorUtility.SetDirty(w);
                mills++;
            }
            foreach (var b in Object.FindObjectsByType<CloudBankPiece>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (b.id != "gw.clouds") continue;
                Undo.RecordObject(b, "Nov 12 evening clouds");
                b.puffs = new Vector2Int[PuffColumns.Length];
                b.puffTwins = new Vector2Int[PuffColumns.Length];
                for (int i = 0; i < PuffColumns.Length; i++)
                {
                    b.puffs[i] = new Vector2Int(PuffColumns[i], PuffRow);
                    b.puffTwins[i] = new Vector2Int(PuffColumns[i], PuffTwinRow);
                }
                EditorUtility.SetDirty(b);
                banks++;
            }
            foreach (var s in Object.FindObjectsByType<StatueSocket>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Undo.RecordObject(s, "Nov 12 evening socket");
                s.grabRadius = Mathf.Max(s.grabRadius, 0.09f);
                EditorUtility.SetDirty(s);
                sockets++;
            }
            foreach (var d in Object.FindObjectsByType<DialogueBubble>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Undo.RecordObject(d, "Nov 12 evening Heart call");
                d.heartCallLayer = 1f;
                EditorUtility.SetDirty(d);
                log.Append("\n  The Heart's bowl and choir call at full volume (was 0.6).");
            }
            log.Append($"\n  Scene: small mill toy 1 anchor c{MillTo.x} r{MillTo.y} ({mills}); ridge cloud bank ({banks}); statue sockets grab 9 cm ({sockets}).");
            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            Debug.Log(log.ToString());
        }
    }
}
