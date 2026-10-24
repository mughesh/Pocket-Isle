using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Diorama.EditorTools.StarterKit;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Writes the first Cinderwood props (kiln, paper lantern post, brazier cold / lit, berry bush, autumn tree,
    /// fallen log, stone lantern, pot cluster, hot spring steam) and the Cinderwood and Test realm themes.
    /// Realm look (decided Oct 24, step 6 fixes): the unrestored state is the everyday bright look; "not flowing" is shown
    /// by stillness (lights sputter, no embers or steam), not colour loss. Restored is a touch warmer and brighter
    /// (<see cref="Warmer"/>) with glowing leaf tips. <see cref="ApplyRealmLook"/> re-applies that to the existing assets
    /// without touching shapes or meshes.
    /// Concept: Docs/ConceptArt/01_realms_diorama_sheet.png (Cinderwood panel) and Concepts_01_Summary.md.
    /// Shape units are tiles (1 = 5 cm at zoom 1), +Z faces the player. Like the creature kits this only seeds the
    /// assets; tune them in the Inspector afterwards. Re-running overwrites them (same files, references survive).
    /// </summary>
    public static class PropKit
    {
        const string Root = "Assets/_Project/Data/Props";

        [MenuItem("Tools/Pocket Isle/Props/Write Prop Kit (overwrites)", priority = 20)]
        static void Menu()
        {
            if (!EditorUtility.DisplayDialog("Write prop kit",
                    "Overwrites the Cinderwood props and the Cinderwood / Test themes in Data/Props with the defaults from code. " +
                    "Inspector tweaks to those assets are lost.", "Overwrite", "Cancel")) return;
            Write();
        }

        /// <summary>True once the kit has been written (Dress Test World writes it if not).</summary>
        public static bool Exists => AssetDatabase.LoadAssetAtPath<RealmTheme>($"{Root}/Themes/Cinderwood.asset") != null;

        public static PropRecipe Load(string name) => AssetDatabase.LoadAssetAtPath<PropRecipe>($"{Root}/{name}.asset");
        public static RealmTheme Theme(string name) => AssetDatabase.LoadAssetAtPath<RealmTheme>($"{Root}/Themes/{name}.asset");

        // ---- realm look ----

        /// <summary>The step 6 restored look, now the everyday (unrestored) island palette: a little more saturated and brighter.</summary>
        internal static Color Brighter(Color c)
        {
            Color.RGBToHSV(c, out float h, out float sat, out float v);
            var b = Color.HSVToRGB(h, Mathf.Min(1f, sat * 1.1f), Mathf.Min(1f, v * 1.08f));
            b.a = c.a;
            return b;
        }

        /// <summary>Restored colour: about 6% warmer and brighter than the everyday one.</summary>
        internal static Color Warmer(Color c)
        {
            Color.RGBToHSV(c, out float h, out float sat, out float v);
            // A small hue nudge toward orange (0.08) on top of the brightness.
            float hue = Mathf.Repeat(h + Mathf.Clamp(Mathf.DeltaAngle(h * 360f, 29f) / 360f, -0.01f, 0.01f), 1f);
            var w = Color.HSVToRGB(hue, Mathf.Min(1f, sat * 1.06f), Mathf.Min(1f, v * 1.07f));
            w.a = c.a;
            return w;
        }

        /// <summary>A prop colour from its everyday hex (unrestored); restored is <see cref="Warmer"/>.</summary>
        static PropColor C(string hex, float glow = 0f) => new PropColor(Warmer(Hex(hex)), Hex(hex), glow);

        static bool Same(Color a, Color b) => Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) < 0.01f;

        [MenuItem("Tools/Pocket Isle/Props/Apply Realm Look (palettes only)", priority = 22)]
        public static void ApplyRealmLook()
        {
            int props = 0;
            foreach (var r in PropBaker.AllRecipes())
            {
                if (r.palette == null) continue;
                for (int i = 0; i < r.palette.Length; i++)
                {
                    // The everyday colour: already applied (restored = Warmer(unrestored)) keeps unrestored; otherwise
                    // the old restored colour is the everyday one (the step 6 muted unrestored is dropped).
                    var c = r.palette[i];
                    var everyday = Same(Warmer(c.unrestored), c.restored) ? c.unrestored : c.restored;
                    r.palette[i].unrestored = everyday;
                    r.palette[i].restored = Warmer(everyday);
                }
                EditorUtility.SetDirty(r);
                props++;
            }
            var cinder = Theme("Cinderwood");
            if (cinder != null) CinderwoodColors(cinder);
            AssetDatabase.SaveAssets();
            Debug.Log($"[PropKit] Realm look: {props} prop palettes bright when unrestored, a touch warmer when restored; Cinderwood theme " +
                      $"{(cinder != null ? "the same, starts unrestored" : "missing (write the prop kit first)")}.");
        }

        // ---- Cinderwood palette ----
        static readonly PropColor Clay = C("#C27542");
        static readonly PropColor Wood = C("#6B4A33");
        static readonly PropColor Stone = C("#A39A8E");
        static readonly PropColor Iron = C("#3D3633");
        static readonly PropColor Leaf = C("#C4482A");
        static readonly PropColor Moss = C("#6B7A52");
        static readonly PropColor Ember = C("#FFB347", 1f);
        static readonly PropColor Smoke = C("#EDE5D8", 0.2f);

        public static void Write()
        {
            DioramaSceneSetup.EnsureFolder(Root);
            DioramaSceneSetup.EnsureFolder($"{Root}/Shapes");
            DioramaSceneSetup.EnsureFolder($"{Root}/Themes");

            // ---- pottery kiln: beehive dome, chimney, glowing mouth in a stone arch, smoke ----
            var kiln = Recipe("Kiln", new Vector2Int(1, 1), true, false, 1500);
            kiln.palette = Palette(Clay, Wood, Stone, Leaf, Ember, Smoke);
            kiln.yawJitter = 20f;
            kiln.pieces = new List<PropPiece>
            {
                Piece(PShape("Kiln_Body",
                    Ell(new Vector3(0f, 0.36f, 0f), new Vector3(0.38f, 0.42f, 0.38f)),
                    Cone(new Vector3(0.13f, 0.55f, -0.12f), new Vector3(0.16f, 1.0f, -0.15f), 0.12f, 0.085f, 0.12f),
                    Torus(new Vector3(0.16f, 1.0f, -0.15f), 0.08f, 0.035f, Vector3.zero, 0.04f),
                    Sphere(new Vector3(0.16f, 1.08f, -0.15f), 0.065f, 0.02f).Sub(),
                    Ell(new Vector3(0f, 0.18f, 0.36f), new Vector3(0.16f, 0.2f, 0.18f), 0.04f).Sub(),
                    Floor(0f)), PropSlot.Clay),
                Piece(PShape("Kiln_Base",
                    Torus(new Vector3(0f, 0.05f, 0f), 0.36f, 0.07f, Vector3.zero, 0f),
                    Torus(new Vector3(0f, 0.18f, 0.33f), 0.17f, 0.035f, new Vector3(90f, 0f, 0f), 0.02f),
                    Floor(0f)), PropSlot.Stone),
                Piece(PShape("Kiln_Glow",
                    Ell(new Vector3(0f, 0.16f, 0.25f), new Vector3(0.13f, 0.15f, 0.1f))), PropSlot.Glow, PropMotion.Flicker, 0.005f),
                Piece(PShape("Kiln_Smoke", SmokePuff(0.07f)), PropSlot.Accent, PropMotion.Steam, 0.05f, new Vector3(0.16f, 1.12f, -0.15f)),
            };

            // ---- paper lantern on a post with an arm ----
            var lantern = Recipe("LanternPost", new Vector2Int(1, 1), true, false, 900);
            lantern.palette = Palette(Clay, Wood, Stone, Leaf, C("#FFC86B", 1.1f),
                C("#7A2E22"));
            lantern.yawJitter = 35f;
            lantern.pieces = new List<PropPiece>
            {
                Piece(PShape("LanternPost_Wood",
                    Cone(new Vector3(0f, 0f, 0f), new Vector3(0f, 1.12f, 0f), 0.05f, 0.042f, 0f),
                    Cone(new Vector3(0f, 1.08f, 0f), new Vector3(0.27f, 1.1f, 0f), 0.032f, 0.028f, 0.03f),
                    Cone(new Vector3(0.26f, 1.1f, 0f), new Vector3(0.26f, 0.97f, 0f), 0.018f, 0.018f, 0.01f),
                    Ell(new Vector3(0f, 0.02f, 0f), new Vector3(0.1f, 0.05f, 0.1f), 0.06f),
                    Floor(0f)), PropSlot.Wood),
                Piece(PShape("LanternPost_Paper",
                    Ell(new Vector3(0.26f, 0.84f, 0f), new Vector3(0.1f, 0.13f, 0.1f))), PropSlot.Glow, PropMotion.Flicker, 0.004f),
                Piece(PShape("LanternPost_Caps",
                    Ell(new Vector3(0.26f, 0.97f, 0f), new Vector3(0.075f, 0.026f, 0.075f), 0f),
                    Ell(new Vector3(0.26f, 0.71f, 0f), new Vector3(0.065f, 0.024f, 0.065f), 0f)), PropSlot.Accent),
            };

            // ---- charcoal brazier: tripod and bowl; cold (for the temple puzzle) and lit ----
            var brazierFrame = PShape("Brazier_Frame", BrazierFrame());
            var brazierCoals = PShape("Brazier_Coals",
                Ell(new Vector3(0f, 0.5f, 0f), new Vector3(0.19f, 0.05f, 0.19f)).Bumps(0.015f, 30f));
            var brazierFlame = PShape("Brazier_Flame",
                Sphere(new Vector3(0f, 0.6f, 0f), 0.11f),
                Cone(new Vector3(0f, 0.6f, 0f), new Vector3(0f, 0.88f, 0f), 0.1f, 0.015f, 0.1f),
                Cone(new Vector3(0.05f, 0.6f, 0f), new Vector3(0.1f, 0.78f, 0.02f), 0.05f, 0.01f, 0.06f),
                Cone(new Vector3(-0.05f, 0.6f, 0.02f), new Vector3(-0.08f, 0.74f, -0.02f), 0.045f, 0.01f, 0.06f));
            var coldCoal = C("#4A4442");
            var hotEmber = C("#FF8A3D", 1.2f);

            var brazier = Recipe("Brazier", new Vector2Int(1, 1), true, true, 1000);
            brazier.palette = Palette(Clay, Wood, Iron, Leaf, hotEmber, coldCoal);
            brazier.pieces = new List<PropPiece> { Piece(brazierFrame, PropSlot.Stone), Piece(brazierCoals, PropSlot.Accent) };

            var brazierLit = Recipe("BrazierLit", new Vector2Int(1, 1), true, true, 1200);
            brazierLit.palette = Palette(Clay, Wood, Iron, Leaf, hotEmber, coldCoal);
            brazierLit.pieces = new List<PropPiece>
            {
                Piece(brazierFrame, PropSlot.Stone),
                Piece(brazierCoals, PropSlot.Glow),
                Piece(brazierFlame, PropSlot.Glow, PropMotion.Flicker, 0.035f),
            };

            // ---- ember berry bush: three leafy blobs and red berries ----
            var leafPrims = new[]
            {
                Sphere(new Vector3(-0.17f, 0.2f, 0.02f), 0.21f).Bumps(0.02f, 14f),
                Sphere(new Vector3(0.17f, 0.19f, -0.03f), 0.2f, 0.12f).Bumps(0.02f, 14f),
                Sphere(new Vector3(0f, 0.33f, -0.04f), 0.22f, 0.12f).Bumps(0.02f, 14f),
                Sphere(new Vector3(0.02f, 0.2f, 0.16f), 0.17f, 0.12f).Bumps(0.02f, 14f),
                Floor(0f),
            };
            var bush = Recipe("BerryBush", new Vector2Int(1, 1), true, true, 1100);
            bush.palette = Palette(Clay, Wood, Stone, Moss, Ember, C("#E0342A", 0.25f));
            bush.pieces = new List<PropPiece>
            {
                Piece(PShape("BerryBush_Leaves", leafPrims), PropSlot.Leaf),
                Piece(PShape("BerryBush_Berries", Berries(leafPrims, 9)), PropSlot.Accent),
            };

            // ---- autumn tree: round russet canopy with glowing leaf tips ----
            var tree = Recipe("AutumnTree", new Vector2Int(1, 1), true, true, 900);
            tree.palette = Palette(Clay, C("#4A3328"), Stone, Leaf,
                C("#F3A33A", 0.5f), Smoke);
            tree.tips = new PropTips { on = true, onSlot = PropSlot.Leaf, tipSlot = PropSlot.Glow, at = 0.55f };
            tree.scaleJitter = 0.12f;
            tree.pieces = new List<PropPiece>
            {
                Piece(PShape("AutumnTree_Trunk",
                    Cone(new Vector3(0f, 0f, 0f), new Vector3(0f, 0.95f, 0f), 0.085f, 0.055f, 0f),
                    Ell(new Vector3(0f, 0.03f, 0f), new Vector3(0.14f, 0.06f, 0.14f), 0.08f),
                    Cone(new Vector3(0f, 0.62f, 0f), new Vector3(0.2f, 0.92f, 0.06f), 0.04f, 0.03f, 0.06f),
                    Cone(new Vector3(0f, 0.7f, 0f), new Vector3(-0.18f, 0.98f, -0.05f), 0.035f, 0.025f, 0.06f),
                    Floor(0f)), PropSlot.Wood),
                Piece(PShape("AutumnTree_Canopy",
                    Sphere(new Vector3(0f, 1.32f, 0f), 0.4f).Bumps(0.025f, 10f),
                    Sphere(new Vector3(0.3f, 1.18f, 0.08f), 0.28f, 0.14f).Bumps(0.025f, 10f),
                    Sphere(new Vector3(-0.3f, 1.22f, -0.06f), 0.3f, 0.14f).Bumps(0.025f, 10f),
                    Sphere(new Vector3(0.04f, 1.2f, 0.3f), 0.26f, 0.14f).Bumps(0.025f, 10f),
                    Sphere(new Vector3(-0.06f, 1.18f, -0.3f), 0.26f, 0.14f).Bumps(0.025f, 10f),
                    Sphere(new Vector3(0.02f, 1.6f, -0.02f), 0.27f, 0.14f).Bumps(0.025f, 10f)), PropSlot.Leaf, PropMotion.Sway, 0.035f),
            };

            // ---- fallen mossy log, 2x1, blocks the path (the hand puzzle will lift it later) ----
            var log = Recipe("FallenLog", new Vector2Int(2, 1), true, false, 1200);
            log.palette = Palette(Clay, Wood, Stone, Moss, Ember, C("#D9B07A"));
            log.yawJitter = 6f;
            log.scaleJitter = 0.04f;
            log.pieces = new List<PropPiece>
            {
                Piece(PShape("FallenLog_Bark",
                    Cone(new Vector3(-0.85f, 0.19f, 0f), new Vector3(0.85f, 0.18f, 0.02f), 0.2f, 0.18f, 0f).Bumps(0.012f, 18f),
                    Cone(new Vector3(0.25f, 0.27f, 0.06f), new Vector3(0.4f, 0.48f, 0.18f), 0.055f, 0.035f, 0.06f),
                    Plane(new Vector3(0.92f, 0f, 0f), Vector3.right, 0f),
                    Plane(new Vector3(-0.92f, 0f, 0f), Vector3.left, 0f),
                    Floor(0f)), PropSlot.Wood),
                Piece(PShape("FallenLog_Rings",
                    Ell(new Vector3(0.925f, 0.19f, 0.005f), new Vector3(0.012f, 0.16f, 0.16f), 0f).Mirror()), PropSlot.Accent),
                Piece(PShape("FallenLog_Moss",
                    Ell(new Vector3(0.05f, 0.36f, 0f), new Vector3(0.45f, 0.05f, 0.14f)).Bumps(0.012f, 25f),
                    Ell(new Vector3(-0.48f, 0.35f, 0.04f), new Vector3(0.22f, 0.045f, 0.11f), 0.06f).Bumps(0.012f, 25f)), PropSlot.Leaf),
            };

            // ---- stone path lantern ----
            var stoneLantern = Recipe("StoneLantern", new Vector2Int(1, 1), true, false, 900);
            stoneLantern.palette = Palette(Clay, Wood, Stone, Leaf, C("#FFC060", 1f), Smoke);
            stoneLantern.pieces = new List<PropPiece>
            {
                Piece(PShape("StoneLantern_Stone",
                    Box(new Vector3(0f, 0.04f, 0f), new Vector3(0.15f, 0.04f, 0.15f), 0.02f, 0f),
                    Cone(new Vector3(0f, 0.06f, 0f), new Vector3(0f, 0.32f, 0f), 0.065f, 0.055f, 0.03f),
                    Box(new Vector3(0f, 0.34f, 0f), new Vector3(0.13f, 0.025f, 0.13f), 0.015f, 0.02f),
                    Box(new Vector3(0f, 0.46f, 0f), new Vector3(0.1f, 0.1f, 0.1f), 0.015f, 0.01f),
                    Box(new Vector3(0f, 0.46f, 0f), new Vector3(0.055f, 0.055f, 0.2f), 0.01f, 0.01f).Sub(),
                    Box(new Vector3(0f, 0.46f, 0f), new Vector3(0.2f, 0.055f, 0.055f), 0.01f, 0.01f).Sub(),
                    Ell(new Vector3(0f, 0.6f, 0f), new Vector3(0.21f, 0.06f, 0.21f), 0.04f),
                    Cone(new Vector3(0f, 0.62f, 0f), new Vector3(0f, 0.7f, 0f), 0.05f, 0.035f, 0.04f),
                    Sphere(new Vector3(0f, 0.74f, 0f), 0.045f, 0.03f),
                    Floor(0f)), PropSlot.Stone),
                Piece(PShape("StoneLantern_Light",
                    Ell(new Vector3(0f, 0.46f, 0f), new Vector3(0.07f, 0.06f, 0.07f))), PropSlot.Glow, PropMotion.Flicker, 0.002f),
            };

            // ---- small pot cluster (walk-through decor) ----
            var pots = Recipe("PotCluster", new Vector2Int(1, 1), false, true, 900);
            pots.palette = Palette(Clay, Wood, Stone, Leaf, Ember, C("#4A3B34"));
            pots.pieces = new List<PropPiece>
            {
                Piece(PShape("PotCluster_Clay",
                    Ell(new Vector3(-0.14f, 0.15f, 0.06f), new Vector3(0.15f, 0.15f, 0.15f)),
                    Torus(new Vector3(-0.14f, 0.29f, 0.06f), 0.075f, 0.03f, Vector3.zero, 0.04f),
                    Sphere(new Vector3(-0.14f, 0.36f, 0.06f), 0.08f, 0.02f).Sub(),
                    Ell(new Vector3(0.13f, 0.07f, 0.21f), new Vector3(0.07f, 0.07f, 0.07f), 0f),
                    Floor(0f)), PropSlot.Clay),
                Piece(PShape("PotCluster_Glazed",
                    Ell(new Vector3(0.15f, 0.11f, -0.08f), new Vector3(0.12f, 0.11f, 0.12f)),
                    Torus(new Vector3(0.15f, 0.21f, -0.08f), 0.06f, 0.025f, Vector3.zero, 0.04f),
                    Sphere(new Vector3(0.15f, 0.27f, -0.08f), 0.065f, 0.02f).Sub(),
                    Floor(0f)), PropSlot.Accent),
            };

            // ---- hot spring steam (placed by the island on H tiles) ----
            var steam = Recipe("HotSpringSteam", new Vector2Int(1, 1), false, true, 400);
            steam.palette = Palette(Clay, Wood, Stone, Leaf, Ember, C("#F2EDE4", 0.25f));
            steam.outline = 0.015f;
            steam.pieces = new List<PropPiece>
            {
                Piece(PShape("HotSpringSteam_Puff", SmokePuff(0.14f)), PropSlot.Accent, PropMotion.Steam, 0.07f, new Vector3(0f, 0.16f, 0f)),
            };

            WriteHearthShrine();
            WriteThemes(tree);
            AssetDatabase.SaveAssets();
            Debug.Log("[PropKit] Wrote 11 Cinderwood props and the Cinderwood / Test themes to Data/Props. Bake them: Tools > Pocket Isle > Props > Bake All.");
        }

        /// <summary>
        /// Hearth Shrine (step 7): a small stone hearth with a flame in its arched mouth (facing the player, +Z) and a
        /// mossy cap like the Lantern Spirit's, hinting they're linked. Heal / save / swap point.
        /// </summary>
        public static PropRecipe WriteHearthShrine()
        {
            DioramaSceneSetup.EnsureFolder(Root);
            DioramaSceneSetup.EnsureFolder($"{Root}/Shapes");
            var shrine = Recipe("HearthShrine", new Vector2Int(1, 1), true, false, 1500);
            shrine.palette = Palette(Clay, Wood, Stone, C("#7E9468"), C("#FFB04A", 1.2f), Smoke);
            shrine.yawJitter = 0f;
            shrine.scaleJitter = 0f;
            shrine.resolution = 30;
            shrine.pieces = new List<PropPiece>
            {
                Piece(PShape("HearthShrine_Stone",
                    Box(new Vector3(0f, 0.05f, 0f), new Vector3(0.3f, 0.05f, 0.27f), 0.03f, 0f),
                    Box(new Vector3(0f, 0.3f, 0f), new Vector3(0.22f, 0.22f, 0.19f), 0.05f, 0.04f),
                    Ell(new Vector3(0f, 0.22f, 0.2f), new Vector3(0.11f, 0.15f, 0.09f), 0.03f).Sub(),
                    Torus(new Vector3(0f, 0.2f, 0.18f), 0.12f, 0.03f, new Vector3(90f, 0f, 0f), 0.03f),
                    Floor(0f)), PropSlot.Stone),
                Piece(PShape("HearthShrine_Flame",
                    Sphere(new Vector3(0f, 0.15f, 0.15f), 0.075f),
                    Cone(new Vector3(0f, 0.16f, 0.15f), new Vector3(0f, 0.33f, 0.14f), 0.065f, 0.01f, 0.07f),
                    Cone(new Vector3(0.04f, 0.15f, 0.15f), new Vector3(0.07f, 0.26f, 0.16f), 0.035f, 0.008f, 0.05f)),
                    PropSlot.Glow, PropMotion.Flicker, 0.02f),
                Piece(PShape("HearthShrine_Cap",
                    Ell(new Vector3(0f, 0.56f, 0f), new Vector3(0.29f, 0.16f, 0.26f)),
                    Plane(new Vector3(0f, 0.5f, 0f), Vector3.down, 0.02f)), PropSlot.Leaf, PropMotion.Sway, 0.01f),
                Piece(PShape("HearthShrine_Tip",
                    Sphere(new Vector3(0f, 0.74f, 0f), 0.05f, 0f)), PropSlot.Glow, PropMotion.Flicker, 0.004f),
            };
            EditorUtility.SetDirty(shrine);
            return shrine;
        }

        // ---- themes ----

        static void WriteThemes(PropRecipe autumnTree)
        {
            var cinder = Asset<RealmTheme>($"{Root}/Themes/Cinderwood.asset");
            CinderwoodColors(cinder);
            cinder.treeProp = autumnTree;

            // Test: the island materials' current colours; unrestored = the same, mostly greyed.
            var test = Asset<RealmTheme>($"{Root}/Themes/Test.asset");
            Color M(string mat, string prop = "_BaseColor")
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>($"Assets/_Project/Materials/Island/{mat}.mat");
                return m != null ? m.GetColor(prop) : Color.magenta;
            }
            var water = M("Island_CapWater");
            test.restored = new ThemeColors
            {
                grass = M("Island_CapGrass"), tallGrass = M("Island_CapTallGrass"), path = M("Island_CapPath"),
                water = water, hotSpring = Color.Lerp(water, Hex("#7FE0C8"), 0.5f),
                shortBase = M("Island_Grass", "_ShortBase"), shortTip = M("Island_Grass", "_ShortTip"),
                tallBase = M("Island_Grass", "_TallBase"), tallTip = M("Island_Grass", "_TallTip"),
                dirt = M("Island_Column"), underside = M("Island_Underside"),
                trunk = M("Tree_Trunk"), leavesLow = M("Tree_LeavesLow"), leavesHigh = M("Tree_LeavesHigh"), rock = M("Rock"),
                leafGlow = 0f, outlineTint = Color.white,
            };
            test.unrestored = Greyed(test.restored, 0.75f);
            test.treeProp = null;
            test.startRestored = 1f;
            EditorUtility.SetDirty(test);
        }

        /// <summary>
        /// Cinderwood island colours: unrestored = the bright step 6 restored palette (the state players spend most time in,
        /// so the scene starts there), restored = a touch warmer with glowing leaf tips.
        /// </summary>
        static void CinderwoodColors(RealmTheme cinder)
        {
            var baseLook = new ThemeColors
            {
                grass = Hex("#C7863A"), tallGrass = Hex("#A9482B"), path = Hex("#E2C79B"),
                water = Hex("#4E9DB0"), hotSpring = Hex("#5FC4B4"),
                shortBase = Hex("#9C5A24"), shortTip = Hex("#E9B35A"), tallBase = Hex("#7E2E1C"), tallTip = Hex("#E5641E"),
                dirt = Hex("#8A5634"), underside = Hex("#5E534D"),
                trunk = Hex("#4A3328"), leavesLow = Hex("#B5452A"), leavesHigh = Hex("#D98E32"), rock = Hex("#9A9086"),
                leafGlow = 0.12f, outlineTint = Color.white,
            };
            var everyday = Map(baseLook, Brighter, 0f);
            cinder.unrestored = everyday;
            cinder.restored = Map(everyday, Warmer, 0.35f);
            cinder.startRestored = 0f;
            EditorUtility.SetDirty(cinder);
        }

        static ThemeColors Map(ThemeColors c, System.Func<Color, Color> f, float leafGlow) => new ThemeColors
        {
            grass = f(c.grass), tallGrass = f(c.tallGrass), path = f(c.path), water = f(c.water), hotSpring = f(c.hotSpring),
            shortBase = f(c.shortBase), shortTip = f(c.shortTip), tallBase = f(c.tallBase), tallTip = f(c.tallTip),
            dirt = f(c.dirt), underside = f(c.underside), trunk = f(c.trunk), leavesLow = f(c.leavesLow),
            leavesHigh = f(c.leavesHigh), rock = f(c.rock), leafGlow = leafGlow, outlineTint = c.outlineTint,
        };

        static ThemeColors Greyed(ThemeColors c, float amount)
        {
            Color G(Color x)
            {
                float l = x.grayscale;
                return Color.Lerp(x, new Color(l, l, l, x.a), amount);
            }
            return new ThemeColors
            {
                grass = G(c.grass), tallGrass = G(c.tallGrass), path = G(c.path), water = G(c.water), hotSpring = G(c.hotSpring),
                shortBase = G(c.shortBase), shortTip = G(c.shortTip), tallBase = G(c.tallBase), tallTip = G(c.tallTip),
                dirt = G(c.dirt), underside = G(c.underside), trunk = G(c.trunk), leavesLow = G(c.leavesLow),
                leavesHigh = G(c.leavesHigh), rock = G(c.rock), leafGlow = 0f, outlineTint = c.outlineTint,
            };
        }

        // ---- shape helpers ----

        static SdfPrimitive[] SmokePuff(float r) => new[]
        {
            Sphere(Vector3.zero, r),
            Sphere(new Vector3(r * 0.7f, r * 0.5f, r * 0.1f), r * 0.7f, r * 0.6f),
            Sphere(new Vector3(-r * 0.6f, r * 0.6f, -r * 0.2f), r * 0.65f, r * 0.6f),
            Sphere(new Vector3(r * 0.1f, r * 1.2f, 0f), r * 0.6f, r * 0.6f),
        };

        static SdfPrimitive[] BrazierFrame()
        {
            var prims = new List<SdfPrimitive>();
            foreach (float a in new[] { 90f, 210f, 330f })
            {
                var d = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                prims.Add(Cone(d * 0.22f, d * 0.08f + Vector3.up * 0.44f, 0.03f, 0.025f, prims.Count == 0 ? 0f : 0.03f));
            }
            prims.Add(Ell(new Vector3(0f, 0.46f, 0f), new Vector3(0.25f, 0.13f, 0.25f), 0.03f));
            prims.Add(Sphere(new Vector3(0f, 0.63f, 0f), 0.23f, 0.02f).Sub());
            prims.Add(Torus(new Vector3(0f, 0.52f, 0f), 0.235f, 0.025f, Vector3.zero, 0.02f));
            prims.Add(Floor(0f));
            return prims.ToArray();
        }

        /// <summary>Small spheres stuck onto the outside of a shape, spread over its upper half (golden angle).</summary>
        static SdfPrimitive[] Berries(SdfPrimitive[] onto, int count)
        {
            var eval = new SdfEvaluator(onto);
            var center = new Vector3(0f, 0.22f, 0f);
            var list = new List<SdfPrimitive>();
            for (int i = 0; i < count; i++)
            {
                float y = 0.85f - 0.75f * (i + 0.5f) / count;
                float r = Mathf.Sqrt(1f - y * y);
                float a = i * 2.39996f;
                var dir = new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
                if (!eval.Surface(center + dir * 1.2f, -dir, out var p, out var n)) continue;
                list.Add(Sphere(p + n * 0.012f, 0.042f, 0f));
            }
            return list.ToArray();
        }

        static SdfPrimitive Box(Vector3 c, Vector3 half, float round, float k, Vector3 rotation = default) =>
            new SdfPrimitive { kind = SdfKind.RoundBox, a = c, size = half, b = new Vector3(round, 0f, 0f), rotation = rotation, k = k };

        static SdfPrimitive Plane(Vector3 point, Vector3 normal, float k) =>
            new SdfPrimitive { kind = SdfKind.Plane, blend = SdfBlend.Intersect, a = point, b = normal, k = k };

        // ---- asset helpers ----

        static SdfShape PShape(string name, params SdfPrimitive[] prims)
        {
            var s = Asset<SdfShape>($"{Root}/Shapes/{name}.asset");
            s.primitives = new List<SdfPrimitive>(prims);
            EditorUtility.SetDirty(s);
            return s;
        }

        static PropRecipe Recipe(string name, Vector2Int footprint, bool blocks, bool randomYaw, int budget)
        {
            var r = Asset<PropRecipe>($"{Root}/{name}.asset");
            r.footprint = footprint;
            r.blocksWalking = blocks;
            r.randomYaw = randomYaw;
            r.yawJitter = 15f;
            r.scaleJitter = 0.08f;
            r.triangleBudget = budget;
            r.resolution = 40;
            r.spec = 0.15f;
            r.outline = 0.022f;
            r.tips = default;
            EditorUtility.SetDirty(r);
            return r;
        }

        static PropPiece Piece(SdfShape shape, PropSlot slot, PropMotion motion = PropMotion.None, float amount = 0f, Vector3 position = default) =>
            new PropPiece { shape = shape, slot = slot, motion = motion, motionAmount = amount, position = position, scale = 1f };

        static PropColor[] Palette(PropColor clay, PropColor wood, PropColor stone, PropColor leaf, PropColor glow, PropColor accent) =>
            new[] { clay, wood, stone, leaf, glow, accent };
    }
}
