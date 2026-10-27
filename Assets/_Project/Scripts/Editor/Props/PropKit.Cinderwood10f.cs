using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Diorama.EditorTools.StarterKit;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 10f: Cinderwood's hand moment props. The village shrine with its flame out (the Hearth Shrine without its
    /// flame piece) and the dying wisp drawn on its own (centred on its base so it scales in place; its Glow slot is a
    /// cold blue when "unrestored" and the shrine's orange when restored, FlameCupPiece blends them). The smoky hollow's
    /// dark smoke (blocks walking; ember specks sputter in it). The forge's firebox (a cliff segment with a stone mouth
    /// and a clay chimney; its coals and chimney smoke wake in a warm spot) and the ember carried on the palm.
    /// </summary>
    public static partial class PropKit
    {
        public static Dictionary<string, PropRecipe> WriteCinderwoodMomentProps()
        {
            DioramaSceneSetup.EnsureFolder(Root);
            DioramaSceneSetup.EnsureFolder($"{Root}/Shapes");
            var all = new Dictionary<string, PropRecipe>();
            void Add(PropRecipe r)
            {
                if (r == null) return;
                all[r.name] = r;
                EditorUtility.SetDirty(r);
            }
            Add(HearthShrineCold());
            Add(ShrineWisp());
            Add(SmokeCloud());
            Add(Firebox());
            Add(EmberCoal());
            return all;
        }

        /// <summary>The Hearth Shrine's own pieces minus its flame (the bowl stays, cold).</summary>
        static PropRecipe HearthShrineCold()
        {
            var lit = Load(HearthShrines.RecipeName);
            if (lit == null)
            {
                Debug.LogError("[PropKit] No HearthShrine recipe: write the prop kit first.");
                return null;
            }
            var cold = Recipe("HearthShrineCold", lit.footprint, true, false, lit.triangleBudget);
            cold.palette = (PropColor[])lit.palette.Clone();
            cold.yawJitter = 0f;
            cold.scaleJitter = 0f;
            cold.resolution = lit.resolution;
            cold.outline = lit.outline;
            cold.spec = lit.spec;
            cold.pieces = new List<PropPiece>();
            foreach (var p in lit.pieces)
                if (p.shape == null || p.shape.name != "HearthShrine_Flame") cold.pieces.Add(p);
            return cold;
        }

        /// <summary>A small flame standing on its own base (origin = the bowl), one Glow piece that flickers.</summary>
        static PropRecipe ShrineWisp()
        {
            var wisp = Recipe("ShrineWisp", Vector2Int.one, false, false, 600);
            var flame = new PropColor(Warmer(Hex("#FFB04A")), Hex("#6F9BFF"), 1.3f);
            wisp.palette = Palette(Clay, Wood, Stone, Leaf, flame, flame);
            wisp.yawJitter = 0f;
            wisp.scaleJitter = 0f;
            wisp.outline = 0.012f;
            wisp.resolution = 40;
            wisp.pieces = new List<PropPiece>
            {
                Piece(PShape("ShrineWisp_Flame",
                    Sphere(new Vector3(0f, 0.02f, 0f), 0.13f),
                    Cone(new Vector3(0f, 0.04f, 0f), new Vector3(0f, 0.4f, 0f), 0.11f, 0.012f, 0.08f),
                    Cone(new Vector3(0.07f, 0.03f, 0.02f), new Vector3(0.13f, 0.24f, 0.04f), 0.05f, 0.01f, 0.06f),
                    Cone(new Vector3(-0.06f, 0.03f, -0.03f), new Vector3(-0.11f, 0.2f, -0.05f), 0.045f, 0.01f, 0.06f)),
                    PropSlot.Glow, PropMotion.Flicker, 0.03f),
            };
            return wisp;
        }

        /// <summary>
        /// A thick dark smoke puff sitting on a path tile (blocks it): heaped grey-violet blobs that roll slowly, a darker
        /// underside, a few ember specks sputtering in it (each lantern's warm spot is smaller than a tile, so it never lights them).
        /// </summary>
        static PropRecipe SmokeCloud()
        {
            var cloud = Recipe("SmokeCloud", Vector2Int.one, true, true, 1100);
            // Oct 27 headset: the grey-violet read as purple blocks; charcoal smoke, lighter on top.
            cloud.palette = Palette(C("#57524F"), Wood, C("#3A3634"), Leaf, C("#FF7A2E", 1.1f), C("#7A7470"));
            cloud.outline = 0.016f;
            cloud.spec = 0.03f;
            cloud.scaleJitter = 0.15f;
            cloud.pieces = new List<PropPiece>
            {
                Piece(PShape("SmokeCloud_Body",
                    Sphere(new Vector3(0f, 0.42f, 0f), 0.32f),
                    Sphere(new Vector3(0.34f, 0.34f, 0.06f), 0.24f, 0.06f),
                    Sphere(new Vector3(-0.36f, 0.36f, -0.04f), 0.25f, 0.06f),
                    Sphere(new Vector3(0.1f, 0.66f, 0.04f), 0.22f, 0.06f),
                    Sphere(new Vector3(-0.15f, 0.62f, -0.06f), 0.21f, 0.06f),
                    Sphere(new Vector3(0.04f, 0.36f, 0.3f), 0.22f, 0.06f),
                    Sphere(new Vector3(-0.04f, 0.38f, -0.3f), 0.22f, 0.06f),
                    Plane(new Vector3(0f, 0.12f, 0f), Vector3.down, 0.06f)), PropSlot.Clay, PropMotion.Sway, 0.09f),
                Piece(PShape("SmokeCloud_Top",
                    Sphere(new Vector3(0.02f, 0.84f, 0f), 0.14f),
                    Sphere(new Vector3(-0.1f, 0.78f, 0.08f), 0.11f, 0.05f)), PropSlot.Accent, PropMotion.Sway, 0.15f),
                Piece(PShape("SmokeCloud_Under",
                    Ell(new Vector3(0f, 0.14f, 0f), new Vector3(0.52f, 0.08f, 0.46f), 0f)), PropSlot.Stone, PropMotion.Sway, 0.03f),
                Piece(PShape("SmokeCloud_Embers",
                    Sphere(new Vector3(0.22f, 0.52f, 0.3f), 0.035f),
                    Sphere(new Vector3(-0.3f, 0.44f, 0.24f), 0.03f),
                    Sphere(new Vector3(0.05f, 0.74f, 0.2f), 0.028f),
                    Sphere(new Vector3(-0.12f, 0.3f, 0.36f), 0.03f)), PropSlot.Glow, PropMotion.Flicker, 0.01f),
            };
            return cloud;
        }

        /// <summary>
        /// The forge's firebox, 1 x 1 in the cliff line beside the bellows: the cliff segment's own rock and ledges, a stone
        /// hearth mouth sticking out at its foot (an arched opening with a coal bed inside), a clay chimney running up the
        /// cliff face with smoke at its top. Cold: dull coals, no smoke; in the forge's warm spot they burn and it smokes.
        /// </summary>
        static PropRecipe Firebox()
        {
            var cliff = Load("CliffWall");
            if (cliff == null)
            {
                Debug.LogError("[PropKit] No CliffWall recipe: write the Cinderwood props first.");
                return null;
            }
            var box = Recipe("Firebox", Vector2Int.one, true, false, 3100);
            box.palette = (PropColor[])cliff.palette.Clone();
            box.palette[(int)PropSlot.Clay] = C("#B56A3E");
            box.palette[(int)PropSlot.Glow] = C("#FF8A2E", 1.4f);
            box.yawJitter = 0f;
            box.scaleJitter = 0f;
            box.resolution = cliff.resolution;
            box.pieces = new List<PropPiece>();
            foreach (var p in cliff.pieces) box.pieces.Add(p);
            box.pieces.Add(Piece(PShape("Firebox_Mouth",
                Box(new Vector3(-0.05f, 0.3f, 0.5f), new Vector3(0.4f, 0.3f, 0.17f), 0.06f, 0f).Bumps(0.02f, 6f),
                Ell(new Vector3(-0.05f, 0.24f, 0.66f), new Vector3(0.22f, 0.22f, 0.24f), 0.02f).Sub(),
                Torus(new Vector3(-0.05f, 0.27f, 0.66f), 0.23f, 0.045f, new Vector3(90f, 0f, 0f), 0.03f),
                Floor(0f)), PropSlot.Stone));
            box.pieces.Add(Piece(PShape("Firebox_Coals",
                Ell(new Vector3(-0.05f, 0.08f, 0.56f), new Vector3(0.19f, 0.08f, 0.12f), 0f).Bumps(0.015f, 14f),
                Sphere(new Vector3(-0.12f, 0.15f, 0.56f), 0.06f, 0.04f),
                Sphere(new Vector3(0.03f, 0.14f, 0.58f), 0.055f, 0.04f)), PropSlot.Glow, PropMotion.Flicker, 0.004f));
            box.pieces.Add(Piece(PShape("Firebox_Chimney",
                Cone(new Vector3(0.36f, 0.5f, 0.55f), new Vector3(0.36f, 4.7f, 0.5f), 0.12f, 0.1f, 0f),
                Torus(new Vector3(0.36f, 4.72f, 0.5f), 0.11f, 0.035f, Vector3.zero, 0.03f),
                Sphere(new Vector3(0.36f, 4.78f, 0.5f), 0.08f, 0.02f).Sub(),
                Torus(new Vector3(0.36f, 2.2f, 0.52f), 0.12f, 0.025f, Vector3.zero, 0.01f),
                Torus(new Vector3(0.36f, 3.5f, 0.51f), 0.115f, 0.025f, Vector3.zero, 0.01f)), PropSlot.Clay));
            box.pieces.Add(Piece(PShape("Firebox_Smoke", SmokePuff(0.12f)), PropSlot.Accent, PropMotion.Steam, 0.08f, new Vector3(0.36f, 4.9f, 0.5f)));
            return box;
        }

        /// <summary>The ember on the palm: a small bumpy glowing coal with a few dark crust flecks, centred on its origin.</summary>
        static PropRecipe EmberCoal()
        {
            var ember = Recipe("Ember", Vector2Int.one, false, true, 500);
            ember.palette = Palette(C("#3A2A22"), Wood, Stone, Leaf, new PropColor(Warmer(Hex("#FF9A3C")), Hex("#FF9A3C"), 1.6f), Smoke);
            ember.outline = 0.03f;
            ember.yawJitter = 0f;
            ember.scaleJitter = 0f;
            ember.pieces = new List<PropPiece>
            {
                Piece(PShape("Ember_Coal",
                    Sphere(Vector3.zero, 0.42f).Bumps(0.05f, 7f),
                    Sphere(new Vector3(0.22f, 0.1f, 0.08f), 0.24f, 0.08f)), PropSlot.Glow, PropMotion.Flicker, 0.02f),
                Piece(PShape("Ember_Crust",
                    Ell(new Vector3(-0.2f, 0.3f, 0.12f), new Vector3(0.16f, 0.06f, 0.12f), 0f),
                    Ell(new Vector3(0.18f, -0.25f, -0.2f), new Vector3(0.14f, 0.05f, 0.12f), 0f),
                    Ell(new Vector3(0.05f, 0.05f, 0.4f), new Vector3(0.1f, 0.08f, 0.04f), 0f)), PropSlot.Clay),
            };
            return ember;
        }
    }
}
