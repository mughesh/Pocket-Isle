using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Diorama.EditorTools.StarterKit;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Still Isle props (step 8): the statue (3 x 3), the three Shard Gates and Cinderwood's return gate (2 x 1 each),
    /// mossy boulder, hub stone lantern, calm tree, stepping stones, and the Still Isle theme. The hub is always calm
    /// (restored), so these palettes use the same colour for both states. Gate portals are drawn at runtime
    /// (ShardGates), so the openings here stay empty.
    /// </summary>
    public static partial class PropKit
    {
        // ---- Still Isle palette ----
        static PropColor H(string hex, float glow = 0f) => new PropColor(Hex(hex), Hex(hex), glow);

        static readonly PropColor HubStone = H("#C9C0AE");
        static readonly PropColor HubStoneDark = H("#9C968A");
        static readonly PropColor HubMoss = H("#7FA35E");
        static readonly PropColor HubWood = H("#7A5E48");
        static readonly PropColor HubGold = H("#FFD27A", 0.6f);
        static readonly PropColor HubCream = H("#EFE6D2");

        /// <summary>Writes the hub props and the Still Isle theme. Returns the props by name (not baked).</summary>
        public static Dictionary<string, PropRecipe> WriteHubProps(out RealmTheme theme)
        {
            DioramaSceneSetup.EnsureFolder(Root);
            DioramaSceneSetup.EnsureFolder($"{Root}/Shapes");
            DioramaSceneSetup.EnsureFolder($"{Root}/Themes");
            var all = new Dictionary<string, PropRecipe>();
            void Add(PropRecipe r) => all[r.name] = r;

            Add(Statue());
            var archShapes = ArchShapes();
            Add(FireGate(archShapes));
            Add(WaterGate());
            Add(WindGate());
            Add(ReturnGate(archShapes));

            // ---- mossy boulder ----
            var boulder = Recipe("MossBoulder", new Vector2Int(1, 1), true, true, 700);
            boulder.palette = Palette(HubStone, HubWood, HubStoneDark, HubMoss, HubGold, HubCream);
            boulder.scaleJitter = 0.18f;
            boulder.pieces = new List<PropPiece>
            {
                Piece(PShape("MossBoulder_Stone",
                    Ell(new Vector3(0f, 0.2f, 0f), new Vector3(0.36f, 0.27f, 0.32f)).Bumps(0.02f, 9f),
                    Sphere(new Vector3(0.2f, 0.12f, 0.16f), 0.16f, 0.1f).Bumps(0.015f, 12f),
                    Floor(0f)), PropSlot.Stone),
                Piece(PShape("MossBoulder_Moss",
                    Ell(new Vector3(-0.03f, 0.4f, -0.02f), new Vector3(0.27f, 0.09f, 0.24f)).Bumps(0.015f, 22f),
                    Ell(new Vector3(-0.22f, 0.3f, 0.1f), new Vector3(0.12f, 0.07f, 0.12f), 0.06f).Bumps(0.012f, 22f)), PropSlot.Leaf),
            };
            Add(boulder);

            // ---- stone lantern, hub colours (same shapes as Cinderwood's) ----
            var lanternStone = AssetDatabase.LoadAssetAtPath<SdfShape>($"{Root}/Shapes/StoneLantern_Stone.asset");
            var lanternLight = AssetDatabase.LoadAssetAtPath<SdfShape>($"{Root}/Shapes/StoneLantern_Light.asset");
            if (lanternStone == null || lanternLight == null)
                Debug.LogError("[PropKit] StoneLantern shapes missing (write the prop kit first): the hub lantern is empty.");
            var lantern = Recipe("HubLantern", new Vector2Int(1, 1), true, false, 900);
            lantern.palette = Palette(HubStone, HubWood, HubStone, HubMoss, H("#FFD98A", 0.9f), HubCream);
            lantern.pieces = new List<PropPiece>
            {
                Piece(lanternStone, PropSlot.Stone),
                Piece(lanternLight, PropSlot.Glow, PropMotion.Flicker, 0.0015f),
            };
            Add(lantern);

            // ---- calm tree: round, soft canopies (drawn on F tiles through the theme) ----
            var tree = Recipe("CalmTree", new Vector2Int(1, 1), true, true, 900);
            tree.palette = Palette(HubStone, H("#6B5442"), HubStone, H("#4E9A84"), H("#9FD8BC", 0.15f), HubCream);
            tree.tips = new PropTips { on = true, onSlot = PropSlot.Leaf, tipSlot = PropSlot.Glow, at = 0.7f };
            tree.scaleJitter = 0.14f;
            tree.pieces = new List<PropPiece>
            {
                Piece(PShape("CalmTree_Trunk",
                    Cone(new Vector3(0f, 0f, 0f), new Vector3(0f, 0.85f, 0f), 0.08f, 0.055f, 0f),
                    Ell(new Vector3(0f, 0.03f, 0f), new Vector3(0.13f, 0.05f, 0.13f), 0.08f),
                    Floor(0f)), PropSlot.Wood),
                Piece(PShape("CalmTree_Canopy",
                    Sphere(new Vector3(0f, 1.2f, 0f), 0.42f).Bumps(0.012f, 7f),
                    Sphere(new Vector3(0.26f, 1.02f, 0.1f), 0.3f, 0.18f).Bumps(0.012f, 7f),
                    Sphere(new Vector3(-0.27f, 1.06f, -0.06f), 0.3f, 0.18f).Bumps(0.012f, 7f),
                    Sphere(new Vector3(0.02f, 1.5f, -0.02f), 0.28f, 0.16f).Bumps(0.012f, 7f)), PropSlot.Leaf, PropMotion.Sway, 0.025f),
            };
            Add(tree);

            // ---- stepping stones (walk-through decor by the pond) ----
            var steps = Recipe("SteppingStones", new Vector2Int(1, 1), false, true, 400);
            steps.palette = Palette(HubStone, HubWood, HubStoneDark, HubMoss, HubGold, HubCream);
            steps.pieces = new List<PropPiece>
            {
                Piece(PShape("SteppingStones_Stone",
                    Ell(new Vector3(-0.18f, 0.02f, 0.14f), new Vector3(0.17f, 0.045f, 0.14f), 0.02f),
                    Ell(new Vector3(0.16f, 0.02f, -0.04f), new Vector3(0.15f, 0.04f, 0.13f), 0.02f),
                    Ell(new Vector3(-0.06f, 0.02f, -0.27f), new Vector3(0.12f, 0.035f, 0.1f), 0.02f),
                    Floor(0f)), PropSlot.Stone),
            };
            Add(steps);

            theme = Asset<RealmTheme>($"{Root}/Themes/StillIsle.asset");
            StillIsleColors(theme);
            theme.treeProp = tree;
            EditorUtility.SetDirty(theme);
            return all;
        }

        /// <summary>
        /// Calm and neutral: soft moss green grass, pale warm stone, cream paths, quiet blue water, teal-green leaves,
        /// soft gold accents, grey mossy base rock. Always restored; no embers, no falling leaves.
        /// </summary>
        static void StillIsleColors(RealmTheme theme)
        {
            var look = new ThemeColors
            {
                grass = Hex("#93B56E"), tallGrass = Hex("#5F9150"), path = Hex("#E9DFC4"),
                water = Hex("#6E9FC4"), hotSpring = Hex("#7FC6C0"),
                shortBase = Hex("#6E914F"), shortTip = Hex("#BCD68D"), tallBase = Hex("#3E6A3A"), tallTip = Hex("#9BC772"),
                dirt = Hex("#8E8B80"), underside = Hex("#5E625A"),
                trunk = Hex("#6B5442"), leavesLow = Hex("#4E9A84"), leavesHigh = Hex("#7CC2A2"), rock = Hex("#BDB4A3"),
                leafGlow = 0f, outlineTint = Color.white,
            };
            theme.restored = theme.unrestored = look;
            theme.startRestored = 1f;
            theme.embers = 0f;
            theme.frontBorderTrees = false;
            theme.leaves = 0f;
            theme.overrideLight = false;
        }

        // ---- statue ----

        /// <summary>Where the statue's chest sockets sit (shape units, statue origin), left to right: fire, water, wind.</summary>
        public static readonly Vector3[] StatueSockets =
        {
            new Vector3(0.24f, 1.03f, 0.49f), new Vector3(0f, 1.17f, 0.54f), new Vector3(-0.24f, 1.03f, 0.49f),
        };

        /// <summary>
        /// A seated, mossy stone creature, 3 x 3 tiles, on a round plinth: sitting upright like a patient old hound-deer,
        /// long soft ears drooping beside its head, a gentle snout, closed smiling eyes, straight front legs, tail curled
        /// round its side, a mossy shawl over the shoulders, faint gold light in a few cracks, and three empty round
        /// sockets on its chest with dim red / blue / white rims.
        /// </summary>
        static PropRecipe Statue()
        {
            var statue = Recipe("Statue", new Vector2Int(3, 3), true, false, 6000);
            statue.palette = Palette(H("#C8533E", 0.25f), H("#4F86C6", 0.25f), H("#BDB5A5"), HubMoss, H("#FFCB6B", 0.65f), H("#F2EEE6", 0.3f));
            statue.yawJitter = 0f;
            statue.scaleJitter = 0f;
            statue.resolution = 48;
            statue.outline = 0.03f;

            var stone = new List<SdfPrimitive>
            {
                Ell(new Vector3(0.5f, 0.42f, -0.25f), new Vector3(0.36f, 0.32f, 0.5f), 0.15f).Mirror(),
                Ell(new Vector3(0f, 1.0f, -0.05f), new Vector3(0.55f, 0.75f, 0.48f), 0.2f),
                Ell(new Vector3(0f, 1.1f, 0.22f), new Vector3(0.46f, 0.5f, 0.32f), 0.15f),
                Cone(new Vector3(0.26f, 0.95f, 0.3f), new Vector3(0.28f, 0.22f, 0.5f), 0.14f, 0.12f, 0.12f).Mirror(),
                Ell(new Vector3(0.28f, 0.11f, 0.58f), new Vector3(0.16f, 0.1f, 0.2f), 0.08f).Mirror(),
                Cone(new Vector3(0f, 1.5f, 0.05f), new Vector3(0f, 1.85f, 0.15f), 0.3f, 0.26f, 0.15f),
                Ell(new Vector3(0f, 2.0f, 0.18f), new Vector3(0.36f, 0.33f, 0.36f), 0.15f),
                Cone(new Vector3(0f, 1.95f, 0.4f), new Vector3(0f, 1.88f, 0.72f), 0.2f, 0.13f, 0.12f),
                Sphere(new Vector3(0f, 1.92f, 0.74f), 0.07f, 0.04f),
                Ell(new Vector3(0.37f, 1.8f, 0.08f), new Vector3(0.1f, 0.34f, 0.17f), 0.08f, new Vector3(0f, 0f, 14f)).Mirror(),
                Cone(new Vector3(-0.45f, 0.3f, -0.6f), new Vector3(-0.95f, 0.2f, 0f), 0.2f, 0.11f, 0.15f),
                Sphere(new Vector3(-0.95f, 0.2f, 0.22f), 0.12f, 0.12f),
                // Closed, smiling eyes and a small mouth line (carved).
                Ell(new Vector3(0.17f, 2.05f, 0.49f), new Vector3(0.09f, 0.025f, 0.07f), 0.01f, new Vector3(0f, 0f, -15f)).Sub().Mirror(),
                Ell(new Vector3(0f, 1.8f, 0.66f), new Vector3(0.08f, 0.02f, 0.06f), 0.01f).Sub(),
                Floor(0.1f),
            };
            foreach (var s in StatueSockets) stone.Add(Sphere(s + new Vector3(0f, 0f, 0.03f), 0.08f, 0.02f).Sub());

            statue.pieces = new List<PropPiece>
            {
                Piece(PShape("Statue_Stone", stone.ToArray()), PropSlot.Stone),
                Piece(PShape("Statue_Plinth",
                    Ell(new Vector3(0f, 0.04f, 0f), new Vector3(1.3f, 0.1f, 1.3f), 0f).Bumps(0.006f, 10f),
                    Floor(0f)), PropSlot.Stone),
                Piece(PShape("Statue_Moss",
                    Ell(new Vector3(0f, 1.52f, -0.06f), new Vector3(0.6f, 0.15f, 0.5f)).Bumps(0.015f, 18f),
                    Ell(new Vector3(0f, 2.3f, 0.12f), new Vector3(0.24f, 0.08f, 0.24f)).Bumps(0.012f, 18f),
                    Ell(new Vector3(0.45f, 0.72f, -0.3f), new Vector3(0.2f, 0.08f, 0.3f), 0.06f).Bumps(0.012f, 18f),
                    Ell(new Vector3(0.85f, 0.13f, 0.75f), new Vector3(0.32f, 0.05f, 0.22f), 0.05f).Bumps(0.012f, 20f),
                    Ell(new Vector3(-0.7f, 0.13f, -0.85f), new Vector3(0.36f, 0.05f, 0.22f), 0.05f).Bumps(0.012f, 20f)), PropSlot.Leaf),
                Piece(PShape("Statue_Cracks",
                    Ell(new Vector3(-0.12f, 2.24f, 0.4f), new Vector3(0.02f, 0.11f, 0.04f), 0f, new Vector3(-30f, 0f, 22f)),
                    Ell(new Vector3(0.42f, 1.05f, 0.25f), new Vector3(0.022f, 0.17f, 0.05f), 0f, new Vector3(0f, -50f, -20f)),
                    Ell(new Vector3(0.47f, 0.86f, 0.18f), new Vector3(0.02f, 0.09f, 0.05f), 0f, new Vector3(0f, -50f, 30f)),
                    Ell(new Vector3(-0.5f, 1.15f, 0.05f), new Vector3(0.02f, 0.14f, 0.05f), 0f, new Vector3(0f, 60f, 18f)),
                    Ell(new Vector3(-0.55f, 0.12f, 1.12f), new Vector3(0.18f, 0.03f, 0.04f), 0f, new Vector3(0f, -20f, 0f))), PropSlot.Glow, PropMotion.Flicker, 0.001f),
                Piece(PShape("Statue_RimFire", Torus(StatueSockets[0] + new Vector3(0f, 0f, 0.02f), 0.085f, 0.018f, new Vector3(90f, 0f, 0f), 0f)), PropSlot.Clay),
                Piece(PShape("Statue_RimWater", Torus(StatueSockets[1] + new Vector3(0f, 0f, 0.02f), 0.085f, 0.018f, new Vector3(90f, 0f, 0f), 0f)), PropSlot.Wood),
                Piece(PShape("Statue_RimWind", Torus(StatueSockets[2] + new Vector3(0f, 0f, 0.02f), 0.085f, 0.018f, new Vector3(90f, 0f, 0f), 0f)), PropSlot.Accent),
            };
            EditorUtility.SetDirty(statue);
            return statue;
        }

        // ---- gates (2 x 1, +Z faces the player; the opening is x -0.52..0.52, up to y ~1.45) ----

        /// <summary>Pillars and the round arch shared by the fire gate and the return gate.</summary>
        static (SdfShape pillars, SdfShape arch) ArchShapes() => (
            PShape("GateArch_Pillars",
                Cone(new Vector3(0.72f, 0f, 0f), new Vector3(0.7f, 0.95f, 0f), 0.2f, 0.17f, 0f).Bumps(0.014f, 9f).Mirror(),
                Box(new Vector3(0.72f, 0.07f, 0f), new Vector3(0.26f, 0.07f, 0.24f), 0.03f, 0.04f).Mirror(),
                Floor(0f)),
            PShape("GateArch_Arch",
                Torus(new Vector3(0f, 0.95f, 0f), 0.7f, 0.17f, new Vector3(90f, 0f, 0f), 0f).Bumps(0.014f, 9f),
                Box(new Vector3(0f, 1.68f, 0f), new Vector3(0.13f, 0.12f, 0.13f), 0.03f, 0.03f),
                Plane(new Vector3(0f, 0.92f, 0f), Vector3.down, 0.02f)));

        static PropRecipe GateRecipe(string name)
        {
            var r = Recipe(name, new Vector2Int(2, 1), true, false, 2400);
            r.yawJitter = 0f;
            r.scaleJitter = 0f;
            r.resolution = 56;
            return r;
        }

        /// <summary>Fire: an ember-stone arch, glowing stones set in it.</summary>
        static PropRecipe FireGate((SdfShape pillars, SdfShape arch) a)
        {
            var gate = GateRecipe("FireGate");
            gate.palette = Palette(H("#7A4232"), HubWood, H("#5E4038"), HubMoss, H("#FF9F45", 1.1f), H("#3A2A26"));
            gate.pieces = new List<PropPiece>
            {
                Piece(a.pillars, PropSlot.Clay),
                Piece(a.arch, PropSlot.Clay),
                Piece(PShape("FireGate_Embers",
                    Sphere(new Vector3(0.72f, 0.62f, 0.15f), 0.06f, 0f).Mirror(),
                    Sphere(new Vector3(0.7f, 0.32f, 0.17f), 0.045f, 0f),
                    Sphere(new Vector3(-0.73f, 0.86f, 0.13f), 0.04f, 0f),
                    Ell(new Vector3(0f, 1.68f, 0.13f), new Vector3(0.06f, 0.06f, 0.03f), 0f),
                    Ell(new Vector3(0.45f, 1.48f, 0.14f), new Vector3(0.05f, 0.03f, 0.03f), 0f, new Vector3(0f, 0f, 40f)).Mirror()), PropSlot.Glow, PropMotion.Flicker, 0.002f),
            };
            EditorUtility.SetDirty(gate);
            return gate;
        }

        /// <summary>Home (Cinderwood's way back to the Still Isle): the same arch in pale mossy stone.</summary>
        static PropRecipe ReturnGate((SdfShape pillars, SdfShape arch) a)
        {
            var gate = GateRecipe("ReturnGate");
            gate.palette = Palette(HubStone, HubWood, HubStone, HubMoss, HubGold, HubCream);
            gate.pieces = new List<PropPiece>
            {
                Piece(a.pillars, PropSlot.Stone),
                Piece(a.arch, PropSlot.Stone),
                Piece(PShape("ReturnGate_Moss",
                    Ell(new Vector3(0f, 1.78f, 0f), new Vector3(0.24f, 0.08f, 0.17f)).Bumps(0.012f, 22f),
                    Ell(new Vector3(0.55f, 1.42f, 0f), new Vector3(0.22f, 0.08f, 0.2f), 0.06f, new Vector3(0f, 0f, 38f)).Bumps(0.012f, 22f),
                    Ell(new Vector3(0.72f, 0.16f, 0.02f), new Vector3(0.3f, 0.06f, 0.27f), 0.05f).Bumps(0.012f, 22f).Mirror()), PropSlot.Leaf),
            };
            EditorUtility.SetDirty(gate);
            return gate;
        }

        /// <summary>Water: a round-cornered stone well with lily pads and flowers on its rim, under a wooden crossbar.</summary>
        static PropRecipe WaterGate()
        {
            var gate = GateRecipe("WaterGate");
            gate.palette = Palette(H("#E58FB0"), HubWood, H("#A9B2B5"), H("#5E9E5A"), H("#9FD9F0", 0.6f), H("#F7E7A0"));
            gate.pieces = new List<PropPiece>
            {
                Piece(PShape("WaterGate_Well",
                    Box(new Vector3(0f, 0.17f, 0f), new Vector3(0.86f, 0.17f, 0.44f), 0.15f, 0f).Bumps(0.008f, 14f),
                    Box(new Vector3(0f, 0.32f, 0f), new Vector3(0.68f, 0.2f, 0.27f), 0.12f, 0.02f).Sub(),
                    Floor(0f)), PropSlot.Stone),
                Piece(PShape("WaterGate_Frame",
                    Cone(new Vector3(0.82f, 0.1f, 0f), new Vector3(0.82f, 1.25f, 0f), 0.06f, 0.05f, 0f).Mirror(),
                    Cone(new Vector3(-0.98f, 1.2f, 0f), new Vector3(0f, 1.34f, 0f), 0.055f, 0.06f, 0.03f),
                    Cone(new Vector3(0f, 1.34f, 0f), new Vector3(0.98f, 1.2f, 0f), 0.06f, 0.055f, 0.03f)), PropSlot.Wood),
                Piece(PShape("WaterGate_Lilies",
                    Ell(new Vector3(0.62f, 0.35f, 0.36f), new Vector3(0.16f, 0.025f, 0.12f), 0.01f).Mirror(),
                    Ell(new Vector3(0.1f, 0.35f, 0.4f), new Vector3(0.14f, 0.025f, 0.1f), 0.01f),
                    Ell(new Vector3(-0.3f, 0.35f, -0.4f), new Vector3(0.15f, 0.025f, 0.1f), 0.01f),
                    Ell(new Vector3(0.82f, 1.1f, 0.06f), new Vector3(0.09f, 0.18f, 0.05f), 0.04f)), PropSlot.Leaf, PropMotion.Sway, 0.004f),
                Piece(PShape("WaterGate_Flowers",
                    Sphere(new Vector3(0.58f, 0.4f, 0.38f), 0.055f, 0f).Mirror(),
                    Sphere(new Vector3(0.14f, 0.4f, 0.42f), 0.05f, 0f),
                    Sphere(new Vector3(-0.85f, 1.22f, 0.07f), 0.05f, 0f)), PropSlot.Clay),
            };
            EditorUtility.SetDirty(gate);
            return gate;
        }

        /// <summary>Wind: a short stone tunnel with a little windmill on top.</summary>
        static PropRecipe WindGate()
        {
            var gate = GateRecipe("WindGate");
            gate.palette = Palette(H("#D8D2C4"), H("#8C6B4E"), H("#CFC8B8"), HubMoss, H("#E8F4FF", 0.5f), H("#F4F1EA"));
            var hub = new Vector3(0.42f, 1.4f, 0.1f);
            var blades = new List<SdfPrimitive>();
            foreach (float a in new[] { 15f, 105f, 195f, 285f })
            {
                var dir = Quaternion.Euler(0f, 0f, a) * Vector3.up;
                blades.Add(Box(hub + dir * 0.17f, new Vector3(0.05f, 0.15f, 0.012f), 0.01f, 0f, new Vector3(0f, 0f, a)));
            }
            blades.Add(Sphere(hub, 0.05f, 0.02f));
            gate.pieces = new List<PropPiece>
            {
                Piece(PShape("WindGate_Tunnel",
                    Box(new Vector3(0f, 0.48f, 0f), new Vector3(0.9f, 0.48f, 0.44f), 0.16f, 0f).Bumps(0.01f, 12f),
                    Ell(new Vector3(0f, 0.3f, 0f), new Vector3(0.46f, 0.52f, 0.9f), 0.03f).Sub(),
                    Floor(0f)), PropSlot.Stone),
                Piece(PShape("WindGate_Mill",
                    Cone(new Vector3(0.42f, 0.9f, -0.08f), new Vector3(0.42f, 1.48f, -0.08f), 0.17f, 0.12f, 0.03f),
                    Cone(new Vector3(0.42f, 1.46f, -0.08f), new Vector3(0.42f, 1.74f, -0.08f), 0.17f, 0.02f, 0.03f)), PropSlot.Clay),
                Piece(PShape("WindGate_Blades", blades.ToArray()), PropSlot.Accent, PropMotion.Sway, 0.01f),
                Piece(PShape("WindGate_Moss",
                    Ell(new Vector3(-0.42f, 0.98f, 0f), new Vector3(0.32f, 0.07f, 0.3f)).Bumps(0.012f, 22f)), PropSlot.Leaf),
            };
            EditorUtility.SetDirty(gate);
            return gate;
        }
    }
}
