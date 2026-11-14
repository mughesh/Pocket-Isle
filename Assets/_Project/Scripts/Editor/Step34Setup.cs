using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static Diorama.EditorTools.StarterKit;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 34: a hand moment to wake each Keeper. Bridge (re-run safe):
    /// exec:Diorama.EditorTools.Step34Setup.Bakes   the two "before" bodies (Hearthstag_Kneel, Tidemother_Shell)
    /// exec:Diorama.EditorTools.Step34Setup.Lines   the two lesson lines in Script_v1.asset (the .md is edited by hand)
    /// exec:Diorama.EditorTools.Step34Setup.Scene   the two pieces in MR_Main, wired to their halls (saves the scene)
    /// exec:Diorama.EditorTools.Step34Setup.All
    /// </summary>
    public static class Step34Setup
    {
        const string Root = "Assets/_Project/Data/Creatures";
        public const string KneelName = "Hearthstag_Kneel", ShellName = "Tidemother_Shell";
        public const string KindleId = "cw.kindle", ShellId = "lm.shell";
        public const string KindleLine = "cw.keeper.kindle", ShellLine = "lm.keeper.shell";
        const string ScriptPath = "Assets/_Project/Data/Dialogue/Script_v1.asset";

        [MenuItem("Tools/Pocket Isle/Setup/Step 34/All (bakes, lines, scene)", priority = 934)]
        public static void All()
        {
            Bakes();
            Lines();
            Scene();
        }

        // ================= the "before" bodies =================

        [MenuItem("Tools/Pocket Isle/Setup/Step 34/Bake the before bodies", priority = 935)]
        public static void Bakes()
        {
            var log = new StringBuilder("[Step34Setup] Before bodies:");
            log.Append("\n  ").Append(Bake(Kneel(), "Hearthstag"));
            log.Append("\n  ").Append(Bake(Shell(), "Tidemother"));
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }

        static CreatureRecipe Load(string name) => AssetDatabase.LoadAssetAtPath<CreatureRecipe>($"{Root}/Recipes/{name}.asset");

        /// <summary>A copy of <paramref name="of"/>'s recipe under a new name (same palette, parts, budgets), to re-shape.</summary>
        static CreatureRecipe Variant(string name, string of)
        {
            var stand = Load(of);
            if (stand == null)
            {
                Debug.LogError($"[Step34Setup] {of}'s recipe is missing.");
                return null;
            }
            var r = Asset<CreatureRecipe>($"{Root}/Recipes/{name}.asset");
            var prefab = r.bakedPrefab;
            EditorUtility.CopySerialized(stand, r);
            r.name = name;
            r.bakedPrefab = prefab;
            r.parts = new List<PlacedPart>(stand.parts);
            return r;
        }

        /// <summary>
        /// Hearthstag kneeling like a hurt deer: the barrel resting on the floor, front legs folded (knees forward, hooves
        /// tucked back beside the chest), hind legs tucked along the flanks, the neck low and forward, the head down.
        /// The crown, ears and tail follow the head. Hooves are painted (the sock line no longer fits).
        /// </summary>
        static CreatureRecipe Kneel()
        {
            var r = Variant(KneelName, "Hearthstag");
            if (r == null) return null;
            // Standing head (0, 0.88, 0.60) -> kneeling head (0, 0.38, 0.92): low, chin forward.
            var head = new Vector3(0f, -0.5f, 0.32f);
            r.body = Shape(KneelName + "_Body",
                Ell(new Vector3(0f, 0.02f, -0.08f), new Vector3(0.5f, 0.42f, 0.64f)),
                Ell(new Vector3(0f, 0.1f, 0.34f), new Vector3(0.45f, 0.42f, 0.36f), 0.18f),
                Cone(new Vector3(0f, 0.22f, 0.5f), new Vector3(0f, 0.32f, 0.84f), 0.25f, 0.21f, 0.12f),
                Ell(new Vector3(0f, 0.88f, 0.6f) + head, new Vector3(0.3f, 0.28f, 0.29f), 0.1f),
                Ell(new Vector3(0f, 0.75f, 0.85f) + head, new Vector3(0.16f, 0.13f, 0.14f), 0.08f),
                // Front legs: shoulder forward to the knee on the floor, then the shin folded back beside the chest.
                Cone(new Vector3(0.3f, -0.14f, 0.34f), new Vector3(0.35f, -0.29f, 0.7f), 0.16f, 0.15f, 0.08f).Mirror(),
                Cone(new Vector3(0.35f, -0.29f, 0.7f), new Vector3(0.5f, -0.31f, 0.42f), 0.14f, 0.13f, 0.06f).Mirror(),
                // Hind legs: a haunch on the flank, the shin lying forward along the floor.
                Ell(new Vector3(0.4f, -0.12f, -0.42f), new Vector3(0.2f, 0.27f, 0.3f), 0.1f).Mirror(),
                Cone(new Vector3(0.5f, -0.3f, -0.5f), new Vector3(0.53f, -0.31f, -0.1f), 0.13f, 0.13f, 0.06f).Mirror(),
                Floor(-0.42f));
            var z = r.zones;
            z.socks = false;
            z.patches = new List<PatternPatch>
            {
                new PatternPatch { center = new Vector3(0f, 0.04f, 0.7f), radius = 0.3f, color = PaletteSlot.Belly },
                new PatternPatch { center = new Vector3(0f, -0.36f, 0.02f), radius = 0.24f, color = PaletteSlot.Belly },
                new PatternPatch { center = new Vector3(0f, 0.76f, 0.97f) + head, radius = 0.12f, color = PaletteSlot.Belly },
                // The hooves: the front ones tucked by the chest, the hind ones at the shins' ends.
                new PatternPatch { center = new Vector3(0.52f, -0.33f, 0.4f), radius = 0.15f, color = PaletteSlot.Socks, mirrorX = true },
                new PatternPatch { center = new Vector3(0.54f, -0.33f, -0.06f), radius = 0.15f, color = PaletteSlot.Socks, mirrorX = true },
            };
            r.zones = z;
            var f = r.face;
            f.eyeHeight += head.y;
            f.decals = new List<FaceDecal>(f.decals);
            for (int i = 0; i < f.decals.Count; i++)
            {
                var d = f.decals[i];
                d.at.y += head.y;
                f.decals[i] = d;
            }
            r.face = f;
            // Parts 0-2 crystals, 3 ears, 4 tail (KeeperKit): the head's pieces move with it; the ears droop, the tail hangs.
            for (int i = 0; i < r.parts.Count; i++)
            {
                var p = r.parts[i];
                bool tail = p.part != null && p.part.name == "StagTail";
                if (!tail) p.position += head;
                if (p.part != null && p.part.name == "StagEar") p.rotation = new Vector3(0f, 0f, -12f);
                if (tail) p.rotation = new Vector3(-70f, 0f, 0f);
                r.parts[i] = p;
            }
            EditorUtility.SetDirty(r);
            return r;
        }

        /// <summary>
        /// Tidemother withdrawn into its shell: the dome and plastron only, a dark opening where the neck went in, the
        /// flippers pulled in small under the rim, no tail. The lily pads and the lotus stay on the shell.
        /// </summary>
        static CreatureRecipe Shell()
        {
            var r = Variant(ShellName, "Tidemother");
            if (r == null) return null;
            r.body = Shape(ShellName + "_Body",
                Ell(new Vector3(0f, 0.05f, -0.06f), new Vector3(0.62f, 0.42f, 0.62f), 0f),
                Ell(new Vector3(0f, -0.2f, -0.04f), new Vector3(0.58f, 0.16f, 0.56f), 0.1f),
                // The collar the neck went into.
                Ell(new Vector3(0f, -0.1f, 0.5f), new Vector3(0.2f, 0.13f, 0.12f), 0.08f),
                Floor(-0.34f));
            var z = r.zones;
            z.patches = new List<PatternPatch>
            {
                new PatternPatch { center = new Vector3(0f, 0.62f, -0.06f), radius = 0.78f, color = PaletteSlot.Glow },
                new PatternPatch { center = new Vector3(0f, -0.28f, 0.12f), radius = 0.42f, color = PaletteSlot.Belly },
                new PatternPatch { center = new Vector3(0f, -0.1f, 0.64f), radius = 0.13f, color = PaletteSlot.Face },
            };
            r.zones = z;
            // Parts (TidemotherKit): 0 flippers, 1 back flippers, 2-4 pads, 5 lotus (on pad 4), 6 tail.
            for (int i = r.parts.Count - 1; i >= 0; i--)
            {
                var p = r.parts[i];
                string n = p.part != null ? p.part.name : "";
                if (n == "TurtleTail")
                {
                    r.parts.RemoveAt(i); // the last one: no parent index shifts
                    continue;
                }
                if (n == "TurtleFlipper")
                {
                    p.position = new Vector3(0.4f, -0.22f, 0.3f);
                    p.scale = Vector3.one * 0.6f;
                }
                else if (n == "TurtleBackFlipper")
                {
                    p.position = new Vector3(0.38f, -0.23f, -0.42f);
                    p.scale = Vector3.one * 0.55f;
                }
                r.parts[i] = p;
            }
            EditorUtility.SetDirty(r);
            return r;
        }

        /// <summary>Bakes <paramref name="r"/> at the same model scale as the standing <paramref name="of"/> (its height follows from its own bounds).</summary>
        static string Bake(CreatureRecipe r, string of)
        {
            if (r == null) return "skipped (no recipe)";
            var stand = Load(of);
            var standView = stand != null && stand.bakedPrefab != null ? stand.bakedPrefab.GetComponent<CreatureView>() : null;
            var report = CreatureBaker.Bake(r);
            var view = r.bakedPrefab != null ? r.bakedPrefab.GetComponent<CreatureView>() : null;
            if (view == null || standView == null) return $"{r.name}: baked, but no view to match the scale ({report})";
            float unit = view.height / Mathf.Max(1e-6f, view.rigScale);
            float want = standView.rigScale * unit;
            if (Mathf.Abs(want - r.height) > 1e-5f)
            {
                r.height = want;
                EditorUtility.SetDirty(r);
                report = CreatureBaker.Bake(r);
                view = r.bakedPrefab.GetComponent<CreatureView>();
            }
            return $"{report}\n    height {r.height * 100f:0.0} cm (standing {stand.height * 100f:0.0} cm), model scale {view.rigScale:0.0000} (standing {standView.rigScale:0.0000})";
        }

        /// <summary>
        /// exec:Diorama.EditorTools.Step34Setup.Pictures: the standing and the before body side by side, seen from the seat
        /// (about 40 degrees down) and from the side, to Logs/Step34/&lt;name&gt;_seat.png / _side.png.
        /// </summary>
        public static void Pictures()
        {
            System.IO.Directory.CreateDirectory("Logs/Step34");
            var log = new StringBuilder("[Step34Setup] Pictures:");
            foreach (var (before, standing) in new[] { (KneelName, "Hearthstag"), (ShellName, "Tidemother") })
            {
                var a = Load(standing);
                var b = Load(before);
                if (a == null || b == null || a.bakedPrefab == null || b.bakedPrefab == null) continue;
                var root = new GameObject("Step34 Picture") { hideFlags = HideFlags.DontSave };
                root.transform.position = new Vector3(0f, -50f, 0f);
                float gap = a.height * 1.4f;
                foreach (var (prefab, x) in new[] { (a.bakedPrefab, -gap * 0.5f), (b.bakedPrefab, gap * 0.5f) })
                {
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    go.hideFlags = HideFlags.DontSave;
                    go.transform.SetParent(root.transform, false);
                    go.transform.localPosition = new Vector3(x, 0f, 0f);
                    var view = go.GetComponent<CreatureView>();
                    if (view != null) view.ApplyStyles();
                }
                var camGo = new GameObject("Step34 Camera") { hideFlags = HideFlags.DontSave };
                var cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.16f, 0.17f, 0.2f);
                cam.fieldOfView = 30f;
                cam.nearClipPlane = 0.01f;
                var lightGo = new GameObject("Step34 Light") { hideFlags = HideFlags.DontSave };
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.2f;
                lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                var rt = new RenderTexture(1024, 640, 24);
                cam.targetTexture = rt;
                Vector3 mid = root.transform.position + Vector3.up * a.height * 0.45f;
                float dist = gap * 3.2f;
                foreach (var (tag, pitch, yaw) in new[] { ("seat", 40f, 0f), ("side", 8f, 90f) })
                {
                    var rot = Quaternion.Euler(pitch, 180f + yaw, 0f);
                    camGo.transform.position = mid - rot * Vector3.forward * dist;
                    camGo.transform.rotation = rot;
                    cam.Render();
                    RenderTexture.active = rt;
                    var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                    tex.Apply();
                    RenderTexture.active = null;
                    string path = $"Logs/Step34/{before}_{tag}.png";
                    System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
                    Object.DestroyImmediate(tex);
                    log.Append($"\n  {path}");
                }
                cam.targetTexture = null;
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(camGo);
                Object.DestroyImmediate(lightGo);
                Object.DestroyImmediate(root);
            }
            Debug.Log(log.ToString());
        }

        // ================= the lesson lines =================

        public static void Lines()
        {
            var script = AssetDatabase.LoadAssetAtPath<DialogueScript>(ScriptPath);
            if (script == null)
            {
                Debug.LogError("[Step34Setup] Script_v1.asset is missing.");
                return;
            }
            var so = new SerializedObject(script);
            var triggers = so.FindProperty("triggers");
            var log = new StringBuilder("[Step34Setup] Lines:");
            Line(triggers, "lm.keeper.meet", ShellLine, "puzzle:" + ShellId, "lesson in the lake temple: Tidemother is inside its shell; chip \"Knock on its shell\"",
                "It's hiding in its shell. Knock on it!", log);
            Line(triggers, "cinder.keeper.first", KindleLine, "puzzle:" + KindleId, "lesson in the Forge Hall: Hearthstag kneels, its crown fire nearly out; chip \"Cup the fire\"",
                "Its fire is dying! Cup your hands round it.", log);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(script);
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }

        /// <summary>A copy of Galeowl's knots lesson (an objective line in a hall) under a new id, placed before <paramref name="before"/>.</summary>
        static void Line(SerializedProperty triggers, string before, string id, string target, string note, string text, StringBuilder log)
        {
            int template = -1, at = -1, have = -1;
            for (int i = 0; i < triggers.arraySize; i++)
            {
                string tid = triggers.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue;
                if (tid == "gw.keeper.knots") template = i;
                if (tid == before) at = i;
                if (tid == id) have = i;
            }
            if (template < 0 || at < 0)
            {
                log.Append($"\n  {id}: NOT added (template gw.keeper.knots {template}, {before} {at})");
                return;
            }
            SerializedProperty e;
            if (have >= 0) e = triggers.GetArrayElementAtIndex(have);
            else
            {
                // Duplicate the template, then move the copy in front of the Keeper's meeting line.
                triggers.InsertArrayElementAtIndex(template);
                int copy = template + 1;
                if (at > template) at++;
                triggers.MoveArrayElement(copy, at > copy ? at - 1 : at);
                e = triggers.GetArrayElementAtIndex(at > copy ? at - 1 : at);
            }
            e.FindPropertyRelative("id").stringValue = id;
            e.FindPropertyRelative("target").stringValue = target;
            e.FindPropertyRelative("note").stringValue = note;
            var lines = e.FindPropertyRelative("lines");
            lines.arraySize = 1;
            lines.GetArrayElementAtIndex(0).FindPropertyRelative("text").stringValue = text;
            log.Append($"\n  {id} ({(have >= 0 ? "updated" : "added")}): \"{text}\"");
        }

        // ================= the scene =================

        public static void Scene()
        {
            var log = new StringBuilder("[Step34Setup] Scene:");
            var guard = Object.FindFirstObjectByType<IslandHandle>(FindObjectsInactive.Include);
            var tide = KeeperHall.FindById("lm.keeper");
            var stag = KeeperHall.FindById(KeeperHall.KeeperId);
            if (tide == null || stag == null)
            {
                Debug.LogError("[Step34Setup] The Lilymere or the Cinderwood Keeper hall is missing from the open scene.");
                return;
            }

            var shell = Piece<ShellKnockPiece>(tide, "Keeper Shell Knock", ShellId, guard, Load(ShellName));
            shell.lessonLine = ShellLine;
            shell.lessonChip = "Knock on its shell";
            shell.ghostClip = "poke";
            shell.ghostScale = 0.65f;
            EditorUtility.SetDirty(shell);
            log.Append($"\n  '{shell.name}' {shell.id} on {tide.name}: before body {(shell.beforePrefab != null ? shell.beforePrefab.name : "MISSING")}, ghost {shell.ghostClip}");

            var kindle = Piece<CrownKindlePiece>(stag, "Keeper Crown Kindle", KindleId, guard, Load(KneelName));
            kindle.lessonLine = KindleLine;
            kindle.lessonChip = "Cup the fire";
            kindle.ghostClip = "cup_hands";
            // R7: the cupping ghost holds the crown only (at 1 it hid the whole kneeling stag from the seat).
            kindle.ghostScale = 0.55f;
            kindle.ringMaterial = stag.beaconMaterial;
            EditorUtility.SetDirty(kindle);
            Undo.RecordObject(stag, "Step 34");
            stag.calmCrownFlame = true;
            EditorUtility.SetDirty(stag);
            log.Append($"\n  '{kindle.name}' {kindle.id} on {stag.name}: before body {(kindle.beforePrefab != null ? kindle.beforePrefab.name : "MISSING")}, ghost {kindle.ghostClip}, " +
                       $"ring {(kindle.ringMaterial != null ? kindle.ringMaterial.name : "MISSING")}; the calmed crown keeps a steady flame");

            var scene = stag.gameObject.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log(log.ToString());
        }

        static T Piece<T>(KeeperHall hall, string name, string id, IslandHandle guard, CreatureRecipe before) where T : KeeperWakePiece
        {
            var parent = hall.transform.parent;
            Transform t = parent != null ? parent.Find(name) : null;
            if (t == null)
            {
                var found = GameObject.Find(name);
                t = found != null ? found.transform : null;
            }
            if (t == null)
            {
                var go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, name);
                go.transform.SetParent(parent, false);
                t = go.transform;
            }
            var p = t.GetComponent<T>();
            if (p == null) p = Undo.AddComponent<T>(t.gameObject);
            Undo.RecordObject(p, "Step 34");
            p.id = id;
            p.map = hall.hall;
            p.island = hall.island;
            p.window = hall.island != null ? hall.island.GetComponent<WorldWindow>() : null;
            p.explorer = hall.explorer;
            p.grabGuard = guard;
            p.ghosts = hall.ghosts;
            p.fx = hall.fx;
            p.reach = 12;
            p.keeper = hall;
            p.swap = hall.swap;
            p.beforePrefab = before != null ? before.bakedPrefab : null;
            Undo.RecordObject(hall, "Step 34");
            hall.wake = p;
            EditorUtility.SetDirty(hall);
            return p;
        }
    }
}
