using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Writes the first creature data: the parts kit (10 parts), the 3 starters (Kilnpot, Pebbloop, Pufflet) and one
    /// evolution (Kilnhearth) to prove modifiers. After this, creatures are tuned in the Inspector, not here.
    /// Re-running overwrites these assets (same files, so references survive). Shapes are in shape units:
    /// a creature is about 1.5 tall, +Z is its front, +Y up.
    /// </summary>
    public static class StarterKit
    {
        const string Root = "Assets/_Project/Data/Creatures";

        [MenuItem("Tools/Pocket Isle/Creatures/Write Starter Kit (overwrites)", priority = 20)]
        static void Menu()
        {
            if (!EditorUtility.DisplayDialog("Write starter kit",
                    "Overwrites the kit parts, the 3 starters and Kilnhearth in Data/Creatures with the defaults from code. " +
                    "Inspector tweaks to those assets are lost.", "Overwrite", "Cancel")) return;
            Write();
        }

        public static void Write()
        {
            foreach (var d in new[] { "Shapes", "Parts", "Recipes", "Species" }) DioramaSceneSetup.EnsureFolder($"{Root}/{d}");

            // ---- parts kit ----
            var roundEar = Part("RoundEar", PartMotion.Flick, Vector3.right, 25f, 1f,
                new PartStyle { color = PaletteSlot.Base, spec = 0.2f, outline = 0.025f },
                Ell(new Vector3(0f, 0.07f, 0f), new Vector3(0.085f, 0.08f, 0.05f)));

            var bearEar = Part("BearEar", PartMotion.Flick, Vector3.right, 25f, 1f,
                new PartStyle
                {
                    color = PaletteSlot.Base, spec = 0.15f, outline = 0.025f,
                    inner = true, innerColor = PaletteSlot.Belly, innerDirection = new Vector3(0f, 0.15f, 1f), innerAt = 0.75f,
                },
                Ell(new Vector3(0f, 0.09f, 0f), new Vector3(0.12f, 0.11f, 0.06f)),
                Sphere(new Vector3(0f, 0.1f, 0.075f), 0.065f, 0.03f).Sub());

            var droopyEar = Part("DroopyEar", PartMotion.Flick, Vector3.right, 18f, 0.8f,
                new PartStyle { color = PaletteSlot.Socks, spec = 0.1f, outline = 0.022f },
                Ell(new Vector3(0f, 0.1f, 0f), new Vector3(0.065f, 0.13f, 0.035f)));

            var woolTuft = Part("WoolTuft", PartMotion.Wag, Vector3.up, 15f, 0.8f,
                new PartStyle { color = PaletteSlot.Base, spec = 0.05f, outline = 0.025f },
                Sphere(new Vector3(0f, 0.06f, 0f), 0.08f).Bumps(0.012f, 30f),
                Sphere(new Vector3(0.05f, 0.11f, 0f), 0.06f, 0.05f).Bumps(0.012f, 30f),
                Sphere(new Vector3(-0.05f, 0.11f, 0.01f), 0.06f, 0.05f).Bumps(0.012f, 30f));

            var wingTuft = Part("WingTuft", PartMotion.Flap, Vector3.forward, 18f, 0.8f,
                new PartStyle
                {
                    color = PaletteSlot.Base, spec = 0.05f, outline = 0.022f,
                    tip = true, tipColor = PaletteSlot.Accent, tipAxis = Vector3.right, tipAt = 0.3f,
                },
                Ell(new Vector3(0.12f, 0.04f, 0f), new Vector3(0.13f, 0.07f, 0.1f), 0f, new Vector3(0f, 0f, 15f)).Bumps(0.01f, 26f),
                Ell(new Vector3(0.25f, 0.1f, -0.03f), new Vector3(0.1f, 0.04f, 0.07f), 0.05f, new Vector3(0f, 0f, 30f)).Bumps(0.008f, 28f));

            var chimneyTail = Part("ChimneyTail", PartMotion.Wag, Vector3.up, 10f, 0.5f,
                new PartStyle
                {
                    color = PaletteSlot.Base, spec = 0.25f, outline = 0.03f,
                    tip = true, tipColor = PaletteSlot.Socks, tipAxis = Vector3.up, tipAt = 0.24f,
                },
                Cone(new Vector3(0f, 0f, 0f), new Vector3(0f, 0.02f, -0.2f), 0.09f, 0.085f),
                Cone(new Vector3(0f, 0.02f, -0.2f), new Vector3(0f, 0.27f, -0.3f), 0.085f, 0.095f, 0.08f),
                Torus(new Vector3(0f, 0.29f, -0.305f), 0.085f, 0.03f, new Vector3(-20f, 0f, 0f), 0.03f),
                Sphere(new Vector3(0f, 0.34f, -0.32f), 0.065f, 0.02f).Sub());

            var paddleTail = Part("PaddleTail", PartMotion.Wag, Vector3.right, 12f, 0.7f,
                new PartStyle { color = PaletteSlot.Extra, spec = 0.3f, outline = 0.025f },
                Cone(Vector3.zero, new Vector3(0f, 0f, -0.12f), 0.07f, 0.05f),
                Ell(new Vector3(0f, 0f, -0.24f), new Vector3(0.17f, 0.04f, 0.17f), 0.05f));

            var rotorPrims = new List<SdfPrimitive>
            {
                Cone(Vector3.zero, new Vector3(0f, 0.1f, 0f), 0.022f, 0.018f),
                Sphere(new Vector3(0f, 0.11f, 0f), 0.035f, 0.02f),
            };
            for (int i = 0; i < 3; i++)
            {
                float a = i * 120f;
                Vector3 dir = Quaternion.Euler(0f, a, 0f) * Vector3.right;
                rotorPrims.Add(Ell(new Vector3(0f, 0.11f, 0f) + dir * 0.15f, new Vector3(0.15f, 0.018f, 0.045f), 0.02f, new Vector3(0f, a, 0f)));
            }
            var rotor = Part("Rotor", PartMotion.Spin, Vector3.up, 0f, 1.2f,
                new PartStyle { color = PaletteSlot.Accent, spec = 0.5f, outline = 0.018f },
                rotorPrims.ToArray());

            var flamePuff = Part("FlamePuff", PartMotion.Flicker, Vector3.up, 0.14f, 1f,
                new PartStyle { color = PaletteSlot.Glow, gradient = true, gradientTo = PaletteSlot.Extra, glow = 0.75f, spec = 0f, outline = 0.016f },
                Sphere(new Vector3(0f, 0.07f, 0f), 0.08f),
                Cone(new Vector3(0f, 0.07f, 0f), new Vector3(0f, 0.3f, -0.03f), 0.07f, 0.01f, 0.05f),
                Cone(new Vector3(0.045f, 0.08f, 0f), new Vector3(0.09f, 0.2f, -0.01f), 0.035f, 0.008f, 0.04f).Mirror());

            var bubbleRing = Part("BubbleRing", PartMotion.Bob, Vector3.up, 0.02f, 0.5f,
                new PartStyle { color = PaletteSlot.Glow, glow = 0.2f, spec = 1f, outline = 0.02f },
                Torus(Vector3.zero, 0.46f, 0.075f, Vector3.zero).Bumps(0.018f, 22f));
            bubbleRing.resolution = 56;

            // ---- Kilnpot: clay-pot tanuki cub, upright, chimney tail with a flame ----
            var kilnpotBody = Shape("Kilnpot_Body",
                Ell(new Vector3(0f, -0.26f, 0f), new Vector3(0.4f, 0.38f, 0.36f)),
                Ell(new Vector3(0f, -0.36f, 0.03f), new Vector3(0.42f, 0.27f, 0.37f), 0.15f),
                Ell(new Vector3(0f, 0.3f, 0.03f), new Vector3(0.42f, 0.36f, 0.37f), 0.16f),
                Ell(new Vector3(0.25f, 0.2f, 0.12f), new Vector3(0.17f, 0.13f, 0.15f), 0.1f).Mirror(),
                Ell(new Vector3(0f, 0.2f, 0.36f), new Vector3(0.14f, 0.09f, 0.09f), 0.07f),
                Cone(new Vector3(0.36f, -0.06f, 0.06f), new Vector3(0.27f, -0.27f, 0.28f), 0.1f, 0.095f, 0.04f).Mirror(),
                Cone(new Vector3(0.19f, -0.52f, 0.02f), new Vector3(0.2f, -0.76f, 0.05f), 0.13f, 0.125f, 0.05f).Mirror(),
                Floor(-0.79f));
            var kilnpot = Recipe("Kilnpot", kilnpotBody, 0.045f,
                new CreaturePalette
                {
                    baseColor = Hex("#CF6E45"), belly = Hex("#F5E3C6"), accent = Hex("#8C4128"), socks = Hex("#4A3328"),
                    glow = Hex("#F0702A"), extra = Hex("#FFC845"), face = Hex("#2A1A14"), iris = Hex("#5A3420"),
                },
                new BodyZones
                {
                    socks = true, socksY = -0.62f,
                    patches = new List<PatternPatch>
                    {
                        new PatternPatch { center = new Vector3(0f, -0.36f, 0.4f), radius = 0.26f, color = PaletteSlot.Belly },
                        new PatternPatch { center = new Vector3(0.16f, 0.32f, 0.36f), radius = 0.12f, color = PaletteSlot.Accent, mirrorX = true },
                        new PatternPatch { center = new Vector3(0.27f, -0.28f, 0.36f), radius = 0.1f, color = PaletteSlot.Socks, mirrorX = true },
                        new PatternPatch { center = new Vector3(0f, 0.2f, 0.43f), radius = 0.14f, color = PaletteSlot.Belly },
                    },
                },
                new FaceSettings
                {
                    eyeSize = 0.065f, eyeSpacing = 0.16f, eyeHeight = 0.33f, eyeStretch = 1.2f,
                    decals = new List<FaceDecal>
                    {
                        new FaceDecal { kind = DecalKind.Disc, at = new Vector2(0f, 0.25f), size = new Vector2(0.05f, 0.035f), color = PaletteSlot.Face },
                        new FaceDecal { kind = DecalKind.Smile, at = new Vector2(0f, 0.165f), size = new Vector2(0.035f, 0f), color = PaletteSlot.Face },
                    },
                },
                new IdleStyle { breathe = 0.018f, breatheSpeed = 2.6f, sway = 2f },
                new List<PlacedPart>
                {
                    Place(bearEar, new Vector3(0.27f, 0.55f, 0f), new Vector3(0f, 0f, -22f), 1f, true),
                    Place(chimneyTail, new Vector3(0.12f, -0.42f, -0.3f), new Vector3(0f, 25f, 0f), 1.2f),
                    Place(flamePuff, new Vector3(0f, 0.3f, -0.31f), Vector3.zero, 1f, false, 1),
                });

            // ---- Pebbloop: chubby river-hippo calf, bubble-ring collar, paddle tail, pebble spots ----
            var pebbloopBody = Shape("Pebbloop_Body",
                Ell(new Vector3(0f, -0.02f, -0.12f), new Vector3(0.44f, 0.36f, 0.52f)),
                Ell(new Vector3(0f, 0.14f, 0.36f), new Vector3(0.33f, 0.31f, 0.3f), 0.2f),
                Ell(new Vector3(0f, -0.01f, 0.65f), new Vector3(0.33f, 0.22f, 0.23f), 0.12f),
                Cone(new Vector3(0.24f, -0.25f, 0.16f), new Vector3(0.25f, -0.48f, 0.18f), 0.13f, 0.12f, 0.08f).Mirror(),
                Cone(new Vector3(0.27f, -0.25f, -0.4f), new Vector3(0.27f, -0.48f, -0.42f), 0.14f, 0.13f, 0.08f).Mirror(),
                Floor(-0.5f));
            var pebbloop = Recipe("Pebbloop", pebbloopBody, 0.04f,
                new CreaturePalette
                {
                    baseColor = Hex("#8EA0D8"), belly = Hex("#F2F4F7"), accent = Hex("#34477A"), socks = Hex("#8EA0D8"),
                    glow = Hex("#6ED6E6"), extra = Hex("#4A5C96"), face = Hex("#2A2440"), iris = Hex("#2A2A55"),
                },
                new BodyZones
                {
                    patches = new List<PatternPatch>
                    {
                        new PatternPatch { center = new Vector3(0f, -0.01f, 0.86f), radius = 0.3f, color = PaletteSlot.Belly },
                        new PatternPatch { center = new Vector3(0.13f, 0.31f, -0.18f), radius = 0.1f, color = PaletteSlot.Accent },
                        new PatternPatch { center = new Vector3(-0.12f, 0.3f, -0.36f), radius = 0.09f, color = PaletteSlot.Accent },
                        new PatternPatch { center = new Vector3(0.16f, 0.22f, -0.5f), radius = 0.075f, color = PaletteSlot.Accent },
                    },
                },
                new FaceSettings
                {
                    eyeSize = 0.075f, eyeSpacing = 0.17f, eyeHeight = 0.34f, eyeStretch = 1.15f,
                    decals = new List<FaceDecal>
                    {
                        new FaceDecal { kind = DecalKind.Disc, at = new Vector2(0.09f, 0.1f), size = new Vector2(0.03f, 0.022f), color = PaletteSlot.Face, mirrorX = true },
                        new FaceDecal { kind = DecalKind.Smile, at = new Vector2(0f, -0.06f), size = new Vector2(0.07f, 0f), color = PaletteSlot.Face },
                    },
                },
                new IdleStyle { breathe = 0.02f, breatheSpeed = 2.2f, jiggle = 0.006f, sway = 1.5f },
                new List<PlacedPart>
                {
                    Place(roundEar, new Vector3(0.2f, 0.38f, 0.28f), new Vector3(0f, 0f, -15f), 1f, true),
                    Place(bubbleRing, new Vector3(0f, 0.12f, 0.16f), new Vector3(65f, 0f, 0f), 1f),
                    Place(paddleTail, new Vector3(0f, -0.02f, -0.6f), new Vector3(20f, 0f, 0f), 1f),
                });

            // ---- Pufflet: cloud-alpaca lamb, long neck, dandelion rotor, wing tufts ----
            var pufflBody = Shape("Pufflet_Body",
                Sphere(new Vector3(0f, -0.02f, -0.1f), 0.33f).Bumps(0.026f, 24f),
                Sphere(new Vector3(0f, 0.02f, 0.12f), 0.29f, 0.12f).Bumps(0.026f, 24f),
                Sphere(new Vector3(0f, 0.02f, -0.32f), 0.28f, 0.12f).Bumps(0.026f, 24f),
                Cone(new Vector3(0f, 0.14f, 0.16f), new Vector3(0f, 0.6f, 0.22f), 0.19f, 0.16f, 0.12f).Bumps(0.024f, 24f),
                Sphere(new Vector3(0f, 0.8f, 0.15f), 0.2f, 0.1f).Bumps(0.024f, 24f),
                Ell(new Vector3(0f, 0.64f, 0.4f), new Vector3(0.17f, 0.17f, 0.15f), 0.06f),
                Cone(new Vector3(0.15f, -0.22f, 0.14f), new Vector3(0.15f, -0.52f, 0.15f), 0.075f, 0.07f, 0.05f).Mirror(),
                Cone(new Vector3(0.15f, -0.22f, -0.32f), new Vector3(0.15f, -0.52f, -0.33f), 0.075f, 0.07f, 0.05f).Mirror(),
                Floor(-0.53f));
            var pufflet = Recipe("Pufflet", pufflBody, 0.05f,
                new CreaturePalette
                {
                    baseColor = Hex("#F5F0E0"), belly = Hex("#FFFFFF"), accent = Hex("#E8C84A"), socks = Hex("#7AA87A"),
                    glow = Hex("#E8C84A"), extra = Hex("#E8C84A"), face = Hex("#2A3028"), iris = Hex("#2A4A3A"),
                },
                new BodyZones
                {
                    socks = true, socksY = -0.3f,
                    patches = new List<PatternPatch>
                    {
                        new PatternPatch { center = new Vector3(0f, 0.64f, 0.5f), radius = 0.17f, color = PaletteSlot.Socks },
                    },
                },
                new FaceSettings
                {
                    eyeSize = 0.05f, eyeSpacing = 0.085f, eyeHeight = 0.68f, eyeStretch = 1.25f,
                    decals = new List<FaceDecal>
                    {
                        new FaceDecal { kind = DecalKind.Smile, at = new Vector2(0f, 0.58f), size = new Vector2(0.03f, 0f), color = PaletteSlot.Face },
                    },
                },
                new IdleStyle { breathe = 0.015f, breatheSpeed = 2f, jiggle = 0.004f, hover = 0.002f, bob = 0.0012f, sway = 1.5f },
                new List<PlacedPart>
                {
                    Place(droopyEar, new Vector3(0.16f, 0.72f, 0.3f), new Vector3(0f, 0f, -100f), 1f, true),
                    Place(rotor, new Vector3(0f, 0.98f, 0.15f), Vector3.zero, 1f),
                    Place(wingTuft, new Vector3(0.28f, 0.12f, -0.08f), new Vector3(0f, 35f, 25f), 0.85f, true),
                    Place(woolTuft, new Vector3(0f, 0.06f, -0.58f), new Vector3(-60f, 0f, 0f), 1f),
                });

            // ---- Kilnhearth: Kilnpot's evolution, the same recipe with modifiers ----
            var kilnhearth = Asset<CreatureRecipe>($"{Root}/Recipes/Kilnhearth.asset");
            kilnhearth.baseRecipe = kilnpot;
            kilnhearth.heightScale = 1.3f;
            kilnhearth.extraParts = new List<PlacedPart> { Place(flamePuff, new Vector3(0f, 0.62f, 0.05f), Vector3.zero, 0.9f) };
            kilnhearth.overridePalette = false;
            kilnhearth.hueShift = -0.02f;
            kilnhearth.saturationScale = 1.1f;
            kilnhearth.valueScale = 0.88f;
            EditorUtility.SetDirty(kilnhearth);

            var kilnhearthSpecies = Species("Kilnhearth", Element.Fire, kilnhearth, null, 0);
            Species("Kilnpot", Element.Fire, kilnpot, kilnhearthSpecies, 14);
            Species("Pebbloop", Element.Water, pebbloop, null, 14);
            Species("Pufflet", Element.Wind, pufflet, null, 14);

            AssetDatabase.SaveAssets();
            Debug.Log($"[StarterKit] Wrote 10 parts, 3 starters and Kilnhearth to {Root}.");
        }

        // ---- asset helpers ----

        internal static T Asset<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        internal static SdfShape Shape(string name, params SdfPrimitive[] prims)
        {
            var s = Asset<SdfShape>($"{Root}/Shapes/{name}.asset");
            s.primitives = new List<SdfPrimitive>(prims);
            EditorUtility.SetDirty(s);
            return s;
        }

        internal static CreaturePart Part(string name, PartMotion motion, Vector3 axis, float amount, float speed, PartStyle style, params SdfPrimitive[] prims)
        {
            var p = Asset<CreaturePart>($"{Root}/Parts/{name}.asset");
            p.shape = Shape("Part_" + name, prims);
            p.pivot = Vector3.zero;
            p.motion = motion;
            p.motionAxis = axis;
            p.motionAmount = amount;
            p.motionSpeed = speed;
            p.style = style;
            p.resolution = 40;
            p.triangleBudget = 1200;
            EditorUtility.SetDirty(p);
            return p;
        }

        internal static CreatureRecipe Recipe(string name, SdfShape body, float height, CreaturePalette palette, BodyZones zones,
                                     FaceSettings face, IdleStyle idle, List<PlacedPart> parts)
        {
            var r = Asset<CreatureRecipe>($"{Root}/Recipes/{name}.asset");
            r.baseRecipe = null;
            r.body = body;
            r.height = height;
            r.palette = palette;
            r.zones = zones;
            r.face = face;
            r.idle = idle;
            r.parts = parts;
            r.spec = 0.2f;
            r.outline = 0.035f;
            r.resolution = 88;
            r.triangleBudget = 6000;
            EditorUtility.SetDirty(r);
            return r;
        }

        internal static CreatureSpecies Species(string name, Element element, CreatureRecipe recipe, CreatureSpecies evolvesTo, int level)
        {
            var s = Asset<CreatureSpecies>($"{Root}/Species/{name}.asset");
            s.displayName = name;
            s.element = element;
            s.recipe = recipe;
            s.evolvesTo = evolvesTo;
            s.evolveLevel = evolvesTo != null ? level : 0;
            EditorUtility.SetDirty(s);
            return s;
        }

        internal static PlacedPart Place(CreaturePart part, Vector3 position, Vector3 rotation, float scale, bool mirror = false, int parent = -1) =>
            new PlacedPart { part = part, position = position, rotation = rotation, scale = Vector3.one * scale, mirror = mirror, parent = parent };

        internal static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        // ---- primitive helpers ----

        internal static SdfPrimitive Sphere(Vector3 c, float r, float k = 0.1f) =>
            new SdfPrimitive { kind = SdfKind.Sphere, a = c, size = new Vector3(r, 0f, 0f), k = k };

        internal static SdfPrimitive Ell(Vector3 c, Vector3 r, float k = 0.1f, Vector3 rotation = default) =>
            new SdfPrimitive { kind = SdfKind.Ellipsoid, a = c, size = r, k = k, rotation = rotation };

        internal static SdfPrimitive Cone(Vector3 a, Vector3 b, float r1, float r2, float k = 0.08f) =>
            new SdfPrimitive { kind = SdfKind.RoundCone, a = a, b = b, size = new Vector3(r1, r2, 0f), k = k };

        internal static SdfPrimitive Torus(Vector3 c, float ring, float tube, Vector3 rotation, float k = 0.05f) =>
            new SdfPrimitive { kind = SdfKind.Torus, a = c, size = new Vector3(ring, tube, 0f), rotation = rotation, k = k };

        /// <summary>Cuts everything below <paramref name="y"/> (flat feet).</summary>
        internal static SdfPrimitive Floor(float y) =>
            new SdfPrimitive { kind = SdfKind.Plane, blend = SdfBlend.Intersect, a = new Vector3(0f, y, 0f), b = Vector3.down, k = 0.02f };

        internal static SdfPrimitive Mirror(this SdfPrimitive p)
        {
            p.mirrorX = true;
            return p;
        }

        internal static SdfPrimitive Sub(this SdfPrimitive p)
        {
            p.blend = SdfBlend.Subtract;
            return p;
        }

        internal static SdfPrimitive Bumps(this SdfPrimitive p, float height, float frequency)
        {
            p.bumps = height;
            p.bumpFrequency = frequency;
            return p;
        }
    }
}
