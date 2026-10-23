using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Diorama.EditorTools.StarterKit;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Writes the first 4 wild fire creatures (Toastling, Waxle, Kettlewing, Glazegolem) and their evolutions
    /// (Crumbhog, Lanternsnail, Kilngolem), plus the kit parts they add. Concepts: Docs/ConceptArt/05 and 06.
    /// Like <see cref="StarterKit"/>, this only seeds the assets; tune them in the Inspector afterwards.
    /// Re-running overwrites these assets (same files, so references survive). Needs the starter kit (FlamePuff,
    /// RoundEar) written first.
    /// </summary>
    public static class FireKit
    {
        const string Root = "Assets/_Project/Data/Creatures";

        [MenuItem("Tools/Pocket Isle/Creatures/Write Fire Kit (overwrites)", priority = 21)]
        static void Menu()
        {
            if (!EditorUtility.DisplayDialog("Write fire kit",
                    "Overwrites the fire parts, Toastling, Waxle, Kettlewing, Glazegolem and their evolutions in Data/Creatures " +
                    "with the defaults from code. Inspector tweaks to those assets are lost.", "Overwrite", "Cancel")) return;
            Write();
        }

        public static void Write()
        {
            var flamePuff = AssetDatabase.LoadAssetAtPath<CreaturePart>($"{Root}/Parts/FlamePuff.asset");
            var roundEar = AssetDatabase.LoadAssetAtPath<CreaturePart>($"{Root}/Parts/RoundEar.asset");
            if (flamePuff == null || roundEar == null)
            {
                Debug.LogError("[FireKit] FlamePuff / RoundEar missing. Run Write Starter Kit first.");
                return;
            }

            // ---- new kit parts (generic, reusable) ----

            // Chunky rounded cone with a toasted tip; wobbles. Hedgehog spines, back spikes.
            var spine = Part("Spine", PartMotion.Wag, Vector3.right, 6f, 0.6f,
                new PartStyle
                {
                    color = PaletteSlot.Base, spec = 0.15f, outline = 0.022f,
                    tip = true, tipColor = PaletteSlot.Accent, tipAxis = Vector3.up, tipAt = 0.11f,
                },
                Cone(Vector3.zero, new Vector3(0f, 0.22f, -0.03f), 0.085f, 0.022f));
            Budget(spine, 32, 400);

            // Blobby chunk (crumbs, pebbles, nuggets).
            var nugget = Part("Nugget", PartMotion.None, Vector3.up, 0f, 0f,
                new PartStyle { color = PaletteSlot.Extra, spec = 0.3f, outline = 0.018f },
                Sphere(new Vector3(0f, 0.04f, 0f), 0.055f).Bumps(0.012f, 30f),
                Sphere(new Vector3(0.03f, 0.07f, 0.01f), 0.035f, 0.04f).Bumps(0.01f, 30f));
            Budget(nugget, 28, 300);

            // Candle with a dished top, a melted lip and chunky drips down the sides. Flame rides on (0, 0.6, 0).
            var candlePrims = new List<SdfPrimitive>
            {
                Cone(new Vector3(0f, -0.1f, 0f), new Vector3(0f, 0.62f, 0f), 0.25f, 0.25f),
                Plane(new Vector3(0f, 0.58f, 0f), Vector3.up, 0.08f),
                Plane(Vector3.zero, Vector3.down, 0.04f),
                Sphere(new Vector3(0f, 0.86f, 0f), 0.3f, 0.05f).Sub(),
                Torus(new Vector3(0f, 0.56f, 0f), 0.22f, 0.045f, Vector3.zero, 0.05f),
            };
            float[] dripAngles = { 20f, 95f, 160f, 230f, 300f };
            float[] dripLengths = { 0.22f, 0.32f, 0.16f, 0.27f, 0.2f };
            for (int i = 0; i < dripAngles.Length; i++)
            {
                var d = Quaternion.Euler(0f, dripAngles[i], 0f) * Vector3.forward * 0.25f;
                candlePrims.Add(Cone(new Vector3(d.x, 0.55f, d.z), new Vector3(d.x * 1.04f, 0.55f - dripLengths[i], d.z * 1.04f), 0.05f, 0.058f, 0.05f));
            }
            var candleShell = Part("CandleShell", PartMotion.None, Vector3.up, 0f, 0f,
                new PartStyle { color = PaletteSlot.Belly, glow = 0.08f, spec = 0.35f, outline = 0.025f },
                candlePrims.ToArray());
            Budget(candleShell, 48, 1400);

            // Round lantern glass, glowing warm from the bottom up. Pair with LanternFrame at the same spot.
            var lanternGlass = Part("LanternGlass", PartMotion.None, Vector3.up, 0f, 0f,
                new PartStyle { color = PaletteSlot.Glow, gradient = true, gradientTo = PaletteSlot.Extra, glow = 0.55f, spec = 0.8f, outline = 0.02f },
                Sphere(new Vector3(0f, 0.3f, 0f), 0.29f));

            // Lantern cage: three vertical hoops, a base band and a top cap. Flame rides on (0, 0.66, 0).
            var framePrims = new List<SdfPrimitive>();
            for (int i = 0; i < 3; i++)
                framePrims.Add(Torus(new Vector3(0f, 0.3f, 0f), 0.3f, 0.022f, new Vector3(90f, i * 60f, 0f), 0.02f));
            framePrims.Add(Torus(new Vector3(0f, 0.07f, 0f), 0.2f, 0.035f, Vector3.zero, 0.03f));
            framePrims.Add(Ell(new Vector3(0f, 0.6f, 0f), new Vector3(0.11f, 0.05f, 0.11f), 0.04f));
            var lanternFrame = Part("LanternFrame", PartMotion.None, Vector3.up, 0f, 0f,
                new PartStyle { color = PaletteSlot.Accent, spec = 0.7f, outline = 0.015f },
                framePrims.ToArray());
            Budget(lanternFrame, 72, 1600);

            // Kettle / pot lid: low dome, rim and a knob (knob painted in the accent).
            var kettleLid = Part("KettleLid", PartMotion.None, Vector3.up, 0f, 0f,
                new PartStyle
                {
                    color = PaletteSlot.Base, spec = 0.5f, outline = 0.022f,
                    tip = true, tipColor = PaletteSlot.Accent, tipAxis = Vector3.up, tipAt = 0.115f,
                },
                Ell(new Vector3(0f, 0.03f, 0f), new Vector3(0.25f, 0.08f, 0.25f)),
                Torus(new Vector3(0f, 0.01f, 0f), 0.24f, 0.03f, Vector3.zero, 0.03f),
                Cone(new Vector3(0f, 0.08f, 0f), new Vector3(0f, 0.12f, 0f), 0.035f, 0.03f, 0.03f),
                Sphere(new Vector3(0f, 0.15f, 0f), 0.055f, 0.03f));
            Budget(kettleLid, 40, 800);

            // Short thick spout pointing forward (+Z) with an open rim. Steam rides on (0, 0.1, 0.31).
            var spoutBeak = Part("SpoutBeak", PartMotion.None, Vector3.up, 0f, 0f,
                new PartStyle { color = PaletteSlot.Accent, spec = 0.5f, outline = 0.022f },
                Sphere(Vector3.zero, 0.12f),
                Cone(Vector3.zero, new Vector3(0f, 0.06f, 0.26f), 0.11f, 0.065f, 0.06f),
                Torus(new Vector3(0f, 0.065f, 0.27f), 0.06f, 0.025f, new Vector3(75f, 0f, 0f), 0.03f),
                Sphere(new Vector3(0f, 0.08f, 0.33f), 0.05f, 0.02f).Sub());
            Budget(spoutBeak, 40, 800);

            // Small smooth wing stub with a rounded trailing lobe; flaps.
            var wingNub = Part("WingNub", PartMotion.Flap, Vector3.forward, 14f, 0.9f,
                new PartStyle
                {
                    color = PaletteSlot.Base, spec = 0.4f, outline = 0.022f,
                    tip = true, tipColor = PaletteSlot.Accent, tipAxis = Vector3.down, tipAt = 0.13f,
                },
                Ell(new Vector3(0.07f, -0.04f, 0f), new Vector3(0.09f, 0.16f, 0.07f), 0.1f, new Vector3(0f, 0f, 25f)),
                Ell(new Vector3(0.12f, -0.15f, -0.01f), new Vector3(0.06f, 0.08f, 0.05f), 0.05f, new Vector3(0f, 0f, 10f)));
            Budget(wingNub, 36, 600);

            // Little cloud of steam / smoke; bobs.
            var steamPuff = Part("SteamPuff", PartMotion.Bob, Vector3.up, 0.025f, 0.7f,
                new PartStyle { color = PaletteSlot.Glow, glow = 0.3f, spec = 0f, outline = 0.016f },
                Sphere(new Vector3(0f, 0.06f, 0f), 0.065f),
                Sphere(new Vector3(0.06f, 0.1f, 0f), 0.05f, 0.04f),
                Sphere(new Vector3(-0.05f, 0.11f, 0.01f), 0.045f, 0.04f),
                Sphere(new Vector3(0.01f, 0.16f, 0f), 0.045f, 0.04f));
            Budget(steamPuff, 32, 500);

            // Thick pot rim with a flared lip (sits on a hollowed pot).
            var potRim = Part("PotRim", PartMotion.None, Vector3.up, 0f, 0f,
                new PartStyle { color = PaletteSlot.Base, spec = 0.35f, outline = 0.022f },
                Torus(Vector3.zero, 0.2f, 0.05f, Vector3.zero),
                Torus(new Vector3(0f, 0.035f, 0f), 0.225f, 0.035f, Vector3.zero, 0.04f));
            Budget(potRim, 48, 900);

            // Stubby arm with a mitten hand; swings slowly. Points to +X (mirror for the left).
            var stubArm = Part("StubArm", PartMotion.Wag, Vector3.right, 9f, 0.35f,
                new PartStyle { color = PaletteSlot.Base, spec = 0.3f, outline = 0.025f },
                Cone(Vector3.zero, new Vector3(0.12f, -0.15f, 0.02f), 0.075f, 0.07f),
                Sphere(new Vector3(0.15f, -0.24f, 0.03f), 0.1f, 0.06f),
                Sphere(new Vector3(0.09f, -0.21f, 0.11f), 0.045f, 0.04f));
            Budget(stubArm, 36, 700);

            // Straight chimney with a rim and an open top. Flames / embers ride on (0, 0.24, 0).
            var chimney = Part("Chimney", PartMotion.None, Vector3.up, 0f, 0f,
                new PartStyle { color = PaletteSlot.Extra, spec = 0.2f, outline = 0.02f },
                Cone(new Vector3(0f, -0.05f, 0f), new Vector3(0f, 0.22f, 0f), 0.08f, 0.07f),
                Torus(new Vector3(0f, 0.22f, 0f), 0.07f, 0.03f, Vector3.zero, 0.03f),
                Sphere(new Vector3(0f, 0.28f, 0f), 0.055f, 0.02f).Sub());
            Budget(chimney, 36, 600);

            // Crown of six short chunky nubs on a band.
            var nubPrims = new List<SdfPrimitive> { Torus(Vector3.zero, 0.2f, 0.04f, Vector3.zero) };
            for (int i = 0; i < 6; i++)
            {
                var d = Quaternion.Euler(0f, i * 60f + 30f, 0f) * Vector3.forward * 0.2f;
                nubPrims.Add(Cone(d, d * 1.05f + new Vector3(0f, 0.11f, 0f), 0.05f, 0.015f, 0.04f));
            }
            var nubRing = Part("NubRing", PartMotion.None, Vector3.up, 0f, 0f,
                new PartStyle { color = PaletteSlot.Accent, spec = 0.2f, outline = 0.02f },
                nubPrims.ToArray());
            Budget(nubRing, 48, 900);

            // ---- Toastling: bread-loaf hedgehog on 4 pegs, toasted spines ----
            var toastlingBody = Shape("Toastling_Body",
                Ell(new Vector3(0f, -0.02f, 0f), new Vector3(0.48f, 0.42f, 0.56f)),
                Plane(new Vector3(0f, 0.28f, 0f), Vector3.up, 0.22f),
                Plane(new Vector3(0.38f, 0f, 0f), Vector3.right, 0.22f),
                Plane(new Vector3(-0.38f, 0f, 0f), Vector3.left, 0.22f),
                Plane(new Vector3(0f, 0f, 0.46f), Vector3.forward, 0.22f),
                Plane(new Vector3(0f, 0f, -0.46f), Vector3.back, 0.22f),
                Ell(new Vector3(0f, -0.01f, 0.45f), new Vector3(0.11f, 0.08f, 0.08f), 0.08f),
                Cone(new Vector3(0.24f, -0.26f, 0.28f), new Vector3(0.24f, -0.48f, 0.29f), 0.095f, 0.09f, 0.07f).Mirror(),
                Cone(new Vector3(0.24f, -0.26f, -0.28f), new Vector3(0.24f, -0.48f, -0.29f), 0.095f, 0.09f, 0.07f).Mirror(),
                Floor(-0.5f));
            var toastling = Recipe("Toastling", toastlingBody, 0.036f,
                new CreaturePalette
                {
                    baseColor = Hex("#C4943A"), belly = Hex("#F5EDDA"), accent = Hex("#5C3A1E"), socks = Hex("#C4943A"),
                    glow = Hex("#E5641E"), extra = Hex("#E8B860"), face = Hex("#2A2018"), iris = Hex("#4A3020"),
                },
                new BodyZones
                {
                    patches = new List<PatternPatch>
                    {
                        new PatternPatch { center = new Vector3(0f, -0.27f, 0.48f), radius = 0.2f, color = PaletteSlot.Belly },
                    },
                },
                new FaceSettings
                {
                    eyeSize = 0.05f, eyeSpacing = 0.15f, eyeHeight = 0.09f, eyeStretch = 1.1f,
                    decals = new List<FaceDecal>
                    {
                        new FaceDecal { kind = DecalKind.Disc, at = new Vector2(0f, 0.01f), size = new Vector2(0.035f, 0.025f), color = PaletteSlot.Face },
                        new FaceDecal { kind = DecalKind.Smile, at = new Vector2(0f, -0.06f), size = new Vector2(0.03f, 0f), color = PaletteSlot.Face },
                    },
                },
                new IdleStyle { breathe = 0.018f, breatheSpeed = 2.4f, sway = 1.5f },
                new List<PlacedPart>
                {
                    Place(roundEar, new Vector3(0.23f, 0.19f, 0.33f), new Vector3(0f, 0f, -25f), 0.8f, true),
                    Place(spine, new Vector3(0f, 0.26f, 0.12f), new Vector3(-15f, 0f, 0f), 1f),
                    Place(spine, new Vector3(0.22f, 0.21f, 0.02f), new Vector3(-10f, 0f, -35f), 0.9f, true),
                    Place(spine, new Vector3(0f, 0.26f, -0.2f), new Vector3(-30f, 0f, 0f), 1f),
                    Place(spine, new Vector3(0.22f, 0.19f, -0.3f), new Vector3(-30f, 0f, -35f), 0.9f, true),
                });

            // ---- Crumbhog: Toastling grown up, standing like a little bear, spines all round, crumb crown ----
            var crumbhogBody = Shape("Crumbhog_Body",
                Ell(new Vector3(0f, -0.22f, 0f), new Vector3(0.44f, 0.42f, 0.38f)),
                Ell(new Vector3(0f, 0.33f, 0.04f), new Vector3(0.36f, 0.32f, 0.32f), 0.18f),
                Ell(new Vector3(0f, 0.22f, 0.33f), new Vector3(0.13f, 0.09f, 0.09f), 0.07f),
                Cone(new Vector3(0.35f, -0.04f, 0.08f), new Vector3(0.25f, -0.22f, 0.28f), 0.09f, 0.085f, 0.05f).Mirror(),
                Cone(new Vector3(0.2f, -0.5f, 0.02f), new Vector3(0.21f, -0.74f, 0.06f), 0.13f, 0.125f, 0.06f).Mirror(),
                Floor(-0.77f));
            var crumbhog = Recipe("Crumbhog", crumbhogBody, 0.05f,
                new CreaturePalette
                {
                    baseColor = Hex("#9E6B30"), belly = Hex("#F5EDDA"), accent = Hex("#4A2810"), socks = Hex("#9E6B30"),
                    glow = Hex("#E5641E"), extra = Hex("#D4A84A"), face = Hex("#2A2018"), iris = Hex("#3A2414"),
                },
                new BodyZones
                {
                    patches = new List<PatternPatch>
                    {
                        new PatternPatch { center = new Vector3(0f, -0.26f, 0.38f), radius = 0.26f, color = PaletteSlot.Belly },
                        new PatternPatch { center = new Vector3(0f, 0.21f, 0.41f), radius = 0.13f, color = PaletteSlot.Belly },
                    },
                },
                new FaceSettings
                {
                    eyeSize = 0.058f, eyeSpacing = 0.15f, eyeHeight = 0.36f, eyeStretch = 1.15f,
                    decals = new List<FaceDecal>
                    {
                        new FaceDecal { kind = DecalKind.Disc, at = new Vector2(0f, 0.26f), size = new Vector2(0.045f, 0.032f), color = PaletteSlot.Face },
                        new FaceDecal { kind = DecalKind.Smile, at = new Vector2(0f, 0.17f), size = new Vector2(0.035f, 0f), color = PaletteSlot.Face },
                    },
                },
                new IdleStyle { breathe = 0.02f, breatheSpeed = 2.2f, sway = 2f },
                new List<PlacedPart>
                {
                    Place(roundEar, new Vector3(0.25f, 0.56f, 0f), new Vector3(0f, 0f, -25f), 0.9f, true),
                    Place(spine, new Vector3(0.27f, 0.47f, -0.08f), new Vector3(-25f, 0f, -50f), 1.2f, true),
                    Place(spine, new Vector3(0.38f, 0.2f, -0.1f), new Vector3(-15f, 0f, -80f), 1.25f, true),
                    Place(spine, new Vector3(0.4f, -0.18f, -0.12f), new Vector3(0f, 0f, -105f), 1.2f, true),
                    Place(spine, new Vector3(0f, 0.45f, -0.22f), new Vector3(-60f, 0f, 0f), 1.25f),
                    Place(spine, new Vector3(0.16f, 0.1f, -0.33f), new Vector3(-90f, 0f, -20f), 1.25f, true),
                    Place(spine, new Vector3(0f, -0.25f, -0.36f), new Vector3(-100f, 0f, 0f), 1.2f),
                    Place(nugget, new Vector3(0f, 0.63f, 0.06f), Vector3.zero, 1.3f),
                    Place(nugget, new Vector3(0.1f, 0.6f, 0.08f), new Vector3(0f, 40f, -20f), 1.1f, true),
                });

            // ---- Waxle: soft snail with a dripping candle for a shell ----
            var waxleBody = Shape("Waxle_Body",
                Ell(new Vector3(0f, -0.4f, 0.04f), new Vector3(0.32f, 0.12f, 0.6f)),
                Ell(new Vector3(0f, -0.47f, -0.08f), new Vector3(0.44f, 0.05f, 0.62f), 0.1f).Bumps(0.02f, 9f),
                Cone(new Vector3(0f, -0.33f, 0.36f), new Vector3(0f, 0f, 0.52f), 0.2f, 0.17f, 0.12f),
                Sphere(new Vector3(0f, 0.05f, 0.56f), 0.2f, 0.1f),
                Cone(new Vector3(0.08f, 0.17f, 0.58f), new Vector3(0.13f, 0.42f, 0.6f), 0.04f, 0.034f, 0.05f).Mirror(),
                Sphere(new Vector3(0.13f, 0.45f, 0.61f), 0.085f, 0.04f).Mirror(),
                Floor(-0.5f));
            var waxle = Recipe("Waxle", waxleBody, 0.036f,
                new CreaturePalette
                {
                    baseColor = Hex("#F0E0A0"), belly = Hex("#F5E6C8"), accent = Hex("#D8C080"), socks = Hex("#F0E0A0"),
                    glow = Hex("#E5641E"), extra = Hex("#FFD060"), face = Hex("#3A2A18"), iris = Hex("#3A2A18"),
                },
                new BodyZones(),
                new FaceSettings
                {
                    eyeSize = 0.055f, eyeSpacing = 0.13f, eyeHeight = 0.46f, eyeStretch = 1.1f,
                    decals = new List<FaceDecal>
                    {
                        new FaceDecal { kind = DecalKind.Smile, at = new Vector2(0f, 0.01f), size = new Vector2(0.04f, 0f), color = PaletteSlot.Face },
                    },
                },
                new IdleStyle { breathe = 0.015f, breatheSpeed = 1.8f, jiggle = 0.006f, sway = 1f },
                new List<PlacedPart>
                {
                    Place(candleShell, new Vector3(0f, -0.4f, -0.14f), new Vector3(-6f, 0f, 0f), 1.3f),
                    Place(flamePuff, new Vector3(0f, 0.58f, 0f), Vector3.zero, 0.95f, false, 0),
                });

            // ---- Lanternsnail: bigger golden snail with a lantern for a shell ----
            var lanternsnailBody = Shape("Lanternsnail_Body",
                Ell(new Vector3(0f, -0.4f, 0.04f), new Vector3(0.34f, 0.13f, 0.66f)),
                Cone(new Vector3(0f, -0.33f, 0.4f), new Vector3(0f, 0.02f, 0.56f), 0.21f, 0.18f, 0.12f),
                Sphere(new Vector3(0f, 0.08f, 0.6f), 0.21f, 0.1f),
                Cone(new Vector3(0.08f, 0.22f, 0.62f), new Vector3(0.15f, 0.56f, 0.64f), 0.04f, 0.034f, 0.05f).Mirror(),
                Sphere(new Vector3(0.15f, 0.59f, 0.65f), 0.08f, 0.04f).Mirror(),
                Sphere(new Vector3(0.05f, -0.46f, -0.72f), 0.06f, 0.06f),
                Sphere(new Vector3(-0.06f, -0.47f, -0.86f), 0.045f, 0.05f),
                Floor(-0.5f));
            var lanternsnail = Recipe("Lanternsnail", lanternsnailBody, 0.045f,
                new CreaturePalette
                {
                    baseColor = Hex("#D4A84A"), belly = Hex("#F5E6C8"), accent = Hex("#7A5A2A"), socks = Hex("#D4A84A"),
                    glow = Hex("#FF8C28"), extra = Hex("#F0C060"), face = Hex("#3A2A18"), iris = Hex("#3A2A18"),
                },
                new BodyZones
                {
                    patches = new List<PatternPatch>
                    {
                        new PatternPatch { center = new Vector3(0f, -0.47f, -0.8f), radius = 0.14f, color = PaletteSlot.Belly },
                    },
                },
                new FaceSettings
                {
                    eyeSize = 0.052f, eyeSpacing = 0.15f, eyeHeight = 0.6f, eyeStretch = 1.1f,
                    decals = new List<FaceDecal>
                    {
                        new FaceDecal { kind = DecalKind.Smile, at = new Vector2(0f, 0.04f), size = new Vector2(0.045f, 0f), color = PaletteSlot.Face },
                    },
                },
                new IdleStyle { breathe = 0.015f, breatheSpeed = 1.7f, jiggle = 0.006f, sway = 2f, swaySpeed = 0.9f },
                new List<PlacedPart>
                {
                    Place(lanternGlass, new Vector3(0f, -0.36f, -0.16f), Vector3.zero, 1.35f),
                    Place(lanternFrame, new Vector3(0f, -0.36f, -0.16f), Vector3.zero, 1.35f),
                    Place(flamePuff, new Vector3(0f, 0.63f, 0f), Vector3.zero, 0.6f, false, 1),
                });

            // ---- Kettlewing: kettle-shaped duckling, spout beak, lid, wing nubs, orange feet ----
            var kettlewingBody = Shape("Kettlewing_Body",
                Ell(new Vector3(0f, -0.08f, 0f), new Vector3(0.47f, 0.42f, 0.45f)),
                Ell(new Vector3(0f, 0.18f, 0f), new Vector3(0.36f, 0.2f, 0.34f), 0.2f),
                Cone(new Vector3(0.17f, -0.42f, 0.06f), new Vector3(0.17f, -0.6f, 0.1f), 0.07f, 0.075f, 0.05f).Mirror(),
                Ell(new Vector3(0.17f, -0.61f, 0.15f), new Vector3(0.08f, 0.04f, 0.11f), 0.05f).Mirror(),
                Floor(-0.65f));
            var kettlewing = Recipe("Kettlewing", kettlewingBody, 0.04f,
                new CreaturePalette
                {
                    baseColor = Hex("#B8784A"), belly = Hex("#F0DFC0"), accent = Hex("#8A5A32"), socks = Hex("#E58A32"),
                    glow = Hex("#F4F4F4"), extra = Hex("#C0963C"), face = Hex("#2A2018"), iris = Hex("#2A2018"),
                },
                new BodyZones
                {
                    socks = true, socksY = -0.5f,
                    patches = new List<PatternPatch>
                    {
                        new PatternPatch { center = new Vector3(0f, -0.3f, 0.4f), radius = 0.22f, color = PaletteSlot.Belly },
                    },
                },
                new FaceSettings { eyeSize = 0.06f, eyeSpacing = 0.17f, eyeHeight = 0.13f, eyeStretch = 1.2f, decals = new List<FaceDecal>() },
                new IdleStyle { breathe = 0.016f, breatheSpeed = 2.4f, sway = 2f },
                new List<PlacedPart>
                {
                    Place(kettleLid, new Vector3(0f, 0.32f, 0f), Vector3.zero, 1f),
                    Place(spoutBeak, new Vector3(0f, -0.08f, 0.38f), new Vector3(14f, 0f, 0f), 1.05f),
                    Place(steamPuff, new Vector3(0f, 0.2f, 0.33f), Vector3.zero, 0.9f, false, 1),
                    Place(wingNub, new Vector3(0.43f, -0.04f, -0.04f), new Vector3(0f, 15f, 15f), 1f, true),
                });
            kettlewing.spec = 0.45f;
            EditorUtility.SetDirty(kettlewing);

            // ---- Glazegolem (rare): stack of three glazed pots, glowing cracks that pulse, slow heavy sway ----
            var glazegolemBody = Shape("Glazegolem_Body",
                Ell(new Vector3(0f, -0.33f, 0f), new Vector3(0.46f, 0.3f, 0.42f)),
                Torus(new Vector3(0f, -0.06f, 0f), 0.27f, 0.055f, Vector3.zero, 0.03f),
                Ell(new Vector3(0f, 0.13f, 0f), new Vector3(0.36f, 0.19f, 0.33f), 0.04f),
                Torus(new Vector3(0f, 0.3f, 0f), 0.22f, 0.05f, Vector3.zero, 0.03f),
                Ell(new Vector3(0f, 0.47f, 0f), new Vector3(0.28f, 0.17f, 0.26f), 0.04f),
                Ell(new Vector3(0f, 0.66f, 0f), new Vector3(0.16f, 0.12f, 0.16f), 0.03f).Sub(),
                Cone(new Vector3(0.2f, -0.55f, 0.04f), new Vector3(0.2f, -0.74f, 0.06f), 0.12f, 0.12f, 0.06f).Mirror(),
                Floor(-0.76f));
            var glazegolem = Recipe("Glazegolem", glazegolemBody, 0.058f,
                new CreaturePalette
                {
                    baseColor = Hex("#A0522D"), belly = Hex("#D98E32"), accent = Hex("#4A2414"), socks = Hex("#A0522D"),
                    glow = Hex("#FF7A28"), extra = Hex("#6B3A1E"), face = Hex("#2A1A14"), iris = Hex("#FF7A28"),
                },
                new BodyZones
                {
                    belly = true, bellyDirection = Vector3.up, bellyAt = 0.55f, bellyMaxY = 9f, bellyDrips = 9f, bellyDripLength = 0.9f,
                    cracks = true, crackScale = 1.6f, crackWidth = 0.045f, crackPulse = 0.8f,
                    patches = new List<PatternPatch>
                    {
                        new PatternPatch { center = new Vector3(0f, 0.6f, 0f), radius = 0.17f, color = PaletteSlot.Accent },
                    },
                },
                new FaceSettings { eyeSize = 0.055f, eyeSpacing = 0.11f, eyeHeight = 0.47f, eyeStretch = 0.8f, decals = new List<FaceDecal>() },
                new IdleStyle { breathe = 0.008f, breatheSpeed = 1.4f, sway = 3f, swaySpeed = 0.6f },
                new List<PlacedPart>
                {
                    Place(potRim, new Vector3(0f, 0.62f, 0f), Vector3.zero, 1f),
                    Place(stubArm, new Vector3(0.34f, 0.14f, 0f), Vector3.zero, 1.15f, true),
                });
            glazegolem.spec = 0.25f;
            EditorUtility.SetDirty(glazegolem);

            // ---- Kilngolem: Glazegolem + modifiers (bigger, chimney with a flame, crown of nubs, own palette) ----
            var kilngolem = Asset<CreatureRecipe>($"{Root}/Recipes/Kilngolem.asset");
            kilngolem.baseRecipe = glazegolem;
            kilngolem.heightScale = 1.2f;
            kilngolem.extraParts = new List<PlacedPart>
            {
                Place(nubRing, new Vector3(0f, 0.63f, 0f), Vector3.zero, 1.05f),
                Place(chimney, new Vector3(0f, 0.58f, 0f), Vector3.zero, 1.1f),
                Place(flamePuff, new Vector3(0f, 0.24f, 0f), Vector3.zero, 0.6f, false, 1),
            };
            kilngolem.overridePalette = true;
            kilngolem.palette = new CreaturePalette
            {
                baseColor = Hex("#A0522D"), belly = Hex("#E0A040"), accent = Hex("#6B3A1E"), socks = Hex("#A0522D"),
                glow = Hex("#FF9A3C"), extra = Hex("#3A3A3A"), face = Hex("#2A1A14"), iris = Hex("#FF8A30"),
            };
            EditorUtility.SetDirty(kilngolem);

            // ---- species (all Fire; Steamswan is skipped until its concept is redrawn) ----
            var crumbhogSpecies = FireSpecies("Crumbhog", crumbhog, null, 0, 58, 14, 14, 9);
            FireSpecies("Toastling", toastling, crumbhogSpecies, 16, 40, 10, 10, 9);
            var lanternsnailSpecies = FireSpecies("Lanternsnail", lanternsnail, null, 0, 56, 13, 13, 7);
            FireSpecies("Waxle", waxle, lanternsnailSpecies, 18, 42, 9, 11, 6);
            FireSpecies("Kettlewing", kettlewing, null, 0, 38, 11, 9, 12);
            var kilngolemSpecies = FireSpecies("Kilngolem", kilngolem, null, 0, 72, 15, 18, 5);
            FireSpecies("Glazegolem", glazegolem, kilngolemSpecies, 26, 54, 12, 15, 5);

            AssetDatabase.SaveAssets();
            Debug.Log($"[FireKit] Wrote 12 parts, 7 fire recipes and species to {Root}.");
        }

        static CreatureSpecies FireSpecies(string name, CreatureRecipe recipe, CreatureSpecies evolvesTo, int level,
                                           int hp, int attack, int defense, int speed)
        {
            var s = Species(name, Element.Fire, recipe, evolvesTo, level);
            s.baseStats = new BaseStats { hp = hp, attack = attack, defense = defense, speed = speed };
            EditorUtility.SetDirty(s);
            return s;
        }

        static void Budget(CreaturePart part, int resolution, int triangles)
        {
            part.resolution = resolution;
            part.triangleBudget = triangles;
            EditorUtility.SetDirty(part);
        }

        /// <summary>Cuts away everything past the plane through <paramref name="point"/> facing <paramref name="normal"/>.</summary>
        static SdfPrimitive Plane(Vector3 point, Vector3 normal, float k) =>
            new SdfPrimitive { kind = SdfKind.Plane, blend = SdfBlend.Intersect, a = point, b = normal, k = k };
    }
}
