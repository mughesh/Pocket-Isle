using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Diorama.EditorTools.StarterKit;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 9f creatures: Steambara / Onsenbara (concept 07_fire_rest, a Water visitor at the hot springs) and the five
    /// fire evolutions rebuilt as full recipes from sheet 11 (they were modifiers on their base). Like the other kits
    /// this seeds the assets; re-running overwrites them (same files, references survive). Needs the starter, fire and
    /// fire roster kits written first (their parts and moves).
    /// </summary>
    public static class Step9fKit
    {
        const string Root = "Assets/_Project/Data/Creatures";
        const string MoveRoot = "Assets/_Project/Data/Battle/Moves";

        static CreaturePart Kit(string name) => AssetDatabase.LoadAssetAtPath<CreaturePart>($"{Root}/Parts/{name}.asset");
        static IslingSpecies LoadSpecies(string name) => AssetDatabase.LoadAssetAtPath<IslingSpecies>($"{Root}/Species/{name}.asset");
        static MoveData LoadMove(string file) => AssetDatabase.LoadAssetAtPath<MoveData>($"{MoveRoot}/{file}.asset");

        static IslingSpecies MakeSpecies(string name, Element element, CreatureRecipe recipe, IslingSpecies evolvesTo, int level, int hp, int attack, int defense,
                                         int speed, params MoveData[] moves)
        {
            var s = Species(name, element, recipe, evolvesTo, level);
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

        // ---- E. The five fire evolutions, full recipes from sheet 11 ----
        // Each overwrites the evolution's recipe asset (it was a modifier on its base); the species data and moves stay.
        // FireRosterKit calls these, so re-running the 9d lines keeps the new look.

        static FaceSettings Eyes(float size, float spacing, float height, float stretch, params FaceDecal[] decals) =>
            new FaceSettings { eyeSize = size, eyeSpacing = spacing, eyeHeight = height, eyeStretch = stretch, decals = new List<FaceDecal>(decals) };

        static FaceDecal Smile(float y, float size = 0.03f) =>
            new FaceDecal { kind = DecalKind.Smile, at = new Vector2(0f, y), size = new Vector2(size, 0f), color = PaletteSlot.Face };

        static PatternPatch Patch(Vector3 c, float r, PaletteSlot slot, bool mirror = false) =>
            new PatternPatch { center = c, radius = r, color = slot, mirrorX = mirror };

        static IslingSpecies Point(string species, CreatureRecipe recipe)
        {
            var s = LoadSpecies(species);
            if (s != null)
            {
                s.recipe = recipe;
                EditorUtility.SetDirty(s);
            }
            AssetDatabase.SaveAssets();
            return s;
        }

        /// <summary>
        /// Forgehorn: a big barrel-chested charcoal beetle, a small round head in front with thick mandibles, a large
        /// curved glowing forge horn, a chunky anvil plate on its back behind the head, big wing covers, four thick legs.
        /// </summary>
        public static IslingSpecies Forgehorn()
        {
            var cover = Kit("WingCover");
            if (cover == null)
            {
                Debug.LogError("[Step9fKit] WingCover missing: write the 9d Coalbeetle line first.");
                return null;
            }
            var horn = Part("ForgeHorn", PartMotion.None, Vector3.up, 0f, 0f,
                new PartStyle { color = PaletteSlot.Glow, gradient = true, gradientTo = PaletteSlot.Extra, glow = 0.7f, spec = 0.3f, outline = 0.02f },
                Cone(Vector3.zero, new Vector3(0f, 0.2f, 0.09f), 0.11f, 0.085f, 0f),
                Cone(new Vector3(0f, 0.2f, 0.09f), new Vector3(0f, 0.38f, 0.04f), 0.085f, 0.05f, 0.04f),
                Cone(new Vector3(0f, 0.38f, 0.04f), new Vector3(0f, 0.48f, -0.07f), 0.05f, 0.014f, 0.03f));
            Budget(horn, 32, 450);
            var anvil = Part("AnvilPlate", PartMotion.None, Vector3.up, 0f, 0f,
                new PartStyle { color = PaletteSlot.Socks, spec = 0.6f, outline = 0.02f },
                Ell(Vector3.zero, new Vector3(0.13f, 0.06f, 0.1f), 0f),
                Ell(new Vector3(0f, 0.08f, 0f), new Vector3(0.2f, 0.045f, 0.12f), 0.03f),
                Cone(new Vector3(0.18f, 0.08f, 0f), new Vector3(0.3f, 0.085f, 0f), 0.045f, 0.012f, 0.03f));
            Budget(anvil, 32, 400);

            var body = Shape("Forgehorn_Body",
                Ell(new Vector3(0f, -0.04f, -0.12f), new Vector3(0.5f, 0.42f, 0.58f)),
                Ell(new Vector3(0f, -0.12f, 0.46f), new Vector3(0.3f, 0.26f, 0.24f), 0.06f),
                Cone(new Vector3(0.1f, -0.28f, 0.62f), new Vector3(0.14f, -0.34f, 0.74f), 0.055f, 0.03f, 0.03f).Mirror(),
                Cone(new Vector3(0.34f, -0.34f, 0.24f), new Vector3(0.38f, -0.56f, 0.28f), 0.1f, 0.09f, 0.05f).Mirror(),
                Cone(new Vector3(0.36f, -0.34f, -0.34f), new Vector3(0.4f, -0.56f, -0.38f), 0.1f, 0.09f, 0.05f).Mirror(),
                Floor(-0.58f));
            var recipe = Recipe("Forgehorn", body, 0.047f,
                new CreaturePalette
                {
                    baseColor = Hex("#3A3A3A"), belly = Hex("#6A6A6A"), accent = Hex("#4A4A4A"), socks = Hex("#2A2A2A"),
                    glow = Hex("#D4501A"), extra = Hex("#FF8C28"), face = Hex("#1A1A1A"), iris = Hex("#1A1A1A"),
                },
                new BodyZones { patches = new List<PatternPatch> { Patch(new Vector3(0f, -0.46f, 0.12f), 0.36f, PaletteSlot.Belly) } },
                Eyes(0.045f, 0.13f, -0.06f, 1.2f),
                new IdleStyle { breathe = 0.012f, breatheSpeed = 1.8f, sway = 1.2f, swaySpeed = 0.8f },
                new List<PlacedPart>
                {
                    Place(horn, new Vector3(0f, 0.06f, 0.5f), new Vector3(-8f, 0f, 0f), 1.35f),
                    Place(anvil, new Vector3(0f, 0.37f, -0.02f), new Vector3(-4f, 0f, 0f), 1.75f),
                    Place(cover, new Vector3(0.04f, 0.16f, -0.16f), Vector3.zero, 1.35f, true),
                });
            recipe.spec = 0.45f;
            EditorUtility.SetDirty(recipe);
            return Point("Forgehorn", recipe);
        }

        /// <summary>
        /// Emberdillo: a bigger armadillo ball with a pronounced hump under eight large overlapping leaf plates (ember
        /// edges), a small head with a thicker snout and pink nose, bigger round ears, a thick tail with a big leaf tip.
        /// </summary>
        public static IslingSpecies Emberdillo()
        {
            var ear = Kit("RoundEar");
            var plate = Kit("LeafPlate");
            var tail = Kit("LeafTail");
            if (ear == null || plate == null || tail == null)
            {
                Debug.LogError("[Step9fKit] RoundEar / LeafPlate / LeafTail missing: write the 9d Mapadillo line first.");
                return null;
            }
            var body = Shape("Emberdillo_Body",
                Ell(new Vector3(0f, -0.02f, -0.06f), new Vector3(0.48f, 0.43f, 0.54f)),
                Ell(new Vector3(0f, -0.12f, 0.46f), new Vector3(0.2f, 0.18f, 0.19f), 0.1f),
                Cone(new Vector3(0f, -0.15f, 0.58f), new Vector3(0f, -0.19f, 0.76f), 0.09f, 0.045f, 0.05f),
                Cone(new Vector3(0.26f, -0.3f, 0.24f), new Vector3(0.27f, -0.52f, 0.26f), 0.095f, 0.09f, 0.05f).Mirror(),
                Cone(new Vector3(0.26f, -0.3f, -0.32f), new Vector3(0.27f, -0.52f, -0.34f), 0.095f, 0.09f, 0.05f).Mirror(),
                Floor(-0.55f));
            var recipe = Recipe("Emberdillo", body, 0.045f,
                new CreaturePalette
                {
                    baseColor = Hex("#8B6040"), belly = Hex("#F0DFC0"), accent = Hex("#D4888A"), socks = Hex("#8B6040"),
                    glow = Hex("#FF8C28"), extra = Hex("#CC5522"), face = Hex("#2A2018"), iris = Hex("#2A2018"),
                },
                new BodyZones
                {
                    patches = new List<PatternPatch>
                    {
                        Patch(new Vector3(0f, -0.3f, 0.44f), 0.19f, PaletteSlot.Belly),
                        Patch(new Vector3(0f, -0.19f, 0.78f), 0.045f, PaletteSlot.Accent),
                    },
                },
                Eyes(0.042f, 0.1f, -0.08f, 1.15f),
                new IdleStyle { breathe = 0.016f, breatheSpeed = 2.1f, sway = 1.5f },
                new List<PlacedPart>
                {
                    Place(plate, new Vector3(0f, 0.29f, 0.3f), new Vector3(35f, 0f, 0f), 1.6f),
                    Place(plate, new Vector3(0f, 0.42f, 0.04f), new Vector3(4f, 0f, 0f), 1.75f),
                    Place(plate, new Vector3(0f, 0.35f, -0.33f), new Vector3(-36f, 0f, 0f), 1.7f),
                    Place(plate, new Vector3(0f, 0.12f, -0.55f), new Vector3(-68f, 0f, 0f), 1.4f),
                    Place(plate, new Vector3(0.36f, 0.2f, 0.1f), new Vector3(15f, 0f, -55f), 1.5f, true),
                    Place(plate, new Vector3(0.36f, 0.13f, -0.25f), new Vector3(-15f, 0f, -58f), 1.45f, true),
                    Place(ear, new Vector3(0.13f, 0.0f, 0.46f), new Vector3(0f, 0f, -25f), 0.6f, true),
                    Place(tail, new Vector3(0f, -0.22f, -0.58f), new Vector3(10f, 0f, 0f), 1.55f),
                });
            EditorUtility.SetDirty(recipe);
            return Point("Emberdillo", recipe);
        }

        /// <summary>
        /// Cindermole: a bigger egg-shaped mole wider at the base, a chunky head with a miner's lamp (a glowing coal on a
        /// dark band round the head), big round digging mitts (no claws), short back feet, tiny ears, squinty eyes.
        /// </summary>
        public static IslingSpecies Cindermole()
        {
            var ear = Kit("RoundEar");
            if (ear == null)
            {
                Debug.LogError("[Step9fKit] RoundEar missing: write the starter kit first.");
                return null;
            }
            var mitt = Part("MittPaw", PartMotion.Wag, Vector3.right, 8f, 0.6f,
                new PartStyle { color = PaletteSlot.Socks, spec = 0.15f, outline = 0.022f },
                Cone(Vector3.zero, new Vector3(0.08f, -0.12f, 0.06f), 0.075f, 0.07f, 0f),
                Ell(new Vector3(0.13f, -0.2f, 0.1f), new Vector3(0.15f, 0.1f, 0.14f), 0.06f));
            Budget(mitt, 30, 400);
            var band = Part("LampBand", PartMotion.None, Vector3.up, 0f, 0f,
                new PartStyle { color = PaletteSlot.Extra, spec = 0.5f, outline = 0.016f },
                Torus(Vector3.zero, 0.335f, 0.032f, Vector3.zero, 0f));
            Budget(band, 48, 400);
            var coal = Part("LampCoal", PartMotion.None, Vector3.up, 0f, 0f,
                new PartStyle { color = PaletteSlot.Glow, glow = 1f, spec = 0.2f, outline = 0.016f },
                Sphere(Vector3.zero, 0.07f).Bumps(0.01f, 30f));
            Budget(coal, 24, 260);

            var body = Shape("Cindermole_Body",
                Ell(new Vector3(0f, -0.14f, 0f), new Vector3(0.48f, 0.44f, 0.42f)),
                Ell(new Vector3(0f, 0.24f, 0.14f), new Vector3(0.33f, 0.28f, 0.3f), 0.18f),
                Cone(new Vector3(0f, 0.15f, 0.38f), new Vector3(0f, 0.12f, 0.54f), 0.11f, 0.06f, 0.06f),
                Ell(new Vector3(0.22f, -0.54f, 0.14f), new Vector3(0.12f, 0.06f, 0.16f), 0.05f).Mirror(),
                Cone(new Vector3(0f, -0.4f, -0.36f), new Vector3(0f, -0.45f, -0.5f), 0.06f, 0.03f, 0.04f),
                Floor(-0.58f));
            var recipe = Recipe("Cindermole", body, 0.0475f,
                new CreaturePalette
                {
                    baseColor = Hex("#7A6A5A"), belly = Hex("#F0DFC0"), accent = Hex("#C4948A"), socks = Hex("#C4948A"),
                    glow = Hex("#E5641E"), extra = Hex("#3A3232"), face = Hex("#2A2018"), iris = Hex("#2A2018"),
                },
                new BodyZones
                {
                    patches = new List<PatternPatch>
                    {
                        Patch(new Vector3(0f, -0.16f, 0.4f), 0.3f, PaletteSlot.Belly),
                        Patch(new Vector3(0f, 0.12f, 0.56f), 0.05f, PaletteSlot.Accent),
                    },
                },
                Eyes(0.045f, 0.13f, 0.27f, 0.3f, Smile(0.03f)),
                new IdleStyle { breathe = 0.018f, breatheSpeed = 1.9f, sway = 1.5f },
                new List<PlacedPart>
                {
                    Place(mitt, new Vector3(0.32f, -0.04f, 0.26f), new Vector3(0f, 10f, 0f), 1.25f, true),
                    Place(band, new Vector3(0f, 0.34f, 0.13f), new Vector3(-8f, 0f, 0f), 1f),
                    Place(coal, new Vector3(0f, 0.42f, 0.42f), Vector3.zero, 1f),
                    Place(ear, new Vector3(0.24f, 0.44f, 0.04f), new Vector3(0f, 0f, -30f), 0.35f, true),
                });
            EditorUtility.SetDirty(recipe);
            return Point("Cindermole", recipe);
        }

        /// <summary>
        /// Cinderbloom: Cindercap's cap opened into a bloom of six thick charcoal petal chunks with glowing ember cracks
        /// round a bright core, on a taller warm-orange stem body with stubby legs; ember spores drift round it.
        /// The petals are in the body (so the crack zone paints them); the stem is the "socks" zone.
        /// </summary>
        public static IslingSpecies Cinderbloom()
        {
            var spore = Kit("EmberSpore");
            if (spore == null)
            {
                Debug.LogError("[Step9fKit] EmberSpore missing: write the 9d Cindercap line first.");
                return null;
            }
            var core = Part("BloomCore", PartMotion.Flicker, Vector3.up, 0.08f, 0.8f,
                new PartStyle { color = PaletteSlot.Glow, gradient = true, gradientTo = PaletteSlot.Extra, glow = 1.2f, spec = 0f, outline = 0.016f },
                Sphere(Vector3.zero, 0.17f).Bumps(0.014f, 24f));
            Budget(core, 28, 360);

            var prims = new List<SdfPrimitive>
            {
                Cone(new Vector3(0f, -0.2f, 0f), new Vector3(0f, 0.18f, 0f), 0.3f, 0.25f, 0.1f),
                Ell(new Vector3(0f, -0.26f, 0.02f), new Vector3(0.33f, 0.28f, 0.31f), 0.12f),
                Cone(new Vector3(0.13f, -0.46f, 0.04f), new Vector3(0.14f, -0.6f, 0.05f), 0.085f, 0.08f, 0.05f).Mirror(),
                Floor(-0.62f),
            };
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f + 30f;
                Vector3 d = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                prims.Insert(2, Ell(new Vector3(0f, 0.48f, 0f) + d * 0.32f, new Vector3(0.19f, 0.085f, 0.3f), 0.03f, new Vector3(-40f, a, 0f)).Bumps(0.012f, 8f));
            }
            var body = Shape("Cinderbloom_Body", prims.ToArray());
            var recipe = Recipe("Cinderbloom", body, 0.047f,
                new CreaturePalette
                {
                    baseColor = Hex("#2E2E2E"), belly = Hex("#F8B870"), accent = Hex("#5A3A30"), socks = Hex("#D47A28"),
                    glow = Hex("#FF8C28"), extra = Hex("#FFD27A"), face = Hex("#3A2018"), iris = Hex("#2A2018"),
                },
                new BodyZones { socks = true, socksY = 0.34f, cracks = true, crackScale = 1.8f, crackWidth = 0.045f, crackPulse = 0.8f },
                Eyes(0.042f, 0.11f, -0.02f, 1.15f, Smile(-0.14f)),
                new IdleStyle { breathe = 0.018f, breatheSpeed = 1.8f, sway = 2.2f, swaySpeed = 0.9f },
                new List<PlacedPart>
                {
                    Place(core, new Vector3(0f, 0.5f, 0f), Vector3.zero, 1f),
                    Place(spore, new Vector3(0.42f, 0.58f, 0.1f), Vector3.zero, 1.1f),
                    Place(spore, new Vector3(-0.44f, 0.5f, -0.06f), Vector3.zero, 0.9f),
                    Place(spore, new Vector3(0.12f, 0.8f, -0.24f), Vector3.zero, 0.8f),
                    Place(spore, new Vector3(-0.2f, 0.74f, 0.26f), Vector3.zero, 0.75f),
                    Place(spore, new Vector3(0.36f, 0.2f, -0.3f), Vector3.zero, 0.7f),
                });
            recipe.spec = 0.25f;
            EditorUtility.SetDirty(recipe);
            return Point("Cinderbloom", recipe);
        }

        /// <summary>
        /// Pyrecrane: a tall upright crane, cream chest, face and front of a thick neck, charcoal back and wings, a red
        /// cap, a crest of four flame feathers, three ember feathers per wing, a grand seven-feather fan tail, a thick
        /// tapered beak, thick dark legs, half-lidded eyes. Proud and toy-like, not a phoenix.
        /// </summary>
        public static IslingSpecies Pyrecrane()
        {
            var wing = Kit("WingNub");
            var feather = Kit("FlameFeather");
            var beak = Kit("CraneBeak");
            if (wing == null || feather == null || beak == null)
            {
                Debug.LogError("[Step9fKit] WingNub / FlameFeather / CraneBeak missing: write the 9d Embercrane line first.");
                return null;
            }
            var body = Shape("Pyrecrane_Body",
                Ell(new Vector3(0f, -0.1f, -0.06f), new Vector3(0.42f, 0.5f, 0.44f)),
                Cone(new Vector3(0f, 0.25f, 0.12f), new Vector3(0f, 0.62f, 0.2f), 0.19f, 0.16f, 0.12f),
                Sphere(new Vector3(0f, 0.78f, 0.22f), 0.2f, 0.1f),
                Cone(new Vector3(0.14f, -0.52f, 0f), new Vector3(0.14f, -0.88f, 0.02f), 0.065f, 0.06f, 0.04f).Mirror(),
                Ell(new Vector3(0.14f, -0.9f, 0.08f), new Vector3(0.08f, 0.035f, 0.11f), 0.03f).Mirror(),
                Floor(-0.93f));
            var recipe = Recipe("Pyrecrane", body, 0.065f,
                new CreaturePalette
                {
                    baseColor = Hex("#3A3A3A"), belly = Hex("#F0E6D0"), accent = Hex("#5A5048"), socks = Hex("#3A3A3A"),
                    glow = Hex("#E5641E"), extra = Hex("#CC2222"), face = Hex("#2A2018"), iris = Hex("#2A2018"),
                },
                new BodyZones
                {
                    socks = true, socksY = -0.55f,
                    patches = new List<PatternPatch>
                    {
                        Patch(new Vector3(0f, -0.1f, 0.4f), 0.38f, PaletteSlot.Belly),
                        Patch(new Vector3(0f, 0.42f, 0.3f), 0.17f, PaletteSlot.Belly),
                        Patch(new Vector3(0f, 0.74f, 0.38f), 0.19f, PaletteSlot.Belly),
                        Patch(new Vector3(0f, 0.97f, 0.2f), 0.14f, PaletteSlot.Extra),
                    },
                },
                Eyes(0.042f, 0.12f, 0.8f, 0.65f),
                new IdleStyle { breathe = 0.012f, breatheSpeed = 1.6f, sway = 1.6f, swaySpeed = 0.7f },
                new List<PlacedPart>
                {
                    Place(beak, new Vector3(0f, 0.75f, 0.4f), new Vector3(8f, 0f, 0f), 1.35f),
                    Place(feather, new Vector3(0f, 0.95f, 0.13f), new Vector3(-28f, 0f, 0f), 1.15f),
                    Place(feather, new Vector3(0.06f, 0.93f, 0.1f), new Vector3(-40f, 0f, -22f), 1f, true),
                    Place(feather, new Vector3(0f, 0.92f, 0.04f), new Vector3(-58f, 0f, 0f), 0.95f),
                    Place(wing, new Vector3(0.38f, 0.02f, -0.1f), new Vector3(0f, 25f, 5f), 1.5f, true),
                    Place(feather, new Vector3(0.44f, -0.14f, -0.3f), new Vector3(-120f, 0f, -20f), 0.8f, true),
                    Place(feather, new Vector3(0.42f, -0.24f, -0.26f), new Vector3(-130f, 0f, -15f), 0.75f, true),
                    Place(feather, new Vector3(0.4f, -0.32f, -0.2f), new Vector3(-140f, 0f, -10f), 0.7f, true),
                    Place(feather, new Vector3(0f, -0.04f, -0.5f), new Vector3(-62f, 0f, 0f), 2.05f),
                    Place(feather, new Vector3(0.06f, -0.05f, -0.49f), new Vector3(-60f, 0f, -22f), 2f, true),
                    Place(feather, new Vector3(0.12f, -0.07f, -0.47f), new Vector3(-56f, 0f, -44f), 1.9f, true),
                    Place(feather, new Vector3(0.17f, -0.1f, -0.44f), new Vector3(-50f, 0f, -66f), 1.75f, true),
                });
            EditorUtility.SetDirty(recipe);
            return Point("Pyrecrane", recipe);
        }

        // ---- D. The explorer (concept 12, Option A: teal hood) ----

        /// <summary>Leg length below the body, shape units: the baked rig is lifted by this so the boots stand on the floor.</summary>
        public const float ExplorerLegDrop = 0.3f;

        /// <summary>
        /// A gender-neutral kid: big cream head and a small teal tunic (the body; the tunic is the "socks" zone below the
        /// chin), a pointed teal hood with a cream lining (its own rigid piece, the point bent back: the top-down cue),
        /// two arm nubs with cream hands, two leg nubs with brown boots, a brown backpack and a small lantern on a pivot
        /// glowing the Spirit's colour. Eyes are flat face marks, cheeks painted patches (cheap). About 4 cm tall at zoom 1, under 3k
        /// triangles. Motion lives in <see cref="ExplorerView"/> (it finds the parts by name).
        /// </summary>
        public static CreatureRecipe Explorer()
        {
            var hood = Part("ExplorerHood", PartMotion.None, Vector3.up, 0f, 0f,
                new PartStyle
                {
                    color = PaletteSlot.Socks, spec = 0.15f, outline = 0.02f,
                    inner = true, innerColor = PaletteSlot.Base, innerDirection = Vector3.forward, innerAt = 0.55f,
                },
                Sphere(new Vector3(0f, 0.02f, -0.03f), 0.43f, 0f),
                Cone(new Vector3(0f, 0.3f, -0.12f), new Vector3(0.06f, 0.66f, -0.36f), 0.2f, 0.05f, 0.1f),
                Ell(new Vector3(0f, -0.2f, -0.16f), new Vector3(0.34f, 0.2f, 0.24f), 0.08f),
                Sphere(new Vector3(0f, -0.01f, 0.06f), 0.355f, 0.03f).Sub(),
                Ell(new Vector3(0f, -0.04f, 0.4f), new Vector3(0.27f, 0.3f, 0.26f), 0.05f).Sub(),
                Cut(new Vector3(0f, -0.3f, 0f), Vector3.down, 0.03f));
            Budget(hood, 48, 480);
            // The cream lining round the face opening (also hides the shell's cut edge).
            var trim = Part("HoodTrim", PartMotion.None, Vector3.up, 0f, 0f,
                new PartStyle { color = PaletteSlot.Base, spec = 0.1f, outline = 0.018f },
                Torus(Vector3.zero, 0.3f, 0.045f, new Vector3(90f, 0f, 0f), 0f));
            Budget(trim, 40, 180);
            var arm = Part("ExplorerArm", PartMotion.None, Vector3.right, 0f, 0f,
                new PartStyle { color = PaletteSlot.Socks, spec = 0.1f, outline = 0.02f, tip = true, tipColor = PaletteSlot.Base, tipAxis = Vector3.down, tipAt = 0.16f },
                Cone(Vector3.zero, new Vector3(0f, -0.19f, 0.02f), 0.075f, 0.068f, 0f));
            Budget(arm, 22, 100);
            var leg = Part("ExplorerLeg", PartMotion.None, Vector3.right, 0f, 0f,
                new PartStyle { color = PaletteSlot.Base, spec = 0.1f, outline = 0.02f, tip = true, tipColor = PaletteSlot.Extra, tipAxis = Vector3.down, tipAt = 0.18f },
                Cone(Vector3.zero, new Vector3(0f, -0.24f, 0f), 0.07f, 0.075f, 0f),
                Ell(new Vector3(0f, -0.25f, 0.03f), new Vector3(0.095f, 0.065f, 0.11f), 0.04f));
            Budget(leg, 24, 120);
            var pack = Part("Backpack", PartMotion.None, Vector3.up, 0f, 0f,
                new PartStyle { color = PaletteSlot.Extra, spec = 0.25f, outline = 0.02f },
                Ell(Vector3.zero, new Vector3(0.21f, 0.22f, 0.12f), 0f),
                Ell(new Vector3(0f, 0.13f, -0.04f), new Vector3(0.21f, 0.08f, 0.11f), 0.03f),
                Ell(new Vector3(0f, -0.06f, -0.11f), new Vector3(0.11f, 0.08f, 0.05f), 0.03f));
            Budget(pack, 26, 170);
            var lantern = Part("ExplorerLantern", PartMotion.None, Vector3.right, 0f, 0f,
                new PartStyle { color = PaletteSlot.Glow, glow = 0.9f, spec = 0.3f, outline = 0.016f, tip = true, tipColor = PaletteSlot.Extra, tipAxis = Vector3.up, tipAt = -0.075f },
                Torus(new Vector3(0f, -0.02f, 0f), 0.03f, 0.01f, new Vector3(90f, 0f, 0f), 0f),
                Ell(new Vector3(0f, -0.06f, 0f), new Vector3(0.055f, 0.02f, 0.055f), 0.02f),
                Cone(new Vector3(0f, -0.08f, 0f), new Vector3(0f, -0.16f, 0f), 0.05f, 0.045f, 0.02f),
                Ell(new Vector3(0f, -0.17f, 0f), new Vector3(0.055f, 0.018f, 0.055f), 0.02f));
            Budget(lantern, 26, 150);

            var body = Shape("Explorer_Body",
                Sphere(new Vector3(0f, 0.28f, 0.02f), 0.34f, 0f),
                Cone(new Vector3(0f, -0.02f, 0f), new Vector3(0f, -0.4f, 0f), 0.19f, 0.27f, 0.08f),
                Floor(-0.45f));
            // Mugesh, Oct 26: 2 cm "fits on a creature's hand"; now about a starter's height (~4 cm with the hood).
            var recipe = Recipe("Explorer", body, 0.0245f,
                new CreaturePalette
                {
                    baseColor = Hex("#F5EDDA"), belly = Hex("#F5EDDA"), accent = Hex("#EBA28E"), socks = Hex("#2A7A7A"),
                    glow = Hex("#FFC45C"), extra = Hex("#6B4A28"), face = Hex("#2A2018"), iris = Hex("#2A2018"),
                },
                new BodyZones
                {
                    socks = true, socksY = -0.02f,
                    patches = new List<PatternPatch> { new PatternPatch { center = new Vector3(0.19f, 0.19f, 0.28f), radius = 0.05f, color = PaletteSlot.Accent, mirrorX = true } },
                },
                new FaceSettings
                {
                    eyeSize = 0f,
                    decals = new List<FaceDecal>
                    {
                        new FaceDecal { kind = DecalKind.Disc, at = new Vector2(0.11f, 0.3f), size = new Vector2(0.045f, 0.06f), color = PaletteSlot.Face, mirrorX = true },
                    },
                },
                new IdleStyle { breathe = 0.01f, breatheSpeed = 2f },
                new List<PlacedPart>
                {
                    Place(hood, new Vector3(0f, 0.3f, 0f), Vector3.zero, 1f),
                    Place(trim, new Vector3(0f, -0.03f, 0.21f), new Vector3(-8f, 0f, 0f), 1f, false, 0),
                    Place(arm, new Vector3(0.2f, -0.1f, 0f), new Vector3(0f, 0f, 12f), 1f, true),
                    Place(leg, new Vector3(0.1f, -0.42f, 0f), Vector3.zero, 1f, true),
                    Place(pack, new Vector3(0f, -0.17f, -0.27f), new Vector3(-6f, 0f, 0f), 1f),
                    Place(lantern, new Vector3(-0.3f, -0.1f, -0.2f), Vector3.zero, 1.1f),
                });
            recipe.resolution = 56;
            recipe.triangleBudget = 1200;
            recipe.outline = 0.03f;
            recipe.spec = 0.15f;
            EditorUtility.SetDirty(recipe);
            AssetDatabase.SaveAssets();
            return recipe;
        }

        // ---- C. Steambara -> Onsenbara ----

        /// <summary>
        /// A relaxed capybara sitting in a steam cloud: wide low body, a flat-topped boxy head with a blunt snout,
        /// half-closed eyes, little round ears and an orange fruit (with a leaf) on its head. Onsenbara (Lv 14) is bigger,
        /// soaks in a wooden hot-spring tub ring and wears a folded towel under its fruit. Water: Spring Splash breaks
        /// the Keeper's crown shield in one hit.
        /// </summary>
        public static IslingSpecies Steambara()
        {
            var ear = Kit("RoundEar");
            if (ear == null)
            {
                Debug.LogError("[Step9fKit] RoundEar missing: write the starter kit first.");
                return null;
            }
            var cloud = Part("SteamCloud", PartMotion.Bob, Vector3.up, 0.02f, 0.45f,
                new PartStyle { color = PaletteSlot.Glow, glow = 0.25f, spec = 0f, outline = 0.02f },
                Ell(new Vector3(0f, 0f, 0.05f), new Vector3(0.34f, 0.15f, 0.3f), 0f),
                Ell(new Vector3(0.3f, 0.02f, -0.08f), new Vector3(0.22f, 0.14f, 0.24f), 0.06f).Mirror(),
                Ell(new Vector3(0f, 0.03f, -0.32f), new Vector3(0.26f, 0.14f, 0.18f), 0.06f),
                Ell(new Vector3(0.2f, 0.05f, 0.26f), new Vector3(0.18f, 0.12f, 0.16f), 0.06f).Mirror());
            Budget(cloud, 40, 900);
            var fruit = Part("HeadFruit", PartMotion.None, Vector3.up, 0f, 0f,
                new PartStyle { color = PaletteSlot.Extra, spec = 0.5f, outline = 0.016f, tip = true, tipColor = PaletteSlot.Accent, tipAxis = Vector3.up, tipAt = 0.145f },
                Sphere(new Vector3(0f, 0.065f, 0f), 0.07f).Bumps(0.003f, 60f),
                Ell(new Vector3(0.04f, 0.165f, 0f), new Vector3(0.075f, 0.02f, 0.038f), 0.02f, new Vector3(0f, 0f, -20f)));
            Budget(fruit, 28, 300);
            var towel = Part("FoldedTowel", PartMotion.None, Vector3.up, 0f, 0f,
                new PartStyle { color = PaletteSlot.Belly, spec = 0.05f, outline = 0.016f },
                Ell(new Vector3(0f, 0.025f, 0f), new Vector3(0.16f, 0.03f, 0.12f), 0f),
                Ell(new Vector3(0f, 0.055f, -0.02f), new Vector3(0.14f, 0.025f, 0.1f), 0.03f));
            Budget(towel, 32, 400);
            var tub = Part("TubRing", PartMotion.None, Vector3.up, 0f, 0f,
                new PartStyle { color = PaletteSlot.Socks, spec = 0.2f, outline = 0.02f },
                Torus(Vector3.zero, 0.5f, 0.06f, Vector3.zero, 0f).Bumps(0.008f, 12f),
                Torus(new Vector3(0f, 0.07f, 0f), 0.5f, 0.03f, Vector3.zero, 0.03f));
            Budget(tub, 56, 900);

            var body = Shape("Steambara_Body",
                Ell(new Vector3(0f, -0.2f, -0.12f), new Vector3(0.44f, 0.34f, 0.5f)),
                Ell(new Vector3(0f, 0.16f, 0.24f), new Vector3(0.31f, 0.28f, 0.32f), 0.16f),
                Ell(new Vector3(0f, 0.08f, 0.5f), new Vector3(0.22f, 0.17f, 0.16f), 0.1f),
                Cut(new Vector3(0f, 0.38f, 0f), Vector3.up, 0.08f),
                Ell(new Vector3(0.22f, -0.46f, 0.24f), new Vector3(0.1f, 0.07f, 0.12f), 0.05f).Mirror(),
                Floor(-0.52f));
            var bodyZones = new BodyZones
            {
                patches = new List<PatternPatch>
                {
                    new PatternPatch { center = new Vector3(0f, -0.2f, 0.36f), radius = 0.24f, color = PaletteSlot.Belly },
                    new PatternPatch { center = new Vector3(0f, 0.04f, 0.64f), radius = 0.15f, color = PaletteSlot.Belly },
                },
            };
            var face = new FaceSettings
            {
                eyeSize = 0.045f, eyeSpacing = 0.15f, eyeHeight = 0.24f, eyeStretch = 0.45f,
                decals = new List<FaceDecal>
                {
                    new FaceDecal { kind = DecalKind.Disc, at = new Vector2(0.05f, 0.13f), size = new Vector2(0.02f, 0.014f), color = PaletteSlot.Face, mirrorX = true },
                    new FaceDecal { kind = DecalKind.Smile, at = new Vector2(0f, 0.04f), size = new Vector2(0.03f, 0f), color = PaletteSlot.Face },
                },
            };
            var bara = Recipe("Steambara", body, 0.034f,
                new CreaturePalette
                {
                    baseColor = Hex("#8B6840"), belly = Hex("#F0DFC0"), accent = Hex("#6BA868"), socks = Hex("#6E5030"),
                    glow = Hex("#F0F0F0"), extra = Hex("#E58A32"), face = Hex("#2A2018"), iris = Hex("#2A2018"),
                },
                bodyZones, face,
                new IdleStyle { breathe = 0.012f, breatheSpeed = 1.6f, sway = 1f, swaySpeed = 0.7f },
                new List<PlacedPart>
                {
                    Place(cloud, new Vector3(0f, -0.36f, -0.04f), Vector3.zero, 1.3f),
                    Place(ear, new Vector3(0.2f, 0.36f, 0.14f), new Vector3(0f, 0f, -30f), 0.42f, true),
                    Place(fruit, new Vector3(0f, 0.37f, 0.22f), new Vector3(0f, 20f, 0f), 1f),
                });
            bara.spec = 0.2f;
            EditorUtility.SetDirty(bara);

            // Onsenbara: the same body, bigger, in a tub ring with a towel; its own recipe (no extra-only modifiers).
            var onsen = Recipe("Onsenbara", body, 0.034f * 1.3f,
                new CreaturePalette
                {
                    baseColor = Hex("#7E5C38"), belly = Hex("#F6EAD2"), accent = Hex("#5E9C5E"), socks = Hex("#A0703C"),
                    glow = Hex("#F4F6F8"), extra = Hex("#F0943A"), face = Hex("#2A2018"), iris = Hex("#2A2018"),
                },
                bodyZones, face,
                new IdleStyle { breathe = 0.012f, breatheSpeed = 1.4f, sway = 1f, swaySpeed = 0.6f },
                new List<PlacedPart>
                {
                    Place(cloud, new Vector3(0f, -0.4f, -0.04f), Vector3.zero, 1.25f),
                    Place(tub, new Vector3(0f, -0.24f, -0.08f), new Vector3(0f, 0f, 0f), 1.32f),
                    Place(ear, new Vector3(0.2f, 0.36f, 0.14f), new Vector3(0f, 0f, -30f), 0.42f, true),
                    Place(towel, new Vector3(0f, 0.37f, 0.2f), new Vector3(0f, 12f, 0f), 1.15f),
                    Place(fruit, new Vector3(0.02f, 0.44f, 0.2f), new Vector3(0f, 20f, 0f), 0.95f),
                });
            onsen.spec = 0.2f;
            EditorUtility.SetDirty(onsen);

            var splash = Attack("SpringSplash", "Spring Splash", Element.Water, 45, 95, MoveAnimation.Shoot, true);
            var soak = Attack("HotSoak", "Hot Soak", Element.Water, 60, 90, MoveAnimation.Slam);
            var onsenS = MakeSpecies("Onsenbara", Element.Water, onsen, null, 0, 64, 15, 16, 9, soak, splash, LoadMove("WarmNap"));
            var s = MakeSpecies("Steambara", Element.Water, bara, onsenS, 14, 46, 11, 12, 8, splash, LoadMove("Tackle"), LoadMove("WarmNap"));
            AssetDatabase.SaveAssets();
            return s;
        }
    }
}
