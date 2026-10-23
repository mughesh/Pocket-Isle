using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 5: battle data (type chart, placeholder moves, encounter tables, species move slots) and the battle objects
    /// in MR_Main. Starting values live here; after review, tune them in the Inspector, not in code
    /// (menu 1 overwrites the assets).
    /// </summary>
    static class Step5BattleSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";
        const string BattleRoot = "Assets/_Project/Data/Battle";
        const string SpeciesRoot = "Assets/_Project/Data/Creatures/Species";
        const string MaterialRoot = "Assets/_Project/Materials/Battle";
        static readonly string[] OldWanderers = { "Toastling", "Waxle", "Kettlewing" };

        [MenuItem("Tools/Diorama/Step 5/1. Write Battle Data (overwrites)")]
        public static void WriteData()
        {
            DioramaSceneSetup.EnsureFolder(BattleRoot);
            DioramaSceneSetup.EnsureFolder(BattleRoot + "/Moves");
            Asset<TypeChart>(BattleRoot + "/TypeChart.asset");

            var ember = Move("EmberPuff", "Ember Puff", Element.Fire, 40, 95, MoveAnimation.Shoot);
            var steam = Move("SteamJet", "Steam Jet", Element.Fire, 45, 90, MoveAnimation.Shoot);
            var stomp = Move("KilnStomp", "Kiln Stomp", Element.Fire, 50, 85, MoveAnimation.Slam);
            var bubble = Move("BubblePop", "Bubble Pop", Element.Water, 40, 95, MoveAnimation.Shoot, arc: true);
            var gust = Move("GustFlick", "Gust Flick", Element.Wind, 40, 95, MoveAnimation.Shoot);
            var tackle = Move("Tackle", "Tackle", Element.Neutral, 35, 100, MoveAnimation.Lunge);
            var bodySlam = Move("BodySlam", "Body Slam", Element.Neutral, 45, 90, MoveAnimation.Slam);
            var hunker = Support("HunkerDown", "Hunker Down", SupportEffect.Shield);
            var puff = Support("PuffUp", "Puff Up", SupportEffect.RaiseAttack);
            var nap = Support("WarmNap", "Warm Nap", SupportEffect.Heal);

            var report = new StringBuilder("[Step5] Battle data written:");
            SetMoves(report, "Kilnpot", ember, tackle, hunker);
            SetMoves(report, "Kilnhearth", ember, bodySlam, puff);
            SetMoves(report, "Pebbloop", bubble, bodySlam, hunker);
            SetMoves(report, "Pufflet", gust, tackle, puff);
            SetMoves(report, "Toastling", ember, tackle, puff);
            SetMoves(report, "Crumbhog", ember, bodySlam, puff);
            SetMoves(report, "Waxle", ember, tackle, nap);
            SetMoves(report, "Lanternsnail", ember, bodySlam, nap);
            SetMoves(report, "Kettlewing", steam, tackle, hunker);
            SetMoves(report, "Glazegolem", stomp, bodySlam, hunker);
            SetMoves(report, "Kilngolem", stomp, bodySlam, hunker);

            var grass = Asset<EncounterTable>(BattleRoot + "/Encounters_TestGrass.asset");
            grass.habitat = Habitat.Grass;
            grass.entries = new[]
            {
                Entry("Toastling", 3, 5, 32f),
                Entry("Waxle", 3, 5, 32f),
                Entry("Kettlewing", 3, 5, 31f),
                Entry("Glazegolem", 5, 6, 5f, rare: true),
            };
            EditorUtility.SetDirty(grass);
            var water = Asset<EncounterTable>(BattleRoot + "/Encounters_TestWater.asset");
            water.habitat = Habitat.Water;
            water.entries = new[] { Entry("Pebbloop", 3, 5, 1f) };
            EditorUtility.SetDirty(water);

            AssetDatabase.SaveAssets();
            report.Append("\n  Grass table: Toastling, Waxle, Kettlewing (lv 3-5), Glazegolem rare 5% (lv 5-6). Water table: Pebbloop (lv 3-5).");
            Debug.Log(report.ToString());
        }

        [MenuItem("Tools/Diorama/Step 5/2. Add Battle to MR_Main")]
        public static void AddToMain()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                if (scene.isDirty)
                {
                    Debug.LogError($"[Step5] Save or discard changes in {scene.name} first.");
                    return;
                }
                scene = EditorSceneManager.OpenScene(ScenePath);
            }
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            var explorer = Object.FindFirstObjectByType<GestureTestPiece>();
            var window = Object.FindFirstObjectByType<WorldWindow>();
            var handle = Object.FindFirstObjectByType<IslandHandle>();
            if (island == null || explorer == null || window == null)
            {
                Debug.LogError("[Step5] MR_Main needs the island, the explorer and the world window (Step 3 menus).");
                return;
            }
            var chart = AssetDatabase.LoadAssetAtPath<TypeChart>(BattleRoot + "/TypeChart.asset");
            if (chart == null)
            {
                WriteData();
                chart = AssetDatabase.LoadAssetAtPath<TypeChart>(BattleRoot + "/TypeChart.asset");
            }
            var kilnpot = AssetDatabase.LoadAssetAtPath<CreatureSpecies>(SpeciesRoot + "/Kilnpot.asset");

            var report = new StringBuilder("[Step5] Battle added to MR_Main:");
            foreach (var name in OldWanderers)
            {
                var old = island.transform.Find(name);
                if (old == null) continue;
                Object.DestroyImmediate(old.gameObject);
                report.Append($"\n  removed the wandering {name}");
            }

            DioramaSceneSetup.EnsureFolder(MaterialRoot);
            var orb = FxMaterial("FxOrb", 0, UnityEngine.Rendering.CullMode.Back);
            var ring = FxMaterial("FxRing", 1, UnityEngine.Rendering.CullMode.Off);
            var particle = FxMaterial("FxParticle", 2, UnityEngine.Rendering.CullMode.Off);
            var hud = MaterialAsset("Hud", "Diorama/Hud");
            AssetDatabase.SaveAssets();

            var stageGo = Child(island.transform, "Battle Stage");
            var fx = Component<BattleFx>(stageGo);
            fx.orbMaterial = orb;
            fx.ringMaterial = ring;
            fx.particleMaterial = particle;
            var hudComp = Component<BattleHud>(stageGo);
            hudComp.hudMaterial = hud;
            var stage = Component<BattleStage>(stageGo);
            stage.island = island;
            stage.fx = fx;
            stage.hud = hudComp;

            var runesGo = Child(island.transform, "Battle Runes");
            var runes = Component<BattleRunes>(runesGo);
            runes.island = island;
            runes.grabGuard = handle;
            runes.hudMaterial = hud;

            var root = GameObject.Find("Battle");
            if (root == null) root = new GameObject("Battle");
            var party = Component<Party>(root);
            if (party.members.Count == 0) party.members.Add(new CreatureInstance(kilnpot, 5));
            var director = Component<EncounterDirector>(root);
            director.explorer = explorer;
            director.window = window;
            director.island = island;
            director.party = party;
            director.stage = stage;
            director.runes = runes;
            director.typeChart = chart;
            director.grassTable = AssetDatabase.LoadAssetAtPath<EncounterTable>(BattleRoot + "/Encounters_TestGrass.asset");
            director.waterTable = AssetDatabase.LoadAssetAtPath<EncounterTable>(BattleRoot + "/Encounters_TestWater.asset");
            foreach (var c in new Component[] { fx, hudComp, stage, runes, party, director }) EditorUtility.SetDirty(c);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            report.Append($"\n  Battle (Party: {party.members.Count} creature(s), EncounterDirector), Island/Battle Stage, Island/Battle Runes");
            report.Append($"\n  rim grab guard: {(handle != null ? "yes" : "none found, pinches are never blocked")}");
            Debug.Log(report.ToString());
        }

        /// <summary>Data then scene in one go (bridge: exec:Diorama.EditorTools.Step5BattleSetup.Batch).</summary>
        public static void Batch()
        {
            WriteData();
            AddToMain();
        }

        static T Asset<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        static MoveData Move(string file, string name, Element element, int power, int accuracy, MoveAnimation animation, bool arc = false)
        {
            var m = Asset<MoveData>($"{BattleRoot}/Moves/{file}.asset");
            m.displayName = name;
            m.element = element;
            m.kind = MoveKind.Attack;
            m.power = power;
            m.accuracy = accuracy;
            m.animation = animation;
            m.arc = arc;
            m.effect = SupportEffect.None;
            EditorUtility.SetDirty(m);
            return m;
        }

        static MoveData Support(string file, string name, SupportEffect effect)
        {
            var m = Asset<MoveData>($"{BattleRoot}/Moves/{file}.asset");
            m.displayName = name;
            m.element = Element.Neutral;
            m.kind = MoveKind.Support;
            m.power = 0;
            m.accuracy = 100;
            m.animation = MoveAnimation.Shield;
            m.arc = false;
            m.effect = effect;
            m.healShare = 0.3f;
            EditorUtility.SetDirty(m);
            return m;
        }

        static void SetMoves(StringBuilder report, string species, params MoveData[] moves)
        {
            var s = AssetDatabase.LoadAssetAtPath<CreatureSpecies>($"{SpeciesRoot}/{species}.asset");
            if (s == null)
            {
                Debug.LogError($"[Step5] Species {species} not found (write the starter and fire kits first).");
                return;
            }
            s.moves = moves;
            EditorUtility.SetDirty(s);
            report.Append($"\n  {species}: {moves[0].displayName}, {moves[1].displayName}, {moves[2].displayName}");
        }

        static EncounterTable.Entry Entry(string species, int min, int max, float weight, bool rare = false)
        {
            var s = AssetDatabase.LoadAssetAtPath<CreatureSpecies>($"{SpeciesRoot}/{species}.asset");
            if (s == null) Debug.LogError($"[Step5] Species {species} not found for the encounter table.");
            return new EncounterTable.Entry { species = s, minLevel = min, maxLevel = max, weight = weight, rare = rare };
        }

        static Material FxMaterial(string name, int mode, UnityEngine.Rendering.CullMode cull)
        {
            var m = MaterialAsset(name, "Diorama/Fx");
            m.SetFloat("_Mode", mode);
            m.SetFloat("_Cull", (float)cull);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material MaterialAsset(string name, string shader)
        {
            string path = $"{MaterialRoot}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find(shader)) { name = name };
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static GameObject Child(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t != null) return t.gameObject;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        static T Component<T>(GameObject go) where T : Component => go.TryGetComponent(out T c) ? c : go.AddComponent<T>();
    }
}
