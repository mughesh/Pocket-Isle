using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Diorama.EditorTools.StarterKit;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 9c: the fire shard (a glowing ember crystal, drawn ~3 cm: 1 shape unit = <see cref="ShardMetres"/>) and the
    /// shard holder (a small brass cradle on the rim, three cups: fire, water, wind; drawn at the same metres per unit).
    /// Not map props: the puzzle draws them on their own (PropView.PlaceLocal).
    /// </summary>
    public static partial class PropKit
    {
        /// <summary>Metres per shape unit when drawn at real size (the shard ~3 cm tall).</summary>
        public const float ShardMetres = 0.026f;
        /// <summary>The holder's cups (shape units, holder origin), left to right as the player sees them: fire, water, wind.</summary>
        public static readonly Vector3[] HolderCups = { new Vector3(0.62f, 0.16f, 0f), new Vector3(0f, 0.16f, 0f), new Vector3(-0.62f, 0.16f, 0f) };

        /// <summary>Step 10e: the wind shard, a slim teal crystal with a swirl of wind round it (drawn like the fire shard).</summary>
        public static PropRecipe WriteWindShardProp()
        {
            DioramaSceneSetup.EnsureFolder(Root);
            DioramaSceneSetup.EnsureFolder($"{Root}/Shapes");
            var shard = Recipe("WindShard", new Vector2Int(1, 1), false, false, 1000);
            shard.palette = Palette(Clay, Wood, Stone, Leaf, new PropColor(Hex("#4FD8C0"), Hex("#4FD8C0"), 1.2f), new PropColor(Hex("#D2FFF2"), Hex("#D2FFF2"), 0.9f));
            shard.yawJitter = shard.scaleJitter = 0f;
            shard.resolution = 26;
            shard.outline = 0.03f;
            shard.pieces = new List<PropPiece>
            {
                Piece(PShape("WindShard_Core",
                    Cone(new Vector3(0f, 0f, 0f), new Vector3(0f, 0.62f, 0f), 0.16f, 0.12f, 0f),
                    Cone(new Vector3(0f, 0.62f, 0f), new Vector3(0f, 1.05f, 0f), 0.12f, 0.01f, 0.02f),
                    Cone(new Vector3(0f, 0f, 0f), new Vector3(0f, -0.34f, 0f), 0.16f, 0.02f, 0.02f)), PropSlot.Glow, PropMotion.Flicker, 0.003f),
                Piece(PShape("WindShard_Swirl",
                    Torus(new Vector3(0f, 0.3f, 0f), 0.26f, 0.028f, new Vector3(72f, 0f, 18f), 0f),
                    Plane(new Vector3(0.05f, 0f, 0f), Vector3.left, 0.02f),
                    Torus(new Vector3(0f, 0.55f, 0f), 0.2f, 0.024f, new Vector3(68f, 0f, -22f), 0f)), PropSlot.Accent),
            };
            EditorUtility.SetDirty(shard);
            return shard;
        }

        public static Dictionary<string, PropRecipe> WriteShardProps()
        {
            DioramaSceneSetup.EnsureFolder(Root);
            DioramaSceneSetup.EnsureFolder($"{Root}/Shapes");
            var all = new Dictionary<string, PropRecipe>();

            var shard = Recipe("FireShard", new Vector2Int(1, 1), false, false, 900);
            shard.palette = Palette(Clay, Wood, Stone, Leaf, new PropColor(Hex("#FF8A30"), Hex("#FF8A30"), 1.2f), new PropColor(Hex("#FFD27A"), Hex("#FFD27A"), 0.9f));
            shard.yawJitter = shard.scaleJitter = 0f;
            shard.resolution = 22;
            shard.outline = 0.03f;
            shard.pieces = new List<PropPiece>
            {
                Piece(PShape("FireShard_Core",
                    Cone(new Vector3(0f, 0f, 0f), new Vector3(0f, 0.6f, 0f), 0.2f, 0.14f, 0f),
                    Cone(new Vector3(0f, 0.6f, 0f), new Vector3(0f, 0.98f, 0f), 0.14f, 0.01f, 0.02f),
                    Cone(new Vector3(0f, 0f, 0f), new Vector3(0f, -0.3f, 0f), 0.2f, 0.02f, 0.02f)), PropSlot.Glow, PropMotion.Flicker, 0.004f),
                Piece(PShape("FireShard_Side",
                    Cone(new Vector3(0.1f, 0.05f, 0.02f), new Vector3(0.3f, 0.42f, 0.05f), 0.08f, 0.01f, 0f),
                    Cone(new Vector3(-0.08f, 0f, -0.04f), new Vector3(-0.26f, 0.3f, -0.08f), 0.07f, 0.01f, 0f)), PropSlot.Accent),
            };
            all[shard.name] = shard;

            var holder = Recipe("ShardHolder", new Vector2Int(1, 1), false, false, 1400);
            holder.palette = Palette(new PropColor(Hex("#C9A040"), Hex("#C9A040"), 0.05f), new PropColor(Hex("#C8533E"), Hex("#C8533E"), 0.3f),
                new PropColor(Hex("#4F86C6"), Hex("#4F86C6"), 0.3f), new PropColor(Hex("#F2EEE6"), Hex("#F2EEE6"), 0.3f), Ember, Smoke);
            holder.yawJitter = holder.scaleJitter = 0f;
            holder.spec = 0.9f;
            holder.resolution = 56;
            holder.outline = 0.03f;
            var brass = new List<SdfPrimitive>
            {
                Ell(new Vector3(0f, 0.04f, 0f), new Vector3(1.0f, 0.06f, 0.36f), 0f),
            };
            foreach (var c in HolderCups)
            {
                brass.Add(Torus(c, 0.2f, 0.05f, Vector3.zero, 0.04f));
                brass.Add(Cone(new Vector3(c.x, 0.04f, 0f), new Vector3(c.x, c.y - 0.04f, 0f), 0.09f, 0.07f, 0.04f));
            }
            holder.pieces = new List<PropPiece>
            {
                Piece(PShape("ShardHolder_Brass", brass.ToArray()), PropSlot.Clay),
                Piece(PShape("ShardHolder_Fire", Ell(HolderCups[0] + new Vector3(0f, -0.03f, 0f), new Vector3(0.12f, 0.03f, 0.12f), 0f)), PropSlot.Wood),
                Piece(PShape("ShardHolder_Water", Ell(HolderCups[1] + new Vector3(0f, -0.03f, 0f), new Vector3(0.12f, 0.03f, 0.12f), 0f)), PropSlot.Stone),
                Piece(PShape("ShardHolder_Wind", Ell(HolderCups[2] + new Vector3(0f, -0.03f, 0f), new Vector3(0.12f, 0.03f, 0.12f), 0f)), PropSlot.Leaf),
            };
            all[holder.name] = holder;
            foreach (var r in all.Values) EditorUtility.SetDirty(r);
            return all;
        }
    }
}
