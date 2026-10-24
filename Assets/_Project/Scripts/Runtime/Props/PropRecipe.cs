using System;
using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>Colour slots of a prop palette. Each piece of a prop is painted with one slot.</summary>
    public enum PropSlot { Clay, Wood, Stone, Leaf, Glow, Accent }

    /// <summary>Shader motion of a piece. Lives in the vertex shader so props stay instanced.</summary>
    public enum PropMotion
    {
        None,
        [Tooltip("Leans in the wind, more toward the top of the piece (canopies, grass tufts). Amount in shape units.")]
        Sway,
        [Tooltip("Glow flickers and the piece wobbles a little toward its top (flames, lantern light). Amount in shape units.")]
        Flicker,
        [Tooltip("Puffs up, drifts and wobbles around its centre in a loop; gone while the realm is unrestored (steam, smoke).")]
        Steam,
    }

    [Serializable]
    public struct PropColor
    {
        [Tooltip("Colour once the realm is restored.")]
        public Color restored;
        [Tooltip("Colour while the realm is unrestored (ash grey, cold).")]
        public Color unrestored;
        [Tooltip("Self-lit amount (scaled by how restored the realm is, so glows go cold).")]
        [Range(0f, 2f)] public float glow;

        public PropColor(Color restored, Color unrestored, float glow = 0f)
        {
            this.restored = restored;
            this.unrestored = unrestored;
            this.glow = glow;
        }
    }

    /// <summary>One piece of a prop: an SDF shape (or a creature kit part) placed in prop units and painted with one slot.</summary>
    [Serializable]
    public struct PropPiece
    {
        [Tooltip("The shape. Leave empty to use Part instead.")]
        public SdfShape shape;
        [Tooltip("Optional creature kit part (its shape and pivot) when Shape is empty.")]
        public CreaturePart part;
        public Vector3 position;
        public Vector3 rotation;
        [Tooltip("0 = 1.")]
        public float scale;
        [Tooltip("Also adds a copy mirrored across X.")]
        public bool mirrorX;
        public PropSlot slot;
        public PropMotion motion;
        [Tooltip("Sway / wobble distance in shape units (0 = a default per motion).")]
        public float motionAmount;
    }

    /// <summary>Faces of one slot that point up are painted with another slot (glowing leaf tips on a canopy).</summary>
    [Serializable]
    public struct PropTips
    {
        public bool on;
        public PropSlot onSlot;
        public PropSlot tipSlot;
        [Tooltip("-1..1: how directly a face must point up to get the tip colour.")]
        public float at;
    }

    /// <summary>
    /// An environment prop made like a creature: SDF pieces baked to one mesh (Tools > Pocket Isle > Props), drawn
    /// instanced by the island from the world map's props layer. Shape units are map tiles: a 1x1 prop spans -0.5..0.5
    /// on X and Z, its origin is the middle of its footprint on the ground, and +Z faces the player.
    /// </summary>
    [CreateAssetMenu(menuName = "Pocket Isle/Props/Prop Recipe", fileName = "Prop")]
    public class PropRecipe : ScriptableObject
    {
        public const int SlotCount = 6;

        public List<PropPiece> pieces = new List<PropPiece>();

        [Header("Look")]
        [Tooltip("Clay, Wood, Stone, Leaf, Glow, Accent.")]
        public PropColor[] palette = new PropColor[SlotCount];
        public PropTips tips;
        [Range(0f, 2f)] public float spec = 0.15f;
        [Tooltip("Outline thickness in shape units (tiles). 0 = no outline.")]
        public float outline = 0.022f;

        [Header("Placement")]
        [Tooltip("Tiles covered (columns, rows), 1x1 up to 2x2. The map char sits on the first tile; the rest extend to +column / +row.")]
        public Vector2Int footprint = Vector2Int.one;
        public bool blocksWalking = true;
        [Tooltip("Any yaw (1x1 only). Off = faces the player, plus the jitter below.")]
        public bool randomYaw = true;
        [Tooltip("Random yaw either way, degrees (when Random Yaw is off).")]
        public float yawJitter = 15f;
        [Tooltip("Random scale either way (0.1 = 90..110 %).")]
        [Range(0f, 0.5f)] public float scaleJitter = 0.1f;

        [Header("Bake")]
        [Tooltip("Grid cells across the longest side of each piece before simplifying.")]
        [Range(16, 96)] public int resolution = 40;
        public int triangleBudget = 1500;
        [Tooltip("Written by the baker.")]
        public Mesh bakedMesh;

        public Vector2Int Footprint => new Vector2Int(Mathf.Clamp(footprint.x, 1, 2), Mathf.Clamp(footprint.y, 1, 2));

        public PropColor Color(PropSlot slot) =>
            palette != null && (int)slot < palette.Length ? palette[(int)slot] : new PropColor(UnityEngine.Color.magenta, UnityEngine.Color.magenta);

        static readonly int[] RestoredIds = Ids("_Slot");
        static readonly int[] AshIds = Ids("_Ash");
        static readonly int GlowAId = Shader.PropertyToID("_GlowA");
        static readonly int GlowBId = Shader.PropertyToID("_GlowB");
        static readonly int TipsId = Shader.PropertyToID("_Tips");
        static readonly int SpecId = Shader.PropertyToID("_Spec");
        static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");

        static int[] Ids(string prefix)
        {
            var ids = new int[SlotCount];
            for (int i = 0; i < SlotCount; i++) ids[i] = Shader.PropertyToID(prefix + i);
            return ids;
        }

        /// <summary>Writes the palette into a property block for the Diorama/Prop shader.</summary>
        public void ApplyTo(MaterialPropertyBlock block)
        {
            var glow = new float[SlotCount];
            for (int i = 0; i < SlotCount; i++)
            {
                var c = Color((PropSlot)i);
                block.SetColor(RestoredIds[i], c.restored);
                block.SetColor(AshIds[i], c.unrestored);
                glow[i] = c.glow;
            }
            block.SetVector(GlowAId, new Vector4(glow[0], glow[1], glow[2], glow[3]));
            block.SetVector(GlowBId, new Vector4(glow[4], glow[5], 0f, 0f));
            block.SetVector(TipsId, tips.on ? new Vector4((int)tips.onSlot, (int)tips.tipSlot, tips.at, 1f) : Vector4.zero);
            block.SetFloat(SpecId, spec);
            block.SetFloat(OutlineWidthId, outline);
        }
    }
}
