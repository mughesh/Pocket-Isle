using System;
using UnityEngine;

namespace Diorama
{
    /// <summary>Island colours for one restoration state of a realm.</summary>
    [Serializable]
    public struct ThemeColors
    {
        [Header("Tile caps")]
        public Color grass;
        public Color tallGrass;
        public Color path;
        public Color water;
        public Color hotSpring;

        [Header("Grass blades (base to tip)")]
        public Color shortBase;
        public Color shortTip;
        public Color tallBase;
        public Color tallTip;

        [Header("Rocky base")]
        public Color dirt;
        public Color underside;

        [Header("Trees and rocks")]
        public Color trunk;
        public Color leavesLow;
        public Color leavesHigh;
        public Color rock;
        [Tooltip("Self-lit amount on the cone tree leaves.")]
        [Range(0f, 1f)] public float leafGlow;

        [Tooltip("Multiplies every outline colour (outline = colour x 0.28 in linear, then this).")]
        public Color outlineTint;

        public static ThemeColors Lerp(in ThemeColors a, in ThemeColors b, float t) => new ThemeColors
        {
            grass = Color.Lerp(a.grass, b.grass, t),
            tallGrass = Color.Lerp(a.tallGrass, b.tallGrass, t),
            path = Color.Lerp(a.path, b.path, t),
            water = Color.Lerp(a.water, b.water, t),
            hotSpring = Color.Lerp(a.hotSpring, b.hotSpring, t),
            shortBase = Color.Lerp(a.shortBase, b.shortBase, t),
            shortTip = Color.Lerp(a.shortTip, b.shortTip, t),
            tallBase = Color.Lerp(a.tallBase, b.tallBase, t),
            tallTip = Color.Lerp(a.tallTip, b.tallTip, t),
            dirt = Color.Lerp(a.dirt, b.dirt, t),
            underside = Color.Lerp(a.underside, b.underside, t),
            trunk = Color.Lerp(a.trunk, b.trunk, t),
            leavesLow = Color.Lerp(a.leavesLow, b.leavesLow, t),
            leavesHigh = Color.Lerp(a.leavesHigh, b.leavesHigh, t),
            rock = Color.Lerp(a.rock, b.rock, t),
            leafGlow = Mathf.Lerp(a.leafGlow, b.leafGlow, t),
            outlineTint = Color.Lerp(a.outlineTint, b.outlineTint, t),
        };
    }

    /// <summary>
    /// Per-realm island colours, restored and unrestored. The global restoration value (<see cref="RealmState"/>,
    /// shader global _RealmRestored) blends between them; props read it in their shader (colours and glows).
    /// </summary>
    [CreateAssetMenu(menuName = "Pocket Isle/Props/Realm Theme", fileName = "Theme")]
    public class RealmTheme : ScriptableObject
    {
        public ThemeColors restored;
        public ThemeColors unrestored;

        [Tooltip("Optional. Drawn on tree (F) and border (X) tiles instead of the cone trees.")]
        public PropRecipe treeProp;

        [Tooltip("Trees on the border tiles of the front edge (the side facing the player). Off = an open front.")]
        public bool frontBorderTrees = true;

        [Tooltip("Restoration when a scene starts (1 = restored).")]
        [Range(0f, 1f)] public float startRestored = 1f;

        [Header("Drift (RealmDrift)")]
        [Tooltip("Scales the rising embers (Cinderwood 1, the calm Still Isle 0).")]
        [Range(0f, 1f)] public float embers = 1f;
        [Tooltip("Scales the falling leaves.")]
        [Range(0f, 1f)] public float leaves = 1f;

        [Header("Light (interiors)")]
        [Tooltip("While this map shows, the main light takes this colour and intensity (interiors: warm hearth light).")]
        public bool overrideLight;
        public Color lightColor = Color.white;
        [Min(0f)] public float lightIntensity = 1f;

        public ThemeColors Evaluate(float restoredAmount) => ThemeColors.Lerp(unrestored, restored, Mathf.Clamp01(restoredAmount));
    }
}
