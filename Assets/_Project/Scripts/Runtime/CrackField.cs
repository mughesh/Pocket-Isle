using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 22: the Toon shader's crack pattern on the CPU (ToonCrackField, ToonCrackWidth in ToonCommon.hlsl; keep them
    /// the same), so particles can start on the cracks the shader draws.
    /// </summary>
    public static class CrackField
    {
        public struct Point
        {
            public Vector3 position, normal;
        }

        static float Frac(float x) => x - Mathf.Floor(x);

        static Vector3 Hash(Vector3 p)
        {
            float a = p.x * 127.1f + p.y * 311.7f + p.z * 74.7f;
            float b = p.x * 269.5f + p.y * 183.3f + p.z * 246.1f;
            float c = p.x * 113.5f + p.y * 271.9f + p.z * 124.6f;
            return new Vector3(Frac(Mathf.Sin(a) * 43758.5453f), Frac(Mathf.Sin(b) * 43758.5453f), Frac(Mathf.Sin(c) * 43758.5453f));
        }

        static readonly Vector3[] Sites =
        {
            new Vector3(0.3904f, 0.9202f, -0.0294f),
            new Vector3(-0.3486f, 0.9195f, 0.1817f),
            new Vector3(-0.0362f, 0.8626f, -0.5046f),
            new Vector3(0.299f, 0.8641f, 0.4049f),
            new Vector3(-0.7035f, 0.7046f, -0.0932f),
            new Vector3(0.7092f, 0.5774f, -0.4045f),
            new Vector3(-0.1175f, 0.536f, 0.836f),
            new Vector3(-0.411f, 0.221f, -0.8845f),
            new Vector3(0.8539f, 0.3199f, 0.4104f),
            new Vector3(-0.7146f, 0.4479f, 0.5373f),
            new Vector3(0.5001f, 0.404f, -0.766f),
            new Vector3(0.2273f, 0.2827f, 0.9319f),
            new Vector3(-0.8282f, 0.2246f, -0.5134f),
            new Vector3(0.9793f, -0.0393f, -0.1984f),
            new Vector3(-0.7018f, -0.212f, 0.6801f),
            new Vector3(-0.2828f, -0.2451f, -0.9273f),
            new Vector3(0.6745f, -0.1757f, 0.7171f),
            new Vector3(-0.8823f, -0.4385f, 0.1712f),
            new Vector3(0.6976f, -0.3805f, -0.6071f),
            new Vector3(-0.185f, -0.4214f, 0.8878f),
            new Vector3(-0.5818f, -0.5196f, -0.6257f),
            new Vector3(0.8022f, -0.5833f, 0.1276f),
            new Vector3(-0.5068f, -0.7526f, 0.4204f),
            new Vector3(0.2779f, -0.7833f, -0.5561f),
            new Vector3(0.2587f, -0.834f, 0.4873f),
            new Vector3(-0.3655f, -0.8966f, -0.2499f),
        };

        public static float Distance(Vector3 pO, float scale) => Distance(pO, scale, out _);

        public static float Distance(Vector3 pO, float scale, out float edge)
        {
            var p = pO + new Vector3(Mathf.Sin(pO.y * 9f), Mathf.Sin(pO.z * 9f), Mathf.Sin(pO.x * 9f)) * 0.03f
                + new Vector3(Mathf.Sin(pO.z * 23f + 1.3f), Mathf.Sin(pO.x * 23f + 1.3f), Mathf.Sin(pO.y * 23f + 1.3f)) * 0.0125f
                + new Vector3(Mathf.Sin(pO.y * 57f + 0.7f), Mathf.Sin(pO.z * 57f + 0.7f), Mathf.Sin(pO.x * 57f + 0.7f)) * 0.005f;
            var d = p.normalized;
            int best = 0;
            float top = -2f;
            for (int i = 0; i < Sites.Length; i++)
            {
                float c = Vector3.Dot(d, Sites[i]);
                if (c > top) { top = c; best = i; }
            }
            var a = Sites[best];
            float e = 8f;
            edge = 0f;
            for (int j = 0; j < Sites.Length; j++)
            {
                if (j == best) continue;
                var ab = a - Sites[j];
                float g = Vector3.Dot(d, ab) / ab.magnitude;
                if (g < e) { e = g; edge = Hash((a + Sites[j]) * 17f).y; }
            }
            return Mathf.Max(e, 0f) * p.magnitude * 2f * scale;
        }

        public static float Width(Vector3 pO, float width, float edge) =>
            width * Mathf.Lerp(0.5f, 1.4f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.25f, 0.8f, edge))) * (0.8f + 0.2f * Mathf.Sin(Vector3.Dot(pO, new Vector3(1.7f, 2.3f, 1.1f)) * 2.6f));

        /// <summary>Fills <paramref name="into"/> with up to <paramref name="count"/> object-space points inside the cracks (fixed seed).</summary>
        public static void FindPoints(Mesh mesh, float scale, float width, int count, List<Point> into)
        {
            var v = mesh.vertices;
            var t = mesh.triangles;
            var rnd = new System.Random(1729);
            for (int tries = 0; tries < count * 200 && into.Count < count; tries++)
            {
                int tri = rnd.Next(t.Length / 3) * 3;
                Vector3 a = v[t[tri]], b = v[t[tri + 1]], c = v[t[tri + 2]];
                float u = (float)rnd.NextDouble(), w = (float)rnd.NextDouble();
                if (u + w > 1f) { u = 1f - u; w = 1f - w; }
                var p = a + (b - a) * u + (c - a) * w;
                var n = Vector3.Cross(b - a, c - a).normalized;
                if (Distance(p, scale, out float edge) < Width(p, width, edge) * 0.7f)
                    into.Add(new Point { position = p, normal = n });
            }
        }
    }
}
