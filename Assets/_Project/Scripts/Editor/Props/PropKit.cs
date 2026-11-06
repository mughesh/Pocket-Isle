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
    public static partial class PropKit
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

            // ---- autumn tree (step 9a: stacked blobs, red-orange to gold; PropKit.Cinderwood) ----
            var tree = AutumnTree();

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
        /// Hearth Shrine (step 7b): 2 x 2 tiles, a stone hut with an arched doorway (warm light inside, facing the
        /// player, +Z), a mossy domed roof like the Lantern Spirit's cap, and a stone bowl on top holding the hearth
        /// flame. The flame beacon above it is drawn at runtime (HearthShrines). Leads into the Hearth Lodge.
        /// </summary>
        public static PropRecipe WriteHearthShrine()
        {
            DioramaSceneSetup.EnsureFolder(Root);
            DioramaSceneSetup.EnsureFolder($"{Root}/Shapes");
            var shrine = Recipe("HearthShrine", new Vector2Int(2, 2), true, false, 2600);
            shrine.palette = Palette(Clay, Wood, Stone, C("#7E9468"), C("#FFB04A", 1.2f), Smoke);
            shrine.yawJitter = 0f;
            shrine.scaleJitter = 0f;
            shrine.resolution = 40;
            shrine.pieces = new List<PropPiece>
            {
                Piece(PShape("HearthShrine_Stone",
                    Box(new Vector3(0f, 0.05f, 0f), new Vector3(0.88f, 0.05f, 0.8f), 0.04f, 0f),
                    Box(new Vector3(0f, 0.42f, -0.04f), new Vector3(0.66f, 0.38f, 0.56f), 0.12f, 0.05f),
                    Ell(new Vector3(0f, 0.34f, 0.5f), new Vector3(0.24f, 0.32f, 0.22f), 0.03f).Sub(),
                    Box(new Vector3(0f, 0.14f, 0.5f), new Vector3(0.24f, 0.14f, 0.22f), 0.02f, 0.02f).Sub(),
                    Torus(new Vector3(0f, 0.36f, 0.52f), 0.28f, 0.045f, new Vector3(90f, 0f, 0f), 0.03f),
                    Cone(new Vector3(0f, 0.95f, 0f), new Vector3(0f, 1.3f, 0f), 0.13f, 0.1f, 0.05f),
                    Ell(new Vector3(0f, 1.32f, 0f), new Vector3(0.26f, 0.09f, 0.26f), 0.04f),
                    Torus(new Vector3(0f, 1.39f, 0f), 0.22f, 0.055f, Vector3.zero, 0.03f),
                    Sphere(new Vector3(0f, 1.44f, 0f), 0.2f, 0.02f).Sub(),
                    Floor(0f)), PropSlot.Stone),
                Piece(PShape("HearthShrine_Cap",
                    Ell(new Vector3(0f, 0.82f, -0.04f), new Vector3(0.78f, 0.34f, 0.68f)),
                    Plane(new Vector3(0f, 0.74f, 0f), Vector3.down, 0.03f)), PropSlot.Leaf, PropMotion.Sway, 0.008f),
                Piece(PShape("HearthShrine_Door",
                    Ell(new Vector3(0f, 0.3f, 0.36f), new Vector3(0.2f, 0.28f, 0.08f))), PropSlot.Glow, PropMotion.Flicker, 0.003f),
                Piece(PShape("HearthShrine_Flame",
                    Sphere(new Vector3(0f, 1.42f, 0f), 0.15f),
                    Cone(new Vector3(0f, 1.44f, 0f), new Vector3(0f, 1.82f, 0f), 0.13f, 0.015f, 0.08f),
                    Cone(new Vector3(0.08f, 1.43f, 0.02f), new Vector3(0.14f, 1.66f, 0.04f), 0.06f, 0.01f, 0.06f),
                    Cone(new Vector3(-0.07f, 1.43f, -0.03f), new Vector3(-0.12f, 1.62f, -0.05f), 0.055f, 0.01f, 0.06f)),
                    PropSlot.Glow, PropMotion.Flicker, 0.03f),
            };
            // The 1 x 1 step 7 shrine's extra piece.
            if (AssetDatabase.LoadAssetAtPath<SdfShape>($"{Root}/Shapes/HearthShrine_Tip.asset") != null)
                AssetDatabase.DeleteAsset($"{Root}/Shapes/HearthShrine_Tip.asset");
            EditorUtility.SetDirty(shrine);
            return shrine;
        }

        /// <summary>
        /// Hearth Lodge props (step 7b, interior): wall arch segment, big lit hearth (2 x 1), honeycomb pod wall (2 x 1,
        /// six cells; LodgeRoom puts sleeping pods in them, keep the cell offsets in sync), rug (2 x 2), cushion,
        /// inspect pedestal, door archway (2 x 1). Warm wood, cream and ember light. Returns them by name.
        /// </summary>
        public static Dictionary<string, PropRecipe> WriteLodgeProps()
        {
            DioramaSceneSetup.EnsureFolder(Root);
            DioramaSceneSetup.EnsureFolder($"{Root}/Shapes");
            var cream = C("#D8BE96");
            var oak = C("#7A5236");
            var stone = C("#B8AA96");
            var ember = C("#FFB04A", 1.2f);
            var all = new Dictionary<string, PropRecipe>();

            var wall = Recipe("LodgeWall", new Vector2Int(1, 1), true, false, 700);
            wall.palette = Palette(cream, oak, stone, Moss, ember, C("#8E6E54"));
            wall.yawJitter = 0f;
            wall.scaleJitter = 0f;
            wall.pieces = new List<PropPiece>
            {
                Piece(PShape("LodgeWall_Stone",
                    Box(new Vector3(0f, 0.62f, 0f), new Vector3(0.5f, 0.62f, 0.13f), 0.03f, 0f),
                    Ell(new Vector3(0f, 0.6f, 0.13f), new Vector3(0.27f, 0.4f, 0.06f), 0.02f).Sub(),
                    Floor(0f)), PropSlot.Stone),
                Piece(PShape("LodgeWall_Beam",
                    Box(new Vector3(0f, 1.27f, 0f), new Vector3(0.52f, 0.05f, 0.17f), 0.015f, 0f)), PropSlot.Wood),
            };
            all["LodgeWall"] = wall;

            var hearth = Recipe("LodgeHearth", new Vector2Int(2, 1), true, false, 2200);
            hearth.palette = Palette(cream, oak, stone, Moss, ember, C("#5A3A26"));
            hearth.yawJitter = 0f;
            hearth.scaleJitter = 0f;
            hearth.pieces = new List<PropPiece>
            {
                Piece(PShape("LodgeHearth_Stone",
                    Box(new Vector3(0f, 0.55f, -0.08f), new Vector3(0.92f, 0.55f, 0.34f), 0.08f, 0f),
                    Ell(new Vector3(0f, 0.34f, 0.26f), new Vector3(0.46f, 0.32f, 0.3f), 0.03f).Sub(),
                    Box(new Vector3(0f, 1.38f, -0.18f), new Vector3(0.36f, 0.32f, 0.2f), 0.05f, 0.04f),
                    Floor(0f)), PropSlot.Stone),
                Piece(PShape("LodgeHearth_Mantel",
                    Box(new Vector3(0f, 1.03f, 0.06f), new Vector3(1.0f, 0.055f, 0.3f), 0.02f, 0f)), PropSlot.Wood),
                Piece(PShape("LodgeHearth_Logs",
                    Cone(new Vector3(-0.3f, 0.08f, 0.12f), new Vector3(0.3f, 0.08f, 0.2f), 0.06f, 0.06f, 0f),
                    Cone(new Vector3(0.28f, 0.1f, 0.1f), new Vector3(-0.25f, 0.12f, 0.22f), 0.055f, 0.055f, 0.02f)), PropSlot.Accent),
                Piece(PShape("LodgeHearth_Fire",
                    Sphere(new Vector3(0f, 0.2f, 0.14f), 0.17f),
                    Cone(new Vector3(0f, 0.22f, 0.14f), new Vector3(0f, 0.6f, 0.1f), 0.15f, 0.02f, 0.08f),
                    Cone(new Vector3(0.14f, 0.2f, 0.14f), new Vector3(0.22f, 0.45f, 0.12f), 0.07f, 0.01f, 0.06f),
                    Cone(new Vector3(-0.14f, 0.2f, 0.16f), new Vector3(-0.2f, 0.42f, 0.12f), 0.065f, 0.01f, 0.06f)),
                    PropSlot.Glow, PropMotion.Flicker, 0.03f),
            };
            all["LodgeHearth"] = hearth;

            var comb = Recipe("LodgeHoneycomb", new Vector2Int(2, 1), true, false, 2000);
            comb.palette = Palette(cream, oak, stone, Moss, ember, C("#E3A23B"));
            comb.yawJitter = 0f;
            comb.scaleJitter = 0f;
            var frame = new List<SdfPrimitive>
            {
                Box(new Vector3(0f, 0.68f, -0.1f), new Vector3(0.95f, 0.62f, 0.18f), 0.05f, 0f),
            };
            // Keep in sync with LodgeRoom.Cells.
            foreach (float y in new[] { 0.42f, 0.96f })
                foreach (float x in new[] { -0.55f, 0f, 0.55f })
                    frame.Add(Cone(new Vector3(x, y, 0.2f), new Vector3(x, y, -0.05f), 0.21f, 0.21f, 0.02f).Sub());
            frame.Add(Floor(0f));
            comb.pieces = new List<PropPiece>
            {
                Piece(PShape("LodgeHoneycomb_Frame", frame.ToArray()), PropSlot.Wood),
                Piece(PShape("LodgeHoneycomb_Back",
                    Box(new Vector3(0f, 0.68f, -0.07f), new Vector3(0.86f, 0.56f, 0.03f), 0.01f, 0f)), PropSlot.Accent),
            };
            all["LodgeHoneycomb"] = comb;

            var rug = Recipe("LodgeRug", new Vector2Int(2, 2), false, false, 600);
            rug.palette = Palette(cream, oak, stone, Moss, ember, C("#9E3B2E"));
            rug.yawJitter = 0f;
            rug.scaleJitter = 0f;
            rug.outline = 0f;
            rug.pieces = new List<PropPiece>
            {
                Piece(PShape("LodgeRug_Weave", Ell(new Vector3(0f, 0.012f, 0f), new Vector3(0.86f, 0.016f, 0.74f), 0f)), PropSlot.Accent),
                Piece(PShape("LodgeRug_Inner", Ell(new Vector3(0f, 0.02f, 0f), new Vector3(0.55f, 0.012f, 0.46f), 0f)), PropSlot.Clay),
            };
            all["LodgeRug"] = rug;

            var cushion = Recipe("LodgeCushion", new Vector2Int(1, 1), false, true, 400);
            cushion.palette = Palette(cream, oak, stone, Moss, ember, C("#D9A441"));
            cushion.pieces = new List<PropPiece>
            {
                Piece(PShape("LodgeCushion_Body", Ell(new Vector3(0f, 0.09f, 0f), new Vector3(0.3f, 0.09f, 0.28f), 0f)), PropSlot.Accent),
            };
            all["LodgeCushion"] = cushion;

            var pedestal = Recipe("LodgePedestal", new Vector2Int(1, 1), true, false, 700);
            pedestal.palette = Palette(cream, oak, stone, Moss, ember, Smoke);
            pedestal.yawJitter = 0f;
            pedestal.scaleJitter = 0f;
            pedestal.pieces = new List<PropPiece>
            {
                Piece(PShape("LodgePedestal_Stone",
                    Cone(new Vector3(0f, 0f, 0f), new Vector3(0f, 0.43f, 0f), 0.3f, 0.22f, 0f),
                    Ell(new Vector3(0f, 0.46f, 0f), new Vector3(0.36f, 0.045f, 0.36f), 0.03f),
                    Floor(0f)), PropSlot.Stone),
                Piece(PShape("LodgePedestal_Ring",
                    Torus(new Vector3(0f, 0.5f, 0f), 0.3f, 0.022f, Vector3.zero, 0f)), PropSlot.Glow, PropMotion.Flicker, 0.002f),
            };
            all["LodgePedestal"] = pedestal;

            var door = Recipe("LodgeDoor", new Vector2Int(2, 1), false, false, 1200);
            door.palette = Palette(cream, oak, stone, Moss, ember, Smoke);
            door.yawJitter = 0f;
            door.scaleJitter = 0f;
            // A 2-tile glowing archway on the open front edge (about 0.78 tile tall, LodgeRoom.archHeight): a rounded oak
            // arch on two little feet, lined with a warm glowing band, and the glowing threshold under it.
            door.pieces = new List<PropPiece>
            {
                Piece(PShape("LodgeDoor_Frame",
                    Ell(new Vector3(0f, 0f, 0.3f), new Vector3(0.94f, 0.78f, 0.08f), 0f),
                    Ell(new Vector3(0f, 0f, 0.3f), new Vector3(0.74f, 0.62f, 0.2f), 0.02f).Sub(),
                    Box(new Vector3(0.86f, 0.03f, 0.3f), new Vector3(0.1f, 0.03f, 0.1f), 0.01f, 0.02f),
                    Box(new Vector3(-0.86f, 0.03f, 0.3f), new Vector3(0.1f, 0.03f, 0.1f), 0.01f, 0.02f),
                    Floor(0f)), PropSlot.Wood),
                Piece(PShape("LodgeDoor_Glow",
                    Ell(new Vector3(0f, 0f, 0.3f), new Vector3(0.76f, 0.64f, 0.04f), 0f),
                    Ell(new Vector3(0f, 0f, 0.3f), new Vector3(0.67f, 0.55f, 0.1f), 0.01f).Sub(),
                    Floor(0f)), PropSlot.Glow, PropMotion.Flicker, 0.002f),
                Piece(PShape("LodgeDoor_Threshold",
                    Box(new Vector3(0f, 0.014f, 0.25f), new Vector3(0.72f, 0.014f, 0.14f), 0.01f, 0f)), PropSlot.Glow, PropMotion.Flicker, 0.001f),
            };
            if (AssetDatabase.LoadAssetAtPath<SdfShape>($"{Root}/Shapes/LodgeDoor_Posts.asset") != null)
                AssetDatabase.DeleteAsset($"{Root}/Shapes/LodgeDoor_Posts.asset");
            all["LodgeDoor"] = door;

            foreach (var r in all.Values) EditorUtility.SetDirty(r);
            return all;
        }

        // ---- themes ----

        /// <summary>
        /// Step 16 (judges: the lodge read like a prototype): a round woven rug (cream, terracotta, sage rings; replaces the
        /// rug's pieces), warm stone tiles in front of the hearth, and two low wall sets: a small bookshelf with a potted
        /// fern and a cushion stack; a side table with a lantern and a chest. Each set is one recipe (one instanced draw),
        /// round enough to read from any yaw. Shape units are tiles, y up from the floor.
        /// </summary>
        public static Dictionary<string, PropRecipe> WriteLodgeDressing()
        {
            DioramaSceneSetup.EnsureFolder(Root);
            DioramaSceneSetup.EnsureFolder($"{Root}/Shapes");
            var cream = C("#E3CDA6");
            var oak = C("#7A5236");
            var stone = C("#B88E6E");
            var sage = C("#8FA37A");
            var ember = C("#FFB04A", 1.2f);
            var terracotta = C("#C0603E");
            var all = new Dictionary<string, PropRecipe>();

            // Nov 6 fix (Mugesh: the rug and hearth stones looked broken, then too dense): marching cubes made thin flat
            // rings jagged (4.4 cm cells for 2 cm steps) and, finer, 6-21k triangles. The rug and the hearth apron are now
            // built as extruded outlines by FlatProps (a few hundred triangles); these SDF pieces stay as the same shapes
            // (for previews) and give each layer its palette slot, in order.
            var rug = Recipe("LodgeRug", new Vector2Int(2, 2), false, false, 700); // Nov 6: built flat (FlatProps); the pieces give the slots
            rug.resolution = 64;
            rug.palette = Palette(cream, oak, stone, sage, ember, terracotta);
            rug.yawJitter = 0f;
            rug.scaleJitter = 0f;
            rug.outline = 0f;
            SdfPrimitive Ring(float r) => Ell(Vector3.zero, new Vector3(r, 0.4f, r), 0f);
            SdfPrimitive Top(float y) => Plane(new Vector3(0f, y, 0f), Vector3.up, 0f);
            SdfPrimitive Bottom() => Plane(Vector3.zero, Vector3.down, 0f);
            rug.pieces = new List<PropPiece>
            {
                Piece(PShape("LodgeRug_Weave", Ring(0.88f), Top(0.035f), Bottom()), PropSlot.Accent),
                Piece(PShape("LodgeRug_Inner", Ring(0.66f), Top(0.065f), Bottom()), PropSlot.Clay),
                Piece(PShape("LodgeRug_Sage", Ring(0.42f), Top(0.095f), Bottom()), PropSlot.Leaf),
                Piece(PShape("LodgeRug_Dot", Ring(0.17f), Top(0.125f), Bottom()), PropSlot.Accent),
            };
            all["LodgeRug"] = rug;

            // Same fix (Mugesh: the small tiles by the fire looked broken): eleven little slabs read as scattered chips at
            // island scale. Now one half-round stone apron out from the hearth with a warm inset, flat tops cut at the floor.
            var stones = Recipe("LodgeHearthStones", new Vector2Int(2, 1), false, false, 300); // Nov 6: built flat (FlatProps)
            stones.resolution = 72;
            stones.palette = Palette(cream, oak, stone, sage, ember, C("#9C6B4E"));
            stones.yawJitter = 0f;
            stones.scaleJitter = 0f;
            stones.outline = 0f; // hard-edged flat layers: an inverted hull would crack at the corners
            // Keeps the half toward the room (z > -0.5: the hearth's side of the tile is z = -0.5).
            SdfPrimitive Back() => Plane(new Vector3(0f, 0f, -0.5f), Vector3.back, 0f);
            stones.pieces = new List<PropPiece>
            {
                Piece(PShape("LodgeHearthStones_Slabs", Ell(new Vector3(0f, 0f, -0.5f), new Vector3(0.86f, 0.4f, 0.62f), 0f),
                    Top(0.07f), Bottom(), Back()), PropSlot.Stone),
                Piece(PShape("LodgeHearthStones_Warm", Ell(new Vector3(0f, 0f, -0.5f), new Vector3(0.56f, 0.4f, 0.38f), 0f),
                    Top(0.105f), Bottom(), Back()), PropSlot.Accent),
            };
            all["LodgeHearthStones"] = stones;

            var shelf = Recipe("LodgeShelfFern", new Vector2Int(1, 1), true, false, 2000);
            shelf.palette = Palette(C("#B5653F"), oak, C("#D9A441"), C("#5E8A4E"), ember, C("#C9A15A"));
            shelf.yawJitter = 0f;
            shelf.scaleJitter = 0f;
            shelf.pieces = new List<PropPiece>
            {
                // A low two-sided bookshelf along the back of the tile.
                Piece(PShape("LodgeShelf_Frame",
                    Box(new Vector3(0f, 0.2f, -0.2f), new Vector3(0.34f, 0.2f, 0.09f), 0.02f, 0f),
                    Box(new Vector3(0f, 0.2f, -0.2f), new Vector3(0.3f, 0.06f, 0.1f), 0.01f, 0f).Sub()), PropSlot.Wood),
                Piece(PShape("LodgeShelf_Books",
                    Box(new Vector3(-0.2f, 0.47f, -0.2f), new Vector3(0.035f, 0.07f, 0.06f), 0.01f, 0f),
                    Box(new Vector3(-0.12f, 0.46f, -0.2f), new Vector3(0.03f, 0.06f, 0.06f), 0.01f, 0f),
                    Box(new Vector3(-0.05f, 0.45f, -0.2f), new Vector3(0.025f, 0.05f, 0.06f), 0.01f, 0f, new Vector3(0f, 0f, -14f)),
                    Box(new Vector3(0.14f, 0.43f, -0.2f), new Vector3(0.08f, 0.025f, 0.06f), 0.01f, 0f)), PropSlot.Accent),
                // A potted fern, front right.
                Piece(PShape("LodgeFern_Pot", Cone(new Vector3(0.22f, 0f, 0.18f), new Vector3(0.22f, 0.15f, 0.18f), 0.08f, 0.11f, 0f)), PropSlot.Clay),
                Piece(PShape("LodgeFern_Leaves",
                    Ell(new Vector3(0.22f, 0.24f, 0.18f), new Vector3(0.12f, 0.09f, 0.12f), 0.05f),
                    Ell(new Vector3(0.12f, 0.27f, 0.18f), new Vector3(0.11f, 0.03f, 0.05f), 0.04f, new Vector3(0f, 0f, 25f)),
                    Ell(new Vector3(0.32f, 0.27f, 0.18f), new Vector3(0.11f, 0.03f, 0.05f), 0.04f, new Vector3(0f, 0f, -25f)),
                    Ell(new Vector3(0.22f, 0.27f, 0.08f), new Vector3(0.05f, 0.03f, 0.11f), 0.04f, new Vector3(-25f, 0f, 0f)),
                    Ell(new Vector3(0.22f, 0.27f, 0.28f), new Vector3(0.05f, 0.03f, 0.11f), 0.04f, new Vector3(25f, 0f, 0f))), PropSlot.Leaf, PropMotion.Sway, 0.01f),
                // A stack of two cushions, front left.
                Piece(PShape("LodgeCushionStack",
                    Ell(new Vector3(-0.2f, 0.05f, 0.18f), new Vector3(0.14f, 0.05f, 0.13f), 0.02f),
                    Ell(new Vector3(-0.2f, 0.13f, 0.18f), new Vector3(0.11f, 0.045f, 0.1f), 0.02f)), PropSlot.Stone),
            };
            all["LodgeShelfFern"] = shelf;

            var table = Recipe("LodgeTableChest", new Vector2Int(1, 1), true, false, 3000);
            table.palette = Palette(cream, oak, C("#3D3633"), sage, ember, C("#C9A15A"));
            table.yawJitter = 0f;
            table.scaleJitter = 0f;
            table.pieces = new List<PropPiece>
            {
                // A little round side table (and the chest, front right).
                Piece(PShape("LodgeTable_Wood",
                    Ell(new Vector3(-0.12f, 0.3f, -0.15f), new Vector3(0.17f, 0.025f, 0.17f), 0.01f),
                    Cone(new Vector3(-0.12f, 0f, -0.15f), new Vector3(-0.12f, 0.3f, -0.15f), 0.07f, 0.03f, 0.02f),
                    Box(new Vector3(0.18f, 0.1f, 0.17f), new Vector3(0.14f, 0.1f, 0.1f), 0.02f, 0f)), PropSlot.Wood),
                Piece(PShape("LodgeTable_Iron",
                    Ell(new Vector3(-0.12f, 0.46f, -0.15f), new Vector3(0.05f, 0.02f, 0.05f), 0.01f),
                    Ell(new Vector3(-0.12f, 0.33f, -0.15f), new Vector3(0.055f, 0.012f, 0.055f), 0.01f)), PropSlot.Stone),
                Piece(PShape("LodgeTable_Lantern",
                    Ell(new Vector3(-0.12f, 0.39f, -0.15f), new Vector3(0.045f, 0.06f, 0.045f), 0.01f)), PropSlot.Glow, PropMotion.Flicker, 0.003f),
                Piece(PShape("LodgeChest_Bands",
                    Box(new Vector3(0.18f, 0.205f, 0.17f), new Vector3(0.145f, 0.018f, 0.105f), 0.01f, 0f),
                    Box(new Vector3(0.18f, 0.12f, 0.27f), new Vector3(0.025f, 0.03f, 0.008f), 0.005f, 0f)), PropSlot.Accent),
            };
            all["LodgeTableChest"] = table;
            AssetDatabase.SaveAssets();
            return all;
        }

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
            grass = f(c.grass), tallGrass = f(c.tallGrass), path = f(c.path), water = f(c.water), hotSpring = f(c.hotSpring), dryBed = f(c.dryBed),
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
                grass = G(c.grass), tallGrass = G(c.tallGrass), path = G(c.path), water = G(c.water), hotSpring = G(c.hotSpring), dryBed = G(c.dryBed),
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
