using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Diorama.EditorTools.StarterKit;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 11c lake temple props (Tidemother's room, like the tower hall): pale jade stone walls and rounded corners
    /// with a lily frieze, pillars wound with lily vines, a high window of still water-light, the glassy dome of hoarded
    /// water (drawn by <see cref="LakeTempleHall"/>, which lets it flow out once she calms), the water shard and the door
    /// arch. The floor's shallow water is the hall's theme; stepping stones and lilies are Lilymere's own props.
    /// </summary>
    public static partial class PropKit
    {
        static readonly PropColor JadeStone = C("#9EC2B4");
        static readonly PropColor JadeDark = C("#3E6A62");
        static readonly PropColor WaterLight = C("#BFEAF2", 0.9f);

        /// <summary>Writes the lake temple props. Returns them by name (not baked).</summary>
        public static Dictionary<string, PropRecipe> WriteLakeTempleProps()
        {
            DioramaSceneSetup.EnsureFolder(Root);
            DioramaSceneSetup.EnsureFolder($"{Root}/Shapes");
            var all = new Dictionary<string, PropRecipe>();
            void Add(PropRecipe r)
            {
                r.yawJitter = 0f;
                r.scaleJitter = 0f;
                all[r.name] = r;
                EditorUtility.SetDirty(r);
            }
            PropColor[] Stones() => Palette(LotusPink, StiltWood, JadeStone, LilyGreen, WaterLight, JadeDark);

            var wall = Recipe("TempleWall", new Vector2Int(1, 1), true, false, 800);
            wall.palette = Stones();
            wall.pieces = new List<PropPiece>
            {
                Piece(PShape("TempleWall_Stone",
                    Box(new Vector3(0f, 0.8f, 0f), new Vector3(0.5f, 0.8f, 0.16f), 0.04f, 0f).Bumps(0.016f, 6f),
                    Box(new Vector3(0f, 1.63f, 0f), new Vector3(0.52f, 0.05f, 0.2f), 0.02f, 0f),
                    Floor(0f)), PropSlot.Stone),
                Piece(PShape("TempleWall_Frieze",
                    Ell(new Vector3(-0.25f, 1.35f, 0.17f), new Vector3(0.14f, 0.03f, 0.1f), 0f, new Vector3(-70f, 0f, 0f)),
                    Ell(new Vector3(0.25f, 1.35f, 0.17f), new Vector3(0.14f, 0.03f, 0.1f), 0f, new Vector3(-70f, 0f, 0f))), PropSlot.Leaf),
            };
            Add(wall);

            foreach (var (name, side) in new[] { ("TempleCornerL", -1f), ("TempleCornerR", 1f) })
            {
                var corner = Recipe(name, new Vector2Int(1, 1), true, false, 800);
                corner.palette = Stones();
                corner.pieces = new List<PropPiece>
                {
                    Piece(PShape($"{name}_Stone",
                        Box(new Vector3(-0.22f * side, 0.8f, 0.22f), new Vector3(0.4f, 0.8f, 0.16f), 0.04f, 0.06f, new Vector3(0f, -45f * side, 0f)).Bumps(0.016f, 6f),
                        Box(new Vector3(-0.22f * side, 1.63f, 0.22f), new Vector3(0.42f, 0.05f, 0.2f), 0.02f, 0.02f, new Vector3(0f, -45f * side, 0f)),
                        Floor(0f)), PropSlot.Stone),
                };
                Add(corner);
            }

            var pillar = Recipe("TemplePillar", new Vector2Int(1, 1), true, false, 900);
            pillar.palette = Stones();
            var vine = new List<SdfPrimitive>();
            for (int i = 0; i < 6; i++)
            {
                float a = i * 70f * Mathf.Deg2Rad, y = 0.3f + i * 0.25f;
                vine.Add(Ell(new Vector3(Mathf.Cos(a) * 0.17f, y, Mathf.Sin(a) * 0.17f), new Vector3(0.08f, 0.02f, 0.06f), 0f, new Vector3(0f, -i * 70f, 20f)));
            }
            pillar.pieces = new List<PropPiece>
            {
                Piece(PShape("TemplePillar_Stone",
                    Box(new Vector3(0f, 0.07f, 0f), new Vector3(0.26f, 0.07f, 0.26f), 0.03f, 0f),
                    Cone(new Vector3(0f, 0.1f, 0f), new Vector3(0f, 1.9f, 0f), 0.17f, 0.15f, 0.03f).Bumps(0.008f, 8f),
                    Box(new Vector3(0f, 1.95f, 0f), new Vector3(0.24f, 0.06f, 0.24f), 0.03f, 0.02f),
                    Floor(0f)), PropSlot.Stone),
                Piece(PShape("TemplePillar_Vine", vine.ToArray()), PropSlot.Leaf, PropMotion.Sway, 0.01f),
            };
            Add(pillar);

            // The high window (2 x 1, in the back wall): a tall rounded opening high up, filled with still water-light.
            var window = Recipe("TempleWindow", new Vector2Int(2, 1), true, false, 2000);
            window.palette = Stones();
            window.resolution = 60;
            window.pieces = new List<PropPiece>
            {
                Piece(PShape("TempleWindow_Stone",
                    Box(new Vector3(0f, 1f, 0f), new Vector3(1f, 1f, 0.16f), 0.04f, 0f).Bumps(0.016f, 6f),
                    Box(new Vector3(0f, 2.03f, 0f), new Vector3(1.02f, 0.05f, 0.2f), 0.02f, 0f),
                    Ell(new Vector3(0f, 1.3f, 0f), new Vector3(0.48f, 0.62f, 0.4f), 0f).Sub(),
                    Floor(0f)), PropSlot.Stone),
                Piece(PShape("TempleWindow_Light",
                    Ell(new Vector3(0f, 1.3f, -0.06f), new Vector3(0.5f, 0.64f, 0.02f), 0f)), PropSlot.Glow),
                Piece(PShape("TempleWindow_Frame",
                    Torus(new Vector3(0f, 1.3f, 0.05f), 0.5f, 0.04f, new Vector3(90f, 0f, 0f), 0f),
                    Box(new Vector3(0f, 0.62f, 0.1f), new Vector3(0.45f, 0.035f, 0.1f), 0.015f, 0.02f)), PropSlot.Accent),
            };
            Add(window);

            // The hoarded water: a big glassy dome (3 x 1 footprint, about 2 tiles high), still and heavy.
            var dome = Recipe("WaterDome", new Vector2Int(3, 1), false, false, 1800);
            dome.palette = Palette(LotusPink, StiltWood, JadeStone, LilyGreen,
                new PropColor(Hex("#9EE4EE"), Hex("#9EE4EE"), 0.35f), new PropColor(Hex("#E6FBFF"), Hex("#E6FBFF"), 0.6f));
            dome.resolution = 56;
            dome.outline = 0.02f;
            dome.spec = 0.9f;
            dome.pieces = new List<PropPiece>
            {
                Piece(PShape("WaterDome_Water",
                    Ell(new Vector3(0f, 0.55f, 0f), new Vector3(1.45f, 1.1f, 0.6f), 0f),
                    Floor(0f)), PropSlot.Glow),
                Piece(PShape("WaterDome_Shine",
                    Ell(new Vector3(-0.55f, 1.15f, 0.42f), new Vector3(0.28f, 0.12f, 0.06f), 0f, new Vector3(0f, 0f, 25f)),
                    Ell(new Vector3(0.6f, 0.75f, 0.52f), new Vector3(0.14f, 0.07f, 0.05f), 0f, new Vector3(0f, 0f, -20f))), PropSlot.Accent),
            };
            Add(dome);

            var door = Recipe("TempleHallDoor", new Vector2Int(2, 1), false, false, 1200);
            door.palette = Stones();
            door.pieces = new List<PropPiece>
            {
                Piece(PShape("TempleHallDoor_Arch",
                    Torus(new Vector3(0f, 1.5f, 0.3f), 0.95f, 0.13f, new Vector3(90f, 0f, 0f), 0f),
                    Plane(new Vector3(0f, 1.48f, 0f), Vector3.down, 0f),
                    Box(new Vector3(0.95f, 0.76f, 0.3f), new Vector3(0.14f, 0.76f, 0.14f), 0.04f, 0.05f).Mirror(),
                    Floor(0f)), PropSlot.Stone),
            };
            Add(door);
            return all;
        }

        /// <summary>Step 11c: the water shard (a blue crystal with a wave curl), carried by the Spirit and set in the statue.</summary>
        public static PropRecipe WriteWaterShardProp()
        {
            DioramaSceneSetup.EnsureFolder(Root);
            DioramaSceneSetup.EnsureFolder($"{Root}/Shapes");
            var shard = Recipe("WaterShard", new Vector2Int(1, 1), false, false, 1000);
            shard.palette = Palette(Clay, Wood, Stone, Leaf, new PropColor(Hex("#4FA8F0"), Hex("#4FA8F0"), 1.2f), new PropColor(Hex("#D2ECFF"), Hex("#D2ECFF"), 0.9f));
            shard.yawJitter = shard.scaleJitter = 0f;
            shard.resolution = 26;
            shard.outline = 0.03f;
            shard.pieces = new List<PropPiece>
            {
                Piece(PShape("WaterShard_Core",
                    Cone(new Vector3(0f, 0f, 0f), new Vector3(0f, 0.6f, 0f), 0.18f, 0.13f, 0f),
                    Cone(new Vector3(0f, 0.6f, 0f), new Vector3(0f, 1f, 0f), 0.13f, 0.01f, 0.02f),
                    Cone(new Vector3(0f, 0f, 0f), new Vector3(0f, -0.32f, 0f), 0.18f, 0.02f, 0.02f)), PropSlot.Glow, PropMotion.Flicker, 0.003f),
                Piece(PShape("WaterShard_Wave",
                    Torus(new Vector3(0f, 0.25f, 0f), 0.25f, 0.03f, new Vector3(80f, 0f, 0f), 0f),
                    Plane(new Vector3(0f, 0.25f, 0f), Vector3.down, 0.02f)), PropSlot.Accent),
            };
            EditorUtility.SetDirty(shard);
            return shard;
        }
    }
}
