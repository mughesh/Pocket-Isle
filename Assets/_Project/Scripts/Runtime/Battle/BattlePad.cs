using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// A round, slightly domed earth disc a battle creature stands on. Sits on a map tile under the island (scrolls,
    /// zooms and clips at the rim like the tiles), rises out of the ground with <see cref="Rise"/> and sinks back.
    /// Tinted per habitat: earth with a grass ring, or sand with a wet ring for pond battles.
    /// The mesh is a unit disc (0.5 radius, top at <see cref="topHeight"/>); the prefab is made by the Step 5 setup menu.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class BattlePad : MonoBehaviour
    {
        public IslandBuilder island;
        [Tooltip("Map position (column, row).")]
        public Vector2 tile;
        [Tooltip("Width in tiles.")]
        public float widthTiles = 0.8f;
        [Tooltip("Mesh top height in mesh units (the creature stands here).")]
        public float topHeight = 0.16f;
        [Tooltip("Rest height above the cap, map metres.")]
        public float restLift = 0.0015f;

        [Header("Colours")]
        public Color grassEarth = new Color(0.55f, 0.39f, 0.25f);
        public Color grassTop = new Color(0.66f, 0.5f, 0.33f);
        public Color grassRing = new Color(0.45f, 0.68f, 0.32f);
        public Color sandSide = new Color(0.78f, 0.66f, 0.45f);
        public Color sandTop = new Color(0.92f, 0.82f, 0.6f);
        public Color wetRing = new Color(0.55f, 0.62f, 0.6f);
        public Color outline = new Color(0.17f, 0.13f, 0.1f);

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int SockOnId = Shader.PropertyToID("_SockOn");
        static readonly int SockColorId = Shader.PropertyToID("_SockColor");
        static readonly int SockYId = Shader.PropertyToID("_SockY");
        static readonly int Patch0Id = Shader.PropertyToID("_Patch0");
        static readonly int PatchColor0Id = Shader.PropertyToID("_PatchColor0");
        static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
        static readonly int WindowClipId = Shader.PropertyToID("_WindowClip");

        /// <summary>0 = under the ground, 1 = up.</summary>
        public float Rise { get; set; }

        float Scale => widthTiles * island.map.tileSize * island.Zoom;

        /// <summary>Height of the pad top above the cap, map metres (where a creature's feet go).</summary>
        public float TopLift => restLift + topHeight * widthTiles * island.map.tileSize;

        public void Paint(Habitat habitat)
        {
            bool sand = habitat == Habitat.Water;
            var block = new MaterialPropertyBlock();
            // Ring colour everywhere, the sides below the top in earth / sand, and a round top patch in the middle.
            block.SetColor(BaseColorId, sand ? wetRing : grassRing);
            block.SetFloat(SockOnId, 1f);
            block.SetColor(SockColorId, sand ? sandSide : grassEarth);
            block.SetFloat(SockYId, topHeight - 0.06f);
            block.SetVector(Patch0Id, new Vector4(0f, topHeight, 0f, 0.36f));
            block.SetColor(PatchColor0Id, sand ? sandTop : grassTop);
            block.SetColor(OutlineColorId, outline);
            block.SetFloat(OutlineWidthId, 0.02f);
            block.SetFloat(WindowClipId, 1f);
            GetComponent<MeshRenderer>().SetPropertyBlock(block);
        }

        void LateUpdate() => Snap();

        public void Snap()
        {
            if (island == null || island.Content == null) return;
            float down = (1f - Mathf.SmoothStep(0f, 1f, Rise)) * (topHeight + 0.1f) * widthTiles * island.map.tileSize;
            transform.localPosition = island.TileToLocal(tile, island.Content.capHeight + restLift - down);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one * Scale;
        }

        /// <summary>Domed disc, radius 0.5, base at y = 0, top at <paramref name="top"/>; smooth normals for the outline.</summary>
        public static Mesh BuildMesh(float top = 0.16f, int segments = 28)
        {
            // Lathe profile (radius, height), from the top centre down the side to just below the ground.
            Vector2[] profile =
            {
                new Vector2(0f, top), new Vector2(0.2f, top - 0.006f), new Vector2(0.36f, top - 0.02f),
                new Vector2(0.45f, top - 0.045f), new Vector2(0.5f, top - 0.09f), new Vector2(0.5f, 0f), new Vector2(0.47f, -0.08f),
            };
            int rings = profile.Length;
            var verts = new Vector3[rings * (segments + 1)];
            var normals = new Vector3[verts.Length];
            for (int r = 0; r < rings; r++)
            {
                Vector2 prev = profile[Mathf.Max(0, r - 1)], next = profile[Mathf.Min(rings - 1, r + 1)];
                Vector2 tangent = (next - prev).normalized;
                Vector2 n2 = r == 0 ? Vector2.up : new Vector2(-tangent.y, tangent.x);
                if (n2.y < 0f && r < 3) n2 = -n2;
                if (n2.x < 0f) n2 = -n2;
                for (int s = 0; s <= segments; s++)
                {
                    float a = s / (float)segments * Mathf.PI * 2f;
                    float cos = Mathf.Cos(a), sin = Mathf.Sin(a);
                    verts[r * (segments + 1) + s] = new Vector3(profile[r].x * cos, profile[r].y, profile[r].x * sin);
                    normals[r * (segments + 1) + s] = new Vector3(n2.x * cos, n2.y, n2.x * sin).normalized;
                }
            }
            var tris = new System.Collections.Generic.List<int>();
            for (int r = 0; r < rings - 1; r++)
                for (int s = 0; s < segments; s++)
                {
                    int a = r * (segments + 1) + s, b = a + segments + 1;
                    tris.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b });
                }
            var mesh = new Mesh { name = "BattlePad" };
            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
