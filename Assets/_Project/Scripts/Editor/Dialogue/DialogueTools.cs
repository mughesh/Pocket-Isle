using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static Diorama.EditorTools.StarterKit;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 5c: imports Docs/Story/Script_v1.md into a DialogueScript asset, writes and bakes the Lantern Spirit, and
    /// adds the Spirit, the dialogue system and its bubble to MR_Main.
    /// </summary>
    public static class DialogueTools
    {
        const string ScriptPath = "Assets/Docs/Story/Script_v1.md";
        const string AssetPath = "Assets/_Project/Data/Dialogue/Script_v1.asset";
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";
        const string HudPath = "Assets/_Project/Materials/Battle/Hud.mat";

        // ---- import ----

        static readonly Regex TriggerLine = new Regex(@"^- `([^`]+)`(.*)$");
        static readonly Regex SpeakerLine = new Regex(@"^(?:- )?([A-Z][A-Za-z ]*?):\s+(.+)$");

        [MenuItem("Tools/Pocket Isle/Dialogue/Import Script_v1", priority = 1)]
        public static DialogueScript Import()
        {
            if (!File.Exists(ScriptPath))
            {
                Debug.LogError($"[Dialogue] {ScriptPath} not found.");
                return null;
            }
            DioramaSceneSetup.EnsureFolder("Assets/_Project/Data/Dialogue");
            var asset = Asset<DialogueScript>(AssetPath);
            asset.triggers = new List<DialogueTrigger>();
            asset.idleHints = new List<DialogueLine>();

            string section = "";
            DialogueTrigger current = null;
            var warnings = new StringBuilder();
            int n = 0;
            foreach (var raw in File.ReadAllLines(ScriptPath))
            {
                n++;
                if (raw.StartsWith("## "))
                {
                    section = raw.Substring(3).Trim();
                    current = null;
                    continue;
                }
                if (string.IsNullOrWhiteSpace(raw) || raw.StartsWith("#")) continue;
                bool indented = raw.StartsWith(" ") || raw.StartsWith("\t");
                string line = raw.Trim();
                if (!line.StartsWith("- ")) continue;

                var tm = TriggerLine.Match(line);
                if (!indented && tm.Success)
                {
                    current = new DialogueTrigger { id = tm.Groups[1].Value.Trim(), priority = PriorityOf(section) };
                    asset.triggers.Add(current);
                    ParseFlags(current, tm.Groups[2].Value, n, warnings);
                    continue;
                }
                // Step 14k: an idle hint carries its context in front: "- [in outside & !battle] Spirit: text".
                string hintContext = null;
                var hc = HintContext.Match(line);
                if (hc.Success)
                {
                    hintContext = hc.Groups[1].Value.Trim();
                    line = "- " + hc.Groups[2].Value;
                }
                var sm = SpeakerLine.Match(line);
                if (!sm.Success) continue;
                var dl = Line(sm.Groups[1].Value, sm.Groups[2].Value);
                if (hintContext != null) dl.context = hintContext == "any" ? "" : hintContext;
                if (indented && current != null) current.lines.Add(dl);
                else if (!indented && section.StartsWith("Idle")) asset.idleHints.Add(dl);
                else warnings.Append($"\n  line {n}: speaker line outside a trigger, ignored");
            }
            foreach (var t in asset.triggers)
                if (t.lines.Count == 0) warnings.Append($"\n  {t.id}: no lines");
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            int lines = 0;
            foreach (var t in asset.triggers) lines += t.lines.Count;
            Debug.Log($"[Dialogue] Imported {ScriptPath}: {asset.triggers.Count} triggers, {lines} lines, {asset.idleHints.Count} idle hints." +
                      (warnings.Length > 0 ? $" Warnings:{warnings}" : ""));
            return asset;
        }

        /// <summary>Opening and tutorials 1, story (realms, finale) 2. Idle hints are 0.</summary>
        static int PriorityOf(string section) =>
            section.StartsWith("Opening") || section.StartsWith("Tutorials") ? 1 : 2;

        // Dots only between word characters (puzzle:cinder.log), so a target can still end a sentence.
        static readonly Regex TargetFlag = new Regex(@"·?\s*→\s*([A-Za-z][\w:,]*(?:\.\w+)*)");
        static readonly Regex WhileFlag = new Regex(@"·\s*while\s+([!\w.&| ]+)");
        static readonly Regex ObjectiveFlag = new Regex(@"·\s*objective\b");
        // Step 14k: "· in lodge", "· in outside & !battle" (the line's context) and "· intro" / "· story" / "· filler".
        static readonly Regex InFlag = new Regex(@"·\s*in\s+([!\w.&| ]+?)(?=\s*(?:·|\(|:|$))");
        static readonly Regex KindFlag = new Regex(@"·\s*(intro|story|filler)\b");
        static readonly Regex HintContext = new Regex(@"^- \[in ([^\]]+)\]\s*(.+)$");

        /// <summary>
        /// After the id: "· once", "· once (note)", "· once: Spirit: text", "· Spirit: text", or nothing, plus an optional
        /// target anchor anywhere before the text: "· → cradle" (the Spirit flies there for these lines), and an optional
        /// condition "· while canWalk &amp; !battle" (<see cref="DialogueTrigger.validWhile"/>).
        /// </summary>
        static void ParseFlags(DialogueTrigger t, string rest, int n, StringBuilder warnings)
        {
            var objective = ObjectiveFlag.Match(rest);
            if (objective.Success)
            {
                t.objective = true;
                t.kind = LineKind.Objective;
                rest = rest.Remove(objective.Index, objective.Length);
            }
            var where = InFlag.Match(rest);
            if (where.Success)
            {
                string c = where.Groups[1].Value.Trim();
                t.context = c == "any" ? "" : c;
                rest = rest.Remove(where.Index, where.Length);
            }
            else warnings.Append($"\n  line {n} ({t.id}): no '· in <place>' context");
            var kind = KindFlag.Match(rest);
            if (kind.Success)
            {
                if (!t.objective)
                    t.kind = kind.Groups[1].Value == "intro" ? LineKind.Intro : kind.Groups[1].Value == "filler" ? LineKind.Filler : LineKind.Story;
                rest = rest.Remove(kind.Index, kind.Length);
            }
            var when = WhileFlag.Match(rest);
            if (when.Success)
            {
                t.validWhile = when.Groups[1].Value.Trim();
                rest = rest.Remove(when.Index, when.Length);
            }
            var target = TargetFlag.Match(rest);
            if (target.Success)
            {
                t.target = target.Groups[1].Value;
                rest = rest.Remove(target.Index, target.Length);
            }
            rest = rest.Trim().TrimStart('·').Trim();
            if (rest.StartsWith("once"))
            {
                t.once = true;
                rest = rest.Substring(4).Trim();
            }
            if (rest.StartsWith("("))
            {
                int close = rest.IndexOf(')');
                if (close > 0)
                {
                    t.note = rest.Substring(1, close - 1).Trim();
                    rest = rest.Substring(close + 1).Trim();
                }
            }
            rest = rest.TrimStart(':', '·').Trim();
            if (rest.Length == 0) return;
            var sm = SpeakerLine.Match(rest);
            if (sm.Success) t.lines.Add(Line(sm.Groups[1].Value, sm.Groups[2].Value));
            else warnings.Append($"\n  line {n} ({t.id}): couldn't read '{rest}'");
        }

        static DialogueLine Line(string speaker, string text)
        {
            speaker = speaker.Trim();
            var s = speaker == "Spirit" ? DialogueSpeaker.Spirit
                : speaker == "Heart" ? DialogueSpeaker.Heart
                : speaker == "Hearthstag" || speaker == "Tidemother" || speaker == "Galeowl" ? DialogueSpeaker.Keeper
                : DialogueSpeaker.Other;
            return new DialogueLine { speaker = s, speakerName = speaker, text = text.Trim() };
        }

        // ---- Lantern Spirit ----

        /// <summary>Glass-acorn body glowing amber, crooked mossy stone cap, two ear-wisps orbiting. Concept 04 (left).</summary>
        public static CreatureRecipe WriteSpirit()
        {
            var cap = Part("AcornCap", PartMotion.None, Vector3.up, 0f, 0f,
                new PartStyle
                {
                    color = PaletteSlot.Accent, spec = 0.1f, outline = 0.03f,
                    inner = true, innerColor = PaletteSlot.Extra, innerDirection = Vector3.up, innerAt = 0.55f,
                },
                Ell(new Vector3(0f, 0.04f, 0f), new Vector3(0.5f, 0.2f, 0.5f)).Bumps(0.012f, 16f),
                Torus(new Vector3(0f, 0.0f, 0f), 0.42f, 0.085f, Vector3.zero, 0.08f).Bumps(0.01f, 16f),
                Cone(new Vector3(0f, 0.18f, 0f), new Vector3(0.05f, 0.34f, 0.02f), 0.07f, 0.045f, 0.06f));
            Budget(cap, 44, 900);

            var wisps = Part("EarWisps", PartMotion.Spin, Vector3.up, 0f, 0.18f,
                new PartStyle { color = PaletteSlot.Glow, gradient = true, gradientTo = PaletteSlot.Belly, glow = 0.9f, spec = 0.4f, outline = 0.018f },
                Sphere(new Vector3(0.64f, 0f, 0f), 0.085f, 0f).Mirror(),
                Cone(new Vector3(0.64f, 0f, 0f), new Vector3(0.69f, 0.17f, 0f), 0.08f, 0.012f, 0.07f).Mirror());
            Budget(wisps, 48, 600);

            var body = Shape("LanternSpirit_Body",
                Ell(new Vector3(0f, 0.52f, 0f), new Vector3(0.42f, 0.44f, 0.42f)),
                Cone(new Vector3(0f, 0.42f, 0f), new Vector3(0f, 0.03f, 0f), 0.3f, 0.05f, 0.28f));

            var palette = new CreaturePalette
            {
                baseColor = Hex("#F5A83A"), belly = Hex("#FFD983"), accent = Hex("#8E9682"), socks = Hex("#F5A83A"),
                glow = Hex("#FFC45C"), extra = Hex("#6F8A4C"), face = Hex("#3B2416"), iris = Hex("#2A1A10"),
            };
            var zones = new BodyZones { belly = true, bellyDirection = new Vector3(0f, 0.25f, 1f), bellyAt = 0.45f, bellyMaxY = 9f };
            var face = new FaceSettings
            {
                eyeSize = 0.06f, eyeSpacing = 0.15f, eyeHeight = 0.55f, eyeStretch = 1.3f,
                decals = new List<FaceDecal> { new FaceDecal { kind = DecalKind.Smile, at = new Vector2(0f, 0.43f), size = new Vector2(0.05f, 0.05f), color = PaletteSlot.Face } },
            };
            var idle = new IdleStyle { breathe = 0.015f, breatheSpeed = 2f };
            var parts = new List<PlacedPart>
            {
                Place(cap, new Vector3(0.02f, 0.88f, 0f), new Vector3(0f, 0f, 11f), 1f),
                Place(wisps, new Vector3(0f, 0.5f, 0f), Vector3.zero, 1f),
            };
            var recipe = Recipe("LanternSpirit", body, 0.042f, palette, zones, face, idle, parts);
            recipe.glow = 0.55f;
            recipe.spec = 0.9f;
            recipe.triangleBudget = 2200;
            recipe.resolution = 72;
            EditorUtility.SetDirty(recipe);
            Species("LanternSpirit", Element.Neutral, recipe, null, 0);
            AssetDatabase.SaveAssets();
            return recipe;
        }

        static void Budget(CreaturePart part, int resolution, int budget)
        {
            part.resolution = resolution;
            part.triangleBudget = budget;
            EditorUtility.SetDirty(part);
        }

        // ---- scene ----

        [MenuItem("Tools/Pocket Isle/Dialogue/Set Up MR_Main (Spirit + dialogue)", priority = 20)]
        public static void SetUpMain()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Dialogue] Stop Play mode first.");
                return;
            }
            var recipe = WriteSpirit();
            Debug.Log($"[Dialogue] {CreatureBaker.Bake(recipe)}");
            var script = Import();

            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            var explorer = Object.FindFirstObjectByType<GestureTestPiece>();
            var encounters = Object.FindFirstObjectByType<EncounterDirector>();
            var input = Object.FindFirstObjectByType<MicrogestureInput>();
            var handle = Object.FindFirstObjectByType<IslandHandle>();
            var hud = AssetDatabase.LoadAssetAtPath<Material>(HudPath);
            if (island == null || explorer == null || hud == null)
            {
                Debug.LogError("[Dialogue] MR_Main needs the island, the explorer and Materials/Battle/Hud.mat.");
                return;
            }

            var old = island.transform.Find("Lantern Spirit");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var spiritGo = new GameObject("Lantern Spirit");
            spiritGo.transform.SetParent(island.transform, false);
            var spirit = spiritGo.AddComponent<LanternSpirit>();
            spirit.island = island;
            spirit.prefab = recipe.bakedPrefab;

            var oldDialogue = GameObject.Find("Dialogue");
            if (oldDialogue != null) Object.DestroyImmediate(oldDialogue);
            var dialogueGo = new GameObject("Dialogue");
            var bubbleGo = new GameObject("Dialogue Bubble");
            bubbleGo.transform.SetParent(dialogueGo.transform, false);
            var bubble = bubbleGo.AddComponent<DialogueBubble>();
            bubble.hudMaterial = hud;
            bubble.grabGuard = handle;
            var dialogue = dialogueGo.AddComponent<Dialogue>();
            dialogue.script = script;
            dialogue.bubble = bubble;
            dialogue.explorer = explorer;
            dialogue.input = input;
            dialogue.encounters = encounters;

            CreatureTools.RefreshLabSpecies();
            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            Debug.Log($"[Dialogue] MR_Main: Lantern Spirit on {island.name}, Dialogue + bubble{(input == null ? " (no MicrogestureInput: idle timer only counts keys)" : "")}.");
        }
    }

    [CustomEditor(typeof(Dialogue))]
    sealed class DialogueEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var d = (Dialogue)target;
            EditorGUILayout.Space();
            if (GUILayout.Button("Re-import Script_v1")) DialogueTools.Import();
            if (d.script == null) return;
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Play mode: fire any trigger from the list below, or press D to cycle through them.", MessageType.None);
                return;
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Idle hint")) d.DebugIdle();
                if (GUILayout.Button("Clear")) d.ClearAll();
            }
            EditorGUILayout.LabelField("Fire (ignores once)", EditorStyles.boldLabel);
            foreach (var t in d.script.triggers)
                if (GUILayout.Button($"{t.id}  ({t.lines.Count}{(t.once ? ", once" : "")})", EditorStyles.miniButton))
                {
                    d.ClearAll();
                    d.Fire(t.id, true);
                }
        }
    }
}
