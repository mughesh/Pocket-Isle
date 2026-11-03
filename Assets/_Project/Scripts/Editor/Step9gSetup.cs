using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 9g: title screen, game menu, settings, Quick Journey. One method per section
    /// (bridge: exec:Diorama.EditorTools.Step9gSetup.X), <see cref="All"/> for everything. Safe to re-run.
    /// </summary>
    public static class Step9gSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";
        const string LogoMaterialPath = "Assets/_Project/Materials/Props/Logo.mat";

        [MenuItem("Tools/Pocket Isle/Setup/Cinderwood + Hub (9, 10f)/Set Up MR_Main (9g title, menus, quick)", priority = 604)]
        public static void All()
        {
            var log = new StringBuilder("[Step9g] Set up:");
            Title(log);
            Menu(log);
            Quick(log);
            Save(log);
        }

        public static void QuickOnly()
        {
            var log = new StringBuilder("[Step9g] Quick:");
            Quick(log);
            Save(log);
        }

        public static void MenuOnly()
        {
            var log = new StringBuilder("[Step9g] Menu:");
            Menu(log);
            Save(log);
        }

        public static void TitleOnly()
        {
            var log = new StringBuilder("[Step9g] Title:");
            Title(log);
            Save(log);
        }

        static T Find<T>() where T : Object => Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);

        static GameObject Child(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t != null) return t.gameObject;
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Step 9g");
            go.transform.SetParent(parent, false);
            return go;
        }

        static T Component<T>(GameObject go) where T : Component => go.TryGetComponent(out T c) ? c : Undo.AddComponent<T>(go);

        /// <summary>A: the logo prop (written and baked), its material, the Title object (logo + runes) wired to the opening.</summary>
        public static void Title(StringBuilder log)
        {
            var recipe = PropKit.WriteLogo();
            var report = PropBaker.Bake(recipe);
            log.Append($"\n  Logo: {report}");
            var material = LogoMaterial();

            var island = Find<IslandBuilder>();
            var opening = Find<Opening>();
            var runesRef = Find<BattleRunes>();
            if (island == null || opening == null || runesRef == null)
            {
                log.Append("\n  MISSING island, opening or battle runes: open MR_Main.");
                return;
            }
            var titleGo = Child(island.transform, "Title");
            var title = Component<TitleScreen>(titleGo);
            var logo = Component<TitleLogo>(Child(titleGo.transform, "Title Logo"));
            var runes = Component<RuneBoard>(Child(titleGo.transform, "Title Runes"));
            Undo.RecordObject(title, "Step 9g");
            Undo.RecordObject(logo, "Step 9g");
            Undo.RecordObject(runes, "Step 9g");
            Undo.RecordObject(opening, "Step 9g");
            logo.island = island;
            logo.recipe = recipe;
            logo.material = material;
            logo.fx = Find<BattleFx>();
            Wire(runes, island, runesRef);
            title.island = island;
            title.logo = logo;
            title.runes = runes;
            title.save = Find<GameSave>();
            opening.title = title;
            foreach (var o in new Object[] { title, logo, runes, opening }) EditorUtility.SetDirty(o);
            log.Append($"\n  Title: logo {(logo.fx != null ? "+ fx" : "(no fx)")}, runes, wired to the opening.");
        }

        /// <summary>
        /// B: the game menu (left-hand menu gesture / Esc) and the settings board, both rune boards over the near rim;
        /// the menu's row sits a little higher than the battle runes so the two never overlap. Reimports the dialogue
        /// script (menu.first).
        /// </summary>
        public static void Menu(StringBuilder log)
        {
            var island = Find<IslandBuilder>();
            var runesRef = Find<BattleRunes>();
            if (island == null || runesRef == null)
            {
                log.Append("\n  MISSING island or battle runes: open MR_Main.");
                return;
            }
            var menuGo = Child(island.transform, "Game Menu");
            var menu = Component<GameMenu>(menuGo);
            var menuRunes = Component<RuneBoard>(Child(menuGo.transform, "Menu Runes"));
            var settingsGo = Child(island.transform, "Settings");
            var settings = Component<SettingsBoard>(settingsGo);
            var settingsRunes = Component<RuneBoard>(Child(settingsGo.transform, "Settings Runes"));
            Wire(menuRunes, island, runesRef);
            Wire(settingsRunes, island, runesRef);
            menuRunes.offset = runesRef.offset + new Vector3(0f, 0.07f, 0f);
            Undo.RecordObject(menu, "Step 9g");
            Undo.RecordObject(settings, "Step 9g");
            menu.runes = menuRunes;
            menu.settings = settings;
            menu.island = island;
            menu.handle = Find<IslandHandle>();
            menu.explorer = Find<GestureTestPiece>();
            menu.save = Find<GameSave>();
            menu.pause = Find<GamePause>();
            menu.encounters = Find<EncounterDirector>();
            menu.onboarding = Find<Onboarding>();
            settings.runes = settingsRunes;
            settings.placer = Find<IslandPlacer>();
            const string audioCredits = "Assets/Docs/AudioCredits.md";
            if (System.IO.File.Exists(audioCredits) && !settings.credits.Contains("Audio"))
                settings.credits += "\n\nAudio\n" + System.IO.File.ReadAllText(audioCredits).Trim();
            EditorUtility.SetDirty(menu);
            EditorUtility.SetDirty(settings);
            int missing = new Object[] { menu.handle, menu.explorer, menu.save, menu.pause, menu.encounters, menu.onboarding, settings.placer }.Count(o => o == null);
            log.Append($"\n  Game menu + settings: {(missing == 0 ? "all references found" : $"{missing} reference(s) MISSING")}" +
                       $"{(System.IO.File.Exists(audioCredits) ? ", audio credits added" : ", no AudioCredits.md yet")}.");
            DialogueTools.Import();
            log.Append("\n  Dialogue script reimported (menu.first).");
        }

        /// <summary>
        /// C: the Quick Journey config (Data/QuickJourney.asset: created once, then only the gift species are filled in
        /// when empty, so tuning survives a re-run), the director with its end runes, and the dialogue lines.
        /// </summary>
        public static void Quick(StringBuilder log)
        {
            var config = AssetDatabase.LoadAssetAtPath<QuickJourneyConfig>(QuickJourneySim.ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<QuickJourneyConfig>();
                AssetDatabase.CreateAsset(config, QuickJourneySim.ConfigPath);
                log.Append("\n  Created Data/QuickJourney.asset.");
            }
            EditorUtility.SetDirty(config);

            var island = Find<IslandBuilder>();
            var runesRef = Find<BattleRunes>();
            if (island == null || runesRef == null)
            {
                log.Append("\n  MISSING island or battle runes: open MR_Main.");
                return;
            }
            var go = Child(island.transform, "Quick Journey");
            var director = Component<QuickJourneyDirector>(go);
            var runes = Component<RuneBoard>(Child(go.transform, "Quick Runes"));
            Wire(runes, island, runesRef);
            Undo.RecordObject(director, "Step 9g");
            director.config = config;
            director.party = Find<Party>();
            director.pods = Find<PodRack>();
            director.spirit = Find<LanternSpirit>();
            director.island = island;
            director.explorer = Find<GestureTestPiece>();
            director.encounters = Find<EncounterDirector>();
            director.onboarding = Find<Onboarding>();
            director.save = Find<GameSave>();
            director.swap = Find<SceneSwap>();
            director.fx = Find<BattleFx>();
            director.runes = runes;
            director.hub = AssetDatabase.LoadAssetAtPath<WorldMap>("Assets/_Project/Data/WorldMap_StillIsle.asset");
            director.galewick = AssetDatabase.LoadAssetAtPath<WorldMap>(GalewickTools.MapPath); // step 14c: the quick realm
            EditorUtility.SetDirty(director);
            int missing = new Object[] { director.party, director.pods, director.spirit, director.explorer, director.encounters, director.onboarding,
                director.save, director.swap, director.fx, director.hub, director.galewick }.Count(o => o == null);
            log.Append($"\n  Quick Journey: Keeper {config.keeperLevelOffset:+0;-0} levels, " +
                       $"cuts {string.Join(", ", config.cutLessons)}; director {(missing == 0 ? "wired" : $"{missing} reference(s) MISSING")}.");
            DialogueTools.Import();
        }

        /// <summary>A rune board placed and painted like the battle runes.</summary>
        static void Wire(RuneBoard board, IslandBuilder island, BattleRunes like)
        {
            Undo.RecordObject(board, "Step 9g");
            board.island = island;
            board.grabGuard = like.grabGuard;
            board.hudMaterial = like.hudMaterial;
            board.offset = like.offset;
            board.fontSize = like.fontSize;
            board.pressDepth = like.pressDepth;
            EditorUtility.SetDirty(board);
        }

        /// <summary>Diorama/Prop without the island window clip, its own (restored) colours, no battle sinking.</summary>
        static Material LogoMaterial()
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(LogoMaterialPath);
            if (m == null)
            {
                m = new Material(Shader.Find("Diorama/Prop")) { name = "Logo" };
                AssetDatabase.CreateAsset(m, LogoMaterialPath);
            }
            m.SetFloat("_WindowClip", 0f);
            m.SetFloat("_RestoredSelf", 1f);
            m.SetFloat("_ClearSink", 0f);
            EditorUtility.SetDirty(m);
            return m;
        }

        static void Save(StringBuilder log)
        {
            AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path == ScenePath && !EditorApplication.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                log.Append("\n  Scene saved.");
            }
            else log.Append($"\n  Scene NOT saved (active: {scene.path}, playing {EditorApplication.isPlaying}).");
            Debug.Log(log.ToString());
        }
    }
}
