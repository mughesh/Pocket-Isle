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
            // Step 13: the statue wakes a little with each shard (drawn by the statue socket once one is placed).
            Add(Statue(StatueLitName, 1));
            Add(Statue(StatueAwakeName, 2));
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

        /// <summary>Step 10e: the statue's wind socket rim (the Accent slot), green-teal like the wind shard.</summary>
        // Step 12b: wind is green (was teal, which read as blue next to the water shard).
        public static PropColor StatueWindRim => H("#6CCB4E", 0.3f);

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
        /// <summary>Step 13: the statue with one shard placed (its closed eyes glow faintly) and with two (moss sways too).</summary>
        public const string StatueLitName = "StatueLit", StatueAwakeName = "StatueAwake";

        static PropRecipe Statue() => Statue("Statue", 0);

        /// <summary>
        /// Step 13: <paramref name="stage"/> 1 adds faint gold light in the closed eyes (Glow slot, lit with the cracks
        /// once a shard is in), 2 also lets the moss sway (the socket adds the slow breathing).
        /// </summary>
        static PropRecipe Statue(string name, int stage)
        {
            // Nov 12 (Mugesh: "it looks like a deformed dog", broken mesh, no detail): the statue is the Heart of the World
            // asleep in old stone, the same guardian the finale wakes (PropKit.Heart): its pose, its face with a broad
            // muzzle and closed lids, the mane of carved curls, the bead collar, claws and the curled tail. A carved
            // breastplate hangs under the collar and holds the three sockets. Finer bake (48 -> 72) and each part baked
            // as its own piece, so small forms stay crisp.
            var statue = Recipe(name, new Vector2Int(3, 3), true, false, 9000);
            statue.palette = Palette(H("#C8533E", 0.25f), H("#4F86C6", 0.25f), H("#BDB5A5"), HubMoss, H("#FFCB6B", 0.65f), StatueWindRim);
            statue.yawJitter = 0f;
            statue.scaleJitter = 0f;
            statue.resolution = 72;
            statue.outline = 0.03f;

            var bodyPrims = new List<SdfPrimitive>
            {
                // Haunches, a deep torso rising to a high chest (the Heart's own pose).
                Ell(new Vector3(0.5f, 0.46f, -0.3f), new Vector3(0.4f, 0.4f, 0.54f), 0.15f).Mirror(),
                Ell(new Vector3(0f, 0.95f, -0.14f), new Vector3(0.56f, 0.72f, 0.5f), 0.24f),
                Ell(new Vector3(0f, 1.27f, 0.17f), new Vector3(0.5f, 0.52f, 0.38f), 0.18f),
                // Straight front legs, broad paws; back paws tucked beside the haunches.
                Cone(new Vector3(0.27f, 1.05f, 0.3f), new Vector3(0.3f, 0.22f, 0.54f), 0.15f, 0.12f, 0.12f).Mirror(),
                Ell(new Vector3(0.3f, 0.13f, 0.64f), new Vector3(0.17f, 0.11f, 0.22f), 0.08f).Mirror(),
                Ell(new Vector3(0.75f, 0.12f, 0.16f), new Vector3(0.15f, 0.1f, 0.25f), 0.08f).Mirror(),
                // The neck up to the head.
                Cone(new Vector3(0f, 1.5f, 0f), new Vector3(0f, 1.88f, 0.1f), 0.34f, 0.27f, 0.15f),
                // The breastplate: its face passes through the three sockets.
                Ell(new Vector3(0f, 1.08f, 0.36f), new Vector3(0.43f, 0.3f, 0.175f), 0.04f),
                Floor(0.1f),
            };
            var collarEval = new SdfEvaluator(bodyPrims);
            foreach (var s in StatueSockets) bodyPrims.Add(Sphere(s + new Vector3(0f, 0f, 0.03f), 0.08f, 0.02f).Sub());

            var claws = new List<SdfPrimitive>();
            foreach (float x in new[] { 0.3f, -0.3f })
                foreach (float dx in new[] { -0.085f, 0f, 0.085f })
                    claws.Add(Cone(new Vector3(x + dx, 0.09f, 0.8f), new Vector3(x + dx, 0.05f, 0.9f), 0.036f, 0.012f, 0f));

            // The tail curled up at its side.
            var tailPts = new[]
            {
                (new Vector3(-0.45f, 0.24f, -0.82f), 0.17f), (new Vector3(-0.82f, 0.44f, -0.72f), 0.15f), (new Vector3(-0.98f, 0.78f, -0.52f), 0.13f),
                (new Vector3(-0.92f, 1.06f, -0.32f), 0.11f), (new Vector3(-0.74f, 1.18f, -0.24f), 0.09f), (new Vector3(-0.66f, 1.08f, -0.22f), 0.07f),
            };
            var tail = new List<SdfPrimitive>();
            for (int i = 0; i + 1 < tailPts.Length; i++)
                tail.Add(Cone(tailPts[i].Item1, tailPts[i + 1].Item1, tailPts[i].Item2, tailPts[i + 1].Item2, i == 0 ? 0f : 0.06f).Bumps(0.008f, 12f));

            // The bead collar round the neck, low on the chest at the front.
            var collar = new List<SdfPrimitive>();
            for (int i = 0; i < 26; i++)
            {
                float a = i / 26f * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                float y = Mathf.Lerp(1.42f, 1.66f, (1f - Mathf.Cos(a)) * 0.5f);
                var axis = new Vector3(0f, y, 0.06f);
                if (collarEval.Surface(axis + dir * 1.2f, -dir, out var bp, out var bn))
                    collar.Add(Sphere(bp + bn * 0.012f, 0.062f, collar.Count == 0 ? 0f : 0.03f).Bumps(0.006f, 20f));
            }

            // The head: the Heart's, asleep. Stone lids fill the eye sockets, a thin line carved along each.
            var skull = new List<SdfPrimitive>
            {
                Ell(new Vector3(0f, 2.14f, 0.24f), new Vector3(0.39f, 0.35f, 0.36f), 0f),
                Ell(new Vector3(0f, 1.94f, 0.57f), new Vector3(0.24f, 0.15f, 0.25f), 0.12f),
                Ell(new Vector3(0.13f, 1.9f, 0.68f), new Vector3(0.1f, 0.09f, 0.1f), 0.06f).Mirror(),
                Ell(new Vector3(0f, 2.02f, 0.8f), new Vector3(0.1f, 0.065f, 0.065f), 0.05f),
                Ell(new Vector3(0.16f, 2.275f, 0.53f), new Vector3(0.13f, 0.035f, 0.08f), 0.05f, new Vector3(0f, 0f, -12f)).Mirror(),
                Ell(new Vector3(0.37f, 2.17f, 0.16f), new Vector3(0.07f, 0.17f, 0.12f), 0.06f, new Vector3(0f, 0f, 28f)).Mirror(),
                Ell(new Vector3(0f, 1.86f, 0.77f), new Vector3(0.06f, 0.016f, 0.05f), 0.01f).Sub(),
                Ell(new Vector3(0.085f, 1.875f, 0.752f), new Vector3(0.042f, 0.014f, 0.045f), 0.01f, new Vector3(0f, -20f, 28f)).Sub().Mirror(),
                Ell(new Vector3(0.155f, 2.15f, 0.572f), new Vector3(0.095f, 0.074f, 0.057f), 0.02f).Mirror(),
                Ell(new Vector3(0.155f, 2.135f, 0.632f), new Vector3(0.078f, 0.013f, 0.03f), 0.006f, new Vector3(0f, 0f, -10f)).Sub().Mirror(),
                // The empty round setting in its forehead.
                Sphere(ForeheadSocket + new Vector3(0f, 0f, -0.03f), 0.068f, 0.01f).Sub(),
            };

            var plinth = new List<SdfPrimitive>
            {
                Ell(new Vector3(0f, 0.04f, 0f), new Vector3(1.35f, 0.1f, 1.35f), 0f).Bumps(0.006f, 10f),
                Torus(new Vector3(0f, 0.13f, 0f), 1.22f, 0.035f, Vector3.zero, 0.02f),
            };
            // Small square notches carved round the plinth's edge.
            for (int i = 0; i < 16; i++)
            {
                float a = i * 22.5f + 11.25f;
                var d = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                plinth.Add(Box(d * 1.36f + Vector3.up * 0.07f, new Vector3(0.05f, 0.035f, 0.05f), 0.01f, 0.01f, new Vector3(0f, a, 0f)).Sub());
            }
            plinth.Add(Floor(0f));

            var cracks = HeartVeins(collarEval);
            if (cracks.Count > 0) cracks.RemoveAt(0); // no glowing core: the statue sleeps
            // Hairline cracks, not the Heart's bright veins: half as wide, and only every other stretch.
            var fine = new List<SdfPrimitive>();
            for (int i = 0; i < cracks.Count; i++)
            {
                if (i % 2 == 1) continue;
                var c = cracks[i];
                c.size = new Vector3(c.size.x * 0.45f, c.size.y, c.size.z);
                fine.Add(c);
            }
            cracks = fine;

            statue.pieces = new List<PropPiece>
            {
                W(Piece(PShape("Statue_Stone", bodyPrims.ToArray()), PropSlot.Stone), 2500f),
                W(Piece(PShape("Statue_Head", skull.ToArray()), PropSlot.Stone), 1500f),
                W(Piece(PShape("Statue_Mane", Mane().ToArray()), PropSlot.Stone), 2300f),
                W(Piece(PShape("Statue_Plinth", plinth.ToArray()), PropSlot.Stone), 600f),
                W(Piece(PShape("Statue_Claws", claws.ToArray()), PropSlot.Stone), 150f),
                W(Piece(PShape("Statue_Tail", tail.ToArray()), PropSlot.Stone), 380f),
                W(Piece(PShape("Statue_Collar", collar.ToArray()), PropSlot.Stone), 450f),
                W(Piece(PShape("Statue_Moss",
                    Ell(new Vector3(0f, 1.66f, -0.14f), new Vector3(0.54f, 0.12f, 0.42f), 0f).Bumps(0.016f, 18f),
                    Ell(new Vector3(0.24f, 2.6f, 0.2f), new Vector3(0.16f, 0.06f, 0.14f), 0f).Bumps(0.012f, 20f),
                    Ell(new Vector3(-0.3f, 2.66f, 0.05f), new Vector3(0.15f, 0.06f, 0.13f), 0.04f).Bumps(0.012f, 20f),
                    Ell(new Vector3(-0.56f, 2.3f, 0.18f), new Vector3(0.08f, 0.16f, 0.14f), 0.04f).Bumps(0.012f, 20f),
                    Ell(new Vector3(0.52f, 0.8f, -0.4f), new Vector3(0.22f, 0.07f, 0.3f), 0.06f).Bumps(0.012f, 18f),
                    Ell(new Vector3(0.9f, 0.14f, 0.72f), new Vector3(0.3f, 0.05f, 0.2f), 0.05f).Bumps(0.012f, 20f),
                    Ell(new Vector3(-0.7f, 0.14f, -0.95f), new Vector3(0.34f, 0.05f, 0.22f), 0.05f).Bumps(0.012f, 20f)), PropSlot.Leaf,
                    stage >= 2 ? PropMotion.Sway : PropMotion.None, stage >= 2 ? 0.035f : 0f), 520f),
                W(Piece(PShape("Statue_Cracks", cracks.ToArray()), PropSlot.Glow, PropMotion.Flicker, 0.001f), 260f),
                Small("Statue_RimFire", StatueSockets[0], 14f, PropSlot.Clay, PropMotion.None, 0f, 110f, Torus(StatueSockets[0] + new Vector3(0f, 0f, 0.02f), 0.085f, 0.02f, new Vector3(90f, 0f, 0f), 0f)),
                Small("Statue_RimWater", StatueSockets[1], 14f, PropSlot.Wood, PropMotion.None, 0f, 110f, Torus(StatueSockets[1] + new Vector3(0f, 0f, 0.02f), 0.085f, 0.02f, new Vector3(90f, 0f, 0f), 0f)),
                Small("Statue_RimWind", StatueSockets[2], 14f, PropSlot.Accent, PropMotion.None, 0f, 110f, Torus(StatueSockets[2] + new Vector3(0f, 0f, 0.02f), 0.085f, 0.02f, new Vector3(90f, 0f, 0f), 0f)),
            };
            if (stage >= 1)
                // The closed eyes: a thin gold line along each carved lid.
                statue.pieces.Add(Small("Statue_Eyes", new Vector3(0f, 2.15f, 0.59f), 3f, PropSlot.Glow, PropMotion.Flicker, 0.001f, 70f,
                    Ell(new Vector3(0.155f, 2.135f, 0.628f), new Vector3(0.07f, 0.011f, 0.02f), 0f, new Vector3(0f, 0f, -10f)).Mirror()));
            EditorUtility.SetDirty(statue);
            return statue;
        }

        /// <summary>Step 13: writes and bakes the statue's wake stages (the finale setup; the hub writer writes them too).</summary>
/// <summary>Nov 12: the three statue recipes alone (not baked), for a rebake that leaves the hub's other props be.</summary>
        internal static PropRecipe[] WriteStatues() => new[] { Statue(), Statue(StatueLitName, 1), Statue(StatueAwakeName, 2) };

        internal static void WriteStatueStages()
        {
            foreach (var r in new[] { Statue(StatueLitName, 1), Statue(StatueAwakeName, 2) })
            {
                EditorUtility.SetDirty(r);
                AssetDatabase.SaveAssets();
                PropBaker.Bake(r);
            }
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
