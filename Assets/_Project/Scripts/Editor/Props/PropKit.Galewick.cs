using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Diorama.EditorTools.StarterKit;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 10a Galewick props (windmill highlands, RealmsDesign.md + concept sheet 01): big windmill (2 x 2, sails spin
    /// with the restored wind), small mill, cottage (2 x 2), dry-stone wall (turn it with the legend), kite lying in the
    /// grass (three colour swaps of one shape set), kite post (its kite only flies once restored), hay bale, low cloud
    /// puff, wind tower (2 x 2, vanes at the crown) and the hillock that stands in for trees (rolling green-gold hills
    /// without tile heights). Fence and signpost are Cinderwood's. Shape units are tiles, +Z faces the player.
    /// </summary>
    public static partial class PropKit
    {
        // ---- Galewick palette (RealmsDesign) ----
        static readonly PropColor CloudStone = C("#D9D4C7");
        static readonly PropColor Slate = C("#7E8A93");
        static readonly PropColor SailWhite = C("#F4F1E6");
        static readonly PropColor MillWood = C("#8A6440");
        static readonly PropColor Meadow = C("#8DBF5E");
        static readonly PropColor Wheat = C("#E3C66F");
        static readonly PropColor KiteRed = C("#E0644A");
        static readonly PropColor KiteTeal = C("#3FA7A3");
        static readonly PropColor KiteGold = C("#E8C84A");
        static readonly PropColor WarmWindow = C("#FFD27A", 1f);

        /// <summary>The kite colour swaps (recipe names): Kite (red), KiteTeal, KiteGold.</summary>
        public static readonly string[] KiteNames = { "Kite", "KiteTeal", "KiteGold" };

        /// <summary>Sails' hub on the big windmill, in tiles from the footprint centre (Windmill piece position).</summary>
        public static readonly Vector3 MillHub = new Vector3(0f, 2.25f, 0.62f);

        /// <summary>Writes the 10a props and the Galewick theme. Returns them by name (not baked).</summary>
        public static Dictionary<string, PropRecipe> WriteGalewickProps()
        {
            DioramaSceneSetup.EnsureFolder(Root);
            DioramaSceneSetup.EnsureFolder($"{Root}/Shapes");
            DioramaSceneSetup.EnsureFolder($"{Root}/Themes");
            var all = new Dictionary<string, PropRecipe>();
            void Add(PropRecipe r) => all[r.name] = r;

            Add(Windmill());
            Add(SmallMill());
            Add(Cottage());
            Add(StoneWall());
            foreach (var k in Kites()) Add(k);
            Add(KitePost());
            Add(HayBale());
            Add(CloudPuff());
            Add(WindTower());
            Add(Hillock());
            Add(MeadowTree());

            WriteGalewickTheme(all["Hillock"], all["MeadowTree"]);
            foreach (var r in all.Values) EditorUtility.SetDirty(r);
            return all;
        }

        // ---- mills ----

        /// <summary>Four sail arms in the XY plane round the origin (an X), canvas or spars.</summary>
        static SdfPrimitive[] Sails(float reach, float width, bool canvas)
        {
            var list = new List<SdfPrimitive>();
            foreach (float a in new[] { 45f, 135f, 225f, 315f })
            {
                var dir = Quaternion.Euler(0f, 0f, a) * Vector3.right;
                if (canvas)
                {
                    // The canvas sits on one side of the spar, from a third of the way out to the tip.
                    var side = Quaternion.Euler(0f, 0f, a + 90f) * Vector3.right;
                    var c = dir * (reach * 0.62f) + side * (width * 0.55f);
                    list.Add(Box(c, new Vector3(reach * 0.36f, width * 0.5f, 0.06f), 0.025f, list.Count == 0 ? 0f : 0.01f, new Vector3(0f, 0f, a)));
                }
                else
                {
                    list.Add(Cone(Vector3.zero, dir * reach, 0.05f, 0.035f, 0.02f));
                    // Cross slats along the arm.
                    for (int i = 1; i <= 3; i++)
                    {
                        var side = Quaternion.Euler(0f, 0f, a + 90f) * Vector3.right;
                        var at = dir * (reach * (0.25f + 0.24f * i));
                        list.Add(Cone(at - side * 0.03f, at + side * width * 1.05f, 0.022f, 0.022f, 0.01f));
                    }
                }
            }
            if (!canvas) list.Add(Sphere(new Vector3(0f, 0f, 0.03f), 0.11f, 0.03f));
            return list.ToArray();
        }

        /// <summary>
        /// Big windmill, 2 x 2: a round cloud-stone tower narrowing upward, a slate cap, a wooden door and gallery, and four
        /// white sails on a hub facing the player (+Z). The sails spin with the restored wind (PropMotion.Spin); the village's
        /// big mill (step 10b) draws its own sails turned by flicks.
        /// </summary>
        static PropRecipe Windmill()
        {
            var mill = Recipe("Windmill", new Vector2Int(2, 2), true, false, 3000);
            mill.palette = Palette(CloudStone, MillWood, Slate, Meadow, WarmWindow, SailWhite);
            mill.yawJitter = 0f;
            mill.scaleJitter = 0f;
            mill.resolution = 64;
            mill.pieces = new List<PropPiece>
            {
                Piece(PShape("Windmill_Tower",
                    Cone(new Vector3(0f, 0f, 0f), new Vector3(0f, 2.05f, 0f), 0.7f, 0.46f, 0f).Bumps(0.012f, 9f),
                    Torus(new Vector3(0f, 0.05f, 0f), 0.7f, 0.06f, Vector3.zero, 0.02f),
                    Ell(new Vector3(0f, 0.32f, 0.66f), new Vector3(0.19f, 0.32f, 0.12f), 0.02f).Sub(),
                    Floor(0f)), PropSlot.Clay),
                Piece(PShape("Windmill_Cap",
                    Ell(new Vector3(0f, 2.12f, 0f), new Vector3(0.56f, 0.12f, 0.56f), 0f),
                    Cone(new Vector3(0f, 2.15f, 0f), new Vector3(0f, 2.75f, 0f), 0.5f, 0.06f, 0.12f),
                    Cone(new Vector3(0f, 2.2f, 0.3f), new Vector3(0f, 2.25f, 0.62f), 0.1f, 0.08f, 0.04f)), PropSlot.Stone),
                Piece(PShape("Windmill_Wood",
                    Ell(new Vector3(0f, 0.3f, 0.62f), new Vector3(0.18f, 0.3f, 0.05f), 0f),
                    Torus(new Vector3(0f, 1.15f, 0f), 0.58f, 0.04f, Vector3.zero, 0f),
                    Floor(0f)), PropSlot.Wood),
                Piece(PShape("Windmill_Window",
                    Ell(new Vector3(0.36f, 1.45f, 0.42f), new Vector3(0.08f, 0.11f, 0.06f), 0f),
                    Ell(new Vector3(-0.3f, 0.85f, 0.55f), new Vector3(0.07f, 0.1f, 0.06f), 0f)), PropSlot.Glow, PropMotion.Flicker, 0.002f),
                Piece(PShape("Windmill_Spars", Sails(1.25f, 0.2f, false)), PropSlot.Wood, PropMotion.Spin, 1f, MillHub),
                Piece(PShape("Windmill_Sails", Sails(1.25f, 0.2f, true)), PropSlot.Accent, PropMotion.Spin, 1f, MillHub + new Vector3(0f, 0f, -0.02f)),
                Piece(PShape("Windmill_Tufts", GrassTufts(0.72f, 7)), PropSlot.Leaf, PropMotion.Sway, 0.03f),
            };
            return mill;
        }

        /// <summary>Small mill, 1 x 1: the same build at about half size (two in the village, flickable for fun in 10b).</summary>
        static PropRecipe SmallMill()
        {
            var mill = Recipe("SmallMill", new Vector2Int(1, 1), true, false, 1800);
            mill.palette = Palette(CloudStone, MillWood, Slate, Meadow, WarmWindow, SailWhite);
            mill.yawJitter = 8f;
            mill.scaleJitter = 0.05f;
            mill.resolution = 60;
            var hub = new Vector3(0f, 1.32f, 0.34f);
            mill.pieces = new List<PropPiece>
            {
                Piece(PShape("SmallMill_Tower",
                    Cone(new Vector3(0f, 0f, 0f), new Vector3(0f, 1.2f, 0f), 0.38f, 0.25f, 0f).Bumps(0.01f, 12f),
                    Ell(new Vector3(0f, 0.2f, 0.36f), new Vector3(0.11f, 0.2f, 0.08f), 0.02f).Sub(),
                    Floor(0f)), PropSlot.Clay),
                Piece(PShape("SmallMill_Cap",
                    Cone(new Vector3(0f, 1.2f, 0f), new Vector3(0f, 1.6f, 0f), 0.31f, 0.04f, 0f),
                    Cone(new Vector3(0f, 1.28f, 0.15f), new Vector3(0f, 1.32f, 0.34f), 0.06f, 0.05f, 0.03f)), PropSlot.Stone),
                Piece(PShape("SmallMill_Door",
                    Ell(new Vector3(0f, 0.18f, 0.33f), new Vector3(0.1f, 0.18f, 0.04f), 0f),
                    Floor(0f)), PropSlot.Wood),
                Piece(PShape("SmallMill_Spars", Sails(0.68f, 0.12f, false)), PropSlot.Wood, PropMotion.Spin, 1.3f, hub),
                Piece(PShape("SmallMill_Sails", Sails(0.68f, 0.12f, true)), PropSlot.Accent, PropMotion.Spin, 1.3f, hub + new Vector3(0f, 0f, -0.015f)),
            };
            return mill;
        }

        // ---- village ----

        /// <summary>Whitewashed cottage, 2 x 2: pitched slate roof with the ridge across, a chimney, a red door, lit windows.</summary>
        static PropRecipe Cottage()
        {
            var cottage = Recipe("Cottage", new Vector2Int(2, 2), true, false, 2400);
            cottage.palette = Palette(C("#EFE6D2"), MillWood, CloudStone, C("#6F7F8E"), WarmWindow, KiteRed);
            cottage.yawJitter = 6f;
            cottage.scaleJitter = 0.05f;
            cottage.resolution = 48;
            cottage.pieces = new List<PropPiece>
            {
                Piece(PShape("Cottage_Walls",
                    Box(new Vector3(0f, 0.42f, 0f), new Vector3(0.72f, 0.42f, 0.55f), 0.05f, 0f),
                    Ell(new Vector3(0f, 0.3f, 0.56f), new Vector3(0.17f, 0.3f, 0.06f), 0.01f).Sub(),
                    Floor(0f)), PropSlot.Clay),
                Piece(PShape("Cottage_Roof",
                    Box(new Vector3(0f, 0.86f, 0f), new Vector3(0.82f, 0.48f, 0.48f), 0.04f, 0f, new Vector3(45f, 0f, 0f)),
                    Plane(new Vector3(0f, 0.86f, 0f), Vector3.down, 0.02f)), PropSlot.Leaf),
                Piece(PShape("Cottage_Base",
                    Box(new Vector3(0f, 0.05f, 0f), new Vector3(0.78f, 0.05f, 0.61f), 0.03f, 0f),
                    Cone(new Vector3(-0.48f, 0.9f, -0.18f), new Vector3(-0.48f, 1.55f, -0.18f), 0.11f, 0.1f, 0f),
                    Sphere(new Vector3(-0.48f, 1.6f, -0.18f), 0.07f, 0.02f).Sub()), PropSlot.Stone),
                Piece(PShape("Cottage_Door",
                    Ell(new Vector3(0f, 0.28f, 0.53f), new Vector3(0.16f, 0.28f, 0.05f), 0f),
                    Floor(0f)), PropSlot.Accent),
                Piece(PShape("Cottage_Windows",
                    Box(new Vector3(0.45f, 0.45f, 0.55f), new Vector3(0.11f, 0.1f, 0.03f), 0.02f, 0f),
                    Box(new Vector3(-0.45f, 0.45f, 0.55f), new Vector3(0.11f, 0.1f, 0.03f), 0.02f, 0f)), PropSlot.Glow, PropMotion.Flicker, 0.001f),
                Piece(PShape("Cottage_Smoke", SmokePuff(0.07f)), PropSlot.Accent, PropMotion.Steam, 0.05f, new Vector3(-0.48f, 1.68f, -0.18f)),
            };
            return cottage;
        }

        /// <summary>Dry-stone wall along X (stones reach the tile edges so neighbours join), a cap row and a little moss.</summary>
        static PropRecipe StoneWall()
        {
            var wall = Recipe("StoneWall", new Vector2Int(1, 1), true, false, 900);
            wall.palette = Palette(CloudStone, MillWood, C("#B3B0A6"), Meadow, WarmWindow, C("#9FA39A"));
            wall.yawJitter = 0f;
            wall.scaleJitter = 0.04f;
            wall.outline = 0.018f;
            var stones = new List<SdfPrimitive>
            {
                Box(new Vector3(0f, 0.16f, 0f), new Vector3(0.52f, 0.16f, 0.15f), 0.06f, 0f).Bumps(0.025f, 7f),
                Floor(0f),
            };
            var caps = new List<SdfPrimitive>();
            foreach (var (x, z, r) in new[] { (-0.38f, 0.01f, 0.1f), (-0.15f, -0.02f, 0.11f), (0.08f, 0.02f, 0.1f), (0.31f, -0.01f, 0.11f), (0.5f, 0f, 0.09f) })
                caps.Add(Ell(new Vector3(x, 0.34f, z), new Vector3(r * 1.2f, r * 0.6f, r), 0.04f));
            wall.pieces = new List<PropPiece>
            {
                Piece(PShape("StoneWall_Stones", stones.ToArray()), PropSlot.Stone),
                Piece(PShape("StoneWall_Caps", caps.ToArray()), PropSlot.Accent),
                Piece(PShape("StoneWall_Moss",
                    Ell(new Vector3(0.1f, 0.38f, 0f), new Vector3(0.22f, 0.04f, 0.09f), 0f).Bumps(0.01f, 25f)), PropSlot.Leaf),
            };
            return wall;
        }

        // ---- kites ----

        /// <summary>Diamond kite lying almost flat in the grass, its tail of four bows curling away (+X).</summary>
        static IEnumerable<PropRecipe> Kites()
        {
            var sail = PShape("Kite_Sail",
                Ell(new Vector3(0f, 0.05f, -0.02f), new Vector3(0.24f, 0.025f, 0.34f), 0f, new Vector3(-6f, 45f, 0f)),
                Box(new Vector3(0f, 0.05f, -0.02f), new Vector3(0.2f, 0.02f, 0.2f), 0.02f, 0f, new Vector3(-6f, 45f, 0f)));
            var spars = PShape("Kite_Spars",
                Cone(new Vector3(0f, 0.08f, -0.32f), new Vector3(0f, 0.04f, 0.28f), 0.018f, 0.018f, 0f),
                Cone(new Vector3(-0.24f, 0.07f, -0.08f), new Vector3(0.24f, 0.07f, -0.08f), 0.016f, 0.016f, 0.01f));
            var tail = new List<SdfPrimitive>();
            for (int i = 0; i < 4; i++)
            {
                float t = (i + 1) / 4f;
                var at = new Vector3(0.08f + t * 0.36f, 0.03f, 0.3f + Mathf.Sin(t * 3f) * 0.08f);
                tail.Add(Ell(at + new Vector3(-0.03f, 0f, 0f), new Vector3(0.045f, 0.025f, 0.03f), i == 0 ? 0f : 0.01f, new Vector3(0f, 30f, 0f)));
                tail.Add(Ell(at + new Vector3(0.03f, 0f, 0f), new Vector3(0.045f, 0.025f, 0.03f), 0.01f, new Vector3(0f, -30f, 0f)));
            }
            var bows = PShape("Kite_Bows", tail.ToArray());
            var colours = new[] { (KiteRed, KiteGold), (KiteTeal, KiteRed), (KiteGold, KiteTeal) };
            for (int i = 0; i < KiteNames.Length; i++)
            {
                var kite = Recipe(KiteNames[i], new Vector2Int(1, 1), false, false, 700);
                kite.palette = Palette(colours[i].Item1, MillWood, CloudStone, Meadow, WarmWindow, colours[i].Item2);
                kite.yawJitter = 30f;
                kite.scaleJitter = 0.06f;
                kite.outline = 0.016f;
                kite.resolution = 48;
                kite.pieces = new List<PropPiece>
                {
                    Piece(sail, PropSlot.Clay),
                    Piece(spars, PropSlot.Wood),
                    Piece(bows, PropSlot.Accent),
                };
                yield return kite;
            }
        }

        /// <summary>A tall pole with a kite tied on: the kite (and its tail) only shows and flies once Galewick is restored.</summary>
        static PropRecipe KitePost()
        {
            var post = Recipe("KitePost", new Vector2Int(1, 1), true, false, 1100);
            post.palette = Palette(KiteRed, MillWood, CloudStone, Meadow, WarmWindow, KiteGold);
            post.yawJitter = 25f;
            post.resolution = 48;
            var kite = new Vector3(0.35f, 2.15f, 0.1f);
            post.pieces = new List<PropPiece>
            {
                Piece(PShape("KitePost_Pole",
                    Cone(new Vector3(0f, 0f, 0f), new Vector3(0f, 1.55f, 0f), 0.05f, 0.035f, 0f),
                    Ell(new Vector3(0f, 0.03f, 0f), new Vector3(0.12f, 0.05f, 0.12f), 0.05f),
                    Sphere(new Vector3(0f, 1.58f, 0f), 0.05f, 0.02f),
                    Floor(0f)), PropSlot.Wood),
                Piece(PShape("KitePost_Kite",
                    Box(Vector3.zero, new Vector3(0.18f, 0.18f, 0.02f), 0.02f, 0f, new Vector3(0f, 0f, 45f)),
                    Ell(new Vector3(0f, 0.05f, 0f), new Vector3(0.17f, 0.3f, 0.025f), 0.02f)), PropSlot.Clay, PropMotion.Fly, 0.1f, kite),
                Piece(PShape("KitePost_Tail",
                    Ell(new Vector3(0f, -0.36f, 0f), new Vector3(0.05f, 0.03f, 0.02f), 0f),
                    Ell(new Vector3(0.04f, -0.52f, 0f), new Vector3(0.05f, 0.03f, 0.02f), 0.01f),
                    Ell(new Vector3(0.02f, -0.68f, 0f), new Vector3(0.05f, 0.03f, 0.02f), 0.01f),
                    Cone(new Vector3(0f, -0.25f, 0f), new Vector3(0.02f, -0.7f, 0f), 0.012f, 0.01f, 0.01f)), PropSlot.Accent, PropMotion.Fly, 0.1f, kite),
            };
            return post;
        }

        // ---- fields ----

        /// <summary>Round hay bale lying on its side, a wrapped band, a few loose strands.</summary>
        static PropRecipe HayBale()
        {
            var bale = Recipe("HayBale", new Vector2Int(1, 1), true, false, 700);
            bale.palette = Palette(Wheat, MillWood, CloudStone, C("#C9A94E"), WarmWindow, KiteRed);
            bale.yawJitter = 50f;
            bale.pieces = new List<PropPiece>
            {
                Piece(PShape("HayBale_Body",
                    Cone(new Vector3(-0.3f, 0.3f, 0f), new Vector3(0.3f, 0.3f, 0f), 0.3f, 0.3f, 0f).Bumps(0.012f, 18f),
                    Floor(0f)), PropSlot.Clay),
                Piece(PShape("HayBale_Ends",
                    Ell(new Vector3(0.305f, 0.3f, 0f), new Vector3(0.012f, 0.25f, 0.25f), 0f).Mirror()), PropSlot.Leaf),
                Piece(PShape("HayBale_Band",
                    Torus(new Vector3(0f, 0.3f, 0f), 0.305f, 0.02f, new Vector3(0f, 0f, 90f), 0f)), PropSlot.Accent),
            };
            return bale;
        }

        /// <summary>A low cloud puff (soft toon blob). Parked while Galewick is still; drifts a little once restored (sway).</summary>
        static PropRecipe CloudPuff()
        {
            var puff = Recipe("CloudPuff", new Vector2Int(1, 1), true, true, 900);
            puff.palette = Palette(C("#FBFAF5"), MillWood, C("#DCE6EE"), Meadow, WarmWindow, C("#EEF2F6"));
            puff.outline = 0.014f;
            puff.spec = 0.05f;
            puff.scaleJitter = 0.15f;
            puff.pieces = new List<PropPiece>
            {
                Piece(PShape("CloudPuff_Body",
                    Sphere(new Vector3(0f, 0.5f, 0f), 0.3f),
                    Sphere(new Vector3(0.36f, 0.4f, 0.04f), 0.22f, 0.05f),
                    Sphere(new Vector3(-0.38f, 0.42f, -0.03f), 0.23f, 0.05f),
                    Sphere(new Vector3(0.16f, 0.7f, 0.02f), 0.2f, 0.05f),
                    Sphere(new Vector3(-0.16f, 0.66f, -0.04f), 0.19f, 0.05f),
                    Sphere(new Vector3(0.05f, 0.42f, 0.26f), 0.2f, 0.05f),
                    Sphere(new Vector3(-0.05f, 0.44f, -0.26f), 0.2f, 0.05f),
                    Plane(new Vector3(0f, 0.3f, 0f), Vector3.down, 0.06f)), PropSlot.Clay, PropMotion.Sway, 0.12f),
                Piece(PShape("CloudPuff_Under",
                    Ell(new Vector3(0f, 0.32f, 0f), new Vector3(0.56f, 0.07f, 0.4f), 0f)), PropSlot.Stone, PropMotion.Sway, 0.12f),
            };
            return puff;
        }

        // ---- the temple ----

        /// <summary>
        /// Wind tower, 2 x 2: a tall round cloud-stone tower with a crenellated crown, an arched door to the player (dark,
        /// closed until step 10b opens it), slit windows and wooden vanes on the crown that spin once the wind blows.
        /// </summary>
        static PropRecipe WindTower()
        {
            var tower = Recipe("WindTower", new Vector2Int(2, 2), true, false, 3200);
            tower.palette = Palette(CloudStone, MillWood, Slate, Meadow, WarmWindow, C("#3B3A3F"));
            tower.yawJitter = 0f;
            tower.scaleJitter = 0f;
            tower.resolution = 60;
            var crown = new List<SdfPrimitive>
            {
                Cone(new Vector3(0f, 0f, 0f), new Vector3(0f, 3.6f, 0f), 0.78f, 0.62f, 0f).Bumps(0.015f, 7f),
                Torus(new Vector3(0f, 0.06f, 0f), 0.78f, 0.08f, Vector3.zero, 0.03f),
                Cone(new Vector3(0f, 3.55f, 0f), new Vector3(0f, 3.95f, 0f), 0.74f, 0.74f, 0.03f),
                Cone(new Vector3(0f, 3.7f, 0f), new Vector3(0f, 4.1f, 0f), 0.6f, 0.6f, 0f).Sub(),
                Ell(new Vector3(0f, 0.45f, 0.76f), new Vector3(0.28f, 0.48f, 0.2f), 0.02f).Sub(),
                Floor(0f),
            };
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f + 30f;
                var d = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                crown.Add(Box(d * 0.68f + Vector3.up * 4.0f, new Vector3(0.16f, 0.2f, 0.2f), 0.03f, 0.03f, new Vector3(0f, a, 0f)).Sub());
            }
            var slits = new List<SdfPrimitive>();
            foreach (var (a, y) in new[] { (30f, 1.6f), (-35f, 2.4f), (10f, 3.05f) })
            {
                var d = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                slits.Add(Box(d * 0.66f + Vector3.up * y, new Vector3(0.05f, 0.16f, 0.06f), 0.02f, 0f, new Vector3(0f, a, 0f)));
            }
            var hub = new Vector3(0f, 3.25f, 0.72f);
            tower.pieces = new List<PropPiece>
            {
                Piece(PShape("WindTower_Stone", crown.ToArray()), PropSlot.Clay),
                Piece(PShape("WindTower_Door",
                    Ell(new Vector3(0f, 0.42f, 0.6f), new Vector3(0.27f, 0.44f, 0.06f), 0f),
                    Floor(0f)), PropSlot.Accent),
                Piece(PShape("WindTower_Frame",
                    Torus(new Vector3(0f, 0.45f, 0.72f), 0.3f, 0.05f, new Vector3(90f, 0f, 0f), 0f),
                    Plane(new Vector3(0f, 0.4f, 0f), Vector3.down, 0f),
                    Cone(new Vector3(0.3f, 0f, 0.72f), new Vector3(0.3f, 0.45f, 0.72f), 0.05f, 0.05f, 0f),
                    Cone(new Vector3(-0.3f, 0f, 0.72f), new Vector3(-0.3f, 0.45f, 0.72f), 0.05f, 0.05f, 0f),
                    Floor(0f)), PropSlot.Wood),
                Piece(PShape("WindTower_Slits", slits.ToArray()), PropSlot.Glow, PropMotion.Flicker, 0.001f),
                Piece(PShape("WindTower_Vanes", Sails(0.85f, 0.16f, false)), PropSlot.Wood, PropMotion.Spin, 0.7f, hub),
                Piece(PShape("WindTower_VaneCloth", Sails(0.85f, 0.16f, true)), PropSlot.Accent, PropMotion.Spin, 0.7f, hub + new Vector3(0f, 0f, -0.02f)),
            };
            return tower;
        }

        // ---- the hills ----

        /// <summary>
        /// Rolling green-gold hillock (Galewick's theme tree, on F tiles): a low grassy dome a bit wider than its tile so a
        /// row reads as a hill, wheat tufts on top. Blocks walking like trees.
        /// </summary>
        static PropRecipe Hillock()
        {
            var hill = Recipe("Hillock", new Vector2Int(1, 1), true, true, 520);
            hill.palette = Palette(CloudStone, MillWood, CloudStone, Meadow, Wheat, C("#B9CF6E"));
            hill.tips = new PropTips { on = true, onSlot = PropSlot.Leaf, tipSlot = PropSlot.Accent, at = 0.55f, byHeight = 0.7f };
            hill.scaleJitter = 0.18f;
            hill.outline = 0.016f;
            hill.pieces = new List<PropPiece>
            {
                Piece(PShape("Hillock_Dome",
                    Ell(new Vector3(0f, 0f, 0f), new Vector3(0.66f, 0.62f, 0.62f)).Bumps(0.03f, 4f),
                    Ell(new Vector3(0.22f, 0.12f, 0.1f), new Vector3(0.4f, 0.48f, 0.4f), 0.15f).Bumps(0.03f, 4f),
                    Floor(0f)), PropSlot.Leaf),
                Piece(PShape("Hillock_Tufts", GrassTufts(0.42f, 5, 0.4f)), PropSlot.Glow, PropMotion.Sway, 0.04f),
            };
            return hill;
        }

        /// <summary>The hub's calm tree in Galewick greens (a palette swap: same shapes, meadow canopy, gold tips).</summary>
        static PropRecipe MeadowTree()
        {
            var calm = Load("CalmTree");
            var tree = Recipe("MeadowTree", new Vector2Int(1, 1), true, true, 900);
            if (calm != null) tree.pieces = new List<PropPiece>(calm.pieces);
            else Debug.LogError("[PropKit] CalmTree missing (hub props): MeadowTree has no shape.");
            tree.palette = Palette(CloudStone, C("#6B5A48"), CloudStone, C("#86B95A"), Wheat, C("#B9D77A"));
            tree.tips = new PropTips { on = true, onSlot = PropSlot.Leaf, tipSlot = PropSlot.Accent, at = 0.55f, byHeight = 0.8f };
            tree.scaleJitter = 0.12f;
            return tree;
        }

        /// <summary>Thin wheat grass tufts on a ring of <paramref name="radius"/> at ground level (or <paramref name="y"/>).</summary>
        static SdfPrimitive[] GrassTufts(float radius, int count, float y = 0f)
        {
            var list = new List<SdfPrimitive>();
            for (int i = 0; i < count; i++)
            {
                float a = i * 2.39996f;
                var at = new Vector3(Mathf.Cos(a) * radius * (0.6f + 0.4f * ((i * 7) % 3) / 2f), y, Mathf.Sin(a) * radius);
                list.Add(Cone(at, at + new Vector3(0.03f, 0.22f, 0.02f), 0.035f, 0.008f, i == 0 ? 0f : 0.02f));
                list.Add(Cone(at + new Vector3(0.04f, 0f, 0f), at + new Vector3(0.1f, 0.17f, -0.02f), 0.03f, 0.006f, 0.02f));
            }
            return list.ToArray();
        }

        // ---- theme ----

        /// <summary>
        /// Galewick island colours (RealmsDesign palette): meadow green, wheat tall grass, pale cloud-stone base. Unrestored is
        /// the bright everyday look but still (theme.stillWhenUnrestored); restored a touch warmer, grass waving, sails turning,
        /// seeds drifting. Trees are hillocks with a round meadow tree mixed in.
        /// </summary>
        static void WriteGalewickTheme(PropRecipe hillock, PropRecipe tree)
        {
            var theme = Asset<RealmTheme>($"{Root}/Themes/Galewick.asset");
            var baseLook = new ThemeColors
            {
                grass = Hex("#9CCB6A"), tallGrass = Hex("#C8B85E"), path = Hex("#E4DCC4"),
                water = Hex("#6FB6D8"), hotSpring = Hex("#7FD0E0"),
                shortBase = Hex("#6E9E4A"), shortTip = Hex("#BCD97C"), tallBase = Hex("#A49A46"), tallTip = Hex("#E3C66F"),
                dirt = Hex("#CFCBC1"), underside = Hex("#9AA2A8"),
                trunk = Hex("#6B5A48"), leavesLow = Hex("#7FB55A"), leavesHigh = Hex("#A8D06E"), rock = Hex("#AEB3B4"),
                leafGlow = 0f, outlineTint = Color.white,
            };
            theme.unrestored = baseLook;
            theme.restored = Map(baseLook, Warmer, 0.1f);
            theme.startRestored = 0f;
            theme.treeProp = hillock;
            theme.treePropAlt = tree;
            theme.treeAltShare = 0.22f;
            theme.frontBorderTrees = true;
            theme.embers = 0f;
            theme.leaves = 0f;
            theme.seeds = 1f;
            theme.stillWhenUnrestored = 0.92f;
            theme.windSpin = 1.4f;
            theme.bladeHeight = 1.3f;
            theme.bladeWidth = 0.72f;
            EditorUtility.SetDirty(theme);
        }
    }
}
