using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Diorama.EditorTools.StarterKit;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 11c: Tidemother, Lilymere's Keeper (Concepts_01_Summary.md, sheet 02): an enormous cozy sea turtle (~2.5x an
    /// Isling), seafoam-green skin, a jade dome shell with mini lily pads and a pink lotus on top, warm eyes, stubby
    /// flippers that paddle. The baked palette is the calmed one; frightened colours come from her
    /// <see cref="KeeperHall"/> (grey-teal, red-teal eyes, a churning aura). Species: Water, Keeper, the Lily Shell
    /// breaks to Wind; moves Tide Shot (Water shoot), Shell Slam (Neutral slam, the every-3rd-turn charge), Lily Shell
    /// (shield).
    /// </summary>
    public static class TidemotherKit
    {
        const string MoveRoot = "Assets/_Project/Data/Battle/Moves";

        /// <summary>Lv 9 with no other shard (+2 per other shard, ShardScaling). HP about 2.75x a common.</summary>
        public const int Level = 9;
        public const int KeeperHp = 132, KeeperAttack = 10, KeeperDefense = 11;

        static MoveData Move(string file, string name, Element element, int power, int accuracy, MoveAnimation animation)
        {
            var m = Asset<MoveData>($"{MoveRoot}/{file}.asset");
            m.displayName = name;
            m.element = element;
            m.kind = MoveKind.Attack;
            m.power = power;
            m.accuracy = accuracy;
            m.animation = animation;
            m.arc = false;
            m.effect = SupportEffect.None;
            EditorUtility.SetDirty(m);
            return m;
        }

        static void Budget(CreaturePart p, int res, int tris)
        {
            p.resolution = res;
            p.triangleBudget = tris;
            EditorUtility.SetDirty(p);
        }

        public static IslingSpecies Write()
        {
            var flipper = Part("TurtleFlipper", PartMotion.Flap, Vector3.forward, 12f, 0.45f,
                new PartStyle { color = PaletteSlot.Base, spec = 0.3f, outline = 0.022f, tip = true, tipColor = PaletteSlot.Socks, tipAxis = Vector3.right, tipAt = 0.2f },
                Ell(new Vector3(0.16f, -0.03f, 0.04f), new Vector3(0.2f, 0.06f, 0.12f), 0f, new Vector3(0f, -20f, -12f)));
            Budget(flipper, 28, 420);
            var backFlipper = Part("TurtleBackFlipper", PartMotion.Flap, Vector3.forward, 8f, 0.4f, Style(PaletteSlot.Base),
                Ell(new Vector3(0.1f, -0.03f, -0.04f), new Vector3(0.13f, 0.05f, 0.09f), 0f, new Vector3(0f, 25f, -10f)));
            Budget(backFlipper, 26, 320);
            var pad = Part("ShellLilyPad", PartMotion.Wag, Vector3.forward, 4f, 0.4f, Style(PaletteSlot.Extra, 0.3f, 0.018f),
                Ell(Vector3.zero, new Vector3(0.13f, 0.018f, 0.12f), 0f),
                Box(new Vector3(0.09f, 0f, 0.02f), new Vector3(0.06f, 0.04f, 0.012f), 0f, 0.005f, new Vector3(0f, -20f, 0f)).Sub());
            Budget(pad, 28, 360);
            var lotusPrims = new List<SdfPrimitive> { Sphere(new Vector3(0f, 0.04f, 0f), 0.045f, 0f) };
            for (int i = 0; i < 6; i++)
            {
                var d = Quaternion.Euler(0f, i * 60f, 0f) * Vector3.forward;
                lotusPrims.Add(Ell(d * 0.07f + Vector3.up * 0.06f, new Vector3(0.04f, 0.085f, 0.025f), 0f, new Vector3(35f, i * 60f, 0f)));
            }
            var lotus = Part("ShellLotus", PartMotion.Bob, Vector3.up, 0.01f, 0.35f,
                new PartStyle { color = PaletteSlot.Accent, spec = 0.4f, outline = 0.015f, tip = true, tipColor = PaletteSlot.Belly, tipAxis = Vector3.up, tipAt = 0.1f },
                lotusPrims.ToArray());
            Budget(lotus, 32, 800);
            var tail = Part("TurtleTail", PartMotion.Wag, Vector3.up, 10f, 0.5f, Style(PaletteSlot.Base),
                Cone(Vector3.zero, new Vector3(0f, -0.02f, -0.14f), 0.05f, 0.015f, 0f));
            Budget(tail, 22, 200);

            var body = Shape("Tidemother_Body",
                // The jade dome shell (painted Accent-free: the shell zone is a patch on top), the plastron beneath.
                Ell(new Vector3(0f, 0.05f, -0.06f), new Vector3(0.62f, 0.42f, 0.62f), 0f),
                Ell(new Vector3(0f, -0.2f, -0.04f), new Vector3(0.58f, 0.16f, 0.56f), 0.1f),
                // The head and neck, low at the front.
                Ell(new Vector3(0f, -0.04f, 0.56f), new Vector3(0.17f, 0.14f, 0.2f), 0.14f),
                Sphere(new Vector3(0f, 0.06f, 0.72f), 0.22f, 0.1f),
                Floor(-0.34f));
            var recipe = Recipe("Tidemother", body, 0.1f,
                new CreaturePalette
                {
                    baseColor = Hex("#8FD6B8"), belly = Hex("#F2EAD0"), accent = Hex("#F2A7B8"), socks = Hex("#5FA88C"),
                    glow = Hex("#3A8466"), extra = Hex("#5AA868"), face = Hex("#2A3030"), iris = Hex("#7A4E2E"),
                },
                new BodyZones
                {
                    patches = new List<PatternPatch>
                    {
                        // The shell: a big jade cap over the top, a lighter rim band low on it, the cream plastron.
                        new PatternPatch { center = new Vector3(0f, 0.62f, -0.06f), radius = 0.78f, color = PaletteSlot.Glow },
                        new PatternPatch { center = new Vector3(0f, -0.28f, 0.12f), radius = 0.42f, color = PaletteSlot.Belly },
                        new PatternPatch { center = new Vector3(0f, -0.02f, 0.84f), radius = 0.12f, color = PaletteSlot.Belly },
                    },
                },
                new FaceSettings
                {
                    eyeSize = 0.07f, eyeSpacing = 0.15f, eyeHeight = 0.12f, eyeStretch = 1.05f,
                    decals = new List<FaceDecal> { new FaceDecal { kind = DecalKind.Smile, at = new Vector2(0f, 0.0f), size = new Vector2(0.04f, 0f), color = PaletteSlot.Face } },
                },
                new IdleStyle { breathe = 0.012f, breatheSpeed = 0.9f, sway = 1.5f, swaySpeed = 0.4f },
                new List<PlacedPart>
                {
                    Place(flipper, new Vector3(0.5f, -0.18f, 0.32f), new Vector3(0f, 0f, -8f), 1.3f, true),
                    Place(backFlipper, new Vector3(0.46f, -0.2f, -0.46f), new Vector3(0f, 0f, -8f), 1.2f, true),
                    Place(pad, new Vector3(-0.2f, 0.47f, -0.18f), new Vector3(-12f, 20f, 8f), 1.1f),
                    Place(pad, new Vector3(0.26f, 0.44f, -0.26f), new Vector3(-8f, -70f, -10f), 0.95f),
                    Place(pad, new Vector3(0.02f, 0.49f, 0.04f), new Vector3(-14f, 120f, 0f), 1.2f),
                    Place(lotus, new Vector3(0.02f, 0.02f, 0f), Vector3.zero, 1.4f, false, 4),
                    Place(tail, new Vector3(0f, -0.14f, -0.66f), Vector3.zero, 1f),
                });
            recipe.spec = 0.3f;
            recipe.outline = 0.03f;
            recipe.resolution = 104;
            recipe.triangleBudget = 5600;
            EditorUtility.SetDirty(recipe);

            var shot = Move("TideShot", "Tide Shot", Element.Water, 36, 95, MoveAnimation.Shoot);
            var slam = Move("ShellSlam", "Shell Slam", Element.Neutral, 48, 90, MoveAnimation.Slam);
            var shell = Asset<MoveData>($"{MoveRoot}/LilyShell.asset");
            shell.displayName = "Lily Shell";
            shell.element = Element.Water;
            shell.kind = MoveKind.Support;
            shell.power = 0;
            shell.accuracy = 100;
            shell.animation = MoveAnimation.Shield;
            shell.effect = SupportEffect.Shield;
            EditorUtility.SetDirty(shell);

            var species = Species("Tidemother", Element.Water, recipe, null, 0);
            species.baseStats = new BaseStats { hp = KeeperHp, attack = KeeperAttack, defense = KeeperDefense, speed = 7 };
            species.moves = new[] { shot, slam, shell };
            species.keeper = true;
            species.keeperBreaksWith = Element.Wind;
            EditorUtility.SetDirty(species);
            AssetDatabase.SaveAssets();
            return species;
        }

        static PartStyle Style(PaletteSlot slot, float spec = 0.25f, float outline = 0.022f) => new PartStyle { color = slot, spec = spec, outline = outline };

        static SdfPrimitive Box(Vector3 c, Vector3 half, float round, float k, Vector3 rotation = default) =>
            new SdfPrimitive { kind = SdfKind.RoundBox, a = c, size = half, b = new Vector3(round, 0f, 0f), rotation = rotation, k = k };
    }
}
