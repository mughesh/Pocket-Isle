using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Diorama.EditorTools.StarterKit;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 9d: the rest of the fire roster (concepts Docs/ConceptArt/07_fire_rest and 10_fire_redraws). One method
    /// per line writes the recipe(s), parts, species and moves; evolutions without a concept are modifiers on their base
    /// (bigger, extra parts, own palette), like Kilngolem. Like the other kits this seeds the assets; re-running
    /// overwrites them (same files, references survive). Needs the starter and fire kits written first.
    /// </summary>
    public static class FireRosterKit
    {
        const string Root = "Assets/_Project/Data/Creatures";
        const string MoveRoot = "Assets/_Project/Data/Battle/Moves";

        static CreaturePart Kit(string name) => AssetDatabase.LoadAssetAtPath<CreaturePart>($"{Root}/Parts/{name}.asset");
        static IslingSpecies LoadSpecies(string name) => AssetDatabase.LoadAssetAtPath<IslingSpecies>($"{Root}/Species/{name}.asset");
        static MoveData LoadMove(string file) => AssetDatabase.LoadAssetAtPath<MoveData>($"{MoveRoot}/{file}.asset");

        static IslingSpecies FireSpecies(string name, CreatureRecipe recipe, IslingSpecies evolvesTo, int level, int hp, int attack, int defense, int speed,
                                         params MoveData[] moves)
        {
            var s = Species(name, Element.Fire, recipe, evolvesTo, level);
            s.baseStats = new BaseStats { hp = hp, attack = attack, defense = defense, speed = speed };
            s.moves = moves;
            EditorUtility.SetDirty(s);
            return s;
        }

        static MoveData Attack(string file, string name, Element element, int power, int accuracy, MoveAnimation animation, bool arc = false)
        {
            var m = Asset<MoveData>($"{MoveRoot}/{file}.asset");
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

        static void Budget(CreaturePart part, int resolution, int triangles)
        {
            part.resolution = resolution;
            part.triangleBudget = triangles;
            EditorUtility.SetDirty(part);
        }

        static SdfPrimitive Cut(Vector3 point, Vector3 normal, float k) =>
            new SdfPrimitive { kind = SdfKind.Plane, blend = SdfBlend.Intersect, a = point, b = normal, k = k };

        // ---- 1. Steamswan: Kettlewing grown into a teapot swan ----

        /// <summary>
        /// A copper teapot of a swan (redraw 10): egg-shaped teapot body, a thick neck, a duck bill, a lid on its back
        /// and a little lid crown on its head, a spout on its chest puffing steam, the teapot handle for a tail, wings.
        /// Keeps Kettlewing's kettle look. Kettlewing evolves into it at Lv 14.
        /// </summary>
        public static IslingSpecies Steamswan()
        {
            var lid = Kit("KettleLid");
            var spout = Kit("SpoutBeak");
            var steam = Kit("SteamPuff");
            var wing = Kit("WingNub");
            if (lid == null || spout == null || steam == null || wing == null)
            {
                Debug.LogError("[FireRosterKit] Kettle parts missing: write the fire kit first.");
                return null;
            }
            var bill = Part("DuckBill", PartMotion.None, Vector3.up, 0f, 0f,
                new PartStyle { color = PaletteSlot.Socks, spec = 0.4f, outline = 0.022f },
                Ell(new Vector3(0f, 0f, 0.1f), new Vector3(0.1f, 0.04f, 0.13f), 0f),
                Ell(new Vector3(0f, -0.03f, 0.08f), new Vector3(0.085f, 0.03f, 0.1f), 0.03f));
            Budget(bill, 28, 400);
            var handle = Part("TeapotHandle", PartMotion.Wag, Vector3.up, 6f, 0.5f,
                new PartStyle { color = PaletteSlot.Base, spec = 0.5f, outline = 0.022f },
                Torus(Vector3.zero, 0.16f, 0.045f, new Vector3(0f, 0f, 90f), 0f),
                Cut(new Vector3(0f, 0f, 0.02f), Vector3.forward, 0f));
            Budget(handle, 32, 500);

            var body = Shape("Steamswan_Body",
                Ell(new Vector3(0f, -0.12f, -0.05f), new Vector3(0.5f, 0.42f, 0.56f)),
                Ell(new Vector3(0f, 0.1f, -0.08f), new Vector3(0.38f, 0.22f, 0.42f), 0.18f),
                Cone(new Vector3(0f, 0.12f, 0.3f), new Vector3(0f, 0.62f, 0.4f), 0.17f, 0.13f, 0.12f),
                Sphere(new Vector3(0f, 0.76f, 0.42f), 0.2f, 0.1f),
                Cone(new Vector3(0.18f, -0.48f, 0.06f), new Vector3(0.18f, -0.66f, 0.1f), 0.07f, 0.075f, 0.05f).Mirror(),
                Ell(new Vector3(0.18f, -0.67f, 0.16f), new Vector3(0.09f, 0.04f, 0.12f), 0.05f).Mirror(),
                Floor(-0.71f));
            var recipe = Recipe("Steamswan", body, 0.056f,
                new CreaturePalette
                {
                    baseColor = Hex("#B8784A"), belly = Hex("#F0DFC0"), accent = Hex("#8A5A32"), socks = Hex("#E58A32"),
                    glow = Hex("#F4F4F4"), extra = Hex("#C0963C"), face = Hex("#2A2018"), iris = Hex("#2A2018"),
                },
                new BodyZones
                {
                    socks = true, socksY = -0.56f,
                    patches = new List<PatternPatch>
                    {
                        new PatternPatch { center = new Vector3(0f, -0.3f, 0.46f), radius = 0.3f, color = PaletteSlot.Belly },
                    },
                },
                new FaceSettings { eyeSize = 0.05f, eyeSpacing = 0.11f, eyeHeight = 0.8f, eyeStretch = 1.2f, decals = new List<FaceDecal>() },
                new IdleStyle { breathe = 0.014f, breatheSpeed = 2f, sway = 2f },
                new List<PlacedPart>
                {
                    Place(lid, new Vector3(0f, 0.3f, -0.16f), new Vector3(-8f, 0f, 0f), 1.15f),
                    Place(lid, new Vector3(0f, 0.95f, 0.4f), Vector3.zero, 0.5f),
                    Place(bill, new Vector3(0f, 0.72f, 0.56f), new Vector3(8f, 0f, 0f), 1.1f),
                    Place(spout, new Vector3(0.3f, -0.08f, 0.36f), new Vector3(10f, 35f, 0f), 0.95f),
                    Place(steam, new Vector3(0f, 0.18f, 0.33f), Vector3.zero, 0.9f, false, 3),
                    Place(handle, new Vector3(0f, -0.05f, -0.6f), Vector3.zero, 1.1f),
                    Place(wing, new Vector3(0.47f, -0.04f, -0.08f), new Vector3(0f, 15f, 15f), 1.35f, true),
                });
            recipe.spec = 0.45f;
            EditorUtility.SetDirty(recipe);

            var species = FireSpecies("Steamswan", recipe, null, 0, 58, 14, 13, 14,
                LoadMove("SteamJet"), LoadMove("BodySlam"), LoadMove("HunkerDown"));
            var kettlewing = LoadSpecies("Kettlewing");
            if (kettlewing != null)
            {
                kettlewing.evolvesTo = species;
                kettlewing.evolveLevel = 14;
                EditorUtility.SetDirty(kettlewing);
            }
            AssetDatabase.SaveAssets();
            return species;
        }

        /// <summary>An evolution as modifiers on its base (no concept yet): bigger, extra parts, its own palette.</summary>
        static CreatureRecipe Evolution(string name, CreatureRecipe baseRecipe, float heightScale, CreaturePalette palette, params PlacedPart[] extras)
        {
            var r = Asset<CreatureRecipe>($"{Root}/Recipes/{name}.asset");
            r.baseRecipe = baseRecipe;
            r.heightScale = heightScale;
            r.extraParts = new List<PlacedPart>(extras);
            r.overridePalette = true;
            r.palette = palette;
            EditorUtility.SetDirty(r);
            return r;
        }

        // ---- 2. Coalbeetle -> Forgehorn ----

        /// <summary>A round charcoal beetle on six stubby legs, wing covers that lift a little, a glowing coal horn.</summary>
        public static IslingSpecies Coalbeetle()
        {
            var flame = Kit("FlamePuff");
            var chimney = Kit("Chimney");
            var horn = Part("CoalHorn", PartMotion.None, Vector3.up, 0f, 0f,
                new PartStyle { color = PaletteSlot.Glow, gradient = true, gradientTo = PaletteSlot.Extra, glow = 0.85f, spec = 0.3f, outline = 0.02f },
                Cone(Vector3.zero, new Vector3(0f, 0.28f, 0.05f), 0.075f, 0.018f, 0f));
            Budget(horn, 28, 350);
            var cover = Part("WingCover", PartMotion.Flap, Vector3.forward, 5f, 0.4f,
                new PartStyle { color = PaletteSlot.Accent, spec = 0.7f, outline = 0.022f },
                Ell(new Vector3(0.16f, 0f, 0f), new Vector3(0.22f, 0.14f, 0.44f), 0f, new Vector3(0f, 0f, -40f)));
            Budget(cover, 32, 500);

            var body = Shape("Coalbeetle_Body",
                Ell(new Vector3(0f, -0.1f, -0.08f), new Vector3(0.42f, 0.34f, 0.48f)),
                Ell(new Vector3(0f, -0.16f, 0.4f), new Vector3(0.28f, 0.24f, 0.22f), 0.04f),
                Cone(new Vector3(0.08f, -0.28f, 0.56f), new Vector3(0.11f, -0.32f, 0.66f), 0.035f, 0.02f, 0.03f).Mirror(),
                Cone(new Vector3(0.3f, -0.34f, 0.2f), new Vector3(0.35f, -0.5f, 0.24f), 0.075f, 0.07f, 0.05f).Mirror(),
                Cone(new Vector3(0.33f, -0.34f, -0.06f), new Vector3(0.38f, -0.5f, -0.06f), 0.075f, 0.07f, 0.05f).Mirror(),
                Cone(new Vector3(0.3f, -0.34f, -0.32f), new Vector3(0.35f, -0.5f, -0.36f), 0.075f, 0.07f, 0.05f).Mirror(),
                Floor(-0.52f));
            var beetle = Recipe("Coalbeetle", body, 0.036f,
                new CreaturePalette
                {
                    baseColor = Hex("#3A3A3A"), belly = Hex("#6A6A6A"), accent = Hex("#4A4A4A"), socks = Hex("#3A3A3A"),
                    glow = Hex("#E5641E"), extra = Hex("#FFB050"), face = Hex("#1A1A1A"), iris = Hex("#1A1A1A"),
                },
                new BodyZones
                {
                    patches = new List<PatternPatch>
                    {
                        new PatternPatch { center = new Vector3(0f, -0.4f, 0.1f), radius = 0.3f, color = PaletteSlot.Belly },
                    },
                },
                new FaceSettings { eyeSize = 0.045f, eyeSpacing = 0.12f, eyeHeight = -0.1f, eyeStretch = 1.2f, decals = new List<FaceDecal>() },
                new IdleStyle { breathe = 0.014f, breatheSpeed = 2.2f, sway = 1.5f },
                new List<PlacedPart>
                {
                    Place(horn, new Vector3(0f, 0.04f, 0.46f), new Vector3(-15f, 0f, 0f), 1.1f),
                    Place(cover, new Vector3(0.03f, 0.12f, -0.1f), Vector3.zero, 1f, true),
                });
            beetle.spec = 0.45f;
            EditorUtility.SetDirty(beetle);

            var forge = Evolution("Forgehorn", beetle, 1.3f,
                new CreaturePalette
                {
                    baseColor = Hex("#2E2A28"), belly = Hex("#5A524C"), accent = Hex("#6A3A22"), socks = Hex("#2E2A28"),
                    glow = Hex("#FF7A28"), extra = Hex("#FFD27A"), face = Hex("#1A1A1A"), iris = Hex("#FF8A30"),
                },
                Place(horn, new Vector3(0f, 0.06f, 0.48f), new Vector3(-25f, 0f, 0f), 1.5f),
                Place(chimney, new Vector3(0f, 0.2f, -0.34f), new Vector3(-10f, 0f, 0f), 0.9f),
                Place(flame, new Vector3(0f, 0.24f, 0f), Vector3.zero, 0.55f, false, 1));

            var hornRam = Attack("HornRam", "Horn Ram", Element.Fire, 45, 95, MoveAnimation.Lunge);
            var forgeS = FireSpecies("Forgehorn", forge, null, 0, 62, 16, 17, 8, hornRam, LoadMove("BodySlam"), LoadMove("PuffUp"));
            var s = FireSpecies("Coalbeetle", beetle, forgeS, 16, 44, 12, 13, 7, hornRam, LoadMove("Tackle"), LoadMove("HunkerDown"));
            AssetDatabase.SaveAssets();
            return s;
        }

        // ---- 3. Mapadillo -> Emberdillo ----

        /// <summary>A curled armadillo under overlapping maple-leaf plates, a little pointed snout, a leaf-tipped tail.</summary>
        public static IslingSpecies Mapadillo()
        {
            var ear = Kit("RoundEar");
            var flame = Kit("FlamePuff");
            var plate = Part("LeafPlate", PartMotion.None, Vector3.up, 0f, 0f,
                new PartStyle { color = PaletteSlot.Extra, spec = 0.25f, outline = 0.02f, tip = true, tipColor = PaletteSlot.Glow, tipAxis = Vector3.back, tipAt = 0.14f },
                Ell(Vector3.zero, new Vector3(0.2f, 0.05f, 0.2f), 0f),
                Ell(new Vector3(0.13f, 0f, -0.08f), new Vector3(0.1f, 0.045f, 0.12f), 0.04f).Mirror(),
                Ell(new Vector3(0f, 0f, -0.18f), new Vector3(0.08f, 0.045f, 0.1f), 0.04f));
            Budget(plate, 30, 400);
            var tail = Part("LeafTail", PartMotion.Wag, Vector3.up, 14f, 0.9f,
                new PartStyle { color = PaletteSlot.Base, spec = 0.2f, outline = 0.02f, tip = true, tipColor = PaletteSlot.Extra, tipAxis = Vector3.back, tipAt = 0.18f },
                Cone(Vector3.zero, new Vector3(0f, 0.03f, -0.22f), 0.05f, 0.03f, 0.03f),
                Ell(new Vector3(0f, 0.04f, -0.28f), new Vector3(0.08f, 0.025f, 0.09f), 0.03f));
            Budget(tail, 28, 350);

            var body = Shape("Mapadillo_Body",
                Ell(new Vector3(0f, -0.06f, -0.06f), new Vector3(0.45f, 0.4f, 0.5f)),
                Ell(new Vector3(0f, -0.16f, 0.42f), new Vector3(0.18f, 0.16f, 0.17f), 0.1f),
                Cone(new Vector3(0f, -0.18f, 0.52f), new Vector3(0f, -0.21f, 0.7f), 0.075f, 0.035f, 0.05f),
                Cone(new Vector3(0.24f, -0.3f, 0.22f), new Vector3(0.25f, -0.5f, 0.24f), 0.08f, 0.075f, 0.05f).Mirror(),
                Cone(new Vector3(0.24f, -0.3f, -0.3f), new Vector3(0.25f, -0.5f, -0.32f), 0.08f, 0.075f, 0.05f).Mirror(),
                Floor(-0.53f));
            var dillo = Recipe("Mapadillo", body, 0.036f,
                new CreaturePalette
                {
                    baseColor = Hex("#8B6040"), belly = Hex("#F0DFC0"), accent = Hex("#D4888A"), socks = Hex("#8B6040"),
                    glow = Hex("#E5641E"), extra = Hex("#CC5522"), face = Hex("#2A2018"), iris = Hex("#2A2018"),
                },
                new BodyZones
                {
                    patches = new List<PatternPatch>
                    {
                        new PatternPatch { center = new Vector3(0f, -0.38f, 0.3f), radius = 0.22f, color = PaletteSlot.Belly },
                        new PatternPatch { center = new Vector3(0f, -0.2f, 0.71f), radius = 0.04f, color = PaletteSlot.Accent },
                    },
                },
                new FaceSettings { eyeSize = 0.04f, eyeSpacing = 0.09f, eyeHeight = -0.1f, eyeStretch = 1.15f, decals = new List<FaceDecal>() },
                new IdleStyle { breathe = 0.016f, breatheSpeed = 2.3f, sway = 1.5f },
                new List<PlacedPart>
                {
                    Place(plate, new Vector3(0f, 0.32f, 0.22f), new Vector3(-30f, 0f, 0f), 1.2f),
                    Place(plate, new Vector3(0f, 0.36f, -0.08f), new Vector3(-5f, 0f, 0f), 1.3f),
                    Place(plate, new Vector3(0f, 0.26f, -0.38f), new Vector3(25f, 0f, 0f), 1.2f),
                    Place(plate, new Vector3(0.3f, 0.2f, 0.06f), new Vector3(-15f, 0f, -50f), 1.15f, true),
                    Place(plate, new Vector3(0.3f, 0.12f, -0.28f), new Vector3(15f, 0f, -55f), 1.1f, true),
                    Place(ear, new Vector3(0.12f, -0.02f, 0.46f), new Vector3(0f, 0f, -25f), 0.45f, true),
                    Place(tail, new Vector3(0f, -0.22f, -0.54f), new Vector3(10f, 0f, 0f), 1f),
                });
            EditorUtility.SetDirty(dillo);

            var ember = Evolution("Emberdillo", dillo, 1.25f,
                new CreaturePalette
                {
                    baseColor = Hex("#7A4A30"), belly = Hex("#F0DFC0"), accent = Hex("#D4888A"), socks = Hex("#6A4028"),
                    glow = Hex("#FFC040"), extra = Hex("#E8642A"), face = Hex("#2A2018"), iris = Hex("#2A2018"),
                },
                Place(plate, new Vector3(0f, 0.42f, 0.06f), new Vector3(-10f, 0f, 0f), 1.1f),
                Place(flame, new Vector3(0f, 0.48f, 0.04f), Vector3.zero, 0.5f));

            var roll = Attack("RollOut", "Roll Out", Element.Neutral, 40, 100, MoveAnimation.Lunge);
            var emberS = FireSpecies("Emberdillo", ember, null, 0, 64, 13, 18, 10, roll, LoadMove("KilnStomp"), LoadMove("HunkerDown"));
            var s = FireSpecies("Mapadillo", dillo, emberS, 15, 46, 10, 14, 9, roll, LoadMove("EmberPuff"), LoadMove("HunkerDown"));
            AssetDatabase.SaveAssets();
            return s;
        }

        // ---- 4. Ashmole -> Cindermole ----

        /// <summary>A round sleepy mole with big pink digging paws and a glowing coal on the tip of its nose.</summary>
        public static IslingSpecies Ashmole()
        {
            var ear = Kit("RoundEar");
            var nugget = Kit("Nugget");
            var flame = Kit("FlamePuff");
            var paw = Part("DigPaw", PartMotion.Wag, Vector3.right, 10f, 0.6f,
                new PartStyle { color = PaletteSlot.Socks, spec = 0.2f, outline = 0.022f },
                Cone(Vector3.zero, new Vector3(0.1f, -0.12f, 0.06f), 0.07f, 0.06f, 0.03f),
                Ell(new Vector3(0.14f, -0.17f, 0.08f), new Vector3(0.12f, 0.05f, 0.11f), 0.05f),
                Cone(new Vector3(0.18f, -0.17f, 0.15f), new Vector3(0.22f, -0.19f, 0.22f), 0.025f, 0.012f, 0.02f),
                Cone(new Vector3(0.12f, -0.17f, 0.17f), new Vector3(0.14f, -0.19f, 0.25f), 0.025f, 0.012f, 0.02f),
                Cone(new Vector3(0.06f, -0.17f, 0.16f), new Vector3(0.06f, -0.19f, 0.23f), 0.025f, 0.012f, 0.02f));
            Budget(paw, 32, 500);
            var coal = Part("NoseCoal", PartMotion.None, Vector3.up, 0f, 0f,
                new PartStyle { color = PaletteSlot.Glow, gradient = true, gradientTo = PaletteSlot.Extra, glow = 0.9f, spec = 0.2f, outline = 0.015f },
                Sphere(Vector3.zero, 0.05f).Bumps(0.008f, 30f));
            Budget(coal, 24, 250);

            var body = Shape("Ashmole_Body",
                Ell(new Vector3(0f, -0.12f, 0f), new Vector3(0.43f, 0.42f, 0.38f)),
                Ell(new Vector3(0f, 0.22f, 0.14f), new Vector3(0.3f, 0.26f, 0.28f), 0.18f),
                Cone(new Vector3(0f, 0.14f, 0.34f), new Vector3(0f, 0.1f, 0.5f), 0.1f, 0.055f, 0.06f),
                Ell(new Vector3(0.2f, -0.5f, 0.12f), new Vector3(0.11f, 0.06f, 0.15f), 0.05f).Mirror(),
                Floor(-0.55f));
            var mole = Recipe("Ashmole", body, 0.038f,
                new CreaturePalette
                {
                    baseColor = Hex("#7A6A5A"), belly = Hex("#F0DFC0"), accent = Hex("#C4948A"), socks = Hex("#C4948A"),
                    glow = Hex("#E5641E"), extra = Hex("#FFB050"), face = Hex("#2A2018"), iris = Hex("#2A2018"),
                },
                new BodyZones
                {
                    patches = new List<PatternPatch>
                    {
                        new PatternPatch { center = new Vector3(0f, -0.15f, 0.38f), radius = 0.28f, color = PaletteSlot.Belly },
                    },
                },
                new FaceSettings
                {
                    eyeSize = 0.045f, eyeSpacing = 0.13f, eyeHeight = 0.27f, eyeStretch = 0.35f,
                    decals = new List<FaceDecal> { new FaceDecal { kind = DecalKind.Smile, at = new Vector2(0f, 0.04f), size = new Vector2(0.03f, 0f), color = PaletteSlot.Face } },
                },
                new IdleStyle { breathe = 0.018f, breatheSpeed = 2f, sway = 1.5f },
                new List<PlacedPart>
                {
                    Place(paw, new Vector3(0.3f, -0.06f, 0.24f), new Vector3(0f, 10f, 0f), 1.15f, true),
                    Place(coal, new Vector3(0f, 0.1f, 0.53f), Vector3.zero, 1f),
                    Place(ear, new Vector3(0.22f, 0.36f, 0.06f), new Vector3(0f, 0f, -30f), 0.4f, true),
                });
            EditorUtility.SetDirty(mole);

            var cinder = Evolution("Cindermole", mole, 1.25f,
                new CreaturePalette
                {
                    baseColor = Hex("#5A4A40"), belly = Hex("#E8D2B0"), accent = Hex("#C4948A"), socks = Hex("#B88478"),
                    glow = Hex("#FF7A28"), extra = Hex("#FFD27A"), face = Hex("#2A2018"), iris = Hex("#2A2018"),
                },
                Place(flame, new Vector3(0f, 0.17f, 0.53f), Vector3.zero, 0.5f),
                Place(nugget, new Vector3(0.1f, 0.44f, 0.1f), new Vector3(0f, 30f, 0f), 1.2f),
                Place(nugget, new Vector3(-0.12f, 0.42f, 0.02f), new Vector3(0f, -40f, 10f), 1f));

            var dig = Attack("DigStrike", "Dig Strike", Element.Neutral, 45, 90, MoveAnimation.Slam);
            var cinderS = FireSpecies("Cindermole", cinder, null, 0, 66, 16, 14, 9, dig, LoadMove("KilnStomp"), LoadMove("WarmNap"));
            var s = FireSpecies("Ashmole", mole, cinderS, 16, 48, 12, 11, 8, dig, LoadMove("EmberPuff"), LoadMove("WarmNap"));
            AssetDatabase.SaveAssets();
            return s;
        }

        // ---- 5. Cindercap -> Cinderbloom ----

        /// <summary>
        /// Redraw 10: a lumpy charcoal cap split by glowing lava cracks over a small warm-orange body, ember spores
        /// drifting above. No arms, no stem, no spots: not a classic mushroom person.
        /// </summary>
        public static IslingSpecies Cindercap()
        {
            var flame = Kit("FlamePuff");
            var nubs = Kit("NubRing");
            var spore = Part("EmberSpore", PartMotion.Bob, Vector3.up, 0.04f, 0.6f,
                new PartStyle { color = PaletteSlot.Glow, glow = 1f, spec = 0f, outline = 0.012f },
                Sphere(Vector3.zero, 0.035f).Bumps(0.006f, 40f));
            Budget(spore, 20, 160);

            var body = Shape("Cindercap_Body",
                Ell(new Vector3(0f, 0.18f, 0f), new Vector3(0.5f, 0.34f, 0.47f)).Bumps(0.02f, 6f),
                Cut(new Vector3(0f, 0.02f, 0f), Vector3.down, 0.04f),
                Ell(new Vector3(0f, -0.22f, 0.02f), new Vector3(0.27f, 0.3f, 0.25f), 0.12f),
                Cone(new Vector3(0.12f, -0.42f, 0.04f), new Vector3(0.13f, -0.56f, 0.05f), 0.08f, 0.075f, 0.05f).Mirror(),
                Floor(-0.58f));
            var cap = Recipe("Cindercap", body, 0.036f,
                new CreaturePalette
                {
                    baseColor = Hex("#2E2A28"), belly = Hex("#F2A860"), accent = Hex("#4A3A34"), socks = Hex("#E8883A"),
                    glow = Hex("#FF8C28"), extra = Hex("#FFC060"), face = Hex("#3A2018"), iris = Hex("#2A2018"),
                },
                new BodyZones
                {
                    socks = true, socksY = 0.02f,
                    cracks = true, crackScale = 1.7f, crackWidth = 0.04f, crackPulse = 0.7f,
                },
                new FaceSettings
                {
                    eyeSize = 0.04f, eyeSpacing = 0.1f, eyeHeight = -0.14f, eyeStretch = 1.15f,
                    decals = new List<FaceDecal> { new FaceDecal { kind = DecalKind.Smile, at = new Vector2(0f, -0.24f), size = new Vector2(0.03f, 0f), color = PaletteSlot.Face } },
                },
                new IdleStyle { breathe = 0.016f, breatheSpeed = 2.2f, sway = 2f },
                new List<PlacedPart>
                {
                    Place(spore, new Vector3(0.16f, 0.6f, 0.06f), Vector3.zero, 1f),
                    Place(spore, new Vector3(-0.2f, 0.56f, -0.1f), Vector3.zero, 0.8f),
                    Place(spore, new Vector3(0.04f, 0.68f, -0.16f), Vector3.zero, 0.7f),
                });
            EditorUtility.SetDirty(cap);

            var bloom = Evolution("Cinderbloom", cap, 1.3f,
                new CreaturePalette
                {
                    baseColor = Hex("#3A2A28"), belly = Hex("#F8B870"), accent = Hex("#5A3A30"), socks = Hex("#F0903A"),
                    glow = Hex("#FF6A2A"), extra = Hex("#FFD27A"), face = Hex("#3A2018"), iris = Hex("#2A2018"),
                },
                Place(nubs, new Vector3(0f, 0.46f, 0f), Vector3.zero, 1.25f),
                Place(flame, new Vector3(0f, 0.52f, 0f), Vector3.zero, 0.7f));

            var spores = Attack("SporeDrift", "Spore Drift", Element.Fire, 40, 95, MoveAnimation.Shoot, true);
            var bloomS = FireSpecies("Cinderbloom", bloom, null, 0, 60, 17, 13, 11, spores, LoadMove("BodySlam"), LoadMove("PuffUp"));
            var s = FireSpecies("Cindercap", cap, bloomS, 16, 42, 13, 10, 10, spores, LoadMove("Tackle"), LoadMove("WarmNap"));
            AssetDatabase.SaveAssets();
            return s;
        }

        // ---- 6. Embercrane -> Pyrecrane ----

        /// <summary>
        /// Redraw 10, a rare surprise: an elegant crane-like bird, cream front and face, charcoal back and wings, a red cap,
        /// a crest of flame feathers and a fiery tail fan, dark legs, a long dark beak, sleepy half-lidded eyes.
        /// </summary>
        public static IslingSpecies Embercrane()
        {
            var wing = Kit("WingNub");
            var flame = Kit("FlamePuff");
            var feather = Part("FlameFeather", PartMotion.Flicker, Vector3.up, 0.08f, 1.2f,
                new PartStyle { color = PaletteSlot.Glow, gradient = true, gradientTo = PaletteSlot.Extra, glow = 0.6f, spec = 0.2f, outline = 0.016f },
                Ell(new Vector3(0f, 0.15f, 0f), new Vector3(0.055f, 0.17f, 0.03f), 0f),
                Cone(new Vector3(0f, 0.28f, 0f), new Vector3(0f, 0.38f, 0f), 0.04f, 0.008f, 0.03f));
            Budget(feather, 24, 300);
            var beak = Part("CraneBeak", PartMotion.None, Vector3.up, 0f, 0f,
                new PartStyle { color = PaletteSlot.Accent, spec = 0.5f, outline = 0.018f },
                Cone(Vector3.zero, new Vector3(0f, -0.05f, 0.32f), 0.065f, 0.01f, 0f));
            Budget(beak, 24, 260);

            var body = Shape("Embercrane_Body",
                Ell(new Vector3(0f, -0.08f, -0.06f), new Vector3(0.4f, 0.46f, 0.44f)),
                Cone(new Vector3(0f, 0.2f, 0.16f), new Vector3(0f, 0.56f, 0.24f), 0.17f, 0.14f, 0.12f),
                Sphere(new Vector3(0f, 0.7f, 0.26f), 0.21f, 0.1f),
                Cone(new Vector3(0.13f, -0.48f, 0f), new Vector3(0.13f, -0.8f, 0.02f), 0.05f, 0.045f, 0.04f).Mirror(),
                Ell(new Vector3(0.13f, -0.82f, 0.07f), new Vector3(0.07f, 0.03f, 0.1f), 0.03f).Mirror(),
                Floor(-0.85f));
            var crane = Recipe("Embercrane", body, 0.05f,
                new CreaturePalette
                {
                    baseColor = Hex("#3A3632"), belly = Hex("#F2E6D0"), accent = Hex("#4A4440"), socks = Hex("#2A2826"),
                    glow = Hex("#FF8C28"), extra = Hex("#FFD060"), face = Hex("#2A2018"), iris = Hex("#2A2018"),
                },
                new BodyZones
                {
                    socks = true, socksY = -0.5f,
                    patches = new List<PatternPatch>
                    {
                        new PatternPatch { center = new Vector3(0f, -0.12f, 0.4f), radius = 0.36f, color = PaletteSlot.Belly },
                        new PatternPatch { center = new Vector3(0f, 0.68f, 0.46f), radius = 0.2f, color = PaletteSlot.Belly },
                        new PatternPatch { center = new Vector3(0f, 0.9f, 0.3f), radius = 0.12f, color = PaletteSlot.Glow },
                    },
                },
                new FaceSettings { eyeSize = 0.042f, eyeSpacing = 0.12f, eyeHeight = 0.74f, eyeStretch = 0.7f, decals = new List<FaceDecal>() },
                new IdleStyle { breathe = 0.012f, breatheSpeed = 1.8f, sway = 2f, swaySpeed = 0.8f },
                new List<PlacedPart>
                {
                    Place(beak, new Vector3(0f, 0.68f, 0.44f), new Vector3(8f, 0f, 0f), 1.1f),
                    Place(feather, new Vector3(0f, 0.86f, 0.16f), new Vector3(-35f, 0f, 0f), 1.1f),
                    Place(feather, new Vector3(0.05f, 0.84f, 0.14f), new Vector3(-45f, 0f, -22f), 0.95f, true),
                    Place(feather, new Vector3(0f, -0.02f, -0.46f), new Vector3(-55f, 0f, 0f), 1.4f),
                    Place(feather, new Vector3(0.08f, -0.04f, -0.44f), new Vector3(-50f, 0f, -30f), 1.3f, true),
                    Place(feather, new Vector3(0.14f, -0.08f, -0.4f), new Vector3(-40f, 0f, -60f), 1.15f, true),
                    Place(wing, new Vector3(0.4f, -0.04f, -0.06f), new Vector3(0f, 15f, 15f), 1.3f, true),
                });
            EditorUtility.SetDirty(crane);

            var pyre = Evolution("Pyrecrane", crane, 1.3f,
                new CreaturePalette
                {
                    baseColor = Hex("#2E2A28"), belly = Hex("#FFF0D8"), accent = Hex("#3A3430"), socks = Hex("#2A2826"),
                    glow = Hex("#FF6A20"), extra = Hex("#FFE080"), face = Hex("#2A2018"), iris = Hex("#C04020"),
                },
                Place(feather, new Vector3(0f, 0.9f, 0.1f), new Vector3(-20f, 0f, 0f), 1.4f),
                Place(feather, new Vector3(0f, 0.04f, -0.5f), new Vector3(-70f, 0f, 0f), 1.6f),
                Place(flame, new Vector3(0f, 0.38f, 0f), Vector3.zero, 0.45f, false, 0));

            var wingMove = Attack("FlameWing", "Flame Wing", Element.Fire, 50, 90, MoveAnimation.Lunge);
            var pyreS = FireSpecies("Pyrecrane", pyre, null, 0, 68, 18, 13, 17, wingMove, LoadMove("SteamJet"), LoadMove("PuffUp"));
            var s = FireSpecies("Embercrane", crane, pyreS, 16, 50, 14, 10, 15, wingMove, LoadMove("EmberPuff"), LoadMove("PuffUp"));
            AssetDatabase.SaveAssets();
            return s;
        }
    }
}
