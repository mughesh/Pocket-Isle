using System;
using UnityEngine;

namespace Diorama
{
    /// <summary>Colour slots of a creature palette. Parts and pattern zones pick a slot, so one part fits any palette.</summary>
    public enum PaletteSlot { Base, Belly, Accent, Socks, Glow, Extra, Face }

    public enum PartMotion
    {
        None,
        [Tooltip("A quick twitch around the axis now and then (ears).")]
        Flick,
        [Tooltip("Swings back and forth around the axis (tails).")]
        Wag,
        [Tooltip("Turns around the axis all the time (rotors). Speed = turns per second.")]
        Spin,
        [Tooltip("Floats up and down along the axis (rings, bubbles). Amount in shape units.")]
        Bob,
        [Tooltip("Like wag, for wings and tufts; mirrored pairs flap together.")]
        Flap,
        [Tooltip("Stretches along the axis with a jitter (flames). Amount = stretch fraction.")]
        Flicker,
    }

    /// <summary>How a mesh is painted: a palette slot plus optional zones, all in its own object space.</summary>
    [Serializable]
    public struct PartStyle
    {
        public PaletteSlot color;
        [Tooltip("Blends bottom to top into a second slot (flames).")]
        public bool gradient;
        public PaletteSlot gradientTo;

        [Tooltip("Paints faces that point along the direction (inner ear).")]
        public bool inner;
        public PaletteSlot innerColor;
        public Vector3 innerDirection;
        [Tooltip("-1..1: how directly a face must point that way.")]
        public float innerAt;

        [Tooltip("Paints the far end along the axis (ear tips, wing tips).")]
        public bool tip;
        public PaletteSlot tipColor;
        public Vector3 tipAxis;
        [Tooltip("Distance along the axis where the tip starts, in shape units.")]
        public float tipAt;

        [Range(0f, 2f)] public float glow;
        [Range(0f, 2f)] public float spec;
        public float jiggle;
        [Tooltip("Outline thickness in shape units. 0 = no outline.")]
        public float outline;

        public static PartStyle Plain(PaletteSlot slot) => new PartStyle { color = slot, spec = 0.2f, outline = 0.03f };
    }

    /// <summary>
    /// A reusable rigid piece (ear, tail, rotor, flame) from the parts kit. Baked once and shared by every creature
    /// that uses it; each creature places it and paints it with its own palette.
    /// The shape is authored around its pivot (shape origin = the point it turns around).
    /// </summary>
    [CreateAssetMenu(menuName = "Pocket Isle/Creatures/Part", fileName = "Part")]
    public class CreaturePart : ScriptableObject
    {
        public SdfShape shape;
        [Tooltip("Moves the pivot away from the shape origin, in shape units.")]
        public Vector3 pivot;

        [Header("Idle motion")]
        public PartMotion motion;
        [Tooltip("Local axis it turns around (or moves / stretches along).")]
        public Vector3 motionAxis = Vector3.right;
        [Tooltip("Degrees for flick / wag / flap, units for bob, fraction for flicker.")]
        public float motionAmount = 20f;
        [Tooltip("Cycles per second (spin: turns per second; flick: twitches per ~4 s).")]
        public float motionSpeed = 1f;

        [Header("Paint")]
        public PartStyle style = PartStyle.Plain(PaletteSlot.Base);

        [Header("Bake")]
        [Tooltip("Grid cells across the longest side before simplifying.")]
        [Range(16, 96)] public int resolution = 40;
        public int triangleBudget = 1200;
    }
}
