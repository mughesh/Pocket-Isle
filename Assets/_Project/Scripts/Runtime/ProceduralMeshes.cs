using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Small primitives built in code, matching the three.js geometries the prototype uses.
    /// Triangles are wound from a known outward direction, so callers never think about winding.
    /// </summary>
    public static class ProceduralMeshes
    {
        sealed class Builder
        {
            public readonly List<Vector3> Vertices = new List<Vector3>();
            public readonly List<Vector3> Normals = new List<Vector3>();
            public readonly List<Vector2> Uvs = new List<Vector2>();
            public readonly List<int> Triangles = new List<int>();

            public int Add(Vector3 position, Vector3 normal, Vector2 uv = default)
            {
                Vertices.Add(position);
                Normals.Add(normal);
                Uvs.Add(uv);
                return Vertices.Count - 1;
            }

            /// <summary>Adds a triangle whose front face points along <paramref name="outward"/>.</summary>
            public void Triangle(int a, int b, int c, Vector3 outward)
            {
                Vector3 pa = Vertices[a], pb = Vertices[b], pc = Vertices[c];
                Vector3 n = Vector3.Cross(pb - pa, pc - pa);
                if (n.sqrMagnitude < 1e-14f) return; // degenerate (cone apex)
                if (Vector3.Dot(n, outward) < 0f) (b, c) = (c, b);
                Triangles.Add(a); Triangles.Add(b); Triangles.Add(c);
            }

            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
                mesh.SetVertices(Vertices);
                mesh.SetNormals(Normals);
                mesh.SetUVs(0, Uvs);
                mesh.SetTriangles(Triangles, 0);
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        /// <summary>Axis-aligned box with flat faces.</summary>
        public static Mesh Box(Vector3 size, Vector3 center, string name = "Box")
        {
            var b = new Builder();
            Vector3 half = size * 0.5f;
            Vector3[] axes = { Vector3.right, Vector3.up, Vector3.forward };
            for (int axis = 0; axis < 3; axis++)
            {
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    Vector3 n = axes[axis] * sign;
                    Vector3 u = axes[(axis + 1) % 3], v = axes[(axis + 2) % 3];
                    Vector3 c = center + Vector3.Scale(n, half);
                    Vector3 du = Vector3.Scale(u, half), dv = Vector3.Scale(v, half);
                    int i0 = b.Add(c - du - dv, n, new Vector2(0, 0));
                    int i1 = b.Add(c + du - dv, n, new Vector2(1, 0));
                    int i2 = b.Add(c + du + dv, n, new Vector2(1, 1));
                    int i3 = b.Add(c - du + dv, n, new Vector2(0, 1));
                    b.Triangle(i0, i1, i2, n);
                    b.Triangle(i0, i2, i3, n);
                }
            }
            return b.ToMesh(name);
        }

        /// <summary>
        /// Like THREE.CylinderGeometry (smooth sides, flat caps), centred at y = <paramref name="centerY"/>.
        /// A top radius of 0 makes a cone.
        /// </summary>
        public static Mesh Cylinder(float radiusTop, float radiusBottom, float height, int radialSegments,
            float centerY, string name = "Cylinder")
        {
            var b = new Builder();
            float halfH = height * 0.5f;
            float slope = (radiusBottom - radiusTop) / height;

            var top = new int[radialSegments + 1];
            var bottom = new int[radialSegments + 1];
            for (int x = 0; x <= radialSegments; x++)
            {
                float u = (float)x / radialSegments;
                float theta = u * Mathf.PI * 2f;
                float sin = Mathf.Sin(theta), cos = Mathf.Cos(theta);
                Vector3 n = new Vector3(sin, slope, cos).normalized;
                top[x] = b.Add(new Vector3(radiusTop * sin, centerY + halfH, radiusTop * cos), n, new Vector2(u, 1));
                bottom[x] = b.Add(new Vector3(radiusBottom * sin, centerY - halfH, radiusBottom * cos), n, new Vector2(u, 0));
            }
            for (int x = 0; x < radialSegments; x++)
            {
                float mid = (x + 0.5f) / radialSegments * Mathf.PI * 2f;
                Vector3 outward = new Vector3(Mathf.Sin(mid), 0f, Mathf.Cos(mid));
                b.Triangle(top[x], bottom[x], top[x + 1], outward);
                b.Triangle(bottom[x], bottom[x + 1], top[x + 1], outward);
            }

            if (radiusTop > 0f) Cap(b, radiusTop, centerY + halfH, radialSegments, Vector3.up);
            if (radiusBottom > 0f) Cap(b, radiusBottom, centerY - halfH, radialSegments, Vector3.down);
            return b.ToMesh(name);
        }

        static void Cap(Builder b, float radius, float y, int segments, Vector3 n)
        {
            int center = b.Add(new Vector3(0f, y, 0f), n, new Vector2(0.5f, 0.5f));
            int prev = -1;
            for (int x = 0; x <= segments; x++)
            {
                float theta = (float)x / segments * Mathf.PI * 2f;
                float sin = Mathf.Sin(theta), cos = Mathf.Cos(theta);
                int i = b.Add(new Vector3(radius * sin, y, radius * cos), n, new Vector2(sin * 0.5f + 0.5f, cos * 0.5f + 0.5f));
                if (x > 0) b.Triangle(center, prev, i, n);
                prev = i;
            }
        }

        /// <summary>Flat-shaded icosahedron, like THREE.IcosahedronGeometry(radius, 0).</summary>
        public static Mesh Icosahedron(float radius, string name = "Icosahedron")
        {
            float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
            Vector3[] p =
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1),
            };
            int[] f =
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
                1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
                4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
            };
            var b = new Builder();
            for (int i = 0; i < f.Length; i += 3)
            {
                Vector3 a = p[f[i]].normalized * radius, c1 = p[f[i + 1]].normalized * radius, c2 = p[f[i + 2]].normalized * radius;
                Vector3 n = Vector3.Cross(c1 - a, c2 - a).normalized;
                Vector3 outward = (a + c1 + c2) / 3f;
                if (Vector3.Dot(n, outward) < 0f) n = -n;
                b.Triangle(b.Add(a, n), b.Add(c1, n), b.Add(c2, n), outward);
            }
            return b.ToMesh(name);
        }

        /// <summary>
        /// Grass blade: a 1 x 1 strip with 4 height segments, base at y = 0, tapering to 12% width
        /// at the tip. uv.y runs 0 at the base to 1 at the tip. Drawn double-sided.
        /// </summary>
        public static Mesh GrassBlade(string name = "GrassBlade")
        {
            const int segments = 4;
            var b = new Builder();
            for (int iy = 0; iy <= segments; iy++)
            {
                float y = (float)iy / segments;
                float halfW = 0.5f * (1f - y * 0.88f);
                b.Add(new Vector3(-halfW, y, 0f), Vector3.back, new Vector2(0f, y));
                b.Add(new Vector3(halfW, y, 0f), Vector3.back, new Vector2(1f, y));
            }
            for (int iy = 0; iy < segments; iy++)
            {
                int i = iy * 2;
                b.Triangle(i, i + 1, i + 3, Vector3.back);
                b.Triangle(i, i + 3, i + 2, Vector3.back);
            }
            return b.ToMesh(name);
        }

        /// <summary>
        /// Copy of a mesh with normals averaged across vertices that share a position.
        /// Use it for the outline hull of flat-shaded meshes so the hull has no cracks.
        /// </summary>
        public static Mesh WithSmoothedNormals(Mesh source)
        {
            var vertices = source.vertices;
            var normals = source.normals;
            var sums = new Dictionary<Vector3Int, Vector3>();
            Vector3Int Key(Vector3 v) => Vector3Int.RoundToInt(v * 100000f);

            for (int i = 0; i < vertices.Length; i++)
            {
                var k = Key(vertices[i]);
                sums.TryGetValue(k, out var sum);
                sums[k] = sum + normals[i];
            }
            var smoothed = new Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
                smoothed[i] = sums[Key(vertices[i])].normalized;

            var mesh = Object.Instantiate(source);
            mesh.name = source.name + " (outline)";
            mesh.hideFlags = HideFlags.DontSave;
            mesh.normals = smoothed;
            return mesh;
        }
    }
}
