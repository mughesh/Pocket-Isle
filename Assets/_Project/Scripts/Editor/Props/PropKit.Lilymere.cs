using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Diorama.EditorTools.StarterKit;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 11a Lilymere props (lily lagoon, RealmsDesign.md + concept sheet 01): stilt hut (2 x 2, on posts), plank
    /// bridge (the trail over the shallows, the jetty), reed clump with cattails (also the woods' second shape), lily pad
    /// (one shape, sizes from the scale jitter), lotus (a closed bud; the open flower only shows once restored), frozen
    /// waterfall and running waterfall (2 x 1; 11b swaps them), stone basin (2 x 2, dry), lake temple (3 x 2, half-sunk,
    /// moss roof, round door) and stepping stones. The woods are the hub's calm tree in willow greens (LagoonTree).
    /// Shape units are tiles, +Z faces the player; y 0 is the cap top (water sits 0.1 below).
    /// </summary>
    public static partial class PropKit
    {
        // ---- Lilymere palette (step 11a prompt) ----
        static readonly PropColor LagoonTeal = C("#5FB8C4");
        static readonly PropColor LagoonDeep = C("#2F7F95");
        static readonly PropColor LilyGreen = C("#5AA868");
        static readonly PropColor LotusPink = C("#F2A7B8");
        static readonly PropColor Reed = C("#B7B26A");
        static readonly PropColor Sand = C("#E8D8B0");
        static readonly PropColor StiltWood = C("#8B6A4A");
        static readonly PropColor TempleStone = C("#A9B8B0");
        static readonly PropColor LagoonMoss = C("#6E9A5E");
        static readonly PropColor Ice = C("#D8F1F4", 0.15f);
        static readonly PropColor LagoonWindow = C("#FFD98A", 1f);

        /// <summary>Writes the 11a props and the Lilymere theme. Returns them by name (not baked).</summary>
        public static Dictionary<string, PropRecipe> WriteLilymereProps()
        {
            DioramaSceneSetup.EnsureFolder(Root);
            DioramaSceneSetup.EnsureFolder($"{Root}/Shapes");
            DioramaSceneSetup.EnsureFolder($"{Root}/Themes");
            var all = new Dictionary<string, PropRecipe>();
            void Add(PropRecipe r) => all[r.name] = r;

            Add(StiltHut());
            Add(PlankBridge());
            Add(ReedClump());
            Add(LilyPad());
            Add(Lotus());
            Add(Waterfall(false));
            Add(Waterfall(true));
            Add(StoneBasin());
            Add(LakeTemple());
            Add(SteppingStone());
            Add(LagoonTree());
            // Step 11b: the puzzle gates' covers and the pieces' own views.
            Add(ChannelGap());
            Add(BedGap());
            Add(LotusStone());
            Add(WaterBall());
            Add(BasinWater());
            // Step 12c: the cliff with the carved bowl and the fall from its lip.
            Add(CliffBowl());
            Add(BowlFall(false));
            Add(BowlFall(true));

            WriteLilymereTheme(all["LagoonTree"], all["ReedClump"]);
            foreach (var r in all.Values) EditorUtility.SetDirty(r);
            return all;
        }

        /// <summary>Stilt hut, 2 x 2: a plank deck on four posts over the bank, plank walls, a reed thatch roof, a warm window.</summary>
        static PropRecipe StiltHut()
        {
            var hut = Recipe("StiltHut", new Vector2Int(2, 2), true, false, 2600);
            hut.palette = Palette(Sand, StiltWood, TempleStone, Reed, LagoonWindow, LilyGreen);
            hut.yawJitter = 8f;
            hut.resolution = 40;
            var posts = new List<SdfPrimitive>();
            foreach (var (x, z) in new[] { (-0.62f, -0.55f), (0.62f, -0.55f), (-0.62f, 0.55f), (0.62f, 0.55f) })
                posts.Add(Cone(new Vector3(x, -0.12f, z), new Vector3(x, 0.52f, z), 0.07f, 0.06f, posts.Count == 0 ? 0f : 0.02f));
            posts.Add(Box(new Vector3(0f, 0.55f, 0.05f), new Vector3(0.78f, 0.04f, 0.72f), 0.02f, 0.02f));
            posts.Add(Box(new Vector3(0f, 0.3f, 0.82f), new Vector3(0.18f, 0.03f, 0.12f), 0.01f, 0.02f, new Vector3(-28f, 0f, 0f)));
            hut.pieces = new List<PropPiece>
            {
                Piece(PShape("StiltHut_Posts", posts.ToArray()), PropSlot.Wood),
                Piece(PShape("StiltHut_Walls",
                    Box(new Vector3(0f, 0.95f, -0.05f), new Vector3(0.58f, 0.38f, 0.5f), 0.04f, 0f),
                    Box(new Vector3(0f, 0.82f, 0.46f), new Vector3(0.14f, 0.24f, 0.06f), 0.02f, 0.01f).Sub()), PropSlot.Clay),
                Piece(PShape("StiltHut_Roof",
                    Cone(new Vector3(0f, 1.25f, -0.05f), new Vector3(0f, 1.95f, -0.05f), 0.86f, 0.06f, 0f)), PropSlot.Leaf, PropMotion.Sway, 0.02f),
                Piece(PShape("StiltHut_Window",
                    Box(new Vector3(0.32f, 1.0f, 0.45f), new Vector3(0.1f, 0.09f, 0.03f), 0.02f, 0f)), PropSlot.Glow),
            };
            return hut;
        }

        /// <summary>Plank bridge, 1 x 1 (not blocking): four planks across the tile on two rails, low posts at the sides.</summary>
        static PropRecipe PlankBridge()
        {
            var plank = Recipe("PlankBridge", new Vector2Int(1, 1), false, false, 600);
            plank.palette = Palette(Sand, StiltWood, TempleStone, Reed, LagoonWindow, LilyGreen);
            plank.yawJitter = 0f;
            plank.scaleJitter = 0f;
            plank.outline = 0.016f;
            var planks = new List<SdfPrimitive>();
            for (int i = 0; i < 4; i++)
            {
                float z = -0.36f + i * 0.24f;
                planks.Add(Box(new Vector3((i % 2) * 0.03f, 0.05f, z), new Vector3(0.52f, 0.025f, 0.1f), 0.015f, planks.Count == 0 ? 0f : 0.005f, new Vector3(0f, (i - 1.5f) * 2f, 0f)));
            }
            plank.pieces = new List<PropPiece>
            {
                Piece(PShape("PlankBridge_Planks", planks.ToArray()), PropSlot.Clay),
                Piece(PShape("PlankBridge_Rails",
                    Box(new Vector3(0f, 0.01f, -0.2f), new Vector3(0.5f, 0.02f, 0.03f), 0.01f, 0f),
                    Box(new Vector3(0f, 0.01f, 0.2f), new Vector3(0.5f, 0.02f, 0.03f), 0.01f, 0f),
                    Cone(new Vector3(0.52f, -0.15f, 0.42f), new Vector3(0.52f, 0.16f, 0.42f), 0.035f, 0.03f, 0f),
                    Cone(new Vector3(-0.52f, -0.15f, -0.42f), new Vector3(-0.52f, 0.16f, -0.42f), 0.035f, 0.03f, 0.01f)), PropSlot.Wood),
            };
            return plank;
        }

        /// <summary>Reed clump, 1 x 1: a tuft of tall reeds with three brown cattail heads; sways.</summary>
        static PropRecipe ReedClump()
        {
            var reeds = Recipe("ReedClump", new Vector2Int(1, 1), true, true, 900);
            reeds.palette = Palette(Sand, C("#7A5A3A"), TempleStone, Reed, LagoonWindow, C("#8FAE5A"));
            reeds.scaleJitter = 0.18f;
            reeds.outline = 0.014f;
            var blades = new List<SdfPrimitive>();
            var heads = new List<SdfPrimitive>();
            for (int i = 0; i < 9; i++)
            {
                float a = i * 2.39996f, r = 0.08f + 0.18f * ((i * 5) % 7) / 6f;
                var at = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                float h = 0.7f + 0.35f * ((i * 3) % 5) / 4f;
                var top = at + new Vector3(Mathf.Cos(a) * 0.08f, h, Mathf.Sin(a) * 0.08f);
                blades.Add(Cone(at, top, 0.035f, 0.008f, blades.Count == 0 ? 0f : 0.02f));
                if (i % 3 == 0) heads.Add(Ell(top + new Vector3(0f, -0.12f, 0f), new Vector3(0.04f, 0.1f, 0.04f), heads.Count == 0 ? 0f : 0.01f));
            }
            reeds.pieces = new List<PropPiece>
            {
                Piece(PShape("ReedClump_Blades", blades.ToArray()), PropSlot.Leaf, PropMotion.Sway, 0.08f),
                Piece(PShape("ReedClump_Heads", heads.ToArray()), PropSlot.Wood, PropMotion.Sway, 0.1f),
            };
            return reeds;
        }

        /// <summary>Lily pad, 1 x 1 (on water, not blocking): a flat disc with a notch, a thin rim; sizes from the scale jitter.</summary>
        static PropRecipe LilyPad()
        {
            var pad = Recipe("LilyPad", new Vector2Int(1, 1), false, true, 500);
            pad.palette = Palette(Sand, StiltWood, TempleStone, LilyGreen, LagoonWindow, C("#7CC27E"));
            pad.yawJitter = 180f;
            pad.scaleJitter = 0.35f;
            pad.outline = 0.012f;
            pad.spec = 0.3f;
            pad.pieces = new List<PropPiece>
            {
                Piece(PShape("LilyPad_Disc",
                    Ell(new Vector3(0f, -0.07f, 0f), new Vector3(0.34f, 0.025f, 0.34f), 0f),
                    Box(new Vector3(0.22f, -0.07f, 0f), new Vector3(0.16f, 0.06f, 0.03f), 0f, 0.01f, new Vector3(0f, 0f, 0f)).Sub()), PropSlot.Leaf, PropMotion.Sway, 0.02f),
                Piece(PShape("LilyPad_Vein",
                    Torus(new Vector3(0f, -0.05f, 0f), 0.2f, 0.012f, Vector3.zero, 0f)), PropSlot.Accent, PropMotion.Sway, 0.02f),
            };
            return pad;
        }

        /// <summary>
        /// Lotus, 1 x 1 (on water or the dry garden bed, not blocking): a pad and a closed pink bud; the open flower
        /// (petals round a gold heart) only shows once the realm is restored (PropMotion.Fly with no flutter).
        /// </summary>
        static PropRecipe Lotus()
        {
            var lotus = Recipe("Lotus", new Vector2Int(1, 1), false, true, 1000);
            lotus.palette = Palette(LotusPink, StiltWood, TempleStone, LilyGreen, C("#FFD45A", 0.5f), C("#FBD3DC"));
            lotus.scaleJitter = 0.2f;
            lotus.outline = 0.014f;
            lotus.resolution = 32;
            var petals = new List<SdfPrimitive>();
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                petals.Add(Ell(new Vector3(0f, 0.2f, 0f) + dir * 0.12f, new Vector3(0.07f, 0.05f, 0.13f), petals.Count == 0 ? 0f : 0.02f,
                    new Vector3(-25f, -a * Mathf.Rad2Deg + 90f, 0f)));
            }
            lotus.pieces = new List<PropPiece>
            {
                Piece(PShape("Lotus_Pad",
                    Ell(new Vector3(0.05f, -0.07f, 0.04f), new Vector3(0.3f, 0.022f, 0.3f), 0f),
                    Cone(new Vector3(0f, -0.08f, 0f), new Vector3(0f, 0.12f, 0f), 0.025f, 0.02f, 0.01f)), PropSlot.Leaf, PropMotion.Sway, 0.02f),
                Piece(PShape("Lotus_Bud",
                    Ell(new Vector3(0f, 0.22f, 0f), new Vector3(0.07f, 0.12f, 0.07f), 0f),
                    Cone(new Vector3(0f, 0.3f, 0f), new Vector3(0f, 0.38f, 0f), 0.04f, 0.005f, 0.03f)), PropSlot.Clay, PropMotion.Sway, 0.03f),
                Piece(PShape("Lotus_Open", petals.ToArray()), PropSlot.Accent, PropMotion.Fly, 0f),
                Piece(PShape("Lotus_Heart", Sphere(new Vector3(0f, 0.24f, 0f), 0.05f, 0f)), PropSlot.Glow, PropMotion.Fly, 0f),
            };
            return lotus;
        }

        /// <summary>
        /// Waterfall, 2 x 1 on the cliff face: a stone lip, a sheet of water down to a foot pool. Frozen = a pale glassy
        /// sheet with stopped droplets; running = a teal sheet that sways, foam at its foot and mist (11b swaps them).
        /// </summary>
        static PropRecipe Waterfall(bool running)
        {
            var fall = Recipe(running ? "WaterfallRunning" : "WaterfallFrozen", new Vector2Int(2, 1), true, false, 2200);
            fall.palette = Palette(running ? LagoonTeal : Ice, StiltWood, TempleStone, LagoonMoss, C("#FFFFFF", 0.3f), running ? C("#F2FBFC") : C("#BFE6EC", 0.1f));
            fall.yawJitter = 0f;
            fall.scaleJitter = 0f;
            fall.resolution = 56;
            fall.spec = running ? 0.4f : 0.6f;
            var sheet = new List<SdfPrimitive>
            {
                Box(new Vector3(0f, 1.3f, -0.18f), new Vector3(0.62f, 1.1f, 0.06f), 0.05f, 0f).Bumps(running ? 0.01f : 0.02f, running ? 14f : 6f),
                Ell(new Vector3(0f, 0.1f, 0.05f), new Vector3(0.8f, 0.12f, 0.42f), 0.08f),
            };
            var bits = new List<SdfPrimitive>();
            if (running)
            {
                foreach (var x in new[] { -0.5f, -0.15f, 0.2f, 0.5f })
                    bits.Add(Sphere(new Vector3(x, 0.22f, 0.3f), 0.13f, bits.Count == 0 ? 0f : 0.04f));
            }
            else
            {
                // Stopped droplets hang in the air; a few cracks are left to the bumps.
                foreach (var (x, y, z) in new[] { (-0.4f, 1.6f, 0.05f), (0.35f, 1.2f, 0.08f), (-0.1f, 0.8f, 0.1f), (0.5f, 0.55f, 0.12f), (-0.55f, 0.45f, 0.1f) })
                    bits.Add(Ell(new Vector3(x, y, z), new Vector3(0.05f, 0.08f, 0.05f), bits.Count == 0 ? 0f : 0.005f));
            }
            fall.pieces = new List<PropPiece>
            {
                Piece(PShape(running ? "WaterfallRunning_Sheet" : "WaterfallFrozen_Sheet", sheet.ToArray()), PropSlot.Clay, running ? PropMotion.Sway : PropMotion.None, running ? 0.03f : 0f),
                Piece(PShape("Waterfall_Lip",
                    Box(new Vector3(0f, 2.42f, -0.22f), new Vector3(0.86f, 0.14f, 0.24f), 0.08f, 0f).Bumps(0.02f, 8f),
                    Sphere(new Vector3(-0.85f, 1.6f, -0.3f), 0.32f, 0.12f),
                    Sphere(new Vector3(0.85f, 1.4f, -0.3f), 0.34f, 0.12f),
                    Sphere(new Vector3(0.8f, 0.4f, -0.2f), 0.26f, 0.1f)), PropSlot.Stone),
                Piece(PShape("Waterfall_Moss",
                    Ell(new Vector3(-0.5f, 2.55f, -0.18f), new Vector3(0.3f, 0.06f, 0.18f), 0f),
                    Ell(new Vector3(0.45f, 2.57f, -0.2f), new Vector3(0.28f, 0.05f, 0.16f), 0.03f)), PropSlot.Leaf),
                Piece(PShape(running ? "WaterfallRunning_Foam" : "WaterfallFrozen_Drops", bits.ToArray()), PropSlot.Accent,
                    running ? PropMotion.Steam : PropMotion.None, running ? 0.06f : 0f),
            };
            return fall;
        }

        /// <summary>
        /// Step 12c (Oct 27 notes: "mountains and an empty bowl kind of dent over it, and the waterfall on that is
        /// frozen"): a rocky cliff, 2 x 1, about 2.5 tiles tall, with craggy shoulders and a carved empty bowl on top (a dry
        /// cracked sand floor, a mossy rim) and a notch in its front lip where the fall leaves it. Front = +z (the player).
        /// </summary>
        static PropRecipe CliffBowl()
        {
            var cliff = Recipe("CliffBowl", new Vector2Int(2, 1), true, false, 2600);
            cliff.palette = Palette(Sand, StiltWood, C("#8E9AA6"), LagoonMoss, LagoonWindow, C("#6F7B86"));
            cliff.yawJitter = 0f;
            cliff.scaleJitter = 0f;
            cliff.resolution = 60;
            cliff.pieces = new List<PropPiece>
            {
                Piece(PShape("CliffBowl_Rock",
                    Box(new Vector3(0f, 1.25f, -0.05f), new Vector3(0.9f, 1.25f, 0.4f), 0.22f, 0f).Bumps(0.04f, 5f),
                    Sphere(new Vector3(-0.78f, 1.95f, -0.05f), 0.42f, 0.15f),
                    Sphere(new Vector3(0.8f, 1.7f, -0.1f), 0.46f, 0.15f),
                    Sphere(new Vector3(0.66f, 0.55f, 0.18f), 0.38f, 0.12f),
                    Sphere(new Vector3(-0.7f, 0.6f, 0.18f), 0.36f, 0.12f),
                    // The carved bowl on top, and the notch in its front lip.
                    Ell(new Vector3(0f, 2.56f, 0.02f), new Vector3(0.5f, 0.32f, 0.3f), 0.04f).Sub(),
                    Box(new Vector3(0f, 2.42f, 0.36f), new Vector3(0.15f, 0.12f, 0.14f), 0.04f, 0.02f).Sub(),
                    Floor(0f)), PropSlot.Stone),
                Piece(PShape("CliffBowl_Floor",
                    Ell(new Vector3(0f, 2.25f, 0.02f), new Vector3(0.42f, 0.035f, 0.24f), 0f).Bumps(0.012f, 16f)), PropSlot.Clay),
                Piece(PShape("CliffBowl_Moss",
                    Ell(new Vector3(-0.62f, 2.5f, -0.18f), new Vector3(0.28f, 0.06f, 0.18f), 0f),
                    Ell(new Vector3(0.58f, 2.48f, -0.2f), new Vector3(0.3f, 0.05f, 0.16f), 0.03f),
                    Ell(new Vector3(0.86f, 1.15f, 0.26f), new Vector3(0.14f, 0.24f, 0.1f), 0.03f)), PropSlot.Leaf),
            };
            return cliff;
        }

        /// <summary>
        /// Step 12c: the fall from the cliff bowl's lip, 2 x 1 (drawn by ScoopPour over the cliff): a narrow sheet from the
        /// notch down the cliff's front to a splash at its foot. Frozen = pale glassy ice with stopped droplets; running =
        /// a teal sheet that sways, foam and mist at the foot.
        /// </summary>
        static PropRecipe BowlFall(bool running)
        {
            var fall = Recipe(running ? "BowlFallRunning" : "BowlFallFrozen", new Vector2Int(2, 1), false, false, 1600);
            fall.palette = Palette(running ? LagoonTeal : Ice, StiltWood, TempleStone, LagoonMoss, C("#FFFFFF", 0.3f), running ? C("#F2FBFC") : C("#BFE6EC", 0.1f));
            fall.yawJitter = 0f;
            fall.scaleJitter = 0f;
            fall.resolution = 56;
            fall.spec = running ? 0.4f : 0.6f;
            var sheet = new List<SdfPrimitive>
            {
                Ell(new Vector3(0f, 2.36f, 0.4f), new Vector3(0.17f, 0.07f, 0.1f), 0f),
                Box(new Vector3(0f, 1.25f, 0.48f), new Vector3(0.16f, 1.1f, 0.05f), 0.05f, 0.04f).Bumps(running ? 0.01f : 0.02f, running ? 14f : 6f),
                Ell(new Vector3(0f, 0.12f, 0.62f), new Vector3(0.34f, 0.12f, 0.22f), 0.08f),
            };
            var bits = new List<SdfPrimitive>();
            if (running)
            {
                foreach (var x in new[] { -0.28f, 0f, 0.28f })
                    bits.Add(Sphere(new Vector3(x, 0.2f, 0.7f), 0.12f, bits.Count == 0 ? 0f : 0.04f));
            }
            else
            {
                foreach (var (x, y, z) in new[] { (-0.26f, 1.7f, 0.56f), (0.24f, 1.25f, 0.58f), (-0.2f, 0.8f, 0.6f), (0.28f, 0.5f, 0.64f) })
                    bits.Add(Ell(new Vector3(x, y, z), new Vector3(0.045f, 0.075f, 0.045f), bits.Count == 0 ? 0f : 0.005f));
            }
            fall.pieces = new List<PropPiece>
            {
                Piece(PShape(running ? "BowlFallRunning_Sheet" : "BowlFallFrozen_Sheet", sheet.ToArray()), PropSlot.Clay, running ? PropMotion.Sway : PropMotion.None, running ? 0.03f : 0f),
                Piece(PShape(running ? "BowlFallRunning_Foam" : "BowlFallFrozen_Drops", bits.ToArray()), PropSlot.Accent,
                    running ? PropMotion.Steam : PropMotion.None, running ? 0.06f : 0f),
            };
            return fall;
        }

        /// <summary>Step 12c: writes and bakes the cliff bowl and its two falls (the 12c setup; the full kit writes them too).</summary>
        internal static void WriteCliffBowl()
        {
            foreach (var r in new[] { CliffBowl(), BowlFall(false), BowlFall(true) })
            {
                EditorUtility.SetDirty(r);
                AssetDatabase.SaveAssets();
                PropBaker.Bake(r);
            }
        }

        /// <summary>Stone basin, 2 x 2: a round stone ring with a mossy lip and a dry cracked sand floor (11b fills it).</summary>
        static PropRecipe StoneBasin()
        {
            var basin = Recipe("StoneBasin", new Vector2Int(2, 2), true, false, 1800);
            basin.palette = Palette(Sand, StiltWood, TempleStone, LagoonMoss, LagoonWindow, LagoonTeal);
            basin.yawJitter = 0f;
            basin.scaleJitter = 0f;
            basin.resolution = 56;
            basin.pieces = new List<PropPiece>
            {
                Piece(PShape("StoneBasin_Ring",
                    Cone(new Vector3(0f, 0f, 0f), new Vector3(0f, 0.32f, 0f), 0.86f, 0.8f, 0f).Bumps(0.015f, 9f),
                    Cone(new Vector3(0f, 0.12f, 0f), new Vector3(0f, 0.5f, 0f), 0.66f, 0.66f, 0.03f).Sub(),
                    Floor(0f)), PropSlot.Stone),
                Piece(PShape("StoneBasin_Floor",
                    Ell(new Vector3(0f, 0.12f, 0f), new Vector3(0.66f, 0.03f, 0.66f), 0f).Bumps(0.012f, 16f)), PropSlot.Clay),
                Piece(PShape("StoneBasin_Moss",
                    Torus(new Vector3(0f, 0.32f, 0f), 0.74f, 0.05f, Vector3.zero, 0f).Bumps(0.02f, 10f)), PropSlot.Leaf),
            };
            return basin;
        }

        /// <summary>
        /// Lake temple, 3 x 2: a squat stone shrine half-sunk in the lagoon shore (its base below ground), pillars, a mossy
        /// dome roof with a lotus finial, a round door facing the player (dark until the lagoon rises, 11c).
        /// </summary>
        static PropRecipe LakeTemple()
        {
            var temple = Recipe("LakeTemple", new Vector2Int(3, 2), true, false, 3800);
            temple.palette = Palette(LotusPink, StiltWood, TempleStone, LagoonMoss, C("#9FE8F0", 0.8f), C("#5E6E68"));
            temple.yawJitter = 0f;
            temple.scaleJitter = 0f;
            temple.resolution = 40;
            var pillars = new List<SdfPrimitive>();
            foreach (var x in new[] { -1.05f, -0.45f, 0.45f, 1.05f })
                pillars.Add(Cone(new Vector3(x, -0.2f, 0.55f), new Vector3(x, 1.05f, 0.55f), 0.11f, 0.09f, pillars.Count == 0 ? 0f : 0.02f));
            temple.pieces = new List<PropPiece>
            {
                Piece(PShape("LakeTemple_Body",
                    Box(new Vector3(0f, 0.45f, -0.1f), new Vector3(1.2f, 0.7f, 0.72f), 0.08f, 0f),
                    Box(new Vector3(0f, 1.15f, -0.1f), new Vector3(1.32f, 0.08f, 0.82f), 0.04f, 0.04f),
                    Ell(new Vector3(0f, 0.38f, 0.64f), new Vector3(0.34f, 0.34f, 0.2f), 0.02f).Sub(),
                    Floor(-0.25f)), PropSlot.Stone),
                Piece(PShape("LakeTemple_Pillars", pillars.ToArray()), PropSlot.Stone),
                Piece(PShape("LakeTemple_Roof",
                    Ell(new Vector3(0f, 1.25f, -0.1f), new Vector3(1.1f, 0.48f, 0.7f), 0f),
                    Ell(new Vector3(-0.9f, 1.2f, 0.5f), new Vector3(0.3f, 0.1f, 0.2f), 0.06f)), PropSlot.Leaf, PropMotion.Sway, 0.01f),
                Piece(PShape("LakeTemple_Door",
                    Torus(new Vector3(0f, 0.38f, 0.66f), 0.3f, 0.05f, new Vector3(90f, 0f, 0f), 0f),
                    Ell(new Vector3(0f, 0.38f, 0.6f), new Vector3(0.27f, 0.27f, 0.04f), 0.02f)), PropSlot.Accent),
                Piece(PShape("LakeTemple_Finial",
                    Ell(new Vector3(0f, 1.78f, -0.1f), new Vector3(0.12f, 0.16f, 0.12f), 0f),
                    Cone(new Vector3(0f, 1.88f, -0.1f), new Vector3(0f, 2.0f, -0.1f), 0.06f, 0.005f, 0.03f)), PropSlot.Clay),
                Piece(PShape("LakeTemple_Glow",
                    Sphere(new Vector3(0f, 0.38f, 0.62f), 0.08f, 0f)), PropSlot.Glow),
            };
            return temple;
        }

        /// <summary>Stepping stones, 1 x 1 (not blocking): three flat rounded stones in a short line.</summary>
        static PropRecipe SteppingStone()
        {
            var stones = Recipe("SteppingStone", new Vector2Int(1, 1), false, true, 600);
            stones.palette = Palette(Sand, StiltWood, TempleStone, LagoonMoss, LagoonWindow, LilyGreen);
            stones.outline = 0.014f;
            stones.pieces = new List<PropPiece>
            {
                Piece(PShape("SteppingStone_Stones",
                    Ell(new Vector3(-0.25f, 0.02f, -0.2f), new Vector3(0.17f, 0.05f, 0.14f), 0f),
                    Ell(new Vector3(0.05f, 0.02f, 0.05f), new Vector3(0.15f, 0.05f, 0.13f), 0.02f),
                    Ell(new Vector3(0.3f, 0.02f, 0.27f), new Vector3(0.13f, 0.045f, 0.12f), 0.02f)), PropSlot.Stone),
            };
            return stones;
        }

        /// <summary>
        /// Step 11b: open water over the channel crossing (1 x 1, blocks): a flat teal sheet just above the path, so the
        /// crossing reads as channel until the lily pads lock in (the piece hides it and opens the tiles).
        /// </summary>
        static PropRecipe ChannelGap()
        {
            var gap = Recipe("ChannelGap", new Vector2Int(1, 1), true, false, 200);
            gap.palette = Palette(LagoonTeal, StiltWood, TempleStone, LilyGreen, LagoonWindow, LagoonDeep);
            gap.yawJitter = 0f;
            gap.scaleJitter = 0f;
            gap.outline = 0f;
            gap.spec = 0.5f;
            gap.resolution = 24;
            gap.pieces = new List<PropPiece>
            {
                Piece(PShape("ChannelGap_Sheet", Box(new Vector3(0f, 0.015f, 0f), new Vector3(0.52f, 0.02f, 0.52f), 0.005f, 0f)), PropSlot.Clay),
            };
            return gap;
        }

        /// <summary>Step 12b: the drought's cracked mud (dry bed tiles, the garden crossing).</summary>
        public static Color DroughtMud => Hex("#A8774A");

        /// <summary>Step 11b: cracked dry bed over the garden crossing (1 x 1, blocks) until the lotus stones bloom.</summary>
        static PropRecipe BedGap()
        {
            var gap = Recipe("BedGap", new Vector2Int(1, 1), true, false, 300);
            gap.palette = Palette(new PropColor(DroughtMud, DroughtMud, 0f), StiltWood, C("#6E4A2C"), LilyGreen, LagoonWindow, C("#6E4A2C"));
            gap.yawJitter = 0f;
            gap.scaleJitter = 0f;
            gap.outline = 0f;
            gap.resolution = 28;
            gap.pieces = new List<PropPiece>
            {
                Piece(PShape("BedGap_Sand", Box(new Vector3(0f, 0.015f, 0f), new Vector3(0.52f, 0.02f, 0.52f), 0.005f, 0f).Bumps(0.008f, 14f)), PropSlot.Clay),
            };
            return gap;
        }

        /// <summary>Step 11b: a bloomed lotus as a stepping stone (big open flower on a wide pad, the petals always open).</summary>
        static PropRecipe LotusStone()
        {
            var stone = Recipe("LotusStone", new Vector2Int(1, 1), false, true, 900);
            stone.palette = Palette(LotusPink, StiltWood, TempleStone, LilyGreen, C("#FFD45A", 0.6f), C("#FBD3DC"));
            stone.scaleJitter = 0f;
            stone.outline = 0.014f;
            stone.resolution = 32;
            var petals = new List<SdfPrimitive>();
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                petals.Add(Ell(new Vector3(0f, 0.08f, 0f) + dir * 0.22f, new Vector3(0.12f, 0.05f, 0.2f), petals.Count == 0 ? 0f : 0.03f,
                    new Vector3(-12f, -a * Mathf.Rad2Deg + 90f, 0f)));
            }
            stone.pieces = new List<PropPiece>
            {
                Piece(PShape("LotusStone_Pad", Ell(new Vector3(0f, -0.04f, 0f), new Vector3(0.5f, 0.04f, 0.5f), 0f)), PropSlot.Leaf),
                Piece(PShape("LotusStone_Petals", petals.ToArray()), PropSlot.Clay),
                Piece(PShape("LotusStone_Heart", Sphere(new Vector3(0f, 0.12f, 0f), 0.08f, 0f)), PropSlot.Glow),
            };
            return stone;
        }

        /// <summary>Step 11b: the scooped ball of water (a glossy teal blob with bubbles; the piece wobbles and places it).</summary>
        static PropRecipe WaterBall()
        {
            var ball = Recipe("WaterBall", new Vector2Int(1, 1), false, false, 700);
            ball.palette = Palette(LagoonTeal, StiltWood, TempleStone, LilyGreen, C("#E8FBFF", 0.6f), C("#BFEFF4", 0.2f));
            ball.yawJitter = 0f;
            ball.scaleJitter = 0f;
            ball.outline = 0.012f;
            ball.spec = 0.9f;
            ball.resolution = 32;
            ball.pieces = new List<PropPiece>
            {
                Piece(PShape("WaterBall_Body", Sphere(new Vector3(0f, 0.5f, 0f), 0.45f, 0f)), PropSlot.Clay),
                Piece(PShape("WaterBall_Bubbles",
                    Sphere(new Vector3(0.18f, 0.62f, 0.3f), 0.07f, 0f),
                    Sphere(new Vector3(-0.15f, 0.42f, 0.32f), 0.05f, 0f),
                    Sphere(new Vector3(0.05f, 0.75f, 0.33f), 0.04f, 0f)), PropSlot.Glow),
            };
            return ball;
        }

        /// <summary>Step 11b: the water in the stone basin (a teal disc the piece raises a step per pour).</summary>
        static PropRecipe BasinWater()
        {
            var water = Recipe("BasinWater", new Vector2Int(2, 2), false, false, 300);
            water.palette = Palette(LagoonTeal, StiltWood, TempleStone, LilyGreen, C("#E8FBFF", 0.5f), LagoonDeep);
            water.yawJitter = 0f;
            water.scaleJitter = 0f;
            water.outline = 0f;
            water.spec = 0.7f;
            water.resolution = 32;
            water.pieces = new List<PropPiece>
            {
                Piece(PShape("BasinWater_Disc", Cone(new Vector3(0f, 0f, 0f), new Vector3(0f, 0.05f, 0f), 0.66f, 0.66f, 0f)), PropSlot.Clay),
            };
            return water;
        }

        /// <summary>The woods: the hub's calm tree in willow greens with pale tips.</summary>
        static PropRecipe LagoonTree()
        {
            var calm = Load("CalmTree");
            var tree = Recipe("LagoonTree", new Vector2Int(1, 1), true, true, 900);
            if (calm != null) tree.pieces = new List<PropPiece>(calm.pieces);
            else Debug.LogError("[PropKit] CalmTree missing (hub props): LagoonTree has no shape.");
            tree.palette = LagoonTreePalette();
            tree.tips = new PropTips { on = true, onSlot = PropSlot.Leaf, tipSlot = PropSlot.Accent, at = 0.55f, byHeight = 0.8f };
            tree.scaleJitter = 0.14f;
            return tree;
        }

        /// <summary>Step 12c: willow teal leaves with pale aqua tips, a cool grey-brown trunk (was Galewick's meadow green).</summary>
        static PropColor[] LagoonTreePalette() => Palette(TempleStone, C("#5E5650"), TempleStone, C("#3F8F8A"), Sand, C("#8FD0C4"));

        /// <summary>
        /// Step 12c (Oct 28 notes: Lilymere and Galewick shared one palette): the Lilymere theme and its willow trees in
        /// the wetland look, rebaked. Run by the 12c setup; the full Lilymere prop kit writes the same.
        /// </summary>
        internal static void WriteLilymereLook()
        {
            var tree = Load("LagoonTree");
            var reeds = Load("ReedClump");
            if (tree == null || reeds == null)
            {
                Debug.LogError("[PropKit] LagoonTree / ReedClump missing (run the Lilymere prop kit first).");
                return;
            }
            tree.palette = LagoonTreePalette();
            EditorUtility.SetDirty(tree);
            WriteLilymereTheme(tree, reeds);
            AssetDatabase.SaveAssets();
            PropBaker.Bake(tree);
        }

        static void WriteLilymereTheme(PropRecipe tree, PropRecipe reeds)
        {
            var theme = Asset<RealmTheme>($"{Root}/Themes/Lilymere.asset");
            var baseLook = new ThemeColors
            {
                // Step 12c: a wetland, not Galewick's meadow: teal-green grass and willows, blue-grey stones and reeds,
                // pale stone paths and deeper blue water so the lake reads apart from the grass. The dry lagoon bed is
                // drought mud (12b), so walkable reads apart and a cream rain cloud stands out against it.
                grass = Hex("#73C2A8"), tallGrass = Hex("#80B3A8"), path = Hex("#D9CFB5"),
                water = Hex("#4A93D6"), hotSpring = Hex("#7FD0E0"), dryBed = DroughtMud,
                shortBase = Hex("#428C82"), shortTip = Hex("#94DBC9"), tallBase = Hex("#4D7D80"), tallTip = Hex("#A8D4D1"),
                dirt = Hex("#B5B8B0"), underside = Hex("#7891A3"),
                trunk = Hex("#5C5A55"), leavesLow = Hex("#3D8A8C"), leavesHigh = Hex("#73C2BA"), rock = Hex("#9EB0C4"),
                leafGlow = 0f, outlineTint = Color.white,
            };
            // Unrestored: the lagoon is still and a little grey (the shader's ripples are off too).
            var still = baseLook;
            still.water = Color.Lerp(Hex("#4A93D6"), Hex("#9AA9AC"), 0.4f);
            theme.unrestored = still;
            theme.restored = Map(baseLook, Warmer, 0.1f);
            theme.restored.water = Hex("#4A93D6");
            theme.restored.dryBed = baseLook.dryBed;
            theme.startRestored = 0f;
            theme.treeProp = tree;
            theme.treePropAlt = reeds;
            theme.treeAltShare = 0.25f;
            theme.frontBorderTrees = true;
            theme.embers = 0f;
            theme.leaves = 0f;
            theme.seeds = 0f;
            theme.stillWhenUnrestored = 0.85f;
            theme.windSpin = 0f;
            theme.bladeHeight = 1.15f;
            theme.bladeWidth = 0.85f;
            theme.waterRipples = 1f;
            EditorUtility.SetDirty(theme);
        }
    }
}
