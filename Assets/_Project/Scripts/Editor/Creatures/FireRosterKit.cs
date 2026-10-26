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
    }
}
