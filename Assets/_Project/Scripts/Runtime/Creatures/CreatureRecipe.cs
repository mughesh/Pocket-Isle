using System;
using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    [Serializable]
    public struct CreaturePalette
    {
        public Color baseColor;
        public Color belly;
        [Tooltip("Spots, tips, masks.")]
        public Color accent;
        public Color socks;
        [Tooltip("Flames and other glowing bits.")]
        public Color glow;
        [Tooltip("One more colour for a signature part (collar, rotor).")]
        public Color extra;
        [Tooltip("Nose, mouth and other face marks.")]
        public Color face;
        public Color iris;

        public Color this[PaletteSlot slot] => slot switch
        {
            PaletteSlot.Belly => belly,
            PaletteSlot.Accent => accent,
            PaletteSlot.Socks => socks,
            PaletteSlot.Glow => glow,
            PaletteSlot.Extra => extra,
            PaletteSlot.Face => face,
            _ => baseColor,
        };

        /// <summary>Same palette with hue / saturation / value shifted (evolutions).</summary>
        public CreaturePalette Shifted(float hue, float saturation, float value)
        {
            Color S(Color c)
            {
                Color.RGBToHSV(c, out float h, out float s, out float v);
                return Color.HSVToRGB(Mathf.Repeat(h + hue, 1f), Mathf.Clamp01(s * saturation), Mathf.Clamp01(v * value));
            }
            return new CreaturePalette
            {
                baseColor = S(baseColor), belly = S(belly), accent = S(accent), socks = S(socks),
                glow = S(glow), extra = S(extra), face = face, iris = iris,
            };
        }
    }

    /// <summary>A round painted patch on the body (eye mask, back spot, muzzle), in body shape units.</summary>
    [Serializable]
    public struct PatternPatch
    {
        public Vector3 center;
        public float radius;
        public PaletteSlot color;
        public bool mirrorX;
    }

    /// <summary>Pattern zones painted on the body in object space (port of the prototype toon masks).</summary>
    [Serializable]
    public struct BodyZones
    {
        [Tooltip("Faces pointing along the direction, below a height.")]
        public bool belly;
        public Vector3 bellyDirection;
        public float bellyAt;
        public float bellyMaxY;

        [Tooltip("Everything below a height.")]
        public bool socks;
        public float socksY;

        [Tooltip("Everything past a distance along an axis.")]
        public bool tips;
        public Vector3 tipAxis;
        public float tipAt;

        [Tooltip("Scattered spots on the back, in the accent colour.")]
        public bool spots;
        public float spotScale;

        [Tooltip("Up to 6 round patches (mirrored ones count twice). Later ones paint over earlier ones.")]
        public List<PatternPatch> patches;
    }

    /// <summary>A kit part placed on a creature, in body shape units.</summary>
    [Serializable]
    public struct PlacedPart
    {
        public CreaturePart part;
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
        [Tooltip("Adds a mirrored copy on the other side.")]
        public bool mirror;
        [Tooltip("Index of another placed part to ride on (the flame on a tail). -1 = the body.")]
        public int parent;
    }

    public enum DecalKind
    {
        [Tooltip("Flat oval (nose, cheeks, nostrils, teeth).")]
        Disc,
        [Tooltip("Half-ring smile.")]
        Smile,
    }

    /// <summary>A face mark placed by a ray from the front onto the body, like surface() in the prototype.</summary>
    [Serializable]
    public struct FaceDecal
    {
        public DecalKind kind;
        [Tooltip("x, y where the ray starts in front of the face (shape units).")]
        public Vector2 at;
        [Tooltip("Width, height (smile: x = size).")]
        public Vector2 size;
        public PaletteSlot color;
        public bool mirrorX;
    }

    [Serializable]
    public struct FaceSettings
    {
        [Tooltip("Eye radius in shape units.")]
        public float eyeSize;
        [Tooltip("Distance of each eye from the middle.")]
        public float eyeSpacing;
        public float eyeHeight;
        [Tooltip("Height / width of the eye.")]
        public float eyeStretch;
        public List<FaceDecal> decals;
    }

    [Serializable]
    public struct IdleStyle
    {
        public float breathe;
        [Tooltip("Breaths per ~2.4 s (2.6 = the prototype).")]
        public float breatheSpeed;
        public float jiggle;
        [Tooltip("Floats above the ground by this much (metres at zoom 1). For floaty creatures.")]
        public float hover;
        [Tooltip("Up and down float, metres at zoom 1.")]
        public float bob;
        [Tooltip("Side to side rock, degrees.")]
        public float sway;
    }

    /// <summary>
    /// A creature is data, not code: a body shape, kit parts placed on it, face, palette, pattern zones and idle style.
    /// The baker turns it into meshes and a prefab. An evolution sets <see cref="baseRecipe"/> and only lists
    /// what changes (size, extra parts, palette).
    /// </summary>
    [CreateAssetMenu(menuName = "Pocket Isle/Creatures/Recipe", fileName = "Recipe")]
    public class CreatureRecipe : ScriptableObject
    {
        [Tooltip("Evolutions: start from this recipe and apply the modifiers below. Empty = a base recipe.")]
        public CreatureRecipe baseRecipe;

        [Header("Body")]
        public SdfShape body;
        [Tooltip("Height of the body in metres at zoom 1 (a tile is 0.05).")]
        public float height = 0.045f;
        public BodyZones zones;
        public List<PlacedPart> parts = new List<PlacedPart>();

        [Header("Face")]
        public FaceSettings face;

        [Header("Look")]
        public CreaturePalette palette;
        [Range(0f, 2f)] public float spec = 0.2f;
        public float outline = 0.035f;
        public IdleStyle idle;

        [Header("Evolution modifiers (used when Base Recipe is set)")]
        public float heightScale = 1.25f;
        public List<PlacedPart> extraParts = new List<PlacedPart>();
        [Tooltip("Use this recipe's palette instead of shifting the base one.")]
        public bool overridePalette;
        [Range(-0.5f, 0.5f)] public float hueShift;
        public float saturationScale = 1f;
        public float valueScale = 1f;

        [Header("Bake")]
        [Range(32, 128)] public int resolution = 88;
        public int triangleBudget = 6000;
        [Tooltip("Written by the baker.")]
        public GameObject bakedPrefab;

        /// <summary>The recipe with any base recipe and modifiers applied.</summary>
        public Resolved Resolve()
        {
            if (baseRecipe == null || baseRecipe == this) return new Resolved(this);
            var r = baseRecipe.Resolve();
            r.name = name;
            r.height *= heightScale;
            r.parts = new List<PlacedPart>(r.parts);
            int offset = r.parts.Count;
            foreach (var p in extraParts)
            {
                var q = p;
                if (q.parent >= 0) q.parent += offset; // extra parts index among themselves
                r.parts.Add(q);
            }
            r.palette = overridePalette ? palette : r.palette.Shifted(hueShift, saturationScale, valueScale);
            return r;
        }

        /// <summary>Everything the baker needs, after evolution modifiers.</summary>
        public sealed class Resolved
        {
            public string name;
            public SdfShape body;
            public float height;
            public BodyZones zones;
            public List<PlacedPart> parts;
            public FaceSettings face;
            public CreaturePalette palette;
            public float spec, outline;
            public IdleStyle idle;
            public int resolution, triangleBudget;

            public Resolved(CreatureRecipe r)
            {
                name = r.name;
                body = r.body;
                height = r.height;
                zones = r.zones;
                parts = r.parts ?? new List<PlacedPart>();
                face = r.face;
                palette = r.palette;
                spec = r.spec;
                outline = r.outline;
                idle = r.idle;
                resolution = r.resolution;
                triangleBudget = r.triangleBudget;
            }
        }
    }
}
