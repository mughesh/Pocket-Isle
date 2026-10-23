using System;
using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    public enum SdfKind
    {
        [Tooltip("a = centre, size.x = radius.")]
        Sphere,
        [Tooltip("a = centre, size = radii, rotation tilts it.")]
        Ellipsoid,
        [Tooltip("Capsule that tapers: a to b, size.x = radius at a, size.y = radius at b.")]
        RoundCone,
        [Tooltip("a = centre, size.x = ring radius, size.y = tube radius; ring lies in the local XZ plane, rotation tilts it.")]
        Torus,
        [Tooltip("Half space: a = point on the plane, b = outward normal. Use with Intersect to cut (flat feet).")]
        Plane,
    }

    public enum SdfBlend
    {
        [Tooltip("Melts into what came before (smooth union, k = melt radius).")]
        Union,
        [Tooltip("Carves itself out of what came before.")]
        Subtract,
        [Tooltip("Keeps only the overlap (use with Plane to cut).")]
        Intersect,
    }

    /// <summary>One primitive of a shape, in shape units (a creature is about 1.5 units tall).</summary>
    [Serializable]
    public struct SdfPrimitive
    {
        public SdfKind kind;
        [Tooltip("How it combines with the primitives above it. Ignored on the first one.")]
        public SdfBlend blend;
        [Tooltip("Smoothing radius. 0 = hard edge.")]
        [Min(0f)] public float k;
        [Tooltip("Also adds a copy mirrored across X (pairs of legs, ears, cheeks).")]
        public bool mirrorX;
        public Vector3 a;
        public Vector3 b;
        public Vector3 size;
        [Tooltip("Euler degrees. Ellipsoid and torus only.")]
        public Vector3 rotation;
        [Tooltip("Lumpy surface (wool, clouds). Height in units; 0 = smooth.")]
        public float bumps;
        [Tooltip("Lumps per unit (about 20 for wool).")]
        public float bumpFrequency;
    }

    /// <summary>A signed distance field recipe: primitives melted together in order. Baked to a mesh by the creature baker.</summary>
    [CreateAssetMenu(menuName = "Pocket Isle/Creatures/SDF Shape", fileName = "Shape")]
    public class SdfShape : ScriptableObject
    {
        public List<SdfPrimitive> primitives = new List<SdfPrimitive>();
    }

    /// <summary>
    /// Fast evaluation of an <see cref="SdfShape"/> (rotations inverted once). Distances are negative inside.
    /// Ported from sdSphere / sdEll / sdRC / smin / smax in Docs/Reference/creature-diorama-v2.html.
    /// </summary>
    public sealed class SdfEvaluator
    {
        struct Prim
        {
            public SdfPrimitive p;
            public Quaternion inverse;
        }

        readonly Prim[] _prims;

        public SdfEvaluator(IList<SdfPrimitive> primitives)
        {
            _prims = new Prim[primitives.Count];
            for (int i = 0; i < primitives.Count; i++)
                _prims[i] = new Prim { p = primitives[i], inverse = Quaternion.Inverse(Quaternion.Euler(primitives[i].rotation)) };
        }

        public SdfEvaluator(SdfShape shape) : this(shape != null ? shape.primitives : new List<SdfPrimitive>()) { }

        public bool IsEmpty => _prims.Length == 0;

        public float Evaluate(Vector3 p)
        {
            if (_prims.Length == 0) return 1f;
            float d = 0f;
            for (int i = 0; i < _prims.Length; i++)
            {
                ref var pr = ref _prims[i];
                float di = Primitive(ref pr, p);
                if (pr.p.mirrorX) di = SMin(di, Primitive(ref pr, new Vector3(-p.x, p.y, p.z)), pr.p.k);
                if (i == 0) { d = di; continue; }
                switch (pr.p.blend)
                {
                    case SdfBlend.Union: d = SMin(d, di, pr.p.k); break;
                    case SdfBlend.Subtract: d = SMax(d, -di, pr.p.k); break;
                    default: d = SMax(d, di, pr.p.k); break;
                }
            }
            return d;
        }

        /// <summary>Outward surface normal (normalised gradient).</summary>
        public Vector3 Normal(Vector3 p, float e = 0.002f)
        {
            var g = new Vector3(
                Evaluate(new Vector3(p.x + e, p.y, p.z)) - Evaluate(new Vector3(p.x - e, p.y, p.z)),
                Evaluate(new Vector3(p.x, p.y + e, p.z)) - Evaluate(new Vector3(p.x, p.y - e, p.z)),
                Evaluate(new Vector3(p.x, p.y, p.z + e)) - Evaluate(new Vector3(p.x, p.y, p.z - e)));
            return g.sqrMagnitude > 1e-20f ? g.normalized : Vector3.up;
        }

        /// <summary>
        /// Marches from <paramref name="from"/> along <paramref name="dir"/> to where it first enters the shape
        /// (surface() in the prototype). False if it never does.
        /// </summary>
        public bool Surface(Vector3 from, Vector3 dir, out Vector3 point, out Vector3 normal)
        {
            dir = dir.normalized;
            point = from;
            for (int i = 0; i < 2000; i++)
            {
                float d = Evaluate(point);
                if (d < 0.0005f)
                {
                    normal = Normal(point);
                    return true;
                }
                point += dir * Mathf.Max(0.002f, d * 0.8f);
            }
            normal = -dir;
            return false;
        }

        /// <summary>Conservative bounds of everything that can be inside (subtractions and cuts ignored).</summary>
        public Bounds Bounds
        {
            get
            {
                bool any = false;
                var b = new Bounds();
                foreach (var pr in _prims)
                {
                    if (pr.p.kind == SdfKind.Plane || pr.p.blend != SdfBlend.Union && any) continue;
                    var pb = PrimitiveBounds(pr.p);
                    pb.Expand(2f * (Mathf.Abs(pr.p.bumps) + pr.p.k * 0.25f));
                    if (pr.p.mirrorX) pb.Encapsulate(new Bounds(new Vector3(-pb.center.x, pb.center.y, pb.center.z), pb.size));
                    if (any) b.Encapsulate(pb);
                    else b = pb;
                    any = true;
                }
                return b;
            }
        }

        static Bounds PrimitiveBounds(in SdfPrimitive p)
        {
            switch (p.kind)
            {
                case SdfKind.Sphere: return new Bounds(p.a, Vector3.one * (2f * p.size.x));
                case SdfKind.Ellipsoid:
                {
                    float r = Mathf.Max(p.size.x, Mathf.Max(p.size.y, p.size.z));
                    return new Bounds(p.a, Vector3.one * (2f * r));
                }
                case SdfKind.RoundCone:
                {
                    var b = new Bounds(p.a, Vector3.one * (2f * p.size.x));
                    b.Encapsulate(new Bounds(p.b, Vector3.one * (2f * p.size.y)));
                    return b;
                }
                case SdfKind.Torus: return new Bounds(p.a, Vector3.one * (2f * (p.size.x + p.size.y)));
                default: return new Bounds(p.a, Vector3.zero);
            }
        }

        static float Primitive(ref Prim pr, Vector3 p)
        {
            ref var s = ref pr.p;
            float d;
            switch (s.kind)
            {
                case SdfKind.Sphere:
                    d = (p - s.a).magnitude - s.size.x;
                    break;
                case SdfKind.Ellipsoid:
                {
                    Vector3 q = pr.inverse * (p - s.a);
                    Vector3 r = new Vector3(Mathf.Max(s.size.x, 1e-4f), Mathf.Max(s.size.y, 1e-4f), Mathf.Max(s.size.z, 1e-4f));
                    float k0 = new Vector3(q.x / r.x, q.y / r.y, q.z / r.z).magnitude;
                    float k1 = new Vector3(q.x / (r.x * r.x), q.y / (r.y * r.y), q.z / (r.z * r.z)).magnitude;
                    d = k1 < 1e-6f ? -Mathf.Min(r.x, Mathf.Min(r.y, r.z)) : k0 * (k0 - 1f) / k1;
                    break;
                }
                case SdfKind.RoundCone:
                {
                    Vector3 ba = s.b - s.a, pa = p - s.a;
                    float t = Mathf.Clamp01(Vector3.Dot(pa, ba) / Mathf.Max(ba.sqrMagnitude, 1e-8f));
                    d = (pa - ba * t).magnitude - Mathf.Lerp(s.size.x, s.size.y, t);
                    break;
                }
                case SdfKind.Torus:
                {
                    Vector3 q = pr.inverse * (p - s.a);
                    float ring = new Vector2(q.x, q.z).magnitude - s.size.x;
                    d = new Vector2(ring, q.y).magnitude - s.size.y;
                    break;
                }
                default:
                    d = Vector3.Dot(p - s.a, s.b.sqrMagnitude > 1e-8f ? s.b.normalized : Vector3.up);
                    break;
            }
            if (s.bumps != 0f)
            {
                float f = s.bumpFrequency > 0f ? s.bumpFrequency : 20f;
                d -= s.bumps * Mathf.Sin(p.x * f) * Mathf.Sin(p.y * f * 1.13f + 1.7f) * Mathf.Sin(p.z * f * 0.91f + 0.5f);
            }
            return d;
        }

        public static float SMin(float a, float b, float k)
        {
            if (k <= 0f) return Mathf.Min(a, b);
            float h = Mathf.Max(k - Mathf.Abs(a - b), 0f) / k;
            return Mathf.Min(a, b) - h * h * k * 0.25f;
        }

        public static float SMax(float a, float b, float k) => -SMin(-a, -b, k);
    }
}
