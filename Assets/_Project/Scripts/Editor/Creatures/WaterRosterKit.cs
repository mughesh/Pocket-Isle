using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Diorama.EditorTools.StarterKit;
using static Diorama.EditorTools.WindRosterKit;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 11c A: the water roster (concept sheets 13 and 14, Concepts_03_Summary.md). Marimoss -> Marimound (moss ball,
    /// then a little grassy hill with flowers), Drizzcan -> Rainurn (watering-can elephant calf, then a banded urn
    /// elephant), Lilyvole -> Lotusvole (vole with a lily-pad hat, then a lotus hat), Sailfold -> Galleyfold (paper boat,
    /// then three sails), Bottlewhale -> Lighthousewhale (rare: a corked bottle whale with a ship painted inside, then a
    /// lighthouse on its back). Rigid pivot parts with idle motions (drops drip, the pad wobbles, the boat rocks, the
    /// cork pops, the lamp glows). Three moves each (a Water attack, a Neutral one, a support). Evolve at 16 (the rare at
    /// 18). Riverloom (Pebbloop's) is in <see cref="WindRosterKit"/>. Re-running overwrites (same files).
    /// </summary>
    public static class WaterRosterKit
    {
        static IslingSpecies WaterSpecies(string name, CreatureRecipe recipe, IslingSpecies evolvesTo, int level, int hp, int attack, int defense, int speed,
                                          params MoveData[] moves)
        {
            var s = Species(name, Element.Water, recipe, evolvesTo, level);
            s.baseStats = new BaseStats { hp = hp, attack = attack, defense = defense, speed = speed };
            s.moves = moves;
            EditorUtility.SetDirty(s);
            return s;
        }

        static CreaturePalette Palette(string body, string belly, string accent, string socks, string glow, string extra) => new CreaturePalette
        {
            baseColor = Hex(body), belly = Hex(belly), accent = Hex(accent), socks = Hex(socks), glow = Hex(glow), extra = Hex(extra),
            face = Hex("#2A2018"), iris = Hex("#2A2018"),
        };

        /// <summary>A water drop that drips: falls a little along -Y and comes back (Bob), light cyan.</summary>
        static CreaturePart Drop(string name, float r)
        {
            var p = Part(name, PartMotion.Bob, Vector3.down, 0.05f, 0.9f, new PartStyle { color = PaletteSlot.Glow, spec = 0.8f, outline = 0.015f, glow = 0.15f },
                Sphere(new Vector3(0f, -r * 0.3f, 0f), r * 0.85f, 0f),
                Cone(new Vector3(0f, -r * 0.3f, 0f), new Vector3(0f, r * 0.7f, 0f), r * 0.8f, r * 0.15f, 0.04f));
            Budget(p, 22, 220);
            return p;
        }

        /// <summary>A thick C-shaped handle arc (a torus standing upright), in the accent colour.</summary>
        static CreaturePart Handle(string name, float ring, float tube, Vector3 rotation)
        {
            var p = Part(name, PartMotion.None, Vector3.up, 0f, 0f, Style(PaletteSlot.Accent, 0.35f, 0.02f),
                Torus(Vector3.zero, ring, tube, rotation, 0f));
            Budget(p, 30, 420);
            return p;
        }

        // ================= 1. Marimoss -> Marimound =================

        /// <summary>Marimoss: a smooth round moss ball with a two-leaf sprout, two tiny feet. Marimound: a little grassy hill with flowers.</summary>
        public static IslingSpecies Marimoss()
        {
            var sprout = Part("MossSprout", PartMotion.Wag, Vector3.forward, 10f, 0.5f, Style(PaletteSlot.Extra, 0.3f, 0.02f),
                Cone(Vector3.zero, new Vector3(0f, 0.12f, 0f), 0.025f, 0.02f, 0f),
                Ell(new Vector3(0.07f, 0.15f, 0f), new Vector3(0.075f, 0.035f, 0.05f), 0.03f, new Vector3(0f, 0f, 20f)),
                Ell(new Vector3(-0.07f, 0.15f, 0f), new Vector3(0.075f, 0.035f, 0.05f), 0.03f, new Vector3(0f, 0f, -20f)));
            Budget(sprout, 28, 420);
            var foot = PegFoot("MossFoot", 0.05f, 0.06f, PaletteSlot.Socks);
            var body = Shape("Marimoss_Body",
                Sphere(new Vector3(0f, 0.02f, 0f), 0.42f, 0f),
                Floor(-0.36f));
            var moss = Recipe("Marimoss", body, 0.036f,
                Palette("#5A8A4A", "#8AB870", "#4A7A3A", "#4A7A3A", "#E8A090", "#6BAA58"),
                Patches((new Vector3(0f, -0.12f, 0.34f), 0.28f, PaletteSlot.Belly), (new Vector3(0.24f, 0.04f, 0.33f), 0.06f, PaletteSlot.Glow),
                        (new Vector3(-0.24f, 0.04f, 0.33f), 0.06f, PaletteSlot.Glow)),
                Face(0.048f, 0.13f, 0.12f, 1.15f, true, 0.04f),
                new IdleStyle { breathe = 0.014f, breatheSpeed = 1.6f, hover = 0.006f, bob = 0.004f, sway = 3f },
                new List<PlacedPart>
                {
                    Place(sprout, new Vector3(0f, 0.42f, 0f), Vector3.zero, 1f),
                    Place(foot, new Vector3(0.13f, -0.3f, 0.06f), Vector3.zero, 1f, true),
                });
            moss.spec = 0.18f;
            EditorUtility.SetDirty(moss);

            CreaturePart Flower(string name, PaletteSlot slot)
            {
                var prims = new List<SdfPrimitive> { Sphere(Vector3.zero, 0.03f, 0f) };
                for (int i = 0; i < 5; i++)
                {
                    var d = Quaternion.Euler(0f, i * 72f, 0f) * Vector3.forward;
                    prims.Add(Ell(d * 0.045f, new Vector3(0.035f, 0.016f, 0.035f), 0.02f));
                }
                var f = Part(name, PartMotion.Wag, Vector3.forward, 8f, 0.6f, new PartStyle { color = slot, spec = 0.3f, outline = 0.015f }, prims.ToArray());
                Budget(f, 26, 320);
                return f;
            }
            var pink = Flower("MoundFlowerPink", PaletteSlot.Accent);
            var blue = Flower("MoundFlowerBlue", PaletteSlot.Glow);
            var yellow = Flower("MoundFlowerYellow", PaletteSlot.Extra);
            var bigFoot = PegFoot("MoundFoot", 0.07f, 0.07f, PaletteSlot.Base);
            var mBody = Shape("Marimound_Body",
                Ell(new Vector3(0f, -0.02f, 0f), new Vector3(0.56f, 0.4f, 0.48f), 0f),
                Ell(new Vector3(0.18f, 0.16f, -0.1f), new Vector3(0.3f, 0.26f, 0.3f), 0.12f),
                Ell(new Vector3(-0.2f, 0.12f, -0.12f), new Vector3(0.28f, 0.24f, 0.28f), 0.12f),
                Floor(-0.34f));
            var mound = Recipe("Marimound", mBody, 0.056f,
                Palette("#5A8A4A", "#8AB870", "#E888A0", "#E8A090", "#88B8E0", "#F0D060"),
                Patches((new Vector3(0f, -0.12f, 0.42f), 0.3f, PaletteSlot.Belly), (new Vector3(0.27f, 0.0f, 0.4f), 0.06f, PaletteSlot.Socks),
                        (new Vector3(-0.27f, 0.0f, 0.4f), 0.06f, PaletteSlot.Socks)),
                Face(0.045f, 0.15f, 0.06f, 1.1f, true, -0.02f),
                new IdleStyle { breathe = 0.012f, breatheSpeed = 1.2f, hover = 0.004f, bob = 0.003f, sway = 2f },
                new List<PlacedPart>
                {
                    Place(pink, new Vector3(-0.18f, 0.4f, 0.14f), new Vector3(30f, 0f, 10f), 1.5f),
                    Place(blue, new Vector3(0.04f, 0.45f, 0.08f), new Vector3(25f, 0f, -5f), 1.4f),
                    Place(yellow, new Vector3(0.25f, 0.4f, 0.06f), new Vector3(25f, 0f, -15f), 1.4f),
                    Place(pink, new Vector3(0.12f, 0.44f, -0.14f), new Vector3(10f, 0f, 0f), 1.2f),
                    Place(bigFoot, new Vector3(0.2f, -0.28f, 0.1f), Vector3.zero, 1f, true),
                });
            mound.spec = 0.18f;
            EditorUtility.SetDirty(mound);

            var splash = Attack("MossSplash", "Moss Splash", Element.Water, 40, 95, MoveAnimation.Shoot);
            var moundS = WaterSpecies("Marimound", mound, null, 0, 66, 13, 17, 9, splash, LoadMove("BodySlam"), LoadMove("HunkerDown"));
            var s = WaterSpecies("Marimoss", moss, moundS, 16, 46, 9, 12, 9, splash, LoadMove("Tackle"), LoadMove("HunkerDown"));
            AssetDatabase.SaveAssets();
            return s;
        }

        // ================= 2. Drizzcan -> Rainurn =================

        /// <summary>
        /// Drizzcan: a round elephant calf like a watering can: a short chunky spout-trunk with drops dripping off it,
        /// floppy round ears, a thick C handle on its back. Rainurn: an urn-shaped body with bands and two side handles.
        /// </summary>
        public static IslingSpecies Drizzcan()
        {
            var spout = Part("CanSpout", PartMotion.None, Vector3.up, 0f, 0f, Style(PaletteSlot.Base, 0.3f, 0.022f),
                Cone(Vector3.zero, new Vector3(0f, -0.05f, 0.12f), 0.085f, 0.07f, 0f),
                Torus(new Vector3(0f, -0.055f, 0.13f), 0.07f, 0.028f, new Vector3(112f, 0f, 0f), 0.02f));
            Budget(spout, 30, 480);
            var drop = Drop("CanDrop", 0.032f);
            var ear = Part("CanEar", PartMotion.Flap, Vector3.up, 14f, 0.5f,
                new PartStyle { color = PaletteSlot.Base, spec = 0.2f, outline = 0.02f, inner = true, innerColor = PaletteSlot.Belly, innerDirection = Vector3.forward, innerAt = 0.5f },
                Ell(new Vector3(0.11f, 0f, 0f), new Vector3(0.12f, 0.12f, 0.03f), 0f));
            Budget(ear, 26, 360);
            var handle = Handle("CanHandle", 0.12f, 0.035f, new Vector3(0f, 0f, 90f));
            var body = Shape("Drizzcan_Body",
                Sphere(new Vector3(0f, -0.06f, -0.08f), 0.36f, 0f),
                Sphere(new Vector3(0f, 0.16f, 0.2f), 0.24f, 0.12f),
                Cone(new Vector3(0.18f, -0.24f, 0.08f), new Vector3(0.18f, -0.4f, 0.1f), 0.075f, 0.07f, 0.05f).Mirror(),
                Cone(new Vector3(0.17f, -0.24f, -0.26f), new Vector3(0.17f, -0.4f, -0.28f), 0.075f, 0.07f, 0.05f).Mirror(),
                Floor(-0.41f));
            var can = Recipe("Drizzcan", body, 0.04f,
                Palette("#7A9AB0", "#B8D0E0", "#6888A0", "#6888A0", "#88C8E8", "#88C8E8"),
                Patches((new Vector3(0f, -0.2f, 0.18f), 0.24f, PaletteSlot.Belly)),
                Face(0.042f, 0.11f, 0.22f, 1.15f, false),
                new IdleStyle { breathe = 0.014f, breatheSpeed = 1.8f, jiggle = 0.003f, sway = 2f },
                new List<PlacedPart>
                {
                    Place(spout, new Vector3(0f, 0.1f, 0.4f), new Vector3(10f, 0f, 0f), 1f),
                    Place(drop, new Vector3(0f, 0.0f, 0.58f), Vector3.zero, 1f),
                    Place(drop, new Vector3(0.05f, -0.05f, 0.56f), Vector3.zero, 0.8f),
                    Place(ear, new Vector3(0.18f, 0.22f, 0.12f), new Vector3(0f, -30f, -10f), 1f, true),
                    Place(handle, new Vector3(0f, 0.18f, -0.4f), Vector3.zero, 1f),
                });
            EditorUtility.SetDirty(can);

            var bigSpout = Part("UrnSpout", PartMotion.None, Vector3.up, 0f, 0f, Style(PaletteSlot.Base, 0.3f, 0.022f),
                Cone(Vector3.zero, new Vector3(0f, -0.06f, 0.2f), 0.09f, 0.07f, 0f),
                Torus(new Vector3(0f, -0.065f, 0.21f), 0.07f, 0.03f, new Vector3(110f, 0f, 0f), 0.02f));
            Budget(bigSpout, 30, 520);
            var band = Part("UrnBand", PartMotion.None, Vector3.up, 0f, 0f, Style(PaletteSlot.Accent, 0.35f, 0f),
                Torus(Vector3.zero, 0.42f, 0.03f, Vector3.zero, 0f));
            Budget(band, 40, 700);
            var sideHandle = Handle("UrnHandle", 0.11f, 0.035f, new Vector3(90f, 0f, 0f));
            var bigEar = Part("UrnEar", PartMotion.Flap, Vector3.up, 12f, 0.4f,
                new PartStyle { color = PaletteSlot.Base, spec = 0.2f, outline = 0.02f, inner = true, innerColor = PaletteSlot.Belly, innerDirection = Vector3.forward, innerAt = 0.5f },
                Ell(new Vector3(0.14f, 0f, 0f), new Vector3(0.15f, 0.15f, 0.035f), 0f));
            Budget(bigEar, 26, 380);
            var uBody = Shape("Rainurn_Body",
                Ell(new Vector3(0f, -0.04f, -0.06f), new Vector3(0.44f, 0.44f, 0.42f), 0f),
                Cone(new Vector3(0f, 0.3f, -0.06f), new Vector3(0f, 0.44f, -0.06f), 0.24f, 0.17f, 0.06f),
                Torus(new Vector3(0f, 0.47f, -0.06f), 0.2f, 0.055f, Vector3.zero, 0.03f),
                Cone(new Vector3(0f, 0.38f, -0.06f), new Vector3(0f, 0.6f, -0.06f), 0.15f, 0.15f, 0.02f).Sub(),
                Sphere(new Vector3(0f, 0.14f, 0.3f), 0.25f, 0.12f),
                Cone(new Vector3(0.22f, -0.28f, 0.12f), new Vector3(0.22f, -0.5f, 0.14f), 0.095f, 0.09f, 0.05f).Mirror(),
                Cone(new Vector3(0.21f, -0.28f, -0.28f), new Vector3(0.21f, -0.5f, -0.3f), 0.095f, 0.09f, 0.05f).Mirror(),
                Floor(-0.51f));
            var urn = Recipe("Rainurn", uBody, 0.06f,
                Palette("#7A9AB0", "#B8D0E0", "#6888A0", "#6888A0", "#88C8E8", "#88C8E8"),
                Patches((new Vector3(0f, -0.2f, 0.32f), 0.24f, PaletteSlot.Belly)),
                Face(0.045f, 0.12f, 0.22f, 1.15f, false),
                new IdleStyle { breathe = 0.012f, breatheSpeed = 1.4f, jiggle = 0.002f, sway = 1.5f },
                new List<PlacedPart>
                {
                    Place(bigSpout, new Vector3(0f, 0.08f, 0.52f), new Vector3(8f, 0f, 0f), 1f),
                    Place(drop, new Vector3(0f, 0.0f, 0.76f), Vector3.zero, 1.1f),
                    Place(drop, new Vector3(0.06f, -0.06f, 0.74f), Vector3.zero, 0.9f),
                    Place(drop, new Vector3(-0.05f, -0.05f, 0.75f), Vector3.zero, 0.85f),
                    Place(drop, new Vector3(0.02f, -0.11f, 0.72f), Vector3.zero, 0.75f),
                    Place(bigEar, new Vector3(0.2f, 0.2f, 0.22f), new Vector3(0f, -30f, -10f), 1f, true),
                    Place(band, new Vector3(0f, -0.12f, -0.06f), Vector3.zero, 1.04f),
                    Place(band, new Vector3(0f, 0.12f, -0.06f), Vector3.zero, 0.97f),
                    Place(sideHandle, new Vector3(0.42f, 0.18f, -0.1f), new Vector3(0f, 0f, 0f), 1f, true),
                });
            EditorUtility.SetDirty(urn);

            var spray = Attack("SpoutSpray", "Spout Spray", Element.Water, 40, 95, MoveAnimation.Shoot, true);
            var urnS = WaterSpecies("Rainurn", urn, null, 0, 62, 15, 14, 11, spray, LoadMove("BodySlam"), LoadMove("PuffUp"));
            var s = WaterSpecies("Drizzcan", can, urnS, 16, 44, 11, 10, 10, spray, LoadMove("Tackle"), LoadMove("PuffUp"));
            AssetDatabase.SaveAssets();
            return s;
        }

        // ================= 3. Lilyvole -> Lotusvole =================

        /// <summary>Lilyvole: an egg-round brown vole with a lily-pad hat that wobbles, pink nose, paddle tail. Lotusvole: a lotus on the pad.</summary>
        public static IslingSpecies Lilyvole()
        {
            var hat = Part("LilyHat", PartMotion.Wag, Vector3.forward, 7f, 0.7f, Style(PaletteSlot.Extra, 0.3f, 0.02f),
                Cone(new Vector3(0f, -0.06f, 0f), new Vector3(0f, 0.01f, 0f), 0.022f, 0.02f, 0f),
                Ell(new Vector3(0f, 0.02f, 0f), new Vector3(0.36f, 0.036f, 0.34f), 0.02f, new Vector3(-6f, 0f, 0f)),
                Box(new Vector3(0.24f, 0.02f, 0.05f), new Vector3(0.14f, 0.06f, 0.025f), 0f, 0.01f, new Vector3(0f, -20f, 0f)).Sub());
            Budget(hat, 44, 760);
            var nose = Part("VoleNose", PartMotion.None, Vector3.up, 0f, 0f, Style(PaletteSlot.Glow, 0.5f, 0.012f), Sphere(Vector3.zero, 0.035f, 0f));
            Budget(nose, 20, 160);
            var ear = RoundEar("VoleEar", new Vector3(0.055f, 0.055f, 0.03f));
            var tail = Part("VoleTail", PartMotion.Wag, Vector3.up, 16f, 0.9f, Style(PaletteSlot.Base, 0.25f, 0.02f),
                Ell(new Vector3(0f, 0f, -0.1f), new Vector3(0.07f, 0.035f, 0.12f), 0f));
            Budget(tail, 24, 280);
            var body = Shape("Lilyvole_Body",
                Ell(new Vector3(0f, -0.08f, 0f), new Vector3(0.34f, 0.36f, 0.3f), 0f),
                Sphere(new Vector3(0f, 0.28f, 0.05f), 0.24f, 0.12f),
                Ell(new Vector3(0f, 0.2f, 0.25f), new Vector3(0.1f, 0.08f, 0.06f), 0.06f),
                Ell(new Vector3(0.2f, -0.06f, 0.2f), new Vector3(0.06f, 0.07f, 0.05f), 0.04f).Mirror(),
                Cone(new Vector3(0.14f, -0.3f, 0.08f), new Vector3(0.14f, -0.42f, 0.1f), 0.06f, 0.055f, 0.04f).Mirror(),
                Cone(new Vector3(0.14f, -0.3f, -0.14f), new Vector3(0.14f, -0.42f, -0.15f), 0.06f, 0.055f, 0.04f).Mirror(),
                Floor(-0.43f));
            var vole = Recipe("Lilyvole", body, 0.038f,
                Palette("#8B7060", "#F0E8D8", "#7A6050", "#7A6050", "#D4888A", "#5AA868"),
                Patches((new Vector3(0f, -0.12f, 0.26f), 0.22f, PaletteSlot.Belly), (new Vector3(0f, 0.2f, 0.28f), 0.09f, PaletteSlot.Belly),
                        (new Vector3(0.15f, 0.25f, 0.24f), 0.045f, PaletteSlot.Glow), (new Vector3(-0.15f, 0.25f, 0.24f), 0.045f, PaletteSlot.Glow)),
                Face(0.045f, 0.1f, 0.32f, 1.15f, true, 0.17f),
                new IdleStyle { breathe = 0.016f, breatheSpeed = 2.2f, jiggle = 0.004f, sway = 2f },
                new List<PlacedPart>
                {
                    Place(hat, new Vector3(0f, 0.52f, 0.02f), Vector3.zero, 1f),
                    Place(nose, new Vector3(0f, 0.23f, 0.31f), Vector3.zero, 1f),
                    Place(ear, new Vector3(0.16f, 0.45f, 0.02f), new Vector3(0f, 0f, -25f), 1f, true),
                    Place(tail, new Vector3(0f, -0.3f, -0.3f), new Vector3(15f, 0f, 0f), 1f),
                });
            EditorUtility.SetDirty(vole);

            var lotusHat = Part("LotusHatPad", PartMotion.None, Vector3.up, 0f, 0f, Style(PaletteSlot.Extra, 0.3f, 0.02f),
                Cone(new Vector3(0f, -0.07f, 0f), new Vector3(0f, 0.01f, 0f), 0.025f, 0.022f, 0f),
                Ell(new Vector3(0f, 0.02f, 0f), new Vector3(0.4f, 0.038f, 0.38f), 0.02f));
            Budget(lotusHat, 44, 700);
            var bloomPrims = new List<SdfPrimitive> { Sphere(new Vector3(0f, 0.04f, 0f), 0.06f, 0f) };
            for (int i = 0; i < 6; i++)
            {
                var d = Quaternion.Euler(0f, i * 60f, 0f) * Vector3.forward;
                bloomPrims.Add(Ell(d * 0.1f + Vector3.up * 0.07f, new Vector3(0.05f, 0.1f, 0.03f), 0f, new Vector3(40f, i * 60f, 0f)));
            }
            var bloom = Part("LotusBloom", PartMotion.Bob, Vector3.up, 0.012f, 0.4f,
                new PartStyle { color = PaletteSlot.Accent, spec = 0.4f, outline = 0.015f, tip = true, tipColor = PaletteSlot.Belly, tipAxis = Vector3.up, tipAt = 0.12f },
                bloomPrims.ToArray());
            Budget(bloom, 34, 900);
            var bigEar = RoundEar("LotusVoleEar", new Vector3(0.07f, 0.07f, 0.035f));
            var bigTail = Part("LotusVoleTail", PartMotion.Wag, Vector3.up, 12f, 0.6f, Style(PaletteSlot.Base, 0.25f, 0.02f),
                Ell(new Vector3(0f, 0f, -0.13f), new Vector3(0.1f, 0.045f, 0.15f), 0f));
            Budget(bigTail, 24, 300);
            var lBody = Shape("Lotusvole_Body",
                Ell(new Vector3(0f, -0.06f, 0f), new Vector3(0.4f, 0.44f, 0.36f), 0f),
                Sphere(new Vector3(0f, 0.38f, 0.06f), 0.27f, 0.12f),
                Ell(new Vector3(0f, 0.3f, 0.29f), new Vector3(0.11f, 0.09f, 0.07f), 0.06f),
                Ell(new Vector3(0.24f, -0.02f, 0.24f), new Vector3(0.07f, 0.08f, 0.06f), 0.04f).Mirror(),
                Cone(new Vector3(0.17f, -0.36f, 0.1f), new Vector3(0.17f, -0.5f, 0.12f), 0.075f, 0.07f, 0.04f).Mirror(),
                Cone(new Vector3(0.17f, -0.36f, -0.16f), new Vector3(0.17f, -0.5f, -0.17f), 0.075f, 0.07f, 0.04f).Mirror(),
                Floor(-0.51f));
            var lotus = Recipe("Lotusvole", lBody, 0.056f,
                Palette("#8B7060", "#F0E8D8", "#F0C0D0", "#7A6050", "#D4888A", "#5AA868"),
                Patches((new Vector3(0f, -0.1f, 0.32f), 0.26f, PaletteSlot.Belly), (new Vector3(0f, 0.3f, 0.32f), 0.1f, PaletteSlot.Belly),
                        (new Vector3(0.17f, 0.34f, 0.27f), 0.05f, PaletteSlot.Glow), (new Vector3(-0.17f, 0.34f, 0.27f), 0.05f, PaletteSlot.Glow)),
                Face(0.048f, 0.11f, 0.42f, 1.15f, true, 0.27f),
                new IdleStyle { breathe = 0.012f, breatheSpeed = 1.6f, jiggle = 0.003f, sway = 1.5f },
                new List<PlacedPart>
                {
                    Place(lotusHat, new Vector3(0f, 0.66f, 0.02f), Vector3.zero, 1f),
                    Place(bloom, new Vector3(0f, 0.04f, 0f), Vector3.zero, 1.1f, false, 0),
                    Place(nose, new Vector3(0f, 0.34f, 0.37f), Vector3.zero, 1.15f),
                    Place(bigEar, new Vector3(0.19f, 0.58f, 0.02f), new Vector3(0f, 0f, -25f), 1f, true),
                    Place(bigTail, new Vector3(0f, -0.36f, -0.36f), new Vector3(15f, 0f, 0f), 1f),
                });
            EditorUtility.SetDirty(lotus);

            var swipe = Attack("PadSwipe", "Pad Swipe", Element.Water, 40, 95, MoveAnimation.Lunge);
            var lotusS = WaterSpecies("Lotusvole", lotus, null, 0, 58, 15, 13, 14, swipe, LoadMove("BodySlam"), LoadMove("WarmNap"));
            var s = WaterSpecies("Lilyvole", vole, lotusS, 16, 40, 11, 9, 13, swipe, LoadMove("Tackle"), LoadMove("WarmNap"));
            AssetDatabase.SaveAssets();
            return s;
        }

        // ================= 4. Sailfold -> Galleyfold =================

        /// <summary>A rounded paper-boat hull (long axis X, the face on its +Z side): a hollowed shell with raised ends.</summary>
        static SdfPrimitive[] Hull(float length, float height, float width)
        {
            return new[]
            {
                Ell(new Vector3(0f, 0f, 0f), new Vector3(length, height, width), 0f),
                Ell(new Vector3(0f, height * 0.85f, 0f), new Vector3(length * 0.86f, height * 0.9f, width * 0.78f), 0.03f).Sub(),
                Cone(new Vector3(length * 0.7f, height * 0.05f, 0f), new Vector3(length * 1.0f, height * 0.7f, 0f), width * 0.5f, width * 0.24f, 0.08f).Mirror(),
                Floor(-height * 0.82f),
            };
        }

        static CreaturePart Sail(string name, float height, float width, PaletteSlot slot)
        {
            var p = Part(name, PartMotion.Wag, Vector3.forward, 4f, 0.5f, Style(slot, 0.3f, 0.02f),
                Cone(Vector3.zero, new Vector3(0f, height, 0f), width, 0.012f, 0f));
            Budget(p, 32, 520);
            return p;
        }

        /// <summary>Sailfold: a chunky paper boat with a blue sail, two tiny feet, rocking as if on waves. Galleyfold: three sails.</summary>
        public static IslingSpecies Sailfold()
        {
            var sail = Sail("PaperSail", 0.42f, 0.2f, PaletteSlot.Extra);
            var foot = PegFoot("BoatFoot", 0.045f, 0.06f, PaletteSlot.Socks);
            var body = Shape("Sailfold_Body", Hull(0.5f, 0.24f, 0.3f));
            var boat = Recipe("Sailfold", body, 0.027f,
                Palette("#F0EBE0", "#E8E2D4", "#D8D0C0", "#E8E0D0", "#D8D0C0", "#88B8D8"),
                Patches((new Vector3(0f, -0.14f, 0.24f), 0.16f, PaletteSlot.Belly)),
                Face(0.04f, 0.2f, 0.02f, 1.1f, false),
                new IdleStyle { breathe = 0.008f, breatheSpeed = 1.4f, hover = 0.004f, bob = 0.004f, sway = 7f, swaySpeed = 0.7f },
                new List<PlacedPart>
                {
                    Place(sail, new Vector3(0f, 0.04f, 0f), Vector3.zero, 1f),
                    Place(foot, new Vector3(0.14f, -0.18f, 0.06f), Vector3.zero, 1f, true),
                });
            boat.spec = 0.15f;
            EditorUtility.SetDirty(boat);

            var mid = Sail("GalleySailMid", 0.36f, 0.17f, PaletteSlot.Glow);
            var aft = Sail("GalleySailAft", 0.26f, 0.14f, PaletteSlot.Accent);
            var bigFoot = PegFoot("GalleyFoot", 0.055f, 0.07f, PaletteSlot.Socks);
            var gBody = Shape("Galleyfold_Body", Hull(0.66f, 0.3f, 0.36f));
            var galley = Recipe("Galleyfold", gBody, 0.038f,
                Palette("#F0EBE0", "#E8E2D4", "#68A0C0", "#E8E0D0", "#78A8C8", "#88B8D8"),
                Patches((new Vector3(0f, -0.18f, 0.3f), 0.2f, PaletteSlot.Belly)),
                Face(0.045f, 0.24f, 0.02f, 1.1f, false),
                new IdleStyle { breathe = 0.008f, breatheSpeed = 1.2f, hover = 0.004f, bob = 0.005f, sway = 6f, swaySpeed = 0.6f },
                new List<PlacedPart>
                {
                    Place(sail, new Vector3(0f, 0.06f, 0f), Vector3.zero, 1.25f),
                    Place(mid, new Vector3(0.26f, 0.05f, 0f), new Vector3(0f, 0f, -6f), 1f),
                    Place(aft, new Vector3(-0.26f, 0.04f, 0f), new Vector3(0f, 0f, 6f), 1f),
                    Place(bigFoot, new Vector3(0.18f, -0.22f, 0.08f), Vector3.zero, 1f, true),
                });
            galley.spec = 0.15f;
            EditorUtility.SetDirty(galley);

            var wake = Attack("WakeRam", "Wake Ram", Element.Water, 45, 90, MoveAnimation.Lunge);
            var galleyS = WaterSpecies("Galleyfold", galley, null, 0, 58, 16, 12, 15, wake, LoadMove("RollOut"), LoadMove("PuffUp"));
            var s = WaterSpecies("Sailfold", boat, galleyS, 16, 40, 12, 9, 14, wake, LoadMove("RollOut"), LoadMove("PuffUp"));
            AssetDatabase.SaveAssets();
            return s;
        }

        // ================= 5. Bottlewhale -> Lighthousewhale (rare) =================

        /// <summary>
        /// Bottlewhale: a sea-glass bottle-shaped whale with a cork that pops, paddling flippers, a fluke; a little amber
        /// ship painted on its side. Lighthousewhale: bigger, a chunky lighthouse on its back with a glowing lamp.
        /// </summary>
        public static IslingSpecies Bottlewhale()
        {
            var cork = Part("BottleCork", PartMotion.Bob, Vector3.up, 0.02f, 0.35f, Style(PaletteSlot.Accent, 0.2f, 0.02f),
                Cone(Vector3.zero, new Vector3(0f, 0.1f, 0f), 0.085f, 0.095f, 0f));
            Budget(cork, 26, 300);
            var flipper = Part("WhaleFlipper", PartMotion.Flap, Vector3.forward, 18f, 0.8f, Style(PaletteSlot.Socks, 0.4f, 0.02f),
                Ell(new Vector3(0.1f, -0.03f, 0f), new Vector3(0.12f, 0.035f, 0.07f), 0f, new Vector3(0f, 0f, -20f)));
            Budget(flipper, 26, 300);
            var fluke = Part("WhaleFluke", PartMotion.Wag, Vector3.right, 14f, 0.7f, Style(PaletteSlot.Socks, 0.4f, 0.02f),
                Cone(Vector3.zero, new Vector3(0f, 0.05f, -0.14f), 0.045f, 0.035f, 0f),
                Ell(new Vector3(0.09f, 0.08f, -0.18f), new Vector3(0.1f, 0.03f, 0.06f), 0.03f, new Vector3(0f, 30f, 15f)),
                Ell(new Vector3(-0.09f, 0.08f, -0.18f), new Vector3(0.1f, 0.03f, 0.06f), 0.03f, new Vector3(0f, -30f, -15f)));
            Budget(fluke, 30, 420);
            var ship = Part("BottleShip", PartMotion.Wag, Vector3.forward, 6f, 0.4f, new PartStyle { color = PaletteSlot.Glow, spec = 0.1f, outline = 0f },
                Ell(Vector3.zero, new Vector3(0.075f, 0.025f, 0.008f), 0f),
                Cone(new Vector3(0f, 0.01f, 0f), new Vector3(0f, 0.085f, 0f), 0.006f, 0.005f, 0f),
                Ell(new Vector3(-0.022f, 0.052f, 0f), new Vector3(0.022f, 0.03f, 0.006f), 0f),
                Ell(new Vector3(0.024f, 0.045f, 0f), new Vector3(0.018f, 0.024f, 0.006f), 0f));
            Budget(ship, 30, 300);
            var body = Shape("Bottlewhale_Body",
                Ell(new Vector3(0f, -0.04f, 0f), new Vector3(0.38f, 0.34f, 0.46f), 0f),
                Cone(new Vector3(0f, 0.2f, -0.04f), new Vector3(0f, 0.4f, -0.04f), 0.2f, 0.11f, 0.1f),
                Floor(-0.36f));
            var whale = Recipe("Bottlewhale", body, 0.042f,
                Palette("#A0D8D0", "#C8ECE6", "#C4A070", "#88C0B8", "#D4A060", "#88C0B8"),
                Patches((new Vector3(0f, -0.12f, 0.34f), 0.24f, PaletteSlot.Belly), (new Vector3(0f, 0.18f, 0.14f), 0.12f, PaletteSlot.Belly)),
                Face(0.042f, 0.15f, 0.06f, 1.15f, true, -0.02f),
                new IdleStyle { breathe = 0.012f, breatheSpeed = 1.3f, hover = 0.005f, bob = 0.004f, sway = 3f },
                new List<PlacedPart>
                {
                    Place(cork, new Vector3(0f, 0.39f, -0.04f), Vector3.zero, 1f),
                    Place(flipper, new Vector3(0.32f, -0.14f, 0.12f), Vector3.zero, 1f, true),
                    Place(fluke, new Vector3(0f, -0.04f, -0.44f), Vector3.zero, 1f),
                    Place(ship, new Vector3(0.372f, -0.03f, -0.08f), new Vector3(0f, 90f, 0f), 1.4f, true),
                });
            whale.spec = 0.6f;
            EditorUtility.SetDirty(whale);

            var tower = Part("LighthouseTower", PartMotion.None, Vector3.up, 0f, 0f, Style(PaletteSlot.Extra, 0.3f, 0.02f),
                Cone(Vector3.zero, new Vector3(0f, 0.26f, 0f), 0.09f, 0.07f, 0f),
                Cone(new Vector3(0f, 0.26f, 0f), new Vector3(0f, 0.29f, 0f), 0.1f, 0.1f, 0.01f),
                Cone(new Vector3(0f, 0.38f, 0f), new Vector3(0f, 0.44f, 0f), 0.08f, 0.02f, 0.03f));
            Budget(tower, 30, 520);
            var lamp = Part("LighthouseLamp", PartMotion.Flicker, Vector3.up, 0.04f, 1.2f, new PartStyle { color = PaletteSlot.Glow, spec = 0.3f, outline = 0.012f, glow = 1.4f },
                Cone(Vector3.zero, new Vector3(0f, 0.09f, 0f), 0.075f, 0.075f, 0f));
            Budget(lamp, 26, 260);
            var bigFlipper = Part("BigWhaleFlipper", PartMotion.Flap, Vector3.forward, 16f, 0.6f, Style(PaletteSlot.Socks, 0.4f, 0.02f),
                Ell(new Vector3(0.13f, -0.04f, 0f), new Vector3(0.16f, 0.045f, 0.09f), 0f, new Vector3(0f, 0f, -20f)));
            Budget(bigFlipper, 26, 320);
            var lBody = Shape("Lighthousewhale_Body",
                Ell(new Vector3(0f, -0.04f, 0f), new Vector3(0.5f, 0.42f, 0.6f), 0f),
                Cone(new Vector3(0f, 0.24f, 0.18f), new Vector3(0f, 0.44f, 0.2f), 0.22f, 0.12f, 0.1f),
                Floor(-0.44f));
            var light = Recipe("Lighthousewhale", lBody, 0.064f,
                Palette("#A0D8D0", "#C8ECE6", "#C4A070", "#88C0B8", "#FFC23A", "#E8D8B0"),
                Patches((new Vector3(0f, -0.16f, 0.42f), 0.3f, PaletteSlot.Belly)),
                Face(0.045f, 0.18f, 0.06f, 1.15f, true, -0.04f),
                new IdleStyle { breathe = 0.01f, breatheSpeed = 1.1f, hover = 0.004f, bob = 0.003f, sway = 2f },
                new List<PlacedPart>
                {
                    Place(cork, new Vector3(0f, 0.43f, 0.2f), Vector3.zero, 1.15f),
                    Place(tower, new Vector3(0f, 0.3f, -0.2f), Vector3.zero, 1f),
                    Place(lamp, new Vector3(0f, 0.29f, 0f), Vector3.zero, 1f, false, 1),
                    Place(bigFlipper, new Vector3(0.42f, -0.18f, 0.16f), Vector3.zero, 1f, true),
                    Place(fluke, new Vector3(0f, -0.04f, -0.58f), Vector3.zero, 1.3f),
                    Place(ship, new Vector3(0.49f, -0.04f, -0.1f), new Vector3(0f, 90f, 0f), 1.8f, true),
                });
            light.spec = 0.6f;
            EditorUtility.SetDirty(light);

            var pop = Attack("CorkPop", "Cork Pop", Element.Water, 50, 90, MoveAnimation.Shoot, true);
            var lightS = WaterSpecies("Lighthousewhale", light, null, 0, 70, 16, 15, 11, pop, LoadMove("BodySlam"), LoadMove("WarmNap"));
            var s = WaterSpecies("Bottlewhale", whale, lightS, 18, 52, 13, 12, 10, pop, LoadMove("BodySlam"), LoadMove("WarmNap"));
            AssetDatabase.SaveAssets();
            return s;
        }
    }
}
