using System.Collections.Generic;
using UnityEngine;
using static Diorama.EditorTools.StarterKit;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 9b forge props. The map gets Bellows (with its press pad) and ForgePipe (one 1 x 1 prop on the door's
    /// east tile; step 9f layout round the door: from the bellows' nozzle 2 tiles east, over the brazier 1 east, up the
    /// door's east side, across over the arch (feeding the brazier there), down the west side to the brazier 3 west). The forge puzzle draws its own pieces: BellowsBody (squashes under
    /// the finger), BrazierWarm (coals glowing, no flame yet), ForgeDoorFrame
    /// (the forge mouth without its inside, a glow panel behind) and ForgeDoorLeaf (the iron-strapped door that sinks
    /// into the ground). Shape units are tiles; shape +X is map west (lower columns), +Z faces the player.
    /// </summary>
    public static partial class PropKit
    {
        /// <summary>The bellows is this many tiles east of the pipe prop's tile (see CinderwoodTools).</summary>
        public const int PipeSpan = 2;
        /// <summary>Step 9f: the run over the arch (tiles), and the raised brazier on it: back toward the cliff, height.</summary>
        public const float PipeOverDoor = 2.95f, TopBrazierBack = 0.38f, TopBrazierHeight = 3f;
        /// <summary>The pipe's climbs beside the door, this far out from the door's outer tiles' centres (tiles).</summary>
        public const float PipeDoorMargin = 0.75f;
        /// <summary>Pipe run height and its offset back toward the cliff (tiles), shared with the forge puzzle's glow pulses.</summary>
        public const float PipeHeight = 0.72f, PipeBack = 0.38f;
        /// <summary>
        /// Top of the bellows' press pad (tiles): poke it down to pump (Mugesh, Oct 26: a poke instead of the first
        /// version's lever). The forge puzzle squashes the whole body with the finger.
        /// </summary>
        public const float BellowsTop = 0.57f;

        static readonly PropColor Leather = C("#8A5A3A");
        static readonly PropColor ForgeIron = C("#3D3633");

        static IEnumerable<PropRecipe> ForgeProps()
        {
            var bodyShapes = BellowsBodyPieces();

            var body = Recipe("BellowsBody", new Vector2Int(1, 1), true, false, 1800);
            body.palette = Palette(ForgeIron, Wood, Stone, Leather, Ember, C("#C0824A", 0.15f));
            body.yawJitter = 0f;
            body.scaleJitter = 0f;
            body.pieces = new List<PropPiece>(bodyShapes);
            yield return body;

            // The map's look: the same bellows (the puzzle draws its own copy that squashes).
            var bellows = Recipe("Bellows", new Vector2Int(1, 1), true, false, 1800);
            bellows.palette = body.palette;
            bellows.yawJitter = 0f;
            bellows.scaleJitter = 0f;
            bellows.pieces = new List<PropPiece>(bodyShapes);
            yield return bellows;

            yield return ForgePipe();
            yield return BrazierWarm();
            yield return ForgeDoorFrame();
            yield return ForgeDoorLeaf();
        }

        /// <summary>Leather bag between two boards on a low stand, an iron nozzle out of its west side.</summary>
        static PropPiece[] BellowsBodyPieces() => new[]
        {
            Piece(PShape("BellowsBody_Boards",
                Box(new Vector3(0f, 0.07f, 0f), new Vector3(0.36f, 0.035f, 0.3f), 0.015f, 0f),
                Box(new Vector3(0f, 0.45f, 0.02f), new Vector3(0.33f, 0.03f, 0.29f), 0.015f, 0f),
                Box(new Vector3(0.3f, 0.02f, 0.24f), new Vector3(0.04f, 0.035f, 0.04f), 0.01f, 0.01f).Mirror(),
                Box(new Vector3(0.3f, 0.02f, -0.24f), new Vector3(0.04f, 0.035f, 0.04f), 0.01f, 0.01f).Mirror(),
                Floor(0f)), PropSlot.Wood),
            Piece(PShape("BellowsBody_Bag",
                Ell(new Vector3(0f, 0.26f, 0f), new Vector3(0.31f, 0.16f, 0.27f), 0f).Bumps(0.012f, 16f),
                Torus(new Vector3(0f, 0.2f, 0f), 0.29f, 0.022f, Vector3.zero, 0.03f),
                Torus(new Vector3(0f, 0.31f, 0f), 0.29f, 0.022f, Vector3.zero, 0.03f)), PropSlot.Leaf),
            Piece(PShape("BellowsBody_Nozzle",
                Cone(new Vector3(0.25f, 0.25f, -0.15f), new Vector3(0.38f, 0.3f, -0.3f), 0.06f, 0.04f, 0f),
                Sphere(new Vector3(0.38f, 0.3f, -0.3f), 0.05f, 0.02f),
                Torus(new Vector3(0f, 0.5f, 0.02f), 0.2f, 0.022f, Vector3.zero, 0f)), PropSlot.Clay),
            // The press pad: a round stuffed leather cushion on the top board, in an iron ring. Poke it.
            Piece(PShape("BellowsBody_Pad",
                Ell(new Vector3(0f, 0.51f, 0.02f), new Vector3(0.19f, 0.06f, 0.19f), 0f).Bumps(0.006f, 20f)), PropSlot.Accent),
        };

        /// <summary>
        /// The iron pipe (this prop on the door's east tile, shape +X = west): rises from the bellows' nozzle 2 tiles east,
        /// runs west at <see cref="PipeHeight"/> and drops into the brazier 1 east, climbs beside the door, runs over the
        /// arch at <see cref="PipeOverDoor"/> with a bracket shelf for the raised brazier 1 west, comes down the far side
        /// and drops into the brazier 3 west. Brackets into the cliff. Walk-through (it hangs at the back of its tiles).
        /// </summary>
        static PropRecipe ForgePipe()
        {
            var pipe = Recipe("ForgePipe", new Vector2Int(1, 1), false, false, 1600);
            pipe.palette = Palette(ForgeIron, Wood, CliffStone, Leaf, Ember, Smoke);
            pipe.yawJitter = 0f;
            pipe.scaleJitter = 0f;
            pipe.resolution = 72;
            pipe.outline = 0.016f;
            float h = PipeHeight, z = -PipeBack, r = 0.04f;
            float east = -(PipeSpan - 0.38f); // the nozzle (shape +X is west)
            float o = PipeOverDoor, near = -PipeDoorMargin, far = 2f + PipeDoorMargin; // this tile is the door's east one
            var prims = new List<SdfPrimitive>
            {
                Cone(new Vector3(east, 0.3f, z), new Vector3(east, h, z), r, r, 0f),
                Sphere(new Vector3(east, h, z), r * 1.15f, 0.01f),
                Cone(new Vector3(east, h, z), new Vector3(near, h, z), r, r, 0.01f),
                Sphere(new Vector3(near, h, z), r * 1.15f, 0.01f),
                Cone(new Vector3(near, h, z), new Vector3(near, o, z), r, r, 0.01f),
                Sphere(new Vector3(near, o, z), r * 1.15f, 0.01f),
                Cone(new Vector3(near, o, z), new Vector3(far, o, z), r, r, 0.01f),
                Sphere(new Vector3(far, o, z), r * 1.15f, 0.01f),
                Cone(new Vector3(far, o, z), new Vector3(far, h, z), r, r, 0.01f),
                Sphere(new Vector3(far, h, z), r * 1.15f, 0.01f),
                Cone(new Vector3(far, h, z), new Vector3(3f, h, z), r, r, 0.01f),
                // The shelf the raised brazier stands on, over the arch.
                Box(new Vector3(1f, o + 0.01f, -TopBrazierBack), new Vector3(0.24f, 0.03f, 0.2f), 0.01f, 0.02f),
            };
            foreach (float x in new[] { -1f, 3f })
            {
                prims.Add(Sphere(new Vector3(x, h, z), r * 1.2f, 0.01f));
                prims.Add(Cone(new Vector3(x, h, z), new Vector3(x, 0.64f, -0.17f), r * 0.9f, r * 0.8f, 0.02f));
            }
            foreach (var (x, y) in new[] { (-1.6f, h), (near, 1.8f), (far, 1.8f), (0.2f, o), (1.8f, o), (far, h) })
                prims.Add(Box(new Vector3(x, y, z - 0.12f), new Vector3(0.04f, 0.06f, 0.12f), 0.01f, 0.015f));
            pipe.pieces = new List<PropPiece> { Piece(PShape("ForgePipe_Iron", prims.ToArray()), PropSlot.Clay) };
            return pipe;
        }

        /// <summary>A brazier warming up: the cold frame with its coals in the glow slot (no flame yet).</summary>
        static PropRecipe BrazierWarm()
        {
            var warm = Recipe("BrazierWarm", new Vector2Int(1, 1), true, true, 1000);
            warm.palette = Palette(Clay, Wood, Iron, Leaf, C("#FF8A3D", 1.2f), C("#4A4442"));
            warm.pieces = new List<PropPiece>
            {
                Piece(PShape("Brazier_Frame", BrazierFrame()), PropSlot.Stone),
                Piece(PShape("Brazier_Coals", Ell(new Vector3(0f, 0.5f, 0f), new Vector3(0.19f, 0.05f, 0.19f)).Bumps(0.015f, 30f)), PropSlot.Glow),
            };
            return warm;
        }

        /// <summary>
        /// The forge mouth (ForgeDoor) without its dark inside: a glow panel behind the arch instead, cold (dark) until the
        /// puzzle warms it (own restoration: 0 cold, 1 hot). The iron bands heat with it.
        /// </summary>
        static PropRecipe ForgeDoorFrame()
        {
            var frame = Recipe("ForgeDoorFrame", new Vector2Int(3, 1), true, false, 2600);
            // Clay = iron bands (cold iron -> hot orange), Stone = cliff (the same either way), Glow = the forge inside.
            frame.palette = Palette(
                new PropColor(Hex("#D8662A"), Hex("#3D3633"), 0.55f), Wood,
                new PropColor(Hex("#6E5A4E"), Hex("#6E5A4E")), CliffGrass,
                new PropColor(Hex("#FF8A30"), Hex("#1E1816"), 1.4f),
                new PropColor(Hex("#FF7A2E"), Hex("#4A4442"), 0.8f));
            frame.yawJitter = 0f;
            frame.scaleJitter = 0f;
            frame.resolution = 56;
            frame.pieces = new List<PropPiece>
            {
                Piece(PShape("ForgeDoor_Stone",
                    Box(new Vector3(0f, 2.5f, -0.05f), new Vector3(1.62f, 2.5f, 0.45f), 0.22f, 0f).Bumps(0.06f, 3.2f),
                    Box(new Vector3(0f, 0.9f, 0.35f), new Vector3(0.82f, 0.9f, 0.55f), 0.04f, 0.02f).Sub(),
                    Ell(new Vector3(0f, 1.8f, 0.35f), new Vector3(0.82f, 0.78f, 0.55f), 0.02f).Sub(),
                    Box(new Vector3(0f, 2.85f, 0.42f), new Vector3(0.18f, 0.2f, 0.06f), 0.03f, 0.03f),
                    Floor(0f)), PropSlot.Stone),
                // Just in front of the stone at the back of the arch (it ends at z -0.2), behind the door leaf.
                Piece(PShape("ForgeDoorFrame_Glow",
                    Box(new Vector3(0f, 1.3f, -0.18f), new Vector3(0.86f, 1.3f, 0.02f), 0.01f, 0f),
                    Floor(0f)), PropSlot.Glow),
                Piece(PShape("ForgeDoor_Iron",
                    Torus(new Vector3(0f, 1.8f, 0.4f), 0.9f, 0.07f, new Vector3(90f, 0f, 0f), 0f),
                    Plane(new Vector3(0f, 1.78f, 0f), Vector3.down, 0f),
                    Cone(new Vector3(0.9f, 0f, 0.4f), new Vector3(0.9f, 1.82f, 0.4f), 0.07f, 0.07f, 0f),
                    Cone(new Vector3(-0.9f, 0f, 0.4f), new Vector3(-0.9f, 1.82f, 0.4f), 0.07f, 0.07f, 0f),
                    Box(new Vector3(0f, 0.04f, 0.42f), new Vector3(1.0f, 0.04f, 0.1f), 0.02f, 0f),
                    Floor(0f)), PropSlot.Clay),
                Piece(PShape("ForgeDoor_Coals",
                    Ell(new Vector3(0f, 0.04f, 0f), new Vector3(0.55f, 0.08f, 0.22f)).Bumps(0.015f, 20f),
                    Floor(0f)), PropSlot.Accent),
            };
            return frame;
        }

        /// <summary>The forge door itself: dark planks filling the arch, three iron straps and a boss. It sinks into the ground to open.</summary>
        static PropRecipe ForgeDoorLeaf()
        {
            var leaf = Recipe("ForgeDoorLeaf", new Vector2Int(3, 1), true, false, 1600);
            leaf.palette = Palette(new PropColor(Hex("#D8662A"), Hex("#3D3633"), 0.45f), C("#2E2420"), Stone, Leaf, Ember, C("#5A4E48"));
            leaf.yawJitter = 0f;
            leaf.scaleJitter = 0f;
            leaf.resolution = 56;
            leaf.pieces = new List<PropPiece>
            {
                Piece(PShape("ForgeDoorLeaf_Planks",
                    Box(new Vector3(0f, 0.9f, -0.08f), new Vector3(0.8f, 0.9f, 0.05f), 0.01f, 0f),
                    Ell(new Vector3(0f, 1.8f, -0.08f), new Vector3(0.8f, 0.76f, 0.05f), 0.02f),
                    Floor(0f)), PropSlot.Wood),
                Piece(PShape("ForgeDoorLeaf_Straps",
                    Box(new Vector3(0f, 0.45f, -0.02f), new Vector3(0.78f, 0.05f, 0.025f), 0.01f, 0f),
                    Box(new Vector3(0f, 1.25f, -0.02f), new Vector3(0.78f, 0.05f, 0.025f), 0.01f, 0f),
                    Box(new Vector3(0f, 2.0f, -0.02f), new Vector3(0.66f, 0.05f, 0.025f), 0.01f, 0f),
                    Torus(new Vector3(0f, 1.6f, -0.01f), 0.16f, 0.03f, new Vector3(90f, 0f, 0f), 0f)), PropSlot.Clay),
            };
            return leaf;
        }
    }
}
