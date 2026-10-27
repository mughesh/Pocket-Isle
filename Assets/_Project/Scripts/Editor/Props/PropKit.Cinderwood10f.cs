using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Diorama.EditorTools.StarterKit;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 10f: Cinderwood's hand moment props. The village shrine with its flame out (the Hearth Shrine without its
    /// flame piece) and the dying wisp drawn on its own (centred on its base so it scales in place; its Glow slot is a
    /// cold blue when "unrestored" and the shrine's orange when restored, FlameCupPiece blends them).
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
    }
}
