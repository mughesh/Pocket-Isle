using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Diorama.EditorTools.StarterKit;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Nov 3 (Mugesh: Cinderwood's arrival was bland): a thorny vine grown across the trail past the arrival, 1 x 1, blocks
    /// walking. Two woody hoops arching over the tile and a tangle of stems between them, hooked thorns along the stems,
    /// dark leaves. The arrival's ember burns it away (VineBurnPiece).
    /// </summary>
    public static partial class PropKit
    {
        public const string VineName = "ThornVine";

        /// <summary>Writes the vine's recipe (bake it with PropBaker).</summary>
        public static PropRecipe WriteThornVine()
        {
            DioramaSceneSetup.EnsureFolder(Root);
            DioramaSceneSetup.EnsureFolder($"{Root}/Shapes");
            var vine = Recipe(VineName, Vector2Int.one, true, false, 3400);
            // Clay stems (dark red-brown), Wood unused, Stone roots, Leaf dark green, Glow embers (unused), Accent pale thorns.
            vine.palette = Palette(C("#5B3A2C"), Wood, C("#4A3E36"), C("#3F5E33"), C("#FF7A2E", 1.1f), C("#D8C7A6"));
            vine.resolution = 48;
            vine.outline = 0.02f;
            vine.yawJitter = 8f;
            vine.scaleJitter = 0.08f;
            vine.pieces = new List<PropPiece>
            {
                Piece(PShape("ThornVine_Stems",
                    // Two hoops arching across the tile (x), a little apart in depth.
                    Torus(new Vector3(0f, 0.12f, 0.1f), 0.42f, 0.055f, new Vector3(90f, 0f, 0f), 0.04f),
                    Torus(new Vector3(0.06f, 0.08f, -0.12f), 0.36f, 0.05f, new Vector3(90f, 18f, 0f), 0.04f),
                    // The tangle between them.
                    Cone(new Vector3(-0.5f, 0.06f, 0.02f), new Vector3(-0.1f, 0.5f, -0.05f), 0.06f, 0.035f, 0.05f),
                    Cone(new Vector3(-0.1f, 0.5f, -0.05f), new Vector3(0.48f, 0.18f, 0.08f), 0.04f, 0.03f, 0.05f),
                    Cone(new Vector3(0.5f, 0.05f, -0.04f), new Vector3(0.05f, 0.34f, 0.12f), 0.055f, 0.03f, 0.05f),
                    Cone(new Vector3(0.05f, 0.34f, 0.12f), new Vector3(-0.42f, 0.26f, -0.1f), 0.035f, 0.025f, 0.05f),
                    // Thorns: short hooked cones off the hoops' tops and the tangle.
                    Cone(new Vector3(-0.2f, 0.55f, 0.1f), new Vector3(-0.26f, 0.68f, 0.12f), 0.035f, 0.004f, 0.01f),
                    Cone(new Vector3(0.18f, 0.55f, 0.1f), new Vector3(0.26f, 0.66f, 0.06f), 0.035f, 0.004f, 0.01f),
                    Cone(new Vector3(0f, 0.54f, 0.1f), new Vector3(0.02f, 0.68f, 0.18f), 0.035f, 0.004f, 0.01f),
                    Cone(new Vector3(-0.3f, 0.4f, -0.12f), new Vector3(-0.42f, 0.48f, -0.16f), 0.032f, 0.004f, 0.01f),
                    Cone(new Vector3(0.3f, 0.36f, -0.12f), new Vector3(0.4f, 0.48f, -0.18f), 0.032f, 0.004f, 0.01f),
                    Cone(new Vector3(0.24f, 0.26f, 0.05f), new Vector3(0.3f, 0.36f, 0.16f), 0.03f, 0.004f, 0.01f),
                    Floor(0f)), PropSlot.Clay, PropMotion.Sway, 0.02f),
                // A low bramble under the arch: no gap that reads as a way through.
                Piece(PShape("ThornVine_Bramble",
                    Ell(new Vector3(0.02f, 0.14f, 0f), new Vector3(0.34f, 0.16f, 0.22f), 0.06f).Bumps(0.03f, 9f),
                    Sphere(new Vector3(-0.16f, 0.26f, 0.06f), 0.12f, 0.06f),
                    Sphere(new Vector3(0.18f, 0.24f, -0.04f), 0.11f, 0.06f),
                    Floor(0f)), PropSlot.Leaf, PropMotion.Sway, 0.02f),
                Piece(PShape("ThornVine_Roots",
                    Ell(new Vector3(-0.42f, 0.04f, 0.05f), new Vector3(0.14f, 0.07f, 0.12f), 0.04f),
                    Ell(new Vector3(0.44f, 0.04f, -0.06f), new Vector3(0.14f, 0.07f, 0.12f), 0.04f),
                    Floor(0f)), PropSlot.Stone),
                Piece(PShape("ThornVine_Leaves",
                    Ell(new Vector3(-0.3f, 0.48f, 0.14f), new Vector3(0.11f, 0.04f, 0.08f), 0.03f, new Vector3(0f, 30f, 20f)),
                    Ell(new Vector3(0.32f, 0.44f, 0.12f), new Vector3(0.1f, 0.04f, 0.07f), 0.03f, new Vector3(0f, -25f, -20f)),
                    Ell(new Vector3(0.1f, 0.42f, -0.16f), new Vector3(0.1f, 0.04f, 0.07f), 0.03f, new Vector3(0f, 60f, 15f)),
                    Ell(new Vector3(-0.14f, 0.3f, -0.12f), new Vector3(0.09f, 0.035f, 0.07f), 0.03f, new Vector3(0f, -40f, -25f)),
                    Ell(new Vector3(-0.46f, 0.18f, 0f), new Vector3(0.09f, 0.035f, 0.06f), 0.03f, new Vector3(0f, 10f, 35f)),
                    Ell(new Vector3(0.46f, 0.2f, 0.02f), new Vector3(0.09f, 0.035f, 0.06f), 0.03f, new Vector3(0f, -10f, -35f))), PropSlot.Leaf, PropMotion.Sway, 0.04f),
            };
            EditorUtility.SetDirty(vine);
            return vine;
        }
    }
}
