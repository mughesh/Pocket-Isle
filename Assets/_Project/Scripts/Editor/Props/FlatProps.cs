using System.Collections.Generic;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Nov 6 (Mugesh: "why is the rug this dense? it's just concentric circles"): flat props built directly as extruded
    /// outlines (a flat top fan and a straight wall down to the floor) instead of marching cubes, which needs many
    /// triangles for thin flat shapes and still leaves jagged rims. <see cref="PropBaker"/> asks here first; the recipe's
    /// pieces still give each layer its palette slot (in order). Units are tiles, y up from the floor, like the SDF recipes.
    /// </summary>
    public static class FlatProps
    {
        /// <summary>Segments around a full circle.</summary>
        public const int Segments = 48;

        public struct Layer
        {
            public List<Vector2> outline; // x, z, convex, in order around
            public float top;
        }

        /// <summary>The flat layers of a recipe this class builds, or null (bake it from its SDF pieces).</summary>
        public static List<Layer> LayersFor(string recipe)
        {
            switch (recipe)
            {
                case "LodgeRug":
                    // A round woven rug: weave, terracotta, sage, dot; 3 cm steps.
                    return new List<Layer>
                    {
                        Disc(0.88f, 0.88f, 0f, 0.035f),
                        Disc(0.66f, 0.66f, 0f, 0.065f),
                        Disc(0.42f, 0.42f, 0f, 0.095f),
                        Disc(0.17f, 0.17f, 0f, 0.125f),
                    };
                case "LodgeHearthStones":
                    // A half-round stone apron out from the hearth (the tile's back edge, z = -0.5) with a warm inset.
                    return new List<Layer>
                    {
                        HalfOval(0.86f, 0.62f, -0.5f, 0.07f),
                        HalfOval(0.56f, 0.38f, -0.5f, 0.105f),
                    };
                default:
                    return null;
            }
        }

        static Layer Disc(float rx, float rz, float cz, float top)
        {
            var o = new List<Vector2>();
            for (int i = 0; i < Segments; i++)
            {
                float a = i * Mathf.PI * 2f / Segments;
                o.Add(new Vector2(Mathf.Cos(a) * rx, cz + Mathf.Sin(a) * rz));
            }
            return new Layer { outline = o, top = top };
        }

        static Layer HalfOval(float rx, float rz, float backZ, float top)
        {
            var o = new List<Vector2>();
            int n = Segments / 2;
            for (int i = 0; i <= n; i++)
            {
                float a = i * Mathf.PI / n;
                o.Add(new Vector2(Mathf.Cos(a) * rx, backZ + Mathf.Sin(a) * rz));
            }
            return new Layer { outline = o, top = top };
        }

        /// <summary>
        /// One layer's triangles: the top as a fan (normal up) and a wall down to y = 0 (normals outward, hard edges).
        /// </summary>
        public static void Build(Layer layer, List<Vector3> verts, List<Vector3> normals, List<int> tris)
        {
            var o = layer.outline;
            Vector2 c = Vector2.zero;
            foreach (var p in o) c += p;
            c /= o.Count;
            int n = o.Count;

            // Top fan.
            int centre = verts.Count;
            verts.Add(new Vector3(c.x, layer.top, c.y));
            normals.Add(Vector3.up);
            int ring = verts.Count;
            foreach (var p in o)
            {
                verts.Add(new Vector3(p.x, layer.top, p.y));
                normals.Add(Vector3.up);
            }
            for (int i = 0; i < n; i++)
                Tri(verts, tris, centre, ring + i, ring + (i + 1) % n, Vector3.up);

            // Wall: one quad per edge, its own vertices (a crisp edge).
            for (int i = 0; i < n; i++)
            {
                var a = o[i];
                var b = o[(i + 1) % n];
                var mid = (a + b) * 0.5f;
                var edge = b - a;
                var nrm = new Vector3(edge.y, 0f, -edge.x).normalized;
                if (Vector2.Dot(new Vector2(nrm.x, nrm.z), mid - c) < 0f) nrm = -nrm;
                int s = verts.Count;
                verts.Add(new Vector3(a.x, 0f, a.y));
                verts.Add(new Vector3(b.x, 0f, b.y));
                verts.Add(new Vector3(b.x, layer.top, b.y));
                verts.Add(new Vector3(a.x, layer.top, a.y));
                for (int k = 0; k < 4; k++) normals.Add(nrm);
                Tri(verts, tris, s, s + 1, s + 2, nrm);
                Tri(verts, tris, s, s + 2, s + 3, nrm);
            }
        }

        /// <summary>Adds a triangle wound so that its front faces <paramref name="facing"/> (Unity: clockwise = front).</summary>
        static void Tri(List<Vector3> v, List<int> tris, int a, int b, int c, Vector3 facing)
        {
            var n = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
            tris.Add(a);
            if (Vector3.Dot(n, facing) >= 0f)
            {
                tris.Add(b);
                tris.Add(c);
            }
            else
            {
                tris.Add(c);
                tris.Add(b);
            }
        }
    }
}
