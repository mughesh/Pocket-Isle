using System;
using System.Collections.Generic;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Marching cubes over a sampled signed distance field (negative inside), the C# side of sculpt() in the
    /// prototype. Vertices are shared per grid edge, so the result is already welded. Triangles face outward
    /// (checked against the field gradient). Tables: Paul Bourke, "Polygonising a scalar field".
    /// </summary>
    public static class MarchingCubes
    {
        public struct Result
        {
            public List<Vector3> vertices;
            public List<int> triangles;
        }

        /// <summary>Samples <paramref name="sdf"/> over <paramref name="bounds"/> with cubic cells of <paramref name="cell"/> and builds the surface.</summary>
        public static Result Build(Func<Vector3, float> sdf, Bounds bounds, float cell)
        {
            Vector3 min = bounds.min - Vector3.one * (cell * 2f);
            Vector3 size = bounds.size + Vector3.one * (cell * 4f);
            int nx = Mathf.CeilToInt(size.x / cell) + 1, ny = Mathf.CeilToInt(size.y / cell) + 1, nz = Mathf.CeilToInt(size.z / cell) + 1;
            var field = new float[nx * ny * nz];
            for (int z = 0; z < nz; z++)
                for (int y = 0; y < ny; y++)
                    for (int x = 0; x < nx; x++)
                    {
                        float v = sdf(min + new Vector3(x, y, z) * cell);
                        if (Mathf.Abs(v) < 1e-7f) v = 1e-7f; // keeps vertices off grid corners (no zero-area triangles)
                        field[x + nx * (y + ny * z)] = v;
                    }

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var edgeVertex = new Dictionary<long, int>();
            var corner = new float[8];
            var cornerPos = new Vector3Int[8];
            var edgeIds = new int[12];

            for (int z = 0; z < nz - 1; z++)
                for (int y = 0; y < ny - 1; y++)
                    for (int x = 0; x < nx - 1; x++)
                    {
                        int cube = 0;
                        for (int i = 0; i < 8; i++)
                        {
                            var o = CornerOffsets[i];
                            cornerPos[i] = new Vector3Int(x + o.x, y + o.y, z + o.z);
                            corner[i] = field[cornerPos[i].x + nx * (cornerPos[i].y + ny * cornerPos[i].z)];
                            if (corner[i] < 0f) cube |= 1 << i;
                        }
                        var tris = TriTable[cube];
                        if (tris.Length == 0) continue;

                        for (int t = 0; t < tris.Length; t++)
                        {
                            int e = tris[t];
                            int a = EdgeCorners[e, 0], b = EdgeCorners[e, 1];
                            Vector3Int pa = cornerPos[a], pb = cornerPos[b];
                            if (Compare(pa, pb) > 0) { (pa, pb) = (pb, pa); (a, b) = (b, a); }
                            long key = (((long)(pa.x + nx * (pa.y + ny * pa.z))) * 3) + Axis(pa, pb);
                            if (!edgeVertex.TryGetValue(key, out int id))
                            {
                                float va = corner[a], vb = corner[b];
                                // Kept off the corners so neighbouring edge vertices never coincide (no zero-area holes).
                                float u = Mathf.Clamp(va / (va - vb), 0.01f, 0.99f);
                                id = vertices.Count;
                                vertices.Add(min + Vector3.Lerp(pa, pb, u) * cell);
                                edgeVertex[key] = id;
                            }
                            edgeIds[t % 3] = id;
                            if (t % 3 == 2) AddTriangle(triangles, vertices, edgeIds[0], edgeIds[1], edgeIds[2], sdf);
                        }
                    }
            return new Result { vertices = vertices, triangles = triangles };
        }

        static void AddTriangle(List<int> tris, List<Vector3> verts, int a, int b, int c, Func<Vector3, float> sdf)
        {
            if (a == b || b == c || a == c) return;
            Vector3 pa = verts[a], pb = verts[b], pc = verts[c];
            Vector3 n = Vector3.Cross(pb - pa, pc - pa);
            Vector3 m = (pa + pb + pc) / 3f;
            const float e = 0.002f;
            var g = new Vector3(sdf(m + Vector3.right * e) - sdf(m - Vector3.right * e),
                                sdf(m + Vector3.up * e) - sdf(m - Vector3.up * e),
                                sdf(m + Vector3.forward * e) - sdf(m - Vector3.forward * e));
            if (Vector3.Dot(n, g) < 0f) (b, c) = (c, b);
            tris.Add(a); tris.Add(b); tris.Add(c);
        }

        static int Compare(Vector3Int a, Vector3Int b)
        {
            if (a.z != b.z) return a.z.CompareTo(b.z);
            if (a.y != b.y) return a.y.CompareTo(b.y);
            return a.x.CompareTo(b.x);
        }

        static int Axis(Vector3Int a, Vector3Int b) => a.x != b.x ? 0 : a.y != b.y ? 1 : 2;

        // Corner i of a cell, Bourke's numbering.
        static readonly Vector3Int[] CornerOffsets =
        {
            new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(1, 1, 0), new Vector3Int(0, 1, 0),
            new Vector3Int(0, 0, 1), new Vector3Int(1, 0, 1), new Vector3Int(1, 1, 1), new Vector3Int(0, 1, 1),
        };

        static readonly int[,] EdgeCorners =
        {
            { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 }, { 4, 5 }, { 5, 6 }, { 6, 7 }, { 7, 4 }, { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 },
        };

        static readonly int[][] TriTable =
        {
            new int[] { },
            new[] { 0, 8, 3 },
            new[] { 0, 1, 9 },
            new[] { 1, 8, 3, 9, 8, 1 },
            new[] { 1, 2, 10 },
            new[] { 0, 8, 3, 1, 2, 10 },
            new[] { 9, 2, 10, 0, 2, 9 },
            new[] { 2, 8, 3, 2, 10, 8, 10, 9, 8 },
            new[] { 3, 11, 2 },
            new[] { 0, 11, 2, 8, 11, 0 },
            new[] { 1, 9, 0, 2, 3, 11 },
            new[] { 1, 11, 2, 1, 9, 11, 9, 8, 11 },
            new[] { 3, 10, 1, 11, 10, 3 },
            new[] { 0, 10, 1, 0, 8, 10, 8, 11, 10 },
            new[] { 3, 9, 0, 3, 11, 9, 11, 10, 9 },
            new[] { 9, 8, 10, 10, 8, 11 },
            new[] { 4, 7, 8 },
            new[] { 4, 3, 0, 7, 3, 4 },
            new[] { 0, 1, 9, 8, 4, 7 },
            new[] { 4, 1, 9, 4, 7, 1, 7, 3, 1 },
            new[] { 1, 2, 10, 8, 4, 7 },
            new[] { 3, 4, 7, 3, 0, 4, 1, 2, 10 },
            new[] { 9, 2, 10, 9, 0, 2, 8, 4, 7 },
            new[] { 2, 10, 9, 2, 9, 7, 2, 7, 3, 7, 9, 4 },
            new[] { 8, 4, 7, 3, 11, 2 },
            new[] { 11, 4, 7, 11, 2, 4, 2, 0, 4 },
            new[] { 9, 0, 1, 8, 4, 7, 2, 3, 11 },
            new[] { 4, 7, 11, 9, 4, 11, 9, 11, 2, 9, 2, 1 },
            new[] { 3, 10, 1, 3, 11, 10, 7, 8, 4 },
            new[] { 1, 11, 10, 1, 4, 11, 1, 0, 4, 7, 11, 4 },
            new[] { 4, 7, 8, 9, 0, 11, 9, 11, 10, 11, 0, 3 },
            new[] { 4, 7, 11, 4, 11, 9, 9, 11, 10 },
            new[] { 9, 5, 4 },
            new[] { 9, 5, 4, 0, 8, 3 },
            new[] { 0, 5, 4, 1, 5, 0 },
            new[] { 8, 5, 4, 8, 3, 5, 3, 1, 5 },
            new[] { 1, 2, 10, 9, 5, 4 },
            new[] { 3, 0, 8, 1, 2, 10, 4, 9, 5 },
            new[] { 5, 2, 10, 5, 4, 2, 4, 0, 2 },
            new[] { 2, 10, 5, 3, 2, 5, 3, 5, 4, 3, 4, 8 },
            new[] { 9, 5, 4, 2, 3, 11 },
            new[] { 0, 11, 2, 0, 8, 11, 4, 9, 5 },
            new[] { 0, 5, 4, 0, 1, 5, 2, 3, 11 },
            new[] { 2, 1, 5, 2, 5, 8, 2, 8, 11, 4, 8, 5 },
            new[] { 10, 3, 11, 10, 1, 3, 9, 5, 4 },
            new[] { 4, 9, 5, 0, 8, 1, 8, 10, 1, 8, 11, 10 },
            new[] { 5, 4, 0, 5, 0, 11, 5, 11, 10, 11, 0, 3 },
            new[] { 5, 4, 8, 5, 8, 10, 10, 8, 11 },
            new[] { 9, 7, 8, 5, 7, 9 },
            new[] { 9, 3, 0, 9, 5, 3, 5, 7, 3 },
            new[] { 0, 7, 8, 0, 1, 7, 1, 5, 7 },
            new[] { 1, 5, 3, 3, 5, 7 },
            new[] { 9, 7, 8, 9, 5, 7, 10, 1, 2 },
            new[] { 10, 1, 2, 9, 5, 0, 5, 3, 0, 5, 7, 3 },
            new[] { 8, 0, 2, 8, 2, 5, 8, 5, 7, 10, 5, 2 },
            new[] { 2, 10, 5, 2, 5, 3, 3, 5, 7 },
            new[] { 7, 9, 5, 7, 8, 9, 3, 11, 2 },
            new[] { 9, 5, 7, 9, 7, 2, 9, 2, 0, 2, 7, 11 },
            new[] { 2, 3, 11, 0, 1, 8, 1, 7, 8, 1, 5, 7 },
            new[] { 11, 2, 1, 11, 1, 7, 7, 1, 5 },
            new[] { 9, 5, 8, 8, 5, 7, 10, 1, 3, 10, 3, 11 },
            new[] { 5, 7, 0, 5, 0, 9, 7, 11, 0, 1, 0, 10, 11, 10, 0 },
            new[] { 11, 10, 0, 11, 0, 3, 10, 5, 0, 8, 0, 7, 5, 7, 0 },
            new[] { 11, 10, 5, 7, 11, 5 },
            new[] { 10, 6, 5 },
            new[] { 0, 8, 3, 5, 10, 6 },
            new[] { 9, 0, 1, 5, 10, 6 },
            new[] { 1, 8, 3, 1, 9, 8, 5, 10, 6 },
            new[] { 1, 6, 5, 2, 6, 1 },
            new[] { 1, 6, 5, 1, 2, 6, 3, 0, 8 },
            new[] { 9, 6, 5, 9, 0, 6, 0, 2, 6 },
            new[] { 5, 9, 8, 5, 8, 2, 5, 2, 6, 3, 2, 8 },
            new[] { 2, 3, 11, 10, 6, 5 },
            new[] { 11, 0, 8, 11, 2, 0, 10, 6, 5 },
            new[] { 0, 1, 9, 2, 3, 11, 5, 10, 6 },
            new[] { 5, 10, 6, 1, 9, 2, 9, 11, 2, 9, 8, 11 },
            new[] { 6, 3, 11, 6, 5, 3, 5, 1, 3 },
            new[] { 0, 8, 11, 0, 11, 5, 0, 5, 1, 5, 11, 6 },
            new[] { 3, 11, 6, 0, 3, 6, 0, 6, 5, 0, 5, 9 },
            new[] { 6, 5, 9, 6, 9, 11, 11, 9, 8 },
            new[] { 5, 10, 6, 4, 7, 8 },
            new[] { 4, 3, 0, 4, 7, 3, 6, 5, 10 },
            new[] { 1, 9, 0, 5, 10, 6, 8, 4, 7 },
            new[] { 10, 6, 5, 1, 9, 7, 1, 7, 3, 7, 9, 4 },
            new[] { 6, 1, 2, 6, 5, 1, 4, 7, 8 },
            new[] { 1, 2, 5, 5, 2, 6, 3, 0, 4, 3, 4, 7 },
            new[] { 8, 4, 7, 9, 0, 5, 0, 6, 5, 0, 2, 6 },
            new[] { 7, 3, 9, 7, 9, 4, 3, 2, 9, 5, 9, 6, 2, 6, 9 },
            new[] { 3, 11, 2, 7, 8, 4, 10, 6, 5 },
            new[] { 5, 10, 6, 4, 7, 2, 4, 2, 0, 2, 7, 11 },
            new[] { 0, 1, 9, 4, 7, 8, 2, 3, 11, 5, 10, 6 },
            new[] { 9, 2, 1, 9, 11, 2, 9, 4, 11, 7, 11, 4, 5, 10, 6 },
            new[] { 8, 4, 7, 3, 11, 5, 3, 5, 1, 5, 11, 6 },
            new[] { 5, 1, 11, 5, 11, 6, 1, 0, 11, 7, 11, 4, 0, 4, 11 },
            new[] { 0, 5, 9, 0, 6, 5, 0, 3, 6, 11, 6, 3, 8, 4, 7 },
            new[] { 6, 5, 9, 6, 9, 11, 4, 7, 9, 7, 11, 9 },
            new[] { 10, 4, 9, 6, 4, 10 },
            new[] { 4, 10, 6, 4, 9, 10, 0, 8, 3 },
            new[] { 10, 0, 1, 10, 6, 0, 6, 4, 0 },
            new[] { 8, 3, 1, 8, 1, 6, 8, 6, 4, 6, 1, 10 },
            new[] { 1, 4, 9, 1, 2, 4, 2, 6, 4 },
            new[] { 3, 0, 8, 1, 2, 9, 2, 4, 9, 2, 6, 4 },
            new[] { 0, 2, 4, 4, 2, 6 },
            new[] { 8, 3, 2, 8, 2, 4, 4, 2, 6 },
            new[] { 10, 4, 9, 10, 6, 4, 11, 2, 3 },
            new[] { 0, 8, 2, 2, 8, 11, 4, 9, 10, 4, 10, 6 },
            new[] { 3, 11, 2, 0, 1, 6, 0, 6, 4, 6, 1, 10 },
            new[] { 6, 4, 1, 6, 1, 10, 4, 8, 1, 2, 1, 11, 8, 11, 1 },
            new[] { 9, 6, 4, 9, 3, 6, 9, 1, 3, 11, 6, 3 },
            new[] { 8, 11, 1, 8, 1, 0, 11, 6, 1, 9, 1, 4, 6, 4, 1 },
            new[] { 3, 11, 6, 3, 6, 0, 0, 6, 4 },
            new[] { 6, 4, 8, 11, 6, 8 },
            new[] { 7, 10, 6, 7, 8, 10, 8, 9, 10 },
            new[] { 0, 7, 3, 0, 10, 7, 0, 9, 10, 6, 7, 10 },
            new[] { 10, 6, 7, 1, 10, 7, 1, 7, 8, 1, 8, 0 },
            new[] { 10, 6, 7, 10, 7, 1, 1, 7, 3 },
            new[] { 1, 2, 6, 1, 6, 8, 1, 8, 9, 8, 6, 7 },
            new[] { 2, 6, 9, 2, 9, 1, 6, 7, 9, 0, 9, 3, 7, 3, 9 },
            new[] { 7, 8, 0, 7, 0, 6, 6, 0, 2 },
            new[] { 7, 3, 2, 6, 7, 2 },
            new[] { 2, 3, 11, 10, 6, 8, 10, 8, 9, 8, 6, 7 },
            new[] { 2, 0, 7, 2, 7, 11, 0, 9, 7, 6, 7, 10, 9, 10, 7 },
            new[] { 1, 8, 0, 1, 7, 8, 1, 10, 7, 6, 7, 10, 2, 3, 11 },
            new[] { 11, 2, 1, 11, 1, 7, 10, 6, 1, 6, 7, 1 },
            new[] { 8, 9, 6, 8, 6, 7, 9, 1, 6, 11, 6, 3, 1, 3, 6 },
            new[] { 0, 9, 1, 11, 6, 7 },
            new[] { 7, 8, 0, 7, 0, 6, 3, 11, 0, 11, 6, 0 },
            new[] { 7, 11, 6 },
            new[] { 7, 6, 11 },
            new[] { 3, 0, 8, 11, 7, 6 },
            new[] { 0, 1, 9, 11, 7, 6 },
            new[] { 8, 1, 9, 8, 3, 1, 11, 7, 6 },
            new[] { 10, 1, 2, 6, 11, 7 },
            new[] { 1, 2, 10, 3, 0, 8, 6, 11, 7 },
            new[] { 2, 9, 0, 2, 10, 9, 6, 11, 7 },
            new[] { 6, 11, 7, 2, 10, 3, 10, 8, 3, 10, 9, 8 },
            new[] { 7, 2, 3, 6, 2, 7 },
            new[] { 7, 0, 8, 7, 6, 0, 6, 2, 0 },
            new[] { 2, 7, 6, 2, 3, 7, 0, 1, 9 },
            new[] { 1, 6, 2, 1, 8, 6, 1, 9, 8, 8, 7, 6 },
            new[] { 10, 7, 6, 10, 1, 7, 1, 3, 7 },
            new[] { 10, 7, 6, 1, 7, 10, 1, 8, 7, 1, 0, 8 },
            new[] { 0, 3, 7, 0, 7, 10, 0, 10, 9, 6, 10, 7 },
            new[] { 7, 6, 10, 7, 10, 8, 8, 10, 9 },
            new[] { 6, 8, 4, 11, 8, 6 },
            new[] { 3, 6, 11, 3, 0, 6, 0, 4, 6 },
            new[] { 8, 6, 11, 8, 4, 6, 9, 0, 1 },
            new[] { 9, 4, 6, 9, 6, 3, 9, 3, 1, 11, 3, 6 },
            new[] { 6, 8, 4, 6, 11, 8, 2, 10, 1 },
            new[] { 1, 2, 10, 3, 0, 11, 0, 6, 11, 0, 4, 6 },
            new[] { 4, 11, 8, 4, 6, 11, 0, 2, 9, 2, 10, 9 },
            new[] { 10, 9, 3, 10, 3, 2, 9, 4, 3, 11, 3, 6, 4, 6, 3 },
            new[] { 8, 2, 3, 8, 4, 2, 4, 6, 2 },
            new[] { 0, 4, 2, 4, 6, 2 },
            new[] { 1, 9, 0, 2, 3, 4, 2, 4, 6, 4, 3, 8 },
            new[] { 1, 9, 4, 1, 4, 2, 2, 4, 6 },
            new[] { 8, 1, 3, 8, 6, 1, 8, 4, 6, 6, 10, 1 },
            new[] { 10, 1, 0, 10, 0, 6, 6, 0, 4 },
            new[] { 4, 6, 3, 4, 3, 8, 6, 10, 3, 0, 3, 9, 10, 9, 3 },
            new[] { 10, 9, 4, 6, 10, 4 },
            new[] { 4, 9, 5, 7, 6, 11 },
            new[] { 0, 8, 3, 4, 9, 5, 11, 7, 6 },
            new[] { 5, 0, 1, 5, 4, 0, 7, 6, 11 },
            new[] { 11, 7, 6, 8, 3, 4, 3, 5, 4, 3, 1, 5 },
            new[] { 9, 5, 4, 10, 1, 2, 7, 6, 11 },
            new[] { 6, 11, 7, 1, 2, 10, 0, 8, 3, 4, 9, 5 },
            new[] { 7, 6, 11, 5, 4, 10, 4, 2, 10, 4, 0, 2 },
            new[] { 3, 4, 8, 3, 5, 4, 3, 2, 5, 10, 5, 2, 11, 7, 6 },
            new[] { 7, 2, 3, 7, 6, 2, 5, 4, 9 },
            new[] { 9, 5, 4, 0, 8, 6, 0, 6, 2, 6, 8, 7 },
            new[] { 3, 6, 2, 3, 7, 6, 1, 5, 0, 5, 4, 0 },
            new[] { 6, 2, 8, 6, 8, 7, 2, 1, 8, 4, 8, 5, 1, 5, 8 },
            new[] { 9, 5, 4, 10, 1, 6, 1, 7, 6, 1, 3, 7 },
            new[] { 1, 6, 10, 1, 7, 6, 1, 0, 7, 8, 7, 0, 9, 5, 4 },
            new[] { 4, 0, 10, 4, 10, 5, 0, 3, 10, 6, 10, 7, 3, 7, 10 },
            new[] { 7, 6, 10, 7, 10, 8, 5, 4, 10, 4, 8, 10 },
            new[] { 6, 9, 5, 6, 11, 9, 11, 8, 9 },
            new[] { 3, 6, 11, 0, 6, 3, 0, 5, 6, 0, 9, 5 },
            new[] { 0, 11, 8, 0, 5, 11, 0, 1, 5, 5, 6, 11 },
            new[] { 6, 11, 3, 6, 3, 5, 5, 3, 1 },
            new[] { 1, 2, 10, 9, 5, 11, 9, 11, 8, 11, 5, 6 },
            new[] { 0, 11, 3, 0, 6, 11, 0, 9, 6, 5, 6, 9, 1, 2, 10 },
            new[] { 11, 8, 5, 11, 5, 6, 8, 0, 5, 10, 5, 2, 0, 2, 5 },
            new[] { 6, 11, 3, 6, 3, 5, 2, 10, 3, 10, 5, 3 },
            new[] { 5, 8, 9, 5, 2, 8, 5, 6, 2, 3, 8, 2 },
            new[] { 9, 5, 6, 9, 6, 0, 0, 6, 2 },
            new[] { 1, 5, 8, 1, 8, 0, 5, 6, 8, 3, 8, 2, 6, 2, 8 },
            new[] { 1, 5, 6, 2, 1, 6 },
            new[] { 1, 3, 6, 1, 6, 10, 3, 8, 6, 5, 6, 9, 8, 9, 6 },
            new[] { 10, 1, 0, 10, 0, 6, 9, 5, 0, 5, 6, 0 },
            new[] { 0, 3, 8, 5, 6, 10 },
            new[] { 10, 5, 6 },
            new[] { 11, 5, 10, 7, 5, 11 },
            new[] { 11, 5, 10, 11, 7, 5, 8, 3, 0 },
            new[] { 5, 11, 7, 5, 10, 11, 1, 9, 0 },
            new[] { 10, 7, 5, 10, 11, 7, 9, 8, 1, 8, 3, 1 },
            new[] { 11, 1, 2, 11, 7, 1, 7, 5, 1 },
            new[] { 0, 8, 3, 1, 2, 7, 1, 7, 5, 7, 2, 11 },
            new[] { 9, 7, 5, 9, 2, 7, 9, 0, 2, 2, 11, 7 },
            new[] { 7, 5, 2, 7, 2, 11, 5, 9, 2, 3, 2, 8, 9, 8, 2 },
            new[] { 2, 5, 10, 2, 3, 5, 3, 7, 5 },
            new[] { 8, 2, 0, 8, 5, 2, 8, 7, 5, 10, 2, 5 },
            new[] { 9, 0, 1, 5, 10, 3, 5, 3, 7, 3, 10, 2 },
            new[] { 9, 8, 2, 9, 2, 1, 8, 7, 2, 10, 2, 5, 7, 5, 2 },
            new[] { 1, 3, 5, 3, 7, 5 },
            new[] { 0, 8, 7, 0, 7, 1, 1, 7, 5 },
            new[] { 9, 0, 3, 9, 3, 5, 5, 3, 7 },
            new[] { 9, 8, 7, 5, 9, 7 },
            new[] { 5, 8, 4, 5, 10, 8, 10, 11, 8 },
            new[] { 5, 0, 4, 5, 11, 0, 5, 10, 11, 11, 3, 0 },
            new[] { 0, 1, 9, 8, 4, 10, 8, 10, 11, 10, 4, 5 },
            new[] { 10, 11, 4, 10, 4, 5, 11, 3, 4, 9, 4, 1, 3, 1, 4 },
            new[] { 2, 5, 1, 2, 8, 5, 2, 11, 8, 4, 5, 8 },
            new[] { 0, 4, 11, 0, 11, 3, 4, 5, 11, 2, 11, 1, 5, 1, 11 },
            new[] { 0, 2, 5, 0, 5, 9, 2, 11, 5, 4, 5, 8, 11, 8, 5 },
            new[] { 9, 4, 5, 2, 11, 3 },
            new[] { 2, 5, 10, 3, 5, 2, 3, 4, 5, 3, 8, 4 },
            new[] { 5, 10, 2, 5, 2, 4, 4, 2, 0 },
            new[] { 3, 10, 2, 3, 5, 10, 3, 8, 5, 4, 5, 8, 0, 1, 9 },
            new[] { 5, 10, 2, 5, 2, 4, 1, 9, 2, 9, 4, 2 },
            new[] { 8, 4, 5, 8, 5, 3, 3, 5, 1 },
            new[] { 0, 4, 5, 1, 0, 5 },
            new[] { 8, 4, 5, 8, 5, 3, 9, 0, 5, 0, 3, 5 },
            new[] { 9, 4, 5 },
            new[] { 4, 11, 7, 4, 9, 11, 9, 10, 11 },
            new[] { 0, 8, 3, 4, 9, 7, 9, 11, 7, 9, 10, 11 },
            new[] { 1, 10, 11, 1, 11, 4, 1, 4, 0, 7, 4, 11 },
            new[] { 3, 1, 4, 3, 4, 8, 1, 10, 4, 7, 4, 11, 10, 11, 4 },
            new[] { 4, 11, 7, 9, 11, 4, 9, 2, 11, 9, 1, 2 },
            new[] { 9, 7, 4, 9, 11, 7, 9, 1, 11, 2, 11, 1, 0, 8, 3 },
            new[] { 11, 7, 4, 11, 4, 2, 2, 4, 0 },
            new[] { 11, 7, 4, 11, 4, 2, 8, 3, 4, 3, 2, 4 },
            new[] { 2, 9, 10, 2, 7, 9, 2, 3, 7, 7, 4, 9 },
            new[] { 9, 10, 7, 9, 7, 4, 10, 2, 7, 8, 7, 0, 2, 0, 7 },
            new[] { 3, 7, 10, 3, 10, 2, 7, 4, 10, 1, 10, 0, 4, 0, 10 },
            new[] { 1, 10, 2, 8, 7, 4 },
            new[] { 4, 9, 1, 4, 1, 7, 7, 1, 3 },
            new[] { 4, 9, 1, 4, 1, 7, 0, 8, 1, 8, 7, 1 },
            new[] { 4, 0, 3, 7, 4, 3 },
            new[] { 4, 8, 7 },
            new[] { 9, 10, 8, 10, 11, 8 },
            new[] { 3, 0, 9, 3, 9, 11, 11, 9, 10 },
            new[] { 0, 1, 10, 0, 10, 8, 8, 10, 11 },
            new[] { 3, 1, 10, 11, 3, 10 },
            new[] { 1, 2, 11, 1, 11, 9, 9, 11, 8 },
            new[] { 3, 0, 9, 3, 9, 11, 1, 2, 9, 2, 11, 9 },
            new[] { 0, 2, 11, 8, 0, 11 },
            new[] { 3, 2, 11 },
            new[] { 2, 3, 8, 2, 8, 10, 10, 8, 9 },
            new[] { 9, 10, 2, 0, 9, 2 },
            new[] { 2, 3, 8, 2, 8, 10, 0, 1, 8, 1, 10, 8 },
            new[] { 1, 10, 2 },
            new[] { 1, 3, 8, 9, 1, 8 },
            new[] { 0, 9, 1 },
            new[] { 0, 3, 8 },
            new int[] { },
        };

        /// <summary>Edges used by one triangle only (0 for a closed surface). Used to check the tables and bakes.</summary>
        public static int OpenEdges(List<int> triangles)
        {
            var count = new Dictionary<long, int>();
            for (int i = 0; i < triangles.Count; i += 3)
                for (int e = 0; e < 3; e++)
                {
                    int a = triangles[i + e], b = triangles[i + (e + 1) % 3];
                    long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                    count.TryGetValue(key, out int c);
                    count[key] = c + 1;
                }
            int open = 0;
            foreach (var c in count.Values) if (c != 2) open++;
            return open;
        }

        public static int TableRows => TriTable.Length;
    }
}
