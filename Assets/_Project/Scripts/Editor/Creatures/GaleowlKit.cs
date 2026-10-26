using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Diorama.EditorTools.StarterKit;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 10d (first part): Galeowl, Galewick's Keeper (RealmsDesign.md, concept sheet 02): a big round slate-blue owl
    /// (~2.5x an Isling) with a cream face disc and belly, feather tufts like kite tails, a wind-vane crest, rigid wings
    /// that flap, little talons. The baked palette is the calmed one (soft slate, amber eyes). Species: Wind, Keeper,
    /// shield breaks to Fire; moves Gale Shot (Wind shoot), Talon Dive (Neutral lunge, the every-3rd-turn charge), Gust Wall
    /// (shield). The tower hall, untangling, victory and shard come later (step 10d, part two).
    /// </summary>
    public static class GaleowlKit
    {
        const string Root = "Assets/_Project/Data/Creatures";
        const string MoveRoot = "Assets/_Project/Data/Battle/Moves";

        /// <summary>Lv 9 with no other shard (Lv 11 with one, ShardScaling). HP about 2.75x a common.</summary>
        public const int Level = 9;
        public const int KeeperHp = 120, KeeperAttack = 9, KeeperDefense = 10;

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
            var wing = Part("OwlWing", PartMotion.Flap, Vector3.forward, 10f, 0.5f,
                new PartStyle { color = PaletteSlot.Base, spec = 0.25f, outline = 0.025f, tip = true, tipColor = PaletteSlot.Accent, tipAxis = Vector3.down, tipAt = 0.45f },
                Ell(new Vector3(0.06f, -0.2f, -0.02f), new Vector3(0.1f, 0.34f, 0.24f), 0f, new Vector3(0f, 0f, 8f)),
                Ell(new Vector3(0.08f, -0.46f, -0.06f), new Vector3(0.07f, 0.12f, 0.14f), 0.06f));
            Budget(wing, 30, 480);
            // Feather tufts like kite tails: chunky beads on a short stalk, swaying.
            var tuft = Part("OwlTuft", PartMotion.Wag, Vector3.forward, 14f, 0.7f,
                new PartStyle { color = PaletteSlot.Base, spec = 0.25f, outline = 0.022f, tip = true, tipColor = PaletteSlot.Extra, tipAxis = Vector3.up, tipAt = 0.5f },
                Ell(new Vector3(0f, 0.09f, 0f), new Vector3(0.05f, 0.1f, 0.04f), 0f, new Vector3(0f, 0f, -12f)),
                Ell(new Vector3(0.03f, 0.2f, 0f), new Vector3(0.035f, 0.06f, 0.03f), 0.03f, new Vector3(0f, 0f, -25f)));
            Budget(tuft, 22, 220);
            // The wind-vane crest: an arrow on a post that turns now and then.
            var crest = Part("VaneCrest", PartMotion.Flick, Vector3.up, 40f, 0.25f,
                new PartStyle { color = PaletteSlot.Glow, spec = 0.7f, outline = 0.022f },
                Cone(Vector3.zero, new Vector3(0f, 0.12f, 0f), 0.03f, 0.025f, 0f),
                Cone(new Vector3(0f, 0.15f, -0.18f), new Vector3(0f, 0.15f, 0.14f), 0.022f, 0.022f, 0.02f),
                Cone(new Vector3(0f, 0.15f, 0.14f), new Vector3(0f, 0.15f, 0.26f), 0.065f, 0.008f, 0.02f),
                Box(new Vector3(0f, 0.15f, -0.18f), new Vector3(0.015f, 0.07f, 0.06f), 0.012f, 0.02f));
            Budget(crest, 26, 320);
            var talon = Part("OwlTalon", PartMotion.None, Vector3.up, 0f, 0f,
                new PartStyle { color = PaletteSlot.Socks, spec = 0.3f, outline = 0.02f },
                Ell(new Vector3(0f, 0f, 0.05f), new Vector3(0.09f, 0.04f, 0.1f), 0f),
                Cone(new Vector3(0.05f, -0.01f, 0.12f), new Vector3(0.06f, -0.03f, 0.18f), 0.025f, 0.012f, 0.02f).Mirror(),
                Cone(new Vector3(0f, -0.01f, 0.13f), new Vector3(0f, -0.03f, 0.19f), 0.025f, 0.012f, 0.02f));
            Budget(talon, 22, 220);
            var beak = Part("OwlBeak", PartMotion.None, Vector3.up, 0f, 0f,
                new PartStyle { color = PaletteSlot.Glow, spec = 0.5f, outline = 0.02f },
                Cone(Vector3.zero, new Vector3(0f, -0.08f, 0.06f), 0.05f, 0.012f, 0f));
            Budget(beak, 22, 220);

            var body = Shape("Galeowl_Body",
                Ell(new Vector3(0f, -0.05f, -0.04f), new Vector3(0.56f, 0.58f, 0.5f)),
                Ell(new Vector3(0f, 0.5f, 0.02f), new Vector3(0.48f, 0.4f, 0.42f), 0.2f),
                Ell(new Vector3(0f, 0.48f, 0.36f), new Vector3(0.36f, 0.3f, 0.12f), 0.08f),
                Floor(-0.6f));
            var recipe = Recipe("Galeowl", body, 0.11f,
                new CreaturePalette
                {
                    baseColor = Hex("#5E7590"), belly = Hex("#F2E8D4"), accent = Hex("#465A72"), socks = Hex("#C9A15A"),
                    glow = Hex("#D9A83E"), extra = Hex("#E0644A"), face = Hex("#2A2A30"), iris = Hex("#E0A040"),
                },
                new BodyZones
                {
                    patches = new List<PatternPatch>
                    {
                        new PatternPatch { center = new Vector3(0f, 0.48f, 0.5f), radius = 0.3f, color = PaletteSlot.Belly },
                        new PatternPatch { center = new Vector3(0f, -0.15f, 0.44f), radius = 0.34f, color = PaletteSlot.Belly },
                    },
                },
                new FaceSettings
                {
                    eyeSize = 0.085f, eyeSpacing = 0.17f, eyeHeight = 0.56f, eyeStretch = 1.05f,
                    decals = new List<FaceDecal>(),
                },
                new IdleStyle { breathe = 0.014f, breatheSpeed = 1.3f, sway = 2f, swaySpeed = 0.5f },
                new List<PlacedPart>
                {
                    Place(beak, new Vector3(0f, 0.46f, 0.5f), Vector3.zero, 1f),
                    Place(wing, new Vector3(0.5f, 0.18f, -0.04f), new Vector3(0f, 10f, 10f), 1.25f, true),
                    Place(tuft, new Vector3(0.26f, 0.84f, 0.06f), new Vector3(0f, 0f, -20f), 1.4f, true),
                    Place(tuft, new Vector3(0.18f, -0.1f, -0.5f), new Vector3(-60f, 0f, -20f), 1.2f, true),
                    Place(crest, new Vector3(0f, 0.9f, -0.02f), Vector3.zero, 1.3f),
                    Place(talon, new Vector3(0.2f, -0.6f, 0.2f), new Vector3(0f, 10f, 0f), 1f, true),
                });
            recipe.spec = 0.2f;
            recipe.outline = 0.03f;
            recipe.resolution = 104;
            recipe.triangleBudget = 5600;
            EditorUtility.SetDirty(recipe);

            var shot = Move("GaleShot", "Gale Shot", Element.Wind, 36, 95, MoveAnimation.Shoot);
            var dive = Move("TalonDive", "Talon Dive", Element.Neutral, 48, 90, MoveAnimation.Lunge);
            var wall = Asset<MoveData>($"{MoveRoot}/GustWall.asset");
            wall.displayName = "Gust Wall";
            wall.element = Element.Wind;
            wall.kind = MoveKind.Support;
            wall.power = 0;
            wall.accuracy = 100;
            wall.animation = MoveAnimation.Shield;
            wall.effect = SupportEffect.Shield;
            EditorUtility.SetDirty(wall);

            var species = Species("Galeowl", Element.Wind, recipe, null, 0);
            species.baseStats = new BaseStats { hp = KeeperHp, attack = KeeperAttack, defense = KeeperDefense, speed = 9 };
            species.moves = new[] { shot, dive, wall };
            species.keeper = true;
            species.keeperBreaksWith = Element.Fire;
            EditorUtility.SetDirty(species);
            AssetDatabase.SaveAssets();
            return species;
        }

        static SdfPrimitive Box(Vector3 c, Vector3 half, float round, float k) =>
            new SdfPrimitive { kind = SdfKind.RoundBox, a = c, size = half, b = new Vector3(round, 0f, 0f), k = k };
    }
}
