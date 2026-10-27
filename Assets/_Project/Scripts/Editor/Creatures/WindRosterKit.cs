using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Diorama.EditorTools.StarterKit;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 10c: the wind roster (concept sheets 15 and 16, Concepts_03_Summary.md) and the two missing starter evolutions
    /// (Pufflet -> Nimbulpaca, sheet 16; Pebbloop -> Riverloom, sheet 14). Full recipes, rigid pivot parts (no bones):
    /// Kitelet's bow tail is a short chain of wagging beads, pinwheels / rotors / propellers / the waterwheel spin, the
    /// weathervane ears flick. Three moves each (a Wind attack, a Neutral one, a support). Evolutions at Lv 14 (the
    /// starters, like Kilnpot -> Kilnhearth) and 16 (commons), the rare at 18. Re-running overwrites (same files).
    /// </summary>
    public static class WindRosterKit
    {
        const string Root = "Assets/_Project/Data/Creatures";
        const string MoveRoot = "Assets/_Project/Data/Battle/Moves";

        internal static IslingSpecies LoadSpecies(string name) => AssetDatabase.LoadAssetAtPath<IslingSpecies>($"{Root}/Species/{name}.asset");
        internal static MoveData LoadMove(string file) => AssetDatabase.LoadAssetAtPath<MoveData>($"{MoveRoot}/{file}.asset");

        internal static void Budget(CreaturePart part, int resolution, int triangles)
        {
            part.resolution = resolution;
            part.triangleBudget = triangles;
            EditorUtility.SetDirty(part);
        }

        internal static MoveData Attack(string file, string name, Element element, int power, int accuracy, MoveAnimation animation, bool arc = false)
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

        static IslingSpecies WindSpecies(string name, CreatureRecipe recipe, IslingSpecies evolvesTo, int level, int hp, int attack, int defense, int speed,
                                         params MoveData[] moves)
        {
            var s = Species(name, Element.Wind, recipe, evolvesTo, level);
            s.baseStats = new BaseStats { hp = hp, attack = attack, defense = defense, speed = speed };
            s.moves = moves;
            EditorUtility.SetDirty(s);
            return s;
        }

        internal static PartStyle Style(PaletteSlot slot, float spec = 0.25f, float outline = 0.022f) => new PartStyle { color = slot, spec = spec, outline = outline };

        internal static FaceSettings Face(float size, float spacing, float height, float stretch = 1.2f, bool smile = true, float smileY = float.NaN) => new FaceSettings
        {
            eyeSize = size, eyeSpacing = spacing, eyeHeight = height, eyeStretch = stretch,
            decals = smile
                ? new List<FaceDecal> { new FaceDecal { kind = DecalKind.Smile, at = new Vector2(0f, float.IsNaN(smileY) ? height - 0.1f : smileY), size = new Vector2(0.03f, 0f), color = PaletteSlot.Face } }
                : new List<FaceDecal>(),
        };

        internal static BodyZones Patches(params (Vector3 c, float r, PaletteSlot slot)[] patches)
        {
            var z = new BodyZones { patches = new List<PatternPatch>() };
            foreach (var (c, r, slot) in patches) z.patches.Add(new PatternPatch { center = c, radius = r, color = slot });
            return z;
        }

        // ---- shared parts ----

        /// <summary>A four-petal pinwheel facing +Z (spins about Z): thick rounded petals round a hemisphere pin.</summary>
        static CreaturePart Pinwheel(string name, float size, float speed)
        {
            var prims = new List<SdfPrimitive>();
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f + 20f;
                var d = Quaternion.Euler(0f, 0f, a) * Vector3.right;
                var side = Quaternion.Euler(0f, 0f, a + 90f) * Vector3.right;
                prims.Add(Ell(d * size * 0.5f + side * size * 0.12f, new Vector3(size * 0.45f, size * 0.2f, size * 0.07f), i == 0 ? 0f : 0.03f, new Vector3(0f, 0f, a)));
            }
            var p = Part(name, PartMotion.Spin, Vector3.forward, 0f, speed, new PartStyle { color = PaletteSlot.Extra, spec = 0.5f, outline = 0.02f, tip = true, tipColor = PaletteSlot.Glow, tipAxis = Vector3.right, tipAt = 0.6f }, prims.ToArray());
            Budget(p, 36, 700);
            return p;
        }

        static CreaturePart Pin(string name, float r)
        {
            var p = Part(name, PartMotion.None, Vector3.up, 0f, 0f, Style(PaletteSlot.Accent, 0.5f),
                Sphere(new Vector3(0f, 0f, 0.02f), r, 0f),
                Cone(new Vector3(0f, 0f, -0.12f), Vector3.zero, r * 0.5f, r * 0.5f, 0.02f));
            Budget(p, 24, 300);
            return p;
        }

        internal static CreaturePart PegFoot(string name, float r, float length, PaletteSlot slot)
        {
            var p = Part(name, PartMotion.None, Vector3.up, 0f, 0f, Style(slot, 0.2f),
                Cone(Vector3.zero, new Vector3(0f, -length, 0f), r, r * 0.95f, 0f),
                Ell(new Vector3(0f, -length, r * 0.3f), new Vector3(r * 1.1f, r * 0.55f, r * 1.3f), 0.03f));
            Budget(p, 24, 260);
            return p;
        }

        internal static CreaturePart RoundEar(string name, Vector3 size, PartMotion motion = PartMotion.Flick)
        {
            var p = Part(name, motion, Vector3.forward, 10f, 0.5f,
                new PartStyle { color = PaletteSlot.Base, spec = 0.2f, outline = 0.02f, inner = true, innerColor = PaletteSlot.Belly, innerDirection = Vector3.forward, innerAt = 0.4f },
                Ell(new Vector3(0f, size.y, 0f), size, 0f));
            Budget(p, 26, 320);
            return p;
        }

        /// <summary>A samara (maple seed) wing: a thick bevelled slab with a rounded tip, its root at the origin, along +X.</summary>
        static SdfPrimitive[] SamaraWing(float length, float width)
        {
            return new[]
            {
                Ell(new Vector3(length * 0.55f, 0f, 0f), new Vector3(length * 0.5f, width * 0.5f, 0.035f), 0f, new Vector3(0f, 0f, 8f)),
                Cone(Vector3.zero, new Vector3(length * 0.25f, 0.02f, 0f), 0.06f, 0.04f, 0.05f),
            };
        }

        // ================= 1. Kitelet -> Kitewyrm =================

        /// <summary>
        /// Kitelet: a puffy diamond kite with a little face low on its front, two peg feet and a long tail of chunky bead
        /// bows that sways (a chain of wagging pivots). Kitewyrm: a box-kite dragon of four block segments, coral streamers.
        /// </summary>
        public static IslingSpecies Kitelet()
        {
            var foot = PegFoot("KitePegFoot", 0.05f, 0.1f, PaletteSlot.Socks);
            var bead = Part("RibbonBead", PartMotion.Wag, Vector3.forward, 14f, 0.7f, Style(PaletteSlot.Extra, 0.35f, 0.02f),
                Ell(new Vector3(-0.07f, -0.05f, 0f), new Vector3(0.07f, 0.05f, 0.045f), 0f, new Vector3(0f, 0f, 25f)),
                Ell(new Vector3(0.07f, -0.05f, 0f), new Vector3(0.07f, 0.05f, 0.045f), 0.02f, new Vector3(0f, 0f, -25f)),
                Sphere(new Vector3(0f, -0.05f, 0f), 0.04f, 0.02f),
                Cone(Vector3.zero, new Vector3(0f, -0.14f, 0f), 0.018f, 0.018f, 0.01f));
            Budget(bead, 26, 360);
            var body = Shape("Kitelet_Body",
                Box(new Vector3(0f, 0.15f, 0f), new Vector3(0.36f, 0.36f, 0.16f), 0.1f, 0f, new Vector3(0f, 0f, 45f)),
                Ell(new Vector3(0f, 0.15f, 0.06f), new Vector3(0.3f, 0.42f, 0.12f), 0.08f),
                Cone(new Vector3(0.1f, -0.25f, 0f), new Vector3(0.1f, -0.38f, 0.02f), 0.05f, 0.045f, 0.04f).Mirror(),
                Floor(-0.4f));
            var kitelet = Recipe("Kitelet", body, 0.042f,
                new CreaturePalette
                {
                    baseColor = Hex("#8ED0E8"), belly = Hex("#F4F8FA"), accent = Hex("#FFF6E5"), socks = Hex("#708090"),
                    glow = Hex("#F88379"), extra = Hex("#FCE881"), face = Hex("#2A2018"), iris = Hex("#2A2018"),
                },
                Patches((new Vector3(0.18f, 0.32f, 0.16f), 0.16f, PaletteSlot.Belly), (new Vector3(-0.18f, 0.32f, 0.16f), 0.16f, PaletteSlot.Accent),
                        (new Vector3(0.1f, -0.03f, 0.18f), 0.045f, PaletteSlot.Glow), (new Vector3(-0.1f, -0.03f, 0.18f), 0.045f, PaletteSlot.Glow)),
                Face(0.045f, 0.09f, 0.08f, 1.2f, true, -0.02f),
                new IdleStyle { breathe = 0.01f, breatheSpeed = 1.8f, hover = 0.004f, bob = 0.002f, sway = 3f },
                new List<PlacedPart>
                {
                    Place(foot, new Vector3(0.1f, -0.3f, 0.02f), Vector3.zero, 1f, true),
                    Place(bead, new Vector3(0f, -0.1f, -0.16f), new Vector3(0f, 0f, 0f), 1f),
                    Place(bead, new Vector3(0f, -0.14f, 0f), new Vector3(0f, 0f, 12f), 0.95f, false, 1),
                    Place(bead, new Vector3(0f, -0.14f, 0f), new Vector3(0f, 0f, -12f), 0.9f, false, 2),
                    Place(bead, new Vector3(0f, -0.14f, 0f), new Vector3(0f, 0f, 10f), 0.85f, false, 3),
                });
            kitelet.spec = 0.3f;
            // The tail hangs behind the diamond, sloping down to the ground.
            kitelet.parts[1] = Place(bead, new Vector3(0.08f, -0.2f, -0.1f), new Vector3(-15f, 0f, 55f), 1.1f);
            EditorUtility.SetDirty(kitelet);

            var horn = Part("KiteHorn", PartMotion.None, Vector3.up, 0f, 0f, Style(PaletteSlot.Accent, 0.3f),
                Cone(Vector3.zero, new Vector3(0f, 0.12f, -0.04f), 0.035f, 0.015f, 0f));
            Budget(horn, 22, 200);
            var streamer = Part("KiteStreamer", PartMotion.Flap, Vector3.forward, 12f, 0.8f, Style(PaletteSlot.Glow, 0.3f),
                Ell(new Vector3(0.12f, -0.02f, 0f), new Vector3(0.14f, 0.05f, 0.03f), 0f, new Vector3(0f, 0f, -15f)),
                Ell(new Vector3(0.26f, -0.07f, 0f), new Vector3(0.08f, 0.045f, 0.028f), 0.03f, new Vector3(0f, 0f, -30f)));
            Budget(streamer, 26, 300);
            var fin = Part("KiteFin", PartMotion.Wag, Vector3.up, 12f, 0.6f, Style(PaletteSlot.Socks, 0.3f),
                Box(new Vector3(0f, 0.04f, -0.08f), new Vector3(0.02f, 0.1f, 0.1f), 0.02f, 0f, new Vector3(45f, 0f, 0f)));
            Budget(fin, 24, 260);
            var wyrmBody = Shape("Kitewyrm_Body",
                Box(new Vector3(0f, 0.22f, 0.32f), new Vector3(0.2f, 0.2f, 0.17f), 0.06f, 0f),           // head block
                Box(new Vector3(0f, 0.02f, 0.06f), new Vector3(0.2f, 0.17f, 0.16f), 0.06f, 0.03f),      // chest block
                Box(new Vector3(0f, 0.06f, -0.24f), new Vector3(0.18f, 0.15f, 0.15f), 0.06f, 0.03f),    // middle block
                Box(new Vector3(0f, 0.16f, -0.5f), new Vector3(0.14f, 0.12f, 0.13f), 0.05f, 0.03f),     // tail block
                Cone(new Vector3(0.14f, -0.12f, 0.08f), new Vector3(0.15f, -0.3f, 0.12f), 0.05f, 0.045f, 0.03f).Mirror(),
                Cone(new Vector3(0.13f, -0.08f, -0.26f), new Vector3(0.14f, -0.3f, -0.28f), 0.05f, 0.045f, 0.03f).Mirror(),
                Floor(-0.32f));
            var wyrm = Recipe("Kitewyrm", wyrmBody, 0.06f,
                new CreaturePalette
                {
                    baseColor = Hex("#7EBEE0"), belly = Hex("#FFF4E0"), accent = Hex("#E8D5B5"), socks = Hex("#6EAFC8"),
                    glow = Hex("#FA8572"), extra = Hex("#FCE881"), face = Hex("#2A2018"), iris = Hex("#2A2018"),
                },
                Patches((new Vector3(0f, 0.12f, 0.5f), 0.14f, PaletteSlot.Belly), (new Vector3(0f, -0.02f, 0.24f), 0.15f, PaletteSlot.Belly),
                        (new Vector3(0f, 0.0f, -0.08f), 0.13f, PaletteSlot.Belly)),
                Face(0.045f, 0.1f, 0.28f, 1.2f, true, 0.16f),
                new IdleStyle { breathe = 0.012f, breatheSpeed = 1.6f, hover = 0.003f, sway = 4f },
                new List<PlacedPart>
                {
                    Place(horn, new Vector3(0.1f, 0.4f, 0.3f), new Vector3(0f, 0f, -15f), 1f, true),
                    Place(horn, new Vector3(0.06f, 0.4f, 0.22f), new Vector3(-20f, 0f, -10f), 0.8f, true),
                    Place(streamer, new Vector3(0.2f, 0.12f, 0.16f), new Vector3(0f, -20f, 0f), 1f, true),
                    Place(fin, new Vector3(0f, 0.26f, -0.6f), Vector3.zero, 1.2f),
                });
            wyrm.spec = 0.3f;
            EditorUtility.SetDirty(wyrm);

            var lash = Attack("RibbonLash", "Ribbon Lash", Element.Wind, 40, 95, MoveAnimation.Lunge);
            var wyrmS = WindSpecies("Kitewyrm", wyrm, null, 0, 60, 16, 13, 16, lash, LoadMove("BodySlam"), LoadMove("PuffUp"));
            var s = WindSpecies("Kitelet", kitelet, wyrmS, 16, 42, 11, 9, 14, lash, LoadMove("Tackle"), LoadMove("PuffUp"));
            AssetDatabase.SaveAssets();
            return s;
        }

        // ================= 2. Whirlby -> Windwhirl =================

        /// <summary>Whirlby: a chubby honey hamster with a pinwheel tail on a peg. Windwhirl: bigger, two pinwheels on back posts.</summary>
        public static IslingSpecies Whirlby()
        {
            var ear = RoundEar("HamsterEar", new Vector3(0.07f, 0.07f, 0.03f));
            var wheel = Pinwheel("HamsterPinwheel", 0.34f, 1.4f);
            var pin = Pin("PinwheelPin", 0.045f);
            var body = Shape("Whirlby_Body",
                Ell(new Vector3(0f, -0.08f, 0f), new Vector3(0.36f, 0.34f, 0.34f)),
                Sphere(new Vector3(0f, 0.28f, 0.06f), 0.26f, 0.12f),
                Ell(new Vector3(0.15f, 0.2f, 0.2f), new Vector3(0.1f, 0.08f, 0.08f), 0.06f).Mirror(),
                Ell(new Vector3(0.14f, -0.38f, 0.14f), new Vector3(0.08f, 0.05f, 0.1f), 0.05f).Mirror(),
                Ell(new Vector3(0.14f, -0.1f, 0.3f), new Vector3(0.06f, 0.05f, 0.06f), 0.04f).Mirror(),
                Floor(-0.42f));
            var whirlby = Recipe("Whirlby", body, 0.038f,
                new CreaturePalette
                {
                    baseColor = Hex("#E8A850"), belly = Hex("#FBF2DB"), accent = Hex("#E25241"), socks = Hex("#FBF2DB"),
                    glow = Hex("#A3E5BA"), extra = Hex("#86D2D9"), face = Hex("#2A2018"), iris = Hex("#2A2018"),
                },
                Patches((new Vector3(0f, -0.1f, 0.3f), 0.24f, PaletteSlot.Belly), (new Vector3(0.15f, 0.2f, 0.26f), 0.08f, PaletteSlot.Belly),
                        (new Vector3(-0.15f, 0.2f, 0.26f), 0.08f, PaletteSlot.Belly)),
                Face(0.05f, 0.1f, 0.33f),
                new IdleStyle { breathe = 0.018f, breatheSpeed = 2.4f, jiggle = 0.004f, sway = 2f },
                new List<PlacedPart>
                {
                    Place(ear, new Vector3(0.16f, 0.48f, 0.02f), new Vector3(0f, 0f, -20f), 1f, true),
                    Place(pin, new Vector3(0f, 0.02f, -0.36f), new Vector3(0f, 180f, 0f), 1.3f),
                    Place(wheel, new Vector3(0f, 0.04f, -0.46f), new Vector3(0f, 180f, 0f), 1.6f),
                });
            EditorUtility.SetDirty(whirlby);

            var post = Part("PinwheelPost", PartMotion.None, Vector3.up, 0f, 0f, Style(PaletteSlot.Socks, 0.2f),
                Cone(Vector3.zero, new Vector3(0f, 0.3f, -0.05f), 0.035f, 0.03f, 0f));
            Budget(post, 22, 240);
            var bigWheel = Pinwheel("WhirlPinwheel", 0.4f, 2.2f);
            var wBody = Shape("Windwhirl_Body",
                Ell(new Vector3(0f, -0.04f, 0f), new Vector3(0.42f, 0.42f, 0.38f)),
                Sphere(new Vector3(0f, 0.4f, 0.08f), 0.28f, 0.14f),
                Ell(new Vector3(0.17f, 0.3f, 0.22f), new Vector3(0.12f, 0.1f, 0.09f), 0.06f).Mirror(),
                Ell(new Vector3(0.16f, -0.44f, 0.16f), new Vector3(0.1f, 0.06f, 0.13f), 0.05f).Mirror(),
                Ell(new Vector3(0.2f, 0.0f, 0.32f), new Vector3(0.07f, 0.06f, 0.07f), 0.04f).Mirror(),
                Floor(-0.48f));
            var windwhirl = Recipe("Windwhirl", wBody, 0.052f,
                new CreaturePalette
                {
                    baseColor = Hex("#E8A850"), belly = Hex("#FDF6E8"), accent = Hex("#DDA83B"), socks = Hex("#7E5434"),
                    glow = Hex("#FBE068"), extra = Hex("#50B8BA"), face = Hex("#2A2018"), iris = Hex("#2A2018"),
                },
                Patches((new Vector3(0f, -0.05f, 0.36f), 0.3f, PaletteSlot.Belly), (new Vector3(0f, 0.28f, 0.32f), 0.16f, PaletteSlot.Belly)),
                Face(0.05f, 0.11f, 0.44f),
                new IdleStyle { breathe = 0.018f, breatheSpeed = 2.2f, jiggle = 0.004f, sway = 2f },
                new List<PlacedPart>
                {
                    Place(ear, new Vector3(0.18f, 0.62f, 0.04f), new Vector3(0f, 0f, -20f), 1.1f, true),
                    Place(post, new Vector3(0.18f, 0.12f, -0.3f), new Vector3(-25f, 0f, -25f), 1f, true),
                    Place(bigWheel, new Vector3(0.3f, 0.4f, -0.42f), new Vector3(0f, 180f, 0f), 1f),
                    Place(bigWheel, new Vector3(-0.3f, 0.4f, -0.42f), new Vector3(0f, 180f, 0f), 1f),
                });
            EditorUtility.SetDirty(windwhirl);

            var burst = Attack("PinwheelBurst", "Pinwheel Burst", Element.Wind, 40, 100, MoveAnimation.Shoot);
            var wwS = WindSpecies("Windwhirl", windwhirl, null, 0, 64, 15, 14, 13, burst, LoadMove("RollOut"), LoadMove("WarmNap"));
            var s = WindSpecies("Whirlby", whirlby, wwS, 16, 46, 11, 10, 12, burst, LoadMove("Tackle"), LoadMove("WarmNap"));
            AssetDatabase.SaveAssets();
            return s;
        }

        // ================= 3. Samarling -> Samaroak =================

        /// <summary>Samarling: a plump seed nut with one big samara wing up and back that spins it like a helicopter seed. Samaroak: an acorn with a four-wing rotor.</summary>
        public static IslingSpecies Samarling()
        {
            var foot = PegFoot("SeedFoot", 0.045f, 0.06f, PaletteSlot.Socks);
            var wing = Part("SamaraWing", PartMotion.Spin, Vector3.up, 0f, 0.35f,
                new PartStyle { color = PaletteSlot.Extra, spec = 0.3f, outline = 0.02f, tip = true, tipColor = PaletteSlot.Glow, tipAxis = Vector3.right, tipAt = 0.75f },
                SamaraWing(0.55f, 0.22f));
            Budget(wing, 34, 500);
            var body = Shape("Samarling_Body",
                Ell(new Vector3(0f, -0.05f, 0f), new Vector3(0.28f, 0.32f, 0.26f)),
                Cone(new Vector3(0f, 0.2f, -0.02f), new Vector3(0f, 0.34f, -0.04f), 0.1f, 0.05f, 0.08f),
                Floor(-0.38f));
            var samarling = Recipe("Samarling", body, 0.034f,
                new CreaturePalette
                {
                    baseColor = Hex("#9C6A3C"), belly = Hex("#D4A875"), accent = Hex("#8FA361"), socks = Hex("#6B4726"),
                    glow = Hex("#8FA361"), extra = Hex("#C4DF9B"), face = Hex("#2A2018"), iris = Hex("#2A2018"),
                },
                Patches((new Vector3(0f, 0.0f, 0.26f), 0.17f, PaletteSlot.Belly)),
                Face(0.045f, 0.085f, 0.05f, 1.2f, true, -0.04f),
                new IdleStyle { breathe = 0.012f, breatheSpeed = 2f, hover = 0.006f, bob = 0.003f, sway = 2f },
                new List<PlacedPart>
                {
                    Place(foot, new Vector3(0.1f, -0.3f, 0.04f), Vector3.zero, 1f, true),
                    Place(wing, new Vector3(0f, 0.36f, -0.04f), new Vector3(0f, 0f, 25f), 1f),
                });
            EditorUtility.SetDirty(samarling);

            var rotor = Part("SamaraRotor", PartMotion.Spin, Vector3.up, 0f, 0.5f,
                new PartStyle { color = PaletteSlot.Extra, spec = 0.3f, outline = 0.02f, tip = true, tipColor = PaletteSlot.Glow, tipAxis = Vector3.right, tipAt = 0.75f },
                Rotor4(0.55f, 0.2f));
            Budget(rotor, 40, 900);
            var cap = Part("AcornCap", PartMotion.None, Vector3.up, 0f, 0f, Style(PaletteSlot.Accent, 0.15f),
                Ell(new Vector3(0f, 0.04f, 0f), new Vector3(0.36f, 0.14f, 0.34f), 0f).Bumps(0.012f, 30f),
                Cone(new Vector3(0f, 0.12f, 0f), new Vector3(0f, 0.22f, 0f), 0.05f, 0.04f, 0.04f));
            Budget(cap, 34, 700);
            var oakBody = Shape("Samaroak_Body",
                Ell(new Vector3(0f, -0.05f, 0f), new Vector3(0.36f, 0.4f, 0.34f)),
                Cone(new Vector3(0.18f, -0.32f, 0.14f), new Vector3(0.19f, -0.46f, 0.16f), 0.065f, 0.06f, 0.04f).Mirror(),
                Cone(new Vector3(0.18f, -0.32f, -0.14f), new Vector3(0.19f, -0.46f, -0.16f), 0.065f, 0.06f, 0.04f).Mirror(),
                Floor(-0.48f));
            var samaroak = Recipe("Samaroak", oakBody, 0.05f,
                new CreaturePalette
                {
                    baseColor = Hex("#85522E"), belly = Hex("#D4A875"), accent = Hex("#583920"), socks = Hex("#432B18"),
                    glow = Hex("#72964C"), extra = Hex("#A7D47B"), face = Hex("#2A2018"), iris = Hex("#2A2018"),
                },
                new BodyZones
                {
                    socks = true, socksY = -0.38f,
                    patches = new List<PatternPatch> { new PatternPatch { center = new Vector3(0f, 0.0f, 0.34f), radius = 0.2f, color = PaletteSlot.Belly } },
                },
                Face(0.05f, 0.1f, 0.08f, 1.2f, true, -0.04f),
                new IdleStyle { breathe = 0.012f, breatheSpeed = 1.6f, hover = 0.003f, sway = 1.5f },
                new List<PlacedPart>
                {
                    Place(cap, new Vector3(0f, 0.26f, 0f), Vector3.zero, 1f),
                    Place(rotor, new Vector3(0f, 0.5f, 0f), Vector3.zero, 1f),
                });
            EditorUtility.SetDirty(samaroak);

            var copter = Attack("SeedCopter", "Seed Copter", Element.Wind, 45, 95, MoveAnimation.Slam);
            var oakS = WindSpecies("Samaroak", samaroak, null, 0, 66, 15, 16, 10, copter, LoadMove("BodySlam"), LoadMove("HunkerDown"));
            var s = WindSpecies("Samarling", samarling, oakS, 16, 44, 11, 11, 11, copter, LoadMove("Tackle"), LoadMove("HunkerDown"));
            AssetDatabase.SaveAssets();
            return s;
        }

        static SdfPrimitive[] Rotor4(float length, float width)
        {
            var list = new List<SdfPrimitive> { Cone(new Vector3(0f, -0.03f, 0f), new Vector3(0f, 0.05f, 0f), 0.07f, 0.06f, 0f) };
            for (int i = 0; i < 4; i++)
            {
                var rot = Quaternion.Euler(0f, i * 90f + 45f, 0f);
                var tip = rot * new Vector3(length * 0.55f, 0f, 0f);
                list.Add(Ell(tip, new Vector3(length * 0.5f, 0.035f, width * 0.5f), 0.03f, new Vector3(0f, i * 90f + 45f, 6f)));
            }
            return list.ToArray();
        }

        // ================= 4. Vanehare -> Compasshare =================

        /// <summary>Vanehare: a pear-shaped cloud-grey hare with two brass weathervane-arrow ears on pivot posts. Compasshare: slate, compass-rose ears, a dial on its chest.</summary>
        public static IslingSpecies Vanehare()
        {
            var arrow = Part("VaneArrowEar", PartMotion.Flick, Vector3.up, 35f, 0.35f, Style(PaletteSlot.Extra, 0.7f),
                Cone(new Vector3(0f, 0f, 0f), new Vector3(0f, 0.04f, 0f), 0.035f, 0.035f, 0f),
                Cone(new Vector3(0f, 0.08f, -0.14f), new Vector3(0f, 0.08f, 0.12f), 0.022f, 0.022f, 0.02f),
                Cone(new Vector3(0f, 0.08f, 0.12f), new Vector3(0f, 0.08f, 0.22f), 0.06f, 0.008f, 0.02f),
                Box(new Vector3(0f, 0.08f, -0.14f), new Vector3(0.015f, 0.055f, 0.045f), 0.012f, 0.02f),
                Cone(new Vector3(0f, 0.02f, 0f), new Vector3(0f, 0.08f, 0f), 0.02f, 0.02f, 0.02f));
            Budget(arrow, 30, 450);
            var tailPuff = Part("BobTail", PartMotion.Wag, Vector3.up, 10f, 1.2f, Style(PaletteSlot.Glow, 0.1f),
                Sphere(Vector3.zero, 0.09f, 0f).Bumps(0.01f, 20f));
            Budget(tailPuff, 24, 300);
            var body = Shape("Vanehare_Body",
                Ell(new Vector3(0f, -0.1f, -0.04f), new Vector3(0.3f, 0.3f, 0.32f)),
                Sphere(new Vector3(0f, 0.24f, 0.1f), 0.2f, 0.12f),
                Ell(new Vector3(0f, 0.18f, 0.27f), new Vector3(0.08f, 0.06f, 0.06f), 0.04f),
                Ell(new Vector3(0.14f, -0.36f, 0.12f), new Vector3(0.07f, 0.04f, 0.1f), 0.04f).Mirror(),
                Ell(new Vector3(0.18f, -0.34f, -0.18f), new Vector3(0.08f, 0.06f, 0.14f), 0.05f).Mirror(),
                Floor(-0.4f));
            var vanehare = Recipe("Vanehare", body, 0.04f,
                new CreaturePalette
                {
                    baseColor = Hex("#E8ECEF"), belly = Hex("#F7F9FA"), accent = Hex("#F0A8A8"), socks = Hex("#C9CFD4"),
                    glow = Hex("#FFFFFF"), extra = Hex("#DDA83B"), face = Hex("#2A2018"), iris = Hex("#2A2018"),
                },
                Patches((new Vector3(0f, 0.18f, 0.32f), 0.07f, PaletteSlot.Accent), (new Vector3(0f, -0.12f, 0.26f), 0.18f, PaletteSlot.Belly)),
                Face(0.045f, 0.09f, 0.27f, 1.25f, false),
                new IdleStyle { breathe = 0.014f, breatheSpeed = 2.4f, jiggle = 0.003f, sway = 2f },
                new List<PlacedPart>
                {
                    Place(arrow, new Vector3(0.09f, 0.4f, 0.06f), new Vector3(0f, 20f, 8f), 1f),
                    Place(arrow, new Vector3(-0.09f, 0.4f, 0.06f), new Vector3(0f, -25f, -8f), 1f),
                    Place(tailPuff, new Vector3(0f, -0.12f, -0.36f), Vector3.zero, 1f),
                });
            EditorUtility.SetDirty(vanehare);

            var rose = Part("CompassRoseEar", PartMotion.Flick, Vector3.up, 60f, 0.25f, Style(PaletteSlot.Extra, 0.8f),
                Cone(Vector3.zero, new Vector3(0f, 0.06f, 0f), 0.03f, 0.03f, 0f),
                Box(new Vector3(0f, 0.14f, 0f), new Vector3(0.022f, 0.08f, 0.022f), 0.01f, 0.02f, new Vector3(0f, 45f, 0f)),
                Box(new Vector3(0f, 0.14f, 0f), new Vector3(0.11f, 0.022f, 0.022f), 0.01f, 0.02f),
                Box(new Vector3(0f, 0.14f, 0f), new Vector3(0.022f, 0.022f, 0.11f), 0.01f, 0.02f),
                Sphere(new Vector3(0f, 0.14f, 0f), 0.04f, 0.02f));
            Budget(rose, 32, 500);
            var dial = Part("CompassDial", PartMotion.None, Vector3.up, 0f, 0f, Style(PaletteSlot.Extra, 0.8f),
                Cone(new Vector3(0f, 0f, -0.01f), new Vector3(0f, 0f, 0.02f), 0.09f, 0.09f, 0f),
                Cone(new Vector3(0f, 0f, 0.015f), new Vector3(0f, 0f, 0.03f), 0.07f, 0.07f, 0f).Sub());
            Budget(dial, 28, 360);
            var cBody = Shape("Compasshare_Body",
                Ell(new Vector3(0f, -0.08f, -0.04f), new Vector3(0.3f, 0.38f, 0.3f)),
                Sphere(new Vector3(0f, 0.38f, 0.1f), 0.21f, 0.12f),
                Ell(new Vector3(0f, 0.32f, 0.28f), new Vector3(0.08f, 0.06f, 0.06f), 0.04f),
                Ell(new Vector3(0.14f, -0.44f, 0.14f), new Vector3(0.07f, 0.04f, 0.11f), 0.04f).Mirror(),
                Ell(new Vector3(0.2f, -0.4f, -0.16f), new Vector3(0.09f, 0.07f, 0.16f), 0.05f).Mirror(),
                Floor(-0.48f));
            var compass = Recipe("Compasshare", cBody, 0.054f,
                new CreaturePalette
                {
                    baseColor = Hex("#8E9DAE"), belly = Hex("#F5F6F8"), accent = Hex("#D9453B"), socks = Hex("#5C5242"),
                    glow = Hex("#2C6A72"), extra = Hex("#D8A638"), face = Hex("#2A2018"), iris = Hex("#2A2018"),
                },
                Patches((new Vector3(0f, -0.06f, 0.28f), 0.22f, PaletteSlot.Belly), (new Vector3(0f, 0.3f, 0.3f), 0.1f, PaletteSlot.Belly)),
                Face(0.045f, 0.095f, 0.42f, 1.25f, false),
                new IdleStyle { breathe = 0.013f, breatheSpeed = 2f, sway = 1.5f },
                new List<PlacedPart>
                {
                    Place(rose, new Vector3(0.1f, 0.56f, 0.06f), new Vector3(0f, 0f, 10f), 1f),
                    Place(rose, new Vector3(-0.1f, 0.56f, 0.06f), new Vector3(0f, 30f, -10f), 1f),
                    Place(dial, new Vector3(0f, 0.08f, 0.31f), new Vector3(-10f, 0f, 0f), 1f),
                    Place(tailPuff, new Vector3(0f, -0.14f, -0.34f), Vector3.zero, 1.1f),
                });
            EditorUtility.SetDirty(compass);

            var dart = Attack("VaneDart", "Vane Dart", Element.Wind, 45, 95, MoveAnimation.Shoot, true);
            var cS = WindSpecies("Compasshare", compass, null, 0, 58, 16, 12, 17, dart, LoadMove("Tackle"), LoadMove("PuffUp"));
            var s = WindSpecies("Vanehare", vanehare, cS, 16, 40, 12, 9, 15, dart, LoadMove("Tackle"), LoadMove("PuffUp"));
            AssetDatabase.SaveAssets();
            return s;
        }

        // ================= 5. Zeppelope -> Skyshiplope (rare) =================

        /// <summary>Zeppelope: a striped blimp body melting into a fawn antelope head, antler nubs, a gondola keel, a brass propeller, dangling legs. Skyshiplope: navy, bigger, twin props, a gondola on struts.</summary>
        public static IslingSpecies Zeppelope()
        {
            var prop = Part("BrassProp", PartMotion.Spin, Vector3.forward, 0f, 1.2f, Style(PaletteSlot.Extra, 0.8f), Prop3(0.18f));
            Budget(prop, 30, 500);
            var antler = Part("AntlerNub", PartMotion.Flick, Vector3.forward, 6f, 0.3f, Style(PaletteSlot.Accent, 0.3f),
                Cone(Vector3.zero, new Vector3(0.02f, 0.14f, -0.02f), 0.03f, 0.022f, 0f),
                Cone(new Vector3(0.01f, 0.08f, -0.01f), new Vector3(0.07f, 0.13f, -0.01f), 0.018f, 0.015f, 0.02f));
            Budget(antler, 24, 260);
            var leg = Part("DanglerLeg", PartMotion.Wag, Vector3.right, 12f, 0.6f,
                new PartStyle { color = PaletteSlot.Accent, spec = 0.2f, outline = 0.02f, tip = true, tipColor = PaletteSlot.Socks, tipAxis = Vector3.down, tipAt = 0.75f },
                Cone(Vector3.zero, new Vector3(0f, -0.12f, 0f), 0.028f, 0.024f, 0f),
                Sphere(new Vector3(0f, -0.15f, 0f), 0.032f, 0.02f));
            Budget(leg, 22, 220);
            var body = Shape("Zeppelope_Body",
                Ell(new Vector3(0f, 0.02f, -0.12f), new Vector3(0.3f, 0.26f, 0.5f)),
                Cone(new Vector3(0f, 0.04f, 0.26f), new Vector3(0f, 0.2f, 0.42f), 0.16f, 0.12f, 0.12f),
                Ell(new Vector3(0f, 0.24f, 0.48f), new Vector3(0.13f, 0.12f, 0.15f), 0.06f),
                Ell(new Vector3(0f, -0.26f, -0.08f), new Vector3(0.1f, 0.06f, 0.24f), 0.06f),
                Ell(new Vector3(0f, 0.12f, -0.6f), new Vector3(0.02f, 0.12f, 0.08f), 0.04f),
                Ell(new Vector3(0f, -0.06f, -0.6f), new Vector3(0.02f, 0.12f, 0.08f), 0.04f));
            var zep = Recipe("Zeppelope", body, 0.046f,
                new CreaturePalette
                {
                    baseColor = Hex("#78BEE0"), belly = Hex("#F0F4F7"), accent = Hex("#C89464"), socks = Hex("#4A3525"),
                    glow = Hex("#7C5233"), extra = Hex("#DDA83B"), face = Hex("#2A2018"), iris = Hex("#2A2018"),
                },
                new BodyZones
                {
                    patches = new List<PatternPatch>
                    {
                        new PatternPatch { center = new Vector3(0f, 0.2f, 0.46f), radius = 0.2f, color = PaletteSlot.Accent },
                        new PatternPatch { center = new Vector3(0f, 0.1f, 0.26f), radius = 0.17f, color = PaletteSlot.Accent },
                        new PatternPatch { center = new Vector3(0.3f, 0.04f, -0.12f), radius = 0.14f, color = PaletteSlot.Belly, mirrorX = true },
                        new PatternPatch { center = new Vector3(0.24f, 0.08f, -0.42f), radius = 0.12f, color = PaletteSlot.Belly, mirrorX = true },
                        new PatternPatch { center = new Vector3(0f, -0.28f, -0.08f), radius = 0.14f, color = PaletteSlot.Glow },
                    },
                },
                Face(0.04f, 0.09f, 0.28f, 1.2f, true, 0.2f),
                new IdleStyle { breathe = 0.012f, breatheSpeed = 1.4f, hover = 0.008f, bob = 0.004f, sway = 3f },
                new List<PlacedPart>
                {
                    Place(antler, new Vector3(0.07f, 0.34f, 0.46f), new Vector3(0f, 0f, -15f), 1f, true),
                    Place(prop, new Vector3(0f, 0.03f, -0.64f), new Vector3(0f, 180f, 0f), 1f),
                    Place(leg, new Vector3(0.1f, -0.2f, 0.12f), Vector3.zero, 1f, true),
                    Place(leg, new Vector3(0.1f, -0.2f, -0.28f), Vector3.zero, 1f, true),
                });
            zep.spec = 0.35f;
            EditorUtility.SetDirty(zep);

            var gondola = Part("Gondola", PartMotion.Wag, Vector3.forward, 4f, 0.5f, Style(PaletteSlot.Glow, 0.4f),
                Box(new Vector3(0f, -0.08f, 0f), new Vector3(0.12f, 0.06f, 0.24f), 0.04f, 0f),
                Cone(new Vector3(0.09f, -0.02f, 0.14f), new Vector3(0.09f, 0.08f, 0.14f), 0.018f, 0.018f, 0.01f).Mirror(),
                Cone(new Vector3(0.09f, -0.02f, -0.14f), new Vector3(0.09f, 0.08f, -0.14f), 0.018f, 0.018f, 0.01f).Mirror());
            Budget(gondola, 30, 500);
            var bigAntler = Part("BranchAntler", PartMotion.None, Vector3.up, 0f, 0f, Style(PaletteSlot.Socks, 0.3f),
                Cone(Vector3.zero, new Vector3(0.03f, 0.22f, -0.04f), 0.03f, 0.02f, 0f),
                Cone(new Vector3(0.015f, 0.1f, -0.02f), new Vector3(0.1f, 0.18f, -0.02f), 0.018f, 0.014f, 0.02f),
                Cone(new Vector3(0.025f, 0.17f, -0.03f), new Vector3(-0.04f, 0.26f, -0.05f), 0.016f, 0.012f, 0.02f));
            Budget(bigAntler, 28, 360);
            var sBody = Shape("Skyshiplope_Body",
                Ell(new Vector3(0f, 0.04f, -0.14f), new Vector3(0.34f, 0.3f, 0.6f)),
                Cone(new Vector3(0f, 0.06f, 0.32f), new Vector3(0f, 0.26f, 0.5f), 0.18f, 0.13f, 0.12f),
                Ell(new Vector3(0f, 0.3f, 0.56f), new Vector3(0.14f, 0.13f, 0.16f), 0.06f),
                Ell(new Vector3(0f, 0.16f, -0.72f), new Vector3(0.025f, 0.14f, 0.09f), 0.04f),
                Ell(new Vector3(0.14f, 0.0f, -0.72f), new Vector3(0.1f, 0.025f, 0.08f), 0.04f).Mirror());
            var ship = Recipe("Skyshiplope", sBody, 0.064f,
                new CreaturePalette
                {
                    baseColor = Hex("#436280"), belly = Hex("#F6F2E8"), accent = Hex("#BAA896"), socks = Hex("#5A4232"),
                    glow = Hex("#825332"), extra = Hex("#D8A638"), face = Hex("#2A2018"), iris = Hex("#2A2018"),
                },
                new BodyZones
                {
                    patches = new List<PatternPatch>
                    {
                        new PatternPatch { center = new Vector3(0f, 0.28f, 0.54f), radius = 0.2f, color = PaletteSlot.Accent },
                        new PatternPatch { center = new Vector3(0f, 0.12f, 0.32f), radius = 0.18f, color = PaletteSlot.Accent },
                        new PatternPatch { center = new Vector3(0.34f, 0.06f, -0.14f), radius = 0.15f, color = PaletteSlot.Belly, mirrorX = true },
                        new PatternPatch { center = new Vector3(0.28f, 0.1f, -0.5f), radius = 0.13f, color = PaletteSlot.Belly, mirrorX = true },
                    },
                },
                Face(0.04f, 0.1f, 0.34f, 1.2f, true, 0.26f),
                new IdleStyle { breathe = 0.01f, breatheSpeed = 1.2f, hover = 0.008f, bob = 0.004f, sway = 2f },
                new List<PlacedPart>
                {
                    Place(bigAntler, new Vector3(0.08f, 0.42f, 0.54f), new Vector3(0f, 0f, -18f), 1f, true),
                    Place(prop, new Vector3(0.16f, 0.0f, -0.76f), new Vector3(0f, 180f, 0f), 1.1f, true),
                    Place(gondola, new Vector3(0f, -0.3f, -0.1f), Vector3.zero, 1.2f),
                });
            ship.spec = 0.35f;
            EditorUtility.SetDirty(ship);

            var wash = Attack("PropWash", "Prop Wash", Element.Wind, 50, 90, MoveAnimation.Shoot);
            var shipS = WindSpecies("Skyshiplope", ship, null, 0, 72, 17, 15, 12, wash, LoadMove("BodySlam"), LoadMove("HunkerDown"));
            var s = WindSpecies("Zeppelope", zep, shipS, 18, 52, 13, 12, 11, wash, LoadMove("Tackle"), LoadMove("HunkerDown"));
            AssetDatabase.SaveAssets();
            return s;
        }

        static SdfPrimitive[] Prop3(float length)
        {
            var list = new List<SdfPrimitive> { Sphere(Vector3.zero, 0.045f, 0f) };
            for (int i = 0; i < 3; i++)
            {
                float a = i * 120f;
                var d = Quaternion.Euler(0f, 0f, a) * Vector3.up;
                list.Add(Ell(d * length * 0.55f, new Vector3(0.045f, length * 0.5f, 0.02f), 0.03f, new Vector3(0f, 20f, a)));
            }
            return list.ToArray();
        }

        // ================= 6. Pufflet -> Nimbulpaca =================

        /// <summary>A tall cloud-fleece alpaca of big pillowy blobs, sage face and leg bands, two golden dandelion rotors on its head.</summary>
        public static IslingSpecies Nimbulpaca()
        {
            var ear = Part("NimbusEar", PartMotion.Flick, Vector3.forward, 10f, 0.4f,
                new PartStyle { color = PaletteSlot.Socks, spec = 0.2f, outline = 0.02f },
                Ell(new Vector3(0f, -0.07f, 0f), new Vector3(0.045f, 0.09f, 0.03f), 0f));
            Budget(ear, 24, 260);
            var rotor = Part("DandelionRotor", PartMotion.Spin, Vector3.up, 0f, 0.8f, Style(PaletteSlot.Extra, 0.5f), Rotor3Flat(0.18f));
            Budget(rotor, 30, 420);
            var peg = Part("RotorPeg", PartMotion.None, Vector3.up, 0f, 0f, Style(PaletteSlot.Glow, 0.4f),
                Cone(Vector3.zero, new Vector3(0f, 0.1f, 0f), 0.025f, 0.02f, 0f));
            Budget(peg, 20, 160);
            var body = Shape("Nimbulpaca_Body",
                Sphere(new Vector3(0f, -0.02f, -0.12f), 0.38f).Bumps(0.03f, 20f),
                Sphere(new Vector3(0.12f, 0.06f, 0.12f), 0.3f, 0.12f).Bumps(0.03f, 20f),
                Sphere(new Vector3(-0.12f, 0.06f, 0.12f), 0.3f, 0.12f).Bumps(0.03f, 20f),
                Sphere(new Vector3(0f, 0.1f, -0.4f), 0.3f, 0.12f).Bumps(0.03f, 20f),
                Cone(new Vector3(0f, 0.2f, 0.2f), new Vector3(0f, 0.78f, 0.28f), 0.2f, 0.16f, 0.12f).Bumps(0.028f, 20f),
                Sphere(new Vector3(0f, 0.98f, 0.22f), 0.22f, 0.1f).Bumps(0.026f, 20f),
                Ell(new Vector3(0f, 0.82f, 0.46f), new Vector3(0.16f, 0.16f, 0.15f), 0.06f),
                Cone(new Vector3(0.17f, -0.26f, 0.16f), new Vector3(0.17f, -0.62f, 0.17f), 0.085f, 0.08f, 0.05f).Mirror(),
                Cone(new Vector3(0.17f, -0.26f, -0.38f), new Vector3(0.17f, -0.62f, -0.39f), 0.085f, 0.08f, 0.05f).Mirror(),
                Floor(-0.63f));
            var nim = Recipe("Nimbulpaca", body, 0.07f,
                new CreaturePalette
                {
                    baseColor = Hex("#F5F0E0"), belly = Hex("#FFFFFF"), accent = Hex("#E8C84A"), socks = Hex("#7AA87A"),
                    glow = Hex("#C8A435"), extra = Hex("#E8C84A"), face = Hex("#2A3028"), iris = Hex("#2A4A3A"),
                },
                new BodyZones
                {
                    socks = true, socksY = -0.42f,
                    patches = new List<PatternPatch> { new PatternPatch { center = new Vector3(0f, 0.82f, 0.56f), radius = 0.17f, color = PaletteSlot.Socks } },
                },
                Face(0.05f, 0.09f, 0.86f, 1.25f, true, 0.76f),
                new IdleStyle { breathe = 0.016f, breatheSpeed = 1.8f, jiggle = 0.004f, hover = 0.002f, sway = 1.5f },
                new List<PlacedPart>
                {
                    Place(ear, new Vector3(0.17f, 0.94f, 0.26f), new Vector3(0f, 0f, -100f), 1.1f, true),
                    Place(peg, new Vector3(0.1f, 1.14f, 0.2f), new Vector3(0f, 0f, -12f), 1f, true),
                    Place(rotor, new Vector3(0.12f, 1.24f, 0.2f), Vector3.zero, 1f),
                    Place(rotor, new Vector3(-0.12f, 1.24f, 0.2f), new Vector3(0f, 60f, 0f), 1f),
                });
            EditorUtility.SetDirty(nim);
            var pufflet = LoadSpecies("Pufflet");
            var moves = pufflet != null ? pufflet.moves : new[] { LoadMove("GustFlick"), LoadMove("Tackle"), LoadMove("PuffUp") };
            var s = WindSpecies("Nimbulpaca", nim, null, 0, 62, 15, 14, 15, moves);
            if (pufflet != null)
            {
                pufflet.evolvesTo = s;
                pufflet.evolveLevel = 14;
                EditorUtility.SetDirty(pufflet);
            }
            AssetDatabase.SaveAssets();
            return s;
        }

        static SdfPrimitive[] Rotor3Flat(float length)
        {
            var list = new List<SdfPrimitive> { Sphere(Vector3.zero, 0.035f, 0f) };
            for (int i = 0; i < 3; i++)
            {
                var rot = Quaternion.Euler(0f, i * 120f, 0f);
                list.Add(Ell(rot * new Vector3(length * 0.55f, 0f, 0f), new Vector3(length * 0.5f, 0.022f, 0.05f), 0.03f, new Vector3(0f, i * 120f, 10f)));
            }
            return list.ToArray();
        }

        // ================= 7. Pebbloop -> Riverloom =================

        /// <summary>A big barrel-chested lavender river hippo with a wooden waterwheel turning on its back, a wide flat tail, thick legs, little teeth.</summary>
        public static IslingSpecies Riverloom()
        {
            var wheel = Part("Waterwheel", PartMotion.Spin, Vector3.right, 0f, 0.3f, Style(PaletteSlot.Extra, 0.3f), Waterwheel(0.3f));
            Budget(wheel, 36, 800);
            var tail = Part("FlatTail", PartMotion.Wag, Vector3.up, 12f, 0.6f, Style(PaletteSlot.Accent, 0.3f),
                Ell(new Vector3(0f, 0f, -0.12f), new Vector3(0.14f, 0.04f, 0.14f), 0f));
            Budget(tail, 26, 300);
            var ear = RoundEar("HippoEar", new Vector3(0.05f, 0.045f, 0.03f));
            var body = Shape("Riverloom_Body",
                Ell(new Vector3(0f, -0.06f, -0.06f), new Vector3(0.46f, 0.38f, 0.52f)),
                Ell(new Vector3(0f, 0.06f, 0.4f), new Vector3(0.3f, 0.26f, 0.24f), 0.14f),
                Ell(new Vector3(0f, -0.04f, 0.56f), new Vector3(0.26f, 0.15f, 0.12f), 0.08f),
                Ell(new Vector3(0.07f, -0.1f, 0.66f), new Vector3(0.025f, 0.035f, 0.02f), 0.01f).Mirror(),
                Cone(new Vector3(0.26f, -0.3f, 0.26f), new Vector3(0.26f, -0.52f, 0.28f), 0.11f, 0.105f, 0.06f).Mirror(),
                Cone(new Vector3(0.26f, -0.3f, -0.36f), new Vector3(0.26f, -0.52f, -0.38f), 0.11f, 0.105f, 0.06f).Mirror(),
                Floor(-0.53f));
            var river = Recipe("Riverloom", body, 0.068f,
                new CreaturePalette
                {
                    baseColor = Hex("#8888C0"), belly = Hex("#B8C8E0"), accent = Hex("#6878A8"), socks = Hex("#6878A8"),
                    glow = Hex("#B8C8E0"), extra = Hex("#8B7050"), face = Hex("#2A2018"), iris = Hex("#2A2018"),
                },
                Patches((new Vector3(0f, -0.3f, 0.2f), 0.34f, PaletteSlot.Belly), (new Vector3(0f, -0.06f, 0.6f), 0.14f, PaletteSlot.Belly)),
                Face(0.05f, 0.14f, 0.24f, 1.15f, false),
                new IdleStyle { breathe = 0.016f, breatheSpeed = 1.4f, jiggle = 0.003f, sway = 1f },
                new List<PlacedPart>
                {
                    Place(ear, new Vector3(0.17f, 0.3f, 0.38f), new Vector3(0f, 0f, -20f), 1f, true),
                    Place(wheel, new Vector3(0f, 0.38f, -0.16f), Vector3.zero, 1f),
                    Place(tail, new Vector3(0f, -0.08f, -0.56f), new Vector3(10f, 0f, 0f), 1f),
                });
            EditorUtility.SetDirty(river);
            var pebbloop = LoadSpecies("Pebbloop");
            var moves = pebbloop != null ? pebbloop.moves : new[] { LoadMove("BubblePop"), LoadMove("BodySlam"), LoadMove("HunkerDown") };
            var s = Species("Riverloom", Element.Water, river, null, 0);
            s.baseStats = new BaseStats { hp = 68, attack = 14, defense = 16, speed = 10 };
            s.moves = moves;
            EditorUtility.SetDirty(s);
            if (pebbloop != null)
            {
                pebbloop.evolvesTo = s;
                pebbloop.evolveLevel = 14;
                EditorUtility.SetDirty(pebbloop);
            }
            AssetDatabase.SaveAssets();
            return s;
        }

        /// <summary>A chunky disc with five thick paddles, turning about X (it stands across the back).</summary>
        static SdfPrimitive[] Waterwheel(float r)
        {
            var list = new List<SdfPrimitive>
            {
                Cone(new Vector3(-0.035f, 0f, 0f), new Vector3(0.035f, 0f, 0f), r * 0.5f, r * 0.5f, 0f),
                Cone(new Vector3(-0.09f, 0f, 0f), new Vector3(0.09f, 0f, 0f), 0.04f, 0.04f, 0.02f),
            };
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f;
                var d = Quaternion.Euler(a, 0f, 0f) * Vector3.up;
                list.Add(Box(d * r * 0.78f, new Vector3(0.09f, r * 0.34f, 0.04f), 0.015f, 0.02f, new Vector3(a, 0f, 0f)));
            }
            return list.ToArray();
        }

        internal static SdfPrimitive Box(Vector3 c, Vector3 half, float round, float k, Vector3 rotation = default) =>
            new SdfPrimitive { kind = SdfKind.RoundBox, a = c, size = half, b = new Vector3(round, 0f, 0f), rotation = rotation, k = k };
    }
}
