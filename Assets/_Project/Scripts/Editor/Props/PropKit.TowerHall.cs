using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Diorama.EditorTools.StarterKit;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 10e tower hall props (Galewick's Keeper room, like the Forge Hall): cool cloud-stone wall segments and
    /// rounded corners, slim pillars, beams jutting out of the back wall with tangled kite strings and torn kite scraps
    /// hanging from them, heaps of tangled kites on the floor, a big round window in the back wall (its sky pane is a
    /// separate prop, <c>TowerSky</c>, drawn by <see cref="TowerHall"/>: still grey until Galeowl calms, then bright),
    /// drifting cloud wisps for the calmed sky, the empty perch and the front door arch.
    /// </summary>
    public static partial class PropKit
    {
        static readonly PropColor TowerStone = C("#8C9298");
        static readonly PropColor TowerDark = C("#3E444A");

        /// <summary>Writes the tower hall props. Returns them by name (not baked).</summary>
        public static Dictionary<string, PropRecipe> WriteTowerHallProps()
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
            PropColor[] Stones() => Palette(KiteRed, MillWood, TowerStone, KiteTeal, WarmWindow, TowerDark);

            var wall = Recipe("TowerWall", new Vector2Int(1, 1), true, false, 700);
            wall.palette = Stones();
            wall.pieces = new List<PropPiece>
            {
                Piece(PShape("TowerWall_Stone",
                    Box(new Vector3(0f, 0.8f, 0f), new Vector3(0.5f, 0.8f, 0.16f), 0.03f, 0f).Bumps(0.022f, 7f),
                    Box(new Vector3(0f, 1.63f, 0f), new Vector3(0.52f, 0.05f, 0.2f), 0.02f, 0f),
                    Floor(0f)), PropSlot.Stone),
                Piece(PShape("TowerWall_Beam",
                    Box(new Vector3(0f, 1.42f, 0.17f), new Vector3(0.53f, 0.06f, 0.05f), 0.015f, 0f),
                    Box(new Vector3(0f, 1.42f, -0.17f), new Vector3(0.53f, 0.06f, 0.05f), 0.015f, 0f)), PropSlot.Wood),
            };
            Add(wall);

            // Rounded corners: the wall turns from the back wall's line to the side wall's line (left and right).
            foreach (var (name, side) in new[] { ("TowerCornerL", -1f), ("TowerCornerR", 1f) })
            {
                var corner = Recipe(name, new Vector2Int(1, 1), true, false, 800);
                corner.palette = Stones();
                corner.pieces = new List<PropPiece>
                {
                    Piece(PShape($"{name}_Stone",
                        Box(new Vector3(-0.22f * side, 0.8f, 0.22f), new Vector3(0.4f, 0.8f, 0.16f), 0.03f, 0.06f, new Vector3(0f, -45f * side, 0f)).Bumps(0.022f, 7f),
                        Box(new Vector3(-0.22f * side, 1.63f, 0.22f), new Vector3(0.42f, 0.05f, 0.2f), 0.02f, 0.02f, new Vector3(0f, -45f * side, 0f)),
                        Floor(0f)), PropSlot.Stone),
                };
                Add(corner);
            }

            var pillar = Recipe("TowerPillar", new Vector2Int(1, 1), true, false, 800);
            pillar.palette = Stones();
            pillar.pieces = new List<PropPiece>
            {
                Piece(PShape("TowerPillar_Stone",
                    Box(new Vector3(0f, 0.07f, 0f), new Vector3(0.26f, 0.07f, 0.26f), 0.02f, 0f),
                    Cone(new Vector3(0f, 0.1f, 0f), new Vector3(0f, 1.9f, 0f), 0.17f, 0.15f, 0.03f).Bumps(0.01f, 9f),
                    Box(new Vector3(0f, 1.95f, 0f), new Vector3(0.24f, 0.06f, 0.24f), 0.02f, 0.02f),
                    Floor(0f)), PropSlot.Stone),
                Piece(PShape("TowerPillar_Band",
                    Torus(new Vector3(0f, 0.6f, 0f), 0.17f, 0.028f, Vector3.zero, 0f),
                    Torus(new Vector3(0f, 1.5f, 0f), 0.155f, 0.028f, Vector3.zero, 0f)), PropSlot.Wood),
            };
            Add(pillar);

            // The big round window (2 x 1, in the back wall, which rises a little higher here): a round hole, a wooden
            // frame and cross bars.
            var window = Recipe("TowerWindow", new Vector2Int(2, 1), true, false, 2000);
            window.palette = Stones();
            window.resolution = 60;
            window.pieces = new List<PropPiece>
            {
                Piece(PShape("TowerWindow_Stone",
                    Box(new Vector3(0f, 0.95f, 0f), new Vector3(1f, 0.95f, 0.16f), 0.03f, 0f).Bumps(0.022f, 7f),
                    Box(new Vector3(0f, 1.93f, 0f), new Vector3(1.02f, 0.05f, 0.2f), 0.02f, 0f),
                    Ell(new Vector3(0f, 1.02f, 0f), new Vector3(0.74f, 0.74f, 0.4f), 0f).Sub(),
                    Floor(0f)), PropSlot.Stone),
                Piece(PShape("TowerWindow_Frame",
                    Torus(new Vector3(0f, 1.02f, 0.05f), 0.76f, 0.06f, new Vector3(90f, 0f, 0f), 0f),
                    Box(new Vector3(0f, 1.02f, 0.02f), new Vector3(0.74f, 0.025f, 0.025f), 0.01f, 0f),
                    Box(new Vector3(0f, 1.02f, 0.02f), new Vector3(0.025f, 0.74f, 0.025f), 0.01f, 0.01f),
                    Box(new Vector3(0f, 0.25f, 0.1f), new Vector3(0.55f, 0.035f, 0.1f), 0.015f, 0.02f)), PropSlot.Wood),
            };
            Add(window);

            // The window's sky pane (drawn by TowerHall with its own restoration: still grey, then bright blue).
            var sky = Recipe("TowerSky", new Vector2Int(2, 1), false, false, 500);
            sky.palette = Palette(KiteRed, MillWood, TowerStone, KiteTeal, WarmWindow,
                new PropColor(new Color(0.55f, 0.82f, 1f), new Color(0.6f, 0.62f, 0.66f), 0.9f));
            sky.resolution = 48;
            sky.outline = 0f;
            sky.pieces = new List<PropPiece>
            {
                Piece(PShape("TowerSky_Pane", Ell(new Vector3(0f, 1.02f, -0.05f), new Vector3(0.76f, 0.76f, 0.02f), 0f)), PropSlot.Accent),
            };
            Add(sky);

            // A cloud wisp for the calmed window (drifts across the pane).
            var wisp = Recipe("TowerWisp", new Vector2Int(1, 1), false, false, 400);
            wisp.palette = Palette(KiteRed, MillWood, TowerStone, KiteTeal, WarmWindow, new PropColor(Color.white, new Color(0.85f, 0.86f, 0.88f), 0.5f));
            wisp.resolution = 40;
            wisp.outline = 0f;
            wisp.pieces = new List<PropPiece>
            {
                Piece(PShape("TowerWisp_Puff",
                    Ell(new Vector3(0f, 0f, 0f), new Vector3(0.16f, 0.07f, 0.015f), 0f),
                    Ell(new Vector3(0.1f, 0.03f, 0f), new Vector3(0.09f, 0.06f, 0.015f), 0.04f),
                    Ell(new Vector3(-0.11f, 0.02f, 0f), new Vector3(0.08f, 0.05f, 0.015f), 0.04f)), PropSlot.Accent),
            };
            Add(wisp);

            // A beam jutting out of the back wall, tangled kite strings and torn kite scraps hanging from it.
            var beam = Recipe("TowerBeam", new Vector2Int(1, 1), false, false, 2400);
            beam.palette = Stones();
            beam.resolution = 64;
            beam.outline = 0.014f;
            var strings = new List<SdfPrimitive>();
            var rnd = new System.Random(7);
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            for (int s = 0; s < 3; s++)
            {
                float x = (s - 1) * 0.22f, z = -0.25f + s * 0.25f;
                var at = new Vector3(x, 1.68f, z);
                float bottom = R(0.55f, 0.85f);
                for (int i = 0; i < 6; i++)
                {
                    var next = new Vector3(x + R(-0.08f, 0.08f), Mathf.Lerp(1.68f, bottom, (i + 1) / 6f), z + R(-0.06f, 0.06f));
                    strings.Add(Cone(at, next, 0.011f, 0.011f, 0.004f));
                    at = next;
                }
                // A tangled ball halfway down.
                var knot = new Vector3(x, Mathf.Lerp(1.68f, bottom, 0.5f), z);
                strings.Add(Torus(knot, 0.05f, 0.01f, new Vector3(R(0f, 90f), R(0f, 90f), 0f), 0.004f));
                strings.Add(Torus(knot, 0.045f, 0.01f, new Vector3(R(0f, 90f), 0f, R(0f, 90f)), 0.004f));
            }
            beam.pieces = new List<PropPiece>
            {
                Piece(PShape("TowerBeam_Wood",
                    Box(new Vector3(0f, 1.74f, -0.05f), new Vector3(0.06f, 0.07f, 0.56f), 0.015f, 0f)), PropSlot.Wood),
                Piece(PShape("TowerBeam_Strings", strings.ToArray()), PropSlot.Stone, PropMotion.Sway, 0.02f),
                Piece(PShape("TowerBeam_ScrapRed",
                    Box(new Vector3(-0.2f, 0.85f, -0.25f), new Vector3(0.09f, 0.11f, 0.008f), 0.004f, 0f, new Vector3(0f, 20f, 40f)),
                    Box(new Vector3(0.24f, 0.95f, 0.25f), new Vector3(0.07f, 0.09f, 0.008f), 0.004f, 0f, new Vector3(0f, -30f, 35f))), PropSlot.Clay, PropMotion.Sway, 0.03f),
                Piece(PShape("TowerBeam_ScrapTeal",
                    Box(new Vector3(0.02f, 1.05f, 0f), new Vector3(0.08f, 0.1f, 0.008f), 0.004f, 0f, new Vector3(0f, 10f, -45f))), PropSlot.Leaf, PropMotion.Sway, 0.03f),
            };
            Add(beam);

            // A heap of tangled kites on the floor.
            var heap = Recipe("TangledKites", new Vector2Int(1, 1), true, true, 1400);
            heap.palette = Stones();
            heap.resolution = 56;
            heap.outline = 0.014f;
            heap.pieces = new List<PropPiece>
            {
                Piece(PShape("TangledKites_Red", KiteDiamond(new Vector3(-0.08f, 0.1f, 0.02f), Quaternion.Euler(-12f, 30f, 8f), 0.2f, 0.26f, 0.17f, 0.07f, 0.012f, 0, 0).ToArray()), PropSlot.Clay),
                Piece(PShape("TangledKites_Teal", KiteDiamond(new Vector3(0.12f, 0.16f, -0.08f), Quaternion.Euler(-35f, -50f, -15f), 0.18f, 0.24f, 0.15f, 0.06f, 0.012f, 0, 0).ToArray()), PropSlot.Leaf),
                Piece(PShape("TangledKites_String",
                    Torus(new Vector3(0f, 0.06f, 0.05f), 0.16f, 0.01f, new Vector3(8f, 0f, 12f), 0.004f),
                    Torus(new Vector3(0.05f, 0.1f, -0.02f), 0.12f, 0.01f, new Vector3(70f, 30f, 0f), 0.004f),
                    Torus(new Vector3(-0.06f, 0.09f, 0.04f), 0.1f, 0.01f, new Vector3(40f, -60f, 20f), 0.004f),
                    Floor(0f)), PropSlot.Stone),
                Piece(PShape("TangledKites_Spars",
                    Cone(new Vector3(-0.25f, 0.03f, 0.2f), new Vector3(0.25f, 0.2f, -0.15f), 0.014f, 0.014f, 0f),
                    Cone(new Vector3(0.2f, 0.02f, 0.22f), new Vector3(-0.1f, 0.18f, -0.22f), 0.014f, 0.014f, 0f)), PropSlot.Wood),
            };
            Add(heap);

            // The perch (2 x 1): a thick wooden bar on two legs, Galeowl sits on it (KeeperHall.keeperLift).
            var perch = Recipe("TowerPerch", new Vector2Int(2, 1), true, false, 1200);
            perch.palette = Stones();
            perch.resolution = 56;
            perch.pieces = new List<PropPiece>
            {
                Piece(PShape("TowerPerch_Wood",
                    Cone(new Vector3(-0.75f, 0.45f, 0.55f), new Vector3(0.75f, 0.45f, 0.55f), 0.07f, 0.07f, 0f).Bumps(0.008f, 12f),
                    Cone(new Vector3(-0.62f, 0f, 0.55f), new Vector3(-0.62f, 0.45f, 0.55f), 0.06f, 0.05f, 0.03f).Mirror(),
                    Box(new Vector3(-0.62f, 0.04f, 0.55f), new Vector3(0.1f, 0.04f, 0.22f), 0.02f, 0.03f).Mirror(),
                    Floor(0f)), PropSlot.Wood),
                Piece(PShape("TowerPerch_Rope",
                    Torus(new Vector3(-0.45f, 0.45f, 0.55f), 0.075f, 0.012f, new Vector3(0f, 0f, 90f), 0f).Mirror()), PropSlot.Accent),
            };
            Add(perch);

            var door = Recipe("TowerHallDoor", new Vector2Int(2, 1), false, false, 1200);
            door.palette = Stones();
            door.pieces = new List<PropPiece>
            {
                Piece(PShape("TowerHallDoor_Arch",
                    Torus(new Vector3(0f, 1.5f, 0.3f), 0.95f, 0.13f, new Vector3(90f, 0f, 0f), 0f),
                    Plane(new Vector3(0f, 1.48f, 0f), Vector3.down, 0f),
                    Box(new Vector3(0.95f, 0.76f, 0.3f), new Vector3(0.14f, 0.76f, 0.14f), 0.03f, 0.05f).Mirror(),
                    Floor(0f)), PropSlot.Stone),
            };
            Add(door);
            return all;
        }
    }
}
