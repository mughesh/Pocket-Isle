using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Diorama.EditorTools.StarterKit;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 9a Cinderwood props: clay hut (2 x 2), fence (straight + corner; turn them with the legend's yaw), signpost,
    /// woodpile, leaf pile, cliff wall (its second look = the same segment turned 180), forge door (3 x 1, cold),
    /// ruined kiln, pot shards, ember vent; and the tree fix: AutumnTree rebuilt as stacked blobs (red-orange at the
    /// base to gold at the top, no overhanging cap) plus a tall narrow TallTree, mixed in through the theme.
    /// Shape units are tiles, +Z faces the player, origin = middle of the footprint on the ground.
    /// </summary>
    public static partial class PropKit
    {
        static readonly PropColor HutClay = C("#C98A5A");
        static readonly PropColor Thatch = C("#A86A32");
        static readonly PropColor FenceWood = C("#7A5236");
        static readonly PropColor LightWood = C("#B98A55");
        static readonly PropColor CliffStone = C("#6E5A4E");
        static readonly PropColor CliffGrass = C("#B5652E");
        static readonly PropColor CanopyRed = C("#B8401F");
        static readonly PropColor CanopyGold = C("#F2B03A", 0.5f);

        /// <summary>Writes the 9a props and points the Cinderwood theme at both trees. Returns them by name (not baked).</summary>
        public static Dictionary<string, PropRecipe> WriteCinderwoodProps()
        {
            DioramaSceneSetup.EnsureFolder(Root);
            DioramaSceneSetup.EnsureFolder($"{Root}/Shapes");
            var all = new Dictionary<string, PropRecipe>();
            void Add(PropRecipe r) => all[r.name] = r;

            Add(AutumnTree());
            Add(TallTree());
            Add(ClayHut());
            Add(Fence());
            Add(FenceCorner());
            Add(Signpost());
            Add(Woodpile());
            Add(LeafPile());
            Add(CliffWall());
            Add(ForgeDoor());
            Add(RuinedKiln());
            Add(PotShards());
            Add(EmberVent());
            foreach (var r in ForgeProps()) Add(r); // step 9b: bellows, pipe, warm brazier, forge door frame + leaf
            foreach (var r in WriteCinderwoodMomentProps().Values) Add(r); // step 10f: cold shrine, wisp, hollow smoke

            var theme = Theme("Cinderwood");
            if (theme == null) Debug.LogError("[PropKit] No Cinderwood theme (write the prop kit first): the trees aren't mixed.");
            else
            {
                theme.treeProp = all["AutumnTree"];
                theme.treePropAlt = all["TallTree"];
                theme.treeAltShare = 0.35f;
                EditorUtility.SetDirty(theme);
            }
            foreach (var r in all.Values) EditorUtility.SetDirty(r);
            return all;
        }

        // ---- trees ----

        static PropTips CanopyTips => new PropTips { on = true, onSlot = PropSlot.Leaf, tipSlot = PropSlot.Glow, at = 0.55f, byHeight = 0.85f };

        /// <summary>Round autumn tree: 2-3 stacked, offset blobs, widest in the middle, trunk showing below.</summary>
        static PropRecipe AutumnTree()
        {
            var tree = Recipe("AutumnTree", new Vector2Int(1, 1), true, true, 700);
            tree.palette = Palette(Clay, C("#4A3328"), Stone, CanopyRed, CanopyGold, Smoke);
            tree.tips = CanopyTips;
            tree.scaleJitter = 0.12f;
            tree.pieces = new List<PropPiece>
            {
                Piece(PShape("AutumnTree_Trunk",
                    Cone(new Vector3(0f, 0f, 0f), new Vector3(0f, 1.12f, 0f), 0.085f, 0.05f, 0f),
                    Ell(new Vector3(0f, 0.03f, 0f), new Vector3(0.14f, 0.06f, 0.14f), 0.08f),
                    Cone(new Vector3(0f, 0.72f, 0f), new Vector3(0.18f, 0.98f, 0.05f), 0.035f, 0.025f, 0.05f),
                    Floor(0f)), PropSlot.Wood),
                Piece(PShape("AutumnTree_Canopy",
                    Sphere(new Vector3(0.04f, 1.13f, 0.03f), 0.25f).Bumps(0.022f, 11f),
                    Ell(new Vector3(-0.04f, 1.43f, -0.03f), new Vector3(0.42f, 0.36f, 0.4f), 0.14f).Bumps(0.022f, 11f),
                    Sphere(new Vector3(0.24f, 1.36f, 0.06f), 0.24f, 0.14f).Bumps(0.022f, 11f),
                    Sphere(new Vector3(0.03f, 1.8f, 0f), 0.26f, 0.14f).Bumps(0.022f, 11f)), PropSlot.Leaf, PropMotion.Sway, 0.035f),
            };
            return tree;
        }

        /// <summary>Tall narrow tree (poplar-like): four stacked slim blobs.</summary>
        static PropRecipe TallTree()
        {
            var tree = Recipe("TallTree", new Vector2Int(1, 1), true, true, 700);
            tree.palette = Palette(Clay, C("#4A3328"), Stone, CanopyRed, CanopyGold, Smoke);
            tree.tips = CanopyTips;
            tree.scaleJitter = 0.1f;
            tree.pieces = new List<PropPiece>
            {
                Piece(PShape("TallTree_Trunk",
                    Cone(new Vector3(0f, 0f, 0f), new Vector3(0f, 1.15f, 0f), 0.075f, 0.045f, 0f),
                    Ell(new Vector3(0f, 0.03f, 0f), new Vector3(0.12f, 0.05f, 0.12f), 0.07f),
                    Floor(0f)), PropSlot.Wood),
                Piece(PShape("TallTree_Canopy",
                    Ell(new Vector3(0f, 1.12f, 0.01f), new Vector3(0.22f, 0.28f, 0.22f)).Bumps(0.02f, 12f),
                    Ell(new Vector3(0.04f, 1.5f, -0.02f), new Vector3(0.28f, 0.36f, 0.27f), 0.12f).Bumps(0.02f, 12f),
                    Ell(new Vector3(-0.03f, 1.9f, 0.02f), new Vector3(0.22f, 0.3f, 0.22f), 0.12f).Bumps(0.02f, 12f),
                    Ell(new Vector3(0.01f, 2.24f, 0f), new Vector3(0.13f, 0.2f, 0.13f), 0.1f).Bumps(0.02f, 12f)), PropSlot.Leaf, PropMotion.Sway, 0.04f),
            };
            return tree;
        }

        // ---- village ----

        /// <summary>Round clay hut, 2 x 2: thatched cone roof, chimney (smoke when restored), round door to the front.</summary>
        static PropRecipe ClayHut()
        {
            var hut = Recipe("ClayHut", new Vector2Int(2, 2), true, false, 2400);
            hut.palette = Palette(HutClay, Wood, Stone, Thatch, C("#FFC86B", 1f), C("#8E5A3C"));
            hut.yawJitter = 10f;
            hut.scaleJitter = 0.05f;
            hut.resolution = 48;
            hut.pieces = new List<PropPiece>
            {
                Piece(PShape("ClayHut_Wall",
                    Cone(new Vector3(0f, 0f, 0f), new Vector3(0f, 0.78f, 0f), 0.7f, 0.64f, 0f),
                    Ell(new Vector3(0f, 0.32f, 0.68f), new Vector3(0.2f, 0.32f, 0.12f), 0.02f).Sub(),
                    Floor(0f)), PropSlot.Clay),
                Piece(PShape("ClayHut_Roof",
                    Ell(new Vector3(0f, 0.86f, 0f), new Vector3(0.9f, 0.16f, 0.9f), 0f).Bumps(0.015f, 18f),
                    Cone(new Vector3(0f, 0.9f, 0f), new Vector3(0f, 1.75f, 0f), 0.62f, 0.05f, 0.15f).Bumps(0.015f, 18f),
                    Plane(new Vector3(0f, 0.75f, 0f), Vector3.down, 0.02f)), PropSlot.Leaf),
                Piece(PShape("ClayHut_Base",
                    Torus(new Vector3(0f, 0.04f, 0f), 0.7f, 0.07f, Vector3.zero, 0f)), PropSlot.Stone),
                Piece(PShape("ClayHut_Door",
                    Ell(new Vector3(0f, 0.3f, 0.64f), new Vector3(0.19f, 0.3f, 0.05f), 0f),
                    Floor(0f)), PropSlot.Wood),
                Piece(PShape("ClayHut_Window",
                    Ell(new Vector3(0.5f, 0.45f, 0.46f), new Vector3(0.1f, 0.1f, 0.07f), 0f)), PropSlot.Glow, PropMotion.Flicker, 0.002f),
                Piece(PShape("ClayHut_Chimney",
                    Cone(new Vector3(0.32f, 1.0f, -0.3f), new Vector3(0.36f, 1.75f, -0.34f), 0.1f, 0.085f, 0f),
                    Sphere(new Vector3(0.36f, 1.8f, -0.34f), 0.06f, 0.02f).Sub()), PropSlot.Accent),
                Piece(PShape("ClayHut_Smoke", SmokePuff(0.08f)), PropSlot.Accent, PropMotion.Steam, 0.05f, new Vector3(0.36f, 1.86f, -0.34f)),
            };
            return hut;
        }

        /// <summary>Straight fence along X (rails reach the tile edges so neighbours join). Turned = along the rows.</summary>
        static PropRecipe Fence()
        {
            var fence = Recipe("Fence", new Vector2Int(1, 1), true, false, 500);
            fence.palette = Palette(Clay, FenceWood, Stone, Leaf, Ember, LightWood);
            fence.yawJitter = 0f;
            fence.scaleJitter = 0f;
            fence.outline = 0.018f;
            fence.pieces = new List<PropPiece>
            {
                Piece(PShape("Fence_Wood",
                    Cone(new Vector3(-0.32f, 0f, 0f), new Vector3(-0.32f, 0.44f, 0f), 0.042f, 0.036f, 0f),
                    Cone(new Vector3(0.32f, 0f, 0f), new Vector3(0.32f, 0.42f, 0f), 0.042f, 0.036f, 0f),
                    Cone(new Vector3(-0.5f, 0.32f, 0f), new Vector3(0.5f, 0.32f, 0f), 0.022f, 0.022f, 0.01f),
                    Cone(new Vector3(-0.5f, 0.17f, 0f), new Vector3(0.5f, 0.17f, 0f), 0.022f, 0.022f, 0.01f),
                    Floor(0f)), PropSlot.Wood),
            };
            return fence;
        }

        /// <summary>Fence corner: a post with rails to +X and +Z. The legend's yaw (0/90/180/270) points it.</summary>
        static PropRecipe FenceCorner()
        {
            var corner = Recipe("FenceCorner", new Vector2Int(1, 1), true, false, 500);
            corner.palette = Palette(Clay, FenceWood, Stone, Leaf, Ember, LightWood);
            corner.yawJitter = 0f;
            corner.scaleJitter = 0f;
            corner.outline = 0.018f;
            corner.pieces = new List<PropPiece>
            {
                Piece(PShape("FenceCorner_Wood",
                    Cone(new Vector3(0f, 0f, 0f), new Vector3(0f, 0.46f, 0f), 0.046f, 0.04f, 0f),
                    Cone(new Vector3(0f, 0.32f, 0f), new Vector3(0.5f, 0.32f, 0f), 0.022f, 0.022f, 0.01f),
                    Cone(new Vector3(0f, 0.17f, 0f), new Vector3(0.5f, 0.17f, 0f), 0.022f, 0.022f, 0.01f),
                    Cone(new Vector3(0f, 0.32f, 0f), new Vector3(0f, 0.32f, 0.5f), 0.022f, 0.022f, 0.01f),
                    Cone(new Vector3(0f, 0.17f, 0f), new Vector3(0f, 0.17f, 0.5f), 0.022f, 0.022f, 0.01f),
                    Floor(0f)), PropSlot.Wood),
            };
            return corner;
        }

        /// <summary>A post with two arrow boards pointing opposite ways (no text).</summary>
        static PropRecipe Signpost()
        {
            var sign = Recipe("Signpost", new Vector2Int(1, 1), true, false, 600);
            sign.palette = Palette(Clay, FenceWood, Stone, Leaf, Ember, LightWood);
            sign.yawJitter = 25f;
            sign.pieces = new List<PropPiece>
            {
                Piece(PShape("Signpost_Post",
                    Cone(new Vector3(0f, 0f, 0f), new Vector3(0f, 0.86f, 0f), 0.045f, 0.038f, 0f),
                    Sphere(new Vector3(0f, 0.87f, 0f), 0.045f, 0.02f),
                    Floor(0f)), PropSlot.Wood),
                Piece(PShape("Signpost_Boards",
                    Box(new Vector3(0.1f, 0.72f, 0.04f), new Vector3(0.2f, 0.055f, 0.02f), 0.012f, 0f),
                    Cone(new Vector3(0.3f, 0.72f, 0.04f), new Vector3(0.4f, 0.72f, 0.04f), 0.055f, 0.008f, 0.01f),
                    Box(new Vector3(-0.08f, 0.53f, 0.04f), new Vector3(0.18f, 0.05f, 0.02f), 0.012f, 0f),
                    Cone(new Vector3(-0.26f, 0.53f, 0.04f), new Vector3(-0.35f, 0.53f, 0.04f), 0.05f, 0.008f, 0.01f)), PropSlot.Accent),
            };
            return sign;
        }

        /// <summary>Stacked split logs (3, 2, 1) with pale cut ends.</summary>
        static PropRecipe Woodpile()
        {
            var pile = Recipe("Woodpile", new Vector2Int(1, 1), true, false, 800);
            pile.palette = Palette(Clay, Wood, Stone, Leaf, Ember, C("#D9B07A"));
            pile.yawJitter = 20f;
            var logs = new List<SdfPrimitive>();
            var ends = new List<SdfPrimitive>();
            foreach (var (y, z) in new[] { (0.085f, -0.17f), (0.085f, 0f), (0.085f, 0.17f), (0.23f, -0.085f), (0.23f, 0.085f), (0.37f, 0f) })
            {
                logs.Add(Cone(new Vector3(-0.3f, y, z), new Vector3(0.3f, y, z), 0.08f, 0.08f, 0.01f));
                ends.Add(Ell(new Vector3(0.305f, y, z), new Vector3(0.012f, 0.068f, 0.068f), 0f).Mirror());
            }
            logs.Add(Floor(0f));
            pile.pieces = new List<PropPiece>
            {
                Piece(PShape("Woodpile_Logs", logs.ToArray()), PropSlot.Wood),
                Piece(PShape("Woodpile_Ends", ends.ToArray()), PropSlot.Accent),
            };
            return pile;
        }

        /// <summary>Low mound of autumn leaves (walk-through filler).</summary>
        static PropRecipe LeafPile()
        {
            var pile = Recipe("LeafPile", new Vector2Int(1, 1), false, true, 400);
            pile.palette = Palette(Clay, Wood, Stone, C("#C4482A"), C("#F3A33A", 0.3f), Smoke);
            pile.tips = new PropTips { on = true, onSlot = PropSlot.Leaf, tipSlot = PropSlot.Glow, at = 0.6f, byHeight = 0.6f };
            pile.outline = 0.015f;
            pile.scaleJitter = 0.15f;
            pile.pieces = new List<PropPiece>
            {
                Piece(PShape("LeafPile_Leaves",
                    Ell(new Vector3(0f, 0.05f, 0f), new Vector3(0.28f, 0.11f, 0.24f)).Bumps(0.03f, 11f),
                    Ell(new Vector3(0.12f, 0.1f, 0.06f), new Vector3(0.13f, 0.1f, 0.12f), 0.05f).Bumps(0.025f, 13f),
                    Ell(new Vector3(-0.1f, 0.09f, -0.08f), new Vector3(0.12f, 0.09f, 0.11f), 0.05f).Bumps(0.025f, 13f),
                    Ell(new Vector3(0.02f, 0.16f, -0.02f), new Vector3(0.09f, 0.07f, 0.09f), 0.05f).Bumps(0.02f, 15f),
                    Floor(0f)), PropSlot.Leaf),
            };
            return pile;
        }

        // ---- temple ----

        /// <summary>
        /// Tall layered rock segment (5 tiles high, ~2.6 trees), terraced back with autumn grass on the ledges, wider than
        /// its tile so a row reads as one wall. Its second look is the same segment turned 180 (other ledges in front).
        /// </summary>
        static PropRecipe CliffWall()
        {
            var cliff = Recipe("CliffWall", new Vector2Int(1, 1), true, false, 1400);
            cliff.palette = Palette(Clay, Wood, CliffStone, CliffGrass, Ember, Smoke);
            cliff.yawJitter = 0f;
            cliff.scaleJitter = 0.03f;
            cliff.resolution = 44;
            cliff.pieces = new List<PropPiece>
            {
                Piece(PShape("CliffWall_Rock",
                    Box(new Vector3(0f, 0.7f, 0f), new Vector3(0.74f, 0.7f, 0.46f), 0.07f, 0f, new Vector3(0f, 2f, 0f)).Bumps(0.06f, 3.2f),
                    Box(new Vector3(0.05f, 1.95f, -0.06f), new Vector3(0.74f, 0.6f, 0.4f), 0.07f, 0.04f, new Vector3(0f, -2f, 1.5f)).Bumps(0.06f, 3.4f),
                    Sphere(new Vector3(-0.3f, 1.35f, 0.3f), 0.26f, 0.08f).Bumps(0.05f, 4f),
                    Box(new Vector3(-0.04f, 3.1f, -0.13f), new Vector3(0.74f, 0.58f, 0.35f), 0.08f, 0.04f, new Vector3(0f, 2f, -1.5f)).Bumps(0.06f, 3.6f),
                    Sphere(new Vector3(0.28f, 2.5f, 0.2f), 0.22f, 0.08f).Bumps(0.05f, 4f),
                    Box(new Vector3(0.02f, 4.25f, -0.2f), new Vector3(0.74f, 0.62f, 0.3f), 0.18f, 0.04f, new Vector3(0f, -2f, 0f)).Bumps(0.06f, 3.8f),
                    Floor(0f)), PropSlot.Stone),
                Piece(PShape("CliffWall_Ledges",
                    Ell(new Vector3(0.15f, 1.42f, 0.36f), new Vector3(0.38f, 0.05f, 0.1f), 0f).Bumps(0.012f, 25f),
                    Ell(new Vector3(-0.2f, 2.58f, 0.26f), new Vector3(0.3f, 0.05f, 0.1f), 0f).Bumps(0.012f, 25f),
                    Ell(new Vector3(0.12f, 3.69f, 0.18f), new Vector3(0.34f, 0.05f, 0.09f), 0f).Bumps(0.012f, 25f),
                    Ell(new Vector3(-0.25f, 1.42f, -0.4f), new Vector3(0.24f, 0.05f, 0.08f), 0f).Bumps(0.012f, 25f),
                    Ell(new Vector3(0.22f, 2.58f, -0.44f), new Vector3(0.26f, 0.05f, 0.07f), 0f).Bumps(0.012f, 25f),
                    Ell(new Vector3(0f, 4.86f, -0.18f), new Vector3(0.36f, 0.06f, 0.24f), 0f).Bumps(0.015f, 20f)), PropSlot.Leaf),
            };
            return cliff;
        }

        /// <summary>
        /// The forge mouth, 3 x 1, as tall as the cliff: a stone face with a big arched opening, iron bands round the
        /// arch, cold and dark inside (step 9b lights it with its own recipe).
        /// </summary>
        static PropRecipe ForgeDoor()
        {
            var door = Recipe("ForgeDoor", new Vector2Int(3, 1), true, false, 2600);
            // Clay = iron bands, Wood = the dark inside, Accent = cold coals.
            door.palette = Palette(C("#3D3633"), C("#1E1816"), CliffStone, CliffGrass, Ember, C("#4A4442"));
            door.yawJitter = 0f;
            door.scaleJitter = 0f;
            door.resolution = 56;
            door.pieces = new List<PropPiece>
            {
                Piece(PShape("ForgeDoor_Stone",
                    Box(new Vector3(0f, 2.5f, -0.05f), new Vector3(1.62f, 2.5f, 0.45f), 0.22f, 0f).Bumps(0.06f, 3.2f),
                    Box(new Vector3(0f, 0.9f, 0.35f), new Vector3(0.82f, 0.9f, 0.55f), 0.04f, 0.02f).Sub(),
                    Ell(new Vector3(0f, 1.8f, 0.35f), new Vector3(0.82f, 0.78f, 0.55f), 0.02f).Sub(),
                    Box(new Vector3(0f, 2.85f, 0.42f), new Vector3(0.18f, 0.2f, 0.06f), 0.03f, 0.03f),
                    Floor(0f)), PropSlot.Stone),
                Piece(PShape("ForgeDoor_Inside",
                    Box(new Vector3(0f, 1.3f, -0.22f), new Vector3(0.84f, 1.3f, 0.06f), 0.02f, 0f),
                    Floor(0f)), PropSlot.Wood),
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
            return door;
        }

        // ---- ruins ----

        /// <summary>A broken kiln: the dome's top caved in, its chimney fallen beside it, some rubble.</summary>
        static PropRecipe RuinedKiln()
        {
            var ruin = Recipe("RuinedKiln", new Vector2Int(1, 1), true, false, 1200);
            ruin.palette = Palette(C("#B07A52"), Wood, Stone, Moss, Ember, C("#3A2C26"));
            ruin.yawJitter = 40f;
            ruin.pieces = new List<PropPiece>
            {
                Piece(PShape("RuinedKiln_Body",
                    Ell(new Vector3(0f, 0.33f, -0.05f), new Vector3(0.36f, 0.4f, 0.36f)),
                    Ell(new Vector3(0f, 0.3f, -0.05f), new Vector3(0.28f, 0.34f, 0.28f), 0.02f).Sub(),
                    Sphere(new Vector3(0.18f, 0.78f, 0.1f), 0.36f, 0.04f).Sub(),
                    Ell(new Vector3(0f, 0.16f, 0.3f), new Vector3(0.15f, 0.18f, 0.15f), 0.03f).Sub(),
                    Cone(new Vector3(-0.38f, 0.07f, 0.3f), new Vector3(0.12f, 0.08f, 0.4f), 0.08f, 0.07f, 0f),
                    Sphere(new Vector3(0.12f, 0.08f, 0.4f), 0.05f, 0.01f).Sub(),
                    Floor(0f)), PropSlot.Clay),
                Piece(PShape("RuinedKiln_Rubble",
                    Torus(new Vector3(0f, 0.04f, -0.05f), 0.34f, 0.06f, Vector3.zero, 0f),
                    Sphere(new Vector3(0.36f, 0.05f, 0.22f), 0.07f, 0.02f),
                    Sphere(new Vector3(0.28f, 0.04f, 0.34f), 0.05f, 0.02f),
                    Floor(0f)), PropSlot.Stone),
                Piece(PShape("RuinedKiln_Dark",
                    Ell(new Vector3(0f, 0.06f, -0.05f), new Vector3(0.26f, 0.04f, 0.26f), 0f)), PropSlot.Accent),
            };
            return ruin;
        }

        /// <summary>Scattered glazed pot shards (walk-through filler).</summary>
        static PropRecipe PotShards()
        {
            var shards = Recipe("PotShards", new Vector2Int(1, 1), false, true, 500);
            shards.palette = Palette(C("#B07A52"), Wood, Stone, Leaf, Ember, C("#3F8A86"));
            shards.outline = 0.012f;
            shards.pieces = new List<PropPiece>
            {
                Piece(PShape("PotShards_Glazed",
                    Ell(new Vector3(-0.16f, 0.025f, 0.08f), new Vector3(0.11f, 0.025f, 0.07f), 0f, new Vector3(0f, 30f, 12f)),
                    Ell(new Vector3(0.18f, 0.03f, -0.1f), new Vector3(0.09f, 0.03f, 0.06f), 0f, new Vector3(-10f, -40f, 0f)),
                    Ell(new Vector3(0.05f, 0.02f, 0.22f), new Vector3(0.07f, 0.02f, 0.05f), 0f, new Vector3(0f, 70f, 8f)),
                    Floor(0f)), PropSlot.Accent),
                Piece(PShape("PotShards_Clay",
                    Ell(new Vector3(0.02f, 0.03f, -0.05f), new Vector3(0.1f, 0.03f, 0.06f), 0f, new Vector3(14f, 10f, 0f)),
                    Ell(new Vector3(-0.22f, 0.02f, -0.18f), new Vector3(0.06f, 0.02f, 0.05f), 0f, new Vector3(0f, -20f, 0f)),
                    Floor(0f)), PropSlot.Clay),
            };
            return shards;
        }

        /// <summary>Glowing cracks in a dark crust: sputtering dim while unrestored, bright when restored (Glow slot).</summary>
        static PropRecipe EmberVent()
        {
            var vent = Recipe("EmberVent", new Vector2Int(1, 1), false, true, 500);
            vent.palette = Palette(Clay, Wood, C("#4A3F3A"), Leaf, C("#FF7A2E", 1.3f), Smoke);
            vent.outline = 0.012f;
            vent.pieces = new List<PropPiece>
            {
                Piece(PShape("EmberVent_Crust",
                    Ell(new Vector3(0f, 0.012f, 0f), new Vector3(0.34f, 0.03f, 0.3f), 0f).Bumps(0.01f, 14f),
                    Ell(new Vector3(0.2f, 0.02f, 0.15f), new Vector3(0.12f, 0.035f, 0.1f), 0.03f),
                    Floor(0f)), PropSlot.Stone),
                Piece(PShape("EmberVent_Cracks",
                    Ell(new Vector3(0f, 0.035f, 0f), new Vector3(0.26f, 0.02f, 0.03f), 0f, new Vector3(0f, 20f, 0f)),
                    Ell(new Vector3(0.1f, 0.035f, -0.12f), new Vector3(0.14f, 0.02f, 0.025f), 0f, new Vector3(0f, -55f, 0f)),
                    Ell(new Vector3(-0.12f, 0.035f, 0.1f), new Vector3(0.12f, 0.02f, 0.025f), 0f, new Vector3(0f, 70f, 0f)),
                    Ell(new Vector3(-0.05f, 0.035f, -0.16f), new Vector3(0.09f, 0.02f, 0.02f), 0f, new Vector3(0f, 110f, 0f))), PropSlot.Glow, PropMotion.Flicker, 0.001f),
            };
            return vent;
        }
    }
}
