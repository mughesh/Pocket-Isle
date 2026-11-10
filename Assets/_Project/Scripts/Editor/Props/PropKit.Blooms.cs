using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Diorama.EditorTools.StarterKit;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 29 C: the healing blooms, one small glowing plant per realm on the approach to its Keeper's door (1 x 1, never
    /// blocks walking, flat enough not to hide the explorer). Galewick: a dandelion puff on a stem with two leaves.
    /// Cinderwood: an ember-lily, six curled flame petals round a glowing heart. Lilymere: a lotus bud on a pad.
    /// </summary>
    public static partial class PropKit
    {
        public const string BloomGalewick = "HealBloom_Galewick", BloomCinderwood = "HealBloom_Cinderwood", BloomLilymere = "HealBloom_Lilymere";

        public static PropRecipe[] WriteHealBlooms()
        {
            DioramaSceneSetup.EnsureFolder(Root);
            DioramaSceneSetup.EnsureFolder($"{Root}/Shapes");
            return new[] { WriteDandelion(), WriteEmberLily(), WriteLotusBud() };
        }

        static PropRecipe Bloom(string name) {
            var r = Recipe(name, Vector2Int.one, false, false, 2600);
            r.resolution = 48;
            r.outline = 0.02f;
            r.yawJitter = 0f;
            r.scaleJitter = 0f;
            return r;
        }

        static PropRecipe WriteDandelion()
        {
            var r = Bloom(BloomGalewick);
            // Clay stem green, Wood unused, Stone base tuft, Leaf leaves, Glow the puff (white-gold light), Accent seed tips.
            r.palette = Palette(C("#6FA055"), Wood, C("#5C8A47"), C("#7DB35E"), C("#FFF4C8", 1.25f), C("#FFE38A", 0.9f));
            r.pieces = new List<PropPiece>
            {
                Piece(PShape("HealBloom_Galewick_Stem",
                    Cone(new Vector3(0f, 0f, 0f), new Vector3(0.02f, 0.52f, 0f), 0.045f, 0.03f, 0.03f),
                    Floor(0f)), PropSlot.Clay, PropMotion.Sway, 0.03f),
                Piece(PShape("HealBloom_Galewick_Leaves",
                    Ell(new Vector3(-0.16f, 0.1f, 0.02f), new Vector3(0.18f, 0.035f, 0.07f), 0.03f, new Vector3(0f, 10f, 25f)),
                    Ell(new Vector3(0.15f, 0.08f, -0.03f), new Vector3(0.17f, 0.035f, 0.07f), 0.03f, new Vector3(0f, -20f, -22f)),
                    Ell(new Vector3(0.02f, 0.05f, 0.16f), new Vector3(0.07f, 0.03f, 0.15f), 0.03f, new Vector3(18f, 0f, 0f)),
                    Floor(0f)), PropSlot.Leaf, PropMotion.Sway, 0.02f),
                Piece(PShape("HealBloom_Galewick_Puff",
                    Sphere(new Vector3(0.02f, 0.64f, 0f), 0.17f, 0.05f).Bumps(0.025f, 14f)), PropSlot.Glow, PropMotion.Flicker, 0.015f),
                Piece(PShape("HealBloom_Galewick_Seeds",
                    Sphere(new Vector3(0.02f, 0.82f, 0f), 0.035f, 0.02f),
                    Sphere(new Vector3(0.19f, 0.67f, 0.03f), 0.03f, 0.02f),
                    Sphere(new Vector3(-0.15f, 0.69f, -0.02f), 0.03f, 0.02f),
                    Sphere(new Vector3(0.04f, 0.66f, 0.18f), 0.03f, 0.02f),
                    Sphere(new Vector3(0f, 0.66f, -0.17f), 0.03f, 0.02f)), PropSlot.Accent, PropMotion.Flicker, 0.01f),
            };
            EditorUtility.SetDirty(r);
            return r;
        }

        static PropRecipe WriteEmberLily()
        {
            var r = Bloom(BloomCinderwood);
            r.palette = Palette(C("#3E5A33"), Wood, C("#4A3E36"), C("#4E7A3A"), C("#FFB040", 1.35f), C("#FF5A2A", 0.9f));
            var petals = new List<SdfPrimitive>();
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                // A curled tongue: out and up from the heart, tip bending outward.
                petals.Add(Cone(new Vector3(0f, 0.34f, 0f) + dir * 0.04f, new Vector3(0f, 0.5f, 0f) + dir * 0.2f, 0.07f, 0.02f, 0.03f));
            }
            petals.Add(Floor(0.28f));
            r.pieces = new List<PropPiece>
            {
                Piece(PShape("HealBloom_Cinderwood_Stem",
                    Cone(new Vector3(0f, 0f, 0f), new Vector3(0f, 0.34f, 0f), 0.05f, 0.035f, 0.03f),
                    Floor(0f)), PropSlot.Clay, PropMotion.Sway, 0.02f),
                Piece(PShape("HealBloom_Cinderwood_Leaves",
                    Ell(new Vector3(-0.15f, 0.1f, 0f), new Vector3(0.16f, 0.04f, 0.06f), 0.03f, new Vector3(0f, 0f, 35f)),
                    Ell(new Vector3(0.14f, 0.12f, 0.03f), new Vector3(0.15f, 0.04f, 0.06f), 0.03f, new Vector3(0f, 30f, -35f)),
                    Floor(0f)), PropSlot.Leaf, PropMotion.Sway, 0.02f),
                Piece(PShape("HealBloom_Cinderwood_Petals", petals.ToArray()), PropSlot.Accent, PropMotion.Flicker, 0.02f),
                Piece(PShape("HealBloom_Cinderwood_Heart",
                    Sphere(new Vector3(0f, 0.42f, 0f), 0.09f, 0.04f),
                    Cone(new Vector3(0f, 0.42f, 0f), new Vector3(0f, 0.62f, 0f), 0.05f, 0.01f, 0.04f)), PropSlot.Glow, PropMotion.Flicker, 0.02f),
            };
            EditorUtility.SetDirty(r);
            return r;
        }

        static PropRecipe WriteLotusBud()
        {
            var r = Bloom(BloomLilymere);
            r.palette = Palette(C("#4F8A5C"), Wood, C("#3F7A66"), C("#5FA77A"), C("#FFE6F2", 1.2f), C("#F49AC1", 0.6f));
            var petals = new List<SdfPrimitive>();
            for (int i = 0; i < 5; i++)
            {
                float a = (i * 72f + 15f) * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                petals.Add(Ell(new Vector3(0f, 0.36f, 0f) + dir * 0.09f, new Vector3(0.09f, 0.2f, 0.06f), 0.05f, new Vector3(0f, -i * 72f - 15f, 22f)));
            }
            r.pieces = new List<PropPiece>
            {
                Piece(PShape("HealBloom_Lilymere_Pad",
                    Ell(new Vector3(0f, 0.04f, 0f), new Vector3(0.42f, 0.035f, 0.38f), 0.03f),
                    Floor(0f)), PropSlot.Leaf, PropMotion.None),
                Piece(PShape("HealBloom_Lilymere_Stem",
                    Cone(new Vector3(0f, 0.05f, 0f), new Vector3(0f, 0.26f, 0f), 0.05f, 0.04f, 0.03f)), PropSlot.Clay, PropMotion.Sway, 0.015f),
                Piece(PShape("HealBloom_Lilymere_Petals", petals.ToArray()), PropSlot.Accent, PropMotion.Sway, 0.01f),
                Piece(PShape("HealBloom_Lilymere_Glow",
                    Ell(new Vector3(0f, 0.4f, 0f), new Vector3(0.08f, 0.17f, 0.08f), 0.04f)), PropSlot.Glow, PropMotion.Flicker, 0.01f),
            };
            EditorUtility.SetDirty(r);
            return r;
        }
    }
}
