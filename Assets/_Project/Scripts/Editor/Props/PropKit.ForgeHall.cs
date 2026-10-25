using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Diorama.EditorTools.StarterKit;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 9b Forge Hall props (interior, placeholder until 9c): stone wall segment, pillar, the big cold forge (2 x 2),
    /// anvil, chain posts, dim ember crystals and the front door arch (2 x 1, walk-through: stepping on it leads out).
    /// </summary>
    public static partial class PropKit
    {
        static readonly PropColor HallStone = C("#6A605A");
        static readonly PropColor HallDark = C("#2E2826");

        /// <summary>Writes the Forge Hall props. Returns them by name (not baked).</summary>
        public static Dictionary<string, PropRecipe> WriteForgeHallProps()
        {
            DioramaSceneSetup.EnsureFolder(Root);
            DioramaSceneSetup.EnsureFolder($"{Root}/Shapes");
            var ember = C("#FF6A2E", 0.35f);
            var all = new Dictionary<string, PropRecipe>();
            void Add(PropRecipe r)
            {
                r.yawJitter = 0f;
                r.scaleJitter = 0f;
                all[r.name] = r;
                EditorUtility.SetDirty(r);
            }

            var wall = Recipe("HallWall", new Vector2Int(1, 1), true, false, 700);
            wall.palette = Palette(ForgeIron, Wood, HallStone, Leaf, ember, HallDark);
            wall.pieces = new List<PropPiece>
            {
                Piece(PShape("HallWall_Stone",
                    Box(new Vector3(0f, 0.8f, 0f), new Vector3(0.5f, 0.8f, 0.16f), 0.03f, 0f).Bumps(0.025f, 6f),
                    Box(new Vector3(0f, 1.63f, 0f), new Vector3(0.52f, 0.05f, 0.2f), 0.02f, 0f),
                    Floor(0f)), PropSlot.Stone),
            };
            Add(wall);

            var pillar = Recipe("HallPillar", new Vector2Int(1, 1), true, false, 800);
            pillar.palette = Palette(ForgeIron, Wood, HallStone, Leaf, ember, HallDark);
            pillar.pieces = new List<PropPiece>
            {
                Piece(PShape("HallPillar_Stone",
                    Box(new Vector3(0f, 0.07f, 0f), new Vector3(0.3f, 0.07f, 0.3f), 0.02f, 0f),
                    Cone(new Vector3(0f, 0.1f, 0f), new Vector3(0f, 1.9f, 0f), 0.2f, 0.18f, 0.03f).Bumps(0.012f, 9f),
                    Box(new Vector3(0f, 1.95f, 0f), new Vector3(0.28f, 0.06f, 0.28f), 0.02f, 0.02f),
                    Floor(0f)), PropSlot.Stone),
                Piece(PShape("HallPillar_Band",
                    Torus(new Vector3(0f, 0.55f, 0f), 0.2f, 0.03f, Vector3.zero, 0f)), PropSlot.Clay),
            };
            Add(pillar);

            var forge = Recipe("HallForge", new Vector2Int(2, 2), true, false, 2600);
            forge.palette = Palette(ForgeIron, Wood, HallStone, Leaf, ember, C("#4A4442"));
            forge.resolution = 56;
            forge.pieces = new List<PropPiece>
            {
                Piece(PShape("HallForge_Hearth",
                    Box(new Vector3(0f, 0.35f, 0.05f), new Vector3(0.95f, 0.35f, 0.75f), 0.05f, 0f).Bumps(0.02f, 5f),
                    Box(new Vector3(0f, 0.42f, 0.7f), new Vector3(0.6f, 0.24f, 0.4f), 0.04f, 0.02f).Sub(),
                    Cone(new Vector3(0f, 0.68f, -0.1f), new Vector3(0f, 1.6f, -0.25f), 0.85f, 0.3f, 0.06f),
                    Cone(new Vector3(0f, 1.55f, -0.25f), new Vector3(0f, 2.7f, -0.3f), 0.28f, 0.24f, 0.04f),
                    Floor(0f)), PropSlot.Stone),
                Piece(PShape("HallForge_Bands",
                    Torus(new Vector3(0f, 1.0f, -0.17f), 0.66f, 0.035f, Vector3.zero, 0f),
                    Torus(new Vector3(0f, 2.2f, -0.28f), 0.27f, 0.03f, Vector3.zero, 0f)), PropSlot.Clay),
                Piece(PShape("HallForge_Coals",
                    Ell(new Vector3(0f, 0.22f, 0.55f), new Vector3(0.52f, 0.07f, 0.3f)).Bumps(0.015f, 20f)), PropSlot.Accent),
            };
            Add(forge);

            var anvil = Recipe("Anvil", new Vector2Int(1, 1), true, false, 900);
            anvil.palette = Palette(ForgeIron, Wood, HallStone, Leaf, ember, HallDark);
            anvil.pieces = new List<PropPiece>
            {
                Piece(PShape("Anvil_Stump",
                    Cone(new Vector3(0f, 0f, 0f), new Vector3(0f, 0.28f, 0f), 0.22f, 0.2f, 0f),
                    Floor(0f)), PropSlot.Wood),
                Piece(PShape("Anvil_Iron",
                    Box(new Vector3(0f, 0.33f, 0f), new Vector3(0.12f, 0.05f, 0.08f), 0.02f, 0f),
                    Box(new Vector3(0f, 0.43f, 0f), new Vector3(0.26f, 0.06f, 0.12f), 0.02f, 0.03f),
                    Cone(new Vector3(0.24f, 0.45f, 0f), new Vector3(0.45f, 0.46f, 0f), 0.07f, 0.012f, 0.03f)), PropSlot.Clay),
            };
            Add(anvil);

            var chains = Recipe("HallChains", new Vector2Int(1, 1), true, false, 1100);
            chains.palette = Palette(ForgeIron, Wood, HallStone, Leaf, ember, HallDark);
            chains.resolution = 64;
            chains.outline = 0.014f;
            var links = new List<SdfPrimitive>
            {
                Cone(new Vector3(-0.32f, 0f, 0f), new Vector3(-0.32f, 0.9f, 0f), 0.05f, 0.04f, 0f),
                Cone(new Vector3(0.32f, 0f, 0f), new Vector3(0.32f, 0.9f, 0f), 0.05f, 0.04f, 0f),
                Floor(0f),
            };
            for (int i = 0; i < 7; i++)
            {
                float u = (i + 0.5f) / 7f;
                float x = Mathf.Lerp(-0.28f, 0.28f, u), y = 0.86f - 0.28f * Mathf.Sin(u * Mathf.PI);
                links.Add(Torus(new Vector3(x, y, 0f), 0.045f, 0.013f, i % 2 == 0 ? new Vector3(90f, 0f, 0f) : new Vector3(0f, 0f, 90f), 0.004f));
            }
            chains.pieces = new List<PropPiece> { Piece(PShape("HallChains_Iron", links.ToArray()), PropSlot.Clay) };
            Add(chains);

            var crystal = Recipe("EmberCrystal", new Vector2Int(1, 1), true, true, 700);
            crystal.palette = Palette(ForgeIron, Wood, HallStone, Leaf, ember, HallDark);
            crystal.pieces = new List<PropPiece>
            {
                Piece(PShape("EmberCrystal_Rock",
                    Ell(new Vector3(0f, 0.05f, 0f), new Vector3(0.28f, 0.1f, 0.24f)).Bumps(0.02f, 9f),
                    Floor(0f)), PropSlot.Stone),
                Piece(PShape("EmberCrystal_Shards",
                    Ell(new Vector3(0f, 0.3f, 0f), new Vector3(0.07f, 0.26f, 0.07f), 0f, new Vector3(0f, 0f, 8f)),
                    Ell(new Vector3(0.12f, 0.2f, 0.05f), new Vector3(0.05f, 0.18f, 0.05f), 0.01f, new Vector3(10f, 0f, -28f)),
                    Ell(new Vector3(-0.11f, 0.18f, -0.04f), new Vector3(0.05f, 0.16f, 0.05f), 0.01f, new Vector3(-12f, 0f, 30f)),
                    Ell(new Vector3(0.02f, 0.14f, 0.13f), new Vector3(0.04f, 0.12f, 0.04f), 0.01f, new Vector3(30f, 0f, 0f))), PropSlot.Glow, PropMotion.Flicker, 0.001f),
            };
            Add(crystal);

            var door = Recipe("HallDoor", new Vector2Int(2, 1), false, false, 1200);
            door.palette = Palette(ForgeIron, Wood, HallStone, Leaf, ember, HallDark);
            door.pieces = new List<PropPiece>
            {
                // In order: the arch ring, cut to its top half, then the posts (a cut after them would cut them too).
                Piece(PShape("HallDoor_Arch",
                    Torus(new Vector3(0f, 1.5f, 0.3f), 0.95f, 0.13f, new Vector3(90f, 0f, 0f), 0f),
                    Plane(new Vector3(0f, 1.48f, 0f), Vector3.down, 0f),
                    Box(new Vector3(0.95f, 0.76f, 0.3f), new Vector3(0.14f, 0.76f, 0.14f), 0.03f, 0.05f).Mirror(),
                    Floor(0f)), PropSlot.Stone),
            };
            Add(door);
            return all;
        }
    }
}
