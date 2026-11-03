using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 14e (Mugesh's Nov 3 late notes): Cinderwood's arrival is a thorny vine across the trail, burnt with the plaza's
    /// ember (bakes ThornVine, rewrites the Cinderwood map: the vine and trees either side, the 14c lanterns gone; "Arrival
    /// Vine", a VineBurnPiece, replaces "Arrival Lanterns"; the smoky hollow gets its touch lesson back). The script is
    /// reimported (the Quick Journey's end lines, the vine's lines). Code-only parts need no setup: the Quick Journey's
    /// end runes over the statue and its dream ahead, the Spirit's typing voice, the Galewick ridge clouds back in the
    /// quick slot. Safe to re-run.
    /// </summary>
    public static class Step14eSetup
    {
        const string NL = "\n";
        public const string VineId = "cw.arrive";

        [MenuItem("Tools/Pocket Isle/Setup/First Minute (14)/Set Up MR_Main (14e Nov 3 late notes)", priority = 1004)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step14e] Stop Play mode first.");
                return;
            }
            var log = new StringBuilder("[Step14e] MR_Main set up:");
            DialogueTools.Import();
            log.Append(NL + "  Script_v1 reimported (quick.end asks, quick.ending.go, cw.vine lines)");
            log.Append(NL + $"  {PropBaker.Bake(PropKit.WriteThornVine())}");
            var map = CinderwoodTools.Write(log);
            Vine(map, log);
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }

        /// <summary>Bridge: the baked vine beside the ember, front on (a preview scene: no Play needed).</summary>
        public static void VineShot()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(Step14cSetup.LogoMaterialPath);
            var path = $"{Step9cSetup.ShotDir}/14e_vine.png";
            Step14cSetup.Shot(path, material, new[] { PropKit.Load(PropKit.VineName), PropKit.Load(PropKit.VineName) }, 2, 1400, 700);
            Debug.Log($"[Step14e] Vine shot: {path}.");
        }

        static void Vine(WorldMap map, StringBuilder log)
        {
            ForgeEmberPiece forge = null;
            foreach (var e in Object.FindObjectsByType<ForgeEmberPiece>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (e.id == Step10fSetup.EmberId) forge = e;
            if (forge == null || map == null)
            {
                log.Append(NL + "  the vine: no forge ember to copy the wiring from (Set Up MR_Main (10f Cinderwood hands)) or no map");
                return;
            }
            // The 14c lanterns go (same id as the vine: an old save that lit them finds the vine burnt).
            foreach (var old in Object.FindObjectsByType<LanternHollowPiece>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (old.id == Step10fSetup.HollowId)
                {
                    Undo.RecordObject(old, "Step 14e");
                    old.lessonChip = "Touch them!";
                    old.ghostClip = "poke";
                    old.ghostScale = 1f;
                    EditorUtility.SetDirty(old);
                    log.Append(NL + "  smoky hollow: its touch lesson back (chip 'Touch them!', ghost poke)");
                    continue;
                }
                if (old.id != VineId) continue;
                log.Append(NL + $"  '{old.name}' (the 14c arrival lanterns) removed from the scene");
                Undo.DestroyObjectImmediate(old.gameObject);
            }

            var parent = forge.transform.parent;
            var t = parent != null ? parent.Find("Arrival Vine") : null;
            if (t == null)
            {
                var go = new GameObject("Arrival Vine");
                Undo.RegisterCreatedObjectUndo(go, "Step 14e");
                go.transform.SetParent(parent, false);
                t = go.transform;
            }
            var vine = t.GetComponent<VineBurnPiece>();
            if (vine == null) vine = Undo.AddComponent<VineBurnPiece>(t.gameObject);
            Undo.RecordObject(vine, "Step 14e");
            vine.map = map;
            vine.island = forge.island;
            vine.window = forge.window;
            vine.explorer = forge.explorer;
            vine.grabGuard = forge.grabGuard;
            vine.ghosts = forge.ghosts;
            vine.fx = forge.fx;
            vine.focusMaterial = forge.focusMaterial;
            vine.markIntensity = forge.markIntensity;
            vine.grabAssist = forge.grabAssist;
            vine.id = VineId;
            vine.reach = 4;
            vine.lessonLine = "cw.vine";
            vine.lessonChip = "Pick it up!";
            vine.ghostClip = "grab_drop";
            vine.ghostScale = 1f;
            vine.counterLabel = "";
            vine.vent = CinderwoodTools.Tile(CinderwoodTools.ArriveVentXY);
            vine.vine = CinderwoodTools.VineTiles;
            vine.emberRecipe = forge.emberRecipe;
            vine.vineRecipe = PropKit.Load(PropKit.VineName);
            vine.emberSize = forge.emberSize;
            vine.guideMaterial = forge.guideMaterial;
            EditorUtility.SetDirty(vine);
            log.Append(NL + $"  'Arrival Vine' ({VineId}): vent c{vine.vent.x} r{vine.vent.y}, vine c{vine.vine[0].x}-{vine.vine[vine.vine.Length - 1].x} r{vine.vine[0].y}, " +
                       $"let go within {vine.vineRadius * 100f:0} cm of a vine tile; lesson {vine.lessonLine} + ghost {vine.ghostClip}; " +
                       $"recipes {(vine.emberRecipe != null && vine.vineRecipe != null ? "ok" : "MISSING")}, rings {(vine.guideMaterial != null ? "on" : "MISSING")}");
        }
    }
}
