using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Diorama.EditorTools.StarterKit;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 9c: Hearthstag, Cinderwood's Keeper (concept Docs/ConceptArt/08_hearthstag_v2): a barrel-chested russet
    /// stag on very thick stubby legs, cream belly, charcoal hooves, round head, small round ears, a short tail with a
    /// cream tip, and a chunky crown of five short faceted ember crystals (NOT branching antlers). ~2.5x a normal
    /// Isling. The crystals, ears and tail are rigid parts (each crystal its own piece, so its glow can change on its
    /// own: <see cref="KeeperLook"/>); the head stays in the body so the eyes and face marks bake onto it.
    /// The baked palette is the calmed one; KeeperLook paints the frightened one (ash fur, dull red crystals) at runtime.
    /// Species: Fire, Keeper, Lv 12, ~2.5x HP; moves Ember Shot, Crown Charge, Crown Shield.
    /// </summary>
    public static class KeeperKit
    {
        const string Root = "Assets/_Project/Data/Creatures";
        const string MoveRoot = "Assets/_Project/Data/Battle/Moves";
        public const string CrystalPartName = "CrownCrystal";

        // Step 9e balance (KeeperSim): Lv 10, HP ~2.2x a normal Lv 10, a Lv 9 party of 3 wins ~65%.
        public const int Level = 10;
        public const int KeeperHp = 88, KeeperAttack = 8, KeeperDefense = 9;

        public static IslingSpecies Write()
        {
            var roundEar = AssetDatabase.LoadAssetAtPath<CreaturePart>($"{Root}/Parts/RoundEar.asset");
            if (roundEar == null)
            {
                Debug.LogError("[KeeperKit] RoundEar missing. Run Write Starter Kit first.");
                return null;
            }

            // A short faceted crystal: a coarse grid keeps its facets. Glows from the base up, brighter at the tip.
            var crystal = Part(CrystalPartName, PartMotion.None, Vector3.up, 0f, 0f,
                new PartStyle
                {
                    color = PaletteSlot.Glow, glow = 0.6f, spec = 0.9f, outline = 0.018f,
                    tip = true, tipColor = PaletteSlot.Extra, tipAxis = Vector3.up, tipAt = 0.24f,
                },
                Cone(new Vector3(0f, -0.03f, 0f), new Vector3(0f, 0.27f, 0f), 0.085f, 0.06f, 0f),
                Cone(new Vector3(0f, 0.27f, 0f), new Vector3(0f, 0.38f, 0f), 0.06f, 0.008f, 0.01f));
            crystal.resolution = 18;
            crystal.triangleBudget = 320;
            EditorUtility.SetDirty(crystal);

            // Its own small round ear (the starter kit's is finer than a Keeper's budget allows): flicks now and then.
            var ear = Part("StagEar", PartMotion.Flick, Vector3.forward, 18f, 1f,
                new PartStyle
                {
                    color = PaletteSlot.Base, spec = 0.2f, outline = 0.025f,
                    inner = true, innerColor = PaletteSlot.Belly, innerDirection = Vector3.forward, innerAt = 0.55f,
                },
                Ell(new Vector3(0.09f, 0f, 0f), new Vector3(0.11f, 0.08f, 0.04f), 0.03f));
            ear.resolution = 24;
            ear.triangleBudget = 350;
            EditorUtility.SetDirty(ear);

            var tail = Part("StagTail", PartMotion.Wag, Vector3.up, 16f, 1.1f,
                new PartStyle
                {
                    color = PaletteSlot.Base, spec = 0.25f, outline = 0.025f,
                    tip = true, tipColor = PaletteSlot.Belly, tipAxis = Vector3.up, tipAt = 0.1f,
                },
                Ell(new Vector3(0f, 0.08f, -0.02f), new Vector3(0.08f, 0.13f, 0.07f), 0.05f, new Vector3(-20f, 0f, 0f)));
            tail.resolution = 28;
            tail.triangleBudget = 420;
            EditorUtility.SetDirty(tail);

            var body = Shape("Hearthstag_Body",
                Ell(new Vector3(0f, 0.02f, -0.08f), new Vector3(0.5f, 0.42f, 0.64f)),
                Ell(new Vector3(0f, 0.16f, 0.32f), new Vector3(0.45f, 0.44f, 0.36f), 0.18f),
                Cone(new Vector3(0f, 0.36f, 0.4f), new Vector3(0f, 0.72f, 0.56f), 0.25f, 0.21f, 0.12f),
                Ell(new Vector3(0f, 0.88f, 0.6f), new Vector3(0.3f, 0.28f, 0.29f), 0.1f),
                Ell(new Vector3(0f, 0.77f, 0.85f), new Vector3(0.16f, 0.13f, 0.14f), 0.08f),
                Cone(new Vector3(0.27f, -0.12f, 0.3f), new Vector3(0.28f, -0.62f, 0.33f), 0.16f, 0.15f, 0.08f).Mirror(),
                Cone(new Vector3(0.28f, -0.12f, -0.44f), new Vector3(0.29f, -0.62f, -0.46f), 0.16f, 0.15f, 0.08f).Mirror(),
                Floor(-0.66f));

            var recipe = Recipe("Hearthstag", body, 0.11f,
                new CreaturePalette
                {
                    baseColor = Hex("#8B3A1E"), belly = Hex("#F5EDDA"), accent = Hex("#6A2A14"), socks = Hex("#3A3A3A"),
                    glow = Hex("#E8862A"), extra = Hex("#FFB24A"), face = Hex("#4A2818"), iris = Hex("#C07828"),
                },
                new BodyZones
                {
                    socks = true, socksY = -0.5f,
                    patches = new List<PatternPatch>
                    {
                        new PatternPatch { center = new Vector3(0f, 0.08f, 0.66f), radius = 0.3f, color = PaletteSlot.Belly },
                        new PatternPatch { center = new Vector3(0f, -0.36f, 0.02f), radius = 0.24f, color = PaletteSlot.Belly },
                        new PatternPatch { center = new Vector3(0f, 0.76f, 0.97f), radius = 0.12f, color = PaletteSlot.Belly },
                    },
                },
                new FaceSettings
                {
                    eyeSize = 0.062f, eyeSpacing = 0.14f, eyeHeight = 0.93f, eyeStretch = 1.15f,
                    decals = new List<FaceDecal>
                    {
                        new FaceDecal { kind = DecalKind.Disc, at = new Vector2(0f, 0.81f), size = new Vector2(0.06f, 0.04f), color = PaletteSlot.Face },
                        new FaceDecal { kind = DecalKind.Smile, at = new Vector2(0f, 0.72f), size = new Vector2(0.04f, 0f), color = PaletteSlot.Face },
                    },
                },
                new IdleStyle { breathe = 0.012f, breatheSpeed = 1.5f, sway = 1.5f, swaySpeed = 0.6f },
                new List<PlacedPart>
                {
                    Place(crystal, new Vector3(0f, 1.08f, 0.56f), new Vector3(-8f, 0f, 0f), 1.35f),
                    Place(crystal, new Vector3(0.12f, 1.06f, 0.6f), new Vector3(-4f, 0f, -16f), 1.05f, true),
                    Place(crystal, new Vector3(0.23f, 1.01f, 0.52f), new Vector3(0f, 0f, -34f), 1.15f, true),
                    Place(ear, new Vector3(0.27f, 0.95f, 0.55f), new Vector3(0f, 0f, 15f), 1f, true),
                    Place(tail, new Vector3(0f, 0.32f, -0.66f), new Vector3(-35f, 0f, 0f), 1.2f),
                });
            recipe.spec = 0.18f;
            recipe.outline = 0.03f;
            recipe.resolution = 104;
            recipe.triangleBudget = 10000;
            EditorUtility.SetDirty(recipe);

            var emberShot = Move("EmberShot", "Ember Shot", Element.Fire, 36, 95, MoveAnimation.Shoot);
            var crownCharge = Move("CrownCharge", "Crown Charge", Element.Fire, 48, 90, MoveAnimation.Lunge);
            var crownShield = Asset<MoveData>($"{MoveRoot}/CrownShield.asset");
            crownShield.displayName = "Crown Shield";
            crownShield.element = Element.Fire;
            crownShield.kind = MoveKind.Support;
            crownShield.power = 0;
            crownShield.accuracy = 100;
            crownShield.animation = MoveAnimation.Shield;
            crownShield.effect = SupportEffect.Shield;
            EditorUtility.SetDirty(crownShield);

            var species = Species("Hearthstag", Element.Fire, recipe, null, 0);
            // HP ~2.5x a normal Lv 12 Isling (their base HP is ~40-58).
            species.baseStats = new BaseStats { hp = KeeperHp, attack = KeeperAttack, defense = KeeperDefense, speed = 8 };
            species.moves = new[] { emberShot, crownCharge, crownShield };
            species.keeper = true;
            EditorUtility.SetDirty(species);
            AssetDatabase.SaveAssets();
            return species;
        }

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
    }
}
