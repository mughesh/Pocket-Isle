using System.Collections.Generic;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Quadric error edge-collapse simplification (Garland and Heckbert) for closed, welded meshes such as the
    /// marching cubes output. Keeps the surface manifold (link condition) and never flips a triangle.
    /// </summary>
    public static class MeshSimplifier
    {
        struct Candidate
        {
            public double cost;
            public int a, b, versionA, versionB;
            public Vector3 target;
        }

        sealed class Heap
        {
            readonly List<Candidate> _items = new List<Candidate>();
            public int Count => _items.Count;

            public void Push(Candidate c)
            {
                _items.Add(c);
                int i = _items.Count - 1;
                while (i > 0)
                {
                    int p = (i - 1) / 2;
                    if (_items[p].cost <= _items[i].cost) break;
                    (_items[p], _items[i]) = (_items[i], _items[p]);
                    i = p;
                }
            }

            public Candidate Pop()
            {
                var top = _items[0];
                int last = _items.Count - 1;
                _items[0] = _items[last];
                _items.RemoveAt(last);
                int i = 0;
                while (true)
                {
                    int l = i * 2 + 1, r = l + 1, m = i;
                    if (l < _items.Count && _items[l].cost < _items[m].cost) m = l;
                    if (r < _items.Count && _items[r].cost < _items[m].cost) m = r;
                    if (m == i) break;
                    (_items[m], _items[i]) = (_items[i], _items[m]);
                    i = m;
                }
                return top;
            }
        }

        public static void Simplify(List<Vector3> vertices, List<int> triangles, int targetTriangles,
                                    out List<Vector3> outVertices, out List<int> outTriangles)
        {
            int vCount = vertices.Count, fCount = triangles.Count / 3;
            if (fCount <= targetTriangles)
            {
                outVertices = new List<Vector3>(vertices);
                outTriangles = new List<int>(triangles);
                return;
            }

            var pos = vertices.ToArray();
            var faces = triangles.ToArray();
            var faceDead = new bool[fCount];
            var q = new double[vCount * 10];
            var version = new int[vCount];
            var alive = new bool[vCount];
            var vertFaces = new List<int>[vCount];
            for (int v = 0; v < vCount; v++)
            {
                vertFaces[v] = new List<int>(8);
                alive[v] = true;
            }

            for (int f = 0; f < fCount; f++)
            {
                int a = faces[f * 3], b = faces[f * 3 + 1], c = faces[f * 3 + 2];
                vertFaces[a].Add(f);
                vertFaces[b].Add(f);
                vertFaces[c].Add(f);
                Vector3 n = Vector3.Cross(pos[b] - pos[a], pos[c] - pos[a]);
                double area = n.magnitude * 0.5;
                if (area < 1e-14) continue;
                n.Normalize();
                double d = -Vector3.Dot(n, pos[a]);
                AddPlane(q, a, n, d, area);
                AddPlane(q, b, n, d, area);
                AddPlane(q, c, n, d, area);
            }

            var heap = new Heap();
            for (int f = 0; f < fCount; f++)
                for (int e = 0; e < 3; e++)
                {
                    int a = faces[f * 3 + e], b = faces[f * 3 + (e + 1) % 3];
                    if (a < b) heap.Push(MakeCandidate(q, pos, version, a, b));
                }

            var neighboursA = new HashSet<int>();
            var neighboursB = new HashSet<int>();
            int live = fCount;
            while (live > targetTriangles && heap.Count > 0)
            {
                var c = heap.Pop();
                if (!alive[c.a] || !alive[c.b] || version[c.a] != c.versionA || version[c.b] != c.versionB) continue;
                if (!CanCollapse(c, pos, faces, faceDead, vertFaces, neighboursA, neighboursB)) continue;

                int keep = c.a, gone = c.b;
                pos[keep] = c.target;
                for (int i = 0; i < 10; i++) q[keep * 10 + i] += q[gone * 10 + i];
                foreach (int f in vertFaces[gone])
                {
                    if (faceDead[f]) continue;
                    int i0 = f * 3;
                    bool hasKeep = faces[i0] == keep || faces[i0 + 1] == keep || faces[i0 + 2] == keep;
                    if (hasKeep)
                    {
                        faceDead[f] = true;
                        live--;
                        continue;
                    }
                    for (int k = 0; k < 3; k++) if (faces[i0 + k] == gone) faces[i0 + k] = keep;
                    vertFaces[keep].Add(f);
                }
                alive[gone] = false;
                vertFaces[gone].Clear();
                version[keep]++;
                vertFaces[keep].RemoveAll(f => faceDead[f]);

                neighboursA.Clear();
                foreach (int f in vertFaces[keep])
                    for (int k = 0; k < 3; k++)
                    {
                        int w = faces[f * 3 + k];
                        if (w != keep) neighboursA.Add(w);
                    }
                foreach (int w in neighboursA) heap.Push(MakeCandidate(q, pos, version, keep, w));
            }

            var remap = new int[vCount];
            for (int i = 0; i < vCount; i++) remap[i] = -1;
            outVertices = new List<Vector3>();
            outTriangles = new List<int>(live * 3);
            for (int f = 0; f < fCount; f++)
            {
                if (faceDead[f]) continue;
                for (int k = 0; k < 3; k++)
                {
                    int v = faces[f * 3 + k];
                    if (remap[v] < 0)
                    {
                        remap[v] = outVertices.Count;
                        outVertices.Add(pos[v]);
                    }
                    outTriangles.Add(remap[v]);
                }
            }
        }

        static void AddPlane(double[] q, int v, Vector3 n, double d, double w)
        {
            int i = v * 10;
            double a = n.x, b = n.y, c = n.z;
            q[i] += w * a * a; q[i + 1] += w * a * b; q[i + 2] += w * a * c; q[i + 3] += w * a * d;
            q[i + 4] += w * b * b; q[i + 5] += w * b * c; q[i + 6] += w * b * d;
            q[i + 7] += w * c * c; q[i + 8] += w * c * d;
            q[i + 9] += w * d * d;
        }

        static double Error(double[] m, Vector3 p)
        {
            double x = p.x, y = p.y, z = p.z;
            return m[0] * x * x + 2 * m[1] * x * y + 2 * m[2] * x * z + 2 * m[3] * x
                 + m[4] * y * y + 2 * m[5] * y * z + 2 * m[6] * y
                 + m[7] * z * z + 2 * m[8] * z
                 + m[9];
        }

        static readonly double[] Sum = new double[10];

        static Candidate MakeCandidate(double[] q, Vector3[] pos, int[] version, int a, int b)
        {
            for (int i = 0; i < 10; i++) Sum[i] = q[a * 10 + i] + q[b * 10 + i];
            Vector3 pa = pos[a], pb = pos[b], mid = (pa + pb) * 0.5f;
            Vector3 best = mid;
            double bestCost = Error(Sum, mid);
            double ca = Error(Sum, pa), cb = Error(Sum, pb);
            if (ca < bestCost) { best = pa; bestCost = ca; }
            if (cb < bestCost) { best = pb; bestCost = cb; }

            // Optimal point: solve the 3x3 system, if it is well conditioned and stays near the edge.
            double m00 = Sum[0], m01 = Sum[1], m02 = Sum[2], m11 = Sum[4], m12 = Sum[5], m22 = Sum[7];
            double det = m00 * (m11 * m22 - m12 * m12) - m01 * (m01 * m22 - m12 * m02) + m02 * (m01 * m12 - m11 * m02);
            double scale = m00 + m11 + m22;
            if (scale > 0 && System.Math.Abs(det) > 1e-6 * scale * scale * scale)
            {
                double bx = -Sum[3], by = -Sum[6], bz = -Sum[8];
                double x = (bx * (m11 * m22 - m12 * m12) - m01 * (by * m22 - m12 * bz) + m02 * (by * m12 - m11 * bz)) / det;
                double y = (m00 * (by * m22 - m12 * bz) - bx * (m01 * m22 - m12 * m02) + m02 * (m01 * bz - by * m02)) / det;
                double z = (m00 * (m11 * bz - by * m12) - m01 * (m01 * bz - by * m02) + bx * (m01 * m12 - m11 * m02)) / det;
                var p = new Vector3((float)x, (float)y, (float)z);
                if ((p - mid).sqrMagnitude <= (pb - pa).sqrMagnitude)
                {
                    double cp = Error(Sum, p);
                    if (cp < bestCost) { best = p; bestCost = cp; }
                }
            }
            return new Candidate { cost = bestCost, a = a, b = b, versionA = version[a], versionB = version[b], target = best };
        }

        static bool CanCollapse(Candidate c, Vector3[] pos, int[] faces, bool[] faceDead, List<int>[] vertFaces,
                                HashSet<int> na, HashSet<int> nb)
        {
            // Link condition: on a closed manifold the edge's two triangles are the only shared neighbours.
            na.Clear();
            nb.Clear();
            foreach (int f in vertFaces[c.a])
                if (!faceDead[f]) for (int k = 0; k < 3; k++) na.Add(faces[f * 3 + k]);
            foreach (int f in vertFaces[c.b])
                if (!faceDead[f]) for (int k = 0; k < 3; k++) nb.Add(faces[f * 3 + k]);
            na.Remove(c.a); na.Remove(c.b);
            nb.Remove(c.a); nb.Remove(c.b);
            int shared = 0;
            foreach (int v in na) if (nb.Contains(v)) shared++;
            if (shared != 2 || na.Count < 3 || nb.Count < 3) return false;

            return !Flips(c.a, c.b, c.target, pos, faces, faceDead, vertFaces) &&
                   !Flips(c.b, c.a, c.target, pos, faces, faceDead, vertFaces);
        }

        /// <summary>True if moving <paramref name="v"/> to <paramref name="target"/> turns or squashes one of its triangles (except those on the edge).</summary>
        static bool Flips(int v, int other, Vector3 target, Vector3[] pos, int[] faces, bool[] faceDead, List<int>[] vertFaces)
        {
            foreach (int f in vertFaces[v])
            {
                if (faceDead[f]) continue;
                int i0 = f * 3;
                int a = faces[i0], b = faces[i0 + 1], cc = faces[i0 + 2];
                if (a == other || b == other || cc == other) continue;
                Vector3 pa = pos[a], pb = pos[b], pc = pos[cc];
                Vector3 before = Vector3.Cross(pb - pa, pc - pa);
                if (a == v) pa = target; else if (b == v) pb = target; else pc = target;
                Vector3 after = Vector3.Cross(pb - pa, pc - pa);
                float la = after.magnitude, lb = before.magnitude;
                if (la < 1e-12f || lb < 1e-12f) return true;
                if (Vector3.Dot(before, after) / (la * lb) < 0.3f) return true;
            }
            return false;
        }
    }
}
