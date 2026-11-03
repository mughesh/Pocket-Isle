using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 14c (Nov 3): Galewick first. A: the realm order's Keeper levels (Galeowl Lv 8, Hearthstag Lv 12, Tidemother
    /// unchanged; RealmOrderSim), all three gates open in builds, the script reimported. B: the Quick Journey goes to
    /// Galewick (gifts Toastling + Whirlby, per-starter levels from QuickJourneySim.SweepWind, the director's realm).
    /// C: Cinderwood's arrival lanterns (rewrites the Cinderwood map: 4 stone lanterns past the arrival, nothing else
    /// moves; "Arrival Lanterns", a LanternHollowPiece copied from the hollow's; the lesson and ghost move there).
    /// D: bakes the four realm cards (PropKit.RealmCards), "Realm Name Card" (RealmNameCard) and "Gate Signs" (GateSigns).
    /// Safe to re-run.
    /// </summary>
    public static class Step14cSetup
    {
        const string NL = "\n";

        [MenuItem("Tools/Pocket Isle/Setup/First Minute (14)/Set Up MR_Main (14c Galewick first)", priority = 1002)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step14c] Stop Play mode first.");
                return;
            }
            var log = new StringBuilder("[Step14c] MR_Main set up:");
            DialogueTools.Import();
            log.Append(NL + "  Script_v1 reimported");
            Order(log);
            Quick(log);
            CinderArrival(log);
            Cards(log);
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }

        /// <summary>
        /// B: the director's quick realm (the gift went on Nov 3: Step14dSetup sets Galeowl's quick offset instead).
        /// </summary>
        static void Quick(StringBuilder log)
        {
            var config = AssetDatabase.LoadAssetAtPath<QuickJourneyConfig>(QuickJourneySim.ConfigPath);
            if (config == null)
            {
                log.Append(NL + "  Quick Journey config MISSING (Step9gSetup.QuickOnly)");
                return;
            }
            var director = Object.FindFirstObjectByType<QuickJourneyDirector>(FindObjectsInactive.Include);
            if (director != null)
            {
                Undo.RecordObject(director, "Step 14c");
                director.galewick = AssetDatabase.LoadAssetAtPath<WorldMap>(GalewickTools.MapPath);
                EditorUtility.SetDirty(director);
            }
            log.Append(NL + $"  Quick Journey: director {(director != null && director.galewick != null ? "-> Galewick" : "MISSING")}");
        }

        /// <summary>C: the arrival lanterns (map + piece); the hollow keeps a counter and a short line, no ghost.</summary>
        static void CinderArrival(StringBuilder log)
        {
            CinderwoodTools.Write(log);
            var pieces = Object.FindObjectsByType<LanternHollowPiece>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var hollow = System.Array.Find(pieces, p => p.id == Step10fSetup.HollowId);
            if (hollow == null)
            {
                log.Append(NL + "  arrival lanterns: no smoky hollow to copy (Set Up MR_Main (10f Cinderwood hands))");
                return;
            }
            var arrive = System.Array.Find(pieces, p => p.id == ArriveLanternsId);
            if (arrive == null)
            {
                var go = new GameObject("Arrival Lanterns");
                Undo.RegisterCreatedObjectUndo(go, "Step 14c");
                go.transform.SetParent(hollow.transform.parent, false);
                arrive = go.AddComponent<LanternHollowPiece>();
            }
            Undo.RecordObject(arrive, "Step 14c");
            EditorUtility.CopySerialized(hollow, arrive);
            // Every field that differs, set explicitly (a re-run copies whatever the hollow has now).
            arrive.id = ArriveLanternsId;
            arrive.reach = 4;
            arrive.lanterns = CinderwoodTools.ArriveLanterns;
            arrive.smoke = new Vector2Int[0];
            arrive.smokeRecipe = null;
            arrive.firstSlot = ArriveFirstSlot;
            arrive.spotRadius = 1.6f;
            arrive.waitFor = "";
            arrive.lessonLine = "cw.arrive.lanterns";
            arrive.lessonChip = "Touch";
            arrive.counterLabel = "Touch · Lanterns";
            arrive.ghostClip = "poke";
            arrive.ghostScale = 1f;
            arrive.doneLine = "cw.arrive.lanterns.done";
            arrive.raceOnTouch = true;
            arrive.embersPerLantern = 8;
            EditorUtility.SetDirty(arrive);

            Undo.RecordObject(hollow, "Step 14c");
            hollow.ghostClip = "";
            hollow.lessonChip = "";
            hollow.counterLabel = "Lanterns";
            hollow.raceOnTouch = false;
            hollow.embersPerLantern = 0;
            EditorUtility.SetDirty(hollow);
            log.Append(NL + $"  'Arrival Lanterns' ({ArriveLanternsId}): {arrive.lanterns.Length} lanterns from c{arrive.lanterns[0].x} r{arrive.lanterns[0].y}, " +
                       $"one touch races the line, warm slots {arrive.firstSlot}-{arrive.firstSlot + arrive.lanterns.Length - 1} (radius {arrive.spotRadius} tiles), " +
                       $"lesson {arrive.lessonLine} + ghost {arrive.ghostClip}, chip '{arrive.counterLabel} n/{arrive.lanterns.Length}'; hollow: no ghost, chip '{hollow.counterLabel}'");
        }

        public const string ArriveLanternsId = "cw.arrive";

        const string LogoMaterialPath = "Assets/_Project/Materials/Props/Logo.mat";
        const string HubPath = "Assets/_Project/Data/WorldMap_StillIsle.asset";

        /// <summary>The lines under each realm's name (first visit / first visit restored).</summary>
        static readonly (string realm, string line, string restored, string shard)[] CardLines =
        {
            ("galewick", "The windmill highlands, where the wind stopped", "The windmill highlands, where the wind blows again", ShardHolder.WindId),
            ("cinderwood", "The autumn forest, where the fire went cold", "The autumn forest, where the fire burns again", ShardHolder.FireId),
            ("lilymere", "The lily lagoon, where the water froze", "The lily lagoon, where the water flows again", ShardHolder.WaterId),
            ("stillisle", "Home", "", ""),
        };

        /// <summary>Each hub gate's sign: its card and the top of its arch (tiles; ShardGates' portal looks).</summary>
        static readonly (string gate, string realm, float archTop)[] SignGates =
        {
            ("wind", "galewick", 0.85f), ("fire", "cinderwood", 1.55f), ("water", "lilymere", 1.05f),
        };

        /// <summary>D: bake the cards, wire the name card and the hub's gate signs.</summary>
        static void Cards(StringBuilder log)
        {
            var baked = new System.Collections.Generic.Dictionary<string, PropRecipe>();
            foreach (var style in PropKit.RealmCards)
            {
                var recipe = PropKit.WriteRealmCard(style);
                var report = PropBaker.Bake(recipe);
                baked[style.realm] = recipe;
                log.Append(NL + $"  card {style.name}: {report}");
            }
            var material = AssetDatabase.LoadAssetAtPath<Material>(LogoMaterialPath);
            if (material == null) log.Append(NL + $"  {LogoMaterialPath} MISSING (Step9gSetup.TitleOnly)");
            var maps = new System.Collections.Generic.Dictionary<string, WorldMap>
            {
                { "galewick", AssetDatabase.LoadAssetAtPath<WorldMap>(GalewickTools.MapPath) },
                { "cinderwood", AssetDatabase.LoadAssetAtPath<WorldMap>(CinderwoodTools.MapPath) },
                { "lilymere", AssetDatabase.LoadAssetAtPath<WorldMap>(LilymereTools.MapPath) },
                { "stillisle", AssetDatabase.LoadAssetAtPath<WorldMap>(HubPath) },
            };
            var island = Object.FindFirstObjectByType<IslandBuilder>(FindObjectsInactive.Include);
            var logo = Object.FindFirstObjectByType<TitleLogo>(FindObjectsInactive.Include);
            var parent = logo != null ? logo.transform.parent : null;

            var card = Object.FindFirstObjectByType<RealmNameCard>(FindObjectsInactive.Include);
            if (card == null)
            {
                var go = new GameObject("Realm Name Card");
                Undo.RegisterCreatedObjectUndo(go, "Step 14c");
                if (parent != null) go.transform.SetParent(parent, false);
                card = go.AddComponent<RealmNameCard>();
            }
            Undo.RecordObject(card, "Step 14c");
            card.island = island;
            card.swap = Object.FindFirstObjectByType<SceneSwap>(FindObjectsInactive.Include);
            card.encounters = Object.FindFirstObjectByType<EncounterDirector>(FindObjectsInactive.Include);
            card.fx = Object.FindFirstObjectByType<BattleFx>(FindObjectsInactive.Include);
            card.material = material;
            card.cards = System.Array.ConvertAll(CardLines, c => new RealmNameCard.Card
            {
                map = maps[c.realm], recipe = baked[c.realm], line = c.line, restoredLine = c.restored, shard = c.shard,
            });
            EditorUtility.SetDirty(card);

            var signs = Object.FindFirstObjectByType<GateSigns>(FindObjectsInactive.Include);
            if (signs == null)
            {
                var go = new GameObject("Gate Signs");
                Undo.RegisterCreatedObjectUndo(go, "Step 14c");
                if (parent != null) go.transform.SetParent(parent, false);
                signs = go.AddComponent<GateSigns>();
            }
            Undo.RecordObject(signs, "Step 14c");
            signs.island = island;
            signs.window = Object.FindFirstObjectByType<WorldWindow>(FindObjectsInactive.Include);
            signs.gates = Object.FindFirstObjectByType<ShardGates>(FindObjectsInactive.Include);
            signs.material = material;
            signs.signs = System.Array.ConvertAll(SignGates, s => new GateSigns.Sign { gate = s.gate, recipe = baked[s.realm], archTop = s.archTop });
            EditorUtility.SetDirty(signs);
            int missing = new Object[] { card.island, card.swap, card.encounters, card.fx, card.material, signs.window, signs.gates }.Count(o => o == null) +
                          card.cards.Count(c => c.map == null);
            log.Append(NL + $"  'Realm Name Card' + 'Gate Signs' (wind / fire / water): {(missing == 0 ? "wired" : $"{missing} reference(s) MISSING")}");
            Shots(log, baked);
        }

        /// <summary>Logs/BattleShots/14c_cards.png (the four cards) and 14c_gatesigns.png (the three gate signs side by side).</summary>
        static void Shots(StringBuilder log, System.Collections.Generic.Dictionary<string, PropRecipe> baked)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(LogoMaterialPath);
            if (material == null) return;
            Shot($"{Step9cSetup.ShotDir}/14c_cards.png", material, new[] { baked["galewick"], baked["cinderwood"], baked["lilymere"], baked["stillisle"] }, 2, 1600, 900);
            Shot($"{Step9cSetup.ShotDir}/14c_gatesigns.png", material, new[] { baked["galewick"], baked["cinderwood"], baked["lilymere"] }, 3, 2100, 420);
            log.Append(NL + $"  shots: {Step9cSetup.ShotDir}/14c_cards.png, 14c_gatesigns.png (cards only: the line text and the hub need Play)");
        }

        static void Shot(string path, Material material, PropRecipe[] recipes, int columns, int w, int h)
        {
            var preview = EditorSceneManager.NewPreviewScene();
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            try
            {
                var camGo = new GameObject("Card Camera");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camGo, preview);
                var cam = camGo.AddComponent<Camera>();
                cam.scene = preview;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.55f, 0.66f, 0.72f);
                cam.orthographic = true;
                cam.stereoTargetEye = StereoTargetEyeMask.None;
                var lightGo = new GameObject("Card Light");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightGo, preview);
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                lightGo.transform.rotation = Quaternion.LookRotation(-new Vector3(-0.35f, 0.7f, 0.6f).normalized);
                int rows = (recipes.Length + columns - 1) / columns;
                float cellW = 9f, cellH = 3.2f;
                for (int i = 0; i < recipes.Length; i++)
                {
                    var mesh = recipes[i] != null ? recipes[i].bakedMesh : null;
                    if (mesh == null) continue;
                    var go = new GameObject(recipes[i].name);
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, preview);
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var r = go.AddComponent<MeshRenderer>();
                    r.sharedMaterial = material;
                    var block = new MaterialPropertyBlock();
                    recipes[i].ApplyTo(block);
                    block.SetFloat("_WindowClip", 0f);
                    block.SetFloat("_RestoredSelf", 1f);
                    block.SetFloat("_ClearSink", 0f);
                    r.SetPropertyBlock(block);
                    float k = (cellW * 0.92f) / Mathf.Max(1e-3f, mesh.bounds.size.x);
                    go.transform.localScale = Vector3.one * k;
                    int col = i % columns, row = i / columns;
                    // Seen from +Z looking back: the camera's right is -X.
                    var at = new Vector3(-(col - (columns - 1) * 0.5f) * cellW, -(row - (rows - 1) * 0.5f) * cellH, 0f);
                    go.transform.position = at - mesh.bounds.center * k;
                }
                cam.orthographicSize = Mathf.Max(rows * cellH * 0.55f, columns * cellW * 0.52f * h / w);
                cam.transform.position = new Vector3(0f, 0f, 20f);
                cam.transform.rotation = Quaternion.LookRotation(Vector3.back);
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 60f;
                var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
                cam.targetTexture = rt;
                cam.Render();
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                RenderTexture.active = prev;
                cam.targetTexture = null;
                rt.Release();
                Object.DestroyImmediate(rt);
                System.IO.Directory.CreateDirectory(Step9cSetup.ShotDir);
                System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(tex);
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        /// <summary>Bridge: re-bake the four cards and shoot them (no scene changes).</summary>
        public static void BakeCards()
        {
            var log = new StringBuilder("[Step14c] Cards baked:");
            foreach (var style in PropKit.RealmCards) log.Append(NL + $"  {PropBaker.Bake(PropKit.WriteRealmCard(style))}");
            Debug.Log(log.ToString());
            CardShots();
        }

        /// <summary>Bridge: the card shots only (no scene changes).</summary>
        public static void CardShots()
        {
            var baked = new System.Collections.Generic.Dictionary<string, PropRecipe>();
            foreach (var style in PropKit.RealmCards) baked[style.realm] = PropKit.Load(style.asset);
            var log = new StringBuilder("[Step14c] Card shots:");
            Shots(log, baked);
            Debug.Log(log.ToString());
        }
        /// <summary>WarmSpots slots 8-11 (Cinderwood's village 0, hollow 1-6, forge 7).</summary>
        public const int ArriveFirstSlot = 8;

        /// <summary>A: Keeper levels for the Galewick-first order, every gate open in builds.</summary>
        static void Order(StringBuilder log)
        {
            foreach (var hall in Object.FindObjectsByType<KeeperHall>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (hall.keeperSpecies == null) continue;
                int level = hall.keeperSpecies.name switch
                {
                    "Galeowl" => GaleowlKit.Level,
                    "Hearthstag" => KeeperKit.Level,
                    "Tidemother" => TidemotherKit.Level,
                    _ => hall.keeperLevel,
                };
                if (hall.keeperLevel == level) continue;
                Undo.RecordObject(hall, "Step 14c");
                log.Append(NL + $"  {hall.keeperSpecies.name}: Lv {hall.keeperLevel} -> {level}");
                hall.keeperLevel = level;
                EditorUtility.SetDirty(hall);
            }
            foreach (var path in new[] { GalewickTools.ConfigPath, LilymereTools.ConfigPath })
            {
                var c = AssetDatabase.LoadAssetAtPath<GalewickConfig>(path);
                if (c == null)
                {
                    log.Append(NL + $"  {path} MISSING");
                    continue;
                }
                if (!c.open)
                {
                    c.open = true;
                    EditorUtility.SetDirty(c);
                }
                log.Append(NL + $"  {c.name}: open in builds");
            }
        }
    }
}
